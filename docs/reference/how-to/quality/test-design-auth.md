# 测试设计方案 - LYBT.Module.Auth.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Server/Modules/LYBT.Module.Auth/` |
| **测试路径** | `tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/` |
| **现有测试数** | 67 |
| **目标测试数** | 90 |
| **新增测试数** | +23 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 现有测试覆盖

| 测试类 | 现有测试 | 覆盖范围 |
|--------|----------|----------|
| AuthServiceTests | 18 | 基础认证、登录、登出、Token刷新 |
| JwtServiceTests | 23 | Token生成、验证、密钥验证 |
| TokenRevocationServiceTests | 6 | Token撤销、查询 |
| SecurityAuditServiceTests | 9 | 审计日志记录 |
| SecurityAuditCleanupServiceTests | 4 | 审计清理 |
| JwtOptionsValidationTests | 7 | JWT选项验证 |

### 2.2 需补充测试

| 功能 | 目标测试 | 新增 |
|------|----------|------|
| AutoLoginToken流程 | 8 | +8 |
| Token Family重放攻击 | 5 | +5 |
| Session管理 | 4 | +4 |
| 边界条件补充 | 6 | +6 |

---

## 3. AuthService 补充测试设计 (15个)

### 3.1 AutoLoginToken 流程 (8个)

```
LoginWithAutoTokenAsync_WithValidToken_ShouldLogin
LoginWithAutoTokenAsync_WithExpiredToken_ShouldReturnFailure
LoginWithAutoTokenAsync_WithInvalidToken_ShouldReturnFailure
LoginWithAutoTokenAsync_WithRevokedToken_ShouldReturnFailure
LoginWithAutoTokenAsync_ShouldDetectReplayAttack
GenerateAutoLoginToken_ShouldReturnValidToken
RevokeAutoLoginTokenFamilyAsync_ShouldRevokeAll
AutoLoginToken_ShouldHaveCorrectExpiration
```

### 3.2 Token Family 重放攻击 (5个)

```
RefreshTokenAsync_WithReusedToken_ShouldDetectReplay
RefreshTokenAsync_WithReusedToken_ShouldRevokeFamily
RevokeTokenFamilyAsync_ShouldRevokeAllRelated
TokenFamily_ShouldTrackLineage
TokenFamily_ShouldPreventConcurrentRefresh
```

### 3.3 Session 管理 (2个)

```
GetSessionInfoAsync_ShouldReturnUserInfo
GetSessionInfoAsync_WithExpiredSession_ShouldReturnFailure
```

---

## 4. JwtService 补充测试设计 (4个)

```
GenerateToken_WithAdditionalClaims_ShouldIncludeClaims
ValidateToken_WithTamperedToken_ShouldReturnNull
ValidateToken_WithWrongSigningKey_ShouldReturnNull
ValidateSecretKeyStrength_WithWeakKey_ShouldThrow
```

---

## 5. TokenRevocationService 补充测试设计 (4个)

```
RevokeTokenAsync_ShouldLogAuditEvent
RevokeTokenAsync_WithAlreadyRevokedToken_ShouldReturnFalse
IsTokenRevokedAsync_WithExpiredToken_ShouldCleanup
BatchRevokeTokensAsync_ShouldRevokeAll
```

---

## 6. 测试数据设计

### 6.1 TestTokenBuilder

```csharp
public static class TestTokenBuilder
{
    public static string CreateValidAccessToken(
        Guid? userId = null,
        UserRole? role = null,
        int expirationMinutes = 30)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
            new Claim(ClaimTypes.Role, (role ?? UserRole.Doctor).ToString())
        };

        // 使用测试密钥生成JWT
        return GenerateTestJwt(claims, expirationMinutes);
    }

    public static string CreateExpiredAccessToken(Guid? userId = null)
    {
        return CreateValidAccessToken(userId, expirationMinutes: -1);
    }

    public static RefreshToken CreateRefreshToken(
        Guid? userId = null,
        string? familyId = null,
        bool isRevoked = false)
    {
        return new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = userId ?? Guid.NewGuid(),
            FamilyId = familyId ?? Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked
        };
    }
}
```

---

## 7. Mock 策略

### 7.1 AuthServiceTests 补充 Mock

```csharp
public class AuthServiceTests
{
    // 现有 Mock...

    // 新增: AutoLoginToken 相关
    private void SetupAutoLoginTokenValidation(bool isValid)
    {
        _autoLoginTokenStoreMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(isValid);
    }

    // 新增: Token Family 追踪
    private void SetupTokenFamilyTracking(string familyId)
    {
        _refreshTokenStoreMock
            .Setup(x => x.GetFamilyTokensAsync(familyId))
            .ReturnsAsync(new List<RefreshToken>());
    }
}
```

---

## 8. 验收标准

| 指标 | 目标 |
|------|------|
| AuthService 测试数 | 33 (18+15) |
| JwtService 测试数 | 27 (23+4) |
| TokenRevocationService 测试数 | 10 (6+4) |
| 总测试数 | 90 |
| AutoLoginToken 覆盖 | 100% |
| Token Family 覆盖 | 100% |

---

## 9. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | AutoLoginToken 测试 (8个) | 30min |
| 2 | Token Family 测试 (5个) | 25min |
| 3 | Session 管理测试 (2个) | 10min |
| 4 | JwtService 补充 (4个) | 15min |
| 5 | TokenRevocation 补充 (4个) | 15min |
| 6 | 编译验证和修复 | 15min |
| **总计** | | **~2h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
