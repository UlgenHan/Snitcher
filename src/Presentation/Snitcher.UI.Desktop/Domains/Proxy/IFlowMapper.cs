using Snitcher.Sniffer.Core.Models;
using Snitcher.UI.Desktop.Models;

namespace Snitcher.UI.Desktop.Domains.Proxy
{
    public interface IFlowMapper
    {
        FlowItem MapToFlowItem(Flow flow);
    }
}
