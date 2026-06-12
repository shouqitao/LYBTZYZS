# Sprint 5: v1.0-rc 生产就绪冲刺 - 详细设计

> **创建日期**: 2026-03-09
> **目标 Release**: v1.0-rc
> **前置条件**: Sprint 4 完成 (v1.0-beta 达成, 2026-03-09)

---

## 1. 背景与目标

v1.0-beta 已于 2026-03-09 达成 (133/138 US 完成)。Sprint 5 专注于关闭最后 2 个开放审计项，实现 NFR 备份要求，完成生产就绪验收。

### 范围

| 类型 | 编号 | 名称 | 优先级 |
|------|------|------|--------|
| US (Backlog) | US-ERR-007 | 错误追踪码 + TokenExpired | Could |
| US (Backlog) | US-SHELL-007 | 状态栏用户名/版本号 | Could |
| NFR | NFR-AVAIL-001 | SQLite 启动自动备份 | Quality |
| Quality | - | v1.0-rc 验收 (测试/架构/文档) | - |

---

## 2. Phase A: US-ERR-007 - TokenExpired 错误码补全 (CODE-25)

### 2.1 根因

PRD US-ERR-004 Business Rule 6 要求 `UnauthorizedException` 提供以下工厂方法：
```
InvalidPassword / CredentialsExpired / UserDisabled / UserLocked /
TokenExpired / DeviceMismatch / SessionExpired
```

当前代码**缺少** `TokenExpired()` 方法，当 JWT AccessToken 在 API 调用时过期返回 401，没有语义化的异常可以抛出。

### 2.2 文件变更

#### A.1: `src/Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs`

在 `AuthRefreshTokenInvalid = 10205` 之后新增：

```csharp
/// <summary>访问令牌过期</summary>
AuthAccessTokenExpired = 10206,
```

同步更新 `ErrorCodeExtensions.cs`：
```csharp
// ToHttpStatusCode()
ErrorCode.AuthAccessTokenExpired => 401,

// ToErrorCategory()
ErrorCode.AuthAccessTokenExpired => ErrorCategory.Authentication,
```

同步更新 `ErrorMessages.cs`：
```csharp
[ErrorCode.AuthAccessTokenExpired] = ("访问令牌已过期，请重新登录", "Access token expired"),
```

#### A.2: `src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Security/UnauthorizedException.cs`

新增工厂方法：
```csharp
/// <summary>
/// JWT 访问令牌已过期 (AccessToken expired, RefreshToken 可能仍有效)
/// US-ERR-007: CODE-25 补全
/// </summary>
public static UnauthorizedException TokenExpired() =>
    new(EC.AuthAccessTokenExpired, "访问令牌已过期，请重新登录", "AccessToken 已过期");
```

#### A.3: `src/Shared/LYBT.Shared.ExceptionHandling/Mappers/ClientErrorMessageMapper.cs`

在 Auth 错误码映射区块内新增 (紧接 10205 之后)：
```csharp
["10206"] = "访问令牌已过期，请重新登录",
```

#### A.4 (验证): `DesktopExceptionHandler` 追踪码验证

确认 `GetSafeMessageWithTrackingCode` 在系统错误路径中被调用 (非业务错误)。追踪码基础设施已完整实现，无需新增代码。

### 2.3 测试文件

`tests/LYBT.Tests.Desktop/PureLogic/Error/TokenExpiredErrorCodeTests.cs`

| 测试 | 说明 |
|------|------|
| `TokenExpired_FactoryMethod_ReturnsCorrectErrorCode` | 工厂方法返回 10206 |
| `TokenExpired_FactoryMethod_Returns401HttpCode` | HTTP 状态码为 401 |
| `TokenExpired_FactoryMethod_HasCorrectUserMessage` | 用户消息为中文 |
| `ClientErrorMessageMapper_10206_MapsToChineseMessage` | 映射正确 |
| `ClientErrorMessageMapper_TrackingCode_AttachedToSystemErrors` | 追踪码仅附加到系统级错误 |

**预估代码量**: ErrorCode.cs +3行; UnauthorizedException.cs +6行; ClientErrorMessageMapper.cs +1行; 测试 ~60行

---

## 3. Phase B: US-SHELL-007 - 状态栏用户名/版本号 (CODE-21)

### 3.1 根因

`GlobalStatusBar` 仅显示加载状态/状态消息/进度/时间，缺少：
- 当前登录用户名 (左中区域)
- 应用版本号 (右侧区域)

`AccountSettingsView/ViewModel/Control` 均已完整实现，无需补充。

### 3.2 文件变更

#### B.1: `GlobalStatusBar.xaml.cs` - 新增 DP

```csharp
// 当前用户名 DependencyProperty
public static readonly DependencyProperty CurrentUserNameProperty =
    DependencyProperty.Register(nameof(CurrentUserName), typeof(string),
        typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));

public string CurrentUserName
{
    get => (string)GetValue(CurrentUserNameProperty);
    set => SetValue(CurrentUserNameProperty, value);
}

// 应用版本 DependencyProperty
public static readonly DependencyProperty AppVersionProperty =
    DependencyProperty.Register(nameof(AppVersion), typeof(string),
        typeof(GlobalStatusBar),
        new PropertyMetadata(SystemConstants.ApplicationVersion));

public string AppVersion
{
    get => (string)GetValue(AppVersionProperty);
    set => SetValue(AppVersionProperty, value);
}
```

#### B.2: `GlobalStatusBar.xaml` - UI 布局调整

现有 4 列布局调整为 5 列，增加用户名列和版本列：

```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto" />   <!-- Col 0: 加载指示器 -->
    <ColumnDefinition Width="*" />      <!-- Col 1: 状态消息 -->
    <ColumnDefinition Width="Auto" />   <!-- Col 2: 进度 % -->
    <ColumnDefinition Width="Auto" />   <!-- Col 3: 用户名 [新增] -->
    <ColumnDefinition Width="Auto" />   <!-- Col 4: 版本 [新增] -->
    <ColumnDefinition Width="Auto" />   <!-- Col 5: 时间 -->
</Grid.ColumnDefinitions>

<!-- 用户名 (Col 3) -->
<StackPanel Grid.Column="3" Orientation="Horizontal"
            Visibility="{Binding CurrentUserName,
                Converter={x:Static converters:Cvt.StringToVis}}">
    <TextBlock Text="&#128100;" FontFamily="Segoe UI Symbol" FontSize="11"
               VerticalAlignment="Center" Margin="4,0,2,0" Foreground="#888" />
    <TextBlock Text="{Binding CurrentUserName}"
               Style="{StaticResource StatusTextStyle}" Foreground="#555" />
</StackPanel>

<!-- 版本号 (Col 4) -->
<TextBlock Grid.Column="4"
           Text="{Binding AppVersion, StringFormat=v{0}}"
           Style="{StaticResource StatusTextStyle}"
           Foreground="#AAAAAA" />

<!-- 系统时间移到 Col 5 -->
<TextBlock Grid.Column="5" ... />
```

#### B.3: Shell 绑定

**方案**: 在 `MainWindowViewModel` 或 Shell 的 CodeBehind 中，订阅 `IEventAggregator` 的登录/登出事件，更新 `GlobalStatusBar.CurrentUserName`。

检查 Shell 中 GlobalStatusBar 的引用点，在登录成功回调时：
```csharp
_statusBar.CurrentUserName = _sessionManager.CurrentUser?.DisplayName ?? string.Empty;
```

在登出时：
```csharp
_statusBar.CurrentUserName = string.Empty;
```

**替代方案** (如果直接引用不方便): 通过 `IEventAggregator` 发布 `UserLoggedInEvent`，GlobalStatusBar CodeBehind 订阅并更新。

### 3.3 测试

`tests/LYBT.Tests.Desktop/PureLogic/Shell/GlobalStatusBarTests.cs`

| 测试 | 说明 |
|------|------|
| `CurrentUserName_Default_IsEmpty` | DP 默认为空字符串 |
| `AppVersion_Default_MatchesSystemConstants` | DP 默认值等于 SystemConstants |
| `CurrentUserName_Set_ReflectsNewValue` | DP 赋值后可读取 |
| `AppVersion_Set_ReflectsNewValue` | DP 赋值后可读取 |

**预估代码量**: GlobalStatusBar.xaml.cs +20行; GlobalStatusBar.xaml +15行; Shell 绑定 +5行; 测试 ~50行

---

## 4. Phase C: NFR-AVAIL-001 - 本地数据库启动自动备份

> **实际实现偏差**: 本地模式已从 SQLite 迁移到 SQL Server LocalDB (Sprint 2 完成)。
> Phase C 的实际实现使用 `ILocalDbBackupService` + `LocalDbBackupService`，
> 通过 T-SQL `BACKUP DATABASE` 执行备份，而非设计文档中的 `ISqliteBackupService` + `File.Copy`。
> 备份文件格式为 `.bak` (位于 `%AppData%/LYBTZYZS/Backup/lybt_{yyyyMMdd}.bak`)，而非 `.db`。
> 触发时机为登录成功后 (LoginCoordinator.LoginLocalAsync) 而非应用启动时。

### 4.1 设计 (原始设计，实际实现见上方偏差说明)

#### 接口 (`ISqliteBackupService`)

位置: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/ISqliteBackupService.cs`

```csharp
/// <summary>
/// SQLite 数据库启动备份服务
/// NFR-AVAIL-001: 应用启动时自动备份，保留最近 7 天
/// </summary>
public interface ISqliteBackupService
{
    /// <summary>执行备份。若今日已有备份则跳过。</summary>
    Task BackupAsync(CancellationToken cancellationToken = default);

    /// <summary>清理超过 retentionDays 天的旧备份文件。</summary>
    Task CleanupOldBackupsAsync(int retentionDays = 7, CancellationToken cancellationToken = default);
}
```

#### 实现 (`SqliteBackupService`)

位置: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/SqliteBackupService.cs`

```
关键逻辑:
1. Source: DatabaseInitializer.DatabasePath
2. BackupDir: Path.Combine(Path.GetDirectoryName(DatabasePath)!, "Backup")
3. BackupFile: lybt_{DateTime.Today:yyyyMMdd}.db
4. 若 BackupFile 已存在 → 跳过 (幂等)
5. Directory.CreateDirectory(BackupDir) 确保目录存在
6. File.Copy(source, dest, overwrite: false)
7. CleanupOldBackupsAsync: 枚举 Backup/*.db，删除 > 7天的文件
   基于 File.GetCreationTime 或文件名日期解析
```

```csharp
public sealed class SqliteBackupService : ISqliteBackupService
{
    private readonly ILogger<SqliteBackupService> _logger;
    private static string BackupDirectory =>
        Path.Combine(Path.GetDirectoryName(DatabaseInitializer.DatabasePath)!, "Backup");

    public async Task BackupAsync(CancellationToken cancellationToken = default)
    {
        var source = DatabaseInitializer.DatabasePath;
        if (!File.Exists(source)) return; // 数据库未初始化时跳过

        var backupDir = BackupDirectory;
        Directory.CreateDirectory(backupDir);

        var backupFile = Path.Combine(backupDir, $"lybt_{DateTime.Today:yyyyMMdd}.db");
        if (File.Exists(backupFile))
        {
            _logger.LogDebug("今日备份已存在，跳过: {BackupFile}", backupFile);
            return;
        }

        await Task.Run(() => File.Copy(source, backupFile, overwrite: false), cancellationToken);
        _logger.LogInformation("SQLite 备份完成: {BackupFile}", backupFile);
    }

    public async Task CleanupOldBackupsAsync(int retentionDays = 7,
        CancellationToken cancellationToken = default)
    {
        var backupDir = BackupDirectory;
        if (!Directory.Exists(backupDir)) return;

        var cutoff = DateTime.Today.AddDays(-retentionDays);
        var files = Directory.GetFiles(backupDir, "lybt_*.db");

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryParseDateFromFilename(file, out var fileDate) && fileDate < cutoff)
            {
                File.Delete(file);
                _logger.LogDebug("删除过期备份: {File}", file);
            }
        }

        await Task.CompletedTask; // 保持接口 async 签名
    }

    private static bool TryParseDateFromFilename(string filePath, out DateTime date)
    {
        // 文件名格式: lybt_20260309.db
        var name = Path.GetFileNameWithoutExtension(filePath);
        if (name.Length >= 13 && name.StartsWith("lybt_"))
        {
            return DateTime.TryParseExact(name[5..], "yyyyMMdd",
                null, System.Globalization.DateTimeStyles.None, out date);
        }
        date = default;
        return false;
    }
}
```

#### 集成到启动流程

位置: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/DatabaseInitializer.cs`

在 `InitializeAsync` 完成后 (仅本地模式)，在 Shell 的 `LocalModeStartupStep` 中触发：

```csharp
// 不阻塞启动流程，fire-and-forget 模式
_ = Task.Run(async () =>
{
    try
    {
        await _backupService.BackupAsync();
        await _backupService.CleanupOldBackupsAsync(retentionDays: 7);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "SQLite 备份失败，不影响正常启动");
    }
}, CancellationToken.None);
```

### 4.2 测试

`tests/LYBT.Tests.Desktop/PureLogic/LocalData/SqliteBackupServiceTests.cs`

| 测试 | 说明 |
|------|------|
| `BackupAsync_WhenDatabaseExists_CreatesBackupFile` | 正常备份创建 |
| `BackupAsync_WhenDatabaseMissing_SkipsGracefully` | 数据库不存在时不抛 |
| `BackupAsync_WhenTodayBackupExists_SkipsDuplicate` | 幂等性 |
| `CleanupOldBackupsAsync_RemovesFilesOlderThan7Days` | 7天外文件删除 |
| `CleanupOldBackupsAsync_KeepsRecentFiles` | 7天内文件保留 |
| `TryParseDateFromFilename_ValidFormat_ParsesCorrectly` | 文件名日期解析 |

**预估代码量**: ISqliteBackupService.cs ~15行; SqliteBackupService.cs ~80行; 集成 +10行; 测试 ~120行

---

## 5. Phase D: v1.0-rc 验收

### 5.1 测试目标

- Desktop: 482 + ~20 (Phase A/B/C 新增) ≈ 500+
- Server: 1185 (保持不变)
- Architecture: 76 (可能 +2~4 新规则)

### 5.2 架构测试检查点

验证以下 Sprint 4 新增类型是否满足架构规则：
- `EditModeStateMachine` 所在层级 (Desktop.MedicalCase 模块内)
- `SyncPhase` / `SyncResultSummary` (Desktop.Sync 模块内)
- 无需新增架构规则，但需确认现有规则通过

### 5.3 文档更新

- `roadmap.md`: 新增 Sprint 5 记录，关闭 CODE-25/CODE-21
- `docs/03-architecture/05-dual-mode.md`: 更新本地模式备份说明 (如需)
- `docs/02-requirements/17-nfr.md`: 更新 NFR-AVAIL-001 验收标准 checkbox

---

## 6. 工作量估算

| Phase | 文件数 | 代码行数 | 测试数 | 估时 |
|-------|--------|---------|--------|------|
| A: TokenExpired | 4 | ~70 行 | 5 | 2h |
| B: StatusBar | 3 | ~90 行 | 4 | 2h |
| C: SQLite 备份 | 3 | ~200 行 | 6 | 3h |
| D: 验收 | 2 | docs | - | 1h |
| **合计** | **12** | **~360 行** | **~15** | **~8h** |

---

## 7. 风险与依赖

| 风险 | 可能性 | 影响 | 缓解 |
|------|--------|------|------|
| Shell 事件订阅时序问题 (Phase B) | 低 | 用户名不显示 | 使用 EventAggregator 模式, 确认 LoginSucceeded 事件顺序 |
| SQLite 文件锁定 (Phase C) | 低 | 备份失败 | fire-and-forget + try-catch + 仅在启动初期执行 |
| Architecture 测试失败 (Phase D) | 低 | 需补规则或调整代码 | 提前检查 Sprint 4 新增类型 |

---

## 8. Sprint 5 完成标准 (v1.0-rc Exit Criteria)

- [ ] CODE-25 关闭: `UnauthorizedException.TokenExpired()` 实现并有测试
- [ ] CODE-21 关闭: 状态栏显示用户名和版本号
- [ ] NFR-AVAIL-001 满足: 启动时备份 SQLite，7 天保留
- [ ] 全量测试通过 (Server + Desktop + Architecture)
- [ ] 所有 CRITICAL/HIGH 审计项已关闭 (已在 Sprint 3/4 完成)
- [ ] roadmap.md 更新为 v1.0-rc 完成状态

---

## 变更记录

| 日期 | 版本 | 内容 |
|------|------|------|
| 2026-03-09 | v1.0 | 初始设计，基于 Sprint 4 完成后的调研结果 |
