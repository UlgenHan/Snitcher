using NUnit.Framework;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Test.Library.Sniffer.Models;

[TestFixture]
public class FlowTests
{
    [Test]
    public void Flow_CreatesWithDefaultValues()
    {
        // Arrange & Act
        var flow = new Flow();

        // Assert
        Assert.That(flow.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(flow.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(flow.ClientAddress, Is.EqualTo(string.Empty));
        Assert.That(flow.Request, Is.Not.Null);
        Assert.That(flow.Response, Is.Not.Null);
        Assert.That(flow.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(flow.Status, Is.EqualTo(FlowStatus.Pending));
    }

    [Test]
    public void Flow_CanSetProperties()
    {
        // Arrange
        var flow = new Flow();
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var clientAddress = "192.168.1.1";
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        flow.Id = id;
        flow.Timestamp = timestamp;
        flow.ClientAddress = clientAddress;
        flow.Duration = duration;
        flow.Status = FlowStatus.Completed;

        // Assert
        Assert.That(flow.Id, Is.EqualTo(id));
        Assert.That(flow.Timestamp, Is.EqualTo(timestamp));
        Assert.That(flow.ClientAddress, Is.EqualTo(clientAddress));
        Assert.That(flow.Duration, Is.EqualTo(duration));
        Assert.That(flow.Status, Is.EqualTo(FlowStatus.Completed));
    }

    [Test]
    public void FlowStatus_EnumValues()
    {
        // Assert
        Assert.That((int)FlowStatus.Pending, Is.EqualTo(0));
        Assert.That((int)FlowStatus.Completed, Is.EqualTo(1));
        Assert.That((int)FlowStatus.Failed, Is.EqualTo(2));
    }
}
