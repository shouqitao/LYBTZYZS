# Design: eliminate-service-catch-return

## 概述

本设计文档定义将106个Service层catch-return反模式重构为`ExecuteAsync<T>()`包装器的技术方案。

## 当前架构分析

### catch块分布

| Service文件 | catch块数量 | 模块 |
|------------|-------------|------|
| UserService.cs | 14 | Users |
| PatientService.cs | 15 | Patients |
| HerbService.cs | 19 | Herbs |
| FormulaService.cs | 17 | Formula |
| MedicalCaseCommandService.cs | 15 | MedicalCase |
| MedicalCaseQueryService.cs | 10 | MedicalCase |
| MedicalCaseStateService.cs | 6 | MedicalCase |
| AuthService.cs | 5 | Auth |
| TokenRevocationService.cs | 3 | Auth |
| SecurityAuditService.cs | 1 | Auth |
| MedicalCaseAuditService.cs | 1 | MedicalCase |
| **合计** | **106** | |

### 当前反模式示例

```csharp
// UserService.cs - 典型的catch-return反模式
public async Task<Result<UserDto>> CreateUserAsync(CreateUserDto dto)
{
    try
    {
        // 验证
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return Result<UserDto>.Failure(validationResult.Errors.Select(e => e.ErrorMessage));

        // 业务逻辑
        var user = _mapper.Map<User>(dto);
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建用户失败: {UserName}", dto.UserName);
        return Result<UserDto>.Failure("创建用户失败");  // 有些地方仍用ex.Message
    }
}
```

## 目标架构

### ExecuteAsync包装器模式

```csharp
// UserService.cs - 目标模式
public Task<Result<UserDto>> CreateUserAsync(CreateUserDto dto)
{
    return ExecuteAsync(async () =>
    {
        // 验证 - 验证失败会抛出ValidationException
        var validationResult = await ValidateAsync(dto, _validator);
        if (!validationResult.IsSuccess)
            throw new ValidationException(validationResult.Errors);

        // 业务逻辑
        var user = _mapper.Map<User>(dto);
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }, "创建用户");
}
```

### BaseService.ExecuteAsync实现

```csharp
// BaseService.cs - 已存在
protected async Task<Result<TResult>> ExecuteAsync<TResult>(
    Func<Task<TResult>> operation,
    string operationName)
{
    try
    {
        var result = await operation();
        return Result<TResult>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "{Operation} 失败", operationName);
        return Result<TResult>.Failure($"{operationName}失败");
    }
}
```

## 重构规则

### 规则1: 直接数据库操作

**Before**:
```csharp
try {
    var entity = await _repo.GetByIdAsync(id);
    return Result<T>.Success(_mapper.Map<TDto>(entity));
} catch (Exception ex) {
    _logger.LogError(ex, "...");
    return Result<T>.Failure("...");
}
```

**After**:
```csharp
return ExecuteAsync(async () => {
    var entity = await _repo.GetByIdAsync(id);
    return _mapper.Map<TDto>(entity);
}, "获取XXX");
```

### 规则2: 带验证的操作

**Before**:
```csharp
try {
    var validation = await _validator.ValidateAsync(dto);
    if (!validation.IsValid)
        return Result<T>.Failure(validation.Errors...);
    // 业务逻辑
} catch ...
```

**After**:
```csharp
return ExecuteAsync(async () => {
    var validation = await ValidateAsync(dto, _validator);
    if (!validation.IsSuccess)
        return validation.Errors.First(); // 或抛出ValidationException
    // 业务逻辑
}, "创建XXX");
```

### 规则3: 带权限检查的操作

**Before**:
```csharp
try {
    var (isAuthorized, error) = ValidateEditPermission(...);
    if (!isAuthorized)
        return Result<T>.Failure(error);
    // 业务逻辑
} catch ...
```

**After**:
```csharp
return ExecuteAsync(async () => {
    var (isAuthorized, error) = ValidateEditPermission(...);
    if (!isAuthorized)
        throw new UnauthorizedAccessException(error);
    // 业务逻辑
}, "编辑XXX");
```

## Controller层变更

### 删除try-catch块

**Before**:
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateDto dto)
{
    try
    {
        var result = await _service.CreateAsync(dto);
        return result.ToActionResult();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建失败");
        return StatusCode(500, "服务器内部错误");
    }
}
```

**After**:
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateDto dto)
{
    var result = await _service.CreateAsync(dto);
    return result.ToActionResult();
}
// 异常由IExceptionHandler统一处理
```

## 异常处理链

```
Controller (无try-catch)
    ↓
Service.ExecuteAsync (catch → Result.Failure)
    ↓
如果Service未捕获 → IExceptionHandler → ProblemDetails
```

## 测试策略

### 单元测试更新

Mock设置不变，但断言可能需要调整：

```csharp
// 测试异常场景
_repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .ThrowsAsync(new Exception("DB error"));

var result = await _service.GetByIdAsync(id);

result.IsSuccess.Should().BeFalse();
result.ErrorMessage.Should().Contain("获取"); // 操作名称
```

## 实施顺序

1. **Auth模块** (9个catch块) - 影响范围小，适合先验证模式
2. **Users模块** (14个catch块) - 核心模块之一
3. **Patients模块** (15个catch块) - 核心模块之一
4. **Herbs模块** (19个catch块) - 业务逻辑较复杂
5. **Formula模块** (17个catch块) - 业务逻辑较复杂
6. **MedicalCase模块** (32个catch块) - 最大模块，最后处理

## 风险缓解

| 风险 | 缓解措施 |
|------|----------|
| 回归bug | 每个模块完成后立即运行测试 |
| 异常信息丢失 | ExecuteAsync已记录完整异常到日志 |
| 性能影响 | 无运行时开销，仅代码结构变更 |
