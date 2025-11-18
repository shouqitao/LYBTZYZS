# LYBT.Desktop.Clinical - 临床医生角色模块

## 📦 项目定位

- **层级**:Client端
- **类型**:角色模块(医生工作台)
- **职责**:提供临床医生角色专属的工作台主页和诊疗功能入口。作为临床医生的核心工作界面,提供"开始看诊"核心功能,展示今日接诊统计和待处理病案数量,支持快速进入诊疗流程,优化医生日常诊疗工作效率。

## 📂 代码结构

```
LYBT.Desktop.Clinical/
├── ClinicalModule.cs                # Prism模块注册
│   └── RegisterTypes()              # 注册Views和ViewModels
├── ViewModels/
│   └── ClinicalHomeViewModel.cs     # 医生工作台ViewModel
│       ├── TodayConsultationCount   # 今日接诊数量(统计数据)
│       ├── PendingCaseCount         # 待处理病案数量(统计数据)
│       ├── StartConsultationCommand # 开始看诊命令(核心功能)
│       ├── ExecuteStartConsultation() # 执行开始看诊
│       ├── LoadTodayStatistics()    # 加载今日统计数据
│       ├── OnNavigatedTo()          # Prism导航生命周期(进入)
│       ├── OnNavigatedFrom()        # Prism导航生命周期(离开)
│       └── IsNavigationTarget()     # Prism导航目标判断
└── Views/
    ├── ClinicalHomeView.xaml        # 医生工作台视图(XAML)
    └── ClinicalHomeView.xaml.cs     # 医生工作台视图后置代码
```

**说明**:
- **ClinicalModule**:Prism模块注册,自动发现Views和ViewModels
- **ClinicalHomeViewModel**:核心功能是"开始看诊" + 实时统计数据
- **ClinicalHomeView**:医生工作台UI,包含统计卡片和"开始看诊"按钮

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Foundation** - Desktop端基础类型和接口
2. **LYBT.Desktop.Infrastructure** - 基础设施库(区域管理、导航服务)
3. **LYBT.Desktop.Models** - ViewModels基类(ViewModelBase)
4. **LYBT.Desktop.Contracts** - 契约定义(区域名称常量等)
5. **LYBT.Shared.Models** - 共享DTO模型(病案数据等)

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell层加载临床医生模块,注入主工作区

### NuGet包
- **Prism.Core** (8.x) - Prism核心库(导航、命令)
- **Prism.Wpf** (8.x) - Prism WPF扩展(区域管理、依赖注入)
- **Prism.DryIoc** (8.x) - Prism DI容器(依赖注入实现)
- **Microsoft.Extensions.Logging** (8.0.x) - 日志框架

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF (Windows Presentation Foundation)**: UI框架
- **Prism 8.x**: MVVM框架(区域导航、命令、事件聚合器)
- **DryIoc**: 依赖注入容器
- **MVVM模式**: Model-View-ViewModel架构
- **INavigationAware**: Prism导航感知接口

##  快速开始

此项目是一个类库,作为Prism模块被Shell层加载,无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Clinical/LYBT.Desktop.Clinical.csproj
```

**集成说明**:

### 1. Shell层加载模块(在App.xaml.cs中)
```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 加载临床医生角色模块
    moduleCatalog.AddModule<ClinicalModule>();
}
```

### 2. 导航到医生工作台(从Shell或其他模块)
```csharp
public class MainViewModel
{
    private readonly IRegionManager _regionManager;

    public MainViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    private void NavigateToClinicalHome()
    {
        // 导航到医生工作台(注入到ContentRegion)
        _regionManager.RequestNavigate("ContentRegion", "ClinicalHomeView");
    }
}
```

### 3. XAML布局示例(ClinicalHomeView.xaml)
```xml
<UserControl x:Class="LYBT.Desktop.Clinical.Views.ClinicalHomeView"
             xmlns:prism="http://prismlibrary.com/">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <TextBlock Grid.Row="0" Text="临床医生工作台" FontSize="24" Margin="20"/>

        <!-- 统计信息卡片 -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="20">
            <!-- 今日接诊数 -->
            <Border Style="{StaticResource StatisticCardStyle}" Margin="0,0,20,0">
                <StackPanel>
                    <TextBlock Text="今日接诊" FontSize="14" Foreground="Gray"/>
                    <TextBlock Text="{Binding TodayConsultationCount}" FontSize="36" FontWeight="Bold"/>
                </StackPanel>
            </Border>

            <!-- 待处理病案 -->
            <Border Style="{StaticResource StatisticCardStyle}">
                <StackPanel>
                    <TextBlock Text="待处理病案" FontSize="14" Foreground="Gray"/>
                    <TextBlock Text="{Binding PendingCaseCount}" FontSize="36" FontWeight="Bold" Foreground="Orange"/>
                </StackPanel>
            </Border>
        </StackPanel>

        <!-- 核心功能:开始看诊 -->
        <Button Grid.Row="2"
                Content="开始看诊"
                Command="{Binding StartConsultationCommand}"
                Style="{StaticResource PrimaryActionButtonStyle}"
                HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Width="200"
                Height="80"
                FontSize="24"/>
    </Grid>
</UserControl>
```

### 4. ViewModel实现示例(开始看诊流程)
```csharp
public class ClinicalHomeViewModel : ViewModelBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly IMedicalCaseService _medicalCaseService;

    public ClinicalHomeViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        IMedicalCaseService medicalCaseService)
        : base(eventAggregator, loggerFactory)
    {
        _regionManager = regionManager;
        _medicalCaseService = medicalCaseService;

        // 初始化开始看诊命令
        StartConsultationCommand = new DelegateCommand(
            async () => await ExecuteStartConsultation()
        );
    }

    private int _todayConsultationCount;
    public int TodayConsultationCount
    {
        get => _todayConsultationCount;
        set => SetProperty(ref _todayConsultationCount, value);
    }

    private int _pendingCaseCount;
    public int PendingCaseCount
    {
        get => _pendingCaseCount;
        set => SetProperty(ref _pendingCaseCount, value);
    }

    public DelegateCommand StartConsultationCommand { get; }

    private async Task ExecuteStartConsultation()
    {
        // ExecuteSafelyAsync自动处理异常和加载状态
        await ExecuteSafelyAsync(async () =>
        {
            // 导航到患者选择页面(开始诊疗流程)
            _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");

            // 刷新统计数据
            await LoadTodayStatistics();
        });
    }

    private async Task LoadTodayStatistics()
    {
        // 加载今日接诊统计
        var today = DateTime.Today;
        var result = await _medicalCaseService.GetPagedAsync(
            pageIndex: 1,
            pageSize: 1,
            startDate: today,
            endDate: today.AddDays(1)
        );
        TodayConsultationCount = result.TotalCount;

        // 加载待处理病案数量
        var pending = await _medicalCaseService.GetPendingCasesAsync(1, 1);
        PendingCaseCount = pending.TotalCount;
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 进入页面时刷新统计数据
        _ = LoadTodayStatistics();
    }
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/desktop-clinical/](../../../../../docs/reference/modules/desktop-clinical/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/clinical-module-design.md](../../../../../docs/explanation/architecture/client/clinical-module-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/clinical-development.md](../../../../../docs/how-to-guides/client/clinical-development.md) *(待创建)*
- **诊疗流程**:[docs/reference/quick-reference/code-patterns.md](../../../../../docs/reference/quick-reference/code-patterns.md) - 参见"诊疗流程模式"章节

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
