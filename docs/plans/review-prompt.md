# Cross-AI Plan Review Request

You are reviewing implementation plans for a software project phase.
Provide structured feedback on plan quality, completeness, and risks.

## Project Context
LYBTZYZS - TCM Clinic Management System. .NET 8 WPF/Prism + ASP.NET Core + EF Core.
Dual-mode architecture: Remote (SQL Server) vs Local (SQLite embedded in WPF process).
LocalWebAPI: Embedded Kestrel in WPF, direct DbContext (no service layer), simplified JWT.

## Plan to Review
# Local Mode Feature Coverage Improvement Plan

## Executive Summary

**Scope**: Increase LocalWebAPI feature coverage from ~31% to ~85% across 6 modules (MedicalCase, Patients, Herbs, Formulas, Users, Auth) by adding ~50 missing endpoints and replacing ~40+ Http*Repository stubs with real HTTP calls.

**Approach**: 5-phase rollout prioritized by user impact (workflow-critical → batch ops → security → tail features). Each phase pairs server-side controller endpoints with client-side Http*Repository implementations.

**Constraints honored**:
- TBD-01 exclusions: no token refresh, no audit log queries, no user sync, no auto-login
- LocalWebAPI architecture: direct `LocalWebApiDbContext`, no service layer, no DTOs
- Soft-delete via `IsDeleted = true`
- Excel import/export handled on client side — server provides JSON API only
- Reference checks use inline DbContext queries
- Permissions return permissive results for offline single-user scenario

## Design Decisions (User-Confirmed)

1. **引用检查**: 内联查询检查 — Use inline DbContext queries for reference checks before delete
2. **Excel 导入导出**: Server provides JSON API, client handles Excel conversion. Both Remote and Local modes follow this approach.
3. **权限**: 返回宽松权限 — Return permissive permissions (CanEdit=true, CanClose=true, etc.) for offline single-user scenario

---

## Phase 1 (P0 — Critical): MedicalCase Workflow

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T1.1 | MedicalCase lifecycle endpoints (Close/Suspend/Cancel) | None |
| T1.2 | MedicalCase query/search endpoints (Search/Query/BatchDetails/Permissions/GetByStatus) | None |
| T1.3 | MedicalCase status/print/batch-delete endpoints | T1.1 |
| T1.4 | HttpMedicalCaseRepository — implement 12 stubs | T1.1, T1.2, T1.3 |
| T1.5 | MedicalCase E2E integration tests | T1.4 |

### Endpoints to Add (8 new on MedicalCasesController)

```
POST   /api/medicalcases/{id}/close           → CloseCase
POST   /api/medicalcases/{id}/suspend          → Suspend
POST   /api/medicalcases/{id}/cancel           → Cancel (soft-delete + registration rollback)
GET    /api/medicalcases/search                → Search (patientName, diagnosisKeyword, date range, pagination)
POST   /api/medicalcases/query                 → Query (MedicalCaseQueryDto)
POST   /api/medicalcases/batch-details         → GetBatchDetails (List<Guid> ids)
GET    /api/medicalcases/{id}/permissions       → GetPermissions (permissive for offline)
PUT    /api/medicalcases/{id}/prescription-flag → SetPrescriptionFlag
PUT    /api/medicalcases/{id}/status            → UpdateStatus
POST   /api/medicalcases/{id}/print-completed   → RecordPrintCompleted
POST   /api/medicalcases/save                   → SaveAsync (upsert)
DELETE /api/medicalcases/batch                  → BatchDelete
```

### HttpMedicalCaseRepository Methods to Implement (12)

```
SearchAsync, QueryAsync, CloseCaseAsync, GetPermissionsAsync, SaveAsync,
GetBatchDetailsAsync, SetPrescriptionFlagAsync, UpdateStatusAsync,
CancelMedicalCaseAsync, SuspendAsync, RecordPrintCompletedAsync, BatchDeleteAsync
```

---

## Phase 2 (P1 — High): Patients / Herbs / Formulas Batch Operations

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T2.1 | Patients controller (BatchDelete/Restore + JSON export/import API) | None |
| T2.2 | Herbs controller (BatchDelete/BatchEnable/BatchDisable/Toggle/Restore + JSON export/import API) | None |
| T2.3 | Formulas controller (BatchDelete/Restore + JSON export/import API) | None |
| T2.4 | HttpPatientRepository implementation | T2.1 |
| T2.5 | HttpHerbRepository implementation | T2.2 |
| T2.6 | HttpFormulaRepository implementation | T2.3 |
| T2.7 | Phase 2 integration tests | T2.4, T2.5, T2.6 |

**Note**: Excel conversion happens client-side. Server endpoints return/accept JSON data only.

---

## Phase 3 (P2 — Medium): Auth Security

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T3.1 | UsersController.ChangePassword + Lock/Unlock + Roles | None |
| T3.2 | HttpAuthRepository / HttpUserRepository implementation | T3.1 |
| T3.3 | Phase 3 integration tests | T3.2 |

**TBD-01 excluded**: RefreshToken, RevokeToken, AutoLogin → return 501

---

## Phase 4 (P3 — Low): Diagnostics / Configuration / Tail Coverage

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T4.1 | DiagnosticsController (local) | None |
| T4.2 | ConfigurationController (local) | None |
| T4.3 | HealthController extensions | None |
| T4.4 | Advanced query endpoints | T1.2 |
| T4.5 | Phase 4 integration tests | T4.1-T4.4 |

---

## Phase 5 (Wrap-up): Documentation & Architecture Guards

| Task | Description | Depends On |
|------|-------------|------------|
| T5.1 | Update dual-mode.md coverage table | All phases |
| T5.2 | Architecture test for LocalWebAPI pattern compliance | All phases |

---

## Success Criteria

1. **Coverage**: ≥85% endpoint parity (≥80/102 endpoints)
2. **Tests**: ≥40 new integration tests, all green
3. **Zero stubs**: No `LogWarning("not supported")` except TBD-01 excluded methods
4. **Architecture**: All new architecture tests pass
5. **Docs**: `docs/03-architecture/dual-mode.md` coverage table updated


## Feature Comparison (Before Implementation)
# 远程模式 vs 本地模式 功能对比清单

**文档版本**: v1.0  
**生成日期**: 2026-05-01  
**数据来源**: 代码库实际 API 端点统计 + 架构文档分析

---

## 一、总览

| 指标 | 远程模式 (Remote) | 本地模式 (Local) | 本地覆盖率 |
|------|-------------------|------------------|------------|
| **控制器数量** | 14 | 8 | 57% |
| **API 端点总数** | ~102 | 32 | **~31%** |
| **数据库** | SQL Server | SQL Server (文档标注 SQLite，代码实际用 SQL Server) | — |
| **认证方式** | JWT + RefreshToken | JWT (仅登录) | — |
| **同步模块** | ✅ | ❌ | 0% |
| **离线支持** | ❌ | ✅ (设计目标) | — |

---

## 二、按模块端点对比

| 模块 | 远程端点 | 本地端点 | 覆盖率 | 关键缺失 |
|------|----------|----------|--------|----------|
| **Auth** | 5 | 1 | 20% | 刷新令牌、撤销、密码修改/重置、自动登录、账户锁定 |
| **Users** | 14 | 5 | 36% | 批量删除、批量启用/禁用、修改密码 |
| **Patients** | 11 | 5 | 45% | 批量操作、Excel/JSON 导入导出、高级搜索 |
| **Herbs** | 17 | 5 | 29% | 批量操作、导入导出、Record-Only 标记、引用检查 |
| **Formulas** | 15 | 5 | 33% | 批量操作、导入导出、模板管理 |
| **MedicalCases** | 19 | 5 | 26% | 完整工作流（关闭/暂停/取消）、状态查询、权限管理、审计日志、处方标记 |
| **Registrations** | 7 | 5 | 71% | 快速就诊、批量操作 |
| **Sync** | 6 | 0 | **0%** | 完全缺失 |
| **Diagnostics** | 4 | 0 | **0%** | 完全缺失 |
| **Configuration** | ≥1 | 0 | **0%** | 完全缺失 |
| **Health** | 3 | 1 | 33% | 详细诊断信息 |

---

## 三、逐模块详细对比

### 3.1 Auth 认证模块

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 用户名密码登录 | ✅ | ✅ | 本地仅支持用户名密码 |
| 刷新令牌 | ✅ | ❌ | 本地无 RefreshToken 机制 |
| 撤销令牌 | ✅ | ❌ | 本地无 TokenRevocationService |
| 密码修改 | ✅ | ❌ | |
| 密码重置 | ✅ | ❌ | |
| 自动登录 | ✅ | ❌ | 本地无 RememberMe 令牌持久化 |
| 账户锁定 | ✅ | ❌ | 本地无失败计数锁定 |
| BCrypt 验证 | ✅ | ❌ | 本地用明文比较 |
| 安全审计日志 | ✅ | ❌ | |

### 3.2 Users 用户管理

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取所有用户 | ✅ | ✅ | |
| 按角色筛选 | ✅ | ❌ | |
| 创建用户 | ✅ | ✅ | |
| 更新用户 | ✅ | ✅ | |
| 删除用户 | ✅ | ✅ | |
| 批量删除 | ✅ | ❌ | |
| 启用/禁用 | ✅ | ❌ | |
| 批量启用/禁用 | ✅ | ❌ | |
| 修改密码 | ✅ | ❌ | |
| 重置密码 | ✅ | ❌ | |
| 搜索用户 | ✅ | ❌ | |
| 用户统计 | ✅ | ❌ | |
| 导入用户 | ✅ | ❌ | |
| 导出用户 | ✅ | ❌ | |

### 3.3 Patients 患者管理

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取所有患者 | ✅ | ✅ | |
| 分页查询 | ✅ | ✅ | |
| 按 ID 查询 | ✅ | ✅ | |
| 创建患者 | ✅ | ✅ | |
| 更新患者 | ✅ | ✅ | |
| 删除患者 | ✅ | ❌ | |
| 搜索患者 | ✅ | ❌ | |
| 高级搜索 | ✅ | ❌ | |
| 批量删除 | ✅ | ❌ | |
| Excel 导入 | ✅ | ❌ | |
| Excel 导出 | ✅ | ❌ | |
| JSON 导入 | ✅ | ❌ | |
| JSON 导出 | ✅ | ❌ | |

### 3.4 Herbs 药材管理

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取所有药材 | ✅ | ✅ | |
| 分页查询 | ✅ | ✅ | |
| 按 ID 查询 | ✅ | ❌ | |
| 创建药材 | ✅ | ✅ | |
| 更新药材 | ✅ | ✅ | |
| 删除药材 | ✅ | ❌ | |
| 搜索药材 | ✅ | ✅ | |
| 批量操作 | ✅ | ❌ | |
| 批量导入 | ✅ | ❌ | |
| Record-Only 标记 | ✅ | ❌ | |
| 引用检查 | ✅ | ❌ | |
| Excel 导入 | ✅ | ❌ | |
| Excel 导出 | ✅ | ❌ | |
| JSON 导入 | ✅ | ❌ | |
| JSON 导出 | ✅ | ❌ | |
| 模板管理 | ✅ | ❌ | |
| 分类管理 | ✅ | ❌ | |

### 3.5 Formulas 验方管理

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取所有验方 | ✅ | ✅ | |
| 分页查询 | ✅ | ✅ | |
| 按 ID 查询 | ✅ | ❌ | |
| 创建验方 | ✅ | ✅ | |
| 更新验方 | ✅ | ✅ | |
| 删除验方 | ✅ | ❌ | |
| 搜索验方 | ✅ | ✅ | |
| 批量操作 | ✅ | ❌ | |
| 模板管理 | ✅ | ❌ | |
| 导入导出 | ✅ | ❌ | |
| 共享/个人切换 | ✅ | ❌ | |

### 3.6 MedicalCases 医案管理

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取所有医案 | ✅ | ✅ | |
| 分页查询 | ✅ | ✅ | |
| 按 ID 查询 (含导航属性) | ✅ | ✅ | |
| 创建医案 | ✅ | ✅ | |
| 更新医案 | ✅ | ✅ | |
| 删除医案 | ✅ | ❌ | |
| 关闭医案 | ✅ | ❌ | |
| 暂停医案 | ✅ | ❌ | |
| 取消医案 | ✅ | ❌ | |
| 状态查询 | ✅ | ❌ | |
| 搜索医案 | ✅ | ❌ | |
| 高级查询 | ✅ | ❌ | |
| 权限管理 | ✅ | ❌ | |
| 审计日志 | ✅ | ❌ | |
| 处方标记 | ✅ | ❌ | |
| 处方管理 | ✅ | ❌ | |
| 打印 | ✅ | ❌ | |
| 处理记录 | ✅ | ❌ | |
| 批量操作 | ✅ | ❌ | |

### 3.7 Registrations 挂号管理

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取所有挂号 | ✅ | ✅ | |
| 分页查询 | ✅ | ✅ | |
| 创建挂号 | ✅ | ✅ | |
| 更新挂号 | ✅ | ✅ | |
| 删除挂号 | ✅ | ❌ | |
| 日期筛选 | ✅ | ✅ | |
| 快速就诊 | ✅ | ❌ | |
| 批量操作 | ✅ | ❌ | |

### 3.8 Sync 同步模块

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 同步状态查询 | ✅ | ❌ | |
| 数据上传 | ✅ | ❌ | |
| 数据下载 | ✅ | ❌ | |
| 冲突解决 | ✅ | ❌ | |
| 同步历史 | ✅ | ❌ | |
| 手动触发同步 | ✅ | ❌ | |

### 3.9 Diagnostics 诊断模块

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 数据库连接测试 | ✅ | ❌ | |
| 系统信息查询 | ✅ | ❌ | |
| 性能指标 | ✅ | ❌ | |
| 健康详情 | ✅ | ❌ | |

### 3.10 Configuration 配置模块

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 获取系统配置 | ✅ | ❌ | |
| 更新系统配置 | ✅ | ❌ | |

### 3.11 Health 健康检查

| 功能 | 远程 | 本地 | 说明 |
|------|------|------|------|
| 基本健康检查 | ✅ | ✅ | |
| 详细健康检查 | ✅ | ❌ | |
| 数据库健康检查 | ✅ | ❌ | |

---

## 四、基础设施差异

| 特性 | 远程模式 | 本地模式 |
|------|----------|----------|
| **Web 服务器** | Kestrel (WebAPI) | Kestrel (LocalWebAPI) |
| **端口** | 配置决定 | `LocalWebApi:Port`，默认 5290 |
| **数据库** | SQL Server | SQL Server (代码实际)，文档标注 SQLite |
| **ORM** | EF Core 8 | EF Core 8 |
| **认证** | JWT + RefreshToken | 仅 JWT 登录 |
| **Repository 模式** | Refit (HTTP 客户端) | Http*Repository (直接 HTTP 调用) |
| **DI 注册** | DataSourceRegistrationExtensions | 同左，根据 ConnectionMode 切换 |
| **菜单可见性** | 全部模块 | MenuManager 限制：用户管理/同步/密码修改不可见 |
| **控制器数量** | 14 | 8 |
| **处理控制器** | MedicalCaseProcessingController | 无 |
| **审计控制器** | MedicalCaseAuditController | 无 |
| **打印控制器** | MedicalCasePrintController | 无 |

---

## 五、UI 层差异

| 特性 | 远程模式 | 本地模式 |
|------|----------|----------|
| **用户管理入口** | ✅ 可见 | ❌ MenuManager 隐藏 |
| **同步入口** | ✅ 可见 | ❌ MenuManager 隐藏 |
| **密码修改入口** | ✅ 可见 | ❌ MenuManager 隐藏 |
| **导入导出按钮** | ✅ 全部可用 | ❌ 功能降级 |
| **批量操作按钮** | ✅ 全部可用 | ❌ 功能降级 |
| **高级搜索** | ✅ 全部可用 | ❌ 功能降级 |

---

## 六、数据流差异

### 远程模式数据流
```
WPF Client → Refit HTTP Client → Remote WebAPI → Service → Repository → SQL Server
```

### 本地模式数据流
```
WPF Client → Http*Repository → LocalWebAPI → Service → Repository → SQL Server
```

### 切换机制
- `IConnectionModeProvider.CurrentMode` 返回 `Remote` 或 `Local`
- `DataSourceRegistrationExtensions.cs` 根据模式注册对应的 Repository 工厂
- 运行时切换，无需重启应用

---

## 七、已知问题

1. **DB 提供程序不一致**: `LocalWebApiProgram.cs` 使用 `UseSqlServer()`，但架构文档标注本地模式应使用 SQLite
2. **功能降级未充分处理**: HttpRepository 返回 null 并记录 Warning 日志，UI 层可能未正确处理
3. **菜单隐藏不完整**: MenuManager 隐藏了部分入口，但直接访问路由仍可进入
4. **同步模块完全缺失**: 本地模式无任何同步能力，数据无法与远程同步

---

## 八、建议优先级

### P0 - 必须实现
1. MedicalCases 工作流（关闭/暂停/取消）
2. 批量操作（删除/启用/禁用）
3. Auth 安全（密码修改/重置、账户锁定）

### P1 - 应该实现
4. 导入导出功能
5. 同步模块基础功能
6. 修复 DB 提供程序不一致

### P2 - 可以延后
7. 高级搜索
8. 诊断模块
9. 配置管理
10. 审计日志


## Review Instructions

Analyze the plan and provide:

1. **Summary** — One-paragraph assessment
2. **Strengths** — What's well-designed (bullet points)
3. **Concerns** — Potential issues, gaps, risks (bullet points with severity: HIGH/MEDIUM/LOW)
4. **Suggestions** — Specific improvements (bullet points)
5. **Risk Assessment** — Overall risk level (LOW/MEDIUM/HIGH) with justification

Focus on:
- Missing edge cases or error handling
- Dependency ordering issues
- Scope creep or over-engineering
- Security considerations
- Performance implications
- Whether the plans actually achieve the phase goals

Output your review in markdown format.
