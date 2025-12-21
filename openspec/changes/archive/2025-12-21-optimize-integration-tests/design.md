# Design: 集成测试优化

## Context

LYBTZYZS项目当前处于Pre-Release Stabilization阶段，需要确保测试覆盖完整性。
集成测试使用xUnit + FluentAssertions框架，通过WebApplicationFactory进行API测试。

## Goals / Non-Goals

### Goals
- 实现所有Controller的API集成测试覆盖
- 统一测试基础设施和模式
- 清理遗留代码和备份文件

### Non-Goals
- 不涉及单元测试范围
- 不涉及性能测试优化
- 不引入新的测试框架

## Decisions

### Decision 1: 测试基类选择

**选择**: 所有WebAPI集成测试继承`IntegrationTestBase`

**原因**:
- 提供统一的WebApplicationFactory配置
- 内置JWT Token生成和认证
- 统一的数据库种子数据管理
- 统一的资源清理机制

### Decision 2: 数据库策略

**选择**: 使用InMemory数据库进行隔离测试

**原因**:
- 测试速度快
- 每个测试实例独立数据库
- 无需外部数据库依赖
- FormulaServiceIntegrationTests使用真实SQL Server是特例(需验证真实药材库)

### Decision 3: 测试命名规范

**选择**: `[Method]_[Scenario]_Should[Expected]`

**示例**:
- `Create_WithValidData_ShouldCreateHerb`
- `GetById_WithNonExistingId_ShouldReturn404`
- `BatchDelete_WithValidIds_ShouldDeleteMultiple`

### Decision 4: 测试文件命名

**选择**: `[Controller]IntegrationTests.cs`

**示例**:
- `FormulasControllerIntegrationTests.cs`
- `HerbsControllerIntegrationTests.cs`
- `HealthCheckIntegrationTests.cs`

## Test Infrastructure Architecture

```
tests/
├── TestConfiguration/
│   ├── IntegrationTestBase.cs       # 测试基类
│   └── appsettings.Test.json        # 测试配置
├── IntegrationTests/
│   ├── WebAPI.IntegrationTests/
│   │   ├── Controllers/
│   │   │   ├── AuthControllerIntegrationTests.cs
│   │   │   ├── FormulasControllerIntegrationTests.cs  [NEW]
│   │   │   ├── HerbsControllerIntegrationTests.cs     [NEW]
│   │   │   ├── HealthCheckIntegrationTests.cs         [NEW]
│   │   │   ├── MedicalCaseControllerIntegrationTests.cs
│   │   │   ├── PatientsControllerIntegrationTests.cs
│   │   │   └── UsersControllerIntegrationTests.cs
│   │   ├── Logging/
│   │   └── Middleware/
│   └── Server/Modules/
│       └── LYBT.Module.Formula.IntegrationTests/
```

## Test Template Pattern

```csharp
public class [Entity]ControllerIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/[entity]";

    // 测试数据ID
    private Guid _testEntityId;

    public [Entity]ControllerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    protected override void SeedBasicTestData(AppDbContext context)
    {
        base.SeedBasicTestData(context);
        // 创建测试数据
    }

    #region CRUD Tests
    [Fact] public async Task Create_WithValidData_ShouldCreate() { }
    [Fact] public async Task GetById_WithExistingId_ShouldReturn() { }
    [Fact] public async Task GetList_ShouldReturnPagedResults() { }
    [Fact] public async Task Update_WithValidData_ShouldUpdate() { }
    [Fact] public async Task Delete_WithExistingId_ShouldSoftDelete() { }
    #endregion

    #region Batch Operations
    [Fact] public async Task BatchDelete_WithValidIds_ShouldDeleteMultiple() { }
    #endregion

    #region Authorization Tests
    [Fact] public async Task GetList_WithoutAuth_ShouldReturn401() { }
    #endregion
}
```

## Risks / Trade-offs

### Risk 1: 测试执行时间增加
- **风险**: 新增38+个测试方法，执行时间增加
- **缓解**: 使用InMemory数据库，并行执行测试

### Risk 2: 测试数据干扰
- **风险**: 测试间数据可能相互影响
- **缓解**: 每个测试实例使用独立数据库名

## Open Questions

1. 是否需要为Import/Export测试准备测试Excel文件?
   - 建议: 可以在测试中动态生成测试文件

2. HealthCheck测试是否需要模拟数据库故障场景?
   - 建议: 暂时只测试正常场景，故障场景可后续添加
