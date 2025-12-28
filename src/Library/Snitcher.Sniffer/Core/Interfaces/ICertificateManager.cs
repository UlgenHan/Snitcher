using System.Security.Cryptography.X509Certificates;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface ICertificateManager
    {
        Task<X509Certificate2> GetOrCreateCACertificateAsync(string password, CancellationToken cancellationToken = default);
        Task<X509Certificate2> GetCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default);
        Task<X509Certificate2> GenerateCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default);
        bool IsCACertificateTrusted();
        Task InstallCACertificateAsync(string password, CancellationToken cancellationToken = default);
    }
}
