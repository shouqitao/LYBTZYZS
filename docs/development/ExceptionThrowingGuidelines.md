# 异常抛出规范指南

**版本**: 1.0
**更新日期**: 2025-12-20
**OpenSpec**: refactor-exception-handling-system

---

## 概述

本文档定义了LYBTZYZS项目中Service层的异常抛出标准，旨在建立统一、可追踪的异常处理体系。

## 核心原则

### 1. 禁止catch-and-return模式

**Service层禁止使用以下模式：**

```csharp
// ❌ 禁止 - 异常吞没
public async Task<Result<T>> DoSomethingAsync()
{
    try
    {
        // 业务逻辑
        return Result<T>.Success(data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "操作失败");
        return Result<T>.Failure("操作失败");  // 禁止!
    }
}
```

**原因：**
- 异常被吞没后，IExceptionHandler中间件无法捕获
- CorrelationId追踪链断裂
- 无法生成标准的ProblemDetails响应
- 调用方无法区分业务失败和系统异常

### 2. 统一使用AppException体系

**正确的异常抛出方式：**

```csharp
// ✅ 正确 - 直接抛出异常
public async Task<T> DoSomethingAsync(Guid id)
{
    // 资源未找到 → NotFoundException
    var entity = await _repository.GetByIdAsync(id)
        ?? throw ExceptionFactory.NotFound(
            ErrorCode.EntityNotFound,
            $"实体 {id} 不存在");

    // 业务规则验证 → BusinessException
    if (!entity.CanBeModified)
        throw ExceptionFactory.Business(
            ErrorCode.EntityLocked,
            "实体已锁定，无法修改");

    // 正常返回
    return entity;
}
```

## 异常类型选择矩阵

| 场景 | 异常类型 | HTTP状态码 | ErrorCode前缀 |
|------|----------|------------|---------------|
| 资源未找到 | `NotFoundException` | 404 | *_NotFound |
| 业务规则违反 | `BusinessException` | 400 | *_InvalidOperation |
| 数据验证失败 | `ValidationException` | 400 | *_ValidationFailed |
| 并发冲突 | `ConflictException` | 409 | *_ConcurrencyConflict |
| 权限不足 | `UnauthorizedException` | 401 | Auth_* |
| 禁止访问 | `ForbiddenException` | 403 | Auth_Forbidden |
| 瞬态故障 | `TransientException` | 503 | System_Transient |
| 请求限流 | `RateLimitException` | 429 | System_RateLimit |

## ExceptionFactory使用指南

### 创建业务异常

```csharp
// 带ErrorCode的业务异常
throw ExceptionFactory.Business(
    ErrorCode.Patient_IdCardDuplicate,
    "该身份证号已被使用");

// 带额外数据的业务异常
throw ExceptionFactory.Business(
    ErrorCode.MedicalCase_Locked,
    "病历已锁定",
    new { MedicalCaseId = id, LockedAt = entity.LockedAt });
```

### 创建资源未找到异常

```csharp
// 简单形式
throw ExceptionFactory.NotFound(
    ErrorCode.Patient_NotFound,
    $"患者 {id} 不存在");

// 使用null合并运算符
var patient = await _repository.GetByIdAsync(id)
    ?? throw ExceptionFactory.NotFound(ErrorCode.Patient_NotFound, $"患者 {id} 不存在");
```

### 创建并发冲突异常

```csharp
// EF Core并发异常转换
try
{
    await _repository.UpdateAsync(entity);
}
catch (DbUpdateConcurrencyException)
{
    throw ExceptionFactory.Conflict(
        ErrorCode.ConcurrencyConflict,
        "数据已被其他用户修改，请刷新后重试");
}
```

### 创建验证异常

```csharp
// 单个验证错误
throw ExceptionFactory.Validation(
    ErrorCode.ValidationFailed,
    "用户名格式不正确");

// 多个验证错误
var errors = new Dictionary<string, string[]>
{
    ["UserName"] = new[] { "用户名不能为空", "用户名长度不能超过50个字符" },
    ["Email"] = new[] { "邮箱格式不正确" }
};
throw ExceptionFactory.Validation(ErrorCode.ValidationFailed, errors);
```

## 方法签名规范

### 查询方法

```csharp
// 返回单个实体 - 可能抛出NotFoundException
Task<PatientDetailDto> GetByIdAsync(Guid id);

// 返回集合 - 返回空集合而非null
Task<List<PatientListDto>> SearchAsync(string keyword);

// 分页查询 - 返回PagedResult
Task<PagedResult<PatientListDto>> GetPagedAsync(int page, int pageSize);
```

### 命令方法

```csharp
// 创建 - 返回创建的实体ID或完整DTO
Task<Guid> CreateAsync(CreatePatientDto dto);

// 更新 - 无返回值，失败抛异常
Task UpdateAsync(Guid id, UpdatePatientDto dto);

// 删除 - 无返回值，失败抛异常
Task DeleteAsync(Guid id);

// 状态变更 - 无返回值，失败抛异常
Task LockAsync(Guid id);
Task UnlockAsync(Guid id);
```

## 日志记录规范

### Service层日志

```csharp
// 操作开始 - Debug级别
_logger.LogDebug("开始创建患者: {@Dto}", dto);

// 操作成功 - Information级别
_logger.LogInformation("患者创建成功: {PatientId}", patient.Id);

// 业务警告 - Warning级别（不抛异常的情况）
_logger.LogWarning("患者 {PatientId} 已存在相同身份证号", existingId);

// 异常由IExceptionHandler统一记录，Service层不需要catch-log-rethrow
```

## 迁移检查清单

在改造现有Service时，请检查：

- [ ] 移除所有`try-catch { return Result.Failure }`模式
- [ ] 方法签名从`Task<Result<T>>`改为`Task<T>`
- [ ] 使用ExceptionFactory创建异常
- [ ] 确保异常包含正确的ErrorCode
- [ ] 确保异常消息对用户友好
- [ ] 更新对应的单元测试

## 相关文档

- [ErrorCode枚举定义](../../src/Shared/LYBT.Shared.Models/Errors/ErrorCode.cs)
- [ExceptionFactory源码](../../src/Shared/LYBT.Shared.Models/Exceptions/ExceptionFactory.cs)
- [IExceptionHandler实现](../../src/Server/Services/LYBT.WebAPI/ExceptionHandlers/)
