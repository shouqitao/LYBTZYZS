# Design: 认证与用户模块集成重构

## Context

### 背景
LYBTZYZS项目使用JWT认证，Server端通过AuthService生成Token，Desktop客户端通过AuthenticationService管理认证状态。当前实现存在以下技术债务：

1. **Token过期处理不一致** - Server端LogoutAsync要求有效Token，但Token可能已过期
2. **职责边界模糊** - AuthService直接依赖UserRepository，密码验证逻辑重复
3. **错误码不统一** - 使用字符串Message而非结构化错误码

### 约束
- 必须保持向后兼容(现有客户端能继续工作)
- 不引入新的外部依赖(保持技术栈简洁)
- 遵循项目三层架构规范(ADR-002)

### 安全约束(医疗系统合规)
- **Logout后Token必须失效** - 服务端必须撤销RefreshToken，AccessToken自然过期
- **Logout后必须重新登录** - 客户端清除所有认证信息，用户必须输入密码执行Login操作
- **不支持"记住登录状态"** - 即使客户端保存了密码，也必须执行完整Login流程获取新Token
- **Token仅存内存** - 不持久化到磁盘，应用关闭即失效(Issue #1907)

### 利益相关者
- Desktop客户端开发
- Server端API开发
- 系统管理员(会话配置)

## Goals / Non-Goals

### Goals
1. **G1**: Token生命周期管理规范化 - 从创建到过期有明确状态转换
2. **G2**: Auth/User模块职责清晰 - 认证归Auth，用户管理归User
3. **G3**: 统一错误处理 - 结构化错误码，一致的异常处理
4. **G4**: 客户端认证体验提升 - 优雅处理Token过期、网络断开

### Non-Goals
- 不引入OAuth/OIDC (保持简单JWT认证)
- 不实现多因素认证 (后续独立提案)
- 不修改数据库Schema (仅代码层重构)

## Decisions

### D1: Token过期登出策略

**决策**: 登出API允许过期Token调用，仅清除服务端会话记录

**理由**:
- 避免Refit.ApiException异常
- 用户体验更友好(总能成功登出)
- 服务端会话记录仍需清理(安全)

**实现**:
```csharp
// AuthController.cs
[AllowAnonymous] // 允许未认证调用
[HttpPost("logout")]
public async Task<ApiResponse> LogoutAsync([FromBody] LogoutRequest request)
{
    // 尝试从Header获取Token(可选)
    var token = GetTokenFromHeader();

    // 即使Token无效或过期，也清除服务端会话
    await _authService.LogoutAsync(request.Username, request.RefreshToken);

    return ApiResponse.Ok("登出成功");
}
```

**替代方案**:
- A: 客户端检查Token过期后跳过API调用 (已临时实现，但不清理服务端会话)
- B: 使用RefreshToken进行登出认证 (增加复杂度)

### D2: Auth/User职责划分

**决策**: 采用"认证/身份"分离模式

| 职责 | 归属模块 |
|------|----------|
| 登录/登出 | Auth |
| Token生成/验证 | Auth |
| 会话管理 | Auth |
| 用户CRUD | User |
| 密码策略验证 | User |
| 密码修改(业务) | User + Auth协作 |

**理由**:
- 符合单一职责原则
- 减少模块间耦合
- 便于独立测试

**实现**:
```csharp
// Auth模块：只负责凭据验证
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string username, string? refreshToken);
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken);
}

// User模块：提供用户查询和密码服务
public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<bool> ValidatePasswordAsync(Guid userId, string password);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
```

### D3: 统一错误码体系

**决策**: 引入AuthErrorCode枚举

```csharp
public enum AuthErrorCode
{
    None = 0,

    // 认证错误 1xx
    InvalidCredentials = 101,
    UserNotFound = 102,
    UserDisabled = 103,
    PasswordExpired = 104,

    // Token错误 2xx
    TokenExpired = 201,
    TokenInvalid = 202,
    TokenRevoked = 203,
    RefreshTokenExpired = 204,

    // 会话错误 3xx
    SessionNotFound = 301,
    SessionExpired = 302,
    ConcurrentSessionLimit = 303,

    // 系统错误 9xx
    InternalError = 901,
    ServiceUnavailable = 902
}
```

**理由**:
- 便于客户端统一处理
- 支持国际化(错误码映射消息)
- 便于日志分析和监控

### D4: 客户端Token生命周期状态机

**决策**: 实现TokenLifecycleService管理状态

```
┌──────────┐    登录成功    ┌──────────┐
│ NotAuth  │───────────────→│  Active  │
└──────────┘                └────┬─────┘
      ↑                          │
      │                     Token即将过期
      │                          ↓
      │                    ┌──────────┐
      │                    │ Warning  │──→ 用户选择保持登录 ──→ Active
      │                    └────┬─────┘
      │                         │
      │                    超时/用户登出
      │                         ↓
      │    清除凭据       ┌──────────┐
      └───────────────────│ Expired  │
                          └──────────┘
```

## Risks / Trade-offs

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 向后兼容性 | 现有客户端可能无法处理新错误码 | 保持旧API响应格式，新增字段扩展 |
| 重构范围大 | 引入新Bug风险 | 分Phase实施，每Phase独立测试 |
| 登出允许过期Token | 潜在安全风险(伪造登出) | 使用RefreshToken或UserId验证 |

## Migration Plan

### Phase 1: Server端错误码统一 (低风险)
1. 添加AuthErrorCode枚举
2. 修改AuthService返回结构化错误
3. 保持API响应格式兼容

### Phase 2: 登出流程重构 (中风险)
1. 修改LogoutAsync允许过期Token
2. 使用RefreshToken作为登出凭据
3. 更新客户端AuthenticationService

### Phase 3: Token生命周期服务 (中风险)
1. 实现TokenLifecycleService
2. 集成到Desktop Shell
3. 实现状态转换和事件通知

### Phase 4: Auth/User职责重构 (高风险)
1. 提取密码验证到UserService
2. 修改AuthService依赖IUserService
3. 更新单元测试

### Rollback
- 每个Phase可独立回滚
- 数据库无Schema变更，无需数据迁移
- 保留旧API端点直到所有客户端升级

## 行业最佳实践整合

### BP1: Refresh Token轮换 (Token Rotation)

**来源**: Auth0, Okta, Microsoft Identity Platform

**原则**: 每次使用RefreshToken获取新AccessToken时，同时颁发新的RefreshToken并使旧Token失效。

**当前项目实现分析**:
```csharp
// 现有AuthService.RefreshTokenAsync已实现Token轮换
var refreshTokenRecord = new RefreshToken
{
    Token = refreshToken,
    UserId = userDto.Id,
    FamilyId = Guid.NewGuid().ToString() // 新家族ID - 已有Family概念
};
```

**改进方向**:
- 保持FamilyId在轮换时不变（同一会话）
- 仅在新登录时生成新FamilyId
- 利用FamilyId实现重放攻击检测

### BP2: Token重放攻击检测 (Replay Attack Detection)

**来源**: Auth0 Automatic Reuse Detection, Okta Token Reuse Detection

**原则**: 如果已使用的RefreshToken被再次使用，立即使整个Token Family失效。

**实现方案**:
```csharp
public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken)
{
    var tokenRecord = await _dbContext.RefreshTokens
        .FirstOrDefaultAsync(t => t.Token == refreshToken);

    // 检测重放攻击：Token已被使用过
    if (tokenRecord?.IsUsed == true)
    {
        _logger.LogWarning("检测到Token重放攻击! FamilyId: {FamilyId}", tokenRecord.FamilyId);

        // 使整个Token Family失效
        await InvalidateTokenFamilyAsync(tokenRecord.FamilyId);

        return Result<LoginResponse>.Failure("会话已失效，请重新登录", AuthErrorCode.TokenRevoked);
    }

    // 标记当前Token为已使用
    tokenRecord.IsUsed = true;
    tokenRecord.UsedAt = DateTime.UtcNow;

    // 颁发新Token (保持FamilyId)
    var newToken = await GenerateNewTokenPairAsync(tokenRecord.UserId, tokenRecord.FamilyId);
    return Result<LoginResponse>.Success(newToken);
}
```

**数据库Schema建议** (RefreshToken表):
| 字段 | 类型 | 说明 |
|------|------|------|
| Token | string | RefreshToken值 |
| UserId | Guid | 用户ID |
| FamilyId | string | Token家族ID |
| IsUsed | bool | 是否已使用 |
| UsedAt | DateTime? | 使用时间 |
| IsRevoked | bool | 是否已撤销 |
| ExpiresAt | DateTime | 过期时间 |

### BP3: Token过期时间最佳实践

**来源**: Microsoft, OWASP

| Token类型 | 推荐过期时间 | 当前配置 | 建议 |
|-----------|-------------|----------|------|
| AccessToken | 5-15分钟 | 15分钟 | 保持 |
| RefreshToken | 7天(普通) / 30天(记住我) | 7天 | 保持 |
| 绝对过期 | 30天 | 无 | 新增 |

**绝对过期**: 无论Token刷新多少次，30天后必须重新登录（医疗系统合规要求）

### BP4: 医疗系统特殊安全要求

**当前项目TokenStorageService已符合**:
```csharp
// Issue #1907: Token改为内存存储 - 符合医疗系统安全要求
// 1. Token = 会话级数据，应用关闭即失效
// 2. 存储方式：进程内存（不持久化到磁盘）
// 3. 每次启动必须输入密码（合规性要求）
```

**额外建议**:
- 审计日志完整记录登录/登出/Token刷新事件（已实现）
- 敏感操作需要二次确认（后续提案）
- 会话超时强制重新认证（AUTH-002/003已定义）

### BP5: 错误处理HTTP状态码规范

**来源**: RFC 9110, OAuth 2.0 RFC 6749

| 场景 | HTTP状态码 | AuthErrorCode |
|------|-----------|---------------|
| 凭据错误 | 401 Unauthorized | InvalidCredentials |
| Token过期 | 401 Unauthorized | TokenExpired |
| Token无效 | 401 Unauthorized | TokenInvalid |
| 权限不足 | 403 Forbidden | - |
| 用户禁用 | 403 Forbidden | UserDisabled |
| RefreshToken过期 | 400 Bad Request | RefreshTokenExpired |

## 与现有实现对比

### 现有优势（保留）
1. ✅ 内存存储Token（医疗合规）
2. ✅ RefreshToken机制
3. ✅ 审计日志记录
4. ✅ FamilyId概念

### 需改进项
1. ❌ Token重放检测（添加IsUsed字段）
2. ❌ 统一错误码（添加AuthErrorCode）
3. ❌ 登出允许过期Token（修改Authorize策略）
4. ❌ 绝对过期时间（添加AbsoluteExpiresAt）

## Open Questions

1. **Q1**: 是否需要支持多设备同时登录？当前设计假设单设备
   - **建议**: MVP阶段保持单设备，后续通过DeviceId扩展
2. **Q2**: RefreshToken是否需要与设备绑定？
   - **建议**: 是，可通过UserAgent或自定义DeviceFingerprint
3. **Q3**: 密码过期策略是否在本次重构范围？
   - **建议**: 否，独立Issue处理

## References

- Microsoft JWT Best Practices: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
- Auth0 Refresh Token Rotation: https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/
- Okta Token Reuse Detection: https://developer.okta.com/docs/guides/refresh-tokens/main/
- OAuth 2.0 Token Revocation: https://datatracker.ietf.org/doc/html/rfc7009
- 项目ADR-002: Desktop端架构决策
- Milan Jovanović - Master Refresh Tokens in ASP.NET Core
