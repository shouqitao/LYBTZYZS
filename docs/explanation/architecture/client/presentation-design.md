# Client端呈现层架构设计文档

## 文档版本

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|----------|
| 1.0.0 | 2025-10-30 | Claude | 初始版本 - Client端呈现层架构设计 |

## 1. 模块概述

### 1.1 定位与职责

呈现层（Presentation Layer）是WPF桌面应用的最外层，负责用户界面展示和用户交互管理。

**核心职责**：
1. **视图渲染**：使用XAML定义UI界面，展示数据和交互元素
2. **视图逻辑**：通过ViewModel处理业务逻辑和用户交互
3. **数据绑定**：实现View和ViewModel之间的双向数据流
4. **样式主题**：提供统一的视觉风格和用户体验
5. **导航管理**：通过Prism Region实现模块化导航
6. **对话框管理**：通过Prism Dialog实现弹窗交互

**架构定位**：
```
┌─────────────────────────────────────────────────────────┐
│                      呈现层（Presentation）              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐│
│  │  Views   │  │ViewModels│  │ Styles   │  │Converters││
│  │  (XAML)  │←→│  (C#)    │  │ (XAML)   │  │  (C#)   ││
│  └──────────┘  └──────────┘  └──────────┘  └─────────┘│
└─────────────────────────────────────────────────────────┘
                          ↓ 依赖
┌─────────────────────────────────────────────────────────┐
│                    契约层（Contracts）                   │
│             IApi接口 + IService接口                      │
└─────────────────────────────────────────────────────────┘
```

### 1.2 技术栈

- **.NET Framework**: .NET 8.0
- **UI框架**: WPF (Windows Presentation Foundation)
- **MVVM框架**: Prism 9.0.x
- **依赖注入**: Prism.Ioc + Microsoft.Extensions.DependencyInjection
- **日志**: Microsoft.Extensions.Logging
- **数据绑定**: WPF DataBinding Engine
- **样式系统**: ResourceDictionary + MergedDictionaries

## 2. MVVM架构模式

### 2.1 MVVM模式概述

MVVM（Model-View-ViewModel）是WPF应用的标准架构模式，实现视图与业务逻辑的分离。

```
┌────────────────────────────────────────────────────────────┐
│                      MVVM模式关系图                        │
├────────────────────────────────────────────────────────────┤
│                                                            │
│   ┌──────────┐                                             │
│   │   View   │  (XAML)                                     │
│   │          │  - UserControl/Window                       │
│   │          │  - XAML标记                                 │
│   └─────┬────┘  - 数据绑定表达式                           │
│         │                                                   │
│         │ DataBinding                                       │
│         │ {Binding PropertyName}                            │
│         │ {Binding CommandName}                             │
│         ↓                                                   │
│   ┌──────────┐                                             │
│   │ViewModel │  (C#)                                       │
│   │          │  - 继承UnifiedViewModelBase                 │
│   │          │  - 属性（INotifyPropertyChanged）           │
│   │          │  - 命令（DelegateCommand）                  │
│   │          │  - 业务逻辑                                  │
│   └─────┬────┘                                             │
│         │                                                   │
│         │ 依赖注入                                          │
│         │ IApi, IService, IRepository                      │
│         ↓                                                   │
│   ┌──────────┐                                             │
│   │  Model   │  (DTO)                                      │
│   │          │  - LYBT.Shared.Models.Contracts             │
│   │          │  - 数据传输对象                              │
│   └──────────┘  - 无业务逻辑                                │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 2.2 View（视图）

**职责**：
- 定义UI布局和控件
- 数据绑定表达式
- 触发器和动画
- 样式引用

**示例**（MedicalCaseListView.xaml）：
```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseListView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="../../../Resources/ManagementModuleStyles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 工具栏 -->
            <RowDefinition Height="*" />     <!-- 数据列表 -->
            <RowDefinition Height="Auto" />  <!-- 分页控件 -->
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <Border Grid.Row="0" Style="{StaticResource ToolBarBorder}">
            <StackPanel Orientation="Horizontal">
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                         Style="{StaticResource ModernTextBoxStyle}"
                         Width="250" />

                <Button Content="搜索"
                        Command="{Binding SearchCommand}"
                        Style="{StaticResource PrimaryButtonStyle}" />

                <Button Content="+ 新建"
                        Command="{Binding AddCommand}"
                        Style="{StaticResource SuccessButtonStyle}" />
            </StackPanel>
        </Border>

        <!-- 数据列表 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding MedicalCases}"
                  SelectedItem="{Binding SelectedMedicalCase}"
                  Style="{StaticResource ModernDataGridStyle}"
                  IsReadOnly="True">

            <DataGrid.Columns>
                <DataGridTextColumn Header="案例编号" Binding="{Binding CaseNumber}" Width="120" />
                <DataGridTextColumn Header="患者姓名" Binding="{Binding PatientName}" Width="100" />

                <!-- 操作列 -->
                <DataGridTemplateColumn Header="操作" Width="200">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="查看"
                                        Command="{Binding DataContext.ViewDetailCommand,
                                                 RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource PrimaryButtonStyle}" />

                                <Button Content="删除"
                                        Command="{Binding DataContext.DeleteCommand,
                                                 RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource DangerButtonStyle}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 加载遮罩 -->
        <Grid Grid.Row="0" Grid.RowSpan="3"
              Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"
              Background="#80000000">
            <Border Background="White" CornerRadius="5" Padding="20">
                <StackPanel Orientation="Horizontal">
                    <ProgressBar IsIndeterminate="True" Width="20" Height="20" />
                    <TextBlock Text="加载中..." Margin="10,0,0,0" />
                </StackPanel>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

**关键特性**：
1. **ViewModelLocator自动绑定**：`prism:ViewModelLocator.AutoWireViewModel="True"` 自动关联ViewModel
2. **样式资源合并**：通过MergedDictionaries引入模块样式
3. **数据绑定**：`{Binding PropertyName}` 绑定ViewModel属性
4. **命令绑定**：`{Binding CommandName}` 绑定ViewModel命令
5. **转换器使用**：`Converter={StaticResource BooleanToVisibilityConverter}` 数据转换
6. **相对源绑定**：`RelativeSource={RelativeSource AncestorType=DataGrid}` 访问父级DataContext

### 2.3 ViewModel（视图模型）

**职责**：
- 提供View所需的数据属性
- 提供View所需的命令
- 实现业务逻辑
- 与契约层交互（IApi, IService）
- 实现INotifyPropertyChanged

**示例**（MedicalCaseListViewModel.cs）：
```csharp
/// <summary>
/// 病历列表视图模型 - UltraThink精简架构
/// 基于UnifiedViewModelBase实现病历列表管理功能
/// </summary>
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    #region 服务依赖

    private readonly IMedicalCaseRepository _medicalCaseRepository;

    #endregion

    #region 数据属性

    private ObservableCollection<MedicalCaseDto> _medicalCases = new();
    private MedicalCaseDto? _selectedMedicalCase;
    private string _searchText = string.Empty;
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalCount;

    /// <summary>
    /// 病历列表
    /// </summary>
    public ObservableCollection<MedicalCaseDto> MedicalCases
    {
        get => _medicalCases;
        set => SetProperty(ref _medicalCases, value);
    }

    /// <summary>
    /// 选中的病历
    /// </summary>
    public MedicalCaseDto? SelectedMedicalCase
    {
        get => _selectedMedicalCase;
        set
        {
            if (SetProperty(ref _selectedMedicalCase, value))
            {
                UpdateCommandStates(); // 属性变化时更新命令状态
            }
        }
    }

    /// <summary>
    /// 搜索关键字
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 加载数据命令
    /// </summary>
    public DelegateCommand LoadDataCommand { get; }

    /// <summary>
    /// 搜索命令
    /// </summary>
    public DelegateCommand SearchCommand { get; }

    /// <summary>
    /// 创建病历命令
    /// </summary>
    public DelegateCommand CreateCommand { get; }

    /// <summary>
    /// 编辑病历命令
    /// </summary>
    public DelegateCommand EditCommand { get; }

    /// <summary>
    /// 删除病历命令
    /// </summary>
    public DelegateCommand DeleteCommand { get; }

    /// <summary>
    /// 查看详情命令
    /// </summary>
    public DelegateCommand ViewDetailCommand { get; }

    #endregion

    #region 构造函数

    public MedicalCaseListViewModel(
        IMedicalCaseRepository medicalCaseService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _medicalCaseRepository = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

        // 初始化命令
        LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
        SearchCommand = new DelegateCommand(async () => await SearchAsync());
        CreateCommand = new DelegateCommand(Create);
        EditCommand = new DelegateCommand(Edit, CanEdit);
        DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanDelete);
        ViewDetailCommand = new DelegateCommand(ViewDetail, CanViewDetail);

        // 属性变更时刷新命令状态
        PropertyChanged += (s, e) => UpdateCommandStates();
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 页面加载时调用
    /// </summary>
    protected override async Task InitializeAsync(NavigationParameters parameters)
    {
        await base.InitializeAsync(parameters);
        await LoadDataAsync();
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 加载数据
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            SetIsBusy(true, "正在加载病历列表...");

            var pagedData = await _medicalCaseRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
            MedicalCases.Clear();
            foreach (var item in pagedData.Items)
            {
                MedicalCases.Add(item);
            }
            TotalCount = pagedData.TotalCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载病历列表时发生异常");
            await ShowErrorMessageAsync("加载病历列表时发生系统错误，请稍后重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 搜索
    /// </summary>
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>
    /// 创建病历
    /// </summary>
    private void Create()
    {
        NavigateTo("MainRegion", "CreateMedicalCaseView");
    }

    /// <summary>
    /// 编辑病历
    /// </summary>
    private void Edit()
    {
        if (SelectedMedicalCase != null)
        {
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", SelectedMedicalCase.Id }
            };
            NavigateTo("MainRegion", "MedicalCaseDetailView", parameters);
        }
    }

    /// <summary>
    /// 删除病历
    /// </summary>
    private async Task DeleteAsync()
    {
        if (SelectedMedicalCase == null) return;

        var confirmed = await ShowConfirmMessageAsync($"确定要删除病历 '{SelectedMedicalCase.CaseNumber}' 吗？");
        if (!confirmed) return;

        try
        {
            SetIsBusy(true, "正在删除病历...");

            var success = await _medicalCaseRepository.DeleteAsync(SelectedMedicalCase.Id);
            if (success)
            {
                await ShowSuccessMessageAsync("病历删除成功");
                await LoadDataAsync();
            }
            else
            {
                await ShowErrorMessageAsync("删除病历失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除病历时发生异常");
            await ShowErrorMessageAsync("删除病历时发生系统错误，请稍后重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    /// <summary>
    /// 查看详情
    /// </summary>
    private void ViewDetail()
    {
        if (SelectedMedicalCase != null)
        {
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", SelectedMedicalCase.Id },
                { "IsReadOnly", true }
            };
            NavigateTo("MainRegion", "MedicalCaseDetailView", parameters);
        }
    }

    #endregion

    #region 命令状态检查

    private bool CanEdit() => SelectedMedicalCase != null && !IsBusy;
    private bool CanDelete() => SelectedMedicalCase != null && !IsBusy;
    private bool CanViewDetail() => SelectedMedicalCase != null;

    private void UpdateCommandStates()
    {
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        ViewDetailCommand.RaiseCanExecuteChanged();
    }

    #endregion
}
```

**关键特性**：
1. **继承UnifiedViewModelBase**：获取导航、日志、错误处理等基础功能
2. **构造函数注入**：通过DI注入所需服务（IRepository, IEventAggregator, IRegionManager等）
3. **属性实现INotifyPropertyChanged**：通过`SetProperty(ref _field, value)` 触发属性变更通知
4. **命令实现**：使用`DelegateCommand` 绑定View的按钮点击事件
5. **命令状态管理**：通过`CanExecute` 控制命令是否可用，通过`RaiseCanExecuteChanged()` 刷新状态
6. **异步操作**：使用async/await处理异步数据加载
7. **错误处理**：try-catch捕获异常，使用Logger记录日志，使用ShowErrorMessageAsync显示错误信息
8. **IsBusy状态管理**：通过SetIsBusy控制加载遮罩显示

### 2.4 Model（模型）

**职责**：
- 数据传输对象（DTO）
- 来自`LYBT.Shared.Models.Contracts`
- 无业务逻辑

**示例**：
```csharp
// 来自 LYBT.Shared.Models.Contracts.MedicalCase
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientGender { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public MedicalCaseStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**核心特点**：
- ✅ 纯数据对象，无方法
- ✅ 属性验证（DataAnnotations）
- ✅ 与API响应格式100%对齐
- ❌ 不包含业务逻辑
- ❌ 不直接操作数据库

## 3. Prism框架核心组件

### 3.1 Region Navigation（区域导航）

**Region概念**：
- Region是视图容器，可动态加载和切换View
- 通过RegionManager管理导航

**MainWindow.xaml中的Region定义**：
```xml
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns:prism="http://prismlibrary.com/"
        prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />   <!-- 主内容区 -->
            <RowDefinition Height="30" />  <!-- 底部状态栏 -->
        </Grid.RowDefinitions>

        <!-- 登录界面Region -->
        <Grid Grid.Row="0" Visibility="{Binding IsNotLoggedIn, Converter={StaticResource BooleanToVisibilityConverter}}">
            <ContentControl prism:RegionManager.RegionName="LoginRegion" />
        </Grid>

        <!-- 登录后的主界面 -->
        <Grid Grid.Row="0" Visibility="{Binding IsLoggedIn, Converter={StaticResource BooleanToVisibilityConverter}}">
            <Grid.RowDefinitions>
                <RowDefinition Height="60" />  <!-- 顶部工具栏 -->
                <RowDefinition Height="*" />   <!-- 主内容区 -->
            </Grid.RowDefinitions>

            <!-- 动态内容区域 -->
            <ContentControl Grid.Row="1" prism:RegionManager.RegionName="ContentRegion" />
        </Grid>
    </Grid>
</Window>
```

**ViewModel中使用Region导航**：
```csharp
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;

    public MedicalCaseListViewModel(IRegionManager regionManager, ...)
    {
        _regionManager = regionManager;
    }

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
    {
        _regionManager.RequestNavigate(regionName, viewName, parameters);
    }

    /// <summary>
    /// 编辑病历（带参数导航）
    /// </summary>
    private void Edit()
    {
        if (SelectedMedicalCase != null)
        {
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", SelectedMedicalCase.Id }
            };
            NavigateTo("ContentRegion", "MedicalCaseDetailView", parameters);
        }
    }
}
```

**Region导航生命周期**：
```csharp
public class MedicalCaseDetailViewModel : UnifiedViewModelBase, INavigationAware
{
    /// <summary>
    /// 导航到此视图时调用
    /// </summary>
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var parameters = navigationContext.Parameters;
        if (parameters.ContainsKey("MedicalCaseId"))
        {
            var id = parameters.GetValue<Guid>("MedicalCaseId");
            await LoadMedicalCaseAsync(id);
        }
    }

    /// <summary>
    /// 离开此视图时调用（可以阻止导航）
    /// </summary>
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 返回true：重用当前实例
        // 返回false：创建新实例
        return false;
    }

    /// <summary>
    /// 从此视图导航离开时调用
    /// </summary>
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 保存状态、释放资源
    }
}
```

**Region导航模式对比**：
| 特性 | Region Navigation | 传统Page Navigation |
|------|-------------------|---------------------|
| **视图容器** | ContentControl（Region） | Frame |
| **视图生命周期** | ViewModel控制 | Page生命周期 |
| **参数传递** | NavigationParameters（强类型） | QueryString（弱类型） |
| **ViewModel解耦** | INavigationAware | 耦合Page |
| **模块化支持** | ✅ 原生支持 | ❌ 需要自定义 |

### 3.2 Dialog Service（对话框服务）

**Dialog注册**（PrescriptionsModule.cs）：
```csharp
[Module(ModuleName = nameof(PrescriptionsModule))]
public class PrescriptionsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Phase 3: 启用 Prism Dialog 注册
        containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
        containerRegistry.RegisterDialog<Views.HerbSelectionDialog, HerbSelectionDialogViewModel>();
        containerRegistry.RegisterDialog<Views.SelectFormulaDialog, SelectFormulaDialogViewModel>();
    }
}
```

**Dialog使用**：
```csharp
public class MedicalCaseDetailViewModel : UnifiedViewModelBase
{
    private readonly IDialogService _dialogService;

    public MedicalCaseDetailViewModel(IDialogService dialogService, ...)
    {
        _dialogService = dialogService;
    }

    /// <summary>
    /// 选择药材（打开对话框）
    /// </summary>
    private void SelectHerb()
    {
        var parameters = new DialogParameters
        {
            { "SearchKeyword", "黄芪" }
        };

        _dialogService.ShowDialog("HerbSelectionDialog", parameters, result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var selectedHerb = result.Parameters.GetValue<HerbDto>("SelectedHerb");
                AddHerb(selectedHerb);
            }
        });
    }
}
```

**Dialog ViewModel实现**：
```csharp
public class HerbSelectionDialogViewModel : ViewModelBase, IDialogAware
{
    private HerbDto? _selectedHerb;

    public HerbDto? SelectedHerb
    {
        get => _selectedHerb;
        set => SetProperty(ref _selectedHerb, value);
    }

    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public HerbSelectionDialogViewModel()
    {
        ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
        CancelCommand = new DelegateCommand(Cancel);
    }

    #region IDialogAware实现

    /// <summary>
    /// 对话框标题
    /// </summary>
    public string Title => "选择药材";

    /// <summary>
    /// 对话框关闭事件
    /// </summary>
    public event Action<IDialogResult> RequestClose;

    /// <summary>
    /// 是否可以关闭对话框
    /// </summary>
    public bool CanCloseDialog() => true;

    /// <summary>
    /// 对话框关闭时调用
    /// </summary>
    public void OnDialogClosed()
    {
        // 清理资源
    }

    /// <summary>
    /// 对话框打开时调用
    /// </summary>
    public void OnDialogOpened(IDialogParameters parameters)
    {
        if (parameters.ContainsKey("SearchKeyword"))
        {
            var keyword = parameters.GetValue<string>("SearchKeyword");
            SearchHerbs(keyword);
        }
    }

    #endregion

    /// <summary>
    /// 确认选择
    /// </summary>
    private void Confirm()
    {
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("SelectedHerb", SelectedHerb);
        RequestClose?.Invoke(result);
    }

    /// <summary>
    /// 取消选择
    /// </summary>
    private void Cancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    private bool CanConfirm() => SelectedHerb != null;
}
```

**Dialog View示例**（HerbSelectionDialog.xaml）：
```xml
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.HerbSelectionDialog"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             Width="600" Height="400">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />     <!-- 药材列表 -->
            <RowDefinition Height="Auto" />  <!-- 按钮区域 -->
        </Grid.RowDefinitions>

        <!-- 药材列表 -->
        <ListBox Grid.Row="0"
                 ItemsSource="{Binding Herbs}"
                 SelectedItem="{Binding SelectedHerb}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{Binding Name}" FontWeight="Bold" Width="100" />
                        <TextBlock Text="{Binding Category}" Foreground="Gray" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- 按钮区域 -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
            <Button Content="确定"
                    Command="{Binding ConfirmCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"
                    Width="80" Margin="5" />

            <Button Content="取消"
                    Command="{Binding CancelCommand}"
                    Style="{StaticResource BaseButtonStyle}"
                    Width="80" Margin="5" />
        </StackPanel>
    </Grid>
</UserControl>
```

### 3.3 Module System（模块系统）

**模块定义**（PrescriptionsModule.cs）：
```csharp
/// <summary>
/// 处方管理模块 - 简化版
/// ADR-008: Desktop端已删除IPrescriptionRepository，所有操作通过MedicalCaseRepository聚合根
/// </summary>
[Module(ModuleName = nameof(PrescriptionsModule))]
[ModuleDependency("ConsultationModule")] // 处方依赖诊疗
[ModuleDependency("HerbsModule")] // 处方依赖药材
[ModuleDependency("FormulaModule")] // 处方依赖方剂
public class PrescriptionsModule : IModule
{
    /// <summary>
    /// 模块初始化（在所有类型注册后调用）
    /// </summary>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑
    }

    /// <summary>
    /// 注册模块类型（ViewModel、View、Service等）
    /// </summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册打印服务（Issue #1381: PRINT-4）
        containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();

        // Epic #1540: 注册处方编辑器服务（方案B - 包装模式）
        containerRegistry.RegisterSingleton<IPrescriptionEditorService, PrescriptionEditorService>();

        // Phase 2: 启用 Region Navigation 注册
        containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();
        containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();

        // Phase 3: 启用 Prism Dialog 注册
        containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
        containerRegistry.RegisterDialog<Views.HerbSelectionDialog, HerbSelectionDialogViewModel>();
        containerRegistry.RegisterDialog<Views.SelectFormulaDialog, SelectFormulaDialogViewModel>();
    }
}
```

**App.xaml.cs中加载模块**：
```csharp
public partial class App : PrismApplication
{
    /// <summary>
    /// 创建容器扩展
    /// </summary>
    protected override IContainerExtension CreateContainerExtension()
    {
        return new DryIocContainerExtension();
    }

    /// <summary>
    /// 配置模块目录（加载所有模块）
    /// </summary>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // Infrastructure模块（无UI，最先加载）
        moduleCatalog.AddModule<InfrastructureModule>();

        // 业务模块（按依赖顺序加载）
        moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable); // 认证模块
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable); // 患者模块
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable); // 药材模块
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable); // 验方模块
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable); // 诊疗模块
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.WhenAvailable); // 处方模块（依赖Consultation、Herbs、Formula）
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.WhenAvailable); // 病案模块
    }

    /// <summary>
    /// 创建Shell（主窗口）
    /// </summary>
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    /// <summary>
    /// 注册类型（全局服务）
    /// </summary>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册全局服务
        containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
        containerRegistry.RegisterSingleton<IUserNotificationService, UserNotificationService>();

        // 注册ILoggerFactory
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);
    }
}
```

**模块加载流程**：
```
┌────────────────────────────────────────────────────────────┐
│                     Prism模块加载流程                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  1. App.OnStartup()                                        │
│     └─→ ConfigureModuleCatalog()  // 配置模块目录          │
│                                                            │
│  2. Module Discovery                                       │
│     └─→ 解析[ModuleDependency]属性  // 构建依赖图          │
│                                                            │
│  3. Module Loading (按依赖顺序)                            │
│     └─→ InfrastructureModule.RegisterTypes()               │
│     └─→ AuthModule.RegisterTypes()                         │
│     └─→ PatientsModule.RegisterTypes()                     │
│     └─→ HerbsModule.RegisterTypes()                        │
│     └─→ FormulaModule.RegisterTypes()                      │
│     └─→ ConsultationModule.RegisterTypes()                 │
│     └─→ PrescriptionsModule.RegisterTypes() // 最后加载    │
│                                                            │
│  4. Module Initialization (按依赖顺序)                     │
│     └─→ InfrastructureModule.OnInitialized()               │
│     └─→ AuthModule.OnInitialized()                         │
│     └─→ ... (其他模块)                                     │
│                                                            │
│  5. CreateShell()                                          │
│     └─→ Container.Resolve<MainWindow>() // 创建主窗口     │
│                                                            │
│  6. Show MainWindow                                        │
│     └─→ MainWindow.Show() // 显示主窗口                    │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 3.4 ViewModelLocator（自动绑定）

**原理**：
- Prism通过约定命名自动关联View和ViewModel
- 约定：`Namespace.Views.FooView` → `Namespace.ViewModels.FooViewModel`

**自动绑定（推荐）**：
```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseListView"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    <!-- Prism自动解析并绑定MedicalCaseListViewModel -->
</UserControl>
```

**手动绑定（不推荐）**：
```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseListView">
    <UserControl.DataContext>
        <viewmodels:MedicalCaseListViewModel />
    </UserControl.DataContext>
</UserControl>
```

**ViewModelLocator工作流程**：
```
┌────────────────────────────────────────────────────────────┐
│              ViewModelLocator工作流程                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  1. View加载时触发                                          │
│     └─→ prism:ViewModelLocator.AutoWireViewModel="True"    │
│                                                            │
│  2. 解析View类型                                            │
│     └─→ Type viewType = typeof(MedicalCaseListView)        │
│                                                            │
│  3. 约定命名转换                                            │
│     └─→ viewType.FullName                                  │
│         = "LYBT.Desktop.MedicalCase.Views.MedicalCaseListView" │
│     └─→ Replace("Views", "ViewModels")                     │
│     └─→ Replace("View", "ViewModel")                       │
│     └─→ viewModelTypeName                                  │
│         = "LYBT.Desktop.MedicalCase.ViewModels.MedicalCaseListViewModel" │
│                                                            │
│  4. 从容器解析ViewModel                                     │
│     └─→ var viewModel = Container.Resolve<MedicalCaseListViewModel>(); │
│                                                            │
│  5. 设置DataContext                                         │
│     └─→ view.DataContext = viewModel;                      │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## 4. 样式系统

### 4.1 资源字典结构

**App.xaml中的全局样式合并**：
```xml
<prism:PrismApplication x:Class="LYBT.Desktop.Shell.App"
                        xmlns:prism="http://prismlibrary.com/">
    <Application.Resources>
        <ResourceDictionary>
            <!-- 合并全局样式系统资源 (Issue #1484) -->
            <ResourceDictionary.MergedDictionaries>
                <!-- 全局样式系统 (新) -->
                <ResourceDictionary Source="Styles/Colors.xaml" />
                <ResourceDictionary Source="Styles/Typography.xaml" />
                <ResourceDictionary Source="Styles/Controls.xaml" />

                <!-- 保留旧样式以兼容现有代码 -->
                <ResourceDictionary Source="Styles/CommonStyles.xaml" />
            </ResourceDictionary.MergedDictionaries>

            <!-- 基础转换器 - 引用 Infrastructure 中的统一定义 -->
            <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
            <converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter" />
            <converters:BoolToBrushConverter x:Key="BoolToBrushConverter" />
            <converters:StringToVisibilityConverter x:Key="StringToVisibilityConverter" />

            <!-- 应用级全局样式覆盖 -->
            <Style BasedOn="{StaticResource {x:Type TextBlock}}" TargetType="TextBlock">
                <Setter Property="FontFamily" Value="Microsoft YaHei" />
            </Style>

            <Style BasedOn="{StaticResource {x:Type TextBox}}" TargetType="TextBox">
                <Setter Property="FontFamily" Value="Microsoft YaHei" />
                <Setter Property="VerticalContentAlignment" Value="Center" />
            </Style>

            <Style BasedOn="{StaticResource {x:Type Button}}" TargetType="Button">
                <Setter Property="FontFamily" Value="Microsoft YaHei" />
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</prism:PrismApplication>
```

**样式系统架构**：
```
Shell/Styles/
├── Colors.xaml          # 颜色定义（Color + SolidColorBrush）
├── Typography.xaml      # 字体定义（FontFamily, FontSize, FontWeight）
├── Controls.xaml        # 控件样式（Button, TextBox, DataGrid等）
└── CommonStyles.xaml    # 通用样式（旧版，兼容性保留）
```

### 4.2 颜色系统（CommonStyles.xaml）

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 颜色定义 -->
    <Color x:Key="PrimaryColor">#2E86AB</Color>
    <Color x:Key="SecondaryColor">#A0C3D2</Color>
    <Color x:Key="AccentColor">#FF5722</Color>
    <Color x:Key="SuccessColor">#4CAF50</Color>
    <Color x:Key="WarningColor">#FFC107</Color>
    <Color x:Key="ErrorColor">#F44336</Color>
    <Color x:Key="BackgroundColor">#F5F5F5</Color>
    <Color x:Key="CardBackgroundColor">#FFFFFF</Color>
    <Color x:Key="BorderColor">#E0E0E0</Color>
    <Color x:Key="TextPrimaryColor">#212121</Color>
    <Color x:Key="TextSecondaryColor">#757575</Color>

    <!-- 扩展颜色定义 -->
    <Color x:Key="SurfaceColor">#F8F9FA</Color>
    <Color x:Key="StatusBarColor">#E9ECEF</Color>
    <Color x:Key="AlternateBorderColor">#DDD</Color>
    <Color x:Key="AlternateRowColor">#FAFAFA</Color>
    <Color x:Key="HoverColor">#F0F0F0</Color>
    <Color x:Key="PressedColor">#E0E0E0</Color>
    <Color x:Key="OverlayColor">#40FFFFFF</Color>

    <!-- 画刷定义 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="SecondaryBrush" Color="{StaticResource SecondaryColor}" />
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}" />
    <SolidColorBrush x:Key="WarningBrush" Color="{StaticResource WarningColor}" />
    <SolidColorBrush x:Key="ErrorBrush" Color="{StaticResource ErrorColor}" />
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}" />
    <SolidColorBrush x:Key="CardBackgroundBrush" Color="{StaticResource CardBackgroundColor}" />
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}" />
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}" />
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}" />

    <!-- 扩展画刷定义 -->
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}" />
    <SolidColorBrush x:Key="StatusBarBrush" Color="{StaticResource StatusBarColor}" />
    <SolidColorBrush x:Key="AlternateBorderBrush" Color="{StaticResource AlternateBorderColor}" />
    <SolidColorBrush x:Key="AlternateRowBrush" Color="{StaticResource AlternateRowColor}" />
    <SolidColorBrush x:Key="HoverBrush" Color="{StaticResource HoverColor}" />
    <SolidColorBrush x:Key="PressedBrush" Color="{StaticResource PressedColor}" />
    <SolidColorBrush x:Key="OverlayBrush" Color="{StaticResource OverlayColor}" />

    <!-- 字体大小 -->
    <system:Double x:Key="FontSizeSmall" xmlns:system="clr-namespace:System;assembly=mscorlib">12</system:Double>
    <system:Double x:Key="FontSizeNormal" xmlns:system="clr-namespace:System;assembly=mscorlib">14</system:Double>
    <system:Double x:Key="FontSizeMedium" xmlns:system="clr-namespace:System;assembly=mscorlib">16</system:Double>
    <system:Double x:Key="FontSizeLarge" xmlns:system="clr-namespace:System;assembly=mscorlib">18</system:Double>
    <system:Double x:Key="FontSizeXLarge" xmlns:system="clr-namespace:System;assembly=mscorlib">24</system:Double>

</ResourceDictionary>
```

**颜色使用示例**：
```xml
<!-- 使用预定义颜色 -->
<Button Background="{StaticResource PrimaryBrush}"
        Foreground="White"
        Content="主按钮" />

<TextBlock Foreground="{StaticResource TextPrimaryBrush}"
           FontSize="{StaticResource FontSizeLarge}"
           Text="标题" />
```

### 4.3 控件样式（CommonStyles.xaml）

**基础按钮样式**：
```xml
<!-- 基础按钮样式 -->
<Style x:Key="BaseButtonStyle" TargetType="Button">
    <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}" />
    <Setter Property="FontWeight" Value="Medium" />
    <Setter Property="Padding" Value="15,8" />
    <Setter Property="Margin" Value="5" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border x:Name="border"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="4">
                    <ContentPresenter HorizontalAlignment="Center"
                                      VerticalAlignment="Center"
                                      Margin="{TemplateBinding Padding}" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="border" Property="Opacity" Value="0.9" />
                    </Trigger>
                    <Trigger Property="IsPressed" Value="True">
                        <Setter TargetName="border" Property="Opacity" Value="0.8" />
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="border" Property="Opacity" Value="0.5" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- 主按钮样式 -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
    <Setter Property="Foreground" Value="White" />
</Style>

<!-- 成功按钮样式 -->
<Style x:Key="SuccessButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource SuccessBrush}" />
    <Setter Property="Foreground" Value="White" />
</Style>

<!-- 警告按钮样式 -->
<Style x:Key="WarningButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource WarningBrush}" />
    <Setter Property="Foreground" Value="White" />
</Style>

<!-- 危险按钮样式 -->
<Style x:Key="DangerButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource ErrorBrush}" />
    <Setter Property="Foreground" Value="White" />
</Style>
```

**输入框样式**：
```xml
<!-- 输入框样式 -->
<Style x:Key="ModernTextBoxStyle" TargetType="TextBox">
    <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}" />
    <Setter Property="Padding" Value="10,8" />
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Background" Value="White" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="TextBox">
                <Border x:Name="border"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        Background="{TemplateBinding Background}"
                        CornerRadius="4">
                    <ScrollViewer x:Name="PART_ContentHost"
                                  VerticalAlignment="Center"
                                  Margin="{TemplateBinding Padding}" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsFocused" Value="True">
                        <Setter TargetName="border" Property="BorderBrush" Value="{StaticResource PrimaryBrush}" />
                        <Setter TargetName="border" Property="BorderThickness" Value="2" />
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="border" Property="Background" Value="#F5F5F5" />
                        <Setter TargetName="border" Property="Opacity" Value="0.6" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

**DataGrid样式**：
```xml
<!-- DataGrid样式 -->
<Style x:Key="ModernDataGridStyle" TargetType="DataGrid">
    <Setter Property="AutoGenerateColumns" Value="False" />
    <Setter Property="CanUserAddRows" Value="False" />
    <Setter Property="GridLinesVisibility" Value="Horizontal" />
    <Setter Property="HorizontalGridLinesBrush" Value="{StaticResource BorderBrush}" />
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Background" Value="White" />
    <Setter Property="RowBackground" Value="White" />
    <Setter Property="AlternatingRowBackground" Value="#FAFAFA" />
    <Setter Property="ColumnHeaderHeight" Value="40" />
    <Setter Property="RowHeight" Value="35" />
    <Setter Property="FontSize" Value="{StaticResource FontSizeNormal}" />
</Style>

<!-- DataGrid列头样式 -->
<Style x:Key="DataGridColumnHeaderStyle" TargetType="DataGridColumnHeader">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="VerticalContentAlignment" Value="Center" />
    <Setter Property="Height" Value="40" />
</Style>
```

**卡片样式**：
```xml
<!-- 卡片样式 -->
<Style x:Key="CardStyle" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource CardBackgroundBrush}" />
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="5" />
    <Setter Property="Margin" Value="5" />
    <Setter Property="Effect">
        <Setter.Value>
            <DropShadowEffect Color="Gray" Direction="315" ShadowDepth="2" Opacity="0.2" BlurRadius="5" />
        </Setter.Value>
    </Setter>
</Style>
```

**样式使用示例**：
```xml
<!-- 使用预定义样式 -->
<Button Content="确定"
        Style="{StaticResource PrimaryButtonStyle}"
        Command="{Binding ConfirmCommand}" />

<TextBox Text="{Binding SearchText}"
         Style="{StaticResource ModernTextBoxStyle}"
         Width="250" />

<DataGrid ItemsSource="{Binding Items}"
          Style="{StaticResource ModernDataGridStyle}"
          ColumnHeaderStyle="{StaticResource DataGridColumnHeaderStyle}">
    <!-- ... -->
</DataGrid>

<Border Style="{StaticResource CardStyle}">
    <!-- 卡片内容 -->
</Border>
```

## 5. Converters（数据转换器）

### 5.1 Converter概念

**作用**：在数据绑定时进行数据类型转换或格式化。

**常见场景**：
- 布尔值转可见性（bool → Visibility）
- 枚举转字符串（enum → string）
- 日期格式化（DateTime → string）
- 状态转颜色（Status → Brush）
- 字符串转可见性（string → Visibility）

### 5.2 常用Converters

**BooleanToVisibilityConverter**（布尔值转可见性）：
```csharp
/// <summary>
/// 布尔值到可见性转换器
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}
```

**使用示例**：
```xml
<!-- 在App.xaml中全局注册 -->
<converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />

<!-- 在View中使用 -->
<Grid Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
    <ProgressBar IsIndeterminate="True" />
</Grid>
```

**InverseBooleanToVisibilityConverter**（反向布尔值转可见性）：
```csharp
/// <summary>
/// 反向布尔值到可见性转换器（True → Collapsed, False → Visible）
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}
```

**StatusToColorConverter**（状态转颜色）：
```csharp
/// <summary>
/// 状态到颜色转换器
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MedicalCaseStatus status)
        {
            return status switch
            {
                MedicalCaseStatus.Registered => new SolidColorBrush(Colors.Orange),
                MedicalCaseStatus.InConsultation => new SolidColorBrush(Colors.Blue),
                MedicalCaseStatus.Completed => new SolidColorBrush(Colors.Green),
                MedicalCaseStatus.Cancelled => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

**DateTimeFormatConverter**（日期格式化）：
```csharp
/// <summary>
/// 日期时间格式化转换器
/// </summary>
public class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            string format = parameter as string ?? "yyyy-MM-dd HH:mm:ss";
            return dateTime.ToString(format);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParse(str, out var dateTime))
        {
            return dateTime;
        }
        return DateTime.MinValue;
    }
}
```

**EnumDescriptionConverter**（枚举描述转换器）：
```csharp
/// <summary>
/// 枚举到描述文本的转换器（读取DescriptionAttribute）
/// </summary>
public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        var type = value.GetType();
        if (!type.IsEnum) return value.ToString() ?? string.Empty;

        var memberInfo = type.GetMember(value.ToString() ?? string.Empty).FirstOrDefault();
        if (memberInfo == null) return value.ToString() ?? string.Empty;

        var descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttribute?.Description ?? value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

**Converter注册和使用**：
```xml
<!-- 在App.xaml中全局注册 -->
<Application.Resources>
    <ResourceDictionary>
        <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
        <converters:InverseBooleanToVisibilityConverter x:Key="InverseBooleanToVisibilityConverter" />
        <converters:StatusToColorConverter x:Key="StatusToColorConverter" />
        <converters:DateTimeFormatConverter x:Key="DateTimeFormatConverter" />
        <converters:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
    </ResourceDictionary>
</Application.Resources>

<!-- 在View中使用 -->
<TextBlock Text="{Binding CreatedAt, Converter={StaticResource DateTimeFormatConverter}, ConverterParameter='yyyy-MM-dd'}"
           Foreground="{StaticResource TextSecondaryBrush}" />

<TextBlock Text="{Binding Status, Converter={StaticResource EnumDescriptionConverter}}"
           Foreground="{Binding Status, Converter={StaticResource StatusToColorConverter}}" />

<Grid Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
    <ProgressBar IsIndeterminate="True" />
</Grid>
```

### 5.3 Infrastructure中的Converters清单

**当前可用Converters**（来自`LYBT.Desktop.Infrastructure.Converters`）：
1. **BooleanToVisibilityConverter** - 布尔值转可见性
2. **InverseBooleanToVisibilityConverter** - 反向布尔值转可见性
3. **InverseBooleanConverter** - 布尔值取反
4. **BoolToBrushConverter** - 布尔值转画刷
5. **StringToVisibilityConverter** - 字符串转可见性
6. **NullToVisibilityConverter** - 空值转可见性
7. **ZeroToVisibilityConverter** - 零值转可见性
8. **DateTimeFormatConverter** - 日期格式化
9. **StatusToColorConverter** - 状态转颜色
10. **ApiHealthStatusToColorConverter** - API健康状态转颜色
11. **FirstCharacterConverter** - 提取首字符
12. **EnumConverters** - 枚举转换器集合
13. **EnumDescriptionConverter** - 枚举描述转换器

## 6. UnifiedViewModelBase（统一ViewModel基类）

### 6.1 基类概述

**继承层次**：
```
┌────────────────────────────────────────────────────────────┐
│                   ViewModel继承层次                         │
├────────────────────────────────────────────────────────────┤
│                                                            │
│   ViewModelBase                                            │
│   │  - 实现INotifyPropertyChanged                          │
│   │  - 提供SetProperty()方法                               │
│   │  - 提供IsBusy状态管理                                   │
│   │  - 提供Logger                                          │
│   │  - 提供EventAggregator                                 │
│   └─→ UnifiedViewModelBase                                 │
│         │  - 实现INavigationAware                          │
│         │  - 提供Region导航支持                             │
│         │  - 提供错误处理                                   │
│         │  - 提供会话管理                                   │
│         │  - 提供通知服务                                   │
│         └─→ MedicalCaseListViewModel                       │
│            PrescriptionEditorViewModel                     │
│            PatientListViewModel                            │
│            ... (所有业务ViewModel)                         │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 6.2 ViewModelBase（底层基类）

**职责**：
- 实现`INotifyPropertyChanged`
- 提供属性变更通知机制
- 提供IsBusy状态管理
- 提供Logger和EventAggregator

**核心代码**（简化版）：
```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    protected readonly IEventAggregator EventAggregator;
    protected readonly ILogger Logger;

    private bool _isBusy;
    private string _busyMessage = string.Empty;

    public ViewModelBase(IEventAggregator eventAggregator, ILoggerFactory loggerFactory)
    {
        EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        Logger = loggerFactory.CreateLogger(GetType());
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region IsBusy状态管理

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        private set => SetProperty(ref _busyMessage, value);
    }

    protected void SetIsBusy(bool isBusy, string message = "")
    {
        IsBusy = isBusy;
        BusyMessage = message;
    }

    #endregion
}
```

### 6.3 UnifiedViewModelBase（统一基类）

**职责**：
- 实现`INavigationAware`（Prism导航生命周期）
- 提供Region导航支持（NavigateTo, NavigateBack, NavigateForward）
- 提供错误处理（HandleError, ShowErrorMessageAsync）
- 提供会话管理（ISessionManager）
- 提供通知服务（IUserNotificationService）
- 提供异步初始化（InitializeAsync）

**核心代码**（部分）：
```csharp
/// <summary>
/// 统一ViewModel基类 - UltraThink架构重构版本
/// 提供统一的导航、错误处理、会话管理功能
/// Issue #1240: 添加自定义 InitializeAsync 支持，优化异步导航模式
/// </summary>
public abstract class UnifiedViewModelBase : ViewModelBase, INavigationAware, IRegionMemberLifetime
{
    #region 依赖服务

    protected readonly IRegionManager RegionManager;
    protected readonly ISessionManager? SessionManager;
    protected readonly IUserNotificationService? UserNotificationService;

    #endregion

    #region 页面属性

    private string _pageTitle = string.Empty;

    /// <summary>
    /// 页面标题
    /// </summary>
    public string PageTitle
    {
        get => _pageTitle;
        protected set => SetProperty(ref _pageTitle, value);
    }

    #endregion

    #region 构造函数

    protected UnifiedViewModelBase(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory)
    {
        RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        SessionManager = sessionManager;
        UserNotificationService = userNotificationService;
    }

    #endregion

    #region 导航支持

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
    {
        try
        {
            Logger.LogDebug("导航到视图: {ViewName} (区域: {RegionName})", viewName, regionName);

            parameters ??= new NavigationParameters();
            RegionManager.RequestNavigate(regionName, viewName, parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航失败: {ViewName}", viewName);
            HandleError(ex, "导航");
        }
    }

    /// <summary>
    /// 导航回退
    /// </summary>
    protected virtual void NavigateBack(string regionName)
    {
        try
        {
            var region = RegionManager.Regions[regionName];
            if (region?.NavigationService?.Journal?.CanGoBack == true)
            {
                region.NavigationService.Journal.GoBack();
                Logger.LogDebug("导航回退成功: {RegionName}", regionName);
            }
            else
            {
                Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航回退失败");
            HandleError(ex, "导航回退");
        }
    }

    #endregion

    #region 错误处理

    /// <summary>
    /// 处理错误
    /// </summary>
    protected virtual void HandleError(Exception ex, string context = "操作")
    {
        Logger.LogError(ex, "{Context}时发生异常", context);
        ShowErrorMessage($"{context}失败：{ex.Message}");
    }

    /// <summary>
    /// 显示错误信息
    /// </summary>
    protected virtual void ShowErrorMessage(string message)
    {
        UserNotificationService?.ShowErrorMessage(message);
    }

    /// <summary>
    /// 显示错误信息（异步）
    /// </summary>
    protected virtual async Task ShowErrorMessageAsync(string message)
    {
        if (UserNotificationService != null)
        {
            await UserNotificationService.ShowErrorMessageAsync(message);
        }
    }

    /// <summary>
    /// 显示成功信息（异步）
    /// </summary>
    protected virtual async Task ShowSuccessMessageAsync(string message)
    {
        if (UserNotificationService != null)
        {
            await UserNotificationService.ShowSuccessMessageAsync(message);
        }
    }

    /// <summary>
    /// 显示确认对话框（异步）
    /// </summary>
    protected virtual async Task<bool> ShowConfirmMessageAsync(string message)
    {
        if (UserNotificationService != null)
        {
            return await UserNotificationService.ShowConfirmMessageAsync(message);
        }
        return false;
    }

    #endregion

    #region INavigationAware实现

    /// <summary>
    /// 导航到此视图时调用（同步入口）
    /// Issue #1240: 检测是否实现了自定义InitializeAsync，如果有则异步调用
    /// </summary>
    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        try
        {
            var parameters = navigationContext.Parameters;
            Logger.LogDebug("OnNavigatedTo: {Parameters}", parameters);

            // 异步初始化（不阻塞UI线程）
            _ = InitializeAsync(parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnNavigatedTo执行失败");
            HandleError(ex, "页面导航");
        }
    }

    /// <summary>
    /// 离开此视图时调用（可以阻止导航）
    /// </summary>
    public virtual bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 返回true：重用当前实例
        // 返回false：创建新实例
        return false;
    }

    /// <summary>
    /// 从此视图导航离开时调用
    /// </summary>
    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Logger.LogDebug("OnNavigatedFrom");
    }

    #endregion

    #region 异步初始化支持

    /// <summary>
    /// 页面异步初始化（子类可重写）
    /// Issue #1240: 子类可以重写此方法实现异步初始化逻辑
    /// </summary>
    protected virtual async Task InitializeAsync(NavigationParameters parameters)
    {
        await Task.CompletedTask;
    }

    #endregion

    #region IRegionMemberLifetime实现

    /// <summary>
    /// 视图是否保持活动状态（默认：否）
    /// </summary>
    public virtual bool KeepAlive => false;

    #endregion
}
```

**使用示例**：
```csharp
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    public MedicalCaseListViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IUserNotificationService userNotificationService)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
    }

    /// <summary>
    /// 页面加载时调用（异步初始化）
    /// </summary>
    protected override async Task InitializeAsync(NavigationParameters parameters)
    {
        await base.InitializeAsync(parameters);
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            SetIsBusy(true, "正在加载数据...");
            // 加载数据
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载数据失败");
            await ShowErrorMessageAsync("加载数据失败，请稍后重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }
}
```

## 7. 数据绑定模式

### 7.1 属性绑定

**单向绑定（OneWay）**：
```xml
<!-- ViewModel → View（默认模式） -->
<TextBlock Text="{Binding PatientName}" />
<TextBlock Text="{Binding PatientName, Mode=OneWay}" />
```

**双向绑定（TwoWay）**：
```xml
<!-- ViewModel ↔ View -->
<TextBox Text="{Binding SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

**单次绑定（OneTime）**：
```xml
<!-- 仅初始化时绑定一次，后续不更新 -->
<TextBlock Text="{Binding InitialValue, Mode=OneTime}" />
```

**绑定模式对比**：
| 绑定模式 | 数据流向 | 更新时机 | 性能 | 使用场景 |
|---------|---------|---------|------|---------|
| **OneWay**（默认） | ViewModel → View | 属性变更时 | 中 | 只读数据显示 |
| **TwoWay** | ViewModel ↔ View | 属性变更时 + 控件失去焦点 | 低 | 表单输入 |
| **OneTime** | ViewModel → View | 初始化时 | 高 | 静态数据显示 |
| **OneWayToSource** | View → ViewModel | 控件值变更时 | 中 | 特殊场景 |

**UpdateSourceTrigger模式**：
```xml
<!-- 失去焦点时更新（默认） -->
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=LostFocus}" />

<!-- 属性变更时立即更新（实时搜索） -->
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" />

<!-- 显式调用UpdateSource()时更新 -->
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=Explicit}" />
```

### 7.2 命令绑定

**普通命令绑定**：
```xml
<!-- 绑定DelegateCommand -->
<Button Content="搜索"
        Command="{Binding SearchCommand}" />

<!-- 绑定带参数的DelegateCommand<T> -->
<Button Content="查看"
        Command="{Binding ViewDetailCommand}"
        CommandParameter="{Binding SelectedItem}" />
```

**DataGrid行命令绑定**：
```xml
<DataGrid ItemsSource="{Binding MedicalCases}">
    <DataGrid.Columns>
        <DataGridTemplateColumn Header="操作">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <Button Content="查看"
                            Command="{Binding DataContext.ViewDetailCommand,
                                     RelativeSource={RelativeSource AncestorType=DataGrid}}"
                            CommandParameter="{Binding}" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

**快捷键绑定**：
```xml
<Window.InputBindings>
    <!-- Ctrl+N: 快速添加患者 -->
    <KeyBinding Key="N" Modifiers="Ctrl" Command="{Binding QuickAddPatientCommand}" />
    <!-- Ctrl+Shift+C: 快速开始诊疗 -->
    <KeyBinding Key="C" Modifiers="Ctrl+Shift" Command="{Binding QuickStartConsultationCommand}" />
    <!-- F1: 显示帮助 -->
    <KeyBinding Key="F1" Command="{Binding ShowHelpCommand}" />
</Window.InputBindings>
```

### 7.3 相对源绑定（RelativeSource）

**绑定父级DataContext**：
```xml
<!-- 绑定父级DataGrid的DataContext -->
<Button Command="{Binding DataContext.DeleteCommand,
                 RelativeSource={RelativeSource AncestorType=DataGrid}}"
        CommandParameter="{Binding}" />
```

**绑定自身属性**：
```xml
<!-- 绑定自身的ActualWidth -->
<TextBlock Text="{Binding ActualWidth, RelativeSource={RelativeSource Self}}" />
```

**绑定模板父级**：
```xml
<ControlTemplate TargetType="Button">
    <!-- 绑定Button的Background属性 -->
    <Border Background="{TemplateBinding Background}" />
</ControlTemplate>
```

### 7.4 多重绑定（MultiBinding）

**StringFormat格式化**：
```xml
<!-- 格式化多个属性为一个字符串 -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="第 {0} 页，共 {1} 条记录">
            <Binding Path="CurrentPage" />
            <Binding Path="TotalCount" />
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

**MultiValueConverter自定义转换**：
```xml
<!-- 使用MultiValueConverter合并多个属性 -->
<TextBlock Visibility="{Binding Converter={StaticResource AndBoolToVisibilityConverter}}">
    <TextBlock.Visibility>
        <MultiBinding Converter="{StaticResource AndBoolToVisibilityConverter}">
            <Binding Path="IsLoggedIn" />
            <Binding Path="HasPermission" />
        </MultiBinding>
    </TextBlock.Visibility>
</TextBlock>
```

## 8. 最佳实践

### 8.1 View最佳实践

**1. 使用ViewModelLocator自动绑定**：
```xml
<!-- ✅ 推荐：自动绑定ViewModel -->
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseListView"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
</UserControl>

<!-- ❌ 不推荐：手动绑定ViewModel -->
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseListView">
    <UserControl.DataContext>
        <viewmodels:MedicalCaseListViewModel />
    </UserControl.DataContext>
</UserControl>
```

**2. 合并ResourceDictionary**：
```xml
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="../../../Resources/ManagementModuleStyles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>
```

**3. 使用预定义样式**：
```xml
<!-- ✅ 推荐：使用预定义样式 -->
<Button Content="确定" Style="{StaticResource PrimaryButtonStyle}" />

<!-- ❌ 不推荐：内联样式 -->
<Button Content="确定" Background="#2E86AB" Foreground="White" Padding="15,8" />
```

**4. UpdateSourceTrigger优化**：
```xml
<!-- ✅ 实时搜索：使用PropertyChanged -->
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" />

<!-- ✅ 表单输入：使用LostFocus（默认） -->
<TextBox Text="{Binding PatientName}" />
```

**5. 避免在XAML中编写复杂逻辑**：
```xml
<!-- ❌ 不推荐：复杂的Visibility逻辑 -->
<Grid>
    <Grid.Style>
        <Style TargetType="Grid">
            <Style.Triggers>
                <MultiDataTrigger>
                    <MultiDataTrigger.Conditions>
                        <Condition Binding="{Binding IsLoggedIn}" Value="True" />
                        <Condition Binding="{Binding HasPermission}" Value="True" />
                    </MultiDataTrigger.Conditions>
                    <Setter Property="Visibility" Value="Visible" />
                </MultiDataTrigger>
            </Style.Triggers>
        </Style>
    </Grid.Style>
</Grid>

<!-- ✅ 推荐：在ViewModel中计算Visibility属性 -->
<Grid Visibility="{Binding CanAccessContent, Converter={StaticResource BooleanToVisibilityConverter}}" />
```

### 8.2 ViewModel最佳实践

**1. 继承UnifiedViewModelBase**：
```csharp
// ✅ 推荐：继承统一基类
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    public MedicalCaseListViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IUserNotificationService userNotificationService)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
    }
}

// ❌ 不推荐：从头实现INotifyPropertyChanged
public class MedicalCaseListViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    // ... 需要手动实现所有基础功能
}
```

**2. 构造函数注入依赖**：
```csharp
// ✅ 推荐：构造函数注入
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public MedicalCaseListViewModel(
        IMedicalCaseRepository medicalCaseRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
    }
}

// ❌ 不推荐：Service Locator反模式
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    private IMedicalCaseRepository _medicalCaseRepository;

    public MedicalCaseListViewModel()
    {
        _medicalCaseRepository = Container.Resolve<IMedicalCaseRepository>(); // 反模式
    }
}
```

**3. 使用SetProperty触发属性变更通知**：
```csharp
// ✅ 推荐：使用SetProperty
private string _searchText = string.Empty;
public string SearchText
{
    get => _searchText;
    set => SetProperty(ref _searchText, value);
}

// ❌ 不推荐：手动触发通知
private string _searchText = string.Empty;
public string SearchText
{
    get => _searchText;
    set
    {
        _searchText = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
    }
}
```

**4. 命令CanExecute管理**：
```csharp
public DelegateCommand EditCommand { get; }

public MedicalCaseListViewModel(...)
{
    EditCommand = new DelegateCommand(Edit, CanEdit);

    // 属性变更时刷新命令状态
    PropertyChanged += (s, e) => UpdateCommandStates();
}

private bool CanEdit() => SelectedMedicalCase != null && !IsBusy;

private void UpdateCommandStates()
{
    EditCommand.RaiseCanExecuteChanged();
}
```

**5. 异步操作规范**：
```csharp
// ✅ 推荐：完整的异步错误处理
private async Task LoadDataAsync()
{
    try
    {
        SetIsBusy(true, "正在加载数据...");

        var data = await _medicalCaseRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
        MedicalCases.Clear();
        foreach (var item in data.Items)
        {
            MedicalCases.Add(item);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载数据失败");
        await ShowErrorMessageAsync("加载数据失败，请稍后重试");
    }
    finally
    {
        SetIsBusy(false);
    }
}

// ❌ 不推荐：缺少错误处理和状态管理
private async Task LoadDataAsync()
{
    var data = await _medicalCaseRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
    MedicalCases = new ObservableCollection<MedicalCaseDto>(data.Items);
}
```

**6. 避免在ViewModel中直接操作UI元素**：
```csharp
// ❌ 不推荐：直接操作UI元素
private void ShowMessage()
{
    MessageBox.Show("操作成功"); // ViewModel不应依赖MessageBox
}

// ✅ 推荐：通过IUserNotificationService
private async Task ShowMessageAsync()
{
    await ShowSuccessMessageAsync("操作成功");
}
```

### 8.3 Prism最佳实践

**1. Region名称约定**：
```csharp
// ✅ 推荐：使用常量定义Region名称
public static class RegionNames
{
    public const string LoginRegion = "LoginRegion";
    public const string ContentRegion = "ContentRegion";
    public const string ToolbarRegion = "ToolbarRegion";
}

// 使用
NavigateTo(RegionNames.ContentRegion, "MedicalCaseListView");

// ❌ 不推荐：硬编码Region名称
NavigateTo("ContentRegion", "MedicalCaseListView");
```

**2. NavigationParameters强类型**：
```csharp
// ✅ 推荐：使用扩展方法封装参数
public static class NavigationParameterExtensions
{
    public static void AddMedicalCaseId(this NavigationParameters parameters, Guid id)
    {
        parameters.Add("MedicalCaseId", id);
    }

    public static Guid GetMedicalCaseId(this NavigationParameters parameters)
    {
        return parameters.GetValue<Guid>("MedicalCaseId");
    }
}

// 使用
var parameters = new NavigationParameters();
parameters.AddMedicalCaseId(SelectedMedicalCase.Id);
NavigateTo(RegionNames.ContentRegion, "MedicalCaseDetailView", parameters);

// ❌ 不推荐：硬编码参数名称
var parameters = new NavigationParameters { { "MedicalCaseId", SelectedMedicalCase.Id } };
```

**3. Dialog参数封装**：
```csharp
// ✅ 推荐：封装Dialog参数
public static class DialogNames
{
    public const string HerbSelectionDialog = "HerbSelectionDialog";
    public const string ConfirmationDialog = "ConfirmationDialog";
}

public void ShowHerbSelectionDialog()
{
    var parameters = new DialogParameters
    {
        { "SearchKeyword", "黄芪" }
    };

    _dialogService.ShowDialog(DialogNames.HerbSelectionDialog, parameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            var herb = result.Parameters.GetValue<HerbDto>("SelectedHerb");
            AddHerb(herb);
        }
    });
}
```

**4. Module依赖管理**：
```csharp
// ✅ 推荐：明确声明模块依赖
[Module(ModuleName = nameof(PrescriptionsModule))]
[ModuleDependency("ConsultationModule")] // 处方依赖诊疗
[ModuleDependency("HerbsModule")] // 处方依赖药材
[ModuleDependency("FormulaModule")] // 处方依赖方剂
public class PrescriptionsModule : IModule
{
}

// ❌ 不推荐：隐式依赖（运行时可能出错）
[Module(ModuleName = nameof(PrescriptionsModule))]
public class PrescriptionsModule : IModule
{
}
```

### 8.4 样式系统最佳实践

**1. 使用全局样式资源**：
```xml
<!-- ✅ 推荐：使用全局定义的样式 -->
<Button Style="{StaticResource PrimaryButtonStyle}" />

<!-- ❌ 不推荐：内联样式 -->
<Button Background="#2E86AB" Foreground="White" Padding="15,8" CornerRadius="4" />
```

**2. 样式继承（BasedOn）**：
```xml
<!-- ✅ 推荐：使用BasedOn继承基础样式 -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
    <Setter Property="Foreground" Value="White" />
</Style>

<!-- ❌ 不推荐：重复定义所有属性 -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Padding" Value="15,8" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="Background" Value="#2E86AB" />
    <Setter Property="Foreground" Value="White" />
    <!-- ... 重复的属性 -->
</Style>
```

**3. 颜色使用StaticResource**：
```xml
<!-- ✅ 推荐：使用预定义的颜色资源 -->
<Button Background="{StaticResource PrimaryBrush}"
        Foreground="White" />

<!-- ❌ 不推荐：硬编码颜色值 -->
<Button Background="#2E86AB" Foreground="#FFFFFF" />
```

**4. 模块化样式文件**：
```
Shell/Styles/
├── Colors.xaml          # 颜色定义
├── Typography.xaml      # 字体定义
├── Controls.xaml        # 控件样式
└── CommonStyles.xaml    # 通用样式
```

### 8.5 性能最佳实践

**1. 使用VirtualizingStackPanel**：
```xml
<!-- ✅ 推荐：启用虚拟化（DataGrid默认启用） -->
<DataGrid ItemsSource="{Binding MedicalCases}"
          VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling" />

<!-- ListBox也要启用虚拟化 -->
<ListBox ItemsSource="{Binding Herbs}"
         VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling" />
```

**2. 避免频繁触发PropertyChanged**：
```csharp
// ✅ 推荐：批量更新后触发一次通知
public void UpdatePatientInfo(string name, int age, string gender)
{
    _patientName = name;
    _patientAge = age;
    _patientGender = gender;

    OnPropertyChanged(nameof(PatientName));
    OnPropertyChanged(nameof(PatientAge));
    OnPropertyChanged(nameof(PatientGender));
}

// ❌ 不推荐：每次设置都触发通知
public void UpdatePatientInfo(string name, int age, string gender)
{
    PatientName = name; // 触发通知
    PatientAge = age;   // 触发通知
    PatientGender = gender; // 触发通知
}
```

**3. 使用OneTime绑定优化静态数据**：
```xml
<!-- ✅ 推荐：静态数据使用OneTime -->
<TextBlock Text="{Binding CompanyName, Mode=OneTime}" />

<!-- ❌ 不推荐：静态数据使用OneWay（默认） -->
<TextBlock Text="{Binding CompanyName}" />
```

**4. 避免在DataTemplate中使用复杂的Converter**：
```xml
<!-- ❌ 不推荐：每一行都执行复杂转换 -->
<DataGrid ItemsSource="{Binding MedicalCases}">
    <DataGrid.Columns>
        <DataGridTextColumn Binding="{Binding CreatedAt, Converter={StaticResource ComplexDateTimeFormatConverter}}" />
    </DataGrid.Columns>
</DataGrid>

<!-- ✅ 推荐：在ViewModel中预计算 -->
public class MedicalCaseDto
{
    public DateTime CreatedAt { get; set; }
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"); // 预计算
}

<DataGridTextColumn Binding="{Binding CreatedAtFormatted}" />
```

**5. 避免在循环中添加ObservableCollection**：
```csharp
// ❌ 不推荐：每次Add都触发CollectionChanged
foreach (var item in newItems)
{
    MedicalCases.Add(item); // 触发多次通知
}

// ✅ 推荐：批量添加（如果支持）或一次性替换
var newCollection = new ObservableCollection<MedicalCaseDto>(newItems);
MedicalCases = newCollection; // 触发一次通知

// ✅ 或者：临时禁用通知
MedicalCases.Clear();
foreach (var item in newItems)
{
    MedicalCases.Add(item);
}
```

## 9. 总结

### 9.1 核心优势

**呈现层架构优势**：
1. ✅ **MVVM模式**：实现View和业务逻辑的完全解耦
2. ✅ **Prism框架**：提供模块化、导航、对话框等企业级功能
3. ✅ **统一基类**：UnifiedViewModelBase提供导航、错误处理、会话管理等通用功能
4. ✅ **样式系统**：全局样式资源实现UI一致性
5. ✅ **数据绑定**：WPF强大的数据绑定引擎实现声明式UI
6. ✅ **Converter系统**：灵活的数据转换机制
7. ✅ **依赖注入**：通过Prism.Ioc实现松耦合
8. ✅ **模块化设计**：业务模块独立开发、独立部署

### 9.2 关键技术

**必须掌握的技术栈**：
- ✅ WPF XAML（布局、控件、样式、数据绑定）
- ✅ Prism Framework（Module、Region、Dialog、ViewModelLocator）
- ✅ MVVM模式（View、ViewModel、Model分离）
- ✅ INotifyPropertyChanged（属性变更通知）
- ✅ DelegateCommand（命令模式）
- ✅ IValueConverter（数据转换器）
- ✅ 依赖注入（构造函数注入）
- ✅ 异步编程（async/await）

### 9.3 维护规范

**开发规范**：
1. ✅ **View零逻辑**：所有业务逻辑在ViewModel中实现
2. ✅ **继承统一基类**：所有ViewModel继承UnifiedViewModelBase
3. ✅ **构造函数注入**：禁止使用Container.Resolve（Service Locator反模式）
4. ✅ **使用预定义样式**：禁止内联样式
5. ✅ **命名约定**：View命名为`*View.xaml`，ViewModel命名为`*ViewModel.cs`
6. ✅ **Region导航**：使用常量定义Region名称
7. ✅ **异步操作**：所有I/O操作必须使用async/await
8. ✅ **错误处理**：所有异步操作必须包含try-catch
9. ✅ **日志记录**：关键操作必须记录日志
10. ✅ **IsBusy状态管理**：长时间操作必须显示加载遮罩

**文档维护**：
- 新增View/ViewModel时更新此文档
- 新增Converter时更新Converter清单
- 样式系统变更时更新样式系统章节
- 发现新的最佳实践时更新最佳实践章节

---

## 附录

### A. 相关文档

**架构文档**：
- [Client端架构概览](../client/README.md) - Client端整体架构
- [Client端契约层设计](../client/contracts-design.md) - 契约层架构
- [Client端数据层设计](../client/data-design.md) - 数据层架构（如有）

**开发指南**：
- [Client端开发指南](../../how-to-guides/client/README.md) - Client端开发总览
- [MVVM模式实战指南](../../how-to-guides/client/mvvm-patterns.md) - MVVM实战（如有）
- [Prism框架使用指南](../../how-to-guides/client/prism-usage.md) - Prism详解（如有）

**参考资料**：
- [WPF官方文档](https://docs.microsoft.com/zh-cn/dotnet/desktop/wpf/)
- [Prism官方文档](https://prismlibrary.com/docs/)
- [MVVM模式指南](https://docs.microsoft.com/zh-cn/dotnet/architecture/modern-web-apps-azure/architectural-principles#mvvm)

### B. 目录结构

**Client端呈现层目录结构**：
```
src/Client/Desktop/
├── Shell/                          # Shell主程序
│   ├── App.xaml                    # 应用程序入口
│   ├── App.xaml.cs                 # 应用程序代码
│   ├── Views/
│   │   └── MainWindow.xaml         # 主窗口
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs  # 主窗口ViewModel
│   ├── Styles/                     # 样式系统
│   │   ├── Colors.xaml             # 颜色定义
│   │   ├── Typography.xaml         # 字体定义
│   │   ├── Controls.xaml           # 控件样式
│   │   └── CommonStyles.xaml       # 通用样式
│   └── Dialogs/                    # 全局对话框
│       ├── Views/
│       │   └── ConfirmationDialog.xaml
│       └── ViewModels/
│           └── ConfirmationDialogViewModel.cs
│
├── Core/                           # 核心基础设施
│   ├── LYBT.Desktop.Infrastructure/
│   │   ├── Converters/             # 全局Converter
│   │   │   ├── BooleanToVisibilityConverter.cs
│   │   │   ├── InverseBooleanToVisibilityConverter.cs
│   │   │   ├── StatusToColorConverter.cs
│   │   │   └── ... (13个Converter)
│   │   └── Interfaces/
│   │       ├── ISessionManager.cs
│   │       └── IUserNotificationService.cs
│   │
│   ├── LYBT.Desktop.Models/
│   │   └── ViewModels/
│   │       └── Base/
│   │           ├── ViewModelBase.cs           # 底层基类
│   │           └── UnifiedViewModelBase.cs    # 统一基类
│   │
│   └── LYBT.Desktop.Contracts/     # 契约层（API接口、Service接口）
│
└── Modules/                        # 业务模块
    ├── LYBT.Desktop.MedicalCase/   # 病案模块
    │   ├── Views/
    │   │   ├── MedicalCaseListView.xaml
    │   │   ├── MedicalCaseDetailView.xaml
    │   │   └── ...
    │   ├── ViewModels/
    │   │   ├── MedicalCaseListViewModel.cs
    │   │   ├── MedicalCaseDetailViewModel.cs
    │   │   └── ...
    │   └── MedicalCaseModule.cs    # Prism模块注册
    │
    ├── LYBT.Desktop.Prescriptions/ # 处方模块
    │   ├── Views/
    │   ├── ViewModels/
    │   └── PrescriptionsModule.cs
    │
    └── ... (其他模块)
```

### C. 常见问题

**Q1: ViewModelLocator无法自动绑定ViewModel？**
- 检查View命名是否符合约定（`*View.xaml`）
- 检查ViewModel命名是否符合约定（`*ViewModel.cs`）
- 检查ViewModel是否在模块中注册
- 检查`prism:ViewModelLocator.AutoWireViewModel="True"`是否设置

**Q2: 数据绑定无效？**
- 检查属性是否实现INotifyPropertyChanged
- 检查是否使用SetProperty触发通知
- 检查DataContext是否正确
- 使用Snoop工具调试绑定问题

**Q3: 命令无法执行？**
- 检查命令是否正确初始化
- 检查CanExecute是否返回true
- 检查命令绑定路径是否正确
- 检查DataContext是否正确

**Q4: Region导航失败？**
- 检查Region名称是否正确
- 检查View是否通过RegisterForNavigation注册
- 检查Region是否在XAML中定义
- 查看日志输出错误信息

**Q5: Dialog无法显示？**
- 检查Dialog是否通过RegisterDialog注册
- 检查DialogService是否正确注入
- 检查Dialog View和ViewModel是否正确实现
- 检查IDialogAware接口是否正确实现

**Q6: 样式不生效？**
- 检查ResourceDictionary是否正确合并
- 检查样式Key是否正确
- 检查样式TargetType是否匹配
- 使用Live Visual Tree查看样式应用情况

**Q7: 性能问题（界面卡顿）？**
- 检查是否启用虚拟化（DataGrid、ListBox）
- 检查是否频繁触发PropertyChanged
- 检查DataTemplate中是否有复杂的Converter
- 使用WPF Performance Profiler分析性能瓶颈
