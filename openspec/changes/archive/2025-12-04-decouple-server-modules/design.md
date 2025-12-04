# Design: decouple-server-modules

## Overview

本设计文档详细描述Server端模块解耦的技术方案，实现Server端与Desktop端设计思想统一。

**核心目标**:
- Prescriptions模块依赖从5个减少到0个
- Formula模块依赖从1个减少到0个
- 建立统一的跨模块通信规范

---

## Part 1: 设计背景与分析

### 1.1 Server-Client设计对比

| 维度 | Desktop端 (良好设计) | Server端 (当前问题) | Server端 (重构目标) |
|------|----------------------|---------------------|---------------------|
| **模块依赖声明** | Prism `[ModuleDependency]` | csproj `ProjectReference` | 仅Infrastructure/Entities/Shared |
| **跨模块数据获取** | 通过HTTP API调用 | 直接注入其他模块Repository | 通过ICrossModuleQueryService |
| **聚合根遵循** | Issue #1606已实现 | 部分遵循 | 完全遵循 |
| **模块边界** | 清晰隔离 | 被穿透 | 清晰隔离 |

#### Desktop端良好设计示例

```csharp
// PrescriptionsModule.cs - Desktop端
[ModuleDependency("ConsultationModule")] // 功能依赖：UI组件加载顺序
[ModuleDependency("HerbsModule")]        // 功能依赖：UI组件加载顺序
[ModuleDependency("FormulaModule")]      // 功能依赖：UI组件加载顺序
public class PrescriptionsModule : IModule
{
    // Issue #1606: IPrescriptionRepository已删除
    // 所有Write操作通过MedicalCaseRepository聚合根
}
```

**特点**:
- 这是**功能依赖**，仅影响模块加载顺序
- 数据通过API获取，不直接访问其他模块内部
- 遵循DDD聚合根边界

### 1.2 行业最佳实践参考

#### ABP Framework模块通信规范

| 通信方式 | ABP实现 | 本项目对应 |
|----------|---------|------------|
| **Application Contracts** | 共享DTO和接口定义 | LYBT.Shared.Models |
| **Service接口依赖** | 通过IAppService接口 | ICrossModuleQueryService |
| **禁止直接Repository** | 模块内部实现细节 | 本次重构目标 |

#### Microsoft eShopOnContainers

- 使用简化CQRS模式
- 读写操作分离
- 跨服务通信通过API或事件

### 1.3 当前模块依赖矩阵

```
依赖方 →        Auth  Users  Patients  MedicalCase  Consultation  Prescriptions  Herbs  Formula
被依赖方 ↓
Auth             -      -       -           -            -              -           -       -
Users            Y      -       -           -            -              -           -       -
Patients         -      -       -           Y            -              Y           -       -
MedicalCase      -      -       -           -            Y              Y           -       -
Consultation     -      -       -           -            -              Y           -       -
Prescriptions    -      -       -           -            -              -           -       -
Herbs            -      -       -           -            -              Y           -       Y
Formula          -      -       -           -            -              Y           -       -
```

**数据模型说明** (值对象副本模式):
- PrescriptionItem使用值对象副本(HerbId+HerbName)存储药材信息
- FormulaItem使用值对象副本(HerbId+HerbName)存储药材信息
- **但** FormulaService当前注入IHerbRepository进行药材验证和名称匹配，需要解耦

### 1.4 问题模块详细分析：Prescriptions

**当前PrescriptionService依赖**:

```csharp
public class PrescriptionService : IPrescriptionService
{
    // 本模块Repository
    private readonly IPrescriptionRepository _repository;

    // 跨模块Repository (问题所在 - 5个!)
    private readonly IMedicalCaseRepository _medicalCaseRepository; // MedicalCase模块
    private readonly IPatientRepository _patientRepository;         // Patients模块
    private readonly IConsultationRepository _consultationRepository; // Consultation模块
    // 注: Herbs和Formula通过csproj引用，但Service中未直接使用

    // 其他依赖
    private readonly IPrescriptionNumberService _numberService;
    private readonly IMapper _mapper;
    private readonly ILogger<PrescriptionService> _logger;
}
```

**LYBT.Module.Prescriptions.csproj当前引用**:
```xml
<ProjectReference Include="..\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
<ProjectReference Include="..\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />
<ProjectReference Include="..\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj" />
<ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
<ProjectReference Include="..\LYBT.Module.Consultation\LYBT.Module.Consultation.csproj" />
```

**使用场景分析**:

| Repository | 使用方法 | 用途 | 解耦方案 |
|------------|----------|------|----------|
| _medicalCaseRepository | GetAllAsync() | 搜索时关联医案 | CrossModuleQueryService |
| _patientRepository | GetAllAsync() | 搜索时关联患者 | CrossModuleQueryService |
| _consultationRepository | GetAllAsync() | 搜索时关联诊断 | 通过MedicalCaseBasicDto.TCMDiagnosis获取 |

### 1.5 问题模块详细分析：Formula

**当前FormulaService依赖**:

```csharp
public class FormulaService : IFormulaService
{
    // 本模块Repository
    private readonly IFormulaRepository _repository;

    // 跨模块Repository (问题所在)
    private readonly IHerbRepository _herbRepository; // Herbs模块

    // 其他依赖
    private readonly IMapper _mapper;
    private readonly ILogger<FormulaService> _logger;
}
```

**使用场景分析**:

| Repository | 使用方法 | 用途 | 解耦方案 |
|------------|----------|------|----------|
| _herbRepository | GetByIdAsync() | 验证药材(ValidateFormulaHerbAsync) | CrossModuleQueryService |
| _herbRepository | GetByNameOrPinyinAsync() | 匹配药材(TryMatchHerbAsync) | CrossModuleQueryService |

---

## Part 2: 解耦方案设计

### 2.1 CrossModuleQueryService接口设计

创建一个轻量级的跨模块查询服务，放在Infrastructure层，提供批量查询能力。

**设计原则**:
- **轻量封装**: 不引入框架级复杂性，仅封装跨模块查询
- **返回DTO**: 防止Entity泄露，符合Bounded Context
- **批量优先**: 提供批量查询方法，避免N+1问题
- **只读查询**: 使用AsNoTracking()优化性能
- **投影查询**: 使用Select()减少数据传输

**接口定义**:

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/ICrossModuleQueryService.cs
namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块查询服务接口
/// 提供模块间只读数据访问，避免直接跨模块注入Repository
/// </summary>
public interface ICrossModuleQueryService
{
    #region 患者查询

    /// <summary>
    /// 获取患者基本信息
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>患者基本信息DTO，不存在或已删除返回null</returns>
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);

    /// <summary>
    /// 批量获取患者基本信息
    /// </summary>
    /// <param name="patientIds">患者ID集合</param>
    /// <returns>患者ID到基本信息的字典，不包含已删除的患者</returns>
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    #endregion

    #region 医案查询

    /// <summary>
    /// 获取医案基本信息（包含患者ID、诊断摘要）
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>医案基本信息DTO，包含关联的TCMDiagnosis</returns>
    Task<MedicalCaseBasicDto?> GetMedicalCaseBasicInfoAsync(Guid medicalCaseId);

    /// <summary>
    /// 批量获取医案基本信息
    /// </summary>
    /// <param name="medicalCaseIds">医案ID集合</param>
    /// <returns>医案ID到基本信息的字典，包含关联的TCMDiagnosis</returns>
    Task<Dictionary<Guid, MedicalCaseBasicDto>> GetMedicalCasesBasicInfoAsync(IEnumerable<Guid> medicalCaseIds);

    #endregion

    #region 药材查询

    /// <summary>
    /// 获取药材基本信息 (供Formula模块使用)
    /// </summary>
    /// <param name="herbId">药材ID</param>
    /// <returns>药材基本信息DTO，不存在或已删除返回null</returns>
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);

    /// <summary>
    /// 按名称或拼音查询药材 (供Formula模块使用)
    /// </summary>
    /// <param name="nameOrPinyin">药材名称或拼音</param>
    /// <returns>匹配的药材基本信息DTO，无匹配返回null</returns>
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);

    #endregion
}
```

### 2.2 CrossModuleQueryService实现

```csharp
// src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs
namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块查询服务实现
/// 直接使用DbContext进行只读查询，不经过模块Service
/// </summary>
public class CrossModuleQueryService : ICrossModuleQueryService
{
    private readonly AppDbContext _context;

    public CrossModuleQueryService(AppDbContext context)
    {
        _context = context;
    }

    #region 患者查询

    public async Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId)
    {
        return await _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId && !p.IsDeleted)
            .Select(p => new PatientBasicDto
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.Phone
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(
        IEnumerable<Guid> patientIds)
    {
        var ids = patientIds.ToList();
        if (!ids.Any()) return new Dictionary<Guid, PatientBasicDto>();

        var patients = await _context.Patients
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new PatientBasicDto
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.Phone
            })
            .ToListAsync();

        return patients.ToDictionary(p => p.Id);
    }

    #endregion

    #region 医案查询

    public async Task<MedicalCaseBasicDto?> GetMedicalCaseBasicInfoAsync(Guid medicalCaseId)
    {
        return await _context.MedicalCases
            .AsNoTracking()
            .Where(mc => mc.Id == medicalCaseId && !mc.IsDeleted)
            .Select(mc => new MedicalCaseBasicDto
            {
                Id = mc.Id,
                PatientId = mc.PatientId,
                Status = mc.Status,
                CreatedAt = mc.CreatedAt,
                // 关联诊断信息 - 使用子查询
                TCMDiagnosis = _context.Consultations
                    .Where(c => c.Id == mc.Id)
                    .Select(c => c.TCMDiagnosis)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, MedicalCaseBasicDto>> GetMedicalCasesBasicInfoAsync(
        IEnumerable<Guid> medicalCaseIds)
    {
        var ids = medicalCaseIds.ToList();
        if (!ids.Any()) return new Dictionary<Guid, MedicalCaseBasicDto>();

        // 批量查询医案 - 分两步避免复杂Join
        var medicalCases = await _context.MedicalCases
            .AsNoTracking()
            .Where(mc => ids.Contains(mc.Id) && !mc.IsDeleted)
            .Select(mc => new MedicalCaseBasicDto
            {
                Id = mc.Id,
                PatientId = mc.PatientId,
                Status = mc.Status,
                CreatedAt = mc.CreatedAt
            })
            .ToListAsync();

        // 批量查询关联诊断 - 第二次数据库查询
        var consultations = await _context.Consultations
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.TCMDiagnosis })
            .ToDictionaryAsync(c => c.Id, c => c.TCMDiagnosis);

        // 合并诊断信息 - 内存操作
        foreach (var mc in medicalCases)
        {
            if (consultations.TryGetValue(mc.Id, out var diagnosis))
            {
                mc.TCMDiagnosis = diagnosis;
            }
        }

        return medicalCases.ToDictionary(mc => mc.Id);
    }

    #endregion

    #region 药材查询

    public async Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId)
    {
        return await _context.Herbs
            .AsNoTracking()
            .Where(h => h.Id == herbId && !h.IsDeleted)
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.Pinyin,
                Category = h.Category
            })
            .FirstOrDefaultAsync();
    }

    public async Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin)
    {
        if (string.IsNullOrWhiteSpace(nameOrPinyin))
            return null;

        return await _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted &&
                (h.Name == nameOrPinyin || h.Pinyin == nameOrPinyin))
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.Pinyin,
                Category = h.Category
            })
            .FirstOrDefaultAsync();
    }

    #endregion
}
```

### 2.3 基础DTO定义

**位置**: `src/Shared/LYBT.Shared.Models/Contracts/Common/`

```csharp
// PatientBasicDto.cs
namespace LYBT.Shared.Models.Contracts.Common;

using LYBT.Shared.Models.Enums;

/// <summary>
/// 患者基本信息DTO - 用于跨模块查询
/// 仅包含最少必要字段，避免过度暴露
/// </summary>
public class PatientBasicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }  // 使用枚举类型，与Patient实体一致
    public string? Phone { get; set; }
}
```

```csharp
// MedicalCaseBasicDto.cs
namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 医案基本信息DTO - 用于跨模块查询
/// 包含关联的诊断信息，避免额外查询Consultation
/// </summary>
public class MedicalCaseBasicDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 中医诊断 - 来自关联的Consultation
    /// </summary>
    public string? TCMDiagnosis { get; set; }
}
```

```csharp
// HerbBasicDto.cs
namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 药材基本信息DTO - 用于跨模块查询
/// 供Formula模块验证和匹配药材使用
/// </summary>
public class HerbBasicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Pinyin { get; set; }
    public string? Category { get; set; }
}
```

---

## Part 3: 重构后的代码设计

### 3.1 重构后的PrescriptionService

```csharp
// 重构后
public class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _repository;
    private readonly ICrossModuleQueryService _crossModuleQuery; // 替代5个跨模块依赖
    private readonly IPrescriptionNumberService _numberService;
    private readonly IMapper _mapper;
    private readonly ILogger<PrescriptionService> _logger;

    public PrescriptionService(
        IPrescriptionRepository repository,
        ICrossModuleQueryService crossModuleQuery,
        IPrescriptionNumberService numberService,
        IMapper mapper,
        ILogger<PrescriptionService> logger)
    {
        _repository = repository;
        _crossModuleQuery = crossModuleQuery;
        _numberService = numberService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 加载关联数据 - 重构后使用批量查询
    /// </summary>
    private async Task<(Dictionary<Guid, MedicalCaseBasicDto> medicalCases,
                        Dictionary<Guid, PatientBasicDto> patients)>
        LoadRelatedDataAsync(IEnumerable<Guid> medicalCaseIds)
    {
        // 批量查询医案（包含诊断）- 避免N+1
        var medicalCases = await _crossModuleQuery.GetMedicalCasesBasicInfoAsync(medicalCaseIds);

        // 提取患者ID并批量查询 - 避免N+1
        var patientIds = medicalCases.Values.Select(mc => mc.PatientId).Distinct();
        var patients = await _crossModuleQuery.GetPatientsBasicInfoAsync(patientIds);

        return (medicalCases, patients);
    }

    /// <summary>
    /// 搜索处方 - 重构后使用CrossModuleQueryService
    /// </summary>
    public async Task<Result<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
        string? patientName = null,
        string? symptomKeyword = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
            {
                return Result<List<PrescriptionSearchResultDto>>.Success(new List<PrescriptionSearchResultDto>());
            }

            // 获取所有处方
            var prescriptions = await _repository.GetAllAsync();

            // 收集需要查询的医案ID
            var medicalCaseIds = prescriptions.Select(p => p.MedicalCaseId).Distinct().ToList();

            // 批量查询关联数据
            var (medicalCasesDict, patientsDict) = await LoadRelatedDataAsync(medicalCaseIds);

            // 内存过滤与关联
            var searchResults = new List<PrescriptionSearchResultDto>();

            foreach (var prescription in prescriptions)
            {
                if (!medicalCasesDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                    continue;

                if (!patientsDict.TryGetValue(medicalCase.PatientId, out var patient))
                    continue;

                // 按患者姓名筛选
                if (!string.IsNullOrWhiteSpace(patientName) &&
                    !patient.Name.Contains(patientName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 按症状/诊断关键字筛选 - 诊断从MedicalCaseBasicDto获取
                if (!string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    var matchedInDiagnosis = medicalCase.TCMDiagnosis?.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase) ?? false;
                    var matchedInIndication = prescription.Indication?.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase) ?? false;

                    if (!matchedInDiagnosis && !matchedInIndication)
                        continue;
                }

                searchResults.Add(new PrescriptionSearchResultDto
                {
                    Id = prescription.Id,
                    CreatedAt = prescription.CreatedAt,
                    PatientId = patient.Id,
                    PatientName = patient.Name,
                    Indication = prescription.Indication,
                    TCMDiagnosis = medicalCase.TCMDiagnosis,
                    DosageCount = prescription.DosageCount,
                    Advice = prescription.Advice,
                    FormulaSource = prescription.FormulaSource,
                    Remark = prescription.Remark
                });
            }

            return Result<List<PrescriptionSearchResultDto>>.Success(searchResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索处方失败");
            return Result<List<PrescriptionSearchResultDto>>.Failure($"搜索处方失败：{ex.Message}");
        }
    }
}
```

### 3.2 重构后的FormulaService

```csharp
// 重构后
public class FormulaService : IFormulaService
{
    private readonly IFormulaRepository _repository;
    private readonly ICrossModuleQueryService _crossModuleQuery; // 替代IHerbRepository
    private readonly IMapper _mapper;
    private readonly ILogger<FormulaService> _logger;

    public FormulaService(
        IFormulaRepository repository,
        ICrossModuleQueryService crossModuleQuery,
        IMapper mapper,
        ILogger<FormulaService> logger)
    {
        _repository = repository;
        _crossModuleQuery = crossModuleQuery;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 验证经验方药材 - 重构后使用CrossModuleQueryService
    /// </summary>
    public async Task<Result<FormulaHerbValidationResult>> ValidateFormulaHerbAsync(Guid selectedHerbId)
    {
        try
        {
            // 使用CrossModuleQueryService替代直接注入IHerbRepository
            var herb = await _crossModuleQuery.GetHerbBasicInfoAsync(selectedHerbId);

            if (herb == null)
            {
                return Result<FormulaHerbValidationResult>.Failure("指定的药材不存在");
            }

            return Result<FormulaHerbValidationResult>.Success(new FormulaHerbValidationResult
            {
                IsValid = true,
                HerbId = herb.Id,
                HerbName = herb.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证药材失败: {HerbId}", selectedHerbId);
            return Result<FormulaHerbValidationResult>.Failure($"验证药材失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 尝试匹配药材 - 重构后使用CrossModuleQueryService
    /// </summary>
    public async Task<Result<FormulaHerbMatchResult>> TryMatchHerbAsync(string herbName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(herbName))
            {
                return Result<FormulaHerbMatchResult>.Failure("药材名称不能为空");
            }

            // 使用CrossModuleQueryService替代直接注入IHerbRepository
            var herb = await _crossModuleQuery.GetHerbByNameOrPinyinAsync(herbName);

            if (herb == null)
            {
                return Result<FormulaHerbMatchResult>.Success(new FormulaHerbMatchResult
                {
                    IsMatched = false,
                    OriginalName = herbName
                });
            }

            return Result<FormulaHerbMatchResult>.Success(new FormulaHerbMatchResult
            {
                IsMatched = true,
                HerbId = herb.Id,
                HerbName = herb.Name,
                OriginalName = herbName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "匹配药材失败: {HerbName}", herbName);
            return Result<FormulaHerbMatchResult>.Failure($"匹配药材失败：{ex.Message}");
        }
    }
}
```

---

## Part 4: 模块依赖规范

### 4.1 合法依赖类型

| 类型 | 说明 | 示例 |
|------|------|------|
| **聚合内依赖** | 聚合根内部实体 | MedicalCase → Consultation, Prescription |
| **Infrastructure依赖** | 所有模块依赖Infrastructure | Module → Infrastructure |
| **Shared依赖** | 所有模块依赖Shared | Module → Shared.Models |
| **Service接口依赖** | 通过接口调用其他模块Service | Auth → IUserService |
| **CrossModule查询** | 只读跨模块数据访问 | Prescriptions/Formula → ICrossModuleQueryService |

### 4.2 禁止的依赖类型

| 类型 | 原因 | 替代方案 |
|------|------|----------|
| **跨模块Repository注入** | 违反模块边界 | 使用Service接口或CrossModuleQueryService |
| **跨模块Entity引用** | 实体属于特定模块 | 使用DTO传递数据 |
| **循环依赖** | 编译问题、架构问题 | 提取公共依赖到Infrastructure |

### 4.3 重构后的依赖矩阵

```
依赖方 →              Auth  Users  Patients  MedicalCase  Consultation  Prescriptions  Herbs  Formula
被依赖方 ↓
Auth                   -      -       -           -            -              -           -       -
Users                  Y      -       -           -            -              -           -       -
Patients               -      -       -           Y            -              -           -       -
MedicalCase            -      -       -           -            Y              -           -       -
Consultation           -      -       -           -            -              -           -       -
Prescriptions          -      -       -           -            -              -           -       -
Herbs                  -      -       -           -            -              -           -       -
Formula                -      -       -           -            -              -           -       -
CrossModule(Infra)     -      -       -           -            -              Y           -       Y
```

**变化总结**:
- Prescriptions: 5个模块依赖 → 0个模块依赖 (通过CrossModuleQueryService)
- Formula: 1个模块依赖 → 0个模块依赖 (通过CrossModuleQueryService)
- Herbs: 保持完全独立
- 总跨模块依赖: 9个 → 4个

### 4.4 合法依赖的解耦分析

#### 问题：合法依赖是否也需要解耦？

当前设计将模块依赖分为"需要解耦"和"合法依赖"两类。这里分析为什么"合法依赖"不需要进一步解耦。

#### 当前合法依赖清单

| 依赖关系 | 当前实现方式 | 依赖类型 |
|----------|--------------|----------|
| Auth → Users | 注入IUserService接口 | Service接口依赖 |
| MedicalCase → Patients | 注入IPatientService接口 | Service接口依赖 |
| MedicalCase → Users | 注入IUserService接口 | Service接口依赖 |
| Consultation → MedicalCase | 聚合根内部关系 | DDD聚合关系 |

#### 分析：已经使用依赖注入

这些"合法依赖"**已经通过Service接口实现了依赖注入**：

```csharp
// AuthService - 当前实现
public class AuthService : IAuthService
{
    private readonly IUserService _userService;  // ✅ 接口依赖，非Repository
    // ...
}

// MedicalCaseService - 当前实现
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IPatientService _patientService;  // ✅ 接口依赖
    private readonly IUserService _userService;        // ✅ 接口依赖
    // ...
}
```

**符合依赖倒置原则**：依赖于抽象(IUserService)而非具体实现(UserService/UserRepository)。

#### 是否需要改用CrossModuleQueryService？

| 考虑因素 | Service接口依赖 | CrossModuleQueryService |
|----------|-----------------|-------------------------|
| **业务逻辑** | 可调用业务方法 | 仅数据查询 |
| **验证规则** | 可复用Service验证 | 需要重复验证逻辑 |
| **事务一致性** | 可参与事务 | 只读，不参与事务 |
| **适用场景** | 需要业务处理 | 纯数据展示 |

#### 具体场景分析

**1. Auth → Users (保持Service接口)**
```csharp
// 认证过程需要调用Users模块的业务方法
var user = await _userService.ValidateCredentialsAsync(username, password);
var permissions = await _userService.GetUserPermissionsAsync(userId);
```
- 需要密码验证、权限获取等**业务逻辑**
- 不是简单的数据查询
- **结论：保持IUserService接口依赖**

**2. MedicalCase → Patients (保持Service接口)**
```csharp
// 创建医案时需要验证患者
var patient = await _patientService.GetByIdAsync(patientId);
if (patient == null) throw new BusinessException("患者不存在");
```
- 需要验证患者是否存在、是否有效
- 可能涉及患者状态检查等业务规则
- **结论：保持IPatientService接口依赖**

**3. Consultation → MedicalCase (保持聚合关系)**
```csharp
// Consultation是MedicalCase聚合根的内部成员
public class MedicalCase : AggregateRoot
{
    public Consultation? Consultation { get; private set; }  // 聚合内导航
}
```
- DDD聚合根模式，Consultation是MedicalCase的一部分
- 一起加载、一起保存
- **结论：保持聚合内关系，不需要解耦**

#### 解耦程度分层模型

```
解耦程度
    |
高  |  CrossModuleQueryService  ← 纯只读查询，无业务逻辑
    |          ↑                   Prescriptions, Formula 使用
    |          |
中  |  Service接口依赖  ← 需要业务方法调用
    |          ↑           Auth→Users, MedicalCase→Patients 使用 ✓
    |          |
低  |  聚合内直接引用  ← DDD聚合根内部关系
    |                     Consultation→MedicalCase 使用 ✓
    |________________________________________________
```

#### 结论：当前设计合理

| 依赖类型 | 使用场景 | 本项目应用 |
|----------|----------|------------|
| **CrossModuleQueryService** | 只需要数据，无业务逻辑 | Prescriptions搜索、Formula验证 |
| **Service接口依赖** | 需要调用业务方法 | Auth认证、MedicalCase创建 |
| **聚合内关系** | DDD聚合根内部 | MedicalCase包含Consultation |

**不建议进一步解耦的原因**：
1. 这些依赖已经是接口依赖，符合DIP原则
2. Auth/MedicalCase需要调用**业务方法**，不仅仅是数据查询
3. 过度解耦会增加复杂性，需要重复业务逻辑
4. 这些关系是业务核心，不太可能变化

**未来扩展方案**（如果确实需要）：
- 可以扩展`ICrossModuleQueryService`添加更多查询方法
- 或创建专门的`IUserQueryService`/`IPatientQueryService`只读接口
- 但目前没有这个需求

---

## Part 5: DI注册

### 5.1 Infrastructure层注册

```csharp
// src/Server/Core/LYBT.Infrastructure/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // 现有注册...

        // 新增: 跨模块查询服务
        services.AddScoped<ICrossModuleQueryService, CrossModuleQueryService>();

        return services;
    }
}
```

### 5.2 Prescriptions模块注册 (修改后)

```csharp
// src/Server/Modules/LYBT.Module.Prescriptions/PrescriptionsModule.cs
public static class PrescriptionsModule
{
    public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
    {
        // Repository
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

        // Services
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionNumberService, PrescriptionNumberService>();

        // 注意: 不再需要注入其他模块的Repository
        // ICrossModuleQueryService 由 Infrastructure 层提供

        return services;
    }
}
```

---

## Part 6: 测试策略

### 6.1 CrossModuleQueryService单元测试

```csharp
public class CrossModuleQueryServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CrossModuleQueryService _service;

    public CrossModuleQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new CrossModuleQueryService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetPatientBasicInfoAsync_ExistingPatient_ReturnsDto()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _context.Patients.Add(new Patient { Id = patientId, Name = "张三" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientBasicInfoAsync(patientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("张三", result.Name);
    }

    [Fact]
    public async Task GetPatientsBasicInfoAsync_BatchQuery_ReturnsAllMatching()
    {
        // Arrange
        var patient1 = new Patient { Id = Guid.NewGuid(), Name = "张三" };
        var patient2 = new Patient { Id = Guid.NewGuid(), Name = "李四" };
        _context.Patients.AddRange(patient1, patient2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientsBasicInfoAsync(new[] { patient1.Id, patient2.Id });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(patient1.Id));
        Assert.True(result.ContainsKey(patient2.Id));
    }
}
```

### 6.2 重构后PrescriptionService测试

```csharp
public class PrescriptionServiceTests
{
    private readonly Mock<IPrescriptionRepository> _repositoryMock;
    private readonly Mock<ICrossModuleQueryService> _crossModuleMock; // 只需要Mock这一个
    private readonly PrescriptionService _service;

    public PrescriptionServiceTests()
    {
        _repositoryMock = new Mock<IPrescriptionRepository>();
        _crossModuleMock = new Mock<ICrossModuleQueryService>();
        // 其他Mock初始化...
        _service = new PrescriptionService(
            _repositoryMock.Object,
            _crossModuleMock.Object,
            // ...
        );
    }

    [Fact]
    public async Task SearchPrescriptionsAsync_WithPatientName_FiltersCorrectly()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _repositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Prescription>
            {
                new Prescription { Id = Guid.NewGuid(), MedicalCaseId = medicalCaseId }
            });

        _crossModuleMock
            .Setup(x => x.GetMedicalCasesBasicInfoAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, MedicalCaseBasicDto>
            {
                { medicalCaseId, new MedicalCaseBasicDto { Id = medicalCaseId, PatientId = patientId, TCMDiagnosis = "肝郁气滞" } }
            });

        _crossModuleMock
            .Setup(x => x.GetPatientsBasicInfoAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, PatientBasicDto>
            {
                { patientId, new PatientBasicDto { Id = patientId, Name = "张三" } }
            });

        // Act
        var result = await _service.SearchPrescriptionsAsync(patientName: "张");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("张三", result.Value[0].PatientName);
    }
}
```

---

## Part 7: 迁移步骤概览

### Phase 1: 基础设施准备 (13个任务)
1. 创建3个BasicDto文件
2. 创建ICrossModuleQueryService接口
3. 实现CrossModuleQueryService (患者/医案/药材查询)
4. 注册服务到DI容器
5. 创建单元测试 (17个测试用例)

### Phase 2: 重构Prescriptions模块 (10个任务)
1. 分析现有方法使用情况
2. 修改构造函数
3. 重构LoadRelatedDataAsync
4. 重构SearchPrescriptionsAsync
5. 重构GetPatientRecentPrescriptionsAsync
6. 移除5个跨模块项目引用
7. 清理using语句
8. 更新单元测试

### Phase 3: 重构Formula模块 (8个任务)
1. 分析现有方法使用情况
2. 修改构造函数
3. 重构ValidateFormulaHerbAsync
4. 重构TryMatchHerbAsync
5. 移除1个跨模块项目引用
6. 清理using语句
7. 更新单元测试

### Phase 4: 创建模块通信规范 (2个任务)
1. 创建规范目录结构
2. 编写5个Requirements (MOD-001 ~ MOD-005)

### Phase 5: 综合验证 (6个任务)
1. 编译整个解决方案
2. 运行所有单元测试
3. 运行集成测试
4. 验证Prescriptions模块依赖
5. 验证Formula模块依赖
6. 手动功能验证

---

## References

- [ABP Framework - Module Development](https://docs.abp.io/en/abp/latest/Module-Development-Basics)
- [Modular Monolith Architecture](https://www.kamilgrzybek.com/design/modular-monolith-primer/)
- [DDD Bounded Context](https://martinfowler.com/bliki/BoundedContext.html)
- [Clean Architecture - Module Boundaries](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [现有service-conventions规范](../../specs/service-conventions/spec.md)
- [现有repository-patterns规范](../../specs/repository-patterns/spec.md)
- [现有project-architecture规范](../../specs/project-architecture/spec.md)
