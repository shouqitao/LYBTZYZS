# Permissions — 权限矩阵

> **角色定义**见 [02-personas.md](../01-product/02-personas.md)。**完整权限矩阵**见 [04-permissions.md](../01-product/04-permissions.md)。

## Resource × Operation × Role Matrix

| 资源 | 操作 | Receptionist | Doctor | Admin | SuperAdmin | Sysadmin |
|------|------|:---:|:---:|:---:|:---:|:---:|
| 患者 | 查看 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 患者 | 创建/编辑 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 患者 | 删除(软) | ❌ | ❌ | ✅ | ✅ | ✅ |
| 患者 | 恢复 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 挂号 | 查看 | ✅ | ✅(自己) | ✅(只读) | ✅(只读) | ✅(只读) |
| 挂号 | 创建 | ✅ | ✅(QuickVisit) | ❌ | ❌ | ❌ |
| 挂号 | 取消 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 医案 | 查看 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 医案 | 创建 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 医案 | 编辑 | ❌ | ✅(当天) | ✅(纠偏) | ✅(EditReason) | ✅ |
| 医案 | 完成 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 医案 | 审计日志 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 处方 | 打印 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 处方 | 打印记录 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 处方 | 回写 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 药材 | 查看 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 药材 | 创建/编辑 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 药材 | 删除 | ❌ | ❌ | ✅(D5 引用检查) | ✅ | ✅ |
| 验方 | 查看 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 验方 | 创建/编辑 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 用户 | 查看 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 用户 | 创建/编辑 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 用户 | 删除/禁用 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 用户 | 重置密码 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 系统设置 | 查看/修改 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 审计日志 | 查看 | ❌ | ❌ | ❌ | ✅ | ✅ |

> **矩阵说明（2026-08-03 更新）**：本表为**目标态**（2026-08-03 权限四连决策）。药材查看 Receptionist❌、药材创建 Admin+ only、挂号创建仅 Receptionist、医案创建 Doctor only 均为产品决策目标。**代码当前仍为类级策略**（患者/挂号 `DoctorOrAdminOrReceptionist`、药材/验方 `DoctorOrReceptionist`、医案 `DoctorOrAdmin`+创建 `DoctorOrAdminOrReceptionist`），操作级细分待修（见 [04-permissions.md](../01-product/04-permissions.md) P0-P2 修复项）。K1/K7/K8/K9 待修复项仍保留在「代码待对齐清单」中。

## Row-Level Security

| 表 | 行级安全 | 实现方式 |
|---|:---:|------|
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

> ⚠️ 以下为**代码层问题**（非文档问题），源自 [角色驱动审计报告](../compose/reports/2026-06-28-role-driven-audit.md)。文档保留目标态，代码修复由各 D 项跟踪。标注 💻=纯代码修复。

### P0 阻断（修复前不可动 D7）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
|---|------|------|---------|---------|
| **K1** | ⚠️💻 | `DoctorOrReceptionist` 策略注册仅含 Doctor/Receptionist，与本文档矩阵矛盾（矩阵称含 SuperAdmin/Admin/Doctor/Receptionist）。**D7 切换前若不先扩策略定义加 Admin/SuperAdmin，切换后 Admin/SuperAdmin 全部锁出** | `AuthenticationServiceCollectionExtensions.cs:129-131` | 扩策略注册：`RequireRole(SuperAdmin, Admin, Doctor, Receptionist)` |

### P0 信任根安全（公网部署前必修）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
|---|------|------|---------|---------|
| **K4** | 💻 | **生产门控失效**：`IdentitySeedData.SeedRolesAndAdminAsync` 不读 `SystemAdminOptions.AllowAutoCreateInProduction`/`InitialSetupToken`，也无 `IHostEnvironment` 判定，直接用明文 `SysAdmin@2026!` 创建 sysadmin。生产环境 sysadmin 默认密码裸奔 | `IdentitySeedData.cs` | 注入 `IDefaultPasswordService`+`IHostEnvironment`，复用 `GetOrGeneratePassword`/`ValidateSetupToken` |
| **K5** | ✅ | **已修复**：`IdentitySeedData` 已移除 admin 种子，仅创建 sysadmin。admin 改由向导创建 | `IdentitySeedData.cs` | 已完成 |

### P1 严重（角色边界正确性）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
|---|------|------|---------|---------|
| **K7** | ✅ | **接诊链断裂（D8 bug）设计已确认**：`StartVisit` 仅调 `StartVisitAsync` 不建医案 + 返回 RegistrationId 冒充 MedicalCaseId。**2026-08-03 产品决策「接诊即建」**，R10 spec S5 修复方向确认，代码待实施 | `RegistrationsController.cs:200` | StartVisit 改原子创建 MedicalCase(Active)+Registration(InProgress)+返回 MedicalCaseId（已列入 code-gap-fix-list B1） |
| **K8** | 💻 | **LocalWebAPI 权限策略空缺**：`LocalWebAPI/Controllers/{Registrations,Patients,Herbs,MedicalCases}.cs` 仅 `[Authorize]` 无 Policy，本地 Doctor 可删患者/药材 CRUD，违本文档矩阵。R10 S3"本地全角色支持"↔Flow 3"本地无角色检查"矛盾 | `LocalWebAPI/Controllers/*.cs` | 明确本地是否启用角色策略（建议与远程一致+角色策略） |
| **K9** | 💻 | **接诊 Cancel 权限三向倒置**：`Cancel` XML 注释称"仅 Receptionist 可操作"，但无操作级 `[Authorize]`，回落类级 `DoctorOrAdmin`：前台被挡、Doctor/Admin 反被放行。与本文档矩阵（Receptionist✅/Doctor❌/Admin❌）三向倒置 | `RegistrationsController.cs:216-220` | 补操作级 `[Authorize(Policy=...)]` |
| **K3** | 💻 | `PolicyConstants` 缺 `DoctorOnly`：本文档"医案创建 Doctor 唯一"目标无策略可执行（`PolicyConstants.cs` 仅 4 项）。`personas` "唯一创建者"+"DoctorOrAdmin 策略"自相矛盾（该策略含 Admin） | `PolicyConstants.cs` | 新增 `DoctorOnly` 常量，或服务层 `CreatedBy==currentUser` 归属校验兜底 |

> 完整问题清单（含 I1-I10 重要问题、S1-S10 次要问题）见 [审计报告全文](../compose/reports/2026-06-28-role-driven-audit.md)。

### P1 文档校准发现（2026-08-02）

> 以下为文档校准（documentation-calibration）发现的**代码与矩阵不一致**项。矩阵已更新为目标态，代码待修复。

| # | 类型 | 问题 | 代码位置 | 修复方向（2026-08-03 决策已确认） |
|---|------|------|---------|---------|
| **C1** | 💻 | **患者删除缺策略**：`PatientsController.Delete` 无操作级 `[Authorize]`，回退类级 `DoctorOrAdminOrReceptionist`（Doctor/Receptionist 也可删患者）。矩阵要求 Admin+ | `PatientsController.cs:129` | 补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`；禁用同理 |
| **C2** | 💻 | **药材创建/编辑缺策略**：`HerbsController.Create/Update` 无操作级 `[Authorize]`，回退类级 `DoctorOrReceptionist`（Doctor 也可创建/编辑药材）。矩阵要求 Admin+ | `HerbsController.cs:73,97` | Create/Update 补 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`；GET 不含 Receptionist（前台不可查药材） |
| **C3** | 💻 | **挂号取消权限倒置**（与 K9 合并）：`RegistrationsController.Cancel` 无操作级策略，回退类级 `DoctorOrAdminOrReceptionist`。矩阵要求仅 Receptionist | `RegistrationsController.cs:95` | 补 `[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]` + 服务层校验 Source=Receptionist |
| **C4** | 💻 | **医案创建含 Receptionist**：`MedicalCasesController.Create` 策略 `DoctorOrAdminOrReceptionist`（含 Receptionist/代建）。设计决策 BR-000：医案创建仅 Doctor | `MedicalCasesController.cs:93` | 改策略为 `DoctorOnly`（需新增 PolicyConstants） |
| **C5** | 💻 | **前台可查看药材/验方**：`HerbsController`/`FormulasController` GET 类级 `DoctorOrReceptionist`。2026-08-03 决策：前台不可查看药材/验方 | `HerbsController.cs` / `FormulasController.cs` | GET 策略改为 Doctor+Admin+SuperAdmin（不含 Receptionist），需新增策略或操作级覆盖 |
| **C6** | 💻 | **打印无 DoctorOnly**：处方打印端点无操作级策略，2026-08-03 决策：仅 Doctor 打印（管理员可查打印记录） | 打印模块 Controller | 打印操作补 `[Authorize(Policy = PolicyConstants.DoctorOnly)]` |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-08-03 | v2.2 | 权限决策四连落地（四角色需求审查）：新增「挂号查看」行（Admin 只读）、「打印记录」行（Admin 可查）；药材/验方查看 Receptionist ❌；C1-C6 待对齐清单决策标注 |
| 2026-08-03 | v2.1 | K7 状态更新：D8 bug「接诊链断裂」设计已确认（2026-08-03 产品决策：接诊即建），代码待实施（code-gap-fix-list B1） |
| 2026-08-02 | v2.0 | 去重：角色定义/策略表改为引用 02-personas.md 和 04-permissions.md；保留架构级 Resource×Operation 矩阵 + 代码待对齐清单 |
| 2026-08-02 | v1.3 | 文档校准（documentation-calibration）：权限策略新增 `DoctorOrAdminOrReceptionist`；矩阵对齐代码实际策略 |
| 2026-06-28 | v1.2 | 审计 S2/S6/K1-K9 文档标注：SuperAdmin/Sysadmin 双列语义说明；`AdminOnly`≡`AdminOrSuperAdmin` 合并建议；新增「代码待对齐清单」段（K1/K3/K4/K5/K7/K8/K9） | 角色驱动审计报告 S 类清理 + K 类代码待修项文档标注 |
| 2026-06-28 | v1.1 | 权限矩阵统一（权威决策 2026-06-28）：挂号创建 Doctor✅(QuickVisit)/Admin✗、挂号取消 Admin✗、药材删除 Admin✅(D5)、用户重置密码 Admin✅；D7 脚注与 Authorization Policies 段标注 `DoctorOnly` 为目标策略待新增 | 三文档（personas/matrix/代码）矛盾收敛，以 personas+权威决策为准 |
| 2026-06-28 | v1.0 | 结构治理：修正 `PolicyConstants` 与 baseline 链接相对路径（多余的 `../`）；补充变更记录段 |
