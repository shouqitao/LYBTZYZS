# 集成测试架构

## 概述

本文档说明 LYBT 项目的集成测试架构、最佳实践和使用指南。

## 项目结构

```
tests/IntegrationTests/
└── WebAPI.IntegrationTests/
    ├── Infrastructure/           # 测试基础设施
    │   ├── CustomWebApplicationFactory.cs  # 测试应用工厂
    │   ├── TestHelpers.cs                  # 辅助方法
    │   ├── TestDataSeeder.cs               # 种子数据生成器
    │   └── Builders/
    │       └── UserBuilder.cs              # 用户构建器
    ├── Modules/                  # 模块测试
    │   ├── AuthTests.cs         # Auth 模块测试
    │   ├── HealthTests.cs       # Health 模块测试
    │   └── UsersTests.cs        # Users 模块测试
    └── Examples/
        └── UsageExamples.cs     # 使用示例
```

## 核心组件

### 1. CustomWebApplicationFactory

**用途**：为每个测试类提供隔离的测试环境

**特性**：
- 每个测试类使用独立的 SQL Server 数据库（格式：`LYBT_IntegrationTest_{GUID}`）
- 自动创建和清理数据库
- 支持测试环境配置覆盖
- 集成 TestDataSeeder

**示例**：
```csharp
public class MyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }
}
```

### 2. TestHelpers

**用途**：提供常用的测试辅助方法

**主要方法**：
- `LoginAndGetTokenAsync()` - 登录并获取 Token
- `SetAuthorizationHeader()` - 设置授权头
- `LoginAndSetAuthorizationAsync()` - 登录并设置授权（一步完成）
- `CreateUser()` - 创建用户构建器
- `SaveUserAsync()` - 保存用户到数据库

**示例**：
```csharp
// 登录并设置授权
await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

// 创建自定义用户
var (user, password) = CreateUser()
    .WithUserName("test_user")
    .AsDoctor()
    .BuildWithPassword();

await _factory.SaveUserAsync(user);
```

### 3. TestDataSeeder

**用途**：管理测试种子数据

**主要方法**：
- `SeedDefaultUsersAsync()` - 创建默认用户（admin, doctor, pharmacist）
- `CleanAllDataAsync()` - 清理所有数据
- `ResetAsync()` - 重置数据库到初始状态

**示例**：
```csharp
// 初始化默认用户
await _factory.Seeder.SeedDefaultUsersAsync();

// 重置测试数据
await _factory.Seeder.ResetAsync();
```

### 4. UserBuilder

**用途**：使用 Fluent API 构建测试用户

**特性**：
- 链式调用
- 角色快捷方法（`AsAdmin()`, `AsDoctor()`）
- 自动生成唯一用户名
- 支持返回明文密码（用于登录测试）

**示例**：
```csharp
// 创建自定义用户
var (user, password) = CreateUser()
    .WithUserName("dr_zhang")
    .WithRealName("张医生")
    .AsDoctor()
    .WithPhoneNumber("13800138000")
    .WithEmail("dr.zhang@example.com")
    .BuildWithPassword();

await _factory.SaveUserAsync(user);
var token = await _client.LoginAndGetTokenAsync("dr_zhang", password);
```

## 测试模式

### 基础测试模式

```csharp
[Fact]
public async Task TestName_Scenario_ExpectedBehavior()
{
    // Arrange - 准备测试数据
    await _factory.Seeder.SeedDefaultUsersAsync();
    await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

    // Act - 执行操作
    var response = await _client.GetAsync("/api/v1/users");

    // Assert - 验证结果
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
}
```

### 权限测试模式

```csharp
[Fact]
public async Task GetUsers_WithoutAuth_ReturnsUnauthorized()
{
    // Arrange - 不设置授权头
    _client.DefaultRequestHeaders.Authorization = null;

    // Act
    var response = await _client.GetAsync("/api/v1/users");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### 数据验证测试模式

```csharp
[Fact]
public async Task CreateUser_WithInvalidData_ReturnsValidationError()
{
    // Arrange
    await _factory.Seeder.SeedDefaultUsersAsync();
    await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

    var createRequest = new UserCreateDto
    {
        Username = "",  // 无效：空用户名
        Password = "Test123!",
        ConfirmPassword = "Test123!",
        Role = UserRole.Doctor
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/users", createRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
    result.Should().NotBeNull();
    result!.Success.Should().BeFalse();
    result.Message.Should().Contain("用户名");
}
```

## 数据库隔离策略

### 测试类级别隔离

每个测试类（通过 `IClassFixture<CustomWebApplicationFactory>`）使用独立的数据库：

- ✅ **优点**：测试类之间完全隔离，互不影响
- ✅ **优点**：测试类内部可以共享数据
- ⚠️ **注意**：测试类内的测试用例共享同一数据库，需注意数据污染

### 数据清理策略

**自动清理**（推荐）：
```csharp
public class MyTests : IClassFixture<CustomWebApplicationFactory>
{
    // 测试类销毁时，CustomWebApplicationFactory.Dispose()
    // 会自动删除测试数据库
}
```

**手动清理**：
```csharp
[Fact]
public async Task MyTest()
{
    // Arrange
    await _factory.Seeder.SeedDefaultUsersAsync();

    // ... 测试代码 ...

    // Cleanup（可选）
    await _factory.Seeder.CleanAllDataAsync();
}
```

## 本地运行

### 前置条件

- SQL Server LocalDB（随 Visual Studio 或 SQL Server Express 安装）
- .NET 8.0 SDK

### 运行所有集成测试

```powershell
dotnet test tests/IntegrationTests/WebAPI.IntegrationTests/LYBT.WebAPI.IntegrationTests.csproj
```

### 运行特定测试类

```powershell
dotnet test tests/IntegrationTests/WebAPI.IntegrationTests/LYBT.WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~AuthTests"
```

### 运行单个测试

```powershell
dotnet test tests/IntegrationTests/WebAPI.IntegrationTests/LYBT.WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Login_WithValidCredentials_ReturnsToken"
```

### 调试模式

在 Visual Studio 中：
1. 打开 Test Explorer
2. 右键点击测试
3. 选择 "Debug"

## CI/CD 集成

### GitHub Actions

集成测试在 CI 中自动运行（`.github/workflows/ci-integration.yml`）：

**触发条件**：
- Push 到 `master`/`main`/`develop` 分支
- Pull Request 到 `master`/`main`/`develop` 分支
- 手动触发（`workflow_dispatch`）

**执行步骤**：
1. 检出代码
2. 设置 .NET 8.0
3. 恢复 NuGet 包
4. 构建解决方案
5. 初始化 SQL Server LocalDB
6. 运行 WebAPI 集成测试
7. 上传测试结果

### 环境变量

测试支持通过环境变量配置：

```bash
# 自定义数据库连接字符串（可选）
export LYBT_TEST_CONNECTION_STRING="Server=...;Database={DatabaseName};..."
```

**注意**：`{DatabaseName}` 占位符会被自动替换为唯一的测试数据库名称。

## 最佳实践

### 1. 测试命名

使用清晰的命名约定：`MethodName_Scenario_ExpectedBehavior`

**示例**：
- `Login_WithValidCredentials_ReturnsToken`
- `CreateUser_WithDuplicateUsername_ReturnsFail`
- `GetUsers_WithoutAuth_ReturnsUnauthorized`

### 2. 测试结构

遵循 AAA 模式（Arrange-Act-Assert）：

```csharp
[Fact]
public async Task TestName()
{
    // Arrange - 准备
    // ... 初始化数据、设置状态 ...

    // Act - 执行
    // ... 调用被测试的方法 ...

    // Assert - 验证
    // ... 断言结果符合预期 ...
}
```

### 3. 数据隔离

- 每个测试应尽可能独立
- 使用 Builder 模式创建测试数据
- 避免硬编码 ID，使用动态生成

### 4. 异步操作

所有涉及 I/O 的操作使用 `async`/`await`：

```csharp
[Fact]
public async Task MyTest()  // ✅ 正确
{
    await _client.GetAsync("/api/v1/users");
}

[Fact]
public void MyTest()  // ❌ 错误
{
    _client.GetAsync("/api/v1/users").Wait();
}
```

### 5. 使用 FluentAssertions

使用 FluentAssertions 提供清晰的断言：

```csharp
// ✅ 推荐
response.StatusCode.Should().Be(HttpStatusCode.OK);
result.Success.Should().BeTrue();
result.Data.Should().NotBeNull();

// ❌ 避免
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
Assert.True(result.Success);
Assert.NotNull(result.Data);
```

## 故障排查

### 常见问题

**问题 1：数据库连接失败**

```
Cannot connect to database
```

**解决方案**：
1. 确认 SQL Server LocalDB 已安装
2. 检查 LocalDB 实例是否启动：`sqllocaldb info mssqllocaldb`
3. 启动实例：`sqllocaldb start mssqllocaldb`

**问题 2：测试超时**

```
Test timed out after 30000ms
```

**解决方案**：
1. 检查数据库性能
2. 确认没有死锁
3. 增加测试超时时间（如果合理）

**问题 3：端口占用**

```
Address already in use
```

**解决方案**：
1. 停止占用端口的进程
2. 使用不同的端口
3. 清理后台测试进程

## 参考资料

- [ASP.NET Core 集成测试](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit 文档](https://xunit.net/)
- [FluentAssertions 文档](https://fluentassertions.com/)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)

## 更新历史

| 日期 | 版本 | 说明 |
|------|------|------|
| 2025-01-05 | 1.0 | 初始版本 - Phase 3 完成 |
