# LYBT.Module.Auth.Tests

> 认证模块单元测试，覆盖 JWT 签发与验证、用户认证、安全审计、令牌吊销等核心安全功能。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.Auth
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Security/JwtOptionsValidationTests.cs` | `JwtOptionsValidation` | ~7 |
| `Services/AuthServiceTests.cs` | `AuthService` | ~17 |
| `Services/JwtServiceTests.cs` | `JwtService` | ~23 |
| `Services/SecurityAuditCleanupServiceTests.cs` | `SecurityAuditCleanupService` | ~4 |
| `Services/SecurityAuditServiceTests.cs` | `SecurityAuditService` | ~9 |
| `Services/TokenRevocationServiceTests.cs` | `TokenRevocationService` | ~6 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Auth
- LYBT.Infrastructure
- LYBT.Entities
- Microsoft.AspNetCore.Authentication.JwtBearer

## 更新记录

- 2026-03-01: 创建 README
