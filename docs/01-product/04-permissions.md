# 权限矩阵与修复项 (Permissions)

> 版本: v4.0 | 日期: 2026-08-02 | 状态: 文档定义（设计态）

本文件定义四角色的权限矩阵、当前代码策略映射、已知问题与修复计划。

**图例**：✅ 已实现 | 📋 已设计未实现 | 🔴 未实现/缺失

> 角色定位与工作流程详见 [02-personas.md](02-personas.md)。
> 角色交接与协同流程详见 [05-role-interactions.md](05-role-interactions.md)。

---

## 一、统一权限矩阵

### 1.1 功能权限

| 操作 | Receptionist | Doctor | Admin | SuperAdmin |
|------|:-----------:|:------:|:-----:|:----------:|
| **用户管理** |||||
| 用户 CRUD | ✗ | ✗ | ✅（仅 Doctor/Receptionist） | ✅（仅 Admin） |
| 重置密码 | ✗ | ✗ | ✅（仅 Doctor/Receptionist） | ✅（仅 Admin） |
| 禁用/启用/删除 | ✗ | ✗ | ✅（仅 Doctor/Receptionist） | ✅（仅 Admin） |
| 角色变更 | ✗ | ✗ | ✗ | ✗ |
| **患者管理** |||||
| 患者 CRUD | ✅ | ✅ | ✅ | ✅ |
| 患者删除 | ✗ | ✅ | ✅ | ✅ |
| 患者启用/禁用 | ✗ | ✗ | ✅ | ✅ |
| **药材管理** |||||
| 药材查询 | ✗（前台不涉及药材） | ✅ | ✅ | ✅ |
| 药材创建/编辑 | ✗ | ✗（Admin 统一管库） | ✅ | ✅ |
| 药材删除 | ✗ | ✗ | ✅（需引用检查） | ✅ |
| 药材启用/禁用 | ✗ | ✗ | ✅ | ✅ |
| **验方管理** |||||
| 验方查询 | ✗ | ✅（自己 + 共享） | ✅ | ✅ |
| 验方创建/编辑 | ✗ | ✅（仅自己创建） | ✅ | ✅ |
| 验方导出/导入 | ✗ | 📋 | 📋 | 📋 |
| **医案管理** |||||
| 医案创建 | ✗ | ✅ **唯一** | ✗ | ✗ |
| 医案查看 | ✗ | ✅（仅自己的） | ✅（全部） | ✅ |
| 医案完成/关闭 | ✗ | ✅（仅自己的） | ✅（仅状态变更） | ✅ |
| 医案纠偏修改 | ✗ | ✗ | ✅（需填原因） | ✅ |
| **挂号管理** |||||
| 挂号创建 | ✅ | ✅ QuickVisit | ✗ | ✗ |
| 挂号取消 | ✅ | ✗ | ✗ | ✗ |
| **打印** |||||
| 处方打印 | ✗ | ✅ **唯一** | ✗ | ✗ |
| **报表** |||||
| 报表查看 | ✗ | ✅ | ✅ | ✅ |

### 1.2 会话超时策略

| 类型 | 值 | 说明 |
|------|:---:|------|
| 不活动超时 | **30 分钟** | 无操作 30 分钟自动退出 |
| 绝对超时 | **禁用**（v1.0） | 240 分钟绝对超时不启用，小诊所场景无此需求 |

---

## 二、当前代码策略映射

### 2.1 Controller 级授权策略

| Controller | 代码策略 | 目标策略 | 差异 |
|-----------|---------|---------|------|
| `RegistrationsController` | `DoctorOrAdminOrReceptionist` | `DoctorOrReceptionist` | ⚠️ 多了 Admin（Admin 不参与挂号） |
| `PatientsController` | `DoctorOrAdminOrReceptionist` | `DoctorOrReceptionist` | ⚠️ 多了 Admin（Admin 不直接管理患者） |
| `MedicalCasesController` | `DoctorOrAdmin` | `DoctorOnly` | ⚠️ 多了 Admin（Admin 不创建医案） |
| `HerbsController` | `DoctorOrReceptionist` | `AdminOrSuperAdmin` | 🔴 策略错误：应仅 Admin 管药材，Doctor/Receptionist 无写权限 |
| `FormulasController` | `DoctorOrReceptionist` | `DoctorOrAdmin` | 🔴 策略待细化 |

### 2.2 PolicyConstants 现有策略

| 策略名 | 包含角色 |
|--------|---------|
| `AdminOnly` | Admin |
| `DoctorOrAdmin` | Doctor, Admin |
| `AdminOrSuperAdmin` | Admin, SuperAdmin |
| `DoctorOrReceptionist` | Doctor, Receptionist |
| `DoctorOrAdminOrReceptionist` | Doctor, Admin, Receptionist |

> **Phase② 计划**：采用 RBAC + Permission 枚举方案（~35 项原子操作），替代当前分散在 Controller 策略/Service 层的碎片化权限逻辑。

---

## 三、已知权限问题与修复项

### 3.1 P0 必须修复（阻断核心流程）

| # | 问题 | Controller | 目标策略 | 修复方案 |
|---|------|-----------|---------|----------|
| P0-1 | Herbs 策略 `DoctorOrReceptionist` → Doctor/Receptionist 可写药材 | `HerbsController` | `AdminOrSuperAdmin`（写操作）；查询 `DoctorOrReceptionist`（读操作） | 按操作细分：GET `DoctorOrReceptionist`，POST/PUT/DELETE `AdminOrSuperAdmin` |
| P0-2 | MedicalCases `DoctorOrAdmin` → Admin 可创建医案 | `MedicalCasesController` | `DoctorOnly` | 新增 `DoctorOnly` 策略；Create 操作限定 Doctor |
| P0-3 | Formulas 策略 `DoctorOrReceptionist` → Receptionist 可写验方 | `FormulasController` | `DoctorOrAdmin`（写操作）；查询 `DoctorOrReceptionist`（读操作） | 按操作细分 |
| P0-4 | Herbs 删除无引用检查 → 可删除被处方引用的药材 | `HerbsService` | — | 增加引用检查（BR-DEL-001） |
| P0-5 | Patients 删除无引用检查 → 可删除被医案引用的患者 | `PatientsService` | — | 增加引用检查（BR-DEL-001） |

### 3.2 P1 重要（影响安全性/完整性）

| # | 问题 | 位置 | 修复方案 |
|---|------|------|----------|
| P1-1 | Formulas `GetDetail` 无所有权检查 → Admin 可读他人非共享验方 | `FormulasService` | 增加 `CreatedBy` 归属检查 |
| P1-2 | 医案打印回写缺失 | 医案模块 | 恢复 PrintLog 字段/实体 |
| P1-3 | 审计日志缺失 | SecurityAuditLog | 恢复审计日志记录 |
| P1-4 | Registrations 多了 Admin 策略 | `RegistrationsController` | 改为 `DoctorOrReceptionist`，QuickVisit 保持 `DoctorOrAdmin` |
| P1-5 | Patients 多了 Admin 策略 | `PatientsController` | 改为 `DoctorOrReceptionist` |

### 3.3 P2 增强（Phase②）

| # | 问题 | 修复方案 |
|---|------|----------|
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

---

## 五、变更日志

| 日期 | 变更 |
|------|------|
| 2026-08-02 | v4.0 新建：从 02-personas.md 拆分；修正代码策略映射（实际代码与文档偏差）；增加 P0/P1/P2 分级 |
| 2026-06-28 | 初始权限矩阵（含在 personas 中） |
