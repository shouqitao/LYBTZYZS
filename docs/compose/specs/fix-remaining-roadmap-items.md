---
feature: fix-remaining-roadmap-items
status: in-progress
updated: 2026-09-17
branch: master
commits: 91f31c446..HEAD
---

# 按路线图修复剩余 P1-P3

## Report

## [S1] Problem

复审后仍有 1 P1（R-7 需独立批次）、10 P2、8 P3 未修复。用户要求按改进建议路线图一次性修复。

## [S2] Design

### P2 批次 A — Server CQRS + 事件闭环
- R-9: BatchDelete/SetPrescriptionFlag/RecordPrint/UpdateStatus 迁移到 CommandHandler
- R-10: MedicalCaseDeletedEvent 替换软删直调
- R-13: 补领域事件 Handler 测试 + 架构守卫
- R-14: Remote/Local UpdateStatus 契约对齐

### P2 批次 B — 安全 + 性能
- R-19: LocalWebAPI JWT 365 天改短周期
- R-22: 补 SecurityHeadersMiddleware 测试
- R-23: 写端点补限流标注
- R-25: 报表查询优化
- R-27: 清理已知失败测试

### P3 批次 — 清理
- AsNoTracking 一致性
- PreloadModulesAsync 双数据源收敛
- 死配置清理
- 依赖版本更新

## [S3] Out of Scope

- R-7: SQL 集成测试基建重建（需独立批次，工作量大）
- R-24: ValidateTokenReplay（设计决策，当前关闭是已知取舍）

## Tasks
- [ ] T1: P2-A Server CQRS + 事件闭环 (covers: S2)
- [ ] T2: P2-B 安全 + 性能 (covers: S2)
- [x] T3: P3 清理 (covers: S2) — PreloadModulesAsync→IRoleRegistry.RequiredModules；Patient/Herb/Formula/MedicalCase/User/Registration 列表查询 AsNoTracking；InternalPermitLimit/InternalQueueLimit/AdminPermitLimit 标预留；ModuleCoordinator ParallelGroup=null；启动步骤 Name 英文对齐 DI key；appsettings/模板 Encrypt=True
- [ ] T4: 全量构建验证 (depends: T1, T2, T3)
