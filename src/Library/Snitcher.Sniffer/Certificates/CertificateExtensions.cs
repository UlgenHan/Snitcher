using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Snitcher.Sniffer.Certificates
{
    public static class CertificateExtensions
    {
        public static CertificateInfo GetInfo(this X509Certificate2 certificate)
        {
            var info = new CertificateInfo
            {
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                NotBefore = certificate.NotBefore,
                NotAfter = certificate.NotAfter,
                Thumbprint = certificate.Thumbprint ?? string.Empty,
                SerialNumber = certificate.SerialNumber ?? string.Empty,
                HasPrivateKey = certificate.HasPrivateKey
            };

            // Extract Subject Alternative Names
            var sanExtension = certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().FirstOrDefault();
            if (sanExtension != null)
            {
                info.SubjectAlternativeNames = sanExtension.EnumerateDnsNames().ToList();
            }

            return info;
        }
    }
}
