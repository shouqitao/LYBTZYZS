# Server端病案管理开发指南

## 📋 文档信息

- **适用范围**: LYBTZYZS项目 - Server端病案管理模块开发
- **技术栈**: ASP.NET Core 8.0 + Entity Framework Core 8.0 + FluentValidation + AutoMapper
- **架构层级**: Controller → Service → Repository 三层架构
- **最后更新**: 2025-10-30
- **Epic关联**: #1612, #1669, #1676

---

## 1. 概述

### 1.1 模块职责

**LYBT.Module.MedicalCase** 负责病案的完整生命周期管理，遵循 **Epic #1612 聚合根架构**：

- **AR-001**: 所有写操作通过MedicalCase聚合根执行
- **AR-003**: 每个病案只能关联一个处方
- **BF-002**: 三步工作流验证（辨证 → 处方 → 完成）
- **状态管理**: Draft → Active → Completed/Cancelled

### 1.2 核心技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| ASP.NET Core | 8.0 | Web API框架 |
| Entity Framework Core | 8.0 | ORM数据持久化 |
| FluentValidation | 11.x | DTO验证框架 |
| AutoMapper | 13.x | Entity ↔ DTO映射 |
| xUnit | 2.x | 单元测试框架 |
| Moq | 4.x | Mock对象框架 |

### 1.3 三层架构模式

```
┌─────────────────────────────────────────────┐
│  LYBT.WebAPI (Presentation Layer)          │
│  ├── Controllers/                          │
│  │   └── MedicalCaseController.cs          │
│  └── ApiResponse<T> 统一响应包装            │
└─────────────────────────────────────────────┘
              ↓ HTTP Request/Response
┌─────────────────────────────────────────────┐
│  LYBT.Module.MedicalCase (Business Layer)  │
│  ├── Services/                             │
│  │   └── MedicalCaseService.cs (21个方法)  │
│  ├── Validators/                           │
│  │   ├── CreateDtoValidator.cs             │
│  │   └── UpdateDtoValidator.cs             │
│  └── Mapping/                              │
│      └── MedicalCaseMappingProfile.cs      │
└─────────────────────────────────────────────┘
              ↓ IRepository Interface
┌─────────────────────────────────────────────┐
│  LYBT.Infrastructure (Data Access Layer)   │
│  ├── Repositories/                         │
│  │   └── MedicalCaseRepository.cs          │
│  └── AppDbContext (EF Core)                │
└─────────────────────────────────────────────┘
              ↓ SQL
┌─────────────────────────────────────────────┐
│  SQL Server 2022 (Database)                │
└─────────────────────────────────────────────┘
```

---

## 2. Controller层实现

### 2.1 基础结构

```csharp
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 病案管理控制器 - Epic #1612三层架构
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class MedicalCaseController : BaseApiController
    {
        private readonly NewMedicalCaseService _medicalCaseService;

        public MedicalCaseController(
            NewMedicalCaseService medicalCaseService,
            ILogger<MedicalCaseController> logger,
            IMemoryCache cache) : base(logger, cache)
        {
            _medicalCaseService = medicalCaseService;
        }

        // Write Layer - 9个写操作端点
        // Read Layer - 5个读操作端点
        // Helper Layer - 2个辅助端点
    }
}
```

**关键点**：
- ✅ 继承 `BaseApiController` 获取通用功能（Logger、Cache、HandleException）
- ✅ 构造函数注入依赖（Service、Logger、Cache）
- ✅ Route属性定义API路径 `/api/v1/medicalcase`
- ✅ Produces属性指定JSON响应格式

### 2.2 Write Layer - 创建病案

```csharp
/// <summary>
/// 创建病案（Epic #1612 AR-001: 通过聚合根创建）
/// </summary>
/// <param name="request">创建请求（PatientId, VisitDate）</param>
/// <returns>创建的病案实体</returns>
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]  // 患者不存在
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 422)]  // BR-001违规
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreateMedicalCase(
    [FromBody] CreateMedicalCaseRequest request)
{
    try
    {
        var result = await _medicalCaseService.CreateAsync(request.PatientId, request.VisitDate);

        if (result == null)
            return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("患者不存在"));

        _logger.LogInformation("病案创建成功，ID: {Id}", result.Id);
        return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "病案创建成功"));
    }
    catch (InvalidOperationException ex)
    {
        // BR-001: 单个患者只能有一个Active病案
        _logger.LogWarning(ex, "创建病案失败：业务规则验证失败");
        return UnprocessableEntity(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
    catch (Exception ex)
    {
        return HandleException<MedicalCaseEntity>(ex, "创建病案", request);
    }
}

// Request DTO
public class CreateMedicalCaseRequest
{
    public Guid PatientId { get; set; }
    public DateTime VisitDate { get; set; }
}
```

**HTTP状态码映射**：
- **200 OK**: 创建成功
- **404 Not Found**: 患者不存在（引用完整性检查）
- **422 Unprocessable Entity**: BR-001业务规则违规（患者已有Active病案）
- **500 Internal Server Error**: 未预期的异常（BaseApiController.HandleException处理）

### 2.3 Write Layer - 更新辨证信息

```csharp
/// <summary>
/// 更新辨证信息（Epic #1612 BF-002: 三步工作流Step 1）
/// </summary>
/// <param name="id">病案ID</param>
/// <param name="request">辨证信息（Symptoms, TCMDiagnosis等）</param>
/// <returns>更新后的病案实体</returns>
[HttpPut("{id}/consultation")]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 400)]  // 状态不允许
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]  // 病案不存在
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> UpdateConsultation(
    Guid id,
    [FromBody] UpdateConsultationRequest request)
{
    try
    {
        var result = await _medicalCaseService.UpdateConsultationAsync(id, request);

        if (result == null)
            return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

        _logger.LogInformation("辨证信息更新成功，MedicalCaseId: {Id}", id);
        return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "辨证信息更新成功"));
    }
    catch (InvalidOperationException ex)
    {
        // 状态不允许编辑（Completed/Cancelled）
        _logger.LogWarning(ex, "更新辨证信息失败：状态不允许");
        return BadRequest(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
    catch (Exception ex)
    {
        return HandleException<MedicalCaseEntity>(ex, "更新辨证信息", new { id, request });
    }
}

// Request DTO
public class UpdateConsultationRequest
{
    public string? ChiefComplaint { get; set; }          // 主诉
    public string? PresentIllness { get; set; }          // 现病史
    public string? Symptoms { get; set; }                // 症状
    public string? TCMDiagnosis { get; set; }            // 中医诊断
    public string? WMDiagnosis { get; set; }             // 西医诊断
    public string? TreatmentPrinciple { get; set; }      // 治则治法
}
```

**关键点**：
- ✅ RESTful路径设计 `/api/v1/medicalcase/{id}/consultation`
- ✅ 幂等性：多次调用相同请求结果一致
- ✅ 状态验证：只有Draft/Active状态允许更新

### 2.4 Write Layer - 创建处方

```csharp
/// <summary>
/// 创建处方（Epic #1612 AR-003: 单处方约束）
/// </summary>
/// <param name="id">病案ID</param>
/// <param name="request">处方信息（Items, TotalPrice等）</param>
/// <returns>更新后的病案实体（包含新创建的Prescription）</returns>
[HttpPost("{id}/prescription")]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 400)]  // 已有处方或状态不允许
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]  // 病案不存在
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreatePrescription(
    Guid id,
    [FromBody] CreatePrescriptionRequest request)
{
    try
    {
        var result = await _medicalCaseService.CreatePrescriptionAsync(id, request);

        if (result == null)
            return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

        _logger.LogInformation("处方创建成功，MedicalCaseId: {Id}", id);
        return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "处方创建成功"));
    }
    catch (InvalidOperationException ex)
    {
        // AR-003: 已有处方或状态不允许
        _logger.LogWarning(ex, "创建处方失败：{Message}", ex.Message);
        return BadRequest(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
    }
    catch (Exception ex)
    {
        return HandleException<MedicalCaseEntity>(ex, "创建处方", new { id, request });
    }
}

// Request DTO
public class CreatePrescriptionRequest
{
    public List<PrescriptionItemRequest> Items { get; set; } = new();
    public decimal? TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class PrescriptionItemRequest
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal Dosage { get; set; }
    public string Unit { get; set; } = "g";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

**业务规则验证**（Service层）：
- ✅ **AR-003**: 每个病案只能有一个处方
- ✅ **BF-002**: 必须先完成辨证（Consultation不为null）
- ✅ 状态验证：只有Active状态允许创建处方

### 2.5 Read Layer - 分页查询

```csharp
/// <summary>
/// 分页查询病案列表（支持状态和患者过滤）
/// </summary>
/// <param name="status">状态过滤（可选）</param>
/// <param name="patientId">患者ID过滤（可选）</param>
/// <param name="page">页码（默认1）</param>
/// <param name="pageSize">页大小（默认20，最大100）</param>
/// <returns>分页结果</returns>
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseEntity>>), 200)]
[ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseEntity>>), 400)]  // 参数无效
public async Task<ActionResult<ApiResponse<PagedResult<MedicalCaseEntity>>>> GetList(
    [FromQuery] MedicalCaseStatus? status = null,
    [FromQuery] Guid? patientId = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    try
    {
        // 参数验证
        if (page <= 0 || pageSize <= 0 || pageSize > 100)
        {
            return BadRequest(ApiResponse<PagedResult<MedicalCaseEntity>>.CreateFail(
                "页码和页大小参数无效（页码>0，页大小1-100）"));
        }

        var result = await _medicalCaseService.GetListAsync(status, patientId, page, pageSize);

        return Ok(ApiResponse<PagedResult<MedicalCaseEntity>>.CreateSuccess(result, "查询成功"));
    }
    catch (Exception ex)
    {
        return HandleException<PagedResult<MedicalCaseEntity>>(ex, "获取病案列表",
            new { status, patientId, page, pageSize });
    }
}
```

**分页响应结构**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [ /* MedicalCaseEntity数组 */ ],
    "totalCount": 156,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "timestamp": "2025-10-30T10:30:00Z"
}
```

### 2.6 Read Layer - 获取详情

```csharp
/// <summary>
/// 获取病案详情（包含Consultation和Prescription完整数据）
/// </summary>
/// <param name="id">病案ID</param>
/// <returns>病案详情</returns>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
[ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> GetById(Guid id)
{
    try
    {
        var result = await _medicalCaseService.GetByIdAsync(id);

        if (result == null)
            return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

        return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "查询成功"));
    }
    catch (Exception ex)
    {
        return HandleException<MedicalCaseEntity>(ex, "获取病案详情", new { id });
    }
}
```

**返回数据包含**：
- ✅ MedicalCase基本信息
- ✅ Consultation完整数据（辨证信息）
- ✅ Prescription完整数据（包含Items子集合）

### 2.7 Helper Layer - 验证可编辑性

```csharp
/// <summary>
/// 验证病案是否可编辑（Helper端点）
/// </summary>
/// <param name="id">病案ID</param>
/// <returns>验证结果（CanEdit, Reason）</returns>
[HttpGet("{id}/can-edit")]
[ProducesResponseType(typeof(ApiResponse<CanEditResponse>), 200)]
[ProducesResponseType(typeof(ApiResponse<CanEditResponse>), 404)]
public async Task<ActionResult<ApiResponse<CanEditResponse>>> CanEdit(Guid id)
{
    try
    {
        var result = await _medicalCaseService.CanEditAsync(id);

        if (result == null)
            return NotFound(ApiResponse<CanEditResponse>.CreateFail("病案不存在"));

        return Ok(ApiResponse<CanEditResponse>.CreateSuccess(result, "验证成功"));
    }
    catch (Exception ex)
    {
        return HandleException<CanEditResponse>(ex, "验证病案可编辑性", new { id });
    }
}

// Response DTO
public class CanEditResponse
{
    public bool CanEdit { get; set; }
    public string? Reason { get; set; }  // 不可编辑时的原因说明
}
```

**使用场景**：
- ✅ Client端在打开编辑界面前调用验证
- ✅ 避免用户编辑后提交失败的糟糕体验
- ✅ 提供友好的不可编辑原因提示

---

## 3. Service层业务逻辑

### 3.1 Service接口定义

```csharp
using LYBT.Entities;
using LYBT.Shared.Models;

namespace LYBT.Server.Interfaces.Services
{
    /// <summary>
    /// 病案管理服务接口（21个方法）
    /// </summary>
    public interface IMedicalCaseService
    {
        // Write Operations (9个)
        Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate);
        Task<MedicalCaseEntity?> UpdateConsultationAsync(Guid id, UpdateConsultationRequest request);
        Task<MedicalCaseEntity?> SetPrescriptionFlagAsync(Guid id, bool hasPrescription);
        Task<MedicalCaseEntity?> CreatePrescriptionAsync(Guid id, CreatePrescriptionRequest request);
        Task<MedicalCaseEntity?> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionRequest request);
        Task<bool> DeletePrescriptionAsync(Guid id);
        Task<MedicalCaseEntity?> UpdateStatusAsync(Guid id, MedicalCaseStatus status);
        Task<MedicalCaseEntity?> CompleteAsync(Guid id);
        Task<MedicalCaseEntity?> CloseCaseAsync(Guid id);

        // Read Operations (5个)
        Task<MedicalCaseEntity?> GetByIdAsync(Guid id);
        Task<PagedResult<MedicalCaseEntity>> GetListAsync(
            MedicalCaseStatus? status, Guid? patientId, int page, int pageSize);
        Task<List<ConsultationEntity>> GetConsultationListAsync(Guid medicalCaseId);
        Task<List<PrescriptionEntity>> GetPrescriptionListAsync(Guid medicalCaseId);
        Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId);

        // Helper Operations (2个)
        Task<CanEditResponse?> CanEditAsync(Guid id);
        Task<CanDeletePrescriptionResponse?> CanDeletePrescriptionAsync(Guid id);
    }
}
```

### 3.2 Service实现 - 创建病案

```csharp
public async Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate)
{
    _logger.LogInformation("开始创建病案，PatientId: {PatientId}", patientId);

    // 1. 验证患者存在性（通过PatientService）
    var patient = await _patientRepository.GetByIdAsync(patientId);
    if (patient == null)
    {
        _logger.LogWarning("创建病案失败：患者不存在，PatientId: {PatientId}", patientId);
        return null;
    }

    // 2. BR-001业务规则：检查患者是否已有Active病案
    var existingActiveCase = await _repository.GetByPatientIdAsync(patientId);
    var hasActiveCase = existingActiveCase?
        .Any(c => c.Status == MedicalCaseStatus.Active) ?? false;

    if (hasActiveCase)
    {
        _logger.LogWarning("创建病案失败：患者已有Active病案，PatientId: {PatientId}", patientId);
        throw new InvalidOperationException("该患者已有进行中的病案，请先完成或关闭现有病案");
    }

    // 3. 创建病案实体（初始状态为Draft）
    var medicalCase = new MedicalCaseEntity
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,
        PatientName = patient.Name,
        DoctorId = _currentUserService.GetCurrentUserId(),  // 从当前登录用户获取
        DoctorName = _currentUserService.GetCurrentUserName(),
        VisitDate = visitDate,
        Status = MedicalCaseStatus.Draft,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // 4. 持久化到数据库
    var created = await _repository.AddAsync(medicalCase);
    await _repository.SaveChangesAsync();

    _logger.LogInformation("病案创建成功，Id: {Id}, Status: {Status}", created.Id, created.Status);
    return created;
}
```

**关键点**：
- ✅ 引用完整性验证（患者存在性）
- ✅ BR-001业务规则验证（单个患者Active病案唯一性）
- ✅ 自动设置创建人信息（从CurrentUserService获取）
- ✅ 初始状态为Draft（用户可暂存）
- ✅ 结构化日志记录（LogInformation/LogWarning）

### 3.3 Service实现 - 更新辨证信息

```csharp
public async Task<MedicalCaseEntity?> UpdateConsultationAsync(
    Guid id,
    UpdateConsultationRequest request)
{
    _logger.LogInformation("开始更新辨证信息，MedicalCaseId: {Id}", id);

    // 1. 获取病案实体（包含Consultation）
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
    if (medicalCase == null)
    {
        _logger.LogWarning("更新辨证信息失败：病案不存在，Id: {Id}", id);
        return null;
    }

    // 2. 状态验证：只有Draft/Active允许编辑
    if (medicalCase.Status == MedicalCaseStatus.Completed ||
        medicalCase.Status == MedicalCaseStatus.Cancelled)
    {
        _logger.LogWarning("更新辨证信息失败：状态不允许，Status: {Status}", medicalCase.Status);
        throw new InvalidOperationException("已完成或已取消的病案不允许编辑");
    }

    // 3. 创建或更新Consultation子实体
    if (medicalCase.Consultation == null)
    {
        // 首次创建Consultation
        medicalCase.Consultation = new ConsultationEntity
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = id,
            CreatedAt = DateTime.UtcNow
        };
        _logger.LogInformation("创建新的Consultation实体，Id: {ConsultationId}",
            medicalCase.Consultation.Id);
    }

    // 4. 更新Consultation字段
    medicalCase.Consultation.ChiefComplaint = request.ChiefComplaint;
    medicalCase.Consultation.PresentIllness = request.PresentIllness;
    medicalCase.Consultation.Symptoms = request.Symptoms;
    medicalCase.Consultation.TCMDiagnosis = request.TCMDiagnosis;
    medicalCase.Consultation.WMDiagnosis = request.WMDiagnosis;
    medicalCase.Consultation.TreatmentPrinciple = request.TreatmentPrinciple;
    medicalCase.Consultation.UpdatedAt = DateTime.UtcNow;

    // 5. 如果是首次添加辨证且状态为Draft，自动切换到Active
    if (medicalCase.Status == MedicalCaseStatus.Draft &&
        !string.IsNullOrWhiteSpace(request.TCMDiagnosis))
    {
        medicalCase.Status = MedicalCaseStatus.Active;
        _logger.LogInformation("病案状态自动切换：Draft → Active");
    }

    medicalCase.UpdatedAt = DateTime.UtcNow;

    // 6. 持久化更新（通过聚合根）
    var updated = await _repository.UpdateAsync(medicalCase);
    await _repository.SaveChangesAsync();

    _logger.LogInformation("辨证信息更新成功，MedicalCaseId: {Id}", id);
    return updated;
}
```

**业务规则**：
- ✅ **AR-001**: 通过MedicalCase聚合根更新Consultation
- ✅ **BF-002**: 辨证是三步工作流的第一步
- ✅ 自动状态转换：Draft → Active（首次填写诊断时）
- ✅ 幂等性：多次调用相同请求结果一致

### 3.4 Service实现 - 创建处方

```csharp
public async Task<MedicalCaseEntity?> CreatePrescriptionAsync(
    Guid id,
    CreatePrescriptionRequest request)
{
    _logger.LogInformation("开始创建处方，MedicalCaseId: {Id}", id);

    // 1. 获取病案实体（包含Consultation和Prescription）
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
    if (medicalCase == null)
    {
        _logger.LogWarning("创建处方失败：病案不存在，Id: {Id}", id);
        return null;
    }

    // 2. 状态验证：只有Active状态允许创建处方
    if (medicalCase.Status != MedicalCaseStatus.Active)
    {
        _logger.LogWarning("创建处方失败：状态不是Active，Status: {Status}", medicalCase.Status);
        throw new InvalidOperationException("只有进行中的病案才能创建处方");
    }

    // 3. BF-002业务规则：必须先完成辨证
    if (medicalCase.Consultation == null ||
        string.IsNullOrWhiteSpace(medicalCase.Consultation.TCMDiagnosis))
    {
        _logger.LogWarning("创建处方失败：未完成辨证，MedicalCaseId: {Id}", id);
        throw new InvalidOperationException("请先完成辨证信息填写");
    }

    // 4. AR-003业务规则：每个病案只能有一个处方
    if (medicalCase.Prescription != null)
    {
        _logger.LogWarning("创建处方失败：已存在处方，MedicalCaseId: {Id}", id);
        throw new InvalidOperationException("该病案已有处方，请使用更新接口");
    }

    // 5. 创建Prescription子实体
    var prescription = new PrescriptionEntity
    {
        Id = Guid.NewGuid(),
        MedicalCaseId = id,
        TotalPrice = request.TotalPrice ?? 0,
        Notes = request.Notes,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Items = new List<PrescriptionItemEntity>()
    };

    // 6. 创建PrescriptionItem子实体（药材明细）
    foreach (var itemRequest in request.Items)
    {
        var item = new PrescriptionItemEntity
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescription.Id,
            HerbId = itemRequest.HerbId,
            HerbName = itemRequest.HerbName,
            Dosage = itemRequest.Dosage,
            Unit = itemRequest.Unit,
            Quantity = itemRequest.Quantity,
            UnitPrice = itemRequest.UnitPrice,
            CreatedAt = DateTime.UtcNow
        };
        prescription.Items.Add(item);
    }

    // 7. 关联Prescription到MedicalCase
    medicalCase.Prescription = prescription;
    medicalCase.HasPrescription = true;
    medicalCase.UpdatedAt = DateTime.UtcNow;

    // 8. 持久化更新（通过聚合根，级联插入Prescription和Items）
    var updated = await _repository.UpdateAsync(medicalCase);
    await _repository.SaveChangesAsync();

    _logger.LogInformation("处方创建成功，PrescriptionId: {PrescriptionId}, ItemsCount: {Count}",
        prescription.Id, prescription.Items.Count);

    return updated;
}
```

**关键点**：
- ✅ **AR-003**: 单处方约束验证
- ✅ **BF-002**: 三步工作流验证（辨证 → 处方）
- ✅ 级联创建：Prescription + Items一次性持久化
- ✅ 自动计算总价（可选，由Client端计算后传入）

### 3.5 状态转换验证

```csharp
/// <summary>
/// 私有方法：验证状态转换是否合法
/// </summary>
private bool IsValidStatusTransition(MedicalCaseStatus currentStatus, MedicalCaseStatus newStatus)
{
    // 状态机定义
    var validTransitions = new Dictionary<MedicalCaseStatus, List<MedicalCaseStatus>>
    {
        // Draft可以转换为Active或Cancelled
        { MedicalCaseStatus.Draft, new List<MedicalCaseStatus>
            { MedicalCaseStatus.Active, MedicalCaseStatus.Cancelled } },

        // Active可以转换为Completed或Cancelled
        { MedicalCaseStatus.Active, new List<MedicalCaseStatus>
            { MedicalCaseStatus.Completed, MedicalCaseStatus.Cancelled } },

        // Completed和Cancelled是终态，不允许转换
        { MedicalCaseStatus.Completed, new List<MedicalCaseStatus>() },
        { MedicalCaseStatus.Cancelled, new List<MedicalCaseStatus>() }
    };

    // 允许相同状态（幂等性）
    if (currentStatus == newStatus)
        return true;

    // 验证转换是否在允许列表中
    return validTransitions.TryGetValue(currentStatus, out var allowedStatuses) &&
           allowedStatuses.Contains(newStatus);
}
```

**状态机图**：
```
Draft ──────┬──→ Active ────┬──→ Completed (终态)
            │               │
            └──→ Cancelled ←┘
                  (终态)
```

---

## 4. Repository层数据访问

### 4.1 Repository基础结构

```csharp
using LYBT.Entities;
using LYBT.Infrastructure.Database;
using LYBT.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Repositories
{
    /// <summary>
    /// 病案仓储实现（Epic #1612 + Issue #1669 + Epic #1676）
    /// </summary>
    internal class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(AppDbContext context) : base(context)
        {
        }

        public MedicalCaseRepository(AppDbContext context, ILogger<MedicalCaseRepository> logger)
            : base(context, logger)
        {
        }

        // 基础查询（不Include关联数据）
        private IQueryable<MedicalCaseEntity> GetBaseQuery()
        {
            return _dbSet.Where(m => !m.IsDeleted);
        }

        // 详细查询（Include Consultation和Prescription.Items）
        private IQueryable<MedicalCaseEntity> GetDetailQuery()
        {
            return _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription!)
                    .ThenInclude(p => p.Items)
                .Where(m => !m.IsDeleted);
        }
    }
}
```

**关键设计**：
- ✅ **GetBaseQuery()**: 列表查询，不Include关联数据（性能优化）
- ✅ **GetDetailQuery()**: 详情查询，Include完整数据（避免N+1）
- ✅ **ThenInclude**: 深度预加载 Prescription.Items
- ✅ **软删除过滤**: 所有查询自动排除 IsDeleted=true

### 4.2 GetByIdWithDetailsAsync - 详情查询

```csharp
/// <summary>
/// 按ID查询病案详情（包含Consultation和Prescription.Items）
/// </summary>
public async Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id)
{
    return (await GetDetailQuery()
        .Where(m => m.Id == id)
        .FirstOrDefaultAsync())!;
}
```

**执行的SQL**（示例）：
```sql
SELECT
    m.*,
    c.*,
    p.*,
    pi.*
FROM MedicalCases m
LEFT JOIN Consultations c ON m.Id = c.MedicalCaseId
LEFT JOIN Prescriptions p ON m.Id = p.MedicalCaseId
LEFT JOIN PrescriptionItems pi ON p.Id = pi.PrescriptionId
WHERE m.Id = @id AND m.IsDeleted = 0
```

**优势**：
- ✅ 单次查询加载所有数据（避免N+1查询）
- ✅ 返回完整对象图（MedicalCase + Consultation + Prescription + Items）

### 4.3 GetPagedWithDetailsAsync - 分页查询

```csharp
/// <summary>
/// 分页查询病案列表（支持关键字搜索）
/// </summary>
public async Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
    int pageNumber,
    int pageSize,
    string? keyword = null)
{
    var query = GetBaseQuery();  // 使用BaseQuery提升性能

    // 关键字搜索（只搜索基本字段）
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(m =>
            m.PatientName.Contains(keyword) ||
            m.DoctorName.Contains(keyword));
    }

    // 总记录数
    var totalCount = await query.CountAsync();

    // 分页数据
    var items = await query
        .OrderByDescending(m => m.CreatedAt)  // 默认按创建时间倒序
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<MedicalCaseEntity>
    {
        Items = items,
        TotalCount = totalCount,
        CurrentPage = pageNumber,
        PageSize = pageSize
    };
}
```

**关键点**：
- ✅ 使用 `GetBaseQuery()`（列表不需要Include关联数据）
- ✅ 先Count再Skip/Take（标准分页模式）
- ✅ OrderByDescending（最新记录优先）
- ✅ 返回 `PagedResult<T>`（包含分页元数据）

### 4.4 UpdateAsync - 复杂更新逻辑（Issue #1669 Phase 7）

```csharp
/// <summary>
/// 更新病案（处理Tracked/Detached状态 + 级联删除逻辑）
/// Issue #1669 Phase 7: 修复InMemory数据库RowVersion同步问题
/// Issue #1571: 状态变为终态时级联删除Consultation和Prescription
/// </summary>
public override async Task<MedicalCaseEntity> UpdateAsync(MedicalCaseEntity entity)
{
    if (entity == null)
        throw new ArgumentNullException(nameof(entity));

    // ⚠️ Issue #1669 Phase 7: 检查entity的跟踪状态
    var entry = _context.Entry(entity);
    _logger?.LogInformation("🔍 [诊断] UpdateAsync开始 - MedicalCaseId: {Id}, EntryState: {State}, HasPrescription: {HasPrescription}",
        entity.Id, entry.State, entity.Prescription != null);

    // ⚠️ Issue #1669 Phase 7: 修复Prescription状态错误
    if (entity.Prescription != null)
    {
        var prescriptionEntry = _context.Entry(entity.Prescription);

        if (prescriptionEntry.State == EntityState.Modified)
        {
            // 检查Prescription是否真的存在于数据库
            var existsInDb = await _context.Set<PrescriptionEntity>()
                .AnyAsync(p => p.Id == entity.Prescription.Id);

            if (!existsInDb)
            {
                _logger?.LogInformation("🔧 [修复] 检测到新Prescription被错误标记为Modified，改为Added");
                prescriptionEntry.State = EntityState.Added;
            }
        }
    }

    MedicalCaseEntity existingEntity;

    if (entry.State == EntityState.Detached)
    {
        // Detached场景：从数据库查询existingEntity
        existingEntity = await _dbSet
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
            .FirstOrDefaultAsync(m => m.Id == entity.Id);

        if (existingEntity == null)
            throw new InvalidOperationException($"医案 {entity.Id} 不存在");

        // 使用SetValues复制属性
        _context.Entry(existingEntity).CurrentValues.SetValues(entity);
    }
    else
    {
        // Tracked场景：entity本身就是existingEntity
        existingEntity = entity;

        // ⚠️ Issue #1669 Phase 7: InMemory数据库RowVersion同步
        var rowVersionProperty = entry.Property("RowVersion");
        if (rowVersionProperty != null)
        {
            rowVersionProperty.OriginalValue = rowVersionProperty.CurrentValue;
        }
    }

    // 检测状态变更：从Active/Draft变为Completed或Cancelled
    bool isMovingToTerminalState =
        (existingEntity.Status == MedicalCaseStatus.Active || existingEntity.Status == MedicalCaseStatus.Draft) &&
        (entity.Status == MedicalCaseStatus.Completed || entity.Status == MedicalCaseStatus.Cancelled);

    if (isMovingToTerminalState)
    {
        _logger?.LogInformation("检测到医案状态变更为终态（Completed/Cancelled），准备级联删除关联数据");

        // 删除关联的Consultation
        if (existingEntity.Consultation != null)
        {
            _context.Set<ConsultationEntity>().Remove(existingEntity.Consultation);
            _logger?.LogInformation("级联删除Consultation，Id: {Id}", existingEntity.Consultation.Id);
        }

        // 删除关联的Prescription（包含Items，由EF Core级联）
        if (existingEntity.Prescription != null)
        {
            _context.Set<PrescriptionEntity>().Remove(existingEntity.Prescription);
            _logger?.LogInformation("级联删除Prescription，Id: {Id}", existingEntity.Prescription.Id);
        }
    }

    await SaveChangesAsync();
    return existingEntity;
}
```

**关键点**：
- ✅ **Tracked vs Detached**: 处理不同EntityState的更新策略
- ✅ **Prescription状态修正**: 新Prescription错误标记为Modified时自动修正为Added
- ✅ **RowVersion同步**: InMemory数据库的并发控制兼容
- ✅ **级联删除**: 状态变为终态时自动删除Consultation和Prescription
- ✅ **详细日志**: 每个关键步骤都有日志记录

### 4.5 QueryAsync - 多条件查询

```csharp
/// <summary>
/// 多条件组合查询（Issue #1592）
/// </summary>
public async Task<List<MedicalCaseEntity>> QueryAsync(
    string? patientName = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? diagnosisKeyword = null)
{
    var query = GetDetailQuery();

    // 患者姓名模糊匹配
    if (!string.IsNullOrWhiteSpace(patientName))
    {
        query = query.Where(m => m.PatientName.Contains(patientName));
    }

    // 日期范围过滤
    if (startDate.HasValue)
    {
        query = query.Where(m => m.CreatedAt >= startDate.Value);
    }
    if (endDate.HasValue)
    {
        // 包含整个结束日期（到23:59:59）
        var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
        query = query.Where(m => m.CreatedAt <= endOfDay);
    }

    // 诊断关键字搜索（搜索Consultation.TCMDiagnosis）
    if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
    {
        query = query.Where(m =>
            m.Consultation != null &&
            m.Consultation.TCMDiagnosis != null &&
            m.Consultation.TCMDiagnosis.Contains(diagnosisKeyword));
    }

    var result = await query
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();

    return result;
}
```

**使用场景**：
- ✅ 高级搜索功能
- ✅ 病案统计报表
- ✅ 数据导出功能

---

## 5. FluentValidation验证器

### 5.1 CreateDto验证器

```csharp
using FluentValidation;
using LYBT.Shared.Models;

namespace LYBT.Module.MedicalCase.Validators
{
    /// <summary>
    /// 创建病案DTO验证器
    /// </summary>
    public class MedicalCaseCreateDtoValidator : AbstractValidator<CreateMedicalCaseRequest>
    {
        public MedicalCaseCreateDtoValidator()
        {
            // 患者ID必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空")
                .NotEqual(Guid.Empty).WithMessage("患者ID无效");

            // 就诊日期必填且不能为未来日期
            RuleFor(x => x.VisitDate)
                .NotEmpty().WithMessage("就诊日期不能为空")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("就诊日期不能为未来日期");
        }
    }
}
```

### 5.2 UpdateConsultation验证器

```csharp
/// <summary>
/// 更新辨证信息DTO验证器
/// </summary>
public class UpdateConsultationRequestValidator : AbstractValidator<UpdateConsultationRequest>
{
    public MedicalCaseUpdateDtoValidator()
    {
        // 主诉：可选，但如果填写则长度限制
        RuleFor(x => x.ChiefComplaint)
            .MaximumLength(500).WithMessage("主诉不能超过500字符");

        // 现病史：可选，长度限制
        RuleFor(x => x.PresentIllness)
            .MaximumLength(1000).WithMessage("现病史不能超过1000字符");

        // 症状：可选，长度限制
        RuleFor(x => x.Symptoms)
            .MaximumLength(500).WithMessage("症状不能超过500字符");

        // 中医诊断：建议必填（三步工作流第一步）
        RuleFor(x => x.TCMDiagnosis)
            .NotEmpty().WithMessage("中医诊断不能为空")
            .MaximumLength(200).WithMessage("中医诊断不能超过200字符");

        // 西医诊断：可选，长度限制
        RuleFor(x => x.WMDiagnosis)
            .MaximumLength(200).WithMessage("西医诊断不能超过200字符");

        // 治则治法：建议必填
        RuleFor(x => x.TreatmentPrinciple)
            .NotEmpty().WithMessage("治则治法不能为空")
            .MaximumLength(200).WithMessage("治则治法不能超过200字符");
    }
}
```

### 5.3 CreatePrescription验证器

```csharp
/// <summary>
/// 创建处方DTO验证器
/// </summary>
public class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        // 药材列表：至少1个药材
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("处方药材不能为空")
            .Must(items => items.Count >= 1).WithMessage("处方至少包含1味药材")
            .Must(items => items.Count <= 50).WithMessage("处方最多包含50味药材");

        // 总价：可选，但如果填写则必须>0
        RuleFor(x => x.TotalPrice)
            .GreaterThan(0).When(x => x.TotalPrice.HasValue)
            .WithMessage("总价必须大于0");

        // 备注：可选，长度限制
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("备注不能超过500字符");

        // 药材明细验证
        RuleForEach(x => x.Items).SetValidator(new PrescriptionItemRequestValidator());
    }
}

/// <summary>
/// 处方药材明细验证器
/// </summary>
public class PrescriptionItemRequestValidator : AbstractValidator<PrescriptionItemRequest>
{
    public PrescriptionItemRequestValidator()
    {
        // 药材ID必填
        RuleFor(x => x.HerbId)
            .NotEmpty().WithMessage("药材ID不能为空")
            .NotEqual(Guid.Empty).WithMessage("药材ID无效");

        // 药材名称必填
        RuleFor(x => x.HerbName)
            .NotEmpty().WithMessage("药材名称不能为空")
            .MaximumLength(50).WithMessage("药材名称不能超过50字符");

        // 剂量：必填且>0
        RuleFor(x => x.Dosage)
            .GreaterThan(0).WithMessage("剂量必须大于0")
            .LessThanOrEqualTo(1000).WithMessage("剂量不能超过1000克");

        // 单位：必填
        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("剂量单位不能为空")
            .MaximumLength(10).WithMessage("单位不能超过10字符");

        // 数量（剂数）：必填且>0
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("数量必须大于0")
            .LessThanOrEqualTo(100).WithMessage("数量不能超过100剂");

        // 单价：必填且>=0
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("单价不能为负数");
    }
}
```

### 5.4 注册验证器（Startup.cs）

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;

public void ConfigureServices(IServiceCollection services)
{
    // 注册FluentValidation
    services.AddFluentValidationAutoValidation();
    services.AddFluentValidationClientsideAdapters();

    // 自动扫描并注册所有验证器
    services.AddValidatorsFromAssemblyContaining<MedicalCaseCreateDtoValidator>();

    // 或手动注册
    services.AddScoped<IValidator<CreateMedicalCaseRequest>, MedicalCaseCreateDtoValidator>();
    services.AddScoped<IValidator<UpdateConsultationRequest>, UpdateConsultationRequestValidator>();
    services.AddScoped<IValidator<CreatePrescriptionRequest>, CreatePrescriptionRequestValidator>();
}
```

---

## 6. AutoMapper配置

### 6.1 Mapping Profile

```csharp
using AutoMapper;
using LYBT.Entities;
using LYBT.Shared.Models;

namespace LYBT.Module.MedicalCase.Mapping
{
    /// <summary>
    /// 病案模块AutoMapper映射配置
    /// </summary>
    public class MedicalCaseMappingProfile : Profile
    {
        public MedicalCaseMappingProfile()
        {
            // MedicalCase Entity ↔ DTO
            CreateMap<MedicalCaseEntity, MedicalCaseDto>()
                .ReverseMap();

            // Consultation Entity ↔ DTO
            CreateMap<ConsultationEntity, ConsultationDto>()
                .ReverseMap();

            // Prescription Entity ↔ DTO
            CreateMap<PrescriptionEntity, PrescriptionDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ReverseMap();

            // PrescriptionItem Entity ↔ DTO
            CreateMap<PrescriptionItemEntity, PrescriptionItemDto>()
                .ReverseMap();

            // CreateRequest → Entity（单向映射）
            CreateMap<UpdateConsultationRequest, ConsultationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}
```

### 6.2 注册AutoMapper（Startup.cs）

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 自动扫描并注册所有Profile
    services.AddAutoMapper(typeof(MedicalCaseMappingProfile).Assembly);
}
```

### 6.3 在Service中使用Mapper

```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMapper _mapper;

    public MedicalCaseService(IMapper mapper, ...)
    {
        _mapper = mapper;
    }

    // Entity → DTO
    public async Task<MedicalCaseDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdWithDetailsAsync(id);
        if (entity == null) return null;

        var dto = _mapper.Map<MedicalCaseDto>(entity);  // ✅ 自动映射
        return dto;
    }

    // Request DTO → Entity（部分字段）
    public async Task UpdateConsultationAsync(Guid id, UpdateConsultationRequest request)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(id);

        // ✅ 自动映射request字段到medicalCase.Consultation
        _mapper.Map(request, medicalCase.Consultation);

        await _repository.UpdateAsync(medicalCase);
    }
}
```

---

## 7. ApiResponse统一响应

### 7.1 ApiResponse<T>定义

```csharp
namespace LYBT.WebAPI.Models
{
    /// <summary>
    /// 统一API响应包装（Epic #1612）
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // 工厂方法：创建成功响应
        public static ApiResponse<T> CreateSuccess(T data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        // 工厂方法：创建失败响应
        public static ApiResponse<T> CreateFail(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}
```

### 7.2 HTTP状态码映射规则

| 状态码 | 场景 | ApiResponse.Success | 示例 |
|-------|------|---------------------|------|
| **200 OK** | 操作成功 | true | 查询成功、更新成功 |
| **201 Created** | 资源创建成功 | true | POST创建病案 |
| **204 No Content** | 删除成功 | - | DELETE删除处方 |
| **400 Bad Request** | 客户端错误 | false | 参数无效、状态不允许 |
| **404 Not Found** | 资源不存在 | false | 病案不存在、患者不存在 |
| **422 Unprocessable Entity** | 业务规则违规 | false | BR-001违规、AR-003违规 |
| **500 Internal Server Error** | 服务端错误 | false | 未预期异常（HandleException） |

### 7.3 使用示例

```csharp
// ✅ 成功响应（200 OK）
return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(medicalCase, "病案创建成功"));

// ❌ 失败响应（404 Not Found）
return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

// ❌ 失败响应（422 Unprocessable Entity）
return UnprocessableEntity(ApiResponse<MedicalCaseEntity>.CreateFail("患者已有Active病案"));

// ❌ 失败响应（400 Bad Request）
return BadRequest(ApiResponse<MedicalCaseEntity>.CreateFail("状态不允许编辑"));
```

---

## 8. 异常处理与日志

### 8.1 BaseApiController.HandleException

```csharp
/// <summary>
/// 统一异常处理方法（BaseApiController提供）
/// </summary>
protected ActionResult<ApiResponse<T>> HandleException<T>(
    Exception ex,
    string operation,
    object? context = null)
{
    _logger.LogError(ex, "执行{Operation}时发生异常，Context: {@Context}",
        operation, context);

    return StatusCode(500, ApiResponse<T>.CreateFail($"执行{operation}失败：{ex.Message}"));
}
```

### 8.2 结构化日志最佳实践

```csharp
// ✅ 推荐：结构化日志（使用{}占位符）
_logger.LogInformation("病案创建成功，Id: {Id}, PatientId: {PatientId}",
    medicalCase.Id, medicalCase.PatientId);

// ❌ 避免：字符串拼接
_logger.LogInformation($"病案创建成功，Id: {medicalCase.Id}");

// ✅ 推荐：记录完整上下文（使用{@}占位符）
_logger.LogError(ex, "创建病案失败，Request: {@Request}", request);

// ✅ 推荐：不同日志级别的使用
_logger.LogDebug("开始创建病案，PatientId: {PatientId}", patientId);  // 调试信息
_logger.LogInformation("病案创建成功，Id: {Id}", id);                   // 正常流程
_logger.LogWarning("创建病案失败：患者已有Active病案");                 // 业务警告
_logger.LogError(ex, "创建病案失败：数据库异常");                       // 错误异常
_logger.LogCritical(ex, "数据库连接失败，服务不可用");                   // 致命错误
```

### 8.3 日志输出示例

```json
{
  "Timestamp": "2025-10-30T10:30:00.123Z",
  "Level": "Information",
  "MessageTemplate": "病案创建成功，Id: {Id}, PatientId: {PatientId}",
  "Properties": {
    "Id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "PatientId": "a8d8e729-4b2f-4e93-b7c6-1f1234567890",
    "SourceContext": "LYBT.Module.MedicalCase.Services.MedicalCaseService"
  }
}
```

---

## 9. 单元测试

### 9.1 Service层测试（xUnit + Moq）

```csharp
using Xunit;
using Moq;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.MedicalCase.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService单元测试（AAA模式）
    /// </summary>
    public class MedicalCaseServiceTests
    {
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;
        private readonly MedicalCaseService _service;

        public MedicalCaseServiceTests()
        {
            _repositoryMock = new Mock<IMedicalCaseRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<MedicalCaseService>>();
            _service = new MedicalCaseService(
                _repositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidPatient_ReturnsNewMedicalCase()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            var patient = new PatientEntity { Id = patientId, Name = "张三" };
            _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _repositoryMock.Setup(r => r.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());  // 无Active病案

            var expectedMedicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Status = MedicalCaseStatus.Draft
            };
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(expectedMedicalCase);

            // Act
            var result = await _service.CreateAsync(patientId, visitDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal(MedicalCaseStatus.Draft, result.Status);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<MedicalCaseEntity>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_PatientNotFound_ReturnsNull()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
                .ReturnsAsync((PatientEntity?)null);  // 患者不存在

            // Act
            var result = await _service.CreateAsync(patientId, visitDate);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<MedicalCaseEntity>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_PatientHasActiveCase_ThrowsInvalidOperationException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            var patient = new PatientEntity { Id = patientId, Name = "张三" };
            _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            var existingActiveCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Status = MedicalCaseStatus.Active  // 已有Active病案
            };
            _repositoryMock.Setup(r => r.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity> { existingActiveCase });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(patientId, visitDate));

            Assert.Contains("已有进行中的病案", ex.Message);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<MedicalCaseEntity>()), Times.Never);
        }

        [Fact]
        public async Task UpdateConsultationAsync_ValidRequest_ReturnsUpdatedMedicalCase()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateConsultationRequest
            {
                ChiefComplaint = "头痛",
                TCMDiagnosis = "外感风寒"
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = id,
                Status = MedicalCaseStatus.Active,
                Consultation = null  // 首次创建Consultation
            };
            _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.UpdateConsultationAsync(id, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Consultation);
            Assert.Equal(request.ChiefComplaint, result.Consultation.ChiefComplaint);
            Assert.Equal(request.TCMDiagnosis, result.Consultation.TCMDiagnosis);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MedicalCaseEntity>()), Times.Once);
        }

        [Fact]
        public async Task UpdateConsultationAsync_CompletedStatus_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateConsultationRequest();

            var medicalCase = new MedicalCaseEntity
            {
                Id = id,
                Status = MedicalCaseStatus.Completed  // 终态不允许编辑
            };
            _repositoryMock.Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateConsultationAsync(id, request));

            Assert.Contains("不允许编辑", ex.Message);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MedicalCaseEntity>()), Times.Never);
        }
    }
}
```

**AAA模式总结**：
- **Arrange**：准备测试数据和Mock对象
- **Act**：执行被测试方法
- **Assert**：验证结果和行为（使用`Assert.*`和`Verify`）

### 9.2 Repository层测试（InMemory数据库）

```csharp
using Xunit;
using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Database;
using LYBT.Module.MedicalCase.Repositories;

namespace LYBT.Module.MedicalCase.Tests.Repositories
{
    /// <summary>
    /// MedicalCaseRepository集成测试（InMemory数据库）
    /// </summary>
    public class MedicalCaseRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicalCaseRepository _repository;

        public MedicalCaseRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new MedicalCaseRepository(_context);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_ReturnsWithConsultationAndPrescription()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity
                {
                    Id = Guid.NewGuid(),
                    TCMDiagnosis = "外感风寒"
                },
                Prescription = new PrescriptionEntity
                {
                    Id = Guid.NewGuid(),
                    TotalPrice = 150.00m,
                    Items = new List<PrescriptionItemEntity>
                    {
                        new PrescriptionItemEntity
                        {
                            Id = Guid.NewGuid(),
                            HerbName = "麻黄",
                            Dosage = 6
                        }
                    }
                }
            };
            await _context.Set<MedicalCaseEntity>().AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdWithDetailsAsync(medicalCase.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(medicalCase.Id, result.Id);
            Assert.NotNull(result.Consultation);
            Assert.Equal("外感风寒", result.Consultation.TCMDiagnosis);
            Assert.NotNull(result.Prescription);
            Assert.Single(result.Prescription.Items);  // 验证ThenInclude加载Items
            Assert.Equal("麻黄", result.Prescription.Items.First().HerbName);
        }

        [Fact]
        public async Task GetPagedWithDetailsAsync_WithKeyword_ReturnsFilteredResults()
        {
            // Arrange
            await _context.Set<MedicalCaseEntity>().AddRangeAsync(
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientName = "张三", DoctorName = "李医生" },
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientName = "李四", DoctorName = "王医生" },
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientName = "王五", DoctorName = "李医生" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedWithDetailsAsync(1, 10, "李");

            // Assert
            Assert.Equal(3, result.TotalCount);  // "李四" + "李医生"(2次)
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task UpdateAsync_StatusToCompleted_CascadeDeletesConsultationAndPrescription()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = Guid.NewGuid() },
                Prescription = new PrescriptionEntity { Id = Guid.NewGuid() }
            };
            await _context.Set<MedicalCaseEntity>().AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            medicalCase.Status = MedicalCaseStatus.Completed;
            await _repository.UpdateAsync(medicalCase);

            // Assert
            var consultationExists = await _context.Set<ConsultationEntity>()
                .AnyAsync(c => c.Id == medicalCase.Consultation.Id);
            var prescriptionExists = await _context.Set<PrescriptionEntity>()
                .AnyAsync(p => p.Id == medicalCase.Prescription.Id);

            Assert.False(consultationExists, "Consultation应该被级联删除");
            Assert.False(prescriptionExists, "Prescription应该被级联删除");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
```

---

## 10. 集成测试

### 10.1 WebApplicationFactory测试

```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using LYBT.WebAPI;

namespace LYBT.Module.MedicalCase.IntegrationTests
{
    /// <summary>
    /// MedicalCase API集成测试（WebApplicationFactory）
    /// </summary>
    public class MedicalCaseApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MedicalCaseApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateMedicalCase_ValidRequest_Returns200WithMedicalCase()
        {
            // Arrange
            var request = new CreateMedicalCaseRequest
            {
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),  // 测试患者ID
                VisitDate = DateTime.Now
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/medicalcase", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseEntity>>();

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(MedicalCaseStatus.Draft, apiResponse.Data.Status);
        }

        [Fact]
        public async Task CreateMedicalCase_PatientNotFound_Returns404()
        {
            // Arrange
            var request = new CreateMedicalCaseRequest
            {
                PatientId = Guid.NewGuid(),  // 不存在的患者ID
                VisitDate = DateTime.Now
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/medicalcase", request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseEntity>>();

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Contains("患者不存在", apiResponse.Message);
        }

        [Fact]
        public async Task GetById_ExistingId_Returns200WithDetails()
        {
            // Arrange
            var id = Guid.Parse("22222222-2222-2222-2222-222222222222");  // 测试病案ID

            // Act
            var response = await _client.GetAsync($"/api/v1/medicalcase/{id}");

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseEntity>>();

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(id, apiResponse.Data.Id);
        }

        [Fact]
        public async Task UpdateConsultation_ValidRequest_Returns200()
        {
            // Arrange
            var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var request = new UpdateConsultationRequest
            {
                ChiefComplaint = "头痛3天",
                TCMDiagnosis = "外感风寒",
                TreatmentPrinciple = "辛温解表"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/medicalcase/{id}/consultation", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseEntity>>();

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data.Consultation);
            Assert.Equal(request.ChiefComplaint, apiResponse.Data.Consultation.ChiefComplaint);
        }
    }
}
```

---

## 11. 常见问题与陷阱

### 11.1 N+1查询问题

**❌ 错误**：未使用Include导致N+1查询
```csharp
// 每个MedicalCase会触发2次额外查询（Consultation + Prescription）
var medicalCases = await _context.Set<MedicalCaseEntity>().ToListAsync();
foreach (var mc in medicalCases)
{
    var consultation = mc.Consultation;  // 触发查询1
    var prescription = mc.Prescription;  // 触发查询2
}
```

**✅ 正确**：使用Include/ThenInclude预加载
```csharp
var medicalCases = await _context.Set<MedicalCaseEntity>()
    .Include(m => m.Consultation)
    .Include(m => m.Prescription)
        .ThenInclude(p => p.Items)
    .ToListAsync();  // 单次查询加载所有数据
```

### 11.2 Tracked vs Detached实体问题

**❌ 错误**：未检查EntityState直接Update
```csharp
public async Task UpdateAsync(MedicalCaseEntity entity)
{
    _context.Update(entity);  // 可能抛出异常（Detached状态）
    await _context.SaveChangesAsync();
}
```

**✅ 正确**：根据EntityState选择策略
```csharp
public async Task UpdateAsync(MedicalCaseEntity entity)
{
    var entry = _context.Entry(entity);

    if (entry.State == EntityState.Detached)
    {
        // 从数据库查询existingEntity，使用SetValues复制
        var existing = await _dbSet.FindAsync(entity.Id);
        _context.Entry(existing).CurrentValues.SetValues(entity);
    }
    else
    {
        // 已跟踪，直接SaveChanges
    }

    await _context.SaveChangesAsync();
}
```

### 11.3 级联删除未生效

**❌ 错误**：未配置EF Core级联删除
```csharp
// OnModelCreating未配置
modelBuilder.Entity<MedicalCaseEntity>()
    .HasOne(m => m.Consultation)
    .WithOne()
    .HasForeignKey<ConsultationEntity>(c => c.MedicalCaseId);
// 缺少 .OnDelete(DeleteBehavior.Cascade)
```

**✅ 正确**：显式配置级联删除
```csharp
modelBuilder.Entity<MedicalCaseEntity>()
    .HasOne(m => m.Consultation)
    .WithOne()
    .HasForeignKey<ConsultationEntity>(c => c.MedicalCaseId)
    .OnDelete(DeleteBehavior.Cascade);  // ✅ 显式指定级联删除
```

### 11.4 业务规则验证缺失

**❌ 错误**：Controller直接调用Repository
```csharp
[HttpPost]
public async Task<ActionResult> CreateMedicalCase(CreateRequest request)
{
    var medicalCase = new MedicalCaseEntity { ... };
    await _repository.AddAsync(medicalCase);  // ❌ 跳过Service层验证
    return Ok(medicalCase);
}
```

**✅ 正确**：通过Service层验证业务规则
```csharp
[HttpPost]
public async Task<ActionResult> CreateMedicalCase(CreateRequest request)
{
    var result = await _medicalCaseService.CreateAsync(request.PatientId, request.VisitDate);
    // ✅ Service层已验证：患者存在性、BR-001单个Active病案约束

    if (result == null)
        return NotFound("患者不存在");

    return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result));
}
```

### 11.5 异常处理不当

**❌ 错误**：捕获Exception但不记录
```csharp
try
{
    await _service.CreateAsync(...);
}
catch (Exception)
{
    return BadRequest("创建失败");  // ❌ 丢失异常信息
}
```

**✅ 正确**：记录异常并使用HandleException
```csharp
try
{
    await _service.CreateAsync(...);
}
catch (InvalidOperationException ex)
{
    // ✅ 业务异常：转换为422
    _logger.LogWarning(ex, "创建病案失败：业务规则验证失败");
    return UnprocessableEntity(ApiResponse.CreateFail(ex.Message));
}
catch (Exception ex)
{
    // ✅ 未预期异常：使用HandleException统一处理
    return HandleException<MedicalCaseEntity>(ex, "创建病案", request);
}
```

### 11.6 分页参数未验证

**❌ 错误**：未验证分页参数
```csharp
[HttpGet]
public async Task<ActionResult> GetList(int page, int pageSize)
{
    var result = await _service.GetListAsync(page, pageSize);
    // ❌ page=0或pageSize=1000可能导致问题
    return Ok(result);
}
```

**✅ 正确**：验证分页参数
```csharp
[HttpGet]
public async Task<ActionResult> GetList(int page = 1, int pageSize = 20)
{
    if (page <= 0 || pageSize <= 0 || pageSize > 100)
    {
        return BadRequest(ApiResponse.CreateFail(
            "页码和页大小参数无效（页码>0，页大小1-100）"));
    }

    var result = await _service.GetListAsync(page, pageSize);
    return Ok(ApiResponse.CreateSuccess(result));
}
```

### 11.7 AutoMapper配置错误

**❌ 错误**：未注册Profile导致映射失败
```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAutoMapper(typeof(Startup));  // ❌ 只扫描WebAPI程序集
}

// Service层
var dto = _mapper.Map<MedicalCaseDto>(entity);  // ❌ 抛出异常（未找到映射配置）
```

**✅ 正确**：注册包含Profile的程序集
```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAutoMapper(typeof(MedicalCaseMappingProfile).Assembly);  // ✅ 扫描正确程序集
}
```

### 11.8 RowVersion并发冲突

**❌ 错误**：未处理DbUpdateConcurrencyException
```csharp
public async Task UpdateAsync(MedicalCaseEntity entity)
{
    _context.Update(entity);
    await _context.SaveChangesAsync();  // ❌ 可能抛出并发异常
}
```

**✅ 正确**：捕获并处理并发异常
```csharp
public async Task UpdateAsync(MedicalCaseEntity entity)
{
    try
    {
        _context.Update(entity);
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        _logger.LogWarning(ex, "并发冲突：RowVersion不匹配");
        throw new InvalidOperationException("数据已被其他用户修改，请刷新后重试", ex);
    }
}
```

---

## 12. 检查清单

### 12.1 开发前检查

- [ ] 已理解Epic #1612架构（AR-001/AR-003/BF-002）
- [ ] 已阅读 `docs/architecture/server/README.md`
- [ ] 已理解三层架构（Controller → Service → Repository）
- [ ] 已配置开发环境（.NET 8 SDK + SQL Server 2022）

### 12.2 Controller开发检查

- [ ] 继承 `BaseApiController`
- [ ] 使用构造函数注入依赖（Service、Logger、Cache）
- [ ] 所有端点都有 `[HttpXxx]` 和 `[ProducesResponseType]`
- [ ] 返回 `ApiResponse<T>` 统一响应
- [ ] 正确的HTTP状态码映射（200/404/422/400/500）
- [ ] 使用 `try-catch` 捕获异常并调用 `HandleException`
- [ ] 分页端点验证参数（page>0，pageSize 1-100）

### 12.3 Service开发检查

- [ ] 实现 `IMedicalCaseService` 接口
- [ ] 使用构造函数注入依赖（Repository、Mapper、Logger）
- [ ] 所有方法都是 `async Task<T>`
- [ ] 验证业务规则（BR-001、AR-003、BF-002）
- [ ] 状态转换使用 `IsValidStatusTransition` 验证
- [ ] 创建子实体时生成新Guid
- [ ] 更新时设置 `UpdatedAt = DateTime.UtcNow`
- [ ] 记录结构化日志（LogInformation/LogWarning/LogError）
- [ ] 抛出 `InvalidOperationException`（业务规则违规）

### 12.4 Repository开发检查

- [ ] 继承 `BaseRepository<MedicalCaseEntity>`
- [ ] 实现 `IMedicalCaseRepository` 接口
- [ ] 提供 `GetBaseQuery()` 和 `GetDetailQuery()` 方法
- [ ] 详情查询使用 `Include().ThenInclude()`（避免N+1）
- [ ] 列表查询使用 `GetBaseQuery()`（性能优化）
- [ ] 分页查询先Count再Skip/Take
- [ ] UpdateAsync处理Tracked/Detached状态
- [ ] UpdateAsync实现级联删除逻辑（状态变为终态时）
- [ ] UpdateAsync同步RowVersion（InMemory数据库）

### 12.5 测试检查

- [ ] Service层单元测试覆盖核心业务逻辑
- [ ] 使用Moq模拟依赖（Repository、Mapper）
- [ ] 遵循AAA模式（Arrange、Act、Assert）
- [ ] 使用 `Verify` 验证方法调用次数
- [ ] Repository层使用InMemory数据库测试
- [ ] 集成测试使用WebApplicationFactory
- [ ] 测试覆盖正常流程和异常流程
- [ ] 测试命名清晰（MethodName_Scenario_ExpectedResult）

### 12.6 提交前检查

- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 单元测试全部通过
- [ ] 集成测试全部通过
- [ ] 代码格式化（dotnet format）
- [ ] 更新相关文档（README、API文档）
- [ ] Commit Message符合规范（feat/fix/refactor）
- [ ] PR关联相关Issue

---

## 13. 参考资料

### 13.1 项目文档

- **架构设计**：`docs/architecture/server/README.md`
- **API文档**：`docs/api/medicalcase-api.md`
- **开发规范**：`docs/development/server/coding-standards.md`
- **测试指南**：`docs/development/server/testing-guide.md`

### 13.2 技术文档

- **ASP.NET Core 8.0**: https://learn.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core 8.0**: https://learn.microsoft.com/en-us/ef/core/
- **FluentValidation**: https://docs.fluentvalidation.net/
- **AutoMapper**: https://docs.automapper.org/
- **xUnit**: https://xunit.net/
- **Moq**: https://github.com/moq/moq4

### 13.3 Epic与Issue

- **Epic #1612**: 三层对齐架构（AR-001/AR-003/BF-002）
- **Issue #1669 Phase 7**: InMemory数据库RowVersion修复
- **Issue #1571**: 级联删除逻辑
- **Epic #1676**: 暂存病案功能
- **Issue #1592**: 多条件查询功能

---

## 附录：完整代码示例

### A. MedicalCaseController完整代码

> **位置**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`

（完整代码略，参考2.1-2.7节）

### B. MedicalCaseService完整代码

> **位置**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`

（完整代码略，参考3.2-3.5节）

### C. MedicalCaseRepository完整代码

> **位置**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

（完整代码略，参考4.2-4.5节）

---

**最后更新**: 2025-10-30
**维护负责**: Server端开发组
**版本**: v1.0

---

**本指南涵盖LYBTZYZS项目Server端病案管理模块的完整开发流程，从Controller层到Repository层，包含代码示例、最佳实践、常见陷阱和完整的测试策略。遵循本指南可确保代码符合Epic #1612架构规范和项目编码标准。**
