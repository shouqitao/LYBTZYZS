# 凌隐宝堂中医诊所系统 - 快速开始指南

> **系统状态**: ✅ UltraThink项目文档标准化完成 | ✅ 生产就绪 | ✅ 0错误 0警告

## 🚀 一键启动

### 方法1: 使用启动脚本（推荐）
```bash
# 1. 启动开发服务器
scripts\start-dev.bat

# 2. 启动桌面客户端
tools\start-system.bat
```

### 方法2: 手动启动
```bash
# 1. 启动后端API服务
dotnet run --project src/Server/Services/LYBT.WebAPI --urls "https://localhost:7001"

# 2. 启动WPF桌面客户端
dotnet run --project src/Client/Desktop/Shell
```

## 🔑 默认登录信息

- **用户名**: `sysadmin`
- **密码**: `Admin@123456`  
- **角色**: 系统管理员 (Admin)

> ⚠️ **安全提醒**: 生产环境请立即修改默认密码

## 🌐 系统访问地址

- **API服务**: https://localhost:7001
- **Swagger文档**: https://localhost:7001/swagger
- **健康检查**: https://localhost:7001/health
- **桌面客户端**: WPF应用程序自动启动

## 🧱 8个核心业务模块

登录后可以访问以下功能模块：

| 模块 | 功能描述 | 快速操作 |
|------|----------|----------|
| **用户管理** | 医生和管理员账户管理 | 创建医生账户 |
| **患者档案** | 患者基本信息和病历 | 登记新患者 |
| **医疗案例** | 诊疗流程管理容器 | 创建新医案 |
| **看诊诊断** | 中医四诊记录 | 望闻问切诊断 |
| **处方管理** | 智能配伍处方开具 | 开具中药处方 |
| **中药材库** | 药材信息价格管理 | 管理药材库 |
| **验方模板** | 经典验方库管理 | 创建验方模板 |
| **权限认证** | JWT认证和角色管理 | 权限分配 |

## 🏥 核心诊疗流程

```
1. 患者登记 (Patients) 
    ↓
2. 创建医案 (MedicalCase)
    ↓  
3. 中医四诊 (Consultation)
    ↓
4. 开具处方 (Prescriptions) [可选]
    ↓
5. 完成诊疗
```

## 🔧 环境要求

### 开发环境
- **操作系统**: Windows 10/11
- **IDE**: Visual Studio 2022 或 Visual Studio Code
- **.NET SDK**: .NET 8.0 或更高版本
- **数据库**: SQL Server 2019 或更高版本

### 运行时环境
- **.NET Runtime**: .NET 8.0 Desktop Runtime
- **数据库**: SQL Server Express (免费版)
- **端口**: 7001 (HTTPS), 5001 (HTTP)

## 🐛 常见问题解决

### 启动问题
```bash
# 问题1: 端口被占用
netstat -ano | findstr :7001
taskkill /PID <PID> /F

# 问题2: 数据库连接失败
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 问题3: 依赖包缺失
dotnet restore LYBT.All.sln
```

### 登录问题
- ✅ 确认API服务正在运行 (https://localhost:7001/health)
- ✅ 检查用户名密码是否正确
- ✅ 确认数据库中管理员账户已初始化

### 功能异常
- ✅ 检查浏览器控制台错误信息
- ✅ 查看API服务日志输出
- ✅ 确认所有模块依赖注入正确

## 📊 系统健康检查

访问健康检查端点验证系统状态：

```bash
# 检查API服务状态
curl -k https://localhost:7001/health

# 检查数据库连接
curl -k https://localhost:7001/health/database

# 检查缓存状态  
curl -k https://localhost:7001/health/cache
```

## 📚 更多文档

- **[完整项目总结](../ultrathink/ultrathink-complete-project-summary-20250823.md)** - 项目完整技术成果
- **[CLAUDE.md](../../CLAUDE.md)** - 主要开发指导文档
- **[开发标准](../development/DEVELOPMENT_STANDARDS_V2.md)** - 开发规范和最佳实践
- **[架构文档](../README.md)** - 系统架构和技术栈说明

## 🆘 技术支持

如果遇到问题，请按以下顺序处理：

1. **查阅文档**: 检查相关README和技术文档
2. **健康检查**: 验证系统各组件状态
3. **重启服务**: 尝试重启API服务和客户端
4. **查看日志**: 检查系统日志输出信息
5. **环境验证**: 确认.NET 8和SQL Server正常运行

---

**🎯 快速上手提示**: 系统采用现代化UltraThink架构，具备完整的中医诊疗业务流程。建议先熟悉基本的患者登记→创建医案→诊断记录→处方开具流程，然后逐步探索高级功能。