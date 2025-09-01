# LYBT.Desktop.Shell

## 概述

LYBT.Desktop.Shell是凌隐宝堂桌面客户端的应用程序外壳和入口点，负责整个桌面应用的启动、初始化、模块加载和全局配置。作为WPF应用程序的主体，它整合了所有业务模块、工作台和基础服务，为用户提供统一的应用入口。

## 核心功能

### 🚀 应用程序启动
- **应用启动**: WPF应用程序的主入口点
- **模块加载**: 自动发现和加载所有业务模块
- **配置初始化**: 应用程序配置的加载和验证
- **服务注册**: 全局服务和依赖注入容器配置

### 🖥️ 主界面框架
- **主窗口**: 应用程序的主界面容器
- **首页视图**: 系统概览和快速导航
- **菜单导航**: 模块间的导航和切换
- **状态管理**: 全局应用状态的管理和同步

### 🎨 UI框架支持
- **对话框系统**: 统一的对话框管理框架
- **样式系统**: 全局样式和主题资源
- **错误处理**: 全局异常处理和用户提示
- **调试支持**: 开发阶段的UI展示和测试

### 📦 模块集成
- **业务模块**: 8个核心业务模块的集成
- **工作台模块**: 3个专业工作台的集成  
- **基础服务**: 核心服务和基础设施的整合
- **共享资源**: 全局资源和配置的共享

## 项目结构

```
src/Client/Desktop/Shell/
├── App.xaml                     # 应用程序定义
├── App.xaml.cs                  # 应用程序启动逻辑
├── appsettings.json             # 应用配置文件
├── appsettings.example.json     # 配置示例文件
├── GlobalAssemblyInfo.cs        # 全局程序集信息
├── Views/                       # 主界面视图
│   ├── MainWindow.xaml          # 主窗口界面
│   ├── MainWindow.xaml.cs       # 主窗口逻辑
│   ├── HomeView.xaml            # 首页视图
│   ├── HomeView.xaml.cs         # 首页逻辑
│   ├── TestView.xaml            # 测试视图
│   ├── UIShowcaseWindow.xaml    # UI展示窗口
│   └── PlaceholderViews.cs      # 占位符视图
├── ViewModels/                  # 视图模型
│   ├── MainWindowViewModel.cs   # 主窗口视图模型
│   ├── HomeViewModel.cs         # 首页视图模型
│   └── PlaceholderViewModels.cs # 占位符视图模型
├── Dialogs/                     # 对话框
│   ├── Views/                   # 对话框视图
│   │   ├── ConfirmationDialog.xaml    # 确认对话框
│   │   ├── ErrorDetailsDialog.xaml   # 错误详情对话框
│   │   └── InformationDialog.xaml    # 信息对话框
│   └── ViewModels/              # 对话框视图模型
│       ├── ConfirmationDialogViewModel.cs
│       ├── ErrorDetailsDialogViewModel.cs
│       └── InformationDialogViewModel.cs
├── Extensions/                  # 扩展方法
│   ├── ServiceCollectionExtensions.cs   # 服务注册扩展
│   └── ErrorHandlingServiceExtensions.cs # 错误处理扩展
└── Styles/                      # 样式资源
    └── CommonStyles.xaml        # 通用样式定义
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架 
- **WPF**: Windows Presentation Foundation
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入
- **AutoMapper 15.0.1**: 对象映射

### 配置和日志
- **Microsoft.Extensions.Configuration 9.0.7**: 配置管理
- **Microsoft.Extensions.Configuration.Json 9.0.7**: JSON配置支持
- **Microsoft.Extensions.Logging 9.0.0**: 日志框架
- **Microsoft.Extensions.Logging.Debug 9.0.0**: 调试日志

### 模块依赖
- **LYBT.Desktop.Core**: 核心框架
- **LYBT.Desktop.Services**: 业务服务层
- **LYBT.Desktop.Infrastructure**: 基础设施
- **8个业务模块**: Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula
- **3个工作台**: Core、Admin、Consultation
- **LYBT.Shared.Models**: 共享模型

## 核心特性

### 🚀 Prism应用架构

#### 应用程序启动
```csharp
public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册全局服务
        containerRegistry.RegisterSingleton<IDialogService, DialogService>();
        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
        
        // 注册应用服务
        containerRegistry.RegisterServices();
        
        // 注册对话框
        containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        containerRegistry.RegisterDialog<ErrorDetailsDialog, ErrorDetailsDialogViewModel>();
        containerRegistry.RegisterDialog<InformationDialog, InformationDialogViewModel>();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 注册所有业务模块
        moduleCatalog.AddModule<AuthModule>();
        moduleCatalog.AddModule<UsersModule>();
        moduleCatalog.AddModule<PatientsModule>();
        moduleCatalog.AddModule<MedicalCaseModule>();
        moduleCatalog.AddModule<ConsultationModule>();
        moduleCatalog.AddModule<PrescriptionsModule>();
        moduleCatalog.AddModule<HerbsModule>();
        moduleCatalog.AddModule<FormulaModule>();
        
        // 注册工作台模块
        moduleCatalog.AddModule<WorkbenchCoreModule>();
        moduleCatalog.AddModule<AdminWorkbenchModule>();
        moduleCatalog.AddModule<ConsultationWorkbenchModule>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // 配置全局异常处理
        ConfigureGlobalExceptionHandling();
        
        // 初始化配置
        InitializeConfiguration();
        
        // 启动应用
        base.OnStartup(e);
    }
}
```

### 🖥️ 主窗口框架

#### 主窗口布局
```xml
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        Title="凌隐宝堂中医诊所管理系统 v1.0"
        WindowState="Maximized"
        MinWidth="1200" MinHeight="800">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 标题栏 -->
            <RowDefinition Height="Auto"/>  <!-- 菜单栏 -->
            <RowDefinition Height="*"/>     <!-- 主内容区 -->
            <RowDefinition Height="Auto"/>  <!-- 状态栏 -->
        </Grid.RowDefinitions>
        
        <!-- 标题栏 -->
        <Border Grid.Row="0" Background="{StaticResource PrimaryBrush}">
            <Grid>
                <TextBlock Text="凌隐宝堂中医诊所管理系统" 
                          Style="{StaticResource TitleStyle}"/>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <TextBlock Text="{Binding CurrentUser.RealName}"/>
                    <Button Command="{Binding LogoutCommand}" Content="退出"/>
                </StackPanel>
            </Grid>
        </Border>
        
        <!-- 菜单栏 -->
        <Menu Grid.Row="1">
            <MenuItem Header="患者管理" Command="{Binding NavigateToCommand}" 
                      CommandParameter="PatientsManagement"/>
            <MenuItem Header="看诊诊断" Command="{Binding NavigateToCommand}" 
                      CommandParameter="ConsultationMain"/>
            <MenuItem Header="处方管理" Command="{Binding NavigateToCommand}" 
                      CommandParameter="PrescriptionsManagement"/>
            <MenuItem Header="验方管理" Command="{Binding NavigateToCommand}" 
                      CommandParameter="FormulaManagement"/>
            <MenuItem Header="中药材" Command="{Binding NavigateToCommand}" 
                      CommandParameter="HerbsManagement"/>
            <MenuItem Header="系统管理" Command="{Binding NavigateToCommand}" 
                      CommandParameter="SystemManagement"/>
        </Menu>
        
        <!-- 主内容区域 -->
        <ContentControl Grid.Row="2" prism:RegionManager.RegionName="ContentRegion"/>
        
        <!-- 状态栏 -->
        <StatusBar Grid.Row="3">
            <StatusBarItem Content="{Binding StatusMessage}"/>
            <StatusBarItem HorizontalAlignment="Right">
                <TextBlock Text="{Binding CurrentDateTime, StringFormat=yyyy-MM-dd HH:mm:ss}"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

#### 主窗口ViewModel
```csharp
public class MainWindowViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private readonly IUserSessionManager _sessionManager;
    private readonly IEventAggregator _eventAggregator;

    public UserDto CurrentUser => _sessionManager.CurrentUser;
    public string StatusMessage { get; set; }
    public DateTime CurrentDateTime { get; set; }

    public ICommand NavigateToCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainWindowViewModel(
        IRegionManager regionManager,
        IUserSessionManager sessionManager,
        IEventAggregator eventAggregator)
    {
        _regionManager = regionManager;
        _sessionManager = sessionManager;
        _eventAggregator = eventAggregator;

        NavigateToCommand = new DelegateCommand<string>(NavigateToModule);
        LogoutCommand = new DelegateCommand(ExecuteLogout);
        
        // 启动定时器更新时间
        StartTimeUpdater();
        
        // 订阅事件
        SubscribeToEvents();
    }

    private void NavigateToModule(string moduleName)
    {
        try
        {
            _regionManager.RequestNavigate("ContentRegion", moduleName);
            StatusMessage = $"已切换到{moduleName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"导航失败: {ex.Message}";
        }
    }

    private async void ExecuteLogout()
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync("确定要退出系统吗？");
            if (result)
            {
                await _sessionManager.LogoutAsync();
                
                // 发布登出事件
                _eventAggregator.GetEvent<LogoutEvent>().Publish();
                
                // 关闭主窗口
                Application.Current.MainWindow?.Close();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"退出失败: {ex.Message}");
        }
    }
}
```

### 🎨 对话框系统

#### 确认对话框
```csharp
public class ConfirmationDialogViewModel : BindableBase, IDialogAware
{
    public string Title { get; set; } = "确认";
    public string Message { get; set; }
    public string Icon { get; set; } = "Question";

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    public ConfirmationDialogViewModel()
    {
        OkCommand = new DelegateCommand(ExecuteOk);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    private void ExecuteOk()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }
    public void OnDialogOpened(IDialogParameters parameters)
    {
        Message = parameters.GetValue<string>("message");
        Title = parameters.GetValue<string>("title") ?? Title;
    }

    public string Title { get; set; }
    public event Action<IDialogResult> RequestClose;
}
```

### 📦 配置管理

#### 应用配置文件 (appsettings.json)
```json
{
  "ApiBaseUrl": "https://localhost:7001",
  "ConnectionTimeout": 30,
  "IsDebugMode": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "UI": {
    "Theme": "Light",
    "Language": "zh-CN",
    "WindowState": "Maximized"
  },
  "Features": {
    "EnableDeveloperMode": false,
    "EnableUIShowcase": false,
    "EnableAdvancedLogging": false
  }
}
```

#### 配置加载
```csharp
public static class ConfigurationHelper
{
    public static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{GetEnvironmentName()}.json", optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    private static string GetEnvironmentName()
    {
#if DEBUG
        return "Development";
#else
        return "Production";
#endif
    }
}
```

## 使用指南

### 应用程序启动

```bash
# 开发环境启动
dotnet run --project src/Client/Desktop/Shell

# 生产环境启动
LYBT.Desktop.Shell.exe
```

### 配置文件设置

```json
{
  "ApiBaseUrl": "https://your-api-server.com",
  "UI": {
    "Theme": "Dark",
    "WindowState": "Normal"
  }
}
```

### 模块导航

```csharp
// 在任何地方导航到模块
_regionManager.RequestNavigate("ContentRegion", "PatientsManagement");

// 带参数导航
var parameters = new NavigationParameters();
parameters.Add("PatientId", selectedPatientId);
_regionManager.RequestNavigate("ContentRegion", "PatientDetail", parameters);
```

### 对话框使用

```csharp
// 显示确认对话框
var parameters = new DialogParameters();
parameters.Add("message", "确定要删除这个患者吗？");
parameters.Add("title", "删除确认");

_dialogService.ShowDialog("ConfirmationDialog", parameters, (result) =>
{
    if (result.Result == ButtonResult.OK)
    {
        // 用户确认删除
        DeletePatient();
    }
});
```

## 开发规范

### 模块集成
- 所有新模块必须在App.xaml.cs中注册
- 模块间通过EventAggregator通信
- 使用依赖注入管理模块依赖
- 遵循Prism模块化架构原则

### UI一致性
- 使用统一的样式资源
- 遵循Material Design设计规范
- 保持界面元素的一致性
- 支持主题切换功能

### 错误处理
- 全局异常捕获和处理
- 用户友好的错误提示
- 详细的开发者错误信息
- 错误上报和日志记录

### 性能优化
- 延迟加载非关键模块
- 优化启动时间和内存使用
- 使用虚拟化技术处理大数据
- 实现智能缓存策略

## 开发和调试

### 开发者模式
```json
{
  "Features": {
    "EnableDeveloperMode": true,
    "EnableUIShowcase": true,
    "EnableAdvancedLogging": true
  }
}
```

### UI展示窗口
- **UIShowcaseWindow**: 用于展示所有UI组件
- **TestView**: 用于测试新功能
- **PlaceholderViews**: 开发阶段的占位符界面

### 调试功能
- 详细的日志输出
- 性能计数器监控
- 内存使用情况追踪
- UI响应时间统计

## 部署和分发

### 发布配置
```xml
<PropertyGroup>
    <PublishProfile>win-x64</PublishProfile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishSingleFile>false</PublishSingleFile>
</PropertyGroup>
```

### 安装包制作
- 使用WiX Toolset制作MSI安装包
- 包含所有依赖项和配置文件
- 支持静默安装和卸载
- 自动创建桌面快捷方式

### 更新机制
- 检查服务端版本更新
- 自动下载和安装更新
- 支持增量更新
- 更新前数据备份

## 维护说明

### 版本管理
- 遵循语义化版本规范
- 主版本.次版本.修补版本格式
- 记录详细的版本变更日志
- 支持多版本并行部署

### 配置管理
- 生产环境配置加密
- 敏感信息使用环境变量
- 配置文件的版本控制
- 配置变更的影响评估

### 监控和日志
- 应用启动和关闭日志
- 模块加载成功失败日志
- 用户操作行为日志
- 系统性能指标监控

### 故障排查
- 详细的错误堆栈信息
- 用户操作步骤重现
- 系统环境信息收集
- 远程诊断支持功能

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*