using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Refit;

namespace LYBT.Tests.Desktop.Integration.Flows;

public class RealTestComposition
{
    private readonly IServiceCollection _services;
    private IServiceProvider? _serviceProvider;
    private HttpClient? _refitClient;

    public RealTestComposition()
    {
        _services = new ServiceCollection();
        RegisterDefaultServices();
    }

    public RealTestComposition WithRealRefitClient(HttpClient httpClient)
    {
        _refitClient = httpClient;
        
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            })
        };

        _services.AddSingleton<IAuthApi>(_ => RestService.For<IAuthApi>(httpClient, refitSettings));
        
        return this;
    }

    public RealTestComposition Build()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return this;
    }

    public T Resolve<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("必须先调用 Build() 方法");
        
        return _serviceProvider.GetRequiredService<T>();
    }

    public IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("必须先调用 Build() 方法");
        
        return _serviceProvider;
    }

    private void RegisterDefaultServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "your-test-secret-key-at-least-32-characters-long-for-testing",
                ["Jwt:Issuer"] = "LYBT.WebAPI",
                ["Jwt:Audience"] = "LYBT.Desktop",
                ["Jwt:ClockSkewSeconds"] = "300"
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        _services.AddSingleton<IEventAggregator, EventAggregator>();
        _services.AddSingleton<IRegionManager, RegionManager>();
        _services.AddSingleton<ISessionManager>(_ => Substitute.For<ISessionManager>());
        _services.AddSingleton<IUserNotificationService>(_ => Substitute.For<IUserNotificationService>());
        _services.AddSingleton<ICommonDialogService>(_ => Substitute.For<ICommonDialogService>());
        _services.AddSingleton<IRoleRegistry>(_ => Substitute.For<IRoleRegistry>());

        _services.AddSingleton<IViewModelServices, ViewModelServices>();
        _services.AddSingleton<IApplicationStateService>(_ => CreateMockApplicationStateService());
        _services.AddSingleton<IUsernameStorageService>(_ => Substitute.For<IUsernameStorageService>());
        _services.AddSingleton<ICredentialVault>(_ => Substitute.For<ICredentialVault>());
        _services.AddSingleton<IModeSwitchValidator>(_ => Substitute.For<IModeSwitchValidator>());

        _services.AddSingleton<ITokenStorageService, TokenStorageService>();
        _services.AddSingleton<ITokenValidator, LocalTokenValidator>();
        _services.AddSingleton<IAuthenticationService, AuthenticationService>();

        _services.AddSingleton<IAuthApi>(_ => CreateMockAuthApi());
        _services.AddSingleton<ILoginCoordinator>(_ => Substitute.For<ILoginCoordinator>());

        _services.AddTransient<LoginViewModel>();
    }

    private static IAuthApi CreateMockAuthApi()
    {
        var mock = Substitute.For<IAuthApi>();
        
        mock.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    Token = GenerateValidToken(Guid.NewGuid(), "test_doctor", "Doctor"),
                    RefreshToken = "test_refresh_token",
                    User = new UserDetailDto
                    {
                        Id = Guid.NewGuid(),
                        UserName = "test_doctor",
                        RealName = "测试医生",
                        Role = UserRole.Doctor
                    },
                    ExpiresAt = DateTime.UtcNow.AddHours(8)
                },
                Message = "登录成功"
            }));

        return mock;
    }

    private static IApplicationStateService CreateMockApplicationStateService()
    {
        var mock = Substitute.For<IApplicationStateService>();
        mock.IsApiHealthy.Returns(true);
        mock.ConnectionStatus.Returns("Connected");
        return mock;
    }

    private static string GenerateValidToken(Guid userId, string userName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("your-test-secret-key-at-least-32-characters-long-for-testing");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role),
                new Claim("user_type", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = "LYBT.WebAPI",
            Audience = "LYBT.Desktop",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class ViewModelServices : IViewModelServices
{
    public ILoggerFactory LoggerFactory { get; }
    public IEventAggregator EventAggregator { get; }
    public IRegionManager RegionManager { get; }
    public ISessionManager SessionManager { get; }
    public IUserNotificationService UserNotificationService { get; }
    public ICommonDialogService CommonDialogService { get; }
    public IRoleRegistry RoleRegistry { get; }

    public ViewModelServices(
        ILoggerFactory loggerFactory,
        IEventAggregator eventAggregator,
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IUserNotificationService userNotificationService,
        ICommonDialogService commonDialogService,
        IRoleRegistry roleRegistry)
    {
        LoggerFactory = loggerFactory;
        EventAggregator = eventAggregator;
        RegionManager = regionManager;
        SessionManager = sessionManager;
        UserNotificationService = userNotificationService;
        CommonDialogService = commonDialogService;
        RoleRegistry = roleRegistry;
    }
}
