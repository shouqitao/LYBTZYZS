# 部署指南

本文档包含服务端部署、客户端部署和数据库运维的详细操作说明。

> 部署架构总览见 [README.md](./README.md)

---

## 服务端部署

### 运行环境

| 组件 | 要求 |
|------|------|
| 运行时 | .NET 8.0 Runtime |
| 数据库 | SQL Server 2019+ |
| 操作系统 | Windows Server 2019+ / Linux |
| 端口 | 5000 (HTTP) / 5001 (HTTPS) |

### 发布命令

```bash
# 发布为自包含 (推荐)
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -r win-x64 --self-contained

# 发布为框架依赖
dotnet publish src/Server/Services/LYBT.WebAPI -c Release
```

### 目录结构

```
deploy/
  LYBT.WebAPI.exe          # 主程序
  appsettings.json         # 主配置
  appsettings.Production.json  # 生产环境覆盖
  logs/                    # 日志目录 (自动创建)
```

---

## 客户端部署

### Desktop 安装包

WPF Desktop 客户端通过 ClickOnce 或 MSI 分发。

### 本地模式数据

- SQLite 数据库: `%APPDATA%\LYBT\data\lybt-local.db`
- 日志文件: `%APPDATA%\LYBT\logs\`
- 配置文件: `%APPDATA%\LYBT\config\`

---

## 数据库运维

### 迁移

```bash
# 生成迁移
dotnet ef migrations add <MigrationName> -p src/Server/Core/LYBT.Infrastructure -s src/Server/Services/LYBT.WebAPI

# 应用迁移
dotnet ef database update -s src/Server/Services/LYBT.WebAPI
```

### 日志清理

系统内置日志自动清理:

```json
{
  "Logging": {
    "Cleanup": {
      "Enabled": true,
      "RetentionDays": 90,
      "CleanupIntervalHours": 24,
      "BatchSize": 1000
    }
  }
}
```

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 从 README.md 拆分，初始版本 |
