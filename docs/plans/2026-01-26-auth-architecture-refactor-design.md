# 认证架构重构设计文档

**创建日期**: 2026-01-26
**状态**: 待实施
**OpenSpec**: 待创建

---

## 1. 设计目标

基于现有OpenSpec规范，整合用户场景决策，简化实现代码。

| 指标 | 当前 | 目标 |
|------|------|------|
| 代码行数 | ~5800行 | ~1500行 |
| 服务数量 | 6个 | 3个 |
| 状态数量 | 6个 | 5个（移除Expiring独立状态） |

---

## 2. 状态模型

### 2.1 状态定义（简化版）

```
┌─────────────────────────────────────────────────────────────┐
│  状态机                                                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│    ┌──────┐   提交凭据   ┌────────────┐   验证成功   ┌──────┐
│    │ Idle │────────────▶│ Validating │────────────▶│Active│
│    └──┬───┘             └─────┬──────┘             └──┬───┘
│       ▲                       │                       │
│       │验证失败/登出完成       │                       │
│       └───────────────────────┘                       │
│       ▲                                               │
│       │                   Token即将过期需刷新          │
│       │              ┌────────────────────────────────┘
│       │              ▼
│       │        ┌────────────┐   刷新成功
│       │        │ Refreshing │──────────────▶ Active
│       │        └─────┬──────┘
│       │              │刷新失败(非网络)
│       │              ▼
│       │        ┌─────────┐
│       │        │ Expired │
│       │        └────┬────┘
│       └─────────────┘
│
│  注：网络问题时进入"优雅降级"模式，不转换状态
└─────────────────────────────────────────────────────────────┘
```

### 2.2 状态说明

| 状态 | 说明 | 允许的转换 |
|------|------|-----------|
| Idle | 未登录，等待用户操作 | → Validating |
| Validating | 正在验证凭据 | → Active, → Idle |
| Active | 已登录，会话活跃 | → Refreshing, → Idle |
| Refreshing | 正在刷新Token | → Active, → Expired |
| Expired | 会话已过期 | → Idle |

### 2.3 与原规范差异

| 原规范 | 本设计 | 原因 |
|--------|--------|------|
| 6个状态（含Expiring） | 5个状态 | 移除警告对话框，静默处理超时 |
| Expiring显示警告对话框 | 直接超时logout | 用户决策：无感操作 |

---

## 3. 核心服务架构

### 3.1 服务结构

```
┌─────────────────────────────────────────────────────────────┐
│  Shell/App.xaml.cs (DI注册)                                 │
├─────────────────────────────────────────────────────────────┤
│  containerRegistry.RegisterSingleton<IAuthService,         │
│                                       AuthService>();       │
│  containerRegistry.RegisterSingleton<ICredentialVault,     │
│                                       CredentialVault>();   │
│  containerRegistry.RegisterSingleton<ITokenManager,        │
│                                       TokenManager>();      │
│  containerRegistry.RegisterSingleton<IUserActivityTracker, │
│                                       UserActivityTracker>();│
└─────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│  IAuthService（统一门面）                                    │
├─────────────────────────────────────────────────────────────┤
│  • 管理登录状态机                                           │
│  • 协调其他服务                                             │
│  • 提供统一的认证API                                        │
└─────────────────────────────────────────────────────────────┘
          │
          ├──────────────────┬──────────────────┐
          ▼                  ▼                  ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
│ ICredentialVault│ │ ITokenManager   │ │ IUserActivityTracker│
├─────────────────┤ ├─────────────────┤ ├─────────────────────┤
│ 凭据持久化存储   │ │ Token内存管理    │ │ 用户活动监控        │
│ DPAPI加密       │ │ 刷新逻辑         │ │ 超时检测            │
│ HMAC校验        │ │ 有效性检查       │ │                     │
└─────────────────┘ └─────────────────┘ └─────────────────────┘
```

### 3.2 服务职责

| 服务 | 职责 | 对应规范 |
|------|------|---------|
| IAuthService | 统一门面，状态机管理 | LSM-001~004 |
| ICredentialVault | AutoLoginToken加密存储 | CVT-001~004 |
| ITokenManager | AccessToken/RefreshToken内存管理 | TKM-001~003 |
| IUserActivityTracker | 用户活动监控，超时检测 | AUTH-001~003 |

---

## 4. 场景设计

### 4.1 场景1：日常首次登录

```
应用启动
    │
    ▼
CredentialVault.HasAutoLoginToken()?
    │
┌───┴───┐
│       │
▼ 是    ▼ 否
显示    显示登录界面
登录    ├─▶ 用户名：显示上次保存的用户名
界面    ├─▶ 密码：空
│       └─▶ □ 记住密码
│
▼
用户点击登录按钮
    │
┌───┴───────────────┐
│                   │
▼ 有AutoLoginToken  ▼ 无AutoLoginToken
使用AutoLogin API   使用密码登录API
    │                   │
    └───────┬───────────┘
            ▼
       登录成功？
            │
    ┌───────┴───────┐
    │               │
    ▼ 是            ▼ 否
进入主界面      显示错误提示
State=Active    清除AutoLoginToken(如有)
```

**关键规则**：
- 有AutoLoginToken时仍需点击登录按钮触发
- `□ 记住密码` = 启用AutoLoginToken
- 不存储明文密码

### 4.2 场景2：主动登出 / 退出应用

```
用户操作
    │
    ├─▶ 点击"退出登录"按钮
    └─▶ 关闭应用窗口
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│ AuthService.LogoutAsync()                                   │
│                                                             │
│ 1. 清除内存Token (AccessToken, RefreshToken)                │
│ 2. 保留AutoLoginToken（不改变"记住密码"状态）               │
│ 3. 保留Username                                             │
│ 4. State → Idle                                             │
│ 5. 后台通知服务端登出                                        │
│    - 成功：完成                                             │
│    - 失败(网络问题)：加入待处理队列                          │
└─────────────────────────────────────────────────────────────┘
            │
            ▼
    ┌───────┴───────┐
    │               │
    ▼               ▼
退出登录        关闭应用
导航到登录页    Application.Shutdown()
```

**核心原则**：logout不改变"记住密码"状态，只有用户主动取消才改变。

### 4.3 场景3：超时登出

```
UserActivityTracker
    │
    ├─▶ 监听用户输入（键盘、鼠标点击、滚轮）
    ├─▶ 每60秒检查不活跃时间
    │
    ▼
不活跃时间 >= 15分钟？
    │
┌───┴───┐
│       │
▼ 是    ▼ 否
触发    继续监听
超时
登出
    │
    ▼
AuthService.LogoutAsync()
（与主动登出相同逻辑，保留AutoLoginToken）
    │
    ▼
导航到登录页面
提示："会话已过期，请重新登录"
```

**设计要点**：无警告对话框，静默等待超时后直接登出。

### 4.4 场景4：多账号 / 切换账号

```
登录界面状态：
- 当前保存：Username="张医生", HasAutoLoginToken=true

用户在用户名输入框输入（任何输入）
    │
    ▼
CredentialVault.ClearAutoLoginToken()
    │
    ▼
用户界面更新：
- "记住密码"复选框 → 未勾选
- 密码框 → 清空
- 用户需手动输入账号和密码
    │
    ▼
登录成功后：
- 保存新Username
- 如果勾选"记住密码" → 保存新AutoLoginToken
```

**触发条件**：用户名输入框**任何输入**即清除AutoLoginToken。

### 4.5 场景5：网络断开处理（优雅降级）

```
Token刷新失败
    │
    ▼
检测失败原因
    │
    ├─── 网络错误 ───────────────────────────────────────────┐
    │                                                        │
    │    ┌──────────────────────────────────────────────────┐│
    │    │ 优雅降级模式                                     ││
    │    │ • 保持当前状态（不转换到Expired）                ││
    │    │ • 显示"网络断开，部分功能受限"提示              ││
    │    │ • 本地缓存数据可查看                            ││
    │    │ • 网络恢复后自动重试刷新Token                   ││
    │    │ • 若Token已绝对过期(30天) → 强制logout          ││
    │    └──────────────────────────────────────────────────┘│
    │                                                        │
    ├─── Token过期(非网络问题) ──────────────────────────────┤
    │                                                        │
    │    ┌──────────────────────────────────────────────────┐│
    │    │ 尝试AutoLogin                                    ││
    │    │ • 有AutoLoginToken → 尝试自动登录               ││
    │    │   - 成功：继续使用                              ││
    │    │   - 失败：logout到登录页                        ││
    │    │ • 无AutoLoginToken → 直接logout                 ││
    │    └──────────────────────────────────────────────────┘│
    │                                                        │
    └─── Token撤销 ──────────────────────────────────────────┤
                                                             │
         ┌──────────────────────────────────────────────────┐│
         │ 强制logout                                       ││
         │ • 清除本地Token                                  ││
         │ • 清除AutoLoginToken                            ││
         │ • 显示提示（如"账号已在其他设备登录"）          ││
         │ • 导航到登录页面                                ││
         └──────────────────────────────────────────────────┘│
```

### 4.6 场景6：Token静默刷新

```
HTTP请求拦截器
    │
    ▼
发送API请求前检查Token
    │
    ▼
TokenManager.IsExpiring(threshold: 5min)?
    │
┌───┴───┐
│       │
▼ 是    ▼ 否
后台    使用当前Token发送请求
静默
刷新
    │
    ▼
刷新成功？
    │
┌───┴───┐
│       │
▼ 是    ▼ 否
更新    按场景5处理
内存
Token
```

**Token有效期**：
- AccessToken: 1小时
- RefreshToken: 7天
- 绝对过期: 30天（即使持续活跃也必须重新登录）

### 4.7 场景7：服务端logout失败队列处理

```
下次登录成功
    │
    ▼
检查待处理队列
    │
    ▼
有待处理的logout请求？
    │
┌───┴───┐
│       │
▼ 是    ▼ 否
后台    完成
静默
处理
    │
    ├─▶ 调用服务端logout API撤销旧Token
    ├─▶ 成功：从队列移除
    └─▶ 失败：保留在队列，下次再试
```

---

## 5. 存储设计

### 5.1 存储分层

```
┌─────────────────────────────────────────────────────────────┐
│  内存层（Token）                                            │
├─────────────────────────────────────────────────────────────┤
│  TokenManager                                               │
│    _accessToken: string?                                    │
│    _refreshToken: string?                                   │
│    _expiresAt: DateTime?                                    │
│                                                             │
│  ✗ 不持久化到任何位置                                       │
│  ✗ 应用重启/logout自动清除                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  持久层（凭据）                                             │
├─────────────────────────────────────────────────────────────┤
│  CredentialVault                                            │
│    存储位置: %APPDATA%\LYBT\credentials.dat                 │
│                                                             │
│  数据结构:                                                  │
│  {                                                          │
│    "Username": "张医生",       // 明文                      │
│    "AutoLoginToken": "加密值", // DPAPI加密                 │
│    "HMAC": "校验值"            // HMAC-SHA256              │
│  }                                                          │
│                                                             │
│  安全措施:                                                  │
│  ✓ DPAPI加密（用户级，绑定Windows账户）                     │
│  ✓ HMAC完整性校验（防篡改）                                 │
│  ✓ 校验失败自动删除                                         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  队列层（待处理logout）                                     │
├─────────────────────────────────────────────────────────────┤
│  存储位置: %APPDATA%\LYBT\pending_logout.dat                │
│                                                             │
│  数据结构:                                                  │
│  [                                                          │
│    { "refreshToken": "xxx", "failedAt": "2026-01-26" }      │
│  ]                                                          │
│                                                             │
│  处理时机: 下次登录成功后                                   │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 生命周期对比

| 数据项 | 生命周期 | 清除时机 |
|--------|---------|---------|
| Username | 持久 | 用户切换账号后登录成功 |
| AutoLoginToken | 持久 | 用户取消"记住密码" / 用户名输入框有输入 / Token被撤销 |
| AccessToken | 会话（内存） | logout / 应用关闭 / 过期 |
| RefreshToken | 会话（内存） | logout / 应用关闭 / 绝对过期 |

---

## 6. 接口定义

### 6.1 IAuthService

```csharp
/// <summary>
/// 统一认证服务接口
/// </summary>
public interface IAuthService
{
    // === 状态 ===

    /// <summary>当前登录状态</summary>
    LoginState CurrentState { get; }

    /// <summary>是否已认证</summary>
    bool IsAuthenticated { get; }

    /// <summary>当前用户名</summary>
    string? CurrentUsername { get; }

    /// <summary>是否处于优雅降级模式（网络断开）</summary>
    bool IsInGracefulDegradation { get; }

    // === 事件 ===

    /// <summary>状态变更事件</summary>
    event EventHandler<StateChangedEventArgs>? StateChanged;

    /// <summary>登出完成事件</summary>
    event EventHandler? LogoutCompleted;

    /// <summary>网络状态变更事件</summary>
    event EventHandler<NetworkStatusEventArgs>? NetworkStatusChanged;

    // === 操作 ===

    /// <summary>
    /// 使用密码登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="rememberMe">是否记住密码（启用自动登录）</param>
    Task<AuthResult> LoginAsync(string username, string password, bool rememberMe);

    /// <summary>
    /// 使用AutoLoginToken自动登录
    /// </summary>
    Task<AuthResult> AutoLoginAsync();

    /// <summary>
    /// 登出
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// 检查是否可以自动登录
    /// </summary>
    bool CanAutoLogin();
}
```

### 6.2 ICredentialVault

```csharp
/// <summary>
/// 凭据保管接口（CVT-001~004）
/// </summary>
public interface ICredentialVault
{
    // === 用户名 ===

    /// <summary>加载保存的用户名</summary>
    string? LoadUsername();

    /// <summary>保存用户名</summary>
    void SaveUsername(string username);

    /// <summary>清除用户名</summary>
    void ClearUsername();

    // === AutoLoginToken ===

    /// <summary>是否有AutoLoginToken</summary>
    bool HasAutoLoginToken();

    /// <summary>加载AutoLoginToken（解密）</summary>
    string? LoadAutoLoginToken();

    /// <summary>保存AutoLoginToken（加密）</summary>
    void SaveAutoLoginToken(string token);

    /// <summary>清除AutoLoginToken</summary>
    void ClearAutoLoginToken();

    // === 完整清除 ===

    /// <summary>清除所有凭据（Token撤销时使用）</summary>
    void ClearAll();
}
```

### 6.3 ITokenManager

```csharp
/// <summary>
/// Token管理接口（TKM-001~003）
/// </summary>
public interface ITokenManager
{
    // === 状态 ===

    /// <summary>是否有有效Token</summary>
    bool HasValidToken { get; }

    /// <summary>AccessToken（用于API调用）</summary>
    string? AccessToken { get; }

    /// <summary>Token是否即将过期</summary>
    bool IsExpiring(TimeSpan threshold);

    /// <summary>Token是否已绝对过期（30天）</summary>
    bool IsAbsolutelyExpired { get; }

    // === 操作 ===

    /// <summary>设置Token（登录成功后调用）</summary>
    void SetTokens(string accessToken, string refreshToken, DateTime expiresAt);

    /// <summary>刷新Token</summary>
    Task<TokenRefreshResult> RefreshAsync();

    /// <summary>清除Token</summary>
    void Clear();
}

/// <summary>
/// Token刷新结果
/// </summary>
public record TokenRefreshResult(
    bool Success,
    TokenRefreshFailureReason? FailureReason = null
);

/// <summary>
/// Token刷新失败原因
/// </summary>
public enum TokenRefreshFailureReason
{
    NetworkError,    // 网络错误（可降级）
    TokenExpired,    // Token过期（尝试AutoLogin）
    TokenRevoked     // Token撤销（强制logout）
}
```

### 6.4 LoginState枚举

```csharp
/// <summary>
/// 登录状态枚举
/// </summary>
public enum LoginState
{
    /// <summary>未登录，等待用户操作</summary>
    Idle,

    /// <summary>正在验证凭据</summary>
    Validating,

    /// <summary>已登录，会话活跃</summary>
    Active,

    /// <summary>正在刷新Token</summary>
    Refreshing,

    /// <summary>会话已过期</summary>
    Expired
}
```

---

## 7. 规范更新清单

本设计与现有规范存在以下差异，实施时需同步更新规范：

| 规范文件 | 更新内容 |
|---------|---------|
| `login-state-machine/spec.md` | 移除Expiring状态，简化为5状态 |
| `authentication/spec.md` | AUTH-003改为静默超时，移除警告对话框 |
| `credential-vault/spec.md` | CVT-004更新：logout不清除AutoLoginToken，仅用户主动取消时清除 |

---

## 8. 代码精简方案

### 8.1 文件变更

| 当前文件 | 行数 | 精简后 | 说明 |
|---------|------|--------|------|
| AuthService | 400 | 300 | 统一门面 |
| CredentialVault | 300 | 200 | 简化加密逻辑 |
| TokenManager | 200 | 150 | 内存存储 |
| UserActivityTracker | 300 | 250 | 移除警告逻辑 |
| LoginViewModel | 500 | 300 | 简化状态绑定 |
| 其他（删除） | 4100 | 0 | 移除冗余 |
| **合计** | **5800** | **~1200** | **-79%** |

### 8.2 删除清单

- `SecureCredentialStorage.cs` → 合并到CredentialVault
- `CredentialMigrationService.cs` → 简化迁移逻辑内联
- `LoginStateManager.cs` → 合并到AuthService
- `TokenRefreshService.cs` → 合并到TokenManager
- `SessionExpiringDialog.xaml` → 删除（静默处理）
- 多余的事件类和DTO → 使用record简化

---

## 9. 实施阶段

| 阶段 | 工作内容 | 预估 |
|------|---------|------|
| Phase 1 | IAuthService统一门面 + 状态机 | 2-3小时 |
| Phase 2 | ICredentialVault重构 | 1-2小时 |
| Phase 3 | ITokenManager重构 | 1-2小时 |
| Phase 4 | LoginViewModel适配 | 1-2小时 |
| Phase 5 | 删除冗余代码 + 更新规范 | 2-3小时 |
| Phase 6 | 测试验证 | 2-3小时 |

---

## 10. 决策记录

| 序号 | 决策点 | 决策 | 原因 |
|------|--------|------|------|
| 1 | 超时警告 | 静默登出，无警告对话框 | 用户偏好无感操作 |
| 2 | logout保留状态 | 保留AutoLoginToken | 用户决策 |
| 3 | 用户名输入触发 | 任何输入即清除AutoLoginToken | 用户决策 |
| 4 | 网络断开处理 | 优雅降级，保持本地会话 | 用户决策 |
| 5 | logout后登录页 | 需点击按钮触发登录 | 给用户控制权 |
| 6 | Token撤销处理 | 清除所有凭据 + 提示原因 | 安全考虑 |
| 7 | 服务端logout失败 | 下次登录成功后处理 | 简单可靠 |

---

**文档版本**: v1.0
**创建者**: Claude Code
**最后更新**: 2026-01-26
