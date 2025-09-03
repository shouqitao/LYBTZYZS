# Consultation Module (看诊诊断模块)

## 📋 项目概述

### 项目定位
**Consultation 模块**是凌隐宝堂中医诊所系统的**核心诊断数据记录模块**，负责中医四诊（望闻问切）的数据存储和辨证论治记录。作为纯数据记录模块，专注于诊断信息的结构化管理，不涉及诊疗流程控制。

### 核心价值
- 🏥 **中医四诊标准化**: 望闻问切数据结构化存储
- 📋 **辨证论治记录**: 中医诊断思路完整记录
- 🔗 **医案集成**: 与MedicalCase 1:1关联，构成完整病历
- 📊 **数据专业化**: 纯数据记录定位，支持多种业务场景
- 🎯 **诊断质量**: 提升中医诊断的标准化和可追溯性

### 业务定位 (v1.0)
```
MedicalCase (诊疗流程容器)
    ↓ 1:1 关系
Consultation (诊断数据记录) ← 本模块
    ↓ 关联查询
Prescriptions (处方记录)
```

## 🏗️ 技术架构

### UltraThink双层架构实现
```
ConsultationService (主服务 - 纯委托层)
├── ConsultationQueryService (查询专业层)
│   ├── 诊断记录检索 (按医案、患者、时间范围)
│   ├── 四诊数据统计 (症状分布、诊断类型)
│   ├── 辨证论治分析 (治则治法统计)
│   └── 诊断质量报表 (完整度、规范性)
└── ConsultationBusinessService (业务逻辑层)
    ├── 四诊记录管理 (望闻问切数据CRUD)
    ├── 辨证论治处理 (诊断逻辑、治疗原则)
    ├── 医案关联验证 (MedicalCase状态检查)
    └── 数据完整性校验 (必填项、格式验证)
```

### 技术栈配置
```csharp
// 基础技术栈
- .NET 8.0
- Entity Framework Core 8.0.17
- AutoMapper 配置
- BCrypt.Net 密码处理
- 依赖注入模式

// 模块注册 (Program.cs)
builder.Services.AddConsultationModule();

public static class ConsultationModuleExtensions
{
    public static IServiceCollection AddConsultationModule(this IServiceCollection services)
    {
        // Repository Layer
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        
        // Service Layer - UltraThink双层架构
        services.AddScoped<ConsultationQueryService>();
        services.AddScoped<ConsultationBusinessService>();
        services.AddScoped<IConsultationService, ConsultationService>(); // 纯委托
        
        return services;
    }
}
```

### 核心实体模型
```csharp
public class ConsultationModel : BaseEntity
{
    [Required]
    public Guid MedicalCaseId { get; set; }  // 1:1关联医案
    
    // 中医四诊记录
    public string? Observation { get; set; }        // 望诊 - 气色面容
    public string? Listening { get; set; }          // 闻诊 - 声音气味
    public string? Inquiry { get; set; }            // 问诊 - 症状询问
    public string? Palpation { get; set; }          // 切诊 - 脉象舌象
    
    // 辨证论治
    public string? Diagnosis { get; set; }          // 中医诊断
    public string? Syndrome { get; set; }           // 证候分析
    public string? Treatment { get; set; }          // 治法治则
    public string? Principle { get; set; }          // 治疗原则
    
    // 诊断质量
    public DiagnosisStatus Status { get; set; }     // 诊断状态
    public DateTime ConsultationDate { get; set; }  // 诊断时间
    
    // 导航属性
    public virtual MedicalCaseModel MedicalCase { get; set; }
}

public enum DiagnosisStatus
{
    Draft = 0,      // 草稿
    InProgress = 1, // 诊断中
    Completed = 2   // 诊断完成
}
```

## 🎯 功能规范

### 核心业务功能

#### 1. 四诊记录管理
```csharp
// 业务服务实现
public class ConsultationBusinessService
{
    // 创建诊断记录
    public async Task<ServiceResult<ConsultationDto>> CreateConsultationAsync(
        ConsultationCreateDto dto)
    {
        // 验证医案状态
        var medicalCase = await _medicalCaseRepository.GetByIdAsync(dto.MedicalCaseId);
        if (medicalCase?.Status != MedicalCaseStatus.InProgress)
            return ServiceResult<ConsultationDto>.Failure("只能为进行中的医案创建诊断记录");
        
        // 检查重复诊断
        var existing = await _repository.GetByMedicalCaseIdAsync(dto.MedicalCaseId);
        if (existing != null)
            return ServiceResult<ConsultationDto>.Failure("该医案已存在诊断记录");
        
        var consultation = _mapper.Map<ConsultationModel>(dto);
        consultation.Status = DiagnosisStatus.Draft;
        consultation.ConsultationDate = DateTime.Now;
        
        await _repository.CreateAsync(consultation);
        var result = _mapper.Map<ConsultationDto>(consultation);
        
        return ServiceResult<ConsultationDto>.Success(result);
    }
    
    // 更新四诊记录
    public async Task<ServiceResult<ConsultationDto>> UpdateFourExaminationsAsync(
        Guid id, FourExaminationsDto dto)
    {
        var consultation = await _repository.GetByIdAsync(id);
        if (consultation == null)
            return ServiceResult<ConsultationDto>.Failure("诊断记录不存在");
            
        if (consultation.Status == DiagnosisStatus.Completed)
            return ServiceResult<ConsultationDto>.Failure("已完成的诊断记录不能修改");
        
        // 更新四诊数据
        consultation.Observation = dto.Observation;
        consultation.Listening = dto.Listening;
        consultation.Inquiry = dto.Inquiry;
        consultation.Palpation = dto.Palpation;
        
        await _repository.UpdateAsync(consultation);
        var result = _mapper.Map<ConsultationDto>(consultation);
        
        return ServiceResult<ConsultationDto>.Success(result);
    }
}
```

#### 2. 辨证论治处理
```csharp
// 辨证论治业务逻辑
public async Task<ServiceResult<ConsultationDto>> UpdateDiagnosisAsync(
    Guid id, DiagnosisDto dto)
{
    var consultation = await _repository.GetByIdAsync(id);
    if (consultation == null)
        return ServiceResult<ConsultationDto>.Failure("诊断记录不存在");
    
    // 验证四诊完整性
    if (string.IsNullOrEmpty(consultation.Observation) || 
        string.IsNullOrEmpty(consultation.Inquiry))
        return ServiceResult<ConsultationDto>.Failure("请先完成四诊记录");
    
    // 更新辨证论治
    consultation.Diagnosis = dto.Diagnosis;
    consultation.Syndrome = dto.Syndrome;
    consultation.Treatment = dto.Treatment;
    consultation.Principle = dto.Principle;
    consultation.Status = DiagnosisStatus.InProgress;
    
    await _repository.UpdateAsync(consultation);
    var result = _mapper.Map<ConsultationDto>(consultation);
    
    return ServiceResult<ConsultationDto>.Success(result);
}
```

#### 3. 诊断完成处理
```csharp
// 诊断完成业务流程
public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id)
{
    var consultation = await _repository.GetByIdAsync(id);
    if (consultation == null)
        return ServiceResult<bool>.Failure("诊断记录不存在");
    
    // 验证诊断完整性
    var validationResult = ValidateConsultationCompleteness(consultation);
    if (!validationResult.Success)
        return validationResult;
    
    consultation.Status = DiagnosisStatus.Completed;
    await _repository.UpdateAsync(consultation);
    
    return ServiceResult<bool>.Success(true);
}

private ServiceResult<bool> ValidateConsultationCompleteness(ConsultationModel consultation)
{
    if (string.IsNullOrEmpty(consultation.Diagnosis))
        return ServiceResult<bool>.Failure("必须填写中医诊断");
        
    if (string.IsNullOrEmpty(consultation.Treatment))
        return ServiceResult<bool>.Failure("必须填写治法治则");
        
    return ServiceResult<bool>.Success(true);
}
```

### 查询服务专业功能

#### 1. 诊断记录检索
```csharp
public class ConsultationQueryService
{
    // 按医案获取诊断
    public async Task<ServiceResult<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
        if (consultation == null)
            return ServiceResult<ConsultationDto>.Failure("未找到诊断记录");
            
        var result = _mapper.Map<ConsultationDto>(consultation);
        return ServiceResult<ConsultationDto>.Success(result);
    }
    
    // 患者诊断历史
    public async Task<ServiceResult<List<ConsultationHistoryDto>>> GetPatientConsultationHistoryAsync(
        Guid patientId)
    {
        var consultations = await _repository.GetPatientConsultationHistoryAsync(patientId);
        var results = _mapper.Map<List<ConsultationHistoryDto>>(consultations);
        
        return ServiceResult<List<ConsultationHistoryDto>>.Success(results);
    }
    
    // 诊断记录搜索
    public async Task<ServiceResult<PagedResult<ConsultationSearchResultDto>>> SearchConsultationsAsync(
        ConsultationSearchDto criteria)
    {
        var query = _repository.GetQueryable();
        
        // 时间范围过滤
        if (criteria.StartDate.HasValue)
            query = query.Where(c => c.ConsultationDate >= criteria.StartDate.Value);
        if (criteria.EndDate.HasValue)
            query = query.Where(c => c.ConsultationDate <= criteria.EndDate.Value);
            
        // 诊断状态过滤
        if (criteria.Status.HasValue)
            query = query.Where(c => c.Status == criteria.Status.Value);
            
        // 症状关键词搜索
        if (!string.IsNullOrEmpty(criteria.SymptomKeyword))
        {
            query = query.Where(c => 
                c.Inquiry.Contains(criteria.SymptomKeyword) ||
                c.Observation.Contains(criteria.SymptomKeyword));
        }
        
        var pagedResult = await _repository.GetPagedAsync(query, criteria.Page, criteria.PageSize);
        return ServiceResult<PagedResult<ConsultationSearchResultDto>>.Success(pagedResult);
    }
}
```

#### 2. 统计分析功能
```csharp
// 四诊数据统计
public async Task<ServiceResult<ConsultationStatisticsDto>> GetConsultationStatisticsAsync(
    DateTime startDate, DateTime endDate)
{
    var consultations = await _repository.GetConsultationsByDateRangeAsync(startDate, endDate);
    
    var statistics = new ConsultationStatisticsDto
    {
        TotalConsultations = consultations.Count,
        CompletedConsultations = consultations.Count(c => c.Status == DiagnosisStatus.Completed),
        InProgressConsultations = consultations.Count(c => c.Status == DiagnosisStatus.InProgress),
        DraftConsultations = consultations.Count(c => c.Status == DiagnosisStatus.Draft),
        
        // 四诊完整性统计
        CompleteObservations = consultations.Count(c => !string.IsNullOrEmpty(c.Observation)),
        CompleteInquiries = consultations.Count(c => !string.IsNullOrEmpty(c.Inquiry)),
        CompletePalpations = consultations.Count(c => !string.IsNullOrEmpty(c.Palpation)),
        
        // 诊断质量统计
        WithDiagnosis = consultations.Count(c => !string.IsNullOrEmpty(c.Diagnosis)),
        WithTreatment = consultations.Count(c => !string.IsNullOrEmpty(c.Treatment))
    };
    
    return ServiceResult<ConsultationStatisticsDto>.Success(statistics);
}
```

### 主服务委托层
```csharp
public class ConsultationService : IConsultationService
{
    private readonly ConsultationQueryService _queryService;
    private readonly ConsultationBusinessService _businessService;
    
    public ConsultationService(
        ConsultationQueryService queryService,
        ConsultationBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }
    
    // 纯委托实现 - 查询功能
    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
        
    public async Task<ServiceResult<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        => await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);
        
    public async Task<ServiceResult<PagedResult<ConsultationSearchResultDto>>> SearchConsultationsAsync(
        ConsultationSearchDto criteria)
        => await _queryService.SearchConsultationsAsync(criteria);
    
    // 纯委托实现 - 业务功能
    public async Task<ServiceResult<ConsultationDto>> CreateConsultationAsync(ConsultationCreateDto dto)
        => await _businessService.CreateConsultationAsync(dto);
        
    public async Task<ServiceResult<ConsultationDto>> UpdateFourExaminationsAsync(
        Guid id, FourExaminationsDto dto)
        => await _businessService.UpdateFourExaminationsAsync(id, dto);
        
    public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id)
        => await _businessService.CompleteConsultationAsync(id);
}
```

## 🔧 开发标准

### 代码质量要求
- **零编译警告**: 严格遵循.NET 8最佳实践
- **异步优先**: 所有数据库操作使用async/await
- **LINQ安全**: 杜绝原生SQL，防止注入攻击
- **异常处理**: 完整的try-catch和错误日志记录

### UltraThink架构标准
```csharp
// 1. Repository层 - 数据访问
public interface IConsultationRepository : IBaseRepository<ConsultationModel>
{
    Task<ConsultationModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    Task<List<ConsultationModel>> GetPatientConsultationHistoryAsync(Guid patientId);
    Task<List<ConsultationModel>> GetConsultationsByDateRangeAsync(DateTime start, DateTime end);
}

// 2. QueryService层 - 查询专业化
public class ConsultationQueryService
{
    // 专注复杂查询、统计、报表
}

// 3. BusinessService层 - 业务逻辑
public class ConsultationBusinessService  
{
    // 专注业务流程、CRUD、验证
}

// 4. Service层 - 纯委托
public class ConsultationService : IConsultationService
{
    // 纯委托，无业务逻辑
}
```

### 数据传输对象 (DTOs)
```csharp
// 创建诊断DTO
public class ConsultationCreateDto
{
    [Required]
    public Guid MedicalCaseId { get; set; }
    public string? Observation { get; set; }
    public string? Listening { get; set; }
    public string? Inquiry { get; set; }
    public string? Palpation { get; set; }
}

// 四诊更新DTO
public class FourExaminationsDto
{
    public string? Observation { get; set; }  // 望诊
    public string? Listening { get; set; }    // 闻诊  
    public string? Inquiry { get; set; }      // 问诊
    public string? Palpation { get; set; }    // 切诊
}

// 辨证论治DTO
public class DiagnosisDto
{
    [Required]
    public string Diagnosis { get; set; }     // 中医诊断
    public string? Syndrome { get; set; }     // 证候分析
    public string? Treatment { get; set; }    // 治法治则
    public string? Principle { get; set; }    // 治疗原则
}
```

## 🔗 集成接口

### API控制器实现
```csharp
[ApiController]
[ApiVersion("1")]  
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ConsultationController : BaseApiController
{
    private readonly IConsultationService _consultationService;
    
    public ConsultationController(
        IConsultationService consultationService,
        ILogger<ConsultationController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _consultationService = consultationService;
    }
    
    /// <summary>
    /// 根据医案ID获取诊断记录
    /// </summary>
    [HttpGet("by-medical-case/{medicalCaseId:guid}")]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> GetByMedicalCaseId(Guid medicalCaseId)
    {
        try
        {
            var validation = ValidateGuid<ConsultationDto>(medicalCaseId, "医案ID");
            if (validation != null) return validation;
            
            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
            return HandleServiceResult(result, "获取诊断记录成功");
        }
        catch (Exception ex)
        {
            return HandleException<ConsultationDto>(ex, "获取诊断记录", medicalCaseId);
        }
    }
    
    /// <summary>
    /// 创建诊断记录
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation(
        [FromBody] ConsultationCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<ConsultationDto>(dto);
            if (validation != null) return validation;
            
            var result = await _consultationService.CreateConsultationAsync(dto);
            return HandleServiceResult(result, "创建诊断记录成功", true);
        }
        catch (Exception ex)
        {
            return HandleException<ConsultationDto>(ex, "创建诊断记录", dto.MedicalCaseId);
        }
    }
    
    /// <summary>
    /// 更新四诊记录
    /// </summary>
    [HttpPut("{id:guid}/four-examinations")]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateFourExaminations(
        Guid id, [FromBody] FourExaminationsDto dto)
    {
        try
        {
            var validation = ValidateGuid<ConsultationDto>(id, "诊断记录ID");
            if (validation != null) return validation;
            
            var result = await _consultationService.UpdateFourExaminationsAsync(id, dto);
            return HandleServiceResult(result, "更新四诊记录成功");
        }
        catch (Exception ex)
        {
            return HandleException<ConsultationDto>(ex, "更新四诊记录", id);
        }
    }
    
    /// <summary>
    /// 完成诊断
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteConsultation(Guid id)
    {
        try
        {
            var validation = ValidateGuid<bool>(id, "诊断记录ID");
            if (validation != null) return validation;
            
            var result = await _consultationService.CompleteConsultationAsync(id);
            return HandleServiceResult(result, "诊断完成");
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex, "完成诊断", id);
        }
    }
}
```

### 与其他模块集成

#### 1. MedicalCase集成
```csharp
// MedicalCase模块调用
public async Task<ServiceResult<bool>> CompleteMedicalCaseAsync(Guid id)
{
    // 检查诊断记录完成状态
    var consultationResult = await _consultationService.GetByMedicalCaseIdAsync(id);
    if (consultationResult.Success && 
        consultationResult.Data?.Status != DiagnosisStatus.Completed)
    {
        return ServiceResult<bool>.Failure("请先完成诊断记录");
    }
    
    // 继续医案完成流程...
}
```

#### 2. Prescriptions集成  
```csharp
// Prescriptions模块调用
public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto dto)
{
    // 验证诊断记录存在
    var consultationResult = await _consultationService.GetByMedicalCaseIdAsync(dto.MedicalCaseId);
    if (!consultationResult.Success)
        return ServiceResult<PrescriptionDto>.Failure("该医案还没有诊断记录");
        
    // 继续处方创建流程...
}
```

## ⚙️ 配置管理

### 诊断配置选项
```csharp
public class ConsultationOptions
{
    public const string SectionName = "Consultation";
    
    /// <summary>
    /// 四诊记录最大长度
    /// </summary>
    public int MaxExaminationLength { get; set; } = 1000;
    
    /// <summary>
    /// 诊断记录最大长度
    /// </summary>
    public int MaxDiagnosisLength { get; set; } = 500;
    
    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// 诊断完整性检查级别
    /// </summary>
    public DiagnosisValidationLevel ValidationLevel { get; set; } = DiagnosisValidationLevel.Standard;
}

public enum DiagnosisValidationLevel
{
    Basic = 0,     // 基础检查
    Standard = 1,  // 标准检查  
    Strict = 2     // 严格检查
}
```

### 应用配置
```json
{
  "Consultation": {
    "MaxExaminationLength": 1000,
    "MaxDiagnosisLength": 500,
    "AutoSaveIntervalSeconds": 30,
    "ValidationLevel": "Standard"
  },
  "Logging": {
    "LogLevel": {
      "LYBT.Module.Consultation": "Information"
    }
  }
}
```

## 🧪 测试规范

### 单元测试要求

#### 1. 业务服务测试
```csharp
[Test]
public async Task CreateConsultationAsync_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var dto = new ConsultationCreateDto 
    { 
        MedicalCaseId = Guid.NewGuid(),
        Observation = "面色潮红，精神倦怠"
    };
    
    _medicalCaseRepositoryMock
        .Setup(x => x.GetByIdAsync(dto.MedicalCaseId))
        .ReturnsAsync(new MedicalCaseModel { Status = MedicalCaseStatus.InProgress });
        
    _consultationRepositoryMock
        .Setup(x => x.GetByMedicalCaseIdAsync(dto.MedicalCaseId))
        .ReturnsAsync((ConsultationModel?)null);
    
    // Act
    var result = await _businessService.CreateConsultationAsync(dto);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.MedicalCaseId, Is.EqualTo(dto.MedicalCaseId));
}

[Test]
public async Task CompleteConsultationAsync_IncompleteDiagnosis_ReturnsFailure()
{
    // Arrange
    var consultationId = Guid.NewGuid();
    var consultation = new ConsultationModel
    {
        Id = consultationId,
        Status = DiagnosisStatus.InProgress,
        Diagnosis = null // 诊断未完成
    };
    
    _consultationRepositoryMock
        .Setup(x => x.GetByIdAsync(consultationId))
        .ReturnsAsync(consultation);
    
    // Act
    var result = await _businessService.CompleteConsultationAsync(consultationId);
    
    // Assert
    Assert.That(result.Success, Is.False);
    Assert.That(result.ErrorMessage, Contains.Substring("必须填写中医诊断"));
}
```

#### 2. 查询服务测试
```csharp
[Test]
public async Task SearchConsultationsAsync_WithSymptomKeyword_ReturnsFilteredResults()
{
    // Arrange
    var criteria = new ConsultationSearchDto
    {
        SymptomKeyword = "头痛",
        Page = 1,
        PageSize = 10
    };
    
    var consultations = new List<ConsultationModel>
    {
        new() { Inquiry = "主诉头痛3天" },
        new() { Observation = "头痛面红" }
    };
    
    _consultationRepositoryMock
        .Setup(x => x.GetPagedAsync(It.IsAny<IQueryable<ConsultationModel>>(), 1, 10))
        .ReturnsAsync(new PagedResult<ConsultationSearchResultDto>(consultations.Count, []));
    
    // Act
    var result = await _queryService.SearchConsultationsAsync(criteria);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.TotalCount, Is.EqualTo(2));
}
```

### 集成测试
```csharp
[Test]
public async Task ConsultationWorkflow_CompleteFlow_Success()
{
    // 1. 创建诊断记录
    var createDto = new ConsultationCreateDto { MedicalCaseId = _testMedicalCaseId };
    var createResult = await _consultationService.CreateConsultationAsync(createDto);
    Assert.That(createResult.Success, Is.True);
    
    // 2. 更新四诊
    var fourExamDto = new FourExaminationsDto
    {
        Observation = "面色萎黄",
        Inquiry = "纳差乏力", 
        Palpation = "脉象沉细"
    };
    var updateResult = await _consultationService.UpdateFourExaminationsAsync(
        createResult.Data.Id, fourExamDto);
    Assert.That(updateResult.Success, Is.True);
    
    // 3. 完成诊断
    var completeResult = await _consultationService.CompleteConsultationAsync(createResult.Data.Id);
    Assert.That(completeResult.Success, Is.True);
}
```

## 🚀 部署说明

### 数据库迁移
```bash
# Consultation模块相关迁移
dotnet ef migrations add AddConsultationModule --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### 配置检查清单
- [ ] ConsultationOptions配置正确
- [ ] 数据库连接字符串有效
- [ ] MedicalCase模块依赖可用
- [ ] AutoMapper映射配置完整
- [ ] 日志记录级别适当
- [ ] 缓存策略配置合理

## 📚 相关文档

### 架构文档
- [UltraThink双层架构标准](../../architecture/ultrathink-dual-layer-architecture.md)
- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)  
- [API响应标准](../../architecture/ultrathink-api-response-standards-20250817.md)

### 业务文档
- [MedicalCase模块文档](./medicalcase.md) - 1:1关联关系
- [Prescriptions模块文档](./prescriptions.md) - 诊断后处方流程
- [Patients模块文档](./patients.md) - 患者诊断历史

### 开发指南
- [模块开发规范](../../development/MODULE_DEVELOPMENT_STANDARDS.md)
- [测试指南](../../testing/MODULE_TESTING_GUIDE.md)
- [部署指南](../../deployment/MODULE_DEPLOYMENT_GUIDE.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 后端开发组