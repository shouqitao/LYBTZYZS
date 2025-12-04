# Tasks: decouple-server-modules

## Overview

解耦Server端模块间的过度依赖，实现Server端与Desktop端设计思想统一。

**核心问题**:
- Prescriptions模块依赖5个其他模块 (MedicalCase, Patients, Consultation, Herbs, Formula)
- Formula模块依赖Herbs模块 (IHerbRepository注入)

**设计对齐目标**:
- Desktop端: 通过Prism `[ModuleDependency]` 声明功能依赖，数据通过API获取
- Server端(重构后): 通过ICrossModuleQueryService获取跨模块数据，不直接注入Repository

---

## Phase 1: 基础设施准备

### Task 1.1: 创建PatientBasicDto

**目的**: 定义跨模块传递的患者基本信息DTO

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Common/PatientBasicDto.cs`

**内容**:
```csharp
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

**验收标准**:
- [ ] 文件创建在正确位置
- [ ] 命名空间正确 (`LYBT.Shared.Models.Contracts.Common`)
- [ ] 引用 `LYBT.Shared.Models.Enums` 命名空间
- [ ] 包含XML文档注释
- [ ] 4个属性: Id, Name, Gender(枚举类型), Phone
- [ ] 编译通过

---

### Task 1.2: 创建MedicalCaseBasicDto

**目的**: 定义跨模块传递的医案基本信息DTO，包含诊断摘要

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Common/MedicalCaseBasicDto.cs`

**内容**:
```csharp
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

**依赖**: 需要引用 `MedicalCaseStatus` 枚举

**验收标准**:
- [ ] 文件创建在正确位置
- [ ] 命名空间正确
- [ ] 包含XML文档注释
- [ ] 5个属性: Id, PatientId, Status, CreatedAt, TCMDiagnosis
- [ ] TCMDiagnosis字段有说明注释
- [ ] 编译通过

---

### Task 1.3: 创建HerbBasicDto

**目的**: 定义跨模块传递的药材基本信息DTO，供Formula模块验证和匹配使用

**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Common/HerbBasicDto.cs`

**内容**:
```csharp
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

**验收标准**:
- [ ] 文件创建在正确位置
- [ ] 命名空间正确
- [ ] 包含XML文档注释
- [ ] 4个属性: Id, Name, Pinyin, Category
- [ ] 编译通过

---

### Task 1.4: 创建ICrossModuleQueryService接口

**目的**: 定义跨模块查询服务接口，统一跨模块只读数据访问

**文件**: `src/Server/Core/LYBT.Infrastructure/Services/ICrossModuleQueryService.cs`

**方法清单**:

| 方法 | 返回类型 | 用途 |
|------|----------|------|
| GetPatientBasicInfoAsync(Guid) | PatientBasicDto? | 单个患者查询 |
| GetPatientsBasicInfoAsync(IEnumerable<Guid>) | Dictionary<Guid, PatientBasicDto> | 批量患者查询 |
| GetMedicalCaseBasicInfoAsync(Guid) | MedicalCaseBasicDto? | 单个医案查询 |
| GetMedicalCasesBasicInfoAsync(IEnumerable<Guid>) | Dictionary<Guid, MedicalCaseBasicDto> | 批量医案查询 |
| GetHerbBasicInfoAsync(Guid) | HerbBasicDto? | 单个药材查询 |
| GetHerbByNameOrPinyinAsync(string) | HerbBasicDto? | 按名称/拼音查询药材 |

**验收标准**:
- [ ] 接口文件创建在正确位置
- [ ] 命名空间: `LYBT.Infrastructure.Services`
- [ ] 接口包含完整XML文档注释
- [ ] 6个方法签名定义
- [ ] 每个方法有summary注释说明用途
- [ ] 所有方法返回Task<T>
- [ ] 编译通过

---

### Task 1.5: 实现CrossModuleQueryService - 患者查询

**目的**: 实现患者相关的跨模块查询方法

**文件**: `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs`

**实现方法**:
1. `GetPatientBasicInfoAsync` - 单个患者查询
2. `GetPatientsBasicInfoAsync` - 批量患者查询

**技术要求**:
- 使用AppDbContext直接查询
- 使用AsNoTracking()优化只读查询
- 使用Select()投影查询，减少数据传输
- 过滤已删除记录 (IsDeleted = false)
- 批量查询使用Contains避免N+1

**验收标准**:
- [ ] 类文件创建，实现ICrossModuleQueryService接口
- [ ] 注入AppDbContext依赖
- [ ] GetPatientBasicInfoAsync使用AsNoTracking
- [ ] GetPatientBasicInfoAsync使用Select投影
- [ ] GetPatientBasicInfoAsync过滤IsDeleted
- [ ] GetPatientsBasicInfoAsync处理空列表
- [ ] GetPatientsBasicInfoAsync使用Contains批量查询
- [ ] GetPatientsBasicInfoAsync返回Dictionary<Guid, PatientBasicDto>
- [ ] 编译通过

---

### Task 1.6: 实现CrossModuleQueryService - 医案查询

**目的**: 实现医案相关的跨模块查询方法，包含诊断信息关联

**文件**: `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs`

**实现方法**:
1. `GetMedicalCaseBasicInfoAsync` - 单个医案查询(含诊断)
2. `GetMedicalCasesBasicInfoAsync` - 批量医案查询(含诊断)

**技术要求**:
- 医案查询需要关联Consultation获取TCMDiagnosis
- 单个查询使用子查询获取诊断
- 批量查询分两步: 先查医案，再批量查诊断，最后合并
- 避免N+1问题

**关键实现逻辑**:
```csharp
// 批量查询优化: 分两步避免复杂Join
var medicalCases = await _context.MedicalCases
    .AsNoTracking()
    .Where(mc => ids.Contains(mc.Id) && !mc.IsDeleted)
    .Select(...)
    .ToListAsync();

var consultations = await _context.Consultations
    .AsNoTracking()
    .Where(c => ids.Contains(c.Id))
    .Select(c => new { c.Id, c.TCMDiagnosis })
    .ToDictionaryAsync(...);

// 合并诊断信息
foreach (var mc in medicalCases)
{
    if (consultations.TryGetValue(mc.Id, out var diagnosis))
        mc.TCMDiagnosis = diagnosis;
}
```

**验收标准**:
- [ ] GetMedicalCaseBasicInfoAsync实现
- [ ] GetMedicalCaseBasicInfoAsync包含TCMDiagnosis
- [ ] GetMedicalCasesBasicInfoAsync实现
- [ ] GetMedicalCasesBasicInfoAsync处理空列表
- [ ] 批量查询避免N+1 (验证只有2次数据库查询)
- [ ] 编译通过

---

### Task 1.7: 实现CrossModuleQueryService - 药材查询

**目的**: 实现药材相关的跨模块查询方法，供Formula模块使用

**文件**: `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs`

**实现方法**:
1. `GetHerbBasicInfoAsync` - 按ID查询药材
2. `GetHerbByNameOrPinyinAsync` - 按名称或拼音查询药材

**技术要求**:
- GetHerbByNameOrPinyinAsync需要处理空输入
- 名称匹配使用精确匹配 (Name == nameOrPinyin || Pinyin == nameOrPinyin)
- 过滤已删除记录

**验收标准**:
- [ ] GetHerbBasicInfoAsync实现
- [ ] GetHerbBasicInfoAsync使用AsNoTracking
- [ ] GetHerbByNameOrPinyinAsync实现
- [ ] GetHerbByNameOrPinyinAsync处理空字符串输入返回null
- [ ] GetHerbByNameOrPinyinAsync支持按Name或Pinyin匹配
- [ ] 编译通过

---

### Task 1.8: 注册CrossModuleQueryService

**目的**: 在DI容器中注册跨模块查询服务

**文件**: `src/Server/Core/LYBT.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

**变更**:
```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    // 现有注册...

    // 新增: 跨模块查询服务
    services.AddScoped<ICrossModuleQueryService, CrossModuleQueryService>();

    return services;
}
```

**验收标准**:
- [ ] 在AddInfrastructure方法中添加注册
- [ ] 使用AddScoped生命周期
- [ ] 编译通过
- [ ] 可以在其他服务中注入ICrossModuleQueryService

---

### Task 1.9: 创建CrossModuleQueryService单元测试文件

**目的**: 为CrossModuleQueryService创建测试类框架

**文件**: `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/CrossModuleQueryServiceTests.cs`

**测试类结构**:
```csharp
public class CrossModuleQueryServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CrossModuleQueryService _service;

    public CrossModuleQueryServiceTests()
    {
        // 使用InMemory数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new CrossModuleQueryService(_context);
    }

    public void Dispose() => _context.Dispose();
}
```

**验收标准**:
- [ ] 测试文件创建
- [ ] 使用InMemory数据库
- [ ] 实现IDisposable清理资源
- [ ] 编译通过

---

### Task 1.10: 添加患者查询测试用例

**文件**: `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/CrossModuleQueryServiceTests.cs`

**测试用例**:

| 测试方法 | 场景 | 期望结果 |
|----------|------|----------|
| GetPatientBasicInfoAsync_ExistingPatient_ReturnsDto | 查询存在的患者 | 返回PatientBasicDto |
| GetPatientBasicInfoAsync_NonExistingPatient_ReturnsNull | 查询不存在的患者 | 返回null |
| GetPatientBasicInfoAsync_DeletedPatient_ReturnsNull | 查询已删除的患者 | 返回null |
| GetPatientsBasicInfoAsync_EmptyList_ReturnsEmptyDictionary | 空ID列表 | 返回空Dictionary |
| GetPatientsBasicInfoAsync_MultiplePatients_ReturnsAll | 批量查询 | 返回所有匹配患者 |
| GetPatientsBasicInfoAsync_SomeDeleted_ReturnsOnlyActive | 部分已删除 | 只返回活跃患者 |

**验收标准**:
- [ ] 6个测试用例实现
- [ ] 使用AAA模式 (Arrange-Act-Assert)
- [ ] 所有测试通过

---

### Task 1.11: 添加医案查询测试用例

**文件**: `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/CrossModuleQueryServiceTests.cs`

**测试用例**:

| 测试方法 | 场景 | 期望结果 |
|----------|------|----------|
| GetMedicalCaseBasicInfoAsync_WithConsultation_IncludesDiagnosis | 医案有诊断 | 返回包含TCMDiagnosis |
| GetMedicalCaseBasicInfoAsync_WithoutConsultation_DiagnosisNull | 医案无诊断 | TCMDiagnosis为null |
| GetMedicalCaseBasicInfoAsync_NonExisting_ReturnsNull | 不存在的医案 | 返回null |
| GetMedicalCasesBasicInfoAsync_BatchQuery_ReturnsAll | 批量查询 | 返回所有匹配 |
| GetMedicalCasesBasicInfoAsync_MixedDiagnosis_CorrectAssociation | 部分有诊断 | 正确关联诊断 |

**验收标准**:
- [ ] 5个测试用例实现
- [ ] 验证TCMDiagnosis正确关联
- [ ] 所有测试通过

---

### Task 1.12: 添加药材查询测试用例

**文件**: `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/CrossModuleQueryServiceTests.cs`

**测试用例**:

| 测试方法 | 场景 | 期望结果 |
|----------|------|----------|
| GetHerbBasicInfoAsync_ExistingHerb_ReturnsDto | 查询存在的药材 | 返回HerbBasicDto |
| GetHerbBasicInfoAsync_DeletedHerb_ReturnsNull | 查询已删除药材 | 返回null |
| GetHerbByNameOrPinyinAsync_ByName_ReturnsDto | 按名称查询 | 返回匹配药材 |
| GetHerbByNameOrPinyinAsync_ByPinyin_ReturnsDto | 按拼音查询 | 返回匹配药材 |
| GetHerbByNameOrPinyinAsync_EmptyString_ReturnsNull | 空字符串 | 返回null |
| GetHerbByNameOrPinyinAsync_NonExisting_ReturnsNull | 不存在 | 返回null |

**验收标准**:
- [ ] 6个测试用例实现
- [ ] 所有测试通过

---

### Task 1.13: 运行Phase 1测试验证

**命令**:
```bash
dotnet test tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/ --filter "FullyQualifiedName~CrossModuleQueryServiceTests"
```

**验收标准**:
- [ ] 所有17个测试用例通过
- [ ] 无编译警告

---

## Phase 2: 重构Prescriptions模块

### Task 2.1: 分析PrescriptionService现有方法

**目的**: 识别所有使用跨模块Repository的方法

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**分析结果**:

| 方法 | 使用的跨模块Repository | 用途 |
|------|------------------------|------|
| LoadRelatedDataAsync | _medicalCaseRepository, _patientRepository, _consultationRepository | 加载关联数据 |
| SearchPrescriptionsAsync | 调用LoadRelatedDataAsync | 处方搜索 |
| GetPatientRecentPrescriptionsAsync | 调用LoadRelatedDataAsync | 患者最近处方 |

**验收标准**:
- [ ] 确认所有使用跨模块Repository的方法
- [ ] 记录每个方法的改造点

---

### Task 2.2: 修改PrescriptionService构造函数

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**变更**:

移除:
```csharp
private readonly IMedicalCaseRepository _medicalCaseRepository;
private readonly IPatientRepository _patientRepository;
private readonly IConsultationRepository _consultationRepository;
```

新增:
```csharp
private readonly ICrossModuleQueryService _crossModuleQuery;
```

更新构造函数参数和赋值。

**验收标准**:
- [ ] 移除3个跨模块Repository字段
- [ ] 新增_crossModuleQuery字段
- [ ] 构造函数参数更新
- [ ] 构造函数赋值更新
- [ ] 编译通过

---

### Task 2.3: 重构LoadRelatedDataAsync方法

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**当前实现问题**:
```csharp
// 加载全量数据，性能风险
var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
var allConsultations = await _consultationRepository.GetAllAsync();
var allPatients = await _patientRepository.GetAllAsync();
```

**重构后**:
```csharp
private async Task<(Dictionary<Guid, MedicalCaseBasicDto> medicalCases,
                    Dictionary<Guid, PatientBasicDto> patients)>
    LoadRelatedDataAsync(IEnumerable<Guid> medicalCaseIds)
{
    // 批量查询医案（包含诊断）
    var medicalCases = await _crossModuleQuery.GetMedicalCasesBasicInfoAsync(medicalCaseIds);

    // 提取患者ID并批量查询
    var patientIds = medicalCases.Values.Select(mc => mc.PatientId).Distinct();
    var patients = await _crossModuleQuery.GetPatientsBasicInfoAsync(patientIds);

    return (medicalCases, patients);
}
```

**验收标准**:
- [ ] 方法签名修改(接收medicalCaseIds参数)
- [ ] 返回类型使用BasicDto
- [ ] 使用_crossModuleQuery替代Repository
- [ ] 移除_consultationRepository调用(诊断从MedicalCaseBasicDto获取)
- [ ] 编译通过

---

### Task 2.4: 重构SearchPrescriptionsAsync方法

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**变更要点**:
1. 先获取处方，收集医案ID
2. 调用重构后的LoadRelatedDataAsync
3. 内存过滤与关联
4. 更新字段访问 (使用BasicDto属性)

**验收标准**:
- [ ] 使用重构后的LoadRelatedDataAsync
- [ ] 字典类型从Entity改为BasicDto
- [ ] 患者姓名筛选逻辑正确
- [ ] 症状/诊断关键字筛选逻辑正确
- [ ] 编译通过

---

### Task 2.5: 重构GetPatientRecentPrescriptionsAsync方法

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**变更要点**:
- 同Task 2.4，使用CrossModuleQueryService
- 确保返回结果正确关联患者信息

**验收标准**:
- [ ] 使用重构后的LoadRelatedDataAsync
- [ ] 功能逻辑不变
- [ ] 编译通过

---

### Task 2.6: 移除Prescriptions模块跨模块项目引用

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/LYBT.Module.Prescriptions.csproj`

**移除的ProjectReference**:
```xml
<!-- 移除以下引用 -->
<ProjectReference Include="..\LYBT.Module.Patients\LYBT.Module.Patients.csproj" />
<ProjectReference Include="..\LYBT.Module.Consultation\LYBT.Module.Consultation.csproj" />
<ProjectReference Include="..\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj" />
<ProjectReference Include="..\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
<ProjectReference Include="..\LYBT.Module.Formula\LYBT.Module.Formula.csproj" />
```

**保留的ProjectReference**:
```xml
<ProjectReference Include="..\..\Core\LYBT.Entities\LYBT.Entities.csproj" />
<ProjectReference Include="..\..\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
```

**验收标准**:
- [ ] 5个跨模块引用移除
- [ ] 保留Entities和Infrastructure引用
- [ ] 编译通过

---

### Task 2.7: 清理PrescriptionService的using语句

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**移除不再需要的using**:
```csharp
// 移除
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.MedicalCase.Repositories;
```

**新增**:
```csharp
using LYBT.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
```

**验收标准**:
- [ ] 移除无用using
- [ ] 添加必要using
- [ ] 编译无警告

---

### Task 2.8: 更新PrescriptionService单元测试 - Mock替换

**文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Services/PrescriptionServiceTests.cs`

**变更**:

移除Mock:
```csharp
private readonly Mock<IMedicalCaseRepository> _medicalCaseRepositoryMock;
private readonly Mock<IPatientRepository> _patientRepositoryMock;
private readonly Mock<IConsultationRepository> _consultationRepositoryMock;
```

新增Mock:
```csharp
private readonly Mock<ICrossModuleQueryService> _crossModuleQueryMock;
```

更新测试类构造函数。

**验收标准**:
- [ ] Mock对象替换完成
- [ ] 测试类构造函数更新
- [ ] 编译通过

---

### Task 2.9: 更新PrescriptionService单元测试 - 测试数据

**文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Services/PrescriptionServiceTests.cs`

**变更**:
- 测试数据从Entity改为BasicDto
- Setup方法使用新的Mock接口

**示例**:
```csharp
_crossModuleQueryMock
    .Setup(x => x.GetMedicalCasesBasicInfoAsync(It.IsAny<IEnumerable<Guid>>()))
    .ReturnsAsync(new Dictionary<Guid, MedicalCaseBasicDto>
    {
        { medicalCaseId, new MedicalCaseBasicDto { Id = medicalCaseId, PatientId = patientId, ... } }
    });
```

**验收标准**:
- [ ] 测试数据更新为BasicDto
- [ ] Mock Setup使用新接口方法
- [ ] 所有现有测试用例编译通过

---

### Task 2.10: 运行Prescriptions模块测试验证

**命令**:
```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/
```

**验收标准**:
- [ ] 所有单元测试通过
- [ ] 无编译警告

---

## Phase 3: 重构Formula模块

### Task 3.1: 分析FormulaService现有方法

**目的**: 识别所有使用IHerbRepository的方法

**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

**分析结果**:

| 方法 | 使用的Repository方法 | 用途 |
|------|---------------------|------|
| ValidateFormulaHerbAsync | _herbRepository.GetByIdAsync() | 验证药材存在 |
| TryMatchHerbAsync | _herbRepository.GetByNameOrPinyinAsync() | 按名称/拼音匹配药材 |

**验收标准**:
- [ ] 确认使用IHerbRepository的方法
- [ ] 记录改造点

---

### Task 3.2: 修改FormulaService构造函数

**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

**变更**:

移除:
```csharp
private readonly IHerbRepository _herbRepository;
```

新增:
```csharp
private readonly ICrossModuleQueryService _crossModuleQuery;
```

**验收标准**:
- [ ] 移除_herbRepository字段
- [ ] 新增_crossModuleQuery字段
- [ ] 构造函数参数更新
- [ ] 编译通过

---

### Task 3.3: 重构ValidateFormulaHerbAsync方法

**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

**当前**:
```csharp
var herb = await _herbRepository.GetByIdAsync(selectedHerbId);
```

**重构后**:
```csharp
var herb = await _crossModuleQuery.GetHerbBasicInfoAsync(selectedHerbId);
```

**验收标准**:
- [ ] 使用_crossModuleQuery替代_herbRepository
- [ ] 返回值处理逻辑不变
- [ ] 编译通过

---

### Task 3.4: 重构TryMatchHerbAsync方法

**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

**当前**:
```csharp
var herb = await _herbRepository.GetByNameOrPinyinAsync(herbName);
```

**重构后**:
```csharp
var herb = await _crossModuleQuery.GetHerbByNameOrPinyinAsync(herbName);
```

**验收标准**:
- [ ] 使用_crossModuleQuery替代_herbRepository
- [ ] 返回值处理逻辑不变
- [ ] 编译通过

---

### Task 3.5: 移除Formula模块跨模块项目引用

**文件**: `src/Server/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj`

**移除的ProjectReference**:
```xml
<ProjectReference Include="..\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj" />
```

**验收标准**:
- [ ] 跨模块引用移除
- [ ] 编译通过

---

### Task 3.6: 清理FormulaService的using语句

**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`

**移除**:
```csharp
using LYBT.Module.Herbs.Repositories;
```

**新增**:
```csharp
using LYBT.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
```

**验收标准**:
- [ ] 移除无用using
- [ ] 添加必要using
- [ ] 编译无警告

---

### Task 3.7: 更新FormulaService单元测试

**文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Formula.Tests/Services/FormulaServiceTests.cs`

**变更**:
1. 移除IHerbRepository Mock
2. 新增ICrossModuleQueryService Mock
3. 更新测试数据使用HerbBasicDto

**验收标准**:
- [ ] Mock对象替换完成
- [ ] 测试数据更新
- [ ] 所有测试通过

---

### Task 3.8: 运行Formula模块测试验证

**命令**:
```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Formula.Tests/
```

**验收标准**:
- [ ] 所有单元测试通过
- [ ] 无编译警告

---

## Phase 4: 创建模块通信规范

### Task 4.1: 创建module-communication规范目录

**目的**: 为模块通信规范创建目录结构

**文件**: `openspec/specs/module-communication/spec.md`

**验收标准**:
- [ ] 目录创建
- [ ] spec.md文件创建

---

### Task 4.2: 编写模块通信规范内容

**文件**: `openspec/specs/module-communication/spec.md`

**Requirements清单**:

| ID | 名称 | 描述 |
|----|------|------|
| MOD-001 | 合法依赖类型 | 定义允许的跨模块依赖方式 |
| MOD-002 | 禁止的依赖类型 | 定义禁止的跨模块依赖方式 |
| MOD-003 | CrossModuleQueryService使用规范 | 定义跨模块查询服务的使用规范 |
| MOD-004 | 模块边界验证 | 定义如何验证模块边界 |
| MOD-005 | 允许的跨模块依赖清单 | 列出所有合法的跨模块依赖 |

**验收标准**:
- [ ] 规范文件完整
- [ ] 5个Requirements定义
- [ ] 每个Requirement包含Scenarios
- [ ] 包含Cross-Reference部分

---

## Phase 5: 综合验证

### Task 5.1: 编译整个解决方案

**命令**:
```bash
dotnet build LYBT.All.sln
```

**验收标准**:
- [ ] 编译成功
- [ ] 无编译警告
- [ ] 无编译错误

---

### Task 5.2: 运行所有单元测试

**命令**:
```bash
dotnet test tests/UnitTests/ --logger "console;verbosity=normal"
```

**验收标准**:
- [ ] Infrastructure测试通过
- [ ] Prescriptions测试通过
- [ ] Formula测试通过
- [ ] 其他模块测试不受影响

---

### Task 5.3: 运行集成测试

**命令**:
```bash
dotnet test tests/IntegrationTests/
```

**验收标准**:
- [ ] 所有集成测试通过

---

### Task 5.4: 验证Prescriptions模块依赖

**命令**:
```bash
grep -A20 "ProjectReference" src/Server/Modules/LYBT.Module.Prescriptions/*.csproj
```

**期望结果**:
- LYBT.Entities ✓
- LYBT.Infrastructure ✓
- LYBT.Module.Patients ✗ (已移除)
- LYBT.Module.Consultation ✗ (已移除)
- LYBT.Module.MedicalCase ✗ (已移除)
- LYBT.Module.Herbs ✗ (已移除)
- LYBT.Module.Formula ✗ (已移除)

**验收标准**:
- [ ] 5个跨模块引用确认移除

---

### Task 5.5: 验证Formula模块依赖

**命令**:
```bash
grep -A20 "ProjectReference" src/Server/Modules/LYBT.Module.Formula/*.csproj
```

**期望结果**:
- LYBT.Entities ✓
- LYBT.Infrastructure ✓
- LYBT.Module.Herbs ✗ (已移除)

**验收标准**:
- [ ] 跨模块引用确认移除

---

### Task 5.6: 手动功能验证清单

**测试场景**:

| 场景 | 操作 | 期望结果 |
|------|------|----------|
| 处方搜索-按患者姓名 | 在处方搜索界面输入患者姓名 | 正确显示匹配处方 |
| 处方搜索-按症状关键字 | 在处方搜索界面输入症状关键字 | 正确显示匹配处方 |
| 获取患者最近处方 | 查看患者详情中的最近处方 | 正确显示处方列表 |
| 经验方药材验证 | 在经验方中选择药材 | 正确验证药材存在 |
| 经验方药材匹配 | 在经验方中输入药材名称 | 正确匹配药材 |

**验收标准**:
- [ ] 5个场景功能正常
- [ ] 无性能明显下降

---

## Summary

| Phase | 任务数 | 主要内容 |
|-------|--------|----------|
| Phase 1: 基础设施准备 | 13 | DTO定义、接口设计、服务实现、测试编写 |
| Phase 2: 重构Prescriptions | 10 | 构造函数重构、方法重构、引用移除、测试更新 |
| Phase 3: 重构Formula | 8 | 构造函数重构、方法重构、引用移除、测试更新 |
| Phase 4: 创建规范 | 2 | 模块通信规范文档 |
| Phase 5: 综合验证 | 6 | 编译验证、测试验证、依赖验证、功能验证 |

**总计**: 39个任务

**关键里程碑**:
1. Phase 1完成: CrossModuleQueryService可用 (包含Patient、MedicalCase、Herb查询)
2. Phase 2完成: Prescriptions模块完全解耦 (依赖从5个减少到0个)
3. Phase 3完成: Formula模块完全解耦 (依赖从1个减少到0个)
4. Phase 5完成: 所有测试通过，功能验证通过

**设计成果**:
- Server端与Desktop端设计思想统一
- Prescriptions模块成为完全独立的模块
- Formula模块成为完全独立的模块
- 建立模块通信规范，防止未来设计回退
