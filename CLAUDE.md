# LYBTZYZS 凌隐宝堂中医诊所管理系统

**技术栈**: .NET 8 + WPF/Prism + ASP.NET Core + EF Core + SQL Server/SQLite (双模式)
**阶段**: 正式版开发阶段

---

## 构建与测试

```bash
# 编译
dotnet build LYBT.All.sln

# 测试 (3个测试项目, Testing Trophy 架构, ~2021 tests)
dotnet test tests/LYBT.Tests.Server/           # 1185 tests (真实 SQL Server + Respawn, 零 mock)
dotnet test tests/LYBT.Tests.Desktop/          # 760 tests (SQLite InMemory + 真实 Repository)
dotnet test tests/LYBT.Tests.Architecture/     # 76 tests (架构防护 + AntiMockRules)

# 全量测试
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests"
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
- **双模式**: 远程 (SQL Server) + 本地 (SQLite)，共享 Service/Repository 层，仅 DbContext Provider 不同 (SYNC-D02)。详见 [dual-mode.md](docs/03-architecture/dual-mode.md)

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
