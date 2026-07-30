---
feature: architecture-cleanup-phase2
status: draft
updated: 2026-07-30
scope: 结构收敛（多 DbContext 统一 + Controller 去重 + 双写路径修复）
---

# Architecture Cleanup — Phase 2: Structural Convergence

## [S1] Problem

Phase 1 修复了 5 个 CRITICAL 正确性问题后，3 个 HIGH 级结构性问题仍存在：

| # | 问题 | 严重度 | 影响 |
|---|------|--------|------|
| H1 | 5 个模块 DbContext 与 AppDbContext 同表混用，模块 DbContext 缺失软删除过滤器 | HIGH | 同一张表两个 DbContext 读到的数据集不一致 |
| H2 | WebAPI/LocalWebAPI 10 对重复 Controller，Herbs/Formulas 授权策略反向 | HIGH | Local 端药材管理权限比 Remote 端更宽，安全策略漂移 |
| H3 | CreateMedicalCaseCommandHandler 绕过 Service 层验证 | MEDIUM | 跳过 BR-001（单患者单活动医案）和患者禁用检查 |

### 证据索引

| # | 证据 |
|---|------|
| H1 | `AuthDbContext.cs:24` ToTable("AuthSessions") — 与 AppDbContext 同表，无 HasQueryFilter |
| H1 | `HerbsDbContext.cs:22` ToTable("Herbs") — 同上 |
| H1 | `FormulaDbContext.cs:31` ToTable("Formulas") — 同上 |
| H1 | `UsersDbContext.cs:24` ToTable("Users") — 同上 |
| H1 | `ReportsDbContext` — 空壳，零 DbSet |
| H2 | `WebAPI FormulasController.cs:23` AdminOrSuperAdmin |
| H2 | `LocalWebAPI FormulasController.cs:17` DoctorOrReceptionist — 权限反向 |
| H2 | `WebAPI HerbsController.cs:24` AdminOrSuperAdmin |
| H2 | `LocalWebAPI HerbsController.cs:16` DoctorOrReceptionist — 权限反向 |
| H3 | `CreateMedicalCaseCommandHandler.cs:32-105` 直接 new 实体 + repository.AddAsync，未调用 MedicalCaseCommandService |

## [S2] Fix Design

### [S2.1] H1 — 删除模块 DbContext，统一使用 AppDbContext

**根因**：5 个模块 DbContext（Auth/Herbs/Formula/Users/Reports）与 AppDbContext 映射到同一张表，但模块 DbContext 缺失 HasQueryFilter（软删除过滤器），导致同表双 DbContext 读到的数据集不一致。

**修复**：

1. **删除 5 个模块 DbContext 文件**：
   - `Module.Auth/Infrastructure/AuthDbContext.cs` + `Migrations/` 目录
   - `Module.Herbs/Infrastructure/HerbsDbContext.cs`
   - `Module.Formula/Infrastructure/FormulaDbContext.cs`
   - `Module.Users/Infrastructure/UsersDbContext.cs`
   - `Module.Reports/Infrastructure/ReportsDbContext.cs`

2. **修改受影响的 Repository**：注入 IDbContextAccessor 而非模块 DbContext
   - `UserRepository.cs` — `UsersDbContext` → `IDbContextAccessor` → `AppDbContext`
   - `HerbRepository.cs` — `HerbsDbContext` → `IDbContextAccessor` → `AppDbContext`
   - `FormulaRepository.cs` — `FormulaDbContext` → `IDbContextAccessor` → `AppDbContext`
   - `SecurityAuditRepository.cs` — `AuthDbContext` → `IDbContextAccessor` → `AppDbContext`
   - `AuthSessionRepository.cs` — `AuthDbContext` → `IDbContextAccessor` → `AppDbContext`
   - `RefreshTokenRepository.cs` — `AuthDbContext` → `IDbContextAccessor` → `AppDbContext`

3. **修改模块 DI 注册**：移除模块 DbContext 注册，改用 IDbContextAccessor
   - `AuthModule.cs` — 移除 `AddDbContext<AuthDbContext>()`，Repository 改注入 IDbContextAccessor
   - `HerbsModule.cs` — 同上
   - `FormulaModule.cs` — 同上
   - `UsersModule.cs` — 同上
   - `ReportsModule.cs` — 移除 `AddDbContext<ReportsDbContext>()`（空壳，直接删除注册）

4. **保留 AppDbContext 中已有的实体配置**：确认 AppDbContext 包含所有实体的 DbSet + HasQueryFilter。当前 AppDbContext 已通过 `ApplyOptimizations()` 动态为 ISoftDeletable 实体添加过滤器，无需额外修改。

**影响范围**：5 个模块的 Infrastructure 层（Repository + DbContext + Module 注册）。不涉及 Controller、Service、DTO 层。

**验收**：`dotnet build` 通过；`dotnet test tests/LYBT.Tests.Server/` 通过；无模块 DbContext 引用残留

---

### [S2.2] H2 — 对齐 Herbs/Formulas 授权策略 + Controller 合并

**根因**：WebAPI Herbs/FormulasController 用 `AdminOrSuperAdmin` 策略（仅管理员可操作药材/验方），LocalWebAPI 用 `DoctorOrReceptionist`（医生/前台可操作）。Local 端权限比 Remote 端更宽，产生安全策略漂移。

**修复分两步**：

**Step 1：对齐授权策略**

将 WebAPI 的 Herbs/Formulas 授权策略从 `AdminOrSuperAdmin` 改为 `DoctorOrReceptionist`。理由：
- 本地模式是单用户桌面端，Doctor 需要管理药材/验方才能开方
- Remote 模式下 Doctor 也需要药材管理能力（查询、引用验方）
- 当前 Remote 端的限制实际上阻碍了医生的正常工作流

**Step 2：Controller 共享基类下沉（6 对可合并的）**

可安全合并的 6 对 Controller（核心逻辑相同，仅 HTTP 层装饰不同）：
- PatientsController
- RegistrationsController
- ReportsController
- ConfigurationController
- HealthController
- MedicalCases 查询端点

合并方式：将共享逻辑下沉到 `BaseApiController` 的泛型版本或创建 `SharedController<TService>` 基类，WebAPI/LocalWebAPI Controller 继承基类仅添加路由和授权装饰。

MedicalCases 写端点因端点漂移差异大（LocalWebAPI 多 by-status/pending 端点），暂不合并。

**影响范围**：Herbs/Formulas Controller 授权策略 + 6 对 Controller 基类重构

**验收**：`dotnet build` 通过；Herbs/Formulas 在 WebAPI 和 LocalWebAPI 端授权策略一致

---

### [S2.3] H3 — CreateMedicalCaseCommandHandler 委托 Service 层

**根因**：`CreateMedicalCaseCommandHandler.cs:32-105` 直接 new 实体 + `repository.AddAsync()`，绕过 `MedicalCaseCommandService` 的验证逻辑，跳过了 BR-001（单患者单活动医案）和患者禁用检查。

**修复**：将 CreateMedicalCaseCommandHandler 改为委托 `IMedicalCaseCommandService.SaveAsync`（或新建 `CreateFromInputAsync` 方法），恢复 BR-001 验证。删除 Handler 中 100 行直接实体创建代码。

**影响范围**：仅 `CreateMedicalCaseCommandHandler.cs` 一个文件

**验收**：`dotnet build` 通过；BR-001 验证恢复（创建医案时检查单患者单活动医案规则）

## [S3] Out of Scope

- Controller 合并的详细基类设计 → 留到实施时根据实际代码结构决定
- MVVM 双栈统一 → Phase 3
- 数据入口统一（VM 直调 IApiClient）→ Phase 3
- 架构测试补全 → Phase 3

## [S4] Tasks

- [ ] T1: H1 删除 5 个模块 DbContext + 迁移 Repository 注入 + 修改模块 DI 注册 (covers: S2.1)
- [ ] T2: H2 Step1 对齐 Herbs/Formulas WebAPI 授权策略 (covers: S2.2)
- [ ] T3: H2 Step2 Controller 共享基类下沉（6 对合并） (covers: S2.2)
- [ ] T4: H3 CreateMedicalCaseCommandHandler 委托 Service 层 (covers: S2.3)
- [ ] T5: 全量验证 — build + architecture tests + server tests (covers: S4)
