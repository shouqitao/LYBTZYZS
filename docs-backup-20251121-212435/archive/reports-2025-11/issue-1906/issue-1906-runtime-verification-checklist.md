# Issue #1906 运行时验证清单

**Issue**: https://github.com/shouqitao/LYBTZYZS/issues/1906

**功能**: 密码修改后自动导航到登录界面

**实施日期**: 2025-11-07

**实施方案**: EventAggregator模式（方案B）

---

## ✅ 代码变更总结

### 变更文件（4个）

1. **PasswordChangedEvent.cs** （新建）
   - 路径: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Events/PasswordChangedEvent.cs`
   - 内容: Prism事件类，用于通知密码修改成功

2. **ChangePasswordDialogViewModel.cs** （修改）
   - 路径: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/ChangePasswordDialogViewModel.cs`
   - 变更:
     - 添加`using LYBT.Desktop.Infrastructure.Events;`
     - 移除`NavigationManager`相关代码
     - 修改sysadmin分支：发布`PasswordChangedEvent`事件（line 227）
     - 修改普通用户分支：发布`PasswordChangedEvent`事件（line 261）

3. **MainWindowViewModel.cs** （修改）
   - 路径: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
   - 变更:
     - 在`InitializeEvents`中订阅`PasswordChangedEvent`（line 248）
     - 添加`OnPasswordChanged`事件处理方法（lines 353-366）

4. **LYBT.Desktop.Users.csproj** （未变更）
   - 无需添加项目引用（避免循环依赖）

### 编译结果

```
✅ 编译成功
0 errors
0 warnings
编译时间: 18.91秒
```

---

## 🧪 测试场景1：Doctor账户修改密码

### 前置条件
- [ ] Server端已启动（`LYBT.WebAPI`）
- [ ] Client端已启动（`LYBT.Desktop.Shell`）
- [ ] Doctor账户已登录（例如：`doctor1`）
- [ ] 系统显示`ClinicalHomeView`

### 测试步骤
1. [ ] 点击"修改密码"按钮
2. [ ] 输入旧密码（当前密码）
3. [ ] 输入新密码（至少6位）
4. [ ] 确认密码（与新密码一致）
5. [ ] 点击"确定"按钮

### 预期结果
- [ ] ✅ 显示"密码修改成功！请使用新密码重新登录。"消息
- [ ] ✅ 对话框自动关闭
- [ ] ✅ **UI自动跳转到LoginView**（关键验证点）
- [ ] ✅ 系统已退出登录状态（无用户信息显示）
- [ ] ✅ 使用旧密码无法登录（显示"密码错误"）
- [ ] ✅ 使用新密码可以成功登录

### 日志验证
查看Client日志，应包含：
```
[Info] 用户 doctor1 密码修改成功，准备自动logout
[Info] 用户 doctor1 已自动logout并发布密码修改事件
[Info] 收到密码修改成功事件，导航到登录界面
```

### 数据库验证
```sql
-- 验证密码哈希已更新
SELECT Id, UserName, PasswordHash, UpdatedAt
FROM Users
WHERE UserName = 'doctor1';
-- 预期：PasswordHash已变化，UpdatedAt为当前时间
```

---

## 🧪 测试场景2：sysadmin账户修改密码

### 前置条件
- [ ] Server端已启动
- [ ] Client端已启动
- [ ] sysadmin账户已登录
- [ ] 系统显示`AdminHomeView`

### 测试步骤
1. [ ] 点击"修改密码"按钮
2. [ ] 输入旧密码（当前sysadmin密码）
3. [ ] 输入新密码（至少6位）
4. [ ] 确认密码（与新密码一致）
5. [ ] 点击"确定"按钮

### 预期结果
- [ ] ✅ 显示"密码修改成功！请使用新密码重新登录。"消息
- [ ] ✅ 对话框自动关闭
- [ ] ✅ **UI自动跳转到LoginView**（关键验证点）
- [ ] ✅ 系统已退出登录状态
- [ ] ✅ 使用旧密码无法登录
- [ ] ✅ 使用新密码可以成功登录

### 日志验证
查看Client日志，应包含：
```
[Info] sysadmin 密码修改成功，准备自动logout
[Info] sysadmin 已自动logout并发布密码修改事件
[Info] 收到密码修改成功事件，导航到登录界面
```

### 数据库验证
```sql
-- 验证AdminSecrets表中PasswordHash已更新
SELECT Id, AdminSecretKey, PasswordHash, UpdatedAt
FROM AdminSecrets
WHERE AdminSecretKey = 'SysAdmin';
-- 预期：PasswordHash已变化，UpdatedAt为当前时间
```

---

## 🧪 测试场景3：密码修改失败（旧密码错误）

### 前置条件
- [ ] Doctor账户已登录

### 测试步骤
1. [ ] 点击"修改密码"按钮
2. [ ] 输入**错误的**旧密码
3. [ ] 输入新密码
4. [ ] 确认密码
5. [ ] 点击"确定"按钮

### 预期结果
- [ ] ❌ 显示"旧密码错误"或类似错误消息
- [ ] ✅ 对话框保持打开
- [ ] ✅ **UI未导航到LoginView**（关键验证点）
- [ ] ✅ 用户仍保持登录状态
- [ ] ✅ 可以继续尝试修改密码

### 日志验证
查看Client日志，应**不包含**：
```
[Info] 收到密码修改成功事件，导航到登录界面
```

---

## 🧪 测试场景4：密码修改时网络错误

### 前置条件
- [ ] Doctor账户已登录
- [ ] **断开Server端连接**（停止LYBT.WebAPI）

### 测试步骤
1. [ ] 点击"修改密码"按钮
2. [ ] 输入正确的旧密码
3. [ ] 输入新密码
4. [ ] 确认密码
5. [ ] 点击"确定"按钮

### 预期结果
- [ ] ❌ 显示网络错误或连接失败消息
- [ ] ✅ 对话框保持打开
- [ ] ✅ **UI未导航到LoginView**（关键验证点）
- [ ] ✅ 用户仍保持登录状态

---

## 🧪 测试场景5：新密码格式验证

### 测试步骤
1. [ ] 点击"修改密码"按钮
2. [ ] 输入正确的旧密码
3. [ ] 输入短密码（少于6位）
4. [ ] 确认密码（与新密码一致）
5. [ ] 点击"确定"按钮

### 预期结果
- [ ] ❌ 显示"密码长度至少6位"或类似错误
- [ ] ✅ **UI未导航到LoginView**
- [ ] ✅ 用户仍保持登录状态

---

## ⚡ 性能测试

### 测试场景6：导航响应时间

**测试步骤**:
1. [ ] Doctor账户修改密码
2. [ ] 记录从显示"密码修改成功"消息到LoginView完全渲染的时间

**性能目标**:
- [ ] ✅ 导航响应时间 < 500ms

**测量方法**:
查看日志时间戳：
```
[HH:mm:ss.fff] 用户 doctor1 已自动logout并发布密码修改事件
[HH:mm:ss.fff] 收到密码修改成功事件，导航到登录界面
```

---

## 📊 验收标准总结

### 功能验收（必须全部通过）
- [ ] Doctor修改密码后，UI自动跳转到LoginView
- [ ] sysadmin修改密码后，UI自动跳转到LoginView
- [ ] 旧密码无法登录（Token已失效）
- [ ] 新密码可以正常登录
- [ ] 密码修改失败时，不导航到登录界面
- [ ] 网络错误时，不导航到登录界面

### 代码质量（已验证）
- [x] 编译通过（0 errors, 0 warnings）
- [x] 符合EventAggregator模式
- [x] 代码注释清晰（Issue #1906）
- [x] 日志输出完整
- [x] 避免循环依赖

### 性能指标
- [ ] 导航响应时间 < 500ms
- [ ] UI线程无阻塞
- [ ] 无内存泄漏

---

## 🐛 问题记录

### 已解决问题

**问题1**: 循环依赖错误
- **描述**: 添加`LYBT.Desktop.Shell`项目引用后出现循环依赖
- **根因**: Shell已引用Users模块
- **解决方案**: 采用EventAggregator模式，移除项目引用
- **状态**: ✅ 已解决

---

## 📝 验证结果

### 执行人员
- [ ] 姓名: __________
- [ ] 日期: __________

### 整体评估
- [ ] ✅ 所有功能测试通过
- [ ] ✅ 所有边界测试通过
- [ ] ✅ 性能测试通过
- [ ] ✅ 无新增Bug

### 验证结论
- [ ] **通过** - 可以关闭Issue #1906
- [ ] **不通过** - 需要修复（记录问题到下方）

### 待修复问题（如有）
```
1. 问题描述：
   重现步骤：
   预期结果：
   实际结果：

2. 问题描述：
   ...
```

---

## 📚 相关Issue

- **Epic #1886**: 用户个人信息与密码修改功能
- **Issue #1887-1892**: 密码修改功能实现（已完成）
- **Issue #1893-1896**: UI更新（已完成）
- **Issue #1906**: 密码修改后自动导航到登录界面（当前Issue）

---

**文档生成时间**: 2025-11-07

**下一步**: 执行运行时验证，完成所有测试场景，确认功能正常后关闭Issue #1906。
