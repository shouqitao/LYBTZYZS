# 任务 A-31-C7：复核项 64 处置 + 新发现死代码清理

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 依据：S3 报告 `method-audit-desktop-2026-08-08.md` §7.2（复核项 64）+ C-6 报告 `a31-c6-dead-code-cleanup.md`「新发现死代码」表
> 用户指令（2026-08-08）：复核项 64 和新发现死代码先执行

## 任务

处置 S3 审查标记"需人工复核"的 **64 项死代码候选** + C-6 发现的**新死代码**。用户已拍板执行删除——但**每项删除前必须复核确认死**（报告是审查时点，C-0/C-1/C-2/C-5/C-6 改动后可能变化），并按 SSOT 原则先更新文档再删代码。

## 范围

- ✅ Desktop Core 6 项目 + Modules 7 + Roles 2 + Shell 1
- ✅ Shared（新发现死代码：PasswordHelper/RetryPolicyExtensions/PerformanceReport）
- ✅ 文档同步（README/ADR-0007/DESKTOP_ARCHITECTURE_STANDARD 等）
- ❌ 排除：extern P/Invoke 硬件 SDK（D72 HuaDaNativeMethods——**需确认无外部 DLL 依赖，本批次不删**，记录待用户确认）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线（C-6 完成后 `54bedadf3`）

---

## 第一部分：复核项 64（S3 §7.2，分 5 类）

### A. 基类 protected API 残留（7 项，D12-D18）

`NavigableViewModelBase.Async.cs`：`ExecuteWithErrorHandlingAsync`/`ExecuteWithErrorHandlingAsync<T>`/`RunOnUIThread`/`RunOnUIThreadAsync`（:22/:62/:102/:110）+ `AddDisposable`（:284）
`DialogViewModelBase.cs`：`CloseDialogWithResult<T>`（:125）/`TryGetDialogParameter<T>`（:174）

**复核**：确认 0 子类调用（grep `: [A-Za-z]+ViewModel` 继承链 + 方法名引用）。
**处置**：确认死后删除。
**前置**：这些是 **README/ADR-0007 文档化基类 API**——**先更新文档**（移除 API 描述/ADR 标注）再删代码（SSOT 原则）。

### B. DI 注册扩展 0 调用（3 项，D19-D21）

`HttpClientApiClientExtensions.cs`：`AddHttpClientApiClient(IContainerRegistry, int)`（:43）/`AddHttpClientApiClient(IContainerRegistry)`（:86）
`RefitApiClientExtensions.cs`：`AddRefitApiClient`（:34）

**复核**：确认无 DI 注册调用（Shell 已改走 SwitchingApiClient）。**处置**：确认死后删除 + 注释同步。

### C. 接口成员 0 消费（14 项，D22-D35）

按表逐项复核（接口+实现+调用点）：
- D22/D52 `InvalidateAll`（IDesktopCacheManager + DesktopCacheManager:94）
- D23 `IsLoggedInAsync` / D24 `CheckConnectionAsync`（IAuthenticationService + AuthenticationService:47/:221）
- D25 `StartupStepResult.SkippedResult`（IStartupPipeline:128）
- D26 `EnterReadOnlyMode`（WorkspaceState:88，FSM 重构遗留）
- D27 `GetByIdSimpleAsync` / D28 `CancelMedicalCaseViaApiAsync`（MedicalCaseService:240/:330）
- D29 `CanFire`/`GetPermittedEvents`（EditModeStateMachine:93/:170，接口成员）
- D30/D55 `BatchDeletePatientsAsync`（PatientService:98，仅接口声明）
- D31 `MatchPatientAsync`（PatientCardReaderIntegration:148，仅测试）
- D32 `CanRestore`（UserStatusHandler:67）
- D33/D49 `StopMonitoringAsync`/`ResetCircuitBreaker`（ApiHealthMonitor:75/:103）
- D34/D53 `HandleLoginSuccessAsync`/`GetDiagnostics`（LoginCoordinator:183/:271）
- D35/D57 `GetDiagnostics`×2（StartupPipeline:219/SessionLifecycleManager:237）

**规则**：接口+实现成对且 0 消费 → 全删；接口成员有实现但删除破坏编译 → 只删实现，接口留复核。**GetDiagnostics 3 处接口成员 0 消费——若接口被外部契约引用（如诊断 UI）保留接口注明**。

### D. 死代码 Model/映射（4 项，D36-D39）

- D36 `PatientItem.UpdateFromDto` + 整类（**C-6 已删整类**——本项应为 0，核实无残留）
- D37 `UserItem.UpdateFromDto` + 整类（**C-6 已删整类**——同上核实）
- D38 `HerbListControlViewModel.MoveItem`/`GetNextEmptySlotIndex`（:249/:296，类活仅 2 方法断链）
- D39 `FormulaMasterDetailViewModel.OnSelfPropertyChanged` 空体（**C-6 已删**——同上核实）

### E. 接口契约成员 0 业务调用（39 项，D43-D71）

按表逐项复核后处置（大量为"仅测试引用"/"重构后遗留 API"）：

**仅测试引用（删生产，保留/改测试）**：D65 CredentialVault 3 方法 / D66 PhotoStorage 3 方法 / D67 LogoutService 1 方法
**纯异步包装残留**：D43 `FireAsync`（消费方全用同步 Fire）
**整链预留（远程实现 throw NotSupportedException）**：D44/D45 `GetCategoriesAsync`×2 / D46 Registrations 3 方法
**重构后遗留 API 集群**：D60 `IAsyncExecutor` 7 成员（被 UnifiedApiClientExtensions/ServiceEventBridge 取代）
**其余接口成员**：D47 PerformanceMonitor 5 / D48 RoleRegistry 2 / D50 CommonDialog 4 / D51 ConnectionMode 1 / D54 NavigationCoordinator 2 / D56 SessionManager 4 / D58 UserNotification 1 / D59 DesktopExceptionHandler 5 / D61 SelectionService 2 / D62 DialogManager 3 / D63 NotificationService 3 / D64 CardReaderFactory 1 / D68 TokenValidator 1 / D69 TokenManager 4 / D70 ModuleLoading 2 / D71 PrescriptionPrint 1

**通用规则**：
1. 每项复核确认死（serena/grep 全仓含 XAML/DI/测试）
2. 接口+实现成对 0 消费 → 全删
3. 仅测试引用 → 删生产方法 + 删测试（测死功能）或改测试（测有效行为）
4. **D72 HuaDaNativeMethods 13 个 extern 不删**（硬件 SDK 预留，需外部 DLL 确认）
5. **删除破坏编译的接口成员** → 只删实现，接口留复核注明

---

## 第二部分：新发现死代码（C-6 报告）

1. `PasswordHelper.RandomByteLength`（:21，GenerateSalt 删后无引用）
2. `PasswordHelper.PasswordValidationResult` 类（ValidatePassword 删后生产 0 引用）
3. `PasswordHelper.CheckPasswordStrength`/`IsCommonPassword`（生产 0 调用，仅测试引用——**测试有效断言保留测试改走活方法，否则删测试**）
4. `RetryPolicyExtensions.CreateTimeoutPolicy`/`CreateHttpRetryPolicy`/`CreateCircuitBreakerPolicy`（:16/:43/:53，CreateCompositePolicy 删后 0 引用）
5. `PerformanceReport.GetLevelIndicator`/`FormatBytes`（GetFormattedReport 删后无引用）

---

## 第三部分：文档同步（SSOT，先文档后代码）

1. `DESKTOP_ARCHITECTURE_STANDARD.md:1179-1186` + 模块 README/AGENTS.md 中 PatientItem/UserItem 描述 → 删除/更新
2. README/ADR-0007 中基类 API 描述（D12-D18 若删）→ 同步移除
3. 蓝图 §3.2/§3.3 若有接口成员清单 → 同步（删除成员后）
4. **删除前先改文档，再改代码**（文档是 SSOT）

---

## 硬性约束

1. **每个删除前复核**：报告是审查时点，C 批次改动后可能变化——serena/grep 复查
2. **分 5 组独立 commit + push**：
   - `refactor(desktop): A-31-C7-A 基类 API 残留删除（D12-D18，先改 README/ADR-0007）`
   - `refactor(desktop): A-31-C7-B/C DI 扩展+接口成员删除（D19-D35）`
   - `refactor(desktop): A-31-C7-D/E 死 Model+接口契约成员删除（D36-D71）`
   - `refactor(shared): A-31-C7-S 新发现死代码删除（PasswordHelper/RetryPolicy/PerformanceReport）`
   - `docs: A-31-C7 文档同步（DESKTOP_ARCHITECTURE_STANDARD/README/ADR/蓝图）`
3. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`（每组后）
4. **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿（每组后）
5. 产出报告：`docs/compose/reports/a31-c7-review-items-cleanup.md`（每项复核结论/删除/保留/D72 待确认）

## 明确不做（防发散）

- ❌ 不删 D72 HuaDaNativeMethods extern（硬件 SDK，待用户确认外部 DLL）
- ❌ 不删 D25 StartupStepResult.SkippedResult 若被 startup 管道流程引用（复核为准）
- ❌ 不碰映射双机制（决策点 4）、跨模块门面（决策点 2）
- ❌ 不执行项目合并（C-3/C-4 延期）
- ❌ 不重构（只删，不提取/不合并/不改逻辑）
- ❌ 不删"路由存活/0 客户端消费"端点（Reports/Health——需产品确认）
