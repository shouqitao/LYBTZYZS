# Delta: desktop-core-cleanup

## MODIFIED Requirements

### Requirement: DCC-002 Token管理统一

Desktop层 SHALL 使用Foundation.ITokenService作为唯一Token管理接口。

**变更内容**:
- 合并ITokenStorage和ITokenStorageService为ITokenService
- 合并SecureTokenStorage和TokenStorageService为TokenService
- 简化TokenLifecycleService，移除过度设计的状态机
- 合并ISecureCredentialStorage和IUsernameStorageService为ICredentialStorage

**删除的文件**:
- Foundation/Security/ITokenStorage.cs
- Foundation/Security/ITokenStorageService.cs
- Foundation/Security/SecureTokenStorage.cs
- Foundation/Security/TokenStorageService.cs
- Foundation/Security/ITokenLifecycleService.cs
- Foundation/Security/TokenLifecycleService.cs
- Foundation/Security/TokenLifecycleState.cs
- Foundation/Security/TokenLifecycleStateChangedEvent.cs
- Foundation/Security/IUsernameStorageService.cs
- Foundation/Security/UsernameStorageService.cs

**新增的文件**:
- Foundation/Security/ITokenService.cs
- Foundation/Security/TokenService.cs
- Foundation/Security/ICredentialStorage.cs

#### Scenario: 保存认证信息
- **WHEN** 登录成功需要保存Token
- **THEN** SHALL 调用ITokenService.SaveAuthenticationAsync()
- **AND** SHALL NOT 使用已废弃的ITokenStorage或ITokenStorageService

#### Scenario: 获取当前Token
- **WHEN** 需要获取Access Token
- **THEN** SHALL 调用ITokenService.GetAccessTokenAsync()
- **AND** SHALL 自动处理Token刷新

#### Scenario: 检查Token状态
- **WHEN** 需要检查Token有效性
- **THEN** SHALL 使用ITokenService.CurrentState属性
- **AND** 可用状态包括：None, Valid, Expiring, Expired

---

### Requirement: DCC-009 HTTP处理统一

Foundation/Http SHALL 整合所有HTTP相关代码。

**变更内容**:
- 移动Infrastructure/Http/ProblemDetailsParser.cs到Foundation/Http/
- 移动Infrastructure/Http/ProblemDetailsResponse.cs到Foundation/Http/
- 删除Infrastructure/Http/目录

**目录结构**:
```
Foundation/Http/
├── ApiService.cs
├── AuthorizationMessageHandler.cs
├── TokenRefreshHandler.cs
├── RetryPolicyExtensions.cs
├── ProblemDetailsParser.cs      # 从Infrastructure移入
└── ProblemDetailsResponse.cs    # 从Infrastructure移入
```

#### Scenario: 解析API错误响应
- **WHEN** API返回ProblemDetails格式错误
- **THEN** SHALL 使用Foundation.Http.ProblemDetailsParser
- **AND** SHALL NOT 从Infrastructure.Http引用

---

## ADDED Requirements

### Requirement: DCC-010 凭证存储统一

ICredentialStorage SHALL 统一管理Token和用户名存储。

**接口定义**:
```csharp
public interface ICredentialStorage
{
    // Token存储
    Task SaveLoginResponseAsync(LoginResponse response);
    Task<LoginResponse?> LoadLoginResponseAsync();
    Task ClearLoginResponseAsync();
    
    // 用户名记忆
    Task SaveUsernameAsync(string username);
    Task<string?> LoadUsernameAsync();
    Task ClearUsernameAsync();
}
```

**变更说明**:
- 合并ISecureCredentialStorage和IUsernameStorageService
- 使用Windows DPAPI进行加密存储

#### Scenario: 记住用户名
- **WHEN** 用户勾选"记住用户名"
- **THEN** SHALL 调用ICredentialStorage.SaveUsernameAsync()
- **AND** 下次启动自动填充

#### Scenario: 持久化Token
- **WHEN** 用户勾选"记住登录状态"
- **THEN** SHALL 调用ICredentialStorage.SaveLoginResponseAsync()
- **AND** 使用DPAPI加密存储到本地文件

---

### Requirement: DCC-011 ITokenService接口规范

ITokenService SHALL 提供统一的Token管理能力。

**接口定义**:
```csharp
public interface ITokenService
{
    // 存储操作
    Task SaveAuthenticationAsync(LoginResponse loginResponse, bool persist = false);
    Task<LoginResponse?> GetLoginResponseAsync();
    Task ClearAuthenticationAsync();
    
    // Token访问
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    
    // 生命周期
    Task<bool> IsTokenExpiredAsync();
    Task<TimeSpan?> GetRemainingTimeAsync();
    TokenState CurrentState { get; }
    
    // 事件
    event EventHandler<TokenStateChangedEventArgs>? StateChanged;
}

public enum TokenState
{
    None,       // 无Token
    Valid,      // Token有效
    Expiring,   // 即将过期（5分钟内）
    Expired     // 已过期
}
```

#### Scenario: 监听Token状态变化
- **WHEN** Token状态发生变化
- **THEN** SHALL 触发StateChanged事件
- **AND** 传递旧状态和新状态

#### Scenario: 自动Token刷新
- **WHEN** Token进入Expiring状态
- **THEN** TokenRefreshHandler SHALL 自动尝试刷新
- **AND** 刷新成功后状态变为Valid
