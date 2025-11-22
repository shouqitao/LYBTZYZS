# 模块化设计指南

**基于凌隐宝堂中医诊所 8个业务模块的实际架构设计** - 深入理解模块化开发原则和最佳实践

## 🏗️ 模块化架构概览

### 模块化架构图
```
                    ┌─────────────────────────────────────┐
                    │           Core Layer               │
                    │      (共享核心和基础设施)            │
                    └─────────────────────────────────────┘
                                      │
            ┌─────────────────────┼─────────────────────┐
            │                     │                     │
    ┌───────▼───────┐   ┌─────────▼─────────┐   ┌───────▼───────┐
    │  Auth Module  │   │  Users Module    │   │Patients Module│
    │   (认证模块)   │   │  (用户管理模块)   │   │ (患者管理模块) │
    └───────────────┘   └───────────────────┘   └───────────────┘
            │                     │                     │
    ┌───────▼───────┐   ┌─────────▼─────────┐   ┌───────▼───────┐
    │MedicalCase    │   │ Consultation     │   │Prescriptions  │
    │   Module      │   │    Module        │   │    Module      │    │
    │  (医案模块)    │   │  (诊疗记录模块)   │   │  (处方管理模块) │
    └───────────────┘   └───────────────────┘   └───────────────┘
            │                     │                     │
    ┌───────▼───────┐   ┌─────────▼─────────┐
    │  Herbs Module │   │  Formula Module   │
    │  (药材模块)    │   │  (验方模块)       │
    └───────────────┘   └───────────────────┘
```

### 模块化设计原则

#### ✅ 核心原则
1. **高内聚**：模块内部功能紧密相关
2. **低耦合**：模块间依赖最小化
3. **单一职责**：每个模块负责一个业务领域
4. **开闭原则**：对扩展开放，对修改关闭
5. **接口隔离**：通过接口定义模块间契约

#### 📋 模块职责划分
- **Auth Module**: 身份认证和授权管理
- **Users Module**: 用户管理和权限控制
- **Patients Module**: 患者信息管理
- **MedicalCase Module**: 医案记录和状态管理
- **Consultation Module**: 诊疗记录和四诊信息
- **Prescriptions Module**: 处方管理和药材计算
- **Herbs Module**: 药材信息管理
- **Formula Module**: 验方模板管理

## 🧩 模块标准结构

### 1. Server端模块结构

#### 标准模块模板
```
LYBT.Module.{ModuleName}/
├── Interfaces/                        # 接口定义
│   ├── I{ModuleName}Service.cs       # 服务接口
│   ├── I{ModuleName}Repository.cs    # 仓储接口
│   └── Validators/                   # 验证器接口
│       └── I{ModuleName}Validator.cs
├── Services/                         # 服务实现
│   ├── {ModuleName}Service.cs        # 服务实现
│   └── Implementations/              # 具体实现
│       ├── {ModuleName}CreateHandler.cs
│       └── {ModuleName}UpdateHandler.cs
├── Repositories/                     # 仓储实现
│   └── {ModuleName}Repository.cs     # 仓储实现类
├── DTOs/                            # 数据传输对象
│   ├── {ModuleName}Dto.cs           # 查询DTO
│   ├── {ModuleName}CreateDto.cs     # 创建DTO
│   ├── {ModuleName}UpdateDto.cs     # 更新DTO
│   ├── {ModuleName}SearchDto.cs     # 搜索DTO
│   └── Validators/                  # 验证器
│       ├── {ModuleName}CreateValidator.cs
│       ├── {ModuleName}UpdateValidator.cs
│       └── {ModuleName}SearchValidator.cs
├── Entities/                        # 实体定义（可选）
│   ├── {ModuleName}.cs             # 主实体
│   └── SubEntities/                # 子实体
│       ├── {ModuleName}Item.cs
│       └── {ModuleName}History.cs
├── Enums/                          # 枚举定义
│   ├── {ModuleName}Status.cs       # 状态枚举
│   ├── {ModuleName}Type.cs         # 类型枚举
│   └── {ModuleName}Category.cs     # 分类枚举
├── Events/                         # 领域事件
│   ├── {ModuleName}CreatedEvent.cs
│   ├── {ModuleName}UpdatedEvent.cs
│   └── {ModuleName}DeletedEvent.cs
├── Exceptions/                     # 自定义异常
│   └── {ModuleName}Exception.cs
├── Extensions/                     # 扩展方法
│   └── {ModuleName}Extensions.cs
├── Configuration/                  # 配置类
│   ├── {ModuleName}Options.cs
│   └── {ModuleName}Configuration.cs
├── Mappings/                       # AutoMapper配置
│   └── {ModuleName}MappingProfile.cs
├── Tests/                          # 测试项目
│   ├── Unit/                       # 单元测试
│   │   ├── Services/
│   │   └── Repositories/
│   └── Integration/                # 集成测试
└── {ModuleName}.Module.cs          # 模块注册类
```

#### 实际模块示例：Patients Module
```
LYBT.Module.Patients/
├── Interfaces/
│   ├── IPatientService.cs
│   ├── IPatientRepository.cs
│   └── Validators/
│       └── IPatientValidator.cs
├── Services/
│   ├── PatientService.cs
│   └── Implementations/
│       ├── PatientCreateHandler.cs
│       ├── PatientUpdateHandler.cs
│       └── PatientImportHandler.cs
├── Repositories/
│   └── PatientRepository.cs
├── DTOs/
│   ├── PatientDto.cs
│   ├── PatientCreateDto.cs
│   ├── PatientUpdateDto.cs
│   ├── PatientSearchDto.cs
│   ├── PatientImportDto.cs
│   └── Validators/
│       ├── PatientCreateValidator.cs
│       ├── PatientUpdateValidator.cs
│       └── PatientImportValidator.cs
├── Entities/
│   ├── Patient.cs
│   └── SubEntities/
│       └── PatientHistory.cs
├── Enums/
│   ├── PatientStatus.cs
│   ├── PatientGender.cs
│   └── PatientCategory.cs
├── Events/
│   ├── PatientCreatedEvent.cs
│   ├── PatientUpdatedEvent.cs
│   └── PatientDeletedEvent.cs
├── Extensions/
│   └── PatientExtensions.cs
├── Configuration/
│   ├── PatientOptions.cs
│   └── PatientConfiguration.cs
├── Mappings/
│   └── PatientMappingProfile.cs
└── Tests/
    ├── Unit/
    │   ├── Services/
    │   │   └── PatientServiceTests.cs
    │   └── Repositories/
    │       └── PatientRepositoryTests.cs
    └── Integration/
        └── PatientIntegrationTests.cs
```

### 2. Client端模块结构

#### WPF客户端模块模板
```
LYBT.Desktop.{ModuleName}/
├── Views/                           # 视图
│   ├── {ModuleName}ManagementView.xaml      # 管理主视图
│   ├── {ModuleName}DetailView.xaml          # 详情视图
│   ├── {ModuleName}CreateView.xaml          # 创建视图
│   ├── {ModuleName}EditView.xaml            # 编辑视图
│   └── Components/                     # 自定义组件
│       ├── {ModuleName}Card.xaml
│       └── {ModuleName}SearchBox.xaml
├── ViewModels/                      # 视图模型
│   ├── {ModuleName}ManagementViewModel.cs   # 管理主ViewModel
│   ├── {ModuleName}DetailViewModel.cs       # 详情ViewModel
│   ├── {ModuleName}CreateViewModel.cs       # 创建ViewModel
│   ├── {ModuleName}EditViewModel.cs         # 编辑ViewModel
│   └── Base/                          # 基础ViewModel
│       ├── Base{ModuleName}ViewModel.cs
│       └── Base{ModuleName}ListViewModel.cs
├── Models/                          # 模型
│   ├── {ModuleName}Model.cs               # 主模型
│   ├── {ModuleName}FilterModel.cs         # 筛选模型
│   ├── {ModuleName}SearchModel.cs         # 搜索模型
│   └── {ModuleName}StatisticsModel.cs     # 统计模型
├── Converters/                      # 值转换器
│   ├── {ModuleName}StatusConverter.cs
│   ├── {ModuleName}TypeConverter.cs
│   └── {ModuleName}DateConverter.cs
├── Commands/                        # 命令
│   ├── Save{ModuleName}Command.cs
│   ├── Delete{ModuleName}Command.cs
│   └── Import{ModuleName}Command.cs
├── Validators/                      # 验证器
│   ├── {ModuleName}Validator.cs
│   └── {ModuleName}CreateValidator.cs
├── Services/                        # 模块服务
│   ├── I{ModuleName}Service.cs
│   ├── I{ModuleName}NavigationService.cs
│   └── Local/                        # 本地服务
│       └── {ModuleName}CacheService.cs
├── Resources/                       # 资源
│   ├── Styles/                       # 样式
│   │   ├── {ModuleName}Styles.xaml
│   │   └── {ModuleName}ButtonStyles.xaml
│   ├── Templates/                    # 模板
│   │   ├── {ModuleName}DataTemplate.xaml
│   │   └── {ModuleName}ControlItemTemplate.xaml
│   ├── Images/                       # 图片
│   │   ├── {ModuleName}.png
│   │   └── {ModuleName}_icon.png
│   └── Strings/                      # 本地化字符串
│       └── {ModuleName}Resources.resx
├── Behaviors/                       # 行为
│   ├── {ModuleName}ValidationBehavior.cs
│   └── {ModuleName}SearchBehavior.cs
├── Controls/                        # 自定义控件
│   ├── {ModuleName}DataGrid.cs
│   └── {ModuleName}SearchControl.cs
└── {ModuleName}Module.cs            # 模块注册类
```

## 🔄 模块间通信机制

### 1. 接口通信

#### 跨模块服务接口
```csharp
/// <summary>
/// 模块间通信接口定义
/// </summary>
public interface IModuleCommunicationService
{
    /// <summary>
    /// 获取患者基本信息（供其他模块调用）
    /// </summary>
    Task<ServiceResult<PatientBasicInfoDto>> GetPatientBasicInfoAsync(Guid patientId);

    /// <summary>
    /// 获取药材库存信息（供处方模块调用）
    /// </summary>
    Task<ServiceResult<HerbInventoryDto>> GetHerbInventoryAsync(Guid herbId);

    /// <summary>
    /// 验证处方药材配伍（供药材模块调用）
    /// </summary>
    Task<ServiceResult<CompatibilityCheckResult>> ValidateHerbCompatibilityAsync(List<Guid> herbIds);
}

/// <summary>
/// 模块通信服务实现
/// </summary>
public class ModuleCommunicationService : IModuleCommunicationService
{
    private readonly IPatientService _patientService;
    private readonly IHerbService _herbService;
    private readonly IPrescriptionService _prescriptionService;

    public ModuleCommunicationService(
        IPatientService patientService,
        IHerbService herbService,
        IPrescriptionService prescriptionService)
    {
        _patientService = patientService;
        _herbService = herbService;
        _prescriptionService = prescriptionService;
    }

    public async Task<ServiceResult<PatientBasicInfoDto>> GetPatientBasicInfoAsync(Guid patientId)
    {
        try
        {
            var result = await _patientService.GetByIdAsync(patientId);
            if (!result.IsSuccess)
                return ServiceResult<PatientBasicInfoDto>.Failure(result.Message);

            var basicInfo = new PatientBasicInfoDto
            {
                Id = result.Data!.Id,
                Name = result.Data.Name,
                Gender = result.Data.Gender,
                PhoneNumber = result.Data.PhoneNumber,
                Age = result.Data.Age
            };

            return ServiceResult<PatientBasicInfoDto>.Success(basicInfo);
        }
        catch (Exception ex)
        {
            return ServiceResult<PatientBasicInfoDto>.Failure("获取患者基本信息失败");
        }
    }

    public async Task<ServiceResult<HerbInventoryDto>> GetHerbInventoryAsync(Guid herbId)
    {
        try
        {
            var result = await _herbService.GetByIdAsync(herbId);
            if (!result.IsSuccess)
                return ServiceResult<HerbInventoryDto>.Failure(result.Message);

            var inventory = new HerbInventoryDto
            {
                HerbId = result.Data!.Id,
                HerbName = result.Data.Name,
                Unit = result.Data.Unit,
                CurrentStock = result.Data.Stock ?? 0,
                UnitPrice = result.Data.Price,
                Status = result.Data.Status
            };

            return ServiceResult<HerbInventoryDto>.Success(inventory);
        }
        catch (Exception ex)
        {
            return ServiceResult<HerbInventoryDto>.Failure("获取药材库存信息失败");
        }
    }

    public async Task<ServiceResult<CompatibilityCheckResult>> ValidateHerbCompatibilityAsync(List<Guid> herbIds)
    {
        try
        {
            // 获取所有药材信息
            var herbs = new List<HerbDto>();
            foreach (var herbId in herbIds)
            {
                var result = await _herbService.GetByIdAsync(herbId);
                if (result.IsSuccess)
                {
                    herbs.Add(result.Data!);
                }
            }

            // 执行配伍验证逻辑
            var compatibilityResult = await _prescriptionService.ValidateHerbCompatibilityAsync(herbs);
            
            return ServiceResult<CompatibilityCheckResult>.Success(compatibilityResult);
        }
        catch (Exception ex)
        {
            return ServiceResult<CompatibilityCheckResult>.Failure("药材配伍验证失败");
        }
    }
}
```

### 2. 事件驱动通信

#### 领域事件定义
```csharp
/// <summary>
/// 患者创建事件
/// </summary>
public class PatientCreatedEvent
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public PatientCreatedEvent(Guid patientId, string patientName, string phoneNumber)
    {
        PatientId = patientId;
        PatientName = patientName;
        PhoneNumber = phoneNumber;
        CreatedAt = DateTime.Now;
    }
}

/// <summary>
/// 医案状态变更事件
/// </summary>
public class MedicalCaseStatusChangedEvent
{
    public Guid MedicalCaseId { get; set; }
    public Guid PatientId { get; set; }
    public MedicalCaseStatus OldStatus { get; set; }
    public MedicalCaseStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }

    public MedicalCaseStatusChangedEvent(Guid medicalCaseId, Guid patientId, 
        MedicalCaseStatus oldStatus, MedicalCaseStatus newStatus, string changedBy)
    {
        MedicalCaseId = medicalCaseId;
        PatientId = patientId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        ChangedAt = DateTime.Now;
    }
}

/// <summary>
/// 处方创建事件
/// </summary>
public class PrescriptionCreatedEvent
{
    public Guid PrescriptionId { get; set; }
    public Guid MedicalCaseId { get; set; }
    public Guid PatientId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<Guid> HerbIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public PrescriptionCreatedEvent(Guid prescriptionId, Guid medicalCaseId, 
        Guid patientId, decimal totalAmount, List<Guid> herbIds)
    {
        PrescriptionId = prescriptionId;
        MedicalCaseId = medicalCaseId;
        PatientId = patientId;
        TotalAmount = totalAmount;
        HerbIds = herbIds;
        CreatedAt = DateTime.Now;
    }
}
```

#### 事件处理器实现
```csharp
/// <summary>
/// 患者事件处理器
/// </summary>
public class PatientEventHandler :
    IEventHandler<PatientCreatedEvent>,
    IEventHandler<PatientUpdatedEvent>,
    IEventHandler<PatientDeletedEvent>
{
    private readonly ILogger<PatientEventHandler> _logger;
    private readonly IEventStore _eventStore;

    public PatientEventHandler(ILogger<PatientEventHandler> logger, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    public async Task HandleAsync(PatientCreatedEvent @event)
    {
        try
        {
            _logger.LogInformation("处理患者创建事件: {PatientId}, {PatientName}", 
                @event.PatientId, @event.PatientName);

            // 保存事件到事件存储
            await _eventStore.SaveEventAsync(@event);

            // 触发相关业务逻辑
            await TriggerPatientCreatedWorkflows(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理患者创建事件失败: {PatientId}", @event.PatientId);
            throw;
        }
    }

    public async Task HandleAsync(PatientUpdatedEvent @event)
    {
        try
        {
            _logger.LogInformation("处理患者更新事件: {PatientId}", @event.PatientId);
            await _eventStore.SaveEventAsync(@event);
            await TriggerPatientUpdatedWorkflows(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理患者更新事件失败: {PatientId}", @event.PatientId);
            throw;
        }
    }

    public async Task HandleAsync(PatientDeletedEvent @event)
    {
        try
        {
            _logger.LogInformation("处理患者删除事件: {PatientId}", @event.PatientId);
            await _eventStore.SaveEventAsync(@event);
            await TriggerPatientDeletedWorkflows(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理患者删除事件失败: {PatientId}", @event.PatientId);
            throw;
        }
    }

    private async Task TriggerPatientCreatedWorkflows(PatientCreatedEvent @event)
    {
        // 触发创建欢迎消息
        // 触发初始化患者档案
        // 触发通知相关医护人员
        await Task.CompletedTask;
    }

    private async Task TriggerPatientUpdatedWorkflows(PatientUpdatedEvent @event)
    {
        // 触发更新相关统计信息
        // 触发同步到其他系统
        await Task.CompletedTask;
    }

    private async Task TriggerPatientDeletedWorkflows(PatientDeletedEvent @event)
    {
        // 触发清理相关数据
        // 触发归档患者记录
        await Task.CompletedTask;
    }
}
```

### 3. 共享数据访问

#### 跨模块数据访问
```csharp
/// <summary>
/// 跨模块数据访问服务
/// </summary>
public interface ICrossModuleDataService
{
    /// <summary>
    /// 获取患者相关统计信息
    /// </summary>
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync(Guid patientId);

    /// <summary>
    /// 获取医案相关处方信息
    /// </summary>
    Task<ServiceResult<List<PrescriptionSummaryDto>>> GetPrescriptionsByMedicalCaseAsync(Guid medicalCaseId);

    /// <summary>
    /// 获取药材使用频率统计
    /// </summary>
    Task<ServiceResult<List<HerbUsageFrequencyDto>>> GetHerbUsageFrequencyAsync(DateTime startDate, DateTime endDate);
}

/// <summary>
/// 跨模块数据访问服务实现
/// </summary>
public class CrossModuleDataService : ICrossModuleDataService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CrossModuleDataService> _logger;

    public CrossModuleDataService(AppDbContext dbContext, ILogger<CrossModuleDataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync(Guid patientId)
    {
        try
        {
            var patient = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId);
            
            if (patient == null)
                return ServiceResult<PatientStatisticsDto>.Failure("患者不存在");

            var medicalCasesCount = await _dbContext.MedicalCases
                .CountAsync(mc => mc.PatientId == patientId);

            var prescriptionsCount = await _dbContext.Prescriptions
                .Join(_dbContext.MedicalCases, p => p.MedicalCaseId, mc => mc.Id, 
                    (p, mc) => new { Prescription = p, MedicalCase = mc })
                .CountAsync(x => x.MedicalCase.PatientId == patientId);

            var totalAmount = await _dbContext.Prescriptions
                .Join(_dbContext.MedicalCases, p => p.MedicalCaseId, mc => mc.Id,
                    (p, mc) => new { Prescription = p, MedicalCase = mc })
                .Where(x => x.MedicalCase.PatientId == patientId)
                .SumAsync(x => x.Prescription.TotalAmount);

            var statistics = new PatientStatisticsDto
            {
                PatientId = patientId,
                PatientName = patient.Name,
                MedicalCasesCount = medicalCasesCount,
                PrescriptionsCount = prescriptionsCount,
                TotalAmount = totalAmount,
                FirstVisitDate = patient.CreatedAt,
                LastVisitDate = await GetLastVisitDateAsync(patientId)
            };

            return ServiceResult<PatientStatisticsDto>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者统计信息失败，PatientId: {PatientId}", patientId);
            return ServiceResult<PatientStatisticsDto>.Failure("获取患者统计信息失败");
        }
    }

    public async Task<ServiceResult<List<PrescriptionSummaryDto>>> GetPrescriptionsByMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            var prescriptions = await _dbContext.Prescriptions
                .Where(p => p.MedicalCaseId == medicalCaseId)
                .Select(p => new PrescriptionSummaryDto
                {
                    Id = p.Id,
                    PrescriptionNo = p.PrescriptionNo,
                    Indication = p.Indication,
                    DosageCount = p.DosageCount,
                    TotalAmount = p.TotalAmount,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    HerbCount = _dbContext.PrescriptionItems.Count(pi => pi.PrescriptionId == p.Id)
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<PrescriptionSummaryDto>>.Success(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医案处方信息失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<List<PrescriptionSummaryDto>>.Failure("获取医案处方信息失败");
        }
    }

    public async Task<ServiceResult<List<HerbUsageFrequencyDto>>> GetHerbUsageFrequencyAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var herbUsage = await _dbContext.PrescriptionItems
                .Join(_dbContext.Prescriptions, pi => pi.PrescriptionId, p => p.Id,
                    (pi, p) => new { PrescriptionItem = pi, Prescription = p })
                .Where(x => x.Prescription.CreatedAt >= startDate && x.Prescription.CreatedAt <= endDate)
                .GroupBy(x => new { x.PrescriptionItem.HerbId, x.PrescriptionItem.HerbName })
                .Select(g => new HerbUsageFrequencyDto
                {
                    HerbId = g.Key.HerbId,
                    HerbName = g.Key.HerbName,
                    UsageCount = g.Count(),
                    TotalQuantity = g.Sum(x => x.PrescriptionItem.Quantity),
                    TotalAmount = g.Sum(x => x.PrescriptionItem.UnitPrice * x.PrescriptionItem.Quantity * x.Prescription.DosageCount)
                })
                .OrderByDescending(x => x.UsageCount)
                .Take(50)
                .ToListAsync();

            return ServiceResult<List<HerbUsageFrequencyDto>>.Success(herbUsage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药材使用频率统计失败");
            return ServiceResult<List<HerbUsageFrequencyDto>>.Failure("获取药材使用频率统计失败");
        }
    }

    private async Task<DateTime?> GetLastVisitDateAsync(Guid patientId)
    {
        return await _dbContext.MedicalCases
            .Where(mc => mc.PatientId == patientId)
            .OrderByDescending(mc => mc.CreatedAt)
            .Select(mc => (DateTime?)mc.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
```

## 🔧 模块注册与配置

### 1. 模块注册类

#### 标准模块注册实现
```csharp
/// <summary>
/// 患者模块注册类
/// </summary>
public class PatientModule : Module
{
    public override void Load(ContainerBuilder builder)
    {
        // 注册服务
        builder.RegisterType<PatientService>()
               .As<IPatientService>()
               .InstancePerLifetimeScope();

        builder.RegisterType<PatientRepository>()
               .As<IPatientRepository>()
               .InstancePerLifetimeScope();

        // 注册验证器
        builder.RegisterType<PatientCreateValidator>()
               .As<IValidator<PatientCreateDto>>()
               .InstancePerDependency();

        builder.RegisterType<PatientUpdateValidator>()
               .As<IValidator<PatientUpdateDto>>()
               .InstancePerDependency();

        // 注册事件处理器
        builder.RegisterType<PatientEventHandler>()
               .As<IEventHandler<PatientCreatedEvent>>()
               .As<IEventHandler<PatientUpdatedEvent>>()
               .As<IEventHandler<PatientDeletedEvent>>()
               .InstancePerDependency();

        // 注册跨模块服务
        builder.RegisterType<ModuleCommunicationService>()
               .As<IModuleCommunicationService>()
               .SingleInstance();

        // 注册配置
        builder.Register(c =>
        {
            var configuration = c.Resolve<IConfiguration>();
            var options = new PatientOptions();
            configuration.GetSection("PatientOptions").Bind(options);
            return options;
        }).As<PatientOptions>().SingleInstance();
    }
}

/// <summary>
/// 医案模块注册类
/// </summary>
public class MedicalCaseModule : Module
{
    public override void Load(ContainerBuilder builder)
    {
        // 注册服务
        builder.RegisterType<MedicalCaseService>()
               .As<IMedicalCaseService>()
               .InstancePerLifetimeScope();

        builder.RegisterType<MedicalCaseRepository>()
               .As<IMedicalCaseRepository>()
               .InstancePerLifetimeScope();

        // 注册状态机
        builder.RegisterType<MedicalCaseStateMachine>()
               .As<IMedicalCaseStateMachine>()
               .SingleInstance();

        // 注册事件处理器
        builder.RegisterType<MedicalCaseEventHandler>()
               .As<IEventHandler<MedicalCaseCreatedEvent>>()
               .As<IEventHandler<MedicalCaseStatusChangedEvent>>()
               .InstancePerDependency();

        // 注册工作流服务
        builder.RegisterType<MedicalCaseWorkflowService>()
               .As<IMedicalCaseWorkflowService>()
               .InstancePerLifetimeScope();
    }
}
```

### 2. 模块配置

#### 模块选项配置
```csharp
/// <summary>
/// 患者模块配置选项
/// </summary>
public class PatientOptions
{
    /// <summary>
    /// 默认分页大小
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// 最大分页大小
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Excel导入最大文件大小（MB）
    /// </summary>
    public int MaxImportFileSizeMB { get; set; } = 10;

    /// <summary>
    /// 是否启用患者搜索缓存
    /// </summary>
    public bool EnableSearchCache { get; set; } = true;

    /// <summary>
    /// 搜索缓存过期时间（分钟）
    /// </summary>
    public int SearchCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 患者名称最大长度
    /// </summary>
    public int MaxNameLength { get; set; } = 50;

    /// <summary>
    /// 手机号码正则表达式
    /// </summary>
    public string PhoneNumberRegex { get; set; } = @"^1[3-9]\d{9}$";

    /// <summary>
    /// 身份证号正则表达式
    /// </summary>
    public string IdNumberRegex { get; set; } = @"^\d{17}[\dX]$";
}

/// <summary>
/// 医案模块配置选项
/// </summary>
public class MedicalCaseOptions
{
    /// <summary>
    /// 是否启用医案编号自动生成
    /// </summary>
    public bool EnableAutoNumberGeneration { get; set; } = true;

    /// <summary>
    /// 医案编号前缀
    /// </summary>
    public string NumberPrefix { get; set; } = "MC";

    /// <summary>
    /// 医案状态自动转移间隔（小时）
    /// </summary>
    public int AutoStatusTransitionHours { get; set; } = 24;

    /// <summary>
    /// 是否启用医案工作流
    /// </summary>
    public bool EnableWorkflow { get; set; } = true;

    /// <summary>
    /// 医案归档天数
    /// </summary>
    public int ArchiveDays { get; set; } = 365;
}
```

#### 配置文件集成
```json
// appsettings.json
{
  "PatientOptions": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100,
    "MaxImportFileSizeMB": 10,
    "EnableSearchCache": true,
    "SearchCacheExpirationMinutes": 30,
    "MaxNameLength": 50,
    "PhoneNumberRegex": "^1[3-9]\\d{9}$",
    "IdNumberRegex": "^\\d{17}[\\dX]$"
  },
  "MedicalCaseOptions": {
    "EnableAutoNumberGeneration": true,
    "NumberPrefix": "MC",
    "AutoStatusTransitionHours": 24,
    "EnableWorkflow": true,
    "ArchiveDays": 365
  },
  "PrescriptionOptions": {
    "DefaultDosageCount": 7,
    "MaxDosageCount": 30,
    "EnablePriceCalculation": true,
    "DefaultDiscount": 1.0,
    "MaxDiscount": 0.5
  }
}
```

## 🧪 模块测试策略

### 1. 单元测试

#### 服务层测试模板
```csharp
/// <summary>
/// 患者服务单元测试
/// </summary>
[TestFixture]
public class PatientServiceTests
{
    private Mock<IPatientRepository> _mockRepository;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<PatientService>> _mockLogger;
    private Mock<IValidator<PatientCreateDto>> _mockCreateValidator;
    private PatientService _patientService;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IPatientRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PatientService>>();
        _mockCreateValidator = new Mock<IValidator<PatientCreateDto>>();

        _patientService = new PatientService(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockCreateValidator.Object,
            null); // 简化测试，其他验证器省略
    }

    [Test]
    public async Task CreateAsync_ValidPatient_ReturnsSuccess()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "测试患者",
            PhoneNumber = "13800138000",
            Gender = Gender.Male
        };

        var validationResult = new ValidationResult();
        _mockCreateValidator.Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(validationResult);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            PhoneNumber = createDto.PhoneNumber,
            Gender = createDto.Gender,
            CreatedAt = DateTime.Now
        };

        var patientDto = new PatientDto
        {
            Id = patient.Id,
            Name = patient.Name,
            PhoneNumber = patient.PhoneNumber,
            Gender = patient.Gender
        };

        _mockMapper.Setup(m => m.Map<Patient>(createDto)).Returns(patient);
        _mockRepository.Setup(r => r.GetByPhoneAsync(createDto.PhoneNumber)).ReturnsAsync((Patient?)null);
        _mockRepository.Setup(r => r.AddAsync(patient)).ReturnsAsync(patient);
        _mockMapper.Setup(m => m.Map<PatientDto>(patient)).Returns(patientDto);

        // Act
        var result = await _patientService.CreateAsync(createDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(createDto.Name, result.Data.Name);
        Assert.AreEqual(createDto.PhoneNumber, result.Data.PhoneNumber);

        _mockRepository.Verify(r => r.GetByPhoneAsync(createDto.PhoneNumber), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(patient), Times.Once);
    }

    [Test]
    public async Task CreateAsync_DuplicatePhoneNumber_ReturnsFailure()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "测试患者",
            PhoneNumber = "13800138000",
            Gender = Gender.Male
        };

        var validationResult = new ValidationResult();
        _mockCreateValidator.Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(validationResult);

        var existingPatient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "已有患者",
            PhoneNumber = createDto.PhoneNumber
        };

        _mockRepository.Setup(r => r.GetByPhoneAsync(createDto.PhoneNumber)).ReturnsAsync(existingPatient);

        // Act
        var result = await _patientService.CreateAsync(createDto);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("该手机号已被使用", result.Message);

        _mockRepository.Verify(r => r.GetByPhoneAsync(createDto.PhoneNumber), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Never);
    }

    [Test]
    public async Task GetPagedAsync_ValidParameters_ReturnsPagedResult()
    {
        // Arrange
        int page = 1;
        int pageSize = 20;
        string keyword = "测试";

        var patients = new List<Patient>
        {
            new() { Id = Guid.NewGuid(), Name = "测试患者1", PhoneNumber = "13800138001" },
            new() { Id = Guid.NewGuid(), Name = "测试患者2", PhoneNumber = "13800138002" }
        };

        var pagedResult = new PagedResult<Patient>
        {
            Items = patients,
            TotalCount = patients.Count,
            CurrentPage = page,
            PageSize = pageSize
        };

        var patientDtos = patients.Select(p => new PatientDto
        {
            Id = p.Id,
            Name = p.Name,
            PhoneNumber = p.PhoneNumber
        }).ToList();

        _mockRepository.Setup(r => r.GetPagedAsync(page, pageSize, keyword))
                     .ReturnsAsync(pagedResult);
        _mockMapper.Setup(m => m.Map<List<PatientDto>>(patients))
                 .Returns(patientDtos);

        // Act
        var result = await _patientService.GetPagedAsync(page, pageSize, keyword);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(patients.Count, result.Data.Items.Count);
        Assert.AreEqual(page, result.Data.CurrentPage);
        Assert.AreEqual(pageSize, result.Data.PageSize);

        _mockRepository.Verify(r => r.GetPagedAsync(page, pageSize, keyword), Times.Once);
    }
}
```

### 2. 集成测试

#### 模块集成测试模板
```csharp
/// <summary>
/// 患者模块集成测试
/// </summary>
[TestFixture]
public class PatientModuleIntegrationTests
{
    private AppDbContext _dbContext;
    private IPatientService _patientService;
    private IPatientRepository _patientRepository;
    private IMapper _mapper;

    [SetUp]
    public void Setup()
    {
        // 配置内存数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        
        // 确保数据库创建
        _dbContext.Database.EnsureCreated();

        // 配置AutoMapper
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new PatientMappingProfile());
        });
        _mapper = mappingConfig.CreateMapper();

        // 创建服务实例
        _patientRepository = new PatientRepository(_dbContext, NullLogger<PatientRepository>.Instance);
        _patientService = new PatientService(
            _patientRepository,
            _mapper,
            NullLogger<PatientService>.Instance,
            new PatientCreateValidator(),
            new PatientUpdateValidator());
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
    }

    [Test]
    public async Task CreatePatient_CompleteWorkflow_Success()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "集成测试患者",
            Gender = Gender.Male,
            PhoneNumber = "13900139000",
            BirthDate = new DateTime(1990, 1, 1),
            IdNumber = "110101199001011234",
            Address = "测试地址"
        };

        // Act
        var createResult = await _patientService.CreateAsync(createDto);

        // Assert
        Assert.IsTrue(createResult.IsSuccess, "创建患者应该成功");
        Assert.IsNotNull(createResult.Data, "返回的患者数据不应为空");
        Assert.AreEqual(createDto.Name, createResult.Data.Name, "患者姓名应该一致");
        Assert.AreEqual(createDto.PhoneNumber, createResult.Data.PhoneNumber, "手机号码应该一致");

        // 验证数据库中的数据
        var patientFromDb = await _patientRepository.GetByIdAsync(createResult.Data.Id);
        Assert.IsNotNull(patientFromDb, "数据库中应该存在该患者");
        Assert.AreEqual(createDto.Name, patientFromDb.Name, "数据库中的姓名应该一致");

        // 测试查询功能
        var getResult = await _patientService.GetByIdAsync(createResult.Data.Id);
        Assert.IsTrue(getResult.IsSuccess, "查询患者应该成功");
        Assert.AreEqual(createDto.Name, getResult.Data.Name, "查询的患者姓名应该一致");

        // 测试更新功能
        var updateDto = new PatientUpdateDto
        {
            Name = "更新后的患者名称",
            Gender = Gender.Female,
            PhoneNumber = createDto.PhoneNumber,
            Address = "更新后的地址"
        };

        var updateResult = await _patientService.UpdateAsync(createResult.Data.Id, updateDto);
        Assert.IsTrue(updateResult.IsSuccess, "更新患者应该成功");
        Assert.AreEqual(updateDto.Name, updateResult.Data.Name, "更新后的姓名应该一致");

        // 测试删除功能
        var deleteResult = await _patientService.DeleteAsync(createResult.Data.Id);
        Assert.IsTrue(deleteResult.IsSuccess, "删除患者应该成功");

        // 验证删除后无法查询
        var deletedPatient = await _patientRepository.GetByIdAsync(createResult.Data.Id);
        Assert.IsNull(deletedPatient, "删除后患者应该不存在");
    }

    [Test]
    public async Task SearchPatient_CompleteWorkflow_Success()
    {
        // Arrange
        var patients = new List<PatientCreateDto>
        {
            new() { Name = "张三", PhoneNumber = "13800138001", Gender = Gender.Male },
            new() { Name = "李四", PhoneNumber = "13800138002", Gender = Gender.Female },
            new() { Name = "王五", PhoneNumber = "13800138003", Gender = Gender.Male }
        };

        // 创建测试数据
        foreach (var patientDto in patients)
        {
            await _patientService.CreateAsync(patientDto);
        }

        // Act & Assert - 按姓名搜索
        var searchResult = await _patientService.SearchAsync("张");
        Assert.IsTrue(searchResult.IsSuccess, "按姓名搜索应该成功");
        Assert.AreEqual(1, searchResult.Data.Count, "应该找到1个患者");
        Assert.AreEqual("张三", searchResult.Data[0].Name, "找到的患者姓名应该正确");

        // Act & Assert - 按手机号搜索
        searchResult = await _patientService.SearchAsync("13800138002");
        Assert.IsTrue(searchResult.IsSuccess, "按手机号搜索应该成功");
        Assert.AreEqual(1, searchResult.Data.Count, "应该找到1个患者");
        Assert.AreEqual("李四", searchResult.Data[0].Name, "找到的患者姓名应该正确");

        // Act & Assert - 搜索不存在的结果
        searchResult = await _patientService.SearchAsync("不存在的姓名");
        Assert.IsTrue(searchResult.IsSuccess, "搜索不存在的患者应该成功");
        Assert.AreEqual(0, searchResult.Data.Count, "应该找到0个患者");
    }
}
```

## 🎯 模块化最佳实践

### ✅ 推荐做法

1. **模块边界清晰**
   - 每个模块有明确的职责范围
   - 接口定义在模块内部
   - 避免跨模块直接访问实现

2. **依赖注入优先**
   - 通过接口进行模块间通信
   - 使用DI容器管理依赖关系
   - 支持模块的独立测试

3. **事件驱动架构**
   - 使用领域事件解耦模块
   - 异步事件处理
   - 事件溯源和重放

4. **配置外部化**
   - 模块配置独立管理
   - 支持环境差异化配置
   - 运行时配置更新

### ❌ 避免做法

1. **循环依赖**
   - 模块间相互引用
   - 通过接口间接循环依赖
   - 静态类耦合

2. **过度抽象**
   - 不必要的接口抽象
   - 过度的设计模式
   - 复杂的继承层次

3. **硬编码依赖**
   - 模块内部硬编码配置
   - 直接实例化其他模块类
   - 静态方法调用

4. **数据库耦合**
   - 跨模块直接访问数据库表
   - 共享数据库连接
   - 绕过仓储层

### 📊 模块化成熟度评估

#### Level 1: 基础模块化
- [ ] 按业务功能划分模块
- [ ] 模块间接口定义清晰
- [ ] 基础的依赖注入配置

#### Level 2: 高级模块化
- [ ] 事件驱动架构实现
- [ ] 模块独立部署能力
- [ ] 完整的测试覆盖

#### Level 3: 企业级模块化
- [ ] 微服务架构支持
- [ ] 动态模块加载
- [ ] 完整的监控和治理

---

## 🔄 模块演进策略

### 1. 模块拆分策略

#### 单体到模块化演进
```
阶段1: 识别业务边界
├── 分析现有代码结构
├── 识别业务领域
└── 定义模块职责

阶段2: 接口抽象
├── 定义模块间接口
├── 实现依赖注入
└── 重构现有代码

阶段3: 物理分离
├── 创建独立模块项目
├── 迁移相关代码
└── 配置模块注册

阶段4: 测试验证
├── 单元测试覆盖
├── 集成测试验证
└── 性能测试评估
```

### 2. 模块治理

#### 模块质量标准
- **代码质量**: 遵循编码规范，圈复杂度适中
- **测试覆盖**: 单元测试覆盖率 > 80%
- **性能指标**: 响应时间 < 200ms
- **依赖管理**: 无循环依赖，依赖方向正确
- **文档完整**: 接口文档和使用示例

#### 模块监控指标
- **API响应时间**
- **错误率统计**
- **并发用户数**
- **资源使用情况**
- **业务指标统计**

---

*此模块化设计指南基于凌隐宝堂中医诊所项目的8个实际业务模块编写，为模块化开发提供完整的指导原则和最佳实践。*