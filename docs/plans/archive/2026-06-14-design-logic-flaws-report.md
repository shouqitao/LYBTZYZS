# 设计逻辑缺陷审查报告

> 日期: 2026-06-14
> 方法: 4 维度并行审查（状态机/跨模块流程/权限安全/业务规则冲突）
> 范围: 全部 6 实体状态机 + 5 跨模块数据流 + 6 安全领域 + 6 规则集

---

## CRITICAL 问题（15 个）

### 🔴 安全类 — 本地模式权限全面缺失

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| S1 | **本地 AutoLogin 无密码认证绕过** — 仅验证用户名即可登录任意账户 | `LocalWebAPI/AuthController.cs:143` | 完全认证绕过 |
| S2 | **本地用户管理零授权** — delete/toggle/restore/batch 无任何角色检查 | `LocalWebAPI/UsersController.cs:109-195` | 任意用户禁用所有管理员 |
| S3 | **本地 Login 不检查 Status==Disabled** — 已禁用用户仍可本地登录 | `LocalWebAPI/AuthController.cs:36` | 禁用用户绕过限制 |
| S4 | **远程验方批量操作无所有权检查** — BatchDelete/Enable/Disable 任何医生可操作任意验方 | `FormulaService.cs:348-462` | 权限提升 |
| S5 | **远程 ChangeProfile/ChangePassword 缺少 id==当前用户检查 (IDOR)** | `UsersController.cs:220-261` | 任意用户修改他人资料 |
| S6 | **本地医案端点零权限执行** — 所有变更操作不检查 MedicalCasePermissionService | `LocalWebAPI/MedicalCasesController.cs` | 接待员可编辑任意医案 |

### 🔴 数据完整性类 — 状态机死胡同

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| D1 | **StartVisit 创建 InProgress 挂号但无医案** — 挂号永久滞留 InProgress | `RegistrationsController.cs:197` | 挂号记录死锁 |
| D2 | **Delete/BatchDelete 不回滚挂号** — 仅 Cancel 回滚 | `MedicalCaseCommandService.cs:593` | 挂号孤儿记录 |
| D3 | **回滚后 Waiting 挂号无法取消** — MedicalCaseId 保留导致 CancelAsync 拒绝 | `MedicalCaseStateService.cs:343` | 队列永久污染 |
| D4 | **两套取消回滚实现语义冲突** — 一套清除 MedicalCaseId，一套保留 | `RegistrationService.cs:246` vs `MedicalCaseStateService.cs:343` | 不确定行为 |

### 🔴 打印保护类

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| P1 | **可删除已打印医案** — Delete/Cancel 路径不检查 IsPrinted | `MedicalCaseCommandService.cs:593` | 违反 ERR-30404 |
| P2 | **打印保护仅 IsPrinted && IsCompleted 双条件** — 单独 IsPrinted 无保护 | `MedicalCaseCommandService.cs:497` | Active 打印后可篡改 |
| P3 | **AddPrintLogAsync 递增 PrintVersion** — 与 RecordPrintCompletedAsync 语义冲突 | `MedicalCasePrintService.cs:115` | 审计版本号漂移 |

### 🔴 计算类

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| C1 | **IsLocked UTC 时区 bug** — CompletedAt(UTC) vs DateTime.Today(本地) 比较 | `MedicalCaseModel.cs:113` | 中国 UTC+8 锁定提前一天 |
| C2 | **软删除药材绕过 AD-02 过滤** — 仅检查 Status==Disabled，不检查 IsDeleted | `CrossModuleService.cs:192` | 处方含已删除药材 |

---

## HIGH 问题（18 个）

### 状态机
- CompleteAsync 不检查当前状态（可重复完成）
- Formula ValidationStatus 更新后不重置（标记变谎言）
- 禁用药材后处方完成时不重新检查
- 本地控制器跳过 IsValidStatusTransition
- Suspended 医案无超时机制（永久阻塞 BR-001）
- 禁用 Doctor 不检查 Active/Suspended 医案（医案孤儿）
- 禁用 Patient 不检查 Waiting/InProgress 挂号（挂号孤儿）

### 安全
- 禁用用户 JWT 在过期前仍有效（无黑名单）
- 本地 Admin 可创建 SuperAdmin 用户（权限提升）
- 远程验方 Restore 无所有权检查
- 本地 SuperAdmin 比 Admin 权限更低（字符串匹配 bug）
- 本地 PatientsController 变更操作零授权

### 数据流
- CaseNumber/PrescriptionNumber 并发竞争（count+1 模式）
- Sync 下载盲目覆盖本地修改（无乐观锁）
- 价格快照信任客户端值（仅 <=0 时服务端获取）
- 服务端历史处方复制不刷新价格

---

## 建议修复优先级

### Tier 1 — 立即修复（安全+数据完整性）
1. S1: 本地 AutoLogin 加密码/Token 验证
2. S3: 本地 Login 检查 Status==Enabled
3. S5: 远程 ChangeProfile/ChangePassword 加 id==当前用户检查
4. D1+D2: Delete/BatchDelete 加挂号回滚
5. D3: 回滚挂号时清除 MedicalCaseId（或放宽 CancelAsync 检查）
6. P1: Delete/Cancel 加 IsPrinted 检查
7. C1: IsLocked 使用 UTC 日期比较

### Tier 2 — 尽快修复（权限对齐）
8. S6: 本地医案控制器注入 MedicalCasePermissionService
9. S2: 本地用户管理加角色检查
10. S4: 远程验方批量操作加 per-item 所有权检查
11. P2: 打印保护改为 PrintCount > 0（独立于 IsPrinted+IsCompleted）

### Tier 3 — 后续迭代
- Formula ValidationStatus 更新时重置
- 软删除药材加入 AD-02 过滤
- 本地/远程权限架构统一（共享 Service 层）
- IsLocked 加诊所时区配置
- Sync 下载加乐观锁检查

---

## 根因分析

**最大系统性问题：LocalWebAPI 绕过了远程服务器的整个权限模型。**

LocalWebAPI 的 AGENTS.md 明确记录："Controllers use LocalWebApiDbContext directly (no service/repository layer for simplicity)"。这个"简化"设计决策创建了一个**平行的权限宇宙**，与远程端在以下方面严重偏离：

| 维度 | 远程 | 本地 |
|------|------|------|
| 用户管理 | AdminOnly + 多层服务检查 | 零检查 |
| 医案操作 | DoctorOrAdmin + PermissionService | [Authorize] 放行所有角色 |
| 验方所有权 | 服务层 CreatedBy 检查 | 部分检查（之前已修） |
| 患者操作 | PatientAccess + 所有权 | 零检查 |
| 认证 | 密码+锁定+状态检查 | 仅 !IsDeleted |
| 自动登录 | AutoLoginToken DB 验证 | 仅用户名 |

**推荐架构修复**：本地控制器委托给与远程控制器相同的 Service 接口，而非直接操作 DbContext。
