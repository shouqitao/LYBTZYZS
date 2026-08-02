# 文档校准报告

> 扫描日期: 2026-08-02 | 分支: master | 代码基线: latest

## 校准摘要

- 扫描模块数: 12（Auth, Users, Patients, Herbs, Formulas, MedicalCases, Registrations, Reports, Configuration, Diagnostics, Deploy, Health）
- 发现差距数: 14
- A 类（代码已修复/变更，文档未更新）: 7 项
- B 类（文档正确，代码未实现）: 0 项
- C 类（两者都有问题，需确认）: 4 项
- D 类（文档描述过时/缺失）: 3 项

---

## 逐模块差距清单

### 1. PolicyConstants（权限策略常量）

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 1 | 12-permissions-matrix.md:72-78 | 4 项策略 + DoctorOnly⏳待新增 | 5 项: AdminOnly, DoctorOrAdmin, AdminOrSuperAdmin, DoctorOrReceptionist, **DoctorOrAdminOrReceptionist** | **A** | 更新文档：新增 DoctorOrAdminOrReceptionist 说明；DoctorOnly 仍标记待新增 |
| 2 | 03-glossary.md:83-90 | AdminOnly ≡ AdminOrSuperAdmin（合并建议） | 两者仍独立存在 | **D** | 更新术语表：移除合并建议或标注为 Phase 2 目标 |

### 2. Patients 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 3 | 12-permissions-matrix.md:30-31 | 患者创建/编辑: Receptionist✅ Doctor✅ Admin✅ SuperAdmin✅ | 类级策略 `DoctorOrAdminOrReceptionist`（4角色均可） | **A** | 矩阵已正确（含SuperAdmin✅），但需补注代码策略名 `DoctorOrAdminOrReceptionist` |
| 4 | 12-permissions-matrix.md:32 | 患者删除: Admin✅ SuperAdmin✅ Sysadmin✅（Doctor❌ Receptionist❌） | 代码 Delete 无额外策略覆盖，回退类级 `DoctorOrAdminOrReceptionist`（**Doctor 也可删**） | **C** | 代码删除策略需确认：是否允许 Doctor 删除患者？如不允许需补 `[Authorize(Policy=AdminOrSuperAdmin)]` |

### 3. Herbs 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 5 | 12-permissions-matrix.md:43-45 | 药材创建/编辑: Admin✅ SuperAdmin✅（Doctor❌ Receptionist❌） | 代码 Create/Update 类级 `DoctorOrReceptionist`（**Doctor 也可创建/编辑**） | **C** | 需确认：Doctor 是否应有药材创建/编辑权限？如是，更新矩阵 |
| 6 | 05-herbs.md:37 | US-HERB-001 角色: 医生/管理员 | 代码端点 DoctorOrReceptionist（含Receptionist） | **A** | 更新 US-HERB-001 角色描述：前台/医生/管理员 |

### 4. Registrations 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 7 | 12-permissions-matrix.md:34-35 | 挂号创建: Receptionist✅ Doctor(QuickVisit)✅ Admin❌ SuperAdmin❌ | 代码 Create 类级 `DoctorOrAdminOrReceptionist`（**Admin 也可创建**） | **C** | 需确认：Admin 是否应有挂号创建权限？当前代码允许 |
| 8 | 12-permissions-matrix.md:35 | 挂号取消: Receptionist✅（Doctor❌ Admin❌ SuperAdmin❌） | 代码 Cancel 类级 `DoctorOrAdminOrReceptionist`（**Doctor/Admin/SuperAdmin 也可取消**） | **C** | 已知 Bug K9（文档已标注），需代码修复或更新文档 |

### 5. MedicalCases 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 9 | 12-permissions-matrix.md:37 | 医案创建: Doctor✅（Admin❌ SuperAdmin❌ Receptionist❌） | 代码 Create 策略 `DoctorOrAdminOrReceptionist`（Receptionist **也可创建**） | **A** | 更新矩阵：Receptionist 创建医案 ✅（代码已扩展，允许前台代建） |

### 6. Reports 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 10 | 10-reports.md:27-28 | US-REPORT-001/002/003 状态: 🚧 v1.0 待实现（startDate/endDate 代码未实现） | 代码 ReportsController **已实现** startDate/endDate 参数 | **A** | 更新 US 状态为 ✅ 已实现 |

### 7. Auth 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 11 | 02-auth.md:59 | 登出允许匿名: `[AllowAnonymous]` | 代码 LogoutAsync 有 `[AllowAnonymous]` ✅ | — | 无需修改 |

### 8. Users 模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 12 | 03-users.md:73 | 修改密码: AdminOrSuperAdmin（IDOR 防护） | 代码 ChangePassword 仅有类级 `[Authorize]`，无显式 AdminOrSuperAdmin 策略 | **A** | 更新文档：修改密码端点策略为类级认证（非 AdminOrSuperAdmin），依赖 IDOR 防护 |

### 9. 缺失文档模块

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 13 | 无 | Configuration/Diagnostics/Deploy/Health 无独立需求文档 | 代码有完整端点实现，策略 AdminOrSuperAdmin | **D** | 在 01-prd.md 或独立文档中补录这 4 个运维模块的 API 说明 |

### 10. 批量操作一致性

| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 14 | 多个模块文档 | batch-enable/batch-disable 策略未明确文档化 | 代码 Herbs/Formulas/Users 的 batch-enable/batch-disable 统一使用 `AdminOrSuperAdmin` | **A** | 在各模块文档中补充 batch-enable/disable 策略说明 |

---

## 枚举值一致性检查

| 枚举 | 代码值 | 术语表值 | 结果 |
|------|--------|---------|------|
| MedicalCaseStatus | Suspended=0, Active=1, Completed=2 | Suspended=0, Active=1, Completed=2 | ✅ 一致 |
| RegistrationStatus | Waiting=0, InProgress=1, Completed=2, Cancelled=3 | Waiting=0, InProgress=1, Completed=2, Cancelled=3 | ✅ 一致 |
| FormulaValidationStatus | Draft=0, Validated=1 | Draft=0, Validated=1 | ✅ 一致 |
| UserRole | Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100 | 同左 | ✅ 一致 |
| CommonStatus | Disabled=0, Enabled=1 | 术语表未单独列出 | ⚠️ 建议补充 |
| FormulaType | Classic=1, Experience=2 | 术语表未列出 | ⚠️ 建议补充 |
| HerbRole | None=0, Sovereign=1, Minister=2, Assistant=3, Guide=4 | 术语表未列出 | ⚠️ 建议补充 |

---

## 权限策略实际 vs 文档 对照表

| Controller | 代码策略 | 文档策略 | 差距 |
|-----------|---------|---------|------|
| Auth | `[Authorize]` + `[AllowAnonymous]` | AllowAnonymous for login/logout/validate | ✅ 一致 |
| Users | AdminOrSuperAdmin (CRUD) | AdminOrSuperAdmin | ✅ 一致 |
| Patients | DoctorOrAdminOrReceptionist | DoctorOrReceptionist（矩阵含4角色） | ⚠️ 策略名不同但语义需确认 |
| Herbs | DoctorOrReceptionist (CRUD) | DoctorOrReceptionist | ✅ 一致 |
| Formulas | DoctorOrReceptionist (CRUD) | DoctorOrReceptionist | ✅ 一致 |
| MedicalCases | DoctorOrAdmin (列表) + DoctorOrAdminOrReceptionist (创建) | DoctorOnly（目标）/ DoctorOrAdmin（当前） | ⚠️ 已知差距 |
| Registrations | DoctorOrAdminOrReceptionist | Receptionist + Doctor(QuickVisit) | ⚠️ 已知差距 K9 |
| Reports | DoctorOrAdmin | DoctorOrAdmin | ✅ 一致 |
| Configuration | AdminOrSuperAdmin | 无文档 | ❌ 缺文档 |
| Diagnostics | AdminOrSuperAdmin | 无文档 | ❌ 缺文档 |
| Deploy | AdminOrSuperAdmin | 无文档 | ❌ 缺文档 |
| Health | `[Authorize]` + `[AllowAnonymous]`(基础) | 无文档 | ❌ 缺文档 |

---

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-08-02 | 初始校准报告 | 文档校准任务启动 |
