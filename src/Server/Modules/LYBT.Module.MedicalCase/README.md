# LYBT.Module.MedicalCase - 医疗案例管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(医疗案例管理 - **核心聚合根**)
- **职责**:作为整个诊疗流程的**管理容器**和**聚合根**,每一个`MedicalCase`代表一次完整的看诊会话,1:1关联`Consultation`诊断记录,可选关联`Prescription`处方。统一管理患者从接诊到完成的全程诊疗状态,包括状态迁移、权限控制、业务规则验证。采用标准三层架构（Controller → Service → Repository）,配合**MedicalCaseRules业务规则类**确保诊疗流程的完整性和一致性。

##  代码结构

```
LYBT.Module.MedicalCase/
├── MedicalCaseModule.cs                # 模块依赖注入注册
│   └── AddMedicalCaseModule()          # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                         # 模块接口定义
│   └── IMedicalCaseRepository.cs       # 医案仓储接口(11个方法)
├── Services/                           # 业务逻辑实现
│   ├── IMedicalCaseService.cs          # 服务接口定义
│   ├── MedicalCaseService.cs           # 医案服务(19个方法)
│   │   ├── CreateAsync()               # 创建医案
│   │   ├── UpdateConsultationAsync()   # 更新诊断记录
│   │   ├── SetPrescriptionFlagAsync()  # 设置处方标志(是否开方)
│   │   ├── CreatePrescriptionAsync()   # 创建处方
│   │   ├── UpdatePrescriptionAsync()   # 更新处方
│   │   ├── DeletePrescriptionAsync()   # 删除处方
│   │   ├── UpdateStatusAsync()         # 更新医案状态(状态机)
│   │   ├── CompleteAsync()             # 完成医案(终态)
│   │   ├── CloseCaseAsync()            # 关闭医案(终态)
│   │   ├── GetByIdAsync()              # 按ID查询医案详情
│   │   ├── GetListAsync()              # 分页查询医案列表
│   │   ├── GetConsultationListAsync()  # 查询诊断记录列表
│   │   ├── GetPrescriptionListAsync()  # 查询处方列表
│   │   ├── GetUnfinishedCaseByPatientIdAsync() # 获取患者未完成医案
│   │   ├── CanEditAsync()              # 权限检查(是否可编辑)
│   │   ├── CanDeletePrescriptionAsync()# 权限检查(是否可删除处方)
│   │   └── IsValidStatusTransition()   # 状态迁移验证
│   └── MedicalCaseRules.cs             # 业务规则类(6个规则)
│       ├── CanCreateNewCase()          # 患者是否可创建新医案
│       ├── CanEdit()                   # 医案是否可编辑(非终态)
│       ├── CanDelete()                 # 医案是否可删除(草稿状态)
│       ├── CanComplete()               # 医案是否可完成(必须有诊断)
│       ├── ValidateNewCaseCreation()   # 验证新医案创建
│       └── ValidateCaseUpdate()        # 验证医案更新
├── Repositories/                       # 数据仓储实现
│   └── MedicalCaseRepository.cs        # 医案仓储(11个方法)
│       ├── GetBaseQuery()              # 基础查询(无Include,仅Where过滤)
│       ├── GetDetailQuery()            # 详情查询(Include Consultation+Prescription.ThenInclude(Items))
│       ├── GetByPatientIdAsync()       # 按患者ID查询医案列表
│       ├── GetByIdWithDetailsAsync()   # 查询医案详情(含关联数据)
│       ├── GetPagedWithDetailsAsync()  # 分页查询详情(含统计)
│       ├── GetByDoctorIdAsync()        # 按医生ID查询医案列表
│       ├── UpdateAsync()               # 更新医案(统一入口)
│       ├── GetPendingCasesAsync()      # 获取待处理医案
│       ├── QueryAsync()                # 动态查询(支持多条件)
│       ├── GetUnfinishedCaseByPatientIdAsync() # 获取患者未完成医案
│       └── MaskPhoneNumber()           # 隐私保护(脱敏手机号)
├── Validators/                         # FluentValidation验证器 (DTO定义已迁移至Shared层)
│   ├── MedicalCaseCreateDtoValidator.cs # 创建医案DTO验证
│   └── MedicalCaseUpdateDtoValidator.cs # 更新医案DTO验证
└── Mapping/                            # AutoMapper映射配置
    └── MedicalCaseMappingProfile.cs    # Entity ↔ DTO映射规则
```

**说明**:
- **MedicalCaseModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **MedicalCaseService**:19个方法覆盖诊疗流程的完整生命周期管理
- **MedicalCaseRules**:6个业务规则方法确保诊疗流程的业务逻辑正确性
- **MedicalCaseRepository**:11个方法提供灵活的数据查询能力(详情、分页、动态查询)
- **Dtos目录**:8个专属DTO用于诊断、处方的嵌套数据传输
- **聚合根模式**:MedicalCase作为聚合根,统一管理Consultation和Prescription的生命周期

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(MedicalCaseModel、ConsultationModel、PrescriptionModel)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(MedicalCaseDto、CreateMedicalCaseDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IMedicalCaseService、IConsultationRepository、IPrescriptionRepository)

### 被依赖项目
1. **LYBT.Module.Consultation** - 诊断模块通过MedicalCase聚合根访问
2. **LYBT.Module.Prescriptions** - 处方模块通过MedicalCase聚合根访问
3. **LYBT.WebAPI** - Web服务层通过MedicalCasesController暴露API
4. **测试项目**:
   - LYBT.Module.MedicalCase.Tests（单元测试）
   - LYBT.Module.MedicalCase.IntegrationTests（集成测试）
   - LYBT.Server.ArchTests（架构测试）

### NuGet包
- **FluentValidation** (11.x) - DTO验证框架
- **AutoMapper** (13.x) - 对象映射框架
- **Microsoft.Extensions.DependencyInjection** (8.0.x) - 依赖注入容器

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Entity Framework Core 8**: 通过Repository模式间接使用,用于数据持久化
- **AutoMapper 13.x**: Entity与DTO之间的自动映射
- **FluentValidation 11.x**: DTO数据验证框架
- **LINQ**: 复杂查询表达式(分页、动态查询、状态过滤)
- **异步编程**: 全异步方法(async/await),提升性能
- **聚合根模式**: DDD聚合根设计,确保业务一致性

##  快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj
```

**集成说明**:

### 1. 注册医案模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册医案模块(自动注册仓储+服务+验证器)
        services.AddMedicalCaseModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class MedicalCasesController : ControllerBase
{
    private readonly IMedicalCaseService _medicalCaseService;

    public MedicalCasesController(IMedicalCaseService medicalCaseService)
    {
        _medicalCaseService = medicalCaseService;
    }

    // 创建医案
    [HttpPost]
    public async Task<IActionResult> CreateMedicalCase([FromBody] CreateMedicalCaseDto dto)
    {
        var caseDto = await _medicalCaseService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetMedicalCase), new { id = caseDto.Id }, caseDto);
    }

    // 更新诊断记录
    [HttpPut("{id}/consultation")]
    public async Task<IActionResult> UpdateConsultation(
        Guid id,
        [FromBody] UpdateConsultationRequest request)
    {
        await _medicalCaseService.UpdateConsultationAsync(id, request);
        return NoContent();
    }
}
```

### 3. 诊疗流程完整示例(聚合根模式)
```csharp
// 1. 创建医案(聚合根)
var newCase = await _medicalCaseService.CreateAsync(new CreateMedicalCaseDto
{
    PatientId = patientId,
    DoctorId = doctorId,
    Status = MedicalCaseStatus.InProgress
});

// 2. 更新诊断记录(通过聚合根)
await _medicalCaseService.UpdateConsultationAsync(newCase.Id, new UpdateConsultationRequest
{
    ChiefComplaint = "头痛三天",
    PresentIllness = "患者自述三天前开始出现头痛...",
    Inspection = "面色微红,舌质红",
    Inquiry = "伴有发热,口干",
    TcmDiagnosis = "外感风热",
    TreatmentMethod = "疏风清热"
});

// 3. 设置处方标志(是否需要开方)
await _medicalCaseService.SetPrescriptionFlagAsync(newCase.Id, new SetPrescriptionFlagRequest
{
    HasPrescription = true
});

// 4. 创建处方(通过聚合根)
await _medicalCaseService.CreatePrescriptionAsync(newCase.Id, new CreatePrescriptionRequest
{
    Items = new List<PrescriptionItemDto>
    {
        new() { HerbId = herb1Id, Dosage = 10, Unit = "克" },
        new() { HerbId = herb2Id, Dosage = 15, Unit = "克" }
    },
    UsageInstructions = "水煎服,日一剂"
});

// 5. 完成医案(状态迁移到终态)
await _medicalCaseService.CompleteAsync(newCase.Id);
```

### 4. 业务规则验证(MedicalCaseRules)
```csharp
public class MedicalCaseRules
{
    // 规则1：患者是否可创建新医案（只能有一个未完成医案）
    public static bool CanCreateNewCase(IEnumerable<MedicalCaseModel> existingCases)
    {
        return !existingCases.Any(c =>
            c.Status == MedicalCaseStatus.InProgress ||
            c.Status == MedicalCaseStatus.Pending
        );
    }

    // 规则2：医案是否可编辑（非终态）
    public static bool CanEdit(MedicalCaseModel medicalCase)
    {
        return medicalCase.Status != MedicalCaseStatus.Completed &&
               medicalCase.Status != MedicalCaseStatus.Closed;
    }

    // 规则3：医案是否可完成（必须有诊断记录）
    public static bool CanComplete(MedicalCaseModel medicalCase)
    {
        return medicalCase.Consultation != null &&
               !string.IsNullOrWhiteSpace(medicalCase.Consultation.ChiefComplaint);
    }

    // 规则4：验证新医案创建（业务一致性）
    public static ValidationResult ValidateNewCaseCreation(
        MedicalCaseModel newCase,
        IEnumerable<MedicalCaseModel> existingCases)
    {
        // 检查患者是否有未完成医案
        if (!CanCreateNewCase(existingCases))
        {
            return ValidationResult.Failure("患者已有未完成医案,请先完成或关闭");
        }

        // 检查医生是否有效
        if (newCase.DoctorId == Guid.Empty)
        {
            return ValidationResult.Failure("医生ID无效");
        }

        return ValidationResult.Success();
    }
}

// 在Service中使用业务规则
public async Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto)
{
    // 获取患者现有医案
    var existingCases = await _repository.GetByPatientIdAsync(dto.PatientId);

    // 验证业务规则
    var validation = MedicalCaseRules.ValidateNewCaseCreation(
        newCase,
        existingCases
    );

    if (!validation.IsValid)
    {
        throw new ValidationException(validation.ErrorMessage);
    }

    // 保存医案
    await _repository.AddAsync(newCase);
    return _mapper.Map<MedicalCaseDto>(newCase);
}
```

### 5. 状态机管理(状态迁移验证)
```csharp
// 状态迁移图
// Pending(待接诊) → InProgress(诊疗中) → Completed(已完成)
//                 ↓
//                Closed(已关闭)

private bool IsValidStatusTransition(
    MedicalCaseStatus from,
    MedicalCaseStatus to)
{
    return (from, to) switch
    {
        (MedicalCaseStatus.Pending, MedicalCaseStatus.InProgress) => true,
        (MedicalCaseStatus.InProgress, MedicalCaseStatus.Completed) => true,
        (MedicalCaseStatus.Pending, MedicalCaseStatus.Closed) => true,
        (MedicalCaseStatus.InProgress, MedicalCaseStatus.Closed) => true,
        _ => false // 其他迁移非法
    };
}

// 在Service中验证状态迁移
public async Task UpdateStatusAsync(Guid id, MedicalCaseStatus newStatus)
{
    var medicalCase = await _repository.GetByIdAsync(id);
    if (medicalCase == null) throw new NotFoundException("医案不存在");

    // 验证状态迁移合法性
    if (!IsValidStatusTransition(medicalCase.Status, newStatus))
    {
        throw new ValidationException(
            $"无效的状态迁移:{medicalCase.Status} → {newStatus}"
        );
    }

    // 验证业务规则
    if (newStatus == MedicalCaseStatus.Completed &&
        !MedicalCaseRules.CanComplete(medicalCase))
    {
        throw new ValidationException("医案缺少必要的诊断信息,无法完成");
    }

    medicalCase.Status = newStatus;
    await _repository.UpdateAsync(medicalCase);
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `MedicalCasesController` 对外暴露。

- **API路由前缀**: `/api/v1/medicalcases`

**主要端点**:
- `POST /api/v1/medicalcases` - 创建医案
- `GET /api/v1/medicalcases/{id}` - 按ID查询医案详情
- `GET /api/v1/medicalcases` - 分页查询医案列表
- `PUT /api/v1/medicalcases/{id}/consultation` - 更新诊断记录
- `PUT /api/v1/medicalcases/{id}/prescription-flag` - 设置处方标志
- `POST /api/v1/medicalcases/{id}/prescription` - 创建处方
- `PUT /api/v1/medicalcases/{id}/prescription` - 更新处方
- `DELETE /api/v1/medicalcases/{id}/prescription` - 删除处方
- `PUT /api/v1/medicalcases/{id}/status` - 更新医案状态
- `POST /api/v1/medicalcases/{id}/complete` - 完成医案
- `POST /api/v1/medicalcases/{id}/close` - 关闭医案
- `GET /api/v1/medicalcases/patient/{patientId}/unfinished` - 获取患者未完成医案

**完整API定义**请参考 `IMedicalCaseService` 接口和 `MedicalCasesController` 的实现。

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/medical-case/](../../../../docs/reference/modules/medical-case/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/medical-case-design.md](../../../../docs/explanation/architecture/server/medical-case-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/medical-case-development.md](../../../../docs/how-to-guides/server/medical-case-development.md) *(待创建)*
- **性能优化**:[docs/explanation/performance/repository-include-strategy.md](../../../../docs/explanation/performance/repository-include-strategy.md) - Repository Include预加载策略
- **业务规则**:[docs/business-rules.md](../../../../docs/business-rules.md) - 参见"医案管理规则"章节

---

**最后更新**:2025-11-20 (Epic #2175 Phase 4: Repository Include策略优化)
**维护负责**:Server端开发组
