# 第3轮审查：状态机实现一致性验证

> **审查日期**: 2026-08-20  
> **审查人**: Hermes Agent (架构师视角, intended vs implemented)  
> **审查范围**: `04-data-model` 状态定义 + `07-medical-cases` BR-000~BR-003 + `08-registration` REG-BR-001~012 + `02-auth` 状态图 + `Entities`域方法 + `MedicalCaseStateService` + `Registration Handlers` + `AuthenticationStateMachine` + `TokenLifecycleService` + `EditModeStateMachine`

## 一、执行摘要

| 状态机 | 文档意图 | 代码实现 | 结论 |
| :--- | :--- | :--- | :--- |
| **MedicalCase CaseStatus** `Active↔Suspended→Completed→(*物理删)/软删` | `Suspended(0)↔Active(1)→Completed(2)`，`Completed`终态，取消=物理删，软删仅`Completed` | `BusinessRules.IsValidStatusTransition`仅双向`Active↔Suspended`，`UpdateStatus→CompleteAsync`拦截`Completed`，`Cancel=HardDelete`正确 | ✅ 主链闭合，**但`EditReason/IsLocked`守卫对`Admin`失效** |
| **Registration Status** `Waiting→InProgress→Completed/Cancelled` | `Waiting→InProgress`(StartVisit) `→Completed`(医案完成) / `→Cancelled`(前台退号) + `Source-aware`回退 | `Entity.StartVisit/Cancel/Complete/RevertToWaiting` + `StartVisitCommandHandler`原子事务正确 | 🔴 **Doctor来源取消联动必抛异常**，`Complete()`无守卫 |
| **Desktop Auth** 11状态 19迁移 | `Idle→Authenticating/ValidatingToken→LoadingProfile→LoadingModules→Navigating→Authenticated→...` | `AuthenticationStateMachine.Transitions`字典驱动 + `lock`线程安全 | 🟡 `ValidatingToken+LoginFailure→Idle`与文档`→Failed`分叉 |
| **TokenLifecycle** `NotAuthed→Active→Warning→Expired` | 剩余<5min进`Warning`自动`TryRefresh`，30s轮询 | `TokenLifecycleService WarningThreshold=5min, MonitorInterval=30s` 实现一致 | ✅ |
| **Desktop EditMode** `ReadOnly→Editing→Dirty→Saving→...` | BR-002 离开必选挂起/取消/完成 | `EditModeStateMachine`转换表 + `ReentrancyGuard` + `GuardPredicate` | ✅ 结构正确，缺`Saving→Dirty`的`EditReason`透传 |

**严重度**：🔴 CRITICAL 2 · 🟠 HIGH 3 · 🟡 MEDIUM 3 · ✅ PASS 5

---

## 二、CRITICAL（状态机断裂/事务回滚）

### C1 — `Registration.HandleMedicalCaseCancelledAsync` 对 Doctor 来源挂号必抛 `InvalidOperationException`
- **意图** (`08-registration US-MC-014/US-REG-007`)：`Source=Doctor`的`InProgress`挂号在医案取消时**自动闭环** `InProgress→Cancelled`
- **证据**：
  ```csharp
  // RegistrationCrossModuleService.cs:40-46
  if (entity.Source == Receptionist) entity.RevertToWaiting();
  else entity.Cancel(); // Doctor → Cancelled
  // RegistrationModel.cs:114-122
  public void Cancel(){ if(Status!=Waiting) throw new InvalidOperationException("只有等待中的挂号可以取消"); ... }
  ```
  调用时 `Status==InProgress`且`MedicalCaseId==cancelledMedicalCaseId`，两条件皆触发异常，`MedicalCaseStateService.CancelAsync:294 RollbackRegistrationAsync`抛异常导致挂号状态未变更，留下`InProgress`孤儿
- **影响**：医生取消`QuickVisit`(Doctor来源)医案后，挂号永远占位，`HasSameDayWaiting`误判重复挂号
- **修复**：新增`CancelFromInProgress()`域方法（允许`InProgress→Cancelled`且清空`MedicalCaseId`，仅供联动调用），`CrossModuleService`对`Doctor`分支调新方法

### C2 — `MedicalCase.ValidateEditReason` 对 `Admin/SuperAdmin` 编辑 `Completed` 完全放行
- **意图** (`07-medical-cases §编辑理由`)：`修改已完成医案 ✅ 需 EditReason`（`Admin/SuperAdmin`含），`隔天 IsLocked`同样需原因
- **证据**：
  ```csharp
  // MedicalCaseCommandService.cs:251-255
  var isCompletedEdit = medicalCase.CaseStatus == MedicalCaseStatus.Completed && !isAdmin; // ← !isAdmin
  if ((isPrintedEdit || isCompletedEdit) && IsNullOrWhiteSpace(EditReason)) throw ...
  ```
  当`isAdmin==true`时`isCompletedEdit==false`，即使`Completed`+`IsLocked==true`也不要求`EditReason`
- **证据2**：`MedicalCaseModel.IsLocked => IsCompleted && CompletedAt.Date < UtcNow.Date` 存在但**全仓`grep IsLocked`无一处Server Guard引用**
- **修复**：
  ```csharp
  var isCompletedEdit = medicalCase.CaseStatus == MedicalCaseStatus.Completed;
  var isLockedEdit = medicalCase.IsLocked;
  var isForeignEdit = medicalCase.UserId != currentUserId;
  if ((isPrintedEdit || isCompletedEdit || isLockedEdit || isForeignEdit) && IsNullOrWhiteSpace(EditReason)) throw ...
  ```

---

## 三、HIGH（状态机守卫缺失/越权迁移）

### H1 — `Registration.Complete()` 无状态守卫：`Waiting`可直跳`Completed`
- **证据**：`RegistrationModel.cs:98-103 Complete(){ Status=Completed; }`无`if(Status!=InProgress) throw`，而文档仅`InProgress→Completed`。当`MedicalCase`完成时若`Registration`仍为`Waiting`（断网残留），会被误标`Completed`
- **修复**：`Complete()`加`if(Status!=InProgress) throw new InvalidOperationException("仅进行中的挂号可完成")`

### H2 — `MedicalCaseBusinessRules.IsValidStatusTransition` 未覆盖 `Completed` 终态的显式拒绝
- **证据**：`BusinessRules:12-16`仅`Active↔Suspended`为`true`，`MedicalCaseStateService.UpdateStatus:62-66`虽拦截`Completed→CompleteAsync`，但`Completed→Completed`重复完成会幂等而非报错，与“`Completed`终态”语义不符
- **修复**：`UpdateStatus`首行加`if(CaseStatus==Completed) throw McCompletedCannotTransition`；`CompleteAsync`加幂等检查

### H3 — `AuthenticationStateMachine` `ValidatingToken+LoginFailure → Idle` 与文档 `→Failed` 分叉
- **证据**：`AuthenticationStateMachine.cs:43 Transitions[(ValidatingToken,LoginFailure)]=Idle`，而`docs/09-security §5` 图为`→Failed`，`Failed`才有`StartLogin/StartAutoLogin`重试事件
- **修复**：改`→Failed`，或文档同步为`Idle`并说明“自动登录失败回`Idle`为有意”

---

## 四、MEDIUM

### M1 — `EditModeStateMachine` 缺 `Saving→Saving`重入与`TransitionBlocked`的`EditReason`透传
- **证据**：`EditModeStateMachine.cs:35 Saving+SaveFailed→TransitionBlocked`正确，但`TransitionBlocked`仅`MakeChange→DirtyEditing`，未处理`Save→Saving`重试时需携带`EditReason`
- **修复**：`TransitionBlocked + Save → Saving`补迁移，并把`context`（含`EditReason`）透传

### M2 — `MedicalCaseDetailModel/ MedicalCaseEditContext` 默认`Status=Suspended` 与 `Entity` 默认`Active`不一致
- **证据**：`MedicalCaseDetailModel.cs:25 _status=Suspended` / `MedicalCaseEditContext.cs:20 _status=Suspended`，而`MedicalCaseModel.cs:31 CaseStatus=Active`
- **修复**：`DetailModel/EditContext`默认值改`Active`

### M3 — `Registration.HasSameDayWaitingAsync` 仅查`Waiting`，漏`InProgress`
- **证据**：`CreateRegistrationCommandHandler.cs:68` + `RegistrationRepository:90-115` `Where Status==Waiting`，同日已有`InProgress`时仍允许重复挂号，违反`REG-BR-007`
- **修复**：`Where Status in (Waiting,InProgress)`且`!IsDeleted`

---

## 五、PASS

| 状态机 | 证据 | 结论 |
| :--- | :--- | :--- |
| `MedicalCase` 创建`BR-001` | `ValidateAndFetchCreationContextAsync`查`Active/Suspended` + 抛`McActiveCaseExists` | ✅ |
| `MedicalCase` 完成校验`BR-003` | `CompleteAsync:118-145`校验`NeedsPrescription/Prescription/Items/TcmDiagnosis` | ✅ |
| `MedicalCase` 取消审计隔离 | `CancelAsync:287 HardDelete → 292 TryWriteCancelAuditAsync catch仅LogError` | ✅ |
| `Registration` StartVisit原子性 | `StartVisitCommandHandler:46-55 BeginTransaction + RevertToWaiting on null` | ✅ |
| `TokenLifecycle` 阈值 | `WarningThreshold=5min, MonitorInterval=30s`与文档一致 | ✅ |

---

## 六、修复清单

| 优先级 | 缺陷 | 文件 | 改动 |
| :--- | :--- | :--- | :--- |
| **P0** | C1 Doctor来源取消抛异常 | `RegistrationModel.cs` + `RegistrationCrossModuleService.cs` | 新增`CancelFromMedicalCase()`域方法 |
| **P0** | C2 Admin Completed免EditReason | `MedicalCaseCommandService.cs:251` | 去`&&!isAdmin`，加`isLockedEdit/isForeignEdit` |
| **P1** | H1 Registration.Complete守卫 | `RegistrationModel.cs:98` | `if(Status!=InProgress) throw` |
| **P1** | H3 Auth状态图分叉 | `AuthenticationStateMachine.cs:43` 或 `09-security.md` | 统一`ValidatingToken+LoginFailure→Failed` |
| **P2** | M3 重复挂号漏InProgress | `RegistrationRepository.cs:90` | `Where Status in (Waiting,InProgress)` |

> **验证**：`dotnet test` 覆盖 `MedicalCaseStateTests` + `RegistrationStateTests` + `AuthenticationStateMachineTests` 全绿；`SwitchingApiClient`双端冒烟 `StartVisit→CancelMedicalCase` 回滚路径。
