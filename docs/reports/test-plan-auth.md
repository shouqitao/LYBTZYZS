# Auth 模块测试计划

**日期**: 2025-10-03
**相关Issue**: #864 - Phase 2.3
**目标覆盖率**: 12.5% → 80%
**预计工作量**: 1周

---

## 模块概览

### 源代码结构
- **Services**:
  - `AuthService.cs` (12个方法 - 核心认证逻辑)
  - `JwtService.cs` (4个方法 - Token生成与验证)
- **Interfaces**: `IJwtService.cs`
- **无 Repository 层** (直接使用 UserService)
- **无 Validators** (使用 UserService 的验证)

### 现有测试
- `AuthServiceTests.cs` (基础登录测试)

---

## 测试计划清单

### 1️⃣ AuthService 测试 (优先级: 🔴 高)

**测试文件**: `AuthServiceTests.cs`

#### 1.1 超级管理员认证 (6个测试)

##### IsSuperAdminCredentials
- [ ] `IsSuperAdminCredentials_WithCorrectCredentials_ReturnsTrue`
  - 验证: 正确的超管账号密码返回true
  - Mock: Configuration 返回超管账号配置

- [ ] `IsSuperAdminCredentials_WithIncorrectUsername_ReturnsFalse`
  - 验证: 错误用户名返回false

- [ ] `IsSuperAdminCredentials_WithIncorrectPassword_ReturnsFalse`
  - 验证: 错误密码返回false

- [ ] `IsSuperAdminCredentials_WithNullCredentials_ReturnsFalse`
  - 验证: null凭据返回false

- [ ] `IsSuperAdminCredentials_WhenConfigMissing_ReturnsFalse`
  - 验证: 配置缺失时返回false

- [ ] `ChangeSysAdminPasswordAsync_UpdatesConfiguredPassword`
  - 验证: 更新超管密码成功

#### 1.2 用户凭据验证 (8个测试)

##### VerifyCredentialsAsync
- [ ] `VerifyCredentialsAsync_WithValidCredentials_ReturnsSuccess`
  - 验证: 正确的用户凭据返回成功
  - Mock: UserService.GetByUsernameOrEmailAsync, ValidatePasswordAsync

- [ ] `VerifyCredentialsAsync_WithNonExistentUser_ReturnsFailure`
  - 验证: 不存在的用户返回失败

- [ ] `VerifyCredentialsAsync_WithWrongPassword_ReturnsFailure`
  - 验证: 错误密码返回失败

- [ ] `VerifyCredentialsAsync_WithLockedAccount_ReturnsFailure`
  - 验证: 锁定的账户返回失败
  - Mock: UserService.IsAccountLockedAsync 返回 true

- [ ] `VerifyCredentialsAsync_WithDisabledAccount_ReturnsFailure`
  - 验证: 禁用的账户返回失败

- [ ] `VerifyCredentialsAsync_IncrementsFailedLoginCount_OnFailure`
  - 验证: 失败时增加失败计数
  - Verify: UserService.IncrementFailedLoginCountAsync 被调用

- [ ] `VerifyCredentialsAsync_ResetsFailedLoginCount_OnSuccess`
  - 验证: 成功时重置失败计数
  - Verify: UserService.ResetFailedLoginCountAsync 被调用

- [ ] `VerifyCredentialsAsync_WithNullCredentials_ThrowsArgumentNullException`
  - 验证: null凭据抛出异常

#### 1.3 登录流程 (12个测试)

##### LoginAsync
- [ ] `LoginAsync_WithValidUserCredentials_ReturnsToken`
  - 验证: 普通用户登录成功返回Token
  - Mock: VerifyCredentialsAsync, JwtService.GenerateToken

- [ ] `LoginAsync_WithSuperAdminCredentials_ReturnsToken`
  - 验证: 超管登录成功返回Token
  - Mock: IsSuperAdminCredentials, JwtService.GenerateToken

- [ ] `LoginAsync_WithInvalidCredentials_ReturnsFailure`
  - 验证: 无效凭据返回失败

- [ ] `LoginAsync_GeneratesAccessToken`
  - 验证: 生成访问Token
  - Verify: JwtService.GenerateToken 被调用

- [ ] `LoginAsync_GeneratesRefreshToken`
  - 验证: 生成刷新Token

- [ ] `LoginAsync_UpdatesLastLoginTime`
  - 验证: 更新最后登录时间
  - Verify: UserService.UpdateLastLoginTimeAsync 被调用

- [ ] `LoginAsync_SavesAuthentication`
  - 验证: 保存认证信息
  - Verify: SaveAuthenticationAsync 被调用

- [ ] `LoginAsync_WithRememberMe_SetsLongerExpiration`
  - 验证: 记住我设置更长过期时间

- [ ] `LoginAsync_WithoutRememberMe_SetsDefaultExpiration`
  - 验证: 不记住我使用默认过期时间

- [ ] `LoginAsync_WhenUserServiceFails_ReturnsFailure`
  - 验证: UserService异常时返回失败

- [ ] `LoginAsync_WhenJwtServiceFails_ReturnsFailure`
  - 验证: JwtService异常时返回失败

- [ ] `LoginAsync_LogsLoginAttempt`
  - 验证: 记录登录尝试日志
  - Verify: Logger.Log 被调用

#### 1.4 登出与Token管理 (8个测试)

##### LogoutAsync
- [ ] `LogoutAsync_WithValidToken_RevokesToken`
  - 验证: 注销撤销Token
  - Verify: RevokeTokenAsync 被调用

- [ ] `LogoutAsync_ClearsAuthentication`
  - 验证: 清除认证信息

##### RefreshTokenAsync
- [ ] `RefreshTokenAsync_WithValidRefreshToken_ReturnsNewAccessToken`
  - 验证: 有效的刷新Token返回新访问Token
  - Mock: ValidateTokenAsync, JwtService.GenerateToken

- [ ] `RefreshTokenAsync_WithExpiredRefreshToken_ReturnsFailure`
  - 验证: 过期的刷新Token返回失败

- [ ] `RefreshTokenAsync_WithInvalidRefreshToken_ReturnsFailure`
  - 验证: 无效的刷新Token返回失败

##### ValidateTokenAsync
- [ ] `ValidateTokenAsync_WithValidToken_ReturnsSuccess`
  - 验证: 有效Token验证成功
  - Mock: JwtService.ValidateToken

- [ ] `ValidateTokenAsync_WithExpiredToken_ReturnsFailure`
  - 验证: 过期Token验证失败

- [ ] `ValidateTokenAsync_WithRevokedToken_ReturnsFailure`
  - 验证: 已撤销Token验证失败

##### RevokeTokenAsync
- [ ] `RevokeTokenAsync_AddsTokenToBlacklist`
  - 验证: 撤销Token加入黑名单

#### 1.5 会话管理 (4个测试)

##### GetSessionInfoAsync
- [ ] `GetSessionInfoAsync_WithValidToken_ReturnsUserInfo`
  - 验证: 有效Token返回用户信息

- [ ] `GetSessionInfoAsync_WithInvalidToken_ReturnsNull`
  - 验证: 无效Token返回null

- [ ] `GetSessionInfoAsync_ParsesClaimsCorrectly`
  - 验证: 正确解析Claims

- [ ] `GetSessionInfoAsync_IncludesUserRoles`
  - 验证: 包含用户角色信息

**预计测试数**: 38个

---

### 2️⃣ JwtService 测试 (优先级: 🔴 高)

**测试文件**: `JwtServiceTests.cs`

#### 2.1 Token 生成 (12个测试)

##### GenerateToken (UserDto重载)
- [ ] `GenerateToken_WithUserDto_GeneratesValidToken`
  - 验证: 生成有效的Token

- [ ] `GenerateToken_WithUserDto_IncludesUserId`
  - 验证: Token包含用户ID Claim

- [ ] `GenerateToken_WithUserDto_IncludesUsername`
  - 验证: Token包含用户名Claim

- [ ] `GenerateToken_WithUserDto_IncludesRoles`
  - 验证: Token包含角色Claims

- [ ] `GenerateToken_WithCustomExpiration_SetsCorrectExpiry`
  - 验证: 自定义过期时间设置正确

- [ ] `GenerateToken_WithNullUser_ThrowsArgumentNullException`
  - 验证: null用户抛出异常

##### GenerateToken (Claims重载)
- [ ] `GenerateToken_WithClaims_GeneratesValidToken`
  - 验证: 使用Claims生成有效Token

- [ ] `GenerateToken_WithEmptyClaims_GeneratesBasicToken`
  - 验证: 空Claims生成基础Token

- [ ] `GenerateToken_IncludesIssuer`
  - 验证: Token包含Issuer

- [ ] `GenerateToken_IncludesAudience`
  - 验证: Token包含Audience

- [ ] `GenerateToken_IncludesJti`
  - 验证: Token包含唯一标识符(Jti)

- [ ] `GenerateToken_SignsWithSecretKey`
  - 验证: 使用密钥签名

#### 2.2 Token 验证 (10个测试)

##### ValidateToken
- [ ] `ValidateToken_WithValidToken_ReturnsPrincipal`
  - 验证: 有效Token返回ClaimsPrincipal

- [ ] `ValidateToken_WithExpiredToken_ThrowsSecurityTokenException`
  - 验证: 过期Token抛出异常

- [ ] `ValidateToken_WithInvalidSignature_ThrowsSecurityTokenException`
  - 验证: 无效签名抛出异常

- [ ] `ValidateToken_WithTamperedToken_ThrowsException`
  - 验证: 篡改的Token抛出异常

- [ ] `ValidateToken_WithNullToken_ThrowsArgumentNullException`
  - 验证: null Token抛出异常

- [ ] `ValidateToken_WithEmptyToken_ThrowsArgumentException`
  - 验证: 空Token抛出异常

- [ ] `ValidateToken_ValidatesIssuer`
  - 验证: 验证Issuer正确性

- [ ] `ValidateToken_ValidatesAudience`
  - 验证: 验证Audience正确性

- [ ] `ValidateToken_ValidatesLifetime`
  - 验证: 验证生命周期

- [ ] `ValidateToken_ExtractsClaimsCorrectly`
  - 验证: 正确提取Claims

#### 2.3 密钥验证 (4个测试)

##### ValidateSecretKeyStrength
- [ ] `ValidateSecretKeyStrength_WithStrongKey_DoesNotThrow`
  - 验证: 强密钥不抛出异常

- [ ] `ValidateSecretKeyStrength_WithWeakKey_ThrowsException`
  - 验证: 弱密钥抛出异常

- [ ] `ValidateSecretKeyStrength_WithMinimumLength_DoesNotThrow`
  - 验证: 最小长度密钥不抛出异常

- [ ] `ValidateSecretKeyStrength_WithTooShortKey_ThrowsException`
  - 验证: 过短密钥抛出异常

**预计测试数**: 26个

---

## 测试数据准备

### 测试用户数据

```csharp
public class AuthTestData
{
    public static UserDto CreateTestUser()
    {
        return new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            RealName = "测试用户",
            Email = "test@example.com",
            Status = CommonStatus.Enabled
        };
    }

    public static LoginRequest CreateValidLoginRequest()
    {
        return new LoginRequest
        {
            Username = "testuser",
            Password = "Test@123456",
            RememberMe = false
        };
    }

    public static IConfiguration CreateMockConfiguration()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Jwt:SecretKey"]).Returns("ThisIsAVeryStrongSecretKeyForTesting123456789");
        config.Setup(c => c["Jwt:Issuer"]).Returns("LYBT");
        config.Setup(c => c["Jwt:Audience"]).Returns("LYBTUsers");
        config.Setup(c => c["Jwt:AccessTokenExpirationMinutes"]).Returns("30");
        config.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");
        config.Setup(c => c["Auth:SuperAdmin:Username"]).Returns("sysadmin");
        config.Setup(c => c["Auth:SuperAdmin:Password"]).Returns("Admin@123");
        return config.Object;
    }
}
```

---

## Mock 对象配置

### AuthService 测试的 Mock 设置

```csharp
public class AuthServiceTests
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<LybtDbContext> _mockDbContext;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockDbContext = CreateMockDbContext();
        _configuration = AuthTestData.CreateMockConfiguration();

        _sut = new AuthService(
            _mockJwtService.Object,
            _mockUserService.Object,
            _mockLogger.Object,
            _mockDbContext.Object,
            _configuration
        );
    }
}
```

### JwtService 测试的 Mock 设置

```csharp
public class JwtServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        _configuration = AuthTestData.CreateMockConfiguration();
        _sut = new JwtService(_configuration);
    }
}
```

---

## 验收标准

- ✅ **行覆盖率**: ≥80%
- ✅ **分支覆盖率**: ≥70%
- ✅ **方法覆盖率**: 100% (AuthService 12个 + JwtService 4个方法)
- ✅ **测试数量**: 64个测试
- ✅ **测试通过率**: 100%
- ✅ **遵循AAA模式**
- ✅ **使用FluentAssertions断言**
- ✅ **安全性测试覆盖** (密码、Token、验证)

---

## 安全测试重点

### 必须覆盖的安全场景

1. **密码安全**
   - 密码哈希验证
   - 弱密码拒绝
   - 密码未明文存储

2. **Token安全**
   - Token签名验证
   - Token过期处理
   - Token篡改检测
   - Token撤销机制

3. **账户锁定**
   - 失败尝试计数
   - 自动锁定机制
   - 锁定时间验证

4. **注入攻击防护**
   - SQL注入测试
   - XSS防护测试

---

## 实施步骤

1. **Step 1**: 创建测试文件骨架 (15分钟)
   - AuthServiceTests.cs
   - JwtServiceTests.cs

2. **Step 2**: 实现 AuthService 超管认证测试 (45分钟)

3. **Step 3**: 实现 AuthService 凭据验证测试 (1小时)

4. **Step 4**: 实现 AuthService 登录流程测试 (1.5小时)

5. **Step 5**: 实现 AuthService Token管理测试 (1小时)

6. **Step 6**: 实现 AuthService 会话管理测试 (30分钟)

7. **Step 7**: 实现 JwtService Token生成测试 (1.5小时)

8. **Step 8**: 实现 JwtService Token验证测试 (1.5小时)

9. **Step 9**: 实现 JwtService 密钥验证测试 (30分钟)

10. **Step 10**: 运行并验证 (30分钟)

---

**下一步**: 开始实施 Step 1 - 创建测试文件骨架
