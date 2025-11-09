# MedicalCase模块架构设计(Client端)

> **文档层级**: Level 2 - 架构解释(Explanation)
> **目标读者**: 架构师、高级开发者
> **更新日期**: 2025-10-29

---

## 📋 目录

1. [MedicalCase模块定位与职责](#1-medicalcase模块定位与职责)
2. [核心架构设计](#2-核心架构设计)
3. [流程编排架构](#3-流程编排架构)
4. [步骤契约接口设计](#4-步骤契约接口设计)
5. [Repository模式与数据访问](#5-repository模式与数据访问)
6. [暂存与继续架构](#6-暂存与继续架构)
7. [Prism导航与参数传递](#7-prism导航与参数传递)
8. [ViewModel生命周期管理](#8-viewmodel生命周期管理)
9. [UI组件与数据绑定](#9-ui组件与数据绑定)
10. [设计模式与最佳实践](#10-设计模式与最佳实践)
11. [参考资料](#11-参考资料)

---

## 1. MedicalCase模块定位与职责

### 1.1 模块定位

```
Client端模块层级结构：
┌─────────────────────────────────────────┐
│  Shell层（应用启动与主窗口）             │
├─────────────────────────────────────────┤
│  ✨ Modules层（业务模块）✨               │
│  - Auth（认证模块）                      │
│  - Patients（患者管理模块）              │
│  - 🏥 MedicalCase（医案管理模块）🏥      │ ← 当前模块
│  - Consultation（诊断子模块）            │
│  - Prescriptions（处方子模块）           │
│  - Herbs（中药材模块）                   │
│  - Formula（验方模块）                   │
├─────────────────────────────────────────┤
│  Infrastructure层（WPF UI基础组件）      │
├─────────────────────────────────────────┤
│  Foundation层（平台无关技术基础）        │
├─────────────────────────────────────────┤
│  Shared层（跨端共享DTO和组件）           │
└─────────────────────────────────────────┘
```

**MedicalCase模块核心定位**：
- **流程容器与编排中心**：管理完整诊疗流程（诊断 → 处方 → 完成）
- **跨模块协作中枢**：关联Patients、Consultation、Prescriptions模块
- **医案生命周期管理**：创建、暂存、继续、完成、查询医案
- **Prism模块化设计**：通过INavigationAware实现导航生命周期管理

### 1.2 核心职责

| 职责类别 | 核心能力 | 实现方式 |
|---------|---------|---------|
| **流程编排** | 3步诊疗流程（诊断 → 处方 → 完成） | `MedicalCaseFlowViewModel`（792行） |
| **状态管理** | 医案状态跟踪（Registered/InProgress/Completed） | `MedicalCaseStatus`枚举 |
| **步骤解耦** | 动态加载步骤ViewModel | `CurrentStepViewModel`属性 + DI容器 |
| **契约验证** | 统一步骤验证和保存接口 | `ISaveable`/`IValidatable`接口 |
| **暂存/继续** | 暂存医案并恢复到上次步骤 | `SaveAsDraftAsync()` + 步骤判断逻辑 |
| **数据访问** | 医案CRUD、诊断/处方管理 | `IMedicalCaseRepository`（20方法） |
| **历史查询** | 患者历史病案查询 | `OtherCasesQueryViewModel` |
| **导航集成** | Prism区域导航与参数传递 | `INavigationAware`接口 |

### 1.3 模块特性

**代码规模**：
- 6个目录、34个文件
- 1个Converter、3个Interface、3个Model
- 1个Repository、8个ViewModel、8对View
- 核心ViewModel：`MedicalCaseFlowViewModel`（792行）

**技术栈**：
- .NET 8 + WPF（Windows Presentation Foundation）
- Prism.DryIoc 9.0.x（MVVM框架、模块化、DI容器）
- MaterialDesignThemes 5.1.x（Material Design UI组件库）
- INavigationAware（Prism导航生命周期接口）
- AsyncDelegateCommand（Prism异步命令）

---

## 2. 核心架构设计

### 2.1 三层架构总览

```
┌──────────────────────────────────────────────────────────┐
│                  Presentation Layer (MVVM)                │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Views (8个XAML视图)                               │  │
│  │  - MedicalCaseFlowView（流程容器）                  │  │
│  │  - MedicalCaseConsultationView（诊断视图）          │  │
│  │  - PrescriptionEditorView（处方视图）               │  │
│  │  - CompletionView（完成视图）                       │  │
│  │  - MedicalCaseListView（医案列表）                  │  │
│  │  - MedicalCaseDetailView（医案详情）                │  │
│  │  - OtherCasesQueryView（历史查询）                  │  │
│  │  - MedicalCaseManagementView（医案管理）            │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↕ 数据绑定                       │
│  ┌────────────────────────────────────────────────────┐  │
│  │  ViewModels (8个ViewModel)                         │  │
│  │  - 🎯 MedicalCaseFlowViewModel（流程编排核心,792行）│  │
│  │  - MedicalCaseConsultationViewModel（诊断步骤）      │  │
│  │  - PrescriptionEditorViewModel（处方步骤）           │  │
│  │  - CompletionViewModel（完成步骤）                  │  │
│  │  - MedicalCaseListViewModel（医案列表）             │  │
│  │  - MedicalCaseDetailViewModel（医案详情）           │  │
│  │  - OtherCasesQueryViewModel（历史查询）             │  │
│  │  - MedicalCaseManagementViewModel（医案管理）       │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                          ↓ 调用Repository
┌──────────────────────────────────────────────────────────┐
│                  Data Access Layer                        │
│  ┌────────────────────────────────────────────────────┐  │
│  │  IMedicalCaseRepository (20个方法)                  │  │
│  │  - 基础CRUD: GetPaged/GetById/Create/Update/Delete  │  │
│  │  - 详情查询: GetByIdWithDetails/QueryAsync         │  │
│  │  - 诊断管理: UpdateConsultation/CompleteStep1      │  │
│  │  - 处方管理: Create/Update/DeletePrescription      │  │
│  │  - 暂存继续: SaveAsDraft/GetUnfinishedCase/Close   │  │
│  └────────────────────────────────────────────────────┘  │
│                          ↓ 实现类                         │
│  ┌────────────────────────────────────────────────────┐  │
│  │  MedicalCaseRepository                              │  │
│  │  - 继承自 BaseApiRepository（Foundation层）         │  │
│  │  - 封装HTTP调用 + 异常处理                          │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                          ↓ HTTP调用
┌──────────────────────────────────────────────────────────┐
│              Foundation Layer (Technical Services)        │
│  IApiService（HTTP通信）+ ICacheService（缓存）           │
└──────────────────────────────────────────────────────────┘
                          ↓ RESTful API
┌──────────────────────────────────────────────────────────┐
│                    Server端 WebAPI                        │
│  /api/v1/medical-cases/*（医案管理API）                   │
└──────────────────────────────────────────────────────────┘
```

### 2.2 核心接口与模型

#### IMedicalCaseRepository（20个方法）

```csharp
public interface IMedicalCaseRepository
{
    // ========== 基础CRUD（5个方法） ==========
    Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int pageIndex, int pageSize);
    Task<MedicalCaseDto?> GetByIdAsync(Guid id);
    Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto);
    Task<MedicalCaseDto> UpdateAsync(Guid id, UpdateMedicalCaseDto dto);
    Task DeleteAsync(Guid id);

    // ========== 详情与查询（4个方法） ==========
    Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);
    Task<MedicalCaseDto> CreateWithDetailsAsync(CreateMedicalCaseDto dto);
    Task<MedicalCaseDetailDto?> GetByIdWithDetailsAsync(Guid id); // 含诊断+处方
    Task<List<MedicalCaseDto>> QueryAsync(MedicalCaseQueryDto dto);

    // ========== 诊断管理（3个方法） ==========
    Task<ConsultationDto> UpdateConsultationAsync(Guid caseId, UpdateConsultationDto dto);
    Task<ConsultationFlowResult> CompleteStep1Async(Guid caseId, UpdateConsultationDto dto);
    Task ResetConsultationStepsAsync(Guid caseId);

    // ========== 处方管理（5个方法） ==========
    Task<PrescriptionDto> CreatePrescriptionAsync(Guid caseId, CreatePrescriptionDto dto);
    Task<PrescriptionDto> UpdatePrescriptionAsync(Guid caseId, Guid prescriptionId, UpdatePrescriptionDto dto);
    Task DeletePrescriptionAsync(Guid caseId, Guid prescriptionId);
    Task ClearPrescriptionAsync(Guid caseId);
    Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(Guid caseId, Guid formulaId);

    // ========== 暂存与继续（3个方法） ==========
    Task<MedicalCaseDto> SaveAsDraftAsync(Guid id); // 状态 → InProgress
    Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId);
    Task CloseCaseAsync(Guid id); // 状态 → Completed
}
```

#### ConsultationStep（诊疗步骤枚举）

```csharp
public enum ConsultationStep
{
    Step1Consultation = 1, // 诊断录入（四诊记录+中医诊断）
    Step2Prescription = 2, // 处方开具（药材选择+剂量配伍）
    Step3Completion = 3    // 完成（打印处方+关闭医案）
}
```

#### MedicalCaseStatus（医案状态枚举）

```csharp
public enum MedicalCaseStatus
{
    Registered = 1,  // 已登记（初始状态）
    InProgress = 2,  // 进行中（暂存状态）
    Completed = 3,   // 已完成（关闭状态）
    Cancelled = 4    // 已取消（异常状态）
}
```

### 2.3 目录结构

```
LYBT.Desktop.MedicalCase/
├── Converters/                            # 值转换器（1个）
│   └── InvertedBoolConverter.cs          # 布尔值反转转换器
├── Interfaces/                            # 接口定义（3个）
│   ├── IMedicalCaseRepository.cs         # 医案仓储接口（20个方法）
│   ├── ISaveable.cs                      # 可保存接口（步骤ViewModel契约）
│   └── IValidatable.cs                   # 可验证接口（步骤ViewModel契约）
├── Models/                                # 数据模型（3个）
│   ├── ConsultationStep.cs               # 诊疗步骤枚举
│   ├── FlowStep.cs                       # 流程步骤元数据
│   └── MedicalCaseItem.cs                # 医案列表项
├── Repositories/                          # 数据仓储实现（1个）
│   └── MedicalCaseRepository.cs          # 医案仓储实现（20个方法）
├── ViewModels/                            # MVVM视图模型（8个）
│   ├── CompletionViewModel.cs            # 完成视图模型
│   ├── MedicalCaseConsultationViewModel.cs # 诊断视图模型（Step 1）
│   ├── MedicalCaseDetailViewModel.cs     # 医案详情视图模型
│   ├── MedicalCaseFlowViewModel.cs       # 🎯 流程编排核心（792行）
│   ├── MedicalCaseListViewModel.cs       # 医案列表视图模型
│   ├── MedicalCaseManagementViewModel.cs # 医案管理视图模型
│   ├── OtherCasesQueryViewModel.cs       # 历史病案查询视图模型
│   └── PrescriptionEditorViewModel.cs    # 处方编辑视图模型（Step 2）
├── Views/                                 # WPF视图（8对16个文件）
│   ├── CompletionView.xaml/.xaml.cs
│   ├── MedicalCaseConsultationView.xaml/.xaml.cs
│   ├── MedicalCaseDetailView.xaml/.xaml.cs
│   ├── MedicalCaseFlowView.xaml/.xaml.cs
│   ├── MedicalCaseListView.xaml/.xaml.cs
│   ├── MedicalCaseManagementView.xaml/.xaml.cs
│   ├── OtherCasesQueryView.xaml/.xaml.cs
│   └── PrescriptionEditorView.xaml/.xaml.cs
└── MedicalCaseModule.cs                   # Prism模块注册
```

---

## 3. 流程编排架构

### 3.1 流程编排设计哲学

```
流程编排核心思想：
┌─────────────────────────────────────────────────────────┐
│  MedicalCaseFlowViewModel = 流程容器 + 步骤协调器        │
│  - 当前步骤管理（CurrentStep）                           │
│  - 动态ViewModel加载（CurrentStepViewModel）             │
│  - 统一验证和保存（ISaveable/IValidatable接口）          │
│  - 步骤导航控制（NextStepCommand/PreviousStepCommand）  │
│  - 医案状态同步（SaveAsDraftAsync/CloseCaseAsync）      │
└─────────────────────────────────────────────────────────┘
                          ↓ 步骤切换
┌─────────────────────────────────────────────────────────┐
│  Step 1: 诊断录入                                        │
│  MedicalCaseConsultationViewModel（实现ISaveable）       │
│  - 四诊录入：望诊/闻诊/问诊/切诊                         │
│  - 中医诊断：辨证论治                                    │
│  - 验证逻辑：主诉+中医诊断必填                           │
└─────────────────────────────────────────────────────────┘
                          ↓ 下一步（验证+保存+导航）
┌─────────────────────────────────────────────────────────┐
│  Step 2: 处方开具                                        │
│  PrescriptionEditorViewModel（实现IValidatable）         │
│  - 药材选择：搜索中药材                                  │
│  - 剂量配伍：单味剂量+总剂数                             │
│  - 验证逻辑：至少1味药材                                 │
└─────────────────────────────────────────────────────────┘
                          ↓ 下一步（验证+保存+导航）
┌─────────────────────────────────────────────────────────┐
│  Step 3: 完成                                            │
│  CompletionViewModel                                     │
│  - 打印处方                                              │
│  - 关闭医案（状态 → Completed）                          │
│  - 导航回患者列表                                        │
└─────────────────────────────────────────────────────────┘
```

### 3.2 MedicalCaseFlowViewModel核心属性

```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase, INavigationAware
{
    // ========== 流程状态属性（5个） ==========
    private ConsultationStep _currentStep = ConsultationStep.Step1Consultation;
    public ConsultationStep CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                UpdateCurrentStepText();
                NavigateToStep(_currentStep); // 动态加载步骤ViewModel
                RaisePropertyChanged(nameof(CanGoBack));
                RaisePropertyChanged(nameof(CanGoNext));
            }
        }
    }

    private object? _currentStepViewModel;
    public object? CurrentStepViewModel // 动态切换的步骤ViewModel
    {
        get => _currentStepViewModel;
        set => SetProperty(ref _currentStepViewModel, value);
    }

    private string _currentStepText = "诊断录入";
    public string CurrentStepText
    {
        get => _currentStepText;
        set => SetProperty(ref _currentStepText, value);
    }

    private Guid _medicalCaseId;
    public Guid MedicalCaseId
    {
        get => _medicalCaseId;
        set => SetProperty(ref _medicalCaseId, value);
    }

    // ========== 患者信息属性（3个） ==========
    private PatientDto? _currentPatient;
    public PatientDto? CurrentPatient
    {
        get => _currentPatient;
        set
        {
            SetProperty(ref _currentPatient, value);
            UpdatePatientInfoDisplay();
        }
    }

    private string _selectedPatientName = string.Empty;
    public string SelectedPatientName
    {
        get => _selectedPatientName;
        set => SetProperty(ref _selectedPatientName, value);
    }

    private string _selectedPatientInfo = string.Empty;
    public string SelectedPatientInfo // "男 | 45岁"
    {
        get => _selectedPatientInfo;
        set => SetProperty(ref _selectedPatientInfo, value);
    }

    // ========== UI控制属性（5个） ==========
    public bool CanGoBack => CurrentStep != ConsultationStep.Step1Consultation;

    public bool CanGoNext => CanExecuteNextStep();

    public string NextButtonText => CurrentStep switch
    {
        ConsultationStep.Step1Consultation => "下一步：开具处方",
        ConsultationStep.Step2Prescription => "下一步：完成",
        ConsultationStep.Step3Completion => "完成并关闭",
        _ => "下一步"
    };

    public string PreviousButtonText => CurrentStep switch
    {
        ConsultationStep.Step2Prescription => "上一步：诊断录入",
        ConsultationStep.Step3Completion => "上一步：处方开具",
        _ => "上一步"
    };

    private bool _patientInfoBarVisible = true;
    public bool PatientInfoBarVisible
    {
        get => _patientInfoBarVisible;
        set => SetProperty(ref _patientInfoBarVisible, value);
    }
}
```

### 3.3 流程控制命令

```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    // ========== 构造函数与依赖注入 ==========
    private readonly IContainerProvider _containerProvider;
    private readonly IRegionManager _regionManager;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public MedicalCaseFlowViewModel(
        IContainerProvider containerProvider,
        IRegionManager regionManager,
        IMedicalCaseRepository medicalCaseRepository,
        ILogger<MedicalCaseFlowViewModel> logger)
        : base(logger)
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

    // ========== 命令定义（5个） ==========
    public AsyncDelegateCommand NextStepCommand { get; }
    public DelegateCommand PreviousStepCommand { get; }
    public AsyncDelegateCommand SaveDraftCommand { get; }
    public AsyncDelegateCommand CancelCommand { get; }
    public DelegateCommand BackToHomeCommand { get; }
}
```

### 3.4 下一步执行流程

```csharp
// 执行下一步（验证 → 保存 → 导航）
private async Task ExecuteNextStepAsync()
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // ========== Step 1: 验证当前步骤 ==========
        if (CurrentStepViewModel is IValidatable validatable)
        {
            if (!validatable.Validate())
            {
                SetWarningMessage("请完成必填项");
                return;
            }
        }

        // ========== Step 2: 保存当前步骤数据 ==========
        if (CurrentStepViewModel is ISaveable saveable)
        {
            await saveable.SaveAsync();
        }

        // ========== Step 3: 导航到下一步骤 ==========
        switch (CurrentStep)
        {
            case ConsultationStep.Step1Consultation:
                // 诊断完成 → 处方编辑
                CurrentStep = ConsultationStep.Step2Prescription;
                break;

            case ConsultationStep.Step2Prescription:
                // 处方完成 → 完成页面（可选跳过处方）
                CurrentStep = ConsultationStep.Step3Completion;
                break;

            case ConsultationStep.Step3Completion:
                // 完成医案 → 关闭并返回患者列表
                await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Completed);
                _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                break;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "执行下一步失败");
        SetErrorMessage($"操作失败: {ex.Message}");
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

// 下一步是否可用（验证逻辑）
private bool CanExecuteNextStep()
{
    if (CurrentStepViewModel is IValidatable validatable)
    {
        return validatable.Validate();
    }

    return true; // 无验证要求则默认可用
}

// 上一步是否可用（Step1禁用）
private bool CanExecutePreviousStep()
{
    return CurrentStep != ConsultationStep.Step1Consultation;
}
```

### 3.5 动态步骤ViewModel加载

```csharp
// 导航到指定步骤（动态加载ViewModel）
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

    // 如果ViewModel需要医案ID，通过属性注入
    if (CurrentStepViewModel is IMedicalCaseContext context)
    {
        context.MedicalCaseId = MedicalCaseId;
    }

    _logger.LogInformation("导航到步骤: {Step}", step);
}

// 更新步骤文本（显示在UI上）
private void UpdateCurrentStepText()
{
    CurrentStepText = CurrentStep switch
    {
        ConsultationStep.Step1Consultation => "诊断录入",
        ConsultationStep.Step2Prescription => "处方开具",
        ConsultationStep.Step3Completion => "完成",
        _ => "未知步骤"
    };
}
```

---

## 4. 步骤契约接口设计

### 4.1 接口契约设计哲学

```
接口契约目标：
┌─────────────────────────────────────────────────────────┐
│  统一步骤ViewModel行为，确保流程一致性                    │
│  - ISaveable: 确保所有步骤可保存数据到服务器             │
│  - IValidatable: 确保所有步骤可验证数据完整性            │
│  - 流程编排器：基于接口契约统一调用，无需类型检查         │
└─────────────────────────────────────────────────────────┘
                          ↓ 实现接口
┌─────────────────────────────────────────────────────────┐
│  步骤1: MedicalCaseConsultationViewModel                 │
│  implements ISaveable, IValidatable                      │
│  - Validate(): 检查主诉+中医诊断必填                     │
│  - SaveAsync(): 调用Repository保存诊断记录               │
└─────────────────────────────────────────────────────────┘
                          ↓ 实现接口
┌─────────────────────────────────────────────────────────┐
│  步骤2: PrescriptionEditorViewModel                      │
│  implements ISaveable, IValidatable                      │
│  - Validate(): 检查至少1味药材                           │
│  - SaveAsync(): 调用Repository保存处方                   │
└─────────────────────────────────────────────────────────┘
                          ↓ 实现接口
┌─────────────────────────────────────────────────────────┐
│  步骤3: CompletionViewModel                              │
│  （可选实现接口，无验证和保存逻辑）                        │
└─────────────────────────────────────────────────────────┘
```

### 4.2 ISaveable接口定义

```csharp
namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 可保存接口，用于步骤ViewModel实现
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 保存当前步骤数据到服务器
    /// </summary>
    /// <returns>保存任务</returns>
    Task SaveAsync();
}
```

**设计目标**：
- ✅ 统一保存行为：流程编排器通过接口调用，无需关心具体保存逻辑
- ✅ 异步优先：所有I/O操作使用async/await，避免阻塞UI线程
- ✅ 异常传播：SaveAsync()抛出异常由流程编排器统一处理

### 4.3 IValidatable接口定义

```csharp
namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 可验证接口，用于步骤ViewModel实现
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// 验证当前步骤数据是否完整
    /// </summary>
    /// <returns>true = 验证通过, false = 验证失败</returns>
    bool Validate();
}
```

**设计目标**：
- ✅ 统一验证行为：流程编排器在"下一步"前调用Validate()
- ✅ 同步验证：验证逻辑通常是同步的（检查字段非空/格式等）
- ✅ 返回布尔值：简单明了，true=通过，false=失败

### 4.4 步骤1: MedicalCaseConsultationViewModel实现

```csharp
public class MedicalCaseConsultationViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    private readonly IMedicalCaseRepository _repository;

    public Guid MedicalCaseId { get; set; }

    // ========== 四诊数据属性 ==========
    private string _chiefComplaint = string.Empty;
    public string ChiefComplaint // 主诉（必填）
    {
        get => _chiefComplaint;
        set => SetProperty(ref _chiefComplaint, value);
    }

    private string _presentIllness = string.Empty;
    public string PresentIllness // 现病史
    {
        get => _presentIllness;
        set => SetProperty(ref _presentIllness, value);
    }

    private string _inspection = string.Empty;
    public string Inspection // 望诊
    {
        get => _inspection;
        set => SetProperty(ref _inspection, value);
    }

    private string _auscultation = string.Empty;
    public string Auscultation // 闻诊
    {
        get => _auscultation;
        set => SetProperty(ref _auscultation, value);
    }

    private string _inquiry = string.Empty;
    public string Inquiry // 问诊
    {
        get => _inquiry;
        set => SetProperty(ref _inquiry, value);
    }

    private string _palpation = string.Empty;
    public string Palpation // 切诊
    {
        get => _palpation;
        set => SetProperty(ref _palpation, value);
    }

    private string _tcmDiagnosis = string.Empty;
    public string TcmDiagnosis // 中医诊断（必填）
    {
        get => _tcmDiagnosis;
        set => SetProperty(ref _tcmDiagnosis, value);
    }

    // ========== IValidatable实现 ==========
    public bool Validate()
    {
        // 必填项验证：主诉 + 中医诊断
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

        ClearMessage();
        return true;
    }

    // ========== ISaveable实现 ==========
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
            _logger.LogInformation("诊断记录已保存，医案ID: {MedicalCaseId}", MedicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存诊断失败");
            throw; // 向流程编排器抛出异常
        }
    }
}
```

### 4.5 步骤2: PrescriptionEditorViewModel实现

```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    private readonly IMedicalCaseRepository _repository;

    public Guid MedicalCaseId { get; set; }

    // ========== 处方数据属性 ==========
    public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; } = new();

    private decimal _totalPrice;
    public decimal TotalPrice // 处方总价
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }

    private int _totalDosages = 7; // 默认7剂
    public int TotalDosages
    {
        get => _totalDosages;
        set
        {
            if (SetProperty(ref _totalDosages, value))
            {
                CalculateTotalPrice(); // 剂数变化时重算总价
            }
        }
    }

    // ========== IValidatable实现 ==========
    public bool Validate()
    {
        // 必填项验证：至少1味药材
        if (PrescriptionItems.Count == 0)
        {
            SetWarningMessage("请至少添加一味药材");
            return false;
        }

        // 验证每味药材的剂量
        foreach (var item in PrescriptionItems)
        {
            if (item.Dosage <= 0)
            {
                SetWarningMessage($"药材[{item.HerbName}]剂量必须大于0");
                return false;
            }
        }

        ClearMessage();
        return true;
    }

    // ========== ISaveable实现 ==========
    public async Task SaveAsync()
    {
        try
        {
            var dto = new CreatePrescriptionDto
            {
                TotalDosages = TotalDosages,
                Items = PrescriptionItems.Select(item => new PrescriptionItemDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            await _repository.CreatePrescriptionAsync(MedicalCaseId, dto);
            _logger.LogInformation("处方已保存，医案ID: {MedicalCaseId}", MedicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存处方失败");
            throw;
        }
    }

    // ========== 辅助方法 ==========
    private void CalculateTotalPrice()
    {
        // 总价 = Σ(单味剂量 × 单价) × 总剂数
        TotalPrice = PrescriptionItems.Sum(item => item.Dosage * item.UnitPrice) * TotalDosages;
    }
}
```

### 4.6 接口契约优势

| 优势 | 说明 | 代码示例 |
|-----|------|---------|
| **统一流程** | 流程编排器无需关心具体步骤类型 | `if (vm is ISaveable s) await s.SaveAsync()` |
| **松耦合** | 步骤ViewModel独立实现，互不依赖 | 新增步骤只需实现接口 |
| **可测试性** | 接口易于Mock，单元测试友好 | `Mock<ISaveable>` |
| **扩展性** | 新增步骤无需修改流程编排器 | 开闭原则（OCP） |

---

## 5. Repository模式与数据访问

### 5.1 Repository模式设计

```
Repository模式三层架构：
┌─────────────────────────────────────────────────────────┐
│  ViewModel层（8个ViewModel）                             │
│  - 调用Repository获取裸类型（MedicalCaseDto）             │
│  - try-catch统一异常处理                                 │
└─────────────────────────────────────────────────────────┘
                          ↓ 调用Repository
┌─────────────────────────────────────────────────────────┐
│  Repository层（MedicalCaseRepository）                   │
│  - 实现IMedicalCaseRepository接口（20个方法）             │
│  - 返回裸类型（MedicalCaseDto），不返回Result<T>          │
│  - 继承BaseApiRepository（Foundation层）                 │
└─────────────────────────────────────────────────────────┘
                          ↓ 继承自
┌─────────────────────────────────────────────────────────┐
│  BaseApiRepository（Foundation层）                       │
│  - 封装HTTP调用（GET/POST/PUT/DELETE）                   │
│  - 统一异常处理（ApiException → throw）                  │
│  - 日志记录                                              │
└─────────────────────────────────────────────────────────┘
                          ↓ 调用
┌─────────────────────────────────────────────────────────┐
│  IApiService（Foundation层）                             │
│  - 统一HTTP通信 + 认证Token + Polly策略                  │
└─────────────────────────────────────────────────────────┘
```

### 5.2 MedicalCaseRepository实现

```csharp
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models;

public class MedicalCaseRepository : BaseApiRepository, IMedicalCaseRepository
{
    public MedicalCaseRepository(
        IApiService apiService,
        ILogger<MedicalCaseRepository> logger)
        : base(apiService, logger)
    {
    }

    // ========== 基础CRUD ==========
    public async Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int pageIndex, int pageSize)
    {
        var url = $"/api/v1/medical-cases?pageIndex={pageIndex}&pageSize={pageSize}";
        return await GetAsync<PagedResult<MedicalCaseDto>>(url);
    }

    public async Task<MedicalCaseDto?> GetByIdAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}";
        return await GetAsync<MedicalCaseDto>(url);
    }

    public async Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto)
    {
        var url = "/api/v1/medical-cases";
        return await PostAsync<CreateMedicalCaseDto, MedicalCaseDto>(url, dto);
    }

    public async Task<MedicalCaseDto> UpdateAsync(Guid id, UpdateMedicalCaseDto dto)
    {
        var url = $"/api/v1/medical-cases/{id}";
        return await PutAsync<UpdateMedicalCaseDto, MedicalCaseDto>(url, dto);
    }

    public async Task DeleteAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}";
        await DeleteAsync(url);
    }

    // ========== 详情与查询 ==========
    public async Task<MedicalCaseDetailDto?> GetByIdWithDetailsAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}/details";
        return await GetAsync<MedicalCaseDetailDto>(url);
    }

    public async Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId)
    {
        var url = $"/api/v1/medical-cases/patients/{patientId}";
        return await GetAsync<List<MedicalCaseDto>>(url);
    }

    public async Task<List<MedicalCaseDto>> QueryAsync(MedicalCaseQueryDto dto)
    {
        var url = "/api/v1/medical-cases/query";
        return await PostAsync<MedicalCaseQueryDto, List<MedicalCaseDto>>(url, dto);
    }

    // ========== 诊断管理 ==========
    public async Task<ConsultationDto> UpdateConsultationAsync(
        Guid caseId,
        UpdateConsultationDto dto)
    {
        var url = $"/api/v1/medical-cases/{caseId}/consultation";
        return await PutAsync<UpdateConsultationDto, ConsultationDto>(url, dto);
    }

    public async Task<ConsultationFlowResult> CompleteStep1Async(
        Guid caseId,
        UpdateConsultationDto dto)
    {
        var url = $"/api/v1/medical-cases/{caseId}/consultation/complete";
        return await PostAsync<UpdateConsultationDto, ConsultationFlowResult>(url, dto);
    }

    public async Task ResetConsultationStepsAsync(Guid caseId)
    {
        var url = $"/api/v1/medical-cases/{caseId}/consultation/reset";
        await PostAsync<object, object>(url, new { });
    }

    // ========== 处方管理 ==========
    public async Task<PrescriptionDto> CreatePrescriptionAsync(
        Guid caseId,
        CreatePrescriptionDto dto)
    {
        var url = $"/api/v1/medical-cases/{caseId}/prescriptions";
        return await PostAsync<CreatePrescriptionDto, PrescriptionDto>(url, dto);
    }

    public async Task<PrescriptionDto> UpdatePrescriptionAsync(
        Guid caseId,
        Guid prescriptionId,
        UpdatePrescriptionDto dto)
    {
        var url = $"/api/v1/medical-cases/{caseId}/prescriptions/{prescriptionId}";
        return await PutAsync<UpdatePrescriptionDto, PrescriptionDto>(url, dto);
    }

    public async Task DeletePrescriptionAsync(Guid caseId, Guid prescriptionId)
    {
        var url = $"/api/v1/medical-cases/{caseId}/prescriptions/{prescriptionId}";
        await DeleteAsync(url);
    }

    public async Task ClearPrescriptionAsync(Guid caseId)
    {
        var url = $"/api/v1/medical-cases/{caseId}/prescriptions/clear";
        await PostAsync<object, object>(url, new { });
    }

    public async Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(
        Guid caseId,
        Guid formulaId)
    {
        var url = $"/api/v1/medical-cases/{caseId}/prescriptions/import-formula";
        return await PostAsync<object, PrescriptionDto>(url, new { FormulaId = formulaId });
    }

    // ========== 暂存与继续 ==========
    public async Task<MedicalCaseDto> SaveAsDraftAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}/draft";
        return await PutAsync<object, MedicalCaseDto>(url, new { });
    }

    public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
    {
        var url = $"/api/v1/medical-cases/patients/{patientId}/unfinished";
        return await GetAsync<MedicalCaseDto>(url);
    }

    public async Task CloseCaseAsync(Guid id)
    {
        var url = $"/api/v1/medical-cases/{id}/close";
        await PutAsync<object, MedicalCaseDto>(url, new { });
    }
}
```

### 5.3 ViewModel调用Repository示例

```csharp
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    private readonly IMedicalCaseRepository _repository;

    public ObservableCollection<MedicalCaseItem> MedicalCases { get; } = new();

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

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

            // 调用Repository获取裸类型
            var result = await _repository.GetPagedAsync(CurrentPage, PageSize);

            if (result != null)
            {
                MedicalCases.Clear();

                foreach (var medicalCase in result.Items)
                {
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
            SetErrorMessage($"加载失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

---

## 6. 暂存与继续架构

### 6.1 暂存/继续设计哲学

```
暂存/继续核心逻辑：
┌─────────────────────────────────────────────────────────┐
│  暂存功能（SaveAsDraftAsync）                            │
│  1. 保存当前步骤数据（ISaveable.SaveAsync()）            │
│  2. 更新医案状态为InProgress                             │
│  3. 导航回患者列表                                       │
└─────────────────────────────────────────────────────────┘
                          ↓ 下次继续
┌─────────────────────────────────────────────────────────┐
│  继续功能（Continue Medical Case）                       │
│  1. 查询患者未完成医案（GetUnfinishedCaseByPatientId）   │
│  2. 加载医案详情（GetByIdWithDetails）                   │
│  3. 根据数据判断恢复到哪个步骤                            │
│     - 有处方 → Step3Completion                           │
│     - 有诊断 → Step2Prescription                         │
│     - 无数据 → Step1Consultation                         │
│  4. 导航到MedicalCaseFlowView并传递参数                  │
└─────────────────────────────────────────────────────────┘
```

### 6.2 暂存医案实现

```csharp
// MedicalCaseFlowViewModel.cs - 暂存医案
public AsyncDelegateCommand SaveDraftCommand { get; }

private async Task ExecuteSaveDraft()
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // ========== Step 1: 保存当前步骤数据 ==========
        if (CurrentStepViewModel is ISaveable saveable)
        {
            await saveable.SaveAsync();
        }

        // ========== Step 2: 更新医案状态为InProgress（暂存） ==========
        await _medicalCaseRepository.SaveAsDraftAsync(MedicalCaseId);

        SetSuccessMessage("医案已暂存");
        _logger.LogInformation("医案已暂存，ID: {MedicalCaseId}", MedicalCaseId);

        // ========== Step 3: 导航回患者列表 ==========
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "暂存医案失败");
        SetErrorMessage($"暂存失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 6.3 继续医案实现

```csharp
// PatientSelectionViewModel.cs - 继续医案
private async Task ContinueConsultationAsync(Guid patientId)
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // ========== Step 1: 查询患者未完成医案 ==========
        var unfinishedCase = await _medicalCaseRepository
            .GetUnfinishedCaseByPatientIdAsync(patientId);

        if (unfinishedCase == null)
        {
            SetWarningMessage("该患者没有未完成的医案");
            return;
        }

        // ========== Step 2: 导航到流程视图，传递医案ID和继续标志 ==========
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", unfinishedCase.Id },
            { "PatientId", patientId },
            { "IsContinue", true } // 标记为继续模式
        };

        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "继续医案失败");
        SetErrorMessage($"继续失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 6.4 恢复步骤逻辑

```csharp
// MedicalCaseFlowViewModel.cs - OnNavigatedTo处理继续模式
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    var isContinue = navigationContext.Parameters.GetValue<bool>("IsContinue");

    if (isContinue)
    {
        // ========== 继续模式：加载医案详情并恢复到上次步骤 ==========
        MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");

        var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

        if (medicalCase != null)
        {
            // 更新患者信息
            CurrentPatient = medicalCase.Patient;

            // ========== 根据医案数据判断当前步骤 ==========
            if (medicalCase.Prescription != null && medicalCase.Prescription.Items.Count > 0)
            {
                // 有处方数据 → 恢复到Step3完成页
                CurrentStep = ConsultationStep.Step3Completion;
                _logger.LogInformation("恢复到Step3: 完成");
            }
            else if (medicalCase.Consultation != null)
            {
                // 有诊断数据但无处方 → 恢复到Step2处方编辑
                CurrentStep = ConsultationStep.Step2Prescription;
                _logger.LogInformation("恢复到Step2: 处方开具");
            }
            else
            {
                // 无诊断和处方数据 → 从Step1诊断录入开始
                CurrentStep = ConsultationStep.Step1Consultation;
                _logger.LogInformation("恢复到Step1: 诊断录入");
            }
        }
    }
    else
    {
        // ========== 新建模式：创建医案并从Step1开始 ==========
        var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
        await CreateMedicalCaseAsync(patientId);
        CurrentStep = ConsultationStep.Step1Consultation;
    }
}
```

### 6.5 暂存/继续状态机

```
医案状态机：
┌─────────────────────────────────────────────────────────┐
│  Registered（已登记）                                    │
│  - 初始状态                                              │
│  - CreateAsync() 创建医案                                │
└─────────────────────────────────────────────────────────┘
                          ↓ SaveAsDraftAsync()
┌─────────────────────────────────────────────────────────┐
│  InProgress（进行中/暂存）                               │
│  - 暂存状态                                              │
│  - 可继续编辑                                            │
│  - GetUnfinishedCaseByPatientId() 查询未完成医案         │
└─────────────────────────────────────────────────────────┘
                          ↓ CloseCaseAsync()
┌─────────────────────────────────────────────────────────┐
│  Completed（已完成）                                     │
│  - 终态                                                  │
│  - 不可继续编辑                                          │
│  - 仅可查看历史                                          │
└─────────────────────────────────────────────────────────┘
                          ↓ （异常情况）
┌─────────────────────────────────────────────────────────┐
│  Cancelled（已取消）                                     │
│  - 异常终态                                              │
└─────────────────────────────────────────────────────────┘
```

---

## 7. Prism导航与参数传递

### 7.1 Prism导航架构

```
Prism导航架构：
┌─────────────────────────────────────────────────────────┐
│  IRegionManager（区域管理器）                            │
│  - RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters) │
│  - 管理WPF区域（ContentRegion）                          │
│  - 传递NavigationParameters                             │
└─────────────────────────────────────────────────────────┘
                          ↓ 导航到
┌─────────────────────────────────────────────────────────┐
│  MedicalCaseFlowView（目标View）                         │
│  - DataContext = MedicalCaseFlowViewModel                │
└─────────────────────────────────────────────────────────┘
                          ↓ ViewModel实现
┌─────────────────────────────────────────────────────────┐
│  INavigationAware（导航生命周期接口）                     │
│  - OnNavigatedTo(context): 接收导航参数，加载数据        │
│  - IsNavigationTarget(context): 是否重用ViewModel        │
│  - OnNavigatedFrom(context): 导航离开，清理资源          │
└─────────────────────────────────────────────────────────┘
```

### 7.2 NavigationParameters传递

```csharp
// ========== 导航源：PatientSelectionViewModel ==========
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;

    // 开始诊疗（新建医案）
    private void StartConsultation(PatientDto patient)
    {
        var parameters = new NavigationParameters
        {
            { "PatientId", patient.Id },
            { "IsContinue", false } // 新建模式
        };

        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
    }

    // 继续诊疗（继续未完成医案）
    private async Task ContinueConsultation(Guid patientId)
    {
        // 查询未完成医案
        var unfinishedCase = await _medicalCaseRepository
            .GetUnfinishedCaseByPatientIdAsync(patientId);

        if (unfinishedCase != null)
        {
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", unfinishedCase.Id },
                { "PatientId", patientId },
                { "IsContinue", true } // 继续模式
            };

            _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
        }
    }
}
```

### 7.3 INavigationAware实现

```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase, INavigationAware
{
    // ========== OnNavigatedTo: 接收导航参数 ==========
    public async Task OnNavigatedTo(NavigationContext navigationContext)
    {
        // 从参数中提取数据
        var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
        var isContinue = navigationContext.Parameters.GetValue<bool>("IsContinue");

        _logger.LogInformation("导航到MedicalCaseFlowView, PatientId: {PatientId}, IsContinue: {IsContinue}",
            patientId, isContinue);

        if (isContinue)
        {
            // 继续模式：加载医案详情
            MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
            await LoadMedicalCaseDetailsAsync();
        }
        else
        {
            // 新建模式：创建医案
            await CreateMedicalCaseAsync(patientId);
        }
    }

    // ========== IsNavigationTarget: 是否重用ViewModel ==========
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 返回true允许ViewModel重用（避免重复创建）
        return true;
    }

    // ========== OnNavigatedFrom: 导航离开清理资源 ==========
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 清理资源
        CurrentStepViewModel = null;
        _logger.LogInformation("离开MedicalCaseFlowView");
    }

    // ========== 辅助方法 ==========
    private async Task CreateMedicalCaseAsync(Guid patientId)
    {
        try
        {
            var dto = new CreateMedicalCaseDto
            {
                PatientId = patientId,
                Status = MedicalCaseStatus.Registered
            };

            var medicalCase = await _medicalCaseRepository.CreateAsync(dto);
            MedicalCaseId = medicalCase.Id;

            // 加载患者信息
            CurrentPatient = medicalCase.Patient;

            _logger.LogInformation("医案已创建，ID: {MedicalCaseId}", MedicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建医案失败");
            SetErrorMessage($"创建失败: {ex.Message}");
        }
    }

    private async Task LoadMedicalCaseDetailsAsync()
    {
        try
        {
            var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

            if (medicalCase != null)
            {
                CurrentPatient = medicalCase.Patient;

                // 根据数据判断当前步骤（详见6.4节）
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载医案详情失败");
            SetErrorMessage($"加载失败: {ex.Message}");
        }
    }
}
```

### 7.4 导航返回示例

```csharp
// 取消操作：返回患者列表
public AsyncDelegateCommand CancelCommand { get; }

private async Task ExecuteCancel()
{
    try
    {
        // 确认对话框
        var result = await _dialogService.ShowConfirmAsync(
            "确认取消",
            "未保存的数据将丢失，确认取消吗？"
        );

        if (result == DialogResult.Yes)
        {
            // 导航回患者列表（不传递参数）
            _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "取消操作失败");
    }
}
```

---

## 8. ViewModel生命周期管理

### 8.1 ViewModel生命周期

```
ViewModel生命周期：
┌─────────────────────────────────────────────────────────┐
│  1. 构造函数（Constructor）                              │
│  - 依赖注入（Repository/RegionManager/Logger）           │
│  - 初始化Command                                         │
│  - 初始化ObservableCollection                            │
└─────────────────────────────────────────────────────────┘
                          ↓ Prism导航到
┌─────────────────────────────────────────────────────────┐
│  2. OnNavigatedTo（导航到）                              │
│  - 接收NavigationParameters                             │
│  - 加载数据（异步）                                      │
│  - 初始化UI状态                                          │
└─────────────────────────────────────────────────────────┘
                          ↓ 用户交互
┌─────────────────────────────────────────────────────────┐
│  3. 命令执行（Command Execution）                        │
│  - NextStepCommand（下一步）                            │
│  - PreviousStepCommand（上一步）                        │
│  - SaveDraftCommand（暂存）                             │
│  - CancelCommand（取消）                                │
└─────────────────────────────────────────────────────────┘
                          ↓ Prism导航离开
┌─────────────────────────────────────────────────────────┐
│  4. OnNavigatedFrom（导航离开）                          │
│  - 清理资源（CurrentStepViewModel = null）               │
│  - 取消订阅事件                                          │
│  - 记录日志                                              │
└─────────────────────────────────────────────────────────┘
                          ↓ GC回收
┌─────────────────────────────────────────────────────────┐
│  5. 垃圾回收（Garbage Collection）                       │
│  - ViewModel被GC回收（如果没有强引用）                    │
└─────────────────────────────────────────────────────────┘
```

### 8.2 构造函数与依赖注入

```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase, INavigationAware
{
    // ========== 依赖注入字段 ==========
    private readonly IContainerProvider _containerProvider;
    private readonly IRegionManager _regionManager;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MedicalCaseFlowViewModel> _logger;

    // ========== 构造函数 ==========
    public MedicalCaseFlowViewModel(
        IContainerProvider containerProvider,
        IRegionManager regionManager,
        IMedicalCaseRepository medicalCaseRepository,
        IDialogService dialogService,
        ILogger<MedicalCaseFlowViewModel> logger)
        : base(logger)
    {
        _containerProvider = containerProvider;
        _regionManager = regionManager;
        _medicalCaseRepository = medicalCaseRepository;
        _dialogService = dialogService;
        _logger = logger;

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

        _logger.LogDebug("MedicalCaseFlowViewModel已构造");
    }
}
```

### 8.3 资源清理

```csharp
public void OnNavigatedFrom(NavigationContext navigationContext)
{
    // 清理CurrentStepViewModel
    if (CurrentStepViewModel is IDisposable disposable)
    {
        disposable.Dispose();
    }
    CurrentStepViewModel = null;

    // 取消订阅事件（如果有）
    // EventAggregator.GetEvent<SomeEvent>().Unsubscribe(OnSomeEvent);

    _logger.LogInformation("MedicalCaseFlowViewModel资源已清理");
}
```

---

## 9. UI组件与数据绑定

### 9.1 MedicalCaseFlowView XAML结构

```xml
<!-- MedicalCaseFlowView.xaml -->
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseFlowView"
             xmlns:materialDesign="http://materialdesigninxaml.net/wprism"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 患者信息栏 -->
            <RowDefinition Height="Auto"/> <!-- 步骤指示器 -->
            <RowDefinition Height="*"/>    <!-- 步骤内容区（动态切换） -->
            <RowDefinition Height="Auto"/> <!-- 按钮栏 -->
        </Grid.RowDefinitions>

        <!-- ========== Row 0: 患者信息栏 ========== -->
        <Border Grid.Row="0" Background="{DynamicResource MaterialDesignPaper}"
                Padding="16" Margin="0,0,0,8"
                Visibility="{Binding PatientInfoBarVisible, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="患者:" FontSize="14" Margin="0,0,8,0"/>
                <TextBlock Text="{Binding SelectedPatientName}" FontWeight="Bold" FontSize="14" Margin="0,0,16,0"/>
                <TextBlock Text="{Binding SelectedPatientInfo}" FontSize="14" Foreground="Gray"/>
            </StackPanel>
        </Border>

        <!-- ========== Row 1: 步骤指示器 ========== -->
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

        <!-- ========== Row 2: 步骤内容区（动态切换ViewModel） ========== -->
        <Border Grid.Row="2" Background="{DynamicResource MaterialDesignPaper}" Padding="16">
            <!-- 🎯 关键：ContentControl动态加载当前步骤的ViewModel -->
            <ContentControl Content="{Binding CurrentStepViewModel}">
                <!-- DataTemplate根据ViewModel类型自动匹配View -->
                <ContentControl.Resources>
                    <DataTemplate DataType="{x:Type viewModels:MedicalCaseConsultationViewModel}">
                        <views:MedicalCaseConsultationView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type viewModels:PrescriptionEditorViewModel}">
                        <views:PrescriptionEditorView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type viewModels:CompletionViewModel}">
                        <views:CompletionView/>
                    </DataTemplate>
                </ContentControl.Resources>
            </ContentControl>
        </Border>

        <!-- ========== Row 3: 按钮栏 ========== -->
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

### 9.2 步骤颜色转换器

```csharp
// Converters/StepToColorConverter.cs
public class StepToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConsultationStep currentStep && parameter is string stepParam)
        {
            var targetStep = stepParam switch
            {
                "Step1" => ConsultationStep.Step1Consultation,
                "Step2" => ConsultationStep.Step2Prescription,
                "Step3" => ConsultationStep.Step3Completion,
                _ => ConsultationStep.Step1Consultation
            };

            // 当前步骤 → 高亮颜色，其他步骤 → 灰色
            if ((int)currentStep >= (int)targetStep)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F51B5")); // Primary
            }
            else
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")); // Gray
            }
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### 9.3 数据绑定优势

| 绑定类型 | 示例 | 优势 |
|---------|------|------|
| **属性绑定** | `Text="{Binding SelectedPatientName}"` | 自动更新UI |
| **命令绑定** | `Command="{Binding NextStepCommand}"` | 解耦UI和逻辑 |
| **转换器** | `Converter={StaticResource BoolToVisibilityConverter}` | 类型转换复用 |
| **动态内容** | `Content="{Binding CurrentStepViewModel}"` | 动态切换View |
| **数据模板** | `DataTemplate DataType="{x:Type ...}"` | 自动匹配View |

---

## 10. 设计模式与最佳实践

### 10.1 六大设计原则

#### 原则1：流程编排与步骤解耦

**原则说明**：
- 使用`ConsultationStep`枚举定义流程步骤，避免硬编码
- `CurrentStepViewModel`动态切换，实现步骤ViewModel解耦
- 通过`ISaveable`/`IValidatable`接口契约确保步骤一致性

**代码示例**（见3.5节）：
```csharp
// ✅ 好的做法：枚举 + 动态加载
private void NavigateToStep(ConsultationStep step)
{
    switch (step)
    {
        case ConsultationStep.Step1Consultation:
            CurrentStepViewModel = _containerProvider.Resolve<MedicalCaseConsultationViewModel>();
            break;
        // ...
    }
}

// ❌ 避免的做法：硬编码
if (currentPage == 1)
{
    CurrentViewModel = new MedicalCaseConsultationViewModel(...);
}
```

#### 原则2：接口契约与一致性验证

**原则说明**：
- `ISaveable`接口确保所有步骤ViewModel可保存
- `IValidatable`接口确保所有步骤ViewModel可验证
- 流程编排器统一调用接口方法，避免类型检查

**代码示例**（见4.4节）：
```csharp
// ✅ 好的做法：基于接口契约
if (CurrentStepViewModel is ISaveable saveable)
{
    await saveable.SaveAsync();
}

// ❌ 避免的做法：类型检查和强制转换
if (CurrentStepViewModel is MedicalCaseConsultationViewModel consultation)
{
    await consultation.SaveAsync();
}
```

#### 原则3：暂存/继续与状态管理

**原则说明**：
- 医案状态使用`MedicalCaseStatus`枚举（Registered/InProgress/Completed/Cancelled）
- 暂存时保存所有步骤数据 + 更新状态为InProgress
- 继续时根据医案数据恢复到正确步骤

**代码示例**（见6.2-6.4节）。

#### 原则4：Repository模式与三层架构

**原则说明**：
- ViewModel → Repository → BaseApiRepository → IApiService → HTTP（严格三层）
- Repository返回裸类型（MedicalCaseDto），不返回`Result<T>`（避免冗余错误处理）
- BaseApiRepository统一封装HTTP操作和异常处理

**代码示例**（见5.2-5.3节）。

#### 原则5：Prism导航与参数传递

**原则说明**：
- 使用`NavigationParameters`传递跨View数据（MedicalCaseId、PatientId、IsContinue等）
- 实现`INavigationAware`接口处理导航生命周期
- `IsNavigationTarget`返回true允许ViewModel重用

**代码示例**（见7.2-7.3节）。

#### 原则6：异步优先与UI响应性

**原则说明**：
- 所有I/O操作使用async/await，避免阻塞UI线程
- 使用`IsBusy`属性显示加载状态，防止重复操作
- `AsyncDelegateCommand`支持异步Command执行

**代码示例**：
```csharp
// ✅ 好的做法：异步Command + IsBusy
public AsyncDelegateCommand NextStepCommand { get; }

private async Task ExecuteNextStepAsync()
{
    try
    {
        IsBusy = true;
        await SaveAsync();
        CurrentStep++;
    }
    finally
    {
        IsBusy = false;
    }
}

// ❌ 避免的做法：同步I/O阻塞UI
public void ExecuteNextStep()
{
    var result = _repository.GetByIdAsync(MedicalCaseId).Result; // 阻塞UI
}
```

### 10.2 反模式（避免）

| 反模式 | 问题 | 正确做法 |
|-------|------|---------|
| **硬编码步骤逻辑** | 紧耦合，难以扩展 | 使用枚举 + 动态加载 |
| **类型检查和强制转换** | 脆弱，违反开闭原则 | 使用接口契约 |
| **静态变量传递数据** | 线程不安全 | 使用NavigationParameters |
| **直接创建ViewModel** | 紧耦合 | 使用DI容器Resolve |
| **同步I/O阻塞UI** | 用户体验差 | 使用async/await |
| **Repository返回Result<T>** | 冗余错误处理 | 返回裸类型 + try-catch |

---

## 11. 参考资料

### 11.1 内部文档

| 文档类型 | 文档路径 | 说明 |
|---------|---------|-----|
| **项目README** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/README.md` | MedicalCase模块完整项目文档（15000字） |
| **Client架构总览** | `docs/explanation/architecture/client/README.md` | Client端五层架构设计 |
| **Foundation设计** | `docs/explanation/architecture/client/foundation-design.md` | Foundation层架构设计（与MedicalCase对应） |
| **Infrastructure设计** | `docs/explanation/architecture/client/infrastructure-design.md` | Infrastructure层架构（UI基础组件） |
| **Models设计** | `docs/explanation/architecture/client/models-layer-design.md` | Client端Models层设计 |
| **Server MedicalCase设计** | `docs/explanation/architecture/server/medical-case-design.md` | Server端MedicalCase模块设计（待创建） |

### 11.2 技术栈参考

| 技术 | 官方文档 | 说明 |
|-----|---------|-----|
| **.NET 8** | https://learn.microsoft.com/dotnet/core/ | 基础框架 |
| **WPF** | https://learn.microsoft.com/dotnet/desktop/wpf/ | Windows Presentation Foundation |
| **Prism 8.x** | https://prismlibrary.com/ | MVVM框架、模块化、DI容器 |
| **MaterialDesignThemes** | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit | Material Design UI组件库 |
| **Prism.DryIoc** | https://github.com/PrismLibrary/Prism | DryIoc依赖注入容器 |
| **INavigationAware** | https://prismlibrary.com/docs/navigation.html | Prism导航生命周期 |
| **AsyncDelegateCommand** | https://prismlibrary.com/docs/commanding.html | Prism异步命令 |

### 11.3 设计模式参考

| 模式名称 | 应用场景 | MedicalCase实现 |
|---------|---------|----------------|
| **Repository模式** | 数据访问抽象 | `MedicalCaseRepository`（20方法） |
| **MVVM模式** | UI与逻辑分离 | View-ViewModel-Model |
| **策略模式** | 步骤验证和保存 | `ISaveable`/`IValidatable` |
| **模板方法模式** | 统一流程控制 | `ExecuteNextStepAsync()` |
| **状态机模式** | 医案状态管理 | `MedicalCaseStatus`枚举 |
| **观察者模式** | UI自动更新 | `ObservableCollection` |
| **命令模式** | UI操作解耦 | `AsyncDelegateCommand` |
| **依赖注入模式** | 松耦合设计 | Prism.DryIoc容器 |

---

**文档维护**：Client端架构团队
**最后更新**：2025-10-29
**相关Epic**：#1718 - Phase 1架构文档补充
