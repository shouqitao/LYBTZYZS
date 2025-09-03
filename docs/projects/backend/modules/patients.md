# LYBT.Module.Patients 项目文档

## 📋 项目概述

**LYBT.Module.Patients**是凌隐宝堂中医诊所系统的患者档案管理核心模块，负责患者基础信息的完整生命周期管理。作为诊疗流程的起点，Patients模块提供患者档案建立、信息维护、就诊历史跟踪和患者接待功能，为整个诊疗体系提供可靠的患者数据基础。

### 项目职责
- **患者档案管理**: 患者基础信息的创建、更新、查询和归档
- **接待流程支持**: 患者登记、信息核实和就诊前准备
- **就诊历史跟踪**: 患者历次就诊记录和诊疗轨迹管理
- **紧急联系人管理**: 患者紧急联系信息的维护和更新
- **患者搜索查询**: 高效的患者信息检索和筛选功能
- **数据导入导出**: 患者数据的批量导入和统计报表导出

### 在系统中的位置
Patients模块是整个诊疗流程的数据源头，为MedicalCase、Consultation、Prescriptions等业务模块提供患者基础数据。它与所有诊疗相关模块都有紧密的数据关联，是医疗信息系统的核心支撑。

### 关键业务价值
- **诊疗基础**: 为所有医疗活动提供准确的患者身份信息
- **历史追溯**: 完整的患者就诊历史支持连续性医疗服务
- **高效接待**: 简化患者登记流程，提升接待效率
- **信息完整性**: 确保患者联系方式和基础信息的准确性

## 🏗️ 技术架构

### 项目架构设计
Patients模块采用UltraThink双层架构标准，解决了字段映射问题，优化了导入导出功能：

```
PatientService (纯委托层)
    ├── PatientQueryService (查询专业层)
    │   ├── 患者搜索和筛选查询
    │   ├── 患者统计和报表查询
    │   ├── 就诊历史关联查询
    │   └── 患者年龄分布统计查询
    └── PatientBusinessService (业务逻辑层)
        ├── 患者CRUD操作和验证
        ├── 患者档案完整性检查
        ├── 紧急联系人信息管理
        ├── 患者数据导入导出
        └── 患者状态变更流程
```

### 核心技术栈
- **.NET 8.0**: 现代C#语言特性和高性能运行时
- **Entity Framework Core 8.0.17**: ORM框架，支持复杂关联查询
- **AutoMapper**: 实体和DTO自动映射，解决字段映射问题
- **FluentValidation**: 患者信息业务规则验证
- **EPPlus**: Excel文件处理，支持患者数据导入导出
- **Microsoft.Extensions.Logging**: 结构化患者操作日志
- **Microsoft.Extensions.Caching.Memory**: 患者信息缓存优化

### 依赖项目列表
**直接依赖**:
- `LYBT.Infrastructure` - 数据访问和基础服务支持
- `LYBT.Entities` - PatientModel实体定义
- `LYBT.Shared.Models` - 患者相关DTO定义
- `LYBT.Shared.Interfaces` - 患者服务接口契约
- `LYBT.Shared.Utilities` - 数据验证和格式化工具

**被依赖项目**:
- `LYBT.Module.MedicalCase` - 医疗案例患者关联
- `LYBT.Module.Consultation` - 诊断记录患者信息
- `LYBT.Module.Prescriptions` - 处方患者关联
- `LYBT.WebAPI` - 控制器层调用患者服务

### 设计模式采用
- **Repository Pattern**: 通过Infrastructure的统一数据访问
- **Service Pattern**: UltraThink双层服务架构
- **Specification Pattern**: 复杂患者查询条件的组合
- **Factory Pattern**: 患者档案创建和初始化
- **Builder Pattern**: 患者搜索条件构建器

## 🎯 功能规范

### 必须实现的功能清单

#### 1. 患者档案CRUD功能
- ✅ **创建患者档案**: 新患者信息登记，包含基础信息和联系方式
- ✅ **更新患者信息**: 患者基础信息修改，联系方式更新，紧急联系人维护
- ✅ **患者详情查询**: 完整患者信息展示，包含就诊历史统计
- ✅ **删除患者档案**: 软删除机制，保留历史就诊记录
- ✅ **患者列表查询**: 分页查询，支持多条件筛选和排序

#### 2. 高级搜索功能
- ✅ **姓名搜索**: 按患者姓名模糊搜索，支持拼音检索
- ✅ **联系方式搜索**: 按电话号码、身份证号码精确搜索
- ✅ **年龄范围筛选**: 按出生日期计算年龄范围筛选
- ✅ **性别筛选**: 按男性、女性、其他性别筛选
- ✅ **就诊时间筛选**: 按首次就诊或最后就诊时间筛选
- ✅ **复合条件查询**: 多个筛选条件的组合查询

#### 3. 患者接待功能
- ✅ **快速登记**: 新患者快速信息录入和档案创建
- ✅ **身份验证**: 通过姓名、电话等信息验证患者身份
- ✅ **信息核实**: 患者基础信息的确认和更新
- ✅ **就诊准备**: 患者信息准备和医生接诊信息提供
- ✅ **接待记录**: 患者接待过程和信息变更记录

#### 4. 数据导入导出功能
- ✅ **Excel导入**: 批量导入患者基础信息，支持数据验证
- ✅ **Excel导出**: 患者列表导出为Excel格式，支持自定义字段
- ✅ **CSV导入导出**: 患者数据CSV格式的导入导出支持
- ✅ **导入验证**: 导入数据的格式验证和重复检查
- ✅ **导出模板**: 标准患者信息导入模板提供

#### 5. 统计分析功能
- ✅ **患者统计**: 按性别、年龄段、地区的患者分布统计
- ✅ **增长趋势**: 患者注册和就诊增长趋势分析
- ✅ **活跃度分析**: 患者就诊频率和活跃度统计
- ✅ **地域分布**: 患者来源地域分析和统计
- ✅ **年龄结构**: 患者年龄分布和结构分析

### 接口定义规范

#### IPatientService主服务接口
```csharp
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientSearchDto searchDto);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria);
    Task<ServiceResult<List<PatientDto>>> GetPatientsByAgeRangeAsync(int minAge, int maxAge);
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync();
    Task<ServiceResult<List<PatientDto>>> ImportPatientsAsync(List<PatientImportDto> patients);
    Task<ServiceResult<byte[]>> ExportPatientsAsync(PatientExportOptionsDto options);
}
```

#### IPatientQueryService查询服务接口
```csharp
public interface IPatientQueryService
{
    Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria);
    Task<ServiceResult<List<PatientDto>>> GetPatientsByGenderAsync(Gender gender);
    Task<ServiceResult<List<PatientDto>>> GetPatientsByAgeRangeAsync(int minAge, int maxAge);
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync();
    Task<ServiceResult<List<PatientAgeGroupDto>>> GetPatientAgeDistributionAsync();
    Task<ServiceResult<bool>> IsPatientPhoneExistsAsync(string phone);
    Task<ServiceResult<bool>> IsPatientIdNumberExistsAsync(string idNumber);
    Task<ServiceResult<List<PatientDto>>> GetRecentlyRegisteredAsync(int days);
    Task<ServiceResult<List<PatientMedicalHistoryDto>>> GetPatientMedicalHistoryAsync(Guid patientId);
}
```

#### IPatientBusinessService业务服务接口
```csharp
public interface IPatientBusinessService
{
    Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult<bool>> DeletePatientAsync(Guid id);
    Task<ServiceResult<PatientDto>> RegisterNewPatientAsync(PatientRegistrationDto dto);
    Task<ServiceResult<bool>> VerifyPatientIdentityAsync(PatientIdentityDto dto);
    Task<ServiceResult<bool>> UpdateEmergencyContactAsync(Guid patientId, EmergencyContactDto dto);
    Task<ServiceResult<List<PatientDto>>> ImportPatientsFromExcelAsync(byte[] excelData);
    Task<ServiceResult<byte[]>> ExportPatientsToExcelAsync(PatientExportOptionsDto options);
    Task<ServiceResult<bool>> MergeDuplicatePatientsAsync(Guid keepPatientId, List<Guid> mergePatientIds);
}
```

### 数据模型定义

#### PatientCreateDto患者创建
```csharp
public class PatientCreateDto
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "患者姓名长度必须在2-20个字符之间")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; set; }
    
    [Required(ErrorMessage = "出生日期不能为空")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
    [Phone(ErrorMessage = "电话号码格式不正确")]
    public string? Phone { get; set; }
    
    [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
    [RegularExpression(@"^\d{17}[\dXx]$", ErrorMessage = "身份证号格式不正确")]
    public string? IdNumber { get; set; }
    
    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    public string? Address { get; set; }
    
    [StringLength(20, MinimumLength = 2, ErrorMessage = "紧急联系人姓名长度必须在2-20个字符之间")]
    public string? EmergencyContactName { get; set; }
    
    [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20个字符")]
    [Phone(ErrorMessage = "紧急联系人电话格式不正确")]
    public string? EmergencyContactPhone { get; set; }
    
    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Remark { get; set; }
}
```

#### PatientUpdateDto患者更新
```csharp
public class PatientUpdateDto
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "患者姓名长度必须在2-20个字符之间")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; set; }
    
    [Required(ErrorMessage = "出生日期不能为空")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
    [Phone(ErrorMessage = "电话号码格式不正确")]
    public string? Phone { get; set; }
    
    [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
    [RegularExpression(@"^\d{17}[\dXx]$", ErrorMessage = "身份证号格式不正确")]
    public string? IdNumber { get; set; }
    
    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    public string? Address { get; set; }
    
    [StringLength(20, MinimumLength = 2, ErrorMessage = "紧急联系人姓名长度必须在2-20个字符之间")]
    public string? EmergencyContactName { get; set; }
    
    [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20个字符")]
    [Phone(ErrorMessage = "紧急联系人电话格式不正确")]
    public string? EmergencyContactPhone { get; set; }
    
    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Remark { get; set; }
    
    public CommonStatus Status { get; set; }
}
```

#### PatientDto患者信息DTO
```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string GenderName => Gender.ToString();
    public DateTime DateOfBirth { get; set; }
    public int Age => DateTime.Today.Year - DateOfBirth.Year - 
        (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
    public string? Phone { get; set; }
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Remark { get; set; }
    public CommonStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    
    // 关联统计信息
    public int TotalMedicalCases { get; set; }
    public int TotalConsultations { get; set; }
    public int TotalPrescriptions { get; set; }
    public DateTime? FirstVisitTime { get; set; }
    public DateTime? LastVisitTime { get; set; }
    public string? LastDiagnosis { get; set; }
}
```

#### PatientSearchDto患者搜索条件
```csharp
public class PatientSearchDto : BaseSearchDto
{
    public string? Keyword { get; set; }
    public Gender? Gender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public DateTime? BirthDateStart { get; set; }
    public DateTime? BirthDateEnd { get; set; }
    public DateTime? FirstVisitStart { get; set; }
    public DateTime? FirstVisitEnd { get; set; }
    public DateTime? LastVisitStart { get; set; }
    public DateTime? LastVisitEnd { get; set; }
    public CommonStatus? Status { get; set; }
    public bool? HasPhone { get; set; }
    public bool? HasIdNumber { get; set; }
    public bool? HasEmergencyContact { get; set; }
    public string? SortBy { get; set; } = "CreateTime";
    public bool SortDescending { get; set; } = true;
}
```

#### PatientStatisticsDto患者统计
```csharp
public class PatientStatisticsDto
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int MalePatients { get; set; }
    public int FemalePatients { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public int RecentlyVisitedPatients { get; set; }
    public double AverageAge { get; set; }
    public int PatientsWithPhone { get; set; }
    public int PatientsWithIdNumber { get; set; }
    public int PatientsWithEmergencyContact { get; set; }
    
    public List<PatientAgeGroupDto> AgeDistribution { get; set; } = new();
    public List<PatientGenderStatDto> GenderDistribution { get; set; } = new();
    public List<PatientRegistrationTrendDto> RegistrationTrend { get; set; } = new();
    public List<PatientVisitFrequencyDto> VisitFrequency { get; set; } = new();
}
```

#### PatientImportDto患者导入
```csharp
public class PatientImportDto
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Remark { get; set; }
    
    // 验证结果
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public int RowNumber { get; set; }
}
```

### 业务规则约束
1. **姓名规则**: 患者姓名必填，长度2-20字符，支持中文、英文和少数民族文字
2. **年龄计算**: 根据出生日期自动计算年龄，支持新生儿到老年患者
3. **联系方式**: 电话号码和身份证号可选，但至少应有其中一项便于联系
4. **重复检查**: 同一患者不允许重复建档，基于姓名+出生日期组合检查
5. **软删除策略**: 患者删除使用软删除，保留所有历史就诊记录
6. **隐私保护**: 身份证号和电话号码等敏感信息需要脱敏处理
7. **紧急联系人**: 未成年患者（<18岁）强制要求填写紧急联系人

## 📋 开发规范

### 代码结构要求
```
src/Server/Modules/LYBT.Module.Patients/
├── Services/
│   ├── PatientQueryService.cs      # 查询专业层
│   ├── PatientBusinessService.cs   # 业务逻辑层
│   └── PatientService.cs           # 纯委托层
├── Controllers/
│   └── PatientsController.cs       # API控制器
├── DTOs/
│   ├── PatientCreateDto.cs         # 患者创建DTO
│   ├── PatientUpdateDto.cs         # 患者更新DTO
│   ├── PatientDto.cs               # 患者信息DTO
│   ├── PatientSearchDto.cs         # 搜索条件DTO
│   ├── PatientStatisticsDto.cs     # 统计信息DTO
│   └── PatientImportDto.cs         # 导入数据DTO
├── Validators/
│   ├── PatientCreateValidator.cs   # 创建验证器
│   ├── PatientUpdateValidator.cs   # 更新验证器
│   └── PatientImportValidator.cs   # 导入验证器
├── Mapping/
│   └── PatientMappingProfile.cs    # AutoMapper配置
├── Services/
│   ├── PatientImportExportService.cs  # 导入导出服务
│   └── PatientStatisticsService.cs    # 统计服务
├── Exceptions/
│   ├── PatientNotFoundException.cs     # 患者不存在异常
│   ├── DuplicatePatientException.cs    # 重复患者异常
│   └── PatientValidationException.cs   # 患者验证异常
└── PatientsModule.cs               # 模块依赖注入注册
```

### UltraThink双层架构实现

#### PatientService主服务(纯委托)
```csharp
public class PatientService : IPatientService
{
    private readonly IPatientQueryService _queryService;
    private readonly IPatientBusinessService _businessService;
    private readonly ILogger<PatientService> _logger;
    
    public PatientService(IPatientQueryService queryService,
                         IPatientBusinessService businessService,
                         ILogger<PatientService> logger)
    {
        _queryService = queryService;
        _businessService = businessService;
        _logger = logger;
    }
    
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
    
    public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria)
        => await _queryService.SearchPatientsAsync(criteria);
    
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        => await _businessService.CreatePatientAsync(dto);
    
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        => await _businessService.UpdatePatientAsync(id, dto);
    
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeletePatientAsync(id);
    
    public async Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync()
        => await _queryService.GetPatientStatisticsAsync();
    
    public async Task<ServiceResult<List<PatientDto>>> ImportPatientsAsync(List<PatientImportDto> patients)
        => await _businessService.ImportPatientsFromExcelAsync(ConvertImportData(patients));
    
    public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PatientExportOptionsDto options)
        => await _businessService.ExportPatientsToExcelAsync(options);
    
    // 其他方法类似的纯委托实现...
}
```

#### PatientQueryService查询专业层
```csharp
public class PatientQueryService : IPatientQueryService
{
    private readonly IRepository<PatientModel> _patientRepository;
    private readonly IRepository<MedicalCaseModel> _medicalCaseRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PatientQueryService> _logger;
    
    public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria)
    {
        try
        {
            _logger.LogInformation("执行患者搜索查询: {@Criteria}", criteria);
            
            var patients = await _patientRepository.GetAllAsync();
            var query = patients.AsQueryable();
            
            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var keyword = criteria.Keyword.ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(keyword) ||
                    (p.Phone != null && p.Phone.Contains(keyword)) ||
                    (p.IdNumber != null && p.IdNumber.Contains(keyword)));
            }
            
            if (criteria.Gender.HasValue)
            {
                query = query.Where(p => p.Gender == criteria.Gender.Value);
            }
            
            // 年龄范围筛选
            if (criteria.MinAge.HasValue || criteria.MaxAge.HasValue)
            {
                var today = DateTime.Today;
                if (criteria.MinAge.HasValue)
                {
                    var maxBirthDate = today.AddYears(-criteria.MinAge.Value);
                    query = query.Where(p => p.DateOfBirth <= maxBirthDate);
                }
                if (criteria.MaxAge.HasValue)
                {
                    var minBirthDate = today.AddYears(-criteria.MaxAge.Value - 1);
                    query = query.Where(p => p.DateOfBirth > minBirthDate);
                }
            }
            
            // 出生日期范围
            if (criteria.BirthDateStart.HasValue)
            {
                query = query.Where(p => p.DateOfBirth >= criteria.BirthDateStart.Value);
            }
            if (criteria.BirthDateEnd.HasValue)
            {
                query = query.Where(p => p.DateOfBirth <= criteria.BirthDateEnd.Value);
            }
            
            // 状态筛选
            if (criteria.Status.HasValue)
            {
                query = query.Where(p => p.Status == criteria.Status.Value);
            }
            
            // 联系信息筛选
            if (criteria.HasPhone.HasValue)
            {
                if (criteria.HasPhone.Value)
                    query = query.Where(p => !string.IsNullOrEmpty(p.Phone));
                else
                    query = query.Where(p => string.IsNullOrEmpty(p.Phone));
            }
            
            if (criteria.HasIdNumber.HasValue)
            {
                if (criteria.HasIdNumber.Value)
                    query = query.Where(p => !string.IsNullOrEmpty(p.IdNumber));
                else
                    query = query.Where(p => string.IsNullOrEmpty(p.IdNumber));
            }
            
            if (criteria.HasEmergencyContact.HasValue)
            {
                if (criteria.HasEmergencyContact.Value)
                    query = query.Where(p => !string.IsNullOrEmpty(p.EmergencyContactName));
                else
                    query = query.Where(p => string.IsNullOrEmpty(p.EmergencyContactName));
            }
            
            // 排序
            query = criteria.SortBy?.ToLower() switch
            {
                "name" => criteria.SortDescending ? 
                    query.OrderByDescending(p => p.Name) : 
                    query.OrderBy(p => p.Name),
                "age" => criteria.SortDescending ? 
                    query.OrderBy(p => p.DateOfBirth) : 
                    query.OrderByDescending(p => p.DateOfBirth),
                "gender" => criteria.SortDescending ? 
                    query.OrderByDescending(p => p.Gender) : 
                    query.OrderBy(p => p.Gender),
                _ => criteria.SortDescending ? 
                    query.OrderByDescending(p => p.CreateTime) : 
                    query.OrderBy(p => p.CreateTime)
            };
            
            // 分页处理
            var totalCount = query.Count();
            var pagedPatients = query
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToList();
            
            // 映射到DTO并添加统计信息
            var patientDtos = new List<PatientDto>();
            foreach (var patient in pagedPatients)
            {
                var dto = _mapper.Map<PatientDto>(patient);
                
                // 添加关联统计信息
                await PopulatePatientStatistics(dto, patient.Id);
                patientDtos.Add(dto);
            }
            
            var pagedResult = new PagedResult<PatientDto>
            {
                Items = patientDtos,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize,
                TotalRecords = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
            };
            
            _logger.LogInformation("患者搜索完成: 找到 {TotalCount} 个结果", totalCount);
            return ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者搜索查询失败: {@Criteria}", criteria);
            return ServiceResult<PagedResult<PatientDto>>.Failure("搜索患者失败");
        }
    }
    
    public async Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync()
    {
        try
        {
            const string cacheKey = "patient_statistics";
            if (_cache.TryGetValue(cacheKey, out PatientStatisticsDto? cachedStats))
                return ServiceResult<PatientStatisticsDto>.Success(cachedStats!);
            
            var patients = await _patientRepository.GetAllAsync();
            var medicalCases = await _medicalCaseRepository.GetAllAsync();
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            
            var statistics = new PatientStatisticsDto
            {
                TotalPatients = patients.Count(),
                ActivePatients = patients.Count(p => p.Status == CommonStatus.Active),
                MalePatients = patients.Count(p => p.Gender == Gender.Male),
                FemalePatients = patients.Count(p => p.Gender == Gender.Female),
                NewPatientsThisMonth = patients.Count(p => p.CreateTime >= thirtyDaysAgo),
                AverageAge = patients.Select(p => DateTime.Today.Year - p.DateOfBirth.Year).Average(),
                PatientsWithPhone = patients.Count(p => !string.IsNullOrEmpty(p.Phone)),
                PatientsWithIdNumber = patients.Count(p => !string.IsNullOrEmpty(p.IdNumber)),
                PatientsWithEmergencyContact = patients.Count(p => !string.IsNullOrEmpty(p.EmergencyContactName)),
                
                // 年龄分布统计
                AgeDistribution = CalculateAgeDistribution(patients),
                
                // 性别分布统计
                GenderDistribution = patients.GroupBy(p => p.Gender)
                    .Select(g => new PatientGenderStatDto
                    {
                        Gender = g.Key,
                        Count = g.Count(),
                        Percentage = (double)g.Count() / patients.Count() * 100
                    }).ToList(),
                
                // 注册趋势统计
                RegistrationTrend = patients.Where(p => p.CreateTime >= thirtyDaysAgo)
                    .GroupBy(p => p.CreateTime.Date)
                    .Select(g => new PatientRegistrationTrendDto
                    {
                        Date = g.Key,
                        Count = g.Count()
                    }).OrderBy(x => x.Date).ToList()
            };
            
            _cache.Set(cacheKey, statistics, TimeSpan.FromMinutes(15));
            return ServiceResult<PatientStatisticsDto>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者统计信息失败");
            return ServiceResult<PatientStatisticsDto>.Failure("获取统计信息失败");
        }
    }
    
    private async Task PopulatePatientStatistics(PatientDto dto, Guid patientId)
    {
        try
        {
            var medicalCases = await _medicalCaseRepository.GetAllAsync();
            var patientCases = medicalCases.Where(mc => mc.PatientId == patientId).ToList();
            
            dto.TotalMedicalCases = patientCases.Count;
            dto.FirstVisitTime = patientCases.OrderBy(mc => mc.VisitDate).FirstOrDefault()?.VisitDate;
            dto.LastVisitTime = patientCases.OrderByDescending(mc => mc.VisitDate).FirstOrDefault()?.VisitDate;
            
            // 获取最近诊断信息（这里需要关联Consultation模块）
            // dto.LastDiagnosis = await GetLastDiagnosisAsync(patientId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取患者统计信息失败: {PatientId}", patientId);
        }
    }
    
    private List<PatientAgeGroupDto> CalculateAgeDistribution(IEnumerable<PatientModel> patients)
    {
        var ageGroups = new List<PatientAgeGroupDto>();
        var today = DateTime.Today;
        
        var ageRanges = new[]
        {
            new { Name = "0-18岁", Min = 0, Max = 18 },
            new { Name = "19-30岁", Min = 19, Max = 30 },
            new { Name = "31-45岁", Min = 31, Max = 45 },
            new { Name = "46-60岁", Min = 46, Max = 60 },
            new { Name = "61-75岁", Min = 61, Max = 75 },
            new { Name = "75岁以上", Min = 76, Max = int.MaxValue }
        };
        
        foreach (var range in ageRanges)
        {
            var count = patients.Count(p =>
            {
                var age = today.Year - p.DateOfBirth.Year;
                if (today.DayOfYear < p.DateOfBirth.DayOfYear) age--;
                return age >= range.Min && age <= range.Max;
            });
            
            ageGroups.Add(new PatientAgeGroupDto
            {
                AgeGroup = range.Name,
                Count = count,
                MinAge = range.Min,
                MaxAge = range.Max == int.MaxValue ? null : range.Max
            });
        }
        
        return ageGroups;
    }
}
```

#### PatientBusinessService业务逻辑层
```csharp
public class PatientBusinessService : IPatientBusinessService
{
    private readonly IRepository<PatientModel> _patientRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientBusinessService> _logger;
    
    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto)
    {
        try
        {
            _logger.LogInformation("开始创建患者档案: {Name}", dto.Name);
            
            // 1. 验证重复患者
            var existingPatients = await _patientRepository.GetAllAsync();
            var duplicatePatient = existingPatients.FirstOrDefault(p => 
                p.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase) &&
                p.DateOfBirth.Date == dto.DateOfBirth.Date);
            
            if (duplicatePatient != null)
            {
                _logger.LogWarning("患者创建失败 - 重复患者: {Name}, 出生日期: {DateOfBirth}", 
                    dto.Name, dto.DateOfBirth);
                return ServiceResult<PatientDto>.Failure("相同姓名和出生日期的患者已存在");
            }
            
            // 2. 验证电话号码唯一性（如果提供）
            if (!string.IsNullOrEmpty(dto.Phone))
            {
                var phoneExists = existingPatients.Any(p => 
                    !string.IsNullOrEmpty(p.Phone) && p.Phone.Equals(dto.Phone));
                if (phoneExists)
                {
                    _logger.LogWarning("患者创建失败 - 电话号码已存在: {Phone}", dto.Phone);
                    return ServiceResult<PatientDto>.Failure("该电话号码已被其他患者使用");
                }
            }
            
            // 3. 验证身份证号唯一性（如果提供）
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var idExists = existingPatients.Any(p => 
                    !string.IsNullOrEmpty(p.IdNumber) && p.IdNumber.Equals(dto.IdNumber));
                if (idExists)
                {
                    _logger.LogWarning("患者创建失败 - 身份证号已存在: {IdNumber}", dto.IdNumber);
                    return ServiceResult<PatientDto>.Failure("该身份证号已被其他患者使用");
                }
            }
            
            // 4. 检查未成年患者紧急联系人
            var age = DateTime.Today.Year - dto.DateOfBirth.Year;
            if (DateTime.Today.DayOfYear < dto.DateOfBirth.DayOfYear) age--;
            
            if (age < 18 && string.IsNullOrEmpty(dto.EmergencyContactName))
            {
                return ServiceResult<PatientDto>.Failure("未成年患者必须填写紧急联系人信息");
            }
            
            // 5. 创建患者实体
            var patient = new PatientModel
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                Phone = dto.Phone,
                IdNumber = dto.IdNumber,
                Address = dto.Address,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                Remark = dto.Remark,
                Status = CommonStatus.Active,
                CreateTime = DateTime.UtcNow
            };
            
            // 6. 保存到数据库
            var createdPatient = await _patientRepository.CreateAsync(patient);
            
            // 7. 记录操作日志
            _logger.LogInformation("患者档案创建成功: {Name}, PatientId: {PatientId}, 年龄: {Age}", 
                dto.Name, createdPatient.Id, age);
            
            // 8. 映射返回结果
            var patientDto = _mapper.Map<PatientDto>(createdPatient);
            
            return ServiceResult<PatientDto>.Success(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者档案异常: {Name}", dto.Name);
            return ServiceResult<PatientDto>.Failure("创建患者档案失败，请稍后重试");
        }
    }
    
    public async Task<ServiceResult<List<PatientDto>>> ImportPatientsFromExcelAsync(byte[] excelData)
    {
        try
        {
            _logger.LogInformation("开始导入患者数据，数据大小: {Size} bytes", excelData.Length);
            
            // 1. 解析Excel数据
            var importedPatients = await ParseExcelDataAsync(excelData);
            
            // 2. 验证导入数据
            var validatedPatients = await ValidateImportDataAsync(importedPatients);
            var validPatients = validatedPatients.Where(p => p.IsValid).ToList();
            
            if (!validPatients.Any())
            {
                return ServiceResult<List<PatientDto>>.Failure("没有有效的患者数据可以导入");
            }
            
            // 3. 批量创建患者
            var createdPatients = new List<PatientDto>();
            var errors = new List<string>();
            
            foreach (var importPatient in validPatients)
            {
                try
                {
                    var createDto = MapImportToCreateDto(importPatient);
                    var result = await CreatePatientAsync(createDto);
                    
                    if (result.IsSuccess)
                    {
                        createdPatients.Add(result.Data!);
                    }
                    else
                    {
                        errors.Add($"第{importPatient.RowNumber}行: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "导入患者失败，行号: {RowNumber}", importPatient.RowNumber);
                    errors.Add($"第{importPatient.RowNumber}行: 导入失败");
                }
            }
            
            // 4. 记录导入结果
            _logger.LogInformation("患者数据导入完成: 成功 {Success} 个，失败 {Failed} 个", 
                createdPatients.Count, errors.Count);
            
            if (errors.Any())
            {
                var errorMessage = $"部分导入失败: {string.Join("; ", errors.Take(5))}";
                if (errors.Count > 5) errorMessage += $"等{errors.Count}个错误";
                
                return ServiceResult<List<PatientDto>>.SuccessWithWarning(createdPatients, errorMessage);
            }
            
            return ServiceResult<List<PatientDto>>.Success(createdPatients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入患者数据异常");
            return ServiceResult<List<PatientDto>>.Failure("导入患者数据失败");
        }
    }
    
    private async Task<List<PatientImportDto>> ParseExcelDataAsync(byte[] excelData)
    {
        // 使用EPPlus解析Excel文件
        var patients = new List<PatientImportDto>();
        
        using var stream = new MemoryStream(excelData);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.First();
        
        var rowCount = worksheet.Dimension.Rows;
        for (int row = 2; row <= rowCount; row++) // 跳过表头
        {
            var patient = new PatientImportDto
            {
                RowNumber = row,
                Name = worksheet.Cells[row, 1].Text,
                Gender = worksheet.Cells[row, 2].Text,
                DateOfBirth = worksheet.Cells[row, 3].Text,
                Phone = worksheet.Cells[row, 4].Text,
                IdNumber = worksheet.Cells[row, 5].Text,
                Address = worksheet.Cells[row, 6].Text,
                EmergencyContactName = worksheet.Cells[row, 7].Text,
                EmergencyContactPhone = worksheet.Cells[row, 8].Text,
                Remark = worksheet.Cells[row, 9].Text
            };
            
            patients.Add(patient);
        }
        
        return patients;
    }
    
    private async Task<List<PatientImportDto>> ValidateImportDataAsync(List<PatientImportDto> patients)
    {
        foreach (var patient in patients)
        {
            var errors = new List<string>();
            
            // 验证姓名
            if (string.IsNullOrWhiteSpace(patient.Name))
                errors.Add("患者姓名不能为空");
            else if (patient.Name.Length < 2 || patient.Name.Length > 20)
                errors.Add("患者姓名长度必须在2-20个字符之间");
            
            // 验证性别
            if (string.IsNullOrWhiteSpace(patient.Gender))
                errors.Add("性别不能为空");
            else if (!Enum.TryParse<Gender>(patient.Gender, true, out _))
                errors.Add("性别格式不正确，应为：Male, Female, Other");
            
            // 验证出生日期
            if (string.IsNullOrWhiteSpace(patient.DateOfBirth))
                errors.Add("出生日期不能为空");
            else if (!DateTime.TryParse(patient.DateOfBirth, out var birthDate))
                errors.Add("出生日期格式不正确");
            else if (birthDate > DateTime.Today)
                errors.Add("出生日期不能晚于今天");
            
            // 验证电话号码（可选）
            if (!string.IsNullOrEmpty(patient.Phone))
            {
                if (patient.Phone.Length > 20)
                    errors.Add("电话号码长度不能超过20个字符");
            }
            
            // 验证身份证号（可选）
            if (!string.IsNullOrEmpty(patient.IdNumber))
            {
                if (patient.IdNumber.Length > 18)
                    errors.Add("身份证号长度不能超过18个字符");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(patient.IdNumber, @"^\d{17}[\dXx]$"))
                    errors.Add("身份证号格式不正确");
            }
            
            patient.IsValid = !errors.Any();
            patient.ValidationErrors = errors;
        }
        
        return patients;
    }
}
```

### 命名规范
- **服务类**: PascalCase + Service后缀 (PatientService, PatientQueryService)
- **DTO类**: PascalCase + Dto后缀 (PatientCreateDto, PatientSearchDto)
- **统计类**: PascalCase + Statistics/Stat后缀 (PatientStatisticsDto)
- **导入导出类**: PascalCase + Import/Export后缀 (PatientImportDto)
- **异常类**: PascalCase + Exception后缀 (PatientNotFoundException)
- **验证器**: PascalCase + Validator后缀 (PatientCreateValidator)

### 质量标准
- **数据完整性**: 关键患者信息必须完整，支持数据完整性验证
- **隐私保护**: 敏感信息（身份证号、电话）需要适当的脱敏处理
- **查询性能**: 患者搜索支持索引优化，大量数据查询<3秒
- **导入导出**: 支持大批量数据处理，单次导入支持10000条记录
- **缓存策略**: 患者统计信息缓存15分钟，常用查询结果适当缓存
- **并发安全**: 支持多用户同时操作，避免数据冲突

### 测试要求
- **单元测试覆盖率**: >85%，特别是业务逻辑和数据验证
- **集成测试**: 完整的患者CRUD流程和导入导出功能
- **性能测试**: 大量患者数据的查询和统计性能测试
- **数据一致性测试**: 患者关联数据的一致性和完整性测试

## 🔌 集成接口

### 对外提供的接口

#### 1. RESTful API接口
```http
# 获取患者列表
GET /api/v1/patients?pageNumber=1&pageSize=10&keyword=张&gender=Male&minAge=20&maxAge=60
Authorization: Bearer <access_token>

# 响应
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "123e4567-e89b-12d3-a456-426614174000",
                "name": "张三",
                "gender": "Male",
                "genderName": "男",
                "dateOfBirth": "1985-06-15T00:00:00Z",
                "age": 39,
                "phone": "13800138000",
                "address": "北京市朝阳区xxx街道",
                "emergencyContactName": "李四",
                "emergencyContactPhone": "13800138001",
                "status": "Active",
                "totalMedicalCases": 5,
                "firstVisitTime": "2024-01-15T10:30:00Z",
                "lastVisitTime": "2025-08-20T14:00:00Z"
            }
        ],
        "pageNumber": 1,
        "pageSize": 10,
        "totalRecords": 1,
        "totalPages": 1
    }
}

# 创建新患者
POST /api/v1/patients
Authorization: Bearer <access_token>
{
    "name": "李五",
    "gender": "Female",
    "dateOfBirth": "1990-03-20",
    "phone": "13800138002",
    "address": "上海市浦东新区xxx路",
    "emergencyContactName": "王六",
    "emergencyContactPhone": "13800138003"
}

# 更新患者信息
PUT /api/v1/patients/{id}
Authorization: Bearer <access_token>
{
    "name": "李五五",
    "gender": "Female", 
    "dateOfBirth": "1990-03-20",
    "phone": "13800138002",
    "address": "上海市浦东新区新地址",
    "emergencyContactName": "王六六",
    "emergencyContactPhone": "13800138003",
    "status": "Active"
}

# 获取患者统计
GET /api/v1/patients/statistics
Authorization: Bearer <access_token>

# 患者数据导入
POST /api/v1/patients/import
Authorization: Bearer <access_token>
Content-Type: multipart/form-data
[Excel文件上传]

# 患者数据导出
GET /api/v1/patients/export?format=excel&includeInactive=false
Authorization: Bearer <access_token>
```

#### 2. 内部服务接口
```csharp
// 其他业务模块可以通过依赖注入使用
public class MedicalCaseBusinessService
{
    private readonly IPatientService _patientService;
    
    public async Task<bool> ValidatePatientExists(Guid patientId)
    {
        var result = await _patientService.GetByIdAsync(patientId);
        return result.IsSuccess && result.Data?.Status == CommonStatus.Active;
    }
    
    public async Task<string> GetPatientName(Guid patientId)
    {
        var result = await _patientService.GetByIdAsync(patientId);
        return result.IsSuccess ? result.Data?.Name ?? "未知患者" : "未知患者";
    }
}
```

### 依赖的外部接口
- **IRepository<PatientModel>**: Infrastructure提供的患者数据访问接口
- **IRepository<MedicalCaseModel>**: 医疗案例数据，用于统计患者就诊信息
- **IMapper**: AutoMapper对象映射服务
- **IMemoryCache**: .NET内存缓存服务
- **ILogger<T>**: .NET结构化日志服务
- **ExcelPackage**: EPPlus Excel处理库

### 数据传输格式

#### 患者详细信息响应格式
```json
{
    "success": true,
    "message": "获取患者信息成功",
    "data": {
        "id": "guid",
        "name": "string",
        "gender": "Male|Female|Other",
        "genderName": "男|女|其他",
        "dateOfBirth": "datetime",
        "age": number,
        "phone": "string",
        "idNumber": "string",
        "address": "string",
        "emergencyContactName": "string",
        "emergencyContactPhone": "string",
        "remark": "string",
        "status": "Active|Inactive|Deleted",
        "statusName": "活跃|禁用|已删除",
        "createTime": "datetime",
        "updateTime": "datetime",
        "totalMedicalCases": number,
        "totalConsultations": number,
        "totalPrescriptions": number,
        "firstVisitTime": "datetime",
        "lastVisitTime": "datetime",
        "lastDiagnosis": "string"
    }
}
```

#### 患者统计响应格式
```json
{
    "success": true,
    "data": {
        "totalPatients": 500,
        "activePatients": 480,
        "malePatients": 240,
        "femalePatients": 260,
        "newPatientsThisMonth": 25,
        "averageAge": 45.5,
        "patientsWithPhone": 450,
        "patientsWithIdNumber": 320,
        "patientsWithEmergencyContact": 180,
        "ageDistribution": [
            {
                "ageGroup": "0-18岁",
                "count": 50,
                "minAge": 0,
                "maxAge": 18
            },
            {
                "ageGroup": "19-30岁", 
                "count": 80,
                "minAge": 19,
                "maxAge": 30
            }
        ],
        "genderDistribution": [
            {
                "gender": "Male",
                "count": 240,
                "percentage": 48.0
            },
            {
                "gender": "Female",
                "count": 260,
                "percentage": 52.0
            }
        ],
        "registrationTrend": [
            {
                "date": "2025-08-01",
                "count": 5
            }
        ]
    }
}
```

### 错误处理规范
- **400 Bad Request**: 患者信息验证失败或格式错误
- **404 Not Found**: 指定的患者不存在
- **409 Conflict**: 重复患者档案或联系方式冲突
- **422 Unprocessable Entity**: 业务规则验证失败（如未成年患者缺少紧急联系人）
- **413 Payload Too Large**: 导入文件过大
- **500 Internal Server Error**: 服务器内部错误

## ⚙️ 配置管理

### 配置项定义

#### 患者管理相关配置
```json
{
  "PatientManagementOptions": {
    "EnablePatientImportExport": true,
    "MaxImportBatchSize": 10000,
    "EnableDuplicateCheck": true,
    "DuplicateCheckFields": ["Name", "DateOfBirth"],
    "RequireEmergencyContactForMinors": true,
    "MinorAgeThreshold": 18,
    "EnablePhoneValidation": true,
    "EnableIdNumberValidation": true,
    "AutoGeneratePatientNumber": false,
    "PatientNumberFormat": "P{0:yyyyMMdd}{1:0000}",
    "EnablePatientStatisticsCache": true,
    "StatisticsCacheMinutes": 15
  },
  "PatientValidationOptions": {
    "NameMinLength": 2,
    "NameMaxLength": 20,
    "PhoneMaxLength": 20,
    "IdNumberMaxLength": 18,
    "AddressMaxLength": 200,
    "RemarkMaxLength": 500,
    "RequirePhone": false,
    "RequireIdNumber": false,
    "RequireAddress": false
  },
  "PatientImportOptions": {
    "SupportedFormats": ["xlsx", "csv"],
    "MaxFileSize": 10485760,
    "RequiredColumns": ["Name", "Gender", "DateOfBirth"],
    "OptionalColumns": ["Phone", "IdNumber", "Address", "EmergencyContactName", "EmergencyContactPhone", "Remark"],
    "ValidateDataIntegrity": true,
    "SkipDuplicateRows": true
  }
}
```

### 环境变量要求
```bash
# 患者管理配置
PATIENTMANAGEMENTOPTIONS__ENABLEPATIENTIMPORTEXPORT=true
PATIENTMANAGEMENTOPTIONS__MAXIMPORTBATCHSIZE=10000
PATIENTMANAGEMENTOPTIONS__ENABLEDUPLICATECHECK=true
PATIENTMANAGEMENTOPTIONS__REQUIREEMERGENCYCONTACTFORMINORS=true

# 患者验证配置
PATIENTVALIDATIONOPTIONS__NAMEMINLENGTH=2
PATIENTVALIDATIONOPTIONS__NAMEMAXLENGTH=20
PATIENTVALIDATIONOPTIONS__REQUIREPHONE=false

# 患者导入配置
PATIENTIMPORTOPTIONS__MAXFILESIZE=10485760
PATIENTIMPORTOPTIONS__VALIDATEDATAINTEGRITY=true
PATIENTIMPORTOPTIONS__SKIPDUPLICATEROWS=true
```

### 部署配置说明
1. **开发环境**: 降低数据验证要求，支持测试数据快速导入
2. **测试环境**: 接近生产环境的验证规则，但允许重复数据便于测试
3. **生产环境**: 严格的数据验证和重复检查，启用所有安全特性
4. **数据迁移**: 支持从其他系统批量导入患者历史数据

## 🧪 测试规范

### 单元测试要求

#### 患者业务逻辑测试
```csharp
public class PatientBusinessServiceTests : IDisposable
{
    private readonly Mock<IRepository<PatientModel>> _mockPatientRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PatientBusinessService _service;
    
    [Fact]
    public async Task CreatePatientAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var dto = new PatientCreateDto
        {
            Name = "测试患者",
            Gender = Gender.Male,
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "13800138000"
        };
        
        _mockPatientRepository.Setup(r => r.GetAllAsync())
                              .ReturnsAsync(new List<PatientModel>());
        
        // Act & Assert
        var result = await _service.CreatePatientAsync(dto);
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task CreatePatientAsync_DuplicatePatient_ReturnsFailure()
    {
        // 测试重复患者检查
    }
    
    [Fact]
    public async Task CreatePatientAsync_MinorWithoutEmergencyContact_ReturnsFailure()
    {
        // 测试未成年患者必须有紧急联系人的规则
    }
    
    [Fact]
    public async Task ImportPatientsFromExcelAsync_ValidData_ReturnsSuccessfulImports()
    {
        // 测试Excel导入功能
    }
}
```

#### 患者查询服务测试
```csharp
public class PatientQueryServiceTests
{
    [Theory]
    [InlineData("张", 1)]
    [InlineData("不存在的患者", 0)]
    public async Task SearchPatientsAsync_WithKeyword_ReturnsExpectedCount(string keyword, int expectedCount)
    {
        // 测试关键词搜索
    }
    
    [Fact]
    public async Task GetPatientStatisticsAsync_ValidCall_ReturnsCorrectStatistics()
    {
        // 测试患者统计功能
    }
    
    [Theory]
    [InlineData(0, 18, "儿童患者")]
    [InlineData(19, 65, "成年患者")]
    [InlineData(66, 150, "老年患者")]
    public async Task GetPatientsByAgeRangeAsync_ValidRange_ReturnsCorrectPatients(int minAge, int maxAge, string description)
    {
        // 测试年龄范围查询
    }
}
```

### 集成测试要求

#### 患者API集成测试
```csharp
public class PatientsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GET_Patients_WithValidAuth_ReturnsPatientList()
    {
        // 测试获取患者列表API
    }
    
    [Fact]
    public async Task POST_Patients_ValidData_CreatesPatient()
    {
        // 测试创建患者API
    }
    
    [Fact] 
    public async Task POST_PatientsImport_ValidExcelFile_ImportsPatients()
    {
        // 测试患者Excel导入API
    }
    
    [Fact]
    public async Task GET_PatientsStatistics_ValidAuth_ReturnsStatistics()
    {
        // 测试患者统计API
    }
}
```

### 性能测试要求
```csharp
public class PatientPerformanceTests
{
    [Fact]
    public async Task SearchPatients_LargeDataset_CompletesWithinTimeLimit()
    {
        // 测试大数据量患者搜索性能
        // 目标: 50000个患者的搜索在3秒内完成
    }
    
    [Fact]
    public async Task ImportPatients_10000Records_CompletesWithinTimeLimit()
    {
        // 测试大批量导入性能
        // 目标: 10000条患者记录导入在30秒内完成
    }
    
    [Fact]
    public async Task GetPatientStatistics_LargeDataset_CompletesWithinTimeLimit()
    {
        // 测试统计性能
        // 目标: 大数据量统计在5秒内完成
    }
}
```

### 测试覆盖率目标
- **核心业务逻辑**: >90%覆盖率
- **查询服务**: >85%覆盖率
- **数据验证规则**: 100%覆盖率
- **导入导出功能**: >80%覆盖率
- **API端点**: >80%覆盖率

## 🚀 部署说明

### 构建要求
- **.NET 8.0 SDK**: 编译Patients模块
- **EPPlus依赖**: Excel文件处理库
- **AutoMapper依赖**: 对象映射库
- **FluentValidation依赖**: 数据验证库

### 部署步骤

#### 1. 模块部署验证
```bash
# 验证Patients模块编译
dotnet build LYBT.Module.Patients.csproj

# 验证服务注册
dotnet run --project LYBT.WebAPI
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/v1/patients
```

#### 2. 数据导入导出测试
```bash
# 测试患者数据导出
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/api/v1/patients/export?format=excel" \
  -o patients.xlsx

# 测试患者数据导入
curl -X POST http://localhost:5000/api/v1/patients/import \
  -H "Authorization: Bearer <token>" \
  -F "file=@patients.xlsx"
```

#### 3. 性能基准测试
```bash
# 测试大量患者查询性能
time curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/api/v1/patients?pageSize=1000"

# 测试患者统计性能
time curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/api/v1/patients/statistics"
```

### 环境依赖
- **数据库访问**: 需要PatientModel表的完整读写权限
- **文件系统**: 导入导出功能需要临时文件存储权限
- **内存资源**: 大批量导入需要足够的内存空间
- **缓存服务**: 统计功能需要缓存服务支持

### 运行监控

#### 患者模块性能监控
```http
# 患者操作性能指标
GET /api/v1/monitoring/patients/performance

# 患者增长趋势监控
GET /api/v1/monitoring/patients/growth-trend?period=30d

# 导入导出操作监控
GET /api/v1/monitoring/patients/import-export-status
```

#### 数据质量监控
```http
# 患者数据完整性检查
GET /api/v1/monitoring/patients/data-integrity

# 重复患者检测
GET /api/v1/monitoring/patients/duplicates

# 患者信息缺失统计
GET /api/v1/monitoring/patients/missing-info
```

## 📚 相关文档

### 相关项目文档链接
- [LYBT.Module.MedicalCase项目文档](./medicalcase.md) - 医疗案例患者关联
- [LYBT.Infrastructure项目文档](../core/infrastructure.md) - 数据访问基础设施
- [LYBT.Entities项目文档](../core/entities.md) - PatientModel实体定义

### API文档链接
- [患者管理API规范](../../../api/patients-api.md) - 完整的患者管理REST API
- [数据导入导出API](../../../api/data-import-export-api.md) - 导入导出接口规范
- [患者统计API](../../../api/patient-statistics-api.md) - 患者数据统计接口

### 技术规范引用
- [UltraThink双层架构标准](../../../ultrathink/ultrathink-comprehensive-refactoring-complete-20250831.md) - 架构实施标准
- [数据导入导出最佳实践](../../../development/data-import-export-guide.md) - 批量数据处理指南
- [医疗数据隐私保护](../../../security/medical-data-privacy.md) - 患者隐私保护规范
- [Excel文件处理规范](../../../development/excel-processing-guide.md) - Excel导入导出技术规范

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ 已审核通过