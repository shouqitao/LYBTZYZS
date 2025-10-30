# 病案管理(Client端)开发指南

> **文档版本**: v1.0
> **最后更新**: 2025-01-30
> **适用范围**: LYBT.Desktop.MedicalCase 模块开发

## 📋 目录

1. [概述](#1-概述)
2. [MVVM架构实践](#2-mvvm架构实践)
3. [FlowViewModel实现](#3-flowviewmodel实现)
4. [子步骤ViewModel开发](#4-子步骤viewmodel开发)
5. [XAML视图设计](#5-xaml视图设计)
6. [Repository数据访问](#6-repository数据访问)
7. [数据验证与保存](#7-数据验证与保存)
8. [Prism Region导航](#8-prism-region导航)
9. [事件总线通信](#9-事件总线通信)
10. [跨端组件复用](#10-跨端组件复用)
11. [错误处理与日志](#11-错误处理与日志)
12. [用户交互反馈](#12-用户交互反馈)
13. [常见问题与陷阱](#13-常见问题与陷阱)
14. [检查清单](#14-检查清单)
15. [参考资料](#15-参考资料)

---

## 1. 概述

### 1.1 模块职责

LYBT.Desktop.MedicalCase 模块负责 Client 端的病案管理功能，包括：

- **看病流程管理**：辨证 → 施治 → 完成三步骤工作流
- **病案数据录入**：诊疗记录、处方信息、患者信息展示
- **状态管理**：暂存、取消、完成病案的生命周期控制
- **数据验证**：基于 IValidatable 接口的统一验证机制
- **导航控制**：Prism Region 实现的模块化视图切换

### 1.2 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| WPF | .NET 8.0 | UI框架 |
| Prism | 9.0.x | MVVM框架、依赖注入、事件总线 |
| LYBT.Client.Infrastructure | v1.0 | Repository、UnifiedViewModelBase |
| LYBT.Shared.Models | v1.0 | DTO模型 |
| LYBT.Shared.Components | v1.0 | 中药计算器、验证器 |

### 1.3 架构图

```
┌─────────────────────────────────────────────────────────┐
│                   MedicalCaseFlowView                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │          WorkflowContentRegion (Prism)           │  │
│  │  ┌─────────────────────────────────────────────┐ │  │
│  │  │  ConsultationFormView (Step 1: 辨证)        │ │  │
│  │  │  PrescriptionEditorView (Step 2: 施治)      │ │  │
│  │  │  CompletionView (Step 3: 完成)              │ │  │
│  │  └─────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │   MedicalCaseFlowViewModel      │
        │   - CurrentStep管理             │
        │   - Region导航控制              │
        │   - IValidatable/ISaveable协调  │
        └─────────────────────────────────┘
                          │
        ┌─────────────────┴───────────────┐
        │                                 │
        ▼                                 ▼
┌───────────────────┐          ┌──────────────────────┐
│  ConsultationVM   │          │  PrescriptionVM      │
│  - 诊疗记录       │          │  - 处方药材          │
│  - IValidatable   │          │  - 价格计算          │
│  - ISaveable      │          │  - IValidatable      │
└───────────────────┘          └──────────────────────┘
        │                                 │
        └─────────────────┬───────────────┘
                          ▼
        ┌─────────────────────────────────┐
        │   IMedicalCaseRepository        │
        │   - CreateAsync()               │
        │   - UpdateAsync()               │
        │   - GetByIdWithDetailsAsync()   │
        └─────────────────────────────────┘
                          │
                          ▼
               ┌──────────────────┐
               │   WebAPI Server  │
               │   /api/v1/medical│
               └──────────────────┘
```

---

## 2. MVVM架构实践

### 2.1 ViewModel基类继承

所有 ViewModel 继承自 `UnifiedViewModelBase`，提供统一的基础功能：

```csharp
using LYBT.Client.Infrastructure.ViewModels;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 病案流程控制ViewModel
    /// Epic #1494: 医案流程UI重构
    /// </summary>
    public class MedicalCaseFlowViewModel : UnifiedViewModelBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IMedicalCaseRepository _medicalCaseRepository;

        public MedicalCaseFlowViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IRegionManager regionManager,
            IContainerProvider containerProvider,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令和事件订阅
            InitializeCommands();
            SubscribeToEvents();
        }

        private void InitializeCommands()
        {
            // DelegateCommand初始化（见下一节）
        }

        private void SubscribeToEvents()
        {
            // EventAggregator事件订阅（见9.事件总线通信章节）
        }
    }
}
```

**继承的核心功能**：
- ✅ `INotifyPropertyChanged` 实现（`SetProperty` 方法）
- ✅ `INavigationAware` 接口（`OnNavigatedTo/From`）
- ✅ `Logger` 日志记录器
- ✅ `EventAggregator` 事件总线
- ✅ `SessionManager` 当前用户会话
- ✅ `IsBusy` 加载状态管理
- ✅ `UserNotificationService` 用户通知服务

### 2.2 属性定义与绑定

**核心原则**：使用 `SetProperty` 方法自动触发 `PropertyChanged` 事件

```csharp
#region 属性

private ConsultationStep _currentStep = ConsultationStep.Consultation;
/// <summary>
/// 当前流程步骤
/// Issue #1567 - 重构为ConsultationStep（删除患者选择）
/// </summary>
public ConsultationStep CurrentStep
{
    get => _currentStep;
    set
    {
        if (SetProperty(ref _currentStep, value))
        {
            // SetProperty返回true表示值已变更，触发关联属性更新
            RaisePropertyChanged(nameof(CanGoBack));
            RaisePropertyChanged(nameof(CanGoNext));
            RaisePropertyChanged(nameof(NextButtonText));
            RaisePropertyChanged(nameof(PreviousButtonText));

            // 更新步骤名称文本
            UpdateCurrentStepText();

            // 刷新命令的CanExecute状态
            PreviousStepCommand.RaiseCanExecuteChanged();
            NextStepCommand.RaiseCanExecuteChanged();
        }
    }
}

private ViewModelBase? _currentStepViewModel;
/// <summary>
/// 当前步骤的ViewModel（用于ContentControl绑定）
/// </summary>
public ViewModelBase? CurrentStepViewModel
{
    get => _currentStepViewModel;
    set => SetProperty(ref _currentStepViewModel, value);
}

private PatientDto? _currentPatient;
/// <summary>
/// 当前选择的患者信息（用于传递给子步骤ViewModel）
/// </summary>
public PatientDto? CurrentPatient
{
    get => _currentPatient;
    set => SetProperty(ref _currentPatient, value);
}

/// <summary>
/// 是否可以返回上一步（计算属性）
/// </summary>
public bool CanGoBack => CurrentStep > ConsultationStep.Consultation;

/// <summary>
/// 是否可以前进下一步（计算属性）
/// </summary>
public bool CanGoNext => CurrentStep < ConsultationStep.Completion;

/// <summary>
/// 下一步按钮文字（动态计算）
/// </summary>
public string NextButtonText => CurrentStep == ConsultationStep.Completion ? "完成病案" : "下一步";

#endregion
```

**关键点**：
- ✅ **私有字段命名**：`_camelCase` 格式
- ✅ **计算属性**：使用 `=>` 表达式体，避免重复存储
- ✅ **级联更新**：当某个属性变更影响其他属性时，显式调用 `RaisePropertyChanged`
- ✅ **命令刷新**：属性变更后调用 `RaiseCanExecuteChanged()` 更新按钮状态

### 2.3 DelegateCommand命令

**Prism命令模式**：支持异步、CanExecute逻辑和属性监听

#### 同步命令

```csharp
public DelegateCommand BackToHomeCommand { get; }
public DelegateCommand PreviousStepCommand { get; }
public DelegateCommand CancelCommand { get; }

private void InitializeCommands()
{
    // 1. 无CanExecute判断的命令
    BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);

    // 2. 带CanExecute判断的命令
    PreviousStepCommand = new DelegateCommand(
        ExecutePreviousStep,
        CanExecutePreviousStep);

    // 3. 异步命令（自动处理async/await）
    NextStepCommand = new DelegateCommand(
        async () => await ExecuteNextStepAsync(),
        CanExecuteNextStep)
        .ObservesProperty(() => CurrentPatient)  // 监听属性变化自动刷新CanExecute
        .ObservesProperty(() => IsBusy);
}

private void ExecuteBackToHome()
{
    try
    {
        Logger.LogInformation("返回患者选择页面");
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "返回患者选择时发生异常");
    }
}

private void ExecutePreviousStep()
{
    if (CurrentStep <= ConsultationStep.Consultation)
    {
        Logger.LogWarning("已是第一步，无法返回");
        return;
    }

    var previousStep = (ConsultationStep)((int)CurrentStep - 1);
    Logger.LogInformation("从 {CurrentStep} 返回到 {PreviousStep}", CurrentStep, previousStep);
    NavigateToStep(previousStep);
}

private bool CanExecutePreviousStep()
{
    return CanGoBack;
}
```

#### 异步命令（关键模式）

```csharp
private async Task ExecuteNextStepAsync()
{
    // ⚠️ 所有异步操作必须设置IsBusy状态，防止用户重复操作
    try
    {
        SetIsBusy(true, "正在处理...");

        // 1. 验证当前步骤数据
        if (CurrentStepViewModel is IValidatable validatable)
        {
            if (!validatable.Validate())
            {
                await ShowErrorMessageAsync(validatable.ValidationMessage);
                return;
            }
        }

        // 2. 保存当前步骤数据
        if (CurrentStepViewModel is ISaveable saveable)
        {
            var saveResult = await saveable.SaveAsync();
            if (!saveResult)
            {
                await ShowErrorMessageAsync("保存失败，请检查数据后重试");
                return;
            }
        }

        // 3. 跳转到下一步
        var nextStep = (ConsultationStep)((int)CurrentStep + 1);
        NavigateToStep(nextStep);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "执行下一步时发生异常");
        await ShowErrorMessageAsync($"操作失败：{ex.Message}");
    }
    finally
    {
        // ⚠️ 必须在finally中重置IsBusy，确保异常时也能恢复
        SetIsBusy(false);
    }
}

private bool CanExecuteNextStep()
{
    // 如果正在处理中，禁用下一步按钮
    if (IsBusy)
    {
        return false;
    }

    return CurrentStep switch
    {
        ConsultationStep.Consultation => true, // Step 1: 辨证（可选，允许前进）
        ConsultationStep.Prescription => true, // Step 2: 施治（可选，允许前进）
        ConsultationStep.Completion => true,   // Step 3: 完成确认
        _ => false
    };
}
```

**命令最佳实践**：
- ✅ **异步命令**：使用 `async () => await` 包装异步方法
- ✅ **CanExecute监听**：使用 `.ObservesProperty()` 自动刷新
- ✅ **IsBusy保护**：异步操作期间禁用按钮，防止重复点击
- ✅ **异常处理**：try-catch-finally 确保状态正确恢复
- ✅ **用户反馈**：使用 `ShowErrorMessageAsync/ShowSuccessMessageAsync` 提示用户

---

## 3. FlowViewModel实现

### 3.1 工作流状态管理

**ConsultationStep枚举定义**：

```csharp
/// <summary>
/// 诊疗流程步骤枚举
/// Issue #1567: 三步骤流程（删除患者选择）
/// </summary>
public enum ConsultationStep
{
    /// <summary>
    /// 步骤1：辨证（诊疗记录）
    /// </summary>
    Consultation = 1,

    /// <summary>
    /// 步骤2：施治（处方开立）
    /// </summary>
    Prescription = 2,

    /// <summary>
    /// 步骤3：完成（完成确认）
    /// </summary>
    Completion = 3
}
```

**步骤导航逻辑**：

```csharp
/// <summary>
/// 导航到指定步骤
/// Issue #1567 - 使用Prism Region导航
/// </summary>
private void NavigateToStep(ConsultationStep step)
{
    CurrentStep = step;

    switch (step)
    {
        case ConsultationStep.Consultation:
            Logger.LogInformation("导航到辨证步骤（使用Region导航）");

            var consultationParameters = new NavigationParameters
            {
                { "MedicalCaseId", MedicalCaseId },
                { "CurrentPatient", CurrentPatient }
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "ConsultationFormView", consultationParameters);
            break;

        case ConsultationStep.Prescription:
            Logger.LogInformation("导航到施治步骤（使用Region导航）");

            var prescriptionParameters = new NavigationParameters
            {
                { "MedicalCaseId", MedicalCaseId },
                { "CurrentPatient", CurrentPatient }
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "PrescriptionEditorView", prescriptionParameters);
            break;

        case ConsultationStep.Completion:
            Logger.LogInformation("导航到完成步骤（使用Region导航）");

            var completionParameters = new NavigationParameters
            {
                { "MedicalCaseId", MedicalCaseId }
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "CompletionView", completionParameters);
            break;

        default:
            Logger.LogWarning("未知步骤：{Step}", step);
            break;
    }
}
```

### 3.2 MedicalCase生命周期管理

#### 创建病案

```csharp
/// <summary>
/// 创建MedicalCase（患者选择后自动创建）
/// Phase 2: 实现真实API调用
/// </summary>
private async Task<Guid> CreateMedicalCaseAsync(Guid patientId)
{
    try
    {
        Logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patientId);

        // 1. 验证SessionManager和CurrentUser
        if (SessionManager?.CurrentUser == null)
        {
            Logger.LogError("SessionManager.CurrentUser为null，无法创建MedicalCase");
            if (UserNotificationService != null)
            {
                _ = UserNotificationService.ShowErrorAsync("用户信息丢失，无法创建医案");
            }
            return Guid.Empty;
        }

        // 2. 构建MedicalCaseCreateDto
        var createDto = new MedicalCaseCreateDto
        {
            PatientId = patientId,
            DoctorId = SessionManager.CurrentUser.Id,
            Status = MedicalCaseStatus.Active, // 初始状态为Active
            Remark = null // 初始创建无备注
        };

        Logger.LogInformation("准备调用API创建MedicalCase，PatientId: {PatientId}, DoctorId: {DoctorId}",
            createDto.PatientId, createDto.DoctorId);

        // 3. 调用真实API创建MedicalCase
        var createdDto = await _medicalCaseRepository.CreateAsync(createDto);

        Logger.LogInformation("MedicalCase创建成功，ID: {MedicalCaseId}", createdDto.Id);
        return createdDto.Id;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "创建MedicalCase失败，PatientId: {PatientId}", patientId);
        if (UserNotificationService != null)
        {
            _ = UserNotificationService.ShowErrorAsync($"创建医案失败：{ex.Message}");
        }
        return Guid.Empty;
    }
}
```

#### 暂存病案（SaveDraft）

```csharp
/// <summary>
/// 暂存医案（保存数据 + 更新状态 + 停留在当前界面）
/// Issue #1567 Phase 3 - Task 3.1
/// </summary>
private async void ExecuteSaveDraft()
{
    try
    {
        Logger.LogInformation("暂存医案，当前步骤：{CurrentStep}, MedicalCaseId: {MedicalCaseId}", CurrentStep, MedicalCaseId);

        SetIsBusy(true, "正在保存...");

        // 1. 调用当前Step的ISaveable接口保存数据
        if (CurrentStepViewModel is ISaveable saveable)
        {
            var success = await saveable.SaveAsync();
            if (!success)
            {
                Logger.LogWarning("当前步骤数据保存失败");
                await ShowErrorMessageAsync("保存失败，请检查数据");
                return;
            }
        }

        // 2. 更新MedicalCase状态为Active
        await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Active);

        Logger.LogInformation("医案暂存成功");
        await ShowSuccessMessageAsync("医案已暂存");

        // Epic #1583 Phase 4: 移除自动导航，暂存后停留在当前界面（修复Issue #1569）
        // 用户可以通过"返回主页"按钮手动返回
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "暂存医案失败");
        await ShowErrorMessageAsync($"暂存失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

#### 取消病案（Cancel）

```csharp
/// <summary>
/// 取消医案（确认对话框 + 更新状态 + 返回患者选择）
/// Issue #1567 Phase 3 - Task 3.2
/// </summary>
private async void ExecuteCancel()
{
    try
    {
        Logger.LogInformation("取消医案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

        // 1. 显示确认对话框
        var confirmed = await ShowConfirmationAsync(
            "确定要取消本次医案吗？未保存的数据将丢失！",
            "取消医案");

        if (!confirmed)
        {
            Logger.LogInformation("用户取消了取消操作");
            return;
        }

        // 2. 更新MedicalCase状态为Cancelled
        await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Cancelled);

        Logger.LogInformation("医案已取消");

        // 3. 返回患者选择界面
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "取消医案失败");
        await ShowErrorMessageAsync($"取消失败：{ex.Message}");
    }
}
```

#### 完成病案（Complete）

```csharp
/// <summary>
/// 完成病案（验证 + 保存 + 更新状态 + 返回患者选择）
/// Issue #1567 Phase 3 - Task 3.3
/// </summary>
private async Task CompleteMedicalCaseAsync()
{
    try
    {
        SetIsBusy(true, "正在完成病案...");

        // 1. 验证并保存当前步骤数据
        if (CurrentStepViewModel is IValidatable validatable)
        {
            if (!validatable.Validate())
            {
                await ShowErrorMessageAsync(validatable.ValidationMessage);
                return;
            }
        }

        if (CurrentStepViewModel is ISaveable saveable)
        {
            var success = await saveable.SaveAsync();
            if (!success)
            {
                await ShowErrorMessageAsync("保存失败，请检查数据");
                return;
            }
        }

        // 2. 更新MedicalCase状态为Completed
        await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Completed);

        Logger.LogInformation("病案已完成");
        await ShowSuccessMessageAsync("病案已完成");

        // 3. 返回患者选择界面
        _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "完成病案失败");
        await ShowErrorMessageAsync($"完成失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

/// <summary>
/// 更新MedicalCase状态
/// Issue #1567 Phase 3 - 支持暂存/取消/完成状态更新
/// </summary>
private async Task UpdateMedicalCaseStatusAsync(MedicalCaseStatus newStatus)
{
    try
    {
        Logger.LogInformation("更新MedicalCase状态，MedicalCaseId: {MedicalCaseId}, 新状态: {NewStatus}",
            MedicalCaseId, newStatus);

        // 构建更新DTO
        var updateDto = new MedicalCaseUpdateDto
        {
            Id = MedicalCaseId,
            Status = newStatus.ToString()
        };

        // 调用API更新状态
        await _medicalCaseRepository.UpdateAsync(updateDto);

        Logger.LogInformation("MedicalCase状态更新成功，新状态: {NewStatus}", newStatus);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "更新MedicalCase状态失败，MedicalCaseId: {MedicalCaseId}, 目标状态: {NewStatus}",
            MedicalCaseId, newStatus);
        throw; // 重新抛出异常，让调用方处理
    }
}
```

---

## 4. 子步骤ViewModel开发

### 4.1 IValidatable接口实现

**接口定义**：

```csharp
/// <summary>
/// 可验证接口（定义在LYBT.Desktop.Contracts）
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// 执行数据验证
    /// </summary>
    /// <returns>验证通过返回true，否则返回false</returns>
    bool Validate();

    /// <summary>
    /// 获取验证失败的错误消息
    /// </summary>
    string ValidationMessage { get; }
}
```

**实现示例（诊疗表单ViewModel）**：

```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    #region 属性

    private string _chiefComplaint = string.Empty;
    /// <summary>
    /// 主诉（必填）
    /// </summary>
    public string ChiefComplaint
    {
        get => _chiefComplaint;
        set => SetProperty(ref _chiefComplaint, value);
    }

    private string _presentIllness = string.Empty;
    /// <summary>
    /// 现病史（必填）
    /// </summary>
    public string PresentIllness
    {
        get => _presentIllness;
        set => SetProperty(ref _presentIllness, value);
    }

    private string _pastHistory = string.Empty;
    /// <summary>
    /// 既往史
    /// </summary>
    public string PastHistory
    {
        get => _pastHistory;
        set => SetProperty(ref _pastHistory, value);
    }

    private string _diagnosis = string.Empty;
    /// <summary>
    /// 诊断（必填）
    /// </summary>
    public string Diagnosis
    {
        get => _diagnosis;
        set => SetProperty(ref _diagnosis, value);
    }

    private string _treatmentPlan = string.Empty;
    /// <summary>
    /// 治则治法
    /// </summary>
    public string TreatmentPlan
    {
        get => _treatmentPlan;
        set => SetProperty(ref _treatmentPlan, value);
    }

    private string _validationMessage = string.Empty;
    /// <summary>
    /// 验证失败的错误消息
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    #endregion

    #region IValidatable实现

    /// <summary>
    /// 验证诊疗记录数据
    /// 必填项：主诉、现病史、诊断
    /// </summary>
    public bool Validate()
    {
        // 清空之前的验证消息
        ValidationMessage = string.Empty;

        var errors = new List<string>();

        // 验证必填字段
        if (string.IsNullOrWhiteSpace(ChiefComplaint))
        {
            errors.Add("主诉不能为空");
        }

        if (string.IsNullOrWhiteSpace(PresentIllness))
        {
            errors.Add("现病史不能为空");
        }

        if (string.IsNullOrWhiteSpace(Diagnosis))
        {
            errors.Add("诊断不能为空");
        }

        // 验证字段长度
        if (ChiefComplaint.Length > 500)
        {
            errors.Add("主诉不能超过500字符");
        }

        if (PresentIllness.Length > 2000)
        {
            errors.Add("现病史不能超过2000字符");
        }

        if (errors.Any())
        {
            ValidationMessage = string.Join("\n", errors);
            Logger.LogWarning("诊疗记录验证失败：{ValidationMessage}", ValidationMessage);
            return false;
        }

        Logger.LogInformation("诊疗记录验证通过");
        return true;
    }

    #endregion
}
```

### 4.2 ISaveable接口实现

**接口定义**：

```csharp
/// <summary>
/// 可保存接口（定义在LYBT.Desktop.Contracts）
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 保存数据到Repository
    /// </summary>
    /// <returns>保存成功返回true，否则返回false</returns>
    Task<bool> SaveAsync();
}
```

**实现示例（诊疗表单ViewModel）**：

```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    private readonly IConsultationRepository _consultationRepository;
    private Guid _medicalCaseId;

    public ConsultationFormViewModel(
        IConsultationRepository consultationRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
    {
        _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));
    }

    #region ISaveable实现

    /// <summary>
    /// 保存诊疗记录
    /// Issue #1562 - 直接保存ConsultationDto（不依赖MedicalCase级联保存）
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        try
        {
            Logger.LogInformation("开始保存诊疗记录，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

            // 1. 验证数据
            if (!Validate())
            {
                Logger.LogWarning("诊疗记录验证失败，无法保存");
                return false;
            }

            // 2. 构建ConsultationDto
            var consultationDto = new ConsultationDto
            {
                MedicalCaseId = _medicalCaseId,
                ChiefComplaint = ChiefComplaint.Trim(),
                PresentIllness = PresentIllness.Trim(),
                PastHistory = PastHistory.Trim(),
                Diagnosis = Diagnosis.Trim(),
                TreatmentPlan = TreatmentPlan.Trim(),
                VisitDate = DateTime.Now
            };

            // 3. 保存到Repository
            // 如果是新增，使用CreateAsync；如果是更新，使用UpdateAsync
            if (_existingConsultationId == Guid.Empty)
            {
                // 新增
                var createdDto = await _consultationRepository.CreateAsync(consultationDto);
                _existingConsultationId = createdDto.Id;
                Logger.LogInformation("诊疗记录创建成功，ConsultationId: {ConsultationId}", createdDto.Id);
            }
            else
            {
                // 更新
                consultationDto.Id = _existingConsultationId;
                await _consultationRepository.UpdateAsync(consultationDto);
                Logger.LogInformation("诊疗记录更新成功，ConsultationId: {ConsultationId}", _existingConsultationId);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存诊疗记录失败，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);
            return false;
        }
    }

    #endregion

    #region INavigationAware实现

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 接收FlowViewModel传入的参数
        _medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
        var currentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

        Logger.LogInformation("进入诊疗表单，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
            _medicalCaseId, currentPatient?.Name);

        // 如果是继续看诊，加载已有的Consultation数据
        var loadedConsultation = navigationContext.Parameters.GetValue<ConsultationDto>("LoadedConsultation");
        if (loadedConsultation != null)
        {
            LoadConsultationData(loadedConsultation);
        }
    }

    private void LoadConsultationData(ConsultationDto consultation)
    {
        _existingConsultationId = consultation.Id;
        ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
        PresentIllness = consultation.PresentIllness ?? string.Empty;
        PastHistory = consultation.PastHistory ?? string.Empty;
        Diagnosis = consultation.Diagnosis ?? string.Empty;
        TreatmentPlan = consultation.TreatmentPlan ?? string.Empty;

        Logger.LogInformation("加载已有诊疗记录，ConsultationId: {ConsultationId}", consultation.Id);
    }

    #endregion
}
```

**关键点**：
- ✅ **验证先行**：SaveAsync内部先调用Validate()，验证失败直接返回false
- ✅ **新增/更新判断**：通过_existingConsultationId判断是创建还是更新
- ✅ **数据清洗**：使用Trim()清理首尾空格
- ✅ **日志记录**：记录保存操作的关键信息，方便排查问题
- ✅ **异常处理**：捕获异常返回false，不抛出异常（由FlowViewModel统一处理）

---

## 5. XAML视图设计

### 5.1 FlowView主视图结构

**MedicalCaseFlowView.xaml** - 四行布局结构：

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseFlowView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid Background="#F9F9F9">
        <Grid.RowDefinitions>
            <RowDefinition Height="60"/>  <!-- Row 0: 顶部导航栏 -->
            <RowDefinition Height="50"/>  <!-- Row 1: 患者信息条 -->
            <RowDefinition Height="*"/>   <!-- Row 2: 主内容区 -->
            <RowDefinition Height="80"/>  <!-- Row 3: 底部操作栏 -->
        </Grid.RowDefinitions>

        <!-- Row 0: 顶部导航栏 -->
        <Border Grid.Row="0" Background="White" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid Margin="20,0">
                <!-- 左侧：返回按钮 -->
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <Button Command="{Binding BackToHomeCommand}"
                           Background="Transparent"
                           BorderThickness="0">
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="← " FontSize="18" Foreground="#2E86AB" />
                            <TextBlock Text="返回患者选择" FontSize="14" Foreground="#2E86AB" />
                        </StackPanel>
                    </Button>
                </StackPanel>

                <!-- 中间：当前步骤标题 -->
                <TextBlock FontSize="18" FontWeight="Bold"
                          HorizontalAlignment="Center"
                          VerticalAlignment="Center">
                    <Run Text="看病中 - " />
                    <Run Text="{Binding CurrentStepText}" Foreground="#2E86AB" />
                </TextBlock>

                <!-- 右侧：取消诊疗按钮 -->
                <StackPanel Orientation="Horizontal"
                           HorizontalAlignment="Right"
                           VerticalAlignment="Center">
                    <Button Content="取消诊疗"
                           Command="{Binding CancelCommand}"
                           Background="#F44336"
                           Foreground="White"
                           Padding="20,10" />
                </StackPanel>
            </Grid>
        </Border>

        <!-- Row 1: 患者信息条 -->
        <Border Grid.Row="1"
               Background="#E3F2FD"
               BorderBrush="#90CAF9"
               BorderThickness="0,0,0,1">
            <Grid Margin="20,0">
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock Text="看病中 | 患者：" FontSize="14" Foreground="#333" FontWeight="Bold" />
                    <TextBlock Text="{Binding SelectedPatientName}" FontSize="14" Foreground="#2E86AB" FontWeight="Bold" Margin="5,0" />
                    <TextBlock Text="{Binding SelectedPatientInfo}" FontSize="13" Foreground="#666" Margin="20,0,0,0" />
                </StackPanel>
            </Grid>
        </Border>

        <!-- Row 2: 主内容区（Prism Region） -->
        <Border Grid.Row="2" Background="White">
            <ContentControl prism:RegionManager.RegionName="WorkflowContentRegion" />
        </Border>

        <!-- Row 3: 底部操作栏 -->
        <Border Grid.Row="3" Background="White" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid Margin="20,0">
                <!-- 中间：上一步 + 步骤名称 + 下一步 -->
                <StackPanel Orientation="Horizontal"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center">
                    <Button Content="{Binding PreviousButtonText}"
                           Command="{Binding PreviousStepCommand}"
                           Background="#4CAF50"
                           Foreground="White"
                           Padding="20,10"
                           Margin="5,0" />

                    <TextBlock Text="{Binding CurrentStepText}"
                              FontSize="16"
                              FontWeight="Bold"
                              VerticalAlignment="Center"
                              Margin="20,0" />

                    <Button Content="{Binding NextButtonText}"
                           Command="{Binding NextStepCommand}"
                           Background="#4CAF50"
                           Foreground="White"
                           Padding="20,10"
                           Margin="5,0" />
                </StackPanel>

                <!-- 右侧：暂停诊疗按钮 -->
                <Button Content="暂停诊疗"
                       Command="{Binding SaveDraftCommand}"
                       Background="#FF9800"
                       Foreground="White"
                       Padding="20,10"
                       HorizontalAlignment="Right"
                       VerticalAlignment="Center" />
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

**设计原则**：
- ✅ **清晰的视觉分层**：顶部导航 + 患者信息 + 主内容 + 底部操作
- ✅ **Prism Region导航**：`WorkflowContentRegion` 承载子步骤视图
- ✅ **一致的颜色体系**：绿色（前进）、橙色（暂存）、红色（取消）
- ✅ **按钮状态绑定**：Command的CanExecute自动控制Enabled状态

### 5.2 子步骤View实现

**ConsultationFormView.xaml** - 诊疗表单视图示例：

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.ConsultationFormView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="40,20">
            <!-- 标题 -->
            <TextBlock Text="辨证论治" FontSize="20" FontWeight="Bold" Margin="0,0,0,20" />

            <!-- 主诉 -->
            <Grid Margin="0,10">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="100"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="主诉 *" Grid.Column="0" VerticalAlignment="Top" FontSize="14" FontWeight="Bold" Margin="0,10,0,0"/>
                <TextBox Grid.Column="1"
                        Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"
                        TextWrapping="Wrap"
                        Height="80"
                        AcceptsReturn="True"
                        VerticalScrollBarVisibility="Auto"
                        Padding="10"
                        FontSize="14"/>
            </Grid>

            <!-- 现病史 -->
            <Grid Margin="0,10">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="100"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="现病史 *" Grid.Column="0" VerticalAlignment="Top" FontSize="14" FontWeight="Bold" Margin="0,10,0,0"/>
                <TextBox Grid.Column="1"
                        Text="{Binding PresentIllness, UpdateSourceTrigger=PropertyChanged}"
                        TextWrapping="Wrap"
                        Height="120"
                        AcceptsReturn="True"
                        VerticalScrollBarVisibility="Auto"
                        Padding="10"
                        FontSize="14"/>
            </Grid>

            <!-- 既往史 -->
            <Grid Margin="0,10">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="100"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="既往史" Grid.Column="0" VerticalAlignment="Top" FontSize="14" FontWeight="Bold" Margin="0,10,0,0"/>
                <TextBox Grid.Column="1"
                        Text="{Binding PastHistory, UpdateSourceTrigger=PropertyChanged}"
                        TextWrapping="Wrap"
                        Height="80"
                        AcceptsReturn="True"
                        VerticalScrollBarVisibility="Auto"
                        Padding="10"
                        FontSize="14"/>
            </Grid>

            <!-- 诊断 -->
            <Grid Margin="0,10">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="100"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="诊断 *" Grid.Column="0" VerticalAlignment="Top" FontSize="14" FontWeight="Bold" Margin="0,10,0,0"/>
                <TextBox Grid.Column="1"
                        Text="{Binding Diagnosis, UpdateSourceTrigger=PropertyChanged}"
                        TextWrapping="Wrap"
                        Height="60"
                        AcceptsReturn="True"
                        VerticalScrollBarVisibility="Auto"
                        Padding="10"
                        FontSize="14"/>
            </Grid>

            <!-- 治则治法 -->
            <Grid Margin="0,10">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="100"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="治则治法" Grid.Column="0" VerticalAlignment="Top" FontSize="14" FontWeight="Bold" Margin="0,10,0,0"/>
                <TextBox Grid.Column="1"
                        Text="{Binding TreatmentPlan, UpdateSourceTrigger=PropertyChanged}"
                        TextWrapping="Wrap"
                        Height="100"
                        AcceptsReturn="True"
                        VerticalScrollBarVisibility="Auto"
                        Padding="10"
                        FontSize="14"/>
            </Grid>

            <!-- 必填项提示 -->
            <TextBlock Text="* 为必填项" Foreground="Red" FontSize="12" Margin="0,20,0,0"/>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**XAML绑定最佳实践**：
- ✅ **UpdateSourceTrigger=PropertyChanged**：实时同步用户输入到ViewModel
- ✅ **TextWrapping=Wrap**：多行文本自动换行
- ✅ **AcceptsReturn=True**：允许TextBox内换行
- ✅ **ScrollViewer**：整个表单可滚动，适配不同屏幕分辨率
- ✅ **Grid布局**：标签固定宽度100，输入框自适应宽度

---

## 6. Repository数据访问

### 6.1 Repository接口定义

**IMedicalCaseRepository.cs**：

```csharp
using LYBT.Shared.Models;

namespace LYBT.Client.Infrastructure.Repositories
{
    /// <summary>
    /// 病案仓储接口（Client端）
    /// Epic #1494: 医案流程UI重构
    /// </summary>
    public interface IMedicalCaseRepository
    {
        /// <summary>
        /// 创建病案
        /// </summary>
        Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto createDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新病案（部分更新，只更新提供的字段）
        /// </summary>
        Task<MedicalCaseDto> UpdateAsync(MedicalCaseUpdateDto updateDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取病案详情（包含关联的Consultation和Prescription）
        /// </summary>
        Task<MedicalCaseDto> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页查询病案列表
        /// </summary>
        Task<PagedResult<MedicalCaseDto>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            MedicalCaseStatus? status = null,
            Guid? patientId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除病案
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
```

### 6.2 Repository实现（HTTP调用）

**MedicalCaseRepository.cs**：

```csharp
using LYBT.Client.Infrastructure.Http;
using LYBT.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;

namespace LYBT.Client.Infrastructure.Repositories
{
    /// <summary>
    /// 病案仓储实现（HTTP API调用）
    /// Epic #1494: 医案流程UI重构
    /// </summary>
    public class MedicalCaseRepository : BaseRepository, IMedicalCaseRepository
    {
        private const string BaseEndpoint = "/api/v1/medicalcases";

        public MedicalCaseRepository(
            IHttpClientFactory httpClientFactory,
            ILogger<MedicalCaseRepository> logger)
            : base(httpClientFactory, logger, "LybtApi")
        {
        }

        /// <summary>
        /// 创建病案
        /// </summary>
        public async Task<MedicalCaseDto> CreateAsync(
            MedicalCaseCreateDto createDto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("调用API创建病案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    createDto.PatientId, createDto.DoctorId);

                var response = await HttpClient.PostAsJsonAsync(BaseEndpoint, createDto, cancellationToken);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDto>>(cancellationToken: cancellationToken);

                if (apiResponse?.Data == null)
                {
                    throw new InvalidOperationException("API返回的病案数据为null");
                }

                Logger.LogInformation("病案创建成功，MedicalCaseId: {MedicalCaseId}", apiResponse.Data.Id);
                return apiResponse.Data;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建病案失败，PatientId: {PatientId}", createDto.PatientId);
                throw;
            }
        }

        /// <summary>
        /// 更新病案（部分更新）
        /// </summary>
        public async Task<MedicalCaseDto> UpdateAsync(
            MedicalCaseUpdateDto updateDto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("调用API更新病案，MedicalCaseId: {MedicalCaseId}, Status: {Status}",
                    updateDto.Id, updateDto.Status);

                var endpoint = $"{BaseEndpoint}/{updateDto.Id}";
                var response = await HttpClient.PutAsJsonAsync(endpoint, updateDto, cancellationToken);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDto>>(cancellationToken: cancellationToken);

                if (apiResponse?.Data == null)
                {
                    throw new InvalidOperationException("API返回的病案数据为null");
                }

                Logger.LogInformation("病案更新成功，MedicalCaseId: {MedicalCaseId}", apiResponse.Data.Id);
                return apiResponse.Data;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新病案失败，MedicalCaseId: {MedicalCaseId}", updateDto.Id);
                throw;
            }
        }

        /// <summary>
        /// 获取病案详情（包含关联数据）
        /// </summary>
        public async Task<MedicalCaseDto> GetByIdWithDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("调用API获取病案详情，MedicalCaseId: {MedicalCaseId}", id);

                var endpoint = $"{BaseEndpoint}/{id}/details";
                var response = await HttpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDto>>(cancellationToken: cancellationToken);

                if (apiResponse?.Data == null)
                {
                    throw new InvalidOperationException($"API返回的病案数据为null，MedicalCaseId: {id}");
                }

                Logger.LogInformation("病案详情获取成功，包含{ConsultationCount}条诊疗记录，{PrescriptionCount}张处方",
                    apiResponse.Data.Consultation != null ? 1 : 0,
                    apiResponse.Data.Prescription != null ? 1 : 0);

                return apiResponse.Data;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取病案详情失败，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        /// <summary>
        /// 分页查询病案列表
        /// </summary>
        public async Task<PagedResult<MedicalCaseDto>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            MedicalCaseStatus? status = null,
            Guid? patientId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("调用API查询病案列表，PageIndex: {PageIndex}, PageSize: {PageSize}, Status: {Status}, PatientId: {PatientId}",
                    pageIndex, pageSize, status, patientId);

                var queryParams = new List<string>
                {
                    $"pageIndex={pageIndex}",
                    $"pageSize={pageSize}"
                };

                if (status.HasValue)
                {
                    queryParams.Add($"status={status.Value}");
                }

                if (patientId.HasValue && patientId.Value != Guid.Empty)
                {
                    queryParams.Add($"patientId={patientId.Value}");
                }

                var endpoint = $"{BaseEndpoint}?{string.Join("&", queryParams)}";
                var response = await HttpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<MedicalCaseDto>>>(cancellationToken: cancellationToken);

                if (apiResponse?.Data == null)
                {
                    throw new InvalidOperationException("API返回的分页数据为null");
                }

                Logger.LogInformation("病案列表查询成功，返回{TotalCount}条记录", apiResponse.Data.TotalCount);
                return apiResponse.Data;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查询病案列表失败，PageIndex: {PageIndex}, PageSize: {PageSize}", pageIndex, pageSize);
                throw;
            }
        }

        /// <summary>
        /// 删除病案
        /// </summary>
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("调用API删除病案，MedicalCaseId: {MedicalCaseId}", id);

                var endpoint = $"{BaseEndpoint}/{id}";
                var response = await HttpClient.DeleteAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                Logger.LogInformation("病案删除成功，MedicalCaseId: {MedicalCaseId}", id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除病案失败，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }
    }
}
```

**关键点**：
- ✅ **继承BaseRepository**：获取HttpClient和Logger基础设施
- ✅ **IHttpClientFactory**：避免HttpClient资源耗尽
- ✅ **ApiResponse<T>包装**：统一的API响应格式
- ✅ **日志记录**：记录API调用的关键参数和结果
- ✅ **异常处理**：捕获异常记录日志后重新抛出，由ViewModel处理

---

## 7. 数据验证与保存

### 7.1 IValidatable接口模式

**验证时机**：
1. **ExecuteNextStepAsync**：下一步之前验证
2. **SaveDraftCommand**：暂存之前验证（可选，看业务需求）
3. **CompleteMedicalCaseAsync**：完成病案之前验证

**验证流程**：

```csharp
// FlowViewModel中统一的验证调用
if (CurrentStepViewModel is IValidatable validatable)
{
    if (!validatable.Validate())
    {
        await ShowErrorMessageAsync(validatable.ValidationMessage);
        return;
    }
}
```

**子步骤ViewModel实现**（复杂验证示例）：

```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    private ObservableCollection<PrescriptionItemDto> _prescriptionItems = new();
    public ObservableCollection<PrescriptionItemDto> PrescriptionItems
    {
        get => _prescriptionItems;
        set => SetProperty(ref _prescriptionItems, value);
    }

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool Validate()
    {
        ValidationMessage = string.Empty;
        var errors = new List<string>();

        // 1. 验证处方药材列表非空
        if (PrescriptionItems == null || PrescriptionItems.Count == 0)
        {
            errors.Add("处方药材列表不能为空");
        }
        else
        {
            // 2. 验证每个药材项的必填字段
            for (int i = 0; i < PrescriptionItems.Count; i++)
            {
                var item = PrescriptionItems[i];

                if (item.HerbId == Guid.Empty)
                {
                    errors.Add($"第{i + 1}行：未选择药材");
                }

                if (item.Dosage <= 0)
                {
                    errors.Add($"第{i + 1}行（{item.HerbName}）：剂量必须大于0");
                }

                if (item.Dosage > 100)
                {
                    errors.Add($"第{i + 1}行（{item.HerbName}）：单味药剂量不能超过100克");
                }

                if (item.Quantity <= 0)
                {
                    errors.Add($"第{i + 1}行（{item.HerbName}）：剂数必须大于0");
                }

                if (item.Quantity > 100)
                {
                    errors.Add($"第{i + 1}行（{item.HerbName}）：剂数不能超过100");
                }
            }

            // 3. 使用跨端验证器检查重复药材
            var validator = new HerbValidatorBase<PrescriptionItemDto>();
            if (validator.HasDuplicateHerbs(PrescriptionItems.ToList()))
            {
                var duplicates = validator.GetDuplicateHerbs(PrescriptionItems.ToList());
                errors.Add($"发现重复药材：{string.Join("、", duplicates)}");
            }

            // 4. 验证总剂量合理性（使用跨端计算器）
            var calculator = new HerbCalculatorBase<PrescriptionItemDto>();
            var totalDosage = calculator.CalculateTotalDosage(PrescriptionItems.ToList());
            if (totalDosage > 300)
            {
                errors.Add($"处方总剂量过大（{totalDosage:F1}克），单剂总剂量建议不超过300克");
            }
        }

        if (errors.Any())
        {
            ValidationMessage = string.Join("\n", errors);
            Logger.LogWarning("处方验证失败：{ValidationMessage}", ValidationMessage);
            return false;
        }

        Logger.LogInformation("处方验证通过");
        return true;
    }
}
```

### 7.2 ISaveable接口模式

**保存时机**：
1. **ExecuteNextStepAsync**：下一步之前保存
2. **SaveDraftCommand**：暂存时保存
3. **CompleteMedicalCaseAsync**：完成病案之前保存

**保存流程**：

```csharp
// FlowViewModel中统一的保存调用
if (CurrentStepViewModel is ISaveable saveable)
{
    var success = await saveable.SaveAsync();
    if (!success)
    {
        await ShowErrorMessageAsync("保存失败，请检查数据后重试");
        return;
    }
}
```

**复杂保存示例（处方编辑器）**：

```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private Guid _medicalCaseId;
    private Guid _existingPrescriptionId = Guid.Empty;

    public async Task<bool> SaveAsync()
    {
        try
        {
            Logger.LogInformation("开始保存处方，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

            // 1. 验证数据
            if (!Validate())
            {
                Logger.LogWarning("处方验证失败，无法保存");
                return false;
            }

            // 2. 构建PrescriptionDto
            var prescriptionDto = new PrescriptionDto
            {
                MedicalCaseId = _medicalCaseId,
                PrescriptionItems = PrescriptionItems.ToList(),
                TotalPrice = CalculateTotalPrice(),
                PrescriptionDate = DateTime.Now,
                Remark = Remark?.Trim()
            };

            // 3. 保存到Repository
            if (_existingPrescriptionId == Guid.Empty)
            {
                // 新增
                var createdDto = await _prescriptionRepository.CreateAsync(prescriptionDto);
                _existingPrescriptionId = createdDto.Id;
                Logger.LogInformation("处方创建成功，PrescriptionId: {PrescriptionId}, 药材{ItemCount}味",
                    createdDto.Id, PrescriptionItems.Count);
            }
            else
            {
                // 更新
                prescriptionDto.Id = _existingPrescriptionId;
                await _prescriptionRepository.UpdateAsync(prescriptionDto);
                Logger.LogInformation("处方更新成功，PrescriptionId: {PrescriptionId}, 药材{ItemCount}味",
                    _existingPrescriptionId, PrescriptionItems.Count);
            }

            // 4. 发布处方完成事件（通知FlowViewModel自动跳转到Step 3）
            EventAggregator.GetEvent<PrescriptionCompletedEvent>().Publish(new PrescriptionCompletedPayload
            {
                PrescriptionId = _existingPrescriptionId,
                TotalItems = PrescriptionItems.Count,
                TotalAmount = prescriptionDto.TotalPrice
            });

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方失败，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);
            return false;
        }
    }

    private decimal CalculateTotalPrice()
    {
        // 使用跨端计算器计算总价
        var calculator = new HerbCalculatorBase<PrescriptionItemDto>();
        return calculator.CalculateEstimatedTotalPrice(PrescriptionItems.ToList());
    }
}
```

---

## 8. Prism Region导航

### 8.1 Region定义

**在Shell.xaml中定义ContentRegion**：

```xml
<ContentControl prism:RegionManager.RegionName="ContentRegion" Grid.Row="1"/>
```

**在MedicalCaseFlowView.xaml中定义WorkflowContentRegion**：

```xml
<ContentControl prism:RegionManager.RegionName="WorkflowContentRegion" />
```

### 8.2 Region导航调用

**导航到MedicalCaseFlowView**（从患者选择页面）：

```csharp
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;

    private async void ExecuteStartConsultation(PatientDto patient)
    {
        try
        {
            SetIsBusy(true, "正在创建医案...");

            // 1. 创建MedicalCase（调用Repository）
            var medicalCaseId = await CreateMedicalCaseAsync(patient.Id);

            if (medicalCaseId == Guid.Empty)
            {
                await ShowErrorMessageAsync("创建医案失败，请重试");
                return;
            }

            // 2. 导航到MedicalCaseFlowView，传递参数
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", medicalCaseId },
                { "CurrentPatient", patient }
            };

            _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "开始看诊失败，PatientId: {PatientId}", patient.Id);
            await ShowErrorMessageAsync($"开始看诊失败：{ex.Message}");
        }
        finally
        {
            SetIsBusy(false);
        }
    }
}
```

**子步骤Region导航**（FlowViewModel内部）：

```csharp
private void NavigateToStep(ConsultationStep step)
{
    CurrentStep = step;

    switch (step)
    {
        case ConsultationStep.Consultation:
            var consultationParameters = new NavigationParameters
            {
                { "MedicalCaseId", MedicalCaseId },
                { "CurrentPatient", CurrentPatient },
                { "LoadedConsultation", _loadedConsultation } // 继续看诊场景
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "ConsultationFormView", consultationParameters);
            break;

        case ConsultationStep.Prescription:
            var prescriptionParameters = new NavigationParameters
            {
                { "MedicalCaseId", MedicalCaseId },
                { "CurrentPatient", CurrentPatient },
                { "LoadedPrescription", _loadedPrescription } // 继续看诊场景
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "PrescriptionEditorView", prescriptionParameters);
            break;

        case ConsultationStep.Completion:
            var completionParameters = new NavigationParameters
            {
                { "MedicalCaseId", MedicalCaseId }
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "CompletionView", completionParameters);
            break;
    }
}
```

### 8.3 INavigationAware实现

**子步骤ViewModel接收导航参数**：

```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    private Guid _medicalCaseId;
    private PatientDto? _currentPatient;
    private Guid _existingConsultationId = Guid.Empty;

    #region INavigationAware实现

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 1. 接收FlowViewModel传入的参数
        _medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
        _currentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

        Logger.LogInformation("进入诊疗表单，MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
            _medicalCaseId, _currentPatient?.Name);

        // 2. 如果是继续看诊，加载已有的Consultation数据
        var loadedConsultation = navigationContext.Parameters.GetValue<ConsultationDto>("LoadedConsultation");
        if (loadedConsultation != null)
        {
            LoadConsultationData(loadedConsultation);
        }
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 允许重复导航（每次新建医案或继续看诊都是新的导航）
        return false;
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        base.OnNavigatedFrom(navigationContext);
        Logger.LogInformation("离开诊疗表单，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);
    }

    #endregion

    private void LoadConsultationData(ConsultationDto consultation)
    {
        _existingConsultationId = consultation.Id;
        ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
        PresentIllness = consultation.PresentIllness ?? string.Empty;
        PastHistory = consultation.PastHistory ?? string.Empty;
        Diagnosis = consultation.Diagnosis ?? string.Empty;
        TreatmentPlan = consultation.TreatmentPlan ?? string.Empty;

        Logger.LogInformation("加载已有诊疗记录，ConsultationId: {ConsultationId}", consultation.Id);
    }
}
```

**关键点**：
- ✅ **OnNavigatedTo**：接收导航参数，初始化ViewModel状态
- ✅ **IsNavigationTarget**：返回false允许重复导航（每次都创建新实例）
- ✅ **OnNavigatedFrom**：清理资源、保存临时状态（如需要）
- ✅ **NavigationParameters**：类型安全的参数传递机制

---

## 9. 事件总线通信

### 9.1 PubSubEvent定义

**PrescriptionCompletedEvent.cs**：

```csharp
using Prism.Events;

namespace LYBT.Desktop.Contracts.Events
{
    /// <summary>
    /// 处方完成事件
    /// Issue #1557 Phase 4: PrescriptionEditorViewModel发布 → MedicalCaseFlowViewModel订阅
    /// </summary>
    public class PrescriptionCompletedEvent : PubSubEvent<PrescriptionCompletedPayload>
    {
    }

    /// <summary>
    /// 处方完成事件载荷
    /// </summary>
    public class PrescriptionCompletedPayload
    {
        /// <summary>
        /// 处方ID
        /// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 药材总数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}
```

### 9.2 发布事件

**在PrescriptionEditorViewModel中发布**：

```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    public async Task<bool> SaveAsync()
    {
        try
        {
            // ... 保存处方数据 ...

            // 发布处方完成事件
            EventAggregator.GetEvent<PrescriptionCompletedEvent>().Publish(new PrescriptionCompletedPayload
            {
                PrescriptionId = _existingPrescriptionId,
                TotalItems = PrescriptionItems.Count,
                TotalAmount = CalculateTotalPrice()
            });

            Logger.LogInformation("发布PrescriptionCompletedEvent，PrescriptionId: {PrescriptionId}", _existingPrescriptionId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方失败");
            return false;
        }
    }
}
```

### 9.3 订阅事件

**在MedicalCaseFlowViewModel中订阅**：

```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    public MedicalCaseFlowViewModel(
        IMedicalCaseRepository medicalCaseRepository,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        ISessionManager? sessionManager = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
    {
        // ...

        // Issue #1557 Phase 4: 订阅处方完成事件
        EventAggregator.GetEvent<PrescriptionCompletedEvent>()
            .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
    }

    /// <summary>
    /// 处方完成事件处理方法
    /// Issue #1567 - 自动跳转到Step 3（完成病案）
    /// </summary>
    private async void OnPrescriptionCompleted(PrescriptionCompletedPayload payload)
    {
        try
        {
            Logger.LogInformation("接收到PrescriptionCompletedEvent，PrescriptionId: {PrescriptionId}, 药材总数: {TotalItems}, 总金额: {TotalAmount:F2}",
                payload.PrescriptionId, payload.TotalItems, payload.TotalAmount);

            // 自动触发下一步：跳转到Step 3（完成病案）
            await ExecuteNextStepAsync();

            Logger.LogInformation("处方完成事件处理完成，准备跳转到Step 3");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理PrescriptionCompletedEvent失败");
            await ShowErrorMessageAsync($"处理处方完成失败：{ex.Message}");
        }
    }
}
```

**事件订阅选项**：
- `ThreadOption.UIThread`：在UI线程执行（推荐，避免跨线程访问UI）
- `ThreadOption.BackgroundThread`：在后台线程执行
- `ThreadOption.PublisherThread`：在发布者线程执行

**事件取消订阅**：

```csharp
private SubscriptionToken? _prescriptionCompletedToken;

public MedicalCaseFlowViewModel(...)
{
    _prescriptionCompletedToken = EventAggregator.GetEvent<PrescriptionCompletedEvent>()
        .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
}

protected override void OnDispose()
{
    // 取消订阅（避免内存泄漏）
    if (_prescriptionCompletedToken != null)
    {
        EventAggregator.GetEvent<PrescriptionCompletedEvent>().Unsubscribe(_prescriptionCompletedToken);
        _prescriptionCompletedToken = null;
    }

    base.OnDispose();
}
```

---

## 10. 跨端组件复用

### 10.1 IHerbItem接口实现

**PrescriptionItemDto实现**（已在Shared.Models中定义）：

```csharp
using LYBT.Shared.Components;

namespace LYBT.Shared.Models
{
    public class PrescriptionItemDto : IHerbItem
    {
        public Guid Id { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public string Unit { get; set; } = "g";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
```

### 10.2 HerbCalculatorBase使用

**在PrescriptionEditorViewModel中使用**：

```csharp
using LYBT.Shared.Components;

public class PrescriptionEditorViewModel : UnifiedViewModelBase
{
    private readonly HerbCalculatorBase<PrescriptionItemDto> _calculator = new();

    private decimal CalculateTotalPrice()
    {
        // 计算估算总价（单剂总价 × 剂数）
        return _calculator.CalculateEstimatedTotalPrice(PrescriptionItems.ToList());
    }

    private decimal CalculateTotalDosage()
    {
        // 计算单剂总剂量（所有药材剂量之和）
        return _calculator.CalculateTotalDosage(PrescriptionItems.ToList());
    }

    private decimal CalculateTotalWeight()
    {
        // 计算总重量（单剂总剂量 × 剂数）
        return _calculator.CalculateTotalWeight(PrescriptionItems.ToList());
    }

    private double CalculateDosageStandardDeviation()
    {
        // 计算剂量标准差（判断剂量均衡性）
        return _calculator.CalculateStandardDeviation(PrescriptionItems.ToList());
    }

    /// <summary>
    /// 实时更新处方统计信息
    /// </summary>
    private void UpdatePrescriptionSummary()
    {
        TotalPrice = CalculateTotalPrice();
        TotalDosage = CalculateTotalDosage();
        TotalWeight = CalculateTotalWeight();
        DosageStandardDeviation = CalculateDosageStandardDeviation();

        // 剂量均衡性提示
        if (DosageStandardDeviation > 15)
        {
            DosageWarning = "剂量差异较大，建议检查配伍合理性";
        }
        else
        {
            DosageWarning = string.Empty;
        }
    }
}
```

### 10.3 HerbValidatorBase使用

**在PrescriptionEditorViewModel验证中使用**：

```csharp
using LYBT.Shared.Components;

public class PrescriptionEditorViewModel : UnifiedViewModelBase, IValidatable
{
    private readonly HerbValidatorBase<PrescriptionItemDto> _validator = new();

    public bool Validate()
    {
        ValidationMessage = string.Empty;
        var errors = new List<string>();

        // 1. 验证药材列表非空
        var emptyResult = _validator.ValidateHerbListNotEmpty(PrescriptionItems.ToList());
        if (!emptyResult.IsValid)
        {
            errors.Add(emptyResult.ErrorMessage);
            ValidationMessage = string.Join("\n", errors);
            return false;
        }

        // 2. 检查重复药材
        if (_validator.HasDuplicateHerbs(PrescriptionItems.ToList()))
        {
            var duplicates = _validator.GetDuplicateHerbs(PrescriptionItems.ToList());
            errors.Add($"发现重复药材：{string.Join("、", duplicates)}");
        }

        // 3. 验证每个药材项的必填字段
        var requiredFieldsResult = _validator.ValidateRequiredFields(PrescriptionItems.ToList());
        if (!requiredFieldsResult.IsValid)
        {
            errors.Add(requiredFieldsResult.ErrorMessage);
        }

        // 4. 验证剂量合理性
        foreach (var item in PrescriptionItems)
        {
            if (!_validator.IsValidDosage(item.Dosage))
            {
                var warning = _validator.GetDosageWarning(item.Dosage);
                errors.Add($"{item.HerbName}：{warning}");
            }
        }

        // 5. 综合验证
        var comprehensiveResult = _validator.ValidateHerbList(PrescriptionItems.ToList());
        if (!comprehensiveResult.IsValid)
        {
            errors.Add(comprehensiveResult.ErrorMessage);
        }

        if (errors.Any())
        {
            ValidationMessage = string.Join("\n", errors);
            Logger.LogWarning("处方验证失败：{ValidationMessage}", ValidationMessage);
            return false;
        }

        return true;
    }
}
```

**跨端组件复用优势**：
- ✅ **一致的业务逻辑**：Client和Server使用相同的计算和验证规则
- ✅ **减少重复代码**：避免在Client和Server分别实现相同逻辑
- ✅ **类型安全**：泛型约束确保编译期类型检查
- ✅ **易于测试**：纯逻辑组件，无UI依赖，便于单元测试

---

## 11. 错误处理与日志

### 11.1 日志记录规范

**日志级别使用**：

```csharp
// Information - 正常业务流程关键节点
Logger.LogInformation("进入诊疗表单，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

// Warning - 非致命错误，业务可以继续
Logger.LogWarning("诊疗记录验证失败：{ValidationMessage}", ValidationMessage);

// Error - 致命错误，业务无法继续
Logger.LogError(ex, "保存处方失败，MedicalCaseId: {MedicalCaseId}", _medicalCaseId);

// Critical - 系统级错误（极少使用）
Logger.LogCritical(ex, "数据库连接失败，系统无法使用");
```

**结构化日志**（使用占位符）：

```csharp
// ✅ 正确 - 使用占位符，支持结构化日志查询
Logger.LogInformation("病案创建成功，MedicalCaseId: {MedicalCaseId}, PatientId: {PatientId}",
    medicalCaseId, patientId);

// ❌ 错误 - 字符串拼接，无法结构化查询
Logger.LogInformation($"病案创建成功，MedicalCaseId: {medicalCaseId}, PatientId: {patientId}");
```

### 11.2 异常处理模式

**Repository层异常处理**：

```csharp
public async Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto createDto, CancellationToken cancellationToken = default)
{
    try
    {
        Logger.LogInformation("调用API创建病案，PatientId: {PatientId}", createDto.PatientId);

        var response = await HttpClient.PostAsJsonAsync(BaseEndpoint, createDto, cancellationToken);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDto>>(cancellationToken: cancellationToken);

        if (apiResponse?.Data == null)
        {
            throw new InvalidOperationException("API返回的病案数据为null");
        }

        return apiResponse.Data;
    }
    catch (HttpRequestException ex)
    {
        // HTTP请求异常（网络错误、连接超时等）
        Logger.LogError(ex, "创建病案HTTP请求失败，PatientId: {PatientId}, StatusCode: {StatusCode}",
            createDto.PatientId, ex.StatusCode);
        throw new InvalidOperationException($"网络请求失败：{ex.Message}", ex);
    }
    catch (Exception ex)
    {
        // 其他异常（JSON反序列化失败、业务逻辑错误等）
        Logger.LogError(ex, "创建病案失败，PatientId: {PatientId}", createDto.PatientId);
        throw;
    }
}
```

**ViewModel层异常处理**：

```csharp
private async Task ExecuteNextStepAsync()
{
    try
    {
        SetIsBusy(true, "正在处理...");

        // 业务逻辑
        // ...
    }
    catch (InvalidOperationException ex)
    {
        // 业务逻辑异常（如数据验证失败、状态不一致等）
        Logger.LogWarning(ex, "业务操作失败：{Message}", ex.Message);
        await ShowErrorMessageAsync($"操作失败：{ex.Message}");
    }
    catch (Exception ex)
    {
        // 其他未预期异常
        Logger.LogError(ex, "执行下一步时发生未预期异常");
        await ShowErrorMessageAsync("系统错误，请联系管理员");
    }
    finally
    {
        // ⚠️ 必须在finally中重置IsBusy，确保异常时也能恢复
        SetIsBusy(false);
    }
}
```

**用户友好的错误提示**：

```csharp
// ❌ 错误 - 技术细节暴露给用户
await ShowErrorMessageAsync(ex.ToString());

// ✅ 正确 - 用户友好的提示
await ShowErrorMessageAsync("保存失败，请检查网络连接后重试");

// ✅ 开发环境可以显示更多细节
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    await ShowErrorMessageAsync($"保存失败：{ex.Message}\n\n堆栈：{ex.StackTrace}");
}
else
{
    await ShowErrorMessageAsync("保存失败，请稍后重试");
}
```

---

## 12. 用户交互反馈

### 12.1 IsBusy加载状态

**设置加载状态**：

```csharp
private async Task ExecuteNextStepAsync()
{
    try
    {
        // 1. 设置IsBusy=true，显示加载指示器
        SetIsBusy(true, "正在保存数据...");

        // 2. 执行长时间操作
        await Task.Delay(2000); // 模拟网络请求
        await SaveDataAsync();

        // 3. 操作成功，显示成功提示
        await ShowSuccessMessageAsync("保存成功");
    }
    catch (Exception ex)
    {
        // 4. 操作失败，显示错误提示
        Logger.LogError(ex, "保存失败");
        await ShowErrorMessageAsync($"保存失败：{ex.Message}");
    }
    finally
    {
        // 5. 重置IsBusy=false，隐藏加载指示器
        SetIsBusy(false);
    }
}
```

**XAML中绑定IsBusy**（在Shell或FlowView中）：

```xml
<!-- 全屏加载遮罩 -->
<Border Background="#80000000"
       Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibility}}"
       Panel.ZIndex="999">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
        <!-- 加载动画 -->
        <ProgressBar IsIndeterminate="True" Width="200" Height="4" />

        <!-- 加载文本 -->
        <TextBlock Text="{Binding BusyMessage}"
                  Foreground="White"
                  FontSize="14"
                  Margin="0,10,0,0"
                  HorizontalAlignment="Center"/>
    </StackPanel>
</Border>
```

### 12.2 UserNotificationService通知

**成功提示**：

```csharp
await ShowSuccessMessageAsync("病案已完成");
```

**错误提示**：

```csharp
await ShowErrorMessageAsync("保存失败，请检查数据");
```

**确认对话框**：

```csharp
var confirmed = await ShowConfirmationAsync(
    "确定要取消本次医案吗？未保存的数据将丢失！",
    "取消医案");

if (confirmed)
{
    // 用户点击确认
    await CancelMedicalCaseAsync();
}
else
{
    // 用户点击取消
    Logger.LogInformation("用户取消了取消操作");
}
```

**信息提示**：

```csharp
await ShowInformationAsync("病案已自动保存为草稿");
```

### 12.3 命令状态反馈

**按钮禁用/启用**：

```csharp
// 命令定义时监听属性变化
NextStepCommand = new DelegateCommand(
    async () => await ExecuteNextStepAsync(),
    CanExecuteNextStep)
    .ObservesProperty(() => CurrentPatient)  // 监听属性变化
    .ObservesProperty(() => IsBusy);         // IsBusy变化时自动刷新

private bool CanExecuteNextStep()
{
    // IsBusy=true时禁用按钮
    if (IsBusy)
    {
        return false;
    }

    // CurrentPatient为null时禁用按钮
    if (CurrentPatient == null)
    {
        return false;
    }

    return true;
}
```

**手动刷新命令状态**：

```csharp
private void OnPropertyChanged(string propertyName)
{
    if (propertyName == nameof(CurrentStep))
    {
        // 步骤变更时，手动刷新所有命令的CanExecute
        PreviousStepCommand.RaiseCanExecuteChanged();
        NextStepCommand.RaiseCanExecuteChanged();
    }
}
```

---

## 13. 常见问题与陷阱

### 问题1：Region导航失败，视图未显示

**❌ 错误原因**：
```csharp
// 忘记在Module中注册Region视图
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ❌ 缺少Region视图注册
    }
}
```

**✅ 正确做法**：
```csharp
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ✅ 注册Region视图
        containerRegistry.RegisterForNavigation<ConsultationFormView>();
        containerRegistry.RegisterForNavigation<PrescriptionEditorView>();
        containerRegistry.RegisterForNavigation<CompletionView>();
        containerRegistry.RegisterForNavigation<MedicalCaseFlowView>();
    }
}
```

---

### 问题2：NavigationParameters参数丢失

**❌ 错误原因**：
```csharp
// 使用错误的参数键名
var parameters = new NavigationParameters
{
    { "medicalCaseId", MedicalCaseId } // ❌ 键名不一致
};

// 接收时使用了不同的键名
_medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId"); // ❌ 键名不匹配
```

**✅ 正确做法**：
```csharp
// 发送方：使用统一的键名（建议定义常量）
public static class NavigationParameterKeys
{
    public const string MedicalCaseId = "MedicalCaseId";
    public const string CurrentPatient = "CurrentPatient";
    public const string LoadedConsultation = "LoadedConsultation";
}

var parameters = new NavigationParameters
{
    { NavigationParameterKeys.MedicalCaseId, MedicalCaseId }
};

// 接收方：使用相同的键名
_medicalCaseId = navigationContext.Parameters.GetValue<Guid>(NavigationParameterKeys.MedicalCaseId);
```

---

### 问题3：SetProperty未触发PropertyChanged

**❌ 错误原因**：
```csharp
// 直接修改私有字段，未调用SetProperty
_currentStep = ConsultationStep.Prescription; // ❌ UI不会更新
```

**✅ 正确做法**：
```csharp
// 使用SetProperty方法，自动触发PropertyChanged
public ConsultationStep CurrentStep
{
    get => _currentStep;
    set => SetProperty(ref _currentStep, value); // ✅ 自动通知UI更新
}

// 或者在方法中直接赋值给属性（不是字段）
CurrentStep = ConsultationStep.Prescription; // ✅ 调用了属性的set，触发SetProperty
```

---

### 问题4：异步命令未正确处理

**❌ 错误原因**：
```csharp
// 直接传递异步方法，导致await被忽略
NextStepCommand = new DelegateCommand(ExecuteNextStepAsync); // ❌ 编译错误

// 或者使用async void（不推荐）
NextStepCommand = new DelegateCommand(async () => ExecuteNextStepAsync()); // ❌ await被忽略
```

**✅ 正确做法**：
```csharp
// 使用async () => await包装异步方法
NextStepCommand = new DelegateCommand(async () => await ExecuteNextStepAsync(), CanExecuteNextStep);
```

---

### 问题5：IsBusy未在finally中重置

**❌ 错误原因**：
```csharp
private async Task ExecuteNextStepAsync()
{
    SetIsBusy(true, "正在处理...");

    await SaveDataAsync(); // ❌ 如果这里抛出异常，IsBusy永远是true

    SetIsBusy(false); // ❌ 异常时不会执行
}
```

**✅ 正确做法**：
```csharp
private async Task ExecuteNextStepAsync()
{
    try
    {
        SetIsBusy(true, "正在处理...");
        await SaveDataAsync();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "保存失败");
        await ShowErrorMessageAsync($"保存失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false); // ✅ 无论成功还是异常，都会重置IsBusy
    }
}
```

---

### 问题6：ViewModel未实现INavigationAware导致数据不刷新

**❌ 错误原因**：
```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    // ❌ 未实现OnNavigatedTo，无法接收导航参数
}
```

**✅ 正确做法**：
```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // ✅ 接收导航参数，初始化ViewModel
        _medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
        LoadData();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // ✅ 返回false允许每次导航都创建新实例
        return false;
    }
}
```

---

### 问题7：事件订阅未取消导致内存泄漏

**❌ 错误原因**：
```csharp
public MedicalCaseFlowViewModel(...)
{
    // 订阅事件
    EventAggregator.GetEvent<PrescriptionCompletedEvent>()
        .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);

    // ❌ 未保存SubscriptionToken，无法取消订阅
}
```

**✅ 正确做法**：
```csharp
private SubscriptionToken? _prescriptionCompletedToken;

public MedicalCaseFlowViewModel(...)
{
    // 保存SubscriptionToken
    _prescriptionCompletedToken = EventAggregator.GetEvent<PrescriptionCompletedEvent>()
        .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
}

protected override void OnDispose()
{
    // ✅ 取消订阅，避免内存泄漏
    if (_prescriptionCompletedToken != null)
    {
        EventAggregator.GetEvent<PrescriptionCompletedEvent>().Unsubscribe(_prescriptionCompletedToken);
        _prescriptionCompletedToken = null;
    }

    base.OnDispose();
}
```

---

### 问题8：Repository异常未正确处理

**❌ 错误原因**：
```csharp
public async Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto createDto)
{
    try
    {
        var response = await HttpClient.PostAsJsonAsync(BaseEndpoint, createDto);
        return await response.Content.ReadFromJsonAsync<MedicalCaseDto>(); // ❌ 未检查HTTP状态码
    }
    catch (Exception)
    {
        return null; // ❌ 吞掉异常，调用方无法感知错误
    }
}
```

**✅ 正确做法**：
```csharp
public async Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto createDto)
{
    try
    {
        var response = await HttpClient.PostAsJsonAsync(BaseEndpoint, createDto);
        response.EnsureSuccessStatusCode(); // ✅ 检查HTTP状态码，失败抛出异常

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDto>>();

        if (apiResponse?.Data == null)
        {
            throw new InvalidOperationException("API返回的数据为null"); // ✅ 验证响应数据
        }

        return apiResponse.Data;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "创建病案失败，PatientId: {PatientId}", createDto.PatientId);
        throw; // ✅ 重新抛出异常，让调用方处理
    }
}
```

---

## 14. 检查清单

### 14.1 开发阶段检查清单

**ViewModel开发**：
- [ ] 继承自 `UnifiedViewModelBase`
- [ ] 构造函数依赖注入（IRepository、IEventAggregator、ILoggerFactory等）
- [ ] 属性使用 `SetProperty` 方法绑定
- [ ] 计算属性使用 `=>` 表达式体
- [ ] 命令初始化（DelegateCommand）
- [ ] 异步命令使用 `async () => await` 包装
- [ ] 命令CanExecute逻辑正确
- [ ] 实现 `IValidatable` 接口（如需验证）
- [ ] 实现 `ISaveable` 接口（如需保存）
- [ ] 实现 `INavigationAware` 接口（如需接收导航参数）
- [ ] 事件订阅保存 `SubscriptionToken`
- [ ] `OnDispose` 中取消事件订阅

**XAML视图开发**：
- [ ] `prism:ViewModelLocator.AutoWireViewModel="True"` 自动关联ViewModel
- [ ] 使用 `{Binding PropertyName}` 绑定属性
- [ ] 使用 `{Binding Command}` 绑定命令
- [ ] TextBox使用 `UpdateSourceTrigger=PropertyChanged` 实时更新
- [ ] Region使用 `prism:RegionManager.RegionName` 定义
- [ ] 样式定义合理（颜色、字体、间距）
- [ ] 响应式布局（Grid、StackPanel、ScrollViewer）

**Repository开发**：
- [ ] 继承自 `BaseRepository`
- [ ] 接口定义在 `LYBT.Client.Infrastructure.Repositories`
- [ ] 实现定义在同一命名空间
- [ ] 所有方法使用 `async/await`
- [ ] 使用 `EnsureSuccessStatusCode()` 检查HTTP状态
- [ ] 使用 `ApiResponse<T>` 包装解析响应
- [ ] 记录详细日志（Information、Error）
- [ ] 异常重新抛出（不吞掉异常）

**数据验证**：
- [ ] `Validate()` 方法返回true/false
- [ ] `ValidationMessage` 属性存储错误消息
- [ ] 验证失败记录Warning日志
- [ ] 使用跨端验证器（HerbValidatorBase）
- [ ] 验证逻辑清晰（必填项、格式、范围、重复等）

**数据保存**：
- [ ] `SaveAsync()` 方法返回true/false
- [ ] 保存前调用 `Validate()` 验证
- [ ] 区分新增（CreateAsync）和更新（UpdateAsync）
- [ ] 记录详细日志（Information、Error）
- [ ] 异常返回false（不抛出异常）

### 14.2 测试阶段检查清单

**功能测试**：
- [ ] 新建病案流程完整（患者选择 → 辨证 → 施治 → 完成）
- [ ] 继续看诊流程完整（加载已有数据 → 修改 → 保存）
- [ ] 暂存病案功能正常（保存数据 + 更新状态 + 停留当前界面）
- [ ] 取消病案功能正常（确认对话框 + 更新状态 + 返回患者选择）
- [ ] 完成病案功能正常（验证 + 保存 + 更新状态 + 返回患者选择）
- [ ] 上一步/下一步导航正常
- [ ] Region导航正常（子步骤视图切换）
- [ ] 数据验证正常（必填项、格式、重复等）
- [ ] 数据保存正常（新增、更新）
- [ ] 跨端组件正常（计算器、验证器）

**UI交互测试**：
- [ ] 按钮状态正确（Enabled/Disabled）
- [ ] 加载指示器正常显示（IsBusy）
- [ ] 成功提示正常显示
- [ ] 错误提示正常显示
- [ ] 确认对话框正常显示
- [ ] 患者信息条正常显示
- [ ] 当前步骤名称正常显示
- [ ] 下一步按钮文字正确（"下一步"/"完成病案"）

**异常处理测试**：
- [ ] 网络异常处理正常（超时、断网）
- [ ] API错误处理正常（400、500错误）
- [ ] 数据验证失败提示正常
- [ ] IsBusy异常时正确重置
- [ ] 日志记录完整（Information、Warning、Error）

### 14.3 代码审查检查清单

**代码质量**：
- [ ] 命名规范（PascalCase、_camelCase）
- [ ] 无编译警告（0 warnings）
- [ ] 无未使用的using
- [ ] 无未使用的变量
- [ ] 无重复代码（遵循DRY原则）
- [ ] 方法长度合理（<50行）
- [ ] 类长度合理（<500行）

**架构规范**：
- [ ] 遵循MVVM模式（View ↔ ViewModel分离）
- [ ] 遵循依赖注入原则（构造函数注入）
- [ ] 遵循Repository模式（数据访问隔离）
- [ ] 遵循单一职责原则（类职责单一）
- [ ] 遵循接口隔离原则（IValidatable、ISaveable）

**性能优化**：
- [ ] 避免UI线程阻塞（使用async/await）
- [ ] 避免内存泄漏（取消事件订阅）
- [ ] 避免不必要的属性通知（使用SetProperty判断值是否变更）
- [ ] 避免频繁的Repository调用（缓存数据）

---

## 15. 参考资料

### 15.1 内部文档

**架构文档**：
- [Client端MVVM架构设计](../../architecture/client/README.md) - Client端五层架构总览
- [病案管理架构设计](../../architecture/client/medical-case-design.md) - MedicalCase模块架构详解
- [Foundation设计文档](../../architecture/client/foundation-design.md) - UnifiedViewModelBase基类设计

**相关开发指南**：
- [Models层使用指南](../client/models-usage.md) - DTO模型使用
- [Infrastructure使用指南](../client/infrastructure-usage.md) - Repository模式
- [Foundation开发指南](../client/foundation-development.md) - UnifiedViewModelBase详解

**API文档**：
- [MedicalCase API参考](../../api/medical-case-api.md) - Server端API接口定义

### 15.2 外部资源

**Prism官方文档**：
- [Prism Library](https://prismlibrary.com/) - Prism框架官网
- [Region Navigation](https://prismlibrary.com/docs/wpf/navigation/navigation-basics.html) - Region导航详解
- [Event Aggregator](https://prismlibrary.com/docs/event-aggregator.html) - 事件总线详解
- [INavigationAware](https://prismlibrary.com/docs/wpf/navigation/navigation-awareness.html) - 导航感知接口

**WPF官方文档**：
- [Data Binding](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/) - WPF数据绑定
- [Commanding](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/commanding-overview) - WPF命令模式
- [MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm) - MVVM模式详解

**.NET官方文档**：
- [Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) - 依赖注入
- [Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) - 日志框架
- [HttpClient Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) - HttpClient最佳实践

---

**文档结束**

如有问题或建议，请联系：
- **技术支持**：LYBT开发团队
- **文档维护**：Client端开发组
