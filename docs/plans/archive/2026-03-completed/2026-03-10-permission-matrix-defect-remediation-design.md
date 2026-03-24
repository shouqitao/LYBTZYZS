# 角色权限矩阵缺陷修复设计

> **版本**: v1.0
> **创建日期**: 2026-03-10
> **定位**: 针对 role-permission-matrix.md v1.1 架构审查发现的 11 个问题的修复设计
> **前置**: role-permission-matrix.md v1.1 (审查基准)

---

## 问题索引

| 编号 | 类别 | 严重程度 | 简述 | 设计章节 |
|------|------|---------|------|---------|
| D-4 | 逻辑缺陷 | HIGH | Receptionist 医案可见性无 API 端点支撑 | 2.1 |
| D-5 | 逻辑缺陷 | MEDIUM | Formula/Herb 归属字段命名不一致 | 2.2 |
| D-6 | 逻辑缺陷 | LOW | SuperAdmin 挂号权限"推断"状态 | 2.3 |
| I-1 | PRD不一致 | MEDIUM | Herb PRD 主表遗漏 Doctor 编辑/删除权限 | 3.1 |
| I-2 | PRD不一致 | HIGH | "当天可编辑"缺乏精确定义 | 3.2 |
| G-7 | 设计不足 | MEDIUM | Admin 无法创建挂号 | 4.1 |
| G-8 | 设计不足 | LOW | Doctor 无法禁用患者 | 4.2 |
| G-9 | 设计不足 | HIGH | Registration 回退后流程未定义 | 4.3 |
| G-10 | 设计不足 | MEDIUM | Admin 互管死锁 + SuperAdmin 恢复机制 | 4.4 |
| G-11 | 设计不足 | MEDIUM | 医生禁用后 Waiting 挂号处理 | 4.5 |
| G-12 | 设计不足 | MEDIUM | 软删除恢复后状态未定义 | 4.6 |

---

## 1. 设计原则

1. **最小变更**: 优先修复文档和规则定义，避免引入新的架构复杂度
2. **向前兼容**: 修复不破坏现有已实现模块的行为
3. **明确优于隐含**: 消除所有"推断"和"待确认"标注，每个权限必须有明确定义
4. **场景驱动**: 每个设计方案必须覆盖触发场景、异常路径、边界条件

---

## 2. 逻辑缺陷修复

### 2.1 D-4: Receptionist 医案可见性 (HIGH)

**问题**: 矩阵声明 Receptionist 可查看医案"简要提示"，但 `/medicalcases` 端点受 `DoctorOrAdmin` 策略保护，Receptionist 无法访问。

**方案: 通过 Registration 模块间接提供**

不新增 medicalcases 端点，而是通过 Registration 队列 API 附带必要的医案状态信息。

**理由**:
- Receptionist 的核心职责是管理挂号队列，不需要独立访问医案模块
- 简要提示 (创建时间 + 主治医生) 本质上是 Registration 已有数据 (CreatedAt + DoctorId)
- 额外需要的信息仅有"是否正在看诊"，即 Registration.Status 本身

**设计**:

```
GET /api/registrations/queue  (PatientAccess 策略)

Response RegistrationQueueItemDto:
{
  "id": "guid",
  "patientName": "张三",
  "doctorName": "李医生",
  "createdAt": "2026-03-10T09:00:00",
  "status": "Waiting|InProgress|Completed",
  "waitingMinutes": 15,           // 计算字段: Now - CreatedAt
  "hasMedicalCase": true           // MedicalCaseId.HasValue
}
```

**矩阵修复**:
- Section 3.2 MedicalCase Receptionist 行: 改为 "无直接访问权限; 通过挂号队列间接查看就诊状态 (Registration.Status + DoctorName)"
- Section 5.1 Receptionist 医案行: 改为 "无直接权限 (通过 Registration 间接获取: 队列状态 + 等待时长)"
- Section 2.1 API 策略: 无需修改 (`/medicalcases` 保持 DoctorOrAdmin)

**影响范围**: 仅文档修改，无代码变更 (Registration API 已包含上述字段)

---

### 2.2 D-5: 归属字段命名不一致 (MEDIUM)

**问题**: Herb 用 `BaseEntity.CreatedBy`，Formula 用 `Formula.UserId`，两种字段做同一件事 (归属检查)。

**方案: 统一语义 + 文档澄清**

**现状分析**:

| 模块 | 归属字段 | 写入时机 | 可被修改 | 用途 |
|------|---------|---------|---------|------|
| Herb | BaseEntity.CreatedBy | 创建时自动填充 (审计) | 否 (审计字段不可变) | 归属检查 |
| Formula | Formula.UserId | 创建时显式写入 | 否 (创建时写入后不变) | 归属检查 |
| MedicalCase | MedicalCase.UserId | 创建时显式写入 | 否 | 归属检查 |

两种字段的值在正常流程中始终相等 (创建者 = 当前登录用户)。差异的根源是 Herb 没有显式 UserId 字段，复用了 BaseEntity 的审计字段。

**设计决策: 保持现状，文档统一描述**

不为 Herb 新增 UserId 字段 (YAGNI)，但在矩阵中统一描述:

```
| 模块 | 归属字段 | 归属语义 |
|------|---------|---------|
| Herb | CreatedBy (BaseEntity 审计字段) | 创建者 = 归属者，不可变 |
| Formula | UserId (业务字段) | 创建者 = 归属者，不可变 |
| MedicalCase | UserId (业务字段) | 创建者 = 归属者，不可变 |
```

**矩阵修复**: Section 3.1 归属检查总览表增加"归属语义"列，统一标注"创建者 = 归属者，不可变"

**风险备注**: 如果未来允许 Admin 代创建 (如代医生创建验方)，则 `CreatedBy` (=Admin) != 期望归属者 (=Doctor)。此时 Herb 需要新增显式 `OwnerId` 字段。当前 v1.0 无此需求，标记为 Future Risk。

---

### 2.3 D-6: SuperAdmin 挂号权限 (LOW)

**问题**: 矩阵标注 SuperAdmin 挂号权限为"推断"。

**方案: 明确定义为与 Admin 对等**

**设计决策**: SuperAdmin 在挂号模块的权限与 Admin 完全对等: 查看全部队列和历史 (只读)，不参与创建和取消操作。

**理由**:
- SuperAdmin 是系统管理角色，不参与日常诊疗流程
- 权限值层级 (USER-D04) 保证 SuperAdmin > Admin，对等权限符合层级继承原则
- registration.md v2.0 Section 2 已补充 SuperAdmin 行

**矩阵修复**:
- Section 3.2 Registration 表底部注释: 删除"推断"和"待 PRD 补充确认"
- 改为: "SuperAdmin 在挂号模块与 Admin 权限对等 (只读)，依据 registration.md v2.0 Section 2"

---

## 3. PRD 不一致修复

### 3.1 I-1: Herb PRD Target Users 表 (MEDIUM)

**问题**: herbs.md Section 2 主表仅写 Doctor = "查看药材、创建药材"，遗漏了编辑/删除自己药材的权限。

**修复**: 更新 herbs.md Section 2 Target Users 表:

```markdown
| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部药材 |
| Admin | CRUD 全部药材 |
| Doctor | 创建药材；编辑/删除/启用/禁用自己创建的药材；查看全部药材 |
| Receptionist | 无权限 |
```

**影响**: 仅 herbs.md 文档修改，矩阵本身无需变更 (矩阵描述已正确)

---

### 3.2 I-2: MedicalCase "当天可编辑" 精确定义 (HIGH)

**问题**: US-MC-007 "完成后当天内可编辑，隔天锁定" 缺乏精确规则。

**方案: 动态计算 IsLocked + 角色分层规则**

#### 3.2.1 锁定规则精确定义

```
IsLocked = (Status == Completed) AND (CompletedAt.Value.Date < DateTime.Today)
```

- **时区**: 服务器本地时间 (中国标准时间 UTC+8)
- **计算方式**: 查询时动态计算，不使用定时任务
- **实现位置**: `MedicalCaseService` 查询方法中计算，映射到 DTO 的 `IsLocked` 字段

#### 3.2.2 角色-状态-锁定 权限矩阵

| 角色 | 未完成 (Active/Suspended) | 已完成 + 未锁定 (当天) | 已完成 + 已锁定 (隔天+) |
|------|--------------------------|----------------------|----------------------|
| Doctor (自己的) | 可编辑 | **可编辑** | 403 |
| Doctor (他人的) | 403 | 403 | 403 |
| Admin | 可编辑 | 可编辑 (EditReason + 确认弹窗) | 可编辑 (EditReason + 确认弹窗) |
| SuperAdmin | 可编辑 | 可编辑 (EditReason + 确认弹窗) | 可编辑 (EditReason + 确认弹窗) |

**关键决策**:

| 决策 | 规则 | 理由 |
|------|------|------|
| MC-LOCK-01 | Doctor 当天可编辑自己的已完成医案，隔天锁定 403 | US-MC-007 "当天可编辑" 适用于 Doctor 自己的医案; 隔天由 IsLocked 锁定 |
| MC-LOCK-02 | Admin/SuperAdmin 可编辑任何已完成医案，不受 IsLocked 限制 | Admin 与 SuperAdmin 在医案编辑上权限对等，均为管理角色 |
| MC-LOCK-03 | 所有已完成医案编辑均需 EditReason + UI 确认弹窗 | EditReason 非空校验 + 写入 AuditLog; UI 确认弹窗提示"正在编辑已完成医案，将记入审计日志"，防止误操作 |

> IsLocked 的作用: 仅限制 Doctor。Admin/SuperAdmin 不受 IsLocked 影响。
> UI 确认弹窗: 所有角色编辑已完成医案时均触发 (含 Doctor 当天编辑)。

#### 3.2.3 API 行为

```
PUT /api/medicalcases/{id}

前置检查流程:
1. 认证检查 (401)
2. 角色策略检查 - DoctorOrAdmin (403)
3. 资源存在检查 (404)
4. 状态检查:
   a. Active/Suspended:
      - Doctor: 归属检查 (UserId == currentUser) -> 允许/403
      - Admin/SuperAdmin: 允许
   b. Completed + !IsLocked (当天):
      - Doctor: 归属检查 -> EditReason 非空检查 -> 允许/403/422
      - Admin/SuperAdmin: EditReason 非空检查 -> 允许/422
   c. Completed + IsLocked (隔天+):
      - Doctor: 403 (MC-LOCK-01)
      - Admin/SuperAdmin: EditReason 非空检查 -> 允许/422
```

**矩阵修复**:
- Section 3.2 MedicalCase 表: 将"编辑 (已完成)"行拆分为两行: "编辑 (已完成, 当天)" + "编辑 (已完成, 隔天+)"
- 删除底部 PRD 歧义注释，替换为精确规则引用

**PRD 修复**: medical-cases.md 新增 Decision Log 条目 MC-LOCK-01 ~ MC-LOCK-03

---

## 4. 设计不足补充

### 4.1 G-7: Admin 创建挂号能力 (MEDIUM)

**问题**: Receptionist 缺席时 Admin 无法代为挂号。

**方案: 不新增 -- 设计决策确认**

**决策**: Admin 不参与挂号操作。挂号是诊疗流程操作，不属于管理职能。

**理由**:
1. 角色职责分离: Admin 负责管理 (用户/数据/配置)，不介入日常诊疗流程
2. Receptionist 缺席时，Doctor 使用"快速看诊"模式 (US-REG-002) 直接接诊，流程完整
3. 两种入口模式 (前台挂号 + 医生快速看诊) 已覆盖所有场景

**矩阵修复**: 无变更，Admin 创建挂号保持 "-"

---

### 4.2 G-8: Doctor 禁用患者 (LOW)

**问题**: Doctor 无法禁用患者，离线场景可能受限。

**方案: 不新增 -- 接受当前设计**

**理由**:
1. 患者禁用是管理性操作 (影响该患者的所有后续就诊)，不应在诊疗流程中随意执行
2. 禁用患者有前置条件 (无 Active/Suspended 医案)，需要全局视角判断
3. 离线模式 (本地) 通常是 Doctor 独立使用，此时 Doctor 同时扮演 Admin 角色 -- 应使用 Admin 账号操作

**矩阵修复**: 无变更，保持 Doctor 启用/禁用患者 = 403

**后续深化点**: 如果离线场景频繁出现此需求，考虑在本地模式增加 Doctor 禁用患者的能力 (仅限本地模式)

---

### 4.3 G-9: Registration 回退后流程 (HIGH)

**问题**: 医案取消后 Registration 回退为 Waiting，后续流程未定义。

**方案: 完善 Registration 状态机 + 补充回退后操作定义 (v1.0 不含改派)**

#### 4.3.1 完善后的状态机

```
前台模式:
  Created -> Waiting -> InProgress -> Completed
                |            |
                |            v
                |        (医案取消)
                |            |
                |     [Source=Receptionist]
                |            |
                |            v
                |<------ Waiting (回退)
                |
                v
           Cancelled (前台取消)

医生模式:
  Created -> InProgress -> Completed
                 |
                 v
            (医案取消)
                 |
          [Source=Doctor]
                 |
                 v
            Cancelled (自动闭环)
```

#### 4.3.2 医案取消时的分流规则

| Registration.Source | 医案取消后 Registration | MedicalCaseId | 后续路径 |
|--------------------|-----------------------|---------------|---------|
| Receptionist | 回退 Waiting | **保留** (用于恢复) | 医生重新接诊 (恢复原医案) 或前台取消退号 |
| Doctor | 自动 Cancelled (闭环) | 保留 (历史记录) | 流程终止。再看需重新挂号，创建新医案 |

#### 4.3.3 回退后操作定义

| 操作 | 操作者 | 前置条件 | 行为 |
|------|--------|---------|------|
| 重新接诊 (恢复) | Doctor | Registration.Status=Waiting, DoctorId=自己, MedicalCaseId 非空 | **恢复**原 MedicalCase (IsDeleted=false, Status -> Active)，Registration -> InProgress |
| 取消退号 | Receptionist | Registration.Status=Waiting, Source=Receptionist | Registration -> Cancelled，MedicalCase 保持软删除 |

> v1.0 不支持改派医生。回退后仅两条路径: 原医生重新接诊 (恢复原医案)，或前台取消退号。改派能力留待 v2.0 评估 (见 Section 6 DP-04)。

**关键规则**:
1. 回退后 MedicalCaseId **保留不清空** -- 作为恢复原医案的线索
2. 医生重新接诊时**恢复**原 MedicalCase (IsDeleted=false)，保留之前的诊断/处方数据
3. 前台取消退号后，原 MedicalCase 保持软删除状态不变
4. 医生模式 (Source=Doctor) 取消医案 = 自动取消挂号 = 流程彻底关闭。再来需重新挂号，创建全新 MedicalCase
5. 同一 Registration 可经历多次 "Waiting -> InProgress -> (取消回退) -> Waiting" 循环
6. DoctorId 在回退后**不变** -- 该挂号仍属于原医生

#### 4.3.4 Registration 历史追踪

当 Registration 经历回退时:

| 字段 | 回退前 (InProgress) | 回退后 (Waiting) |
|------|-------------------|-----------------|
| Status | InProgress | Waiting |
| MedicalCaseId | {已取消医案ID} | **保留** (不清空) |
| DoctorId | 保持不变 | 保持不变 |
| UpdatedAt | 更新 | 更新 |

**矩阵修复**:
- Section 6.5 增加: "医案取消 (Source=Receptionist) -> 恢复原医案" 联动规则
- Section 6.5 增加: "医案取消 (Source=Doctor) -> 挂号自动关闭" 联动规则
- Section 6.5 修正: 原"MedicalCaseId 清空"改为"MedicalCaseId 保留"

**PRD 修复**:
- registration.md US-REG-006 验收标准修正: Source=Receptionist 回退后 MedicalCaseId 保留 (不清空)
- registration.md Business Rules 新增 REG-BR-005: 回退后医生重新接诊时恢复原医案 (IsDeleted=false)，v1.0 不支持改派

---

### 4.4 G-10: Admin 互管死锁 (MEDIUM)

**问题**: 严格大于比较 (`>`) 导致 Admin 无法管理其他 Admin。SuperAdmin 不可用时系统管理层锁死。

**方案: 保持严格大于 + 增加 SuperAdmin 紧急恢复机制**

**理由保持严格大于**:
- 同级互管会引入"谁先操作谁赢"的竞争条件
- 中医诊所 Admin 通常 1-2 人，同级管理需求极低
- 严格层级是安全性保障

**紧急恢复机制**:

```
场景: SuperAdmin 密码丢失，无法登录

恢复方案 (运维层面，非应用层):
1. 数据库种子脚本包含 SuperAdmin 密码重置功能
2. 执行: dotnet run --project tools/SeedTool -- reset-sysadmin-password
3. 重置为默认密码 + 强制下次登录修改
4. 操作需要数据库直连权限 (非 API)
```

**设计**:

| 组件 | 说明 |
|------|------|
| 位置 | `tools/SeedTool/Commands/ResetSysAdminPasswordCommand.cs` |
| 触发 | CLI 命令行，需数据库连接字符串 |
| 行为 | 重置 sysadmin 密码为配置默认值，标记 `MustChangePassword=true` |
| 审计 | 写入 AuditLog: "SysAdmin password reset via SeedTool" |
| 安全 | 仅接受本地/可信网络的数据库连接，不暴露 API |

**矩阵修复**:
- Section 1.2 SuperAdmin 特殊规则表增加: "密码恢复: 通过 SeedTool CLI 重置 (运维操作，需数据库直连)"

**后续深化点**: 是否需要 Admin 之间的有限只读权限 (如查看但不可编辑其他 Admin)

---

### 4.5 G-11: 医生禁用后 Waiting 挂号处理 (MEDIUM)

**问题**: 医生被禁用后，其名下的 Waiting 状态挂号无人处理。

**方案: 禁用前校验 + 强制先处理挂号**

> v1.0 不支持改派，因此采用"先清后禁"策略: 医生有 Waiting 挂号时阻止禁用操作。

#### 4.5.1 禁用联动规则

```
用户管理: PUT /api/users/{id}/toggle-status (禁用 Doctor)

前置校验:
1. 查询该 Doctor 的所有 Status=Waiting 的 Registration
2. 如果存在 Waiting 挂号:
   a. 拒绝禁用 (422)
   b. 返回: "该医生有 N 条等待中的挂号记录，请先由前台取消后再禁用"
3. 如果无 Waiting 挂号:
   a. 正常禁用
   b. 撤销所有 Token Family (已有规则)
   c. 禁止为该医生创建新挂号 (REG-70006, 已有规则)
```

#### 4.5.2 操作流程

```
Admin 禁用医生
  ├─ 该医生有 Waiting 挂号 -> 422 "请先由前台取消 N 条等待挂号"
  │   └─ 前台逐个取消 (US-REG-004)
  │       └─ Admin 再次禁用 -> 成功
  └─ 该医生无 Waiting 挂号 -> 禁用成功
```

**关键规则**: 不新增字段，不新增 API。复用现有的取消流程 (US-REG-004) 清理 Waiting 挂号，然后再执行禁用。

**矩阵修复**:
- Section 6.4 用户状态联动: 删除"**PRD 未定义**"标注
- 替换为: "医生禁用 -> Registration (已有 Waiting): 阻止禁用 (422)，需前台先取消所有 Waiting 挂号"

**PRD 修复**: registration.md Business Rules 新增 REG-BR-006: 医生有 Waiting 挂号时不可禁用

---

### 4.6 G-12: 软删除恢复后状态 (MEDIUM)

**问题**: 恢复操作后实体回到什么状态未定义。

**方案: 恢复到删除前状态**

**设计规则**:

| 模块 | 恢复后状态 | 实现方式 |
|------|-----------|---------|
| Patient | 恢复到删除前的 Status (Enabled/Disabled) | IsDeleted=false, Status 不变 |
| Herb | 恢复到删除前的 Status (Enabled/Disabled) | IsDeleted=false, Status 不变 |
| Formula | 恢复到删除前的 Status (Enabled/Disabled) + ValidationStatus 不变 | IsDeleted=false, Status/ValidationStatus 不变 |
| User | 恢复到删除前的 Status (Active/Disabled) | IsDeleted=false, Status 不变 |
| MedicalCase | **不可恢复** (无 Restore 端点) | N/A |

**理由**:
- 软删除仅设置 `IsDeleted=true`，不修改 Status 字段
- 恢复时仅设置 `IsDeleted=false`，Status 保持删除前的值
- 这是最简实现，且符合"撤销"的语义 (回到删除前的精确状态)

**特殊场景**:

| 场景 | 行为 |
|------|------|
| 药材删除前是 Disabled，恢复后 | Disabled (不自动启用) |
| 用户删除前是 Disabled，恢复后 | Disabled (不自动启用) |
| 验方删除前是 Draft + Disabled，恢复后 | Draft + Disabled (两个状态字段均保持) |

**矩阵修复**:
- Section 5.3 软删除数据可见性表增加"恢复后状态"列
- 统一标注: "恢复到删除前状态 (IsDeleted=false, 其余字段不变)"

---

## 5. 变更影响汇总

### 5.1 文档变更

| 文件 | 变更类型 | 涉及章节 |
|------|---------|---------|
| role-permission-matrix.md | 修改 | Section 1.2, 2.1, 3.1, 3.2 (Registration/MedicalCase), 5.1, 5.3, 6.4, 6.5 |
| registration.md | 修改+新增 | US-REG-006 验收标准修正 (MedicalCaseId 保留), REG-BR-005/006, Section 6.4 联动规则 |
| herbs.md | 修改 | Section 2 Target Users 表 |
| medical-cases.md | 新增 | Decision Log MC-LOCK-01 ~ MC-LOCK-03 |

### 5.2 代码变更 (后期实现)

| 变更 | 优先级 | 模块 | 说明 |
|------|--------|------|------|
| IsLocked 动态计算 | HIGH (I-2) | MedicalCase Service | CompletedAt.Date < Today |
| 角色-状态-锁定权限矩阵 | HIGH (I-2) | MedicalCase Service | 3 层检查: 状态 -> 锁定 -> 角色 |
| 禁用医生前置校验 | MEDIUM (G-11) | User Service -> Registration Service | 有 Waiting 挂号时阻止禁用 (422) |
| SeedTool 密码重置 | MEDIUM (G-10) | tools/SeedTool | CLI 命令 |

### 5.3 新增 Business Rules

| 编号 | 模块 | 规则 |
|------|------|------|
| REG-BR-005 | Registration | 回退后仅原医生可重新接诊或前台取消，v1.0 不支持改派 |
| REG-BR-006 | Registration | 医生有 Waiting 挂号时不可禁用，需先取消 |

---

## 6. 深化待定项

以下问题在本设计中给出了方向，但需要后续深化确认:

| 编号 | 待定项 | 状态 | 结论/深化方向 |
|------|--------|------|-------------|
| DP-01 | Admin 代创建验方/药材场景 | **CLOSED** | v1.0 不存在代创建，归属字段保持现状 |
| DP-02 | Doctor 本地模式禁用患者 | **CLOSED** | 不放宽，需要时切换 Admin 账号 |
| DP-03 | Admin 之间的只读权限 | **CLOSED** | 不需要，Admin 互不可见 |
| DP-04 | Registration 改派医生 (v2.0) | OPEN | v2.0 评估 reassign API，v1.0 回退后只能原医生重新接诊或前台取消退号 |
| DP-05 | SuperAdmin 锁定医案编辑的审批流程 | **CLOSED** | 已确认: EditReason + UI 确认弹窗，Admin/SuperAdmin 对等 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-10 | v1.0 | 初始版本: 11 个问题的修复设计 (3 逻辑缺陷 + 2 PRD不一致 + 6 设计不足) |
| 2026-03-10 | v1.1 | **讨论深化**: MC-LOCK 规则重写 (Doctor 当天可编辑, Admin/SuperAdmin 不受限, 统一 EditReason+确认弹窗); G-9 回退保留 MedicalCaseId+恢复原医案; Source=Doctor 取消=闭环; 移除改派/NeedsReassignment; Admin 不挂号确认; DP-01/02/03/05 关闭; SeedTool 密码恢复确认; 先清后禁策略确认 |
| 2026-03-11 | v1.2 | **v1.2 审查问题修复 (12 问题 P-01~P-12)**: 详见下方"v1.2 审查问题索引" |

---

## 7. v1.2 审查问题索引 (P-01 ~ P-12)

> 以下问题在 role-permission-matrix.md v1.2 架构师+需求分析师双视角审查中发现，已逐项与用户确认决策方案并同步到相关文档。

| 编号 | 严重程度 | 问题简述 | 决策方案 | 影响文件 | 状态 |
|------|---------|---------|---------|---------|------|
| P-01 | HIGH | registration.md US-REG-006 "MedicalCaseId 清空" 与矩阵 Section 6.5 "MedicalCaseId 保留" 矛盾 | 修正为"保留" + 新增 REG-BR-005 (回退后恢复原医案) | registration.md | DONE |
| P-02 | HIGH | Patient 端点策略粒度不足 (CRU/Delete/ToggleStatus 未区分) | 三层策略: PatientAccess(CRU) + DoctorOrAdmin(DELETE) + AdminOnly(ToggleStatus) | role-permission-matrix.md Section 2.1 | DONE |
| P-03 | HIGH | Admin 取消医案的挂号联动规则缺失 | Admin 取消医案按原 Registration.Source 执行对应策略 + 需 EditReason | role-permission-matrix.md Section 6.5 | DONE |
| P-04 | HIGH | patients.md US-PAT-006 Restore 角色含 Doctor，但矩阵中 Restore 为 Admin-only | 统一 Restore 为 Admin-only，修正 patients.md | patients.md Section 2, US-PAT-006 | DONE |
| P-05 | MEDIUM | 缺少 API 权限检查顺序规范 (401/403/ERR-xxxxx/422 顺序不明确) | 新增 Section 2.3 检查顺序规范 | role-permission-matrix.md Section 2.3 | DONE |
| P-06 | MEDIUM | Registration 端点策略粗放 (POST 创建和取消未区分角色) | 细化: Roles=Receptionist(POST/cancel) + Roles=Doctor(quick-visit) + PatientAccess(GET) | role-permission-matrix.md Section 2.1 | DONE |
| P-07 | MEDIUM | MC-LOCK 当天/隔天区分仅影响 Doctor 的注释缺失 | 补充注释: Admin/SuperAdmin 规则始终一致，不受 IsLocked 影响 | role-permission-matrix.md Section 3.2 | DONE |
| P-08 | MEDIUM | ERR-50103/ERR-60103 触发范围含 Restore，但 Restore 是 Admin-only 操作 | 修正: 仅 Update/Delete/ToggleStatus 触发，Restore 不触发 | role-permission-matrix.md Section 8 | DONE |
| P-09 | MEDIUM | herbs.md US-HERB-005 引用检查仅提及 PrescriptionItem，遗漏 FormulaItem | 补充: "有处方引用 (PrescriptionItem) 或验方引用 (FormulaItem)" | herbs.md US-HERB-005 | DONE |
| P-10 | LOW | Registration "全部历史"含义不明确 (是否含所有 Source 和所有医生) | 补充注释: "(含所有 Source 类型和所有医生的挂号记录)" | role-permission-matrix.md Section 3.2 | DONE |
| P-11 | LOW | herbs.md Section 2 Doctor 权限描述不完整 (遗漏编辑/删除/启用/禁用) | 补全: "创建药材; 编辑/删除/启用/禁用自己创建的药材; 查看全部药材" | herbs.md Section 2 | DONE |
| P-12 | LOW | medical-cases.md 状态表 Completed 行未体现 MC-LOCK 角色差异 | 展开描述: Doctor 当天可编辑/隔天 403; Admin/SuperAdmin 可编辑/不受时间限制 | medical-cases.md Section 4 | DONE |
