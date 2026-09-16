---
feature: fix-p2-architecture-issues
status: in-progress
updated: 2026-09-16
branch: master
commits: b369479ff..HEAD
---

# 修复 P2 架构审查问题

## Report

## [S1] Problem

全项目架构审查发现 35 个 P2 中等问题。P1 批次已完成，本批次修复可机械执行的 P2 项。

## [S2] Design

### 批次 A — Shared 层快速修复
- H-2: MedicalCaseDetailDto.ClinicLocalDate 改为调用 MedicalCaseTime.ClinicLocalDate
- H-3: AppException.ErrorCode 改 init-only
- H-4: ValidationException.GetHttpStatusCode 走 TypedErrorCode 映射
- H-6: ValidationConstants.UserNameMaxLength 改为 32
- H-12: Shared/README.md 移除已合并项目，补 LYBT.Entities
- H-13: Shared/AGENTS.md 补 LYBT.Entities

### 批次 B — Desktop 修复
- D-1: MedicalCaseEditContext Singleton → Scoped
- D-2: AppStartupOrchestrator 去容器化（构造注入 IEnumerable<IStartupStep>）
- D-3: 启动管线 Service 层去 MessageBox（改用 IUserNotificationService 或日志）
- D-5: SuperAdminRoleDefinition 补 AdminModule

### 批次 C — Server 修复
- S-3: CatalogDbContext Herb 内联配置改为 ApplyConfiguration(new HerbConfiguration())
- S-4: IdentityDbContext ApplicationUser 内联配置改为 ApplyConfiguration(new UserConfiguration())
- X-5: Token 旋转加事务（RefreshTokenCommandHandler + LoginCommandHandler）
- X-8: LoginCommandHandler 改 IOptionsMonitor

### 批次 D — 文档同步
- X-1: 04-testing.md 同步实际基建（Respawn 已删、4 项目）
- X-6: 09-security-architecture.md 安全基线表修正（DPAPI → 进程内存）
- X-9: .runsettings TestSessionTimeout 放宽

## [S3] Out of Scope

- S-1 领域事件框架（需独立设计，L 级）
- S-6 模块内部结构统一（需独立批次）
- X-3 异常双轨收口（跨端重构，需 ADR 推进）
- MedicalCases CQRS 化（L 级，需独立批次）
- P3 轻微问题（后续批次）

## Tasks
- [ ] T1: 批次 A — Shared 层 6 项快速修复 (covers: S2)
- [ ] T2: 批次 B — Desktop 4 项修复 (covers: S2)
- [ ] T3: 批次 C — Server 4 项修复 (covers: S2)
- [ ] T4: 批次 D — 文档同步 3 项 (covers: S2)
- [ ] T5: 全量构建验证 — acceptance: 0 错误 0 警告 + 架构测试通过 (depends: T1, T2, T3, T4)
