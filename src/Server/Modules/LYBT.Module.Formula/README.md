# LYBT.Module.Formula - 验方管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(验方管理)
- **职责**:提供验方（经典方剂和经验方）的完整生命周期管理,包括验方创建、药材组成配置、方剂分享、从处方创建验方、Excel导入/导出等功能。作为处方系统的模板支撑,旨在提高医生开方效率,积累诊疗经验。采用标准三层架构（Controller → Service → Repository）,确保业务逻辑清晰、数据访问高效。

##  代码结构

```
LYBT.Module.Formula/
├── FormulaModule.cs                    # Prism模块注册
│   └── AddFormulaModule()              # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                         # 模块接口定义
│   └── IFormulaRepository.cs           # 验方仓储接口(8个方法)
├── Services/                           # 业务逻辑实现
│   └── FormulaService.cs               # 验方服务(19个方法)
│       ├── GetPagedAsync()             # 分页查询验方
│       ├── GetByIdAsync()              # 按ID查询验方详情
│       ├── CreateAsync()               # 创建验方
│       ├── UpdateAsync()               # 更新验方
│       ├── SearchAsync()               # 搜索验方(按名称/分类)
│       ├── CloneFormulaAsync()         # 克隆验方(复制现有验方)
│       ├── DeleteAsync()               # 删除验方
│       ├── ValidateFormulaHerbAsync()  # 验证药材有效性
│       ├── GetPendingValidationFormulasAsync() # 获取待验证验方
│       ├── BatchDeleteAsync()          # 批量删除验方
│       ├── ImportFromExcelAsync()      # Excel导入验方
│       ├── ExportAsync()               # 导出验方到Excel
│       ├── GenerateImportTemplate()    # 生成Excel导入模板
│       ├── ParseHerbItems()            # 解析药材字符串
│       └── TryMatchHerbAsync()         # 智能匹配药材
├── Repositories/                       # 数据仓储实现
│   └── FormulaRepository.cs            # 验方仓储(8个方法)
│       ├── GetBaseQuery()              # 基础查询(Include HerbItems)
│       ├── GetTemplatesAsync()         # 获取验方模板
│       ├── GetByIdWithHerbsAsync()     # 查询验方及药材
│       ├── GetPagedWithDetailsAsync()  # 分页查询详情(含统计)
│       ├── GetByUserIdAsync()          # 按用户查询验方
│       ├── GetSharedFormulasAsync()    # 获取共享验方
│       └── GetByCategoryAsync()        # 按分类查询验方
├── Validators/                         # FluentValidation验证器
│   ├── FormulaCreateDtoValidator.cs    # 创建验方DTO验证
│   └── FormulaUpdateDtoValidator.cs    # 更新验方DTO验证
└── Mapping/                            # AutoMapper映射配置
    └── FormulaMappingProfile.cs        # Entity ↔ DTO映射规则
```

**说明**:
- **FormulaModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **FormulaService**:19个方法覆盖验方的增删改查、搜索、克隆、导入导出、智能匹配等功能
- **FormulaRepository**:8个方法提供灵活的数据查询能力(模板、共享、分类等)
- **Validators**:FluentValidation验证器确保DTO数据完整性
- **Mapping**:AutoMapper配置统一处理Entity与DTO的转换

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(FormulaModel、FormulaHerbItem)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(FormulaDto、CreateFormulaDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IFormulaService、IHerbRepository)

### 被依赖项目
1. **LYBT.Module.Prescriptions** - 处方模块使用验方作为模板
2. **LYBT.WebAPI** - Web服务层通过FormulasController暴露API
3. **测试项目**:
   - LYBT.Module.Formula.Tests（单元测试）
   - LYBT.Module.Formula.IntegrationTests（集成测试）
   - LYBT.Server.ArchTests（架构测试）

### NuGet包
- **FluentValidation** (11.x) - DTO验证框架
- **AutoMapper** (13.x) - 对象映射框架
- **Microsoft.Extensions.DependencyInjection** (8.0.x) - 依赖注入容器

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Entity Framework Core 8**: 通过Repository模式间接使用,用于数据持久化
- **AutoMapper 13.x**: Entity与DTO之间的自动映射
- **FluentValidation 11.x**: DTO数据验证框架
- **LINQ**: 复杂查询表达式(分页、搜索、过滤)
- **异步编程**: 全异步方法(async/await),提升性能

##  快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj
```

**集成说明**:

### 1. 注册验方模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册验方模块(自动注册仓储+服务+验证器)
        services.AddFormulaModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class FormulasController : ControllerBase
{
    private readonly IFormulaService _formulaService;

    public FormulasController(IFormulaService formulaService)
    {
        _formulaService = formulaService;
    }

    // 分页查询验方
    [HttpGet]
    public async Task<IActionResult> GetFormulas(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] bool? isShared = null)
    {
        var result = await _formulaService.GetPagedAsync(
            pageIndex, pageSize, category, isShared
        );
        return Ok(result);
    }

    // 创建验方
    [HttpPost]
    public async Task<IActionResult> CreateFormula([FromBody] CreateFormulaDto dto)
    {
        var formulaDto = await _formulaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetFormulaById), new { id = formulaDto.Id }, formulaDto);
    }
}
```

### 3. 克隆验方功能(复制现有验方)
```csharp
public class FormulaService : IFormulaService
{
    public async Task<FormulaDto> CloneFormulaAsync(Guid sourceId, string newName)
    {
        // 查询原验方(含药材)
        var source = await _repository.GetByIdWithHerbsAsync(sourceId);
        if (source == null) throw new NotFoundException("验方不存在");

        // 复制验方及药材
        var clone = new FormulaModel
        {
            Name = newName,
            Category = source.Category,
            Description = source.Description,
            UsageInstructions = source.UsageInstructions,
            IsShared = false, // 克隆的验方默认不共享
            HerbItems = source.HerbItems.Select(item => new FormulaHerbItem
            {
                HerbId = item.HerbId,
                Dosage = item.Dosage,
                Unit = item.Unit,
                Notes = item.Notes
            }).ToList()
        };

        await _repository.AddAsync(clone);
        return _mapper.Map<FormulaDto>(clone);
    }
}
```

### 4. Excel导入验方(批量创建)
```csharp
// 在FormulasController中
[HttpPost("import")]
public async Task<IActionResult> ImportFormulas(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var result = await _formulaService.ImportFromExcelAsync(stream);

    return Ok(new
    {
        SuccessCount = result.Succeeded.Count,
        FailedCount = result.Failed.Count,
        Errors = result.Failed
    });
}

// FormulaService实现智能药材匹配
private async Task<HerbItemData?> TryMatchHerbAsync(string herbName)
{
    // 精确匹配
    var herb = await _herbRepository.GetByNameAsync(herbName);
    if (herb != null) return new HerbItemData { HerbId = herb.Id, Name = herb.Name };

    // 模糊匹配(中医药材别名)
    herb = await _herbRepository.SearchByAliasAsync(herbName);
    if (herb != null)
    {
        _logger.LogWarning($"使用别名匹配:{herbName} → {herb.Name}");
        return new HerbItemData { HerbId = herb.Id, Name = herb.Name };
    }

    return null; // 匹配失败
}
```

### 5. 验方药材验证(确保药材存在)
```csharp
public async Task ValidateFormulaHerbAsync(Guid formulaId)
{
    var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
    if (formula == null) throw new NotFoundException("验方不存在");

    var invalidHerbs = new List<string>();
    foreach (var item in formula.HerbItems)
    {
        var herb = await _herbRepository.GetByIdAsync(item.HerbId);
        if (herb == null || herb.IsDeleted)
        {
            invalidHerbs.Add(item.Notes ?? item.HerbId.ToString());
        }
    }

    if (invalidHerbs.Any())
    {
        throw new ValidationException(
            $"验方包含无效药材:{string.Join(", ", invalidHerbs)}"
        );
    }
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `FormulasController` 对外暴露。

- **API路由前缀**: `/api/v1/formulas`

**主要端点**:
- `GET /api/v1/formulas` - 分页查询验方
- `GET /api/v1/formulas/{id}` - 按ID查询验方详情
- `POST /api/v1/formulas` - 创建验方
- `PUT /api/v1/formulas/{id}` - 更新验方
- `DELETE /api/v1/formulas/{id}` - 删除验方
- `POST /api/v1/formulas/{id}/clone` - 克隆验方
- `GET /api/v1/formulas/search` - 搜索验方(按名称/分类)
- `POST /api/v1/formulas/import` - Excel导入验方
- `GET /api/v1/formulas/export` - 导出验方到Excel
- `GET /api/v1/formulas/pending-validation` - 获取待验证验方
- `POST /api/v1/formulas/batch-delete` - 批量删除验方

**完整API定义**请参考 `IFormulaService` 接口和 `FormulasController` 的实现。

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/formula/](../../../../docs/reference/modules/formula/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/formula-design.md](../../../../docs/explanation/architecture/server/formula-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/formula-development.md](../../../../docs/how-to-guides/server/formula-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
