using System.Security.Cryptography;
using System.Text;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Infrastructure.Configuration.Validation;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Logging.Bootstrap;
using LYBT.WebAPI.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// P1-3: Program.cs 上帝类拆分——将热更新、单实例、配置闭环、Kestrel 等 4 职责抽为扩展，保持 Program.Main 仅编排。
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>热更新检查与应用（P0-2 SHA256 校验）——Program.Main 首段。</summary>
    public static async Task ApplyHotUpdateAsync()
    {
        var updateFlag = Path.Combine(AppContext.BaseDirectory, ".update-pending");
        var shaFlag = Path.Combine(AppContext.BaseDirectory, ".update-pending.sha256");
        if (!File.Exists(updateFlag)) return;

        try
        {
            var zipPath = (await File.ReadAllTextAsync(updateFlag)).Trim();
            if (File.Exists(zipPath))
            {
                if (File.Exists(shaFlag))
                {
                    var expected = (await File.ReadAllTextAsync(shaFlag)).Trim().ToLowerInvariant();
                    var actual = await ComputeFileSha256Async(zipPath);
                    if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Error.WriteLine($"[UPDATE] SHA256 校验失败: expected={expected} actual={actual} —— 拒绝解压（防篡改）");
                        Log.Fatal("[UPDATE] 热更新 SHA256 校验失败 expected={Expected} actual={Actual} zip={ZipPath} —— 拒绝解压", expected, actual, zipPath);
                        File.Delete(updateFlag);
                        try { File.Delete(shaFlag); } catch { }
                        Environment.Exit(1);
                    }
                }
                Console.WriteLine("[UPDATE] 检测到更新包，正在应用...");
                var currentDir = AppContext.BaseDirectory;
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, currentDir, overwriteFiles: true);
                File.Delete(zipPath);
                Console.WriteLine("[UPDATE] 更新完成，重新启动...");
            }
            File.Delete(updateFlag);
            try { if (File.Exists(shaFlag)) File.Delete(shaFlag); } catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UPDATE] 更新失败: {ex.Message}");
            Log.Error(ex, "[UPDATE] 热更新应用失败");
            try { File.Delete(updateFlag); } catch { }
            try { if (File.Exists(shaFlag)) File.Delete(shaFlag); } catch { }
        }
    }

    /// <summary>单实例守卫（US-SHELL-024）——仅 Production 非 testhost 生效。</summary>
    public static bool AddSingleInstanceGuard(this WebApplicationBuilder builder, string environment, bool isTestHost)
    {
        if (environment == "Production" && !isTestHost && !Program.TryAcquireSingleInstance(Program.InstanceMutexName))
        {
            Log.Fatal("[启动] 检测到已有 LYBT.WebAPI 实例在运行（Mutex={Mutex}）——拒绝启动（US-SHELL-024 单实例保护）", Program.InstanceMutexName);
            Console.Error.WriteLine("[启动] 已有实例在运行，拒绝启动（单实例保护——请先停止旧进程或用 start.sh 重启）");
            Environment.Exit(1);
        }
        return true;
    }

    /// <summary>配置闭环：ADR-0019 config/ 路径 + runtime-overrides + 环境变量优先级 + 占位符后处理。</summary>
    public static void AddConfigurationClosedLoop(this WebApplicationBuilder builder, string environment)
    {
        foreach (var source in builder.Configuration.Sources.Where(src => src is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource).ToList())
            builder.Configuration.Sources.Remove(source);

        var configDir = Path.Combine(AppContext.BaseDirectory, "config");
        builder.Configuration.AddJsonFile(Path.Combine(configDir, "appsettings.json"), optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile(Path.Combine(configDir, $"appsettings.{environment}.json"), optional: true, reloadOnChange: true);

        var runtimeOverridesPath = Path.Combine(configDir, "runtime-overrides.json");
        var baseline = builder.Configuration.AsEnumerable().Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)!;

        foreach (var source in builder.Configuration.Sources.OfType<Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource>().ToList())
            builder.Configuration.Sources.Remove(source);

        builder.Configuration.AddJsonFile(runtimeOverridesPath, optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        if (builder.Configuration is IConfigurationRoot root)
            ConfigurationPostProcessor.Process(root);

        builder.Services.AddSingleton<LYBT.Infrastructure.Configuration.Stores.IConfigurationStore>(
            new JsonFileConfigurationStore(runtimeOverridesPath, baseline));
        builder.Services.AddLybtServerConfiguration(builder.Configuration);
        builder.Services.AddScoped<ProductionConfigurationValidator>();
        builder.Services.AddScoped<LYBT.Infrastructure.Configuration.Services.ISystemConfigurationService, LYBT.Infrastructure.Configuration.Services.SystemConfigurationService>();
        Log.Information("强类型配置注册完成");
    }

    /// <summary>Kestrel 多端点配置（P2-09）</summary>
    public static void MapKestrelEndpoints(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
            var endpoints = builder.Configuration.GetSection("Server:Endpoints");
            var httpEnabled = endpoints.GetValue<bool>("Http:Enabled", true);
            var httpUrl = endpoints["Http:Url"] ?? "http://0.0.0.0:5000";
            if (httpEnabled)
            {
                options.ListenAnyIP(GetPort(httpUrl), lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
                Log.Information("[启动] Listening on {HttpUrl}", httpUrl);
            }
            var httpsEnabled = endpoints.GetValue<bool>("Https:Enabled", false);
            var httpsUrl = endpoints["Https:Url"] ?? "https://0.0.0.0:5001";
            if (httpsEnabled)
            {
                var certPath = builder.Configuration["Server:Endpoints:Https:Certificate:Path"];
                var certPassword = builder.Configuration["Server:Endpoints:Https:Certificate:Password"];
                options.ListenAnyIP(GetPort(httpsUrl), lo =>
                {
                    lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                    if (string.IsNullOrWhiteSpace(certPath)) lo.UseHttps();
                    else if (string.IsNullOrWhiteSpace(certPassword)) lo.UseHttps(certPath);
                    else lo.UseHttps(certPath, certPassword);
                });
                Log.Information("[启动] Listening on {HttpsUrl} (cert={CertPath})", httpsUrl, certPath ?? "dev-cert");
            }
        });
    }

    private static int GetPort(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsDefaultPort) return uri.Port;
        return url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? 5001 : 5000;
    }

    private static async Task<string> ComputeFileSha256Async(string path)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
