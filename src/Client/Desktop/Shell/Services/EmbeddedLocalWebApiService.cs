using LYBT.Desktop.Contracts.Services;
using LYBT.LocalWebAPI;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// Starts and manages the embedded LocalWebAPI Kestrel server within the WPF process.
/// </summary>
public sealed class EmbeddedLocalWebApiService : IEmbeddedLocalWebApiService, IDisposable
{
    private const string LocalUrl = "http://localhost:5300";
    private const string LocalConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=LYBTDesktop;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    private readonly ILogger<EmbeddedLocalWebApiService> _logger;
    private readonly IConfiguration _configuration;
    private WebApplication? _app;
    private readonly object _lock = new();

    public EmbeddedLocalWebApiService(ILogger<EmbeddedLocalWebApiService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public bool IsRunning => Volatile.Read(ref _app) != null;
    public string BaseUrl => LocalUrl;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_app != null) return;
        }

        try
        {
            _logger.LogInformation("[LOCAL-API] Starting embedded LocalWebAPI on {Url}", LocalUrl);

            var builder = LocalWebApiProgram.CreateBuilder();
            builder.WebHost.UseUrls(LocalUrl);

            builder.Services.Configure<DefaultPasswordOptions>(options =>
            {
                options.SysAdminPassword = _configuration["DefaultPasswords:SysAdminPassword"] ?? "SysAdmin@2026!";
                options.AdminPassword = _configuration["DefaultPasswords:AdminPassword"] ?? "Admin@123456";
                options.NewUserPassword = _configuration["DefaultPasswords:NewUserPassword"] ?? "User@123456";
                options.ForceChangeOnFirstLogin = _configuration.GetValue<bool>("DefaultPasswords:ForceChangeOnFirstLogin");
            });

            _app = LocalWebApiProgram.CreateApplication(builder, LocalConnectionString);
            await LocalWebApiProgram.InitializeDatabaseAsync(_app);

            await _app.StartAsync(cancellationToken);

            _logger.LogInformation("[LOCAL-API] Embedded LocalWebAPI started on {Url}", LocalUrl);
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
        _ = StopAsync();
    }
}
