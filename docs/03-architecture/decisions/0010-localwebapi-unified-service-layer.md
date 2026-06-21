# ADR-0010: LocalWebAPI 统一服务层 — 文档化跨层引用例外

**Date**: 2026-06-21  
**Status**: ACCEPTED  
**Supersedes**: None  
**Related**: ADR-0002 (双模式架构), ADR-0009 (URL 驱动双模式)

## Context

LYBTZYZS Desktop 客户端支持双模式运行：远程模式（连接远程 WebAPI → SQL Server）和本地模式（嵌入式 LocalWebAPI → LocalDB）。架构规则"Server 和 Client 不互相引用"在此处存在唯一例外。

## Decision

**保留 LocalWebAPI 对 Server 层的直接引用（unified service layer）。不解耦。**

LocalWebAPI 引用以下 Server 项目：
- `LYBT.Entities`（领域实体）
- `LYBT.Infrastructure`（AppDbContext, BaseRepository）
- `LYBT.Module.Auth`（IAuthService）
- `LYBT.Module.Users`（IUserService）
- `LYBT.Module.Patients`（IPatientService）
- `LYBT.Module.Herbs`（IHerbService）
- `LYBT.Module.Formulas`（IFormulaService）
- `LYBT.Module.MedicalCases`（IMedicalCaseFacade）
- `LYBT.Module.Registration`（IRegistrationService）
- `LYBT.Module.Reports`（IReportsService）

## Rationale

1. **逻辑不偏离**：远程和本地共享同一套 Service 代码，确保两种模式行为一致。解耦会创造两套实现，导致 bug 在模式间不一致。

2. **零代码重复**：一个 Service 实现，bug 修复和新功能自动在两种模式生效。

3. **UI 无感知**：Desktop 始终通过 HTTP/Refit 调用 API，SwitchingApiClient 透明切换 URL。UI 代码与运行模式完全解耦。

4. **ROI 不足**：解耦方案（接口提取）是"假隔离"——编译时隔了一层但运行时依赖不变。真正的解耦（移除 LocalWebAPI）需要重写 8 个 Service + 16 个 Repository，工作量大且维护成本高。

## Consequences

- 架构测试 P21 从"跳过"改为"例外审计"：验证引用列表与本 ADR 一致
- 新增 Server Module 引用到 LocalWebAPI 时，必须先更新本 ADR
- Desktop 编译依赖 Server 项目（在 monorepo 中可接受）
- 部署包包含 Server DLL（~5-10MB，可接受）

## 变更协议

如需修改 LocalWebAPI 的 Server 引用列表：
1. 更新本 ADR 的引用列表
2. 更新 `src/Client/Desktop/LocalWebAPI/AGENTS.md` 依赖图
3. 确保 P21 审计测试通过
