# Task Plan

## Goal
基于PRD全量闭环分析(16个问题修复)的结果，更新设计文档(架构层+API层)，确保设计文档与PRD保持同步。

## Current Phase
Phase 1: BRAINSTORM -> complete

---

## Phases

### Phase 1: BRAINSTORM - 调研现有设计文档现状，识别差距
- Status: complete
- Tasks:
  - [x] 读取 docs/03-architecture/ 全部文档，了解现有架构设计
  - [x] 读取 docs/04-api-reference/ 关键文档，了解现有API设计
  - [x] 对比PRD变更清单，识别设计文档差距
  - [x] 形成分阶段工作计划

### Phase 2: 数据模型更新 (data-model.md) [P0]
- Status: pending
- Tasks:
  - [ ] MedicalCase 实体: 新增 IsPrinted(bool) 字段 (从 Prescription 提升, MC-D15)
  - [ ] MedicalCase 实体: 新增 EditReason(string?) 字段 (打印后修改需填写原因)
  - [ ] Patient 实体: 明确 Status:CommonStatus 语义 (PAT-D05 禁用=患者已故)
  - [ ] Prescription 实体: 确认 LastPrintedAt/PrintVersion 字段描述与PRD一致
  - [ ] 变更记录更新

### Phase 3: 患者API设计 (04-api-reference/patients.md) [P0]
- Status: pending
- Tasks:
  - [ ] 新增 PUT /patients/{id}/status 端点 (FR-PAT-013)
  - [ ] 补充请求/响应DTO、权限、错误码
  - [ ] 变更记录更新

### Phase 4: 医案API补充 (04-api-reference/medical-cases.md) [P1]
- Status: pending
- Tasks:
  - [ ] 文档化 FR-MC-018 复制历史处方的组合API实现路径
  - [ ] 补充 IsPrinted 相关的业务规则说明 (MC-D15)
  - [ ] 补充 EditReason 字段在更新API中的说明
  - [ ] 变更记录更新

### Phase 5: 架构设计补充 (server.md) [P1]
- Status: pending
- Tasks:
  - [ ] 新增缓存失效策略章节 (OutputCache + Desktop缓存, 来自nfr.md v1.2)
  - [ ] 错误码体系与MCCEE对齐验证 (确认前缀分配一致)
  - [ ] 变更记录更新

### Phase 6: 索引同步与收尾 [P2]
- Status: pending
- Tasks:
  - [ ] 04-api-reference/README.md 端点总数更新
  - [ ] shared.md MC-D16 角色脱敏DTO补充 (如需要)
  - [ ] 全量交叉验证: 确认7项PRD变更均有设计文档覆盖
  - [ ] progress.md 最终总结

---

## Decisions Made
- FR-MC-018 不新增专用API端点，通过现有端点组合实现 (客户端驱动模式)
- Patient 状态管理复用 toggle-status 模式 (参考 herbs/users 已有端点)

## Errors Encountered
(无)
