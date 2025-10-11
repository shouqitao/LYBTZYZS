# 单元测试培训材料

**维护人**：Coder (Claude Code)  
**最后更新**：2025-10-11  
**培训对象**：开发团队新人  
**培训时长**：2小时  
**Issue追踪**：#1143 - Phase 2 Day 2 测试指南SSOT重构

> 📚 **技术规范**：详细的测试编写规范请参考 [testing-guide.md](testing-guide.md)  
> 🏗️ **架构测试**：架构约束请参考 [architecture/testing/architecture-testing-guide.md](../architecture/testing/architecture-testing-guide.md)

---

本文档面向**新人入职培训**，帮助团队成员理解单元测试的价值、掌握TDD方法论，并通过实战练习快速上手。

## 培训目标

通过本次培训，参与者将能够：
- 理解单元测试的重要性和价值主张
- 掌握测试驱动开发（TDD）的Red-Green-Refactor循环
- 学会在日常开发中应用单元测试
- 理解测试金字塔与覆盖率目标
- 通过实战练习巩固测试技能

---

## 第一部分：理论基础 (30分钟)

### 1.1 为什么需要单元测试？

#### 场景演示：没有测试的代码

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

**问题讨论**：
- 这段代码可能出现什么问题？（空值、负数、精度损失）
- 如何确保代码按预期工作？（手动测试？生产验证？）
- 代码修改后如何快速验证？（回归风险？）

#### 单元测试的价值主张

- ✅ **快速反馈** - 秒级验证代码正确性（vs. 手动测试几分钟）
- ✅ **回归保护** - 防止修改破坏现有功能（自动化安全网）
- ✅ **文档作用** - 测试即活文档，展示API用法（比注释更可靠）
- ✅ **设计改进** - 测试驱动更好的代码设计（可测试性=解耦）
- ✅ **重构信心** - 有测试保护的重构更安全（绿灯=安全）

#### ROI分析

| 阶段 | 无测试成本 | 有测试成本 | 收益 |
|------|----------|----------|------|
| **开发阶段** | 快速编码 | +20%时间编写测试 | 早期发现Bug |
| **维护阶段** | 手动回归测试（每次1小时） | 自动测试（每次5分钟） | 节省90%时间 |
| **重构阶段** | 高风险（可能引入Bug） | 低风险（测试保护） | 代码质量提升 |
| **长期收益** | 技术债务累积 | 可持续迭代 | 团队效率提升 |

**结论**：初期投入20%时间，长期节省90%维护成本。

---

### 1.2 测试金字塔

```
        🔺 E2E Tests (5%)
           少量、慢速、昂贵、脆弱

     🔺🔺 Integration Tests (15%)
        中等数量、中等速度、中等成本

  🔺🔺🔺🔺 Unit Tests (80%)
    大量、快速、便宜、稳定
```

#### 为什么单元测试占80%？

| 测试类型 | 执行速度 | 反馈时间 | 维护成本 | 隔离性 |
|---------|---------|---------|---------|-------|
| **单元测试** | <100ms | 秒级 | 低 | 高（Mock依赖） |
| **集成测试** | 1-5s | 分钟级 | 中 | 中（真实依赖） |
| **E2E测试** | 10-60s | 小时级 | 高 | 低（全栈依赖） |

**最佳实践**：
- 单元测试覆盖业务逻辑（Service层、Domain层）
- 集成测试覆盖关键路径（Repository+数据库、API+Controller）
- E2E测试覆盖核心用户场景（登录、开处方、打印）

#### 凌隐宝堂项目现状

**当前覆盖率**（示例）：
```
整体行覆盖率: 62.5%  → 目标: 80% (MVP阶段)
关键模块覆盖率:
- Auth:         15%  → 目标: 90% (P0)
- Users:         5%  → 目标: 85% (P0)
- Patients:      0%  → 目标: 80% (P0)
- MedicalCase:   0%  → 目标: 85% (P1)
- Prescriptions: 0%  → 目标: 85% (P1)
```

**目标和计划**（Epic #1078）：
- 🎯 MVP阶段达到80%整体覆盖率
- 🎯 关键模块优先达到85-90%
- 🎯 新代码必须带测试（CI/CD门禁）

---

## 第二部分：TDD实战演示 (40分钟)

### 2.1 TDD方法论：Red-Green-Refactor

#### 循环步骤

```
1. 🔴 Red   - 写一个失败的测试（定义预期行为）
     ↓
2. 🟢 Green - 写最小实现让测试通过（不考虑优化）
     ↓
3. 🔵 Refactor - 重构代码（保持测试绿色）
     ↓
   重复...
```

#### 为什么要"先写测试"？

- **需求澄清** - 写测试前必须理解需求（测试=可执行规范）
- **接口设计** - 从使用者角度设计API（更好的用户体验）
- **最小实现** - 避免过度设计（YAGNI原则）
- **重构安全** - 绿灯状态重构有保障（测试即安全网）

---

### 2.2 完整TDD演示：PricingService

#### 需求：处方折扣计算

```
业务规则：
- 数量 ≤ 100：无折扣 (0%)
- 数量 > 100 且 ≤ 200：10%折扣
- 数量 > 200：20%折扣
```

#### 步骤1：🔴 Red - 写失败测试

```csharp
public class PricingServiceTests
{
    [Theory]
    [InlineData(50, 0)]      // 无折扣
    [InlineData(100, 0)]     // 边界：无折扣
    [InlineData(150, 0.10)]  // 10%折扣
    [InlineData(200, 0.10)]  // 边界：10%折扣
    [InlineData(250, 0.20)]  // 20%折扣
    public void GetDiscountRate_Should_ReturnCorrectRate_When_QuantityProvided(
        int quantity, decimal expectedRate)
    {
        // Arrange
        var service = new PricingService();

        // Act
        var rate = service.GetDiscountRate(quantity);

        // Assert
        rate.Should().Be(expectedRate);
    }
}
```

**运行测试** → ❌ 编译失败（PricingService不存在）

#### 步骤2：🟢 Green - 最小实现

```csharp
public class PricingService
{
    public decimal GetDiscountRate(int quantity)
    {
        if (quantity <= 100)
            return 0m;
        
        if (quantity <= 200)
            return 0.10m;
        
        return 0.20m;
    }
}
```

**运行测试** → ✅ 全部通过（绿灯）

#### 步骤3：🔵 Refactor - 重构优化

```csharp
public class PricingService
{
    private static readonly Dictionary<int, decimal> DiscountTiers = new()
    {
        { 100, 0m },
        { 200, 0.10m },
        { int.MaxValue, 0.20m }
    };

    public decimal GetDiscountRate(int quantity)
    {
        foreach (var (threshold, rate) in DiscountTiers)
        {
            if (quantity <= threshold)
                return rate;
        }
        
        return 0m; // 默认无折扣
    }
}
```

**运行测试** → ✅ 仍然通过（重构安全）

#### 步骤4：🔴 Red - 添加新需求

```
新需求：VIP用户额外5%折扣
```

```csharp
[Theory]
[InlineData(50, false, 0)]       // 普通用户：无折扣
[InlineData(150, false, 0.10)]   // 普通用户：10%折扣
[InlineData(150, true, 0.15)]    // VIP用户：15%折扣 (10% + 5%)
[InlineData(250, true, 0.25)]    // VIP用户：25%折扣 (20% + 5%)
public void GetDiscountRate_Should_AddVipBonus_When_UserIsVip(
    int quantity, bool isVip, decimal expectedRate)
{
    var service = new PricingService();
    var rate = service.GetDiscountRate(quantity, isVip);
    rate.Should().Be(expectedRate);
}
```

**运行测试** → ❌ 失败（方法签名不匹配）

#### 步骤5：🟢 Green - 实现VIP逻辑

```csharp
public decimal GetDiscountRate(int quantity, bool isVip = false)
{
    var baseRate = GetBaseDiscountRate(quantity);
    var vipBonus = isVip ? 0.05m : 0m;
    
    return baseRate + vipBonus;
}

private decimal GetBaseDiscountRate(int quantity)
{
    if (quantity <= 100) return 0m;
    if (quantity <= 200) return 0.10m;
    return 0.20m;
}
```

**运行测试** → ✅ 全部通过

---

### 2.3 实践要点总结

**TDD核心思想**：
1. **测试先行** - 先定义行为，再实现功能
2. **小步迭代** - 每次只添加一个测试用例
3. **快速反馈** - 秒级验证，立即发现问题
4. **重构自信** - 绿灯状态重构，测试即保障

**常见误区**：
- ❌ 写完所有代码再补测试（失去设计价值）
- ❌ 测试覆盖私有方法（应测试公共行为）
- ❌ 测试依赖执行顺序（应独立可重复）
- ❌ 过度Mock简单依赖（增加维护成本）

---

## 第三部分：常见问题Q&A (20分钟)

### Q1: 如何测试私有方法？

**答**：不要直接测试私有方法。

**理由**：
- 私有方法是实现细节，可能频繁变化
- 私有方法通过公共方法间接测试
- 如果私有方法太复杂，考虑提取为独立类

**示例**：
```csharp
// ❌ 错误：反射测试私有方法
var method = typeof(Service).GetMethod("ValidateAge", BindingFlags.NonPublic);
method.Invoke(service, new object[] { 25 });

// ✅ 正确：通过公共方法测试
var result = service.CreatePatient(new CreatePatientDto { Age = 25 });
result.Should().NotBeNull(); // ValidateAge在内部被调用
```

---

### Q2: Mock太多怎么办？

**答**：考虑重构依赖结构。

**问题诊断**：
- 如果一个类需要Mock 5个以上依赖 → 违反单一职责原则
- 如果Mock配置超过10行 → 可能过度设计

**解决方案**：
```csharp
// ❌ 问题：依赖过多
public class OrderService
{
    public OrderService(
        IProductRepo productRepo,
        IUserRepo userRepo,
        IPaymentGateway paymentGateway,
        IEmailService emailService,
        ILogger logger,
        ICache cache,
        IEventBus eventBus) { ... }
}

// ✅ 改进：聚合依赖或使用Facade模式
public class OrderService
{
    public OrderService(
        IOrderRepository orderRepo,
        IPaymentProcessor paymentProcessor,  // Facade：聚合PaymentGateway+Email
        ILogger logger) { ... }
}
```

---

### Q3: 如何测试DateTime.Now？

**答**：抽象时间依赖。

**问题**：`DateTime.Now`不可控，测试不可重复。

**解决方案**：
```csharp
// ❌ 错误：直接使用DateTime.Now
public class AgeCalculator
{
    public int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Now;  // 不可控
        return today.Year - birthDate.Year;
    }
}

// ✅ 正确：注入时间提供者
public interface ITimeProvider
{
    DateTime Now { get; }
}

public class AgeCalculator
{
    private readonly ITimeProvider _timeProvider;

    public AgeCalculator(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public int CalculateAge(DateTime birthDate)
    {
        var today = _timeProvider.Now;
        return today.Year - birthDate.Year;
    }
}

// 测试中Mock固定时间
_mockTimeProvider.Setup(t => t.Now).Returns(new DateTime(2025, 1, 1));
```

---

### Q4: 集成测试和单元测试如何选择？

**答**：根据测试目标选择。

| 场景 | 推荐测试类型 | 理由 |
|------|------------|------|
| 业务逻辑验证 | 单元测试 | 快速、稳定、易定位问题 |
| 数据库查询优化 | 集成测试 | 需要真实数据库验证性能 |
| API契约验证 | 集成测试 | 验证序列化、路由、中间件 |
| 算法正确性 | 单元测试 | 纯函数，无外部依赖 |
| 事务一致性 | 集成测试 | 需要真实事务管理 |

**原则**：
- 单元测试优先（80%）- 覆盖业务逻辑
- 集成测试补充（15%）- 覆盖关键路径
- E2E测试点睛（5%）- 覆盖核心场景

---

### Q5: 覆盖率100%是目标吗？

**答**：不是。追求有意义的覆盖率。

**合理覆盖率目标**：
- **关键模块**（Auth、支付、处方）：85-90%
- **普通模块**（患者、用户、药材）：75-80%
- **工具类**（Helper、Extension）：60-70%

**不需要测试的代码**：
- ❌ 自动生成代码（Migrations、Scaffold）
- ❌ 简单POCO类（只有属性）
- ❌ 配置类（只有常量）
- ❌ Main/Program.cs（集成测试覆盖）

**有价值的覆盖率**：
- ✅ 所有业务规则（if/switch/循环）
- ✅ 异常处理路径
- ✅ 边界条件（null、空、极值）
- ✅ 并发场景（锁、竞态条件）

---

## 第四部分：实战练习 (30分钟)

### 练习1：UserService测试（15分钟）

#### 需求

实现`UserService.CreateUserAsync`方法，需要：
1. 验证用户名非空
2. 检查用户名是否已存在（调用Repository）
3. 保存用户
4. 记录日志

#### 被测试代码

```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        // TODO: 实现业务逻辑
        throw new NotImplementedException();
    }
}
```

#### 任务

使用TDD方法完成实现：
1. 写测试：验证用户名非空（应抛出ArgumentException）
2. 写测试：验证用户名唯一性（已存在应抛出DuplicateException）
3. 写测试：验证成功创建（Mock Repository返回保存的用户）
4. 写测试：验证日志记录（Verify Logger被调用）

#### 提示

```csharp
// 测试结构提示
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    [Fact]
    public async Task CreateUserAsync_Should_ThrowException_When_UsernameIsEmpty()
    {
        // 你的实现...
    }
}
```

---

### 练习2：PrescriptionCalculator测试（15分钟）

#### 需求

实现`PrescriptionCalculator.CalculateTotal`方法：
```
- 计算所有药材的总价（数量 × 单价）
- 如果总价 > 1000元，打9折
- 结果保留2位小数
```

#### 被测试代码

```csharp
public class PrescriptionCalculator
{
    public decimal CalculateTotal(List<PrescriptionItem> items)
    {
        // TODO: 实现计算逻辑
        throw new NotImplementedException();
    }
}

public class PrescriptionItem
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

#### 任务

使用数据驱动测试（Theory + InlineData）：
1. 测试空列表 → 返回0
2. 测试单个药材（10克 × 5.5元）→ 返回55.00
3. 测试总价1200元 → 返回1080.00（9折）
4. 测试总价999元 → 返回999.00（无折扣）
5. 测试精度（10克 × 3.33元）→ 返回33.30（保留2位）

#### 提示

```csharp
public static IEnumerable<object[]> CalculationTestData =>
    new List<object[]>
    {
        new object[] { new List<PrescriptionItem>(), 0m },
        new object[] { new List<PrescriptionItem> 
        { 
            new() { Quantity = 10, UnitPrice = 5.5m } 
        }, 55.00m },
        // 添加更多测试数据...
    };

[Theory]
[MemberData(nameof(CalculationTestData))]
public void CalculateTotal_Should_ReturnCorrectAmount(
    List<PrescriptionItem> items, decimal expected)
{
    // 你的实现...
}
```

---

## 练习答案与讲解

### 练习1答案

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _service = new UserService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateUserAsync_Should_ThrowException_When_UsernameIsEmpty()
    {
        var dto = new CreateUserDto { Username = "" };
        
        var act = () => _service.CreateUserAsync(dto);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*用户名不能为空*");
    }

    [Fact]
    public async Task CreateUserAsync_Should_ThrowException_When_UsernameExists()
    {
        var dto = new CreateUserDto { Username = "existing" };
        _mockRepository.Setup(r => r.ExistsAsync("existing")).ReturnsAsync(true);
        
        var act = () => _service.CreateUserAsync(dto);
        
        await act.Should().ThrowAsync<DuplicateException>();
    }

    [Fact]
    public async Task CreateUserAsync_Should_ReturnUser_When_ValidDataProvided()
    {
        var dto = new CreateUserDto { Username = "newuser", Name = "张三" };
        var expectedUser = new User { Id = Guid.NewGuid(), Username = "newuser" };
        
        _mockRepository.Setup(r => r.ExistsAsync("newuser")).ReturnsAsync(false);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<User>())).ReturnsAsync(expectedUser);
        
        var result = await _service.CreateUserAsync(dto);
        
        result.Should().NotBeNull();
        result.Username.Should().Be("newuser");
        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), 
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("创建成功")),
                null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

**讲解要点**：
- AAA模式应用（Arrange-Act-Assert）
- Mock Setup配置返回值
- FluentAssertions断言语法
- Logger验证（复杂泛型参数匹配）

---

### 练习2答案

```csharp
public class PrescriptionCalculatorTests
{
    public static IEnumerable<object[]> CalculationTestData =>
        new List<object[]>
        {
            new object[] { new List<PrescriptionItem>(), 0m },
            new object[] { 
                new List<PrescriptionItem> { new() { Quantity = 10, UnitPrice = 5.5m } }, 
                55.00m 
            },
            new object[] { 
                new List<PrescriptionItem> { new() { Quantity = 100, UnitPrice = 12m } }, 
                1080.00m  // 1200 * 0.9
            },
            new object[] { 
                new List<PrescriptionItem> { new() { Quantity = 90, UnitPrice = 11m } }, 
                990.00m   // 无折扣
            },
            new object[] { 
                new List<PrescriptionItem> { new() { Quantity = 10, UnitPrice = 3.33m } }, 
                33.30m    // 精度保留
            },
        };

    [Theory]
    [MemberData(nameof(CalculationTestData))]
    public void CalculateTotal_Should_ReturnCorrectAmount(
        List<PrescriptionItem> items, decimal expected)
    {
        var calculator = new PrescriptionCalculator();
        
        var result = calculator.CalculateTotal(items);
        
        result.Should().Be(expected);
    }
}

// 实现代码
public class PrescriptionCalculator
{
    public decimal CalculateTotal(List<PrescriptionItem> items)
    {
        if (items == null || !items.Any())
            return 0m;

        var subtotal = items.Sum(i => i.Quantity * i.UnitPrice);
        var discount = subtotal > 1000 ? 0.9m : 1.0m;
        
        return Math.Round(subtotal * discount, 2);
    }
}
```

**讲解要点**：
- 数据驱动测试（MemberData）
- 边界值覆盖（0、999、1000、1200）
- 精度处理（Math.Round）
- 空值防御（null检查）

---

## 总结与行动计划

### 关键要点回顾

1. **Why单元测试** - 快速反馈、回归保护、重构信心、长期ROI
2. **测试金字塔** - 80%单元 + 15%集成 + 5%E2E
3. **TDD循环** - Red → Green → Refactor（小步迭代）
4. **最佳实践** - 测试公共行为、独立可重复、有意义的覆盖率

### 后续学习资源

- 📚 [testing-guide.md](testing-guide.md) - 完整技术规范
- 📚 [architecture/testing/architecture-testing-guide.md](../architecture/testing/architecture-testing-guide.md) - 架构测试
- 📚 [xUnit官方文档](https://xunit.net/)
- 📚 [FluentAssertions文档](https://fluentassertions.com/)

### 行动计划（每位开发者）

#### 第1周：熟悉工具
- [ ] 阅读 testing-guide.md
- [ ] 在本地运行所有测试（`dotnet test LYBT.Server.sln`）
- [ ] 为自己负责的一个Service类编写3个单元测试

#### 第2周：实践TDD
- [ ] 选择一个新功能需求
- [ ] 使用TDD方法实现（Red-Green-Refactor）
- [ ] 达到≥80%覆盖率

#### 第3周：代码审查
- [ ] 审查团队成员的测试代码
- [ ] 提出改进建议（遵循FIRST原则）
- [ ] 分享自己的TDD实践经验

#### 持续改进
- [ ] 每次提交前运行测试（git pre-commit hook）
- [ ] 关注CI/CD覆盖率报告
- [ ] 积累测试模式库（常见Mock场景、边界值测试）

---

**培训结束！开始你的测试之旅吧！** 🚀

有问题随时在团队群提问或查阅 [testing-guide.md](testing-guide.md)。
