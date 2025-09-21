# LYBT.Module.Formula

> **验方管理核心模块** - 经典方剂与经验方管理中心
> 验方档案管理 | 药材组成配置 | 方剂复制与分享 | 处方转验方
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.Formula是系统的验方（经典方剂和经验方）管理核心模块，采用分层架构设计，提供完整的验方管理、药材组成配置、方剂分享和从处方创建验方等功能。作为处方系统的模板支撑，提高医生开方效率，积累诊疗经验。

**技术栈**: .NET 8 + 实体（实体（Entity）） Framework Core 8.0 + AutoMapper 13.0.1
**架构特色**: 分层架构（QueryService + BusinessService）+ 纯委托模式
**业务特色**: 支持经典方剂、临床验方、个人验方，方剂复制与分享功能

## 🎆 分层架构实现

### 架构层次图
```
FormulaService (主服务层 - 纯委托模式)
    │
    ├── FormulaQueryService (查询专业化层)
    │   ├── 基础查询
    │   │   ├── GetByIdAsync - 根据ID获取验方详情
    │   │   ├── GetPagedAsync - 分页查询验方
    │   │   └── GetAllFormulasAsync - 获取所有验方
    │   │
    │   ├── 搜索功能
    │   │   ├── SearchAsync - 关键词搜索验方
    │   │   ├── SearchFormulasAsync - 高级搜索（分页）
    │   │   └── GetFormulasAsync - 条件查询验方
    │   │
    │   ├── 分类查询
    │   │   ├── GetCategoriesAsync - 获取验方分类
    │   │   ├── GetByTypeAsync - 按类型获取验方
    │   │   └── GetTemplatesAsync - 获取模板验方
    │   │
    │   └── 验证查询
    │       ├── ExistsAsync - 检查验方是否存在
    │       └── IsNameDuplicatedAsync - 检查名称重复
    │
    └── FormulaBusinessService (业务逻辑+CRUD层)
        ├── 基础CRUD
        │   ├── CreateAsync - 创建验方
        │   ├── UpdateAsync - 更新验方
        │   ├── DeleteAsync - 删除验方
        │   ├── EnableAsync - 启用验方
        │   └── DisableAsync - 禁用验方
        │
        ├── 业务操作
        │   ├── CopyAsync - 复制验方
        │   ├── CreateFromPrescriptionAsync - 从处方创建验方
        │   ├── ToggleStatusAsync - 切换验方状态
        │   └── AnalyzeFormulaAsync - 分析验方组成
        │
        └── 分享功能
            ├── ShareFormulaAsync - 分享验方
            └── UnshareFormulaAsync - 取消分享
```

## 📦 核心接口设计

### 1. 主服务接口（统一入口）
```csharp
// IFormulaService - 在LYBT.Shared.Interfaces中定义
public interface IFormulaService
{
    // 查询操作 - 委托到QueryService
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
    Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType);
    Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();
    Task<ServiceResult<List<string>>> GetCategoriesAsync();

    // 业务操作 - 委托到BusinessService
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
    Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult> EnableAsync(Guid id);
    Task<ServiceResult> DisableAsync(Guid id);

    // 特色功能
    Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName);
    Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name);
    Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName);
    Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName);
    Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId);
}
```

### 2. 查询专业化接口
```csharp
public interface IFormulaQueryService
{
    // 基础查询
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);
    Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync();

    // 搜索功能
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
    Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query);
    Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null);

    // 分类查询
    Task<ServiceResult<List<string>>> GetCategoriesAsync();
    Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType);
    Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();

    // 推荐功能
    Task<ServiceResult<List<FormulaDto>>> GetRecommendedFormulasAsync(string symptom);
    Task<ServiceResult<List<FormulaDto>>> GetPopularFormulasAsync(int topN = 10);
}
```

### 3. 业务逻辑接口
```csharp
public interface IFormulaBusinessService
{
    // CRUD操作
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
    Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult> EnableAsync(Guid id);
    Task<ServiceResult> DisableAsync(Guid id);

    // 业务操作
    Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName);
    Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name);
    Task<ServiceResult<bool>> ToggleStatusAsync(Guid id);
    Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId);

    // 分享管理
    Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName);
    Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName);

    // 验证功能
    Task<ServiceResult<bool>> ValidateCompositionAsync(List<FormulaHerbItemDto> herbs);
}
```

## 🧪 数据传输对象（数据传输对象（数据传输对象（DTO）））

### 核心DTOs
```csharp
// 验方信息DTO - 与Formula实体对齐
public class FormulaDto : StatusDto, IRemarkable
{
    [DisplayName("验方名称")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("功效")]
    public string? Effect { get; set; }

    [DisplayName("用法")]
    public string? Usage { get; set; }

    [DisplayName("性味归经")]
    public string? Property { get; set; }

    [DisplayName("是否共享")]
    public bool IsShared { get; set; } = false;

    [DisplayName("备注")]
    public string? Remark { get; set; }

    [DisplayName("药材组成")]
    public List<FormulaHerbItemDto> Herbs { get; set; } = new();

    // 计算属性
    public int HerbCount => Herbs?.Count ?? 0;  // 药材数量
    public decimal TotalPrice { get; }  // 总价格（根据药材计算）
    public string HerbNames { get; }  // 药材名称列表
    public string Category { get; }  // 智能分类

    // 扩展属性
    public string? Indications { get; set; }  // 适应症
    public string? Source { get; set; }  // 来源
    public string? Instructions { get; set; }  // 用药指导
    public string? Contraindications { get; set; }  // 禁忌症
    public string? Preparation { get; set; }  // 制备方法
}

// 验方药材项DTO
public class FormulaHerbItemDto
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public Guid HerbId { get; set; }

    public HerbDto? Herb { get; set; }  // 药材信息
    public decimal Quantity { get; set; }  // 用量（克）
    public string? Usage { get; set; }  // 特殊用法（先煎、后下等）
    public string? Remark { get; set; }  // 备注

    // 显示属性
    public string DisplayName => $"{Herb?.Name ?? "未知药材"} {Quantity}g";
}
```

### 请求DTOs
```csharp
// 创建验方DTO
public class FormulaCreateDto : CreateDtoBase
{
    [Required(ErrorMessage = "验方名称不能为空")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Effect { get; set; }

    [StringLength(500)]
    public string? Usage { get; set; }

    [StringLength(200)]
    public string? Property { get; set; }

    public bool IsShared { get; set; } = false;

    [Required(ErrorMessage = "必须包含药材组成")]
    public List<FormulaHerbItemCreateDto> Herbs { get; set; } = new();

    [StringLength(500)]
    public string? Remark { get; set; }

    // 扩展字段
    public string? Source { get; set; }
    public string? Indications { get; set; }
    public string? Contraindications { get; set; }
}

// 更新验方DTO
public class FormulaUpdateDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Effect { get; set; }

    [StringLength(500)]
    public string? Usage { get; set; }

    [StringLength(200)]
    public string? Property { get; set; }

    public bool? IsShared { get; set; }

    public List<FormulaHerbItemUpdateDto>? Herbs { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}

// 验方查询DTO
public class FormulaQueryDto : PagedRequestDto
{
    public string? Keyword { get; set; }  // 搜索关键词
    public string? Category { get; set; }  // 分类筛选
    public bool? IsShared { get; set; }  // 是否共享
    public string? Effect { get; set; }  // 功效筛选
    public Guid? CreatorId { get; set; }  // 创建者筛选
}
```

## 💼 核心业务功能

### 1. 创建验方
```csharp
public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
{
    // 1. 验证名称唯一性
    var existingFormula = await _dbContext.Formulas
        .FirstOrDefaultAsync(f => f.Name == dto.Name);

    if (existingFormula != null)
        return ServiceResult<FormulaDto>.Failure("验方名称已存在");

    // 2. 创建验方实体
    var formula = new Formula
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Effect = dto.Effect,
        Usage = dto.Usage,
        Property = dto.Property,
        IsShared = dto.IsShared,
        Remark = dto.Remark,
        Status = CommonStatus.Enabled
    };

    // 3. 添加药材组成
    foreach (var herbDto in dto.Herbs)
    {
        var formulaHerb = new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            FormulaId = formula.Id,
            HerbId = herbDto.HerbId,
            Quantity = herbDto.Quantity,
            Usage = herbDto.Usage,
            Remark = herbDto.Remark
        };
        _dbContext.FormulaHerbItems.Add(formulaHerb);
    }

    // 4. 保存到数据库
    _dbContext.Formulas.Add(formula);
    await _dbContext.SaveChangesAsync();

    _logger.LogInformation("创建验方成功: {Name} ({Id})", formula.Name, formula.Id);

    return ServiceResult<FormulaDto>.Success(_mapper.Map<FormulaDto>(formula));
}
```

### 2. 从处方创建验方
```csharp
public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(
    Guid prescriptionId,
    string name)
{
    // 1. 获取处方信息
    var prescription = await _dbContext.Prescriptions
        .Include(p => p.Items)
        .FirstOrDefaultAsync(p => p.Id == prescriptionId);

    if (prescription == null)
        return ServiceResult<FormulaDto>.Failure("处方不存在");

    // 2. 创建验方
    var formula = new Formula
    {
        Id = Guid.NewGuid(),
        Name = name,
        Effect = prescription.Indication,  // 从处方主治转换
        Usage = prescription.Usage,
        Property = null,  // 需要后续补充
        IsShared = false,  // 默认不共享
        Remark = $"来源于处方: {prescription.Id}",
        Status = CommonStatus.Enabled
    };

    // 3. 复制处方项目为验方药材组成
    foreach (var item in prescription.Items)
    {
        var formulaHerb = new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            FormulaId = formula.Id,
            HerbId = item.HerbId,
            Quantity = item.Quantity,
            Usage = item.Usage,
            Remark = item.Remark
        };
        _dbContext.FormulaHerbItems.Add(formulaHerb);
    }

    // 4. 保存
    _dbContext.Formulas.Add(formula);
    await _dbContext.SaveChangesAsync();

    _logger.LogInformation("从处方创建验方成功: {Name} (处方ID: {PrescriptionId})", name, prescriptionId);

    return ServiceResult<FormulaDto>.Success(_mapper.Map<FormulaDto>(formula));
}
```

### 3. 复制验方
```csharp
public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
{
    // 1. 获取源验方
    var sourceFormula = await _dbContext.Formulas
        .Include(f => f.Herbs)
        .FirstOrDefaultAsync(f => f.Id == id);

    if (sourceFormula == null)
        return ServiceResult<FormulaDto>.Failure("源验方不存在");

    // 2. 创建新验方
    var newFormula = new Formula
    {
        Id = Guid.NewGuid(),
        Name = newName,
        Effect = sourceFormula.Effect,
        Usage = sourceFormula.Usage,
        Property = sourceFormula.Property,
        IsShared = false,  // 复制的验方默认不共享
        Remark = $"复制自: {sourceFormula.Name}",
        Status = CommonStatus.Enabled
    };

    // 3. 复制药材组成
    foreach (var herb in sourceFormula.Herbs)
    {
        var newHerb = new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            FormulaId = newFormula.Id,
            HerbId = herb.HerbId,
            Quantity = herb.Quantity,
            Usage = herb.Usage,
            Remark = herb.Remark
        };
        _dbContext.FormulaHerbItems.Add(newHerb);
    }

    // 4. 保存
    _dbContext.Formulas.Add(newFormula);
    await _dbContext.SaveChangesAsync();

    return ServiceResult<FormulaDto>.Success(_mapper.Map<FormulaDto>(newFormula));
}
```

### 4. 验方分析
```csharp
public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
{
    var formula = await _dbContext.Formulas
        .Include(f => f.Herbs)
        .ThenInclude(h => h.Herb)
        .FirstOrDefaultAsync(f => f.Id == formulaId);

    if (formula == null)
        return ServiceResult<FormulaAnalysisResult>.Failure("验方不存在");

    var result = new FormulaAnalysisResult
    {
        FormulaName = formula.Name,
        HerbCount = formula.Herbs.Count,
        TotalWeight = formula.Herbs.Sum(h => h.Quantity),
        EstimatedPrice = formula.Herbs.Sum(h => h.Quantity * (h.Herb?.Price ?? 0)),

        // 按功效分组
        HerbsByEffect = formula.Herbs
            .Where(h => h.Herb != null && !string.IsNullOrWhiteSpace(h.Herb.Effect))
            .GroupBy(h => h.Herb.Effect)
            .ToDictionary(g => g.Key!, g => g.Select(h => h.Herb!.Name).ToList()),

        // 特殊用法统计
        SpecialUsageCount = formula.Herbs.Count(h => !string.IsNullOrWhiteSpace(h.Usage))
    };

    return ServiceResult<FormulaAnalysisResult>.Success(result);
}
```

## 🔧 特色功能

### 1. 智能分类
验方根据名称自动判断分类：
```csharp
public string Category
{
    get
    {
        if (Name?.Contains("感冒") == true) return "内科方";
        if (Name?.Contains("外伤") == true) return "外科方";
        if (Name?.Contains("妇科") == true) return "妇科方";
        if (Name?.Contains("儿童") == true) return "儿科方";
        return "验方";  // 默认分类
    }
}
```

### 2. 分享机制
验方可以在医生之间分享：
```csharp
public async Task<ServiceResult<bool>> ShareFormulaAsync(
    Guid id,
    Guid operatorId,
    string operatorName)
{
    var formula = await _dbContext.Formulas.FindAsync(id);
    if (formula == null)
        return ServiceResult<bool>.Failure("验方不存在");

    formula.IsShared = true;
    formula.Remark = $"{formula.Remark} | 由{operatorName}分享于{DateTime.Now:yyyy-MM-dd}";

    await _dbContext.SaveChangesAsync();
    _logger.LogInformation("验方已分享: {Name} by {Operator}", formula.Name, operatorName);

    return ServiceResult<bool>.Success(true);
}
```

### 3. 验方推荐
基于症状推荐合适的验方：
```csharp
public async Task<ServiceResult<List<FormulaDto>>> GetRecommendedFormulasAsync(string symptom)
{
    var formulas = await _dbContext.Formulas
        .Where(f => f.Status == CommonStatus.Enabled &&
                   f.IsShared &&  // 只推荐共享的验方
                   (f.Effect != null && f.Effect.Contains(symptom) ||
                    f.Indications != null && f.Indications.Contains(symptom)))
        .OrderBy(f => f.Name)
        .Take(10)
        .ToListAsync();

    var dtos = _mapper.Map<List<FormulaDto>>(formulas);
    return ServiceResult<List<FormulaDto>>.Success(dtos);
}
```

## 🎯 中医验方管理特色

### 1. 验方类型
- **经典验方**: 传统经典方剂（四物汤、六味地黄丸等）
- **临床验方**: 临床实践总结的有效方剂
- **个人验方**: 医生个人经验积累的处方

### 2. 药材用法标注
支持标注每味药材的特殊用法：
- 先煎、后下、包煎
- 另煎、烊化、冲服
- 炒制、醋制、酒制等

### 3. 方剂加减
支持记录方剂的加减变化，便于临床灵活应用

## 📚 相关模块

- [Prescriptions处方模块](../LYBT.Module.Prescriptions/README.md) - 使用验方开具处方
- [Herbs药材模块](../LYBT.Module.Herbs/README.md) - 验方药材组成
- [MedicalCase医案模块](../LYBT.Module.MedicalCase/README.md) - 验方应用记录

## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FormulasController : BaseApiController
{
    private readonly IFormulaService _formulaService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FormulaDto>>>> GetPaged(
        [FromQuery] FormulaQueryDto query)
    {
        var result = await _formulaService.GetPagedAsync(query);
        return HandleServiceResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormulaDto>>> Create(
        [FromBody] FormulaCreateDto dto)
    {
        var result = await _formulaService.CreateAsync(dto);
        return HandleServiceResult(result);
    }

    [HttpPost("{id}/copy")]
    public async Task<ActionResult<ApiResponse<FormulaDto>>> Copy(
        Guid id, [FromBody] CopyFormulaDto dto)
    {
        var result = await _formulaService.CopyAsync(id, dto.NewName);
        return HandleServiceResult(result);
    }

    [HttpPost("from-prescription/{prescriptionId}")]
    public async Task<ActionResult<ApiResponse<FormulaDto>>> CreateFromPrescription(
        Guid prescriptionId, [FromBody] CreateFromPrescriptionDto dto)
    {
        var result = await _formulaService.CreateFromPrescriptionAsync(
            prescriptionId, dto.Name);
        return HandleServiceResult(result);
    }

    [HttpPost("{id}/share")]
    public async Task<ActionResult<ApiResponse<bool>>> Share(Guid id)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var result = await _formulaService.ShareFormulaAsync(id, userId, userName);
        return HandleServiceResult(result);
    }
}
```

### 服务注册
```csharp
// 在FormulaModule.cs中
services.AddScoped<IFormulaService, FormulaService>();
services.AddScoped<IFormulaQueryService, FormulaQueryService>();
services.AddScoped<IFormulaBusinessService, FormulaBusinessService>();
services.AddScoped<IFormulaRepository, FormulaRepository>();
```

---

> 📌 **最新成果**: 分层架构完整实现，验方管理功能全覆盖
> 🎆 **生产就绪**: 完整的验方管理体系，支持经验积累与分享

## 🎯 项目概述
- [待补充] 简要描述 LYBT.Module.Formula 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。
