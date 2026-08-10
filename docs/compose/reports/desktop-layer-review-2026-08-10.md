# Desktop 层现状分析报告（重构基线）

> **日期**: 2026-08-10
> **范围**: `src/Client/Desktop/` 全部（Shell / Roles / Modules / Core / LocalWebAPI）
> **性质**: READ-ONLY 现状基线，不修改任何代码
> **方法**: 目录结构 + csproj 引用图 + 全模块源码阅读（5 个并行 scout 深挖 + 主 agent 对已知待办逐项核验）
> **证据标注**: 除明确标注【推断】外均为【已读】代码/文件

---

## 1. 项目结构总览

### 1.1 项目组成与职责

| 项目 | 目录 | 职责 | 引用（ProjectReference） |
|------|------|------|--------------------------|
| **LYBT.Desktop.Shell** | `Shell/` | PrismApplication 入口、启动编排、登录/会话、导航、嵌入 LocalWebAPI 宿主 | Core×3(Foundation/Infrastructure/Printing) + LocalWebAPI + 6 Modules + 2 Roles |
| **LYBT.Desktop.Admin** | `Roles/LYBT.Desktop.Admin/` | 管理员角色台（含 Sysadmin 运维控制台） | Core×3 + Shared.Models + 4 Modules(Catalog/Patients/MedicalCase/Users) |
| **LYBT.Desktop.Clinical** | `Roles/LYBT.Desktop.Clinical/` | 临床角色台（含 Receptionist 前台） | Core×3 + Shared.Models + 4 Modules(Catalog/Patients/MedicalCase/Registrations) |
| **LYBT.Desktop.Auth** | `Modules/LYBT.Desktop.Auth/` | 登录/首启向导/服务器配置（仅 UI 状态，无 Repository/Service） | Core×3 + Shared.Models |
| **LYBT.Desktop.Catalog** | `Modules/LYBT.Desktop.Catalog/` | 药材 + 验方双域（C-3c 合并自 Herbs/Formula） | Core×3 + Shared.Models |
| **LYBT.Desktop.MedicalCase** | `Modules/LYBT.Desktop.MedicalCase/` | 医案 + 子域（Consultation/Prescription）+ Reports 子模块 | Core×4(含 Printing) + Shared.Models |
| **LYBT.Desktop.Patients** | `Modules/LYBT.Desktop.Patients/` | 患者管理 + 读卡 | Core×2(Infrastructure/Contracts) + Shared.Models |
| **LYBT.Desktop.Registrations** | `Modules/LYBT.Desktop.Registrations/` | 挂号队列 + 创建弹窗 + SignalR | Core×2 + Shared.Models |
| **LYBT.Desktop.Users** | `Modules/LYBT.Desktop.Users/` | 用户管理 | Core×3 + Shared.Models |
| **LYBT.Desktop.Contracts** | `Core/LYBT.Desktop.Contracts/` | 契约层：36 个 Service 接口文件（含 CrossModule 2 个）+ 6 个领域 Repository 接口 + 11 个 internal Refit 接口 + 12 个 ApiClient 接口（IApiClient 聚合根 + 11 域） | Shared.Models |
| **LYBT.Desktop.Foundation** | `Core/LYBT.Desktop.Foundation/` | HTTP 双适配器、SwitchingApiClient、Security、缓存、健康检查、Repository 基类 | Contracts + Shared×4 |
| **LYBT.Desktop.Infrastructure** | `Core/LYBT.Desktop.Infrastructure/` | VM 基类、导航、角色注册、CardReader、Services、DI | Contracts/Controls/Foundation + Shared×4 |
| **LYBT.Desktop.Controls** | `Core/LYBT.Desktop.Controls/` | 可复用 WPF 控件（14 顶层 + HerbList/HerbItem/FormulaView 等） | Contracts/Foundation + Shared.Models |
| **LYBT.Desktop.Printing** | `Core/LYBT.Desktop.Printing/` | 处方打印管线（Builder/Executor/PdfExporter/PreviewWindow） | Infrastructure + Shared |
| **LYBT.LocalWebAPI** | `LocalWebAPI/` | 嵌入式 ASP.NET Core Kestrel（本地模式），复用 Server 6 模块 | Shared.Models/Entities/Logging + Desktop.Contracts + Infrastructure + Server 6 Modules |

### 1.2 依赖方向

```
Shell → Roles → Modules → Core(Infrastructure → Foundation/Controls → Contracts) → Shared
Shell → LocalWebAPI → Server Modules（ADR-0010 唯一跨层例外）
```

- 业务模块（Modules/）之间 **0 编译引用**（已验证 csproj + 架构测试 DM06）；跨模块协作仅经 Contracts 接口（`IHerbSearchProvider`/`IFormulaSearchProvider`/`IMasterDetailServices`/`IApiClient` 聚合根）。
- Roles 允许引用 Modules（组合角色台）；Modules 不允许引用 Roles（方向正确）。
- 架构测试 88/88 通过（2026-08-09 commit 声明 + 测试文件结构核对）。

### 1.3 目录结构一致性（对标 16-desktop-architecture-spec §3.3）

| 子目录 | Auth | Catalog | MedicalCase | Patients | Registrations | Users |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|
| Models/（含 Items/） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ViewModels/（含 Handlers/） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Views/ | ✅ | ❌ 用 Controls/ | ✅ 兼有 | ❌ 用 Controls/ | ✅ | ❌ 用 Controls/ |
| Services/ | ❌ 无 | ✅ | ✅ | ✅ | ✅ | ✅ |
| Repositories/ | ❌ 无 | ✅ | ✅ | ✅ | ✅ | ✅ |
| Mappers/ | ❌ 无 | ✅ | ✅ | ❌ 无 | ❌ 无 | ❌ 无 |
| Controls/ | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Dialogs/ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ |
| 独有 | — | — | Interfaces/ Extensions/ Reports/ | Models/Display/ | Events/ | — |

**结论**：目录未统一（spec §3.3 原样）。spec 已注明 Controls/ vs Views/ 是 Prism 合法用法可不强制统一；但 Services/、Mappers/ 存在性差异仍存在。MedicalCase 是唯一含 Interfaces/Extensions/Reports 子模块的模块（Reports 为独立 `[Module]` 标 STUB 的 `ReportsModule`，注册在 Shell `ConfigureModuleCatalog`）。

---

## 2. 逐模块现状

### 2.1 Catalog（药材 Herb + 验方 Formula）— 重构第一步已完成

| 项 | Herb | Formula |
|----|------|---------|
| Model | `HerbDetailModel`（16 字段，Name setter 自动拼音码） | `FormulaDetailModel`（Herbs=ObservableCollection\<FormulaHerbItemModel\> ✅） |
| EditContext | `HerbEditContext`（编辑真源，XAML 对象 DP 绑定目标） | `FormulaEditContext`（Herbs 同型，但 **从未被填充/读取**【已读】——编辑行实际走 `FormulaHerbItemViewModel`） |
| Item Model | — | `FormulaHerbItemModel`（7 字段，替代 DTO） |
| MasterDetail VM | `HerbMasterDetailViewModel` | `FormulaMasterDetailViewModel`（**注入的 FormulaDetailModelMapper 全程未使用**【已读】） |
| Editor VM | `HerbEditorViewModel`（EditorViewModelBase\<HerbEditContext\>） | `FormulaEditorViewModel`（编辑行=FormulaHerbItemViewModel 集合） |
| Repository | `HerbRepository`（EntityApiClientRepositoryBase，api 段=apiClient.Herbs） | `FormulaRepository`（同构，apiClient.Formulas） |
| Service | `RemoteHerbService`（CrudServiceBase，CommandResult\<T\>） | `FormulaService`（手写 try/catch+CommandResult，不继承基类） |
| Mapper | 无（全内联手写） | `FormulaDetailModelMapper`（Mapperly，仅服务 DetailModel） |
| StatusHandler | `HerbStatusHandler` | `FormulaStatusHandler` |

- **DTO 编辑边界**：两域编辑真源均为 EditContext，VM 不直接编辑 DTO；DTO 仅在 `InitializeFromDto`/`GetHerbData`/`GetHerbInputDtos` 边界手写转换。✅ 符合 spec。
- **已知待办「Formula Herbs 集合类型」验证：已修复** —— `FormulaDetailModel.Herbs` 与 `FormulaEditContext.Herbs` 均为 `ObservableCollection<FormulaHerbItemModel>`（Models/FormulaDetailModel.cs:30,134；Models/Items/FormulaEditContext.cs:24,93）。
- 接口归属：`IHerbService`/`IFormulaService`→Contracts.Services；`IHerbSearchProvider`/`IFormulaSearchProvider`→Contracts.Services.CrossModule；`IHerbRepository`/`IFormulaRepository`→Contracts.Repositories；StatusHandler 接口为模块本地。
- Repository 返回裸类型、Service 包装 CommandResult\<T\> 分层成立；但 `FormulaSearchProvider` 直通 Repository 返回裸类型（接口契约所致），`HerbSearchProvider` 经 Service 解包——两个 Provider 返回风格不一致【已读】。

### 2.2 Patients（患者）

- **Model**：`PatientDetailModel`（ValidatableModelBase）+ `PatientDetailDisplayModel`（只读格式化）+ `PatientEditContext`（编辑真源）。
- **VM**：`PatientMasterDetailViewModel`（MasterDetailViewModelBase\<PatientListDto,PatientDetailModel\>）+ `PatientEditorViewModel`（EditorViewModelBase\<PatientEditContext\>，DTO→Context:40 / Context→InputDto:62 手写）+ `PatientCardReaderViewModel`。
- **DataServices**：`PatientService`（CrudServiceBase → IPatientRepository；**ToggleStatusCoreAsync 抛 NotSupportedException**【已读】）；`PatientCardReaderIntegration`；`PatientRepository`（EntityApiClientRepositoryBase，GetByIdNumberAsync=搜索+逐条比对）。
- **DTO 编辑边界**：编辑全走 Editor 子 VM 的 EditContext，主 VM 不直改 DTO；保存回写 DetailModel。✅
- 命名空间 `LYBT.Desktop.Patients.*` 统一。✅

### 2.3 Users（用户）

- **Model**：`UserDetailModel` + `UserEditContext`（+IsUserNameReadOnly +Clone）。
- **VM**：`UserMasterDetailViewModel`（MasterDetailViewModelBase\<UserListDto,UserDetailModel\>）+ `UserEditorViewModel`（User=UserEditContext）+ Handlers（`UserPasswordHandler`/`UserStatusHandler`）。
- **DataServices**：`UserRepository`（EntityApiClientRepositoryBase → IApiClient.Identity；GetDoctors/ChangeProfile/ChangePassword/ResetPassword/ToggleStatus/Restore/BatchDelete）；`RemoteUserService`（CrudServiceBase；GetAllAsync 分页拉全量+逐条 GetById）。
- **DTO 编辑边界**：编辑走 EditContext ✅；但 `UserMasterDetailViewModel.LoadDetailAsync` 内手写 **双份映射**（new UserDetailDto:194 + new UserDetailModel:212，DTO 仅用于喂 editor）【已读】——DTO 边界内的冗余构造。
- 命名空间统一 ✅。

### 2.4 Auth — 无数据层，职责纯净

- 6 个 VM 全部不触 DTO：`LoginViewModel`（组合子 VM）、`LoginCredentialsViewModel`（DPAPI 凭证）、`ConnectionStatusViewModel`、`ConnectionTestViewModelBase`（抽象）、`FirstRunSetupViewModel`、`ServerConfigViewModel`。
- **认证链路**（【已读】）：`LoginViewModel:247` → `ILoginCoordinator`（Shell LoginCoordinator.cs:111）→ `IAuthenticationService`（Foundation AuthenticationService.cs:54）→ `IApiClientIdentity`。Auth 模块内 **0 个 IAuthApi/IApiClient 直用**。
- Module 注册仅 VM + 导航 + Dialog；无 Repository/Service（注释：服务由 Core 统一注册）。

### 2.5 Registrations（挂号）— 重构第一步已完成

| 项 | 现状 |
|----|------|
| Model | `RegistrationDetailModel`（ValidatableModelBase，158 行）✅ 新增 |
| EditContext | `RegistrationEditContext`（101 行，ToInputDto()）✅ 新增 |
| MasterDetail VM | `RegistrationListViewModel`（NavigableViewModelBase；WaitingQueue/SelectedRegistration=RegistrationDetailModel；DTO→Model 在 LoadQueueAsync 内手写 Select 映射）✅ |
| Dialog VM | `RegistrationCreateDialogViewModel`：构造 `RegistrationEditContext` → `ToInputDto()` → `CreateAsync` ✅（不再直接构造 DTO） |
| Service | `IRegistrationService`（Contracts.Services）→ 实现见 `RemoteRegistrationService` |
| Repository | `RegistrationRepository`（ApiClientRepositoryBase\<RegistrationListDto,RegistrationDetailDto\>） |
| 其他 | `SignalRClient`（US-REG-008 医生实时队列）、`RegistrationRefreshedEvent` |

- **已知待办「Registration 补 Model + EditContext」验证：已修复**（commit `6c159d8f6` D-4）。`RegistrationListViewModel` 持有 Model 非 DTO；弹窗经 EditContext 构造输入。
- **残留**：Dialog VM 仍直接持有 `ObservableCollection<PatientListDto>`（患者搜索结果）与 `UserListDto`（医生列表）——只读选择列表，非编辑属性，DP-M1 豁免范围内【已读】。
- **目录残留**：`LYBT.Desktop.Registrations_s1utir2s_wpftmp.csproj`（62KB WPF 临时项目文件）躺在模块根目录【已读】——未入库（git ls-files 无匹配），属本地构建残留，建议清理。

### 2.6 MedicalCase（医案）— 重构第一步部分完成，DTO 直编辑残留最多

| 项 | 现状 |
|----|------|
| Model | `MedicalCaseDetailModel`（ValidatableModelBase；**PrescriptionItems 直接持 ObservableCollection\<PrescriptionItemDto\>** :36/:170） |
| EditContext | **两个同名类**：`Services/MedicalCaseEditContext`（sealed，DTO 快照共享上下文 CurrentDetail/OriginalDetail/Cached*，被 Service 四件套注入）与 `Models/Items/MedicalCaseEditContext`（诊断字段编辑上下文 PresentIllness 等 5 字段——**grep 全库无任何引用，死代码**） |
| MasterDetail VM | `MedicalCaseMasterDetailViewModel`（MasterDetailViewModelBase\<MedicalCaseListDto,MedicalCaseDetailModel\>，组合 Consultation/Prescription Editor） |
| Workspace VM | `MedicalCaseWorkspaceViewModel` 位于 **Roles/Clinical**（595 行，自带 TODO「超大类型，建议拆分」） |
| 子 VM | Workspace/：`ConsultationEditorViewModel`（62 行）、`PrescriptionEditorViewModel`（92 行）、`MedicalCaseCommandsViewModel`（514 行）；Items/：`PrescriptionItemViewModel`（470 行） |
| Components | `EditModeStateMachine`（6 状态×10 事件 FSM）、`PrescriptionPrintHandler`（**全库无 DI 注册点**【已读】【推断：可能靠 DryIoc 具体类型解析，未验证】） |
| Repository | `MedicalCaseRepository`（ApiClientRepositoryBase，注入 IApiClient+ILogger） |
| Service | 四拆分：`MedicalCaseService`（聚合代理）/`QueryService`/`CommandService`（156 行）/`LifecycleService`（203 行）+ `AuditLogService`（**唯一直接注入 IApiClient 的 Service**） |
| Mapper | 4 个 Mapperly：ConsultationMapper / PrescriptionMapper / MedicalCaseDetailModelMapper / MedicalCaseCloneMapper |
| Reports 子模块 | `Reports/`（ReportsModule 标 STUB）：ReportService 直注 IApiClient；ReportsHomeViewModel 直持 DailyIncomeDto 等 |

- **已知待办「MedicalCase 补 EditContext」验证：名义完成、实际未接线** —— 新类已建（Models/Items/MedicalCaseEditContext.cs，89 行），但 **0 引用（死代码）**；真实编辑恢复机制仍是 `Services/MedicalCaseEditContext` 的 DTO 快照 + `MedicalCaseCloneMapper.Clone()`（spec §4.6 所述「当前用 Clone() 恢复」依旧）。
- **DTO 直编辑残留（DP-M1 未覆盖点）**【已读】：
  1. `PrescriptionItemViewModel.Items` = `ObservableCollection<PrescriptionItemDto>`（:164/168），经 `MedicalCaseEditControl.xaml:472` `HerbItems="{Binding Prescription.Items, Mode=TwoWay}"` 直绑到 `HerbListControl`→`HerbItemControl`（Dosage 可编辑）——**处方药材行以 DTO 为 UI 编辑类型**。
  2. `MedicalCaseDetailModel.PrescriptionItems` 同型。
  3. `HistoryCopyDialogViewModel` 直持 `MedicalCaseDetailDto`/`PrescriptionItemDto`；`FormulaImportDialogViewModel` 直持 `FormulaListDto`/`FormulaDetailDto`/`FormulaHerbItemDto`。
  4. `MedicalCaseCommandsViewModel` 直改 `_context.CurrentDetail` 嵌套 DTO（:229/:263 经 ConsultationMapper.ToInputDto）；`MedicalCaseWorkspaceViewModel`（Clinical）:439 直改 `CachedMedicalCase.CaseStatus`。
- **命名空间**：`LYBT.Desktop.Modules.*` 0 残留（全 Desktop grep 0 命中）；csproj 注释「跨模块 ProjectReference 已全部移除」属实。
- **已知待办「PrescriptionItemViewModel 位置修复」验证：已修复** —— 已位于 ViewModels/Items/（commit D-2）。

---

## 3. 已知待办逐项对照（用户要求核验的 4 项）

| # | 待办声明（spec/任务） | 代码现状 | 结论 |
|---|---------------------|---------|------|
| 1 | **Model/EditContext 缺失（Registration/Formula/MedicalCase）** | Registration：`RegistrationDetailModel`+`RegistrationEditContext` 已建且**真实使用**（VM 持 Model、弹窗经 EditContext→ToInputDto）✅；Formula：`FormulaDetailModel`+`FormulaEditContext`+`FormulaHerbItemModel` 已建且 Herbs 集合已改 Model 类型 ✅；MedicalCase：`Models/Items/MedicalCaseEditContext` 已建但 **0 引用（死代码）**，编辑恢复仍走 DTO 快照 Clone | **部分完成**：Registration/Formula 已修复；MedicalCase 仅建类未接线 |
| 2 | **DTO 仅传输不编辑** | Catalog/Patients/Users 三域编辑真源均为 EditContext，VM 不直编辑 DTO ✅；Registration 已 Model 化 ✅；**MedicalCase 大量残留**：处方药材行（PrescriptionItemDto 集合双向绑定）、两个对话框（MedicalCaseDetailDto/FormulaHerbItemDto）、CommandService 与 Workspace VM 直改嵌套 DTO | **部分属实**：5/6 域已闭合，MedicalCase 未闭合（含最核心的处方编辑行） |
| 3 | **目录统一** | 未统一（见 §1.3 矩阵）：Services/、Mappers/ 存在性各模块不同；MedicalCase 独有 Interfaces/Extensions/Reports；spec 已认可 Controls/ vs Views/ 差异为合法 | **属实**（未完成，但部分差异 spec 已判定可不改） |
| 4 | **命名空间问题** | 全 Desktop grep `namespace LYBT.Desktop.Modules.` **0 命中**；6 模块 + 2 Roles + 5 Core 全部 `LYBT.Desktop.{项目}.*` 统一；Q-03/A-25 复数对齐已完成（Registrations） | **已修复**（但存在 `MedicalCaseEditContext` 双类同名隐患，不同命名空间可编译） |

### 3.1 架构测试（DP-M1/M2/M3）已补但存在盲区

- DP-M1（VM 禁持有可写 DTO 属性）只查 `PropertyType.Name.EndsWith("Dto")` 的**直接声明属性**——`ObservableCollection<PrescriptionItemDto>` 类型名不以 Dto 结尾，**绕过检查**（这正是处方行的逃逸路径）；白名单豁免了 `CurrentPatient`/`SelectedPatient`/`CurrentDetail` 等 10 个基类/接口属性。
- DP-M2（MasterDetail 模块必须有 DetailModel）已含 5 模块（Users/Patients/MedicalCase/Catalog/Registrations）——通过。
- DP-M3（禁 `LYBT.Desktop.Modules.` 前缀）无独立测试（已由 Q-03 代码层面清零）。

---

## 4. 与 Server 层的关系

### 4.1 双控制器树（LocalWebAPI 11 个 vs Remote 10 个）

| Local（LocalWebAPI/Controllers/） | Remote（LYBT.WebAPI/Controllers/） | 差异 |
|-----------------------------------|-------------------------------------|------|
| AuthController（5 端点） | —（并入 IdentityController） | login 走共享 `LoginCommand`（C-3a 后统一，IsLocal=true）；**logout 仅日志 no-op** vs Remote 发 LogoutCommand；refresh/auto-login/validate 走本地 Handler（**平行实现**，与 Remote 共享 Command 并存）；validate 输入源不同（claims vs Authorization 头） |
| UsersController（全继承 BaseUsersController，0 override） | —（并入 IdentityController） | Local 为空派生类 |
| CatalogController（27 action） | CatalogController（26 action） | **Local 多 CloneFormula**（Remote 无克隆端点）；批量导入签名不同（List\<FormulaImportItemDto\> vs FormulaBatchImportInputDto+FileName）；Remote 有 ApiVersion/限流/ProducesResponseType，Local 无 |
| PatientsController（12 端点） | PatientsController（12） | 端点一致，逻辑同源；仅版本/限流/文档差异 |
| RegistrationsController（4 action+继承） | RegistrationsController（4） | 端点一致 |
| MedicalCasesController | MedicalCasesController | Local 多 2 端点（by-status/{status}、pending）；Local **不 override** SetPrescriptionFlag/RecordPrint（继承基类），Remote override |
| ReportsController（3 端点） | ReportsController（8 端点） | **Local 缺 5 个**：trend/income、trend/consultations、doctor-performance、herbs/ranking、patient-flow |
| ConfigurationController（4 端点） | ConfigurationController（5） | **Local 缺 PUT /**（批量更新） |
| DiagnosticsController（7 端点） | DiagnosticsController（4） | **Local 多 3 个**：db-info/version/logs/recent |
| HealthController（3 端点） | HealthController（3） | 授权粒度不同（Local 类级 AllowAnonymous + details 单独 Authorize + UserManager 注入）；Remote 类级 Authorize + GET/ping AllowAnonymous |
| DeployController（2 端点） | DeployController（2） | **Local 为 stub**（直接 BusinessFail）；Remote 真实实现（上传+RESTART 确认+重启） |

**要点**：本地模式 11 个控制器全部复用 Server 6 模块的 Service/Handler 层（写走 MediatR Command、读走 ICatalogQueryService/IPatientService/IMedicalCase\*Service/IReportRepository）；基类 5 个（BaseApiController/BaseCrudController/BaseUsersController/BaseMedicalCasesController/BaseRegistrationsController）双端共享。Reports 注入 `IReportRepository`（非 IReportService，与 Remote 不同）【已读】。

### 4.2 调用链（DataServices 全链路）

```
VM → Service(CommandResult<T>) → Repository(裸类型) → IApiClient(聚合根)
  → SwitchingApiClient（IsLocal 路由, SwitchingApiClient.cs:75-77）
      ├─ Local:  HttpClientApiClient（10 个 {Domain}HttpApiClient 适配器, Http/Clients/）
      │            → HTTP → LocalWebAPI Controller（11）→ Server 模块 Service/Handler → Repository → AppDbContext → LocalDB
      └─ Remote: RefitApiClient（包装 11 个 internal Refit 接口）→ HTTP → Remote WebAPI Controller（10）
```

- Repository 层返回裸类型（`EntityApiClientRepositoryBase` 失败返 null / 抛 InvalidOperationException），Service 层包装 `CommandResult<T>`；Desktop 无 `ServiceResult`（仅文档禁令，代码 0 命中）。
- 唯一例外：`AuditLogService`/`Reports.ReportService` 直注 IApiClient（跳过 Repository）；`LoginCoordinator` 用 CommandResult\<T\>（服务层例外）。
- 双适配器为**架构性重复**（by design，ADR-0009/0010）：每域一对 Refit/HttpClient 实现同一 `IApiClient*` 接口。

### 4.3 DTO 复用

- **全部复用 `LYBT.Shared.Models.Contracts.*`**；LocalWebAPI 未定义任何本地 DTO（仅 3 个 MediatR record 包装共享 DTO）。
- 例外：Diagnostics/Configuration 部分响应返回匿名对象 `new { ... }`（非 Shared DTO 非本地类型）；Remote DeployController 定义本地 `RestartConfirmDto`（Local 无需求，stub）。
- 每域 3 DTO 模式（XxxListDto/XxxDetailDto/XxxInputDto）+ Formula 的 FormulaHerbItemDto/InputDto + MedicalCase 的 Consultation/Prescription DTO。

### 4.4 文档与 ADR 失实（重要）

| 项 | 声明 | 实际 |
|----|------|------|
| **ADR-0010 引用清单** | 8 个 Server 模块（Auth/Users/Patients/Herbs/Formulas/MedicalCases/Registration/Reports） | csproj 实际 6 个（Identity/Patients/Catalog/MedicalCase/Registration/Reports）；Auth+Users→Identity、Herbs+Formulas→Catalog 已合并；**ADR 变更协议未执行** |
| LocalWebAPI README | 端口 5300 / 8 模块 / EnsureCreatedAsync / HerbsController+FormulasController / 5 授权策略 / 无 Token 刷新 | 端口 **5290**（Program.cs:5）/ 6 模块 / MigrateAsync / 实际 CatalogController / **6 策略** / 存在 LocalRefreshTokenCommandHandler；README 目录树中 `Mappers/LocalApiMapper.cs`+`Repositories/Http*Repository`（6 个）**在代码中不存在** |
| 「12 controllers」说法 | README 无此字样 | 实际 11 个；「12」仅出现在 README 的 FormulasController 端点计数行 |

### 4.5 端口不一致（高风险发现，需修复基线确认）

- **Shell 侧**：`Shell/appsettings.json:48` `LocalApiBaseUrl: http://localhost:5300`；`StatusBarManager.cs:27` 硬编码 `http://127.0.0.1:5300`。
- **LocalWebAPI 侧**：独立入口 `Program.cs:5` 监听 `http://127.0.0.1:5290`。
- **关键修正**【已读】:嵌入式启动路径 `EmbeddedLocalWebApiService.StartAsync` 用 `builder.WebHost.UseUrls(BaseUrl)`（BaseUrl=5300 来自 OfflineModeOptions）覆盖 `CreateBuilder()`（其内部无 UseUrls）——**嵌入式模式实际监听 5300，与 Shell 配置一致**。5290 仅独立运行 LocalWebAPI 时生效（launchSettings 另指向 57703/57704，均不覆盖嵌入式路径）。→ scout 初判「端口不一致导致本地模式不可达」为【推断】，经主 agent 核查嵌入式覆盖逻辑后**判定不成立**，但 5300/5290 双端口并存仍是配置卫生问题，重构时应统一。

---

## 5. 其他基线事实

### 5.1 已知问题清单（13c-current-status）核对

| 项 | 声明 | 现状 |
|----|------|------|
| P1-03 5 个超大类型（>600 行） | MedicalCaseCommandService/Repository/HttpClientApiClient/NavigableViewModelBase/PrescriptionPrintService | **全部已降至 600 以下**【已读】：156/370/77/277/194 行（A-03/A-04 + 拆分已落地）；但 Clinical 的 `MedicalCaseWorkspaceViewModel` 595 行 + `MedicalCaseCommandsViewModel` 514 行接近阈值，自带 TODO |
| P1-04 22 个 MediatR trivial Handler | Server Application 层 | Desktop 侧 LocalWebAPI 仅 3 个本地 Handler（Auth 相关），已非「22 个」状态 |
| P1-09 处方价格刷新未实现 | MedicalCasePrescriptionService TODO | 处方价格实时计算已实现（`PrescriptionItemViewModel.SingleDosePrice` 等，P2-4 注释）【已读】 |
| P0-06 104 个 Desktop 测试失败 | 测试主机崩溃 | 未在本报告范围验证（需运行测试环境），维持原状态声明 |

### 5.2 模块级差异速查（重构关注点）

1. **MedicalCase 是 DTO 直编辑重灾区**：处方行、两个对话框、CommandService/Workspace 直改嵌套 DTO——spec DP-M1 的目标态尚未在该域达成。
2. **同名类隐患**：`MedicalCaseEditContext` 双份（Services/ 与 Models/Items/），后者死代码——重构第二步应二选一或改名。
3. **死依赖/惰性属性**：`FormulaMasterDetailViewModel._mapper`（注入未用）；`FormulaEditContext.Herbs`（从未填充）；`Models/Items/MedicalCaseEditContext`（0 引用）；`PatientService.ToggleStatusCoreAsync` 抛 NotSupportedException（桩）。
4. **双轨转换风格**：Herb/Patient/User/Registration 全内联手写映射（spec 判定简单域可接受）；Formula/MedicalCase 用 Mapperly；`UserMasterDetailViewModel` 有冗余双份构造。
5. **Service 风格不一**：`RemoteHerbService` 继承 CrudServiceBase、`FormulaService` 手写、`MedicalCaseService` tuple 返回（LoadDetailsAsync 返回 `(bool, DTO?, string?)` 非 CommandResult，与其余不一致——desktop-class-method-audit 已记录）。
6. **SearchProvider 返回风格不一**：HerbSearchProvider（经 Service，解包 CommandResult）vs FormulaSearchProvider（直通 Repository，裸类型）。

---

## 6. 结论（现状基线一句话）

Desktop 层 2026-08-09 重构第一步（commit `6c159d8f6`，spec v1.0 的 D-1~D-6）已落地：**Registration 补 Model/EditContext ✅、Formula Herbs 集合改 Model ✅、命名空间清零 ✅、PrescriptionItemViewModel 归位 ✅、DP-M1/M2 测试补上 ✅**；**MedicalCase 的 EditContext 仅建类未接线（死代码）❌、处方药材行与两个对话框仍直编辑 DTO ❌、目录结构未统一 ❌**。架构骨架（三层 + Contracts 接口 + 双适配器 + LocalWebAPI 复用 Server 服务层 + DTO 全共享）健康；主要风险在 MedicalCase 域编辑边界、LocalWebAPI README/ADR-0010 文档失实、5300/5290 双端口卫生问题。

---

*本报告为 READ-ONLY 基线产出，未修改任何代码。*
