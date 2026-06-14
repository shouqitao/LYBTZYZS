# 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server

面向小型中医诊所（1-3 名医生）的诊疗管理平台，覆盖从患者登记到处方打印的完整中医诊疗流程。

## 核心功能

| 模块 | 功能 |
|------|------|
| 患者管理 | 档案、拼音码搜索、身份证读卡、批量导入导出 |
| 医案管理 | DDD 聚合根（诊断 + 处方）、三步完成流程、审计追踪 |
| 药材管理 | 药材库、拼音码检索、引用保护、Excel 导入 |
| 验方管理 | 经验方模板、延迟绑定、共享与验证 |
| 挂号管理 | 前台排队、医生快速看诊、状态联动 |
| 处方打印 | WPF FixedDocument 预览、QuestPDF 导出、版本管理 |
| 数据同步 | 远程/本地双向同步、Checksum 冲突检测 |
| 用户权限 | 4 级角色（Receptionist/Doctor/Admin/SuperAdmin） |

## 双运行模式

| 模式 | 数据链路 | 适用场景 |
|------|----------|----------|
| **远程** | WPF → HTTP API → SQL Server | 多用户、联网环境 |
| **本地** | WPF → 嵌入式 LocalWebAPI → SQL Server LocalDB | 单用户、离线外出 |

业务代码完全模式无关——切换 = URL 变更，无需改代码。

## 快速开始

```bash
git clone https://gitee.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS
dotnet build LYBTZYZS.sln

# 启动 WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI

# 运行测试
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/
```

## 技术栈

| 层 | 技术 |
|----|------|
| Desktop | WPF + Prism.DryIoc (.NET 8) |
| Server | ASP.NET Core WebAPI (.NET 8) |
| ORM | Entity Framework Core 8 |
| 数据库 | SQL Server 2019+ / LocalDB |
| 认证 | JWT + RefreshToken + AutoLoginToken |
| 日志 | Serilog |
| 映射 | Riok.Mapperly (编译时) |
| 测试 | xUnit + FluentAssertions |

## 文档

| 文档 | 内容 |
|------|------|
| [产品文档](docs/01-product/) | 愿景、功能概览、角色画像 |
| [需求文档](docs/02-requirements/) | PRD（15 模块，138 User Stories） |
| [架构文档](docs/03-architecture/) | 系统架构、数据模型、ADR |
| [API 参考](docs/04-api-reference/) | 106 个远程 + 112 个本地端点 |
| [开发指南](docs/05-development/) | 编码规范、测试标准 |
| [运维文档](docs/06-operations/) | 部署、配置、监控 |

## 提交规范

```
feat(模块): 功能描述
fix(模块): 缺陷修复
docs: 文档更新
refactor: 代码重构
test: 测试相关
```

## 许可证

MIT License

---

Copyright 2025-2026 LYBT. All rights reserved.
