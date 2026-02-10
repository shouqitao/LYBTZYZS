# Task Plan: 需求文档深化 -- 回填待讨论标记

## Goal
消除 docs/ 中全部 57 处"待讨论/TBD/待扩展"标记，基于代码事实回填明确决策。

## Current Phase
ALL PHASES COMPLETE. 12/12 Tasks done.

## Design Reference
- 设计文档: `docs/plans/2026-02-10-requirements-deepening-design.md` (22 决策点)
- 实施计划: `docs/plans/2026-02-10-requirements-deepening-plan.md` (12 Tasks)

---

## Phases

### Phase 1: 需求文档回填 (Task 1-8, 并行)
- [x] Task 1: auth.md - 4 处标记 (B-1, B-2: 自动登录/会话超时)
- [x] Task 2: users.md - 14 处标记 (A-2, A-3: 用户管理/Receptionist)
- [x] Task 3: patients.md - 5 处标记 (D-1, D-2: 导入导出/加密)
- [x] Task 4: herbs.md - 6 处标记 (D-1, F-1: 导入导出/价格快照)
- [x] Task 5: formulas.md - 5 处标记 (D-1, F-2: 导入导出/价格计算)
- [x] Task 6: medical-cases.md - 5 处标记 (E-1~E-3: 审计/编号/搜索)
- [x] Task 7: sync.md - 5 处标记 (C-1~C-5: 冲突/同步范围)
- [x] Task 8: printing.md - 4 处标记 (F-3~F-5: PDF/模板/批量)
- **Status:** complete

### Phase 2: 汇总与架构文档 (Task 9-11)
- [x] Task 9: README.md (requirements) - 2 处标记
- [x] Task 10: dual-mode.md - 6 处标记 (A-1, C-1~C-5)
- [x] Task 11: ADR-0002 - 1 处标记
- **Status:** complete

### Phase 3: 全量验证 (Task 12)
- [x] Task 12: 全量验证 + planning-with-files 更新
- **Status:** complete

---

## Decisions Made

| Decision | Rationale | Date |
|----------|-----------|------|
| 全部决策基于代码事实 | 15+ 核心源文件逆向分析，非猜测 | 2026-02-10 |
| 按文件分组 Task | 一个 Task 一个文件，原子性强 | 2026-02-10 |
| 最大化并行 | Task 1-8+10 无依赖，可 9 路并行 | 2026-02-10 |
| 3 批次执行 | Batch1: 5文件, Batch2: 4文件, Batch3: 2文件+验证 | 2026-02-10 |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| (无) | | |

---
**Started**: 2026-02-10
**Last Updated**: 2026-02-10 (ALL COMPLETE - 12/12 Tasks)
