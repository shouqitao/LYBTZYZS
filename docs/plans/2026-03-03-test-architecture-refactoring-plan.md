# Test Architecture Refactoring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 全量修复 5 个测试项目中的 26 个偏差，确保测试真实反映生产代码现状。

**Architecture:** 策略 B (Infrastructure-First) -- 先修生产代码 Bug，再统一配置，然后修复 Mock 偏差，最后修补覆盖盲区。

**Tech Stack:** .NET 8 / xUnit / NSubstitute / FluentAssertions / BCrypt.Net

---

## Phase 1: 修复生产代码 Bug (先修代码，再修测试)

### Task 1.1: MedicalCase CreateWhenActiveCase -- InvalidOperationException -> BusinessException

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseServiceHelper.cs:139-152`
- Modify: `tests/LYBT.Tests.Server.Integration/MedicalCases/MedicalCaseIntegrationTests.cs:820-843`

**Step 1: 修复 MedicalCaseServiceHelper.cs**

将两处 `InvalidOperationException` 改为 `BusinessException`:

```csharp
// Line 144: Active case
throw new BusinessException("该患者已有进行中的医案，请先完成现有医案");

// Line 152: Suspended case
throw new BusinessException("该患者已有暂存的医案，请先处理现有医案（继续或关闭）");
```

注意: `BusinessException.GetHttpStatusCode()` 返回 400 (非 422)。添加 `using LYBT.Shared.ExceptionHandling.Exceptions;` 如果不存在。

**Step 2: 更新集成测试断言**

```csharp
// MedicalCaseIntegrationTests.cs Line 821: 方法名更新
public async Task CreateMedicalCase_WhenPatientHasActiveCase_ShouldReturn400()

// Line 841: 断言更新
response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
    "同一患者已有进行中医案时，BusinessException 被映射为 400");
```

**Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

**Step 4: 运行受影响测试**

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --filter "MedicalCase"`
Expected: ALL PASS

---

### Task 1.2: Users GetUsers_InvalidPage -- 添加输入验证

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:42-52`
- Modify: `tests/LYBT.Tests.Server.Integration/Users/UserIntegrationTests.cs:769-780`

**Step 1: 在 UsersController.GetList 添加参数验证**

在 `UsersController.cs:48` 方法体开头添加:

```csharp
public async Task<IActionResult> GetList(
    int page = 1,
    int pageSize = 20,
    string? keyword = null,
    UserRole? role = null,
    CommonStatus? status = null)
{
    if (page < 1)
        return ValidationFail("页码必须大于0");
    if (pageSize < 1 || pageSize > 100)
        return ValidationFail("每页数量必须在1-100之间");

    var result = await _userService.GetPagedAsync(page, pageSize, keyword, role, status);
    return SuccessPaged(result.Data!, "查询成功");
}
```

**Step 2: 更新测试断言**

```csharp
// UserIntegrationTests.cs Line 778
response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
    "page=0 是无效参数，Controller 验证应返回 400");
```

**Step 3: 编译 + 测试**

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --filter "UserIntegration"`
Expected: ALL PASS

---

### Task 1.3: DuplicateUsername 状态码收紧

**Files:**
- Modify: `tests/LYBT.Tests.Server.Integration/Users/UserIntegrationTests.cs:97-102`

**Step 1: 收紧测试断言**

当前代码返回 `Result.Failure()` -> Controller 调用 `BusinessFail()` -> 422。测试应期望唯一状态码:

```csharp
// UserIntegrationTests.cs Lines 100-102
response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
    "重复用户名通过 Result.Failure -> BusinessFail 返回 422");
```

**Step 2: 运行测试验证**

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --filter "DuplicateUsername"`
Expected: PASS

---

### Task 1.4: Phase 1 全量验证

Run: `dotnet test tests/LYBT.Tests.Server.Integration/`
Expected: ALL PASS (266 tests)

---

## Phase 2: 统一测试配置与生产路径

### Task 2.1: 补全 appsettings.Test.json 缺失配置

**Files:**
- Modify: `tests/LYBT.Tests.Server.Integration/appsettings.Test.json`
- Reference: `src/Server/Services/LYBT.WebAPI/appsettings.json`

**Step 1: 添加缺失的配置节**

在 appsettings.Test.json 中添加以下配置节 (从生产配置复制，测试环境适当调整):

```json
{
  "PasswordPolicy": {
    "MinLength": 8,
    "RequireDigit": true,
    "RequireLowercase": true,
    "RequireUppercase": true,
    "RequireSpecialChar": true
  },
  "Session": {
    "TimeoutMinutes": 120,
    "AllowConcurrentSessions": false,
    "SlidingExpiration": true
  },
  "UserManagement": {
    "DefaultRole": "Doctor",
    "AllowSelfRegistration": false,
    "RequireEmailConfirmation": true,
    "EnableUserCache": true,
    "MaxBatchOperationSize": 100
  },
  "SystemAdmin": {
    "UserName": "sysadmin",
    "Email": "admin@lybt.com",
    "DisplayName": "系统管理员",
    "AutoCreateOnStartup": true,
    "SessionTimeoutMinutes": 240
  }
}
```

关键变更: `AutoCreateOnStartup: false -> true`，`ForceChangeOnFirstLogin` 加入 (值为 `false`，测试用户无需首登改密)。

**Step 2: 编译 + 全量测试**

Run: `dotnet test tests/LYBT.Tests.Server.Integration/`
Expected: ALL PASS。如果启用 AutoCreateOnStartup 导致失败，分析原因并调整 WebApiFixture。

---

### Task 2.2: WebApiFixture 适配生产初始化路径

**Files:**
- Modify: `tests/LYBT.Tests.Server.Integration/Fixtures/WebApiFixture.cs`

**Step 1: 确保 WebApiFixture 与 DatabaseInitializationService 兼容**

当 `AutoCreateOnStartup=true` 时，`DatabaseInitializationService` 会在启动时自动创建 sysadmin 用户。WebApiFixture 的 `SeedDefaultUsers` 使用 Upsert 模式 (FindAsync -> 存在则更新)，需确保:

1. 检查 `UpsertUser` 中的 `FindAsync` 是否受 `IsDeleted` 全局过滤器影响 (如果被过滤，应改用 `IgnoreQueryFilters`)
2. 如果 `DatabaseInitializationService` 创建的 sysadmin 使用随机 Guid，而 WebApiFixture 使用固定 `SysAdminUserId`，两者是不同的用户。需要在 `SeedDefaultUsers` 中检查并处理这种情况。

调查并修复，确保两个路径不冲突。

**Step 2: 测试验证**

Run: `dotnet test tests/LYBT.Tests.Server.Integration/`
Expected: ALL PASS

---

### Task 2.3: 添加 RateLimiting 专项集成测试

**Files:**
- Create: `tests/LYBT.Tests.Server.Integration/RateLimiting/RateLimitingIntegrationTests.cs`

**Step 1: 创建测试类**

需要一个使用 `RateLimiting.Enabled=true` 的单独 Fixture，或在测试中动态覆盖配置。

测试场景:
1. Login 端点 5 次/分钟限制: 连续 6 次登录请求，第 6 次应返回 429
2. 429 响应包含正确的 ApiResponse 格式和 errorCode

注意: 此测试必须在独立的 xUnit Collection 中运行 (不与其他集成测试共享 Fixture)，避免速率限制影响其他测试。

**Step 2: 运行新测试**

Run: `dotnet test tests/LYBT.Tests.Server.Integration/ --filter "RateLimiting"`
Expected: ALL PASS

---

## Phase 3: 修复 Server Unit Test Mock 偏差

### Task 3.1: AuthServiceTests 配置键修复

**Files:**
- Modify: `tests/LYBT.Tests.Unit/Modules/Auth/Services/AuthServiceTests.cs:73-82`

**Step 1: 修复配置键**

对照生产 `appsettings.json`，修正 mock 配置键:

```csharp
// 修复: Lybt: 前缀可能不正确，需对照 AuthService 实际读取的配置路径
// 删除重复的 Line 78 (Username vs UserName)
// 修复 ExpireMinutes -> AccessTokenExpirationMinutes
```

调查 `AuthService` 构造函数中实际使用的 IConfiguration 路径，确保 mock 与之一致。

**Step 2: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Unit/ --filter "AuthService"`
Expected: ALL PASS

---

### Task 3.2: UserServiceTests async mock + 验证失败路径

**Files:**
- Modify: `tests/LYBT.Tests.Unit/Modules/Users/Services/UserServiceTests.cs`

**Step 1: 修复 async mock 返回值**

搜索所有 `.Returns(value)` 中 value 非 Task 类型的调用，改为 `.Returns(Task.FromResult(value))` 或确认 NSubstitute 正确处理。

**Step 2: 添加 Validator 失败路径测试**

新增测试方法:

```csharp
[Fact]
public async Task CreateAsync_WhenValidationFails_ShouldReturnFailure()
{
    // Arrange
    var dto = new UserInputDto { UserName = "" }; // 无效输入
    var validationResult = new ValidationResult(new[]
    {
        new ValidationFailure("UserName", "用户名不能为空")
    });
    _validatorMock.ValidateAsync(Arg.Any<UserInputDto>(), Arg.Any<CancellationToken>())
        .Returns(validationResult);

    // Act
    var result = await _sut.CreateAsync(dto);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Message.Should().Contain("验证失败");
}
```

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Unit/ --filter "UserService"`
Expected: ALL PASS

---

### Task 3.3: PatientServiceTests GetPagedWithStatusFilterAsync mock

**Files:**
- Modify: `tests/LYBT.Tests.Unit/Modules/Patients/Services/PatientServiceTests.cs`

**Step 1: 调查实际 PatientService.GetPagedAsync 实现**

确认是否存在 `filterDisabled` 参数和 `GetPagedWithStatusFilterAsync` 调用路径。

**Step 2: 添加 mock 配置**

```csharp
_repositoryMock
    .GetPagedWithStatusFilterAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CommonStatus>())
    .Returns(pagedResult);
```

**Step 3: 添加 filterDisabled=true 测试场景**

```csharp
[Fact]
public async Task GetPagedAsync_WithFilterDisabled_ShouldCallStatusFilter()
{
    // Arrange
    var pagedResult = new PagedResult<PatientEntity> { Items = new List<PatientEntity>() };
    _repositoryMock
        .GetPagedWithStatusFilterAsync(1, 20, null, CommonStatus.Enabled)
        .Returns(pagedResult);

    // Act
    var result = await _sut.GetPagedAsync(1, 20, null, filterDisabled: true);

    // Assert
    await _repositoryMock.Received(1)
        .GetPagedWithStatusFilterAsync(1, 20, null, CommonStatus.Enabled);
}
```

**Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Unit/ --filter "PatientService"`
Expected: ALL PASS

---

### Task 3.4: MedicalCaseCommandServiceTests 补全 mock

**Files:**
- Modify: `tests/LYBT.Tests.Unit/Modules/MedicalCases/Services/MedicalCaseCommandServiceTests.cs`

**Step 1: 补全缺失的 cross-module mock 配置**

在测试 setup/constructor 中添加:

```csharp
// 确保 GetHerbByIdAsync / GetHerbByNameAsync 有默认 mock
_herbCrossModuleMock.GetHerbByIdAsync(Arg.Any<Guid>())
    .Returns((HerbBasicDto?)null);

// 确保 GetByPatientIdAsync 有默认 mock (BR-001)
_repositoryMock.GetByPatientIdAsync(Arg.Any<Guid>())
    .Returns(new List<MedicalCaseEntity>());
```

**Step 2: 添加 BR-001 验证测试 (如果不存在)**

确认是否有测试验证"当患者已有 Active 医案时 CreateAsync 返回失败"。

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Unit/ --filter "MedicalCaseCommand"`
Expected: ALL PASS

---

### Task 3.5: FormulaServiceTests 重载区分

**Files:**
- Modify: `tests/LYBT.Tests.Unit/Modules/Formulas/Services/FormulaServiceTests.cs`

**Step 1: 调查 FormulaService.GetPagedAsync 实际调用**

确认 Service 方法调用的是 3 参数还是 6 参数 `GetPagedWithDetailsAsync`。

**Step 2: 修正 mock 匹配**

确保 mock 的参数签名与 Service 实际调用的 Repository 方法重载一致。

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Unit/ --filter "FormulaService"`
Expected: ALL PASS

---

### Task 3.6: HerbServiceTests 引用检查补全

**Files:**
- Modify: `tests/LYBT.Tests.Unit/Modules/Herbs/Services/HerbServiceTests.cs`

**Step 1: 在 DeleteAsync 测试中补全 CheckReferenceAsync mock**

```csharp
[Fact]
public async Task DeleteAsync_WithExistingId_ShouldSoftDelete()
{
    // Arrange
    var herbId = Guid.NewGuid();
    _repositoryMock.CheckReferenceAsync(herbId)
        .Returns(Result<ReferenceCheckResultDto>.Success(
            new ReferenceCheckResultDto { HasReferences = false }));

    // Act
    var result = await _sut.DeleteAsync(herbId);

    // Assert
    result.IsSuccess.Should().BeTrue();
    await _repositoryMock.Received(1).DeleteAsync(herbId);
}
```

**Step 2: 添加引用保护测试**

```csharp
[Fact]
public async Task DeleteAsync_WithReferences_ShouldReturnFailure()
{
    // Arrange
    var herbId = Guid.NewGuid();
    _repositoryMock.CheckReferenceAsync(herbId)
        .Returns(Result<ReferenceCheckResultDto>.Success(
            new ReferenceCheckResultDto { HasReferences = true, ReferenceCount = 3 }));

    // Act
    var result = await _sut.DeleteAsync(herbId);

    // Assert
    result.IsSuccess.Should().BeFalse();
    await _repositoryMock.DidNotReceive().DeleteAsync(herbId);
}
```

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Unit/ --filter "HerbService"`
Expected: ALL PASS

---

### Task 3.7: Phase 3 全量验证

Run: `dotnet test tests/LYBT.Tests.Unit/`
Expected: ALL PASS (1302+ tests)

---

## Phase 4: 修复 Desktop 测试 + Architecture Tests

### Task 4.1: LoginCoordinator Local mode 测试补全

**Files:**
- Modify: `tests/LYBT.Tests.Desktop.Unit/Shell/Services/Login/LoginCoordinatorTests.cs`

**Step 1: 添加 ILocalAuthService mock 和 local mode 构造**

在测试 setup 中添加:

```csharp
private readonly ILocalAuthService _localAuthService = Substitute.For<ILocalAuthService>();

// 构造时传入 localAuthService
_sut = new LoginCoordinator(
    _logger, _authService, _tokenStorage, _sessionManager,
    _moduleLoading, _navigationCoordinator, _stateMachine, _configuration,
    credentialVault: null, usernameStorage: null, localAuthService: _localAuthService);
```

**Step 2: 添加 Local mode 登录测试**

```csharp
[Fact]
public async Task LoginLocalAsync_WithValidCredentials_ShouldSucceed()
{
    // Arrange
    _localAuthService.ValidateAsync("admin", "password")
        .Returns(new LocalUserInfo { ... });

    // Act
    var result = await _sut.LoginAsync("admin", "password", isLocal: true);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Desktop.Unit/ --filter "LoginCoordinator"`
Expected: ALL PASS

---

### Task 4.2: Desktop E2E Fixture DI 生命周期修复

**Files:**
- Modify: `tests/LYBT.Tests.Desktop.Integration/EndToEnd/Fixtures/DesktopE2ETestFixture.cs`

**Step 1: 调查生产 DI 生命周期**

对比 production `ServiceCollectionExtensions.cs` 中的注册方式，确认 Repository 等服务的正确生命周期。

**Step 2: 修复 Singleton -> Transient/Scoped**

将错误注册的 Singleton 改为正确的生命周期。注意: Mock 对象保持 Singleton (因为 NSubstitute 的 mock 是无状态的)。

**Step 3: 运行 E2E 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop.Integration/ --filter "EndToEnd"`
Expected: ALL PASS

---

### Task 4.3: LoginViewModelTests UsernameChange 真实测试

**Files:**
- Modify: `tests/LYBT.Tests.Desktop.Unit/Auth/ViewModels/LoginViewModelTests.cs:469-519`

**Step 1: 评估是否可以直接测试 LoginViewModel**

调查 `LoginViewModel.Username` setter 的 `OnUsernameChanged` 逻辑是否可以在测试中正常触发 (不依赖 `Application.Current`)。

**Step 2: 如果可以，替换 UsernameChangeLogicTester 为真实 ViewModel 测试**

如果不可以 (WPF 依赖)，在 `UsernameChangeLogicTester` 旁添加注释说明原因并保留。

**Step 3: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Desktop.Unit/ --filter "LoginViewModel"`
Expected: ALL PASS

---

### Task 4.4: AuthenticationService LoginWithAutoTokenAsync 补全

**Files:**
- Modify: `tests/LYBT.Tests.Desktop.Unit/Foundation/Security/AuthenticationServiceTests.cs`

**Step 1: 添加 AutoLogin 测试**

```csharp
[Fact]
public async Task LoginWithAutoTokenAsync_Success_ReturnsLoginResponse()
{
    // Arrange
    var request = new AutoLoginRequest { Token = "valid-token", DeviceId = "test-device" };
    var loginResponse = new LoginResponse { /* ... */ };
    _authApi.LoginWithAutoTokenAsync(request)
        .Returns(ApiResponse<LoginResponse>.Ok(loginResponse));

    // Act
    var result = await _authService.LoginWithAutoTokenAsync(request);

    // Assert
    result.IsSuccess.Should().BeTrue();
}

[Fact]
public async Task LoginWithAutoTokenAsync_Failure_ReturnsError()
{
    // Arrange
    var request = new AutoLoginRequest { Token = "expired-token", DeviceId = "test-device" };
    _authApi.LoginWithAutoTokenAsync(request)
        .Returns(ApiResponse<LoginResponse>.Fail("Token 已过期"));

    // Act
    var result = await _authService.LoginWithAutoTokenAsync(request);

    // Assert
    result.IsSuccess.Should().BeFalse();
}
```

**Step 2: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Desktop.Unit/ --filter "AuthenticationService"`
Expected: ALL PASS

---

### Task 4.5: Architecture Tests 修复

**Files:**
- Modify: `tests/LYBT.Tests.Architecture/CustomControlArchTests.cs:118-159`
- Modify: `tests/LYBT.Tests.Architecture/ArchTests.cs` (Batch2_ConfigurationDirectRead)
- Modify: `tests/LYBT.Tests.Architecture/ServerArchTests.cs` (循环依赖白名单)

**Step 1: DataContext 构造函数检测**

`SetsDataContextInConstructor` 方法始终返回 false。选项:
- A: 标记测试为 `[Skip("IL analysis not implemented")]` 并在 CLAUDE.md 记录
- B: 实现简单的 Roslyn 检测

推荐 A (YAGNI)，除非用户要求实现。

**Step 2: 配置直读占位测试**

如果 `Batch2_ConfigurationDirectRead` 确实是占位符 (仅 `true.Should().BeTrue()`)，标记为 `[Skip]` 并记录原因。

**Step 3: 循环依赖白名单收紧**

缩小 `Module.Sync` 和 `MedicalCase` 的排除范围，仅排除已知的合法跨模块依赖。

**Step 4: 运行测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: ALL PASS

---

### Task 4.6: Phase 4 全量验证

Run: `dotnet test tests/LYBT.Tests.Desktop.Unit/ && dotnet test tests/LYBT.Tests.Desktop.Integration/ && dotnet test tests/LYBT.Tests.Architecture/`
Expected: ALL PASS

---

## Phase 5: 全量验证 + 文档

### Task 5.1: 全量编译与测试

Run:
```bash
dotnet build LYBTZYZS.sln
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"
```
Expected: ALL PASS (2366+ tests)

### Task 5.2: 偏差清单确认

逐项确认 26 个偏差:
- 2 CRITICAL: 已修正 (Task 1.1, Task 3.1)
- 12 HIGH: 已修正 (Tasks 1.2, 2.3, 3.2-3.6, 4.1-4.2, 4.5)
- 12 MEDIUM: 已修正或标记为设计决策

### Task 5.3: 更新 progress.md 最终结果

---

## Execution Dependencies

```
Task 1.1 -> 1.2 -> 1.3 -> 1.4 (串行)
Task 1.4 -> Task 2.1 -> 2.2 -> 2.3 (串行)
Task 2.3 -> Task 3.1-3.6 (可并行) -> 3.7
Task 2.3 -> Task 4.1-4.5 (可并行) -> 4.6
Task 3.7 + 4.6 -> Task 5.1 -> 5.2 -> 5.3
```
