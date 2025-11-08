# 用户信息修改功能 - 需求讨论

## 📋 需求背景

### 业务目标
实现用户自己修改个人资料的功能（场景一），满足不同用户角色的信息管理需求：
- **系统管理员（sysadmin）**：仅能修改登录密码（sysadmin不在Users表中，存储在AdminSecrets表，没有RealName等个人信息字段）
- **普通用户（Admin/Doctor角色）**：可以修改个人资料（真实姓名、电话、邮箱）和登录密码
- **重要说明**：本需求是"用户自己修改自己"，区别于"管理员在用户管理模块中修改其他用户"（场景二，另一个需求）

### 现有实现分析
**已有代码**：
- ✅ `UserProfileDialogViewModel.cs`：个人资料对话框UI已实现
- ✅ `ChangePasswordDto`：密码修改DTO已定义（含旧密码验证）
- ✅ `ChangeProfileDto`：个人资料修改DTO已定义
- ⚠️ **关键问题**：
  - SaveAsync方法当前是Mock实现（Line 381-382）
  - ChangeProfileDto包含Avatar和Bio字段，但User实体中不存在
  - 缺少Server端的ChangeProfileAsync服务方法
  - 缺少角色差异化的UI逻辑

**现有约束**：
- `UserInputDto`明确规定：密码在更新时禁止通过该DTO修改（Line 78-83）
- UserName在更新时不可修改（Line 71注释）

---

## 🎯 核心目标

1. **角色差异化修改**：根据用户角色显示不同修改界面
2. **安全密码修改**：必须验证旧密码才能设置新密码
3. **个人资料更新**：仅Doctor角色可修改RealName、PhoneNumber、Email、PinYinCode
4. **字段约束**：UserName字段全局禁止修改
5. **完整实现替代Mock**：实现真实的Client-Server通信

---

## 📐 关键业务规则

### BR-1: 角色识别与界面差异化
- **判断依据**：`SessionManager.CurrentUser.UserName == SystemConstants.SuperAdminUsername`（检查用户名是否为"sysadmin"）
- **sysadmin界面**：仅显示密码修改区域（旧密码、新密码、确认密码）
- **Admin/Doctor界面**：显示个人资料编辑区域 + 密码修改区域（可选）
- **重要说明**：sysadmin不能通过UserRole判断，因为UserRole枚举中没有SuperAdmin值，必须通过用户名检查

### BR-2: 字段修改权限
| 字段 | sysadmin | Admin/Doctor | 说明 |
|------|----------|--------------|------|
| UserName | ❌ 禁止 | ❌ 禁止 | 全局只读 |
| RealName | ❌ 禁止 | ✅ 允许 | 真实姓名（sysadmin无此字段） |
| PhoneNumber | ❌ 禁止 | ✅ 允许 | 电话号码（sysadmin无此字段） |
| Email | ❌ 禁止 | ✅ 允许 | 邮箱地址（sysadmin无此字段） |
| PinYinCode | ❌ 禁止 | ✅ 允许 | 拼音码（sysadmin无此字段，普通用户自动生成） |
| Password | ✅ 允许 | ✅ 允许 | 必须提供旧密码验证 |

### BR-3: 密码修改验证规则
- **必需字段**：OldPassword（旧密码）+ NewPassword（新密码）+ ConfirmNewPassword（确认密码）
- **验证逻辑**：
  1. 旧密码必须验证正确（Server端验证）
  2. 新密码长度：6-128字符
  3. 新密码与确认密码必须一致
  4. 新密码不能与旧密码相同（可选）

### BR-4: 个人资料验证规则
- **RealName**：必填，最大50字符
- **PhoneNumber**：可选，符合中国手机号格式（11位，以1开头）
- **Email**：可选，符合邮箱格式，最大100字符
- **PinYinCode**：可选，最大50字符

### BR-5: 提交策略
- **sysadmin提交**：仅调用 `AuthService.ChangeSysAdminPasswordAsync` API（如果填写了密码字段）
- **Admin/Doctor提交**：
  - 如果仅修改个人资料：调用个人资料修改API
  - 如果仅修改密码：调用密码修改API
  - 如果同时修改：依次调用两个API（事务性考虑）
- **重要说明**：sysadmin使用单独的密码修改API（不是Users表的API），密码存储在AdminSecrets表

---

## 🗂️ 数据模型讨论

### 现有实体对齐检查
**User实体字段**（`UserModel.cs`）：
```csharp
public string UserName { get; set; }      // ✅ 存在，不可修改
public string RealName { get; set; }      // ✅ 存在，可修改
public string? PhoneNumber { get; set; }  // ✅ 存在，可修改
public string? Email { get; set; }        // ✅ 存在，可修改
public string? PinYinCode { get; set; }   // ✅ 存在，可修改
public string PasswordHash { get; set; }  // ✅ 存在，通过DTO间接修改
```

**ChangeProfileDto问题**（`UserDtos.cs` Line 241-275）：
- ❌ `Avatar` 字段：User实体中不存在
- ❌ `Bio` 字段：User实体中不存在

**解决方案**：MVP阶段移除Avatar和Bio字段，仅支持RealName、PhoneNumber、Email修改

### DTO设计建议

#### 方案A：复用现有DTO（推荐）
```csharp
// 密码修改：复用现有ChangePasswordDto（完全符合需求）
public class ChangePasswordDto
{
    public Guid UserId { get; set; }
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmNewPassword { get; set; }
}

// 个人资料修改：简化ChangeProfileDto
public class ChangeProfileDto
{
    public Guid UserId { get; set; }
    public string RealName { get; set; }      // 必填
    public string? PhoneNumber { get; set; }  // 可选
    public string? Email { get; set; }        // 可选
    // 移除：Avatar、Bio（MVP阶段不支持）
    // PinYinCode由Server端自动生成，暂不支持手动修改
}
```

#### 方案B：新建统一DTO（备选）
```csharp
public class UpdateUserProfileDto
{
    public Guid UserId { get; set; }
    public string RealName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    
    // 密码修改（可选字段组）
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}
```

**推荐方案A理由**：
- 职责单一：密码修改和资料修改分离
- 复用现有代码：ChangePasswordDto已验证可用
- 符合Constitution：拒绝过度设计，简单直接

---

## 🔌 API设计考虑

### Client端调用逻辑
```csharp
// UserProfileDialogViewModel.SaveAsync()
private async Task SaveAsync()
{
    // 通过用户名判断是否为sysadmin（不能通过Role判断）
    bool isSysAdmin = _sessionManager?.CurrentUser?.UserName?.Equals(
        SystemConstants.SuperAdminUsername,  // "sysadmin"
        StringComparison.OrdinalIgnoreCase) == true;

    if (isSysAdmin)
    {
        // sysadmin仅修改密码（使用单独的API）
        if (!string.IsNullOrEmpty(OldPassword) && !string.IsNullOrEmpty(NewPassword))
        {
            var dto = new ChangeSysAdminPassword
            {
                OldPassword = OldPassword,
                NewPassword = NewPassword
            };
            await _authService.ChangeSysAdminPasswordAsync(dto);
        }
    }
    else
    {
        // Admin/Doctor修改个人资料
        bool profileChanged = HasProfileChanges();
        bool passwordChanged = !string.IsNullOrEmpty(NewPassword);

        if (profileChanged)
        {
            await _userService.ChangeProfileAsync(changeProfileDto);
        }

        if (passwordChanged)
        {
            await _userService.ChangePasswordAsync(changePasswordDto);
        }
    }
}
```

### Server端API需求
**sysadmin密码修改**：
1. `POST /api/auth/sysadmin/change-password` - sysadmin修改密码（**需实现**）
   - 当前 `AuthService.ChangeSysAdminPasswordAsync` 方法存在但返回"暂未实现"
   - 需实现：验证旧密码 → 使用BCrypt哈希新密码 → 更新AdminSecrets表

**普通用户API**（需验证是否存在）：
1. `POST /api/users/{id}/change-password` - 修改密码
2. `PUT /api/users/{id}/profile` - 修改个人资料（**需新建**）

**ChangeProfileAsync Service层方法**（需实现）：
```csharp
// IUserService接口
Task<OperationResult> ChangeProfileAsync(ChangeProfileDto dto);

// UserService实现
public async Task<OperationResult> ChangeProfileAsync(ChangeProfileDto dto)
{
    // 1. 验证用户存在
    // 2. 验证字段合法性
    // 3. 更新RealName、PhoneNumber、Email
    // 4. 自动更新PinYinCode（基于RealName）
    // 5. 保存修改
}
```

---

## 🎨 UI交互设计

### 入口设计（方案C：统一顶部用户信息栏）
**实施位置**：AdminHomeView 和 ClinicalHomeView 两个主页

**添加位置**：标题区域（"凌隐宝堂中医诊所" + "系统管理工作台/临床工作站"）下方

**XAML结构**：
```xaml
<!-- 在两个主页的标题区域后添加 -->
<Border Background="#F0F8FF" 
        BorderBrush="{StaticResource BorderBrush}"
        BorderThickness="0,1"
        Padding="20,12"
        Margin="0,20,0,20">
    <Grid>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="👤" FontSize="20" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBlock Text="当前用户: " FontSize="14" VerticalAlignment="Center"/>
            <TextBlock Text="{Binding CurrentUserName}" FontSize="14" FontWeight="SemiBold" VerticalAlignment="Center"/>
        </StackPanel>
        
        <Button Content="个人中心" 
                Command="{Binding OpenUserProfileCommand}"
                HorizontalAlignment="Right"
                Style="{StaticResource SecondaryButton}"/>
    </Grid>
</Border>
```

**优势**：
- ✅ 两个主页保持一致的UX
- ✅ 不影响现有卡片布局
- ✅ 用户信息始终可见
- ✅ 实施简单，代码复用度高

### 个人资料对话框布局差异
**sysadmin视图**：
```
┌──────────────────────────────┐
│  编辑个人资料                │
├──────────────────────────────┤
│  用户名：sysadmin （只读）    │
│                              │
│  【修改密码】                │
│  旧密码：[__________]        │
│  新密码：[__________]        │
│  确认密码：[__________]      │
│                              │
│     [保存]    [取消]         │
└──────────────────────────────┘
```

**Admin/Doctor视图**：
```
┌──────────────────────────────┐
│  编辑个人资料                │
├──────────────────────────────┤
│  用户名：zhang_doctor （只读）│
│  真实姓名：[__________]      │
│  电话号码：[__________]      │
│  邮箱地址：[__________]      │
│                              │
│  【修改密码】（可选）        │
│  旧密码：[__________]        │
│  新密码：[__________]        │
│  确认密码：[__________]      │
│                              │
│     [保存]    [取消]         │
└──────────────────────────────┘
```

### 交互逻辑
1. **主页入口点击**：
   - AdminHomeView/ClinicalHomeView 点击"个人中心"按钮
   - 触发 `OpenUserProfileCommand`
   - 弹出 `UserProfileDialog` 对话框

2. **对话框打开时**：
   - 根据 `SessionManager.CurrentUser.UserName == "sysadmin"` 决定显示哪些字段
   - 加载当前用户信息填充表单
   - UserName字段设为只读

3. **密码字段可见性**：
   - sysadmin：默认显示密码修改区域（唯一可修改内容）
   - Admin/Doctor：密码修改区域可折叠（Expander控件，默认折叠）

4. **保存按钮启用条件**：
   - sysadmin：至少填写了密码三字段（OldPassword + NewPassword + ConfirmNewPassword）
   - Admin/Doctor：个人资料有变更 OR 密码字段已填写

5. **验证反馈**：
   - 实时验证：电话号码格式（11位，以1开头）、邮箱格式
   - 提交验证：旧密码错误时显示错误提示
   - 成功提示："个人资料保存成功" / "密码修改成功"

---

## 🚧 技术约束与MVP范围

### Constitution约束
- ✅ **允许技术**：.NET 8.0, EF Core, WPF, Prism, xUnit
- ❌ **禁止技术**：Redis缓存、MediatR、过度DI抽象
- ✅ **设计原则**：简单直接、够用即好、拒绝过度设计

### MVP范围界定
**包含**：
- ✅ 角色差异化修改逻辑
- ✅ 密码修改（旧密码验证）
- ✅ 个人资料修改（RealName、PhoneNumber、Email）
- ✅ 字段验证（格式、长度）
- ✅ Server端API实现

**不包含**：
- ❌ 头像上传功能（Avatar字段）
- ❌ 个人简介（Bio字段）
- ❌ 邮箱验证（发送验证码）
- ❌ 手机号验证（发送短信）
- ❌ 修改历史记录
- ❌ PinYinCode手动编辑（Server端自动生成）

### 依赖服务需求
**Client端**：
- `IAuthService.ChangeSysAdminPasswordAsync(ChangeSysAdminPassword)` - **需新建接口定义**
- `IUserService.ChangePasswordAsync(ChangePasswordDto)` - 可能已存在
- `IUserService.ChangeProfileAsync(ChangeProfileDto)` - **需新建**

**Server端**：
- `AuthService.ChangeSysAdminPasswordAsync()` - **需实现**（当前仅返回"暂未实现"）
- `UserService.ChangePasswordAsync()` - 需验证是否存在
- `UserService.ChangeProfileAsync()` - **需新建**
- `AuthController.ChangeSysAdminPassword()` - **需新建**
- `UsersController.ChangePassword()` - 需验证是否存在
- `UsersController.ChangeProfile()` - **需新建**

---

## 📊 实施影响分析

### 代码变更范围
**修改文件**（约7个）：
1. `AdminHomeView.xaml` - 添加顶部用户信息栏
2. `AdminHomeViewModel.cs` - 添加 `OpenUserProfileCommand` 和 `CurrentUserName` 属性
3. `ClinicalHomeView.xaml` - 添加顶部用户信息栏
4. `ClinicalHomeViewModel.cs` - 添加 `OpenUserProfileCommand` 和 `CurrentUserName` 属性
5. `UserProfileDialogViewModel.cs` - 替换Mock逻辑，添加角色判断
6. `UserProfileDialog.xaml` - 添加角色差异化UI绑定（Expander控件）
7. `IUserService.cs` - 添加ChangeProfileAsync接口
8. `UserService.cs` - 实现ChangeProfileAsync方法
9. `UsersController.cs` - 添加ChangeProfile API端点

**新建文件**（0个）：
- 复用现有DTO（`ChangePasswordDto` 和 简化后的 `ChangeProfileDto`）

### 测试需求
**单元测试**（约7个）：
- `UserProfileDialogViewModel_SysAdmin_OnlyPasswordModification` - sysadmin仅密码修改
- `UserProfileDialogViewModel_Doctor_ProfileAndPasswordModification` - 普通用户资料+密码
- `AuthService_ChangeSysAdminPassword_Success` - sysadmin密码修改成功
- `AuthService_ChangeSysAdminPassword_WrongOldPassword_Fails` - 旧密码错误
- `UserService_ChangeProfile_Success` - 普通用户资料修改成功
- `UserService_ChangeProfile_InvalidEmail_ThrowsException` - 邮箱格式验证
- `ChangePasswordDto_Validation_OldPasswordRequired` - 密码DTO验证

**集成测试**（约2个）：
- 完整流程测试：打开对话框 → 修改信息 → 保存 → 验证数据库
- 角色切换测试：不同角色看到不同界面

---

## ⚠️ 风险与注意事项

### 安全风险
1. **旧密码验证**：必须在Server端验证，防止客户端绕过
2. **权限校验**：Server端API必须验证当前用户只能修改自己的信息
3. **密码强度**：虽然DTO已限制6-128字符，但未强制复杂度要求（MVP可暂不实现）

### 用户体验风险
1. **sysadmin混淆**：sysadmin可能期望修改其他信息，需在UI上明确说明"系统管理员仅支持密码修改（因为不在Users表中，无个人资料字段）"
2. **密码可选性**：Admin/Doctor修改个人资料时，密码字段应该是可选的（不修改密码时留空）
3. **保存反馈**：分别调用两个API时，需明确告知用户哪部分成功/失败
4. **sysadmin识别**：确保SessionManager中sysadmin登录后CurrentUser.UserName正确存储为"sysadmin"

### 技术债务
1. **ChangeProfileDto不一致**：Avatar和Bio字段应该移除或明确标记为"未来功能"
2. **PinYinCode自动生成**：当前逻辑未明确，需确认是否在Server端自动更新

---

## 📝 后续设计阶段关键决策点

1. **DTO选择**：复用现有DTO vs 新建统一DTO？
2. **API端点设计**：RESTful路径规范（`/api/users/{id}/profile` vs `/api/users/{id}`）
3. **PinYinCode处理**：用户修改RealName时，是否自动重新生成PinYinCode？
4. **事务处理**：同时修改资料+密码时，如果密码修改失败，是否回滚资料修改？
5. **UI框架选择**：密码字段折叠使用Expander还是TabControl？

---

**文档版本**：v1.0  
**创建日期**：2025-11-07  
**状态**：待用户确认  
**下一步**：进入DesignGeneration阶段（需用户批准）
