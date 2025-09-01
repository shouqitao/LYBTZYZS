# Desktop.Shell Project (桌面应用程序主壳项目)

## 📋 项目概述

### 项目定位
**Desktop.Shell** 是凌隐宝堂中医诊所系统的**WPF主应用程序壳项目**，负责应用程序的启动、初始化、主窗口管理和模块加载协调。作为整个桌面应用的入口点和容器，统一管理所有业务模块、工作台和核心功能的集成。

### 核心价值
- 🚀 **应用程序启动**: 系统初始化、依赖注入配置和模块注册
- 🏠 **主窗口管理**: 统一的主界面框架和布局管理
- 🔌 **模块集成**: Prism模块化架构的统一加载和管理
- 🎛️ **工作台协调**: 多工作台切换和状态管理
- 🔐 **权限控制**: 全局权限验证和访问控制
- 🎨 **主题管理**: 全局主题切换和样式管理
- 📡 **全局事件**: 应用程序级事件处理和通信协调

### 技术定位 (v1.0)
```
用户交互界面
    ↑ 承载
LYBT.Desktop.Shell (主应用程序壳) ← 本项目
    ↑ 集成
所有业务模块 + 工作台 + 核心服务
```

## 🏗️ 技术架构

### 核心技术栈
```csharp
// 基础技术栈
- .NET 8.0-windows (主应用程序)
- WPF (Windows Presentation Foundation)
- Prism.DryIoc 9.0.537 (模块化 + 依赖注入)
- Microsoft.Extensions.Hosting (主机服务)
- Microsoft.Extensions.Configuration (配置管理)
- Microsoft.Extensions.Logging (日志系统)

// 项目引用 (所有项目的集成点)
<ProjectReference Include="..\Core\LYBT.Desktop.Core.csproj" />
<ProjectReference Include="..\Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
<ProjectReference Include="..\Services\LYBT.Desktop.Services.csproj" />
<ProjectReference Include="..\Modules\Auth\LYBT.Desktop.Auth.csproj" />
<ProjectReference Include="..\Modules\Users\LYBT.Desktop.Users.csproj" />
<ProjectReference Include="..\Modules\Patients\LYBT.Desktop.Patients.csproj" />
<ProjectReference Include="..\Modules\MedicalCase\LYBT.Desktop.MedicalCase.csproj" />
<ProjectReference Include="..\Modules\Consultation\LYBT.Desktop.Consultation.csproj" />
<ProjectReference Include="..\Modules\Prescriptions\LYBT.Desktop.Prescriptions.csproj" />
<ProjectReference Include="..\Modules\Herbs\LYBT.Desktop.Herbs.csproj" />
<ProjectReference Include="..\Modules\Formula\LYBT.Desktop.Formula.csproj" />
<ProjectReference Include="..\Workbenches\Core\LYBT.Desktop.Workbench.Core.csproj" />
<ProjectReference Include="..\Workbenches\SystemWorkbench\LYBT.Desktop.Workbench.Admin.csproj" />
<ProjectReference Include="..\Workbenches\ConsultationWorkbench\LYBT.Desktop.Workbench.Consultation.csproj" />
```

### 项目结构架构
```
src/Client/Desktop/Shell/
├── App.xaml                       # 应用程序入口
├── App.xaml.cs                    # 应用程序逻辑
├── Views/                         # 主界面视图
│   ├── MainWindow.xaml           # 主窗口
│   ├── HomeView.xaml             # 首页视图
│   ├── TestView.xaml             # 测试页面
│   └── UIShowcaseWindow.xaml     # UI展示窗口
├── ViewModels/                   # 视图模型
│   ├── MainWindowViewModel.cs
│   ├── HomeViewModel.cs
│   └── ShellViewModel.cs
├── Dialogs/                      # 全局对话框
│   └── Views/                    
│       ├── ConfirmationDialog.xaml
│       ├── ErrorDetailsDialog.xaml
│       └── InformationDialog.xaml
├── Services/                     # Shell专用服务
│   ├── IShellService.cs
│   ├── ShellService.cs
│   ├── IWorkbenchManager.cs
│   └── WorkbenchManager.cs
├── Configuration/               # 配置管理
│   ├── ModuleConfiguration.cs
│   └── ShellConfiguration.cs
├── Extensions/                  # 扩展方法
│   └── PrismExtensions.cs
└── Resources/                   # 资源文件
    ├── Styles.xaml
    ├── Templates.xaml
    └── Dictionaries/
```

## 🚀 应用程序启动架构

### App.xaml.cs 主应用程序类
```csharp
/// <summary>
/// 凌隐宝堂中医诊所系统主应用程序
/// </summary>
public partial class App : PrismApplication
{
    private IHost _host;
    private ILogger<App> _logger;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // 设置全局异常处理
        SetupGlobalExceptionHandling();
        
        // 初始化主机服务
        InitializeHost();
        
        // 启动Prism应用程序
        base.OnStartup(e);
        
        _logger = Container.Resolve<ILogger<App>>();
        _logger.LogInformation("凌隐宝堂中医诊所系统启动完成");
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("凌隐宝堂中医诊所系统正在关闭");
            
            // 发布应用程序关闭事件
            Container.Resolve<IEventAggregator>().GetEvent<ApplicationShuttingDownEvent>().Publish();
            
            // 关闭主机服务
            _host?.StopAsync(TimeSpan.FromSeconds(5)).Wait();
            _host?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"应用程序关闭时发生异常: {ex}");
        }
        finally
        {
            base.OnExit(e);
        }
    }
    
    protected override Window CreateShell()
    {
        // 检查是否需要显示登录窗口
        var authService = Container.Resolve<IAuthenticationService>();
        if (!authService.IsAuthenticated)
        {
            var loginWindow = Container.Resolve<LoginWindow>();
            if (loginWindow.ShowDialog() == true)
            {
                return Container.Resolve<MainWindow>();
            }
            else
            {
                // 用户取消登录，退出应用程序
                Current.Shutdown();
                return null;
            }
        }
        
        return Container.Resolve<MainWindow>();
    }
    
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Shell专用服务
        containerRegistry.Register<IShellService, ShellService>();
        containerRegistry.Register<IWorkbenchManager, WorkbenchManager>();
        containerRegistry.RegisterSingleton<IDialogCoordinator, DialogCoordinator>();
        
        // 注册主要视图和ViewModel
        containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
        containerRegistry.RegisterForNavigation<TestView>();
        
        // 注册对话框
        containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        containerRegistry.RegisterDialog<ErrorDetailsDialog, ErrorDetailsDialogViewModel>();
        containerRegistry.RegisterDialog<InformationDialog, InformationDialogViewModel>();
        
        // 注册全局单例服务
        containerRegistry.RegisterSingleton<IApplicationLifecycleManager, ApplicationLifecycleManager>();
    }
    
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 添加核心模块 (按依赖顺序加载)
        moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable);
        
        // 添加业务模块
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);  
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.WhenAvailable);
        
        // 添加工作台模块 (按需加载)
        moduleCatalog.AddModule<SystemWorkbenchModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<ConsultationWorkbenchModule>(InitializationMode.OnDemand);
    }
    
    protected override IContainerExtension CreateContainerExtension()
    {
        var containerExtension = base.CreateContainerExtension();
        
        // 配置依赖注入容器
        var services = new ServiceCollection();
        
        // 添加配置
        var configuration = BuildConfiguration();
        services.AddSingleton<IConfiguration>(configuration);
        
        // 添加基础设施服务
        services.AddDesktopInfrastructure(configuration);
        services.AddDesktopServices(configuration);
        services.AddDesktopCore();
        
        // 添加日志服务
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
            
            if (configuration.GetValue<bool>("Logging:EnableFileLogging"))
            {
                builder.AddFile(configuration.GetValue<string>("Logging:LogFilePath"));
            }
        });
        
        // 将服务注册到Prism容器
        var serviceProvider = services.BuildServiceProvider();
        foreach (var service in services)
        {
            if (service.Lifetime == ServiceLifetime.Singleton)
            {
                containerExtension.RegisterInstance(service.ServiceType, serviceProvider.GetService(service.ServiceType));
            }
            else
            {
                containerExtension.Register(service.ServiceType, service.ImplementationType, service.Lifetime.ToPrismScope());
            }
        }
        
        return containerExtension;
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        try
        {
            // 发布应用程序初始化完成事件
            Container.Resolve<IEventAggregator>().GetEvent<ApplicationInitializedEvent>().Publish();
            
            // 初始化工作台管理器
            var workbenchManager = Container.Resolve<IWorkbenchManager>();
            workbenchManager.Initialize();
            
            _logger.LogInformation("应用程序初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用程序初始化过程中发生异常");
            
            var dialogService = Container.Resolve<IDialogService>();
            dialogService.ShowError($"应用程序初始化失败：{ex.Message}", "系统错误");
            
            Current.Shutdown(-1);
        }
    }
    
    private void InitializeHost()
    {
        var hostBuilder = Host.CreateDefaultBuilder();
        
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddDesktopInfrastructure(context.Configuration);
            services.AddDesktopServices(context.Configuration);
        });
        
        _host = hostBuilder.Build();
        _host.StartAsync();
    }
    
    private IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();
        
        return builder.Build();
    }
    
    private void SetupGlobalExceptionHandling()
    {
        // WPF主线程异常
        DispatcherUnhandledException += (sender, e) =>
        {
            LogUnhandledException(e.Exception, "WPF主线程异常");
            e.Handled = true;
            ShowCriticalErrorDialog(e.Exception);
        };
        
        // 应用程序域异常
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            LogUnhandledException(e.ExceptionObject as Exception, "应用程序域异常");
            if (e.IsTerminating)
            {
                ShowCriticalErrorDialog(e.ExceptionObject as Exception);
            }
        };
        
        // Task异常
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LogUnhandledException(e.Exception, "未观察到的Task异常");
            e.SetObserved();
        };
    }
    
    private void LogUnhandledException(Exception exception, string context)
    {
        try
        {
            var errorMessage = $"{context}: {exception?.ToString() ?? "未知异常"}";
            System.Diagnostics.Debug.WriteLine(errorMessage);
            
            // 如果日志服务可用，记录到日志
            if (_logger != null)
            {
                _logger.LogCritical(exception, context);
            }
        }
        catch
        {
            // 忽略日志记录异常
        }
    }
    
    private void ShowCriticalErrorDialog(Exception exception)
    {
        try
        {
            var errorWindow = new CriticalErrorDialog();
            errorWindow.SetError(exception);
            errorWindow.ShowDialog();
        }
        catch
        {
            // 如果无法显示错误对话框，使用系统消息框
            MessageBox.Show(
                $"系统发生严重错误：{exception?.Message ?? "未知错误"}\n\n应用程序将关闭。",
                "系统错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
```

## 🏠 主窗口架构

### MainWindow.xaml 主窗口界面
```xml
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:prism="http://prismlibrary.com/"
        xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
        prism:ViewModelLocator.AutoWireViewModel="True"
        Title="{Binding WindowTitle}"
        Icon="/Assets/Images/app-icon.ico"
        Width="1400" Height="900"
        MinWidth="1200" MinHeight="700"
        WindowStartupLocation="CenterScreen"
        WindowState="Maximized">
    
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Themes/MainWindowTheme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
    
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="Loaded">
            <i:InvokeCommandAction Command="{Binding LoadedCommand}"/>
        </i:EventTrigger>
        <i:EventTrigger EventName="Closing">
            <i:InvokeCommandAction Command="{Binding ClosingCommand}"/>
        </i:EventTrigger>
    </i:Interaction.Triggers>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 标题栏 -->
            <RowDefinition Height="Auto"/>  <!-- 工具栏 -->
            <RowDefinition Height="*"/>     <!-- 主内容区 -->
            <RowDefinition Height="Auto"/>  <!-- 状态栏 -->
        </Grid.RowDefinitions>
        
        <!-- 自定义标题栏 -->
        <Border Grid.Row="0" 
                Background="{DynamicResource PrimaryBrush}"
                Height="40">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <!-- 应用程序图标和标题 -->
                <StackPanel Grid.Column="0" 
                           Orientation="Horizontal" 
                           VerticalAlignment="Center"
                           Margin="12,0,0,0">
                    <Image Source="/Assets/Images/app-icon.png" 
                           Width="20" Height="20" 
                           Margin="0,0,8,0"/>
                    <TextBlock Text="{Binding WindowTitle}" 
                              Foreground="White" 
                              FontSize="14" 
                              FontWeight="SemiBold"/>
                </StackPanel>
                
                <!-- 用户信息 -->
                <StackPanel Grid.Column="1" 
                           Orientation="Horizontal" 
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center">
                    <TextBlock Text="{Binding CurrentUserInfo}" 
                              Foreground="White" 
                              FontSize="12"/>
                    <TextBlock Text="|" 
                              Foreground="White" 
                              Margin="10,0"/>
                    <TextBlock Text="{Binding CurrentWorkbenchName}" 
                              Foreground="White" 
                              FontSize="12" 
                              FontWeight="Medium"/>
                </StackPanel>
                
                <!-- 窗口控制按钮 -->
                <StackPanel Grid.Column="2" 
                           Orientation="Horizontal">
                    <Button Content="🗕" 
                           Command="{Binding MinimizeCommand}"
                           Style="{DynamicResource TitleBarButtonStyle}"/>
                    <Button Content="🗖" 
                           Command="{Binding MaximizeCommand}"
                           Style="{DynamicResource TitleBarButtonStyle}"/>
                    <Button Content="🗙" 
                           Command="{Binding CloseCommand}"
                           Style="{DynamicResource TitleBarCloseButtonStyle}"/>
                </StackPanel>
            </Grid>
        </Border>
        
        <!-- 主工具栏 -->
        <Border Grid.Row="1" 
                Background="{DynamicResource ToolbarBackgroundBrush}"
                BorderBrush="{DynamicResource DividerBrush}"
                BorderThickness="0,0,0,1">
            <ToolBar Background="Transparent" 
                    BorderThickness="0"
                    Margin="12,4">
                
                <!-- 工作台切换 -->
                <ComboBox ItemsSource="{Binding AvailableWorkbenches}"
                         SelectedItem="{Binding CurrentWorkbench}"
                         DisplayMemberPath="DisplayName"
                         Width="180"
                         Margin="0,0,12,0"/>
                
                <Separator/>
                
                <!-- 快捷操作按钮 -->
                <Button Content="首页" 
                       Command="{Binding NavigateToHomeCommand}"
                       Style="{DynamicResource ToolbarButtonStyle}"/>
                       
                <Button Content="新增患者" 
                       Command="{Binding CreatePatientCommand}"
                       Style="{DynamicResource ToolbarButtonStyle}"/>
                       
                <Button Content="开具处方" 
                       Command="{Binding CreatePrescriptionCommand}"
                       Style="{DynamicResource ToolbarButtonStyle}"/>
                
                <Separator/>
                
                <!-- 搜索框 -->
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                        Width="250"
                        Height="28"
                        VerticalContentAlignment="Center"
                        Tag="搜索患者、医案...">
                    <TextBox.Style>
                        <Style TargetType="TextBox" BasedOn="{StaticResource SearchTextBoxStyle}">
                            <Style.Triggers>
                                <Trigger Property="Text" Value="">
                                    <Setter Property="Background">
                                        <Setter.Value>
                                            <VisualBrush AlignmentX="Left" AlignmentY="Center" Stretch="None">
                                                <VisualBrush.Visual>
                                                    <Label Content="{Binding RelativeSource={RelativeSource AncestorType=TextBox}, Path=Tag}" 
                                                          Foreground="Gray" FontStyle="Italic"/>
                                                </VisualBrush.Visual>
                                            </VisualBrush>
                                        </Setter.Value>
                                    </Setter>
                                </Trigger>
                            </Style.Triggers>
                        </Style>
                    </TextBox.Style>
                </TextBox>
                
                <Button Content="🔍" 
                       Command="{Binding SearchCommand}"
                       Style="{DynamicResource ToolbarButtonStyle}"/>
            </ToolBar>
        </Border>
        
        <!-- 主内容区域 -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="200" MinWidth="150"/>  <!-- 左侧导航 -->
                <ColumnDefinition Width="Auto"/>                <!-- 分隔器 -->
                <ColumnDefinition Width="*"/>                   <!-- 主内容 -->
            </Grid.ColumnDefinitions>
            
            <!-- 左侧导航面板 -->
            <Border Grid.Column="0" 
                   Background="{DynamicResource NavigationBackgroundBrush}"
                   BorderBrush="{DynamicResource DividerBrush}"
                   BorderThickness="0,0,1,0">
                <ContentControl prism:RegionManager.RegionName="NavigationRegion"/>
            </Border>
            
            <!-- 分隔器 -->
            <GridSplitter Grid.Column="1" 
                         Width="4" 
                         HorizontalAlignment="Stretch" 
                         Background="{DynamicResource DividerBrush}"/>
            
            <!-- 主内容区域 -->
            <Border Grid.Column="2" 
                   Background="{DynamicResource ContentBackgroundBrush}">
                <ContentControl prism:RegionManager.RegionName="ContentRegion"/>
            </Border>
        </Grid>
        
        <!-- 状态栏 -->
        <StatusBar Grid.Row="3" 
                  Background="{DynamicResource StatusBarBackgroundBrush}">
            <StatusBarItem>
                <TextBlock Text="{Binding StatusMessage}" 
                          FontSize="11"/>
            </StatusBarItem>
            
            <StatusBarItem HorizontalAlignment="Right">
                <StackPanel Orientation="Horizontal">
                    <!-- 系统状态指示器 -->
                    <Ellipse Width="8" Height="8" 
                            Fill="{Binding ConnectionStatusBrush}" 
                            Margin="0,0,4,0"/>
                    <TextBlock Text="{Binding ConnectionStatusText}" 
                              FontSize="11" 
                              Margin="0,0,12,0"/>
                    
                    <!-- 当前时间 -->
                    <TextBlock Text="{Binding CurrentTime}" 
                              FontSize="11"/>
                </StackPanel>
            </StatusBarItem>
        </StatusBar>
        
        <!-- 全局加载指示器 -->
        <Grid Grid.RowSpan="4"
             Background="#80000000"
             Visibility="{Binding IsGlobalBusy, Converter={StaticResource BooleanToVisibilityConverter}}">
            <Border Background="White"
                   CornerRadius="8"
                   Padding="30"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center">
                <StackPanel>
                    <ProgressBar IsIndeterminate="True" 
                               Width="200" 
                               Height="4" 
                               Margin="0,0,0,16"/>
                    <TextBlock Text="{Binding GlobalBusyMessage}" 
                              HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
        </Grid>
        
    </Grid>
</Window>
```

### MainWindowViewModel 主窗口视图模型
```csharp
public class MainWindowViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAuthenticationService _authenticationService;
    private readonly IWorkbenchManager _workbenchManager;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainWindowViewModel> _logger;
    
    private string _windowTitle = "凌隐宝堂中医诊所诊疗系统 v1.0";
    private string _statusMessage = "就绪";
    private string _searchText;
    private bool _isGlobalBusy;
    private string _globalBusyMessage;
    private Timer _timeUpdateTimer;
    private string _currentTime;
    
    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
    
    public bool IsGlobalBusy
    {
        get => _isGlobalBusy;
        set => SetProperty(ref _isGlobalBusy, value);
    }
    
    public string GlobalBusyMessage
    {
        get => _globalBusyMessage;
        set => SetProperty(ref _globalBusyMessage, value);
    }
    
    public string CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }
    
    public string CurrentUserInfo => 
        _authenticationService.CurrentUser != null 
            ? $"欢迎，{_authenticationService.CurrentUser.DisplayName}"
            : "未登录";
    
    public string CurrentWorkbenchName => _workbenchManager.CurrentWorkbench?.DisplayName ?? "未选择工作台";
    
    public ObservableCollection<WorkbenchInfo> AvailableWorkbenches { get; }
    
    public WorkbenchInfo CurrentWorkbench
    {
        get => _workbenchManager.CurrentWorkbench;
        set => _workbenchManager.SwitchWorkbench(value?.Name);
    }
    
    public Brush ConnectionStatusBrush => 
        _authenticationService.IsAuthenticated 
            ? Brushes.Green 
            : Brushes.Red;
    
    public string ConnectionStatusText => 
        _authenticationService.IsAuthenticated 
            ? "已连接" 
            : "未连接";
    
    // Commands
    public DelegateCommand LoadedCommand { get; }
    public DelegateCommand ClosingCommand { get; }
    public DelegateCommand MinimizeCommand { get; }
    public DelegateCommand MaximizeCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public DelegateCommand NavigateToHomeCommand { get; }
    public DelegateCommand CreatePatientCommand { get; }
    public DelegateCommand CreatePrescriptionCommand { get; }
    public DelegateCommand SearchCommand { get; }
    
    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IAuthenticationService authenticationService,
        IWorkbenchManager workbenchManager,
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
        _authenticationService = authenticationService;
        _workbenchManager = workbenchManager;
        _dialogService = dialogService;
        _logger = logger;
        
        AvailableWorkbenches = new ObservableCollection<WorkbenchInfo>();
        
        // 初始化Commands
        LoadedCommand = new DelegateCommand(OnLoaded);
        ClosingCommand = new DelegateCommand(OnClosing);
        MinimizeCommand = new DelegateCommand(OnMinimize);
        MaximizeCommand = new DelegateCommand(OnMaximize);
        CloseCommand = new DelegateCommand(OnClose);
        NavigateToHomeCommand = new DelegateCommand(OnNavigateToHome);
        CreatePatientCommand = new DelegateCommand(OnCreatePatient);
        CreatePrescriptionCommand = new DelegateCommand(OnCreatePrescription);
        SearchCommand = new DelegateCommand(OnSearch);
        
        // 订阅事件
        SubscribeToEvents();
        
        // 初始化时间更新器
        InitializeTimeUpdater();
    }
    
    private void OnLoaded()
    {
        try
        {
            _logger.LogInformation("主窗口已加载");
            
            // 加载可用工作台
            LoadAvailableWorkbenches();
            
            // 导航到首页
            _regionManager.RequestNavigate("ContentRegion", "HomeView");
            
            // 更新状态
            StatusMessage = "应用程序已就绪";
            
            _logger.LogInformation("主窗口初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "主窗口加载过程中发生异常");
            _dialogService.ShowError($"主窗口初始化失败：{ex.Message}", "系统错误");
        }
    }
    
    private async void OnClosing()
    {
        try
        {
            _logger.LogInformation("主窗口正在关闭");
            
            // 询问用户是否确认关闭
            var shouldClose = await _dialogService.ShowConfirmationAsync(
                "确定要退出凌隐宝堂中医诊所系统吗？", 
                "确认退出");
            
            if (shouldClose)
            {
                // 执行清理工作
                await PerformCleanupAsync();
                
                _logger.LogInformation("主窗口关闭确认");
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "主窗口关闭过程中发生异常");
        }
    }
    
    private void OnMinimize()
    {
        Application.Current.MainWindow.WindowState = WindowState.Minimized;
    }
    
    private void OnMaximize()
    {
        var mainWindow = Application.Current.MainWindow;
        mainWindow.WindowState = mainWindow.WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }
    
    private async void OnClose()
    {
        var shouldClose = await _dialogService.ShowConfirmationAsync(
            "确定要退出系统吗？", 
            "确认退出");
        
        if (shouldClose)
        {
            Application.Current.Shutdown();
        }
    }
    
    private void OnNavigateToHome()
    {
        _regionManager.RequestNavigate("ContentRegion", "HomeView");
        StatusMessage = "导航到首页";
    }
    
    private void OnCreatePatient()
    {
        // 导航到患者创建页面
        var navigationParams = new NavigationParameters
        {
            { "mode", "create" }
        };
        
        _regionManager.RequestNavigate("ContentRegion", "PatientCreateView", navigationParams);
        StatusMessage = "正在创建新患者...";
    }
    
    private void OnCreatePrescription()
    {
        // 导航到处方创建页面
        _regionManager.RequestNavigate("ContentRegion", "PrescriptionCreateView");
        StatusMessage = "正在开具处方...";
    }
    
    private void OnSearch()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            // 执行全局搜索
            var searchParams = new NavigationParameters
            {
                { "searchTerm", SearchText }
            };
            
            _regionManager.RequestNavigate("ContentRegion", "SearchResultsView", searchParams);
            StatusMessage = $"搜索：{SearchText}";
        }
    }
    
    private void SubscribeToEvents()
    {
        // 订阅用户登录状态变化
        _authenticationService.AuthenticationStateChanged += (sender, isAuthenticated) =>
        {
            RaisePropertyChanged(nameof(CurrentUserInfo));
            RaisePropertyChanged(nameof(ConnectionStatusBrush));
            RaisePropertyChanged(nameof(ConnectionStatusText));
        };
        
        // 订阅工作台变化事件
        _workbenchManager.WorkbenchChanged += (sender, workbench) =>
        {
            RaisePropertyChanged(nameof(CurrentWorkbenchName));
            RaisePropertyChanged(nameof(CurrentWorkbench));
        };
        
        // 订阅全局忙碌状态变化
        _eventAggregator.GetEvent<GlobalBusyStateChangedEvent>().Subscribe(OnGlobalBusyStateChanged);
        
        // 订阅状态消息变化
        _eventAggregator.GetEvent<StatusMessageChangedEvent>().Subscribe(OnStatusMessageChanged);
    }
    
    private void OnGlobalBusyStateChanged(GlobalBusyState busyState)
    {
        IsGlobalBusy = busyState.IsBusy;
        GlobalBusyMessage = busyState.Message;
    }
    
    private void OnStatusMessageChanged(string message)
    {
        StatusMessage = message;
    }
    
    private void LoadAvailableWorkbenches()
    {
        AvailableWorkbenches.Clear();
        
        var workbenches = _workbenchManager.GetAvailableWorkbenches();
        foreach (var workbench in workbenches)
        {
            AvailableWorkbenches.Add(workbench);
        }
        
        // 设置默认工作台
        if (workbenches.Any())
        {
            var defaultWorkbench = workbenches.FirstOrDefault(w => w.IsDefault) ?? workbenches.First();
            _workbenchManager.SwitchWorkbench(defaultWorkbench.Name);
        }
    }
    
    private void InitializeTimeUpdater()
    {
        _timeUpdateTimer = new Timer(_ =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    private async Task PerformCleanupAsync()
    {
        try
        {
            // 停止计时器
            _timeUpdateTimer?.Dispose();
            
            // 保存用户设置
            var configService = Container.Resolve<IConfigurationService>();
            await configService.SaveAsync();
            
            // 清理缓存
            var cacheService = Container.Resolve<ICacheService>();
            await cacheService.ClearAsync();
            
            _logger.LogInformation("应用程序清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "应用程序清理过程中发生异常");
        }
    }
}
```

## 🎛️ 工作台管理架构

### 工作台管理器 (WorkbenchManager)
```csharp
// 工作台管理器接口
public interface IWorkbenchManager
{
    WorkbenchInfo CurrentWorkbench { get; }
    List<WorkbenchInfo> GetAvailableWorkbenches();
    bool SwitchWorkbench(string workbenchName);
    void Initialize();
    event EventHandler<WorkbenchInfo> WorkbenchChanged;
}

// 工作台管理器实现
public class WorkbenchManager : IWorkbenchManager
{
    private readonly IModuleManager _moduleManager;
    private readonly IRegionManager _regionManager;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<WorkbenchManager> _logger;
    
    private WorkbenchInfo _currentWorkbench;
    private readonly Dictionary<string, WorkbenchInfo> _workbenches;
    
    public WorkbenchInfo CurrentWorkbench
    {
        get => _currentWorkbench;
        private set
        {
            if (_currentWorkbench != value)
            {
                _currentWorkbench = value;
                WorkbenchChanged?.Invoke(this, value);
            }
        }
    }
    
    public event EventHandler<WorkbenchInfo> WorkbenchChanged;
    
    public WorkbenchManager(
        IModuleManager moduleManager,
        IRegionManager regionManager,
        IAuthenticationService authenticationService,
        ILogger<WorkbenchManager> logger)
    {
        _moduleManager = moduleManager;
        _regionManager = regionManager;
        _authenticationService = authenticationService;
        _logger = logger;
        _workbenches = new Dictionary<string, WorkbenchInfo>();
    }
    
    public void Initialize()
    {
        try
        {
            _logger.LogInformation("初始化工作台管理器");
            
            // 注册可用工作台
            RegisterWorkbenches();
            
            _logger.LogInformation("工作台管理器初始化完成，共注册 {Count} 个工作台", _workbenches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作台管理器初始化失败");
            throw;
        }
    }
    
    public List<WorkbenchInfo> GetAvailableWorkbenches()
    {
        var currentUser = _authenticationService.CurrentUser;
        if (currentUser == null)
            return new List<WorkbenchInfo>();
        
        return _workbenches.Values
            .Where(w => w.RequiredRoles.Contains(currentUser.Role))
            .OrderBy(w => w.DisplayOrder)
            .ToList();
    }
    
    public bool SwitchWorkbench(string workbenchName)
    {
        try
        {
            if (string.IsNullOrEmpty(workbenchName) || !_workbenches.ContainsKey(workbenchName))
            {
                _logger.LogWarning("尝试切换到不存在的工作台: {WorkbenchName}", workbenchName);
                return false;
            }
            
            var workbench = _workbenches[workbenchName];
            var currentUser = _authenticationService.CurrentUser;
            
            // 检查权限
            if (currentUser == null || !workbench.RequiredRoles.Contains(currentUser.Role))
            {
                _logger.LogWarning("用户 {Username} 没有权限访问工作台 {WorkbenchName}", 
                    currentUser?.Username, workbenchName);
                return false;
            }
            
            // 加载工作台模块
            if (!string.IsNullOrEmpty(workbench.ModuleName))
            {
                _moduleManager.LoadModule(workbench.ModuleName);
            }
            
            // 切换导航区域内容
            if (!string.IsNullOrEmpty(workbench.NavigationView))
            {
                _regionManager.RequestNavigate("NavigationRegion", workbench.NavigationView);
            }
            
            // 切换主内容区域
            if (!string.IsNullOrEmpty(workbench.MainView))
            {
                _regionManager.RequestNavigate("ContentRegion", workbench.MainView);
            }
            
            CurrentWorkbench = workbench;
            
            _logger.LogInformation("成功切换到工作台: {WorkbenchName}", workbenchName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换工作台失败: {WorkbenchName}", workbenchName);
            return false;
        }
    }
    
    private void RegisterWorkbenches()
    {
        // 注册系统管理工作台
        _workbenches.Add("SystemWorkbench", new WorkbenchInfo
        {
            Name = "SystemWorkbench",
            DisplayName = "系统管理",
            Description = "用户管理、系统配置、数据维护",
            ModuleName = "SystemWorkbenchModule",
            NavigationView = "SystemWorkbenchNavigationView",
            MainView = "SystemWorkbenchMainView",
            RequiredRoles = [UserRole.Admin],
            DisplayOrder = 1,
            IsDefault = false,
            Icon = "/Assets/Icons/system-workbench.png"
        });
        
        // 注册诊疗工作台
        _workbenches.Add("ConsultationWorkbench", new WorkbenchInfo
        {
            Name = "ConsultationWorkbench",
            DisplayName = "中医诊疗",
            Description = "患者接诊、四诊记录、处方开具",
            ModuleName = "ConsultationWorkbenchModule",
            NavigationView = "ConsultationWorkbenchNavigationView",
            MainView = "ConsultationWorkbenchMainView",
            RequiredRoles = [UserRole.Doctor, UserRole.Admin],
            DisplayOrder = 2,
            IsDefault = true,
            Icon = "/Assets/Icons/consultation-workbench.png"
        });
    }
}

// 工作台信息模型
public class WorkbenchInfo
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string ModuleName { get; set; }
    public string NavigationView { get; set; }
    public string MainView { get; set; }
    public List<UserRole> RequiredRoles { get; set; } = [];
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public string Icon { get; set; }
}
```

## 🔧 依赖注入和服务配置

### Shell配置扩展
```csharp
// Shell服务扩展
public static class ShellServiceExtensions
{
    public static IContainerRegistry RegisterShellServices(this IContainerRegistry containerRegistry)
    {
        // 注册Shell核心服务
        containerRegistry.RegisterSingleton<IShellService, ShellService>();
        containerRegistry.RegisterSingleton<IWorkbenchManager, WorkbenchManager>();
        containerRegistry.RegisterSingleton<IApplicationLifecycleManager, ApplicationLifecycleManager>();
        
        // 注册视图和ViewModel
        containerRegistry.RegisterForNavigation<MainWindow, MainWindowViewModel>();
        containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
        
        // 注册全局对话框
        containerRegistry.RegisterDialog<CriticalErrorDialog, CriticalErrorDialogViewModel>();
        containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        
        return containerRegistry;
    }
}

// ServiceLifetime到Prism作用域转换
public static class ServiceLifetimeExtensions
{
    public static Prism.Ioc.IScopedProvider ToPrismScope(this ServiceLifetime lifetime)
    {
        return lifetime switch
        {
            ServiceLifetime.Singleton => new Prism.Ioc.IScopedProvider.Singleton(),
            ServiceLifetime.Scoped => new Prism.Ioc.IScopedProvider.Scoped(),
            ServiceLifetime.Transient => new Prism.Ioc.IScopedProvider.Transient(),
            _ => new Prism.Ioc.IScopedProvider.Transient()
        };
    }
}
```

## 🧪 测试规范

### Shell集成测试
```csharp
[TestFixture]
public class ShellIntegrationTests
{
    private App _app;
    private TestContext _testContext;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _testContext = new TestContext();
        _app = new App();
        _app.InitializeComponent();
    }
    
    [Test]
    public void App_Initialization_CompletesSuccessfully()
    {
        Assert.DoesNotThrow(() =>
        {
            var containerExtension = _app.CreateContainerExtension();
            Assert.That(containerExtension, Is.Not.Null);
        });
    }
    
    [Test]
    public void ModuleCatalog_Configuration_RegistersAllModules()
    {
        var moduleCatalog = new ModuleCatalog();
        _app.ConfigureModuleCatalog(moduleCatalog);
        
        Assert.That(moduleCatalog.Modules.Count, Is.GreaterThan(0));
        Assert.That(moduleCatalog.Modules.Any(m => m.ModuleName == "AuthModule"), Is.True);
        Assert.That(moduleCatalog.Modules.Any(m => m.ModuleName == "UsersModule"), Is.True);
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _app?.Shutdown();
        _testContext?.Dispose();
    }
}

[TestFixture]
public class WorkbenchManagerTests
{
    private Mock<IModuleManager> _mockModuleManager;
    private Mock<IRegionManager> _mockRegionManager;
    private Mock<IAuthenticationService> _mockAuthenticationService;
    private Mock<ILogger<WorkbenchManager>> _mockLogger;
    private WorkbenchManager _workbenchManager;
    
    [SetUp]
    public void SetUp()
    {
        _mockModuleManager = new Mock<IModuleManager>();
        _mockRegionManager = new Mock<IRegionManager>();
        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<WorkbenchManager>>();
        
        _workbenchManager = new WorkbenchManager(
            _mockModuleManager.Object,
            _mockRegionManager.Object,
            _mockAuthenticationService.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public void Initialize_RegistersWorkbenches_Successfully()
    {
        // Act
        _workbenchManager.Initialize();
        
        // Assert
        var workbenches = _workbenchManager.GetAvailableWorkbenches();
        Assert.That(workbenches, Is.Not.Empty);
    }
    
    [Test]
    public void SwitchWorkbench_WithValidWorkbench_ReturnsTrue()
    {
        // Arrange
        var user = new UserInfo { Role = UserRole.Admin };
        _mockAuthenticationService.SetupGet(x => x.CurrentUser).Returns(user);
        _workbenchManager.Initialize();
        
        // Act
        var result = _workbenchManager.SwitchWorkbench("SystemWorkbench");
        
        // Assert
        Assert.That(result, Is.True);
        Assert.That(_workbenchManager.CurrentWorkbench?.Name, Is.EqualTo("SystemWorkbench"));
    }
}
```

## 🚀 构建和部署

### 项目文件配置
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- 应用程序信息 -->
    <AssemblyTitle>凌隐宝堂中医诊所诊疗系统</AssemblyTitle>
    <AssemblyDescription>中医诊所管理系统桌面客户端</AssemblyDescription>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <FileVersion>1.0.0</FileVersion>
    <ApplicationIcon>Assets\app-icon.ico</ApplicationIcon>
    
    <!-- 发布配置 -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Prism.DryIoc" Version="9.0.537" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />
    <PackageReference Include="Serilog.Extensions.Logging.File" Version="3.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- 引用所有项目 -->
    <ProjectReference Include="..\Core\LYBT.Desktop.Core.csproj" />
    <ProjectReference Include="..\Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
    <ProjectReference Include="..\Services\LYBT.Desktop.Services.csproj" />
    
    <!-- 业务模块 -->
    <ProjectReference Include="..\Modules\Auth\LYBT.Desktop.Auth.csproj" />
    <ProjectReference Include="..\Modules\Users\LYBT.Desktop.Users.csproj" />
    <ProjectReference Include="..\Modules\Patients\LYBT.Desktop.Patients.csproj" />
    <ProjectReference Include="..\Modules\MedicalCase\LYBT.Desktop.MedicalCase.csproj" />
    <ProjectReference Include="..\Modules\Consultation\LYBT.Desktop.Consultation.csproj" />
    <ProjectReference Include="..\Modules\Prescriptions\LYBT.Desktop.Prescriptions.csproj" />
    <ProjectReference Include="..\Modules\Herbs\LYBT.Desktop.Herbs.csproj" />
    <ProjectReference Include="..\Modules\Formula\LYBT.Desktop.Formula.csproj" />
    
    <!-- 工作台 -->
    <ProjectReference Include="..\Workbenches\Core\LYBT.Desktop.Workbench.Core.csproj" />
    <ProjectReference Include="..\Workbenches\SystemWorkbench\LYBT.Desktop.Workbench.Admin.csproj" />
    <ProjectReference Include="..\Workbenches\ConsultationWorkbench\LYBT.Desktop.Workbench.Consultation.csproj" />
    
    <!-- 共享项目 -->
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Resource Include="Assets\**\*" />
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="appsettings.Development.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

### 发布脚本
```bash
#!/bin/bash
# 构建和发布Shell应用程序

echo "开始构建Desktop.Shell项目..."

# 清理之前的构建
dotnet clean src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --configuration Release

# 构建项目
dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --configuration Release --verbosity minimal

if [ $? -eq 0 ]; then
    echo "✅ 构建成功"
    
    # 发布应用程序
    echo "开始发布应用程序..."
    dotnet publish src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj \
        --configuration Release \
        --runtime win-x64 \
        --self-contained true \
        --single-file \
        --output dist/desktop-app
    
    if [ $? -eq 0 ]; then
        echo "✅ 发布成功"
        echo "发布文件位于: dist/desktop-app/"
    else
        echo "❌ 发布失败"
        exit 1
    fi
else
    echo "❌ 构建失败"
    exit 1
fi
```

## 📚 相关文档

### 架构文档
- [WPF主应用程序架构设计](../../architecture/wpf-main-application-architecture.md)
- [Prism模块化架构实现](../../architecture/prism-modular-architecture.md)
- [工作台管理系统设计](../../architecture/workbench-management-system.md)

### 开发指南
- [Shell应用程序开发规范](../../development/shell-application-development-standards.md)
- [模块加载和初始化指南](../../development/module-loading-initialization-guide.md)
- [全局异常处理策略](../../development/global-exception-handling-strategy.md)
- [应用程序生命周期管理](../../development/application-lifecycle-management.md)

### 测试指南
- [Shell集成测试规范](../../testing/shell-integration-testing-standards.md)
- [模块化应用测试实践](../../testing/modular-application-testing-practices.md)

### 部署文档
- [桌面应用程序部署指南](../../deployment/desktop-application-deployment-guide.md)
- [单文件发布配置](../../deployment/single-file-publishing-configuration.md)
- [Windows安装程序制作](../../deployment/windows-installer-creation.md)

### 用户文档
- [系统启动和登录指南](../../guides/system-startup-login-guide.md)
- [工作台切换使用说明](../../guides/workbench-switching-guide.md)
- [快捷操作使用手册](../../guides/quick-actions-manual.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 前端开发组