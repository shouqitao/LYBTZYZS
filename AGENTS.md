# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

> **本文档被两个 agent 共同读取，各取所需**：
> - **Hermes coder（统筹）** → 见「🤝 协作角色」「⚠️ 强制规则」
> - **Mimo Code（编码执行）** → 见「🛠️ Mimo Code 执行守则」+ 项目级 skill `.mimocode/skills/lybtzys-coder-rules/SKILL.md`
>
> **项目总账（强制）**: `docs/03-architecture/13-project-master-plan.md`
> **架构决策 (ADR)**: `docs/03-architecture/decisions/`
> **当前状态 SSOT**: `docs/03-architecture/13c-current-status.md`

---

## 🤝 协作角色（Hermes 统筹 · 2026-08-02 确立）

| | 总设计师（AI agent） | 产品负责人（用户） |
|---|---|---|
| **职责** | 架构设计 + 任务派遣 + 监测 + 验收 | 决定业务「要什么」+ 确认方向 |
| **不做** | 亲自写代码（全部委派 Mimo Code） | 回答技术细节 |
| **工作流** | 汇报计划 → 提出问题 → 等待确认 → 调整方向 → 派遣执行 | 提供需求 → 确认/否决/调整 |
| **沟通** | 给「现状 + 专家推荐 + 理由」 | 在推荐上确认/否决/调整 |

问用户只问业务结果（是/否/选项/展开），不问技术题。

---

## ⚠️ 强制规则（Hermes 统筹 · 每个 session 必读）

1. **先加载 Skill**：`skill_view(name='lybtzys-coder-rules')` — 完整规范、Pitfalls、MCP 纪律、工程流程
2. **项目总账维护**：session 启动读 `13-project-master-plan.md` 接上进度；任务完成后立即更新状态表 ⬜→✅ + Commit SHA；取消标 ❌ + 原因；决策变更在 §九 追加一行。**禁止做完任务不更新清单。**
2b. **横截面文档同步（2026-08-10 确立，强制）**：总账是时间维度决策史（纵向），架构文档（00-summary/01-overview/02-desktop/03-server/04-data-model/05-dual-mode/08-shared/09-security/16-spec/14-blueprint 等）是当前态快照（横向）。每个统一/合并/重构批次验收必须同步受影响横截面文档（横截面同步清单），**禁止只更新总账而不同步架构文档**。详见 skill 规则。
3. **以文档为准（2026-08-06 确立，强制）**：文档定义设计态（系统应该是什么），代码实现当前态（系统现在是什么）。文档与代码冲突时——先更新文档、再按文档改代码，禁止跳过文档直接改代码。文档是 SSOT：同一信息点只有一个权威定义（见 `docs/00-governance/02-ssot-architecture.md`），禁止在代码或新文档中引入权威文档未定义的设计。任务开始时先查文档查询指南 `docs/README.md#ai-查询指南`。
3b. **需求先行门禁（2026-08-11 确立，强制）**：权威来自「需求→设计→代码」链条的源头——需求文档（docs/02-requirements/，业务「要什么」）是最高权威，设计文档（03-architecture/ + ADR）次之，代码是实现。**新功能/变更必须先过需求**：需求深化 → 用户确认 → 设计 → ADR → 代码，禁止跳过需求直接写代码。开工前必须查 `docs/compose/reports/requirements-coverage-matrix-2026-08-11.md`（R1 覆盖矩阵）定位需求依据；需求文档状态列（✅/⚠️/🔴/🧲/📦）是需求-代码对齐 SSOT（R2 已校准，2026-08-11），🔴 缺口进 backlog 修代码、⚠️ 部分实现按需补全，禁止文档与代码长期背离。
4. **先文档后代码 + 技术引入治理（2026-08-08 确立，强制）**：任何技术方案变更（T1 收敛/T2 调整/T3 引入新技术）必须**先更新权威文档再改代码**（见 `docs/00-governance/03-technical-adoption-governance.md`）；新技术引入必须走「深度分析 → 有依据 → 修改方案文档 → 用户审批 → 执行」流程，禁止随意引包。**文档是字典不是过程**：正式目录只放当前态定义，过程记录（任务书/审计报告/分析）一律归档 `docs/compose/`。
5. **修改后自动提交**：代码/文档修改验证通过（`dotnet build` 或相关测试）后自动 `git add` + `git commit`，除非用户明确说「先不要提交」。
6. **声称完成必有证据**：`dotnet build` 通过（**0 错误 0 警告**，存量警告一并修复，验证用 `--no-incremental` 强制全量编译）。

---

## 🛠️ Mimo Code 执行守则（每次启动任务必读）

> 详细守则（完整 Common Pitfalls、Key Patterns、命令速查、文档导航）见项目级 skill：**`.mimocode/skills/lybtzys-coder-rules/SKILL.md`**（本仓库已内置，Mimo `skill_search` 可搜到；找不到时直接读取该文件）。

1. **0 错误 0 警告（硬性门禁）**：`dotnet build LYBTZYZS.sln --no-incremental` 必须 0 错误 **且 0 警告**；存量警告一并修复，不允许带警告交付。
2. **先文档后代码**：文档是设计态（SSOT），代码是当前态。任务开始前先查 `docs/README.md#ai-查询指南` 定位权威文档；文档与代码冲突时先更新文档再改代码；禁止引入权威文档未定义的设计；禁止随意引入新包/新技术。
2b. **需求先行门禁（2026-08-11，强制）**：需求文档（docs/02-requirements/）是最高权威。新功能/变更必须先过需求（需求深化→用户确认→设计→ADR→代码），禁止跳过需求直接写代码；开工前查 R1 覆盖矩阵 `docs/compose/reports/requirements-coverage-matrix-2026-08-11.md` 定位需求依据；需求文档状态列（✅/⚠️/🔴/🧲/📦）是需求-代码对齐 SSOT，🔴 缺口进 backlog 不擅自实现。
3. **外科手术式修改**：只改任务要求的代码，不「顺手」改相邻代码/注释/格式；每行改动可追溯到任务需求。发现错误设计直接重写为正确版本，不做兼容层。
4. **架构约束（不可违反）**：3-Layer（Controller→Service→Repository→DbContext）；模块间禁止直接引用（P07）；跨模块必须用接口（P08）；Service 禁注入 AppDbContext（P10）；**权限/端点变更必须同时改 Remote Server（`src/Server/Services/LYBT.WebAPI/Controllers/`）与 Desktop LocalWebAPI（`src/Client/Desktop/LocalWebAPI/Controllers/`）双控制器树**。
5. **提交规范**：验证通过后 `git add` 具体文件 → `git commit`（英文，`feat/fix/docs/refactor/test(模块): 描述`）→ `git push origin master`。除非用户明确说先不提交/先不推送。
6. **声称完成必有证据**：报告真实 build/测试输出，不轻信自报成功；交付前跑完整 build + 相关测试。
6b. **横截面文档同步（2026-08-10，强制）**：交付涉及架构/数据模型/安全/双模式等横截面文档（`00-architecture-summary`/`03-server`/`04-data-model`/`05-dual-mode`/`08-shared`/`09-security-architecture`/`16-desktop-architecture-spec` 等）的任务，验收时同步更新对应文档；禁止只更新总账而不同步架构文档。

---

## 快速入口

| 内容 | Hermes | Mimo Code |
|------|--------|-----------|
| 完整开发规范 | `skill_view(name='lybtzys-coder-rules')` | `.mimocode/skills/lybtzys-coder-rules/SKILL.md` |
| 需求分析师（通用） | `skill_view(name='requirements-analyst')` | 通用方法论（任何项目） |
| 需求分析师（LYBTZYZS） | `skill_view(name='lybtzys-requirements-analyst')` | 项目细节（路径/编号/踩坑） |
| 系统设计师（通用） | `skill_view(name='system-designer')` | 通用方法论（任何项目） |
| 系统设计师（LYBTZYZS） | `skill_view(name='lybtzys-system-designer')` | 项目细节（文档清单/DP/踩坑） |
| 项目总账（任务状态） | `docs/03-architecture/13-project-master-plan.md` | 同上（编号任务完成后更新状态） |
| 架构决策记录 (ADR) | `docs/03-architecture/decisions/` | 同上 |
| 文档查询指南 | `docs/README.md#ai-查询指南` | 同上 |
| 需求文档 | `docs/02-requirements/` | 同上 |
| 架构文档 | `docs/03-architecture/` | 同上 |
| **Project 文档规范** | `docs/00-governance/04-project-doc-standard.md` | 同上（新建/改 project 文档必读） |

## 关键命令

```bash
dotnet build LYBTZYZS.sln --no-incremental # 门禁：0 错误 0 警告（必须用 --no-incremental）
dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (LocalDB)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
# Migration:
dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

## 技术栈速览

. NET 8 | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

## Git

Remote=Gitee(`gitee.com/shouqitao/LYBTZYZS.git`) 非 GitHub｜Branch=`master`｜Commit 英文 `feat/fix/docs/refactor/test(模块): 描述`
