# 环境搭建

## 必要工具

### 1. .NET SDK

- **版本**: 8.0.406+ (由 `global.json` 锁定)
- **下载**: https://dotnet.microsoft.com/download/dotnet/8.0
- **验证**: `dotnet --version` 输出 `8.0.4xx`

### 2. Visual Studio 2022

- **版本**: 17.8+
- **工作负载**:
  - ASP.NET and web development
  - .NET desktop development (含 WPF)
- **推荐扩展**: EditorConfig Language Service

### 3. SQL Server

- **版本**: SQL Server 2019+ 或 SQL Server Express (LocalDB)
- **远程模式必需**: 仅远程模式需要独立 SQL Server
- **本地模式**: 使用 SQLite，无需安装

### 4. Git

- **版本**: 2.30+
- **配置**: `git config core.autocrlf true` (Windows)

---

## 数据库配置

### 远程模式 (SQL Server)

1. 创建数据库:

```sql
CREATE DATABASE LYBTDB;
```

2. 配置连接字符串 (`src/Server/Services/LYBT.WebAPI/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

3. 数据库迁移会在应用启动时自动执行 (`EnsureCreatedInDevelopment: true`)。

### 本地模式 (SQLite)

- 无需配置，Desktop 客户端自动创建 `lybt-local.db` 文件
- 数据存储位置: `%APPDATA%\LYBT\data\`

---

## 依赖管理

项目使用 **Central Package Management** (`Directory.Packages.props`)。

所有 NuGet 包版本在根目录 `Directory.Packages.props` 统一管理，各 `.csproj` 只声明包名不声明版本。

核心依赖:

| 包 | 版本 | 用途 |
|-----|------|------|
| Microsoft.EntityFrameworkCore | 8.x | ORM |
| Prism.DryIoc | 9.x | WPF MVVM + DI |
| Asp.Versioning.Mvc | 8.x | API 版本控制 |
| Serilog | 4.x | 结构化日志 |
| NSubstitute | 5.x | 测试 Mock 框架 |
| ClosedXML | 0.102.x | Excel 导入导出 |

---

## 首次运行

### 服务端

```bash
cd src/Server/Services/LYBT.WebAPI
dotnet run
# 访问 https://localhost:5001/api/v1/health 验证
```

默认管理员账号:
- 用户名: `sysadmin`
- 密码: 见 `appsettings.json` > `DefaultPasswords.SysAdminPassword`

### 客户端

1. Visual Studio 打开 `LYBT.All.sln`
2. 设置 `LYBT.Desktop.Shell` 为启动项目
3. F5 运行
4. 默认连接远程模式 (localhost:5001)

---

## 常见问题

| 问题 | 解决方案 |
|------|----------|
| `dotnet build` 失败: 找不到 SDK | 检查 `global.json` 版本匹配 |
| WPF 项目编译失败 | 确认已安装 ".NET desktop development" 工作负载 |
| 数据库连接失败 | 检查 SQL Server 服务运行状态和连接字符串 |
| Desktop 启动白屏 | 检查 WebAPI 是否运行 (远程模式需要) |
| 测试运行失败 | `dotnet restore` 后重试；Desktop 测试需 Windows |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
