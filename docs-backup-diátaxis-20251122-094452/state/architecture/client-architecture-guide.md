# Client端WPF架构指南

**基于凌隐宝堂中医诊所实际WPF客户端架构的完整指南** - 深入理解WPF五层架构设计与MVVM模式实现

## 🏗️ WPF五层架构概览

### 架构层次图
```
┌─────────────────────────────────────────────────────────────┐
│                         Shell Layer                         │
│                    (主窗口与导航管理)                         │
├─────────────────────────────────────────────────────────────┤
│                      Core_New Layer                         │
│                  (共享核心与基础设施)                        │
├─────────────────────────────────────────────────────────────┤
│                      Services Layer                         │
│                   (业务服务与数据访问)                        │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                      │
│              (数据访问与外部服务集成)                        │
├─────────────────────────────────────────────────────────────┤
│                      Modules Layer                          │
│                 (业务模块与用户界面)                         │
├─────────────────────────────────────────────────────────────┤
│                   Workstations Layer                        │
│                  (工作台与功能区域)                          │
└─────────────────────────────────────────────────────────────┘
```

### 实际项目结构
```
src/Client/Desktop/
├── LYBT.Desktop.Shell/                    # Shell Layer
│   ├── Views/                             # 主窗口和导航视图
│   ├── ViewModels/                        # Shell ViewModel
│   ├── Services/                          # Shell服务
│   └── Styles/                            # 全局样式和资源
├── LYBT.Desktop.Core_New/                 # Core_New Layer
│   ├── Common/                            # 通用组件和工具
│   ├── Converters/                        # 值转换器
│   ├── Controls/                          # 自定义控件
│   ├── Extensions/                        # 扩展方法
│   └── Themes/                            # 主题资源
├── LYBT.Desktop.Services/                 # Services Layer
│   ├── Interfaces/                        # 服务接口
│   ├── Implementations/                   # 服务实现
│   └── Http/                              # HTTP客户端
├── LYBT.Desktop.Infrastructure/           # Infrastructure Layer
│   ├── Data/                              # 数据访问
│   ├── Configuration/                     # 配置管理
│   └── Security/                          # 安全相关
├── Modules/                               # Modules Layer
│   ├── LYBT.Desktop.Auth/                 # 认证模块
│   ├── LYBT.Desktop.Users/                # 用户管理模块
│   ├── LYBT.Desktop.Patients/             # 患者管理模块
│   ├── LYBT.Desktop.MedicalCase/          # 医案管理模块
│   ├── LYBT.Desktop.Consultation/         # 诊疗记录模块
│   ├── LYBT.Desktop.Prescriptions/        # 处方管理模块
│   ├── LYBT.Desktop.Herbs/                # 药材管理模块
│   └── LYBT.Desktop.Formula/              # 验方管理模块
└── Workstations/                          # Workstations Layer
    ├── LYBT.Desktop.Workstation.Patient/  # 患者管理工作台
    ├── LYBT.Desktop.Workstation.Doctor/   # 医生工作台
    └── LYBT.Desktop.Workstation.Admin/    # 管理员工作台
```

## 🐚 Shell Layer (外壳层)

### 1. 主窗口设计

#### 主窗口结构模板
```xml
<!-- MainWindow.xaml -->
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:prism="http://prismlibrary.com/"
        mc:Ignorable="d"
        Title="凌隐宝堂中医诊所管理系统" 
        Height="800" Width="1200"
        WindowStartupLocation="CenterScreen"
        WindowState="Maximized">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- 标题栏 -->
        <Border Grid.Row="0" Background="{DynamicResource PrimaryBrush}" 
                Height="60" Padding="20,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <!-- 系统标题 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal" 
                            VerticalAlignment="Center">
                    <Image Source="/LYBT.Desktop.Shell;component/Resources/logo.png" 
                           Width="32" Height="32" Margin="0,0,10,0"/>
                    <TextBlock Text="凌隐宝堂中医诊所管理系统" 
                               FontSize="18" FontWeight="Bold" 
                               Foreground="White" VerticalAlignment="Center"/>
                </StackPanel>
                
                <!-- 用户信息 -->
                <StackPanel Grid.Column="2" Orientation="Horizontal" 
                            VerticalAlignment="Center">
                    <TextBlock Text="{Binding CurrentUser.UserName}" 
                               Foreground="White" Margin="0,0,15,0"/>
                    <TextBlock Text="{Binding CurrentUser.RoleName}" 
                               Foreground="White" Margin="0,0,15,0"/>
                    <Button Content="退出" Command="{Binding LogoutCommand}" 
                            Style="{DynamicResource AccentButtonStyle}" 
                            Padding="15,5"/>
                </StackPanel>
            </Grid>
        </Border>
        
        <!-- 主内容区域 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- 导航菜单 -->
            <Border Grid.Column="0" Background="{DynamicResource BackgroundBrush}" 
                    BorderBrush="{DynamicResource BorderBrush}" BorderThickness="0,0,1,0">
                <ScrollViewer>
                    <ItemsControl ItemsSource="{Binding NavigationItems}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Button Style="{DynamicResource NavigationButtonStyle}"
                                        Command="{Binding DataContext.NavigateCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                                        CommandParameter="{Binding NavigationPath}">
                                    <StackPanel Orientation="Horizontal" Margin="15,10">
                                        <Image Source="{Binding IconPath}" Width="20" Height="20" 
                                               Margin="0,0,10,0"/>
                                        <TextBlock Text="{Binding DisplayName}" 
                                                   VerticalAlignment="Center"/>
                                    </StackPanel>
                                </Button>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>
            </Border>
            
            <!-- 内容区域 -->
            <Border Grid.Column="1" Background="{DynamicResource ContentBackgroundBrush}">
                <ContentControl prism:RegionManager.RegionName="MainContentRegion"/>
            </Border>
        </Grid>
        
        <!-- 状态栏 -->
        <StatusBar Grid.Row="2" Background="{DynamicResource StatusBarBrush}">
            <StatusBarItem>
                <TextBlock Text="{Binding StatusMessage}"/>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <TextBlock Text="{Binding CurrentTime, StringFormat='yyyy-MM-dd HH:mm:ss'}"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

#### 主窗口ViewModel
```csharp
/// <summary>
/// 主窗口ViewModel
/// </summary>
public class MainWindowViewModel : BindableBase, IDisposable
{
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<MainWindowViewModel> _logger;

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    private UserDto? _currentUser;
    public UserDto? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    private string _statusMessage = "系统就绪";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private DateTime _currentTime;
    public DateTime CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IAuthenticationService authService,
        ILogger<MainWindowViewModel> logger)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
        _authService = authService;
        _logger = logger;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>();
        NavigateCommand = new DelegateCommand<string>(ExecuteNavigate);
        LogoutCommand = new DelegateCommand(ExecuteLogout);

        InitializeNavigation();
        InitializeTimer();
        SubscribeEvents();
    }

    /// <summary>
    /// 初始化导航菜单
    /// </summary>
    private void InitializeNavigation()
    {
        var currentUser = _authService.GetCurrentUser();
        if (currentUser == null) return;

        // 根据用户角色动态生成导航菜单
        var navigationItems = new List<NavigationItemViewModel>
        {
            new() { DisplayName = "仪表板", NavigationPath = "DashboardView", IconPath = "/Resources/dashboard.png" },
            new() { DisplayName = "患者管理", NavigationPath = "PatientManagementView", IconPath = "/Resources/patients.png" },
            new() { DisplayName = "医案管理", NavigationPath = "MedicalCaseManagementView", IconPath = "/Resources/medicalcase.png" },
            new() { DisplayName = "处方管理", NavigationPath = "PrescriptionManagementView", IconPath = "/Resources/prescription.png" }
        };

        // 管理员专用菜单
        if (currentUser.Role == UserRole.Admin)
        {
            navigationItems.AddRange(new[]
            {
                new() { DisplayName = "用户管理", NavigationPath = "UserManagementView", IconPath = "/Resources/users.png" },
                new() { DisplayName = "药材管理", NavigationPath = "HerbManagementView", IconPath = "/Resources/herbs.png" },
                new() { DisplayName = "验方管理", NavigationPath = "FormulaManagementView", IconPath = "/Resources/formula.png" }
            });
        }

        foreach (var item in navigationItems)
        {
            NavigationItems.Add(item);
        }
    }

    /// <summary>
    /// 执行导航
    /// </summary>
    private void ExecuteNavigate(string? navigationPath)
    {
        if (string.IsNullOrEmpty(navigationPath)) return;

        try
        {
            _regionManager.RequestNavigate("MainContentRegion", navigationPath);
            _logger.LogInformation("导航到页面: {NavigationPath}", navigationPath);
            StatusMessage = $"当前页面: {NavigationItems.FirstOrDefault(x => x.NavigationPath == navigationPath)?.DisplayName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航失败: {NavigationPath}", navigationPath);
            StatusMessage = "导航失败";
        }
    }

    /// <summary>
    /// 执行登出
    /// </summary>
    private async void ExecuteLogout()
    {
        try
        {
            await _authService.LogoutAsync();
            _logger.LogInformation("用户登出成功");
            
            // 导航到登录页面
            _regionManager.RequestNavigate("MainContentRegion", "LoginView");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            StatusMessage = "登出失败";
        }
    }

    /// <summary>
    /// 初始化定时器
    /// </summary>
    private void InitializeTimer()
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        timer.Start();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeEvents()
    {
        // 订阅用户登录事件
        _eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
        
        // 订阅状态消息事件
        _eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusMessage);
    }

    private void OnUserLoggedIn(UserDto user)
    {
        CurrentUser = user;
        InitializeNavigation();
        ExecuteNavigate("DashboardView");
    }

    private void OnStatusMessage(string message)
    {
        StatusMessage = message;
    }

    public void Dispose()
    {
        // 清理资源
    }
}
```

### 2. 导航管理

#### Prism导航配置
```csharp
/// <summary>
/// 应用程序引导程序
/// </summary>
public class Bootstrapper : PrismBootstrapper
{
    protected override DependencyObject CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();
        containerRegistry.RegisterSingleton<IPatientService, PatientService>();
        containerRegistry.RegisterSingleton<IMedicalCaseService, MedicalCaseService>();
        
        // 注册ViewModels
        containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
        containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
        containerRegistry.RegisterForNavigation<MedicalCaseManagementView, MedicalCaseManagementViewModel>();
        
        // 注册HTTP客户端
        containerRegistry.RegisterInstance<IHttpClientFactory>(new HttpClientFactory());
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 注册模块
        moduleCatalog.AddModule<AuthModule>();
        moduleCatalog.AddModule<PatientModule>();
        moduleCatalog.AddModule<MedicalCaseModule>();
    }

    protected override void InitializeShell()
    {
        Application.Current.MainWindow.Show();
    }
}
```

## 🎯 Core_New Layer (核心层)

### 1. MVVM基础设施

#### BindableBase基类
```csharp
/// <summary>
/// ViewModel基类
/// </summary>
public abstract class BindableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### AsyncRelayCommand实现
```csharp
/// <summary>
/// 异步命令实现
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Action<Exception>? _onException;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null, Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
            // 或者记录日志
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
```

### 2. 通用控件和转换器

#### 自定义控件模板
```xml
<!-- LoadingControl.xaml -->
<UserControl x:Class="LYBT.Desktop.Core_New.Controls.LoadingControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d" 
             d:DesignHeight="100" d:DesignWidth="100">
    
    <Grid Background="#80000000" Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}">
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
            <ProgressBar IsIndeterminate="True" Width="50" Height="50" 
                         Style="{DynamicResource MaterialDesignCircularProgressBar}"/>
            <TextBlock Text="{Binding LoadingMessage}" HorizontalAlignment="Center" 
                       Foreground="White" Margin="0,10,0,0"/>
        </StackPanel>
    </Grid>
</UserControl>
```

```csharp
/// <summary>
/// 加载控件ViewModel
/// </summary>
public class LoadingControlViewModel : BindableBase
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _loadingMessage = "正在加载...";
    public string LoadingMessage
    {
        get => _loadingMessage;
        set => SetProperty(ref _loadingMessage, value);
    }

    public void Show(string message = "正在加载...")
    {
        IsLoading = true;
        LoadingMessage = message;
    }

    public void Hide()
    {
        IsLoading = false;
    }
}
```

#### 值转换器集合
```csharp
/// <summary>
/// 布尔值到可见性转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            bool invert = parameter?.ToString() == "Invert";
            bool result = invert ? !boolValue : boolValue;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool invert = parameter?.ToString() == "Invert";
            bool result = visibility == Visibility.Visible;
            return invert ? !result : result;
        }
        return false;
    }
}

/// <summary>
/// 枚举到描述转换器
/// </summary>
public class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name == null) return string.Empty;

        FieldInfo field = type.GetField(name);
        if (field == null) return string.Empty;

        DescriptionAttribute? attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 日期格式转换器
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            string format = parameter?.ToString() ?? "yyyy-MM-dd HH:mm:ss";
            return dateTime.ToString(format);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (DateTime.TryParse(value?.ToString(), out DateTime result))
        {
            return result;
        }
        return DateTime.MinValue;
    }
}
```

### 3. 主题和样式

#### 主题资源字典
```xml
<!-- Themes/LightTheme.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:colors="clr-namespace:LYBT.Desktop.Core_New.Colors">

    <!-- 主色调 -->
    <Color x:Key="PrimaryColor">#2196F3</Color>
    <Color x:Key="PrimaryDarkColor">#1976D2</Color>
    <Color x:Key="PrimaryLightColor">#BBDEFB</Color>
    <Color x:Key="AccentColor">#FF4081</Color>

    <!-- 背景色 -->
    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <Color x:Key="CardColor">#FFFFFF</Color>

    <!-- 文字色 -->
    <Color x:Key="PrimaryTextColor">#212121</Color>
    <Color x:Key="SecondaryTextColor">#757575</Color>
    <Color x:Key="HintTextColor">#BDBDBD</Color>

    <!-- 边框色 -->
    <Color x:Key="BorderColor">#E0E0E0</Color>
    <Color x:Key="DividerColor">#EEEEEE</Color>

    <!-- 画刷 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="PrimaryDarkBrush" Color="{StaticResource PrimaryDarkColor}"/>
    <SolidColorBrush x:Key="PrimaryLightBrush" Color="{StaticResource PrimaryLightColor}"/>
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
    <SolidColorBrush x:Key="CardBrush" Color="{StaticResource CardColor}"/>
    <SolidColorBrush x:Key="PrimaryTextBrush" Color="{StaticResource PrimaryTextColor}"/>
    <SolidColorBrush x:Key="SecondaryTextBrush" Color="{StaticResource SecondaryTextColor}"/>
    <SolidColorBrush x:Key="HintTextBrush" Color="{StaticResource HintTextColor}"/>
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}"/>
    <SolidColorBrush x:Key="DividerBrush" Color="{StaticResource DividerColor}"/>

    <!-- 按钮样式 -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" 
                                        VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryDarkBrush}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="{StaticResource PrimaryLightBrush}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 卡片样式 -->
    <Style x:Key="CardStyle" TargetType="Border">
        <Setter Property="Background" Value="{StaticResource CardBrush}"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="Padding" Value="16"/>
        <Setter Property="Margin" Value="8"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect Color="Black" Opacity="0.1" ShadowDepth="2" BlurRadius="8"/>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>
```

## 🔧 Services Layer (服务层)

### 1. HTTP服务封装

#### 通用HTTP客户端
```csharp
/// <summary>
/// 通用HTTP客户端服务
/// </summary>
public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object data);
    Task<T?> PutAsync<T>(string endpoint, object data);
    Task<bool> DeleteAsync(string endpoint);
    Task<T?> PostFileAsync<T>(string endpoint, Stream fileStream, string fileName);
}

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly IAuthenticationService _authService;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger,
        IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;

        ConfigureHttpClient();
    }

    /// <summary>
    /// 配置HTTP客户端
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(AppSettings.Current.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LYBT.Desktop");
        
        // 设置认证头
        var token = _authService.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            _logger.LogDebug("发送GET请求: {Endpoint}", endpoint);
            
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogDebug("GET请求成功: {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET请求失败: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            _logger.LogDebug("发送POST请求: {Endpoint}", endpoint);
            
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogDebug("POST请求成功: {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST请求失败: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            _logger.LogDebug("发送PUT请求: {Endpoint}", endpoint);
            
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogDebug("PUT请求成功: {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT请求失败: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            _logger.LogDebug("发送DELETE请求: {Endpoint}", endpoint);
            
            var response = await _httpClient.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            _logger.LogDebug("DELETE请求成功: {Endpoint}", endpoint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE请求失败: {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<T?> PostFileAsync<T>(string endpoint, Stream fileStream, string fileName)
    {
        try
        {
            _logger.LogDebug("发送文件上传请求: {Endpoint}", endpoint);
            
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentDisposition = 
                new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = fileName
                };
            
            content.Add(fileContent);
            
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogDebug("文件上传成功: {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败: {Endpoint}", endpoint);
            return default;
        }
    }
}
```

### 2. 业务服务接口

#### 患者服务接口和实现
```csharp
/// <summary>
/// 患者服务接口
/// </summary>
public interface IPatientService
{
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page, int pageSize, string? keyword = null);
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
    Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null);
    MemoryStream GenerateImportTemplate();
}

/// <summary>
/// 患者服务实现
/// </summary>
public class PatientService : IPatientService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<PatientService> _logger;
    private readonly IEventAggregator _eventAggregator;

    public PatientService(
        IHttpClientService httpClientService,
        ILogger<PatientService> logger,
        IEventAggregator eventAggregator)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _eventAggregator = eventAggregator;
    }

    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            var endpoint = $"api/patients?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                endpoint += $"&keyword={Uri.EscapeDataString(keyword)}";
            }

            var result = await _httpClientService.GetAsync<ServiceResult<PagedResult<PatientDto>>>(endpoint);
            return result ?? ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
    }

    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var result = await _httpClientService.GetAsync<ServiceResult<PatientDto>>($"api/patients/{id}");
            return result ?? ServiceResult<PatientDto>.Failure("获取患者详情失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败，ID: {PatientId}", id);
            return ServiceResult<PatientDto>.Failure("获取患者详情失败");
        }
    }

    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<ServiceResult<PatientDto>>("api/patients", dto);
            
            if (result != null && result.IsSuccess)
            {
                // 发布患者创建事件
                _eventAggregator.GetEvent<PatientCreatedEvent>().Publish(result.Data!);
                _logger.LogInformation("患者创建成功，ID: {PatientId}", result.Data!.Id);
            }
            
            return result ?? ServiceResult<PatientDto>.Failure("创建患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            return ServiceResult<PatientDto>.Failure("创建患者失败");
        }
    }

    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<ServiceResult<PatientDto>>($"api/patients/{id}", dto);
            
            if (result != null && result.IsSuccess)
            {
                // 发布患者更新事件
                _eventAggregator.GetEvent<PatientUpdatedEvent>().Publish(result.Data!);
                _logger.LogInformation("患者更新成功，ID: {PatientId}", id);
            }
            
            return result ?? ServiceResult<PatientDto>.Failure("更新患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败，ID: {PatientId}", id);
            return ServiceResult<PatientDto>.Failure("更新患者失败");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var result = await _httpClientService.DeleteAsync($"api/patients/{id}");
            
            if (result)
            {
                // 发布患者删除事件
                _eventAggregator.GetEvent<PatientDeletedEvent>().Publish(id);
                _logger.LogInformation("患者删除成功，ID: {PatientId}", id);
            }
            
            return result ? ServiceResult.Success() : ServiceResult.Failure("删除患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败，ID: {PatientId}", id);
            return ServiceResult.Failure("删除患者失败");
        }
    }

    public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
    {
        try
        {
            var endpoint = $"api/patients/search?keyword={Uri.EscapeDataString(keyword)}";
            var result = await _httpClientService.GetAsync<ServiceResult<List<PatientDto>>>(endpoint);
            return result ?? ServiceResult<List<PatientDto>>.Failure("搜索患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索患者失败，关键字: {Keyword}", keyword);
            return ServiceResult<List<PatientDto>>.Failure("搜索患者失败");
        }
    }

    public async Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
    {
        try
        {
            var result = await _httpClientService.PostFileAsync<ServiceResult<ImportResultDto<PatientDto>>>(
                "api/patients/import", stream, fileName ?? "patients.xlsx");
            
            if (result != null && result.IsSuccess)
            {
                _logger.LogInformation("患者导入成功，文件: {FileName}", fileName);
            }
            
            return result ?? ServiceResult<ImportResultDto<PatientDto>>.Failure("导入患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入患者失败，文件: {FileName}", fileName);
            return ServiceResult<ImportResultDto<PatientDto>>.Failure("导入患者失败");
        }
    }

    public MemoryStream GenerateImportTemplate()
    {
        try
        {
            // 调用API获取模板
            var templateStream = new MemoryStream();
            // TODO: 实现模板下载逻辑
            return templateStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成患者导入模板失败");
            throw;
        }
    }
}
```

## 🧩 Modules Layer (模块层)

### 1. 模块结构模板

#### 患者管理模块结构
```
LYBT.Desktop.Patients/
├── Views/                              # 视图
│   ├── PatientManagementView.xaml      # 患者管理主视图
│   ├── PatientDetailView.xaml          # 患者详情视图
│   ├── PatientCreateView.xaml          # 患者创建视图
│   └── PatientEditView.xaml            # 患者编辑视图
├── ViewModels/                         # 视图模型
│   ├── PatientManagementViewModel.cs   # 患者管理主ViewModel
│   ├── PatientDetailViewModel.cs       # 患者详情ViewModel
│   ├── PatientCreateViewModel.cs       # 患者创建ViewModel
│   └── PatientEditViewModel.cs         # 患者编辑ViewModel
├── Models/                             # 模型
│   ├── PatientModel.cs                 # 患者模型
│   ├── PatientFilterModel.cs           # 筛选模型
│   └── PatientStatisticsModel.cs       # 统计模型
├── Converters/                         # 转换器
│   ├── GenderToTextConverter.cs        # 性别转换器
│   └── AgeCalculatorConverter.cs       # 年龄计算转换器
├── Commands/                           # 命令
│   ├── SavePatientCommand.cs           # 保存患者命令
│   └── DeletePatientCommand.cs         # 删除患者命令
├── Validators/                         # 验证器
│   └── PatientValidator.cs             # 患者验证器
├── Services/                           # 模块服务
│   └── IPatientNavigationService.cs    # 患者导航服务
├── Resources/                          # 资源
│   ├── Styles/                         # 样式
│   ├── Templates/                      # 模板
│   └── Images/                         # 图片
└── PatientsModule.cs                   # 模块注册类
```

### 2. 模块视图实现

#### 患者管理主视图
```xml
<!-- PatientManagementView.xaml -->
<UserControl x:Class="LYBT.Desktop.Patients.Views.PatientManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:prism="http://prismlibrary.com/"
             xmlns:controls="clr-namespace:LYBT.Desktop.Core_New.Controls;assembly=LYBT.Desktop.Core_New"
             mc:Ignorable="d" 
             d:DesignHeight="600" d:DesignWidth="800"
             prism:ViewModelLocator.AutoWireViewModel="True">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Border Grid.Row="0" Background="{DynamicResource PrimaryBrush}" 
                Padding="20" Margin="8">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="患者管理" FontSize="24" FontWeight="Bold" 
                           Foreground="White" VerticalAlignment="Center"/>
                <TextBlock Text="{Binding TotalCount, StringFormat='共 {0} 条记录'}" 
                           Foreground="White" VerticalAlignment="Center" 
                           Margin="20,0,0,0" Opacity="0.8"/>
            </StackPanel>
        </Border>

        <!-- 搜索和操作栏 -->
        <Border Grid.Row="1" Style="{DynamicResource CardStyle}" Margin="8,4,8,4">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- 搜索区域 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}" 
                             Width="300" Height="35" 
                             Style="{DynamicResource MaterialDesignTextBox}"
                             materialDesign:HintAssist.Hint="搜索患者姓名或电话"/>
                    <Button Content="搜索" Command="{Binding SearchCommand}" 
                            Style="{DynamicResource PrimaryButtonStyle}" 
                            Margin="10,0,0,0" Padding="20,8"/>
                    <Button Content="重置" Command="{Binding ResetSearchCommand}" 
                            Style="{DynamicResource SecondaryButtonStyle}" 
                            Margin="5,0,0,0" Padding="20,8"/>
                </StackPanel>

                <!-- 操作按钮 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                    <Button Content="导入Excel" Command="{Binding ImportExcelCommand}" 
                            Style="{DynamicResource AccentButtonStyle}" 
                            Padding="20,8"/>
                    <Button Content="导出Excel" Command="{Binding ExportExcelCommand}" 
                            Style="{DynamicResource PrimaryButtonStyle}" 
                            Margin="5,0,0,0" Padding="20,8"/>
                    <Button Content="新增患者" Command="{Binding CreatePatientCommand}" 
                            Style="{DynamicResource PrimaryButtonStyle}" 
                            Margin="5,0,0,0" Padding="20,8"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 数据表格 -->
        <Border Grid.Row="2" Style="{DynamicResource CardStyle}" Margin="8,4,8,4">
            <DataGrid ItemsSource="{Binding Patients}" 
                      SelectedItem="{Binding SelectedPatient}"
                      AutoGenerateColumns="False" 
                      CanUserSortColumns="True" 
                      CanUserAddRows="False"
                      GridLinesVisibility="Horizontal"
                      HeadersVisibility="Column"
                      SelectionMode="Single"
                      IsReadOnly="True"
                      AlternatingRowBackground="#FAFAFA">
                
                <DataGrid.Columns>
                    <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="120"/>
                    <DataGridTextColumn Header="性别" Binding="{Binding Gender, Converter={StaticResource GenderToTextConverter}}" Width="80"/>
                    <DataGridTextColumn Header="年龄" Binding="{Binding BirthDate, Converter={StaticResource AgeCalculatorConverter}}" Width="80"/>
                    <DataGridTextColumn Header="联系电话" Binding="{Binding PhoneNumber}" Width="120"/>
                    <DataGridTextColumn Header="身份证号" Binding="{Binding IdNumber}" Width="180"/>
                    <DataGridTextColumn Header="地址" Binding="{Binding Address}" Width="200"/>
                    <DataGridTextColumn Header="状态" Binding="{Binding Status, Converter={StaticResource EnumToDescriptionConverter}}" Width="100"/>
                    <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd HH:mm'}" Width="150"/>
                    
                    <DataGridTemplateColumn Header="操作" Width="120">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal">
                                    <Button Content="查看" Command="{Binding DataContext.ViewPatientCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" 
                                            CommandParameter="{Binding}" 
                                            Style="{DynamicResource SmallButtonStyle}" 
                                            Margin="2,0"/>
                                    <Button Content="编辑" Command="{Binding DataContext.EditPatientCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" 
                                            CommandParameter="{Binding}" 
                                            Style="{DynamicResource SmallButtonStyle}" 
                                            Margin="2,0"/>
                                    <Button Content="删除" Command="{Binding DataContext.DeletePatientCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" 
                                            CommandParameter="{Binding}" 
                                            Style="{DynamicResource DangerButtonStyle}" 
                                            Margin="2,0"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>

        <!-- 分页控件 -->
        <Border Grid.Row="3" Style="{DynamicResource CardStyle}" Margin="8,4,8,8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- 分页信息 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock Text="{Binding CurrentPage, StringFormat='第 {0} 页'}" Margin="10,0"/>
                    <TextBlock Text="/"/>
                    <TextBlock Text="{Binding TotalPages}"/>
                    <TextBlock Text="{Binding TotalCount, StringFormat='，共 {0} 条记录'}" Margin="10,0"/>
                </StackPanel>

                <!-- 分页按钮 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                    <Button Content="首页" Command="{Binding FirstPageCommand}" 
                            Style="{DynamicResource SmallButtonStyle}" Margin="2,0"/>
                    <Button Content="上一页" Command="{Binding PreviousPageCommand}" 
                            Style="{DynamicResource SmallButtonStyle}" Margin="2,0"/>
                    <Button Content="下一页" Command="{Binding NextPageCommand}" 
                            Style="{DynamicResource SmallButtonStyle}" Margin="2,0"/>
                    <Button Content="末页" Command="{Binding LastPageCommand}" 
                            Style="{DynamicResource SmallButtonStyle}" Margin="2,0"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 加载遮罩 -->
        <controls:LoadingControl Grid.RowSpan="4" DataContext="{Binding LoadingControl}"/>
    </Grid>
</UserControl>
```

#### 患者管理ViewModel
```csharp
/// <summary>
/// 患者管理ViewModel
/// </summary>
public class PatientManagementViewModel : BindableBase, IDisposable
{
    private readonly IPatientService _patientService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<PatientManagementViewModel> _logger;

    public ObservableCollection<PatientDto> Patients { get; }
    public LoadingControlViewModel LoadingControl { get; }

    // 命令
    public ICommand SearchCommand { get; }
    public ICommand ResetSearchCommand { get; }
    public ICommand CreatePatientCommand { get; }
    public ICommand ViewPatientCommand { get; }
    public ICommand EditPatientCommand { get; }
    public ICommand DeletePatientCommand { get; }
    public ICommand ImportExcelCommand { get; }
    public ICommand ExportExcelCommand { get; }

    // 分页命令
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    // 属性
    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                // 延迟搜索
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }
    }

    private PatientDto? _selectedPatient;
    public PatientDto? SelectedPatient
    {
        get => _selectedPatient;
        set => SetProperty(ref _selectedPatient, value);
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    private int _totalPages;
    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private readonly DispatcherTimer _searchTimer;

    public PatientManagementViewModel(
        IPatientService patientService,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        ILogger<PatientManagementViewModel> logger)
    {
        _patientService = patientService;
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        _logger = logger;

        Patients = new ObservableCollection<PatientDto>();
        LoadingControl = new LoadingControlViewModel();

        // 初始化命令
        SearchCommand = new AsyncRelayCommand(ExecuteSearch);
        ResetSearchCommand = new DelegateCommand(ExecuteResetSearch);
        CreatePatientCommand = new DelegateCommand(ExecuteCreatePatient);
        ViewPatientCommand = new DelegateCommand<PatientDto>(ExecuteViewPatient);
        EditPatientCommand = new DelegateCommand<PatientDto>(ExecuteEditPatient);
        DeletePatientCommand = new AsyncRelayCommand<PatientDto>(ExecuteDeletePatient);
        ImportExcelCommand = new AsyncRelayCommand(ExecuteImportExcel);
        ExportExcelCommand = new AsyncRelayCommand(ExecuteExportExcel);

        FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanGoToFirstPage);
        PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanGoToPreviousPage);
        NextPageCommand = new DelegateCommand(ExecuteNextPage, CanGoToNextPage);
        LastPageCommand = new DelegateCommand(ExecuteLastPage, CanGoToLastPage);

        // 初始化搜索定时器
        _searchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _searchTimer.Tick += OnSearchTimerTick;

        // 订阅事件
        SubscribeEvents();

        // 加载数据
        _ = LoadDataAsync();
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            LoadingControl.Show("正在加载患者数据...");
            
            var result = await _patientService.GetPagedAsync(CurrentPage, PageSize, SearchKeyword);
            if (result.IsSuccess)
            {
                Patients.Clear();
                foreach (var patient in result.Data!.Items)
                {
                    Patients.Add(patient);
                }

                CurrentPage = result.Data.CurrentPage;
                TotalPages = result.Data.TotalPages;
                TotalCount = result.Data.TotalCount;
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.Message ?? "加载失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载患者数据失败");
            await _dialogService.ShowErrorAsync("加载患者数据失败");
        }
        finally
        {
            LoadingControl.Hide();
        }
    }

    /// <summary>
    /// 搜索
    /// </summary>
    private async Task ExecuteSearch()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>
    /// 重置搜索
    /// </summary>
    private void ExecuteResetSearch()
    {
        SearchKeyword = string.Empty;
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    /// <summary>
    /// 创建患者
    /// </summary>
    private void ExecuteCreatePatient()
    {
        _eventAggregator.GetEvent<NavigateToPatientCreateEvent>().Publish();
    }

    /// <summary>
    /// 查看患者
    /// </summary>
    private void ExecuteViewPatient(PatientDto? patient)
    {
        if (patient != null)
        {
            _eventAggregator.GetEvent<NavigateToPatientDetailEvent>().Publish(patient.Id);
        }
    }

    /// <summary>
    /// 编辑患者
    /// </summary>
    private void ExecuteEditPatient(PatientDto? patient)
    {
        if (patient != null)
        {
            _eventAggregator.GetEvent<NavigateToPatientEditEvent>().Publish(patient.Id);
        }
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    private async Task ExecuteDeletePatient(PatientDto? patient)
    {
        if (patient == null) return;

        var result = await _dialogService.ShowConfirmAsync(
            $"确定要删除患者 {patient.Name} 吗？", "确认删除");
        
        if (result == true)
        {
            try
            {
                LoadingControl.Show("正在删除患者...");
                
                var deleteResult = await _patientService.DeleteAsync(patient.Id);
                if (deleteResult.IsSuccess)
                {
                    await _dialogService.ShowInfoAsync("删除成功");
                    await LoadDataAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync(deleteResult.Message ?? "删除失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败，ID: {PatientId}", patient.Id);
                await _dialogService.ShowErrorAsync("删除患者失败");
            }
            finally
            {
                LoadingControl.Hide();
            }
        }
    }

    /// <summary>
    /// 导入Excel
    /// </summary>
    private async Task ExecuteImportExcel()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择Excel文件",
            Filter = "Excel文件|*.xlsx;*.xls"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                LoadingControl.Show("正在导入Excel文件...");
                
                using var stream = File.OpenRead(dialog.FileName);
                var result = await _patientService.ImportFromExcelAsync(stream, Path.GetFileName(dialog.FileName));
                
                if (result.IsSuccess)
                {
                    await _dialogService.ShowInfoAsync(
                        $"导入完成：成功 {result.Data!.SuccessCount} 条，失败 {result.Data.FailureCount} 条");
                    await LoadDataAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.Message ?? "导入失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入Excel失败，文件: {FileName}", dialog.FileName);
                await _dialogService.ShowErrorAsync("导入Excel失败");
            }
            finally
            {
                LoadingControl.Hide();
            }
        }
    }

    /// <summary>
    /// 导出Excel
    /// </summary>
    private async Task ExecuteExportExcel()
    {
        try
        {
            LoadingControl.Show("正在导出Excel文件...");
            
            var dialog = new SaveFileDialog
            {
                Title = "保存Excel文件",
                Filter = "Excel文件|*.xlsx",
                FileName = $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                // TODO: 实现导出逻辑
                await _dialogService.ShowInfoAsync("导出功能正在开发中");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出Excel失败");
            await _dialogService.ShowErrorAsync("导出Excel失败");
        }
        finally
        {
            LoadingControl.Hide();
        }
    }

    // 分页方法
    private void ExecuteFirstPage()
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    private void ExecutePreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            _ = LoadDataAsync();
        }
    }

    private void ExecuteNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            _ = LoadDataAsync();
        }
    }

    private void ExecuteLastPage()
    {
        CurrentPage = TotalPages;
        _ = LoadDataAsync();
    }

    // 分页条件
    private bool CanGoToFirstPage() => CurrentPage > 1;
    private bool CanGoToPreviousPage() => CurrentPage > 1;
    private bool CanGoToNextPage() => CurrentPage < TotalPages;
    private bool CanGoToLastPage() => CurrentPage < TotalPages;

    /// <summary>
    /// 搜索定时器触发
    /// </summary>
    private void OnSearchTimerTick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        _ = LoadDataAsync();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<PatientCreatedEvent>().Subscribe(OnPatientCreated);
        _eventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(OnPatientUpdated);
        _eventAggregator.GetEvent<PatientDeletedEvent>().Subscribe(OnPatientDeleted);
    }

    private void OnPatientCreated(PatientDto patient)
    {
        Patients.Insert(0, patient);
        TotalCount++;
    }

    private void OnPatientUpdated(PatientDto patient)
    {
        var index = Patients.ToList().FindIndex(p => p.Id == patient.Id);
        if (index >= 0)
        {
            Patients[index] = patient;
        }
    }

    private void OnPatientDeleted(Guid patientId)
    {
        var patient = Patients.FirstOrDefault(p => p.Id == patientId);
        if (patient != null)
        {
            Patients.Remove(patient);
            TotalCount--;
        }
    }

    public void Dispose()
    {
        _searchTimer?.Stop();
        // 取消事件订阅
    }
}
```

### 3. 模块注册

#### 患者模块注册类
```csharp
/// <summary>
/// 患者模块
/// </summary>
public class PatientModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();
        
        // 注册视图到区域
        regionManager.RegisterViewWithRegion<MainContentRegion>(typeof(PatientManagementView));
        regionManager.RegisterViewWithRegion<DetailContentRegion>(typeof(PatientDetailView));
        regionManager.RegisterViewWithRegion<FormContentRegion>(typeof(PatientCreateView));
        regionManager.RegisterViewWithRegion<FormContentRegion>(typeof(PatientEditView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IPatientService, PatientService>();
        
        // 注册ViewModels
        containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
        containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
        containerRegistry.RegisterForNavigation<PatientCreateView, PatientCreateViewModel>();
        containerRegistry.RegisterForNavigation<PatientEditView, PatientEditViewModel>();
        
        // 注册对话框服务
        containerRegistry.Register<IDialogService, DialogService>();
    }
}
```

## 🎨 MVVM最佳实践

### 1. 数据绑定优化

#### 命令绑定最佳实践
```xml
<!-- 推荐做法：使用CommandParameter传递数据 -->
<Button Content="删除" 
        Command="{Binding DeleteCommand}"
        CommandParameter="{Binding SelectedItem}"/>

<!-- 推荐做法：使用RelativeSource访问父级ViewModel -->
<Button Content="刷新" 
        Command="{Binding DataContext.RefreshCommand, RelativeSource={RelativeSource AncestorType=Window}}"/>

<!-- 避免做法：在View中包含业务逻辑 -->
<Button Content="保存" Click="Button_Save_Click"/>
```

#### 属性绑定优化
```xml
<!-- 推荐做法：使用UpdateSourceTrigger控制更新时机 -->
<TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"/>

<!-- 推荐做法：使用转换器处理数据格式 -->
<TextBlock Text="{Binding BirthDate, Converter={StaticResource DateFormatConverter}, ConverterParameter='yyyy-MM-dd'}"/>

<!-- 推荐做法：使用StringFormat处理简单格式 -->
<TextBlock Text="{Binding TotalCount, StringFormat='共 {0} 条记录'}"/>
```

### 2. 性能优化指南

#### 集合优化
```csharp
// 推荐做法：使用ObservableCollection进行数据绑定
public ObservableCollection<PatientDto> Patients { get; }

// 批量更新时禁用通知
private void UpdatePatients(List<PatientDto> newPatients)
{
    Patients.Clear();
    foreach (var patient in newPatients)
    {
        Patients.Add(patient);
    }
}

// 大量数据时使用分页加载
private async Task LoadPagedDataAsync(int page, int pageSize)
{
    var result = await _patientService.GetPagedAsync(page, pageSize);
    // 只加载当前页的数据
}
```

#### 内存管理
```csharp
// 探查式模式：及时释放资源
public class PatientManagementViewModel : BindableBase, IDisposable
{
    private readonly IDisposable _subscription;

    public PatientManagementViewModel()
    {
        _subscription = _eventAggregator.GetEvent<PatientUpdatedEvent>()
            .Subscribe(OnPatientUpdated);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

---

## 📚 架构合规检查

### ✅ WPF架构检查清单
- [ ] **MVVM模式检查**
  - [ ] View和ViewModel分离
  - [ ] 数据绑定正确使用
  - [ ] 命令模式正确实现
  - [ ] 属性通知机制正常

- [ ] **模块化检查**
  - [ ] 模块边界清晰
  - [ ] 依赖注入正确配置
  - [ ] 事件通信机制完善
  - [ ] 模块注册正确

- [ ] **性能检查**
  - [ ] 大数据量分页处理
  - [ ] 异步操作使用正确
  - [ ] 内存泄漏风险排查
  - [ ] UI响应性良好

### ❌ 常见架构错误
- **直接在View中写业务逻辑**
- **ViewModel中包含UI元素**
- **同步操作阻塞UI线程**
- **循环引用导致内存泄漏**
- **缺少适当的错误处理**

---

*此Client端WPF架构指南基于凌隐宝堂中医诊所实际客户端架构编写，完全符合项目的MVVM五层架构设计。开发过程中应严格遵循此架构模式。*