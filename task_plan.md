# PRD vs Code 偏差分类确认清单

## Goal
对 257 个偏差逐一分类: CODE(代码修复) / PRD(PRD修订) / DEFER(延期) / BOTH(双方调整)，形成用户可确认的清单文档。

## Phases

| Phase | 描述 | Status |
|-------|------|--------|
| 1 | 建立分类标准 + 横切面 X1~X8 分类 | complete |
| 2 | 5 个并行 Agent 处理 259 项偏差分类 | complete |
| 3 | 汇总输出确认清单文档 | complete |
| 4 | 更新 planning-with-files 三文件 | complete |

## Decisions
- 分类维度: CODE / PRD / DEFER / BOTH
- 判断依据: 安全性/MVP必要性/技术可行性/项目阶段
- 横切面 X1~X8 分类由用户在 Plan 阶段确认
- 输出: `docs/plans/2026-02-21-deviation-triage-checklist.md`

## 结果摘要 (用户已确认)
- **总计 259 项** (原报告 257 + MC P3 实际多 1 项 + Sync P2 实际多 1 项)
- CODE: **201** (77.6%) -- 代码修复
- PRD: **40** (15.4%) -- PRD 文档修订
- DEFER: **18** (6.9%) -- 延期到后续 Epic/Sprint
- BOTH: 0 -- 全部已确认方向
- 用户确认: 244 项自动确认 + 15 项人工确认

## Errors Encountered
| 错误 | 解决方案 |
|------|----------|
| 部分 P3 在原报告中仅有概括描述 | 5 个 Agent 根据概括内容生成了具体条目 |
| MC P3 / Sync P2 实际条目多于标题数 | 以实际列出条目为准，总数 259 |
