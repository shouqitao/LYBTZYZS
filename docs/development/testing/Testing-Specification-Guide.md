# 测试规范指南

## 概述
本文档定义了凌隐宝堂中医诊所管理系统的测试规范和最佳实践，旨在提升代码质量并确保系统稳定性。

## 测试覆盖率目标

### 整体目标
- **当前覆盖率**: 0.09%
- **短期目标**: 40%（2025年Q1）
- **中期目标**: 70%（2025年Q2）
- **长期目标**: 85%（2025年Q3）

### 模块优先级
| 模块 | 当前覆盖率 | 目标覆盖率 | 优先级 |
|-----|----------|----------|-------|
| Auth (认证) | 15% | 90% | 高 |
| Users (用户) | 5% | 85% | 高 |
| Patients (患者) | 0% | 80% | 高 |
| Prescriptions (处方) | 0% | 85% | 高 |
| Infrastructure | 8% | 75% | 中 |
| Herbs (药材) | 0% | 70% | 中 |
| Formula (方剂) | 0% | 70% | 中 |
| UI Components | 0% | 60% | 低 |

## 测试架构

### 测试基础设施
所有测试必须继承自 `TestBase` 类，该类提供：
- 依赖注入容器配置
- AutoMapper自动配置
- Mock对象创建工具
- Logger Mock支持

```csharp
public class MyServiceTests : TestBase
{
    private readonly IMyService _service;
    
    public MyServiceTests()
    {
        _service = GetRequiredService<IMyService>();
    }
}
```

### 测试命名规范

#### 测试类命名
- 格式：`{被测试类名}Tests`
- 示例：`JwtAuthenticationServiceTests`

#### 测试方法命名
- 格式：`{方法名}_{场景}_{预期结果}`
- 示例：
  - `GenerateToken_WithValidParameters_ShouldReturnValidToken`
  - `ValidateToken_WithExpiredToken_ShouldReturnNull`
  - `RefreshToken_WithInvalidToken_ShouldThrowException`

### 测试组织结构

```
tests/
├── UnitTests/
│   ├── Server/
│   │   ├── Infrastructure.UnitTests/
│   │   └── Modules/
│   │       ├── Auth.UnitTests/
│   │       ├── Users.UnitTests/
│   │       └── Patients.UnitTests/
│   ├── Client/
│   │   └── Desktop.UnitTests/
│   └── Shared/
│       └── Shared.UnitTests/
├── IntegrationTests/
│   ├── API.IntegrationTests/
│   └── Database.IntegrationTests/
└── TestConfiguration/
    ├── TestBase.cs
    ├── DatabaseTestBase.cs
    └── IntegrationTestBase.cs
```

## 测试类型

### 1. 单元测试 (Unit Tests)
**目标**: 测试单个类或方法的逻辑
**特点**:
- 快速执行（< 100ms）
- 无外部依赖
- 使用Mock对象
- 高覆盖率要求（> 80%）

**示例**:
```csharp
[Fact]
public void CalculateDiscount_WithVIPCustomer_ShouldApply20PercentDiscount()
{
    // Arrange
    var service = new PricingService();
    var originalPrice = 100m;
    var customerType = CustomerType.VIP;
    
    // Act
    var discountedPrice = service.CalculateDiscount(originalPrice, customerType);
    
    // Assert
    discountedPrice.Should().Be(80m);
}
```

### 2. 集成测试 (Integration Tests)
**目标**: 测试多个组件协同工作
**特点**:
- 使用测试数据库（SQLite In-Memory）
- 测试完整的请求流程
- 验证组件间交互

**示例**:
```csharp
[Fact]
public async Task CreatePatient_WithValidData_ShouldPersistToDatabase()
{
    // Arrange
    using var context = CreateTestContext();
    var repository = new PatientRepository(context);
    var patient = CreateTestPatient();
    
    // Act
    await repository.AddAsync(patient);
    await context.SaveChangesAsync();
    
    // Assert
    var savedPatient = await repository.GetByIdAsync(patient.Id);
    savedPatient.Should().BeEquivalentTo(patient);
}
```

### 3. E2E测试 (End-to-End Tests)
**目标**: 测试完整的用户场景
**特点**:
- 模拟真实用户操作
- 覆盖完整业务流程
- 使用WebApplicationFactory

## 测试工具和框架

### 必需依赖
```xml
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
<PackageReference Include="FluentAssertions" Version="6.11.0" />
<PackageReference Include="Moq" Version="4.18.4" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="coverlet.collector" Version="3.2.0" />
```

### 断言库：FluentAssertions
优先使用FluentAssertions进行断言，提供更好的可读性：

```csharp
// 推荐
result.Should().NotBeNull();
result.Count.Should().Be(5);
result.Should().Contain(x => x.Name == "张三");

// 避免
Assert.NotNull(result);
Assert.Equal(5, result.Count);
Assert.Contains(result, x => x.Name == "张三");
```

### Mock框架：Moq
使用Moq创建测试替身：

```csharp
var mockLogger = new Mock<ILogger<MyService>>();
var mockRepository = new Mock<IUserRepository>();

mockRepository
    .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(new User { Id = Guid.NewGuid() });
```

## 测试数据管理

### 测试数据构建器
使用Builder模式创建测试数据：

```csharp
public class PatientBuilder
{
    private Patient _patient = new();
    
    public PatientBuilder WithName(string name)
    {
        _patient.Name = name;
        return this;
    }
    
    public PatientBuilder WithAge(int age)
    {
        _patient.Age = age;
        return this;
    }
    
    public Patient Build() => _patient;
}
```

### 测试数据隔离
- 每个测试使用独立的数据
- 测试完成后清理数据
- 避免测试间依赖

## 测试最佳实践

### AAA模式 (Arrange-Act-Assert)
所有测试必须遵循AAA模式：

```csharp
[Fact]
public void TestMethod()
{
    // Arrange - 准备测试数据和环境
    var input = CreateTestInput();
    var service = CreateService();
    
    // Act - 执行被测试的操作
    var result = service.Process(input);
    
    // Assert - 验证结果
    result.Should().NotBeNull();
}
```

### 测试隔离原则
1. **单一职责**: 每个测试只验证一个行为
2. **独立性**: 测试不依赖执行顺序
3. **可重复性**: 测试结果稳定可重复
4. **自描述性**: 测试名称清晰表达意图

### 异常测试
必须测试异常场景：

```csharp
[Fact]
public void Method_WithInvalidInput_ShouldThrowArgumentException()
{
    // Arrange
    var service = new MyService();
    
    // Act & Assert
    service.Invoking(s => s.Method(null))
        .Should().Throw<ArgumentNullException>()
        .WithMessage("*parameter name*");
}
```

### 异步测试
正确处理异步操作：

```csharp
[Fact]
public async Task GetDataAsync_ShouldReturnData()
{
    // Arrange
    var service = new DataService();
    
    // Act
    var result = await service.GetDataAsync();
    
    // Assert
    result.Should().NotBeEmpty();
}
```

## 代码覆盖率

### 覆盖率收集
使用dotnet-coverage工具：

```bash
# 安装工具
dotnet tool install --global dotnet-coverage

# 运行测试并收集覆盖率
dotnet-coverage collect "dotnet test" -f xml -o coverage.xml

# 生成HTML报告
reportgenerator -reports:coverage.xml -targetdir:coveragereport
```

### 覆盖率标准
| 类型 | 最低要求 | 推荐值 |
|-----|---------|-------|
| 行覆盖率 | 60% | 80% |
| 分支覆盖率 | 50% | 70% |
| 方法覆盖率 | 70% | 85% |

### 排除项
以下可从覆盖率统计中排除：
- Program.cs和Startup.cs
- 迁移文件
- 自动生成的代码
- 纯POCO类（仅包含属性）

使用ExcludeFromCodeCoverage特性：
```csharp
[ExcludeFromCodeCoverage]
public class MigrationFile { }
```

## 持续集成

### GitHub Actions配置
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - run: dotnet restore
    - run: dotnet build --no-restore
    - run: dotnet test --no-build --verbosity normal
      
  coverage:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
    - uses: codecov/codecov-action@v3
```

### 测试门控
Pull Request必须满足：
- 所有测试通过
- 代码覆盖率不降低
- 新代码覆盖率 > 80%

## 测试检查清单

### 新功能开发
- [ ] 编写单元测试覆盖所有公共方法
- [ ] 编写集成测试验证组件交互
- [ ] 测试异常和边界情况
- [ ] 验证异步操作正确性
- [ ] 确保测试独立可重复

### Bug修复
- [ ] 先编写失败的测试重现bug
- [ ] 修复代码使测试通过
- [ ] 添加回归测试防止复发
- [ ] 验证相关功能未受影响

### 代码重构
- [ ] 确保现有测试全部通过
- [ ] 重构后覆盖率不降低
- [ ] 性能测试验证无退化
- [ ] 更新测试以反映新结构

## 常见问题

### AutoMapper配置错误
**问题**: 测试运行时提示AutoMapper配置无效
**解决**: 在TestBase中扫描所有Profile：
```csharp
cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
```

### 数据库上下文Mock
**问题**: 无法Mock DbContext
**解决**: 使用SQLite In-Memory数据库：
```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("DataSource=:memory:")
    .Options;
```

### 异步测试死锁
**问题**: 异步测试挂起
**解决**: 始终使用async/await，避免.Result或.Wait()

## 测试报告模板

### 单元测试报告
```
测试执行时间: 2025-09-26 10:30:00
测试总数: 245
通过: 238
失败: 5
跳过: 2
覆盖率: 42.3%

失败测试详情:
1. UserServiceTests.CreateUser_WithDuplicateEmail_ShouldThrow
   原因: 预期ArgumentException，实际返回null
```

### 性能测试报告
```
API端点性能测试
GET /api/patients: 平均响应时间 45ms, P95 120ms
POST /api/prescriptions: 平均响应时间 80ms, P95 200ms
数据库查询: 平均执行时间 15ms
```

## 更新日志
- 2025-09-26: 创建初始测试规范
- 2025-09-26: 添加测试基础设施要求
- 2025-09-26: 定义覆盖率目标和优先级