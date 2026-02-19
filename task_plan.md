# Task Plan

## Goal
基于设计文档全量覆盖审查结果，修复全部暴露的问题: 1 CRITICAL + 8 HIGH + 4 MEDIUM系统性 + 14 FR PARTIAL + 5 NFR PARTIAL + 6 其他。

## Current Phase
ALL PHASES COMPLETE

---

## Phases

### Phase 1: CRITICAL -- dual-mode.md MedicalCase 同步不一致修复
- Status: **complete**
- Tasks: [x] 1.1~1.5 全部完成

### Phase 2: server.md 设计补全 (9 项)
- Status: **complete**
- Tasks: [x] 2.1~2.9 全部完成

### Phase 3: desktop.md 设计补全 (14 项)
- Status: **complete**
- Tasks: [x] 3.1~3.14 全部完成

### Phase 4: API Reference MCCEE 错误码补全 (8 文档)
- Status: **complete**
- Tasks: [x] 4.1~4.8 全部完成 (含 check-reference 端点 + 客户端错误码)

### Phase 5: 补充修复 (D维度 PARTIAL + data-model)
- Status: **complete**
- Tasks: [x] 5.1~5.5 全部完成

### Phase 6: 验证
- Status: **complete**
- Tasks:
  - [x] 6.1 交叉引用检查: 发现并修复 40 个错误路径 (../../ -> ../)
  - [x] 6.2 覆盖率抽检: 全部 MISSING/PARTIAL 项已修复 (见下方)
  - [x] 6.3 findings.md 最终覆盖率已更新

---

## Task Statistics

| Phase | 总任务 | 已完成 | 剩余 |
|-------|--------|--------|------|
| Phase 1 | 5 | 5 | 0 |
| Phase 2 | 9 | 9 | 0 |
| Phase 3 | 14 | 14 | 0 |
| Phase 4 | 8 | 8 | 0 |
| Phase 5 | 5 | 5 | 0 |
| Phase 6 | 3 | 3 | 0 |
| **合计** | **44** | **44** | **0** |

## Verification Results

### MISSING -> COVERED (8 items)
- FR-PAT-011/012: check-reference 端点已添加到 patients.md API
- FR-ERR-005/007/008: 客户端异常体系已设计在 desktop.md
- FR-LOG-003: SensitiveDataMaskingEnricher 设计已添加到 server.md
- FR-LOG-007: ApiLoggingFilter 设计已添加到 server.md
- FR-CFG-004: ProductionConfigurationValidator 设计已添加到 server.md

### PARTIAL -> COVERED (14 FR + 5 NFR + D items)
- 全部 14 项 FR PARTIAL 已补全设计 (desktop.md + server.md)
- 全部 5 项 NFR PARTIAL 已补全 (性能预算/加密/备份/清理)
- MC-D06/D07/D14, AUTH timeout, USER-D03 已补全

### DISCREPANCY -> RESOLVED (2 items)
- MedicalCase 同步 PRD-设计文档不一致已修复 (dual-mode.md v1.1)

### 链接修复
- 40 个 ../../02-requirements/ 错误路径修正为 ../02-requirements/

## Decisions Made
- 任务按目标文件分组 (非按问题类型)，减少文件切换
- API README 4 个多余错误码: 标注为设计扩展保留 (防御性措施)
- diagnostics.md 确认无 MCCEE 码需要 (PRD 仅定义泛化 HTTP 状态码)
- 交叉引用链接使用 ../02-requirements/ (同级目录间相对路径)

## Errors Encountered
| 错误 | 尝试 | 解决方案 |
|------|------|----------|
| 40个文档交叉引用路径错误 | 1 | 批量替换 ../../ -> ../ |
