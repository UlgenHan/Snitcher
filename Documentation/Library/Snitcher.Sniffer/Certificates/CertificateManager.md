# CertificateManager

## Overview
The CertificateManager class is a comprehensive certificate authority and management system responsible for generating, storing, and managing X.509 certificates required for HTTPS traffic interception. It implements a complete CA infrastructure including root certificate generation, per-host certificate creation, certificate caching, and trust store management.

**Why it exists:** To enable HTTPS traffic interception by acting as a certificate authority that can generate trusted certificates for any hostname on demand.

**Problem it solves:** HTTPS traffic cannot be intercepted without valid certificates. This component solves the "man-in-the-middle" certificate problem by creating a CA certificate and generating per-host certificates signed by that CA.

**What would break if removed:** HTTPS interception would be impossible. All HTTPS traffic would fail due to certificate validation errors, making the proxy unable to monitor secure connections.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Security.Cryptography.X509Certificates (X.509 certificate handling)
  - System.Security.Cryptography (RSA, certificate generation)
  - System.IO (file operations for certificate persistence)
- **Persistence/communication:** File system (PFX files), Windows Certificate Store
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** Windows certificate store access, file system permissions
- **Version hints:** Uses modern certificate APIs, async patterns, proper key storage flags

## Architectural Role
- **Layer:** Infrastructure/Security layer
- **Responsibility boundaries:**
  - MUST: Generate CA certificate, create per-host certificates, manage trust store
  - MUST NOT: Handle network connections, parse HTTP traffic, implement business logic
- **Dependencies:**
  - **Incoming:** ILogger (for debugging and monitoring)
  - **Outgoing:** X.509 certificates, file system operations, certificate store modifications

## Execution Flow
**CA Certificate Management:**
1. GetOrCreateCACertificateAsync checks for existing CA certificate
2. Loads from "mitmproxy-ca.pfx" if exists, or generates new CA certificate
3. Stores CA certificate in memory with proper key storage flags
4. Persists to file system for future use

**Per-Host Certificate Generation:**
1. GetCertificateForHostAsync ensures CA certificate is loaded
2. GenerateCertificateForHostAsync checks cache for existing certificate
3. Creates new RSA key pair for host certificate
4. Generates certificate request with Subject Alternative Name
5. Signs certificate with CA private key
6. Caches certificate for future use

**Trust Store Management:**
1. IsCACertificateTrusted checks Current User Root store for CA certificate
2. InstallCACertificateAsync adds CA certificate to trusted root store

**Synchronous vs asynchronous:** Public methods are async but use lock statements for thread safety.

**Threading notes:** Uses lock statements to prevent concurrent certificate generation. Thread-safe for concurrent reads.

**Lifecycle:** Created per dependency injection → CA certificate loaded on first use → Host certificates generated on demand → Cached for duration of application

## Public API / Surface Area
**Constructors:**
- `CertificateManager(ILogger logger)` - Creates manager with logging capability

**Public Methods:**
- `Task<X509Certificate2> GetOrCreateCACertificateAsync(string password, CancellationToken cancellationToken = default)` - Loads or creates CA certificate
- `Task<X509Certificate2> GetCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default)` - Gets cached or creates new certificate for hostname
- `Task<X509Certificate2> GenerateCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default)` - Forces generation of new certificate for hostname
- `bool IsCACertificateTrusted()` - Checks if CA certificate is in trusted root store
- `Task InstallCACertificateAsync(string password, CancellationToken cancellationToken = default)` - Installs CA certificate to trusted root store

**Expected input/output:**
- Input: Hostnames, passwords for certificate protection
- Output: X509Certificate2 objects with private keys
- Trust status boolean for installation checks

**Side effects:** Modifies Windows certificate store, writes certificate files to disk, maintains in-memory certificate cache

**Error behavior:** Throws InvalidOperationException for missing CA certificates, propagates certificate store exceptions, validates certificate requirements

## Internal Logic Breakdown
**Lines 14-17 (Constructor):**
- Validates and injects ILogger dependency
- Initializes certificate cache dictionary and lock object
- Sets CA certificate to null (lazy loading)

**Lines 19-44 (GetOrCreateCACertificateAsync):**
- Double-checked locking pattern for thread safety
- Attempts to load existing CA certificate from "mitmproxy-ca.pfx"
- Uses proper X509KeyStorageFlags for machine-level key persistence
- Generates new CA certificate if file doesn't exist
- Persists generated CA certificate to file system
- Logs certificate loading/generation progress

**Lines 46-55 (GetCertificateForHostAsync):**
- Ensures CA certificate is loaded before host certificate generation
- Delegates to GenerateCertificateForHostAsync for actual certificate creation
- Uses default password "mitmproxy" for CA certificate access

**Lines 57-104 (GenerateCertificateForHostAsync):**
- Thread-safe certificate generation with locking
- Checks certificate cache before generating new certificates
- Creates 2048-bit RSA key for host certificate
- Builds certificate request with SHA256 hashing
- Adds Subject Alternative Name extension for hostname validation
- Creates certificate signed by CA with 1-year validity
- Attaches private key and makes certificate exportable
- Caches generated certificate for future requests
- Includes comprehensive logging of generation process

**Lines 106-121 (IsCACertificateTrusted):**
- Opens Current User Root certificate store in read-only mode
- Searches for certificate by subject name "MITMProxy CA"
- Returns boolean indicating trust status
- Includes error handling for certificate store access issues

**Lines 123-148 (InstallCACertificateAsync):**
- Ensures CA certificate exists before installation
- Opens Current User Root certificate store with read-write access
- Checks for existing installation to avoid duplicates
- Adds CA certificate to trusted root store
- Logs installation success or failure

**Lines 150-179 (GenerateCACertificate):**
- Creates 4096-bit RSA key for CA certificate (stronger than host certificates)
- Builds self-signed certificate request with CA-specific extensions
- Adds Basic Constraints extension marking it as a CA certificate
- Adds Key Usage extension for signing certificates
- Creates self-signed certificate with 10-year validity
- Exports with proper key storage flags for machine-level persistence

## Patterns & Principles Used
**Design Patterns:**
- **Singleton Pattern:** Single CA certificate instance per application
- **Factory Pattern:** Creates certificates on demand for different hostnames
- **Cache Pattern:** Stores generated certificates to avoid regeneration
- **Double-Checked Locking:** Thread-safe lazy initialization

**Architectural Patterns:**
- **Certificate Authority Pattern:** Implements full CA hierarchy
- **Repository Pattern:** Manages certificate persistence and retrieval

**Why these patterns were chosen:**
- Singleton ensures consistent CA certificate across the application
- Factory pattern enables flexible certificate generation for different hosts
- Cache pattern improves performance for repeated host connections
- Double-checked locking provides thread safety without excessive locking

**Trade-offs:**
- File-based persistence may fail on read-only file systems
- Windows certificate store integration limits cross-platform compatibility
- In-memory cache may consume memory for many unique hostnames

**Anti-patterns avoided:**
- No static global state (instance-based design)
- No certificate generation without proper validation
- No insecure key storage practices

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of ILogger for debugging
- No other external dependencies

**Configuration Sources:**
- Hardcoded CA certificate filename "mitmproxy-ca.pfx"
- Default CA password "mitmproxy"
- Fixed certificate validity periods (10 years for CA, 1 year for host)
- Fixed RSA key sizes (4096-bit for CA, 2048-bit for host)

**Runtime Wiring:**
- Certificate cache populated on-demand
- CA certificate loaded lazily on first use
- No dynamic configuration changes during runtime

**Registration Points:**
- Should be registered as ICertificateManager in DI container
- Singleton lifetime appropriate (shared CA certificate)
- Used by SSL interception components for certificate provisioning

## Example Usage
**Minimal Example:**
```csharp
var logger = new ConsoleLogger();
var certManager = new CertificateManager(logger);

// Load or create CA certificate
var caCert = await certManager.GetOrCreateCACertificateAsync("password");

// Get certificate for specific host
var hostCert = await certManager.GetCertificateForHostAsync("example.com");

// Check if CA is trusted
var isTrusted = certManager.IsCACertificateTrusted();
```

**Realistic Example with SSL Setup:**
```csharp
public class SslInterceptor
{
    private readonly ICertificateManager _certManager;
    
    public SslInterceptor(ICertificateManager certManager)
    {
        _certManager = certManager;
    }
    
    public async Task<SslStream> CreateSslStreamAsync(TcpClient client, string hostname)
    {
        var clientStream = client.GetStream();
        
        // Get certificate for the target hostname
        var certificate = await _certManager.GetCertificateForHostAsync(hostname);
        
        // Create SSL stream with our certificate
        var sslStream = new SslStream(clientStream, false);
        await sslStream.AuthenticateAsServerAsync(certificate, 
            clientCertificateRequired: false, 
            enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
            checkCertificateRevocation: false);
        
        return sslStream;
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Using certificates without checking CA first
var hostCert = await certManager.GenerateCertificateForHostAsync("example.com");
// This will throw if CA certificate is not loaded

// WRONG: Not handling certificate store permissions
await certManager.InstallCACertificateAsync("password");
// May fail without admin privileges

// WRONG: Sharing certificates across threads without proper synchronization
var cert = certManager.GetOrCreateCACertificateAsync("password");
// Should await the async call properly
```

**How to test in isolation:**
```csharp
[Test]
public async Task GenerateCertificateForHost_ShouldCreateValidCertificate()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var certManager = new CertificateManager(logger.Object);
    
    // Act
    var caCert = await certManager.GetOrCreateCACertificateAsync("testpass");
    var hostCert = await certManager.GenerateCertificateForHostAsync("test.example.com");
    
    // Assert
    Assert.That(hostCert.Subject, Does.Contain("test.example.com"));
    Assert.That(hostCert.Issuer, Is.EqualTo(caCert.Subject));
    Assert.That(hostCert.NotBefore, Is.LessThanOrEqualTo(DateTime.UtcNow));
    Assert.That(hostCert.NotAfter, Is.GreaterThan(DateTime.UtcNow.AddMonths(11)));
    Assert.That(hostCert.HasPrivateKey, Is.True);
}
```

**How to mock or replace it:**
```csharp
public class MockCertificateManager : ICertificateManager
{
    private readonly Dictionary<string, X509Certificate2> _certs = new();
    
    public Task<X509Certificate2> GetOrCreateCACertificateAsync(string password, CancellationToken cancellationToken = default)
    {
        var caCert = CreateSelfSignedCertificate("Mock CA");
        return Task.FromResult(caCert);
    }
    
    public Task<X509Certificate2> GetCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default)
    {
        if (!_certs.TryGetValue(hostname, out var cert))
        {
            cert = CreateSelfSignedCertificate(hostname);
            _certs[hostname] = cert;
        }
        return Task.FromResult(cert);
    }
    
    public Task<X509Certificate2> GenerateCertificateForHostAsync(string hostname, CancellationToken cancellationToken = default)
    {
        var cert = CreateSelfSignedCertificate(hostname);
        _certs[hostname] = cert;
        return Task.FromResult(cert);
    }
    
    public bool IsCACertificateTrusted() => true;
    
    public Task InstallCACertificateAsync(string password, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    private X509Certificate2 CreateSelfSignedCertificate(string subject)
    {
        // Create mock certificate for testing
        return CertificateGenerator.CreateSelfSignedCertificate(subject);
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **Custom certificate templates:** Add methods for specific certificate types
- **Certificate revocation:** Add CRL generation and checking
- **Different key types:** Add ECDSA certificate generation options
- **Certificate validation:** Add custom validation logic

**Where NOT to add logic:**
- Don't add network connection handling (belongs in connection managers)
- Don't add HTTP traffic processing (belongs in HTTP parsers)
- Don't add business logic for certificate usage decisions

**Safe extension points:**
- New certificate generation methods following existing patterns
- Additional certificate extensions in generation methods
- Custom validation methods for certificate properties

**Common mistakes:**
- Changing certificate storage flags (breaks private key access)
- Modifying CA certificate after generation (breaks certificate chain)
- Not properly disposing certificate stores
- Ignoring thread safety in certificate generation

**Refactoring warnings:**
- Changing CA certificate filename will break existing installations
- Modifying certificate validity periods may affect client trust
- Removing certificate caching will impact performance significantly
- Changing password handling will break certificate loading

## Failure Modes & Debugging
**Common runtime errors:**
- `CryptographicException`: Certificate generation or loading failures
- `UnauthorizedAccessException`: Insufficient permissions for certificate store
- `InvalidOperationException`: CA certificate not loaded when required
- `FileNotFoundException`: CA certificate file missing or inaccessible

**Null/reference risks:**
- CA certificate null-checked before use in host certificate generation
- Logger validated in constructor
- Certificate cache initialized with empty dictionary

**Performance risks:**
- Large number of unique hostnames may consume significant memory
- Certificate generation is CPU-intensive for RSA key creation
- File I/O for certificate persistence may be slow under load
- Certificate store operations may be slow on some systems

**Logging points:**
- CA certificate loading and generation progress
- Host certificate generation requests
- Certificate caching hits/misses
- Trust store installation status
- Error conditions with full exception details

**How to debug step-by-step:**
1. Check CA certificate loading in GetOrCreateCACertificateAsync
2. Verify certificate file permissions and existence
3. Monitor certificate generation for specific hostnames
4. Check certificate cache behavior for repeated requests
5. Verify trust store installation with Windows certificate manager
6. Test certificate validation with SSL clients
7. Use certificate viewer tools to inspect generated certificates

## Cross-References
**Related classes:**
- `ICertificateManager` - Interface defining certificate management contract
- `CertificateGenerator` - Static utility methods for certificate creation
- `CertificateExtensions` - Extension methods for certificate information
- `CertificateInfo` - Model representing certificate metadata

**Upstream callers:**
- `SslInterceptor` - Uses certificates for HTTPS traffic interception
- `ConnectionHandler` - May use certificates for SSL termination
- Test fixtures - Direct calls for certificate management testing

**Downstream dependencies:**
- Windows Certificate Store for trust management
- File system for certificate persistence
- Cryptographic APIs for certificate generation

**Documents that should be read before/after:**
- Before: ICertificateManager interface documentation
- Before: CertificateGenerator utility documentation
- After: SslInterceptor documentation (certificate usage)
- Related: CertificateExtensions documentation (certificate inspection)

## Knowledge Transfer Notes
**Reusable concepts:**
- X.509 certificate authority implementation
- Certificate caching and lifecycle management
- Windows certificate store integration
- RSA key generation and management
- Subject Alternative Name handling for modern certificates

**Project-specific elements:**
- Snitcher's specific CA certificate naming ("MITMProxy CA")
- Integration with Snitcher's HTTPS interception workflow
- Specific key storage flags for machine-level certificate access
- Default password and filename conventions

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for certificate management operations
2. Implement CA certificate generation with proper extensions
3. Add certificate caching mechanism with thread safety
4. Implement per-host certificate generation with SAN support
5. Add certificate persistence to file system
6. Integrate with platform certificate store for trust management
7. Include comprehensive error handling and logging
8. Add certificate validation and inspection capabilities

**Key architectural insights:**
- Certificate authority pattern enables scalable HTTPS interception
- Proper key storage flags are essential for certificate accessibility
- Certificate caching significantly improves performance for repeated connections
- Trust store integration is critical for seamless HTTPS interception
- Thread safety is essential when dealing with cryptographic operations
