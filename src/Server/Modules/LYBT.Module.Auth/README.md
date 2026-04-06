# LYBT.Module.Auth

> 身份认证与授权 | 传统三层 | JWT + RefreshToken

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: IUserService(查询用户信息)

## 目录结构

```
LYBT.Module.Auth/
├── AuthModule.cs
├── Interfaces/
│   └── IJwtService.cs
├── Services/
│   ├── AuthService.cs
│   └── JwtService.cs
└── Models/
    └── (配置模型)
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IAuthService | 9 | 登录/登出/刷新令牌/会话管理 |
| IJwtService | 3 | JWT生成/验证/密钥强度检查 |

## 安全特性

| 特性 | 说明 |
|------|------|
| 双轨认证 | 超级管理员(AdminSecrets表) + 普通用户(Users表) |
| JWT | AccessToken 2小时有效期 |
| RefreshToken | 7天有效期，支持撤销 |
| 密码加密 | BCrypt(工作因子12) |

## 设计依据

- 双轨认证 (AdminSecrets + Users 表) 分离超级管理员和普通用户的身份验证路径，提高安全性
- 通过 IUserCrossModuleService 解耦 Auth 与 Users 模块，Auth 不直接访问 UserRepository
- AccessToken 短有效期 (2h) + RefreshToken 长有效期 (7d) 平衡安全与用户体验
- BCrypt 工作因子 12 兼顾密码安全和小型诊所硬件性能
- TokenRevocationService 支持主动撤销，SecurityAuditService 记录安全事件

## 依赖关系

### 依赖
- LYBT.Infrastructure (AppDbContext)
- LYBT.Entities (User, AdminSecret, AuthSession)
- LYBT.Shared.Models (LoginRequest, LoginResponse等)
- LYBT.Module.Users (用户数据访问)

### 被依赖
- LYBT.WebAPI (AuthController)
- 所有需要认证的模块

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/auth/login | POST | 用户登录(双轨认证) |
| /api/auth/logout | POST | 用户登出 |
| /api/auth/refresh-token | POST | 刷新访问令牌 |
| /api/auth/revoke-token | POST | 撤销RefreshToken |
| /api/auth/validate-token | POST | 验证JWT有效性 |
| /api/auth/session-info | GET | 获取会话信息 |
| /api/auth/change-password | POST | 修改密码 |
| /api/auth/verify | GET | 心跳检查 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Module.Auth 代码知识

服务端认证模块，提供JWT令牌生成/验证、登录/登出流程、Token撤销、自动登录和安全审计功能。

## 代码文件结构

```
LYBT.Module.Auth/
├── AuthModule.cs                          # 模块DI注册入口
├── Interfaces/
│   ├── IAuthService.cs                    # 认证服务接口
│   ├── IJwtService.cs                     # JWT令牌服务接口
│   ├── ISecurityAuditService.cs           # 安全审计服务接口
│   └── ITokenRevocationService.cs         # Token撤销服务接口
├── Models/
│   └── SecurityAuditEvent.cs              # 安全审计事件传输对象
└── Services/
    ├── AuthService.cs                     # 认证服务实现（核心，845行）
    ├── JwtService.cs                      # JWT令牌生成/验证实现
    ├── SecurityAuditService.cs            # 安全审计日志记录实现
    └── TokenRevocationService.cs          # RefreshToken撤销实现
```

### AuthModule.cs
**AuthModule** (static) | 模块DI注册和中间件配置

| 方法 | 说明 |
|------|------|
| AddAuthModule(IServiceCollection, IConfiguration) | 注册认证模块所有服务：IJwtService(Singleton)、IAuthService(Scoped)、ITokenRevocationService(Scoped)、ISecurityAuditService(Scoped)、HttpContextAccessor、FluentValidation验证器 |
| UseAuthModule(IApplicationBuilder) | 配置UseAuthentication + UseAuthorization中间件 |

### Interfaces/IAuthService.cs
**IAuthService** | 身份认证服务统一接口，使用Result<T>返回值

| 方法 | 说明 |
|------|------|
| LoginAsync(LoginRequest, CancellationToken) | 用户登录验证，返回JWT+RefreshToken |
| LoginWithAutoTokenAsync(AutoLoginRequest, CancellationToken) | 使用AutoLoginToken自动登录 (OpenSpec: refactor-login-authentication CVT-001) |
| LogoutAsync(LogoutRequest) | 用户登出，撤销RefreshToken及其Family |
| VerifyCredentialsAsync(LoginRequest, CancellationToken) | 验证用户凭据，返回UserId |
| RefreshTokenAsync(string refreshToken) | 刷新Token，包含Token轮换和重放攻击检测 |
| ValidateTokenAsync(string token) | 验证JWT Token有效性 |
| GetSessionInfoAsync(string token) | 从Token提取会话信息(UserId/UserName/Role) |

### Interfaces/IJwtService.cs
**IJwtService** | JWT令牌生成和验证接口

| 方法 | 说明 |
|------|------|
| GenerateToken(string userId, string userName, UserRole role, string userType) | 生成JWT访问令牌 |
| GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims, string userType) | 生成带额外Claims的JWT令牌 |
| ValidateToken(string token) | 验证JWT令牌，返回ClaimsPrincipal或null |

### Interfaces/ISecurityAuditService.cs
**ISecurityAuditService** | 安全审计服务接口 (Issue #1871)

| 方法 | 说明 |
|------|------|
| LogAsync(SecurityAuditEvent) | 记录安全审计事件，自动从HttpContext提取IP(脱敏)和UserAgent(截断) |

### Interfaces/ITokenRevocationService.cs
**ITokenRevocationService** | Token撤销服务接口 (Issue #1870)

| 方法 | 说明 |
|------|------|
| RevokeTokenAsync(string token, string reason) | 撤销单个RefreshToken |
| IsTokenRevokedAsync(string token) | 查询Token是否已撤销 |

### Models/SecurityAuditEvent.cs
**SecurityAuditEvent** | 安全审计事件DTO

属性: EventType, UserId?, UserType?, UserName?, Success, ErrorMessage?, Metadata?

### Services/JwtService.cs
**JwtService** : IJwtService | JWT令牌生成/验证实现 (Singleton)

| 方法 | 说明 |
|------|------|
| GenerateToken(...) | 创建JWT Token，包含NameIdentifier/Name/Role/user_type/Jti/Iat Claims |
| GenerateToken(..., additionalClaims) | 创建带额外Claims的JWT Token |
| ValidateToken(string token) | 验证Token签名、Issuer、Audience、Lifetime，失败返回null |

依赖: IOptions\<JwtOptions\> (LYBT.Shared.Configuration), IConfiguration
安全特性: 启动时验证密钥长度>=32字符，生产环境禁止默认密钥

### Services/AuthService.cs
**AuthService** : IAuthService | 认证服务核心实现 (845行，模块内最大文件)

| 方法 | 说明 |
|------|------|
| LoginAsync(LoginRequest, CancellationToken) | 统一登录流程：凭据验证->旧Token撤销->JWT生成->RefreshToken存储->AutoLoginToken(可选)->审计日志 |
| LoginWithAutoTokenAsync(AutoLoginRequest, CancellationToken) | AutoLoginToken自动登录：Token验证->重放攻击检测->JWT生成->Token轮换 |
| LogoutAsync(LogoutRequest) | 登出：撤销RefreshToken+整个Token Family->审计日志 |
| VerifyCredentialsAsync(LoginRequest, CancellationToken) | 公开凭据验证，返回UserId字符串 |
| RefreshTokenAsync(string refreshToken) | Token刷新：重放攻击检测->Token轮换->新Token对生成 |
| ValidateTokenAsync(string token) | JWT Token有效性验证 |
| GetSessionInfoAsync(string token) | 从Token提取会话信息 |

私有方法:
| 方法 | 说明 |
|------|------|
| VerifyCredentialsInternalAsync(LoginRequest) | 内部凭据验证，含账户锁定(5次/15分钟)、BCrypt哈希升级、登录状态重置 |
| GenerateRefreshToken() | 生成64字节加密安全随机RefreshToken |
| GenerateAutoLoginToken(...) | 生成AutoLoginToken并存储到数据库 |
| RevokeTokenFamilyAsync(string familyId, string reason) | 撤销RefreshToken Family (重放攻击防护) |
| RevokeAutoLoginTokenFamilyAsync(string familyId, string reason) | 撤销AutoLoginToken Family |
| MapToUserDetailDto(UserBasicDto) | UserBasicDto到UserDetailDto映射 |

依赖: IJwtService, IUserCrossModuleService (跨模块查询), AppDbContext, ITokenRevocationService, ISecurityAuditService

### Services/SecurityAuditService.cs
**SecurityAuditService** : ISecurityAuditService | 安全审计日志实现

| 方法 | 说明 |
|------|------|
| LogAsync(SecurityAuditEvent) | 记录审计事件到SecurityAuditLogs表，失败不影响主流程 |

私有方法:
| 方法 | 说明 |
|------|------|
| ExtractAndMaskIpAddress(HttpContext?) | IPv4脱敏(最后段替换为\*)，IPv6保留前4组 |
| ExtractAndTruncateUserAgent(HttpContext?) | UserAgent截断到500字符 |

### Services/TokenRevocationService.cs
**TokenRevocationService** : ITokenRevocationService | Token撤销实现

| 方法 | 说明 |
|------|------|
| RevokeTokenAsync(string token, string reason) | 标记RefreshToken为已撤销，记录审计日志 |
| IsTokenRevokedAsync(string token) | 查询Token撤销状态，利用覆盖索引优化 |

## 死代码与废弃标记

(无)

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| Services/AuthService.cs | 文件过大(845行) | 包含登录、登出、Token刷新、AutoLogin、Token Family管理等多个职责 | 考虑拆分为AuthService(登录/登出) + TokenManagementService(刷新/撤销/Family管理) |
| Services/AuthService.cs | 直接依赖AppDbContext | RefreshToken和AutoLoginToken操作直接通过DbContext，绕过Repository模式 | 引入IRefreshTokenRepository和IAutoLoginTokenRepository |
| Services/TokenRevocationService.cs | 内部重复记录审计 | RevokeTokenAsync自行记录SecurityAuditLog，而AuthService.LogoutAsync也通过ISecurityAuditService记录，可能产生重复审计记录 | 统一通过ISecurityAuditService记录，TokenRevocationService不应直接写入审计表 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 登录失败次数锁定不跨实例同步 | FailedLoginCount通过IUserCrossModuleService写入DB，但并发登录可能产生竞态条件 | 当前单实例部署无影响，多实例需引入分布式锁或DB层原子操作 |
| JwtService注册为Singleton | 无状态设计正确，但如果JwtOptions在运行时变更(如密钥轮换)，需要重启应用 | 如需热更新密钥，改为Scoped或使用IOptionsMonitor |
| RefreshToken绝对过期时间(T5-P2-04) | AbsoluteExpiresAt在Token轮换时继承，不可延长，最长30天 | 设计如此，超过30天必须重新登录 |
| AutoLoginToken FamilyId继承 | Token轮换时继承FamilyId，检测重放攻击时整个Family失效 | 确保客户端正确处理重放攻击导致的登出场景 |
