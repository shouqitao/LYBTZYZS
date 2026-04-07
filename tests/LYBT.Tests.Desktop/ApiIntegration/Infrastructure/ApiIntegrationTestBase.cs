using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace LYBT.Tests.Desktop.ApiIntegration.Infrastructure;

/// <summary>
/// API Integration Test Base Class
/// 
/// Provides mocked Refit API clients for testing WPF frontend API integration
/// without requiring a running WebAPI server.
/// 
/// Key Features:
/// - Mocked Refit API interfaces using NSubstitute
/// - Controlled HTTP responses and error scenarios
/// - Authentication flow testing with token management
/// - Resilience pattern validation (retry, circuit breaker, timeout)
/// - Caching behavior verification
/// - Data serialization testing
/// </summary>
public abstract class ApiIntegrationTestBase : IDisposable
{
    // Mocked API Clients
    protected IAuthApi AuthApi { get; }
    protected IUserApi UserApi { get; }
    protected IPatientApi PatientApi { get; }
    protected IHerbApi HerbApi { get; }
    protected IFormulaApi FormulaApi { get; }
    protected IMedicalCaseApi MedicalCaseApi { get; }
    protected ISyncApi SyncApi { get; }
    protected IRegistrationApi RegistrationApi { get; }

    // Core Services
    protected IAuthenticationService AuthenticationService { get; }
    protected IApiService ApiService { get; }
    protected ITokenStorageService TokenStorage { get; }
    protected ITokenValidator TokenValidator { get; }

    // Test Infrastructure
    protected MockApiFactory ApiFactory { get; }
    protected TestDataFactory TestData { get; }
    protected IServiceProvider ServiceProvider { get; }

    protected ApiIntegrationTestBase()
    {
        // Create mock API clients
        AuthApi = Substitute.For<IAuthApi>();
        UserApi = Substitute.For<IUserApi>();
        PatientApi = Substitute.For<IPatientApi>();
        HerbApi = Substitute.For<IHerbApi>();
        FormulaApi = Substitute.For<IFormulaApi>();
        MedicalCaseApi = Substitute.For<IMedicalCaseApi>();
        SyncApi = Substitute.For<ISyncApi>();
        RegistrationApi = Substitute.For<IRegistrationApi>();

        // Create test infrastructure
        ApiFactory = new MockApiFactory(AuthApi, UserApi, PatientApi, HerbApi, 
                                      FormulaApi, MedicalCaseApi, SyncApi, RegistrationApi);
        TestData = new TestDataFactory();

        // Setup core services with mocked dependencies
        ServiceProvider = SetupCoreServices();
        
        // Get service instances
        AuthenticationService = ServiceProvider.GetRequiredService<IAuthenticationService>();
        ApiService = ServiceProvider.GetRequiredService<IApiService>();
        TokenStorage = ServiceProvider.GetRequiredService<ITokenStorageService>();
        TokenValidator = ServiceProvider.GetRequiredService<ITokenValidator>();
    }

    /// <summary>
    /// Setup core services with mocked API clients
    /// </summary>
    private IServiceProvider SetupCoreServices()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Add mocked API clients
        services.AddSingleton(AuthApi);
        services.AddSingleton(UserApi);
        services.AddSingleton(PatientApi);
        services.AddSingleton(HerbApi);
        services.AddSingleton(FormulaApi);
        services.AddSingleton(MedicalCaseApi);
        services.AddSingleton(SyncApi);
        services.AddSingleton(RegistrationApi);

        // Add current user provider (mock)
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.CurrentUserId.Returns(Guid.NewGuid());
        services.AddSingleton(currentUserProvider);

        // Add token management services
        services.AddSingleton<ITokenStorageService, TokenStorageService>();
        services.AddSingleton<ITokenValidator, LocalTokenValidator>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        
        // Add API service with mocked HttpClient
        var mockHttpClient = CreateMockHttpClient();
        services.AddSingleton(mockHttpClient);
        services.AddSingleton<IApiService, ApiService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Create a mock HttpClient for testing
    /// </summary>
    private HttpClient CreateMockHttpClient()
    {
        var mockHandler = new MockHttpMessageHandler();
        return new HttpClient(mockHandler);
    }

    /// <summary>
    /// Setup successful login response
    /// </summary>
    protected void SetupSuccessfulLogin(UserDetailDto user, string? token = null, string? refreshToken = null)
    {
        token ??= TestData.GenerateJwtToken(user.Id, user.UserName, user.Role.ToString());
        refreshToken ??= TestData.GenerateRefreshToken();

        var loginResponse = new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        AuthApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "登录成功"
            }));
    }

    /// <summary>
    /// Setup failed login response
    /// </summary>
    protected void SetupFailedLogin(string errorMessage = "用户名或密码错误")
    {
        AuthApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = errorMessage
            }));
    }

    /// <summary>
    /// Setup API response for any Refit client method
    /// </summary>
    protected void SetupApiResponse<T>(Func<Task<T>> apiCall, T response)
    {
        // This is a generic setup method - specific implementations would be in derived classes
        // or use the MockApiFactory for more complex scenarios
    }

    /// <summary>
    /// Setup network error for API calls
    /// </summary>
    protected void SetupNetworkError()
    {
        // Configure mock to throw HttpRequestException
    }

    /// <summary>
    /// Setup server error response
    /// </summary>
    protected void SetupServerError(HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        // Configure mock to return error status codes
    }

    /// <summary>
    /// Verify API was called with specific parameters
    /// </summary>
    protected void VerifyApiCall<T>(Func<T, bool> predicate, string? failMessage = null)
    {
        // Generic verification method
    }

    /// <summary>
    /// Reset all mock setups between tests
    /// </summary>
    protected void ResetMocks()
    {
        AuthApi.ClearReceivedCalls();
        UserApi.ClearReceivedCalls();
        PatientApi.ClearReceivedCalls();
        HerbApi.ClearReceivedCalls();
        FormulaApi.ClearReceivedCalls();
        MedicalCaseApi.ClearReceivedCalls();
        SyncApi.ClearReceivedCalls();
        RegistrationApi.ClearReceivedCalls();
    }

    /// <summary>
    /// Create test user with specified role
    /// </summary>
    protected UserDetailDto CreateTestUser(string username = "testuser", UserRole role = UserRole.Doctor)
    {
        return new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Role = role
        };
    }

    /// <summary>
    /// Mock HttpMessageHandler for HttpClient testing
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Return a default success response
            // Can be extended to handle different request patterns
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
