using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Snitcher.Sniffer.Core.Interfaces;

namespace Snitcher.Sniffer.Certificates
{
    public class CertificateManager : ICertificateManager
    {
        private readonly ILogger _logger;
        private X509Certificate2? _caCertificate;
        private readonly Dictionary<string, X509Certificate2> _certCache = new();
        private readonly object _lock = new();

        public CertificateManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<X509Certificate2> GetOrCreateCACertificateAsync(string password, CancellationToken cancellationToken = default)
        {
            if (_caCertificate != null) return _caCertificate;

            lock (_lock)
            {
                if (_caCertificate != null) return _caCertificate;

                var caPath = "mitmproxy-ca.pfx";
                if (File.Exists(caPath))
                {
                    _logger.LogInfo("Loading existing CA certificate from {0}", caPath);
                    _caCertificate = new X509Certificate2(caPath, password,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
                }
                else
                {
                    _logger.LogInfo("Generating new CA certificate...");
                    _caCertificate = GenerateCACertificate(password);
                    File.WriteAllBytes(caPath, _caCertificate.Export(X509ContentType.Pfx, password));
                }
            }

            _logger.LogInfo("CA certificate loaded: {0}", _caCertificate.Subject);
            return _caCertificate;
        }

        public async Task<X509Certificate2> GetCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default)
        {
            // Ensure CA certificate is loaded first
            if (_caCertificate == null)
            {
                await GetOrCreateCACertificateAsync("mitmproxy", cancellationToken);
            }
            
            return await GenerateCertificateForHostAsync(hostname, cancellationToken);
        }

        public async Task<X509Certificate2> GenerateCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_certCache.TryGetValue(hostname, out var cached))
                    return cached;

                _logger.LogInfo("Generating certificate for {0}", hostname);

                var ca = _caCertificate ?? throw new InvalidOperationException("CA certificate not loaded");

                if (!ca.HasPrivateKey)
                    throw new InvalidOperationException("CA certificate does not have a private key");

                // Create RSA key for host certificate
                var rsa = RSA.Create(2048);

                var request = new CertificateRequest(
                    $"CN={hostname}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add Subject Alternative Name
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(hostname);
                request.CertificateExtensions.Add(sanBuilder.Build());

                // Create certificate signed by CA
                var cert = request.Create(
                    ca,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(1),
                    new byte[] { 1, 2, 3, 4 });

                // Attach private key and make exportable
                var certWithPrivateKey = cert.CopyWithPrivateKey(rsa);
                var exportableCert = new X509Certificate2(
                    certWithPrivateKey.Export(X509ContentType.Pfx, "mitmproxy"),
                    "mitmproxy",
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                _certCache[hostname] = exportableCert;
                _logger.LogInfo("Generated certificate for {0}: {1}", hostname, exportableCert.Subject);

                return exportableCert;
            }
        }

        public bool IsCACertificateTrusted()
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "MITMProxy CA", false);
                return certs.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking CA certificate trust");
                return false;
            }
        }

        public async Task InstallCACertificateAsync(string password, CancellationToken cancellationToken = default)
        {
            var ca = await GetOrCreateCACertificateAsync(password, cancellationToken);

            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // Check if already installed
                var existing = store.Certificates.Find(X509FindType.FindBySubjectName, "MITMProxy CA", false);
                if (existing.Count > 0)
                {
                    _logger.LogInfo("CA certificate already installed");
                    return;
                }

                store.Add(ca);
                _logger.LogInfo("CA certificate installed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install CA certificate");
                throw;
            }
        }

        private X509Certificate2 GenerateCACertificate(string password)
        {
            var rsa = RSA.Create(4096);

            var caRequest = new CertificateRequest(
                "CN=MITMProxy CA",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add CA basic constraints
            caRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            // Add key usage
            caRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.KeyCertSign |
                    X509KeyUsageFlags.CrlSign, false));

            var caCert = caRequest.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(10));

            return new X509Certificate2(
                caCert.Export(X509ContentType.Pfx, password),
                password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
        }
    }
}
