# Task Plan: 文档体系重构

## Goal
基于已确认的文档体系标准，将 docs/ 从 608 个文件/17 个目录重构为约 35 个高质量文档/6 个目录。以产品需求文档 (PRD) 为核心，建立清晰、无冗余、可维护的文档体系。

## Current Phase
ALL PHASES COMPLETE. 40/40 Tasks done.

## Design Reference
- 设计文档: `docs/plans/2026-02-10-documentation-system-design.md`
- 实施计划: `docs/plans/2026-02-10-documentation-system-plan.md` (40 Tasks)

---

## Executive Summary

| 指标 | 当前 | 目标 |
|------|------|------|
| **文件数** | 608 | ~35 |
| **目录数** | 17 (含18个空目录) | 6 + assets/ + plans/ |
| **总行数** | 444,670 | TBD (精炼后) |
| **分类体系** | Diataxis (执行不一致) | 6层递进 (product→requirements→architecture→api→dev→ops) |
| **需求文档** | 分散在48个OpenSpec spec中 | 统一PRD (8个模块文件) |

---

## Phases

### Phase 1: 目录骨架 + 01-product/ (Task 1-5)
- [x] Task 1: 创建 6 个新目录骨架
- [x] Task 2: docs/01-product/README.md - 产品概述
- [x] Task 3: docs/01-product/vision.md - 产品愿景与目标
- [x] Task 4: docs/01-product/glossary.md - 术语表 (中英文对照)
- [x] Task 5: docs/01-product/user-roles.md - 用户角色与权限定义
- **Status:** complete
- **Parallelism:** Task 2-5 可并行

### Phase 2: 02-requirements/ (Task 6-15)
- [x] Task 6: docs/02-requirements/README.md - 需求总览
- [x] Task 7: docs/02-requirements/auth.md - 认证与会话管理 (FR-AUTH-001~013)
- [x] Task 8: docs/02-requirements/users.md - 用户管理 (FR-USER-001~012)
- [x] Task 9: docs/02-requirements/patients.md - 患者管理 (FR-PAT-001~012)
- [x] Task 10: docs/02-requirements/herbs.md - 药材管理 (FR-HERB-001~013)
- [x] Task 11: docs/02-requirements/formulas.md - 验方管理 (FR-FORM-001~013)
- [x] Task 12: docs/02-requirements/medical-cases.md - 医案管理 (FR-MC-001~017, 核心)
- [x] Task 13: docs/02-requirements/sync.md - 数据同步 (FR-SYNC-001~008)
- [x] Task 14: docs/02-requirements/printing.md - 打印功能 (FR-PRINT-001~004)
- [x] Task 15: 回填 README.md 功能数 (总计 92 个 FR)
- **Status:** complete
- **Parallelism:** Task 7-14 可并行 (核心产出)

### Phase 3: 03-architecture/ (Task 16-23)
- [x] Task 16: docs/03-architecture/README.md - 架构总览
- [x] Task 17: docs/03-architecture/system-overview.md - 系统架构图
- [x] Task 18: docs/03-architecture/server.md - 服务端架构
- [x] Task 19: docs/03-architecture/desktop.md - 桌面端架构
- [x] Task 20: docs/03-architecture/shared.md - 共享层架构
- [x] Task 21: docs/03-architecture/dual-mode.md - 双模式架构 (本地+远程)
- [x] Task 22: docs/03-architecture/data-model.md - 数据模型
- [x] Task 23: docs/03-architecture/decisions/ - ADR 提取 (6 个 ADR)
- **Status:** complete
- **Parallelism:** Task 17-22 可并行

### Phase 4: 04-api-reference/ (Task 24-30)
- [x] Task 24: docs/04-api-reference/README.md - API 总览
- [x] Task 25: docs/04-api-reference/auth.md - 认证 API
- [x] Task 26: docs/04-api-reference/users.md - 用户 API
- [x] Task 27: docs/04-api-reference/patients.md - 患者 API
- [x] Task 28: docs/04-api-reference/herbs.md - 药材 API
- [x] Task 29: docs/04-api-reference/formulas.md - 验方 API
- [x] Task 30: docs/04-api-reference/medical-cases.md - 医案 API + sync.md
- **Status:** complete
- **Parallelism:** Task 25-30 可并行

### Phase 5: 05-development/ + 06-operations/ (Task 31-36)
- [x] Task 31: docs/05-development/README.md - 快速开始
- [x] Task 32: docs/05-development/setup.md - 环境搭建
- [x] Task 33: docs/05-development/code-standards.md - 编码规范
- [x] Task 34: docs/05-development/patterns.md - 设计模式速查
- [x] Task 35: docs/05-development/testing.md - 测试指南
- [x] Task 36: docs/06-operations/ (README + deployment + configuration)
- **Status:** complete
- **Parallelism:** 全部可并行

### Phase 6: 清理旧文档 + 更新引用 (Task 37-40)
- [x] Task 37: 编写 docs/README.md 导航入口
- [x] Task 38: 删除全部旧目录 (17个旧目录已删除)
- [x] Task 39: 精简项目根 README.md (416行 -> 88行)
- [x] Task 40: 更新 CLAUDE.md 引用路径 (已确认无需修改)
- **Status:** complete
- **Parallelism:** 顺序执行

---

## Task 依赖关系

```
Task 1 (目录骨架)
  ├→ Task 2-5 (01-product/, 可并行)
  │    └→ Task 6-14 (02-requirements/, 可并行)
  │         ├→ Task 15 (回填 README 功能数)
  │         └→ Task 16-23 (03-architecture/, 可并行)
  │              └→ Task 24-30 (04-api-reference/, 可并行)
  │                   └→ Task 31-36 (05-dev + 06-ops, 可并行)
  │                        └→ Task 37 (docs/README.md)
  │                             └→ Task 38 (清理旧文档)
  │                                  └→ Task 39-40 (更新根目录)
```

---

## Decisions Made

| Decision | Rationale | Date |
|----------|-----------|------|
| 6 目录扁平结构 | 消除 17 目录嵌套混乱，数字前缀保证排序 | 2026-02-10 |
| 需求文档双模式对比 | 本地+远程是核心特性，每个功能必须明确两种模式行为 | 2026-02-10 |
| FR 编号体系 | 全局唯一功能编号，可追踪 | 2026-02-10 |
| OpenSpec 合并后废弃 | 48 个 spec 的业务规则合并到需求层，架构规则合并到架构层 | 2026-02-10 |
| 过程文档全删 | 有标准后过程文档不再需要，git history 已记录一切 | 2026-02-10 |
| 中文正文 + 英文技术标识 | 不翻译代码标识符，保持一致性 | 2026-02-10 |
| 40 Task 实施计划 | 细粒度可并行，每 Phase 内部最大化并行 | 2026-02-10 |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| (无) | | |

---
**Started**: 2026-02-10
**Last Updated**: 2026-02-10 (ALL COMPLETE - 40/40 Tasks)
