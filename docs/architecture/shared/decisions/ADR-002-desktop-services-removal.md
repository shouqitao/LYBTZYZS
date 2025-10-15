# ADR-002: Desktop.Services 层移除与 Repository 注册位置标准

**状态**: 已接受
**日期**: 2025-10-12
**决策者**: 项目架构团队

## 背景

在 Desktop 端架构演进过程中,我们发现 `LYBT.Desktop.Services` 层存在以下问题：

### 1. 职责不清

`Desktop.Services` 层包含了两类性质完全不同的服务：

- **Infrastructure Service** (基础设施服务): 如 `NavigationService`、`SessionManager`、`NotificationService` 等,这些服务是应用程序级别的,应该由 Shell 统一管理
- **Repository** (数据访问层): 如 `UserRepository`、`PatientRepository` 等,这些服务是业务模块级别的,应该由各模块自行管理

两类服务混在一起,导致职责边界模糊,违反了单一职责原则。

### 2. 依赖关系混乱

- Infrastructure Service 应该在 Shell 层注册,供所有模块使用
- Repository 应该在业务模块内注册,实现模块自治
- 混在一起导致依赖关系不清晰,模块间耦合度高

### 3. 命名空间污染

多个服务使用了相同的命名空间 `LYBT.Desktop.Services`,但它们的实际位置和职责却不同：
- 有些在 `Desktop.Infrastructure`
- 有些在 `Desktop.Shell`
- 有些在各业务模块内

这导致命名空间与物理结构不一致,增加了理解成本。

### 4. 重复定义

发现 Shell 层和 Presentation 层存在完全相同的 `INotificationService` 接口和 `NotificationService` 实现,造成代码重复。

## 决策

**我们决定移除 `LYBT.Desktop.Services` 独立层,按职责重新组织服务：**

### 1. Infrastructure Service → Shell 统一注册

**定义**: 应用程序级别的基础设施服务,所有模块都依赖

**位置**:
- 接口与实现: `LYBT.Desktop.Infrastructure.Services.*` 或 `LYBT.Desktop.Shell.Services.*`
- 注册位置: `LYBT.Desktop.Shell/App.xaml.cs` 或 `ShellModule.cs`

**示例**:
```csharp
// LYBT.Desktop.Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // Foundation 层服务（Shell 统一注册）
    containerRegistry.RegisterSingleton<INavigationService, EnhancedNavigationService>();
    containerRegistry.RegisterSingleton<IDialogService, PrismDialogService>();
    containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
    containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
    containerRegistry.RegisterSingleton<INotificationService, NotificationService>();

    // Infrastructure 层服务（Shell 统一注册）
    containerRegistry.RegisterSingleton<ILogger<T>, Logger<T>>();
    containerRegistry.RegisterSingleton<ICacheService, MemoryCacheService>();
    containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();
}
```

### 2. Repository → 模块自行注册

**定义**: 业务模块级别的数据访问层,封装 API 调用逻辑

**位置**:
- 接口: `LYBT.Desktop.{模块}.Interfaces/IXxxRepository.cs`
- 实现: `LYBT.Desktop.{模块}.Repositories/XxxRepository.cs`
- 注册位置: `LYBT.Desktop.{模块}/{模块名}Module.cs` 的 `RegisterTypes` 方法

**示例**:
```csharp
// LYBT.Desktop.Prescriptions/PrescriptionsModule.cs
namespace LYBT.Desktop.Prescriptions;

[Module(ModuleName = nameof(PrescriptionsModule))]
[ModuleDependency("ConsultationModule")]
[ModuleDependency("HerbsModule")]
[ModuleDependency("FormulaModule")]
public class PrescriptionsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IPrescriptionRepository, PrescriptionRepository>();

        // 注册 ViewModel
        containerRegistry.Register<PrescriptionManagementViewModel>();
        containerRegistry.Register<PrescriptionsMainViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();
        containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();

        // 注册对话框
        containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
        containerRegistry.RegisterDialog<Views.HerbSelectionDialog, HerbSelectionDialogViewModel>();
    }
}
```

### 3. 服务分层总结

| 层级 | 位置 | 注册位置 | 职责 | 示例 |
|------|------|---------|------|------|
| **Foundation 层** | `Desktop.Infrastructure` 或 `Desktop.Shell` | Shell 统一注册 | 应用程序基础功能 | NavigationService, DialogService, SessionManager |
| **Infrastructure 层** | `Desktop.Infrastructure` | Shell 统一注册 | 横切关注点 | Logger, CacheService, ConfigurationService |
| **Repository 层** | `Desktop.{模块}.Repositories` | **模块自行注册** | 数据访问与 API 调用 | UserRepository, PatientRepository, PrescriptionRepository |

## 常见误区与错误示例

### ❌ 错误示例 1: 在 Shell 中注册 Repository

```csharp
// ❌ 错误：不要在 Shell 中注册 Repository
// LYBT.Desktop.Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ❌ 这样会导致 Shell 依赖具体业务模块,破坏模块独立性
    containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();
    containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();
}
```

**问题**:
- Shell 应该是轻量级的,不应该依赖具体业务模块
- 违反了模块自治原则
- 增加了 Shell 层的复杂度和依赖

### ❌ 错误示例 2: ViewModel 直接注入 API 接口

```csharp
// ❌ 错误：不要在 ViewModel 中直接注入 API
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserApi _userApi; // ❌ 不应该直接注入 API

    public UserManagementViewModel(IUserApi userApi)
    {
        _userApi = userApi;
    }

    public async Task LoadUsersAsync()
    {
        var users = await _userApi.GetAllUsersAsync(); // ❌ 不应该直接调用 API
        Items = new ObservableCollection<UserItem>(users);
    }
}
```

**问题**:
- 违反了三层架构原则（View → ViewModel → Repository → API）
- ViewModel 直接依赖 API,耦合度高
- 无法进行单元测试（难以 Mock API）
- 无法统一处理数据缓存、重试等逻辑

### ✅ 正确示例: 通过 Repository 访问数据

```csharp
// ✅ 正确：ViewModel 通过 Repository 访问数据
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserRepository _userRepository; // ✅ 注入 Repository
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserManagementViewModel> _logger;

    public UserManagementViewModel(
        IUserRepository userRepository,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<UserManagementViewModel> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "正在加载用户列表...";

            // ✅ 通过 Repository 获取数据
            var users = await _userRepository.GetAllAsync();

            // ✅ 使用 AutoMapper 转换为 UI Model
            Items = new ObservableCollection<UserItem>(_mapper.Map<List<UserItem>>(users));

            _notificationService.ShowSuccess("用户列表加载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户列表失败");
            _notificationService.ShowError($"加载用户列表失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

## Repository 设计约定

### 1. 返回值约定

**Desktop Repository 返回裸类型,不包装 ServiceResult**:

```csharp
// ✅ Desktop Repository - 返回裸类型
public interface IUserRepository
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> AddAsync(CreateUserDto dto);
    Task UpdateAsync(int id, UpdateUserDto dto);
    Task DeleteAsync(int id);
}
```

**原因**:
- Server 端需要返回统一的 HTTP 响应格式,所以使用 `ServiceResult<T>`
- Desktop 端 Repository 仅负责数据访问,异常由 ViewModel 处理
- 简化代码,避免不必要的包装拆包

### 2. 异常处理约定

**Repository 不处理异常,仅记录日志后向上抛出**:

```csharp
public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserRepository> _logger;

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有用户...");
            return await _userApi.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有用户失败");
            throw; // 异常向上抛出,由 ViewModel 处理
        }
    }
}
```

**由 ViewModel 处理异常并显示给用户**:

```csharp
public async Task LoadUsersAsync()
{
    try
    {
        IsBusy = true;
        var users = await _userRepository.GetAllAsync();
        Items = new ObservableCollection<UserItem>(users);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载用户失败");
        _notificationService.ShowError($"加载用户失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

## 后果

### 积极影响

1. **职责更清晰**
   - Infrastructure Service 由 Shell 统一管理,全局可用
   - Repository 由模块自行管理,实现模块自治
   - 符合单一职责原则和关注点分离原则

2. **依赖关系更合理**
   - Shell 层轻量化,不依赖具体业务模块
   - 模块间通过事件通信,降低耦合
   - 依赖方向清晰: View → ViewModel → Repository → API

3. **命名空间更规范**
   - `LYBT.Desktop.Infrastructure.Services.*` - 基础设施服务
   - `LYBT.Desktop.Shell.Services.*` - Shell 层服务
   - `LYBT.Desktop.{模块}.Repositories.*` - 业务模块 Repository

4. **模块更独立**
   - 新增模块时,只需在模块内注册 Repository
   - 无需修改 Shell 或其他模块
   - 模块可以独立测试和部署

5. **代码更易维护**
   - 消除了重复定义（如 NotificationService）
   - 减少了命名空间污染
   - 降低了理解成本

### 潜在风险

1. **迁移成本**
   - 需要调整现有模块的 Repository 注册位置
   - 需要更新架构测试用例
   - **缓解**: 提供详细的迁移指南和代码示例

2. **学习曲线**
   - 新开发者需要理解服务分层标准
   - **缓解**: 在文档中提供完整的代码示例和 FAQ

3. **一致性风险**
   - 可能有开发者不遵循标准,仍然在 Shell 中注册 Repository
   - **缓解**: 通过架构测试强制执行（`All_Repositories_Should_Be_Registered_In_Modules` 测试）

## 实施方案

### Phase 1: 修正现有问题（已完成）

✅ **Issue #1211**: 修正命名空间污染
- 将 Infrastructure 和 Shell 服务的命名空间修正为正确位置
- 影响文件: 5 个服务文件

✅ **Issue #1212**: 统一通知服务
- 删除 Shell 层重复的 NotificationService
- 保留 Presentation 层的实现

✅ **Issue #1213**: 更新架构测试
- 删除 `Desktop.Services` 相关测试
- 新增 `All_Repositories_Should_Be_Registered_In_Modules` 测试

✅ **Issue #1214**: 清理空目录
- 删除 `Desktop.Services` 下的空目录

### Phase 2: 文档完善（进行中）

✅ **Issue #1216**: 创建 Desktop 架构标准文档
- 完整的三层架构设计说明
- Repository、ViewModel、View 设计规范
- 代码示例和 FAQ

🔄 **Issue #1217**: 创建 ADR-002 文档（本文档）
- 明确服务分层标准
- 说明 Repository 注册位置
- 提供正确和错误示例

### Phase 3: 持续优化（规划中）

⏳ **Issue #1219**: 添加代码模板和 Snippet
- Repository 接口与实现模板
- Module 注册模板
- ViewModel 模板

⏳ **Issue #1220**: Desktop 端单元测试补充
- Repository 单元测试示例
- ViewModel 单元测试示例

## 评估指标

每季度评估一次,检查以下指标：

1. **架构测试通过率**: 必须 100%
2. **模块独立性**: 新增模块无需修改 Shell
3. **代码重复率**: `Desktop.Services` 相关代码无重复
4. **开发效率**: 新增 Repository 平均耗时 < 30 分钟

## 参考资料

1. [Desktop 深度分析报告](../../reports/desktop-deep-analysis-2025-10-12.md) - §1.2, §7.1
2. [Desktop 架构标准文档](../../../src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md)
3. [Client 端统一设计标准](../client/unified-design-standard.md)
4. [架构测试用例](../../../tests/Architecture/DesktopLayerArchTests.cs)
5. [ADR-001: 拒绝过度工程](ADR-001-reject-overengineering.md)

## 引用

> "关注点分离是软件工程中最重要的原则之一" - Edsger W. Dijkstra

> "模块化设计的核心是高内聚、低耦合" - Larry Constantine

## 签署

- 架构决策者：[签名]
- 日期：2025-10-12
- 审核状态：已通过

---

**注**: 本决策具有约束力,所有开发人员和 AI 助手必须遵守。任何违反此决策的代码提交都将被 CI/CD 流程拦截（通过架构测试 `All_Repositories_Should_Be_Registered_In_Modules`）。
