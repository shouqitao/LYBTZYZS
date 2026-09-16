---
feature: fix-architecture-deep-items
status: done
updated: 2026-09-16
branch: master
commits: 52db8b76a..HEAD
---

# 架构级深度改进

## Report

## [S1] Problem

架构审查遗留 5 项需独立设计的深度改进：领域事件框架（S-1）、模块结构统一（S-6）、异常双轨收口（X-3）、MedicalCases CQRS 化、Registration 目录重命名（P3-4）。

## [S2] Design

### S-1: 领域事件基础框架
按 ADR-0018 实现 `IDomainEvent : INotification` + `IDomainEventDispatcher`。首个用例：MedicalCase 完成/取消 → Registration 状态联动（当前走同步 IXxxCrossModuleService）。Outbox 延后 v2.0，当前直接投递。

### S-6 + MedicalCases CQRS: 模块结构统一
为 MedicalCases 模块添加 Application/ 层（Commands/Queries/Validators/Mappers），与 Patients/Catalog/Identity 对齐。将核心写操作（Create/Update/Delete/Complete/Cancel/Suspend）迁移为 CommandHandler，读操作迁移为 QueryHandler。Controller 改为注入 ISender。

### X-3: 异常双轨收口
Server 端异常路径当前写 ApiResponse，ProblemDetails 已注册但不参与。将异常响应统一为 ProblemDetails（RFC 7807），ApiResponse 仅用于成功响应。需同步 Desktop 端反序列化。

### P3-4: Registration 目录重命名
目录 `LYBT.Module.Registration` → `LYBT.Module.Registrations`（与程序集名一致），更新 sln 和所有 ProjectReference。

## [S3] Out of Scope

- Outbox 模式（v2.0）
- 全部 10 个 MedicalCases Service 的完整迁移（分批进行，本批次覆盖核心操作）
- Desktop 端 ADR-0020 错误契约全面对齐（独立批次）

## Tasks
- [x] T1: P3-4 Registration 目录重命名 (covers: S2) — 目录已为 `LYBT.Module.Registrations`
- [x] T2: S-1 领域事件基础框架 + MedicalCase→Registration 事件化 (covers: S2) — `SharedKernel/Events/` + `MedicalCaseCompleted/CancelledEventHandler`
- [x] T3: S-6+MedicalCases CQRS — Application/ 层 + 核心 Command/Query Handler (covers: S2)
- [x] T4: X-3 异常双轨收口 — Server 端 ProblemDetails 统一 (covers: S2) — 2026-09-16
- [ ] T5: 全量构建验证 (depends: T1, T2, T3, T4) — 由父代理统一执行 `dotnet build LYBTZYZS.sln --no-incremental` + 测试
