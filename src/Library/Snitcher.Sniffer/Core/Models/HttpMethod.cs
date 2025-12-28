namespace Snitcher.Sniffer.Core.Models
{
    public class HttpMethod
    {
        public string Method { get; set; }

        public static readonly HttpMethod Get = new("GET");
        public static readonly HttpMethod Post = new("POST");
        public static readonly HttpMethod Put = new("PUT");
        public static readonly HttpMethod Delete = new("DELETE");
        public static readonly HttpMethod Head = new("HEAD");
        public static readonly HttpMethod Connect = new("CONNECT");

        public HttpMethod(string method) => Method = method.ToUpperInvariant();

        public static implicit operator string(HttpMethod method) => method.Method;
        public static implicit operator HttpMethod(string method) => new(method);

        public override string ToString() => Method;
        public override bool Equals(object? obj)
        {
            return obj is HttpMethod other && other.Method == Method;
        }

        public override int GetHashCode() => Method.GetHashCode();
    }
}
