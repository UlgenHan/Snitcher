using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Services
{
    public class ConsoleLogger : ILogger
    {
        public void LogInfo(string message, params object[] args)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {string.Format(message, args)}");
        }

        public void LogWarning(string message, params object[] args)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {string.Format(message, args)}");
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {string.Format(message, args)}");
            Console.WriteLine($"Exception: {exception}");
        }

        public void LogFlow(Flow flow)
        {
            var status = flow.Status == FlowStatus.Completed ? "?" : "?";
            Console.WriteLine($"[FLOW] {status} {flow.ClientAddress} {flow.Request.Method} {flow.Request.Url} ? {flow.Response.StatusCode} ({flow.Duration.TotalMilliseconds:F0}ms)");
        }
    }
}
