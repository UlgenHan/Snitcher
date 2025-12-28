using Microsoft.Extensions.Logging;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;
using System;

namespace Snitcher.UI.Desktop.Services
{
    public class SnitcherLoggerAdapter : Snitcher.Sniffer.Core.Interfaces.ILogger
    {
        private readonly ILogger<SnitcherLoggerAdapter> _logger;

        public SnitcherLoggerAdapter(ILogger<SnitcherLoggerAdapter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInfo(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(System.Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void LogFlow(Flow flow)
        {
            _logger.LogDebug("Flow captured: {Method} {Url} -> {StatusCode}", 
                flow.Request.Method, 
                flow.Request.Url, 
                flow.Response.StatusCode);
        }
    }
}
