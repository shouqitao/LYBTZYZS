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

> **SuperAdmin vs Sysadmin 双列语义说明**（审计 S2 澄清，2026-06-28）：
> 矩阵保留两列不合并，因二者是**不同机制**：
> - **SuperAdmin（角色列）**= 角色 PermissionLevel=100，通过角色放行获得权限（角色体系内的最高权限）
> - **Sysadmin（独立用户列）**= `IsSysAdmin=true` 布尔标记，提供**不可删除/禁用/修改**的额外保护（独立于角色体系）
> - **并存关系**：sysadmin 用户默认即被赋予 SuperAdmin 角色（见 `IdentitySeedData`），即「SuperAdmin 角色获权 + IsSysAdmin 布尔提供保护」双机制叠加
> - 两列在「资源×操作」矩阵中几乎全 ✅ 重叠属**设计预期**（sysadmin 经 SuperAdmin 角色获权），而非冗余

## Resource × Operation × Role Matrix

| 资源 | 操作 | Receptionist | Doctor | Admin | SuperAdmin | Sysadmin |
|------|------|:---:|:---:|:---:|:---:|:---:|
| 患者 | 查看 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 患者 | 创建/编辑 | ✅ | ✅ | ✅ | ✅ | ✅ |
| 患者 | 删除(软) | ❌ | ❌ | ✅ | ✅ | ✅ |
| 患者 | 恢复 | ❌ | ❌ | ❌ | ✅ | ✅ |
| 挂号 | 创建 | ✅ | ✅(QuickVisit) | ❌ | ❌ | ❌ |
| 挂号 | 取消 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 医案 | 查看 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 医案 | 创建 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 医案 | 编辑 | ❌ | ✅(当天) | ❌ | ✅(EditReason) | ✅ |
| 医案 | 完成 | ❌ | ✅ | ❌ | ❌ | ❌ |
| 医案 | 审计日志 | ❌ | ✅(自己) | ✅(全部) | ✅(全部) | ✅(全部) |
| 处方 | 打印 | ❌ | ✅ | ❌ | ❌ | ❌ |
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

> **已知 Bug（D7/D8 决策）**：挂号/患者/药材/医案创建的当前代码策略与矩阵不一致（详见下方「代码待对齐清单」）。文档保留**目标态**，代码修复由 D7 跟踪。

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

> 与 [`PolicyConstants`](../../src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs) 一致 —— 实有 **4 项**。**`DoctorOnly` 为目标策略，`PolicyConstants` 待新增**（医案创建 Doctor 唯一、打印权限强制需要）。

| Policy | 常量 | 要求角色 | 用途 |
|--------|------|----------|------|
| `DoctorOrReceptionist` | `PolicyConstants.DoctorOrReceptionist` | SuperAdmin / Admin / Doctor / Receptionist | 患者、验方、挂号（**目标态**，D7 待对齐） |
| `DoctorOrAdmin` | `PolicyConstants.DoctorOrAdmin` | SuperAdmin / Admin / Doctor | **代码当前最常用策略**（挂号/患者/药材/医案创建当前均用此策略） |
| `AdminOnly` | `PolicyConstants.AdminOnly` | SuperAdmin / Admin | 管理员级操作 |
| `AdminOrSuperAdmin` | `PolicyConstants.AdminOrSuperAdmin` | SuperAdmin / Admin | 用户管理、系统配置（与 `AdminOnly` 行为等价，命名历史并存） |
| `DoctorOnly` ⏳ | `PolicyConstants.DoctorOnly`（**待新增**） | Doctor | **目标策略**：医案创建（Doctor 唯一）、处方打印强制。代码当前无此策略（⚠️ D7 待对齐） |
| `FallbackPolicy` | （`RequireAuthenticatedUser`） | 任何已登录用户 | 默认策略，所有未显式标注 Policy 的端点 |

> 📌 **`AdminOnly` ≡ `AdminOrSuperAdmin` 合并建议**（审计 S6，2026-06-28）：两者行为完全等价，建议 Phase② 合并为单一策略。

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
| **K5** | 💻 | **种子矛盾**：`IdentitySeedData` 同时种子 admin + sysadmin，违反"只种子 sysadmin"设计（admin 应由 US-SHELL-011 向导 Step4 创建） | `IdentitySeedData.cs:26-27` | 删 admin 种子，admin 改由向导创建 |

### P1 严重（角色边界正确性）

| # | 类型 | 问题 | 代码位置 | 修复方向 |
|---|------|------|---------|---------|
| **K7** | 💻 | **接诊链断裂（D8 bug）**：`StartVisit` 仅调 `StartVisitAsync` 不建医案 + 返回 RegistrationId 冒充 MedicalCaseId。R10 spec S5 要求原子创建 MedicalCase(Active)+Registration(InProgress)+返回 MedicalCaseId | `RegistrationsController.cs:200` | StartVisit 改原子创建医案 |
| **K8** | 💻 | **LocalWebAPI 权限策略空缺**：`LocalWebAPI/Controllers/{Registrations,Patients,Herbs,MedicalCases}.cs` 仅 `[Authorize]` 无 Policy，本地 Doctor 可删患者/药材 CRUD，违本文档矩阵。R10 S3"本地全角色支持"↔Flow 3"本地无角色检查"矛盾 | `LocalWebAPI/Controllers/*.cs` | 明确本地是否启用角色策略（建议与远程一致+角色策略） |
| **K9** | 💻 | **接诊 Cancel 权限三向倒置**：`Cancel` XML 注释称"仅 Receptionist 可操作"，但无操作级 `[Authorize]`，回落类级 `DoctorOrAdmin`：前台被挡、Doctor/Admin 反被放行。与本文档矩阵（Receptionist✅/Doctor❌/Admin❌）三向倒置 | `RegistrationsController.cs:216-220` | 补操作级 `[Authorize(Policy=...)]` |
| **K3** | 💻 | `PolicyConstants` 缺 `DoctorOnly`：本文档"医案创建 Doctor 唯一"目标无策略可执行（`PolicyConstants.cs` 仅 4 项）。`personas` "唯一创建者"+"DoctorOrAdmin 策略"自相矛盾（该策略含 Admin） | `PolicyConstants.cs` | 新增 `DoctorOnly` 常量，或服务层 `CreatedBy==currentUser` 归属校验兜底 |

> 完整问题清单（含 I1-I10 重要问题、S1-S10 次要问题）见 [审计报告全文](../compose/reports/2026-06-28-role-driven-audit.md)。

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-28 | v1.2 | 审计 S2/S6/K1-K9 文档标注：SuperAdmin/Sysadmin 双列语义说明；`AdminOnly`≡`AdminOrSuperAdmin` 合并建议；新增「代码待对齐清单」段（K1/K3/K4/K5/K7/K8/K9） | 角色驱动审计报告 S 类清理 + K 类代码待修项文档标注 |
| 2026-06-28 | v1.1 | 权限矩阵统一（权威决策 2026-06-28）：挂号创建 Doctor✅(QuickVisit)/Admin✗、挂号取消 Admin✗、药材删除 Admin✅(D5)、用户重置密码 Admin✅；D7 脚注与 Authorization Policies 段标注 `DoctorOnly` 为目标策略待新增 | 三文档（personas/matrix/代码）矛盾收敛，以 personas+权威决策为准 |
| 2026-06-28 | v1.0 | 结构治理：修正 `PolicyConstants` 与 baseline 链接相对路径（多余的 `../`）；补充变更记录段 |
