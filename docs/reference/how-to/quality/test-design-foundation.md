# 测试设计方案 - LYBT.Desktop.Foundation.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Client/Desktop/Core/LYBT.Desktop.Foundation/` |
| **测试路径** | `tests/UnitTests/Client/Desktop/LYBT.Desktop.Foundation.Tests/` |
| **现有测试数** | 43 |
| **目标测试数** | 100 |
| **新增测试数** | +57 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 Security 包

| 服务类 | 现有测试 | 目标测试 | 新增 |
|--------|----------|----------|------|
| AuthenticationService | 8 | 15 | +7 |
| TokenManager | 11 | 11 | 0 |
| TokenStorageService | 0 | 8 | +8 |
| TokenLifecycleService | 0 | 6 | +6 |
| LocalTokenValidator | 6 | 8 | +2 |
| CredentialVault | 5 | 8 | +3 |
| LogoutService | 4 | 6 | +2 |
| UsernameStorageService | 0 | 5 | +5 |

### 2.2 Application 包

| 服务类 | 现有测试 | 目标测试 | 新增 |
|--------|----------|----------|------|
| ApplicationStateService | 0 | 8 | +8 |
| ApiHealthCheckService | 0 | 5 | +5 |

### 2.3 其他

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| AuthenticationStateMachine | 6 | 10 | +4 |
| TokenEvents | 3 | 3 | 0 |

---

## 3. AuthenticationService 补充测试设计 (7个)

```
LoginAsync_WithValidCredentials_ShouldReturnUser
LoginAsync_WithInvalidCredentials_ShouldReturnFailure
LoginAsync_ShouldStoreTokens
LoginWithAutoTokenAsync_WithValidToken_ShouldLogin
LoginWithAutoTokenAsync_WithExpiredToken_ShouldReturnFailure
LogoutAsync_ShouldClearTokens
LogoutAsync_ShouldRevokeServerToken
```

---

## 4. TokenStorageService 测试设计 (8个)

```
SaveAuthenticationAsync_ShouldStoreTokens
GetTokenAsync_WithStoredToken_ShouldReturn
GetTokenAsync_WithNoToken_ShouldReturnNull
ClearAuthenticationAsync_ShouldClearAllTokens
IsTokenExpiredAsync_WithExpiredToken_ShouldReturnTrue
IsTokenExpiredAsync_WithValidToken_ShouldReturnFalse
SaveAuthenticationAsync_ShouldOverwriteExisting
GetTokenAsync_ShouldReturnCorrectTokenType
```

---

## 5. TokenLifecycleService 测试设计 (6个)

```
StartMonitoring_ShouldBeginTimer
StopMonitoring_ShouldStopTimer
StartMonitoring_ShouldRefreshExpiringSoonToken
StartMonitoring_WithExpiredToken_ShouldTriggerLogout
RefreshToken_WithApiError_ShouldHandleGracefully
StopMonitoring_ShouldCancelPendingRefresh
```

---

## 6. LocalTokenValidator 补充测试设计 (2个)

```
ValidateTokenAsync_WithTamperedToken_ShouldReturnFalse
ValidateWithKeyAsync_WithWrongKey_ShouldReturnFalse
```

---

## 7. CredentialVault 补充测试设计 (3个)

```
SavePasswordAsync_ShouldEncryptWithDPAPI
GetPasswordAsync_WithCorruptedData_ShouldReturnNull
ClearPasswordAsync_ShouldRemoveFromStorage
```

---

## 8. LogoutService 补充测试设计 (2个)

```
LogoutAsync_WithApiError_ShouldStillClearLocal
LogoutLocallyAsync_ShouldNotCallApi
```

---

## 9. UsernameStorageService 测试设计 (5个)

```
SaveUsernameAsync_ShouldPersistUsername
GetSavedUsernameAsync_WithSavedUsername_ShouldReturn
GetSavedUsernameAsync_WithNoSaved_ShouldReturnNull
ClearUsernameAsync_ShouldRemoveUsername
SaveUsernameAsync_ShouldOverwriteExisting
```

---

## 10. ApplicationStateService 测试设计 (8个)

```
CheckApiHealthAsync_WithHealthyApi_ShouldSetHealthy
CheckApiHealthAsync_WithUnhealthyApi_ShouldSetUnhealthy
CheckApiHealthAsync_ShouldRaiseStateChanged
ApiHealthy_ShouldUpdateConnectionStatus
ApiUnhealthy_ShouldUpdateConnectionStatus
CheckApiHealthAsync_WithTimeout_ShouldSetUnhealthy
ApiBaseUrl_ShouldReturnConfiguredUrl
CheckApiHealthAsync_ShouldRetryOnFailure
```

---

## 11. ApiHealthCheckService 测试设计 (5个)

```
CheckHealthAsync_WithHealthyEndpoint_ShouldReturnTrue
CheckHealthAsync_WithUnhealthyEndpoint_ShouldReturnFalse
CheckHealthAsync_WithTimeout_ShouldReturnFalse
CheckHealthAsync_WithNetworkError_ShouldReturnFalse
CheckHealthAsync_ShouldUseConfiguredUrl
```

---

## 12. AuthenticationStateMachine 补充测试设计 (4个)

```
Transition_FromLoggedOut_ToLoggingIn_ShouldSucceed
Transition_FromLoggedIn_ToLoggingOut_ShouldSucceed
Transition_Invalid_ShouldThrow
GetAllowedTransitions_ShouldReturnValidTransitions
```

---

## 13. 测试数据设计

### 13.1 TestTokenData

```csharp
public static class TestTokenData
{
    public static string CreateValidJwt(
        Guid? userId = null,
        int expirationMinutes = 30)
    {
        // 使用测试密钥生成有效 JWT
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("TestSecretKeyForUnitTesting12345"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
            new Claim(ClaimTypes.Role, "Doctor")
        };

        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateExpiredJwt(Guid? userId = null)
    {
        return CreateValidJwt(userId, -5);
    }

    public static string CreateExpiringSoonJwt(Guid? userId = null)
    {
        return CreateValidJwt(userId, 2); // 2分钟内过期
    }
}
```

---

## 14. Mock 策略

```csharp
public class AuthenticationServiceTests
{
    private readonly Mock<IAuthApi> _authApiMock;
    private readonly Mock<ITokenManager> _tokenManagerMock;
    private readonly Mock<ITokenStorageService> _tokenStorageMock;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _authApiMock = new Mock<IAuthApi>();
        _tokenManagerMock = new Mock<ITokenManager>();
        _tokenStorageMock = new Mock<ITokenStorageService>();

        // 默认: 登录成功
        _authApiMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    AccessToken = TestTokenData.CreateValidJwt(),
                    RefreshToken = "test-refresh-token"
                }
            });

        _sut = new AuthenticationService(
            _authApiMock.Object,
            _tokenManagerMock.Object,
            _tokenStorageMock.Object,
            NullLogger<AuthenticationService>.Instance);
    }
}

public class ApplicationStateServiceTests
{
    private readonly Mock<IApiHealthCheckService> _healthCheckMock;
    private readonly Mock<IOptions<ApiOptions>> _optionsMock;
    private readonly ApplicationStateService _sut;

    public ApplicationStateServiceTests()
    {
        _healthCheckMock = new Mock<IApiHealthCheckService>();
        _optionsMock = new Mock<IOptions<ApiOptions>>();

        // 默认: API 健康
        _healthCheckMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _optionsMock
            .Setup(x => x.Value)
            .Returns(new ApiOptions { BaseUrl = "http://test-api" });

        _sut = new ApplicationStateService(
            _healthCheckMock.Object,
            _optionsMock.Object,
            NullLogger<ApplicationStateService>.Instance);
    }
}
```

---

## 15. 验收标准

| 指标 | 目标 |
|------|------|
| Security 包测试数 | 67 |
| Application 包测试数 | 13 |
| StateMachine 测试数 | 10 |
| 总测试数 | 100 |
| 认证流程覆盖 | 100% |
| Token管理覆盖 | 100% |

---

## 16. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | AuthenticationService 补充 (7个) | 25min |
| 2 | TokenStorageService 测试 (8个) | 25min |
| 3 | TokenLifecycleService 测试 (6个) | 20min |
| 4 | LocalTokenValidator 补充 (2个) | 10min |
| 5 | CredentialVault 补充 (3个) | 10min |
| 6 | LogoutService 补充 (2个) | 10min |
| 7 | UsernameStorageService 测试 (5个) | 15min |
| 8 | ApplicationStateService 测试 (8个) | 25min |
| 9 | ApiHealthCheckService 测试 (5个) | 15min |
| 10 | StateMachine 补充 (4个) | 15min |
| 11 | 编译验证和修复 | 15min |
| **总计** | | **~3h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
