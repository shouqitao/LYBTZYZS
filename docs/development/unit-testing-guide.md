# 单元测试指南

**版本**: 1.0
**创建时间**: 2025-09-21
**维护团队**: 开发团队

## 概述

本指南提供LYBT项目单元测试的标准化方法、最佳实践和覆盖率要求。

## 目录

- [测试框架和工具](#测试框架和工具)
- [项目结构](#项目结构)
- [编写测试的基本原则](#编写测试的基本原则)
- [覆盖率要求](#覆盖率要求)
- [测试模式和最佳实践](#测试模式和最佳实践)
- [Mock和依赖注入](#mock和依赖注入)
- [数据库测试](#数据库测试)
- [CI/CD集成](#cicd集成)
- [故障排除](#故障排除)

## 测试框架和工具

### 核心框架
- **xUnit**: 主测试框架
- **FluentAssertions**: 断言库，提供更好的可读性
- **Moq**: Mock框架
- **Microsoft.EntityFrameworkCore.InMemory**: 内存数据库测试
- **Coverlet**: 代码覆盖率收集
- **ReportGenerator**: 覆盖率报告生成

### 工具安装
```bash
# 全局安装覆盖率工具
dotnet tool install -g dotnet-reportgenerator-globaltool

# 项目级别安装
dotnet add package coverlet.collector
dotnet add package Microsoft.NET.Test.Sdk
```

## 项目结构

```
tests/
├── UnitTests/
│   ├── Core/
│   │   ├── LYBT.Infrastructure.Tests/
│   │   └── LYBT.Entities.Tests/
│   ├── Modules/
│   │   ├── LYBT.Module.Auth.Tests/
│   │   ├── LYBT.Module.Users.Tests/
│   │   ├── LYBT.Module.MedicalCase.Tests/
│   │   └── LYBT.Module.Prescriptions.Tests/
│   └── Shared/
│       └── LYBT.Shared.Tests/
├── IntegrationTests/
└── ArchitectureTests/
```

## 编写测试的基本原则

### 1. AAA模式 (Arrange-Act-Assert)

```csharp
[Fact]
public void CreateUser_Should_ReturnSuccess_When_ValidDataProvided()
{
    // Arrange
    var userDto = new UserCreateDto
    {
        Username = "testuser",
        Password = "TestPass123!",
        Name = "Test User",
        Role = UserRole.Doctor
    };

    // Act
    var result = _userService.CreateUser(userDto);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.Data.Username.Should().Be("testuser");
}
```

### 2. 测试命名约定

```csharp
// 格式: MethodName_Should_ExpectedBehavior_When_StateUnderTest
[Fact]
public void Login_Should_ReturnFailure_When_InvalidCredentials()

[Theory]
[InlineData("", "password")]  // 空用户名
[InlineData("user", "")]      // 空密码
[InlineData(null, "password")] // null用户名
public void Login_Should_ReturnValidationError_When_RequiredFieldsMissing(string username, string password)
```

### 3. 测试类结构

```csharp
public class UserServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;
    private readonly AppDbContext _context;

    public UserServiceTests()
    {
        // 构造函数中初始化测试依赖
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Constructor Tests
    // 构造函数测试
    #endregion

    #region CreateUser Tests
    // CreateUser方法测试
    #endregion

    #region UpdateUser Tests
    // UpdateUser方法测试
    #endregion
}
```

## 覆盖率要求

### 整体要求
- **行覆盖率**: ≥ 90%
- **分支覆盖率**: ≥ 80%
- **方法覆盖率**: ≥ 85%

### 关键模块要求 (≥ 95%)
- **Auth模块**: 认证和授权相关功能
- **Users模块**: 用户管理功能
- **MedicalCase模块**: 病历管理功能
- **Prescriptions模块**: 处方管理功能

### 覆盖率收集和报告

```bash
# 运行测试并收集覆盖率
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# 生成HTML报告
reportgenerator \
  "-reports:TestResults/**/coverage.opencover.xml" \
  "-targetdir:TestResults/CoverageReport" \
  "-reporttypes:Html;Cobertura;JsonSummary"

# 打开报告
start TestResults/CoverageReport/index.html
```

## 测试模式和最佳实践

### 1. 构造函数测试

```csharp
[Fact]
public void Constructor_Should_ThrowArgumentNullException_When_RepositoryIsNull()
{
    // Act & Assert
    var action = () => new UserService(null, _mockLogger.Object);
    action.Should().Throw<ArgumentNullException>();
}
```

### 2. 参数验证测试

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void CreateUser_Should_ReturnValidationError_When_UsernameIsInvalid(string username)
{
    // Arrange
    var userDto = new UserCreateDto { Username = username };

    // Act
    var result = _userService.CreateUser(userDto);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ErrorMessage.Should().Contain("用户名");
}
```

### 3. 异常处理测试

```csharp
[Fact]
public void GetUser_Should_ReturnError_When_DatabaseThrowsException()
{
    // Arrange
    _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                      .ThrowsAsync(new InvalidOperationException("Database error"));

    // Act
    var result = await _userService.GetUserAsync(Guid.NewGuid());

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ErrorMessage.Should().Contain("数据库");
}
```

### 4. 业务逻辑测试

```csharp
[Fact]
public void CalculatePrescriptionTotal_Should_ReturnCorrectAmount_When_MultipleItems()
{
    // Arrange
    var prescription = new Prescription();
    prescription.Items.Add(new PrescriptionItem { HerbId = Guid.NewGuid(), Quantity = 10, UnitPrice = 5.50m });
    prescription.Items.Add(new PrescriptionItem { HerbId = Guid.NewGuid(), Quantity = 5, UnitPrice = 12.00m });

    // Act
    var total = prescription.CalculateTotal();

    // Assert
    total.Should().Be(115.00m); // (10 * 5.50) + (5 * 12.00) = 55 + 60 = 115
}
```

## Mock和依赖注入

### 1. Repository Mock

```csharp
private void SetupUserRepositoryMock()
{
    var users = new List<User>
    {
        new User { Id = Guid.NewGuid(), Username = "admin", Name = "管理员" },
        new User { Id = Guid.NewGuid(), Username = "doctor", Name = "医生" }
    };

    _mockUserRepository.Setup(x => x.GetAllAsync())
                      .ReturnsAsync(users);

    _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                      .ReturnsAsync((Guid id) => users.FirstOrDefault(u => u.Id == id));
}
```

### 2. Logger Mock验证

```csharp
[Fact]
public void CreateUser_Should_LogInformation_When_UserCreatedSuccessfully()
{
    // Arrange & Act
    var result = _userService.CreateUser(validUserDto);

    // Assert
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("用户创建成功")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

## 数据库测试

### 1. 内存数据库设置

```csharp
private AppDbContext CreateInMemoryContext()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    var context = new AppDbContext(options);
    context.Database.EnsureCreated();
    return context;
}
```

### 2. 数据初始化

```csharp
private void SeedTestData(AppDbContext context)
{
    var users = new[]
    {
        new User { Id = Guid.NewGuid(), Username = "admin", Name = "管理员", Role = UserRole.Admin },
        new User { Id = Guid.NewGuid(), Username = "doctor", Name = "医生", Role = UserRole.Doctor }
    };

    context.Users.AddRange(users);
    context.SaveChanges();
}
```

### 3. 事务测试

```csharp
[Fact]
public async Task CreateUserWithProfile_Should_RollbackAll_When_ProfileCreationFails()
{
    // Arrange
    using var context = CreateInMemoryContext();
    var service = new UserService(context);

    // 模拟失败场景
    var invalidUserDto = new UserCreateDto { /* 会导致失败的数据 */ };

    // Act
    var result = await service.CreateUserWithProfileAsync(invalidUserDto);

    // Assert
    result.IsSuccess.Should().BeFalse();
    context.Users.Should().BeEmpty(); // 验证回滚
}
```

## CI/CD集成

### 1. GitHub Actions工作流

测试和覆盖率检查已集成到CI/CD管道中:

- **触发条件**: Push到主分支、PR到主分支
- **覆盖率阈值**: 行覆盖率90%、分支覆盖率80%
- **关键模块**: 95%覆盖率要求
- **门禁机制**: 未达标时构建失败

### 2. 本地验证

```bash
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test tests/UnitTests/Modules/LYBT.Module.Users.Tests/

# 运行带覆盖率的测试
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## 故障排除

### 1. 常见问题

**Q: 内存数据库测试时出现"表不存在"错误**
```csharp
// A: 确保调用EnsureCreated()
context.Database.EnsureCreated();
```

**Q: Mock验证失败**
```csharp
// A: 检查参数匹配
_mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))  // 使用It.IsAny<>
              .ReturnsAsync(expectedUser);
```

**Q: 覆盖率报告不生成**
```bash
# A: 检查Coverlet配置
dotnet add package coverlet.collector
dotnet add package coverlet.msbuild
```

### 2. 性能优化

**并行执行**
```xml
<!-- 在测试项目中添加 -->
<PropertyGroup>
  <ParallelizeTestCollections>true</ParallelizeTestCollections>
</PropertyGroup>
```

**内存数据库优化**
```csharp
// 使用唯一数据库名避免冲突
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
    .Options;
```

### 3. 调试技巧

**输出测试信息**
```csharp
[Fact]
public void TestMethod()
{
    // 使用ITestOutputHelper
    _output.WriteLine($"测试数据: {JsonSerializer.Serialize(testData)}");

    // 执行测试
    var result = _service.Method();

    _output.WriteLine($"结果: {result}");
}
```

## 附录

### A. 测试数据构建器模式

```csharp
public class UserTestDataBuilder
{
    private User _user = new User();

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

    public User Build() => _user;
}

// 使用
var testUser = new UserTestDataBuilder()
    .WithUsername("testuser")
    .WithRole(UserRole.Doctor)
    .Build();
```

### B. 自定义断言扩展

```csharp
public static class CustomAssertions
{
    public static void ShouldBeValidServiceResult<T>(this ServiceResult<T> result)
    {
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    public static void ShouldBeFailedServiceResult<T>(this ServiceResult<T> result, string expectedError = null)
    {
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        if (!string.IsNullOrEmpty(expectedError))
        {
            result.ErrorMessage.Should().Contain(expectedError);
        }
    }
}
```

### C. 测试配置文件示例

```json
// appsettings.Test.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=:memory:"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Security": {
    "Https": {
      "RequireHttps": false
    },
    "PasswordPolicy": {
      "MinLength": 6
    }
  }
}
```

---

**维护说明**: 本文档随项目发展定期更新，如有疑问请联系开发团队。