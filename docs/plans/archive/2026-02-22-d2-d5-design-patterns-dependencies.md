# D2+D5 设计模式一致性与跨模块依赖优化设计

**版本**: v1.0 | **日期**: 2026-02-22 | **前置**: 架构分析报告 + design-deepening-phase3.md 4.5 节

---

## 概述

本文档对 D2 (设计模式一致性, 8.0) 和 D5 (跨模块依赖, 8.2) 进行深入设计，目标将两个维度提升到 9.0+。

**设计范围**:
- D2-1: Server 端 Service 基类继承统一
- D2-2: SyncService 返回类型统一 (ServiceResult -> Result)
- D5-1: ICrossModuleService ISP 拆分 (1 接口 -> 3 接口)
- D5-2: Sync 模块编译期依赖解耦
- D5-3: Desktop MedicalCase 跨模块引用解耦

---

## D2-1: Service 基类继承统一

### 现状

```
BaseService (非泛型: 权限验证 + 用户信息提取)
  └── BaseService<T> (泛型: ExecuteAsync + ValidateAsync)
        ├── HerbService : BaseService<Herb>           // 继承但未使用 ExecuteAsync
        ├── PatientService : BaseService<Patient>     // 继承但未使用 ExecuteAsync
        ├── MedicalCase*Service (3 个)                // 继承
        ├── FormulaService : IFormulaService          // 未继承
        ├── FormulaImportExportService                // 未继承
        ├── AuthService : IAuthService                // 未继承
        └── SyncService : ISyncService                // 未继承
```

**问题**: 6 个 Service 中 3 个未继承 BaseService，模式不统一。

### 目标

**所有 Service 统一继承 BaseService 体系**:
- CRUD Service (操作单一聚合根) -> `BaseService<T>`
- 跨领域 Service (Auth/Sync 等) -> `BaseService` (非泛型)

### 设计

#### 分类规则

| 分类 | 基类 | 适用 Service | 获得能力 |
|------|------|-------------|----------|
| CRUD Service | `BaseService<T>` | Herb, Patient, Formula, MedicalCase* | ExecuteAsync + ValidateAsync + 权限验证 |
| 跨领域 Service | `BaseService` | Auth, Sync | 权限验证 + 用户信息提取 + Logger |

**分类判定**: 是否围绕单一聚合根进行 CRUD 操作。Auth 管理认证流程 (非 CRUD)，Sync 协调多模块 (非单一聚合根)。

#### 变更清单

**FormulaService** (CRUD，自然适配):

```csharp
// Before
public class FormulaService : IFormulaService
{
    private readonly ILogger<FormulaService> _logger;
    public FormulaService(
        IFormulaRepository repository,
        ICrossModuleService crossModuleQuery,      // D5-1 后改为 IHerbCrossModuleService
        ILogger<FormulaService> logger)
    { _logger = logger; }
}

// After
public class FormulaService : BaseService<Formula>, IFormulaService
{
    public FormulaService(
        IFormulaRepository repository,
        IHerbCrossModuleService herbCrossModule,   // D5-1 拆分后
        ILogger<FormulaService> logger)
        : base(logger)
    { }
    // _logger 由基类提供，无需私有字段
}
```

**FormulaImportExportService** (CRUD 辅助，同一聚合根):

```csharp
// Before
public class FormulaImportExportService : IFormulaImportExportService
{
    private readonly ILogger<FormulaImportExportService> _logger;
    // ...
}

// After
public class FormulaImportExportService : BaseService<Formula>, IFormulaImportExportService
{
    public FormulaImportExportService(
        IFormulaRepository repository,
        IHerbCrossModuleService herbCrossModule,
        ILogger<FormulaImportExportService> logger)
        : base(logger)
    { }
}
```

**AuthService** (跨领域，非泛型基类):

```csharp
// Before
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    // 7 个依赖注入
}

// After
public class AuthService : BaseService, IAuthService
{
    // 依赖注入不变，仅增加 : base(logger)
    public AuthService(
        IJwtService jwtService,
        IUserCrossModuleService userCrossModule,   // D5-1 拆分后
        ILogger<AuthService> logger,
        AppDbContext dbContext,
        IConfiguration configuration,
        ITokenRevocationService revocationService,
        ISecurityAuditService auditService)
        : base(logger)
    { }
    // 获得 ExtractUserInfoAsync、权限验证方法
    // Auth 有自己的用户验证逻辑，可选择性使用基类方法
}
```

**SyncService** (跨领域，非泛型基类):

```csharp
// Before
public class SyncService : ISyncService
{
    private readonly ILogger<SyncService> _logger;
    private readonly IHerbService _herbService;       // D5-2 解耦后移除
    private readonly IPatientService _patientService; // D5-2 解耦后移除
}

// After
public class SyncService : BaseService, ISyncService
{
    public SyncService(
        AppDbContext dbContext,
        IHerbCrossModuleService herbCrossModule,       // D5 替换
        IPatientCrossModuleService patientCrossModule, // D5 替换
        ILogger<SyncService> logger)
        : base(logger)
    { }
}
```

#### 影响范围

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `FormulaService.cs` | 继承链 + 删除 _logger 字段 | 改为 BaseService<Formula> |
| `FormulaImportExportService.cs` | 继承链 + 删除 _logger 字段 | 改为 BaseService<Formula> |
| `AuthService.cs` | 继承链 + 删除 _logger 字段 | 改为 BaseService |
| `SyncService.cs` | 继承链 + 删除 _logger 字段 | 改为 BaseService |

**测试**: 现有测试无需修改 (仅增加基类构造函数调用，行为不变)。

---

## D2-2: SyncService 返回类型统一

### 现状

```
CRUD Service          -> Result<T>          (LYBT.Shared.Models.Common)
SyncService           -> ServiceResult<T>   (LYBT.Shared.Models.Contracts.Common)
```

两种类型功能重叠但 API 不同:

| 特性 | Result<T> | ServiceResult<T> |
|------|-----------|-------------------|
| IsSuccess | `bool` (属性) | `bool` (属性) |
| 错误信息 | `string? ErrorMessage` (多种) | `string? ErrorMessage` |
| 错误码 | `ErrorCode?` 支持 | 不支持 |
| 异常引用 | 不支持 | `Exception?` 属性 |
| 多错误 | `List<string> Errors` | 不支持 |
| 工厂方法 | `Success(data)` / `Failure(msg)` | `Success(data)` / `Failure(msg)` |

### 设计

**统一到 `Result<T>`**，理由:
1. Result<T> 功能更丰富 (错误码 + 多错误)
2. Result<T> 是项目标准 (5/6 Service 使用)
3. 与 D4 错误处理体系 (S3-X1/X4/A3) 的 ErrorCode 注册对齐

**变更**:

1. **ISyncService 接口**: 所有方法返回类型 `ServiceResult<T>` -> `Result<T>`
2. **SyncService 实现**: 所有 `ServiceResult<T>.Success/Failure` -> `Result<T>.Success/Failure`
3. **SyncController**: 适配新返回类型 (Result<T> 已有 IsSuccess/Data 属性，Controller 层无需大改)
4. **Desktop SyncViewModel**: 适配 (如果有直接消费 ServiceResult 的地方)

**不删除 ServiceResult<T>**: 可能有其他消费者，仅 SyncService 不再使用。后续 S5 代码卫生阶段统一清理。

**影响范围**:

| 文件 | 变更类型 |
|------|----------|
| `ISyncService.cs` | 返回类型替换 |
| `SyncService.cs` | 工厂方法替换 (~20 处) |
| `SyncController.cs` | 适配新返回类型 |
| Desktop `SyncViewModel` (如有) | 适配 |

---

## D5-1: ICrossModuleService ISP 拆分

### 现状

```csharp
// 1 个接口，3 个域，7 个方法
public interface ICrossModuleService
{
    // 患者域 (2)
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    // 药材域 (2)
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);

    // 用户域 (3)
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId);
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username);
    Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash);
}
```

**ISP 违反**: Formula 模块只需药材查询，却依赖含患者和用户方法的接口。

### 设计

拆分为 3 个领域专用接口，每个模块仅依赖所需领域:

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IPatientCrossModuleService.cs
namespace LYBT.Infrastructure.Services;

/// <summary>
/// 患者跨模块查询服务
/// 供 MedicalCase / Sync 模块使用
/// </summary>
public interface IPatientCrossModuleService
{
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    // D5-2: 新增，供 Sync 模块使用 (替代 IPatientService.CheckReferenceAsync)
    Task<ReferenceCheckDto> CheckPatientReferenceAsync(Guid patientId);
}
```

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IHerbCrossModuleService.cs
namespace LYBT.Infrastructure.Services;

/// <summary>
/// 药材跨模块查询服务
/// 供 Formula / MedicalCase / Sync 模块使用
/// </summary>
public interface IHerbCrossModuleService
{
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);

    // D5-2: 新增，供 Sync 模块使用 (替代 IHerbService.CheckReferenceAsync)
    Task<ReferenceCheckDto> CheckHerbReferenceAsync(Guid herbId);
}
```

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/IUserCrossModuleService.cs
namespace LYBT.Infrastructure.Services;

/// <summary>
/// 用户跨模块查询服务
/// 供 Auth / MedicalCase 模块使用
/// </summary>
public interface IUserCrossModuleService
{
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId);
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username);
    Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash);
}
```

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/ReferenceCheckDto.cs
namespace LYBT.Infrastructure.Services;

/// <summary>
/// 引用检查结果 DTO (供跨模块引用检查使用)
/// </summary>
public record ReferenceCheckDto(
    bool HasReferences,
    int ReferenceCount,
    string? ReferenceSummary);
```

### 实现策略

**单一实现类** (内聚优先):

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModule/CrossModuleService.cs
public class CrossModuleService
    : IPatientCrossModuleService,
      IHerbCrossModuleService,
      IUserCrossModuleService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CrossModuleService> _logger;

    // 一个实现类注册为三个接口
    // 现有方法不变，仅新增 CheckReferenceAsync 方法
}
```

**DI 注册**:

```csharp
// DatabaseServiceCollectionExtensions.cs
// Before:
services.AddScoped<ICrossModuleService, CrossModuleService>();

// After:
services.AddScoped<CrossModuleService>();                            // 单例注册
services.AddScoped<IPatientCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<IHerbCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
services.AddScoped<IUserCrossModuleService>(sp => sp.GetRequiredService<CrossModuleService>());
```

### 旧接口迁移

| 消费者 | 旧注入 | 新注入 |
|--------|--------|--------|
| FormulaService | `ICrossModuleService` | `IHerbCrossModuleService` |
| FormulaImportExportService | `ICrossModuleService` | `IHerbCrossModuleService` |
| AuthService | `ICrossModuleService` | `IUserCrossModuleService` |
| MedicalCaseQueryService | `ICrossModuleService` | `IPatientCrossModuleService` + `IUserCrossModuleService` |
| MedicalCaseCommandService | `ICrossModuleService` | `IPatientCrossModuleService` |
| SyncService | `IHerbService` + `IPatientService` | `IHerbCrossModuleService` + `IPatientCrossModuleService` |

**旧接口 `ICrossModuleService` 处理**:
- 保留为聚合接口 (继承三个子接口)，标记 `[Obsolete]`
- 待所有消费者迁移完成后 (S5) 删除

```csharp
[Obsolete("使用 IPatientCrossModuleService / IHerbCrossModuleService / IUserCrossModuleService")]
public interface ICrossModuleService
    : IPatientCrossModuleService,
      IHerbCrossModuleService,
      IUserCrossModuleService
{ }
```

### 文件变更

| 操作 | 文件 |
|------|------|
| 新建 | `Infrastructure/Services/CrossModule/IPatientCrossModuleService.cs` |
| 新建 | `Infrastructure/Services/CrossModule/IHerbCrossModuleService.cs` |
| 新建 | `Infrastructure/Services/CrossModule/IUserCrossModuleService.cs` |
| 新建 | `Infrastructure/Services/CrossModule/ReferenceCheckDto.cs` |
| 修改 | `CrossModuleService.cs` -> 移入 CrossModule/ 目录 + 实现新接口 |
| 修改 | `ICrossModuleQueryService.cs` -> 标记 Obsolete + 继承三个子接口 |
| 修改 | `DatabaseServiceCollectionExtensions.cs` -> 更新 DI 注册 |
| 修改 | 6 个消费者 Service -> 更新注入类型 |

---

## D5-2: Sync 模块编译期依赖解耦

### 现状

```xml
<!-- LYBT.Module.Sync.csproj -->
<ProjectReference Include="..\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
<ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
<ProjectReference Include="..\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />
```

**耦合点分析**:

| 耦合点 | 使用方式 | 替代方案 |
|--------|----------|----------|
| `IHerbService.CheckReferenceAsync()` | 删除前检查引用 | **D5-1**: `IHerbCrossModuleService.CheckHerbReferenceAsync()` |
| `IPatientService.CheckReferenceAsync()` | 删除前检查引用 | **D5-1**: `IPatientCrossModuleService.CheckPatientReferenceAsync()` |
| `Herb` / `Patient` / `Formula` 实体类型 | JSON 反序列化 + DbContext 操作 | **无需改**: 来自 `LYBT.Entities` (已通过 Infrastructure 引用) |

### 设计

**核心操作**: CheckReference 方法迁移到跨模块接口 (D5-1 已设计)，然后移除 3 个 ProjectReference。

**SyncService 构造函数变更**:

```csharp
// Before
public SyncService(
    AppDbContext dbContext,
    IHerbService herbService,         // LYBT.Module.Herbs 引用
    IPatientService patientService,   // LYBT.Module.Patients 引用
    ILogger<SyncService> logger)

// After
public SyncService(
    AppDbContext dbContext,
    IHerbCrossModuleService herbCrossModule,         // LYBT.Infrastructure 引用 (已有)
    IPatientCrossModuleService patientCrossModule,   // LYBT.Infrastructure 引用 (已有)
    ILogger<SyncService> logger)
    : base(logger)   // D2-1 基类继承
```

**SyncService 方法变更**:

```csharp
// Before
private async Task<(bool canDelete, string? reason)> CanDeleteHerbAsync(Guid herbId)
{
    var result = await _herbService.CheckReferenceAsync(herbId);
    if (!result.IsSuccess || result.Data == null)
        return (false, "无法检查引用关系");
    if (result.Data.HasReferences)
        return (false, $"药材被 {result.Data.ReferenceCount} 个处方引用");
    return (true, null);
}

// After
private async Task<(bool canDelete, string? reason)> CanDeleteHerbAsync(Guid herbId)
{
    var refCheck = await _herbCrossModule.CheckHerbReferenceAsync(herbId);
    if (refCheck.HasReferences)
        return (false, $"药材被 {refCheck.ReferenceCount} 个处方引用");
    return (true, null);
}
```

**CheckReferenceAsync 实现** (在 CrossModuleService 中):

```csharp
// CrossModuleService.cs 新增方法
public async Task<ReferenceCheckDto> CheckHerbReferenceAsync(Guid herbId)
{
    // 检查 FormulaHerb 引用
    var refCount = await _dbContext.Set<FormulaHerb>()
        .CountAsync(fh => fh.HerbId == herbId);

    // 检查 PrescriptionItem 引用
    var prescriptionRefCount = await _dbContext.Set<PrescriptionItem>()
        .CountAsync(pi => pi.HerbId == herbId);

    var total = refCount + prescriptionRefCount;
    return new ReferenceCheckDto(
        HasReferences: total > 0,
        ReferenceCount: total,
        ReferenceSummary: total > 0 ? $"被 {refCount} 个验方和 {prescriptionRefCount} 个处方引用" : null);
}

public async Task<ReferenceCheckDto> CheckPatientReferenceAsync(Guid patientId)
{
    var refCount = await _dbContext.MedicalCases
        .CountAsync(mc => mc.PatientId == patientId && !mc.IsDeleted);

    return new ReferenceCheckDto(
        HasReferences: refCount > 0,
        ReferenceCount: refCount,
        ReferenceSummary: refCount > 0 ? $"关联 {refCount} 个医案" : null);
}
```

**csproj 变更**:

```xml
<!-- LYBT.Module.Sync.csproj -->
<!-- 删除以下 3 行 -->
<!-- <ProjectReference Include="..\LYBT.Module.Herbs\..." /> -->
<!-- <ProjectReference Include="..\LYBT.Module.Patients\..." /> -->
<!-- <ProjectReference Include="..\LYBT.Module.Formula\..." /> -->

<!-- 保留 (已有通过 Infrastructure 间接引用 LYBT.Entities) -->
<ProjectReference Include="..\..\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
```

**验证**: Entity 类型 (Herb/Patient/Formula) 在 `LYBT.Entities` 中定义，通过 Infrastructure 的传递引用可达，无需直接引用业务模块。

---

## D5-3: Desktop MedicalCase 跨模块引用解耦

### 现状

```xml
<!-- LYBT.Desktop.MedicalCase.csproj -->
<ProjectReference Include="..\LYBT.Desktop.Herbs\..." />     <!-- 自动补全 -->
<ProjectReference Include="..\LYBT.Desktop.Formula\..." />   <!-- 经验方导入 -->
```

**耦合点详情**:

| 耦合点 | 位置 | 用途 |
|--------|------|------|
| `IHerbRepository` | MedicalCaseMasterDetailViewModel | 药材自动补全列表 |
| `IFormulaRepository` | FormulaImportDialogViewModel | 经验方搜索和导入 |
| `Formula.Controls` XAML 命名空间 | FormulaImportDialog.xaml | 直接引用 Formula 模块的 UI 控件 |

### 设计

**策略**: 在 `LYBT.Desktop.Contracts` 中定义最小接口 (ISP)，模块实现接口，MedicalCase 仅依赖 Contracts。

#### 新建接口

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IHerbSearchProvider.cs
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 药材搜索提供者 (供跨模块使用)
/// 实现: LYBT.Desktop.Herbs.Services.HerbSearchProvider
/// </summary>
public interface IHerbSearchProvider
{
    /// <summary>搜索药材 (自动补全用)</summary>
    Task<List<HerbListDto>> SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>获取全部启用药材 (处方编辑用)</summary>
    Task<List<HerbListDto>> GetAllActiveAsync(CancellationToken ct = default);
}
```

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IFormulaSearchProvider.cs
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 验方搜索提供者 (供跨模块使用)
/// 实现: LYBT.Desktop.Formula.Services.FormulaSearchProvider
/// </summary>
public interface IFormulaSearchProvider
{
    /// <summary>搜索验方列表</summary>
    Task<List<FormulaListDto>> SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>获取验方详情 (含药材组成)</summary>
    Task<FormulaDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>克隆验方 (导入到处方用)</summary>
    Task<FormulaDetailDto?> CloneFormulaAsync(Guid formulaId, CancellationToken ct = default);
}
```

#### 模块实现

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbSearchProvider.cs
namespace LYBT.Desktop.Herbs.Services;

public class HerbSearchProvider : IHerbSearchProvider
{
    private readonly IHerbRepository _repository;

    public HerbSearchProvider(IHerbRepository repository)
        => _repository = repository;

    public Task<List<HerbListDto>> SearchAsync(string keyword, CancellationToken ct)
        => _repository.SearchAsync(keyword);

    public Task<List<HerbListDto>> GetAllActiveAsync(CancellationToken ct)
        => _repository.GetAllActiveAsync();
}
```

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaSearchProvider.cs
namespace LYBT.Desktop.Formula.Services;

public class FormulaSearchProvider : IFormulaSearchProvider
{
    private readonly IFormulaRepository _repository;

    public FormulaSearchProvider(IFormulaRepository repository)
        => _repository = repository;

    public Task<List<FormulaListDto>> SearchAsync(string keyword, CancellationToken ct)
        => _repository.SearchAsync(keyword);

    public Task<FormulaDetailDto?> GetByIdAsync(Guid id, CancellationToken ct)
        => _repository.GetByIdAsync(id);

    public Task<FormulaDetailDto?> CloneFormulaAsync(Guid formulaId, CancellationToken ct)
        => _repository.CloneFormulaAsync(formulaId);
}
```

#### DI 注册

```csharp
// Herbs Module DI (HerbsModule.cs 或 ServiceCollectionExtensions.cs)
services.AddSingleton<IHerbSearchProvider, HerbSearchProvider>();

// Formula Module DI
services.AddSingleton<IFormulaSearchProvider, FormulaSearchProvider>();
```

#### MedicalCase ViewModel 变更

```csharp
// Before
public MedicalCaseMasterDetailViewModel(
    ...,
    IHerbRepository herbRepository)   // 直接依赖 Herbs 模块

// After
public MedicalCaseMasterDetailViewModel(
    ...,
    IHerbSearchProvider herbSearchProvider)   // 依赖 Contracts 接口
```

```csharp
// Before
public FormulaImportDialogViewModel(
    ...,
    IFormulaRepository formulaRepository)   // 直接依赖 Formula 模块

// After
public FormulaImportDialogViewModel(
    ...,
    IFormulaSearchProvider formulaSearchProvider)   // 依赖 Contracts 接口
```

#### XAML 控件依赖处理

**FormulaImportDialog.xaml** 当前引用 `LYBT.Desktop.Formula.Controls` 命名空间。

**方案**: 将 FormulaImportDialog 使用的控件迁移到 `LYBT.Desktop.Infrastructure`（共享 UI 控件层），或重构为通用列表控件 + 数据绑定。

具体取决于控件复杂度:
- 如果是简单列表控件 -> 迁移到 Infrastructure
- 如果是复杂业务控件 -> 保留 XAML 引用，仅解耦 ViewModel 层

**建议**: XAML 控件解耦为后续 Phase，当前优先解耦 ViewModel 层依赖。

#### csproj 变更

```xml
<!-- LYBT.Desktop.MedicalCase.csproj -->
<!-- 删除 (ViewModel 层不再需要) -->
<!-- <ProjectReference Include="..\LYBT.Desktop.Herbs\..." /> -->

<!-- Formula 引用: 如果 XAML 控件已迁移则删除，否则保留 -->
<!-- <ProjectReference Include="..\LYBT.Desktop.Formula\..." /> -->

<!-- 保留 (Contracts 已是间接依赖) -->
<ProjectReference Include="..\..\Core\LYBT.Desktop.Contracts\..." />
```

**注意**: 如果 XAML 仍引用 Formula.Controls，则 MedicalCase.csproj 的 Formula ProjectReference 暂时保留。ViewModel 层解耦先行，XAML 层解耦跟进。

---

## 依赖关系对比

### Server 端 Before

```
Sync ──→ Herbs Module (ProjectReference)
Sync ──→ Patients Module (ProjectReference)
Sync ──→ Formula Module (ProjectReference)
Formula ──→ ICrossModuleService (单一大接口)
Auth ──→ ICrossModuleService (单一大接口)
MedicalCase ──→ ICrossModuleService (单一大接口)
```

### Server 端 After

```
Sync ──→ IHerbCrossModuleService (Infrastructure 已有)
Sync ──→ IPatientCrossModuleService (Infrastructure 已有)
Formula ──→ IHerbCrossModuleService (最小依赖)
Auth ──→ IUserCrossModuleService (最小依赖)
MedicalCase ──→ IPatientCrossModuleService + IUserCrossModuleService
```

### Desktop 端 Before

```
MedicalCase ──→ LYBT.Desktop.Herbs (ProjectReference)
MedicalCase ──→ LYBT.Desktop.Formula (ProjectReference)
```

### Desktop 端 After

```
MedicalCase ──→ IHerbSearchProvider (Contracts)
MedicalCase ──→ IFormulaSearchProvider (Contracts)
Herbs ──→ IHerbSearchProvider 实现 (内部)
Formula ──→ IFormulaSearchProvider 实现 (内部)
```

---

## Sprint 映射

| 设计项 | Sprint | 工作包 | 预估任务数 |
|--------|--------|--------|-----------|
| D2-1 Service 基类统一 | S5 | 代码卫生 | 4 (每 Service 1 个) |
| D2-2 返回类型统一 | S5 | 代码卫生 | 3 (接口+实现+Controller) |
| D5-1 ICrossModuleService 拆分 | S3 | 标准固化 | 8 (3 接口 + 1 DTO + 实现 + DI + 6 消费者) |
| D5-2 Sync 解耦 | S3 | 标准固化 | 4 (CheckReference 实现 + Sync 适配 + csproj + 测试) |
| D5-3 Desktop 解耦 | S4/S5 | 本地补齐 | 6 (2 接口 + 2 实现 + 2 ViewModel + csproj) |

**执行顺序依赖**:

```
D5-1 ICrossModuleService 拆分
  ├──→ D5-2 Sync 解耦 (依赖新接口)
  └──→ D2-1 Service 基类统一 (依赖新注入类型)
         └──→ D2-2 返回类型统一 (SyncService 同时改)
D5-3 Desktop 解耦 (独立，无前置依赖)
```

---

## 验收标准

| 指标 | 当前 | 目标 |
|------|------|------|
| D2 设计模式一致性 | 8.0 | 9.0+ |
| D5 跨模块依赖 | 8.2 | 9.0+ |
| Service 基类继承覆盖率 | 60% (3/5 类型) | 100% (5/5 类型) |
| 返回类型统一度 | 83% (5/6) | 100% (6/6) |
| ICrossModuleService ISP | 1 接口 3 域 | 3 接口各 1 域 |
| Sync 模块 ProjectReference | 3 个业务模块 | 0 个业务模块 |
| Desktop MedicalCase ProjectReference | 2 个业务模块 | 0-1 个 (XAML 完成后 0) |

---

## 变更记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-02-22 | v1.0 | 初始版本，D2-1/D2-2/D5-1/D5-2/D5-3 完整设计 |
