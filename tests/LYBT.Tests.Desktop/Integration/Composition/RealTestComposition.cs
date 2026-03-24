using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Refit;

namespace LYBT.Tests.Desktop.Integration.Composition;

/// <summary>
/// 真实 DI 容器测试组合 - 用于集成测试
/// 
/// 功能:
/// - 使用真实 Autofac 容器注册 Desktop Foundation/Infrastructure 模块
/// - 支持替换 Refit HttpClient 为测试客户端
/// - 支持注册 Mock 服务
/// </summary>
public class RealTestComposition : IDisposable
{
    private readonly ContainerBuilder _builder;
    private IContainer? _container;
    private bool _disposed;

    public RealTestComposition()
    {
        _builder = new ContainerBuilder();
        RegisterFoundationServices();
    }

    private void RegisterFoundationServices()
    {
        _builder.RegisterInstance(LoggerFactory.Create(builder => builder.AddConsole()))
            .As<ILoggerFactory>();

        _builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiClient:BaseUrl"] = "http://localhost:5001",
                ["ApiClient:IgnoreSslErrors"] = "true",
                ["Jwt:Issuer"] = "LYBT",
                ["Jwt:Audience"] = "LYBT.Client"
            })
            .Build();

        _builder.RegisterInstance(configuration).As<IConfiguration>();

        _builder.RegisterType<TokenStorageService>()
            .As<ITokenStorageService>()
            .SingleInstance();

        _builder.RegisterType<CredentialVault>()
            .As<ICredentialVault>()
            .SingleInstance();
    }

    public RealTestComposition WithRealRefitClient(HttpClient apiClient)
    {
        ArgumentNullException.ThrowIfNull(apiClient, nameof(apiClient));

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            })
        };

        _builder.RegisterInstance(apiClient).As<HttpClient>().SingleInstance();

        RegisterRefitApi<IAuthApi>(apiClient, refitSettings);
        RegisterRefitApi<IPatientApi>(apiClient, refitSettings);
        RegisterRefitApi<IMedicalCaseApi>(apiClient, refitSettings);
        RegisterRefitApi<IRegistrationApi>(apiClient, refitSettings);
        RegisterRefitApi<IHerbApi>(apiClient, refitSettings);
        RegisterRefitApi<IFormulaApi>(apiClient, refitSettings);
        RegisterRefitApi<IUserApi>(apiClient, refitSettings);
        RegisterRefitApi<ISyncApi>(apiClient, refitSettings);

        return this;
    }

    private void RegisterRefitApi<TApi>(HttpClient httpClient, RefitSettings settings) where TApi : class
    {
        _builder.Register(c => RestService.For<TApi>(httpClient, settings))
            .As<TApi>()
            .SingleInstance();
    }

    public RealTestComposition WithMockServices(Action<ContainerBuilder> configureMocks)
    {
        ArgumentNullException.ThrowIfNull(configureMocks, nameof(configureMocks));
        configureMocks(_builder);
        return this;
    }

    public RealTestComposition WithService<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService
    {
        _builder.RegisterType<TImplementation>().As<TService>();
        return this;
    }

    public RealTestComposition WithInstance<TService>(TService instance) where TService : class
    {
        _builder.RegisterInstance(instance).As<TService>();
        return this;
    }

    public RealTestComposition Build()
    {
        if (_container != null)
        {
            throw new InvalidOperationException("容器已经构建完成，不能重复构建");
        }

        _container = _builder.Build();
        return this;
    }

    public T Resolve<T>() where T : notnull
    {
        EnsureContainerBuilt();
        return _container!.Resolve<T>();
    }

    public object Resolve(Type serviceType)
    {
        EnsureContainerBuilt();
        return _container!.Resolve(serviceType);
    }

    public bool TryResolve<T>(out T? service) where T : class
    {
        if (_container == null)
        {
            service = default;
            return false;
        }

        return _container.TryResolve(out service);
    }

    public ILifetimeScope BeginLifetimeScope()
    {
        EnsureContainerBuilt();
        return _container!.BeginLifetimeScope();
    }

    private void EnsureContainerBuilt()
    {
        if (_container == null)
        {
            throw new InvalidOperationException("容器尚未构建，请先调用 Build() 方法");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _container?.Dispose();
            }

            _disposed = true;
        }
    }
}
