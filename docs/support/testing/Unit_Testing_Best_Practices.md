# 凌隐宝堂中医诊所项目单元测试最佳实践

## 概述
**项目**: LYBTZYZS (凌隐宝堂中医诊所管理系统)
**文档目的**: 建立统一的单元测试标准，提升测试质量
**适用范围**: 所有新增和重构的单元测试

## 核心原则

### 1. 测试驱动设计 (TDD) 原则
- **红灯-绿灯-重构**: 先写失败的测试，再实现功能，最后重构
- **测试先行**: 新功能开发必须先编写测试用例
- **回归保护**: 现有功能修改必须保证现有测试通过

### 2. FIRST 原则
- **Fast**: 测试应该快速执行，单元测试应在毫秒级完成
- **Independent**: 测试之间应该独立，不依赖执行顺序
- **Repeatable**: 测试应该可重复，在任何环境下结果一致
- **Self-Validating**: 测试应该有明确的通过/失败结果
- **Timely**: 测试应该及时编写，与代码开发同步

### 3. AAA 模式
每个测试用例应该遵循AAA (Arrange-Act-Assert) 模式：

```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsUser()
{
    // Arrange - 准备测试数据和Mock对象
    var userId = Guid.NewGuid();
    var expectedUser = TestDataFactory.CreateUser(userId);
    _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(expectedUser);

    // Act - 执行被测试的方法
    var result = await _sut.GetByIdAsync(userId);

    // Assert - 验证结果
    result.Should().NotBeNull();
    result.Id.Should().Be(userId);
    result.UserName.Should().Be(expectedUser.UserName);
}
```

## 测试架构和基础设施

### 1. 基础类层次结构

#### BaseServiceTest<TService> (Service层测试基类)
```csharp
public class UserServiceTests : BaseServiceTest<IUserService>
{
    private readonly Mock<IUserRepository> _mockUserRepository;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
    }

    protected override void RegisterTestServices(IServiceCollection services)
    {
        services.AddSingleton(_mockUserRepository.Object);
        services.AddTransient<IUserService, UserService>();
    }
}
```

#### BaseControllerTest<TController> (Controller层测试基类)
```csharp
public class UsersControllerTests : BaseControllerTest<UsersController>
{
    private readonly Mock<IUserService> _mockUserService;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
    }

    protected override void RegisterTestServices(IServiceCollection services)
    {
        services.AddSingleton(_mockUserService.Object);
        services.AddTransient<UsersController>();
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsSuccessResponse()
    {
        // 测试实现
    }
}
```

#### BaseRepositoryTest<TRepository, TDbContext, TEntity> (Repository层测试基类)
```csharp
public class UserRepositoryTests : BaseRepositoryTest<IUserRepository, LYBTDbContext, User>
{
    protected override void RegisterRepositoryServices(IServiceCollection services)
    {
        services.AddTransient<IUserRepository, UserRepository>();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
    {
        // 测试实现
    }
}
```

### 2. 配置测试解决方案

#### InMemoryConfiguration 使用
```csharp
// 正确方式：使用InMemoryConfiguration
protected override IConfiguration CreateInMemoryConfiguration()
{
    var configData = new Dictionary<string, string>
    {
        ["Lybt:Jwt:SecretKey"] = "TestSecretKey",
        ["ConnectionStrings:DefaultConnection"] = "TestConnectionString"
    };
    return new InMemoryConfiguration(configData);
}

// 错误方式：避免Moq ConfigurationBinder.GetValue
// Mock<IConfiguration> 无法处理扩展方法
```

### 3. Mock 对象管理

#### Mock 创建和配置
```csharp
// 推荐：使用基类的CreateMock方法
var mockRepository = CreateMock<IUserRepository>();

// 推荐：链式配置Mock行为
mockRepository
    .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(expectedUser);

// 推荐：使用TestHelper验证Mock调用
TestHelper.VerifyMockCall(mockRepository, r => r.GetByIdAsync(userId), Times.Once);
```

#### Mock 验证最佳实践
```csharp
// 推荐：精确验证
mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);

// 推荐：参数验证
mockRepository.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id != Guid.Empty)), Times.Once);

// 避免过度Mock：不要Mock过于简单的对象
// 避免Mock实现细节：关注行为而非具体实现
```

## 测试数据管理

### 1. TestDataFactory 使用
```csharp
// 推荐：使用TestDataFactory创建标准测试数据
var user = TestDataFactory.CreateUser(Guid.NewGuid(), "testuser", UserRole.Doctor);

// 推荐：批量创建测试数据
var users = TestDataFactory.CreateUsers(10, UserRole.Doctor);

// 推荐：自定义配置
var user = TestDataFactory.CreateUser(null, null, UserRole.Admin, CommonStatus.Enabled);
```

### 2. 测试数据隔离
```csharp
// 推荐：每个测试使用唯一的数据
var userId = Guid.NewGuid();
var user = TestDataFactory.CreateUser(userId);

// 避免：共享静态测试数据
// 避免：测试间依赖共享数据
```

### 3. 数据库测试
```csharp
// 推荐：使用InMemory数据库
public class UserRepositoryTests : BaseRepositoryTest<IUserRepository, LYBTDbContext, User>
{
    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();

        // Act
        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await GetEntityById<Guid>(user.Id);
        savedUser.Should().NotBeNull();
        savedUser.UserName.Should().Be(user.UserName);
    }
}
```

## 测试命名和结构

### 1. 测试类命名
```csharp
// 推荐格式：{ClassName}Tests
public class UserServiceTests { }
public class UsersControllerTests { }
public class UserRepositoryTests { }

// 避免不必要的后缀
// 错误：UserServiceUnitTests, UserServiceIntegrationTests
```

### 2. 测试方法命名
```csharp
// 推荐格式：MethodName_Condition_ExpectedResult
[Fact]
public void GetByIdAsync_WithValidId_ReturnsUser() { }

[Fact]
public void CreateAsync_WithNullUser_ThrowsArgumentNullException() { }

[Fact]
public async Task UpdateAsync_WithValidUser_ReturnsUpdatedUser() { }

// 异步方法：添加Async后缀
// 异常测试：Throws开头
// 返回值测试：明确的期望结果
```

### 3. 测试分类
```csharp
// 使用Traits进行测试分类
[Trait("Category", "Unit")]
[Trait("Component", "UserService")]
[Fact]
public void CreateAsync_WithValidData_ReturnsSuccess() { }

[Trait("Category", "Integration")]
[Trait("Component", "Authentication")]
[Fact]
public void Login_WithValidCredentials_ReturnsSuccess() { }
```

## 断言和验证

### 1. FluentAssertions 使用
```csharp
// 推荐：使用FluentAssertions进行语义化断言
result.Should().NotBeNull();
result.Id.Should().Be(expectedId);
userList.Should().HaveCount(10);
response.Success.Should().BeTrue();

// 推荐：使用自定义验证方法
TestHelper.AssertApiResponseFormat(response, true);
TestHelper.AssertResultSuccess(result);
```

### 2. 复杂对象验证
```csharp
// 推荐：分步骤验证复杂对象
user.Should().NotBeNull();
user.Id.Should().Be(expectedId);
user.UserName.Should().Be(expectedUserName);
user.Email.Should().Match("*@*.com"); // 使用通配符

// 推荐：使用BeEquivalentTo进行对象比较
actualUser.Should().BeEquivalentTo(expectedUser, options =>
    options.Excluding(u => u.CreatedAt)
           .Excluding(u => u.UpdatedAt));
```

### 3. 集合验证
```csharp
// 推荐：使用集合断言方法
users.Should().HaveCount(expectedCount);
users.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
users.Should().Contain(u => u.Role == UserRole.Admin);

// 推荐：验证集合属性
users.Select(u => u.UserName).Should().Contain(expectedUserNames);
```

## 异步测试

### 1. 异步方法测试
```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsUser()
{
    // Arrange
    var userId = Guid.NewGuid();
    var expectedUser = TestDataFactory.CreateUser(userId);
    _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(expectedUser);

    // Act
    var result = await _sut.GetByIdAsync(userId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(userId);
}

// 关键：测试方法必须是async Task返回类型
// 关键：异步调用必须使用await
// 关键：Mock异步方法返回Task<T>
```

### 2. 异步异常测试
```csharp
[Fact]
public async Task GetByIdAsync_WithNotFound_ThrowsEntityNotFoundException()
{
    // Arrange
    var userId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(userId))
                  .ReturnsAsync((User?)null);

    // Act & Assert
    await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetByIdAsync(userId));
}
```

## 错误和异常测试

### 1. 异常验证
```csharp
[Fact]
public void CreateAsync_WithNullUser_ThrowsArgumentNullException()
{
    // Arrange
    User nullUser = null!;

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() => _sut.Create(nullUser));
    exception.ParamName.Should().Be("user");
}

// 推荐：验证异常类型和消息
// 推荐：验证异常参数名
// 避免过度验证异常细节
```

### 2. 业务规则验证
```csharp
[Fact]
public async Task CreatePrescriptionAsync_WithDuplicateDate_ThrowsBusinessRuleViolationException()
{
    // Arrange
    var prescription = TestDataFactory.CreatePrescription();
    _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(true);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
        () => _sut.CreatePrescriptionAsync(prescription));
    exception.ErrorCode.Should().Be("AR-003"); // 一诊一方规则
}
```

## 集成测试

### 1. 数据库集成测试
```csharp
public class UserRepositoryIntegrationTests : BaseRepositoryTest<IUserRepository, LYBTDbContext, User>
{
    [Fact]
    public async Task AddAsync_ShouldPersistUserToDatabase()
    {
        // Arrange
        var user = TestDataFactory.CreateUser();

        // Act
        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Set<User>().FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser.UserName.Should().Be(user.UserName);
    }
}
```

### 2. API集成测试
```csharp
public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_WithValidRequest_ReturnsSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<UserDto>>>(content);
        apiResponse.Success.Should().BeTrue();
    }
}
```

## 测试维护最佳实践

### 1. 测试代码质量
- **保持简单**: 测试代码应该比生产代码更简单
- **单一职责**: 每个测试只验证一个行为
- **可读性**: 测试应该能清楚地表达其意图
- **DRY原则**: 提取重复的测试逻辑到辅助方法

### 2. 测试数据管理
- **最小化**: 使用最小的必要测试数据
- **独立性**: 每个测试创建自己的数据
- **清理**: 及时清理测试创建的数据
- **一致性**: 使用标准化的测试数据创建方法

### 3. 测试性能
- **快速执行**: 单元测试应该在秒级内完成
- **并行执行**: 支持测试并行运行
- **资源管理**: 合理使用测试资源
- **缓存利用**: 利用测试基础设施的缓存机制

## 常见陷阱和避免方法

### 1. 过度Mock
```csharp
// 避免：过度Mock细节
var mockString = new Mock<string>(); // 错误
mockString.Setup(s => s.Length).Returns(5);

// 推荐：Mock真实依赖
var mockRepository = new Mock<IUserRepository>();
mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
```

### 2. 测试实现细节
```csharp
// 避免：测试私有方法
// var privateMethod = typeof(UserService).GetMethod("ValidateUser", BindingFlags.NonPublic);

// 推荐：通过公共接口测试行为
var result = _userService.CreateUser(user);
result.Should().NotBeNull();
```

### 3. 脆弱的测试
```csharp
// 避免：依赖外部系统
// 避免：依赖特定的时间格式
// 避免：依赖文件系统具体路径

// 推荐：使用可控的依赖
// 推荐：使用相对时间
// 推荐：使用临时文件和目录
```

## 工具和框架使用

### 1. xUnit 最佳实践
```csharp
// 使用[Fact]进行单一测试
[Fact]
public void TestMethod() { }

// 使用[Theory]进行参数化测试
[Theory]
[InlineData(UserRole.Doctor, true)]
[InlineData(UserRole.Admin, true)]
[InlineData(UserRole.Unknown, false)]
public void IsValidRole_WithDifferentRoles_ReturnsExpectedResult(UserRole role, bool expected)
{
    var result = _sut.IsValidRole(role);
    result.Should().Be(expected);
}

// 使用IClassFixture进行共享资源
public class DatabaseTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    public DatabaseTests(DatabaseFixture fixture) => _fixture = fixture;
}
```

### 2. Moq 最佳实践
```csharp
// 推荐：使用It.IsAny进行参数匹配
mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

// 推荐：使用It.Is进行条件匹配
mock.Setup(r => r.GetByIdAsync(It.Is<Guid>(id => id != Guid.Empty))).ReturnsAsync(user);

// 推荐：使用Callback记录调用
var capturedId = Guid.Empty;
mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .Callback<Guid>(id => capturedId = id)
    .ReturnsAsync(user);

// 推荐：验证调用次数和参数
mock.Verify(r => r.GetByIdAsync(expectedId), Times.Once);
```

### 3. FluentAssertions 最佳实践
```csharp
// 推荐：链式断言
result.Should().NotBeNull()
        .And.BeOfType<ApiResponse<UserDto>>()
        .Which.Success.Should().BeTrue();

// 推荐：自定义错误消息
result.Should().NotBeNull("操作结果不应为null");
user.Id.Should().Be(expectedId, $"用户ID应该为 {expectedId}");

// 推荐：集合断言
users.Should().Contain(user => user.Role == UserRole.Admin)
        .And.OnlyContain(user => user.Status == CommonStatus.Enabled);
```

## 代码审查检查清单

### 测试完整性检查
- [ ] 每个公共方法都有对应的测试
- [ ] 每个业务规则都有验证测试
- [ ] 每个异常情况都有处理测试
- [ ] 边界条件都有覆盖测试

### 测试质量检查
- [ ] 遵循AAA模式
- [ ] 使用FluentAssertions
- [ ] Mock对象设置正确
- [ ] 测试数据创建规范

### 测试维护性检查
- [ ] 测试名称清晰
- [ ] 测试逻辑简单
- [ ] 依赖注入正确
- [ ] 资源清理完整

### 测试性能检查
- [ ] 测试执行快速
- [ ] 支持并行执行
- [ ] 内存使用合理
- [ ] 无外部依赖

## 总结

本最佳实践文档基于LYBTZYZS项目的实际经验，特别是JWT测试修复的成功模式，建立了一套完整、实用的单元测试标准。遵循这些实践可以：

1. **提升测试质量**: 减少测试失败，提高测试可靠性
2. **提高开发效率**: 标准化的测试模式减少学习成本
3. **降低维护成本**: 清晰的结构和标准化的代码易于维护
4. **保证代码质量**: 完善的测试覆盖确保功能正确性

定期回顾和更新这些实践，根据项目发展不断完善测试标准。

---
**文档版本**: v1.0
**创建日期**: 2025-11-15
**维护团队**: LYBTZYZS开发团队