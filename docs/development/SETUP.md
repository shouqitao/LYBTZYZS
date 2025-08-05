# 开发环境设置指南

## 目录

1. [系统要求](#系统要求)
2. [必需软件](#必需软件)
3. [环境配置](#环境配置)
4. [获取代码](#获取代码)
5. [项目构建](#项目构建)
6. [数据库设置](#数据库设置)
7. [运行项目](#运行项目)
8. [开发工具配置](#开发工具配置)
9. [常见问题](#常见问题)

## 系统要求

### 最低要求

- **操作系统**: Windows 10 (1809+) / Windows 11
- **处理器**: Intel Core i5 或 AMD Ryzen 5 同等性能
- **内存**: 8GB RAM
- **硬盘**: 10GB 可用空间
- **显卡**: 支持 DirectX 11

### 推荐配置

- **操作系统**: Windows 11 最新版本
- **处理器**: Intel Core i7 或 AMD Ryzen 7 同等性能
- **内存**: 16GB RAM 或更高
- **硬盘**: SSD 硬盘，20GB 可用空间
- **显卡**: 独立显卡，支持 DirectX 12

## 必需软件

### 1. .NET SDK

安装 .NET 8.0 SDK 或更高版本：

```bash
# 下载地址
https://dotnet.microsoft.com/download/dotnet/8.0

# 验证安装
dotnet --version
# 应显示: 8.0.x 或更高版本
```

### 2. Visual Studio 2022

推荐使用 Visual Studio 2022 Community 或更高版本：

- **下载地址**: https://visualstudio.microsoft.com/
- **必需工作负载**:
  - ASP.NET 和 Web 开发
  - .NET 桌面开发
  - 数据存储和处理

### 3. SQL Server

安装 SQL Server 2019 或更高版本：

- **下载地址**: https://www.microsoft.com/sql-server/sql-server-downloads
- **推荐版本**: SQL Server 2019 Express (免费)
- **管理工具**: SQL Server Management Studio (SSMS)

### 4. Git

安装 Git 版本控制工具：

```bash
# 下载地址
https://git-scm.com/download/windows

# 验证安装
git --version
```

### 5. 其他推荐工具

- **Postman**: API 测试工具
- **Visual Studio Code**: 轻量级编辑器
- **PowerShell 7+**: 增强的命令行工具

## 环境配置

### 1. 配置 Git

```bash
# 设置用户信息
git config --global user.name "您的姓名"
git config --global user.email "您的邮箱"

# 设置换行符处理（Windows）
git config --global core.autocrlf true
```

### 2. 配置 NuGet 源

如果在中国大陆，建议添加国内镜像源：

```bash
# 添加华为云镜像
dotnet nuget add source https://repo.huaweicloud.com/repository/nuget/v3/index.json -n huawei

# 或添加腾讯云镜像
dotnet nuget add source https://mirrors.cloud.tencent.com/nuget/ -n tencent
```

### 3. 设置环境变量

添加以下环境变量（可选）：

```bash
# 开发环境标识
ASPNETCORE_ENVIRONMENT=Development

# 日志级别
Logging__LogLevel__Default=Information
```

## 获取代码

### 1. 克隆仓库

```bash
# 克隆代码仓库
git clone https://github.com/yourusername/LYBTZYZS.git

# 进入项目目录
cd LYBTZYZS

# 查看项目结构
dir /s
```

### 2. 切换分支（如需要）

```bash
# 查看所有分支
git branch -a

# 切换到开发分支
git checkout develop
```

## 项目构建

### 1. 恢复 NuGet 包

```bash
# 恢复所有项目的依赖包
dotnet restore LYBT.All.sln

# 如果遇到问题，清理缓存后重试
dotnet nuget locals all --clear
dotnet restore LYBT.All.sln
```

### 2. 构建解决方案

```bash
# 构建后端项目
dotnet build LYBT.Backend.sln

# 构建前端项目
dotnet build LYBT.Desktop.sln

# 或构建所有项目
dotnet build LYBT.All.sln
```

### 3. 使用批处理脚本

```bash
# 使用开发管理器（推荐）
scripts\dev-manager.bat

# 选择选项 1: 构建项目
```

## 数据库设置

### 1. 创建数据库

使用 SQL Server Management Studio 或命令行：

```sql
-- 创建数据库
CREATE DATABASE LYBTDB;
GO

-- 使用数据库
USE LYBTDB;
GO
```

### 2. 配置连接字符串

编辑 `src/Backend/Services/LYBT.WebAPI/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. 运行数据库迁移

```bash
# 使用数据库管理器（推荐）
scripts\database-manager.bat

# 选择选项 2: 更新数据库

# 或手动运行
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 4. 初始化种子数据

数据库会在首次运行时自动初始化种子数据，包括：
- 管理员账户：sysadmin / Admin@123456
- 基础角色和权限
- 示例数据（开发环境）

## 运行项目

### 1. 启动 Web API

#### 方法一：使用 Visual Studio

1. 打开 `LYBT.Backend.sln`
2. 设置 `LYBT.WebAPI` 为启动项目
3. 按 F5 运行

#### 方法二：使用命令行

```bash
# 使用快速启动脚本
scripts\start-dev.bat

# 或手动启动
cd src/Backend/Services/LYBT.WebAPI
dotnet run
```

API 将在以下地址运行：
- https://localhost:7001
- http://localhost:5001

### 2. 启动 WPF 客户端

#### 方法一：使用 Visual Studio

1. 打开 `LYBT.Desktop.sln`
2. 设置 `LYBT.WPF.Client.Shell` 为启动项目
3. 按 F5 运行

#### 方法二：使用命令行

```bash
cd src/Frontend/Desktop/Shell
dotnet run
```

### 3. 验证运行状态

1. **访问 Swagger**: https://localhost:7001/swagger
2. **健康检查**: https://localhost:7001/health
3. **登录测试**:
   - 用户名: sysadmin
   - 密码: Admin@123456

## 开发工具配置

### Visual Studio 2022 配置

#### 1. 安装推荐扩展

- **ReSharper**: 代码分析和重构工具
- **CodeMaid**: 代码清理和格式化
- **Git Extensions**: Git 集成增强
- **Swagger Editor**: API 文档编辑

#### 2. 配置代码格式化

1. 工具 → 选项 → 文本编辑器 → C# → 代码样式
2. 启用 EditorConfig 支持
3. 使用项目中的 `.editorconfig` 文件

#### 3. 配置调试

1. 调试 → 选项 → 常规
2. 取消勾选"启用仅我的代码"
3. 勾选"启用源服务器支持"

### Visual Studio Code 配置

#### 1. 安装必需扩展

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "ms-vscode.powershell",
    "humao.rest-client",
    "42crunch.vscode-openapi"
  ]
}
```

#### 2. 配置 launch.json

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch WebAPI",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Backend/Services/LYBT.WebAPI/bin/Debug/net8.0/LYBT.WebAPI.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Backend/Services/LYBT.WebAPI",
      "console": "internalConsole",
      "stopAtEntry": false
    }
  ]
}
```

## 常见问题

### 1. 构建失败

**问题**: 提示找不到 SDK 或包

**解决方案**:
```bash
# 清理构建输出
scripts\clean-build-outputs.bat

# 清理 NuGet 缓存
dotnet nuget locals all --clear

# 重新恢复和构建
dotnet restore
dotnet build
```

### 2. 数据库连接失败

**问题**: 无法连接到 SQL Server

**解决方案**:
1. 确认 SQL Server 服务正在运行
2. 检查连接字符串是否正确
3. 确认 Windows 防火墙允许 SQL Server 端口（1433）
4. 使用 SQL Server Configuration Manager 启用 TCP/IP 协议

### 3. 端口被占用

**问题**: 端口 7001 或 5001 已被占用

**解决方案**:
```bash
# 查找占用端口的进程
netstat -ano | findstr :7001

# 终止进程（替换 PID）
taskkill /PID <进程ID> /F

# 或修改 launchSettings.json 中的端口
```

### 4. 权限问题

**问题**: 访问被拒绝或权限不足

**解决方案**:
1. 以管理员身份运行 Visual Studio
2. 确保当前用户有数据库访问权限
3. 检查文件夹权限设置

### 5. 迁移失败

**问题**: Entity Framework 迁移失败

**解决方案**:
```bash
# 删除现有数据库
dotnet ef database drop --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI --force

# 删除迁移文件
删除 src/Backend/Core/LYBT.Infrastructure/Migrations 文件夹

# 重新创建迁移
dotnet ef migrations add InitialCreate --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 6. 证书问题

**问题**: HTTPS 证书错误

**解决方案**:
```bash
# 信任开发证书
dotnet dev-certs https --trust

# 清理并重新生成证书
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

## 开发提示

### 1. 使用脚本工具

项目提供了丰富的批处理脚本，建议优先使用：

- `scripts\main.bat` - 主菜单
- `scripts\dev-manager.bat` - 开发管理器
- `scripts\database-manager.bat` - 数据库管理器
- `scripts\start-dev.bat` - 快速启动

### 2. 调试技巧

- 使用断点调试 API 和业务逻辑
- 查看输出窗口的日志信息
- 使用 Swagger UI 测试 API
- 检查浏览器开发者工具的网络请求

### 3. 性能优化

- 启用开发环境的热重载功能
- 使用 IIS Express 而非 Kestrel（开发时）
- 配置合适的日志级别
- 定期清理临时文件和日志

### 4. 团队协作

- 遵循 Git 工作流程
- 定期拉取最新代码
- 提交前运行测试
- 保持代码风格一致

## 下一步

完成环境设置后，建议：

1. 阅读 [编码标准](CODING_STANDARDS.md)
2. 了解 [系统架构](../architecture/ARCHITECTURE.md)
3. 查看 [模块说明](../architecture/MODULES.md)
4. 参考 [开发路线图](ROADMAP.md)

如有问题，请查看项目 Wiki 或联系技术负责人。