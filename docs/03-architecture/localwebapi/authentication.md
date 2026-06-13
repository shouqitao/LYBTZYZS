# LocalWebAPI 认证机制

> **注意**: 本文档的核心内容已合并到 [05-dual-mode.md](../05-dual-mode.md) §"LocalWebAPI 架构 > 本地认证架构"。此文件保留作为详细实现参考。

## 概述

LocalWebAPI 使用简化的 JWT 认证，适用于本地离线场景。与 Server WebAPI 的完整 JWT + Refresh Token 流程不同，LocalWebAPI 采用一次性 Token 方案。

## 配置

```csharp
// LocalJwtConfig.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("LYBT-LocalWebAPI-Secret-Key-2024-DoNotUseInProduction"))
        };
    });
```

## Token 生成

```csharp
public static string GenerateToken(User user)
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

    var token = new JwtSecurityToken(
        expires: DateTime.UtcNow.AddDays(365),
        claims: claims,
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

## 安全特性

| 特性 | 值 | 说明 |
|------|-----|------|
| 签名算法 | HMAC-SHA256 | 对称加密 |
| 密钥 | 固定字符串 | 本地模式可接受 |
| Token 有效期 | 365 天 | 无需刷新 |
| Refresh Token | 无 | 简化设计 |
| Issuer 验证 | 禁用 | 本地模式不需要 |
| Audience 验证 | 禁用 | 本地模式不需要 |
| Lifetime 验证 | 启用 | 防止过期 Token |

## 与 Server WebAPI 认证对比

| 特性 | Server WebAPI | LocalWebAPI |
|------|---------------|-------------|
| 签名算法 | HMAC-SHA256 | HMAC-SHA256 |
| 密钥管理 | 配置化 | 固定字符串 |
| Token 有效期 | 较短 (小时级) | 365 天 |
| Refresh Token | 有 | 无 |
| Token 刷新端点 | 有 | 无 |
| AutoLoginToken | 有 | 无 |
| 速率限制 | 有 | 无 |
| 审计日志 | 有 | 无 |

## 登录流程

1. 用户输入用户名密码
2. POST `/api/auth/login` → AuthController
3. AuthController 查询 LocalWebApiDbContext.Users
4. 使用 `PasswordHelper.VerifyPassword()` 验证 BCrypt 哈希
5. 验证通过 → 调用 `LocalJwtConfig.GenerateToken(user)` → 返回 Token
6. Desktop 端存储 Token，后续请求通过 Authorization header 携带

## 种子数据

系统启动时自动创建默认管理员账户：
- 用户名: `admin`
- 密码: `admin` (BCrypt 哈希)
- 角色: `Admin`
