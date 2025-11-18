# LYBT.Module.Consultation - 看诊管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(看诊管理)
- **职责**:提供中医看诊的完整生命周期管理,包括中医四诊合参（望、闻、问、切）记录、辨证论治、三步工作流（辩证→施治→总结）、患者就诊历史查询等功能。作为MedicalCase聚合根的核心组成部分,采用共享主键的一对一关系设计,确保看诊数据与医案数据的强关联性。采用标准三层架构（Controller → Service → Repository）,确保业务逻辑清晰、数据访问高效。

## 📂 代码结构

```
LYBT.Module.Consultation/
├── ConsultationModule.cs                    # 模块依赖注入注册
│   └── AddConsultationModule()              # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                              # 模块接口定义
│   └── IConsultationRepository.cs           # 看诊仓储接口(6个方法)
├── Services/                                # 业务逻辑实现
│   └── ConsultationService.cs               # 看诊服务(2个方法)
│       ├── GetByIdAsync()                   # 按ID查询看诊详情
│       └── GetByMedicalCaseIdAsync()        # 按医案ID查询看诊
├── Repositories/                            # 数据仓储实现
│   └── ConsultationRepository.cs            # 看诊仓储(6个方法)
│       ├── GetByPatientIdAsync()            # 按患者ID查询就诊历史
│       ├── GetPagedWithDetailsAsync()       # 分页查询看诊(含详情)
│       ├── GetByIdWithDetailsAsync()        # 按ID查询看诊详情(含关联)
│       ├── GetByMedicalCaseIdAsync()        # 按医案ID查询看诊
│       ├── GetAllAsync()                    # 查询所有看诊记录
│       └── FindAsync()                      # 条件查询看诊记录
├── Validators/                              # FluentValidation验证器
│   ├── ConsultationCreateDtoValidator.cs    # 创建看诊DTO验证
│   └── ConsultationUpdateDtoValidator.cs    # 更新看诊DTO验证
└── Mapping/                                 # AutoMapper映射配置
    └── ConsultationMappingProfile.cs        # Entity ↔ DTO映射规则
```

**说明**:
- **ConsultationModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **ConsultationService**:2个方法提供核心看诊查询功能(创建通过MedicalCase完成)
- **ConsultationRepository**:6个方法提供灵活的数据查询能力(患者历史、分页、详情等)
- **共享主键设计**:Consultation.Id与MedicalCase.Id相同,通过EF Core配置一对一关系
- **三步工作流**:Step1CompletedAt(辩证)、Step2CompletedAt(施治)、Step3CompletedAt(总结)
- **处方开关**:PrescriptionEnabled控制是否开具处方(灵活支持不开方场景)
- **Validators**:FluentValidation验证器确保DTO数据完整性(中医四诊、诊断信息)
- **Mapping**:AutoMapper配置统一处理Entity与DTO的转换

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(Consultation)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(ConsultationDto、CreateConsultationDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IConsultationService、IConsultationRepository)

### 被依赖项目
1. **LYBT.Module.MedicalCase** - 医案模块创建看诊(聚合根管理)
2. **LYBT.Module.Prescriptions** - 处方模块使用看诊数据
3. **LYBT.WebAPI** - Web服务层通过ConsultationsController暴露API
4. **测试项目**:
   - LYBT.Module.Consultation.Tests（单元测试）
   - LYBT.Module.Consultation.IntegrationTests（集成测试）
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
- **LINQ**: 复杂查询表达式(分页、过滤、Include关联)
- **异步编程**: 全异步方法(async/await),提升性能

##  快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Consultation/LYBT.Module.Consultation.csproj
```

**集成说明**:

### 1. 注册看诊模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册看诊模块(自动注册仓储+服务+验证器)
        services.AddConsultationModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;

    public ConsultationsController(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }

    // 按医案ID查询看诊
    [HttpGet("medical-case/{medicalCaseId}")]
    public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
    {
        var consultation = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
        if (consultation == null) return NotFound();
        return Ok(consultation);
    }

    // 按ID查询看诊详情
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var consultation = await _consultationService.GetByIdAsync(id);
        if (consultation == null) return NotFound();
        return Ok(consultation);
    }
}
```

### 3. 中医四诊合参记录(望闻问切)
```csharp
// Consultation实体的中医四诊字段
public class Consultation : BaseEntity
{
    // 中医四诊
    [StringLength(500)]
    public string? Inspection { get; set; }              // 望诊（观察面色、舌象、形体等）

    [StringLength(500)]
    public string? AuscultationOlfaction { get; set; }   // 闻诊（听声音、嗅气味）

    [StringLength(500)]
    public string? Inquiry { get; set; }                 // 问诊（询问症状、病史）

    [StringLength(500)]
    public string? Palpation { get; set; }               // 切诊（脉诊、按诊等）

    // 中医诊断结果
    [StringLength(500)]
    public string? TCMDiagnosis { get; set; }            // 中医辨证（如:肝郁脾虚证）

    [StringLength(500)]
    public string? TreatmentPrinciple { get; set; }      // 治疗原则（如:疏肝健脾）

    [StringLength(1000)]
    public string? MedicalAdvice { get; set; }           // 医嘱（饮食、起居建议）
}

// 在Service层完整记录四诊信息
public async Task RecordFourDiagnosesAsync(
    Guid consultationId,
    string inspection,
    string auscultationOlfaction,
    string inquiry,
    string palpation)
{
    var consultation = await _repository.GetByIdAsync(consultationId);
    if (consultation == null) throw new NotFoundException("看诊记录不存在");

    // 记录四诊信息
    consultation.Inspection = inspection;
    consultation.AuscultationOlfaction = auscultationOlfaction;
    consultation.Inquiry = inquiry;
    consultation.Palpation = palpation;

    await _repository.UpdateAsync(consultation);
}
```

### 4. 三步工作流(辩证→施治→总结)
```csharp
// 三步工作流时间戳字段
public class Consultation : BaseEntity
{
    // Issue #1598: REQ-001 - 三步工作流优化状态字段

    [DisplayName("Step1完成时间")]
    public DateTime? Step1CompletedAt { get; set; }  // 辩证完成

    [DisplayName("Step2完成时间")]
    public DateTime? Step2CompletedAt { get; set; }  // 施治完成

    [DisplayName("Step3完成时间")]
    public DateTime? Step3CompletedAt { get; set; }  // 总结完成

    [DisplayName("处方开关")]
    public bool PrescriptionEnabled { get; set; } = true;  // 控制是否开处方
}

// Step1: 完成辩证(记录四诊、诊断、治则)
public async Task CompleteStep1Async(
    Guid consultationId,
    string tcmDiagnosis,
    string treatmentPrinciple)
{
    var consultation = await _repository.GetByIdAsync(consultationId);
    if (consultation == null) throw new NotFoundException("看诊记录不存在");

    // 验证四诊信息已记录
    if (string.IsNullOrWhiteSpace(consultation.Inspection) &&
        string.IsNullOrWhiteSpace(consultation.Inquiry) &&
        string.IsNullOrWhiteSpace(consultation.Palpation))
    {
        throw new ValidationException("请先完成四诊信息录入");
    }

    // 完成辩证
    consultation.TCMDiagnosis = tcmDiagnosis;
    consultation.TreatmentPrinciple = treatmentPrinciple;
    consultation.Step1CompletedAt = DateTime.Now;

    await _repository.UpdateAsync(consultation);
}

// Step2: 完成施治(开具处方或非药物治疗)
public async Task CompleteStep2Async(Guid consultationId)
{
    var consultation = await _repository.GetByIdAsync(consultationId);
    if (consultation == null) throw new NotFoundException("看诊记录不存在");

    // 验证Step1已完成
    if (!consultation.Step1CompletedAt.HasValue)
    {
        throw new ValidationException("请先完成辩证步骤");
    }

    // 完成施治
    consultation.Step2CompletedAt = DateTime.Now;
    await _repository.UpdateAsync(consultation);
}

// Step3: 完成总结(记录医嘱、注意事项)
public async Task CompleteStep3Async(
    Guid consultationId,
    string medicalAdvice)
{
    var consultation = await _repository.GetByIdAsync(consultationId);
    if (consultation == null) throw new NotFoundException("看诊记录不存在");

    // 验证Step2已完成
    if (!consultation.Step2CompletedAt.HasValue)
    {
        throw new ValidationException("请先完成施治步骤");
    }

    // 完成总结
    consultation.MedicalAdvice = medicalAdvice;
    consultation.Step3CompletedAt = DateTime.Now;

    await _repository.UpdateAsync(consultation);
}
```

### 5. 处方开关功能(灵活控制开方)
```csharp
// 处方开关控制
public class Consultation : BaseEntity
{
    [DisplayName("处方开关")]
    public bool PrescriptionEnabled { get; set; } = true;  // 默认开启
}

// 设置处方开关
public async Task SetPrescriptionEnabledAsync(
    Guid consultationId,
    bool enabled)
{
    var consultation = await _repository.GetByIdAsync(consultationId);
    if (consultation == null) throw new NotFoundException("看诊记录不存在");

    consultation.PrescriptionEnabled = enabled;
    await _repository.UpdateAsync(consultation);
}

// 在Prescription创建时检查开关
public async Task<PrescriptionDto> CreatePrescriptionAsync(
    Guid medicalCaseId,
    CreatePrescriptionDto dto)
{
    // 查询关联的Consultation
    var consultation = await _consultationRepository.GetByMedicalCaseIdAsync(medicalCaseId);
    if (consultation == null) throw new NotFoundException("看诊记录不存在");

    // 检查处方开关
    if (!consultation.PrescriptionEnabled)
    {
        throw new ValidationException("当前看诊不允许开具处方");
    }

    // 创建处方...
}
```

### 6. 患者就诊历史查询(支持复诊)
```csharp
public class ConsultationRepository : BaseRepository<Consultation>, IConsultationRepository
{
    // 按患者ID查询所有就诊历史
    public async Task<List<Consultation>> GetByPatientIdAsync(Guid patientId)
    {
        // 通过MedicalCase关联查询患者的所有看诊记录
        return await _dbSet
            .Include(c => c.MedicalCase)           // 包含医案信息
                .ThenInclude(mc => mc.Patient)     // 包含患者信息
            .Where(c => c.MedicalCase.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)   // 按就诊时间倒序
            .ToListAsync();
    }

    // 分页查询看诊详情(含关联信息)
    public async Task<PagedResult<Consultation>> GetPagedWithDetailsAsync(
        int pageIndex,
        int pageSize,
        Guid? patientId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _dbSet
            .Include(c => c.MedicalCase)
                .ThenInclude(mc => mc.Patient)
            .AsQueryable();

        // 按患者筛选
        if (patientId.HasValue)
        {
            query = query.Where(c => c.MedicalCase.PatientId == patientId.Value);
        }

        // 按日期范围筛选
        if (startDate.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= endDate.Value);
        }

        // 分页查询
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Consultation>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}

// 在Controller中使用
[HttpGet("patient/{patientId}/history")]
public async Task<IActionResult> GetPatientHistory(Guid patientId)
{
    var consultations = await _repository.GetByPatientIdAsync(patientId);
    var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
    return Ok(dtos);
}
```

### 7. 共享主键与MedicalCase一对一关系
```csharp
// Consultation与MedicalCase共享主键
public class Consultation : BaseEntity
{
    // Id字段与MedicalCase共享主键
    // 通过EF Core配置建立一对一关系

    /// <summary>
    /// 所属医疗案例（必需的，通过共享主键关联）
    /// </summary>
    [Required]
    public virtual MedicalCase MedicalCase { get; set; } = null!;
}

// EF Core配置(在AppDbContext中)
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 配置Consultation与MedicalCase的一对一关系
    modelBuilder.Entity<Consultation>()
        .HasOne(c => c.MedicalCase)
        .WithOne(mc => mc.Consultation)
        .HasForeignKey<Consultation>(c => c.Id)  // 共享主键
        .OnDelete(DeleteBehavior.Cascade);       // 级联删除
}

// 创建看诊时自动生成共享ID
public async Task<MedicalCaseDto> CreateMedicalCaseWithConsultationAsync(
    CreateMedicalCaseDto dto)
{
    // 创建医案(生成主键ID)
    var medicalCase = new MedicalCase
    {
        Id = Guid.NewGuid(),  // 生成主键
        PatientId = dto.PatientId,
        DoctorId = dto.DoctorId,
        Status = MedicalCaseStatus.InProgress
    };

    // 创建看诊(使用相同主键)
    var consultation = new Consultation
    {
        Id = medicalCase.Id,  // ⚠️ 共享主键
        ChiefComplaint = dto.ChiefComplaint,
        PresentIllness = dto.PresentIllness,
        PrescriptionEnabled = true
    };

    medicalCase.Consultation = consultation;

    await _medicalCaseRepository.AddAsync(medicalCase);
    return _mapper.Map<MedicalCaseDto>(medicalCase);
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `ConsultationsController` 对外暴露。

- **API路由前缀**: `/api/v1/consultations`

**主要端点**:
- `GET /api/v1/consultations/{id}` - 按ID查询看诊详情
- `GET /api/v1/consultations/medical-case/{medicalCaseId}` - 按医案ID查询看诊
- `GET /api/v1/consultations/patient/{patientId}/history` - 查询患者就诊历史
- `GET /api/v1/consultations` - 分页查询看诊记录(支持患者、日期筛选)
- `POST /api/v1/consultations` - 创建看诊记录(通常由MedicalCase创建)
- `PUT /api/v1/consultations/{id}` - 更新看诊记录(四诊、诊断)
- `PUT /api/v1/consultations/{id}/step1` - 完成Step1(辩证)
- `PUT /api/v1/consultations/{id}/step2` - 完成Step2(施治)
- `PUT /api/v1/consultations/{id}/step3` - 完成Step3(总结)
- `PUT /api/v1/consultations/{id}/prescription-enabled` - 设置处方开关

**完整API定义**请参考 `IConsultationService` 接口和 `ConsultationsController` 的实现。

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/consultation/](../../../../docs/reference/modules/consultation/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/consultation-design.md](../../../../docs/explanation/architecture/server/consultation-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/consultation-development.md](../../../../docs/how-to-guides/server/consultation-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
