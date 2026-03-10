# 需求总览

## 概述

本目录包含凌隐宝堂中医诊所管理系统所有业务模块的产品需求文档 (PRD)。每个模块文档遵循 PRD Development 10 章节标准结构，每个 User Story 使用全局唯一的 US 编号标识，包含验收标准、业务规则和远程/本地双模式行为。

---

## 模块索引

| 模块 | 文件 | US 编号范围 | User Story 数 |
|------|------|------------|--------------|
| 认证与会话管理 | [auth.md](auth.md) | US-AUTH-001 ~ 013 | 13 |
| 用户管理 | [users.md](users.md) | US-USER-001 ~ 012 | 12 |
| 患者管理 | [patients.md](patients.md) | US-PAT-001 ~ 013 | 13 |
| 药材管理 | [herbs.md](herbs.md) | US-HERB-001 ~ 013 | 13 |
| 验方管理 | [formulas.md](formulas.md) | US-FORM-001 ~ 013 | 13 |
| 医案管理 | [medical-cases.md](medical-cases.md) | US-MC-001 ~ 018 | 18 |
| 数据同步 | [sync.md](sync.md) | US-SYNC-001 ~ 008 | 8 |
| 打印 | [printing.md](printing.md) | US-PRINT-001 ~ 004 | 4 |
| 身份证读卡器 | [card-reader.md](card-reader.md) | US-CARD-001 ~ 002 | 2 |
| 系统健康与诊断 | [health-diagnostics.md](health-diagnostics.md) | US-SYS-001 ~ 009 | 9 |
| 异常处理策略 | [error-handling.md](error-handling.md) | US-ERR-001 ~ 008 | 8 |
| 日志与审计 | [logging.md](logging.md) | US-LOG-001 ~ 007 | 7 |
| Desktop Shell | [desktop-shell.md](desktop-shell.md) | US-SHELL-001 ~ 007 | 7 |
| 配置参数 | [configuration.md](configuration.md) | US-CFG-001 ~ 004 | 4 |
| 挂号管理 | [registration.md](registration.md) | US-REG-001 ~ 007 | 7 |
| **非功能性需求** | **[nfr.md](nfr.md)** | **NFR-PERF/DATA/AVAIL/SEC** | **跨模块** |
| **UI/UX 交互规范** | **[ui-patterns.md](ui-patterns.md)** | **UI-D01~D06** | **跨模块** |
| **角色权限与数据归属** | **[role-permission-matrix.md](role-permission-matrix.md)** | **-** | **跨模块** |

> **总计: 138 个 User Stories (15 个模块) + NFR 文档 (性能/数据量/可用性/安全)**
>
> **顶层 PRD**: [prd.md](prd.md) -- 产品需求文档总纲 (Executive Summary / Problem Statement / Success Metrics / Out of Scope / Dependencies & Risks)

---

## US 编号规则

| 组成 | 格式 | 示例 |
|------|------|------|
| 前缀 | `US` | User Story |
| 模块缩写 | `AUTH` / `USER` / `PAT` / `HERB` / `FORM` / `MC` / `SYNC` / `PRINT` / `CARD` / `SYS` / `ERR` / `LOG` / `SHELL` / `CFG` / `REG` | 见模块索引 |
| 序号 | 三位数字 | `001`, `002`, ... |
| 完整格式 | `US-{MODULE}-{NNN}` | `US-MC-005` |

编号在模块内连续递增，不跳号。新增 User Story 追加到末尾。

> **编号迁移**: 2026-03-06 从 `FR-{MODULE}-{NNN}` 迁移到 `US-{MODULE}-{NNN}`，编号序号不变。

---

## 双模式标注说明

每个 User Story 包含 Dual Mode 表格，描述远程模式和本地模式的行为:

| 模式 | 数据链路 |
|------|----------|
| **远程** | WPF → HTTP API → Controller → Service → SQL Server |
| **本地** | WPF → DataSource → SQLite |

特殊标注:
- **不支持**: 该 User Story 在本地模式下不可用
- **不适用**: 该 User Story 仅涉及远程模式特有功能 (如 JWT Token)

---

## 模块 PRD 模板

每个模块文档遵循 PRD Development 10 章节标准结构:

```
# [模块名] 产品需求文档
## 1. Problem Statement (问题描述 / 用户痛点 / 证据)
## 2. Target Users (角色权限矩阵)
## 3. Strategic Context (业务目标 / Why Now)
## 4. Solution Overview (模块能力概览 / 核心流程)
## 5. Success Metrics (量化成功指标)
## 6. Epic Hypothesis (可验证假设)
## 7. User Stories (US-xxx-NNN，含 Acceptance Criteria / Business Rules / Dual Mode)
## 8. Out of Scope (v1.0 排除项)
## 9. Dependencies & Risks (风险与缓解)
## 10. Open Questions (未决问题)
## Data Model (数据模型)
## Error Codes (错误码)
## Decision Log (决策记录 + 修订历史)
## Change Log (变更记录)
```

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-11 | v1.1 | 新增5个模块索引 (SYS/ERR/LOG/SHELL/CFG)，总计从94更新到120个FR。模板新增"错误码"章节 |
| 2026-02-17 | v1.2 | 新增非功能性需求 (NFR) 文档索引 |
| 2026-02-17 | v1.3 | 新增 UI/UX 交互规范 (ui-patterns.md) 文档索引 |
| 2026-02-17 | v1.4 | PRD审查Phase3: FR数量更新120->129 (SYS+2/ERR+3/LOG+3/CFG+1)，编号范围修正 |
| 2026-02-18 | v1.5 | PRD全量闭环分析: 新增FR-MC-018+FR-PAT-013，FR总数129->131 |
| 2026-03-06 | v1.6 | 新增顶层 PRD 文档 (prd.md) |
| 2026-03-06 | v2.0 | **PRD 全面重写**: 14 个模块从 "需求规格" 升级为 "模块 PRD" (10 章节结构)；131 FR 迁移为 131 US (User Story 格式)；编号体系 FR→US；模板从 7 章节升级为 14 章节 |
| 2026-03-06 | v2.1 | 新增 Registration 模块 (7 US)，总计 15 模块 138 US；模块缩写追加 REG |
