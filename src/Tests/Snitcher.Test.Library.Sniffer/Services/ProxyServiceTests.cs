using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Snitcher.UI.Desktop.Models;
using Snitcher.UI.Desktop.Domains.Proxy;
using Snitcher.Sniffer.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Snitcher.Test.Library.Sniffer.Services
{
    [TestFixture]
    public class ProxyServiceTests
    {
        private Mock<ILogger<ProxyService>> _mockLogger;
        private Mock<ICertificateManager> _mockCertificateManager;
        private Mock<Snitcher.Sniffer.Core.Interfaces.ILogger> _mockSnifferLogger;
        private IProxyService _proxyService;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ProxyService>>();
            _mockCertificateManager = new Mock<ICertificateManager>();
            _mockSnifferLogger = new Mock<Snitcher.Sniffer.Core.Interfaces.ILogger>();
            _proxyService = new ProxyService(_mockLogger.Object, _mockCertificateManager.Object, _mockSnifferLogger.Object);
        }

        [Test]
        public void ProxyService_InitializesWithCorrectState()
        {
            // Assert
            Assert.That(_proxyService.IsRunning, Is.False);
        }

        [Test]
        public void ProxyService_CanStartAndStop()
        {
            // Arrange
            FlowItem? capturedFlow = null;
            _proxyService.FlowCaptured += (sender, flow) => capturedFlow = flow;

            // Act & Assert - These should not throw
            Assert.DoesNotThrowAsync(async () => await _proxyService.StartAsync(8080));
            Assert.That(_proxyService.IsRunning, Is.True);

            Assert.DoesNotThrowAsync(async () => await _proxyService.StopAsync());
            Assert.That(_proxyService.IsRunning, Is.False);
        }

        [Test]
        public void ProxyService_EventsWorkCorrectly()
        {
            // Arrange
            string? statusChanged = null;
            string? errorOccurred = null;

            _proxyService.StatusChanged += (sender, status) => statusChanged = status;
            _proxyService.ErrorOccurred += (sender, error) => errorOccurred = null;

            // Act
            var startTask = _proxyService.StartAsync(8080);
            startTask.Wait(5000); // Wait up to 5 seconds

            // Assert
            Assert.That(statusChanged, Is.Not.Null.And.Contains("Running"));
        }

        [Test]
        public void ProxyService_InvalidPortThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _proxyService.StartAsync(80));
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_proxyService.IsRunning)
            {
                await _proxyService.StopAsync();
            }
        }
    }
}
