# 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core Identity | EF Core | SQL Server

面向小型中医诊所（1-3 名医生）的诊疗管理平台，聚焦看诊记录核心流程。

## 核心功能

| 模块 | 功能 |
|------|------|
| 患者管理 | 档案、拼音码搜索、身份证读卡 |
| 医案管理 | DDD 聚合根（诊断 + 处方）、三步完成流程 |
| 药材管理 | 药材库、拼音码检索、君臣佐使角色排序 |
| 验方管理 | 经验方模板、延迟绑定、批量导入 |
| 挂号管理 | 前台排队（顺序号）、挂号费、医生快速看诊 |
| 处方打印 | QuestPDF 导出、A5/A4 模板 |
| 日报统计 | 当日收入、看诊量、药材频次 |
| 用户管理 | ASP.NET Core Identity、4 级角色 |

## 双运行模式

| 模式 | 数据链路 | 适用场景 | 认证 |
|------|----------|----------|------|
| **远程** | WPF → HTTP API → SQL Server | 多用户、联网环境 | 完整 Identity（密码策略、锁定、2FA） |
| **本地** | WPF → 嵌入式 LocalWebAPI → LocalDB | 单医生、离线应急 | 简化认证（多账号，无锁定） |

业务代码完全模式无关——切换 = URL 变更，无需改代码。

## 登录流程

```
首次启动
  ↓
FirstRunSetup 向导 → 配置远程 URL 或选择本地模式
  ↓
登录界面
  ├── 自动检测远程可用性（GET /api/v1/health，3 秒超时）
  ├── 远程可用 → 远程模式（绿色徽章）
  ├── 远程不可用 → 透明降级到本地模式（橙色徽章）
  ├── ⚙ 设置按钮 → 服务器配置（URL + 测试连接 + 多服务器）
  └── 用户名 + 密码 → JWT 令牌
```

| 功能 | 说明 |
|------|------|
| 零配置启动 | 自动检测远程 API，无需手动选择模式 |
| 透明降级 | 网络断开自动切换本地模式 |
| 模式指示器 | 登录界面 + 主窗口状态栏显示当前模式 |
| 服务器配置 | 登录前可配置，Admin 登录后可修改 |
| 首次使用向导 | 引导用户配置远程 URL 或选择本地模式 |

## 快速开始

```bash
git clone https://gitee.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS
dotnet build LYBTZYZS.sln

# 数据库迁移
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 运行测试
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/
```

## 技术栈

| 层 | 技术 |
|----|------|
| Desktop | WPF + Prism.DryIoc (.NET 8) |
| Server | ASP.NET Core WebAPI + Identity (.NET 8) |
| ORM | Entity Framework Core 8 |
| 数据库 | SQL Server 2019+ / LocalDB |
| 认证 | ASP.NET Core Identity + JWT |
| 日志 | Serilog |
| 映射 | Riok.Mapperly (编译时) |
| 测试 | xUnit + FluentAssertions |

## 架构

```
src/
├── Server/
│   ├── Core/           # Entities, Infrastructure (DbContext, Identity)
│   ├── Modules/        # Auth, Users, Patients, Herbs, Formulas, MedicalCase, Registration, Reports
│   └── Services/       # WebAPI (Controllers, Program.cs)
├── Client/
│   └── Desktop/
│       ├── Core/       # Contracts, Foundation, Infrastructure, LocalData, Printing, CardReader
│       ├── Modules/    # Auth, Patients, Herbs, Formula, MedicalCase, Registration, Users, Reports
│       ├── Roles/      # Admin, Clinical, Receptionist workspaces
│       ├── Shell/      # App entry, MainWindow, Login, Navigation
│       └── LocalWebAPI/# Embedded ASP.NET Core for local mode
├── Shared/             # DTOs, Validators, Configuration, ExceptionHandling
└── Tests/              # Server, Desktop, Architecture
```

## 文档

| 文档 | 内容 |
|------|------|
| [产品文档](docs/01-product/) | 愿景、角色画像、术语表 |
| [需求文档](docs/02-requirements/) | PRD + 10 模块 141 User Stories |
| [架构文档](docs/03-architecture/) | 系统架构、数据模型、双模式设计 |
| [API 参考](docs/04-api-reference/) | 远程 + 本地端点文档 |
| [开发指南](docs/05-development/) | 编码规范、测试标准 |
| [运维文档](docs/06-operations/) | 部署、配置、监控 |

## Git

- **Remote**: [Gitee](https://gitee.com/shouqitao/LYBTZYZS.git)
- **Branch**: `master`
- **Commit**: `feat(模块): 描述` / `fix(模块): 描述` / `docs:` / `refactor:`

## 许可证

MIT License

---

Copyright 2025-2026 LYBT. All rights reserved.
