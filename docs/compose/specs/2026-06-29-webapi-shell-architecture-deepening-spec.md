# WebAPI + Shell 架构深化规格

> 日期: 2026-06-29
> 状态: 草稿
> 范围: WebAPI 项目设计 + Shell 代码层架构
> 目标: 固定 solution 基础架构，业务先放一放

## [S1] WebAPI 方法级审计

### 1.1 胖控制器问题（W1）

**UsersController (610 行)** — 方法清单:

| 方法 | 行号 | 职责 | 问题 |
|------|------|------|------|
| `GetCurrentUserId()` | ~70 | 从 Claims 提取用户 ID | 应提取到 BaseApiController |
| `IsAdmin()` | ~75 | 检查是否管理员 | 应提取到 BaseApiController |
| `ParseUserRole()` | ~80 | Identity 角色字符串→枚举 | 应提取到共享工具类 |
| `MapUserRoleToString()` | ~90 | 枚举→Identity 角色字符串 | 应提取到共享工具类 |
| `CanManageUser()` | ~100 | 角色层级权限检查 | 应提取到服务层 |
| `MapToDetailDtoAsync()` | ~120 | ApplicationUser→UserDetailDto | 应使用 Mapperly |
| `GetList()` | ~150 | 分页查询+内存过滤 | 应在数据库层过滤 |
| `Create()` | ~250 | 创建用户+角色分配 | 业务逻辑在 Controller |
| `Update()` | ~350 | 更新用户+角色同步 | 业务逻辑在 Controller |
| `ResetPassword()` | ~450 | 重置密码+返回临时密码 | 业务逻辑在 Controller |
| `BatchDelete()` | ~550 | 批量删除+逐项权限检查 | 业务逻辑在 Controller |

**MedicalCasesController (WebAPI, ~500 行)** — 方法清单:

| 方法 | 行号 | 职责 | 问题 |
|------|------|------|------|
| `GetCurrentUserId()` | ~45 | 从 Claims 提取用户 ID | 与 LocalWebAPI 重复 |
| `IsAdmin()` | ~50 | 检查是否管理员 | 与 LocalWebAPI 重复 |
| `GetDoctorFilter()` | ~55 | 医生过滤逻辑 | 应提取到服务层 |
| `GetList()` | ~70 | 分页查询+权限过滤 | 业务逻辑在 Controller |
| `GetById()` | ~90 | 获取详情+组装响应 | 应使用 Facade |
| `Create()` | ~130 | 创建医案 | 业务逻辑在 Controller |
| `Save()` | ~145 | 保存医案 | 业务逻辑在 Controller |
| `Delete()` | ~155 | 删除医案+回滚挂号 | 业务逻辑在 Controller |
| `CloseCase()` | ~175 | 完成医案 | 业务逻辑在 Controller |
| `SuspendCase()` | ~185 | 挂起医案 | 业务逻辑在 Controller |
| `CancelCase()` | ~195 | 取消医案 | 业务逻辑在 Controller |

### 1.2 Controller 重复问题（W2）

**重复方法清单**（WebAPI vs LocalWebAPI）:

| 方法 | WebAPI 位置 | LocalWebAPI 位置 | 重复度 |
|------|------------|-----------------|--------|
| `GetCurrentUserId()` | UsersController:70 | UsersController:77 | 100% |
| `IsAdmin()` | UsersController:75 | UsersController:82 | 100% |
| `ParseUserRole()` | UsersController:80 | UsersController:87 | 100% |
| `MapUserRoleToString()` | UsersController:90 | UsersController:97 | 100% |
| `CanManageUser()` | UsersController:100 | UsersController:107 | 95% |
| `MapToDetailDtoAsync()` | UsersController:120 | UsersController:127 | 100% |
| `GetCurrentUserId()` | MedicalCasesController:45 | MedicalCasesController:30 | 100% |
| `IsAdmin()` | MedicalCasesController:50 | MedicalCasesController:33 | 100% |

**总重复代码量**: ~600 行

### 1.3 安全问题（W4/W5/W6）

| 问题 | 位置 | 行号 | 严重度 |
|------|------|------|--------|
| 生产配置验证禁用 | WebAPI Program.cs | 172 | HIGH |
| 硬编码密码回退 | WebAPI Program.cs | ~100 | CRITICAL |
| 硬编码密码回退 | LocalWebAPI Program.cs | ~80 | CRITICAL |
| 硬编码 JWT 密钥 | LocalWebAPI AuthController | 158 | CRITICAL |

---

## [S2] Shell 方法级审计

### 2.1 MainWindowViewModel (1025 行) — 方法清单

| 区域 | 方法 | 行号 | 职责 | 问题 |
|------|------|------|------|------|
| 依赖服务 | 构造函数 | 285-329 | 14 个依赖注入 | 合理但可拆分 |
| 可观察属性 | `Title` | 73 | 窗口标题 | 正常 |
| 可观察属性 | `CurrentUser` | 80 | 当前用户 | 正常 |
| 可观察属性 | `IsLoggedIn` | 87 | 登录状态 | 正常 |
| 可观察属性 | `ApiStatus` | 94 | API 健康状态 | 正常 |
| 可观察属性 | `ConnectionUrl` | 101 | 连接 URL | 正常 |
| 可观察属性 | `IsDarkMode` | 108 | 暗色主题 | 正常 |
| 可观察属性 | `NavigationItems` | 115 | 导航项 | 正常 |
| 可观察属性 | `SelectedNavItem` | 122 | 选中导航项 | 正常 |
| 计算属性 | `DisplayName` | 203 | 显示名称 | 正常 |
| 计算属性 | `RoleDisplay` | 210 | 角色显示 | 正常 |
| 计算属性 | `ApiStatusIcon` | 220 | API 状态图标 | 正常 |
| 计算属性 | `GroupedNavItems` | 240 | 分组导航项 | 正常 |
| 委托命令 | 16 个命令 | 333-422 | 委托给 MenuManager | 合理的委托模式 |
| 事件处理 | `OnTick` | 543 | 时钟更新 | 正常 |
| 事件处理 | `OnHealthStatusChanged` | 560 | 健康状态变更 | 正常 |
| 事件处理 | `OnConnectionUrlChanged` | 580 | URL 变更 | 正常 |
| 事件处理 | `OnSessionExpired` | 620 | 会话过期 | 正常 |
| 事件处理 | `OnTokenLifecycleStateChanged` | 640 | Token 生命周期 | 正常 |
| 事件处理 | `OnLoginCoordinatorSuccess` | 660 | 登录成功 | 正常 |
| 业务逻辑 | `BuildNavigationItems` | 734-801 | 构建导航项 | 应提取到 NavigationManager |
| 业务逻辑 | `HandleTokenExpired` | 810 | 处理 Token 过期 | 应提取到 TokenManager |
| 业务逻辑 | `PerformLogoutAsync` | 830 | 执行登出 | 应提取到 LoginManager |
| 业务逻辑 | `CheckLoginStatusAsync` | 860 | 检查登录状态 | 应提取到 LoginManager |

### 2.2 StartupPipeline (412 行) — 方法清单

| 方法 | 行号 | 职责 | 问题 |
|------|------|------|------|
| `ExecuteAsync()` | 50 | 执行管道 | 正常 |
| `ExecuteStepAsync()` | 120 | 执行单步 | 正常 |
| `ExecuteParallelGroup()` | 180 | 并行执行组 | 正常 |
| `GetDiagnostics()` | 300 | 获取诊断信息 | 正常 |

### 2.3 LoginCoordinator (315 行) — 方法清单

| 方法 | 行号 | 职责 | 问题 |
|------|------|------|------|
| `LoginAsync()` | 50 | 登录流程 | 正常 |
| `HandleLoginSuccess()` | 150 | 处理登录成功 | 正常 |
| `HandleLoginFailure()` | 200 | 处理登录失败 | 正常 |
| `LogoutAsync()` | 250 | 登出流程 | 正常 |

### 2.4 MenuManager (328 行) — 方法清单

| 方法 | 行号 | 职责 | 问题 |
|------|------|------|------|
| 构造函数 | 30-67 | 初始化命令+可见性 | 正常 |
| `ExecuteQuickAddPatientAsync` | 100 | 快速添加患者 | 正常 |
| `ExecuteQuickStartMedicalCaseAsync` | 130 | 快速开始医案 | 正常 |
| `ExecuteToggleThemeAsync` | 259-287 | 切换主题 | **与 ThemeService 重复** |
| `ApplyDarkTheme` | 290-310 | 应用暗色主题 | **硬编码颜色** |
| `ApplyLightTheme` | 312-327 | 应用亮色主题 | **硬编码颜色** |

---

## [S3] WebAPI 架构深化方案

### 方案: 提取共享基类 + 服务层

**Step 1: 提取 BaseClaimsHelper**

```csharp
// 位置: LYBT.Infrastructure/Web/BaseClaimsHelper.cs
public static class BaseClaimsHelper
{
    public static Guid GetCurrentUserId(ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
    
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
    }
    
    public static UserRole GetCurrentUserRole(ClaimsPrincipal user)
        => Enum.TryParse<UserRole>(user.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : UserRole.Receptionist;
    
    public static UserRole ParseUserRole(IList<string> identityRoles)
    {
        // 统一的角色解析逻辑
    }
    
    public static string MapUserRoleToString(UserRole role)
    {
        // 统一的角色映射逻辑
    }
    
    public static bool CanManageUser(UserRole current, UserRole target, bool isTargetSysAdmin)
    {
        // 统一的权限检查逻辑
    }
}
```

**Step 2: 提取 UsersController 业务逻辑到 UserService**

```csharp
// 位置: LYBT.Module.Users/Services/UserService.cs
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    
    public async Task<Result<UserDetailDto>> CreateAsync(UserInputDto dto)
    {
        // 业务逻辑: 保留用户名检查、角色权限、密码生成
    }
    
    public async Task<Result> UpdateAsync(Guid id, UserInputDto dto)
    {
        // 业务逻辑: Sysadmin 保护、角色同步
    }
    
    public async Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id)
    {
        // 业务逻辑: 密码生成、强制改密标记
    }
    
    public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId)
    {
        // 业务逻辑: 逐项权限检查、批量删除
    }
}
```

**Step 3: 提取共享 Controller 基类**

```csharp
// 位置: LYBT.Infrastructure/Web/BaseUsersController.cs
public abstract class BaseUsersController : BaseApiController
{
    protected readonly IUserService _userService;
    
    protected BaseUsersController(IUserService userService, ILogger logger) : base(logger)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public virtual async Task<IActionResult> GetList([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? keyword)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, keyword);
        return HandleResult(result);
    }
    
    [HttpGet("{id}")]
    public virtual async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return HandleResult(result);
    }
    
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] UserInputDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return HandleResult(result, 201);
    }
    
    // ... 其他共享端点
}
```

**Step 4: 启用生产配置验证**

```csharp
// WebAPI Program.cs
// 修改前:
// if (false && builder.Environment.IsProduction())

// 修改后:
if (builder.Environment.IsProduction())
{
    // 启用生产配置验证
}
```

**Step 5: 移除硬编码密码**

```csharp
// WebAPI Program.cs
// 修改前:
var defaultPassword = configuration["DefaultPasswords:AdminPassword"] ?? "Lybt2025@TempPass!";

// 修改后:
var defaultPassword = configuration["DefaultPasswords:AdminPassword"] 
    ?? throw new InvalidOperationException("DefaultPasswords:AdminPassword 配置缺失");
```

---

## [S4] Shell 架构深化方案

### 方案: 拆分 MainWindowViewModel + 清理反模式

**Step 1: 提取 StatusBarManager**

```csharp
// 位置: LYBT.Desktop.Shell/Services/StatusBarManager.cs
public class StatusBarManager : IStatusBarManager
{
    private readonly IApiHealthMonitor _healthMonitor;
    private readonly IConnectionSettingsService _connectionSettings;
    private readonly IConnectionModeService _connectionMode;
    
    public string ApiStatusIcon { get; }
    public string ApiStatusColor { get; }
    public string ConnectionUrl { get; }
    public string ConnectionModeDisplay { get; }
    
    public void Initialize()
    {
        // 订阅事件、更新状态栏
    }
}
```

**Step 2: 提取 NavigationManager**

```csharp
// 位置: LYBT.Desktop.Shell/Services/NavigationManager.cs
public class NavigationManager : INavigationManager
{
    private readonly IRoleRegistry _roleRegistry;
    
    public ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)
    {
        // 从 RoleRegistry 构建导航项
    }
    
    public void OnNavItemSelected(NavigationItem item)
    {
        // 处理导航项选择
    }
}
```

**Step 3: 提取 ThemeManager**

```csharp
// 位置: LYBT.Desktop.Shell/Services/ThemeManager.cs
public class ThemeManager : IThemeManager
{
    private readonly IThemeService _themeService;
    
    public void ToggleTheme()
    {
        _themeService.ToggleTheme();
        // 移除 MenuManager 中的重复逻辑
    }
}
```

**Step 4: 移除 ContainerLocator 反模式**

```csharp
// MainWindow.xaml.cs
// 修改前:
ContainerLocator.Current.Resolve<ISnackbarMessageQueue>()

// 修改后:
// 在构造函数中注入 ISnackbarMessageQueue
public MainWindow(ISnackbarMessageQueue snackbarMessageQueue)
{
    _snackbarMessageQueue = snackbarMessageQueue;
}
```

**Step 5: 移除反射**

```csharp
// ApplicationInitializationService.cs
// 修改前:
var loadedModules = typeof(ModuleCatalog).GetProperty("LoadedModules", BindingFlags.NonPublic)

// 修改后:
// 使用 IModuleCatalog.Modules 属性（公共 API）
var loadedModules = _moduleCatalog.Modules;
```

**Step 6: 清理死代码**

- 删除 `StartupPerformanceMonitor.cs`（未使用）
- 移动 `TodayPatientItem.cs` 到 Patients 模块

---

## [S5] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | W5/W6: 移除硬编码密码/密钥 | 低 | 低 |
| 2 | W4: 启用生产配置验证 | 低 | 低 |
| 3 | W1: 提取 BaseClaimsHelper + UserService | 高 | 中 |
| 4 | W2: 提取共享 Controller 基类 | 中 | 中 |
| 5 | S2: 移除 ContainerLocator 反模式 | 低 | 低 |
| 6 | S3: 统一主题逻辑 | 低 | 低 |
| 7 | S4: 移除反射 | 低 | 低 |
| 8 | S1: 拆分 MainWindowViewModel | 高 | 中 |
| 9 | S5/S6: 清理死代码 | 低 | 低 |

## [S6] 验收标准

1. **WebAPI**: UsersController 行数 < 200（当前 610）
2. **WebAPI**: WebAPI/LocalWebAPI 重复代码减少 80%+
3. **WebAPI**: 无硬编码密码/密钥
4. **WebAPI**: 生产配置验证启用
5. **Shell**: MainWindowViewModel 行数 < 400（当前 1025）
6. **Shell**: 无 ContainerLocator 使用
7. **Shell**: 无反射使用
8. **Shell**: 主题逻辑统一（仅 ThemeService）
9. **Shell**: 无死代码
10. **所有现有测试通过**
