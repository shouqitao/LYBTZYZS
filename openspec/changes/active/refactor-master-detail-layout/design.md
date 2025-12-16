# Design: 重构基础数据模块为Master-Detail布局

## 1. 架构设计

### 1.1 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    MasterDetailLayout                        │
├─────────────────────────┬───────────────────────────────────┤
│      Master (40%)       │         Detail (60%)              │
│  ┌───────────────────┐  │  ┌─────────────────────────────┐  │
│  │    搜索框         │  │  │     工具栏 (查看/编辑/保存) │  │
│  ├───────────────────┤  │  ├─────────────────────────────┤  │
│  │                   │  │  │                             │  │
│  │    DataGrid       │  │  │    ViewControl              │  │
│  │    列表           │  │  │    或                       │  │
│  │                   │  │  │    EditControl              │  │
│  │    [选中高亮]     │  │  │                             │  │
│  │                   │  │  │                             │  │
│  ├───────────────────┤  │  │                             │  │
│  │    分页控件       │  │  │                             │  │
│  └───────────────────┘  │  └─────────────────────────────┘  │
└─────────────────────────┴───────────────────────────────────┘
```

### 1.2 组件层次

```
LYBT.Desktop.Infrastructure
├── Controls/
│   ├── MasterDetailLayout.xaml         # 新增: 通用容器
│   └── MasterDetailLayout.xaml.cs
├── ViewModels/
│   ├── IMasterDetailViewModel.cs       # 新增: 接口
│   └── MasterDetailViewModelBase.cs    # 新增: 基类
└── Themes/
    └── MasterDetailStyles.xaml         # 新增: 专用样式

LYBT.Desktop.Patients
├── Views/
│   ├── PatientMasterDetailView.xaml    # 新增: 替代List+Detail
│   └── PatientMasterDetailView.xaml.cs
├── ViewModels/
│   └── PatientMasterDetailViewModel.cs # 新增: 合并ViewModel
└── Controls/
    ├── PatientViewControl.xaml         # 复用
    └── PatientEditControl.xaml         # 复用
```

---

## 2. 核心组件设计

### 2.1 MasterDetailLayout控件

```xml
<!-- MasterDetailLayout.xaml -->
<UserControl x:Class="LYBT.Desktop.Infrastructure.Controls.MasterDetailLayout">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" MinWidth="300"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="3*" MinWidth="400"/>
        </Grid.ColumnDefinitions>

        <!-- Master区域 -->
        <ContentPresenter Grid.Column="0"
                          Content="{Binding MasterContent, RelativeSource={RelativeSource TemplatedParent}}"/>

        <!-- 分割线 -->
        <GridSplitter Grid.Column="1" Width="5"
                      HorizontalAlignment="Center" VerticalAlignment="Stretch"/>

        <!-- Detail区域 -->
        <Grid Grid.Column="2">
            <!-- 有选中项时显示详情 -->
            <ContentPresenter Content="{Binding DetailContent, RelativeSource={RelativeSource TemplatedParent}}"
                              Visibility="{Binding HasSelection, Converter={StaticResource BoolToVisibility}}"/>

            <!-- 无选中项时显示空状态 -->
            <ContentPresenter Content="{Binding EmptyContent, RelativeSource={RelativeSource TemplatedParent}}"
                              Visibility="{Binding HasSelection, Converter={StaticResource InverseBoolToVisibility}}"/>
        </Grid>
    </Grid>
</UserControl>
```

**依赖属性:**
```csharp
public static readonly DependencyProperty MasterContentProperty;
public static readonly DependencyProperty DetailContentProperty;
public static readonly DependencyProperty EmptyContentProperty;
public static readonly DependencyProperty HasSelectionProperty;
public static readonly DependencyProperty MasterWidthProperty;  // 默认 2*
public static readonly DependencyProperty DetailWidthProperty;  // 默认 3*
```

### 2.2 IMasterDetailViewModel接口

```csharp
public interface IMasterDetailViewModel<TListItem, TDetail>
{
    // 列表相关
    ObservableCollection<TListItem> Items { get; }
    TListItem SelectedItem { get; set; }
    string SearchText { get; set; }
    int CurrentPage { get; set; }
    int TotalPages { get; }

    // 详情相关
    TDetail CurrentDetail { get; }
    bool IsEditMode { get; set; }
    bool HasSelection { get; }

    // 命令
    ICommand LoadCommand { get; }
    ICommand RefreshCommand { get; }
    ICommand SearchCommand { get; }
    ICommand SelectItemCommand { get; }
    ICommand CreateCommand { get; }
    ICommand EditCommand { get; }
    ICommand SaveCommand { get; }
    ICommand CancelCommand { get; }
    ICommand DeleteCommand { get; }

    // 分页
    ICommand NextPageCommand { get; }
    ICommand PreviousPageCommand { get; }
}
```

### 2.3 MasterDetailViewModelBase基类

```csharp
public abstract class MasterDetailViewModelBase<TListItem, TDetail>
    : ViewModelBase, IMasterDetailViewModel<TListItem, TDetail>
{
    // 抽象方法 - 子类实现
    protected abstract Task<PagedResult<TListItem>> LoadItemsAsync(int page, string search);
    protected abstract Task<TDetail> LoadDetailAsync(TListItem item);
    protected abstract Task<bool> SaveAsync(TDetail detail);
    protected abstract Task<bool> DeleteAsync(TListItem item);
    protected abstract TDetail CreateNewDetail();

    // 通用实现
    private async Task OnSelectedItemChanged(TListItem item)
    {
        if (item == null)
        {
            CurrentDetail = default;
            return;
        }

        IsLoading = true;
        CurrentDetail = await LoadDetailAsync(item);
        IsEditMode = false;
        IsLoading = false;
    }
}
```

---

## 3. 模块适配设计

### 3.1 患者模块示例

```xml
<!-- PatientMasterDetailView.xaml -->
<controls:MasterDetailLayout>
    <!-- Master: 患者列表 -->
    <controls:MasterDetailLayout.MasterContent>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- 工具栏 -->
                <RowDefinition Height="*"/>     <!-- 列表 -->
                <RowDefinition Height="Auto"/>  <!-- 分页 -->
            </Grid.RowDefinitions>

            <!-- 工具栏 -->
            <StackPanel Grid.Row="0" Orientation="Horizontal">
                <TextBox Text="{Binding SearchText}" Width="200"/>
                <Button Content="新增" Command="{Binding CreateCommand}"/>
            </StackPanel>

            <!-- 列表 -->
            <DataGrid Grid.Row="1"
                      ItemsSource="{Binding Items}"
                      SelectedItem="{Binding SelectedItem}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="姓名" Binding="{Binding Name}"/>
                    <DataGridTextColumn Header="手机" Binding="{Binding Phone}"/>
                    <DataGridTextColumn Header="年龄" Binding="{Binding Age}"/>
                </DataGrid.Columns>
            </DataGrid>

            <!-- 分页 -->
            <controls:Pagination Grid.Row="2"
                                 CurrentPage="{Binding CurrentPage}"
                                 TotalPages="{Binding TotalPages}"/>
        </Grid>
    </controls:MasterDetailLayout.MasterContent>

    <!-- Detail: 患者详情/编辑 -->
    <controls:MasterDetailLayout.DetailContent>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- 工具栏 -->
                <RowDefinition Height="*"/>     <!-- 内容 -->
            </Grid.RowDefinitions>

            <!-- 工具栏 -->
            <StackPanel Grid.Row="0" Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="编辑" Command="{Binding EditCommand}"
                        Visibility="{Binding IsEditMode, Converter={StaticResource InverseBoolToVisibility}}"/>
                <Button Content="保存" Command="{Binding SaveCommand}"
                        Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibility}}"/>
                <Button Content="取消" Command="{Binding CancelCommand}"
                        Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibility}}"/>
                <Button Content="删除" Command="{Binding DeleteCommand}"/>
            </StackPanel>

            <!-- 查看模式 -->
            <patientControls:PatientViewControl
                Grid.Row="1"
                Visibility="{Binding IsEditMode, Converter={StaticResource InverseBoolToVisibility}}"
                PatientName="{Binding CurrentDetail.Name}"
                .../>

            <!-- 编辑模式 -->
            <patientControls:PatientEditControl
                Grid.Row="1"
                Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibility}}"
                Patient="{Binding CurrentDetail}"/>
        </Grid>
    </controls:MasterDetailLayout.DetailContent>

    <!-- 空状态 -->
    <controls:MasterDetailLayout.EmptyContent>
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            <TextBlock Text="请从左侧列表选择患者" FontSize="16" Foreground="#888"/>
        </StackPanel>
    </controls:MasterDetailLayout.EmptyContent>
</controls:MasterDetailLayout>
```

---

## 4. 样式设计

### 4.1 MasterDetailStyles.xaml

```xml
<!-- 列表选中样式 -->
<Style x:Key="MasterListRowStyle" TargetType="DataGridRow">
    <Setter Property="Background" Value="Transparent"/>
    <Style.Triggers>
        <Trigger Property="IsSelected" Value="True">
            <Setter Property="Background" Value="#E6F3FF"/>
            <Setter Property="BorderBrush" Value="#0078D4"/>
            <Setter Property="BorderThickness" Value="0,0,3,0"/>
        </Trigger>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#F5F5F5"/>
        </Trigger>
    </Style.Triggers>
</Style>

<!-- 详情面板样式 -->
<Style x:Key="DetailPanelStyle" TargetType="Border">
    <Setter Property="Background" Value="#FAFAFA"/>
    <Setter Property="BorderBrush" Value="#E0E0E0"/>
    <Setter Property="BorderThickness" Value="1,0,0,0"/>
    <Setter Property="Padding" Value="16"/>
</Style>

<!-- 空状态样式 -->
<Style x:Key="EmptyStateStyle" TargetType="StackPanel">
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Setter Property="HorizontalAlignment" Value="Center"/>
    <Setter Property="Opacity" Value="0.6"/>
</Style>
```

---

## 5. 导航设计

### 5.1 路由变更

| 原路由 | 新路由 | 说明 |
|--------|--------|------|
| PatientManagementView | PatientMasterDetailView | 合并 |
| PatientDetailView | (删除) | 内嵌到MasterDetail |
| UserManagementView | UserMasterDetailView | 合并 |
| UserDetailView | (删除) | 内嵌到MasterDetail |
| HerbManagementView | HerbMasterDetailView | 合并 |
| HerbDetailView | (删除) | 内嵌到MasterDetail |
| FormulaManagementView | FormulaMasterDetailView | 合并 |
| FormulaDetailView | (删除) | 内嵌到MasterDetail |

### 5.2 模块注册更新

```csharp
// PatientsModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 移除
    // containerRegistry.RegisterForNavigation<PatientManagementView>();
    // containerRegistry.RegisterForNavigation<PatientDetailView>();

    // 新增
    containerRegistry.RegisterForNavigation<PatientMasterDetailView>();
}
```

---

## 6. 响应式设计

### 6.1 窗口尺寸适配

| 窗口宽度 | 布局模式 | 说明 |
|----------|----------|------|
| >= 1200px | 并排显示 | 40:60分割 |
| 800-1200px | 并排显示 | 列表收窄，优先详情 |
| < 800px | 堆叠显示 | 列表全屏，点击进入详情 |

### 6.2 实现方式

```xml
<controls:MasterDetailLayout>
    <controls:MasterDetailLayout.Style>
        <Style TargetType="controls:MasterDetailLayout">
            <Style.Triggers>
                <DataTrigger Binding="{Binding ActualWidth, RelativeSource={RelativeSource Self},
                             Converter={StaticResource WidthToLayoutModeConverter}}"
                             Value="Stacked">
                    <!-- 切换到堆叠模式 -->
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </controls:MasterDetailLayout.Style>
</controls:MasterDetailLayout>
```

---

## 7. 技术决策记录

### Decision 1: 使用自定义MasterDetailLayout而非第三方控件

**选项:**
- A) Windows Community Toolkit ListDetailsView (UWP/WinUI)
- B) 自定义MasterDetailLayout控件

**决策:** B - 自定义控件

**理由:**
- ListDetailsView是UWP/WinUI控件，需要额外适配WPF
- 自定义控件可完全控制样式和行为
- 更好地与现有Prism架构集成

### Decision 2: ViewModel合并策略

**选项:**
- A) 保留独立的List和Detail ViewModel，通过消息通信
- B) 合并为单一MasterDetailViewModel

**决策:** B - 合并ViewModel

**理由:**
- 减少组件间通信复杂度
- 状态管理更简单
- 更符合MVVM单一职责（一个视图一个ViewModel）

### Decision 3: 详情面板切换方式

**选项:**
- A) 使用ContentControl + DataTemplate切换
- B) 使用Visibility控制两个控件显隐

**决策:** B - Visibility控制

**理由:**
- 复用已有的ViewControl和EditControl
- 切换性能更好（控件已实例化）
- 编辑状态保持更简单
