# 凌隐宝堂中医诊所精细编码与实现规约

**文档编号**: DEV-001  
**版本**: 1.0  
**日期**: 2025年9月24日

---

## 1. 概述

本规约是 [ADR-002 技术路线建议报告](docs/architecture/ADR-002-technology-roadmap-suggestion.md) 的配套实施细则。它为开发者提供了在本项目技术栈下进行日常编码时必须遵循的具体标准和最佳实践。所有新编写及重构的代码都应严格遵守本规约。

## 2. 后端实现规约 (Backend)

### 2.1. Controller 层

Controller 层是API的门户，必须保持“瘦”和“清晰”。

*   **职责**：仅负责①解析HTTP请求，②调用一个或多个服务方法，③将服务层的返回结果（DTO或业务结果对象）映射为HTTP响应。**严禁在Controller中编写任何业务逻辑**。
*   **数据契约**：所有方法的输入参数和返回类型都必须是定义在 `LYBT.Shared.Models` 中的DTO。**严禁将EF Core实体（Entities）暴露到Controller层**。
*   **属性**：必须使用 `[ApiController]` 和 `[Route("api/v{version:apiVersion}/[controller]")]` 属性。
*   **返回值**：推荐使用 `ActionResult<T>` 作为返回类型，以便利用框架的类型推断和标准化响应。

**示例**:
```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : BaseApiController // 假设有统一的基类
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetPatientById(Guid id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if (patient == null) return NotFound();
        return Ok(patient);
    }
}
```

### 2.2. Service / Repository 层 (写入侧)

写入侧的服务层是业务逻辑的核心，应遵循标准的仓储模式。

*   **职责**：封装所有业务规则、验证和数据持久化操作。
*   **依赖注入**：必须通过构造函数注入 `DbContext`、其他服务或仓储接口。
*   **原子性**：一个公开的服务方法应对应一个完整的业务用例（Use Case）。
*   **验证**：所有公开方法的入口处都必须对输入参数（DTO）进行验证。推荐使用 `FluentValidation`。
*   **事务管理**：对于涉及多个实体修改的复杂操作，必须使用 `IDbContextTransaction` 显式包裹，确保操作的原子性。
*   **并发控制**：更新操作必须处理 `DbUpdateConcurrencyException`，以应对并发冲突。

**示例**:
```csharp
public class PatientService : IPatientService
{
    private readonly AppDbContext _context;
    private readonly IValidator<CreatePatientDto> _validator;

    public PatientService(AppDbContext context, IValidator<CreatePatientDto> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<Guid> CreatePatientAsync(CreatePatientDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid) 
        {
            throw new ValidationException(validationResult.Errors);
        }

        var patient = new Patient { /* ... 从 DTO 映射 ... */ };
        
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return patient.Id;
    }
}
```

### 2.3. QueryService / ReadRepository 层 (读取侧)

读取侧的核心是性能和效率。

*   **只读性**：**严禁**在读取侧的任何服务中调用 `SaveChangesAsync()`。
*   **性能优化**：所有查询默认必须使用 `.AsNoTracking()` 以禁用EF Core的变更跟踪，这是提升查询性能的关键。
*   **投影 (Projection)**：必须在数据库层面完成数据到DTO的转换，以减少传输的数据量。优先使用 AutoMapper 的 `.ProjectTo<T>()` 或 LINQ 的 `.Select(e => new TDto { ... })`。
*   **缓存**：缓存逻辑应封装在 `QueryService` 中，对调用方透明。遵循项目已定义的缓存键策略。

**示例**:
```csharp
public class PatientQueryService : IPatientQueryService
{
    private readonly AppDbContext _context;
    private readonly IConfigurationProvider _mapperConfig;

    // ... 构造函数 ...

    public async Task<PatientDto> GetByIdAsync(Guid id)
    {
        return await _context.Patients
            .AsNoTracking() // 禁用变更跟踪
            .Where(p => p.Id == id && !p.IsDeleted)
            .ProjectTo<PatientDto>(_mapperConfig) // 在DB层投影到DTO
            .FirstOrDefaultAsync();
    }
}
```

## 3. 前端实现规约 (Frontend - WPF)

### 3.1. MVVM 模式

*   **View (XAML)**：视图必须是“哑”的。除纯粹的UI交互动效（如动画）外，**严禁在Code-behind中编写任何业务逻辑**。所有状态和操作都应通过数据绑定到ViewModel。
*   **ViewModel**：包含所有UI状态（属性）和业务操作（命令）。**严禁直接引用任何UI控件**（如`TextBox`, `Button`）。ViewModel应是可独立测试的。
*   **Model**：通常指代从 `LYBT.Shared.Models` 中获取的DTO。

### 3.2. ViewModel 实现

*   **基类**：所有列表管理类ViewModel应继承自项目中定义的现代化基类（如`ModernManagementViewModel`），以复用通用功能。
*   **属性通知**：推荐使用 `CommunityToolkit.Mvvm` 包，通过 `[ObservableProperty]` 特性自动实现 `INotifyPropertyChanged`，减少样板代码。
*   **命令**：推荐使用 `[RelayCommand]` 特性自动生成 `ICommand` 实现。

**示例**:
```csharp
// using CommunityToolkit.Mvvm.ComponentModel;
// using CommunityToolkit.Mvvm.Input;

public partial class PatientDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string _patientName;

    [ObservableProperty]
    private bool _isLoading;

    private readonly IPatientService _patientService;

    public PatientDetailViewModel(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [RelayCommand]
    private async Task LoadPatientAsync(Guid patientId)
    {
        IsLoading = true;
        var patient = await _patientService.GetByIdAsync(patientId);
        PatientName = patient.Name;
        IsLoading = false;
    }
}
```

### 3.3. XAML 与视图

*   **资源**：所有跨视图共享的资源（样式、转换器、画刷等）**必须**定义在 `src/Client/Desktop/Shell/Resources/UnifiedDesignSystem.xaml` 中。**严禁**在视图或模块级别定义重复的通用资源。
*   **命名**：所有重要的、需要引用的控件都应使用 `x:Name` 命名，并遵循 `控件名+用途` 的驼峰式命名法（如 `PatientNameTextBox`, `SaveButton`）。
*   **数据绑定**：优先使用编译时检查更强的 `x:Bind`（如可用），否则使用标准的 `Binding`。

## 4. 编码风格与质量

*   **命名**：严格遵循.NET命名规范。私有字段使用 `_camelCase`。
*   **注释**：所有 `public` 的类、方法、属性都**必须**添加 `///` XML文档注释，说明其用途、参数和返回值。
*   **魔法字符串**：严禁在代码中使用“魔法字符串”（未经定义的字符串字面量）。对于属性名，使用 `nameof()`；对于固定的业务字符串，应定义在静态常量类中。
*   **代码分析**：必须处理所有由 StyleCop 和 Roslyn Analyzers 报告的编译警告。如需忽略，必须提供明确的 `#pragma warning disable` 及理由说明。
