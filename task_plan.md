# Task Plan

## Goal
完成两个遗留独立任务: (1) 信息保护深化 -- 统一敏感数据保护策略; (2) MedicalCase 同步设计 -- 扩展同步模块支持医案聚合。

## Current Phase
Phase 5: VERIFY -> complete

---

## Phases

### Phase 1: BRAINSTORM - 需求讨论与方案设计
- Status: complete
- Tasks:
  - [x] 收集上下文 (NFR/sync.md/架构文档/Serena记忆/代码现状)
  - [x] 任务 A: 信息保护深化 - 3级分级+Value Converter+日志脱敏
  - [x] 任务 B: MedicalCase 同步设计 - 外出看诊工作流, 9 项决策

### Phase 2: PLAN - 详细实施计划
- Status: complete
- 确认修改 3 个文档: nfr.md + patients.md + sync.md

### Phase 3: EXECUTE - 文档编写
- Status: complete
- nfr.md v1.1, patients.md v1.6, sync.md v3.0

### Phase 4: REVIEW - 审查
- Status: complete
- 修复 4 个审查问题

### Phase 5: VERIFY - 验证
- Status: complete

---

## Decisions Made

| # | Decision | Rationale | Date |
|---|----------|-----------|------|
| A1 | 3级敏感数据分级 (L1/L2/L3) | 区分保护力度，避免过度加密 | 2026-02-18 |
| A2 | EF Core Value Converter 实现加密 | 对业务层透明，无需改 Repository/Service | 2026-02-18 |
| A3 | DPAPI 密钥生命周期: 生成→保护→丢失重同步 | 利用已有 CredentialVault 基础设施 | 2026-02-18 |
| A4 | 产出为需求文档补充 | 当前阶段完善 PRD 规格 | 2026-02-18 |
| B1 | 全状态双向同步 | 离线创建任何状态医案都能同步 | 2026-02-18 |
| B2 | 聚合级原子同步 | DDD 聚合一致性 | 2026-02-18 |
| B3 | 打印字段不参与同步 | 打印是本地行为 | 2026-02-18 |
| B4 | 自动强制依赖顺序 Herb→Patient→MC | 用户无需关心顺序 | 2026-02-18 |
| B5 | 患者去重: IdCardNumber匹配+PatientId重映射 | 忘记同步患者的恢复路径 | 2026-02-18 |
| B6 | CaseNumber/PrescriptionNumber Server重分配 | 全局序列一致 | 2026-02-18 |
| B7 | GUID保留本地生成值 | 全局唯一无冲突 | 2026-02-18 |
| B8 | BR-001冲突提示医生选择 | 单活跃医案约束 | 2026-02-18 |
| B9 | Checksum排除审计/打印/编号/冗余字段 | 避免假差异 | 2026-02-18 |

---

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
