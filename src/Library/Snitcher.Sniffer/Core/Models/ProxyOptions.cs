namespace Snitcher.Sniffer.Core.Models
{
    public class ProxyOptions
    {
        public int ListenPort { get; set; } = 7865;
        public string ListenAddress { get; set; } = "127.0.0.1";
        public bool InterceptHttps { get; set; } = true;
        public string CaCertificatePath { get; set; } = string.Empty;
        public string CaPassword { get; set; } = "snitch";
        public bool EnableLogging { get; set; } = true;
    }
}
