# 用户管理功能需求说明书

**文档版本**: v1.0（待确认）  
**创建日期**: 2025-11-07  
**适用项目**: LYBTZYZS - 中医诊疗管理系统  
**需求状态**: 🟡 待用户确认

---

## 📋 目录

1. [概述与背景](#1-概述与背景)
2. [功能范围](#2-功能范围)
3. [权限模型](#3-权限模型)
4. [UI设计方案](#4-ui设计方案)
5. [数据完整性与安全](#5-数据完整性与安全)
6. [Server端设计](#6-server端设计)
7. [Client端设计](#7-client端设计)
8. [验证标准](#8-验证标准)
9. [MVP优先级](#9-mvp优先级)
10. [需要确认的问题](#10-需要确认的问题)

---

## 1. 概述与背景

### 1.1 功能定位

为系统管理员（sysadmin + Admin权限User）提供用户全生命周期管理功能，包括：
- 用户的创建、查询、编辑
- 用户密码管理（重置、强制修改）
- 用户状态管理（启用、禁用）
- 权限分级控制（sysadmin vs Admin）

### 1.2 背景

**当前系统现状**：
- ✅ **已完成**：用户自我管理功能（Epic #1886）
  - 修改个人信息（UserProfileDialog）
  - 修改密码（ChangePasswordDialog）
  - Token生命周期管理（4条核心规则）
  
- ⏸ **待实现**：管理员的用户管理功能
  - 创建/编辑系统用户
  - 重置用户密码
  - 管理用户状态和角色

### 1.3 设计约束

**架构约束**：
1. **三层对齐架构**：Server端（Repository-Service-Controller）+ Client端（ViewModel-Repository）
2. **MVP原则**：够用即好，避免过度设计
3. **sysadmin架构独立性**：
   - sysadmin是虚拟超级用户（类似Linux root）
   - ❌ 不在User表中
   - ✅ 仅在appsettings.json + AdminSecrets表配置
   - ⚠️ Admin权限User不能修改sysadmin
   - ⚠️ sysadmin密码丢失需要通过**密码重置工具**恢复

**技术约束**：
- .NET 8.0、WPF、Prism、EF Core
- SQL Server 2022
- JWT Bearer Authentication

---

## 2. 功能范围

### 2.1 核心功能（P0 - 必须实现）

#### 2.1.1 查询用户列表

**功能描述**：
- 显示系统中所有用户的列表
- 支持分页浏览（每页20条，可配置）
- 支持按UserName、RealName搜索
- 支持按Role、Status筛选

**权限差异**：
- **sysadmin**：查看所有用户（包括Admin权限User）
- **Admin权限User**：仅查看非Admin用户（Doctor、Nurse等）

**显示字段**：
| 字段 | 说明 | 宽度 |
|------|------|------|
| UserName | 用户名 | 120px |
| RealName | 真实姓名 | 100px |
| Role | 角色（枚举显示） | 80px |
| PhoneNumber | 电话号码 | 120px |
| Email | 邮箱地址 | 150px |
| Status | 状态（Enabled/Disabled） | 80px |
| LastLoginTime | 最后登录时间 | 150px |
| 操作 | 按钮组 | 180px |

#### 2.1.2 创建新用户

**功能描述**：
- 打开CreateUserDialog对话框
- 填写完整用户信息
- 初始密码从配置文件读取（统一默认密码）

**输入字段**：
```
必填字段：
- UserName（唯一，3-50字符，字母数字）
- RealName（2-50字符）
- Role（下拉选择：Admin、Doctor、Nurse等）

可选字段：
- PhoneNumber（11位，唯一）
- Email（格式验证，唯一）
- Remark（备注，最多500字符）

自动字段：
- PinYinCode（根据RealName自动生成，首字母大写，例如：张三→ZS，黄芪→HQ）
- Status（默认Enabled）
- 初始密码（从配置文件读取统一默认密码）
```

**配置文件示例**：
```json
// appsettings.json
{
  "UserManagement": {
    "DefaultPassword": "Admin123!@#"
  }
}
```

**权限差异**：
- **sysadmin**：可创建任何角色用户（包括Admin）
- **Admin权限User**：仅可创建非Admin用户

**验证规则**：
- ✅ UserName唯一性（数据库 + Server端验证）
- ✅ PhoneNumber唯一性（如果填写）
- ✅ Email唯一性（如果填写）
- ✅ 密码复杂度验证
- ⚠️ **架构保护**：禁止创建UserName="sysadmin"

**详细验证规则**（Q4/Q9/Q10确认）：

**密码复杂度**（Q4确认）：
```
必须满足：
- 至少8个字符
- 包含大写字母（A-Z）
- 包含小写字母（a-z）
- 包含数字（0-9）
- 包含特殊字符（!@#$%^&*()_+-=[]{}|;:,.<>?）

示例：
✅ 合格: Admin123!@#, Test@Pass1, MyP@ssw0rd
❌ 不合格: admin123 (缺少大写+特殊字符), Admin123 (缺少特殊字符), Short1! (少于8位)
```

**电话号码格式**（Q9确认）：
```
- 11位数字
- 只允许数字字符（0-9）
- 不允许空格、短横线等分隔符

示例：
✅ 合格: 13812345678, 18600001234
❌ 不合格: 138-1234-5678 (包含分隔符), 1381234567 (不足10位), 138123456789 (超过12位)
```

**邮箱格式**（Q10确认）：
```
- 标准邮箱格式验证（RFC 5322）
- 必须包含 @ 符号
- @ 前后必须有内容
- 域名部分必须包含至少一个点（.）

示例：
✅ 合格: user@example.com, admin@company.com.cn, test.user@mail.org
❌ 不合格: user@, @example.com, user@com (缺少点), user example.com (缺少@)
```

#### 2.1.3 编辑用户信息

**功能描述**：
- 打开EditUserDialog对话框
- 修改用户可变字段
- 保存后立即生效

**可编辑字段**：
```
可修改：
- RealName（真实姓名）
- PhoneNumber（电话号码）
- Email（邮箱地址）
- Role（角色）
- Status（状态：Enabled/Disabled）
- Remark（备注）

不可修改：
- UserName（唯一标识，创建后不可改）
- Id（主键）
- PasswordHash（通过"重置密码"功能修改）
- PinYinCode（自动根据RealName更新，首字母大写，允许重复）
```

**PinYinCode规则**（Q8确认）：
- 提取RealName的拼音首字母
- 首字母大写（例如：张三→ZS，黄芪→HQ）
- 允许重复（多个用户可以有相同的PinYinCode）
- 编辑RealName时自动重新生成

**权限差异**：
- **sysadmin**：可编辑任何用户（包括Admin权限User）
- **Admin权限User**：
  - ✅ 可编辑非Admin用户
  - ❌ 不能编辑其他Admin用户
  - ❌ 不能看到/操作sysadmin

**验证规则**：
- ✅ PhoneNumber唯一性（排除当前用户）
- ✅ Email唯一性（排除当前用户）
- ✅ 不能将自己的Role改为非Admin（防止锁定）

### 2.2 密码管理（P1 - 应该实现）

#### 2.2.1 重置用户密码

**功能描述**：
- 管理员强制重置用户密码
- 恢复为配置文件中的默认密码（统一默认密码）
- **关键安全操作**：撤销该用户所有RefreshToken（参考Token生命周期4条规则）

**UI流程**：
```
1. 点击用户列表的"重置密码"按钮
2. 弹出确认对话框："确认重置用户 [张三] 的密码？重置后密码将恢复为默认密码。"
3. 点击确认后：
   - Server端读取配置文件的默认密码
   - 更新用户PasswordHash
   - 撤销该用户所有RefreshToken（IsRevoked=true）
   - 记录安全审计日志（SecurityAuditLog表）
4. 显示成功提示：
   "密码重置成功！
   用户密码已恢复为默认密码，请告知用户使用默认密码登录。"
```

**密码重置后Token处理**（关键安全要求）：
```csharp
// UserService.ResetPasswordAsync
public async Task<Result> ResetPasswordAsync(Guid userId)
{
    // 1. 读取默认密码
    var defaultPassword = _configuration["UserManagement:DefaultPassword"];
    
    // 2. 更新PasswordHash
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
    
    // 3. ⭐ 关键：撤销所有RefreshToken
    var userTokens = await _dbContext.RefreshTokens
        .Where(t => t.UserId == userId && !t.IsRevoked)
        .ToListAsync();
    
    foreach (var token in userTokens)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "管理员重置密码";
    }
    
    // 4. 记录审计日志
    _dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
    {
        EventType = "PasswordReset",
        UserId = userId,
        Timestamp = DateTime.UtcNow,
        Details = $"管理员重置用户密码，所有Token已撤销"
    });
    
    await _dbContext.SaveChangesAsync();
    return Result.Success();
}
```

**权限差异**：
- **sysadmin**：可重置任何用户密码（包括Admin权限User）
- **Admin权限User**：
  - ✅ 可重置非Admin用户密码
  - ❌ 不能重置其他Admin用户密码
  - ❌ 不能重置sysadmin密码（sysadmin不可见）

**特殊情况**：
- ⚠️ sysadmin密码丢失：通过独立的**密码重置工具**（控制台应用或PowerShell脚本）直接操作AdminSecrets表

#### 2.2.2 用户修改密码后自动Logout（关键安全要求）

**功能描述**：
- 用户通过ChangePasswordDialog修改自己的密码
- **修改成功后自动logout**（Token会自动清除）
- 跳转到登录页，要求用户使用新密码重新登录

**实现方式**（整合auth-user-security-improvement-discussion.md需求）：
```csharp
// ChangePasswordDialogViewModel.ChangePasswordAsync
private async Task ChangePasswordAsync()
{
    // 1. 验证输入
    if (!ValidatePasswords()) return;
    
    // 2. 调用密码修改API
    var result = await _authService/UserRepository.ChangePasswordAsync(...);
    if (!result.Success) return;
    
    // 3. ⭐ 自动logout（会自动清除Server端和Client端的所有Token）
    await _authService.LogoutAsync();
    
    // 4. 显示成功消息
    await ShowSuccessMessageAsync("密码修改成功！\n\n请使用新密码重新登录。");
    
    // 5. 导航到登录页面
    _regionManager.RequestNavigate("MainRegion", "LoginView");
    RequestClose();
}
```

**Server端Token撤销**（必须实现）：
```csharp
// AuthService.ChangeSysAdminPasswordAsync / UserService.ChangePasswordAsync
public async Task<Result> ChangePasswordAsync(...)
{
    // 1. 验证旧密码 + 更新新密码Hash
    
    // 2. ⭐ 撤销所有RefreshToken
    var userTokens = await _dbContext.RefreshTokens
        .Where(t => t.UserId == userId && !t.IsRevoked)
        .ToListAsync();
    
    foreach (var token in userTokens)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "用户修改密码";
    }
    
    // 3. 记录审计日志
    _dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
    {
        EventType = "PasswordChanged",
        UserId = userId,
        Details = "用户密码修改，所有Token已撤销"
    });
    
    await _dbContext.SaveChangesAsync();
    return Result.Success();
}
```

**执行流程总结**：
1. 用户在ChangePasswordDialog中修改密码
2. Server端验证旧密码并更新为新密码Hash
3. Server端撤销所有RefreshToken（记录审计日志）
4. Client端自动执行logout（清除Client端Token缓存）
5. 显示成功消息并导航到登录页
6. 用户使用新密码重新登录

**安全原则**（来自auth-user文档）：
- ✅ **安全优先于便利性**：密码修改后自动logout，强制重新认证
- ✅ **Token自动清除**：logout会同时清除Server端和Client端的所有Token
- ✅ **审计记录完整**：SecurityAuditLog表记录所有密码修改事件

### 2.3 状态管理（P1 - 应该实现）

#### 2.3.1 启用/禁用用户

**功能描述**：
- 切换用户Status字段（Enabled ↔ Disabled）
- 禁用后用户无法登录
- 不删除用户数据，可随时恢复

**UI流程**：
```
1. 点击"禁用"按钮
2. 确认对话框："确认禁用用户 [张三]？禁用后该用户将无法登录系统。"
3. 点击确认后：
   - 更新Status = Disabled
   - 撤销该用户的所有活跃Token（调用Logout）
   - 刷新列表
```

**权限差异**：
- **sysadmin**：可禁用任何用户
- **Admin权限User**：仅可禁用非Admin用户

**安全保护**：
- ⚠️ 不能禁用自己（防止锁定）
- ⚠️ 禁用用户时自动撤销其Token

### 2.4 不包含的功能（P3 - 后续迭代）

以下功能**不在本次MVP范围内**：

```
❌ 首次登录强制修改密码（P3）
   - 需要User表新增 RequirePasswordChange 字段
   - 登录流程需要检测并强制弹出ChangePasswordDialog
   - 用户确认需求：MVP阶段不需要此功能
   - 决策依据：Q1确认 - "默认强制修改密码MVP阶段不需要"
   - 决策依据：Q11确认 - 首次登录强制修改密码移到P3

❌ 删除用户（软删除）（P2）
   - 涉及业务数据清理（病历、处方等）
   - 复杂的级联关系处理
   - 暂时用"禁用"替代
   - 决策依据：Q2确认 - 选择软删除方案（IsDeleted标志 + 可恢复）
   - 注：虽然已确认方案，但MVP阶段用"禁用"功能替代

❌ 批量操作（P2）
   - 批量启用/禁用
   - 批量删除
   - MVP阶段无此需求

❌ 导入/导出用户（P3）
   - Excel导入用户列表
   - CSV导出用户数据
   - 决策依据：Q3补充确认 - "用户数量不会很多，不需要导入导出功能"

❌ 详细活动日志查看（P2）
   - 用户操作审计记录
   - 登录历史详情
   - 应独立设计审计模块

❌ 用户分组/部门管理（P3）
   - 当前系统无此概念
   - 需求不明确

❌ 高级权限系统（RBAC）（P3）
   - 细粒度权限控制
   - 过度设计，违反MVP原则
```

---

## 3. 权限模型

### 3.1 sysadmin（超级管理员）

**定位**：虚拟超级用户，类似Linux root

**权限清单**：
```
✅ 查看所有用户（包括Admin权限User）
✅ 创建任何角色用户（包括Admin）
✅ 编辑任何用户信息（包括Admin）
✅ 重置任何用户密码（包括Admin）
✅ 启用/禁用任何用户（包括Admin）
✅ 修改任何用户角色（包括提升为Admin）
❌ 不能修改自己（sysadmin不在User表中）
```

**架构特性**：
- ❌ 不在User表中
- ✅ 认证走独立路径（appsettings.json + AdminSecrets表）
- ✅ SessionManager.CurrentUser = null 或 Id = Guid.Empty
- ⚠️ 密码丢失需要通过密码重置工具恢复

#### 3.1.1 sysadmin密码重置工具设计

**背景**：
- sysadmin密码存储在AdminSecrets表，无法通过UI重置
- Admin用户无法重置sysadmin密码（权限不足）
- 密码丢失时需要特殊恢复机制

**决策依据**：Q5确认 - "生成SQL语句的小工具，不直接修改数据库"

**工具类型**：Console App（独立控制台应用）

**实现方式**：
```csharp
// Tools/LYBT.Tools.SysAdminPasswordReset/Program.cs
using BCrypt.Net;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=======================================");
        Console.WriteLine("sysadmin 密码重置工具 v1.0");
        Console.WriteLine("=======================================");
        Console.WriteLine();
        
        // 1. 输入新密码（隐藏显示）
        Console.Write("请输入新密码: ");
        string newPassword = ReadPasswordFromConsole();
        
        Console.Write("确认新密码: ");
        string confirmPassword = ReadPasswordFromConsole();
        
        if (newPassword != confirmPassword)
        {
            Console.WriteLine("\n❌ 两次密码输入不一致！");
            return;
        }
        
        // 2. 验证密码复杂度
        if (!ValidatePasswordComplexity(newPassword))
        {
            Console.WriteLine("\n❌ 密码不符合复杂度要求！");
            Console.WriteLine("要求：至少8位，包含大小写字母、数字、特殊字符");
            return;
        }
        
        // 3. 生成BCrypt Hash
        string passwordHash = BCrypt.HashPassword(newPassword);
        
        // 4. 生成SQL语句
        Console.WriteLine();
        Console.WriteLine("=======================================");
        Console.WriteLine("生成的SQL语句（请在SSMS中执行）:");
        Console.WriteLine("=======================================");
        Console.WriteLine();
        
        string sql = $@"
USE [LYBTZYZS]
GO

BEGIN TRANSACTION;

-- 1. 更新sysadmin密码Hash
UPDATE [dbo].[AdminSecrets]
SET PasswordHash = '{passwordHash}',
    UpdatedAt = GETUTCDATE()
WHERE AdminUserName = 'sysadmin';

-- 2. 撤销所有sysadmin的RefreshToken（如果有）
UPDATE [dbo].[RefreshTokens]
SET IsRevoked = 1,
    RevokedAt = GETUTCDATE(),
    ReasonRevoked = 'sysadmin密码重置'
WHERE UserId = '00000000-0000-0000-0000-000000000000' -- sysadmin特殊ID
  AND IsRevoked = 0;

-- 3. 记录审计日志
INSERT INTO [dbo].[SecurityAuditLogs] (Id, EventType, UserId, Timestamp, Details)
VALUES (NEWID(), 'SysAdminPasswordReset', '00000000-0000-0000-0000-000000000000', GETUTCDATE(), 'sysadmin密码通过工具重置');

COMMIT TRANSACTION;
GO

PRINT '✅ sysadmin密码已重置';
GO
";
        
        Console.WriteLine(sql);
        Console.WriteLine();
        Console.WriteLine("=======================================");
        Console.WriteLine("⚠️  重要提醒:");
        Console.WriteLine("1. 请复制以上SQL语句到SSMS执行");
        Console.WriteLine("2. 确保连接到正确的数据库");
        Console.WriteLine("3. 执行后sysadmin需要重新登录");
        Console.WriteLine("=======================================");
    }
    
    static string ReadPasswordFromConsole()
    {
        string password = "";
        ConsoleKeyInfo key;
        
        do
        {
            key = Console.ReadKey(true);
            
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
        }
        while (key.Key != ConsoleKey.Enter);
        
        Console.WriteLine();
        return password;
    }
    
    static bool ValidatePasswordComplexity(string password)
    {
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) return false;
        return true;
    }
}
```

**使用流程**：
1. 运行工具：`LYBT.Tools.SysAdminPasswordReset.exe`
2. 输入新密码（隐藏显示）
3. 工具生成SQL语句
4. 在SSMS中执行生成的SQL
5. sysadmin使用新密码登录

**安全特性**：
- ✅ 密码输入时隐藏显示（显示为 *）
- ✅ 密码复杂度验证（8位+大小写+数字+特殊字符）
- ✅ 不直接修改数据库（生成SQL语句）
- ✅ 包含Token撤销逻辑
- ✅ 记录安全审计日志

### 3.2 Admin权限User（普通管理员）

**定位**：在User表中的管理员用户（Role = Admin）

**权限清单**：
```
✅ 查看非Admin用户（Doctor、Nurse等）
✅ 创建非Admin用户
✅ 编辑非Admin用户信息
✅ 重置非Admin用户密码
✅ 启用/禁用非Admin用户
✅ 修改非Admin用户角色（在Doctor/Nurse等之间切换）
❌ 不能查看其他Admin用户
❌ 不能操作其他Admin用户
❌ 不能创建Admin用户
❌ 不能查看/操作sysadmin（sysadmin对其不可见）
✅ 可以修改自己的个人信息（通过UserProfileDialog）
❌ 不能修改自己的角色（防止降级）
❌ 不能禁用自己（防止锁定）
```

### 3.3 权限验证实现

**Server端权限检查**：

```csharp
// UserService.cs
private bool CanManageUser(User? currentUser, User targetUser)
{
    // sysadmin可以操作所有用户
    if (currentUser == null || currentUser.Id == Guid.Empty)
        return true;
    
    // 不能操作自己的关键字段（Role、Status）
    if (currentUser.Id == targetUser.Id)
        return false; // 部分操作禁止
    
    // Admin只能操作非Admin用户
    if (currentUser.Role == UserRole.Admin)
    {
        if (targetUser.Role == UserRole.Admin)
            return false; // 不能操作其他Admin
        return true;
    }
    
    // 非管理员不能操作任何用户
    return false;
}
```

**Client端UI控制**：

```csharp
// UserManagementViewModel.cs
private bool CanEditUser(UserDto user)
{
    var currentUser = _sessionManager.CurrentUser;
    
    // sysadmin可以编辑所有用户
    if (currentUser == null || currentUser.Id == Guid.Empty)
        return true;
    
    // 不能编辑自己（通过UserProfileDialog修改个人信息）
    if (currentUser.Id == user.Id)
        return false;
    
    // Admin不能编辑其他Admin
    if (currentUser.Role == UserRole.Admin && user.Role == UserRole.Admin)
        return false;
    
    return true;
}
```

---

## 4. UI设计方案

### 4.1 整体架构（推荐方案）

**设计模式**：独立管理视图模式（参考Patients模块）

```
AdminHomeView（管理员主页）
  └─ [用户管理] 按钮
      ↓ 导航
UserManagementView（用户管理主视图）
  ├─ 顶部：搜索栏 + 筛选器
  ├─ 中部：用户列表（DataGrid）
  ├─ 右侧：[创建新用户] 按钮
  └─ 底部：分页控件
  
操作对话框：
  ├─ CreateUserDialog（创建用户）
  ├─ EditUserDialog（编辑用户）
  ├─ ResetPasswordDialog（重置密码）
  └─ ConfirmDialog（确认禁用/删除）
```

**优势**：
- ✅ 符合现有Patients模块设计模式（一致性）
- ✅ 功能清晰，易于扩展
- ✅ 主页不臃肿（独立导航）
- ✅ 可以独立开发和测试

### 4.2 UserManagementView（用户管理主视图）

**布局结构**：

```xml
<UserControl>
  <Grid>
    <!-- 顶部：搜索与筛选 -->
    <StackPanel Orientation="Horizontal">
      <TextBox PlaceholderText="搜索用户名/姓名..." />
      <ComboBox Header="角色筛选" ItemsSource="{Binding Roles}" />
      <ComboBox Header="状态筛选" ItemsSource="{Binding Statuses}" />
      <Button Content="搜索" Command="{Binding SearchCommand}" />
      <Button Content="重置" Command="{Binding ResetFilterCommand}" />
    </StackPanel>
    
    <!-- 中部：用户列表 -->
    <DataGrid ItemsSource="{Binding Users}" AutoGenerateColumns="False">
      <DataGrid.Columns>
        <DataGridTextColumn Header="用户名" Binding="{Binding UserName}" />
        <DataGridTextColumn Header="姓名" Binding="{Binding RealName}" />
        <DataGridTextColumn Header="角色" Binding="{Binding RoleDisplay}" />
        <DataGridTextColumn Header="电话" Binding="{Binding PhoneNumber}" />
        <DataGridTextColumn Header="邮箱" Binding="{Binding Email}" />
        <DataGridTextColumn Header="状态" Binding="{Binding StatusDisplay}" />
        <DataGridTextColumn Header="最后登录" Binding="{Binding LastLoginTime}" />
        
        <!-- 操作列 -->
        <DataGridTemplateColumn Header="操作">
          <DataGridTemplateColumn.CellTemplate>
            <DataTemplate>
              <StackPanel Orientation="Horizontal">
                <Button Content="编辑" 
                        Command="{Binding DataContext.EditUserCommand, RelativeSource={...}}"
                        CommandParameter="{Binding}"
                        Visibility="{Binding CanEdit, Converter={...}}" />
                        
                <Button Content="重置密码" 
                        Command="{Binding DataContext.ResetPasswordCommand, RelativeSource={...}}"
                        CommandParameter="{Binding}"
                        Visibility="{Binding CanResetPassword, Converter={...}}" />
                        
                <Button Content="禁用" 
                        Command="{Binding DataContext.DisableUserCommand, RelativeSource={...}}"
                        CommandParameter="{Binding}"
                        Visibility="{Binding IsEnabled, Converter={...}}" />
                        
                <Button Content="启用" 
                        Command="{Binding DataContext.EnableUserCommand, RelativeSource={...}}"
                        CommandParameter="{Binding}"
                        Visibility="{Binding IsDisabled, Converter={...}}" />
              </StackPanel>
            </DataTemplate>
          </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
      </DataGrid.Columns>
    </DataGrid>
    
    <!-- 右侧：操作按钮 -->
    <StackPanel>
      <Button Content="创建新用户" 
              Command="{Binding CreateUserCommand}"
              Style="{StaticResource AccentButtonStyle}" />
      <Button Content="刷新列表" 
              Command="{Binding RefreshCommand}" />
    </StackPanel>
    
    <!-- 底部：分页 -->
    <StackPanel Orientation="Horizontal">
      <Button Content="上一页" Command="{Binding PreviousPageCommand}" />
      <TextBlock Text="{Binding PageInfo}" />
      <Button Content="下一页" Command="{Binding NextPageCommand}" />
    </StackPanel>
  </Grid>
</UserControl>
```

### 4.3 CreateUserDialog（创建用户对话框）

**输入表单**：

```xml
<DialogHost Title="创建新用户">
  <Grid>
    <StackPanel>
      <!-- 基本信息 -->
      <TextBox Header="用户名*" Text="{Binding UserName}" />
      <TextBox Header="真实姓名*" Text="{Binding RealName}" />
      <ComboBox Header="角色*" ItemsSource="{Binding AvailableRoles}" 
                SelectedItem="{Binding SelectedRole}" />
      
      <!-- 联系方式 -->
      &lt;TextBox Header="电话号码" Text="{Binding PhoneNumber}" /&gt;
      &lt;TextBox Header="邮箱地址" Text="{Binding Email}" /&gt;
      
      <!-- 备注 -->
      <TextBox Header="备注" Text="{Binding Remark}" 
               MaxLength="500" AcceptsReturn="True" />
      
      <!-- 提示 -->
      &lt;TextBlock Foreground="Orange"&gt;
        提示：用户初始密码为系统默认密码，请告知用户登录后及时修改。
      &lt;/TextBlock&gt;
      
      <!-- 错误提示 -->
      <TextBlock Foreground="Red" Text="{Binding ErrorMessage}" 
                 Visibility="{Binding HasError, Converter={...}}" />
    </StackPanel>
    
    <!-- 按钮 -->
    <StackPanel Orientation="Horizontal">
      <Button Content="创建" Command="{Binding CreateCommand}" 
              Style="{StaticResource AccentButtonStyle}" />
      <Button Content="取消" Command="{Binding CancelCommand}" />
    </StackPanel>
  </Grid>
</DialogHost>
```

### 4.4 EditUserDialog（编辑用户对话框）

**输入表单**：

```xml
<DialogHost Title="编辑用户信息">
  <Grid>
    <StackPanel>
      <!-- 只读字段 -->
      <TextBlock Text="用户名" />
      <TextBox Text="{Binding UserName}" IsReadOnly="True" Background="LightGray" />
      
      <!-- 可编辑字段 -->
      <TextBox Header="真实姓名*" Text="{Binding RealName}" />
      <ComboBox Header="角色*" ItemsSource="{Binding AvailableRoles}" 
                SelectedItem="{Binding SelectedRole}" />
      <TextBox Header="电话号码" Text="{Binding PhoneNumber}" />
      <TextBox Header="邮箱地址" Text="{Binding Email}" />
      <ComboBox Header="状态" ItemsSource="{Binding AvailableStatuses}" 
                SelectedItem="{Binding SelectedStatus}" />
      <TextBox Header="备注" Text="{Binding Remark}" 
               MaxLength="500" AcceptsReturn="True" />
      
      <!-- 错误提示 -->
      <TextBlock Foreground="Red" Text="{Binding ErrorMessage}" />
    </StackPanel>
    
    <!-- 按钮 -->
    <StackPanel Orientation="Horizontal">
      <Button Content="保存" Command="{Binding SaveCommand}" />
      <Button Content="取消" Command="{Binding CancelCommand}" />
    </StackPanel>
  </Grid>
</DialogHost>
```

### 4.5 ResetPasswordDialog（重置密码确认对话框）

**确认对话框**（Q1/Q12决策 - 统一默认密码）：

```xml
&lt;MessageBox Title="确认重置密码"&gt;
  &lt;StackPanel&gt;
    &lt;TextBlock Text="确认重置用户 [张三] 的密码？" FontWeight="Bold" /&gt;
    &lt;TextBlock TextWrapping="Wrap" Margin="0,10,0,0"&gt;
      密码将恢复为系统默认密码。
      为确保安全，该用户的所有登录Token将被撤销，需要重新登录。
    &lt;/TextBlock&gt;
    &lt;TextBlock Foreground="Orange" Margin="0,10,0,0"&gt;
      ⚠️ 重置后请告知用户使用默认密码登录，并建议及时修改密码。
    &lt;/TextBlock&gt;
  &lt;/StackPanel&gt;
  
  &lt;StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0"&gt;
    &lt;Button Content="确认重置" Command="{Binding ConfirmCommand}" 
            Style="{StaticResource AccentButtonStyle}" /&gt;
    &lt;Button Content="取消" Command="{Binding CancelCommand}" /&gt;
  &lt;/StackPanel&gt;
&lt;/MessageBox&gt;
```

**成功提示**：

```csharp
// 重置成功后显示Toast或MessageBox
await _notificationService.ShowSuccessAsync(
    "密码重置成功！\n" +
    "用户密码已恢复为默认密码，所有Token已撤销。\n" +
    "请告知用户使用默认密码重新登录。"
);
```

---

## 5. 数据完整性与安全

### 5.1 唯一性约束

**数据库约束**：

```sql
CREATE UNIQUE INDEX UX_Users_UserName ON Users(UserName);
CREATE UNIQUE INDEX UX_Users_PhoneNumber ON Users(PhoneNumber) WHERE PhoneNumber IS NOT NULL;
CREATE UNIQUE INDEX UX_Users_Email ON Users(Email) WHERE Email IS NOT NULL;
```

**Server端验证**：

```csharp
// UserService.CreateAsync
public async Task<Result<UserDto>> CreateAsync(CreateUserInputDto dto)
{
    // 1. UserName唯一性
    if (await _dbContext.Users.AnyAsync(u => u.UserName == dto.UserName))
        return Result<UserDto>.Failure("用户名已存在");
    
    // 2. PhoneNumber唯一性（如果填写）
    if (!string.IsNullOrEmpty(dto.PhoneNumber))
    {
        if (await _dbContext.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            return Result<UserDto>.Failure("电话号码已被其他用户使用");
    }
    
    // 3. Email唯一性（如果填写）
    if (!string.IsNullOrEmpty(dto.Email))
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == dto.Email))
            return Result<UserDto>.Failure("邮箱地址已被其他用户使用");
    }
    
    // 4. 架构保护：禁止创建sysadmin
    if (dto.UserName?.ToLower() == "sysadmin")
        return Result<UserDto>.Failure("禁止创建sysadmin用户，sysadmin是虚拟超级用户");
    
    // ... 继续创建逻辑
}
```

### 5.2 密码安全

#### 5.2.1 密码复杂度要求

```csharp
// PasswordValidator.cs
public static bool IsStrongPassword(string password, out string errorMessage)
{
    if (string.IsNullOrWhiteSpace(password))
    {
        errorMessage = "密码不能为空";
        return false;
    }
    
    if (password.Length < 8)
    {
        errorMessage = "密码长度至少8个字符";
        return false;
    }
    
    if (password.Length > 20)
    {
        errorMessage = "密码长度不能超过20个字符";
        return false;
    }
    
    // 必须包含大写字母
    if (!password.Any(char.IsUpper))
    {
        errorMessage = "密码必须包含至少一个大写字母";
        return false;
    }
    
    // 必须包含小写字母
    if (!password.Any(char.IsLower))
    {
        errorMessage = "密码必须包含至少一个小写字母";
        return false;
    }
    
    // 必须包含数字
    if (!password.Any(char.IsDigit))
    {
        errorMessage = "密码必须包含至少一个数字";
        return false;
    }
    
    // 必须包含特殊字符
    var specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
    if (!password.Any(c => specialChars.Contains(c)))
    {
        errorMessage = "密码必须包含至少一个特殊字符（如 !@#$%^&*）";
        return false;
    }
    
    errorMessage = string.Empty;
    return true;
}
```

#### 5.2.2 默认密码配置（Q1/Q12确认）

**配置文件管理**（替代临时密码生成）：

```csharp
// appsettings.json 配置
{
  "UserManagement": {
    "DefaultPassword": "Admin123!@#"  // 统一默认密码
  }
}

// 读取配置
public class UserService
{
    private readonly IConfiguration _configuration;
    
    public UserService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    // 创建新用户时使用默认密码
    public async Task&lt;Result&gt; CreateAsync(CreateUserInputDto dto)
    {
        var defaultPassword = _configuration["UserManagement:DefaultPassword"];
        
        var user = new User
        {
            UserName = dto.UserName,
            RealName = dto.RealName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            // ... 其他字段
        };
        
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        
        return Result.Success();
    }
    
    // 重置密码时恢复为默认密码
    public async Task&lt;Result&gt; ResetPasswordAsync(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return Result.Failure("用户不存在");
        
        var defaultPassword = _configuration["UserManagement:DefaultPassword"];
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
        
        // 撤销所有RefreshToken
        // ... Token撤销逻辑（见Section 2.2.1）
        
        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }
}
```

**安全考虑**：
- ✅ 默认密码符合复杂度要求（8位+大小写+数字+特殊字符）
- ✅ 配置文件不纳入版本控制（.gitignore）
- ⚠️ 生产环境部署时修改默认密码
- ⚠️ 定期检查是否有用户仍使用默认密码

**注意**：首次登录强制修改密码功能已移至P3（Q11确认），MVP阶段不实现。

### 5.3 架构保护

**禁止sysadmin写入User表**：

```csharp
// UserRepository.cs
public async Task<User> SaveAsync(User user)
{
    // ⚠️ 架构保护：禁止sysadmin写入数据库
    if (user.UserName?.ToLower() == "sysadmin")
    {
        throw new InvalidOperationException(
            "sysadmin是虚拟超级用户，不允许保存到数据库。" +
            "请检查代码逻辑，确保sysadmin仅在appsettings.json + AdminSecrets表中配置。");
    }
    
    // 正常保存逻辑
    // ...
}
```

### 5.4 删除策略（暂不实现，供参考）

**选项A：软删除（推荐）**

```csharp
// 优势：数据不丢失，可恢复
// 缺点：需要维护IsDeleted字段，查询需要过滤

public async Task<Result> SoftDeleteAsync(Guid userId)
{
    var user = await _dbContext.Users.FindAsync(userId);
    if (user == null)
        return Result.Failure("用户不存在");
    
    // 软删除：标记IsDeleted = true
    user.IsDeleted = true;
    user.Status = CommonStatus.Disabled;
    await _dbContext.SaveChangesAsync();
    
    // 撤销该用户的所有Token
    await _authService.RevokeAllTokensAsync(userId);
    
    return Result.Success();
}
```

**选项B：硬删除 + 级联检查（复杂）**

```csharp
// 优势：数据库干净
// 缺点：可能导致业务数据孤立，需要复杂的级联逻辑

public async Task<Result> HardDeleteAsync(Guid userId)
{
    var user = await _dbContext.Users.FindAsync(userId);
    if (user == null)
        return Result.Failure("用户不存在");
    
    // 检查是否有关联业务数据
    var hasMedicalCases = await _dbContext.MedicalCases
        .AnyAsync(mc => mc.CreatedBy == userId || mc.UpdatedBy == userId);
    
    var hasPrescriptions = await _dbContext.Prescriptions
        .AnyAsync(p => p.CreatedBy == userId);
    
    if (hasMedicalCases || hasPrescriptions)
    {
        return Result.Failure(
            "该用户有关联的业务数据（病历、处方等），无法删除。" +
            "建议使用"禁用"功能代替删除。");
    }
    
    // 硬删除
    _dbContext.Users.Remove(user);
    await _dbContext.SaveChangesAsync();
    
    return Result.Success();
}
```

**MVP阶段建议**：
- ⏸ 暂不实现删除功能
- ✅ 使用"禁用"功能替代（Status = Disabled）
- ⏸ 后续根据实际需求决定是否实施软删除

---

## 6. Server端设计

### 6.1 新增DTO

```csharp
// UserDtos.cs

/// <summary>
/// 创建用户输入DTO
/// </summary>
public class CreateUserInputDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string RealName { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
    
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }
    
    [Required]
    public UserRole Role { get; set; }
    
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// &lt;summary&gt;
/// 重置密码响应DTO（Q1/Q12决策 - 统一默认密码）
/// &lt;/summary&gt;
public class ResetPasswordResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// &lt;summary&gt;提示信息（告知密码已恢复为默认密码）&lt;/summary&gt;
    public string? Hint { get; set; } = "密码已恢复为默认密码，请告知用户使用默认密码登录。";
}
```

### 6.2 UserService新增方法

```csharp
// IUserService.cs

/// <summary>
/// 创建新用户（管理员功能）
/// </summary>
Task<Result<UserDto>> CreateUserAsync(CreateUserInputDto dto, User? currentUser);

/// <summary>
/// 管理员编辑用户
/// </summary>
Task<Result<UserDto>> AdminUpdateUserAsync(Guid userId, UserInputDto dto, User? currentUser);

/// <summary>
/// 重置用户密码（管理员功能）
/// </summary>
Task<Result<ResetPasswordResultDto>> ResetUserPasswordAsync(Guid userId, User? currentUser);

/// <summary>
/// 启用/禁用用户
/// </summary>
Task<Result> SetUserStatusAsync(Guid userId, CommonStatus status, User? currentUser);

/// <summary>
/// 获取用户列表（分页、筛选）
/// </summary>
Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query, User? currentUser);
```

### 6.3 UsersController新增端点

```csharp
// UsersController.cs

/// <summary>
/// 创建新用户（管理员功能）
/// POST /api/users/admin/create
/// </summary>
[HttpPost("admin/create")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserInputDto dto)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.CreateUserAsync(dto, currentUser);
    return ToApiResponse(result);
}

/// <summary>
/// 管理员编辑用户
/// PUT /api/users/{id}/admin
/// </summary>
[HttpPut("{id}/admin")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<UserDto>>> AdminUpdateUser(Guid id, [FromBody] UserInputDto dto)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.AdminUpdateUserAsync(id, dto, currentUser);
    return ToApiResponse(result);
}

/// <summary>
/// 重置用户密码（管理员功能）
/// POST /api/users/{id}/reset-password
/// </summary>
[HttpPost("{id}/reset-password")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<ResetPasswordResultDto>>> ResetUserPassword(Guid id)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.ResetUserPasswordAsync(id, currentUser);
    return ToApiResponse(result);
}

/// <summary>
/// 启用/禁用用户
/// PATCH /api/users/{id}/status
/// </summary>
[HttpPatch("{id}/status")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse>> SetUserStatus(Guid id, [FromBody] SetUserStatusDto dto)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.SetUserStatusAsync(id, dto.Status, currentUser);
    return ToApiResponse(result);
}

/// <summary>
/// 获取用户列表（管理员功能）
/// GET /api/users/admin/list
/// </summary>
[HttpGet("admin/list")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] UserQueryDto query)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.GetUsersAsync(query, currentUser);
    return Ok(ApiResponse<PagedResult<UserDto>>.SuccessResponse(result));
}
```

---

## 7. Client端设计

### 7.1 UserManagementViewModel

```csharp
public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IDialogService _dialogService;
    
    // Commands
    public DelegateCommand CreateUserCommand { get; }
    public DelegateCommand<UserDto> EditUserCommand { get; }
    public DelegateCommand<UserDto> ResetPasswordCommand { get; }
    public DelegateCommand<UserDto> EnableUserCommand { get; }
    public DelegateCommand<UserDto> DisableUserCommand { get; }
    
    // 筛选条件
    public ObservableCollection<UserRole> AvailableRoles { get; }
    public UserRole? SelectedRoleFilter { get; set; }
    public CommonStatus? SelectedStatusFilter { get; set; }
    
    // 权限判断
    private bool CanEditUser(UserDto user)
    {
        var currentUser = _sessionManager.CurrentUser;
        if (currentUser == null || currentUser.Id == Guid.Empty)
            return true; // sysadmin
        
        if (currentUser.Id == user.Id)
            return false; // 不能编辑自己
        
        if (currentUser.Role == UserRole.Admin && user.Role == UserRole.Admin)
            return false; // Admin不能编辑其他Admin
        
        return true;
    }
    
    // 创建用户
    private async Task ExecuteCreateUserAsync()
    {
        var result = await _dialogService.ShowDialogAsync("CreateUserDialog");
        if (result.Result == ButtonResult.OK)
        {
            await RefreshAsync();
        }
    }
    
    // 重置密码
    private async Task ExecuteResetPasswordAsync(UserDto user)
    {
        var confirmResult = await _dialogService.ShowConfirmAsync(
            "确认重置密码",
            $"确认重置用户 [{user.RealName}] 的密码？\n系统将生成临时密码，用户下次登录需要修改密码。");
        
        if (confirmResult != ButtonResult.OK)
            return;
        
        var result = await _userRepository.ResetPasswordAsync(user.Id);
        if (result.Success)
        {
            // 显示临时密码
            await _dialogService.ShowDialogAsync("ShowTemporaryPasswordDialog", 
                new DialogParameters
                {
                    { "UserName", user.UserName },
                    { "RealName", user.RealName },
                    { "TemporaryPassword", result.Data.TemporaryPassword }
                });
        }
        else
        {
            await ShowErrorAsync(result.Message);
        }
    }
}
```

### 7.2 IUserRepository新增方法

```csharp
public interface IUserRepository
{
    /// <summary>
    /// 创建用户（管理员功能）
    /// </summary>
    Task<Result<UserDto>> CreateUserAsync(CreateUserInputDto dto);
    
    /// <summary>
    /// 管理员编辑用户
    /// </summary>
    Task<Result<UserDto>> AdminUpdateUserAsync(Guid userId, UserInputDto dto);
    
    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<Result<ResetPasswordResultDto>> ResetPasswordAsync(Guid userId);
    
    /// <summary>
    /// 设置用户状态
    /// </summary>
    Task<Result> SetUserStatusAsync(Guid userId, CommonStatus status);
    
    /// <summary>
    /// 获取用户列表（分页、筛选）
    /// </summary>
    Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query);
}
```

### 7.3 对话框ViewModels

#### CreateUserDialogViewModel

```csharp
public class CreateUserDialogViewModel : DialogViewModelBase
{
    // 输入属性
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string InitialPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public UserRole SelectedRole { get; set; }
    public string? Remark { get; set; }
    
    // 可选角色列表（根据当前用户权限过滤）
    public ObservableCollection<UserRole> AvailableRoles { get; }
    
    // Commands
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand CancelCommand { get; }
    
    // 验证
    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            SetError("请输入用户名");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(RealName))
        {
            SetError("请输入真实姓名");
            return false;
        }
        
        if (!PasswordValidator.IsStrongPassword(InitialPassword, out var error))
        {
            SetError(error);
            return false;
        }
        
        if (InitialPassword != ConfirmPassword)
        {
            SetError("两次输入的密码不一致");
            return false;
        }
        
        return true;
    }
    
    // 创建用户
    private async Task ExecuteCreateAsync()
    {
        if (!ValidateInput())
            return;
        
        SetIsBusy(true, "正在创建用户...");
        
        var result = await _userRepository.CreateUserAsync(new CreateUserInputDto
        {
            UserName = UserName,
            RealName = RealName,
            InitialPassword = InitialPassword,
            PhoneNumber = PhoneNumber,
            Email = Email,
            Role = SelectedRole,
            Remark = Remark
        });
        
        SetIsBusy(false);
        
        if (result.Success)
        {
            await ShowSuccessAsync($"用户 [{RealName}] 创建成功！");
            RequestClose(ButtonResult.OK);
        }
        else
        {
            SetError(result.Message);
        }
    }
}
```

---

## 8. 验证标准

### 8.1 编译验证

```bash
# 编译所有项目
dotnet build LYBT.All.sln -c Release --no-restore

# 预期结果
0 errors, 0 warnings
所有项目生成成功
```

### 8.2 运行时验证清单

#### 8.2.1 sysadmin场景

**测试步骤**：
1. ✅ sysadmin登录
2. ✅ 点击"用户管理"按钮 → 跳转到UserManagementView
3. ✅ 查看用户列表 → 显示所有用户（包括Admin权限User）
4. ✅ 创建Admin用户 → 成功创建
5. ✅ 编辑Admin用户信息 → 成功编辑
6. ✅ 重置Admin用户密码 → 成功重置并显示临时密码
7. ✅ 禁用Admin用户 → 成功禁用
8. ✅ 启用Admin用户 → 成功启用

#### 8.2.2 Admin权限User场景

**测试步骤**：
1. ✅ Admin用户登录
2. ✅ 点击"用户管理"按钮 → 跳转到UserManagementView
3. ✅ 查看用户列表 → 仅显示非Admin用户
4. ✅ 尝试创建Admin用户 → Role下拉列表中无Admin选项
5. ✅ 创建Doctor用户 → 成功创建
6. ✅ 编辑Doctor用户信息 → 成功编辑
7. ✅ 重置Doctor用户密码 → 成功重置并显示临时密码
8. ✅ 禁用Doctor用户 → 成功禁用
9. ❌ 列表中不显示其他Admin用户
10. ❌ 列表中不显示sysadmin

#### 8.2.3 首次登录场景

**测试步骤**：
1. ✅ 管理员创建新用户（UserName: testuser, 初始密码: Test123!@#）
2. ✅ 确认RequirePasswordChange = true
3. ✅ 用testuser登录
4. ✅ 系统强制弹出ChangePasswordDialog（模态对话框）
5. ✅ 提示"首次登录或密码已重置，请修改密码后继续"
6. ✅ 必须修改密码才能关闭对话框
7. ✅ 修改密码成功后，RequirePasswordChange = false
8. ✅ 跳转到主页

#### 8.2.4 密码重置场景

**测试步骤**：
1. ✅ 管理员重置testuser密码
2. ✅ 显示临时密码：Qw3!xYz9（示例）
3. ✅ 复制临时密码成功
4. ✅ 关闭对话框后无法再次查看临时密码
5. ✅ 用testuser + 临时密码登录
6. ✅ 系统强制弹出ChangePasswordDialog
7. ✅ 修改密码成功后进入系统

### 8.3 边界测试场景

#### 8.3.1 唯一性验证

```
✅ 创建用户时UserName重复 → 提示"用户名已存在"
✅ 创建用户时PhoneNumber重复 → 提示"电话号码已被其他用户使用"
✅ 创建用户时Email重复 → 提示"邮箱地址已被其他用户使用"
✅ 编辑用户时PhoneNumber改为已存在的号码 → 提示错误
✅ 编辑用户时Email改为已存在的邮箱 → 提示错误
```

#### 8.3.2 密码复杂度验证

```
❌ 初始密码"short1!" → 提示"密码长度至少8个字符"
❌ 初始密码"nouppercase123!" → 提示"密码必须包含至少一个大写字母"
❌ 初始密码"NOLOWERCASE123!" → 提示"密码必须包含至少一个小写字母"
❌ 初始密码"NoDigits!@#" → 提示"密码必须包含至少一个数字"
❌ 初始密码"NoSpecial123" → 提示"密码必须包含至少一个特殊字符"
✅ 初始密码"Valid123!@#" → 验证通过
```

#### 8.3.3 权限边界验证

```
❌ Admin用户尝试创建Admin角色用户 → Role下拉列表无Admin选项
❌ Admin用户尝试编辑其他Admin用户 → 列表中不显示（无编辑按钮）
❌ Admin用户尝试重置其他Admin密码 → 列表中不显示（无重置按钮）
❌ Admin用户尝试直接调用API编辑其他Admin → Server端返回403 Forbidden
❌ 用户尝试禁用自己 → 按钮禁用（UI控制）
❌ 用户尝试修改自己的Role → 按钮禁用（UI控制）
```

#### 8.3.4 架构保护验证

```
❌ 尝试创建UserName="sysadmin"的用户 → 提示"禁止创建sysadmin用户"
❌ Admin用户在列表中看到sysadmin → 不显示
❌ 数据库User表中存在sysadmin记录 → 启动时自动清理（防御机制）
```

---

## 9. MVP优先级

### P0（必须实现）- 第一期交付

```
✅ 查询用户列表（分页、搜索、筛选）
✅ 创建新用户（完整字段 + 初始密码）
✅ 编辑用户信息（可变字段）
✅ 权限控制（sysadmin vs Admin差异）
✅ UserManagementView（主视图）
✅ CreateUserDialog（创建对话框）
✅ EditUserDialog（编辑对话框）
```

**验收标准**：
- 管理员可以创建、查看、编辑用户
- 权限控制正确（sysadmin vs Admin）
- 唯一性约束生效
- 编译无错误、无警告

### P1（应该实现）- 第二期交付

```
✅ 重置用户密码（生成临时密码）
✅ 首次登录强制修改密码
✅ 启用/禁用用户（Status切换）
✅ ResetPasswordDialog（重置密码对话框）
✅ 密码复杂度验证
✅ 架构保护（禁止创建sysadmin）
```

**验收标准**：
- 管理员可以重置用户密码并获取临时密码
- 用户首次登录必须修改密码
- 禁用用户后无法登录
- 密码符合复杂度要求

### P2（可以延后）- 后续迭代

```
⏸ 删除用户（软删除/硬删除）
⏸ 批量操作（批量启用/禁用）
⏸ 导入/导出用户（Excel/CSV）
⏸ 详细活动日志查看
⏸ 密码历史记录（防止重复使用）
⏸ 账户锁定策略（失败次数限制）
```

**决策依据**：实际使用反馈、业务需求、ROI评估

### P3（不在MVP范围）

```
❌ 用户分组/部门管理
❌ 复杂权限系统（RBAC）
❌ 双因素认证（2FA）
❌ SSO单点登录
❌ LDAP集成
```

**原因**：过度设计，违反MVP原则，当前无业务需求

---

## 10. 需要确认的问题

请您对以下问题进行确认或提供反馈：

### 10.1 功能范围确认

**Q1**: P0（核心CRUD + 权限控制）和P1（密码管理 + 状态管理）的功能范围是否完整？是否有遗漏的核心功能？

**Q2**: 删除用户功能是否需要在MVP阶段实现？如果需要，倾向于软删除还是硬删除？

**Q3**: 是否需要批量操作（批量启用/禁用）？

### 10.2 权限模型确认

**Q4**: Admin权限User的权限边界是否合理？是否需要调整？
- 当前设计：Admin不能操作其他Admin，不能创建Admin
- 是否需要更细粒度的权限控制？

**Q5**: sysadmin密码丢失后的恢复方案是否明确？
- 当前方案：独立的密码重置工具（控制台应用/PowerShell脚本）
- 是否需要在本次开发中包含密码重置工具的开发？

### 10.3 UI设计确认

**Q6**: UserManagementView的UI设计方案（独立视图模式）是否认可？
- 是否有其他UI布局偏好？

**Q7**: 对话框的交互流程是否合理？
- CreateUserDialog：输入所有字段 + 初始密码
- EditUserDialog：修改可变字段
- ResetPasswordDialog：两步流程（确认 → 显示临时密码）

### 10.4 数据完整性确认

**Q8**: 唯一性约束是否完整？
- UserName：必须唯一 ✅
- PhoneNumber：可选，如果填写则唯一 ✅
- Email：可选，如果填写则唯一 ✅
- 是否有其他字段需要唯一性约束？

**Q9**: 密码复杂度要求是否合理？
- 8-20字符
- 至少1个大写、1个小写、1个数字、1个特殊字符
- 是否需要调整？

### 10.5 优先级确认

**Q10**: P0和P1的优先级划分是否认可？
- P0：核心CRUD + 权限控制
- P1：密码管理 + 状态管理
- 是否需要调整优先级？

### 10.6 技术细节确认

**Q11**: 首次登录强制修改密码的实现方式是否认可？
- 需要User表新增 `RequirePasswordChange` 字段
- 登录后检查此字段，如果为true则强制弹出ChangePasswordDialog

**Q12**: 临时密码生成规则是否合理？
- 8-12位随机密码
- 至少包含大小写、数字、特殊字符
- 去除易混淆字符（0、O、1、l、I等）

---

## 📝 附录

### A. 相关Issue

- Epic #1886：用户自我管理功能（已完成）
- Issue #1901-1904：验证任务（待完成）
- 新Issue：用户管理功能开发（待创建）

### B. 相关文档

- [Token管理架构](auth-user-security-improvement-discussion.md#25-token生命周期管理需求)
- [Client端架构总结](../client/current-architecture-summary.md)
- [MVP约束原则](../../../../.claude/explanation/mvp-philosophy.md)
- [三层架构指南](../../../../.claude/explanation/architecture-philosophy.md)

### C. 技术参考

- [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [JWT认证最佳实践](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [EF Core唯一索引](https://docs.microsoft.com/en-us/ef/core/modeling/indexes)
- [Prism对话框服务](https://prismlibrary.com/docs/dialogs.html)

---

**文档状态**: 🟡 待用户确认  
**下一步**: 用户确认需求后，生成技术设计文档和GitHub Issues

