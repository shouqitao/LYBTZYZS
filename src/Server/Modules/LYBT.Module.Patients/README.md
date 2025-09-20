# LYBT.Module.Patients

> **患者档案管理核心模块** - 中医诊疗患者信息中心
> 完整病历管理 | 就诊历史追踪 | 健康档案维护
> **模块状态**: ✅ **生产就绪** | 🎆 **DTO优化完成** | **零编译错误** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.Patients是系统的患者管理核心模块，采用UltraThink双层架构设计，提供完整的患者档案管理、就诊历史记录、健康信息维护等功能。专为中医诊所场景优化，支持中医特色的体质辨识、过敏史记录等功能。

**技术栈**: .NET 8 + Entity Framework Core 8.0 + AutoMapper + LINQ
**最新优化**: DTO字段与实体完全对齐、PatientSearchDto命名规范化、字段类型安全增强

## 🎉 2025-09-20 DTO优化成果

### ✅ 优化完成内容
- **查询DTO规范**: PatientPagedQueryDto → PatientSearchDto，命名统一
- **字段对齐**: PatientDto字段100%与Patient实体对齐
- **类型修正**: MaritalStatus从string改为int类型
- **字段重命名**:
  - EmergencyContact → EmergencyContactName
  - EmergencyPhone → EmergencyContactPhone
- **编译状态**: 零错误零警告，完全生产就绪

## 🏗️ UltraThink双层架构设计

```
PatientService (主服务层 - 纯委托模式)
    │
    ├── PatientQueryService (查询专业化层)
    │   ├── 患者搜索和筛选 (SearchPatientsAsync)
    │   ├── 就诊历史查询 (GetMedicalHistoryAsync)
    │   ├── 统计分析 (GetPatientStatisticsAsync)
    │   └── 复杂条件查询 (GetPatientsByConditionAsync)
    │
    └── PatientBusinessService (业务逻辑+CRUD层)
        ├── 患者CRUD操作 (Create/Update/Delete/GetById)
        ├── 档案管理 (UpdateHealthInfoAsync)
        ├── 过敏史管理 (UpdateAllergiesAsync)
        └── 体质辨识 (UpdateConstitutionAsync)
```

### 核心接口设计

```csharp
// 主服务接口 (统一入口) - 2025-09-20更新
public interface IPatientService
{
    // CRUD操作
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

    // 查询操作 - 使用新的SearchDto
    Task<ServiceResult<PagedResult<PatientDto>>> SearchAsync(PatientSearchDto query);
    Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard);
    Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone);

    // 业务操作
    Task<ServiceResult<bool>> UpdateHealthInfoAsync(Guid id, HealthInfoDto dto);
    Task<ServiceResult<List<MedicalCaseDto>>> GetMedicalHistoryAsync(Guid id);
}

// 查询专业化接口
public interface IPatientQueryService
{
    Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria);
    Task<ServiceResult<List<PatientDto>>> GetRecentPatientsAsync(int days = 7);
    Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync();
    Task<ServiceResult<List<PatientDto>>> GetPatientsByConstitutionAsync(string constitution);
}

// 业务逻辑接口
public interface IPatientBusinessService
{
    Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult<bool>> DeletePatientAsync(Guid id);
    Task<ServiceResult<bool>> UpdateAllergiesAsync(Guid id, string[] allergies);
    Task<ServiceResult<bool>> UpdateConstitutionAsync(Guid id, string constitution);
}
```

## 📦 核心功能模块

### 1. 患者档案管理

**创建患者档案**:
```csharp
public async Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto)
{
    // 1. 验证身份证唯一性
    if (!string.IsNullOrEmpty(dto.IdCard))
    {
        var existing = await _repository.GetByIdCardAsync(dto.IdCard);
        if (existing != null)
            return ServiceResult<PatientDto>.Failure("该身份证号已存在");
    }

    // 2. 创建患者实体
    var patient = new Patient
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Gender = dto.Gender,
        BirthDate = dto.BirthDate,
        Phone = dto.Phone,
        IdCard = dto.IdCard,
        Address = dto.Address,
        EmergencyContactName = dto.EmergencyContactName,  // 新字段名
        EmergencyContactPhone = dto.EmergencyContactPhone,  // 新字段名
        MaritalStatus = dto.MaritalStatus,  // int类型
        Occupation = dto.Occupation,
        Allergies = dto.Allergies,
        MedicalHistory = dto.MedicalHistory,
        CreateTime = DateTime.UtcNow
    };

    // 3. 保存到数据库
    var created = await _repository.CreateAsync(patient);

    // 4. 返回DTO
    return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(created));
}
```

### 2. 患者搜索查询

**高级搜索功能**:
```csharp
public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria)
{
    var query = _repository.GetQueryable();

    // 关键词搜索（姓名、电话、身份证）
    if (!string.IsNullOrWhiteSpace(criteria.Keyword))
    {
        query = query.Where(p =>
            p.Name.Contains(criteria.Keyword) ||
            p.Phone.Contains(criteria.Keyword) ||
            p.IdCard.Contains(criteria.Keyword));
    }

    // 性别筛选
    if (criteria.Gender.HasValue)
    {
        query = query.Where(p => p.Gender == criteria.Gender.Value);
    }

    // 年龄范围筛选
    if (criteria.MinAge.HasValue)
    {
        var maxBirthDate = DateTime.Today.AddYears(-criteria.MinAge.Value);
        query = query.Where(p => p.BirthDate <= maxBirthDate);
    }
    if (criteria.MaxAge.HasValue)
    {
        var minBirthDate = DateTime.Today.AddYears(-criteria.MaxAge.Value - 1);
        query = query.Where(p => p.BirthDate >= minBirthDate);
    }

    // 创建时间范围
    if (criteria.CreateTimeFrom.HasValue)
    {
        query = query.Where(p => p.CreateTime >= criteria.CreateTimeFrom.Value);
    }
    if (criteria.CreateTimeTo.HasValue)
    {
        query = query.Where(p => p.CreateTime <= criteria.CreateTimeTo.Value);
    }

    // 分页查询
    var totalCount = await query.CountAsync();
    var patients = await query
        .OrderByDescending(p => p.CreateTime)
        .Skip((criteria.PageIndex - 1) * criteria.PageSize)
        .Take(criteria.PageSize)
        .ToListAsync();

    return ServiceResult<PagedResult<PatientDto>>.Success(new PagedResult<PatientDto>
    {
        Items = _mapper.Map<List<PatientDto>>(patients),
        TotalCount = totalCount,
        PageIndex = criteria.PageIndex,
        PageSize = criteria.PageSize
    });
}
```

### 3. 就诊历史管理

**获取患者就诊历史**:
```csharp
public async Task<ServiceResult<List<MedicalCaseDto>>> GetMedicalHistoryAsync(Guid patientId)
{
    var patient = await _repository.GetByIdAsync(patientId);
    if (patient == null)
        return ServiceResult<List<MedicalCaseDto>>.Failure("患者不存在");

    var medicalCases = await _medicalCaseRepository
        .GetByPatientIdAsync(patientId);

    var caseDtos = medicalCases
        .OrderByDescending(c => c.CreateTime)
        .Select(c => new MedicalCaseDto
        {
            Id = c.Id,
            VisitDate = c.VisitDate,
            ChiefComplaint = c.ChiefComplaint,
            Diagnosis = c.Diagnosis,
            Treatment = c.Treatment,
            DoctorName = c.Doctor?.DisplayName,
            Status = c.Status
        })
        .ToList();

    return ServiceResult<List<MedicalCaseDto>>.Success(caseDtos);
}
```

## 🧪 数据传输对象 (DTOs) - 2025-09-20更新

### 请求DTOs
```csharp
// 创建患者DTO
public class PatientCreateDto
{
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; set; }

    public DateTime? BirthDate { get; set; }

    [Required(ErrorMessage = "电话不能为空")]
    [Phone(ErrorMessage = "电话格式无效")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(18)]
    public string? IdCard { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? EmergencyContactName { get; set; }  // 原EmergencyContact

    [Phone]
    public string? EmergencyContactPhone { get; set; }  // 原EmergencyPhone

    public int MaritalStatus { get; set; } = 0;  // int类型，原string

    [StringLength(50)]
    public string? Occupation { get; set; }

    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
}

// 更新患者DTO
public class PatientUpdateDto
{
    [StringLength(50)]
    public string? Name { get; set; }

    public Gender? Gender { get; set; }
    public DateTime? BirthDate { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? EmergencyContactName { get; set; }

    [Phone]
    public string? EmergencyContactPhone { get; set; }

    public int? MaritalStatus { get; set; }  // int类型

    [StringLength(50)]
    public string? Occupation { get; set; }

    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Constitution { get; set; }  // 中医体质
}

// 患者搜索DTO (原PatientPagedQueryDto)
public class PatientSearchDto : PagedRequestDto
{
    public string? Keyword { get; set; }
    public Gender? Gender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public DateTime? CreateTimeFrom { get; set; }
    public DateTime? CreateTimeTo { get; set; }
    public string? Constitution { get; set; }  // 体质筛选
}
```

### 响应DTOs
```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? IdCard { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }  // 原EmergencyContact
    public string? EmergencyContactPhone { get; set; }  // 原EmergencyPhone
    public int MaritalStatus { get; set; }  // int类型，原string
    public string? Occupation { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Constitution { get; set; }  // 中医体质
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }

    // 计算属性
    public int? Age => BirthDate.HasValue ?
        DateTime.Today.Year - BirthDate.Value.Year : null;

    public string GenderDisplay => Gender == Gender.Male ? "男" : "女";

    public string MaritalStatusDisplay => MaritalStatus switch
    {
        0 => "未婚",
        1 => "已婚",
        2 => "离异",
        3 => "丧偶",
        _ => "未知"
    };
}

public class PatientStatisticsDto
{
    public int TotalPatients { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public int ActivePatients { get; set; }  // 近3个月有就诊
    public Dictionary<Gender, int> GenderDistribution { get; set; }
    public Dictionary<string, int> AgeDistribution { get; set; }
    public Dictionary<string, int> ConstitutionDistribution { get; set; }
}
```

## 🔧 Repository层设计

```csharp
public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public async Task<Patient?> GetByIdCardAsync(string idCard)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.IdCard == idCard && !p.IsDeleted);
    }

    public async Task<List<Patient>> GetByPhoneAsync(string phone)
    {
        return await _context.Patients
            .Where(p => p.Phone == phone && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<Patient>> GetRecentPatientsAsync(int days)
    {
        var startDate = DateTime.Today.AddDays(-days);
        return await _context.Patients
            .Where(p => p.CreateTime >= startDate && !p.IsDeleted)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetConstitutionDistributionAsync()
    {
        return await _context.Patients
            .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Constitution))
            .GroupBy(p => p.Constitution)
            .Select(g => new { Constitution = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Constitution!, x => x.Count);
    }
}
```

## 🎯 中医特色功能

### 体质辨识
- 九种体质分类：平和质、气虚质、阳虚质、阴虚质、痰湿质、湿热质、血瘀质、气郁质、特禀质
- 体质评估记录
- 体质调理建议关联

### 过敏史管理
- 药物过敏记录
- 食物过敏记录
- 过敏严重程度分级
- 处方开具时自动提醒

### 就诊关联
- 与MedicalCase模块深度集成
- 完整就诊历史追踪
- 历史处方查询
- 疗效跟踪评估

## 📚 相关文档

- [MedicalCase医案模块](../LYBT.Module.MedicalCase/README.md) - 就诊记录
- [Consultation诊察模块](../LYBT.Module.Consultation/README.md) - 四诊信息
- [Prescriptions处方模块](../LYBT.Module.Prescriptions/README.md) - 处方管理

## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PatientsController : BaseApiController
{
    private readonly IPatientService _patientService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> SearchPatients(
        [FromQuery] PatientSearchDto criteria)
    {
        var result = await _patientService.SearchAsync(criteria);
        return HandleServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> CreatePatient(
        [FromBody] PatientCreateDto dto)
    {
        var result = await _patientService.CreateAsync(dto);
        return HandleServiceResult(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> UpdatePatient(
        Guid id, [FromBody] PatientUpdateDto dto)
    {
        var result = await _patientService.UpdateAsync(id, dto);
        return HandleServiceResult(result);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> GetMedicalHistory(Guid id)
    {
        var result = await _patientService.GetMedicalHistoryAsync(id);
        return HandleServiceResult(result);
    }
}
```

---

> 📌 **最新成果**: DTO字段完全对齐，类型安全增强，零编译错误
> 🎆 **生产就绪**: 完整的患者档案管理体系，支撑中医诊所核心业务