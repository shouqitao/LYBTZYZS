# LYBTZYZS 测试规范

## 1. 测试分类标准

### 1.1 单元测试 (Unit Tests)
- **位置**: `tests/UnitTests/`
- **依赖**: 仅依赖被测代码和 Mock 对象
- **数据库**: InMemory 或完全 Mock
- **执行时间**: < 100ms 每个测试
- **命名**: `{ClassName}Tests.cs`

### 1.2 集成测试 (Integration Tests)
- **位置**: `tests/IntegrationTests/`
- **依赖**: 真实服务、真实数据库 (SQLite/SQL Server)
- **执行时间**: < 5s 每个测试
- **命名**: `{ClassName}IntegrationTests.cs`

### 1.3 架构测试 (Architecture Tests)
- **位置**: `tests/Architecture/`
- **目的**: 验证依赖关系、命名规范、层级约束
- **工具**: NetArchTest

### 1.4 性能测试 (Performance Tests)
- **位置**: `tests/PerformanceTests/` 或 `tests/BenchmarkTests/`
- **工具**: BenchmarkDotNet
- **目的**: 基准测试、性能回归检测

---

## 2. 测试命名规范

### 2.1 方法命名格式
```
{MethodName}_{Scenario}_{ExpectedBehavior}
```

**示例**:
```csharp
// Good
GetByIdAsync_WithExistingEntity_ShouldReturnEntity()
GetByIdAsync_WithNonExistentId_ShouldReturnNull()
CreateAsync_WithValidInput_ShouldCreateAndReturnEntity()
CreateAsync_WithInvalidInput_ShouldThrowValidationException()
DeleteAsync_WithReferencedEntity_ShouldReturnFailure()

// Bad
Test1()
GetByIdTest()
CreateAsyncTest()
```

### 2.2 场景分类
| 场景类型 | 前缀示例 |
|----------|----------|
| 正常路径 | `WithValid...`, `WithExisting...` |
| 边界条件 | `WithNull...`, `WithEmpty...`, `WithMaxLength...` |
| 错误路径 | `WithInvalid...`, `WithNonExistent...`, `WithDuplicate...` |
| 状态测试 | `WhenDeleted...`, `WhenDisabled...` |

### 2.3 预期行为描述
| 行为 | 描述 |
|------|------|
| `ShouldReturn...` | 返回特定值 |
| `ShouldThrow...` | 抛出异常 |
| `ShouldNotThrow` | 不抛出异常 |
| `ShouldCreate...` | 创建资源 |
| `ShouldUpdate...` | 更新资源 |
| `ShouldDelete...` | 删除资源 |
| `ShouldLog...` | 记录日志 |

---

## 3. AAA 模式执行标准

每个测试必须严格遵循 **Arrange-Act-Assert** 模式：

```csharp
[Fact]
public async Task GetByIdAsync_WithExistingEntity_ShouldReturnEntity()
{
    // Arrange - 准备测试数据和依赖
    var entityId = Guid.NewGuid();
    var expectedEntity = new Herb
    {
        Id = entityId,
        Name = "黄芪",
        Status = CommonStatus.Enabled
    };
    _dbContext.Herbs.Add(expectedEntity);
    await _dbContext.SaveChangesAsync();

    // Act - 执行被测方法（单一操作）
    var result = await _sut.GetByIdAsync(entityId);

    // Assert - 验证结果
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.Id.Should().Be(entityId);
    result.Data.Name.Should().Be("黄芪");
}
```

### 3.1 Arrange 规则
- 每个测试独立准备数据
- 使用 Builder 模式创建复杂对象
- Mock 设置清晰明确
- 避免共享状态

### 3.2 Act 规则
- **仅执行一个操作**
- 不在 Act 中进行断言
- 捕获异常使用 `var act = () => ...`

### 3.3 Assert 规则
- 使用 FluentAssertions
- 断言数量适中 (3-7 个)
- 先验证关键属性，再验证次要属性
- 负面测试使用 `Should().Throw<>()`

---

## 4. Mock/Stub 使用规范

### 4.1 Mock 框架选择
- **Moq**: 服务层 Mock
- **NSubstitute**: 备选（项目已配置）

### 4.2 Mock 设置原则
```csharp
// Good - 明确设置返回值
_herbServiceMock
    .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(Result<HerbDto>.Success(testDto));

// Good - 验证调用
_herbServiceMock.Verify(
    x => x.GetByIdAsync(expectedId),
    Times.Once);

// Bad - 未设置返回值（返回 default）
_herbServiceMock.Setup(x => x.GetByIdAsync(specificId));
```

### 4.3 测试替身分类
| 类型 | 用途 | 示例 |
|------|------|------|
| **Stub** | 提供固定返回值 | `_repo.Setup(...).Returns(entity)` |
| **Mock** | 验证交互 | `_repo.Verify(..., Times.Once)` |
| **Fake** | 简化实现 | InMemory Database |
| **Spy** | 部分真实 + 部分 Mock | 少用 |

### 4.4 Logger Mock 标准
```csharp
// 创建 Logger Mock
protected Mock<ILogger<T>> CreateLoggerMock<T>() { ... }

// 或使用 NullLogger（推荐用于 internal 类型）
protected ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;
```

---

## 5. 边界条件测试清单

### 5.1 输入验证
| 场景 | 必须测试 |
|------|----------|
| Null 输入 | `ArgumentNullException` 或优雅处理 |
| 空字符串 | 验证行为 |
| 空集合 | 验证行为 |
| 最大长度 | 边界值 |
| 特殊字符 | 中文、emoji、换行符 |

### 5.2 数值边界
| 场景 | 必须测试 |
|------|----------|
| 0 值 | 验证行为 |
| 负数 | 如适用 |
| 最大值 | `int.MaxValue`, `decimal.MaxValue` |
| 精度 | decimal 小数位 |

### 5.3 日期时间边界
| 场景 | 必须测试 |
|------|----------|
| DateTime.MinValue | |
| DateTime.MaxValue | |
| 时区转换 | UTC vs Local |

### 5.4 GUID 边界
| 场景 | 必须测试 |
|------|----------|
| Guid.Empty | |
| 不存在的 GUID | |

### 5.5 集合边界
| 场景 | 必须测试 |
|------|----------|
| 空集合 | |
| 单元素集合 | |
| 大量元素 (100+) | |
| null 元素 | |

---

## 6. 测试覆盖率要求

### 6.1 最低覆盖率
| 层级 | 最低覆盖率 |
|------|------------|
| Service 层 | 80% |
| Repository 层 | 70% |
| Helper/Utility | 90% |
| Controller | 60% |
| ViewModel | 70% |

### 6.2 关键路径
- 所有公开 API 必须有测试
- 所有业务规则必须有测试
- 所有异常处理路径必须有测试

---

## 7. 测试数据管理

### 7.1 TestDataBuilder 模式
```csharp
public class HerbBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "测试药材";
    private CommonStatus _status = CommonStatus.Enabled;

    public HerbBuilder WithId(Guid id) { _id = id; return this; }
    public HerbBuilder WithName(string name) { _name = name; return this; }
    public HerbBuilder WithStatus(CommonStatus status) { _status = status; return this; }

    public Herb Build() => new Herb
    {
        Id = _id,
        Name = _name,
        Status = _status,
        // ... 其他默认值
    };
}
```

### 7.2 测试数据隔离
- 每个测试使用独立的数据库实例 (Guid 命名)
- 不依赖测试执行顺序
- 测试后清理数据

---

## 8. 测试基类使用

### 8.1 单元测试
```csharp
public class MyServiceTests : TestBase
{
    private readonly Mock<IMyRepository> _repoMock;
    private readonly MyService _sut; // System Under Test

    public MyServiceTests()
    {
        _repoMock = CreateMock<IMyRepository>();
        _sut = new MyService(_repoMock.Object, CreateLogger<MyService>());
    }
}
```

### 8.2 数据库测试
```csharp
public class MyRepositoryTests : TestBase
{
    private readonly AppDbContext _dbContext;
    private readonly MyRepository _sut;

    public MyRepositoryTests()
    {
        _dbContext = CreateInMemoryContext();
        _sut = new MyRepository(_dbContext, CreateLogger<MyRepository>());
    }
}
```

### 8.3 集成测试
```csharp
public class MyControllerTests : IntegrationTestBase
{
    // 使用 Client 发送 HTTP 请求
    // 使用真实数据库
}
```

---

## 9. 常见反模式

### 9.1 禁止的做法
- 测试之间共享状态
- 硬编码 Magic Number
- 测试依赖执行顺序
- 过度使用 `Thread.Sleep`
- 在 Assert 中使用 `Debug.WriteLine`
- 一个测试验证多个不相关的行为

### 9.2 警示信号
- 测试名称含有 "And" (可能需要拆分)
- Arrange 部分超过 20 行 (使用 Builder)
- Assert 部分超过 10 个断言 (可能需要拆分)
- Mock 设置过于复杂 (考虑集成测试)

---

## 10. 项目特定规范

### 10.1 Checksum 测试
- 必须测试所有业务字段变更
- 必须测试审计字段排除
- 必须测试 JSON 序列化确定性

### 10.2 同步服务测试
- 必须测试所有 CRUD 操作
- 必须测试冲突处理
- 必须测试引用检查

### 10.3 WPF ViewModel 测试
- 使用 `[STAThread]` 或 `WpfTestCollection`
- 测试命令绑定
- 测试属性变更通知

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*维护者: Claude Code*
