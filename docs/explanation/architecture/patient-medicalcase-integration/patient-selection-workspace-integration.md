# 患者选择与看诊工作台集成方案

**Patient Selection & Medical Case Workspace Integration Design**

本文档详细说明LYBTZYZS系统中患者选择模块与看诊工作台模块的集成架构设计、技术决策和实施计划。

---

## 目录

1. [方案概述](#方案概述)
2. [当前实现分析](#当前实现分析)
3. [集成架构设计](#集成架构设计)
4. [详细实施计划](#详细实施计划)
5. [技术决策记录](#技术决策记录)
6. [风险评估与缓解](#风险评估与缓解)

---

## 方案概述

### 业务背景

患者选择和看诊流程是中医诊所临床工作的核心环节,两者应该是紧密集成的单一业务流程,而非两个独立模块。

**核心诉求**:
- 医生从患者列表选择患者后,应该直接进入看诊工作台
- 如果患者有未完成的病历,需要智能检测并提供选项(继续/新建/仅关闭)
- 看诊工作台应该是单一视图,诊断和处方编辑同屏进行
- 简化操作流程,减少界面跳转,提升工作效率

### 设计目标

1. **流程简化**: 从3步流程(患者选择→诊断→处方)简化为单一工作台
2. **最小化修改**: 复用现有PatientSelectionViewModel的逻辑,仅修改导航目标
3. **数据一致性**: 医案创建时机明确,参数传递清晰
4. **组件化架构**: 诊断面板和处方面板作为独立UserControl,便于复用

---

## 当前实现分析

### PatientSelectionViewModel核心流程

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`

**执行流程**:

```
ExecuteStartConsultation()
  ↓
CheckUnfinishedMedicalCaseAsync(patientId)
  ↓
┌─ 有未完成病历 ───────────────────┐
│ HandleUnfinishedCaseAsync()      │
│   ↓                               │
│ ShowUnfinishedCaseDialogAsync()  │
│ (3选项对话框)                     │
│   ├─ 选项1: 继续看诊              │
│   │   → ContinueConsultationAsync│
│   │   → 传递MedicalCaseId        │
│   ├─ 选项2: 新建医案              │
│   │   → CreateNewCaseAfterClosing│
│   │   → 先关闭旧案,传Empty       │
│   └─ 选项3: 仅关闭医案            │
│       → CloseOldCaseOnlyAsync    │
└──────────────────────────────────┘
           或
┌─ 无未完成病历 ────────────────┐
│ HandleNoUnfinishedCase()      │
│   → PublishPatientSelectedEvent│
│   → 传递Empty MedicalCaseId   │
└───────────────────────────────┘
           ↓
NavigateToMedicalCaseFlow(patient, medicalCaseId)
  → RegionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters)
```

**关键代码** (Line 836-853):

```csharp
private void NavigateToMedicalCaseFlow(PatientDto patient, Guid? medicalCaseId)
{
    var parameters = new NavigationParameters { { "CurrentPatient", patient } };

    if (medicalCaseId.HasValue && medicalCaseId.Value != Guid.Empty)
    {
        parameters.Add("MedicalCaseId", medicalCaseId.Value);
        Logger.LogInformation("导航到医案录入界面(继续看诊): MedicalCaseId={0}", medicalCaseId.Value);
    }
    else
    {
        Logger.LogInformation("导航到医案录入界面(新建医案): PatientId={0}", patient.Id);
    }

    RegionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
    // ^^^ 需要修改为: "MedicalCaseWorkspaceView"
}
```

### MedicalCaseFlowViewModel核心流程 (旧方案)

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

**OnNavigatedTo流程** (Line 577-601):

```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);

    // 接收导航参数
    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
    CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

    // 初始化患者信息和医案
    var initializationFailed = await InitializePatientInfoAsync();
    if (initializationFailed) return;

    // 加载继续看诊的医案数据
    await LoadMedicalCaseDetailsAsync(navigationContext);

    // 默认导航到Step 1 (辨证)
    NavigateToStep(ConsultationStep.Consultation);
}
```

**医案创建逻辑** (Line 617-645):

```csharp
// 在InitializePatientInfoAsync中
if (MedicalCaseId == Guid.Empty)
{
    SetIsBusy(true, "正在创建医案...");

    // 委托给LifecycleHandler创建医案
    MedicalCaseId = await CreateMedicalCaseAsync(CurrentPatient.Id);

    if (MedicalCaseId == Guid.Empty)
    {
        await ShowErrorMessageAsync("创建医案失败,请重试");
        return true; // 失败,需要中止
    }

    Logger.LogInformation("MedicalCase创建成功: {0}", MedicalCaseId);
}
```

### 关键发现

1. **医案创建位置**: 在工作台ViewModel的OnNavigatedTo中,而非患者选择时
2. **创建时机信号**: MedicalCaseId == Guid.Empty
3. **参数传递**: CurrentPatient(必传) + MedicalCaseId(可选)
4. **未完成病历检测**: 已完整实现,逻辑成熟可复用

---

## 集成架构设计

### 整体导航流程

```
┌─────────────────┐
│ ClinicalHomeView│
│  (医生工作台主页)│
└────────┬────────┘
         │ [开始接诊]
         ↓
┌──────────────────────┐
│ PatientSelectionView │
│ ┌──────────────────┐ │
│ │ 患者列表(搜索)    │ │
│ │ 待诊队列(草稿)    │ │
│ └──────────────────┘ │
└────────┬─────────────┘
         │ [选择患者/双击]
         ↓
┌──────────────────────┐
│ 未完成病历检测        │
│ UnfinishedCaseHandler│
└────────┬─────────────┘
         │
    ┌────┴────┐
    │         │
   有草稿    无草稿
    │         │
    ↓         ↓
┌─────────┐ 直接导航
│3选项对话框│
│ 1.继续   │
│ 2.新建   │
│ 3.仅关闭 │
└────┬────┘
     │ 用户选择
     ↓
┌────────────────────────────────┐
│ MedicalCaseWorkspaceView       │
│ (看诊工作台 - 单一视图)         │
│ ┌────────────────────────────┐ │
│ │ 顶部: 患者信息栏(只读)      │ │
│ └────────────────────────────┘ │
│ ┌──────────┬─────────────────┐ │
│ │ 左40%    │ 右60%           │ │
│ │诊断面板   │ 处方编辑面板     │ │
│ │Consultation│ Prescription   │ │
│ │Panel     │ EditorPanel     │ │
│ └──────────┴─────────────────┘ │
│ ┌────────────────────────────┐ │
│ │ 底部: 4按钮                 │ │
│ │ [取消看诊] [保存医案]       │ │
│ │ [打印处方笺] [完成看诊]     │ │
│ └────────────────────────────┘ │
└────────────────────────────────┘
```

### 导航参数传递机制

**NavigationParameters结构**:

```csharp
// PatientSelectionViewModel.NavigateToMedicalCaseFlow
var parameters = new NavigationParameters
{
    { "CurrentPatient", patient },  // PatientDto对象 (必传)
    { "MedicalCaseId", medicalCaseId }  // Guid (可选, Empty=新建, 有值=继续)
};

RegionManager.RequestNavigate("ContentRegion", "MedicalCaseWorkspaceView", parameters);
```

**MedicalCaseWorkspaceViewModel.OnNavigatedTo接收**:

```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);

    // 接收参数
    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
    CurrentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

    // 判断医案创建或加载
    if (MedicalCaseId == Guid.Empty)
    {
        // 新建医案场景
        MedicalCaseId = await CreateMedicalCaseAsync(CurrentPatient.Id);
    }
    else
    {
        // 继续看诊场景
        await LoadMedicalCaseDetailsAsync(MedicalCaseId);
    }

    // 初始化两个子面板
    InitializeConsultationPanel();
    InitializePrescriptionEditorPanel();
}
```

### MedicalCaseWorkspaceView架构

**XAML布局结构**:

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseWorkspaceView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" /> <!-- 患者信息栏 -->
            <RowDefinition Height="*" />    <!-- 主内容区 -->
            <RowDefinition Height="Auto" /> <!-- 底部按钮栏 -->
        </Grid.RowDefinitions>

        <!-- Row 0: 患者信息栏 -->
        <Border Grid.Row="0" Background="White" Padding="20">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding PatientName}" FontSize="18" FontWeight="Bold" />
                <TextBlock Text="{Binding PatientInfo}" Margin="20,0,0,0" />
            </StackPanel>
        </Border>

        <!-- Row 1: 主内容区 (左右分栏) -->
        <Grid Grid.Row="1" Margin="20">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*" /> <!-- 左侧40% 诊断 -->
                <ColumnDefinition Width="20" /> <!-- 间隔 -->
                <ColumnDefinition Width="3*" /> <!-- 右侧60% 处方 -->
            </Grid.ColumnDefinitions>

            <!-- 左: 诊断面板 (UserControl) -->
            <Border Grid.Column="0" Background="White" CornerRadius="8">
                <controls:ConsultationPanel
                    MedicalCaseId="{Binding MedicalCaseId}"
                    CurrentPatient="{Binding CurrentPatient}"
                    ConsultationDataChanged="OnConsultationDataChanged"/>
            </Border>

            <!-- 右: 处方编辑面板 (UserControl) -->
            <Border Grid.Column="2" Background="White" CornerRadius="8">
                <controls:PrescriptionEditorPanel
                    MedicalCaseId="{Binding MedicalCaseId}"
                    PrescriptionDataChanged="OnPrescriptionDataChanged"/>
            </Border>
        </Grid>

        <!-- Row 2: 底部按钮栏 -->
        <Border Grid.Row="2" Background="White" Padding="20">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="取消看诊" Command="{Binding CancelConsultationCommand}"
                        Style="{StaticResource SecondaryButtonStyle}" Margin="0,0,10,0"/>
                <Button Content="保存医案" Command="{Binding SaveMedicalCaseCommand}"
                        Style="{StaticResource SecondaryButtonStyle}" Margin="0,0,10,0"/>
                <Button Content="打印处方笺" Command="{Binding PrintPrescriptionCommand}"
                        Style="{StaticResource SecondaryButtonStyle}" Margin="0,0,10,0"/>
                <Button Content="完成看诊" Command="{Binding CompleteConsultationCommand}"
                        Style="{StaticResource PrimaryButtonStyle}"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

### MedicalCaseWorkspaceViewModel职责

**组件依赖**:

```csharp
public class MedicalCaseWorkspaceViewModel : NavigationViewModelBase
{
    private readonly IRegionManager _regionManager;
    private readonly MedicalCaseDataManager _medicalCaseDataManager;
    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly ConsultationDataManager _consultationDataManager;
    private readonly PrescriptionDataManager _prescriptionDataManager;
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler;

    // 导航参数
    public Guid MedicalCaseId { get; private set; }
    public PatientDto CurrentPatient { get; private set; }

    // 子面板引用
    private ConsultationPanel _consultationPanel;
    private PrescriptionEditorPanel _prescriptionEditorPanel;

    // 命令
    public DelegateCommand CancelConsultationCommand { get; }
    public DelegateCommand SaveMedicalCaseCommand { get; }
    public DelegateCommand PrintPrescriptionCommand { get; }
    public DelegateCommand CompleteConsultationCommand { get; }
}
```

**4个按钮命令实现**:

| 按钮 | 命令 | 核心逻辑 |
|------|------|---------|
| 取消看诊 | CancelConsultationCommand | 1. 确认对话框<br>2. 新建医案→删除(DeleteAsync)<br>3. 继续医案→保留(Draft状态)<br>4. 导航回PatientSelectionView |
| 保存医案 | SaveMedicalCaseCommand | 1. 获取ConsultationPanel数据<br>2. 获取PrescriptionPanel数据<br>3. 调用SaveConsultationAsync<br>4. 调用SavePrescriptionAsync<br>5. 医案状态保持Draft<br>6. 提示"医案已保存" |
| 打印处方笺 | PrintPrescriptionCommand | 1. 验证处方数据非空<br>2. 自动保存医案(如有未保存)<br>3. 调用打印服务生成PDF<br>4. 打开打印对话框 |
| 完成看诊 | CompleteConsultationCommand | 1. 验证必填数据(诊断或处方)<br>2. 保存所有数据<br>3. 更新医案状态Draft→Completed<br>4. 清理缓存(UnfinishedCaseHandler)<br>5. 导航回PatientSelectionView |

### ConsultationPanel设计 (诊断面板UserControl)

**依赖属性**:

```csharp
// MedicalCaseId - 医案ID(必须)
public static readonly DependencyProperty MedicalCaseIdProperty =
    DependencyProperty.Register("MedicalCaseId", typeof(Guid),
        typeof(ConsultationPanel), new PropertyMetadata(Guid.Empty, OnMedicalCaseIdChanged));

// CurrentPatient - 患者信息(可选,用于显示)
public static readonly DependencyProperty CurrentPatientProperty =
    DependencyProperty.Register("CurrentPatient", typeof(PatientDto),
        typeof(ConsultationPanel), new PropertyMetadata(null));

// ConsultationData - 诊断数据(可选,继续看诊时传入)
public static readonly DependencyProperty ConsultationDataProperty =
    DependencyProperty.Register("ConsultationData", typeof(ConsultationDto),
        typeof(ConsultationPanel), new PropertyMetadata(null, OnConsultationDataChanged));
```

**事件**:

```csharp
// 数据变更事件
public event EventHandler<ConsultationDataChangedEventArgs> ConsultationDataChanged;

// IsDirty标记属性
public bool IsDirty { get; private set; }
```

**公开方法**:

```csharp
// 获取当前诊断数据
public ConsultationDto GetConsultationData();

// 验证数据有效性
public bool ValidateData();

// 重置面板(清空数据)
public void Reset();
```

### PrescriptionEditorPanel设计 (处方编辑面板UserControl)

**依赖属性**:

```csharp
// MedicalCaseId - 医案ID(必须)
public static readonly DependencyProperty MedicalCaseIdProperty =
    DependencyProperty.Register("MedicalCaseId", typeof(Guid),
        typeof(PrescriptionEditorPanel), new PropertyMetadata(Guid.Empty));

// PrescriptionData - 处方数据(可选,继续看诊时传入)
public static readonly DependencyProperty PrescriptionDataProperty =
    DependencyProperty.Register("PrescriptionData", typeof(PrescriptionDto),
        typeof(PrescriptionEditorPanel), new PropertyMetadata(null));

// ConsultationData - 诊断数据(可选,用于根据诊断推荐用药)
public static readonly DependencyProperty ConsultationDataProperty =
    DependencyProperty.Register("ConsultationData", typeof(ConsultationDto),
        typeof(PrescriptionEditorPanel), new PropertyMetadata(null));
```

**事件**:

```csharp
// 数据变更事件
public event EventHandler<PrescriptionDataChangedEventArgs> PrescriptionDataChanged;

// IsDirty标记
public bool IsDirty { get; private set; }
```

**公开方法**:

```csharp
// 获取当前处方数据
public PrescriptionDto GetPrescriptionData();

// 验证数据有效性
public bool ValidateData();

// 重置面板
public void Reset();
```

---

## 详细实施计划

### Phase 0: Bug修复与准备 (优先级: P0)

**任务清单**:

1. ✅ **修复MedicalCaseDataManager.CreateAsync返回null的bug**
   - 位置: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseDataManager.cs`
   - 问题: CreateAsync方法可能返回null,导致MedicalCaseId为Empty
   - 验证: 添加单元测试确保正确返回Guid

2. ✅ **阅读现有ConsultationFormView和PrescriptionEditorView的XAML**
   - 目的: 了解UI结构,便于改造为UserControl
   - 文件: `ConsultationFormView.xaml`, `PrescriptionEditorView.xaml`

3. ✅ **验证UnfinishedCaseHandler的缓存管理逻辑**
   - 确保检测、关闭、清理逻辑正确
   - 测试3选项对话框的所有分支

**验收标准**:
- [ ] CreateAsync单元测试通过
- [ ] 能正确创建医案并返回有效Guid
- [ ] UnfinishedCaseHandler所有分支测试通过

---

### Phase 1: 工作台骨架实现 (优先级: P0)

**任务清单**:

1. **创建MedicalCaseWorkspaceView.xaml**
   - 3行布局: 患者信息栏 + 主内容区 + 按钮栏
   - 主内容区暂时放置空白Border(占位)
   - 4个按钮: 取消看诊/保存医案/打印处方笺/完成看诊

2. **创建MedicalCaseWorkspaceViewModel.cs**
   - 继承NavigationViewModelBase
   - 实现INavigationAware接口
   - OnNavigatedTo: 接收参数 + 医案创建/加载逻辑
   - 4个Command: 暂时只打印日志,不实现实际功能

3. **修改PatientSelectionViewModel.cs**
   - Line 852: `"MedicalCaseFlowView"` → `"MedicalCaseWorkspaceView"`

4. **注册到MedicalCaseModule.cs**
   - RegisterForNavigation\<MedicalCaseWorkspaceView\>()

5. **端到端测试**
   - 从ClinicalHomeView → 开始接诊 → 选择患者 → 进入工作台
   - 验证参数传递正确
   - 验证医案创建成功
   - 验证继续看诊加载数据

**验收标准**:
- [ ] 导航流程通畅,无异常
- [ ] 新建医案时MedicalCaseId正确生成
- [ ] 继续看诊时MedicalCaseId正确传递
- [ ] 患者信息正确显示

**代码位置**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs:852`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`

---

### Phase 2: 诊断面板集成 (优先级: P1)

**任务清单**:

1. **创建ConsultationPanel.xaml.cs (Code-behind)**
   - 定义3个DependencyProperty: MedicalCaseId, CurrentPatient, ConsultationData
   - 定义ConsultationDataChanged事件
   - 实现GetConsultationData()方法
   - 实现ValidateData()方法

2. **将ConsultationFormView.xaml的UI复制到ConsultationPanel.xaml**
   - 删除顶部导航栏(工作台不需要)
   - 删除底部按钮栏(工作台统一管理)
   - 保留核心四诊录入区域

3. **创建ConsultationPanelViewModel (可选)**
   - 如果逻辑复杂,可以创建独立ViewModel
   - 使用ViewModelLocator或手动绑定

4. **集成到MedicalCaseWorkspaceView**
   - 左侧Border内放置ConsultationPanel
   - 绑定MedicalCaseId和CurrentPatient
   - 订阅ConsultationDataChanged事件

5. **实现SaveMedicalCaseCommand的Consultation保存逻辑**
   - 调用ConsultationPanel.GetConsultationData()
   - 调用ConsultationDataManager.SaveAsync()
   - 错误处理和提示

**验收标准**:
- [ ] 诊断面板正确显示
- [ ] 四诊信息可以录入和保存
- [ ] 继续看诊时正确加载现有诊断数据
- [ ] [保存医案]按钮可以保存诊断信息

**代码位置**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`

---

### Phase 3: 处方面板集成 (优先级: P1)

**任务清单**:

1. **创建PrescriptionEditorPanel.xaml.cs**
   - 定义DependencyProperty: MedicalCaseId, PrescriptionData
   - 定义PrescriptionDataChanged事件
   - 实现GetPrescriptionData()方法

2. **将PrescriptionEditorView.xaml的UI复制到PrescriptionEditorPanel.xaml**
   - 删除顶部导航栏
   - 删除底部按钮栏
   - 保留核心药材选择和配置区域

3. **集成到MedicalCaseWorkspaceView**
   - 右侧Border内放置PrescriptionEditorPanel
   - 绑定MedicalCaseId
   - 订阅PrescriptionDataChanged事件

4. **实现SaveMedicalCaseCommand的Prescription保存逻辑**
   - 调用PrescriptionEditorPanel.GetPrescriptionData()
   - 调用PrescriptionDataManager.SaveAsync()
   - 协调Consultation和Prescription的事务性

**验收标准**:
- [ ] 处方面板正确显示
- [ ] 药材可以选择和配置
- [ ] 继续看诊时正确加载现有处方数据
- [ ] [保存医案]按钮可以同时保存诊断+处方

**代码位置**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml.cs`

---

### Phase 4: 完整工作流实现 (优先级: P2)

**任务清单**:

1. **实现CancelConsultationCommand**
   - 弹出确认对话框: "确定要取消当前看诊吗?未保存的内容将丢失"
   - 判断医案状态:
     - 新建医案(刚创建的Draft) → 调用DeleteMedicalCaseAsync删除
     - 继续医案(之前的Draft) → 不删除,保持Draft状态
   - 导航回PatientSelectionView
   - 清理ViewModel状态

2. **实现CompleteConsultationCommand**
   - 验证必填数据:
     - ConsultationPanel.ValidateData() || PrescriptionPanel.ValidateData()
     - 至少录入诊断或处方其中一项
   - 保存所有数据 (调用SaveMedicalCaseCommand逻辑)
   - 更新医案状态: Draft → Completed
   - 清理UnfinishedCaseHandler缓存
   - 显示成功提示: "看诊完成,医案已保存"
   - 导航回PatientSelectionView

3. **实现PrintPrescriptionCommand (可选)**
   - 验证处方数据非空
   - 自动保存医案(如有未保存内容)
   - 调用打印服务生成处方笺PDF
   - 打开打印对话框

4. **IsDirty标记和未保存提示**
   - ViewModel维护全局IsDirty标记
   - IsDirty = ConsultationPanel.IsDirty || PrescriptionPanel.IsDirty
   - 点击[取消看诊]时,如果IsDirty=true,弹出二次确认

**验收标准**:
- [ ] [取消看诊]正确处理新建/继续两种场景
- [ ] [完成看诊]验证数据并更新状态
- [ ] 医案状态正确从Draft变为Completed
- [ ] UnfinishedCaseHandler缓存正确清理
- [ ] [打印处方笺]功能可用(或合理禁用)

**代码位置**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`

---

### Phase 5: 测试与优化 (优先级: P2)

**任务清单**:

1. **单元测试**
   - MedicalCaseWorkspaceViewModel单元测试
   - ConsultationPanel单元测试
   - PrescriptionEditorPanel单元测试
   - 导航参数传递测试

2. **集成测试**
   - 完整看诊流程端到端测试
   - 未完成病历检测+3选项对话框测试
   - 继续看诊+数据加载测试
   - 新建医案+数据保存测试

3. **性能优化**
   - 医案创建速度优化
   - 数据加载速度优化
   - UI响应性优化

4. **用户体验优化**
   - 加载状态提示
   - 错误提示友好化
   - 操作引导(Tooltip)

**验收标准**:
- [ ] 单元测试覆盖率≥80%
- [ ] 所有集成测试通过
- [ ] 医案创建时间<500ms
- [ ] 数据加载时间<1s

---

## 技术决策记录

### TD-INT-001: 医案创建时机

**决策**: 医案创建在MedicalCaseWorkspaceViewModel.OnNavigatedTo中执行,而非在PatientSelectionViewModel中

**理由**:
1. 职责分离: PatientSelectionViewModel只负责导航和参数传递
2. 错误处理: 工作台ViewModel可以直接处理创建失败,无需跨模块传递错误
3. 生命周期管理: 医案的整个生命周期应该由工作台统一管理
4. 简化逻辑: 避免PatientSelectionViewModel依赖MedicalCaseDataManager

**实现**:
```csharp
// PatientSelectionViewModel: 只传递信号
var parameters = new NavigationParameters
{
    { "CurrentPatient", patient },
    { "MedicalCaseId", Guid.Empty }  // Empty作为"需要创建"的信号
};

// MedicalCaseWorkspaceViewModel: 执行创建
if (MedicalCaseId == Guid.Empty)
{
    MedicalCaseId = await CreateMedicalCaseAsync(CurrentPatient.Id);
}
```

**代码位置**:
- `MedicalCaseWorkspaceViewModel.cs:OnNavigatedTo`
- `PatientSelectionViewModel.cs:NavigateToMedicalCaseFlow`

---

### TD-INT-002: 导航目标修改策略

**决策**: 仅修改导航目标字符串,不修改参数传递逻辑

**理由**:
1. 最小化改动: 只需修改1行代码(Line 852)
2. 向后兼容: 参数格式不变,便于回滚
3. 测试简单: 只需验证导航目标,无需重新测试参数传递

**实现**:
```csharp
// PatientSelectionViewModel.cs:852
// 修改前:
RegionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);

// 修改后:
RegionManager.RequestNavigate("ContentRegion", "MedicalCaseWorkspaceView", parameters);
```

**代码位置**: `PatientSelectionViewModel.cs:852`

---

### TD-INT-003: 子面板数据交互方式

**决策**: 使用DependencyProperty + 事件的方式,而非ViewModel注入

**理由**:
1. UserControl标准模式: DependencyProperty是WPF推荐的数据传递方式
2. 松耦合: 父子组件通过属性和事件通信,无需相互依赖
3. 可复用性: ConsultationPanel和PrescriptionEditorPanel可以在其他场景复用
4. 简化依赖注入: UserControl无法直接使用构造函数注入

**实现**:
```csharp
// ConsultationPanel.xaml.cs
public static readonly DependencyProperty MedicalCaseIdProperty =
    DependencyProperty.Register("MedicalCaseId", typeof(Guid), ...);

public event EventHandler<ConsultationDataChangedEventArgs> ConsultationDataChanged;

// MedicalCaseWorkspaceView.xaml
<controls:ConsultationPanel
    MedicalCaseId="{Binding MedicalCaseId}"
    ConsultationDataChanged="OnConsultationDataChanged"/>
```

**代码位置**: `ConsultationPanel.xaml.cs`, `PrescriptionEditorPanel.xaml.cs`

---

### TD-INT-004: UnfinishedCaseHandler逻辑复用

**决策**: 完全复用现有UnfinishedCaseHandler逻辑,不做任何修改

**理由**:
1. 逻辑成熟: 已经过测试,3选项对话框逻辑完整
2. 缓存管理: CheckUnfinishedMedicalCaseAsync的缓存策略合理
3. 避免回归: 修改可能引入新bug
4. 架构一致: 新工作台与旧流程在这一层保持一致

**实现**:
- 不修改UnfinishedCaseHandler
- 不修改3选项对话框
- 仅修改导航目标字符串

**代码位置**:
- `PatientSelectionViewModel.cs:CheckUnfinishedMedicalCaseAsync`
- `Components/UnfinishedCaseHandler.cs`

---

### TD-INT-005: 保存策略

**决策**: 无自动保存,仅手动保存;[保存医案]和[完成看诊]都会保存数据

**理由**:
1. 用户反馈: 之前会话中用户明确要求无自动保存
2. 数据量小: 单次病案内容不多,手动保存可接受
3. 避免误操作: 自动保存可能保存错误数据
4. 明确的保存时机: [保存医案]=Draft状态保存, [完成看诊]=Completed状态保存

**实现**:
```csharp
// SaveMedicalCaseCommand: 保存但保持Draft状态
await SaveConsultationAsync();
await SavePrescriptionAsync();
// 状态不变,仍为Draft

// CompleteConsultationCommand: 保存并更新为Completed
await SaveConsultationAsync();
await SavePrescriptionAsync();
await UpdateMedicalCaseStatusAsync(MedicalCaseStatus.Completed);
```

**代码位置**: `MedicalCaseWorkspaceViewModel.cs`

---

### TD-INT-006: 4个按钮的功能定位

**决策**:
- [取消看诊] = 放弃当前工作,可能删除医案
- [保存医案] = 保存数据,保持Draft状态,可继续编辑
- [打印处方笺] = 打印处方,不影响医案状态
- [完成看诊] = 保存数据,更新为Completed状态,结束工作流

**理由**:
1. 符合用户习惯: 与常见编辑器的按钮逻辑一致(取消/保存/打印/完成)
2. 状态清晰: Draft vs Completed的区分明确
3. 灵活性: 医生可以多次保存草稿,最后一次性完成

**实现**:
- CancelConsultationCommand: 确认对话框 + 可能删除 + 导航回
- SaveMedicalCaseCommand: 保存 + 提示 + 不导航
- PrintPrescriptionCommand: 打印服务 + 打印对话框
- CompleteConsultationCommand: 保存 + 状态更新 + 缓存清理 + 导航回

**代码位置**: `MedicalCaseWorkspaceViewModel.cs`

---

## 风险评估与缓解

### 风险1: ConsultationPanel和PrescriptionEditorPanel实现复杂度高

**风险等级**: 中

**影响**: 两个UserControl各自包含复杂逻辑,可能难以调试和维护

**缓解策略**:
1. **渐进式重构**:
   - Phase 2: 先直接复用ConsultationFormView的XAML,不创建独立ViewModel
   - Phase 3: 再根据需要提取ViewModel
2. **充分测试**: 为每个UserControl编写单元测试
3. **代码复审**: 组件化实现需要严格代码复审

**责任人**: 开发团队

---

### 风险2: MedicalCaseDataManager.CreateAsync返回null的bug未修复

**风险等级**: 高

**影响**: 医案创建失败,导致工作台无法正常工作

**缓解策略**:
1. **Phase 0优先修复**: 将bug修复作为Phase 0的P0任务
2. **单元测试覆盖**: 添加CreateAsync的单元测试,确保正确返回Guid
3. **错误处理**: 在OnNavigatedTo中增加错误处理,创建失败时友好提示并返回

**责任人**: 后端开发

---

### 风险3: 从旧流程到新流程的平滑过渡

**风险等级**: 低

**影响**: 用户已习惯3步流程,突然改变可能不适应

**缓解策略**:
1. **保留旧流程**: 暂时保留MedicalCaseFlowView,通过配置切换
2. **操作引导**: 在新工作台增加操作引导(Tooltip/帮助按钮)
3. **用户培训**: 上线前对医生进行培训
4. **反馈机制**: 收集用户反馈,快速迭代优化

**责任人**: 产品经理 + 开发团队

---

### 风险4: 打印功能未实现

**风险等级**: 低

**影响**: [打印处方笺]按钮无法使用

**缓解策略**:
1. **功能降级**: Phase 1-3暂时禁用打印按钮,显示"功能开发中"
2. **独立开发**: 打印服务作为独立模块,Phase 4单独实现
3. **前端占位**: UI按钮保留,后端接口准备好后快速集成

**责任人**: 开发团队

---

### 风险5: 数据一致性问题(Consultation和Prescription保存事务性)

**风险等级**: 中

**影响**: 如果Consultation保存成功但Prescription保存失败,数据不一致

**缓解策略**:
1. **补偿事务**:
   - 如果Prescription保存失败,回滚Consultation
   - 或记录部分失败状态,提示用户重试
2. **前端验证**: 在保存前验证数据完整性
3. **后端事务**: 考虑在Server端实现事务性保存(SaveMedicalCaseFullAsync)

**责任人**: 后端开发 + 前端开发

---

## 附录

### 相关文档

- [中医诊断系统架构设计](../consultation-system/overview.md)
- [医案管理系统架构设计](../medicalcase-system/overview.md)
- [患者管理系统架构设计](../patient-system/overview.md)

### 代码位置索引

| 组件 | 文件路径 |
|------|---------|
| PatientSelectionViewModel | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs` |
| PatientSelectionView | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml` |
| MedicalCaseWorkspaceViewModel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs` |
| MedicalCaseWorkspaceView | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml` |
| ConsultationPanel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml` |
| PrescriptionEditorPanel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml` |
| UnfinishedCaseHandler | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Components/UnfinishedCaseHandler.cs` |
| MedicalCaseDataManager | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseDataManager.cs` |

---

**文档版本**: v1.0
**创建日期**: 2025-01-22
**最后更新**: 2025-01-22
**作者**: Claude (UltraThink深度分析)
**维护团队**: LYBTZYZS开发组
