# LYBT.Desktop.Admin - 管理员角色模块

## 📦 项目定位

- **层级**:Client端
- **类型**:角色模块(管理员工作台)
- **职责**:提供管理员角色专属的工作台主页和功能导航入口。作为系统管理员的核心工作界面,集成用户管理、中药管理、患者管理、方剂管理、病案管理、系统设置6个管理功能模块的快速导航,支持基于角色的权限控制。

##  代码结构

```
LYBT.Desktop.Admin/
├── AdminModule.cs                   # Prism模块注册
│   └── RegisterTypes()              # 注册Views和ViewModels
├── ViewModels/
│   └── AdminHomeViewModel.cs        # 管理员主页ViewModel
│       ├── NavigateToUserManagementCommand # 用户管理导航命令
│       ├── NavigateToHerbManagementCommand # 中药管理导航命令
│       ├── NavigateToPatientManagementCommand # 患者管理导航命令
│       ├── NavigateToFormulaManagementCommand # 方剂管理导航命令
│       ├── NavigateToMedicalCaseManagementCommand # 病案管理导航命令
│       ├── NavigateToSystemSettingsCommand # 系统设置导航命令
│       ├── NavigateTo()             # 通用导航方法
│       ├── OnNavigatedTo()          # Prism导航生命周期(进入)
│       ├── OnNavigatedFrom()        # Prism导航生命周期(离开)
│       └── IsNavigationTarget()     # Prism导航目标判断
└── Views/
    ├── AdminHomeView.xaml           # 管理员主页视图(XAML)
    └── AdminHomeView.xaml.cs        # 管理员主页视图后置代码
```

**说明**:
- **AdminModule**:Prism模块注册,自动发现Views和ViewModels
- **AdminHomeViewModel**:6个导航命令 + Prism导航接口实现(INavigationAware)
- **AdminHomeView**:管理员工作台主页UI,包含6个功能卡片/按钮

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Foundation** - Desktop端基础类型和接口
2. **LYBT.Desktop.Infrastructure** - 基础设施库(区域管理、导航服务)
3. **LYBT.Desktop.Models** - ViewModels基类(ViewModelBase)
4. **LYBT.Desktop.Contracts** - 契约定义(区域名称常量等)
5. **LYBT.Shared.Models** - 共享DTO模型(用户权限等)

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell层加载管理员模块,注入主工作区

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
dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Admin/LYBT.Desktop.Admin.csproj
```

**集成说明**:

### 1. Shell层加载模块(在App.xaml.cs中)
```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 加载管理员角色模块
    moduleCatalog.AddModule<AdminModule>();
}
```

### 2. 导航到管理员主页(从Shell或其他模块)
```csharp
public class MainViewModel
{
    private readonly IRegionManager _regionManager;

    public MainViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    private void NavigateToAdminHome()
    {
        // 导航到管理员主页(注入到ContentRegion)
        _regionManager.RequestNavigate("ContentRegion", "AdminHomeView");
    }
}
```

### 3. XAML布局示例(AdminHomeView.xaml)
```xml
<UserControl x:Class="LYBT.Desktop.Admin.Views.AdminHomeView"
             xmlns:prism="http://prismlibrary.com/">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <TextBlock Grid.Row="0" Text="管理员工作台" FontSize="24" Margin="20"/>

        <!-- 功能卡片网格 -->
        <UniformGrid Grid.Row="1" Rows="2" Columns="3" Margin="20">
            <!-- 用户管理 -->
            <Button Content="用户管理"
                    Command="{Binding NavigateToUserManagementCommand}"
                    Style="{StaticResource FunctionCardStyle}"/>

            <!-- 中药管理 -->
            <Button Content="中药管理"
                    Command="{Binding NavigateToHerbManagementCommand}"
                    Style="{StaticResource FunctionCardStyle}"/>

            <!-- 患者管理 -->
            <Button Content="患者管理"
                    Command="{Binding NavigateToPatientManagementCommand}"
                    Style="{StaticResource FunctionCardStyle}"/>

            <!-- 方剂管理 -->
            <Button Content="方剂管理"
                    Command="{Binding NavigateToFormulaManagementCommand}"
                    Style="{StaticResource FunctionCardStyle}"/>

            <!-- 病案管理 -->
            <Button Content="病案管理"
                    Command="{Binding NavigateToMedicalCaseManagementCommand}"
                    Style="{StaticResource FunctionCardStyle}"/>

            <!-- 系统设置 -->
            <Button Content="系统设置"
                    Command="{Binding NavigateToSystemSettingsCommand}"
                    Style="{StaticResource FunctionCardStyle}"/>
        </UniformGrid>
    </Grid>
</UserControl>
```

### 4. 权限控制(基于SessionManager)
```csharp
public class AdminHomeViewModel : ViewModelBase, INavigationAware
{
    private readonly ISessionManager _sessionManager;
    private readonly IRegionManager _regionManager;

    public AdminHomeViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        ISessionManager sessionManager,
        IRegionManager regionManager)
        : base(eventAggregator, loggerFactory)
    {
        _sessionManager = sessionManager;
        _regionManager = regionManager;

        // 初始化命令
        NavigateToUserManagementCommand = new DelegateCommand(
            () => NavigateTo("UserManagementView"),
            () => _sessionManager.HasPermission("Users.View") // 权限检查
        );
    }

    private void NavigateTo(string viewName)
    {
        _regionManager.RequestNavigate("ContentRegion", viewName);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 页面进入时刷新权限(可能用户权限已变更)
        RaisePropertyChanged(nameof(NavigateToUserManagementCommand));
        RaisePropertyChanged(nameof(NavigateToHerbManagementCommand));
        // 其他命令...
    }
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/desktop-admin/](../../../../../docs/reference/modules/desktop-admin/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/admin-module-design.md](../../../../../docs/explanation/architecture/client/admin-module-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/admin-development.md](../../../../../docs/how-to-guides/client/admin-development.md) *(待创建)*
- **Prism导航**:[docs/reference/quick-reference/code-patterns.md](../../../../../docs/reference/quick-reference/code-patterns.md) - 参见"Prism导航模式"章节

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
