# 任务分解清单 - 管理员重置用户密码功能

**关联需求**: `.requirements/discussions/admin-user-management-reset-password.md`  
**关联设计**: `.design/admin-user-management-reset-password.md`  
**创建时间**: 2025-11-08  
**预估总工作量**: 1.5-2.5小时

---

## 任务概览

| 阶段 | 任务数 | 预估时间 | 状态 |
|------|--------|----------|------|
| 准备阶段 | 2 | 5分钟 | ⏳ Pending |
| 实现阶段 | 7 | 60-80分钟 | ⏳ Pending |
| 验证阶段 | 3 | 30-40分钟 | ⏳ Pending |
| 提交阶段 | 2 | 10分钟 | ⏳ Pending |
| **总计** | **14** | **1.5-2.5小时** | **⏳ Pending** |

---

## 准备阶段（2个任务，5分钟）

### Task 0.1: 查找UserApi实现类位置

**优先级**: P0（阻塞任务）  
**预估时间**: 3分钟  
**状态**: ⏳ Pending

**任务描述**:
查找Client端UserApi的实现类，确定其文件路径和类名。

**执行步骤**:
1. 使用serena工具查找：`serena.find_file(*UserApi.cs, src/Client)`
2. 过滤掉IUserApi.cs接口文件
3. 阅读找到的文件，确认是否包含CreateUserAsync等方法
4. 记录文件路径和类名

**验收标准**:
- [ ] 找到UserApi实现类的完整路径
- [ ] 确认该类实现了IUserApi接口
- [ ] 确认该类有CreateUserAsync/UpdateUserAsync等方法

**输出**:
- UserApi实现类路径（例如：`src/Client/Desktop/Infrastructure/Api/UserApi.cs`）
- 类名（例如：`UserApi` 或 `HttpUserApi`）

---

### Task 0.2: 验证ResetPasswordDialog注册

**优先级**: P1（重要但非阻塞）  
**预估时间**: 2分钟  
**状态**: ⏳ Pending

**任务描述**:
验证ResetPasswordDialog是否已在UsersModule中注册。

**执行步骤**:
1. 读取文件：`src/Client/Desktop/Modules/LYBT.Desktop.Users/UsersModule.cs`
2. 查找RegisterTypes方法
3. 检查是否有：`containerRegistry.RegisterDialog<ResetPasswordDialog, ResetPasswordDialogViewModel>();`

**验收标准**:
- [ ] 找到UsersModule.cs的RegisterTypes方法
- [ ] 确认ResetPasswordDialog已注册 或 确认需要添加注册代码

**输出**:
- 注册状态（已注册 / 需要注册）
- 如果未注册，记录需要添加的代码位置

---

## 实现阶段（7个任务，60-80分钟）

### Phase 1: 基础接口层（2个任务，15分钟）

#### Task 1.1: 修改IUserApi接口

**优先级**: P0  
**预估时间**: 5分钟  
**状态**: ⏳ Pending

**文件路径**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs`

**修改位置**: interface内部，ChangePasswordAsync方法之后

**修改内容**:
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

**验收标准**:
- [ ] 方法签名正确
- [ ] 使用中文注释
- [ ] 参数名使用camelCase
- [ ] 编译通过（LYBT.Desktop.Contracts项目）

---

#### Task 1.2: 实现UserApi方法

**优先级**: P0  
**预估时间**: 10分钟  
**状态**: ⏳ Pending  
**依赖**: Task 0.1（需要知道UserApi实现类路径）

**文件路径**: `【待Task 0.1确定】`

**修改位置**: 类内部，参考其他方法的位置

**修改内容**:
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

**注意事项**:
1. 参考该类中CreateUserAsync/UpdateUserAsync的实现模式
2. 使用项目统一的`_httpClient.PostAsync`扩展方法
3. 路由格式：`users/{userId}/reset-password`
4. 完整的日志记录（Debug/Info/Warning/Error）

**验收标准**:
- [ ] 方法签名与IUserApi接口一致
- [ ] 使用_httpClient.PostAsync发送HTTP请求
- [ ] 路由正确：`users/{userId}/reset-password`
- [ ] 日志完整（LogDebug/LogInformation/LogWarning/LogError）
- [ ] 编译通过（UserApi所在项目）

---

### Phase 2: Repository层（2个任务，15分钟）

#### Task 2.1: 修改IUserRepository接口

**优先级**: P0  
**预估时间**: 5分钟  
**状态**: ⏳ Pending

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserRepository.cs`

**修改位置**: interface内部，ChangePasswordAsync方法之后

**修改内容**:
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

**验收标准**:
- [ ] 方法签名正确
- [ ] 返回类型为ServiceResult<ResetPasswordResponseDto>
- [ ] 使用中文注释
- [ ] 编译通过（LYBT.Desktop.Users项目）

---

#### Task 2.2: 实现UserRepository方法

**优先级**: P0  
**预估时间**: 10分钟  
**状态**: ⏳ Pending

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`

**修改位置**: ChangePasswordAsync方法之后

**修改内容**:
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

**注意事项**:
1. 参考ChangePasswordAsync方法的实现模式
2. ApiResponse→ServiceResult转换逻辑
3. 异常不向上抛出，返回ServiceResult.Failure

**验收标准**:
- [ ] 方法签名与IUserRepository接口一致
- [ ] 调用_api.ResetPasswordAsync
- [ ] ApiResponse正确转换为ServiceResult
- [ ] 异常处理完整（try-catch，返回Failure）
- [ ] 日志完整（LogInformation/LogWarning/LogError）
- [ ] 编译通过（LYBT.Desktop.Users项目）

---

### Phase 3: 业务逻辑层（1个任务，20分钟）

#### Task 3.1: 修复UserCommandHandler.ResetPasswordAsync

**优先级**: P0  
**预估时间**: 20分钟  
**状态**: ⏳ Pending

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
1. ✅ 返回类型：`(bool, string?)` → `(bool, string?, ResetPasswordResponseDto?)`
2. ✅ 方法签名：同步 → 异步（Task.FromResult → async/await）
3. ✅ 实现逻辑：Mock → 真实调用_repository
4. ✅ 异常处理：新增try-catch

**验收标准**:
- [ ] 返回类型已修改为三元组（新增response字段）
- [ ] 方法改为async/await模式
- [ ] 调用_repository.ResetPasswordAsync
- [ ] 构造ResetPasswordRequestDto正确
- [ ] 异常处理完整（try-catch）
- [ ] 日志完整（LogInformation/LogWarning/LogError）
- [ ] 编译通过（LYBT.Desktop.Users项目）
- [ ] 无编译警告

---

### Phase 4: 表示层（2个任务，20分钟）

#### Task 4.1: 修复ResetPasswordDialogViewModel.ConfirmAsync

**优先级**: P0  
**预估时间**: 15分钟  
**状态**: ⏳ Pending

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/ResetPasswordDialogViewModel.cs`

**修改位置**: ConfirmAsync方法（约line 250-280）

**当前代码**: 包含Mock实现（await Task.Delay(500)）

**修改后代码**: 见设计文档4.6节

**关键变更**:
1. ✅ 移除Mock实现：`await Task.Delay(500)`
2. ✅ 调用CommandHandler：`await _commandHandler.ResetPasswordAsync(_targetUserId, NewPassword)`
3. ✅ 错误处理：成功/失败分支处理
4. ✅ 用户体验：显示新密码给管理员
5. ✅ Dialog参数：新增NewPassword参数

**验收标准**:
- [ ] 移除Mock实现（await Task.Delay(500)）
- [ ] 调用_commandHandler.ResetPasswordAsync
- [ ] 成功分支：显示新密码（result.response.NewPassword）
- [ ] 失败分支：显示错误消息（SetError）
- [ ] Dialog返回参数包含NewPassword
- [ ] 异常处理完整（try-catch-finally）
- [ ] SetIsBusy正确管理
- [ ] 编译通过（LYBT.Desktop.Users项目）

---

#### Task 4.2: 修复UserManagementViewModel.ExecuteResetPasswordAsync

**优先级**: P0  
**预估时间**: 5分钟  
**状态**: ⏳ Pending

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs`

**修改位置**: ExecuteResetPasswordAsync方法（line 418-437）

**当前代码**: 只记录日志，未打开Dialog

**修改后代码**: 见设计文档4.7节

**关键变更**:
1. ✅ 移除ExecuteSafelyAsync包装
2. ✅ 打开Dialog：`_dialogService.ShowDialog("ResetPasswordDialog", parameters, callback)`
3. ✅ Dialog回调：result.Result == ButtonResult.OK 时记录日志
4. ✅ 移除TODO注释

**验收标准**:
- [ ] 移除ExecuteSafelyAsync包装
- [ ] 构造DialogParameters（包含UserId和UserName）
- [ ] 调用_dialogService.ShowDialog
- [ ] Dialog回调处理（OK时记录日志）
- [ ] 移除TODO注释和注释掉的代码
- [ ] 编译通过（LYBT.Desktop.Users项目）

---

## 验证阶段（3个任务，30-40分钟）

### Task 5.1: 编译验证

**优先级**: P0（阻塞后续任务）  
**预估时间**: 10分钟  
**状态**: ⏳ Pending  
**依赖**: Task 1.1-4.2全部完成

**执行步骤**:
1. 清理解决方案：`dotnet clean LYBT.All.sln`
2. 还原NuGet包：`dotnet restore LYBT.All.sln`
3. 编译解决方案：`dotnet build LYBT.All.sln -c Release --no-restore`
4. 检查编译输出

**验收标准**:
- [ ] 编译成功（Build succeeded）
- [ ] 0 errors
- [ ] 0 warnings
- [ ] 所有项目编译通过

**如果失败**:
- 记录错误信息
- 定位错误文件和行号
- 修复错误后重新编译

---

### Task 5.2: 功能测试（手动）

**优先级**: P0  
**预估时间**: 15分钟  
**状态**: ⏳ Pending  
**依赖**: Task 5.1完成

**测试环境**:
- Server端：LYBT.WebAPI（已启动）
- Client端：LYBT.Desktop.Shell（已启动）
- 登录账号：admin / Admin@123

**测试场景1：手动输入密码**
1. [ ] 进入"管理员工作台" → "用户管理"
2. [ ] 点击某个用户的"重置密码"按钮
3. [ ] 验证ResetPasswordDialog正常打开
4. [ ] 输入新密码："TestPass123"
5. [ ] 确认密码："TestPass123"
6. [ ] 点击"确定"按钮
7. [ ] 期望：提示"密码重置成功！\n\n新密码: TestPass123"
8. [ ] Dialog自动关闭

**测试场景2：自动生成密码**
1. [ ] 打开ResetPasswordDialog
2. [ ] 点击"生成随机密码"按钮
3. [ ] 验证生成的密码（12位复杂密码）
4. [ ] 点击"确定"按钮
5. [ ] 期望：提示"密码重置成功！\n\n新密码: {生成的密码}"
6. [ ] Dialog自动关闭

**测试场景3：错误处理**
1. [ ] 输入新密码："Pass123"
2. [ ] 确认密码："Pass456"（不一致）
3. [ ] 点击"确定"
4. [ ] 期望：提示"两次输入的密码不一致"
5. [ ] Dialog不关闭
6. [ ] 输入新密码："Short"（<8字符）
7. [ ] 确认密码："Short"
8. [ ] 点击"确定"
9. [ ] 期望：提示"密码长度至少8个字符"

**验收标准**:
- [ ] 场景1：手动输入密码重置成功
- [ ] 场景2：自动生成密码重置成功
- [ ] 场景3：错误处理正确

---

### Task 5.3: 数据验证

**优先级**: P0  
**预估时间**: 10分钟  
**状态**: ⏳ Pending  
**依赖**: Task 5.2完成

**验证步骤**:

**Step 1: 使用新密码登录**
1. [ ] 记录重置后的新密码（例如："TestPass123"）
2. [ ] 退出当前用户
3. [ ] 使用重置密码的用户名 + 新密码登录
4. [ ] 期望：登录成功

**Step 2: 检查数据库**
1. [ ] 打开SQL Server Management Studio
2. [ ] 连接到数据库（LYBTZYZS）
3. [ ] 查询Users表：
   ```sql
   SELECT Id, UserName, PasswordHash, UpdatedAt 
   FROM Users 
   WHERE UserName = '【重置密码的用户名】'
   ```
4. [ ] 验证：
   - PasswordHash字段已更新（BCrypt加密字符串）
   - UpdatedAt时间戳已更新
   - 长度约60字符（BCrypt标准长度）

**Step 3: 检查日志输出**
1. [ ] 检查Client端控制台日志
2. [ ] 验证日志包含：
   - "重置用户密码: UserId={UserId}"
   - "密码重置成功"
   - "用户 {UserName} 密码重置成功"
3. [ ] 检查Server端日志
4. [ ] 验证日志包含：
   - "重置用户密码"操作记录
   - 无异常错误

**验收标准**:
- [ ] 新密码登录成功
- [ ] 数据库PasswordHash字段已更新
- [ ] Client端日志完整
- [ ] Server端日志完整
- [ ] 无异常错误

---

## 提交阶段（2个任务，10分钟）

### Task 6.1: Git提交

**优先级**: P0  
**预估时间**: 5分钟  
**状态**: ⏳ Pending  
**依赖**: Task 5.1-5.3全部完成

**执行步骤**:

1. 检查修改的文件：
   ```bash
   git status
   ```

2. 添加文件到暂存区：
   ```bash
   git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs
   git add 【UserApi实现类路径】
   git add src/Client/Desktop/Modules/LYBT.Desktop.Users/Interfaces/IUserRepository.cs
   git add src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs
   git add src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Components/UserCommandHandler.cs
   git add src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/ResetPasswordDialogViewModel.cs
   git add src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs
   ```

3. 提交代码：
   ```bash
   git commit -m "feat(users): 完善用户管理 - 打通管理员重置用户密码功能

   修复内容：
   - 添加IUserApi.ResetPasswordAsync接口定义
   - 实现UserApi.ResetPasswordAsync方法（HTTP POST）
   - 添加IUserRepository.ResetPasswordAsync接口定义
   - 实现UserRepository.ResetPasswordAsync方法
   - 修复UserCommandHandler.ResetPasswordAsync方法（调用Repository）
   - 修复ResetPasswordDialogViewModel.ConfirmAsync方法（调用CommandHandler）
   - 修复UserManagementViewModel.ExecuteResetPasswordAsync方法（打开Dialog）

   影响范围：
   - Client端7个文件修改
   - 新增约150行代码
   - 打通完整调用链：ViewModel → CommandHandler → Repository → API → Server

   验证：
   - 编译通过（0 errors, 0 warnings）
   - 功能测试通过（手动输入/自动生成/错误处理）
   - 数据验证通过（数据库已更新、日志完整）
   - 新密码登录成功

   🤖 Generated with [Claude Code](https://claude.com/claude-code)

   Co-Authored-By: Claude <noreply@anthropic.com>"
   ```

4. 推送到远程仓库：
   ```bash
   git push origin master
   ```

**验收标准**:
- [ ] git status显示所有文件已暂存
- [ ] 提交信息符合Conventional Commits规范
- [ ] 提交信息包含完整的修复内容说明
- [ ] git push成功

---

### Task 6.2: 创建GitHub Issue（记录完成）

**优先级**: P1  
**预估时间**: 5分钟  
**状态**: ⏳ Pending  
**依赖**: Task 6.1完成

**Issue标题**:
```
[完成] 管理员工作台用户管理功能完善 - 重置用户密码
```

**Issue内容**:
```markdown
## 需求背景

完善管理员工作台中的"用户管理"功能，打通管理员重置用户密码的完整调用链。

## 实现内容

### 修改文件（7个）

1. ✅ **IUserApi.cs** - 添加ResetPasswordAsync接口定义
2. ✅ **UserApi实现类** - 添加ResetPasswordAsync方法实现
3. ✅ **IUserRepository.cs** - 添加ResetPasswordAsync接口定义
4. ✅ **UserRepository.cs** - 添加ResetPasswordAsync方法实现
5. ✅ **UserCommandHandler.cs** - 修复ResetPasswordAsync方法
6. ✅ **ResetPasswordDialogViewModel.cs** - 修复ConfirmAsync方法
7. ✅ **UserManagementViewModel.cs** - 修复ExecuteResetPasswordAsync方法

### 调用链

```
UserManagementView 
  → UserManagementViewModel.ExecuteResetPasswordAsync()
  → ResetPasswordDialog（UI）
  → ResetPasswordDialogViewModel.ConfirmAsync()
  → UserCommandHandler.ResetPasswordAsync()
  → UserRepository.ResetPasswordAsync()
  → UserApi.ResetPasswordAsync()
  → HTTP POST /api/users/{id}/reset-password
  → Server端处理（已实现）
```

### 功能验证

- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 场景1：手动输入密码重置成功
- ✅ 场景2：自动生成密码重置成功
- ✅ 场景3：错误处理（密码不一致/长度不足）
- ✅ 新密码登录成功
- ✅ 数据库PasswordHash字段已更新
- ✅ 日志完整（Client端和Server端）

### 工作量统计

- **代码量**: 约150行
- **文件数**: 7个
- **耗时**: 【实际耗时】小时
- **复杂度**: 低（重复模式，参考现有代码）

### 相关文档

- 需求讨论: `.requirements/discussions/admin-user-management-reset-password.md`
- 技术设计: `.design/admin-user-management-reset-password.md`
- 任务清单: `.tasks/admin-user-management-reset-password-tasks.md`

### 后续优化（Phase 2，可选）

1. ❌ 邮件/短信通知 - 重置密码后自动发送通知
2. ❌ 强制修改密码 - 用户下次登录时强制修改密码
3. ❌ 密码历史记录 - 防止重复使用旧密码
4. ❌ 密码强度检查 - 实时检查密码复杂度
5. ❌ 权限细化 - 验证SuperAdmin/Admin权限

## 提交记录

Commit SHA: 【待填写】
PR链接: 【如果有】

## 标签

`enhancement` `user-management` `security` `completed`
```

**执行步骤**:
1. 使用GitHub CLI或Web界面创建Issue
2. 填写Issue标题和内容
3. 添加标签：`enhancement`, `user-management`, `security`, `completed`
4. 关闭Issue（标记为已完成）

**验收标准**:
- [ ] Issue已创建
- [ ] Issue内容完整（包含修改文件、调用链、验证结果）
- [ ] Issue已添加标签
- [ ] Issue状态为Closed（已完成）

---

## 任务总览（CheckList）

### 准备阶段
- [ ] Task 0.1: 查找UserApi实现类位置
- [ ] Task 0.2: 验证ResetPasswordDialog注册

### 实现阶段
- [ ] Task 1.1: 修改IUserApi接口
- [ ] Task 1.2: 实现UserApi方法
- [ ] Task 2.1: 修改IUserRepository接口
- [ ] Task 2.2: 实现UserRepository方法
- [ ] Task 3.1: 修复UserCommandHandler.ResetPasswordAsync
- [ ] Task 4.1: 修复ResetPasswordDialogViewModel.ConfirmAsync
- [ ] Task 4.2: 修复UserManagementViewModel.ExecuteResetPasswordAsync

### 验证阶段
- [ ] Task 5.1: 编译验证（0 errors, 0 warnings）
- [ ] Task 5.2: 功能测试（3个场景）
- [ ] Task 5.3: 数据验证（数据库+日志）

### 提交阶段
- [ ] Task 6.1: Git提交（7个文件）
- [ ] Task 6.2: 创建GitHub Issue

---

**任务清单状态**: ✅ 已生成，等待执行

**预估总时间**: 1.5-2.5小时  
**实际总时间**: 【待完成后填写】
