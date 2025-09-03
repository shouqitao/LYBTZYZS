# LYBT.Module.Patients

> **患者档案管理核心模块** - UltraThink双层架构版  
> 完整患者档案管理与诊疗历史追踪 | 专为小型中医诊所(<20人)优化
> **模块状态**: ✅ **生产就绪** | 🎆 **UltraThink双层架构完成** | **零编译错误**

## 🎯 模块概述

LYBT.Module.Patients是系统的患者档案管理核心模块，采用UltraThink双层架构设计，提供完整的患者基础信息管理、快速检索和诊疗历史追踪功能。专为中医诊所场景优化，支持患者全生命周期管理。

**技术栈**: UltraThink双层架构 + Entity Framework Core + AutoMapper + 优化查询缓存

## 🎆 UltraThink双层架构设计

**架构层次**:
```
PatientService (主服务层 - 纯委托模式)
    │
    ├── PatientQueryService (查询专业化层)
    │   ├── 患者搜索和筛选 (SearchPatientsAsync)
    │   ├── 统计分析 (GetPatientStatisticsAsync) 
    │   ├── 诊疗历史查询 (GetPatientHistoryAsync)
    │   ├── 快速查找 (QuickSearchAsync)
    │   └── 复杂条件查询 (GetPatientsByConditionAsync)
    │
    └── PatientBusinessService (业务逻辑+CRUD层)
        ├── 患者CRUD操作 (Create/Update/Delete/GetById)
        ├── 档案管理 (ArchivePatientAsync, RestorePatientAsync)
        ├── 状态管理 (UpdatePatientStatusAsync)
        ├── 联系方式更新 (UpdateContactInfoAsync)
        ├── 数据导入导出 (ImportPatientsAsync, ExportPatientsAsync)
        └── 业务验证逻辑 (ValidatePatientData, CheckDuplicates)
```

### 核心接口设计

```csharp
// 主服务接口 (统一入口)
public interface IPatientService
{
    // 委托到BusinessService的CRUD操作
    Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto);
    Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientDto dto);
    Task<ServiceResult<bool>> DeletePatientAsync(Guid id);
    Task<ServiceResult<PatientDto?>> GetByIdAsync(Guid id);
    
    // 委托到QueryService的查询操作
    Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria);
    Task<ServiceResult<List<PatientDto>>> QuickSearchAsync(string keyword);
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync();
    Task<ServiceResult<List<PatientHistoryDto>>> GetPatientHistoryAsync(Guid patientId);
    
    // 委托到BusinessService的业务操作
    Task<ServiceResult<bool>> UpdateContactInfoAsync(Guid id, UpdateContactDto dto);
    Task<ServiceResult<bool>> ArchivePatientAsync(Guid id, string reason);
    Task<ServiceResult<List<PatientDto>>> ImportPatientsAsync(ImportPatientsDto dto);
}

// 查询专业化接口
public interface IPatientQueryService
{
    Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria);
    Task<ServiceResult<List<PatientDto>>> QuickSearchAsync(string keyword);
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync();
    Task<ServiceResult<List<PatientHistoryDto>>> GetPatientHistoryAsync(Guid patientId);
    Task<ServiceResult<List<PatientDto>>> GetRecentPatientsAsync(int count = 10);
}

// 业务逻辑接口
public interface IPatientBusinessService
{
    Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto);
    Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientDto dto);
    Task<ServiceResult<bool>> DeletePatientAsync(Guid id);
    Task<ServiceResult<PatientDto?>> GetByIdAsync(Guid id);
    Task<ServiceResult<bool>> UpdateContactInfoAsync(Guid id, UpdateContactDto dto);
    Task<ServiceResult<ValidationResult>> ValidatePatientDataAsync(PatientDataDto dto);
}
```

## 📦 核心功能模块

### 1. 患者档案管理

**创建患者档案流程**:
```csharp
public async Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto)
{
    try
    {
        // 1. 数据验证
        var validationResult = await ValidatePatientDataAsync(dto);
        if (!validationResult.IsSuccess)
            return ServiceResult<PatientDto>.Failure(validationResult.Message);
        
        // 2. 检查重复患者
        var duplicateCheck = await CheckForDuplicatesAsync(dto);
        if (duplicateCheck.HasDuplicates)
        {
            return ServiceResult<PatientDto>.Failure(
                $"发现可能重复的患者记录: {string.Join(", ", duplicateCheck.SimilarPatients)}");
        }
        
        // 3. 创建患者实体
        var patient = new PatientModel
        {
            Name = dto.Name.Trim(),
            Gender = dto.Gender,
            Age = dto.Age,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IdNumber = dto.IdNumber?.Trim(),
            Address = dto.Address?.Trim(),
            EmergencyContact = dto.EmergencyContact?.Trim(),
            EmergencyPhone = dto.EmergencyPhone?.Trim(),
            Allergies = dto.Allergies?.Trim(),
            MedicalHistory = dto.MedicalHistory?.Trim(),
            Remarks = dto.Remarks?.Trim(),
            Status = PatientStatus.Active
        };
        
        // 4. 保存到数据库
        var createdPatient = await _repository.CreateAsync(patient);
        
        // 5. 记录操作日志
        _logger.LogInformation("患者档案创建成功: {PatientName}, ID: {PatientId}", 
            dto.Name, createdPatient.Id);
        
        // 6. 返回DTO
        return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(createdPatient));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建患者档案时发生错误: {PatientName}", dto.Name);
        return ServiceResult<PatientDto>.Failure("创建患者档案失败");
    }
}
```

### 2. 高级搜索和查询

**多维度搜索功能**:
```csharp
public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria)
{
    try
    {
        var query = _repository.GetQueryable();
        
        // 关键词搜索 (姓名、电话、身份证)
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword.Trim().ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(keyword) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                (p.IdNumber != null && p.IdNumber.ToLower().Contains(keyword)));
        }
        
        // 性别筛选
        if (criteria.Gender.HasValue)
        {
            query = query.Where(p => p.Gender == criteria.Gender.Value);
        }
        
        // 年龄范围
        if (criteria.AgeFrom.HasValue)
        {
            query = query.Where(p => p.Age >= criteria.AgeFrom.Value);
        }
        if (criteria.AgeTo.HasValue)
        {
            query = query.Where(p => p.Age <= criteria.AgeTo.Value);
        }
        
        // 状态筛选
        if (criteria.Status.HasValue)
        {
            query = query.Where(p => p.Status == criteria.Status.Value);
        }
        
        // 创建时间范围 (注册时间)
        if (criteria.RegisterTimeFrom.HasValue)
        {
            query = query.Where(p => p.CreateTime >= criteria.RegisterTimeFrom.Value);
        }
        if (criteria.RegisterTimeTo.HasValue)
        {
            query = query.Where(p => p.CreateTime <= criteria.RegisterTimeTo.Value);
        }
        
        // 过敏史筛选
        if (!string.IsNullOrWhiteSpace(criteria.AllergyKeyword))
        {
            query = query.Where(p => p.Allergies != null && 
                p.Allergies.ToLower().Contains(criteria.AllergyKeyword.ToLower()));
        }
        
        // 排序
        query = criteria.SortBy?.ToLower() switch
        {
            "name" => criteria.SortDescending ? 
                query.OrderByDescending(p => p.Name) : 
                query.OrderBy(p => p.Name),
            "age" => criteria.SortDescending ? 
                query.OrderByDescending(p => p.Age) : 
                query.OrderBy(p => p.Age),
            "createtime" => criteria.SortDescending ? 
                query.OrderByDescending(p => p.CreateTime) : 
                query.OrderBy(p => p.CreateTime),
            _ => query.OrderByDescending(p => p.CreateTime) // 默认按注册时间倒序
        };
        
        // 分页查询
        var totalCount = await query.CountAsync();
        var patients = await query
            .Skip((criteria.PageIndex - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();
            
        // 加载诊疗次数统计
        var patientIds = patients.Select(p => p.Id).ToList();
        var visitCounts = await _context.MedicalCases
            .Where(mc => patientIds.Contains(mc.PatientId))
            .GroupBy(mc => mc.PatientId)
            .Select(g => new { PatientId = g.Key, VisitCount = g.Count() })
            .ToListAsync();
            
        var patientDtos = _mapper.Map<List<PatientDto>>(patients);
        
        // 填充诊疗次数
        foreach (var dto in patientDtos)
        {
            dto.TotalVisits = visitCounts
                .FirstOrDefault(vc => vc.PatientId == dto.Id)?.VisitCount ?? 0;
        }
        
        var result = new PagedResult<PatientDto>
        {
            Items = patientDtos,
            TotalCount = totalCount,
            PageIndex = criteria.PageIndex,
            PageSize = criteria.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
        };
        
        return ServiceResult<PagedResult<PatientDto>>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "搜索患者时发生错误");
        return ServiceResult<PagedResult<PatientDto>>.Failure("搜索患者失败");
    }
}
```

### 3. 快速检索功能

**智能快速搜索**:
```csharp
public async Task<ServiceResult<List<PatientDto>>> QuickSearchAsync(string keyword)
{
    try
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            
        var searchKey = keyword.Trim().ToLower();
        
        // 缓存键
        var cacheKey = $"QuickSearch_{searchKey.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out List<PatientDto>? cachedResult))
            return ServiceResult<List<PatientDto>>.Success(cachedResult);
        
        // 智能搜索策略
        var patients = await _repository.GetQueryable()
            .Where(p => p.Status == PatientStatus.Active)
            .Where(p => 
                p.Name.ToLower().Contains(searchKey) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                (p.IdNumber != null && p.IdNumber.ToLower().Contains(searchKey)))
            .OrderBy(p => p.Name.ToLower().StartsWith(searchKey) ? 0 : 1) // 姓名开头匹配优先
            .ThenBy(p => p.Name)
            .Take(10) // 限制返回数量
            .ToListAsync();
            
        var result = _mapper.Map<List<PatientDto>>(patients);
        
        // 缓存结果 (3分钟)
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(3));
        
        return ServiceResult<List<PatientDto>>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "快速搜索患者时发生错误，关键词: {Keyword}", keyword);
        return ServiceResult<List<PatientDto>>.Failure("快速搜索失败");
    }
}
```

### 4. 患者统计分析

**统计数据生成**:
```csharp
public async Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync()
{
    try
    {
        var totalPatients = await _repository.CountAsync();
        var activePatients = await _repository.CountAsync(p => p.Status == PatientStatus.Active);
        
        // 性别统计
        var maleCount = await _repository.CountAsync(p => p.Gender == Gender.Male);
        var femaleCount = await _repository.CountAsync(p => p.Gender == Gender.Female);
        
        // 年龄段统计
        var ageGroups = await _repository.GetQueryable()
            .Where(p => p.Status == PatientStatus.Active)
            .GroupBy(p => 
                p.Age < 18 ? "儿童" :
                p.Age < 35 ? "青年" :
                p.Age < 60 ? "中年" : "老年")
            .Select(g => new AgeGroupStatistic 
            { 
                AgeGroup = g.Key, 
                Count = g.Count() 
            })
            .ToListAsync();
        
        // 近期新增患者
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        var newPatientsLast30Days = await _repository
            .CountAsync(p => p.CreateTime >= thirtyDaysAgo);
            
        // 最近就诊患者
        var recentVisitPatients = await _context.MedicalCases
            .Where(mc => mc.CreateTime >= thirtyDaysAgo)
            .Select(mc => mc.PatientId)
            .Distinct()
            .CountAsync();
            
        // 有过敏史的患者
        var patientsWithAllergies = await _repository
            .CountAsync(p => !string.IsNullOrEmpty(p.Allergies));
            
        var statistics = new PatientStatisticsDto
        {
            TotalPatients = totalPatients,
            ActivePatients = activePatients,
            InactivePatients = totalPatients - activePatients,
            MalePatients = maleCount,
            FemalePatients = femaleCount,
            NewPatientsLast30Days = newPatientsLast30Days,
            RecentVisitPatients = recentVisitPatients,
            PatientsWithAllergies = patientsWithAllergies,
            AgeGroupDistribution = ageGroups,
            AverageAge = await _repository.GetQueryable()
                .Where(p => p.Status == PatientStatus.Active)
                .AverageAsync(p => (double?)p.Age) ?? 0
        };
        
        return ServiceResult<PatientStatisticsDto>.Success(statistics);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取患者统计数据时发生错误");
        return ServiceResult<PatientStatisticsDto>.Failure("获取统计数据失败");
    }
}
```

### 5. 诊疗历史查询

**患者就诊历史**:
```csharp
public async Task<ServiceResult<List<PatientHistoryDto>>> GetPatientHistoryAsync(Guid patientId)
{
    try
    {
        // 验证患者存在
        var patient = await _repository.GetByIdAsync(patientId);
        if (patient == null)
            return ServiceResult<List<PatientHistoryDto>>.Failure("患者不存在");
            
        // 查询医疗案例历史
        var medicalCases = await _context.MedicalCases
            .Where(mc => mc.PatientId == patientId)
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Doctor)
            .OrderByDescending(mc => mc.CreateTime)
            .ToListAsync();
            
        var history = new List<PatientHistoryDto>();
        
        foreach (var medicalCase in medicalCases)
        {
            var historyItem = new PatientHistoryDto
            {
                VisitDate = medicalCase.CreateTime,
                MedicalCaseId = medicalCase.Id,
                DoctorName = medicalCase.Doctor?.DisplayName ?? medicalCase.Doctor?.Username ?? "未知医生",
                ChiefComplaint = medicalCase.Consultation?.ChiefComplaint ?? "",
                TcmDiagnosis = medicalCase.Consultation?.TcmDiagnosis ?? "",
                Status = medicalCase.Status,
                HasPrescription = await _context.Prescriptions
                    .AnyAsync(p => p.MedicalCaseId == medicalCase.Id)
            };
            
            // 获取处方信息
            if (historyItem.HasPrescription)
            {
                var prescription = await _context.Prescriptions
                    .Where(p => p.MedicalCaseId == medicalCase.Id)
                    .Include(p => p.Items)
                    .ThenInclude(i => i.Herb)
                    .FirstOrDefaultAsync();
                    
                if (prescription != null)
                {
                    historyItem.PrescriptionSummary = string.Join("、", 
                        prescription.Items.Take(3).Select(i => i.Herb.Name));
                    if (prescription.Items.Count > 3)
                        historyItem.PrescriptionSummary += "等";
                }
            }
            
            history.Add(historyItem);
        }
        
        return ServiceResult<List<PatientHistoryDto>>.Success(history);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取患者诊疗历史时发生错误，患者ID: {PatientId}", patientId);
        return ServiceResult<List<PatientHistoryDto>>.Failure("获取诊疗历史失败");
    }
}
```

## 🔧 Repository层设计

### PatientRepository (优化版)
```csharp
public class OptimizedPatientRepository : BaseRepository<PatientModel>, IPatientRepository
{
    public async Task<PatientModel?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber && !p.IsDeleted);
    }
    
    public async Task<PatientModel?> GetByIdNumberAsync(string idNumber)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber && !p.IsDeleted);
    }
    
    public async Task<List<PatientModel>> GetSimilarPatientsAsync(string name, DateTime? dateOfBirth, string? phoneNumber)
    {
        var query = _context.Patients.Where(p => !p.IsDeleted);
        
        // 姓名相似度检查 (简化版)
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(p => p.Name.Contains(name) || name.Contains(p.Name));
        }
        
        // 出生日期匹配
        if (dateOfBirth.HasValue)
        {
            query = query.Where(p => p.DateOfBirth == dateOfBirth.Value);
        }
        
        // 电话号码匹配
        if (!string.IsNullOrEmpty(phoneNumber))
        {
            query = query.Where(p => p.PhoneNumber == phoneNumber);
        }
        
        return await query.Take(5).ToListAsync();
    }
    
    public async Task<List<PatientModel>> GetRecentPatientsAsync(int count)
    {
        return await _context.Patients
            .Where(p => p.Status == PatientStatus.Active && !p.IsDeleted)
            .OrderByDescending(p => p.UpdateTime ?? p.CreateTime)
            .Take(count)
            .ToListAsync();
    }
    
    public async Task<int> CountByAgeRangeAsync(int minAge, int maxAge)
    {
        return await _context.Patients
            .CountAsync(p => p.Age >= minAge && p.Age <= maxAge && 
                           p.Status == PatientStatus.Active && !p.IsDeleted);
    }
    
    public async Task<Dictionary<Gender, int>> GetGenderDistributionAsync()
    {
        return await _context.Patients
            .Where(p => p.Status == PatientStatus.Active && !p.IsDeleted)
            .GroupBy(p => p.Gender)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}
```

## 🧪 数据传输对象 (DTOs)

### 请求DTOs
```csharp
public record CreatePatientDto
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50字符")]
    public string Name { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; init; }
    
    [Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]
    public int Age { get; init; }
    
    public DateTime? DateOfBirth { get; init; }
    
    [Phone(ErrorMessage = "手机号码格式不正确")]
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20字符")]
    public string? PhoneNumber { get; init; }
    
    [StringLength(18, ErrorMessage = "身份证号码长度不能超过18字符")]
    public string? IdNumber { get; init; }
    
    [StringLength(200, ErrorMessage = "地址长度不能超过200字符")]
    public string? Address { get; init; }
    
    [StringLength(50, ErrorMessage = "紧急联系人姓名长度不能超过50字符")]
    public string? EmergencyContact { get; init; }
    
    [Phone(ErrorMessage = "紧急联系人电话格式不正确")]
    [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20字符")]
    public string? EmergencyPhone { get; init; }
    
    [StringLength(500, ErrorMessage = "过敏史长度不能超过500字符")]
    public string? Allergies { get; init; }
    
    [StringLength(1000, ErrorMessage = "既往病史长度不能超过1000字符")]
    public string? MedicalHistory { get; init; }
    
    [StringLength(500, ErrorMessage = "备注长度不能超过500字符")]
    public string? Remarks { get; init; }
}

public record PatientSearchDto : PagedRequestDto
{
    public string? Keyword { get; init; }
    public Gender? Gender { get; init; }
    public int? AgeFrom { get; init; }
    public int? AgeTo { get; init; }
    public PatientStatus? Status { get; init; }
    public DateTime? RegisterTimeFrom { get; init; }
    public DateTime? RegisterTimeTo { get; init; }
    public string? AllergyKeyword { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;
}

public record UpdateContactDto
{
    [Phone(ErrorMessage = "手机号码格式不正确")]
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20字符")]
    public string? PhoneNumber { get; init; }
    
    [StringLength(200, ErrorMessage = "地址长度不能超过200字符")]
    public string? Address { get; init; }
    
    [StringLength(50, ErrorMessage = "紧急联系人姓名长度不能超过50字符")]
    public string? EmergencyContact { get; init; }
    
    [Phone(ErrorMessage = "紧急联系人电话格式不正确")]
    [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20字符")]
    public string? EmergencyPhone { get; init; }
}
```

### 响应DTOs
```csharp
public record PatientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public int Age { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PhoneNumber { get; init; }
    public string? IdNumber { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalHistory { get; init; }
    public string? Remarks { get; init; }
    public PatientStatus Status { get; init; }
    public DateTime CreateTime { get; init; }
    public DateTime? UpdateTime { get; init; }
    
    // 计算属性
    public string GenderDisplay => Gender == Gender.Male ? "男" : 
                                 Gender == Gender.Female ? "女" : "其他";
    public string StatusDisplay => Status switch
    {
        PatientStatus.Active => "正常",
        PatientStatus.Inactive => "停用",
        PatientStatus.Archived => "归档",
        _ => "未知"
    };
    public int TotalVisits { get; set; }
    public DateTime? LastVisitTime { get; init; }
    public bool HasAllergies => !string.IsNullOrEmpty(Allergies);
    public int CalculatedAge => DateOfBirth.HasValue ? 
        DateTime.Now.Year - DateOfBirth.Value.Year : Age;
}

public record PatientStatisticsDto
{
    public int TotalPatients { get; init; }
    public int ActivePatients { get; init; }
    public int InactivePatients { get; init; }
    public int MalePatients { get; init; }
    public int FemalePatients { get; init; }
    public int NewPatientsLast30Days { get; init; }
    public int RecentVisitPatients { get; init; }
    public int PatientsWithAllergies { get; init; }
    public double AverageAge { get; init; }
    public List<AgeGroupStatistic> AgeGroupDistribution { get; init; } = new();
}

public record PatientHistoryDto
{
    public DateTime VisitDate { get; init; }
    public Guid MedicalCaseId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string ChiefComplaint { get; init; } = string.Empty;
    public string TcmDiagnosis { get; init; } = string.Empty;
    public MedicalCaseStatus Status { get; init; }
    public bool HasPrescription { get; init; }
    public string? PrescriptionSummary { get; init; }
}

public record AgeGroupStatistic
{
    public string AgeGroup { get; init; } = string.Empty;
    public int Count { get; init; }
}
```

## 🔒 数据验证和重复检查

### 重复患者检测
```csharp
private async Task<DuplicateCheckResult> CheckForDuplicatesAsync(CreatePatientDto dto)
{
    var similarPatients = new List<string>();
    
    // 检查完全相同的姓名和生日
    if (dto.DateOfBirth.HasValue)
    {
        var exactMatch = await _repository.GetQueryable()
            .Where(p => p.Name == dto.Name && p.DateOfBirth == dto.DateOfBirth.Value)
            .Select(p => p.Name)
            .FirstOrDefaultAsync();
            
        if (exactMatch != null)
            similarPatients.Add($"姓名生日完全匹配: {exactMatch}");
    }
    
    // 检查相同电话号码
    if (!string.IsNullOrEmpty(dto.PhoneNumber))
    {
        var phoneMatch = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
        if (phoneMatch != null)
            similarPatients.Add($"电话号码重复: {phoneMatch.Name}");
    }
    
    // 检查身份证号码
    if (!string.IsNullOrEmpty(dto.IdNumber))
    {
        var idMatch = await _repository.GetByIdNumberAsync(dto.IdNumber);
        if (idMatch != null)
            similarPatients.Add($"身份证号码重复: {idMatch.Name}");
    }
    
    return new DuplicateCheckResult
    {
        HasDuplicates = similarPatients.Any(),
        SimilarPatients = similarPatients
    };
}
```

### 数据验证增强
```csharp
private async Task<ValidationResult> ValidatePatientDataAsync(PatientDataDto dto)
{
    var errors = new List<string>();
    
    // 姓名格式验证
    if (string.IsNullOrWhiteSpace(dto.Name))
        errors.Add("患者姓名不能为空");
    else if (dto.Name.Length > 50)
        errors.Add("患者姓名长度不能超过50字符");
    else if (Regex.IsMatch(dto.Name, @"[0-9]"))
        errors.Add("患者姓名不能包含数字");
    
    // 年龄和生日验证
    if (dto.DateOfBirth.HasValue)
    {
        var calculatedAge = DateTime.Now.Year - dto.DateOfBirth.Value.Year;
        if (Math.Abs(calculatedAge - dto.Age) > 1)
            errors.Add("年龄与出生日期不符");
    }
    
    // 电话号码验证
    if (!string.IsNullOrEmpty(dto.PhoneNumber) && 
        !Regex.IsMatch(dto.PhoneNumber, @"^1[3-9]\d{9}$"))
        errors.Add("手机号码格式不正确");
    
    // 身份证号码验证
    if (!string.IsNullOrEmpty(dto.IdNumber) && 
        !IsValidIdNumber(dto.IdNumber))
        errors.Add("身份证号码格式不正确");
    
    return new ValidationResult
    {
        IsValid = !errors.Any(),
        Message = errors.Any() ? string.Join("；", errors) : "数据验证通过",
        Errors = errors
    };
}

private static bool IsValidIdNumber(string idNumber)
{
    // 简化的身份证验证
    return Regex.IsMatch(idNumber, @"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dX]$");
}
```

## 🎯 UltraThink架构优势

**适合小型中医诊所(<20人)的精简设计**:
- ✅ **双层架构**: Query+Business分层，查询优化，业务清晰
- ✅ **智能搜索**: 多维度检索，快速定位患者档案
- ✅ **重复检测**: 防止重复建档，保证数据质量
- ✅ **诊疗追踪**: 完整的就诊历史，支持医疗连续性
- ✅ **统计分析**: 患者结构分析，支持诊所运营决策
- ✅ **缓存优化**: 快速搜索缓存，提升用户体验

## 📚 相关文档

- [MedicalCase医疗案例](../LYBT.Module.MedicalCase/README.md) - 患者就诊流程管理
- [Consultation诊断记录](../LYBT.Module.Consultation/README.md) - 患者诊疗记录
- [Infrastructure基础设施](../../Core/LYBT.Infrastructure/README.md) - Repository基类和缓存

## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PatientsController : BaseApiController
{
    private readonly IPatientService _patientService;
    
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> SearchPatients([FromQuery] PatientSearchDto criteria)
    {
        var result = await _patientService.SearchPatientsAsync(criteria);
        return HandleServiceResult(result, "获取患者列表成功");
    }
    
    [HttpGet("quick-search")]
    public async Task<ActionResult<ApiResponse<List<PatientDto>>>> QuickSearch([FromQuery] string keyword)
    {
        var result = await _patientService.QuickSearchAsync(keyword);
        return HandleServiceResult(result, "快速搜索成功");
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> CreatePatient([FromBody] CreatePatientDto dto)
    {
        var result = await _patientService.CreatePatientAsync(dto);
        return HandleServiceResult(result, "创建患者档案成功");
    }
    
    [HttpGet("{id}/history")]
    public async Task<ActionResult<ApiResponse<List<PatientHistoryDto>>>> GetPatientHistory(Guid id)
    {
        var result = await _patientService.GetPatientHistoryAsync(id);
        return HandleServiceResult(result, "获取患者诊疗历史成功");
    }
}
```

### 前端集成示例
```csharp
// WPF前端快速搜索
private async void QuickSearch_TextChanged(object sender, TextChangedEventArgs e)
{
    var keyword = ((TextBox)sender).Text;
    if (string.IsNullOrWhiteSpace(keyword)) return;
    
    var result = await _patientService.QuickSearchAsync(keyword);
    if (result.Success)
    {
        PatientSuggestions.ItemsSource = result.Data;
        PatientSuggestions.IsOpen = result.Data.Count > 0;
    }
}
```

---

> 📌 **UltraThink成果**: Patients模块采用双层架构，实现智能搜索和完整档案管理
> 🎆 **生产就绪**: 零编译错误，完整的患者管理体系，支持中医诊所患者全生命周期管理