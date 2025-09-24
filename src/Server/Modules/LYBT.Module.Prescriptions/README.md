# LYBT.Module.Prescriptions

> **处方管理核心模块** - 中医智能处方系统
> 处方开具与管理 | 药材项目管理 | 剂量自动计算 | 处方复制功能
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.Prescriptions是系统的处方管理核心模块，采用分层架构设计，提供完整的中医处方开具、管理、验证和统计功能。与MedicalCase（医案）、Consultation（诊断）和Herbs（药材）模块紧密集成，支撑完整的诊疗流程。

**技术栈**: .NET 8 + 实体（实体（Entity）） Framework Core 8.0 + AutoMapper 13.0.1 + FluentValidation
**架构特色**: 分层架构（QueryService + BusinessService）+ 纯委托模式
**业务特色**: 支持处方复制、快速保存、剂量计算、处方统计等中医特色功能

## 🎆 分层架构实现

### 架构层次图
```
PrescriptionService (主服务层 - 纯委托模式)
    │
    ├── PrescriptionQueryService (查询专业化层)
    │   ├── 基础查询
    │   │   ├── GetByIdAsync - 根据ID获取处方详情
    │   │   └── GetPagedAsync - 分页查询处方
    │   │
    │   ├── 关联查询
    │   │   ├── GetByPatientIdAsync - 获取患者处方历史
    │   │   ├── GetByMedicalCaseIdAsync - 获取医案相关处方
    │   │   └── GetDoctorTodayPrescriptionsAsync - 获取医生今日处方
    │   │
    │   ├── 搜索功能
    │   │   ├── SearchAsync - 关键词搜索处方
    │   │   └── GetAllAsync - 获取所有处方列表
    │   │
    │   └── 统计分析
    │       └── GetStatsAsync - 获取处方统计信息
    │
    └── PrescriptionBusinessService (业务逻辑+CRUD层)
        ├── 基础CRUD
        │   ├── CreateAsync - 创建处方
        │   ├── UpdateAsync - 更新处方
        │   ├── DeleteAsync - 删除处方
        │   └── CancelAsync - 取消处方
        │
        ├── 业务操作
        │   ├── CopyAsync - 复制处方
        │   ├── CopyLastPrescriptionAsync - 复制患者上次处方
        │   └── QuickSaveAsync - 快速保存处方（草稿）
        │
        └── 验证功能
            └── ValidateAsync - 验证处方数据
```

## 📦 核心接口设计

### 1. 主服务接口（统一入口）
```csharp
// IPrescriptionService - 在LYBT.Shared.Interfaces中定义
public interface IPrescriptionService
{
    // 查询操作 - 委托到QueryService
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);
    Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId);
    Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword);

    // 业务操作 - 委托到BusinessService
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName);

    // 验证操作
    Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto);
}
```

### 2. 查询专业化接口
```csharp
public interface IPrescriptionQueryService
{
    // 基础查询
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);

    // 患者相关查询
    Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId);
    Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    // 医生相关查询
    Task<ServiceResult<List<PrescriptionDto>>> GetDoctorTodayPrescriptionsAsync(Guid doctorId);

    // 搜索和统计
    Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword);
    Task<ServiceResult<List<PrescriptionDto>>> GetAllAsync();
    Task<ServiceResult<PrescriptionStatsDto>> GetStatsAsync();
}
```

### 3. 业务逻辑接口
```csharp
public interface IPrescriptionBusinessService
{
    // CRUD操作
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<bool>> CancelAsync(Guid id, Guid operatorId, string operatorName);

    // 处方复制
    Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName, Guid operatorId, string operatorName);
    Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName);

    // 快速保存
    Task<ServiceResult<bool>> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName);
}
```

## 🧪 数据传输对象（数据传输对象（数据传输对象（DTO）））

### 核心DTOs
```csharp
// 处方信息DTO - 与Prescription实体对齐
public class PrescriptionDto : StatusDto, IRemarkable
{
    public Guid MedicalCaseId { get; set; }
    public Guid PatientId { get; set; }
    public Guid UserId { get; set; }  // 医生ID

    public string? Name { get; set; }  // 患者姓名
    public string? DoctorName { get; set; }  // 医生姓名

    public string? Diagnosis { get; set; }  // 诊断
    public string? Indication { get; set; }  // 主治
    public string? Usage { get; set; }  // 用法
    public string? Advice { get; set; }  // 医嘱

    public int DosageCount { get; set; } = 7;  // 剂数
    public decimal Discount { get; set; } = 1.0m;  // 折扣

    public string? FormulaSource { get; set; }  // 验方来源
    public string? DosageForm { get; set; } = "汤剂";  // 剂型

    public List<PrescriptionItemDto> Items { get; set; } = new();  // 处方项目

    // 计算属性
    public decimal SingleDosePrice { get; }  // 单帖价格
    public decimal TotalPrice { get; }  // 总价格
    public decimal TotalWeight { get; }  // 总重量
}

// 处方项目DTO
public class PrescriptionItemDto
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public Guid HerbId { get; set; }

    public string HerbName { get; set; }  // 药材名称
    public decimal Quantity { get; set; }  // 数量
    public decimal UnitPrice { get; set; }  // 单价
    public string Unit { get; set; } = "g";  // 单位
    public string? Usage { get; set; }  // 用法（如：先煎、后下）
    public string? Remark { get; set; }  // 备注
}
```

### 请求DTOs
```csharp
// 创建处方DTO
public class PrescriptionCreateDto : PrescriptionInputBaseDto
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public Guid? ConsultationId { get; set; }

    [Required]
    public string Diagnosis { get; set; }  // 诊断

    [Range(1, 30)]
    public int DosageCount { get; set; } = 7;  // 剂数

    public string? DosageForm { get; set; }  // 剂型
    public string? Usage { get; set; }  // 用法说明

    [Required]
    public List<PrescriptionItemCreateDto> Items { get; set; }  // 处方项目
}

// 更新处方DTO
public class PrescriptionEditDto
{
    public string? Diagnosis { get; set; }
    public string? Indication { get; set; }
    public string? Usage { get; set; }
    public string? Advice { get; set; }

    public int? DosageCount { get; set; }
    public decimal? Discount { get; set; }

    public List<PrescriptionItemEditDto>? Items { get; set; }
}

// 处方查询DTO
public class PrescriptionQueryDto : PagedRequestDto
{
    public string? Keyword { get; set; }  // 搜索关键词
    public Guid? PatientId { get; set; }  // 患者ID
    public Guid? DoctorId { get; set; }  // 医生ID
    public int? Status { get; set; }  // 状态筛选
    public DateTime? StartDate { get; set; }  // 开始日期
    public DateTime? EndDate { get; set; }  // 结束日期
}

// 快速保存DTO
public class QuickPrescriptionDto
{
    public string? Diagnosis { get; set; }  // 诊断
    public string? Advice { get; set; }  // 医嘱
    public int? DosageCount { get; set; }  // 剂数
}
```

## 💼 核心业务功能

### 1. 处方开具流程
```csharp
// 在PrescriptionBusinessService中
public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
{
    // 1. 数据验证
    var validationResult = await ValidateCreateAsync(dto);
    if (!validationResult.IsSuccess)
        return ServiceResult<PrescriptionDto>.Failure(validationResult.Message);

    // 2. 创建处方实体
    var prescription = new Prescription
    {
        Id = Guid.NewGuid(),
        MedicalCaseId = dto.MedicalCaseId,
        PatientId = dto.PatientId,
        UserId = dto.DoctorId,
        Indication = dto.Diagnosis,
        DosageCount = dto.DosageCount,
        Status = PrescriptionStatus.Draft,
        Advice = dto.Advice
    };

    // 3. 添加处方项目
    foreach (var itemDto in dto.Items)
    {
        var item = new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescription.Id,
            HerbId = itemDto.HerbId,
            HerbName = itemDto.HerbName,
            Quantity = itemDto.Quantity,
            UnitPrice = itemDto.UnitPrice,
            Unit = itemDto.Unit,
            Usage = itemDto.Usage
        };
        _context.PrescriptionItems.Add(item);
    }

    // 4. 保存到数据库
    _context.Prescriptions.Add(prescription);
    await _context.SaveChangesAsync();

    // 5. 返回DTO
    return ServiceResult<PrescriptionDto>.Success(_mapper.Map<PrescriptionDto>(prescription));
}
```

### 2. 处方复制功能
```csharp
// 复制处方的核心逻辑
public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid sourceId, string newName, Guid operatorId, string operatorName)
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. 获取源处方
        var sourcePrescription = await _context.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == sourceId);

        // 2. 创建新处方（复制基本信息）
        var newPrescription = new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = sourcePrescription.PatientId,
            UserId = sourcePrescription.UserId,
            MedicalCaseId = sourcePrescription.MedicalCaseId,
            Indication = newName,  // 使用新名称
            DosageCount = sourcePrescription.DosageCount,
            Advice = sourcePrescription.Advice,
            Status = PrescriptionStatus.Draft,  // 新处方为草稿状态
            Remark = $"复制自: {sourcePrescription.Indication}"
        };

        // 3. 复制所有处方项目
        foreach (var sourceItem in sourcePrescription.Items)
        {
            var newItem = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = newPrescription.Id,
                HerbId = sourceItem.HerbId,
                HerbName = sourceItem.HerbName,
                Quantity = sourceItem.Quantity,
                UnitPrice = sourceItem.UnitPrice,
                Unit = sourceItem.Unit,
                Usage = sourceItem.Usage
            };
            _context.PrescriptionItems.Add(newItem);
        }

        // 4. 保存并提交事务
        _context.Prescriptions.Add(newPrescription);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<PrescriptionDto>.Success(_mapper.Map<PrescriptionDto>(newPrescription));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "复制处方失败");
        return ServiceResult<PrescriptionDto>.Failure($"复制处方失败: {ex.Message}");
    }
}
```

### 3. 处方查询和搜索
```csharp
// 分页查询处方（支持多条件筛选）
public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
{
    var queryable = _context.Prescriptions.AsQueryable();

    // 排除已删除的处方
    queryable = queryable.Where(p => p.Remark == null || !p.Remark.Contains("处方已删除"));

    // 关键词搜索
    if (!string.IsNullOrWhiteSpace(query.Keyword))
    {
        var keyword = query.Keyword.Trim();
        queryable = queryable.Where(p =>
            (p.Indication != null && p.Indication.Contains(keyword)) ||
            (p.Advice != null && p.Advice.Contains(keyword)));
    }

    // 患者筛选
    if (query.PatientId.HasValue)
        queryable = queryable.Where(p => p.PatientId == query.PatientId.Value);

    // 医生筛选
    if (query.DoctorId.HasValue)
        queryable = queryable.Where(p => p.UserId == query.DoctorId.Value);

    // 状态筛选
    if (query.Status.HasValue)
    {
        var prescriptionStatus = query.Status.Value == 0 ?
            PrescriptionStatus.Draft : PrescriptionStatus.Completed;
        queryable = queryable.Where(p => p.Status == prescriptionStatus);
    }

    // 分页处理
    var totalCount = await queryable.CountAsync();
    var prescriptions = await queryable
        .OrderByDescending(p => p.Id)
        .Skip((query.PageIndex - 1) * query.PageSize)
        .Take(query.PageSize)
        .Include(p => p.Items)
        .ToListAsync();

    var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);

    return ServiceResult<PagedResult<PrescriptionDto>>.Success(new PagedResult<PrescriptionDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        CurrentPage = query.PageIndex,
        PageSize = query.PageSize
    });
}
```

## 🔧 特色功能

### 1. 价格计算（自动计算）
处方的价格通过计算属性自动得出，不存储在数据库中：
```csharp
// 单帖价格（所有药材单价×数量×折扣）
public decimal SingleDosePrice
{
    get
    {
        if (Items == null || !Items.Any())
            return 0m;

        var subtotal = Items.Sum(item => item.UnitPrice * item.Quantity);
        return subtotal * Discount;
    }
}

// 总价格（单帖价格×剂数）
public decimal TotalPrice => SingleDosePrice * DosageCount;
```

### 2. 快速保存功能
支持在诊疗过程中快速保存处方草稿：
```csharp
public async Task<ServiceResult<bool>> QuickSaveAsync(
    Guid prescriptionId,
    QuickPrescriptionDto dto,
    Guid operatorId,
    string operatorName)
{
    var prescription = await _context.Prescriptions
        .FirstOrDefaultAsync(p => p.Id == prescriptionId);

    // 只能编辑草稿状态的处方
    if (prescription.Status != PrescriptionStatus.Draft)
        return ServiceResult<bool>.Failure("只能编辑草稿状态的处方");

    // 快速更新基本信息
    if (!string.IsNullOrWhiteSpace(dto.Diagnosis))
        prescription.Indication = dto.Diagnosis;

    if (!string.IsNullOrWhiteSpace(dto.Advice))
        prescription.Advice = dto.Advice;

    if (dto.DosageCount.HasValue)
        prescription.DosageCount = dto.DosageCount.Value;

    await _context.SaveChangesAsync();
    return ServiceResult<bool>.Success(true);
}
```

### 3. 处方统计
提供处方统计信息供分析使用：
```csharp
public async Task<ServiceResult<PrescriptionStatsDto>> GetStatsAsync()
{
    var stats = new PrescriptionStatsDto
    {
        TotalCount = await _context.Prescriptions
            .CountAsync(p => p.Remark == null || !p.Remark.Contains("处方已删除")),

        DraftCount = await _context.Prescriptions
            .CountAsync(p => p.Status == PrescriptionStatus.Draft),

        CompletedCount = await _context.Prescriptions
            .CountAsync(p => p.Status == PrescriptionStatus.Completed)
    };

    return ServiceResult<PrescriptionStatsDto>.Success(stats);
}
```

## 🎯 中医处方特色

### 1. 药材用法标注
支持标注每味药材的特殊用法：
- 先煎：矿石类、贝壳类药材
- 后下：芳香类药材
- 包煎：细小种子类、花粉类
- 另煎：贵重药材
- 烊化：胶类药材
- 冲服：粉末类药材

### 2. 剂型支持
- 汤剂（默认）
- 散剂
- 丸剂
- 膏方
- 颗粒剂
- 代茶饮

### 3. 处方来源管理
- 经典方剂（如：小柴胡汤、四物汤）
- 经验方（医生个人经验方）
- 协定方（医院协定处方）
- 自拟方（临时组方）

## 📚 相关模块

- [MedicalCase医案模块](../LYBT.Module.MedicalCase/README.md) - 处方所属医案
- [Consultation诊断模块](../LYBT.Module.Consultation/README.md) - 处方关联诊断
- [Herbs药材模块](../LYBT.Module.Herbs/README.md) - 处方项目药材
- [Formula验方模块](../LYBT.Module.Formula/README.md) - 处方模板来源

## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PrescriptionsController : BaseApiController
{
    private readonly IPrescriptionService _prescriptionService;

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
    {
        var result = await _prescriptionService.GetByIdAsync(id);
        return HandleServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Create(
        [FromBody] PrescriptionCreateDto dto)
    {
        var result = await _prescriptionService.CreateAsync(dto);
        return HandleServiceResult(result);
    }

    [HttpPost("{id}/copy")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Copy(
        Guid id, [FromBody] CopyPrescriptionDto dto)
    {
        var result = await _prescriptionService.CopyAsync(id, dto.NewName);
        return HandleServiceResult(result);
    }
}
```

### 服务注册
```csharp
// 在PrescriptionsModule.cs中
services.AddScoped<IPrescriptionService, PrescriptionService>();
services.AddScoped<IPrescriptionQueryService, PrescriptionQueryService>();
services.AddScoped<IPrescriptionBusinessService, PrescriptionBusinessService>();
services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
```

---

> 📌 **最新成果**: 分层架构完整实现，处方管理功能全覆盖
> 🎆 **生产就绪**: 支持完整的中医处方开具流程，可直接应用于临床

## 🎯 项目概述
- [待补充] 简要描述 LYBT.Module.Prescriptions 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- 控制器:   路由前缀: /api/v1/Prescriptions
- 控制器:   路由前缀: /api/v1/prescriptions/operation
## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- src/Shared/LYBT.Shared.Interfaces/Api/IPrescriptionsApi.cs

