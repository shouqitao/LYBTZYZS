# 代码偏移修复清单（Documentation Calibration + 审稿产出）

> 来源：2026-08-02 文档校准 + 审稿任务
> 状态：⬜ 待执行
> 原则：先批 A（纯策略补丁），再批 B（业务逻辑）
> 术语：Admin = 管理员，Sysadmin = 超管

---

## 批次 A：纯策略补丁（⚡ 改动极小，可一次过）

### A1. PatientsController.Delete 补策略
- **问题**：Delete 无操作级 `[Authorize]`，回退类级 `DoctorOrAdminOrReceptionist`，Doctor/Receptionist 也可删患者
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:129`
- **修复**：Delete 方法补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`
- **验证**：`dotnet build` + 架构测试

### A2. HerbsController Create/Update 改策略
- **问题**：Create/Update 无操作级 `[Authorize]`，回退类级 `DoctorOrReceptionist`，Doctor 也可创建/编辑药材
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:73,97`
- **修复**：Create 和 Update 方法各补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`
- **验证**：`dotnet build` + 架构测试

### A3. FormulasController Create/Update 补策略
- **问题**：Create/Update 用类级 `DoctorOrReceptionist`，不含管理员
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- **修复**：Create 和 Update 方法各补 `[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]`
- **验证**：`dotnet build` + 架构测试

### A4. MedicalCasesController.Create 改策略
- **问题**：Create 策略含 Receptionist（可代建），但 BR-000 决策仅 Doctor 可建
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs:93`
- **修复**：Create 策略从 `DoctorOrAdminOrReceptionist` 改为 `DoctorOrAdmin`
- **验证**：`dotnet build` + 架构测试

### A5. PolicyConstants 补 DoctorOnly（K3）
- **问题**：医案创建目标策略 `DoctorOnly` 不存在
- **文件**：`src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs`
- **修复**：新增 `public const string DoctorOnly = "DoctorOnly";` + 注册策略
- **验证**：`dotnet build`

---

## 批次 B：业务逻辑修复（🔧 需设计确认）

### B1. StartVisit 接诊即建（D8 修复）
- **问题**：StartVisit 当前不创建医案 + 返回 RegistrationId 冒充 MedicalCaseId → Desktop 导航到空医案（D8 bug 确认存在）
- **决策**：2026-08-03 产品负责人确认「**接诊即建**」——StartVisit/QuickVisit/本地选患者开始看诊时原子创建 MedicalCase(Active) + Registration(InProgress)。原 B1「不再建医案」基于旧 BR-000（2026-08-02），已随决策修订作废
- **文件**：`src/Server/Modules/LYBT.Module.Registration/Application/Commands/StartVisitCommandHandler.cs`、`RegistrationsController.cs`
- **修复**：StartVisit 改为原子事务：`Registration.Status=InProgress` + 创建 `MedicalCase(Active)` 关联 `RegistrationId` + 返回 `MedicalCaseId`；复用 BR-001 单活跃医案约束，碰撞时提示「重开现有医案」
- **参考**：BR-000（2026-08-03 修订）、R10 spec S5、08-registration.md US-REG-005
- **验证**：集成测试 + Desktop 端接诊流程

### B2. 挂号取消权限修复（K9/C3 合并）
- **问题**：Cancel 无操作级策略，前台被挡、Doctor/Admin 反被放行
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:95`
- **修复**：Cancel 补 `[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]` + 服务层校验 Source=Receptionist
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

---

## 验收清单

- [ ] 所有策略补丁后 `dotnet build` 0 错误
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` 通过
- [ ] `dotnet test tests/LYBT.Tests.Server/` 通过
- [ ] 权限矩阵文档与代码策略完全一致
- [ ] 医案创建时机符合 BR-000（2026-08-03 修订：**接诊即建**——StartVisit/QuickVisit/本地模式原子创建）
- [ ] Git commit: `fix(auth): align authorization policies with permission matrix`
