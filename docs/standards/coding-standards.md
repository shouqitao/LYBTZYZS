# 通用编码规范

本文档定义了 LYBTZYZS 项目的通用编码规范，所有开发人员都应遵循这些规范。

## 基本原则

1. **可读性优先** - 代码是写给人看的，其次才是给机器执行
2. **保持一致性** - 在整个项目中保持风格一致
3. **简单明了** - 避免过度设计，保持代码简洁
4. **注释适度** - 代码应该自解释，注释用于解释"为什么"而不是"是什么"

## 命名规范

### 通用规则

- 使用有意义的名称，避免缩写
- 避免使用单字母变量名（除了循环变量）
- 不要使用魔法数字，使用常量代替

### C# 命名规范

| 类型 | 命名方式 | 示例 |
|------|----------|------|
| 类/接口 | PascalCase | `PatientService`, `IUserRepository` |
| 方法 | PascalCase | `GetPatientById()`, `SaveRecord()` |
| 属性 | PascalCase | `FirstName`, `IsActive` |
| 私有字段 | _camelCase | `_userService`, `_isInitialized` |
| 参数/局部变量 | camelCase | `patientId`, `recordCount` |
| 常量 | UPPER_CASE | `MAX_RETRY_COUNT`, `DEFAULT_TIMEOUT` |
| 命名空间 | PascalCase | `LYBT.Module.Patients` |

### TypeScript/JavaScript 命名规范

| 类型 | 命名方式 | 示例 |
|------|----------|------|
| 类 | PascalCase | `UserViewModel`, `PatientService` |
| 接口 | PascalCase + I前缀 | `IUserInfo`, `IApiResponse` |
| 函数/方法 | camelCase | `getUserById()`, `handleClick()` |
| 变量 | camelCase | `currentUser`, `isLoading` |
| 常量 | UPPER_CASE | `API_BASE_URL`, `MAX_PAGE_SIZE` |
| 组件 | PascalCase | `LoginView`, `PatientListControl` |

## 代码组织

### 文件组织

```
模块目录/
├── Interfaces/      # 接口定义
├── Services/        # 服务实现
├── Models/          # 数据模型
├── ViewModels/      # 视图模型（前端）
├── Views/           # 视图文件（前端）
└── Tests/           # 单元测试
```

### 类文件结构

```csharp
// 1. Using 语句（按字母顺序）
using System;
using System.Collections.Generic;
using LYBT.Models;

namespace LYBT.Module.Patients
{
    // 2. 类定义
    public class PatientService : IPatientService
    {
        // 3. 私有字段
        private readonly IRepository _repository;
        
        // 4. 构造函数
        public PatientService(IRepository repository)
        {
            _repository = repository;
        }
        
        // 5. 公共属性
        public string ServiceName { get; }
        
        // 6. 公共方法
        public async Task<Patient> GetByIdAsync(Guid id)
        {
            // 实现
        }
        
        // 7. 私有方法
        private void ValidateInput(Patient patient)
        {
            // 实现
        }
    }
}
```

## 编码最佳实践

### 异常处理

```csharp
// 好的做法
try
{
    await _service.ProcessAsync();
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "验证失败");
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "处理失败");
    return StatusCode(500, "内部服务器错误");
}

// 避免
catch
{
    // 不要吞掉异常
}
```

### 异步编程

```csharp
// 好的做法
public async Task<IActionResult> GetPatientAsync(Guid id)
{
    var patient = await _service.GetByIdAsync(id);
    return Ok(patient);
}

// 避免
public Task<IActionResult> GetPatient(Guid id)
{
    var patient = _service.GetByIdAsync(id).Result; // 避免 .Result
    return Task.FromResult(Ok(patient));
}
```

### 依赖注入

```csharp
// 好的做法 - 构造函数注入
public class PatientService
{
    private readonly IRepository _repository;
    private readonly ILogger<PatientService> _logger;
    
    public PatientService(IRepository repository, ILogger<PatientService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### LINQ 使用

```csharp
// 好的做法 - 链式调用，每个操作单独一行
var activePatients = patients
    .Where(p => p.IsActive)
    .OrderBy(p => p.LastName)
    .ThenBy(p => p.FirstName)
    .Take(10)
    .ToList();

// 避免过长的单行 LINQ
var result = patients.Where(p => p.IsActive && p.Age > 18).OrderBy(p => p.LastName).Select(p => new { p.Id, p.Name }).ToList();
```

## 注释规范

### XML 文档注释

```csharp
/// <summary>
/// 根据ID获取患者信息
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>患者信息，如果未找到返回 null</returns>
/// <exception cref="ArgumentException">当 id 为空 GUID 时抛出</exception>
public async Task<Patient?> GetByIdAsync(Guid id)
{
    if (id == Guid.Empty)
        throw new ArgumentException("患者ID不能为空", nameof(id));
        
    return await _repository.GetByIdAsync(id);
}
```

### 代码注释

```csharp
// TODO: 优化查询性能，考虑添加缓存
var patients = await GetAllPatientsAsync();

// HACK: 临时解决方案，等待第三方库修复
Thread.Sleep(100);

// NOTE: 这里使用了特殊的业务规则
if (patient.Age > 65)
{
    // 老年患者享受优惠
    discount = 0.2m;
}
```

## 版本控制

### Git 提交信息格式

```
<type>(<scope>): <subject>

<body>

<footer>
```

类型：
- feat: 新功能
- fix: 修复bug
- docs: 文档更新
- style: 代码格式调整
- refactor: 重构
- test: 测试相关
- chore: 构建过程或辅助工具的变动

示例：
```
feat(patients): 添加患者批量导入功能

- 支持 Excel 文件导入
- 添加数据验证
- 实现进度显示

Closes #123
```

## 代码审查清单

- [ ] 代码符合命名规范
- [ ] 没有硬编码的值
- [ ] 异常处理恰当
- [ ] 日志记录充分
- [ ] 没有注释掉的代码
- [ ] 单元测试覆盖主要逻辑
- [ ] 文档注释完整
- [ ] 没有明显的性能问题

---

记住：好的代码不需要过多的注释，因为它本身就是最好的文档。