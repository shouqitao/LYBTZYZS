# 登录功能测试指南

## 测试环境准备

1. 确保API服务运行在 https://localhost:7001
2. 确保数据库连接正常

## 测试步骤

### 测试1：管理员登录
1. 启动WPF客户端
2. 登录信息：
   - 用户名：`sysadmin`
   - 密码：`Admin@123456`
3. **预期结果**：登录后显示 **AdminMainView（系统管理界面）**
4. 标题栏应显示：`凌隐宝堂中医诊所诊疗系统 - [真实姓名] (系统管理员)`

### 测试2：医生用户登录
1. 启动WPF客户端
2. 使用医生账户登录（任何非sysadmin的用户）
3. **预期结果**：登录后显示 **ConsultationMainView（看诊界面）**
4. 标题栏应显示：`凌隐宝堂中医诊所诊疗系统 - [真实姓名] (医生)`

## 验证要点

✅ sysadmin账户登录 → 管理界面（AdminMainView）
✅ 医生账户登录 → 看诊界面（ConsultationMainView）
✅ 标题栏正确显示用户角色
✅ 界面切换流畅，无错误提示

## 问题排查

如果界面仍然不正确：
1. 检查编译是否成功
2. 确认视图文件存在：
   - `src/Frontend/Desktop/Modules/SystemManagement/Views/AdminMainView.xaml`
   - `src/Frontend/Desktop/Modules/Consultation/Views/ConsultationMainView.xaml`
3. 查看调试输出窗口的错误信息

## 修复说明

本次修复解决了原先硬编码导航到SafeHomeView的问题，现在系统会根据用户角色智能选择合适的主界面：
- 管理员看到管理功能
- 医生看到诊疗功能