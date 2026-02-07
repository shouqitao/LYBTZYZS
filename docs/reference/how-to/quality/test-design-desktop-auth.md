# 测试设计方案 - LYBT.Desktop.Auth.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Client/Desktop/Modules/LYBT.Desktop.Auth/` |
| **测试路径** | `tests/UnitTests/Client/Desktop/LYBT.Desktop.Auth.Tests/` |
| **现有测试数** | 13 |
| **目标测试数** | 40 |
| **新增测试数** | +27 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 LoginViewModel

| 属性/命令 | 现有测试 | 目标测试 | 新增 |
|-----------|----------|----------|------|
| 构造函数 | 2 | 2 | 0 |
| Username 属性 | 3 | 5 | +2 |
| Password 属性 | 2 | 4 | +2 |
| RememberUsername | 0 | 3 | +3 |
| RememberPassword | 0 | 3 | +3 |
| LoginCommand | 3 | 8 | +5 |
| ApiStatus | 3 | 5 | +2 |
| ConnectionMode | 0 | 4 | +4 |
| LoadSavedCredentials | 0 | 3 | +3 |
| RetryApiCheckCommand | 0 | 3 | +3 |

---

## 3. LoginViewModel 测试设计

### 3.1 属性变更测试 (7个)

```
Username_WhenChanged_ShouldClearPassword
Username_WhenChanged_ShouldRaisePropertyChanged
Password_WhenChanged_ShouldRaisePropertyChanged
RememberUsername_WhenChanged_ShouldRaisePropertyChanged
RememberPassword_WhenChanged_ShouldRaisePropertyChanged
ApiStatus_WhenChanged_ShouldUpdateApiStatusMessage
ConnectionMode_WhenChanged_ShouldUpdateModeProperties
```

### 3.2 LoginCommand 测试 (8个)

```
LoginCommand_CanExecute_WithValidCredentials_ShouldReturnTrue
LoginCommand_CanExecute_WithEmptyUsername_ShouldReturnFalse
LoginCommand_CanExecute_WithEmptyPassword_ShouldReturnFalse
LoginCommand_CanExecute_WhenBusy_ShouldReturnFalse
ExecuteLoginAsync_WithValidCredentials_ShouldLogin
ExecuteLoginAsync_WithInvalidCredentials_ShouldShowError
ExecuteLoginAsync_ShouldSetBusyState
ExecuteLoginAsync_WithApiError_ShouldHandleGracefully
```

### 3.3 RememberCredentials 测试 (6个)

```
RememberUsername_WhenEnabled_ShouldSaveUsername
RememberUsername_WhenDisabled_ShouldClearSavedUsername
RememberPassword_WhenEnabled_ShouldSavePassword
RememberPassword_WhenDisabled_ShouldClearSavedPassword
LoadSavedCredentialsAsync_ShouldLoadUsername
LoadSavedCredentialsAsync_ShouldLoadPassword
```

### 3.4 ApiStatus 测试 (5个)

```
ApiStatus_WhenHealthy_ShouldShowHealthyMessage
ApiStatus_WhenUnhealthy_ShouldShowUnhealthyMessage
ApiStatus_WhenChecking_ShouldShowCheckingMessage
ApiStatus_ShouldUpdateOnStateChange
ApiStatus_ShouldAffectLoginCommandCanExecute
```

### 3.5 ConnectionMode 测试 (4个)

```
SelectedConnectionMode_WhenRemote_ShouldSetIsRemoteMode
SelectedConnectionMode_WhenLocal_ShouldSetIsLocalMode
SelectedConnectionMode_WhenChanged_ShouldTriggerApiCheck
SelectedConnectionMode_ShouldPersistSelection
```

### 3.6 RetryApiCheckCommand 测试 (3个)

```
RetryApiCheckCommand_CanExecute_WhenUnhealthy_ShouldReturnTrue
RetryApiCheckCommand_CanExecute_WhenHealthy_ShouldReturnFalse
ExecuteRetryApiCheckAsync_ShouldCheckApiHealth
```

---

## 4. 测试数据设计

### 4.1 TestLoginViewModelBuilder

```csharp
public static class TestLoginViewModelBuilder
{
    public static LoginViewModel Create(
        Mock<IAuthenticationService>? authServiceMock = null,
        Mock<IUsernameStorageService>? usernameStorageMock = null,
        Mock<ICredentialVault>? credentialVaultMock = null,
        Mock<IApplicationStateService>? appStateMock = null)
    {
        authServiceMock ??= CreateDefaultAuthServiceMock();
        usernameStorageMock ??= CreateDefaultUsernameStorageMock();
        credentialVaultMock ??= CreateDefaultCredentialVaultMock();
        appStateMock ??= CreateDefaultAppStateMock();

        return new LoginViewModel(
            authServiceMock.Object,
            usernameStorageMock.Object,
            credentialVaultMock.Object,
            appStateMock.Object,
            NullLogger<LoginViewModel>.Instance);
    }

    private static Mock<IAuthenticationService> CreateDefaultAuthServiceMock()
    {
        var mock = new Mock<IAuthenticationService>();
        mock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = Guid.NewGuid(), RealName = "测试用户" }));
        return mock;
    }

    private static Mock<IUsernameStorageService> CreateDefaultUsernameStorageMock()
    {
        var mock = new Mock<IUsernameStorageService>();
        mock.Setup(x => x.GetSavedUsernameAsync())
            .ReturnsAsync((string?)null);
        return mock;
    }

    private static Mock<ICredentialVault> CreateDefaultCredentialVaultMock()
    {
        var mock = new Mock<ICredentialVault>();
        mock.Setup(x => x.GetPasswordAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);
        return mock;
    }

    private static Mock<IApplicationStateService> CreateDefaultAppStateMock()
    {
        var mock = new Mock<IApplicationStateService>();
        mock.Setup(x => x.IsApiHealthy).Returns(true);
        mock.Setup(x => x.ConnectionStatus).Returns(ConnectionStatus.Connected);
        return mock;
    }
}
```

---

## 5. Mock 策略

```csharp
public class LoginViewModelTests
{
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<IUsernameStorageService> _usernameStorageMock;
    private readonly Mock<ICredentialVault> _credentialVaultMock;
    private readonly Mock<IApplicationStateService> _appStateMock;
    private readonly LoginViewModel _sut;

    public LoginViewModelTests()
    {
        _authServiceMock = new Mock<IAuthenticationService>();
        _usernameStorageMock = new Mock<IUsernameStorageService>();
        _credentialVaultMock = new Mock<ICredentialVault>();
        _appStateMock = new Mock<IApplicationStateService>();

        // 默认: API 健康
        _appStateMock.Setup(x => x.IsApiHealthy).Returns(true);

        // 默认: 登录成功
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto
            {
                Id = Guid.NewGuid(),
                RealName = "测试用户"
            }));

        _sut = new LoginViewModel(
            _authServiceMock.Object,
            _usernameStorageMock.Object,
            _credentialVaultMock.Object,
            _appStateMock.Object,
            NullLogger<LoginViewModel>.Instance);
    }

    // 辅助方法: 设置已保存凭据
    private void SetupSavedCredentials(string username, string? password = null)
    {
        _usernameStorageMock
            .Setup(x => x.GetSavedUsernameAsync())
            .ReturnsAsync(username);

        if (password != null)
        {
            _credentialVaultMock
                .Setup(x => x.GetPasswordAsync(username))
                .ReturnsAsync(password);
        }
    }

    // 辅助方法: 设置 API 不健康
    private void SetupUnhealthyApi()
    {
        _appStateMock.Setup(x => x.IsApiHealthy).Returns(false);
        _appStateMock.Setup(x => x.ConnectionStatus).Returns(ConnectionStatus.Disconnected);
    }
}
```

---

## 6. WPF 特殊测试注意事项

### 6.1 STA Thread

```csharp
// 对于需要 WPF Dispatcher 的测试，使用 [StaFact]
[StaFact]
public void LoginCommand_WhenExecuted_ShouldUpdateBusyState()
{
    // Arrange
    _sut.Username = "testuser";
    _sut.Password = "password";

    // Act & Assert - 使用 Dispatcher
    // ...
}
```

### 6.2 PropertyChanged 验证

```csharp
[Fact]
public void Username_WhenChanged_ShouldRaisePropertyChanged()
{
    // Arrange
    var propertyChangedRaised = false;
    _sut.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(LoginViewModel.Username))
            propertyChangedRaised = true;
    };

    // Act
    _sut.Username = "newuser";

    // Assert
    propertyChangedRaised.Should().BeTrue();
}
```

---

## 7. 验收标准

| 指标 | 目标 |
|------|------|
| 属性测试数 | 7 |
| LoginCommand 测试数 | 8 |
| RememberCredentials 测试数 | 6 |
| ApiStatus 测试数 | 5 |
| ConnectionMode 测试数 | 4 |
| RetryCommand 测试数 | 3 |
| 总测试数 | 40 |
| 登录流程覆盖 | 100% |

---

## 8. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | 属性变更测试 (7个) | 20min |
| 2 | LoginCommand 测试 (8个) | 30min |
| 3 | RememberCredentials 测试 (6个) | 20min |
| 4 | ApiStatus 测试 (5个) | 15min |
| 5 | ConnectionMode 测试 (4个) | 15min |
| 6 | RetryCommand 测试 (3个) | 10min |
| 7 | 编译验证和修复 | 15min |
| **总计** | | **~2h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
