# Herbs Module (中药材管理模块)

## 📋 项目概述

### 项目定位
**Herbs 模块**是凌隐宝堂中医诊所系统的**中药材基础数据管理模块**，专注于药材信息维护、价格管理和处方用药支撑。作为纯数据管理模块，不涉及库存管理，专门为处方开具提供标准化的药材选择和价格信息。

### 核心价值
- 🌿 **标准化药材库**: 完整的中药材信息数据库
- 💰 **价格管理**: 实时价格更新和成本控制
- 📋 **处方支撑**: 为处方开具提供药材选择依据
- 🔍 **智能检索**: 多维度药材搜索和筛选功能
- 📊 **用药分析**: 药材使用频次和成本统计
- 📤 **数据管理**: 批量导入导出和数据维护功能

### 业务定位 (v1.0)
```
Herbs (药材基础数据) ← 本模块
    ↓ 提供数据
Prescriptions (处方开具) ← 引用药材信息和价格
    ↓ 统计反馈
药材使用分析 ← 用药频次和成本分析
```

## 🏗️ 技术架构

### UltraThink双层架构实现
```
HerbService (主服务 - 纯委托层)
├── HerbQueryService (查询专业层)
│   ├── 药材信息检索 (名称、功效、归经搜索)
│   ├── 价格查询统计 (价格范围、涨跌分析)
│   ├── 用药频次统计 (处方使用统计)
│   └── 药材分类报表 (按功效、归经分类)
└── HerbBusinessService (业务逻辑层)
    ├── 药材信息管理 (CRUD操作、状态控制)
    ├── 价格管理服务 (价格更新、历史记录)
    ├── 批量数据处理 (导入导出、批量更新)
    ├── 药材验证服务 (重复检查、格式验证)
    └── 使用统计更新 (处方引用计数更新)
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
builder.Services.AddHerbModule();

public static class HerbModuleExtensions
{
    public static IServiceCollection AddHerbModule(this IServiceCollection services)
    {
        // Repository Layer
        services.AddScoped<IHerbRepository, HerbRepository>();
        services.AddScoped<IHerbPriceHistoryRepository, HerbPriceHistoryRepository>();
        
        // Service Layer - UltraThink双层架构
        services.AddScoped<HerbQueryService>();
        services.AddScoped<HerbBusinessService>();
        services.AddScoped<IHerbService, HerbService>(); // 纯委托
        
        // 专业服务
        services.AddScoped<IHerbImportExportService, HerbImportExportService>();
        services.AddScoped<IHerbPriceService, HerbPriceService>();
        
        return services;
    }
}
```

### 核心实体模型
```csharp
public class HerbModel : BaseEntity
{
    // 基本信息
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }            // 药材名称
    
    [MaxLength(100)]
    public string? EnglishName { get; set; }    // 英文名
    
    [MaxLength(100)]
    public string? LatinName { get; set; }      // 拉丁学名
    
    [MaxLength(50)]
    public string? Alias { get; set; }          // 别名
    
    // 药材属性
    [MaxLength(50)]
    public string? Category { get; set; }       // 分类(清热药、补气药等)
    
    [MaxLength(50)]
    public string? Nature { get; set; }         // 性味(寒、热、平等)
    
    [MaxLength(50)]
    public string? Flavor { get; set; }         // 味道(苦、甘、辛等)
    
    [MaxLength(100)]
    public string? Meridian { get; set; }       // 归经
    
    [MaxLength(200)]
    public string? Function { get; set; }       // 功效
    
    [MaxLength(200)]
    public string? Indication { get; set; }     // 主治
    
    [MaxLength(100)]
    public string? Usage { get; set; }          // 用法用量
    
    [MaxLength(200)]
    public string? Caution { get; set; }        // 注意事项
    
    // 价格信息
    [Required]
    public decimal Price { get; set; }          // 当前单价(元/克)
    public DateTime? PriceUpdatedAt { get; set; } // 价格更新时间
    public Guid? PriceUpdatedBy { get; set; }   // 价格更新人
    
    // 规格信息
    [MaxLength(50)]
    public string? Specification { get; set; }  // 规格
    
    [MaxLength(50)]
    public string? Origin { get; set; }         // 产地
    
    [MaxLength(50)]
    public string? Quality { get; set; }        // 品质等级
    
    // 状态信息
    public HerbStatus Status { get; set; }      // 药材状态
    public bool IsActive { get; set; } = true; // 是否启用
    
    // 统计信息
    public int UsageCount { get; set; } = 0;   // 处方使用次数
    public DateTime? LastUsedAt { get; set; }   // 最后使用时间
    
    // 导航属性
    public virtual ICollection<HerbPriceHistoryModel> PriceHistories { get; set; } = [];
    public virtual ICollection<PrescriptionItemModel> PrescriptionItems { get; set; } = [];
}

public class HerbPriceHistoryModel : BaseEntity
{
    [Required]
    public Guid HerbId { get; set; }            // 药材ID
    
    [Required]
    public decimal OldPrice { get; set; }       // 原价格
    
    [Required]
    public decimal NewPrice { get; set; }       // 新价格
    
    public decimal ChangeAmount { get; set; }   // 变动金额
    public decimal ChangePercentage { get; set; } // 变动百分比
    
    [MaxLength(200)]
    public string? Reason { get; set; }         // 调价原因
    
    public DateTime ChangeDate { get; set; }    // 调价日期
    public Guid? ChangedBy { get; set; }        // 调价操作人
    
    // 导航属性
    public virtual HerbModel Herb { get; set; }
}

public enum HerbStatus
{
    Available = 0,    // 可用
    OutOfStock = 1,   // 缺货
    Discontinued = 2, // 停用
    Restricted = 3    // 限制使用
}
```

## 🎯 功能规范

### 核心业务功能

#### 1. 药材信息管理
```csharp
// 业务服务实现
public class HerbBusinessService
{
    // 创建药材
    public async Task<ServiceResult<HerbDto>> CreateHerbAsync(HerbCreateDto dto)
    {
        // 检查药材名称重复
        var existing = await _repository.GetByNameAsync(dto.Name);
        if (existing != null)
            return ServiceResult<HerbDto>.Failure($"药材【{dto.Name}】已存在");
        
        // 验证价格合理性
        if (dto.Price <= 0)
            return ServiceResult<HerbDto>.Failure("药材价格必须大于0");
            
        if (dto.Price > 1000)
            return ServiceResult<HerbDto>.Failure("药材价格不能超过1000元/克，请确认价格是否正确");
        
        var herb = _mapper.Map<HerbModel>(dto);
        herb.Status = HerbStatus.Available;
        herb.PriceUpdatedAt = DateTime.Now;
        herb.PriceUpdatedBy = _currentUserService.GetCurrentUserId();
        
        await _repository.CreateAsync(herb);
        var result = _mapper.Map<HerbDto>(herb);
        
        return ServiceResult<HerbDto>.Success(result);
    }
    
    // 更新药材信息
    public async Task<ServiceResult<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto dto)
    {
        var herb = await _repository.GetByIdAsync(id);
        if (herb == null)
            return ServiceResult<HerbDto>.Failure("药材不存在");
        
        // 检查名称重复(排除自己)
        if (herb.Name != dto.Name)
        {
            var existing = await _repository.GetByNameAsync(dto.Name);
            if (existing != null)
                return ServiceResult<HerbDto>.Failure($"药材名称【{dto.Name}】已被使用");
        }
        
        // 更新基本信息
        _mapper.Map(dto, herb);
        herb.UpdateTime = DateTime.Now;
        
        await _repository.UpdateAsync(herb);
        var result = _mapper.Map<HerbDto>(herb);
        
        return ServiceResult<HerbDto>.Success(result);
    }
    
    // 批量更新药材状态
    public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(
        List<Guid> herbIds, HerbStatus status)
    {
        if (!herbIds.Any())
            return ServiceResult<bool>.Failure("请选择需要更新的药材");
        
        var herbs = await _repository.GetByIdsAsync(herbIds);
        if (herbs.Count != herbIds.Count)
            return ServiceResult<bool>.Failure("部分药材不存在");
        
        foreach (var herb in herbs)
        {
            herb.Status = status;
            herb.UpdateTime = DateTime.Now;
        }
        
        await _repository.BatchUpdateAsync(herbs);
        return ServiceResult<bool>.Success(true);
    }
}
```

#### 2. 价格管理服务
```csharp
public class HerbPriceService : IHerbPriceService
{
    // 更新药材价格
    public async Task<ServiceResult<bool>> UpdateHerbPriceAsync(
        Guid herbId, decimal newPrice, string? reason = null)
    {
        var herb = await _herbRepository.GetByIdAsync(herbId);
        if (herb == null)
            return ServiceResult<bool>.Failure("药材不存在");
        
        if (newPrice <= 0)
            return ServiceResult<bool>.Failure("价格必须大于0");
            
        if (newPrice == herb.Price)
            return ServiceResult<bool>.Failure("新价格与当前价格相同");
        
        // 记录价格历史
        var priceHistory = new HerbPriceHistoryModel
        {
            HerbId = herbId,
            OldPrice = herb.Price,
            NewPrice = newPrice,
            ChangeAmount = newPrice - herb.Price,
            ChangePercentage = (newPrice - herb.Price) / herb.Price * 100,
            Reason = reason,
            ChangeDate = DateTime.Now,
            ChangedBy = _currentUserService.GetCurrentUserId()
        };
        
        await _priceHistoryRepository.CreateAsync(priceHistory);
        
        // 更新当前价格
        herb.Price = newPrice;
        herb.PriceUpdatedAt = DateTime.Now;
        herb.PriceUpdatedBy = _currentUserService.GetCurrentUserId();
        
        await _herbRepository.UpdateAsync(herb);
        
        return ServiceResult<bool>.Success(true);
    }
    
    // 批量调价
    public async Task<ServiceResult<int>> BatchUpdatePricesAsync(
        BatchPriceUpdateDto dto)
    {
        var herbs = await _herbRepository.GetByIdsAsync(dto.HerbIds);
        if (!herbs.Any())
            return ServiceResult<int>.Failure("没有找到需要调价的药材");
        
        int updatedCount = 0;
        
        foreach (var herb in herbs)
        {
            decimal newPrice;
            
            if (dto.UpdateType == PriceUpdateType.Percentage)
            {
                newPrice = herb.Price * (1 + dto.Value / 100);
            }
            else
            {
                newPrice = herb.Price + dto.Value;
            }
            
            if (newPrice <= 0)
            {
                _logger.LogWarning($"药材 {herb.Name} 调价后价格为负数或零，跳过");
                continue;
            }
            
            var updateResult = await UpdateHerbPriceAsync(herb.Id, newPrice, dto.Reason);
            if (updateResult.Success)
                updatedCount++;
        }
        
        return ServiceResult<int>.Success(updatedCount);
    }
    
    // 获取价格变动趋势
    public async Task<ServiceResult<List<HerbPriceTrendDto>>> GetPriceTrendAsync(
        Guid herbId, DateTime startDate, DateTime endDate)
    {
        var priceHistories = await _priceHistoryRepository.GetPriceTrendAsync(
            herbId, startDate, endDate);
        
        var trends = priceHistories.Select(ph => new HerbPriceTrendDto
        {
            Date = ph.ChangeDate,
            Price = ph.NewPrice,
            ChangeAmount = ph.ChangeAmount,
            ChangePercentage = ph.ChangePercentage,
            Reason = ph.Reason
        }).ToList();
        
        return ServiceResult<List<HerbPriceTrendDto>>.Success(trends);
    }
}
```

#### 3. 批量数据处理
```csharp
public class HerbImportExportService : IHerbImportExportService
{
    // 导入药材数据
    public async Task<ServiceResult<HerbImportResult>> ImportHerbsFromExcelAsync(
        Stream excelStream)
    {
        var result = new HerbImportResult();
        
        try
        {
            // 解析Excel文件
            var herbs = await ParseExcelToHerbsAsync(excelStream);
            
            foreach (var herbDto in herbs)
            {
                try
                {
                    // 验证数据
                    var validation = ValidateHerbData(herbDto);
                    if (!validation.IsValid)
                    {
                        result.Errors.Add($"第{result.TotalRows + 1}行: {validation.ErrorMessage}");
                        result.TotalRows++;
                        continue;
                    }
                    
                    // 检查是否已存在
                    var existing = await _herbRepository.GetByNameAsync(herbDto.Name);
                    if (existing != null)
                    {
                        if (result.UpdateExisting)
                        {
                            // 更新现有药材
                            _mapper.Map(herbDto, existing);
                            await _herbRepository.UpdateAsync(existing);
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.Warnings.Add($"药材 {herbDto.Name} 已存在，跳过导入");
                        }
                    }
                    else
                    {
                        // 创建新药材
                        var herb = _mapper.Map<HerbModel>(herbDto);
                        await _herbRepository.CreateAsync(herb);
                        result.CreatedCount++;
                    }
                    
                    result.TotalRows++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"处理第{result.TotalRows + 1}行时发生错误: {ex.Message}");
                    result.TotalRows++;
                }
            }
            
            result.Success = true;
            return ServiceResult<HerbImportResult>.Success(result);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"导入过程中发生错误: {ex.Message}");
            return ServiceResult<HerbImportResult>.Failure("导入失败", result);
        }
    }
    
    // 导出药材数据
    public async Task<ServiceResult<Stream>> ExportHerbsToExcelAsync(
        HerbExportCriteria criteria)
    {
        var herbs = await _herbRepository.GetHerbsForExportAsync(criteria);
        
        var exportData = herbs.Select(h => new HerbExportDto
        {
            Name = h.Name,
            EnglishName = h.EnglishName,
            Category = h.Category,
            Nature = h.Nature,
            Flavor = h.Flavor,
            Function = h.Function,
            Price = h.Price,
            Status = h.Status.ToString(),
            UsageCount = h.UsageCount
        }).ToList();
        
        var excelStream = await GenerateExcelStreamAsync(exportData);
        return ServiceResult<Stream>.Success(excelStream);
    }
}
```

### 查询服务专业功能

#### 1. 药材检索和筛选
```csharp
public class HerbQueryService
{
    // 高级搜索
    public async Task<ServiceResult<PagedResult<HerbSearchResultDto>>> SearchHerbsAsync(
        HerbSearchDto criteria)
    {
        var query = _repository.GetQueryable();
        
        // 名称搜索(支持模糊匹配)
        if (!string.IsNullOrEmpty(criteria.Name))
        {
            query = query.Where(h => h.Name.Contains(criteria.Name) || 
                                   h.Alias != null && h.Alias.Contains(criteria.Name));
        }
        
        // 分类过滤
        if (!string.IsNullOrEmpty(criteria.Category))
            query = query.Where(h => h.Category == criteria.Category);
            
        // 性味过滤
        if (!string.IsNullOrEmpty(criteria.Nature))
            query = query.Where(h => h.Nature == criteria.Nature);
            
        if (!string.IsNullOrEmpty(criteria.Flavor))
            query = query.Where(h => h.Flavor == criteria.Flavor);
        
        // 功效搜索
        if (!string.IsNullOrEmpty(criteria.Function))
            query = query.Where(h => h.Function != null && h.Function.Contains(criteria.Function));
        
        // 价格区间过滤
        if (criteria.MinPrice.HasValue)
            query = query.Where(h => h.Price >= criteria.MinPrice.Value);
        if (criteria.MaxPrice.HasValue)
            query = query.Where(h => h.Price <= criteria.MaxPrice.Value);
            
        // 状态过滤
        if (criteria.Status.HasValue)
            query = query.Where(h => h.Status == criteria.Status.Value);
        else
            query = query.Where(h => h.IsActive); // 默认只显示启用的药材
        
        // 使用频次排序
        if (criteria.SortBy == HerbSortBy.UsageCount)
        {
            query = criteria.SortDirection == SortDirection.Descending 
                ? query.OrderByDescending(h => h.UsageCount)
                : query.OrderBy(h => h.UsageCount);
        }
        else if (criteria.SortBy == HerbSortBy.Price)
        {
            query = criteria.SortDirection == SortDirection.Descending 
                ? query.OrderByDescending(h => h.Price)
                : query.OrderBy(h => h.Price);
        }
        else
        {
            query = query.OrderBy(h => h.Name); // 默认按名称排序
        }
        
        var pagedResult = await _repository.GetPagedAsync(query, criteria.Page, criteria.PageSize);
        return ServiceResult<PagedResult<HerbSearchResultDto>>.Success(pagedResult);
    }
    
    // 获取药材分类统计
    public async Task<ServiceResult<List<HerbCategoryStatDto>>> GetCategoryStatisticsAsync()
    {
        var herbs = await _repository.GetActiveHerbsAsync();
        
        var statistics = herbs
            .GroupBy(h => h.Category ?? "未分类")
            .Select(g => new HerbCategoryStatDto
            {
                Category = g.Key,
                Count = g.Count(),
                AveragePrice = g.Average(h => h.Price),
                MaxPrice = g.Max(h => h.Price),
                MinPrice = g.Min(h => h.Price),
                TotalUsage = g.Sum(h => h.UsageCount)
            })
            .OrderByDescending(s => s.Count)
            .ToList();
        
        return ServiceResult<List<HerbCategoryStatDto>>.Success(statistics);
    }
    
    // 获取热门药材排行
    public async Task<ServiceResult<List<HerbUsageRankDto>>> GetPopularHerbsAsync(int topCount = 20)
    {
        var herbs = await _repository.GetTopUsedHerbsAsync(topCount);
        
        var rankings = herbs.Select((h, index) => new HerbUsageRankDto
        {
            Rank = index + 1,
            HerbId = h.Id,
            HerbName = h.Name,
            UsageCount = h.UsageCount,
            LastUsedAt = h.LastUsedAt,
            Price = h.Price
        }).ToList();
        
        return ServiceResult<List<HerbUsageRankDto>>.Success(rankings);
    }
    
    // 价格监控报表
    public async Task<ServiceResult<HerbPriceMonitorDto>> GetPriceMonitoringAsync(
        DateTime startDate, DateTime endDate)
    {
        var priceChanges = await _priceHistoryRepository.GetPriceChangesAsync(startDate, endDate);
        
        var monitoring = new HerbPriceMonitorDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalPriceChanges = priceChanges.Count,
            AveragePriceChangePercentage = priceChanges.Any() ? 
                priceChanges.Average(pc => Math.Abs(pc.ChangePercentage)) : 0,
            
            // 涨价最多的药材
            TopPriceIncreases = priceChanges
                .Where(pc => pc.ChangeAmount > 0)
                .OrderByDescending(pc => pc.ChangePercentage)
                .Take(10)
                .Select(pc => new HerbPriceChangeDto
                {
                    HerbName = pc.Herb.Name,
                    OldPrice = pc.OldPrice,
                    NewPrice = pc.NewPrice,
                    ChangePercentage = pc.ChangePercentage,
                    ChangeDate = pc.ChangeDate
                })
                .ToList(),
                
            // 降价最多的药材
            TopPriceDecreases = priceChanges
                .Where(pc => pc.ChangeAmount < 0)
                .OrderBy(pc => pc.ChangePercentage)
                .Take(10)
                .Select(pc => new HerbPriceChangeDto
                {
                    HerbName = pc.Herb.Name,
                    OldPrice = pc.OldPrice,
                    NewPrice = pc.NewPrice,
                    ChangePercentage = pc.ChangePercentage,
                    ChangeDate = pc.ChangeDate
                })
                .ToList()
        };
        
        return ServiceResult<HerbPriceMonitorDto>.Success(monitoring);
    }
}
```

### 主服务委托层
```csharp
public class HerbService : IHerbService
{
    private readonly HerbQueryService _queryService;
    private readonly HerbBusinessService _businessService;
    
    public HerbService(
        HerbQueryService queryService,
        HerbBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }
    
    // 纯委托实现 - 查询功能
    public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
        
    public async Task<ServiceResult<PagedResult<HerbSearchResultDto>>> SearchHerbsAsync(
        HerbSearchDto criteria)
        => await _queryService.SearchHerbsAsync(criteria);
        
    public async Task<ServiceResult<List<HerbCategoryStatDto>>> GetCategoryStatisticsAsync()
        => await _queryService.GetCategoryStatisticsAsync();
    
    // 纯委托实现 - 业务功能
    public async Task<ServiceResult<HerbDto>> CreateHerbAsync(HerbCreateDto dto)
        => await _businessService.CreateHerbAsync(dto);
        
    public async Task<ServiceResult<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto dto)
        => await _businessService.UpdateHerbAsync(id, dto);
        
    public async Task<ServiceResult<bool>> DeleteHerbAsync(Guid id)
        => await _businessService.DeleteHerbAsync(id);
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
public interface IHerbRepository : IBaseRepository<HerbModel>
{
    Task<HerbModel?> GetByNameAsync(string name);
    Task<List<HerbModel>> GetByIdsAsync(List<Guid> ids);
    Task<List<HerbModel>> GetActiveHerbsAsync();
    Task<List<HerbModel>> GetTopUsedHerbsAsync(int count);
}

// 2. QueryService层 - 查询专业化
public class HerbQueryService
{
    // 专注复杂查询、统计、报表
}

// 3. BusinessService层 - 业务逻辑
public class HerbBusinessService  
{
    // 专注业务流程、CRUD、验证
}

// 4. Service层 - 纯委托
public class HerbService : IHerbService
{
    // 纯委托，无业务逻辑
}
```

### 数据传输对象 (DTOs)
```csharp
// 创建药材DTO
public class HerbCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(100)]
    public string? EnglishName { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    [MaxLength(50)]
    public string? Nature { get; set; }
    
    [MaxLength(50)]
    public string? Flavor { get; set; }
    
    [MaxLength(200)]
    public string? Function { get; set; }
    
    [Required]
    [Range(0.01, 1000)]
    public decimal Price { get; set; }
    
    [MaxLength(100)]
    public string? Usage { get; set; }
}

// 药材搜索DTO
public class HerbSearchDto : PagedRequestDto
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Nature { get; set; }
    public string? Flavor { get; set; }
    public string? Function { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public HerbStatus? Status { get; set; }
    public HerbSortBy SortBy { get; set; } = HerbSortBy.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}

// 批量调价DTO
public class BatchPriceUpdateDto
{
    [Required]
    public List<Guid> HerbIds { get; set; } = [];
    
    [Required]
    public PriceUpdateType UpdateType { get; set; }
    
    [Required]
    public decimal Value { get; set; }  // 百分比或固定金额
    
    public string? Reason { get; set; }
}

public enum PriceUpdateType
{
    Amount = 0,     // 固定金额调整
    Percentage = 1  // 百分比调整
}

public enum HerbSortBy
{
    Name = 0,
    Price = 1,
    UsageCount = 2,
    UpdateTime = 3
}
```

## 🔗 集成接口

### API控制器实现
```csharp
[ApiController]
[ApiVersion("1")]  
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class HerbsController : BaseApiController
{
    private readonly IHerbService _herbService;
    private readonly IHerbPriceService _priceService;
    private readonly IHerbImportExportService _importExportService;
    
    public HerbsController(
        IHerbService herbService,
        IHerbPriceService priceService,
        IHerbImportExportService importExportService,
        ILogger<HerbsController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _herbService = herbService;
        _priceService = priceService;
        _importExportService = importExportService;
    }
    
    /// <summary>
    /// 搜索药材
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<HerbSearchResultDto>>>> SearchHerbs(
        [FromQuery] HerbSearchDto criteria)
    {
        try
        {
            var result = await _herbService.SearchHerbsAsync(criteria);
            return HandleServiceResult(result, "搜索药材成功");
        }
        catch (Exception ex)
        {
            return HandleException<PagedResult<HerbSearchResultDto>>(ex, "搜索药材");
        }
    }
    
    /// <summary>
    /// 创建药材
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<HerbDto>>> CreateHerb([FromBody] HerbCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<HerbDto>(dto);
            if (validation != null) return validation;
            
            var result = await _herbService.CreateHerbAsync(dto);
            return HandleServiceResult(result, "创建药材成功", true);
        }
        catch (Exception ex)
        {
            return HandleException<HerbDto>(ex, "创建药材", dto.Name);
        }
    }
    
    /// <summary>
    /// 更新药材价格
    /// </summary>
    [HttpPut("{id:guid}/price")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePrice(
        Guid id, [FromBody] UpdatePriceDto dto)
    {
        try
        {
            var validation = ValidateGuid<bool>(id, "药材ID");
            if (validation != null) return validation;
            
            var result = await _priceService.UpdateHerbPriceAsync(id, dto.NewPrice, dto.Reason);
            return HandleServiceResult(result, "更新价格成功");
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex, "更新药材价格", id);
        }
    }
    
    /// <summary>
    /// 批量调价
    /// </summary>
    [HttpPost("batch-update-prices")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> BatchUpdatePrices(
        [FromBody] BatchPriceUpdateDto dto)
    {
        try
        {
            var validation = ValidateModel<int>(dto);
            if (validation != null) return validation;
            
            var result = await _priceService.BatchUpdatePricesAsync(dto);
            return HandleServiceResult(result, "批量调价完成");
        }
        catch (Exception ex)
        {
            return HandleException<int>(ex, "批量调价");
        }
    }
    
    /// <summary>
    /// 导入药材数据
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<HerbImportResult>>> ImportHerbs(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<HerbImportResult>.Failure("请选择要导入的文件"));
            
            using var stream = file.OpenReadStream();
            var result = await _importExportService.ImportHerbsFromExcelAsync(stream);
            return HandleServiceResult(result, "导入完成");
        }
        catch (Exception ex)
        {
            return HandleException<HerbImportResult>(ex, "导入药材数据");
        }
    }
    
    /// <summary>
    /// 导出药材数据
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportHerbs([FromBody] HerbExportCriteria criteria)
    {
        try
        {
            var result = await _importExportService.ExportHerbsToExcelAsync(criteria);
            if (!result.Success)
                return BadRequest(ApiResponse<object>.Failure(result.ErrorMessage));
            
            return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                       $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            return HandleSystemException(ex, "导出药材数据");
        }
    }
    
    /// <summary>
    /// 获取分类统计
    /// </summary>
    [HttpGet("category-statistics")]
    public async Task<ActionResult<ApiResponse<List<HerbCategoryStatDto>>>> GetCategoryStatistics()
    {
        try
        {
            var result = await _herbService.GetCategoryStatisticsAsync();
            return HandleServiceResult(result, "获取分类统计成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<HerbCategoryStatDto>>(ex, "获取分类统计");
        }
    }
}
```

### 与其他模块集成

#### 1. Prescriptions集成
```csharp
// Prescriptions模块调用药材信息
public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsForPrescriptionAsync()
{
    var criteria = new HerbSearchDto
    {
        Status = HerbStatus.Available,
        PageSize = 1000 // 获取所有可用药材
    };
    
    return await _herbService.SearchHerbsAsync(criteria);
}

// 更新药材使用统计
public async Task<ServiceResult<bool>> UpdateHerbUsageStatisticsAsync(
    List<Guid> herbIds)
{
    return await _herbService.BatchUpdateUsageCountAsync(herbIds);
}
```

#### 2. Formula集成
```csharp
// Formula模块获取验方相关药材信息
public async Task<ServiceResult<List<HerbDto>>> GetHerbsForFormulaAsync(List<Guid> herbIds)
{
    return await _herbService.GetHerbsByIdsAsync(herbIds);
}
```

## ⚙️ 配置管理

### 药材管理配置选项
```csharp
public class HerbOptions
{
    public const string SectionName = "Herb";
    
    /// <summary>
    /// 最大单价(元/克)
    /// </summary>
    public decimal MaxPrice { get; set; } = 1000;
    
    /// <summary>
    /// 价格变动预警阈值(百分比)
    /// </summary>
    public decimal PriceChangeWarningThreshold { get; set; } = 20;
    
    /// <summary>
    /// 导入批次大小
    /// </summary>
    public int ImportBatchSize { get; set; } = 100;
    
    /// <summary>
    /// 支持的导入文件格式
    /// </summary>
    public List<string> SupportedImportFormats { get; set; } = [".xlsx", ".xls"];
    
    /// <summary>
    /// 启用价格历史记录
    /// </summary>
    public bool EnablePriceHistory { get; set; } = true;
    
    /// <summary>
    /// 默认药材分类
    /// </summary>
    public List<string> DefaultCategories { get; set; } = 
    [
        "解表药", "清热药", "泻下药", "祛风湿药", "芳香化湿药",
        "利水渗湿药", "温里药", "理气药", "消食药", "驱虫药",
        "止血药", "活血化瘀药", "化痰止咳平喘药", "安神药",
        "平肝息风药", "开窍药", "补虚药", "收涩药", "涌吐药", "外用药"
    ];
}
```

### 应用配置
```json
{
  "Herb": {
    "MaxPrice": 1000,
    "PriceChangeWarningThreshold": 20,
    "ImportBatchSize": 100,
    "SupportedImportFormats": [".xlsx", ".xls"],
    "EnablePriceHistory": true,
    "DefaultCategories": [
      "解表药", "清热药", "泻下药", "祛风湿药"
    ]
  },
  "Logging": {
    "LogLevel": {
      "LYBT.Module.Herbs": "Information"
    }
  }
}
```

## 🧪 测试规范

### 单元测试要求

#### 1. 业务服务测试
```csharp
[Test]
public async Task CreateHerbAsync_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var dto = new HerbCreateDto
    {
        Name = "当归",
        Category = "补虚药",
        Nature = "温",
        Flavor = "甘、辛",
        Function = "补血活血，调经止痛",
        Price = 0.5m
    };
    
    _herbRepositoryMock
        .Setup(x => x.GetByNameAsync(dto.Name))
        .ReturnsAsync((HerbModel?)null);
    
    // Act
    var result = await _businessService.CreateHerbAsync(dto);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.Name, Is.EqualTo(dto.Name));
}

[Test]
public async Task UpdateHerbPriceAsync_PriceIncrease_RecordsHistory()
{
    // Arrange
    var herbId = Guid.NewGuid();
    var herb = new HerbModel { Id = herbId, Name = "人参", Price = 2.0m };
    var newPrice = 2.5m;
    
    _herbRepositoryMock
        .Setup(x => x.GetByIdAsync(herbId))
        .ReturnsAsync(herb);
    
    // Act
    var result = await _priceService.UpdateHerbPriceAsync(herbId, newPrice, "市场价格上涨");
    
    // Assert
    Assert.That(result.Success, Is.True);
    _priceHistoryRepositoryMock.Verify(x => 
        x.CreateAsync(It.Is<HerbPriceHistoryModel>(ph => 
            ph.HerbId == herbId && 
            ph.NewPrice == newPrice && 
            ph.ChangeAmount == 0.5m)), Times.Once);
}
```

#### 2. 查询服务测试
```csharp
[Test]
public async Task SearchHerbsAsync_WithCategoryFilter_ReturnsFilteredResults()
{
    // Arrange
    var criteria = new HerbSearchDto
    {
        Category = "补虚药",
        Page = 1,
        PageSize = 10
    };
    
    var herbs = new List<HerbModel>
    {
        new() { Name = "人参", Category = "补虚药" },
        new() { Name = "当归", Category = "补虚药" }
    };
    
    _herbRepositoryMock
        .Setup(x => x.GetPagedAsync(It.IsAny<IQueryable<HerbModel>>(), 1, 10))
        .ReturnsAsync(new PagedResult<HerbSearchResultDto>(2, []));
    
    // Act
    var result = await _queryService.SearchHerbsAsync(criteria);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.TotalCount, Is.EqualTo(2));
}
```

### 集成测试
```csharp
[Test]
public async Task HerbManagement_CompleteWorkflow_Success()
{
    // 1. 创建药材
    var createDto = new HerbCreateDto
    {
        Name = "黄芪",
        Category = "补虚药",
        Price = 0.3m
    };
    var createResult = await _herbService.CreateHerbAsync(createDto);
    Assert.That(createResult.Success, Is.True);
    
    // 2. 更新价格
    var updateResult = await _priceService.UpdateHerbPriceAsync(
        createResult.Data.Id, 0.35m, "季节性涨价");
    Assert.That(updateResult.Success, Is.True);
    
    // 3. 搜索验证
    var searchResult = await _herbService.SearchHerbsAsync(
        new HerbSearchDto { Name = "黄芪" });
    Assert.That(searchResult.Success, Is.True);
    Assert.That(searchResult.Data.Items.First().Price, Is.EqualTo(0.35m));
}
```

## 🚀 部署说明

### 数据库迁移
```bash
# Herb模块相关迁移
dotnet ef migrations add AddHerbModule --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### 配置检查清单
- [ ] HerbOptions配置正确
- [ ] 数据库连接字符串有效
- [ ] 导入导出文件路径权限正确
- [ ] AutoMapper映射配置完整
- [ ] 价格历史记录功能启用
- [ ] 日志记录级别适当
- [ ] 缓存策略配置合理

## 📚 相关文档

### 架构文档
- [UltraThink双层架构标准](../../architecture/ultrathink-dual-layer-architecture.md)
- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)  
- [API响应标准](../../architecture/ultrathink-api-response-standards-20250817.md)

### 业务文档
- [Prescriptions模块文档](./prescriptions.md) - 处方用药集成
- [Formula模块文档](./formula.md) - 验方药材关联
- [用药统计分析报告](../../reports/herb-usage-analysis-report.md)

### 开发指南
- [模块开发规范](../../development/MODULE_DEVELOPMENT_STANDARDS.md)
- [数据导入导出指南](../../development/DATA_IMPORT_EXPORT_GUIDE.md)
- [测试指南](../../testing/MODULE_TESTING_GUIDE.md)
- [部署指南](../../deployment/MODULE_DEPLOYMENT_GUIDE.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 后端开发组