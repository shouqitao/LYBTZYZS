# 需求总览

## 概述

本目录包含凌隐宝堂中医诊所管理系统所有业务模块的功能需求规格。每个功能使用全局唯一的 FR 编号标识，每个功能条目包含远程/本地双模式的行为对比。

---

## 模块索引

| 模块 | 文件 | FR 编号范围 | 功能数 |
|------|------|------------|--------|
| 认证与会话管理 | [auth.md](auth.md) | FR-AUTH-001 ~ 013 | 13 |
| 用户管理 | [users.md](users.md) | FR-USER-001 ~ 012 | 12 |
| 患者管理 | [patients.md](patients.md) | FR-PAT-001 ~ 013 | 13 |
| 药材管理 | [herbs.md](herbs.md) | FR-HERB-001 ~ 013 | 13 |
| 验方管理 | [formulas.md](formulas.md) | FR-FORM-001 ~ 013 | 13 |
| 医案管理 | [medical-cases.md](medical-cases.md) | FR-MC-001 ~ 018 | 18 |
| 数据同步 | [sync.md](sync.md) | FR-SYNC-001 ~ 008 | 8 |
| 打印 | [printing.md](printing.md) | FR-PRINT-001 ~ 004 | 4 |
| 身份证读卡器 | [card-reader.md](card-reader.md) | FR-CARD-001 ~ 002 | 2 |
| 系统健康与诊断 | [health-diagnostics.md](health-diagnostics.md) | FR-SYS-001 ~ 009 | 9 |
| 异常处理策略 | [error-handling.md](error-handling.md) | FR-ERR-001 ~ 008 | 8 |
| 日志与审计 | [logging.md](logging.md) | FR-LOG-001 ~ 007 | 7 |
| Desktop Shell | [desktop-shell.md](desktop-shell.md) | FR-SHELL-001 ~ 007 | 7 |
| 配置参数 | [configuration.md](configuration.md) | FR-CFG-001 ~ 004 | 4 |
| **非功能性需求** | **[nfr.md](nfr.md)** | **NFR-PERF/DATA/AVAIL/SEC** | **跨模块** |
| **UI/UX 交互规范** | **[ui-patterns.md](ui-patterns.md)** | **UI-D01~D06** | **跨模块** |

> **总计: 131 个功能需求 (14 个模块) + NFR 文档 (性能/数据量/可用性/安全)**

---

## FR 编号规则

| 组成 | 格式 | 示例 |
|------|------|------|
| 前缀 | `FR` | 固定 |
| 模块缩写 | `AUTH` / `USER` / `PAT` / `HERB` / `FORM` / `MC` / `SYNC` / `PRINT` / `CARD` / `SYS` / `ERR` / `LOG` / `SHELL` / `CFG` | 见模块索引 |
| 序号 | 三位数字 | `001`, `002`, ... |
| 完整格式 | `FR-{MODULE}-{NNN}` | `FR-MC-005` |

编号在模块内连续递增，不跳号。新增功能追加到末尾。

---

## 双模式标注说明

每个功能条目包含远程模式和本地模式的行为描述:

| 标注 | 含义 |
|------|------|
| **远程模式** | WPF → HTTP API → Controller → Service → SQL Server |
| **本地模式** | WPF → DataSource → SQLite |
| **不支持** | 该功能在本地模式下不可用 |
| **已确定** | 本地模式下的行为已基于代码事实确定 (详见各模块文档) |

---

## 需求文档模板

每个模块文档遵循统一模板:

```
# [模块名] 需求规格
## 概述 (业务目标)
## 用户角色 (权限矩阵)
## 功能清单 (FR-xxx-NNN 列表)
## 数据模型 (涉及的实体)
## 错误码 (错误场景/HTTP状态码/用户消息/触发条件)
## 决策记录
## 变更记录
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
| 2026-02-18 | v1.5 | PRD全量闭环分析: 16个问题(14修复+2非问题)。新增FR-MC-018(复制历史处方)+FR-PAT-013(患者状态管理)，FR总数129->131。错误码全量分配90个编号(MCCEE体系)，缓存失效策略完整重写，NFR-API-001全局分页规范，10个PRD文件更新 |
