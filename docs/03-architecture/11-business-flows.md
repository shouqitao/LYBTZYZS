# Flows — 关键业务流程

## Flow 1: 首诊全流程（前台→医生）

**覆盖 US**: US-AUTH-001, US-CARD-001/002, US-PAT-003, US-REG-001/004/005/006/007, US-MC-001/002/003/011, US-PRINT-001/004

> **双模式分野**（见 [R10 spec S2/S3/S4](../compose/specs/2026-06-28-registration-workflow-redesign.md)）：本地模式 = 远程功能完整副本（数据孤立 N1），不做角色强制过滤，差异由用户配置自然调节。默认无前台用户时本地为医生独立链（无挂号前置）；若 Admin 建前台用户则远程挂号链在本地同样可用。

### 远程链（有前台，挂号驱动）

```
1. 前台登录 → Auth.LoginAsync → JWT
2. 读身份证 → CardReader → 患者信息
3. 创建挂号 → RegistrationController.Create → 候诊队列
   （SignalR 推送通知医生，仅远程，见 ADR-0013）
4. 医生登录 → Auth.LoginAsync → JWT
5. 开始就诊 → RegistrationController.StartVisit → 原子创建 MedicalCase(Active) + Registration(InProgress) + 返回 MedicalCaseId
6. 望闻问切 → MedicalCaseController.Save → Consultation
7. 开方 → MedicalCaseController.Save → Prescription
8. 完成医案 → MedicalCaseController.Complete → Completed
9. 打印处方 → PrintService → PDF/纸质
10. 打印回写 → MedicalCaseController.PrintCompleted → IsPrinted=true
```

**急诊/特殊通道**：医生 QuickVisit（US-REG-002）→ 选/建患者 → 原子创建 Registration+MedicalCase → 跳过队列直接看诊。

### 本地链（远程功能副本；默认医生独立，建前台用户则挂号链可用）

> 本地模式 = 远程功能完整副本（数据孤立 N1），不做角色强制过滤，差异由用户配置自然调节。以下为**默认无前台用户**时的医生独立链。

```
1. 医生登录
2. 选/建患者 → PatientsController（姓名/拼音码/身份证查询，不存在则新建）
3. 直接开医案 → MedicalCaseFacade.Create → MedicalCase(Active)（无挂号环节）
4. 望闻问切 → MedicalCaseController.Save → Consultation
5. 开方 → MedicalCaseController.Save → Prescription
6. 完成医案 → MedicalCaseController.Complete → Completed
7. 打印处方 → PrintService → PDF/纸质
8. 打印回写 → MedicalCaseController.PrintCompleted → IsPrinted=true
```

**本地链要素**：默认无前台用户时无 Registration、无待诊队列；**若 Admin 建前台用户则前台挂号→待诊队列→StartVisit 链同样可用**（本地全角色支持）。本地无 SignalR（即使有前台也用轮询/手动刷新）。Registration 模块按需——无前台用户时不显现，有则可用。

### 流程一致性约束

无论远程（挂号→StartVisit / QuickVisit）还是本地（直接开医案），**医案创建统一经 `MedicalCaseFacade`**：同样的 MedicalCase(Active) 创建逻辑、同样的状态机（Active↔Suspended→Completed）、同样的导航（→医案编辑页）、同样的打印流程（系统终点）。差异仅在入口前置。

**权限检查**：
- 远程链步骤 3: Receptionist 可创建挂号（D7 修复后）
- 步骤 6-8(远程)/4-6(本地): 仅 Doctor 可操作医案
- 打印步骤: Doctor 可打印

**系统终点**：打印处方笺为系统侧终点。后续付费、发药、库存均为线下流程，**不在系统范围内**（X2.2 系统外）。患者凭打印的处方笺线下付费取药。

**信任边界跨越**：
- Desktop → Server (Refit HTTP)
- Server → DB (EF Core)

**就诊完成→前台反向通知**（审计 I6 标注，2026-06-28）：
v1.0 **未设计 SignalR 反向推送**（医生→前台方向）。ADR-0013 SignalR 仅覆盖挂号→医生单向通知。前台靠**队列轮询**被动感知：`GetQueue` 返回 `Waiting` 状态患者，患者进入 `InProgress`/`Completed` 后从队列消失，前台据此**被动推断**就诊已开始/完成。反向通知（医生完成/取消→前台实时推送）属 **v2.0 规划**。

---

**覆盖 US**: US-PAT-001, US-HERB-001, US-MC-002/008/009/019, US-PRINT-001

```
1. 医生登录
2. 搜索患者 → PatientsController.Search (拼音码)
3. 查历史医案 → MedicalCaseController.GetHistory (D9 补回)
4. 查历史处方 → MedicalCaseController.GetPrescriptionHistory (D9 补回)
5. 复制上次处方 → MedicalCaseController.CopyPrescription
6. 微调药材 → HerbsController.Search
7. 保存 → MedicalCaseController.Save
8. 打印 → PrintService
```

**权限检查**：
- 步骤 2-8: 仅 Doctor 可操作

**关键点**：
- D9 补回后，步骤 3-4 返回跨医案的历史聚合数据
- 步骤 5 使用 CopyPrescription 并发重试机制

---

## Flow 3: 离线模式切换

**覆盖 US**: US-SHELL-007, US-AUTH-009/012/013

```
1. 切换到本地 → ModeSwitchValidator.Validate
   - 检查：无未结医案 + 网络连通 + Token 有效
2. SwitchingApiClient 切换 URL → localhost:5300
3. 本地登录 → LocalWebAPI/AuthController
4. 本地操作 → LocalDB (SQL Server LocalDB)
5. 网络恢复 → 切换到远程
6. 本地数据 → ⚠️ 无同步机制 (v2.0 延期)
```

**权限检查**：
- 本地模式无角色检查（简化认证）

**风险**：
- 本地→远程切换前必须检查无未结医案（ERR-70506）
- 本地数据无法回到远程（数据孤岛）

---

## Flow 4: 模块加载（C1 修复后）

**覆盖 US**: US-AUTH-001, US-SHELL-001/003/005

```
1. 用户登录 → AuthService.LoginAsync
2. LoginCoordinator 接收登录结果
3. 调用 ApplicationBootstrapper.LoadModulesForRoleAsync(role)
4. RoleRegistry.GetModulesForRole(role) → 模块列表
5. IModuleManager.LoadModule(moduleName) → 按需加载
6. NavigationCoordinator.NavigateTo(homeView) → 角色首页
```

**权限检查**：
- 步骤 4: RoleRegistry 按角色过滤模块
- 菜单可见性矩阵在导航项构建时过滤

**C1 修复前**：
- LoginCoordinator 硬编码只加 PatientsModule（非 Admin）
- Doctor/Receptionist 拿不到角色模块

---

## Flow 5: 打印保护（D2 补回后）

**覆盖 US**: US-PRINT-001/004, US-MC-002/014/017

```
1. 打印处方 → PrintService.Render
2. 打印成功 → MedicalCaseController.PrintCompleted
   - IsPrinted = true
   - PrintVersion++
   - PrintCount++
   - LastPrintedAt = now
3. 记录 MedicalCasePrintLog
4. 编辑已打印医案 → 检查 IsPrinted
   - 需要 EditReason
   - 修改后 IsPrinted = false
   - PrintVersion++
```

**权限检查**：
- Doctor 可打印
- Admin/SuperAdmin 可编辑已打印医案（需 EditReason）

**信任边界**：
- 打印日志回写到 Server（远程）或 LocalDB（本地）
