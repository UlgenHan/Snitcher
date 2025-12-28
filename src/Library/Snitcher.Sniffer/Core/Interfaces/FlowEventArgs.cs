using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public class FlowEventArgs : EventArgs
    {
        public Flow Flow { get; }

        public FlowEventArgs(Flow flow) => Flow = flow;
    }
}
