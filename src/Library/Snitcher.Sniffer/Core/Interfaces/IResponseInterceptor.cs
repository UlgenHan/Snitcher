using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface IResponseInterceptor
    {
        Task<Models.HttpResponseMessage> InterceptAsync(Models.HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default);
        int Priority { get; } // Lower = higher priority
    }
}
