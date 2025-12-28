using Snitcher.UI.Desktop.Models;
using System.Threading.Tasks;

namespace Snitcher.UI.Desktop.Domains.RequestBuilder
{
    public interface IRequestSender
    {
        Task<HttpResponse> SendRequestAsync(HttpRequest request);
    }
}
