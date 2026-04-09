# Cross-Module Decoupling Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove all 7 cross-module ProjectReferences by introducing ISP interfaces (Server) and Provider interfaces (Desktop).

**Architecture:** ICrossModuleService ISP split into 4 domain interfaces in LYBT.Infrastructure. Desktop modules expose narrow Provider interfaces via LYBT.Desktop.Contracts. All implementations registered in DI, consumers migrated to use new interfaces.

**Tech Stack:** .NET 8, EF Core, Prism/DryIoc, xUnit/NSubstitute

**Design Doc:** `docs/plans/2026-02-23-cross-module-decoupling-design.md`

---

## Phase 1: Infrastructure -- Interfaces & Implementation

### Task 1: Create Server ISP Interfaces

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IPatientCrossModuleService.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IHerbCrossModuleService.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IUserCrossModuleService.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ICrossModuleAuthService.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ReferenceCheckResult.cs`

**Step 1: Create directory and IPatientCrossModuleService**

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IPatientCrossModuleService.cs
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 患者域跨模块服务 (ISP: D5-1)
/// 供 MedicalCase + Sync 模块使用
/// </summary>
public interface IPatientCrossModuleService
{
    /// <summary>获取患者基本信息</summary>
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);

    /// <summary>批量获取患者基本信息</summary>
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    /// <summary>检查患者是否存在 (未删除)</summary>
    Task<bool> PatientExistsAsync(Guid patientId);

    /// <summary>检查患者引用关系 (医案引用数)</summary>
    Task<ReferenceCheckResult> CheckPatientReferenceAsync(Guid patientId);
}
```

**Step 2: Create IHerbCrossModuleService**

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IHerbCrossModuleService.cs
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 药材域跨模块服务 (ISP: D5-1)
/// 供 Sync 模块使用
/// </summary>
public interface IHerbCrossModuleService
{
    /// <summary>获取药材基本信息</summary>
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);

    /// <summary>按名称或拼音查找药材</summary>
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);

    /// <summary>检查药材引用关系 (处方引用数)</summary>
    Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId);
}
```

**Step 3: Create IUserCrossModuleService**

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IUserCrossModuleService.cs
using LYBT.Shared.Models.DTOs.Users;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 用户域跨模块服务 (ISP: D5-1)
/// 供 MedicalCase + Auth 模块使用
/// </summary>
public interface IUserCrossModuleService
{
    /// <summary>获取用户基本信息</summary>
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId);

    /// <summary>按用户名获取用户凭证信息 (含密码哈希)</summary>
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username);

    /// <summary>更新用户密码哈希</summary>
    Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash);

    /// <summary>检查用户是否存在 (未删除)</summary>
    Task<bool> UserExistsAsync(Guid userId);
}
```

**Step 4: Create ICrossModuleAuthService**

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ICrossModuleAuthService.cs
namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 认证域跨模块服务 (AUTH-D06/D07)
/// 供 Users 模块在角色变更/禁用等场景触发 Token 撤销
/// </summary>
public interface ICrossModuleAuthService
{
    /// <summary>撤销指定用户的所有 Token Family</summary>
    Task RevokeUserTokensAsync(Guid userId, string reason);
}
```

**Step 5: Create ReferenceCheckResult DTO**

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ReferenceCheckResult.cs
namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 跨模块引用检查结果 (统一 DTO，替代模块内 HerbReferenceCheckDto/PatientReferenceCheckDto)
/// </summary>
public record ReferenceCheckResult(bool HasReferences, int ReferenceCount, string? Message = null);
```

**Step 6: Verify compilation**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: BUILD SUCCEEDED (新增文件不影响现有代码)

---

### Task 2: Implement New Methods in CrossModuleService

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs`

**Step 1: Add interface implementations and new methods**

CrossModuleService 改为实现 4 个新接口 + 保留旧接口。新增 `PatientExistsAsync`、`UserExistsAsync`、`CheckPatientReferenceAsync`、`CheckHerbReferenceAsync` 方法。

CheckReference 逻辑直接查询 DbContext (不依赖模块 Service):
- `CheckPatientReferenceAsync`: 查询 `_context.MedicalCases.CountAsync(mc => mc.PatientId == patientId && !mc.IsDeleted)`
- `CheckHerbReferenceAsync`: 查询 `_context.PrescriptionItems.CountAsync(pi => pi.HerbId == herbId)` (通过 Include 或直连)

ICrossModuleAuthService 暂时抛出 NotImplementedException (Token 撤销在 S1 实现)。

**Step 2: Verify compilation**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: BUILD SUCCEEDED

---

### Task 3: Update DI Registration

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/ServiceCollectionExtensions.cs:146-148`
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs:175-178`

**Step 1: Update Infrastructure ServiceCollectionExtensions**

在 `AddDatabaseServices` 方法中替换旧注册:

```csharp
// 旧:
// services.AddScoped<ICrossModuleService, CrossModuleService>();

// 新: ISP 拆分 (D5-1) -- 4 接口共享同一 Scoped 实例
services.AddScoped<CrossModuleService>();
services.AddScoped<IPatientCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<IHerbCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<IUserCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<ICrossModuleAuthService>(sp => sp.GetRequiredService<CrossModuleService>());
// 旧接口保留兼容 (标记 [Obsolete])
services.AddScoped<ICrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
```

**Step 2: Update WebAPI DatabaseServiceCollectionExtensions**

同样替换 `DatabaseServiceCollectionExtensions.cs:177-178` 的旧注册为新模式。注意: 如果两处都注册会重复，确认只保留一处生效。

**Step 3: Verify compilation**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

---

### Task 4: Mark Old Interface [Obsolete]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Services/ICrossModuleQueryService.cs:18`

**Step 1: Add Obsolete attribute**

```csharp
[Obsolete("使用 IPatientCrossModuleService/IHerbCrossModuleService/IUserCrossModuleService 替代 (D5-1)")]
public interface ICrossModuleService
```

**Step 2: Verify compilation (allow warnings)**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED (有 obsolete warnings，不阻塞)

---

### Task 5: Create Desktop Provider Interfaces

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/CrossModule/IHerbSearchProvider.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/CrossModule/IFormulaSearchProvider.cs`

**Step 1: Create IHerbSearchProvider**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/CrossModule/IHerbSearchProvider.cs
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Services.CrossModule;

/// <summary>
/// 药材搜索提供者 (D5-3)
/// 供 MedicalCase 模块加载药材列表，解耦对 LYBT.Desktop.Herbs 的编译期依赖
/// </summary>
public interface IHerbSearchProvider
{
    /// <summary>搜索药材列表 (keyword 为空时返回全部启用药材)</summary>
    Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword);
}
```

**Step 2: Create IFormulaSearchProvider**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/CrossModule/IFormulaSearchProvider.cs
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.Services.CrossModule;

/// <summary>
/// 验方搜索提供者 (D5-3)
/// 供 MedicalCase 模块导入验方，解耦对 LYBT.Desktop.Formula 的编译期依赖
/// </summary>
public interface IFormulaSearchProvider
{
    /// <summary>分页获取验方列表</summary>
    Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize);

    /// <summary>获取验方详情 (含药材列表)</summary>
    Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id);
}
```

注意: `PagedResult<T>` 需确认在 `LYBT.Shared.Models` 中是否已定义。如未定义，使用 `IReadOnlyList<FormulaListDto>` + 总数参数替代。

**Step 3: Verify compilation**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Contracts/LYBT.Desktop.Contracts.csproj`
Expected: BUILD SUCCEEDED

**Step 4: Commit Phase 1**

```
git add src/Server/Core/LYBT.Infrastructure/Services/CrossModule/
git add src/Server/Core/LYBT.Infrastructure/Services/ICrossModuleQueryService.cs
git add src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs
git add src/Server/Core/LYBT.Infrastructure/ServiceCollectionExtensions.cs
git add src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/CrossModule/
git commit -m "refactor: Phase 1 - ISP interfaces + Desktop Provider interfaces (D5-1/D5-3)"
```

---

## Phase 2: Server Module Migration

### Task 6: Migrate SyncService

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Sync/Services/SyncService.cs`

**Step 1: Replace injected services**

```diff
- using LYBT.Module.Herbs.Interfaces;
- using LYBT.Module.Patients.Interfaces;
+ using LYBT.Infrastructure.Services.CrossModule;

  public class SyncService : ISyncService
  {
      private readonly AppDbContext _dbContext;
-     private readonly IHerbService _herbService;
-     private readonly IPatientService _patientService;
+     private readonly IHerbCrossModuleService _herbCrossModule;
+     private readonly IPatientCrossModuleService _patientCrossModule;
      private readonly ILogger<SyncService> _logger;

      public SyncService(
          AppDbContext dbContext,
-         IHerbService herbService,
-         IPatientService patientService,
+         IHerbCrossModuleService herbCrossModule,
+         IPatientCrossModuleService patientCrossModule,
          ILogger<SyncService> logger)
      {
          _dbContext = dbContext;
-         _herbService = herbService;
-         _patientService = patientService;
+         _herbCrossModule = herbCrossModule;
+         _patientCrossModule = patientCrossModule;
          _logger = logger;
      }
```

**Step 2: Update CheckReference calls (lines ~524-554)**

```diff
  private async Task<(bool canDelete, string? reason)> CanDeleteHerbAsync(Guid herbId)
  {
-     var result = await _herbService.CheckReferenceAsync(herbId);
-     if (!result.IsSuccess || result.Data == null)
-         return (false, "无法检查引用关系");
-     if (result.Data.HasReferences)
-         return (false, $"药材被 {result.Data.ReferenceCount} 个处方引用，请先禁用");
+     var result = await _herbCrossModule.CheckHerbReferenceAsync(herbId);
+     if (result.HasReferences)
+         return (false, $"药材被 {result.ReferenceCount} 个处方引用，请先禁用");
      return (true, null);
  }

  private async Task<(bool canDelete, string? reason)> CanDeletePatientAsync(Guid patientId)
  {
-     var result = await _patientService.CheckReferenceAsync(patientId);
-     if (!result.IsSuccess || result.Data == null)
-         return (false, "无法检查引用关系");
-     if (result.Data.HasReferences)
-         return (false, $"患者有 {result.Data.ReferenceCount} 条医案记录，请先禁用");
+     var result = await _patientCrossModule.CheckPatientReferenceAsync(patientId);
+     if (result.HasReferences)
+         return (false, $"患者有 {result.ReferenceCount} 条医案记录，请先禁用");
      return (true, null);
  }
```

**Step 3: Update SyncService tests**

Modify: `tests/UnitTests/Server/Modules/LYBT.Module.Sync.Tests/`

Replace mocked `IHerbService` / `IPatientService` with `IHerbCrossModuleService` / `IPatientCrossModuleService`.
CheckReference 返回值从 `Result<HerbReferenceCheckDto>` 改为 `ReferenceCheckResult`。

**Step 4: Verify compilation**

Run: `dotnet build src/Server/Modules/LYBT.Module.Sync/LYBT.Module.Sync.csproj`
Expected: BUILD SUCCEEDED

---

### Task 7: Migrate MedicalCase Server Services

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseServiceHelper.cs`

**Step 1: Migrate MedicalCaseCommandService constructor**

```diff
- using LYBT.Module.Patients.Interfaces;
- using LYBT.Module.Users.Interfaces;
+ using LYBT.Infrastructure.Services.CrossModule;

  public class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService
  {
      private readonly IMedicalCaseRepository _repository;
-     private readonly IPatientRepository _patientRepository;
-     private readonly IUserRepository _userRepository;
+     private readonly IPatientCrossModuleService _patientCrossModule;
+     private readonly IUserCrossModuleService _userCrossModule;
      ...

      public MedicalCaseCommandService(
          IMedicalCaseRepository repository,
-         IPatientRepository patientRepository,
-         IUserRepository userRepository,
+         IPatientCrossModuleService patientCrossModule,
+         IUserCrossModuleService userCrossModule,
          IMedicalCaseAuditService auditService,
          IMedicalCasePermissionService permissionService,
          ILogger<MedicalCaseCommandService> logger)
```

**Step 2: Migrate MedicalCaseStateService constructor**

```diff
- using LYBT.Module.Users.Interfaces;
+ using LYBT.Infrastructure.Services.CrossModule;

-     private readonly IUserRepository _userRepository;
+     private readonly IUserCrossModuleService _userCrossModule;
```

**Step 3: Migrate MedicalCaseServiceHelper**

`ValidateAndFetchCreationContextAsync` 和 `GetOperatorInfoAsync` 的参数从 `IPatientRepository` / `IUserRepository` 改为 `IPatientCrossModuleService` / `IUserCrossModuleService`。

关键变更:
- `patientRepository.GetByIdAsync(patientId)` 替换为 `patientCrossModule.GetPatientBasicInfoAsync(patientId)` (返回 DTO)
- `userRepository.GetByIdAsync(userId)` 替换为 `userCrossModule.GetUserBasicInfoAsync(userId)` (返回 DTO)
- `ValidateAndFetchCreationContextAsync` 返回类型从 `(Patient, User)` 改为 `(PatientBasicDto, UserBasicDto)` 或提取所需字段
- 调用此 Helper 的地方需同步调整字段访问

**Step 4: Update MedicalCase tests**

- Modify: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/`
- Replace mocked `IPatientRepository` / `IUserRepository` with ISP 接口

**Step 5: Verify compilation**

Run: `dotnet build src/Server/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj`
Expected: BUILD SUCCEEDED

---

### Task 8: Remove Server ProjectReferences

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Sync/LYBT.Module.Sync.csproj`
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj`

**Step 1: Remove from Sync.csproj (lines 23-26)**

```diff
-   <!-- 引用其他模块以访问 Service 接口进行引用检查 -->
-   <ProjectReference Include="..\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
-   <ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
-   <ProjectReference Include="..\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />
```

**Step 2: Remove from MedicalCase.csproj (lines 17-18)**

```diff
-   <ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
-   <ProjectReference Include="..\LYBT.Module.Users\LYBT.Module.Users.csproj" />
```

**Step 3: Verify full build + tests**

Run: `dotnet build LYBTZYZS.sln && dotnet test tests/LYBT.Tests.Unit/ && dotnet test tests/LYBT.Tests.Server.Integration/`
Expected: BUILD SUCCEEDED, all tests pass

**Step 4: Commit Phase 2**

```
git add src/Server/Modules/LYBT.Module.Sync/
git add src/Server/Modules/LYBT.Module.MedicalCase/
git add tests/UnitTests/Server/Modules/LYBT.Module.Sync.Tests/
git add tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/
git commit -m "refactor: Phase 2 - Server modules decoupled, 5 ProjectReferences removed (D5-1/D5-2)"
```

---

## Phase 3: Desktop Module Migration

### Task 9: Create Desktop Provider Implementations

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbSearchProvider.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaSearchProvider.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/FormulaModule.cs`

**Step 1: Create HerbSearchProvider**

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbSearchProvider.cs
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 药材搜索提供者实现 (D5-3)
/// 委托给 IHerbRepository，供跨模块使用
/// </summary>
public class HerbSearchProvider : IHerbSearchProvider
{
    private readonly IHerbRepository _herbRepository;

    public HerbSearchProvider(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword)
    {
        var results = await _herbRepository.SearchAsync(keyword);
        return results;
    }
}
```

**Step 2: Create FormulaSearchProvider**

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaSearchProvider.cs
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方搜索提供者实现 (D5-3)
/// 委托给 IFormulaRepository，供跨模块使用
/// </summary>
public class FormulaSearchProvider : IFormulaSearchProvider
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaSearchProvider(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize)
    {
        return await _formulaRepository.GetPagedAsync(page, pageSize);
    }

    public async Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id)
    {
        return await _formulaRepository.GetByIdAsync(id);
    }
}
```

注意: 确认 `GetPagedAsync` 和 `GetByIdAsync` 的返回类型与 Provider 接口匹配。如不匹配需适配。

**Step 3: Register providers in Module classes**

HerbsModule.cs 添加:
```csharp
using LYBT.Desktop.Contracts.Services.CrossModule;
// ...
containerRegistry.Register<IHerbSearchProvider, Services.HerbSearchProvider>();
```

FormulaModule.cs 添加:
```csharp
using LYBT.Desktop.Contracts.Services.CrossModule;
// ...
containerRegistry.Register<IFormulaSearchProvider, Services.FormulaSearchProvider>();
```

**Step 4: Verify compilation**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ && dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Formula/`
Expected: BUILD SUCCEEDED

---

### Task 10: Migrate Desktop MedicalCase ViewModels

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialogViewModel.cs`

**Step 1: Migrate MedicalCaseMasterDetailViewModel**

```diff
- using LYBT.Desktop.Herbs.Interfaces;
+ using LYBT.Desktop.Contracts.Services.CrossModule;

- private readonly IHerbRepository _herbRepository;
+ private readonly IHerbSearchProvider _herbSearchProvider;

  // Constructor:
- IHerbRepository herbRepository)
+ IHerbSearchProvider herbSearchProvider)

- _herbRepository = herbRepository;
+ _herbSearchProvider = herbSearchProvider;

  // LoadHerbsAsync method (~line 266):
- var herbs = await _herbRepository.SearchAsync(string.Empty);
+ var herbs = await _herbSearchProvider.SearchHerbsAsync(string.Empty);
```

**Step 2: Migrate FormulaImportDialogViewModel**

```diff
- using LYBT.Desktop.Formula.Interfaces;
+ using LYBT.Desktop.Contracts.Services.CrossModule;

- private readonly IFormulaRepository _formulaRepository;
+ private readonly IFormulaSearchProvider _formulaSearchProvider;

  // Constructor:
- IFormulaRepository formulaRepository)
+ IFormulaSearchProvider formulaSearchProvider)

  // GetPagedAsync call (~line 200):
- var result = await _formulaRepository.GetPagedAsync(1, SystemConstants.DefaultPageSize);
+ var result = await _formulaSearchProvider.GetFormulasPagedAsync(1, SystemConstants.DefaultPageSize);

  // GetByIdAsync call (~line 298):
- var detail = await _formulaRepository.GetByIdAsync(SelectedFormula.Id);
+ var detail = await _formulaSearchProvider.GetFormulaByIdAsync(SelectedFormula.Id);
```

**Step 3: Update Desktop MedicalCase tests**

- Modify: `tests/LYBT.Tests.Desktop.Unit/` -- MedicalCase 相关测试
- Replace mocked `IHerbRepository` / `IFormulaRepository` with `IHerbSearchProvider` / `IFormulaSearchProvider`

**Step 4: Verify compilation**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/`
Expected: BUILD SUCCEEDED

---

### Task 11: Remove Desktop ProjectReferences

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj`

**Step 1: Remove module references (lines 89-96)**

```diff
- <ItemGroup Label="Module Dependencies - Epic #2175">
-   <!-- Task 3.7: MedicalCase需要Herbs模块以支持药材拼音码过滤 -->
-   <ProjectReference Include="..\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
-   <!-- Task 3.8: MedicalCase需要Formula模块以支持经验方导入 -->
-   <ProjectReference Include="..\LYBT.Desktop.Formula\LYBT.Desktop.Formula.csproj" />
-   <!-- [已删除] ... -->
- </ItemGroup>
+ <!-- D5-3: 跨模块依赖已通过 IHerbSearchProvider/IFormulaSearchProvider 解耦 -->
+ <!-- Prism [ModuleDependency] 保证运行时加载顺序，无需编译时 ProjectReference -->
```

**Step 2: Update MedicalCaseModule.cs ModuleDependency**

确认 `[ModuleDependency("PatientsModule")]` 保留 (运行时仍依赖患者模块加载)。
考虑添加 `[ModuleDependency("HerbsModule")]` 和 `[ModuleDependency("FormulaModule")]` 确保 Provider 注册先于 MedicalCase 解析。

**Step 3: Verify full Desktop build + tests**

Run: `dotnet build LYBTZYZS.sln && dotnet test tests/LYBT.Tests.Desktop.Unit/`
Expected: BUILD SUCCEEDED, all tests pass

**Step 4: Commit Phase 3**

```
git add src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbSearchProvider.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaSearchProvider.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.Formula/FormulaModule.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/
git commit -m "refactor: Phase 3 - Desktop MedicalCase decoupled, 2 ProjectReferences removed (D5-3)"
```

---

## Phase 4: Cleanup

### Task 12: Merge Architecture Tests

**Files:**
- Read: `tests/Architecture/ArchTests.cs`, `DesktopLayerArchTests.cs`, `CustomControlArchTests.cs`
- Copy to: `tests/LYBT.Tests.Architecture/`
- Delete: `tests/Architecture/` directory

**Step 1: Read existing test classes**

确认 3 个测试文件内容和命名空间，评估是否有命名冲突。

**Step 2: Copy test classes to new project**

将 `ArchTests.cs`、`DesktopLayerArchTests.cs`、`CustomControlArchTests.cs` 复制到 `tests/LYBT.Tests.Architecture/`。
更新命名空间为 `LYBT.Tests.Architecture`。
确认 `.csproj` 包含所需的 ProjectReference (与旧 ArchTests 项目一致)。

**Step 3: Verify tests pass**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 所有迁移测试通过

**Step 4: Remove old project**

从 `LYBTZYZS.sln` 中移除 `tests/Architecture/LYBT.ArchTests.csproj` 引用。
删除 `tests/Architecture/` 目录。

---

### Task 13: Delete Empty Shell Directories

**Files:**
- Delete: `src/Server/Modules/LYBT.Module.Consultation/`
- Delete: `src/Server/Modules/LYBT.Module.Prescriptions/`

**Step 1: Verify no references**

```bash
grep -r "Consultation" LYBTZYZS.sln
grep -r "Prescriptions" LYBTZYZS.sln
```

Expected: 无 .csproj 引用 (仅注释中可能提及)

**Step 2: Delete directories**

```bash
rm -rf src/Server/Modules/LYBT.Module.Consultation/
rm -rf src/Server/Modules/LYBT.Module.Prescriptions/
```

**Step 3: Final verification**

Run: `dotnet build LYBTZYZS.sln && dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`
Expected: BUILD SUCCEEDED, ALL TESTS PASS

**Step 4: Commit Phase 4**

```
git add -A tests/Architecture/ tests/LYBT.Tests.Architecture/
git add -A src/Server/Modules/LYBT.Module.Consultation/ src/Server/Modules/LYBT.Module.Prescriptions/
git commit -m "refactor: Phase 4 - ArchTests merged, empty shell directories cleaned"
```

---

## Verification Checklist

- [ ] `dotnet build LYBTZYZS.sln` -- zero errors
- [ ] `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"` -- all pass
- [ ] Sync.csproj: 0 cross-module ProjectReference
- [ ] MedicalCase Server .csproj: 0 cross-module ProjectReference
- [ ] MedicalCase Desktop .csproj: 0 cross-module ProjectReference
- [ ] `grep -r "LYBT.Module.Herbs" src/Server/Modules/LYBT.Module.Sync/` -- no matches
- [ ] `grep -r "LYBT.Module.Patients" src/Server/Modules/LYBT.Module.MedicalCase/` -- no matches
- [ ] `grep -r "LYBT.Desktop.Herbs" src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/` -- no matches (except comments)
- [ ] Architecture test count unchanged after merge
- [ ] Empty directories removed

## Change Log

| Date | Version | Change |
|------|---------|--------|
| 2026-02-23 | v1.0 | Initial plan |
