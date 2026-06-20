# 用户画像 (Personas)

> 版本: v3.1 | 日期: 2026-06-20 | 状态: 代码验证

本文件定义凌隐宝堂中医诊所管理系统的四个核心角色。所有功能映射均经过代码验证。

---

## 角色一：Sysadmin（系统运维 — 独立用户）

> **身份：独立用户（非角色）** | 授权：系统运维 | 使用频率低

### 定位

**sysadmin 不是一个角色，而是一个独立的系统用户。** 安装时自动创建，不可删除，独立于角色体系。负责平台正常运行。

### 设计要点

- 安装时自动创建，凭证在部署阶段确定
- 不可删除、不可禁用
- 不参与常规角色权限检查（CanManageUser 逻辑跳过 sysadmin）
- 认证方式：本地模式下自动登录，远程模式下密码登录
- 存储：独立标记字段（非 Role 枚举），与普通用户隔离

### 核心职责

- 系统部署与初始化（创建首个 Admin 账号）
- 系统配置管理（配置查询/验证）
- 运行时诊断（健康检查、日志级别、调试模式）
- 数据库维护（迁移执行与验证）
- 紧急故障排查

### 当前实现状态

已完成（commit `810c06c01`）：
- `ApplicationUser.IsSysAdmin` 布尔字段
- `SuperAdminRoleDefinition` 已删除
- `CanManageUser` 使用 `IsSysAdmin` 判断
- JWT Claims 包含 `IsSysAdmin=true`
- 种子数据自动创建 sysadmin（IsSysAdmin=true）

### 默认用户

| 用户 | 用户名 | 默认密码 | 角色 | IsSysAdmin | 可删除 |
|------|--------|---------|------|-----------|--------|
| 业务管理员 | `admin` | `Admin@123456` | Admin | **false** | ✅ |
| 系统运维 | `sysadmin` | `SysAdmin@2026!` | SuperAdmin | **true** | ❌ |

> ⚠️ admin 和 sysadmin 是**两个独立用户**，不可混淆。sysadmin 是信任根——创建第一个 admin，可重置 admin 密码。admin 只能由 sysadmin 创建。

---

## 角色二：Admin（业务管理员）

> **PermissionLevel = 10** | 授权策略：`DoctorOrReceptionist` + `AdminOrSuperAdmin`

### 定位

负责中医诊所业务管理的管理员。主要工作是维护业务基础数据和管理用户账号。

### 代码实现

| 配置项 | 值 | 代码位置 |
|--------|-----|---------|
| `UserRole` | `Admin(10)` | `AdminRoleDefinition.cs:28` |
| `HomeViewName` | `ViewNames.AdminHome` | `AdminRoleDefinition.cs:36` |
| `RequiredModules` | UsersModule, PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule | `AdminRoleDefinition.cs:17-22` |
| `CanManageUser` 逻辑 | Admin 可管理 Doctor/Receptionist，不可管理 Admin/SuperAdmin | `UsersController.cs` 内联 |

### 核心职责

- **用户管理**：创建用户、分配角色、启用/禁用账号
- **药材管理**：药材信息录入与维护、价格更新、批量导入
- **验方管理**：经验方录入、验证状态管理
- **医案审核**：查看全局医案，审核异常处方
- **业务配置**：与中医看诊相关的系统配置

---

## 角色三：Doctor（中医医生）

> **PermissionLevel = 1** | 授权策略：`DoctorOrReceptionist`

### 定位

中医内科主治医师，核心业务是看诊。偶尔在看台或特殊情况下需要代为挂号。

### 代码实现

| 配置项 | 值 | 代码位置 |
|--------|-----|---------|
| `UserRole` | `Doctor(1)` | `DoctorRoleDefinition.cs:28` |
| `HomeViewName` | `ViewNames.ClinicalWorkspace` | `DoctorRoleDefinition.cs:37` |
| `RequiredModules` | UsersModule, PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule, **RegistrationModule** | `DoctorRoleDefinition.cs:16-24` |
| 专属 UI | ClinicalWorkspaceView（患者列表+看诊工作区一体化） | `Roles/LYBT.Desktop.Clinical/Views/` |
| 医案创建权限 | 仅 Doctor 可创建（UsersController.Create 的 CanManageUser 逻辑） | `UsersController.cs:441` |

### 核心职责

- **看诊**（主要）：望闻问切 → 辨证论治 → 开方 → 打印处方
- **验方管理**：管理自己创建的经验方
- **患者管理**：查看和编辑自己负责的患者信息
- **偶尔挂号**：DoctorRoleDefinition 加载了 RegistrationModule，在前台繁忙时可代替挂号

---

## 角色四：Receptionist（前台）

> **PermissionLevel = 0** | 授权策略：`DoctorOrReceptionist`

### 定位

前台接待人员，专注患者挂号和相关信息维护。

### 代码实现

| 配置项 | 值 | 代码位置 |
|--------|-----|---------|
| `UserRole` | `Receptionist(0)` | `ReceptionistRoleDefinition.cs:27` |
| `HomeViewName` | `ViewNames.ReceptionistHome` | `ReceptionistRoleDefinition.cs:34` |
| `RequiredModules` | **仅 3 个**：UsersModule, PatientsModule, RegistrationModule | `ReceptionistRoleDefinition.cs:18-21` |
| 专属 UI | ReceptionistHomeView（挂号队列+快捷操作） | `Roles/LYBT.Desktop.Receptionist/Views/` |

### 核心职责

- **患者挂号**：新患者登记、老患者挂号（刷卡或手动搜索）
- **患者信息维护**：录入和更新患者基本信息
- **就诊引导**：告知患者就诊流程和注意事项

---

## 架构层差异汇总

| 维度 | Sysadmin | Admin | Doctor | Receptionist |
|------|----------|-------|--------|-------------|
| **身份类型** | **独立用户** | 角色 | 角色 | 角色 |
| **模块数** | 5（全量） | 5 | **6** | **3** |
| **包含 RegistrationModule** | ❌ | ❌ | **✅** | **✅** |
| **包含 HerbsModule** | ✅ | ✅ | ✅ | ❌ |
| **包含 FormulaModule** | ✅ | ✅ | ✅ | ❌ |
| **包含 MedicalCaseModule** | ✅ | ✅ | ✅ | ❌ |
| **首页视图** | AdminHome | AdminHome | **ClinicalWorkspace** | ReceptionistHome |
| **授权策略** | 跳过角色检查 | DoctorOrReceptionist + AdminOrSuperAdmin | 仅 DoctorOrReceptionist | 仅 DoctorOrReceptionist |
| **CanManageUser** | 跳过 | 可管理 Doctor/Receptionist | 不可管理 | 不可管理 |
| **可删除** | ❌ | ✅ | ✅ | ✅ |
| **可禁用** | ❌ | ✅ | ✅ | ✅ |

---

## 变更日志

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-20 | v3.2 Sysadmin 改为独立用户设计 | 行业标准：sysadmin 是用户而非角色 |
| 2026-06-20 | v3.1 增加代码实现列 | 用户要求结合代码验证角色定位 |
| 2026-06-20 | v3.0 重构角色定义 | 明确各角色定位 |
| 2026-06-15 | v2.0 重建 | Phase 1 简化后重建 |
