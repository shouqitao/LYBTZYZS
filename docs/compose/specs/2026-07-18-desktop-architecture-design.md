# Desktop 前端架构设计规范

> **文档版本**: v2.0
> **基于**: 2026-07-18 五并行 Agent 深度分析
> **适用范围**: LYBTZYZS Desktop 全层架构
> **状态**: 架构基线文档

---

## 目录

- [1. 架构总览](#1-架构总览)
- [2. Core 基础设施层设计](#2-core-基础设施层设计)
- [3. Business Modules 层设计](#3-business-modules-层设计)
- [4. Roles 角色工作台层设计](#4-roles-角色工作台层设计)
- [5. Shell 入口层设计](#5-shell-入口层设计)
- [6. Shared 共享层设计](#6-shared-共享层设计)
- [7. 设计决策记录 (ADR)](#7-设计决策记录-adr)
- [8. 架构改进路线图](#8-架构改进路线图)
- [9. 编码规范速查](#9-编码规范速查)

---

## 1. 架构总览

### 1.1 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行时 |
| WPF | - | UI 框架 |
| Prism.DryIoc | 8.x+ | 模块化 + DI + 导航 |
| CommunityToolkit.Mvvm | 8.x | MVVM 源生成器 |
| Riok.Mapperly | 3.x | 编译期对象映射 |
| Refit | 7.x | 类型安全 HTTP 客户端 |
| EF Core | 8.x | 本地数据访问 (LocalDB) |
| MaterialDesignThemes | 5.x | UI 主题 |
| Serilog | - | 日志框架 |
| QuestPDF | - | PDF 导出 |

### 1.2 架构分层

```
┌─────────────────────────────────────────────────────────┐
│                      Shell 入口层                         │
│  App.xaml.cs → StartupPipeline → MainWindow              │
│  DI 注册 / 模块加载 / 双模式切换 / 断路器                  │
├─────────────────────────────────────────────────────────┤
│                    Roles 角色工作台层                       │
│  Admin / Clinical / Receptionist / Sysadmin              │
│  薄包装 View + 导航路由 + 角色驱动模块加载                   │
├─────────────────────────────────────────────────────────┤
│                   Business Modules 业务模块层              │
│  Auth / Users / Patients / MedicalCase / Herbs /         │
│  Formula / Registration / Reports                        │
│  MasterDetail CRUD / 聚合根 DataManager / CommandHandler  │
├─────────────────────────────────────────────────────────┤
│                   Core 基础设施层                          │
│  Contracts ← Foundation ← Infrastructure ← Controls     │
│  Navigation / LocalData / Printing / CardReader          │
├─────────────────────────────────────────────────────────┤
│                   Shared 共享层                           │
│  Primitives ← Models ← Validators / Components / Config  │
└─────────────────────────────────────────────────────────┘
```

### 1.3 依赖方向规则

```
Shell → Roles → Modules → Core (Infrastructure → Foundation → Contracts)
                ↓
           Shared (DTO/枚举/验证)
```

**铁律**:
- Server 和 Client 不互相引用
- 模块间禁止直接引用（通过 Contracts 接口 + EventAggregator 通信）
- 依赖方向严格单向，零循环

### 1.4 核心设计模式

| 模式 | 应用位置 | 说明 |
|------|---------|------|
| **MVVM** | 全局 | View ← DataBinding → ViewModel → Repository → API |
| **策略模式** | SwitchingApiClient | 运行时 Remote/Local 透明切换 |
| **组合模式** | MasterDetailViewModelBase | 泛型组合 Loading/Pagination/Search/Selection/DetailEditor |
| **门面模式** | MedicalCaseService | 聚合 Query/Command/Lifecycle 三接口 |
| **CQRS-lite** | MedicalCase | Query/Command/Lifecycle 分离读写和状态流转 |
| **状态机** | EditModeStateMachine | ReadOnly ↔ Editing ↔ DirtyEditing |
| **断路器** | ApiHealthMonitor | Closed → Open → HalfOpen |
| **管道模式** | StartupPipeline | 步骤化启动 + 并行组 |
| **工厂模式** | CardReaderFactory | 按厂商创建读卡器适配器 |
| **薄包装** | Roles Views | 一行 XAML 嵌入业务 Control |

---

## 2. Core 基础设施层设计

### 2.1 依赖关系

```
LYBT.Desktop.Shared (net8.0, 零依赖)
       ↑
LYBT.Desktop.Contracts (net8.0;net8.0-windows 双目标)
  ↑          ↑
Foundation   LocalData (唯一引用 LYBT.Entities)
  ↑
┌──┼──────────┐
Controls  Infrastructure
  ↑         ↑
┌──┼──────────┐
Navigation  Printing  CardReader
```

### 2.2 各项目职责

| 项目 | 框架 | 职责 | 关键类型 |
|------|------|------|---------|
| **Shared** | net8.0 | 纯 DTO/record/枚举 | CommandResult, BreadcrumbItem, AuthState |
| **Contracts** | net8.0;net8.0-windows | 接口契约 | IApiClient, IXxxRepository, IViewModelServices, IWorkspaceHost |
| **Foundation** | net8.0-windows | 技术基础设施 | SwitchingApiClient, AuthenticationService, TokenManager, DesktopCacheManager |
| **Infrastructure** | net8.0-windows | ViewModel 基类 + WPF 服务 | CoreViewModelBase, NavigableViewModelBase, MasterDetailViewModelBase, RoleRegistry |
| **Controls** | net8.0-windows | 自定义控件 + 转换器 | DataGridToolbar, MasterDetailLayout, SearchBox, 22 个 Converter |
| **Navigation** | net8.0-windows | 导航服务 | NavigationCoordinator (防抖 300ms + 超时 10s + 懒加载) |
| **LocalData** | net8.0-windows | 本地数据访问 | LocalDbContext, 6 个 Mapperly 映射器 |
| **Printing** | net8.0-windows | 打印服务 | IPrintService\<T\>, QuestPDF PDF 导出 |
| **CardReader** | net8.0-windows | 身份证读卡器 | ICardReader (策略), CardReaderFactory (工厂) |

### 2.3 Contracts 双目标框架

```xml
<TargetFrameworks>net8.0;net8.0-windows</TargetFrameworks>
```

- **net8.0**: 排除 WPF 相关文件 (IViewModelServices, IUiThreadDispatcher, SyncEvents)，Server 端测试可引用
- **net8.0-windows**: 包含完整 Desktop 接口

### 2.4 ViewModel 基类体系

```
ObservableObject (CommunityToolkit.Mvvm)
  └── CoreViewModelBase
      │  - IsBusy, ErrorMessage, BusyMessage
      │  - SetBusy(), ExecuteWithErrorHandlingAsync()
      │  - ILogger, IEventAggregator, IViewModelServices
      │
      ├── NavigableViewModelBase
      │   │  + INavigationAware, IConfirmNavigationRequest
      │   │  + IEditable (BeginEdit/EndEdit/CancelEdit)
      │   │  + HasUnsavedChanges 检测
      │   │
      │   └── MasterDetailViewModelBase<TList, TDetail>
      │       │  + IMasterDetailServices<TList, TDetail> (组合注入)
      │       │  + Loading/Pagination/Search/Selection/DetailEditor/ErrorHandler
      │       │  + 5 个抽象方法: LoadListAsync, LoadDetailAsync, CreateNewDetail, SaveDetailAsync, DeleteItemAsync
      │       │
      └── DialogViewModelBase
          │  + IDialogAware
          │  + ConfirmCommand, CancelCommand
          │  + 参数提取

ObservableObject
  └── ChildViewModelBase
      │  + IWorkspaceHost (Composite VM 父子通信)
      │  + ILoggerFactory

ObservableObject
  └── ValidatableModelBase
      │  + INotifyDataErrorInfo
      │  + DataAnnotations 验证

ObservableObject
  └── HerbItemViewModelBase
      │  + IHerbItemEditable
      │  + 拼音码智能匹配
```

### 2.5 IViewModelServices 构造函数聚合器

```csharp
public interface IViewModelServices
{
    ILoggerFactory LoggerFactory { get; }
    IEventAggregator EventAggregator { get; }
    IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IUserNotificationService UserNotificationService { get; }
    ICommonDialogService CommonDialogService { get; }
    IToastService ToastService { get; }
    IRoleRegistry RoleRegistry { get; }
    IUiThreadDispatcher UiThreadDispatcher { get; }
}
```

**设计意图**: 将 9 个常用服务接口合并为 1 个构造函数参数，解决 ViewModel 胖构造函数问题。

### 2.6 SwitchingApiClient 运行时切换

```csharp
private IApiClient Current
{
    get {
        var url = _connectionSettings.CurrentUrl;
        if (_current is null || _currentUrl != url) {
            _current = _connectionSettings.IsLocal
                ? new HttpClientApiClient(_localHttpClientFactory(url))    // Local: localhost:5300
                : new RefitApiClient(_remoteHttpClientFactory(url), ...); // Remote: 远程 API
            _currentUrl = url;
        }
        return _current;
    }
}
```

**Repository 层完全无感** — 同一个 IApiClient 接口，运行时透明切换。

---

## 3. Business Modules 层设计

### 3.1 三种模块类型

| 类型 | 数据访问 | 特征 | 典型模块 |
|------|---------|------|----------|
| **独立实体** | Repository | 独立管理的实体，完整 CRUD | Users, Patients, Herbs |
| **聚合根** | Repository + DataManager | 管理聚合及其子实体生命周期 | MedicalCase |
| **从属实体** | CommandHandler / 直接引用 | 子实体，通过父聚合访问 | Formula (依赖 Herbs) |

### 3.2 模块目录结构标准

```
LYBT.Desktop.{模块名}/
├── {模块名}Module.cs              # Prism IModule 入口
├── Interfaces/                     # 接口定义
│   ├── I{Entity}Repository.cs
│   └── I{Entity}Service.cs
├── Repositories/                   # Repository 实现
│   └── {Entity}Repository.cs
├── Services/                       # 业务服务
│   ├── {Entity}Service.cs
│   └── Handlers/                   # 命令处理器
├── ViewModels/                     # ViewModel
│   ├── {Entity}MasterDetailViewModel.cs
│   └── {Entity}EditorViewModel.cs
├── Controls/                       # 业务控件 (非 View)
│   ├── {Entity}MasterDetailControl.xaml
│   ├── {Entity}EditControl.xaml
│   └── {Entity}ViewControl.xaml
├── Models/                         # UI 模型
│   ├── {Entity}DetailModel.cs
│   └── Items/
│       └── {Entity}Item.cs
├── Mappings/                       # Mapperly 映射器
│   └── {Entity}Mapper.cs
├── Dialogs/                        # 对话框 (可选)
└── Events/                         # 模块事件 (可选)
```

### 3.3 MedicalCase 聚合根设计

**CQRS 分离**:
```
IMedicalCaseService (门面接口)
  ├── IMedicalCaseQueryService      # 读操作
  ├── IMedicalCaseCommandService    # 写操作 + HasChanges 变更检测
  └── IMedicalCaseLifecycleService  # 状态流转 (Initialize/Suspend/Complete/Resume/Close)
```

**共享状态**:
```
MedicalCaseEditContext
  ├── CurrentDetail / OriginalDetail  # 变更检测快照
  ├── Consultation / Prescription     # 缓存的子实体
  └── MedicalCaseCloneMapper.Clone()  # 深拷贝 (UseDeepCloning=true)
```

**子 ViewModel 组合**:
```
MedicalCaseWorkspaceViewModel (父)
  ├── ConsultationEditorViewModel    (ChildViewModelBase) — 四诊采集+中医辨证
  ├── PrescriptionEditorViewModel    (ChildViewModelBase) — 处方编辑器
  └── MedicalCaseCommandsViewModel   (ChildViewModelBase) — 操作命令集
```

### 3.4 跨模块通信规范

| 场景 | 机制 | 规范 |
|------|------|------|
| 模块 A 需要模块 B 的数据 | 接口在 Contracts 定义 | `IHerbSearchProvider` 在 Contracts，实现在 HerbsModule |
| 模块 A 需要通知模块 B | EventAggregator | `CaseEvents.ConsultationCompletedEvent` |
| 工作流跨模块 | 允许的直接引用 | Registration 引用 Patients + Users (文档化例外) |

### 3.5 Mapperly 使用规范

```csharp
[Mapper]
public partial class {Entity}Mapper
{
    // DTO → UI Item
    public partial {Entity}Item ToItem({Entity}Dto dto);

    // UI Item → Update DTO
    public partial {Entity}InputDto ToInputDto({Entity}Item item);

    // 批量映射
    public partial List<{Entity}Item> ToItemList(List<{Entity}Dto> dtos);

    // 深拷贝 (变更检测)
    [UseDeepCloning]
    public partial {Entity}DetailModel Clone({Entity}DetailModel source);
}
```

**规范**:
- 统一使用 `[RequiredMappingStrategy.Target]`
- 三向映射: ToItem / ToDto / ToInputDto
- 变更检测使用 `[UseDeepCloning]`

---

## 4. Roles 角色工作台层设计

### 4.1 角色定义

| 角色 | 入口 View | 核心功能 | 模块依赖 |
|------|----------|---------|---------|
| **Admin** | AdminHomeView | 7 个功能卡片网格 | Users, Patients, Herbs, Formula, MedicalCase, Reports |
| **Doctor** | ClinicalWorkspaceView | 一体化诊疗工作台 | Patients, MedicalCase, Herbs, Formula |
| **Receptionist** | ReceptionistHomeView | 搜索 + 挂号 + 读卡 | Patients, Registration, CardReader |
| **SuperAdmin** | SysadminHomeView | 仪表盘 + 日志 + 部署 | Users |

### 4.2 薄包装 View 模式

```xml
<!-- Admin/HerbManagementView.xaml — 一行嵌入业务控件 -->
<Grid>
    <herbControls:HerbMasterDetailControl />
</Grid>
```

**原则**: View 在角色台，Control 在业务模块。同一业务控件可在多角色中复用。

### 4.3 Composite ViewModel 模式

```
MedicalCaseWorkspaceViewModel (784 行, 父 VM)
  │  实现 IMedicalCaseWorkspaceContext + IWorkspaceHost
  │
  ├── ConsultationEditorViewModel (子 VM, ChildViewModelBase)
  │     通过 IWorkspaceHost.SetBusy/SetError/ShowConfirm 回调父 VM
  │
  ├── PrescriptionEditorViewModel (子 VM, ChildViewModelBase)
  │     同上
  │
  └── MedicalCaseCommandsViewModel (子 VM, ChildViewModelBase)
        同上
```

**子 VM 生命周期**: 在父 VM 构造函数中 `new` 创建，与父 VM 绑定。

### 4.4 权限模型

三层权限:
1. `IRoleDefinition.RequiredModules` → 控制模块加载
2. `NavigationManager.BuildNavigationItems()` → 控制菜单可见性
3. ViewModel 内部逻辑 → 控制操作权限 (如 Admin 判断 IsSysAdmin)

---

## 5. Shell 入口层设计

### 5.1 三阶段启动流程

```
阶段1: App.OnStartup (同步, 主线程)
  ├─ Mutex 单实例检查
  ├─ Serilog 初始化
  └─ Prism DI 容器 + 模块目录 + CreateShell → MainWindow.Show()

阶段2: MainWindow.Loaded (异步)
  └─ 500ms 延迟 → ShowLoginDialog → NavigateTo(LoginRegion, "Login")

阶段3: StartupPipeline (后台异步, 5 步骤)
  ├─ ErrorHandling (Order=10, Required=true)
  ├─ ModuleCoordinator (Order=20, ParallelGroup="CoreInit")
  ├─ LocalWebApi (Order=250, Kestrel 进程内)
  ├─ ApiHealthCheck (Order=40, 后台 Task)
  └─ Warmup (Order=50)
```

**设计决策**: MainWindow 立即 Show()，启动管道在后台运行。登录界面不受后台初始化影响。

### 5.2 DI 注册结构

```csharp
RegisterTypes() {
    RegisterAllServices() {
        RegisterConfiguration()           // IConfiguration + 9 Options
        RegisterLogging()                 // ~40 ILogger<T>
        RegisterCacheServices()           // IMemoryCache + IDesktopCacheManager
        RegisterRepositories()            // 6 I{Entity}Repository
        AddUnifiedApiClient()             // IApiClient → SwitchingApiClient (Singleton)
        RegisterFoundationServices()      // 14 安全/认证服务
        RegisterPresentationServices()    // 7 UI/导航服务
        RegisterInfrastructureServices()  // 12 基础设施服务
        RegisterCommandServices()         // IApplicationCommands
        RegisterApplicationServices()     // 15 启动/会话/登录服务
        AddViewModelServices()            // IViewModelServices
    }
}
```

### 5.3 双模式切换

```
IApiClient (Contracts)
  → SwitchingApiClient (Foundation)
    ├─ [Local: localhost] → HttpClientApiClient → 内嵌 Kestrel (localhost:5300)
    └─ [Remote: 其他]    → RefitApiClient → 远程 WebAPI
                            Handler Chain: Logging → TokenRefresh → Authorization
```

### 5.4 JWT Token 生命周期

```
Login (LoginCoordinator)
  → SaveAuthenticationAsync → TokenStorageService (持久化)
  → StartSessionAsync → TokenLifecycleService (Timer 监控)
  → LoadModulesForRoleAsync → ApplicationBootstrapper
  → NavigateToRoleHomeAsync

HTTP 请求链:
  LoggingHttpHandler → AuthorizationMessageHandler → TokenRefreshHandler → HttpClientHandler
  (自动注入 Bearer Token, 401 时自动刷新重试)

Token 过期:
  TokenLifecycleService → Expired 事件 → ShellEventCoordinator → LoginStateManager → 自动登出
```

### 5.5 断路器 (ApiHealthMonitor)

```
Closed (正常) → 连续失败≥3次 → Open (熔断, 30s)
Open → 30s 后 → HalfOpen (试探)
HalfOpen → 成功→Closed / 失败→Open
```

### 5.6 导航系统

```
侧边栏点击 → NavigationManager.OnSelectedNavItemChanged
  → INavigationCoordinator.NavigateTo(viewName)
    → 防抖检查 (300ms)
    → ModuleLazyLoader.EnsureModuleLoaded (按需加载)
    → IRegionManager.RequestNavigate(ContentRegion, viewName)
    → 超时检测 (10s)
```

---

## 6. Shared 共享层设计

### 6.1 项目拓扑

```
LYBT.Shared.Primitives (零依赖)
  ↑
LYBT.Shared.Models (DTO + 枚举)
  ↑
┌──┼──────────────┐
Validators  Components  Utilities
```

### 6.2 关键组件

| 组件 | 职责 | 关键类型 |
|------|------|---------|
| **Primitives** | ErrorCode (150+), ValidationConstants | MCCEE 编码规则 |
| **Models** | DTO, PagedResult, ApiResponse | InputDto 创建/更新共用 |
| **Validators** | FluentValidation 双端共享 | 嵌套验证, 纯业务规则 |
| **Configuration** | 20+ Options 类 | ValidateOnStart, 热更新 |
| **ExceptionHandling** | AppException 继承体系 | DesktopExceptionHandler |
| **Logging** | Serilog + 脱敏 + 运行时级别 | SensitiveDataMasker |
| **Components** | 药材项接口 | IHerbItem, HerbValidatorBase |

### 6.3 异常体系

```
AppException (Base)
  ├── BusinessException (业务规则)
  │     ├── NotFoundException (404)
  │     ├── ConflictException (409)
  │     └── ValidationException (400)
  ├── UnauthorizedException (401)
  └── ApiException (外部 API)
```

### 6.4 验证三层架构

| 层级 | 机制 | 位置 |
|------|------|------|
| Shared | FluentValidation | LYBT.Shared.Validators |
| Server | ASP.NET Model Validation | Controller 层 |
| Desktop | DataAnnotations + FluentValidation | ViewModel 层 |

---

## 7. 设计决策记录 (ADR)

### ADR-001: 双模式透明切换

**决策**: 使用 SwitchingApiClient 代理模式实现 Remote/Local 运行时切换

**理由**: Repository 层完全无感，无需条件编译或手动切换

**权衡**: 增加了一层代理，但换来了极高的代码复用

### ADR-002: 模块注册分工

**决策**: Foundation/Infrastructure 服务由 Shell 统一注册，Repository 由各业务模块自行注册

**理由**: 模块自治 + Shell 全局管控的平衡

**权衡**: 需要明确的注册位置约定

### ADR-003: 三种模块类型分类

**决策**: 按业务实体与聚合根的关系将模块分为独立实体/聚合根/从属实体三类

**理由**: 不同类型的实体有不同的数据访问模式和生命周期管理需求

**权衡**: 增加了分类复杂度，但换来了更精确的架构指导

### ADR-004: 薄包装 View 模式

**决策**: 角色模块的 View 仅作为业务 Control 的薄包装

**理由**: 同一业务控件可在多角色中复用，角色模块只做布局和导航

**权衡**: 增加了间接层，但换来了极高的复用性

### ADR-005: Contracts 双目标框架

**决策**: Contracts 项目使用 net8.0 + net8.0-windows 双目标框架

**理由**: net8.0 下排除 WPF 相关接口，Server 端测试可引用而不拉入 WPF

**权衡**: 增加了编译复杂度，但换来了跨层测试能力

---

## 8. 架构改进路线图

### 阶段一: 架构健壮性 (P1)

| # | 改进项 | 工作量 | 影响 |
|---|--------|:------:|------|
| 1 | Local/Remote API 路由前缀统一 (`/api/v1/`) | 0.5 天 | LocalWebAPI 路由对齐 |
| 2 | LocalDbContext 改用 MigrateAsync | 0.5 天 | 支持 schema 演进 |
| 3 | ILocalMedicalCaseApi 方法补齐 | 1 天 | 本地模式功能完整 |
| 4 | ErrorCategory/ErrorSeverity 去重 | 0.5 天 | 消除歧义 |

### 阶段二: 代码质量 (P2)

| # | 改进项 | 工作量 | 影响 |
|---|--------|:------:|------|
| 5 | Admin/Clinical 薄包装 View 去重 | 2 天 | 减少维护成本 |
| 6 | RoleHomeViewModelBase 抽取 | 1 天 | 消除重复逻辑 |
| 7 | 验证器硬编码值常量化 | 0.5 天 | 验证一致性 |
| 8 | Logger 泛型注册替代手动注册 | 0.5 天 | 简化 DI 配置 |

### 阶段三: 可维护性 (P3)

| # | 改进项 | 工作量 | 影响 |
|---|--------|:------:|------|
| 9 | Clinical VM 拆分 | 2 天 | 降低复杂度 |
| 10 | Sysadmin UI 风格统一 | 1 天 | 视觉一致性 |
| 11 | Desktop 验证机制统一为 FluentValidation | 1 天 | 双端验证一致 |
| 12 | 事件定义归并到统一命名空间 | 0.5 天 | 事件管理清晰 |

---

## 9. 编码规范速查

### 9.1 命名规范

| 类型 | 规范 | 示例 |
|------|------|------|
| 私有字段 | `_camelCase` | `_userRepository` |
| 属性 | `PascalCase` | `IsBusy`, `UserName` |
| 命令 | `{动作}Command` | `LoadUsersCommand` |
| 异步方法 | `{动作}Async` | `LoadUsersAsync` |
| 事件 | `{实体}{动作}Event` | `PatientCreatedEvent` |
| 命名空间 | `LYBT.Desktop.{模块}.{层级}` | `LYBT.Desktop.Users.ViewModels` |

### 9.2 禁止事项

| 禁止 | 原因 |
|------|------|
| Code-Behind 中调用 Repository | 违反 MVVM |
| 模块间直接引用 | 破坏模块化 |
| 属性注入/方法注入 | 只用构造函数注入 |
| 返回 ServiceResult\<T\> (Desktop) | Server 端专用 |
| 使用 AutoMapper | 使用 Mapperly |
| 使用 BindableBase/DelegateCommand | 使用 CommunityToolkit.Mvvm |
| XAML 自定义 ControlTemplate | 使用 MDIX 内置样式 |

### 9.3 必须遵守

| 必须 | 原因 |
|------|------|
| ViewModel 继承标准基类 | 架构一致性 |
| 使用 `{x:Static converters:Cvt.Xxx}` 绑定转换器 | 性能优化 |
| XAML pack URI: `/Assembly;component/Path.xaml` | 资源引用 |
| Repository 返回裸类型 | 异常向上抛出 |
| Mapperly 编译期映射 | 无运行时反射 |
| 中文业务文档/注释 | 可读性 |
| 英文标识符/commit | 国际化 |
