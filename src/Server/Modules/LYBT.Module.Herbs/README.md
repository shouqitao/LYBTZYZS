# LYBT.Module.Herbs - 中药材管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(中药材管理)
- **职责**:提供中药材信息的完整生命周期管理,包括药材档案创建、价格维护、拼音检索、批量导入导出等功能。作为处方和验方系统的基础数据支撑,本模块采用**Record-Only模式**（只管理药材档案信息,不涉及库存）,以简化流程,特别适合小型诊所的需求。采用标准三层架构（Controller → Service → Repository）,确保业务逻辑清晰、数据访问高效。

## 📂 代码结构

```
LYBT.Module.Herbs/
├── HerbsModule.cs                      # 模块依赖注入注册
│   └── AddHerbsModule()                # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                         # 模块接口定义
│   └── IHerbRepository.cs              # 药材仓储接口(2个方法)
├── Services/                           # 业务逻辑实现
│   └── HerbService.cs                  # 药材服务(13个方法)
│       ├── GetPagedAsync()             # 分页查询药材
│       ├── GetByIdAsync()              # 按ID查询药材详情
│       ├── CreateAsync()               # 创建药材
│       ├── UpdateAsync()               # 更新药材
│       ├── DeleteAsync()               # 删除药材
│       ├── SearchAsync()               # 搜索药材(按名称/拼音/功效)
│       ├── BatchDeleteAsync()          # 批量删除药材
│       ├── ImportFromExcelAsync()      # Excel导入药材
│       ├── ExportAsync()               # 导出药材到Excel
│       └── GenerateImportTemplate()    # 生成Excel导入模板
├── Repositories/                       # 数据仓储实现
│   └── HerbRepository.cs               # 药材仓储(2个方法)
│       ├── GetByNameAsync()            # 按药材名称精确查询
│       └── GetByNameOrPinyinAsync()    # 按名称或拼音模糊查询
├── Validators/                         # FluentValidation验证器
│   ├── HerbCreateDtoValidator.cs       # 创建药材DTO验证
│   └── HerbUpdateDtoValidator.cs       # 更新药材DTO验证
└── Mapping/                            # AutoMapper映射配置
    └── HerbMappingProfile.cs           # Entity ↔ DTO映射规则
```

**说明**:
- **HerbsModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **HerbService**:13个方法覆盖药材的增删改查、搜索、批量导入导出等功能
- **HerbRepository**:2个方法提供药材精确查询和拼音模糊查询（支持中医快速输入）
- **Record-Only模式**:只管理药材档案信息（名称、功效、价格、剂量等）,不涉及库存管理
- **Validators**:FluentValidation验证器确保DTO数据完整性（必填项、格式验证）
- **Mapping**:AutoMapper配置统一处理Entity与DTO的转换

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(HerbModel)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(HerbDto、CreateHerbDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IHerbService、IHerbRepository)

### 被依赖项目
1. **LYBT.Module.Formula** - 验方模块使用药材作为组成部分
2. **LYBT.Module.Prescriptions** - 处方模块使用药材配方
3. **LYBT.WebAPI** - Web服务层通过HerbsController暴露API
4. **测试项目**:
   - LYBT.Module.Herbs.Tests（单元测试）
   - LYBT.Module.Herbs.IntegrationTests（集成测试）
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
- **LINQ**: 复杂查询表达式(分页、搜索、拼音匹配)
- **异步编程**: 全异步方法(async/await),提升性能

## 🚀 快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Herbs/LYBT.Module.Herbs.csproj
```

**集成说明**:

### 1. 注册药材模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册药材模块(自动注册仓储+服务+验证器)
        services.AddHerbsModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class HerbsController : ControllerBase
{
    private readonly IHerbService _herbService;

    public HerbsController(IHerbService herbService)
    {
        _herbService = herbService;
    }

    // 分页查询药材
    [HttpGet]
    public async Task<IActionResult> GetHerbs(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _herbService.GetPagedAsync(
            pageIndex, pageSize, searchTerm
        );
        return Ok(result);
    }

    // 创建药材
    [HttpPost]
    public async Task<IActionResult> CreateHerb([FromBody] CreateHerbDto dto)
    {
        var herbDto = await _herbService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetHerbById), new { id = herbDto.Id }, herbDto);
    }
}
```

### 3. 拼音检索功能(中医快速输入)
```csharp
public class HerbRepository : BaseRepository<HerbModel>, IHerbRepository
{
    // 支持拼音模糊匹配(如输入"dg"可匹配"当归")
    public async Task<HerbModel?> GetByNameOrPinyinAsync(string keyword)
    {
        return await _dbSet
            .Where(h =>
                h.Name.Contains(keyword) ||            // 名称匹配
                h.PinyinAbbreviation.Contains(keyword) // 拼音首字母匹配
            )
            .FirstOrDefaultAsync();
    }
}

// 在Service层使用
public async Task<List<HerbDto>> SearchAsync(string keyword)
{
    // 支持按名称、拼音、功效搜索
    var herbs = await _repository.GetByNameOrPinyinAsync(keyword);
    return _mapper.Map<List<HerbDto>>(herbs);
}
```

### 4. Excel批量导入药材
```csharp
// 在HerbsController中
[HttpPost("import")]
public async Task<IActionResult> ImportHerbs(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var result = await _herbService.ImportFromExcelAsync(stream);

    return Ok(new
    {
        SuccessCount = result.Succeeded.Count,
        FailedCount = result.Failed.Count,
        Errors = result.Failed.Select(f => new
        {
            f.RowNumber,
            f.ErrorMessage,
            f.Data
        })
    });
}

// HerbService实现数据验证
private async Task<ImportResult> ImportFromExcelAsync(Stream stream)
{
    var result = new ImportResult();
    var herbs = ParseExcelData(stream);

    foreach (var (rowNumber, herb) in herbs)
    {
        try
        {
            // 验证必填项
            if (string.IsNullOrWhiteSpace(herb.Name))
            {
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = "药材名称不能为空",
                    Data = herb
                });
                continue;
            }

            // 检查重复
            var existing = await _repository.GetByNameAsync(herb.Name);
            if (existing != null)
            {
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = $"药材已存在:{herb.Name}",
                    Data = herb
                });
                continue;
            }

            // 保存药材
            await _repository.AddAsync(herb);
            result.Succeeded.Add(herb);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"导入药材失败:行{rowNumber}");
            result.Failed.Add(new ImportError
            {
                RowNumber = rowNumber,
                ErrorMessage = ex.Message,
                Data = herb
            });
        }
    }

    return result;
}
```

### 5. Record-Only模式的价格维护
```csharp
public class HerbDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }           // 药材名称
    public string? Category { get; set; }      // 分类（如:补益药、清热药）
    public string? Effects { get; set; }       // 功效（如:补气养血）
    public decimal? UnitPrice { get; set; }    // 单价（元/克）
    public string? DefaultUnit { get; set; }   // 默认计量单位（如:克、两）
    public string? DefaultDosage { get; set; } // 常用剂量（如:3-9g）
    public string? PinyinAbbreviation { get; set; } // 拼音首字母（快速检索）
    public string? Notes { get; set; }         // 备注

    // ⚠️ 不包含库存字段（库存管理超出MVP范围）
}

// 在Service层更新价格
public async Task UpdatePriceAsync(Guid herbId, decimal newPrice)
{
    var herb = await _repository.GetByIdAsync(herbId);
    if (herb == null) throw new NotFoundException("药材不存在");

    herb.UnitPrice = newPrice;
    await _repository.UpdateAsync(herb);
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `HerbsController` 对外暴露。

- **API路由前缀**: `/api/v1/herbs`

**主要端点**:
- `GET /api/v1/herbs` - 分页查询药材
- `GET /api/v1/herbs/{id}` - 按ID查询药材详情
- `POST /api/v1/herbs` - 创建药材
- `PUT /api/v1/herbs/{id}` - 更新药材
- `DELETE /api/v1/herbs/{id}` - 删除药材
- `GET /api/v1/herbs/search` - 搜索药材(按名称/拼音/功效)
- `POST /api/v1/herbs/batch-delete` - 批量删除药材
- `POST /api/v1/herbs/import` - Excel导入药材
- `GET /api/v1/herbs/export` - 导出药材到Excel
- `GET /api/v1/herbs/template` - 下载Excel导入模板

**完整API定义**请参考 `IHerbService` 接口和 `HerbsController` 的实现。

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/herbs/](../../../../docs/reference/modules/herbs/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/herbs-design.md](../../../../docs/explanation/architecture/server/herbs-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/herbs-development.md](../../../../docs/how-to-guides/server/herbs-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
