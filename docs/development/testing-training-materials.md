# 单元测试与覆盖率培训材料

**版本**: 1.0
**创建时间**: 2025-09-21
**培训对象**: 开发团队
**培训时长**: 2小时

## 培训目标

通过本次培训，参与者将能够：
- 理解单元测试的重要性和基本概念
- 掌握xUnit、FluentAssertions、Moq的使用方法
- 学会编写高质量的单元测试
- 理解覆盖率指标并学会解读报告
- 在日常开发中应用TDD或测试优先的开发方式

## 培训大纲

### 第一部分：理论基础 (30分钟)

#### 1.1 为什么需要单元测试？

**现实场景演示**:
```csharp
// 没有测试的代码 - 容易出错且难以维护
public class PrescriptionService
{
    public decimal CalculateTotal(List<PrescriptionItem> items)
    {
        decimal total = 0;
        foreach (var item in items)
        {
            total += item.Quantity * item.UnitPrice;
        }
        return total; // 缺少税费计算？舍入处理？空值检查？
    }
}
```

**问题讨论**:
- 这段代码可能出现什么问题？
- 如何确保代码按预期工作？
- 代码修改后如何快速验证？

**单元测试的价值**:
- ✅ **快速反馈**: 秒级验证代码正确性
- ✅ **回归保护**: 防止修改破坏现有功能
- ✅ **文档作用**: 测试即活文档，展示API用法
- ✅ **设计改进**: 测试驱动更好的代码设计
- ✅ **重构信心**: 有测试保护的重构更安全

#### 1.2 测试金字塔

```
        🔺 E2E Tests (5%)
           少量、慢速、昂贵

     🔺🔺 Integration Tests (15%)
        中等数量、中等速度

  🔺🔺🔺🔺 Unit Tests (80%)
    大量、快速、便宜
```

**讨论**: 为什么单元测试占大部分比例？

#### 1.3 凌隐宝堂项目测试现状

**当前覆盖率数据** (示例):
```
整体行覆盖率: 68.4%  (目标: 90%)
关键模块覆盖率:
- Auth: 85.2%        (目标: 95%)
- Users: 72.8%       (目标: 95%)
- MedicalCase: 45.3% (目标: 95%)
- Prescriptions: 38.1% (目标: 95%)
```

**目标和计划**:
- 🎯 2个月内达到90%整体覆盖率
- 🎯 关键模块优先达到95%
- 🎯 新代码必须带测试

### 第二部分：实践环节 (60分钟)

#### 2.1 环境准备 (10分钟)

**工具安装验证**:
```bash
# 验证.NET SDK
dotnet --version

# 安装覆盖率工具
dotnet tool install -g dotnet-reportgenerator-globaltool

# 创建示例测试项目
dotnet new xunit -n LYBT.Demo.Tests
cd LYBT.Demo.Tests
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

#### 2.2 第一个单元测试 (15分钟)

**被测试类**:
```csharp
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public decimal Divide(decimal a, decimal b)
    {
        if (b == 0)
            throw new DivideByZeroException("除数不能为零");

        return a / b;
    }
}
```

**测试类编写**:
```csharp
public class CalculatorTests
{
    private readonly Calculator _calculator;

    public CalculatorTests()
    {
        _calculator = new Calculator();
    }

    [Fact]
    public void Add_Should_ReturnSum_When_ValidInputsProvided()
    {
        // Arrange
        int a = 5;
        int b = 3;

        // Act
        int result = _calculator.Add(a, b);

        // Assert
        result.Should().Be(8);
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(9, 3, 3)]
    [InlineData(-6, 2, -3)]
    public void Divide_Should_ReturnCorrectResult_When_ValidInputsProvided(
        decimal a, decimal b, decimal expected)
    {
        // Act
        var result = _calculator.Divide(a, b);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Divide_Should_ThrowDivideByZeroException_When_DivisorIsZero()
    {
        // Arrange
        decimal a = 10;
        decimal b = 0;

        // Act & Assert
        var action = () => _calculator.Divide(a, b);
        action.Should().Throw<DivideByZeroException>()
              .WithMessage("除数不能为零");
    }
}
```

**实践任务**: 参与者编写自己的测试类

#### 2.3 Mock和依赖注入 (20分钟)

**业务场景**:
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task<User> SaveAsync(User user);
}

public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.Username))
            return ServiceResult<User>.Failure("用户名不能为空");

        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            var savedUser = await _repository.SaveAsync(user);
            _logger.LogInformation("用户创建成功: {Username}", dto.Username);

            return ServiceResult<User>.Success(savedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败: {Username}", dto.Username);
            return ServiceResult<User>.Failure("系统错误");
        }
    }
}
```

**测试实现**:
```csharp
public class UserServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateUserAsync_Should_ReturnSuccess_When_ValidDataProvided()
    {
        // Arrange
        var dto = new UserCreateDto { Username = "testuser", Name = "测试用户" };
        var expectedUser = new User { Id = Guid.NewGuid(), Username = "testuser" };

        _mockRepository.Setup(x => x.SaveAsync(It.IsAny<User>()))
                      .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Username.Should().Be("testuser");

        // 验证Mock调用
        _mockRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_Should_ReturnFailure_When_UsernameIsEmpty()
    {
        // Arrange
        var dto = new UserCreateDto { Username = "", Name = "测试用户" };

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("用户名不能为空");

        // 验证Repository未被调用
        _mockRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_Should_LogError_When_ExceptionOccurs()
    {
        // Arrange
        var dto = new UserCreateDto { Username = "testuser", Name = "测试用户" };

        _mockRepository.Setup(x => x.SaveAsync(It.IsAny<User>()))
                      .ThrowsAsync(new InvalidOperationException("数据库错误"));

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("系统错误");

        // 验证日志记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("创建用户失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        // 清理资源
    }
}
```

**实践任务**: 参与者为自己的服务类编写Mock测试

#### 2.4 数据库测试 (15分钟)

**Entity Framework内存数据库测试**:
```csharp
public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task SaveAsync_Should_PersistUser_When_ValidUserProvided()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Name = "测试用户",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.SaveAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);

        // 验证数据库状态
        var savedUser = await _context.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnUser_When_UserExists()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "existinguser",
            Name = "现有用户"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("existinguser");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_UserNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

### 第三部分：覆盖率实践 (20分钟)

#### 3.1 运行覆盖率收集

**命令演示**:
```bash
# 运行测试并收集覆盖率
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# 查看生成的文件
ls TestResults/

# 生成HTML报告
reportgenerator \
  "-reports:TestResults/**/coverage.opencover.xml" \
  "-targetdir:TestResults/CoverageReport" \
  "-reporttypes:Html;JsonSummary"

# 打开报告
start TestResults/CoverageReport/index.html
```

#### 3.2 报告解读练习

**现场查看报告**:
1. 整体覆盖率指标解读
2. 模块级别分析
3. 类级别详细查看
4. 具体代码行覆盖情况

**识别改进点**:
- 🔍 哪些类覆盖率最低？
- 🔍 哪些方法完全未覆盖？
- 🔍 哪些分支缺失测试？

#### 3.3 提升覆盖率实践

**现场编写测试**:
选择一个覆盖率低的类，现场补充测试：

```csharp
// 示例：为未覆盖的异常分支添加测试
[Fact]
public void ProcessPayment_Should_ThrowException_When_AmountIsNegative()
{
    // Arrange
    var payment = new Payment { Amount = -10 };

    // Act & Assert
    var action = () => _service.ProcessPayment(payment);
    action.Should().Throw<ArgumentException>()
          .WithMessage("金额不能为负数");
}
```

### 第四部分：最佳实践与持续改进 (10分钟)

#### 4.1 TDD演示

**红-绿-重构循环**:

**步骤1 - 红色（写失败测试）**:
```csharp
[Fact]
public void GetDiscountRate_Should_Return20Percent_When_QuantityOver100()
{
    // Arrange
    var service = new PricingService();

    // Act
    var rate = service.GetDiscountRate(150);

    // Assert
    rate.Should().Be(0.20m);
}
```

**步骤2 - 绿色（最小实现）**:
```csharp
public class PricingService
{
    public decimal GetDiscountRate(int quantity)
    {
        if (quantity > 100)
            return 0.20m;

        return 0m;
    }
}
```

**步骤3 - 重构（优化代码）**:
```csharp
public decimal GetDiscountRate(int quantity)
{
    return quantity switch
    {
        > 100 => 0.20m,
        > 50 => 0.10m,
        > 10 => 0.05m,
        _ => 0m
    };
}
```

#### 4.2 CI集成说明

**自动化流程**:
```
开发者提交代码 → CI运行测试 → 生成覆盖率报告 → 检查阈值 → 通过/失败
```

**门禁要求**:
- ✅ 所有测试必须通过
- ✅ 行覆盖率 ≥ 90%
- ✅ 分支覆盖率 ≥ 80%
- ✅ 关键模块覆盖率 ≥ 95%

#### 4.3 日常开发建议

**开发流程**:
1. 📝 分析需求，设计API
2. 🧪 编写测试用例
3. 💻 实现业务逻辑
4. ✅ 运行测试验证
5. 📊 检查覆盖率
6. 🔄 重构优化

**Code Review检查项**:
- [ ] 新增功能有对应测试？
- [ ] 测试覆盖了正常和异常路径？
- [ ] 测试命名清晰描述了场景？
- [ ] Mock使用合理，验证恰当？
- [ ] 测试独立，无外部依赖？

## 互动环节

### Q&A时间 (15分钟)

**常见问题预设**:

**Q1**: "编写测试会不会拖慢开发进度？"
**A**: 短期看确实需要额外时间，但长期收益巨大：
- 减少手动测试时间
- 降低bug修复成本
- 提高重构信心
- 改善代码设计

**Q2**: "私有方法需要测试吗？"
**A**: 一般不直接测试，而是通过公共接口间接测试。如果私有方法逻辑复杂，考虑提取为独立服务。

**Q3**: "如何处理外部依赖（数据库、网络）？"
**A**: 使用Mock、Stub或测试替身：
- Repository模式 + Mock
- HttpClient + Mock Handler
- 内存数据库测试

**Q4**: "测试运行很慢怎么办？"
**A**: 优化策略：
- 使用内存数据库
- 并行运行测试
- 避免Thread.Sleep
- 合理使用TestContext

### 实践练习 (15分钟)

**任务分配**:
每个参与者选择一个实际的LYBT模块类，现场编写单元测试：

1. **初级任务**: 为简单服务方法编写基础测试
2. **中级任务**: 为复杂业务逻辑编写包含Mock的测试
3. **高级任务**: 为数据访问层编写集成测试

**成果展示**:
- 每人展示1-2个测试用例
- 讨论遇到的问题和解决方案
- 分享最佳实践发现

## 培训总结

### 关键要点回顾

1. **测试价值**: 快速反馈、回归保护、文档作用
2. **AAA模式**: Arrange-Act-Assert结构化测试
3. **Mock技术**: 隔离依赖，专注被测逻辑
4. **覆盖率目标**: 90%整体，95%关键模块
5. **CI集成**: 自动化测试和覆盖率检查

### 后续行动计划

**本周目标**:
- [ ] 每人为负责模块补充至少5个测试用例
- [ ] 熟悉覆盖率报告的查看和解读
- [ ] 在Code Review中关注测试质量

**本月目标**:
- [ ] 负责模块覆盖率提升10%
- [ ] 新增功能必须包含测试
- [ ] 参与测试用例设计讨论

**持续改进**:
- [ ] 每周分享测试技巧
- [ ] 定期回顾覆盖率趋势
- [ ] 不断完善测试工具链

## 学习资源

### 推荐阅读
- 📚 《单元测试的艺术》- Roy Osherove
- 📚 《重构：改善既有代码的设计》- Martin Fowler
- 📚 《测试驱动开发：实战与模式解析》- Kent Beck

### 在线资源
- 🌐 [xUnit官方文档](https://xunit.net/)
- 🌐 [FluentAssertions文档](https://fluentassertions.com/)
- 🌐 [Moq快速入门](https://github.com/moq/moq)
- 🌐 [.NET测试最佳实践](https://docs.microsoft.com/dotnet/core/testing/)

### 内部资源
- 📖 [LYBT单元测试指南](./unit-testing-guide.md)
- 📖 [覆盖率报告解读指南](./coverage-report-guide.md)
- 🔗 [CI覆盖率仪表板](github-actions-link)

---

**培训反馈**: 请在培训结束后填写反馈表，帮助我们改进培训内容和方式。

**技术支持**: 如在实践中遇到问题，请随时联系开发团队或在内部技术群讨论。