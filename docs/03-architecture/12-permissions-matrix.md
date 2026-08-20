# Permissions — 权限矩阵（视图层速查）

> **⚠️ 本文件是权限矩阵的视图层速查，权威定义见 [[01-product/04-permissions#一、统一权限矩阵]]（SSOT）。**
>
> - **角色定义**见 [02-personas.md](../01-product/02-personas.md)
> - **完整权限矩阵 + 代码策略映射 + P0/P1/P2 修复项**见 [04-permissions.md](../01-product/04-permissions.md)
> - **本文件**仅保留行级安全、授权策略速查、代码待对齐清单等架构层视图内容

> 权限矩阵详见 [[01-product/04-permissions#一、统一权限矩阵]]（SSOT）。本节为视图层速查——角色 × 操作权限详见目标态定义（2026-08-03 权限四连决策），代码策略映射与 P0-P2 修复项均在 04-permissions.md 中跟踪。

## Row-Level Security

| 表 | 行级安全 | 实现方式 |
| --- | :---: | ------ |
| MedicalCases | ✅ | Doctor 仅查自己创建的（代码检查） |
| Consultations | ✅ | 通过 MedicalCase 聚合根间接访问 |
| Prescriptions | ✅ | 通过 MedicalCase 聚合根间接访问 |
| Users | ✅ | Admin 仅管理非 SuperAdmin 用户 |
| Patients | ❌ | 所有角色可访问全部患者 |
| Herbs | ❌ | 所有有权限角色可访问全部药材 |

## Authorization Policies

> 授权策略定义、代码策略映射、修复项详见 [04-permissions.md](../01-product/04-permissions.md) §当前代码策略映射。

---

## 代码待对齐清单（审计 2026-06-28）

> ⚠️ 以下为**代码层问题**（非文档问题），源自 2026-06-28 角色驱动审计（报告已完成使命删除，决策痕迹见 [项目总账 §九](../03-architecture/13-project-master-plan.md)）。文档保留目标态，代码修复由各 D 项跟踪。标注 💻=纯代码修复。

### P0 阻断（修复前不可动 D7）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
|---|------|------|---------|---------|
| **K1** | ⚠️💻 | `DoctorOrReceptionist` 策略注册仅含 Doctor/Receptionist，与本文档矩阵矛盾（矩阵称含 SuperAdmin/Admin/Doctor/Receptionist）。**D7 切换前若不先扩策略定义加 Admin/SuperAdmin，切换后 Admin/SuperAdmin 全部锁出** | `AuthenticationServiceCollectionExtensions.cs:129-131` | 扩策略注册：`RequireRole(SuperAdmin, Admin, Doctor, Receptionist)` |

### P0 信任根安全（公网部署前必修）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
| --- | ------ | ------ | --------- | --------- |
| **K4** | 💻 | **生产门控失效**：✅ 已修复（2026-08-12 K4）：`IdentitySeedData.ResolveSysAdminPassword` 生产必须环境变量 `DefaultPasswords__SysAdminPassword`（缺失抛异常禁回退），不再有明文默认密码 | `IdentitySeedData.cs` | K4 已实施（`IdentitySeedData.ResolveSysAdminPassword` + `DatabaseInitializationService.ValidateSetupToken`） |
| **K5** | ✅ | **已修复**：`IdentitySeedData` 已移除 admin 种子，仅创建 sysadmin。admin 改由向导创建 | `IdentitySeedData.cs` | 已完成 |

### P1 严重（角色边界正确性）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
| --- | ------ | ------ | --------- | --------- |
| **K7** | ✅ | **接诊链断裂（D8 bug）设计已确认**：`StartVisit` 仅调 `StartVisitAsync` 不建医案 + 返回 RegistrationId 冒充 MedicalCaseId。**2026-08-03 产品决策「接诊即建」**，R10 spec S5 修复方向确认，代码待实施 | `RegistrationsController.cs:200` | **2026-08-11 已修复**（StartVisit 原子创建 MedicalCase(Active)+Registration(InProgress)+返回 MedicalCaseId，见 13c §五 B1/T5 登记） |
| **K8** | 💻 | **LocalWebAPI 权限策略空缺**：`LocalWebAPI/Controllers/{Registrations,Patients,Herbs,MedicalCases}.cs` 仅 `[Authorize]` 无 Policy，本地 Doctor 可删患者/药材 CRUD，违本文档矩阵。R10 S3"本地全角色支持"↔Flow 3"本地无角色检查"矛盾 | `LocalWebAPI/Controllers/*.cs` | 明确本地是否启用角色策略（建议与远程一致+角色策略） |
| **K9** | 💻 | **接诊 Cancel 权限三向倒置**：`Cancel` XML 注释称"仅 Receptionist 可操作"，但无操作级 `[Authorize]`，回落类级 `DoctorOrAdmin`：前台被挡、Doctor/Admin 反被放行。与本文档矩阵（Receptionist✅/Doctor❌/Admin❌）三向倒置 | `RegistrationsController.cs:216-220` | 补操作级 `[Authorize(Policy=...)]` |
| **K3** | ✅ | **`DoctorOnly` 已落地**（2026-08-04）：`PolicyConstants.cs:6` 已有 `DoctorOnly` 常量（共 6 项），`MedicalCasesController.cs:98` 创建端点已使用。本文档「医案创建 Doctor 唯一」目标已有策略可执行 | `PolicyConstants.cs` | ~~新增 `DoctorOnly` 常量~~ → 已完成；恢复/纯 Admin 场景需新增纯 Admin 策略（见 04-permissions §2.2 注） |

| 完整问题清单（含 I1-I10 重要问题、S1-S10 次要问题）见 [项目总账 §九](../03-architecture/13-project-master-plan.md)（原审计报告已完成使命删除）。

### P1 文档校准发现（2026-08-02）

> 以下为文档校准（documentation-calibration）发现的**代码与矩阵不一致**项。矩阵已更新为目标态，代码待修复。

| # | 类型 | 问题 | 代码位置 | 修复方向（2026-08-03 决策已确认） |
| --- | ------ | ------ | --------- | --------- |
| **C1** | 💻 | **患者删除缺策略**：`PatientsController.Delete` 无操作级 `[Authorize]`，回退类级 `DoctorOrAdminOrReceptionist`（Doctor/Receptionist 也可删患者）。矩阵要求 Admin+ | `PatientsController.cs:129` | 补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`；禁用同理 |
| **C2** | 💻 | **药材创建/编辑缺策略**：`HerbsController.Create/Update` 无操作级 `[Authorize]`，回退类级 `DoctorOrReceptionist`（Doctor 也可创建/编辑药材）。矩阵要求 Admin+ | `HerbsController.cs:73,97` | Create/Update 补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`；GET 不含 Receptionist（前台不可查药材） |
| **C3** | 💻 | **挂号取消权限倒置**（与 K9 合并）：`RegistrationsController.Cancel` 无操作级策略，回退类级 `DoctorOrAdminOrReceptionist`。矩阵要求仅 Receptionist | `RegistrationsController.cs:95` | 补 `[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]` + 服务层校验 Source=Receptionist |
| **C4** | 💻 | **医案创建含 Receptionist**：`MedicalCasesController.Create` 策略 `DoctorOrAdminOrReceptionist`（含 Receptionist/代建）。设计决策 BR-000：医案创建仅 Doctor | `MedicalCasesController.cs:93` | 改策略为 `DoctorOnly`（需新增 PolicyConstants） |
| **C5** | 💻 | **前台可查看药材/验方**：`HerbsController`/`FormulasController` GET 类级 `DoctorOrReceptionist`。2026-08-03 决策：前台不可查看药材/验方 | `HerbsController.cs` / `FormulasController.cs` | GET 策略改为 Doctor+Admin+SuperAdmin（不含 Receptionist），需新增策略或操作级覆盖 |
| **C6** | 💻 | **打印无 DoctorOnly**：处方打印端点无操作级策略，2026-08-03 决策：仅 Doctor 打印（管理员可查打印记录） | 打印模块 Controller | 打印操作补 `[Authorize(Policy = PolicyConstants.DoctorOnly)]` |
| **C7** | ✅ | **CreateUser 层级校验缺失（USER-D04）**：创建用户仅粗粒度 `IsAdmin` 检查——sysadmin 创建 Doctor 实测 200 成功（应拒绝）。2026-08-13 已修复：`CreateUserCommandHandler` 加层级校验（Sysadmin→仅 Admin；Admin→仅 Doctor/Receptionist；禁创建 SuperAdmin——403） | `CreateUserCommandHandler.cs` | ~~补层级校验~~ → 已完成（2026-08-13 createuser-hierarchy-fix；`CreateUserHierarchyTests` 7 用例） |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
| ------ | ------ | ---------- |
| 2026-08-03 | v2.2 | 权限决策四连落地（四角色需求审查）：新增「挂号查看」行（Admin 只读）、「打印记录」行（Admin 可查）；药材/验方查看 Receptionist ❌；C1-C6 待对齐清单决策标注 |
| 2026-08-03 | v2.1 | K7 状态更新：D8 bug「接诊链断裂」设计已确认（2026-08-03 产品决策：接诊即建），代码待实施（已列入 backlog，2026-08-11 修复，见 13c §五） |
| 2026-08-02 | v2.0 | 去重：角色定义/策略表改为引用 02-personas.md 和 04-permissions.md；保留架构级 Resource×Operation 矩阵 + 代码待对齐清单 |
| 2026-08-02 | v1.3 | 文档校准（documentation-calibration）：权限策略新增 `DoctorOrAdminOrReceptionist`；矩阵对齐代码实际策略 |
| 2026-06-28 | v1.2 | 审计 S2/S6/K1-K9 文档标注：SuperAdmin/Sysadmin 双列语义说明；`AdminOnly`≡`AdminOrSuperAdmin` 合并建议；新增「代码待对齐清单」段（K1/K3/K4/K5/K7/K8/K9） | 角色驱动审计报告 S 类清理 + K 类代码待修项文档标注 |
| 2026-06-28 | v1.1 | 权限矩阵统一（权威决策 2026-06-28）：挂号创建 Doctor✅(QuickVisit)/Admin✗、挂号取消 Admin✗、药材删除 Admin✅(D5)、用户重置密码 Admin✅；D7 脚注与 Authorization Policies 段标注 `DoctorOnly` 为目标策略待新增 | 三文档（personas/matrix/代码）矛盾收敛，以 personas+权威决策为准 |
| 2026-06-28 | v1.0 | 结构治理：修正 `PolicyConstants` 与 baseline 链接相对路径（多余的 `../`）；补充变更记录段 |
