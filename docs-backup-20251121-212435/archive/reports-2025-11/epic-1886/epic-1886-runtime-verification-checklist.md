# Epic #1886 运行时验证清单

**创建时间**: 2025-11-07
**验证范围**: 用户个人信息与密码修改功能
**Issues**: #1902 (sysadmin场景), #1903 (Doctor场景)

---

## 📋 验证前准备

### 环境要求

- [ ] SQL Server数据库已启动
- [ ] Server端（LYBT.WebAPI）已启动
- [ ] Client端（LYBT.Desktop）已启动
- [ ] 数据库中有测试用户数据

### 测试账户准备

**sysadmin账户**:
- 用户名: `sysadmin`
- 密码: （appsettings.json中配置）
- 特点: 虚拟账户，不在数据库中

**Doctor测试账户** (需提前创建):
- 用户名: `test_doctor`
- 真实姓名: `测试医生`
- 角色: `Doctor`
- 电话: `13800138000`
- 邮箱: `test@example.com`
- 密码: `Test@123456`

---

## ✅ Issue #1902: sysadmin场景验证

### 场景1: sysadmin登录验证

**步骤**:
1. 启动Client端应用
2. 使用sysadmin账户登录
3. 观察登录成功后的界面

**验收标准**:
- [ ] 登录成功，进入AdminHomeView
- [ ] Header栏显示"sysadmin"用户名
- [ ] 左侧导航栏显示管理员菜单

**预期结果**: ✅ 登录成功

---

### 场景2: sysadmin修改密码按钮可见性

**步骤**:
1. sysadmin登录成功后
2. 观察AdminHomeView中的按钮

**验收标准**:
- [ ] **"修改个人信息"按钮不可见**（sysadmin是虚拟账户，无个人信息）
- [ ] **"修改密码"按钮可见**

**预期结果**: ✅ 按钮可见性符合设计

**参考代码**: 
- `AdminHomeViewModel.cs` (line 40-54): `IsSysAdmin`属性默认为`true`
- `AdminHomeView.xaml` (line 78-89): `Visibility`绑定`IsNotSysAdmin`转换器

---

### 场景3: sysadmin修改密码成功流程

**步骤**:
1. 点击"修改密码"按钮
2. ChangePasswordDialog弹出
3. 输入以下信息：
   - 旧密码: （当前密码）
   - 新密码: `NewPass@123`
   - 确认密码: `NewPass@123`
4. 点击"确定"按钮

**验收标准**:
- [ ] Dialog标题显示"修改密码 - sysadmin"
- [ ] 输入框正常工作
- [ ] 密码验证规则生效（长度≥6，不能与旧密码相同，新密码=确认密码）
- [ ] 点击"确定"后Dialog关闭
- [ ] 显示成功消息："密码修改成功！请使用新密码重新登录。"
- [ ] **自动logout**（Token已清除）
- [ ] ⚠️ **UI导航到登录界面**（Issue #1906待实现）

**预期结果**: ✅ 密码修改成功，自动logout

**已知问题**: UI不会自动导航到登录界面（Issue #1906已创建跟踪）

---

### 场景4: sysadmin使用新密码重新登录

**步骤**:
1. 在登录界面输入：
   - 用户名: `sysadmin`
   - 密码: `NewPass@123`（新密码）
2. 点击"登录"

**验收标准**:
- [ ] 登录成功
- [ ] 进入AdminHomeView

**预期结果**: ✅ 新密码登录成功

---

### 场景5: 验证旧密码已失效

**步骤**:
1. 退出登录
2. 在登录界面输入：
   - 用户名: `sysadmin`
   - 密码: （旧密码）
3. 点击"登录"

**验收标准**:
- [ ] 登录失败
- [ ] 显示错误消息："用户名或密码错误"

**预期结果**: ✅ 旧密码无法登录

---

### 场景6: 验证AdminSecrets表已更新

**步骤**:
1. 打开SQL Server Management Studio
2. 执行查询：
   ```sql
   SELECT TOP 1 
       Username,
       PasswordHash,
       UpdatedAt
   FROM AdminSecrets
   WHERE Username = 'sysadmin'
   ORDER BY UpdatedAt DESC;
   ```

**验收标准**:
- [ ] PasswordHash字段已更新（与之前不同）
- [ ] UpdatedAt字段为最近时间

**预期结果**: ✅ AdminSecrets表已更新

---

## ✅ Issue #1903: Doctor场景验证

### 场景1: Doctor登录验证

**步骤**:
1. 启动Client端应用
2. 使用Doctor测试账户登录
3. 观察登录成功后的界面

**验收标准**:
- [ ] 登录成功，进入ClinicalHomeView
- [ ] Header栏显示Doctor真实姓名
- [ ] 左侧导航栏显示诊疗菜单

**预期结果**: ✅ 登录成功

---

### 场景2: Doctor修改个人信息按钮可见性

**步骤**:
1. Doctor登录成功后
2. 观察ClinicalHomeView中的按钮

**验收标准**:
- [ ] **"修改个人信息"按钮可见**
- [ ] **"修改密码"按钮可见**
- [ ] 两个按钮都可以点击

**预期结果**: ✅ 按钮可见且可用

---

### 场景3: Doctor修改个人信息成功流程

**步骤**:
1. 点击"修改个人信息"按钮
2. UserProfileDialog弹出
3. 修改以下信息：
   - 真实姓名: `测试医生（已修改）`
   - 电话号码: `13900139000`
   - 邮箱: `modified@example.com`
4. 点击"保存"按钮

**验收标准**:
- [ ] Dialog标题显示"修改个人信息"
- [ ] 当前信息正确显示
- [ ] 输入框可以编辑
- [ ] 电话号码格式验证生效（11位数字）
- [ ] 邮箱格式验证生效
- [ ] 点击"保存"后Dialog关闭
- [ ] 显示成功消息："个人信息修改成功"

**预期结果**: ✅ 个人信息修改成功

---

### 场景4: 验证Doctor个人信息已更新

**步骤**:
1. 刷新ClinicalHomeView或重新登录
2. 再次点击"修改个人信息"
3. 观察显示的信息

**验收标准**:
- [ ] 真实姓名显示为: `测试医生（已修改）`
- [ ] 电话号码显示为: `13900139000`
- [ ] 邮箱显示为: `modified@example.com`
- [ ] PinYinCode已自动更新（根据新姓名生成）

**预期结果**: ✅ 信息已更新

---

### 场景5: 验证Users表已更新

**步骤**:
1. 打开SQL Server Management Studio
2. 执行查询：
   ```sql
   SELECT 
       UserName,
       RealName,
       PhoneNumber,
       Email,
       PinYinCode,
       UpdatedAt
   FROM Users
   WHERE UserName = 'test_doctor';
   ```

**验收标准**:
- [ ] RealName字段为: `测试医生（已修改）`
- [ ] PhoneNumber字段为: `13900139000`
- [ ] Email字段为: `modified@example.com`
- [ ] PinYinCode字段已更新
- [ ] UpdatedAt字段为最近时间

**预期结果**: ✅ Users表已更新

---

### 场景6: Doctor修改密码成功流程

**步骤**:
1. 点击"修改密码"按钮
2. ChangePasswordDialog弹出
3. 输入以下信息：
   - 旧密码: `Test@123456`
   - 新密码: `NewTest@654321`
   - 确认密码: `NewTest@654321`
4. 点击"确定"按钮

**验收标准**:
- [ ] Dialog标题显示"修改密码 - test_doctor"
- [ ] 输入框正常工作
- [ ] 密码验证规则生效
- [ ] 点击"确定"后Dialog关闭
- [ ] 显示成功消息："密码修改成功！请使用新密码重新登录。"
- [ ] **自动logout**（Token已清除）
- [ ] ⚠️ **UI导航到登录界面**（Issue #1906待实现）

**预期结果**: ✅ 密码修改成功，自动logout

**已知问题**: UI不会自动导航到登录界面（Issue #1906已创建跟踪）

---

### 场景7: Doctor使用新密码重新登录

**步骤**:
1. 在登录界面输入：
   - 用户名: `test_doctor`
   - 密码: `NewTest@654321`（新密码）
2. 点击"登录"

**验收标准**:
- [ ] 登录成功
- [ ] 进入ClinicalHomeView
- [ ] 个人信息显示为修改后的信息

**预期结果**: ✅ 新密码登录成功

---

### 场景8: 验证旧密码已失效

**步骤**:
1. 退出登录
2. 在登录界面输入：
   - 用户名: `test_doctor`
   - 密码: `Test@123456`（旧密码）
3. 点击"登录"

**验收标准**:
- [ ] 登录失败
- [ ] 显示错误消息："用户名或密码错误"

**预期结果**: ✅ 旧密码无法登录

---

### 场景9: 验证Users表密码已更新

**步骤**:
1. 打开SQL Server Management Studio
2. 执行查询：
   ```sql
   SELECT 
       UserName,
       PasswordHash,
       UpdatedAt
   FROM Users
   WHERE UserName = 'test_doctor';
   ```

**验收标准**:
- [ ] PasswordHash字段已更新（与之前不同）
- [ ] UpdatedAt字段为最近时间
- [ ] PasswordHash是BCrypt哈希值（以`$2a$`或`$2b$`开头）

**预期结果**: ✅ Users表密码已更新

---

## ⚠️ 已知限制

### Issue #1906: 密码修改后UI不会自动导航到登录界面

**当前行为**:
- 密码修改成功
- 显示成功消息
- 自动logout（Token清除）
- ❌ UI保持在当前视图（AdminHomeView或ClinicalHomeView）

**期望行为**:
- ✅ 以上所有步骤
- ✅ **UI自动导航到登录界面**

**状态**: 已创建Issue #1906跟踪，作为后续改进

**影响**: 用户需要手动点击"登出"按钮或关闭应用重新打开才能看到登录界面

---

## 📊 验证总结模板

### Issue #1902验证结果

```
✅ sysadmin登录 - 通过
✅ 按钮可见性 - 通过
✅ 修改密码成功 - 通过
✅ 自动logout - 通过
⚠️ 自动导航到登录界面 - 未实现（Issue #1906）
✅ 新密码登录 - 通过
✅ 旧密码失效 - 通过
✅ AdminSecrets表更新 - 通过

**总体评估**: ✅ **通过**（除Issue #1906外，所有功能正常）
```

### Issue #1903验证结果

```
✅ Doctor登录 - 通过
✅ 按钮可见性 - 通过
✅ 修改个人信息 - 通过
✅ 信息持久化 - 通过
✅ Users表更新 - 通过
✅ 修改密码成功 - 通过
✅ 自动logout - 通过
⚠️ 自动导航到登录界面 - 未实现（Issue #1906）
✅ 新密码登录 - 通过
✅ 旧密码失效 - 通过
✅ Users表密码更新 - 通过

**总体评估**: ✅ **通过**（除Issue #1906外，所有功能正常）
```

---

**清单创建时间**: 2025-11-07
**预计验证时间**: 30-45分钟
**下一步**: 执行验证，根据结果关闭Issue #1902和#1903
