# WPF Frontend API Integration Tests

This directory contains integration tests for the WPF frontend's API communication layer, designed to test the complete integration between the desktop application and backend APIs without requiring a running server.

## Overview

The API integration tests use mocked Refit clients to simulate backend API responses, allowing comprehensive testing of:

- **HTTP Client Setup**: Handler chains, base URLs, timeouts, SSL configuration
- **API Service Classes**: Refit client registration, DI injection, service resolution
- **Authentication**: Login flows, token management, refresh, authorization headers
- **Request Patterns**: GET/POST/PUT/DELETE, query parameters, request bodies
- **Error Handling**: Network errors, server errors (4xx/5xx), auth failures
- **Data Serialization**: CommandResult<T>, ApiResponse<T>, JSON handling
- **Resilience**: Retry policies, circuit breakers, timeouts (Polly)
- **Caching**: Request deduplication, cache hits/misses, invalidation
- **Real-time**: N/A (REST-only architecture)

## Architecture

`
ApiIntegrationTestBase (inherits UserJourneyTestBase)
├── Mocked Refit API Clients (IAuthApi, IUserApi, etc.)
├── MockApiFactory (fluent response setup)
├── TestDataFactory (test data generation)
├── Core Services (AuthenticationService, ApiService, etc.)
└── Test Scenarios (Authentication, Error Handling, Caching)
`

## Key Components

### ApiIntegrationTestBase

Base class providing:
- Mocked Refit API interfaces using NSubstitute
- Pre-configured DI container with test services
- Helper methods for common test setups
- Automatic mock cleanup between tests

### MockApiFactory

Fluent API for configuring mock responses:

`csharp
// Setup successful login
ApiFactory.WithSuccessfulLogin(loginResponse);

// Setup API error
ApiFactory.Users.FailsToGetUsers("Server error");

// Setup network failure
ApiFactory.WithNetworkError(api => api.GetUsersAsync);
`

### TestDataFactory

Generates test data and tokens:

`csharp
// Generate JWT tokens
var token = TestData.GenerateJwtToken(userId, username, role);

// Create test users
var doctor = TestData.CreateDoctor("dr_smith");

// Create API responses
var response = TestData.CreateSuccessResponse(user, "Created successfully");
`

## Usage Examples

### Basic Authentication Test

`csharp
public class MyAuthTests : ApiIntegrationTestBase
{
    public MyAuthTests(UserJourneyFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Login_Success_StoresToken()
    {
        // Arrange
        var testUser = CreateTestUser("doctor", UserRole.Doctor);
        SetupSuccessfulLogin(testUser);

        // Act
        var result = await AuthenticationService.LoginAsync(
            new LoginRequest { UserName = "doctor", Password = "pass" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await TokenStorage.GetLoginResponseAsync();
        stored.Should().NotBeNull();
    }
}
`

### Error Handling Test

`csharp
[Fact]
public async Task ApiCall_NetworkError_HandlesGracefully()
{
    // Arrange
    ApiFactory.WithNetworkError(api => api.GetUsersAsync);

    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(
        () => ApiService.GetUsersAsync());
}
`

### Caching Test

`csharp
[Fact]
public async Task IdenticalRequests_UsesCache()
{
    // Arrange
    var callCount = 0;
    UserApi.GetUsersAsync(default, default, default)
        .Returns(_ => 
        {
            callCount++;
            return Task.FromResult(TestData.CreateSuccessResponse(
                TestData.CreatePagedResult(new[] { TestData.CreateUser() }, 1)));
        });

    // Act - Multiple identical calls
    await ApiService.GetUsersAsync();
    await ApiService.GetUsersAsync();

    // Assert - Only one API call made
    callCount.Should().Be(1);
}
`

## Running Tests

### Run All API Integration Tests

`ash
dotnet test tests/LYBT.Tests.Desktop --filter "Category=ApiIntegration"
`

### Run Specific Test Categories

`ash
# Authentication tests
dotnet test tests/LYBT.Tests.Desktop --filter "Category=ApiIntegration AND Phase=Authentication"

# Error handling tests
dotnet test tests/LYBT.Tests.Desktop --filter "Category=ApiIntegration AND Phase=ErrorHandling"

# Caching tests
dotnet test tests/LYBT.Tests.Desktop --filter "Category=ApiIntegration AND Phase=Caching"
`

## Test Categories and Traits

All tests are marked with traits for filtering:

`csharp
[Trait("Category", "ApiIntegration")]
[Trait("Phase", "Authentication")]     // Login, token management
[Trait("Phase", "ErrorHandling")]      // Network/server errors
[Trait("Phase", "Caching")]           // Cache behavior
[Trait("Phase", "Serialization")]     // Data handling
[Trait("Phase", "Resilience")]        // Polly policies
`

## Extending the Framework

### Adding New API Mocks

1. Add the API interface to ApiIntegrationTestBase
2. Create a configurator in MockApiFactory
3. Add setup methods for common scenarios

### Creating New Test Scenarios

1. Create a new test class inheriting from ApiIntegrationTestBase
2. Use appropriate traits for categorization
3. Follow the Arrange-Act-Assert pattern
4. Reset mocks between tests if needed

### Custom Test Data

Extend TestDataFactory with domain-specific data generators:

`csharp
public MedicalCaseTestData CreateMedicalCaseData()
{
    return new MedicalCaseTestData
    {
        Case = CreateMedicalCase(),
        Consultation = CreateConsultation(),
        Prescription = CreatePrescription()
    };
}
`

## Best Practices

### Test Isolation
- Each test gets fresh mock instances
- Use ResetMocks() if cross-test state is needed
- Avoid sharing state between tests

### Mock Setup
- Use specific argument matchers (Arg.Any<T>(), Arg.Is<T>(...))
- Verify calls with Received() and DidNotReceive()
- Setup responses before calling services

### Assertions
- Use FluentAssertions for readable assertions
- Verify both success and error paths
- Check API call counts for caching tests

### Performance
- Keep test data generation lightweight
- Use async operations appropriately
- Avoid unnecessary mock setups

## Troubleshooting

### Common Issues

**Mock not working**: Ensure the mock is set up before the service call. Check argument matchers.

**DI resolution fails**: Verify all required services are registered in the test container.

**Token validation fails**: Check JWT generation parameters match the expected issuer/audience.

**Caching not working**: Ensure identical parameters and proper cache key generation.

### Debug Tips

- Use mock.ReceivedCalls() to inspect actual calls
- Add logging to see request/response flow
- Use breakpoints in test infrastructure code

## Integration with CI/CD

These tests are designed to run in CI/CD pipelines:

- No external dependencies (mocked APIs)
- Fast execution (no network calls)
- Deterministic results
- Comprehensive coverage of integration points

## Comparison with E2E Tests

| Aspect | API Integration Tests | E2E Tests (WebApiE2ETestBase) |
|--------|----------------------|--------------------------------|
| **Speed** | Fast (no network) | Slower (real HTTP calls) |
| **Dependencies** | None | Running WebAPI server |
| **Coverage** | Unit integration | Full stack integration |
| **Isolation** | Complete (mocks) | Partial (real database) |
| **Debugging** | Easy (controlled responses) | Harder (server state) |
| **CI/CD** | Always runnable | Requires server setup |

Use API integration tests for:
- Fast feedback during development
- Testing error scenarios and edge cases
- Validating integration patterns
- CI/CD pipelines

Use E2E tests for:
- End-to-end validation
- Real API contract verification
- Performance testing
- Deployment verification
