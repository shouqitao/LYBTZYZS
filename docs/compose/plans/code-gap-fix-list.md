# 代码偏移修复清单（Documentation Calibration + 审稿产出）

> 来源：2026-08-02 文档校准 + 审稿任务 + 2026-08-04 doc-audit（G-01）
> 状态：🟡 批次 A 已完成（2026-08-03，commit `165f1b08f` + LocalWebAPI 补齐 T2 中）；批次 B 待执行；批次 C 待派发（2026-08-04 审计产出）
> 原则：先批 A（纯策略补丁），再批 B（业务逻辑），再批 C（审计缺口，P0 优先）
> 术语：Admin = 管理员（业务管理），Sysadmin = 超级管理员（系统运维，不碰业务）

---

## 批次 A：纯策略补丁（⚡ 改动极小，可一次过）—— ✅ 2026-08-03 完成

> 全部 A 项已落地：Server 端 5 控制器 + LocalWebAPI 端（commit `165f1b08f`）；LocalWebAPI Patients/MedicalCases 补齐由 T2 跟进。

### A1. PatientsController.Delete 补策略 ✅
- **问题**：Delete 无操作级 `[Authorize]`，回退类级 `DoctorOrAdminOrReceptionist`，Doctor/Receptionist 也可删患者
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:129`
- **修复**：Delete 方法补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`
- **验证**：`dotnet build` + 架构测试

### A2. HerbsController Create/Update 改策略 ✅
- **问题**：Create/Update 无操作级 `[Authorize]`，回退类级 `DoctorOrReceptionist`，Doctor 也可创建/编辑药材
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:73,97`
- **修复**：Create 和 Update 方法各补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`
- **验证**：`dotnet build` + 架构测试

### A3. FormulasController Create/Update 补策略（P0-3）✅
- **问题**：Create/Update 用类级 `DoctorOrReceptionist`，前台也可写验方
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- **修复**：Create/Update 补 `[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]`；GET 拆前台见 A6
- **验证**：`dotnet build` + 架构测试

### A4. MedicalCasesController.Create 改策略（C4）✅
- **问题**：Create 策略含 Receptionist/Admin，但 BR-000 决策仅 Doctor 可建
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs:93`
- **修复**：Create 策略改为 `DoctorOnly`（依赖 A5 新增策略常量）
- **验证**：`dotnet build` + 架构测试

### A5. PolicyConstants 补 DoctorOnly（K3）✅
- **问题**：医案创建目标策略 `DoctorOnly` 不存在
- **文件**：`src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs`
- **修复**：新增 `public const string DoctorOnly = "DoctorOnly";` + 注册策略
- **验证**：`dotnet build`

### A6. Herbs/Formulas GET 拆前台（C5，2026-08-03 决策）✅
- **问题**：`HerbsController`/`FormulasController` GET 类级 `DoctorOrReceptionist`，前台可查看药材/验方
- **文件**：`HerbsController.cs` / `FormulasController.cs`
- **修复**：GET 策略改为 Doctor+Admin+SuperAdmin（不含 Receptionist），需新增策略（如 `DoctorOrAdminOrSuperAdmin`）或操作级覆盖
- **验证**：`dotnet build` + 角色权限测试

### A7. 打印补 DoctorOnly（C6/P1-6，2026-08-03 决策）✅
- **问题**：处方打印端点无操作级策略，管理员/前台可打印
- **文件**：打印模块 Controller（Desktop 打印调用方）
- **修复**：打印操作补 `[Authorize(Policy = PolicyConstants.DoctorOnly)]`（依赖 A5）；管理员仅可查看打印记录
- **验证**：`dotnet build` + 角色权限测试

---

## 批次 B：业务逻辑修复（🔧 需设计确认）

### B1. StartVisit 接诊即建（D8 修复）
- **问题**：StartVisit 当前不创建医案 + 返回 RegistrationId 冒充 MedicalCaseId → Desktop 导航到空医案（D8 bug 确认存在）
- **决策**：2026-08-03 产品负责人确认「**接诊即建**」——StartVisit/QuickVisit/本地选患者开始看诊时原子创建 MedicalCase(Active) + Registration(InProgress)。原 B1「不再建医案」基于旧 BR-000（2026-08-02），已随决策修订作废
- **文件**：`src/Server/Modules/LYBT.Module.Registration/Application/Commands/StartVisitCommandHandler.cs`、`RegistrationsController.cs`
- **修复**：StartVisit 改为原子事务：`Registration.Status=InProgress` + 创建 `MedicalCase(Active)` 关联 `RegistrationId` + 返回 `MedicalCaseId`；复用 BR-001 单活跃医案约束，碰撞时提示「重开现有医案」
- **参考**：BR-000（2026-08-03 修订）、R10 spec S5、08-registration.md US-REG-005
- **验证**：集成测试 + Desktop 端接诊流程

### B2. 挂号操作级权限细分（K9/C3/P1-4 合并，2026-08-03 决策）
- **问题**：Cancel 无操作级策略（前台被挡、Doctor/Admin 反被放行）；Create/start-visit/quick-visit 均回退类级 `DoctorOrAdminOrReceptionist`（Admin 可创/取/接诊）
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs`
- **修复**：按操作级细分——GET：Doctor+Receptionist+Admin 只读；POST：Receptionist；quick-visit/start-visit：`DoctorOnly`；cancel：`Receptionist` + 服务层校验 Source=Receptionist
- **验证**：`dotnet build` + 角色权限测试

### B3. LocalWebAPI 权限策略（K8）
- **问题**：LocalWebAPI Controllers 仅 `[Authorize]` 无 Policy，本地 Doctor 可做所有操作
- **文件**：`src/Server/LocalWebAPI/Controllers/*.cs`
- **修复**：明确本地是否启用角色策略（建议与远程一致）
- **验证**：本地模式全角色测试

### B4. 生产环境 sysadmin 安全（K4）
- **问题**：IdentitySeedData 不读生产配置，直接用明文密码创建 sysadmin
- **文件**：`src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs`
- **修复**：注入 `IDefaultPasswordService` + `IHostEnvironment`，复用 `GetOrGeneratePassword`/`ValidateSetupToken`
- **验证**：生产环境配置测试

### B5. 医生挂号费字段（2026-08-03 决策）
- **问题**：REG-BR-009「挂号费跟医生相关」无载体（`ApplicationUser` 无 RegistrationFee）；QuickVisit/本地自动建挂号恒 0，报表挂号费失真
- **文件**：`ApplicationUser` 实体 + Migration；UsersController/Admin 用户管理 UI；前台挂号表单；QuickVisitCommandHandler；本地模式建挂号
- **修复**：`ApplicationUser` 加 `RegistrationFee`（decimal(10,2)，默认 0）；Admin 创建/编辑医生时设置；前台创建挂号自动带出（可改，义诊/优惠）；QuickVisit/本地自动带出
- **验证**：集成测试（挂号带出）+ 报表测试（RegistrationFeeTotal 覆盖 QuickVisit/本地）

### B6. 医案取消 = 物理删除（2026-08-03 决策）
- **问题**：`CancelAsync` 当前软删除（IsDeleted=true）；新决策取消=物理删除（不判内容，级联清聚合，审计记录 Cancel）
- **文件**：`MedicalCaseStateService.CancelAsync`、`MedicalCaseServiceHelper`、Desktop 取消确认弹窗
- **修复**：CancelAsync 改为物理删除（EF 级联删除聚合）；前端强确认「将永久删除，不可恢复」；审计 OperationType=Cancel；已完成医案不可取消（只可软删）
- **验证**：集成测试（取消后无残留 + Registration 联动）

### B7. 未完成医案不可打印（2026-08-03 决策）
- **问题**：当前允许未完成打印（草稿水印）；新决策仅 Completed 可打印
- **文件**：Desktop 打印入口（MedicalCaseCommandsViewModel）、PrescriptionPrintHandler
- **修复**：打印前校验 CaseStatus=Completed；删除草稿水印逻辑
- **验证**：Desktop 集成测试

### B8. 打印保护简化（2026-08-03 决策）
- **问题**：IsPrinted 作为操作限制触发器（打印后禁止取消/删除、修改需 EditReason）；新设计降级为打印状态标记（PrintVersion++ 重打）
- **文件**：`MedicalCaseStateService`、`MedicalCaseBusinessRules`、US-MC-014/015 相关校验
- **修复**：IsPrinted 仅用于标记/版本追踪；删除「打印后修改需 EditReason」强制校验（隔天由 IsLocked 覆盖）
- **验证**：状态机测试

### B9. 状态机绕过漏洞（P4）
- **问题**：`UpdateMedicalCaseStatusCommandHandler` 直接 `CaseStatus = request.Status`，绕过 StateService 校验（可设 Completed）
- **文件**：`UpdateMedicalCaseStatusCommandHandler.cs`、`MedicalCasesController PUT /{id}/status`
- **修复**：Handler 委托 `IMedicalCaseStateService.UpdateStatusAsync`（或删除 handler，统一走 StateService）
- **验证**：状态机测试（UpdateStatus 仅允许 Suspended↔Active）

---

## 批次 C：doc-audit 代码缺口（2026-08-04，G-01 审计产出）—— ✅ 已完成
> 来源：2026-08-04 doc-audit（3 并行子代理审稿 + 主代理代码校准）。
> 完成：C1-C3 → commit `f50269f23`；C4-C6 → commit `ce905f8b3`。Build 0 错误、架构测试通过。

### C1. 患者 Restore 补操作级权限（P0 安全漏洞）
- **问题**：`POST /patients/{id}/restore` 无操作级 `[Authorize]`，回退类级 `DoctorOrAdminOrReceptionist` → **前台/医生都能恢复患者**。裁决：恢复患者 = 仅 Admin（业务管理）
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:181` + LocalWebAPI 对应
- **修复**：Restore 方法补 `[Authorize(Policy = PolicyConstants.AdminOnly)]`（需 C2 新增纯 Admin 策略后改用）
- **验证**：集成测试（Receptionist/Doctor 调 restore 403）

### C2. 新增纯 Admin 策略（P0）
- **问题**：现有 `AdminOnly` 注册为 `RequireRole(SuperAdmin, Admin)` 含 sysadmin；「恢复业务数据=仅 Admin」无策略可执行。sysadmin 不碰业务
- **文件**：`PolicyConstants.cs` + `AuthenticationServiceCollectionExtensions.cs:111` + `LocalJwtConfig.cs:69`
- **修复**：新增 `PolicyConstants.AdminBusinessOnly = "AdminBusinessOnly"`，注册 `RequireRole(Admin)`（不含 SuperAdmin）；C1 及患者删除/禁用如需纯 Admin 时使用
- **验证**：架构测试 + 集成测试（sysadmin 调业务恢复 403）

### C3. 患者单删补引用检查（P0，D5）
- **问题**：`DeletePatientCommandHandler.cs:34` 直接 `SoftDelete`，未查 MedicalCase 引用 → 被引用的患者仍可删
- **文件**：`DeletePatientCommandHandler.cs`
- **修复**：删除前查 `PatientCrossModuleService` 引用计数，被引用返回 422（同批量删除逻辑）
- **验证**：集成测试（被引用患者删除 422）

### C4. 用户 Restore 端点缺失（P1）
- **问题**：US-USER-011 声称 ✅ 已实现，但 `ExecuteRestoreAsync` 返回 null、Server 无端点
- **文件**：Users 模块 Controller/Service/Desktop API
- **修复**：实现 `POST /users/{id}/restore`（层级管理：sysadmin 恢复 Admin，Admin 恢复 Doctor/Receptionist）
- **验证**：集成测试

### C5. 验方 Restore 端点缺失（P1）
- **问题**：US-FORM-012 状态 🔴 未实现（Server 无端点，Desktop 返回 null）
- **文件**：Formulas 模块 Controller/Service
- **修复**：实现 `POST /formulas/{id}/restore`（仅 Admin 业务管理）
- **验证**：集成测试

### C6. 医案打印日志回写端点未实现（P1）
- **问题**：`PUT /medicalcases/{id}/print-completed`、`POST /medicalcases/{id}/print-logs` 在 v1.0 范围但代码未实现
- **文件**：MedicalCaseProcessingController + PrintLog 实体
- **修复**：实现打印回写 + AuditLog/MedicalCasePrintLog
- **验证**：集成测试

---

## 批次 D：G-02 缺口补写发现的代码待实现（2026-08-04）—— ✅ 已完成（D1 `bea06505b` + D2 `4f7a9563c`）

> 来源：2026-08-04 G-02 缺口补写（doc-gap 核实发现文档目标态但代码缺失）。

### D1. 离线密码重置工具哈希算法缺陷（P1）
- **问题**：`src/Tools/PasswordHashGenerator/` 生成 BCrypt 哈希，但登录认证走 Identity PBKDF2（`UserManager`）——**两者不兼容**，直接写入 `AspNetUsers.PasswordHash` 会导致该用户无法登录（`DatabaseInitializationService.cs:193` 注释明确「避免 BCrypt/PBKDF2 哈希冲突」）
- **文件**：`src/Tools/PasswordHashGenerator/`（依赖 `PasswordHelper`）、或新增专用重置命令
- **修复**：改为生成 Identity PBKDF2 兼容哈希（参考 `IdentitySeedData` 哈希流程），或改用专用离线重置命令（`UserManager.GeneratePasswordResetTokenAsync` 风格）
- **验证**：工具输出哈希 → 写入 DB → 该用户可登录

### D2. ConfigurationSections 常量类缺失（P2）
- **问题**：11b-configuration.md 声称「14 个 Options 类由 `ConfigurationSections` 常量类统一管理配置节名称」——**代码中无此类**
- **文件**：`src/Shared/LYBT.Shared.Configuration/Options/`（若需）
- **修复**：新增 `ConfigurationSections` 常量类（8 服务端 + 1 共享 + 4 客户端 + 1 WebAPI = 14 项）或删除文档声称
- **验证**：文档与代码一致

---

## 验收清单

- [ ] 所有策略补丁后 `dotnet build` 0 错误
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` 通过
- [ ] `dotnet test tests/LYBT.Tests.Server/` 通过
- [ ] 权限矩阵文档与代码策略完全一致
- [ ] 医案创建时机符合 BR-000（2026-08-03 修订：**接诊即建**——StartVisit/QuickVisit/本地模式原子创建）
- [ ] 患者删除/禁用仅 Admin+；前台不可查看药材/验方；打印仅 Doctor；Admin 挂号只读（2026-08-03 决策四连）
- [ ] Git commit: `fix(auth): align authorization policies with permission matrix`
