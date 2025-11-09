# 技术设计文档 - 管理员重置用户密码功能

**文档类型**: 技术设计  
**创建时间**: 2025-11-08  
**关联需求**: `.requirements/discussions/admin-user-management-reset-password.md`  
**状态**: ✅ 设计完成  
**预估工作量**: 150-200行代码，1.5-2小时

---

## 1. 架构设计

### 1.1 调用链路图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Client端 (WPF MVVM)                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  UserManagementView (用户列表)                                        │
│         │                                                            │
│         │ [点击"重置密码"按钮]                                         │
│         ↓                                                            │
│  UserManagementViewModel.ExecuteResetPasswordAsync()                │
│         │                                                            │
│         │ _dialogService.ShowDialog("ResetPasswordDialog")          │
│         ↓                                                            │
│  ResetPasswordDialog (弹窗)                                          │
│         │                                                            │
│         │ [输入新密码 / 生成随机密码]                                   │
│         │ [点击"确定"按钮]                                             │
│         ↓                                                            │
│  ResetPasswordDialogViewModel.ConfirmAsync()                        │
│         │                                                            │
│         │ await _commandHandler.ResetPasswordAsync(userId, pwd)     │
│         ↓                                                            │
│  UserCommandHandler.ResetPasswordAsync()  ← 【修复点5】               │
│         │                                                            │
│         │ await _repository.ResetPasswordAsync(userId, request)     │
│         ↓                                                            │
│  IUserRepository.ResetPasswordAsync()  ← 【修复点3：添加接口定义】      │
│         │                                                            │
│         ↓                                                            │
│  UserRepository.ResetPasswordAsync()  ← 【修复点4：添加实现】          │
│         │                                                            │
│         │ await _api.ResetPasswordAsync(userId, request)            │
│         ↓                                                            │
│  IUserApi.ResetPasswordAsync()  ← 【修复点1：添加接口定义】            │
│         │                                                            │
│         ↓                                                            │
│  【需查找】UserApi实现类.ResetPasswordAsync()  ← 【修复点2：添加实现】   │
│         │                                                            │
│         │ HTTP POST /api/users/{userId}/reset-password              │
│         ↓                                                            │
└─────────────────────────────────────────────────────────────────────┘
                            │
                            │ HTTP Request
                            ↓
┌─────────────────────────────────────────────────────────────────────┐
│                     Server端 (ASP.NET Core)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  UsersController.ResetPassword()  ✅ 已实现                          │
│         │                                                            │
│         │ await _userService.ResetPasswordAsync(id, request)        │
│         ↓                                                            │
│  UserService.ResetPasswordAsync()  ✅ 已实现                         │
│         │                                                            │
│         │ 1. 验证用户存在                                              │
│         │ 2. 生成/使用新密码                                           │
│         │ 3. BCrypt加密                                               │
│         │ 4. 更新数据库                                                │
│         │ 5. 记录审计日志                                              │
│         ↓                                                            │
│  Database (Users表)                                                 │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 三层架构对齐检查

**Client端架构**:
```
Presentation Layer (MVVM)
  ↓
  UserManagementViewModel
  ResetPasswordDialogViewModel
  ↓
Business Logic Layer
  ↓
  UserCommandHandler (业务命令处理器)
  ↓
Data Access Layer
  ↓
  IUserRepository + UserRepository
  ↓
HTTP API Client Layer
  ↓
  IUserApi + UserApi实现
```

**Server端架构** (已完整实现):
```
Presentation Layer (Controller)
  ↓
  UsersController
  ↓
Business Logic Layer (Service)
  ↓
  UserService
  ↓
Data Access Layer (Repository)
  ↓
  UserRepository
  ↓
  AppDbContext (EF Core)
```

✅ **符合三层对齐架构原则**

---

## 2. 接口设计

### 2.1 IUserApi接口扩展

**文件位置**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs`

**新增接口方法**:
```csharp
/// <summary>
/// 重置用户密码（管理员功能）
/// </summary>
/// <param name="userId">目标用户ID</param>
/// <param name="request">重置密码请求（包含新密码，为空则自动生成）</param>
/// <returns>重置密码响应（包含最终密码）</returns>
Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(
    Guid userId, 
    ResetPasswordRequestDto request);
```

**设计说明**:
- 返回类型: `ApiResponse<ResetPasswordResponseDto>` - 符合项目统一API响应格式
- 参数设计: 
  - `userId`: Guid类型，匹配Server端API路由参数
  - `request`: ResetPasswordRequestDto（已在Shared层定义）
- 异步模式: Task-based异步，符合项目规范

### 2.2 IUserRepository接口扩展

**文件位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserRepository.cs`

**新增接口方法**:
```csharp
/// <summary>
/// 重置用户密码（管理员功能）
/// </summary>
/// <param name="userId">目标用户ID</param>
/// <param name="request">重置密码请求</param>
/// <returns>重置密码结果（包含新密码）</returns>
Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
    Guid userId, 
    ResetPasswordRequestDto request);
```

**设计说明**:
- 返回类型: `ServiceResult<ResetPasswordResponseDto>` - 符合Repository层统一返回格式
- 与IUserApi的区别: ApiResponse→ServiceResult包装转换由Repository层负责

### 2.3 DTO定义（已存在）

**位置**: `src/Shared/LYBT.Shared.Models/Contracts/Users/UserDtos.cs`

**ResetPasswordRequestDto** (已定义):
```csharp
public class ResetPasswordRequestDto
{
    /// <summary>
    /// 新密码（可选，为空则Server端自动生成）
    /// </summary>
    public string? NewPassword { get; set; }
}
```

**ResetPasswordResponseDto** (已定义):
```csharp
public class ResetPasswordResponseDto
{
    /// <summary>
    /// 最终设置的密码（用于显示给管理员）
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
```

---

## 3. 数据流设计

### 3.1 正常流程（手动输入密码）

```
用户操作：点击"重置密码"按钮
    ↓
Step 1: UserManagementViewModel.ExecuteResetPasswordAsync(user)
    - 构造DialogParameters { UserId, UserName }
    - 打开ResetPasswordDialog
    ↓
Step 2: 用户在Dialog中输入新密码（≥8字符）
    - NewPassword = "MyNewPass123"
    - ConfirmPassword = "MyNewPass123"
    - 点击"确定"
    ↓
Step 3: ResetPasswordDialogViewModel.ConfirmAsync()
    - 验证：两次密码是否一致
    - 验证：密码长度≥8
    - SetIsBusy(true, "正在重置密码...")
    ↓
Step 4: UserCommandHandler.ResetPasswordAsync(userId, "MyNewPass123")
    - 构造ResetPasswordRequestDto { NewPassword = "MyNewPass123" }
    - 调用Repository
    ↓
Step 5: UserRepository.ResetPasswordAsync(userId, request)
    - 调用IUserApi
    - ApiResponse → ServiceResult转换
    ↓
Step 6: UserApi.ResetPasswordAsync(userId, request)
    - HTTP POST /api/users/{userId}/reset-password
    - Body: { "newPassword": "MyNewPass123" }
    ↓
Step 7: Server端处理
    - UsersController.ResetPassword()
    - UserService.ResetPasswordAsync()
    - BCrypt加密密码
    - 更新数据库Users表
    - 返回 { "newPassword": "MyNewPass123" }
    ↓
Step 8: Client端接收响应
    - ResetPasswordDialogViewModel显示成功消息
    - 弹窗显示新密码："密码重置成功！\n\n新密码: MyNewPass123"
    - 关闭Dialog
    - UserManagementViewModel记录日志
```

### 3.2 自动生成密码流程

```
Step 1-2: 相同（打开Dialog）
    ↓
Step 3: 用户点击"生成随机密码"按钮
    - ResetPasswordDialogViewModel.GenerateRandomPassword()
    - 生成12位复杂密码（例如："aB3$xY9@mK2!"）
    - 自动填充NewPassword和ConfirmPassword字段
    ↓
Step 4: 用户点击"确定"
    - 后续流程相同
```

### 3.3 错误流程

**场景1：密码验证失败**
```
ResetPasswordDialogViewModel.ValidatePasswords()
    ↓
如果：NewPassword != ConfirmPassword
    → SetError("两次输入的密码不一致")
    → return（不发送请求）

如果：NewPassword.Length < 8
    → SetError("密码长度至少8个字符")
    → return（不发送请求）
```

**场景2：Server端错误**
```
Server端返回：ApiResponse.Success = false
    ↓
UserRepository.ResetPasswordAsync()
    → ServiceResult.Failure(apiResponse.Message)
    ↓
UserCommandHandler.ResetPasswordAsync()
    → return (false, errorMessage, null)
    ↓
ResetPasswordDialogViewModel.ConfirmAsync()
    → SetError(errorMessage)
    → 不关闭Dialog，用户可以重试
```

**场景3：网络异常**
```
UserApi.ResetPasswordAsync() 抛出异常
    ↓
UserRepository.ResetPasswordAsync() catch异常
    → ServiceResult.Failure($"重置密码失败: {ex.Message}")
    ↓
ResetPasswordDialogViewModel.ConfirmAsync() catch异常
    → ShowErrorMessageAsync($"重置密码失败: {ex.Message}")
    → SetIsBusy(false)
```

---

## 4. 详细实现方案

### 4.1 文件1: IUserApi接口扩展

**文件路径**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs`

**修改位置**: 在interface内部，最后一个方法后添加

**完整代码**:
```csharp
/// <summary>
/// 重置用户密码（管理员功能）
/// </summary>
/// <param name="userId">目标用户ID</param>
/// <param name="request">重置密码请求</param>
/// <returns>重置密码响应（包含新密码）</returns>
Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(
    Guid userId, 
    ResetPasswordRequestDto request);
```

**注意事项**:
- 添加在ChangePasswordAsync方法之后
- 使用中文注释
- 参数名使用camelCase

---

### 4.2 文件2: UserApi实现类（需查找）

**预期文件路径**: 
- 方案1: `src/Client/Desktop/Infrastructure/Api/UserApi.cs`
- 方案2: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Api/UserApi.cs`
- 方案3: 其他位置（需要serena工具查找）

**实现代码模板**:
```csharp
/// <summary>
/// 重置用户密码
/// </summary>
public async Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(
    Guid userId, 
    ResetPasswordRequestDto request)
{
    try
    {
        _logger.LogDebug("调用重置密码API: UserId={UserId}", userId);

        // 使用HttpClient发送POST请求
        var response = await _httpClient.PostAsync<ResetPasswordRequestDto, ResetPasswordResponseDto>(
            $"users/{userId}/reset-password", 
            request);

        if (response.Success)
        {
            _logger.LogInformation("重置密码API调用成功");
        }
        else
        {
            _logger.LogWarning("重置密码API调用失败: {Message}", response.Message);
        }

        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "调用重置密码API时发生异常");
        throw;
    }
}
```

**实现说明**:
- 使用项目统一的`_httpClient.PostAsync`扩展方法
- 路由格式: `users/{userId}/reset-password` (匹配Server端路由)
- 完整的日志记录（Debug/Info/Warning/Error）
- 异常向上抛出，由Repository层统一处理

**查找策略**:
1. 使用`serena.find_file`查找`*UserApi.cs`（排除IUserApi.cs）
2. 检查该文件是否有`CreateUserAsync`等方法
3. 参考其他方法的实现模式

---

### 4.3 文件3: IUserRepository接口扩展

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserRepository.cs`

**修改位置**: 在interface内部，ChangePasswordAsync方法之后添加

**完整代码**:
```csharp
/// <summary>
/// 重置用户密码（管理员功能）
/// </summary>
/// <param name="userId">目标用户ID</param>
/// <param name="request">重置密码请求</param>
/// <returns>重置密码结果</returns>
Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
    Guid userId, 
    ResetPasswordRequestDto request);
```

---

### 4.4 文件4: UserRepository实现

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`

**修改位置**: 在ChangePasswordAsync方法之后添加

**完整代码**:
```csharp
/// <summary>
/// 重置用户密码（管理员功能）
/// Issue: 完善用户管理功能 - 打通重置密码调用链
/// </summary>
public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
    Guid userId, 
    ResetPasswordRequestDto request)
{
    try
    {
        _logger.LogInformation("重置用户密码: UserId={UserId}", userId);

        // 调用IUserApi
        var apiResponse = await _api.ResetPasswordAsync(userId, request);

        // ApiResponse → ServiceResult 转换
        if (apiResponse.Success && apiResponse.Data != null)
        {
            _logger.LogInformation("用户密码重置成功");
            return ServiceResult<ResetPasswordResponseDto>.Success(
                apiResponse.Data, 
                apiResponse.Message ?? "密码重置成功");
        }

        var errorMsg = apiResponse.Message ?? "重置用户密码失败";
        _logger.LogWarning("重置用户密码失败: {Message}", errorMsg);
        return ServiceResult<ResetPasswordResponseDto>.Failure(errorMsg);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "重置用户密码时发生异常: UserId={UserId}", userId);
        return ServiceResult<ResetPasswordResponseDto>.Failure($"重置用户密码失败: {ex.Message}");
    }
}
```

**实现说明**:
- 完整的异常处理（不向上抛出，返回ServiceResult.Failure）
- ApiResponse→ServiceResult转换逻辑
- 日志记录（Info/Warning/Error三级）
- 参考ChangePasswordAsync方法的实现模式

---

### 4.5 文件5: UserCommandHandler修复

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Components/UserCommandHandler.cs`

**修改位置**: 第284-290行（ResetPasswordAsync方法）

**当前代码**:
```csharp
public Task<(bool success, string? errorMessage)> ResetPasswordAsync(Guid userId, string newPassword)
{
    _logger.LogInformation("重置密码: {UserId}", userId);

    // TODO: 实现重置密码逻辑（应该调用认证服务）
    return Task.FromResult<(bool, string?)>((true, "重置密码功能开发中"));
}
```

**修改后代码**:
```csharp
/// <summary>
/// 重置用户密码（管理员功能）
/// Issue: 完善用户管理功能 - 打通重置密码调用链
/// </summary>
/// <param name="userId">目标用户ID</param>
/// <param name="newPassword">新密码</param>
/// <returns>重置结果（success, errorMessage, response）</returns>
public async Task<(bool success, string? errorMessage, ResetPasswordResponseDto? response)> ResetPasswordAsync(
    Guid userId, 
    string newPassword)
{
    _logger.LogInformation("重置密码: {UserId}", userId);

    try
    {
        // 构造请求DTO
        var request = new ResetPasswordRequestDto 
        { 
            NewPassword = newPassword 
        };

        // 调用Repository
        var result = await _repository.ResetPasswordAsync(userId, request);

        if (result.IsSuccess && result.Data != null)
        {
            _logger.LogInformation("密码重置成功");
            return (true, null, result.Data);
        }

        _logger.LogWarning("密码重置失败: {Message}", result.Message);
        return (false, result.Message, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "重置密码时发生异常");
        return (false, $"重置密码失败: {ex.Message}", null);
    }
}
```

**关键变更**:
1. 返回类型：`(bool, string?)` → `(bool, string?, ResetPasswordResponseDto?)`
   - 新增response字段，用于返回Server端生成的密码
2. 方法签名：同步 → 异步（Task.FromResult → async/await）
3. 实现逻辑：Mock → 真实调用_repository
4. 异常处理：新增try-catch

---

### 4.6 文件6: ResetPasswordDialogViewModel修复

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/ResetPasswordDialogViewModel.cs`

**修改位置**: ConfirmAsync方法（约line 250-280）

**当前代码**:
```csharp
private async Task ConfirmAsync()
{
    try
    {
        if (!ValidatePasswords()) return;
        if (_targetUserId == Guid.Empty) { SetError("无效的用户ID"); return; }

        SetIsBusy(true, "正在重置密码...");

        // TODO: 当前 Client 端没有 ResetPassword 服务方法，暂时 Mock 成功
        // 真实实现需要调用服务端 API
        await Task.Delay(500); // 模拟网络延迟

        await ShowSuccessMessageAsync("密码重置成功");

        var dialogResult = new DialogResult(ButtonResult.OK);
        dialogResult.Parameters.Add("RequirePasswordChange", RequirePasswordChange);
        dialogResult.Parameters.Add("SendNotification", SendNotification);

        RequestClose?.Invoke(dialogResult);

        Logger.LogInformation(
            "用户 {UserId} 密码重置成功 (要求修改密码: {RequireChange}, 发送通知: {SendNotification})",
            _targetUserId,
            RequirePasswordChange,
            SendNotification);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "重置密码时发生异常");
        await ShowErrorMessageAsync($"重置密码失败: {ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

**修改后代码**:
```csharp
/// <summary>
/// 确认重置密码
/// Issue: 完善用户管理功能 - 打通重置密码调用链
/// </summary>
private async Task ConfirmAsync()
{
    try
    {
        // 验证输入
        if (!ValidatePasswords()) return;
        if (_targetUserId == Guid.Empty) 
        { 
            SetError("无效的用户ID"); 
            return; 
        }

        SetIsBusy(true, "正在重置密码...");

        // 调用CommandHandler重置密码
        var result = await _commandHandler.ResetPasswordAsync(_targetUserId, NewPassword);

        if (result.success && result.response != null)
        {
            // 重置成功 - 显示新密码
            var successMessage = $"密码重置成功！\n\n新密码: {result.response.NewPassword}";
            await ShowSuccessMessageAsync(successMessage);

            // 构造Dialog返回参数
            var dialogResult = new DialogResult(ButtonResult.OK);
            dialogResult.Parameters.Add("RequirePasswordChange", RequirePasswordChange);
            dialogResult.Parameters.Add("SendNotification", SendNotification);
            dialogResult.Parameters.Add("NewPassword", result.response.NewPassword);

            // 关闭Dialog
            RequestClose?.Invoke(dialogResult);

            Logger.LogInformation(
                "用户 {UserId} 密码重置成功 (要求修改密码: {RequireChange}, 发送通知: {SendNotification})",
                _targetUserId,
                RequirePasswordChange,
                SendNotification);
        }
        else
        {
            // 重置失败 - 显示错误消息
            SetError(result.errorMessage ?? "密码重置失败");
            Logger.LogWarning("重置密码失败: {ErrorMessage}", result.errorMessage);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "重置密码时发生异常");
        await ShowErrorMessageAsync($"重置密码失败: {ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

**关键变更**:
1. 移除Mock实现：`await Task.Delay(500)` → 真实API调用
2. 调用CommandHandler：`await _commandHandler.ResetPasswordAsync(_targetUserId, NewPassword)`
3. 错误处理：成功/失败分支处理
4. 用户体验：显示新密码给管理员
5. Dialog参数：新增NewPassword参数

---

### 4.7 文件7: UserManagementViewModel修复

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs`

**修改位置**: ExecuteResetPasswordAsync方法（line 418-437）

**当前代码**:
```csharp
private async Task ExecuteResetPasswordAsync(UserDto user)
{
    if (user == null) return;

    await ExecuteSafelyAsync(() =>
    {
        Logger.LogDebug("重置用户密码: {UserId} - {UserName}", user.Id, user.UserName);

        // 调用应用的密码重置服务，或者打开密码重置对话框
        // 暂时记录日志
        Logger.LogInformation("用户 {UserName} 的密码重置请求已提交", user.UserName);

        // 实际实现可能需要：
        // 1. 打开密码重置对话框
        // 2. 调用密码重置API
        // 3. 显示成功通知

        return Task.CompletedTask;
    }, "重置密码");
}
```

**修改后代码**:
```csharp
/// <summary>
/// 重置用户密码
/// Issue: 完善用户管理功能 - 打通重置密码调用链
/// </summary>
private async Task ExecuteResetPasswordAsync(UserDto user)
{
    if (user == null) return;

    Logger.LogDebug("重置用户密码: {UserId} - {UserName}", user.Id, user.UserName);

    // 构造Dialog参数
    var parameters = new DialogParameters
    {
        { "UserId", user.Id },
        { "UserName", user.UserName }
    };

    // 打开ResetPasswordDialog
    _dialogService.ShowDialog("ResetPasswordDialog", parameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            Logger.LogInformation("用户 {UserName} 密码重置成功", user.UserName);

            // 可选：刷新列表或显示通知
            // await LoadPageAsync();
        }
        else
        {
            Logger.LogDebug("用户取消重置密码操作");
        }
    });

    await Task.CompletedTask;
}
```

**关键变更**:
1. 移除ExecuteSafelyAsync包装（因为不需要异常处理，Dialog内部已处理）
2. 打开Dialog：`_dialogService.ShowDialog("ResetPasswordDialog", parameters, callback)`
3. Dialog回调：result.Result == ButtonResult.OK 时记录日志
4. 移除TODO注释

---

## 5. 异常处理策略

### 5.1 异常处理层次

```
Layer 1: UserApi（HTTP层）
    - 抛出异常（HttpRequestException, TaskCanceledException等）
    - 记录Error日志

Layer 2: UserRepository（数据访问层）
    - catch所有异常
    - 转换为ServiceResult.Failure
    - 记录Error日志

Layer 3: UserCommandHandler（业务逻辑层）
    - catch所有异常
    - 转换为(false, errorMessage, null)元组
    - 记录Error日志

Layer 4: ResetPasswordDialogViewModel（表示层）
    - catch所有异常
    - 显示错误消息给用户（ShowErrorMessageAsync）
    - 记录Error日志
    - SetIsBusy(false)
```

### 5.2 异常处理矩阵

| 异常类型 | 处理层 | 处理方式 | 用户提示 |
|----------|--------|----------|----------|
| `HttpRequestException` | UserRepository | ServiceResult.Failure | "网络连接失败，请检查网络" |
| `TaskCanceledException` | UserRepository | ServiceResult.Failure | "请求超时，请重试" |
| `InvalidOperationException` | UserCommandHandler | (false, ex.Message, null) | ex.Message |
| `ArgumentNullException` | ResetPasswordDialogViewModel | SetError | "参数错误，请重新操作" |
| 其他Exception | ResetPasswordDialogViewModel | ShowErrorMessageAsync | "重置密码失败: {ex.Message}" |

---

## 6. 日志策略

### 6.1 日志级别使用规范

**LogDebug** - 调试信息（开发环境）:
```csharp
_logger.LogDebug("调用重置密码API: UserId={UserId}", userId);
_logger.LogDebug("重置用户密码: {UserId} - {UserName}", user.Id, user.UserName);
```

**LogInformation** - 重要操作（生产环境）:
```csharp
_logger.LogInformation("重置用户密码: UserId={UserId}", userId);
_logger.LogInformation("用户密码重置成功");
_logger.LogInformation("用户 {UserName} 密码重置成功", user.UserName);
```

**LogWarning** - 业务失败（非异常）:
```csharp
_logger.LogWarning("重置用户密码失败: {Message}", errorMsg);
_logger.LogWarning("密码重置失败: {Message}", result.Message);
```

**LogError** - 异常情况:
```csharp
_logger.LogError(ex, "调用重置密码API时发生异常");
_logger.LogError(ex, "重置用户密码时发生异常: UserId={UserId}", userId);
_logger.LogError(ex, "重置密码时发生异常");
```

### 6.2 日志关键信息

每层日志必须包含的信息：
- **UserApi**: UserId, HTTP Method, Endpoint
- **UserRepository**: UserId, Success/Failure
- **UserCommandHandler**: UserId
- **ResetPasswordDialogViewModel**: UserId, RequirePasswordChange, SendNotification

---

## 7. 验证方案

### 7.1 单元测试（可选，不在本次范围）

**测试文件**: `tests/UnitTests/Client/LYBT.Desktop.Users.Tests/Repositories/UserRepositoryTests.cs`

**测试用例**:
```csharp
[Fact]
public async Task ResetPasswordAsync_Success_ReturnsServiceResultSuccess()
{
    // Arrange
    var userId = Guid.NewGuid();
    var request = new ResetPasswordRequestDto { NewPassword = "NewPass123" };
    var apiResponse = new ApiResponse<ResetPasswordResponseDto> 
    { 
        Success = true, 
        Data = new ResetPasswordResponseDto { NewPassword = "NewPass123" } 
    };
    
    _mockUserApi.Setup(x => x.ResetPasswordAsync(userId, request))
        .ReturnsAsync(apiResponse);

    // Act
    var result = await _repository.ResetPasswordAsync(userId, request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Data);
    Assert.Equal("NewPass123", result.Data.NewPassword);
}

[Fact]
public async Task ResetPasswordAsync_ApiFailure_ReturnsServiceResultFailure()
{
    // Arrange
    var userId = Guid.NewGuid();
    var request = new ResetPasswordRequestDto { NewPassword = "NewPass123" };
    var apiResponse = new ApiResponse<ResetPasswordResponseDto> 
    { 
        Success = false, 
        Message = "用户不存在" 
    };
    
    _mockUserApi.Setup(x => x.ResetPasswordAsync(userId, request))
        .ReturnsAsync(apiResponse);

    // Act
    var result = await _repository.ResetPasswordAsync(userId, request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("用户不存在", result.Message);
}
```

### 7.2 集成测试（手动）

**测试步骤**:
1. ✅ 启动Server端（LYBT.WebAPI）
2. ✅ 启动Client端（LYBT.Desktop.Shell）
3. ✅ 管理员登录（admin / Admin@123）
4. ✅ 进入"管理员工作台" → "用户管理"
5. ✅ 点击某个用户的"重置密码"按钮
6. ✅ 验证ResetPasswordDialog正常打开
7. ✅ 测试场景1：手动输入密码
   - 输入新密码："TestPass123"
   - 确认密码："TestPass123"
   - 点击"确定"
   - 期望：提示"密码重置成功！新密码: TestPass123"
8. ✅ 测试场景2：自动生成密码
   - 点击"生成随机密码"按钮
   - 验证生成的密码（12位复杂密码）
   - 点击"确定"
   - 期望：提示"密码重置成功！新密码: {生成的密码}"
9. ✅ 测试场景3：错误处理
   - 输入不一致的密码 → 提示"两次输入的密码不一致"
   - 输入短密码(<8字符) → 提示"密码长度至少8个字符"
10. ✅ 使用新密码登录验证
11. ✅ 检查数据库`Users`表的`PasswordHash`字段已更新

### 7.3 代码质量检查清单

- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 所有方法有中文注释
- [ ] 所有async方法有try-catch
- [ ] 所有操作有日志记录（LogInformation/LogError）
- [ ] 命名规范符合项目标准（PascalCase/camelCase）
- [ ] 使用using导入的命名空间按字母排序
- [ ] 代码格式化（4空格缩进）
- [ ] 无未使用的using语句
- [ ] 无未使用的变量

---

## 8. 潜在风险与缓解措施

### 8.1 技术风险

**风险1**: UserApi实现类名称和位置未知
- **可能性**: 低
- **影响**: 中（需要额外查找时间）
- **缓解措施**: 
  1. 使用serena工具查找`*UserApi.cs`文件
  2. 检查是否继承/实现IUserApi
  3. 参考CreateUserAsync等方法的实现模式

**风险2**: ResetPasswordDialog未在UsersModule注册
- **可能性**: 低
- **影响**: 高（Dialog无法打开）
- **缓解措施**:
  1. 检查`UsersModule.cs`的`RegisterTypes`方法
  2. 验证是否有`containerRegistry.RegisterDialog<ResetPasswordDialog, ResetPasswordDialogViewModel>();`
  3. 如果缺失，添加注册代码

**风险3**: DTO字段名称不匹配
- **可能性**: 极低
- **影响**: 中（运行时错误）
- **缓解措施**:
  1. 已验证Shared层DTOs存在
  2. 编译时会检查类型匹配

### 8.2 业务风险

**风险4**: 权限控制缺失
- **可能性**: 中
- **影响**: 高（安全漏洞）
- **缓解措施**:
  1. Server端已有[Authorize]特性
  2. 需验证是否有角色检查（SuperAdmin/Admin才能重置）
  3. 如果缺失，后续Issue补充

**风险5**: 密码明文传输
- **可能性**: 低
- **影响**: 高（安全漏洞）
- **缓解措施**:
  1. 使用HTTPS传输（生产环境必须）
  2. Server端BCrypt加密后存储
  3. 当前开发环境HTTP可接受

---

## 9. 实施步骤

### 9.1 准备阶段（5分钟）

1. ✅ 查找UserApi实现类位置
   ```
   serena.find_file(*UserApi.cs, src/Client)
   serena.read_file(找到的文件)
   ```

2. ✅ 验证ResetPasswordDialog注册
   ```
   serena.read_file(src/Client/Desktop/Modules/LYBT.Desktop.Users/UsersModule.cs)
   检查RegisterTypes方法
   ```

### 9.2 实现阶段（60-80分钟）

**Phase 1: 基础接口层（15分钟）**
1. 修改IUserApi.cs（添加接口定义）
2. 修改UserApi实现类（添加方法实现）
3. 编译验证Client.Contracts和Client.Infrastructure项目

**Phase 2: Repository层（15分钟）**
4. 修改IUserRepository.cs（添加接口定义）
5. 修改UserRepository.cs（添加方法实现）
6. 编译验证Client.Users项目

**Phase 3: 业务逻辑层（20分钟）**
7. 修改UserCommandHandler.cs（修复ResetPasswordAsync方法）
8. 编译验证Client.Users项目

**Phase 4: 表示层（20分钟）**
9. 修改ResetPasswordDialogViewModel.cs（修复ConfirmAsync方法）
10. 修改UserManagementViewModel.cs（修复ExecuteResetPasswordAsync方法）
11. 编译验证整个Solution

**Phase 5: 最终验证（10分钟）**
12. 完整编译（dotnet build LYBT.All.sln）
13. 检查编译输出（0 errors, 0 warnings）

### 9.3 测试阶段（30-40分钟）

**Step 1: 启动应用（5分钟）**
1. 启动Server端
2. 启动Client端
3. 管理员登录

**Step 2: 功能测试（15分钟）**
4. 测试场景1：手动输入密码
5. 测试场景2：自动生成密码
6. 测试场景3：错误处理

**Step 3: 数据验证（10分钟）**
7. 使用新密码登录
8. 检查数据库Users表
9. 检查日志输出

### 9.4 提交阶段（10分钟）

1. git add（7个修改的文件）
2. git commit（符合规范的提交信息）
3. git push
4. 创建GitHub Issue（记录完成情况）

---

## 10. 验收标准（复制自需求文档）

### 10.1 功能验收

- [ ] 场景1：管理员手动输入密码重置成功
- [ ] 场景2：管理员自动生成密码重置成功
- [ ] 场景3：错误处理（密码不一致/长度不足/网络异常）

### 10.2 代码质量验收

- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 代码规范符合项目标准
- [ ] 日志完整（LogDebug/LogInformation/LogWarning/LogError）
- [ ] 异常处理完整（每层独立try-catch）
- [ ] 资源释放（Dialog关闭时清空密码字段）

### 10.3 运行时验收

- [ ] 启动Server端成功
- [ ] 启动Client端成功
- [ ] 管理员登录成功
- [ ] 打开用户管理界面成功
- [ ] 点击"重置密码"按钮成功打开Dialog
- [ ] 手动输入密码重置成功
- [ ] 自动生成密码重置成功
- [ ] 使用新密码登录成功
- [ ] 数据库Users表PasswordHash字段已更新

---

## 11. 后续优化（Phase 2，不在本次范围）

1. **邮件/短信通知** - 重置密码后自动发送通知给用户
2. **强制修改密码** - 用户下次登录时强制修改密码（需Login流程配合）
3. **密码历史记录** - 防止重复使用最近N次的旧密码
4. **密码强度检查** - 实时检查密码复杂度（弱/中/强）
5. **权限细化** - 验证SuperAdmin/Admin权限，Doctor不可重置

---

**设计文档状态**: ✅ 设计完成，等待实施

**预估时间**: 
- 准备阶段：5分钟
- 实现阶段：60-80分钟
- 测试阶段：30-40分钟
- 提交阶段：10分钟
- **总计**: 1.5-2.5小时
