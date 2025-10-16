# Client端开发指南

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**维护团队**：前端开发组  

## 🎯 Client端开发导航

凌隐宝堂中医诊所管理系统Client端采用**WPF五层MVVM架构**，严格遵循Shell + Core + Services + Infrastructure + Modules的分层模式，确保UI与业务逻辑的完全分离。

### 📋 Client端技术栈

| 技术 | 版本 | 用途 | 说明 |
|------|------|------|------|
| **.NET** | 8.0 | 运行时 | 最新的LTS版本 |
| **WPF** | .NET 8 | UI框架 | Windows桌面应用开发 |
| **MVVM** | CommunityToolkit.Mvvm | 架构模式 | 数据绑定和命令 |
| **依赖注入** | Microsoft.Extensions.DependencyInjection | DI容器 | 服务注入管理 |
| **AutoMapper** | 12.0 | 对象映射 | 实体与视图模型转换 |
| **Material Design** | 4.9 | UI组件库 | 现代化UI设计 |
| **HttpClientFactory** | 8.0 | HTTP客户端 | API调用管理 |
| **Serilog** | 3.0 | 日志框架 | 客户端日志记录 |
| **NUnit** | 3.13 | 单元测试 | 测试框架 |

## 🏗️ Client端架构设计

### 五层架构模式
```
LYBT.Desktop (WPF应用)
├── Shell/               # Shell层 - 应用程序容器
│   ├── App.xaml         # 应用程序入口
│   ├── MainWindow.xaml  # 主窗口
│   └── AppStart.cs      # 启动配置
├── Core/                # Core层 - 核心基础设施
│   ├── IocContainer.cs  # IoC容器配置
│   ├── EventAggregator.cs # 事件聚合器
│   ├── NavigationService.cs # 导航服务
│   ├── ViewModelBase.cs # 视图模型基类
│   └── Services/        # 核心服务
├── Services/            # Services层 - 业务服务
│   ├── Interfaces/      # 服务接口
│   ├── AuthService.cs   # 认证服务
│   ├── PatientService.cs # 患者服务
│   └── ApiService.cs    # API基础服务
├── Infrastructure/      # Infrastructure层 - 基础设施
│   ├── Caching/         # 缓存服务
│   ├── Storage/         # 本地存储
│   ├── Security/        # 安全组件
│   └── Http/           # HTTP客户端配置
└── Modules/             # Modules层 - 业务模块
    ├── Auth/           # 认证模块
    │   ├── Views/      # 视图
    │   ├── ViewModels/ # 视图模型
    │   ├── Models/     # 数据模型
    │   └── Commands/   # 命令
    ├── Patients/       # 患者管理模块
    ├── MedicalCase/    # 医案管理模块
    ├── Consultation/   # 诊疗模块
    ├── Prescriptions/  # 处方模块
    ├── Herbs/          # 药材模块
    ├── Formula/        # 验方模块
    └── Users/          # 用户管理模块
```

## 🔧 开发环境配置

### 1. 项目启动配置
```csharp
// App.xaml.cs
public partial class App : Application
{
    private IHost _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 构建主机
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            })
            .Build();

        // 启动主窗口
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 核心服务
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        
        // HTTP客户端
        services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri("https://localhost:5001/api/");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", GetAccessToken());
        });
        
        // 基础设施服务
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ISecureStorage, SecureStorageService>();
        
        // 业务服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IMedicalCaseService, MedicalCaseService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IHerbService, HerbService>();
        services.AddScoped<IFormulaService, FormulaService>();
        
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));
        
        // 视图和视图模型
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();
        
        // 注册模块
        RegisterModules(services);
    }
    
    private void RegisterModules(IServiceCollection services)
    {
        // 注册各个模块的服务和视图
        services.AddTransient<LoginView>();
        services.AddTransient<LoginViewModel>();
        
        services.AddTransient<PatientManagementView>();
        services.AddTransient<PatientManagementViewModel>();
        
        // ... 其他模块注册
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
```

### 2. 依赖注入配置
```csharp
// Core/IocContainer.cs
public static class IocContainer
{
    public static IServiceProvider ServiceProvider { get; private set; }

    public static void Initialize(IServiceCollection services)
    {
        var serviceCollection = services.ConfigureDesktopServices();
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public static T GetService<T>() where T : class
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public static T GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }
}

// Core/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureDesktopServices(this IServiceCollection services)
    {
        // 配置日志
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/client-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // 配置HttpClient
        services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:5001/api/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 配置缓存
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024; // 限制缓存大小
        });

        return services;
    }
}
```

## 📝 MVVM架构实现

### 1. ViewModelBase基类
```csharp
// Core/ViewModelBase.cs
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private bool _isBusy;
    private string _title;

    protected ViewModelBase(IEventAggregator eventAggregator, IDialogService dialogService)
    {
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        Commands = new List<ICommand>();
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected List<ICommand> Commands { get; }

    protected void Publish<T>(T eventData) where T : class
    {
        _eventAggregator.Publish(eventData);
    }

    protected void Subscribe<T>(Action<T> handler) where T : class
    {
        _eventAggregator.Subscribe(handler);
    }

    protected async Task ShowErrorAsync(string message)
    {
        await _dialogService.ShowErrorAsync(message);
    }

    protected async Task ShowInfoAsync(string message)
    {
        await _dialogService.ShowInfoAsync(message);
    }

    protected async Task<bool> ShowConfirmAsync(string message)
    {
        return await _dialogService.ShowConfirmAsync(message);
    }

    protected async Task<T> ShowDialogAsync<T>(IDialog dialog) where T : class
    {
        return await _dialogService.ShowDialogAsync<T>(dialog);
    }

    public abstract Task InitializeAsync();

    private bool _disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 取消事件订阅
                Commands.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

### 2. 可观察对象实现
```csharp
// Core/ObservableObject.cs
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void SetProperty<T>(ref T field, T value, Action onPropertyChanged, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke();
    }
}
```

### 3. 中继命令实现
```csharp
// Core/RelayCommand.cs
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;

    public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T> _canExecute;

    public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute?.Invoke((T)parameter) ?? true;
    }

    public void Execute(object parameter)
    {
        _execute((T)parameter);
    }
}

public class AsyncRelayCommand : RelayCommand
{
    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        : base(async _ => await execute(), _ => canExecute?.Invoke() ?? true)
    {
    }

    public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute = null)
        : base(async param => await execute(param), canExecute)
    {
    }
}
```

## 🎨 UI开发规范

### 1. 主窗口实现
```xml
<!-- MainWindow.xaml -->
<Window x:Class="LYBT.Desktop.Shell.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="凌隐宝堂中医诊所管理系统" 
        Height="800" Width="1200"
        WindowStartupLocation="CenterScreen"
        WindowState="Maximized"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        TextElement.FontWeight="Regular"
        TextElement.FontSize="13"
        TextOptions.TextFormattingMode="Ideal"
        TextOptions.TextRenderingMode="Auto"
        Background="{DynamicResource MaterialDesignPaper}"
        FontFamily="{DynamicResource MaterialDesignFont}">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- 标题栏 -->
        <materialDesign:ColorZone Grid.Row="0" 
                                  Mode="PrimaryMid" 
                                  Padding="16">
            <DockPanel>
                <StackPanel Orientation="Horizontal" DockPanel.Dock="Left">
                    <materialDesign:PackIcon Kind="HospitalBox" 
                                            Width="24" Height="24" 
                                            VerticalAlignment="Center"
                                            Margin="0,0,8,0"/>
                    <TextBlock Text="凌隐宝堂中医诊所管理系统" 
                               VerticalAlignment="Center"
                               FontSize="18"
                               FontWeight="Medium"/>
                </StackPanel>
                
                <StackPanel Orientation="Horizontal" DockPanel.Dock="Right">
                    <Button Style="{StaticResource MaterialDesignIconButton}"
                            ToolTip="设置"
                            Command="{Binding SettingsCommand}">
                        <materialDesign:PackIcon Kind="Settings"/>
                    </Button>
                    <Button Style="{StaticResource MaterialDesignIconButton}"
                            ToolTip="关于"
                            Command="{Binding AboutCommand}">
                        <materialDesign:PackIcon Kind="Information"/>
                    </Button>
                    <Button Style="{StaticResource MaterialDesignIconButton}"
                            ToolTip="退出"
                            Command="{Binding ExitCommand}">
                        <materialDesign:PackIcon Kind="ExitToApp"/>
                    </Button>
                </StackPanel>
            </DockPanel>
        </materialDesign:ColorZone>
        
        <!-- 主内容区 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- 导航菜单 -->
            <materialDesign:ColorZone Grid.Column="0" 
                                      Mode="Light" 
                                      Padding="16"
                                      VerticalAlignment="Stretch">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <TextBlock Text="功能模块" 
                                   Style="{DynamicResource MaterialDesignHeadline6TextBlock}"
                                   Margin="0,0,0,16"/>
                        
                        <ListBox ItemsSource="{Binding Modules}" 
                                 SelectedItem="{Binding SelectedModule}"
                                 Style="{StaticResource MaterialDesignNavigationPrimaryListBox}">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <StackPanel Orientation="Horizontal">
                                        <materialDesign:PackIcon Kind="{Binding Icon}" 
                                                                Width="20" Height="20" 
                                                                VerticalAlignment="Center"
                                                                Margin="0,0,8,0"/>
                                        <TextBlock Text="{Binding Name}" 
                                                   VerticalAlignment="Center"/>
                                    </StackPanel>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </StackPanel>
                </ScrollViewer>
            </materialDesign:ColorZone>
            
            <!-- 内容区域 -->
            <ContentControl Grid.Column="1" 
                            Content="{Binding CurrentContent}"
                            Margin="16">
                <ContentControl.Resources>
                    <DataTemplate DataType="{x:Type viewmodels:PatientManagementViewModel}">
                        <views:PatientManagementView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type viewmodels:MedicalCaseManagementViewModel}">
                        <views:MedicalCaseManagementView/>
                    </DataTemplate>
                    <!-- 其他模块的数据模板 -->
                </ContentControl.Resources>
            </ContentControl>
        </Grid>
        
        <!-- 状态栏 -->
        <StatusBar Grid.Row="2">
            <StatusBarItem>
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="当前用户: "/>
                    <TextBlock Text="{Binding CurrentUser.Name}" FontWeight="Bold"/>
                </StackPanel>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="{Binding CurrentTime, StringFormat='yyyy-MM-dd HH:mm:ss'}"/>
                    <Separator Margin="8,0"/>
                    <TextBlock Text="{Binding Status}"/>
                </StackPanel>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

### 2. 主窗口视图模型
```csharp
// Shell/MainWindowViewModel.cs
public class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly DispatcherTimer _timer;

    public MainWindowViewModel(
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        INavigationService navigationService,
        IAuthService authService) : base(eventAggregator, dialogService)
    {
        _navigationService = navigationService;
        _authService = authService;
        
        Modules = new ObservableCollection<ModuleItem>();
        Commands = new List<ICommand>();
        
        InitializeCommands();
        InitializeModules();
        InitializeTimer();
        
        // 订阅事件
        Subscribe<UserLoggedInEvent>(OnUserLoggedIn);
        Subscribe<UserLoggedOutEvent>(OnUserLoggedOut);
    }

    public ObservableCollection<ModuleItem> Modules { get; }
    public ObservableCollection<ICommand> Commands { get; }

    private ViewModelBase _currentContent;
    public ViewModelBase CurrentContent
    {
        get => _currentContent;
        set => SetProperty(ref _currentContent, value);
    }

    private ModuleItem _selectedModule;
    public ModuleItem SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (SetProperty(ref _selectedModule, value))
            {
                NavigateToModule(value);
            }
        }
    }

    private UserDto _currentUser;
    public UserDto CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    private DateTime _currentTime;
    public DateTime CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    private string _status = "就绪";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public ICommand SettingsCommand { get; private set; }
    public ICommand AboutCommand { get; private set; }
    public ICommand ExitCommand { get; private set; }

    public override async Task InitializeAsync()
    {
        // 检查用户登录状态
        var currentUser = await _authService.GetCurrentUserAsync();
        if (currentUser != null)
        {
            CurrentUser = currentUser;
            ShowModuleBasedOnRole(currentUser.Role);
        }
        else
        {
            // 显示登录界面
            await ShowLoginAsync();
        }

        // 默认选择第一个模块
        if (Modules.Any())
        {
            SelectedModule = Modules.First();
        }
    }

    private void InitializeCommands()
    {
        SettingsCommand = new RelayCommand(async _ => await ShowSettingsAsync());
        AboutCommand = new RelayCommand(async _ => await ShowAboutAsync());
        ExitCommand = new RelayCommand(async _ => await ExitAsync());
    }

    private void InitializeModules()
    {
        Modules.Add(new ModuleItem 
        { 
            Name = "患者管理", 
            Icon = "AccountMultiple", 
            ViewModelType = typeof(PatientManagementViewModel),
            RequiredPermission = "PatientManage"
        });
        
        Modules.Add(new ModuleItem 
        { 
            Name = "医案管理", 
            Icon = "ClipboardText", 
            ViewModelType = typeof(MedicalCaseManagementViewModel),
            RequiredPermission = "MedicalCaseManage"
        });
        
        Modules.Add(new ModuleItem 
        { 
            Name = "诊疗管理", 
            Icon = "Stethoscope", 
            ViewModelType = typeof(ConsultationManagementViewModel),
            RequiredPermission = "ConsultationManage"
        });
        
        Modules.Add(new ModuleItem 
        { 
            Name = "处方管理", 
            Icon = "Pill", 
            ViewModelType = typeof(PrescriptionManagementViewModel),
            RequiredPermission = "PrescriptionManage"
        });
        
        Modules.Add(new ModuleItem 
        { 
            Name = "药材管理", 
            Icon = "Leaf", 
            ViewModelType = typeof(HerbManagementViewModel),
            RequiredPermission = "HerbManage"
        });
        
        Modules.Add(new ModuleItem 
        { 
            Name = "验方管理", 
            Icon = "BookOpen", 
            ViewModelType = typeof(FormulaManagementViewModel),
            RequiredPermission = "FormulaManage"
        });
        
        Modules.Add(new ModuleItem 
        { 
            Name = "用户管理", 
            Icon = "AccountGroup", 
            ViewModelType = typeof(UserManagementViewModel),
            RequiredPermission = "UserManage"
        });
    }

    private void InitializeTimer()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        _timer.Start();
    }

    private void ShowModuleBasedOnRole(string role)
    {
        // 根据用户角色显示相应的模块
        foreach (var module in Modules)
        {
            module.IsVisible = HasPermission(module.RequiredPermission);
        }
    }

    private bool HasPermission(string requiredPermission)
    {
        if (CurrentUser?.Role == "SuperAdmin")
            return true;

        if (string.IsNullOrEmpty(requiredPermission))
            return true;

        return CurrentUser?.Permissions?.Contains(requiredPermission) ?? false;
    }

    private async Task NavigateToModule(ModuleItem module)
    {
        if (module?.ViewModelType == null)
            return;

        try
        {
            IsBusy = true;
            Status = $"正在加载 {module.Name}...";

            CurrentContent = _navigationService.NavigateTo(module.ViewModelType);
            
            Status = $"已加载 {module.Name}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"加载模块失败: {ex.Message}");
            Status = "加载失败";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowLoginAsync()
    {
        var loginViewModel = IocContainer.GetService<LoginViewModel>();
        await loginViewModel.InitializeAsync();
        
        var loginView = new LoginView { DataContext = loginViewModel };
        var result = await ShowDialogAsync<bool>(loginView);
        
        if (!result)
        {
            Application.Current.Shutdown();
        }
    }

    private async Task ShowSettingsAsync()
    {
        // 实现设置界面
        await ShowInfoAsync("设置功能正在开发中");
    }

    private async Task ShowAboutAsync()
    {
        var aboutMessage = @"凌隐宝堂中医诊所管理系统
版本: 5.0.0
开发团队: 凌隐宝堂技术团队

© 2025 凌隐宝堂中医诊所. All rights reserved.";
        
        await ShowInfoAsync(aboutMessage);
    }

    private async Task ExitAsync()
    {
        var result = await ShowConfirmAsync("确定要退出系统吗？");
        if (result)
        {
            Application.Current.Shutdown();
        }
    }

    private void OnUserLoggedIn(UserLoggedInEvent eventData)
    {
        CurrentUser = eventData.User;
        ShowModuleBasedOnRole(eventData.User.Role);
    }

    private void OnUserLoggedOut(UserLoggedOutEvent eventData)
    {
        CurrentUser = null;
        CurrentContent = null;
        _ = ShowLoginAsync();
    }
}

// 模块项模型
public class ModuleItem : ObservableObject
{
    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string Name { get; set; }
    public string Icon { get; set; }
    public Type ViewModelType { get; set; }
    public string RequiredPermission { get; set; }
}
```

## 🧪 单元测试

### 1. ViewModel测试
```csharp
// Tests/ViewModels/PatientManagementViewModelTests.cs
[TestFixture]
public class PatientManagementViewModelTests
{
    private Mock<IPatientService> _mockPatientService;
    private Mock<IDialogService> _mockDialogService;
    private Mock<IEventAggregator> _mockEventAggregator;
    private PatientManagementViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _mockPatientService = new Mock<IPatientService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockEventAggregator = new Mock<IEventAggregator>();

        _viewModel = new PatientManagementViewModel(
            _mockPatientService.Object,
            _mockDialogService.Object,
            _mockEventAggregator.Object);
    }

    [Test]
    public async Task LoadPatientsCommand_WhenServiceReturnsSuccess_ShouldPopulatePatients()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new PatientDto { Id = 1, Name = "张三" },
            new PatientDto { Id = 2, Name = "李四" }
        };

        var result = ApiResult<IEnumerable<PatientDto>>.Success(patients);
        _mockPatientService.Setup(x => x.GetPatientsAsync())
            .ReturnsAsync(result);

        // Act
        await _viewModel.LoadPatientsCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual(2, _viewModel.Patients.Count);
        Assert.AreEqual("张三", _viewModel.Patients[0].Name);
        Assert.AreEqual("李四", _viewModel.Patients[1].Name);
        Assert.IsFalse(_viewModel.IsBusy);
    }

    [Test]
    public async Task AddPatientCommand_WhenUserConfirms_ShouldOpenDialog()
    {
        // Arrange
        var dialog = new Mock<IDialog>();
        dialog.Setup(x => x.ShowDialogAsync()).ReturnsAsync(true);

        _mockDialogService.Setup(x => x.ShowDialog<PatientEditViewModel, PatientEditDialog>())
            .ReturnsAsync((true, new PatientDto { Id = 1, Name = "新患者" }));

        // Act
        await _viewModel.AddPatientCommand.ExecuteAsync(null);

        // Assert
        _mockDialogService.Verify(x => x.ShowDialog<PatientEditViewModel, PatientEditDialog>(), Times.Once);
    }
}
```

### 2. 服务层测试
```csharp
// Tests/Services/PatientServiceTests.cs
[TestFixture]
public class PatientServiceTests
{
    private Mock<ApiService> _mockApiService;
    private Mock<ITokenService> _mockTokenService;
    private Mock<IMapper> _mockMapper;
    private PatientService _patientService;

    [SetUp]
    public void Setup()
    {
        _mockApiService = new Mock<ApiService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockMapper = new Mock<IMapper>();

        _patientService = new PatientService(
            _mockApiService.Object,
            _mockTokenService.Object,
            _mockMapper.Object);
    }

    [Test]
    public async Task GetPatientsAsync_WhenApiReturnsSuccess_ShouldReturnPatients()
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<PatientDto>>
        {
            Success = true,
            Data = new List<PatientDto>
            {
                new PatientDto { Id = 1, Name = "张三" },
                new PatientDto { Id = 2, Name = "李四" }
            }
        };

        _mockApiService.Setup(x => x.GetAsync<IEnumerable<PatientDto>>("patients"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _patientService.GetPatientsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Data.Count());
    }

    [Test]
    public async Task CreatePatientAsync_WhenRequestIsValid_ShouldCreatePatient()
    {
        // Arrange
        var request = new PatientCreateRequest
        {
            Name = "张三",
            Gender = "男",
            BirthDate = new DateTime(1990, 1, 1),
            Phone = "13800138000"
        };

        var apiResponse = new ApiResponse<PatientDto>
        {
            Success = true,
            Data = new PatientDto { Id = 1, Name = request.Name }
        };

        _mockApiService.Setup(x => x.PostAsync<PatientDto>("patients", request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await _patientService.CreatePatientAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("张三", result.Data.Name);
    }
}
```

## 🚀 性能优化

### 1. 数据绑定优化
```xml
<!-- 使用OneTime绑定减少更新开销 -->
<TextBlock Text="{Binding Title, Mode=OneTime}"/>

<!-- 对大数据集合使用虚拟化 -->
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         ScrollViewer.IsDeferredScrollingEnabled="True"
         ItemsSource="{Binding Patients}">

<!-- 使用UpdateSourceTrigger控制更新时机 -->
<TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"/>
```

### 2. 异步操作优化
```csharp
// 使用ConfigureAwait减少上下文切换
public async Task<PatientDto> GetPatientAsync(int id)
{
    try
    {
        IsBusy = true;
        var result = await _apiService.GetAsync<PatientDto>($"patients/{id}")
            .ConfigureAwait(false);
        
        if (result.Success)
        {
            return result.Data;
        }
        
        await ShowErrorAsync(result.Message);
        return null;
    }
    finally
    {
        IsBusy = false;
    }
}

// 使用CancellationToken取消长时间操作
public async Task LoadPatientsAsync(CancellationToken cancellationToken = default)
{
    try
    {
        IsBusy = true;
        var result = await _apiService.GetAsync<IEnumerable<PatientDto>>("patients", cancellationToken)
            .ConfigureAwait(false);
        
        if (result.Success)
        {
            Patients.Clear();
            foreach (var patient in result.Data)
            {
                Patients.Add(patient);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // 操作被取消
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 3. 内存管理
```csharp
// 实现IDisposable接口
public class PatientManagementViewModel : ViewModelBase, IDisposable
{
    private bool _disposed = false;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public PatientManagementViewModel(/* 依赖项 */)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        // 初始化逻辑
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 取消所有异步操作
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                
                // 清理集合
                Patients?.Clear();
                
                // 取消事件订阅
                // ...
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

## 🔒 安全实现

### 1. 令牌管理
```csharp
// Infrastructure/TokenService.cs
public class TokenService : ITokenService
{
    private readonly ISecureStorage _secureStorage;
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string UserKey = "current_user";

    public TokenService(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        return await _secureStorage.GetAsync(AccessTokenKey);
    }

    public async Task<string> GetRefreshTokenAsync()
    {
        return await _secureStorage.GetAsync(RefreshTokenKey);
    }

    public async Task<UserDto> GetCurrentUserAsync()
    {
        var userJson = await _secureStorage.GetAsync(UserKey);
        if (string.IsNullOrEmpty(userJson))
            return null;

        return JsonSerializer.Deserialize<UserDto>(userJson);
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken, UserDto user)
    {
        await _secureStorage.SetAsync(AccessTokenKey, accessToken);
        await _secureStorage.SetAsync(RefreshTokenKey, refreshToken);
        await _secureStorage.SetAsync(UserKey, JsonSerializer.Serialize(user));
    }

    public async Task ClearTokensAsync()
    {
        await _secureStorage.RemoveAsync(AccessTokenKey);
        await _secureStorage.RemoveAsync(RefreshTokenKey);
        await _secureStorage.RemoveAsync(UserKey);
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
            return true;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
```

### 2. 安全存储
```csharp
// Infrastructure/SecureStorageService.cs
public class SecureStorageService : ISecureStorage
{
    private readonly string _storagePath;

    public SecureStorageService()
    {
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LYBTClinic",
            "secure_storage.json");
        
        EnsureStorageDirectoryExists();
    }

    public async Task<string> GetAsync(string key)
    {
        try
        {
            if (!File.Exists(_storagePath))
                return null;

            var json = await File.ReadAllTextAsync(_storagePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            return data?.TryGetValue(key, out var value) == true ? 
                DecryptData(value) : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            Dictionary<string, string> data;
            
            if (File.Exists(_storagePath))
            {
                var json = await File.ReadAllTextAsync(_storagePath);
                data = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? 
                       new Dictionary<string, string>();
            }
            else
            {
                data = new Dictionary<string, string>();
            }

            data[key] = EncryptData(value);
            
            var jsonToWrite = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(_storagePath, jsonToWrite);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("无法保存数据到安全存储", ex);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            if (!File.Exists(_storagePath))
                return;

            var json = await File.ReadAllTextAsync(_storagePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (data?.ContainsKey(key) == true)
            {
                data.Remove(key);
                var jsonToWrite = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(_storagePath, jsonToWrite);
            }
        }
        catch
        {
            // 忽略删除错误
        }
    }

    private void EnsureStorageDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_storagePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private string EncryptData(string data)
    {
        // 简单的加密实现，生产环境应使用更安全的加密方式
        var bytes = Encoding.UTF8.GetBytes(data);
        return Convert.ToBase64String(bytes);
    }

    private string DecryptData(string encryptedData)
    {
        try
        {
            var bytes = Convert.FromBase64String(encryptedData);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
```

## 🔗 相关文档

- **[架构总览](../../architecture/README.md)** - 三层对齐架构设计原理
- **[Client端架构](../../architecture/client/README.md)** - WPF五层架构实现
- **[开发指南总览](../README.md)** - 开发规范和流程指导
- **[共享开发指南](../shared/README.md)** - 跨层组件开发指南
- **[UI设计指南](../shared/ui-design-guide.md)** - 用户界面设计规范

---

**文档维护**：前端开发组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核