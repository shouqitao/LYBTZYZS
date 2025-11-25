# LYBT.Desktop.MedicalCase - 医案管理模块

## 📦 项目定位

- **层级**:Client端
- **类型**:业务模块(医案管理)
- **职责**:作为整个诊疗流程的**容器和编排中心**，负责创建、跟踪和管理一次完整的就诊记录（即"医案"），将患者信息、四诊记录、处方等关联起来。提供多步骤诊疗流程导航、病案状态管理、暂存/继续功能、历史病案查询等完整功能。采用Prism MVVM架构，通过`INavigationAware`接口实现跨模块协作，确保诊疗流程的完整性和可追溯性。

##  代码结构

```
LYBT.Desktop.MedicalCase/
├── Converters/                            # 值转换器(1个)
│   └── InvertedBoolConverter.cs          # 布尔值反转转换器
├── Interfaces/                            # 接口定义(3个)
│   ├── IMedicalCaseRepository.cs         # 医案仓储接口(20个方法)
│   ├── ISaveable.cs                      # 可保存接口(步骤ViewModel契约)
│   └── IValidatable.cs                   # 可验证接口(步骤ViewModel契约)
├── Models/                                # 数据模型(3个)
│   ├── ConsultationStep.cs               # 诊疗步骤枚举
│   ├── FlowStep.cs                       # 流程步骤元数据
│   └── MedicalCaseItem.cs                # 医案列表项
├── Repositories/                          # 数据仓储实现(1个)
│   └── MedicalCaseRepository.cs          # 医案仓储实现(20个方法)
├── Services/                              # 组件化服务(Epic #1773 + Epic #2175 Phase 2)
│   ├── MedicalCaseDataManager.cs         # 医案数据管理组件
│   ├── MedicalCaseFlowManager.cs         # 医案流程管理组件
│   ├── MedicalCaseLifecycleHandler.cs    # 医案生命周期处理组件
│   ├── MedicalCaseDataLoader.cs          # 医案数据加载组件
│   ├── PrescriptionEditorHerbFilterManager.cs # 处方药材过滤管理组件
│   ├── PrescriptionEditorValidator.cs    # 处方验证组件
│   ├── PrescriptionCalculator.cs         # 处方价格计算组件
│   ├── FormulaImportHandler.cs           # 经验方导入处理组件
│   └── HerbSelectionManager.cs           # 药材选择管理组件
├── Controls/                              # 自定义控件(Epic #2175 BF-002)
│   └── HerbCardControl.xaml/.xaml.cs     # 处方药材卡片控件
├── ViewModels/                            # MVVM视图模型(9个)
│   ├── CompletionViewModel.cs            # 完成视图模型
│   ├── MedicalCaseDetailViewModel.cs     # 医案详情视图模型
│   ├── MedicalCaseFlowViewModel.cs       # 流程编排核心
│   ├── MedicalCaseFormViewModel.cs       # 一体化病案编辑器ViewModel (Epic #2175 BF-002)
│   ├── MedicalCaseManagementViewModel.cs # 医案管理视图模型
│   ├── PrescriptionEditorViewModel.cs    # 处方编辑视图模型
│   ├── PrescriptionItemViewModel.cs      # 处方药材项ViewModel (Epic #2175 BF-002)
│   ├── FormulaSelectionDialogViewModel.cs # 经验方选择对话框ViewModel
│   ├── HistoryPrescriptionSelectionDialogViewModel.cs # 历史处方选择对话框ViewModel
│   └── DuplicateHerbAlertDialogViewModel.cs # 重复药材聚合提醒对话框ViewModel
├── Views/                                 # WPF视图(7对14个文件 + 3个对话框)
│   ├── CompletionView.xaml/.xaml.cs
│   ├── MedicalCaseDetailView.xaml/.xaml.cs
│   ├── MedicalCaseEditorView.xaml/.xaml.cs # 一体化病案编辑器View (Epic #2175 BF-002)
│   ├── MedicalCaseFlowView.xaml/.xaml.cs
│   ├── MedicalCaseManagementView.xaml/.xaml.cs
│   ├── PrescriptionEditorView.xaml/.xaml.cs
│   ├── FormulaSelectionDialog.xaml/.xaml.cs
│   ├── HistoryPrescriptionSelectionDialog.xaml/.xaml.cs
│   └── DuplicateHerbAlertDialog.xaml/.xaml.cs
└── MedicalCaseModule.cs                   # Prism模块注册

总计: 9个目录，约50个文件
- 1个Converter
- 3个Interface
- 3个Model
- 1个Repository
- 9个组件化Service
- 1个自定义Control
- 9个ViewModel
- 7对View(14个文件) + 3个对话框(6个文件)
- 1个Module配置
```

**说明**:
- **Epic #2175 BF-002核心功能**: 一体化病案编辑器（MedicalCaseFormViewModel + MedicalCaseEditorView），整合诊断录入和处方开具到单一界面
- **PrescriptionItemViewModel**: 7级拼音过滤算法 + 性能优化（缓存小写字符串，避免重复ToLower()）
- **组件化架构**: Epic #1773引入9个Service组件，实现ViewModel轻量化，单一职责原则
- **对话框功能**: 经验方导入、历史处方导入、重复药材聚合提醒
- **IMedicalCaseRepository**: 20个方法，覆盖医案CRUD、诊断/处方管理、暂存/继续等完整功能
- **ISaveable/IValidatable**: 步骤ViewModel契约接口，确保流程一致性

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Contracts** - ViewModel基类与数据契约
2. **LYBT.Desktop.Presentation** - 共享UI组件(MessageBox、对话框服务)
3. **LYBT.Desktop.Infrastructure** - Navigation服务、事件聚合器
4. **LYBT.Desktop.Foundation** - BaseApiRepository与IApiService
5. **LYBT.Shared.Models** - MedicalCaseDto、ConsultationDto、PrescriptionDto等
6. **Prism.Core/Prism.Wpf** - MVVM框架与区域导航

### 被依赖项目
1. **LYBT.Desktop.Shell** - 通过Prism模块化系统加载MedicalCase模块
2. **LYBT.Desktop.Patients** - 患者选择后导航到MedicalCaseFlow
3. **LYBT.Desktop.Consultation** - 诊断步骤子模块(Step 1)
4. **LYBT.Desktop.Prescriptions** - 处方步骤子模块(Step 2)

### NuGet包
- **Prism.DryIoc** (8.x) - MVVM框架、模块化、依赖注入
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件库
- **Microsoft.Extensions.Logging** (8.0.x) - 日志框架

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF (Windows Presentation Foundation)**: Windows桌面UI框架
- **Prism.DryIoc 8.x**: MVVM框架、模块化、依赖注入、区域导航
- **MaterialDesignThemes 5.1.x**: Material Design UI组件库
- **INavigationAware**: Prism导航生命周期接口
- **DelegateCommand/AsyncDelegateCommand**: Prism命令模式
- **ObservableCollection**: 数据绑定集合
- **Repository Pattern**: 三层架构(ViewModel → Repository → ApiClient)
- **异步编程**: 全异步方法(async/await)提升UI响应性

##  快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj
```

**集成说明**:

### 1. Shell加载MedicalCase模块

```csharp
// Shell/App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // WhenAvailable延迟加载,首次导航到MedicalCase时加载
    moduleCatalog.AddModule<MedicalCaseModule>(
        InitializationMode.WhenAvailable
    );
}
```

### 2. IMedicalCaseRepository核心接口

| 方法名 | 返回类型 | 说明 |
|-------|---------|------|
| **基础CRUD** | | |
| `GetPagedAsync(pageIndex, pageSize)` | `Task<PagedResult<MedicalCaseDto>>` | 分页查询医案 |
| `GetByIdAsync(id)` | `Task<MedicalCaseDto?>` | 按ID查询医案 |
| `CreateAsync(dto)` | `Task<MedicalCaseDto>` | 创建医案 |
| `UpdateAsync(id, dto)` | `Task<MedicalCaseDto>` | 更新医案 |
| `DeleteAsync(id)` | `Task` | 删除医案 |
| **详情与查询** | | |
| `GetByPatientIdAsync(patientId)` | `Task<List<MedicalCaseDto>>` | 按患者ID查询医案列表 |
| `CreateWithDetailsAsync(dto)` | `Task<MedicalCaseDto>` | 创建医案并返回详情 |
| `GetByIdWithDetailsAsync(id)` | `Task<MedicalCaseDetailDto?>` | 查询医案详情(含诊断+处方) |
| `QueryAsync(dto)` | `Task<List<MedicalCaseDto>>` | 多条件查询医案 |
| **诊断管理** | | |
| `UpdateConsultationAsync(caseId, dto)` | `Task<ConsultationDto>` | 更新诊断记录 |
| `CompleteStep1Async(caseId, dto)` | `Task<ConsultationFlowResult>` | 完成Step 1诊断步骤 |
| `ResetConsultationStepsAsync(caseId)` | `Task` | 重置诊断步骤 |
| **处方管理** | | |
| `CreatePrescriptionAsync(caseId, dto)` | `Task<PrescriptionDto>` | 创建处方 |
| `UpdatePrescriptionAsync(caseId, prescriptionId, dto)` | `Task<PrescriptionDto>` | 更新处方 |
| `DeletePrescriptionAsync(caseId, prescriptionId)` | `Task` | 删除处方 |
| `ClearPrescriptionAsync(caseId)` | `Task` | 清空处方 |
| `ImportFormulaIntoPrescriptionAsync(caseId, formulaId)` | `Task<PrescriptionDto>` | 从验方导入处方 |
| **暂存与继续** | | |
| `SaveAsDraftAsync(id)` | `Task<MedicalCaseDto>` | 暂存医案(状态:InProgress) |
| `GetUnfinishedCaseByPatientIdAsync(patientId)` | `Task<MedicalCaseDto?>` | 查询患者未完成医案 |
| `CloseCaseAsync(id)` | `Task` | 关闭医案(状态:Completed) |

**共20个方法，覆盖医案完整生命周期管理**

### 3. MedicalCaseFlowViewModel核心属性与方法

**核心属性**(13个):

| 属性名 | 类型 | 说明 |
|-------|------|------|
| `CurrentStep` | `ConsultationStep` | 当前步骤(Step1诊断/Step2处方/Step3完成) |
| `CurrentStepViewModel` | `object?` | 当前步骤ViewModel(动态切换) |
| `CurrentStepText` | `string` | 当前步骤文本(诊断录入/处方开具/完成) |
| `MedicalCaseId` | `Guid` | 当前医案ID |
| `CurrentPatient` | `PatientDto?` | 当前患者信息 |
| `SelectedPatientName` | `string` | 患者姓名(顶部显示) |
| `SelectedPatientInfo` | `string` | 患者信息(性别/年龄) |
| `CanGoBack` | `bool` | 是否可返回上一步 |
| `CanGoNext` | `bool` | 是否可进入下一步 |
| `NextButtonText` | `string` | 下一步按钮文本 |
| `PreviousButtonText` | `string` | 上一步按钮文本 |
| `PatientInfoBarVisible` | `bool` | 患者信息栏是否可见 |
| `NextStepCommand` | `DelegateCommand` | 下一步命令 |
| `PreviousStepCommand` | `DelegateCommand` | 上一步命令 |
| `SaveDraftCommand` | `AsyncDelegateCommand` | 暂存命令 |
| `CancelCommand` | `AsyncDelegateCommand` | 取消命令 |
| `BackToHomeCommand` | `DelegateCommand` | 返回主页命令 |

**核心方法**(14个):

| 方法名 | 返回类型 | 说明 |
|-------|---------|------|
| **流程控制** | | |
| `ExecuteNextStepAsync()` | `Task` | 执行下一步(验证→保存→导航) |
| `ExecutePreviousStep()` | `void` | 执行上一步(导航到前一步骤) |
| `CanExecuteNextStep()` | `bool` | 下一步是否可用(验证当前步骤) |
| `CanExecutePreviousStep()` | `bool` | 上一步是否可用(Step1禁用) |
| `NavigateToStep(step)` | `void` | 导航到指定步骤 |
| `UpdateCurrentStepText()` | `void` | 更新步骤文本 |
| **医案操作** | | |
| `CreateMedicalCaseAsync()` | `Task` | 创建医案(患者选择后) |
| `ExecuteSaveDraft()` | `Task` | 暂存医案(保存所有步骤数据) |
| `ExecuteCancel()` | `Task` | 取消操作(返回患者选择) |
| `UpdateMedicalCaseStatusAsync(status)` | `Task` | 更新医案状态 |
| **导航生命周期** | | |
| `OnNavigatedTo(context)` | `Task` | 导航到(加载医案数据) |
| `IsNavigationTarget(context)` | `bool` | 是否重用ViewModel |
| `OnNavigatedFrom(context)` | `void` | 导航离开(清理资源) |
| **事件处理** | | |
| `OnPrescriptionCompleted(result)` | `Task` | 处方完成事件处理 |

### 4. 患者列表加载与分页

```csharp
// MedicalCaseListViewModel.cs - 医案列表加载
using LYBT.Desktop.Contracts;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models;

public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    private readonly IMedicalCaseRepository _repository;

    public ObservableCollection<MedicalCaseItem> MedicalCases { get; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // 构造函数注入Repository
    public MedicalCaseListViewModel(IMedicalCaseRepository repository)
    {
        _repository = repository;
    }

    // 加载当前页医案
    private async Task LoadCurrentPageAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // 调用Repository分页查询
            var result = await _repository.GetPagedAsync(CurrentPage, PageSize);

            if (result != null)
            {
                MedicalCases.Clear();

                foreach (var medicalCase in result.Items)
                {
                    // 转换为列表项
                    MedicalCases.Add(new MedicalCaseItem
                    {
                        Id = medicalCase.Id,
                        PatientName = medicalCase.PatientName,
                        Status = medicalCase.Status,
                        CreatedAt = medicalCase.CreatedAt,
                        Doctor = medicalCase.DoctorName
                    });
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载医案列表失败");
            SetErrorMessage($"加载失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 上一页/下一页命令
    public DelegateCommand PreviousPageCommand => new DelegateCommand(
        async () => { CurrentPage--; await LoadCurrentPageAsync(); },
        () => CurrentPage > 1
    );

    public DelegateCommand NextPageCommand => new DelegateCommand(
        async () => { CurrentPage++; await LoadCurrentPageAsync(); },
        () => CurrentPage < TotalPages
    );
}
```

### 5. 3步诊疗流程编排(核心功能)

```csharp
// MedicalCaseFlowViewModel.cs - 流程编排核心
using LYBT.Desktop.Contracts;
using LYBT.Desktop.MedicalCase.Interfaces;
using Prism.Regions;

public class MedicalCaseFlowViewModel : UnifiedViewModelBase, INavigationAware
{
    private readonly IContainerProvider _containerProvider;
    private readonly IRegionManager _regionManager;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // 当前步骤(Step1诊断/Step2处方/Step3完成)
    private ConsultationStep _currentStep = ConsultationStep.Step1Consultation;
    public ConsultationStep CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                // 步骤切换时更新UI
                UpdateCurrentStepText();
                RaisePropertyChanged(nameof(CanGoBack));
                RaisePropertyChanged(nameof(CanGoNext));
                RaisePropertyChanged(nameof(NextButtonText));
                RaisePropertyChanged(nameof(PreviousButtonText));

                // 加载对应步骤的ViewModel
                NavigateToStep(_currentStep);
            }
        }
    }

    // 当前步骤ViewModel(动态切换)
    private object? _currentStepViewModel;
    public object? CurrentStepViewModel
    {
        get => _currentStepViewModel;
        set => SetProperty(ref _currentStepViewModel, value);
    }

    // 下一步命令
    public AsyncDelegateCommand NextStepCommand { get; }

    public MedicalCaseFlowViewModel(
        IContainerProvider containerProvider,
        IRegionManager regionManager,
        IMedicalCaseRepository medicalCaseRepository)
    {
        _containerProvider = containerProvider;
        _regionManager = regionManager;
        _medicalCaseRepository = medicalCaseRepository;

        // 初始化命令
        NextStepCommand = new AsyncDelegateCommand(
            ExecuteNextStepAsync,
            CanExecuteNextStep
        );
        PreviousStepCommand = new DelegateCommand(
            ExecutePreviousStep,
            CanExecutePreviousStep
        );
        SaveDraftCommand = new AsyncDelegateCommand(ExecuteSaveDraft);
        CancelCommand = new AsyncDelegateCommand(ExecuteCancel);
        BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
    }

    // 执行下一步(验证→保存→导航)
    private async Task ExecuteNextStepAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // Step 1: 验证当前步骤
            if (CurrentStepViewModel is ISaveable saveable)
            {
                if (CurrentStepViewModel is IValidatable validatable)
                {
                    if (!validatable.Validate())
                    {
                        SetWarningMessage("请完成必填项");
                        return;
                    }
                }

                // Step 2: 保存当前步骤数据
                await saveable.SaveAsync();
            }

            // Step 3: 导航到下一步骤
            switch (CurrentStep)
            {
                case ConsultationStep.Step1Consultation:
                    CurrentStep = ConsultationStep.Step2Prescription;
                    break;

                case ConsultationStep.Step2Prescription:
                    // 可选跳过处方,直接完成
                    CurrentStep = ConsultationStep.Step3Completion;
                    break;

                case ConsultationStep.Step3Completion:
                    // 完成医案,关闭并导航回患者列表
                    await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Completed);
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行下一步失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 执行上一步
    private void ExecutePreviousStep()
    {
        switch (CurrentStep)
        {
            case ConsultationStep.Step2Prescription:
                CurrentStep = ConsultationStep.Step1Consultation;
                break;

            case ConsultationStep.Step3Completion:
                CurrentStep = ConsultationStep.Step2Prescription;
                break;
        }
    }

    // 下一步是否可用(验证当前步骤)
    private bool CanExecuteNextStep()
    {
        if (CurrentStepViewModel is IValidatable validatable)
        {
            return validatable.Validate();
        }

        return true; // 无验证要求则默认可用
    }

    // 上一步是否可用(Step1禁用)
    private bool CanExecutePreviousStep()
    {
        return CurrentStep != ConsultationStep.Step1Consultation;
    }

    // 导航到指定步骤(动态加载ViewModel)
    private void NavigateToStep(ConsultationStep step)
    {
        switch (step)
        {
            case ConsultationStep.Step1Consultation:
                CurrentStepViewModel = _containerProvider.Resolve<MedicalCaseConsultationViewModel>();
                break;

            case ConsultationStep.Step2Prescription:
                CurrentStepViewModel = _containerProvider.Resolve<PrescriptionEditorViewModel>();
                break;

            case ConsultationStep.Step3Completion:
                CurrentStepViewModel = _containerProvider.Resolve<CompletionViewModel>();
                break;
        }

        // 如果ViewModel需要医案ID,通过属性注入
        if (CurrentStepViewModel is IMedicalCaseContext context)
        {
            context.MedicalCaseId = MedicalCaseId;
        }
    }
}
```

### 6. 暂存/继续医案功能

```csharp
// MedicalCaseFlowViewModel.cs - 暂存医案
private async Task ExecuteSaveDraft()
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // Step 1: 保存当前步骤数据
        if (CurrentStepViewModel is ISaveable saveable)
        {
            await saveable.SaveAsync();
        }

        // Step 2: 更新医案状态为InProgress(暂存)
        await _medicalCaseRepository.SaveAsDraftAsync(MedicalCaseId);

        SetSuccessMessage("医案已暂存");

        // Step 3: 导航回患者列表
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "暂存医案失败");
        SetErrorMessage($"暂存失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

// PatientSelectionViewModel.cs - 继续医案
private async Task ContinueConsultationAsync(Guid medicalCaseId)
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // Step 1: 加载医案详情
        var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(medicalCaseId);

        if (medicalCase != null)
        {
            // Step 2: 导航到流程视图,传递医案ID
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", medicalCaseId },
                { "PatientId", medicalCase.PatientId },
                { "IsContinue", true } // 标记为继续模式
            };

            _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "继续医案失败");
        SetErrorMessage($"继续失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

// MedicalCaseFlowViewModel.cs - OnNavigatedTo处理继续模式
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    var isContinue = navigationContext.Parameters.GetValue<bool>("IsContinue");

    if (isContinue)
    {
        // 继续模式:加载医案详情并恢复到上次步骤
        MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");

        var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

        if (medicalCase != null)
        {
            // 根据医案数据判断当前步骤
            if (medicalCase.Prescription != null)
            {
                CurrentStep = ConsultationStep.Step3Completion;
            }
            else if (medicalCase.Consultation != null)
            {
                CurrentStep = ConsultationStep.Step2Prescription;
            }
            else
            {
                CurrentStep = ConsultationStep.Step1Consultation;
            }
        }
    }
    else
    {
        // 新建模式:创建医案并从Step1开始
        await CreateMedicalCaseAsync();
        CurrentStep = ConsultationStep.Step1Consultation;
    }
}
```

### 7. MedicalCaseRepository实现

```csharp
// MedicalCaseRepository.cs - Repository层实现
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models;

public class MedicalCaseRepository : BaseApiRepository, IMedicalCaseRepository
{
    public MedicalCaseRepository(IApiService apiService, ILogger<MedicalCaseRepository> logger)
        : base(apiService, logger)
    {
    }

    // 分页查询医案
    public async Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int pageIndex, int pageSize)
    {
        var url = $"/api/v1/medical-cases?pageIndex={pageIndex}&pageSize={pageSize}";
        return await GetAsync<PagedResult<MedicalCaseDto>>(url);
    }

    // 按ID查询医案
    public async Task<MedicalCaseDto?> GetByIdAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}";
        return await GetAsync<MedicalCaseDto>(url);
    }

    // 创建医案
    public async Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto)
    {
        var url = "/api/v1/medical-cases";
        return await PostAsync<CreateMedicalCaseDto, MedicalCaseDto>(url, dto);
    }

    // 更新医案
    public async Task<MedicalCaseDto> UpdateAsync(Guid id, UpdateMedicalCaseDto dto)
    {
        var url = $"/api/v1/medical-cases/{id}";
        return await PutAsync<UpdateMedicalCaseDto, MedicalCaseDto>(url, dto);
    }

    // 删除医案
    public async Task DeleteAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}";
        await DeleteAsync(url);
    }

    // 查询医案详情(含诊断+处方)
    public async Task<MedicalCaseDetailDto?> GetByIdWithDetailsAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}/details";
        return await GetAsync<MedicalCaseDetailDto>(url);
    }

    // 更新诊断记录
    public async Task<ConsultationDto> UpdateConsultationAsync(Guid caseId, UpdateConsultationDto dto)
    {
        var url = $"/api/v1/medical-cases/{caseId}/consultation";
        return await PutAsync<UpdateConsultationDto, ConsultationDto>(url, dto);
    }

    // 创建处方
    public async Task<PrescriptionDto> CreatePrescriptionAsync(Guid caseId, CreatePrescriptionDto dto)
    {
        var url = $"/api/v1/medical-cases/{caseId}/prescriptions";
        return await PostAsync<CreatePrescriptionDto, PrescriptionDto>(url, dto);
    }

    // 暂存医案(状态:InProgress)
    public async Task<MedicalCaseDto> SaveAsDraftAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}/draft";
        return await PutAsync<object, MedicalCaseDto>(url, new { });
    }

    // 查询患者未完成医案
    public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
    {
        var url = $"/api/v1/medical-cases/patients/{patientId}/unfinished";
        return await GetAsync<MedicalCaseDto>(url);
    }

    // 关闭医案(状态:Completed)
    public async Task CloseCaseAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}/close";
        await PutAsync<object, MedicalCaseDto>(url, new { });
    }
}
```

### 8. ISaveable/IValidatable接口契约

```csharp
// ISaveable.cs - 可保存接口(步骤ViewModel契约)
namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 可保存接口,用于步骤ViewModel实现
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 保存当前步骤数据到服务器
    /// </summary>
    Task SaveAsync();
}

// IValidatable.cs - 可验证接口(步骤ViewModel契约)
namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 可验证接口,用于步骤ViewModel实现
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// 验证当前步骤数据是否完整
    /// </summary>
    /// <returns>true=验证通过, false=验证失败</returns>
    bool Validate();
}

// MedicalCaseConsultationViewModel.cs - 诊断ViewModel实现接口
public class MedicalCaseConsultationViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    private readonly IMedicalCaseRepository _repository;
    public Guid MedicalCaseId { get; set; }

    // 诊断数据属性
    public string ChiefComplaint { get; set; } = string.Empty;
    public string PresentIllness { get; set; } = string.Empty;
    public string Inspection { get; set; } = string.Empty; // 望诊
    public string Auscultation { get; set; } = string.Empty; // 闻诊
    public string Inquiry { get; set; } = string.Empty; // 问诊
    public string Palpation { get; set; } = string.Empty; // 切诊
    public string TcmDiagnosis { get; set; } = string.Empty;

    // 验证数据完整性
    public bool Validate()
    {
        // 必填项:主诉+中医诊断
        if (string.IsNullOrWhiteSpace(ChiefComplaint))
        {
            SetWarningMessage("请输入主诉");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TcmDiagnosis))
        {
            SetWarningMessage("请输入中医诊断");
            return false;
        }

        return true;
    }

    // 保存诊断数据
    public async Task SaveAsync()
    {
        try
        {
            var dto = new UpdateConsultationDto
            {
                ChiefComplaint = ChiefComplaint,
                PresentIllness = PresentIllness,
                Inspection = Inspection,
                Auscultation = Auscultation,
                Inquiry = Inquiry,
                Palpation = Palpation,
                TcmDiagnosis = TcmDiagnosis
            };

            await _repository.UpdateConsultationAsync(MedicalCaseId, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存诊断失败");
            throw;
        }
    }
}
```

### 9. MedicalCaseFlowView XAML绑定

```xml
<!-- MedicalCaseFlowView.xaml -->
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseFlowView"
             xmlns:materialDesign="http://materialdesigninxaml.net/wprism"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 患者信息栏 -->
            <RowDefinition Height="Auto"/> <!-- 步骤指示器 -->
            <RowDefinition Height="*"/>    <!-- 步骤内容区 -->
            <RowDefinition Height="Auto"/> <!-- 按钮栏 -->
        </Grid.RowDefinitions>

        <!-- 患者信息栏 -->
        <Border Grid.Row="0" Background="{DynamicResource MaterialDesignPaper}"
                Padding="16" Margin="0,0,0,8"
                Visibility="{Binding PatientInfoBarVisible, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="患者:" FontSize="14" Margin="0,0,8,0"/>
                <TextBlock Text="{Binding SelectedPatientName}" FontWeight="Bold" FontSize="14" Margin="0,0,16,0"/>
                <TextBlock Text="{Binding SelectedPatientInfo}" FontSize="14" Foreground="Gray"/>
            </StackPanel>
        </Border>

        <!-- 步骤指示器 -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,0,0,16">
            <!-- Step 1: 诊断录入 -->
            <Border Background="{Binding CurrentStep, Converter={StaticResource StepToColorConverter}, ConverterParameter=Step1}"
                    CornerRadius="20" Padding="16,8" Margin="0,0,8,0">
                <TextBlock Text="1. 诊断录入" Foreground="White"/>
            </Border>

            <materialDesign:PackIcon Kind="ChevronRight" VerticalAlignment="Center" Margin="8,0"/>

            <!-- Step 2: 处方开具 -->
            <Border Background="{Binding CurrentStep, Converter={StaticResource StepToColorConverter}, ConverterParameter=Step2}"
                    CornerRadius="20" Padding="16,8" Margin="0,0,8,0">
                <TextBlock Text="2. 处方开具" Foreground="White"/>
            </Border>

            <materialDesign:PackIcon Kind="ChevronRight" VerticalAlignment="Center" Margin="8,0"/>

            <!-- Step 3: 完成 -->
            <Border Background="{Binding CurrentStep, Converter={StaticResource StepToColorConverter}, ConverterParameter=Step3}"
                    CornerRadius="20" Padding="16,8">
                <TextBlock Text="3. 完成" Foreground="White"/>
            </Border>
        </StackPanel>

        <!-- 步骤内容区(动态切换ViewModel) -->
        <Border Grid.Row="2" Background="{DynamicResource MaterialDesignPaper}" Padding="16">
            <ContentControl Content="{Binding CurrentStepViewModel}" />
        </Border>

        <!-- 按钮栏 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,16,0,0">
            <!-- 返回主页 -->
            <Button Content="返回主页" Command="{Binding BackToHomeCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>

            <!-- 暂存 -->
            <Button Content="暂存" Command="{Binding SaveDraftCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>

            <!-- 取消 -->
            <Button Content="取消" Command="{Binding CancelCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,16,0"/>

            <!-- 上一步 -->
            <Button Content="{Binding PreviousButtonText}" Command="{Binding PreviousStepCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"
                    Visibility="{Binding CanGoBack, Converter={StaticResource BoolToVisibilityConverter}}"/>

            <!-- 下一步 -->
            <Button Content="{Binding NextButtonText}" Command="{Binding NextStepCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Background="{DynamicResource PrimaryHueMidBrush}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

### 10. MedicalCaseModule注册

```csharp
// MedicalCaseModule.cs - Prism模块注册
using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.MedicalCase.Views;

[Module(ModuleName = "MedicalCaseModule")]
public class MedicalCaseModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Repository
        containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();

        // 注册ViewModels(8个)
        containerRegistry.Register<CompletionViewModel>();
        containerRegistry.Register<MedicalCaseConsultationViewModel>();
        containerRegistry.Register<MedicalCaseDetailViewModel>();
        containerRegistry.Register<MedicalCaseFlowViewModel>();
        containerRegistry.Register<MedicalCaseListViewModel>();
        containerRegistry.Register<MedicalCaseManagementViewModel>();
        containerRegistry.Register<OtherCasesQueryViewModel>();
        containerRegistry.Register<PrescriptionEditorViewModel>();

        // 注册Views(8对)
        containerRegistry.RegisterForNavigation<CompletionView>();
        containerRegistry.RegisterForNavigation<MedicalCaseConsultationView>();
        containerRegistry.RegisterForNavigation<MedicalCaseDetailView>();
        containerRegistry.RegisterForNavigation<MedicalCaseFlowView>();
        containerRegistry.RegisterForNavigation<MedicalCaseListView>();
        containerRegistry.RegisterForNavigation<MedicalCaseManagementView>();
        containerRegistry.RegisterForNavigation<OtherCasesQueryView>();
        containerRegistry.RegisterForNavigation<PrescriptionEditorView>();
    }
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                     Shell (ContentRegion)                         │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│               MedicalCase模块 (Prism Module)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐  患者选择后导航  ┌──────────────────────┐    │
│  │  Patients    │ ───────────────→ │ MedicalCaseFlowView   │    │
│  │  Module      │                   │ (流程编排容器)        │    │
│  └──────────────┘                   └──────────┬───────────┘    │
│                                                 │                 │
│                                                 ▼                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │          MedicalCaseFlowViewModel (792行)               │    │
│  │  ┌─────────────────────────────────────────────────┐   │    │
│  │  │  CurrentStep: ConsultationStep (枚举)           │   │    │
│  │  │  CurrentStepViewModel: object? (动态切换)       │   │    │
│  │  └─────────────────────────────────────────────────┘   │    │
│  │                                                           │    │
│  │  步骤1: Step1Consultation ──→ MedicalCaseConsultation   │    │
│  │                                  ViewModel (ISaveable)   │    │
│  │                                                           │    │
│  │  步骤2: Step2Prescription ──→ PrescriptionEditor         │    │
│  │                                  ViewModel (IValidatable)│    │
│  │                                                           │    │
│  │  步骤3: Step3Completion   ──→ CompletionViewModel       │    │
│  └─────────────────────────────────────────────────────────┘    │
│                          │                                        │
│                          │ 调用Repository                         │
│                          ▼                                        │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │      MedicalCaseRepository (20个方法)                    │    │
│  │  ┌─────────────────────────────────────────────────┐   │    │
│  │  │  基础CRUD: GetPaged/GetById/Create/Update/Delete│   │    │
│  │  │  详情查询: GetByIdWithDetails/QueryAsync        │   │    │
│  │  │  诊断管理: UpdateConsultation/CompleteStep1     │   │    │
│  │  │  处方管理: Create/Update/DeletePrescription     │   │    │
│  │  │  暂存继续: SaveAsDraft/GetUnfinishedCase/Close  │   │    │
│  │  └─────────────────────────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                          │                                        │
│                          │ HTTP调用                               │
│                          ▼                                        │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │         BaseApiRepository (Foundation层)                 │    │
│  │  Get/Post/Put/Delete封装                                │    │
│  └─────────────────────────────────────────────────────────┘    │
│                          │                                        │
│                          ▼                                        │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │           IApiService (Infrastructure层)                 │    │
│  │  统一HTTP通信 + 认证Token + 错误处理                    │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    WebAPI (Server端)                              │
│  /api/v1/medical-cases/*                                         │
└─────────────────────────────────────────────────────────────────┘

数据流向:
  1. PatientSelectionView选择患者 → 导航到MedicalCaseFlowView
  2. MedicalCaseFlowViewModel创建医案 → Repository.CreateAsync()
  3. 流程编排器切换步骤 → 动态加载Step ViewModel
  4. 每步完成后保存 → Repository.UpdateConsultation/CreatePrescription()
  5. 暂存功能 → Repository.SaveAsDraftAsync()
  6. 继续功能 → Repository.GetUnfinishedCase() + 恢复到上次步骤
  7. 完成医案 → Repository.CloseCaseAsync() + 导航回患者列表
```

##  设计原则

### 1. **流程编排与步骤解耦**

**原则说明**:
- 使用`ConsultationStep`枚举定义流程步骤，避免硬编码
- `CurrentStepViewModel`动态切换，实现步骤ViewModel解耦
- 通过`ISaveable`/`IValidatable`接口契约确保步骤一致性

**代码示例**:
```csharp
// 流程步骤枚举(集中定义)
public enum ConsultationStep
{
    Step1Consultation = 1, // 诊断录入
    Step2Prescription = 2, // 处方开具
    Step3Completion = 3    // 完成
}

// 动态切换ViewModel(解耦具体实现)
private void NavigateToStep(ConsultationStep step)
{
    switch (step)
    {
        case ConsultationStep.Step1Consultation:
            CurrentStepViewModel = _containerProvider.Resolve<MedicalCaseConsultationViewModel>();
            break;

        case ConsultationStep.Step2Prescription:
            CurrentStepViewModel = _containerProvider.Resolve<PrescriptionEditorViewModel>();
            break;

        case ConsultationStep.Step3Completion:
            CurrentStepViewModel = _containerProvider.Resolve<CompletionViewModel>();
            break;
    }
}
```

**反模式(避免)**:
```csharp
//  硬编码步骤逻辑(紧耦合)
if (currentPage == 1)
{
    CurrentViewModel = new MedicalCaseConsultationViewModel(...);
}
else if (currentPage == 2)
{
    CurrentViewModel = new PrescriptionEditorViewModel(...);
}
```

### 2. **接口契约与一致性验证**

**原则说明**:
- `ISaveable`接口确保所有步骤ViewModel可保存
- `IValidatable`接口确保所有步骤ViewModel可验证
- 流程编排器统一调用接口方法，避免类型检查

**代码示例**:
```csharp
// 统一验证和保存(基于接口契约)
private async Task ExecuteNextStepAsync()
{
    if (CurrentStepViewModel is ISaveable saveable)
    {
        if (CurrentStepViewModel is IValidatable validatable)
        {
            if (!validatable.Validate())
            {
                SetWarningMessage("请完成必填项");
                return;
            }
        }

        await saveable.SaveAsync();
    }

    // 导航到下一步
    CurrentStep++;
}

// 步骤ViewModel必须实现接口
public class MedicalCaseConsultationViewModel : ISaveable, IValidatable
{
    public bool Validate() => !string.IsNullOrWhiteSpace(ChiefComplaint);
    public async Task SaveAsync() => await _repository.UpdateConsultationAsync(...);
}
```

**反模式(避免)**:
```csharp
//  类型检查和强制转换(脆弱)
if (CurrentStepViewModel is MedicalCaseConsultationViewModel consultation)
{
    if (string.IsNullOrWhiteSpace(consultation.ChiefComplaint))
        return;
    await consultation.SaveAsync();
}
else if (CurrentStepViewModel is PrescriptionEditorViewModel prescription)
{
    if (!prescription.HasItems())
        return;
    await prescription.SaveAsync();
}
```

### 3. **暂存/继续与状态管理**

**原则说明**:
- 医案状态使用`MedicalCaseStatus`枚举(Draft/Active/Completed) - Issue #2242: 已废弃Cancelled，使用软删除
- 暂存时保存所有步骤数据 + 更新状态为Active
- 继续时根据医案数据恢复到正确步骤

**代码示例**:
```csharp
// 暂存医案(保存所有步骤数据 + 状态InProgress)
private async Task ExecuteSaveDraft()
{
    // Step 1: 保存当前步骤数据
    if (CurrentStepViewModel is ISaveable saveable)
    {
        await saveable.SaveAsync();
    }

    // Step 2: 更新医案状态
    await _medicalCaseRepository.SaveAsDraftAsync(MedicalCaseId);

    // Step 3: 导航回患者列表
    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
}

// 继续医案(恢复到正确步骤)
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    var isContinue = navigationContext.Parameters.GetValue<bool>("IsContinue");

    if (isContinue)
    {
        var medicalCase = await _repository.GetByIdWithDetailsAsync(MedicalCaseId);

        // 根据数据判断恢复到哪个步骤
        if (medicalCase.Prescription != null)
        {
            CurrentStep = ConsultationStep.Step3Completion;
        }
        else if (medicalCase.Consultation != null)
        {
            CurrentStep = ConsultationStep.Step2Prescription;
        }
        else
        {
            CurrentStep = ConsultationStep.Step1Consultation;
        }
    }
}
```

**反模式(避免)**:
```csharp
//  仅保存当前步骤,不保存医案状态(导致无法继续)
private async Task ExecuteSaveDraft()
{
    if (CurrentStepViewModel is ISaveable saveable)
    {
        await saveable.SaveAsync();
    }
    // 缺失:未更新医案状态
    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
}
```

### 4. **Repository模式与三层架构**

**原则说明**:
- ViewModel → Repository → BaseApiRepository → IApiService → HTTP(严格三层)
- Repository返回裸类型(MedicalCaseDto)，不返回`Result<T>`(避免冗余错误处理)
- BaseApiRepository统一封装HTTP操作和异常处理

**代码示例**:
```csharp
// ViewModel层:调用Repository获取裸类型
private async Task LoadMedicalCaseAsync()
{
    try
    {
        var medicalCase = await _repository.GetByIdAsync(MedicalCaseId);
        // 直接使用数据,无需检查Result.IsSuccess
    }
    catch (Exception ex)
    {
        // 统一异常处理
        SetErrorMessage($"加载失败:{ex.Message}");
    }
}

// Repository层:返回裸类型
public async Task<MedicalCaseDto?> GetByIdAsync(Guid id)
{
    var url = $"/api/v1/medical-cases/{id}";
    return await GetAsync<MedicalCaseDto>(url);
}

// BaseApiRepository层:封装HTTP操作
protected async Task<T?> GetAsync<T>(string url)
{
    try
    {
        return await _apiService.GetAsync<T>(url);
    }
    catch (ApiException ex)
    {
        _logger.LogError(ex, "HTTP请求失败");
        throw; // 向上层抛出异常
    }
}
```

**反模式(避免)**:
```csharp
//  Repository返回Result<T>(冗余错误处理)
public async Task<Result<MedicalCaseDto>> GetByIdAsync(Guid id)
{
    try
    {
        var data = await GetAsync<MedicalCaseDto>(...);
        return Result<MedicalCaseDto>.Success(data);
    }
    catch (Exception ex)
    {
        return Result<MedicalCaseDto>.Failure(ex.Message);
    }
}

// ViewModel层需要检查Result.IsSuccess
var result = await _repository.GetByIdAsync(MedicalCaseId);
if (result.IsSuccess)
{
    // 使用result.Data
}
```

### 5. **Prism导航与参数传递**

**原则说明**:
- 使用`NavigationParameters`传递跨View数据(MedicalCaseId、PatientId、IsContinue等)
- 实现`INavigationAware`接口处理导航生命周期
- `IsNavigationTarget`返回true允许ViewModel重用

**代码示例**:
```csharp
// 导航传递参数
private void NavigateToMedicalCaseFlow()
{
    var parameters = new NavigationParameters
    {
        { "MedicalCaseId", medicalCaseId },
        { "PatientId", patientId },
        { "IsContinue", true }
    };

    _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
}

// 接收导航参数
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
    var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
    var isContinue = navigationContext.Parameters.GetValue<bool>("IsContinue");

    // 根据参数加载数据
    if (isContinue)
    {
        await LoadMedicalCaseAsync();
    }
}

// 允许ViewModel重用(避免重复创建)
public bool IsNavigationTarget(NavigationContext navigationContext)
{
    return true;
}
```

**反模式(避免)**:
```csharp
//  使用静态变量传递数据(线程不安全)
public static class GlobalState
{
    public static Guid CurrentMedicalCaseId { get; set; }
}

//  在ViewModel中直接创建其他ViewModel(紧耦合)
public class MedicalCaseFlowViewModel
{
    private void NavigateToNextStep()
    {
        var nextViewModel = new PrescriptionEditorViewModel(...);
        CurrentStepViewModel = nextViewModel;
    }
}
```

### 6. **异步优先与UI响应性**

**原则说明**:
- 所有I/O操作使用async/await，避免阻塞UI线程
- 使用`IsBusy`属性显示加载状态，防止重复操作
- `AsyncDelegateCommand`支持异步Command执行

**代码示例**:
```csharp
// 异步Command(防止阻塞UI)
public AsyncDelegateCommand NextStepCommand { get; }

public MedicalCaseFlowViewModel(...)
{
    NextStepCommand = new AsyncDelegateCommand(
        ExecuteNextStepAsync,
        CanExecuteNextStep
    );
}

// 异步执行 + IsBusy状态管理
private async Task ExecuteNextStepAsync()
{
    try
    {
        IsBusy = true; // 显示加载指示器

        // Step 1: 验证
        if (!Validate())
            return;

        // Step 2: 保存(异步I/O)
        if (CurrentStepViewModel is ISaveable saveable)
        {
            await saveable.SaveAsync();
        }

        // Step 3: 导航
        CurrentStep++;
    }
    catch (Exception ex)
    {
        SetErrorMessage($"操作失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false; // 隐藏加载指示器
    }
}
```

**反模式(避免)**:
```csharp
//  同步I/O阻塞UI线程
public void ExecuteNextStep()
{
    var result = _repository.GetByIdAsync(MedicalCaseId).Result; // 阻塞UI
    // 导航逻辑
}

//  无IsBusy状态管理(允许重复操作)
private async Task ExecuteNextStepAsync()
{
    await SaveAsync(); // 可能被重复触发
    CurrentStep++;
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/medical-case/](../../../../docs/reference/modules/medical-case/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/medical-case-design.md](../../../../docs/explanation/architecture/client/medical-case-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/medical-case-development.md](../../../../docs/how-to-guides/client/medical-case-development.md) *(待创建)*
- **性能优化**:[docs/explanation/performance/repository-include-strategy.md](../../../../docs/explanation/performance/repository-include-strategy.md) - Repository Include预加载策略
- **单元测试**:[tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/](../../../../tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/) - ViewModel单元测试 (Epic #2175 Phase 4)
- **流程图参考**:[docs/deep/medical-case-flow-diagram.md](../../../../docs/deep/medical-case-flow-diagram.md) *(待创建)*

---

**最后更新**:2025-11-20 (Epic #2175 BF-002 Phase 4: 一体化病案编辑器 + 测试与优化)
**维护负责**:Client端开发组
