namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface IHttpParser
    {
        Task<Models.HttpRequestMessage> ParseRequestAsync(Stream stream, CancellationToken cancellationToken = default);
        Task<Models.HttpResponseMessage> ParseResponseAsync(Stream stream, CancellationToken cancellationToken = default);
        Task WriteRequestAsync(Models.HttpRequestMessage request, Stream stream, CancellationToken cancellationToken = default);
        Task WriteResponseAsync(Models.HttpResponseMessage response, Stream stream, CancellationToken cancellationToken = default);
    }
}
