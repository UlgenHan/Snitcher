namespace Snitcher.Sniffer.Core.Models
{
    public class Flow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ClientAddress { get; set; } = string.Empty;
        public HttpRequestMessage Request { get; set; } = new();
        public HttpResponseMessage Response { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public FlowStatus Status { get; set; }
    }

    public enum FlowStatus
    {
        Pending,
        Completed,
        Failed
    }
}
