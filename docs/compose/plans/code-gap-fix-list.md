# 代码偏移修复清单（Documentation Calibration 产出）

> 来源：2026-08-02 文档校准任务
> 状态：⬜ 待执行
> 原则：先批 A（纯策略补丁），再批 B（业务逻辑）

---

## 批次 A：纯策略补丁（⚡ 改动极小，可一次过）

### A1. PatientsController.Delete 补策略
- **问题**：Delete 无操作级 `[Authorize]`，回退类级 `DoctorOrAdminOrReceptionist`，Doctor/Receptionist 也可删患者
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:129`
- **修复**：Delete 方法补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`
- **验证**：`dotnet build` + 架构测试

### A2. HerbsController Create/Update 补策略
- **问题**：Create/Update 无操作级 `[Authorize]`，回退类级 `DoctorOrReceptionist`，Doctor 也可创建/编辑药材
- **文件**：`src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:73,97`
- **修复**：Create 和 Update 方法各补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`
- **验证**：`dotnet build` + 架构测试

### A3. PolicyConstants 补 DoctorOrReceptionist 含 SuperAdmin/Admin（K1）
- **问题**：`DoctorOrReceptionist` 策略注册仅含 Doctor/Receptionist，缺少 SuperAdmin/Admin
- **文件**：`src/Server/Core/LYBT.Infrastructure/.../AuthenticationServiceCollectionExtensions.cs`
- **修复**：扩策略注册 `RequireRole(SuperAdmin, Admin, Doctor, Receptionist)`
- **影响**：所有使用 `DoctorOrReceptionist` 的端点（Herbs/Formulas）权限范围扩大
- **验证**：`dotnet build` + 架构测试 + 角色集成测试

### A4. PolicyConstants 补 DoctorOnly（K3）
- **问题**：医案创建目标策略 `DoctorOnly` 不存在
- **文件**：`src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs`
- **修复**：新增 `public const string DoctorOnly = "DoctorOnly";` + 注册策略
- **验证**：`dotnet build`

---

## 批次 B：业务逻辑修复（🔧 需设计确认）

### B1. StartVisit 不再建医案（K7 修正）
- **问题**：原方案要求 StartVisit 原子创建医案，但架构决策（BR-000）明确医案由医生主动创建
- **文件**：`src/Server/Modules/LYBT.Module.Registration/.../RegistrationsController.cs`
- **修复**：StartVisit 仅改 Registration 状态为 InProgress，**不建医案**；移除返回 MedicalCaseId 的逻辑
- **参考**：BR-000（医案创建时机）、R10 spec S5（需更新）
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

### B5. ~~医案创建 Receptionist 代建确认（C4）~~ ✅ 已关闭
- **决策**：Receptionist 不应直接创建医案。前台只建挂号，医案由医生创建
- **代码改动**：`MedicalCasesController.Create` 策略从 `DoctorOrAdminOrReceptionist` 改为 `DoctorOrAdmin`
- **已记录**：`07-medical-cases.md` BR-000、`08-registration.md` 架构决策

---

## 验收清单

- [ ] 所有策略补丁后 `dotnet build` 0 错误
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` 通过
- [ ] `dotnet test tests/LYBT.Tests.Server/` 通过
- [ ] 权限矩阵文档与代码策略完全一致
- [ ] 医案创建时机符合 BR-000（医生写诊断时才建）
- [ ] Git commit: `fix(auth): align authorization policies with permission matrix`
