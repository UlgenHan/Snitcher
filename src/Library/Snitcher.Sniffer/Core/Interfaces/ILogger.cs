using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface ILogger
    {
        void LogInfo(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogFlow(Flow flow);
    }
}
