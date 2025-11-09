# 待看诊队列功能设计文档

> **文档版本**：v2.0
> **创建时间**：2025-10-23
> **最后更新**：2025-10-23
> **状态**：待审批
> **需求文档**：`docs/explanation/requirements/pending-medicalcase-queue-requirements.md`
> **讨论文档**：
> - `docs/explanation/architecture/client/pending-medicalcase-queue-discussion.md`
> - `docs/explanation/architecture/client/pending-medicalcase-queue-ui-implementation-discussion.md`

---

## 🏗️ 架构设计

### 组件关系图（1080P三区域布局）

```
┌───────────────────────────────────────────────────────────────────────┐
│                 PatientSelectionView (1080P)                    │
├────────────────┬──────────────────────────────────────────────────────┤
│  左侧300px   │  右侧1460px                                         │
│                │                                                   │
│ ┌────────────┐ │  ┌──────────────────────────────────────────────┐ │
│ │ 患者信息   │ │  │ 搜索区域：搜索框+🔍搜索+新建患者+开始诊断 │ │
│ │ 180px高   │ │  └──────────────────────────────────────────────┘ │
│ └────────────┘ │  ┌──────────────────────────────────────────────┐ │
│                │  │ 全部患者DataGrid                          │ │
│ ┌────────────┐ │  │ - 单击=选中 / 双击=开始诊断              │ │
│ │ 待看诊队列 │ │  │ - 移除行内“选择”按钮                      │ │
│ │ 740px高   │ │  └──────────────────────────────────────────────┘ │
│ └────────────┘ │  ┌──────────────────────────────────────────────┐ │
│                │  │ 分页控件（每项20条）                      │ │
│                │  └──────────────────────────────────────────────┘ │
└────────────────┴──────────────────────────────────────────────────────┘
                            ↓
            NavigationParameters (判断逻辑在PatientSelectionViewModel)
            { PatientId, MedicalCaseId?, Action }
            Action: "Continue" | "CreateNew" | "CloseOnly"
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  MedicalCaseFlowView                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                 │
│  │ Step 1   │  │ Step 2   │  │ Step 3   │                 │
│  │ 诊断录入 │→ │ 诊断确认 │→ │ 处方编辑 │                 │
│  └──────────┘  └──────────┘  └──────────┘                 │
│                                                              │
│  [返回] [上一步] [暂存] [下一步] [完成]                     │
└─────────────────────────────────────────────────────────────┘
```

### 数据流设计（含本地缓存优化）

**核心优化**：本地缓存 `PendingCaseCache = Dictionary<Guid PatientId, Guid MedicalCaseId>` 减少重复API调用

```
1. 加载待看诊队列 + 构建缓存：
   Client → GET /api/medicalcases/pending
   Server → 查询Status=Active的MedicalCase（按CreatedAt升序）
   Server → 返回PendingMedicalCaseDto列表（含MedicalCaseId）
   Client → 绑定到PendingQueue
   Client → 构建PendingCaseCache：{ PatientId → MedicalCaseId }
   
   效率提升：队列中10个患者 → 这10个患者后续选择时0次额外API调用

2. 待看诊队列双击（⚠️ 关键逻辑 - MedicalCaseId null检查）：
   Client → 双击待看诊患者
   Client → 检查 PendingMedicalCaseDto.MedicalCaseId 是否为 null
   
   分支A（暂存医案）：
   ✅ MedicalCaseId 有值 → 无需弹窗，直接继续看诊
   Client → NavigationParameters { PatientId, MedicalCaseId, Action="Continue" }
   Client → 导航到MedicalCaseFlowView
   MedicalCaseFlowView → GET /api/medicalcases/{id}/details
   Server → Include Consultation + Prescription
   Client → 加载到Step1ViewModel和Step3ViewModel
   
   分支B（已挂号，未来扩展）：
   ⚠️ MedicalCaseId 为 null → 已挂号患者 → 弹窗询问操作
   （当前阶段：所有待看诊都是暂存医案，MedicalCaseId不会为null）
   （未来挂号集成后：挂号患者MedicalCaseId=null，走新建医案流程）

3. 全部患者列表双击 + 缓存查询：
   Client → 双击全部患者列表患者
   Client → 查询本地缓存 PendingCaseCache[PatientId]
   
   分支A（缓存命中）：
   ✅ 缓存中找到 MedicalCaseId → 弹窗显示三选一
   效率提升：0次API调用
   
   分支B（缓存未命中）：
   ❌ 缓存中未找到 → 调用 GET /api/medicalcases/unfinished/{patientId}
   Server → 查询 Status=Active 的 MedicalCase
   若无未完成医案 → 直接创建新医案（场景6）
   若有未完成医案 → 弹窗显示三选一

4. 用户选择"继续看诊"（从弹窗）：
   Client → NavigationParameters { PatientId, MedicalCaseId, Action="Continue" }
   Client → 导航到MedicalCaseFlowView
   （后续流程同场景2分支A）

5. 用户选择"新建医案"（从弹窗）：
   Client → PUT /api/medicalcases/{oldId}，Status=Closed
   Server → 检测状态变更 → 级联删除 Consultation/Prescription
   Client → 从缓存删除该患者：PendingCaseCache.Remove(PatientId)
   Client → POST /api/medicalcases，创建新医案
   Server → 返回新 MedicalCaseId
   Client → NavigationParameters { PatientId, MedicalCaseId, Action="CreateNew" }
   Client → 导航到MedicalCaseFlowView

6. 用户选择"关闭医案"（从弹窗）：
   Client → PUT /api/medicalcases/{oldId}，Status=Closed
   Server → 检测状态变更 → 级联删除 Consultation/Prescription
   Client → 从缓存删除该患者：PendingCaseCache.Remove(PatientId)
   Client → 刷新待看诊队列（重新调用场景1）
   Client → 显示成功消息，停留在PatientSelectionView

7. 缓存失效时机：
   - 用户点击"关闭医案" → 从缓存删除该患者
   - 用户点击"新建医案" → 从缓存删除该患者（旧医案关闭）
   - 刷新待看诊列表 → 重建整个缓存（场景1）
   - 导航离开患者选择界面 → 清空缓存（可选优化）
```

**效率对比**：

| 场景 | 传统方式（无缓存） | 本地缓存优化 | 提升 |
|-----|------------------|-------------|------|
| 待看诊队列10个患者 | 初始1次 + 选择时10次 = 11次API | 初始1次 + 选择时0次 = 1次API | 91% ↓ |
| 搜索其他患者 | 1次API | 1次API | 无变化 |
| 刷新队列 | 1次API | 1次API + 重建缓存 | 无额外开销 |

---

## 🎨 UI设计

### XAML布局（PatientSelectionView.xaml）

**核心设计**：三区域布局（左侧300px分两行 + 右侧自适应）

```xaml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.PatientSelectionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid Background="#F9F9F9">
        <Grid.RowDefinitions>
            <RowDefinition Height="60"/>   <!-- 顶部导航栏 -->
            <RowDefinition Height="*"/>    <!-- 主内容区（三区域布局） -->
            <RowDefinition Height="80"/>   <!-- 底部操作栏 -->
        </Grid.RowDefinitions>

        <!-- 顶部导航栏 -->
        <Border Grid.Row="0" Background="White" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid Margin="20,0">
                <TextBlock Text="患者选择" FontSize="20" FontWeight="Bold"
                          VerticalAlignment="Center" Foreground="#2C3E50"/>
                <Button Content="返回主页"
                       Command="{Binding BackToHomeCommand}"
                       HorizontalAlignment="Right"
                       Style="{StaticResource SecondaryButtonStyle}"/>
            </Grid>
        </Border>

        <!-- 主内容区：三区域布局（左300px双行 + 右自适应） -->
        <Grid Grid.Row="1" Margin="20">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300"/>  <!-- 左侧：患者信息 + 待看诊队列 -->
                <ColumnDefinition Width="10"/>   <!-- 分隔 -->
                <ColumnDefinition Width="*"/>    <!-- 右侧：全部患者列表 -->
            </Grid.ColumnDefinitions>

            <!-- 左侧：患者信息详情 + 待看诊队列 -->
            <Grid Grid.Column="0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="180"/>  <!-- 患者信息详情 -->
                    <RowDefinition Height="10"/>   <!-- 分隔 -->
                    <RowDefinition Height="740"/>  <!-- 待看诊队列 -->
                </Grid.RowDefinitions>

                <!-- 患者信息详情模块 -->
                <Border Grid.Row="0" Background="White" Padding="15" CornerRadius="5"
                       BorderBrush="#E0E0E0" BorderThickness="1">
                    <Grid Visibility="{Binding CurrentPatient, Converter={StaticResource NullToVisibilityConverter}}">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>

                        <!-- 标题 -->
                        <TextBlock Grid.Row="0" Text="患者信息" FontWeight="Bold" FontSize="14"
                                  Foreground="#2C3E50" Margin="0,0,0,10"/>

                        <!-- 患者详细信息 -->
                        <StackPanel Grid.Row="1" Spacing="8">
                            <!-- 姓名 -->
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="姓　　名：" Width="80" Foreground="#7F8C8D" FontSize="12"/>
                                <TextBlock Text="{Binding CurrentPatient.Name}" FontWeight="Bold" FontSize="12"/>
                            </StackPanel>

                            <!-- 手机号（脱敏） -->
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="手机号：" Width="80" Foreground="#7F8C8D" FontSize="12"/>
                                <TextBlock Text="{Binding CurrentPatient.PhoneMasked}" FontSize="12"/>
                            </StackPanel>

                            <!-- 年龄 -->
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="年　　龄：" Width="80" Foreground="#7F8C8D" FontSize="12"/>
                                <TextBlock Text="{Binding CurrentPatient.Age, StringFormat='{}{0}岁'}" FontSize="12"/>
                            </StackPanel>

                            <!-- 地址 -->
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="地　　址：" Width="80" Foreground="#7F8C8D" FontSize="12" VerticalAlignment="Top"/>
                                <TextBlock Text="{Binding CurrentPatient.Address}" FontSize="12" TextWrapping="Wrap" MaxWidth="180"/>
                            </StackPanel>

                            <!-- 过敏史 -->
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="过敏史：" Width="80" Foreground="#7F8C8D" FontSize="12" VerticalAlignment="Top"/>
                                <TextBlock Text="{Binding CurrentPatient.Allergies, TargetNullValue='无'}" 
                                          FontSize="12" TextWrapping="Wrap" MaxWidth="180" Foreground="#E74C3C"/>
                            </StackPanel>
                        </StackPanel>
                    </Grid>

                    <!-- 未选择患者时的提示 -->
                    <TextBlock Text="请从列表中选择患者" 
                              FontSize="13" Foreground="#95A5A6" 
                              HorizontalAlignment="Center" VerticalAlignment="Center"
                              Visibility="{Binding CurrentPatient, Converter={StaticResource NullToVisibilityConverter}, ConverterParameter=Inverse}"/>
                </Border>

                <!-- 待看诊队列 -->
                <Border Grid.Row="2" Background="White" Padding="15" CornerRadius="5"
                       BorderBrush="#E0E0E0" BorderThickness="1">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>

                        <!-- 标题 -->
                        <TextBlock Grid.Row="0" Text="待看诊队列" FontWeight="Bold" FontSize="14"
                                  Foreground="#2C3E50" Margin="0,0,0,10"/>

                        <!-- 待看诊列表 -->
                        <DataGrid Grid.Row="1"
                                 ItemsSource="{Binding PendingQueue}"
                                 SelectedItem="{Binding SelectedPendingPatient}"
                                 AutoGenerateColumns="False"
                                 IsReadOnly="True"
                                 SelectionMode="Single"
                                 CanUserAddRows="False"
                                 CanUserDeleteRows="False"
                                 CanUserResizeRows="False"
                                 HeadersVisibility="Column"
                                 GridLinesVisibility="None"
                                 BorderThickness="0"
                                 Background="Transparent">

                            <!-- 双击事件：进入看诊 -->
                            <DataGrid.InputBindings>
                                <MouseBinding Gesture="LeftDoubleClick"
                                            Command="{Binding StartConsultationCommand}"/>
                            </DataGrid.InputBindings>

                            <DataGrid.Columns>
                                <!-- 患者姓名 -->
                                <DataGridTextColumn Header="姓名"
                                                  Binding="{Binding PatientName}"
                                                  Width="80"/>

                                <!-- 手机号（脱敏） -->
                                <DataGridTextColumn Header="电话"
                                                  Binding="{Binding PhoneMasked}"
                                                  Width="100"/>

                                <!-- 状态 -->
                                <DataGridTemplateColumn Header="状态" Width="*">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <TextBlock Text="{Binding Type, Converter={StaticResource PendingTypeConverter}}"
                                                      FontSize="11"
                                                      Foreground="#F39C12"
                                                      HorizontalAlignment="Center"/>
                                        </DataTemplate>
                                    </DataGridTemplateColumn.CellTemplate>
                                </DataGridTemplateColumn>
                            </DataGrid.Columns>

                            <!-- 行样式：选中高亮 -->
                            <DataGrid.RowStyle>
                                <Style TargetType="DataGridRow">
                                    <Setter Property="Background" Value="Transparent"/>
                                    <Setter Property="BorderThickness" Value="0"/>
                                    <Style.Triggers>
                                        <Trigger Property="IsSelected" Value="True">
                                            <Setter Property="Background" Value="#E3F2FD"/>
                                        </Trigger>
                                        <Trigger Property="IsMouseOver" Value="True">
                                            <Setter Property="Background" Value="#F5F5F5"/>
                                        </Trigger>
                                    </Style.Triggers>
                                </Style>
                            </DataGrid.RowStyle>
                        </DataGrid>
                    </Grid>
                </Border>
            </Grid>

            <!-- 右侧：全部患者列表 -->
            <Border Grid.Column="2" Background="White" Padding="15" CornerRadius="5"
                   BorderBrush="#E0E0E0" BorderThickness="1">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>  <!-- 标题 -->
                        <RowDefinition Height="Auto"/>  <!-- 搜索框和按钮区 -->
                        <RowDefinition Height="*"/>     <!-- 患者列表 -->
                        <RowDefinition Height="Auto"/>  <!-- 分页控件 -->
                    </Grid.RowDefinitions>

                    <!-- 标题 -->
                    <TextBlock Grid.Row="0" Text="全部患者" FontWeight="Bold" FontSize="16"
                              Foreground="#2C3E50" Margin="0,0,0,15"/>

                    <!-- 搜索框和按钮区 -->
                    <Grid Grid.Row="1" Margin="0,0,0,15">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>    <!-- 搜索框 -->
                            <ColumnDefinition Width="Auto"/> <!-- 🔍搜索按钮 -->
                            <ColumnDefinition Width="Auto"/> <!-- 新建患者按钮 -->
                            <ColumnDefinition Width="Auto"/> <!-- 开始诊断按钮 -->
                        </Grid.ColumnDefinitions>

                        <!-- 搜索框 -->
                        <TextBox Grid.Column="0"
                                Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                                Height="35"
                                Padding="10,0"
                                VerticalContentAlignment="Center"
                                BorderBrush="#BDC3C7"
                                BorderThickness="1">
                            <TextBox.InputBindings>
                                <KeyBinding Key="Return" Command="{Binding SearchCommand}"/>
                            </TextBox.InputBindings>
                        </TextBox>

                        <!-- 🔍搜索按钮 -->
                        <Button Grid.Column="1"
                               Content="🔍 搜索"
                               Command="{Binding SearchCommand}"
                               Width="90" Height="35" Margin="10,0,0,0"
                               Style="{StaticResource PrimaryButtonStyle}"/>

                        <!-- 新建患者按钮 -->
                        <Button Grid.Column="2"
                               Content="+ 新建患者"
                               Command="{Binding CreatePatientCommand}"
                               Width="100" Height="35" Margin="10,0,0,0"
                               Style="{StaticResource SecondaryButtonStyle}"/>

                        <!-- 开始诊断按钮 -->
                        <Button Grid.Column="3"
                               Content="开始诊断"
                               Command="{Binding StartConsultationCommand}"
                               Width="100" Height="35" Margin="10,0,0,0"
                               Style="{StaticResource PrimaryButtonStyle}"
                               IsEnabled="{Binding CurrentPatient, Converter={StaticResource NullToBooleanConverter}}"/>
                    </Grid>

                    <!-- 患者列表 -->
                    <DataGrid Grid.Row="2"
                             ItemsSource="{Binding Patients}"
                             SelectedItem="{Binding SelectedPatient}"
                             AutoGenerateColumns="False"
                             IsReadOnly="True"
                             SelectionMode="Single"
                             CanUserAddRows="False"
                             CanUserDeleteRows="False"
                             CanUserResizeRows="False"
                             HeadersVisibility="Column"
                             GridLinesVisibility="Horizontal"
                             BorderBrush="#E0E0E0"
                             BorderThickness="1">

                        <!-- 双击事件：进入看诊 -->
                        <DataGrid.InputBindings>
                            <MouseBinding Gesture="LeftDoubleClick"
                                        Command="{Binding StartConsultationCommand}"/>
                        </DataGrid.InputBindings>

                        <DataGrid.Columns>
                            <!-- 姓名 -->
                            <DataGridTextColumn Header="姓名" 
                                              Binding="{Binding Name}" 
                                              Width="100"/>

                            <!-- 性别 -->
                            <DataGridTextColumn Header="性别" 
                                              Binding="{Binding Gender}" 
                                              Width="60"/>

                            <!-- 年龄 -->
                            <DataGridTextColumn Header="年龄" 
                                              Binding="{Binding Age}" 
                                              Width="60"/>

                            <!-- 电话 -->
                            <DataGridTextColumn Header="电话" 
                                              Binding="{Binding Phone}" 
                                              Width="120"/>

                            <!-- 创建时间 -->
                            <DataGridTextColumn Header="创建时间"
                                              Binding="{Binding CreatedAt, StringFormat='{}{0:yyyy-MM-dd}'}"
                                              Width="*"/>
                        </DataGrid.Columns>

                        <!-- 行样式 -->
                        <DataGrid.RowStyle>
                            <Style TargetType="DataGridRow">
                                <Setter Property="Height" Value="40"/>
                                <Style.Triggers>
                                    <Trigger Property="IsSelected" Value="True">
                                        <Setter Property="Background" Value="#E3F2FD"/>
                                    </Trigger>
                                    <Trigger Property="IsMouseOver" Value="True">
                                        <Setter Property="Background" Value="#F5F5F5"/>
                                    </Trigger>
                                </Style.Triggers>
                            </Style>
                        </DataGrid.RowStyle>
                    </DataGrid>

                    <!-- 分页控件 -->
                    <Grid Grid.Row="3" Margin="0,15,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>

                        <!-- 分页信息 -->
                        <TextBlock Grid.Column="0" 
                                  VerticalAlignment="Center"
                                  Foreground="#7F8C8D" FontSize="12">
                            <Run Text="共 "/>
                            <Run Text="{Binding TotalCount}" FontWeight="Bold"/>
                            <Run Text=" 条记录，每页 "/>
                            <Run Text="20" FontWeight="Bold"/>
                            <Run Text=" 条"/>
                        </TextBlock>

                        <!-- 分页按钮 -->
                        <StackPanel Grid.Column="1" Orientation="Horizontal">
                            <Button Content="上一页" 
                                   Command="{Binding PreviousPageCommand}"
                                   Width="80" Height="30" Margin="0,0,10,0"
                                   Style="{StaticResource SecondaryButtonStyle}"/>
                            
                            <TextBlock Text="{Binding CurrentPage}" 
                                      VerticalAlignment="Center" 
                                      FontWeight="Bold" FontSize="14"
                                      Margin="10,0"/>
                            
                            <TextBlock Text="/" 
                                      VerticalAlignment="Center" 
                                      Foreground="#BDC3C7" 
                                      Margin="5,0"/>
                            
                            <TextBlock Text="{Binding TotalPages}" 
                                      VerticalAlignment="Center" 
                                      Foreground="#7F8C8D"
                                      Margin="5,0,10,0"/>
                            
                            <Button Content="下一页" 
                                   Command="{Binding NextPageCommand}"
                                   Width="80" Height="30"
                                   Style="{StaticResource SecondaryButtonStyle}"/>
                        </StackPanel>
                    </Grid>
                </Grid>
            </Border>
        </Grid>

        <!-- 底部操作栏 -->
        <Border Grid.Row="2" Background="White" Padding="20"
               BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="返回主页"
                       Command="{Binding BackToHomeCommand}"
                       Width="120" Height="40" Margin="0,0,15,0"
                       Style="{StaticResource SecondaryButtonStyle}"/>

                <Button Content="开始诊断"
                       Command="{Binding StartConsultationCommand}"
                       Width="120" Height="40"
                       Style="{StaticResource PrimaryButtonStyle}"
                       IsEnabled="{Binding CurrentPatient, Converter={StaticResource NullToBooleanConverter}}"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

## 📊 ViewModel设计

### PatientSelectionViewModel.cs

```csharp
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    #region 字段

    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IRegionManager _regionManager;

    #endregion

    #region 属性

    /// <summary>
    /// 待看诊队列
    /// </summary>
    private ObservableCollection<PendingMedicalCaseDto> _pendingQueue = new();
    public ObservableCollection<PendingMedicalCaseDto> PendingQueue
    {
        get => _pendingQueue;
        set => SetProperty(ref _pendingQueue, value);
    }

    /// <summary>
    /// 从待看诊队列选中的患者
    /// </summary>
    private PendingMedicalCaseDto? _selectedPendingPatient;
    public PendingMedicalCaseDto? SelectedPendingPatient
    {
        get => _selectedPendingPatient;
        set
        {
            if (SetProperty(ref _selectedPendingPatient, value) && value != null)
            {
                // 设置"当前选中患者"
                SetCurrentPatientFromPending(value);
            }
        }
    }

    /// <summary>
    /// 全部患者列表
    /// </summary>
    private ObservableCollection<PatientDto> _patients = new();
    public ObservableCollection<PatientDto> Patients
    {
        get => _patients;
        set => SetProperty(ref _patients, value);
    }

    /// <summary>
    /// 从全部患者列表选中的患者
    /// </summary>
    private PatientDto? _selectedPatient;
    public PatientDto? SelectedPatient
    {
        get => _selectedPatient;
        set
        {
            if (SetProperty(ref _selectedPatient, value) && value != null)
            {
                // 设置"当前选中患者"
                CurrentPatient = value;
            }
        }
    }

    /// <summary>
    /// 当前选中患者（核心概念）
    /// </summary>
    private PatientDto? _currentPatient;
    public PatientDto? CurrentPatient
    {
        get => _currentPatient;
        set
        {
            if (SetProperty(ref _currentPatient, value))
            {
                StartConsultationCommand.RaiseCanExecuteChanged();
                Logger.LogInformation("当前选中患者：{PatientName}（ID: {PatientId}）",
                    value?.Name, value?.Id);
            }
        }
    }

    /// <summary>
    /// 搜索关键字
    /// </summary>
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// 待看诊队列缓存（PatientId → MedicalCaseId）
    /// 效率优化：减少重复的API调用
    /// </summary>
    private readonly Dictionary<Guid, Guid> _pendingCaseCache = new();

    #endregion

    #region 命令

    public DelegateCommand BackToHomeCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand StartConsultationCommand { get; }

    #endregion

    #region 构造函数

    public PatientSelectionViewModel(
        IPatientRepository patientRepository,
        IMedicalCaseRepository medicalCaseRepository,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        ISessionManager? sessionManager = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        // 初始化命令
        BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
        SearchCommand = new DelegateCommand(async () => await LoadPatientsAsync());
        StartConsultationCommand = new DelegateCommand(async () => await ExecuteStartConsultationAsync(), CanExecuteStartConsultation)
            .ObservesProperty(() => CurrentPatient)
            .ObservesProperty(() => IsBusy);

        Logger.LogInformation("PatientSelectionViewModel已初始化");
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 返回主页（根据用户角色导航）
    /// </summary>
    private void ExecuteBackToHome()
    {
        try
        {
            var homeViewName = SessionManager?.CurrentUser?.Role switch
            {
                UserRole.Admin => "AdminHomeView",
                UserRole.Doctor => "ClinicalHomeView",
                _ => "ClinicalHomeView"
            };

            Logger.LogInformation("返回主页，导航到：{HomeView}", homeViewName);
            _regionManager.RequestNavigate("ContentRegion", homeViewName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "返回主页时发生异常");
        }
    }

    /// <summary>
    /// 开始看诊（智能判断是否有未完成医案）
    /// </summary>
    private async Task ExecuteStartConsultationAsync()
    {
        if (CurrentPatient == null)
        {
            await ShowErrorMessageAsync("请先选择患者");
            return;
        }

        try
        {
            SetIsBusy(true, "正在检查...");

            Logger.LogInformation("开始看诊，患者：{PatientName}（ID: {PatientId}）",
                CurrentPatient.Name, CurrentPatient.Id);

            // 1. 检查是否有未完成的医案
            var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(CurrentPatient.Id);

            if (unfinishedCase != null)
            {
                // 显示三选一对话框：继续看诊/新建医案/关闭医案
                var result = await ShowUnfinishedCaseDialogAsync(unfinishedCase);

                switch (result)
                {
                    case UnfinishedCaseAction.Continue:
                        // 继续看诊：使用现有MedicalCaseId
                        await ContinueConsultationAsync(unfinishedCase.Id, CurrentPatient);
                        break;

                    case UnfinishedCaseAction.CreateNew:
                        // 新建医案：先关闭旧医案，再创建新医案
                        await CreateNewCaseAfterClosingOldAsync(unfinishedCase, CurrentPatient);
                        break;

                    case UnfinishedCaseAction.CloseOnly:
                        // 仅关闭医案：关闭旧医案，停留在当前界面
                        await CloseOldMedicalCaseAsync(unfinishedCase);
                        await RefreshPendingQueueAsync(); // 刷新待看诊队列
                        await ShowSuccessMessageAsync("医案已关闭");
                        break;

                    case UnfinishedCaseAction.Cancel:
                        // 取消操作
                        Logger.LogInformation("用户取消操作");
                        break;
                }
            }
            else
            {
                // 2. 无未完成医案，直接创建新医案
                await CreateNewCaseAsync(CurrentPatient);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "开始看诊时发生异常");
            await ShowErrorMessageAsync($"开始看诊失败：{ex.Message}");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    private bool CanExecuteStartConsultation()
    {
        return CurrentPatient != null && !IsBusy;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从待看诊队列设置当前患者
    /// </summary>
    private async void SetCurrentPatientFromPending(PendingMedicalCaseDto pendingPatient)
    {
        try
        {
            var patient = await _patientRepository.GetByIdAsync(pendingPatient.PatientId);
            if (patient != null)
            {
                CurrentPatient = patient;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者信息失败，PatientId: {PatientId}", pendingPatient.PatientId);
        }
    }

    /// <summary>
    /// 加载待看诊队列 + 构建本地缓存
    /// </summary>
    private async Task LoadPendingQueueAsync()
    {
        try
        {
            SetIsBusy(true, "加载待看诊队列...");

            Logger.LogInformation("加载待看诊队列");

            var pending = await _medicalCaseRepository.GetPendingCasesAsync();
            PendingQueue = new ObservableCollection<PendingMedicalCaseDto>(pending);

            // 构建本地缓存（效率优化）
            _pendingCaseCache.Clear();
            foreach (var item in pending)
            {
                if (item.MedicalCaseId != Guid.Empty)
                {
                    _pendingCaseCache[item.PatientId] = item.MedicalCaseId;
                }
            }

            Logger.LogInformation("待看诊队列加载完成，共 {Count} 条，缓存 {CacheCount} 条", 
                PendingQueue.Count, _pendingCaseCache.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载待看诊队列失败");
            await ShowErrorMessageAsync($"加载待看诊队列失败：{ex.Message}");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 加载全部患者列表
    /// </summary>
    private async Task LoadPatientsAsync()
    {
        try
        {
            SetIsBusy(true, "加载患者列表...");

            Logger.LogInformation("加载患者列表，搜索关键字：{SearchText}", SearchText);

            var keyword = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            var patients = await _patientRepository.SearchAsync(keyword!);

            Patients = new ObservableCollection<PatientDto>(patients);

            Logger.LogInformation("患者列表加载完成，共 {Count} 条", Patients.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者列表失败");
            await ShowErrorMessageAsync($"加载患者列表失败：{ex.Message}");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 刷新待看诊队列（关闭医案后调用）
    /// </summary>
    private async Task RefreshPendingQueueAsync()
    {
        await LoadPendingQueueAsync();
    }

    /// <summary>
    /// 检查患者是否有未完成的医案（先查本地缓存，减少API调用）
    /// </summary>
    private async Task<MedicalCaseDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId)
    {
        try
        {
            Logger.LogInformation("检查患者未完成医案，PatientId: {PatientId}", patientId);

            // 效率优化：先查本地缓存
            if (_pendingCaseCache.TryGetValue(patientId, out var cachedCaseId))
            {
                Logger.LogInformation("缓存命中，直接返回未完成医案，MedicalCaseId: {MedicalCaseId}", cachedCaseId);
                
                // 查询详细信息（包含创建时间等）
                var cases = await _medicalCaseRepository.GetByPatientIdAsync(patientId);
                var cachedCase = cases.FirstOrDefault(c => c.Id == cachedCaseId);
                
                if (cachedCase != null)
                {
                    return cachedCase;
                }
                else
                {
                    // 缓存失效，删除缓存项
                    _pendingCaseCache.Remove(patientId);
                    Logger.LogWarning("缓存失效，已删除缓存项，PatientId: {PatientId}", patientId);
                }
            }

            // 缓存未命中或失效，调用API查询
            Logger.LogInformation("缓存未命中，调用API查询，PatientId: {PatientId}", patientId);
            var allCases = await _medicalCaseRepository.GetByPatientIdAsync(patientId);
            var unfinishedCase = allCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Active);

            if (unfinishedCase != null)
            {
                Logger.LogInformation("检测到未完成医案，ID: {MedicalCaseId}", unfinishedCase.Id);
                
                // 更新缓存
                _pendingCaseCache[patientId] = unfinishedCase.Id;
            }

            return unfinishedCase;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "检查未完成医案失败，PatientId: {PatientId}", patientId);
            return null;
        }
    }

    /// <summary>
    /// 显示未完成医案处理对话框（三选一）
    /// </summary>
    private async Task<UnfinishedCaseAction> ShowUnfinishedCaseDialogAsync(MedicalCaseDto unfinishedCase)
    {
        var message = $"该患者有未完成的医案（创建于 {unfinishedCase.CreatedAt:yyyy-MM-dd HH:mm}）\n\n请选择操作：";

        // 使用自定义对话框，提供三个按钮
        // 实现细节：可以使用Prism的DialogService或自定义Window
        // 这里假设有一个ShowThreeOptionsDialogAsync方法
        var result = await ShowThreeOptionsDialogAsync(
            title: "未完成的医案",
            message: message,
            option1: "继续看诊",
            option2: "新建医案",
            option3: "关闭医案"
        );

        return result switch
        {
            1 => UnfinishedCaseAction.Continue,
            2 => UnfinishedCaseAction.CreateNew,
            3 => UnfinishedCaseAction.CloseOnly,
            _ => UnfinishedCaseAction.Cancel
        };
    }

    /// <summary>
    /// 继续看诊（打开旧医案）
    /// </summary>
    private async Task ContinueConsultationAsync(Guid medicalCaseId, PatientDto patient)
    {
        Logger.LogInformation("继续看诊，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", medicalCaseId },
            { "CurrentPatient", patient }
        };

        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);

        Logger.LogInformation("已导航到看诊流程，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
    }

    /// <summary>
    /// 新建医案（先关闭旧医案）
    /// </summary>
    private async Task CreateNewCaseAfterClosingOldAsync(MedicalCaseDto oldCase, PatientDto patient)
    {
        Logger.LogInformation("用户选择新建医案，准备关闭旧医案并删除关联数据");

        // 1. 关闭旧医案（Server端自动级联删除Consultation/Prescription）
        await CloseOldMedicalCaseAsync(oldCase);

        // 2. 从缓存删除该患者（旧医案已关闭）
        if (_pendingCaseCache.Remove(oldCase.PatientId))
        {
            Logger.LogInformation("已从缓存删除患者，PatientId: {PatientId}", oldCase.PatientId);
        }

        // 3. 创建新医案
        await CreateNewCaseAsync(patient);
    }

    /// <summary>
    /// 创建新医案
    /// </summary>
    private async Task CreateNewCaseAsync(PatientDto patient)
    {
        SetIsBusy(true, "正在创建医案...");

        Logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patient.Id);

        if (SessionManager == null || SessionManager.CurrentUser == null)
        {
            Logger.LogError("SessionManager或CurrentUser为null，无法创建MedicalCase");
            await ShowErrorMessageAsync("用户信息丢失，无法创建医案");
            return;
        }

        var createDto = new MedicalCaseCreateDto
        {
            PatientId = patient.Id,
            DoctorId = SessionManager.CurrentUser.Id,
            Status = MedicalCaseStatus.Active,
            Remark = null
        };

        Logger.LogInformation("调用API创建MedicalCase，DoctorId: {DoctorId}", createDto.DoctorId);

        var createdDto = await _medicalCaseRepository.CreateAsync(createDto);

        Logger.LogInformation("MedicalCase创建成功，ID: {MedicalCaseId}", createdDto.Id);

        // 导航到看诊流程
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", createdDto.Id },
            { "CurrentPatient", patient }
        };

        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);

        Logger.LogInformation("已导航到看诊流程，MedicalCaseId: {MedicalCaseId}", createdDto.Id);
    }

    /// <summary>
    /// 关闭旧医案并删除关联数据
    /// Server端会自动级联删除关联的Consultation和Prescription
    /// </summary>
    private async Task CloseOldMedicalCaseAsync(MedicalCaseDto oldCase)
    {
        try
        {
            Logger.LogInformation("开始关闭旧医案，MedicalCaseId: {MedicalCaseId}", oldCase.Id);

            if (SessionManager == null || SessionManager.CurrentUser == null)
            {
                Logger.LogError("SessionManager或CurrentUser为null，无法更新MedicalCase状态");
                throw new InvalidOperationException("用户信息丢失，无法关闭医案");
            }

            // 更新医案状态为Closed（Server端会自动级联删除关联的Consultation和Prescription）
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = oldCase.Id,
                PatientId = oldCase.PatientId,
                DoctorId = oldCase.DoctorId,
                Status = MedicalCaseStatus.Closed.ToString()
            };

            Logger.LogInformation("更新MedicalCase状态为Closed（Server端将级联删除Consultation和Prescription）");
            await _medicalCaseRepository.UpdateAsync(updateDto);

            // 从缓存删除该患者（仅关闭医案，不导航的场景）
            if (_pendingCaseCache.Remove(oldCase.PatientId))
            {
                Logger.LogInformation("已从缓存删除患者，PatientId: {PatientId}", oldCase.PatientId);
            }

            Logger.LogInformation("旧医案关闭成功，MedicalCaseId: {MedicalCaseId}", oldCase.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "关闭旧医案失败，MedicalCaseId: {MedicalCaseId}", oldCase.Id);
            throw;
        }
    }

    #endregion

    #region INavigationAware

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        Logger.LogInformation("进入患者选择界面");

        // 自动加载待看诊队列和患者列表
        _ = Task.WhenAll(
            LoadPendingQueueAsync(),
            LoadPatientsAsync()
        );
    }

    #endregion
}

/// <summary>
/// 未完成医案处理动作
/// </summary>
public enum UnfinishedCaseAction
{
    Continue,   // 继续看诊
    CreateNew,  // 新建医案
    CloseOnly,  // 仅关闭医案
    Cancel      // 取消
}
```

---

## 🔧 Server端API设计

### 1. DTO设计

```csharp
/// <summary>
/// 待看诊医案DTO
/// </summary>
public class PendingMedicalCaseDto
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; set; }

    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号（脱敏，如：138****1234）
    /// </summary>
    public string PhoneMasked { get; set; } = string.Empty;

    /// <summary>
    /// 医案创建时间（开始看诊时间）
    /// </summary>
    public DateTime CaseCreatedAt { get; set; }

    /// <summary>
    /// 当前步骤（1=医案创建，2=诊断录入，3=处方编辑）
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// 类型标识（预留，用于未来挂号集成）
    /// </summary>
    public PendingType Type { get; set; } = PendingType.Incomplete;

    /// <summary>
    /// 挂号ID（预留，当前为null）
    /// </summary>
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// 挂号时间（预留，当前为null）
    /// </summary>
    public DateTime? AppointmentTime { get; set; }

    /// <summary>
    /// 挂号号码（预留，当前为null）
    /// </summary>
    public int? AppointmentNumber { get; set; }
}

/// <summary>
/// 待看诊类型（预留，用于未来挂号集成）
/// </summary>
public enum PendingType
{
    /// <summary>
    /// 未完成医案
    /// </summary>
    Incomplete = 1,

    /// <summary>
    /// 已挂号（预留）
    /// </summary>
    Appointment = 2
}
```

### 2. Repository方法

```csharp
// IMedicalCaseRepository.cs
public interface IMedicalCaseRepository : IBaseRepository<MedicalCaseEntity>
{
    /// <summary>
    /// 获取待看诊医案列表（Status=Active）
    /// </summary>
    Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync();

    /// <summary>
    /// 根据ID获取医案（包含Consultation和Prescription）
    /// </summary>
    Task<MedicalCaseEntity?> GetByIdWithDetailsAsync(Guid id);

    // ... 其他方法
}

// MedicalCaseRepository.cs
public class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
{
    /// <summary>
    /// 获取待看诊医案列表（Status=Active）
    /// 按创建时间升序排列（先开始看诊的在前）
    /// </summary>
    public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync()
    {
        var query = from mc in _dbSet
                    join p in _context.Set<PatientEntity>() on mc.PatientId equals p.Id
                    where mc.Status == MedicalCaseStatus.Active && !mc.IsDeleted
                    orderby mc.CreatedAt ascending // 升序：先开始看诊的在前
                    select new
                    {
                        MedicalCase = mc,
                        Patient = p
                    };

        var results = await query.ToListAsync();

        return results.Select(r => new PendingMedicalCaseDto
        {
            MedicalCaseId = r.MedicalCase.Id,
            PatientId = r.MedicalCase.PatientId,
            PatientName = r.Patient.Name,
            PhoneMasked = MaskPhoneNumber(r.Patient.Phone), // 手机号脱敏
            CaseCreatedAt = r.MedicalCase.CreatedAt,
            CurrentStep = DetermineCurrentStepByStatus(r.MedicalCase), // 简化版，不需要Include
            Type = PendingType.Incomplete,

            // 预留字段（当前为null）
            AppointmentId = null,
            AppointmentTime = null,
            AppointmentNumber = null
        }).ToList();
    }

    /// <summary>
    /// 手机号脱敏（138****1234）
    /// </summary>
    private string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 11)
        {
            return phone ?? string.Empty;
        }

        // 保留前3位和后4位，中间用****替换
        return $"{phone.Substring(0, 3)}****{phone.Substring(phone.Length - 4)}";
    }

    /// <summary>
    /// 根据MedicalCase状态简单判断步骤（简化版，不需Include）
    /// </summary>
    private int DetermineCurrentStepByStatus(MedicalCaseEntity medicalCase)
    {
        // 简化版：假定所有Active医案都在步骤1（医案创建）
        // 如需更精确的步骤判断，需要Include Consultation和Prescription
        return 1;
    }

    /// <summary>
    /// 根据ID获取医案（包含Consultation和Prescription）
    /// </summary>
    public async Task<MedicalCaseEntity?> GetByIdWithDetailsAsync(Guid id)
    {
        return await GetDetailQuery()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <summary>
    /// 判断当前步骤
    /// </summary>
    private int DetermineCurrentStep(MedicalCaseEntity medicalCase)
    {
        if (medicalCase.Prescription != null) return 3; // 已到处方步骤
        if (medicalCase.Consultation != null) return 2; // 已到诊断步骤
        return 1; // 医案刚创建
    }

    /// <summary>
    /// 详细查询（Include Consultation + Prescription）
    /// </summary>
    private IQueryable<MedicalCaseEntity> GetDetailQuery()
    {
        return _dbSet
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
            .Where(m => !m.IsDeleted);
    }

    /// <summary>
    /// 更新医案（Issue #1571 - 级联删除）
    /// 当医案状态变更为Closed时，自动删除关联的Consultation和Prescription
    /// </summary>
    public override async Task<MedicalCaseEntity> UpdateAsync(MedicalCaseEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // 获取数据库中的原实体以检测状态变更
        var existingEntity = await _dbSet
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
            .FirstOrDefaultAsync(m => m.Id == entity.Id);

        if (existingEntity == null)
            throw new InvalidOperationException($"医案 {entity.Id} 不存在");

        // 检测状态变更：从Active变为Closed
        if (existingEntity.Status != MedicalCaseStatus.Closed &&
            entity.Status == MedicalCaseStatus.Closed)
        {
            _logger?.LogInformation("检测到医案状态变更为Closed，准备级联删除关联数据，MedicalCaseId: {MedicalCaseId}", entity.Id);

            // 删除关联的Consultation（诊断）
            if (existingEntity.Consultation != null)
            {
                _logger?.LogInformation("删除关联的Consultation，ConsultationId: {ConsultationId}",
                    existingEntity.Consultation.Id);
                _context.Set<ConsultationEntity>().Remove(existingEntity.Consultation);
            }

            // 删除关联的Prescription（处方）
            if (existingEntity.Prescription != null)
            {
                _logger?.LogInformation("删除关联的Prescription，PrescriptionId: {PrescriptionId}",
                    existingEntity.Prescription.Id);
                _context.Set<PrescriptionEntity>().Remove(existingEntity.Prescription);
            }

            _logger?.LogInformation("级联删除完成，即将更新医案状态");
        }

        // 调用基类UpdateAsync完成更新
        return await base.UpdateAsync(entity);
    }
}
```

### 3. API端点

```csharp
// MedicalCaseController.cs

/// <summary>
/// 获取待看诊队列
/// </summary>
[HttpGet("pending")]
[ProducesResponseType(typeof(IEnumerable<PendingMedicalCaseDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetPendingCasesAsync()
{
    try
    {
        _logger.LogInformation("获取待看诊队列");

        var pendingCases = await _medicalCaseRepository.GetPendingCasesAsync();

        _logger.LogInformation("成功获取{Count}个待看诊患者", pendingCases.Count);
        return Ok(pendingCases);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取待看诊队列失败");
        return StatusCode(500, "获取待看诊队列失败");
    }
}

/// <summary>
/// 根据ID获取医案详情（包含Consultation和Prescription）
/// </summary>
[HttpGet("{id}/details")]
[ProducesResponseType(typeof(MedicalCaseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetByIdWithDetailsAsync(Guid id)
{
    try
    {
        _logger.LogInformation("获取医案详情，ID: {MedicalCaseId}", id);

        var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(id);
        if (medicalCase == null)
        {
            _logger.LogWarning("医案不存在，ID: {MedicalCaseId}", id);
            return NotFound($"医案 {id} 不存在");
        }

        // 转换为DTO（包含Consultation和Prescription）
        var dto = MapToDto(medicalCase);

        _logger.LogInformation("成功获取医案详情，ID: {MedicalCaseId}", id);
        return Ok(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取医案详情失败，ID: {MedicalCaseId}", id);
        return StatusCode(500, "获取医案详情失败");
    }
}
```

---

## 📊 Phase实施计划

### Phase 1: UI改造（三区域布局）- 3-4小时

**任务**：
1. 修改PatientSelectionView.xaml，实现三区域布局：
   - 左侧300px分为两部分：患者信息详情(180px) + 待看诊队列(740px)
   - 右侧1460px：搜索区 + 全部患者列表 + 分页控件
   - 调整分页为每页20条（从50条调整）
2. 修改PatientSelectionViewModel.cs：
   - 添加PendingQueue属性（待看诊队列）
   - 重命名SelectedPatient为CurrentPatient（当前选中患者）
   - 添加SelectedPendingPatient和SelectedAllPatient（两个列表独立选中状态）
   - 实现StartConsultationCommand（开始诊断按钮）
3. 移除全部患者列表的行内"选择"按钮
4. 添加统一的"开始诊断"按钮到搜索区
5. 实现双击事件绑定（两个列表都支持）
6. 添加患者信息详情模块的数据绑定

**验收**：
- [ ] 编译通过
- [ ] 界面显示三区域布局（左侧患者信息+待看诊队列，右侧全部患者）
- [ ] 患者信息详情模块显示CurrentPatient信息（姓名/手机/年龄/地址/过敏史）
- [ ] 未选择患者时显示提示文字"请从列表中选择患者"
- [ ] 单击任一列表患者，CurrentPatient更新，患者信息详情模块刷新
- [ ] 双击任一列表患者，触发开始诊断流程
- [ ] "开始诊断"按钮仅在CurrentPatient不为null时可用

### Phase 2: 智能路由（含本地缓存）- 2.5小时

**任务**：
1. 添加`_pendingCaseCache`字段（Dictionary<Guid, Guid>）
2. 修改LoadPendingQueueAsync，构建本地缓存
3. 实现CheckUnfinishedMedicalCaseAsync方法（先查缓存，缓存未命中再调API）
4. 实现ShowUnfinishedCaseDialogAsync方法（三选一对话框）
5. 实现ContinueConsultationAsync方法（继续看诊）
6. 实现CreateNewCaseAfterClosingOldAsync方法（新建医案+缓存清理）
7. 实现CloseOldMedicalCaseAsync方法（关闭医案+缓存清理）
8. 实现RefreshPendingQueueAsync方法（重建缓存）
9. 添加缓存命中/未命中的日志记录

**验收**：
- [ ] 编译通过
- [ ] 加载待看诊队列时，成功构建本地缓存
- [ ] 选择待看诊队列中的患者，缓存命中，无额外API调用
- [ ] 选择其他患者，缓存未命中，调用API查询
- [ ] 选择有未完成医案的患者，弹窗显示三个选项
- [ ] 选择"继续看诊"，进入看诊流程
- [ ] 选择"新建医案"，旧医案关闭，从缓存删除，创建新医案，进入看诊流程
- [ ] 选择"关闭医案"，旧医案关闭，从缓存删除，停留在患者选择界面
- [ ] 日志正确记录缓存命中/未命中情况

### Phase 3: 数据同步 - 2小时

**任务**：
1. 修改MedicalCaseFlowViewModel.OnNavigatedTo，加载Consultation和Prescription
2. 验证Server端UpdateAsync级联删除逻辑
3. 测试数据库数据正确性

**验收**：
- [ ] 编译通过
- [ ] 继续看诊时，诊断和处方数据正确加载（修复Issue #1570）
- [ ] 关闭医案时，Consultation和Prescription被级联删除（修复Issue #1571）
- [ ] 数据库无冗余数据

### Phase 4: 导航修复 - 1小时

**任务**：
1. 修改MedicalCaseFlowViewModel暂存逻辑，移除导航代码
2. 验证返回主页按钮逻辑

**验收**：
- [ ] 编译通过
- [ ] 暂存后停留在当前界面（修复Issue #1569）
- [ ] 返回主页按钮根据用户角色正确导航（验证Issue #1573已修复）

### Phase 5: Server端API（含手机号脱敏）- 2.5小时

**任务**：
1. 在Shared项目添加PendingMedicalCaseDto和PendingType：
   - 添加PhoneMasked字段（手机号脱敏）
2. 实现GetPendingCasesAsync Repository方法：
   - Join PatientEntity获取手机号
   - 实现MaskPhoneNumber()辅助方法（138****1234格式）
   - 简化CurrentStep判断（不需要Include Consultation/Prescription）
3. 实现GetPendingCasesAsync API端点
4. 实现GetByIdWithDetailsAsync API端点
5. Client端调用API

**验收**：
- [ ] 编译通过
- [ ] API返回待看诊队列数据
- [ ] 手机号正确脱敏（格式：138****1234）
- [ ] 待看诊队列按创建时间升序排列
- [ ] Client端正确显示待看诊队列（包含脱敏手机号）
- [ ] 性能验证：待看诊列表加载 < 200ms

---

## ✅ 质量标准

### 编译标准
- 0 errors
- 0 warnings

### 运行时验证
- 启动应用，完整测试5个验收场景
- 数据库状态验证
- 日志输出检查

### 代码规范
- MVVM架构：ViewModel不操作UI
- 三层对齐：Client(五层) + Server(三层) + Shared(DTOs)
- 聚合根模式：MedicalCase管理Consultation/Prescription生命周期
- 命名规范：PascalCase（类型）、_camelCase（私有字段）、Async结尾（异步方法）

### 性能要求
- 待看诊列表加载 < 200ms
- UI响应 < 100ms

---

**下一步**：用户审查设计文档 → 通过后创建Epic Issue → 按设计文档实施
