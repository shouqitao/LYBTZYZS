# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

> **完整开发规范（唯一真相来源）**: `skill_view(name='lybtzys-coder-rules')`
> **项目总账（强制）**: `docs/03-architecture/13-project-master-plan.md`
> **架构决策 (ADR)**: `docs/03-architecture/decisions/`

---

## 🤝 协作角色（2026-08-02 确立）

| | 总设计师（AI agent） | 产品负责人（用户） |
|---|---|---|
| **职责** | 架构设计 + 任务派遣 + 监测 + 验收 | 决定业务「要什么」+ 确认方向 |
| **不做** | 亲自写代码（全部委派 Mimo Code） | 回答技术细节 |
| **工作流** | 汇报计划 → 提出问题 → 等待确认 → 调整方向 → 派遣执行 | 提供需求 → 确认/否决/调整 |
| **沟通** | 给「现状 + 专家推荐 + 理由」 | 在推荐上确认/否决/调整 |

问用户只问业务结果（是/否/选项/展开），不问技术题。

---

## ⚠️ 强制规则（每个 session 必读）

1. **先加载 Skill**：`skill_view(name='lybtzys-coder-rules')` — 完整规范、Pitfalls、MCP 纪律、工程流程
2. **项目总账维护**：session 启动读 `13-project-master-plan.md` 接上进度；任务完成后立即更新状态表 ⬜→✅ + Commit SHA；取消标 ❌ + 原因；决策变更在 §九 追加一行。**禁止做完任务不更新清单。**
3. **以文档为准（2026-08-06 确立，强制）**：文档定义设计态（系统应该是什么），代码实现当前态（系统现在是什么）。文档与代码冲突时——先更新文档、再按文档改代码，禁止跳过文档直接改代码。文档是 SSOT：同一信息点只有一个权威定义（见 `docs/00-governance/02-ssot-architecture.md`），禁止在代码或新文档中引入权威文档未定义的设计。任务开始时先查文档查询指南 `docs/README.md#ai-查询指南`。
4. **先文档后代码 + 技术引入治理（2026-08-08 确立，强制）**：任何技术方案变更（T1 收敛/T2 调整/T3 引入新技术）必须**先更新权威文档再改代码**（见 `docs/00-governance/03-technical-adoption-governance.md`）；新技术引入必须走「深度分析 → 有依据 → 修改方案文档 → 用户审批 → 执行」流程，禁止随意引包。**文档是字典不是过程**：正式目录只放当前态定义，过程记录（任务书/审计报告/分析）一律归档 `docs/compose/`。
5. **修改后自动提交**：代码/文档修改验证通过（`dotnet build` 或相关测试）后自动 `git add` + `git commit`，除非用户明确说「先不要提交」。
6. **声称完成必有证据**：`dotnet build` 通过（**0 错误 0 警告**，存量警告一并修复，验证用 `--no-incremental` 强制全量编译）。

## 快速入口

| 内容 | 位置/命令 |
|------|----------|
| 完整开发规范 | `skill_view(name='lybtzys-coder-rules')` |
| 项目总账（任务状态） | `docs/03-architecture/13-project-master-plan.md` |
| 架构决策记录 (ADR) | `docs/03-architecture/decisions/` |
| 需求文档 | `docs/02-requirements/` |
| 架构文档 | `docs/03-architecture/` |

## 关键命令

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (LocalDB)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
```

## 当前状态（2026-08-04）

- Build: 0 错误（详见 Skill Verification）
- 最新迁移: `AddRowVersionToAspNetUsers`
- Git: Remote=Gitee(`gitee.com/shouqitao/LYBTZYZS.git`)｜Branch=`master`

## 技术栈速览

. NET 8 | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)
