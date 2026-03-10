# 角色权限与数据归属关系

> **版本**: v1.0
> **创建日期**: 2026-03-10
> **定位**: 跨模块权限汇总文档，统一描述系统中角色、授权策略、数据归属、可见性和跨模块联动规则
> **来源**: 从 auth.md / users.md / herbs.md / formulas.md / medical-cases.md / patients.md / registration.md 各模块 PRD 的 Section 2 (Target Users) + Business Rules + Decision Log 汇总提取

---

## 1. 角色体系

### 1.1 角色定义

| 角色 | 权限值 | 定位 | 数量 |
|------|--------|------|------|
| SuperAdmin (sysadmin) | 100 | 系统唯一固定账号，数据库种子预置 | 1 (固定) |
| Admin | 80 | 诊所管理员，管理用户/数据/配置 | 1-2 |
| Doctor | 60 | 医生，核心诊疗操作者 | 1-5 |
| Receptionist | 40 | 前台，患者登记和挂号 | 1-2 |

> 权限值层级模型 (USER-D04): `operator.PermissionLevel > target.PermissionLevel` -> 允许操作

### 1.2 SuperAdmin 特殊规则 (USER-D05)

| 规则 | 说明 |
|------|------|
| 作为操作者 | 拥有 Admin 全部权限，可创建/管理所有低权限用户 |
| 作为目标 | **不可被任何人管理** -- 不可修改角色、不可删除、不可禁用、不可重置密码 |
| 自助操作 | 仅可通过 /profile 和 /change-password 修改自己的个人信息 |
| 可见性 | Admin 用户管理列表中不可见 (权限值过滤) |
| 硬规则 | API 层兜底: 任何以 sysadmin 为目标的用户管理操作一律拒绝 |

---

## 2. API 授权策略

### 2.1 策略矩阵

| 策略 | 允许角色 | 适用端点 | 来源 |
|------|---------|---------|------|
| AllowAnonymous | 所有人 (含未认证) | POST /auth/login, /auth/logout, /auth/refresh-token; GET /health; GET /herbs/import-template; GET /formulas/import-template; GET /patients/import-template | auth.md, herbs.md, formulas.md, patients.md |
| Authenticated (任意已认证) | SuperAdmin + Admin + Doctor + Receptionist | GET /users/current; PUT /users/{id}/change-password; PUT /users/{id}/profile; GET /auth/validate | auth.md, users.md |
| PatientAccess | SuperAdmin + Admin + Doctor + Receptionist | GET/POST/PUT /patients; GET/POST /registrations | patients.md, registration.md |
| DoctorOrAdmin | SuperAdmin + Admin + Doctor | /herbs, /formulas, /medicalcases, /sync | herbs.md, formulas.md, medical-cases.md, sync.md |
| AdminOnly | SuperAdmin + Admin | /users (CRUD) | users.md |
| SuperAdminOnly | SuperAdmin | POST /users/{id}/reset-password; POST /users/{id}/restore; GET /diagnostics | users.md, health-diagnostics.md |
| Roles=Doctor | 仅 Doctor | POST /medicalcases (创建医案) | medical-cases.md US-MC-001 |

### 2.2 策略优先级

```
请求进入
  ├─ [AllowAnonymous] → 放行
  ├─ 无 Token → 401 Unauthorized
  ├─ Token 有效 → 检查角色策略
  │   ├─ 角色匹配 → 放行 (进入资源级权限检查)
  │   └─ 角色不匹配 → 403 Forbidden
  └─ Token 过期 → 401 (客户端触发 refresh)
```

> 策略是"门禁"，通过门禁后还有**资源级权限检查** (所有权、权限值等)。

---

## 3. 数据归属规则

### 3.1 归属检查总览

> **统一原则**: Admin/SuperAdmin 可操作全部数据; Doctor 仅可操作自己创建的数据 (写操作); Receptionist 无写操作或受限写操作。

| 模块 | 归属字段 | 归属检查范围 | 错误码 |
|------|---------|-------------|--------|
| Herb (药材) | BaseEntity.CreatedBy | Update / Delete / ToggleStatus / Restore | ERR-50103 HerbNoPermission |
| Formula (验方) | Formula.UserId + CreatedBy | Update / Delete / ToggleStatus | ERR-60103 FormulaNoPermission |
| MedicalCase (医案) | MedicalCase.UserId | Update / Suspend / Cancel / Complete | ERR-30201 CannotEditCase |
| Patient (患者) | 无归属限制 | 所有 PatientAccess 角色均可 CRU | - |
| User (用户) | 权限值层级 (USER-D04) | 所有 CRUD 操作 | "您没有权限执行此操作" |
| Registration (挂号) | Registration.Source | Cancel (仅 Source=Receptionist 的可被前台取消) | REG-70004 UnauthorizedCancel |

### 3.2 各模块归属详情

#### 药材 (Herb)

| 操作 | SuperAdmin | Admin | Doctor | Receptionist |
|------|-----------|-------|--------|-------------|
| 创建 | 可以 | 可以 | 可以 | 403 (DoctorOrAdmin) |
| 查看列表/详情 | 全部 | 全部 | 全部 | 403 (DoctorOrAdmin) |
| 更新 | 全部 | 全部 | **仅自己创建的** | 403 |
| 删除 | 全部 (引用检查) | 全部 (引用检查) | **仅自己创建的** (引用检查) | 403 |
| 启用/禁用 | 全部 | 全部 | **仅自己创建的** | 403 |

> 来源: herbs.md Section 2, US-HERB-004/005/006

#### 验方 (Formula)

| 操作 | SuperAdmin | Admin | Doctor | Receptionist |
|------|-----------|-------|--------|-------------|
| 创建 | 可以 | 可以 | 可以 | 403 (DoctorOrAdmin) |
| 查看列表 | **全部** | **全部** | **CreatedBy=自己 OR IsShared=true** | 403 |
| 查看详情 | 全部 | 全部 | 自己的 + 共享的 | 403 |
| 更新 | 全部 | 全部 (含他人共享) | **仅自己创建的** | 403 |
| 删除 | 全部 | 全部 | **仅自己创建的** | 403 |
| 启用/禁用 | 全部 | 全部 | **仅自己创建的** | 403 |

> 来源: formulas.md Section 2, US-FORM-002/004/005/006/008

#### 医案 (MedicalCase)

| 操作 | SuperAdmin | Admin | Doctor | Receptionist |
|------|-----------|-------|--------|-------------|
| 创建 | **403** (仅 Doctor) | **403** (仅 Doctor) | 可以 (Roles=Doctor) | 403 |
| 查看列表 | 全部 | 全部 | **仅 UserId=自己的** | 简要提示 (时间+医生) |
| 编辑 (未完成) | 全部 | 全部 | **仅自己的** Active/Suspended | 403 |
| 编辑 (已完成) | 需 EditReason | 需 EditReason | 403 (Doctor 不可编辑已完成) | 403 |
| 完成/挂起/取消 | 全部 | 全部 | **仅自己的** | 403 |

> 来源: medical-cases.md Section 2, US-MC-001/005/009/013

#### 患者 (Patient)

| 操作 | SuperAdmin | Admin | Doctor | Receptionist |
|------|-----------|-------|--------|-------------|
| 创建 | 可以 | 可以 | 可以 | 可以 |
| 查看列表 | 全部 (含禁用) | 全部 (含禁用) | 全部 (含禁用, 标注状态) | **自动过滤禁用患者** |
| 查看详情 | 全部 | 全部 | 全部 | 全部 |
| 更新 | 全部 | 全部 | 全部 | 全部 |
| 删除 | 全部 (引用检查) | 全部 (引用检查) | 全部 (引用检查) | **403 (无删除权限)** |
| 启用/禁用 | 可以 | 可以 | **403** | 403 |

> 来源: patients.md Section 2, US-PAT-002/005/013

#### 用户 (User)

| 操作 | SuperAdmin | Admin | Doctor | Receptionist |
|------|-----------|-------|--------|-------------|
| 创建 | Admin/Doctor/Receptionist | Doctor/Receptionist | 403 (AdminOnly) | 403 |
| 查看列表 | Admin + Doctor + Receptionist (过滤自己) | Doctor + Receptionist (过滤自己 + sysadmin + 其他Admin) | 403 | 403 |
| 编辑 | 权限值低于自己的用户 | 权限值低于自己的用户 | 403 | 403 |
| 删除 | 权限值低于自己的用户 | 权限值低于自己的用户 | 403 | 403 |
| 自助 (改密码/资料) | 自己 | 自己 | 自己 | 自己 |

> 来源: users.md Section 2, US-USER-001~005, USER-D04/D05

#### 挂号 (Registration)

| 操作 | SuperAdmin | Admin | Doctor | Receptionist |
|------|-----------|-------|--------|-------------|
| 创建 (前台模式) | - | - | - | Source=Receptionist, Status=Waiting |
| 创建 (医生模式) | - | - | Source=Doctor, Status=InProgress (静默) | - |
| 查看队列 | 全部 (只读) | 全部 (只读) | **仅个人队列** (DoctorId=自己) | 全部 (只读) |
| 接诊 (开始看诊) | - | - | 从队列选择 | - |
| 取消 | - | - | **403** (无权取消) | 仅 Source=Receptionist 且 Status=Waiting |

> 来源: registration.md Section 2, US-REG-001~004

---

## 4. 共享机制

### 4.1 验方共享 (Formula.IsShared)

> 来源: formulas.md US-FORM-008

**共享规则:**

| 属性 | 说明 |
|------|------|
| 字段 | `Formula.IsShared` (bool, 默认 false) |
| 设置方式 | 通过 PUT /formulas/{id} 修改 IsShared 字段 |
| 谁可以设置 | 验方创建者本人 (Doctor) 或 Admin/SuperAdmin |

**共享后的可见性:**

| 创建者 | IsShared | Admin 可见 | 创建者 Doctor 可见 | 其他 Doctor 可见 |
|--------|----------|-----------|-------------------|-----------------|
| Admin | false | 可见 | 不可见 | 不可见 |
| Admin | true | 可见 | **可见 (只读)** | **可见 (只读)** |
| Doctor A | false | 可见 | 可见 (可编辑) | 不可见 |
| Doctor A | true | 可见 (可编辑) | 可见 (可编辑) | **可见 (只读)** |

**共享后的编辑权限:**

| 操作者 | 对共享验方的权限 |
|--------|----------------|
| 创建者 Doctor | 可编辑 (所有权不因共享而改变) |
| 其他 Doctor | **只读** (编辑返回 403 ERR-60103) |
| Admin/SuperAdmin | **可编辑** (US-FORM-008 BR-3: Admin 可编辑任何共享验方) |

### 4.2 其他模块无共享机制

| 模块 | 说明 |
|------|------|
| Herb (药材) | 无共享标记。所有启用药材对 DoctorOrAdmin 均可见，归属仅限制写操作 |
| MedicalCase (医案) | 无共享标记。Doctor 仅见自己的，Admin 全量。不存在医案共享场景 |
| Patient (患者) | 无归属限制。所有角色 (含 Receptionist) 均可见全部启用患者 |

---

## 5. 数据可见性规则

### 5.1 按角色的数据范围

```
SuperAdmin
  ├─ 用户: Admin + Doctor + Receptionist (过滤自己)
  ├─ 药材: 全部
  ├─ 验方: 全部
  ├─ 医案: 全部
  ├─ 患者: 全部 (含禁用)
  └─ 挂号: 全部 (只读)

Admin
  ├─ 用户: Doctor + Receptionist (过滤自己 + sysadmin + 其他 Admin)
  ├─ 药材: 全部
  ├─ 验方: 全部
  ├─ 医案: 全部
  ├─ 患者: 全部 (含禁用)
  └─ 挂号: 全部 (只读)

Doctor
  ├─ 用户: 403 (无权进入用户管理)
  ├─ 药材: 全部 (写操作仅自己的)
  ├─ 验方: 自己的 + IsShared=true 的 (写操作仅自己的)
  ├─ 医案: 仅 UserId=自己的
  ├─ 患者: 全部 (含禁用, 标注状态)
  └─ 挂号: 仅个人队列 (DoctorId=自己)

Receptionist
  ├─ 用户: 403 (无权进入用户管理)
  ├─ 药材: 403 (DoctorOrAdmin)
  ├─ 验方: 403 (DoctorOrAdmin)
  ├─ 医案: 简要提示 (创建时间 + 主治医生)
  ├─ 患者: 全部启用患者 (自动过滤 Status=Disabled)
  └─ 挂号: 全部队列 (只读) + 创建/取消 Source=Receptionist
```

### 5.2 禁用数据的可见性

| 数据类型 | 禁用后 Admin 视角 | 禁用后 Doctor 视角 | 禁用后 Receptionist 视角 |
|---------|------------------|-------------------|------------------------|
| 禁用患者 | 可见 (列表标注状态) | 可见 (列表标注状态) | **自动过滤不可见** |
| 禁用患者的历史医案 | PatientName **完整显示** | PatientName **掩码显示** (如 "张*") | 无医案权限 |
| 禁用药材 | 列表可见, 开方不可选 | 列表可见, 开方不可选 | 无药材权限 |
| 禁用药材在历史处方中 | 名称后缀 "(已停用)", 只读 | 名称后缀 "(已停用)", 只读 | 无权限 |
| 禁用验方 | 列表可见, 处方导入不可选 | 列表可见 (如有权限), 处方导入不可选 | 无验方权限 |
| 禁用用户 | 可见 (列表标注状态) | 无用户管理权限 | 无用户管理权限 |

> 来源: patients.md US-PAT-002/013, medical-cases.md MC-D07/MC-D16, herbs.md US-HERB-006, formulas.md US-FORM-006

---

## 6. 跨模块联动规则

### 6.1 患者状态联动

| 触发事件 | 影响模块 | 行为 | 来源 |
|---------|---------|------|------|
| 患者禁用 | MedicalCase | 禁止为该患者创建新医案 (ERR-30105) | MC-D16 |
| 患者禁用 | MedicalCase | 历史医案可查阅, PatientName 按角色脱敏 | MC-D16 |
| 患者禁用 | Registration | 禁止为该患者创建挂号 (REG-70005) | registration.md |
| 患者有 Active/Suspended 医案 | Patient | 禁止禁用该患者 (需先完成或取消医案) | patients.md US-PAT-013 |
| 患者有关联医案 (任何状态) | Patient | 禁止删除该患者 (422, 建议禁用) | patients.md US-PAT-005, MC-D04 |

### 6.2 药材状态联动

| 触发事件 | 影响模块 | 行为 | 来源 |
|---------|---------|------|------|
| 药材禁用 | MedicalCase | 开方时不可选择该药材 (处方模块过滤) | herbs.md US-HERB-006 |
| 药材禁用 | Formula (导入处方) | 验方导入处方时自动跳过禁用药材 + 弹出提示 | MC-D09 |
| 药材被处方引用 | Herb | 禁止删除 (422), 建议使用禁用功能 | herbs.md US-HERB-005, BR-DEL-001 |
| 药材被验方引用 | Herb | 检查引用计数 (FormulaReferenceCount) | herbs.md HERB-D03 |
| 药材价格变更 | MedicalCase | 不影响已有处方 (PrescriptionItem.UnitPrice 为快照值) | herbs.md HERB-D06 |

### 6.3 验方状态联动

| 触发事件 | 影响模块 | 行为 | 来源 |
|---------|---------|------|------|
| 验方禁用 | MedicalCase | 处方导入列表不显示该验方 | formulas.md US-FORM-006 |
| 验方未验证 (Draft) | MedicalCase | 处方导入列表不显示 (仅 Validated + Enabled) | MC-D08 |
| 验方导入到处方 | Formula | 导入为数据复制, 修改处方不影响原验方 | MC-D12 |

### 6.4 用户状态联动

| 触发事件 | 影响模块 | 行为 | 来源 |
|---------|---------|------|------|
| 用户角色变更 | Auth | 撤销该用户所有 Token Family, 强制重登录 | AUTH-D07 |
| 用户禁用 | Auth | 撤销所有 Token Family, 当前会话立即失效 | users.md US-USER-011 |
| 用户删除 | Auth | 撤销所有 Token Family | users.md US-USER-005 |
| 医生删除/禁用 | MedicalCase | 名下医案数据保留 (DoctorId 不变), 管理员手动处理 | USER-D06 |
| 医生禁用 | Registration | 指派该医生的挂号: 已在队列中的不自动取消 (前台手动处理) | registration.md REG-70006 |

### 6.5 医案-挂号联动

| 触发事件 | 影响模块 | 行为 | 来源 |
|---------|---------|------|------|
| 医案完成 | Registration | Registration.Status 自动变为 Completed | US-REG-005 |
| 医案取消 (Source=Receptionist) | Registration | Registration.Status 回退为 Waiting, MedicalCaseId 清空 | US-REG-006 |
| 医案取消 (Source=Doctor) | Registration | Registration.Status 自动变为 Cancelled (闭环) | US-REG-006 |
| 挂号取消前置校验 | MedicalCase | 有 Active/Suspended/Completed 医案时拒绝取消挂号 | REG-BR-001 |

---

## 7. 统一删除策略 (BR-DEL-001)

> 来源: medical-cases.md BR-DEL-001

| 模块 | 被引用关系 | 有引用时 | 无引用时 |
|------|-----------|---------|---------|
| Patient (患者) | 被 MedicalCase 引用 | 禁止删除 (422), 建议禁用 | 软删除 (IsDeleted=true) |
| Herb (药材) | 被 PrescriptionItem + FormulaItem 引用 | 禁止删除 (422), 建议禁用 | 软删除 |
| Formula (验方) | 无被引用关系 (导入为数据复制) | N/A | 软删除 |
| User (用户) | 特殊规则 (权限值 + sysadmin 保护) | 软删除 (医案数据保留) | 软删除 |
| MedicalCase (医案) | 取消 = 软删除 (IsDeleted=true) | N/A | 软删除 |

---

## 8. 错误码速查 (权限相关)

| 错误码 | 模块 | 含义 | 触发场景 |
|--------|------|------|---------|
| 401 Unauthorized | Auth | 未认证 | Token 缺失/过期/无效 |
| 403 Forbidden | 全局 | 角色不匹配 | 访问受限端点 (如 Receptionist 访问 /herbs) |
| ERR-50103 | Herb | 药材所有权不足 | Doctor 操作他人创建的药材 |
| ERR-60103 | Formula | 验方所有权不足 | Doctor 操作他人创建的验方 |
| ERR-30201 | MedicalCase | 医案编辑权限不足 | Doctor 编辑他人医案 / 状态不允许 |
| ERR-30105 | MedicalCase | 患者已禁用 | 为禁用患者创建医案 |
| ERR-10006 | User | 用户已禁用 | 禁用用户尝试登录 |
| REG-70004 | Registration | 无权取消挂号 | Doctor 尝试取消前台创建的挂号 |
| USER-D04 | User | 权限值不足 | 低权限用户操作高权限用户 |
| USER-D05 | User | sysadmin 不可管理 | 任何以 sysadmin 为目标的管理操作 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-10 | v1.0 | 初始版本: 从 7 个模块 PRD 汇总提取。8 个章节: 角色体系 / API 授权策略 / 数据归属规则 / 共享机制 / 数据可见性 / 跨模块联动 / 统一删除策略 / 错误码速查 |
