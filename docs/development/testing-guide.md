# 测试运行指南

**最后更新**：2025-10-07（Issue #1024）
**适用版本**：.NET 8.0

本文档说明如何在VS2022和命令行中运行测试，包括配置、常见问题和最佳实践。

---

## 快速开始

### VS2022 IDE测试

1. 打开解决方案：
   - `LYBT.Server.sln`（Server端测试）
   - `LYBT.Desktop.sln`（Desktop端测试，当前阻塞）
   - `LYBT.All.sln`（全部测试）

2. 打开测试资源管理器：`测试` → `测试资源管理器`（Ctrl+E, T）

3. 运行测试：
   - 全部运行：点击"运行所有测试"（绿色三角）
   - 单个测试：右键测试→"运行"
   - 调试测试：右键测试→"调试"

4. 查看结果：测试资源管理器显示通过/失败，双击失败测试查看详情

### 命令行测试

```powershell
# 切换到项目根目录
cd D:\source\repos\LYBTZYZS

# Server端测试（推荐）
dotnet test LYBT.Server.sln -c Release

# Desktop端测试（当前阻塞，需修复）
# dotnet test LYBT.Desktop.sln -c Release

# 全部测试
dotnet test LYBT.All.sln -c Release

# 带详细输出
dotnet test LYBT.Server.sln -c Release --logger "console;verbosity=detailed"

# 使用.runsettings配置
dotnet test LYBT.Server.sln -c Release --settings tests/.runsettings
```

---

## 测试项目结构

```
tests/
├── .runsettings                 # VS2022测试配置
├── Directory.Build.props        # 统一测试项目配置
├── UnitTests/                   # 单元测试
│   ├── Modules/                 # 模块测试
│   │   ├── Patients.UnitTests/
│   │   ├── MedicalCase.UnitTests/
│   │   ├── Prescriptions.UnitTests/
│   │   └── ...
│   ├── Desktop/                 # Desktop测试（当前阻塞）
│   ├── Core/                    # 核心组件测试
│   └── Shared/                  # 共享库测试
├── IntegrationTests/            # 集成测试
│   └── WebAPI.IntegrationTests/
└── TestResults/                 # 测试输出（.gitignore）
```

---

## 测试框架与工具

### 核心框架

- **xUnit 2.6.6**：测试框架
- **Moq 4.20.72**：Mock框架
- **FluentAssertions 6.12.2**：流畅断言
- **Coverlet 6.0.2**：代码覆盖率

### 辅助工具

- **Bogus**：假数据生成（Patients模块）
- **EF Core InMemory/Sqlite**：内存数据库测试
- **AutoMapper**：映射测试验证

---

## 配置说明

### .runsettings配置（tests/.runsettings）

```xml
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <MaxCpuCount>0</MaxCpuCount>  <!-- 自动并行 -->
    <TestSessionTimeout>300000</TestSessionTimeout>  <!-- 5分钟超时 -->
  </RunConfiguration>

  <xUnit>
    <ParallelizeTestCollections>true</ParallelizeTestCollections>
    <MaxParallelThreads>-1</MaxParallelThreads>  <!-- 自动 -->
  </xUnit>
</RunSettings>
```

### VS2022配置

1. **测试设置文件**：`测试` → `配置运行设置` → `选择解决方案范围的 runsettings 文件` → 选择 `tests/.runsettings`

2. **并行执行**：默认启用（xUnit配置）

3. **测试输出目录**：`tests/TestResults/`

---

## 编写测试最佳实践

### AAA模式

```csharp
[Fact]
public async Task GetByIdAsync_Should_Return_Success_When_Patient_Exists()
{
    // Arrange - 准备测试数据和Mock
    var id = Guid.NewGuid();
    var entity = new Patient { Id = id, Name = "张三" };
    _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);

    // Act - 执行被测方法
    var result = await _patientService.GetByIdAsync(id);

    // Assert - 验证结果
    result.IsSuccess.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.Id.Should().Be(id);
}
```

### 命名规范

- **测试类**：`{ClassName}Tests`
- **测试方法**：`{MethodName}_Should_{ExpectedBehavior}_When_{Condition}`
- **文件位置**：`tests/UnitTests/{Layer}/{ClassName}Tests.cs`

### Mock使用

```csharp
// Setup返回值
_mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(entity);

// Setup异常
_mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ThrowsAsync(new Exception("Not found"));

// 验证调用
_mockRepository.Verify(x => x.GetByIdAsync(id), Times.Once);
```

---

## 常见问题

### 1. "找不到设置文件 tests/..\.runsettings"

**原因**：Directory.Build.props中引用路径错误

**解决**：已在Issue #1024 Phase 3修复，使用`tests/.runsettings`

### 2. Desktop测试无法编译

**状态**：已知问题（Issue #1024 Phase 2）

**原因**：Desktop测试代码引用了重构后已不存在的命名空间

**解决方案**：需单独Issue修复（预估4-5天）

**临时workaround**：只运行Server测试 `dotnet test LYBT.Server.sln`

### 3. Coverlet符号不匹配警告

**警告示例**：
```
[coverlet] Unable to instrument module
Mono.Cecil.Cil.SymbolsNotMatchingException: Symbols were found but are not matching the assembly
```

**影响**：不影响测试执行，仅影响覆盖率收集

**原因**：PDB符号文件与DLL不同步

**解决**：Clean + Rebuild解决方案

### 4. 测试并行执行冲突

**症状**：测试偶尔失败，重新运行通过

**原因**：共享静态状态或外部资源竞争

**解决**：
- 使用`[Collection("NonParallel")]`禁用特定测试并行
- 避免静态变量
- Mock外部依赖

---

## MVP测试覆盖现状

详见 [test-coverage-mvp-analysis.md](../reports/test-coverage-mvp-analysis.md)

**总体覆盖度**：~62.5%（未达80%目标）

**P0缺口**：
1. 方剂配伍安全验证（十八反/十九畏）
2. 端到端诊疗流程集成测试

**后续Issue**：见MVP分析报告"建议与行动计划"章节

---

## 测试命令参考

### 基础命令

```powershell
# 还原包
dotnet restore LYBT.All.sln

# 编译（Release模式）
dotnet build LYBT.Server.sln -c Release

# 运行测试
dotnet test LYBT.Server.sln -c Release

# 清理
dotnet clean LYBT.All.sln
```

### 高级命令

```powershell
# 仅运行特定测试项目
dotnet test tests/UnitTests/Modules/Patients.UnitTests/LYBT.Module.Patients.Tests.csproj

# 过滤测试（运行包含"Patient"的测试）
dotnet test LYBT.Server.sln --filter "FullyQualifiedName~Patient"

# 生成覆盖率报告
dotnet test LYBT.Server.sln --collect:"XPlat Code Coverage"
# 报告位置：tests/TestResults/{guid}/coverage.cobertura.xml

# 不编译直接测试（编译后）
dotnet test LYBT.Server.sln --no-build --no-restore
```

---

## CI/CD集成

测试在CI/CD中自动运行：

```yaml
# GitHub Actions示例
- name: 运行Server端测试
  run: dotnet test LYBT.Server.sln -c Release --logger "trx"

- name: 发布测试结果
  uses: dorny/test-reporter@v1
  with:
    name: Server Tests
    path: '**/*.trx'
    reporter: dotnet-trx
```

---

## 相关文档

- [MVP测试覆盖分析](../reports/test-coverage-mvp-analysis.md)
- [编码规范](./standards.md)
- [文档规范](./documentation-guidelines.md)
- [最小实践](./minimal-practice.md)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
