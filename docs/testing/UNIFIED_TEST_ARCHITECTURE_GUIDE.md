# 统一测试架构指南

> 版本：1.0  
> 更新：2025-01-08  
> 目标：建立统一的测试基础设施，提升测试覆盖率和代码质量

## 🎯 测试架构目标

### 统一性目标
- 统一的测试基础设施，减少重复代码
- 一致的测试命名约定和文件组织结构
- 标准化的数据生成和Mock设置
- 统一的断言和验证方法

### 质量目标
- 代码覆盖率从当前2.76%提升至60%
- 支持数据驱动测试，提升测试场景覆盖
- 自动化测试数据生成，减少手动维护成本
- 完整的边界值和异常处理测试

---

## 🏗️ 统一测试架构组件

### 核心基础设施

#### 1. BaseTestFixture
```csharp
// 位置：tests/Backend/Core/BaseTestFixture.cs
// 功能：统一的测试基础设施，整合数据库、Mock、数据生成器
public abstract class BaseTestFixture : IDisposable
{
    protected readonly AppDbContext Context;           // 内存数据库
    protected readonly Mock<IUnifiedLogService> MockLogService;  // 日志Mock
    protected readonly IMapper Mapper;                 // AutoMapper
    protected readonly TestDataFactory DataFactory;   // 数据生成器
    protected readonly List<object> CapturedLogs;     // 日志捕获
}
```

#### 2. TestDataFactory
```csharp
// 位置：tests/Backend/Core/TestDataFactory.cs
// 功能：基于Bogus的测试数据生成工厂
public class TestDataFactory
{
    public Faker<UserModel> UserModelFaker { get; }        // 用户数据生成
    public Faker<PatientModel> PatientModelFaker { get; }  // 患者数据生成
    public Faker<HerbModel> HerbModelFaker { get; }        // 中药材数据生成
    
    // 数据驱动测试支持
    public static IEnumerable<object[]> GetBoundaryTestData<T>();
    public static IEnumerable<object[]> GetPaginationTestData();
}
```

#### 3. DataDrivenTestSupport
```csharp
// 位置：tests/Backend/Core/DataDrivenTestSupport.cs
// 功能：数据驱动测试特性和扩展方法
[TestDataSource("GetUserTestData")]        // 静态数据源
[BoundaryTest(typeof(int))]               // 边界值测试
[PaginationTest]                          // 分页测试
[GuidTest(validOnly: false)]              // GUID测试
[PasswordComplexityTest]                  // 密码复杂度测试
```

---

## 📋 测试命名约定

### 文件命名约定

```
tests/Backend/
├── Core/                           # 核心测试基础设施
│   ├── BaseTestFixture.cs
│   ├── TestDataFactory.cs
│   └── DataDrivenTestSupport.cs
├── LYBT.Module.{ModuleName}.Tests/        # 模块测试项目
│   ├── Base/                       # 模块特定测试基类
│   │   ├── {Module}TestFixture.cs
│   │   └── {Module}TestData.cs
│   ├── Repositories/               # Repository层测试
│   │   └── {Entity}RepositoryTests.cs
│   ├── Services/                   # Service层测试
│   │   └── {Entity}ServiceTests.cs
│   ├── Controllers/                # Controller层测试
│   │   └── {Entity}ControllerTests.cs
│   └── Integration/                # 集成测试
│       └── {Scenario}IntegrationTests.cs
```

### 测试类命名约定

```csharp
// Repository测试：{Entity}RepositoryTests
public class UserRepositoryTests : BaseTestFixture { }

// Service测试：{Entity}ServiceTests
public class UserServiceTests : BaseTestFixture { }

// Controller测试：{Entity}ControllerTests
public class UsersControllerTests : BaseTestFixture { }

// Integration测试：{Scenario}IntegrationTests
public class UserManagementIntegrationTests : BaseTestFixture { }
```

### 测试方法命名约定

```csharp
// 模式：{Method}_{Scenario}_{ExpectedResult}

// 正常场景测试
[Fact]
public async Task GetByIdAsync_WithValidId_ShouldReturnUser() { }

// 异常场景测试
[Fact]
public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull() { }

// 边界值测试
[Theory]
[BoundaryTest(typeof(int))]
public async Task GetPagedAsync_WithBoundaryPageSize_ShouldHandleCorrectly(int pageSize) { }

// 数据驱动测试
[Theory]
[TestDataSource(nameof(GetPasswordTestData))]
public void ValidatePassword_WithVariousInputs_ShouldValidateCorrectly(string password, bool expected) { }
```

---

## 🔧 使用指南

### 1. 创建新的测试类

#### Repository测试示例
```csharp
using LYBT.Tests.Backend.Core;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Models.Users;

namespace LYBT.Module.Users.Tests.Repositories
{
    [TestCategory(TestCategories.Repository)]
    public class UserRepositoryTests : BaseTestFixture
    {
        private readonly IUserRepository _repository;

        public UserRepositoryTests()
        {
            _repository = new UserRepository(Context);
        }

        [Fact]
        public async Task AddAsync_WithValidUser_ShouldReturnCreatedUser()
        {
            // Arrange
            var user = DataFactory.UserModelFaker.Generate();

            // Act
            var result = await _repository.AddAsync(user);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBeEmpty();
            result.Username.Should().Be(user.Username);
        }

        [Theory]
        [GuidTest(validOnly: false)]
        public async Task GetByIdAsync_WithInvalidGuid_ShouldReturnNull(Guid id)
        {
            // Act
            var result = await _repository.GetByIdAsync(id);

            // Assert
            result.Should().BeNull();
        }
    }
}
```

#### Service测试示例
```csharp
using LYBT.Tests.Backend.Core;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Tests.Services
{
    [TestCategory(TestCategories.Service)]
    public class UserServiceTests : BaseTestFixture
    {
        private readonly IUserService _service;
        private readonly Mock<IUserRepository> _mockRepository;

        public UserServiceTests()
        {
            _mockRepository = CreateMockRepository<IUserRepository, UserModel>();
            _service = new UserService(_mockRepository.Object, Mapper, MockLogService.Object);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var createDto = DataFactory.UserCreateDtoFaker.Generate();
            var expectedUser = DataFactory.UserModelFaker.Generate();
            
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserModel>()))
                          .ReturnsAsync(expectedUser);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Username.Should().Be(createDto.Username);
            VerifyNoErrorLogs();
        }

        [Theory]
        [TestDataSource(nameof(GetInvalidUserData))]
        public async Task CreateAsync_WithInvalidData_ShouldThrowException(UserCreateDto invalidDto)
        {
            // Act & Assert
            await _service.CreateAsync(invalidDto)
                         .ShouldThrowAsync<ArgumentException>();
        }

        public static IEnumerable<object[]> GetInvalidUserData()
        {
            var factory = new TestDataFactory();
            
            yield return new object[] { factory.UserCreateDtoFaker.Generate() with { Username = null! } };
            yield return new object[] { factory.UserCreateDtoFaker.Generate() with { Username = "" } };
            yield return new object[] { factory.UserCreateDtoFaker.Generate() with { RealName = null! } };
        }
    }
}
```

### 2. 数据驱动测试

#### 边界值测试
```csharp
[Theory]
[BoundaryTest(typeof(int))]
[TestCategory(TestCategories.BoundaryValue)]
public async Task GetPagedAsync_WithBoundaryPageNumber_ShouldHandleCorrectly(int pageNumber)
{
    // Act
    var result = await _service.GetPagedAsync(pageNumber, 10);

    // Assert
    if (pageNumber <= 0)
    {
        result.Should().BeNull(); // 或抛出异常
    }
    else
    {
        result.ShouldNotBeNull();
    }
}
```

#### 自定义数据驱动测试
```csharp
[Theory]
[TestDataSource(nameof(GetPasswordComplexityTestData))]
[TestCategory(TestCategories.DataDriven)]
public void ValidatePasswordComplexity_WithVariousPasswords_ShouldValidateCorrectly(
    string password, bool expectedValid, string reason)
{
    // Act
    var result = PasswordValidator.IsValid(password);

    // Assert
    Assert.Equal(expectedValid, result.IsValid);
    if (!expectedValid)
    {
        result.ErrorMessage.ShouldContain(reason);
    }
}

public static IEnumerable<object[]> GetPasswordComplexityTestData()
{
    return TestDataFactory.GetPasswordComplexityTestData();
}
```

### 3. 集成测试

```csharp
[TestCategory(TestCategories.Integration)]
public class UserManagementIntegrationTests : BaseTestFixture
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;

    public UserManagementIntegrationTests()
    {
        _userRepository = new UserRepository(Context);
        _userService = new UserService(_userRepository, Mapper, MockLogService.Object);
    }

    [Fact]
    public async Task CreateUser_UpdateUser_DeleteUser_ShouldWorkEndToEnd()
    {
        // Arrange
        var createDto = DataFactory.UserCreateDtoFaker.Generate();

        // Act & Assert - Create
        var createdUser = await _userService.CreateAsync(createDto);
        createdUser.ShouldNotBeNull();

        // Act & Assert - Update
        var updateDto = DataFactory.UserUpdateDtoFaker.Generate() with { Id = createdUser.Id };
        var updateResult = await _userService.UpdateAsync(updateDto.Id, updateDto);
        updateResult.Should().BeTrue();

        // Act & Assert - Delete
        var deleteResult = await _userService.DeleteAsync(createdUser.Id);
        deleteResult.Should().BeTrue();

        // Verify deletion
        var deletedUser = await _userService.GetByIdAsync(createdUser.Id);
        deletedUser.Should().BeNull();
    }
}
```

---

## 📊 测试覆盖率目标

### 当前状态
- **总覆盖率**：2.76%
- **Repository层**：97个测试用例，100%通过
- **Service层**：156个测试用例，100%通过

### 目标状态（60%覆盖率）
- **Repository层**：完成 ✅
- **Service层**：需要补充HerbService、AuthService等
- **Controller层**：新增Controller层单元测试
- **Integration层**：新增集成测试覆盖关键业务流程

### 测试分布目标
```
Repository Tests    (20% of total coverage)
├── UserRepository     ✅ 完成
├── PatientRepository  ✅ 完成  
├── HerbRepository     ✅ 完成
└── Other Repositories ⏳ 待完成

Service Tests       (25% of total coverage)
├── UserService       ✅ 完成
├── PatientService    ✅ 完成
├── HerbService       ⏳ 待完成
├── AuthService       ⏳ 待完成
└── Other Services    ⏳ 待完成

Controller Tests    (10% of total coverage)
├── UsersController   ⏳ 待完成
├── PatientsController ⏳ 待完成
└── Other Controllers ⏳ 待完成

Integration Tests   (5% of total coverage)
├── User Management   ⏳ 待完成
├── Patient Management ⏳ 待完成
└── Consultation Flow ⏳ 待完成
```

---

## 🔍 测试质量检查清单

### 测试结构检查
- [ ] 继承统一的BaseTestFixture
- [ ] 使用TestDataFactory生成测试数据
- [ ] 应用适当的测试类别特性
- [ ] 遵循统一的命名约定

### 测试内容检查
- [ ] 包含正常场景测试
- [ ] 包含异常场景测试
- [ ] 包含边界值测试
- [ ] 包含数据验证测试
- [ ] 验证Mock调用和日志记录

### 测试质量检查
- [ ] 测试独立性（每个测试可独立运行）
- [ ] 数据隔离（测试间不相互影响）
- [ ] 断言充分（验证所有重要属性）
- [ ] 性能合理（单个测试执行时间<1秒）

---

## 🚀 实施路线图

### 第一阶段：基础设施完善 ✅
- [x] 创建BaseTestFixture统一基础设施
- [x] 创建TestDataFactory数据生成器
- [x] 创建DataDrivenTestSupport数据驱动框架
- [x] 创建测试约定和指南文档

### 第二阶段：现有测试迁移
- [ ] 将现有UserRepository/Service测试迁移到新架构
- [ ] 将现有PatientRepository/Service测试迁移到新架构
- [ ] 将现有HerbRepository测试迁移到新架构

### 第三阶段：测试补全
- [ ] 补充缺失的Service层测试
- [ ] 新增Controller层测试
- [ ] 新增集成测试

### 第四阶段：质量验证
- [ ] 运行完整测试套件
- [ ] 验证覆盖率达到60%目标
- [ ] 性能和质量检查

---

*"通过统一测试架构，我们不仅提升了代码覆盖率，更建立了可持续的质量保障体系。"*