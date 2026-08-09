# Desktop 层类/方法级深度审查报告

> **日期**: 2026-08-09
> **范围**: `src/Client/Desktop/` 下所有 .cs 文件（不含 obj/bin）
> **工具**: Serena symbol analysis + grep 跨模块搜索
> **性质**: READ-ONLY 分析，不修改任何代码

---

## 目录

1. [总览](#1-总览)
2. [逐项目类清单](#2-逐项目类清单)
3. [跨项目重复方法清单](#3-跨项目重复方法清单)
4. [工具类使用矩阵](#4-工具类使用矩阵)
5. [基类继承矩阵](#5-基类继承矩阵)
6. [StatusHandler 模式分析](#6-statushandler-模式分析)
7. [发现的问题](#7-发现的问题)

---

## 1. 总览

| 项目 | 文件数 | 类型数 | 正确归属 |
|------|:------:|:------:|:--------:|
| LYBT.Desktop.Contracts | 72 | 106 | 100% |
| LYBT.Desktop.Controls | 41 | 42 | 100% |
| LYBT.Desktop.Foundation | 64 | 64 | 100% |
| LYBT.Desktop.Infrastructure | 83 | 83 | 100% |
| LYBT.Desktop.Printing | 12 | 17 | 100% |
| Shell | 47 | 47 | 100% |
| Modules (Auth/Catalog/MedicalCase/Patients/Registrations/Users) | ~80 | ~81 | 100% |
| Roles (Admin/Clinical) | 38 | 38 | 100% |
| LocalWebAPI | 22 | 22 | 100% |
| **合计** | **~459** | **~500** | **100%** |

**结论**: 所有类均正确归属到对应项目，未发现跨层违规。

---

## 2. 逐项目类清单

### 2.1 LYBT.Desktop.Contracts（72 文件，106 类型）

纯契约层：接口、枚举、record、事件参数。零实现逻辑。

| 子目录 | 文件数 | 类型数 | 说明 |
|--------|:------:|:------:|------|
| ApiClient/ | 12 | 12 接口 | 统一 API 客户端接口（IApiClient 聚合根 + 10 个领域子接口） |
| Api/ (Refit) | 11 | 11 内部接口 | Refit 注解的 HTTP API 接口 |
| Enums/ | 2 | 2 枚举 | EditState, WorkspaceMode |
| Events/ | 1 | 3 类型 | CacheEvents + CacheInvalidatedPayload + CacheDomain |
| Models/ | 4 | 8 类型 | PerformanceReport, MedicalCaseNavigationParameters, AuthState 等 |
| Performance/ | 1 | 2 类型 | IPerformanceMonitor + EventArgs |
| Results/ | 1 | 2 record | CommandResult<T> + CommandResult |
| Roles/ | 2 | 2 接口 | IRoleRegistry + IRoleDefinition |
| Security/ | 1 | 1 接口 | IAuthenticationStateMachine |
| UI/ | 1 | 1 record | BreadcrumbItem |
| Services/ | 33 | ~49 类型 | 30 接口 + 9 枚举 + 7 事件参数 + 3 record |
| Services/CrossModule/ | 2 | 2 接口 | IFormulaSearchProvider, IHerbSearchProvider |
| Repositories/ | 6 | 6 接口 | 6 个领域 Repository 接口 |

### 2.2 LYBT.Desktop.Controls（41 文件，42 类型）

可复用 WPF 控件、转换器、辅助类。

| 子目录 | 文件数 | 说明 |
|--------|:------:|------|
| Converters/ | 13 | 13 个 IValueConverter（Bool/Null/String/Enum → Visibility/Brush/Color/Int） |
| Models/ | 2 | DuplicateDosageStrategy 枚举 + NavigationItem |
| Helpers/ | 2 | ResponsiveLayoutHelper + BindingProxy |
| Controls/ | 24 | 18 个 WPF 控件 + 4 个内部 ViewModel + 8 个枚举/事件参数 |

### 2.3 LYBT.Desktop.Foundation（64 文件，64 类型）

HTTP 客户端、安全/令牌管理、缓存、健康检查、基础 Repository。

| 子目录 | 文件数 | 说明 |
|--------|:------:|------|
| Http/ | 26 | 双适配器模式：10 个 RefitApiClient + 10 个 HttpApiClient + SwitchingApiClient + 基础设施 |
| Security/ | 26 | 令牌生命周期、认证状态机、DPAPI 凭据库、登出服务 + 10 个接口* |
| Caching/ | 1 | DesktopCacheManager |
| ExceptionHandling/ | 1 | ClientErrorMessageMapper |
| HealthCheck/ | 2 | ApiHealthCheckService + 接口 |
| Repositories/ | 2 | EntityApiClientRepositoryBase + ApiClientRepositoryBase |
| Modules/ | 2 | ModuleLoadingService + 接口 |
| Services/ | 1 | ConnectionModeService |
| Application/ | 3 | ApplicationStateService + 接口 + EventArgs |

> *注: Foundation/Security/ 下有 10 个接口与实现并列（ITokenManager 等），偏离"接口放 Contracts"规则，为安全内聚性有意为之。

### 2.4 LYBT.Desktop.Infrastructure（83 文件，83 类型）

ViewModel 基类、导航、角色、服务、读卡器、命令、事件、常量。

| 子目录 | 文件数 | 说明 |
|--------|:------:|------|
| Navigation/ | 10 | NavigationCoordinator + HistoryService + RegionMonitor + LazyLoader + 接口 |
| Roles/ | 6 | RoleDefinitionBase + 4 个角色定义 + RoleRegistry |
| Services/ | 30 | Dialog/Toast/Notification/Session/Search/Pagination/Loading/Selection/ErrorHandler 等 + 9 接口 |
| CardReader/ | 11 | 工厂 + 服务 + 适配器(Mock/HuaDa) + P/Invoke + 模块 + 模型 |
| ViewModels/ | 13 | 7 个基类 + 3 个组合类 + MasterDetailViewModelBase + ValidationAccessors + BaseStatusHandler |
| Constants/ | 4 | SystemConstants, RegionNames, ViewNames, CommonOptions |
| Commands/ | 1 | ApplicationCommands（19 个 CompositeCommand） |
| Events/ | 3 | PatientEvents, CaseEvents, EventSubscriptionManager |
| Behaviors/ | 2 | PasswordBoxHelper, DataGridSelectionBehavior |
| Helpers/ | 1 | PrivacyHelper |
| Extensions/ | 1 | TaskExtensions |
| DI/ | 1 | ViewModelServicesExtensions |
| ExceptionHandling/ | 3 | DesktopExceptionHandler + 接口 + ExceptionSeverity 枚举 |
| Performance/ | 1 | PerformanceMonitor |

### 2.5 LYBT.Desktop.Printing（12 文件，17 类型）

处方打印管线：接口 → 服务 → 执行器/构建器/导出器 → 模板。

| 类 | 职责 | 公开方法数 |
|----|------|:---------:|
| PrintingModule | Prism 模块入口 | 2 |
| IPrintService<TModel> | 泛型打印服务接口 | 6 |
| PrintOptions / PaperSize / PrintOrientation / ExportFormat | 打印配置 DTO 和枚举 | — |
| PrescriptionPrintModel / PrescriptionItemPrintModel | 打印数据模型 | 1 |
| PrescriptionPrintService | IPrintService 编排器 | 6 |
| PrescriptionPrintExecutor | 底层打印机管理 + 执行 | 9 |
| PrescriptionPdfExporter | QuestPDF 导出 (A5) | 1 |
| PrescriptionPreviewWindowBuilder | WPF 预览窗口构建 | 1 |
| PrescriptionDocumentBuilder | FixedDocument 构建 + 分页 | 9 |
| 4 个 Template code-behind | XAML 模板代码隐藏 | 0-1 |

### 2.6 Shell（47 文件，47 类型）

应用外壳：启动、主窗口、导航、登录、会话、对话框、主题、菜单、状态栏。

| 子目录 | 文件数 | 说明 |
|--------|:------:|------|
| Extensions/ | 4 | DI 注册扩展（DataSource, UnifiedApiClient, ServiceCollection, PrismConfig） |
| Services/ | 17+10 | NavigationManager, AppStartupOrchestrator, EmbeddedLocalWebApiService, ShellEventCoordinator 等 + 10 接口 |
| Startup/ | 5 | StartupPipeline + 4 个启动步骤 |
| Session/ | 3 | SessionLifecycleManager + 接口 + SessionBasedCurrentUserProvider |
| Login/ | 3 | LoginCoordinator + LoginStateManager + 接口 |
| HealthCheck/ | 1 | ApiHealthMonitor（断路器模式） |
| Bootstrap/ | 2 | ApplicationBootstrapper + 接口 |
| ViewModels/ | 2 | MainWindowViewModel, AccountSettingsViewModel |
| Dialogs/ | 6 | 3 个对话框 ViewModel + 3 个 View code-behind |
| 其他 | 3 | App, NativeMethods, AccountSettingsControl |

### 2.7 Modules（~80 文件，~81 类型）

| 模块 | 类数 | Repository | Service | ViewModel | Model | StatusHandler | Mapper | 接口 |
|------|:----:|:----------:|:-------:|:---------:|:-----:|:-------------:|:------:|:----:|
| Auth | 8 | 0 | 0 | 5 | 1 | 0 | 0 | 0 |
| Catalog | 22 | 2 | 4 | 5 | 5 | 2+2 | 1 | 0 |
| MedicalCase | 28+ | 1 | 6 | 9 | 6 | 0 | 4 | 6 |
| Patients | 13 | 1 | 2 | 3 | 4 | 1+1 | 0 | 0 |
| Registrations | 9 | 1 | 2 | 2 | 2 | 0 | 0 | 0 |
| Users | 11 | 1 | 1 | 2 | 2 | 2+2 | 0 | 0 |

### 2.8 Roles（38 文件，38 类型）

| 项目 | 文件数 | 说明 |
|------|:------:|------|
| LYBT.Desktop.Admin | 17 | AdminModule + SysadminModule（运维控制台）+ 系统设置 |
| LYBT.Desktop.Clinical | 21 | ClinicalModule（含 Receptionist）+ 临床工作区 VM + 子 VM |

### 2.9 LocalWebAPI（22 文件，22 类型）

| 类 | 职责 | 镜像 Server 控制器 |
|----|------|-------------------|
| AuthController | 登录/登出/刷新/自动登录/验证令牌 | IdentityController (部分) |
| CatalogController | 药材+验方 CRUD、批量操作、引用检查、克隆 | CatalogController |
| ConfigurationController | 键值配置存储 | ConfigurationController |
| DeployController | 存根："本地模式不支持" | DeployController |
| DiagnosticsController | DB 信息、版本、日志管理 | DiagnosticsController |
| HealthController | 健康检查、Ping、详细健康 | HealthController |
| MedicalCasesController | 医案 CRUD + 查询 + 状态转换 | MedicalCasesController |
| PatientsController | 患者 CRUD + 批量操作 + 导入导出 | PatientsController |
| RegistrationsController | 挂号 CRUD + 队列 + 快速就诊 | RegistrationsController |
| ReportsController | 只读报表查询 | ReportsController |
| UsersController | 用户 CRUD（继承 BaseUsersController） | IdentityController |

---

## 3. 跨项目重复方法清单

### 3.1 架构性重复（by design — 非缺陷）

每个领域实体有 **配对** 的 `*ApiClient`（Refit）+ `*HttpApiClient`（raw HttpClient），实现同一 `IApiClient*` 接口。这是 `SwitchingApiClient` 双模架构（ADR-0009/0010）：

| 接口 | Refit 适配器 | HttpClient 适配器 | 方法数 |
|------|-------------|------------------|:------:|
| IApiClientHerbs | HerbApiClient | HerbsHttpApiClient | 13 |
| IApiClientFormulas | FormulaApiClient | FormulasHttpApiClient | 16 |
| IApiClientPatients | PatientApiClient | PatientsHttpApiClient | 11 |
| IApiClientIdentity | IdentityApiClient | IdentityHttpApiClient | 20 |
| IApiClientMedicalCases | MedicalCaseApiClient | MedicalCasesHttpApiClient | 17 |
| IApiClientRegistrations | RegistrationApiClient | RegistrationsHttpApiClient | 6 |
| IApiClientConfiguration | ConfigurationApiClient | ConfigurationHttpApiClient | 5 |

**判定**: 架构性重复，每对共享接口但传输实现不同。不需要去重。

### 3.2 真实结构性重复（跨模块 Service/Repository）

| 方法模式 | 出现次数 | 涉及位置 |
|---------|:--------:|---------|
| `GetPagedAsync(page, pageSize, keyword, ct)` | 6 | UserRepository, PatientRepository, MedicalCaseRepository, RemoteHerbService, RemoteUserService, FormulaService |
| `GetByIdAsync(Guid id, ct)` | 6 | RemoteHerbService, RemoteUserService, FormulaService, PatientService, RegistrationApiClient, MedicalCaseRepository |
| `DeleteXxxAsync(Guid id, ct)` | 5 | RemoteHerbService, RemoteUserService, FormulaService, PatientService, MedicalCaseRepository |
| `CreateXxxAsync(InputDto, ct)` | 5 | RemoteHerbService, RemoteUserService, FormulaService, PatientService, RegistrationRepository |
| `UpdateXxxAsync(InputDto, ct)` | 4 | RemoteHerbService, RemoteUserService, FormulaService, PatientService |
| `GetAllAsync(ct)` | 2 | RemoteHerbService, RemoteUserService |
| `GetDoctorsAsync(ct)` | 2 | UserRepository, RemoteUserService |

**判定**: 结构性重复 — 每个模块独立实现相同的 CRUD 模式。`MasterDetailViewModelBase<T,T>` + `IMasterDetailServices<T,T>` 已标准化消费端，但提供端（Service/Repository）无共享 CRUD 基类。

### 3.3 MedicalCaseService 返回值不一致

`MedicalCaseService` 使用 tuple 返回 `(bool Success, string? Error)` 而非 `CommandResult<T>`：

- `LoadDetailsAsync` → `(bool success, MedicalCaseDetailDto? detail, string? errorMessage)`
- `SaveAndCompleteAsync/SuspendAsync/CancelAsync` → `(bool Success, string? Error)`

与其他所有使用 `CommandResult<T>` 的 Service 不一致。

---

## 4. 工具类使用矩阵

### 4.1 Desktop 原生工具类

| 工具类 | 位置 | 类型 | 用途 | 使用范围 |
|--------|------|------|------|---------|
| `ClientErrorMessageMapper` | Foundation/ExceptionHandling/ | `static` | HTTP 状态码/错误码/异常 → 用户友好中文消息 | **~60+ 调用点**（全模块、Shell、Infrastructure） |
| `PrivacyHelper` | Infrastructure/Helpers/ | `static` | 身份证号脱敏 (`MaskIdNumber`) | 低 — 患者展示 |
| `ResponsiveLayoutHelper` | Controls/Helpers/ | `static` | 屏幕尺寸断点 + 推荐宽度 | 低 — 响应式布局 |
| `PasswordBoxHelper` | Infrastructure/Behaviors/ | `static` | PasswordBox 绑定附加属性 | 低 — 登录视图 |
| `ShellDialogHelper` | Shell/Services/ | `class` (DI) | Toast/确认对话框便捷包装 | 1 注册，有限 Shell 使用 |
| `SystemConstants` | Infrastructure/Constants/ | `static` | 应用级常量（超时、页面大小、文件路径） | 中等 — 配置引用 |

### 4.2 Mapper 类（Riok.Mapperly — 编译期）

| Mapper | 位置 | 用途 |
|--------|------|------|
| FormulaDetailModelMapper | Catalog/Mappers/ | Formula DTO ↔ DetailModel |
| MedicalCaseDetailModelMapper | MedicalCase/Mappers/ | MedicalCase DTO ↔ DetailModel |
| MedicalCaseCloneMapper | MedicalCase/Mappers/ | MedicalCase 深拷贝 |
| ConsultationMapper | MedicalCase/Mappers/ | Consultation DTO 映射 |
| PrescriptionMapper | MedicalCase/Mappers/ | Prescription DTO 映射 |

### 4.3 Extension 类

| Extension | 位置 | 用途 |
|-----------|------|------|
| TaskExtensions | Infrastructure/Extensions/ | `SafeFireAndForget` |
| ViewModelServicesExtensions | Infrastructure/DI/ | VM 服务 DI 注册 |
| ServiceCollectionExtensions | Shell/Extensions/ | 主 DI 配线 |
| DataSourceRegistrationExtensions | Shell/Extensions/ | 数据源 DI |
| UnifiedApiClientExtensions | Shell/Extensions/ | 统一 API 客户端 DI |
| PrismConfigurationExtensions | Shell/Extensions/ | Prism 配置 |
| PrescriptionImportExtensions | MedicalCase/Extensions/ | 处方导入辅助 |
| DuplicateDosageStrategyExtensions | Controls/Models/ | 重复剂量策略计算 |

### 4.4 共享层工具（Desktop 使用）

| 工具类 | 位置 | Desktop 使用范围 |
|--------|------|-----------------|
| `PinYinHelper` | Shared/Models/Utilities/Text/ | HerbDetailModel, HerbEditContext, UserDetailModel, PatientEditorViewModel, HerbMasterDetailViewModel（自动拼音码） |
| `ValidationConstants` | Shared/Models/Primitives/Validation/ | 所有 EditContext/DetailModel 类的 `[StringLength]` 特性（~30+ 使用） |

---

## 5. 基类继承矩阵

### 5.1 继承层次

```
ObservableObject (CommunityToolkit.Mvvm)
├── NavigableViewModelBase (Infrastructure)
│   ├── MasterDetailViewModelBase<TListItem, TDetail>
│   │   ├── HerbMasterDetailViewModel
│   │   ├── FormulaMasterDetailViewModel
│   │   ├── PatientMasterDetailViewModel
│   │   ├── UserMasterDetailViewModel
│   │   └── MedicalCaseMasterDetailViewModel
│   ├── DialogViewModelBase
│   │   ├── RegistrationCreateDialogViewModel
│   │   ├── FormulaImportDialogViewModel
│   │   ├── MessageDialogViewModel
│   │   ├── InputDialogViewModel
│   │   ├── ConfirmationDialogViewModel
│   │   ├── UnsavedChangesDialogViewModel
│   │   ├── HistoryCopyDialogViewModel
│   │   └── ConnectionTestViewModelBase (abstract)
│   ├── LoginViewModel
│   ├── LoginCredentialsViewModel
│   ├── MainWindowViewModel
│   ├── AccountSettingsViewModel
│   ├── ClinicalHomeViewModel
│   ├── ClinicalWorkspaceViewModel
│   ├── AdminHomeViewModel
│   ├── SysadminHomeViewModel
│   ├── DeploymentViewModel
│   ├── LogLevelControlViewModel
│   ├── SystemSettingsViewModel
│   ├── ConnectionStatusViewModel
│   ├── RegistrationListViewModel
│   ├── AuditLogViewModel
│   ├── ReportsHomeViewModel
│   ├── ReceptionistHomeViewModel
│   ├── PatientSelectionViewModel
│   ├── PatientCardReaderViewModel
│   └── MedicalCaseWorkspaceViewModel
│
├── EditorViewModelBase<TContext> (where TContext : ValidatableModelBase)
│   ├── HerbEditorViewModel
│   ├── FormulaEditorViewModel
│   └── PatientEditorViewModel
│
├── ChildViewModelBase
│   ├── MedicalCaseCommandsViewModel
│   ├── ConsultationEditorViewModel
│   ├── PrescriptionEditorViewModel
│   ├── CardReaderViewModel
│   └── PendingQueueViewModel
│
├── HerbItemViewModelBase
│   └── FormulaHerbItemViewModel
│
├── ObservableObject (直接子类)
│   ├── NavigationManager
│   ├── SelectionService<T>
│   ├── ErrorHandler
│   ├── SearchService
│   ├── PaginationService
│   ├── LoadingStateManager
│   ├── DetailEditorService<T>
│   ├── HerbListControlViewModel
│   ├── HerbItemControlViewModel
│   ├── UserEditorViewModel ← 注意：未继承 EditorViewModelBase
│   ├── StatusCard, DashboardStatus
│   ├── StatusBarManager
│   ├── ThemeService
│   ├── MedicalCaseEditContext
│   └── FormulaHerbItemModel

BindableBase (Prism)
└── ValidatableModelBase (INotifyDataErrorInfo)
    ├── HerbEditContext
    ├── HerbDetailModel
    ├── FormulaEditContext
    ├── FormulaDetailModel
    ├── PatientEditContext
    ├── PatientDetailModel
    ├── UserEditContext
    ├── UserDetailModel
    ├── RegistrationDetailModel
    └── MedicalCaseDetailModel

BaseStatusHandler<TListDto>
├── HerbStatusHandler
├── FormulaStatusHandler
├── PatientStatusHandler
└── UserStatusHandler
```

### 5.2 关键观察

| 发现 | 详情 |
|------|------|
| **NavigableViewModelBase 主导** | 22+ 直接子类 — 核心基类 |
| **MasterDetailViewModelBase** | 5 个具体子类（每实体域一个）— 良好分解 |
| **EditorViewModelBase** | 3 个子类（Herb, Formula, Patient）。**UserEditorViewModel 直接继承 ObservableObject** — 不一致 |
| **ChildViewModelBase** | 5 个子类用于复合工作区模式 |
| **DialogViewModelBase** | 7 个具体子类 + 1 个抽象中间层 |
| **HerbItemViewModelBase** | 仅 1 个子类（FormulaHerbItemViewModel） |
| **ValidatableModelBase** | 10 个子类 — 所有 EditContext/DetailModel 一致使用 |

---

## 6. StatusHandler 模式分析

### 6.1 类清单

| Handler | 实体 | 接口 | 模块 |
|---------|------|------|------|
| BaseStatusHandler<TListDto> | (抽象) | — | Infrastructure |
| HerbStatusHandler | HerbListDto | IHerbStatusHandler | Catalog |
| FormulaStatusHandler | FormulaListDto | IFormulaStatusHandler | Catalog |
| PatientStatusHandler | PatientListDto | IPatientStatusHandler | Patients |
| UserStatusHandler | UserListDto | IUserStatusHandler | Users |

### 6.2 模式对比

| 方面 | HerbStatusHandler | FormulaStatusHandler | PatientStatusHandler | UserStatusHandler |
|------|------------------|---------------------|---------------------|-------------------|
| EntityTypeName | "药材" | "验方" | "患者" | "用户" |
| GetEntityId | `e.Id` | `e.Id` | `e.Id` | `e.Id` |
| GetEntityDisplayName | `e.Name` | `e.Name` | `e.Name` | `e.RealName ?? e.UserName` |
| GetEntityStatus | `e.Status` ✅ | `e.Status` ✅ | **未重写** (throws) | **未重写** (throws) |
| ExecuteRestoreAsync | `null` (存根) | `_formulaRepository.RestoreAsync` | `null` (存根) | `_userRepository.RestoreAsync` |
| ExecuteToggleStatusAsync | `_herbService.ToggleStatusAsync` ✅ | `_formulaRepository.ToggleStatusAsync` ✅ | **未重写** (throws) | **自定义 `ToggleUserStatusAsync`** |
| 依赖注入 | IHerbService | IFormulaRepository | IPatientRepository | IUserService + IUserRepository |

---

## 7. 发现的问题

### P0 — 需要立即关注

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| — | 无 P0 问题 | — | — |

### P1 — 值得修复

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| P1-1 | **HerbStatusHandler.ExecuteRestoreAsync 是静默存根** — 返回 `Task.FromResult<object?>(null)`，恢复操作始终"失败"（返回 null → 基类显示错误对话框） | `Modules/Catalog/ViewModels/Handlers/HerbStatusHandler.cs` | 用户点击恢复药材时看到误导性错误 |
| P1-2 | **UserStatusHandler 完全绕过基类 Toggle 模式** — 不重写 `GetEntityStatus` 或 `ExecuteToggleStatusAsync`，而是实现独立的 `ToggleUserStatusAsync` 方法。基类 `ToggleStatusAsync` 对 Users 是死代码 | `Modules/Users/ViewModels/Handlers/UserStatusHandler.cs` | 基类维护成本高，模式不一致 |
| P1-3 | **MedicalCaseService 使用 tuple 返回值而非 CommandResult<T>** — `LoadDetailsAsync` → `(bool, MedicalCaseDetailDto?, string?)`，`SaveAndCompleteAsync/SuspendAsync/CancelAsync` → `(bool, string?)` | `Modules/MedicalCase/Services/MedicalCaseService.cs` | 与其他所有 Service 的 `CommandResult<T>` 模式不一致 |

### P2 — 可改进

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| P2-1 | **CRUD Service/Repository 方法结构性重复 4-6 次** — GetPagedAsync、GetByIdAsync、DeleteXxxAsync、CreateXxxAsync、UpdateXxxAsync 在各模块独立实现 | 所有模块 Service/Repository | 可考虑泛型 `CrudServiceBase<TListDto, TDetailDto, TInputDto>` |
| P2-2 | **UserEditorViewModel 未使用 EditorViewModelBase** — 直接继承 ObservableObject，因 `[ObservableProperty]` + 缓存链接差异 | `Modules/Users/ViewModels/UserEditorViewModel.cs` | 与其他 Editor（Herb/Formula/Patient）不一致 |
| P2-3 | **Foundation/Security/ 下 10 个接口偏离"接口放 Contracts"规则** — ITokenManager、ITokenValidator 等与实现并列 | `Core/Foundation/Security/` | 已知偏差，为安全内聚性有意为之 |
| P2-4 | **PatientStatusHandler.GetEntityStatus 未重写** — 基类 `ToggleStatusAsync` 对 Patients 会抛出 NotSupportedException | `Modules/Patients/ViewModels/Handlers/PatientStatusHandler.cs` | 如误调用 Toggle 会崩溃 |
| P2-5 | **HerbStatusHandler.ExecuteRestoreAsync 与 PatientStatusHandler.ExecuteRestoreAsync 均为存根** — 返回 null 而非实际实现 | Catalog/Patients StatusHandler | 恢复功能不可用 |
| P2-6 | **StatusHandler 依赖注入不一致** — Herb 注入 IHerbService，Formula/Patient 注入 I*Repository，User 注入两者 | 各模块 StatusHandler | 维护认知负担 |

### 信息级

| # | 观察 | 详情 |
|---|------|------|
| I-1 | **ClientErrorMessageMapper 是使用最广泛的工具类** — 60+ 调用点，单例真相源，已良好分解 | Foundation/ExceptionHandling/ |
| I-2 | **双适配器模式（Refit + HttpClient + SwitchingApiClient）是标准 API 客户端架构** — 10 个领域一致 | Foundation/Http/ |
| I-3 | **StartupPipeline 支持并行步骤组** — 新启动步骤应使用此模式 | Shell/Services/Startup/ |
| I-4 | **ApiHealthMonitor 使用断路器模式** — 值得在其他地方复用的弹性模式 | Shell/Services/HealthCheck/ |
| I-5 | **LoginCoordinator 是 Shell 中最重的类**（16 个字段） — 如继续增长可考虑分解 | Shell/Services/Login/ |
| I-6 | **MedicalCase 是最复杂的模块**（28+ 类）— 分离式服务架构（Query/Command/Lifecycle + 共享 EditContext）应保留 | Modules/MedicalCase/ |
| I-7 | **所有 459 个文件正确归属** — 未发现跨层违规 | 全局 |
| I-8 | **ValidatableModelBase 10 个子类一致使用** — 所有 EditContext/DetailModel 均继承 | 全模块 Model |
| I-9 | **PinYinHelper + ValidationConstants 在 Shared 层** — Server 和 Desktop 共享，放置正确 | Shared/ |
| I-10 | **LocalWebAPI 10 个控制器 1:1 镜像 Server 控制器** — 使用共享基类（BaseCrudController 等） | LocalWebAPI/ |

---

> **报告结束**。本报告为 READ-ONLY 分析产出，未修改任何代码。
