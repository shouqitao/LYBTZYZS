# 测试架构标准化指南

## 概述

本文档定义了LYBT项目的测试架构标准化规范，确保所有测试代码具有一致的结构、命名和质量。

## 测试架构层次

### 1. 测试类型分层

```
tests/
├── Architecture/                 # 架构测试
│   ├── LYBT.ArchTests.csproj
│   └── Server/
│       └── LYBT.Server.ArchTests.csproj
├── UnitTests/                    # 单元测试
│   ├── Client/Desktop/           # Desktop端单元测试
│   │   └── LYBT.Desktop.{Module}.Tests/
│   ├── Server/                   # Server端单元测试
│   │   ├── Core/                 # 核心层测试
│   │   └── Modules/              # 模块层测试
│   └── Shared/                   # 共享层测试
├── IntegrationTests/             # 集成测试
│   ├── Controllers/              # API控制器测试
│   └── WebAPI.IntegrationTests/  # WebAPI集成测试
├── SecurityTests/                # 安全测试
├── TestConfiguration/            # 测试配置和基础设施
│   ├── AssertionHelpers/         # 断言辅助类
│   ├── TestDataBuilders/        # 测试数据构建器
│   ├── IntegrationTestBase.cs    # 集成测试基类
│   └── UnitTestBase.cs          # 单元测试基类
└── PerformanceTests/             # 性能测试
```

### 2. 测试项目命名规范

#### 单元测试项目
- **模式**: `LYBT.{Layer}.{Module}.Tests`
- **示例**:
  - `LYBT.Desktop.Auth.Tests`
  - `LYBT.Module.Users.Tests`
  - `LYBT.Infrastructure.Tests`

#### 集成测试项目
- **模式**: `LYBT.{Type}.IntegrationTests`
- **示例**:
  - `LYBT.WebAPI.IntegrationTests`
  - `LYBT.Repository.IntegrationTests`

#### 架构测试项目
- **模式**: `LYBT.{Scope}ArchTests`
- **示例**:
  - `LYBT.ArchTests`
  - `LYBT.Server.ArchTests`

## 测试类命名规范

### 1. 测试类命名

#### 单元测试类
```csharp
// 模式：{ClassName}Tests
public class LoginViewModelTests
public class UserRepositoryTests
public class UserServiceTests
public class PatientServiceTests
```

#### 集成测试类
```csharp
// 模式：{ClassName}IntegrationTests
public class UserControllerIntegrationTests
public class MedicalCaseControllerIntegrationTests
public class OrderServiceIntegrationTests
```

#### API测试类
```csharp
// 模式：{ControllerName}ControllerTests
public class UsersControllerTests
public class PatientsControllerTests
public class PrescriptionsControllerTests
```

### 2. 测试方法命名

采用`{Scenario}_{ExpectedResult}`模式，遵循AAA模式：

```csharp
[Fact]
public async Task Create_ValidUser_ShouldReturnCreatedUser()
{
    // Arrange
    var userDto = new UserCreateDto
    {
        UserName = "testuser",
        RealName = "测试用户"
    };

    // Act
    var result = await _userService.CreateAsync(userDto);

    // Assert
    result.Should().NotBeNull();
    result.UserName.Should().Be("testuser");
    result.RealName.Should().Be("测试用户");
}

[Fact]
public void Create_DuplicateUserName_ShouldThrowValidationException()
{
    // Arrange
    var userDto = new UserCreateDto { UserName = "existinguser" };

    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(
        () => _userService.CreateAsync(userDto));
}
```

## 测试结构规范

### 1. AAA模式

所有测试方法必须遵循Arrange-Act-Assert模式：

```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange - 设置测试数据和依赖
    var service = new UserService(_mockRepository.Object);
    var input = new CreateUserDto { Name = "test" };

    // Act - 执行被测试的方法
    var result = service.Create(input);

    // Assert - 验证结果
    result.Should().NotBeNull();
    result.Name.Should().Be("test");
    _mockRepository.Verify(x => x.Add(It.IsAny<User>()), Times.Once);
}
```

### 2. 测试类组织

```csharp
public class UserServiceTests
{
    private readonly UserService _userService;
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserTestDataBuilder _userBuilder;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockRepository.Object);
        _userBuilder = new UserTestDataBuilder();
    }

    #region Create Tests

    [Fact]
    public void Create_ValidUser_ShouldSucceed() { }

    [Fact]
    public void Create_NullUser_ShouldThrowException() { }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ValidUser_ShouldSucceed() { }

    #endregion

    #region Delete Tests

    [Fact]
    public void Delete_ExistingUser_ShouldSucceed() { }

    #endregion
}
```

## 测试数据管理

### 1. 测试数据构建器

使用Builder模式创建测试数据：

```csharp
public class UserTestDataBuilder : BaseTestDataBuilder<UserDto>
{
    public UserTestDataBuilder WithId(Guid id)
    {
        _entity.Id = id;
        return this;
    }

    public UserTestDataBuilder AsDoctor()
    {
        _entity.Role = UserRole.Doctor;
        return this;
    }

    public UserTestDataBuilder Inactive()
    {
        _entity.Status = CommonStatus.Disabled;
        return this;
    }
}

// 使用示例
var user = new UserTestDataBuilder()
    .AsDoctor()
    .WithId(Guid.NewGuid())
    .Build();
```

### 2. 测试数据分类

```csharp
public static class TestUsers
{
    public static readonly UserDto ValidDoctor = new UserTestDataBuilder()
        .AsDoctor()
        .WithUserName("doctor.test")
        .Build();

    public static readonly UserDto ValidPatient = new UserTestDataBuilder()
        .AsPatient()
        .WithUserName("patient.test")
        .Build();

    public static readonly UserDto InactiveUser = new UserTestDataBuilder()
        .Inactive()
        .Build();
}
```

## Mock管理规范

### 1. Mock对象设置

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
    }

    private void SetupMockRepository()
    {
        _mockRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new User { Id = id });

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);
    }
}
```

### 2. Mock验证

```csharp
[Fact]
public async Task Create_ValidUser_ShouldCallRepositoryAdd()
{
    // Arrange
    var userDto = new UserCreateDto { UserName = "test" };

    // Act
    await _userService.CreateAsync(userDto);

    // Assert
    _mockRepository.Verify(
        x => x.AddAsync(It.Is<User>(u => u.UserName == "test")),
        Times.Once);

    _mockLogger.Verify(
        x => x.LogInformation("User created: {UserId}", It.IsAny<Guid>()),
        Times.Once);
}
```

## 集成测试规范

### 1. 集成测试基类

继承统一的集成测试基类：

```csharp
public class UserControllerTests : IntegrationTestBase
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        // 配置额外的测试服务
        services.AddScoped<IUserRepository, TestUserRepository>();
    }

    protected override void SeedTestData(AppDbContext context)
    {
        base.SeedTestData(context);

        // 添加模块特定的测试数据
        context.Users.AddRange(TestUsers.ValidDoctor, TestUsers.ValidPatient);
        context.SaveChanges();
    }
}
```

### 2. API测试模式

```csharp
public class UsersControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetUsers_ShouldReturnPagedResults()
    {
        // Arrange
        var response = await Client.GetAsync("/api/users?page=1&pageSize=10");

        // Act
        response.Should().BeOk();

        var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<UserDto>>();

        // Assert
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data.Items.Should().HaveCountGreaterThan(0);
    }
}
```

## 断言规范

### 1. 使用FluentAssertions

```csharp
[Fact]
public void User_ShouldHaveValidProperties()
{
    // Arrange
    var user = _userBuilder.Build();

    // Act & Assert
    user.Should().NotBeNull();
    user.Id.Should().NotBeEmpty();
    user.UserName.Should().NotBeNullOrEmpty();
    user.Role.Should().Be(UserRole.Doctor);
    user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
}
```

### 2. 自定义断言扩展

```csharp
public static class UserAssertionExtensions
{
    public static void ShouldBeValidUser(this UserDto user)
    {
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.UserName.Should().NotBeNullOrEmpty();
        user.RealName.Should().NotBeNullOrEmpty();
        user.Role.Should().BeDefined();
    }

    public static void ShouldBeActive(this UserDto user)
    {
        user.Status.Should().Be(CommonStatus.Enabled);
        user.IsActive.Should().BeTrue();
    }
}
```

## 异常测试规范

### 1. 异常验证

```csharp
[Fact]
public async Task Create_NullUser_ShouldThrowArgumentNullException()
{
    // Arrange
    UserCreateDto nullUser = null!;

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        () => _userService.CreateAsync(nullUser));

    exception.ParamName.Should().Be("userDto");
}

[Fact]
public async Task Create_InvalidData_ShouldThrowValidationException()
{
    // Arrange
    var invalidUser = new UserCreateDto { UserName = "" };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => _userService.CreateAsync(invalidUser));

    exception.Message.Should().Contain("用户名不能为空");
}
```

## 异步测试规范

### 1. 异步方法测试

```csharp
[Fact]
public async Task GetByIdAsync_ExistingId_ShouldReturnUser()
{
    // Arrange
    var userId = Guid.NewGuid();
    var expectedUser = _userBuilder.WithId(userId).Build();
    _mockRepository.Setup(x => x.GetByIdAsync(userId))
                  .ReturnsAsync(expectedUser);

    // Act
    var result = await _userService.GetByIdAsync(userId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(userId);
}

[Fact]
public async Task GetByIdAsync_NotFound_ShouldReturnNull()
{
    // Arrange
    var nonExistentId = Guid.NewGuid();
    _mockRepository.Setup(x => x.GetByIdAsync(nonExistentId))
                  .ReturnsAsync((UserDto?)null);

    // Act
    var result = await _userService.GetByIdAsync(nonExistentId);

    // Assert
    result.Should().BeNull();
}
```

## 测试覆盖标准

### 1. 代码覆盖率要求

- **核心业务逻辑**: ≥90%
- **服务层**: ≥85%
- **控制器层**: ≥80%
- **工具类**: ≥95%
- **整体覆盖率**: ≥80%

### 2. 测试场景覆盖

每个公开方法至少需要测试：
- **正常路径**: 正常输入和预期输出
- **边界条件**: 最小值、最大值、空值
- **异常情况**: 无效输入、依赖失败
- **并发场景**: 并发访问（如适用）

## 性能测试规范

### 1. 性能基准测试

```csharp
[Fact]
public async Task GetById_ShouldCompleteWithin100Ms()
{
    // Arrange
    var userId = Guid.NewGuid();
    var stopwatch = Stopwatch.StartNew();

    // Act
    var result = await _userService.GetByIdAsync(userId);
    stopwatch.Stop();

    // Assert
    result.Should().BeNull(); // 不存在的用户
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
}
```

### 2. 内存使用测试

```csharp
[Fact]
public void CreateLargeUserList_ShouldNotExceedMemoryLimit()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(true);

    // Act
    var users = Enumerable.Range(0, 1000)
        .Select(i => _userBuilder.WithUserName($"user{i}").Build())
        .ToList();

    // Assert
    var finalMemory = GC.GetTotalMemory(true);
    var memoryUsed = finalMemory - initialMemory;

    memoryUsed.Should().BeLessThan(50 * 1024 * 1024); // 小于50MB
}
```

## 测试环境配置

### 1. 测试配置文件

```json
// tests/TestConfiguration/appsettings.Test.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "LYBT": "Information"
    }
  },
  "ConnectionStrings": {
    "TestDatabase": "Data Source=:memory:"
  },
  "Jwt": {
    "Issuer": "TestIssuer",
    "Audience": "TestAudience",
    "SecretKey": "ThisIsASecretKeyForTestingOnly123456789",
    "ExpirationMinutes": "60"
  }
}
```

### 2. CI/CD集成

```yaml
# .github/workflows/test.yml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore
      run: dotnet restore LYBT.All.sln

    - name: Build
      run: dotnet build LYBT.All.sln --no-restore --configuration Release

    - name: Test
      run: dotnet test LYBT.All.sln --no-build --configuration Release --collect:"XPlat Code Coverage"

    - name: Upload Coverage
      uses: codecov/codecov-action@v3
```

## 最佳实践总结

### DO (推荐做法)
1. ✅ **使用描述性的测试名称**: 清晰说明测试场景和期望结果
2. ✅ **遵循AAA模式**: Arrange-Act-Assert结构清晰
3. ✅ **使用测试数据构建器**: 提高测试数据的可读性和维护性
4. ✅ **模拟依赖**: 使用Mock对象隔离测试单元
5. ✅ **测试异常情况**: 验证错误处理逻辑
6. ✅ **保持测试独立**: 测试之间不应相互依赖
7. ✅ **使用FluentAssertions**: 编写可读性强的断言
8. ✅ **定期重构测试**: 保持测试代码的质量

### DON'T (避免做法)
1. ❌ **测试私有方法**: 只测试公共接口
2. ❌ **硬编码测试数据**: 使用构建器或常量
3. ❌ **忽略测试警告**: 及时修复所有编译警告
4. ❌ **编写复杂的测试逻辑**: 保持测试简单直接
5. ❌ **忽略测试覆盖率**: 确保重要的代码路径都被测试
6. ❌ **在生产代码中添加测试逻辑**: 保持测试代码分离
7. ❌ **使用Thread.Sleep**: 使用异步等待或同步机制
8. ❌ **忽略测试失败**: 及时修复失败的测试

---

*本标准基于LYBT项目的实际架构和最佳实践制定，确保测试代码的质量和一致性。*