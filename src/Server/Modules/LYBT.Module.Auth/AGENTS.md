# LYBT.Module.Auth - Server Auth Module

**Purpose**: Server-side authentication module with dual-track auth (AdminSecrets + Users), JWT + RefreshToken.

## Structure

```
LYBT.Module.Auth/
├── Interfaces/          # IAuthService, IJwtService, ISecurityAuditService, ITokenRevocationService
├── Services/            # AuthService (845 lines), JwtService, SecurityAuditService, TokenRevocationService
├── Models/              # SecurityAuditEvent DTO
└── AuthModule.cs        # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Login/logout flow | `Services/AuthService.cs` | Core auth logic, 845 lines |
| JWT generation | `Services/JwtService.cs` | Token creation/validation |
| Token revocation | `Services/TokenRevocationService.cs` | RefreshToken revocation |
| Security audit | `Services/SecurityAuditService.cs` | Audit logging |

## CONVENTIONS

- **Dual-track auth** — AdminSecrets table for super-admins, Users table for normal users
- **JWT + RefreshToken** — AccessToken 2h, RefreshToken 7d with family-based revocation
- **BCrypt** — Work factor 12 for password hashing
- **Cross-module** — Uses IUserCrossModuleService to access User data (no direct repo access)
- **Token Family** — RefreshToken rotation with replay attack detection

## ANTI-PATTERNS

- **Direct DbContext for RefreshToken** — AuthService bypasses Repository pattern for token ops
- **Singleton JwtService** — If keys need hot-reload, change to Scoped/IOptionsMonitor
- **Hardcoded account lockout** — 5 attempts / 15 minutes not configurable
