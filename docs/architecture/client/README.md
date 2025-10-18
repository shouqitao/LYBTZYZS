# Client端架构指南

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**对应代码层**：LYBT.Desktop  

## 🏗️ Client端WPF架构设计

凌隐宝堂中医诊所Client端采用\*\*WPF MVVM架构\*\*，严格遵循分层解耦原则。

### ⚠️ 架构演化说明（Phase 2）

**重要变更**：基于Issue #1114，Client端架构已从五层演化到四层架构：

| 架构版本 | 层次结构 | ViewModel依赖 | 实施时间 |
|---------|---------|--------------|----------|
| **Phase 1**（已废弃） | Shell → Core → **Services** → Infrastructure → Modules | ViewModel → Service → Repository | 2024年初 |
| **Phase 2**（当前） | Shell → Core → Infrastructure → Modules | ViewModel → **直接使用Repository** | 2024年Q2 |

**变更原因**：
- ✅ **简化架构**：去除中间Service层，减少抽象层级
- ✅ **提升性能**：减少一层调用，降低内存开销
- ✅ **代码精简**：避免Service层与Repository的重复逻辑
- ✅ **对齐Server**：与Server端保持一致的分层架构风格

**实际代码证据**（PatientDetailViewModel.cs:17-18）：
```csharp
/// <summary>
/// 患者详情视图模型 - Phase 2模块化架构
/// Issue #1114 - 直接使用Repository，去除Service层
/// </summary>
public class PatientDetailViewModel : UnifiedViewModelBase
{
    private readonly IPatientRepository _patientRepository;  // ⭐ 直接注入Repository
    // ...
}
```

### 📐 当前架构（Phase 2）

```
LYBT.Desktop (WPF应用) - 四层架构
├── Shell层           # 主程序入口、窗口容器
├── Core层            # 核心基础设施、DI容器、事件聚合
├── Infrastructure层  # 外部依赖、HTTP客户端、本地存储
└── Modules层         # 业务模块、MVVM组件
    ├── ViewModels/   # ⭐ 直接依赖Repository（Phase 2变更）
    ├── Views/        # XAML视图
    ├── Models/       # 数据模型
    ├── Repositories/ # 数据访问（HTTP API调用）
    └── Interfaces/   # 接口定义
```

**核心数据流**（Phase 2）：
```
User Interaction (View)
    ↓
ViewModel (Command + INotifyPropertyChanged)
    ↓
Repository (HTTP API Call) ⭐ 直接调用，无中间Service层
    ↓
Server API (REST Endpoint)
```

---

## 📐 架构层次详解

### 1. Shell层 - 应用程序容器
**职责**：应用程序启动、窗口管理、主题配置

**核心组件**：
- `App.xaml` - WPF应用程序入口
- `MainWindow.xaml` - 主窗口容器
- `MainWindowViewModel.cs` - 主窗口视图模型

**代码示例**：
```csharp
// App.xaml.cs
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 初始化DI容器
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
            
        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        // 注册所有服务和模块
    }
}
```

### 2. Core层 - 核心基础设施
**职责**：依赖注入、事件聚合、导航服务、共享工具

**核心组件**：
- `IocContainer.cs` - IoC容器配置
- `EventAggregator.cs` - 事件聚合器
- `NavigationService.cs` - 导航服务
- `ViewModelBase.cs` - 视图模型基类

**代码示例**：
```csharp
// Core/ViewModelBase.cs
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    
    protected ViewModelBase(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }
    
    protected void Publish<T>(T eventData) where T : class
    {
        _eventAggregator.Publish(eventData);
    }
    
    protected void Subscribe<T>(Action<T> handler) where T : class
    {
        _eventAggregator.Subscribe(handler);
    }
    
    public abstract string Title { get; }
    
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
}
```

### 3. Services层 - 业务服务
**职责**：API调用、业务逻辑、数据处理、缓存管理

**核心组件**：
- `IAuthService.cs` - 认证服务接口
- `IUserService.cs` - 用户服务接口
- `IPatientService.cs` - 患者服务接口
- `BaseService.cs` - 服务基类

**代码示例**：
```csharp
// Services/AuthService.cs
public class AuthService : BaseService, IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenService _tokenService;
    
    public AuthService(IHttpClientFactory httpClientFactory, ITokenService tokenService)
    {
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
    }
    
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PostAsJsonAsync("/api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                await _tokenService.SaveTokensAsync(result.AccessToken, result.RefreshToken);
                return AuthResult.Success(result.User);
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return AuthResult.Failure(error.Message);
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"登录失败: {ex.Message}");
        }
    }
    
    public async Task LogoutAsync()
    {
        await _tokenService.ClearTokensAsync();
        // 发布用户登出事件
        Publish(new UserLoggedOutEvent());
    }
}
```

### 4. Infrastructure层 - 基础设施
**职责**：数据持久化、外部服务集成、工具类

**核心组件**：
- `TokenService.cs` - 令牌管理服务
- `CacheService.cs` - 缓存服务
- `StorageService.cs` - 本地存储服务
- `HttpMessageHandlers.cs` - HTTP消息处理器

**代码示例**：
```csharp
// Infrastructure/TokenService.cs
public class TokenService : ITokenService
{
    private readonly ISecureStorage _secureStorage;
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    
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
    
    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await _secureStorage.SetAsync(AccessTokenKey, accessToken);
        await _secureStorage.SetAsync(RefreshTokenKey, refreshToken);
    }
    
    public async Task ClearTokensAsync()
    {
        await _secureStorage.RemoveAsync(AccessTokenKey);
        await _secureStorage.RemoveAsync(RefreshTokenKey);
    }
}
```

### 5. Modules层 - 业务模块
**职责**：UI界面、业务逻辑、模块化组件

**模块结构**：
```
Modules/
├── Auth/                    # 认证模块
│   ├── Views/              # 视图
│   ├── ViewModels/         # 视图模型
│   ├── Models/             # 数据模型
│   └── Services/           # 模块服务
├── Users/                  # 用户管理模块
├── Patients/               # 患者管理模块
├── MedicalCase/            # 医案管理模块
├── Consultation/           # 诊疗模块
├── Prescriptions/          # 处方模块
├── Herbs/                  # 药材模块
└── Formula/                # 验方模块
```

#### 📋 Prescriptions模块架构演化（Issue #1445）

**重要变更**：处方模块视图架构已于2025-10-18统一，删除了Phase 4B空骨架实现。

| 演化阶段 | 视图实现 | 状态 | 问题 |
|---------|---------|------|------|
| **Phase 4B**（已废弃） | PrescriptionView（434行空骨架） | 2025-10-18删除 | 导航错误导致空白界面 |
| **统一架构**（当前） | PrescriptionView（932行完整实现） | 当前使用 | 重命名自PrescriptionComposerView |

**架构清理过程**（Epic #1445）：
- ✅ **ARCH-1** (#1446): 删除Phase 4B空骨架（PrescriptionView.xaml/cs/ViewModel）
- ✅ **ARCH-2** (#1447): 重命名PrescriptionComposerView → PrescriptionView
- ✅ **ARCH-3** (#1448): 更新所有导航配置引用
- 🔄 **ARCH-4** (#1449): 更新架构文档（本文档）

**当前Prescriptions视图结构**：
```
Modules/Prescriptions/
├── Views/
│   ├── PrescriptionView.xaml          # 处方编辑主界面（8列DataGrid，完整实现）
│   ├── PrescriptionManagementView.xaml # 处方列表管理界面
│   ├── PrescriptionDetailView.xaml    # 处方详情查看界面
│   └── FormulaTemplateSelectionDialog.xaml  # 验方模板选择对话框
├── ViewModels/
│   ├── PrescriptionViewModel.cs        # 处方编辑ViewModel（包含组件化架构）
│   ├── PrescriptionManagementViewModel.cs
│   ├── PrescriptionsMainViewModel.cs
│   └── FormulaTemplateSelectionDialogViewModel.cs
└── Components/                         # 组件化设计（PrescriptionViewModel依赖）
    ├── PrescriptionDataManager.cs      # 数据管理组件
    ├── PrescriptionCommandHandler.cs   # 命令处理组件
    └── FormulaImportService.cs         # 验方导入服务
```

**导航配置**：
- 创建新处方：`NavigateTo("MainRegion", "PrescriptionView")`
- 编辑处方：`NavigateTo("MainRegion", "PrescriptionView", parameters)`
- 管理列表：`NavigateTo("PrescriptionContentRegion", "PrescriptionManagementView")`

**模块基类**：
```csharp
// Modules/ModuleBase.cs
public abstract class ModuleBase : IModule
{
    protected IServiceProvider ServiceProvider { get; }
    protected IEventAggregator EventAggregator { get; }
    
    protected ModuleBase(IServiceProvider serviceProvider, IEventAggregator eventAggregator)
    {
        ServiceProvider = serviceProvider;
        EventAggregator = eventAggregator;
    }
    
    public abstract void Initialize();
    public abstract string ModuleName { get; }
    public abstract string ModuleDescription { get; }
}

// Modules/Patients/PatientsModule.cs
public class PatientsModule : ModuleBase
{
    public PatientsModule(IServiceProvider serviceProvider, IEventAggregator eventAggregator)
        : base(serviceProvider, eventAggregator)
    {
    }
    
    public override void Initialize()
    {
        // 注册模块服务
        var services = ServiceProvider.GetRequiredService<IServiceCollection>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        
        // 注册视图和视图模型
        services.AddTransient<PatientManagementView>();
        services.AddTransient<PatientManagementViewModel>();
    }
    
    public override string ModuleName => "患者管理";
    public override string ModuleDescription => "患者信息管理、查询统计功能";
}
```

## 🎯 MVVM架构模式

### Model - 数据模型
**职责**：业务实体、数据验证、状态管理

```csharp
// Modules/Patients/Models/PatientModel.cs
public class PatientModel : ObservableObject
{
    private int _id;
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    private string _name;
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    private DateTime _birthDate;
    public DateTime BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }
    
    private string _phone;
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }
    
    private string _address;
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }
    
    public int Age => DateTime.Today.Year - BirthDate.Year;
}
```

### View - 用户界面
**职责**：界面布局、用户交互、数据绑定

```xml
<!-- Modules/Patients/Views/PatientManagementView.xaml -->
<UserControl x:Class="LYBT.Desktop.Modules.Patients.Views.PatientManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="800">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Button Content="新增患者" Command="{Binding AddPatientCommand}" 
                    Style="{StaticResource PrimaryButtonStyle}" Margin="0,0,10,0"/>
            <Button Content="编辑患者" Command="{Binding EditPatientCommand}" 
                    Style="{StaticResource SecondaryButtonStyle}" Margin="0,0,10,0"/>
            <Button Content="删除患者" Command="{Binding DeletePatientCommand}" 
                    Style="{StaticResource DangerButtonStyle}" Margin="0,0,10,0"/>
        </StackPanel>
        
        <!-- 搜索栏 -->
        <Grid Grid.Row="1" Margin="10,5">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBox Grid.Column="0" Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                     PlaceholderText="输入患者姓名或手机号搜索"/>
            <Button Grid.Column="1" Content="搜索" Command="{Binding SearchCommand}" 
                    Style="{StaticResource PrimaryButtonStyle}" Margin="5,0,0,0"/>
        </Grid>
        
        <!-- 患者列表 -->
        <DataGrid Grid.Row="2" ItemsSource="{Binding Patients}" 
                  SelectedItem="{Binding SelectedPatient}"
                  AutoGenerateColumns="False" CanUserAddRows="False" Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="年龄" Binding="{Binding Age}" Width="80"/>
                <DataGridTextColumn Header="手机号" Binding="{Binding Phone}" Width="120"/>
                <DataGridTextColumn Header="地址" Binding="{Binding Address}" Width="*"/>
                <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd HH:mm'}" Width="150"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

### ViewModel - 视图模型
**职责**：业务逻辑、命令处理、数据转换、状态管理

```csharp
// Modules/Patients/ViewModels/PatientManagementViewModel.cs
public class PatientManagementViewModel : ViewModelBase
{
    private readonly IPatientService _patientService;
    private readonly IDialogService _dialogService;
    
    public PatientManagementViewModel(IPatientService patientService, 
                                    IDialogService dialogService,
                                    IEventAggregator eventAggregator)
        : base(eventAggregator)
    {
        _patientService = patientService;
        _dialogService = dialogService;
        
        Patients = new ObservableCollection<PatientModel>();
        LoadPatientsCommand = new AsyncRelayCommand(LoadPatientsAsync);
        AddPatientCommand = new AsyncRelayCommand(AddPatientAsync);
        EditPatientCommand = new AsyncRelayCommand<PatientModel>(EditPatientAsync);
        DeletePatientCommand = new AsyncRelayCommand<PatientModel>(DeletePatientAsync);
        SearchCommand = new AsyncRelayCommand(SearchPatientsAsync);
    }
    
    public ObservableCollection<PatientModel> Patients { get; }
    
    private PatientModel _selectedPatient;
    public PatientModel SelectedPatient
    {
        get => _selectedPatient;
        set => SetProperty(ref _selectedPatient, value);
    }
    
    private string _searchKeyword;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }
    
    public ICommand LoadPatientsCommand { get; }
    public ICommand AddPatientCommand { get; }
    public ICommand EditPatientCommand { get; }
    public ICommand DeletePatientCommand { get; }
    public ICommand SearchCommand { get; }
    
    public override string Title => "患者管理";
    
    private async Task LoadPatientsAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _patientService.GetPatientsAsync();
            if (result.IsSuccess)
            {
                Patients.Clear();
                foreach (var patient in result.Data)
                {
                    Patients.Add(patient);
                }
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"加载患者列表失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task AddPatientAsync()
    {
        var dialog = new PatientEditDialog();
        var viewModel = new PatientEditViewModel(_patientService, _dialogService, EventAggregator);
        dialog.DataContext = viewModel;
        
        if (await _dialogService.ShowDialogAsync(dialog) == true)
        {
            await LoadPatientsAsync();
        }
    }
    
    private async Task EditPatientAsync(PatientModel patient)
    {
        if (patient == null) return;
        
        var dialog = new PatientEditDialog();
        var viewModel = new PatientEditViewModel(_patientService, _dialogService, EventAggregator, patient);
        dialog.DataContext = viewModel;
        
        if (await _dialogService.ShowDialogAsync(dialog) == true)
        {
            await LoadPatientsAsync();
        }
    }
    
    private async Task DeletePatientAsync(PatientModel patient)
    {
        if (patient == null) return;
        
        var result = await _dialogService.ShowConfirmAsync($"确定要删除患者 {patient.Name} 吗？");
        if (!result) return;
        
        IsBusy = true;
        try
        {
            var deleteResult = await _patientService.DeletePatientAsync(patient.Id);
            if (deleteResult.IsSuccess)
            {
                Patients.Remove(patient);
                await _dialogService.ShowSuccessAsync("删除成功");
            }
            else
            {
                await _dialogService.ShowErrorAsync(deleteResult.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task SearchPatientsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            await LoadPatientsAsync();
            return;
        }
        
        IsBusy = true;
        try
        {
            var result = await _patientService.SearchPatientsAsync(SearchKeyword);
            if (result.IsSuccess)
            {
                Patients.Clear();
                foreach (var patient in result.Data)
                {
                    Patients.Add(patient);
                }
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"搜索失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

## 🔧 依赖注入配置

### 服务注册
```csharp
// Core/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
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
        
        return services;
    }
}
```

### AutoMapper配置
```csharp
// Core/MappingProfile.cs
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Patient映射
        CreateMap<PatientDto, PatientModel>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));
        CreateMap<PatientModel, PatientCreateRequest>();
        CreateMap<PatientModel, PatientUpdateRequest>();
        
        // MedicalCase映射
        CreateMap<MedicalCaseDto, MedicalCaseModel>();
        CreateMap<MedicalCaseModel, MedicalCaseCreateRequest>();
        CreateMap<MedicalCaseModel, MedicalCaseUpdateRequest>();
        
        // Consultation映射
        CreateMap<ConsultationDto, ConsultationModel>();
        CreateMap<ConsultationModel, ConsultationCreateRequest>();
        CreateMap<ConsultationModel, ConsultationUpdateRequest>();
        
        // Prescription映射
        CreateMap<PrescriptionDto, PrescriptionModel>();
        CreateMap<PrescriptionModel, PrescriptionCreateRequest>();
        CreateMap<PrescriptionModel, PrescriptionUpdateRequest>();
        
        // Herb映射
        CreateMap<HerbDto, HerbModel>();
        CreateMap<HerbModel, HerbCreateRequest>();
        CreateMap<HerbModel, HerbUpdateRequest>();
        
        // Formula映射
        CreateMap<FormulaDto, FormulaModel>();
        CreateMap<FormulaModel, FormulaCreateRequest>();
        CreateMap<FormulaModel, FormulaUpdateRequest>();
    }
    
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

## 🎨 主题与样式

### 资源字典
```xml
<!-- Styles/Colors.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 主色调 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#2196F3"/>
    <SolidColorBrush x:Key="SecondaryBrush" Color="#FFC107"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#4CAF50"/>
    <SolidColorBrush x:Key="DangerBrush" Color="#F44336"/>
    <SolidColorBrush x:Key="WarningBrush" Color="#FF9800"/>
    <SolidColorBrush x:Key="InfoBrush" Color="#2196F3"/>
    
    <!-- 背景色 -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="#F5F5F5"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="OnSurfaceBrush" Color="#212121"/>
    
    <!-- 文字颜色 -->
    <SolidColorBrush x:Key="PrimaryTextBrush" Color="#212121"/>
    <SolidColorBrush x:Key="SecondaryTextBrush" Color="#757575"/>
    <SolidColorBrush x:Key="DisabledTextBrush" Color="#BDBDBD"/>
</ResourceDictionary>

<!-- Styles/ButtonStyles.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="Margin" Value="4"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>
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
                            <Setter Property="Background" Value="#1976D2"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="#1565C0"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Background" Value="#BDBDBD"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    
    <Style x:Key="SecondaryButtonStyle" TargetType="Button" BasedOn="{StaticResource PrimaryButtonStyle}">
        <Setter Property="Background" Value="{StaticResource SecondaryBrush}"/>
        <Setter Property="Foreground" Value="Black"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#FFA000"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="#FF8F00"/>
            </Trigger>
        </Style.Triggers>
    </Style>
    
    <Style x:Key="DangerButtonStyle" TargetType="Button" BasedOn="{StaticResource PrimaryButtonStyle}">
        <Setter Property="Background" Value="{StaticResource DangerBrush}"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#D32F2F"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
                <Setter Property="Background" Value="#C62828"/>
            </Trigger>
        </Style.Triggers>
    </Style>
</ResourceDictionary>
```

## 🚀 性能优化

### 1. 数据绑定优化
```csharp
// 使用OneTime绑定减少更新开销
<TextBlock Text="{Binding Title, Mode=OneTime}"/>

// 对大数据集合使用虚拟化
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         ScrollViewer.IsDeferredScrollingEnabled="True"/>
```

### 2. 异步操作优化
```csharp
// 使用ConfigureAwait减少上下文切换
var result = await _patientService.GetPatientsAsync().ConfigureAwait(false);

// 使用CancellationToken取消长时间操作
public async Task LoadPatientsAsync(CancellationToken cancellationToken = default)
{
    IsBusy = true;
    try
    {
        var result = await _patientService.GetPatientsAsync(cancellationToken);
        // 处理结果
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
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                Patients?.Clear();
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

## 🧪 测试策略

### 单元测试
```csharp
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
    public async Task LoadPatientsAsync_WhenServiceReturnsSuccess_ShouldPopulatePatients()
    {
        // Arrange
        var patients = new List<PatientModel>
        {
            new PatientModel { Id = 1, Name = "张三" },
            new PatientModel { Id = 2, Name = "李四" }
        };
        
        _mockPatientService.Setup(x => x.GetPatientsAsync())
            .ReturnsAsync(Result<List<PatientModel>>.Success(patients));
        
        // Act
        await _viewModel.LoadPatientsCommand.ExecuteAsync(null);
        
        // Assert
        Assert.AreEqual(2, _viewModel.Patients.Count);
        Assert.AreEqual("张三", _viewModel.Patients[0].Name);
        Assert.AreEqual("李四", _viewModel.Patients[1].Name);
    }
}
```

## 📋 最佳实践

### 1. 代码规范
- **命名约定**：使用PascalCase，接口以I开头
- **依赖注入**：优先使用构造函数注入
- **异步编程**：I/O操作使用async/await
- **错误处理**：统一使用try-catch包装异步操作

### 2. 架构原则
- **单一职责**：每个类只负责一个功能
- **开闭原则**：对扩展开放，对修改封闭
- **依赖倒置**：依赖抽象，不依赖具体实现
- **关注分离**：UI、业务逻辑、数据访问分离

### 3. 性能原则
- **避免阻塞**：UI线程不执行长时间操作
- **合理缓存**：缓存常用数据，减少网络请求
- **资源释放**：及时释放不再使用的资源
- **延迟加载**：大数据集使用分页或虚拟化

### 4. 聚合根设计模式（Issue #1463）

**核心原则**：MedicalCase是聚合根，统一管理Consultation和Prescription的生命周期。

#### ❌ 错误实现
```csharp
public class ConsultationEntryViewModel
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // ❌ 错误：分两步创建，破坏聚合根模式
    private async Task SaveAsync()
    {
        // 1. 单独创建MedicalCase
        if (!MedicalCaseId.HasValue)
        {
            var medicalCase = await _medicalCaseRepository.CreateAsync(medicalCaseDto);
            MedicalCaseId = medicalCase.Id;
        }

        // 2. 单独创建Consultation
        consultationDto.MedicalCaseId = MedicalCaseId.Value;
        await _consultationRepository.CreateAsync(consultationDto);
    }
}
```

**问题**：
- 破坏原子性（两次API调用，可能部分失败）
- 违反DDD聚合根模式（子实体独立创建）
- 依赖混乱（同时注入MedicalCase和Consultation的Repository）

#### ✅ 正确实现
```csharp
public class ConsultationEntryViewModel
{
    // ✅ 只依赖聚合根Repository
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    private async Task SaveAsync()
    {
        if (!ValidateInput()) return;

        // 构造聚合根数据
        var medicalCaseDto = new MedicalCaseCreateDto
        {
            PatientId = CurrentPatient!.Id,
            DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
            ChiefComplaint = ChiefComplaint,
            Remark = $"创建于: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        };

        // 构造子实体数据
        var consultationDto = new ConsultationCreateDto
        {
            PatientId = CurrentPatient!.Id,
            UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
            PatientName = CurrentPatient.Name,
            DoctorName = SessionManager?.CurrentUser?.RealName ?? "未知医生",
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            Inspection = Inspection,
            AuscultationOlfaction = AuscultationOlfaction,
            Inquiry = Inquiry,
            Palpation = Palpation,
            TCMDiagnosis = TCMDiagnosis,
            TreatmentPrinciple = TreatmentPrinciple,
            Remark = Remarks,
            StartTime = DateTime.Now
        };

        // ✅ 使用聚合根方法一次性创建（原子操作）
        var result = await _medicalCaseRepository.CreateWithDetailsAsync(
            medicalCaseDto,
            consultationDto,
            null // 暂无处方
        );

        MedicalCaseId = result.Id;

        Logger.LogInformation("诊疗记录保存成功, MedicalCaseId: {MedicalCaseId}, 患者: {PatientName}",
            result.Id, CurrentPatient.Name);
    }
}
```

**优势**：
- ✅ **原子性**：一次API调用完成整个聚合创建
- ✅ **一致性**：Server端保证MedicalCase和Consultation的共享主键关系
- ✅ **符合DDD**：聚合根统一管理子实体生命周期
- ✅ **简化依赖**：ViewModel只需注入IMedicalCaseRepository

#### 架构规范
1. **聚合识别**：MedicalCase = Consultation + Prescription（一对一关系，共享主键）
2. **创建规则**：必须通过`IMedicalCaseRepository.CreateWithDetailsAsync()`创建
3. **禁止模式**：禁止ViewModel直接调用`IConsultationRepository.CreateAsync()`
4. **模块依赖**：ConsultationModule保留`[ModuleDependency("MedicalCaseModule")]`确保初始化顺序

**参考**：
- Server端实现：`LYBT.Module.MedicalCase.Services.MedicalCaseService:CreateWithDetailsAsync()`
- Desktop端实现：`LYBT.Desktop.MedicalCase.Repositories.MedicalCaseRepository:CreateWithDetailsAsync()`
- 修复Issue：#1463

## 🔗 相关文档

- **[架构总览](../README.md)** - 三层对齐架构设计原理
- **[Server端架构](../server/README.md)** - 服务端三层架构实现
- **[共享架构](../shared/README.md)** - 跨端组件和标准
- **[Client端开发指南](../../development/client/README.md)** - WPF开发规范和实践
- **[模块设计指南](../module-design-guide.md)** - 业务模块化设计标准

---

**文档维护**：架构组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核