# Design: refactor-login-authentication

## Overview

本文档定义登录认证系统重构的架构设计，涵盖Token存储策略、凭据安全、状态管理和事件体系。

## Current Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         LoginView                                │
│                    (LYBT.Desktop.Auth)                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      LoginViewModel                              │
│  - ExecuteLogin()                                                │
│  - LoadSavedCredentials()                                        │
│  - SaveCredentials()                                             │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
┌──────────────────┐ ┌─────────────────┐ ┌──────────────────────┐
│ LoginCoordinator │ │ Authentication  │ │ SecureCredential     │
│   (Shell)        │ │ Service         │ │ Storage              │
│                  │ │ (Foundation)    │ │ (Foundation)         │
│ 5 States:        │ │                 │ │                      │
│ - NotLoggedIn    │ │ - Login()       │ │ - SaveCredentials()  │
│ - Authenticating │ │ - Logout()      │ │ - LoadCredentials()  │
│ - StartingSession│ │ - RefreshToken()│ │ - DeleteCredentials()│
│ - LoadingModules │ └─────────────────┘ └──────────────────────┘
│ - Navigating     │          │
│ - LoggedIn       │          ▼
└──────────────────┘ ┌─────────────────┐
                     │ TokenStorage    │
                     │ Service         │
                     │ (Memory Only)   │
                     └─────────────────┘
```

### 现有问题分析

1. **Token存储与凭据存储混淆**
   - TokenStorageService: 仅内存存储（正确）
   - SecureCredentialStorage: DPAPI加密存储密码（安全隐患）
   - 两者关系不清晰

2. **LoginCoordinator职责过重**
   - 管理5个状态
   - 协调多个服务
   - 难以测试

3. **缺少统一事件机制**
   - 各组件直接依赖
   - 无法监控认证生命周期

## Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         LoginView                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      LoginViewModel                              │
│  (简化：仅UI绑定和命令转发)                                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   AuthenticationFacade                           │
│  (统一入口：Login/Logout/AutoLogin/RefreshToken)                  │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐   ┌─────────────────┐   ┌─────────────────────┐
│ LoginState    │   │ TokenManager    │   │ CredentialVault     │
│ Machine       │   │                 │   │                     │
│               │   │ - AccessToken   │   │ - Username          │
│ States:       │   │   (Memory)      │   │ - AutoLoginToken    │
│ - Idle        │   │ - RefreshToken  │   │   (DPAPI+HMAC)      │
│ - Validating  │   │   (Memory)      │   │ - NO PASSWORD       │
│ - Refreshing  │   │ - Lifecycle     │   │                     │
│ - Active      │   │   Monitoring    │   │                     │
│ - Expired     │   └─────────────────┘   └─────────────────────┘
└───────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                    AuthEventBus                                  │
│  (Prism EventAggregator)                                        │
│                                                                  │
│  Events:                                                         │
│  - LoginStarted                                                  │
│  - LoginSucceeded                                                │
│  - LoginFailed                                                   │
│  - SessionExpiring                                               │
│  - SessionExpired                                                │
│  - LogoutStarted                                                 │
│  - LogoutCompleted                                               │
│  - TokenRefreshed                                                │
│  - TokenRefreshFailed                                            │
└─────────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. Token存储策略

**决策：Token严格仅存内存，不做任何持久化**

```csharp
public interface ITokenManager
{
    // Token仅在内存中，应用重启后需重新登录
    string? AccessToken { get; }
    string? RefreshToken { get; }
    DateTime? AccessTokenExpiry { get; }
    
    void SetTokens(string accessToken, string refreshToken, DateTime expiry);
    void ClearTokens();
    bool IsTokenValid();
    bool IsTokenExpiringSoon(TimeSpan threshold);
}
```

**理由**：
- 符合医疗系统合规要求（Issue #1907）
- 避免Token泄露风险
- 简化安全模型

### 2. 自动登录令牌（AutoLoginToken）

**决策：引入AutoLoginToken替代保存密码**

```csharp
public interface ICredentialVault
{
    // 保存用户名（可选）
    string? SavedUsername { get; set; }
    
    // 自动登录令牌（非密码，由服务端生成）
    string? AutoLoginToken { get; set; }
    
    // 凭据完整性校验
    bool ValidateIntegrity();
    
    // 清除所有凭据
    void Clear();
}
```

**工作原理**：
1. 用户勾选"记住密码"并成功登录
2. 服务端返回AutoLoginToken（长期有效、可撤销）
3. 客户端使用DPAPI+HMAC存储AutoLoginToken
4. 下次启动时，使用AutoLoginToken请求登录
5. 服务端验证AutoLoginToken并返回正常AccessToken/RefreshToken

**优势**：
- 不存储用户密码
- AutoLoginToken可在服务端撤销
- 支持设备级别的访问控制

### 3. 登录状态机

**决策：使用状态机模式替代LoginCoordinator**

```csharp
public enum LoginState
{
    Idle,           // 未登录，等待用户操作
    Validating,     // 正在验证凭据
    Refreshing,     // 正在刷新Token
    Active,         // 已登录，会话活跃
    Expiring,       // 会话即将过期（显示警告）
    Expired         // 会话已过期
}

public interface ILoginStateMachine
{
    LoginState CurrentState { get; }
    
    Task<LoginResult> TriggerLogin(LoginCredentials credentials);
    Task<LoginResult> TriggerAutoLogin();
    Task TriggerLogout();
    Task TriggerRefresh();
    
    event EventHandler<StateChangedEventArgs> StateChanged;
}
```

**状态转换图**：

```
                    ┌─────────────────────────────────────────┐
                    │                                         │
                    ▼                                         │
┌──────┐     ┌────────────┐     ┌──────────┐     ┌─────────┐ │
│ Idle │────▶│ Validating │────▶│ Active   │────▶│Expiring │─┘
└──────┘     └────────────┘     └──────────┘     └─────────┘
    ▲              │                  │               │
    │              │                  │               │
    │              ▼                  ▼               ▼
    │         ┌─────────┐       ┌──────────┐    ┌─────────┐
    └─────────│  Idle   │◀──────│Refreshing│◀───│ Expired │
              │(失败)   │       └──────────┘    └─────────┘
              └─────────┘             │
                                      ▼
                                 ┌──────────┐
                                 │  Active  │
                                 │(刷新成功) │
                                 └──────────┘
```

### 4. 认证事件总线

**决策：使用Prism EventAggregator统一事件**

```csharp
// 事件定义
public class LoginStartedEvent : PubSubEvent<LoginStartedPayload> { }
public class LoginSucceededEvent : PubSubEvent<LoginSucceededPayload> { }
public class LoginFailedEvent : PubSubEvent<LoginFailedPayload> { }
public class SessionExpiringEvent : PubSubEvent<SessionExpiringPayload> { }
public class LogoutCompletedEvent : PubSubEvent<LogoutCompletedPayload> { }
public class TokenRefreshFailedEvent : PubSubEvent<TokenRefreshFailedPayload> { }

// 使用示例
public class SomeViewModel
{
    public SomeViewModel(IEventAggregator eventAggregator)
    {
        eventAggregator.GetEvent<SessionExpiringEvent>()
            .Subscribe(OnSessionExpiring);
    }
    
    private void OnSessionExpiring(SessionExpiringPayload payload)
    {
        // 保存未完成的工作
        // 提示用户
    }
}
```

### 5. Token刷新失败处理

**决策：分级处理策略**

```csharp
public enum TokenRefreshFailureReason
{
    NetworkError,           // 网络问题，可重试
    TokenExpired,           // Token已过期，需重新登录
    TokenRevoked,           // Token被撤销（可能在其他设备登出）
    ServerError,            // 服务端错误，可重试
    InvalidResponse         // 响应格式错误
}

public interface ITokenRefreshHandler
{
    Task<RefreshResult> HandleRefreshFailure(
        TokenRefreshFailureReason reason,
        int retryCount);
}
```

**处理策略**：

| 失败原因 | 处理方式 |
|----------|----------|
| NetworkError | 重试3次，指数退避 |
| TokenExpired | 尝试AutoLogin，失败则跳转登录 |
| TokenRevoked | 直接跳转登录，显示"已在其他设备登出" |
| ServerError | 重试2次，失败则提示用户 |
| InvalidResponse | 记录日志，跳转登录 |

### 6. 可靠Logout

**决策：本地优先+后台同步**

```csharp
public interface ILogoutService
{
    // 立即执行本地登出
    Task LogoutLocal();
    
    // 尝试服务端登出（可能失败）
    Task<bool> LogoutRemote();
    
    // 完整登出流程
    Task<LogoutResult> Logout();
}
```

**流程**：
1. 立即清除本地Token和凭据
2. 尝试通知服务端撤销RefreshToken
3. 如果服务端通知失败，记录到本地队列
4. 下次成功连接时重试队列中的登出请求

## File Structure Changes

### 新增文件

```
src/Client/Desktop/Core/LYBT.Desktop.Foundation/
├── Security/
│   ├── TokenManager.cs                 # 替代TokenStorageService
│   ├── CredentialVault.cs              # 替代SecureCredentialStorage
│   ├── LoginStateMachine.cs            # 新增状态机
│   ├── AuthenticationFacade.cs         # 新增统一入口
│   ├── TokenRefreshHandler.cs          # 新增刷新失败处理
│   └── LogoutService.cs                # 新增可靠登出
├── Events/
│   ├── AuthEvents.cs                   # 认证事件定义
│   └── AuthEventPayloads.cs            # 事件载荷
```

### 修改文件

```
src/Client/Desktop/Shell/Services/Login/
├── LoginCoordinator.cs                 # 重构为使用LoginStateMachine

src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/
├── LoginViewModel.cs                   # 简化，委托给AuthenticationFacade
```

### 删除文件

```
src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/
├── TokenStorageService.cs              # 替换为TokenManager
├── SecureCredentialStorage.cs          # 替换为CredentialVault
```

## API Changes

### 服务端新增API

```
POST /api/auth/auto-login
Request: { autoLoginToken: string }
Response: { accessToken, refreshToken, autoLoginToken(新) }
```

**说明**：客户端使用AutoLoginToken请求自动登录，服务端返回新的Token套件。

## Migration Strategy

### Phase 1: 基础设施
1. 新建TokenManager（保持与TokenStorageService兼容的接口）
2. 新建CredentialVault（支持读取旧格式凭据）
3. 渐进式替换引用

### Phase 2: 状态机
1. 实现LoginStateMachine
2. LoginCoordinator内部委托给状态机
3. 逐步移除LoginCoordinator旧代码

### Phase 3: 事件体系
1. 定义并发布认证事件
2. 现有组件订阅事件
3. 移除直接依赖

## Testing Strategy

### 单元测试

- TokenManager: Token生命周期管理
- CredentialVault: 凭据存储和完整性验证
- LoginStateMachine: 状态转换逻辑
- TokenRefreshHandler: 失败处理策略

### 集成测试

- 完整登录流程
- Token刷新流程
- 自动登录流程
- Logout可靠性

### 安全测试

- 凭据存储安全性
- Token内存清除
- 敏感信息日志检查
