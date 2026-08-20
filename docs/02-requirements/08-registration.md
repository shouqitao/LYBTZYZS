# 挂号管理 (Registration Management)
> 版本: v1.0 | 日期: 2026-08-20

> 挂号（Registration）是患者就诊流程的系统化入口，用于管理患者分流、排队顺序和就诊可追溯性。系统支持**两种来源模式**：前台模式（Source=Receptionist）与医生模式（Source=Doctor）。挂号与医案是**独立实体**——挂号记录排队关系，医案记录诊疗内容，通过 `MedicalCaseId` 关联；**医案由医生接诊时原子创建**（2026-08-03 决策：接诊即建，见 [07-medical-cases.md BR-000](07-medical-cases.md)）。
>
> | 我是… | 我能… |
> |-------|-------|
> | 前台 | 帮患者挂号、取消挂号、查看排队 |
> | 医生 | 看待诊清单、接诊（开始就诊）、快速看诊（跳过排队） |
> | 管理员 | 查看所有挂号记录（只读） |

---

## 模块级设计（横切）

### 双模式工作流

> 双模式工作流（远程/本地分野）设计见 [ADR-0013（SignalR 实时推送）](../03-architecture/decisions/0013-signalr-realtime-push.md)。此处的「双模式」指**部署模式**（远程/本地）下的工作流分野，区别于「两源模型」——后者描述同一远程模式内按 `Source` 字段的来源区分。

**远程模式（有前台，挂号驱动）**：

```
前台建档(首诊) + 挂号(Waiting) → 待诊队列
        ↓ SignalR 推送通知医生（仅远程，见 ADR-0013）
医生待诊列表 → 选患者 →「开始就诊」StartVisit（US-REG-005）
        ↓ 原子创建：Registration(InProgress) + MedicalCase(Active)，返回 MedicalCaseId（2026-08-03 决策：接诊即建）
医生进入诊疗（医案已建，直接编辑诊断/处方）
        ↓
望闻问切 → 开方 → 打印（系统终点）

退号场景：患者退号（Waiting 时）→ Registration→Cancelled，无医案产生
          医生接诊后觉得没问题 → 取消医案 → Registration 回退 Waiting（US-REG-007 Source-aware）→ 前台退号

急诊/特殊通道（US-REG-002 两步）：医生 POST /Registrations（Source=Doctor 建 Waiting）→ PUT /start-visit 接诊（Waiting→InProgress + 原子建 MedicalCase(Active)）
        ↓ 跳转医案编辑
医生看诊（医案已建）
```

**要素**：前台/医生建号（POST /Registrations——Source 区分）；待诊队列；SignalR 推送（仅远程）；StartVisit 接诊（Waiting→InProgress + 原子建 MedicalCase(Active)——接诊即建 BR-000）；两步收敛（2026-08-13——quick-visit 端点已删）。

**本地模式（仅医生使用）**：

```
患者到诊 → 医生选/建患者（Patient）→ 系统自动创建 Registration(Source=Doctor, InProgress) + MedicalCase(Active) → 看诊 → 打印
```

**要素**：

- **本地模式仅医生使用**（2026-08-13 两步收敛）：来一个看一个——POST /Registrations（Source=Doctor 建 Waiting）→ start-visit 接诊（Waiting→InProgress + 原子建 MedicalCase(Active)）；Registration 由系统自动创建以保持数据模型统一（医生无感）
- **数据模型完整但实际不创建前台账号**：本地数据库 Registration.Source=Receptionist 字段保留（模型统一），但 Admin 不在本地创建前台用户 → 本地前台挂号/退号入口自然不出现
- **无待诊队列**：仅医生独立使用，待诊清单恒空
- **无 SignalR**：本地无队列推送需求
- 本地模式 = 医生应急/外出工具；远程模式承载多角色协同。差异为产品定位裁决，非配置可调

**模式适用性**：Registration 模块在本地**仅医生使用**——前台挂号/退号入口不出现（无前台用户）；Shell 按登录角色加载模块（US-SHELL-003）天然处理。

### 两源模型

| 属性 | 前台模式（Source=Receptionist） | 医生模式（Source=Doctor） |
|------|------------------------------|--------------------------|
| 创建者 | Receptionist | Doctor |
| 初始状态 | Waiting（排队） | Waiting（排队） |
| 是否进入队列 | 是 | 是 |
| 医案创建时机 | 医生接诊时 | StartVisit 时（接诊即建） |
| 取消策略 | 前台手动取消 | 医案取消自动闭环 |

> **注（2026-08-13 两步改造修订）**：医生模式初始状态已改为 `Waiting`（两步：先建 Waiting 挂号，再 start-visit 接诊）——InProgress 后置到真正接诊时。

### 状态流

```
前台模式: Waiting → InProgress → Completed
             ↓
         Cancelled

医生模式: Waiting → InProgress → Completed
             ↓
         Cancelled
```

### 业务规则清单

| 编号 | 规则 | 说明 |
|------|------|------|
| REG-BR-001 | 取消前置校验 | 仅 Status=Waiting 的挂号可取消；有活跃/已完成医案时拒绝取消 |
| REG-BR-002 | 前台取消权限 | Source=Receptionist 的挂号仅 Receptionist 可取消，Doctor 无权 |
| REG-BR-003 | 医生模式跳过 Waiting | Source=Doctor 创建时直接进入 InProgress，不经过队列（2026-08-13 修订：改为先 Waiting 再 start-visit） |
| REG-BR-004 | 患者不存在时创建 | 查询无结果时提示创建患者 |
| REG-BR-005 | 回退后重建医案（2026-08-03 修订） | Source=Receptionist 医案取消（物理删除）后 Registration 回退 Waiting；患者回来时医生重新接诊 → **新建**医案（取消=物理删除，无原医案可恢复） |
| REG-BR-006 | 患者侧大屏叫号（R9 决策） | 患者侧候诊大屏叫号属 **v2.0 / 按需**，v1.0 不实现；v1.0 候诊队列仅前台端（US-REG-004）与医生端可见 |
| REG-BR-007 | 当天重复挂号检查 | 患者当天已有未完成挂号时，提示不能重复挂号 |
| REG-BR-008 | 前台仅退当天挂号 | 前台只能退当天的 Status=Waiting 挂号；非当天的需管理员退款 |
| REG-BR-009 | 挂号费跟医生相关 | 挂号费跟医生相关（`ApplicationUser.RegistrationFee`，Admin 设置），创建挂号时自动带出（前台/医生/本地），退号时按实际退；免号填 0。`Registration.RegistrationFee` 字段存储开单时的费用快照（非关联查询），退号/报表均读此字段 |
| REG-BR-010 | 换医生流程 | 先取消原挂号（退费），再重新挂号到新医生（收费） |
| REG-BR-011 | 开始看诊并发保护 | 医生点"开始看诊"时检查挂号状态，防止前台退号同时医生接诊 |
| REG-BR-012 | 医生待诊列表仅显示当天 | 医生待诊列表仅显示当天的 Waiting 挂号，非当天的不显示 |

### 接诊即建原子性（2026-08-13 两步收敛——quick-visit 端点已删）

医生快速就诊（US-REG-002）两步：① POST /Registrations（Source=Doctor，建 Waiting 挂号）② PUT /Registrations/{id}/start-visit（Waiting→InProgress + **原子创建 MedicalCase(Active)**——`StartVisitCommandHandler` 事务内建医案，失败回滚挂号状态）。InProgress 后置——断网残留 Waiting 可被待诊列表捕捉 → 自愈（产品决策）。

### 并发保护

| 约束 | 规则 |
|------|------|
| PatientId+Date+Status 唯一约束 | 同一患者同一日期只允许一条活跃挂号（Status=Waiting 或 InProgress），防止重复挂号 |

### 用户角色权限

| 角色 | 主要操作 |
|------|---------|
| Receptionist (0) | 创建挂号（Waiting）、取消挂号、查看全部队列 |
| Doctor (1) | 查看个人队列、从队列接诊、医生两步建号看诊（POST + start-visit） |
| Admin (10) | 查看全部队列和历史（只读统计） |
| SuperAdmin (100) | 与 Admin 相同（只读监控） |

> **权限细化（2026-08-03 决策）**：Admin 只读查看挂号（列表/详情/队列），不可创建/取消/接诊；QuickVisit 仅 Doctor（现 `DoctorOrAdmin` 需收为 `DoctorOnly`）；创建仅 Receptionist；取消仅 Receptionist（REG-BR-002）。代码待按操作级细分（见 04-permissions P1-4）。

### 挂号费（2026-08-03 决策：医生实体加字段，创建时带出）

- `ApplicationUser.RegistrationFee` 字段，Admin 创建/编辑医生时设置（默认 0）
- 前台创建挂号：自动带出医生挂号费（可改，用于义诊/优惠）
- 医生两步建号 / 本地模式自动建挂号：自动带出医生挂号费（免号时填 0）
- 退号：按实际记录的 RegistrationFee 退（REG-BR-008/009）
- 收入报表 `RegistrationFeeTotal` 覆盖全部来源（前台 + 医生建号 + 本地），本地模式同样统计挂号收入

---

## US-REG-001: 前台创建挂号（Waiting 排队）

**角色**: 前台接待员
**优先级**: Must
**状态**: ✅ 已实现（患者校验 + 挂号费带出）

**作为** 前台接待员，**我想要** 查询患者并创建挂号记录，**以便** 患者进入等待队列，医生可以按序接诊。

**验收标准**:

- [ ] 前台可通过姓名/拼音码/身份证号查询患者
- [ ] 患者不存在时提示是否创建新患者（REG-BR-004）
- [ ] 选择创建：补充必填信息（姓名、联系号码、家庭住址、身份证）后创建患者，返回挂号界面
- [ ] 选择患者后，指派医生（从可用医生列表选择）
- [ ] 创建 Registration：`Source=Receptionist`、`Status=Waiting`
- [ ] 仅 Receptionist 角色可操作
- [ ] 患者 `Status=Disabled` → 返回 422（REG-70005）
- [ ] 患者当天已有未完成挂号时，提示不能重复挂号

**业务规则**:

1. Source=Receptionist，Status=Waiting（进入排队队列）
2. 指派医生（UserId）必填
3. 患者必须存在且 Enabled
4. 同一患者同日唯一活跃挂号约束（并发保护）
5. 仅 Receptionist 可创建
6. **当天检查**：患者当天已有未完成挂号时，提示不能重复挂号
7. **挂号费**：挂号费跟医生相关，创建挂号时收取

**患者必填字段**:

- 姓名
- 联系号码
- 家庭住址
- 身份证

**边界条件**:

- 同一患者同一天由前台创建第二条 Waiting 挂号 → 提示不能重复挂号（REG-BR-007）
- 同一患者同一天已有 Waiting 挂号，医生两步建号 → 提示不能重复挂号（REG-BR-007）
- 同一患者同一天已有 Cancelled 挂号，再次创建 Waiting 挂号 → 允许（Cancelled 不参与唯一约束）
- 患者当天已有 Waiting 挂号，前台可查看但不可重复挂号
- 患者非当天有 Waiting 挂号，前台可正常挂号（不阻塞）

**实现参考**: `RegistrationsController.cs`、`IRegistrationService`

---

## US-REG-002: 医生快速就诊（QuickVisit 两步流程）

**角色**: 医生
**优先级**: Must
**状态**: 🔧 设计修订（2026-08-13：从一步原子改为两步——InProgress 后置，断网自愈；服务端已实现，Desktop 待接线）

> **定位补注**：QuickVisit 承担双重职能——(1) 远程模式下的**急诊/特殊通道**（前台不在或急症时医生直接接诊）；(2) 本地模式**无前台用户时**的常规看诊入口（医生独立「来一个看一个」本质即 QuickVisit；若 Admin 建前台用户则前台挂号→StartVisit 链同样可用）。

**作为** 医生，**我想要** 选择患者后直接进入看诊，**以便** 不需要额外的挂号步骤，前台不在时也能快速开始。

**验收标准**:

- [ ] 医生可通过姓名/拼音码/身份证号查询患者
- [ ] 患者不存在时提示是否创建新患者（REG-BR-004）
- [ ] **UI 双入口（2026-08-13 定案）**：医生选中患者后——「挂号」= 正常挂号单逻辑（可指定医生，UI 设计时深入）；「快速看诊」= 默认当前医生（快捷高效措施）
- [ ] **第 1 步**：快速看诊 → 系统自动创建 Registration：`Source=Doctor`、`Status=Waiting`、`DoctorId=当前医生`（REG-BR-003 修订——不再跳过 Waiting；doctorId 强制=当前医生，不可选他人）
- [ ] **第 2 步**：调用 StartVisit（US-REG-005）→ Registration→InProgress + 原子创建 MedicalCase(Active)，关联 RegistrationId
- [ ] 第 1 步成功第 2 步断网 → 挂号停留 Waiting → **待诊列表可捕捉 → 医生重试接诊（自愈）**
- [ ] 前端 VM 封装「一键快速看诊」：两步串行调用，医生无感知（REG-BR-006）

**业务规则**:

1. Source=Doctor，**第 1 步 Status=Waiting**（2026-08-13 修订：原一步原子 InProgress 改为两步——InProgress 后置到真正接诊时，断网残留 Waiting 可自愈）
2. 第 2 步复用 StartVisit（原子创建 MedicalCase + InProgress）——**与普通挂号流程收敛**
3. 受 BR-001 单活跃医案约束（[07-medical-cases.md](07-medical-cases.md)）
4. API 单一职能：建挂号（POST /Registrations）与开始就诊（PUT /start-visit）各自独立——**组合由前端 VM 编排**

**实现参考**: `RegistrationsController.cs`、`IRegistrationService`（注入 `IMedicalCaseCommandService`）

---

## US-REG-003: 查看挂号详情

**角色**: 前台接待员、医生、管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 前台或医生，**我想要** 查看单条挂号的完整信息，**以便** 了解患者指派、关联医案和状态流转。

**验收标准**:

- [ ] 有效 ID → 返回 `RegistrationDetailDto`（含患者姓名、医生姓名、状态、来源、关联医案 ID）
- [ ] 挂号不存在 → 返回 404（REG-70001）
- [ ] 关联医案 ID 可选（未接诊时为 null）

**业务规则**:

1. 返回挂号完整信息
2. 包含关联医案 ID（MedicalCaseId，接诊前为 null）
3. 包含患者姓名、医生姓名（冗余快照）
4. 权限：Receptionist/Doctor/Admin/SuperAdmin 均可查看

**实现参考**: `RegistrationsController.cs`

---

## US-REG-004: 分页查询挂号 + 查看排队

**角色**: 前台接待员、医生、管理员
**优先级**: Must
**状态**: ✅ 已实现（队列当天过滤）

**作为** 医生，**我想要** 查看当前等待接诊的患者队列，**以便** 按序选择患者开始看诊。

**验收标准**:

- [ ] 显示所有 `Status=Waiting` 且 `DoctorId=当前医生` 的挂号记录（Doctor 视图）
- [ ] Receptionist 可查看全部医生的队列（只读）
- [ ] 列表信息：挂号号码、姓名、手机尾号4位
- [ ] 按挂号时间升序排列（先到先诊）
- [ ] 支持按日期范围、患者、医生、状态筛选
- [ ] 显示挂号时间、患者、医生、Source、Status、关联医案编号

**业务规则**:

1. Doctor 查看个人队列（Waiting 且 DoctorId=自己）
2. Receptionist/Admin 查看全部队列（只读）
3. 按挂号时间升序排列（先到先诊）
4. 列表信息含挂号号码、姓名、手机尾号4位
5. 状态着色：Waiting=黄色、InProgress=蓝色、Completed=灰色、Cancelled=红色
6. **仅显示当天的 Waiting 挂号**：非当天的 Waiting 挂号不显示在医生待诊列表

**实现参考**: `RegistrationsController.cs`、`IRegistrationRepository`

---

## US-REG-005: 开始就诊（Waiting→InProgress）

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现（StartVisit 原子建医案+回退）

**作为** 医生，**我想要** 从队列选中患者开始就诊，**以便** 系统自动创建医案并将挂号状态转为进行中。

**验收标准**:

- [ ] 医生选中 Waiting 挂号 → 自动创建 MedicalCase，Registration 状态转为 InProgress
- [ ] Registration.MedicalCaseId 填充为新建医案 ID
- [ ] 若患者已有活跃医案 → 触发 BR-001 碰撞处理（[07-medical-cases.md](07-medical-cases.md)）
- [ ] **开始看诊时检查挂号状态**：防止并发问题（前台退号同时医生点开始看诊）

**业务规则**:

1. Waiting → InProgress 状态转换
2. 同时创建 MedicalCase 并关联 RegistrationId
3. 受 BR-001 单活跃医案约束
4. 医案创建后医生进入诊疗工作流
5. **并发保护**：开始看诊时检查挂号状态，如果挂号已变为 Cancelled，提示"患者已退号"

**实现参考**: `RegistrationsController.cs`、`IRegistrationService`

---

## US-REG-006: 取消挂号（仅当天 Waiting）

**角色**: 前台接待员
**优先级**: Must
**状态**: ✅ 已实现（服务端守卫：Waiting-only + 关联医案拒绝）

**作为** 前台接待员，**我想要** 取消等待中的挂号记录，**以便** 患者临时不看或医生建议取消时能正确处理。

**验收标准**:

- [ ] 仅 Receptionist 可取消 `Source=Receptionist` 的挂号（REG-BR-002）
- [ ] 仅 `Status=Waiting` 的挂号可取消（REG-BR-001）
- [ ] **仅当天的 Waiting 挂号可取消**：非当天的 Waiting 挂号，前台提醒患者联系管理员退款
- [ ] 取消前校验：无关联医案（医案取消=物理删除，不留 Cancelled 状态）
- [ ] 有 Active/Suspended/Completed 医案时拒绝取消，提示原因（REG-70003）
- [ ] 取消后 Status → Cancelled
- [ ] 取消后自动退挂号费（按医生挂号费）
- [ ] Doctor 无权执行此操作（REG-70004）

**业务规则**:

1. **REG-BR-001 取消前置校验**：无关联医案（医案取消=物理删除，不留 Cancelled 状态），否则拒绝取消
2. **REG-BR-002 前台取消权限**：Source=Receptionist 的挂号仅 Receptionist 可取消
3. 仅 `Status=Waiting` 可取消（InProgress 的挂号需先取消医案联动处理）
4. **仅当天的 Waiting 挂号可取消**：非当天的 Waiting 挂号需管理员退款
5. 取消后 Status=Cancelled
6. **退挂号费**：取消时自动退挂号费（按医生挂号费）

**边界条件**:

- Status=InProgress 的挂号不可直接取消（REG-BR-001），需先取消关联医案
- 医生模式（Source=Doctor）InProgress 挂号 → 取消医案后 Registration 自动变为 Cancelled（US-REG-007 闭环）
- 前台模式（Source=Receptionist）InProgress 挂号 → 取消医案后 Registration 回退为 Waiting（US-REG-007）

**实现参考**: `RegistrationsController.cs`、`IRegistrationService`

---

## US-REG-007: 医案联动（完成/取消自动回写）

**角色**: 系统
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统，**我想要** 医案状态变更时自动更新关联 Registration 状态，**以便** 挂号记录与医案状态保持一致，无需人工操作。

**验收标准**:

- [ ] MedicalCase 完成时（CaseStatus=Completed）→ 关联 Registration.Status 自动变为 Completed
- [ ] MedicalCase 取消时（物理删除）→ 根据 Source 执行不同策略：
  - Source=Receptionist：Registration.Status 回退为 Waiting（等前台取消），MedicalCaseId 清空（原医案已物理删，重新接诊时新建，2026-08-03 修订）
  - Source=Doctor：Registration.Status 自动变为 Cancelled（流程完全闭环）
- [ ] 适用于所有 Source 类型
- [ ] 联动在 MedicalCaseService 内部触发，无需人工操作

**业务规则**:

1. **完成联动**：MedicalCase.CompleteAsync() 内部调用 RegistrationService.CompleteByMedicalCase()，Registration → Completed
2. **取消联动（Source-aware）**：MedicalCase.CancelAsync() 内部根据 Source 调用不同方法
   - Source=Receptionist：回退为 Waiting，MedicalCaseId 清空（2026-08-03：原医案已物理删除，患者回来重新接诊时新建）
   - Source=Doctor：自动变为 Cancelled（闭环）
3. 联动在事务内执行，保证一致性

**实现参考**: `RegistrationsController.cs`、`IMedicalCaseFacade.cs`

---

## US-REG-008: 医生工作台待诊列表实时更新

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现（Server RegistrationHub + Desktop SignalRClient+轮询降级）

**作为** 医生，**我想要** 工作台待诊列表实时刷新，**以便** 第一时间看到新挂号和状态变更，无需手动刷新。

**验收标准**:

- [ ] 新挂号（指派给当前医生）创建后 N 秒内自动出现在医生工作台待诊列表（N 待专项 spec 定义，目标 ≤ 3 秒）
- [ ] 挂号状态变更（开始就诊 / 取消）实时同步到相关医生的待诊列表
- [ ] 推送通道失败时，医生端自动降级为轮询（复用 [US-REG-004](#us-reg-004-分页查询挂号--查看排队) 候诊队列接口），保证列表最终一致
- [ ] 仅推送与当前医生相关的事件（DoctorId 过滤），不越权推送

**业务规则**:

1. 依赖 SignalR 推送通道，架构决策见 [ADR-0013](../03-architecture/decisions/0013-signalr-realtime-push.md)
2. 推送内容：新挂号、挂号状态变更（开始就诊 / 取消）
3. 降级策略：推送失败回退轮询，复用 US-REG-004 候诊队列
4. 双模式推送机制（远程 Hub / 本地模式）待 SignalR 专项 spec 定案，本 US 仅锚定范围

**双模式差异**: 远程由 SignalR Hub（部署于 WebAPI，公网可达）推送挂号变更事件；本地待专项 spec 定案（候选：LocalWebAPI 内嵌 Hub / 前端轮询降级）。

**实现参考**: SignalR Hub（待专项 spec 设计，见 [ADR-0013](../03-architecture/decisions/0013-signalr-realtime-push.md)）

---

## 本地模式设计（横切）

**核心逻辑**：本地模式不使用前台账号，医生直接看诊。

**流程**：

1. 医生从患者库查询或新建患者
2. 选中患者后，系统自动创建 Registration（Source=Doctor, Status=InProgress）
3. 医生开始看诊

**特点**：

- 无待诊队列（大家自觉排队）
- 无前台账号
- 医案有挂号人字段，记录医生
- UI 设计可以隐藏待诊列表或保持空着

---

## 交叉引用

- [医案管理 BR-001 单活跃医案约束](07-medical-cases.md)（医生建号两步受此约束）
- [医案管理 US-MC-011 完成医案](07-medical-cases.md)（完成联动触发）
- [医案管理 US-MC-014 取消医案](07-medical-cases.md)（取消联动 Source-aware 策略）
- [患者管理](04-patients.md)（挂号依赖患者存在）
- [用户管理](03-users.md)（医生列表 Role=Doctor）
- [术语表 Registration = 挂号](../01-product/03-glossary.md)

---

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-06-28 | 新增「双模式工作流」段（远程挂号驱动 / 本地默认医生独立来一个看一个，建前台用户则挂号可用，全角色支持）；US-REG-002 状态改 🧲 v1.0 待激活+定位补注；US-REG-005 D8 修复方向补注 |
| 2026-08-03 | **REG-BR-005 修订**：回退后恢复原医案 → 回退后重建（取消=物理删除，MedicalCaseId 清空，重新接诊时新建）；US-REG-007 取消联动同步 |
| 2026-08-13 | **US-REG-002 QuickVisit 两步流程修订**：从一步原子（InProgress）改为 ①建 Waiting 挂号 ②StartVisit 接诊（InProgress+医案）；断网自愈；与普通挂号收敛 |
| 2026-08-03 | **「接诊即建」决策落地**：模块概述/双模式工作流（远程+本地）/US-REG-005 状态（🔴→✅ 设计已确认）同步为 StartVisit/QuickVisit 原子创建 MedicalCase(Active)+Registration(InProgress) |
| 2026-06-28 | 本地模式表述修正：「取消挂号/Registration 不激活」改为「全角色支持，差异由用户配置决定（无前台用户时医生独立，建前台用户则挂号可用）；无 SignalR」 |
| 2026-06-25 | 补充 InProgress 取消、同日重复挂号边界条件验收标准 |
| 2026-06-28 | 补 REG-BR-006 患者侧大屏叫号业务规则（R9：v2.0/按需，v1.0 候诊队列仅前台/医生端） |
