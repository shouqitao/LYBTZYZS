# 凌隐宝堂中医诊所管理系统

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

**面向中医诊所的企业级管理解决方案**

## 简介

凌隐宝堂中医诊所管理系统 (LYBTZYZS) 是一个专为中医诊所设计的综合管理平台，采用 .NET 8 + WPF + ASP.NET Core + EF Core 技术栈，支持远程 (SQL Server) 和本地 (嵌入式 LocalWebAPI + SQL Server) 双运行模式。

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
dotnet restore LYBTZYZS.sln
dotnet build LYBTZYZS.sln

# 启动服务端
dotnet run --project src/Server/Services/LYBT.WebAPI

# 测试
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"
```

详细步骤见 [开发指南](docs/05-development/README.md)。

## 技术栈

| 层 | 技术 |
|----|------|
| Desktop 客户端 | WPF + Prism.DryIoc (.NET 8) |
| 服务端 API | ASP.NET Core WebAPI (.NET 8) |
| ORM | Entity Framework Core 8.0 |
| 远程数据库 | SQL Server 2019+ |
| 本地数据库 | SQL Server (嵌入式 LocalWebAPI) |
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

## 开发笔记

# LYBTZYZS 凌隐宝堂中医诊所管理系统

**技术栈**: .NET 8 + WPF/Prism + ASP.NET Core + EF Core + SQL Server
**阶段**: 正式版开发阶段

---

## 构建与测试

```bash
# 编译
dotnet build LYBTZYZS.sln

# 测试 (3个测试项目, Testing Trophy 架构, ~2021 tests)
dotnet test tests/LYBT.Tests.Server/           # 1185 tests (真实 SQL Server + Respawn, 零 mock)
dotnet test tests/LYBT.Tests.Desktop/          # 760 tests (SQLite InMemory + 真实 Repository)
dotnet test tests/LYBT.Tests.Architecture/     # 76 tests (架构防护 + AntiMockRules)

# 全量测试
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"
```

---

## 双工具协作体系

本项目使用 **Superpowers** + **Planning-with-files** 协同开发。

**IMPORTANT: 每次 Superpowers 操作完成后，必须将结果同步写入 planning-with-files 三文件 (task_plan.md / findings.md / progress.md)。**

| Superpowers 操作 | 同步目标 |
|------------------|----------|
| `brainstorming` | task_plan.md + findings.md |
| `writing-plans` | task_plan.md + progress.md |
| `executing-plans` / `subagent-driven-development` | task_plan.md + progress.md |
| `requesting-code-review` / `receiving-code-review` | findings.md + progress.md |
| `verification-before-completion` / `finishing-a-development-branch` | progress.md + task_plan.md |

标准流程: `BRAINSTORM → PLAN → EXECUTE → REVIEW → VERIFY`

### 三文件生命周期 (每任务重置)

三文件是**施工脚手架**，不是项目交付物。每个新任务从空白状态开始。

| 时机 | 操作 |
|------|------|
| **新任务开始** (BRAINSTORM) | 覆盖重建三文件，内容清空为当前任务 |
| **任务执行中** | 按同步规则持续更新 |
| **任务完成** (VERIFY) | 重要决策 -> Serena 记忆；设计产出 -> `docs/plans/`；三文件等待下一任务覆盖 |

**禁止**: 三文件跨任务累积增长 (膨胀文件会浪费 PreToolUse hook 读取的 token)

详细同步规则见 @.claude/rules/development-flow.md

---

## 架构要点

- **三层架构**: Controller → Service → Repository → DbContext
- **MVVM**: View (XAML) ← 绑定 → ViewModel → Repository → API
- **DDD**: MedicalCase 是唯一聚合根 (Consultation + Prescription 是内部实体)
- **双模式**: 远程 (SQL Server) + 本地 (嵌入式 LocalWebAPI + SQL Server)，共享 Repository 接口。详见 [dual-mode.md](docs/03-architecture/dual-mode.md)

## Repository 规范 (Task 6)

**核心原则**: Service 层禁止直接注入 `AppDbContext`，必须通过 Repository 接口访问数据。

| 层级 | 职责 | 注入规则 |
|------|------|----------|
| Controller | HTTP 请求处理 | 注入 Service 接口 |
| Service | 业务逻辑编排 | 注入 Repository 接口，禁止注入 DbContext |
| Repository | 数据访问封装 | 注入 DbContext，封装查询/命令 |
| DbContext | EF Core 基础设施 | 仅 Repository 层直接依赖 |

**架构测试**: `P10_Services_Should_Not_Directly_Inject_AppDbContext` 强制约束，详见 `tests/LYBT.Tests.Architecture/ServerArchTests.cs`

## 术语铁律

- **Consultation** = 仅指中医诊断部分，不是"问诊"或"就诊"
- **MedicalCase** = 医案，不是"病历"
- **Formula** = 验方/经验方

---

## 开发准则

1. **Architecture First** - 架构完善优先
2. **Root Cause Analysis** - 定位根因，禁止表面修补
3. **Test Coverage** - 新功能必须编写测试
4. **Documentation** - 架构决策和 API 变更必须更新 `docs/` 文档

## 修改前必查

1. **查记忆**: `mcp__serena__list_memories()` / `mcp__serena__read_memory("文件名")`
2. **查文档**: `mcp__context7__get-library-docs` / `mcp__plugin_wpf-dev-pack_MicrosoftDocs__microsoft_docs_search`
3. **查案例**: `mcp__plugin_claude-code-settings_exa__get_code_context_exa` / `mcp__tavily-mcp__tavily-search`
4. **问用户**: 方案确认后再执行

**IMPORTANT: 禁止未经调研直接编码 | 禁止猜测方案 | 禁止跳过用户确认**

## MCP工具速查

| 场景 | 首选工具 |
|------|---------|
| 查项目历史决策/架构知识 | `mcp__serena__list_memories` → `mcp__serena__read_memory` |
| 查 NuGet 库 / 开源框架文档 | `mcp__context7__resolve-library-id` → `get-library-docs` |
| 查 MS/WPF/.NET/ASP.NET 官方文档 | `mcp__plugin_wpf-dev-pack_MicrosoftDocs__microsoft_docs_search` |
| 查 MS 代码示例 | `mcp__plugin_wpf-dev-pack_MicrosoftDocs__microsoft_code_sample_search` |
| 代码语义搜索 / 技术调研 | `mcp__plugin_claude-code-settings_exa__get_code_context_exa` |
| 通用网络搜索 | `mcp__tavily-mcp__tavily-search` |
| GitHub Issue / PR 操作 | `gh issue list/create/edit/close` / `gh pr list/create/merge` |
| C# 符号跳转 / 引用查找 | `LSP` tool (csharp-ls 0.22.0，已安装) |
| 获取当前时间 | `mcp__time__get_current_time(timezone="Asia/Shanghai")` |

详细工具用法见 @.claude/rules/tools.md

---

## 核心约束

- **Phase 完成必须等待指令** - 每完成一个 Phase 后，汇报结果并等待用户明确指令，禁止自动进入下一 Phase
- **Planning-with-files 必用** - 复杂任务(3+步骤)必须创建 task_plan.md / findings.md / progress.md
- **新任务必重置** - 开始新任务时覆盖三文件，禁止跨任务累积
- **2-Action Rule** - 每2次搜索/浏览操作后，立即更新 findings.md
- **兼容代码临时** - 必须添加注释标记，有明确移除计划

## 常见陷阱

- EF Core 8 的 `FindAsync` 在实体不在 ChangeTracker 中时会应用全局查询过滤器 (IsDeleted)，需要用 `IgnoreQueryFilters()` 查询软删除记录
- WPF Desktop 测试需要 net8.0-windows 目标框架，不能和 Server 测试混在同一个项目
- MedicalCase 的 `HasPrescription` 是计算属性，依赖 `PrescriptionId.HasValue`，Mapper 必须显式设置

---

## 文档体系

```
docs/
├── 01-product/          # 产品层
├── 02-requirements/     # 需求层 (PRD)
├── 03-architecture/     # 架构层
├── 04-api-reference/    # API 参考
├── 05-development/      # 开发指南
└── 06-operations/       # 运维
```

文档标准见 `docs/plans/2026-02-10-documentation-system-design.md`

---

## 详细规则

@.claude/rules/tools.md
@.claude/rules/development-flow.md
@.claude/rules/code-standards.md

---

最后更新: 2026-03-09
文档版本: v6.5-mcp-tools
