using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface IRequestInterceptor
    {
        Task<Models.HttpRequestMessage> InterceptAsync(Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default);
        int Priority { get; } // Lower = higher priority
    }
}
