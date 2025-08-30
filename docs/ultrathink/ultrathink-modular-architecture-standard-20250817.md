# UltraThink前端模块化架构标准

> **项目**: 凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)  
> **日期**: 2025-08-17  
> **状态**: 🎯 设计中  
> **范围**: 基于UltraThink四层架构的前端模块化开发标准

## 🎯 设计目标

基于UltraThink四层架构（BaseModel→EntityModel→Dto→Info），建立统一的前端模块化开发标准，实现真正的**高内聚、低耦合**的模块化设计。

## 🏗️ UltraThink模块化架构核心原则

### 1. **模块独立性原则** 🎯
- 每个模块必须拥有独立的业务服务层
- 模块间通过标准接口通信，禁止直接依赖
- 模块内部实现细节对外部完全透明

### 2. **四层架构遵循原则** 📊
- 严格遵循Layer 4 (Info) → Layer 3 (Dto) → Layer 2 (EntityModel) → Layer 1 (BaseModel)
- 模块内ViewModels只使用Info模型，通过AutoMapper与Services层通信
- Services层处理业务逻辑，使用Dto与后端API通信

### 3. **统一结构原则** 🏛️
- 所有模块必须遵循统一的目录结构标准
- 统一的命名规范和文件组织方式
- 标准化的依赖注入和服务注册模式

## 📁 UltraThink标准模块结构

### 完整模块目录标准
```
ModuleName/
├── ModuleNameModule.cs               # 模块注册和配置 ⭐
├── Constants/                        # 模块常量定义
│   └── ModuleNameConstants.cs
├── Models/                           # 模块特定的Info模型
│   ├── ModuleNameInfo.cs
│   └── ModuleNameCreateInfo.cs
├── Services/                         # 模块业务服务层 ⭐
│   ├── Interfaces/
│   │   ├── IModuleNameService.cs
│   │   └── IModuleNameManager.cs
│   ├── ModuleNameService.cs
│   └── ModuleNameManager.cs
├── ViewModels/                       # 视图模型层
│   ├── Base/
│   │   └── BaseModuleNameViewModel.cs
│   ├── Components/
│   │   └── ModuleNameComponentViewModel.cs
│   ├── ModuleNameMainViewModel.cs
│   └── ModuleNameDialogViewModel.cs
├── Views/                           # 视图层
│   ├── ModuleNameMainView.xaml
│   ├── ModuleNameDialog.xaml
│   └── Controls/
│       └── ModuleNameControl.xaml
├── Mappings/                        # AutoMapper配置 ⭐
│   └── ModuleNameMappingProfile.cs
├── Resources/                       # 模块资源
│   ├── Styles/
│   │   └── ModuleNameStyles.xaml
│   └── Images/
└── Configuration/                   # 模块配置
    └── ModuleNameOptions.cs
```

### 📊 模块复杂度分级标准

| 级别 | 文件数量 | 结构要求 | 适用场景 |
|------|----------|----------|----------|
| **简单模块** | 5-15个 | 基础结构 | Auth, Users |
| **标准模块** | 15-30个 | 完整结构 | Patients, Herbs, Formula |
| **复杂模块** | 30-50个 | 完整结构+Components | Prescriptions, MedicalCase |
| **核心模块** | 50+个 | 完整结构+高级组件 | Consultation |

## 🎯 模块间通信架构

### 1. **事件驱动通信** 🔄
```csharp
// 模块间事件定义
public class PatientSelectedEvent : PubSubEvent<PatientSelectedEventArgs>
{
    public class PatientSelectedEventArgs
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime EventTime { get; set; }
    }
}

// 发布事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Publish(new PatientSelectedEventArgs { PatientId = patient.Id });

// 订阅事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);
```

### 2. **共享服务接口** 🌐
```csharp
// 定义模块间共享接口
public interface IPatientLookupService
{
    Task<IEnumerable<PatientInfo>> SearchPatientsAsync(string keyword);
    Task<PatientInfo> GetPatientByIdAsync(Guid id);
}

// 模块实现共享接口
public class PatientLookupService : IPatientLookupService
{
    // 实现细节...
}
```

### 3. **模块注册标准** ⚙️
```csharp
public class ModuleNameModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑
        ConfigureAutoMapper(containerProvider);
        RegisterEventHandlers(containerProvider);
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 注册模块内部服务
        containerRegistry.Register<IModuleNameService, ModuleNameService>();
        containerRegistry.Register<IModuleNameManager, ModuleNameManager>();
        
        // 2. 注册ViewModels
        containerRegistry.Register<ModuleNameMainViewModel>();
        containerRegistry.Register<ModuleNameDialogViewModel>();
        
        // 3. 注册导航
        containerRegistry.RegisterForNavigation<ModuleNameMainView, ModuleNameMainViewModel>();
        containerRegistry.RegisterForNavigation<ModuleNameDialog, ModuleNameDialogViewModel>();
        
        // 4. 注册共享接口实现（如果模块提供）
        containerRegistry.Register<IModuleNameLookupService, ModuleNameLookupService>();
    }
}
```

## 📦 标准化接口设计

### 1. **模块服务接口标准** 🎯
```csharp
// 标准的模块服务接口模式
public interface IModuleNameService
{
    // 基础CRUD操作
    Task<ServiceResult<PagedResult<ModuleNameInfo>>> GetPagedAsync(PagedQueryDto query);
    Task<ServiceResult<ModuleNameInfo>> GetByIdAsync(Guid id);
    Task<ServiceResult<ModuleNameInfo>> CreateAsync(ModuleNameCreateInfo createInfo);
    Task<ServiceResult<ModuleNameInfo>> UpdateAsync(ModuleNameUpdateInfo updateInfo);
    Task<ServiceResult> DeleteAsync(Guid id);
    
    // 状态管理
    Task<ServiceResult> EnableAsync(Guid id);
    Task<ServiceResult> DisableAsync(Guid id);
    
    // 业务特定操作
    Task<ServiceResult<IEnumerable<ModuleNameInfo>>> SearchAsync(string keyword);
    Task<ServiceResult> ValidateAsync(ModuleNameInfo info);
}
```

### 2. **ViewModel基类标准** 🏛️
```csharp
// 标准的模块ViewModel基类
public abstract class BaseModuleNameViewModel : BaseServiceManagementViewModel<ModuleNameInfo, IModuleNameService>
{
    protected readonly IMapper _mapper;
    protected readonly ICustomDialogService _dialogService;
    protected readonly IModuleNameService _moduleService;
    
    protected override string ModuleName => "模块名称";
    
    protected BaseModuleNameViewModel(
        IModuleNameService moduleService,
        IMapper mapper,
        ICustomDialogService dialogService,
        IEventAggregator eventAggregator)
        : base(moduleService, eventAggregator)
    {
        _moduleService = moduleService;
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }
    
    // UltraThink四层架构：标准的Info→Dto转换模式
    protected virtual TDto ConvertToDto<TDto>(ModuleNameInfo info) where TDto : class
    {
        return _mapper.Map<TDto>(info);
    }
    
    protected virtual ModuleNameInfo ConvertFromDto<TDto>(TDto dto) where TDto : class
    {
        return _mapper.Map<ModuleNameInfo>(dto);
    }
}
```

### 3. **AutoMapper配置标准** 🗺️
```csharp
// 标准的模块AutoMapper配置
public class ModuleNameMappingProfile : Profile
{
    public ModuleNameMappingProfile()
    {
        // Dto → Info 映射
        CreateMap<ModuleNameDto, ModuleNameInfo>()
            .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
            .ForMember(dest => dest.IsLoading, opt => opt.Ignore());
            
        // Info → CreateDto 映射
        CreateMap<ModuleNameCreateInfo, ModuleNameCreateDto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
            
        // Info → UpdateDto 映射
        CreateMap<ModuleNameUpdateInfo, ModuleNameUpdateDto>();
        
        // 特殊业务映射
        CreateMap<ModuleNameInfo, ModuleNameSearchDto>()
            .ForMember(dest => dest.SearchKeyword, opt => opt.MapFrom(src => src.Name));
    }
}
```

## 🔧 模块依赖管理策略

### 1. **依赖层次规则** 📊
```
Core Services (全局共享)
    ↓
Module Services (模块内部)
    ↓
ViewModels (视图逻辑)
    ↓
Views (UI层)
```

### 2. **允许的依赖关系** ✅
- **ViewModels** → **Module Services** ✅
- **Module Services** → **Core Services** ✅
- **Module Services** → **Shared Models** ✅
- **Modules** → **Events** (通过IEventAggregator) ✅

### 3. **禁止的依赖关系** ❌
- **Module A** → **Module B Services** ❌
- **ViewModels** → **Shared Services** (直接依赖) ❌
- **Module Services** → **Other Module Models** ❌
- **Views** → **Services** (任何服务) ❌

## 🎯 模块内聚性优化

### 1. **单一职责原则** 🎯
- 每个模块只负责一个核心业务领域
- 模块内的所有组件都为该业务领域服务
- 避免模块承担多个不相关的职责

### 2. **业务逻辑集中化** 🏢
```csharp
// 业务逻辑统一在Services层处理
public class PatientService : IPatientService
{
    private readonly IPatientApiService _apiService;
    private readonly IMapper _mapper;
    
    public async Task<ServiceResult<PatientInfo>> CreateAsync(PatientCreateInfo createInfo)
    {
        // 1. 业务验证
        var validationResult = await ValidatePatientInfoAsync(createInfo);
        if (!validationResult.IsSuccess)
            return ServiceResult<PatientInfo>.Failure(validationResult.ErrorMessage);
            
        // 2. 数据转换
        var createDto = _mapper.Map<PatientCreateDto>(createInfo);
        
        // 3. API调用
        var apiResult = await _apiService.CreateAsync(createDto);
        if (!apiResult.IsSuccess)
            return ServiceResult<PatientInfo>.Failure(apiResult.ErrorMessage);
            
        // 4. 结果转换
        var patientInfo = _mapper.Map<PatientInfo>(apiResult.Data);
        return ServiceResult<PatientInfo>.Success(patientInfo);
    }
}
```

### 3. **组件协作优化** 🤝
```csharp
// 模块内组件协作模式
public class PatientManagementViewModel : BasePatientViewModel
{
    private readonly IPatientValidationService _validationService;
    private readonly IPatientSearchService _searchService;
    private readonly IPatientExportService _exportService;
    
    // 组件协作通过Services层协调
    private async Task<bool> ValidateAndSave(PatientInfo patient)
    {
        // 1. 使用验证服务
        var validation = await _validationService.ValidateAsync(patient);
        if (!validation.IsSuccess) return false;
        
        // 2. 使用保存服务
        var saveResult = await _patientService.SaveAsync(patient);
        if (!saveResult.IsSuccess) return false;
        
        // 3. 触发模块内事件
        await NotifyPatientSavedAsync(patient);
        return true;
    }
}
```

## 📋 模块开发检查清单

### 🎯 必须实现的组件
- [ ] **ModuleNameModule.cs** - 模块注册类
- [ ] **IModuleNameService.cs** - 核心业务服务接口
- [ ] **ModuleNameService.cs** - 核心业务服务实现
- [ ] **ModuleNameInfo.cs** - 主要的Info模型
- [ ] **ModuleNameMappingProfile.cs** - AutoMapper配置
- [ ] **ModuleNameMainViewModel.cs** - 主视图模型
- [ ] **ModuleNameMainView.xaml** - 主视图

### ⭐ 推荐实现的组件
- [ ] **ModuleNameConstants.cs** - 模块常量
- [ ] **IModuleNameManager.cs** - 管理服务接口
- [ ] **BaseModuleNameViewModel.cs** - ViewModel基类
- [ ] **ModuleNameStyles.xaml** - 模块样式
- [ ] **ModuleNameOptions.cs** - 模块配置

### 🚀 高级组件（复杂模块）
- [ ] **Components/** - 业务组件目录
- [ ] **Controls/** - 自定义控件目录
- [ ] **Events/** - 模块事件定义
- [ ] **Validators/** - 业务验证器
- [ ] **Managers/** - 复杂业务管理器

## 📊 模块质量评估标准

### 1. **结构完整性评分** (40分)
- 基础目录结构完整 (10分)
- Services层完整 (10分)
- AutoMapper配置 (10分)
- 模块注册规范 (10分)

### 2. **架构合规性评分** (30分)
- UltraThink四层架构遵循 (15分)
- 依赖关系清晰 (10分)
- 接口设计标准 (5分)

### 3. **内聚性评分** (20分)
- 单一职责原则 (10分)
- 业务逻辑集中化 (10分)

### 4. **可维护性评分** (10分)
- 代码注释完整 (5分)
- 命名规范统一 (5分)

**总分100分，90分以上为优秀模块**

---

🧠 **Generated with UltraThink方法论** - 系统化模块架构设计，确保高质量可维护代码