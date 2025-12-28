namespace Snitcher.Sniffer.Core.Models
{
    public class HttpRequestMessage
    {
        public Core.Models.HttpMethod Method { get; set; } = Core.Models.HttpMethod.Get;
        public Uri Url { get; set; } = new Uri(@"http://localhost");
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public byte[] Body { get; set; } = Array.Empty<byte>();
        public string HttpVersion { get; set; } = "HTTP/1.1";
    }
}
