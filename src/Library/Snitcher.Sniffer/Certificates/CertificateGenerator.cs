using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;

namespace Snitcher.Sniffer.Certificates
{
    public static class CertificateGenerator
    {
        public static X509Certificate2 CreateSelfSignedCertificate(string subjectName, int validityYears = 10)
        {
            var rsa = RSA.Create(2048);

            var request = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(validityYears));

            return certificate.CopyWithPrivateKey(rsa);
        }

        public static X509Certificate2 CreateCertificateForDomain(
            string domain,
            X509Certificate2 issuerCertificate,
            int validityYears = 1)
        {
            var rsa = RSA.Create(2048);

            var request = new CertificateRequest(
                $"CN={domain}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add Subject Alternative Name
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(domain);
            request.CertificateExtensions.Add(sanBuilder.Build());

            // Add Extended Key Usage for Server Authentication
            var eku = new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false);
            request.CertificateExtensions.Add(eku);

            var certificate = request.Create(
                issuerCertificate,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(validityYears),
                new byte[] { 1, 2, 3, 4 });

            return certificate.CopyWithPrivateKey(rsa);
        }

        public static void ExportToPem(X509Certificate2 certificate, string filePath)
        {
            var certBuilder = new StringBuilder();
            certBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
            certBuilder.AppendLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            certBuilder.AppendLine("-----END CERTIFICATE-----");

            File.WriteAllText(filePath, certBuilder.ToString());
        }

        public static void ExportPrivateKeyToPem(RSA rsa, string filePath)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.AppendLine("-----BEGIN PRIVATE KEY-----");
            keyBuilder.AppendLine(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks));
            keyBuilder.AppendLine("-----END PRIVATE KEY-----");

            File.WriteAllText(filePath, keyBuilder.ToString());
        }

        public static X509Certificate2 LoadFromPem(string certPath, string keyPath)
        {
            var certPem = File.ReadAllText(certPath);
            var keyPem = File.ReadAllText(keyPath);

            return X509Certificate2.CreateFromPem(certPem, keyPem);
        }

        public static void ExportCertificateAndKey(X509Certificate2 certificate, string certPath, string keyPath)
        {
            ExportToPem(certificate, certPath);

            if (certificate.HasPrivateKey && certificate.GetRSAPrivateKey() is RSA rsa)
            {
                ExportPrivateKeyToPem(rsa, keyPath);
            }
        }
    }
}
