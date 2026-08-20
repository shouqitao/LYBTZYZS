# 权限矩阵与修复项 (Permissions)

> 版本: v4.1 | 日期: 2026-08-03 | 状态: 文档定义（设计态）

本文件定义四角色的权限矩阵、当前代码策略映射、已知问题与修复计划。



> 角色定位与工作流程详见 [02-personas.md](02-personas.md)。
> 角色交接与协同流程详见 [05-role-interactions.md](05-role-interactions.md)。

---

## 一、统一权限矩阵

### 1.1 功能权限

| 操作 | Receptionist | Doctor | Admin | SuperAdmin |
| ------ | :-----------: | :------: | :-----: | :----------: |
| **用户管理** | | | | |
| 用户 CRUD | ✗ | ✗ | ✅（仅 Doctor/Receptionist） | ✅（仅 Admin） |
| 用户恢复 | ✗ | ✗ | ✅（sysadmin 恢复 Admin 账号） | ✅（Admin 恢复 Doctor/Receptionist，一级管一级） |
| 重置密码 | ✗ | ✗ | ✅（仅 Doctor/Receptionist） | ✅（仅 Admin） |
| 禁用/启用/删除 | ✗ | ✗ | ✅（仅 Doctor/Receptionist） | ✅（仅 Admin） |
| 角色变更 | ✗ | ✗ | ✗ | ✗ |
| **患者管理** | | | | |
| 患者 CRUD | ✅ | ✅ | ✅ | ✅ |
| 患者删除 | ✗ | ✗ | ✅ | ✅ |
| 患者恢复 | ✗ | ✗ | ✅（业务管理） | ✗（仅系统运维） |
| 患者启用/禁用 | ✗ | ✗ | ✅ | ✅ |
| **药材管理** | | | | |
| 药材查询 | ✗（前台不涉及药材） | ✅ | ✅ | ✅ |
| 药材创建/编辑 | ✗ | ✗（Admin 统一管库） | ✅ | ✅ |
| 药材删除 | ✗ | ✗ | ✅（需引用检查） | ✅ |
| 药材启用/禁用 | ✗ | ✗ | ✅ | ✅ |
| **验方管理** | | | | |
| 验方查询 | ✗ | ✅（自己 + 共享） | ✅ | ✅ |
| 验方创建/编辑 | ✗ | ✅（仅自己创建） | ✅ | ✅ |
| 验方导出/导入 | ✗ | 📋 | 📋 | 📋 |
| **医案管理** | | | | |
| 医案创建 | ✗ | ✅ **唯一** | ✗ | ✗ |
| 医案查看 | ✗ | ✅（仅自己的） | ✅（全部） | ✅ |
| 医案完成/关闭 | ✗ | ✅（仅自己的） | ✅（仅状态变更） | ✅ |
| 医案纠偏修改 | ✗ | ✗ | ✅（需填原因） | ✅ |
| **挂号管理** | | | | |
| 挂号查看 | ✅（全部） | ✅（仅自己的） | ✅（全部只读） | ✅（全部只读） |
| 挂号创建 | ✅ | ✅（Source=Doctor 两步建号） | ✗ | ✗ |
| 挂号取消 | ✅ | ✗ | ✗ | ✗ |
| **打印** | | | | |
| 处方打印 | ✗ | ✅ **唯一** | ✗ | ✗ |
| 打印记录查看 | ✗ | ✅（仅自己的） | ✅（全部） | ✅（全部） |
| **报表** | | | | |
| 报表查看 | ✗ | ✅ | ✅ | ✅ |

### 1.2 会话超时策略

> 会话超时策略（不活动超时、绝对超时）详见 [02-auth.md](../02-requirements/02-auth.md) 认证与会话模块。

---

## 二、当前代码策略映射

### 2.1 Controller 级授权策略

| Controller | 代码策略 | 目标策略（操作级细分） | 差异 |
| ----------- | --------- | --------- | ------ |
| `RegistrationsController` | `DoctorOrAdminOrReceptionist` | GET：Doctor+Receptionist+**Admin 只读**；POST：Receptionist/Doctor（Source 区分——两步建号 2026-08-13）；start-visit：`DoctorOnly`；cancel：Receptionist | ⚠️ Admin 只读查看挂号（2026-08-03 决策）；创建/取消仅前台；接诊/QuickVisit 仅 Doctor |
| `PatientsController` | `DoctorOrAdminOrReceptionist` | GET/POST/PUT：Doctor+Receptionist；DELETE/禁用：`AdminOrSuperAdmin` | ⚠️ 删除/禁用仅 Admin+（2026-08-03 决策）；Admin 不直接管理患者读写 |
| `MedicalCasesController` | `DoctorOrAdmin` | 创建：`DoctorOnly`；查看/编辑按 MC 铁律 | ⚠️ 创建仅 Doctor（C4/K3 待修） |
| `ReportsController` | `DoctorOrAdmin` | GET：Doctor+Admin+SuperAdmin（**前台不可查**） | 2026-08-08 统一双端策略（A-31-C0） |
| `ConfigurationController` | `SysAdminOnly` | 配置读写/生产验证/restart/validate：**仅 SuperAdmin（sysadmin）**——业务管理员不碰系统配置 | — |
| `HerbsController` | `DoctorOrReceptionist` | GET：Doctor+Admin+SuperAdmin（**前台不可查**）；POST/PUT/DELETE：`AdminOrSuperAdmin` | 🔴 前台不可查看药材（2026-08-03 决策）；写操作仅 Admin |
| `FormulasController` | `DoctorOrReceptionist` | GET：Doctor+Admin+SuperAdmin（**前台不可查**）；POST/PUT：`DoctorOrAdmin` | 🔴 前台不可查看验方（2026-08-03 决策）；写操作 Doctor(自己)+Admin |

### 2.2 PolicyConstants 现有策略

| 策略名 | 包含角色 |
| -------- | --------- |
| `DoctorOnly` | Doctor |
| `DoctorOrAdmin` | Doctor, Admin |
| `AdminOrSuperAdmin` | Admin, SuperAdmin |
| `SysAdminOnly` | SuperAdmin（sysadmin 专属——配置/部署等运维操作，2026-08-11 SHELL-018 引入） |
| `DoctorOrReceptionist` | Doctor, Receptionist |
| `DoctorOrAdminOrReceptionist` | Doctor, Admin, Receptionist |

> **Phase② 计划**：采用 RBAC + Permission 枚举方案（~35 项原子操作），替代当前分散在 Controller 策略/Service 层的碎片化权限逻辑。

### 2.3 前台权限细化

| 操作 | 前台权限 | 说明 |
| ------ | ---------- | ------ |
| 挂号创建 | ✅ | 核心职能（当天检查防重复） |
| 挂号取消 | ✅ | 仅当天的 `Status=Waiting` 挂号 |
| 患者 CRUD | ✅ | 登记、查找、编辑 |
| 读卡登记 | ✅ | 身份证读卡 + 自动填充 |
| 药材查询 | ❌ | 前台不涉及药材 |
| 验方/医案 | ❌ | |
| 用户管理 | ❌ | |
| 打印 | ❌ | |

---

## 三、已知权限问题与修复项

### 3.1 P0 必须修复（阻断核心流程）—— 目标策略均已 2026-08-03 产品确认

| # | 问题 | Controller | 目标策略 | 修复方案 |
| --- | ------ | ----------- | --------- | ---------- |
| P0-1 | Herbs 策略 `DoctorOrReceptionist` → Doctor/Receptionist 可写药材 | `HerbsController` | `AdminOrSuperAdmin`（写）；GET 不含前台 | GET：Doctor+Admin+SuperAdmin；POST/PUT/DELETE：`AdminOrSuperAdmin` |
| P0-2 | MedicalCases `DoctorOrAdmin` → Admin 可创建医案 | `MedicalCasesController` | `DoctorOnly` | 新增 `DoctorOnly` 策略；Create 操作限定 Doctor |
| P0-3 | Formulas 策略 `DoctorOrReceptionist` → Receptionist 可写验方 | `FormulasController` | 写：`DoctorOrAdmin`；读：不含前台 | GET：Doctor+Admin+SuperAdmin；POST/PUT：`DoctorOrAdmin` |
| P0-4 | Herbs 删除无引用检查 → 可删除被处方引用的药材 | `HerbsService` | — | 增加引用检查（BR-DEL-001） |
| P0-5 | Patients 删除无引用检查 → 可删除被医案引用的患者 | `PatientsService` | — | 增加引用检查（BR-DEL-001） |

### 3.2 P1 重要（影响安全性/完整性）

| # | 问题 | 位置 | 修复方案 |
| --- | ------ | ------ | ---------- |
| P1-1 | Formulas `GetDetail` 无所有权检查 → Admin 可读他人非共享验方 | `FormulasService` | 增加 `CreatedBy` 归属检查 |
| P1-2 | 医案打印回写缺失 | 医案模块 | 恢复 PrintLog 字段/实体 |
| P1-3 | 审计日志缺失 | SecurityAuditLog | 恢复审计日志记录 |
| P1-4 | Registrations 策略未操作级细分 | `RegistrationsController` | GET：Doctor+Receptionist+Admin 只读；POST：Receptionist/Doctor；start-visit：`DoctorOnly`；cancel：Receptionist |
| P1-5 | Patients 删除/禁用未限 Admin | `PatientsController` | DELETE/禁用：`AdminOrSuperAdmin`；读写：Doctor+Receptionist |
| P1-6 | 打印无 `DoctorOnly` 策略 | 打印模块 | 处方打印操作限定 Doctor（2026-08-03 决策：仅 Doctor 打印，管理员可查记录） |

### 3.3 P2 增强（Phase②）

| # | 问题 | 修复方案 |
| --- | ------ | ---------- |
| P2-1 | RBAC 权限模型统一 | 新增 ~35 项原子操作枚举，替代碎片化策略 |
| P2-2 | 知情同意 | 医案完成时增加「患者已知情同意」勾选 |
| P2-3 | 医案打印策略 | 新增 `DoctorOnly` 策略限定打印操作 |

---

## 四、权限设计原则

1. **层级管理**：上级管下级，不自管，不越级
   - Sysadmin → Admin（创建/编辑/禁用/删除/重置密码）
   - Admin → Doctor/Receptionist（创建/编辑/禁用/删除/重置密码）
   - Doctor/Receptionist → 无用户管理权限
2. **不可自管**：每个角色不可删除/禁用自己。Sysadmin 密码遗忘使用离线重置工具
3. **角色不可变更**：用户创建后角色固定，不支持升级/降级（避免权限追溯问题）
4. **职责分离**：药材管理（Admin 统一管库）与药材查询（Doctor/Receptionist 读取）分离
5. **操作级细分**：同一 Controller 的读/写操作可使用不同策略（Phase② 落地）
6. **sysadmin 特殊性**：绕过所有权限检查，但不可参与业务操作；联系方式在 About 页公开

## 五、数据管理规则

### 5.1 两字段模式（禁用 + 软删除）

系统不执行物理删除，所有数据通过标记位管理生命周期。**两个字段语义不同**：

| 字段 | 存储 | 语义 | 可逆性 | 典型场景 |
|------|------|------|--------|---------|
| `CommonStatus Status` | 枚举（Enabled=1/Disabled=0） | 临时停用 | 立即可逆（`ChangeStatus`） | 医生请假、前台调岗、季节性下架药材 |
| `IsDeleted` | 布尔（BaseEntity） | 永久归档 | Restore 审批流程 | 离职、停售药材、过期患者档案 |

**医疗行业依据**：HIPAA 要求 ePHI 可追溯不可丢，禁用和归档的审计级别不同——禁用只需记录操作，归档需记录归档原因 + 审批人。

> **医案取消语义（2026-08-03 决策）**：医案**取消 = 物理删除**（未完成医案 Active/Suspended 取消即物理删除，不判断是否有内容，无状态残留）；`IsDeleted` 软删除**仅用于管理员清理已完成医案**。医案无 `Status` 字段（只需 `CaseStatus` 管理生命周期）。详见 [07-medical-cases.md](../02-requirements/07-medical-cases.md) US-MC-014/015。

### 5.2 适用范围

| 实体类别 | 需要两字段？ | 实体 | 理由 |
| --------- | :----------: | ------ | ------ |
| **资源类** | ✅ | User, Herb, Formula, Patient | 可以临时停用（请假/下架），也可以归档（离职/停售） |
| **流程类** | ❌ | MedicalCase, Registration | 用业务状态枚举管理生命周期，无「启用/禁用」概念 |
| **从属类** | ❌ | Consultation, Prescription | 跟随父实体（医案）状态，无独立生命周期 |
| **审计类** | ❌ | AuditLog, PrintLog | 追加写入，永不修改/删除 |

### 5.3 实体状态字段映射

| 实体 | 禁用字段 | 归档字段 | 业务状态 |
| ------ | --------- | --------- | --------- |
| ApplicationUser | `CommonStatus Status` | `IsDeleted` | `UserRole` |
| HerbModel | `CommonStatus Status` | `IsDeleted` | — |
| FormulaModel | `CommonStatus Status` | `IsDeleted` | `FormulaValidationStatus` |
| PatientModel | `CommonStatus Status` | `IsDeleted` | — |
| MedicalCaseModel | — | `IsDeleted`（仅已完成医案软删，2026-08-03） | `MedicalCaseStatus` (Active/Suspended/Completed) |
| RegistrationModel | — | `IsDeleted` | `RegistrationStatus` (Waiting/InProgress/Completed/Cancelled) |
| ConsultationModel | — | `IsDeleted` | — |
| PrescriptionModel | — | — | — |

---

## 六、变更日志

| 日期 | 变更 |
| ------ | ------ |
| 2026-08-03 | v4.4 医案状态机注（§5 数据管理规则）：取消=物理删除、软删仅已完成、无 Status 字段 |
| 2026-08-03 | v4.3 权限边界更新（四角色需求审查）：Admin 挂号只读查看 + 打印记录查看；Doctor/Receptionist 患者删除/禁用 ❌；前台不涉及药材/验方（决策确认） |
| 2026-08-02 | §五 新增数据管理规则：两字段模式（禁用+软删除）定义、适用范围（资源类/流程类/从属类/审计类）、实体状态字段映射 |
| 2026-08-02 | v4.0 新建：从 02-personas.md 拆分；修正代码策略映射（实际代码与文档偏差）；增加 P0/P1/P2 分级 |
| 2026-06-28 | 初始权限矩阵（含在 personas 中） |
