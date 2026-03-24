using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
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

/// <summary>
/// 真实测试组合 - 用于集成测试的真实服务配置
/// 
/// 设计决策：
/// - 使用 Microsoft.Extensions.DependencyInjection 作为测试容器
/// - 真实服务: TokenStorageService, LocalTokenValidator, AuthenticationService
/// - 模拟服务: IAuthApi (使用 WebApiFixture 提供的 HttpClient)
/// - 完整模拟 ViewModel 依赖链
/// </summary>
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

    /// <summary>
    /// 使用真实的 Refit Client (连接到 WebApiFixture 提供的 API)
    /// </summary>
    public RealTestComposition WithRealRefitClient(HttpClient httpClient)
    {
        _refitClient = httpClient;
        
        // 配置 Refit 设置
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            })
        };

        // 使用真实 HTTP 客户端创建 Refit 客户端
        _services.AddSingleton<IAuthApi>(_ => RestService.For<IAuthApi>(httpClient, refitSettings));
        
        return this;
    }

    /// <summary>
    /// 构建服务提供者
    /// </summary>
    public RealTestComposition Build()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return this;
    }

    /// <summary>
    /// 解析服务
    /// </summary>
    public T Resolve<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("必须先调用 Build() 方法");
        
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 获取服务提供者
    /// </summary>
    public IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("必须先调用 Build() 方法");
        
        return _serviceProvider;
    }

    /// <summary>
    /// 注册默认服务
    /// </summary>
    private void RegisterDefaultServices()
    {
        // 配置
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "your-test-secret-key-at-least-32-characters-long-for-testing",
                ["Jwt:Issuer"] = "LYBT.WebAPI",
                ["Jwt:Audience"] = "LYBT.Desktop",
                ["Jwt:ClockSkewSeconds"] = "300",
                ["ApiClient:BaseUrl"] = "http://localhost:5001",
                ["ApiClient:TimeoutSeconds"] = "30"
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddSingleton<IOptions<ApiClientOptions>>(_ => 
            new OptionsWrapper<ApiClientOptions>(new ApiClientOptions 
            { 
                BaseUrl = "http://localhost:5001",
                TimeoutSeconds = 30 
            }));

        // 日志
        _services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Prism 核心服务 (模拟)
        _services.AddSingleton<IEventAggregator, EventAggregator>();
        _services.AddSingleton<IRegionManager, RegionManager>();

        // ViewModel 聚合服务
        _services.AddSingleton<IViewModelServices, ViewModelServices>();

        // 模拟 IAuthApi (如果未提供真实客户端)
        _services.AddSingleton<IAuthApi>(sp =>
        {
            // 如果已经通过 WithRealRefitClient 注册，则返回已注册的服务
            var existing = sp.GetService<IAuthApi>();
            if (existing != null && _refitClient != null)
                return existing;

            // 否则创建 Mock
            return CreateMockAuthApi();
        });

        // 模拟其他依赖
        _services.AddSingleton<ICredentialVault>(sp => Substitute.For<ICredentialVault>());
        _services.AddSingleton<IApplicationStateService>(sp => CreateMockApplicationStateService());
        _services.AddSingleton<IUsernameStorageService>(sp => Substitute.For<IUsernameStorageService>());
        _services.AddSingleton<IModeSwitchValidator>(sp => Substitute.For<IModeSwitchValidator>());

        // 真实服务 - Token 存储和验证
        _services.AddSingleton<ITokenStorageService, TokenStorageService>();
        _services.AddSingleton<ITokenValidator, LocalTokenValidator>();
        _services.AddSingleton<IAuthenticationService, AuthenticationService>();

        // LoginCoordinator 及其依赖
        _services.AddSingleton<ILoginCoordinator, LoginCoordinator>();
        _services.AddSingleton<ISessionLifecycleManager>(sp => Substitute.For<ISessionLifecycleManager>());
        _services.AddSingleton<IModuleLoadingService>(sp => Substitute.For<IModuleLoadingService>());
        _services.AddSingleton<INavigationCoordinator>(sp => Substitute.For<INavigationCoordinator>());
        _services.AddSingleton<IAuthenticationStateMachine, AuthenticationStateMachine>();

        // ViewModels
        _services.AddTransient<LoginViewModel>();
    }

    /// <summary>
    /// 创建 Mock IAuthApi
    /// </summary>
    private static IAuthApi CreateMockAuthApi()
    {
        var mock = Substitute.For<IAuthApi>();
        
        // 默认返回成功登录
        mock.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
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

    /// <summary>
    /// 创建 Mock IApplicationStateService
    /// </summary>
    private static IApplicationStateService CreateMockApplicationStateService()
    {
        var mock = Substitute.For<IApplicationStateService>();
        mock.IsApiHealthy.Returns(true);
        mock.ConnectionStatus.Returns("Connected");
        return mock;
    }

    /// <summary>
    /// 生成有效 JWT Token
    /// </summary>
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

/// <summary>
/// ViewModelServices 实现 - 用于测试
/// </summary>
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

/// <summary>
/// IOptions 包装器 - 用于测试
/// </summary>
public class OptionsWrapper<T> : IOptions<T> where T : class, new()
{
    public OptionsWrapper(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

/// <summary>
/// IOptions 接口 - 简化版本
/// </summary>
public interface IOptions<out T> where T : class, new()
{
    T Value { get; }
}
