# 测试编写规范

## 目录结构

```
LYBT.Tests.Server/
├── _Infrastructure/        # 测试基础设施（fixtures, builders, assertions）
├── Integration/{Module}/   # 集成测试，按模块组织
├── Unit/{Type}/            # 纯逻辑测试（Validators, Entities, etc.）
└── UserJourneys/           # 端到端业务流程

LYBT.Tests.Desktop/
├── _Infrastructure/        # 测试基础设施
├── Integration/{Module}/   # 集成测试（含 LocalWebAPI）
└── Unit/{Module}/          # 纯逻辑测试
```

## 命名规范

### 测试文件

```
{Module}_{Feature}_Tests.cs
```

示例：
- `MedicalCases_CreateEdit_Tests.cs`
- `Formula_BatchOperations_Tests.cs`
- `Auth_Login_Tests.cs`

### 测试方法

```
{Module}_{Scenario}_{ExpectedResult}
```

示例：
- `MedicalCases_Create_AsDoctor_Returns201`
- `Formula_BatchDelete_AsNonOwner_ReturnsForbidden`
- `Auth_AutoLogin_WithInvalidToken_Returns401`

### 历史命名（可保留）

现有 `US_MC_001_CreateCase_WithValidData_ReturnsCreatedCase` 格式可保留，
新增测试应使用简短格式。

## 断言规范

### 必须使用的辅助方法

| 场景 | 方法 | 说明 |
|------|------|------|
| 成功 + 数据 | `await response.ShouldBeSuccessWithDataAsync<T>()` | 接受 200/201 |
| 创建 + 数据 | `await response.ShouldBeCreatedWithDataAsync<T>()` | 仅 201 |
| 成功无数据 | `await response.ShouldBeSuccessAsync()` | 接受 200/201 |
| 分页结果 | `await response.ShouldBePagedResultAsync<T>()` | 200 + 分页 |
| 删除/更新 | `response.ShouldBeNoContent()` | 204 |
| 业务错误 | `await response.ShouldBeBusinessErrorAsync(status, msg)` | 指定状态码 |
| 未授权 | `response.ShouldBeUnauthorized()` | 401 |
| 禁止 | `response.ShouldBeForbidden()` | 403 |
| 未找到 | `await response.ShouldBeNotFoundAsync(msg)` | 404 |
| 验证错误 | `await response.ShouldBeValidationErrorAsync(msg)` | 400 |
| 冲突 | `await response.ShouldBeConflictAsync(msg)` | 409 |

### 禁止的模式

```csharp
// ❌ 不要直接检查状态码（除非非 ApiResponse 格式）
response.StatusCode.Should().Be(HttpStatusCode.OK);

// ✅ 使用辅助方法
await response.ShouldBeSuccessWithDataAsync<MyDto>();
```

### 例外

Auth 端点（Login/Refresh）返回非 ApiResponse 格式的 Token 对象，
可直接检查状态码。

## 测试数据

### Builder 模式

使用 `_Infrastructure/TestDataBuilders/` 中的 Builder：

```csharp
var patient = PatientBuilder.Default()
    .WithName("张三")
    .WithPhone("13800138000")
    .Build();

var medicalCase = MedicalCaseBuilder.Default()
    .ForPatient(patientId)
    .WithDoctor(doctorId)
    .BuildCreate();
```

### 角色登录

```csharp
var doctorClient = await LoginAsDoctorAsync();
var adminClient = await LoginAsAdminAsync();
var receptionistClient = await LoginAsReceptionistAsync();
var sysAdminClient = await LoginAsSysAdminAsync();
```

## 测试分层

| 层级 | 位置 | 特征 | 示例 |
|------|------|------|------|
| 集成测试 | `Integration/` | 真实 DB，完整 HTTP 管线 | 创建患者→验证 DB |
| 单元测试 | `Unit/` | 纯逻辑，无 DB | 验证器、实体方法、映射器 |
| 端到端 | `UserJourneys/` | 完整业务流程 | 挂号→就诊→处方→打印 |

## 断言 Helper 实现

### ShouldBeSuccessWithDataAsync<T>

```csharp
public static async Task<T> ShouldBeSuccessWithDataAsync<T>(
    this HttpResponseMessage response, string? because = null)
{
    response.StatusCode.Should().BeOneOf(
        new[] { HttpStatusCode.OK, HttpStatusCode.Created },
        because ?? "API call should succeed (200 OK or 201 Created)");
    var body = await response.Content
        .ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
    body.Should().NotBeNull("response body should be deserializable");
    body!.Success.Should().BeTrue(because ?? "API should indicate success");
    body.Data.Should().NotBeNull(because ?? "response should contain data");
    return body.Data!;
}
```

### Builder 模式实现

```csharp
public sealed class PatientBuilder
{
    private string _name = $"测试患者_{Guid.NewGuid():N}"[..12];
    private string _idNumber = GenerateIdNumber();

    public static PatientBuilder Default() => new();

    public PatientBuilder WithName(string name) { _name = name; return this; }
    public PatientBuilder WithIdNumber(string idNumber) { _idNumber = idNumber; return this; }

    public PatientInputDto Build() => new()
    {
        Name = _name,
        Gender = Gender.Male,
        IdNumber = _idNumber
    };
}

// 使用
var patient = PatientBuilder.Default()
    .WithName("张三")
    .Build();
```

### CI 集成

- **Server 测试**: `dotnet test tests/LYBT.Tests.Server/` — 需要 SQL Server 连接
- **Desktop 测试**: `dotnet test tests/LYBT.Tests.Desktop/` — 需要 LocalDB + Windows
- **Architecture 测试**: `dotnet test tests/LYBT.Tests.Architecture/` — 跨平台无外部依赖
- **CI 推荐**: 分开运行 Server 和 Desktop 测试，Desktop 测试使用 Windows Agent

---

## 运行测试

```bash
# 全量
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/

# 按模块
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"

# 按类型
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Unit"
```

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-25 | v1.1 | 补充 ShouldBeSuccessWithDataAsync 实现、Builder 模式示例、CI 集成指导 |
