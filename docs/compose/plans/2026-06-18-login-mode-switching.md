# 登录远程/本地切换逻辑实现 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 在 WPF 启动时启动嵌入式 LocalWebAPI 服务器，实现远程不可用时自动降级到本地的完整流程。

**Architecture:** 新建 `EmbeddedLocalWebApiService` 在 WPF 进程内启动 Kestrel，作为 StartupPipeline 中的一个步骤执行。ConnectionModeService 探测远程可用性，降级时将 URL 切到 localhost。SwitchingApiClient 自动路由。

**Tech Stack:** ASP.NET Core Kestrel (embedded) / WPF / Prism

---

### Task 1: 创建 EmbeddedLocalWebApiService — 在 WPF 进程内启动 Kestrel

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/EmbeddedLocalWebApiService.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IEmbeddedLocalWebApiService.cs`

- [ ] **Step 1: 创建 IEmbeddedLocalWebApiService 接口**

```csharp
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Manages the embedded LocalWebAPI Kestrel server lifecycle within the WPF process.
/// </summary>
public interface IEmbeddedLocalWebApiService
{
    /// <summary>True if the server is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>The base URL the server is listening on (e.g., http://localhost:5000).</summary>
    string BaseUrl { get; }

    /// <summary>Start the embedded server. Idempotent — no-op if already running.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stop the embedded server gracefully.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: 创建 EmbeddedLocalWebApiService 实现**

```csharp
using LYBT.Desktop.Contracts.Services;
using LYBT.LocalWebAPI;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Services;

public sealed class EmbeddedLocalWebApiService : IEmbeddedLocalWebApiService
{
    private const string LocalUrl = "http://localhost:5000";
    private const string ConnectionStringName = "LocalDb";

    private readonly ILogger<EmbeddedLocalWebApiService> _logger;
    private WebApplication? _app;
    private readonly object _lock = new();

    public EmbeddedLocalWebApiService(ILogger<EmbeddedLocalWebApiService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning => _app != null;
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

            var connectionString = GetLocalConnectionString();
            var builder = LocalWebApiProgram.CreateBuilder();
            builder.WebHost.UseUrls(LocalUrl);

            _app = LocalWebApiProgram.CreateApplication(builder, connectionString);
            await LocalWebApiProgram.InitializeDatabaseAsync(_app);

            await _app.StartAsync(cancellationToken);

            _logger.LogInformation("[LOCAL-API] Embedded LocalWebAPI started successfully on {Url}", LocalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCAL-API] Failed to start embedded LocalWebAPI");
            _app = null;
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
                _logger.LogInformation("[LOCAL-API] Embedded LocalWebAPI stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LOCAL-API] Error stopping LocalWebAPI");
            }
        }
    }

    private static string GetLocalConnectionString()
    {
        // Use the same LocalDB connection string as the Desktop app settings
        return "Server=(localdb)\\MSSQLLocalDB;Database=LYBTDesktop;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
    }
}
```

注意：
- `builder.WebHost.UseUrls(LocalUrl)` 确保监听在 localhost:5000
- `GetLocalConnectionString()` 使用 LocalDB — 需要检查实际配置中的连接字符串名称
- 需要添加 `using Microsoft.AspNetCore.Builder;` 和 `using Microsoft.AspNetCore.Hosting;` 和 `using Microsoft.Extensions.Hosting;`

- [ ] **Step 3: 在 DI 中注册**

在 `src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs` 中添加：
```csharp
containerRegistry.RegisterSingleton<IEmbeddedLocalWebApiService, EmbeddedLocalWebApiService>();
```

- [ ] **Step 4: 构建**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors（如有引用问题，修复 using 或添加项目引用）

### Task 2: 在 StartupPipeline 中启动 LocalWebAPI

**Files:**
- Create: `src/Client/Desktop/Shell/Services/Startup/Steps/LocalWebApiStartupStep.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs` — 注册新步骤

- [ ] **Step 1: 创建 LocalWebApiStartupStep**

```csharp
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 启动嵌入式 LocalWebAPI 服务器（本地模式用）。
/// 非必需步骤——失败只记录警告，不阻塞启动。
/// </summary>
public class LocalWebApiStartupStep : IStartupStep
{
    private readonly IEmbeddedLocalWebApiService _localWebApi;
    private readonly ILogger<LocalWebApiStartupStep> _logger;

    public LocalWebApiStartupStep(
        IEmbeddedLocalWebApiService localWebApi,
        ILogger<LocalWebApiStartupStep> logger)
    {
        _localWebApi = localWebApi;
        _logger = logger;
    }

    public string Name => "本地 API 服务";
    public int Order => 250; // 在 CoreServices(200/300) 之后，ApiHealthCheck(400) 之前
    public bool IsRequired => false; // 本地API启动失败不阻塞应用
    public string? ParallelGroup => null;

    public async Task<StartupStepResult> ExecuteAsync(
        IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("正在启动本地 API 服务...");
            await _localWebApi.StartAsync(cancellationToken);
            return StartupStepResult.Succeeded("本地 API 服务已启动");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "本地 API 服务启动失败，远程模式将需要远程服务器可用");
            return StartupStepResult.Failed(
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("启动本地API", ex),
                ex);
        }
    }
}
```

- [ ] **Step 2: 在 App.xaml.cs 的 RegisterStartupSteps 中注册**

在现有步骤注册后添加：
```csharp
_startupPipeline.RegisterStep(
    Container.Resolve<Services.Startup.Steps.LocalWebApiStartupStep>());
```

位置：在 ApiHealthCheckStartupStep 之前（Order 250 < 400）。

- [ ] **Step 3: 构建 + 提交**

### Task 3: 确保 ConnectionModeService 降级逻辑正确

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/ConnectionModeService.cs`

- [ ] **Step 1: 检查 DetectBestModeAsync 的降级逻辑**

读取 `ConnectionModeService.DetectBestModeAsync` 方法。确认：
1. 先探测远程 URL 的 `/api/v1/health`
2. 如果远程不可用，调用 `SwitchUrlAsync` 切到 `http://localhost:5000`
3. SwitchUrlAsync 调用 `_connectionSettings.SetUrlAsync(localUrl)`
4. SwitchingApiClient 自动检测 URL 变化，路由到 HttpClientApiClient

如果降级逻辑缺少"切换到 localhost"步骤，补充：

```csharp
private async Task SwitchUrlAsync(string url)
{
    if (_connectionSettings.CurrentUrl == url) return;
    await _connectionSettings.SetUrlAsync(url);
    // SwitchingApiClient 在下次属性访问时自动检测 URL 变化
}
```

- [ ] **Step 2: 确保 LoginViewModel 调用 DetectBestModeAsync**

确认 `LoginViewModel.BackgroundInitAsync()` 调用 `_connectionModeService.DetectBestModeAsync()`。

- [ ] **Step 3: 构建验证**

### Task 4: 修正默认 URL 端口一致

**Files:**
- Check: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ConnectionSettingsService.cs` — 默认 URL
- Check: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/ConnectionModeService.cs` — LocalBaseUrl

- [ ] **Step 1: 确认端口一致**

- `ConnectionSettingsService` 默认 URL: `http://127.0.0.1:5100`
- `ConnectionModeService.LocalBaseUrl`: `http://localhost:5000`
- `EmbeddedLocalWebApiService.LocalUrl`: `http://localhost:5000`

如果端口不一致（5100 vs 5000），统一为 `http://localhost:5000`。

特别注意：`ConnectionSettingsService` 的默认 URL 应改为 `http://localhost:5000`，因为这是本地模式启动后的服务地址。或者，让 ConnectionModeService 在启动时自动探测并切换。

- [ ] **Step 2: 构建验证 + 提交**

### Task 5: 最终验证

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: 验证启动流程**

预期行为：
1. 应用启动 → SplashScreen 显示
2. StartupPipeline 执行 → LocalWebApiStartupStep 在 localhost:5000 启动 Kestrel
3. ApiHealthCheckStartupStep 探测远程服务器
4. 如果远程不可用 → ConnectionModeService 切到 localhost:5000
5. 登录界面显示模式指示器（本地模式 = 橙色）
6. 用户登录 → SwitchingApiClient 路由到 localhost:5000 的 LocalWebAPI

- [ ] **Step 3: 提交 + 推送**
