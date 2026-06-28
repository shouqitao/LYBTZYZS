# Compose 工作流产物

> 本目录托管 `compose:*` 工作流（brainstorm → spec → plan → review → report）产生的全部文档。
> 与 `docs/01-06*` 的「稳定文档」相对，这里放的是**过程性、迭代性**的工作产物。

---

## 目录结构

| 子目录 | 用途 | 活跃 | 归档 |
|--------|------|------|------|
| [`specs/`](specs/) | 设计规格（spec）—— 目标态、决策、功能设计 | 17 | 22 |
| [`plans/`](plans/) | 实施计划（plan）—— 落地步骤、任务分解 | 9 | 39 |
| [`reports/`](reports/) | 审查报告（report）—— 审计、对账、回顾（保留原位，不归档） | 4 | — |
| `specs/archive/` · `plans/archive/` | 已完成并归档的项目（历史快照，不再更新） | — | — |

> 文件数为 2026-06-28 统计（结构治理后）。活跃项 = 当前迭代中的设计/计划；归档项 = 已实施完成的历史快照。

---

## 命名规约

**强制格式**：

```
YYYY-MM-DD-<slug>-{design|impl}.md
```

- **日期**：工作启动日（ISO 8601）
- **slug**：小写 kebab-case 主题词（如 `shell-redesign`、`docs-deep-audit`）
- **后缀**：
  - `-design.md` → 设计规格（放 `specs/`）
  - `-impl.md` → 实施计划（放 `plans/`）
  - 报告类无需后缀，直接 `YYYY-MM-DD-<slug>.md`（放 `reports/`）

**示例**：

```
specs/2026-06-28-shell-phase2-design.md          ✅
plans/2026-06-28-shell-phase2-impl.md            ✅
reports/2026-06-28-docs-deep-audit.md            ✅
specs/Shell Redesign.md                          ❌ 缺日期、空格、无后缀
```

---

## 归档规则

一个工作项**完成并合并**后：

1. 将其 `specs/` 与 `plans/` 文件移入对应 `archive/` 子目录
2. `reports/` 文件**保留原位**（报告是历史记录，不归档）
3. 归档不修改文件名（保留可追溯性）

> `archive/` 内文件视为历史快照，不再更新；如需复用结论，请新建 spec 引用之。

---

## 与稳定文档的关系

| compose 产物 | 稳定文档（`docs/01-06*`） |
|--------------|---------------------------|
| 过程性、迭代、可能被推翻 | 权威、稳定、目标是最终态 |
| 设计决策的「草稿」 | 设计决策的「定稿」 |
| 完成后归档 | 持续维护 |

**原则**：compose 产物中**已定稿、已落地**的结论应同步回 `docs/01-06*` 对应文档；compose 文件不作为长期引用源。

---

## 相关

- 工作流命令：`compose:brainstorm` / `compose:spec` / `compose:plan` / `compose:review`（见根 `AGENTS.md` 工程流程段）
- 弃用目录：[`docs/plans/`](../plans/README.md)（已重定向至此）

---

*最后更新: 2026-06-28（结构治理：34 份已完成 spec/plan 迁入 archive）*
