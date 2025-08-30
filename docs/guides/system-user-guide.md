# 凌隐宝堂中医诊所管理系统 - 使用说明

## 🚀 快速启动

### 方法一：一键启动（推荐）
双击运行 **`启动凌隐宝堂系统.bat`** 文件即可自动启动整个系统。

### 方法二：手动启动
1. 启动后端API：
   ```
   dotnet run --project src/Backend/Services/LYBT.WebAPI --urls "https://localhost:7001"
   ```
2. 启动前端应用：
   ```
   双击 src\Frontend\Desktop\Shell\bin\Debug\net8.0-windows\LYBT.WPF.Client.Shell.exe
   ```

## 🔐 登录信息

- **管理员账号**
  - 用户名：`sysadmin`
  - 密码：`Admin@123456`

- **普通用户账号**
  - 用户名：`shouqitao`
  - 密码：`ChangeMe123`

## 📱 系统功能模块

### 管理员功能
1. **用户管理** - 管理系统用户账号
2. **患者档案** - 管理患者基本信息
3. **药材管理** - 管理中药材信息和价格
4. **验方管理** - 管理经典验方模板
5. **系统设置** - 配置系统参数
6. **数据备份** - 备份和恢复数据

### 医生功能
1. **患者接待** - 接待新患者或查询老患者
2. **开始看诊** - 进行中医四诊
3. **开具处方** - 开具中药处方
4. **医疗案例** - 查看历史诊疗记录

## ⚠️ 注意事项

1. **首次使用**：
   - 系统会自动初始化数据库
   - 确保SQL Server服务已启动

2. **运行要求**：
   - Windows 10 或更高版本
   - .NET 8.0 Runtime
   - SQL Server 2012 或更高版本

3. **常见问题**：
   - 如果登录失败，请检查API服务是否正常运行（https://localhost:7001）
   - 如果界面显示异常，请尝试重启应用程序

## 📞 技术支持

如遇到问题，请查看以下日志文件：
- API日志：`src\Backend\Services\LYBT.WebAPI\bin\Debug\net8.0\logs`
- 数据库连接：检查SQL Server服务是否正常

## 🎯 今日修复内容

1. ✅ 修复了登录失败问题（API响应格式不匹配）
2. ✅ 修复了主页空白问题（依赖注入导致）
3. ✅ 创建了完整功能主页界面
4. ✅ 修复了CancellationTokenSource处置异常
5. ✅ 创建了一键启动脚本

## 📌 系统架构

- **后端**：ASP.NET Core 8.0 Web API
- **前端**：WPF + Prism MVVM
- **数据库**：SQL Server
- **认证**：JWT Token

---
*系统版本：v1.0.0 | 更新日期：2025-01-09*