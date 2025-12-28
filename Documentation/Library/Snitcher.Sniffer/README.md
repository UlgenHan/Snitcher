# Snitcher.Sniffer Library Documentation

## Overview

The Snitcher.Sniffer library is a comprehensive HTTP/HTTPS proxy implementation designed for network traffic interception, monitoring, and modification. It provides a complete infrastructure for capturing, analyzing, and manipulating HTTP traffic in real-time, making it ideal for debugging, testing, security analysis, and network monitoring scenarios.

**Why this library exists:** To provide a flexible, extensible proxy framework that can intercept both HTTP and HTTPS traffic while maintaining high performance and ease of use.

**Problem it solves:** Enables developers and security professionals to inspect, modify, and analyze network traffic without requiring complex networking tools or deep protocol knowledge.

**What would break if removed:** The entire proxy and traffic interception functionality would be lost. No network monitoring, HTTPS interception, or traffic modification capabilities would be available.

## Tech Stack Identification

- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Net.Sockets (TCP networking)
  - System.Net.Security (SSL/TLS streams)
  - System.Security.Cryptography.X509Certificates (certificate management)
  - Microsoft.Extensions.Logging.Abstractions (logging abstraction)
- **Persistence/communication:** TCP sockets, SSL streams, in-memory storage, file system for certificates
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** Windows/Linux/macOS with .NET 8.0 runtime, network access, certificate store permissions
- **Version hints:** Modern async/await patterns, cancellation token support, dependency injection throughout

## Architectural Overview

### Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│                  (UI/Desktop Components)                     │
├─────────────────────────────────────────────────────────────┤
│                   Application Layer                          │
│              (ProxyServer, ConnectionHandler)                │
├─────────────────────────────────────────────────────────────┤
│                    Protocol Layer                            │
│              (HttpParser, SSL Interception)                  │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                        │
│        (CertificateManager, FlowStorage, Logger)            │
└─────────────────────────────────────────────────────────────┘
```

### Core Components

1. **Proxy Server Infrastructure**
   - `ProxyServer` - Main TCP listener and connection coordinator
   - `ConnectionHandler` - Individual connection processing
   - `NetworkConnectionHandler` - Alternative connection management

2. **HTTP Protocol Handling**
   - `HttpParser` - HTTP request/response parsing and generation
   - `HttpRequestMessage`/`HttpResponseMessage` - HTTP data models
   - Support for HTTP/1.1, chunked encoding, CONNECT method

3. **HTTPS Interception**
   - `CertificateManager` - CA certificate and per-host certificate generation
   - `SslInterceptor` - SSL/TLS stream interception
   - Certificate trust store management

4. **Traffic Modification**
   - `InterceptorManager` - Coordinated interceptor execution
   - `IRequestInterceptor`/`IResponseInterceptor` - Modification interfaces
   - Built-in interceptors for header injection and response logging

5. **Storage and Logging**
   - `InMemoryFlowStorage` - Flow data persistence
   - `ConsoleLogger` - Simple logging implementation
   - `Flow` model - HTTP transaction representation

## Key Features

### HTTP/HTTPS Proxy Capabilities
- Full HTTP/1.1 protocol support
- HTTPS traffic interception with dynamic certificate generation
- CONNECT method handling for tunneling
- Request/response modification through interceptor system

### Certificate Management
- Automatic CA certificate generation and installation
- Per-host certificate creation with Subject Alternative Names
- Certificate caching for performance optimization
- Trust store integration for seamless HTTPS interception

### Extensible Architecture
- Plugin-like interceptor system for traffic modification
- Dependency injection throughout for testability
- Interface-based design for easy component replacement
- Async/await patterns for high performance

### Monitoring and Debugging
- Comprehensive flow tracking and storage
- Detailed logging throughout the system
- Real-time traffic inspection capabilities
- Performance metrics and timing information

## Execution Flow

### Proxy Startup
1. `ProxyServer.StartAsync()` initializes TCP listener
2. Certificate manager loads or generates CA certificate
3. Connection handlers and interceptors are wired up
4. Server begins accepting client connections

### Connection Processing
1. Client connects to proxy on configured port
2. `ConnectionHandler.HandleConnectionAsync()` processes connection
3. HTTP request parsed using `HttpParser`
4. Request interceptors applied in priority order
5. Request routed to HTTP handler or HTTPS CONNECT handler

### HTTP Request Handling
1. Target server connection established
2. Request forwarded to destination
3. Response parsed from target
4. Response interceptors applied
5. Response sent back to client
6. Flow data stored and events raised

### HTTPS Interception
1. CONNECT request received and parsed
2. TCP tunnel established to target server
3. Dynamic certificate generated for target hostname
4. SSL interception set up with generated certificates
5. Encrypted traffic intercepted and decrypted
6. HTTP processing continues as with unencrypted traffic

## Configuration and Usage

### Basic Proxy Setup
```csharp
// Create services
var logger = new ConsoleLogger();
var httpParser = new HttpParser(logger);
var certManager = new CertificateManager(logger);
var flowStorage = new InMemoryFlowStorage();
var interceptors = new List<IRequestInterceptor>
{
    new HeaderInjectorInterceptor(headers, logger)
};
var responseInterceptors = new List<IResponseInterceptor>
{
    new ResponseLoggerInterceptor(logger)
};

// Create connection handler
var connectionHandler = new ConnectionHandler(
    httpParser, certManager, flowStorage, 
    interceptors, responseInterceptors, logger);

// Create and start proxy server
var proxyServer = new ProxyServer(connectionHandler, logger);
var options = new ProxyOptions 
{ 
    ListenPort = 8080, 
    InterceptHttps = true 
};

await proxyServer.StartAsync(options);
```

### Certificate Configuration
```csharp
// Install CA certificate for HTTPS interception
await certManager.InstallCACertificateAsync("password");

// Generate certificate for specific host
var hostCert = await certManager.GetCertificateForHostAsync("example.com");
```

### Custom Interceptors
```csharp
public class CustomAuthInterceptor : IRequestInterceptor
{
    public int Priority => 100;
    
    public async Task<HttpRequestMessage> InterceptAsync(
        HttpRequestMessage request, Flow flow, CancellationToken cancellationToken)
    {
        if (request.Url.Host.Contains("api.example.com"))
        {
            request.Headers["Authorization"] = $"Bearer {GetApiToken()}";
        }
        
        return await Task.FromResult(request);
    }
}
```

## Performance Considerations

### Memory Usage
- In-memory flow storage grows with traffic volume
- Certificate caching consumes memory per unique hostname
- Large HTTP bodies may impact memory during processing

### Threading Model
- Each connection handled on separate task thread
- Thread-safe implementations for shared components
- Lock-based synchronization for storage and certificate management

### Network Performance
- Async/await throughout prevents thread blocking
- Stream-based processing minimizes memory allocation
- Efficient SSL interception with certificate reuse

## Security Considerations

### Certificate Security
- CA certificate protected with configurable password
- Per-host certificates with limited validity periods
- Proper key storage flags for secure certificate handling

### Traffic Privacy
- HTTPS interception requires explicit trust installation
- Sensitive data logging controlled by interceptor configuration
- No persistent storage of traffic content by default

### Access Control
- Proxy can be bound to specific interfaces
- Certificate installation requires appropriate permissions
- Network access controlled by operating system permissions

## Extension Points

### Custom Storage Implementations
```csharp
public class DatabaseFlowStorage : IFlowStorage
{
    // Implement database persistence for flows
    public async Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken)
    {
        // Store in database instead of memory
    }
}
```

### Advanced Interceptors
- Request/response body modification
- Traffic filtering and blocking
- Content transformation and compression
- Security scanning and validation

### Protocol Extensions
- HTTP/2 support implementation
- WebSocket protocol handling
- Custom protocol parsers
- Traffic shaping and throttling

## Testing and Debugging

### Unit Testing
- All components designed for dependency injection
- Mock implementations available for all interfaces
- In-memory implementations for testing without external dependencies

### Integration Testing
- Test proxy with real HTTP/HTTPS traffic
- Certificate management testing with test certificates
- Performance testing under various load conditions

### Debugging Tools
- Comprehensive logging throughout the system
- Flow inspection and analysis capabilities
- Real-time traffic monitoring
- Certificate inspection tools

## Deployment Considerations

### Production Environment
- Replace ConsoleLogger with production logging framework
- Use database flow storage for persistence
- Implement proper certificate management for enterprise deployment
- Add monitoring and alerting capabilities

### Security Hardening
- Limit proxy binding to required interfaces only
- Implement authentication for proxy access
- Add rate limiting and abuse prevention
- Regular certificate rotation policies

### Scaling Considerations
- Distributed deployment with load balancing
- Shared storage for flow data across instances
- Centralized certificate management
- Performance monitoring and optimization

## Troubleshooting

### Common Issues
- Certificate trust problems for HTTPS interception
- Port binding conflicts on startup
- Memory usage growth with high traffic volumes
- SSL/TLS handshake failures

### Debugging Steps
1. Enable detailed logging to identify failure points
2. Verify certificate installation and trust status
3. Check network connectivity and port availability
4. Monitor system resources under load
5. Validate interceptor configuration and behavior

### Performance Tuning
- Adjust flow storage limits and cleanup policies
- Optimize interceptor chains for minimal overhead
- Tune thread pool settings for high concurrency
- Monitor garbage collection and memory allocation

## Related Documentation

### Core Components
- [ProxyServer](Core/Services/ProxyServer.md) - Main proxy server implementation
- [ConnectionHandler](Core/Services/ConnectionHandler.md) - Connection processing logic
- [HttpParser](Http/HttpParser.md) - HTTP protocol parsing
- [CertificateManager](Certificates/CertificateManager.md) - Certificate authority management

### Interceptors
- [InterceptorManager](Interceptors/InterceptorManager.md) - Interceptor coordination
- [HeaderInjectorInterceptor](Interceptors/HeaderInjectorInterceptor.md) - Header injection
- [ResponseLoggerInterceptor](Interceptors/ResponseLoggerInterceptor.md) - Response logging

### Infrastructure
- [InMemoryFlowStorage](Core/Services/InMemoryFlowStorage.md) - Flow data storage
- [ConsoleLogger](Core/Services/ConsoleLogger.md) - Logging implementation

### Models and Interfaces
- [Flow](Core/Models/Flow.md) - HTTP transaction model
- [HttpRequestMessage](Core/Models/HttpRequestMessage.md) - Request data model
- [HttpResponseMessage](Core/Models/HttpResponseMessage.md) - Response data model
- [ProxyOptions](Core/Models/ProxyOptions.md) - Configuration model

## Knowledge Transfer

### Reusable Patterns
- Async TCP server implementation with proper cancellation
- Certificate authority pattern for HTTPS interception
- Interceptor chain pattern for extensible processing
- Repository pattern for data storage abstraction
- Dependency injection throughout for testability

### Implementation Insights
- HTTP/1.1 protocol handling requires careful parsing
- HTTPS interception needs proper certificate management
- Performance requires async patterns and efficient memory usage
- Security requires proper certificate validation and trust management
- Extensibility requires interface-based design and dependency injection

### Recreation Guide
The patterns and implementations in this library can be recreated in other languages and frameworks by following these key principles:

1. **Interface-Based Design**: Define clear contracts for all major components
2. **Async Processing**: Use non-blocking I/O throughout the system
3. **Certificate Management**: Implement proper CA certificate generation and per-host certificate creation
4. **Interceptor Pattern**: Enable extensible traffic modification through chain of responsibility
5. **Flow Tracking**: Maintain comprehensive transaction state for monitoring and debugging
6. **Thread Safety**: Ensure all shared components are properly synchronized
7. **Dependency Injection**: Enable testability and component replacement
8. **Comprehensive Logging**: Provide visibility into all system operations

This architecture provides a solid foundation for building sophisticated network proxy and traffic interception systems in any modern programming environment.
