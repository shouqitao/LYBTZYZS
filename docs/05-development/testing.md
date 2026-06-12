# 测试指南

## 核心策略: 集成优先，最小 Mock

```
集成测试 = 真实组件端到端，拦截运行时错误 (优先)
单元测试 = 纯逻辑验证，无 Mock 或最小 Mock (补充)
架构测试 = 编译期强制架构约束 (护栏)
```

**Diamond Model**: 用真实组件测试替代过度 Mock 的单元测试。

---

## 测试项目结构 (3 个项目, Testing Trophy 架构)

```
tests/
  LYBT.Tests.Server/                  # Server 端全量测试 (net8.0, 1185 tests)
    Infrastructure/                   # ServerFixture, IntegrationTestBase, Respawn
    Integration/                      # 真实 HTTP + SQL Server 集成测试
      Auth/, Users/, Patients/        # 认证、用户、患者
      Herbs/, Formulas/               # 药材、验方
      MedicalCases/, Sync/            # 医案聚合根、数据同步
    PureLogic/                        # 纯逻辑测试 (Entities, Validators, Utilities)
      Entities/, Shared/, WebAPI/     # 无外部依赖的单元测试

  LYBT.Tests.Desktop/                 # Desktop 端全量测试 (net8.0-windows, ~760 tests)
    _Infrastructure/                  # DesktopFixture (SQLite + 真实 Repository)
    ViewModels/                       # ViewModel 集成测试 (真实 DataSource)
    EndToEnd/                         # 业务流 E2E (Repository -> SQLite)
    LocalData/                        # 本地数据层 DataSource 测试
    PureLogic/                        # 纯逻辑 (状态机、事件、模型)

  LYBT.Tests.Architecture/            # 架构防护测试 (net8.0, 76 tests)
    ServerArchTests                   # 层依赖、命名规范
    CustomControlArchTests            # WPF 控件规范
    AntiMockRuleTests                 # Testing Trophy 防护: Server 零 mock
```

**Testing Trophy 原则**: Server 测试使用真实 SQL Server + Respawn (零 mock)，Desktop 测试使用 SQLite InMemory + 真实 Repository (仅 WPF 边界 mock)。

**平台分离**: Server 测试用 `net8.0` (跨平台)，Desktop 测试用 `net8.0-windows` (WPF)。

---

## 运行命令

```bash
# 全量测试
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"

# 分项目运行
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~PatientModelTests"

# 运行特定命名空间
dotnet test --filter "Namespace~LYBT.Tests.Unit.Entities"
```

---

## 职责划分

### 单元测试职责

| 测试对象 | 职责 | Mock 范围 |
|----------|------|----------|
| Entity Model | 属性验证、默认值、业务规则 | 无依赖 |
| Helper/Utility | 算法正确性、边界值、类型转换 | 无依赖 |
| Validator | 验证规则、错误消息 | 无依赖 |
| BaseService | 权限验证逻辑、角色判断 | NSubstitute (仅 ILogger) |

### 集成测试职责

| 测试对象 | 职责 | 真实组件 |
|----------|------|----------|
| API Endpoint | HTTP 全流程、认证、持久化 | Controller -> Service -> Repository -> DB |
| Data Flow | DI 解析、数据持久化 | DataSource -> DbContext -> SQLite |
| Cross-Module | 模块间协作、聚合根完整性 | MedicalCase -> Consultation -> Prescription |
| Authentication | Token 验证、权限检查 | JWT Handler -> Claims -> DB |

### 归属决策树

```
需要真实 HTTP 请求?  → 是 → 集成测试
需要多组件协作?      → 是 → 集成测试
可以完全隔离 Mock?    → 是 → 单元测试
其他                  → 集成测试
```

---

## 测试编写规范

### AAA 模式

```csharp
[Fact]
public void Patient_Create_WithValidData_ShouldSetDefaults()
{
    // Arrange
    var patient = new Patient();

    // Act
    patient.Name = "张三";
    patient.Gender = Gender.Male;

    // Assert
    Assert.NotEqual(Guid.Empty, patient.Id);
    Assert.Equal("张三", patient.Name);
    Assert.False(patient.IsDeleted);
}
```

### 命名约定

```
单元测试:   {ClassName}Tests.cs
集成测试:   {ClassName}IntegrationTests.cs
方法命名:   {Method}_{Scenario}_{Expected}

示例:
  GetByIdAsync_WithExistingId_ShouldReturnEntity
  Create_WithDuplicateName_ShouldReturnFail
  FullSyncFlow_Upload_ThenDownload_ShouldReturnSameData
```

### Mock 策略 (Testing Trophy)

**Server 测试: 零 mock** -- 所有测试通过真实 HTTP 管线 + SQL Server + Respawn 执行。
**Desktop 测试: 最小 mock** -- 仅限 WPF Runtime 边界接口 (IRegionManager, IDialogService 等)。

```csharp
// Server 测试 -- 真实 HTTP 请求 (零 mock)
var response = await Client.PostAsJsonAsync("/api/v1/patients", dto);
response.StatusCode.Should().Be(HttpStatusCode.OK);

// Desktop 测试 -- 真实 Repository + SQLite (仅 mock WPF 边界)
var fixture = new DesktopFixture(); // SQLite InMemory + 真实 DataSource
var vm = fixture.CreateViewModel<PatientServiceTests>();
```

**NSubstitute 仅在 Desktop 测试中使用** (Server 测试项目通过 AntiMockRuleTests 架构测试禁止引用)。

---

## 避免重复检查清单

- 单元测试不测试 HTTP 状态码
- 集成测试不测试算法细节
- 单元测试不测试 DI 解析
- 集成测试不使用 Mock (用真实组件)
- 边界条件只在单元测试中覆盖
- 端到端流程只在集成测试中覆盖

---

## 覆盖率目标

| 层级 | 单元测试覆盖率 | 集成测试场景 |
|------|---------------|--------------|
| Service | 80%+ | 核心端到端流程 |
| Repository | 70%+ | DI 解析、数据持久化 |
| Helper | 90%+ | - |
| Controller | 20%+ | 全部端点测试 |
| DataSource | 70%+ | CRUD 端到端 |

---

## 常见测试问题

**Q: Desktop 测试在 CI/Linux 上失败**
A: Desktop 测试项目 (`LYBT.Tests.Desktop`) 目标框架为 `net8.0-windows`，仅在 Windows 环境运行。CI 配置应使用 `--filter` 排除或使用 Windows Agent。

**Q: 集成测试数据污染**
A: Server 测试使用 Respawn 在每个测试前重置数据库 (按外键拓扑序 DELETE)。Desktop 测试使用 SQLite InMemory 每测试独立连接。数据隔离由 IntegrationTestBase/DesktopFixture 自动管理。

**Q: 什么时候用 Mock？**
A: Testing Trophy 原则 -- Server 测试零 mock (通过 AntiMockRuleTests 架构测试强制)。Desktop 测试仅 mock WPF Runtime 边界接口 (IRegionManager, IDialogService, IModuleManager 等)。Repository/Service/DbContext 必须使用真实组件。

**Q: 架构测试报 "禁止引用" 错误**
A: 架构测试 (`LYBT.Tests.Architecture`) 强制检查层间依赖方向和 mock 使用限制。AntiMockRuleTests 确保 Server 测试不引用 NSubstitute。修复方向: 用真实集成测试替代 mock 测试。

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-22 | v1.1 | 新增常见测试问题 (FAQ) 章节 |
| 2026-03-04 | v2.0 | Testing Trophy 重构: 5 项目 -> 3 项目, Server 零 mock, Respawn 隔离 |
