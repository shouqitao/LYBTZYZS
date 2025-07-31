# LYBT WebAPI Windows Server 2016 发布指南

## 📋 系统要求

- Windows Server 2016 或更高版本
- .NET 8.0 Runtime
- SQL Server 2016 或更高版本
- IIS 10.0（可选，用于托管）

## 📦 文件说明

### 核心发布脚本
- `publish-production.bat` - 主要的生产环境发布脚本，用于编译和打包应用程序
- `deploy-all.bat` - 完整部署脚本，包括数据库初始化和服务配置
- `auto-deploy.bat` - 自动化部署脚本，用于持续集成/持续部署

### 服务器管理脚本
- `server-deploy.bat` - 服务器端部署脚本
- `setup-server.bat` - 服务器环境初始化脚本
- `install-service.bat` - Windows服务安装脚本
- `health-check.bat` - 服务健康检查脚本

### 数据库脚本
- `database-manager.bat` - 数据库管理工具
- `init-database.bat` - 数据库初始化脚本
- `create-database.bat` - 创建数据库脚本
- `test-database-connections.bat` - 测试数据库连接

### 启动脚本
- `start.bat` - 标准启动脚本
- `start-simple.bat` - 简化启动脚本
- `start-httponly.bat` - 仅HTTP模式启动
- `start-with-db-select.bat` - 带数据库选择的启动脚本
- `manage.bat` - 管理控制台
- `diagnose.bat` - 诊断工具

## 🚀 快速开始

### 1. 安装 .NET 8.0 Runtime
```powershell
# 下载并安装 .NET 8.0 Runtime
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. 准备数据库
```batch
# 运行数据库初始化脚本
init-database.bat
```

### 3. 发布应用程序
```batch
# 运行发布脚本
publish-production.bat
```

### 4. 配置应用程序
编辑 `publish\appsettings.Production.json`：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=你的服务器;Database=LYBT;User Id=sa;Password=你的密码;TrustServerCertificate=True"
  },
  "Jwt": {
    "Secret": "你的JWT密钥（至少32个字符）",
    "Issuer": "LYBT",
    "Audience": "LYBT"
  }
}
```

### 5. 启动服务
```batch
# 方式1：直接运行
cd publish
start-production.bat

# 方式2：安装为Windows服务
install-service.bat
```

## 🔧 部署选项

### 选项1：控制台应用程序
- 使用 `start.bat` 或 `start-production.bat`
- 适合开发和测试环境
- 需要保持控制台窗口开启

### 选项2：Windows服务
- 使用 `install-service.bat` 安装服务
- 服务名称：LYBT.WebAPI
- 自动启动，后台运行
- 适合生产环境

### 选项3：IIS托管
1. 安装 IIS 和 ASP.NET Core Module
2. 创建新网站，指向发布目录
3. 配置应用程序池使用"无托管代码"
4. 设置环境变量 `ASPNETCORE_ENVIRONMENT=Production`

## 📝 配置说明

### 环境变量
- `ASPNETCORE_ENVIRONMENT`: 设置为 `Production`
- `ASPNETCORE_URLS`: 默认 `http://localhost:5000`

### 数据库连接
支持以下数据库：
- SQL Server（推荐）
- LocalDB（仅用于开发）

### 日志配置
日志文件位置：`logs\`
- `app-{Date}.log` - 应用程序日志
- `error-{Date}.log` - 错误日志

## 🛠️ 故障排除

### 1. 端口冲突
如果5000端口被占用，修改 `ASPNETCORE_URLS` 环境变量：
```batch
set ASPNETCORE_URLS=http://localhost:5001
```

### 2. 数据库连接失败
- 检查SQL Server服务是否运行
- 验证连接字符串
- 确保防火墙允许SQL Server端口（默认1433）
- 运行 `test-database-connections.bat` 测试连接

### 3. 权限问题
- 确保运行账户有写入日志目录的权限
- 数据库用户需要 db_owner 权限

### 4. 服务无法启动
- 检查事件查看器中的错误日志
- 运行 `diagnose.bat` 进行诊断
- 确保 .NET Runtime 已正确安装

## 🔒 安全建议

1. **更改默认密码**
   - 修改数据库sa密码
   - 设置强JWT密钥

2. **HTTPS配置**
   - 生产环境建议启用HTTPS
   - 配置SSL证书

3. **防火墙设置**
   - 仅开放必要端口
   - 限制数据库访问

4. **定期备份**
   - 配置数据库自动备份
   - 保存配置文件备份

## 📞 技术支持

如遇到问题，请：
1. 查看日志文件 `logs\`
2. 运行诊断工具 `diagnose.bat`
3. 检查事件查看器中的应用程序日志

## 🔄 更新部署

1. 备份当前版本和数据库
2. 停止服务或应用程序
3. 运行新版本的 `publish-production.bat`
4. 更新配置文件（如需要）
5. 重启服务

---

**版本**: 1.0  
**更新日期**: 2025-01-31  
**项目**: 凌隐宝堂中医诊所管理系统