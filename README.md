# 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core Identity | EF Core | SQL Server | MediatR CQRS

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

> 详细步骤（LocalDB/密码注入/端口/FAQ）见 **[docs/05-development/01-setup.md](docs/05-development/01-setup.md)**。需 **Windows** + VS 2022「.NET 桌面开发」工作负载。

```bash
git clone https://github.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS
dotnet build LYBTZYZS.sln --no-incremental   # 门禁：0 错误 0 警告

# 数据库迁移（远程模式）
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 运行测试
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/
```

本地模式首启需注入默认密码（见 setup 文档）：`DefaultPasswords__SysAdminPassword` / `DefaultPasswords__NewUserPassword`。

## 技术栈

| 层 | 技术 |
|----|------|
| Desktop | WPF + Prism.DryIoc (.NET 8) |
| Server | ASP.NET Core WebAPI + Identity (.NET 8) |
| CQRS | MediatR (Commands/Queries/Handlers + Domain Events) |
| ORM | Entity Framework Core 8 |
| 数据库 | SQL Server 2019+ / LocalDB |
| 认证 | ASP.NET Core Identity + JWT |
| 日志 | Serilog |
| 映射 | Riok.Mapperly (编译时) |
| 测试 | xUnit + FluentAssertions |

## 架构

**模块化单体 + 分层**（当前态权威见 [docs/03-architecture/00-architecture-summary.md](docs/03-architecture/00-architecture-summary.md)）：

- **Server**：Controller → Service → Repository → DbContext（3-Layer）；部分模块写操作走 MediatR Command/Handler，读操作 Service 直查
- **Desktop**：WPF + Prism（Shell → Roles → Modules → Core）+ CommunityToolkit.Mvvm
- **双模式**：远程 WebAPI（SQL Server）+ 本地嵌入式 LocalWebAPI（LocalDB）；切换 = URL 变更
- **DDD**：`MedicalCase` 为唯一聚合根（Consultation + Prescription）
- **模块隔离**：P07 模块间禁止直接引用；P08 跨模块走接口；LocalWebAPI 为唯一白名单例外（ADR-0010/0023）

```
src/
├── Server/
│   ├── Core/                 # Entities, Infrastructure (DbContext, BaseRepository, migrations)
│   ├── Modules/              # LYBT.Module.*（业务模块，模块间零直接引用）
│   └── Services/LYBT.WebAPI/ # ASP.NET Core Controllers
├── Client/Desktop/
│   ├── Core/                 # Contracts, Foundation, Infrastructure, Controls, Printing, ...
│   ├── Modules/              # Auth, Users, Patients, Catalog, MedicalCase, Registration, Reports
│   ├── Roles/                # Admin, Clinical 工作台
│   ├── Shell/                # 应用入口 / MainWindow / 登录 / 导航
│   └── LocalWebAPI/          # 本地模式嵌入式 API（:5300）
├── Shared/                   # Entities, Models/DTO, Configuration, Logging, ExceptionHandling
└── tests/                    # Server / Desktop / Architecture
```

> 模块内目录结构与历史演进详见 [docs/03-architecture/](docs/03-architecture/README.md)。源码树中的 `Domain/Application/Infrastructure` 垂直切片为部分模块内部组织方式，**不以本 README 为架构 SSOT**。

## 文档

| 文档 | 内容 |
|------|------|
| **[文档中心](docs/README.md)** | 总入口：AI 查询指南、按角色导航、目录索引 |
| [产品文档](docs/01-product/) | 愿景、角色画像、术语表、权限矩阵 |
| [需求文档](docs/02-requirements/) | PRD + 模块 User Stories（数量以 [docs/02-requirements/README.md](docs/02-requirements/README.md) 为准） |
| [架构文档](docs/03-architecture/) | 系统架构、数据模型、双模式、ADR、项目总账 |
| [API 参考](docs/04-api-reference/) | 远程 + 本地端点文档 |
| [开发指南](docs/05-development/) | 环境搭建、编码规范、测试 |
| [运维文档](docs/06-operations/) | 部署、配置、监控、备份 |
| [UI/UX](docs/07-ui-ux/) | 桌面端设计规范与 UX 约定 |
| [治理规范](docs/00-governance/) | 命名、SSOT、技术引入治理 |

## Git

- **Remote**: [GitHub](https://github.com/shouqitao/LYBTZYZS)（主）/ [Gitee](https://gitee.com/shouqitao/LYBTZYZS)（镜像）
- **Branch**: `master`
- **Commit**: `feat(模块): 描述` / `fix(模块): 描述` / `docs:` / `refactor:`

## 许可证

MIT License

---

Copyright 2025-2026 LYBT. All rights reserved.
