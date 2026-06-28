# 挂号→接诊工作流重设计 spec（R10）

> **日期**：2026-06-28
> **状态**：📝 待审
> **范围**：挂号→接诊节点的双模式工作流重设计；修复 D8 StartVisit bug；激活 US-REG-002 QuickVisit；明确 SignalR 仅远程、本地模式无挂号。**文档更新可立即执行；代码改动（D8 修复/QuickView 接线）标「待实施」，由后续代码 plan 承载（用户指示「先修正文档不修改代码」）。**
> **配套**：基线 `2026-06-28-docs-reconciliation-baseline.md`、`scenario-functional-map` 旅程1/6、`prd-code-reconciliation` D7/D8、ADR-0013 SignalR

---

## [S1] 背景与目标

R10 节点（挂号→医生接诊）当前断裂：D8 bug（StartVisit 不创建医案 + 返回 RegistrationId 冒充 MedicalCaseId）+ US-REG-002 QuickVisit 死代码 + 双模式工作流未厘清。本 spec 锁定重设计决策，作为文档更新与后续代码实施的依据。

用户决策（2026-06-28）：医生应能直接挂号+看诊（QuickVisit 应急/本地常规）；流程须一致；**本地模式取消挂号，采用「来一个看一个」**。

---

## [S2] 远程模式工作流（有前台）

```
前台建档(首诊) + 挂号(Waiting) → 待诊队列
        ↓ SignalR 推送通知医生（仅远程，见 ADR-0013）
医生待诊列表 → 选患者 →「开始就诊」StartVisit（US-REG-005）
        ↓ 原子事务：创建 MedicalCase(Active) + Registration(InProgress) + 返回 MedicalCaseId
导航医案编辑 → 望闻问切 → 开方 → 打印（系统终点）

急诊/特殊通道：医生 QuickVisit（US-REG-002）→ 选/建患者 → 原子创建 Registration+MedicalCase → 直接看诊
```

**要素**：前台挂号驱动；待诊队列；SignalR 推送（远程）；StartVisit 原子创建医案；QuickVisit 急诊通道并存。

---

## [S3] 本地模式工作流（无前台，医生独立）

```
患者到诊 → 医生选/建患者（Patient 模块）→ 直接开医案（MedicalCase）→ 看诊 → 打印
```

**要素**：
- **取消挂号**：无 Registration 环节（医生看诊时不停下挂号）
- **无待诊队列**：待诊清单恒空
- **来一个看一个**：医生直接 Patient→MedicalCase
- **无 SignalR**：无队列无需推送
- 本质等同远程的 QuickVisit 急诊模式

**模式适用性**：Registration 模块在本地模式**不激活**；本地仅用 Patient + MedicalCase 模块。

---

## [S4] 流程一致性约束

无论远程（前台挂号→StartVisit / QuickVisit）还是本地（直接开医案），**医案创建统一经 MedicalCaseFacade**：
- 同样的 MedicalCase(Active) 创建逻辑
- 同样的状态机（Active↔Suspended→Completed）
- 同样的导航（→医案编辑页）
- 同样的打印流程（系统终点）

差异仅在**入口**：远程有挂号/队列前置；本地无。

---

## [S5] D8 修复方向（StartVisit，待代码实施）

**当前 bug**：`RegistrationsController.StartVisit` 不创建医案 + 返回 RegistrationId 当 MedicalCaseId → Desktop 导航到空医案。

**修复方向**（代码待实施，本 spec 仅锁定）：
- StartVisit 改为原子事务：`Registration.Status = InProgress` + 创建 `MedicalCase(Active)` 关联 `RegistrationId` + 返回 `MedicalCaseId`
- 复用现有 BR-001 单活跃医案约束（同一患者仅一个 Active/Suspended）
- 若患者已有 Active/Suspended 医案，StartVisit 提示「重开现有医案」而非新建

---

## [S6] US-REG-002 QuickVisit 激活（待代码实施）

**当前**：`QuickVisitAsync` Service 方法存在但无调用方（死代码）。

**激活方向**：
- 定位：急诊/特殊通道（远程）+ 常规模式（本地）
- 原子事务：创建 Registration(Waiting→InProgress 直通) + MedicalCase(Active) + 返回 MedicalCaseId
- 远程：医生在患者列表选「快速就诊」或首诊「新建患者+快速就诊」
- 本地：本质即常规看诊入口

---

## [S7] SignalR 范围（ADR-0013 收敛）

- **仅远程模式**：WebAPI Hub 推送挂号变更到医生工作台
- **本地模式不做推送**：无队列，无需 SignalR；医生手动操作
- ADR-0013 的「双模式推送机制」待决项收敛：**本地排除**，仅设计远程 Hub + 推送粒度 + 降级轮询

---

## [S8] 文档更新清单（可立即执行，纯文档）

| 文件 | 更新 |
|------|------|
| `docs/02-requirements/08-registration.md` | 加「双模式工作流」段：远程挂号驱动 / 本地取消挂号来一个看一个；US-REG-002 QuickVisit 定位（急诊+本地常规）；US-REG-005 StartVisit 加 D8 修复说明 |
| `docs/02-requirements/README.md` | REG 总览补注「本地模式不激活 Registration」 |
| `docs/03-architecture/11-business-flows.md` | Flow 1（首诊）补双模式分野：远程挂号链 / 本地直接看诊链 |
| `docs/03-architecture/decisions/0013-signalr-realtime-push.md` | 收敛：仅远程，本地排除 |
| `docs/02-requirements/13-traceability-matrix.md` | US-REG-002 状态从「死代码」改「🧲 v1.0 待激活（急诊+本地常规）」；US-REG-005 加 D8 修复注 |
| `docs/01-product/02-personas.md` | Receptionist 角色补「仅远程模式」注；Doctor 补「本地模式直接看诊」 |

---

## [S9] 代码待实施清单（本 spec 仅记录，不执行）

遵循用户「先修正文档不修改代码」指示，以下由后续代码 plan 承载：
- D8：StartVisit 原子创建医案 + 返回正确 ID
- US-REG-002：QuickView 接线（远程急诊入口 + 本地常规入口）
- SignalR：远程 Hub 实现 + 推送粒度 + 降级（ADR-0013 专项 spec）
- 本地模式：确认 Registration 模块不在 LocalWebAPI/Doctor 工作流激活

---

## [S10] 验收

1. 文档清楚反映双模式工作流分野（远程挂号驱动 / 本地无挂号）
2. US-REG-002 QuickVisit 定位明确（急诊+本地常规）
3. D8 修复方向记录（待代码）
4. SignalR 仅远程（ADR-0013 收敛）
5. 流程一致性约束明示（统一 MedicalCaseFacade）
