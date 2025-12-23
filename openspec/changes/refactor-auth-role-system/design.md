# Technical Design: 用户认证与角色系统重构

## Context

### 背景
LYBTZYZS是一个中医诊所管理系统，当前认证系统经历多次迭代，积累了技术债务。系统需要支持多角色（SuperAdmin、Admin、Doctor、Receptionist等）的灵活权限管理。

### 约束条件
- 必须保持向后兼容，现有用户无感知迁移
- 遵循项目三层对齐架构（Desktop MVVM + Server DDD）
- 符合MVP原则，避免过度设计
- 认证逻辑需同时支持Desktop WPF客户端和Web API

### 利益相关者
- **用户**：需要稳定、安全的登录体验
- **开发者**：需要易于扩展的角色系统
- **运维**：需要可审计的安全日志

## Goals / Non-Goals

### Goals
1. 消除认证流程中的异步反模式，提升响应性能
2. 建立可扩展的角色注册机制，新角色仅需配置
3. 增强Token安全性，防止重放攻击
4. 统一错误处理，提升用户体验
5. 添加Receptionist角色作为可扩展模板

### Non-Goals
1. 不引入OAuth2.0/OpenID Connect外部认证（后续独立提案）
2. 不实现细粒度权限（Feature-level Permission），保持Role-based
3. 不改变现有数据库Schema中的User表结构
4. 不实现多租户支持

## Decisions

### D0: 修复UI导航问题

**问题**：用户密码修改和用户信息修改功能在UI层面无法打开

**分析**：
1. `MenuManager.ExecuteEditProfile()` 调用 `NavigationManager.NavigateTo("UserProfileView")`
2. `MenuManager.ExecuteChangePassword()` 调用 `NavigationManager.NavigateTo("ChangePasswordView")`
3. 导航失败时仅记录日志，用户无感知

**可能原因**：
- ViewModel 依赖注入失败（构造函数参数未正确注册）
- 导航回调异常被静默处理
- 模块加载顺序问题

**解决方案**：
```csharp
// NavigationManager.cs - 改进错误处理
public void NavigateTo(string viewName)
{
    try
    {
        _logger.LogInformation("导航到 {ViewName}", viewName);
        _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
        {
            if (result.Result != true)
            {
                var errorMessage = result.Error?.Message ?? "未知错误";
                _logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, errorMessage);

                // 添加用户友好的错误提示
                _userNotificationService?.ShowErrorAsync($"无法打开 {viewName}：{errorMessage}");
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
        _userNotificationService?.ShowErrorAsync($"导航失败：{ex.Message}");
    }
}
```

### D1: 统一状态机架构

**决策**：将`LoginStateMachine`和`LoginFlowState`合并为`AuthenticationStateMachine`

**理由**：
- 当前双状态机导致状态同步问题（IssueRef: analysis-report）
- 单一状态机降低认知负担，减少bug
- 符合单一职责原则

**实现**：
```csharp
public class AuthenticationStateMachine
{
    public AuthState CurrentState { get; private set; }

    public enum AuthState
    {
        Idle,           // 初始状态
        Authenticating, // 认证中
        ValidatingToken,// Token验证中
        LoadingProfile, // 加载用户配置
        LoadingModules, // 加载角色模块
        Authenticated,  // 已认证
        Failed,         // 认证失败
        LoggingOut      // 登出中
    }

    public async Task<AuthResult> TransitionAsync(AuthEvent evt)
    {
        // 状态转换逻辑
    }
}
```

### D2: 可扩展角色注册机制

**决策**：采用接口+注册表模式，而非反射自动发现

**理由**：
- 反射增加启动时间，对WPF应用影响大
- 显式注册便于控制加载顺序
- 配置驱动更易于运维调整

**实现**：
```csharp
public interface IRoleDefinition
{
    UserRole Role { get; }
    string DisplayName { get; }
    string HomeViewName { get; }
    IEnumerable<string> RequiredModules { get; }
    bool IsEnabled { get; }
}

public class RoleRegistry
{
    private readonly Dictionary<UserRole, IRoleDefinition> _roles = new();

    public void Register(IRoleDefinition definition) { ... }
    public IRoleDefinition GetDefinition(UserRole role) { ... }
    public IEnumerable<string> GetModulesForRole(UserRole role) { ... }
}

// 配置示例 (appsettings.json)
{
  "Roles": {
    "Receptionist": {
      "Enabled": true,
      "DisplayName": "前台",
      "HomeView": "ReceptionistHomeView",
      "Modules": ["PatientsModule"]
    }
  }
}
```

### D3: Token安全增强策略

**决策**：实现Token家族追踪 + 设备绑定

**理由**：
- 简单的RefreshToken轮换无法检测重放攻击
- 设备绑定增加攻击难度，同时保持用户体验
- 行业标准实践（参考OAuth 2.0 Security BCP）

**实现**：
```csharp
public class TokenFamily
{
    public Guid FamilyId { get; init; }
    public string UserId { get; init; }
    public string DeviceFingerprint { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CurrentRefreshToken { get; set; }
    public bool IsRevoked { get; set; }
}

public class JwtService
{
    public async Task<TokenPair> RefreshTokenAsync(string refreshToken, string deviceFingerprint)
    {
        var family = await _tokenFamilyRepository.GetByTokenAsync(refreshToken);

        // 检测重放攻击
        if (family.CurrentRefreshToken != refreshToken)
        {
            // Token已被使用过，可能是重放攻击
            await RevokeTokenFamilyAsync(family.FamilyId);
            throw new SecurityTokenException("Token replay detected");
        }

        // 验证设备指纹
        if (family.DeviceFingerprint != deviceFingerprint)
        {
            throw new SecurityTokenException("Device mismatch");
        }

        // 生成新Token对
        var newTokenPair = GenerateTokenPair(family);
        family.CurrentRefreshToken = newTokenPair.RefreshToken;
        await _tokenFamilyRepository.UpdateAsync(family);

        return newTokenPair;
    }
}
```

### D4: 统一错误处理

**决策**：创建`AuthenticationErrorHandler`集中处理所有认证错误

**理由**：
- 当前错误处理分散在8+个文件
- 用户体验不一致（有些显示技术错误，有些静默失败）
- 便于统一日志记录和安全审计

**实现**：
```csharp
public interface IAuthenticationErrorHandler
{
    Task HandleErrorAsync(AuthenticationException ex, AuthContext context);
}

public class AuthenticationErrorHandler : IAuthenticationErrorHandler
{
    public async Task HandleErrorAsync(AuthenticationException ex, AuthContext context)
    {
        // 1. 记录安全审计日志
        await _auditLogger.LogAuthFailureAsync(ex, context);

        // 2. 根据错误类型返回用户友好消息
        var userMessage = ex switch
        {
            InvalidCredentialsException => "用户名或密码错误",
            AccountLockedException => $"账户已锁定，请{ex.UnlockTime}后重试",
            TokenExpiredException => "登录已过期，请重新登录",
            DeviceMismatchException => "设备验证失败，请重新登录",
            _ => "登录失败，请稍后重试"
        };

        // 3. 通知UI层
        await _eventAggregator.PublishAsync(new AuthErrorEvent(userMessage));
    }
}
```

### D5: Receptionist角色实现

**决策**：创建独立的`LYBT.Desktop.Receptionist`模块

**理由**：
- 遵循项目"一个角色一个项目"的架构约定
- 作为新角色添加的模板，验证可扩展性
- 独立模块便于后续功能迭代

**实现**：
```
src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/
├── ReceptionistModule.cs           # Prism模块定义
├── ViewModels/
│   └── ReceptionistHomeViewModel.cs
├── Views/
│   └── ReceptionistHomeView.xaml   # 显示"功能开发中"
└── LYBT.Desktop.Receptionist.csproj
```

## Alternatives Considered

### A1: 使用现有状态机，仅修复同步问题
**优点**：改动最小
**缺点**：双状态机的根本问题未解决，后续维护成本高
**结论**：拒绝，技术债务需要解决

### A2: 使用反射自动发现角色模块
**优点**：无需手动注册
**缺点**：启动时间增加，调试困难
**结论**：拒绝，显式注册更可控

### A3: 使用第三方认证库（如IdentityServer）
**优点**：功能完善，标准合规
**缺点**：引入重依赖，学习成本高，偏离MVP原则
**结论**：推迟，作为后续独立提案

## Risks / Trade-offs

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 状态机迁移破坏现有登录流程 | Medium | High | 保留旧状态机作为fallback，Feature Flag控制 |
| Token有效期缩短导致频繁刷新 | Low | Medium | 实现静默刷新，距过期2分钟自动刷新 |
| 设备指纹不稳定导致误判 | Medium | Medium | 设备指纹仅作为辅助验证，主要依赖Token家族 |
| Receptionist模块加载失败 | Low | Low | 模块独立，失败不影响其他角色 |

## Migration Plan（直接重构）

### 部署步骤
1. 执行数据库迁移（添加TokenFamily表）
2. 部署新版本代码
3. 所有用户需重新登录（旧Token全部失效）

### Rollback Plan
```bash
# 如发现严重问题，回滚到上一版本
git revert HEAD
# 数据库回滚迁移
dotnet ef database update <previous-migration>
```

**注意**: 直接重构方案不保留兼容层，回滚需要完整版本回退。

## Architecture Diagrams

### 认证流程（重构后）
```
┌────────────┐     ┌─────────────────────┐     ┌───────────────┐
│   UI层     │────>│ AuthenticationState │────>│  JwtService   │
│ LoginView  │     │     Machine         │     │  (Server)     │
└────────────┘     └─────────────────────┘     └───────────────┘
      │                     │                         │
      │              ┌──────▼──────┐                  │
      │              │ RoleRegistry │                  │
      │              └──────┬──────┘                  │
      │                     │                         │
      ▼                     ▼                         ▼
┌────────────┐     ┌───────────────┐     ┌───────────────────┐
│ Module     │<────│ Permission    │<────│ TokenFamily       │
│ Loader     │     │ Gateway       │     │ Repository        │
└────────────┘     └───────────────┘     └───────────────────┘
```

### Token生命周期
```
Login Request
      │
      ▼
┌──────────────────────────────────────────────────────┐
│  Generate TokenFamily                                 │
│  - FamilyId: new Guid                                │
│  - DeviceFingerprint: hash(userAgent + screenRes)    │
│  - AccessToken: 15min expiry                         │
│  - RefreshToken: 7day expiry                         │
└──────────────────────────────────────────────────────┘
      │
      ▼ (使用中)
┌──────────────────────────────────────────────────────┐
│  Access Token Expired                                 │
│  Client calls /refresh with RefreshToken             │
└──────────────────────────────────────────────────────┘
      │
      ▼
┌──────────────────────────────────────────────────────┐
│  Validate & Rotate                                    │
│  1. Check RefreshToken == CurrentRefreshToken        │
│  2. Check DeviceFingerprint match                    │
│  3. Generate new AccessToken + RefreshToken          │
│  4. Update CurrentRefreshToken in DB                 │
└──────────────────────────────────────────────────────┘
      │
      ▼ (检测到重放)
┌──────────────────────────────────────────────────────┐
│  Replay Attack Detected                              │
│  - Revoke entire TokenFamily                         │
│  - Log security event                                │
│  - Force user re-login                               │
└──────────────────────────────────────────────────────┘
```

## Open Questions

1. **Q: Token黑名单是使用内存缓存还是数据库？**
   - 倾向：内存缓存（Redis/MemoryCache），定期持久化
   - 待确认：生产环境是否部署Redis

2. **Q: 设备指纹的组成元素？**
   - 当前考虑：UserAgent + ScreenResolution + Platform
   - 待确认：是否需要更强的设备绑定（如硬件ID）

3. **Q: Receptionist角色的初始权限范围？**
   - 当前方案：仅Patient模块只读
   - 待确认：是否需要挂号相关功能的写权限

## References

- [Microsoft: Configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [OAuth 2.0 Security Best Current Practice](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- 项目内部：`docs/architecture/desktop-mvvm.md`
- 项目内部：`docs/architecture/server-ddd.md`
