# 单元测试指南

**维护人**：Coder (Claude Code)  
**最后更新**：2025-10-11  
**Issue追踪**：#1143 - Phase 2 Day 2 测试指南SSOT重构

> 📚 **培训材料**：新人入职请先阅读 [testing-training-materials.md](testing-training-materials.md)  
> 🏗️ **架构测试**：架构约束请参考 [architecture/testing/architecture-testing-guide.md](../architecture/testing/architecture-testing-guide.md)

---

## 1. 概述

### 1.1 技术栈

| 工具 | 版本 | 用途 |
|------|------|------|
| **xUnit** | 2.6.6 | 测试框架 |
| **Moq** | 4.20.72 | Mock框架 |
| **FluentAssertions** | 6.12.2 | 断言库 |
| **Coverlet** | 6.0.2 | 覆盖率收集 |
| **ReportGenerator** | 5.3.11 | 覆盖率报告 |

### 1.2 覆盖率要求

- **行覆盖率**：≥90%
- **分支覆盖率**：≥80%
- **CI/CD门禁**：≥80%（低于此值构建失败）
- **MVP当前状态**：~62.5% → 80%目标

### 1.3 FIRST原则

- **F**ast - 快速：单个测试<100ms
- **I**ndependent - 独立：测试间无依赖
- **R**epeatable - 可重复：任何环境都能运行
- **S**elf-Validating - 自验证：通过/失败明确
- **T**imely - 及时：与代码同步编写

---

## 2. 快速开始

### 2.1 环境配置

#### 2.1.1 .runsettings配置（推荐）

创建 `tests/.runsettings`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <MaxCpuCount>0</MaxCpuCount>  <!-- 0=自动检测CPU核心数 -->
    <TestSessionTimeout>300000</TestSessionTimeout>  <!-- 5分钟超时 -->
  </RunConfiguration>

  <xUnit>
    <ParallelizeTestCollections>true</ParallelizeTestCollections>
    <MaxParallelThreads>-1</MaxParallelThreads>  <!-- -1=自动 -->
  </xUnit>

  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>opencover,cobertura</Format>
          <Exclude>[*]*.Migrations.*,[*.Tests]*</Exclude>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

#### 2.1.2 NuGet包引用

在测试项目 `.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.6" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="FluentAssertions" Version="6.12.2" />
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
</ItemGroup>
```

### 2.2 创建第一个测试

```csharp
using Xunit;
using FluentAssertions;
using Moq;

namespace LYBT.Module.Patients.Tests
{
    public class PatientServiceTests
    {
        private readonly Mock<IPatientRepository> _mockRepository;
        private readonly PatientService _service;

        public PatientServiceTests()
        {
            // Arrange - 初始化Mock和服务
            _mockRepository = new Mock<IPatientRepository>();
            _service = new PatientService(_mockRepository.Object);
        }

        [Fact]
        public void GetPatient_Should_ReturnPatient_When_PatientExists()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedPatient = new Patient { Id = patientId, Name = "张三" };
            _mockRepository
                .Setup(r => r.GetByIdAsync(patientId))
                .ReturnsAsync(expectedPatient);

            // Act
            var result = await _service.GetPatientAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(patientId);
            result.Name.Should().Be("张三");
        }
    }
}
```

### 2.3 运行测试

#### VS2022运行（推荐）

1. 打开"测试资源管理器"（Ctrl+E, T）
2. 右键解决方案 → "运行所有测试"
3. 使用 `tests/.runsettings`：工具 → 选项 → 测试 → 测试设置文件

#### 命令行运行

```powershell
# Server端测试（推荐）
dotnet test LYBT.Server.sln -c Release --settings tests/.runsettings

# 带详细输出
dotnet test LYBT.Server.sln -c Release --logger "console;verbosity=detailed"

# 收集覆盖率
dotnet test LYBT.Server.sln --collect:"XPlat Code Coverage"
```

**注意**：Desktop端测试当前阻塞（需WPF初始化），仅运行Server端测试。

### 2.4 查看覆盖率报告

```powershell
# 生成HTML报告
reportgenerator `
  -reports:**/coverage.cobertura.xml `
  -targetdir:TestResults/CoverageReport `
  -reporttypes:Html

# 打开报告
start TestResults/CoverageReport/index.html
```

---

## 3. 测试编写规范

### 3.1 AAA模式（Arrange-Act-Assert）

**标准结构**：

```csharp
[Fact]
public void MethodName_Should_ExpectedBehavior_When_Condition()
{
    // Arrange - 准备测试数据和Mock
    var input = new CreateUserDto { Username = "test", Password = "Pass123!" };
    var expectedUser = new User { Id = Guid.NewGuid(), Username = "test" };
    
    _mockRepository
        .Setup(r => r.CreateAsync(It.IsAny<User>()))
        .ReturnsAsync(expectedUser);

    // Act - 执行被测试的方法
    var result = await _service.CreateUserAsync(input);

    // Assert - 验证结果
    result.Should().NotBeNull();
    result.Username.Should().Be("test");
    
    // 验证Mock调用
    _mockRepository.Verify(
        r => r.CreateAsync(It.Is<User>(u => u.Username == "test")),
        Times.Once
    );
}
```

### 3.2 测试命名约定

**格式**：`{MethodName}_{Scenario}_{ExpectedResult}`

**示例**：
- ✅ `GetPatient_Should_ReturnPatient_When_PatientExists`
- ✅ `CreateUser_Should_ThrowException_When_UsernameExists`
- ✅ `CalculateTotal_Should_ReturnZero_When_NoItems`
- ❌ `Test1`、`TestGetPatient`（不明确）

### 3.3 Mock使用规范

#### 3.3.1 Setup返回值

```csharp
// 返回固定值
_mockRepository
    .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(expectedPatient);

// 返回基于参数
_mockRepository
    .Setup(r => r.GetByIdAsync(It.Is<Guid>(id => id == targetId)))
    .ReturnsAsync(expectedPatient);

// 返回序列（多次调用不同值）
_mockRepository
    .SetupSequence(r => r.GetNextAsync())
    .ReturnsAsync(patient1)
    .ReturnsAsync(patient2)
    .ReturnsAsync((Patient)null);
```

#### 3.3.2 Setup异常

```csharp
_mockRepository
    .Setup(r => r.CreateAsync(It.IsAny<Patient>()))
    .ThrowsAsync(new DuplicatePatientException("患者已存在"));
```

#### 3.3.3 Verify调用

```csharp
// 验证调用一次
_mockRepository.Verify(
    r => r.SaveAsync(It.IsAny<Patient>()),
    Times.Once
);

// 验证从未调用
_mockRepository.Verify(
    r => r.DeleteAsync(It.IsAny<Guid>()),
    Times.Never
);

// 验证调用参数
_mockRepository.Verify(
    r => r.UpdateAsync(It.Is<Patient>(p => p.Id == patientId)),
    Times.Once
);
```

#### 3.3.4 Callback副作用

```csharp
Patient capturedPatient = null;

_mockRepository
    .Setup(r => r.CreateAsync(It.IsAny<Patient>()))
    .Callback<Patient>(p => capturedPatient = p)
    .ReturnsAsync((Patient p) => p);

// Act
await _service.CreatePatientAsync(dto);

// Assert
capturedPatient.Should().NotBeNull();
capturedPatient.Name.Should().Be(dto.Name);
```

### 3.4 数据驱动测试

#### 3.4.1 InlineData（简单数据）

```csharp
[Theory]
[InlineData(0, 0)]
[InlineData(50, 0)]
[InlineData(100, 0.10)]
[InlineData(200, 0.20)]
public void GetDiscountRate_Should_ReturnCorrectRate(int quantity, decimal expectedRate)
{
    var rate = _service.GetDiscountRate(quantity);
    rate.Should().Be(expectedRate);
}
```

#### 3.4.2 MemberData（复杂数据）

```csharp
public static IEnumerable<object[]> PrescriptionTestData =>
    new List<object[]>
    {
        new object[] { new List<Item> { new Item(10, 5.50m) }, 55.00m },
        new object[] { new List<Item> { new Item(5, 12.00m) }, 60.00m },
        new object[] { new List<Item>(), 0m }
    };

[Theory]
[MemberData(nameof(PrescriptionTestData))]
public void CalculateTotal_Should_ReturnCorrectAmount(List<Item> items, decimal expected)
{
    var result = _calculator.CalculateTotal(items);
    result.Should().Be(expected);
}
```

#### 3.4.3 ClassData（可重用数据集）

```csharp
public class PatientAgeTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new DateTime(2000, 1, 1), 25 };
        yield return new object[] { new DateTime(1980, 6, 15), 45 };
        yield return new object[] { new DateTime(2020, 12, 31), 5 };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(PatientAgeTestData))]
public void CalculateAge_Should_ReturnCorrectAge(DateTime birthDate, int expectedAge)
{
    var age = _service.CalculateAge(birthDate);
    age.Should().Be(expectedAge);
}
```

### 3.5 异常处理测试

```csharp
[Fact]
public void CreatePatient_Should_ThrowException_When_NameIsEmpty()
{
    var dto = new CreatePatientDto { Name = "" };

    var act = () => _service.CreatePatientAsync(dto);

    act.Should().ThrowAsync<ArgumentException>()
        .WithMessage("*姓名不能为空*");
}

[Fact]
public void GetPatient_Should_ThrowNotFoundException_When_PatientNotExists()
{
    var patientId = Guid.NewGuid();
    _mockRepository
        .Setup(r => r.GetByIdAsync(patientId))
        .ReturnsAsync((Patient)null);

    var act = () => _service.GetPatientAsync(patientId);

    act.Should().ThrowAsync<NotFoundException>()
        .WithMessage($"*{patientId}*");
}
```

---

## 4. 架构要求

### 4.1 测试类结构

#### 4.1.1 标准模板（IDisposable模式）

```csharp
public class UserServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;
    private readonly AppDbContext _context;

    public UserServiceTests()
    {
        // 初始化InMemory数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // 初始化Mock
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();

        // 创建被测试服务
        _service = new UserService(
            _mockRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetUser_Should_ReturnUser_When_UserExists()
    {
        // 测试实现...
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

#### 4.1.2 TestBase基类（可选）

```csharp
public abstract class ServiceTestBase<TService> : IDisposable
    where TService : class
{
    protected readonly AppDbContext Context;
    protected readonly Mock<ILogger<TService>> MockLogger;
    protected TService Service;

    protected ServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);
        MockLogger = new Mock<ILogger<TService>>();
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}

// 使用示例
public class PatientServiceTests : ServiceTestBase<PatientService>
{
    private readonly Mock<IPatientRepository> _mockRepository;

    public PatientServiceTests()
    {
        _mockRepository = new Mock<IPatientRepository>();
        Service = new PatientService(_mockRepository.Object, MockLogger.Object);
    }
}
```

### 4.2 EF Core InMemory测试

#### 4.2.1 数据库配置

```csharp
private AppDbContext CreateDbContext()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .EnableSensitiveDataLogging()  // 开发环境启用
        .Options;

    return new AppDbContext(options);
}
```

**关键点**：
- ✅ 每个测试使用独立数据库（`Guid.NewGuid().ToString()`）
- ✅ 测试结束后Dispose
- ❌ 不要在测试间共享DbContext

#### 4.2.2 测试数据准备

```csharp
[Fact]
public async Task GetActivePatients_Should_ReturnOnlyActivePatients()
{
    // Arrange
    using var context = CreateDbContext();
    
    context.Patients.AddRange(
        new Patient { Id = Guid.NewGuid(), Name = "张三", IsActive = true },
        new Patient { Id = Guid.NewGuid(), Name = "李四", IsActive = false },
        new Patient { Id = Guid.NewGuid(), Name = "王五", IsActive = true }
    );
    await context.SaveChangesAsync();

    var repository = new PatientRepository(context);

    // Act
    var result = await repository.GetActivePatients();

    // Assert
    result.Should().HaveCount(2);
    result.Should().OnlyContain(p => p.IsActive);
}
```

### 4.3 依赖注入测试

**原则**：仅使用构造函数注入，禁止ServiceLocator

```csharp
// ✅ 正确：构造函数注入
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// ❌ 错误：ServiceLocator反模式
public class UserService
{
    public UserService()
    {
        var repository = ServiceLocator.Resolve<IUserRepository>();  // 禁止
    }
}
```

---

## 5. 覆盖率管理

### 5.1 模块覆盖率目标

| 模块 | 当前覆盖率 | 目标覆盖率 | 优先级 | Epic |
|-----|----------|----------|-------|------|
| **Auth** | 15% | 90% | P0 | #1075 |
| **Users** | 5% | 85% | P0 | #1076 |
| **Patients** | 0% | 80% | P0 | #1077 |
| **MedicalCase** | 0% | 85% | P1 | #1078 |
| **Consultation** | 0% | 80% | P1 | #1079 |
| **Prescriptions** | 0% | 85% | P1 | #1080 |
| **Herbs** | 0% | 75% | P2 | #1081 |
| **Formula** | 0% | 75% | P2 | #1082 |

**总体目标**：MVP阶段达到 **80%** 整体覆盖率

### 5.2 CI/CD集成

#### 5.2.1 覆盖率收集

```powershell
# 单个项目
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults

# 整个Server端
dotnet test LYBT.Server.sln `
  --collect:"XPlat Code Coverage" `
  --settings tests/.runsettings
```

#### 5.2.2 覆盖率门禁

```yaml
# .github/workflows/test.yml
- name: Run Tests with Coverage
  run: dotnet test LYBT.Server.sln --collect:"XPlat Code Coverage"

- name: Check Coverage Threshold
  run: |
    $coverage = (Get-Content TestResults/*/coverage.cobertura.xml | Select-String 'line-rate="([\d.]+)"').Matches.Groups[1].Value
    if ([double]$coverage -lt 0.80) {
      Write-Error "Coverage $coverage is below 80% threshold"
      exit 1
    }
```

### 5.3 覆盖率报告

#### 5.3.1 生成HTML报告

```powershell
# 安装ReportGenerator（全局工具）
dotnet tool install -g dotnet-reportgenerator-globaltool

# 生成报告
reportgenerator `
  -reports:**/coverage.cobertura.xml `
  -targetdir:TestResults/CoverageReport `
  -reporttypes:"Html;Badges" `
  -assemblyfilters:"+LYBT.*"

# 打开报告
start TestResults/CoverageReport/index.html
```

#### 5.3.2 集成到VS2022

1. 安装扩展：Fine Code Coverage
2. 运行测试后自动显示覆盖率
3. 查看未覆盖代码：红色高亮

---

## 6. 最佳实践

### 6.1 FIRST原则应用

#### Fast - 快速
```csharp
// ✅ 快速：使用InMemory数据库
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

// ❌ 慢速：使用真实数据库
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)  // 避免
    .Options;
```

#### Independent - 独立
```csharp
// ✅ 独立：每个测试独立数据库
public PatientServiceTests()
{
    _context = CreateDbContext();  // 每次创建新的
}

// ❌ 依赖：共享数据库
private static AppDbContext _sharedContext;  // 避免
```

#### Repeatable - 可重复
```csharp
// ✅ 可重复：固定时间
var fixedDate = new DateTime(2025, 1, 1);
var service = new AgeCalculator(() => fixedDate);

// ❌ 不可重复：依赖当前时间
var service = new AgeCalculator(() => DateTime.Now);  // 避免
```

#### Self-Validating - 自验证
```csharp
// ✅ 自验证：明确断言
result.Should().Be(expectedValue);

// ❌ 需人工验证：打印输出
Console.WriteLine(result);  // 避免
```

#### Timely - 及时
```csharp
// ✅ 及时：先写测试（TDD）
[Fact]
public void NewFeature_Should_Work() { ... }  // 先写

public void NewFeature() { ... }  // 后实现

// ❌ 滞后：代码写完才补测试
```

### 6.2 常见测试模式

#### 6.2.1 边界值测试
```csharp
[Theory]
[InlineData(0)]      // 最小值
[InlineData(1)]      // 最小有效值
[InlineData(100)]    // 正常值
[InlineData(999)]    // 最大有效值
[InlineData(1000)]   // 超出边界
public void ValidateQuantity_Should_HandleBoundaries(int quantity)
{
    // 测试边界行为
}
```

#### 6.2.2 Null检查
```csharp
[Fact]
public void CreatePatient_Should_ThrowException_When_DtoIsNull()
{
    var act = () => _service.CreatePatientAsync(null);
    act.Should().ThrowAsync<ArgumentNullException>();
}
```

#### 6.2.3 并发测试
```csharp
[Fact]
public async Task ConcurrentOperations_Should_NotCauseConflict()
{
    var tasks = Enumerable.Range(1, 10)
        .Select(i => _service.CreatePatientAsync(new CreatePatientDto { Name = $"Patient{i}" }))
        .ToArray();

    var results = await Task.WhenAll(tasks);

    results.Should().HaveCount(10);
    results.Select(r => r.Id).Should().OnlyHaveUniqueItems();
}
```

### 6.3 反模式与避免

#### 6.3.1 ❌ 测试私有方法
```csharp
// ❌ 错误：反射测试私有方法
var method = typeof(PatientService).GetMethod("ValidateAge", BindingFlags.NonPublic | BindingFlags.Instance);
method.Invoke(service, new object[] { 25 });

// ✅ 正确：通过公共接口测试
var result = await service.CreatePatientAsync(dto);  // ValidateAge在内部被调用
result.Should().NotBeNull();
```

#### 6.3.2 ❌ 过度Mock
```csharp
// ❌ 错误：Mock简单值对象
var mockAge = new Mock<int>();  // 不需要Mock

// ✅ 正确：只Mock有行为的依赖
var mockRepository = new Mock<IPatientRepository>();
var mockLogger = new Mock<ILogger>();
```

#### 6.3.3 ❌ 测试间依赖
```csharp
// ❌ 错误：依赖执行顺序
private static Guid _sharedPatientId;

[Fact]
public void Test1_CreatePatient() 
{
    _sharedPatientId = service.Create(...);  // 设置共享状态
}

[Fact]
public void Test2_GetPatient() 
{
    service.Get(_sharedPatientId);  // 依赖Test1
}

// ✅ 正确：每个测试独立
[Fact]
public void GetPatient_Should_ReturnPatient()
{
    var patientId = Guid.NewGuid();  // 独立数据
    // ...
}
```

---

## 7. 故障排查

### 7.1 Desktop测试阻塞问题

**现象**：运行Desktop测试时卡死或超时

**原因**：WPF/Prism需要UI线程初始化

**解决方案**：
```csharp
// 方案1：使用[WpfFact]（推荐）
[WpfFact]
public async Task LoginViewModel_Should_NavigateAfterLogin()
{
    // WPF测试代码
}

// 方案2：手动初始化STA线程
[Fact]
public void DesktopTest()
{
    var thread = new Thread(() =>
    {
        // 测试代码
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
}
```

**临时方案**：仅运行Server端测试
```powershell
dotnet test LYBT.Server.sln -c Release
```

### 7.2 Coverlet符号警告

**现象**：
```
Warning: Unable to find pdb for module 'LYBT.Module.Auth.dll'
```

**原因**：Debug符号文件路径不匹配

**解决方案**：
```xml
<!-- 在测试项目.csproj中添加 -->
<PropertyGroup>
  <DebugType>full</DebugType>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

或者忽略警告（不影响覆盖率收集）：
```powershell
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.SkipAutoProps=true
```

### 7.3 xUnit并行问题

**现象**：并行测试时数据库冲突

**原因**：多个测试共享同一InMemory数据库名称

**解决方案1**：使用Collection禁用并行
```csharp
[Collection("Sequential")]
public class PatientServiceTests { ... }

[Collection("Sequential")]
public class UserServiceTests { ... }

[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }
```

**解决方案2**：使用唯一数据库名（推荐）
```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // 每次唯一
    .Options;
```

### 7.4 CI/CD超时问题

**现象**：GitHub Actions测试超时

**原因**：Desktop测试阻塞或并行度过高

**解决方案**：
```yaml
# .github/workflows/test.yml
- name: Run Server Tests Only
  run: dotnet test LYBT.Server.sln --settings tests/.runsettings
  timeout-minutes: 10  # 设置超时

- name: Limit Parallel Threads
  run: dotnet test -- xUnit.MaxParallelThreads=2
```

---

## 8. 参考资料

### 8.1 内部文档
- [架构测试指南](../architecture/testing/architecture-testing-guide.md)
- [培训材料](testing-training-materials.md)
- [开发规范](standards.md)

### 8.2 外部资源
- [xUnit官方文档](https://xunit.net/)
- [Moq快速入门](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions文档](https://fluentassertions.com/introduction)
- [Coverlet使用指南](https://github.com/coverlet-coverage/coverlet)

### 8.3 相关Issue
- Epic #1078: 架构测试修复与Server端质量保证体系优化
- Epic #1138: 文档SSOT整理与需求完善
- Issue #1143: Phase 2 Day 2 - 测试指南SSOT重构

---

**最后更新**：2025-10-11  
**下一次审查**：2025-11-11（每月审查一次）
