# Clinical模块开发指南

> **文档类型**: How-to Guide
> **目标读者**: Client端开发人员
> **前置阅读**: [Clinical模块架构设计](../../explanation/architecture/client/clinical-module-design.md)

---

## 1. 概述

本指南提供Clinical模块的完整实施步骤，包括模块创建、ViewModel实现、View设计、权限控制集成等。

**模块职责**：
- 待诊列表管理（医生只看自己的病案）
- 快速创建病案
- 今日工作总结
- 病案详情查看与编辑
- 实时病案状态更新

**遵循规范**：
- 业务规则：AC-001（医生只能查看自己的医案）、AC-002（角色路由规则）
- 架构模式：MVVM + Prism + 事件驱动
- 代码规范：PascalCase、依赖注入、异步编程

---

## 2. 前置条件

### 环境要求
- Visual Studio 2022 17.8+
- .NET 8.0 SDK
- LYBT项目已克隆到本地
- WebAPI已启动（`https://localhost:5001`）

### 依赖检查
```powershell
# 检查.NET版本
dotnet --version  # 应该显示 8.0.x

# 检查Prism包
dotnet list package | Select-String "Prism"

# 检查Server端运行状态
curl https://localhost:5001/api/v1/health

# 检查MedicalCase API端点
curl https://localhost:5001/api/v1/medicalcases/pending -H "Authorization: Bearer <token>"
```

### 必读文档
- `docs/explanation/architecture/client/README.md` - Client端架构总览
- `docs/explanation/architecture/client/clinical-module-design.md` - Clinical模块架构设计
- `docs/explanation/business-rules.md` - 业务规则AC-001、AC-002

---

## 3. 模块结构创建

### Step 1: 创建项目结构

在 `src/Client/Desktop/Modules/` 目录下创建Clinical模块项目：

```powershell
cd D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules

# 创建Clinical模块项目
dotnet new classlib -n LYBT.Desktop.Clinical -f net8.0-windows

# 创建标准目录结构
cd LYBT.Desktop.Clinical
mkdir ViewModels
mkdir Views
mkdir Models
mkdir Dialogs
```

### Step 2: 添加Prism依赖

编辑 `LYBT.Desktop.Clinical.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Prism框架 -->
    <PackageReference Include="Prism.Unity" Version="8.1.537" />
    <PackageReference Include="Prism.Wpf" Version="8.1.537" />

    <!-- 日志 -->
    <PackageReference Include="NLog" Version="5.2.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- 项目引用 -->
    <ProjectReference Include="..\..\Foundation\LYBT.Desktop.Foundation\LYBT.Desktop.Foundation.csproj" />
    <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Contracts\LYBT.Shared.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: 创建Prism模块类

创建 `ClinicalModule.cs`：

```csharp
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using LYBT.Desktop.Clinical.Views;
using LYBT.Desktop.Clinical.ViewModels;

namespace LYBT.Desktop.Clinical;

/// <summary>
/// Clinical模块定义
/// </summary>
public class ClinicalModule : IModule
{
    private readonly IRegionManager _regionManager;

    public ClinicalModule(IRegionManager regionManager)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化后的逻辑（如需要）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModels（自动解析）
        containerRegistry.RegisterForNavigation<ClinicalHomeView, ClinicalHomeViewModel>();
        containerRegistry.RegisterForNavigation<PendingListView, PendingListViewModel>();
        containerRegistry.RegisterForNavigation<QuickCreateView, QuickCreateViewModel>();
        containerRegistry.RegisterForNavigation<TodaySummaryView, TodaySummaryViewModel>();

        // 注册对话框（如需要）
        containerRegistry.RegisterDialog<QuickCreateDialog, QuickCreateDialogViewModel>();
    }
}
```

---

## 4. 实现ClinicalHomeViewModel

### Step 1: 创建ViewModel基础结构

创建 `ViewModels/ClinicalHomeViewModel.cs`：

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NLog;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Foundation.ViewModels;
using LYBT.Desktop.Foundation.Services;
using LYBT.Desktop.Foundation.Managers;
using LYBT.Desktop.Foundation.Helpers;
using LYBT.Desktop.Foundation.Events;
using LYBT.Shared.Contracts.DTOs;
using LYBT.Shared.Contracts.Enums;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// Clinical主页ViewModel
/// </summary>
public class ClinicalHomeViewModel : UnifiedViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IPatientService _patientService;
    private readonly IStatisticsService _statisticsService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public ClinicalHomeViewModel(
        IMedicalCaseService medicalCaseService,
        IPatientService patientService,
        IStatisticsService statisticsService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // 初始化命令
        InitializeCommands();

        // 订阅事件
        SubscribeEvents();

        // 加载初始数据
        _ = Task.Run(async () =>
        {
            await LoadPendingListAsync();
            await LoadTodaySummaryAsync();
        });
    }

    #region Properties

    private ObservableCollection<MedicalCaseDto> _pendingMedicalCases = new();
    public ObservableCollection<MedicalCaseDto> PendingMedicalCases
    {
        get => _pendingMedicalCases;
        set => SetProperty(ref _pendingMedicalCases, value);
    }

    private MedicalCaseDto? _selectedMedicalCase;
    public MedicalCaseDto? SelectedMedicalCase
    {
        get => _selectedMedicalCase;
        set
        {
            if (SetProperty(ref _selectedMedicalCase, value))
            {
                (OpenMedicalCaseCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private int _todayMedicalCaseCount;
    public int TodayMedicalCaseCount
    {
        get => _todayMedicalCaseCount;
        set => SetProperty(ref _todayMedicalCaseCount, value);
    }

    private decimal _todayRevenue;
    public decimal TodayRevenue
    {
        get => _todayRevenue;
        set => SetProperty(ref _todayRevenue, value);
    }

    private int _completedTodayCount;
    public int CompletedTodayCount
    {
        get => _completedTodayCount;
        set => SetProperty(ref _completedTodayCount, value);
    }

    #endregion

    #region Commands

    public ICommand QuickCreateCommand { get; private set; } = null!;
    public ICommand OpenMedicalCaseCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        QuickCreateCommand = new DelegateCommand(async () => await ExecuteQuickCreateAsync());
        OpenMedicalCaseCommand = new DelegateCommand(
            async () => await ExecuteOpenMedicalCaseAsync(),
            () => SelectedMedicalCase != null
        );
        RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());
    }

    #endregion

    #region Command Handlers

    private async Task ExecuteQuickCreateAsync()
    {
        try
        {
            // 打开快速创建对话框
            var dialog = new QuickCreateDialog();
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == true)
            {
                var selectedPatient = dialog.SelectedPatient;
                var chiefComplaint = dialog.ChiefComplaint;

                // 创建病案DTO
                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = selectedPatient.Id,
                    PatientName = selectedPatient.Name,
                    DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    DoctorName = SessionManager?.CurrentUser?.RealName ?? "未知医生",
                    ChiefComplaint = chiefComplaint,
                    Status = MedicalCaseStatus.Active
                };

                // 调用API创建病案
                var createdCase = await _medicalCaseService.CreateMedicalCaseAsync(createDto);

                // 导航到病案详情页（进入诊疗流程）
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", createdCase.Id }
                };
                _regionManager.RequestNavigate("MainRegion", "MedicalCaseFlowView", parameters);

                Logger.Info($"快速创建病案成功: {createdCase.Id}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "快速创建病案失败");
            MessageBoxHelper.ShowError("创建病案失败，请稍后重试");
        }
    }

    private async Task ExecuteOpenMedicalCaseAsync()
    {
        if (SelectedMedicalCase == null)
            return;

        try
        {
            // 导航到病案详情页
            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", SelectedMedicalCase.Id }
            };
            _regionManager.RequestNavigate("MainRegion", "MedicalCaseFlowView", parameters);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "打开病案失败");
            MessageBoxHelper.ShowError("打开病案失败，请稍后重试");
        }
    }

    private async Task ExecuteRefreshAsync()
    {
        await LoadPendingListAsync();
        await LoadTodaySummaryAsync();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// AC-001实现：医生只看自己的病案
    /// </summary>
    private async Task LoadPendingListAsync()
    {
        try
        {
            SetLoading(true);

            var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
            if (currentUserId == Guid.Empty)
            {
                Logger.Warn("当前用户未登录，无法加载待诊列表");
                return;
            }

            // AC-001: 只查询当前医生的病案
            var pendingCases = await _medicalCaseService.GetPendingMedicalCasesAsync(currentUserId);

            // 按创建时间倒序排列
            var sortedCases = pendingCases.OrderByDescending(c => c.CreatedAt).ToList();

            // 更新UI绑定集合
            PendingMedicalCases = new ObservableCollection<MedicalCaseDto>(sortedCases);

            Logger.Info($"成功加载 {PendingMedicalCases.Count} 个待诊病案");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载待诊列表失败");
            MessageBoxHelper.ShowError("加载待诊列表失败，请检查网络连接");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task LoadTodaySummaryAsync()
    {
        try
        {
            var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
            if (currentUserId == Guid.Empty)
                return;

            var todaySummary = await _statisticsService.GetDoctorTodaySummaryAsync(currentUserId);

            TodayMedicalCaseCount = todaySummary.TotalMedicalCaseCount;
            TodayRevenue = todaySummary.TotalRevenue;
            CompletedTodayCount = todaySummary.CompletedCount;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载今日总结失败");
        }
    }

    #endregion

    #region Event Handlers

    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<MedicalCaseCreatedEvent>().Subscribe(OnMedicalCaseCreated);
        _eventAggregator.GetEvent<MedicalCaseCompletedEvent>().Subscribe(OnMedicalCaseCompleted);
        _eventAggregator.GetEvent<MedicalCaseCanceledEvent>().Subscribe(OnMedicalCaseCanceled);
    }

    private async void OnMedicalCaseCreated(MedicalCaseDto createdCase)
    {
        // 如果是当前医生的病案，添加到待诊列表
        if (createdCase.DoctorId == SessionManager?.CurrentUser?.Id)
        {
            PendingMedicalCases.Insert(0, createdCase);
            TodayMedicalCaseCount++;
            Logger.Info($"新增待诊病案: {createdCase.PatientName}");
        }
    }

    private async void OnMedicalCaseCompleted(Guid medicalCaseId)
    {
        // 从待诊列表中移除已完成病案
        var completedCase = PendingMedicalCases.FirstOrDefault(c => c.Id == medicalCaseId);
        if (completedCase != null)
        {
            PendingMedicalCases.Remove(completedCase);
            // 刷新今日总结（收入可能变化）
            await LoadTodaySummaryAsync();
            Logger.Info($"病案已完成: {completedCase.PatientName}");
        }
    }

    private void OnMedicalCaseCanceled(Guid medicalCaseId)
    {
        // 从待诊列表中移除已取消病案
        var canceledCase = PendingMedicalCases.FirstOrDefault(c => c.Id == medicalCaseId);
        if (canceledCase != null)
        {
            PendingMedicalCases.Remove(canceledCase);
            Logger.Info($"病案已取消: {canceledCase.PatientName}");
        }
    }

    #endregion
}
```

---

## 5. 实现ClinicalHomeView

### Step 1: 创建XAML布局

创建 `Views/ClinicalHomeView.xaml`：

```xml
<UserControl x:Class="LYBT.Desktop.Clinical.Views.ClinicalHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <StackPanel Grid.Column="0">
                <TextBlock Text="诊疗工作台" FontSize="24" FontWeight="Bold"/>
                <TextBlock Text="{Binding SessionManager.CurrentUser.RealName, StringFormat='医生: {0}'}"
                           FontSize="12" Foreground="Gray" Margin="0,5,0,0"/>
            </StackPanel>

            <StackPanel Grid.Column="1" Orientation="Horizontal">
                <Button Content="快速开单" Command="{Binding QuickCreateCommand}"
                        Width="100" Height="36" Margin="0,0,10,0"
                        Background="#4CAF50" Foreground="White" FontWeight="Bold"/>
                <Button Content="刷新" Command="{Binding RefreshCommand}"
                        Width="80" Height="36"/>
            </StackPanel>
        </Grid>

        <!-- 今日总结卡片 -->
        <Grid Grid.Row="1" Margin="0,20,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- 今日病案数 -->
            <Border Grid.Column="0" Background="#E3F2FD" CornerRadius="8" Padding="15" Margin="0,0,10,0">
                <StackPanel>
                    <TextBlock Text="今日病案" FontSize="14" Foreground="#1976D2"/>
                    <TextBlock Text="{Binding TodayMedicalCaseCount}" FontSize="32" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>

            <!-- 今日收入 -->
            <Border Grid.Column="1" Background="#E8F5E9" CornerRadius="8" Padding="15" Margin="0,0,10,0">
                <StackPanel>
                    <TextBlock Text="今日收入" FontSize="14" Foreground="#388E3C"/>
                    <TextBlock Text="{Binding TodayRevenue, StringFormat='¥{0:F2}'}" FontSize="32" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>

            <!-- 已完成 -->
            <Border Grid.Column="2" Background="#FFF3E0" CornerRadius="8" Padding="15">
                <StackPanel>
                    <TextBlock Text="已完成" FontSize="14" Foreground="#F57C00"/>
                    <TextBlock Text="{Binding CompletedTodayCount}" FontSize="32" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>
        </Grid>

        <!-- 待诊列表 -->
        <Border Grid.Row="2" Margin="0,20,0,0" BorderBrush="LightGray" BorderThickness="1" CornerRadius="8">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- 列表标题 -->
                <Border Grid.Row="0" Background="#F5F5F5" BorderBrush="LightGray" BorderThickness="0,0,0,1" Padding="15">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="待诊列表" FontSize="18" FontWeight="Bold"/>
                        <TextBlock Text="{Binding PendingMedicalCases.Count, StringFormat='（{0}人）'}"
                                   FontSize="14" Foreground="Gray" Margin="10,0,0,0" VerticalAlignment="Center"/>
                    </StackPanel>
                </Border>

                <!-- 列表内容 -->
                <ListView Grid.Row="1" ItemsSource="{Binding PendingMedicalCases}"
                          SelectedItem="{Binding SelectedMedicalCase}"
                          BorderThickness="0">
                    <ListView.View>
                        <GridView>
                            <GridViewColumn Header="患者姓名" Width="120">
                                <GridViewColumn.CellTemplate>
                                    <DataTemplate>
                                        <TextBlock Text="{Binding PatientName}" FontWeight="Bold"/>
                                    </DataTemplate>
                                </GridViewColumn.CellTemplate>
                            </GridViewColumn>

                            <GridViewColumn Header="主诉" Width="300">
                                <GridViewColumn.CellTemplate>
                                    <DataTemplate>
                                        <TextBlock Text="{Binding ChiefComplaint}" TextTrimming="CharacterEllipsis"/>
                                    </DataTemplate>
                                </GridViewColumn.CellTemplate>
                            </GridViewColumn>

                            <GridViewColumn Header="状态" Width="100">
                                <GridViewColumn.CellTemplate>
                                    <DataTemplate>
                                        <Border Background="#FFC107" CornerRadius="4" Padding="8,4">
                                            <TextBlock Text="{Binding Status}" Foreground="White" HorizontalAlignment="Center"/>
                                        </Border>
                                    </DataTemplate>
                                </GridViewColumn.CellTemplate>
                            </GridViewColumn>

                            <GridViewColumn Header="创建时间" Width="150">
                                <GridViewColumn.CellTemplate>
                                    <DataTemplate>
                                        <TextBlock Text="{Binding CreatedAt, StringFormat='{}{0:yyyy-MM-dd HH:mm}'}"/>
                                    </DataTemplate>
                                </GridViewColumn.CellTemplate>
                            </GridViewColumn>

                            <GridViewColumn Header="操作" Width="120">
                                <GridViewColumn.CellTemplate>
                                    <DataTemplate>
                                        <Button Content="开始诊疗" Command="{Binding DataContext.OpenMedicalCaseCommand, RelativeSource={RelativeSource AncestorType=ListView}}"
                                                Width="80" Height="28" Background="#4CAF50" Foreground="White"/>
                                    </DataTemplate>
                                </GridViewColumn.CellTemplate>
                            </GridViewColumn>
                        </GridView>
                    </ListView.View>

                    <!-- 空状态提示 -->
                    <ListView.Style>
                        <Style TargetType="ListView">
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding PendingMedicalCases.Count}" Value="0">
                                    <Setter Property="Template">
                                        <Setter.Value>
                                            <ControlTemplate>
                                                <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                                                    <TextBlock Text="暂无待诊病案" FontSize="16" Foreground="Gray"/>
                                                    <TextBlock Text="点击"快速开单"创建新病案" FontSize="12" Foreground="LightGray" Margin="0,10,0,0"/>
                                                </StackPanel>
                                            </ControlTemplate>
                                        </Setter.Value>
                                    </Setter>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </ListView.Style>
                </ListView>
            </Grid>
        </Border>

        <!-- Loading遮罩 -->
        <Grid Grid.RowSpan="3" Background="#80000000" Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar IsIndeterminate="True" Width="200" Height="10"/>
                <TextBlock Text="加载中..." Foreground="White" Margin="0,10,0,0" HorizontalAlignment="Center"/>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### Step 2: 创建Code-Behind

创建 `Views/ClinicalHomeView.xaml.cs`：

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views;

/// <summary>
/// ClinicalHomeView.xaml 的交互逻辑
/// </summary>
public partial class ClinicalHomeView : UserControl
{
    public ClinicalHomeView()
    {
        InitializeComponent();
    }
}
```

---

## 6. 实现QuickCreateDialog（快速创建对话框）

### Step 1: 创建对话框ViewModel

创建 `ViewModels/QuickCreateDialogViewModel.cs`：

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NLog;
using Prism.Commands;
using Prism.Services.Dialogs;
using LYBT.Desktop.Foundation.ViewModels;
using LYBT.Desktop.Foundation.Services;
using LYBT.Shared.Contracts.DTOs;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 快速创建病案对话框ViewModel
/// </summary>
public class QuickCreateDialogViewModel : ViewModelBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IPatientService _patientService;

    public QuickCreateDialogViewModel(IPatientService patientService)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

        SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanExecuteSearch);
        ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    #region IDialogAware

    public string Title => "快速开单";

    public event Action<IDialogResult>? RequestClose;

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters) { }

    #endregion

    #region Properties

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                (SearchCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private ObservableCollection<PatientDto> _searchResults = new();
    public ObservableCollection<PatientDto> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    private PatientDto? _selectedPatient;
    public PatientDto? SelectedPatient
    {
        get => _selectedPatient;
        set
        {
            if (SetProperty(ref _selectedPatient, value))
            {
                (ConfirmCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private string _chiefComplaint = string.Empty;
    public string ChiefComplaint
    {
        get => _chiefComplaint;
        set
        {
            if (SetProperty(ref _chiefComplaint, value))
            {
                (ConfirmCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(SearchKeyword);

    private async Task ExecuteSearchAsync()
    {
        try
        {
            var results = await _patientService.SearchPatientsAsync(SearchKeyword);
            SearchResults = new ObservableCollection<PatientDto>(results);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "搜索患者失败");
        }
    }

    private bool CanExecuteConfirm() => SelectedPatient != null && !string.IsNullOrWhiteSpace(ChiefComplaint);

    private void ExecuteConfirm()
    {
        var parameters = new DialogParameters
        {
            { "SelectedPatient", SelectedPatient },
            { "ChiefComplaint", ChiefComplaint }
        };
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    #endregion
}
```

### Step 2: 创建对话框View

创建 `Dialogs/QuickCreateDialog.xaml`：

```xml
<Window x:Class="LYBT.Desktop.Clinical.Dialogs.QuickCreateDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:prism="http://prismlibrary.com/"
        prism:ViewModelLocator.AutoWireViewModel="True"
        Title="{Binding Title}" Width="500" Height="600"
        WindowStartupLocation="CenterOwner">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 搜索患者 -->
        <StackPanel Grid.Row="0">
            <TextBlock Text="搜索患者" FontSize="14" FontWeight="Bold"/>
            <Grid Margin="0,10,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBox Grid.Column="0" Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                         Height="32" VerticalContentAlignment="Center"/>
                <Button Grid.Column="1" Content="搜索" Command="{Binding SearchCommand}"
                        Width="80" Height="32" Margin="10,0,0,0"/>
            </Grid>
        </StackPanel>

        <!-- 搜索结果 -->
        <Border Grid.Row="1" Margin="0,20,0,0" BorderBrush="LightGray" BorderThickness="1" CornerRadius="4">
            <ListView ItemsSource="{Binding SearchResults}" SelectedItem="{Binding SelectedPatient}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Margin="5">
                            <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                            <TextBlock Text="{Binding Gender}" FontSize="12" Foreground="Gray"/>
                            <TextBlock Text="{Binding PhoneNumber}" FontSize="12" Foreground="Gray"/>
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Border>

        <!-- 主诉输入 -->
        <StackPanel Grid.Row="2" Margin="0,20,0,0">
            <TextBlock Text="主诉" FontSize="14" FontWeight="Bold"/>
            <TextBox Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"
                     Height="80" TextWrapping="Wrap" AcceptsReturn="True" Margin="0,10,0,0"/>
        </StackPanel>

        <!-- 按钮 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="确定" Command="{Binding ConfirmCommand}"
                    Width="80" Height="32" Margin="0,0,10,0" IsDefault="True"/>
            <Button Content="取消" Command="{Binding CancelCommand}"
                    Width="80" Height="32" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
```

---

## 7. 注册模块到Shell

### Step 1: 在App.xaml.cs中加载模块

编辑 `src/Client/Desktop/Shell/LYBT.Desktop.Shell/App.xaml.cs`：

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    base.ConfigureModuleCatalog(moduleCatalog);

    // 加载Clinical模块
    moduleCatalog.AddModule<ClinicalModule>();

    // 其他模块...
}
```

### Step 2: 配置AC-002角色路由

在 `LoginViewModel.cs` 中添加Clinical路由：

```csharp
private async Task ExecuteLoginAsync()
{
    try
    {
        SetLoading(true);

        var loginResult = await _authenticationService.LoginAsync(new LoginRequest
        {
            Username = Username,
            Password = Password
        });

        if (loginResult.IsSuccess)
        {
            SessionManager.Instance.SetCurrentUser(loginResult.User);

            // AC-002: 根据角色导航
            string targetView = loginResult.User.Role switch
            {
                UserRole.Admin => "AdminHomeView",
                UserRole.Doctor => "ClinicalHomeView",  // ← 医生导航到ClinicalHomeView
                _ => throw new InvalidOperationException($"未知角色: {loginResult.User.Role}")
            };

            _regionManager.RequestNavigate("MainRegion", targetView);
            Logger.Info($"用户 {loginResult.User.RealName} 登录成功，导航到 {targetView}");
        }
        else
        {
            MessageBoxHelper.ShowError("登录失败：用户名或密码错误");
        }
    }
    catch (Exception ex)
    {
        Logger.Error(ex, "登录失败");
        MessageBoxHelper.ShowError("登录失败，请检查网络连接");
    }
    finally
    {
        SetLoading(false);
    }
}
```

---

## 8. 事件驱动集成

### Step 1: 定义事件类

在Foundation项目中创建事件定义（如已存在则跳过）：

```csharp
// Foundation/Events/MedicalCaseCreatedEvent.cs
using Prism.Events;
using LYBT.Shared.Contracts.DTOs;

namespace LYBT.Desktop.Foundation.Events;

public class MedicalCaseCreatedEvent : PubSubEvent<MedicalCaseDto> { }

// Foundation/Events/MedicalCaseCompletedEvent.cs
public class MedicalCaseCompletedEvent : PubSubEvent<Guid> { }

// Foundation/Events/MedicalCaseCanceledEvent.cs
public class MedicalCaseCanceledEvent : PubSubEvent<Guid> { }
```

### Step 2: 发布事件

在MedicalCaseFlowViewModel中发布事件：

```csharp
// 创建病案后
_eventAggregator.GetEvent<MedicalCaseCreatedEvent>().Publish(createdCase);

// 完成病案后
_eventAggregator.GetEvent<MedicalCaseCompletedEvent>().Publish(medicalCaseId);

// 取消病案后
_eventAggregator.GetEvent<MedicalCaseCanceledEvent>().Publish(medicalCaseId);
```

---

## 9. 编译与测试

### Step 1: 编译验证

```powershell
# 编译整个解决方案
cd D:\source\repos\LYBTZYZS
dotnet build LYBT.All.sln -c Release --no-restore

# 预期结果：0 errors, 0 warnings
```

### Step 2: 运行时验证

1. **启动WebAPI**：
   ```powershell
   cd src/Server/Services/LYBT.WebAPI
   dotnet run --launch-profile Production
   ```

2. **启动Desktop客户端**：
   ```powershell
   cd src/Client/Desktop/Shell/LYBT.Desktop.Shell
   dotnet run
   ```

3. **测试Clinical模块**：
   - 使用医生账号登录（如 `doctor1` / `123456`）
   - 验证自动导航到 `ClinicalHomeView`（AC-002）
   - 检查待诊列表是否只显示当前医生的病案（AC-001）
   - 测试"快速开单"功能
   - 验证今日总结数据是否正确
   - 测试"开始诊疗"按钮导航
   - 验证事件驱动更新（创建/完成病案后自动刷新列表）

### Step 3: 功能测试清单

- [ ] 医生登录后自动导航到ClinicalHomeView（AC-002）
- [ ] 待诊列表只显示当前医生的病案（AC-001）
- [ ] 今日总结数据正确显示（病案数、收入、已完成）
- [ ] 点击"快速开单"打开对话框
- [ ] 在对话框中可以搜索患者
- [ ] 输入主诉后可以确认创建病案
- [ ] 创建病案后自动导航到MedicalCaseFlowView
- [ ] 新创建的病案自动添加到待诊列表顶部（事件驱动）
- [ ] 点击"开始诊疗"可以打开病案详情
- [ ] 完成病案后自动从待诊列表移除（事件驱动）
- [ ] 完成病案后今日总结数据自动更新（事件驱动）
- [ ] Loading状态正确显示
- [ ] 网络错误时显示友好提示

---

## 10. 常见问题

### Q1: 待诊列表显示其他医生的病案

**问题**：违反AC-001业务规则。

**解决方案**：
检查 `LoadPendingListAsync` 方法是否传递了 `currentUserId`：

```csharp
var currentUserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty;
var pendingCases = await _medicalCaseService.GetPendingMedicalCasesAsync(currentUserId);
```

同时检查Server端API是否正确过滤：

```csharp
// LYBT.Module.MedicalCase/Services/MedicalCaseService.cs
public async Task<IEnumerable<MedicalCaseDto>> GetPendingMedicalCasesAsync(Guid doctorId)
{
    return await _repository.GetManyAsync(
        mc => mc.DoctorId == doctorId && mc.Status == MedicalCaseStatus.Active
    );
}
```

---

### Q2: 快速创建对话框无法搜索患者

**问题**：点击"搜索"按钮无反应。

**解决方案**：
1. 检查 `SearchCommand` 的 `CanExecute` 条件：
   ```csharp
   private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(SearchKeyword);
   ```

2. 检查API端点是否正常：
   ```powershell
   curl https://localhost:5001/api/v1/patients/search?keyword=张三 -H "Authorization: Bearer <token>"
   ```

---

### Q3: 事件驱动更新不生效

**问题**：创建病案后待诊列表没有自动更新。

**解决方案**：
1. 确认事件已订阅：
   ```csharp
   _eventAggregator.GetEvent<MedicalCaseCreatedEvent>().Subscribe(OnMedicalCaseCreated);
   ```

2. 确认事件已发布：
   ```csharp
   _eventAggregator.GetEvent<MedicalCaseCreatedEvent>().Publish(createdCase);
   ```

3. 检查事件处理方法：
   ```csharp
   private async void OnMedicalCaseCreated(MedicalCaseDto createdCase)
   {
       if (createdCase.DoctorId == SessionManager?.CurrentUser?.Id)
       {
           PendingMedicalCases.Insert(0, createdCase);
       }
   }
   ```

---

### Q4: 今日总结数据不准确

**问题**：今日收入显示为0。

**解决方案**：
检查 `GetDoctorTodaySummaryAsync` API端点：

```csharp
// Server端逻辑
public async Task<DoctorTodaySummaryDto> GetDoctorTodaySummaryAsync(Guid doctorId)
{
    var today = DateTime.Today;
    var todayCases = await _repository.GetManyAsync(
        mc => mc.DoctorId == doctorId && mc.CreatedAt.Date == today
    );

    return new DoctorTodaySummaryDto
    {
        TotalMedicalCaseCount = todayCases.Count(),
        TotalRevenue = todayCases.Where(mc => mc.Status == MedicalCaseStatus.Completed)
                                  .Sum(mc => mc.TotalFee),
        CompletedCount = todayCases.Count(mc => mc.Status == MedicalCaseStatus.Completed)
    };
}
```

---

### Q5: ObservableCollection更新UI不刷新

**问题**：列表数据已更新，但UI没有变化。

**解决方案**：
1. 确保属性使用 `SetProperty`：
   ```csharp
   public ObservableCollection<MedicalCaseDto> PendingMedicalCases
   {
       get => _pendingMedicalCases;
       set => SetProperty(ref _pendingMedicalCases, value);
   }
   ```

2. 使用 `ObservableCollection` 的修改方法（Insert/Remove）而非重新赋值：
   ```csharp
   // ✅ 正确
   PendingMedicalCases.Insert(0, newCase);

   // ❌ 错误（会丢失绑定）
   _pendingMedicalCases.Add(newCase);
   ```

---

## 11. 下一步

完成Clinical模块开发后，继续以下任务：

1. **实现MedicalCaseFlowView**：
   - 完整诊疗流程（诊断→处方→收费→完成）
   - 四诊输入（望闻问切）
   - 药材选择与处方生成
   - 收费确认

2. **补充单元测试**：
   - `ClinicalHomeViewModelTests.cs`
   - 测试AC-001权限控制
   - 测试事件驱动更新逻辑
   - 测试快速创建流程

3. **补充文档**：
   - 更新 `docs/index.md` 添加Clinical模块文档链接
   - 创建 `docs/api/medicalcase-api.md` 记录病案相关API端点

---

## 参考资料

- [Clinical模块架构设计](../../explanation/architecture/client/clinical-module-design.md)
- [Client端架构总览](../../explanation/architecture/client/README.md)
- [业务规则AC-001](../../explanation/business-rules.md#ac-001-医生只能查看自己的医案)
- [业务规则AC-002](../../explanation/business-rules.md#ac-002-角色路由规则)
- [Prism EventAggregator](https://prismlibrary.com/docs/event-aggregator.html)
- [WPF ObservableCollection](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1)
