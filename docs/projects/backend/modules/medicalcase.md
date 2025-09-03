# LYBT.Module.MedicalCase 项目文档

## 📋 项目概述

**LYBT.Module.MedicalCase**是凌隐宝堂中医诊所系统的医疗案例管理核心模块，作为整个诊疗流程的容器和协调中心。MedicalCase模块负责管理从患者接诊到诊疗完成的完整医疗过程，为Consultation、Prescriptions等业务模块提供统一的业务上下文，确保诊疗流程的连续性和完整性。

### 项目职责
- **诊疗流程容器**: 作为整个看诊会话的管理容器，协调各个诊疗环节
- **状态流程管理**: 管理医案从注册、进行中到完成的完整状态流转
- **业务上下文聚合**: 聚合患者信息、医生信息、诊断记录和处方信息
- **历史记录管理**: 维护患者的历次医疗记录和诊疗轨迹
- **医案编号管理**: 自动生成和维护唯一的医案编号体系
- **诊疗数据统计**: 提供医案相关的统计分析和报表功能

### 在系统中的位置
MedicalCase模块位于诊疗流程的核心位置，与Consultation模块保持1:1关联关系。它依赖Patients和Users模块获取基础信息，为Consultation和Prescriptions模块提供业务容器，是连接所有诊疗相关模块的枢纽。

### 关键业务价值
- **流程统一管理**: 确保诊疗过程的规范化和标准化
- **数据一致性**: 保证诊疗相关数据的完整性和一致性
- **追溯能力**: 提供完整的诊疗过程追溯和审计功能
- **效率提升**: 简化医生工作流程，提高诊疗效率

## 🏗️ 技术架构

### 项目架构设计
MedicalCase模块采用UltraThink双层架构，解决了18个编译错误，实现了1:1关联Consultation的设计模式：

```
MedicalCaseService (纯委托层)
    ├── MedicalCaseQueryService (查询专业层)
    │   ├── 医案搜索和筛选查询
    │   ├── 医案统计和报表查询
    │   ├── 患者就诊历史查询
    │   └── 医生工作量统计查询
    └── MedicalCaseBusinessService (业务逻辑层)
        ├── 医案CRUD操作和验证
        ├── 医案状态流转管理
        ├── 诊疗流程编排
        ├── 医案编号生成管理
        └── 医案完成度验证
```

### 核心技术栈
- **.NET 8.0**: 现代C#语言特性和高性能运行时
- **Entity Framework Core 8.0.17**: ORM框架，支持复杂关联查询
- **AutoMapper**: 实体和DTO自动映射，处理复杂对象关系
- **FluentValidation**: 医案业务规则验证
- **Microsoft.Extensions.Logging**: 结构化医案操作日志
- **Microsoft.Extensions.Caching.Memory**: 医案状态缓存优化
- **System.Text.Json**: 医案数据序列化处理

### 依赖项目列表
**直接依赖**:
- `LYBT.Infrastructure` - 数据访问和基础服务支持
- `LYBT.Entities` - MedicalCaseModel实体定义
- `LYBT.Module.Patients` - 患者信息获取
- `LYBT.Module.Users` - 医生信息获取
- `LYBT.Shared.Models` - 医案相关DTO定义
- `LYBT.Shared.Interfaces` - 医案服务接口契约

**被依赖项目**:
- `LYBT.Module.Consultation` - 诊断记录关联（1:1关系）
- `LYBT.Module.Prescriptions` - 处方信息关联（1:N关系）
- `LYBT.WebAPI` - 控制器层调用医案服务

### 设计模式采用
- **Aggregate Pattern**: MedicalCase作为聚合根管理诊疗流程
- **State Pattern**: 医案状态流转的状态机实现
- **Service Pattern**: UltraThink双层服务架构
- **Repository Pattern**: 通过Infrastructure的统一数据访问
- **Factory Pattern**: 医案创建和初始化工厂

## 🎯 功能规范

### 必须实现的功能清单

#### 1. 医案CRUD核心功能
- ✅ **创建医案**: 新建医案记录，自动生成医案编号，初始状态为Registered
- ✅ **更新医案信息**: 医案基础信息修改，主诉症状更新
- ✅ **医案详情查询**: 完整医案信息展示，包含关联的诊断和处方信息
- ✅ **删除医案**: 软删除机制，保留历史诊疗记录和关联数据
- ✅ **医案列表查询**: 分页查询，支持多条件筛选和排序

#### 2. 医案状态管理功能
- ✅ **状态流转**: 支持Registered → InProgress → Completed的状态流转
- ✅ **状态验证**: 确保状态变更符合业务规则和流程要求
- ✅ **进度跟踪**: 跟踪医案各个阶段的完成情况和时间记录
- ✅ **自动状态更新**: 基于关联数据自动更新医案状态
- ✅ **状态回滚**: 支持特殊情况下的状态回滚操作

#### 3. 医案编号管理
- ✅ **编号自动生成**: 按日期和序号规则自动生成唯一医案编号
- ✅ **编号规则配置**: 支持可配置的编号格式和规则
- ✅ **编号唯一性**: 确保医案编号在系统中的唯一性
- ✅ **编号查询**: 支持按医案编号快速查找医案
- ✅ **编号重新生成**: 支持特殊情况下的编号重新生成

#### 4. 诊疗流程管理
- ✅ **流程开始**: 患者接诊，创建新医案并开始诊疗流程
- ✅ **诊断关联**: 与Consultation模块1:1关联，管理诊断记录
- ✅ **处方关联**: 与Prescriptions模块1:N关联，管理处方记录
- ✅ **流程完成**: 诊疗结束，医案状态更新为Completed
- ✅ **流程监控**: 实时监控诊疗流程的进度和状态

#### 5. 统计分析功能
- ✅ **医案统计**: 按时间、医生、患者等维度的医案统计
- ✅ **诊疗效率分析**: 医案处理时间和效率分析
- ✅ **医生工作量**: 各医生的医案数量和工作负荷统计
- ✅ **患者就诊频次**: 患者就诊频率和规律分析
- ✅ **疾病分布**: 基于医案数据的疾病分布统计

### 接口定义规范

#### IMedicalCaseService主服务接口
```csharp
public interface IMedicalCaseService
{
    Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(MedicalCaseSearchDto searchDto);
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchMedicalCasesAsync(MedicalCaseSearchDto criteria);
    Task<ServiceResult<List<MedicalCaseDto>>> GetPatientMedicalCasesAsync(Guid patientId);
    Task<ServiceResult<List<MedicalCaseDto>>> GetDoctorMedicalCasesAsync(Guid doctorId);
    Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status);
    Task<ServiceResult<MedicalCaseStatisticsDto>> GetMedicalCaseStatisticsAsync();
}
```

#### IMedicalCaseQueryService查询服务接口
```csharp
public interface IMedicalCaseQueryService
{
    Task<ServiceResult<MedicalCaseDto>> GetByIdWithDetailsAsync(Guid id);
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchMedicalCasesAsync(MedicalCaseSearchDto criteria);
    Task<ServiceResult<List<MedicalCaseDto>>> GetPatientMedicalHistoryAsync(Guid patientId);
    Task<ServiceResult<List<MedicalCaseDto>>> GetDoctorActiveArfAcallCasesAsync(Guid doctorId);
    Task<ServiceResult<List<MedicalCaseDto>>> GetMedicalCasesByStatusAsync(MedicalCaseStatus status);
    Task<ServiceResult<MedicalCaseStatisticsDto>> GetMedicalCaseStatisticsAsync();
    Task<ServiceResult<bool>> IsCaseNumberExistsAsync(string caseNumber);
    Task<ServiceResult<List<MedicalCaseDto>>> GetRecentMedicalCasesAsync(int days);
    Task<ServiceResult<List<DoctorWorkloadDto>>> GetDoctorWorkloadAsync(DateTime startDate, DateTime endDate);
}
```

#### IMedicalCaseBusinessService业务服务接口
```csharp
public interface IMedicalCaseBusinessService
{
    Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateMedicalCaseAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult<bool>> DeleteMedicalCaseAsync(Guid id);
    Task<ServiceResult<bool>> StartConsultationAsync(Guid medicalCaseId);
    Task<ServiceResult<bool>> CompleteConsultationAsync(Guid medicalCaseId);
    Task<ServiceResult<bool>> UpdateMedicalCaseStatusAsync(Guid id, MedicalCaseStatus status);
    Task<ServiceResult<string>> GenerateCaseNumberAsync();
    Task<ServiceResult<bool>> ValidateMedicalCaseCompletionAsync(Guid id);
    Task<ServiceResult<MedicalCaseDto>> CloneMedicalCaseAsync(Guid sourceId, MedicalCaseCloneOptionsDto options);
}
```

### 数据模型定义

#### MedicalCaseCreateDto医案创建
```csharp
public class MedicalCaseCreateDto
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid PatientId { get; set; }
    
    [Required(ErrorMessage = "医生ID不能为空")]
    public Guid UserId { get; set; }
    
    [Required(ErrorMessage = "就诊日期不能为空")]
    public DateTime VisitDate { get; set; }
    
    [StringLength(500, ErrorMessage = "主诉症状长度不能超过500个字符")]
    public string? ChiefComplaint { get; set; }
    
    [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
    public string? Remark { get; set; }
    
    public bool AutoGenerateCaseNumber { get; set; } = true;
    public string? CustomCaseNumber { get; set; }
}
```

#### MedicalCaseUpdateDto医案更新
```csharp
public class MedicalCaseUpdateDto
{
    [Required(ErrorMessage = "就诊日期不能为空")]
    public DateTime VisitDate { get; set; }
    
    [StringLength(500, ErrorMessage = "主诉症状长度不能超过500个字符")]
    public string? ChiefComplaint { get; set; }
    
    [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
    public string? Remark { get; set; }
    
    public MedicalCaseStatus CaseStatus { get; set; }
}
```

#### MedicalCaseDto医案信息DTO
```csharp
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public Gender PatientGender { get; set; }
    
    public Guid UserId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    
    public string? CaseNumber { get; set; }
    public DateTime VisitDate { get; set; }
    public MedicalCaseStatus CaseStatus { get; set; }
    public string CaseStatusName => CaseStatus.ToString();
    
    public string? ChiefComplaint { get; set; }
    public string? Remark { get; set; }
    
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    
    // 关联信息
    public ConsultationDto? Consultation { get; set; }
    public List<PrescriptionDto> Prescriptions { get; set; } = new();
    
    // 统计信息
    public int TotalPrescriptions { get; set; }
    public bool HasConsultation { get; set; }
    public DateTime? ConsultationTime { get; set; }
    public DateTime? LastPrescriptionTime { get; set; }
    public TimeSpan? TotalTreatmentDuration { get; set; }
}
```

#### MedicalCaseSearchDto医案搜索条件
```csharp
public class MedicalCaseSearchDto : BaseSearchDto
{
    public string? Keyword { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public Guid? UserId { get; set; }
    public string? DoctorName { get; set; }
    public string? CaseNumber { get; set; }
    public MedicalCaseStatus? CaseStatus { get; set; }
    public DateTime? VisitDateStart { get; set; }
    public DateTime? VisitDateEnd { get; set; }
    public DateTime? CreateTimeStart { get; set; }
    public DateTime? CreateTimeEnd { get; set; }
    public bool? HasConsultation { get; set; }
    public bool? HasPrescriptions { get; set; }
    public int? MinTreatmentDays { get; set; }
    public int? MaxTreatmentDays { get; set; }
    public string? SortBy { get; set; } = "VisitDate";
    public bool SortDescending { get; set; } = true;
}
```

#### MedicalCaseStatisticsDto医案统计
```csharp
public class MedicalCaseStatisticsDto
{
    public int TotalMedicalCases { get; set; }
    public int RegisteredCases { get; set; }
    public int InProgressCases { get; set; }
    public int CompletedCases { get; set; }
    
    public int CasesWithConsultation { get; set; }
    public int CasesWithPrescriptions { get; set; }
    public int CasesToday { get; set; }
    public int CasesThisWeek { get; set; }
    public int CasesThisMonth { get; set; }
    
    public double AverageConsultationTime { get; set; }
    public double AverageTreatmentDuration { get; set; }
    public double CompletionRate { get; set; }
    
    public List<MedicalCaseStatusStatDto> StatusDistribution { get; set; } = new();
    public List<DoctorWorkloadDto> DoctorWorkload { get; set; } = new();
    public List<MedicalCaseTrendDto> DailyTrend { get; set; } = new();
    public List<PatientVisitFrequencyDto> PatientFrequency { get; set; } = new();
}
```

#### DoctorWorkloadDto医生工作量统计
```csharp
public class DoctorWorkloadDto
{
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int TotalCases { get; set; }
    public int CompletedCases { get; set; }
    public int InProgressCases { get; set; }
    public double CompletionRate { get; set; }
    public double AverageConsultationTime { get; set; }
    public int CasesToday { get; set; }
    public int CasesThisWeek { get; set; }
    public DateTime? LastCaseDate { get; set; }
}
```

### 业务规则约束
1. **唯一性约束**: 医案编号在系统中必须唯一，支持自动生成和手动指定
2. **状态流转规则**: 必须按Registered → InProgress → Completed顺序流转
3. **关联约束**: 与Consultation保持1:1关系，与Prescriptions保持1:N关系
4. **时间约束**: 就诊日期不能早于患者出生日期，不能晚于当前日期
5. **医生权限**: 只有状态为Active的Doctor角色用户可以创建和管理医案
6. **软删除策略**: 医案删除使用软删除，保留所有关联的诊断和处方记录
7. **编号格式**: 默认格式为"MC{yyyyMMdd}{0000}"，支持配置化定制

## 📋 开发规范

### 代码结构要求
```
src/Server/Modules/LYBT.Module.MedicalCase/
├── Services/
│   ├── MedicalCaseQueryService.cs      # 查询专业层
│   ├── MedicalCaseBusinessService.cs   # 业务逻辑层
│   └── MedicalCaseService.cs           # 纯委托层
├── Controllers/
│   └── MedicalCaseController.cs        # API控制器
├── DTOs/
│   ├── MedicalCaseCreateDto.cs         # 医案创建DTO
│   ├── MedicalCaseUpdateDto.cs         # 医案更新DTO
│   ├── MedicalCaseDto.cs               # 医案信息DTO
│   ├── MedicalCaseSearchDto.cs         # 搜索条件DTO
│   └── MedicalCaseStatisticsDto.cs     # 统计信息DTO
├── Validators/
│   ├── MedicalCaseCreateValidator.cs   # 创建验证器
│   ├── MedicalCaseUpdateValidator.cs   # 更新验证器
│   └── MedicalCaseStatusValidator.cs   # 状态验证器
├── Mapping/
│   └── MedicalCaseMappingProfile.cs    # AutoMapper配置
├── Services/
│   ├── CaseNumberGeneratorService.cs   # 编号生成服务
│   └── MedicalCaseWorkflowService.cs   # 工作流服务
├── Enums/
│   └── MedicalCaseStatus.cs            # 医案状态枚举
├── Exceptions/
│   ├── MedicalCaseNotFoundException.cs # 医案不存在异常
│   ├── InvalidStatusTransitionException.cs # 无效状态转换异常
│   └── MedicalCaseValidationException.cs # 医案验证异常
└── MedicalCaseModule.cs                # 模块依赖注入注册
```

### UltraThink双层架构实现

#### MedicalCaseService主服务(纯委托)
```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseQueryService _queryService;
    private readonly IMedicalCaseBusinessService _businessService;
    private readonly ILogger<MedicalCaseService> _logger;
    
    public MedicalCaseService(IMedicalCaseQueryService queryService,
                             IMedicalCaseBusinessService businessService,
                             ILogger<MedicalCaseService> logger)
    {
        _queryService = queryService;
        _businessService = businessService;
        _logger = logger;
    }
    
    public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdWithDetailsAsync(id);
    
    public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchMedicalCasesAsync(MedicalCaseSearchDto criteria)
        => await _queryService.SearchMedicalCasesAsync(criteria);
    
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        => await _businessService.CreateMedicalCaseAsync(dto);
    
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        => await _businessService.UpdateMedicalCaseAsync(id, dto);
    
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteMedicalCaseAsync(id);
    
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status)
        => await _businessService.UpdateMedicalCaseStatusAsync(id, status);
    
    public async Task<ServiceResult<List<MedicalCaseDto>>> GetPatientMedicalCasesAsync(Guid patientId)
        => await _queryService.GetPatientMedicalHistoryAsync(patientId);
    
    public async Task<ServiceResult<MedicalCaseStatisticsDto>> GetMedicalCaseStatisticsAsync()
        => await _queryService.GetMedicalCaseStatisticsAsync();
    
    // 其他方法类似的纯委托实现...
}
```

#### MedicalCaseQueryService查询专业层
```csharp
public class MedicalCaseQueryService : IMedicalCaseQueryService
{
    private readonly IRepository<MedicalCaseModel> _medicalCaseRepository;
    private readonly IRepository<ConsultationModel> _consultationRepository;
    private readonly IRepository<PrescriptionModel> _prescriptionRepository;
    private readonly IRepository<PatientModel> _patientRepository;
    private readonly IRepository<UserModel> _userRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MedicalCaseQueryService> _logger;
    
    public async Task<ServiceResult<MedicalCaseDto>> GetByIdWithDetailsAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("获取医案详细信息: {MedicalCaseId}", id);
            
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(id);
            if (medicalCase == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("医案不存在");
            }
            
            // 获取患者信息
            var patient = await _patientRepository.GetByIdAsync(medicalCase.PatientId);
            if (patient == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("关联患者不存在");
            }
            
            // 获取医生信息
            var doctor = await _userRepository.GetByIdAsync(medicalCase.UserId);
            if (doctor == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("关联医生不存在");
            }
            
            // 构建医案DTO
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);
            dto.PatientName = patient.FullName;
            dto.PatientAge = DateTime.Today.Year - patient.DateOfBirth.Year;
            dto.PatientGender = patient.Gender;
            dto.DoctorName = doctor.FullName;
            
            // 获取关联的诊断信息
            var consultation = await GetConsultationByMedicalCaseIdAsync(id);
            if (consultation != null)
            {
                dto.Consultation = _mapper.Map<ConsultationDto>(consultation);
                dto.HasConsultation = true;
                dto.ConsultationTime = consultation.CreateTime;
            }
            
            // 获取关联的处方信息
            var prescriptions = await GetPrescriptionsByMedicalCaseIdAsync(id);
            dto.Prescriptions = _mapper.Map<List<PrescriptionDto>>(prescriptions);
            dto.TotalPrescriptions = prescriptions.Count;
            dto.LastPrescriptionTime = prescriptions.OrderByDescending(p => p.CreateTime)
                                                  .FirstOrDefault()?.CreateTime;
            
            // 计算治疗持续时间
            if (dto.HasConsultation && dto.LastPrescriptionTime.HasValue)
            {
                dto.TotalTreatmentDuration = dto.LastPrescriptionTime.Value - dto.ConsultationTime!.Value;
            }
            
            return ServiceResult<MedicalCaseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医案详细信息失败: {MedicalCaseId}", id);
            return ServiceResult<MedicalCaseDto>.Failure("获取医案信息失败");
        }
    }
    
    public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchMedicalCasesAsync(MedicalCaseSearchDto criteria)
    {
        try
        {
            _logger.LogInformation("执行医案搜索查询: {@Criteria}", criteria);
            
            var medicalCases = await _medicalCaseRepository.GetAllAsync();
            var query = medicalCases.AsQueryable();
            
            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var keyword = criteria.Keyword.ToLower();
                query = query.Where(mc =>
                    (mc.CaseNumber != null && mc.CaseNumber.ToLower().Contains(keyword)) ||
                    (mc.ChiefComplaint != null && mc.ChiefComplaint.ToLower().Contains(keyword)) ||
                    (mc.Remark != null && mc.Remark.ToLower().Contains(keyword)));
            }
            
            if (criteria.PatientId.HasValue)
            {
                query = query.Where(mc => mc.PatientId == criteria.PatientId.Value);
            }
            
            if (criteria.UserId.HasValue)
            {
                query = query.Where(mc => mc.UserId == criteria.UserId.Value);
            }
            
            if (!string.IsNullOrWhiteSpace(criteria.CaseNumber))
            {
                query = query.Where(mc => mc.CaseNumber != null && 
                                         mc.CaseNumber.Contains(criteria.CaseNumber));
            }
            
            if (criteria.CaseStatus.HasValue)
            {
                query = query.Where(mc => mc.CaseStatus == criteria.CaseStatus.Value);
            }
            
            // 就诊日期范围
            if (criteria.VisitDateStart.HasValue)
            {
                query = query.Where(mc => mc.VisitDate >= criteria.VisitDateStart.Value);
            }
            if (criteria.VisitDateEnd.HasValue)
            {
                query = query.Where(mc => mc.VisitDate <= criteria.VisitDateEnd.Value);
            }
            
            // 创建时间范围
            if (criteria.CreateTimeStart.HasValue)
            {
                query = query.Where(mc => mc.CreateTime >= criteria.CreateTimeStart.Value);
            }
            if (criteria.CreateTimeEnd.HasValue)
            {
                query = query.Where(mc => mc.CreateTime <= criteria.CreateTimeEnd.Value);
            }
            
            // 关联数据过滤
            if (criteria.HasConsultation.HasValue)
            {
                var consultations = await _consultationRepository.GetAllAsync();
                var casesWithConsultation = consultations.Select(c => c.MedicalCaseId).ToHashSet();
                
                if (criteria.HasConsultation.Value)
                    query = query.Where(mc => casesWithConsultation.Contains(mc.Id));
                else
                    query = query.Where(mc => !casesWithConsultation.Contains(mc.Id));
            }
            
            if (criteria.HasPrescriptions.HasValue)
            {
                var prescriptions = await _prescriptionRepository.GetAllAsync();
                var casesWithPrescriptions = prescriptions.Select(p => p.MedicalCaseId).ToHashSet();
                
                if (criteria.HasPrescriptions.Value)
                    query = query.Where(mc => casesWithPrescriptions.Contains(mc.Id));
                else
                    query = query.Where(mc => !casesWithPrescriptions.Contains(mc.Id));
            }
            
            // 排序
            query = criteria.SortBy?.ToLower() switch
            {
                "casenumber" => criteria.SortDescending ?
                    query.OrderByDescending(mc => mc.CaseNumber) :
                    query.OrderBy(mc => mc.CaseNumber),
                "casestatus" => criteria.SortDescending ?
                    query.OrderByDescending(mc => mc.CaseStatus) :
                    query.OrderBy(mc => mc.CaseStatus),
                "createtime" => criteria.SortDescending ?
                    query.OrderByDescending(mc => mc.CreateTime) :
                    query.OrderBy(mc => mc.CreateTime),
                _ => criteria.SortDescending ?
                    query.OrderByDescending(mc => mc.VisitDate) :
                    query.OrderBy(mc => mc.VisitDate)
            };
            
            // 分页处理
            var totalCount = query.Count();
            var pagedCases = query
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToList();
            
            // 批量获取关联数据
            var patientIds = pagedCases.Select(mc => mc.PatientId).Distinct().ToList();
            var userIds = pagedCases.Select(mc => mc.UserId).Distinct().ToList();
            
            var patients = await _patientRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            
            var patientDict = patients.Where(p => patientIds.Contains(p.Id)).ToDictionary(p => p.Id);
            var userDict = users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id);
            
            // 映射到DTO
            var medicalCaseDtos = new List<MedicalCaseDto>();
            foreach (var medicalCase in pagedCases)
            {
                var dto = _mapper.Map<MedicalCaseDto>(medicalCase);
                
                if (patientDict.TryGetValue(medicalCase.PatientId, out var patient))
                {
                    dto.PatientName = patient.FullName;
                    dto.PatientAge = DateTime.Today.Year - patient.DateOfBirth.Year;
                    dto.PatientGender = patient.Gender;
                }
                
                if (userDict.TryGetValue(medicalCase.UserId, out var user))
                {
                    dto.DoctorName = user.FullName;
                }
                
                // 添加基础统计信息
                await PopulateMedicalCaseStatistics(dto, medicalCase.Id);
                medicalCaseDtos.Add(dto);
            }
            
            var pagedResult = new PagedResult<MedicalCaseDto>
            {
                Items = medicalCaseDtos,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize,
                TotalRecords = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
            };
            
            _logger.LogInformation("医案搜索完成: 找到 {TotalCount} 个结果", totalCount);
            return ServiceResult<PagedResult<MedicalCaseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医案搜索查询失败: {@Criteria}", criteria);
            return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("搜索医案失败");
        }
    }
    
    private async Task PopulateMedicalCaseStatistics(MedicalCaseDto dto, Guid medicalCaseId)
    {
        try
        {
            // 检查是否有诊断记录
            var consultation = await GetConsultationByMedicalCaseIdAsync(medicalCaseId);
            dto.HasConsultation = consultation != null;
            dto.ConsultationTime = consultation?.CreateTime;
            
            // 统计处方数量
            var prescriptions = await GetPrescriptionsByMedicalCaseIdAsync(medicalCaseId);
            dto.TotalPrescriptions = prescriptions.Count;
            dto.LastPrescriptionTime = prescriptions.OrderByDescending(p => p.CreateTime)
                                                  .FirstOrDefault()?.CreateTime;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取医案统计信息失败: {MedicalCaseId}", medicalCaseId);
        }
    }
    
    private async Task<ConsultationModel?> GetConsultationByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        var consultations = await _consultationRepository.GetAllAsync();
        return consultations.FirstOrDefault(c => c.MedicalCaseId == medicalCaseId);
    }
    
    private async Task<List<PrescriptionModel>> GetPrescriptionsByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        var prescriptions = await _prescriptionRepository.GetAllAsync();
        return prescriptions.Where(p => p.MedicalCaseId == medicalCaseId).ToList();
    }
}
```

#### MedicalCaseBusinessService业务逻辑层
```csharp
public class MedicalCaseBusinessService : IMedicalCaseBusinessService
{
    private readonly IRepository<MedicalCaseModel> _medicalCaseRepository;
    private readonly IRepository<PatientModel> _patientRepository;
    private readonly IRepository<UserModel> _userRepository;
    private readonly ICaseNumberGeneratorService _caseNumberGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalCaseBusinessService> _logger;
    
    public async Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(MedicalCaseCreateDto dto)
    {
        try
        {
            _logger.LogInformation("开始创建医案: PatientId: {PatientId}, DoctorId: {DoctorId}", 
                dto.PatientId, dto.UserId);
            
            // 1. 验证患者存在且状态有效
            var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
            if (patient == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("患者不存在");
            }
            if (patient.Status != CommonStatus.Active)
            {
                return ServiceResult<MedicalCaseDto>.Failure("患者状态无效，无法创建医案");
            }
            
            // 2. 验证医生存在且有权限
            var doctor = await _userRepository.GetByIdAsync(dto.UserId);
            if (doctor == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("医生不存在");
            }
            if (doctor.Status != CommonStatus.Active || doctor.Role != UserRole.Doctor)
            {
                return ServiceResult<MedicalCaseDto>.Failure("医生状态无效或无权限创建医案");
            }
            
            // 3. 验证就诊日期
            if (dto.VisitDate.Date > DateTime.Today)
            {
                return ServiceResult<MedicalCaseDto>.Failure("就诊日期不能晚于今天");
            }
            if (dto.VisitDate.Date < patient.DateOfBirth.Date)
            {
                return ServiceResult<MedicalCaseDto>.Failure("就诊日期不能早于患者出生日期");
            }
            
            // 4. 生成医案编号
            string caseNumber;
            if (dto.AutoGenerateCaseNumber)
            {
                var generateResult = await _caseNumberGenerator.GenerateNextCaseNumberAsync();
                if (!generateResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure($"生成医案编号失败: {generateResult.Message}");
                }
                caseNumber = generateResult.Data!;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.CustomCaseNumber))
                {
                    return ServiceResult<MedicalCaseDto>.Failure("自定义医案编号不能为空");
                }
                
                // 检查编号唯一性
                var existingCases = await _medicalCaseRepository.GetAllAsync();
                if (existingCases.Any(mc => mc.CaseNumber == dto.CustomCaseNumber))
                {
                    return ServiceResult<MedicalCaseDto>.Failure("医案编号已存在");
                }
                caseNumber = dto.CustomCaseNumber;
            }
            
            // 5. 创建医案实体
            var medicalCase = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                UserId = dto.UserId,
                CaseNumber = caseNumber,
                VisitDate = dto.VisitDate,
                CaseStatus = MedicalCaseStatus.Registered,
                ChiefComplaint = dto.ChiefComplaint,
                Remark = dto.Remark,
                CreateTime = DateTime.UtcNow,
                Status = CommonStatus.Active
            };
            
            // 6. 保存到数据库
            var createdCase = await _medicalCaseRepository.CreateAsync(medicalCase);
            
            // 7. 记录操作日志
            _logger.LogInformation("医案创建成功: CaseNumber: {CaseNumber}, MedicalCaseId: {Id}, " +
                                 "Patient: {PatientName}, Doctor: {DoctorName}", 
                caseNumber, createdCase.Id, patient.FullName, doctor.FullName);
            
            // 8. 构建返回DTO
            var resultDto = _mapper.Map<MedicalCaseDto>(createdCase);
            resultDto.PatientName = patient.FullName;
            resultDto.PatientAge = DateTime.Today.Year - patient.DateOfBirth.Year;
            resultDto.PatientGender = patient.Gender;
            resultDto.DoctorName = doctor.FullName;
            
            return ServiceResult<MedicalCaseDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建医案异常: PatientId: {PatientId}, DoctorId: {DoctorId}", 
                dto.PatientId, dto.UserId);
            return ServiceResult<MedicalCaseDto>.Failure("创建医案失败，请稍后重试");
        }
    }
    
    public async Task<ServiceResult<bool>> UpdateMedicalCaseStatusAsync(Guid id, MedicalCaseStatus status)
    {
        try
        {
            _logger.LogInformation("更新医案状态: MedicalCaseId: {Id}, NewStatus: {Status}", id, status);
            
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(id);
            if (medicalCase == null)
            {
                return ServiceResult<bool>.Failure("医案不存在");
            }
            
            // 验证状态流转规则
            var isValidTransition = ValidateStatusTransition(medicalCase.CaseStatus, status);
            if (!isValidTransition)
            {
                return ServiceResult<bool>.Failure(
                    $"无效的状态转换: 从 {medicalCase.CaseStatus} 到 {status}");
            }
            
            // 根据目标状态进行额外验证
            switch (status)
            {
                case MedicalCaseStatus.InProgress:
                    // 开始诊疗时的验证
                    break;
                    
                case MedicalCaseStatus.Completed:
                    // 完成诊疗时验证是否有诊断记录
                    var hasConsultation = await HasConsultationAsync(id);
                    if (!hasConsultation)
                    {
                        return ServiceResult<bool>.Failure("完成医案前必须先进行诊断");
                    }
                    break;
            }
            
            // 更新状态
            var affectedRows = await _medicalCaseRepository.ExecuteUpdateAsync(
                mc => mc.Id == id,
                setters => setters
                    .SetProperty(mc => mc.CaseStatus, status)
                    .SetProperty(mc => mc.UpdateTime, DateTime.UtcNow));
            
            if (affectedRows > 0)
            {
                _logger.LogInformation("医案状态更新成功: MedicalCaseId: {Id}, Status: {Status}", id, status);
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                _logger.LogWarning("医案状态更新失败: MedicalCaseId: {Id}", id);
                return ServiceResult<bool>.Failure("医案状态更新失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新医案状态异常: MedicalCaseId: {Id}, Status: {Status}", id, status);
            return ServiceResult<bool>.Failure("更新医案状态失败");
        }
    }
    
    public async Task<ServiceResult<string>> GenerateCaseNumberAsync()
    {
        try
        {
            return await _caseNumberGenerator.GenerateNextCaseNumberAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成医案编号异常");
            return ServiceResult<string>.Failure("生成医案编号失败");
        }
    }
    
    private bool ValidateStatusTransition(MedicalCaseStatus currentStatus, MedicalCaseStatus newStatus)
    {
        // 定义允许的状态转换规则
        var allowedTransitions = new Dictionary<MedicalCaseStatus, List<MedicalCaseStatus>>
        {
            { MedicalCaseStatus.Registered, new List<MedicalCaseStatus> { MedicalCaseStatus.InProgress } },
            { MedicalCaseStatus.InProgress, new List<MedicalCaseStatus> { MedicalCaseStatus.Completed, MedicalCaseStatus.Registered } },
            { MedicalCaseStatus.Completed, new List<MedicalCaseStatus> { MedicalCaseStatus.InProgress } } // 允许重新开始
        };
        
        if (!allowedTransitions.TryGetValue(currentStatus, out var allowedNextStates))
        {
            return false;
        }
        
        return allowedNextStates.Contains(newStatus);
    }
    
    private async Task<bool> HasConsultationAsync(Guid medicalCaseId)
    {
        // 这里需要调用Consultation模块的服务来检查是否有诊断记录
        // 由于是查询操作，可以直接查询数据库
        var consultations = await _consultationRepository.GetAllAsync();
        return consultations.Any(c => c.MedicalCaseId == medicalCaseId);
    }
}
```

### 医案编号生成服务
```csharp
public interface ICaseNumberGeneratorService
{
    Task<ServiceResult<string>> GenerateNextCaseNumberAsync();
    Task<ServiceResult<bool>> IsCaseNumberExistsAsync(string caseNumber);
}

public class CaseNumberGeneratorService : ICaseNumberGeneratorService
{
    private readonly IRepository<MedicalCaseModel> _medicalCaseRepository;
    private readonly ILogger<CaseNumberGeneratorService> _logger;
    private static readonly object _lockObject = new object();
    
    public CaseNumberGeneratorService(IRepository<MedicalCaseModel> medicalCaseRepository,
                                    ILogger<CaseNumberGeneratorService> logger)
    {
        _medicalCaseRepository = medicalCaseRepository;
        _logger = logger;
    }
    
    public async Task<ServiceResult<string>> GenerateNextCaseNumberAsync()
    {
        lock (_lockObject)
        {
            try
            {
                var today = DateTime.Today;
                var datePrefix = today.ToString("yyyyMMdd");
                var pattern = $"MC{datePrefix}";
                
                // 查找当天已有的医案编号
                var existingCases = _medicalCaseRepository.GetAllAsync().Result;
                var todayCases = existingCases
                    .Where(mc => mc.CaseNumber != null && mc.CaseNumber.StartsWith(pattern))
                    .ToList();
                
                // 获取当天的最大序号
                var maxSequence = 0;
                foreach (var existingCase in todayCases)
                {
                    if (existingCase.CaseNumber!.Length == pattern.Length + 4) // MC + 8位日期 + 4位序号
                    {
                        var sequencePart = existingCase.CaseNumber.Substring(pattern.Length);
                        if (int.TryParse(sequencePart, out var sequence))
                        {
                            maxSequence = Math.Max(maxSequence, sequence);
                        }
                    }
                }
                
                // 生成新的序号
                var nextSequence = maxSequence + 1;
                var caseNumber = $"{pattern}{nextSequence:D4}";
                
                _logger.LogInformation("生成医案编号: {CaseNumber}, 当天第 {Sequence} 个", caseNumber, nextSequence);
                
                return ServiceResult<string>.Success(caseNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成医案编号异常");
                return ServiceResult<string>.Failure("生成医案编号失败");
            }
        }
    }
    
    public async Task<ServiceResult<bool>> IsCaseNumberExistsAsync(string caseNumber)
    {
        try
        {
            var existingCases = await _medicalCaseRepository.GetAllAsync();
            var exists = existingCases.Any(mc => mc.CaseNumber == caseNumber);
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查医案编号是否存在异常: {CaseNumber}", caseNumber);
            return ServiceResult<bool>.Failure("检查医案编号失败");
        }
    }
}
```

### 命名规范
- **服务类**: PascalCase + Service后缀 (MedicalCaseService, CaseNumberGeneratorService)
- **DTO类**: PascalCase + Dto后缀 (MedicalCaseCreateDto, MedicalCaseSearchDto)
- **状态枚举**: PascalCase (MedicalCaseStatus)
- **异常类**: PascalCase + Exception后缀 (InvalidStatusTransitionException)
- **接口**: I前缀 + PascalCase (IMedicalCaseService)
- **方法**: PascalCase，异步方法Async后缀，状态相关方法包含Status

### 质量标准
- **状态一致性**: 确保医案状态流转符合业务规则，不允许跳跃性状态变更
- **编号唯一性**: 医案编号全局唯一，支持并发安全的编号生成
- **关联数据完整性**: 确保与患者、医生、诊断、处方的关联关系正确
- **性能要求**: 医案查询<2秒，医案创建<1秒，状态更新<500ms
- **缓存策略**: 医案统计信息缓存10分钟，医案状态变更时清除相关缓存
- **并发安全**: 支持多医生同时创建医案，确保编号生成的线程安全

### 测试要求
- **单元测试覆盖率**: >85%，特别是状态流转和业务规则验证
- **集成测试**: 完整的医案生命周期流程测试
- **状态机测试**: 所有可能的状态转换场景测试
- **并发测试**: 医案编号生成的并发安全测试

## 🔌 集成接口

### 对外提供的接口

#### 1. RESTful API接口
```http
# 获取医案列表
GET /api/v1/medicalcase?pageNumber=1&pageSize=10&caseStatus=InProgress&doctorName=张医生
Authorization: Bearer <access_token>

# 响应
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "123e4567-e89b-12d3-a456-426614174000",
                "caseNumber": "MC202509010001",
                "patientName": "张三",
                "patientAge": 45,
                "patientGender": "Male",
                "doctorName": "李医生",
                "visitDate": "2025-09-01T09:30:00Z",
                "caseStatus": "InProgress",
                "chiefComplaint": "头痛、失眠",
                "hasConsultation": true,
                "totalPrescriptions": 2,
                "createTime": "2025-09-01T09:00:00Z"
            }
        ],
        "pageNumber": 1,
        "pageSize": 10,
        "totalRecords": 25,
        "totalPages": 3
    }
}

# 创建新医案
POST /api/v1/medicalcase
Authorization: Bearer <access_token>
{
    "patientId": "123e4567-e89b-12d3-a456-426614174000",
    "userId": "456e7890-e89b-12d3-a456-426614174001",
    "visitDate": "2025-09-01T10:00:00Z",
    "chiefComplaint": "胸闷、心悸、失眠",
    "autoGenerateCaseNumber": true
}

# 更新医案状态
PUT /api/v1/medicalcase/{id}/status
Authorization: Bearer <access_token>
{
    "status": "Completed"
}

# 获取医案详情
GET /api/v1/medicalcase/{id}
Authorization: Bearer <access_token>

# 响应（包含完整关联信息）
{
    "success": true,
    "data": {
        "id": "guid",
        "caseNumber": "MC202509010001",
        "patientName": "张三",
        "doctorName": "李医生",
        "visitDate": "2025-09-01T10:00:00Z",
        "caseStatus": "Completed",
        "consultation": {
            "id": "guid",
            "tcmDiagnosis": "心肾不交",
            "treatment": "滋阴降火，交通心肾"
        },
        "prescriptions": [
            {
                "id": "guid",
                "indication": "心肾不交证",
                "dosageCount": 7,
                "totalPrice": 156.50
            }
        ],
        "totalTreatmentDuration": "02:30:00"
    }
}

# 获取患者医案历史
GET /api/v1/medicalcase/patient/{patientId}
Authorization: Bearer <access_token>

# 获取医案统计
GET /api/v1/medicalcase/statistics
Authorization: Bearer <access_token>

# 生成医案编号
GET /api/v1/medicalcase/generate-case-number
Authorization: Bearer <access_token>
```

#### 2. 内部服务接口
```csharp
// 其他业务模块可以通过依赖注入使用
public class ConsultationBusinessService
{
    private readonly IMedicalCaseService _medicalCaseService;
    
    public async Task<bool> ValidateMedicalCaseForConsultation(Guid medicalCaseId)
    {
        var result = await _medicalCaseService.GetByIdAsync(medicalCaseId);
        return result.IsSuccess && 
               result.Data?.CaseStatus == MedicalCaseStatus.InProgress &&
               result.Data?.Status == CommonStatus.Active;
    }
}

public class PrescriptionBusinessService
{
    private readonly IMedicalCaseService _medicalCaseService;
    
    public async Task<string> GetMedicalCaseNumber(Guid medicalCaseId)
    {
        var result = await _medicalCaseService.GetByIdAsync(medicalCaseId);
        return result.IsSuccess ? result.Data?.CaseNumber ?? "未知医案" : "未知医案";
    }
}
```

### 依赖的外部接口
- **IRepository<MedicalCaseModel>**: Infrastructure提供的医案数据访问接口
- **IRepository<PatientModel>**: Patients模块患者数据访问
- **IRepository<UserModel>**: Users模块医生数据访问
- **IRepository<ConsultationModel>**: Consultation模块诊断数据（关联查询）
- **IRepository<PrescriptionModel>**: Prescription模块处方数据（关联查询）
- **IMapper**: AutoMapper对象映射服务
- **IMemoryCache**: .NET内存缓存服务
- **ILogger<T>**: .NET结构化日志服务

### 数据传输格式

#### 医案完整信息响应格式
```json
{
    "success": true,
    "message": "获取医案信息成功",
    "data": {
        "id": "guid",
        "patientId": "guid",
        "patientName": "string",
        "patientAge": number,
        "patientGender": "Male|Female|Other",
        "userId": "guid", 
        "doctorName": "string",
        "caseNumber": "string",
        "visitDate": "datetime",
        "caseStatus": "Registered|InProgress|Completed",
        "caseStatusName": "已登记|诊疗中|已完成",
        "chiefComplaint": "string",
        "remark": "string",
        "createTime": "datetime",
        "updateTime": "datetime",
        "consultation": {
            "id": "guid",
            "tcmDiagnosis": "string",
            "treatment": "string",
            "createTime": "datetime"
        },
        "prescriptions": [
            {
                "id": "guid", 
                "indication": "string",
                "dosageCount": number,
                "totalPrice": number,
                "createTime": "datetime"
            }
        ],
        "hasConsultation": boolean,
        "totalPrescriptions": number,
        "consultationTime": "datetime",
        "lastPrescriptionTime": "datetime", 
        "totalTreatmentDuration": "timespan"
    }
}
```

#### 医案统计响应格式
```json
{
    "success": true,
    "data": {
        "totalMedicalCases": 1500,
        "registeredCases": 50,
        "inProgressCases": 120,
        "completedCases": 1330,
        "casesWithConsultation": 1450,
        "casesWithPrescriptions": 1350,
        "casesToday": 25,
        "casesThisWeek": 180,
        "casesThisMonth": 720,
        "averageConsultationTime": 45.5,
        "averageTreatmentDuration": 120.0,
        "completionRate": 88.67,
        "statusDistribution": [
            {
                "status": "Registered",
                "count": 50,
                "percentage": 3.33
            },
            {
                "status": "InProgress", 
                "count": 120,
                "percentage": 8.0
            },
            {
                "status": "Completed",
                "count": 1330, 
                "percentage": 88.67
            }
        ],
        "doctorWorkload": [
            {
                "doctorId": "guid",
                "doctorName": "张医生",
                "totalCases": 150,
                "completedCases": 140,
                "inProgressCases": 10,
                "completionRate": 93.33,
                "averageConsultationTime": 42.0,
                "casesToday": 5
            }
        ],
        "dailyTrend": [
            {
                "date": "2025-09-01",
                "registeredCases": 8,
                "completedCases": 12,
                "inProgressCases": 5
            }
        ]
    }
}
```

### 错误处理规范
- **400 Bad Request**: 医案信息验证失败或状态转换无效
- **404 Not Found**: 指定的医案、患者或医生不存在
- **409 Conflict**: 医案编号冲突或状态冲突
- **422 Unprocessable Entity**: 业务规则验证失败（如完成医案前未进行诊断）
- **500 Internal Server Error**: 服务器内部错误

## ⚙️ 配置管理

### 配置项定义

#### 医案管理相关配置
```json
{
  "MedicalCaseOptions": {
    "EnableAutoGenerateCaseNumber": true,
    "CaseNumberFormat": "MC{0:yyyyMMdd}{1:D4}",
    "CaseNumberStartSequence": 1,
    "EnableCustomCaseNumber": true,
    "RequireChiefComplaint": false,
    "MaxChiefComplaintLength": 500,
    "MaxRemarkLength": 1000,
    "EnableMedicalCaseStatisticsCache": true,
    "StatisticsCacheMinutes": 10,
    "DefaultMedicalCaseStatus": "Registered"
  },
  "MedicalCaseWorkflowOptions": {
    "EnableStatusValidation": true,
    "AllowStatusRollback": true,
    "RequireConsultationForCompletion": true,
    "RequireAtLeastOnePrescription": false,
    "EnableAutoStatusUpdate": false,
    "MaxConcurrentCasesPerDoctor": 50,
    "AutoCompleteAfterHours": 0
  },
  "MedicalCaseValidationOptions": {
    "ValidatePatientStatus": true,
    "ValidateDoctorPermission": true,
    "ValidateVisitDate": true,
    "AllowFutureVisitDate": false,
    "AllowVisitBeforeBirth": false,
    "EnableDuplicateCheck": false,
    "DuplicateCheckWindowHours": 24
  }
}
```

### 环境变量要求
```bash
# 医案管理配置
MEDICALCASEOPTIONS__ENABLEAUTOGENERATECASENUMBER=true
MEDICALCASEOPTIONS__CASENUMBERFORMAT="MC{0:yyyyMMdd}{1:D4}"
MEDICALCASEOPTIONS__ENABLECUSTOMCASENUMBER=true

# 工作流配置
MEDICALCASEWORKFLOWOPTIONS__ENABLESTATUSVALIDATION=true
MEDICALCASEWORKFLOWOPTIONS__ALLOWSTATUSROLLBACK=true
MEDICALCASEWORKFLOWOPTIONS__REQUIRECONSULTATIONFORCOMPLETION=true

# 验证配置
MEDICALCASEVALIDATIONOPTIONS__VALIDATEPATIENTSTATUS=true
MEDICALCASEVALIDATIONOPTIONS__VALIDATEDOCTORPERMISSION=true
MEDICALCASEVALIDATIONOPTIONS__ALLOWFUTUREVISITDATE=false
```

### 部署配置说明
1. **开发环境**: 允许自定义编号和状态回滚，便于测试各种场景
2. **测试环境**: 启用完整的业务规则验证，接近生产环境
3. **生产环境**: 严格的状态流转和权限验证，确保诊疗流程规范
4. **数据恢复**: 支持医案数据的备份和恢复，保证诊疗记录完整性

## 🧪 测试规范

### 单元测试要求

#### 医案业务逻辑测试
```csharp
public class MedicalCaseBusinessServiceTests : IDisposable
{
    private readonly Mock<IRepository<MedicalCaseModel>> _mockMedicalCaseRepository;
    private readonly Mock<IRepository<PatientModel>> _mockPatientRepository;
    private readonly Mock<IRepository<UserModel>> _mockUserRepository;
    private readonly Mock<ICaseNumberGeneratorService> _mockCaseNumberGenerator;
    private readonly MedicalCaseBusinessService _service;
    
    [Fact]
    public async Task CreateMedicalCaseAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var dto = new MedicalCaseCreateDto
        {
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            VisitDate = DateTime.Today,
            ChiefComplaint = "测试主诉",
            AutoGenerateCaseNumber = true
        };
        
        var patient = new PatientModel 
        { 
            Id = dto.PatientId, 
            FullName = "测试患者", 
            Status = CommonStatus.Active,
            DateOfBirth = DateTime.Today.AddYears(-30)
        };
        var doctor = new UserModel 
        { 
            Id = dto.UserId, 
            FullName = "测试医生", 
            Role = UserRole.Doctor, 
            Status = CommonStatus.Active 
        };
        
        _mockPatientRepository.Setup(r => r.GetByIdAsync(dto.PatientId))
                              .ReturnsAsync(patient);
        _mockUserRepository.Setup(r => r.GetByIdAsync(dto.UserId))
                           .ReturnsAsync(doctor);
        _mockCaseNumberGenerator.Setup(g => g.GenerateNextCaseNumberAsync())
                                .ReturnsAsync(ServiceResult<string>.Success("MC202509010001"));
        
        // Act
        var result = await _service.CreateMedicalCaseAsync(dto);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CaseNumber.Should().Be("MC202509010001");
    }
    
    [Theory]
    [InlineData(MedicalCaseStatus.Registered, MedicalCaseStatus.InProgress, true)]
    [InlineData(MedicalCaseStatus.InProgress, MedicalCaseStatus.Completed, true)]
    [InlineData(MedicalCaseStatus.Registered, MedicalCaseStatus.Completed, false)]
    public async Task UpdateMedicalCaseStatusAsync_StatusTransition_ReturnsExpectedResult(
        MedicalCaseStatus currentStatus, MedicalCaseStatus newStatus, bool expectedSuccess)
    {
        // 测试状态转换规则
    }
    
    [Fact]
    public async Task CreateMedicalCaseAsync_FutureVisitDate_ReturnsFailure()
    {
        // 测试未来就诊日期验证
    }
    
    [Fact]
    public async Task GenerateCaseNumberAsync_ConcurrentRequests_ReturnsUniqueNumbers()
    {
        // 测试并发编号生成的唯一性
    }
}
```

#### 医案状态机测试
```csharp
public class MedicalCaseStatusTransitionTests
{
    [Theory]
    [InlineData(MedicalCaseStatus.Registered, MedicalCaseStatus.InProgress)]
    [InlineData(MedicalCaseStatus.InProgress, MedicalCaseStatus.Completed)]
    [InlineData(MedicalCaseStatus.InProgress, MedicalCaseStatus.Registered)]
    public void ValidateStatusTransition_ValidTransitions_ReturnsTrue(
        MedicalCaseStatus from, MedicalCaseStatus to)
    {
        // 测试有效状态转换
    }
    
    [Theory]
    [InlineData(MedicalCaseStatus.Registered, MedicalCaseStatus.Completed)]
    [InlineData(MedicalCaseStatus.Completed, MedicalCaseStatus.Registered)]
    public void ValidateStatusTransition_InvalidTransitions_ReturnsFalse(
        MedicalCaseStatus from, MedicalCaseStatus to)
    {
        // 测试无效状态转换
    }
}
```

### 集成测试要求

#### 医案API集成测试
```csharp
public class MedicalCaseApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task POST_MedicalCase_ValidData_CreatesMedicalCase()
    {
        // 测试创建医案API
    }
    
    [Fact]
    public async Task PUT_MedicalCaseStatus_ValidTransition_UpdatesStatus()
    {
        // 测试状态更新API
    }
    
    [Fact]
    public async Task GET_MedicalCase_WithDetails_ReturnsCompleteInformation()
    {
        // 测试获取医案详情API，包含关联数据
    }
    
    [Fact]
    public async Task GET_PatientMedicalCases_ValidPatient_ReturnsHistory()
    {
        // 测试患者医案历史API
    }
}
```

### 性能测试要求
```csharp
public class MedicalCasePerformanceTests
{
    [Fact]
    public async Task SearchMedicalCases_LargeDataset_CompletesWithinTimeLimit()
    {
        // 测试大数据量医案搜索性能
        // 目标: 100000个医案的搜索在2秒内完成
    }
    
    [Fact]
    public async Task CreateMedicalCase_ConcurrentRequests_HandlesLoad()
    {
        // 测试并发创建医案性能
        // 目标: 100个并发请求在5秒内完成
    }
    
    [Fact]
    public async Task GenerateCaseNumber_HighConcurrency_MaintainsUniqueness()
    {
        // 测试高并发编号生成
        // 目标: 1000个并发编号生成全部唯一
    }
}
```

### 测试覆盖率目标
- **核心业务逻辑**: >90%覆盖率
- **状态机逻辑**: 100%覆盖率
- **查询服务**: >85%覆盖率
- **API端点**: >80%覆盖率
- **编号生成器**: 100%覆盖率

## 🚀 部署说明

### 构建要求
- **.NET 8.0 SDK**: 编译MedicalCase模块
- **AutoMapper依赖**: 复杂对象映射库
- **FluentValidation依赖**: 业务规则验证库
- **Entity Framework Core**: 数据访问和关联查询

### 部署步骤

#### 1. 模块部署验证
```bash
# 验证MedicalCase模块编译
dotnet build LYBT.Module.MedicalCase.csproj

# 验证服务注册
dotnet run --project LYBT.WebAPI
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/v1/medicalcase
```

#### 2. 医案编号生成测试
```bash
# 测试医案编号生成
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/medicalcase/generate-case-number

# 测试医案创建
curl -X POST http://localhost:5000/api/v1/medicalcase \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"patientId":"guid","userId":"guid","visitDate":"2025-09-01T10:00:00Z","autoGenerateCaseNumber":true}'
```

#### 3. 状态流转测试
```bash
# 测试状态更新
curl -X PUT http://localhost:5000/api/v1/medicalcase/{id}/status \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"status":"InProgress"}'
```

### 环境依赖
- **数据库访问**: 需要MedicalCaseModel及相关表的读写权限
- **关联模块**: 依赖Patients、Users、Consultation、Prescriptions模块的数据
- **缓存服务**: 统计功能需要缓存服务支持
- **并发控制**: 编号生成需要并发安全保障

### 运行监控

#### 医案模块性能监控
```http
# 医案操作性能指标
GET /api/v1/monitoring/medicalcase/performance

# 医案状态分布监控
GET /api/v1/monitoring/medicalcase/status-distribution

# 医案编号生成监控
GET /api/v1/monitoring/medicalcase/case-number-generation
```

#### 业务流程监控
```http
# 诊疗流程完成率
GET /api/v1/monitoring/medicalcase/completion-rate

# 医生工作负荷监控
GET /api/v1/monitoring/medicalcase/doctor-workload

# 患者就诊频率分析
GET /api/v1/monitoring/medicalcase/patient-visit-frequency
```

## 📚 相关文档

### 相关项目文档链接
- [LYBT.Module.Patients项目文档](./patients.md) - 患者档案管理
- [LYBT.Module.Users项目文档](./users.md) - 医生用户管理
- [LYBT.Module.Consultation项目文档](./consultation.md) - 诊断记录（1:1关联）
- [LYBT.Module.Prescriptions项目文档](./prescriptions.md) - 处方记录（1:N关联）

### API文档链接
- [医案管理API规范](../../../api/medicalcase-api.md) - 完整的医案管理REST API
- [医案状态流转API](../../../api/medicalcase-status-api.md) - 状态管理接口
- [医案统计API](../../../api/medicalcase-statistics-api.md) - 统计分析接口

### 技术规范引用
- [UltraThink双层架构标准](../../../ultrathink/ultrathink-comprehensive-refactoring-complete-20250831.md) - 架构实施标准
- [状态机设计模式](../../../development/state-machine-pattern.md) - 状态流转实现指南
- [医疗流程管理规范](../../../business/medical-workflow-management.md) - 诊疗流程标准
- [医案编号生成规范](../../../development/case-number-generation.md) - 编号系统设计

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ 已审核通过