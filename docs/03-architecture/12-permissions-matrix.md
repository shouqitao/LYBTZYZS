# Permissions — 权限矩阵

## Roles

| 角色 | PermissionLevel | 说明 |
|------|:---:|------|
| Receptionist | 0 | 前台，负责挂号、患者管理 |
| Doctor | 1 | 医生，负责诊断、开方 |
| Admin | 10 | 管理员，负责用户、药材、验方管理 |
| SuperAdmin | 100 | 超级管理员，系统设置、全局管理 |
| Sysadmin | — | 独立用户（IsSysAdmin=true），非角色，运维 |

**Sysadmin 特殊性**：
- `ApplicationUser.IsSysAdmin = true`
- 跳过角色检查
- 不可被删除/禁用/修改
- 默认凭证：`sysadmin/SysAdmin@2026!`

## Resource × Operation × Role Matrix

| 资源 | 操作 | Receptionist | Doctor | Admin | SuperAdmin | Sysadmin |
|------|------|:---:|:---:|:---:|:---:|:---:|
| 患者 | 查看 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 患者 | 创建/编辑 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 患者 | 删除(软) | ❌ | ❌ | ✅ | ✅ | ✅ |
| 患者 | 恢复 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 挂号 | 创建 | ✅ | ❌ | ✅ | ✅ | ✅ |
| 挂号 | 取消 | ✅ | ❌ | ✅ | ✅ | ✅ |
| 医案 | 查看 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 医案 | 创建 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 医案 | 编辑 | ❌ | ✅(当天) | ❌ | ✅(EditReason) | ✅ |
| 医案 | 完成 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 医案 | 审计日志 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 处方 | 打印 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 处方 | 回写 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 药材 | 查看 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 药材 | 创建/编辑 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 药材 | 删除 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 验方 | 查看 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 验方 | 创建/编辑 | ❌ | ✅ | ✅ | ✅ | ✅ |
| 用户 | 查看 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 用户 | 创建/编辑 | ❌ | ❌ | ✅ | ✅ | ✅ |
| 用户 | 删除/禁用 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 用户 | 重置密码 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 系统设置 | 查看/修改 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 审计日志 | 查看 | ❌ | ❌ | ❌ | ✅ | ✅ |

> **已知 Bug（D7/D8 决策）**：
> - 挂号创建/取消：当前代码 `DoctorOrAdmin` 挡住 Receptionist → 待修复为 `DoctorOrReceptionist`
> - 患者 CRUD：当前代码 `DoctorOrAdmin` 挡住 Receptionist → 待修复为 `DoctorOrReceptionist`
> - 药材 CRUD：当前代码 `DoctorOrAdmin` 挡住 Receptionist → 待修复为 `DoctorOrReceptionist`
> - 医案创建：当前代码 `DoctorOrAdmin` 允许 Admin → 文档目标 `DoctorOrReceptionist`（**注**：文档无 `DoctorOnly` 策略，历史表述已修正）
>
> 文档保留**目标态**；代码修复由 D7 跟踪（见 [baseline §3](../compose/specs/2026-06-28-docs-reconciliation-baseline.md)）。

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

> 与 [`PolicyConstants`](../../src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs) 一致 —— 实有 **4 项**，**无 `DoctorOnly`**（历史文档曾提及，已删除）。

| Policy | 常量 | 要求角色 | 用途 |
|--------|------|----------|------|
| `DoctorOrReceptionist` | `PolicyConstants.DoctorOrReceptionist` | SuperAdmin / Admin / Doctor / Receptionist | 患者、药材、验方、挂号（**目标态**，见 D7 待对齐） |
| `DoctorOrAdmin` | `PolicyConstants.DoctorOrAdmin` | SuperAdmin / Admin / Doctor | **代码当前最常用策略**（挂号/患者/药材/医案创建当前均用此策略） |
| `AdminOnly` | `PolicyConstants.AdminOnly` | SuperAdmin / Admin | 管理员级操作 |
| `AdminOrSuperAdmin` | `PolicyConstants.AdminOrSuperAdmin` | SuperAdmin / Admin | 用户管理、系统配置（与 `AdminOnly` 行为等价，命名历史并存） |
| `FallbackPolicy` | （`RequireAuthenticatedUser`） | 任何已登录用户 | 默认策略，所有未显式标注 Policy 的端点 |

> ⚠️ **D7 待对齐**（详见 [baseline §3](../compose/specs/2026-06-28-docs-reconciliation-baseline.md)）：挂号/患者/药材/医案创建的**目标 Policy 为 `DoctorOrReceptionist`**，代码当前为 `DoctorOrAdmin`。文档保留目标态，代码修复由 D7 跟踪。

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-28 | v1.0 | 结构治理：修正 `PolicyConstants` 与 baseline 链接相对路径（多余的 `../`）；补充变更记录段 |
