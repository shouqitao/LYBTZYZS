# 单元测试最佳实践指南

**版本**: 1.0
**创建时间**: 2025-09-21
**维护团队**: 开发团队

## 概述

本指南总结凌隐宝堂项目中单元测试的最佳实践，帮助团队编写高质量、可维护的测试代码。

## 目录

- [测试设计原则](#测试设计原则)
- [测试命名规范](#测试命名规范)
- [测试结构模式](#测试结构模式)
- [Mock使用规范](#mock使用规范)
- [数据驱动测试](#数据驱动测试)
- [异常处理测试](#异常处理测试)
- [性能测试考虑](#性能测试考虑)
- [代码覆盖率策略](#代码覆盖率策略)
- [测试维护](#测试维护)
- [常见反模式](#常见反模式)

## 测试设计原则

### FIRST原则

#### Fast (快速)
```csharp
// ✅ 好的实践 - 使用内存数据库
[Fact]
public void CreateUser_Should_SaveToDatabase_When_ValidDataProvided()
{
    // 使用InMemory数据库，测试运行很快
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    using var context = new AppDbContext(options);
    var repository = new UserRepository(context);

    // 测试逻辑...
}

// ❌ 避免 - 依赖真实数据库
[Fact]
public void CreateUser_Slow_Test()
{
    // 连接真实SQL Server，测试很慢
    var connectionString = "Server=localhost;Database=LYBT;...";
    // ...
}
```

#### Independent (独立)
```csharp
// ✅ 好的实践 - 每个测试独立
public class UserServiceTests
{
    [Fact]
    public void Test_CreateUser_Success()
    {
        // 每个测试使用独立的上下文
        using var context = CreateInMemoryContext();
        var service = new UserService(context);

        // 测试逻辑...
    }

    [Fact]
    public void Test_UpdateUser_Success()
    {
        // 不依赖其他测试的状态
        using var context = CreateInMemoryContext();
        var service = new UserService(context);

        // 独立的测试数据准备
        var user = new User { Id = Guid.NewGuid(), Username = "test" };
        context.Users.Add(user);
        context.SaveChanges();

        // 测试逻辑...
    }
}

// ❌ 避免 - 测试间有依赖关系
public class BadTestExample
{
    private static User _globalUser; // 测试间共享状态

    [Fact]
    public void Test1_CreateUser()
    {
        _globalUser = new User(); // 影响其他测试
    }

    [Fact]
    public void Test2_UpdateUser()
    {
        // 依赖Test1的结果
        _globalUser.Name = "Updated";
    }
}
```

#### Repeatable (可重复)
```csharp
// ✅ 好的实践 - 固定的测试数据
[Fact]
public void CalculateAge_Should_ReturnCorrectAge_When_ValidBirthDate()
{
    // 使用固定日期，避免时间相关的不确定性
    var fixedCurrentDate = new DateTime(2025, 9, 21);
    var birthDate = new DateTime(1990, 9, 21);

    var age = DateHelper.CalculateAge(birthDate, fixedCurrentDate);

    age.Should().Be(35);
}

// ❌ 避免 - 依赖当前时间
[Fact]
public void CalculateAge_Unreliable_Test()
{
    var birthDate = DateTime.Now.AddYears(-35); // 结果会随时间变化
    var age = DateHelper.CalculateAge(birthDate);
    // 测试可能因为运行时间不同而失败
}
```

#### Self-Validating (自验证)
```csharp
// ✅ 好的实践 - 明确的断言
[Fact]
public void Login_Should_ReturnToken_When_ValidCredentials()
{
    // Arrange
    var credentials = new LoginDto { Username = "admin", Password = "password" };

    // Act
    var result = _authService.Login(credentials);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.Data.Token.Should().NotBeNullOrEmpty();
    result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
}

// ❌ 避免 - 需要人工判断结果
[Fact]
public void Login_Manual_Verification()
{
    var result = _authService.Login(credentials);

    // 需要人工检查控制台输出
    Console.WriteLine($"Login result: {result}");

    // 没有自动化断言
}
```

#### Timely (及时)
```csharp
// ✅ 好的实践 - TDD方式，先写测试
[Fact]
public void ValidatePassword_Should_ReturnFalse_When_PasswordTooShort()
{
    // 先写测试，定义期望行为
    var password = "123";

    var isValid = PasswordValidator.Validate(password);

    isValid.Should().BeFalse();
}

// 然后实现PasswordValidator.Validate方法
```

### Single Responsibility (单一职责)
```csharp
// ✅ 好的实践 - 每个测试只验证一个行为
[Fact]
public void CreateUser_Should_ReturnSuccess_When_ValidDataProvided()
{
    // 只测试成功创建用户的场景
}

[Fact]
public void CreateUser_Should_ReturnFailure_When_UsernameAlreadyExists()
{
    // 只测试用户名重复的场景
}

[Fact]
public void CreateUser_Should_ReturnFailure_When_PasswordTooWeak()
{
    // 只测试密码太弱的场景
}

// ❌ 避免 - 一个测试验证多个行为
[Fact]
public void CreateUser_Multiple_Scenarios()
{
    // 测试成功场景
    var result1 = _service.CreateUser(validUser);
    result1.IsSuccess.Should().BeTrue();

    // 测试失败场景
    var result2 = _service.CreateUser(invalidUser);
    result2.IsSuccess.Should().BeFalse();

    // 测试太多内容，失败时难以定位问题
}
```

## 测试命名规范

### 方法命名模式

**格式**: `MethodName_Should_ExpectedBehavior_When_StateUnderTest`

```csharp
// ✅ 好的命名
[Fact]
public void Login_Should_ReturnSuccess_When_ValidCredentialsProvided()

[Fact]
public void Login_Should_ReturnFailure_When_UserNotFound()

[Fact]
public void Login_Should_ReturnFailure_When_PasswordIncorrect()

[Fact]
public void Login_Should_ThrowException_When_DatabaseUnavailable()

// 中文命名也可以接受（提升可读性）
[Fact]
public void 登录_应该返回成功_当提供有效凭据时()

[Fact]
public void 登录_应该返回失败_当用户不存在时()
```

### 测试类命名

```csharp
// ✅ 好的命名
public class UserServiceTests           // 测试UserService
public class PrescriptionCalculatorTests  // 测试PrescriptionCalculator
public class AuthControllerTests        // 测试AuthController

// 特殊情况下的命名
public class UserService_CreateUser_Tests     // 只测试CreateUser方法
public class UserService_Integration_Tests    // 集成测试
public class UserService_Performance_Tests    // 性能测试
```

### Theory测试数据命名

```csharp
[Theory]
[InlineData("", false, "空用户名")]
[InlineData("a", false, "用户名太短")]
[InlineData("ab", false, "用户名太短")]
[InlineData("abc", true, "最短有效用户名")]
[InlineData("normal_user", true, "正常用户名")]
[InlineData("very_long_username_that_exceeds_limit", false, "用户名太长")]
public void ValidateUsername_Should_ReturnExpectedResult_When_DifferentInputs(
    string username, bool expected, string scenario)
{
    // scenario参数用于测试失败时的诊断
    var result = _validator.ValidateUsername(username);
    result.Should().Be(expected, because: scenario);
}
```

## 测试结构模式

### AAA模式 (Arrange-Act-Assert)

```csharp
[Fact]
public void CalculatePrescriptionTotal_Should_ReturnCorrectAmount_When_MultipleItems()
{
    // Arrange - 准备测试数据
    var prescription = new Prescription
    {
        Id = Guid.NewGuid(),
        PatientId = Guid.NewGuid()
    };

    prescription.Items.Add(new PrescriptionItem
    {
        HerbId = Guid.NewGuid(),
        Quantity = 10,
        UnitPrice = 5.50m
    });

    prescription.Items.Add(new PrescriptionItem
    {
        HerbId = Guid.NewGuid(),
        Quantity = 5,
        UnitPrice = 12.00m
    });

    // Act - 执行被测试的操作
    var total = prescription.CalculateTotal();

    // Assert - 验证结果
    total.Should().Be(115.00m); // (10 * 5.50) + (5 * 12.00)
}
```

### 测试类构造模式

```csharp
public class UserServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly UserService _userService;
    private readonly Fixture _fixture; // AutoFixture用于生成测试数据

    public UserServiceTests()
    {
        // 初始化通用依赖
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockEmailService = new Mock<IEmailService>();

        // 初始化被测试对象
        _userService = new UserService(
            _mockUserRepository.Object,
            _mockLogger.Object,
            _mockEmailService.Object);

        // 初始化测试数据生成器
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    public void Dispose()
    {
        // 清理资源
        _userService?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_RepositoryIsNull()
    {
        // Act & Assert
        var action = () => new UserService(null, _mockLogger.Object, _mockEmailService.Object);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("userRepository");
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public void CreateUser_Should_ReturnSuccess_When_ValidDataProvided()
    {
        // 使用测试分组来组织相关测试
    }

    #endregion
}
```

### Builder模式创建测试数据

```csharp
public class UserTestDataBuilder
{
    private User _user;

    public UserTestDataBuilder()
    {
        _user = new User
        {
            Id = Guid.NewGuid(),
            Username = "default_user",
            Name = "Default Name",
            Email = "default@example.com",
            Role = UserRole.Doctor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public UserTestDataBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }

    public UserTestDataBuilder WithRole(UserRole role)
    {
        _user.Role = role;
        return this;
    }

    public UserTestDataBuilder AsInactive()
    {
        _user.IsActive = false;
        return this;
    }

    public UserTestDataBuilder WithId(Guid id)
    {
        _user.Id = id;
        return this;
    }

    public User Build() => _user;
}

// 使用示例
[Fact]
public void ValidateUser_Should_ReturnFalse_When_UserIsInactive()
{
    // Arrange
    var user = new UserTestDataBuilder()
        .WithUsername("testuser")
        .WithRole(UserRole.Doctor)
        .AsInactive()
        .Build();

    // Act
    var isValid = _validator.ValidateUser(user);

    // Assert
    isValid.Should().BeFalse();
}
```

## Mock使用规范

### Setup最佳实践

```csharp
// ✅ 好的Mock设置
[Fact]
public void GetUser_Should_ReturnUser_When_UserExists()
{
    // Arrange
    var userId = Guid.NewGuid();
    var expectedUser = new User { Id = userId, Username = "testuser" };

    // 具体的Setup，明确参数
    _mockUserRepository
        .Setup(x => x.GetByIdAsync(userId))
        .ReturnsAsync(expectedUser);

    // Act
    var result = await _userService.GetUserAsync(userId);

    // Assert
    result.Should().NotBeNull();
    result.Username.Should().Be("testuser");
}

// ❌ 避免 - 过于宽泛的Setup
[Fact]
public void GetUser_Bad_Setup()
{
    // 使用It.IsAny<>可能隐藏bug
    _mockUserRepository
        .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(new User());
}
```

### Verify最佳实践

```csharp
[Fact]
public void CreateUser_Should_CallRepositoryOnce_When_ValidDataProvided()
{
    // Arrange
    var userDto = new UserCreateDto { Username = "newuser" };

    _mockUserRepository
        .Setup(x => x.CreateAsync(It.IsAny<User>()))
        .ReturnsAsync(new User());

    // Act
    await _userService.CreateUserAsync(userDto);

    // Assert - 验证具体的调用
    _mockUserRepository.Verify(
        x => x.CreateAsync(It.Is<User>(u => u.Username == "newuser")),
        Times.Once);

    // 验证其他方法未被调用
    _mockUserRepository.Verify(
        x => x.UpdateAsync(It.IsAny<User>()),
        Times.Never);
}
```

### Callback使用

```csharp
[Fact]
public void CreateUser_Should_SetCreatedDate_When_Saving()
{
    // Arrange
    var userDto = new UserCreateDto { Username = "newuser" };
    User capturedUser = null;

    _mockUserRepository
        .Setup(x => x.CreateAsync(It.IsAny<User>()))
        .Callback<User>(user => capturedUser = user)
        .ReturnsAsync((User user) => user);

    // Act
    await _userService.CreateUserAsync(userDto);

    // Assert
    capturedUser.Should().NotBeNull();
    capturedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

### 复杂场景Mock

```csharp
[Fact]
public void ProcessPayment_Should_RetryThreeTimes_When_ServiceUnavailable()
{
    // Arrange
    var payment = new Payment { Amount = 100m };
    var callCount = 0;

    _mockPaymentService
        .Setup(x => x.ProcessAsync(It.IsAny<Payment>()))
        .Returns(() =>
        {
            callCount++;
            if (callCount < 3)
                throw new ServiceUnavailableException("Service down");
            return Task.FromResult(new PaymentResult { Success = true });
        });

    // Act
    var result = await _paymentProcessor.ProcessPaymentAsync(payment);

    // Assert
    result.Success.Should().BeTrue();
    callCount.Should().Be(3); // 验证重试次数
}
```

## 数据驱动测试

### Theory与InlineData

```csharp
[Theory]
[InlineData("admin", "Admin123!", UserRole.Admin, true)]
[InlineData("doctor", "Doctor123!", UserRole.Doctor, true)]
[InlineData("", "password", UserRole.Doctor, false)]        // 空用户名
[InlineData("user", "", UserRole.Doctor, false)]           // 空密码
[InlineData("user", "weak", UserRole.Doctor, false)]       // 弱密码
public void ValidateUserCreation_Should_ReturnExpectedResult(
    string username,
    string password,
    UserRole role,
    bool expectedIsValid)
{
    // Arrange
    var userDto = new UserCreateDto
    {
        Username = username,
        Password = password,
        Role = role
    };

    // Act
    var result = _validator.ValidateUserCreation(userDto);

    // Assert
    result.IsValid.Should().Be(expectedIsValid);
}
```

### MemberData使用

```csharp
public class PasswordValidationTests
{
    public static IEnumerable<object[]> PasswordTestCases =>
        new List<object[]>
        {
            new object[] { "Password123!", true, "强密码" },
            new object[] { "password123!", false, "缺少大写字母" },
            new object[] { "PASSWORD123!", false, "缺少小写字母" },
            new object[] { "Password!", false, "缺少数字" },
            new object[] { "Password123", false, "缺少特殊字符" },
            new object[] { "Pass1!", false, "长度不足" },
        };

    [Theory]
    [MemberData(nameof(PasswordTestCases))]
    public void ValidatePassword_Should_ReturnExpectedResult(
        string password,
        bool expected,
        string description)
    {
        // Act
        var result = PasswordValidator.Validate(password);

        // Assert
        result.Should().Be(expected, because: description);
    }
}
```

### ClassData使用

```csharp
public class UserTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            new User { Username = "admin", Role = UserRole.Admin },
            true
        };

        yield return new object[]
        {
            new User { Username = "doctor", Role = UserRole.Doctor },
            true
        };

        yield return new object[]
        {
            new User { Username = "inactive", IsActive = false },
            false
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(UserTestData))]
public void ValidateUser_Should_ReturnExpectedResult(User user, bool expected)
{
    // Act
    var result = _validator.ValidateUser(user);

    // Assert
    result.Should().Be(expected);
}
```

## 异常处理测试

### 验证异常类型和消息

```csharp
[Fact]
public void CreateUser_Should_ThrowArgumentNullException_When_UserDtoIsNull()
{
    // Act & Assert
    var action = () => _userService.CreateUser(null);

    action.Should().Throw<ArgumentNullException>()
          .WithParameterName("userDto")
          .WithMessage("*userDto*"); // 部分消息匹配
}

[Fact]
public void ProcessPayment_Should_ThrowBusinessException_When_InsufficientFunds()
{
    // Arrange
    var payment = new Payment { Amount = 1000m };
    var account = new Account { Balance = 100m };

    // Act & Assert
    var action = () => _paymentService.ProcessPayment(payment, account);

    action.Should().Throw<BusinessException>()
          .WithMessage("余额不足")
          .And.ErrorCode.Should().Be("INSUFFICIENT_FUNDS");
}
```

### 异步异常测试

```csharp
[Fact]
public async Task GetUserAsync_Should_ThrowNotFoundException_When_UserNotExists()
{
    // Arrange
    var userId = Guid.NewGuid();

    _mockUserRepository
        .Setup(x => x.GetByIdAsync(userId))
        .ReturnsAsync((User)null);

    // Act & Assert
    var action = () => _userService.GetUserAsync(userId);

    await action.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"用户未找到: {userId}");
}
```

### 异常链测试

```csharp
[Fact]
public void SaveUser_Should_WrapDatabaseException_When_DatabaseError()
{
    // Arrange
    var user = new User { Username = "testuser" };
    var innerException = new SqlException("Connection timeout");

    _mockUserRepository
        .Setup(x => x.SaveAsync(user))
        .ThrowsAsync(innerException);

    // Act & Assert
    var action = () => _userService.SaveUserAsync(user);

    action.Should().ThrowAsync<DataAccessException>()
          .WithMessage("保存用户失败")
          .WithInnerException<SqlException>()
          .WithInnerException<SqlException>(ex => ex.Message == "Connection timeout");
}
```

## 性能测试考虑

### 简单性能验证

```csharp
[Fact]
public void SearchUsers_Should_CompleteWithinTimeLimit_When_LargeDataset()
{
    // Arrange
    var users = GenerateLargeUserDataset(10000);
    _mockUserRepository
        .Setup(x => x.SearchAsync(It.IsAny<string>()))
        .ReturnsAsync(users);

    var stopwatch = Stopwatch.StartNew();

    // Act
    var result = await _userService.SearchUsersAsync("doctor");

    stopwatch.Stop();

    // Assert
    result.Should().NotBeNull();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // 1秒内完成
}
```

### 内存使用测试

```csharp
[Fact]
public void ProcessLargeFile_Should_NotExceedMemoryLimit()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(true);
    var largeFile = GenerateLargeFileContent(10_000_000); // 10MB

    // Act
    var result = _fileProcessor.ProcessFile(largeFile);

    // Force garbage collection
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var finalMemory = GC.GetTotalMemory(true);
    var memoryIncrease = finalMemory - initialMemory;

    // Assert
    result.Should().NotBeNull();
    memoryIncrease.Should().BeLessThan(50_000_000); // 不超过50MB增长
}
```

## 代码覆盖率策略

### 覆盖率提升技巧

```csharp
// 覆盖所有分支
public class UserValidator
{
    public ValidationResult ValidateUser(User user)
    {
        if (user == null)
            return ValidationResult.Failure("用户不能为空");

        if (string.IsNullOrEmpty(user.Username))
            return ValidationResult.Failure("用户名不能为空");

        if (user.Username.Length < 3)
            return ValidationResult.Failure("用户名太短");

        return ValidationResult.Success();
    }
}

// 对应的测试覆盖所有分支
public class UserValidatorTests
{
    [Fact]
    public void ValidateUser_Should_ReturnFailure_When_UserIsNull()
    {
        var result = _validator.ValidateUser(null);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户不能为空");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateUser_Should_ReturnFailure_When_UsernameIsEmpty(string username)
    {
        var user = new User { Username = username };
        var result = _validator.ValidateUser(user);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名不能为空");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void ValidateUser_Should_ReturnFailure_When_UsernameIsTooShort(string username)
    {
        var user = new User { Username = username };
        var result = _validator.ValidateUser(user);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名太短");
    }

    [Fact]
    public void ValidateUser_Should_ReturnSuccess_When_UserIsValid()
    {
        var user = new User { Username = "validuser" };
        var result = _validator.ValidateUser(user);
        result.IsSuccess.Should().BeTrue();
    }
}
```

### 难以测试代码的处理

```csharp
// ❌ 难以测试的静态依赖
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        order.ProcessedAt = DateTime.Now; // 静态依赖，难以测试
        order.OrderNumber = Guid.NewGuid().ToString(); // 不确定结果

        // 业务逻辑...
    }
}

// ✅ 可测试的设计
public interface IDateTimeProvider
{
    DateTime Now { get; }
}

public interface IGuidProvider
{
    Guid NewGuid();
}

public class OrderService
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGuidProvider _guidProvider;

    public OrderService(IDateTimeProvider dateTimeProvider, IGuidProvider guidProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _guidProvider = guidProvider;
    }

    public void ProcessOrder(Order order)
    {
        order.ProcessedAt = _dateTimeProvider.Now; // 可以Mock
        order.OrderNumber = _guidProvider.NewGuid().ToString(); // 可以控制

        // 业务逻辑...
    }
}

// 对应的测试
[Fact]
public void ProcessOrder_Should_SetProcessedTime_When_OrderProcessed()
{
    // Arrange
    var fixedDateTime = new DateTime(2025, 9, 21, 10, 0, 0);
    var fixedGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");

    _mockDateTimeProvider.Setup(x => x.Now).Returns(fixedDateTime);
    _mockGuidProvider.Setup(x => x.NewGuid()).Returns(fixedGuid);

    var order = new Order();

    // Act
    _orderService.ProcessOrder(order);

    // Assert
    order.ProcessedAt.Should().Be(fixedDateTime);
    order.OrderNumber.Should().Be(fixedGuid.ToString());
}
```

## 测试维护

### 测试重构

```csharp
// ❌ 重复的测试设置
public class UserServiceTests
{
    [Fact]
    public void Test1()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var service = new UserService(context);
        // 测试逻辑...
    }

    [Fact]
    public void Test2()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var service = new UserService(context);
        // 测试逻辑...
    }
}

// ✅ 提取公共设置
public class UserServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private UserService CreateService(AppDbContext context = null)
    {
        context ??= CreateContext();
        return new UserService(context);
    }

    [Fact]
    public void Test1()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        // 测试逻辑...
    }
}
```

### 测试数据管理

```csharp
// 测试数据工厂
public static class TestDataFactory
{
    public static User CreateValidUser(string username = "testuser")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Name = "Test User",
            Email = $"{username}@test.com",
            Role = UserRole.Doctor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Prescription CreateValidPrescription(Guid? patientId = null)
    {
        return new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = patientId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Items = new List<PrescriptionItem>
            {
                new PrescriptionItem
                {
                    HerbId = Guid.NewGuid(),
                    Quantity = 10,
                    UnitPrice = 5.00m
                }
            }
        };
    }
}
```

## 常见反模式

### 1. 测试之间有依赖关系

```csharp
// ❌ 避免
public class BadTestExample
{
    private static int _counter = 0;

    [Fact]
    public void Test_A()
    {
        _counter++; // 影响其他测试
        Assert.Equal(1, _counter);
    }

    [Fact]
    public void Test_B()
    {
        Assert.Equal(2, _counter); // 依赖Test_A的执行
    }
}

// ✅ 正确做法
public class GoodTestExample
{
    [Fact]
    public void Test_A()
    {
        var counter = 0;
        counter++;
        Assert.Equal(1, counter);
    }

    [Fact]
    public void Test_B()
    {
        var counter = 1;
        counter++;
        Assert.Equal(2, counter);
    }
}
```

### 2. 过度使用Mock

```csharp
// ❌ 避免 - Mock过多，测试价值低
[Fact]
public void CalculateTotal_OverMocked()
{
    var mockItem1 = new Mock<IPrescriptionItem>();
    mockItem1.Setup(x => x.GetAmount()).Returns(50m);

    var mockItem2 = new Mock<IPrescriptionItem>();
    mockItem2.Setup(x => x.GetAmount()).Returns(30m);

    var mockItems = new Mock<IList<IPrescriptionItem>>();
    mockItems.Setup(x => x.Count).Returns(2);
    mockItems.Setup(x => x[0]).Returns(mockItem1.Object);
    mockItems.Setup(x => x[1]).Returns(mockItem2.Object);

    // 过度Mock，应该使用真实对象
}

// ✅ 正确做法 - 只Mock外部依赖
[Fact]
public void CalculateTotal_Appropriate()
{
    var prescription = new Prescription();
    prescription.Items.Add(new PrescriptionItem { Quantity = 10, UnitPrice = 5.00m });
    prescription.Items.Add(new PrescriptionItem { Quantity = 6, UnitPrice = 5.00m });

    var total = prescription.CalculateTotal();

    total.Should().Be(80m);
}
```

### 3. 测试实现细节

```csharp
// ❌ 避免 - 测试内部实现
[Fact]
public void ProcessPayment_Bad_Test()
{
    // 测试方法内部调用了哪些私有方法
    var service = new PaymentService();

    // 使用反射访问私有字段/方法进行测试
    var privateField = typeof(PaymentService)
        .GetField("_internalState", BindingFlags.NonPublic | BindingFlags.Instance);

    // 这种测试太脆弱，实现改变就会失败
}

// ✅ 正确做法 - 测试公共行为
[Fact]
public void ProcessPayment_Should_ReturnSuccess_When_ValidPayment()
{
    var payment = new Payment { Amount = 100m };

    var result = _paymentService.ProcessPayment(payment);

    result.IsSuccess.Should().BeTrue();
    result.TransactionId.Should().NotBeNullOrEmpty();
}
```

### 4. 睡眠等待

```csharp
// ❌ 避免 - 使用Thread.Sleep
[Fact]
public void AsyncOperation_Bad_Test()
{
    _service.StartAsyncOperation();

    Thread.Sleep(5000); // 不确定的等待时间

    var result = _service.GetResult();
    result.Should().NotBeNull();
}

// ✅ 正确做法 - 使用proper async/await
[Fact]
public async Task AsyncOperation_Should_CompleteSuccessfully()
{
    var result = await _service.ProcessAsync();

    result.Should().NotBeNull();
}

// 或者使用超时控制
[Fact]
public async Task AsyncOperation_Should_CompleteWithinTimeout()
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    var result = await _service.ProcessAsync(cts.Token);

    result.Should().NotBeNull();
}
```

### 5. 魔法数字和字符串

```csharp
// ❌ 避免
[Fact]
public void Test_With_Magic_Values()
{
    var user = new User { Age = 25 }; // 25是什么含义？

    var result = _service.ValidateAge(user);

    result.Should().Be("VALID"); // "VALID"的含义？
}

// ✅ 正确做法
[Fact]
public void ValidateAge_Should_ReturnValid_When_AgeIsAboveMinimum()
{
    // 使用常量或配置值
    const int validAge = 18;
    const string expectedResult = ValidationResult.Valid;

    var user = new User { Age = validAge + 7 }; // 明确表示大于最小年龄

    var result = _service.ValidateAge(user);

    result.Should().Be(expectedResult);
}
```

## 总结

### 关键原则回顾

1. **FIRST原则**: Fast, Independent, Repeatable, Self-Validating, Timely
2. **单一职责**: 每个测试只验证一个行为
3. **AAA模式**: 清晰的Arrange-Act-Assert结构
4. **描述性命名**: 测试名称要说明测试什么和期望什么
5. **Mock外部依赖**: 只Mock外部依赖，不过度Mock
6. **避免测试实现细节**: 测试公共行为，不测试内部实现

### 检查清单

在编写测试时，使用此检查清单：

- [ ] 测试名称清楚描述了测试场景
- [ ] 测试独立，不依赖其他测试
- [ ] 使用AAA模式组织测试
- [ ] Mock设置具体明确
- [ ] 断言清晰且有意义
- [ ] 测试运行快速（<100ms）
- [ ] 覆盖了正常和异常路径
- [ ] 没有魔法数字和字符串
- [ ] 测试可重复运行

---

**持续改进**: 随着项目发展，这些最佳实践会不断完善。团队成员发现新的模式或遇到问题时，请及时更新本指南。