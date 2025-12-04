# 病案管理系统架构设计

## 1. 模块概述

### 1.1 业务定位

病案管理系统（MedicalCase Module）是LYBTZYZS系统的**核心业务聚合根模块**，负责管理中医诊疗的完整生命周期。该模块将患者信息、诊断记录（Consultation）和处方信息（Prescription）整合为统一的业务聚合体，确保诊疗数据的完整性和一致性。

### 1.2 核心职责

| 职责类别 | 具体职责 | 技术实现 |
|---------|---------|---------|
| **聚合根管理** | 作为MedicalCase聚合根，协调Consultation和Prescription子实体 | Aggregate Root Pattern |
| **三步流程控制** | 强制执行"辨证 → 处方标记 → 开处方/完成"的业务流程 | Workflow State Machine |
| **数据一致性** | 确保病案、诊断、处方数据的事务一致性 | Repository Transaction |
| **业务规则验证** | 执行一诊一方、单患者单Active病案等业务约束 | Domain Service Validation |
| **生命周期管理** | 管理病案从创建到归档的完整状态转换 | Status Enum + Business Rules |

### 1.3 设计原则

**聚合根模式（AR-001）**
```csharp
// ✅ 正确：通过聚合根访问子实体
var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
medicalCase.Consultation.TCMDiagnosis = "肝郁脾虚";
await _repository.UpdateAsync(medicalCase);

// ❌ 错误：直接操作子实体（违反聚合根约束）
var consultation = await _consultationRepository.GetByIdAsync(consultationId);
consultation.TCMDiagnosis = "肝郁脾虚";
await _consultationRepository.UpdateAsync(consultation);
```

**一诊一方约束（AR-003）**
```csharp
// ✅ 正确：每个病案最多一个处方
public class MedicalCase : AggregateRoot<Guid>
{
    public virtual Prescription? Prescription { get; set; }  // 0..1关系
}

// ❌ 错误：一个病案对应多个处方（违反一诊一方约束）
public virtual ICollection<Prescription> Prescriptions { get; set; }  // 1:N关系（禁止）
```

### 1.4 关键指标

| 指标项 | 数值 | 说明 |
|-------|------|------|
| 实体数量 | 1个核心实体 | MedicalCase（聚合根） |
| 关联实体 | 2个 | Consultation (1:1), Prescription (0..1) |
| 业务规则 | 8条 | AR-001, AR-003, BF-002, BR-001等 |
| API端点数 | 16个 | 7个Write + 7个Read + 2个Helper |
| 状态枚举 | 4个 | Draft, Active, Completed, Cancelled |
| 三步流程 | 3个Step | 辨证 → 处方标记 → 开处方/完成 |

---

## 2. 三层架构设计

### 2.1 整体架构

```
┌──────────────────────────────────────────────────────────────┐
│                      Desktop Layer (Client)                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  MedicalCaseListView.xaml                              │  │
│  │  MedicalCaseListViewModel.cs                           │  │
│  │  MedicalCaseQueryService.cs (查询服务)                 │  │
│  │  MedicalCaseBusinessService.cs (业务服务)              │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                              ↓ HTTP (Refit)
┌──────────────────────────────────────────────────────────────┐
│                       Server Layer (API)                      │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  MedicalCaseController.cs                              │  │
│  │    ├─ Write操作（7个端点）                             │  │
│  │    │    POST /medicalcases                             │  │
│  │    │    PUT /medicalcases/{id}                         │  │
│  │    │    PUT /medicalcases/{id}/consultation            │  │
│  │    │    PUT /medicalcases/{id}/prescription-flag       │  │
│  │    │    POST /medicalcases/{id}/prescription           │  │
│  │    │    PUT /medicalcases/{id}/complete                │  │
│  │    │    DELETE /medicalcases/{id}                      │  │
│  │    ├─ Read操作（7个端点）                              │  │
│  │    │    GET /medicalcases                              │  │
│  │    │    GET /medicalcases/{id}                         │  │
│  │    │    GET /medicalcases/search                       │  │
│  │    │    GET /medicalcases/patient/{patientId}          │  │
│  │    │    GET /medicalcases/statistics                   │  │
│  │    └─ Helper操作（2个端点）                            │  │
│  │         GET /medicalcases/{id}/can-edit                │  │
│  │         GET /medicalcases/{id}/prescriptions/{id}/can-delete │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  MedicalCaseService.cs                                 │  │
│  │    ├─ CreateAsync() - 创建病案                         │  │
│  │    ├─ UpdateConsultationAsync() - 更新辨证信息         │  │
│  │    ├─ SetPrescriptionFlagAsync() - 标记处方需求        │  │
│  │    ├─ CreatePrescriptionAsync() - 创建处方             │  │
│  │    ├─ CompleteAsync() - 完成病案                       │  │
│  │    ├─ DeleteAsync() - 删除病案                         │  │
│  │    ├─ GetByIdAsync() - 查询单个病案                    │  │
│  │    ├─ GetAllAsync() - 查询所有病案                     │  │
│  │    └─ SearchAsync() - 搜索病案                         │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  MedicalCaseRepository.cs (IRepository<MedicalCase>)   │  │
│  │    ├─ GetByIdWithDetailsAsync() - 预加载导航属性       │  │
│  │    ├─ GetActiveByPatientIdAsync() - 患者Active病案     │  │
│  │    └─ SearchAsync() - 多条件搜索                       │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                              ↓ EF Core
┌──────────────────────────────────────────────────────────────┐
│                     Database Layer (SQL Server)               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Table: MedicalCases (病案表)                          │  │
│  │    PK: Id (Guid)                                       │  │
│  │    FK: PatientId → Patients.Id                         │  │
│  │    FK: DoctorId → Users.Id                             │  │
│  │    Fields: ConsultationDate, Status, NeedsPrescription │  │
│  ├────────────────────────────────────────────────────────┤  │
│  │  Table: Consultations (诊断表) - 1:1                   │  │
│  │    PK: Id (Guid)                                       │  │
│  │    FK: MedicalCaseId → MedicalCases.Id (Unique)        │  │
│  │    Fields: Inspection, Inquiry, Palpation, TCMDiagnosis│  │
│  ├────────────────────────────────────────────────────────┤  │
│  │  Table: Prescriptions (处方表) - 0..1                  │  │
│  │    PK: Id (Guid)                                       │  │
│  │    FK: MedicalCaseId → MedicalCases.Id (Unique, Nullable)│  │
│  │    Fields: Dosage, Usage, IsPrinted                    │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 Desktop Layer（Desktop-Led模式）

#### 2.2.1 查询服务（MedicalCaseQueryService）

```csharp
public class MedicalCaseQueryService : IMedicalCaseQueryService
{
    private readonly IMedicalCaseApi _api;

    // 查询单个病案（预加载Consultation和Prescription）
    public async Task<MedicalCaseItem?> GetByIdAsync(Guid id)
    {
        var response = await _api.GetByIdAsync(id);
        return response.Data != null ? new MedicalCaseItem(response.Data) : null;
    }

    // 查询患者的Active病案（用于BR-001验证）
    public async Task<MedicalCaseItem?> GetActiveByPatientIdAsync(Guid patientId)
    {
        var response = await _api.GetByPatientIdAsync(patientId);
        return response.Data?
            .Select(mc => new MedicalCaseItem(mc))
            .FirstOrDefault(mc => mc.IsActive);
    }

    // 分页查询病案列表
    public async Task<PagedResult<MedicalCaseItem>> GetPagedAsync(
        int pageIndex, int pageSize, MedicalCaseStatus? status = null)
    {
        var response = await _api.GetPagedAsync(pageIndex, pageSize, status);
        return new PagedResult<MedicalCaseItem>
        {
            Items = response.Data.Items.Select(mc => new MedicalCaseItem(mc)).ToList(),
            TotalCount = response.Data.TotalCount
        };
    }
}
```

#### 2.2.2 业务服务（MedicalCaseBusinessService）

```csharp
public class MedicalCaseBusinessService : IMedicalCaseBusinessService
{
    private readonly IMedicalCaseApi _api;
    private readonly IMedicalCaseQueryService _queryService;

    // Step 1: 更新辨证信息（三步流程第一步）
    public async Task<ApiResponse> UpdateConsultationAsync(
        Guid medicalCaseId, ConsultationInputDto dto)
    {
        // Desktop侧业务规则验证（BF-002）
        var medicalCase = await _queryService.GetByIdAsync(medicalCaseId);
        if (medicalCase?.IsActive != true)
            return ApiResponse.CreateFail("仅Active状态病案可编辑辨证信息");

        return await _api.UpdateConsultationAsync(medicalCaseId, dto);
    }

    // Step 2: 标记处方需求（三步流程第二步）
    public async Task<ApiResponse> SetPrescriptionFlagAsync(
        Guid medicalCaseId, bool needsPrescription)
    {
        // Desktop侧业务规则验证（BF-002）
        var medicalCase = await _queryService.GetByIdAsync(medicalCaseId);
        if (medicalCase?.Consultation == null)
            return ApiResponse.CreateFail("必须先完成辨证信息（Step 1）");

        return await _api.SetPrescriptionFlagAsync(medicalCaseId,
            new SetPrescriptionFlagDto { NeedsPrescription = needsPrescription });
    }

    // Step 3a: 创建处方（三步流程第三步选项A）
    public async Task<ApiResponse> CreatePrescriptionAsync(
        Guid medicalCaseId, PrescriptionInputDto dto)
    {
        // Desktop侧业务规则验证（BF-002 + AR-003）
        var medicalCase = await _queryService.GetByIdAsync(medicalCaseId);
        if (medicalCase?.NeedsPrescription != true)
            return ApiResponse.CreateFail("必须先标记需要处方（Step 2）");
        if (medicalCase.Prescription != null)
            return ApiResponse.CreateFail("已存在处方，违反一诊一方约束（AR-003）");

        return await _api.CreatePrescriptionAsync(medicalCaseId, dto);
    }

    // Step 3b: 完成病案（三步流程第三步选项B）
    public async Task<ApiResponse> CompleteAsync(Guid medicalCaseId)
    {
        // Desktop侧业务规则验证（BF-002）
        var medicalCase = await _queryService.GetByIdAsync(medicalCaseId);
        if (medicalCase?.NeedsPrescription == true && medicalCase.Prescription == null)
            return ApiResponse.CreateFail("已标记需要处方但未创建处方，请先创建处方或取消处方需求");

        return await _api.CompleteAsync(medicalCaseId);
    }
}
```

#### 2.2.3 视图模型（MedicalCaseListViewModel）

```csharp
public class MedicalCaseListViewModel : BindableBase
{
    private readonly IMedicalCaseQueryService _queryService;
    private readonly IMedicalCaseBusinessService _businessService;

    public ObservableCollection<MedicalCaseItem> MedicalCases { get; } = new();

    // 加载病案列表
    public async Task LoadMedicalCasesAsync()
    {
        var result = await _queryService.GetPagedAsync(1, 20, MedicalCaseStatus.Active);
        MedicalCases.Clear();
        foreach (var item in result.Items)
            MedicalCases.Add(item);
    }

    // 三步流程：Step 1更新辨证
    public async Task UpdateConsultationAsync(Guid medicalCaseId, ConsultationInputDto dto)
    {
        var response = await _businessService.UpdateConsultationAsync(medicalCaseId, dto);
        if (response.IsSuccess)
        {
            await LoadMedicalCasesAsync();  // 刷新列表
            MessageBox.Show("辨证信息更新成功（Step 1完成）");
        }
    }

    // 三步流程：Step 2标记处方需求
    public async Task SetPrescriptionFlagAsync(Guid medicalCaseId, bool needs)
    {
        var response = await _businessService.SetPrescriptionFlagAsync(medicalCaseId, needs);
        if (response.IsSuccess)
        {
            await LoadMedicalCasesAsync();
            MessageBox.Show($"处方需求已标记：{(needs ? "需要" : "不需要")}（Step 2完成）");
        }
    }
}
```

### 2.3 Server Layer

#### 2.3.1 Controller层（API端点）

**Write操作（7个端点）**
```csharp
[Route("api/v1/medicalcases")]
[ApiController]
public class MedicalCaseController : ControllerBase
{
    private readonly IMedicalCaseService _service;

    // Write-1: 创建病案
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> Create(
        [FromBody] CreateMedicalCaseDto dto)
    {
        // Server侧BR-001验证：单患者单Active病案
        var result = await _service.CreateAsync(dto);
        return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "病案创建成功"));
    }

    // Write-2: 更新病案基本信息
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> Update(
        Guid id, [FromBody] UpdateMedicalCaseDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "病案更新成功"));
    }

    // Write-3: 更新辨证信息（Step 1）
    [HttpPut("{id}/consultation")]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> UpdateConsultation(
        Guid id, [FromBody] ConsultationInputDto dto)
    {
        // Server侧BF-002验证：三步流程顺序
        var result = await _service.UpdateConsultationAsync(id, dto);
        return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "辨证信息更新成功"));
    }

    // Write-4: 标记处方需求（Step 2）
    [HttpPut("{id}/prescription-flag")]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> SetPrescriptionFlag(
        Guid id, [FromBody] SetPrescriptionFlagDto dto)
    {
        // Server侧BF-002验证：必须先完成Step 1
        var result = await _service.SetPrescriptionFlagAsync(id, dto.NeedsPrescription);
        return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "处方需求已标记"));
    }

    // Write-5: 创建处方（Step 3a）
    [HttpPost("{id}/prescription")]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> CreatePrescription(
        Guid id, [FromBody] PrescriptionInputDto dto)
    {
        // Server侧AR-003 + BF-002验证：一诊一方 + 必须先完成Step 2
        var result = await _service.CreatePrescriptionAsync(id, dto);
        return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "处方创建成功"));
    }

    // Write-6: 完成病案（Step 3b）
    [HttpPut("{id}/complete")]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> Complete(Guid id)
    {
        // Server侧BF-002验证：三步流程完整性
        var result = await _service.CompleteAsync(id);
        return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "病案已完成"));
    }

    // Write-7: 删除病案（软删除）
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse.CreateSuccess("病案删除成功"));
    }
}
```

**Read操作（7个端点）**
```csharp
// Read-1: 查询单个病案（预加载Consultation + Prescription）
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<MedicalCase>>> GetById(Guid id)
{
    var result = await _service.GetByIdAsync(id);
    if (result == null)
        return NotFound(ApiResponse<MedicalCase>.CreateFail("病案不存在"));
    return Ok(ApiResponse<MedicalCase>.CreateSuccess(result, "查询成功"));
}

// Read-2: 分页查询病案列表
[HttpGet]
public async Task<ActionResult<ApiResponse<PagedResult<MedicalCase>>>> GetPaged(
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] MedicalCaseStatus? status = null)
{
    var result = await _service.GetPagedAsync(pageIndex, pageSize, status);
    return Ok(ApiResponse<PagedResult<MedicalCase>>.CreateSuccess(result, "查询成功"));
}

// Read-3: 搜索病案（多条件）
[HttpGet("search")]
public async Task<ActionResult<ApiResponse<List<MedicalCase>>>> Search(
    [FromQuery] string? patientName,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate)
{
    var result = await _service.SearchAsync(patientName, startDate, endDate);
    return Ok(ApiResponse<List<MedicalCase>>.CreateSuccess(result, "搜索成功"));
}

// Read-4: 查询患者的所有病案
[HttpGet("patient/{patientId}")]
public async Task<ActionResult<ApiResponse<List<MedicalCase>>>> GetByPatientId(Guid patientId)
{
    var result = await _service.GetByPatientIdAsync(patientId);
    return Ok(ApiResponse<List<MedicalCase>>.CreateSuccess(result, "查询成功"));
}
```

#### 2.3.2 Service层（业务逻辑）

```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IRepository<MedicalCase> _repository;
    private readonly IRepository<Consultation> _consultationRepository;
    private readonly IRepository<Prescription> _prescriptionRepository;

    // 创建病案（BR-001验证：单患者单Active病案）
    public async Task<MedicalCase> CreateAsync(CreateMedicalCaseDto dto)
    {
        // BR-001验证
        var activeCase = await _repository.GetActiveByPatientIdAsync(dto.PatientId);
        if (activeCase != null)
            throw new BusinessRuleException("BR-001",
                $"患者已存在Active病案（Id: {activeCase.Id}），请先完成或取消该病案");

        var medicalCase = new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            ConsultationDate = dto.ConsultationDate,
            Status = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(medicalCase);
        return medicalCase;
    }

    // 更新辨证信息（BF-002验证：三步流程Step 1）
    public async Task<MedicalCase> UpdateConsultationAsync(
        Guid medicalCaseId, ConsultationInputDto dto)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            throw new NotFoundException("病案不存在");

        // BF-002验证：仅Active状态可编辑
        if (medicalCase.Status != MedicalCaseStatus.Active)
            throw new BusinessRuleException("BF-002",
                $"病案状态为{medicalCase.Status}，仅Active状态可编辑辨证信息");

        // AR-001：通过聚合根操作子实体
        if (medicalCase.Consultation == null)
        {
            medicalCase.Consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId
            };
        }

        medicalCase.Consultation.Inspection = dto.Inspection;
        medicalCase.Consultation.AuscultationOlfaction = dto.AuscultationOlfaction;
        medicalCase.Consultation.Inquiry = dto.Inquiry;
        medicalCase.Consultation.Palpation = dto.Palpation;
        medicalCase.Consultation.TCMDiagnosis = dto.TCMDiagnosis;
        medicalCase.Consultation.TreatmentPrinciple = dto.TreatmentPrinciple;

        await _repository.UpdateAsync(medicalCase);
        return medicalCase;
    }

    // 标记处方需求（BF-002验证：三步流程Step 2）
    public async Task<MedicalCase> SetPrescriptionFlagAsync(
        Guid medicalCaseId, bool needsPrescription)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            throw new NotFoundException("病案不存在");

        // BF-002验证：必须先完成Step 1
        if (medicalCase.Consultation == null)
            throw new BusinessRuleException("BF-002",
                "必须先完成辨证信息（Step 1）才能标记处方需求");

        medicalCase.NeedsPrescription = needsPrescription;
        await _repository.UpdateAsync(medicalCase);
        return medicalCase;
    }

    // 创建处方（AR-003 + BF-002验证：一诊一方 + 三步流程Step 3a）
    public async Task<MedicalCase> CreatePrescriptionAsync(
        Guid medicalCaseId, PrescriptionInputDto dto)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            throw new NotFoundException("病案不存在");

        // BF-002验证：必须先完成Step 2
        if (medicalCase.NeedsPrescription != true)
            throw new BusinessRuleException("BF-002",
                "必须先标记需要处方（Step 2）才能创建处方");

        // AR-003验证：一诊一方
        if (medicalCase.Prescription != null)
            throw new BusinessRuleException("AR-003",
                "已存在处方，违反一诊一方约束");

        // AR-001：通过聚合根操作子实体
        medicalCase.Prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = medicalCaseId,
            Dosage = dto.Dosage,
            Usage = dto.Usage,
            IsPrinted = false
        };

        await _repository.UpdateAsync(medicalCase);
        return medicalCase;
    }

    // 完成病案（BF-002验证：三步流程完整性）
    public async Task<MedicalCase> CompleteAsync(Guid medicalCaseId)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            throw new NotFoundException("病案不存在");

        // BF-002验证：三步流程完整性
        if (medicalCase.NeedsPrescription == true && medicalCase.Prescription == null)
            throw new BusinessRuleException("BF-002",
                "已标记需要处方但未创建处方，请先创建处方或取消处方需求");

        medicalCase.Status = MedicalCaseStatus.Completed;
        medicalCase.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(medicalCase);
        return medicalCase;
    }
}
```

#### 2.3.3 Repository层（数据访问）

```csharp
public class MedicalCaseRepository : Repository<MedicalCase>, IRepository<MedicalCase>
{
    public MedicalCaseRepository(ApplicationDbContext context) : base(context) { }

    // 预加载Consultation和Prescription导航属性
    public async Task<MedicalCase?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(mc => mc.Consultation)
                .ThenInclude(c => c.TongueAnalysis)
            .Include(mc => mc.Consultation)
                .ThenInclude(c => c.PulseAnalysis)
            .Include(mc => mc.Consultation)
                .ThenInclude(c => c.SyndromeAnalysis)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p.PrescriptionItems)
            .Include(mc => mc.Patient)
            .Include(mc => mc.Doctor)
            .FirstOrDefaultAsync(mc => mc.Id == id && !mc.IsDeleted);
    }

    // 查询患者的Active病案（BR-001验证）
    public async Task<MedicalCase?> GetActiveByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(mc =>
                mc.PatientId == patientId &&
                mc.Status == MedicalCaseStatus.Active &&
                !mc.IsDeleted);
    }

    // 查询患者的所有病案
    public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
            .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
            .OrderByDescending(mc => mc.ConsultationDate)
            .ToListAsync();
    }
}
```

### 2.4 Database Layer

#### 2.4.1 表结构设计

**MedicalCases表（聚合根）**
```sql
CREATE TABLE [dbo].[MedicalCases]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [PatientId] UNIQUEIDENTIFIER NOT NULL,
    [DoctorId] UNIQUEIDENTIFIER NOT NULL,
    [ConsultationDate] DATETIME2 NOT NULL,
    [Status] INT NOT NULL DEFAULT 1,  -- 0=Draft, 1=Active, 2=Completed, 3=Cancelled
    [NeedsPrescription] BIT NULL,     -- NULL=未标记, TRUE=需要, FALSE=不需要
    [Remark] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,

    CONSTRAINT [FK_MedicalCases_Patients] FOREIGN KEY ([PatientId])
        REFERENCES [Patients]([Id]),
    CONSTRAINT [FK_MedicalCases_Users] FOREIGN KEY ([DoctorId])
        REFERENCES [Users]([Id]),

    INDEX [IX_MedicalCases_PatientId_Status] ([PatientId], [Status])
        INCLUDE ([ConsultationDate])
        WHERE [IsDeleted] = 0,  -- BR-001查询优化
    INDEX [IX_MedicalCases_DoctorId] ([DoctorId])
        WHERE [IsDeleted] = 0,
    INDEX [IX_MedicalCases_ConsultationDate] ([ConsultationDate] DESC)
        WHERE [IsDeleted] = 0
)
```

**Consultations表（1:1关联）**
```sql
CREATE TABLE [dbo].[Consultations]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [MedicalCaseId] UNIQUEIDENTIFIER NOT NULL UNIQUE,  -- UNIQUE约束：1:1关系
    [Inspection] NVARCHAR(1000) NULL,           -- 望诊
    [AuscultationOlfaction] NVARCHAR(1000) NULL,  -- 闻诊
    [Inquiry] NVARCHAR(2000) NULL,              -- 问诊
    [Palpation] NVARCHAR(1000) NULL,            -- 切诊
    [TCMDiagnosis] NVARCHAR(500) NULL,          -- 中医诊断
    [TreatmentPrinciple] NVARCHAR(500) NULL,    -- 治疗原则
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,

    CONSTRAINT [FK_Consultations_MedicalCases] FOREIGN KEY ([MedicalCaseId])
        REFERENCES [MedicalCases]([Id]) ON DELETE CASCADE
)
```

**Prescriptions表（0..1关联）**
```sql
CREATE TABLE [dbo].[Prescriptions]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [MedicalCaseId] UNIQUEIDENTIFIER NOT NULL UNIQUE,  -- UNIQUE约束：0..1关系 + AR-003
    [Dosage] INT NOT NULL,                      -- 剂数
    [Usage] NVARCHAR(500) NULL,                 -- 用法
    [IsPrinted] BIT NOT NULL DEFAULT 0,         -- 是否已打印
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,

    CONSTRAINT [FK_Prescriptions_MedicalCases] FOREIGN KEY ([MedicalCaseId])
        REFERENCES [MedicalCases]([Id]) ON DELETE CASCADE
)
```

#### 2.4.2 EF Core实体配置

```csharp
public class MedicalCaseConfiguration : IEntityTypeConfiguration<MedicalCase>
{
    public void Configure(EntityTypeBuilder<MedicalCase> builder)
    {
        builder.ToTable("MedicalCases");
        builder.HasKey(mc => mc.Id);

        // 1:1关系配置（Consultation）
        builder.HasOne(mc => mc.Consultation)
            .WithOne()
            .HasForeignKey<Consultation>(c => c.MedicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // 0..1关系配置（Prescription） - AR-003一诊一方约束
        builder.HasOne(mc => mc.Prescription)
            .WithOne()
            .HasForeignKey<Prescription>(p => p.MedicalCaseId)
            .IsRequired(false)  // 可选关系
            .OnDelete(DeleteBehavior.Cascade);

        // N:1关系配置（Patient）
        builder.HasOne(mc => mc.Patient)
            .WithMany()
            .HasForeignKey(mc => mc.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // N:1关系配置（Doctor）
        builder.HasOne(mc => mc.Doctor)
            .WithMany()
            .HasForeignKey(mc => mc.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // 全局查询过滤器（软删除）
        builder.HasQueryFilter(mc => !mc.IsDeleted);
    }
}
```

---

## 3. 核心领域模型

### 3.1 MedicalCase聚合根

```csharp
/// <summary>
/// 病案聚合根 - 管理诊疗的完整生命周期
/// AR-001: 所有Consultation和Prescription操作必须通过MedicalCase聚合根
/// </summary>
public class MedicalCase : AggregateRoot<Guid>
{
    // === 基本信息 ===
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ConsultationDate { get; set; }

    // === 业务状态 ===
    public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Active;
    public bool? NeedsPrescription { get; set; }  // NULL=未标记, TRUE=需要, FALSE=不需要
    public string? Remark { get; set; }

    // === 聚合关系 ===
    // 1:1关系 - 辨证信息（必须）
    public virtual Consultation? Consultation { get; set; }
    // 0..1关系 - 处方信息（可选，AR-003一诊一方约束）
    public virtual Prescription? Prescription { get; set; }

    // N:1关系 - 外部引用
    public virtual Patient Patient { get; set; } = null!;
    public virtual User Doctor { get; set; } = null!;

    // === 计算属性 ===
    public bool IsActive => Status == MedicalCaseStatus.Active;
    public bool IsCompleted => Status == MedicalCaseStatus.Completed;
    public bool CanEdit => Status == MedicalCaseStatus.Active;
    public bool CanDelete => Prescription?.IsPrinted != true;

    // === 业务方法 ===

    /// <summary>
    /// 验证是否可以创建处方（AR-003 + BF-002）
    /// </summary>
    public bool CanCreatePrescription(out string? errorMessage)
    {
        if (NeedsPrescription != true)
        {
            errorMessage = "必须先标记需要处方（Step 2）";
            return false;
        }

        if (Prescription != null)
        {
            errorMessage = "已存在处方，违反一诊一方约束（AR-003）";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// 验证是否可以完成病案（BF-002）
    /// </summary>
    public bool CanComplete(out string? errorMessage)
    {
        if (NeedsPrescription == true && Prescription == null)
        {
            errorMessage = "已标记需要处方但未创建处方";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
```

### 3.2 MedicalCaseStatus枚举

```csharp
/// <summary>
/// 病案状态枚举
/// </summary>
public enum MedicalCaseStatus
{
    /// <summary>
    /// 草稿 - 已创建但未开始诊断
    /// </summary>
    [Description("草稿")]
    Draft = 0,

    /// <summary>
    /// 活动 - 正在诊断中（默认状态）
    /// BR-001: 每个患者同时只能有一个Active病案
    /// </summary>
    [Description("活动")]
    Active = 1,

    /// <summary>
    /// 已完成 - 诊断和处方已完成
    /// </summary>
    [Description("已完成")]
    Completed = 2,

    /// <summary>
    /// 已取消 - 病案已取消
    /// </summary>
    [Description("已取消")]
    Cancelled = 3
}
```

### 3.3 Consultation实体（子实体）

```csharp
/// <summary>
/// 诊断记录 - MedicalCase的子实体（1:1关系）
/// AR-001: 只能通过MedicalCase聚合根访问和修改
/// </summary>
public class Consultation : BaseEntity
{
    public Guid MedicalCaseId { get; set; }  // 外键（UNIQUE约束）

    // === 四诊信息 ===
    public string? Inspection { get; set; }           // 望诊
    public string? AuscultationOlfaction { get; set; }  // 闻诊
    public string? Inquiry { get; set; }              // 问诊
    public string? Palpation { get; set; }            // 切诊

    // === 诊断结论 ===
    public string? TCMDiagnosis { get; set; }         // 中医诊断
    public string? TreatmentPrinciple { get; set; }   // 治疗原则

    // === 导航属性 ===
    public virtual TongueAnalysis? TongueAnalysis { get; set; }      // 舌诊分析
    public virtual PulseAnalysis? PulseAnalysis { get; set; }        // 脉诊分析
    public virtual SyndromeAnalysis? SyndromeAnalysis { get; set; }  // 辨证分析
}
```

### 3.4 Prescription实体（子实体）

```csharp
/// <summary>
/// 处方记录 - MedicalCase的子实体（0..1关系）
/// AR-001: 只能通过MedicalCase聚合根访问和修改
/// AR-003: 一个病案最多一个处方（一诊一方约束）
/// </summary>
public class Prescription : BaseEntity
{
    public Guid MedicalCaseId { get; set; }  // 外键（UNIQUE约束 + AR-003）

    // === 处方信息 ===
    public int Dosage { get; set; }        // 剂数
    public string? Usage { get; set; }     // 用法
    public bool IsPrinted { get; set; }    // 是否已打印

    // === 导航属性 ===
    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
```

### 3.5 DTO设计

**CreateMedicalCaseDto（创建病案）**
```csharp
public class CreateMedicalCaseDto
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "医生ID不能为空")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "诊疗日期不能为空")]
    public DateTime ConsultationDate { get; set; }

    public string? Remark { get; set; }
}
```

**ConsultationInputDto（辨证信息 - Step 1）**
```csharp
public class ConsultationInputDto
{
    public string? Inspection { get; set; }           // 望诊
    public string? AuscultationOlfaction { get; set; }  // 闻诊
    public string? Inquiry { get; set; }              // 问诊
    public string? Palpation { get; set; }            // 切诊
    public string? TCMDiagnosis { get; set; }         // 中医诊断
    public string? TreatmentPrinciple { get; set; }   // 治疗原则
}
```

**SetPrescriptionFlagDto（处方需求标记 - Step 2）**
```csharp
public class SetPrescriptionFlagDto
{
    [Required(ErrorMessage = "处方需求标记不能为空")]
    public bool NeedsPrescription { get; set; }  // TRUE=需要处方, FALSE=不需要处方
}
```

**PrescriptionInputDto（创建处方 - Step 3a）**
```csharp
public class PrescriptionInputDto
{
    [Required(ErrorMessage = "剂数不能为空")]
    [Range(1, 100, ErrorMessage = "剂数范围1-100")]
    public int Dosage { get; set; }

    public string? Usage { get; set; }

    [Required(ErrorMessage = "处方明细不能为空")]
    [MinLength(1, ErrorMessage = "至少需要一味中药")]
    public List<PrescriptionItemDto> Items { get; set; } = new();
}
```

---

## 4. 业务规则体系

### 4.1 聚合根约束（AR-001）

**规则定义**
```markdown
**规则代码**: AR-001
**规则名称**: 聚合根约束
**规则描述**: 所有Consultation和Prescription的创建、修改、删除操作必须通过MedicalCase聚合根进行
**违规后果**: 数据不一致、业务规则绕过
```

**正确示例**
```csharp
// ✅ 正确：通过聚合根更新辨证信息
var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
medicalCase.Consultation.TCMDiagnosis = "肝郁脾虚";
await _repository.UpdateAsync(medicalCase);

// ✅ 正确：通过聚合根创建处方
medicalCase.Prescription = new Prescription
{
    Id = Guid.NewGuid(),
    MedicalCaseId = medicalCase.Id,
    Dosage = 7
};
await _repository.UpdateAsync(medicalCase);
```

**错误示例**
```csharp
// ❌ 错误：直接操作子实体（违反AR-001）
var consultation = await _consultationRepository.GetByIdAsync(consultationId);
consultation.TCMDiagnosis = "肝郁脾虚";
await _consultationRepository.UpdateAsync(consultation);

// ❌ 错误：直接创建处方（违反AR-001）
var prescription = new Prescription
{
    Id = Guid.NewGuid(),
    MedicalCaseId = medicalCaseId,
    Dosage = 7
};
await _prescriptionRepository.AddAsync(prescription);
```

### 4.2 一诊一方约束（AR-003）

**规则定义**
```markdown
**规则代码**: AR-003
**规则名称**: 一诊一方约束
**规则描述**: 每个MedicalCase最多只能有一个Prescription（0..1关系）
**技术实现**:
  - 导航属性: `public virtual Prescription? Prescription { get; set; }`（单数）
  - 数据库约束: MedicalCaseId列添加UNIQUE约束
**违规后果**: 抛出BusinessRuleException，阻止第二个处方创建
```

**验证逻辑**
```csharp
public async Task<MedicalCase> CreatePrescriptionAsync(
    Guid medicalCaseId, PrescriptionInputDto dto)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // AR-003验证：一诊一方
    if (medicalCase.Prescription != null)
        throw new BusinessRuleException("AR-003",
            "已存在处方，违反一诊一方约束");

    // 创建处方
    medicalCase.Prescription = new Prescription { /* ... */ };
    await _repository.UpdateAsync(medicalCase);
    return medicalCase;
}
```

**数据库约束**
```sql
ALTER TABLE [Prescriptions]
ADD CONSTRAINT [UQ_Prescriptions_MedicalCaseId] UNIQUE ([MedicalCaseId])
```

### 4.3 三步流程验证（BF-002）

**规则定义**
```markdown
**规则代码**: BF-002
**规则名称**: 三步流程验证
**规则描述**: MedicalCase必须按照"辨证 → 处方标记 → 开处方/完成"的顺序执行
**流程步骤**:
  - Step 1: 更新辨证信息（UpdateConsultation）
  - Step 2: 标记处方需求（SetPrescriptionFlag）
  - Step 3a: 创建处方（CreatePrescription） OR
  - Step 3b: 完成病案（Complete）
**违规后果**: 抛出BusinessRuleException，阻止跳步操作
```

**流程图**
```
┌─────────────────────────────────────────────────────────────┐
│                      MedicalCase创建                         │
│                    Status = Active                           │
└─────────────────────────────────────────────────────────────┘
                              ↓
                    ┌─────────────────────┐
                    │  Step 1: 辨证信息     │
                    │  UpdateConsultation  │
                    │  (Consultation != null)│
                    └─────────────────────┘
                              ↓
                    ┌─────────────────────┐
                    │  Step 2: 处方标记     │
                    │  SetPrescriptionFlag │
                    │  (NeedsPrescription设置)│
                    └─────────────────────┘
                              ↓
                    ┌─────────────────────┐
                    │   NeedsPrescription? │
                    └─────────────────────┘
                      ↓ TRUE        ↓ FALSE
            ┌──────────────┐    ┌──────────────┐
            │ Step 3a: 创建处方│    │ Step 3b: 完成病案│
            │CreatePrescription│    │   Complete      │
            │(Prescription创建)│    │Status=Completed│
            └──────────────┘    └──────────────┘
                      ↓                ↓
                    ┌─────────────────────┐
                    │   Status = Completed │
                    └─────────────────────┘
```

**验证逻辑**
```csharp
// Step 2验证：必须先完成Step 1
public async Task<MedicalCase> SetPrescriptionFlagAsync(
    Guid medicalCaseId, bool needsPrescription)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // BF-002验证
    if (medicalCase.Consultation == null)
        throw new BusinessRuleException("BF-002",
            "必须先完成辨证信息（Step 1）才能标记处方需求");

    medicalCase.NeedsPrescription = needsPrescription;
    await _repository.UpdateAsync(medicalCase);
    return medicalCase;
}

// Step 3a验证：必须先完成Step 2
public async Task<MedicalCase> CreatePrescriptionAsync(
    Guid medicalCaseId, PrescriptionInputDto dto)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // BF-002验证
    if (medicalCase.NeedsPrescription != true)
        throw new BusinessRuleException("BF-002",
            "必须先标记需要处方（Step 2）才能创建处方");

    // 创建处方...
}

// Step 3b验证：三步流程完整性
public async Task<MedicalCase> CompleteAsync(Guid medicalCaseId)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // BF-002验证
    if (medicalCase.NeedsPrescription == true && medicalCase.Prescription == null)
        throw new BusinessRuleException("BF-002",
            "已标记需要处方但未创建处方，请先创建处方或取消处方需求");

    medicalCase.Status = MedicalCaseStatus.Completed;
    await _repository.UpdateAsync(medicalCase);
    return medicalCase;
}
```

### 4.4 单患者单Active病案（BR-001）

**规则定义**
```markdown
**规则代码**: BR-001
**规则名称**: 单患者单Active病案
**规则描述**: 每个患者同时只能有一个Status=Active的病案
**业务理由**: 避免诊断混乱，确保医生专注于当前诊疗
**违规后果**: 抛出BusinessRuleException，阻止创建新Active病案
```

**验证逻辑**
```csharp
public async Task<MedicalCase> CreateAsync(CreateMedicalCaseDto dto)
{
    // BR-001验证
    var activeCase = await _repository.GetActiveByPatientIdAsync(dto.PatientId);
    if (activeCase != null)
        throw new BusinessRuleException("BR-001",
            $"患者已存在Active病案（Id: {activeCase.Id}），请先完成或取消该病案");

    var medicalCase = new MedicalCase
    {
        Id = Guid.NewGuid(),
        PatientId = dto.PatientId,
        Status = MedicalCaseStatus.Active  // 新病案默认Active
    };

    await _repository.AddAsync(medicalCase);
    return medicalCase;
}
```

**数据库索引优化**
```sql
CREATE INDEX [IX_MedicalCases_PatientId_Status]
ON [MedicalCases] ([PatientId], [Status])
INCLUDE ([ConsultationDate])
WHERE [IsDeleted] = 0;
```

### 4.5 其他业务规则

**BR-002: 软删除约束**
```csharp
// 病案删除仅标记IsDeleted=true，不物理删除
public async Task DeleteAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdAsync(id);
    if (medicalCase == null)
        throw new NotFoundException("病案不存在");

    medicalCase.IsDeleted = true;
    medicalCase.UpdatedAt = DateTime.UtcNow;
    await _repository.UpdateAsync(medicalCase);
}
```

**BR-003: 处方打印后不可删除**
```csharp
public async Task DeletePrescriptionAsync(Guid medicalCaseId)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // BR-003验证
    if (medicalCase.Prescription?.IsPrinted == true)
        throw new BusinessRuleException("BR-003",
            "处方已打印，不允许删除");

    medicalCase.Prescription = null;
    await _repository.UpdateAsync(medicalCase);
}
```

---

## 5. 数据流与交互

### 5.1 完整诊疗流程数据流

```
┌─────────────────────────────────────────────────────────────┐
│                   Desktop客户端                              │
└─────────────────────────────────────────────────────────────┘
        │
        │ 1. 创建病案
        ↓
┌─────────────────────────────────────────────────────────────┐
│  POST /api/v1/medicalcases                                  │
│  Body: { PatientId, DoctorId, ConsultationDate }            │
└─────────────────────────────────────────────────────────────┘
        │
        ↓ MedicalCaseService.CreateAsync()
        │
        ↓ BR-001验证：GetActiveByPatientIdAsync(PatientId)
        │
        ↓ 创建MedicalCase实体（Status=Active）
        │
        ↓ Repository.AddAsync()
        │
        ↓ Response: MedicalCase (Id, Status=Active)
        │
        │ 2. 更新辨证信息（Step 1）
        ↓
┌─────────────────────────────────────────────────────────────┐
│  PUT /api/v1/medicalcases/{id}/consultation                 │
│  Body: { Inspection, Inquiry, Palpation, TCMDiagnosis }     │
└─────────────────────────────────────────────────────────────┘
        │
        ↓ MedicalCaseService.UpdateConsultationAsync()
        │
        ↓ BF-002验证：Status == Active
        │
        ↓ AR-001：通过聚合根操作Consultation子实体
        │  medicalCase.Consultation = new Consultation { ... }
        │
        ↓ Repository.UpdateAsync()
        │
        ↓ Response: MedicalCase (Consultation != null)
        │
        │ 3. 标记处方需求（Step 2）
        ↓
┌─────────────────────────────────────────────────────────────┐
│  PUT /api/v1/medicalcases/{id}/prescription-flag            │
│  Body: { NeedsPrescription: true }                          │
└─────────────────────────────────────────────────────────────┘
        │
        ↓ MedicalCaseService.SetPrescriptionFlagAsync()
        │
        ↓ BF-002验证：Consultation != null
        │
        ↓ 设置NeedsPrescription = true
        │
        ↓ Repository.UpdateAsync()
        │
        ↓ Response: MedicalCase (NeedsPrescription = true)
        │
        │ 4. 创建处方（Step 3a）
        ↓
┌─────────────────────────────────────────────────────────────┐
│  POST /api/v1/medicalcases/{id}/prescription                │
│  Body: { Dosage, Usage, Items: [...] }                      │
└─────────────────────────────────────────────────────────────┘
        │
        ↓ MedicalCaseService.CreatePrescriptionAsync()
        │
        ↓ BF-002验证：NeedsPrescription == true
        │
        ↓ AR-003验证：Prescription == null
        │
        ↓ AR-001：通过聚合根操作Prescription子实体
        │  medicalCase.Prescription = new Prescription { ... }
        │
        ↓ Repository.UpdateAsync()
        │
        ↓ Response: MedicalCase (Prescription != null)
        │
        │ 5. 完成病案（自动）
        ↓
┌─────────────────────────────────────────────────────────────┐
│  PUT /api/v1/medicalcases/{id}/complete                     │
└─────────────────────────────────────────────────────────────┘
        │
        ↓ MedicalCaseService.CompleteAsync()
        │
        ↓ BF-002验证：三步流程完整性
        │  IF NeedsPrescription == true THEN Prescription != null
        │
        ↓ 设置Status = Completed
        │
        ↓ Repository.UpdateAsync()
        │
        ↓ Response: MedicalCase (Status = Completed)
```

### 5.2 查询流程数据流

**场景1: 查询单个病案详情**
```
Desktop客户端
    │
    │ GET /api/v1/medicalcases/{id}
    ↓
MedicalCaseController.GetById()
    │
    ↓ MedicalCaseService.GetByIdAsync()
    │
    ↓ Repository.GetByIdWithDetailsAsync()
    │   - Include Consultation
    │   - Include Consultation.TongueAnalysis
    │   - Include Consultation.PulseAnalysis
    │   - Include Prescription
    │   - Include Prescription.PrescriptionItems
    │   - Include Patient
    │   - Include Doctor
    │
    ↓ EF Core生成SQL JOIN查询
    │
    ↓ 返回完整MedicalCase实体（预加载所有导航属性）
    │
    ↓ Response: MedicalCase实体（包含所有子实体）
```

**场景2: 查询患者Active病案（BR-001验证）**
```
Desktop客户端
    │
    │ 创建新病案前查询
    ↓
MedicalCaseService.CreateAsync()
    │
    ↓ Repository.GetActiveByPatientIdAsync(patientId)
    │   WHERE PatientId = @patientId
    │   AND Status = 1 (Active)
    │   AND IsDeleted = 0
    │
    ↓ 返回null OR MedicalCase实体
    │
    ↓ IF 存在Active病案 THEN 抛出BR-001异常
```

### 5.3 模块间交互

**MedicalCase ↔ Patient**
```csharp
// MedicalCase需要Patient信息（单向依赖）
public class MedicalCase
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;
}

// 查询时预加载Patient
var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
Console.WriteLine($"患者: {medicalCase.Patient.Name}");
```

**MedicalCase ↔ Consultation（聚合关系）**
```csharp
// AR-001: Consultation必须通过MedicalCase访问
medicalCase.Consultation = new Consultation
{
    Id = Guid.NewGuid(),
    MedicalCaseId = medicalCase.Id,
    TCMDiagnosis = "肝郁脾虚"
};
await _repository.UpdateAsync(medicalCase);  // 级联保存Consultation
```

**MedicalCase ↔ Prescription（聚合关系 + AR-003）**
```csharp
// AR-003: 一诊一方约束 + AR-001: 通过聚合根操作
if (medicalCase.Prescription != null)
    throw new BusinessRuleException("AR-003", "已存在处方");

medicalCase.Prescription = new Prescription
{
    Id = Guid.NewGuid(),
    MedicalCaseId = medicalCase.Id,
    Dosage = 7
};
await _repository.UpdateAsync(medicalCase);  // 级联保存Prescription
```

---

## 6. 技术决策

### 6.1 聚合根模式（ADR-006）

**决策内容**: MedicalCase采用Aggregate Root模式管理Consultation和Prescription

**技术理由**:
1. **事务边界清晰**: 一次数据库事务完成聚合内所有操作
2. **数据一致性**: 避免Consultation和Prescription脱离MedicalCase独立修改
3. **业务规则集中**: 所有业务规则在MedicalCase聚合根统一验证

**实现方式**:
```csharp
// ✅ 正确：通过聚合根统一操作
var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
medicalCase.Consultation.TCMDiagnosis = "肝郁脾虚";
medicalCase.Prescription = new Prescription { Dosage = 7 };
await _repository.UpdateAsync(medicalCase);  // 单次事务提交

// ❌ 错误：分散操作子实体（违反聚合根模式）
var consultation = await _consultationRepo.GetByIdAsync(consultationId);
consultation.TCMDiagnosis = "肝郁脾虚";
await _consultationRepo.UpdateAsync(consultation);  // 事务1

var prescription = new Prescription { MedicalCaseId = id, Dosage = 7 };
await _prescriptionRepo.AddAsync(prescription);  // 事务2（可能导致不一致）
```

### 6.2 三步流程强制执行（BF-002）

**决策内容**: 通过状态机强制执行"辨证 → 处方标记 → 开处方/完成"流程

**业务理由**:
1. **规范诊疗流程**: 确保医生按标准流程完成诊断
2. **数据完整性**: 避免跳步导致数据缺失（如未辨证就开处方）
3. **审计追溯**: 清晰记录诊疗流程每一步

**实现方式**:
```csharp
// Step 2验证：必须先完成Step 1
if (medicalCase.Consultation == null)
    throw new BusinessRuleException("BF-002", "必须先完成辨证信息");

// Step 3a验证：必须先完成Step 2
if (medicalCase.NeedsPrescription != true)
    throw new BusinessRuleException("BF-002", "必须先标记需要处方");

// Step 3b验证：三步流程完整性
if (medicalCase.NeedsPrescription == true && medicalCase.Prescription == null)
    throw new BusinessRuleException("BF-002", "已标记需要处方但未创建");
```

### 6.3 Desktop-Led查询优化

**决策内容**: Desktop客户端承担部分查询逻辑，减少服务器往返

**性能理由**:
1. **减少HTTP请求**: GetById一次性返回所有导航属性，Desktop本地判断
2. **降低延迟**: Desktop本地计算CanEdit/CanDelete，无需额外API调用
3. **提升响应速度**: 用户操作即时反馈

**实现对比**:
```csharp
// ❌ 传统方式：多次HTTP请求
var medicalCase = await _api.GetByIdAsync(id);              // HTTP 1
var canEdit = await _api.CanEditAsync(id);                  // HTTP 2
var canDelete = await _api.CanDeletePrescriptionAsync(id);  // HTTP 3

// ✅ Desktop-Led方式：单次HTTP请求 + 本地计算
var medicalCase = await _api.GetByIdAsync(id);  // HTTP 1（预加载所有导航属性）
bool canEdit = medicalCase.Status == MedicalCaseStatus.Active;  // 本地计算
bool canDelete = medicalCase.Prescription?.IsPrinted != true;  // 本地计算
```

### 6.4 预加载策略（Include优化）

**决策内容**: GetByIdWithDetailsAsync统一预加载所有导航属性

**性能理由**:
1. **避免N+1查询**: 单次JOIN查询替代多次SELECT
2. **减少数据库往返**: 1次查询 vs 5次查询（MedicalCase + Consultation + TongueAnalysis + Prescription + Items）
3. **简化客户端逻辑**: 客户端无需关心加载策略

**实现方式**:
```csharp
public async Task<MedicalCase?> GetByIdWithDetailsAsync(Guid id)
{
    return await _dbSet
        .Include(mc => mc.Consultation)
            .ThenInclude(c => c.TongueAnalysis)
        .Include(mc => mc.Consultation)
            .ThenInclude(c => c.PulseAnalysis)
        .Include(mc => mc.Consultation)
            .ThenInclude(c => c.SyndromeAnalysis)
        .Include(mc => mc.Prescription)
            .ThenInclude(p => p.PrescriptionItems)
        .Include(mc => mc.Patient)
        .Include(mc => mc.Doctor)
        .FirstOrDefaultAsync(mc => mc.Id == id && !mc.IsDeleted);
}
```

**生成SQL**:
```sql
SELECT mc.*, c.*, ta.*, pa.*, sa.*, p.*, pi.*, pt.*, u.*
FROM MedicalCases mc
LEFT JOIN Consultations c ON c.MedicalCaseId = mc.Id
LEFT JOIN TongueAnalyses ta ON ta.ConsultationId = c.Id
LEFT JOIN PulseAnalyses pa ON pa.ConsultationId = c.Id
LEFT JOIN SyndromeAnalyses sa ON sa.ConsultationId = c.Id
LEFT JOIN Prescriptions p ON p.MedicalCaseId = mc.Id
LEFT JOIN PrescriptionItems pi ON pi.PrescriptionId = p.Id
LEFT JOIN Patients pt ON pt.Id = mc.PatientId
LEFT JOIN Users u ON u.Id = mc.DoctorId
WHERE mc.Id = @id AND mc.IsDeleted = 0
```

---

## 7. 模块依赖关系

### 7.1 依赖图

```
                    ┌───────────────┐
                    │   Patient     │
                    │   Module      │
                    └───────────────┘
                            ↑
                            │ FK: PatientId
                            │
    ┌───────────────────────┼───────────────────────┐
    │                       │                       │
    │              ┌────────────────┐               │
    │              │  MedicalCase   │               │
    │              │    Module      │               │
    │              │  (Aggregate    │               │
    │              │     Root)      │               │
    │              └────────────────┘               │
    │                       │                       │
    │         ┌─────────────┼─────────────┐         │
    │         │             │             │         │
    ↓         ↓             ↓             ↓         ↓
┌─────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌─────────┐
│  User   │ │Consultation│ │Prescription│ │ Tongue  │ │  Pulse  │
│ Module  │ │  (1:1)   │ │  (0..1)  │ │ Analysis│ │ Analysis│
└─────────┘ └──────────┘ └──────────┘ └──────────┘ └─────────┘
   FK:           聚合关系      聚合关系        聚合关系     聚合关系
DoctorId      (AR-001)      (AR-001+       (AR-001)    (AR-001)
                            AR-003)
```

### 7.2 模块间依赖说明

| 依赖方向 | 关系类型 | 说明 | 技术实现 |
|---------|---------|------|---------|
| MedicalCase → Patient | N:1外键 | 病案必须关联患者 | FK: PatientId (NOT NULL) |
| MedicalCase → User | N:1外键 | 病案必须关联医生 | FK: DoctorId (NOT NULL) |
| MedicalCase → Consultation | 1:1聚合 | 辨证信息（AR-001） | FK: MedicalCaseId (UNIQUE) |
| MedicalCase → Prescription | 0..1聚合 | 处方信息（AR-001 + AR-003） | FK: MedicalCaseId (UNIQUE) |
| Consultation → TongueAnalysis | 1:0..1 | 舌诊分析 | FK: ConsultationId (UNIQUE) |
| Consultation → PulseAnalysis | 1:0..1 | 脉诊分析 | FK: ConsultationId (UNIQUE) |
| Prescription → Herbs | N:M | 处方药材 | PrescriptionItems中间表 |

### 7.3 循环依赖预防

**问题**: MedicalCase ↔ Consultation可能形成循环依赖

**解决方案**: 单向导航属性
```csharp
// ✅ 正确：MedicalCase → Consultation单向导航
public class MedicalCase
{
    public virtual Consultation? Consultation { get; set; }  // 有导航属性
}

public class Consultation
{
    public Guid MedicalCaseId { get; set; }  // 仅外键，无导航属性
    // ❌ 避免: public virtual MedicalCase MedicalCase { get; set; }
}
```

---

## 8. 扩展性设计

### 8.1 水平扩展（分库分表）

**患者维度分片**
```csharp
// 根据PatientId分片
public class MedicalCaseShardingStrategy
{
    public string GetShardKey(Guid patientId)
    {
        // 患者ID哈希取模（4个分片）
        int shard = Math.Abs(patientId.GetHashCode()) % 4;
        return $"MedicalCases_Shard{shard}";
    }
}

// 查询路由
var shardKey = _shardingStrategy.GetShardKey(patientId);
var medicalCases = await _repository.GetByPatientIdAsync(patientId, shardKey);
```

### 8.2 垂直扩展（只读副本）

**读写分离**
```csharp
// 写操作：主库
await _writeRepository.UpdateAsync(medicalCase);

// 读操作：只读副本
var medicalCases = await _readRepository.GetPagedAsync(pageIndex, pageSize);
```

### 8.3 新增业务规则扩展点

**规则接口**
```csharp
public interface IMedicalCaseBusinessRule
{
    string RuleCode { get; }
    Task ValidateAsync(MedicalCase medicalCase);
}

// 新增规则：BR-004医生权限验证
public class DoctorPermissionRule : IMedicalCaseBusinessRule
{
    public string RuleCode => "BR-004";

    public async Task ValidateAsync(MedicalCase medicalCase)
    {
        var doctor = await _userRepository.GetByIdAsync(medicalCase.DoctorId);
        if (!doctor.HasPermission("EDIT_MEDICALCASE"))
            throw new BusinessRuleException("BR-004", "医生无病案编辑权限");
    }
}

// 注册规则
services.AddScoped<IMedicalCaseBusinessRule, DoctorPermissionRule>();
```

### 8.4 新增状态扩展

**状态枚举扩展**
```csharp
public enum MedicalCaseStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3,

    // 未来扩展
    UnderReview = 4,     // 审核中
    Archived = 5,        // 已归档
    Suspended = 6        // 已暂停
}
```

### 8.5 性能监控扩展点

**性能追踪**
```csharp
public class MedicalCaseService
{
    private readonly ILogger<MedicalCaseService> _logger;

    public async Task<MedicalCase> CreateAsync(CreateMedicalCaseDto dto)
    {
        using var activity = Activity.StartActivity("MedicalCase.Create");
        activity?.SetTag("PatientId", dto.PatientId);
        activity?.SetTag("DoctorId", dto.DoctorId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await CreateInternalAsync(dto);
            _logger.LogInformation("病案创建成功，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "病案创建失败，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## 总结

病案管理系统是LYBTZYZS的**核心聚合根模块**，通过**Aggregate Root模式**、**三步流程强制执行**和**一诊一方约束**确保诊疗数据的完整性和一致性。模块采用**Desktop-Led模式**优化性能，通过**预加载策略**减少数据库往返，通过**单向导航属性**避免循环依赖，为中医诊疗提供稳定可靠的业务支撑。
