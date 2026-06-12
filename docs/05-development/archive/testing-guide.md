# 测试开发指南 (Phase 5)

## 测试架构概览

本项目采用三层测试架构：

```
tests/
├── LYBT.Tests.Desktop/          # 单元测试（net8.0-windows）
│   ├── PureLogic/               # 纯逻辑单元测试
│   └── _Infrastructure/Builders/# 测试数据构建器
├── LYBT.Tests.Integration/      # 集成测试（net8.0-windows）
│   └── Flows/                   # 全链路流程测试
└── LYBT.Tests.Server/           # Server端测试
```

## 测试数据构建器（Builders）

使用 Fluent API 模式创建测试数据：

```csharp
// PatientBuilder 示例
var patient = PatientBuilder.Create()
    .WithName("张三")
    .WithPhoneNumber("13800000001")
    .BuildInputDto();

// 使用预设
var adult = PatientBuilder.AdultMale().BuildInputDto();
var child = PatientBuilder.Child().BuildInputDto();
```

### 可用 Builders

- **PatientBuilder** - 患者数据构建
- **MedicalCaseBuilder** - 医案数据构建
- **FormulaBuilder** - 验方数据构建
- **HerbBuilder** - 药材数据构建

## 测试命名规范

```
Method_Scenario_ExpectedBehavior

示例：
- CreatePatient_DuplicatePhone_ReturnsConflict
- LoginAsync_InvalidPassword_Returns401
```

## AAA 模式

```csharp
[Fact]
public async Task CreatePatient_DuplicatePhone_ReturnsConflict()
{
    // Arrange
    var repo = await CreateRepositoryAsync();
    var existing = await repo.CreateAsync(MakePatient("张三", "13800000001"));

    // Act
    var act = () => repo.CreateAsync(MakePatient("李四", "13800000001"));

    // Assert
    var ex = await act.Should().ThrowAsync<ApiException>();
    ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
}
```

## 避免的反模式

❌ **不要**：
```csharp
// 泛异常断言
await Assert.ThrowsAsync<Exception>(() => repo.CreateAsync(patient));

// 只断言"调用了mock"
await repo.Received(1).CreateAsync(Arg.Any<PatientInputDto>());

// 测试实现细节（字段名）
patient.GetType().GetField("_isDirty").GetValue(patient).Should().BeTrue();
```

✅ **要**：
```csharp
// 精确断言异常类型和状态码
var ex = await Assert.ThrowsAsync<ApiException>(() => repo.CreateAsync(patient));
ex.StatusCode.Should().Be(HttpStatusCode.Conflict);

// 断言业务结果
result.Name.Should().Be("张三");
```

## 提交前检查

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Integration/
```
