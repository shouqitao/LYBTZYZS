# Desktop异常处理策略

## 📋 总体原则

遵循**透明传播、统一处理**的异常处理策略，确保异常信息完整传递且在合适的层级进行处理。

## 🏗️ 分层策略

### 1. Infrastructure层（ApiService）
**职责**：抛出结构化异常

```csharp
// ApiService在HTTP调用失败时抛出ApiException
throw new ApiException(response.StatusCode, content);
```

**原则**：
- ✅ 将HTTP错误转换为`ApiException`
- ✅ 保留完整的错误上下文（状态码、响应内容）
- ❌ 不吞掉异常
- ❌ 不记录日志（由上层处理）

### 2. Repository层
**职责**：透明传播异常

```csharp
public virtual async Task<T> GetByIdAsync(Guid id)
{
    // 直接调用ApiService，异常自然传播
    return await _apiService.GetAsync<T>($"{_endpoint}/{id}");
}
```

**原则**：
- ✅ 让异常自然传播到Service层
- ✅ 只记录关键信息日志（可选）
- ❌ 不使用try-catch包裹API调用
- ❌ 不将异常转换为null或空集合

**反例（错误做法）**：
```csharp
// ❌ 错误：掩盖了API异常
public virtual async Task<T> GetByIdAsync(Guid id)
{
    try
    {
        return await _apiService.GetAsync<T>($"{_endpoint}/{id}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting entity");
        return null; // 问题：调用方无法区分"未找到"和"API错误"
    }
}
```

### 3. Service层（Business Logic）
**职责**：透明传播异常 + 业务逻辑验证

```csharp
public class UserService : IUserService
{
    public Task<UserDto> GetByIdAsync(Guid id)
    {
        // 简单转发调用，异常自然传播
        return _repository.GetByIdAsync(id);
    }

    public async Task<UserDto> CreateAsync(UserDto user)
    {
        // 业务验证在这里
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new ArgumentException("用户名不能为空", nameof(user.UserName));

        _logger.LogInformation($"创建用户: {user.UserName}");

        // Repository调用异常自然传播
        return await _repository.CreateAsync(user);
    }
}
```

**原则**：
- ✅ 添加业务逻辑验证，抛出有意义的异常
- ✅ 记录业务操作日志
- ✅ 让Repository异常自然传播
- ❌ 不在Service层处理HTTP异常
- ❌ 不将异常转换为ServiceResult（由ViewModel层负责）

### 4. ViewModel层（Presentation）
**职责**：使用StandardExceptionHandler统一处理

```csharp
public class UserManagementViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IExceptionHandler _exceptionHandler;

    public async Task LoadUsersAsync()
    {
        var result = await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            var users = await _userService.GetAllAsync();
            // 转换为ViewModel...
            return ServiceResult<List<UserViewModel>>.Success(userViewModels);
        }, nameof(LoadUsersAsync), "加载用户列表");

        if (!result.IsSuccess)
        {
            // 显示错误消息给用户
            await _dialogService.ShowErrorAsync(result.ErrorMessage);
        }
    }
}
```

**原则**：
- ✅ 使用`IExceptionHandler.SafeExecuteAsync`包裹操作
- ✅ 将异常转换为用户友好的消息
- ✅ 显示错误提示给用户
- ❌ 不让异常传播到UI线程外

## 🔧 工具类

### StandardExceptionHandler
提供统一的异常处理和转换功能：

```csharp
public interface IExceptionHandler
{
    // 处理异常并返回ServiceResult
    ServiceResult HandleException(Exception exception, string methodName, string? context = null);
    ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null);

    // 安全执行操作，自动处理异常
    Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation, string methodName, string? context = null);
    Task<ServiceResult> SafeExecuteAsync(Func<Task<ServiceResult>> operation, string methodName, string? context = null);
}
```

### ExceptionMessageMapper
将技术异常转换为用户友好消息：

```csharp
// 技术异常 → 用户友好消息
ApiException(404) → "请求的资源未找到"
ApiException(500) → "服务器内部错误，请稍后重试"
TimeoutException → "请求超时，请检查网络连接"
ArgumentException → "输入参数无效，请检查输入内容"
```

## ✅ 完整示例

### 场景：用户管理功能

#### 1. ApiService（Infrastructure）
```csharp
public async Task<TResponse?> GetAsync<TResponse>(string endpoint, ...)
{
    using var response = await _httpClient.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        throw new ApiException(response.StatusCode, content); // 抛出结构化异常
    }

    return JsonSerializer.Deserialize<TResponse>(content);
}
```

#### 2. UserRepository（Repository）
```csharp
public virtual async Task<UserDto> GetByIdAsync(Guid id)
{
    // 透明传播异常，不做处理
    return await _apiService.GetAsync<UserDto>($"{_endpoint}/{id}");
}
```

#### 3. UserService（Business）
```csharp
public async Task<UserDto> CreateAsync(UserDto user)
{
    // 业务验证
    if (user.Id == Guid.Empty)
        throw new ArgumentException("用户ID无效");

    _logger.LogInformation($"创建用户: {user.UserName}");

    // Repository调用，异常自然传播
    return await _repository.CreateAsync(user);
}
```

#### 4. UserManagementViewModel（Presentation）
```csharp
public async Task CreateUserAsync()
{
    var result = await _exceptionHandler.SafeExecuteAsync(async () =>
    {
        var userDto = MapToDto(NewUser);
        var created = await _userService.CreateAsync(userDto);
        return ServiceResult<UserDto>.Success(created);
    }, nameof(CreateUserAsync), "创建用户");

    if (result.IsSuccess)
    {
        await _dialogService.ShowSuccessAsync("用户创建成功");
        await LoadUsersAsync();
    }
    else
    {
        await _dialogService.ShowErrorAsync(result.ErrorMessage);
    }
}
```

## 📊 异常流转图

```
┌─────────────┐
│  ApiService │ → throw ApiException(404, "Not Found")
└──────┬──────┘
       │ (传播)
       ↓
┌─────────────┐
│ Repository  │ → 不处理，直接传播
└──────┬──────┘
       │ (传播)
       ↓
┌─────────────┐
│   Service   │ → 添加业务验证，传播Repository异常
└──────┬──────┘
       │ (传播)
       ↓
┌─────────────┐
│  ViewModel  │ → StandardExceptionHandler.SafeExecuteAsync()
└──────┬──────┘       ↓
       │         转换为ServiceResult<T>
       ↓              ↓
┌─────────────┐    显示用户友好消息
│     UI      │ ← "请求的资源未找到"
└─────────────┘
```

## 🚫 常见错误

### 错误1：在Repository层掩盖异常
```csharp
// ❌ 错误
public virtual async Task<T> GetByIdAsync(Guid id)
{
    try
    {
        return await _apiService.GetAsync<T>($"{_endpoint}/{id}");
    }
    catch
    {
        return null; // 调用方无法知道发生了什么
    }
}
```

### 错误2：在Service层处理HTTP异常
```csharp
// ❌ 错误
public async Task<UserDto> GetByIdAsync(Guid id)
{
    try
    {
        return await _repository.GetByIdAsync(id);
    }
    catch (ApiException ex) // Service不应该知道HTTP细节
    {
        if (ex.StatusCode == HttpStatusCode.NotFound)
            return null;
        throw;
    }
}
```

### 错误3：多层重复日志
```csharp
// ❌ 错误：每层都记录，导致日志重复
// Repository:
catch (Exception ex) { _logger.LogError(ex, ...); throw; }
// Service:
catch (Exception ex) { _logger.LogError(ex, ...); throw; }
// ViewModel:
catch (Exception ex) { _logger.LogError(ex, ...); }

// ✅ 正确：只在ViewModel层统一记录
await _exceptionHandler.SafeExecuteAsync(...)
```

## 📝 最佳实践总结

1. **透明传播**：Repository和Service层让异常自然传播
2. **统一处理**：ViewModel层使用`StandardExceptionHandler`统一处理
3. **用户友好**：使用`ExceptionMessageMapper`转换消息
4. **避免掩盖**：不将异常转换为null或空集合
5. **业务验证**：在Service层添加业务逻辑验证
6. **日志分级**：只在最终处理点记录完整日志
7. **上下文保留**：抛出异常时保留完整的错误上下文

---

**维护日期**：2025-09-30
**相关文档**：
- `docs/architecture/desktop-architecture.md`
- `docs/reports/Issue-815--Architecture-Implementation-Report.md`