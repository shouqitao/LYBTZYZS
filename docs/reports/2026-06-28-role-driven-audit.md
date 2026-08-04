# 角色驱动审计报告（4 角色职能/边界/逻辑/工作流闭环）

> **日期**：2026-06-28
> **方法**：5 路并行 subagent（4 角色 + 跨角色闭环/权限一致性），对照 `personas.md` / `permissions-matrix.md` / `PolicyConstants` / Controller 代码 / 最新设计（R10/sysadmin/审核移除/双模式全角色）
> **基准**：4 角色（Receptionist/Doctor/Admin/Sysadmin）的职能定义、权限边界、跨角色交接闭环
> **标注**：📄=文档可修 / 💻=代码待修 / ⚠️=文档+代码

---

## 一、执行摘要

**整体健康度：C-（设计层已大幅收敛，代码层零进展；存在 1 个会制造新故障的隐藏矛盾）**

文档层（R10 双模式/sysadmin 配置/审核移除/本地全角色）已收敛多数历史模糊，但**代码层 D7/D8 零进展**。最危险的是审计新发现：**`DoctorOrReceptionist` 策略的实际注册定义与文档矩阵根本矛盾**——D7 修复前若不先纠正策略注册，切换后会锁死 Admin/SuperAdmin。

此外，权限矩阵在 personas / permissions-matrix / 代码 三处对**挂号创建、药材创建、医案创建**存在循环矛盾；`PolicyConstants` 缺 `DoctorOnly`（医案"Doctor 唯一"目标无策略可执行）；生产门控（US-SHELL-017）形同虚设。

---

## 二、健康度总览

| 角色域 | 职能定义 | 权限边界 | 闭环 | 总评 |
|---|---|---|---|---|
| Receptionist | 清晰 | 🔴 三 Controller 阻断 + 矩阵矛盾 | 🔴 接诊链断 | D |
| Doctor | 清晰（唯一创建者） | 🔴 缺 DoctorOnly + Admin 越界 | 🔴 StartVisit D8 + 历史聚合缺 | D |
| Admin | 清晰（审核移除后） | 🟡 矩阵矛盾 + 用户硬删 | 🟡 Restore/Excel 缺 | C |
| Sysadmin | 清晰（独立用户） | 🔴 生产门控失效 + 种子矛盾 | 🔴 向导/首改密未实现 | D |
| 跨角色闭环 | — | 🔴 策略定义矛盾 | 🟡 8 交接点 1 闭环 | D |

---

## 三、🔴 关键问题（矛盾/严重缺口/未闭环）

### K1 ⚠️ `DoctorOrReceptionist` 策略定义与文档矩阵根本矛盾（最危险新发现）
`permissions-matrix.md:73` 声称该策略含 SuperAdmin/Admin/Doctor/Receptionist；但 `AuthenticationServiceCollectionExtensions.cs:129-131` 实际仅 `RequireRole(Doctor, Receptionist)`。**D7 切换患者/药材/挂号到此策略后，Admin/SuperAdmin 将被全部锁出**，与矩阵表全面冲突。
→ **D7 修复前必须先扩策略定义（加 Admin/SuperAdmin），否则制造新断裂**。

### K2 ⚠️ 权限矩阵三文档循环矛盾
- **挂号创建**：`personas:165`(Receptionist✓ Doctor✓QuickVisit Admin✗) vs `permissions-matrix:27`(Receptionist✅ Doctor❌ Admin✅) vs 代码 `DoctorOrAdmin`(Doctor✓ Admin✓ Receptionist✗)——三种说法
- **药材创建**：`personas:159`(Doctor✓仅自己) vs `permissions-matrix:37`(Doctor❌) vs 代码允许 Doctor
- **医案创建**：目标"Doctor 唯一、Admin✗"但代码 `DoctorOrAdmin` 允许 Admin
→ 以 personas 为权威冻结一处，驱动 matrix/代码统一。

### K3 💻 `PolicyConstants` 缺 `DoctorOnly`
目标态"医案创建 Doctor 唯一"无策略可执行（`PolicyConstants.cs` 仅 4 项，grep `DoctorOnly` 零命中）。`personas:200/236` 自相矛盾（"唯一创建者"+"DoctorOrAdmin 策略"——该策略本含 Admin）。
→ 新增 `DoctorOnly` 常量，或服务层 `CreatedBy==currentUser` 归属校验兜底。

### K4 💻 生产门控（US-SHELL-017）形同虚设
`IdentitySeedData.SeedRolesAndAdminAsync` 不读 `SystemAdminOptions.AllowAutoCreateInProduction`/`InitialSetupToken`，也无 `IHostEnvironment` 判定，直接用明文 `SysAdmin@2026!` 创建 sysadmin。`DatabaseInitializationService.cs:192` 注释称"用户创建已迁移到 IdentitySeedData"——门控跑完后真正创建路径完全跳过门控。**生产环境 sysadmin 默认密码裸奔**。
→ 让 IdentitySeedData 注入 `IDefaultPasswordService`+`IHostEnvironment`，复用 `GetOrGeneratePassword/ValidateSetupToken`。

### K5 💻 `IdentitySeedData.cs:26-27` 同时种子 admin/sysadmin
违反 `personas:68`/US-SHELL-011"只种子 sysadmin"，admin 应由向导创建。
→ 删 admin 种子，admin 改由 US-SHELL-011 向导 Step4 创建。

### K6 💻 用户硬删除 + 无 Restore（D4 闭环断）
`UsersController.cs:324 DeleteAsync` 硬删除，与全仓 soft-delete 范式相悖；无 `/restore` 端点。Herbs/Formulas/Patients 同缺 Restore 端点（`BaseRepository.RestoreAsync` 基础设施就绪但未接线）。
→ 改软删 + 4 模块补 Restore 端点。

### K7 💻 接诊链断裂（StartVisit D8 + QuickView 未接线）
- `RegistrationsController.cs:200 StartVisit` 仅 `StartVisitAsync` 不建医案 + 返回 RegistrationId 冒充 MedicalCaseId（D8 仍在）
- `QuickVisit`(line 44-94) 已实现原子 Reg+MC 创建——**非死代码**，仅 Desktop 未接线
→ StartVisit 改原子创建医案（R10 spec S5）；QuickVisit Desktop 接线。

### K8 💻 LocalWebAPI 权限策略空缺
`LocalWebAPI/Controllers/{Registrations,Patients,Herbs,MedicalCases}.cs` 仅 `[Authorize]` 无 Policy。R10 S3"本地全角色支持" ↔ Flow 3"本地无角色检查"自相矛盾；本地 Doctor 可删患者/药材 CRUD，违 matrix。
→ 明确本地是否启用角色策略并统一（建议与远程一致 + 角色策略）。

### K9 💻 接诊 Cancel 权限三向倒置
`RegistrationsController.cs:216-220`(Cancel) XML 注释"仅 Receptionist 可操作"，但无操作级 `[Authorize]`，回落类级 `DoctorOrAdmin`：前台被挡、Doctor/Admin 反被放行，与 `personas:287`(Receptionist✅/Doctor❌/Admin❌) 三向倒置。
→ 补操作级策略。

---

## 四、🟡 重要问题（边界模糊/部分闭环）

| # | 类型 | 问题 |
|---|---|---|
| I1 | 💻 | `CanManageUser` 双副本漂移：WebAPI(`:547-560`) vs LocalWebAPI(`:539-550`) 签名不同（Local 缺 `isSysAdmin` 参数）→ sysadmin 本地模式经此路径误判 |
| I2 | 💻 | JWT claim 名不一致：`AuthController.cs:76`/`LocalJwtConfig.cs:87` 写 `"IsSysAdmin"`，`UsersController.cs:107` 读 `"IsSuperAdmin"`，永远取不到 → 兜底死代码 |
| I3 | 💻 | 首登强制改密未实现：`ForceChangeOnFirstLogin=true` 仅 appsettings 值，登录链路无 Filter/中间件检查；`ApplicationUser.MustChangedOnNextLogin` 字段既不写也不读 → US-SHELL-011 Step1 失基 |
| I4 | 💻 | US-SHELL-011 向导闭环断：`FirstRunSetupViewModel` 仅连接配置，5 步向导未实现；安装→向导→建 admin→交权链从第二步起断 |
| I5 | 💻 | Admin→医案 设计收敛但 D1 审计未补：审核移除后 Admin 仅状态变更，但 D1 SecurityAuditLog/MedicalCaseAuditLog 实体已删未补 → 追溯仍断 |
| I6 | 📄 | Doctor→前台就诊完成通知未设计：SignalR(ADR-0013) 仅挂号→医生单向；医生完成/取消→前台通知未设计，前台靠"队列不再出现"被动推断 |
| I7 | 💻 | `FormulasController.cs:70 GetById` 无所有权检查：`personas:174` 把缺陷归为"Admin 读他人非共享"，但 Admin 按设计可读全部；真正越权风险是 **Doctor** 经 GetById 读他人私有验方 → 问题归因错位，应补 `IsAdminOrOwner` |
| I8 | 💻 | `ReceptionistRoleDefinition.cs:17-22` 无 HerbsModule：即使 Controller 权限修复，前台 UI 仍无药材查询入口 → 若确认可查药材，补 HerbsModule（只读） |
| I9 | 💻 | `DatabaseInitializationService.cs:162-170` 查询条件错位：用 `UserRole.SuperAdmin` 找 sysadmin，若库存在 `Role=SuperAdmin && IsSysAdmin=false` 的普通超管会被误升级 → 改查 `IsSysAdmin==true` |
| I10 | 💻 | `IdentitySeedData` 重置语义双轨：line 51-66 不读 `ForceResetOnStartup`，dev 每次 LastLoginAt==null 即重置；与 `SystemAdminOptions.ForceResetOnStartup`(仅 dev) 语义重叠不一致 |

---

## 五、🔵 次要问题（未同步/历史残留）

| # | 问题 |
|---|---|
| S1 | 📄 A7 报表时间参数未实现却标 ✅：`ReportsController` 无 startDate/endDate，`reports.md` 标"已实现（待扩展）"状态虚高（US-REPORT AC 要求时间范围）→ 降级 🚧 |
| S2 | 📄 SuperAdmin vs Sysadmin 双列冗余：matrix 两列几乎全✅重叠；personas:67/115 sysadmin 默认即 SuperAdmin 角色+IsSysAdmin → 合并列或明确"角色获权 + 布尔提供不可删保护" |
| S3 | 📄 "8 交接点 0 闭环"过时：实际 #7(任何→Sysadmin)✅；审核移除/双模式/sysadmin 配置已收敛 #5/#3 设计模糊 → 重评为"1 闭环 + 设计收敛 3 + 代码待对齐 4" |
| S4 | 📄 `DoctorOnly` 历史残留：`personas:237`"打印仅 Doctor(DoctorOnly 策略)"——PolicyConstants 无此策略；打印权限实际未在 Controller 层强制（无 PrintController [Authorize]） |
| S5 | 📄 QuickVisit"死代码"过时：API 已实现（RegistrationsController:44-94），仅 Desktop 接线缺 → 改"API 已实现，Desktop 待激活" |
| S6 | 📄 `AdminOnly ≡ AdminOrSuperAdmin` 冗余：matrix:75-76 自承认等价；代码注册两者均=Admin+SuperAdmin → 合并 |
| S7 | 📄 `personas:395` 首页视图名错：列 sysadmin=AdminHome；代码 `SuperAdminRoleDefinition.cs:27`=`ViewNames.SysadminHome` |
| S8 | 📄 `06-operations/02-configuration.md:280-290` SystemAdmin 节漏字段：缺 `AllowAutoCreateInProduction`/`InitialSetupToken`（SystemAdminOptions.cs 实有） |
| S9 | 💻 `DefaultPasswordService.IsDefaultPasswordAllowed/ShouldForcePasswordChange` 疑似 dead code（无生产调用方）→ 核实 |
| S10 | 📄 personas 未同步 D8/审核移除等：Receptionist 段未提 D8 StartVisit bug；未声明"v1.0 前台无审核职责" |

---

## 六、角色逐个评估

### Receptionist（前台）— 健康度 D
- **职能**：挂号/建档/读卡/药材查询（定义清晰）
- **边界**：🔴 三 Controller `DoctorOrAdmin` 阻断本职（D7 未修）；Cancel 权限三向倒置；本地 LocalWebAPI 无策略
- **闭环**：🔴 接诊链断（StartVisit D8）；就诊完成→前台反向通知未设计
- **结论**：代码层"前台角色基本不可用"（personas:262 自评准确）；D7+D8 是解锁关键

### Doctor（医生）— 健康度 D
- **职能**：唯一医案创建者/诊断开方/打印/QuickVisit（定义清晰）
- **边界**：🔴 缺 DoctorOnly 策略，Admin 经 DoctorOrAdmin 越界创建医案；QuickVisit Admin 也能调
- **闭环**：🔴 StartVisit D8；复诊历史聚合(MC-008/009) Server 无端点；打印回写(D2)无落点
- **结论**：核心卖点（复诊秒查/打印追溯）闭环断裂；需 DoctorOnly + D8/D2/D9

### Admin（业务管理员）— 健康度 C
- **职能**：药材库初始化/用户管理/验方/报表（审核移除后清晰）
- **边界**：🟡 矩阵三文档矛盾；用户硬删违范式；Formulas GetById 归因错位
- **闭环**：🟡 Excel 导入(D6)全缺→初始化药材库断；Restore(D4)四模块缺→数据维护断
- **结论**：审核移除收敛了职责，但初始化/恢复两大职能闭环未达

### Sysadmin（运维）— 健康度 D
- **职能**：部署/初始化/配置/备份/审计（定义清晰，ADR-0014 双模式配置到位）
- **边界**：🔴 生产门控(K4)失效；种子矛盾(K5)；CanManageUser 双副本(I1)；JWT claim(I2)
- **闭环**：🔴 US-SHELL-011 向导未实现；首改密未实现(I3)；审计日志(D1)未补
- **结论**：设计层（ADR-0013/0014）完善，但信任根（sysadmin）的创建/保护链多处断裂，生产部署安全风险高

---

## 七、修复优先级建议

| 优先级 | 范围 | 理由 |
|---|---|---|
| **P0 阻断** | K1 策略定义矛盾 | D7 修复前必须先纠正，否则锁出 Admin/SuperAdmin（制造新故障） |
| **P0 阻断** | K4 生产门控失效 + K5 种子矛盾 | 信任根安全，公网部署前必修 |
| **P0 阻断** | K7 接诊链(D8) + K2 权限矩阵矛盾 | 核心业务闭环 + 三文档权威统一 |
| P1 严重 | K3 DoctorOnly + K6 Restore + K8 本地策略 + K9 Cancel | 角色边界正确性 |
| P1 严重 | I1-I5（CanManageUser/JWT claim/首改密/向导/审计） | sysadmin 信任链闭环 |
| P2 重要 | I6-I10 + S1-S10 | 边界细化/历史同步 |

---

## 八、核心结论

1. **文档层（R10/sysadmin/审核移除/双模式）设计已收敛**，多数历史模糊已解决。
2. **代码层 D7/D8 零进展**，是当前角色闭环断裂的根因。
3. **最危险是 K1**：`DoctorOrReceptionist` 策略定义与文档矛盾——D7 实施前必须先扩策略注册（加 Admin/SuperAdmin），否则切换后锁出管理员。
4. **信任根（sysadmin）链多处断裂**（K4/K5/I1/I2/I3/I4），公网部署前必修。
5. 建议执行序：**K1 先纠正策略 → K2 冻结权限矩阵权威 → K4/K5 信任根 → K7 接诊链 → 其余 D7/D8/DoctorOnly/Restore**。

> 本报告为审查交付物，未修改文档/代码。是否进入修复阶段？
