# Task Plan: Sprint 2 - Core Feature Fixes (Batch Execution)

## Goal

Sprint 2: 核心功能修复 -- 打印层级重构、字段验证值对齐、功能Bug修复、安全端点加固。共 51 项任务, 7 个 Batch。

## Execution Order

`Batch 1 -> Batch 2 -> Batch 3 -> Batch 5 -> Batch 4 -> Batch 6 -> Batch 7`

## Batch Status

| Batch | Theme | Tasks | Status |
|-------|-------|-------|--------|
| 1 | X8 实体基础 + PrintType + 索引 | 5 | **complete** |
| 2 | X8 打印逻辑 + 保护 | 10 | **complete** |
| 3 | X5 Server 侧验证对齐 | 11 | **complete** |
| 4 | X5 Desktop + 配置对齐 | 4 | pending |
| 5 | S4 Bug 修复 | 8 | pending |
| 6 | S4 剩余 + A2 安全加固 | 7 | pending (blocked by B3) |
| 7 | A2 架构测试 + PRD 文档 | 6 | pending (blocked by all) |

## Committed

- `a856fcb69` feat: X8 print hierarchy refactor + print-completed API (Sprint2-Batch1+2)

## Next: Batch 5 (S4 Bug 修复)

执行顺序: Batch 3 complete -> Batch 5 -> Batch 4 -> Batch 6 -> Batch 7

## Decisions

| Decision | Rationale |
|----------|-----------|
| Batch 1+2 合并提交 | 同属 X8 打印层级重构 |
| 打印保护条件: IsPrinted && IsCompleted | 允许未完成医案反复修改打印 |
| 回写容错: 失败不阻止打印 | UX 优先 |
| Herb Effect 不用 RemarkMaxLength 常量 | RemarkMaxLength=1000 不适合 Effect(500)，直接硬编码 500 |
| PatientInputDto: IdNumber/Phone/Address 格式验证保留 When 条件 | NotEmpty 已确保非空，格式验证仅在有值时触发避免双重错误 |
| EF 迁移含 Batch 1+2 打印字段迁移 | 因 Batch 1+2 未生成迁移，此次一并生成 |

## Errors Encountered

| Error | Resolution |
|-------|-----------|
| HerbService.cs 引用 Prescription.IsPrinted | 改为 mc.IsPrinted (join MedicalCase) |
| PrescriptionPrintLogConfiguration WithMany(p => p.PrintLogs) | 改 WithMany() 无导航 |
