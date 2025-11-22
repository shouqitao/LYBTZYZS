---
name: lybtzyzs-test-generator
description: 为LYBTZYZS项目自动生成xUnit测试用例，支持Repository/Service/ViewModel，遵循AAA模式，自动配置Mock对象（NSubstitute），生成边界条件测试，符合项目测试规范。触发关键词：生成测试、创建测试用例、为XXX写测试、单元测试、generate test、create test、unit test、test generation
---

# LYBTZYZS 测试用例自动生成器

## 核心能力

1. **智能分析目标类** - 解析类结构、依赖关系、公开方法
2. **自动生成Mock配置** - 使用NSubstitute创建依赖Mock
3. **遵循AAA模式** - Arrange（准备）- Act（执行）- Assert（断言）
4. **覆盖多种场景** - 正常流程、异常处理、边界条件、空值处理
5. **支持3种类型** - Repository测试、Service测试、ViewModel测试
6. **符合项目规范** - 中文注释、命名规范、测试组织结构
7. **生成完整测试类** - 包含类声明、字段、Setup、多个测试方法

## 何时使用

- 新增Repository/Service/ViewModel需要补充测试
- 重构后需要验证行为不变
- 提升测试覆盖率（目标：Service ≥75%，ViewModel ≥60%）
- 新成员不熟悉测试编写规范
- 快速生成测试框架，再手动补充细节

## 工作流程

1. 接收目标类路径和类型（Repository/Service/ViewModel）
2. 使用serena分析类结构（依赖、方法、返回类型）
3. 识别需要Mock的依赖（IRepository、ILogger、IMapper等）
4. 生成测试类框架（命名空间、类名、字段）
5. 为每个公开方法生成测试用例（正常/异常/边界）
6. 配置Mock返回值和行为验证
7. 生成完整测试文件并保存到测试项目

## 输入要求

**必需**：
- `target_class_path` - 目标类的文件路径（相对于项目根目录）
  - 示例：`src/Server/Modules/LYBT.Server.MedicalCase/Services/MedicalCaseService.cs`

**可选**：
- `class_type` - 类型（默认：自动识别）
  - `Repository` - Repository层测试
  - `Service` - Service层测试
  - `ViewModel` - ViewModel层测试
  - `auto` - 自动识别（根据类名后缀）
- `methods` - 指定方法列表（默认：所有公开方法）
  - 示例：`["GetByIdAsync", "CreateAsync"]`
- `output_path` - 输出路径（默认：自动推断）
  - 示例：`tests/Server/LYBT.Server.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`

## 输出格式

**最终输出**：
- ✅ **测试文件路径**：如 `tests/.../MedicalCaseServiceTests.cs`
- ✅ **测试方法数量**：如 `12个测试方法`
- ✅ **覆盖场景**：正常流程 + 异常处理 + 边界条件

**测试文件内容示例**（Service测试）：

```csharp
using NSubstitute;
using Xunit;
using Microsoft.Extensions.Logging;
using LYBT.Server.MedicalCase.Services;
using LYBT.Server.MedicalCase.Repositories;
using LYBT.Shared.DTOs;
using AutoMapper;

namespace LYBT.Server.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService单元测试
    /// </summary>
    public class MedicalCaseServiceTests
    {
        private readonly IMedicalCaseRepository _mockRepository;
        private readonly IMapper _mockMapper;
        private readonly ILogger<MedicalCaseService> _mockLogger;
        private readonly MedicalCaseService _service;

        public MedicalCaseServiceTests()
        {
            // Arrange: 创建Mock对象
            _mockRepository = Substitute.For<IMedicalCaseRepository>();
            _mockMapper = Substitute.For<IMapper>();
            _mockLogger = Substitute.For<ILogger<MedicalCaseService>>();

            // Arrange: 创建被测试对象
            _service = new MedicalCaseService(
                _mockRepository,
                _mockMapper,
                _mockLogger
            );
        }

        #region GetByIdAsync Tests

        /// <summary>
        /// 测试：根据有效ID获取医案 - 应返回医案DTO
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ValidId_ReturnsMedicalCaseDto()
        {
            // Arrange: 准备测试数据
            var medicalCaseId = 1;
            var medicalCase = new Domain.MedicalCase
            {
                Id = medicalCaseId,
                PatientId = 100,
                Status = Domain.MedicalCaseStatus.InProgress
            };
            var expectedDto = new MedicalCaseDto
            {
                Id = medicalCaseId,
                PatientId = 100,
                Status = "InProgress"
            };

            _mockRepository.GetByIdAsync(medicalCaseId)
                .Returns(Task.FromResult(medicalCase));
            _mockMapper.Map<MedicalCaseDto>(medicalCase)
                .Returns(expectedDto);

            // Act: 执行被测试方法
            var result = await _service.GetByIdAsync(medicalCaseId);

            // Assert: 验证结果
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result.Id);
            Assert.Equal(expectedDto.PatientId, result.PatientId);
            Assert.Equal(expectedDto.Status, result.Status);

            // Assert: 验证Mock调用
            await _mockRepository.Received(1).GetByIdAsync(medicalCaseId);
            _mockMapper.Received(1).Map<MedicalCaseDto>(medicalCase);
        }

        /// <summary>
        /// 测试：根据不存在的ID获取医案 - 应抛出异常
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_NonExistentId_ThrowsBusinessException()
        {
            // Arrange: 准备测试数据
            var nonExistentId = 999;
            _mockRepository.GetByIdAsync(nonExistentId)
                .Returns(Task.FromResult<Domain.MedicalCase>(null));

            // Act & Assert: 执行并验证异常
            var exception = await Assert.ThrowsAsync<BusinessException>(
                () => _service.GetByIdAsync(nonExistentId)
            );

            Assert.Equal($"医案不存在: {nonExistentId}", exception.Message);
            await _mockRepository.Received(1).GetByIdAsync(nonExistentId);
        }

        /// <summary>
        /// 测试：根据无效ID（0）获取医案 - 应抛出参数异常
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetByIdAsync_InvalidId_ThrowsArgumentException(int invalidId)
        {
            // Act & Assert: 执行并验证异常
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByIdAsync(invalidId)
            );

            Assert.Contains("ID必须大于0", exception.Message);
        }

        #endregion

        #region CreateAsync Tests

        /// <summary>
        /// 测试：创建有效医案 - 应返回创建的医案DTO
        /// </summary>
        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedMedicalCaseDto()
        {
            // Arrange: 准备测试数据
            var createDto = new CreateMedicalCaseDto
            {
                PatientId = 100,
                ChiefComplaint = "头痛"
            };
            var medicalCase = new Domain.MedicalCase
            {
                PatientId = 100,
                ChiefComplaint = "头痛",
                Status = Domain.MedicalCaseStatus.InProgress
            };
            var createdEntity = new Domain.MedicalCase
            {
                Id = 1,
                PatientId = 100,
                ChiefComplaint = "头痛",
                Status = Domain.MedicalCaseStatus.InProgress
            };
            var expectedDto = new MedicalCaseDto
            {
                Id = 1,
                PatientId = 100,
                ChiefComplaint = "头痛"
            };

            _mockMapper.Map<Domain.MedicalCase>(createDto).Returns(medicalCase);
            _mockRepository.CreateAsync(medicalCase).Returns(Task.FromResult(createdEntity));
            _mockMapper.Map<MedicalCaseDto>(createdEntity).Returns(expectedDto);

            // Act: 执行被测试方法
            var result = await _service.CreateAsync(createDto);

            // Assert: 验证结果
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(100, result.PatientId);
            Assert.Equal("头痛", result.ChiefComplaint);

            // Assert: 验证Mock调用
            _mockMapper.Received(1).Map<Domain.MedicalCase>(createDto);
            await _mockRepository.Received(1).CreateAsync(medicalCase);
            _mockMapper.Received(1).Map<MedicalCaseDto>(createdEntity);
        }

        /// <summary>
        /// 测试：创建空DTO - 应抛出参数异常
        /// </summary>
        [Fact]
        public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
        {
            // Act & Assert: 执行并验证异常
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.CreateAsync(null)
            );
        }

        /// <summary>
        /// 测试：创建医案时患者ID无效 - 应抛出参数异常
        /// </summary>
        [Fact]
        public async Task CreateAsync_InvalidPatientId_ThrowsArgumentException()
        {
            // Arrange: 准备测试数据
            var createDto = new CreateMedicalCaseDto
            {
                PatientId = 0, // 无效患者ID
                ChiefComplaint = "头痛"
            };

            // Act & Assert: 执行并验证异常
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(createDto)
            );

            Assert.Contains("患者ID必须大于0", exception.Message);
        }

        #endregion

        #region GetIncompleteCasesByPatientIdAsync Tests

        /// <summary>
        /// 测试：查询患者的未完成医案 - 应返回医案列表
        /// </summary>
        [Fact]
        public async Task GetIncompleteCasesByPatientIdAsync_ValidPatientId_ReturnsCases()
        {
            // Arrange: 准备测试数据
            var patientId = 100;
            var cases = new List<Domain.MedicalCase>
            {
                new Domain.MedicalCase { Id = 1, PatientId = patientId, Status = Domain.MedicalCaseStatus.InProgress },
                new Domain.MedicalCase { Id = 2, PatientId = patientId, Status = Domain.MedicalCaseStatus.InProgress }
            };
            var expectedDtos = new List<MedicalCaseDto>
            {
                new MedicalCaseDto { Id = 1, PatientId = patientId },
                new MedicalCaseDto { Id = 2, PatientId = patientId }
            };

            _mockRepository.GetIncompleteCasesByPatientIdAsync(patientId)
                .Returns(Task.FromResult<IEnumerable<Domain.MedicalCase>>(cases));
            _mockMapper.Map<IEnumerable<MedicalCaseDto>>(cases)
                .Returns(expectedDtos);

            // Act: 执行被测试方法
            var result = await _service.GetIncompleteCasesByPatientIdAsync(patientId);

            // Assert: 验证结果
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            await _mockRepository.Received(1).GetIncompleteCasesByPatientIdAsync(patientId);
        }

        /// <summary>
        /// 测试：查询不存在患者的未完成医案 - 应返回空列表
        /// </summary>
        [Fact]
        public async Task GetIncompleteCasesByPatientIdAsync_NonExistentPatient_ReturnsEmptyList()
        {
            // Arrange: 准备测试数据
            var nonExistentPatientId = 999;
            _mockRepository.GetIncompleteCasesByPatientIdAsync(nonExistentPatientId)
                .Returns(Task.FromResult<IEnumerable<Domain.MedicalCase>>(new List<Domain.MedicalCase>()));
            _mockMapper.Map<IEnumerable<MedicalCaseDto>>(Arg.Any<IEnumerable<Domain.MedicalCase>>())
                .Returns(new List<MedicalCaseDto>());

            // Act: 执行被测试方法
            var result = await _service.GetIncompleteCasesByPatientIdAsync(nonExistentPatientId);

            // Assert: 验证结果
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}
```

## 测试模式详解

### 1. Repository测试模式

**特点**：
- Mock `IApi`接口（Refit自动生成）
- 验证`ApiResponse`处理（Success/Error）
- 测试数据映射（Entity ↔ DTO）

**示例场景**：
- ✅ API返回Success，Repository返回数据
- ✅ API返回Error，Repository抛出异常
- ✅ 网络异常，Repository处理异常

### 2. Service测试模式

**特点**：
- Mock `IRepository`和`IMapper`
- 验证业务逻辑（验证、转换、异常处理）
- 测试事务行为（需要时）

**示例场景**：
- ✅ 正常业务流程，返回正确结果
- ✅ 数据验证失败，抛出`BusinessException`
- ✅ Repository异常，Service正确处理

### 3. ViewModel测试模式

**特点**：
- Mock `IRegionManager`、`IEventAggregator`、`IDialogService`
- 验证Command执行逻辑
- 验证属性通知（PropertyChanged）
- 验证导航行为

**示例场景**：
- ✅ Command执行，IsBusy状态正确
- ✅ Command执行失败，ErrorMessage正确设置
- ✅ 导航参数正确传递

## 技术实现

**使用的MCP工具链**:
1. **mcp__serena__read_file** - 读取目标类源码
2. **mcp__serena__find_symbol** - 查找类定义和公开方法
   ```
   find_symbol: "MedicalCaseService"
   depth: 1  # 获取所有方法
   include_body: false  # 仅需方法签名
   ```
3. **mcp__serena__get_symbols_overview** - 获取类结构概览
4. **mcp__context7** - 查询xUnit/NSubstitute最佳实践
   ```
   topic: "xUnit unit testing best practices"
   topic: "NSubstitute mocking framework"
   ```
5. **Write** - 生成测试文件并保存

**实现逻辑**:
```
1. 路径解析 → 确定目标类路径和输出路径
2. 类分析（serena）→ 提取类名、命名空间、依赖、公开方法
3. 依赖识别 → 识别需要Mock的接口（I开头）
4. 类型推断 → 根据类名后缀判断类型（Service/Repository/ViewModel）
5. 场景规划 → 为每个方法生成3-5个测试场景
   - 正常流程（Happy Path）
   - 异常处理（Exception Handling）
   - 边界条件（Edge Cases）
   - 空值处理（Null Handling）
6. Mock配置生成 → NSubstitute配置（Returns、Throws）
7. 测试代码生成 → AAA模式（Arrange-Act-Assert）
8. 文件保存 → Write tool保存到测试项目
```

## 命名规范

### 测试类命名
```
{ClassName}Tests

示例:
- MedicalCaseServiceTests
- PatientRepositoryTests
- MedicalCaseFlowViewModelTests
```

### 测试方法命名
```
{MethodName}_{Scenario}_{ExpectedBehavior}

示例:
- GetByIdAsync_ValidId_ReturnsMedicalCaseDto
- CreateAsync_NullDto_ThrowsArgumentNullException
- GetIncompleteCasesByPatientIdAsync_NonExistentPatient_ReturnsEmptyList
```

### 测试文件路径
```
tests/{Server|Client}/{ProjectName}.Tests/{Layer}/{ClassName}Tests.cs

示例:
- tests/Server/LYBT.Server.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs
- tests/Client/LYBT.Desktop.MedicalCase.Tests/ViewModels/MedicalCaseFlowViewModelTests.cs
```

## 限制条件

- 仅支持C#代码（.cs文件）
- 需要目标类遵循依赖注入规范（构造函数注入）
- 生成的测试是基础框架，可能需要手动补充细节
- 复杂业务逻辑的断言需要人工验证
- Mock配置基于接口（I开头），具体类无法Mock
- 不支持静态方法测试（静态方法难以Mock）

## 最佳实践

1. **先分析再生成** - 确保目标类结构清晰，依赖明确
2. **渐进式补充** - 生成基础框架，手动补充业务断言
3. **保持测试独立** - 每个测试方法独立，不依赖执行顺序
4. **使用Theory参数化** - 相似场景使用`[Theory]`和`[InlineData]`
5. **验证Mock调用** - 使用`Received()`验证Mock方法被正确调用
6. **清晰的注释** - 每个测试方法添加中文注释说明测试目标
7. **及时运行测试** - 生成后立即运行验证测试通过

## 性能指标

- **类分析**（serena）：<5秒
- **测试代码生成**：<5秒（单个方法<1秒）
- **文件保存**：<1秒
- **端到端完成**：<15秒（单个类，5个方法）

**复杂度影响**:
- 简单类（2-3个方法）：<10秒
- 中等类（5-7个方法）：<15秒
- 复杂类（>10个方法）：<30秒

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-10-22 | 初始版本，支持Repository/Service/ViewModel测试生成 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-10-22
