// ---------------------------------------------------------------------------
// UnifiedApiClientExtensions — IApiClient registration with SwitchingApiClient
// ---------------------------------------------------------------------------
// Registers SwitchingApiClient as the singleton IApiClient, which routes to
// RefitApiClient or HttpClientApiClient based on the current connection URL.
// Also registers factory delegates for dynamic HttpClient creation.
// ---------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using DryIoc;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Logging.Correlation;
using LYBT.Shared.Logging.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.DryIoc;
using Prism.Ioc;
using Refit;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// 注册 <see cref="IApiClient"/> 的扩展方法。
/// </summary>
public static class UnifiedApiClientExtensions
{
    /// <summary>
    /// 将 <see cref="SwitchingApiClient"/> 注册为单例 <see cref="IApiClient"/>。
    /// </summary>
    public static void AddUnifiedApiClient(
        this IContainerRegistry containerRegistry,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiOptions = new LYBT.Shared.Configuration.Options.Client.ApiClientOptions();
        configuration
            .GetSection(LYBT.Shared.Configuration.Options.Client.ApiClientOptions.SectionName)
            .Bind(apiOptions);

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() },
                }
            ),
            // 领域错误层统一：远程非 2xx 与本地（HttpApiClientBase.EnsureSuccessOrThrowAsync）
            // 抛同一 ApiClientException，上层无需再区分 Refit.ApiException / HttpRequestException。
            ExceptionFactory = async response =>
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var (errorCode, serverMessage) = ApiErrorEnvelope.TryExtract(body);
                var message = !string.IsNullOrWhiteSpace(body)
                    ? body
                    : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                return new ApiClientException(message, response.StatusCode, errorCode, serverMessage);
            },
        };

        // Get the underlying DryIoc container via Prism extension method
        var container = containerRegistry.GetContainer();

        // 私有 IHttpClientFactory（handler 池化/生命周期 + Polly 弹性）：桌面主容器是 DryIoc，
        // 无法直接 AddHttpClient，故由 DesktopHttpTransportFactory 用私有 ServiceCollection 构建后
        // 注入 DryIoc。先建 handler 提供者，再构建工厂，最后回填工厂引用（两阶段避免循环）。
        var handlerProvider = new DryIocHttpHandlerProvider(container, apiOptions);
        var transportProvider = DesktopHttpTransportFactory.Build(apiOptions, handlerProvider);
        var transportFactory = transportProvider.GetRequiredService<IHttpClientFactory>();
        handlerProvider.AttachFactory(transportFactory);
        containerRegistry.RegisterInstance<IHttpClientFactory>(transportFactory);

        // 远程：RefitApiClient 持有的单一 HttpClient（链由工厂装配，模式切换时重建客户端）
        Func<string, HttpClient> remoteHttpClientFactory = baseUrl =>
        {
            var client = transportFactory.CreateClient(DesktopHttpTransportFactory.RemoteClientName);
            client.BaseAddress = new Uri(baseUrl);
            return client;
        };

        // 本地：把具名客户端包装为 IHttpClientFactory（HttpApiClientBase 每次 CreateClient 取新包装）
        Func<string, IHttpClientFactory> localHttpClientFactory = baseUrl =>
            new NamedLocalApiClientFactory(transportFactory, new Uri(baseUrl), apiOptions);

        containerRegistry.RegisterSingleton<IApiClient>(resolver =>
        {
            var connectionSettings = resolver.Resolve<IConnectionSettingsService>();
            return new SwitchingApiClient(
                connectionSettings,
                remoteHttpClientFactory,
                localHttpClientFactory,
                refitSettings,
                container.Resolve<ILogger<SwitchingApiClient>>()
            );
        });

        // IApiClientIdentity transient = 每次解析从 SwitchingApiClient.Current 动态获取
        // （修复双模式切换后旧 client 被 Dispose、singleton 缓存旧引用导致 ObjectDisposedException）
        // desktop-di-fix 2026-08-14：原缺失导致 Desktop 启动 DI 解析崩溃
        // （Unable to resolve IApiClientIdentity as parameter "authApi"）。
        containerRegistry.Register<IApiClientIdentity>(resolver =>
            resolver.Resolve<IApiClient>().Identity
        );

        // 其余直接注入子接口的服务（启动验证逐层暴露——ConnectionModeService→MedicalCases、
        // DeploymentService→Deploy、DiagnosticsService→Diagnostics、
        // ServerConfigurationService→Configuration）统一注册为 SwitchingApiClient 子接口转发。
        // 注：IApiClient（SwitchingApiClient）本身保持 singleton，仅子接口改为 transient。
        containerRegistry.Register<IApiClientMedicalCases>(resolver =>
            resolver.Resolve<IApiClient>().MedicalCases
        );
        containerRegistry.Register<IApiClientDeploy>(resolver =>
            resolver.Resolve<IApiClient>().Deploy
        );
        containerRegistry.Register<IApiClientDiagnostics>(resolver =>
            resolver.Resolve<IApiClient>().Diagnostics
        );
        containerRegistry.Register<IApiClientConfiguration>(resolver =>
            resolver.Resolve<IApiClient>().Configuration
        );
        // B-06: 备份/恢复服务门面（BackupManagementService → IApiClient.Backup）
        containerRegistry.Register<IApiClientBackup>(resolver =>
            resolver.Resolve<IApiClient>().Backup
        );

        // P1-1: Repository 最小权限注入具体子接口。子接口 transient——
        // 消费方须为 transient（每次解析取 SwitchingApiClient.Current 当前模式客户端），
        // 与 AuthHealthService 先例一致；避免 Singleton 仓库持有模式绑定子接口跨模式切换变陈旧。
        containerRegistry.Register<IApiClientPatients>(resolver =>
            resolver.Resolve<IApiClient>().Patients
        );
        containerRegistry.Register<IApiClientHerbs>(resolver =>
            resolver.Resolve<IApiClient>().Herbs
        );
        containerRegistry.Register<IApiClientFormulas>(resolver =>
            resolver.Resolve<IApiClient>().Formulas
        );
        containerRegistry.Register<IApiClientRegistrations>(resolver =>
            resolver.Resolve<IApiClient>().Registrations
        );
    }
}
