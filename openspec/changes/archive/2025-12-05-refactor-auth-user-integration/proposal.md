# Change: 认证与用户模块集成重构

## Why

当前认证系统存在以下问题：
1. **Token生命周期管理不完善** - 登出时Token已过期会导致API异常(Refit.ApiException)
2. **Auth与User模块职责边界模糊** - 用户密码管理、凭据验证逻辑分散在两个模块
3. **客户端认证逻辑不一致** - Desktop端的AuthenticationService与Server端AuthService职责不对称
4. **缺乏统一的会话管理** - Token刷新、过期处理、会话追踪缺乏统一策略
5. **错误处理不健壮** - 边界情况(如Token过期、网络断开)处理不完善

## What Changes

### Server端 (WebAPI + Module.Auth + Module.Users)

- **MODIFIED**: AuthController增强错误处理和状态码规范
- **MODIFIED**: AuthService优化Token验证和刷新逻辑
- **ADDED**: 统一的认证异常处理中间件
- **MODIFIED**: UserService与AuthService的职责边界清晰化
- **ADDED**: 密码策略验证服务(从User模块提取)

### Client端 (Desktop Foundation)

- **MODIFIED**: AuthenticationService增强Token过期处理
- **ADDED**: TokenLifecycleService统一管理Token生命周期
- **MODIFIED**: ITokenStorageService增加过期预检方法
- **ADDED**: 认证状态机(Logged In → Active → Warning → Expired)

### 共享契约 (Shared.Models)

- **MODIFIED**: 认证相关DTO规范化和文档完善
- **ADDED**: AuthErrorCode枚举统一错误码

## Impact

### Affected Specs
- `authentication/spec.md` - 核心认证规范（更新：添加AUTH-006到AUTH-009，修改AUTH-004）
- `user-management/spec.md` - 用户认证集成规范（新增：USER-AUTH-001到USER-AUTH-003，USER-001）

### Affected Code

**Server端:**
- `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`
- `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

**Client端:**
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthenticationService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/ITokenStorageService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/TokenStorageService.cs`

**共享:**
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/*.cs`
- `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs`

### Breaking Changes
- **BREAKING**: `ValidateTokenAsync` API响应格式变更(增加详细错误码)
- **BREAKING**: 登出API不再要求Token有效(支持过期Token登出)

### Migration
- 客户端需要更新处理新的错误码
- 现有Token在重构后首次登录需重新认证

## References

- Issue #1864: Token认证安全重构
- Epic #1861: Token认证安全重构
- Microsoft JWT Best Practices: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
