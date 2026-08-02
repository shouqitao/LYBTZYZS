# 角色工作流审查报告

> 日期: 2026-08-02 | 审查方法：逐角色追踪完整工作流，对比 personas/需求/权限文档

## 审查摘要

| 角色 | 工作流步骤 | 发现问题 |
|------|:----------:|:--------:|
| 超管(sysadmin) | 7 步 | 3 |
| 管理员(Admin) | 8 步 | 4 |
| 前台(Receptionist) | 5 步 | 3 |
| 医生(Doctor) | 8 步 | 3 |
| **合计** | **28 步** | **13** |

---

## 一、超管（sysadmin）工作流

### 完整流程

```
Step 1: 首次登录（sysadmin/SysAdmin@2026!）
  → 2a. 强制改密（ForceChangeOnFirstLogin=true）
  → 2b. 5步初始化向导（改密→诊所信息→模式选择→创建admin→交权）
Step 2: 创建管理员（admin）
Step 3: 管理员创建医生/前台
Step 4: 系统配置（诊所信息、安全策略、功能开关）
Step 5: 健康监控（/health、/health/details）
Step 6: 日志级别调整（/diagnostics/logging/*）
Step 7: 备份恢复
```

### 发现的问题

| # | 问题 | 严重度 | 说明 |
|---|------|:------:|------|
| S1 | **初始化向导未实现** | 🔴 | personas 说「5步向导：改密→诊所信息→模式选择→创建admin→交权」，但代码 `FirstRunSetupViewModel` 仅做连接配置 |
| S2 | **首次改密流程不明确** | 🟡 | personas 说 `ForceChangeOnFirstLogin=true`，但 02-auth.md 未描述首次登录后的强制改密流程 |
| S3 | **备份恢复 UI 待开发** | 🟡 | personas 标注「备份有、恢复 UI 待开发」，06-operations 未明确此缺口 |

---

## 二、管理员（Admin）工作流

### 完整流程

```
Step 1: 登录
Step 2: 创建医生/前台用户（Users CRUD）
Step 3: 管理药材库（Herbs CRUD + 批量导入）
Step 4: 管理验方库（Formulas CRUD + 验证）
Step 5: 管理患者（Patients CRUD）
Step 6: 查看医案（只读，可状态变更）
Step 7: 查看报表（收入/问诊/药材）
Step 8: 系统配置（仅 SuperAdmin 可操作）
```

### 发现的问题

| # | 问题 | 严重度 | 说明 |
|---|------|:------:|------|
| A1 | **药材创建/编辑：文档说Admin，代码说DoctorOrReceptionist** | 🔴 | 05-herbs.md US-HERB-003~013 角色已改为「管理员」，但代码 HerbsController 类级策略仍为 `DoctorOrReceptionist`，Admin 反而用不了 |
| A2 | **Admin 能否查看所有医案？** | 🟡 | personas 说「Admin 查看全部（不编辑内容）」，07-medical-cases.md 说 Admin 可查看，但代码 `DoctorOrAdmin` 策略确认 Admin 可查。一致但需确认 Admin 不能编辑 Consultation/Prescription |
| A3 | **Admin 能否创建挂号？** | 🟡 | 权限矩阵说 Admin ✅ 创建，但 personas 说 Admin「不参与挂号」，08-registration.md 用户速览说「管理员：查看所有挂号记录（只读）」。矛盾 |
| A4 | **Admin 能否删除患者？** | 🟡 | 权限矩阵说 Admin ✅ 删除，但 03-users.md 用户速览说「管理员：全部操作：增删改、批量导入、启用/禁用」。一致，但需确认代码策略 |

---

## 三、前台（Receptionist）工作流

### 完整流程

```
Step 1: 登录
Step 2: 读卡登记患者（CardReader → 自动填充 → 创建/查找患者）
Step 3: 创建挂号（Waiting 状态）
Step 4: 取消挂号（仅 Waiting 状态）
Step 5: 查看待诊队列
```

### 发现的问题

| # | 问题 | 严重度 | 说明 |
|---|------|:------:|------|
| R1 | **挂号创建/取消：代码阻断前台** | 🔴 | personas 明确标注「DoctorOrAdmin 阻断」，08-registration.md 说前台可创建/取消，但代码 `DoctorOrAdmin` 策略不含 Receptionist |
| R2 | **患者管理：代码阻断前台** | 🔴 | personas 标注「DoctorOrAdmin 阻断」，04-patients.md 说前台可 CRUD，但代码 `DoctorOrAdminOrReceptionist` 策略确认前台可操作（此处已修复） |
| R3 | **前台能否查看验方？** | 🟡 | 权限矩阵说 Receptionist ✅ 查看，06-formulas.md 用户速览说「前台：查看验方（只读）」，但 personas 说前台「不涉及药材」——药材和验方是不同模块，一致 |

---

## 四、医生（Doctor）工作流

### 完整流程

```
Step 1: 登录
Step 2a: 远程模式 → 待诊队列选患者 → StartVisit → 进入看诊
Step 2b: 本地模式 → 直接选/建患者 → 创建医案
Step 2c: QuickVisit → 跳过排队 → 直接看诊
Step 3: 写诊断（主诉/现病史/舌诊/脉诊/辨证）
Step 4: 标记是否需要开处方
Step 5: 开处方（手动添加药材 或 从验方导入）
Step 6: 打印处方（A5/A4）
Step 7: 完成医案
```

### 发现的问题

| # | 问题 | 严重度 | 说明 |
|---|------|:------:|------|
| D1 | **StartVisit 不创建医案** | 🔴 | personas 说「StartVisit → 进入看诊」，08-registration.md BR-000 说「StartVisit 不建医案」，但 personas 的「首诊旅程」检查表说 StartVisit 🔴（P0）。流程断裂：医生点「开始就诊」后，医案从哪来？ |
| D2 | **医生能否查看所有验方？** | 🟡 | 06-formulas.md 说「查看自己和共享的验方」，权限矩阵说 Doctor ✅ 查看。一致。但 personas 说 Doctor 可创建验方——代码确认 Doctor 可创建（DoctorOrReceptionist 策略）。一致 |
| D3 | **医生能否创建药材？** | 🟡 | 05-herbs.md 已改为「管理员」，权限矩阵说 Doctor ❌。一致。但 personas 说 Doctor「药材查询」可做，「药材写操作」✗。一致 |

---

## 五、跨角色矛盾汇总

### 🔴 P0 必须修复

| # | 矛盾 | 涉及文档 | 修复方向 |
|---|------|---------|---------|
| 1 | **前台无法创建/取消挂号** | personas(🔴) vs 08-registration.md(✅) vs 代码(DoctorOrAdmin) | 代码策略改 DoctorOrReceptionist |
| 2 | **药材创建/编辑：Admin 用不了** | 05-herbs.md(管理员) vs 代码(DoctorOrReceptionist) | 代码策略改 AdminOrSuperAdmin |
| 3 | **初始化向导未实现** | personas(📋已设计) vs 代码(❌) | 需开发或标注为 v2.0 |
| 4 | **StartVisit 不创建医案** | personas(🔴) vs BR-000(✅不建) vs 代码(不建) | 需明确：医生点「开始就诊」后，医案何时创建？Desktop 端流程需补充 |

### 🟡 P1 应该修复

| # | 矛盾 | 涉及文档 | 修复方向 |
|---|------|---------|---------|
| 5 | **Admin 能否创建挂号？** | 权限矩阵(✅) vs personas(不参与) vs 08-registration.md(只读) | 统一为 Admin 只读 |
| 6 | **首次改密流程不明确** | personas(强制改密) vs 02-auth.md(未描述) | 02-auth.md 补充首次改密流程 |
| 7 | **备份恢复 UI 待开发** | personas(⚠️) vs 06-operations(未标注) | 标注为 v2.0 |
| 8 | **Admin 医案操作边界** | personas(不编辑内容) vs 07-medical-cases.md(可编辑) | 统一为 Admin 只做状态变更 |

---

## 六、文档完整性检查

| 文档 | 是否覆盖完整工作流 | 缺口 |
|------|:------------------:|------|
| 02-personas.md | ✅ 4 角色完整 | 但含过时的代码阻断描述（已修复的策略未更新） |
| 02-auth.md | ✅ 认证流程完整 | 缺首次改密流程描述 |
| 04-patients.md | ✅ 患者管理完整 | — |
| 05-herbs.md | ⚠️ US 角色已修正 | 代码策略未同步 |
| 06-formulas.md | ✅ 验方管理完整 | — |
| 07-medical-cases.md | ✅ 医案流程完整 | BR-000 已补充 |
| 08-registration.md | ✅ 挂号流程完整 | 但前台阻断问题未在文档中标注 |
| 09-printing.md | ✅ 打印流程完整 | — |
| 10-reports.md | ✅ 报表完整 | — |
