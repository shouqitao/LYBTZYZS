# Server端验方管理开发指南

> **文档版本**: v1.0
> **最后更新**: 2025-01-30
> **适用范围**: LYBT Server - LYBT.Module.Formula模块
> **前置阅读**: `docs/explanation/architecture/server/formula-design.md`

---

## 1. 概述

### 1.1 模块定位

Formula模块是Server端的**验方模板管理模块**，负责中医验方的全生命周期管理。

**核心职责**:
- **验方模板管理**: 创建、修改、删除验方定义
- **药材组成管理**: 管理验方中的药材列表（FormulaHerbItem）
- **延迟绑定验证**: 导入时HerbId可空，后续人工匹配
- **Excel导入导出**: Sheet1验方+Sheet2药材的主从导入/导出
- **自动匹配**: 导入时自动匹配药材库
- **批量操作**: 批量删除（最大100条/次）
- **权限控制**: 验方共享（IsShared）与用户隔离

**架构定位**:
```
Controller层 (FormulaController)
    ↓ 14个API端点
Service层 (FormulaService)
    ↓ 14个业务方法
Repository层 (FormulaRepository)
    ↓ 7个数据方法
Database (Entity Framework Core 8)
```

### 1.2 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 框架基础 |
| ASP.NET Core | 8.0 | Web API框架 |
| Entity Framework Core | 8.0 | ORM框架 |
| EPPlus | 7.5+ | Excel导入导出 |
| Swashbuckle | 6.x | Swagger文档 |
| AutoMapper | 12.x | DTO映射 |
| SQL Server | 2022 | 数据库 |

### 1.3 延迟绑定原则

**核心设计原则**（Issue #1348）:

1. **导入阶段宽松**: 允许HerbId=null，保存OriginalHerbName
2. **自动匹配优先**: TryMatchHerbAsync尝试自动绑定
3. **人工校验补充**: 未匹配的药材通过FormulaValidationViewModel手动绑定
4. **完整性验证**: 只有所有药材IsValidated=true，验方状态才能从Draft→Validated

**代码示例**:
```csharp
// ✅ 正确：延迟绑定允许先保存原始名称
var herbItem = new FormulaHerbItem
{
    HerbId = matchedHerb?.Id, // 可空，自动匹配成功则填充
    OriginalHerbName = "当归", // 保留原始名称
    HerbName = matchedHerb?.Name ?? "当归",
    IsValidated = matchedHerb != null, // 标记是否已验证
    Quantity = 10
};
```

---

## 2. 三层架构实践

### 2.1 三层职责划分

**Controller层（FormulaController）**:
- ✅ HTTP请求处理
- ✅ 参数验证（ModelState）
- ✅ HTTP响应封装（Ok、BadRequest、CreatedAtAction）
- ✅ Swagger注解（SwaggerOperation）
- ❌ 禁止业务逻辑
- ❌ 禁止直接访问Repository

**Service层（FormulaService）**:
- ✅ 业务逻辑实现
- ✅ DTO与Entity映射（AutoMapper）
- ✅ 事务管理（_unitOfWork.SaveChangesAsync）
- ✅ Excel导入导出逻辑
- ✅ 延迟绑定验证逻辑
- ❌ 禁止HTTP相关代码
- ❌ 禁止直接访问DbContext

**Repository层（FormulaRepository）**:
- ✅ 数据库查询（LINQ）
- ✅ Include策略（避免N+1）
- ✅ 软删除过滤
- ✅ 分页查询
- ❌ 禁止业务逻辑
- ❌ 禁止DTO操作

### 2.2 依赖注入配置

**Program.cs**:
```csharp
// 注册Repository
builder.Services.AddScoped<IFormulaRepository, FormulaRepository>();

// 注册Service
builder.Services.AddScoped<IFormulaService, FormulaService>();

// 注册AutoMapper
builder.Services.AddAutoMapper(typeof(FormulaMappingProfile));

// 注册DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));
```

### 2.3 AutoMapper配置

**FormulaMappingProfile.cs**:
```csharp
public class FormulaMappingProfile : Profile
{
    public FormulaMappingProfile()
    {
        // Formula映射
        CreateMap<Formula, FormulaDto>()
            .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs));

        CreateMap<FormulaInputDto, Formula>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ValidationStatus, opt => opt.MapFrom(src => FormulaValidationStatus.Draft))
            .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs));

        CreateMap<FormulaInputDto, Formula>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs));

        // FormulaHerbItem映射
        CreateMap<FormulaHerbItem, FormulaHerbItemDto>();
        CreateMap<FormulaHerbItemInputDto, FormulaHerbItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsValidated, opt => opt.MapFrom(src => src.HerbId != null));
        CreateMap<FormulaHerbItemInputDto, FormulaHerbItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}
```

---

## 3. Entity层设计

### 3.1 Formula实体

```csharp
/// <summary>
/// 验方实体 - 验方为模板，不含价格计算，只定义药材组成和剂量
/// </summary>
[Table("Formulas")]
public class Formula : BaseEntity
{
    /// <summary>验方名称</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>功效</summary>
    [StringLength(500)]
    public string? Effect { get; set; }

    /// <summary>用法</summary>
    [StringLength(500)]
    public string? Usage { get; set; }

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>性味归经</summary>
    [StringLength(200)]
    public string? Property { get; set; }

    /// <summary>验方状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>是否共享</summary>
    public bool IsShared { get; set; } = false;

    /// <summary>验证状态 - Draft=草稿/未验证，Validated=已验证</summary>
    public FormulaValidationStatus ValidationStatus { get; set; } = FormulaValidationStatus.Draft;

    /// <summary>方剂分类</summary>
    [StringLength(50)]
    public string? Category { get; set; }

    /// <summary>方剂类型（经典方/经验方）</summary>
    public FormulaType FormulaType { get; set; } = FormulaType.Experience;

    /// <summary>创建用户ID</summary>
    public Guid? UserId { get; set; }

    /// <summary>药材组成（1:N关系）</summary>
    public List<FormulaHerbItem> Herbs { get; set; } = new();
}
```

**BaseEntity继承**:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

### 3.2 FormulaHerbItem实体（延迟绑定支持）

```csharp
/// <summary>
/// 验方明细 - 验方中的药材组成，支持延迟绑定
/// </summary>
[Table("FormulaHerbItems")]
public class FormulaHerbItem
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>所属验方ID</summary>
    public Guid FormulaId { get; set; }

    /// <summary>关联的验方实体</summary>
    [ForeignKey("FormulaId")]
    public Formula? Formula { get; set; }

    /// <summary>药材ID（可空，支持延迟绑定）</summary>
    public Guid? HerbId { get; set; }

    /// <summary>原始药材名称（从老系统导入时保存，用于延迟绑定）</summary>
    [StringLength(100)]
    public string? OriginalHerbName { get; set; }

    /// <summary>是否已验证绑定（true表示HerbId已绑定到药材库，默认false）</summary>
    public bool IsValidated { get; set; } = false;

    /// <summary>药材名称</summary>
    [Required]
    [StringLength(100)]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>剂量（整数）</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>单位（默认"g"）</summary>
    [StringLength(16)]
    public string Unit { get; set; } = "g";

    /// <summary>用法说明（该药材的特殊用法）</summary>
    [StringLength(200)]
    public string? Usage { get; set; }

    /// <summary>炮制方法</summary>
    [StringLength(100)]
    public string? ProcessingMethod { get; set; }
}
```

### 3.3 枚举定义

```csharp
/// <summary>验方验证状态枚举</summary>
public enum FormulaValidationStatus
{
    /// <summary>草稿/未验证</summary>
    Draft = 1,
    /// <summary>已验证</summary>
    Validated = 2
}

/// <summary>方剂类型枚举</summary>
public enum FormulaType
{
    /// <summary>经典方</summary>
    Classic = 1,
    /// <summary>经验方</summary>
    Experience = 2
}
```

### 3.4 EF Core配置

**FormulaEntityConfiguration.cs**:
```csharp
public class FormulaEntityConfiguration : IEntityTypeConfiguration<Formula>
{
    public void Configure(EntityTypeBuilder<Formula> builder)
    {
        builder.ToTable("Formulas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Effect)
            .HasMaxLength(500);

        builder.Property(f => f.Usage)
            .HasMaxLength(500);

        builder.Property(f => f.Remark)
            .HasMaxLength(500);

        builder.Property(f => f.Property)
            .HasMaxLength(200);

        builder.Property(f => f.Category)
            .HasMaxLength(50);

        builder.Property(f => f.ValidationStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.FormulaType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(f => f.IsShared)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.Status)
            .HasConversion<int>()
            .IsRequired();

        // 1:N关系配置
        builder.HasMany(f => f.Herbs)
            .WithOne(h => h.Formula)
            .HasForeignKey(h => h.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);

        // 软删除全局过滤器
        builder.HasQueryFilter(f => !f.IsDeleted);

        // 索引
        builder.HasIndex(f => f.Name);
        builder.HasIndex(f => f.Category);
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.ValidationStatus);
    }
}
```

**FormulaHerbItemEntityConfiguration.cs**:
```csharp
public class FormulaHerbItemEntityConfiguration : IEntityTypeConfiguration<FormulaHerbItem>
{
    public void Configure(EntityTypeBuilder<FormulaHerbItem> builder)
    {
        builder.ToTable("FormulaHerbItems");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.HerbId)
            .IsRequired(false); // 可空，支持延迟绑定

        builder.Property(h => h.OriginalHerbName)
            .HasMaxLength(100);

        builder.Property(h => h.IsValidated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(h => h.HerbName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Quantity)
            .IsRequired();

        builder.Property(h => h.Unit)
            .IsRequired()
            .HasMaxLength(16)
            .HasDefaultValue("g");

        builder.Property(h => h.Usage)
            .HasMaxLength(200);

        builder.Property(h => h.ProcessingMethod)
            .HasMaxLength(100);

        // 索引
        builder.HasIndex(h => h.FormulaId);
        builder.HasIndex(h => h.HerbId);
        builder.HasIndex(h => h.IsValidated);
    }
}
```

---

## 4. Repository层实现

### 4.1 IFormulaRepository接口

```csharp
public interface IFormulaRepository : IRepository<Formula>
{
    /// <summary>获取启用的验方模板</summary>
    Task<List<Formula>> GetTemplatesAsync();

    /// <summary>根据ID获取验方（包含药材组成）</summary>
    Task<Formula?> GetByIdWithHerbsAsync(Guid id);

    /// <summary>获取分页列表（支持Name+Effect搜索）</summary>
    Task<PagedResult<Formula>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null);

    /// <summary>根据用户ID和权限获取验方列表（自己的+共享的）</summary>
    Task<List<Formula>> GetByUserIdAsync(Guid userId);

    /// <summary>获取共享的验方列表</summary>
    Task<List<Formula>> GetSharedFormulasAsync();

    /// <summary>根据类别获取验方列表</summary>
    Task<List<Formula>> GetByCategoryAsync(string category);

    /// <summary>获取待验证的验方列表（Draft状态）</summary>
    Task<List<Formula>> GetPendingValidationFormulasAsync();
}
```

### 4.2 FormulaRepository实现

```csharp
public class FormulaRepository : Repository<Formula>, IFormulaRepository
{
    public FormulaRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>统一的查询方法 - Eager load + 软删除过滤</summary>
    private IQueryable<Formula> GetBaseQuery()
    {
        return _dbSet
            .Include(f => f.Herbs) // Eager load herbs，避免N+1查询
            .Where(f => !f.IsDeleted); // 软删除过滤
    }

    public async Task<List<Formula>> GetTemplatesAsync()
    {
        return await GetBaseQuery()
            .Where(f => f.Status == CommonStatus.Enabled)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<Formula?> GetByIdWithHerbsAsync(Guid id)
    {
        return await GetBaseQuery()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<PagedResult<Formula>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null)
    {
        var query = GetBaseQuery();

        // 关键词搜索（Name + Effect）
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(f =>
                f.Name.Contains(keyword) ||
                (f.Effect != null && f.Effect.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Formula>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Formula>> GetByUserIdAsync(Guid userId)
    {
        return await GetBaseQuery()
            .Where(f => f.UserId == userId || f.IsShared) // 自己的 + 共享的
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Formula>> GetSharedFormulasAsync()
    {
        return await GetBaseQuery()
            .Where(f => f.IsShared)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<Formula>> GetByCategoryAsync(string category)
    {
        return await GetBaseQuery()
            .Where(f => f.Category == category)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<Formula>> GetPendingValidationFormulasAsync()
    {
        return await GetBaseQuery()
            .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }
}
```

---

## 5. Service层实现

### 5.1 IFormulaService接口

```csharp
public interface IFormulaService
{
    // ========== 基础CRUD ==========
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaInputDto dto);
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaInputDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

    // ========== 查询方法 ==========
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? category = null);
    Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
    Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync();

    // ========== Excel导入导出 ==========
    Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(
        Stream stream,
        string? fileName = null);
    Task<ServiceResult<byte[]>> ExportAsync(List<Guid>? formulaIds = null);
    ServiceResult<byte[]> GenerateImportTemplate();

    // ========== 验证与克隆 ==========
    Task<ServiceResult> ValidateFormulaHerbAsync(
        Guid formulaId,
        Guid herbItemId,
        Guid selectedHerbId);
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid sourceId);
}
```

### 5.2 FormulaService实现（核心方法）

```csharp
public class FormulaService : IFormulaService
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IHerbRepository _herbRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<FormulaService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public FormulaService(
        IFormulaRepository formulaRepository,
        IHerbRepository herbRepository,
        IMapper mapper,
        ILogger<FormulaService> logger,
        IUnitOfWork unitOfWork)
    {
        _formulaRepository = formulaRepository;
        _herbRepository = herbRepository;
        _mapper = mapper;
        _logger = logger;
        _unitOfWork = unitOfWork;

        // 设置EPPlus非商业许可证
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaInputDto dto)
    {
        try
        {
            var formula = _mapper.Map<Formula>(dto);
            formula.ValidationStatus = FormulaValidationStatus.Draft; // 初始状态

            await _formulaRepository.AddAsync(formula);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<FormulaDto>(formula);
            return ServiceResult<FormulaDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建验方失败: {Name}", dto.Name);
            return ServiceResult<FormulaDto>.Failure($"创建验方失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var formula = await _formulaRepository.GetByIdWithHerbsAsync(id);
            if (formula == null)
                return ServiceResult<FormulaDto>.Failure("验方不存在");

            var dto = _mapper.Map<FormulaDto>(formula);
            return ServiceResult<FormulaDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询验方失败: {Id}", id);
            return ServiceResult<FormulaDto>.Failure($"查询验方失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaInputDto dto)
    {
        try
        {
            var formula = await _formulaRepository.GetByIdWithHerbsAsync(id);
            if (formula == null)
                return ServiceResult<FormulaDto>.Failure("验方不存在");

            // 更新基本信息
            formula.Name = dto.Name;
            formula.Effect = dto.Effect;
            formula.Usage = dto.Usage;
            formula.Property = dto.Property;
            formula.Remark = dto.Remark;
            formula.Category = dto.Category;
            formula.IsShared = dto.IsShared;

            // 更新药材组成
            formula.Herbs.Clear();
            foreach (var herbDto in dto.Herbs)
            {
                var herbItem = _mapper.Map<FormulaHerbItem>(herbDto);
                herbItem.FormulaId = formula.Id;
                formula.Herbs.Add(herbItem);
            }

            // 检查验证状态
            if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
            {
                formula.ValidationStatus = FormulaValidationStatus.Validated;
            }
            else
            {
                formula.ValidationStatus = FormulaValidationStatus.Draft;
            }

            await _formulaRepository.UpdateAsync(formula);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<FormulaDto>(formula);
            return ServiceResult<FormulaDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新验方失败: {Id}", id);
            return ServiceResult<FormulaDto>.Failure($"更新验方失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var formula = await _formulaRepository.GetByIdAsync(id);
            if (formula == null)
                return ServiceResult.Failure("验方不存在");

            // 软删除
            formula.IsDeleted = true;
            await _formulaRepository.UpdateAsync(formula);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除验方失败: {Id}", id);
            return ServiceResult.Failure($"删除验方失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? category = null)
    {
        try
        {
            var pagedResult = await _formulaRepository.GetPagedWithDetailsAsync(
                pageNumber,
                pageSize,
                keyword);

            // Category在内存中过滤
            var items = pagedResult.Items;
            if (!string.IsNullOrWhiteSpace(category))
            {
                items = items.Where(f => f.Category == category).ToList();
            }

            var dtos = _mapper.Map<List<FormulaDto>>(items);

            return ServiceResult<PagedResult<FormulaDto>>.Success(new PagedResult<FormulaDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询验方失败");
            return ServiceResult<PagedResult<FormulaDto>>.Failure($"分页查询失败: {ex.Message}");
        }
    }
}
```

### 5.3 批量删除实现

```csharp
public async Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
{
    // Step 1: 验证批量大小
    if (ids == null || !ids.Any())
        return ServiceResult<BatchOperationResultDto>.Failure("删除ID列表不能为空");

    if (ids.Count > 100)
        return ServiceResult<BatchOperationResultDto>.Failure("单次批量删除不能超过100条记录");

    var result = new BatchOperationResultDto();

    // Step 2: 逐个删除（软删除）
    foreach (var id in ids)
    {
        try
        {
            var formula = await _formulaRepository.GetByIdAsync(id);
            if (formula == null)
            {
                result.FailedCount++;
                result.Errors.Add($"验方 {id} 不存在");
                continue;
            }

            formula.IsDeleted = true;
            await _formulaRepository.UpdateAsync(formula);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除验方时发生异常: {FormulaId}", id);
            result.FailedCount++;
            result.Errors.Add($"删除验方 {id} 失败: {ex.Message}");
        }
    }

    await _unitOfWork.SaveChangesAsync();

    return ServiceResult<BatchOperationResultDto>.Success(result);
}
```

---

## 6. Excel导入导出实现

### 6.1 导入流程（主从表格式）

**ImportFromExcelAsync实现**:
```csharp
public async Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(
    Stream stream,
    string? fileName = null)
{
    try
    {
        var result = new FormulaImportResultDto
        {
            StartTime = DateTime.Now
        };

        // Step 1: 打开Excel文件
        using var package = new ExcelPackage(stream);
        var formulaSheet = package.Workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("验方") || ws.Index == 0);
        var herbSheet = package.Workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("药材") || ws.Index == 1);

        if (formulaSheet == null)
            return ServiceResult<FormulaImportResultDto>.Failure("未找到验方Sheet");

        // Step 2: 解析药材组成（按验方编号分组）
        var herbItemsByFormulaCode = new Dictionary<string, List<HerbItemImport>>();
        if (herbSheet != null)
        {
            herbItemsByFormulaCode = ParseHerbItems(herbSheet);
        }

        // Step 3: 逐行导入验方
        int formulaRowCount = formulaSheet.Dimension?.Rows ?? 0;

        for (int row = 2; row <= formulaRowCount; row++)
        {
            try
            {
                var formulaCode = ParseCellValue(formulaSheet.Cells[row, 1]);
                if (string.IsNullOrWhiteSpace(formulaCode))
                    continue;

                var formula = new Formula
                {
                    Name = ParseCellValue(formulaSheet.Cells[row, 2]),
                    Effect = ParseCellValue(formulaSheet.Cells[row, 3]),
                    Usage = ParseCellValue(formulaSheet.Cells[row, 4]),
                    Property = ParseCellValue(formulaSheet.Cells[row, 5]),
                    Remark = ParseCellValue(formulaSheet.Cells[row, 6]),
                    IsShared = bool.TryParse(ParseCellValue(formulaSheet.Cells[row, 7]), out var isShared) && isShared,
                    Category = ParseCellValue(formulaSheet.Cells[row, 8]),
                    ValidationStatus = FormulaValidationStatus.Draft,
                    Herbs = new List<FormulaHerbItem>()
                };

                // Step 4: 匹配药材
                if (herbItemsByFormulaCode.TryGetValue(formulaCode, out var herbItems))
                {
                    foreach (var herbItem in herbItems)
                    {
                        var matchedHerb = await TryMatchHerbAsync(herbItem.HerbName);

                        formula.Herbs.Add(new FormulaHerbItem
                        {
                            HerbId = matchedHerb?.Id, // Nullable延迟绑定
                            HerbName = matchedHerb?.Name ?? herbItem.HerbName,
                            OriginalHerbName = herbItem.HerbName, // 保存原始名称
                            IsValidated = matchedHerb != null,
                            Quantity = herbItem.Quantity,
                            Unit = herbItem.Unit ?? "g",
                            ProcessingMethod = herbItem.ProcessingMethod,
                            Usage = herbItem.Usage
                        });

                        if (matchedHerb != null)
                            result.MatchedHerbsCount++;
                        else
                            result.UnmatchedHerbsCount++;
                    }
                }

                // Step 5: 自动验证完整性
                if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
                {
                    formula.ValidationStatus = FormulaValidationStatus.Validated;
                }

                await _formulaRepository.AddAsync(formula);
                result.SuccessCount++;
                result.SuccessfulFormulas.Add(_mapper.Map<FormulaDto>(formula));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入第{Row}行时发生异常", row);
                result.FailedCount++;
                result.FailedItems.Add(new FormulaImportErrorDto
                {
                    RowNumber = row,
                    ErrorMessage = ex.Message
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        result.EndTime = DateTime.Now;
        return ServiceResult<FormulaImportResultDto>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入验方失败");
        return ServiceResult<FormulaImportResultDto>.Failure($"导入失败: {ex.Message}");
    }
}
```

### 6.2 自动匹配药材

```csharp
/// <summary>
/// 尝试自动匹配药材库
/// </summary>
private async Task<Herb?> TryMatchHerbAsync(string herbName)
{
    try
    {
        if (string.IsNullOrWhiteSpace(herbName))
            return null;

        // 精确匹配
        var herb = await _herbRepository.GetByNameAsync(herbName);
        if (herb != null)
            return herb;

        // 模糊匹配（去除括号、空格等干扰）
        var cleanedName = herbName.Replace("(", "").Replace(")", "").Trim();
        herb = await _herbRepository.GetByNameAsync(cleanedName);
        if (herb != null)
            return herb;

        // 前缀匹配（如"人参10g"匹配"人参"）
        var allHerbs = await _herbRepository.GetAllAsync();
        herb = allHerbs.FirstOrDefault(h => cleanedName.StartsWith(h.Name) || h.Name.StartsWith(cleanedName));

        return herb;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "匹配药材时发生异常: {HerbName}", herbName);
        return null;
    }
}
```

### 6.3 解析药材Sheet

```csharp
/// <summary>
/// 解析药材Sheet（按验方编号分组）
/// </summary>
private Dictionary<string, List<HerbItemImport>> ParseHerbItems(ExcelWorksheet herbSheet)
{
    var result = new Dictionary<string, List<HerbItemImport>>();

    int herbRowCount = herbSheet.Dimension?.Rows ?? 0;

    for (int row = 2; row <= herbRowCount; row++)
    {
        var formulaCode = ParseCellValue(herbSheet.Cells[row, 1]);
        if (string.IsNullOrWhiteSpace(formulaCode))
            continue;

        var herbItem = new HerbItemImport
        {
            HerbName = ParseCellValue(herbSheet.Cells[row, 2]),
            Quantity = int.TryParse(ParseCellValue(herbSheet.Cells[row, 3]), out var qty) ? qty : 1,
            Unit = ParseCellValue(herbSheet.Cells[row, 4]) ?? "g",
            ProcessingMethod = ParseCellValue(herbSheet.Cells[row, 5]),
            Usage = ParseCellValue(herbSheet.Cells[row, 6])
        };

        if (!result.ContainsKey(formulaCode))
            result[formulaCode] = new List<HerbItemImport>();

        result[formulaCode].Add(herbItem);
    }

    return result;
}

/// <summary>
/// 解析Excel单元格值
/// </summary>
private string? ParseCellValue(ExcelRangeBase cell)
{
    if (cell == null || cell.Value == null)
        return null;

    return cell.Value.ToString()?.Trim();
}
```

### 6.4 导出Excel实现

```csharp
public async Task<ServiceResult<byte[]>> ExportAsync(List<Guid>? formulaIds = null)
{
    try
    {
        using var package = new ExcelPackage();

        // Sheet1: 验方基本信息
        var formulaSheet = package.Workbook.Worksheets.Add("验方");
        formulaSheet.Cells[1, 1].Value = "验方编号";
        formulaSheet.Cells[1, 2].Value = "验方名称";
        formulaSheet.Cells[1, 3].Value = "功效";
        formulaSheet.Cells[1, 4].Value = "用法";
        formulaSheet.Cells[1, 5].Value = "性味归经";
        formulaSheet.Cells[1, 6].Value = "备注";
        formulaSheet.Cells[1, 7].Value = "是否共享";
        formulaSheet.Cells[1, 8].Value = "分类";

        // Sheet2: 药材组成
        var herbSheet = package.Workbook.Worksheets.Add("药材");
        herbSheet.Cells[1, 1].Value = "验方编号";
        herbSheet.Cells[1, 2].Value = "药材名称";
        herbSheet.Cells[1, 3].Value = "用量";
        herbSheet.Cells[1, 4].Value = "单位";
        herbSheet.Cells[1, 5].Value = "炮制方法";
        herbSheet.Cells[1, 6].Value = "用法说明";

        // 获取验方数据
        List<Formula> formulas;
        if (formulaIds != null && formulaIds.Any())
        {
            formulas = new List<Formula>();
            foreach (var id in formulaIds)
            {
                var formula = await _formulaRepository.GetByIdWithHerbsAsync(id);
                if (formula != null)
                    formulas.Add(formula);
            }
        }
        else
        {
            formulas = await _formulaRepository.GetAllAsync();
        }

        // 填充验方数据
        int formulaRow = 2;
        foreach (var formula in formulas)
        {
            var formulaCode = $"F{formula.Id.ToString().Substring(0, 8)}";

            formulaSheet.Cells[formulaRow, 1].Value = formulaCode;
            formulaSheet.Cells[formulaRow, 2].Value = formula.Name;
            formulaSheet.Cells[formulaRow, 3].Value = formula.Effect;
            formulaSheet.Cells[formulaRow, 4].Value = formula.Usage;
            formulaSheet.Cells[formulaRow, 5].Value = formula.Property;
            formulaSheet.Cells[formulaRow, 6].Value = formula.Remark;
            formulaSheet.Cells[formulaRow, 7].Value = formula.IsShared;
            formulaSheet.Cells[formulaRow, 8].Value = formula.Category;

            // 填充药材数据
            foreach (var herb in formula.Herbs)
            {
                int herbRow = herbSheet.Dimension?.Rows + 1 ?? 2;

                herbSheet.Cells[herbRow, 1].Value = formulaCode;
                herbSheet.Cells[herbRow, 2].Value = herb.HerbName;
                herbSheet.Cells[herbRow, 3].Value = herb.Quantity;
                herbSheet.Cells[herbRow, 4].Value = herb.Unit;
                herbSheet.Cells[herbRow, 5].Value = herb.ProcessingMethod;
                herbSheet.Cells[herbRow, 6].Value = herb.Usage;
            }

            formulaRow++;
        }

        // 自动调整列宽
        formulaSheet.Cells.AutoFitColumns();
        herbSheet.Cells.AutoFitColumns();

        return ServiceResult<byte[]>.Success(package.GetAsByteArray());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导出验方失败");
        return ServiceResult<byte[]>.Failure($"导出失败: {ex.Message}");
    }
}
```

### 6.5 生成导入模板

```csharp
public ServiceResult<byte[]> GenerateImportTemplate()
{
    try
    {
        using var package = new ExcelPackage();

        // Sheet1: 验方模板
        var formulaSheet = package.Workbook.Worksheets.Add("验方");
        formulaSheet.Cells[1, 1].Value = "验方编号";
        formulaSheet.Cells[1, 2].Value = "验方名称";
        formulaSheet.Cells[1, 3].Value = "功效";
        formulaSheet.Cells[1, 4].Value = "用法";
        formulaSheet.Cells[1, 5].Value = "性味归经";
        formulaSheet.Cells[1, 6].Value = "备注";
        formulaSheet.Cells[1, 7].Value = "是否共享";
        formulaSheet.Cells[1, 8].Value = "分类";

        // 示例数据
        formulaSheet.Cells[2, 1].Value = "F001";
        formulaSheet.Cells[2, 2].Value = "小柴胡汤";
        formulaSheet.Cells[2, 3].Value = "和解少阳，疏肝和胃";
        formulaSheet.Cells[2, 4].Value = "水煎服，每日一剂";
        formulaSheet.Cells[2, 5].Value = "微寒";
        formulaSheet.Cells[2, 6].Value = "经方";
        formulaSheet.Cells[2, 7].Value = "TRUE";
        formulaSheet.Cells[2, 8].Value = "和解剂";

        // Sheet2: 药材模板
        var herbSheet = package.Workbook.Worksheets.Add("药材");
        herbSheet.Cells[1, 1].Value = "验方编号";
        herbSheet.Cells[1, 2].Value = "药材名称";
        herbSheet.Cells[1, 3].Value = "用量";
        herbSheet.Cells[1, 4].Value = "单位";
        herbSheet.Cells[1, 5].Value = "炮制方法";
        herbSheet.Cells[1, 6].Value = "用法说明";

        // 示例数据
        herbSheet.Cells[2, 1].Value = "F001";
        herbSheet.Cells[2, 2].Value = "柴胡";
        herbSheet.Cells[2, 3].Value = 24;
        herbSheet.Cells[2, 4].Value = "g";
        herbSheet.Cells[2, 5].Value = "生用";
        herbSheet.Cells[2, 6].Value = "先煎";

        herbSheet.Cells[3, 1].Value = "F001";
        herbSheet.Cells[3, 2].Value = "黄芩";
        herbSheet.Cells[3, 3].Value = 9;
        herbSheet.Cells[3, 4].Value = "g";
        herbSheet.Cells[3, 5].Value = "生用";

        // 自动调整列宽
        formulaSheet.Cells.AutoFitColumns();
        herbSheet.Cells.AutoFitColumns();

        // 添加说明Sheet
        var instructionSheet = package.Workbook.Worksheets.Add("导入说明");
        instructionSheet.Cells[1, 1].Value = "导入说明";
        instructionSheet.Cells[2, 1].Value = "1. 验方编号必须唯一，用于关联药材";
        instructionSheet.Cells[3, 1].Value = "2. 验方名称为必填项";
        instructionSheet.Cells[4, 1].Value = "3. 是否共享：TRUE表示共享，FALSE表示不共享";
        instructionSheet.Cells[5, 1].Value = "4. 药材Sheet中的验方编号必须与验方Sheet中的编号对应";
        instructionSheet.Cells[6, 1].Value = "5. 药材名称会自动匹配药材库，未匹配的需要在系统中手动校验";
        instructionSheet.Cells[7, 1].Value = "6. 用量必须为整数";
        instructionSheet.Cells[8, 1].Value = "7. 单位默认为'g'";

        instructionSheet.Cells.AutoFitColumns();

        return ServiceResult<byte[]>.Success(package.GetAsByteArray());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成导入模板失败");
        return ServiceResult<byte[]>.Failure($"生成模板失败: {ex.Message}");
    }
}
```

---

## 7. 延迟绑定验证流程

### 7.1 ValidateFormulaHerbAsync实现

```csharp
public async Task<ServiceResult> ValidateFormulaHerbAsync(
    Guid formulaId,
    Guid herbItemId,
    Guid selectedHerbId)
{
    try
    {
        // Step 1: 获取验方（含药材）
        var formula = await _formulaRepository.GetByIdWithHerbsAsync(formulaId);
        if (formula == null)
            return ServiceResult.Failure("验方不存在");

        // Step 2: 查找药材明细
        var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == herbItemId);
        if (herbItem == null)
            return ServiceResult.Failure("药材明细不存在");

        // Step 3: 检查是否已验证
        if (herbItem.IsValidated)
            return ServiceResult.Failure("该药材已校验，无需重复操作");

        // Step 4: 验证选择的药材ID
        var selectedHerb = await _herbRepository.GetByIdAsync(selectedHerbId);
        if (selectedHerb == null)
            return ServiceResult.Failure("选择的药材不存在");

        // Step 5: 更新药材绑定
        herbItem.HerbId = selectedHerbId;
        herbItem.HerbName = selectedHerb.Name;
        herbItem.IsValidated = true;

        // Step 6: 检查验方完整性（是否所有药材都已验证）
        if (formula.Herbs.All(h => h.IsValidated))
        {
            formula.ValidationStatus = FormulaValidationStatus.Validated;
            _logger.LogInformation("验方 {FormulaId} 所有药材已验证，状态更新为Validated", formulaId);
        }

        await _formulaRepository.UpdateAsync(formula);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "验证药材绑定失败: FormulaId={FormulaId}, HerbItemId={HerbItemId}", formulaId, herbItemId);
        return ServiceResult.Failure($"验证药材绑定失败: {ex.Message}");
    }
}
```

### 7.2 GetPendingValidationFormulasAsync实现

```csharp
public async Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync()
{
    try
    {
        var formulas = await _formulaRepository.GetPendingValidationFormulasAsync();
        var dtos = _mapper.Map<List<FormulaDto>>(formulas);

        return ServiceResult<List<FormulaDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取待验证验方失败");
        return ServiceResult<List<FormulaDto>>.Failure($"获取待验证验方失败: {ex.Message}");
    }
}
```

---

## 8. Controller层实现

### 8.1 FormulaController完整实现

```csharp
/// <summary>
/// 验方管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public class FormulaController : ControllerBase
{
    private readonly IFormulaService _formulaService;
    private readonly ILogger<FormulaController> _logger;

    public FormulaController(
        IFormulaService formulaService,
        ILogger<FormulaController> logger)
    {
        _formulaService = formulaService;
        _logger = logger;
    }

    /// <summary>创建验方</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "创建验方", Description = "创建新的验方记录")]
    public async Task<ActionResult<FormulaDto>> CreateAsync([FromBody] FormulaInputDto dto)
    {
        var result = await _formulaService.CreateAsync(dto);
        if (result.Succeeded && result.Data != null)
            return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Data.Id }, result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>查询验方详情</summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "查询验方详情", Description = "根据ID查询验方详情（包含药材组成）")]
    public async Task<ActionResult<FormulaDto>> GetByIdAsync(Guid id)
    {
        var result = await _formulaService.GetByIdAsync(id);
        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return NotFound(result.Message);
    }

    /// <summary>更新验方</summary>
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "更新验方", Description = "更新验方基本信息和药材组成")]
    public async Task<ActionResult<FormulaDto>> UpdateAsync(Guid id, [FromBody] FormulaInputDto dto)
    {
        var result = await _formulaService.UpdateAsync(id, dto);
        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>删除验方</summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "删除验方", Description = "软删除验方")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        var result = await _formulaService.DeleteAsync(id);
        if (result.Succeeded)
            return NoContent();

        return BadRequest(result.Message);
    }

    /// <summary>批量删除验方</summary>
    [HttpPost("batch-delete")]
    [SwaggerOperation(Summary = "批量删除验方", Description = "批量删除验方（最大100条/批次）")]
    public async Task<ActionResult<BatchOperationResultDto>> BatchDeleteAsync([FromBody] List<Guid> ids)
    {
        var result = await _formulaService.BatchDeleteAsync(ids);
        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>分页查询验方</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "分页查询验方", Description = "分页查询验方列表，支持关键词搜索和分类过滤")]
    public async Task<ActionResult<PagedResult<FormulaDto>>> GetPagedAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null)
    {
        var result = await _formulaService.GetPagedAsync(pageNumber, pageSize, keyword, category);
        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>获取验方模板</summary>
    [HttpGet("templates")]
    [SwaggerOperation(Summary = "获取验方模板", Description = "获取启用状态的验方模板列表")]
    public async Task<ActionResult<List<FormulaDto>>> GetTemplatesAsync()
    {
        var result = await _formulaService.GetTemplatesAsync();
        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>搜索验方</summary>
    [HttpGet("search")]
    [SwaggerOperation(Summary = "搜索验方", Description = "按关键词搜索验方（名称+功效）")]
    public async Task<ActionResult<List<FormulaDto>>> SearchAsync([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest("关键词不能为空");

        var result = await _formulaService.SearchAsync(keyword);
        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>克隆验方</summary>
    [HttpPost("{id}/clone")]
    [SwaggerOperation(Summary = "克隆验方", Description = "克隆验方（复制核心信息，不复制药材组成）")]
    public async Task<ActionResult<FormulaDto>> CloneFormulaAsync(Guid id)
    {
        var result = await _formulaService.CloneFormulaAsync(id);
        if (result.Succeeded && result.Data != null)
            return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Data.Id }, result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>Excel导入验方</summary>
    [HttpPost("import")]
    [SwaggerOperation(Summary = "导入验方", Description = "从Excel文件导入验方（Sheet1验方+Sheet2药材）")]
    public async Task<ActionResult<FormulaImportResultDto>> ImportFromExcelAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("文件不能为空");

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            return BadRequest("仅支持Excel文件（.xlsx或.xls）");

        using var stream = file.OpenReadStream();
        var result = await _formulaService.ImportFromExcelAsync(stream, file.FileName);

        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }

    /// <summary>Excel导出验方</summary>
    [HttpGet("export")]
    [SwaggerOperation(Summary = "导出验方", Description = "导出验方到Excel文件")]
    public async Task<IActionResult> ExportAsync([FromQuery] List<Guid>? formulaIds = null)
    {
        var result = await _formulaService.ExportAsync(formulaIds);

        if (result.Succeeded && result.Data != null)
        {
            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"验方导出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        return BadRequest(result.Message);
    }

    /// <summary>生成导入模板</summary>
    [HttpGet("import-template")]
    [SwaggerOperation(Summary = "生成导入模板", Description = "生成Excel导入模板（包含示例数据和说明）")]
    public IActionResult GenerateImportTemplate()
    {
        var result = _formulaService.GenerateImportTemplate();

        if (result.Succeeded && result.Data != null)
        {
            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "验方导入模板.xlsx");
        }

        return BadRequest(result.Message);
    }

    /// <summary>验证药材绑定</summary>
    [HttpPost("{id}/validate-herb/{herbItemId}")]
    [SwaggerOperation(Summary = "验证药材绑定", Description = "为未验证的药材绑定系统药材库ID")]
    public async Task<ActionResult> ValidateFormulaHerbAsync(
        Guid id,
        Guid herbItemId,
        [FromBody] ValidateFormulaHerbRequest request)
    {
        if (request.SelectedHerbId == Guid.Empty)
            return BadRequest("选择的药材ID不能为空");

        var result = await _formulaService.ValidateFormulaHerbAsync(id, herbItemId, request.SelectedHerbId);

        if (result.Succeeded)
            return NoContent();

        return BadRequest(result.Message);
    }

    /// <summary>获取待验证验方</summary>
    [HttpGet("pending-validation")]
    [SwaggerOperation(Summary = "获取待验证验方", Description = "获取Draft状态的验方列表（包含未验证的药材）")]
    public async Task<ActionResult<List<FormulaDto>>> GetPendingValidationFormulasAsync()
    {
        var result = await _formulaService.GetPendingValidationFormulasAsync();

        if (result.Succeeded && result.Data != null)
            return Ok(result.Data);

        return BadRequest(result.Message);
    }
}
```

### 8.2 DTO类定义

**ValidateFormulaHerbRequest.cs**:
```csharp
public class ValidateFormulaHerbRequest
{
    public Guid SelectedHerbId { get; set; }
}
```

---

## 9. 错误处理与日志

### 9.1 统一异常处理中间件

**GlobalExceptionHandlerMiddleware.cs**:
```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的异常: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ServiceResult
        {
            Succeeded = false,
            Message = $"服务器内部错误: {exception.Message}"
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
```

### 9.2 日志记录最佳实践

```csharp
public class FormulaService : IFormulaService
{
    private readonly ILogger<FormulaService> _logger;

    // ✅ 正确：结构化日志
    _logger.LogInformation("创建验方: {Name}, UserId: {UserId}", dto.Name, dto.UserId);

    // ✅ 正确：记录异常
    _logger.LogError(ex, "创建验方失败: {Name}", dto.Name);

    // ✅ 正确：记录关键操作
    _logger.LogInformation("验方 {FormulaId} 状态从Draft更新为Validated", formulaId);

    // ❌ 错误：字符串拼接
    _logger.LogInformation($"创建验方: {dto.Name}");
}
```

---

## 10. 单元测试

### 10.1 Service层单元测试

**FormulaServiceTests.cs**:
```csharp
public class FormulaServiceTests
{
    private readonly Mock<IFormulaRepository> _formulaRepositoryMock;
    private readonly Mock<IHerbRepository> _herbRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<FormulaService>> _loggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly FormulaService _formulaService;

    public FormulaServiceTests()
    {
        _formulaRepositoryMock = new Mock<IFormulaRepository>();
        _herbRepositoryMock = new Mock<IHerbRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<FormulaService>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _formulaService = new FormulaService(
            _formulaRepositoryMock.Object,
            _herbRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsSuccess()
    {
        // Arrange
        var dto = new FormulaInputDto
        {
            Name = "小柴胡汤",
            Effect = "和解少阳",
            Herbs = new List<FormulaHerbItemInputDto>()
        };

        var formula = new Formula
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Effect = dto.Effect
        };

        _mapperMock.Setup(m => m.Map<Formula>(dto)).Returns(formula);
        _mapperMock.Setup(m => m.Map<FormulaDto>(formula)).Returns(new FormulaDto { Id = formula.Id });
        _formulaRepositoryMock.Setup(r => r.AddAsync(formula)).ReturnsAsync(formula);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _formulaService.CreateAsync(dto);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        _formulaRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Formula>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ValidateFormulaHerbAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var herbItemId = Guid.NewGuid();
        var selectedHerbId = Guid.NewGuid();

        var formula = new Formula
        {
            Id = formulaId,
            Herbs = new List<FormulaHerbItem>
            {
                new FormulaHerbItem
                {
                    Id = herbItemId,
                    HerbName = "当归",
                    IsValidated = false
                }
            }
        };

        var selectedHerb = new Herb
        {
            Id = selectedHerbId,
            Name = "当归"
        };

        _formulaRepositoryMock.Setup(r => r.GetByIdWithHerbsAsync(formulaId)).ReturnsAsync(formula);
        _herbRepositoryMock.Setup(r => r.GetByIdAsync(selectedHerbId)).ReturnsAsync(selectedHerb);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(selectedHerbId, formula.Herbs[0].HerbId);
        Assert.True(formula.Herbs[0].IsValidated);
    }

    [Fact]
    public async Task BatchDeleteAsync_ExceedsLimit_ReturnsFailure()
    {
        // Arrange
        var ids = Enumerable.Range(1, 101).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var result = await _formulaService.BatchDeleteAsync(ids);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("不能超过100条", result.Message);
    }
}
```

---

## 11. 常见问题与陷阱

### 11.1 N+1查询问题

**❌ 错误：未Include导致N+1查询**:
```csharp
var formulas = await _formulaRepository.GetAllAsync();
foreach (var formula in formulas)
{
    // N次查询
    var herbs = await _herbRepository.GetByFormulaIdAsync(formula.Id);
}
```

**✅ 正确：使用Include避免N+1查询**:
```csharp
private IQueryable<Formula> GetBaseQuery()
{
    return _dbSet
        .Include(f => f.Herbs) // Eager load，单次查询
        .Where(f => !f.IsDeleted);
}
```

### 11.2 软删除过滤器失效

**❌ 错误：未应用软删除过滤器**:
```csharp
var formula = await _dbSet.FirstOrDefaultAsync(f => f.Id == id); // 可能返回已删除记录
```

**✅ 正确：使用GetBaseQuery统一过滤**:
```csharp
var formula = await GetBaseQuery().FirstOrDefaultAsync(f => f.Id == id);
```

### 11.3 延迟绑定验证不完整

**❌ 错误：未检查所有药材IsValidated**:
```csharp
herbItem.IsValidated = true;
formula.ValidationStatus = FormulaValidationStatus.Validated; // 错误：未检查其他药材
```

**✅ 正确：检查所有药材IsValidated**:
```csharp
herbItem.IsValidated = true;

if (formula.Herbs.All(h => h.IsValidated))
{
    formula.ValidationStatus = FormulaValidationStatus.Validated;
}
```

### 11.4 EPPlus许可证未设置

**❌ 错误：未设置LicenseContext**:
```csharp
using var package = new ExcelPackage(stream); // 抛出异常：Please set the ExcelPackage.LicenseContext property
```

**✅ 正确：设置非商业许可证**:
```csharp
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // 在构造函数中设置
using var package = new ExcelPackage(stream);
```

### 11.5 批量操作事务问题

**❌ 错误：循环中多次SaveChanges**:
```csharp
foreach (var id in ids)
{
    var formula = await _repository.GetByIdAsync(id);
    formula.IsDeleted = true;
    await _unitOfWork.SaveChangesAsync(); // 每次都提交事务
}
```

**✅ 正确：循环后一次性SaveChanges**:
```csharp
foreach (var id in ids)
{
    var formula = await _repository.GetByIdAsync(id);
    formula.IsDeleted = true;
}
await _unitOfWork.SaveChangesAsync(); // 一次性提交事务
```

---

## 12. 检查清单

### 12.1 Entity层检查

- [ ] Formula实体继承BaseEntity
- [ ] FormulaHerbItem.HerbId设置为可空（Nullable）
- [ ] FormulaHerbItem.IsValidated默认为false
- [ ] ValidationStatus默认为Draft
- [ ] EF Core配置中设置软删除全局过滤器
- [ ] 配置1:N关系（Cascade删除）

### 12.2 Repository层检查

- [ ] 实现IFormulaRepository接口的7个方法
- [ ] GetBaseQuery统一Include策略
- [ ] GetBaseQuery统一软删除过滤
- [ ] 分页查询支持关键词搜索
- [ ] 权限过滤逻辑（UserId OR IsShared）

### 12.3 Service层检查

- [ ] 实现IFormulaService接口的14个方法
- [ ] 使用AutoMapper进行DTO映射
- [ ] 使用IUnitOfWork统一事务管理
- [ ] Excel导入实现延迟绑定逻辑
- [ ] TryMatchHerbAsync实现自动匹配
- [ ] ValidateFormulaHerbAsync检查所有药材IsValidated
- [ ] 批量删除限制最大100条
- [ ] 设置EPPlus.LicenseContext

### 12.4 Controller层检查

- [ ] 实现14个API端点
- [ ] 添加SwaggerOperation注解
- [ ] 参数验证（ModelState、IFormFile检查）
- [ ] HTTP响应封装（Ok、BadRequest、CreatedAtAction）
- [ ] Excel导入返回IFormFile参数
- [ ] Excel导出返回File

### 12.5 测试检查

- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行应用，测试Swagger UI
- [ ] 测试创建验方API
- [ ] 测试Excel导入API
- [ ] 测试延迟绑定验证API
- [ ] 测试批量删除API
- [ ] 测试获取待验证验方API
- [ ] 单元测试覆盖核心Service方法

---

## 13. 参考资料

### 13.1 架构文档

- **Formula架构设计**: `docs/explanation/architecture/server/formula-design.md` - 三层架构、延迟绑定、Excel导入导出
- **Server端架构概览**: `docs/explanation/architecture/server/README.md` - 三层架构、SOLID原则、依赖注入
- **Shared Contract层**: `docs/explanation/architecture/shared/README.md` - DTO设计、ServiceResult模式

### 13.2 开发指南

- **Herbs开发指南**: `docs/how-to-guides/server/herbs-development.md` - 类似的Server端开发模式
- **Patients开发指南**: `docs/how-to-guides/server/patients-development.md` - Repository和Service实现示例
- **Medical Case开发指南**: `docs/how-to-guides/server/medical-case-development.md` - 复杂业务逻辑实现

### 13.3 相关Issue

- **Epic #1347**: Formula模块架构设计
- **Issue #1348**: 延迟绑定验证功能
- **Issue #1349**: 待验证验方查询
- **Issue #1169**: 批量删除功能
- **Issue #1166**: Excel导入导出功能

### 13.4 外部资源

- **ASP.NET Core文档**: https://learn.microsoft.com/zh-cn/aspnet/core
- **Entity Framework Core文档**: https://learn.microsoft.com/zh-cn/ef/core
- **EPPlus官方文档**: https://epplussoftware.com/docs
- **AutoMapper文档**: https://docs.automapper.org

---

**文档维护**: 本文档应与 `formula-design.md` 保持同步，任何架构变更需同步更新。

**问题反馈**: 如有疑问或建议，请在GitHub Issue中提出。
