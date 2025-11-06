# 编码规范详解

> **目标读者**: 所有需要编写代码的开发者
> **更新频率**: 规范变更时同步更新

---

## 🎯 核心质量标准

### 编译质量标准（强制）

```bash
# 所有代码提交前必须通过编译认证
dotnet build LYBT.All.sln -c Release --no-restore

# 预期输出
Build succeeded.
    0 Warning(s)  # ⚠️ 必须0警告
    0 Error(s)    # ⚠️ 必须0错误
```

**警告主动修复策略**：
- ≤20个：直接修复
- >20个：创建Issue跟踪

### 运行时验证标准（强制）

**三层验证标准**：
1. **Level 1 - 编译验证（必需）**: 0 errors, 0 warnings
2. **Level 2 - 静态分析（推荐）**: 代码逻辑正确、符合架构规范
3. **Level 3 - 运行时验证（⚠️ 强制）**: 启动应用、执行操作、验证数据库

**禁止行为**：
- ❌ 只编译通过就认为完成
- ❌ 部分功能可用就关闭Issue

---

## 📝 命名规范

### 类型与公开成员（PascalCase）

```csharp
// ✅ 正确
public class MedicalCaseService { }
public interface IMedicalCaseRepository { }
public enum MedicalCaseStatus { }
public struct Point { }

public string PatientName { get; set; }
public void SaveDraftAsync() { }

// ❌ 错误
public class medicalCaseService { }  // 小写开头
public class medical_case_service { } // 下划线分隔
```

### 私有字段（_camelCase）

```csharp
// ✅ 正确
private readonly IMedicalCaseRepository _repository;
private string _diagnosis;
private int _patientId;

// ❌ 错误
private readonly IMedicalCaseRepository repository;  // 缺少下划线
private string Diagnosis;  // PascalCase
```

### 常量（UPPER_SNAKE_CASE）

```csharp
// ✅ 正确
public const int MAX_RETRY_COUNT = 3;
public const string DEFAULT_STATUS = "Draft";

// ❌ 错误
public const int MaxRetryCount = 3;  // PascalCase
public const string defaultStatus = "Draft";  // camelCase
```

### 异步方法（Async结尾）

```csharp
// ✅ 正确
public async Task SaveDraftAsync()
public async Task<MedicalCase> GetByIdAsync(int id)

// ❌ 错误
public async Task SaveDraft()  // 缺少Async后缀
public Task<MedicalCase> GetById(int id)  // 缺少Async后缀
```

### 禁止使用影响理解的命名（⚠️ 强制）

**规则**：方法名、类名中禁止出现数字序号（Phase1/Phase2/Step1/Step2等）或其他影响代码理解的标记。

```csharp
// ❌ 错误：使用Phase序号命名
private void InitializePhase1_ErrorHandling() { }
private void InitializePhase2_ModuleCoordinator() { }
private async Task InitializePhase3_CoreServicesAsync() { }

// ❌ 错误：使用Step序号命名
public void ExecuteStep1_ValidateInput() { }
public void ExecuteStep2_ProcessData() { }
public void ExecuteStep3_SaveResult() { }

// ✅ 正确：使用描述性的业务名称
private void InitializeErrorHandling() { }
private void InitializeModuleCoordinator() { }
private async Task InitializeCoreServicesAsync() { }

// ✅ 正确：使用清晰的业务流程名称
public void ValidateInput() { }
public void ProcessData() { }
public void SaveResult() { }
```

**理由**：
- Phase/Step 序号需要查看调用代码才能理解执行顺序
- 序号变更时需要大量重命名（如插入新阶段）
- 降低代码可读性和可维护性
- 方法名应该直接表达其业务意图

**替代方案**：
- 使用清晰的业务名称（如 `InitializeErrorHandling` 而非 `InitializePhase1`）
- 使用注释说明执行顺序（如果必要）
- 使用方法调用顺序表达流程（代码即文档）

---

## 🔧 依赖注入规范

### 仅用构造函数注入

```csharp
// ✅ 正确：构造函数注入
public class MedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ILogger<MedicalCaseService> _logger;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        ILogger<MedicalCaseService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// ❌ 错误：禁止使用Container.Resolve
public class MedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;

    public MedicalCaseService()
    {
        _repository = Container.Resolve<IMedicalCaseRepository>();  // ❌ 禁止
    }
}

// ❌ 错误：禁止使用ServiceLocator
public class MedicalCaseService
{
    private IMedicalCaseRepository _repository;

    public void Initialize()
    {
        _repository = ServiceLocator.GetInstance<IMedicalCaseRepository>();  // ❌ 禁止
    }
}
```

---

## ⚡ 异步编程约定

### 涉及I/O必须async/await

```csharp
// ✅ 正确：数据库访问使用async/await
public async Task<MedicalCase> GetByIdAsync(int id)
{
    return await _context.MedicalCases
        .FirstOrDefaultAsync(mc => mc.Id == id);
}

// ✅ 正确：HTTP调用使用async/await
public async Task<PatientDto> GetPatientAsync(int id)
{
    var response = await _httpClient.GetAsync($"/api/patients/{id}");
    return await response.Content.ReadAsAsync<PatientDto>();
}

// ❌ 错误：数据库访问使用同步方法
public MedicalCase GetById(int id)
{
    return _context.MedicalCases.FirstOrDefault(mc => mc.Id == id);  // 阻塞
}
```

### 避免阻塞调用

```csharp
// ❌ 错误：.Result会阻塞线程
public MedicalCase GetMedicalCase(int id)
{
    return GetByIdAsync(id).Result;  // 阻塞，可能导致死锁
}

// ❌ 错误：.Wait()会阻塞线程
public void SaveMedicalCase(MedicalCase mc)
{
    SaveAsync(mc).Wait();  // 阻塞
}

// ✅ 正确：使用async/await
public async Task<MedicalCase> GetMedicalCase(int id)
{
    return await GetByIdAsync(id);
}
```

---

## 📁 文件体量规范

### 单文件长度限制

**建议**：单文件 ≤500行

```csharp
// ❌ 错误：单文件超过1000行
// MedicalCaseService.cs (1200行)
public class MedicalCaseService
{
    // ... 1200行代码
}

// ✅ 正确：拆分成多个文件
// MedicalCaseService.cs (300行)
// MedicalCaseService.Draft.cs (200行)
// MedicalCaseService.Validation.cs (150行)
```

### Partial Class拆分

```csharp
// ✅ 使用partial class拆分大文件
// MedicalCaseService.cs
public partial class MedicalCaseService
{
    public async Task<MedicalCase> GetByIdAsync(int id) { }
}

// MedicalCaseService.Draft.cs
public partial class MedicalCaseService
{
    public async Task<bool> SaveDraftAsync(MedicalCase mc) { }
}

// MedicalCaseService.Validation.cs
public partial class MedicalCaseService
{
    private void ValidateInput(MedicalCase mc) { }
}
```

---

## 🧪 测试规范

### AAA模式（Arrange-Act-Assert）

```csharp
[Fact]
public async Task SaveDraftAsync_ShouldSaveConsultation_WhenValidData()
{
    // Arrange（准备）
    var medicalCase = new MedicalCase
    {
        Id = 1,
        Status = MedicalCaseStatus.Draft
    };
    var consultation = new Consultation
    {
        Diagnosis = "测试诊断"
    };

    _mockRepository
        .Setup(r => r.SaveDraftAsync(It.IsAny<MedicalCase>()))
        .ReturnsAsync(true);

    // Act（执行）
    var result = await _service.SaveDraftAsync(medicalCase, consultation);

    // Assert（断言）
    Assert.True(result);
    _mockRepository.Verify(
        r => r.SaveDraftAsync(It.Is<MedicalCase>(
            mc => mc.Consultation.Diagnosis == "测试诊断"
        )),
        Times.Once
    );
}
```

### 测试命名

```csharp
// ✅ 好的命名（方法名_应该做什么_在什么条件下）
[Fact]
public void SaveDraftAsync_ShouldReturnTrue_WhenValidData()

[Fact]
public void SaveDraftAsync_ShouldThrowException_WhenNullInput()

[Fact]
public void SaveDraftAsync_ShouldHandleNull_WhenConsultationIsNull()

// ❌ 不好的命名
[Fact]
public void Test1()

[Fact]
public void SaveTest()

[Fact]
public void TestSaveDraft()
```

---

## 🚫 禁止行为

### 技术黑名单（MVP阶段）

```csharp
// ❌ 禁止：Redis引用
using StackExchange.Redis;

// ❌ 禁止：CQRS模式
public class CreateMedicalCaseCommand : IRequest<int> { }

// ❌ 禁止：MediatR
using MediatR;
private readonly IMediator _mediator;

// ❌ 禁止：Event Sourcing
public class MedicalCaseCreatedEvent : IEvent { }

// ❌ 禁止：过度抽象的工厂
public interface IMedicalCaseServiceFactory
{
    IMedicalCaseService Create(MedicalCaseType type);
}
```

### 过度设计模式

```csharp
// ❌ 禁止：不必要的抽象层
public interface IServiceFactory
{
    TService Create<TService>() where TService : IService;
}

// ❌ 禁止：过度使用设计模式
public class MedicalCaseStrategyFactory
{
    public IMedicalCaseStrategy CreateStrategy(StrategyType type) { }
}

// ✅ 正确：简单直接的实现
public class MedicalCaseService : IMedicalCaseService
{
    public async Task<MedicalCase> GetByIdAsync(int id) { }
}
```

---

## 💬 注释规范

### XML文档注释（公开API）

```csharp
/// <summary>
/// 保存病案草稿
/// </summary>
/// <param name="medicalCase">病案实体</param>
/// <param name="consultation">诊断记录</param>
/// <returns>是否保存成功</returns>
/// <exception cref="ArgumentNullException">当medicalCase为null时抛出</exception>
public async Task<bool> SaveDraftAsync(
    MedicalCase medicalCase,
    Consultation consultation)
{
    // 实现代码
}
```

### 代码内注释（中文）

```csharp
// ✅ 正确：中文注释说明业务逻辑
public async Task<bool> SaveDraftAsync(MedicalCase mc)
{
    // 验证病案状态必须为Draft
    if (mc.Status != MedicalCaseStatus.Draft)
    {
        throw new InvalidOperationException("只能保存状态为Draft的病案");
    }

    // 保存诊断记录和处方数据
    await _consultationRepository.SaveAsync(mc.Consultation);
    await _prescriptionRepository.SaveAsync(mc.Prescription);

    return true;
}

// ❌ 错误：英文注释
public async Task<bool> SaveDraftAsync(MedicalCase mc)
{
    // Validate medical case status must be Draft
    if (mc.Status != MedicalCaseStatus.Draft)
    {
        throw new InvalidOperationException("Invalid status");
    }
}
```

---

## 🔗 相关资源

- [项目基础信息](project-info.md) - 技术栈和版本
- [测试指南](../guides/testing.md) - 测试编写详解
- [架构哲学](../explanation/architecture-philosophy.md) - 架构设计原则
- [MVP哲学](../explanation/mvp-philosophy.md) - MVP约束理念

---

**最后更新**: 2025-10-28（基于Diátaxis框架v1.0）
