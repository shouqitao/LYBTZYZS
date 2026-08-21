# R4 性能与资源管理审查报告

**审查日期**：2026-08-21  
**审查范围**：src/Server/、src/Client/Desktop/、src/Shared/  
**审查角度**：N+1 查询、连接管理、内存泄漏、异步/await、缓存策略  

---

## 审查概览

| 类别 | 发现数 | 严重度分布 |
|------|--------|-----------|
| N+1 查询 | 2 | 🔴 高 / 🟡 中 |
| 缺少分页 | 1 | 🟡 中 |
| 连接管理 | 1 | 🟡 中 |
| HttpClient 复用 | 2 | 🟡 中 / 🟢 低 |
| 内存泄漏 | 2 | 🟡 中 / 🟢 低 |
| 异步问题 | 4 | 🔴 高 ×1 / 🟡 中 ×2 / 🟢 低 ×1 |
| 缺少 CancellationToken | 15+ | 🟡 中 |
| 日志过度写入 | 1 | 🟢 低 |
| 缓存策略 | 1 | 🟢 低 |
| DbContext 层级违规 | 1 | 🟡 中 |

---

## 🔴 高严重度

### P1. N+1 查询：CatalogCrossModuleService.GetHerbPricesAsync

**文件**：`src/Server/Modules/LYBT.Module.Catalog/Services/CatalogCrossModuleService.cs:41-53`

**问题**：对每个 herbId 逐个执行数据库查询，典型 N+1 反模式。当传入多个 herbId 时（如批量开方场景），每次调用产生 N 次独立数据库往返。

```csharp
// 当前实现：N 次数据库查询
foreach (var herbId in idList)
{
    var herb = await _context.Herbs
        .AsNoTracking()
        .Where(h => h.Id == herbId && !h.IsDeleted)
        .Select(h => new { h.Id, h.Price })
        .FirstOrDefaultAsync(cancellationToken);
    if (herb != null) result[herb.Id] = herb.Price;
}
```

**建议修复**：改为单次 `WHERE Id IN (...)` 批量查询：
```csharp
var idList = herbIds.ToList();
if (idList.Count == 0) return new Dictionary<Guid, decimal>();

return await _context.Herbs
    .AsNoTracking()
    .Where(h => idList.Contains(h.Id) && !h.IsDeleted)
    .ToDictionaryAsync(h => h.Id, h => h.Price, cancellationToken);
```

**影响**：批量开方/导入时，100 个药材 = 100 次查询。修复后 1 次查询。

---

### P2. N+1 查询：CatalogCrossModuleService.GetDisabledHerbIdsAsync

**文件**：`src/Server/Modules/LYBT.Module.Catalog/Services/CatalogCrossModuleService.cs:56-76`

**问题**：同样对每个 herbId 逐个执行数据库查询，与 P1 同构。

```csharp
foreach (var herbId in idList)
{
    var herb = await _context.Herbs
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(h => h.Id == herbId)
        .Select(h => new { h.Id, h.Status, h.IsDeleted })
        .FirstOrDefaultAsync(cancellationToken);
    if (herb != null && (herb.IsDeleted || herb.Status == CommonStatus.Disabled))
        disabledIds.Add(herb.Id);
}
```

**建议修复**：改为单次批量查询：
```csharp
var idList = herbIds.ToList();
if (idList.Count == 0) return new HashSet<Guid>();

var disabledIds = await _context.Herbs
    .AsNoTracking()
    .IgnoreQueryFilters()
    .Where(h => idList.Contains(h.Id) && (h.IsDeleted || h.Status == CommonStatus.Disabled))
    .Select(h => h.Id)
    .ToListAsync(cancellationToken);

return new HashSet<Guid>(disabledIds);
```

**影响**：批量操作时 N 次查询 → 1 次查询。

---

## 🟡 中严重度

### P3. sync-over-async：DesktopUpdateService.ApplyUpdateAndRestart

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/DesktopUpdateService.cs:86-92`

**问题**：使用 `.GetAwaiter().GetResult()` 阻塞异步调用，可能导致死锁（WPF Dispatcher 场景）。

```csharp
public void ApplyUpdateAndRestart()
{
    var update = GetManager().CheckForUpdatesAsync().GetAwaiter().GetResult();
    // ...
}
```

**建议修复**：将方法签名改为 `async Task`，或使用 `Task.Run()` 隔离：
```csharp
public async Task ApplyUpdateAndRestartAsync()
{
    var update = await GetManager().CheckForUpdatesAsync();
    if (update?.TargetFullRelease is null) return;
    GetManager().ApplyUpdatesAndRestart(update.TargetFullRelease, Array.Empty<string>());
}
```

---

### P4. async void 风险：8 处未受控的异步异常

**文件**：多个 ViewModel 和 View

| 文件 | 方法 | 风险 |
|------|------|------|
| `CardReaderService.cs:269` | `AutoReadCallback` | Timer 回调，异常会崩溃进程 |
| `ConnectionStatusViewModel.cs:192` | `SyncModeDisplay` | 已有 try-catch（可控） |
| `ReportsHomeViewModel.cs:46` | `OnNavigatedTo` | Prism 导航回调，异常会崩溃 |
| `BackupManagementViewModel.cs:54` | `OnNavigatedTo` | 同上 |
| `AccountSettingsViewModel.cs:254` | `OnNavigatedTo` | 同上 |
| `MainWindow.xaml.cs:31` | `OnWindowLoaded` | WPF 事件回调，异常会崩溃 |
| `MainWindow.xaml.cs:51` | `OnPreviewKeyDown` | 同上 |

**建议修复**：
- Timer 回调 (`AutoReadCallback`)：改用 `async Task` + `Task.Factory.StartNew`，或确保方法体完全 try-catch
- Prism `OnNavigatedTo`：这是框架限制（`override async void` 是唯一签名），但方法体内部必须有完整的顶层 try-catch
- WPF 事件：同理，确保顶层 try-catch

**现状**：`SyncModeDisplay` 已有注释说明 "探测异常必须就地捕获"，说明团队意识到了这个问题，但其他 6 处未见保护。

---

### P5. HttpClient 创建：TokenRefreshHandler 持有独立 HttpClient

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/TokenRefreshHandler.cs:65-74`

**问题**：`TokenRefreshHandler` 构造函数中 `new HttpClient(httpHandler)` 创建了一个不走 IHttpClientFactory 管理的独立 HttpClient。虽然有 Dispose 释放，但绕过了工厂的连接池管理。

```csharp
var httpHandler = new HttpClientHandler();
_refreshHttpClient = new HttpClient(httpHandler)
{
    BaseAddress = new Uri(apiBaseUrl),
    Timeout = TimeSpan.FromSeconds(30)
};
```

**建议**：将 `IHttpClientFactory` 注入，用 `CreateClient("RefreshToken")` 创建受管 HttpClient。

---

### P6. HttpClient 创建：ConfigurationCenterViewModel 与 ConnectionModeService

**文件**：
- `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/ConfigurationCenterViewModel.cs:135`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/ConnectionModeService.cs:115`

**问题**：`using var client = new HttpClient` 用于远程连通性探测。虽然 `using` 确保了释放，但每次探测都创建新连接，不符合 HttpClient 复用最佳实践。

**建议**：注入 `IHttpClientFactory`，或使用已有的 `SwitchingApiClient`。

---

### P7. 内存泄漏风险：EventAggregator 订阅未取消

**文件**：`src/Client/Desktop/Modules/LYBT.Desktop.Registrations/ViewModels/RegistrationListViewModel.cs:96`

**问题**：`GetEvent<RegistrationRefreshedEvent>().Subscribe(OnRegistrationRefreshed)` 未见对应的 `Unsubscribe` 调用。如果 ViewModel 被频繁创建/销毁（如导航），可能导致内存泄漏。

**建议**：实现 `IDisposable`，在 `Dispose` 中取消订阅。或使用 Prism 的 `SubscriptionToken` 模式。

---

### P8. DbContext 注入 Service 层（P10 违规）

**文件**：`src/Server/Modules/LYBT.Module.Catalog/Services/CatalogCrossModuleService.cs:16`

**问题**：`CatalogCrossModuleService` 直接注入 `CatalogDbContext`，违反架构约束 P10（Service 禁注入 DbContext，仅 Repository/Base 可）。

**建议**：将数据访问逻辑抽取到 Repository 层，Service 通过 Repository 接口访问数据。

---

### P9. 缺少 CancellationToken：15+ 处异步方法

**文件**：多处 Desktop 端服务和 Repository

**关键缺失位置**：
- `HttpApiClientBase.EnsureSuccessOrThrowAsync` — HTTP 响应检查
- `TokenRefreshHandler.PublishTokenRefreshSucceededEventAsync` — 事件发布
- `ModuleLoadingService.LoadModuleAsync/LoadAllModulesAsync` — 模块加载
- `ApiClientRepositoryBase.ExecuteAsync` — 通用仓储操作
- `LogoutService.ExecuteLocalLogoutAsync` — 登出流程
- `TokenStorageService.SaveAuthenticationAsync/ClearAuthenticationAsync` — Token 持久化
- `ConnectionModeService.HandleUrlChangedAsync` — 连接切换

**影响**：无法在应用关闭或导航切换时取消进行中的操作，可能导致资源泄漏或操作延迟。

---

### P10. 分页查询：GetAllActiveHerbsAsync 无分页

**文件**：`src/Server/Modules/LYBT.Module.Catalog/Services/CatalogCrossModuleService.cs:91-101`

**问题**：`GetAllActiveHerbsAsync` 加载全部活跃药材到内存，无分页限制。

```csharp
return await _context.Herbs
    .Where(h => !h.IsDeleted && h.Status == CommonStatus.Enabled)
    .ToListAsync(cancellationToken);
```

**风险**：药材数量增长后（如达到数千条），每次调用都全量加载。

**建议**：考虑分页或缓存策略（药材数据变更频率低，适合缓存）。

---

## 🟢 低严重度

### P11. ConfigureAwait(false) 使用不足

**统计**：全项目仅 3 个文件使用了 `ConfigureAwait(false)`。

**建议**：库代码（Shared、Infrastructure）应考虑使用 `ConfigureAwait(false)` 避免同步上下文捕获。WPF 端（Desktop）不需要（需要回到 UI 线程）。

---

### P12. 日志写入量：1245 条日志语句

**统计**：全 src/ 目录共 1245 条日志语句，分布较均匀。

**现状**：未见明显的生产环境日志风暴问题。`LogDebug` 在生产环境默认不输出（Serilog 配置），`LogInformation` 数量合理。

**建议**：持续监控生产环境日志量，特别是 `DatabaseStartupDiagnostics`（31 条）和 `UnifiedApplicationInitialization`（16 条）在启动时的输出。

---

### P13. 缓存策略合理

**Desktop 端缓存**：
- `DesktopCacheManager` 使用 `IMemoryCache` + 前缀清除，设计合理
- `TokenStorageService` 使用内存缓存 + DPAPI 持久化，Token 不落盘为明文
- 缓存失效通过 `EventAggregator` 发布事件通知，解耦良好

**Server 端缓存**：
- 未发现 Redis 或分布式缓存使用（当前架构为单实例部署，无需分布式缓存）
- EF Core 查询结果未做应用层缓存（合理，避免缓存一致性问题）

---

### P14. SemaphoreSlim 使用正确

**文件**：`TokenRefreshHandler.cs:42`

**正面发现**：Token 刷新使用 `SemaphoreSlim(1,1)` 防止并发刷新，设计正确。`Dispose` 中也正确释放了信号量。

---

### P15. SwitchingApiClient 双重检查锁

**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/SwitchingApiClient.cs:57-86`

**正面发现**：使用无锁快路径 + lock 慢路径的双重检查模式，URL 未变时 0 锁开销。旧客户端正确 Dispose。

---

## 总结与优先级

| 优先级 | 问题 | 修复难度 | 建议时间 |
|--------|------|----------|----------|
| 🔴 P1 | N+1 GetHerbPricesAsync | 低（改 1 个方法） | 15 min |
| 🔴 P2 | N+1 GetDisabledHerbIdsAsync | 低（改 1 个方法） | 15 min |
| 🟡 P3 | sync-over-async UpdateService | 中（改接口签名） | 30 min |
| 🟡 P4 | async void 风险 | 中（逐个加固） | 1-2 h |
| 🟡 P5 | HttpClient 独立创建 | 中（改注入） | 30 min |
| 🟡 P8 | DbContext 层级违规 | 高（需重构） | 2-4 h |
| 🟡 P9 | 缺少 CancellationToken | 低（逐个加参数） | 1-2 h |
| 🟢 P6/P7/P10 | 其他改进 | — | 按需 |

---

*审查完成。P1/P2 为高优先级修复项，建议在下一个 sprint 中处理。*
