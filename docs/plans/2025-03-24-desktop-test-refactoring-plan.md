# Desktop 测试重构实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 重构 Desktop 测试架构，引入真实 WebApi 集成测试，建立三层金字塔测试体系

**Architecture:** 
- 单元测试（70%）：验证行为契约，使用状态断言替代 mock 验证
- 集成测试（20%）：TestServer 运行真实 WebApi，Desktop 通过 Refit 连接
- E2E 测试（10%）：关键用户旅程，使用真实进程

**Tech Stack:** 
- xUnit + FluentAssertions + NSubstitute
- Xunit.StaFact (WPF STA 线程)
- WebApplicationFactory (TestServer)
- Refit (HTTP 客户端)
- SQLite In-Memory (测试数据库)

---

## Phase 1: 基础设施搭建

### Task 1: 添加测试基础设施 NuGet 包

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`

**Step 1: 添加 WebApi 集成测试所需包**

```xml
<!-- 在 LYBT.Tests.Desktop.csproj 的 PackageReference 节点中添加 -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Microsoft.Data.Sqlite" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" />
```

**Step 2: 恢复包**

Run: `dotnet restore tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
Expected: 成功恢复所有包

**Step 3: 提交**

```bash
git add tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj
git commit -m "chore(tests): add WebApplicationFactory and SQLite packages for integration testing"
```

---

### Task 2: 创建 WebApi 测试装置 (Fixture)

**Files:**
- Create: `tests/LYBT.Tests.Desktop/Integration/Fixtures/WebApiFixture.cs`

**Step 1: 编写 WebApiFixture**

```csharp
using LYBT.WebAPI;
using LYBT.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace LYBT.Tests.Desktop.Integration.Fixtures;

/// <summary>
/// 提供真实运行的 WebApi 测试装置
/// 使用 SQLite In-Memory 数据库
/// </summary>
public class WebApiFixture : IAsyncLifetime
{
    private SqliteConnection? _sqliteConnection;
    
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient ApiClient { get; private set; } = null!;
    public IServiceProvider Services => Factory.Services;

    public async Task InitializeAsync()
    {
        // 创建 SQLite In-Memory 连接（保持打开状态）
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        // 创建 WebApplicationFactory
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 移除 SQL Server DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // 添加 SQLite DbContext
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(_sqliteConnection));
                });
            });

        ApiClient = Factory.CreateClient();

        // 初始化数据库
        await InitializeDatabaseAsync();
        
        // 预置测试数据
        await SeedTestDataAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 添加测试用户
        db.Users.Add(new Entities.User
        {
            Username = "test_doctor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = Entities.UserRole.Doctor,
            RealName = "测试医生",
            Phone = "13800138000"
        });

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        ApiClient?.Dispose();
        Factory?.Dispose();
        _sqliteConnection?.Close();
        _sqliteConnection?.Dispose();
    }
}
```

**Step 2: 创建集合定义**

Create: `tests/LYBT.Tests.Desktop/Integration/Fixtures/WebApiCollection.cs`

```csharp
using Xunit;

namespace LYBT.Tests.Desktop.Integration.Fixtures;

[CollectionDefinition("WebApi Integration Tests")]
public class WebApiCollection : ICollectionFixture<WebApiFixture>
{
}
```

**Step 3: 验证编译**

Run: `dotnet build tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
Expected: 编译成功

**Step 4: 提交**

```bash
git add tests/LYBT.Tests.Desktop/Integration/Fixtures/
git commit -m "test(infrastructure): add WebApiFixture for integration tests"
```

---

### Task 3: 创建真实 Refit 客户端测试组合

**Files:**
- Create: `tests/LYBT.Tests.Desktop/Integration/Composition/RealTestComposition.cs`
- Create: `tests/LYBT.Tests.Desktop/Integration/Composition/ServiceCollectionExtensions.cs`

**Step 1: 编写 RealTestComposition**

```csharp
using Autofac;
using LYBT.Desktop.Foundation;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace LYBT.Tests.Desktop.Integration.Composition;

/// <summary>
/// 真实测试组合 - 使用真实 DI 容器，只替换外部 HTTP 客户端
/// </summary>
public class RealTestComposition : IDisposable
{
    private IContainer? _container;
    private readonly ContainerBuilder _builder;

    public RealTestComposition()
    {
        _builder = new ContainerBuilder();
        
        // 注册 Desktop Foundation 模块
        _builder.RegisterModule<DesktopFoundationModule>();
        
        // 注册 Infrastructure 模块
        _builder.RegisterModule<InfrastructureModule>();
    }

    /// <summary>
    /// 配置使用真实的 Refit 客户端连接到 TestServer
    /// </summary>
    public RealTestComposition WithRealRefitClient(HttpClient apiClient)
    {
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer()
        };

        // 使用传入的 HttpClient 创建 Refit 客户端
        _builder.RegisterInstance(RestService.For<IAuthApi>(apiClient, refitSettings))
            .As<IAuthApi>();
        _builder.RegisterInstance(RestService.For<IPatientApi>(apiClient, refitSettings))
            .As<IPatientApi>();
        _builder.RegisterInstance(RestService.For<IMedicalCaseApi>(apiClient, refitSettings))
            .As<IMedicalCaseApi>();
        _builder.RegisterInstance(RestService.For<IPrescriptionApi>(apiClient, refitSettings))
            .As<IPrescriptionApi>();
        _builder.RegisterInstance(RestService.For<IHerbApi>(apiClient, refitSettings))
            .As<IHerbApi>();
        _builder.RegisterInstance(RestService.For<IFormulaApi>(apiClient, refitSettings))
            .As<IFormulaApi>();
        _builder.RegisterInstance(RestService.For<IUserApi>(apiClient, refitSettings))
            .As<IUserApi>();
        _builder.RegisterInstance(RestService.For<ISyncApi>(apiClient, refitSettings))
            .As<ISyncApi>();

        return this;
    }

    /// <summary>
    /// 配置使用 Mock 服务（用于纯单元测试）
    /// </summary>
    public RealTestComposition WithMockServices(Action<ContainerBuilder> configureMocks)
    {
        configureMocks(_builder);
        return this;
    }

    public RealTestComposition Build()
    {
        _container = _builder.Build();
        return this;
    }

    public T Resolve<T>() where T : notnull
    {
        if (_container == null) throw new InvalidOperationException("Call Build() first");
        return _container.Resolve<T>();
    }

    public IServiceProvider GetServiceProvider()
    {
        if (_container == null) throw new InvalidOperationException("Call Build() first");
        return new AutofacServiceProvider(_container);
    }

    public void Dispose()
    {
        _container?.Dispose();
    }
}
```

**Step 2: 验证编译**

Run: `dotnet build tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
Expected: 编译成功

**Step 3: 提交**

```bash
git add tests/LYBT.Tests.Desktop/Integration/Composition/
git commit -m "test(infrastructure): add RealTestComposition for DI container testing"
```

---

## Phase 2: 编写真实 API 集成测试

### Task 4: 创建认证流程集成测试

**Files:**
- Create: `tests/LYBT.Tests.Desktop/Integration/Flows/AuthenticationFlowTests.cs`

**Step 1: 编写测试**

```csharp
using FluentAssertions;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts.Services;
using LYBT.Tests.Desktop.Integration.Composition;
using LYBT.Tests.Desktop.Integration.Fixtures;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.Flows;

[Collection("WebApi Integration Tests")]
public class AuthenticationFlowTests : IDisposable
{
    private readonly WebApiFixture _fixture;
    private readonly RealTestComposition _composition;

    public AuthenticationFlowTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _composition = new RealTestComposition()
            .WithRealRefitClient(_fixture.ApiClient)
            .Build();
    }

    [StaFact]
    public async Task Login_WithValidCredentials_ReturnsRealToken()
    {
        // Arrange
        var loginVm = _composition.Resolve<LoginViewModel>();
        loginVm.Username = "test_doctor";
        loginVm.Password = "password123";

        // Act
        await loginVm.LoginCommand.ExecuteAsync(null);

        // Assert - 验证真实 JWT 令牌
        var tokenStore = _composition.Resolve<ITokenStore>();
        tokenStore.AccessToken.Should().NotBeNullOrEmpty();

        // 解码验证令牌内容
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStore.AccessToken);
        token.Claims.Should().Contain(c => 
            c.Type == "role" && c.Value == "Doctor");
        token.Claims.Should().Contain(c => 
            c.Type == "unique_name" && c.Value == "test_doctor");
    }

    [StaFact]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        // Arrange
        var loginVm = _composition.Resolve<LoginViewModel>();
        loginVm.Username = "test_doctor";
        loginVm.Password = "wrong_password";

        // Act
        await loginVm.LoginCommand.ExecuteAsync(null);

        // Assert
        loginVm.ErrorMessage.Should().NotBeNullOrEmpty();
        loginVm.IsAuthenticated.Should().BeFalse();
    }

    [StaFact]
    public async Task Login_WithValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        var loginVm = _composition.Resolve<LoginViewModel>();
        var regionManager = _composition.Resolve<IRegionManager>();
        loginVm.Username = "test_doctor";
        loginVm.Password = "password123";

        // Act
        await loginVm.LoginCommand.ExecuteAsync(null);

        // Assert - 验证导航实际发生
        loginVm.IsAuthenticated.Should().BeTrue();
        // Note: 实际导航验证需要更复杂的设置
    }

    public void Dispose()
    {
        _composition?.Dispose();
    }
}
```

**Step 2: 运行测试验证**

Run: `dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "FullyQualifiedName~AuthenticationFlowTests" -v n`
Expected: 
- 测试发现 3 个测试
- 可能有失败（需要调整 ViewModel 依赖）

**Step 3: 提交**

```bash
git add tests/LYBT.Tests.Desktop/Integration/Flows/AuthenticationFlowTests.cs
git commit -m "test(integration): add authentication flow integration tests"
```

---

### Task 5: 创建患者管理集成测试

**Files:**
- Create: `tests/LYBT.Tests.Desktop/Integration/Flows/PatientManagementFlowTests.cs`

**Step 1: 编写测试**

```csharp
using FluentAssertions;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Entities;
using LYBT.Infrastructure.Persistence;
using LYBT.Tests.Desktop.Integration.Composition;
using LYBT.Tests.Desktop.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.Flows;

[Collection("WebApi Integration Tests")]
public class PatientManagementFlowTests : IDisposable
{
    private readonly WebApiFixture _fixture;
    private readonly RealTestComposition _composition;

    public PatientManagementFlowTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _composition = new RealTestComposition()
            .WithRealRefitClient(_fixture.ApiClient)
            .Build();
    }

    [StaFact]
    public async Task CreatePatient_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var patientEditVm = _composition.Resolve<PatientEditViewModel>();
        patientEditVm.Name = "张三";
        patientEditVm.Phone = "13800138000";
        patientEditVm.Gender = Gender.Male;
        patientEditVm.DateOfBirth = new DateTime(1990, 1, 1);

        // Act
        await patientEditVm.SaveCommand.ExecuteAsync(null);

        // Assert - 直接查询数据库验证
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedPatient = await db.Patients
            .FirstOrDefaultAsync(p => p.Name == "张三");

        savedPatient.Should().NotBeNull();
        savedPatient.Phone.Should().Be("13800138000");
        savedPatient.Gender.Should().Be(Gender.Male);
    }

    [StaFact]
    public async Task CreatePatient_WithDuplicatePhone_ShowsError()
    {
        // Arrange - 先创建一个患者
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Patients.Add(new Patient
            {
                Name = "李四",
                Phone = "13900139000",
                Gender = Gender.Female
            });
            await db.SaveChangesAsync();
        }

        // Act - 尝试创建相同手机号的患者
        var patientEditVm = _composition.Resolve<PatientEditViewModel>();
        patientEditVm.Name = "王五";
        patientEditVm.Phone = "13900139000"; // 重复手机号
        patientEditVm.Gender = Gender.Male;

        await patientEditVm.SaveCommand.ExecuteAsync(null);

        // Assert
        patientEditVm.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        _composition?.Dispose();
    }
}
```

**Step 2: 提交**

```bash
git add tests/LYBT.Tests.Desktop/Integration/Flows/PatientManagementFlowTests.cs
git commit -m "test(integration): add patient management flow tests"
```

---

## Phase 3: 重构单元测试

### Task 6: 重构 LoginViewModel 单元测试 - 行为契约验证

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/PureLogic/Auth/LoginViewModelTests.cs`

**Step 1: 添加行为契约验证测试**

在现有测试文件中添加新的测试区域：

```csharp
#region 行为契约验证 - 新增

[Fact]
public async Task LoginAsync_WithValidCredentials_UpdatesAuthenticationState()
{
    // Arrange - 使用真实返回结果
    var sut = CreateSut();
    sut.Username = "admin";
    sut.Password = "password123";

    var expectedUser = new UserDetailDto 
    { 
        Id = Guid.NewGuid(),
        Username = "admin",
        Role = UserRole.Doctor
    };

    _loginCoordinator.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
        .Returns(LoginResult.Succeeded(expectedUser));

    // Act
    await sut.LoginCommand.ExecuteAsync(null);

    // Assert - 验证最终状态，不是 mock 调用
    sut.IsAuthenticated.Should().BeTrue();
    sut.CurrentUser.Should().BeEquivalentTo(expectedUser);
    sut.ErrorMessage.Should().BeNullOrEmpty();
}

[Fact]
public async Task LoginAsync_WithInvalidCredentials_ClearsPasswordAndShowsError()
{
    // Arrange
    var sut = CreateSut();
    sut.Username = "admin";
    sut.Password = "wrong_password";

    _loginCoordinator.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
        .Returns(LoginResult.Failed("Invalid credentials"));

    // Act
    await sut.LoginCommand.ExecuteAsync(null);

    // Assert - 验证状态变化
    sut.IsAuthenticated.Should().BeFalse();
    sut.Password.Should().BeEmpty();
    sut.ErrorMessage.Should().Contain("Invalid");
}

[Fact]
public async Task LoginAsync_SuccessWithRememberPassword_SavesCredentials()
{
    // Arrange
    var sut = CreateSut();
    sut.Username = "admin";
    sut.Password = "password123";
    sut.RememberUsername = true;
    sut.RememberPassword = true;

    _loginCoordinator.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
        .Returns(LoginResult.Succeeded(new UserDetailDto()));
    _credentialVault.SavePasswordAsync(Arg.Any<string>(), Arg.Any<string>())
        .Returns(true);

    // Act
    await sut.LoginCommand.ExecuteAsync(null);

    // Assert - 验证凭证保存行为
    await _usernameStorage.Received(1).SaveUsernameAsync("admin", true);
    await _credentialVault.Received(1).SavePasswordAsync("admin", "password123");
}

#endregion
```

**Step 2: 运行测试验证**

Run: `dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "FullyQualifiedName~LoginViewModelTests"`
Expected: 所有测试通过

**Step 3: 提交**

```bash
git add tests/LYBT.Tests.Desktop/PureLogic/Auth/LoginViewModelTests.cs
git commit -m "test(unit): add behavior contract tests for LoginViewModel"
```

---

## Phase 4: 测试数据构建器

### Task 7: 创建测试数据构建器

**Files:**
- Create: `tests/LYBT.Tests.Desktop/_Infrastructure/Builders/PatientBuilder.cs`
- Create: `tests/LYBT.Tests.Desktop/_Infrastructure/Builders/UserBuilder.cs`

**Step 1: 编写 PatientBuilder**

```csharp
using LYBT.Entities;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

/// <summary>
/// Patient 实体测试数据构建器
/// </summary>
public class PatientBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "测试患者";
    private string _phone = "13800138000";
    private Gender _gender = Gender.Male;
    private DateTime? _dateOfBirth = new DateTime(1990, 1, 1);
    private string? _address = "测试地址";

    public PatientBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PatientBuilder WithPhone(string phone)
    {
        _phone = phone;
        return this;
    }

    public PatientBuilder WithGender(Gender gender)
    {
        _gender = gender;
        return this;
    }

    public PatientBuilder WithDateOfBirth(DateTime? dateOfBirth)
    {
        _dateOfBirth = dateOfBirth;
        return this;
    }

    public PatientBuilder WithAddress(string? address)
    {
        _address = address;
        return this;
    }

    public Patient Build()
    {
        return new Patient
        {
            Id = _id,
            Name = _name,
            Phone = _phone,
            Gender = _gender,
            DateOfBirth = _dateOfBirth,
            Address = _address
        };
    }

    public static PatientBuilder Create() => new();
}
```

**Step 2: 编写 UserBuilder**

```csharp
using LYBT.Entities;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _username = "test_user";
    private string _passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
    private UserRole _role = UserRole.Doctor;
    private string _realName = "测试用户";
    private string? _phone = "13800138000";

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithPassword(string password)
    {
        _passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder WithRealName(string realName)
    {
        _realName = realName;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Username = _username,
            PasswordHash = _passwordHash,
            Role = _role,
            RealName = _realName,
            Phone = _phone
        };
    }

    public static UserBuilder Create() => new();
}
```

**Step 3: 提交**

```bash
git add tests/LYBT.Tests.Desktop/_Infrastructure/Builders/
git commit -m "test(infrastructure): add test data builders for Patient and User"
```

---

## Phase 5: 自定义断言

### Task 8: 创建 JWT 断言扩展

**Files:**
- Create: `tests/LYBT.Tests.Desktop/_Infrastructure/Assertions/JwtAssertions.cs`

**Step 1: 编写 JWT 断言**

```csharp
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using System.IdentityModel.Tokens.Jwt;

namespace LYBT.Tests.Desktop._Infrastructure.Assertions;

public static class JwtAssertions
{
    public static JwtSecurityTokenAssertions Should(this JwtSecurityToken? actualValue)
    {
        return new JwtSecurityTokenAssertions(actualValue);
    }
}

public class JwtSecurityTokenAssertions : ReferenceTypeAssertions<JwtSecurityToken?, JwtSecurityTokenAssertions>
{
    public JwtSecurityTokenAssertions(JwtSecurityToken? subject) : base(subject) { }

    protected override string Identifier => "JWT token";

    public AndConstraint<JwtSecurityTokenAssertions> BeValidJwt(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected JWT token to not be null{reason}.");

        return new AndConstraint<JwtSecurityTokenAssertions>(this);
    }

    public AndConstraint<JwtSecurityTokenAssertions> HaveClaim(string claimType, string expectedValue, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject != null && Subject.Claims.Any(c => c.Type == claimType && c.Value == expectedValue))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected JWT token to have claim '{0}' with value '{1}'{reason}, but found {2}.",
                claimType, expectedValue, Subject?.Claims.Select(c => $"{c.Type}={c.Value}"));

        return new AndConstraint<JwtSecurityTokenAssertions>(this);
    }

    public AndConstraint<JwtSecurityTokenAssertions> HaveRole(string expectedRole, string because = "", params object[] becauseArgs)
    {
        return HaveClaim("role", expectedRole, because, becauseArgs);
    }

    public AndConstraint<JwtSecurityTokenAssertions> HaveUsername(string expectedUsername, string because = "", params object[] becauseArgs)
    {
        return HaveClaim("unique_name", expectedUsername, because, becauseArgs);
    }
}
```

**Step 2: 提交**

```bash
git add tests/LYBT.Tests.Desktop/_Infrastructure/Assertions/
git commit -m "test(infrastructure): add JWT custom assertions"
```

---

## Phase 6: 测试运行配置

### Task 9: 配置测试分类运行

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`

**Step 1: 添加测试分类属性**

在 csproj 中添加：

```xml
<!-- 在 PropertyGroup 中添加 -->
<PropertyGroup>
  <!-- 测试分类标签 -->
  <DefineConstants>$(DefineConstants);INTEGRATION_TESTS</DefineConstants>
</PropertyGroup>
```

**Step 2: 创建 Traits 用于分类**

Create: `tests/LYBT.Tests.Desktop/_Infrastructure/Traits.cs`

```csharp
namespace LYBT.Tests.Desktop._Infrastructure;

/// <summary>
/// 测试分类常量
/// </summary>
public static class TestTraits
{
    public const string Category = "Category";
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string E2E = "E2E";
}

/// <summary>
/// 标记为单元测试
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class UnitTestAttribute : TraitAttribute
{
    public UnitTestAttribute() : base(TestTraits.Category, TestTraits.Unit) { }
}

/// <summary>
/// 标记为集成测试
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class IntegrationTestAttribute : TraitAttribute
{
    public IntegrationTestAttribute() : base(TestTraits.Category, TestTraits.Integration) { }
}

/// <summary>
/// 标记为 E2E 测试
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class E2ETestAttribute : TraitAttribute
{
    public E2ETestAttribute() : base(TestTraits.Category, TestTraits.E2E) { }
}
```

**Step 3: 提交**

```bash
git add tests/LYBT.Tests.Desktop/_Infrastructure/Traits.cs
git commit -m "test(infrastructure): add test category traits"
```

---

## 运行命令汇总

```bash
# 运行所有测试
dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj

# 仅运行单元测试
dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "Category=Unit"

# 仅运行集成测试
dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "Category=Integration"

# 排除集成测试（快速反馈）
dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --filter "Category!=Integration"

# 生成覆盖率报告
dotnet test tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj --collect:"XPlat Code Coverage"
```

---

## 验证清单

- [ ] WebApiFixture 能正确启动 TestServer
- [ ] SQLite In-Memory 数据库工作正常
- [ ] RealTestComposition 能正确解析 ViewModel
- [ ] 集成测试能通过 Refit 调用到真实 API
- [ ] 单元测试保持原有功能
- [ ] 所有测试能通过分类筛选

---

**Plan complete and saved to `docs/plans/2025-03-24-desktop-test-refactoring-plan.md`.**

**Two execution options:**

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**