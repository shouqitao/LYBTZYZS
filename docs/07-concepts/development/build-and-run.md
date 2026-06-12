---
type: development
title: 构建与运行
tags: [development, setup, build]
created: 2026-06-10
updated: 2026-06-10
source: docs/05-development/setup.md
---

## 概述

本文档说明 LYBTZYZS 项目的开发环境搭建步骤，包括必要工具安装、依赖管理、构建命令和常见问题排查。项目使用 .NET 8 SDK + Central Package Management，支持远程模式 (SQL Server) 和本地模式 (SQLite) 两种运行方式。

## 核心内容

### 必要工具

| 工具 | 版本要求 | 验证命令 |
|------|----------|----------|
| .NET SDK | 8.0.406+ (由 `global.json` 锁定) | `dotnet --version` |
| Visual Studio 2022 | 17.8+ | - |
| SQL Server | 2019+ 或 Express (LocalDB) | - |
| Git | 2.30+ | `git --version` |

**VS 工作负载**: ASP.NET and web development + .NET desktop development (含 WPF)

### 数据库配置

**远程模式 (SQL Server)** — 创建数据库并配置连接字符串:

```sql
CREATE DATABASE LYBTDB;
```

```json
// src/Server/Services/LYBT.WebAPI/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true"
  }
}
```

**本地模式 (SQLite)** — 无需配置，Desktop 客户端自动创建 `%APPDATA%\LYBT\data\lybt-local.db`。

### 依赖管理

项目使用 **Central Package Management** (`Directory.Packages.props`)，所有 NuGet 包版本在根目录统一管理，各 `.csproj` 只声明包名不声明版本。

| 包 | 版本 | 用途 |
|-----|------|------|
| Microsoft.EntityFrameworkCore | 8.x | ORM |
| Prism.DryIoc | 9.x | WPF MVVM + DI |
| Asp.Versioning.Mvc | 8.x | API 版本控制 |
| Serilog | 4.x | 结构化日志 |
| NSubstitute | 5.x | 测试 Mock 框架 |
| ClosedXML | 0.102.x | Excel 导入导出 |

### 构建命令

```bash
# 完整构建
dotnet restore LYBTZYZS.sln
dotnet build LYBTZYZS.sln

# 仅前端 (更快)
dotnet build LYBT.Desktop.sln

# 仅后端
dotnet build LYBT.Backend.sln

# 清理重建
dotnet clean LYBTZYZS.sln && dotnet build LYBTZYZS.sln
```

### 测试命令

```bash
# 全部测试 (~2021+ 跨 6 个测试项目)
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"

# 单独测试项目
dotnet test tests/LYBT.Tests.Server/           # ~1185 tests, real SQL Server + Respawn
dotnet test tests/LYBT.Tests.Desktop/          # ~760 tests, SQLite InMemory + real Repository
dotnet test tests/LYBT.Tests.Architecture/     # ~76 tests, architecture guard

# 单个测试
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"
```

### 运行命令

```bash
# 启动 WebAPI 服务端
cd src/Server/Services/LYBT.WebAPI
dotnet run
# 验证: https://localhost:5001/api/v1/health

# 启动 Desktop 客户端
# Visual Studio: 设置 LYBT.Desktop.Shell 为启动项目，按 F5
```

默认管理员账号: 用户名 `sysadmin`，密码见 `appsettings.json` > `DefaultPasswords.SysAdminPassword`。

### 自动化脚本

```bash
# PowerShell
.\scripts\run-webapi.ps1          # 启动 WebAPI
.\scripts\stop-webapi.ps1         # 停止 WebAPI
.\scripts\run-tests-local.ps1     # 运行本地测试
.\scripts\cleanup.ps1             # 清理临时文件

# Batch
scripts\build.bat                 # 交互式构建管理器
scripts\build-check.bat           # 构建验证
scripts\quick-compile.bat         # 快速编译检查
```

### 常见问题

| 问题 | 解决方案 |
|------|----------|
| `dotnet build` 失败: 找不到 SDK | 检查 `global.json` 版本匹配 |
| WPF 项目编译失败 | 确认已安装 ".NET desktop development" 工作负载 |
| 数据库连接失败 | 检查 SQL Server 服务运行状态和连接字符串 |
| Desktop 启动白屏 | 检查 WebAPI 是否运行 (远程模式需要) |
| 测试运行失败 | `dotnet restore` 后重试；Desktop 测试需 Windows |

## 相关链接

- [[overview]] — 项目概述和架构
- [[testing-strategy]] — 测试策略和 Testing Trophy 架构
- [[dual-mode-architecture]] — 双模式架构 (远程/本地切换)
