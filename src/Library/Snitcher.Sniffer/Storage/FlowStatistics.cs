using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snitcher.Sniffer.Storage
{
    public class FlowStatistics
    {
        public int TotalFlows { get; set; }
        public int SuccessfulFlows { get; set; }
        public int FailedFlows { get; set; }
        public int PendingFlows { get; set; }
        public Dictionary<string, int> Domains { get; set; } = new();
        public Dictionary<string, int> StatusCodes { get; set; } = new();
        public Dictionary<Core.Models.HttpMethod, int> Methods { get; set; } = new();
        public TimeSpan AverageDuration { get; set; }
        public DateTime FirstFlowTime { get; set; } = DateTime.UtcNow;
        public DateTime LastFlowTime { get; set; } = DateTime.UtcNow;

        public double SuccessRate => TotalFlows > 0 ? (double)SuccessfulFlows / TotalFlows * 100 : 0;

        public override string ToString()
        {
            return $"Total: {TotalFlows}, Success: {SuccessRate:F1}%, Avg Duration: {AverageDuration.TotalMilliseconds:F0}ms";
        }
    }
}
