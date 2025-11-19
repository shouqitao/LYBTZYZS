# LYBT.Desktop.Shell - WPF应用程序外壳

## 📦 项目定位

- **层级**: Client端（Desktop桌面应用）
- **类型**: 应用程序入口（Shell）
- **职责**: 作为整个WPF客户端的统一入口点和容器编排中心，基于Prism.DryIoc 8.1.97构建的模块化架构。负责应用启动、模块自动发现与加载、依赖注入（DI）容器管理、主界面框架、导航系统、对话框服务和全局配置管理。集成8个业务模块（Auth/Users/Patients/MedicalCase/Consultation/Prescriptions/Herbs/Formula）、7个工作台（Core/Admin/Consultation等）和完整的基础服务体系，提供企业级桌面应用用户体验。

## 📂 代码结构

```
LYBT.Desktop.Shell/
├── App.xaml                                    # WPF应用程序定义（Application资源字典）
├── App.xaml.cs                                 # PrismApplication启动逻辑（模块目录、DI注册、异常处理）
├── GlobalAssemblyInfo.cs                       # 全局程序集信息（版本号、公司信息）
├── appsettings.json                            # 应用配置文件（API地址、UI设置、功能开关）
├── appsettings.example.json                    # 配置示例文件（供开发者参考）
│
├── Views/                                      # 主界面视图（6个视图）
│   ├── MainWindow.xaml                         # 主窗口界面（标题栏+菜单栏+内容区+状态栏）
│   ├── MainWindow.xaml.cs                      # 主窗口后台代码
│   ├── HomeView.xaml                           # 首页视图（欢迎页面、快速操作入口）
│   ├── HomeView.xaml.cs                        # 首页后台代码
│   ├── TestView.xaml                           # 测试视图（开发阶段调试用）
│   ├── TestView.xaml.cs                        # 测试视图后台代码
│   ├── UIShowcaseWindow.xaml                   # UI展示窗口（组件演示）
│   ├── UIShowcaseWindow.xaml.cs                # UI展示窗口后台代码
│   ├── PlaceholderViews.cs                     # 占位符视图（开发阶段模块占位）
│   └── PlaceholderViewModels.cs                # 占位符视图模型
│
├── ViewModels/                                 # 视图模型（3个核心ViewModel）
│   ├── MainWindowViewModel.cs                  # 主窗口视图模型（导航、用户会话、状态管理）
│   ├── HomeViewModel.cs                        # 首页视图模型（快速操作、统计信息）
│   └── PlaceholderViewModels.cs                # 占位符视图模型
│
├── Dialogs/                                    # 对话框系统（3个标准对话框）
│   ├── Views/                                  # 对话框视图
│   │   ├── ConfirmationDialog.xaml             # 确认对话框（是/否选择）
│   │   ├── ConfirmationDialog.xaml.cs          # 确认对话框后台代码
│   │   ├── ErrorDetailsDialog.xaml             # 错误详情对话框（异常信息展示）
│   │   ├── ErrorDetailsDialog.xaml.cs          # 错误详情对话框后台代码
│   │   ├── InformationDialog.xaml              # 信息对话框（通知提示）
│   │   └── InformationDialog.xaml.cs           # 信息对话框后台代码
│   └── ViewModels/                             # 对话框视图模型
│       ├── ConfirmationDialogViewModel.cs      # 确认对话框视图模型（IDialogAware实现）
│       ├── ErrorDetailsDialogViewModel.cs      # 错误详情对话框视图模型
│       └── InformationDialogViewModel.cs       # 信息对话框视图模型
│
├── Extensions/                                 # 扩展方法（2个扩展类）
│   ├── ServiceCollectionExtensions.cs          # 服务注册扩展（批量服务注册）
│   └── ErrorHandlingServiceExtensions.cs       # 错误处理扩展（全局异常捕获）
│
└── Styles/                                     # 样式资源（1个通用样式）
    └── CommonStyles.xaml                       # 通用样式定义（按钮、文本框、标题等）
```

**说明**:
- **App.xaml.cs**: PrismApplication核心，负责模块目录配置、DI容器注册、异常处理初始化
- **MainWindow**: 主窗口容器（标题栏显示用户信息、菜单栏导航、Region内容区、状态栏实时时间）
- **Dialogs**: Prism对话框服务实现（3个标准对话框覆盖常见交互场景）
- **Extensions**: 服务注册和错误处理的扩展方法（简化启动配置）
- **Styles**: 共享样式资源（Material Design风格，支持主题切换）

## 🔗 依赖关系

### 依赖的项目（15个核心项目）

#### 核心框架层（4个）
1. **LYBT.Desktop.Contracts** - Refit API接口定义（8个API客户端）
2. **LYBT.Desktop.Presentation** - UI基础设施（ViewModelBase/DialogService/通用组件）
3. **LYBT.Desktop.Infrastructure** - 基础设施服务（HttpClient配置/Token管理/缓存）
4. **LYBT.Desktop.Foundation** - 底层基础（Result<T>/异常定义/扩展方法）

#### 业务模块层（8个模块）
5. **LYBT.Desktop.Auth** - 认证模块（登录/登出/JWT管理）
6. **LYBT.Desktop.Users** - 用户管理模块（用户CRUD/角色管理）
7. **LYBT.Desktop.Patients** - 患者管理模块（患者档案/病历查询）
8. **LYBT.Desktop.MedicalCase** - 医案管理模块（看诊流程/病案CRUD）
9. **LYBT.Desktop.Consultation** - 诊疗模块（四诊记录/诊断开方）
10. **LYBT.Desktop.Prescriptions** - 处方模块（处方管理/药材配伍）
11. **LYBT.Desktop.Herbs** - 药材模块（药材档案/拼音检索）
12. **LYBT.Desktop.Formula** - 验方模块（验方模板/从处方创建验方）

#### 工作台层（3个工作台）
13. **LYBT.Desktop.WorkstationCore** - 核心工作台（通用工作区布局）
14. **LYBT.Desktop.Admin** - 管理员工作台（系统管理/用户管理）
15. **LYBT.Desktop.Clinical** - 诊疗工作台（中医诊疗专用界面）

#### 共享层
16. **LYBT.Shared.Models** - 共享DTO模型（跨端数据传输对象）

### 被依赖项目

**无** - Desktop.Shell作为应用程序入口点，是依赖树的顶端，不被其他项目引用。所有模块都由Shell启动时动态加载。

### NuGet包（核心依赖）

#### Prism框架（MVVM + 模块化）
- **Prism.DryIoc 8.1.97** - Prism核心框架（DryIoc容器）

#### 配置和日志
- **Microsoft.Extensions.Configuration 9.0.7** - 配置管理框架
- **Microsoft.Extensions.Configuration.Json 9.0.7** - JSON配置支持
- **Microsoft.Extensions.Logging 9.0.0** - 日志框架
- **Microsoft.Extensions.Logging.Debug 9.0.0** - 调试日志提供程序

#### 对象映射
- **AutoMapper 15.0.1** - 对象映射库（DTO ↔ ViewModel）

## 🛠 技术栈

- **.NET 8.0**: 目标框架
- **WPF (Windows Presentation Foundation)**: UI框架
- **Prism.DryIoc 8.1.97**: MVVM框架 + 模块化架构 + 依赖注入
- **AutoMapper 15.0.1**: 对象映射
- **Microsoft.Extensions.Configuration 9.0.x**: 配置管理
- **Microsoft.Extensions.Logging 9.0.x**: 日志框架

##  核心功能详解

### 1. Prism应用程序启动与模块编排

#### App.xaml.cs - PrismApplication核心实现

```csharp
/// <summary>
/// LYBT Desktop Shell应用程序
/// 基于Prism框架的模块化WPF应用
/// </summary>
public partial class App : PrismApplication
{
    /// <summary>
    /// 创建Shell主窗口
    /// </summary>
    protected override Window CreateShell()
    {
        // 从DI容器解析MainWindow
        return Container.Resolve<MainWindow>();
    }

    /// <summary>
    /// 注册全局服务和类型
    /// </summary>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 注册Prism核心服务
        containerRegistry.RegisterSingleton<IDialogService, DialogService>();
        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
        containerRegistry.RegisterSingleton<IRegionManager, RegionManager>();

        // 2. 注册应用服务（通过扩展方法）
        containerRegistry.RegisterServices();

        // 3. 注册对话框（Prism对话框服务）
        containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        containerRegistry.RegisterDialog<ErrorDetailsDialog, ErrorDetailsDialogViewModel>();
        containerRegistry.RegisterDialog<InformationDialog, InformationDialogViewModel>();

        // 4. 注册导航视图
        containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
        containerRegistry.RegisterForNavigation<TestView>();

        // 5. 注册主窗口
        containerRegistry.Register<MainWindow>();
        containerRegistry.Register<MainWindowViewModel>();
    }

    /// <summary>
    /// 配置模块目录（8个业务模块 + 3个工作台）
    /// </summary>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // ========== 业务模块（8个） ==========

        // 认证模块（优先级最高，必须最先加载）
        moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable);

        // 用户管理模块
        moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);

        // 患者管理模块
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);

        // 医案管理模块
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.WhenAvailable);

        // 诊疗模块
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);

        // 处方管理模块
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.WhenAvailable);

        // 药材管理模块
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable);

        // 验方管理模块
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable);

        // ========== 工作台模块（3个） ==========

        // 核心工作台（通用工作区布局）
        moduleCatalog.AddModule<WorkstationCoreModule>(InitializationMode.WhenAvailable);

        // 管理员工作台（系统管理专用）
        moduleCatalog.AddModule<AdminWorkstationModule>(InitializationMode.WhenAvailable);

        // 诊疗工作台（中医诊疗专用）
        moduleCatalog.AddModule<ClinicalWorkstationModule>(InitializationMode.WhenAvailable);
    }

    /// <summary>
    /// 应用启动事件
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // 1. 配置全局异常处理
            ConfigureGlobalExceptionHandling();

            // 2. 初始化配置系统
            InitializeConfiguration();

            // 3. 初始化日志系统
            InitializeLogging();

            // 4. 启动Prism应用
            base.OnStartup(e);

            // 5. 显示主窗口
            MainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"应用程序启动失败：{ex.Message}\n\n详细信息：{ex.StackTrace}",
                "启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown(-1);
        }
    }

    /// <summary>
    /// 配置全局异常处理
    /// </summary>
    private void ConfigureGlobalExceptionHandling()
    {
        // UI线程未处理异常
        DispatcherUnhandledException += (s, e) =>
        {
            LogError("UI线程异常", e.Exception);
            ShowErrorDialog(e.Exception);
            e.Handled = true;
        };

        // 非UI线程未处理异常
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            LogError("非UI线程异常", exception);
            ShowErrorDialog(exception);
        };

        // Task未处理异常
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogError("Task异常", e.Exception);
            e.SetObserved();
        };
    }

    /// <summary>
    /// 初始化配置系统
    /// </summary>
    private void InitializeConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{GetEnvironmentName()}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 注册配置到DI容器
        Container.RegisterInstance<IConfiguration>(configuration);
    }

    /// <summary>
    /// 获取环境名称
    /// </summary>
    private static string GetEnvironmentName()
    {
#if DEBUG
        return "Development";
#else
        return "Production";
#endif
    }

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    private void InitializeLogging()
    {
        var loggerFactory = Container.Resolve<ILoggerFactory>();
        loggerFactory.AddDebug();
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    private void LogError(string context, Exception exception)
    {
        var logger = Container.Resolve<ILogger<App>>();
        logger.LogError(exception, $"[{context}] {exception?.Message}");
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private void ShowErrorDialog(Exception exception)
    {
        var parameters = new DialogParameters
        {
            { "title", "应用程序错误" },
            { "message", exception?.Message ?? "未知错误" },
            { "details", exception?.StackTrace ?? "" }
        };

        Container.Resolve<IDialogService>()
            .ShowDialog("ErrorDetailsDialog", parameters, null);
    }
}
```

### 2. 主窗口框架（MainWindow）

#### MainWindow.xaml - 主窗口布局

```xml
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:prism="http://prismlibrary.com/"
        prism:ViewModelLocator.AutoWireViewModel="True"
        Title="凌隐宝堂中医诊所管理系统 v1.0"
        WindowState="Maximized"
        MinWidth="1200" MinHeight="800"
        Background="{StaticResource BackgroundBrush}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- 标题栏 -->
            <RowDefinition Height="Auto"/>   <!-- 菜单栏 -->
            <RowDefinition Height="*"/>      <!-- 主内容区 -->
            <RowDefinition Height="Auto"/>   <!-- 状态栏 -->
        </Grid.RowDefinitions>

        <!-- ========== 标题栏 ========== -->
        <Border Grid.Row="0"
                Background="{StaticResource PrimaryBrush}"
                Height="60"
                Padding="20,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- 左侧：系统标题 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <Image Source="/Assets/logo.png" Width="40" Height="40" Margin="0,0,10,0"/>
                    <TextBlock Text="凌隐宝堂中医诊所管理系统"
                               Style="{StaticResource TitleTextStyle}"
                               VerticalAlignment="Center"/>
                </StackPanel>

                <!-- 右侧：用户信息 + 退出按钮 -->
                <StackPanel Grid.Column="2" Orientation="Horizontal" VerticalAlignment="Center">
                    <!-- 用户头像 -->
                    <Ellipse Width="36" Height="36" Margin="0,0,10,0">
                        <Ellipse.Fill>
                            <ImageBrush ImageSource="{Binding CurrentUser.Avatar, FallbackValue='/Assets/default-avatar.png'}"/>
                        </Ellipse.Fill>
                    </Ellipse>

                    <!-- 用户名 + 角色 -->
                    <StackPanel VerticalAlignment="Center" Margin="0,0,15,0">
                        <TextBlock Text="{Binding CurrentUser.RealName, FallbackValue='未登录'}"
                                   Style="{StaticResource UserNameTextStyle}"/>
                        <TextBlock Text="{Binding CurrentUser.RoleDisplay, FallbackValue=''}"
                                   Style="{StaticResource UserRoleTextStyle}"/>
                    </StackPanel>

                    <!-- 退出按钮 -->
                    <Button Command="{Binding LogoutCommand}"
                            Content="退出登录"
                            Style="{StaticResource LogoutButtonStyle}"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- ========== 菜单栏 ========== -->
        <Menu Grid.Row="1"
              Background="{StaticResource MenuBackgroundBrush}"
              Height="40">
            <!-- 患者管理 -->
            <MenuItem Header="患者管理"
                      Command="{Binding NavigateToCommand}"
                      CommandParameter="PatientsManagement"
                      Icon="{StaticResource PatientsIcon}"/>

            <!-- 诊疗诊断 -->
            <MenuItem Header="诊疗诊断"
                      Command="{Binding NavigateToCommand}"
                      CommandParameter="ConsultationMain"
                      Icon="{StaticResource ConsultationIcon}"/>

            <!-- 处方管理 -->
            <MenuItem Header="处方管理"
                      Command="{Binding NavigateToCommand}"
                      CommandParameter="PrescriptionsManagement"
                      Icon="{StaticResource PrescriptionsIcon}"/>

            <!-- 验方管理 -->
            <MenuItem Header="验方管理"
                      Command="{Binding NavigateToCommand}"
                      CommandParameter="FormulaManagement"
                      Icon="{StaticResource FormulaIcon}"/>

            <!-- 中药材管理 -->
            <MenuItem Header="中药材"
                      Command="{Binding NavigateToCommand}"
                      CommandParameter="HerbsManagement"
                      Icon="{StaticResource HerbsIcon}"/>

            <!-- 系统管理（仅管理员可见） -->
            <MenuItem Header="系统管理"
                      Command="{Binding NavigateToCommand}"
                      CommandParameter="SystemManagement"
                      Icon="{StaticResource SystemIcon}"
                      Visibility="{Binding IsAdmin, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </Menu>

        <!-- ========== 主内容区域（Prism Region） ========== -->
        <ContentControl Grid.Row="2"
                        prism:RegionManager.RegionName="ContentRegion"
                        Margin="10"/>

        <!-- ========== 状态栏 ========== -->
        <StatusBar Grid.Row="3" Height="30">
            <StatusBarItem>
                <TextBlock Text="{Binding StatusMessage, FallbackValue='就绪'}"
                           Margin="5,0"/>
            </StatusBarItem>

            <!-- 分隔符 -->
            <Separator Style="{StaticResource StatusBarSeparatorStyle}"/>

            <!-- 当前模块 -->
            <StatusBarItem>
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="当前模块：" Margin="5,0,2,0"/>
                    <TextBlock Text="{Binding CurrentModuleName, FallbackValue='首页'}"
                               FontWeight="Bold"/>
                </StackPanel>
            </StatusBarItem>

            <!-- 分隔符 -->
            <Separator Style="{StaticResource StatusBarSeparatorStyle}"/>

            <!-- 当前时间（右对齐） -->
            <StatusBarItem HorizontalAlignment="Right">
                <TextBlock Text="{Binding CurrentDateTime, StringFormat=yyyy-MM-dd HH:mm:ss}"
                           Margin="0,0,10,0"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

#### MainWindowViewModel.cs - 主窗口视图模型

```csharp
/// <summary>
/// 主窗口视图模型
/// 负责导航、用户会话、状态管理
/// </summary>
public class MainWindowViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private readonly IUserSessionManager _sessionManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly DispatcherTimer _timer;

    // ========== 属性 ==========

    /// <summary>
    /// 当前登录用户
    /// </summary>
    public UserDto CurrentUser => _sessionManager.CurrentUser;

    /// <summary>
    /// 是否是管理员
    /// </summary>
    public bool IsAdmin => _sessionManager.IsAdmin;

    private string _statusMessage = "就绪";
    /// <summary>
    /// 状态栏消息
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _currentModuleName = "首页";
    /// <summary>
    /// 当前模块名称
    /// </summary>
    public string CurrentModuleName
    {
        get => _currentModuleName;
        set => SetProperty(ref _currentModuleName, value);
    }

    private DateTime _currentDateTime = DateTime.Now;
    /// <summary>
    /// 当前时间
    /// </summary>
    public DateTime CurrentDateTime
    {
        get => _currentDateTime;
        set => SetProperty(ref _currentDateTime, value);
    }

    // ========== 命令 ==========

    public DelegateCommand<string> NavigateToCommand { get; }
    public DelegateCommand LogoutCommand { get; }

    // ========== 构造函数 ==========

    public MainWindowViewModel(
        IRegionManager regionManager,
        IUserSessionManager sessionManager,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _regionManager = regionManager;
        _sessionManager = sessionManager;
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        _logger = logger;

        // 初始化命令
        NavigateToCommand = new DelegateCommand<string>(ExecuteNavigateTo);
        LogoutCommand = new DelegateCommand(ExecuteLogout);

        // 启动时间更新定时器（每秒更新）
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now;
        _timer.Start();

        // 订阅事件
        SubscribeToEvents();

        // 默认导航到首页
        NavigateToHome();
    }

    // ========== 导航方法 ==========

    /// <summary>
    /// 导航到首页
    /// </summary>
    private void NavigateToHome()
    {
        try
        {
            _regionManager.RequestNavigate("ContentRegion", "HomeView");
            CurrentModuleName = "首页";
            StatusMessage = "欢迎使用凌隐宝堂中医诊所管理系统";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到首页失败");
            StatusMessage = "导航失败";
        }
    }

    /// <summary>
    /// 执行导航命令
    /// </summary>
    private void ExecuteNavigateTo(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            _logger.LogWarning("导航目标为空");
            return;
        }

        try
        {
            _regionManager.RequestNavigate("ContentRegion", moduleName, result =>
            {
                if (result.Result == true)
                {
                    CurrentModuleName = GetModuleDisplayName(moduleName);
                    StatusMessage = $"已切换到{CurrentModuleName}";
                    _logger.LogInformation($"导航成功：{moduleName}");
                }
                else
                {
                    var errorMessage = result.Error?.Message ?? "未知错误";
                    StatusMessage = $"导航失败：{errorMessage}";
                    _logger.LogError($"导航失败：{moduleName} - {errorMessage}");

                    _dialogService.ShowErrorDialog("导航错误", errorMessage);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"导航异常：{moduleName}");
            StatusMessage = $"导航异常：{ex.Message}";
            _dialogService.ShowErrorDialog("导航异常", ex.Message);
        }
    }

    /// <summary>
    /// 执行登出命令
    /// </summary>
    private async void ExecuteLogout()
    {
        try
        {
            // 显示确认对话框
            var parameters = new DialogParameters
            {
                { "message", "确定要退出系统吗？" },
                { "title", "退出确认" }
            };

            _dialogService.ShowDialog("ConfirmationDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 用户确认退出
                    _logger.LogInformation($"用户{CurrentUser?.RealName}请求登出");

                    // 执行登出逻辑
                    await _sessionManager.LogoutAsync();

                    // 发布登出事件（通知其他模块）
                    _eventAggregator.GetEvent<UserLoggedOutEvent>().Publish();

                    // 关闭主窗口
                    Application.Current.MainWindow?.Close();

                    _logger.LogInformation("用户已登出，应用程序关闭");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            StatusMessage = "登出失败";
            await _dialogService.ShowErrorDialogAsync("登出失败", ex.Message);
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 订阅用户登录成功事件
        _eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

        // 订阅导航请求事件
        _eventAggregator.GetEvent<NavigationRequestEvent>().Subscribe(OnNavigationRequest);

        // 订阅状态消息事件
        _eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusMessage);
    }

    /// <summary>
    /// 用户登录成功事件处理
    /// </summary>
    private void OnUserLoggedIn(UserDto user)
    {
        _logger.LogInformation($"用户{user.RealName}登录成功");
        RaisePropertyChanged(nameof(CurrentUser));
        RaisePropertyChanged(nameof(IsAdmin));
        StatusMessage = $"欢迎，{user.RealName}";
    }

    /// <summary>
    /// 导航请求事件处理
    /// </summary>
    private void OnNavigationRequest(string moduleName)
    {
        ExecuteNavigateTo(moduleName);
    }

    /// <summary>
    /// 状态消息事件处理
    /// </summary>
    private void OnStatusMessage(string message)
    {
        StatusMessage = message;
    }

    /// <summary>
    /// 获取模块显示名称
    /// </summary>
    private string GetModuleDisplayName(string moduleName)
    {
        return moduleName switch
        {
            "PatientsManagement" => "患者管理",
            "ConsultationMain" => "诊疗诊断",
            "PrescriptionsManagement" => "处方管理",
            "FormulaManagement" => "验方管理",
            "HerbsManagement" => "中药材管理",
            "SystemManagement" => "系统管理",
            "HomeView" => "首页",
            _ => moduleName
        };
    }
}
```

### 3. Prism对话框系统

#### ConfirmationDialogViewModel.cs - 确认对话框

```csharp
/// <summary>
/// 确认对话框视图模型
/// 用于显示需要用户确认的操作（是/否选择）
/// </summary>
public class ConfirmationDialogViewModel : BindableBase, IDialogAware
{
    // ========== IDialogAware实现 ==========

    public string Title { get; set; } = "确认";
    public event Action<IDialogResult> RequestClose;

    // ========== 属性 ==========

    private string _message;
    /// <summary>
    /// 对话框消息
    /// </summary>
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private string _icon = "Question";
    /// <summary>
    /// 图标类型（Question/Warning/Information）
    /// </summary>
    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    // ========== 命令 ==========

    public DelegateCommand OkCommand { get; }
    public DelegateCommand CancelCommand { get; }

    // ========== 构造函数 ==========

    public ConfirmationDialogViewModel()
    {
        OkCommand = new DelegateCommand(ExecuteOk);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    // ========== 命令实现 ==========

    /// <summary>
    /// 确认按钮
    /// </summary>
    private void ExecuteOk()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
    }

    /// <summary>
    /// 取消按钮
    /// </summary>
    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    // ========== IDialogAware接口实现 ==========

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 从参数获取消息和标题
        Message = parameters.GetValue<string>("message");

        if (parameters.ContainsKey("title"))
        {
            Title = parameters.GetValue<string>("title");
        }

        if (parameters.ContainsKey("icon"))
        {
            Icon = parameters.GetValue<string>("icon");
        }
    }
}
```

#### ErrorDetailsDialogViewModel.cs - 错误详情对话框

```csharp
/// <summary>
/// 错误详情对话框视图模型
/// 用于显示异常信息和堆栈跟踪
/// </summary>
public class ErrorDetailsDialogViewModel : BindableBase, IDialogAware
{
    public string Title { get; set; } = "错误详情";
    public event Action<IDialogResult> RequestClose;

    private string _message;
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private string _details;
    /// <summary>
    /// 错误详情（堆栈跟踪）
    /// </summary>
    public string Details
    {
        get => _details;
        set => SetProperty(ref _details, value);
    }

    private bool _isDetailsVisible;
    /// <summary>
    /// 是否显示详情
    /// </summary>
    public bool IsDetailsVisible
    {
        get => _isDetailsVisible;
        set => SetProperty(ref _isDetailsVisible, value);
    }

    public DelegateCommand CloseCommand { get; }
    public DelegateCommand ToggleDetailsCommand { get; }
    public DelegateCommand CopyToClipboardCommand { get; }

    public ErrorDetailsDialogViewModel()
    {
        CloseCommand = new DelegateCommand(ExecuteClose);
        ToggleDetailsCommand = new DelegateCommand(ExecuteToggleDetails);
        CopyToClipboardCommand = new DelegateCommand(ExecuteCopyToClipboard);
    }

    private void ExecuteClose()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
    }

    private void ExecuteToggleDetails()
    {
        IsDetailsVisible = !IsDetailsVisible;
    }

    private void ExecuteCopyToClipboard()
    {
        var fullMessage = $"{Message}\n\n详细信息：\n{Details}";
        Clipboard.SetText(fullMessage);
    }

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Message = parameters.GetValue<string>("message");
        Details = parameters.GetValue<string>("details") ?? "";

        if (parameters.ContainsKey("title"))
        {
            Title = parameters.GetValue<string>("title");
        }
    }
}
```

### 4. 配置管理

#### appsettings.json - 应用配置文件

```json
{
  // ========== API配置 ==========
  "ApiBaseUrl": "http://localhost:5001",
  "ConnectionTimeout": 30,
  "RetryCount": 3,
  "RetryDelay": 1000,

  // ========== 日志配置 ==========
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "System": "Warning"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
    }
  },

  // ========== UI配置 ==========
  "UI": {
    "Theme": "Light",
    "Language": "zh-CN",
    "WindowState": "Maximized",
    "EnableAnimations": true,
    "FontSize": 14
  },

  // ========== 功能开关 ==========
  "Features": {
    "EnableDeveloperMode": false,
    "EnableUIShowcase": false,
    "EnableAdvancedLogging": false,
    "EnablePerformanceMonitoring": false
  },

  // ========== 缓存配置 ==========
  "Cache": {
    "EnableMemoryCache": true,
    "DefaultExpirationMinutes": 30,
    "SlidingExpirationMinutes": 10
  },

  // ========== 业务配置 ==========
  "Business": {
    "MaxPatientsPerPage": 50,
    "MaxMedicalCasesPerPage": 20,
    "DefaultPrescriptionDays": 7
  }
}
```

## 🎨 综合使用示例

### 示例1：应用启动与模块加载

```csharp
// ========== Program.cs / Main方法 ==========

/// <summary>
/// 应用程序入口点
/// </summary>
[STAThread]
public static void Main(string[] args)
{
    try
    {
        // 1. 创建WPF应用实例（继承自PrismApplication）
        var app = new App();

        // 2. 初始化Prism容器和模块
        app.InitializeComponent();

        // 3. 运行应用（显示MainWindow）
        app.Run();
    }
    catch (Exception ex)
    {
        // 启动失败处理
        MessageBox.Show(
            $"应用程序启动失败：\n{ex.Message}\n\n请联系系统管理员。",
            "启动错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
}

// ========== App.xaml.cs 启动流程 ==========

protected override void OnStartup(StartupEventArgs e)
{
    // 步骤1：配置全局异常处理
    ConfigureGlobalExceptionHandling();

    // 步骤2：加载配置文件（appsettings.json）
    var configuration = LoadConfiguration();

    // 步骤3：注册配置到DI容器
    Container.RegisterInstance<IConfiguration>(configuration);

    // 步骤4：初始化日志系统
    InitializeLogging();

    // 步骤5：启动Prism应用（加载模块、创建Shell）
    base.OnStartup(e);

    // 步骤6：显示主窗口
    MainWindow.Show();

    // 步骤7：记录启动日志
    var logger = Container.Resolve<ILogger<App>>();
    logger.LogInformation("应用程序启动成功");
}

// ========== 模块自动加载流程 ==========

protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // Prism会按照以下顺序加载模块：

    // 1. AuthModule（认证模块，优先级最高）
    moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable);

    // 2. 其他业务模块（并行加载，无依赖关系）
    moduleCatalog.AddModule<UsersModule>();
    moduleCatalog.AddModule<PatientsModule>();
    moduleCatalog.AddModule<MedicalCaseModule>();
    // ... 其他模块

    // 3. 工作台模块（依赖业务模块，最后加载）
    moduleCatalog.AddModule<WorkstationCoreModule>();
    moduleCatalog.AddModule<AdminWorkstationModule>();
    moduleCatalog.AddModule<ClinicalWorkstationModule>();
}
```

### 示例2：主窗口导航流程

```csharp
// ========== MainWindowViewModel导航逻辑 ==========

/// <summary>
/// 用户点击菜单项，导航到患者管理模块
/// </summary>
private void ExecuteNavigateTo(string moduleName)
{
    // moduleName = "PatientsManagement"

    try
    {
        // 1. 使用Prism RegionManager导航
        _regionManager.RequestNavigate(
            regionName: "ContentRegion",      // 目标Region（MainWindow.xaml中定义）
            source: moduleName,                // 模块名称（路由目标）
            navigationCallback: result =>      // 导航回调
            {
                if (result.Result == true)
                {
                    // 导航成功
                    CurrentModuleName = "患者管理";
                    StatusMessage = "已切换到患者管理";
                    _logger.LogInformation($"导航成功：{moduleName}");
                }
                else
                {
                    // 导航失败
                    var errorMessage = result.Error?.Message ?? "未知错误";
                    StatusMessage = $"导航失败：{errorMessage}";
                    _logger.LogError($"导航失败：{errorMessage}");

                    // 显示错误对话框
                    _dialogService.ShowErrorDialog("导航错误", errorMessage);
                }
            }
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航异常");
        StatusMessage = "导航异常";
        _dialogService.ShowErrorDialog("导航异常", ex.Message);
    }
}

// ========== Prism导航流程（自动执行） ==========

// 1. Prism查找PatientsModule注册的视图
// 2. 解析视图和ViewModel（从DI容器）
// 3. 调用INavigationAware接口方法（如OnNavigatedTo）
// 4. 在ContentRegion中显示视图
// 5. 触发导航回调
```

### 示例3：对话框服务使用

```csharp
// ========== 控制器/ViewModel中使用对话框服务 ==========

public class PatientsManagementViewModel : BindableBase
{
    private readonly IDialogService _dialogService;
    private readonly IPatientService _patientService;

    public DelegateCommand<Guid> DeletePatientCommand { get; }

    public PatientsManagementViewModel(
        IDialogService dialogService,
        IPatientService patientService)
    {
        _dialogService = dialogService;
        _patientService = patientService;

        DeletePatientCommand = new DelegateCommand<Guid>(ExecuteDeletePatient);
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    private async void ExecuteDeletePatient(Guid patientId)
    {
        // 步骤1：显示确认对话框
        var parameters = new DialogParameters
        {
            { "message", "确定要删除这个患者档案吗？删除后无法恢复。" },
            { "title", "删除确认" },
            { "icon", "Warning" }
        };

        _dialogService.ShowDialog(
            name: "ConfirmationDialog",
            parameters: parameters,
            callback: async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 用户确认删除
                    try
                    {
                        // 步骤2：执行删除操作
                        await _patientService.DeleteAsync(patientId);

                        // 步骤3：显示成功信息
                        var successParams = new DialogParameters
                        {
                            { "message", "患者档案已成功删除。" },
                            { "title", "操作成功" }
                        };
                        _dialogService.ShowDialog("InformationDialog", successParams, null);

                        // 步骤4：刷新患者列表
                        await RefreshPatientListAsync();
                    }
                    catch (Exception ex)
                    {
                        // 步骤5：显示错误详情对话框
                        var errorParams = new DialogParameters
                        {
                            { "message", $"删除患者失败：{ex.Message}" },
                            { "details", ex.StackTrace },
                            { "title", "删除失败" }
                        };
                        _dialogService.ShowDialog("ErrorDetailsDialog", errorParams, null);
                    }
                }
                else
                {
                    // 用户取消删除
                    _logger.LogInformation("用户取消删除患者操作");
                }
            }
        );
    }
}
```

### 示例4：EventAggregator跨模块通信

```csharp
// ========== 定义事件 ==========

/// <summary>
/// 用户登录成功事件
/// </summary>
public class UserLoggedInEvent : PubSubEvent<UserDto> { }

/// <summary>
/// 导航请求事件
/// </summary>
public class NavigationRequestEvent : PubSubEvent<string> { }

/// <summary>
/// 患者选择事件
/// </summary>
public class PatientSelectedEvent : PubSubEvent<Guid> { }

// ========== 发布事件（Auth模块） ==========

public class AuthViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;

    public async Task ExecuteLoginAsync()
    {
        try
        {
            // 登录成功
            var user = await _authService.LoginAsync(Username, Password);

            // 发布登录成功事件（通知其他模块）
            _eventAggregator.GetEvent<UserLoggedInEvent>().Publish(user);

            // 导航到首页
            _eventAggregator.GetEvent<NavigationRequestEvent>().Publish("HomeView");
        }
        catch (Exception ex)
        {
            // 登录失败处理
        }
    }
}

// ========== 订阅事件（MainWindow） ==========

public class MainWindowViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;

    public MainWindowViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;

        // 订阅用户登录成功事件
        _eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

        // 订阅导航请求事件
        _eventAggregator.GetEvent<NavigationRequestEvent>().Subscribe(OnNavigationRequest);
    }

    /// <summary>
    /// 用户登录成功事件处理
    /// </summary>
    private void OnUserLoggedIn(UserDto user)
    {
        // 更新UI（显示用户名、角色）
        RaisePropertyChanged(nameof(CurrentUser));
        RaisePropertyChanged(nameof(IsAdmin));
        StatusMessage = $"欢迎，{user.RealName}";

        _logger.LogInformation($"用户{user.RealName}登录成功");
    }

    /// <summary>
    /// 导航请求事件处理
    /// </summary>
    private void OnNavigationRequest(string moduleName)
    {
        ExecuteNavigateTo(moduleName);
    }
}

// ========== 订阅事件（Consultation模块） ==========

public class ConsultationViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;

    public ConsultationViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;

        // 订阅患者选择事件（从患者管理模块发布）
        _eventAggregator.GetEvent<PatientSelectedEvent>().Subscribe(OnPatientSelected);
    }

    /// <summary>
    /// 患者选择事件处理
    /// </summary>
    private async void OnPatientSelected(Guid patientId)
    {
        // 加载患者信息
        var patient = await _patientService.GetByIdAsync(patientId);

        // 更新UI（显示患者信息）
        SelectedPatient = patient;
        StatusMessage = $"已选择患者：{patient.Name}";
    }
}
```

### 示例5：配置管理与环境检测

```csharp
// ========== 配置加载（App.xaml.cs） ==========

/// <summary>
/// 初始化配置系统
/// </summary>
private void InitializeConfiguration()
{
    // 1. 创建配置构建器
    var builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{GetEnvironmentName()}.json", optional: true)
        .AddEnvironmentVariables();

    // 2. 构建配置
    var configuration = builder.Build();

    // 3. 注册到DI容器
    Container.RegisterInstance<IConfiguration>(configuration);

    // 4. 记录配置加载日志
    var logger = Container.Resolve<ILogger<App>>();
    logger.LogInformation($"配置加载成功，环境：{GetEnvironmentName()}");
}

/// <summary>
/// 获取环境名称
/// </summary>
private static string GetEnvironmentName()
{
#if DEBUG
    return "Development";
#else
    return "Production";
#endif
}

// ========== 使用配置（任意ViewModel/Service） ==========

public class ApiClientService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiClientService> _logger;

    public ApiClientService(
        IConfiguration configuration,
        ILogger<ApiClientService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 获取API基础地址
    /// </summary>
    public string GetApiBaseUrl()
    {
        var url = _configuration["ApiBaseUrl"];
        _logger.LogInformation($"API基础地址：{url}");
        return url;
    }

    /// <summary>
    /// 获取超时时间
    /// </summary>
    public int GetConnectionTimeout()
    {
        var timeout = _configuration.GetValue<int>("ConnectionTimeout", 30);
        return timeout;
    }

    /// <summary>
    /// 检查功能开关
    /// </summary>
    public bool IsDeveloperModeEnabled()
    {
        var enabled = _configuration.GetValue<bool>("Features:EnableDeveloperMode", false);
        return enabled;
    }

    /// <summary>
    /// 获取UI配置
    /// </summary>
    public UISetting GetUISettings()
    {
        var settings = new UISetting();
        _configuration.GetSection("UI").Bind(settings);
        return settings;
    }
}

// ========== appsettings.Development.json（开发环境） ==========
{
  "ApiBaseUrl": "http://localhost:5001",
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "Features": {
    "EnableDeveloperMode": true,
    "EnableUIShowcase": true
  }
}

// ========== appsettings.Production.json（生产环境） ==========
{
  "ApiBaseUrl": "https://api.lybtzyzs.com",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Features": {
    "EnableDeveloperMode": false,
    "EnableUIShowcase": false
  }
}
```

##  最佳实践

### 1. Prism模块化架构原则

**模块独立性**：
-  每个模块都是独立的程序集（DLL）
-  模块间通过EventAggregator通信，避免直接依赖
-  模块职责单一，只关注自己的业务领域
-  禁止模块间循环引用

**模块注册规范**：
```csharp
//  正确：在App.xaml.cs中集中注册
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 按业务逻辑顺序注册
    moduleCatalog.AddModule<AuthModule>();
    moduleCatalog.AddModule<UsersModule>();
    // ...
}

//  错误：模块自动发现（不可控）
protected override IModuleCatalog CreateModuleCatalog()
{
    return new DirectoryModuleCatalog { ModulePath = @".\Modules" };
}
```

### 2. 依赖注入（DI）最佳实践

**服务注册规范**：
```csharp
//  正确：使用合适的生命周期
containerRegistry.RegisterSingleton<IDialogService, DialogService>();      // 单例
containerRegistry.Register<IPatientService, PatientService>();            // 瞬态
containerRegistry.RegisterScoped<IUserSessionManager, UserSessionManager>(); // 作用域

//  错误：所有服务都注册为单例
containerRegistry.RegisterSingleton<IPatientService, PatientService>(); // 可能导致状态污染
```

**依赖注入原则**：
-  构造函数注入（推荐）
-  属性注入（仅用于可选依赖）
-  禁止使用 `Container.Resolve<T>()` 直接解析（服务定位器反模式）
-  禁止在构造函数中执行复杂逻辑

### 3. 导航系统最佳实践

**Region导航规范**：
```csharp
//  正确：使用命名Region
_regionManager.RequestNavigate("ContentRegion", "PatientsManagement");

//  正确：带参数导航
var parameters = new NavigationParameters
{
    { "PatientId", selectedPatientId }
};
_regionManager.RequestNavigate("ContentRegion", "PatientDetail", parameters);

//  错误：硬编码Region名称
_regionManager.RequestNavigate("Region1", "View1"); // 不清晰
```

**导航回调处理**：
```csharp
//  正确：处理导航结果
_regionManager.RequestNavigate("ContentRegion", "PatientsManagement", result =>
{
    if (!result.Result)
    {
        _logger.LogError($"导航失败：{result.Error?.Message}");
        _dialogService.ShowErrorDialog("导航失败", result.Error?.Message);
    }
});

//  错误：忽略导航结果
_regionManager.RequestNavigate("ContentRegion", "PatientsManagement"); // 可能静默失败
```

### 4. 对话框服务最佳实践

**对话框注册规范**：
```csharp
//  正确：在App.xaml.cs中注册
containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
containerRegistry.RegisterDialog<ErrorDetailsDialog, ErrorDetailsDialogViewModel>();
containerRegistry.RegisterDialog<InformationDialog, InformationDialogViewModel>();
```

**对话框调用规范**：
```csharp
//  正确：使用参数传递数据
var parameters = new DialogParameters
{
    { "message", "确定要删除吗？" },
    { "title", "删除确认" }
};
_dialogService.ShowDialog("ConfirmationDialog", parameters, result =>
{
    if (result.Result == ButtonResult.OK)
    {
        // 用户确认
    }
});

//  错误：在ViewModel中创建对话框实例
var dialog = new ConfirmationDialog(); // 破坏模块化
dialog.ShowDialog();
```

### 5. 事件聚合器（EventAggregator）最佳实践

**事件定义规范**：
```csharp
//  正确：强类型事件
public class UserLoggedInEvent : PubSubEvent<UserDto> { }
public class PatientSelectedEvent : PubSubEvent<Guid> { }

//  错误：弱类型事件
public class GenericEvent : PubSubEvent<object> { } // 类型不安全
```

**事件发布与订阅规范**：
```csharp
//  正确：订阅事件（在构造函数中）
_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

//  正确：取消订阅（在Dispose中）
_eventAggregator.GetEvent<UserLoggedInEvent>().Unsubscribe(OnUserLoggedIn);

//  错误：忘记取消订阅（可能导致内存泄漏）
```

### 6. 配置管理最佳实践

**配置文件组织**：
```
appsettings.json              # 默认配置
appsettings.Development.json  # 开发环境配置（覆盖默认）
appsettings.Production.json   # 生产环境配置（覆盖默认）
```

**配置访问规范**：
```csharp
//  正确：使用IConfiguration接口
var apiUrl = _configuration["ApiBaseUrl"];
var timeout = _configuration.GetValue<int>("ConnectionTimeout", 30);

//  正确：绑定到强类型对象
var uiSettings = new UISetting();
_configuration.GetSection("UI").Bind(uiSettings);

//  错误：硬编码配置
const string ApiUrl = "http://localhost:5001"; // 不灵活
```

### 7. 异常处理最佳实践

**全局异常处理**：
```csharp
//  正确：在App.xaml.cs中配置
private void ConfigureGlobalExceptionHandling()
{
    DispatcherUnhandledException += OnDispatcherUnhandledException;
    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
}
```

**局部异常处理**：
```csharp
//  正确：在ViewModel中捕获异常
private async void ExecuteDeletePatient(Guid patientId)
{
    try
    {
        await _patientService.DeleteAsync(patientId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "删除患者失败");
        await _dialogService.ShowErrorDialogAsync("删除失败", ex.Message);
    }
}

//  错误：吞掉异常
catch (Exception) { } // 静默失败
```

## 📈 性能优化

### 1. 模块延迟加载

```csharp
// 按需加载模块（减少启动时间）
moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);
moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);

// 用户首次访问时加载
_moduleManager.LoadModule("HerbsModule");
```

### 2. UI虚拟化

```xml
<!-- 大数据列表使用虚拟化 -->
<DataGrid ItemsSource="{Binding Patients}"
          VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"/>
```

### 3. 异步操作

```csharp
//  正确：异步加载数据
public async Task LoadPatientsAsync()
{
    IsBusy = true;
    try
    {
        var patients = await _patientService.GetPagedAsync(1, 50);
        Patients.Clear();
        Patients.AddRange(patients.Items);
    }
    finally
    {
        IsBusy = false;
    }
}

//  错误：同步阻塞UI
var patients = _patientService.GetPaged(1, 50); // 阻塞UI线程
```

## 🔒 安全考虑

### 1. 敏感信息保护

```json
//  错误：appsettings.json中明文存储
{
  "Database": "Server=localhost;Password=123456"
}

//  正确：使用环境变量
{
  "Database": "${DB_CONNECTION_STRING}"
}
```

### 2. 异常信息过滤

```csharp
// 生产环境：过滤敏感异常信息
private void ShowErrorDialog(Exception ex)
{
    var message = IsProduction ? "操作失败，请联系系统管理员" : ex.Message;
    _dialogService.ShowErrorDialog("错误", message);
}
```

## 🧪 测试指南

### 单元测试示例（MainWindowViewModel）

```csharp
public class MainWindowViewModelTests
{
    private Mock<IRegionManager> _regionManagerMock;
    private Mock<IUserSessionManager> _sessionManagerMock;
    private Mock<IEventAggregator> _eventAggregatorMock;
    private MainWindowViewModel _viewModel;

    [SetUp]
    public void SetUp()
    {
        _regionManagerMock = new Mock<IRegionManager>();
        _sessionManagerMock = new Mock<IUserSessionManager>();
        _eventAggregatorMock = new Mock<IEventAggregator>();

        _viewModel = new MainWindowViewModel(
            _regionManagerMock.Object,
            _sessionManagerMock.Object,
            _eventAggregatorMock.Object
        );
    }

    [Test]
    public void NavigateToCommand_ShouldNavigateToModule()
    {
        // Arrange
        var moduleName = "PatientsManagement";
        bool navigationRequested = false;
        _regionManagerMock
            .Setup(rm => rm.RequestNavigate(
                "ContentRegion",
                moduleName,
                It.IsAny<Action<NavigationResult>>()))
            .Callback(() => navigationRequested = true);

        // Act
        _viewModel.NavigateToCommand.Execute(moduleName);

        // Assert
        Assert.IsTrue(navigationRequested);
    }

    [Test]
    public void LogoutCommand_ShouldCallSessionManagerLogout()
    {
        // Arrange
        _sessionManagerMock.Setup(sm => sm.LogoutAsync()).ReturnsAsync(true);

        // Act
        _viewModel.LogoutCommand.Execute();

        // Assert
        _sessionManagerMock.Verify(sm => sm.LogoutAsync(), Times.Once);
    }
}
```

##  快速开始

### 环境准备
-  安装 .NET 8.0 SDK
-  安装 Visual Studio 2022（推荐）或 Rider

### 还原依赖
```bash
# 还原所有项目依赖
dotnet restore LYBT.All.sln
```

### 构建项目
```bash
# 构建整个解决方案
dotnet build LYBT.All.sln -c Release --no-restore

# 或仅构建Desktop.Shell
dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```

### 运行与调试

**Visual Studio**：
1. 在解决方案资源管理器中，右键 `LYBT.Desktop.Shell` 项目
2. 选择"设为启动项目"
3. 按 F5 启动调试

**命令行**：
```bash
# 开发环境运行
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj

# 生产环境运行
cd src/Client/Desktop/Shell/bin/Release/net8.0-windows
LYBT.Desktop.Shell.exe
```

### 配置API地址

编辑 `appsettings.json`：
```json
{
  "ApiBaseUrl": "http://localhost:5001"
}
```

## 🔌 API 接口

**无直接API调用** - `LYBT.Desktop.Shell` 作为应用程序外壳，其本身不包含直接调用后端API的 Refit 客户端。

**职责划分**：
- **Shell职责**: 应用启动、模块编排、主界面框架、导航系统、对话框服务
- **API调用职责**: 由具体的业务模块（如 `LYBT.Desktop.Users`, `LYBT.Desktop.Patients` 等）在其各自的服务层中实现和管理

**鉴权流程**：
- JWT Token的获取、刷新、存储由 `LYBT.Desktop.Auth` 模块负责
- HTTP请求拦截和Token注入由 `LYBT.Desktop.Infrastructure` 中的 `HttpClient` 消息处理器统一处理

## 📚 详细文档

- **架构设计**: [docs/explanation/architecture/client/desktop-architecture.md](../../../../docs/explanation/architecture/client/desktop-architecture.md) *(待创建)*
- **模块开发指南**: [docs/development/client/desktop-module-development.md](../../../../docs/development/client/desktop-module-development.md) *(待创建)*
- **Prism框架指南**: [docs/reference/frameworks/prism-framework-guide.md](../../../../docs/reference/frameworks/prism-framework-guide.md) *(待创建)*

---

**最后更新**: 2025-10-29
**维护负责**: Client端开发组
