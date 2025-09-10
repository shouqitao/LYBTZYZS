# Prescriptions Module (处方管理模块)

## 📋 项目概述

### 项目定位
**Prescriptions 模块**是凌隐宝堂中医诊所系统的**智能处方管理模块**，负责处方开具、药材配伍、价格计算和处方输出的完整流程。作为诊疗流程的关键环节，连接诊断记录与药材管理，实现中医处方的智能化和规范化管理。

### 核心价值
- 💊 **智能配伍检查**: 中药配伍禁忌智能识别和预警
- 📋 **验方组合应用**: 与Formula模块集成，经典验方快速应用
- 💰 **精准价格计算**: 自动计算处方总价，支持多种计价模式
- 🖨️ **标准输出格式**: 符合中医处方规范的标准化输出
- 📊 **处方统计分析**: 用药频次、成本分析、疗效跟踪
- 🔗 **协作API完整**: 支持患者处方历史、医案处方记录等场景

### 业务定位 (v1.0)
```
Consultation (诊断记录)
    ↓ 1:N 关系
Prescriptions (处方记录) ← 本模块
    ↓ 引用关系
Formula (验方模板) + Herbs (中药材)
    ↓ 输出
标准处方单 (打印/复制)
```

## 🏗️ 技术架构

### UltraThink双层架构实现
```
PrescriptionService (主服务 - 纯委托层)
├── PrescriptionQueryService (查询专业层)
│   ├── 处方记录检索 (按患者、医案、时间)
│   ├── 用药统计分析 (频次、成本、疗效)
│   ├── 处方历史查询 (患者用药史、复诊对比)
│   └── 药材使用报表 (库存需求、采购建议)
└── PrescriptionBusinessService (业务逻辑层)
    ├── 处方开具管理 (创建、编辑、审核)
    ├── 智能配伍检查 (禁忌识别、用量验证)
    ├── 验方应用处理 (Formula集成、个性化调整)
    ├── 价格计算服务 (成本核算、收费标准)
    └── 处方输出生成 (标准格式、打印预览)
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
builder.Services.AddPrescriptionModule();

public static class PrescriptionModuleExtensions
{
    public static IServiceCollection AddPrescriptionModule(this IServiceCollection services)
    {
        // Repository Layer
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
        
        // Service Layer - UltraThink双层架构
        services.AddScoped<PrescriptionQueryService>();
        services.AddScoped<PrescriptionBusinessService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>(); // 纯委托
        
        // 专业服务
        services.AddScoped<ICompatibilityCheckService, CompatibilityCheckService>();
        services.AddScoped<IPrescriptionPriceService, PrescriptionPriceService>();
        services.AddScoped<IPrescriptionOutputService, PrescriptionOutputService>();
        
        return services;
    }
}
```

### 核心实体模型
```csharp
public class PrescriptionModel : BaseEntity
{
    [Required]
    public Guid MedicalCaseId { get; set; }     // 关联医案
    public Guid? ConsultationId { get; set; }   // 关联诊断(可选)
    public Guid? FormulaId { get; set; }        // 引用验方(可选)
    
    // 处方基本信息
    [Required]
    [MaxLength(50)]
    public string PrescriptionNumber { get; set; } // 处方编号
    
    public DateTime PrescriptionDate { get; set; } // 开方日期
    public PrescriptionType Type { get; set; }     // 处方类型
    
    // 处方内容
    public string? Instructions { get; set; }      // 用法用量总说明
    public string? Notes { get; set; }            // 处方备注
    public decimal TotalAmount { get; set; }      // 处方总金额
    public int Days { get; set; } = 7;           // 用药天数
    
    // 处方状态
    public PrescriptionStatus Status { get; set; } // 处方状态
    public Guid? ApprovedBy { get; set; }         // 审核医生
    public DateTime? ApprovedAt { get; set; }     // 审核时间
    
    // 导航属性
    public virtual MedicalCaseModel MedicalCase { get; set; }
    public virtual ConsultationModel? Consultation { get; set; }
    public virtual FormulaModel? Formula { get; set; }
    public virtual ICollection<PrescriptionItemModel> Items { get; set; } = [];
}

public class PrescriptionItemModel : BaseEntity
{
    [Required]
    public Guid PrescriptionId { get; set; }    // 处方ID
    [Required] 
    public Guid HerbId { get; set; }            // 药材ID
    
    // 用药信息
    public decimal Dosage { get; set; }         // 单次用量(克)
    public string? Usage { get; set; }          // 特殊用法
    public string? Notes { get; set; }          // 药材备注
    public decimal UnitPrice { get; set; }     // 单价
    public decimal SubTotal { get; set; }      // 小计
    
    // 导航属性
    public virtual PrescriptionModel Prescription { get; set; }
    public virtual HerbModel Herb { get; set; }
}

public enum PrescriptionType
{
    Decoction = 0,    // 汤剂
    Powder = 1,       // 散剂  
    Pill = 2,         // 丸剂
    Other = 99        // 其他
}

public enum PrescriptionStatus
{
    Draft = 0,        // 草稿
    Submitted = 1,    // 已提交
    Approved = 2,     // 已审核
    Dispensed = 3,    // 已调剂
    Completed = 4     // 已完成
}
```

## 🎯 功能规范

### 核心业务功能

#### 1. 处方开具管理
```csharp
// 业务服务实现
public class PrescriptionBusinessService
{
    // 创建处方
    public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(
        PrescriptionCreateDto dto)
    {
        // 验证医案状态
        var medicalCase = await _medicalCaseRepository.GetByIdAsync(dto.MedicalCaseId);
        if (medicalCase?.Status != MedicalCaseStatus.InProgress)
            return ServiceResult<PrescriptionDto>.Failure("只能为进行中的医案开具处方");
        
        // 验证诊断记录(如果提供)
        if (dto.ConsultationId.HasValue)
        {
            var consultation = await _consultationRepository.GetByIdAsync(dto.ConsultationId.Value);
            if (consultation?.Status != DiagnosisStatus.Completed)
                return ServiceResult<PrescriptionDto>.Failure("诊断记录未完成，无法开具处方");
        }
        
        var prescription = _mapper.Map<PrescriptionModel>(dto);
        prescription.PrescriptionNumber = await GeneratePrescriptionNumberAsync();
        prescription.PrescriptionDate = DateTime.Now;
        prescription.Status = PrescriptionStatus.Draft;
        
        await _repository.CreateAsync(prescription);
        var result = _mapper.Map<PrescriptionDto>(prescription);
        
        return ServiceResult<PrescriptionDto>.Success(result);
    }
    
    // 添加处方药材
    public async Task<ServiceResult<PrescriptionDto>> AddPrescriptionItemAsync(
        Guid prescriptionId, PrescriptionItemCreateDto dto)
    {
        var prescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
        if (prescription == null)
            return ServiceResult<PrescriptionDto>.Failure("处方不存在");
            
        if (prescription.Status != PrescriptionStatus.Draft)
            return ServiceResult<PrescriptionDto>.Failure("只能修改草稿状态的处方");
        
        // 验证药材存在
        var herb = await _herbRepository.GetByIdAsync(dto.HerbId);
        if (herb == null)
            return ServiceResult<PrescriptionDto>.Failure("药材不存在");
        
        // 检查重复药材
        if (prescription.Items.Any(i => i.HerbId == dto.HerbId))
            return ServiceResult<PrescriptionDto>.Failure("处方中已包含该药材");
        
        var item = new PrescriptionItemModel
        {
            PrescriptionId = prescriptionId,
            HerbId = dto.HerbId,
            Dosage = dto.Dosage,
            Usage = dto.Usage,
            Notes = dto.Notes,
            UnitPrice = herb.Price,
            SubTotal = dto.Dosage * herb.Price * prescription.Days
        };
        
        await _prescriptionItemRepository.CreateAsync(item);
        
        // 重新计算处方总价
        await RecalculatePrescriptionTotalAsync(prescriptionId);
        
        var result = await _repository.GetByIdWithItemsAsync(prescriptionId);
        return ServiceResult<PrescriptionDto>.Success(_mapper.Map<PrescriptionDto>(result));
    }
}
```

#### 2. 智能配伍检查
```csharp
// 配伍检查专业服务
public class CompatibilityCheckService : ICompatibilityCheckService
{
    // 检查处方配伍安全性
    public async Task<ServiceResult<CompatibilityCheckResult>> CheckPrescriptionCompatibilityAsync(
        Guid prescriptionId)
    {
        var prescription = await _prescriptionRepository.GetByIdWithItemsAsync(prescriptionId);
        if (prescription == null)
            return ServiceResult<CompatibilityCheckResult>.Failure("处方不存在");
        
        var herbs = prescription.Items.Select(i => i.Herb).ToList();
        var result = new CompatibilityCheckResult
        {
            PrescriptionId = prescriptionId,
            IsCompatible = true,
            Warnings = [],
            Incompatibilities = []
        };
        
        // 检查十八反
        var incompatiblePairs = await CheckEighteenIncompatibilitiesAsync(herbs);
        if (incompatiblePairs.Any())
        {
            result.IsCompatible = false;
            result.Incompatibilities.AddRange(incompatiblePairs);
        }
        
        // 检查十九畏
        var fearPairs = await CheckNineteenFearsAsync(herbs);
        if (fearPairs.Any())
        {
            result.Warnings.AddRange(fearPairs.Select(p => 
                new CompatibilityWarning
                {
                    Level = WarningLevel.High,
                    Message = $"{p.Herb1.Name}与{p.Herb2.Name}相畏，需要谨慎使用",
                    Herbs = [p.Herb1, p.Herb2]
                }));
        }
        
        // 检查用量安全性
        var dosageWarnings = CheckDosageSafety(prescription.Items);
        result.Warnings.AddRange(dosageWarnings);
        
        return ServiceResult<CompatibilityCheckResult>.Success(result);
    }
    
    private async Task<List<IncompatibilityPair>> CheckEighteenIncompatibilitiesAsync(
        List<HerbModel> herbs)
    {
        var incompatiblePairs = new List<IncompatibilityPair>();
        
        // 十八反配伍检查逻辑
        var incompatibilityRules = await _compatibilityRepository.GetIncompatibilityRulesAsync();
        
        foreach (var herb1 in herbs)
        {
            foreach (var herb2 in herbs)
            {
                if (herb1.Id == herb2.Id) continue;
                
                var rule = incompatibilityRules.FirstOrDefault(r => 
                    (r.Herb1Id == herb1.Id && r.Herb2Id == herb2.Id) ||
                    (r.Herb1Id == herb2.Id && r.Herb2Id == herb1.Id));
                    
                if (rule != null)
                {
                    incompatiblePairs.Add(new IncompatibilityPair
                    {
                        Herb1 = herb1,
                        Herb2 = herb2,
                        Severity = rule.Severity,
                        Description = rule.Description
                    });
                }
            }
        }
        
        return incompatiblePairs;
    }
}
```

#### 3. 验方应用处理
```csharp
// 验方集成业务逻辑
public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionFromFormulaAsync(
    PrescriptionFromFormulaDto dto)
{
    // 验证验方存在
    var formula = await _formulaRepository.GetByIdWithItemsAsync(dto.FormulaId);
    if (formula == null)
        return ServiceResult<PrescriptionDto>.Failure("验方不存在");
    
    // 创建基础处方
    var prescription = new PrescriptionModel
    {
        MedicalCaseId = dto.MedicalCaseId,
        ConsultationId = dto.ConsultationId,
        FormulaId = dto.FormulaId,
        PrescriptionNumber = await GeneratePrescriptionNumberAsync(),
        Type = formula.Type,
        Instructions = formula.Instructions,
        Notes = $"基于验方【{formula.Name}】开具",
        Days = dto.Days,
        Status = PrescriptionStatus.Draft
    };
    
    await _repository.CreateAsync(prescription);
    
    // 复制验方药材
    decimal totalAmount = 0;
    foreach (var formulaItem in formula.Items)
    {
        // 获取当前药材价格
        var herb = await _herbRepository.GetByIdAsync(formulaItem.HerbId);
        var adjustedDosage = formulaItem.Dosage * dto.DosageMultiplier;
        var subTotal = adjustedDosage * herb.Price * dto.Days;
        
        var prescriptionItem = new PrescriptionItemModel
        {
            PrescriptionId = prescription.Id,
            HerbId = formulaItem.HerbId,
            Dosage = adjustedDosage,
            Usage = formulaItem.Usage,
            Notes = formulaItem.Notes,
            UnitPrice = herb.Price,
            SubTotal = subTotal
        };
        
        await _prescriptionItemRepository.CreateAsync(prescriptionItem);
        totalAmount += subTotal;
    }
    
    // 更新处方总价
    prescription.TotalAmount = totalAmount;
    await _repository.UpdateAsync(prescription);
    
    var result = await _repository.GetByIdWithItemsAsync(prescription.Id);
    return ServiceResult<PrescriptionDto>.Success(_mapper.Map<PrescriptionDto>(result));
}
```

#### 4. 价格计算服务
```csharp
public class PrescriptionPriceService : IPrescriptionPriceService
{
    // 重新计算处方总价
    public async Task<ServiceResult<decimal>> RecalculatePrescriptionTotalAsync(Guid prescriptionId)
    {
        var prescription = await _prescriptionRepository.GetByIdWithItemsAsync(prescriptionId);
        if (prescription == null)
            return ServiceResult<decimal>.Failure("处方不存在");
        
        decimal totalAmount = 0;
        
        foreach (var item in prescription.Items)
        {
            // 获取最新药材价格
            var herb = await _herbRepository.GetByIdAsync(item.HerbId);
            if (herb != null)
            {
                item.UnitPrice = herb.Price;
                item.SubTotal = item.Dosage * herb.Price * prescription.Days;
                await _prescriptionItemRepository.UpdateAsync(item);
            }
            
            totalAmount += item.SubTotal;
        }
        
        prescription.TotalAmount = totalAmount;
        await _prescriptionRepository.UpdateAsync(prescription);
        
        return ServiceResult<decimal>.Success(totalAmount);
    }
    
    // 获取处方成本分析
    public async Task<ServiceResult<PrescriptionCostAnalysisDto>> GetCostAnalysisAsync(
        Guid prescriptionId)
    {
        var prescription = await _prescriptionRepository.GetByIdWithItemsAsync(prescriptionId);
        if (prescription == null)
            return ServiceResult<PrescriptionCostAnalysisDto>.Failure("处方不存在");
        
        var analysis = new PrescriptionCostAnalysisDto
        {
            PrescriptionId = prescriptionId,
            TotalAmount = prescription.TotalAmount,
            DailyAmount = prescription.TotalAmount / prescription.Days,
            Items = prescription.Items.Select(i => new PrescriptionItemCostDto
            {
                HerbName = i.Herb.Name,
                Dosage = i.Dosage,
                UnitPrice = i.UnitPrice,
                Days = prescription.Days,
                SubTotal = i.SubTotal,
                Percentage = i.SubTotal / prescription.TotalAmount * 100
            }).OrderByDescending(i => i.SubTotal).ToList()
        };
        
        return ServiceResult<PrescriptionCostAnalysisDto>.Success(analysis);
    }
}
```

### 查询服务专业功能

#### 1. 处方记录检索
```csharp
public class PrescriptionQueryService
{
    // 按患者获取处方历史
    public async Task<ServiceResult<List<PrescriptionHistoryDto>>> GetPatientPrescriptionHistoryAsync(
        Guid patientId, int limit = 10)
    {
        var prescriptions = await _repository.GetPatientPrescriptionHistoryAsync(patientId, limit);
        var results = _mapper.Map<List<PrescriptionHistoryDto>>(prescriptions);
        
        return ServiceResult<List<PrescriptionHistoryDto>>.Success(results);
    }
    
    // 处方高级搜索
    public async Task<ServiceResult<PagedResult<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
        PrescriptionSearchDto criteria)
    {
        var query = _repository.GetQueryable()
            .Include(p => p.Items)
            .ThenInclude(i => i.Herb)
            .Include(p => p.MedicalCase)
            .ThenInclude(mc => mc.Patient);
        
        // 时间范围过滤
        if (criteria.StartDate.HasValue)
            query = query.Where(p => p.PrescriptionDate >= criteria.StartDate.Value);
        if (criteria.EndDate.HasValue)
            query = query.Where(p => p.PrescriptionDate <= criteria.EndDate.Value);
            
        // 状态过滤
        if (criteria.Status.HasValue)
            query = query.Where(p => p.Status == criteria.Status.Value);
            
        // 药材关键词搜索
        if (!string.IsNullOrEmpty(criteria.HerbKeyword))
        {
            query = query.Where(p => p.Items.Any(i => 
                i.Herb.Name.Contains(criteria.HerbKeyword)));
        }
        
        // 患者信息搜索
        if (!string.IsNullOrEmpty(criteria.PatientKeyword))
        {
            query = query.Where(p => p.MedicalCase.Patient.Name.Contains(criteria.PatientKeyword));
        }
        
        var pagedResult = await _repository.GetPagedAsync(query, criteria.Page, criteria.PageSize);
        return ServiceResult<PagedResult<PrescriptionSearchResultDto>>.Success(pagedResult);
    }
    
    // 用药统计分析
    public async Task<ServiceResult<HerbUsageStatisticsDto>> GetHerbUsageStatisticsAsync(
        DateTime startDate, DateTime endDate)
    {
        var prescriptions = await _repository.GetPrescriptionsByDateRangeAsync(startDate, endDate);
        
        var herbUsages = prescriptions
            .SelectMany(p => p.Items)
            .GroupBy(i => new { i.HerbId, i.Herb.Name })
            .Select(g => new HerbUsageDto
            {
                HerbId = g.Key.HerbId,
                HerbName = g.Key.Name,
                UsageCount = g.Count(),
                TotalDosage = g.Sum(i => i.Dosage * i.Prescription.Days),
                TotalAmount = g.Sum(i => i.SubTotal),
                AverageDosage = g.Average(i => i.Dosage)
            })
            .OrderByDescending(h => h.UsageCount)
            .ToList();
        
        var statistics = new HerbUsageStatisticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalPrescriptions = prescriptions.Count,
            TotalHerbTypes = herbUsages.Count,
            HerbUsages = herbUsages
        };
        
        return ServiceResult<HerbUsageStatisticsDto>.Success(statistics);
    }
}
```

### 主服务委托层
```csharp
public class PrescriptionService : IPrescriptionService
{
    private readonly PrescriptionQueryService _queryService;
    private readonly PrescriptionBusinessService _businessService;
    
    public PrescriptionService(
        PrescriptionQueryService queryService,
        PrescriptionBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }
    
    // 纯委托实现 - 查询功能
    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
        
    public async Task<ServiceResult<List<PrescriptionHistoryDto>>> GetPatientPrescriptionHistoryAsync(
        Guid patientId, int limit = 10)
        => await _queryService.GetPatientPrescriptionHistoryAsync(patientId, limit);
        
    public async Task<ServiceResult<PagedResult<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
        PrescriptionSearchDto criteria)
        => await _queryService.SearchPrescriptionsAsync(criteria);
    
    // 纯委托实现 - 业务功能
    public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto dto)
        => await _businessService.CreatePrescriptionAsync(dto);
        
    public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionFromFormulaAsync(
        PrescriptionFromFormulaDto dto)
        => await _businessService.CreatePrescriptionFromFormulaAsync(dto);
        
    public async Task<ServiceResult<bool>> ApprovePrescriptionAsync(Guid id, Guid approvedBy)
        => await _businessService.ApprovePrescriptionAsync(id, approvedBy);
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
public interface IPrescriptionRepository : IBaseRepository<PrescriptionModel>
{
    Task<PrescriptionModel?> GetByIdWithItemsAsync(Guid id);
    Task<List<PrescriptionModel>> GetPatientPrescriptionHistoryAsync(Guid patientId, int limit);
    Task<List<PrescriptionModel>> GetPrescriptionsByDateRangeAsync(DateTime start, DateTime end);
}

// 2. QueryService层 - 查询专业化
public class PrescriptionQueryService
{
    // 专注复杂查询、统计、报表
}

// 3. BusinessService层 - 业务逻辑
public class PrescriptionBusinessService  
{
    // 专注业务流程、CRUD、验证
}

// 4. Service层 - 纯委托
public class PrescriptionService : IPrescriptionService
{
    // 纯委托，无业务逻辑
}
```

### 数据传输对象 (DTOs)
```csharp
// 创建处方DTO
public class PrescriptionCreateDto
{
    [Required]
    public Guid MedicalCaseId { get; set; }
    public Guid? ConsultationId { get; set; }
    public PrescriptionType Type { get; set; } = PrescriptionType.Decoction;
    public string? Instructions { get; set; }
    public string? Notes { get; set; }
    public int Days { get; set; } = 7;
}

// 从验方创建处方DTO
public class PrescriptionFromFormulaDto
{
    [Required]
    public Guid MedicalCaseId { get; set; }
    public Guid? ConsultationId { get; set; }
    [Required]
    public Guid FormulaId { get; set; }
    public int Days { get; set; } = 7;
    public decimal DosageMultiplier { get; set; } = 1.0m; // 用量倍数调整
}

// 处方药材项创建DTO
public class PrescriptionItemCreateDto
{
    [Required]
    public Guid HerbId { get; set; }
    [Required]
    [Range(0.1, 200)]
    public decimal Dosage { get; set; }    // 单次用量(克)
    public string? Usage { get; set; }     // 特殊用法
    public string? Notes { get; set; }     // 药材备注
}
```

## 🔗 集成接口

### API控制器实现
```csharp
[ApiController]
[ApiVersion("1")]  
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PrescriptionsController : BaseApiController
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly ICompatibilityCheckService _compatibilityService;
    private readonly IPrescriptionPriceService _priceService;
    
    public PrescriptionsController(
        IPrescriptionService prescriptionService,
        ICompatibilityCheckService compatibilityService,
        IPrescriptionPriceService priceService,
        ILogger<PrescriptionsController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _prescriptionService = prescriptionService;
        _compatibilityService = compatibilityService;
        _priceService = priceService;
    }
    
    /// <summary>
    /// 创建处方
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescription(
        [FromBody] PrescriptionCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<PrescriptionDto>(dto);
            if (validation != null) return validation;
            
            var result = await _prescriptionService.CreatePrescriptionAsync(dto);
            return HandleServiceResult(result, "创建处方成功", true);
        }
        catch (Exception ex)
        {
            return HandleException<PrescriptionDto>(ex, "创建处方", dto.MedicalCaseId);
        }
    }
    
    /// <summary>
    /// 从验方创建处方
    /// </summary>
    [HttpPost("from-formula")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescriptionFromFormula(
        [FromBody] PrescriptionFromFormulaDto dto)
    {
        try
        {
            var validation = ValidateModel<PrescriptionDto>(dto);
            if (validation != null) return validation;
            
            var result = await _prescriptionService.CreatePrescriptionFromFormulaAsync(dto);
            return HandleServiceResult(result, "基于验方创建处方成功", true);
        }
        catch (Exception ex)
        {
            return HandleException<PrescriptionDto>(ex, "从验方创建处方", dto.FormulaId);
        }
    }
    
    /// <summary>
    /// 添加处方药材
    /// </summary>
    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> AddPrescriptionItem(
        Guid id, [FromBody] PrescriptionItemCreateDto dto)
    {
        try
        {
            var validation = ValidateGuid<PrescriptionDto>(id, "处方ID");
            if (validation != null) return validation;
            
            var result = await _prescriptionService.AddPrescriptionItemAsync(id, dto);
            return HandleServiceResult(result, "添加药材成功");
        }
        catch (Exception ex)
        {
            return HandleException<PrescriptionDto>(ex, "添加处方药材", id);
        }
    }
    
    /// <summary>
    /// 检查处方配伍安全性
    /// </summary>
    [HttpGet("{id:guid}/compatibility-check")]
    public async Task<ActionResult<ApiResponse<CompatibilityCheckResult>>> CheckCompatibility(Guid id)
    {
        try
        {
            var validation = ValidateGuid<CompatibilityCheckResult>(id, "处方ID");
            if (validation != null) return validation;
            
            var result = await _compatibilityService.CheckPrescriptionCompatibilityAsync(id);
            return HandleServiceResult(result, "配伍检查完成");
        }
        catch (Exception ex)
        {
            return HandleException<CompatibilityCheckResult>(ex, "检查处方配伍", id);
        }
    }
    
    /// <summary>
    /// 获取处方成本分析
    /// </summary>
    [HttpGet("{id:guid}/cost-analysis")]
    public async Task<ActionResult<ApiResponse<PrescriptionCostAnalysisDto>>> GetCostAnalysis(Guid id)
    {
        try
        {
            var validation = ValidateGuid<PrescriptionCostAnalysisDto>(id, "处方ID");
            if (validation != null) return validation;
            
            var result = await _priceService.GetCostAnalysisAsync(id);
            return HandleServiceResult(result, "成本分析获取成功");
        }
        catch (Exception ex)
        {
            return HandleException<PrescriptionCostAnalysisDto>(ex, "获取成本分析", id);
        }
    }
    
    /// <summary>
    /// 获取患者处方历史
    /// </summary>
    [HttpGet("patient/{patientId:guid}/history")]
    public async Task<ActionResult<ApiResponse<List<PrescriptionHistoryDto>>>> GetPatientHistory(
        Guid patientId, [FromQuery] int limit = 10)
    {
        try
        {
            var validation = ValidateGuid<List<PrescriptionHistoryDto>>(patientId, "患者ID");
            if (validation != null) return validation;
            
            var result = await _prescriptionService.GetPatientPrescriptionHistoryAsync(patientId, limit);
            return HandleServiceResult(result, "获取处方历史成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<PrescriptionHistoryDto>>(ex, "获取患者处方历史", patientId);
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
    // 检查是否有未完成的处方
    var prescriptions = await _prescriptionService.GetByMedicalCaseIdAsync(id);
    if (prescriptions.Data?.Any(p => p.Status == PrescriptionStatus.Draft) == true)
    {
        return ServiceResult<bool>.Failure("存在草稿状态的处方，请先完成处方审核");
    }
    
    // 继续医案完成流程...
}
```

#### 2. Formula集成  
```csharp
// Formula模块提供验方数据
public async Task<ServiceResult<List<FormulaDto>>> GetAvailableFormulasForPrescriptionAsync()
{
    var formulas = await _formulaRepository.GetActiveFormulasAsync();
    return ServiceResult<List<FormulaDto>>.Success(_mapper.Map<List<FormulaDto>>(formulas));
}
```

#### 3. Herbs集成
```csharp
// Herbs模块提供药材价格和库存信息
public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
{
    var herbs = await _herbRepository.GetAvailableHerbsAsync();
    return ServiceResult<List<HerbDto>>.Success(_mapper.Map<List<HerbDto>>(herbs));
}
```

## ⚙️ 配置管理

### 处方配置选项
```csharp
public class PrescriptionOptions
{
    public const string SectionName = "Prescription";
    
    /// <summary>
    /// 处方编号前缀
    /// </summary>
    public string NumberPrefix { get; set; } = "RX";
    
    /// <summary>
    /// 处方有效期(天)
    /// </summary>
    public int ValidityDays { get; set; } = 30;
    
    /// <summary>
    /// 最大用药天数
    /// </summary>
    public int MaxDays { get; set; } = 30;
    
    /// <summary>
    /// 单味药最大用量(克)
    /// </summary>
    public decimal MaxSingleHerbDosage { get; set; } = 100;
    
    /// <summary>
    /// 启用配伍检查
    /// </summary>
    public bool EnableCompatibilityCheck { get; set; } = true;
    
    /// <summary>
    /// 自动价格计算
    /// </summary>
    public bool AutoCalculatePrice { get; set; } = true;
}
```

### 应用配置
```json
{
  "Prescription": {
    "NumberPrefix": "RX",
    "ValidityDays": 30,
    "MaxDays": 30,
    "MaxSingleHerbDosage": 100,
    "EnableCompatibilityCheck": true,
    "AutoCalculatePrice": true
  },
  "Logging": {
    "LogLevel": {
      "LYBT.Module.Prescriptions": "Information"
    }
  }
}
```

## 🧪 测试规范

### 单元测试要求

#### 1. 业务服务测试
```csharp
[Test]
public async Task CreatePrescriptionFromFormulaAsync_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var dto = new PrescriptionFromFormulaDto
    {
        MedicalCaseId = Guid.NewGuid(),
        FormulaId = Guid.NewGuid(),
        Days = 7,
        DosageMultiplier = 1.0m
    };
    
    var formula = new FormulaModel
    {
        Id = dto.FormulaId,
        Name = "麻黄汤",
        Type = PrescriptionType.Decoction,
        Items = [
            new FormulaItemModel { HerbId = Guid.NewGuid(), Dosage = 10 }
        ]
    };
    
    _formulaRepositoryMock
        .Setup(x => x.GetByIdWithItemsAsync(dto.FormulaId))
        .ReturnsAsync(formula);
    
    // Act
    var result = await _businessService.CreatePrescriptionFromFormulaAsync(dto);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.FormulaId, Is.EqualTo(dto.FormulaId));
}

[Test]
public async Task CheckPrescriptionCompatibilityAsync_WithIncompatibleHerbs_ReturnsWarnings()
{
    // Arrange
    var prescriptionId = Guid.NewGuid();
    var prescription = new PrescriptionModel
    {
        Id = prescriptionId,
        Items = [
            new PrescriptionItemModel { HerbId = Guid.NewGuid(), Herb = new HerbModel { Name = "甘草" } },
            new PrescriptionItemModel { HerbId = Guid.NewGuid(), Herb = new HerbModel { Name = "甘遂" } }
        ]
    };
    
    _prescriptionRepositoryMock
        .Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
        .ReturnsAsync(prescription);
    
    // Act
    var result = await _compatibilityService.CheckPrescriptionCompatibilityAsync(prescriptionId);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.IsCompatible, Is.False);
    Assert.That(result.Data.Incompatibilities, Is.Not.Empty);
}
```

#### 2. 查询服务测试
```csharp
[Test]
public async Task SearchPrescriptionsAsync_WithHerbKeyword_ReturnsFilteredResults()
{
    // Arrange
    var criteria = new PrescriptionSearchDto
    {
        HerbKeyword = "当归",
        Page = 1,
        PageSize = 10
    };
    
    var prescriptions = new List<PrescriptionModel>
    {
        new() { Items = [new PrescriptionItemModel { Herb = new HerbModel { Name = "当归" } }] }
    };
    
    _prescriptionRepositoryMock
        .Setup(x => x.GetPagedAsync(It.IsAny<IQueryable<PrescriptionModel>>(), 1, 10))
        .ReturnsAsync(new PagedResult<PrescriptionSearchResultDto>(1, []));
    
    // Act
    var result = await _queryService.SearchPrescriptionsAsync(criteria);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.TotalCount, Is.EqualTo(1));
}
```

### 集成测试
```csharp
[Test]
public async Task PrescriptionWorkflow_CompleteFlow_Success()
{
    // 1. 创建处方
    var createDto = new PrescriptionCreateDto { MedicalCaseId = _testMedicalCaseId };
    var createResult = await _prescriptionService.CreatePrescriptionAsync(createDto);
    Assert.That(createResult.Success, Is.True);
    
    // 2. 添加药材
    var itemDto = new PrescriptionItemCreateDto
    {
        HerbId = _testHerbId,
        Dosage = 10
    };
    var addResult = await _prescriptionService.AddPrescriptionItemAsync(
        createResult.Data.Id, itemDto);
    Assert.That(addResult.Success, Is.True);
    
    // 3. 配伍检查
    var checkResult = await _compatibilityService.CheckPrescriptionCompatibilityAsync(
        createResult.Data.Id);
    Assert.That(checkResult.Success, Is.True);
    
    // 4. 审核处方
    var approveResult = await _prescriptionService.ApprovePrescriptionAsync(
        createResult.Data.Id, _testDoctorId);
    Assert.That(approveResult.Success, Is.True);
}
```

## 🚀 部署说明

### 数据库迁移
```bash
# Prescription模块相关迁移
dotnet ef migrations add AddPrescriptionModule --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### 配置检查清单
- [ ] PrescriptionOptions配置正确
- [ ] 数据库连接字符串有效
- [ ] MedicalCase、Formula、Herbs模块依赖可用
- [ ] AutoMapper映射配置完整
- [ ] 配伍检查数据初始化
- [ ] 日志记录级别适当
- [ ] 缓存策略配置合理

## 📚 相关文档

### 架构文档
- [UltraThink双层架构标准](../../architecture/ultrathink-dual-layer-architecture.md)
- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)  
- [API响应标准](../../architecture/ultrathink-api-response-standards-20250817.md)

### 业务文档
- [Consultation模块文档](./consultation.md) - 诊断记录关联
- [Formula模块文档](./formula.md) - 验方集成应用
- [Herbs模块文档](./herbs.md) - 药材价格和用法
- [MedicalCase模块文档](./medicalcase.md) - 医案处方记录

### 开发指南
- [模块开发规范](../../development/MODULE_DEVELOPMENT_STANDARDS.md)
- [测试指南](../../testing/MODULE_TESTING_GUIDE.md)
- [部署指南](../../deployment/MODULE_DEPLOYMENT_GUIDE.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 后端开发组