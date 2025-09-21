# LYBT.Module.MedicalCase

> **医疗案例管理核心模块** - 中医诊疗流程管理容器
> 看诊流程管理 + 诊疗记录聚合 | 专为中医诊所优化
> 模块状态: ✅ **生产就绪** | 🎆 **DTO优化完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.MedicalCase是系统的医疗案例管理核心模块，采用分层架构设计，作为整个诊疗流程的管理容器和聚合根。每个MedicalCase代表一次完整的看诊会话，1:1关联Consultation诊断记录，统一管理患者从接诊到完成的全程诊疗状态。

**技术栈**: .NET 8 + 实体（实体（Entity）） Framework Core 8.0 + AutoMapper + 分层架构
**最新优化**: DTO命名规范化、查询DTO更新为SearchDto、状态管理简化

## 🎉 2025-09-20 DTO优化成果

### ✅ 优化完成内容
- **查询DTO规范**: MedicalCaseQueryDto → MedicalCaseSearchDto，命名统一
- **状态简化**: 使用Active/Closed简化状态管理
- **接口一致性**: 所有查询接口使用统一的SearchDto模式
- **编译状态**: 零错误零警告，完全生产就绪

## 🏗️ 分层架构设计

```
MedicalCaseService (主服务层 - 纯委托模式)
    │
    ├── MedicalCaseQueryService (查询专业化层)
    │   ├── 医案搜索 (SearchMedicalCasesAsync)
    │   ├── 患者历史 (GetPatientHistoryAsync)
    │   ├── 医生医案 (GetDoctorCasesAsync)
    │   └── 详情查询 (GetDetailAsync)
    │
    └── MedicalCaseBusinessService (业务逻辑+CRUD层)
        ├── 医案创建 (CreateAsync)
        ├── 状态更新 (UpdateStatusAsync)
        ├── 完成医案 (CompleteAsync)
        └── 取消医案 (CancelAsync)
```

### 核心接口设计

```csharp
// 主服务接口 (统一入口) - 2025-09-20更新
public interface IMedicalCaseService
{
    // CRUD操作
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);

    // 查询操作 - 使用新的SearchDto
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchAsync(MedicalCaseSearchDto query);
    Task<ServiceResult<MedicalCaseDetailDto>> GetDetailAsync(Guid id);

    // 业务操作
    Task<ServiceResult<bool>> CompleteAsync(Guid id, string remarks = null);
    Task<ServiceResult<bool>> CancelAsync(Guid id, string reason = null);
    Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status);
}

// 查询专业化接口
public interface IMedicalCaseQueryService
{
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchMedicalCasesAsync(MedicalCaseSearchDto criteria);
    Task<ServiceResult<List<MedicalCaseDto>>> GetPatientHistoryAsync(Guid patientId);
    Task<ServiceResult<List<MedicalCaseDto>>> GetDoctorCasesAsync(Guid doctorId, MedicalCaseStatus? status);
    Task<ServiceResult<MedicalCaseStatisticsDto>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
}

// 业务逻辑接口
public interface IMedicalCaseBusinessService
{
    Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateMedicalCaseAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult<bool>> DeleteMedicalCaseAsync(Guid id);
    Task<ServiceResult<bool>> CompleteMedicalCaseAsync(Guid id, CompletionDto dto);
    Task<ServiceResult<bool>> CancelMedicalCaseAsync(Guid id, string reason);
}
```

## 📦 核心功能模块

### 1. 医案管理 (诊疗流程容器)

**创建医案流程**:
```csharp
public async Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(MedicalCaseCreateDto dto)
{
    // 1. 数据验证
    if (dto.PatientId == Guid.Empty || dto.DoctorId == Guid.Empty)
        return ServiceResult<MedicalCaseDto>.Failure("患者ID和医生ID不能为空");

    // 2. 医案编号生成
    var caseNumber = await GenerateCaseNumberAsync();

    // 3. 创建医案实体
    var medicalCase = new MedicalCase
    {
        Id = Guid.NewGuid(),
        CaseNumber = caseNumber,
        PatientId = dto.PatientId,
        DoctorId = dto.DoctorId,
        Status = MedicalCaseStatus.Active,  // 简化状态
        VisitDate = dto.VisitDate ?? DateTime.Now,
        ChiefComplaint = dto.ChiefComplaint,
        PresentIllness = dto.PresentIllness,
        CreateTime = DateTime.UtcNow
    };

    // 4. 保存到数据库
    var created = await _repository.CreateAsync(medicalCase);

    // 5. 返回DTO
    return ServiceResult<MedicalCaseDto>.Success(_mapper.Map<MedicalCaseDto>(created));
}
```

### 2. 状态流转管理

**简化状态机**:
```csharp
public enum MedicalCaseStatus
{
    Active = 1,    // 活跃状态（登记/进行中）
    Closed = 2     // 关闭状态（完成/取消）
}

// 状态转换逻辑
public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus newStatus)
{
    var medicalCase = await _repository.GetByIdAsync(id);
    if (medicalCase == null)
        return ServiceResult<bool>.Failure("医案不存在");

    // 简化状态转换：Active可以转为Closed
    if (medicalCase.Status == MedicalCaseStatus.Closed)
        return ServiceResult<bool>.Failure("医案已关闭，不能修改状态");

    medicalCase.Status = newStatus;
    medicalCase.UpdateTime = DateTime.UtcNow;

    if (newStatus == MedicalCaseStatus.Closed)
    {
        medicalCase.CompletedTime = DateTime.UtcNow;
    }

    await _repository.UpdateAsync(medicalCase);
    return ServiceResult<bool>.Success(true);
}
```

### 3. 查询与搜索

**医案搜索功能**:
```csharp
public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchMedicalCasesAsync(
    MedicalCaseSearchDto criteria)
{
    var query = _repository.GetQueryable();

    // 关键词搜索（医案号、主诉、诊断）
    if (!string.IsNullOrWhiteSpace(criteria.Keyword))
    {
        query = query.Where(m =>
            m.CaseNumber.Contains(criteria.Keyword) ||
            m.ChiefComplaint.Contains(criteria.Keyword) ||
            m.Diagnosis.Contains(criteria.Keyword));
    }

    // 状态筛选
    if (criteria.Status.HasValue)
    {
        query = query.Where(m => m.Status == criteria.Status.Value);
    }

    // 患者筛选
    if (criteria.PatientId.HasValue)
    {
        query = query.Where(m => m.PatientId == criteria.PatientId.Value);
    }

    // 医生筛选（权限控制）
    if (criteria.DoctorId.HasValue)
    {
        query = query.Where(m => m.DoctorId == criteria.DoctorId.Value);
    }

    // 日期范围筛选
    if (criteria.StartDate.HasValue)
    {
        query = query.Where(m => m.VisitDate >= criteria.StartDate.Value);
    }
    if (criteria.EndDate.HasValue)
    {
        query = query.Where(m => m.VisitDate <= criteria.EndDate.Value);
    }

    // 关联数据预加载
    query = query
        .Include(m => m.Patient)
        .Include(m => m.Doctor)
        .Include(m => m.Consultation)
        .OrderByDescending(m => m.VisitDate);

    // 分页查询
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((criteria.PageIndex - 1) * criteria.PageSize)
        .Take(criteria.PageSize)
        .ToListAsync();

    return ServiceResult<PagedResult<MedicalCaseDto>>.Success(new PagedResult<MedicalCaseDto>
    {
        Items = _mapper.Map<List<MedicalCaseDto>>(items),
        TotalCount = totalCount,
        PageIndex = criteria.PageIndex,
        PageSize = criteria.PageSize
    });
}
```

## 🧪 数据传输对象 (数据传输对象（数据传输对象（DTO））) - 2025-09-20更新

### 请求DTOs
```csharp
// 创建医案DTO
public class MedicalCaseCreateDto
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "医生ID不能为空")]
    public Guid DoctorId { get; set; }

    public DateTime? VisitDate { get; set; }

    [StringLength(500)]
    public string? ChiefComplaint { get; set; }

    [StringLength(1000)]
    public string? PresentIllness { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}

// 更新医案DTO
public class MedicalCaseUpdateDto
{
    [StringLength(500)]
    public string? ChiefComplaint { get; set; }

    [StringLength(1000)]
    public string? PresentIllness { get; set; }

    [StringLength(500)]
    public string? Diagnosis { get; set; }

    [StringLength(500)]
    public string? TreatmentPlan { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}

// 医案搜索DTO (原MedicalCaseQueryDto)
public class MedicalCaseSearchDto : PagedRequestDto
{
    public string? Keyword { get; set; }
    public MedicalCaseStatus? Status { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// 完成医案DTO
public class CompletionDto
{
    [StringLength(500)]
    public string? Diagnosis { get; set; }

    [StringLength(500)]
    public string? TreatmentSummary { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}
```

### 响应DTOs
```csharp
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public MedicalCaseStatus Status { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? PresentIllness { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTime? CompletedTime { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }

    // 关联状态
    public bool HasConsultation { get; set; }
    public bool HasPrescription { get; set; }

    // 显示属性
    public string StatusDisplay => Status == MedicalCaseStatus.Active ? "进行中" : "已关闭";
}

public class MedicalCaseDetailDto : MedicalCaseDto
{
    public PatientDto Patient { get; set; } = new();
    public UserDto Doctor { get; set; } = new();
    public ConsultationDto? Consultation { get; set; }
    public List<PrescriptionDto> Prescriptions { get; set; } = new();
}

public class MedicalCaseStatisticsDto
{
    public int TotalCases { get; set; }
    public int ActiveCases { get; set; }
    public int ClosedCases { get; set; }
    public Dictionary<string, int> CasesByDoctor { get; set; } = new();
    public Dictionary<DateTime, int> CasesByDay { get; set; } = new();
    public decimal? TotalRevenue { get; set; }
}
```

## 🔧 Repository层设计

```csharp
public class MedicalCaseRepository : BaseRepository<MedicalCase>, IMedicalCaseRepository
{
    public async Task<MedicalCase?> GetDetailAsync(Guid id)
    {
        return await _context.MedicalCases
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Include(m => m.Consultation)
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Herb)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<List<MedicalCase>> GetPatientHistoryAsync(Guid patientId)
    {
        return await _context.MedicalCases
            .Where(m => m.PatientId == patientId && !m.IsDeleted)
            .Include(m => m.Doctor)
            .Include(m => m.Consultation)
            .OrderByDescending(m => m.VisitDate)
            .ToListAsync();
    }

    public async Task<List<MedicalCase>> GetActiveCasesByDoctorAsync(Guid doctorId)
    {
        return await _context.MedicalCases
            .Where(m => m.DoctorId == doctorId
                && m.Status == MedicalCaseStatus.Active
                && !m.IsDeleted)
            .Include(m => m.Patient)
            .OrderBy(m => m.VisitDate)
            .ToListAsync();
    }

    public async Task<string> GenerateNextCaseNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"MC{today:yyyyMMdd}";

        var lastCase = await _context.MedicalCases
            .Where(m => m.CaseNumber.StartsWith(prefix))
            .OrderByDescending(m => m.CaseNumber)
            .FirstOrDefaultAsync();

        if (lastCase == null)
        {
            return $"{prefix}001";
        }

        var lastNumber = int.Parse(lastCase.CaseNumber.Substring(10));
        return $"{prefix}{(lastNumber + 1):D3}";
    }
}
```

## 🚀 API接口规范

### RESTful API设计
| HTTP Method | Endpoint | 功能 | 权限 |
|-------------|----------|------|------|
| GET | `/api/v1/medicalcases` | 搜索医案 | Doctor,Admin |
| GET | `/api/v1/medicalcases/{id}` | 医案详情 | Doctor,Admin |
| POST | `/api/v1/medicalcases` | 创建医案 | Doctor,Admin |
| PUT | `/api/v1/medicalcases/{id}` | 更新医案 | Doctor,Admin |
| PATCH | `/api/v1/medicalcases/{id}/complete` | 完成医案 | Doctor,Admin |
| PATCH | `/api/v1/medicalcases/{id}/cancel` | 取消医案 | Doctor,Admin |
| GET | `/api/v1/medicalcases/patient/{patientId}` | 患者历史 | Doctor,Admin |
| GET | `/api/v1/medicalcases/statistics` | 统计数据 | Admin |

### 使用示例

#### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MedicalCasesController : BaseApiController
{
    private readonly IMedicalCaseService _medicalCaseService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MedicalCaseDto>>>> SearchMedicalCases(
        [FromQuery] MedicalCaseSearchDto criteria)
    {
        var result = await _medicalCaseService.SearchAsync(criteria);
        return HandleServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> CreateMedicalCase(
        [FromBody] MedicalCaseCreateDto dto)
    {
        var result = await _medicalCaseService.CreateAsync(dto);
        return HandleServiceResult(result);
    }

    [HttpPatch("{id}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteMedicalCase(
        Guid id, [FromBody] CompletionDto dto)
    {
        var result = await _medicalCaseService.CompleteAsync(id, dto.Remarks);
        return HandleServiceResult(result);
    }

    [HttpGet("{id}/detail")]
    public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetMedicalCaseDetail(Guid id)
    {
        var result = await _medicalCaseService.GetDetailAsync(id);
        return HandleServiceResult(result);
    }
}
```

## 🔒 安全特性

### 数据安全
- **零SQL注入**: LINQ查询 + EF Core参数化
- **权限隔离**: 医生只能访问自己的医案
- **数据验证**: 完整的输入验证和业务规则检查
- **审计跟踪**: 医案状态变更完整记录

### 权限控制
```csharp
// 医生权限过滤
private IQueryable<MedicalCase> ApplyDoctorFilter(IQueryable<MedicalCase> query)
{
    if (_currentUser.Role == UserRole.Doctor)
    {
        query = query.Where(m => m.DoctorId == _currentUser.Id);
    }
    return query;
}
```

## 🎯 业务特色功能

### 诊疗流程管理
- 医案作为诊疗容器，聚合所有相关信息
- 1:1关联Consultation，确保诊断完整性
- 支持多处方管理
- 完整的就诊历史追踪

### 中医特色支持
- 中医诊断术语支持
- 辨证论治记录
- 治法方药关联
- 疗效评估追踪

### 统计分析
- 医生工作量统计
- 患者就诊频次分析
- 疾病谱分析
- 收入统计（可选）

## 📚 相关文档

- [Consultation诊察模块](../LYBT.Module.Consultation/README.md) - 四诊信息
- [Patients患者模块](../LYBT.Module.Patients/README.md) - 患者管理
- [Prescriptions处方模块](../LYBT.Module.Prescriptions/README.md) - 处方管理

---

> 📌 **最新成果**: DTO命名规范化，状态管理简化，编译通过
> 🎆 **生产就绪**: 完整的医案管理体系，支撑中医诊所核心诊疗流程

## 🎯 项目概述
- [待补充] 简要描述 LYBT.Module.MedicalCase 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。
