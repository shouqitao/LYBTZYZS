# 测试指南

## 核心策略: 集成优先，最小 Mock

```
集成测试 = 真实组件端到端，拦截运行时错误 (优先)
单元测试 = 纯逻辑验证，无 Mock 或最小 Mock (补充)
架构测试 = 编译期强制架构约束 (护栏)
```

**Diamond Model**: 用真实组件测试替代过度 Mock 的单元测试。

---

## 测试项目结构 (5 个项目)

```
tests/
  LYBT.Tests.Unit/                    # Server/Shared 纯逻辑单元测试 (net8.0)
    Entities/                         # 实体模型验证
    Utilities/                        # 工具类/配置/安全
    Infrastructure/                   # BaseService/序列化

  LYBT.Tests.Desktop.Unit/           # Desktop 单元测试 (net8.0-windows)
    Auth/, Formula/, Foundation/      # ViewModel、Service、Security
    Herbs/, Infrastructure/           # 控件、DataSource、Events、Models
    LocalData/, MedicalCase/          # 本地数据、医案状态机
    Patients/, Shell/, Users/         # 患者、Shell服务、用户

  LYBT.Tests.Server.Integration/     # Server端集成测试 (net8.0)
    Auth/, Users/, Patients/          # 认证、用户、患者
    Herbs/, Formulas/                 # 药材、验方
    MedicalCases/, Sync/             # 医案聚合根、数据同步

  LYBT.Tests.Desktop.Integration/    # Desktop端集成测试 (net8.0)
    EndToEnd/                         # ViewModel -> DB 端到端
    LocalMode/                        # 本地模式 DataSource 集成
    Foundation/                       # 基础设施组件

  LYBT.Tests.Architecture/           # 架构约束测试 (net8.0)
    ArchTests                         # 层依赖、命名规范、禁用框架
    AggregateRootArchTests            # 聚合根模式、软删除
```

**平台分离**: Server/Shared 测试用 `net8.0` (跨平台)，Desktop 测试用 `net8.0-windows` (WPF)。

---

## 运行命令

```bash
# 全量测试
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests"

# 分项目运行
dotnet test tests/LYBT.Tests.Unit/
dotnet test tests/LYBT.Tests.Desktop.Unit/
dotnet test tests/LYBT.Tests.Architecture/
dotnet test tests/LYBT.Tests.Server.Integration/
dotnet test tests/LYBT.Tests.Desktop.Integration/

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

### Mock 框架

**统一使用 NSubstitute** (禁止 Moq):

```csharp
// 正确: NSubstitute 语法
var logger = Substitute.For<ILogger<MyService>>();
var service = Substitute.For<IPatientService>();
service.GetByIdAsync(Arg.Any<Guid>()).Returns(Result.Ok(dto));

// 禁止: Moq 语法
// var mock = new Mock<ILogger>(); // 不允许
```

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

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
