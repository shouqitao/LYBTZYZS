# Task Plan: Sprint 2 - Core Feature Fixes (Batch Execution)

## Goal

Sprint 2: 核心功能修复 -- 打印层级重构、字段验证值对齐、功能Bug修复、安全端点加固。共 51 项任务, 7 个 Batch。

## Execution Order

`Batch 1 -> Batch 2 -> Batch 3 -> Batch 5 -> Batch 4 -> Batch 6 -> Batch 7`

## Batch Status

| Batch | Theme | Tasks | Status |
|-------|-------|-------|--------|
| 1 | X8 实体基础 + PrintType + 索引 | 5 | pending |
| 2 | X8 打印逻辑 + 保护 | 10 | pending (blocked by B1) |
| 3 | X5 Server 侧验证对齐 | 11 | pending |
| 4 | X5 Desktop + 配置对齐 | 4 | pending |
| 5 | S4 Bug 修复 | 8 | pending |
| 6 | S4 剩余 + A2 安全加固 | 7 | pending (blocked by B3) |
| 7 | A2 架构测试 + PRD 文档 | 6 | pending (blocked by all) |

## Decisions

| Decision | Rationale |
|----------|-----------|
| Batch 1 暂不移除 Prescription 旧打印字段 | 标记 OpenSpec, Batch 2 统一清理 |
| X8-04~08 合并为 OnPrintCompleted | 同一逻辑的不同方面 |
| PrintType 放 Shared.Models | 跨 Server/Desktop 共享 |

## Errors Encountered

(Batch 1 尚未开始)
