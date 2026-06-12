# 如何添加新模块

> 以现有 Herbs 模块为参考，完整步骤创建一个新的业务模块。

---

## 概览

添加一个新模块需要创建 **4层文件**，涉及 **5个项目**：

```
LYBT.Entities          ← 实体定义
LYBT.Shared.Models     ← DTO（前后端共享）
LYBT.Shared.Validators ← 输入验证
LYBT.Module.{Name}     ← 服务端模块（Repository + Service）
LYBT.WebAPI            ← Controller + DI注册
LYBT.Desktop.{Name}    ← 桌面模块（VM + View + Repository）
LYBT.Desktop.Shell     ← 模块加载 + DI注册
```

---

## 步骤 1：定义实体

**位置**: `src/Server/Core/LYBT.Entities/{Name}/{Name}Model.cs`

```csharp
[Table("TableName")]
public class EntityName : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    // 其他属性...
}
```

**规则**:
- 继承 `BaseEntity`（提供 Id, CreatedAt, UpdatedAt, IsDeleted, CreatedBy, UpdatedBy, RowVersion）
- anemic 模型，不含业务逻辑（MedicalCase 是唯一例外）
- 表名用 `[Table]` 特性标注

**注册 DbContext**: 在 `AppDbContext.cs` 中添加 `DbSet<EntityName>` 和 Fluent API 配置。

---

## 步骤 2：定义 DTO

**位置**: `src/Shared/LYBT.Shared.Models/Contracts/{Name}/`

创建 3 个基础 DTO：

```csharp
// {Name}ListDto.cs — 列表页
public class NameListDto { ... }

// {Name}DetailDto.cs — 详情页
public class NameDetailDto { ... }

// {Name}InputDto.cs — 创建/编辑
public class NameInputDto { ... }
```

**规则**: DTO 命名空间为 `LYBT.Shared.Models.Contracts.{Name}`。

---

## 步骤 3：定义验证器

**位置**: `src/Shared/LYBT.Shared.Validators/{Name}/{Name}InputDtoValidator.cs`

```csharp
public class NameInputDtoValidator : AbstractValidator<NameInputDto>
{
    public NameInputDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

---

## 步骤 4：创建服务端模块

**位置**: `src/Server/Modules/LYBT.Module.{Name}/`

### 4.1 项目文件

创建 `LYBT.Module.{Name}.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Riok.Mapperly" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\..\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Validators\LYBT.Shared.Validators.csproj" />
  </ItemGroup>
</Project>
```

> 包版本不需要指定（Central Package Management）。实际路径参考现有模块调整。

### 4.2 模块注册

`{Name}Module.cs`：

```csharp
public static class NameModule
{
    public static IServiceCollection AddNameModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INameRepository, NameRepository>();
        services.AddScoped<INameService, NameService>();
        services.AddValidatorsFromAssemblyContaining<NameInputDtoValidator>();
        return services;
    }
}
```

### 4.3 接口

`Interfaces/I{Name}Repository.cs`：

```csharp
public interface INameRepository : IRepository<EntityName>
{
    // 领域特定查询方法
    Task<PagedResult<NameListDto>> GetPagedAsync(int page, int pageSize, string? keyword);
}
```

`Interfaces/I{Name}Service.cs`：

```csharp
public interface INameService
{
    Task<PagedResult<NameListDto>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<NameDetailDto> GetByIdAsync(Guid id);
    Task<NameDetailDto> CreateAsync(NameInputDto dto);
    Task<NameDetailDto> UpdateAsync(Guid id, NameInputDto dto);
    Task DeleteAsync(Guid id);
}
```

### 4.4 Repository

`Repositories/{Name}Repository.cs`：

```csharp
internal class NameRepository : BaseRepository<EntityName>, INameRepository
{
    public NameRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<NameListDto>> GetPagedAsync(int page, int pageSize, string? keyword)
    {
        var query = _dbSet.Where(x => !x.IsDeleted);
        // 筛选、分页、映射...
    }
}
```

> `internal` 类 — 通过 `InternalsVisibleTo("LYBT.Tests.Server")` 暴露给测试。

### 4.5 Service

`Services/{Name}Service.cs`：

```csharp
public class NameService : BaseService<EntityName>, INameService
{
    private readonly INameRepository _repository;
    
    public NameService(INameRepository repository, IMapper mapper, ILogger<NameService> logger)
        : base(repository, mapper, logger)
    {
        _repository = repository;
    }
    // 实现接口方法...
}
```

### 4.6 Mapper

`Mapping/{Name}Mapper.cs`：

```csharp
[Mapper]
public partial class NameMapper
{
    public partial NameListDto ToListDto(EntityName entity);
    public partial NameDetailDto ToDetailDto(EntityName entity);
    public partial EntityName ToEntity(NameInputDto dto);
    public partial void UpdateEntity(NameInputDto dto, EntityName entity);
}
```

> 使用 Mapperly 编译时源生成，无反射。

---

## 步骤 5：创建 Controller

**位置**: `src/Server/Services/LYBT.WebAPI/Controllers/{Name}sController.cs`

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NamesController : BaseApiController
{
    private readonly INameService _service;

    public NamesController(INameService service, ILogger<NamesController> logger)
        : base(logger)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<NameListDto>>> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, keyword);
        return Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<NameDetailDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Success(result);
    }

    [HttpPost]
    public async Task<ApiResponse<NameDetailDto>> Create([FromBody] NameInputDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Success(result);
    }
}
```

---

## 步骤 6：注册服务端模块

**位置**: `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs`

在 `RegisterBusinessModules()` 方法中添加：

```csharp
services.AddNameModule(configuration);
```

同时将模块项目添加到解决方案：

```bash
dotnet sln LYBTZYZS.sln add src/Server/Modules/LYBT.Module.{Name}/LYBT.Module.{Name}.csproj
```

---

## 步骤 7：创建桌面模块

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.{Name}/`

### 7.1 文件结构

```
LYBT.Desktop.{Name}/
├── LYBT.Desktop.{Name}.csproj     # net8.0-windows, UseWPF
├── {Name}Module.cs                # Prism IModule
├── Interfaces/
│   └── I{Name}Service.cs          # Desktop服务接口
├── Services/
│   └── Remote{Name}Service.cs     # 调用Repository
├── Repositories/
│   └── {Name}Repository.cs        # IApiClient (Refit)
├── Models/
│   └── {Name}DetailModel.cs       # UI模型 (ValidatableModelBase)
├── Mappers/
│   └── {Name}Mapper.cs            # Mapperly
├── ViewModels/
│   └── {Name}MasterDetailViewModel.cs
├── Views/
│   └── {Name}MasterDetailView.xaml
└── Controls/
    ├── {Name}MasterDetailControl.xaml
    ├── {Name}EditControl.xaml
    └── {Name}ViewControl.xaml
```

### 7.2 模块注册

`{Name}Module.cs`：

```csharp
[Module(ModuleName = nameof(NameModule))]
[ModuleDependency("AuthenticationModule")]
public class NameModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ViewModelLocationProvider.Register(
            typeof(NameMasterDetailControl).ToString(),
            typeof(NameMasterDetailViewModel));

        containerRegistry.Register<INameService, RemoteNameService>();
        containerRegistry.Register<NameMasterDetailViewModel>();
        containerRegistry.RegisterForNavigation<Views.NameMasterDetailView>();
    }
}
```

### 7.3 Repository 接口

**位置**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/I{Name}Repository.cs`

```csharp
public interface INameRepository
{
    Task<PagedResult<NameListDto>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<NameDetailDto> GetByIdAsync(Guid id);
    Task<NameDetailDto> CreateAsync(NameInputDto dto);
    Task<NameDetailDto> UpdateAsync(Guid id, NameInputDto dto);
    Task DeleteAsync(Guid id);
}
```

---

## 步骤 8：注册桌面模块

### 8.1 ModuleCatalog

**位置**: `src/Client/Desktop/Shell/App.xaml.cs`

```csharp
moduleCatalog.AddModule<NameModule>(InitializationMode.WhenAvailable);
```

### 8.2 Repository DI

**位置**: `src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs`

```csharp
containerRegistry.Register<INameRepository>(resolver =>
    new NameRepository(
        resolver.Resolve<IApiClient>(),
        resolver.Resolve<ILogger<NameRepository>>()));
```

### 8.3 角色加载

**位置**: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`

在对应角色的模块加载数组中添加 `"NameModule"`。

---

## 步骤 9：添加测试

### 服务端测试

**位置**: `tests/LYBT.Tests.Server/Modules/{Name}/`

```csharp
public class NameServiceTests : TestBase
{
    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsCreated()
    {
        // 使用真实 SQL Server + Respawn 清理
        // 零 Mock — 测试完整 Service → Repository → DbContext 链路
    }
}
```

### 桌面测试

**位置**: `tests/LYBT.Tests.Desktop/Modules/{Name}/`

```csharp
public class NameViewModelTests : DesktopTestBase
{
    // 使用 SQLite InMemory
    // 测试 ViewModel 逻辑
}
```

---

## 步骤 10：添加到解决方案

```bash
# 服务端模块
dotnet sln LYBTZYZS.sln add src/Server/Modules/LYBT.Module.{Name}/LYBT.Module.{Name}.csproj

# 桌面模块
dotnet sln LYBTZYZS.sln add src/Client/Desktop/Modules/LYBT.Desktop.{Name}/LYBT.Desktop.{Name}.csproj

# 验证
dotnet build LYBTZYZS.sln
dotnet test LYBTZYZS.sln
```

---

## 检查清单

| # | 项目 | 位置 | 完成 |
|---|------|------|------|
| 1 | 实体 | `LYBT.Entities/{Name}/` | ☐ |
| 2 | DTO (List/Detail/Input) | `LYBT.Shared.Models/Contracts/{Name}/` | ☐ |
| 3 | 验证器 | `LYBT.Shared.Validators/{Name}/` | ☐ |
| 4 | Repository 接口+实现 | `LYBT.Module.{Name}/` | ☐ |
| 5 | Service 接口+实现 | `LYBT.Module.{Name}/` | ☐ |
| 6 | Mapper | `LYBT.Module.{Name}/Mapping/` | ☐ |
| 7 | 模块注册 (AddNameModule) | `LYBT.Module.{Name}/{Name}Module.cs` | ☐ |
| 8 | Controller | `LYBT.WebAPI/Controllers/` | ☐ |
| 9 | WebAPI DI注册 | `ServiceCollectionExtensions.cs` | ☐ |
| 10 | Desktop Repository接口 | `LYBT.Desktop.Contracts/Repositories/` | ☐ |
| 11 | Desktop Repository实现 | `LYBT.Desktop.{Name}/Repositories/` | ☐ |
| 12 | Desktop Service | `LYBT.Desktop.{Name}/Services/` | ☐ |
| 13 | Desktop ViewModel + View | `LYBT.Desktop.{Name}/` | ☐ |
| 14 | Desktop Module注册 | `{Name}Module.cs (IModule)` | ☐ |
| 15 | Shell ModuleCatalog | `App.xaml.cs` | ☐ |
| 16 | Shell Repository DI | `DataSourceRegistrationExtensions.cs` | ☐ |
| 17 | Shell 角色加载 | `LoginCoordinator.cs` | ☐ |
| 18 | 服务端测试 | `tests/LYBT.Tests.Server/` | ☐ |
| 19 | 桌面测试 | `tests/LYBT.Tests.Desktop/` | ☐ |
| 20 | 构建通过 | `dotnet build` | ☐ |

---

## 变更记录
| 日期 | 变更 |
|------|------|
| 2026-06-12 | 初始版本 |
