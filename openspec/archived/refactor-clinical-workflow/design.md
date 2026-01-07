# Design: refactor-clinical-workflow

## Overview

本文档详细说明患者选择界面重新设计的技术架构和实现细节。

---

## 控件模式架构 (Control Pattern Architecture)

### 核心原则

| 层级 | 位置 | 职责 |
|------|------|------|
| **主界面(View)** | 角色模块 `LYBT.Desktop.Clinical` | 页面布局、导航、角色操作按钮 |
| **控件(Control)** | 功能模块 `LYBT.Desktop.Patients` | 可复用业务组件 |

### Clinical模块三个核心界面

**设计决策**: 三个主界面都定义在Clinical模块中，保持工整便于理解。

```
LYBT.Desktop.Clinical/                    # 角色模块 - 医生
├── Views/
│   ├── ClinicalHomeView.xaml             # 1. 主页入口 (已存在)
│   ├── PatientSelectionView.xaml         # 2. 患者选择 (新建)
│   └── MedicalCaseWorkspaceView.xaml     # 3. 医案看诊 (迁移自MedicalCase模块)
└── ViewModels/
    ├── ClinicalHomeViewModel.cs          # (已存在)
    ├── PatientSelectionViewModel.cs      # (新建)
    └── MedicalCaseWorkspaceViewModel.cs  # (迁移)

LYBT.Desktop.Patients/                    # 功能模块 - 患者
└── Controls/
    ├── PatientSelectionControl.xaml      # 新建 - 患者选择控件
    ├── PatientViewControl.xaml           # 复用 - 只读详情
    └── PatientSearchControl.xaml         # 复用 - 搜索列表

LYBT.Desktop.MedicalCase/                 # 功能模块 - 医案
└── Controls/
    └── (医案相关控件，如PendingQueueControl)
```

### 设计优势

1. **关注点分离** - 角色模块负责流程，功能模块负责组件
2. **控件复用** - 患者控件可被多个角色模块复用(医生/前台)
3. **独立演进** - 功能模块控件可独立开发测试
4. **流程清晰** - 三个主界面都在Clinical，便于理解医生工作流

---

## 业务流程设计 (参考 optimize-medicalcase-navigation)

### 核心原则

| 原则 | 说明 |
|------|------|
| **单一挂起** | 同一患者只允许有一个挂起医案 |
| **角色区分** | 前台只挂号，医生直接看诊 |
| **模式区分** | 查看模式无提示，编辑模式有提示 |

### 患者选择界面流程

```
选择患者 → 查询是否有挂起医案
              │
              ├── 无挂起
              │      │
              │      ├── 前台角色 → 挂号 → 患者进入待诊列表
              │      │
              │      └── 医生角色 → 新建医案 → 跳转看诊界面
              │
              └── 有挂起
                     │
                     ├── 前台角色 → 禁止挂号 + 提示"请先到XX医生处理"
                     │
                     └── 医生角色 → 【四选项弹窗】
```

### 四选项弹窗（处理挂起医案）

| # | 选项 | 操作 | 结果 |
|---|------|------|------|
| 1 | 继续挂起医案 | 导航到挂起医案 | 继续编辑原医案 |
| 2 | 关闭挂起+新建 | 取消原医案 → 创建新医案 | 开始新的看诊 |
| 3 | 仅关闭挂起 | 取消原医案 | 留在当前界面 |
| 4 | 取消 | 不做任何操作 | 留在当前界面 |

---

## 待诊队列说明 (参考 redesign-pending-queue)

### 关键决策

**患者选择界面不包含待诊队列**。待诊队列仅在医案工作区(MedicalCaseWorkspaceView)中显示。

| 位置 | 功能 | 说明 |
|------|------|------|
| 患者选择界面 | 选择患者 + 挂号/看诊 | 简洁的Master-Detail布局 |
| 医案工作区 | 待诊队列 + 患者切换 | WorkspacePendingQueueHandler处理 |

### 待诊队列状态定义

| 状态 | 英文 | 含义 | 操作 |
|------|------|------|------|
| 待诊 | Waiting | 无医案，已挂号等待 | 可双击新建医案 |
| 挂起 | Suspended | 有Draft医案 | 可双击显示四选项弹窗 |
| 正在看诊 | InProgress | 当前患者 | 不可操作 |

---

## 挂号功能设计

### 挂号流程

```
前台选择患者
    │
    ├── 检查是否有挂起医案
    │      │
    │      ├── 有 → 禁止挂号，提示"该患者在XX医生处有未完成医案"
    │      │
    │      └── 无 → 创建待诊记录
    │              │
    │              └── 患者进入待诊列表(Waiting状态)
    │
    └── 显示挂号成功提示
```

### 待诊记录数据流

```
前台挂号 → 创建PendingCase(Waiting) → 医生待诊列表显示
                                              │
                                              ▼
                                    医生双击 → 创建医案 → 看诊
```

---

## View设计

### PatientSelectionView.xaml (Clinical模块 - 主界面)

```xml
<UserControl x:Class="LYBT.Desktop.Clinical.Views.PatientSelectionView"
             xmlns:patientControls="clr-namespace:LYBT.Desktop.Patients.Controls;assembly=LYBT.Desktop.Patients">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 顶部导航 -->
            <RowDefinition Height="*"/>      <!-- 控件区域 -->
            <RowDefinition Height="Auto"/>  <!-- 底部操作 -->
        </Grid.RowDefinitions>

        <!-- 顶部导航栏 -->
        <Border Grid.Row="0" Style="{StaticResource PageHeaderStyle}">
            <Grid>
                <TextBlock Text="患者选择" Style="{StaticResource PageTitleStyle}"/>
                <Button Content="返回主页" 
                        Command="{Binding BackCommand}"
                        HorizontalAlignment="Right"/>
            </Grid>
        </Border>

        <!-- 患者选择控件 (来自Patients模块) -->
        <patientControls:PatientSelectionControl
            Grid.Row="1"
            SelectedPatient="{Binding SelectedPatient, Mode=TwoWay}"
            PatientDetail="{Binding PatientDetail, Mode=OneWayToSource}"
            CreateNewCommand="{Binding CreateNewCommand}"/>

        <!-- 底部操作栏 -->
        <Border Grid.Row="2" Style="{StaticResource ActionBarStyle}">
            <Grid>
                <TextBlock Text="{Binding StatusMessage}" 
                           VerticalAlignment="Center"/>
                
                <!-- 角色相关按钮 -->
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <!-- 前台模式：挂号按钮 -->
                    <Button Content="挂号"
                            Command="{Binding RegisterCommand}"
                            Visibility="{Binding IsReceptionMode, Converter={StaticResource BoolToVisibility}}"/>
                    
                    <!-- 医生模式：开始看诊按钮 -->
                    <Button Content="开始看诊"
                            Command="{Binding StartConsultationCommand}"
                            Visibility="{Binding IsClinicalMode, Converter={StaticResource BoolToVisibility}}"
                            Style="{StaticResource PrimaryButtonStyle}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### PatientSelectionControl.xaml (Patients模块 - 可复用控件)

```xml
<UserControl x:Class="LYBT.Desktop.Patients.Controls.PatientSelectionControl">
    <controls:MasterDetailLayout HasSelection="{Binding HasSelection, RelativeSource={RelativeSource AncestorType=UserControl}}">

        <!-- Master: 患者列表 -->
        <controls:MasterDetailLayout.MasterContent>
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>  <!-- 工具栏 -->
                    <RowDefinition Height="Auto"/>  <!-- 搜索框 -->
                    <RowDefinition Height="*"/>      <!-- 列表 -->
                    <RowDefinition Height="Auto"/>  <!-- 分页 -->
                </Grid.RowDefinitions>

                <controls:DataGridToolbar Grid.Row="0"
                    CreateCommand="{Binding CreateNewCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                    RefreshCommand="{Binding RefreshCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"/>

                <controls:SearchBox Grid.Row="1"
                    SearchText="{Binding SearchText, Mode=TwoWay, RelativeSource={RelativeSource AncestorType=UserControl}}"/>

                <DataGrid Grid.Row="2"
                    ItemsSource="{Binding Patients, RelativeSource={RelativeSource AncestorType=UserControl}}"
                    SelectedItem="{Binding SelectedPatient, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="80"/>
                        <DataGridTextColumn Header="性别" Binding="{Binding Gender}" Width="50"/>
                        <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="50"/>
                        <DataGridTextColumn Header="电话" Binding="{Binding PhoneMasked}" Width="120"/>
                    </DataGrid.Columns>
                </DataGrid>

                <controls:PaginationPanel Grid.Row="3"/>
            </Grid>
        </controls:MasterDetailLayout.MasterContent>

        <!-- Detail: 只读患者详情 -->
        <controls:MasterDetailLayout.DetailContent>
            <patientControls:PatientViewControl 
                Patient="{Binding PatientDetail, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
        </controls:MasterDetailLayout.DetailContent>

        <!-- 空状态 -->
        <controls:MasterDetailLayout.EmptyContent>
            <controls:EmptyState
                Title="请选择患者"
                Subtitle="从左侧列表选择患者，或点击新建创建新患者"
                ActionText="新建患者"
                ActionCommand="{Binding CreateNewCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
        </controls:MasterDetailLayout.EmptyContent>
    </controls:MasterDetailLayout>
</UserControl>
```

---

## ViewModel设计

### PatientSelectionViewModel (Clinical模块)

```csharp
/// <summary>
/// 患者选择ViewModel
/// 前台/医生公用，通过WorkspaceMode区分操作
/// </summary>
public class PatientSelectionViewModel : BindableBase, INavigationAware
{
    #region 依赖注入

    private readonly IPatientApi _patientApi;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly ICommonDialogService _dialogService;
    private readonly IRegionManager _regionManager;
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly ILogger<PatientSelectionViewModel> _logger;

    #endregion

    #region 属性

    /// <summary>工作模式：Clinical(医生) 或 Reception(前台)</summary>
    public WorkspaceMode WorkspaceMode { get; set; }

    /// <summary>是否前台模式</summary>
    public bool IsReceptionMode => WorkspaceMode == WorkspaceMode.Reception;

    /// <summary>是否医生模式</summary>
    public bool IsClinicalMode => WorkspaceMode == WorkspaceMode.Clinical;

    /// <summary>选中的患者</summary>
    public PatientListDto? SelectedPatient { get; set; }

    /// <summary>患者详情</summary>
    public PatientDetailDto? PatientDetail { get; set; }

    /// <summary>状态消息</summary>
    public string StatusMessage { get; set; }

    #endregion

    #region 命令

    /// <summary>返回主页</summary>
    public DelegateCommand BackCommand { get; }

    /// <summary>新建患者</summary>
    public DelegateCommand CreateNewCommand { get; }

    /// <summary>挂号（前台模式）</summary>
    public DelegateCommand RegisterCommand { get; }

    /// <summary>开始看诊（医生模式）</summary>
    public DelegateCommand StartConsultationCommand { get; }

    #endregion

    #region 命令实现

    /// <summary>返回主页</summary>
    private void ExecuteBack()
    {
        _roleNavigationService.NavigateToHome();
    }

    /// <summary>挂号（前台模式）</summary>
    private async void ExecuteRegister()
    {
        if (SelectedPatient == null) return;

        // 1. 检查是否有挂起医案
        var hasSuspended = await _medicalCaseApi.HasSuspendedCaseAsync(SelectedPatient.Id);
        if (hasSuspended)
        {
            await _dialogService.ShowInfoAsync(
                $"该患者在其他医生处有未完成医案，请先处理后再挂号");
            return;
        }

        // 2. 创建待诊记录
        var result = await _medicalCaseApi.CreatePendingCaseAsync(new CreatePendingCaseRequest
        {
            PatientId = SelectedPatient.Id,
            Type = PendingCaseType.Waiting
        });

        if (result.IsSuccess)
        {
            StatusMessage = $"患者 {SelectedPatient.Name} 挂号成功";
            await _dialogService.ShowSuccessAsync("挂号成功，患者已进入待诊列表");
        }
    }

    /// <summary>开始看诊（医生模式）</summary>
    private async void ExecuteStartConsultation()
    {
        if (SelectedPatient == null) return;

        // 1. 检查是否有挂起医案
        var suspendedCase = await _medicalCaseApi.GetSuspendedCaseAsync(SelectedPatient.Id);
        
        if (suspendedCase != null)
        {
            // 显示四选项弹窗
            var choice = await _dialogService.ShowSuspendedCaseDialogAsync(
                SelectedPatient.Name, 
                suspendedCase);

            switch (choice)
            {
                case SuspendedCaseChoice.Continue:
                    NavigateToMedicalCase(suspendedCase.MedicalCaseId);
                    return;
                case SuspendedCaseChoice.CloseAndNew:
                    await _medicalCaseApi.CancelAsync(suspendedCase.MedicalCaseId);
                    break;
                case SuspendedCaseChoice.CloseOnly:
                    await _medicalCaseApi.CancelAsync(suspendedCase.MedicalCaseId);
                    return;
                case SuspendedCaseChoice.Cancel:
                    return;
            }
        }

        // 2. 创建新医案
        var createResult = await _medicalCaseApi.CreateAsync(new CreateMedicalCaseRequest
        {
            PatientId = SelectedPatient.Id
        });

        if (createResult.IsSuccess)
        {
            NavigateToMedicalCase(createResult.Value.Id);
        }
    }

    private void NavigateToMedicalCase(Guid medicalCaseId)
    {
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", medicalCaseId },
            { "CurrentPatient", PatientDetail },
            { "WorkspaceMode", WorkspaceMode.Clinical }
        };
        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseWorkspaceView", parameters);
    }

    #endregion
}
```

---

## 导航流程

### 医生看诊流程

```
ClinicalHomeView
    │ 点击"开始接诊"
    ▼
PatientSelectionView (WorkspaceMode=Clinical)
    │ 选择患者 → 点击"开始看诊"
    │     │
    │     ├── 无挂起 → 创建医案 → 导航
    │     └── 有挂起 → 四选项弹窗 → 导航/取消
    ▼
MedicalCaseWorkspaceView
    │ 包含待诊队列(WorkspacePendingQueueHandler)
    │ 可切换患者、暂存、完成
    ▼
返回 → PatientSelectionView
```

### 前台挂号流程

```
ReceptionHomeView
    │ 点击"患者挂号"
    ▼
PatientSelectionView (WorkspaceMode=Reception)
    │ 选择患者 → 点击"挂号"
    │     │
    │     ├── 无挂起 → 创建待诊记录 → 成功提示
    │     └── 有挂起 → 禁止挂号 → 提示去找医生
    ▼
继续挂号或返回
```

---

## 文件变更清单

### 需要新建的文件

| 文件 | 位置 | 说明 |
|------|------|------|
| `PatientSelectionView.xaml` | Clinical/Views/ | 患者选择主界面 |
| `PatientSelectionView.xaml.cs` | Clinical/Views/ | Code-behind |
| `PatientSelectionViewModel.cs` | Clinical/ViewModels/ | ViewModel |
| `PatientSelectionControl.xaml` | Patients/Controls/ | 可复用控件 |
| `PatientSelectionControl.xaml.cs` | Patients/Controls/ | 控件代码 |

### 需要迁移的文件

| 文件 | 原位置 | 新位置 | 说明 |
|------|--------|--------|------|
| `MedicalCaseWorkspaceView.xaml` | MedicalCase/Views/ | Clinical/Views/ | 主界面迁移到Clinical |
| `MedicalCaseWorkspaceView.xaml.cs` | MedicalCase/Views/ | Clinical/Views/ | 同上 |
| `MedicalCaseWorkspaceViewModel.cs` | MedicalCase/ViewModels/ | Clinical/ViewModels/ | 同上 |

### 需要删除的文件

| 文件 | 位置 | 理由 |
|------|------|------|
| `PatientSelectionView.xaml` | Patients/Views/ | 被新设计替代 |
| `PatientSelectionView.xaml.cs` | Patients/Views/ | 被新设计替代 |
| `PatientSelectionViewModel.cs` | Patients/ViewModels/ | 被新设计替代 |
| `PatientSelectionCommandExecutor.cs` | Patients/ViewModels/Components/ | 不再需要 |
| `PendingQueueManager.cs` | Patients/Services/ | 功能已在MedicalCase模块 |

### 需要修改的文件

| 文件 | 修改内容 |
|------|----------|
| `ClinicalModule.cs` | 注册新View/ViewModel |
| `PatientsModule.cs` | 注册新Control，删除旧注册 |
| `MedicalCaseModule.cs` | 删除迁移走的View/ViewModel注册 |
| `ClinicalHomeViewModel.cs` | 更新导航目标 |

---

## 测试场景

### 患者选择界面

| # | 场景 | 预期结果 |
|---|------|----------|
| 1 | 前台选择无挂起患者 | 挂号成功，进入待诊列表 |
| 2 | 前台选择有挂起患者 | 禁止挂号，提示去找医生 |
| 3 | 医生选择无挂起患者 | 直接创建医案，进入看诊 |
| 4 | 医生选择有挂起患者 | 显示四选项弹窗 |
| 5 | 点击新建患者 | 进入患者编辑界面 |
| 6 | 点击返回主页 | 返回对应角色主页 |

### 四选项弹窗

| # | 场景 | 预期结果 |
|---|------|----------|
| 7 | 选择"继续挂起" | 导航到挂起医案 |
| 8 | 选择"关闭挂起+新建" | 取消原医案，创建新医案 |
| 9 | 选择"仅关闭挂起" | 取消原医案，留在当前界面 |
| 10 | 选择"取消" | 不做任何操作 |