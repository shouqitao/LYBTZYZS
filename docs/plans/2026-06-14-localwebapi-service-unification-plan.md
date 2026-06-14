# LocalWebAPI 统一 Service 层 — 实施计划

> 日期: 2026-06-14
> 目标: 消除 LocalWebAPI 与 Remote WebAPI 的并行实现，统一到 Service 层

---

## 现状

```
远程: Controller → Service → Repository → DbContext (完整业务逻辑)
本地: Controller → DbContext (直接操作, 业务逻辑缺失/不一致)
```

## 目标

```
远程: Controller → Service → Repository → DbContext
本地: Controller → Service → Repository → DbContext  (相同 Service 接口)
```

---

## 阶段 1: 基础设施准备

### 1.1 项目引用
- `LYBT.LocalWebAPI.csproj` 添加 8 个 Server 模块引用:
  - `LYBT.Module.Auth`, `LYBT.Module.Users`, `LYBT.Module.Patients`
  - `LYBT.Module.Herbs`, `LYBT.Module.Formula`, `LYBT.Module.MedicalCase`
  - `LYBT.Module.Registration`, `LYBT.Module.Sync`
- 验证编译通过（预期有命名空间冲突需解决）

### 1.2 DI 注册
- `LocalWebApiProgram.cs` 中将各模块的 `AddXxxModule()` 注册替换当前的手动 DbContext 注册
- 保留 `LocalWebApiDbContext` 注册（模块 Service/Repository 依赖它）
- 确保模块的 DI 扩展方法不依赖远程特有配置（如 SecurityOptions）

### 1.3 架构测试更新
- `LYBT.Tests.Architecture/` 中更新跨层引用规则:
  - LocalWebAPI 项目豁免"Desktop 不能引用 Server 模块"约束
  - 添加新规则: LocalWebAPI 控制器必须注入 Service 接口（非 DbContext）

---

## 阶段 2: 控制器重写（按复杂度排序）

### 2.1 简单控制器（直接 CRUD，无复杂业务逻辑）

**顺序**: HealthController → ConfigurationController → DiagnosticsController

每个控制器:
1. 将 `LocalWebApiDbContext _db` 替换为 `I{Entity}Service _service`
2. 将 `_db.{Entity}.ToListAsync()` 替换为 `_service.GetListAsync()`
3. 将 `_db.{Entity}.Add()` 替换为 `_service.CreateAsync()`
4. 删除手动 JWT Claims 检查（Service 层统一处理权限）
5. 验证编译 + 基本功能

### 2.2 中等控制器（含导入导出/批量操作）

**顺序**: HerbsController → FormulasController → UsersController → PatientsController

每个控制器:
1. 注入对应的 Service + ImportExportService（如适用）
2. 替换直接 DbContext 操作为 Service 调用
3. 批量操作使用 Service 层的 BatchDeleteAsync/BatchUpdateStatusAsync
4. 删除手动权限检查（IsAdminOrHigher/IsDoctorOrHigher 等临时方案）
5. 验证编译 + 功能

### 2.3 复杂控制器（MedicalCasesController — 22 端点）

**特殊处理**: 当前本地 MedicalCasesController 合并了远程 4 个控制器的功能

**方案**: 拆分为 4 个控制器，与远程对齐:
- `MedicalCasesController` — CRUD + 查询 (12 端点)
- `MedicalCaseProcessingController` — 状态转换: close/suspend/cancel/status (4 端点)
- `MedicalCasePrintController` — print-completed/print-logs (2 端点)
- `MedicalCaseAuditController` — audit-logs (2 端点)
- 保留本地特有端点: by-status, pending, consultations, prescriptions, batch-details, permissions

每个注入对应的 Service:
- `IMedicalCaseCommandService` (写操作)
- `IMedicalCaseQueryService` (读操作)
- `IMedicalCaseStateService` (状态转换)
- `IMedicalCasePrintService` (打印回写)
- `IMedicalCaseAuditService` (审计查询)
- `IMedicalCasePermissionService` (权限检查)

### 2.4 认证控制器（AuthController）

**特殊处理**: 本地认证逻辑与远程不同（简化 JWT）

**方案**: 保留本地 AuthController 特有逻辑，但注入 `IAuthService` + `IAutoLoginService`:
- Login: 使用 `AuthService.VerifyCredentialsInternalAsync` + `LocalJwtConfig.GenerateToken`
- AutoLogin: 使用 `IAutoLoginService.ValidateTokenAsync` + 本地 JWT 生成
- Refresh: 使用本地 JWT 逻辑（远程 RefreshToken 不适用）
- Validate: 使用 `IAuthService` 验证用户状态
- Logout: 保留本地无状态逻辑

### 2.5 挂号控制器（RegistrationsController）

- 注入 `IRegistrationService` + `IMedicalCaseCommandService`
- QuickVisit: 使用远程的 TransactionScope 编排模式
- StartVisit: 保留本地特有端点
- 其他 CRUD: 委托 Service 层

---

## 阶段 3: 清理

### 3.1 删除临时安全补丁
- 移除 LocalWebAPI 控制器中的 `IsAdminOrHigher()`, `IsDoctorOrHigher()`, `GetCurrentUserId()` 辅助方法
- 移除 FormulasController 的手动所有权检查
- 移除 MedicalCasesController 的手动角色检查
- 移除 UsersController 的手动 Admin 检查
- 这些全部由 Service 层的统一权限模型替代

### 3.2 删除 LocalWebApiDbContext 直接引用
- 10 个控制器中移除 `private readonly LocalWebApiDbContext _db`
- 保留 LocalWebApiDbContext 本身（模块 Repository 层依赖它）

### 3.3 对齐端点
- 检查本地特有端点（如 GetPatientByIdNumber, GetHerbsCategories）是否应保留
- 确认无远程端点在本地缺失（除 SyncController 正确缺失外）

---

## 阶段 4: 验证

### 4.1 编译验证
- `dotnet build LYBTZYZS.sln` — 0 错误

### 4.2 测试验证
- `dotnet test tests/LYBT.Tests.Server/` — 远程功能不受影响
- `dotnet test tests/LYBT.Tests.Desktop/` — 本地端点测试通过
- `dotnet test tests/LYBT.Tests.Architecture/` — 架构规则通过

### 4.3 功能验证
- 启动 WebAPI + Desktop 本地模式
- 验证核心流程: 登录 → 创建患者 → 创建医案 → 诊断 → 处方 → 完成 → 打印
- 验证权限: Receptionist 不能创建医案, Doctor 不能管理用户

---

## 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| 模块 DI 依赖远程配置 (SecurityOptions 等) | 编译/运行失败 | 在 LocalWebApiProgram 中注册所需 Options |
| Service 层假设远程 HttpContext | 运行时 NullRef | 检查 Service 中的 HttpContext 使用，提供本地替代 |
| LocalWebApiDbContext 与 AppDbContext Schema 差异 | EF Core 映射错误 | 验证两 DbContext 实体配置一致 |
| 命名空间冲突 | 编译错误 | 使用别名或调整 using |
| 架构测试失败 | CI 阻断 | 阶段 1.3 更新架构规则 |

---

## 工作量估算

| 阶段 | 控制器数 | 预估复杂度 |
|------|----------|-----------|
| 1. 基础设施 | 0 | 中（DI + 架构测试） |
| 2.1 简单控制器 | 3 | 低 |
| 2.2 中等控制器 | 4 | 中 |
| 2.3 MedicalCase | 1→4 | 高（拆分 + 6 个 Service 接口） |
| 2.4 Auth | 1 | 中（混合本地+远程逻辑） |
| 2.5 Registration | 1 | 中 |
| 3. 清理 | 全部 | 低 |
| 4. 验证 | — | 中 |
