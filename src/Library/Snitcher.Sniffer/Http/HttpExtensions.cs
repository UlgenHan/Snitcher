namespace Snitcher.Sniffer.Http
{
    public static class HttpExtensions
    {
        public static bool IsConnect(this Core.Models.HttpRequestMessage request)
        {
            return request.Method == Core.Models.HttpMethod.Connect;
        }

        public static bool IsHttps(this Core.Models.HttpRequestMessage request)
        {
            return request.Url.Scheme == "https" || request.IsConnect();
        }

        public static string GetHost(this Core.Models.HttpRequestMessage request)
        {
            if (request.Headers.TryGetValue("Host", out var host))
                return host;

            return request.Url.Host;
        }

        public static int GetPort(this Core.Models.HttpRequestMessage request)
        {
            if (request.IsConnect())
            {
                // For CONNECT, URL is like "example.com:443"
                var parts = request.Url.OriginalString.Split(':');
                return parts.Length > 1 ? int.Parse(parts[1]) : 443;
            }

            return request.Url.Port;
        }
    }
}
