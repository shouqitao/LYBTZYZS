# 凌隐宝堂中医诊所管理系统

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

**面向中医诊所的企业级管理解决方案**

## 简介

凌隐宝堂中医诊所管理系统 (LYBTZYZS) 是一个专为中医诊所设计的综合管理平台，采用 .NET 8 + WPF + ASP.NET Core + EF Core 技术栈，支持远程 (SQL Server) 和本地 (SQLite) 双运行模式。

## 核心功能

| 模块 | 功能 |
|------|------|
| **患者管理** | 档案管理、Excel 批量导入导出、历史记录 |
| **医案管理** | 聚合根 (Consultation + Prescription)、三步流程 |
| **诊断管理** | 四诊合参 (望闻问切)、中医辨证 |
| **处方管理** | 表格编辑、快速录入、验方导入、历史复制 |
| **药材管理** | 完整药材库、拼音码检索、引用检查 |
| **验方管理** | 经验方模板、分类管理、延迟绑定验证 |
| **用户管理** | 角色体系 (Doctor/Admin/SuperAdmin) |
| **认证授权** | JWT + RefreshToken、资源级权限 |
| **数据同步** | 本地与远程双向同步 (Herb/Patient/Formula) |

## 快速开始

```bash
# 克隆、编译、运行
git clone <repo-url> && cd LYBTZYZS
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln

# 启动服务端
dotnet run --project src/Server/Services/LYBT.WebAPI

# 测试
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests"
```

详细步骤见 [开发指南](docs/05-development/README.md)。

## 技术栈

| 层 | 技术 |
|----|------|
| Desktop 客户端 | WPF + Prism.DryIoc (.NET 8) |
| 服务端 API | ASP.NET Core WebAPI (.NET 8) |
| ORM | Entity Framework Core 8.0 |
| 远程数据库 | SQL Server 2019+ |
| 本地数据库 | SQLite |
| 认证 | JWT + RefreshToken + AutoLoginToken |
| 日志 | Serilog (Console + File + SQL Server) |
| 测试 | xUnit + NSubstitute |

## 文档

**[文档中心](docs/README.md)** -- 完整文档导航

| 文档 | 内容 |
|------|------|
| [产品文档](docs/01-product/) | 愿景、功能概览、角色、词汇表 |
| [需求文档](docs/02-requirements/) | PRD (9 模块, 92 条功能需求) |
| [架构文档](docs/03-architecture/) | 系统架构、数据模型、安全、ADR |
| [API 参考](docs/04-api-reference/) | 99 个 API 端点文档 |
| [开发指南](docs/05-development/) | 快速开始、编码规范、测试 |
| [运维文档](docs/06-operations/) | 部署、配置、监控 |

## 提交规范

```
feat(模块): 功能描述 - Issue #编号
fix(模块): 缺陷修复 - Issue #编号
docs: 文档更新
refactor: 代码重构
test: 测试相关
```

## 许可证

MIT License - 查看 [LICENSE](LICENSE)

---

**凌隐宝堂中医诊所管理系统** - 专注中医，服务健康

Copyright 2025-2026 LYBT. All rights reserved.
