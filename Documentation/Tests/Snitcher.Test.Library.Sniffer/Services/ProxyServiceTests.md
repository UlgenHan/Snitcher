# ProxyServiceTests

## Overview

ProxyServiceTests is the unit test suite for the ProxyService class, providing comprehensive testing of proxy server functionality including startup, shutdown, event handling, and error scenarios. This test class ensures the reliability and correctness of the proxy service operations that are critical for the Snitcher application's network interception capabilities.

**Why it exists**: To provide automated verification of proxy service behavior, catch regressions, and ensure that the proxy functionality works correctly across different scenarios including normal operation, error conditions, and edge cases.

**Problem it solves**: Without these tests, proxy service bugs could go undetected until runtime, potentially causing application crashes or data loss. The tests provide a safety net for code changes and ensure reliable proxy operation.

**What would break if removed**: The automated test coverage for proxy service would be lost, increasing the risk of undetected bugs in network interception functionality and reducing confidence in code changes.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0, NUnit 3.0, Moq 4.20

**Libraries**: Microsoft.Extensions.Logging, Snitcher.UI.Desktop, Snitcher.Sniffer

**UI frameworks**: N/A (testing infrastructure)

**Persistence / communication technologies**: None (pure unit testing)

**Build tools**: MSBuild, NUnit test runner

**Runtime assumptions**: .NET 8.0 runtime, testing framework available

**Version hints**: Uses modern NUnit patterns, Moq for mocking, async test patterns

## Architectural Role

**Layer**: Testing Layer (Unit Tests)

**Responsibility boundaries**:
- MUST test proxy service public API behavior
- MUST verify event handling and state management
- MUST test error scenarios and edge cases
- MUST NOT test implementation details
- MUST NOT depend on external resources

**What it MUST do**:
- Test proxy service lifecycle (start/stop)
- Verify event firing and handling
- Test error conditions and exceptions
- Ensure proper resource cleanup
- Validate state transitions

**What it MUST NOT do**:
- Test private methods or internal implementation
- Depend on actual network resources
- Test integration with real proxy servers
- Include performance or load testing

**Dependencies (incoming)**: Test runners, CI/CD pipelines

**Dependencies (outgoing)**: ProxyService, mocking frameworks, assertion libraries

## Execution Flow

**Where execution starts**: Tests are executed by test runner when running the test suite.

**How control reaches this component**:
1. Test runner discovers test class
2. Setup() method called before each test
3. Individual test methods executed
4. TearDown() method called after each test
5. Test results reported to runner

**Call sequence (step-by-step)**:
1. Test runner creates test class instance
2. Setup() initializes mocks and service
3. Test method executes test scenario
4. Assertions verify expected behavior
5. TearDown() cleans up resources
6. Test result recorded

**Synchronous vs asynchronous behavior**: Mix of sync and async tests based on tested functionality

**Threading / dispatcher / event loop notes**: Tests run on test runner threads, async tests properly awaited

**Lifecycle**: Per-test lifecycle with Setup/Teardown for isolation

## Public API / Surface Area

**Test Fixtures**:
- `[TestFixture] public class ProxyServiceTests`: Main test class

**Setup Methods**:
- `[SetUp] public void Setup()`: Initializes test dependencies
- `[TearDown] public async Task TearDown()`: Cleans up after tests

**Test Methods**:
- `ProxyService_InitializesWithCorrectState()`: Tests initial state
- `ProxyService_CanStartAndStop()`: Tests basic lifecycle
- `ProxyService_EventsWorkCorrectly()`: Tests event handling
- `ProxyService_InvalidPortThrowsException()`: Tests error handling

**Expected input/output**:
- Input: Test scenarios and mock configurations
- Output: Test pass/fail results with assertions

**Side effects**: Starts/stops proxy service during tests, creates mock objects

**Error behavior**: Tests fail when assertions don't match expected behavior, proper exception handling tested

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Class and fixture declaration (lines 12-13)**:
```csharp
[TestFixture]
public class ProxyServiceTests
{
```
- Marks class as NUnit test fixture
- Enables test discovery and execution

**Mock declarations (lines 15-18)**:
```csharp
private Mock<ILogger<ProxyService>> _mockLogger;
private Mock<ICertificateManager> _mockCertificateManager;
private Mock<Snitcher.Sniffer.Core.Interfaces.ILogger> _mockSnifferLogger;
private IProxyService _proxyService;
```
- Declares mock objects for all dependencies
- Enables isolated testing without external dependencies
- Service under test declared as interface for testability

**Setup method (lines 20-27)**:
```csharp
[SetUp]
public void Setup()
{
    _mockLogger = new Mock<ILogger<ProxyService>>();
    _mockCertificateManager = new Mock<ICertificateManager>();
    _mockSnifferLogger = new Mock<Snitcher.Sniffer.Core.Interfaces.ILogger>();
    _proxyService = new ProxyService(_mockLogger.Object, _mockCertificateManager.Object, _mockSnifferLogger.Object);
}
```
- Runs before each test method
- Creates fresh mock instances for test isolation
- Initializes service under test with mocked dependencies
- Ensures tests don't interfere with each other

**Initial state test (lines 29-34)**:
```csharp
[Test]
public void ProxyService_InitializesWithCorrectState()
{
    // Assert
    Assert.That(_proxyService.IsRunning, Is.False);
}
```
- Tests that service starts in correct initial state
- Verifies IsRunning property is false initially
- Simple state validation test

**Lifecycle test (lines 36-49)**:
```csharp
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
```
- Tests complete proxy service lifecycle
- Sets up event handler to ensure no exceptions
- Verifies service can start without throwing
- Checks state changes during start/stop
- Ensures service can stop without throwing

**Event handling test (lines 51-67)**:
```csharp
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
```
- Tests that events fire correctly
- Sets up event handlers to capture event data
- Starts proxy service and waits for events
- Verifies status change event contains expected content
- Note: ErrorOccurred handler sets error to null (may be bug)

**Error handling test (lines 69-74)**:
```csharp
[Test]
public void ProxyService_InvalidPortThrowsException()
{
    // Act & Assert
    Assert.ThrowsAsync<Exception>(async () => await _proxyService.StartAsync(80));
}
```
- Tests error handling for invalid input
- Uses port 80 (likely requires admin privileges)
- Verifies appropriate exception is thrown
- Ensures proper validation of input parameters

**TearDown method (lines 76-83)**:
```csharp
[TearDown]
public async Task TearDown()
{
    if (_proxyService.IsRunning)
    {
        await _proxyService.StopAsync();
    }
}
```
- Runs after each test method
- Ensures proxy service is stopped after tests
- Prevents resource leaks between tests
- Handles cleanup gracefully

**Algorithms used**:
- Mock object creation and configuration
- Async/await patterns for asynchronous testing
- Event handler setup and verification
- Assertion-based validation

**Conditional logic explanation**:
- Setup/Teardown ensure test isolation
- Event handlers capture data for assertion
- Conditional cleanup based on service state
- Timeout handling for async operations

**State transitions**:
- Test Created → Setup Executed → Test Run → Assertions Verified → TearDown Executed

**Important invariants**:
- Each test runs with fresh mock objects
- Service is cleaned up after each test
- Tests verify both positive and negative scenarios
- Async operations properly awaited

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Test Fixture Pattern**: Organized test class structure
- **Mock Object Pattern**: Isolated testing with mocked dependencies
- **Setup-Teardown Pattern**: Consistent test environment
- **Assertion Pattern**: Clear verification of expected behavior

**Testing patterns**:
- **Arrange-Act-Assert Pattern**: Clear test structure
- **Given-When-Then Pattern**: Behavior-driven testing approach
- **Edge Case Testing**: Testing error conditions and boundaries

**Why these patterns were chosen (inferred)**:
- Mock objects enable isolated unit testing
- Setup-Teardown ensures test consistency
- AAA pattern makes tests readable and maintainable
- Edge case testing ensures robustness

**Trade-offs**:
- Mock vs real dependencies: More isolated but less realistic
- Unit vs integration testing: Faster but less comprehensive
- Simple assertions vs custom matchers: Simpler but less expressive

**Anti-patterns avoided or possibly introduced**:
- Avoided: Test interdependence
- Avoided: Hard-coded test data
- Possible risk: Tests may be too simplistic

## Binding / Wiring / Configuration

**Test framework**: NUnit configuration through attributes

**Mocking framework**: Moq configuration in Setup method

**Assertion library**: NUnit assertions for verification

**Configuration sources**: Test attributes and method parameters

**Runtime wiring**: Test runner automatically discovers and executes tests

## Example Usage (CRITICAL)

**Running individual tests**:
```bash
# Run specific test
dotnet test --filter "ProxyService_InitializesWithCorrectState"

# Run all tests in class
dotnet test --filter "ProxyServiceTests"
```

**Test execution example**:
```csharp
// Tests are automatically discovered and run by test runner
// No manual execution required - just run the test project
```

**Debugging tests**:
```csharp
// Set breakpoint in test method
// Run tests in debug mode from IDE
// Step through test execution to verify behavior
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Tests depending on each other
[Test]
public void Test1()
{
    _proxyService.StartAsync(8080).Wait();
    // Service left running for next test
}

[Test]
public void Test2()
{
    // This test may fail because service is already running
    Assert.That(_proxyService.IsRunning, Is.False);
}

// WRONG: Not cleaning up resources
[Test]
public void StartProxyTest()
{
    _proxyService.StartAsync(8080).Wait();
    // No cleanup - resource leak
}

// WRONG: Testing implementation details
[Test]
public void InternalMethodTest()
{
    // Don't test private methods - test public behavior
    // var result = _proxyService.InternalMethod(); // WRONG
}
```

**How to extend these tests**:
```csharp
[Test]
public void ProxyService_ShouldHandleMultipleStartStop()
{
    // Arrange
    for (int i = 0; i < 3; i++)
    {
        // Act
        Assert.DoesNotThrowAsync(async () => await _proxyService.StartAsync(8080 + i));
        Assert.That(_proxyService.IsRunning, Is.True);
        
        Assert.DoesNotThrowAsync(async () => await _proxyService.StopAsync());
        Assert.That(_proxyService.IsRunning, Is.False);
    }
    // Assert - No exceptions thrown
}

[Test]
public void ProxyService_ShouldValidatePortRange()
{
    // Test various port scenarios
    Assert.ThrowsAsync<Exception>(async () => await _proxyService.StartAsync(0));
    Assert.ThrowsAsync<Exception>(async () => await _proxyService.StartAsync(65536));
    Assert.DoesNotThrowAsync(async () => await _proxyService.StartAsync(8080));
}
```

## Extension & Modification Guide

**How to add a new test here**:
1. Add new test method with [Test] attribute
2. Follow Arrange-Act-Assert pattern
3. Use appropriate assertions for verification
4. Test both positive and negative scenarios
5. Ensure test isolation (don't depend on other tests)

**Where NOT to add logic**:
- Don't test private methods or implementation details
- Don't add integration tests that depend on external resources
- Don't include performance or load testing
- Don't test framework behavior

**Safe extension points**:
- New test methods for additional scenarios
- Additional mock configurations for edge cases
- Custom assertion helpers for common validations
- Parameterized tests for multiple scenarios

**Common mistakes**:
- Tests that depend on execution order
- Not cleaning up resources in TearDown
- Testing implementation instead of behavior
- Ignoring async/await patterns
- Hard-coded test data that may change

**Refactoring warnings**:
- Changing test method names affects test filters
- Modifying mock setup affects multiple tests
- Removing TearDown can cause resource leaks
- Adding slow tests affects CI/CD pipeline

## Failure Modes & Debugging

**Common test failures**:
- **AssertionException**: Expected behavior doesn't match actual
- **TimeoutException**: Async operations don't complete in time
- **InvalidOperationException**: Invalid test state or setup
- **MockException**: Mock configuration issues

**Debugging strategies**:
- Use debug mode to step through test execution
- Check mock configurations and setups
- Verify async operations are properly awaited
- Examine test isolation and cleanup

**Test reliability issues**:
- Race conditions in async tests
- Resource conflicts between tests
- Timing-dependent assertions
- External dependency availability

**Performance considerations**:
- Tests should run quickly (unit tests)
- Avoid unnecessary delays or waits
- Use mocks instead of real resources
- Keep test data minimal

## Cross-References

**Related classes**:
- ProxyService (class being tested)
- IProxyService (interface contract)
- Mock objects for dependencies

**Upstream callers**:
- Test runners and CI/CD pipelines
- Development workflows
- Quality assurance processes

**Downstream dependencies**:
- Assertion libraries
- Mocking frameworks
- Test reporting tools

**Documents that should be read before/after**:
- Read: ProxyService documentation
- Read: IProxyService documentation
- Read: Mock object patterns
- Read: NUnit best practices

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Unit testing patterns with mocks
- Async testing strategies
- Event testing techniques
- Resource cleanup patterns
- Test isolation principles

**What is project-specific**:
- Specific proxy service behavior being tested
- Particular mock configurations
- Snitcher-specific event handling
- Domain-specific error scenarios

**How to recreate this pattern from scratch elsewhere**:
1. Create test class with [TestFixture] attribute
2. Declare mocks for all dependencies
3. Implement [SetUp] method for test initialization
4. Write test methods following AAA pattern
5. Use [TearDown] for cleanup
6. Test both positive and negative scenarios
7. Ensure proper async/await handling
8. Use appropriate assertions for verification

**Key insights for implementation**:
- Always test public behavior, not implementation
- Use mocks to isolate units under test
- Clean up resources to prevent test interference
- Test edge cases and error conditions
- Keep tests fast and reliable
- Use descriptive test names that explain what is being tested
