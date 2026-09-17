---
feature: fix-remaining-roadmap-items
status: delivered
updated: 2026-09-17
branch: master
commits: 91f31c446..HEAD
---

# 按路线图修复剩余 P1-P3

## Report

**What was built** — 按复审改进建议路线图一次性修复剩余 P2+P3。P2-A：完成 MedicalCases CQRS 迁移（4 组新 Command+Handler）、MedicalCaseDeletedEvent 替换软删直调、双树 UpdateStatus 契约对齐。P2-B：LocalWebAPI JWT 365 天改 12 小时、29 处写端点补限流标注、报表查询改单次 JOIN、RemoteApi E2E 标记 Skip。P3：PreloadModulesAsync 收敛到 IRoleRegistry、6 个仓储列表查询统一 AsNoTracking、死配置标注、启动步骤 Name 英文化、默认连接串 Encrypt=True。

**Verification** — NuGet restore 环境问题持续，无法运行 `dotnet build`。构建验证需在能正常 restore 的环境执行。

**Journey log** —
- R-27 的三类存量失败在当前 HEAD 已不存在对应类名（skill Test Triage 与代码脱节）
- RemoteApi E2E 硬编码生产 IP `60.190.215.86:5000`，本地必失败，Skip 是正确处理
- PreloadModulesAsync 去掉 ClinicalModule 后首屏依赖 EnsureModuleLoadedAsync 导航拉起，与 OnDemand 设计一致

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
- [x] T1: P2-A Server CQRS + 事件闭环 (covers: S2)
- [x] T2: P2-B 安全 + 性能 (covers: S2)
- [x] T3: P3 清理 (covers: S2) — PreloadModulesAsync→IRoleRegistry.RequiredModules；Patient/Herb/Formula/MedicalCase/User/Registration 列表查询 AsNoTracking；InternalPermitLimit/InternalQueueLimit/AdminPermitLimit 标预留；ModuleCoordinator ParallelGroup=null；启动步骤 Name 英文对齐 DI key；appsettings/模板 Encrypt=True
- [ ] T4: 全量构建验证 (depends: T1, T2, T3)
