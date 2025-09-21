# LYBT.Module.Herbs

> **药材管理核心模块** - 中药材信息管理中心
> 药材档案管理 | 价格维护 | 拼音检索 | 批量导入导出
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.Herbs是系统的药材管理核心模块，采用分层架构设计，提供完整的中药材信息管理、价格维护、拼音检索和批量导入导出功能。作为处方系统的基础数据支撑，专注于药材档案的记录和管理，不涉及库存管理。

**技术栈**: .NET 8 + 实体（实体（Entity）） Framework Core 8.0 + AutoMapper 13.0.1
**架构特色**: 分层架构（QueryService + BusinessService）+ 纯委托模式
**业务特色**: Record-Only模式，纯药材档案管理，无库存概念，适合小诊所

## 🎆 分层架构实现

### 架构层次图
```
HerbService (主服务层 - 纯委托模式)
    │
    ├── HerbQueryService (查询专业化层)
    │   ├── 基础查询
    │   │   ├── GetByIdAsync - 根据ID获取药材详情
    │   │   ├── GetAllAsync - 获取所有药材列表
    │   │   └── GetPagedAsync - 分页查询药材
    │   │
    │   ├── 搜索功能
    │   │   ├── SearchAsync - 关键词搜索（支持拼音）
    │   │   ├── SearchByNameAsync - 按名称搜索
    │   │   └── GetByIdsAsync - 批量获取药材
    │   │
    │   ├── 筛选查询
    │   │   ├── GetAvailableHerbsAsync - 获取可用药材
    │   │   └── GetByPriceRangeAsync - 按价格范围查询
    │   │
    │   └── 辅助查询
    │       └── GetListAsync - 灵活的列表查询
    │
    └── HerbBusinessService (业务逻辑+CRUD层)
        ├── 基础CRUD
        │   ├── CreateHerbWithAutoCodeAsync - 创建药材（自动生成拼音码）
        │   ├── UpdateAsync - 更新药材信息
        │   ├── SoftDeleteAsync - 软删除药材
        │   └── SetStatusAsync - 设置药材状态
        │
        ├── 批量操作
        │   ├── BatchUpdateStatusAsync - 批量更新状态
        │   ├── ImportHerbsAsync - 批量导入（简化版）
        │   └── ExportHerbsAsync - 导出CSV格式
        │
        └── 辅助功能
            └── GeneratePinYinCode - 自动生成拼音码
```

## 📦 核心接口设计

### 1. 主服务接口（统一入口）
```csharp
// IHerbService - 在LYBT.Shared.Interfaces中定义
public interface IHerbService
{
    // 查询操作 - 委托到QueryService
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbSearchDto query);
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
    Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);

    // 业务操作 - 委托到BusinessService
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult> EnableAsync(Guid id);
    Task<ServiceResult> DisableAsync(Guid id);

    // 批量操作
    Task<ServiceResult<object>> ImportHerbsAsync(List<HerbCreateDto> herbs);
    Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query);
}
```

### 2. 查询专业化接口
```csharp
public interface IHerbQueryService
{
    // 基础查询
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<HerbDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbSearchDto query);

    // 搜索功能
    Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
    Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name);
    Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);

    // 筛选查询
    Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();
    Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    // 统计分析
    Task<ServiceResult<HerbStatisticsDto>> GetStatisticsAsync();
}
```

### 3. 业务逻辑接口
```csharp
public interface IHerbBusinessService
{
    // CRUD操作
    Task<ServiceResult<HerbDto>> CreateHerbWithAutoCodeAsync(HerbCreateDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
    Task<ServiceResult<bool>> SoftDeleteAsync(Guid id);
    Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isEnabled);

    // 批量操作
    Task<ServiceResult<bool>> BatchUpdateStatusAsync(List<Guid> ids, bool status, string? reason);
    Task<ServiceResult<int>> BatchImportAsync(List<HerbImportDto> herbs);

    // 验证功能
    Task<ServiceResult<bool>> ValidateHerbNameAsync(string name, Guid? excludeId = null);
}
```

## 🧪 数据传输对象（数据传输对象（数据传输对象（DTO）））

### 核心DTOs
```csharp
// 药材信息DTO - 与Herb实体对齐
public class HerbDto : StatusDto, IRemarkable
{
    [DisplayName("药材名称")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    [DisplayName("产地")]
    public string? Origin { get; set; }

    [DisplayName("规格")]
    public string? Spec { get; set; }

    [DisplayName("单位")]
    public string Unit { get; set; } = "克";

    [DisplayName("单价")]
    public decimal Price { get; set; }

    [DisplayName("功效")]
    public string? Effect { get; set; }

    [DisplayName("用法")]
    public string? Usage { get; set; }

    [DisplayName("备注")]
    public string? Remark { get; set; }
}
```

### 请求DTOs
```csharp
// 创建药材DTO
public class HerbCreateDto : CreateDtoBase
{
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? PinYinCode { get; set; }  // 可选，系统会自动生成

    [StringLength(100)]
    public string? Origin { get; set; }

    [StringLength(50)]
    public string? Spec { get; set; }

    [Required(ErrorMessage = "单位不能为空")]
    [StringLength(20)]
    public string Unit { get; set; } = "克";

    [Required(ErrorMessage = "单价不能为空")]
    [Range(0, 999999.99)]
    public decimal Price { get; set; }

    [StringLength(1000)]
    public string? Effect { get; set; }

    [StringLength(500)]
    public string? Usage { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}

// 更新药材DTO
public class HerbUpdateDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Origin { get; set; }

    [StringLength(50)]
    public string? Spec { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }

    [Range(0, 999999.99)]
    public decimal? Price { get; set; }

    [StringLength(1000)]
    public string? Effect { get; set; }

    [StringLength(500)]
    public string? Usage { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}

// 药材搜索DTO
public class HerbSearchDto : PagedRequestDto
{
    public string? Keyword { get; set; }  // 支持名称、拼音、产地、功效搜索
    public decimal? MinPrice { get; set; }  // 最低价格
    public decimal? MaxPrice { get; set; }  // 最高价格
    public bool IncludeExpired { get; set; } = false;  // 是否包含已禁用的药材
}
```

## 💼 核心业务功能

### 1. 创建药材（自动生成拼音码）
```csharp
public async Task<ServiceResult<HerbDto>> CreateHerbWithAutoCodeAsync(HerbCreateDto dto)
{
    // 1. 验证药材名称唯一性
    var existingHerb = await _context.Herbs
        .FirstOrDefaultAsync(h => h.Name == dto.Name);

    if (existingHerb != null)
        return ServiceResult<HerbDto>.Failure("药材名称已存在");

    // 2. 自动生成拼音码
    if (string.IsNullOrWhiteSpace(dto.PinYinCode))
    {
        dto.PinYinCode = PinYinHelper.GetFirstLetters(dto.Name);
    }

    // 3. 创建药材实体
    var herb = new Herb
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        PinYinCode = dto.PinYinCode,
        Origin = dto.Origin,
        Spec = dto.Spec,
        Unit = dto.Unit,
        Price = dto.Price,
        Effect = dto.Effect,
        Usage = dto.Usage,
        Remark = dto.Remark,
        Status = CommonStatus.Enabled
    };

    // 4. 保存到数据库
    _context.Herbs.Add(herb);
    await _context.SaveChangesAsync();

    _logger.LogInformation("创建药材成功: {Name} ({Id})", herb.Name, herb.Id);

    return ServiceResult<HerbDto>.Success(_mapper.Map<HerbDto>(herb));
}
```

### 2. 搜索药材（支持拼音检索）
```csharp
public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
{
    if (string.IsNullOrWhiteSpace(keyword))
        return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());

    var searchTerm = keyword.Trim().ToUpper();

    var herbs = await _context.Herbs
        .Where(h => h.Status == CommonStatus.Enabled &&
                   (h.Name.Contains(keyword) ||                      // 名称匹配
                    (h.PinYinCode != null && h.PinYinCode.Contains(searchTerm)) ||  // 拼音码匹配
                    (h.Origin != null && h.Origin.Contains(keyword)) ||              // 产地匹配
                    (h.Effect != null && h.Effect.Contains(keyword))))               // 功效匹配
        .OrderBy(h => h.Name)
        .Take(50)  // 限制返回数量
        .ToListAsync();

    var dtos = _mapper.Map<List<HerbDto>>(herbs);
    return ServiceResult<List<HerbDto>>.Success(dtos);
}
```

### 3. 分页查询药材
```csharp
public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbSearchDto query)
{
    var queryable = _context.Herbs.AsQueryable();

    // 默认只查询启用状态的药材
    if (!query.IncludeExpired)
    {
        queryable = queryable.Where(h => h.Status == CommonStatus.Enabled);
    }

    // 关键词搜索
    if (!string.IsNullOrWhiteSpace(query.Keyword))
    {
        var keyword = query.Keyword.Trim();
        queryable = queryable.Where(h =>
            h.Name.Contains(keyword) ||
            (h.PinYinCode != null && h.PinYinCode.Contains(keyword)) ||
            (h.Origin != null && h.Origin.Contains(keyword)) ||
            (h.Effect != null && h.Effect.Contains(keyword)));
    }

    // 价格范围筛选
    if (query.MinPrice.HasValue)
        queryable = queryable.Where(h => h.Price >= query.MinPrice.Value);

    if (query.MaxPrice.HasValue)
        queryable = queryable.Where(h => h.Price <= query.MaxPrice.Value);

    // 计算总数
    var totalCount = await queryable.CountAsync();

    // 排序和分页
    var herbs = await queryable
        .OrderBy(h => h.Name)
        .Skip((query.PageIndex - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToListAsync();

    var dtos = _mapper.Map<List<HerbDto>>(herbs);
    var pagedResult = new PagedResult<HerbDto>(dtos, totalCount, query.PageIndex, query.PageSize);

    return ServiceResult<PagedResult<HerbDto>>.Success(pagedResult);
}
```

### 4. 导出药材数据（CSV格式）
```csharp
public async Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query)
{
    // 获取药材数据
    var herbsResult = await _queryService.GetAvailableHerbsAsync();
    if (!herbsResult.IsSuccess || herbsResult.Data == null)
        return ServiceResult<byte[]>.Failure("获取药材列表失败");

    try
    {
        // Record-Only模式：使用CSV导出，简单高效
        var csvContent = "药材名称,产地,规格,单位,价格,功效,用法,状态\n";

        foreach (var herb in herbsResult.Data)
        {
            csvContent += $"{herb.Name},{herb.Origin},{herb.Spec},{herb.Unit}," +
                         $"{herb.Price},{herb.Effect},{herb.Usage}," +
                         $"{(herb.IsEnabled ? "启用" : "禁用")}\n";
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        return ServiceResult<byte[]>.Success(bytes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导出药材数据异常");
        return ServiceResult<byte[]>.Failure($"导出异常: {ex.Message}");
    }
}
```

## 🔧 特色功能

### 1. 拼音码自动生成
系统会自动为药材生成拼音码，方便快速检索：
```csharp
// 使用PinYinHelper自动生成拼音码
if (string.IsNullOrWhiteSpace(dto.PinYinCode))
{
    dto.PinYinCode = PinYinHelper.GetFirstLetters(dto.Name);
    // 例如："黄芪" → "HQ"
    // 例如："当归" → "DG"
    // 例如："人参" → "RS"
}
```

### 2. 软删除机制
药材不会真正从数据库删除，而是通过状态标记：
```csharp
public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
{
    var herb = await _context.Herbs.FindAsync(id);
    if (herb == null)
        return ServiceResult<bool>.Failure("药材不存在");

    // 软删除：仅标记状态
    herb.Status = CommonStatus.Deleted;
    herb.Remark = $"已删除于 {DateTime.Now:yyyy-MM-dd HH:mm}";

    await _context.SaveChangesAsync();
    return ServiceResult<bool>.Success(true);
}
```

### 3. 批量状态更新
支持批量启用/禁用药材：
```csharp
public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(
    List<Guid> ids,
    bool status,
    string? reason = null)
{
    var herbs = await _context.Herbs
        .Where(h => ids.Contains(h.Id))
        .ToListAsync();

    foreach (var herb in herbs)
    {
        herb.Status = status ? CommonStatus.Enabled : CommonStatus.Disabled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            herb.Remark = $"{(status ? "启用" : "禁用")}原因: {reason}";
        }
    }

    await _context.SaveChangesAsync();
    return ServiceResult<bool>.Success(true);
}
```

## 🎯 中药材管理特色

### 1. Record-Only模式
- **无库存概念**：只管理药材档案信息，不涉及库存
- **价格记录**：记录当前参考价格，处方时可调整
- **适合小诊所**：简化管理，专注核心功能

### 2. 常用药材单位
- 克（g）- 默认单位
- 两
- 钱
- 个（枚）
- 对（双）
- 条
- 片

### 3. 药材分类（通过备注或标签管理）
- 解表药
- 清热药
- 泻下药
- 祛风湿药
- 化湿药
- 温里药
- 理气药
- 消食药
- 止血药
- 活血化瘀药
- 化痰止咳平喘药
- 安神药
- 补益药
- 收涩药

## 📚 相关模块

- [Prescriptions处方模块](../LYBT.Module.Prescriptions/README.md) - 使用药材开具处方
- [Formula验方模块](../LYBT.Module.Formula/README.md) - 经典方剂药材组成
- [Infrastructure基础设施](../../Core/LYBT.基础设施（基础设施（Infrastructure））/README.md) - 数据访问基础

## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class HerbsController : BaseApiController
{
    private readonly IHerbService _herbService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<HerbDto>>>> GetPaged(
        [FromQuery] HerbSearchDto query)
    {
        var result = await _herbService.GetPagedAsync(query);
        return HandleServiceResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<HerbDto>>> Create(
        [FromBody] HerbCreateDto dto)
    {
        var result = await _herbService.CreateAsync(dto);
        return HandleServiceResult(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<HerbDto>>>> Search(
        [FromQuery] string keyword)
    {
        var result = await _herbService.SearchAsync(keyword);
        return HandleServiceResult(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var result = await _herbService.ExportHerbsAsync(new PagedQueryBaseDto());
        if (result.IsSuccess && result.Data != null)
        {
            return File(result.Data, "text/csv", $"herbs_{DateTime.Now:yyyyMMdd}.csv");
        }
        return BadRequest(result.ErrorMessage);
    }
}
```

### 服务注册
```csharp
// 在HerbsModule.cs中
services.AddScoped<IHerbService, HerbService>();
services.AddScoped<IHerbQueryService, HerbQueryService>();
services.AddScoped<IHerbBusinessService, HerbBusinessService>();
services.AddScoped<IHerbRepository, HerbRepository>();

// 注册拼音码生成服务
services.AddSingleton<IPinYinHelper, PinYinHelper>();
```

---

> 📌 **最新成果**: 分层架构完整实现，Record-Only模式精简高效
> 🎆 **生产就绪**: 完整的药材档案管理，适合小型中医诊所需求

## 🎯 项目概述
- [待补充] 简要描述 LYBT.Module.Herbs 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- [待补充] API 路由前缀：/api/v1/herbs
- [待补充] 控制器与端点：列出主要 Controller 与示例端点
- 参考 WebAPI：src/Server/Services/LYBT.WebAPI/README.md

## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- [待补充] 本模块相关的设计/实现文档链接
