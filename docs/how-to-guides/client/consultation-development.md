# Client端诊疗管理开发指南

> **文档版本**: v1.0
> **最后更新**: 2025-01-30
> **目标读者**: Client端开发者
> **前置阅读**: [Client端诊疗管理架构设计](../../explanation/architecture/client/consultation-design.md)

---

## 📋 目录

1. [快速开始](#1-快速开始)
2. [核心接口实现](#2-核心接口实现)
3. [中医四诊合参开发](#3-中医四诊合参开发)
4. [三步工作流集成](#4-三步工作流集成)
5. [ViewModel开发模式](#5-viewmodel开发模式)
6. [Repository集成](#6-repository集成)
7. [UI开发与数据绑定](#7-ui开发与数据绑定)
8. [暂存与继续功能](#8-暂存与继续功能)
9. [验证与错误处理](#9-验证与错误处理)
10. [最佳实践](#10-最佳实践)
11. [常见问题](#11-常见问题)
12. [调试技巧](#12-调试技巧)

---

## 1. 快速开始

### 1.1 环境准备

**依赖项目**：
```xml
<!-- LYBT.Desktop.Consultation.csproj -->
<ItemGroup>
  <!-- 核心依赖 -->
  <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  <ProjectReference Include="..\..\Core\LYBT.Desktop.Core\LYBT.Desktop.Core.csproj" />
  <ProjectReference Include="..\..\Foundation\LYBT.Desktop.Foundation\LYBT.Desktop.Foundation.csproj" />
  <ProjectReference Include="..\..\Presentation\LYBT.Desktop.Presentation\LYBT.Desktop.Presentation.csproj" />

  <!-- 契约层（接口定义） -->
  <ProjectReference Include="..\..\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />

  <!-- 模块依赖 -->
  <ProjectReference Include="..\LYBT.Desktop.MedicalCase\LYBT.Desktop.MedicalCase.csproj" />
</ItemGroup>

<ItemGroup>
  <!-- Prism框架 -->
  <PackageReference Include="Prism.Wpf" Version="9.0.x" />

  <!-- Material Design -->
  <PackageReference Include="MaterialDesignThemes" Version="5.1.x" />
  <PackageReference Include="MaterialDesignColors" Version="3.1.x" />
</ItemGroup>
```

**命名空间引用**：
```csharp
using LYBT.Desktop.Contracts.Services;        // ISaveable, IValidatable
using LYBT.Desktop.Core;                      // UnifiedViewModelBase
using LYBT.Desktop.Foundation.Commands;       // AsyncDelegateCommand
using LYBT.Shared.Models.MedicalCases;        // ConsultationDto, MedicalCaseDto
using LYBT.Shared.Models.Patients;            // PatientDto
using Prism.Regions;                          // INavigationAware
```

### 1.2 创建诊疗表单的基本步骤

**Step 1: 创建ViewModel类**
```csharp
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 诊疗表单ViewModel（三步工作流 - Step1）
    /// </summary>
    public class ConsultationFormViewModel : UnifiedViewModelBase,
                                             IValidatable,
                                             ISaveable
    {
        #region 依赖注入

        private readonly IMedicalCaseRepository _medicalCaseRepository;

        public ConsultationFormViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            ILogger<ConsultationFormViewModel> logger)
            : base(logger)
        {
            _medicalCaseRepository = medicalCaseRepository ??
                throw new ArgumentNullException(nameof(medicalCaseRepository));

            // 初始化命令
            InitializeCommands();
        }

        #endregion
    }
}
```

**Step 2: 创建XAML视图**
```xml
<UserControl x:Class="LYBT.Desktop.Consultation.Views.ConsultationFormView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <!-- 诊疗表单内容 -->
    </Grid>
</UserControl>
```

**Step 3: 注册到模块**
```csharp
public class ConsultationModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModel
        containerRegistry.Register<ConsultationFormViewModel>();

        // 注册导航
        containerRegistry.RegisterForNavigation<ConsultationFormView>();
    }
}
```

### 1.3 基本使用示例

**在MedicalCaseFlowViewModel中加载诊疗表单**：
```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;

    public void NavigateToConsultation()
    {
        var parameters = new NavigationParameters
        {
            { "CurrentPatient", this.CurrentPatient },
            { "MedicalCaseId", this.MedicalCaseId }
        };

        _regionManager.RequestNavigate(
            "Step1ContentRegion",
            "ConsultationFormView",
            parameters);
    }
}
```

---

## 2. 核心接口实现

### 2.1 IValidatable接口（验证功能）

**接口定义** (`Desktop.Contracts`):
```csharp
public interface IValidatable
{
    /// <summary>
    /// 验证当前步骤数据
    /// </summary>
    /// <returns>验证是否通过</returns>
    bool Validate();

    /// <summary>
    /// 验证错误消息
    /// </summary>
    string ValidationMessage { get; }
}
```

**实现示例**：
```csharp
#region IValidatable实现

private string _validationMessage = string.Empty;

/// <summary>
/// 验证错误消息
/// </summary>
public string ValidationMessage
{
    get => _validationMessage;
    private set => SetProperty(ref _validationMessage, value);
}

/// <summary>
/// 验证当前步骤数据（主诉、中医诊断必填）
/// 业务规则: BF-002 诊断必填项验证
/// </summary>
public bool Validate()
{
    var errors = new List<string>();

    // 必填项1: 主诉
    if (string.IsNullOrWhiteSpace(ChiefComplaint))
    {
        errors.Add("主诉不能为空");
    }

    // 必填项2: 中医诊断
    if (string.IsNullOrWhiteSpace(TCMDiagnosis))
    {
        errors.Add("中医诊断不能为空");
    }

    // 构建错误消息
    if (errors.Any())
    {
        ValidationMessage = string.Join("；", errors);
        Logger.LogWarning("诊断表单验证失败：{ValidationMessage}", ValidationMessage);
        return false;
    }

    // 验证通过
    ValidationMessage = string.Empty;
    Logger.LogInformation("诊断表单验证通过");
    return true;
}

#endregion
```

**使用场景**：
```csharp
// 场景1: CompleteStep1Command执行前验证
private bool CanCompleteStep1()
{
    // 必须有患者信息 + 验证通过
    return CurrentPatient != null && Validate();
}

// 场景2: 外部调用验证
if (!consultationViewModel.Validate())
{
    await _messageService.ShowErrorAsync(
        "验证失败",
        consultationViewModel.ValidationMessage);
    return;
}
```

### 2.2 ISaveable接口（保存功能）

**接口定义** (`Desktop.Contracts`):
```csharp
public interface ISaveable
{
    /// <summary>
    /// 保存当前步骤数据
    /// </summary>
    Task SaveAsync();
}
```

**实现示例**：
```csharp
#region ISaveable实现

/// <summary>
/// 保存诊断数据（暂存或自动保存）
/// </summary>
public async Task SaveAsync()
{
    try
    {
        IsBusy = true;

        // 验证基本数据
        if (MedicalCaseId == Guid.Empty)
        {
            Logger.LogWarning("病案ID为空，无法保存诊断数据");
            return;
        }

        // 构建更新DTO
        var updateDto = BuildConsultationUpdateDto();

        // 调用Repository保存（通过聚合根）
        var result = await _medicalCaseRepository
            .UpdateConsultationAsync(MedicalCaseId, updateDto);

        if (result != null)
        {
            Logger.LogInformation("诊断数据保存成功，病案ID: {MedicalCaseId}", MedicalCaseId);

            // 发布保存完成消息（可选）
            await PublishMessage(new ConsultationSavedMessage(MedicalCaseId));
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "保存诊断数据失败，病案ID: {MedicalCaseId}", MedicalCaseId);
        throw;
    }
    finally
    {
        IsBusy = false;
    }
}

/// <summary>
/// 构建更新DTO
/// </summary>
private ConsultationUpdateDto BuildConsultationUpdateDto()
{
    return new ConsultationUpdateDto
    {
        // 基本诊断信息
        ChiefComplaint = ChiefComplaint?.Trim(),
        PresentIllness = PresentIllness?.Trim(),
        TCMDiagnosis = TCMDiagnosis?.Trim(),
        TreatmentPrinciple = TreatmentPrinciple?.Trim(),

        // 中医四诊合参
        Inspection = Inspection?.Trim(),
        AuscultationOlfaction = AuscultationOlfaction?.Trim(),
        Inquiry = Inquiry?.Trim(),
        Palpation = Palpation?.Trim(),

        // 其他字段
        Remark = Remark?.Trim()
    };
}

#endregion
```

**使用场景**：
```csharp
// 场景1: 暂存按钮命令
public AsyncDelegateCommand SaveDraftCommand { get; private set; }

private async Task ExecuteSaveDraftAsync()
{
    await SaveAsync();
    await _messageService.ShowSuccessAsync("暂存成功", "诊断数据已保存");
}

// 场景2: 自动保存（定时触发）
private async void OnAutoSaveTimerElapsed(object sender, ElapsedEventArgs e)
{
    if (HasUnsavedChanges)
    {
        await SaveAsync();
    }
}

// 场景3: 导航离开前保存
public async void OnNavigatedFrom(NavigationContext navigationContext)
{
    if (HasUnsavedChanges)
    {
        await SaveAsync();
    }
}
```

---

## 3. 中医四诊合参开发

### 3.1 四诊数据模型

**四诊字段定义**：
```csharp
#region 中医四诊合参

/// <summary>
/// 望诊（Inspection）：观察患者神、色、形、态
/// </summary>
private string _inspection = string.Empty;
public string Inspection
{
    get => _inspection;
    set => SetProperty(ref _inspection, value);
}

/// <summary>
/// 闻诊（Auscultation & Olfaction）：听声音、嗅气味
/// </summary>
private string _auscultationOlfaction = string.Empty;
public string AuscultationOlfaction
{
    get => _auscultationOlfaction;
    set => SetProperty(ref _auscultationOlfaction, value);
}

/// <summary>
/// 问诊（Inquiry）：询问病史、症状、生活习惯
/// </summary>
private string _inquiry = string.Empty;
public string Inquiry
{
    get => _inquiry;
    set => SetProperty(ref _inquiry, value);
}

/// <summary>
/// 切诊（Palpation）：脉诊、按腹
/// </summary>
private string _palpation = string.Empty;
public string Palpation
{
    get => _palpation;
    set => SetProperty(ref _palpation, value);
}

#endregion
```

### 3.2 四诊UI布局（XAML）

**Material Design卡片布局**：
```xml
<!-- 中医四诊合参 -->
<materialDesign:Card Margin="0,16,0,0" Padding="16">
    <StackPanel>
        <TextBlock Text="中医四诊合参"
                   Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                   Margin="0,0,0,16" />

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- 望诊 -->
            <materialDesign:Card Grid.Row="0" Margin="0,0,0,12" Padding="12">
                <StackPanel>
                    <TextBlock Text="望诊（Inspection）"
                               Style="{StaticResource MaterialDesignBody1TextBlock}"
                               FontWeight="SemiBold"
                               Margin="0,0,0,8"/>
                    <TextBox Text="{Binding Inspection, UpdateSourceTrigger=PropertyChanged}"
                             materialDesign:HintAssist.Hint="观察患者神、色、形、态"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             TextWrapping="Wrap"
                             VerticalScrollBarVisibility="Auto"
                             MinHeight="80"
                             MaxHeight="120"/>
                </StackPanel>
            </materialDesign:Card>

            <!-- 闻诊 -->
            <materialDesign:Card Grid.Row="1" Margin="0,0,0,12" Padding="12">
                <StackPanel>
                    <TextBlock Text="闻诊（Auscultation & Olfaction）"
                               Style="{StaticResource MaterialDesignBody1TextBlock}"
                               FontWeight="SemiBold"
                               Margin="0,0,0,8"/>
                    <TextBox Text="{Binding AuscultationOlfaction, UpdateSourceTrigger=PropertyChanged}"
                             materialDesign:HintAssist.Hint="听声音、嗅气味"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             TextWrapping="Wrap"
                             VerticalScrollBarVisibility="Auto"
                             MinHeight="80"
                             MaxHeight="120"/>
                </StackPanel>
            </materialDesign:Card>

            <!-- 问诊 -->
            <materialDesign:Card Grid.Row="2" Margin="0,0,0,12" Padding="12">
                <StackPanel>
                    <TextBlock Text="问诊（Inquiry）"
                               Style="{StaticResource MaterialDesignBody1TextBlock}"
                               FontWeight="SemiBold"
                               Margin="0,0,0,8"/>
                    <TextBox Text="{Binding Inquiry, UpdateSourceTrigger=PropertyChanged}"
                             materialDesign:HintAssist.Hint="询问病史、症状、生活习惯"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             TextWrapping="Wrap"
                             VerticalScrollBarVisibility="Auto"
                             MinHeight="80"
                             MaxHeight="120"/>
                </StackPanel>
            </materialDesign:Card>

            <!-- 切诊 -->
            <materialDesign:Card Grid.Row="3" Padding="12">
                <StackPanel>
                    <TextBlock Text="切诊（Palpation）"
                               Style="{StaticResource MaterialDesignBody1TextBlock}"
                               FontWeight="SemiBold"
                               Margin="0,0,0,8"/>
                    <TextBox Text="{Binding Palpation, UpdateSourceTrigger=PropertyChanged}"
                             materialDesign:HintAssist.Hint="脉诊、按腹"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             TextWrapping="Wrap"
                             VerticalScrollBarVisibility="Auto"
                             MinHeight="80"
                             MaxHeight="120"/>
                </StackPanel>
            </materialDesign:Card>
        </Grid>
    </StackPanel>
</materialDesign:Card>
```

### 3.3 四诊数据验证（可选）

**业务规则**：四诊字段均为可选，但建议至少填写一项以支持辨证论治。

```csharp
/// <summary>
/// 检查是否至少填写了一项四诊
/// </summary>
public bool HasAnyFourDiagnosis =>
    !string.IsNullOrWhiteSpace(Inspection) ||
    !string.IsNullOrWhiteSpace(AuscultationOlfaction) ||
    !string.IsNullOrWhiteSpace(Inquiry) ||
    !string.IsNullOrWhiteSpace(Palpation);

/// <summary>
/// 四诊完整性提示
/// </summary>
public string FourDiagnosisHint
{
    get
    {
        if (!HasAnyFourDiagnosis)
            return "建议至少填写一项四诊信息以支持辨证论治";

        return string.Empty;
    }
}
```

---

## 4. 三步工作流集成

### 4.1 工作流状态管理

**REQ-001 三步工作流优化**：Step1 (诊断) → Step2 (处方) → Step3 (总结)

**状态属性定义**：
```csharp
#region REQ-001 三步工作流优化

/// <summary>
/// Step1完成时间（标记诊断完成，允许进入Step2）
/// </summary>
private DateTime? _step1CompletedAt;
public DateTime? Step1CompletedAt
{
    get => _step1CompletedAt;
    private set
    {
        if (SetProperty(ref _step1CompletedAt, value))
        {
            // 级联更新计算属性
            RaisePropertyChanged(nameof(Step1CompletedAtText));
            RaisePropertyChanged(nameof(Step1CompletedAtVisibility));
            RaisePropertyChanged(nameof(PrescriptionDisabled));
        }
    }
}

/// <summary>
/// Step1完成时间显示文本
/// </summary>
public string Step1CompletedAtText =>
    Step1CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

/// <summary>
/// Step1完成标记可见性
/// </summary>
public Visibility Step1CompletedAtVisibility =>
    Step1CompletedAt.HasValue ? Visibility.Visible : Visibility.Collapsed;

/// <summary>
/// 处方功能是否启用
/// </summary>
private bool _prescriptionEnabled = true;
public bool PrescriptionEnabled
{
    get => _prescriptionEnabled;
    set
    {
        if (SetProperty(ref _prescriptionEnabled, value))
        {
            RaisePropertyChanged(nameof(PrescriptionDisabled));
        }
    }
}

/// <summary>
/// 处方功能是否禁用（UI绑定用）
/// </summary>
public bool PrescriptionDisabled => !PrescriptionEnabled || !Step1CompletedAt.HasValue;

#endregion
```

### 4.2 CompleteStep1Command实现

**完成诊断命令**：
```csharp
public AsyncDelegateCommand CompleteStep1Command { get; private set; }

private void InitializeCommands()
{
    CompleteStep1Command = new AsyncDelegateCommand(
        ExecuteCompleteStep1Async,
        CanCompleteStep1)
        .ObservesProperty(() => ChiefComplaint)
        .ObservesProperty(() => TCMDiagnosis);
}

/// <summary>
/// 执行完成Step1
/// </summary>
private async Task ExecuteCompleteStep1Async()
{
    try
    {
        IsBusy = true;

        // 1️⃣ 验证必填项
        if (!Validate())
        {
            await _messageService.ShowWarningAsync(
                "验证失败",
                ValidationMessage);
            return;
        }

        // 2️⃣ 保存诊断数据
        await SaveAsync();

        // 3️⃣ 标记Step1完成（调用Repository）
        var result = await _medicalCaseRepository
            .CompleteStep1Async(MedicalCaseId);

        if (result != null)
        {
            // 更新本地状态
            Step1CompletedAt = DateTime.Now;
            Logger.LogInformation("Step1完成，病案ID: {MedicalCaseId}", MedicalCaseId);

            // 发布完成事件
            await PublishMessage(new Step1CompletedMessage(MedicalCaseId));

            // 提示用户
            await _messageService.ShowSuccessAsync(
                "诊断完成",
                "已完成辨证阶段，可以进入施治阶段");
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "完成Step1失败，病案ID: {MedicalCaseId}", MedicalCaseId);
        await _messageService.ShowErrorAsync(
            "操作失败",
            $"完成诊断失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

/// <summary>
/// 是否可以完成Step1
/// </summary>
private bool CanCompleteStep1()
{
    // 必须有患者信息 + 验证通过 + 未完成
    return CurrentPatient != null &&
           !string.IsNullOrWhiteSpace(ChiefComplaint) &&
           !string.IsNullOrWhiteSpace(TCMDiagnosis) &&
           !Step1CompletedAt.HasValue;
}
```

### 4.3 UI绑定示例

**完成诊断按钮**：
```xml
<!-- 完成诊断按钮 -->
<Button Content="完成诊断"
        Command="{Binding CompleteStep1Command}"
        Style="{StaticResource MaterialDesignRaisedButton}"
        Width="120"
        Margin="8,0,0,0">
    <Button.ToolTip>
        <ToolTip>
            <StackPanel>
                <TextBlock Text="完成辨证阶段（Step1）" FontWeight="SemiBold"/>
                <TextBlock Text="必填项：主诉、中医诊断"/>
                <TextBlock Text="完成后可以进入施治阶段（Step2）"/>
            </StackPanel>
        </ToolTip>
    </Button.ToolTip>
</Button>

<!-- Step1完成标记 -->
<Border Background="{StaticResource PrimaryHueLightBrush}"
        CornerRadius="4"
        Padding="8,4"
        Visibility="{Binding Step1CompletedAtVisibility}">
    <StackPanel Orientation="Horizontal">
        <materialDesign:PackIcon Kind="CheckCircle"
                                 Foreground="White"
                                 VerticalAlignment="Center"/>
        <TextBlock Text="已完成诊断"
                   Foreground="White"
                   Margin="8,0,0,0"
                   VerticalAlignment="Center"/>
        <TextBlock Text="{Binding Step1CompletedAtText, StringFormat='({0})'}"
                   Foreground="White"
                   Margin="4,0,0,0"
                   VerticalAlignment="Center"
                   FontSize="12"
                   Opacity="0.9"/>
    </StackPanel>
</Border>
```

---

## 5. ViewModel开发模式

### 5.1 继承UnifiedViewModelBase

**基类提供的核心功能**：
```csharp
public abstract class UnifiedViewModelBase : BindableBase
{
    // 1️⃣ IsBusy状态管理
    public bool IsBusy { get; set; }

    // 2️⃣ 日志记录器
    protected ILogger Logger { get; }

    // 3️⃣ 消息发布（EventAggregator）
    protected Task PublishMessage<T>(T message);

    // 4️⃣ 导航生命周期（INavigationAware）
    public virtual void OnNavigatedTo(NavigationContext navigationContext);
    public virtual void OnNavigatedFrom(NavigationContext navigationContext);
    public virtual bool IsNavigationTarget(NavigationContext navigationContext);
}
```

**ViewModel类声明**：
```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase,
                                         IValidatable,
                                         ISaveable,
                                         INavigationAware  // 可选，已在基类实现
{
    public ConsultationFormViewModel(
        IMedicalCaseRepository medicalCaseRepository,
        ILogger<ConsultationFormViewModel> logger)
        : base(logger)
    {
        _medicalCaseRepository = medicalCaseRepository;
        InitializeCommands();
    }
}
```

### 5.2 属性定义模式

**数据绑定属性**：
```csharp
// ✅ 推荐：完整的属性实现
private string _chiefComplaint = string.Empty;

[Required(ErrorMessage = "主诉不能为空")]
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        // SetProperty自动触发INotifyPropertyChanged
        if (SetProperty(ref _chiefComplaint, value))
        {
            // 级联更新计算属性
            RaisePropertyChanged(nameof(HasChiefComplaint));

            // 更新命令CanExecute
            CompleteStep1Command?.RaiseCanExecuteChanged();
        }
    }
}

// 计算属性（只读）
public bool HasChiefComplaint =>
    !string.IsNullOrWhiteSpace(ChiefComplaint);
```

**外部数据属性**：
```csharp
// 从导航参数接收的数据
private PatientDto? _currentPatient;
public PatientDto? CurrentPatient
{
    get => _currentPatient;
    set => SetProperty(ref _currentPatient, value);
}

private Guid _medicalCaseId;
public Guid MedicalCaseId
{
    get => _medicalCaseId;
    set => SetProperty(ref _medicalCaseId, value);
}
```

### 5.3 命令定义模式

**AsyncDelegateCommand**：
```csharp
public AsyncDelegateCommand CompleteStep1Command { get; private set; }
public AsyncDelegateCommand SaveDraftCommand { get; private set; }
public DelegateCommand ClearFormCommand { get; private set; }

private void InitializeCommands()
{
    // 异步命令（带CanExecute）
    CompleteStep1Command = new AsyncDelegateCommand(
        ExecuteCompleteStep1Async,
        CanCompleteStep1)
        .ObservesProperty(() => ChiefComplaint)
        .ObservesProperty(() => TCMDiagnosis);

    // 异步命令（无CanExecute）
    SaveDraftCommand = new AsyncDelegateCommand(
        ExecuteSaveDraftAsync);

    // 同步命令
    ClearFormCommand = new DelegateCommand(
        ExecuteClearForm,
        CanClearForm)
        .ObservesProperty(() => ChiefComplaint);
}
```

**命令实现**：
```csharp
private async Task ExecuteCompleteStep1Async()
{
    try
    {
        IsBusy = true;

        // 验证 → 保存 → 完成
        if (!Validate()) return;
        await SaveAsync();
        var result = await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId);

        // 处理结果...
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "完成Step1失败");
        throw;
    }
    finally
    {
        IsBusy = false;
    }
}

private bool CanCompleteStep1()
{
    return CurrentPatient != null &&
           !string.IsNullOrWhiteSpace(ChiefComplaint) &&
           !string.IsNullOrWhiteSpace(TCMDiagnosis) &&
           !Step1CompletedAt.HasValue;
}
```

### 5.4 导航生命周期

**INavigationAware实现**：
```csharp
/// <summary>
/// 导航到此视图时触发
/// </summary>
public override void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);

    // 1️⃣ 接收导航参数
    if (navigationContext.Parameters.TryGetValue("CurrentPatient", out PatientDto patient))
    {
        CurrentPatient = patient;
    }

    if (navigationContext.Parameters.TryGetValue("MedicalCaseId", out Guid medicalCaseId))
    {
        MedicalCaseId = medicalCaseId;
    }

    // 2️⃣ 加载数据
    _ = LoadConsultationDataAsync();

    Logger.LogInformation("导航到诊疗表单，患者ID: {PatientId}, 病案ID: {MedicalCaseId}",
        CurrentPatient?.Id, MedicalCaseId);
}

/// <summary>
/// 离开此视图时触发
/// </summary>
public override void OnNavigatedFrom(NavigationContext navigationContext)
{
    base.OnNavigatedFrom(navigationContext);

    // 自动保存未提交的数据
    if (HasUnsavedChanges)
    {
        _ = SaveAsync();
    }

    Logger.LogInformation("离开诊疗表单");
}

/// <summary>
/// 是否可以作为导航目标
/// </summary>
public override bool IsNavigationTarget(NavigationContext navigationContext)
{
    // 如果当前已有患者数据，且参数中的患者ID相同，则重用当前ViewModel
    if (navigationContext.Parameters.TryGetValue("CurrentPatient", out PatientDto patient))
    {
        return CurrentPatient?.Id == patient.Id;
    }

    return false;
}
```

---

## 6. Repository集成

### 6.1 IMedicalCaseRepository接口

**Issue #1563说明**：Consultation不再有独立Repository，通过MedicalCaseRepository聚合根访问。

**相关方法**：
```csharp
public interface IMedicalCaseRepository
{
    // ========== Consultation相关 ==========

    /// <summary>
    /// 更新诊断数据
    /// </summary>
    Task<MedicalCaseDto?> UpdateConsultationAsync(
        Guid medicalCaseId,
        ConsultationUpdateDto updateDto);

    /// <summary>
    /// 完成Step1（诊断阶段）
    /// </summary>
    Task<MedicalCaseDto?> CompleteStep1Async(Guid medicalCaseId);

    /// <summary>
    /// 获取病案详情（包含Consultation数据）
    /// </summary>
    Task<MedicalCaseDto?> GetByIdAsync(Guid id);
}
```

### 6.2 UpdateConsultationAsync调用

**更新诊断数据**：
```csharp
private async Task UpdateConsultationAsync()
{
    try
    {
        IsBusy = true;

        // 1️⃣ 构建更新DTO
        var updateDto = new ConsultationUpdateDto
        {
            ChiefComplaint = ChiefComplaint?.Trim(),
            PresentIllness = PresentIllness?.Trim(),
            TCMDiagnosis = TCMDiagnosis?.Trim(),
            TreatmentPrinciple = TreatmentPrinciple?.Trim(),
            Inspection = Inspection?.Trim(),
            AuscultationOlfaction = AuscultationOlfaction?.Trim(),
            Inquiry = Inquiry?.Trim(),
            Palpation = Palpation?.Trim(),
            Remark = Remark?.Trim()
        };

        // 2️⃣ 调用Repository
        var result = await _medicalCaseRepository
            .UpdateConsultationAsync(MedicalCaseId, updateDto);

        // 3️⃣ 处理结果
        if (result != null)
        {
            Logger.LogInformation("诊断数据更新成功");
            HasUnsavedChanges = false;
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "更新诊断数据失败");
        throw;
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 6.3 CompleteStep1Async调用

**完成诊断阶段**：
```csharp
private async Task CompleteStep1InternalAsync()
{
    // 1️⃣ 先保存最新数据
    await SaveAsync();

    // 2️⃣ 调用完成接口
    var result = await _medicalCaseRepository
        .CompleteStep1Async(MedicalCaseId);

    // 3️⃣ 更新本地状态
    if (result != null)
    {
        Step1CompletedAt = result.Step1CompletedAt;

        // 发布完成事件
        await PublishMessage(new Step1CompletedMessage
        {
            MedicalCaseId = MedicalCaseId,
            CompletedAt = Step1CompletedAt.Value
        });
    }
}
```

### 6.4 错误处理

**Repository调用错误处理**：
```csharp
try
{
    var result = await _medicalCaseRepository.UpdateConsultationAsync(
        MedicalCaseId,
        updateDto);
}
catch (HttpRequestException ex)
{
    // HTTP请求失败（网络问题、服务器错误）
    Logger.LogError(ex, "HTTP请求失败");
    await _messageService.ShowErrorAsync(
        "网络错误",
        "无法连接到服务器，请检查网络连接");
}
catch (ApiException ex)
{
    // 业务逻辑错误（验证失败、权限不足等）
    Logger.LogError(ex, "API调用失败：{ErrorCode}", ex.ErrorCode);
    await _messageService.ShowErrorAsync(
        "操作失败",
        ex.Message);
}
catch (Exception ex)
{
    // 未预期错误
    Logger.LogError(ex, "未知错误");
    await _messageService.ShowErrorAsync(
        "系统错误",
        "发生未知错误，请联系管理员");
}
```

---

## 7. UI开发与数据绑定

### 7.1 Material Design样式

**基本布局结构**：
```xml
<UserControl x:Class="LYBT.Desktop.Consultation.Views.ConsultationFormView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <UserControl.Resources>
        <!-- 样式资源 -->
    </UserControl.Resources>

    <Grid Margin="16">
        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <StackPanel>
                <!-- 内容区域 -->
            </StackPanel>
        </ScrollViewer>
    </Grid>
</UserControl>
```

### 7.2 基本诊断信息UI

**主诉、中医诊断等必填项**：
```xml
<!-- 基本诊断信息 -->
<materialDesign:Card Padding="16" Margin="0,0,0,16">
    <StackPanel>
        <TextBlock Text="基本诊断信息"
                   Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                   Margin="0,0,0,16"/>

        <!-- 主诉（必填） -->
        <TextBox Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"
                 materialDesign:HintAssist.Hint="主诉（必填）*"
                 materialDesign:HintAssist.IsFloating="True"
                 Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 MinHeight="80"
                 MaxHeight="120"
                 Margin="0,0,0,16">
            <materialDesign:ValidationAssist.OnlyShowOnFocus>False</materialDesign:ValidationAssist.OnlyShowOnFocus>
        </TextBox>

        <!-- 现病史（可选） -->
        <TextBox Text="{Binding PresentIllness, UpdateSourceTrigger=PropertyChanged}"
                 materialDesign:HintAssist.Hint="现病史"
                 materialDesign:HintAssist.IsFloating="True"
                 Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 MinHeight="80"
                 MaxHeight="120"
                 Margin="0,0,0,16"/>

        <!-- 中医诊断（必填） -->
        <TextBox Text="{Binding TCMDiagnosis, UpdateSourceTrigger=PropertyChanged}"
                 materialDesign:HintAssist.Hint="中医诊断（必填）*"
                 materialDesign:HintAssist.IsFloating="True"
                 Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 MinHeight="80"
                 MaxHeight="120"
                 Margin="0,0,0,16">
            <materialDesign:ValidationAssist.OnlyShowOnFocus>False</materialDesign:ValidationAssist.OnlyShowOnFocus>
        </TextBox>

        <!-- 治则治法（可选） -->
        <TextBox Text="{Binding TreatmentPrinciple, UpdateSourceTrigger=PropertyChanged}"
                 materialDesign:HintAssist.Hint="治则治法"
                 materialDesign:HintAssist.IsFloating="True"
                 Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 MinHeight="60"
                 MaxHeight="100"
                 Margin="0,0,0,16"/>
    </StackPanel>
</materialDesign:Card>
```

### 7.3 数据绑定模式

**双向绑定（TwoWay）**：
```xml
<!-- 默认模式：TwoWay + PropertyChanged -->
<TextBox Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"/>

<!-- 显式指定TwoWay -->
<TextBox Text="{Binding ChiefComplaint, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

**单向绑定（OneWay）**：
```xml
<!-- 只读显示 -->
<TextBlock Text="{Binding Step1CompletedAtText, Mode=OneWay}"/>
<TextBlock Text="{Binding ValidationMessage, Mode=OneWay}"
           Foreground="Red"/>
```

**命令绑定**：
```xml
<!-- 按钮命令绑定 -->
<Button Content="完成诊断"
        Command="{Binding CompleteStep1Command}"
        Style="{StaticResource MaterialDesignRaisedButton}"/>

<!-- 禁用状态绑定 -->
<Button Content="开具处方"
        Command="{Binding OpenPrescriptionCommand}"
        IsEnabled="{Binding PrescriptionEnabled}"
        Style="{StaticResource MaterialDesignOutlinedButton}"/>
```

**可见性绑定**：
```xml
<!-- 直接绑定Visibility枚举 -->
<Border Visibility="{Binding Step1CompletedAtVisibility}">
    <!-- Step1完成标记 -->
</Border>

<!-- 使用Converter转换Boolean -->
<TextBlock Visibility="{Binding HasValidationError,
                        Converter={StaticResource BooleanToVisibilityConverter}}"/>
```

### 7.4 验证错误显示

**ValidationMessage显示**：
```xml
<!-- 验证错误消息区域 -->
<Border Background="{StaticResource ValidationErrorBrush}"
        BorderBrush="{StaticResource PrimaryHueMidBrush}"
        BorderThickness="1"
        CornerRadius="4"
        Padding="12"
        Margin="0,0,0,16"
        Visibility="{Binding ValidationMessage,
                     Converter={StaticResource StringNotEmptyToVisibilityConverter}}">
    <StackPanel Orientation="Horizontal">
        <materialDesign:PackIcon Kind="AlertCircle"
                                 Foreground="{StaticResource PrimaryHueMidBrush}"
                                 VerticalAlignment="Center"/>
        <TextBlock Text="{Binding ValidationMessage}"
                   Foreground="{StaticResource PrimaryHueMidBrush}"
                   Margin="8,0,0,0"
                   TextWrapping="Wrap"
                   VerticalAlignment="Center"/>
    </StackPanel>
</Border>
```

**StringNotEmptyToVisibilityConverter**：
```csharp
public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

---

## 8. 暂存与继续功能

### 8.1 暂存功能实现

**SaveDraftCommand**：
```csharp
public AsyncDelegateCommand SaveDraftCommand { get; private set; }

private void InitializeCommands()
{
    SaveDraftCommand = new AsyncDelegateCommand(
        ExecuteSaveDraftAsync,
        CanSaveDraft);
}

/// <summary>
/// 执行暂存
/// </summary>
private async Task ExecuteSaveDraftAsync()
{
    try
    {
        IsBusy = true;

        // 1️⃣ 调用ISaveable.SaveAsync
        await SaveAsync();

        // 2️⃣ 标记无未保存更改
        HasUnsavedChanges = false;

        // 3️⃣ 提示用户
        await _messageService.ShowSuccessAsync(
            "暂存成功",
            "诊断数据已保存，可以稍后继续编辑");

        Logger.LogInformation("暂存诊断数据成功，病案ID: {MedicalCaseId}", MedicalCaseId);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "暂存诊断数据失败");
        await _messageService.ShowErrorAsync(
            "暂存失败",
            $"保存失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

/// <summary>
/// 是否可以暂存
/// </summary>
private bool CanSaveDraft()
{
    // 至少有主诉或中医诊断其中一项
    return !string.IsNullOrWhiteSpace(ChiefComplaint) ||
           !string.IsNullOrWhiteSpace(TCMDiagnosis);
}
```

### 8.2 未保存更改检测

**HasUnsavedChanges属性**：
```csharp
private bool _hasUnsavedChanges;

/// <summary>
/// 是否有未保存的更改
/// </summary>
public bool HasUnsavedChanges
{
    get => _hasUnsavedChanges;
    private set => SetProperty(ref _hasUnsavedChanges, value);
}

/// <summary>
/// 标记有未保存的更改
/// </summary>
private void MarkAsChanged()
{
    HasUnsavedChanges = true;
}

// 在属性Setter中调用
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        if (SetProperty(ref _chiefComplaint, value))
        {
            MarkAsChanged();
            RaisePropertyChanged(nameof(HasChiefComplaint));
        }
    }
}
```

### 8.3 继续编辑功能

**加载已暂存的数据**：
```csharp
/// <summary>
/// 加载诊断数据（继续编辑）
/// </summary>
private async Task LoadConsultationDataAsync()
{
    try
    {
        IsBusy = true;

        // 1️⃣ 获取病案详情（包含Consultation数据）
        var medicalCase = await _medicalCaseRepository.GetByIdAsync(MedicalCaseId);

        if (medicalCase?.Consultation == null)
        {
            Logger.LogWarning("未找到诊断数据，病案ID: {MedicalCaseId}", MedicalCaseId);
            return;
        }

        // 2️⃣ 填充表单数据
        var consultation = medicalCase.Consultation;

        ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
        PresentIllness = consultation.PresentIllness ?? string.Empty;
        TCMDiagnosis = consultation.TCMDiagnosis ?? string.Empty;
        TreatmentPrinciple = consultation.TreatmentPrinciple ?? string.Empty;

        Inspection = consultation.Inspection ?? string.Empty;
        AuscultationOlfaction = consultation.AuscultationOlfaction ?? string.Empty;
        Inquiry = consultation.Inquiry ?? string.Empty;
        Palpation = consultation.Palpation ?? string.Empty;

        Remark = consultation.Remark ?? string.Empty;

        // 3️⃣ 更新工作流状态
        Step1CompletedAt = medicalCase.Step1CompletedAt;

        // 4️⃣ 重置未保存标记
        HasUnsavedChanges = false;

        Logger.LogInformation("已加载诊断数据，病案ID: {MedicalCaseId}", MedicalCaseId);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载诊断数据失败，病案ID: {MedicalCaseId}", MedicalCaseId);
        throw;
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 8.4 自动暂存（可选）

**定时自动保存**：
```csharp
private Timer? _autoSaveTimer;
private const int AUTO_SAVE_INTERVAL = 60000; // 60秒

/// <summary>
/// 启用自动暂存
/// </summary>
private void EnableAutoSave()
{
    _autoSaveTimer = new Timer(AUTO_SAVE_INTERVAL);
    _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
    _autoSaveTimer.Start();

    Logger.LogInformation("已启用自动暂存，间隔: {Interval}秒", AUTO_SAVE_INTERVAL / 1000);
}

/// <summary>
/// 自动暂存触发
/// </summary>
private async void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
{
    if (HasUnsavedChanges && !IsBusy)
    {
        try
        {
            await SaveAsync();
            HasUnsavedChanges = false;
            Logger.LogInformation("自动暂存成功");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "自动暂存失败");
        }
    }
}

/// <summary>
/// 停止自动暂存
/// </summary>
public void Dispose()
{
    _autoSaveTimer?.Stop();
    _autoSaveTimer?.Dispose();
}
```

---

## 9. 验证与错误处理

### 9.1 必填项验证

**ChiefComplaint + TCMDiagnosis验证**：
```csharp
public bool Validate()
{
    var errors = new List<string>();

    // 必填项1: 主诉
    if (string.IsNullOrWhiteSpace(ChiefComplaint))
    {
        errors.Add("主诉不能为空");
    }
    else if (ChiefComplaint.Length < 2)
    {
        errors.Add("主诉至少需要2个字符");
    }
    else if (ChiefComplaint.Length > 500)
    {
        errors.Add("主诉不能超过500个字符");
    }

    // 必填项2: 中医诊断
    if (string.IsNullOrWhiteSpace(TCMDiagnosis))
    {
        errors.Add("中医诊断不能为空");
    }
    else if (TCMDiagnosis.Length < 2)
    {
        errors.Add("中医诊断至少需要2个字符");
    }
    else if (TCMDiagnosis.Length > 500)
    {
        errors.Add("中医诊断不能超过500个字符");
    }

    // 构建错误消息
    if (errors.Any())
    {
        ValidationMessage = string.Join("；", errors);
        return false;
    }

    ValidationMessage = string.Empty;
    return true;
}
```

### 9.2 字段长度验证

**字符串长度限制**：
```csharp
/// <summary>
/// 验证字段长度
/// </summary>
private bool ValidateFieldLength(
    string fieldName,
    string? value,
    int minLength,
    int maxLength,
    List<string> errors)
{
    if (string.IsNullOrWhiteSpace(value))
        return true; // 可选字段，空值跳过

    if (value.Length < minLength)
    {
        errors.Add($"{fieldName}至少需要{minLength}个字符");
        return false;
    }

    if (value.Length > maxLength)
    {
        errors.Add($"{fieldName}不能超过{maxLength}个字符");
        return false;
    }

    return true;
}

// 使用示例
ValidateFieldLength("治则治法", TreatmentPrinciple, 2, 500, errors);
ValidateFieldLength("望诊", Inspection, 2, 2000, errors);
```

### 9.3 异常处理模式

**统一异常处理**：
```csharp
private async Task ExecuteOperationAsync(Func<Task> operation, string operationName)
{
    try
    {
        IsBusy = true;
        await operation();
    }
    catch (ValidationException ex)
    {
        // 验证异常
        Logger.LogWarning(ex, "{OperationName}验证失败", operationName);
        await _messageService.ShowWarningAsync(
            "验证失败",
            ex.Message);
    }
    catch (HttpRequestException ex)
    {
        // 网络异常
        Logger.LogError(ex, "{OperationName}网络请求失败", operationName);
        await _messageService.ShowErrorAsync(
            "网络错误",
            "无法连接到服务器，请检查网络连接");
    }
    catch (ApiException ex)
    {
        // API业务异常
        Logger.LogError(ex, "{OperationName}业务异常，错误码: {ErrorCode}",
            operationName, ex.ErrorCode);
        await _messageService.ShowErrorAsync(
            "操作失败",
            ex.Message);
    }
    catch (Exception ex)
    {
        // 未知异常
        Logger.LogError(ex, "{OperationName}发生未知错误", operationName);
        await _messageService.ShowErrorAsync(
            "系统错误",
            "发生未知错误，请联系管理员");
        throw;
    }
    finally
    {
        IsBusy = false;
    }
}

// 使用示例
await ExecuteOperationAsync(
    async () => await SaveAsync(),
    "保存诊断数据");
```

### 9.4 用户友好的错误提示

**IMessageService使用**：
```csharp
// 成功提示
await _messageService.ShowSuccessAsync(
    "操作成功",
    "诊断数据已保存");

// 警告提示
await _messageService.ShowWarningAsync(
    "验证失败",
    "主诉和中医诊断为必填项");

// 错误提示
await _messageService.ShowErrorAsync(
    "保存失败",
    "网络连接超时，请稍后重试");

// 确认对话框
var confirmed = await _messageService.ShowConfirmAsync(
    "确认操作",
    "是否确认完成诊断？完成后将进入施治阶段");

if (confirmed)
{
    await CompleteStep1InternalAsync();
}
```

---

## 10. 最佳实践

### 10.1 异步优先原则

**所有I/O操作必须异步**：
```csharp
// ✅ 正确：异步I/O
public async Task SaveAsync()
{
    await _medicalCaseRepository.UpdateConsultationAsync(
        MedicalCaseId,
        updateDto);
}

// ❌ 错误：同步I/O（阻塞UI线程）
public void SaveSync()
{
    var task = _medicalCaseRepository.UpdateConsultationAsync(
        MedicalCaseId,
        updateDto);
    task.Wait(); // 阻塞UI线程
}
```

**AsyncDelegateCommand使用**：
```csharp
// ✅ 正确：AsyncDelegateCommand
public AsyncDelegateCommand SaveCommand { get; private set; }

SaveCommand = new AsyncDelegateCommand(
    ExecuteSaveAsync,
    CanSave);

// ❌ 错误：DelegateCommand + async void
public DelegateCommand SaveCommand { get; private set; }

SaveCommand = new DelegateCommand(
    async () => await ExecuteSaveAsync()); // async void，异常无法捕获
```

### 10.2 日志记录规范

**日志级别使用**：
```csharp
// Information: 正常操作流程
Logger.LogInformation("开始加载诊断数据，病案ID: {MedicalCaseId}", MedicalCaseId);
Logger.LogInformation("诊断数据保存成功");

// Warning: 预期内的异常情况
Logger.LogWarning("验证失败：{ValidationMessage}", ValidationMessage);
Logger.LogWarning("未找到诊断数据，病案ID: {MedicalCaseId}", MedicalCaseId);

// Error: 需要关注的错误
Logger.LogError(ex, "保存诊断数据失败，病案ID: {MedicalCaseId}", MedicalCaseId);
Logger.LogError(ex, "网络请求失败");

// Debug: 开发调试信息（生产环境不记录）
Logger.LogDebug("ChiefComplaint changed: {Value}", ChiefComplaint);
```

**结构化日志**：
```csharp
// ✅ 推荐：结构化日志（便于查询和分析）
Logger.LogInformation(
    "完成Step1，患者ID: {PatientId}, 病案ID: {MedicalCaseId}, 用时: {ElapsedMs}ms",
    CurrentPatient?.Id,
    MedicalCaseId,
    stopwatch.ElapsedMilliseconds);

// ❌ 不推荐：字符串拼接
Logger.LogInformation(
    $"完成Step1，患者ID: {CurrentPatient?.Id}, 病案ID: {MedicalCaseId}");
```

### 10.3 资源释放

**IDisposable实现**：
```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase,
                                         IValidatable,
                                         ISaveable,
                                         IDisposable
{
    private Timer? _autoSaveTimer;
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // 释放托管资源
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = null;

            Logger.LogInformation("ConsultationFormViewModel已释放");
        }

        _disposed = true;
    }
}
```

### 10.4 性能优化

**避免不必要的属性通知**：
```csharp
// ✅ 推荐：SetProperty自动检查值是否变化
public string ChiefComplaint
{
    get => _chiefComplaint;
    set => SetProperty(ref _chiefComplaint, value); // 值未变化时不触发通知
}

// ❌ 不推荐：每次都触发通知
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        _chiefComplaint = value;
        RaisePropertyChanged(); // 无论值是否变化都触发
    }
}
```

**批量属性更新**：
```csharp
// ✅ 推荐：批量更新后统一通知
public void LoadConsultationData(ConsultationDto dto)
{
    // 禁用属性通知
    SuspendPropertyChangedNotifications();

    try
    {
        ChiefComplaint = dto.ChiefComplaint ?? string.Empty;
        PresentIllness = dto.PresentIllness ?? string.Empty;
        TCMDiagnosis = dto.TCMDiagnosis ?? string.Empty;
        // ... 更多属性
    }
    finally
    {
        // 恢复属性通知并触发一次
        ResumePropertyChangedNotifications();
    }
}
```

### 10.5 依赖注入最佳实践

**构造函数注入**：
```csharp
// ✅ 推荐：构造函数注入
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMessageService _messageService;

    public ConsultationFormViewModel(
        IMedicalCaseRepository repository,
        IMessageService messageService,
        ILogger<ConsultationFormViewModel> logger)
        : base(logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    }
}

// ❌ 禁止：ServiceLocator反模式
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    private IMedicalCaseRepository _repository;

    public ConsultationFormViewModel()
    {
        _repository = Container.Resolve<IMedicalCaseRepository>(); // 反模式
    }
}
```

---

## 11. 常见问题

### Q1: 如何处理"主诉和中医诊断必填，但用户想先暂存部分数据"？

**A**: 暂存功能不强制验证必填项，只在完成Step1时验证。

```csharp
// SaveDraftCommand: 不验证必填项
private bool CanSaveDraft()
{
    // 至少有任意一项数据即可暂存
    return !string.IsNullOrWhiteSpace(ChiefComplaint) ||
           !string.IsNullOrWhiteSpace(TCMDiagnosis) ||
           !string.IsNullOrWhiteSpace(Inspection);
}

// CompleteStep1Command: 强制验证必填项
private bool CanCompleteStep1()
{
    return Validate(); // 必须通过验证
}
```

### Q2: Step1完成后，用户想修改诊断数据怎么办？

**A**: Step1完成后仍可修改，但需要提供"重新编辑"功能。

```csharp
/// <summary>
/// 重新编辑Step1（取消完成状态）
/// </summary>
public AsyncDelegateCommand ReEditStep1Command { get; private set; }

private async Task ExecuteReEditStep1Async()
{
    var confirmed = await _messageService.ShowConfirmAsync(
        "确认重新编辑",
        "取消完成状态后，需要重新验证并提交诊断数据");

    if (confirmed)
    {
        Step1CompletedAt = null;
        Logger.LogInformation("已取消Step1完成状态，病案ID: {MedicalCaseId}", MedicalCaseId);
    }
}
```

### Q3: 如何实现"快速录入"功能（模板、历史记录）？

**A**: 提供历史诊断模板选择功能。

```csharp
/// <summary>
/// 应用诊断模板
/// </summary>
public AsyncDelegateCommand<ConsultationTemplateDto> ApplyTemplateCommand { get; private set; }

private async Task ExecuteApplyTemplateAsync(ConsultationTemplateDto template)
{
    if (template == null) return;

    var confirmed = await _messageService.ShowConfirmAsync(
        "应用模板",
        $"是否应用模板"{template.Name}"？当前数据将被覆盖");

    if (confirmed)
    {
        ChiefComplaint = template.ChiefComplaint;
        TCMDiagnosis = template.TCMDiagnosis;
        TreatmentPrinciple = template.TreatmentPrinciple;

        Logger.LogInformation("已应用诊断模板: {TemplateName}", template.Name);
    }
}
```

### Q4: 如何处理网络超时或离线场景？

**A**: 实现离线缓存和自动重试机制。

```csharp
/// <summary>
/// 保存到本地缓存（离线场景）
/// </summary>
private async Task SaveToLocalCacheAsync()
{
    var cacheKey = $"consultation_{MedicalCaseId}";
    var cacheData = BuildConsultationUpdateDto();

    await _cacheService.SetAsync(cacheKey, cacheData, TimeSpan.FromHours(24));

    Logger.LogInformation("已保存到本地缓存，病案ID: {MedicalCaseId}", MedicalCaseId);
}

/// <summary>
/// 同步本地缓存到服务器
/// </summary>
private async Task SyncLocalCacheAsync()
{
    var cacheKey = $"consultation_{MedicalCaseId}";
    var cacheData = await _cacheService.GetAsync<ConsultationUpdateDto>(cacheKey);

    if (cacheData != null)
    {
        await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, cacheData);
        await _cacheService.RemoveAsync(cacheKey);

        Logger.LogInformation("本地缓存已同步到服务器");
    }
}
```

### Q5: 如何优化大量文本输入的性能（如四诊合参）？

**A**: 使用防抖（Debounce）延迟属性通知。

```csharp
private string _inspection = string.Empty;
private CancellationTokenSource? _inspectionDebounceToken;

public string Inspection
{
    get => _inspection;
    set
    {
        if (_inspection == value) return;

        _inspection = value;

        // 取消之前的防抖任务
        _inspectionDebounceToken?.Cancel();
        _inspectionDebounceToken = new CancellationTokenSource();

        // 300ms后才触发属性通知
        Task.Delay(300, _inspectionDebounceToken.Token)
            .ContinueWith(_ =>
            {
                if (!_inspectionDebounceToken.Token.IsCancellationRequested)
                {
                    RaisePropertyChanged(nameof(Inspection));
                    MarkAsChanged();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
```

---

## 12. 调试技巧

### 12.1 日志追踪

**启用详细日志**：
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LYBT.Desktop.Consultation": "Debug",  // 启用Consultation模块Debug日志
      "LYBT.Desktop.Core": "Debug"
    }
  }
}
```

**关键操作日志**：
```csharp
Logger.LogDebug("属性变更: ChiefComplaint = {Value}", ChiefComplaint);
Logger.LogDebug("验证结果: IsValid = {IsValid}, Message = {Message}",
    Validate(), ValidationMessage);
Logger.LogDebug("CanCompleteStep1: {CanExecute}", CanCompleteStep1());
```

### 12.2 断点调试

**关键断点位置**：
```csharp
// 1️⃣ 导航生命周期
public override void OnNavigatedTo(NavigationContext navigationContext)
{
    // 🔍 断点：检查导航参数
    var patient = navigationContext.Parameters["CurrentPatient"];
    var caseId = navigationContext.Parameters["MedicalCaseId"];
}

// 2️⃣ 验证逻辑
public bool Validate()
{
    // 🔍 断点：检查验证条件
    if (string.IsNullOrWhiteSpace(ChiefComplaint))
    {
        // 为什么验证失败？
    }
}

// 3️⃣ Repository调用
private async Task SaveAsync()
{
    var updateDto = BuildConsultationUpdateDto();
    // 🔍 断点：检查DTO数据
    var result = await _medicalCaseRepository.UpdateConsultationAsync(
        MedicalCaseId,
        updateDto);
    // 🔍 断点：检查返回结果
}

// 4️⃣ 属性变更
public string ChiefComplaint
{
    set
    {
        // 🔍 断点：检查属性变更触发
        if (SetProperty(ref _chiefComplaint, value))
        {
            // 触发了哪些计算属性？
        }
    }
}
```

### 12.3 Snoop工具使用

**Snoop是WPF调试工具，用于检查运行时UI树和数据绑定**：

1. **下载Snoop**: https://github.com/snoopwpf/snoop
2. **附加到LYBT.Desktop进程**
3. **检查ConsultationFormView**:
   - 查看DataContext是否正确绑定到ConsultationFormViewModel
   - 检查Binding表达式是否有错误（红色标记）
   - 查看属性值实时变化

**常见Binding错误**：
```
System.Windows.Data Error: 40 : BindingExpression path error:
'ChiefComplaint' property not found on 'object'
''ConsultationFormViewModel'

原因：属性名拼写错误或访问修饰符不是public
```

### 12.4 性能分析

**测量操作耗时**：
```csharp
private async Task ExecuteCompleteStep1Async()
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        // 操作逻辑
        await SaveAsync();
        await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId);
    }
    finally
    {
        stopwatch.Stop();
        Logger.LogInformation(
            "CompleteStep1耗时: {ElapsedMs}ms",
            stopwatch.ElapsedMilliseconds);
    }
}
```

**检测内存泄漏**：
```csharp
// 在ViewModel析构函数中记录
~ConsultationFormViewModel()
{
    Logger.LogWarning("ConsultationFormViewModel被GC回收");
    // 如果长时间未回收，可能存在内存泄漏
}
```

### 12.5 单元测试

**ViewModel单元测试示例**：
```csharp
[Fact]
public void Validate_WithEmptyChiefComplaint_ReturnsFalse()
{
    // Arrange
    var mockRepository = Substitute.For<IMedicalCaseRepository>();
    var mockLogger = Substitute.For<ILogger<ConsultationFormViewModel>>();
    var viewModel = new ConsultationFormViewModel(mockRepository, mockLogger)
    {
        ChiefComplaint = string.Empty,
        TCMDiagnosis = "测试诊断"
    };

    // Act
    var result = viewModel.Validate();

    // Assert
    Assert.False(result);
    Assert.Contains("主诉不能为空", viewModel.ValidationMessage);
}

[Fact]
public async Task CompleteStep1Async_ValidData_CallsRepository()
{
    // Arrange
    var mockRepository = Substitute.For<IMedicalCaseRepository>();
    mockRepository.CompleteStep1Async(Arg.Any<Guid>())
        .Returns(new MedicalCaseDto { Step1CompletedAt = DateTime.Now });

    var viewModel = new ConsultationFormViewModel(mockRepository, mockLogger)
    {
        MedicalCaseId = Guid.NewGuid(),
        ChiefComplaint = "测试主诉",
        TCMDiagnosis = "测试诊断"
    };

    // Act
    await viewModel.CompleteStep1Command.ExecuteAsync();

    // Assert
    await mockRepository.Received(1).CompleteStep1Async(Arg.Any<Guid>());
    Assert.NotNull(viewModel.Step1CompletedAt);
}
```

---

## 📚 参考资料

### 架构文档
- [Client端诊疗管理架构设计](../../explanation/architecture/client/consultation-design.md)
- [Client端MVVM架构指南](../../explanation/architecture/client/README.md)
- [三步工作流设计](../../explanation/architecture/client/workflow-design.md)

### 代码参考
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationFormView.xaml`
- `src/Client/Desktop/Core/LYBT.Desktop.Core/UnifiedViewModelBase.cs`

### 业务规则
- **BF-002**: 诊断必填项验证（主诉+中医诊断）
- **REQ-001**: 三步工作流优化（Step1 → Step2 → Step3）
- **Issue #1563**: Consultation聚合根整合（移除IConsultationRepository）

### 相关Issue
- **Epic #1343**: MVP核心功能实现
- **Issue #1562**: 工作流事件精简（Phase 1）
- **Issue #1606**: MedicalCase聚合根边界重构

---

**最后更新**: 2025-01-30
**维护负责**: Client端开发组
**文档状态**: ✅ 完整版
