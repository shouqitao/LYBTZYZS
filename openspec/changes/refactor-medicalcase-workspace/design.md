# Design: 看诊工作台布局重构 V2

**Change ID**: refactor-medicalcase-workspace
**设计类型**: UI架构 + 控件设计
**创建时间**: 2025-12-25
**更新时间**: 2026-01-04
**版本**: V2

---

## 1. 架构概览

### 1.1 四大核心控件

```
LYBT.Desktop.MedicalCase/Controls/
├── MedicalCaseEditControl.xaml(.cs)    # 新建 - 医案编辑表单
└── MedicalCaseViewControl.xaml(.cs)    # 新建 - 医案只读预览

LYBT.Desktop.Infrastructure/Controls/
├── PatientInfoCardControl.xaml(.cs)    # 已有 - 患者信息卡片
└── PendingQueueControl.xaml(.cs)       # 已有 - 待诊队列控件

LYBT.Desktop.Herbs/Controls/
├── HerbList/HerbListControl.xaml(.cs)  # 已有 - 药材列表控件
└── HerbItem/HerbItemControl.xaml(.cs)  # 已有 - 单个药材项控件
```

### 1.2 MedicalCaseWorkspaceView 布局

```
┌─────────────────────────────────────────────────────────────────────┐
│                       MedicalCaseWorkspaceView                       │
│  ┌──────────────────┐  ┌────────────────────────────────────────┐  │
│  │PatientInfoCard   │  │     [经验方] [历史处方] [清空] ←右上角 │  │
│  │Control           │  │  ┌────────────────────────────────────┐│  │
│  │                  │  │  │ 诊断区 (固定高度120px)             ││  │
│  │  - 姓名/性别/年龄│  │  │ Row1: 现病史                       ││  │
│  │  - 挂号时间      │  │  │ Row2: 舌诊 | 脉诊                  ││  │
│  │  - [查看历史]    │  │  │ Row3: 中医诊断*                    ││  │
│  ├──────────────────┤  │  ├────────────────────────────────────┤│  │
│  │PendingQueue      │  │  │ 处方区 (占剩余空间)                ││  │
│  │Control           │  │  │ HerbListControl (4列)              ││  │
│  │                  │  │  │   药材1  药材2  药材3  药材4       ││  │
│  │  - 待诊患者列表  │  │  │   药材5  药材6  ...                ││  │
│  │  - [刷新]        │  │  ├────────────────────────────────────┤│  │
│  │                  │  │  │ 共X味 | 付数/用法/单价 信息区      ││  │
│  │  左侧 25%        │  │  └────────────────────────────────────┘│  │
│  └──────────────────┘  │              右侧 75%                   │  │
└─────────────────────────────────────────────────────────────────────┘
```

**工具栏位置**: 右上角(诊断区上方)，包含经验方、历史处方、清空按钮

### 1.3 数据流设计

```
┌──────────────────────────┐
│MedicalCaseWorkspace      │
│ViewModel                 │
│  - CurrentPatient        │────────▶ PatientInfoCardControl.Patient
│  - PendingQueue          │────────▶ PendingQueueControl.PendingQueue
│  - Consultation          │────────▶ MedicalCaseEditControl.Consultation
│  - PrescriptionPanel     │────────▶ MedicalCaseEditControl.HerbItems
└──────────────────────────┘
```

---

## 2. 控件详细设计

### 2.1 MedicalCaseEditControl

**职责**: 统一的医案编辑表单(诊断+处方)

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Controls.MedicalCaseEditControl">
    <!-- OpenSpec: refactor-medicalcase-workspace V2 -->
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 右上角工具栏 -->
            <RowDefinition Height="Auto"/>  <!-- 诊断区约120px -->
            <RowDefinition Height="*"/>     <!-- 处方区 -->
        </Grid.RowDefinitions>

        <!-- 右上角工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,0,0,8">
            <Button Content="经验方"
                    Command="{Binding ImportFormulaCommand}"
                    Style="{StaticResource SecondaryButtonStyle}"
                    Margin="0,0,8,0"/>
            <Button Content="历史处方"
                    Command="{Binding ImportHistoryCommand}"
                    Style="{StaticResource SecondaryButtonStyle}"
                    Margin="0,0,8,0"/>
            <Button Content="清空"
                    Command="{Binding ClearAllCommand}"
                    Style="{StaticResource LinkButtonStyle}"
                    Foreground="#DC3545"/>
        </StackPanel>

        <!-- 诊断区 -->
        <Border Grid.Row="1" Style="{StaticResource SectionBorder}" Margin="0,0,0,8">
            <StackPanel Margin="12">
                <!-- Row1: 现病史 -->
                <Grid Height="36" Margin="0,0,0,8">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="70"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Text="现病史" VerticalAlignment="Center"/>
                    <TextBox Grid.Column="1"
                             Text="{Binding Consultation.History, Mode=TwoWay}"
                             Style="{StaticResource EditableTextBoxStyle}"/>
                </Grid>

                <!-- Row2: 舌诊 + 脉诊 -->
                <Grid Height="36" Margin="0,0,0,8">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="16"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid Grid.Column="0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="50"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="舌诊" VerticalAlignment="Center"/>
                        <TextBox Grid.Column="1"
                                 Text="{Binding Consultation.TongueDiagnosis, Mode=TwoWay}"
                                 Style="{StaticResource EditableTextBoxStyle}"/>
                    </Grid>
                    <Grid Grid.Column="2">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="50"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="脉诊" VerticalAlignment="Center"/>
                        <TextBox Grid.Column="1"
                                 Text="{Binding Consultation.PulseDiagnosis, Mode=TwoWay}"
                                 Style="{StaticResource EditableTextBoxStyle}"/>
                    </Grid>
                </Grid>

                <!-- Row3: 中医诊断(必填) -->
                <Grid Height="36">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="70"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock VerticalAlignment="Center">
                        <Run Text="中医诊断"/><Run Text="*" Foreground="Red"/>
                    </TextBlock>
                    <TextBox Grid.Column="1"
                             Text="{Binding Consultation.Diagnosis, Mode=TwoWay}"
                             Style="{StaticResource ValidatingTextBoxStyle}"/>
                </Grid>
            </StackPanel>
        </Border>

        <!-- 处方区 -->
        <Border Grid.Row="2" Style="{StaticResource SectionBorder}">
            <Grid Margin="12">
                <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>     <!-- 药材列表 -->
                    <RowDefinition Height="Auto"/>  <!-- 底部信息栏 -->
                </Grid.RowDefinitions>

                <!-- 药材列表 -->
                <herbList:HerbListControl Grid.Row="0"
                    AllHerbs="{Binding AllHerbs}"
                    HerbItems="{Binding HerbItems, Mode=TwoWay}"
                    IsEditMode="True"
                    Columns="4"/>

                <!-- 底部信息栏 -->
                <Grid Grid.Row="1" Margin="0,8,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>  <!-- 药材数 -->
                        <ColumnDefinition Width="Auto"/>  <!-- 付数 -->
                        <ColumnDefinition Width="Auto"/>  <!-- 用法 -->
                        <ColumnDefinition Width="*"/>     <!-- 单价 -->
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" VerticalAlignment="Center" Margin="0,0,24,0">
                        <Run Text="共"/>
                        <Run Text="{Binding ValidHerbCount}" FontWeight="SemiBold" Foreground="#2196F3"/>
                        <Run Text="味药材"/>
                    </TextBlock>
                    <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="0,0,24,0">
                        <TextBlock Text="付数:" VerticalAlignment="Center"/>
                        <TextBox Width="60" Text="{Binding Doses}" Margin="8,0,0,0"/>
                        <TextBlock Text="付" VerticalAlignment="Center" Margin="4,0,0,0"/>
                    </StackPanel>
                    <StackPanel Grid.Column="2" Orientation="Horizontal" Margin="0,0,24,0">
                        <TextBlock Text="用法:" VerticalAlignment="Center"/>
                        <ComboBox Width="120" SelectedItem="{Binding Usage}" Margin="8,0,0,0"/>
                    </StackPanel>
                    <StackPanel Grid.Column="3" Orientation="Horizontal" HorizontalAlignment="Right">
                        <TextBlock Text="单价:" VerticalAlignment="Center"/>
                        <TextBlock Text="{Binding TotalPrice, StringFormat='{}{0:F2}元'}"
                                   VerticalAlignment="Center" Margin="8,0,0,0"
                                   FontWeight="SemiBold" Foreground="#28A745"/>
                    </StackPanel>
                </Grid>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

**代码后台**:
```csharp
public partial class MedicalCaseEditControl : UserControl
{
    #region Consultation属性
    public static readonly DependencyProperty ConsultationProperty =
        DependencyProperty.Register(
            nameof(Consultation),
            typeof(ConsultationEditModel),
            typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ConsultationEditModel? Consultation
    {
        get => (ConsultationEditModel?)GetValue(ConsultationProperty);
        set => SetValue(ConsultationProperty, value);
    }
    #endregion

    #region HerbItems属性
    public static readonly DependencyProperty HerbItemsProperty =
        DependencyProperty.Register(
            nameof(HerbItems),
            typeof(ObservableCollection<HerbItemDto>),
            typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ObservableCollection<HerbItemDto>? HerbItems
    {
        get => (ObservableCollection<HerbItemDto>?)GetValue(HerbItemsProperty);
        set => SetValue(HerbItemsProperty, value);
    }
    #endregion

    #region AllHerbs属性
    public static readonly DependencyProperty AllHerbsProperty =
        DependencyProperty.Register(
            nameof(AllHerbs),
            typeof(IEnumerable<HerbDto>),
            typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public IEnumerable<HerbDto>? AllHerbs
    {
        get => (IEnumerable<HerbDto>?)GetValue(AllHerbsProperty);
        set => SetValue(AllHerbsProperty, value);
    }
    #endregion

    #region Commands
    public static readonly DependencyProperty ImportFormulaCommandProperty =
        DependencyProperty.Register(nameof(ImportFormulaCommand), typeof(ICommand),
            typeof(MedicalCaseEditControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ImportHistoryCommandProperty =
        DependencyProperty.Register(nameof(ImportHistoryCommand), typeof(ICommand),
            typeof(MedicalCaseEditControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ClearAllCommandProperty =
        DependencyProperty.Register(nameof(ClearAllCommand), typeof(ICommand),
            typeof(MedicalCaseEditControl), new PropertyMetadata(null));

    public ICommand? ImportFormulaCommand
    {
        get => (ICommand?)GetValue(ImportFormulaCommandProperty);
        set => SetValue(ImportFormulaCommandProperty, value);
    }

    public ICommand? ImportHistoryCommand
    {
        get => (ICommand?)GetValue(ImportHistoryCommandProperty);
        set => SetValue(ImportHistoryCommandProperty, value);
    }

    public ICommand? ClearAllCommand
    {
        get => (ICommand?)GetValue(ClearAllCommandProperty);
        set => SetValue(ClearAllCommandProperty, value);
    }
    #endregion
}
```

### 2.2 MedicalCaseViewControl

**职责**: 医案只读预览

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Controls.MedicalCaseViewControl">
    <!-- OpenSpec: refactor-medicalcase-workspace -->
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 诊断信息 -->
            <RowDefinition Height="*"/>     <!-- 处方内容 -->
            <RowDefinition Height="Auto"/>  <!-- 底部信息 -->
        </Grid.RowDefinitions>

        <!-- 诊断信息 -->
        <Border Grid.Row="0" Style="{StaticResource SectionBorder}" Margin="0,0,0,8">
            <StackPanel Margin="12">
                <TextBlock Margin="0,0,0,8">
                    <Run Text="现病史: " FontWeight="SemiBold"/>
                    <Run Text="{Binding MedicalCase.History}"/>
                </TextBlock>
                <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                    <TextBlock>
                        <Run Text="舌诊: " FontWeight="SemiBold"/>
                        <Run Text="{Binding MedicalCase.TongueDiagnosis}"/>
                    </TextBlock>
                    <TextBlock Margin="24,0,0,0">
                        <Run Text="脉诊: " FontWeight="SemiBold"/>
                        <Run Text="{Binding MedicalCase.PulseDiagnosis}"/>
                    </TextBlock>
                </StackPanel>
                <TextBlock>
                    <Run Text="中医诊断: " FontWeight="SemiBold"/>
                    <Run Text="{Binding MedicalCase.Diagnosis}"/>
                </TextBlock>
            </StackPanel>
        </Border>

        <!-- 处方内容 -->
        <Border Grid.Row="1" Style="{StaticResource SectionBorder}">
            <Grid Margin="12">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="处方内容" FontWeight="SemiBold" Margin="0,0,0,8"/>
                <herbList:HerbListControl Grid.Row="1"
                    HerbItems="{Binding MedicalCase.HerbItems}"
                    IsEditMode="False"
                    Columns="4"/>
            </Grid>
        </Border>

        <!-- 底部信息 -->
        <Border Grid.Row="2" Style="{StaticResource SectionBorder}" Margin="0,8,0,0">
            <StackPanel Orientation="Horizontal" Margin="12">
                <TextBlock>
                    <Run Text="付数: "/>
                    <Run Text="{Binding MedicalCase.Doses}" FontWeight="SemiBold"/>
                    <Run Text="付"/>
                </TextBlock>
                <TextBlock Margin="24,0,0,0">
                    <Run Text="用法: "/>
                    <Run Text="{Binding MedicalCase.Usage}"/>
                </TextBlock>
                <TextBlock Margin="24,0,0,0">
                    <Run Text="总价: "/>
                    <Run Text="{Binding MedicalCase.TotalPrice, StringFormat='{}{0:F2}元'}" FontWeight="SemiBold"/>
                </TextBlock>
                <Button Content="打印"
                        Command="{Binding PrintCommand}"
                        Visibility="{Binding ShowPrintButton, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Style="{StaticResource PrimaryButtonStyle}"
                        Margin="24,0,0,0"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

## 3. MedicalCaseWorkspaceView 布局调整

### 3.1 新布局实现

```xml
<UserControl x:Class="LYBT.Desktop.Clinical.Views.MedicalCaseWorkspaceView">
    <!-- OpenSpec: refactor-medicalcase-workspace V2 -->
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="25*" MinWidth="280" MaxWidth="350"/>
            <ColumnDefinition Width="75*"/>
        </Grid.ColumnDefinitions>

        <!-- 左侧: 患者信息 + 待诊队列 -->
        <Grid Grid.Column="0" Margin="12">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- 患者信息卡片 -->
                <RowDefinition Height="*"/>     <!-- 待诊队列 -->
            </Grid.RowDefinitions>

            <!-- 患者信息卡片 -->
            <controls:PatientInfoCardControl Grid.Row="0"
                Patient="{Binding CurrentPatient}"
                DisplayMode="Compact"
                ShowHistoryButton="True"
                HistoryCommand="{Binding ViewHistoryCommand}"
                Margin="0,0,0,12"/>

            <!-- 待诊队列 (可折叠) -->
            <controls:PendingQueueControl Grid.Row="1"
                PendingQueue="{Binding PendingQueue}"
                SelectedItem="{Binding SelectedPendingCase}"
                SelectCommand="{Binding SelectPendingCaseCommand}"
                RefreshCommand="{Binding RefreshQueueCommand}"
                IsCompactMode="True"/>
        </Grid>

        <!-- 右侧: 医案编辑表单 -->
        <medicalCase:MedicalCaseEditControl Grid.Column="1"
            Consultation="{Binding Consultation}"
            HerbItems="{Binding PrescriptionPanel.HerbItems}"
            AllHerbs="{Binding AllHerbs}"
            ImportFormulaCommand="{Binding PrescriptionPanel.OpenFormulaImportDialogCommand}"
            ImportHistoryCommand="{Binding PrescriptionPanel.OpenHistoryCopyDialogCommand}"
            ClearAllCommand="{Binding PrescriptionPanel.ClearAllCommand}"
            Margin="0,12,12,12"/>
    </Grid>
</UserControl>
```

---

## 4. ViewModel调整

### 4.1 MedicalCaseWorkspaceViewModel 修改

```csharp
public partial class MedicalCaseWorkspaceViewModel : ViewModelBase
{
    // 患者信息
    [ObservableProperty]
    private PatientDto? _currentPatient;

    // 诊断数据
    [ObservableProperty]
    private ConsultationEditModel _consultation = new();

    // 处方面板(含经验方/历史命令)
    public PrescriptionPanelViewModel PrescriptionPanel { get; }

    // === V2新增: 待诊队列 ===
    public ObservableCollection<PendingCaseDto> PendingQueue =>
        _pendingQueueManager.PendingQueue;

    [ObservableProperty]
    private PendingCaseDto? _selectedPendingCase;

    // 选择待诊患者命令
    [RelayCommand]
    private async Task SelectPendingCaseAsync(PendingCaseDto? pendingCase)
    {
        if (pendingCase == null) return;

        // 1. 暂存当前医案
        if (HasUnsavedChanges)
        {
            await SaveDraftAsync();
        }

        // 2. 加载新患者医案
        await LoadMedicalCaseAsync(pendingCase.PatientId, pendingCase.MedicalCaseId);
    }

    // 刷新队列命令
    [RelayCommand]
    private async Task RefreshQueueAsync() => await _pendingQueueManager.RefreshAsync();

    // 查看历史命令
    [RelayCommand]
    private void ViewHistory()
    {
        if (CurrentPatient == null) return;
        _regionManager.RequestNavigate("MainRegion", "PatientHistoryView",
            new NavigationParameters { { "PatientId", CurrentPatient.Id } });
    }
}
```

---

## 5. 测试策略

### 5.1 控件单元测试

| 测试项 | 测试内容 |
|--------|----------|
| MedicalCaseEditControl绑定 | 验证Consultation/HerbItems双向绑定 |
| MedicalCaseViewControl渲染 | 验证只读数据正确显示 |
| 诊断区布局 | 验证3行布局正确 |
| HerbListControl集成 | 验证4列网格显示 |

### 5.2 集成测试

| 测试场景 | 验证点 |
|----------|--------|
| 新建医案流程 | 诊断输入 -> 药材添加 -> 保存 |
| 待诊队列切换 | 选择患者 -> 暂存 -> 加载新医案 |
| 经验方导入 | 点击按钮 -> 选择方剂 -> 药材导入 |
| 历史医案复制 | 点击按钮 -> 选择医案 -> 内容复制 |

---

## 6. 技术决策记录

### 6.1 为什么统一诊断和处方面板

**决策**: 将诊断区和处方区合并为单一表单

**原因**:
1. 诊断字段已精简至4个，不需要独立面板
2. 连续表单操作更流畅，无需切换焦点
3. 处方区获得更多空间(约85%)
4. 符合用户反馈的操作习惯

### 6.2 为什么将待诊队列移入看诊界面

**决策**: 待诊队列从患者选择界面移到看诊界面左侧

**原因**:
1. 减少界面切换，提高效率
2. 看诊时可快速切换患者
3. 左侧空间利用更充分
4. 支持暂存后切换患者

### 6.3 诊断区3行布局设计

**决策**: 现病史/舌诊+脉诊/中医诊断 三行布局

**原因**:
1. 现病史独占一行(内容较多)
2. 舌诊+脉诊同行(内容较少，相关性强)
3. 中医诊断独占一行(必填项，突出显示)
4. 固定高度约120px，节省空间

---

**文档版本**: v2.0
**最后更新**: 2026-01-04
