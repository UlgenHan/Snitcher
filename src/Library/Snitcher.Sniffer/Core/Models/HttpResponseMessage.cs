namespace Snitcher.Sniffer.Core.Models
{
    public class HttpResponseMessage
    {
        public int StatusCode { get; set; } = 200;
        public string ReasonPhrase { get; set; } = "OK";
        public Dictionary<string, string> Headers { get; set; } = new();
        public byte[] Body { get; set; } = Array.Empty<byte>();
        public string HttpVersion { get; set; } = "HTTP/1.1";
    }
}
