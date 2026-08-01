# 统一代码质量优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 统一代码质量：跨模块接口下沉 Contracts、清理死 using、拆分 5 个超大类型（<400 行/文件）、模块依赖审查，最终 build 0 错误 + Architecture 测试通过。

**Architecture:** 4 组独立改动：(U1) `IPatientService`/`IUserService` 从模块 `Interfaces/` 移到 `LYBT.Desktop.Contracts/Services/`；(U2) 核实并保留（前提有误）；(U3) 5 个超大类型按职责拆分为独立类/partial 文件（Server 2 个 + Desktop 3 个）；(U4) 清理 Registration 模块对 Patients/Users 的项目引用。每个改动独立 commit，最后 push。

**Tech Stack:** .NET 8 / WPF Prism / ASP.NET Core / EF Core。验证命令：`dotnet build LYBTZYZS.sln`、`dotnet test tests/LYBT.Tests.Architecture/`。

## Global Constraints

- 中文业务注释、英文标识符；公共 PascalCase、私有 _camelCase、接口 I 前缀。
- 模块间禁止直接引用；跨模块依赖一律通过 Contracts 层。
- 拆分后每个文件 < 400 行；删除 5 个 TODO 注释（`// TODO: 超大类型，建议拆分（详见 docs/compose/reports/code-review-duplicates.md 🟡5）`）。
- Server 命名空间 `LYBT.Module.MedicalCases.Services` 中的 public 类必须以 Service/Manager/Provider/Summary/Rules/Helper/Facade/SeedData/Base/Validation 结尾（`Services_Should_Have_Service_Suffix` 架构测试）。
- Desktop `Services_Should_Follow_Naming_Convention` 用 `ResideInNamespace("Services")`（空匹配，实际无约束），但 Server 命名约束必须严格遵守。
- 每个任务结束必须 `dotnet build LYBTZYZS.sln` 0 错误；U1/U4 后跑 Architecture 测试。
- Commit 格式：`refactor(desktop|server|domain): 描述`；一次独立改动一个 commit；最后 `git push origin master`。
- 验证基线：当前工作树干净，master 与 origin/master 同步（HEAD=3484d7ee6）。

---

### Task 1: U1 — IPatientService/IUserService 下沉到 Contracts

**Covers:** U1

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IPatientService.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUserService.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientService.cs`
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserService.cs`
- Modify: 见下方"using 更新清单"全部文件

**Interfaces:**
- Consumes: 现有接口方法签名完全不变（`IPatientService` 9 方法、`IUserService` 13 方法）
- Produces: 新命名空间 `LYBT.Desktop.Contracts.Services`；DI 注册 `IPatientService→PatientService`、`IUserService→RemoteUserService` 不变

- [ ] **Step 1: 创建 Contracts 层接口文件**

创建 `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IPatientService.cs`，内容 = 原文件逐字（方法签名/XML 注释全保留），仅改两处：
- `using LYBT.Desktop.Contracts.Results;` 可保留（同程序集，无害）
- `namespace LYBT.Desktop.Patients.Interfaces` → `namespace LYBT.Desktop.Contracts.Services`

创建 `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUserService.cs`，同样逐字迁移，namespace 改为 `LYBT.Desktop.Contracts.Services`。

- [ ] **Step 2: 删除原文件**

```bash
git rm src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientService.cs
git rm src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserService.cs
```

- [ ] **Step 3: 更新所有 using 引用（精确清单）**

原则：文件同时用 `Patients.Interfaces` 其他类型（IPatientSearchCache/IPatientValidator/IMedicalCaseStartCoordinator）则保留该 using；只用 IPatientService 则替换为 `using LYBT.Desktop.Contracts.Services;`。`Users.Interfaces` 仅含 IUserService，移走后该 using 一律替换。

| 文件 | 改动 |
|---|---|
| `Modules/LYBT.Desktop.Registration/Dialogs/RegistrationCreateDialogViewModel.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;`（line 7）、删 `using LYBT.Desktop.Users.Interfaces;`（line 8）；Contracts.Services 已导入（line 4） |
| `Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;`（line 8）；Contracts.Services 已导入（line 4） |
| `Roles/LYBT.Desktop.Clinical/Receptionist/ViewModels/ReceptionistHomeViewModel.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;`（line 11）；Contracts.Services 已导入（line 7） |
| `Modules/LYBT.Desktop.Patients/Services/PatientService.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;`（line 2），加 `using LYBT.Desktop.Contracts.Services;` |
| `Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;`（line 6）；Contracts.Services 已导入（line 3） |
| `Modules/LYBT.Desktop.Patients/Services/PatientSearchManager.cs` | 保留 `using LYBT.Desktop.Patients.Interfaces;`（还需 IPatientSearchCache），加 `using LYBT.Desktop.Contracts.Services;` |
| `Modules/LYBT.Desktop.Patients/PatientsModule.cs` | 保留 `using LYBT.Desktop.Patients.Interfaces;`（还需 IPatientSearchCache/IPatientValidator/IMedicalCaseStartCoordinator）；Contracts.Services 已导入（line 3） |
| `Modules/LYBT.Desktop.Patients/Services/PatientCardReaderIntegration.cs` | 检查实际使用；若只用 IPatientService 则按上法替换，否则保留 |
| `Modules/LYBT.Desktop.Users/UsersModule.cs` | 删 `using LYBT.Desktop.Users.Interfaces;`（line 4），加 `using LYBT.Desktop.Contracts.Services;` |
| `Modules/LYBT.Desktop.Users/Services/RemoteUserService.cs` | 删 `using LYBT.Desktop.Users.Interfaces;`（line 3），加 `using LYBT.Desktop.Contracts.Services;` |
| `Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs` | 删 `using LYBT.Desktop.Users.Interfaces;`（line 10）；Contracts.Services 已导入（line 4） |
| `Modules/LYBT.Desktop.Users/ViewModels/Handlers/UserStatusHandler.cs` | 删 `using LYBT.Desktop.Users.Interfaces;`（line 4），加 `using LYBT.Desktop.Contracts.Services;` |
| `Modules/LYBT.Desktop.Users/ViewModels/Handlers/UserPasswordHandler.cs` | 删 `using LYBT.Desktop.Users.Interfaces;`（line 2），加 `using LYBT.Desktop.Contracts.Services;` |
| `tests/LYBT.Tests.Desktop/Unit/Patients/PatientMasterDetailViewModelTests.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;`，加 `using LYBT.Desktop.Contracts.Services;`（如已导入则只删） |
| `tests/LYBT.Tests.Desktop/Unit/Users/UserMasterDetailViewModelTests.cs` | 删 `using LYBT.Desktop.Users.Interfaces;`；Contracts.Services 已导入（line 2） |
| `tests/LYBT.Tests.Desktop/Unit/Registration/RegistrationMasterDetailViewModelTests.cs` | 删 `using LYBT.Desktop.Patients.Interfaces;` + `using LYBT.Desktop.Users.Interfaces;`；Contracts.Services 已导入（line 5） |
| `tests/LYBT.Tests.Desktop/Unit/Clinical/CardReaderDataFillTests.cs` | 仅用 IPatientRepository（Contracts.Repositories），`using LYBT.Desktop.Patients.Interfaces;` 保留（命名空间仍存在，无类型被引用则属既有死 using，本次不动） |
| `tests/LYBT.Tests.Desktop/Unit/Clinical/PatientMatchFallbackChainTests.cs` | 同上，不动 |

- [ ] **Step 4: 检查 DI 注册无需改动**

`PatientsModule.cs:52` `containerRegistry.Register<IPatientService, Services.PatientService>()`、`UsersModule.cs:38` `containerRegistry.Register<IUserService, Services.RemoteUserService>()` — 类型解析依赖 using，已在上步处理，注册代码本身不改。

- [ ] **Step 5: 构建验证**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors, 0 warnings 新增。

- [ ] **Step 6: 架构测试**

```bash
dotnet test tests/LYBT.Tests.Architecture/
```
Expected: 全部通过（重点 `All_Repository_Interfaces_Should_Be_In_Contracts`、`Business_Modules_Should_Not_Reference_Other_Business_Modules`）。

- [ ] **Step 7: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IPatientService.cs src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IUserService.cs src/Client/Desktop/Modules/LYBT.Desktop.Patients src/Client/Desktop/Modules/LYBT.Desktop.Users src/Client/Desktop/Modules/LYBT.Desktop.Registration src/Client/Desktop/Roles/LYBT.Desktop.Clinical tests/LYBT.Tests.Desktop
git commit -m "refactor(desktop): move IPatientService/IUserService to Contracts layer"
```

---

### Task 2: U2 — 核实"死 using"前提（不删，保留）

**Covers:** U2

**Files:**
- Modify: 无（本任务不改代码）

**Interfaces:**
- Consumes: —
- Produces: 文档化结论：`using LYBT.Desktop.MedicalCase.Models;` 是**活 using**，不可删除

- [ ] **Step 1: 核实用法（证据）**

`src/Client/Desktop/Modules/LYBT.Desktop.Registration/ViewModels/RegistrationListViewModel.cs`：
- line 213 `MedicalCaseNavigationParameters.MedicalCaseIdKey`
- line 215 `MedicalCaseNavigationParameters.WorkspaceModeKey` + `WorkspaceMode.Clinical`
- line 216 `MedicalCaseNavigationParameters.InitialEditStateKey` + `EditState.Editing`

三者（`MedicalCaseNavigationParameters`、`WorkspaceMode`、`EditState`）均定义于 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/`，namespace 均为 `LYBT.Desktop.MedicalCase.Models`；全局 `GlobalUsings.cs` 未包含该命名空间。

**结论：该 using 被 line 213-216 使用，删除将导致编译错误（违反本任务"build 0 错误"验证门禁）。U2 前提有误，不改代码。**

- [ ] **Step 2: 说明**

在最终报告/提交说明中记录此发现。若后续要消除 Registration→MedicalCase 依赖，需另行把 `WorkspaceMode/EditState/MedicalCaseNavigationParameters` 下沉 Contracts（超出本任务 U1 范围，需用户确认）。

---

### Task 3: U3-1 — Server MedicalCaseCommandService.cs 拆分（882 → 4 文件）

**Covers:** U3-1

**Files:**
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCasePrescriptionService.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Services/PrescriptionItemService.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.Deletion.cs`（partial，Delete 操作）
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/MedicalCaseModule.cs`

**Interfaces:**
- Consumes: `IMedicalCaseCommandService`（不变）、`IMedicalCaseRepository`、`ICrossModuleService`、`MedicalCaseMapper`、`ICacheInvalidationService`、`BaseService<T>`、`MedicalCaseServiceHelper`
- Produces: `MedicalCasePrescriptionService`（public，ctor 注入 IMedicalCaseRepository、ICrossModuleService、MedicalCaseMapper、ICacheInvalidationService、ILogger、PrescriptionItemService）；`PrescriptionItemService`（public，ctor 注入 IMedicalCaseRepository、ICrossModuleService、ILogger）。两个新类均在 `LYBT.Module.MedicalCases.Services` 命名空间，以 Service 结尾满足命名约束。主类 ctor 注入 `MedicalCasePrescriptionService` + `PrescriptionItemService`（因 `CreateFromInputDtoAsync`/`ExecuteSaveAttemptAsync` 需调用 item 服务）

- [ ] **Step 1: 创建 `PrescriptionItemService.cs`（≈190 行）**

从 `MedicalCaseCommandService.cs` 逐字搬移以下私有方法 + 编号生成（保留 XML 注释，去掉 `// TODO: 价格刷新` 类中文注释按原文保留）：
- `HandlePrescriptionUpdateAsync(MedicalCase, PrescriptionInputDto, CancellationToken)`
- `SoftDeletePrescriptionIfExists(MedicalCase)`
- `CreateNewPrescriptionAsync(MedicalCase, PrescriptionInputDto, CancellationToken)`
- `UpdateExistingPrescriptionAsync(Prescription, PrescriptionInputDto, CancellationToken)`
- `CreatePrescriptionItemsAsync(Guid, PrescriptionInputDto, CancellationToken)`（含 AD-02 禁用药过滤、UnitPrice 自动填充）
- `GeneratePrescriptionNumberAsync(CancellationToken)`

骨架：
```csharp
namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 处方条目构建服务 - 处方明细创建/更新/软删除/编号生成
    /// 从 MedicalCaseCommandService 拆分（超大类型）
    /// </summary>
    public class PrescriptionItemService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly ICrossModuleService _crossModule;
        private readonly ILogger _logger;

        public PrescriptionItemService(
            IMedicalCaseRepository repository,
            ICrossModuleService crossModule,
            ILogger<PrescriptionItemService> logger)
        { /* 赋值 + ArgumentNullException */ }

        // 上述 6 个方法逐字迁入
    }
}
```

- [ ] **Step 2: 创建 `MedicalCasePrescriptionService.cs`（≈290 行）**

从 `MedicalCaseCommandService.cs` 逐字搬移以下方法（保留签名/注释；`SetPrescriptionFlagAsync` 与 `CreatePrescriptionAsync`/`UpdatePrescriptionAsync`/`DeletePrescriptionAsync` 为 public 接口方法）：
- `SetPrescriptionFlagAsync(Guid, bool, Guid, bool, CancellationToken)` → 内部调用 `_itemService.SoftDeletePrescriptionIfExists`
- `CreatePrescriptionAsync(Guid, PrescriptionInputDto, CancellationToken)` + `ExecuteCreatePrescriptionAsync` → 调 `_itemService.CreatePrescriptionItemsAsync`、`_itemService.GeneratePrescriptionNumberAsync`
- `CopyHistoricalPrescriptionAsync` + `ExecuteCopyHistoricalPrescriptionAsync`（保持 public，即使 in_degree=0，见报告 🔵6，不删除）
- `UpdatePrescriptionAsync(Guid, Guid, PrescriptionInputDto, Guid, bool, string?, CancellationToken)` → 调 `_itemService.CreatePrescriptionItemsAsync`
- `DeletePrescriptionAsync(Guid, Guid, Guid, bool, CancellationToken)`

ctor 注入：`IMedicalCaseRepository`、`ICrossModuleService`、`MedicalCaseMapper`、`ICacheInvalidationService`、`ILogger<MedicalCasePrescriptionService>`、`PrescriptionItemService`。权限检查继续用 `MedicalCaseServiceHelper.EnsureCanEdit/EnsureCanDelete`。

- [ ] **Step 3: 创建 `MedicalCaseCommandService.Deletion.cs`（≈95 行）**

`partial class MedicalCaseCommandService`（同命名空间 `LYBT.Module.MedicalCases.Services`，`public partial class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService`），逐字迁入：`DeleteAsync`、`BatchDeleteAsync`。共享主文件 fields/ctor。

- [ ] **Step 4: 精简 `MedicalCaseCommandService.cs`（≈320 行）**

保留：usings、类头（删除 TODO 注释，改为 `public partial class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService`）、fields、ctor（新增 `MedicalCasePrescriptionService` + `PrescriptionItemService` 两个参数）、`CreateAsync`、`CreateFromInputDtoAsync`（line 133 改为 `_itemService.CreateNewPrescriptionAsync(...)`）、`UpdateConsultationAsync`、`SaveAsync`、`ExecuteSaveAttemptAsync`（line 579 改为 `_itemService.HandlePrescriptionUpdateAsync(...)`）、`ValidateEditPermission`、`UpdateMedicalCaseBasicFields`、`UpdateConsultationFields`、`GenerateCaseNumberAsync`。

接口 5 个处方方法改为单表达式委托（`/// <inheritdoc />` + 一行表达式体）：
```csharp
/// <inheritdoc />
public Task<Prescription?> CreatePrescriptionAsync(Guid medicalCaseId, PrescriptionInputDto request, CancellationToken cancellationToken = default)
    => _prescriptionService.CreatePrescriptionAsync(medicalCaseId, request, cancellationToken);
```
（`SetPrescriptionFlagAsync`、`UpdatePrescriptionAsync`、`DeletePrescriptionAsync`、`CopyHistoricalPrescriptionAsync` 同理委托；`CopyHistoricalPrescriptionAsync` 不在接口中，仍保留为 public 委托。）

- [ ] **Step 5: 注册 DI**

`MedicalCaseModule.cs` 的 `AddMedicalCaseModule` 中新增：
```csharp
services.AddScoped<MedicalCasePrescriptionService>();
services.AddScoped<PrescriptionItemService>();
```

- [ ] **Step 6: 构建验证**

```bash
dotnet build LYBTZYZS.sln
wc -l src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.Deletion.cs src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCasePrescriptionService.cs src/Server/Modules/LYBT.Module.MedicalCase/Services/PrescriptionItemService.cs
```
Expected: 0 errors；四文件均 < 400 行。

- [ ] **Step 7: 架构测试**

```bash
dotnet test tests/LYBT.Tests.Architecture/ --filter "FullyQualifiedName~Services_Should_Have_Service_Suffix"
```
Expected: 通过（新类均以 Service 结尾）。

- [ ] **Step 8: Commit**

```bash
git add src/Server/Modules/LYBT.Module.MedicalCase
git commit -m "refactor(server): split MedicalCaseCommandService prescription logic into dedicated services"
```

---

### Task 4: U3-2 — Server MedicalCaseRepository.cs 拆分（687 → 4 partial 文件）

**Covers:** U3-2

**Files:**
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.PendingCases.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.AuditLogs.cs`
- Create: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.Update.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

**Interfaces:**
- Consumes: `IMedicalCaseRepository`（不变）、`BaseRepository<MedicalCase>`、`AppDbContext`
- Produces: `internal partial class MedicalCaseRepository`（4 个文件同命名空间 `LYBT.Module.MedicalCases.Repositories`，partial 声明；DI 注册 `AddScoped<IMedicalCaseRepository, MedicalCaseRepository>` 不变）

- [ ] **Step 1: 主文件改 partial 并删 TODO**

`MedicalCaseRepository.cs`：`internal class` → `internal partial class`；删除 line 21 TODO 注释；保留：usings、ctor、`GetBaseQuery`、`GetDetailQuery`、`GetByPatientIdAsync`、`GetByPatientIdPagedAsync`、`GetByPatientIdWithDetailsAsync`、`GetByIdWithDetailsAsync`、`GetPagedWithDetailsAsync`、`QueryAsync`、`QueryPagedAsync`、`GetUnfinishedCaseByPatientIdAsync`、`CountByPrefixAsync`、`CountPrescriptionsByPrefixAsync`、`GetBatchWithDetailsAsync`（≈382 行）。

- [ ] **Step 2: 创建 `MedicalCaseRepository.PendingCases.cs`（≈120 行）**

partial 类，迁入：`GetPendingCasesAsync`、`GetAllPendingCasesAsync`、`MaskPhoneNumber`。头部 `internal partial class MedicalCaseRepository`，ctor 由主文件提供（partial 类共享）。

- [ ] **Step 3: 创建 `MedicalCaseRepository.AuditLogs.cs`（≈35 行）**

partial 类，迁入：`GetAuditLogsAsync`、`CountAuditLogsAsync`、`AddPrintLogAsync`。

- [ ] **Step 4: 创建 `MedicalCaseRepository.Update.cs`（≈150 行）**

partial 类，迁入：`GetByIdWithDetailsFreshAsync`、`UpdateAsync`（override）、`FixPrescriptionEntityStatesAsync`、`FixNewPrescriptionItemsState`、`FixExistingPrescriptionItemsStateAsync`、`GetOrLoadExistingEntityAsync`、`LogTrackedEntitiesState`。

- [ ] **Step 5: 构建验证**

```bash
dotnet build LYBTZYZS.sln
wc -l src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository*.cs
```
Expected: 0 errors；每文件 < 400 行。

- [ ] **Step 6: 架构测试 + Commit**

```bash
dotnet test tests/LYBT.Tests.Architecture/
git add src/Server/Modules/LYBT.Module.MedicalCase/Repositories
git commit -m "refactor(server): split MedicalCaseRepository into partial files by concern"
```

---

### Task 5: U3-3 — Desktop HttpClientApiClient.cs 拆分（723 → 12 文件，适配器模式）

**Covers:** U3-3

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpApiClientBase.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/AuthHttpApiClient.cs`、`UsersHttpApiClient.cs`、`PatientsHttpApiClient.cs`、`HerbsHttpApiClient.cs`、`FormulasHttpApiClient.cs`、`MedicalCasesHttpApiClient.cs`、`RegistrationsHttpApiClient.cs`、`ReportsHttpApiClient.cs`、`DeployHttpApiClient.cs`、`DiagnosticsHttpApiClient.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs`（改为 facade）

**Interfaces:**
- Consumes: `IApiClient` + 10 个子接口（`IApiClientAuth`…`IApiClientDiagnostics`）、`IHttpClientFactory`
- Produces: `HttpApiClientBase`（abstract，protected helpers）、10 个 `{Domain}HttpApiClient : HttpApiClientBase, IApiClient{Domain}`（internal sealed，namespace `LYBT.Desktop.Foundation.Http.Clients`，与 Refit 适配器同目录）、`HttpClientApiClient`（facade，只实现 `IApiClient`，ctor 保持 `(IHttpClientFactory)`）

- [ ] **Step 1: 创建 `HttpApiClientBase.cs`（≈200 行）**

从 `HttpClientApiClient.cs` 逐字搬移：`JsonOptions`、`_httpClientFactory` 字段、`CreateClient`、`ToJsonContent`、`DeserializeAsync`、`DeserializeEnvelopeAsync`、`EnsureSuccessOrThrowAsync`、`WrapSuccess`×2、`BuildPagedUrl`、`BuildQueryString`、`SendAsync`、`GetAndWrapAsync`、`GetRawAsync`、`PostAndWrapAsync`、`SendVoidAsync`、`PostVoidAsync`、`PostRawAsync`、`PutAndWrapAsync`、`PutVoidAsync`、`DeleteVoidAsync`、`SendAndWrapAsync`、`GetPagedAndWrapAsync`、`GetResponseAsync`。全部改 `protected`，类为 `internal abstract class HttpApiClientBase`，ctor `protected HttpApiClientBase(IHttpClientFactory)`。

- [ ] **Step 2: 创建 10 个模块适配器**

每个 `{Domain}HttpApiClient`（如 `AuthHttpApiClient`）：`internal sealed class AuthHttpApiClient : HttpApiClientBase, IApiClientAuth`，ctor `(IHttpClientFactory)` → `base(httpClientFactory)`。把 `HttpClientApiClient.cs` 中对应模块的显式接口实现改为普通 public 方法逐字迁入（每个适配器只实现一个子接口，无方法名歧义，去掉 `IApiClientAuth.` 前缀）。文件头保留原 region 注释。估算行数：Auth≈25、Users≈50、Patients≈50、Herbs≈55、Formulas≈65、MedicalCases≈85、Registrations≈55、Reports≈15、Deploy≈12、Diagnostics≈18。

示例（`AuthHttpApiClient.cs`）：
```csharp
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients
{
    /// <summary>本地模式认证 API 客户端（拆分自 HttpClientApiClient）</summary>
    internal sealed class AuthHttpApiClient : HttpApiClientBase, IApiClientAuth
    {
        public AuthHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

        public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)
            => PostAndWrapAsync<LoginResponse>("/api/v1/auth/login", loginRequest);
        // ... 其余方法逐字迁入（去掉显式接口前缀）
    }
}
```

- [ ] **Step 3: 重写 `HttpClientApiClient.cs` 为 facade（≈110 行）**

参照 `RefitApiClient.cs` 的惰性适配器模式：
```csharp
public sealed class HttpClientApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private IApiClientAuth? _auth;
    // ... 10 个字段

    public HttpClientApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public IApiClientAuth Auth => _auth ??= new AuthHttpApiClient(_httpClientFactory);
    // ... 其余 9 个属性
}
```
删除：类头 TODO 注释、原 10 个子接口实现、原私有 helper（已入 base）、原 `IApiClient.Auth => this` 写法。注意 `SwitchingApiClient.cs:77` `new HttpClientApiClient(_localHttpClientFactory(url))` 与 `HttpClientApiClientExtensions` 的 `new HttpClientApiClient(httpClientFactory)` 均兼容（ctor 签名不变）。

- [ ] **Step 4: 构建验证**

```bash
dotnet build LYBTZYZS.sln
wc -l src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpApiClientBase.cs src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/*HttpApiClient.cs
```
Expected: 0 errors；每文件 < 400 行。

- [ ] **Step 5: 架构测试 + Commit**

```bash
dotnet test tests/LYBT.Tests.Architecture/
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http
git commit -m "refactor(desktop): split HttpClientApiClient into per-module adapter classes"
```

---

### Task 6: U3-4 — Desktop NavigableViewModelBase.cs 拆分（719 → 4 partial 文件）

**Covers:** U3-4（评估结论：partial class 拆分可行且零行为变化）

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.Navigation.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.Async.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.Editable.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase.cs`

**Interfaces:**
- Consumes: `IViewModelServices`、`IEditable`、`INavigationAware`、`IRegionMemberLifetime`、`IConfirmNavigationRequest`、CommunityToolkit `[ObservableProperty]`/`[RelayCommand]`
- Produces: `public abstract partial class NavigableViewModelBase`（4 文件同命名空间 `LYBT.Desktop.Infrastructure.ViewModels.Base`；派生类与 DI 零改动）

- [ ] **Step 1: 主文件改 partial**

`NavigableViewModelBase.cs`：`abstract partial class`；删除 line 30 TODO 注释；保留：类头注释、私有字段（`_disposables`/`_eventManager`/`_disposed`）、受保护属性、可观察属性、计算属性、构造函数、属性变更回调、状态管理（`SetBusy`/`ClearError`/`SetError`）、`AddDisposable`（≈310 行）。

- [ ] **Step 2: 创建 `NavigableViewModelBase.Navigation.cs`（≈200 行）**

partial 类，迁入：`INavigationAware` 实现（`IsNavigationTarget`/`OnNavigatedTo`/`OnNavigatedFrom`）、`IConfirmNavigationRequest`（`ConfirmNavigationRequest`/`ConfirmNavigationWithUnsavedChangesAsync`/`ShowUnsavedChangesDialogAsync`/`CanNavigateAway`）、可重写钩子（`OnNavigatedToCore`/`OnNavigatedFromCore`/`InitializeAsync`）、导航命令（`NavigateToHome`）、导航方法（`NavigateTo`/`GetHomeViewName`）。

- [ ] **Step 3: 创建 `NavigableViewModelBase.Async.cs`（≈100 行）**

partial 类，迁入：`ExecuteWithErrorHandlingAsync` ×2（泛型与非泛型）、`RunOnUIThread`、`RunOnUIThreadAsync`。

- [ ] **Step 4: 创建 `NavigableViewModelBase.Editable.cs`（≈120 行）**

partial 类，迁入：对话框方法（`ShowSuccessMessageAsync`/`ShowErrorMessageAsync`/`ShowWarningMessageAsync`/`ShowConfirmMessageAsync`）、辅助方法（`MarkAsChanged`/`MarkAsSaved`）、`IEditable` 显式实现、`BeginEdit`/`CancelEdit`/`EndEdit`/钩子、`IDisposable`（`Dispose`/`Dispose(bool)`/`OnDisposing`）。

- [ ] **Step 5: 构建验证**

```bash
dotnet build LYBTZYZS.sln
wc -l src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/NavigableViewModelBase*.cs
```
Expected: 0 errors；每文件 < 400 行。

- [ ] **Step 6: 架构测试 + Commit**

```bash
dotnet test tests/LYBT.Tests.Architecture/ --filter "FullyQualifiedName~Desktop_ViewModels_Should_Use_Standard_Base_Classes|FullyQualifiedName~Desktop_ViewModels_Should_Inherit_From_Base_Classes"
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base
git commit -m "refactor(desktop): split NavigableViewModelBase into partial files by concern"
```

---

### Task 7: U3-5 — Desktop PrescriptionPrintService.cs 拆分（709 → 4 文件）

**Covers:** U3-5

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionDocumentBuilder.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintExecutor.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPreviewWindowBuilder.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Printing/PrintingModule.cs`

**Interfaces:**
- Consumes: `IPrintService<PrescriptionPrintModel>`、`PrintOptions`、`ExportFormat`、`PrescriptionPrintModel`、`PrescriptionItemPrintModel`、XAML 模板（`PrescriptionPrintTemplate`/`PrescriptionPrintA4Template`/`PrescriptionContinuationTemplate`/`PrescriptionContinuationA4Template`）、QuestPDF `PrescriptionPdfExporter`
- Produces: `PrescriptionDocumentBuilder`（页面构建）、`PrescriptionPrintExecutor`（打印机/执行打印）、`PrescriptionPreviewWindowBuilder`（预览窗+设置面板）——均 public 类、`LYBT.Desktop.Printing.Services` 命名空间；`PrescriptionPrintService` ctor 注入三者

- [ ] **Step 1: 创建 `PrescriptionDocumentBuilder.cs`（≈185 行）**

从 `PrescriptionPrintService.cs` 逐字搬移（保留 T4-S5-09 常量与注释）：`A5PageSize`/`A4PageSize`、`A5FirstPageHerbLimit`/`A4FirstPageHerbLimit`/`ContinuationPageHerbLimit`、`GetPageSize`、`IsA4`、`GetFirstPageHerbLimit`、`BuildFixedDocument`、`BuildMultiPageDocument`、`CloneModelWithItems`、`CreateFixedPage`、`CreateContinuationFixedPage`、`CreatePageFromTemplate`。ctor 注入 `ILogger<PrescriptionDocumentBuilder>`（`BuildFixedDocument`/`BuildMultiPageDocument` 内的日志调用保留）。所有方法改 public（供 service/preview 调用）。

- [ ] **Step 2: 创建 `PrescriptionPrintExecutor.cs`（≈120 行）**

从 `PrescriptionPrintService.cs` 逐字搬移：`_printServer`、`_defaultPrinterName` 字段、`GetAvailablePrinters`、`SetDefaultPrinter`、`GetDefaultPrinter`、`ExecutePrintWithDialog`、`ExecutePrintDirect`、`SetupPrinter`、`GetPrintQueue`。ctor 注入 `ILogger<PrescriptionPrintExecutor>`。

- [ ] **Step 3: 创建 `PrescriptionPreviewWindowBuilder.cs`（≈195 行）**

从 `PrescriptionPrintService.cs` 逐字搬移：`ShowPreviewWindow`、`CreateSettingsPanel`、`PopulatePrinterList`。ctor 注入 `ILogger<PrescriptionPreviewWindowBuilder>`、`PrescriptionDocumentBuilder`、`PrescriptionPrintExecutor`。内部调用改为 `_documentBuilder.BuildFixedDocument(...)`/`_documentBuilder.GetPageSize(...)`、`_executor.ExecutePrintDirect(...)`、`_executor.GetPrintQueues()`（若需枚举队列，在 executor 暴露 `GetPrintQueues()` public 或复用 `GetAvailablePrinters`）。

- [ ] **Step 4: 精简 `PrescriptionPrintService.cs`（≈200 行）**

保留：类头（删 TODO 注释）、`IPrintService<PrescriptionPrintModel>` 实现、ctor（注入 `PrescriptionDocumentBuilder`、`PrescriptionPrintExecutor`、`PrescriptionPreviewWindowBuilder`）、`PrintAsync`、`PreviewAsync`、`ExportAsync`（XPS 分支用 `_documentBuilder.BuildFixedDocument`；PDF 分支不变）、`BatchPrintAsync`。`GetAvailablePrinters`/`SetDefaultPrinter`/`GetDefaultPrinter` 委托 `_executor`（接口要求，public 保留）。

- [ ] **Step 5: 注册 DI**

`PrintingModule.cs` `RegisterTypes` 新增：
```csharp
containerRegistry.RegisterSingleton<PrescriptionDocumentBuilder>();
containerRegistry.RegisterSingleton<PrescriptionPrintExecutor>();
containerRegistry.RegisterSingleton<PrescriptionPreviewWindowBuilder>();
containerRegistry.RegisterSingleton<IPrintService<PrescriptionPrintModel>, PrescriptionPrintService>();
```

- [ ] **Step 6: 构建验证**

```bash
dotnet build LYBTZYZS.sln
wc -l src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/Prescription*.cs
```
Expected: 0 errors；每文件 < 400 行。

- [ ] **Step 7: 架构测试 + Commit**

```bash
dotnet test tests/LYBT.Tests.Architecture/
git add src/Client/Desktop/Core/LYBT.Desktop.Printing
git commit -m "refactor(desktop): split PrescriptionPrintService page-building into dedicated builders"
```

---

### Task 8: U4 — 模块依赖审查 + Registration.csproj 清理

**Covers:** U4

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Registration/LYBT.Desktop.Registration.csproj`

**Interfaces:**
- Consumes: U1 完成后的项目引用状态
- Produces: Registration 仅引用 Infrastructure/Contracts/MedicalCase/Shared；不再引用 Patients/Users

- [ ] **Step 1: 全量审计（证据）**

```bash
grep -rn "ProjectReference" src/Client/Desktop/Modules/*/*.csproj
```
预期结果：仅 `Registration.csproj` 含跨模块引用（MedicalCase/Patients/Users）；其余模块（Auth/Formula/Herbs/MedicalCase/Patients/Users）仅引用 Core（Foundation/Infrastructure/Contracts/Printing）。`_wpftmp.csproj` 为 WPF 构建临时文件，忽略。

- [ ] **Step 2: 移除 Patients/Users 引用**

`Registration.csproj` "Cross-Module Dependencies" ItemGroup 删除两行：
```xml
<ProjectReference Include="..\LYBT.Desktop.Patients\LYBT.Desktop.Patients.csproj" />
<ProjectReference Include="..\LYBT.Desktop.Users\LYBT.Desktop.Users.csproj" />
```
保留 `LYBT.Desktop.MedicalCase`（RegistrationListViewModel 仍需 `WorkspaceMode/EditState/MedicalCaseNavigationParameters`，属既有文档化例外）。

- [ ] **Step 3: 复核无残留引用**

```bash
grep -rn "LYBT.Desktop.Patients\|LYBT.Desktop.Users" src/Client/Desktop/Modules/LYBT.Desktop.Registration --include="*.cs" --include="*.xaml" --include="*.csproj"
```
Expected: 无输出（U1 已移除两个 using）。

- [ ] **Step 4: 构建 + 架构测试**

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Architecture/
```
Expected: 0 errors；架构测试全绿（`Business_Modules_Should_Not_Reference_Other_Business_Modules` 检查的 6 模块本就零互引；Registration 不在受检列表，清理后更干净）。

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Registration/LYBT.Desktop.Registration.csproj
git commit -m "refactor(desktop): remove Registration cross-module references to Patients/Users"
```

---

### Task 9: 终验 + Push

**Covers:** 全部

- [ ] **Step 1: 全量验证**

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Architecture/
```
Expected: build 0 errors；架构测试全通过。

- [ ] **Step 2: 确认 commit 序列**

```bash
git log --oneline -8
git status
```
Expected: 6 个 commit（U1、U3-1、U3-2、U3-3、U3-4、U3-5、U4 = 7 个；U2 无代码改动不产生 commit），工作树干净。

- [ ] **Step 3: Push**

```bash
git push origin master
```

## Self-Review

- **Spec coverage:** U1→Task1 ✓；U2→Task2（前提有误，保留并文档化）✓；U3-1..5→Task3..7 ✓；U4→Task8 ✓；验证/提交→Task9 ✓。
- **Placeholder scan:** 无 TBD/TODO 遗留；所有新文件含完整方法清单与骨架。
- **Type consistency:** `MedicalCasePrescriptionService`/`PrescriptionItemService` 命名与 Task3 内外部调用一致；`HttpApiClientBase`+10 适配器与 facade 属性名一致；`PrescriptionDocumentBuilder`/`PrescriptionPrintExecutor`/`PrescriptionPreviewWindowBuilder` 与 Task7 调用一致。
