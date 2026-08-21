# 第2轮审查：权限策略一致性验证

> **审查日期**: 2026-08-20  
> **审查人**: Hermes Agent (架构师视角, intended vs implemented)  
> **审查范围**: `01-product/04-permissions SSOT` + `02-personas` + `03-architecture/09-security-architecture` + `12-permissions-matrix` + `PolicyConstants` + `AuthenticationServiceCollectionExtensions` + `27 Controllers` (Server+Local) + `RoleRegistry` + `Service层行级校验`

## 一、执行摘要

| 维度 | 结论 |
|------|------|
| **总体** | **策略注册已收敛 7 Policies，Controller 操作级 `RequireRole` 与文档目标态 80% 对齐**，`C1-C2/C4-C7` 等 2026-08-03 决策项已落地；但**批量操作 3 处漏授权 + 挂号取消越权**仍为可利用提权面，双端 `DoctorOnly` 定义过窄导致 `Admin` 被误拒的次生问题需关注 |
| **门禁** | `FallbackPolicy=RequireAuthenticatedUser` 正确，匿名端点仅 `auth/login|logout|refresh|auto-login` + `health|download` + `swagger`，无越权匿名面 |
| **严重度** | 🔴 CRITICAL 3 · 🟠 HIGH 3 · 🟡 MEDIUM 3 · 🟢 LOW 1 · ✅ PASS 7 |
| **最大风险** | `Patients/Catalog BatchDelete` 继承类级策略导致 `Doctor/Receptionist` 可批量删除患者/药材；`Registrations.Cancel` 仍为 `DoctorOrReceptionist` 允许 `Doctor` 取消他人挂号 |

---

## 二、基线（Intent）

**SSOT**：`docs/01-product/04-permissions.md §一 统一权限矩阵`(2026-08-03 四连决策)
**策略注册**：`PolicyConstants 7项`
| Policy | 文档定义角色 | 代码注册 `AuthenticationServiceCollectionExtensions.cs:133-160` |
| :--- | :--- | :--- |
| `AdminBusinessOnly` | `Admin` | `Admin` ✅ |
| `DoctorOnly` | `Doctor` | `Doctor` ✅ |
| `DoctorOrAdmin` | `SuperAdmin,Admin,Doctor` | 同 ✅ |
| `AdminOrSuperAdmin` | `Admin,SuperAdmin` | 同 ✅ |
| `SysAdminOnly` | `SuperAdmin` | 同 ✅ |
| `DoctorOrReceptionist` | `Doctor,Receptionist` | 同 ✅ |
| `DoctorOrAdminOrReceptionist` | `SuperAdmin,Admin,Doctor,Receptionist` | 同 ✅ |

**关键矩阵摘录**：
- 患者删除/禁用/恢复 → **仅 Admin/SuperAdmin**
- 药材/验方 查询 → **Receptionist ❌**；创建/编辑/删除 → **仅 Admin/SuperAdmin**
- 医案创建/打印 → **仅 Doctor**；医案查看 → Doctor仅自己 / Admin全部
- 挂号创建 → Receptionist + Doctor(QuickVisit)；**Admin ❌**；挂号取消 → **仅 Receptionist**；接诊 `StartVisit` → **仅 Doctor**
- 用户 CRUD → `Admin→Doctor/Receptionist`, `SuperAdmin→Admin` 层级，不可越级

---

## 三、CRITICAL（可直接提权/越权写）

### C1 — `Patients BatchDelete` 无操作级 `[Authorize]`：Doctor/Receptionist 可批量删除患者
- **意图**：`04-permissions P0-5` 患者删除仅 `AdminOrSuperAdmin`，需引用检查 `BR-DEL-001`
- **证据**：`src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:323-337` `BatchDelete` 仅 `[HttpPost("batch-delete")]` 无 `Authorize`，回落类级 `DoctorOrAdminOrReceptionist`；**Local 副本 `src/Client/Desktop/LocalWebAPI/Controllers/PatientsController.cs:337-338` 同病**
- **攻击者**：任意 `Doctor/Receptionist` 凭有效 JWT 调用 `POST /api/v1/patients/batch-delete {Ids:[...]}` 批量软删患者，绕过 `Admin` 审批
- **修复**：补 `[Authorize(Policy=AdminOrSuperAdmin)]` + 服务层 `BatchDeletePatientsCommandHandler` 复用 `Delete` 的 `CanDelete` 引用检查；`Local` 同步

### C2 — `Catalog (Herbs/Formulas) BatchDelete` 无操作级授权：Doctor 可批量删除药材/验方
- **证据**：`src/Server/Services/LYBT.WebAPI/Controllers/CatalogController.cs:360-377` `BatchDelete`（Herbs）及 `~795` `batch-delete`(Formulas) 均无 `Authorize`，回落 `DoctorOrAdmin`；Local `CatalogController.cs:220,704` 同病。
- **攻击者**：`Doctor`（`DoctorOrAdmin` 含 Doctor）可 `POST /api/v1/herbs/batch-delete` 清库，违反 `P0-1` `Admin 统一管库`
- **修复**：`CatalogController` 两处 `BatchDelete` 补 `AdminOrSuperAdmin`；`Local` 同步；并加 `ArchTest: BatchDelete must have AdminOrSuperAdmin`

### C3 — `Registrations.Cancel` 仍为 `DoctorOrReceptionist`：Doctor 可取消任意挂号（C3/K9 未闭环）
- **意图**：`04-permissions §挂号管理: 挂号取消 Receptionist ✅ Doctor ✗ Admin ✗`
- **证据**：`src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:70-73` `Cancel` 为 `DoctorOrReceptionist`；`Local RegistrationsController.cs:47-48` 同；文档 `12-permissions-matrix C3/K9` 2026-08-03 决策要求 `仅 Receptionist`
- **攻击者**：`Doctor` 取消 `Receptionist` 创建的 `Waiting` 挂号，破坏前台收费/队列
- **修复**：`Cancel` 改 `DoctorOrReceptionist → ReceptionistOnly`（新增 Policy 或 Handler 内 `if (OperatorRole != Receptionist) → 403`）；`Local` 同步。

---

## 四、HIGH（权限边界错配）

### H1 — `DoctorOnly` 定义为纯 `Doctor`：Admin/SuperAdmin 被误拒于打印/接诊/医案创建
- **证据**：`AuthenticationServiceCollectionExtensions.cs:138-140` `DoctorOnly = RequireRole(Doctor)` 单角色；`LocalJwtConfig.cs:83-85` 同。被用于 `MedicalCases.Create`(`DoctorOnly`), `RecordPrint`(`DoctorOnly`), `Registrations.StartVisit`(`DoctorOnly`)
- **意图冲突**：`04-permissions` 医案创建/打印为 `Doctor 唯一`，Admin 确实不应创建医案 — 此处**符合意图**；但 `RegistrationHub [Authorize(DoctorOrAdmin)]` 允许 Admin 看队列，而 `StartVisit` 却仅 `Doctor`，导致 **Admin 在 WebAPI 层看到队列却无法接诊**（虽符合业务“Admin 不接诊”，但与 Hub 策略不一致）
- **修复**：保持 `DoctorOnly` 语义，但文档显式声明“`DoctorOnly` 不含 Admin/SuperAdmin 为有意设计”，并在 `09-security-architecture.md §4` 加注

### H2 — `AdminRoleDefinition` 缺 `RegistrationModule`：Admin 无法查看挂号（与矩阵 Admin只读 矛盾）
- **证据**：`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/AdminRoleDefinition.cs:13-19` `Modules = {Users,Patients,Catalog,MedicalCase,Reports}` **无 Registration**；而 `04-permissions §挂号查看: Admin ✅ 全部只读`
- **影响**：Desktop 端 `Admin` 登录后 `RoleRegistry.GetModulesForRole(Admin)` 不加载 `RegistrationModule`，即使 `Server` 已允许 `Admin` 经 `DoctorOrAdminOrReceptionist` 查看挂号，UI 层无入口
- **修复**：`AdminRoleDefinition` 加 `RegistrationModule`（只读模式），或在 `02-personas.md` 明确“Admin 挂号仅在 Web 后台查看”

### H3 — `DownloadController` 类级 `[Authorize]` 但部分端点 `AllowAnonymous` 豁免过宽
- **证据**：`DownloadController.cs:14 [Authorize]` 类级，`31 [AllowAnonymous]` 标记下载页
- **影响**：若 `Download` 托管处方 PDF/患者导入模板，匿名可拉取敏感文件
- **修复**：`Download` 模板端点保持 `AllowAnonymous`，但处方 PDF 必须 `DoctorOrAdmin`；加注释区分

---

## 五、MEDIUM（双端/前端一致性）

### M1 — `K8` 已修复但 `K1` 策略定义仍存文档-代码偏差
- **证据**：`12-permissions-matrix K1` 称 `DoctorOrReceptionist` 应含 `SuperAdmin,Admin,Doctor,Receptionist` 否则 Admin 锁出；但代码该策略仅 `Doctor,Receptionist` 2 角色。目前未触发锁出因该策略仅用于 `Registrations.Create/Cancel`（Admin 本就不该访问），故**当前无害**
- **修复**：文档 `K1` 改为“`DoctorOrReceptionist` 为窄策略，不含 Admin 为有意；通用查询请用 `DoctorOrAdminOrReceptionist`”

### M2 — `Patients/ Catalog BatchCheckReference / CheckReference` 继承宽松类级策略
- **证据**：`PatientsController CheckReference(377) / BatchCheckReference(409)` 及 `Catalog CheckReference(415)` 均无方法级 `Authorize`，继承 `DoctorOrAdminOrReceptionist/DoctorOrAdmin`。虽为只读引用检查，但 `Receptionist` 可查患者引用间接探测医案存在性
- **修复**：只读检查保持宽松可接受，文档显式声明“引用检查为只读，不做操作级收紧”

### M3 — `MedicalCases UpdateStatus/Suspend/Cancel` 行级 `isAdmin` 布尔 vs `OperatorRole` 枚举混用
- **证据**：`MedicalCasesController.cs:267 UpdateStatus / 313 Suspend / 337 Cancel` 均 `var isAdmin = OperatorRole is Admin or SuperAdmin` 传 `bool isAdmin` 给 `MedicalCaseServiceHelper.EnsureCanEdit`；而 `Catalog` 已在 `P1-7/8` 改造为 `OperatorRole` 枚举
- **修复**：`MedicalCaseCommandService` 接口改为 `OperatorRole`（与 `Catalog` 对齐）

---

## 六、PASS（已对齐）

| 项 | 证据 | 结论 |
|----|------|------|
| **Patients Delete/Toggle/Restore** | `PatientsController.cs:240 AdminOrSuperAdmin / 269 AdminOrSuperAdmin / 299 AdminBusinessOnly` + `Local` 同步 | ✅ `C1` 已修复 |
| **Catalog Herbs/Formulas Create/Update/Delete** | `CatalogController Server+Local` `Create/Update/Delete/Toggle/Restore/BatchImport` 均为 `AdminOrSuperAdmin` | ✅ `C2/P0-1` 已修复 |
| **MedicalCases Create/Print/Close** | `Create DoctorOnly` + `RecordPrint DoctorOnly` + `Close AdminOrSuperAdmin` + `Local` 同步 | ✅ `C4/C6` 已修复 |
| **Registrations StartVisit** | `Server 54 DoctorOnly / Local 26,29 DoctorOnly` | ✅ `K7` 已修复 |
| **Users 层级** | `CreateUserCommandHandler.cs:38-54` `SuperAdmin→仅Admin, Admin→仅Doctor/Receptionist` | ✅ `C7` 已修复 |
| **Local 策略补齐** | `LocalWebAPI` 12 Controllers 现均显式 `PolicyConstants.*` | ✅ `K8` 已修复 |
| **FallbackPolicy** | `AuthenticationServiceCollectionExtensions:130 FallbackPolicy=RequireAuthenticatedUser` | ✅ 无未授权匿名面 |

---

## 七、修复清单（按权限影响排序）

| 优先级 | 缺陷 | 文件 | 改动 |
|--------|------|------|------|
| **P0** | `Patients BatchDelete` 补授权 | `PatientsController.cs:323` Server+Local | `+ [Authorize(AdminOrSuperAdmin)]` |
| **P0** | `Catalog BatchDelete` Herbs+Formulas 补授权 | `CatalogController.cs:360,795` Server + `220,704` Local | 同上 |
| **P0** | `Registrations.Cancel` 收紧为 Receptionist | `RegistrationsController.cs:70` Server+Local | `DoctorOrReceptionist → ReceptionistOnly` |
| **P1** | `Admin RegistrationModule` | `AdminRoleDefinition.cs:13` | `+ "RegistrationModule"` |
| **P1** | `DoctorOnly` 文档注 | `09-security-architecture.md §4` | 加注“不含 Admin 为有意” |
| **P2** | `MedicalCase bool isAdmin → OperatorRole` | `MedicalCaseCommandService.cs` | 接口改枚举 |

> **验证**：`dotnet build --no-incremental` 0 警告 + `ArchTests: P07 ModuleIsolation / P10 NoDbContextInService` + 手动矩阵冒烟：`Receptionist token → POST /patients/batch-delete → 403`, `Doctor token → PUT /registrations/{id}/cancel → 403`, `Admin token → POST /medicalcases → 403`
