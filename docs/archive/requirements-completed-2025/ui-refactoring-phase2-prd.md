# LYBTZYZS Desktop端UI重构 Phase 2 - ViewModel组件化重构 PRD

**文档版本**: v1.0
**创建日期**: 2025-11-04
**Epic Issue**: #1805 (待创建)
**优先级**: 🟡 P1
**预计工期**: 3-4周（120小时）
**依赖**: Phase 1完成（#1797）

---

## 📋 执行摘要

### 目标
应用ADR-009 Desktop端组件化模式，对4个复杂ViewModel进行组件化重构，降低代码复杂度，提升可测试性和可维护性。

### 范围
- **MedicalCaseFlowViewModel**: ~600行 → <300行（提取3个组件）
- **PrescriptionEditorViewModel**: ~500行 → <300行（提取3个组件）
- **HerbManagementViewModel**: ~400行 → <300行（提取1个组件）
- **FormulaManagementViewModel**: ~450行 → <300行（提取2个组件）

### 成功指标
- ✅ ViewModel平均行数减少45%
- ✅ 单元测试Mock依赖数减少50%
- ✅ 代码可维护性提升60%
- ✅ 单元测试覆盖率≥80%
- ✅ 符合ADR-009组件化规范

---

## 1. 背景与动机

### 1.1 当前问题

根据Issue #1790和ADR-009，部分ViewModel存在以下问题：

| ViewModel | 行数 | 职责数 | 问题 |
|-----------|------|--------|------|
| MedicalCaseFlowViewModel | ~600 | 4 | 🔴 Critical - 三步流程状态管理、处方标记、完成逻辑、UI协调 |
| PrescriptionEditorViewModel | ~500 | 3 | 🟡 High - 价格计算、验方导入、药材选择混杂 |
| HerbManagementViewModel | ~400 | 3 | ⚠️ Medium - 搜索逻辑、分页管理、CRUD操作混杂 |
| FormulaManagementViewModel | ~450 | 3 | ⚠️ Medium - 搜索逻辑、验方校验、CRUD操作混杂 |

### 1.2 组件化标准（ADR-009）

**ViewModel职责原则**:
- ✅ ≤2个职责：可接受（UI协调 + 事件处理）
- ⚠️ 3个职责：建议提取Manager/Handler组件
- ❌ ≥4个职责：必须组件化重构

**组件化规范**:
1. **单一职责原则**：每个组件只负责一个业务领域
2. **事件驱动通信**：使用Prism EventAggregator与ViewModel解耦
3. **DI生命周期正确**：Manager/Handler使用Scoped，ViewModel使用Transient
4. **事件订阅清理**：ViewModel实现IDisposable清理事件订阅

### 1.3 参考实例

**成功案例**: PatientSelectionViewModel（Issue #1790）
- 重构前：350行，3个职责（UI协调、搜索管理、分页管理）
- 重构后：180行，2个职责（UI协调、事件处理）
- 提取组件：PatientSearchManager（搜索和分页逻辑）
- 效果：代码行数-49%，Mock依赖-60%，测试覆盖率+25%

---

## 2. 详细需求

### 2.1 MedicalCaseFlowViewModel 组件化

#### 当前状态
```
文件: src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs
行数: ~600行
职责数: 4个
- UI协调（显示控制、导航）
- 三步流程状态管理（Step 0/1/2/3逻辑）
- 处方标记处理（是否需要处方判断）
- 完成逻辑（医案完结）
```

#### 组件化设计

**提取组件1: MedicalCaseFlowManager**
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Managers/MedicalCaseFlowManager.cs

/// <summary>
/// 医案三步流程状态管理器
/// 职责：管理三步流程状态转换、验证流程完整性
/// </summary>
public class MedicalCaseFlowManager : IDisposable
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IEventAggregator _eventAggregator;

    public MedicalCaseFlowStep CurrentStep { get; private set; }
    public bool CanProceedToNextStep { get; private set; }

    public MedicalCaseFlowManager(
        IMedicalCaseRepository medicalCaseRepository,
        IConsultationRepository consultationRepository,
        IEventAggregator eventAggregator)
    {
        _medicalCaseRepository = medicalCaseRepository;
        _consultationRepository = consultationRepository;
        _eventAggregator = eventAggregator;

        // 订阅事件
        _eventAggregator.GetEvent<ConsultationCompletedEvent>().Subscribe(OnConsultationCompleted);
    }

    /// <summary>
    /// 初始化流程（Step 0 → Step 1）
    /// </summary>
    public async Task<FlowTransitionResult> InitializeFlowAsync(Guid patientId)
    {
        // 1. 检查是否有未完成医案（BF-003）
        var unfinishedCase = await _medicalCaseRepository.GetUnfinishedCaseAsync(patientId);
        if (unfinishedCase != null)
        {
            return FlowTransitionResult.Blocked("该患者有未完成的医案，请先完成或删除");
        }

        // 2. 创建新医案
        var medicalCase = new MedicalCase
        {
            PatientId = patientId,
            VisitDate = DateTime.Now,
            Status = MedicalCaseStatus.InProgress
        };
        await _medicalCaseRepository.CreateAsync(medicalCase);

        // 3. 发布事件
        _eventAggregator.GetEvent<MedicalCaseCreatedEvent>().Publish(medicalCase.Id);

        CurrentStep = MedicalCaseFlowStep.Step1_Consultation;
        return FlowTransitionResult.Success(medicalCase.Id);
    }

    /// <summary>
    /// 完成辨证（Step 1 → Step 2）
    /// </summary>
    public async Task<FlowTransitionResult> CompleteConsultationAsync(Guid medicalCaseId, Consultation consultation)
    {
        // 1. 保存辨证数据
        await _consultationRepository.CreateAsync(medicalCaseId, consultation);

        // 2. 更新医案状态
        await _medicalCaseRepository.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.ConsultationCompleted);

        // 3. 发布事件
        _eventAggregator.GetEvent<ConsultationCompletedEvent>().Publish(medicalCaseId);

        CurrentStep = MedicalCaseFlowStep.Step2_PrescriptionFlag;
        return FlowTransitionResult.Success(medicalCaseId);
    }

    /// <summary>
    /// 标记处方需求（Step 2 → Step 3a/3b）
    /// </summary>
    public async Task<FlowTransitionResult> MarkPrescriptionFlagAsync(Guid medicalCaseId, bool needsPrescription)
    {
        // 1. 更新医案处方标记
        await _medicalCaseRepository.UpdatePrescriptionFlagAsync(medicalCaseId, needsPrescription);

        // 2. 发布事件
        _eventAggregator.GetEvent<PrescriptionFlagMarkedEvent>().Publish(new PrescriptionFlagData
        {
            MedicalCaseId = medicalCaseId,
            NeedsPrescription = needsPrescription
        });

        // 3. 转换状态
        CurrentStep = needsPrescription
            ? MedicalCaseFlowStep.Step3a_CreatePrescription
            : MedicalCaseFlowStep.Step3b_Complete;

        return FlowTransitionResult.Success(medicalCaseId);
    }

    /// <summary>
    /// 验证是否可以进入下一步
    /// </summary>
    public async Task<bool> CanProceedToStep(MedicalCaseFlowStep nextStep, Guid medicalCaseId)
    {
        switch (nextStep)
        {
            case MedicalCaseFlowStep.Step1_Consultation:
                return true; // 总是可以创建医案

            case MedicalCaseFlowStep.Step2_PrescriptionFlag:
                // 必须有辨证数据
                var consultation = await _consultationRepository.GetByMedicalCaseIdAsync(medicalCaseId);
                return consultation != null;

            case MedicalCaseFlowStep.Step3a_CreatePrescription:
                // 必须已标记需要处方
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
                return medicalCase?.NeedsPrescription == true;

            case MedicalCaseFlowStep.Step3b_Complete:
                // 必须已标记不需要处方，或已创建处方
                var caseWithFlag = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
                if (caseWithFlag?.NeedsPrescription == false)
                    return true;

                var prescriptions = await _medicalCaseRepository.GetPrescriptionsAsync(medicalCaseId);
                return prescriptions.Any();

            default:
                return false;
        }
    }

    private void OnConsultationCompleted(Guid medicalCaseId)
    {
        CurrentStep = MedicalCaseFlowStep.Step2_PrescriptionFlag;
        CanProceedToNextStep = true;
    }

    public void Dispose()
    {
        _eventAggregator.GetEvent<ConsultationCompletedEvent>().Unsubscribe(OnConsultationCompleted);
    }
}

public enum MedicalCaseFlowStep
{
    Step0_SelectPatient,
    Step1_Consultation,
    Step2_PrescriptionFlag,
    Step3a_CreatePrescription,
    Step3b_Complete
}

public class FlowTransitionResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public Guid? MedicalCaseId { get; set; }

    public static FlowTransitionResult Success(Guid medicalCaseId) => new FlowTransitionResult
    {
        IsSuccess = true,
        MedicalCaseId = medicalCaseId
    };

    public static FlowTransitionResult Blocked(string message) => new FlowTransitionResult
    {
        IsSuccess = false,
        Message = message
    };
}
```

**提取组件2: PrescriptionFlagHandler**
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Handlers/PrescriptionFlagHandler.cs

/// <summary>
/// 处方标记处理器
/// 职责：处理处方需求判断逻辑
/// </summary>
public class PrescriptionFlagHandler
{
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;

    public PrescriptionFlagHandler(IDialogService dialogService, IEventAggregator eventAggregator)
    {
        _dialogService = dialogService;
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    /// 询问是否需要处方
    /// </summary>
    public async Task<bool?> AskForPrescriptionNeedAsync()
    {
        bool? result = null;

        _dialogService.ShowDialog("ConfirmDialog", new DialogParameters
        {
            { "title", "处方需求" },
            { "message", "该患者是否需要开具处方？" },
            { "confirmButtonText", "是，需要处方" },
            { "cancelButtonText", "否，不需要" }
        }, r =>
        {
            if (r.Result == ButtonResult.OK)
                result = true;
            else if (r.Result == ButtonResult.Cancel)
                result = false;
        });

        return result;
    }

    /// <summary>
    /// 处理处方标记后的导航
    /// </summary>
    public void NavigateBasedOnFlag(bool needsPrescription, Guid medicalCaseId, IRegionManager regionManager)
    {
        if (needsPrescription)
        {
            // 导航到处方编辑器
            var parameters = new NavigationParameters
            {
                { "medicalCaseId", medicalCaseId }
            };
            regionManager.RequestNavigate("MainContentRegion", "PrescriptionEditorView", parameters);

            // 发布事件
            _eventAggregator.GetEvent<NavigatedToPrescriptionEditorEvent>().Publish(medicalCaseId);
        }
        else
        {
            // 导航到完成界面
            var parameters = new NavigationParameters
            {
                { "medicalCaseId", medicalCaseId }
            };
            regionManager.RequestNavigate("MainContentRegion", "CompletionView", parameters);

            // 发布事件
            _eventAggregator.GetEvent<NavigatedToCompletionEvent>().Publish(medicalCaseId);
        }
    }
}
```

**提取组件3: CompletionHandler**
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Handlers/CompletionHandler.cs

/// <summary>
/// 医案完成处理器
/// 职责：处理医案完结逻辑
/// </summary>
public class CompletionHandler
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;

    public CompletionHandler(
        IMedicalCaseRepository medicalCaseRepository,
        IEventAggregator eventAggregator,
        IDialogService dialogService)
    {
        _medicalCaseRepository = medicalCaseRepository;
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
    }

    /// <summary>
    /// 完成医案
    /// </summary>
    public async Task<CompletionResult> CompleteMedicalCaseAsync(Guid medicalCaseId, string summary)
    {
        try
        {
            // 1. 验证医案状态
            var canComplete = await _medicalCaseRepository.CanCompleteAsync(medicalCaseId);
            if (!canComplete)
            {
                return CompletionResult.Failed("医案状态不允许完成，请检查是否已完成所有必要步骤");
            }

            // 2. 更新医案为已完成状态
            await _medicalCaseRepository.CompleteMedicalCaseAsync(medicalCaseId, summary);

            // 3. 发布事件
            _eventAggregator.GetEvent<MedicalCaseCompletedEvent>().Publish(medicalCaseId);

            return CompletionResult.Success();
        }
        catch (Exception ex)
        {
            return CompletionResult.Failed($"完成医案失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 显示完成确认对话框
    /// </summary>
    public async Task<bool> ConfirmCompletionAsync(Guid medicalCaseId)
    {
        bool confirmed = false;

        _dialogService.ShowDialog("ConfirmDialog", new DialogParameters
        {
            { "title", "确认完成" },
            { "message", "确认完成该医案？完成后将不能再编辑。" },
            { "confirmButtonText", "确认完成" },
            { "cancelButtonText", "取消" }
        }, r =>
        {
            confirmed = r.Result == ButtonResult.OK;
        });

        return confirmed;
    }
}

public class CompletionResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }

    public static CompletionResult Success() => new CompletionResult { IsSuccess = true };
    public static CompletionResult Failed(string message) => new CompletionResult
    {
        IsSuccess = false,
        Message = message
    };
}
```

**重构后的MedicalCaseFlowViewModel**
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs

/// <summary>
/// 医案三步流程ViewModel（重构后）
/// 职责：UI协调、事件处理
/// </summary>
public class MedicalCaseFlowViewModel : BindableBase, INavigationAware, IDisposable
{
    // 组件依赖（替代直接Repository依赖）
    private readonly MedicalCaseFlowManager _flowManager;
    private readonly PrescriptionFlagHandler _prescriptionFlagHandler;
    private readonly CompletionHandler _completionHandler;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    // UI绑定属性
    private MedicalCaseFlowStep _currentStep;
    public MedicalCaseFlowStep CurrentStep
    {
        get => _currentStep;
        set
        {
            SetProperty(ref _currentStep, value);
            RaisePropertyChanged(nameof(IsStep1Visible));
            RaisePropertyChanged(nameof(IsStep2Visible));
            RaisePropertyChanged(nameof(IsStep3aVisible));
            RaisePropertyChanged(nameof(IsStep3bVisible));
        }
    }

    public bool IsStep1Visible => CurrentStep == MedicalCaseFlowStep.Step1_Consultation;
    public bool IsStep2Visible => CurrentStep == MedicalCaseFlowStep.Step2_PrescriptionFlag;
    public bool IsStep3aVisible => CurrentStep == MedicalCaseFlowStep.Step3a_CreatePrescription;
    public bool IsStep3bVisible => CurrentStep == MedicalCaseFlowStep.Step3b_Complete;

    // Commands
    public DelegateCommand StartFlowCommand { get; }
    public DelegateCommand CompleteConsultationCommand { get; }
    public DelegateCommand MarkPrescriptionFlagCommand { get; }
    public DelegateCommand CompleteMedicalCaseCommand { get; }

    public MedicalCaseFlowViewModel(
        MedicalCaseFlowManager flowManager,
        PrescriptionFlagHandler prescriptionFlagHandler,
        CompletionHandler completionHandler,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _flowManager = flowManager;
        _prescriptionFlagHandler = prescriptionFlagHandler;
        _completionHandler = completionHandler;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        // 初始化Commands
        StartFlowCommand = new DelegateCommand(async () => await StartFlowAsync());
        CompleteConsultationCommand = new DelegateCommand(async () => await CompleteConsultationAsync());
        MarkPrescriptionFlagCommand = new DelegateCommand(async () => await MarkPrescriptionFlagAsync());
        CompleteMedicalCaseCommand = new DelegateCommand(async () => await CompleteMedicalCaseAsync());

        // 订阅事件
        _eventAggregator.GetEvent<ConsultationCompletedEvent>().Subscribe(OnConsultationCompleted);
        _eventAggregator.GetEvent<PrescriptionCreatedEvent>().Subscribe(OnPrescriptionCreated);
    }

    private async Task StartFlowAsync()
    {
        var result = await _flowManager.InitializeFlowAsync(SelectedPatientId);
        if (result.IsSuccess)
        {
            CurrentMedicalCaseId = result.MedicalCaseId.Value;
            CurrentStep = MedicalCaseFlowStep.Step1_Consultation;
        }
        else
        {
            // 显示错误消息（通过事件发布）
            _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(result.Message);
        }
    }

    private async Task CompleteConsultationAsync()
    {
        var result = await _flowManager.CompleteConsultationAsync(CurrentMedicalCaseId, CurrentConsultation);
        if (result.IsSuccess)
        {
            CurrentStep = MedicalCaseFlowStep.Step2_PrescriptionFlag;
        }
    }

    private async Task MarkPrescriptionFlagAsync()
    {
        var needsPrescription = await _prescriptionFlagHandler.AskForPrescriptionNeedAsync();
        if (needsPrescription.HasValue)
        {
            var result = await _flowManager.MarkPrescriptionFlagAsync(CurrentMedicalCaseId, needsPrescription.Value);
            if (result.IsSuccess)
            {
                _prescriptionFlagHandler.NavigateBasedOnFlag(needsPrescription.Value, CurrentMedicalCaseId, _regionManager);
            }
        }
    }

    private async Task CompleteMedicalCaseAsync()
    {
        var confirmed = await _completionHandler.ConfirmCompletionAsync(CurrentMedicalCaseId);
        if (confirmed)
        {
            var result = await _completionHandler.CompleteMedicalCaseAsync(CurrentMedicalCaseId, CompletionSummary);
            if (result.IsSuccess)
            {
                // 导航回医案管理界面
                _regionManager.RequestNavigate("MainContentRegion", "MedicalCaseManagementView");
            }
            else
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(result.Message);
            }
        }
    }

    private void OnConsultationCompleted(Guid medicalCaseId)
    {
        if (medicalCaseId == CurrentMedicalCaseId)
        {
            CurrentStep = MedicalCaseFlowStep.Step2_PrescriptionFlag;
        }
    }

    private void OnPrescriptionCreated(Guid medicalCaseId)
    {
        if (medicalCaseId == CurrentMedicalCaseId)
        {
            CurrentStep = MedicalCaseFlowStep.Step3b_Complete;
        }
    }

    public void Dispose()
    {
        _eventAggregator.GetEvent<ConsultationCompletedEvent>().Unsubscribe(OnConsultationCompleted);
        _eventAggregator.GetEvent<PrescriptionCreatedEvent>().Unsubscribe(OnPrescriptionCreated);
        _flowManager?.Dispose();
    }
}
```

#### 效果评估
- **代码行数**: 600行 → 180行（ViewModel）+ 200行（MedicalCaseFlowManager）+ 80行（PrescriptionFlagHandler）+ 100行（CompletionHandler）= 560行总计
- **ViewModel行数减少**: 70%
- **职责数**: 4个 → 2个（UI协调、事件处理）
- **Mock依赖**: 5个 → 3个（减少40%）
- **单一职责**: ✅ 每个组件职责明确

---

### 2.2 PrescriptionEditorViewModel 组件化

#### 当前状态
```
文件: src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionEditorViewModel.cs
行数: ~500行
职责数: 3个
- UI协调（显示控制、导航）
- 价格计算（药材价格、总价计算）
- 验方导入（从验方库导入）
- 药材选择管理
```

#### 组件化设计

**提取组件1: PrescriptionCalculator**
```csharp
/// <summary>
/// 处方价格计算器
/// 职责：计算处方总价、单项价格
/// </summary>
public class PrescriptionCalculator
{
    /// <summary>
    /// 计算处方总价
    /// </summary>
    public decimal CalculateTotalPrice(IEnumerable<PrescriptionItem> items)
    {
        return items.Sum(item => item.Herb.Price * item.Dosage);
    }

    /// <summary>
    /// 计算单项价格
    /// </summary>
    public decimal CalculateItemPrice(Herb herb, decimal dosage)
    {
        return herb.Price * dosage;
    }

    /// <summary>
    /// 验证价格合理性
    /// </summary>
    public PriceValidationResult ValidatePrice(decimal totalPrice)
    {
        if (totalPrice <= 0)
            return PriceValidationResult.Invalid("处方总价必须大于0");

        if (totalPrice > 10000)
            return PriceValidationResult.Warning("处方总价超过10000元，请确认是否正确");

        return PriceValidationResult.Valid();
    }
}
```

**提取组件2: FormulaImportHandler**
```csharp
/// <summary>
/// 验方导入处理器
/// 职责：从验方库导入药材组合
/// </summary>
public class FormulaImportHandler
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;

    public FormulaImportHandler(IFormulaRepository formulaRepository, IDialogService dialogService)
    {
        _formulaRepository = formulaRepository;
        _dialogService = dialogService;
    }

    /// <summary>
    /// 选择并导入验方
    /// </summary>
    public async Task<FormulaImportResult> SelectAndImportFormulaAsync()
    {
        FormulaImportResult result = null;

        _dialogService.ShowDialog("SelectFormulaDialog", parameters: null, callback: r =>
        {
            if (r.Result == ButtonResult.OK)
            {
                var selectedFormula = r.Parameters.GetValue<Formula>("selectedFormula");
                result = FormulaImportResult.Success(selectedFormula);
            }
            else
            {
                result = FormulaImportResult.Cancelled();
            }
        });

        return result;
    }

    /// <summary>
    /// 将验方转换为处方项
    /// </summary>
    public IEnumerable<PrescriptionItem> ConvertFormulaToItems(Formula formula)
    {
        return formula.Herbs.Select(fh => new PrescriptionItem
        {
            HerbId = fh.HerbId,
            Herb = fh.Herb,
            Dosage = fh.Dosage,
            Unit = fh.Unit,
            Notes = fh.Notes
        });
    }
}
```

**提取组件3: HerbSelectionManager**
```csharp
/// <summary>
/// 药材选择管理器
/// 职责：管理药材选择对话框、处理选择结果
/// </summary>
public class HerbSelectionManager
{
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;

    public HerbSelectionManager(IDialogService dialogService, IEventAggregator eventAggregator)
    {
        _dialogService = dialogService;
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    /// 选择单个药材
    /// </summary>
    public async Task<Herb> SelectSingleHerbAsync()
    {
        Herb selectedHerb = null;

        _dialogService.ShowDialog("HerbSelectionDialog", new DialogParameters
        {
            { "selectionMode", "single" }
        }, r =>
        {
            if (r.Result == ButtonResult.OK)
            {
                selectedHerb = r.Parameters.GetValue<Herb>("selectedHerb");
            }
        });

        return selectedHerb;
    }

    /// <summary>
    /// 选择多个药材
    /// </summary>
    public async Task<IEnumerable<Herb>> SelectMultipleHerbsAsync()
    {
        IEnumerable<Herb> selectedHerbs = null;

        _dialogService.ShowDialog("HerbSelectionDialog", new DialogParameters
        {
            { "selectionMode", "multiple" }
        }, r =>
        {
            if (r.Result == ButtonResult.OK)
            {
                selectedHerbs = r.Parameters.GetValue<IEnumerable<Herb>>("selectedHerbs");
            }
        });

        return selectedHerbs ?? Enumerable.Empty<Herb>();
    }

    /// <summary>
    /// 发布药材选择完成事件
    /// </summary>
    public void PublishHerbSelectedEvent(Herb herb)
    {
        _eventAggregator.GetEvent<HerbSelectedEvent>().Publish(herb);
    }
}
```

**重构后的PrescriptionEditorViewModel**
```csharp
/// <summary>
/// 处方编辑器ViewModel（重构后）
/// 职责：UI协调、事件处理
/// </summary>
public class PrescriptionEditorViewModel : BindableBase, INavigationAware
{
    private readonly PrescriptionCalculator _calculator;
    private readonly FormulaImportHandler _formulaImportHandler;
    private readonly HerbSelectionManager _herbSelectionManager;
    private readonly IPrescriptionRepository _prescriptionRepository;

    // UI绑定属性
    private ObservableCollection<PrescriptionItem> _items;
    public ObservableCollection<PrescriptionItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }

    // Commands
    public DelegateCommand AddHerbCommand { get; }
    public DelegateCommand ImportFormulaCommand { get; }
    public DelegateCommand<PrescriptionItem> RemoveItemCommand { get; }
    public DelegateCommand SaveCommand { get; }

    public PrescriptionEditorViewModel(
        PrescriptionCalculator calculator,
        FormulaImportHandler formulaImportHandler,
        HerbSelectionManager herbSelectionManager,
        IPrescriptionRepository prescriptionRepository)
    {
        _calculator = calculator;
        _formulaImportHandler = formulaImportHandler;
        _herbSelectionManager = herbSelectionManager;
        _prescriptionRepository = prescriptionRepository;

        Items = new ObservableCollection<PrescriptionItem>();
        Items.CollectionChanged += OnItemsChanged;

        AddHerbCommand = new DelegateCommand(async () => await AddHerbAsync());
        ImportFormulaCommand = new DelegateCommand(async () => await ImportFormulaAsync());
        RemoveItemCommand = new DelegateCommand<PrescriptionItem>(RemoveItem);
        SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
    }

    private async Task AddHerbAsync()
    {
        var herb = await _herbSelectionManager.SelectSingleHerbAsync();
        if (herb != null)
        {
            var item = new PrescriptionItem
            {
                HerbId = herb.Id,
                Herb = herb,
                Dosage = 10, // 默认剂量
                Unit = "g"
            };
            Items.Add(item);

            _herbSelectionManager.PublishHerbSelectedEvent(herb);
        }
    }

    private async Task ImportFormulaAsync()
    {
        var result = await _formulaImportHandler.SelectAndImportFormulaAsync();
        if (result.IsSuccess)
        {
            var items = _formulaImportHandler.ConvertFormulaToItems(result.Formula);
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
    }

    private void RemoveItem(PrescriptionItem item)
    {
        Items.Remove(item);
    }

    private async Task SaveAsync()
    {
        // 验证价格
        var validationResult = _calculator.ValidatePrice(TotalPrice);
        if (!validationResult.IsValid)
        {
            // 显示验证错误
            return;
        }

        // 保存处方
        var prescription = new Prescription
        {
            MedicalCaseId = CurrentMedicalCaseId,
            Items = Items.ToList(),
            TotalPrice = TotalPrice
        };

        await _prescriptionRepository.CreateAsync(prescription);
    }

    private bool CanSave()
    {
        return Items.Any() && TotalPrice > 0;
    }

    private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // 重新计算总价
        TotalPrice = _calculator.CalculateTotalPrice(Items);
        SaveCommand.RaiseCanExecuteChanged();
    }
}
```

#### 效果评估
- **代码行数**: 500行 → 150行（ViewModel）+ 80行（Calculator）+ 100行（FormulaImportHandler）+ 80行（HerbSelectionManager）= 410行总计
- **ViewModel行数减少**: 70%
- **职责数**: 3个 → 2个
- **Mock依赖**: 4个 → 4个（保持，但组件可独立测试）

---

### 2.3 HerbManagementViewModel 组件化

#### 当前状态
```
文件: src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbManagementViewModel.cs
行数: ~400行
职责数: 3个
- UI协调
- 搜索和分页管理
- CRUD操作
```

#### 组件化设计

**提取组件: HerbSearchManager**
```csharp
/// <summary>
/// 药材搜索管理器
/// 职责：搜索逻辑、分页管理、筛选条件
/// </summary>
public class HerbSearchManager
{
    private readonly IHerbRepository _herbRepository;
    private readonly IEventAggregator _eventAggregator;

    // 搜索参数
    public string SearchKeyword { get; set; }
    public string SelectedCategory { get; set; }
    public string SelectedEfficacy { get; set; }

    // 分页参数
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public HerbSearchManager(IHerbRepository herbRepository, IEventAggregator eventAggregator)
    {
        _herbRepository = herbRepository;
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    public async Task<SearchResult<Herb>> SearchAsync()
    {
        var searchParams = new HerbSearchParameters
        {
            Keyword = SearchKeyword,
            Category = SelectedCategory,
            Efficacy = SelectedEfficacy,
            Page = CurrentPage,
            PageSize = PageSize
        };

        var result = await _herbRepository.SearchAsync(searchParams);
        TotalCount = result.TotalCount;

        // 发布搜索完成事件
        _eventAggregator.GetEvent<HerbSearchCompletedEvent>().Publish(result);

        return result;
    }

    /// <summary>
    /// 清空搜索条件
    /// </summary>
    public void ClearSearchCriteria()
    {
        SearchKeyword = string.Empty;
        SelectedCategory = null;
        SelectedEfficacy = null;
        CurrentPage = 1;
    }

    /// <summary>
    /// 跳转到指定页
    /// </summary>
    public async Task<SearchResult<Herb>> GoToPageAsync(int pageNumber)
    {
        CurrentPage = pageNumber;
        return await SearchAsync();
    }

    /// <summary>
    /// 下一页
    /// </summary>
    public async Task<SearchResult<Herb>> NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            return await SearchAsync();
        }
        return null;
    }

    /// <summary>
    /// 上一页
    /// </summary>
    public async Task<SearchResult<Herb>> PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            return await SearchAsync();
        }
        return null;
    }
}
```

**重构后的HerbManagementViewModel**
```csharp
/// <summary>
/// 药材管理ViewModel（重构后）
/// 职责：UI协调、事件处理
/// </summary>
public class HerbManagementViewModel : BindableBase
{
    private readonly HerbSearchManager _searchManager;
    private readonly IHerbRepository _herbRepository;
    private readonly IDialogService _dialogService;

    // UI绑定属性
    private ObservableCollection<Herb> _herbs;
    public ObservableCollection<Herb> Herbs
    {
        get => _herbs;
        set => SetProperty(ref _herbs, value);
    }

    public string SearchKeyword
    {
        get => _searchManager.SearchKeyword;
        set
        {
            _searchManager.SearchKeyword = value;
            RaisePropertyChanged();
        }
    }

    public int CurrentPage => _searchManager.CurrentPage;
    public int TotalPages => _searchManager.TotalPages;

    // Commands
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand ClearCommand { get; }
    public DelegateCommand CreateHerbCommand { get; }
    public DelegateCommand<Herb> EditHerbCommand { get; }
    public DelegateCommand<Herb> DeleteHerbCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }

    public HerbManagementViewModel(
        HerbSearchManager searchManager,
        IHerbRepository herbRepository,
        IDialogService dialogService)
    {
        _searchManager = searchManager;
        _herbRepository = herbRepository;
        _dialogService = dialogService;

        SearchCommand = new DelegateCommand(async () => await SearchAsync());
        ClearCommand = new DelegateCommand(ClearSearch);
        CreateHerbCommand = new DelegateCommand(CreateHerb);
        EditHerbCommand = new DelegateCommand<Herb>(EditHerb);
        DeleteHerbCommand = new DelegateCommand<Herb>(async herb => await DeleteHerbAsync(herb));
        NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);
        PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), () => CurrentPage > 1);
    }

    private async Task SearchAsync()
    {
        var result = await _searchManager.SearchAsync();
        Herbs = new ObservableCollection<Herb>(result.Items);
        RaisePropertyChanged(nameof(CurrentPage));
        RaisePropertyChanged(nameof(TotalPages));
        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
    }

    private void ClearSearch()
    {
        _searchManager.ClearSearchCriteria();
        RaisePropertyChanged(nameof(SearchKeyword));
        SearchCommand.Execute();
    }

    private async Task NextPageAsync()
    {
        var result = await _searchManager.NextPageAsync();
        if (result != null)
        {
            Herbs = new ObservableCollection<Herb>(result.Items);
            RaisePropertyChanged(nameof(CurrentPage));
            NextPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task PreviousPageAsync()
    {
        var result = await _searchManager.PreviousPageAsync();
        if (result != null)
        {
            Herbs = new ObservableCollection<Herb>(result.Items);
            RaisePropertyChanged(nameof(CurrentPage));
            NextPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
        }
    }

    private void CreateHerb()
    {
        _dialogService.ShowDialog("HerbFormDialog", new DialogParameters
        {
            { "mode", "create" }
        }, async r =>
        {
            if (r.Result == ButtonResult.OK)
            {
                await SearchAsync(); // 刷新列表
            }
        });
    }

    private void EditHerb(Herb herb)
    {
        _dialogService.ShowDialog("HerbFormDialog", new DialogParameters
        {
            { "mode", "edit" },
            { "herbId", herb.Id }
        }, async r =>
        {
            if (r.Result == ButtonResult.OK)
            {
                await SearchAsync(); // 刷新列表
            }
        });
    }

    private async Task DeleteHerbAsync(Herb herb)
    {
        // 确认对话框
        bool confirmed = false;
        _dialogService.ShowDialog("ConfirmDialog", new DialogParameters
        {
            { "message", $"确认删除药材 \"{herb.Name}\" ？" }
        }, r => confirmed = r.Result == ButtonResult.OK);

        if (confirmed)
        {
            await _herbRepository.DeleteAsync(herb.Id);
            await SearchAsync(); // 刷新列表
        }
    }
}
```

#### 效果评估
- **代码行数**: 400行 → 180行（ViewModel）+ 150行（HerbSearchManager）= 330行总计
- **ViewModel行数减少**: 55%
- **职责数**: 3个 → 2个
- **搜索逻辑独立测试**: ✅

---

### 2.4 FormulaManagementViewModel 组件化

#### 当前状态
```
文件: src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs
行数: ~450行
职责数: 3个
- UI协调
- 搜索和分页管理
- 验方校验逻辑
```

#### 组件化设计

**提取组件1: FormulaSearchManager**
```csharp
/// <summary>
/// 验方搜索管理器
/// 职责：搜索逻辑、分页管理
/// </summary>
public class FormulaSearchManager
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IEventAggregator _eventAggregator;

    public string SearchKeyword { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public FormulaSearchManager(IFormulaRepository formulaRepository, IEventAggregator eventAggregator)
    {
        _formulaRepository = formulaRepository;
        _eventAggregator = eventAggregator;
    }

    public async Task<SearchResult<Formula>> SearchAsync()
    {
        var searchParams = new FormulaSearchParameters
        {
            Keyword = SearchKeyword,
            Page = CurrentPage,
            PageSize = PageSize
        };

        var result = await _formulaRepository.SearchAsync(searchParams);
        TotalCount = result.TotalCount;

        _eventAggregator.GetEvent<FormulaSearchCompletedEvent>().Publish(result);

        return result;
    }

    public void ClearSearchCriteria()
    {
        SearchKeyword = string.Empty;
        CurrentPage = 1;
    }

    public async Task<SearchResult<Formula>> GoToPageAsync(int pageNumber)
    {
        CurrentPage = pageNumber;
        return await SearchAsync();
    }
}
```

**提取组件2: FormulaValidationHandler**
```csharp
/// <summary>
/// 验方校验处理器
/// 职责：验方合理性校验、十八反十九畏检查
/// </summary>
public class FormulaValidationHandler
{
    private readonly IFormulaValidationService _validationService;
    private readonly IDialogService _dialogService;

    public FormulaValidationHandler(IFormulaValidationService validationService, IDialogService dialogService)
    {
        _validationService = validationService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// 校验验方
    /// </summary>
    public async Task<ValidationResult> ValidateFormulaAsync(Formula formula)
    {
        var result = await _validationService.ValidateAsync(formula);
        return result;
    }

    /// <summary>
    /// 显示校验结果对话框
    /// </summary>
    public void ShowValidationResultDialog(ValidationResult result)
    {
        _dialogService.ShowDialog("FormulaValidationView", new DialogParameters
        {
            { "validationResult", result }
        }, null);
    }

    /// <summary>
    /// 检查十八反十九畏
    /// </summary>
    public async Task<ConflictCheckResult> CheckHerbConflictsAsync(IEnumerable<Herb> herbs)
    {
        return await _validationService.CheckConflictsAsync(herbs);
    }
}
```

**重构后的FormulaManagementViewModel**
```csharp
/// <summary>
/// 验方管理ViewModel（重构后）
/// 职责：UI协调、事件处理
/// </summary>
public class FormulaManagementViewModel : BindableBase
{
    private readonly FormulaSearchManager _searchManager;
    private readonly FormulaValidationHandler _validationHandler;
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;

    // UI绑定属性
    private ObservableCollection<Formula> _formulas;
    public ObservableCollection<Formula> Formulas
    {
        get => _formulas;
        set => SetProperty(ref _formulas, value);
    }

    public string SearchKeyword
    {
        get => _searchManager.SearchKeyword;
        set
        {
            _searchManager.SearchKeyword = value;
            RaisePropertyChanged();
        }
    }

    // Commands
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand ClearCommand { get; }
    public DelegateCommand CreateFormulaCommand { get; }
    public DelegateCommand<Formula> EditFormulaCommand { get; }
    public DelegateCommand<Formula> ValidateFormulaCommand { get; }
    public DelegateCommand<Formula> DeleteFormulaCommand { get; }

    public FormulaManagementViewModel(
        FormulaSearchManager searchManager,
        FormulaValidationHandler validationHandler,
        IFormulaRepository formulaRepository,
        IDialogService dialogService)
    {
        _searchManager = searchManager;
        _validationHandler = validationHandler;
        _formulaRepository = formulaRepository;
        _dialogService = dialogService;

        SearchCommand = new DelegateCommand(async () => await SearchAsync());
        ClearCommand = new DelegateCommand(ClearSearch);
        CreateFormulaCommand = new DelegateCommand(CreateFormula);
        EditFormulaCommand = new DelegateCommand<Formula>(EditFormula);
        ValidateFormulaCommand = new DelegateCommand<Formula>(async f => await ValidateFormulaAsync(f));
        DeleteFormulaCommand = new DelegateCommand<Formula>(async f => await DeleteFormulaAsync(f));
    }

    private async Task SearchAsync()
    {
        var result = await _searchManager.SearchAsync();
        Formulas = new ObservableCollection<Formula>(result.Items);
    }

    private void ClearSearch()
    {
        _searchManager.ClearSearchCriteria();
        RaisePropertyChanged(nameof(SearchKeyword));
        SearchCommand.Execute();
    }

    private async Task ValidateFormulaAsync(Formula formula)
    {
        var validationResult = await _validationHandler.ValidateFormulaAsync(formula);
        _validationHandler.ShowValidationResultDialog(validationResult);
    }

    // ... 其他方法省略（与HerbManagementViewModel类似）
}
```

#### 效果评估
- **代码行数**: 450行 → 180行（ViewModel）+ 120行（SearchManager）+ 100行（ValidationHandler）= 400行总计
- **ViewModel行数减少**: 60%
- **职责数**: 3个 → 2个
- **验证逻辑独立测试**: ✅

---

## 3. 实施计划

### 3.1 Phase 2 Timeline

| 周次 | 任务 | 工作量 | 依赖 |
|------|------|--------|------|
| Week 1 | MedicalCaseFlowViewModel组件化 | 40小时 | Phase 1完成 |
| Week 2 | PrescriptionEditorViewModel组件化 | 32小时 | Week 1完成 |
| Week 3 | HerbManagementViewModel组件化 | 24小时 | Week 2完成 |
| Week 4 | FormulaManagementViewModel组件化 | 24小时 | Week 3完成 |

**总工期**: 4周（120小时）

### 3.2 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 事件订阅内存泄漏 | 中 | 高 | 所有ViewModel实现IDisposable，确保取消订阅 |
| 组件间循环依赖 | 中 | 高 | Manager/Handler不依赖ViewModel，使用事件通信 |
| 单元测试Mock复杂度上升 | 低 | 中 | 组件独立测试，减少ViewModel测试Mock数量 |
| 性能回归（事件开销） | 低 | 低 | 性能基准测试，事件发布异步处理 |

---

## 4. 验收标准

### 4.1 代码质量
- [ ] 所有ViewModel行数<300行
- [ ] 所有ViewModel职责数≤2个
- [ ] 所有Manager/Handler组件单一职责
- [ ] 无循环依赖（Manager/Handler不依赖ViewModel）
- [ ] 所有事件订阅正确清理（实现Dispose）
- [ ] 编译通过，0 warnings

### 4.2 测试覆盖
- [ ] ViewModel单元测试覆盖率≥80%
- [ ] Manager/Handler组件单元测试覆盖率≥85%
- [ ] Mock依赖数减少≥50%
- [ ] 所有单元测试通过（0 failed）

### 4.3 架构合规
- [ ] 符合ADR-009组件化规范
- [ ] DI生命周期正确（Manager/Handler: Scoped, ViewModel: Transient）
- [ ] 事件通信模式正确使用
- [ ] 无ServiceLocator或Container.Resolve反模式

### 4.4 功能完整性
- [ ] 所有原有功能正常工作
- [ ] 回归测试通过（8个核心模块）
- [ ] 性能无回归（UI响应时间≤基线+10%）
- [ ] 无内存泄漏（运行24小时内存稳定）

---

## 5. 测试策略

### 5.1 单元测试示例

**MedicalCaseFlowManager测试**:
```csharp
public class MedicalCaseFlowManagerTests
{
    [Fact]
    public async Task InitializeFlowAsync_WithNoUnfinishedCase_ShouldCreateNewCase()
    {
        // Arrange
        var repository = Substitute.For<IMedicalCaseRepository>();
        repository.GetUnfinishedCaseAsync(Arg.Any<Guid>()).Returns(Task.FromResult<MedicalCase>(null));

        var manager = new MedicalCaseFlowManager(repository, ...);

        // Act
        var result = await manager.InitializeFlowAsync(patientId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.MedicalCaseId);
        await repository.Received(1).CreateAsync(Arg.Any<MedicalCase>());
    }

    [Fact]
    public async Task InitializeFlowAsync_WithUnfinishedCase_ShouldReturnBlocked()
    {
        // Arrange
        var repository = Substitute.For<IMedicalCaseRepository>();
        repository.GetUnfinishedCaseAsync(Arg.Any<Guid>()).Returns(Task.FromResult(new MedicalCase()));

        var manager = new MedicalCaseFlowManager(repository, ...);

        // Act
        var result = await manager.InitializeFlowAsync(patientId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("未完成的医案", result.Message);
    }
}
```

**PrescriptionCalculator测试**:
```csharp
public class PrescriptionCalculatorTests
{
    [Fact]
    public void CalculateTotalPrice_ShouldSumAllItemPrices()
    {
        // Arrange
        var calculator = new PrescriptionCalculator();
        var items = new List<PrescriptionItem>
        {
            new PrescriptionItem { Herb = new Herb { Price = 10 }, Dosage = 5 },
            new PrescriptionItem { Herb = new Herb { Price = 20 }, Dosage = 3 }
        };

        // Act
        var total = calculator.CalculateTotalPrice(items);

        // Assert
        Assert.Equal(110, total); // 10*5 + 20*3 = 110
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(5000, true)]
    [InlineData(15000, false)] // 超过上限，返回警告
    public void ValidatePrice_ShouldValidateCorrectly(decimal price, bool expectedValid)
    {
        // Arrange
        var calculator = new PrescriptionCalculator();

        // Act
        var result = calculator.ValidatePrice(price);

        // Assert
        Assert.Equal(expectedValid, result.IsValid);
    }
}
```

### 5.2 集成测试

**事件通信测试**:
```csharp
[Fact]
public async Task FlowManager_WhenConsultationCompleted_ShouldPublishEvent()
{
    // Arrange
    var eventAggregator = new EventAggregator();
    var manager = new MedicalCaseFlowManager(..., eventAggregator);

    ConsultationCompletedEvent receivedEvent = null;
    eventAggregator.GetEvent<ConsultationCompletedEvent>().Subscribe(e => receivedEvent = e);

    // Act
    await manager.CompleteConsultationAsync(medicalCaseId, consultation);

    // Assert
    Assert.NotNull(receivedEvent);
    Assert.Equal(medicalCaseId, receivedEvent);
}
```

---

## 6. 相关文档

- **架构决策**: `docs/explanation/architecture/decisions/ADR-009-desktop-component-pattern.md`
- **Issue #1790**: PatientSelectionViewModel组件化（参考实例）
- **Issue #1795**: 方法复杂度控制标准
- **重构计划**: `docs/reports/ui-ux-refactoring-plan-2025-11-04.md`
- **Phase 1 PRD**: `docs/requirements/ui-refactoring-phase1-prd.md`

---

**文档状态**: ✅ 待创建GitHub Issues
**下一步**: 创建Phase 2的GitHub Issues（#1805 Epic + 4个子Issues）

**最后更新**: 2025-11-04
