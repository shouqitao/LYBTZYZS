using LYBT.Desktop.Contracts.Services;
using LYBT.LocalWebAPI;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 在 WPF 进程内启动并管理嵌入式 LocalWebAPI Kestrel 服务器。
/// </summary>
public sealed class EmbeddedLocalWebApiService : IEmbeddedLocalWebApiService, IDisposable
{
    private readonly ILogger<EmbeddedLocalWebApiService> _logger;
    private readonly IOptions<OfflineModeOptions> _offlineModeOptions;
    private readonly IOptions<DefaultPasswordOptions> _defaultPasswordOptions;
    private readonly ILocalDatabaseSettingsService _localDatabaseSettings;
    private WebApplication? _app;
    private readonly object _lock = new();

    public EmbeddedLocalWebApiService(
        ILogger<EmbeddedLocalWebApiService> logger,
        IOptions<OfflineModeOptions> offlineModeOptions,
        IOptions<DefaultPasswordOptions> defaultPasswordOptions,
        ILocalDatabaseSettingsService localDatabaseSettings)
    {
        _logger = logger;
        _offlineModeOptions = offlineModeOptions;
        _defaultPasswordOptions = defaultPasswordOptions;
        _localDatabaseSettings = localDatabaseSettings;
    }

    public bool IsRunning => Volatile.Read(ref _app) != null;
    public string BaseUrl => _offlineModeOptions.Value.LocalApiBaseUrl;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_app != null) return;
        }

        try
        {
            _logger.LogInformation("[LOCAL-API] Starting embedded LocalWebAPI on {Url}", BaseUrl);

            var builder = LocalWebApiProgram.CreateBuilder();
            builder.WebHost.UseUrls(BaseUrl);

            var passwords = _defaultPasswordOptions.Value;
            builder.Services.Configure<DefaultPasswordOptions>(options =>
            {
                options.SysAdminPassword = passwords.SysAdminPassword;
                options.AdminPassword = passwords.AdminPassword;
                options.NewUserPassword = passwords.NewUserPassword;
                options.ForceChangeOnFirstLogin = passwords.ForceChangeOnFirstLogin;
            });

            // B-07：数据库目标来自初始化向导配置（未配置时 = 历史默认 LocalDB/LYBTDesktop）。
            // 只记录提供程序/实例/库名——口令与完整连接串永不进日志。
            var databaseProfile = _localDatabaseSettings.Current;
            _logger.LogInformation(
                "[LOCAL-API] Database target: Provider={Provider}, Server={Server}, Database={Database}",
                databaseProfile.Provider, databaseProfile.Server, databaseProfile.Database);

            _app = LocalWebApiProgram.CreateApplication(builder, _localDatabaseSettings.BuildConnectionString());
            await LocalWebApiProgram.InitializeDatabaseAsync(_app);

            await _app.StartAsync(cancellationToken);

            _logger.LogInformation("[LOCAL-API] Embedded LocalWebAPI started on {Url}", BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCAL-API] Failed to start embedded LocalWebAPI");
            lock (_lock) { _app = null; }
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        WebApplication? app;
        lock (_lock)
        {
            app = _app;
            _app = null;
        }

        if (app != null)
        {
            try
            {
                await app.StopAsync(cancellationToken);
                await app.DisposeAsync();
                _logger.LogInformation("[LOCAL-API] Stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LOCAL-API] Error stopping");
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _ = StopAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "[LOCAL-API] Error stopping during dispose");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCAL-API] Error during dispose");
        }
    }
}
