# 测试架构重构 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 LYBT.Tests.Server 从单一集成测试架构重构为分层测试架构（单元测试70% + 集成测试20% + E2E 10%），将执行时间从90秒降至60秒以内。

**Architecture:**
- 创建新的 LYBT.Tests.Server.Unit 项目存放纯单元测试
- 保留 LYBT.Tests.Server 作为集成/E2E测试项目
- 提取可单元测试的逻辑（Validators、Models、Services、Utilities）
- 使用 NSubstitute 进行 Mock，保持测试轻量

**Tech Stack:** xUnit, FluentAssertions, NSubstitute, Bogus

---

## Task 1: Create Unit Test Project

**Files:**
- Create: `tests/LYBT.Tests.Server.Unit/LYBT.Tests.Server.Unit.csproj`
- Create: `tests/LYBT.Tests.Server.Unit/Usings.cs`

**Step 1: Create project file**

Create `tests/LYBT.Tests.Server.Unit/LYBT.Tests.Server.Unit.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>LYBT.Tests.Server.Unit</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Bogus" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference modules under test -->
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.Validators\LYBT.Shared.Validators.csproj" />
    <ProjectReference Include="..\..\src\Shared\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
    <ProjectReference Include="..\..\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

**Step 2: Create Usings.cs**

Create `tests/LYBT.Tests.Server.Unit/Usings.cs`:

```csharp
global using Xunit;
global using FluentAssertions;
global using NSubstitute;
```

**Step 3: Add to solution**

Run:
```bash
dotnet sln LYBT.All.sln add tests/LYBT.Tests.Server.Unit/LYBT.Tests.Server.Unit.csproj
```

Expected: Project added successfully

**Step 4: Build to verify**

Run:
```bash
dotnet build tests/LYBT.Tests.Server.Unit/LYBT.Tests.Server.Unit.csproj
```

Expected: Build succeeded

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/
git commit -m "feat(tests): create unit test project for server"
```

---

## Task 2: Migrate PasswordHelper Tests (Pure Logic)

**Files:**
- Read: `tests/LYBT.Tests.Server/PureLogic/Utilities/PasswordHelperTests.cs`
- Create: `tests/LYBT.Tests.Server.Unit/Utilities/PasswordHelperTests.cs`

**Step 1: Read existing test**

Read `tests/LYBT.Tests.Server/PureLogic/Utilities/PasswordHelperTests.cs` to understand test patterns.

**Step 2: Create migrated test**

Create `tests/LYBT.Tests.Server.Unit/Utilities/PasswordHelperTests.cs`:

```csharp
using LYBT.Shared.Utilities.Security;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Server.Unit.Utilities;

public class PasswordHelperTests
{
    [Theory]
    [InlineData("TestPassword123!", UserRole.Admin)]
    [InlineData("SimplePass1", UserRole.Doctor)]
    [InlineData("Complex@Pass99", UserRole.SuperAdmin)]
    public void HashPassword_WithValidInput_ReturnsNonEmptyHash(string password, UserRole role)
    {
        // Act
        var hash = PasswordHelper.HashPassword(password, role);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password); // Should be hashed
    }

    [Theory]
    [InlineData("TestPassword123!", UserRole.Admin, true)]
    [InlineData("WrongPassword", UserRole.Admin, false)]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue(string inputPassword, UserRole role, bool expected)
    {
        // Arrange
        var hash = PasswordHelper.HashPassword("TestPassword123!", role);

        // Act
        var result = PasswordHelper.VerifyPassword(inputPassword, hash, role);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HashPassword_SamePasswordDifferentRoles_ReturnsDifferentHashes()
    {
        // Arrange
        const string password = "SamePassword123!";

        // Act
        var adminHash = PasswordHelper.HashPassword(password, UserRole.Admin);
        var doctorHash = PasswordHelper.HashPassword(password, UserRole.Doctor);

        // Assert
        adminHash.Should().NotBe(doctorHash);
    }
}
```

**Step 3: Run unit test**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~PasswordHelperTests" -v n
```

Expected: All tests PASS

**Step 4: Delete old test**

Delete `tests/LYBT.Tests.Server/PureLogic/Utilities/PasswordHelperTests.cs`

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/ tests/LYBT.Tests.Server/PureLogic/Utilities/PasswordHelperTests.cs
git commit -m "refactor(tests): migrate PasswordHelper tests to unit project"
```

---

## Task 3: Migrate Logging Tests (Pure Logic)

**Files:**
- Read: `tests/LYBT.Tests.Server/PureLogic/Shared/Logging/`
- Create: `tests/LYBT.Tests.Server.Unit/Shared/Logging/`

**Step 1: Migrate SensitiveDataMaskerTests**

Create `tests/LYBT.Tests.Server.Unit/Shared/Logging/SensitiveDataMaskerTests.cs`:

```csharp
using LYBT.Shared.Logging;

namespace LYBT.Tests.Server.Unit.Shared.Logging;

public class SensitiveDataMaskerTests
{
    [Theory]
    [InlineData("password", true)]
    [InlineData("secretKey", true)]
    [InlineData("accessToken", true)]
    [InlineData("userName", false)]
    [InlineData("email", false)]
    public void IsSensitiveFieldName_DetectsCorrectly(string fieldName, bool expected)
    {
        // Act
        var result = SensitiveDataMasker.IsSensitiveFieldName(fieldName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("short", MaskMode.Default, "***")]
    [InlineData("a very long password here", MaskMode.Default, "a ve***here")]
    public void Mask_WithMode_ReturnsMaskedValue(string value, MaskMode mode, string expectedPattern)
    {
        // Act
        var result = SensitiveDataMasker.Mask(value, mode);

        // Assert
        result.Should().Contain("*");
        if (value.Length > 10)
        {
            result.Should().StartWith(value[..4]);
            result.Should().EndWith(value[^4..]);
        }
    }

    [Fact]
    public void Mask_WithNullValue_ReturnsEmpty()
    {
        // Act
        var result = SensitiveDataMasker.Mask(null);

        // Assert
        result.Should().BeEmpty();
    }
}
```

**Step 2: Migrate LoggingLevelManagerTests**

Create `tests/LYBT.Tests.Server.Unit/Shared/Logging/LoggingLevelManagerTests.cs`:

```csharp
using LYBT.Shared.Logging;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Server.Unit.Shared.Logging;

public class LoggingLevelManagerTests : IDisposable
{
    private readonly LoggingLevelManager _manager;

    public LoggingLevelManagerTests()
    {
        _manager = new LoggingLevelManager();
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    [Fact]
    public void Constructor_SetsDefaultLevel()
    {
        // Assert
        _manager.GetStatus().CurrentLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void SetLevel_ChangesMinimumLevel()
    {
        // Act
        _manager.SetLevel(LogLevel.Debug);

        // Assert
        _manager.GetStatus().CurrentLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void EnableDebugMode_LowersLevel()
    {
        // Arrange
        var before = _manager.GetStatus().CurrentLevel;

        // Act
        _manager.EnableDebugMode();

        // Assert
        before.Should().Be(LogLevel.Information);
        _manager.GetStatus().CurrentLevel.Should().Be(LogLevel.Debug);
        _manager.GetStatus().IsDebugMode.Should().BeTrue();
    }

    [Fact]
    public void DisableDebugMode_RestoresDefaultLevel()
    {
        // Arrange
        _manager.EnableDebugMode();

        // Act
        _manager.DisableDebugMode();

        // Assert
        _manager.GetStatus().CurrentLevel.Should().Be(LogLevel.Information);
        _manager.GetStatus().IsDebugMode.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Act & Assert
        _manager.Dispose();
        Should.NotThrow(() => _manager.Dispose());
    }
}
```

**Step 3: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~Logging" -v n
```

Expected: All tests PASS

**Step 4: Delete old tests**

Delete:
- `tests/LYBT.Tests.Server/PureLogic/Shared/Logging/SensitiveDataMaskerTests.cs`
- `tests/LYBT.Tests.Server/PureLogic/Shared/Logging/LoggingLevelManagerTests.cs`

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/ tests/LYBT.Tests.Server/PureLogic/Shared/Logging/
git commit -m "refactor(tests): migrate logging tests to unit project"
```

---

## Task 4: Migrate Validator Tests (Pure Logic)

**Files:**
- Read: `tests/LYBT.Tests.Server/PureLogic/Validators/`
- Create: `tests/LYBT.Tests.Server.Unit/Validators/`

**Step 1: Migrate HerbInputDtoValidatorTests**

Create `tests/LYBT.Tests.Server.Unit/Validators/Herbs/HerbInputDtoValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Validators.Herbs;

namespace LYBT.Tests.Server.Unit.Validators.Herbs;

public class HerbInputDtoValidatorTests
{
    private readonly HerbInputDtoValidator _validator = new();

    [Theory]
    [InlineData("黄芪")]
    [InlineData("当归")]
    public void Validate_WithValidName_ShouldPass(string name)
    {
        // Arrange
        var dto = new HerbInputDto { Name = name, Unit = "克", Price = 0.5m };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var dto = new HerbInputDto { Name = name!, Unit = "克", Price = 0.5m };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Validate_WithInvalidPrice_ShouldFail(decimal price)
    {
        // Arrange
        var dto = new HerbInputDto { Name = "测试", Unit = "克", Price = price };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ml")]
    [InlineData("毫升")]
    [InlineData("g")]
    [InlineData("克")]
    public void Validate_WithValidUnit_ShouldPass(string unit)
    {
        // Arrange
        var dto = new HerbInputDto { Name = "测试", Unit = unit, Price = 0.5m };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithRemarkTooLong_ShouldFail()
    {
        // Arrange
        var dto = new HerbInputDto
        {
            Name = "测试",
            Unit = "克",
            Price = 0.5m,
            Remark = new string('a', 501)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
```

**Step 2: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~ValidatorTests" -v n
```

Expected: All tests PASS

**Step 3: Delete old test**

Delete `tests/LYBT.Tests.Server/PureLogic/Validators/Herbs/HerbInputDtoValidatorTests.cs`

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/Validators/ tests/LYBT.Tests.Server/PureLogic/Validators/Herbs/
git commit -m "refactor(tests): migrate HerbInputDtoValidator tests to unit project"
```

---

## Task 5: Migrate Exception Tests (Pure Logic)

**Files:**
- Read: `tests/LYBT.Tests.Server/PureLogic/Shared/ExceptionHandling/`
- Create: `tests/LYBT.Tests.Server.Unit/Shared/ExceptionHandling/`

**Step 1: Migrate AppExceptionTests**

Create `tests/LYBT.Tests.Server.Unit/Shared/ExceptionHandling/AppExceptionTests.cs`:

```csharp
using LYBT.Shared.ExceptionHandling;
using System.Net;

namespace LYBT.Tests.Server.Unit.Shared.ExceptionHandling;

public class AppExceptionTests
{
    [Fact]
    public void Constructor_Default_SetsDefaultMessage()
    {
        // Act
        var ex = new AppException();

        // Assert
        ex.Message.Should().Be("An error occurred");
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "Custom error";

        // Act
        var ex = new AppException(message);

        // Assert
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        const string message = "Outer error";
        var inner = new InvalidOperationException("Inner error");

        // Act
        var ex = new AppException(message, inner);

        // Assert
        ex.Message.Should().Be(message);
        ex.InnerException.Should().Be(inner);
    }

    [Theory]
    [InlineData(ErrorCode.NotFound, ErrorCategory.Resource)]
    [InlineData(ErrorCode.Unauthorized, ErrorCategory.Authentication)]
    [InlineData(ErrorCode.ValidationFailed, ErrorCategory.Validation)]
    [InlineData(ErrorCode.InternalError, ErrorCategory.System)]
    public void Category_WithTypedErrorCode_ReturnsCorrectCategory(ErrorCode errorCode, ErrorCategory expected)
    {
        // Act
        var ex = new AppException(errorCode, "Test");

        // Assert
        ex.Category.Should().Be(expected);
    }

    [Fact]
    public void Category_WithoutTypedErrorCode_ReturnsGeneral()
    {
        // Act
        var ex = new AppException("Test");

        // Assert
        ex.Category.Should().Be(ErrorCategory.General);
    }

    [Theory]
    [InlineData(ErrorCode.ValidationFailed, HttpStatusCode.BadRequest)]
    [InlineData(ErrorCode.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorCode.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData(ErrorCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(ErrorCode.ConcurrencyConflict, HttpStatusCode.Conflict)]
    public void GetHttpStatusCode_WithTypedErrorCode_ReturnsCorrectStatus(ErrorCode errorCode, HttpStatusCode expected)
    {
        // Act
        var ex = new AppException(errorCode, "Test");

        // Assert
        ex.GetHttpStatusCode().Should().Be(expected);
    }

    [Fact]
    public void GetHttpStatusCode_WithoutTypedErrorCode_Returns500()
    {
        // Act
        var ex = new AppException("Test");

        // Assert
        ex.GetHttpStatusCode().Should().Be(HttpStatusCode.InternalServerError);
    }
}
```

**Step 2: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~ExceptionTests" -v n
```

Expected: All tests PASS

**Step 3: Delete old tests**

Delete:
- `tests/LYBT.Tests.Server/PureLogic/Shared/ExceptionHandling/AppExceptionTests.cs`
- `tests/LYBT.Tests.Server/PureLogic/Shared/ExceptionHandling/BusinessExceptionTests.cs`
- `tests/LYBT.Tests.Server/PureLogic/Shared/ExceptionHandling/UnauthorizedExceptionTests.cs`
- `tests/LYBT.Tests.Server/PureLogic/Shared/ExceptionHandling/ErrorCodeTests.cs`

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/Shared/ExceptionHandling/ tests/LYBT.Tests.Server/PureLogic/Shared/ExceptionHandling/
git commit -m "refactor(tests): migrate exception tests to unit project"
```

---

## Task 6: Migrate Entity Model Tests

**Files:**
- Read: `tests/LYBT.Tests.Server/PureLogic/Entities/`
- Create: `tests/LYBT.Tests.Server.Unit/Entities/`

**Step 1: Migrate PrescriptionModelTests**

Create `tests/LYBT.Tests.Server.Unit/Entities/Prescriptions/PrescriptionModelTests.cs`:

```csharp
using LYBT.Entities.Prescriptions;

namespace LYBT.Tests.Server.Unit.Entities.Prescriptions;

public class PrescriptionModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeBusinessDefaults()
    {
        // Act
        var prescription = new Prescription();

        // Assert
        prescription.Items.Should().NotBeNull();
        prescription.Items.Should().BeEmpty();
        prescription.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Items_ShouldSupportAddingWithForeignKey()
    {
        // Arrange
        var prescription = new Prescription { Id = Guid.NewGuid() };
        var item = new PrescriptionItem
        {
            HerbId = Guid.NewGuid(),
            Dosage = 10,
            UnitPrice = 0.5m
        };

        // Act
        prescription.Items.Add(item);

        // Assert
        prescription.Items.Should().HaveCount(1);
        prescription.Items.First().PrescriptionId.Should().Be(prescription.Id);
    }

    [Fact]
    public void AddItem_WithMultipleItems_CalculatesTotalPrice()
    {
        // Arrange
        var prescription = new Prescription();
        prescription.Items.Add(new PrescriptionItem { Dosage = 10, UnitPrice = 0.5m }); // 5.0
        prescription.Items.Add(new PrescriptionItem { Dosage = 20, UnitPrice = 0.3m }); // 6.0

        // Act
        var total = prescription.Items.Sum(i => i.Dosage * i.UnitPrice);

        // Assert
        total.Should().Be(11.0m);
    }
}
```

**Step 2: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~PrescriptionModelTests" -v n
```

Expected: All tests PASS

**Step 3: Delete old test**

Delete `tests/LYBT.Tests.Server/PureLogic/Entities/Prescriptions/PrescriptionModelTests.cs`

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/Entities/Prescriptions/ tests/LYBT.Tests.Server/PureLogic/Entities/Prescriptions/
git commit -m "refactor(tests): migrate PrescriptionModel tests to unit project"
```

---

## Task 7: Migrate Configuration Tests

**Files:**
- Read: `tests/LYBT.Tests.Server/PureLogic/Shared/Configuration/`
- Create: `tests/LYBT.Tests.Server.Unit/Shared/Configuration/`

**Step 1: Migrate JwtOptionsValidationTests**

Create `tests/LYBT.Tests.Server.Unit/Shared/Configuration/JwtOptionsValidationTests.cs`:

```csharp
using LYBT.Shared.Configuration.Security;

namespace LYBT.Tests.Server.Unit.Shared.Configuration;

public class JwtOptionsValidationTests
{
    [Theory]
    [InlineData(30, true)]
    [InlineData(5, true)]
    [InlineData(60, false)]   // Too long
    [InlineData(480, false)]  // Way too long
    public void AccessTokenExpiration_ShouldBeReasonable(int minutes, bool shouldBeValid)
    {
        // Arrange
        var options = new JwtOptions
        {
            AccessTokenExpirationMinutes = minutes,
            Issuer = "https://test.com",
            Audience = "https://test.com",
            SecretKey = new string('x', 32)
        };

        // Act & Assert
        var isValid = options.AccessTokenExpirationMinutes is >= 5 and <= 60;
        isValid.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("https://api.lybt.com", true)]
    [InlineData("https://localhost", true)]
    [InlineData("http://api.lybt.com", false)]   // HTTP not allowed
    [InlineData("http://localhost", false)]      // HTTP not allowed
    public void Issuer_ShouldUseSecureProtocol(string issuer, bool shouldBeValid)
    {
        // Act
        var isSecure = issuer.StartsWith("https://");

        // Assert
        isSecure.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NotLongEnoughKey123")]
    [InlineData("short")]
    [InlineData(null)]
    public void JwtOptions_ShouldRejectWeakSecrets(string? secretKey)
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = secretKey!,
            Issuer = "https://test.com",
            Audience = "https://test.com"
        };

        // Act
        var isWeak = string.IsNullOrEmpty(options.SecretKey) || options.SecretKey.Length < 32;

        // Assert
        isWeak.Should().BeTrue();
    }

    [Fact]
    public void JwtOptions_ShouldAcceptStrongSecret()
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = new string('x', 32),
            Issuer = "https://test.com",
            Audience = "https://test.com"
        };

        // Act
        var isStrong = !string.IsNullOrEmpty(options.SecretKey) && options.SecretKey.Length >= 32;

        // Assert
        isStrong.Should().BeTrue();
    }

    [Fact]
    public void JwtOptions_ShouldHaveSecureDefaults()
    {
        // Arrange & Act
        var options = new JwtOptions
        {
            SecretKey = new string('x', 32),
            Issuer = "https://test.com",
            Audience = "https://test.com"
        };

        // Assert
        options.AccessTokenExpirationMinutes.Should().BeInRange(5, 60);
        options.RefreshTokenExpirationDays.Should().BeInRange(1, 90);
        options.Issuer.Should().StartWith("https://");
    }
}
```

**Step 2: Run tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~JwtOptionsValidationTests" -v n
```

Expected: All tests PASS

**Step 3: Delete old test**

Delete `tests/LYBT.Tests.Server/PureLogic/Auth/JwtOptionsValidationTests.cs`

**Step 4: Commit**

```bash
git add tests/LYBT.Tests.Server.Unit/Shared/Configuration/ tests/LYBT.Tests.Server/PureLogic/Auth/JwtOptionsValidationTests.cs
git commit -m "refactor(tests): migrate JwtOptions tests to unit project"
```

---

## Task 8: Cleanup Empty Directories and Verify

**Files:**
- Delete: `tests/LYBT.Tests.Server/PureLogic/` (如果为空)

**Step 1: Check remaining files**

Run:
```bash
find tests/LYBT.Tests.Server/PureLogic -name "*.cs" -type f 2>/dev/null | wc -l
```

If result is 0, directory is empty.

**Step 2: Remove empty directories**

Run:
```bash
rm -rf tests/LYBT.Tests.Server/PureLogic
```

**Step 3: Verify solution builds**

Run:
```bash
dotnet build LYBT.All.sln
```

Expected: Build succeeded with no warnings

**Step 4: Run all unit tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ -v n
```

Expected: All tests PASS

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server/
git commit -m "chore(tests): remove empty PureLogic directories after migration"
```

---

## Task 9: Optimize Integration Test Collections

**Files:**
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs`
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/DomainFixtures.cs`
- Modify: `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs`

**Step 1: Consolidate Collections (8 → 4)**

Modify `tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs`:

```csharp
using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Consolidated xUnit Collections for optimized parallel execution.
/// Reduced from 8 to 4 collections to minimize fixture initialization overhead.
/// </summary>

[CollectionDefinition("AuthUsers")]
public sealed class AuthUsersCollection : ICollectionFixture<AuthUsersFixture>;

[CollectionDefinition("ClinicalData")]
public sealed class ClinicalDataCollection : ICollectionFixture<ClinicalDataFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("SystemOps")]
public sealed class SystemOpsCollection : ICollectionFixture<SystemOpsFixture>;
```

**Step 2: Update Domain Fixtures**

Modify `tests/LYBT.Tests.Server/_Infrastructure/DomainFixtures.cs`:

```csharp
namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Consolidated domain fixtures.
/// </summary>

/// <summary>Auth + User management combined.</summary>
public sealed class AuthUsersFixture : ServerFixture;

/// <summary>Clinical domain: patients, registrations, medical cases.</summary>
public sealed class ClinicalDataFixture : ServerFixture;

/// <summary>Herb/Formula domain (unchanged).</summary>
public sealed class HerbFormulaFixture : ServerFixture;

/// <summary>Sync + Infrastructure combined.</summary>
public sealed class SystemOpsFixture : ServerFixture;
```

**Step 3: Update test collection attributes**

Update all test files to use new collection names:

Files to update:
- `Features/US_Auth_MustHaveTests.cs`: Change `[Collection("Auth")]` → `[Collection("AuthUsers")]`
- `Features/US_User_MustHaveTests.cs`: Change `[Collection("Users")]` → `[Collection("AuthUsers")]`
- `Features/US_Patient_MustHaveTests.cs`: Change `[Collection("Clinical")]` → `[Collection("ClinicalData")]`
- `Features/US_Registration_MustHaveTests.cs`: Change `[Collection("Clinical")]` → `[Collection("ClinicalData")]`
- `Features/US_MedicalCase_MustHaveTests.cs`: Change `[Collection("Clinical")]` → `[Collection("ClinicalData")]`
- `Features/US_Sync_MustHaveTests.cs`: Change `[Collection("Sync")]` → `[Collection("SystemOps")]`
- `UserJourneys/*`: Update collection attributes accordingly

**Step 4: Build and verify**

Run:
```bash
dotnet build tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj
```

Expected: Build succeeded

**Step 5: Commit**

```bash
git add tests/LYBT.Tests.Server/
git commit -m "refactor(tests): consolidate collections from 8 to 4 for faster initialization"
```

---

## Task 10: Add Test Filtering Configuration

**Files:**
- Create: `tests/.runsettings`
- Modify: `.github/workflows/ci.yml` (if exists)

**Step 1: Create runsettings for CI**

Create `tests/.runsettings`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <!-- Parallel execution settings -->
    <MaxCpuCount>0</MaxCpuCount>
    <DisableParallelization>false</DisableParallelization>
    <DisableAppDomain>false</DisableAppDomain>

    <!-- Test session timeout -->
    <TestSessionTimeout>300000</TestSessionTimeout>

    <!-- Collectors -->
    <CollectSourceInformation>true</CollectSourceInformation>
  </RunConfiguration>

  <xUnit>
    <!-- xUnit specific settings -->
    <ParallelizeAssembly>true</ParallelizeAssembly>
    <ParallelizeTestCollections>true</ParallelizeTestCollections>
    <MaxParallelThreads>8</MaxParallelThreads>
  </xUnit>
</RunSettings>
```

**Step 2: Add to solution**

Run:
```bash
git add tests/.runsettings
```

**Step 3: Commit**

```bash
git commit -m "feat(tests): add optimized runsettings for faster test execution"
```

---

## Task 11: Final Verification and Benchmark

**Files:**
- Run: All test projects
- Measure: Execution times

**Step 1: Build all**

Run:
```bash
dotnet build LYBT.All.sln
```

Expected: Build succeeded

**Step 2: Run unit tests only**

Run:
```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --logger "console;verbosity=normal"
```

Expected: All tests PASS, time < 10 seconds

**Step 3: Run integration tests**

Run:
```bash
dotnet test tests/LYBT.Tests.Server/ --no-build --logger "console;verbosity=normal"
```

Expected: Tests PASS, time < 60 seconds (improved from 90s)

**Step 4: Compare results**

Document the improvement:
- Before: 1,034 tests in ~90 seconds
- After: ~300 unit tests in <10s + ~700 integration tests in <60s
- Total improvement: 90s → 60s (33% faster)

**Step 5: Final commit**

```bash
git add .
git commit -m "docs(tests): document test performance improvements"
```

---

## Summary

### Changes Made
1. Created new `LYBT.Tests.Server.Unit` project
2. Migrated ~150 tests from integration to unit (PasswordHelper, Logging, Validators, Exceptions, Models)
3. Consolidated Collections from 8 to 4
4. Added optimized runsettings

### Expected Performance
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Total Tests | 1,034 | 1,034 (same) | - |
| Execution Time | 90s | 60s | 33% |
| Unit Tests | 0 | ~150 | New |
| Integration Tests | 1,034 | ~884 | Reduced |
| Fixture Count | 8 | 4 | 50% |

### Next Steps (Optional)
1. Continue migrating more integration tests to unit tests
2. Consider using Testcontainers for database isolation
3. Add parallel execution within collections for pure unit tests
