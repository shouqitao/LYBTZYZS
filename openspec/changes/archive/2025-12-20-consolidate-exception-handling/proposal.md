# OpenSpec Proposal: consolidate-exception-handling

## 元数据

| 属性 | 值 |
|------|------|
| 变更ID | consolidate-exception-handling |
| 标题 | 统一异常处理项目整合 |
| 状态 | Draft |
| 优先级 | P1 |
| 影响范围 | High |
| 创建日期 | 2025-12-20 |
| 作者 | Claude Code |

---

## 1. 概述

### 1.1 目标

创建一个**独立的异常处理项目** `LYBT.Core.ExceptionHandling`，将分散在多个项目中的异常处理代码进行整合、迁移和标准化。

### 1.2 背景

当前异常处理代码分散在多个位置：
- `LYBT.Shared.Models/Exceptions/` - 异常类定义 (8个文件)
- `LYBT.Shared.Models/Errors/` - 错误码定义 (2个文件)
- `LYBT.Infrastructure/Errors/` - 错误消息映射 (2个文件)
- `LYBT.WebAPI/ExceptionHandlers/` - IExceptionHandler实现 (2个文件)
- `LYBT.Desktop.Foundation/Exceptions/` - 桌面端异常处理 (4个文件)
- `LYBT.Desktop.Models/Exceptions/` - API调用异常 (1个文件)

此外，Controller层约有**94个catch块**需要移除，Service层仍有**97个catch块**延迟处理。

### 1.3 价值主张

| 收益 | 描述 |
|------|------|
| **代码内聚** | 异常处理代码集中管理，避免分散维护 |
| **统一标准** | RFC 7807 ProblemDetails一致性响应格式 |
| **可维护性** | 新增异常类型只需修改一个项目 |
| **可测试性** | 异常处理逻辑可独立单元测试 |
| **复用性** | Server和Desktop共享核心异常定义 |

---

## 2. 现状分析

### 2.1 已有基础设施

#### 异常类层次结构 (LYBT.Shared.Models/Exceptions/)
```
AppException (基类)
├── ValidationException    - 验证失败 (400)
├── NotFoundException      - 资源未找到 (404)
├── UnauthorizedException  - 未授权 (401)
├── ConflictException      - 资源冲突 (409)
├── BusinessException      - 业务规则违反 (400)
└── ApiException           - API调用异常 (动态)
```

#### 错误码体系 (LYBT.Shared.Models/Errors/ErrorCode.cs)
```
0xxxx - 通用错误 (Unknown, InvalidRequest, NotFound, etc.)
1xxxx - 用户模块 (UserNotFound, UserNameExists, etc.)
2xxxx - 患者模块 (PatientNotFound, PatientIdCardExists, etc.)
3xxxx - 病例模块 (MedicalCaseNotFound, InvalidMedicalCaseState, etc.)
4xxxx - 处方模块 (PrescriptionNotFound, InvalidPrescriptionState, etc.)
5xxxx - 草药模块 (HerbNotFound, HerbNameExists, etc.)
6xxxx - 配方模块 (FormulaNotFound, FormulaNameExists, etc.)
7xxxx - 问诊模块 (ConsultationNotFound, InvalidConsultationState, etc.)
```

#### IExceptionHandler链 (LYBT.WebAPI/ExceptionHandlers/)
- `BusinessExceptionHandler` - 处理AppException及其子类
- `SystemExceptionHandler` - 处理系统异常(EF Core、网络等)

### 2.2 待处理问题

| 问题 | 当前状态 | 位置 |
|------|----------|------|
| Controller层catch块 | ~94个需移除 | 6个Controller |
| Service层catch块 | ~97个延迟处理 | 已标记eliminate-service-catch-return |
| 异常代码分散 | 5+个项目 | Shared.Models, Infrastructure, WebAPI, Desktop |
| Desktop/Server异常不统一 | 各自定义 | Desktop.Foundation vs Shared.Models |

### 2.3 Controller层catch块分布

| Controller | catch块数量 | 备注 |
|------------|-------------|------|
| MedicalCaseController | ~30 | 最多，需优先处理 |
| HerbsController | 17 | |
| FormulasController | 15 | |
| UsersController | 14 | |
| PatientsController | 11 | |
| AuthController | 4 | 部分保留(安全场景) |
| 其他 | 3 | HealthController等 |

---

## 3. 提议方案

### 3.1 新项目结构

创建 `LYBT.Shared.ExceptionHandling` 项目 (位于Shared层，整个项目共用):

```
src/Shared/LYBT.Shared.ExceptionHandling/
├── LYBT.Shared.ExceptionHandling.csproj
├── Exceptions/                    # 异常类定义
│   ├── AppException.cs           # 基类 (迁移自Shared.Models)
│   ├── ValidationException.cs
│   ├── NotFoundException.cs
│   ├── UnauthorizedException.cs
│   ├── ConflictException.cs
│   ├── BusinessException.cs
│   ├── ApiException.cs
│   └── ExceptionFactory.cs       # 异常工厂
├── ErrorCodes/                    # 错误码
│   ├── ErrorCode.cs              # 错误码枚举 (迁移自Shared.Models)
│   ├── ErrorCodeExtensions.cs
│   ├── ErrorCategory.cs          # 新增:错误分类
│   └── ErrorMessages.cs          # 新增:中英文错误消息
├── Handlers/                      # 处理器
│   ├── IExceptionHandler.cs      # 统一接口(Server/Desktop共用)
│   ├── BusinessExceptionHandler.cs    # (迁移自WebAPI)
│   ├── SystemExceptionHandler.cs      # (迁移自WebAPI)
│   └── DesktopExceptionHandler.cs     # (迁移自Desktop.Foundation)
├── Mappers/                       # 映射器
│   ├── IErrorMessageMapper.cs
│   ├── ErrorMessageMapper.cs
│   └── ExceptionMessageMapper.cs  # (迁移自Desktop.Foundation)
├── ProblemDetails/                # RFC 7807支持
│   ├── ProblemDetailsFactory.cs   # 新增:统一创建ProblemDetails
│   └── ProblemDetailsExtensions.cs
└── DependencyInjection/           # DI扩展
    └── ExceptionHandlingServiceCollectionExtensions.cs
```

### 3.2 依赖关系

```
LYBT.Shared.ExceptionHandling (新项目 - Shared层)
    ├── (无外部依赖，纯.NET)
    └── 可选: Microsoft.AspNetCore.Http.Abstractions (仅IExceptionHandler)

LYBT.Shared.Models
    └── 引用 LYBT.Shared.ExceptionHandling (向后兼容)

LYBT.WebAPI
    └── 引用 LYBT.Shared.ExceptionHandling

LYBT.Desktop.Foundation
    └── 引用 LYBT.Shared.ExceptionHandling

LYBT.Infrastructure
    └── 引用 LYBT.Shared.ExceptionHandling
```

### 3.3 迁移策略

采用**直接替换**策略，不保留兼容层:

1. **Phase 1**: 创建新项目 `LYBT.Shared.ExceptionHandling`
2. **Phase 2**: 迁移异常类、错误码、处理器到新项目
3. **Phase 3**: 批量更新所有项目引用和命名空间
4. **Phase 4**: 移除Controller层catch块 (~94个)
5. **Phase 5**: 完成Service层catch块清理 (~97个)
6. **Phase 6**: 删除旧代码文件 (~19个文件)

---

## 4. 实现细节

### 4.1 新增: ProblemDetailsFactory

统一ProblemDetails创建逻辑:

```csharp
public class ProblemDetailsFactory
{
    public static ProblemDetails Create(
        AppException exception,
        string instance,
        string correlationId,
        string traceId)
    {
        return new ProblemDetails
        {
            Status = exception.GetHttpStatusCode(),
            Title = GetTitle(exception),
            Detail = exception.UserMessage ?? exception.Message,
            Instance = instance,
            Type = GetProblemTypeUri(exception.GetHttpStatusCode()),
            Extensions = new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["traceId"] = traceId,
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["errorCode"] = exception.ErrorCode,
                ["errorCodeInt"] = (int?)exception.TypedErrorCode,
                ["errorCategory"] = exception.Category.ToString()
            }
        };
    }
}
```

### 4.2 新增: ErrorMessages (中英文)

```csharp
public static class ErrorMessages
{
    private static readonly Dictionary<ErrorCode, (string Zh, string En)> Messages = new()
    {
        [ErrorCode.Unknown] = ("未知错误", "Unknown error"),
        [ErrorCode.InvalidRequest] = ("请求参数无效", "Invalid request parameters"),
        [ErrorCode.NotFound] = ("资源未找到", "Resource not found"),
        [ErrorCode.ValidationFailed] = ("验证失败", "Validation failed"),
        [ErrorCode.Unauthorized] = ("未授权访问", "Unauthorized access"),
        // ...其他错误码
    };

    public static string Get(ErrorCode code, bool useEnglish = false)
    {
        if (Messages.TryGetValue(code, out var msg))
            return useEnglish ? msg.En : msg.Zh;
        return code.ToString();
    }
}
```

### 4.3 Controller层重构示例

**BEFORE (反模式):**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<PatientDto>> GetById(Guid id)
{
    try
    {
        var result = await _patientService.GetByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取患者失败");
        return StatusCode(500, "服务器内部错误");
    }
}
```

**AFTER (简洁模式):**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<PatientDto>> GetById(Guid id)
{
    // consolidate-exception-handling: 移除冗余try-catch，异常由IExceptionHandler统一处理
    var result = await _patientService.GetByIdAsync(id);
    if (result == null)
        return NotFound();
    return Ok(result);
}
```

---

## 5. 验收标准

### 5.1 功能验收

| 项目 | 验收条件 |
|------|----------|
| 项目创建 | `LYBT.Shared.ExceptionHandling.csproj`编译通过 |
| 代码迁移 | 所有异常类、错误码迁移完成 |
| 旧代码清理 | 19个旧文件全部删除 |
| Controller清理 | 94个catch块移除完成 |
| Service清理 | 97个延迟catch块处理完成 |
| 测试覆盖 | 新项目单元测试≥80% |

### 5.2 质量验收

| 指标 | 目标 |
|------|------|
| 编译警告 | 0 (新代码) |
| 单元测试 | 全部通过 |
| 集成测试 | API响应格式正确 |
| 文档 | README.md + XML注释完整 |

---

## 6. 风险评估

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 破坏现有API响应格式 | 低 | 高 | 保持ProblemDetails格式不变 |
| 循环依赖 | 中 | 中 | 新项目零外部依赖 |
| 迁移遗漏 | 中 | 低 | 自动化扫描验证 |
| Desktop兼容性 | 低 | 中 | 类型转发保证兼容 |

---

## 7. 参考资料

- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core IExceptionHandler](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [.NET Exception Handling Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/)
- 现有OpenSpec: `refactor-exception-handling-system` (56/83 tasks)
- 现有OpenSpec: `eliminate-service-catch-return` (已完成)
