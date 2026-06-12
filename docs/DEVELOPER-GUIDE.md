# 凌隐宝堂中医诊所管理系统 — 开发者文档

> **版本**: v1 (开发中) | **框架**: .NET 8 WPF + ASP.NET Core | **数据库**: SQL Server + SQLite 双模式

## 快速开始

### 环境要求
- Visual Studio 2022 17.8+ 或 JetBrains Rider
- .NET 8 SDK
- SQL Server (远程模式) 或 LocalDB (本地模式)
- Node.js 18+ (用于测试工具)

### 构建与运行

```bash
# 构建
dotnet build LYBTZYZS.sln

# 运行服务器 (WebAPI)
dotnet run --project src/Server/Services/LYBT.WebAPI

# 运行桌面客户端
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell
```

详见 → [`05-development/build-and-run.md`](05-development/01-setup.md)

### 测试

```bash
dotnet test tests/LYBT.Tests.Server/        # 1185 tests (SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # 760 tests (SQLite InMemory)
dotnet test tests/LYBT.Tests.Architecture/  # 76 tests (架构守卫)
```

详见 → [`07-concepts/development/build-and-run.md`](07-concepts/development/build-and-run.md)

---

## 文档导航

### 📦 产品与需求

| 文档 | 说明 | 位置 |
|------|------|------|
| 产品愿景 | 系统定位、核心价值、用户角色 | [`01-product/`](01-product/) |
| 功能需求 (PRD) | 15模块、138 User Stories | [`02-requirements/`](02-requirements/) |
| 用户故事地图 | 核心用户流程 | [`02-requirements/19-user-story-map.md`](02-requirements/19-user-story-map.md) |
| 非功能需求 | 性能、安全、可用性 | [`02-requirements/17-nfr.md`](02-requirements/17-nfr.md) |

### 🏗️ 架构

| 文档 | 说明 | 位置 |
|------|------|------|
| 系统架构 | 三层架构 + MVVM + DDD | [`03-architecture/system-architecture.md`](03-architecture/01-system-overview.md) |
| 数据模型 | 实体关系与设计 | [`03-architecture/04-data-model.md`](03-architecture/04-data-model.md) |
| 安全设计 | 认证、授权、数据保护 | [`03-architecture/security.md`](03-architecture/07-configuration.md) |
| 双模式架构 | 远程/本地双数据库 | [`07-concepts/01-dual-mode-architecture.md`](07-concepts/01-dual-mode-architecture.md) |
| 架构决策记录 | 8项ADR | [`03-architecture/decisions/`](03-architecture/decisions/) |

### 🔌 API 参考

| 文档 | 说明 | 位置 |
|------|------|------|
| API 总览 | 100+端点概览 | [`04-api-reference/`](04-api-reference/) |
| 认证接口 | 登录、令牌、刷新 | [`04-api-reference/auth-api.md`](04-api-reference/01-auth.md) |

### 🛠️ 开发指南

| 文档 | 说明 | 位置 |
|------|------|------|
| 编码规范 | 命名、格式、分析器规则 | [`05-development/standards/`](05-development/standards/) |
| 开发流程 | Git工作流、PR规范 | [`05-development/development-guide.md`](05-development/01-setup.md) |
| 测试策略 | 集成优先、零Mock | [`07-concepts/24-testing-strategy.md`](07-concepts/24-testing-strategy.md) |
| 常见陷阱 | 已知坑与解决方案 | [`07-concepts/development/common-pitfalls.md`](07-concepts/development/common-pitfalls.md) |
| 术语表 | 中英文术语对照 | [`07-concepts/development/terminology.md`](07-concepts/development/terminology.md) |

### 🚀 运维

| 文档 | 说明 | 位置 |
|------|------|------|
| 部署指南 | 发布、配置、监控 | [`06-operations/`](06-operations/) |
| 配置参考 | 连接字符串、日志、缓存 | [`06-operations/02-configuration.md`](06-operations/02-configuration.md) |

### 💡 技术概念索引

46项核心技术概念，按类别：

| 类别 | 示例概念 | 位置 |
|------|---------|------|
| 架构模式 | 双模式、单窗口、启动管线 | [`07-concepts/`](07-concepts/) |
| 业务流程 | 临床工作流、患者生命周期、挂号 | [`07-concepts/`](07-concepts/) |
| 安全 | 认证、授权、敏感数据分类 | [`07-concepts/`](07-concepts/) |
| 技术模式 | 缓存、批量操作、异常层次 | [`07-concepts/`](07-concepts/) |
| 模块 | 8个业务模块概述 | [`07-concepts/modules/`](07-concepts/modules/) |

完整索引 → [`07-concepts/README.md`](07-concepts/README.md)

---

## 系统概览

```
┌─────────────────────────────────────────────────────┐
│                   Desktop Client                     │
│              (WPF + Prism MVVM)                      │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐ │
│  │ Admin   │ │Clinical │ │Reception │ │ Shared   │ │
│  │Workspace│ │Workspace│ │Workspace │ │ Modules  │ │
│  └─────────┘ └─────────┘ └──────────┘ └──────────┘ │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP/REST (嵌入式 Kestrel)
┌──────────────────────┴──────────────────────────────┐
│                    WebAPI Server                      │
│              (ASP.NET Core 8)                        │
│  ┌────────┐  ┌────────┐  ┌────────┐                │
│  │Controller│→│ Service │→│Repository│               │
│  └────────┘  └────────┘  └────────┘                │
└──────────────────────┬──────────────────────────────┘
                       │ EF Core 8
              ┌────────┴────────┐
              │  SQL Server /   │
              │  LocalDB        │
              └─────────────────┘
```

### 核心模块 (7个)

| 模块 | 职责 | 文档 |
|------|------|------|
| Auth | 认证授权、JWT、角色 | [`07-concepts/modules/auth-module.md`](07-concepts/modules/auth-module.md) |
| Patients | 患者管理、导入导出 | [`07-concepts/modules/patient-module.md`](07-concepts/modules/patient-module.md) |
| MedicalCase | 医案(聚合根)、诊断、处方 | [`07-concepts/modules/medical-case-module.md`](07-concepts/modules/medical-case-module.md) |
| Herbs | 中药材管理、拼音搜索 | [`07-concepts/modules/herb-module.md`](07-concepts/modules/herb-module.md) |
| Formula | 验方管理、验证 | [`07-concepts/modules/formula-module.md`](07-concepts/modules/formula-module.md) |
| Registration | 挂号、排队 | [`07-concepts/modules/registration-module.md`](07-concepts/modules/registration-module.md) |
| Sync | 数据同步、冲突解决 | [`07-concepts/modules/sync-module.md`](07-concepts/modules/sync-module.md) |

---

## 关键约定

| 规则 | 说明 |
|------|------|
| DDD聚合根 | MedicalCase 是唯一聚合根 (Consultation + Prescription 为内部实体) |
| 三层架构 | Controller → Service → Repository → DbContext |
| 跨模块禁止 | Server模块间/桌面模块间禁止直接引用 |
| 测试策略 | 集成优先：真实SQL Server，零Mock |
| 错误处理 | 全局异常中间件 + 业务异常层次 |
| 软删除 | IsDeleted全局过滤器，需用 `IgnoreQueryFilters()` 查询 |
| 术语 | Consultation=中医诊断, MedicalCase=医案, Formula=验方 |

---

## 外部资源

- **LLM Wiki**: 知识库 (180+技术概念、代码实体、架构分析) — 通过 API 查询
- **GitNexus**: 代码图谱 (35K符号、76K关系、300执行流) — 代码导航
- **Obsidian**: 文档编辑与阅读 (当前docs目录即为vault)
