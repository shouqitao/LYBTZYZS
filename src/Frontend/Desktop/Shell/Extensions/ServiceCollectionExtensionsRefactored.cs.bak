using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Refit;
using LYBT.WPF.Client.Core.Configuration;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Shell.ViewModels;
using LYBT.WPF.Client.Modules.Authentication.ViewModels;
using LYBT.WPF.Client.Core.Security;

namespace LYBT.WPF.Client.Shell.Extensions
{
    /// <summary>
    /// 重构后的服务注册扩展 - 遵循UltraThink标准
    /// </summary>
    public static class ServiceCollectionExtensionsRefactored
    {
        /// <summary>
        /// 注册所有服务（重构版）
        /// </summary>
        public static IContainerRegistry RegisterAllServicesRefactored(this IContainerRegistry container)
        {
            // 1. 注册日志服务
            container.RegisterLogging();
            
            // 2. 注册配置服务
            container.RegisterConfiguration();
            
            // 3. 注册HTTP客户端和Refit接口
            container.RegisterHttpServices();
            
            // 4. 注册核心服务
            container.RegisterCoreServices();
            
            // 5. 注册业务服务
            container.RegisterBusinessServices();
            
            // 6. 注册视图模型
            container.RegisterViewModels();
            
            // 7. 注册导航服务
            container.RegisterNavigationServices();
            
            return container;
        }
        
        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(this IContainerRegistry container)
        {
            // 配置日志工厂
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LYBT", LogLevel.Debug)
                    .AddConsole()
                    .AddDebug();
            });
            
            container.RegisterInstance<ILoggerFactory>(loggerFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>));
        }
        
        /// <summary>
        /// 注册配置服务
        /// </summary>
        private static void RegisterConfiguration(this IContainerRegistry container)
        {
            // 注册配置管理器
            container.RegisterSingleton<IConfigurationManager, ConfigurationManager>();
            
            // 注册特定配置
            container.RegisterInstance(new ApiConfiguration
            {
                BaseUrl = "https://localhost:7001",
                Timeout = TimeSpan.FromSeconds(30),
                MaxRetryAttempts = 3
            });
        }
        
        /// <summary>
        /// 注册HTTP服务
        /// </summary>
        private static void RegisterHttpServices(this IContainerRegistry container)
        {
            // 注册HttpClient工厂
            container.RegisterSingleton<IHttpClientFactory>(() =>
            {
                var services = new ServiceCollection();
                
                // 配置命名HttpClient
                services.AddHttpClient("API", client =>
                {
                    client.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                })
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());
                
                var serviceProvider = services.BuildServiceProvider();
                return serviceProvider.GetRequiredService<IHttpClientFactory>();
            });
            
            // 注册Refit接口
            RegisterRefitInterfaces(container);
        }
        
        /// <summary>
        /// 注册Refit接口
        /// </summary>
        private static void RegisterRefitInterfaces(IContainerRegistry container)
        {
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                })
            };
            
            // 注册认证API
            container.Register<IAuthApiService>(() =>
            {
                var httpClientFactory = container.Resolve<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("API");
                return RestService.For<IAuthApiService>(httpClient, refitSettings);
            });
            
            // 注册其他API服务...
            RegisterOtherApiServices(container, refitSettings);
        }
        
        /// <summary>
        /// 注册其他API服务
        /// </summary>
        private static void RegisterOtherApiServices(IContainerRegistry container, RefitSettings refitSettings)
        {
            // 用户API
            container.Register<IUserApiService>(() =>
            {
                var httpClientFactory = container.Resolve<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("API");
                var tokenManager = container.Resolve<ITokenManager>();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenManager.GetToken());
                return RestService.For<IUserApiService>(httpClient, refitSettings);
            });
            
            // 患者API
            container.Register<IPatientApiService>(() =>
            {
                var httpClientFactory = container.Resolve<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("API");
                var tokenManager = container.Resolve<ITokenManager>();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenManager.GetToken());
                return RestService.For<IPatientApiService>(httpClient, refitSettings);
            });
        }
        
        /// <summary>
        /// 注册核心服务
        /// </summary>
        private static void RegisterCoreServices(this IContainerRegistry container)
        {
            // Token管理
            container.RegisterSingleton<ITokenManager, TokenManager>();
            
            // API健康监控
            container.RegisterSingleton<IApiHealthMonitor, ApiHealthMonitor>();
            
            // 安全服务
            container.Register<SecurePasswordManager>();
            
            // 凭据服务
            container.RegisterSingleton<ICredentialService, SecureCredentialService>();
            
            // 缓存服务
            container.RegisterSingleton<IMemoryCacheService, MemoryCacheService>();
            
            // 错误处理服务
            container.RegisterSingleton<IErrorHandlingService, ErrorHandlingService>();
            
            // 导航服务
            container.RegisterSingleton<INavigationService, NavigationService>();
        }
        
        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(this IContainerRegistry container)
        {
            // 认证服务（使用重构版本）
            container.RegisterSingleton<IAuthenticationService, AuthenticationServiceRefactored>();
            
            // 用户服务
            container.RegisterScoped<IUserService, UserService>();
            
            // 患者服务
            container.RegisterScoped<IPatientService, PatientService>();
            
            // 处方服务
            container.RegisterScoped<IPrescriptionService, PrescriptionService>();
            
            // 诊疗服务
            container.RegisterScoped<IConsultationService, ConsultationService>();
            
            // 中药材服务
            container.RegisterScoped<IHerbService, HerbService>();
        }
        
        /// <summary>
        /// 注册视图模型
        /// </summary>
        private static void RegisterViewModels(this IContainerRegistry container)
        {
            // 主窗口视图模型
            container.Register<MainWindowViewModel>();
            container.Register<HomeViewModel>();
            
            // 认证相关视图模型（使用重构版本）
            container.Register<LoginViewModelRefactored>();
            
            // 业务视图模型
            container.RegisterForNavigation<PatientListViewModel>();
            container.RegisterForNavigation<PatientDetailViewModel>();
            container.RegisterForNavigation<PrescriptionViewModel>();
            container.RegisterForNavigation<ConsultationViewModel>();
        }
        
        /// <summary>
        /// 注册导航服务
        /// </summary>
        private static void RegisterNavigationServices(this IContainerRegistry container)
        {
            // 注册导航视图
            container.RegisterForNavigation<LoginView, LoginViewModelRefactored>();
            container.RegisterForNavigation<HomeView, HomeViewModel>();
            container.RegisterForNavigation<PatientListView, PatientListViewModel>();
            container.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
            container.RegisterForNavigation<PrescriptionView, PrescriptionViewModel>();
            container.RegisterForNavigation<ConsultationView, ConsultationViewModel>();
        }
        
        /// <summary>
        /// 获取重试策略
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var logger = context.Values.ContainsKey("logger") ? context.Values["logger"] as ILogger : null;
                        logger?.LogWarning("重试 {RetryCount} 次，等待 {Timespan} 秒", retryCount, timespan.TotalSeconds);
                    });
        }
        
        /// <summary>
        /// 获取熔断策略
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    3,
                    TimeSpan.FromSeconds(30),
                    onBreak: (result, timespan) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"熔断器开启 {timespan.TotalSeconds} 秒");
                    },
                    onReset: () =>
                    {
                        System.Diagnostics.Debug.WriteLine("熔断器重置");
                    });
        }
        
        /// <summary>
        /// 注册扩展方法
        /// </summary>
        private static IContainerRegistry RegisterForNavigation<TView, TViewModel>(this IContainerRegistry container)
            where TView : class
            where TViewModel : class
        {
            container.RegisterForNavigation<TView>(typeof(TViewModel).Name);
            return container;
        }
    }
}