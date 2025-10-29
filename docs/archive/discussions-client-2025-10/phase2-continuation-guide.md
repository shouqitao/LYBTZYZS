# Phase 2继续指南 - 患者选择迁移

**Issue**: #1557 - 看诊流程模块化迁移
**Phase**: Phase 2 - 患者选择迁移（Step 1）
**分支**: `refactor/medical-workflow-module-migration-phase2-issue-1557`
**创建时间**: 2025-10-21
**会话断点**: 文件迁移前置准备完成

---

## 📊 当前进度摘要

### ✅ 已完成工作

1. **架构设计文档** ✅
   - 📄 `docs/explanation/architecture/shared/medical-workflow-module-migration-discussion.md` - 架构分析和方案对比
   - 📄 `docs/explanation/architecture/client/medical-workflow-events-contract.md` - 事件聚合器契约（5个事件）
   - 📄 `docs/explanation/architecture/client/medical-workflow-navigation-parameters.md` - NavigationParameters规范

2. **GitHub Issue创建** ✅
   - 🔗 Issue #1557: https://github.com/shouqitao/LYBTZYZS/issues/1557

3. **代码审查** ✅
   - ✅ 审查了 `MedicalCaseFlowViewModel.cs`（532行）
   - ✅ 审查了 `PatientSelectionViewModel.cs`（478行）
   - ✅ 审查了 `PatientSelectionView.xaml`（252行）

4. **Git分支** ✅
   - ✅ 创建分支：`refactor/medical-workflow-module-migration-phase2-issue-1557`
   - ✅ 当前在分支上（无提交）

### 🔄 待执行工作（下一会话）

1. **创建事件定义文件**（预计10分钟）
2. **迁移PatientSelectionView**（预计15分钟）
3. **迁移PatientSelectionViewModel**（预计20分钟）
4. **更新PatientsModule注册**（预计10分钟）
5. **更新MedicalCaseFlowViewModel**（预计20分钟）
6. **编译测试**（预计15分钟）

**预计总时长**: 1.5小时

---

## 🔍 代码审查关键发现

### 1. MedicalCaseFlowViewModel 当前实现分析

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

**关键问题点**:

#### 问题1: 直接实例化子步骤ViewModel（405-428行）
```csharp
case FlowStep.SelectPatient:
    var patientSelectionViewModel = _containerProvider.Resolve<PatientSelectionViewModel>();

    // ❌ 使用事件回调，耦合度高
    patientSelectionViewModel.PatientSelected += async (sender, selectedPatient) =>
    {
        CurrentPatient = selectedPatient;
        NextStepCommand.RaiseCanExecuteChanged();
        SelectedPatientName = selectedPatient.Name;
        SelectedPatientInfo = $"{selectedPatient.Gender} | {selectedPatient.Age}岁 | {selectedPatient.PhoneNumber}";
        await ExecuteNextStepAsync();
    };

    CurrentStepViewModel = patientSelectionViewModel;
```

**需要改为**: Region导航 + EventAggregator订阅

#### 问题2: ContentControl直接绑定ViewModel（MedicalCaseFlowView.xaml:256行）
```xaml
<ContentControl Content="{Binding CurrentStepViewModel}" />
```

**需要改为**: Region容器
```xaml
<ContentControl prism:RegionManager.RegionName="WorkflowContentRegion" />
```

### 2. PatientSelectionViewModel 当前实现分析

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs`

**关键问题点**:

#### 问题1: 使用.NET事件而非Prism EventAggregator（126行）
```csharp
/// <summary>
/// 患者选择事件（通知父ViewModel创建MedicalCase）
/// </summary>
public event EventHandler<PatientDto>? PatientSelected;  // ❌ 需要改为EventAggregator
```

#### 问题2: 触发事件的位置（272行、237行、303行）
```csharp
// ExecuteSelectPatient
PatientSelected?.Invoke(this, SelectedPatient);

// ExecuteNewPatient
PatientSelected?.Invoke(this, newPatient);

// ExecuteDoubleClickPatient
PatientSelected?.Invoke(this, patient);
```

**需要改为**: 发布 `PatientSelectedEvent`

#### 问题3: 命名空间（1-12行）
```csharp
namespace LYBT.Desktop.MedicalCase.ViewModels  // ❌ 错误的命名空间
```

**需要改为**: `namespace LYBT.Desktop.Patients.ViewModels`

### 3. PatientSelectionView 当前实现分析

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.xaml`

**关键问题点**:

#### 问题1: x:Class命名空间（1行）
```xaml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.PatientSelectionView"
```

**需要改为**: `LYBT.Desktop.Patients.Views.PatientSelectionView`

#### 问题2: CodeBehind文件
需要创建对应的 `.xaml.cs` 文件

---

## 📋 详细实施步骤（按顺序执行）

### Step 1: 创建事件定义文件（Core模块）

**新建文件**: `src/Client/Desktop/Core/LYBT.Desktop.Core/Events/PatientSelectedEvent.cs`

```csharp
using Prism.Events;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 患者选择完成事件
    /// Issue #1557 - 看诊流程模块化迁移
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientSelectedPayload>
    {
    }

    /// <summary>
    /// 患者选择事件载荷
    /// </summary>
    public class PatientSelectedPayload
    {
        /// <summary>
        /// 选中的患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名（用于显示在患者信息条）
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 患者性别
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 患者年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 患者联系电话
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 医案流程ID（由流程协调器传入）
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

**验证**: 确保 `LYBT.Desktop.Core.csproj` 已引用 `Prism.Events`

---

### Step 2: 在Patients模块创建PatientSelectionView.xaml

**新建文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`

**内容**: 复制 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.xaml`

**修改点**:
1. 第1行：`x:Class="LYBT.Desktop.Patients.Views.PatientSelectionView"`（改命名空间）
2. 其余内容保持不变

---

### Step 3: 创建PatientSelectionView.xaml.cs

**新建文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml.cs`

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Patients.Views
{
    /// <summary>
    /// PatientSelectionView.xaml 的交互逻辑
    /// Issue #1557 - 看诊流程模块化迁移
    /// </summary>
    public partial class PatientSelectionView : UserControl
    {
        public PatientSelectionView()
        {
            InitializeComponent();
        }
    }
}
```

---

### Step 4: 迁移PatientSelectionViewModel到Patients模块

**新建文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`

**内容**: 复制 `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs`

**修改点**:

#### 修改1: 命名空间（12行）
```csharp
// ❌ 旧命名空间
namespace LYBT.Desktop.MedicalCase.ViewModels

// ✅ 新命名空间
namespace LYBT.Desktop.Patients.ViewModels
```

#### 修改2: 添加using（顶部）
```csharp
using LYBT.Desktop.Core.Events;  // 新增，引用PatientSelectedEvent
```

#### 修改3: 删除旧事件定义（121-127行）
```csharp
// ❌ 删除这段
#region 事件

/// <summary>
/// 患者选择事件（通知父ViewModel创建MedicalCase）
/// </summary>
public event EventHandler<PatientDto>? PatientSelected;

#endregion
```

#### 修改4: 添加MedicalCaseFlowId属性（32行后）
```csharp
private Guid _medicalCaseFlowId = Guid.Empty;
/// <summary>
/// 医案流程ID（从NavigationParameters接收）
/// </summary>
public Guid MedicalCaseFlowId
{
    get => _medicalCaseFlowId;
    set => SetProperty(ref _medicalCaseFlowId, value);
}
```

#### 修改5: ExecuteSelectPatient方法（259-278行）
```csharp
/// <summary>
/// 选择患者（点击【选择】按钮）
/// </summary>
private void ExecuteSelectPatient()
{
    if (SelectedPatient == null)
    {
        Logger.LogWarning("未选择患者");
        return;
    }

    try
    {
        Logger.LogInformation("选择患者：{PatientName}（ID: {PatientId}）", SelectedPatient.Name, SelectedPatient.Id);

        // ✅ 发布PatientSelectedEvent
        var payload = new PatientSelectedPayload
        {
            PatientId = SelectedPatient.Id,
            PatientName = SelectedPatient.Name,
            Gender = SelectedPatient.Gender,
            Age = SelectedPatient.Age,
            PhoneNumber = SelectedPatient.PhoneNumber,
            MedicalCaseFlowId = this.MedicalCaseFlowId,
            Timestamp = DateTime.Now
        };

        _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);

        Logger.LogInformation("PatientSelectedEvent已发布");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "选择患者失败");
    }
}
```

#### 修改6: ExecuteNewPatient方法（237行）
```csharp
// 原来的代码（237行）
PatientSelected?.Invoke(this, newPatient);

// ✅ 改为
var payload = new PatientSelectedPayload
{
    PatientId = newPatient.Id,
    PatientName = newPatient.Name,
    Gender = newPatient.Gender,
    Age = newPatient.Age,
    PhoneNumber = newPatient.PhoneNumber,
    MedicalCaseFlowId = this.MedicalCaseFlowId,
    Timestamp = DateTime.Now
};
_eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);
```

#### 修改7: ExecuteDoubleClickPatient方法（303行）
```csharp
// 原来的代码（303行）
PatientSelected?.Invoke(this, patient);

// ✅ 改为
var payload = new PatientSelectedPayload
{
    PatientId = patient.Id,
    PatientName = patient.Name,
    Gender = patient.Gender,
    Age = patient.Age,
    PhoneNumber = patient.PhoneNumber,
    MedicalCaseFlowId = this.MedicalCaseFlowId,
    Timestamp = DateTime.Now
};
_eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);
```

#### 修改8: OnNavigatedTo方法（436-461行）
```csharp
public override void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);

    try
    {
        // ✅ 接收MedicalCaseFlowId参数
        MedicalCaseFlowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
        Logger.LogInformation("接收到MedicalCaseFlowId: {MedicalCaseFlowId}", MedicalCaseFlowId);

        // 接收HomeView传来的搜索关键字
        var searchKeyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
        if (!string.IsNullOrEmpty(searchKeyword))
        {
            Logger.LogInformation("接收到搜索关键字：{SearchKeyword}", searchKeyword);
            SearchKeyword = searchKeyword;
            _ = ExecuteSearchAsync();
        }
        else
        {
            _ = LoadInitialPatientsAsync();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "导航到患者选择视图时发生异常");
    }
}
```

---

### Step 5: 在PatientsModule注册Region View

**修改文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`

**查找**: `RegisterTypes` 方法

**添加**:
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 现有注册...

    // ✅ 新增：注册患者选择View用于Region导航
    containerRegistry.RegisterForNavigation<Views.PatientSelectionView, ViewModels.PatientSelectionViewModel>();

    Logger.LogInformation("PatientSelectionView已注册用于Region导航");
}
```

---

### Step 6: 更新MedicalCaseFlowView添加Region容器

**修改文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`

**查找**（253-265行）:
```xaml
<!-- Row 3: 主内容区（ContentControl动态绑定） -->
<Border Grid.Row="3" Background="White" Margin="0">
    <Grid>
        <ContentControl Content="{Binding CurrentStepViewModel}" />

        <!-- 占位文本（各Step View未实现时显示） -->
        <TextBlock Text="{Binding CurrentStep}"
                  FontSize="24"
                  Foreground="#999"
                  HorizontalAlignment="Center"
                  VerticalAlignment="Center"
                  Visibility="{Binding CurrentStepViewModel, Converter={StaticResource BoolToVisibility}}" />
    </Grid>
</Border>
```

**替换为**:
```xaml
<!-- Row 3: 主内容区（Region容器） -->
<Border Grid.Row="3" Background="White" Margin="0">
    <ContentControl prism:RegionManager.RegionName="WorkflowContentRegion" />
</Border>
```

---

### Step 7: 更新MedicalCaseFlowViewModel使用Region导航

**修改文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

#### 修改1: 添加using（1-10行）
```csharp
using LYBT.Desktop.Core.Events;  // 新增
```

#### 修改2: 删除CurrentStepViewModel属性（53-61行）
```csharp
// ❌ 删除这个属性（不再需要）
private ViewModelBase? _currentStepViewModel;
public ViewModelBase? CurrentStepViewModel
{
    get => _currentStepViewModel;
    set => SetProperty(ref _currentStepViewModel, value);
}
```

#### 修改3: 构造函数添加事件订阅（141-163行）
```csharp
public MedicalCaseFlowViewModel(
    IRegionManager regionManager,
    IContainerProvider containerProvider,
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory)
    : base(eventAggregator, loggerFactory, regionManager)
{
    _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));

    // ✅ 订阅PatientSelectedEvent
    _eventAggregator.GetEvent<PatientSelectedEvent>()
        .Subscribe(OnPatientSelected, ThreadOption.UIThread);

    // 初始化命令
    BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
    PreviousStepCommand = new DelegateCommand(ExecutePreviousStep, CanExecutePreviousStep);
    NextStepCommand = new DelegateCommand(async () => await ExecuteNextStepAsync(), CanExecuteNextStep)
        .ObservesProperty(() => CurrentPatient)
        .ObservesProperty(() => IsBusy);
    SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
    CancelCommand = new DelegateCommand(ExecuteCancel);

    Logger.LogInformation("MedicalCaseFlowViewModel已初始化，当前步骤：{CurrentStep}", CurrentStep);
}
```

#### 修改4: 添加PatientSelected事件处理方法（在构造函数后）
```csharp
#region 事件处理

/// <summary>
/// 处理患者选择事件
/// </summary>
private async void OnPatientSelected(PatientSelectedPayload payload)
{
    try
    {
        Logger.LogInformation("收到PatientSelectedEvent，患者：{PatientName}（ID: {PatientId}）",
            payload.PatientName, payload.PatientId);

        // 创建临时PatientDto对象（用于向后兼容）
        CurrentPatient = new PatientDto
        {
            Id = payload.PatientId,
            Name = payload.PatientName,
            Gender = payload.Gender,
            Age = payload.Age,
            PhoneNumber = payload.PhoneNumber
        };

        // 触发NextStepCommand状态刷新
        NextStepCommand.RaiseCanExecuteChanged();

        // 更新患者信息条
        SelectedPatientName = payload.PatientName;
        SelectedPatientInfo = $"{payload.Gender} | {payload.Age}岁 | {payload.PhoneNumber}";

        // 自动执行下一步（创建MedicalCase并跳转到Step 2）
        await ExecuteNextStepAsync();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "处理PatientSelectedEvent失败");
    }
}

#endregion
```

#### 修改5: NavigateToStep方法（394-491行）
```csharp
/// <summary>
/// 导航到指定步骤
/// </summary>
private void NavigateToStep(FlowStep step)
{
    CurrentStep = step;

    switch (step)
    {
        case FlowStep.SelectPatient:
            Logger.LogInformation("导航到患者选择步骤");

            // ✅ 使用Region导航
            var parameters = new NavigationParameters
            {
                { "MedicalCaseFlowId", MedicalCaseId == Guid.Empty ? Guid.NewGuid() : MedicalCaseId },
                { "FlowContext", "NewMedicalCase" }
            };

            _regionManager.RequestNavigate("WorkflowContentRegion", "PatientSelectionView", parameters);
            Logger.LogInformation("Region导航到PatientSelectionView");
            break;

        case FlowStep.FillConsultation:
            Logger.LogInformation("导航到诊断录入步骤");
            // ⏳ 保持原有逻辑（Phase 3实施）
            // TODO: Phase 3 - 迁移到Consultation模块
            var consultationFormViewModelType = Type.GetType("LYBT.Desktop.Consultation.ViewModels.ConsultationFormViewModel, LYBT.Desktop.Consultation");
            if (consultationFormViewModelType != null)
            {
                var consultationFormViewModel = _containerProvider.Resolve(consultationFormViewModelType) as ViewModelBase;
                if (consultationFormViewModel != null)
                {
                    var currentPatientProperty = consultationFormViewModelType.GetProperty("CurrentPatient");
                    var medicalCaseIdProperty = consultationFormViewModelType.GetProperty("MedicalCaseId");

                    currentPatientProperty?.SetValue(consultationFormViewModel, CurrentPatient);
                    medicalCaseIdProperty?.SetValue(consultationFormViewModel, MedicalCaseId);

                    // ❌ 临时保留ContentControl绑定（Phase 3会移除）
                    // CurrentStepViewModel = consultationFormViewModel;
                    Logger.LogInformation("ConsultationFormViewModel已创建，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                }
            }
            break;

        case FlowStep.FillPrescription:
            Logger.LogInformation("导航到处方编辑步骤");
            // ⏳ 保持原有逻辑（Phase 4实施）
            break;

        case FlowStep.CompleteMedicalCase:
            Logger.LogInformation("导航到完成医案步骤");
            // ⏳ 保持原有逻辑（Phase 5实施）
            break;

        default:
            Logger.LogWarning("未知步骤：{Step}", step);
            break;
    }
}
```

#### 修改6: 添加Destroy方法（取消订阅）
```csharp
public override void Destroy()
{
    // 取消事件订阅
    _eventAggregator.GetEvent<PatientSelectedEvent>().Unsubscribe(OnPatientSelected);

    base.Destroy();
    Logger.LogInformation("MedicalCaseFlowViewModel已销毁");
}
```

---

### Step 8: 编译验证

**执行命令**:
```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

**期望结果**: 0 errors, 0 warnings

**如果有错误**:
1. 检查命名空间引用
2. 检查using语句
3. 检查Region名称一致性
4. 检查事件Payload属性名

---

## ⚠️ 注意事项

### 1. 不要删除旧文件
Phase 2只进行"新增"和"修改"，**不删除** `MedicalCase` 模块中的旧文件：
- ❌ 不要删除 `MedicalCase/Views/PatientSelectionView.xaml`
- ❌ 不要删除 `MedicalCase/ViewModels/PatientSelectionViewModel.cs`

**原因**: 保留作为过渡期兼容，Phase 6最后统一清理

### 2. Region名称必须一致
- `MedicalCaseFlowView.xaml` 中的 Region: `"WorkflowContentRegion"`
- `MedicalCaseFlowViewModel.cs` 中的导航: `"WorkflowContentRegion"`
- 必须完全一致（区分大小写）

### 3. 事件订阅线程安全
```csharp
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);  // ✅ 必须指定UIThread
```

### 4. NavigationParameters键名一致
- `MedicalCaseFlowViewModel` 发送: `"MedicalCaseFlowId"`
- `PatientSelectionViewModel` 接收: `"MedicalCaseFlowId"`
- 必须完全一致（区分大小写）

---

## 📁 文件清单

### 需要创建的文件（4个）
1. `src/Client/Desktop/Core/LYBT.Desktop.Core/Events/PatientSelectedEvent.cs`
2. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`
3. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml.cs`
4. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`

### 需要修改的文件（3个）
1. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`
2. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`
3. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

---

## 🚀 开始新会话的第一句话

建议在新会话中这样开始：

```
请继续Phase 2的患者选择迁移工作。
参考文档：docs/architecture/client/phase2-continuation-guide.md
当前分支：refactor/medical-workflow-module-migration-phase2-issue-1557
请按照指南中的Step 1-8依次执行。
```

---

**文档状态**: ✅ 准备完成
**下一步**: 新会话中执行Step 1-8
