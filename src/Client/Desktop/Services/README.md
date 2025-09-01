# LYBT.Desktop.Services

## 概述

LYBT.Desktop.Services是凌隐宝堂桌面客户端的业务服务层，提供用户认证、权限管理、对话框服务、错误处理等核心业务支持服务。该模块作为桌面端与后端API的中间层，负责业务逻辑的协调和用户体验的优化。

## 核心功能

### 🔐 认证和会话管理
- **UserSessionManager**: 用户会话状态管理和持久化
- **CredentialService**: 用户凭证安全存储和管理
- **SecureCredentialService**: 加密凭证存储服务
- **AuthHeaderHandler**: HTTP请求自动添加认证头

### 🛡️ 权限和安全
- **PermissionService**: 基于角色的权限验证服务
- **角色控制**: Admin/Doctor角色权限验证
- **功能权限**: 细粒度的功能访问控制
- **安全加密**: 敏感信息的加密存储和传输

### 🌐 API通信服务
- **ApiService**: 统一的API调用服务
- **ApiErrorHandler**: API错误统一处理和分类
- **ApiTestService**: API连通性测试服务
- **响应适配**: API响应到UI模型的转换适配

### 💬 用户界面服务
- **CommonDialogService**: 通用对话框服务
- **PrismDialogService**: Prism对话框集成服务
- **ErrorHandlingService**: 用户友好的错误处理
- **消息通知**: 系统消息和用户提示

### 🔧 辅助和集成服务
- **MockIDCardReaderService**: 身份证读卡器模拟服务
- **PlaceholderServices**: 占位符服务实现
- **测试支持**: 单元测试和集成测试支持

## 项目结构

```
src/Client/Desktop/Services/
├── ApiService.cs              # 统一API调用服务
├── ApiErrorHandler.cs         # API错误处理
├── ApiTestService.cs          # API测试服务
├── UserSessionManager.cs      # 用户会话管理
├── PermissionService.cs       # 权限验证服务
├── CommonDialogService.cs     # 通用对话框
├── PrismDialogService.cs      # Prism对话框集成
├── CredentialService.cs       # 凭证管理
├── SecureCredentialService.cs # 安全凭证服务
├── ErrorHandlingService.cs    # 错误处理服务
├── MockIDCardReaderService.cs # 身份证读卡器模拟
├── PlaceholderServices.cs     # 占位符服务
├── Handlers/                  # 处理器
│   └── AuthHeaderHandler.cs  # 认证头处理器
└── Interfaces/               # 服务接口定义
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF + WinForms**: 混合UI技术栈
- **Prism.Wpf 8.1.97**: MVVM框架和对话框服务
- **AutoMapper 15.0.1**: 对象映射

### HTTP通信
- **Refit 8.0.0**: 类型安全的REST客户端
- **System.Net.Http 4.3.4**: HTTP通信基础
- **Microsoft.Extensions.Caching.Memory 9.0.0**: 内存缓存

### 项目引用
- **LYBT.Desktop.Core**: 核心框架和基础设施
- **LYBT.Desktop.Workbench.Core**: 工作台核心
- **LYBT.Shared.Models**: 共享数据模型
- **LYBT.Shared.Interfaces**: 共享服务接口

## 核心特性

### 🔐 会话管理

#### 用户会话状态
```csharp
public class UserSessionManager : IUserSessionManager
{
    // 当前用户信息
    public UserDto? CurrentUser { get; private set; }
    
    // 会话状态检查
    public bool IsLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(AccessToken);
    
    // 自动刷新令牌
    public async Task<bool> RefreshTokenIfNeededAsync()
    
    // 会话超时处理
    public event EventHandler<SessionExpiredEventArgs>? SessionExpired;
}
```

#### 凭证安全存储
```csharp
public class SecureCredentialService
{
    // 加密存储用户凭证
    public Task SaveCredentialsAsync(string username, string password, bool rememberMe);
    
    // 安全获取存储的凭证
    public Task<(string username, string password)?> GetStoredCredentialsAsync();
    
    // 清除凭证
    public Task ClearCredentialsAsync();
}
```

### 🛡️ 权限验证

#### 角色权限控制
```csharp
public class PermissionService : IPermissionService
{
    // 检查用户角色
    public bool HasRole(string role)
    
    // 检查功能权限
    public bool CanAccess(string feature)
    
    // 检查操作权限
    public bool CanPerform(string operation, object? context = null)
}
```

#### 权限验证示例
```csharp
// 在ViewModel中使用权限验证
public bool CanCreatePatient => _permissionService.HasRole("Doctor") || 
                               _permissionService.HasRole("Admin");

public bool CanDeleteUser => _permissionService.HasRole("Admin") && 
                            _permissionService.CanPerform("DeleteUser");
```

### 🌐 API通信

#### 统一API服务
```csharp
public class ApiService : IApiService
{
    // 通用API调用
    public async Task<T> CallApiAsync<T>(Func<Task<T>> apiCall)
    
    // 带错误处理的API调用
    public async Task<ApiResult<T>> SafeApiCallAsync<T>(Func<Task<T>> apiCall)
    
    // 批量API调用
    public async Task<IEnumerable<T>> BatchApiCallAsync<T>(IEnumerable<Func<Task<T>>> apiCalls)
}
```

#### 自动认证头处理
```csharp
public class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 自动添加Authorization头
        if (_sessionManager.IsLoggedIn)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", 
                _sessionManager.AccessToken);
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}
```

### 💬 用户界面服务

#### 统一对话框服务
```csharp
public class CommonDialogService : ICommonDialogService
{
    // 确认对话框
    public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    
    // 错误对话框
    public Task ShowErrorAsync(string message, string title = "错误")
    
    // 输入对话框
    public Task<string?> ShowInputAsync(string prompt, string title = "输入")
    
    // 自定义对话框
    public Task<T?> ShowDialogAsync<T>(string dialogName, IDialogParameters? parameters = null)
}
```

#### Prism对话框集成
```csharp
public class PrismDialogService : IDialogService
{
    // 显示模态对话框
    public void ShowDialog(string name, IDialogParameters parameters, Action<IDialogResult> callback)
    
    // 显示非模态对话框
    public void Show(string name, IDialogParameters parameters, Action<IDialogResult> callback)
}
```

## 服务配置和使用

### 依赖注入注册

```csharp
// 在应用程序启动时注册服务
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 会话管理
    containerRegistry.RegisterSingleton<IUserSessionManager, UserSessionManager>();
    containerRegistry.RegisterSingleton<ICredentialService, SecureCredentialService>();
    
    // 权限服务
    containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();
    
    // API服务
    containerRegistry.RegisterSingleton<IApiService, ApiService>();
    containerRegistry.Register<ApiErrorHandler>();
    
    // UI服务
    containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();
    containerRegistry.Register<IDialogService, PrismDialogService>();
}
```

### HTTP客户端配置

```csharp
// 配置HTTP客户端和处理器
services.AddHttpClient<ApiService>()
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

### 权限验证配置

```csharp
// 权限配置示例
var permissionConfig = new PermissionConfiguration
{
    Roles = new Dictionary<string, string[]>
    {
        ["Admin"] = new[] { "*" }, // 管理员拥有所有权限
        ["Doctor"] = new[] { "ViewPatients", "CreatePatients", "CreatePrescriptions" },
        ["Receptionist"] = new[] { "ViewPatients", "CreatePatients" }
    }
};
```

## 开发规范

### 服务实现规范
- 所有服务必须实现对应的接口
- 使用依赖注入获取其他服务依赖
- 异步方法命名以Async结尾
- 错误处理统一使用ErrorHandlingService

### API调用规范
- 所有API调用通过ApiService统一处理
- 使用ApiErrorHandler处理HTTP错误
- 重要操作需要权限验证
- 长时间操作支持取消令牌

### 会话管理规范
- 会话超时自动处理和用户提示
- 敏感信息加密存储
- Remember Me功能安全实现
- 登出时清理所有会话数据

### 权限控制规范
- UI元素根据权限动态显示/隐藏
- 操作执行前进行权限验证
- 权限变更时更新UI状态
- 最小权限原则，按需分配

## 测试支持

### Mock服务
```csharp
// MockIDCardReaderService - 身份证读卡器模拟
public class MockIDCardReaderService : IIDCardReaderService
{
    public Task<IDCardInfo?> ReadCardAsync()
    {
        // 返回模拟的身份证信息
        return Task.FromResult(new IDCardInfo
        {
            Name = "测试患者",
            IDNumber = "123456789012345678",
            Gender = "男",
            BirthDate = new DateTime(1990, 1, 1)
        });
    }
}
```

### 集成测试
- ApiTestService提供API连通性测试
- PlaceholderServices提供测试替代实现
- 支持离线模式和Mock数据

## 维护说明

### 安全考虑
- **凭证加密**: 用户密码和令牌必须加密存储
- **传输安全**: HTTPS通信和证书验证
- **会话安全**: 自动超时和安全登出
- **权限审计**: 关键操作的权限检查日志

### 性能优化
- **缓存策略**: 用户信息和权限信息缓存
- **连接复用**: HTTP客户端连接池管理
- **异步处理**: 避免UI线程阻塞
- **资源管理**: 及时释放不需要的资源

### 错误处理
- **分级处理**: 区分系统错误和用户错误
- **用户友好**: 提供可理解的错误消息
- **恢复机制**: 网络错误等瞬态故障的自动恢复
- **日志记录**: 详细的错误日志用于调试

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*