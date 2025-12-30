# Design: 看诊工作台布局重构与控件化

**Change ID**: refactor-medicalcase-workspace
**设计类型**: UI架构 + 控件设计
**创建时间**: 2025-12-25

---

## 1. 架构概览

### 1.1 控件层次结构

```
LYBT.Desktop.Shared/Controls/
├── PatientInfoCardControl.xaml(.cs)    // 新建 - 患者信息卡片
├── PatientSearchControl.xaml(.cs)      // 新建 - 患者搜索控件
└── PendingQueueControl.xaml(.cs)       // 新建 - 待诊队列控件

使用场景:
┌─────────────────────────────────────────────────────────────────────┐
│                          PatientSelectionView                        │
│  ┌──────────────────┐  ┌────────────────────────────────────────┐  │
│  │PendingQueueControl│  │         PatientSearchControl          │  │
│  │                   │  │  ┌──────────────────────────────────┐ │  │
│  │  - 待诊列表       │  │  │ 搜索框 + 患者列表 + 分页         │ │  │
│  │  - 选择/刷新      │  │  └──────────────────────────────────┘ │  │
│  └──────────────────┘  └────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                       MedicalCaseWorkspaceView                       │
│  ┌──────────────────┐  ┌────────────────────────────────────────┐  │
│  │PatientInfoCard   │  │              诊断区 35%                │  │
│  │Control           │  ├────────────────────────────────────────┤  │
│  │                  │  │              处方区 65%                │  │
│  │  25%             │  │              75%                       │  │
│  └──────────────────┘  └────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                       未来: 前台挂号界面                             │
│  ┌──────────────────────────────────────┐  ┌──────────────────┐    │
│  │      PatientSearchControl            │  │PendingQueue      │    │
│  │      (复用)                          │  │Control           │    │
│  └──────────────────────────────────────┘  └──────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 数据流设计

```
┌──────────────┐     PatientSelectedEvent      ┌───────────────────────┐
│PatientSelect │ ───────────────────────────▶  │MedicalCaseWorkspace   │
│ionViewModel  │     NavigationParameters      │ViewModel              │
└──────────────┘     {PatientId, Patient}      └───────────────────────┘
       │                                                   │
       │ 绑定                                              │ 绑定
       ▼                                                   ▼
┌──────────────┐                               ┌───────────────────────┐
│PatientSearch │                               │PatientInfoCard        │
│Control       │                               │Control                │
│DP: Patients  │                               │DP: Patient            │
│DP: Selected  │                               │DP: DisplayMode        │
└──────────────┘                               └───────────────────────┘
```

---

## 2. 控件详细设计

### 2.1 PatientInfoCardControl

**职责**: 展示患者基本信息，支持多种显示模式

```xml
<!-- 控件结构 -->
<UserControl x:Class="LYBT.Desktop.Shared.Controls.PatientInfoCardControl">
    <Border Style="{StaticResource CardBorder}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- 头像+姓名 -->
                <RowDefinition Height="Auto"/>  <!-- 基本信息 -->
                <RowDefinition Height="Auto"/>  <!-- 就诊信息 -->
                <RowDefinition Height="*"/>     <!-- 操作按钮 -->
            </Grid.RowDefinitions>

            <!-- Row 0: 头像区 -->
            <StackPanel Orientation="Horizontal">
                <Ellipse Width="48" Height="48">
                    <Ellipse.Fill>
                        <!-- 首字母头像 -->
                    </Ellipse.Fill>
                </Ellipse>
                <StackPanel>
                    <TextBlock Text="{Binding Patient.Name}"/>
                    <TextBlock Text="{Binding Patient.Gender}"/>
                </StackPanel>
            </StackPanel>

            <!-- Row 1: 基本信息 -->
            <ItemsControl Grid.Row="1">
                <TextBlock Text="年龄: XX岁"/>
                <TextBlock Text="电话: XXXX"/>
            </ItemsControl>

            <!-- Row 2: 就诊信息 -->
            <StackPanel Grid.Row="2" Visibility="{Binding ShowVisitCount}">
                <TextBlock Text="就诊次数: XX"/>
                <TextBlock Text="挂号时间: XX"/>
            </StackPanel>

            <!-- Row 3: 操作按钮 -->
            <Button Grid.Row="3"
                    Content="查看历史"
                    Command="{Binding HistoryCommand}"
                    Visibility="{Binding ShowHistoryButton}"/>
        </Grid>
    </Border>
</UserControl>
```

**代码后台**:
```csharp
public partial class PatientInfoCardControl : UserControl
{
    #region Patient属性
    public static readonly DependencyProperty PatientProperty =
        DependencyProperty.Register(
            nameof(Patient),
            typeof(PatientDisplayModel),
            typeof(PatientInfoCardControl),
            new PropertyMetadata(null));

    public PatientDisplayModel? Patient
    {
        get => (PatientDisplayModel?)GetValue(PatientProperty);
        set => SetValue(PatientProperty, value);
    }
    #endregion

    #region DisplayMode属性
    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register(
            nameof(DisplayMode),
            typeof(PatientCardDisplayMode),
            typeof(PatientInfoCardControl),
            new PropertyMetadata(PatientCardDisplayMode.Full));

    public PatientCardDisplayMode DisplayMode
    {
        get => (PatientCardDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }
    #endregion

    #region ShowHistoryButton属性
    public static readonly DependencyProperty ShowHistoryButtonProperty =
        DependencyProperty.Register(
            nameof(ShowHistoryButton),
            typeof(bool),
            typeof(PatientInfoCardControl),
            new PropertyMetadata(true));

    public bool ShowHistoryButton
    {
        get => (bool)GetValue(ShowHistoryButtonProperty);
        set => SetValue(ShowHistoryButtonProperty, value);
    }
    #endregion

    #region HistoryCommand属性
    public static readonly DependencyProperty HistoryCommandProperty =
        DependencyProperty.Register(
            nameof(HistoryCommand),
            typeof(ICommand),
            typeof(PatientInfoCardControl),
            new PropertyMetadata(null));

    public ICommand? HistoryCommand
    {
        get => (ICommand?)GetValue(HistoryCommandProperty);
        set => SetValue(HistoryCommandProperty, value);
    }
    #endregion

    #region ShowVisitCount属性
    public static readonly DependencyProperty ShowVisitCountProperty =
        DependencyProperty.Register(
            nameof(ShowVisitCount),
            typeof(bool),
            typeof(PatientInfoCardControl),
            new PropertyMetadata(true));

    public bool ShowVisitCount
    {
        get => (bool)GetValue(ShowVisitCountProperty);
        set => SetValue(ShowVisitCountProperty, value);
    }
    #endregion
}

public enum PatientCardDisplayMode
{
    Full,      // 完整模式：所有信息
    Compact,   // 紧凑模式：仅姓名+性别+年龄
    Minimal    // 最小模式：仅姓名
}
```

### 2.2 PatientSearchControl

**职责**: 提供患者搜索、列表展示、分页功能

```csharp
public partial class PatientSearchControl : UserControl
{
    #region SearchKeyword属性
    public static readonly DependencyProperty SearchKeywordProperty =
        DependencyProperty.Register(
            nameof(SearchKeyword),
            typeof(string),
            typeof(PatientSearchControl),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string SearchKeyword
    {
        get => (string)GetValue(SearchKeywordProperty);
        set => SetValue(SearchKeywordProperty, value);
    }
    #endregion

    #region Patients属性
    public static readonly DependencyProperty PatientsProperty =
        DependencyProperty.Register(
            nameof(Patients),
            typeof(IEnumerable<PatientListDto>),
            typeof(PatientSearchControl),
            new PropertyMetadata(null));

    public IEnumerable<PatientListDto>? Patients
    {
        get => (IEnumerable<PatientListDto>?)GetValue(PatientsProperty);
        set => SetValue(PatientsProperty, value);
    }
    #endregion

    #region SelectedPatient属性
    public static readonly DependencyProperty SelectedPatientProperty =
        DependencyProperty.Register(
            nameof(SelectedPatient),
            typeof(PatientListDto),
            typeof(PatientSearchControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public PatientListDto? SelectedPatient
    {
        get => (PatientListDto?)GetValue(SelectedPatientProperty);
        set => SetValue(SelectedPatientProperty, value);
    }
    #endregion

    #region SearchCommand属性
    public static readonly DependencyProperty SearchCommandProperty =
        DependencyProperty.Register(
            nameof(SearchCommand),
            typeof(ICommand),
            typeof(PatientSearchControl),
            new PropertyMetadata(null));

    public ICommand? SearchCommand
    {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }
    #endregion

    #region PatientSelectedCommand属性
    public static readonly DependencyProperty PatientSelectedCommandProperty =
        DependencyProperty.Register(
            nameof(PatientSelectedCommand),
            typeof(ICommand),
            typeof(PatientSearchControl),
            new PropertyMetadata(null));

    public ICommand? PatientSelectedCommand
    {
        get => (ICommand?)GetValue(PatientSelectedCommandProperty);
        set => SetValue(PatientSelectedCommandProperty, value);
    }
    #endregion

    #region ShowCreateButton属性
    public static readonly DependencyProperty ShowCreateButtonProperty =
        DependencyProperty.Register(
            nameof(ShowCreateButton),
            typeof(bool),
            typeof(PatientSearchControl),
            new PropertyMetadata(true));

    public bool ShowCreateButton
    {
        get => (bool)GetValue(ShowCreateButtonProperty);
        set => SetValue(ShowCreateButtonProperty, value);
    }
    #endregion

    #region ShowPagination属性
    public static readonly DependencyProperty ShowPaginationProperty =
        DependencyProperty.Register(
            nameof(ShowPagination),
            typeof(bool),
            typeof(PatientSearchControl),
            new PropertyMetadata(true));

    public bool ShowPagination
    {
        get => (bool)GetValue(ShowPaginationProperty);
        set => SetValue(ShowPaginationProperty, value);
    }
    #endregion
}
```

### 2.3 PendingQueueControl

**职责**: 展示待诊队列，支持选择和刷新

```csharp
public partial class PendingQueueControl : UserControl
{
    #region PendingQueue属性
    public static readonly DependencyProperty PendingQueueProperty =
        DependencyProperty.Register(
            nameof(PendingQueue),
            typeof(IEnumerable<PendingPatientDto>),
            typeof(PendingQueueControl),
            new PropertyMetadata(null));

    public IEnumerable<PendingPatientDto>? PendingQueue
    {
        get => (IEnumerable<PendingPatientDto>?)GetValue(PendingQueueProperty);
        set => SetValue(PendingQueueProperty, value);
    }
    #endregion

    #region SelectedItem属性
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(PendingPatientDto),
            typeof(PendingQueueControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public PendingPatientDto? SelectedItem
    {
        get => (PendingPatientDto?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
    #endregion

    #region RefreshCommand属性
    public static readonly DependencyProperty RefreshCommandProperty =
        DependencyProperty.Register(
            nameof(RefreshCommand),
            typeof(ICommand),
            typeof(PendingQueueControl),
            new PropertyMetadata(null));

    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }
    #endregion

    #region SelectCommand属性
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register(
            nameof(SelectCommand),
            typeof(ICommand),
            typeof(PendingQueueControl),
            new PropertyMetadata(null));

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }
    #endregion

    #region IsCompactMode属性
    public static readonly DependencyProperty IsCompactModeProperty =
        DependencyProperty.Register(
            nameof(IsCompactMode),
            typeof(bool),
            typeof(PendingQueueControl),
            new PropertyMetadata(false));

    public bool IsCompactMode
    {
        get => (bool)GetValue(IsCompactModeProperty);
        set => SetValue(IsCompactModeProperty, value);
    }
    #endregion
}
```

---

## 3. 布局详细设计

### 3.1 MedicalCaseWorkspaceView 新布局

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="25*" MinWidth="300" MaxWidth="400"/>
        <ColumnDefinition Width="75*"/>
    </Grid.ColumnDefinitions>

    <!-- 左侧: 患者信息卡片 -->
    <Border Grid.Column="0"
            Margin="12"
            Style="{StaticResource SectionBorder}">
        <controls:PatientInfoCardControl
            Patient="{Binding CurrentPatient}"
            DisplayMode="Full"
            ShowHistoryButton="True"
            ShowVisitCount="True"
            HistoryCommand="{Binding ViewHistoryCommand}"/>
    </Border>

    <!-- 右侧: 诊断+处方 -->
    <Grid Grid.Column="1">
        <Grid.RowDefinitions>
            <RowDefinition Height="35*"/>  <!-- 诊断区 -->
            <RowDefinition Height="65*"/>  <!-- 处方区 -->
        </Grid.RowDefinitions>

        <!-- 诊断区 35% -->
        <Border Grid.Row="0"
                Margin="0,12,12,6"
                Style="{StaticResource SectionBorder}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- 现病史 -->
                <GroupBox Grid.Row="0" Grid.Column="0" Header="现病史">
                    <TextBox Text="{Binding Consultation.History}"/>
                </GroupBox>

                <!-- 舌诊 -->
                <GroupBox Grid.Row="0" Grid.Column="1" Header="舌诊">
                    <TextBox Text="{Binding Consultation.TongueDiagnosis}"/>
                </GroupBox>

                <!-- 脉诊 -->
                <GroupBox Grid.Row="1" Grid.Column="0" Header="脉诊">
                    <TextBox Text="{Binding Consultation.PulseDiagnosis}"/>
                </GroupBox>

                <!-- 中医诊断 -->
                <GroupBox Grid.Row="1" Grid.Column="1" Header="中医诊断*">
                    <TextBox Text="{Binding Consultation.Diagnosis}"/>
                </GroupBox>
            </Grid>
        </Border>

        <!-- 处方区 65% -->
        <Border Grid.Row="1"
                Margin="0,6,12,12"
                Style="{StaticResource SectionBorder}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>  <!-- 标题栏+按钮 -->
                    <RowDefinition Height="*"/>     <!-- 药材列表 -->
                </Grid.RowDefinitions>

                <!-- 标题栏: 包含经验方查询和历史医案按钮 -->
                <Border Grid.Row="0" Style="{StaticResource HeaderBar}">
                    <DockPanel>
                        <TextBlock Text="处方" DockPanel.Dock="Left"/>
                        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal">
                            <Button Content="经验方查询"
                                    Command="{Binding PrescriptionPanel.OpenFormulaImportDialogCommand}"
                                    Style="{StaticResource SecondaryButton}"/>
                            <Button Content="历史医案"
                                    Command="{Binding PrescriptionPanel.OpenHistoryCopyDialogCommand}"
                                    Style="{StaticResource SecondaryButton}"/>
                            <Button Content="清空"
                                    Command="{Binding PrescriptionPanel.ClearAllCommand}"
                                    Style="{StaticResource LinkButton}"/>
                        </StackPanel>
                    </DockPanel>
                </Border>

                <!-- 药材列表 -->
                <DataGrid Grid.Row="1"
                          ItemsSource="{Binding PrescriptionPanel.HerbItems}"
                          Style="{StaticResource PrescriptionDataGrid}">
                    <!-- 列定义 -->
                </DataGrid>
            </Grid>
        </Border>
    </Grid>
</Grid>
```

### 3.2 响应式布局

```xml
<!-- 使用AdaptiveTrigger实现响应式 -->
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <!-- 完整模式: >= 1600px -->
        <VisualState x:Name="FullMode">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="1600"/>
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="LeftColumn.Width" Value="25*"/>
                <Setter Target="PatientCard.DisplayMode" Value="Full"/>
            </VisualState.Setters>
        </VisualState>

        <!-- 折叠模式: 1280-1600px -->
        <VisualState x:Name="CompactMode">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="1280"/>
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="LeftColumn.Width" Value="200"/>
                <Setter Target="LeftColumn.MinWidth" Value="200"/>
                <Setter Target="PatientCard.DisplayMode" Value="Compact"/>
            </VisualState.Setters>
        </VisualState>

        <!-- 下拉模式: < 1280px -->
        <VisualState x:Name="MinimalMode">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="0"/>
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="LeftColumn.Width" Value="0"/>
                <Setter Target="PatientDropdown.Visibility" Value="Visible"/>
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

---

## 4. ViewModel调整

### 4.1 MedicalCaseWorkspaceViewModel 修改

```csharp
public partial class MedicalCaseWorkspaceViewModel : ViewModelBase
{
    // 新增: 当前患者信息(用于左侧卡片)
    [ObservableProperty]
    private PatientDisplayModel? _currentPatient;

    // 保留: 处方面板ViewModel(含经验方/历史命令)
    public PrescriptionPanelViewModel PrescriptionPanel { get; }

    // 保留: 诊断数据
    public ConsultationEditModel Consultation { get; }

    // 新增: 查看历史命令
    [RelayCommand]
    private void ViewHistory()
    {
        if (CurrentPatient == null) return;
        // 打开患者历史记录对话框
    }
}
```

### 4.2 PatientSelectionViewModel 简化

**目标**: 从581行降至<400行

**保留功能**:
- 待诊队列管理
- 患者搜索
- 导航跳转

**移除到控件**:
- UI状态管理(通过DependencyProperty)
- 列表渲染逻辑

```csharp
public partial class PatientSelectionViewModel : ViewModelBase
{
    // 待诊队列数据(绑定到PendingQueueControl)
    public ObservableCollection<PendingPatientDto> PendingQueue =>
        _pendingQueueManager.PendingQueue;

    // 患者列表数据(绑定到PatientSearchControl)
    [ObservableProperty]
    private ObservableCollection<PatientListDto> _patients = new();

    // 选中患者
    [ObservableProperty]
    private PatientListDto? _selectedPatient;

    // 搜索命令
    [RelayCommand]
    private async Task SearchAsync(string keyword) { ... }

    // 选择患者命令
    [RelayCommand]
    private void SelectPatient(PatientListDto patient)
    {
        _eventAggregator.GetEvent<PatientSelectedEvent>()
            .Publish(new PatientSelectedPayload(patient));
        _regionManager.RequestNavigate("MainRegion", "MedicalCaseWorkspaceView");
    }

    // 刷新待诊队列命令
    [RelayCommand]
    private async Task RefreshQueueAsync() => await _pendingQueueManager.RefreshAsync();
}
```

---

## 5. 测试策略

### 5.1 控件单元测试

| 测试项 | 测试内容 |
|--------|----------|
| DependencyProperty绑定 | 验证所有DP双向绑定正常 |
| Command执行 | 验证按钮命令触发正确 |
| 显示模式切换 | 验证DisplayMode切换UI正确 |

### 5.2 集成测试

| 测试场景 | 验证点 |
|----------|--------|
| 患者选择流程 | 搜索 -> 选择 -> 导航 |
| 看诊流程 | 患者卡片显示 -> 诊断输入 -> 处方编辑 |
| 经验方导入 | 点击按钮 -> 对话框弹出 -> 选择方剂 -> 药材导入 |
| 历史医案复制 | 点击按钮 -> 对话框弹出 -> 选择医案 -> 内容复制 |

### 5.3 响应式测试

| 分辨率 | 预期布局 |
|--------|----------|
| 1920x1080 | 完整模式(左25%+右75%) |
| 1440x900 | 折叠模式(左200px+右自适应) |
| 1280x720 | 折叠模式边界 |
| 1024x768 | 下拉模式(顶部选择器) |

---

## 6. 技术决策记录

### 6.1 为什么使用DependencyProperty而非ViewModel

**决策**: 控件使用DependencyProperty暴露接口

**原因**:
1. WPF控件标准模式
2. 支持XAML直接绑定
3. 无需额外ViewModel层
4. 性能更优(绑定引擎优化)

### 6.2 为什么保留两步流程

**决策**: 保留"患者选择 -> 看诊"两步流程

**原因**:
1. 用户明确反馈偏好原有流程
2. 职责分离清晰
3. 复用性更好(前台挂号复用患者选择)
4. 看诊界面更聚焦

### 6.3 为什么不把待诊队列移到看诊界面

**决策**: 待诊队列保留在患者选择界面

**原因**:
1. 用户明确要求保留原有流程
2. 待诊队列是"选择"阶段的功能
3. 看诊界面聚焦于诊断和处方
4. 避免界面过于复杂

---

**文档版本**: v1.0
**最后更新**: 2025-12-25
