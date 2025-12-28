# Snitcher.Sniffer - HTTP Proxy Inspector Library

A powerful HTTP proxy inspection library that enables real-time monitoring and debugging of HTTP/HTTPS traffic.

## Features

- **Real-time HTTP/HTTPS Traffic Interception**: Monitor all HTTP requests and responses
- **Certificate Management**: Automatic SSL certificate generation and management
- **Flow Storage**: In-memory and file-based storage of captured flows
- **Extensible Interceptors**: Request and response interceptors for custom processing
- **Comprehensive Logging**: Detailed logging for debugging and monitoring

## Quick Start

### Basic Usage

```csharp
using Microsoft.Extensions.Logging;
using Snitcher.Sniffer.Core.Services;
using Snitcher.Sniffer.Core.Models;

// Create logger
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ProxyService>();

// Create proxy service
var proxyService = new ProxyService(logger);

// Subscribe to flow events
proxyService.FlowCaptured += (sender, flow) => {
    Console.WriteLine($"Captured: {flow.Request.Method} {flow.Request.Url}");
};

// Start proxy on port 8080
await proxyService.StartAsync(8080);

// Proxy is now running and capturing traffic
Console.WriteLine("Proxy server started on port 8080");

// Stop proxy when done
await proxyService.StopAsync();
```

### Desktop Application Integration

The Snitcher.Sniffer library is fully integrated with the Snitcher Desktop application:

1. **Proxy Inspector View**: Navigate to the Proxy Inspector section in the desktop app
2. **Start/Stop Proxy**: Use the proxy controls to start/stop the proxy server
3. **Real-time Monitoring**: View captured HTTP flows in real-time
4. **Flow Details**: Click on any flow to see detailed request/response information
5. **Filtering and Search**: Filter flows by method, status, or search by URL

### Configuration Options

```csharp
var options = new ProxyOptions
{
    ListenPort = 8080,
    ListenAddress = "127.0.0.1",
    InterceptHttps = true,
    EnableLogging = true
};
```

## Architecture

### Core Components

- **ProxyServer**: Main proxy server implementation
- **ConnectionHandler**: Handles TCP connections and HTTP parsing
- **HttpParser**: Parses HTTP requests and responses
- **CertificateManager**: Manages SSL certificates for HTTPS interception
- **FlowStorage**: Stores captured HTTP flows

### Data Models

- **Flow**: Represents a complete HTTP request-response cycle
- **HttpRequestMessage**: HTTP request details
- **HttpResponseMessage**: HTTP response details
- **ProxyOptions**: Configuration options for the proxy server

## Integration Points

### UI Integration

The library integrates with the desktop application through:

1. **ProxyService**: Service layer that abstracts proxy functionality
2. **FlowMapper**: Maps Snitcher models to UI models
3. **Dependency Injection**: Registered in the application's DI container

### Event System

```csharp
// Flow captured event
proxyService.FlowCaptured += (sender, flow) => {
    // Handle captured flow
};

// Status changed event
proxyService.StatusChanged += (sender, status) => {
    // Handle status changes
};

// Error occurred event
proxyService.ErrorOccurred += (sender, error) => {
    // Handle errors
};
```

## Testing

The library includes comprehensive tests:

```bash
# Run all tests
dotnet test src/Tests/Snitcher.Test.Library.Sniffer

# Run specific test categories
dotnet test --filter "TestCategory=Integration"
```

## Security Considerations

- SSL certificates are generated dynamically for HTTPS interception
- All traffic is logged for debugging purposes
- Proxy binds to localhost by default for security
- Certificate passwords are configurable

## Performance

- Asynchronous processing for high throughput
- In-memory flow storage with optional file persistence
- Configurable connection limits
- Efficient HTTP parsing

## Troubleshooting

### Common Issues

1. **Port Already in Use**: Change the proxy port in configuration
2. **Certificate Trust Issues**: Install the generated CA certificate
3. **Firewall Blocking**: Ensure firewall allows proxy connections

### Debug Logging

Enable debug logging for detailed troubleshooting:

```csharp
services.AddLogging(builder => {
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

## License

This project is part of the Snitcher application suite.
