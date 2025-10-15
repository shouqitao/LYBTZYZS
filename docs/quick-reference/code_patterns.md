# 代码模式

**更新时间**: 2025-10-15 18:11:07
**条目数量**: 18 个
**使用说明**: 快速查找常用解决方案，点击目录直接跳转

## 📋 快速目录

1. [```csharp](#1-```csharp)
2. [```csharp](#2-```csharp)
3. [_logger.LogError(ex, "患者创建失败，姓名: {Name}, 电话: {Phon...](#3-_logger.logerror(ex,-"患者创建失败，姓名:-{name},-电话:-{phon...)
4. [```csharp](#4-```csharp)
5. [```csharp](#5-```csharp)
6. [```csharp](#6-```csharp)
7. [```csharp](#7-```csharp)
8. [```csharp](#8-```csharp)
9. [IErrorHandlingService errorHandlingService)](#9-ierrorhandlingservice-errorhandlingservice))
10. [```csharp](#10-```csharp)
11. [```csharp](#11-```csharp)
12. [```csharp](#12-```csharp)
13. [```csharp](#13-```csharp)
14. [```csharp](#14-```csharp)
15. [```csharp](#15-```csharp)
16. [```csharp](#16-```csharp)
17. [```csharp](#17-```csharp)
18. [// 优化查询方法（解决N+1查询问题）](#18-//-优化查询方法（解决n+1查询问题）)

---

## 1. ```csharp

**解决方案**:
```csharp
// 示例：PatientsModule.cs
public class PatientsModule : IModule

**代码示例**:
```csharp
// 示例：PatientsModule.cs
public class PatientsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册服务
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 注册AutoMapper配置
        services.AddAutoMapper(typeof(PatientMappingProfile));
    }
}
```

**来源**: `module-dependencies.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 2. ```csharp

**解决方案**:
```csharp
public class SwaggerConfiguration
{

**代码示例**:
```csharp
public class SwaggerConfiguration
{
    public static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LYBT 中医诊所管理系统 API",
                Version = "v1",
                Description = "LYBT 中医诊所管理系统 RESTful API 文档"
            });

            // 添加JWT认证
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // 包含XML注释
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });
    }
}
```

**来源**: `module-integration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 3. _logger.LogError(ex, "患者创建失败，姓名: {Name}, 电话: {Phone}",

**解决方案**:
```csharp
// 日志配置 (appsettings.json)
{

**代码示例**:
```csharp
// 日志配置 (appsettings.json)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "LyBT": "Debug"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
    },
    "File": {
      "Path": "logs/lybt-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30,
      "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
    },
    "Seq": {
      "ServerUrl": "http://localhost:5341",
      "ApiKey": "your-api-key"
    }
  }
}

// 日志使用示例
public class PatientService
{
    private readonly ILogger<PatientService> _logger;
    
    public async Task<PatientDto> CreateAsync(CreatePatientDto createDto)
    {
        _logger.LogInformation("开始创建患者，姓名: {Name}, 电话: {Phone}", 
            createDto.Name, createDto.Phone);

        try
        {
            var patient = new Patient
            {
                Name = createDto.Name,
                Gender = createDto.Gender,
                BirthDate = createDto.BirthDate,
                Phone = createDto.Phone,
                Address = createDto.Address
            };

            var createdPatient = await _repository.AddAsync(patient);
            
            _logger.LogInformation("患者创建成功，ID: {PatientId}, 姓名: {Name}", 
                createdPatient.Id, createdPatient.Name);

            return _mapper.Map<PatientDto>(createdPatient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者创建失败，姓名: {Name}, 电话: {Phone}", 
                createDto.Name, createDto.Phone);
            throw;
        }
    }
}
```

**来源**: `monitoring-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 4. ```csharp

**解决方案**:
// 具体仓储实现
// 继承通用方法
// 添加特定查询方法

**代码示例**:
```csharp
// 基础仓储接口
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    IQueryable<T> Query();
}

// 具体仓储实现
public class MedicalCaseRepository : Repository<MedicalCase>, IMedicalCaseRepository
{
    // 继承通用方法
    // 添加特定查询方法
    public async Task<MedicalCase?> GetWithDetailsAsync(Guid id)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}
```

**来源**: `module-implementation-reality.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 5. ```csharp

**解决方案**:
```csharp
// LYBT.Desktop.Prescriptions/PrescriptionsModule.cs
namespace LYBT.Desktop.Prescriptions;

**代码示例**:
```csharp
// LYBT.Desktop.Prescriptions/PrescriptionsModule.cs
namespace LYBT.Desktop.Prescriptions;

[Module(ModuleName = nameof(PrescriptionsModule))]
[ModuleDependency("ConsultationModule")]
[ModuleDependency("HerbsModule")]
[ModuleDependency("FormulaModule")]
public class PrescriptionsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IPrescriptionRepository, PrescriptionRepository>();

        // 注册 ViewModel
        containerRegistry.Register<PrescriptionManagementViewModel>();
        containerRegistry.Register<PrescriptionsMainViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();
        containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();

        // 注册对话框
        containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
        containerRegistry.RegisterDialog<Views.HerbSelectionDialog, HerbSelectionDialogViewModel>();
    }
}
```

**来源**: `ADR-002-desktop-services-removal.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 6. ```csharp

**解决方案**:
```csharp
public interface I{Entity}Service
{

**代码示例**:
```csharp
public interface I{Entity}Service
{
    #region 查询操作 (2-4 methods)
    Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null
    );
    Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<{Entity}Dto>>> SearchAsync(string keyword);
    #endregion

    #region CRUD 操作 (3 methods)
    Task<ServiceResult<{Entity}Dto>> CreateAsync({Entity}CreateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<{Entity}Dto>> UpdateAsync(Guid id, {Entity}UpdateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id); // Soft delete
    #endregion

    #region 业务操作 (0-5 methods)
    // Entity-specific business methods
    // 示例：
    // Task<ServiceResult> DisableAsync(Guid id);
    // Task<ServiceResult> EnableAsync(Guid id);
    // Task<ServiceResult> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
    #endregion
}
```

**来源**: `ADR-004-service-interface-unified-design-standard.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 7. ```csharp

**解决方案**:
// INavigationAware实现
// IDestructible实现

**代码示例**:
```csharp
public abstract class OptimizedViewModelBase : BindableBase, INavigationAware, IDestructible
{
    protected IRegionManager RegionManager { get; }
    protected IEventAggregator EventAggregator { get; }

    // 构造函数注入（不使用Container.Resolve）
    protected OptimizedViewModelBase(
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        RegionManager = regionManager;
        EventAggregator = eventAggregator;
    }

    // INavigationAware实现
    public virtual void OnNavigatedTo(NavigationContext navigationContext) { }
    public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }
    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

    // IDestructible实现
    public virtual void Destroy()
    {
        // 清理资源
    }
}
```

**来源**: `PRISM_OPTIMIZATION_ULTRATHINK.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 8. ```csharp

**解决方案**:
// 实现

**代码示例**:
```csharp
// 定义全局命令接口
public interface IApplicationCommands
{
    CompositeCommand SaveAllCommand { get; }
    CompositeCommand RefreshAllCommand { get; }
}

// 实现
public class ApplicationCommands : IApplicationCommands
{
    public CompositeCommand SaveAllCommand { get; } = new CompositeCommand();
    public CompositeCommand RefreshAllCommand { get; } = new CompositeCommand();
}

// 在ViewModel中注册
public class PatientViewModel : OptimizedViewModelBase
{
    private readonly IApplicationCommands _applicationCommands;

    public PatientViewModel(IApplicationCommands applicationCommands)
    {
        _applicationCommands = applicationCommands;
        SaveCommand = new DelegateCommand(Save);

        // 注册到全局命令
        _applicationCommands.SaveAllCommand.RegisterCommand(SaveCommand);
    }

    public DelegateCommand SaveCommand { get; }

    private void Save()
    {
        // 保存逻辑
    }
}
```

**来源**: `PRISM_OPTIMIZATION_ULTRATHINK.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 9. IErrorHandlingService errorHandlingService)

**解决方案**:
/// TODO: 重构完成后重新实现业务逻辑

**代码示例**:
```csharp
/// <summary>
/// 登录视图模型 - 架构重构后简化版本
/// TODO: 重构完成后重新实现业务逻辑
/// </summary>
public class LoginViewModel : ModernViewModelBase
{
    public LoginViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IErrorHandlingService errorHandlingService)
        : base(eventAggregator, loggerFactory, errorHandlingService)
    {
    }
}
```

**来源**: `auth-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 10. ```csharp

**解决方案**:
// UI层认证接口 - 通过适配器实现

**代码示例**:
```csharp
// UI层认证接口 - 通过适配器实现
public interface IAuthenticationService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync();
    bool IsLoggedIn { get; }
    Task<UserDto?> GetCurrentUserAsync();
}
```

**来源**: `auth-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 11. ```csharp

**解决方案**:
// 待实现的服务接口

**代码示例**:
```csharp
// 待实现的服务接口
public interface IConsultationService
{
    Task<ServiceResult<List<ConsultationDto>>> GetConsultationsAsync();
    Task<ServiceResult<ConsultationDto>> GetConsultationByIdAsync(int id);
    Task<ServiceResult<ConsultationDto>> CreateConsultationAsync(CreateConsultationRequest request);
    Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(UpdateConsultationRequest request);
    Task<ServiceResult> DeleteConsultationAsync(int id);
}
```

**来源**: `consultation-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 12. ```csharp

**解决方案**:
// 待实现的服务接口

**代码示例**:
```csharp
// 待实现的服务接口
public interface IFormulaService
{
    Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync();
    Task<ServiceResult<FormulaDto>> GetFormulaByIdAsync(int id);
    Task<ServiceResult<FormulaDto>> CreateFormulaAsync(CreateFormulaRequest request);
    Task<ServiceResult<FormulaDto>> UpdateFormulaAsync(UpdateFormulaRequest request);
    Task<ServiceResult> DeleteFormulaAsync(int id);
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(int id);
}
```

**来源**: `formula-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 13. ```csharp

**解决方案**:
// 待实现的服务接口

**代码示例**:
```csharp
// 待实现的服务接口
public interface IHerbService
{
    Task<ServiceResult<List<HerbDto>>> GetHerbsAsync();
    Task<ServiceResult<HerbDto>> GetHerbByIdAsync(int id);
    Task<ServiceResult<HerbDto>> CreateHerbAsync(CreateHerbRequest request);
    Task<ServiceResult<HerbDto>> UpdateHerbAsync(UpdateHerbRequest request);
    Task<ServiceResult> DeleteHerbAsync(int id);
    Task<ServiceResult<List<HerbCategoryDto>>> GetCategoriesAsync();
}
```

**来源**: `herbs-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 14. ```csharp

**解决方案**:
// 待实现的服务接口

**代码示例**:
```csharp
// 待实现的服务接口
public interface IMedicalCaseService
{
    Task<ServiceResult<List<MedicalCaseDto>>> GetMedicalCasesAsync();
    Task<ServiceResult<MedicalCaseDto>> GetMedicalCaseByIdAsync(int id);
    Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(CreateMedicalCaseRequest request);
    Task<ServiceResult<MedicalCaseDto>> UpdateMedicalCaseAsync(UpdateMedicalCaseRequest request);
    Task<ServiceResult> DeleteMedicalCaseAsync(int id);
    Task<ServiceResult<List<MedicalCaseCategoryDto>>> GetCategoriesAsync();
}
```

**来源**: `medicalcase-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 15. ```csharp

**解决方案**:
// 待实现的服务接口

**代码示例**:
```csharp
// 待实现的服务接口
public interface IPatientService
{
    Task<ServiceResult<List<PatientDto>>> GetPatientsAsync();
    Task<ServiceResult<PatientDto>> GetPatientByIdAsync(int id);
    Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientRequest request);
    Task<ServiceResult<PatientDto>> UpdatePatientAsync(UpdatePatientRequest request);
    Task<ServiceResult> DeletePatientAsync(int id);
    Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(int patientId);
}
```

**来源**: `patients-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 16. ```csharp

**解决方案**:
// 待实现的服务接口

**代码示例**:
```csharp
// 待实现的服务接口
public interface IPrescriptionService
{
    Task<ServiceResult<List<PrescriptionDto>>> GetPrescriptionsAsync();
    Task<ServiceResult<PrescriptionDto>> GetPrescriptionByIdAsync(int id);
    Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionRequest request);
    Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(UpdatePrescriptionRequest request);
    Task<ServiceResult> DeletePrescriptionAsync(int id);
    Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();
}
```

**来源**: `prescriptions-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 17. ```csharp

**解决方案**:
// 基础CRUD - 已实现
// 扩展功能 - 已实现

**代码示例**:
```csharp
public interface IFormulaService
{
    // 基础CRUD - 已实现
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
    Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    
    // 扩展功能 - 已实现
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId);
}
```

**来源**: `formula-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 18. // 优化查询方法（解决N+1查询问题）

**解决方案**:
// 优化查询方法（解决N+1查询问题）
// 业务查询方法

**代码示例**:
```csharp
public interface IFormulaRepository : IRepository<Formula>
{
    // 优化查询方法（解决N+1查询问题）
    Task<List<Formula>> GetTemplatesAsync();
    Task<Formula> GetByIdWithHerbsAsync(Guid id);
    Task<PagedResult<Formula>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string keyword = null);
    
    // 业务查询方法
    Task<List<Formula>> GetByUserIdAsync(Guid userId);
    Task<List<Formula>> GetSharedFormulasAsync();
    Task<List<Formula>> GetByCategoryAsync(string category);
}
```

**来源**: `formula-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 💡 使用建议

- **快速查找**: 使用目录快速定位到具体问题
- **代码示例**: 所有代码示例都可以直接复制使用
- **相关问题**: 查看条目的来源文档获取更多详细信息
- **反馈建议**: 发现问题或有改进建议请及时反馈

