# LYBT.Desktop.Auth

## 概述

LYBT.Desktop.Auth是凌隐宝堂桌面客户端的认证模块，提供用户登录、身份验证、会话管理等核心认证功能。基于Prism.Wpf模块化架构设计，实现了安全可靠的用户认证体验。

## 核心功能

### 🔐 用户认证
- **登录界面**: 现代化的用户登录界面设计
- **身份验证**: 与后端JWT认证系统集成
- **Remember Me**: 安全的记住登录状态功能
- **自动登录**: 基于存储凭证的自动登录

### 🖥️ 用户界面
- **LoginView**: 主要登录视图
- **LoginWindow**: 独立登录窗口
- **现代设计**: 符合系统整体UI设计风格
- **响应式布局**: 适配不同屏幕分辨率

### 📱 MVVM架构
- **LoginViewModel**: 登录逻辑处理和状态管理
- **命令绑定**: 登录、取消、Remember Me等命令
- **数据验证**: 用户名和密码格式验证
- **错误处理**: 用户友好的错误提示

### 🔧 模块化设计
- **AuthenticationModule**: Prism模块注册和初始化
- **AuthModule**: 认证业务服务模块
- **依赖注入**: 服务和视图的统一注册管理
- **松耦合**: 与其他模块的解耦设计

## 🚨 UltraThink架构重构方案

### 当前架构问题

**🔴 严重架构问题**：
- **AuthModule.cs**: **580行巨无霸单体类**
- **职责严重混乱**: 登录认证、凭证管理、API监控、会话管理、安全验证等8个职责混合
- **违背UltraThink原则**: 与后端Auth模块三层架构完全不一致
- **维护困难**: 认证相关功能修改风险极高，影响系统安全

### 重构目标架构

**🎯 UltraThink三层架构重构**：
```csharp
AuthModule (纯委托层 - 约50行)
    ├── AuthCoreService (核心操作层 - 约140行)
    │   ├── API通信: CallLoginApi, CallLogoutApi
    │   ├── Token管理: SetToken, GetToken, ClearToken
    │   └── 基础验证: ValidateCredentials, CheckConnection
    ├── AuthQueryService (查询专业层 - 约110行)
    │   ├── 会话查询: GetSessionInfo, GetRemainingTime
    │   ├── 状态检查: ValidateToken, CheckApiStatus
    │   └── 历史记录: GetLoginHistory, GetSessionLogs
    └── AuthBusinessService (业务逻辑层 - 约130行)
        ├── 登录流程: ProcessLogin, ProcessLogout
        ├── 凭证管理: SaveCredentials, LoadCredentials
        ├── 会话管理: RefreshToken, ManageSession
        └── 安全策略: EnforcePolicy, HandleFailure
```

#### 🎯 代码质量目标
- **重构前**: 580行单体类，8个职责混合
- **重构后**: 4个文件，职责清晰分离 (总计约430行，减少26%)

### 重构优先级
**🔴 最高优先级**: 认证模块是系统安全基础，必须优先重构确保安全性

## 项目结构

### 当前结构
```
src/Client/Desktop/Modules/Auth/
├── AuthenticationModule.cs    # Prism模块定义和注册
├── Services/                 # 认证业务服务
│   └── AuthModule.cs         # 🔴 580行巨无霸 (需要重构)
├── ViewModels/              # 视图模型
│   └── LoginViewModel.cs    # 登录视图模型
├── Views/                   # 用户界面视图
│   ├── LoginView.xaml       # 主登录视图
│   ├── LoginView.xaml.cs    # 登录视图代码
│   ├── LoginWindow.xaml     # 登录窗口
│   └── LoginWindow.xaml.cs  # 登录窗口代码
├── Mappings/                # 对象映射配置
│   └── MappingProfile.cs    # AutoMapper配置
└── Api/                     # API接口定义(如果存在)
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation
- **Prism.Wpf 8.1.97**: MVVM框架和模块化架构

### 项目引用
- **LYBT.Desktop.Core**: 核心框架和基础设施
- **LYBT.Desktop.Services**: 业务服务层
- **LYBT.Desktop.Infrastructure**: 基础设施层

## 核心特性

### 🔐 安全认证流程

#### 登录流程
1. **用户输入**: 用户名和密码输入验证
2. **API调用**: 调用后端认证接口验证身份
3. **令牌管理**: JWT令牌的安全存储和管理
4. **会话建立**: 用户会话状态的建立和维护
5. **导航跳转**: 认证成功后跳转到主应用界面

#### 自动登录
```csharp
// Remember Me功能实现
public async Task<bool> TryAutoLoginAsync()
{
    var credentials = await _credentialService.GetStoredCredentialsAsync();
    if (credentials.HasValue)
    {
        return await LoginAsync(credentials.Value.username, credentials.Value.password, false);
    }
    return false;
}
```

### 📱 MVVM实现

#### LoginViewModel核心功能
```csharp
public class LoginViewModel : CoreViewModel
{
    // 绑定属性
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
    public bool IsLoggingIn { get; set; }
    
    // 命令
    public ICommand LoginCommand { get; }
    public ICommand CancelCommand { get; }
    
    // 登录方法
    private async Task LoginAsync()
    {
        try
        {
            IsLoggingIn = true;
            var result = await _authService.LoginAsync(Username, Password);
            
            if (result.IsSuccess)
            {
                if (RememberMe)
                {
                    await _credentialService.SaveCredentialsAsync(Username, Password, true);
                }
                
                // 发布登录成功事件
                _eventAggregator.GetEvent<LoginSuccessEvent>().Publish(result.Data);
                
                // 导航到主界面
                await NavigateToMainAsync();
            }
            else
            {
                ShowErrorMessage(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "登录过程中发生错误");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}
```

### 🎨 用户界面设计

#### 登录界面特性
- **现代设计**: 简洁现代的Material Design风格
- **响应式布局**: Grid和StackPanel的合理组合
- **加载状态**: 登录过程中的Loading指示器
- **错误提示**: 友好的错误消息显示
- **记住密码**: 安全的Remember Me选项

#### XAML设计示例
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <!-- 标题 -->
    <TextBlock Grid.Row="1" Text="凌隐宝堂中医诊所系统" 
               Style="{StaticResource TitleTextBlockStyle}"/>
    
    <!-- 登录表单 -->
    <StackPanel Grid.Row="2" Margin="40">
        <TextBox Text="{Binding Username}" PlaceholderText="用户名"/>
        <PasswordBox Password="{Binding Password}" PlaceholderText="密码"/>
        <CheckBox IsChecked="{Binding RememberMe}" Content="记住密码"/>
    </StackPanel>
    
    <!-- 按钮 -->
    <StackPanel Grid.Row="3" Orientation="Horizontal">
        <Button Command="{Binding LoginCommand}" Content="登录" IsDefault="True"/>
        <Button Command="{Binding CancelCommand}" Content="取消"/>
    </StackPanel>
</Grid>
```

### 🔧 模块注册

#### Prism模块配置
```csharp
public class AuthenticationModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册业务服务
        containerRegistry.RegisterSingleton<AuthModule>();
        
        // 注册视图模型
        containerRegistry.Register<LoginViewModel>();
        
        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<LoginView>();
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成
        var logger = containerProvider.Resolve<ILogger<AuthenticationModule>>();
        logger?.LogInformation("Auth模块初始化完成 - UltraThink架构");
    }
}
```

## 使用指南

### 模块注册

```csharp
// 在App.xaml.cs中注册Auth模块
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<AuthenticationModule>();
}
```

### 导航到登录

```csharp
// 导航到登录视图
_regionManager.RequestNavigate("ContentRegion", "LoginView");

// 或者显示登录窗口
var loginWindow = _container.Resolve<LoginWindow>();
loginWindow.ShowDialog();
```

### 登录状态检查

```csharp
// 检查用户是否已登录
if (_sessionManager.IsLoggedIn)
{
    // 用户已登录，进入主应用
    NavigateToMainApplication();
}
else
{
    // 尝试自动登录或显示登录界面
    if (!await TryAutoLoginAsync())
    {
        ShowLoginView();
    }
}
```

## 安全特性

### 🔒 凭证安全
- **密码加密**: 存储的密码使用加密算法保护
- **安全存储**: Windows凭据管理器或加密文件存储
- **自动清理**: 登录失败或安全事件时清理存储凭证
- **令牌管理**: JWT令牌的安全存储和定期刷新

### 🛡️ 输入验证
- **格式验证**: 用户名和密码格式要求
- **长度限制**: 防止超长输入的缓冲区攻击
- **特殊字符**: 过滤和转义特殊字符
- **SQL注入防护**: 参数化查询防止注入攻击

### 🔐 会话安全
- **超时管理**: 会话自动超时和续期
- **并发控制**: 防止同一用户多处登录
- **安全登出**: 完整清理会话数据和令牌
- **异常处理**: 认证失败的安全响应

## 开发规范

### MVVM模式
- LoginViewModel继承自CoreViewModel获得基础功能
- 使用AsyncRelayCommand处理异步登录操作
- 通过数据绑定实现视图和模型的同步
- 使用EventAggregator发布登录状态事件

### 错误处理
- 网络错误、认证失败等统一处理
- 提供用户友好的错误消息
- 详细错误日志记录用于调试
- 支持重试机制和错误恢复

### 性能优化
- 登录界面快速加载和响应
- 异步操作避免UI阻塞
- 内存使用优化，及时释放资源
- 缓存用户偏好和界面状态

## 集成说明

### 与其他模块的关系
- **Core**: 提供基础框架和通用服务
- **Services**: 使用认证服务和会话管理
- **Infrastructure**: 使用HTTP客户端和API配置
- **其他业务模块**: 认证成功后激活和导航

### 事件通信
```csharp
// 发布登录成功事件
_eventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);

// 发布登出事件
_eventAggregator.GetEvent<LogoutEvent>().Publish();

// 订阅会话超时事件
_eventAggregator.GetEvent<SessionExpiredEvent>().Subscribe(OnSessionExpired);
```

## 维护说明

### 安全更新
- 定期更新认证相关的依赖包
- 关注JWT库的安全漏洞和补丁
- 密码存储算法的升级和迁移
- 监控异常登录活动和安全日志

### 用户体验优化
- 登录界面的持续改进和美化
- 错误消息的多语言支持
- 登录流程的用户体验测试
- 无障碍访问支持

### 代码质量
- 单元测试覆盖关键登录逻辑
- 集成测试验证认证流程
- 代码审查关注安全实践
- 性能基准测试和监控

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*