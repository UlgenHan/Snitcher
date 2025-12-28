using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Storage
{
    public static class FlowStatisticsExtensions
    {
        public static FlowStatistics CalculateStatistics(this IEnumerable<Flow> flows)
        {
            var flowList = flows.ToList();

            if (!flowList.Any())
                return new FlowStatistics();

            var stats = new FlowStatistics
            {
                TotalFlows = flowList.Count,
                SuccessfulFlows = flowList.Count(f => f.Status == FlowStatus.Completed),
                FailedFlows = flowList.Count(f => f.Status == FlowStatus.Failed),
                PendingFlows = flowList.Count(f => f.Status == FlowStatus.Pending),
                FirstFlowTime = flowList.Min(f => f.Timestamp),
                LastFlowTime = flowList.Max(f => f.Timestamp),
                AverageDuration = TimeSpan.FromTicks((long)flowList.Average(f => f.Duration.Ticks))
            };

            // Domain statistics
            stats.Domains = flowList
                .GroupBy(f => f.Request.Url.Host)
                .ToDictionary(g => g.Key, g => g.Count());

            // Status code statistics
            stats.StatusCodes = flowList
                .Where(f => f.Response.StatusCode > 0)
                .GroupBy(f => f.Response.StatusCode.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Method statistics
            stats.Methods = flowList
                .GroupBy(f => f.Request.Method)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }
    }
}
