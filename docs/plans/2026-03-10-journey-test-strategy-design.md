# Journey Test Strategy: "From Zero to Production" 设计方案

> **版本**: v1.0
> **创建日期**: 2026-03-10
> **状态**: DESIGN COMPLETE (待用户最终确认)

---

## 1. 设计目标

从一个空白系统开始，按**真实用户使用顺序**验证 WebAPI 全部功能链:
超管启动系统 -> 管理员准备数据 -> 前台登记患者 -> 医生完成诊疗。

测试通过 = 系统可上线。

---

## 2. 已确认的设计决策

| # | 决策 | 理由 | 确认时间 |
|---|------|------|----------|
| D-01 | **Layer A (Journey) 优先，Layer B (Feature) 后补** | 先保证业务链跑通，再逐个验证边界条件 | 2026-03-10 21:00 |
| D-02 | **按角色组织测试，不按流程/Narrative** | 软件是用户用的，按用户角色更直观易懂 | 2026-03-10 21:10 |
| D-03 | **测试共享同一个 SQL Server 数据库，数据层层累积** | 超管创建的用户，医生直接登录使用。模拟真实场景，不每次清空 | 2026-03-10 21:15 |
| D-04 | **用 partial class 拆分: 一本书，多个章节** | 一个测试类按角色拆成多个文件。保证执行顺序的同时每个角色单独一个文件 | 2026-03-10 21:26 |
| D-05 | **多角色共享功能不遗漏** | PRD 中部分功能多角色可用 (如挂号: 前台+医生)，在对应角色章节中分别测试 | 2026-03-10 21:30 |
| D-06 | **数据归属与共享必须跨角色验证** | 验方/药材的所有权规则和共享可见性是核心业务逻辑，需 Ch4 (单医生视角) + Ch5 (多医生隔离 + Admin 全量) 双重验证 | 2026-03-10 补充 |
| D-07 | **挂号状态联动纳入 Ch5** | Registration 状态自动跟随 MedicalCase (US-REG-005/006) 属于跨模块联动，在 Ch5 验证 | 2026-03-10 补充 |

---

## 3. 权限体系速查

### API 授权策略

| Policy | 允许角色 | 适用模块 |
|--------|---------|---------|
| 匿名 (AllowAnonymous) | 所有人 | 登录/登出/Token刷新/Health探活 |
| 任意已认证 | 所有已登录用户 | Token验证、自助改密/改资料、/users/current |
| PatientAccess | SuperAdmin + Admin + Doctor + Receptionist | 患者管理、挂号管理 |
| DoctorOrAdmin | SuperAdmin + Admin + Doctor | 药材/验方/医案/同步 |
| AdminOnly | SuperAdmin + Admin | 用户管理 (CRUD) |
| AdminOnly | SuperAdmin + Admin | 密码重置、用户恢复 |
| SuperAdminOnly | SuperAdmin | 系统诊断 |
| Roles=Doctor | 仅 Doctor | 创建医案 (唯一一个更严格的端点) |

### 关键权限特例

- **创建医案**: 全系统唯一 Doctor 专属操作，Admin 调用返回 403
- **Doctor 所有权**: 药材/验方的编辑/删除/禁用，Doctor 仅可操作自己创建的
- **前台受限**: 无药材/验方/医案权限；患者只能 CRU (无删除)；挂号仅自己创建的能取消
- **管理员专属**: 患者状态管理(禁用)、批量导入导出、审计日志

### 数据归属与共享规则

> 来源: formulas.md Section 2/US-FORM-002/US-FORM-008, herbs.md Section 2/US-HERB-004, medical-cases.md US-MC-009/013

**验方归属 (Formula)**:

| 创建者 | 可见性 | 编辑/删除权限 |
|--------|--------|--------------|
| Admin/SuperAdmin 创建 | Admin: 全部可见; Doctor: 仅 IsShared=true 时可见 | Admin: 全部可操作 |
| Doctor 创建 (IsShared=false) | 仅创建者本人可见 | 仅创建者本人 |
| Doctor 创建 (IsShared=true) | 所有 Doctor + Admin 可见 | 创建者本人可编辑; 其他 Doctor 只读; Admin 可编辑任何共享验方 |

- Doctor 列表查询返回: `CreatedBy=自己 OR IsShared=true` (US-FORM-002)
- Admin 列表查询返回: 全部验方
- Doctor 编辑他人验方 (含他人共享验方) -> 403 (ERR-60103)
- Admin 可编辑任何共享验方 (US-FORM-008 BR-3)

**药材归属 (Herb)**:

| 操作 | Admin/SuperAdmin | Doctor |
|------|------------------|--------|
| 创建 | 可以 | 可以 |
| 查看列表/详情 | 全部可见 | 全部可见 (DoctorOrAdmin) |
| 更新/删除/启禁用 | 全部可操作 | 仅可操作自己创建的 (ERR-50103) |

- 药材无共享标记，所有启用药材对 DoctorOrAdmin 角色均可见
- 所有权限制仅在写操作 (Update/Delete/ToggleStatus)

**医案归属 (MedicalCase)**:

| 操作 | Admin/SuperAdmin | Doctor | Receptionist |
|------|------------------|--------|-------------|
| 创建 | 403 (仅 Doctor) | 仅自己创建 | 无权限 |
| 查看列表 | 全部医案 | 仅 UserId=自己的 | 简要提示 (时间+医生) |
| 编辑 | 全部 (已完成需 EditReason) | 仅自己的未完成医案 | 无权限 |

**患者可见性 (Patient)**:

| 角色 | Status=Enabled | Status=Disabled |
|------|---------------|-----------------|
| Admin/SuperAdmin | 可见 | 可见 (列表标注状态) |
| Doctor | 可见 | 可见 (列表标注状态) |
| Receptionist | 可见 | **自动过滤不可见** |

**挂号归属 (Registration)**:

| 操作 | Receptionist | Doctor | Admin |
|------|-------------|--------|-------|
| 创建挂号 | Source=Receptionist | Source=Doctor (静默) | 无 |
| 查看队列 | 全部医生队列 (只读) | 仅个人队列 | 全部 (只读) |
| 取消挂号 | 仅 Source=Receptionist 的 | 无权取消 | 无 |
| 接诊 (开始看诊) | 无 | 从队列选择接诊 | 无 |

---

## 4. 测试架构

### 文件结构

```
tests/LYBT.Tests.Server/
  SystemJourney/
    _Infrastructure/
      JourneyFixture.cs              -- Fixture: 初始化DB一次，不重置
      JourneyState.cs                -- 共享状态: 用户名/密码/ID 跨章节传递
      TestPriorityAttribute.cs       -- [TestPriority(n)] 排序注解
      PriorityOrderer.cs             -- ITestCaseOrderer 实现
    SystemJourneyTests.cs            -- partial class 主文件 + Collection 绑定
    SystemJourneyTests.Ch1_SuperAdmin.cs
    SystemJourneyTests.Ch2_Admin.cs
    SystemJourneyTests.Ch3_Receptionist.cs
    SystemJourneyTests.Ch4_Doctor.cs
    SystemJourneyTests.Ch5_CrossRole.cs
```

### 共享状态设计

```csharp
// JourneyState.cs -- 章节之间传递数据
public static class JourneyState
{
    // Ch1 产出 -> Ch2/3/4 消费
    public static string AdminUsername { get; set; }
    public static string AdminPassword { get; set; }
    public static string DoctorUsername { get; set; }
    public static string DoctorPassword { get; set; }
    public static string ReceptionistUsername { get; set; }
    public static string ReceptionistPassword { get; set; }

    // Ch2 产出 -> Ch4 消费
    public static Guid Herb1Id { get; set; }  // 黄芪
    public static Guid Herb2Id { get; set; }  // 当归
    public static Guid Herb3Id { get; set; }  // 川芎
    public static Guid FormulaSharedId { get; set; }   // 四君子汤 (Admin创建, IsShared=true)
    public static Guid FormulaPrivateId { get; set; }  // 六味地黄丸 (Admin创建, IsShared=false)

    // Ch3 产出 -> Ch4 消费
    public static Guid PatientId { get; set; }
    public static string PatientName { get; set; }
    public static Guid RegistrationId { get; set; }
}
```

### 执行顺序保证

xUnit 同一个类内的测试方法，通过 `[TestPriority(n)]` + 自定义 `PriorityOrderer` 保证顺序:
- Ch1 方法: Priority 100-199
- Ch2 方法: Priority 200-299
- Ch3 方法: Priority 300-399
- Ch4 方法: Priority 400-499
- Ch5 方法: Priority 500-599

---

## 5. 章节详细设计

### 第1章: 超管 (Ch1_SuperAdmin)

**角色**: SuperAdmin (系统内置账号 sysadmin)
**职责**: 系统"开机"，创建所有角色用户

**正常路径:**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 1 | 超管登录 | POST /auth/login (sysadmin) | 200, Token + User 信息返回 | US-AUTH-001, US-AUTH-012 |
| 2 | 验证身份 | GET /users/current | 200, Role=SuperAdmin | US-USER-012 |
| 3 | 创建管理员 | POST /users (Admin) | 201, Role=Admin | US-USER-001 |
| 4 | 创建医生 | POST /users (Doctor) | 201, Role=Doctor | US-USER-001 |
| 5 | 创建前台 | POST /users (Receptionist) | 201, Role=Receptionist | US-USER-001 |
| 6 | 三个新用户登录 | POST /auth/login x3 | 各自返回 Token | US-AUTH-001 |

**异常路径:**

| # | 步骤 | 操作 | 预期结果 | 覆盖 |
|---|------|------|----------|------|
| 7 | 密码错误 | POST /auth/login (wrong pwd) | 401 Unauthorized | US-AUTH-001 边界 |
| 8 | 不存在用户 | POST /auth/login (unknown) | 401 Unauthorized | US-AUTH-001 边界 |
| 9 | 空密码 | POST /auth/login (empty pwd) | 400 Bad Request | Validator |
| 10 | 重复用户名 | POST /users (同名) | 400 Bad Request | US-USER-001 边界 |
| 11 | 必填缺失 | POST /users (无 UserName) | 400 Bad Request | Validator |

**章节产出 (写入 JourneyState):**
- AdminUsername/Password, DoctorUsername/Password, ReceptionistUsername/Password

---

### 第2章: 管理员 (Ch2_Admin)

**角色**: Admin (第1章创建的管理员账号)
**职责**: 准备基础数据 (用户管理 + 药材 + 验方)

**用户管理:**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 1 | 管理员登录 | POST /auth/login | 200, Token | US-AUTH-001 |
| 2 | 查看用户列表 | GET /users | 200, 包含3个新用户 | US-USER-002 |
| 3 | 查看用户详情 | GET /users/{doctorId} | 200, 完整信息 | US-USER-003 |
| 4 | 更新用户信息 | PUT /users/{doctorId} | 200, RealName 变更 | US-USER-004 |
| 5 | 禁用/启用用户 | POST /users/{id}/toggle-status | 200, 状态切换 | US-USER-011 |

**药材管理:**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 6 | 创建药材 (黄芪) | POST /herbs | 201, PinYin 自动生成 | US-HERB-001 |
| 7 | 创建药材 (当归) | POST /herbs | 201 | US-HERB-001 |
| 8 | 创建药材 (川芎) | POST /herbs | 201 | US-HERB-001 |
| 9 | 查看药材列表 | GET /herbs | 200, 3 个药材 | US-HERB-002 |
| 10 | 搜索药材 (名称) | GET /herbs?keyword=黄 | 200, 匹配黄芪 | US-HERB-002 |
| 11 | 搜索药材 (拼音) | GET /herbs?keyword=HQ | 200, 匹配黄芪 | US-HERB-002 |
| 12 | 查看药材详情 | GET /herbs/{id} | 200, 完整信息 | US-HERB-003 |
| 13 | 更新药材价格 | PUT /herbs/{id} (改价格) | 200 | US-HERB-004 |

**验方管理:**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 14 | 创建验方 (共享) | POST /formulas (四君子汤, 含3味药, IsShared=true) | 201, Herbs 关联正确, IsShared=true | US-FORM-001, US-FORM-008 |
| 15 | 创建验方 (不共享) | POST /formulas (六味地黄丸, IsShared=false) | 201, IsShared=false | US-FORM-001 |
| 16 | 查看验方列表 | GET /formulas | 200, Admin 可见全部验方 (含共享和不共享) | US-FORM-002 |
| 17 | 查看验方详情 | GET /formulas/{id} | 200, 药材组成 3 味 | US-FORM-003 |
| 18 | 更新验方 | PUT /formulas/{id} | 200 | US-FORM-004 |
| 19 | 禁用验方 | POST /formulas/{id}/toggle-status | 200, 状态=Disabled | US-FORM-006 |
| 20 | 启用验方 | POST /formulas/{id}/toggle-status | 200, 状态=Enabled | US-FORM-006 |

**异常路径:**

| # | 步骤 | 操作 | 预期结果 | 覆盖 |
|---|------|------|----------|------|
| 21 | 重复药材名 | POST /herbs (同名) | 400 | US-HERB-001 边界 |
| 22 | 删除被引用药材 | DELETE /herbs/{黄芪id} | 422 引用保护 | US-HERB-005 BR-DEL-001 |
| 23 | Admin 创建 SuperAdmin | POST /users (Role=SuperAdmin) | 403 Forbidden (权限值: Admin=80 < SuperAdmin=100) | US-USER-001 USER-D04 |
| 24 | 删除自己 | DELETE /users/{自己id} | 400 "不能删除自己" | US-USER-005 边界 |
| 25 | Admin 编辑 sysadmin | PUT /users/{sysadmin-id} | 403 "系统管理员账号不可被修改" | USER-D05 |

**章节产出 (写入 JourneyState):**
- Herb1Id, Herb2Id, Herb3Id, FormulaSharedId, FormulaPrivateId

---

### 第3章: 前台 (Ch3_Receptionist)

**角色**: Receptionist (第1章创建的前台账号)
**职责**: 登记患者、创建挂号

**患者管理 (前台有 CRU 权限):**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 1 | 前台登录 | POST /auth/login | 200, Token | US-AUTH-001 |
| 2 | 创建患者 (张三) | POST /patients | 201, 完整信息 | US-PAT-001 |
| 3 | 搜索患者 (姓名) | GET /patients?keyword=张 | 200, 匹配张三 | US-PAT-002 |
| 4 | 查看患者详情 | GET /patients/{id} | 200 | US-PAT-003 |
| 5 | 更新患者信息 | PUT /patients/{id} (补充地址) | 200 | US-PAT-004 |

**挂号管理 (前台核心职责):**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 6 | 创建挂号 (指定医生) | POST /registrations | 201, Status=Waiting | US-REG-001 |
| 7 | 查看挂号队列 | GET /registrations/queue | 200, 包含刚创建的挂号 | US-REG-003 |

**异常路径:**

| # | 步骤 | 操作 | 预期结果 | 覆盖 |
|---|------|------|----------|------|
| 8 | 前台删除患者 | DELETE /patients/{id} | 403 Forbidden (前台无删除权限) | US-PAT-005 权限 |
| 9 | 前台查看药材 | GET /herbs | 403 Forbidden (DoctorOrAdmin) | 权限验证 |
| 10 | 前台查看医案 | GET /medicalcases | 403 Forbidden (DoctorOrAdmin) | 权限验证 |
| 11 | 前台创建挂号后取消 | PUT /registrations/{id}/cancel | 200, Status=Cancelled | US-REG-004 |
| 12 | 重新创建挂号 (供第4章使用) | POST /registrations | 201, Status=Waiting | US-REG-001 |

**章节产出 (写入 JourneyState):**
- PatientId, PatientName, RegistrationId

---

### 第4章: 医生 (Ch4_Doctor)

**角色**: Doctor (第1章创建的医生账号)
**职责**: 完成完整诊疗流程 + 医生可做的非诊疗工作

**从挂号队列开始看诊 (主线: 首诊完整流程):**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 1 | 医生登录 | POST /auth/login | 200, Token | US-AUTH-001 |
| 2 | 查看挂号队列 | GET /registrations/queue?doctorId={id} | 200, 包含前台创建的挂号 | US-REG-003 (医生视角) |
| 3 | 开始看诊 | PUT /registrations/{id}/start-visit | 200, Status=InProgress | US-REG-002 (接诊) |
| 4 | 创建医案 | POST /medicalcases | 201, Status=Active | US-MC-001 |
| 5 | 填写四诊 (Consultation) | PUT /medicalcases/{id} (含诊断) | 200, 诊断保存 | US-MC-002 |
| 6 | 标记需要处方 | PUT /medicalcases/{id}/prescription-flag | 200 | US-MC-003 |
| 7 | 开具处方 (含药材) | PUT /medicalcases/{id} (含 Prescription) | 200, Items 保存 | US-MC-004, US-MC-005 |
| 8 | 验证聚合保存 | GET /medicalcases/{id} | 200, Consultation + Prescription 完整 | US-MC-005 |
| 9 | 完成医案 | PUT /medicalcases/{id}/status (Completed) | 200 | US-MC-007 |

**复诊流程 (第二个患者或复诊场景):**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 10 | 搜索患者 | GET /patients?keyword=张 | 200, 找到张三 | US-PAT-002 (医生视角) |
| 11 | 查看历史医案 | GET /medicalcases/query?queryType=ByPatient&patientId={id} | 200, 包含第一个医案 | US-MC-009 |
| 12 | 创建新医案 (复诊) | POST /medicalcases | 201 | US-MC-001 |
| 13 | 完成复诊 (诊断+处方+完成) | PUT + PUT + PUT | 全部 200 | US-MC-002~007 |
| 14 | 确认两个医案都存在 | GET /medicalcases/query?...&patientId={id} | Items >= 2 | US-MC-009 |

**医生创建药材和验方 + 归属与共享验证:**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 15 | 医生创建药材 | POST /herbs (茯苓) | 201 | US-HERB-001 (医生视角) |
| 16 | 医生更新自己的药材 | PUT /herbs/{茯苓id} | 200 | US-HERB-004 (医生自有) |
| 17 | 医生创建验方 (私有) | POST /formulas (补中益气汤, IsShared=false) | 201, IsShared=false | US-FORM-001 (医生视角) |
| 18 | 医生查看验方列表 | GET /formulas | 200, 含自己的 + Admin IsShared=true 的; **不含** Admin IsShared=false 的 | US-FORM-002 归属过滤 |
| 18a | 验证验方可见性详情 | 解析列表结果 | 包含: 补中益气汤 (自己) + 四君子汤 (Admin共享); 不包含: 六味地黄丸 (Admin私有) | US-FORM-002 |
| 18b | 医生共享自己的验方 | PUT /formulas/{补中益气汤id} (IsShared=true) | 200, IsShared 变为 true | US-FORM-008 |
| 18c | 医生尝试编辑Admin共享验方 | PUT /formulas/{四君子汤id} | 403, ERR-60103 所有权保护 | US-FORM-004 权限 |

**医案编辑与业务规则:**

| # | 步骤 | 操作 | 验证点 | 覆盖 US |
|---|------|------|--------|---------|
| 19 | 编辑已完成医案 (无原因) | PUT /medicalcases/{id} (无 EditReason) | 400 "需要修改原因" | US-MC-005 + BR |
| 20 | 编辑已完成医案 (有原因) | PUT /medicalcases/{id} (含 EditReason) | 200 | US-MC-005 + BR |
| 21 | 挂起医案测试 | POST /medicalcases + PUT suspend | 200, Status=Suspended | US-MC-006 |

**异常路径:**

| # | 步骤 | 操作 | 预期结果 | 覆盖 |
|---|------|------|----------|------|
| 22 | 同一患者重复活跃医案 | POST /medicalcases (同 PatientId) | 400, BR-001 | 业务规则 |
| 23 | 空诊断完成医案 | PUT status=Completed (无 TcmDiagnosis) | 400, BR-003 | 业务规则 |
| 24 | 无处方决定完成医案 | PUT status=Completed (NeedsPrescription=null) | 400, BR-003 | 业务规则 |
| 25 | 医生更新管理员创建的药材 | PUT /herbs/{管理员药材id} | 403, ERR-50103 所有权保护 | US-HERB-004 权限 |
| 25a | 医生删除管理员创建的药材 | DELETE /herbs/{管理员药材id} | 403, ERR-50103 | US-HERB-005 权限 |
| 25b | 医生禁用管理员创建的药材 | POST /herbs/{管理员药材id}/toggle-status | 403, ERR-50103 | US-HERB-006 权限 |
| 25c | 医生删除管理员创建的验方 | DELETE /formulas/{Admin验方id} | 403, ERR-60103 | US-FORM-005 权限 |
| 25d | 医生禁用管理员创建的验方 | POST /formulas/{Admin验方id}/toggle-status | 403, ERR-60103 | US-FORM-006 权限 |
| 26 | 医生创建用户 | POST /users | 403, AdminOnly | 权限验证 |
| 27 | 医生取消前台创建的挂号 | PUT /registrations/{id}/cancel | 403, REG-70004 权限受限 | US-REG-004 权限 |

---

### 第5章: 跨角色验证 (Ch5_CrossRole)

**职责**: 验证角色之间的联动和边界，确保权限体系完整。

**数据归属与共享跨角色验证:**

| # | 步骤 | 操作 | 验证点 | 覆盖 |
|---|------|------|--------|------|
| 1 | Admin 查看医生创建的医案 | GET /medicalcases/{id} (Admin token) | 200, Admin 可查看 | US-MC-013 |
| 2 | Admin 不能创建医案 | POST /medicalcases (Admin token) | 403, Roles=Doctor 限制 | US-MC-001 权限 |
| 3 | Admin 查看验方列表 (全量) | GET /formulas (Admin token) | 200, 包含: Admin 创建的 + Doctor 创建的 (含私有) | US-FORM-002 (Admin 全量视角) |
| 4 | Admin 编辑 Doctor 共享验方 | PUT /formulas/{Doctor共享验方id} (Admin token) | 200, Admin 可编辑任何共享验方 | US-FORM-008 BR-3 |
| 5 | Admin 编辑 Doctor 私有验方 | PUT /formulas/{Doctor私有验方id} (Admin token) | 200, Admin 可操作全部验方 | US-FORM-004 权限 |
| 5a | 创建第二个医生 (验方隔离测试) | POST /users (Doctor2) -> POST /auth/login | 201, Token 获取 | 多医生归属验证前置 |
| 5b | Doctor2 查看验方列表 | GET /formulas (Doctor2 token) | 仅包含 Doctor1 IsShared=true 的 + Admin IsShared=true 的; 不包含 Doctor1 私有验方 | US-FORM-002 跨医生隔离 |

**跨模块联动验证:**

| # | 步骤 | 操作 | 验证点 | 覆盖 |
|---|------|------|--------|------|
| 6 | 禁用患者阻止创建医案 | Admin 禁用患者 -> Doctor 创建医案 | 422 ERR-30105 | MC-D16 跨模块联动 |
| 6a | 禁用患者: Receptionist 不可见 | GET /patients (Receptionist token) | 列表中不含禁用患者 | US-PAT-002 角色过滤 |
| 6b | 禁用患者: Doctor 仍可见 | GET /patients (Doctor token) | 列表中包含禁用患者 (标注状态) | US-PAT-002 角色过滤 |
| 6c | 启用患者恢复正常 | Admin 启用患者 | 200, 限制解除 | US-PAT-013 |
| 7 | 药材引用保护 | Doctor 用药材开方 -> Admin 删除该药材 | 422 引用保护 | US-HERB-005 BR-DEL-001 |
| 8 | 挂号状态自动跟随医案完成 | Doctor 完成医案 -> 检查关联 Registration.Status | Registration.Status=Completed | US-REG-005 |
| 8a | 医案取消联动 (医生模式) | Doctor 取消医案 (Source=Doctor) | Registration.Status=Cancelled (自动闭环) | US-REG-006 |

**认证与基础设施:**

| # | 步骤 | 操作 | 验证点 | 覆盖 |
|---|------|------|--------|------|
| 9 | Token 刷新 | POST /auth/refresh | 200, 新 Token | US-AUTH-003 |
| 10 | 登出 | POST /auth/logout | 2xx | US-AUTH-002 |
| 11 | 匿名访问受保护端点 | GET /users/current (无 Token) | 401 | 认证验证 |
| 12 | 健康检查 (匿名) | GET /health | 200 | 基础设施 |

---

## 6. Must Have US 覆盖追溯矩阵

### 已覆盖 (按章节)

| 章节 | 覆盖 US 数 | US 列表 |
|------|-----------|---------|
| Ch1 超管 | 3 | AUTH-001, AUTH-012, USER-001 |
| Ch2 管理员 | 16 | AUTH-001, USER-002~005, USER-011, HERB-001~004, FORM-001~004, FORM-006, **FORM-008** |
| Ch3 前台 | 7 | AUTH-001, PAT-001~004, REG-001, REG-003, REG-004 |
| Ch4 医生 | 19 | AUTH-001, PAT-002, MC-001~007, MC-009, MC-013, REG-002, REG-003, HERB-001, HERB-004, **HERB-005/006**, FORM-001, FORM-002, **FORM-004, FORM-005, FORM-008** |
| Ch5 跨角色 | 10 | AUTH-002, AUTH-003, MC-001(反面), HERB-005, PAT-013, **PAT-002(角色过滤)**, **FORM-002(全量/隔离)**, **FORM-004(Admin编辑)**, **FORM-008(跨医生)**, **REG-005, REG-006** |

**Must Have 51 US 中 WebAPI 相关约 43 个:**
- Journey 直接覆盖: ~35 US
- Desktop 专属 (不在范围): ~8 US (SHELL-001~005, CFG-002, SYNC-008, AUTH-010 登录状态机)

---

## 7. 与现有测试的关系

| 现有测试 | 数量 | 决策 |
|----------|------|------|
| PureLogic/ (单元测试) | 750 | **保留** - 验证单个函数正确性 |
| RateLimiting/ | 3 | **保留** - 基础设施测试 |
| UserJourneys/ (旧 Journey) | 23 | **SystemJourney 完成后删除** - 被新体系替代 |
| Features/ (Layer B) | 111 | **暂缓** - Layer A 完成后按需补充 |

---

## 8. 未决问题

| # | 问题 | 状态 |
|---|------|------|
| Q-01 | Features/ 测试移到 _Deferred/ 还是直接删除? | 待确认 |
| Q-02 | 挂号状态自动跟随医案完成 (US-REG-005) 是否需要在 Ch4 中验证? | 建议验证 |
| Q-03 | 禁用药材不可用于处方 (X10) 是否放在 Ch5? | 建议放入 |
| Q-04 | Should Have 中的验方导入处方 (US-MC-016) 和复制历史处方 (US-MC-018) 是否纳入 Ch4? | 建议纳入 |
| Q-05 | Admin 创建的验方是否默认 IsShared=true? (PRD 未明确，当前设计为手动设置) | 待确认 -- 当前按 PRD 手动设置 |
| Q-06 | Ch5 第二个医生 (Doctor2) 是否需要独立章节测试多医生场景? | 建议保持在 Ch5，仅做验方可见性隔离验证 |

---

## 9. 预估工作量

| Phase | 内容 | 预计耗时 |
|-------|------|---------|
| 基础设施 | JourneyFixture + JourneyState + PriorityOrderer | 30 min |
| Ch1 超管 | 11 个测试步骤 | 20 min |
| Ch2 管理员 | 23 个测试步骤 | 40 min |
| Ch3 前台 | 12 个测试步骤 | 25 min |
| Ch4 医生 | 31 个测试步骤 (+4 归属/共享) | 55 min |
| Ch5 跨角色 | 16 个测试步骤 (+8 归属/联动) | 35 min |
| 清理旧测试 | 删除 UserJourneys/ + 处理 Features/ | 15 min |
| 全量验证 | 编译 + 运行 + 修复 | 20 min |
| **总计** | | **~4 小时** |

---

> 变更记录:
> - 2026-03-10 21:26: 初始版本，确认 D-01 ~ D-04
> - 2026-03-10 21:40: 完整版本 v1.0，基于 PRD 权限矩阵 + API 端点分析 + 跨角色功能梳理
> - 2026-03-10 补充: v1.1，基于 6 个模块 PRD 全量审查，新增:
>   - Section 3 "数据归属与共享规则" (验方/药材/医案/患者/挂号 5 个模块的归属矩阵)
>   - Ch2: Admin 创建共享/不共享验方 (2 个)、sysadmin 不可管理验证 (1 个)
>   - Ch4: 验方可见性隔离 (3 个)、药材归属保护 (3 个)
>   - Ch5: 多医生验方隔离 (3 个)、Admin 编辑 Doctor 验方 (2 个)、患者角色过滤 (3 个)、挂号状态联动 (2 个)
>   - 新增决策 D-06, D-07; 新增问题 Q-05, Q-06
>   - 总测试步骤: ~96 -> ~110; 预估工作量: 3.5h -> 4h
