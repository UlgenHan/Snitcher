using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snitcher.Sniffer.Certificates
{
    public class CertificateInfo
    {
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public string Thumbprint { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public bool HasPrivateKey { get; set; }
        public List<string> SubjectAlternativeNames { get; set; } = new();

        public bool IsValid => DateTime.UtcNow >= NotBefore && DateTime.UtcNow <= NotAfter;
        public int DaysUntilExpiry => (int)(NotAfter - DateTime.UtcNow).TotalDays;

        public override string ToString()
        {
            return $"{Subject} (Valid: {NotBefore:yyyy-MM-dd} to {NotAfter:yyyy-MM-dd})";
        }
    }
}
