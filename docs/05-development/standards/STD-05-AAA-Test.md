# STD-05: AAA 测试规范

## 适用范围

全系统单元测试和集成测试。

## 规范内容

### 测试框架

| 组件 | 用途 |
|------|------|
| xUnit | 测试框架 |
| NSubstitute | Mock 框架 |
| FluentAssertions | 断言库 |

### AAA 模式 (Arrange-Act-Assert)

> **注意**: 以下 NSubstitute 示例仅适用于 Desktop 测试 (`LYBT.Tests.Desktop`)。Server 测试 (`LYBT.Tests.Server`) 使用零 mock 策略——直接使用真实 SQL Server + Respawn，不使用任何 Mock 框架。

```csharp
[Fact]
public async Task SaveAsync_WithValidInput_ReturnsMedicalCase()
{
    // Arrange - 准备测试数据和依赖
    var inputDto = new MedicalCaseInputDto { PatientId = Guid.NewGuid() };
    _repository.CreateAsync(Arg.Any<MedicalCase>())
        .Returns(new MedicalCase { Id = Guid.NewGuid() });

    // Act - 执行被测方法
    var result = await _service.SaveAsync(inputDto, doctorId, isAdmin: false);

    // Assert - 验证结果
    result.Should().NotBeNull();
    result.PatientId.Should().Be(inputDto.PatientId);
}
```

### 命名规则

格式: `MethodName_Scenario_ExpectedBehavior`

| 示例 | 说明 |
|------|------|
| `CreateAsync_WithDuplicateName_ReturnsConflict` | 方法_场景_预期行为 |
| `GetByIdAsync_WithInvalidId_ReturnsNull` | 方法_无效输入_返回空 |
| `CompleteAsync_WithoutTcmDiagnosis_ThrowsValidation` | 方法_缺必填项_抛验证异常 |

### 规则

1. **每个测试方法一个 Assert 主题**: 可以有多个 Assert 语句，但都围绕同一个验证主题
2. **[Theory] 用于参数化**: 相同逻辑不同输入时使用 `[Theory]` + `[InlineData]` 或 `[MemberData]`
3. **测试项目结构**: Unit 测试和 Integration 测试分离，Desktop 测试需要 `net8.0-windows` 目标框架
4. **Mock 边界**: 仅 Mock 直接依赖 (一层)，禁止 Mock 被测类的内部方法
5. **测试数据**: 使用有意义的测试数据，避免 "test1"/"abc" 等无语义值

### 测试项目分布 (Testing Trophy 架构)

| 项目 | 数量 | 说明 |
|------|------|------|
| LYBT.Tests.Server | ~1185 | Server 全量测试 (真实 SQL Server + Respawn, 零 mock) |
| LYBT.Tests.Desktop | ~760 | Desktop 全量测试 (SQLite + 真实 Repository, 最小 WPF mock) |
| LYBT.Tests.Architecture | ~76 | 架构防护 + AntiMockRules |

## 参考

- 测试指南: `docs/05-development/05-testing.md`
- 架构测试: `tests/LYBT.Tests.Architecture/`

---

创建日期: 2026-02-26
更新日期: 2026-03-04 (Testing Trophy 重构: 项目计数更新)
