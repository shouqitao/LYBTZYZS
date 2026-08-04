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
3. **修改后自动提交**：代码/文档修改验证通过（`dotnet build` 或相关测试）后自动 `git add` + `git commit`，除非用户明确说「先不要提交」。
4. **声称完成必有证据**：`dotnet build` 通过（**0 错误 0 警告**，存量警告一并修复，验证用 `--no-incremental` 强制全量编译）。

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
