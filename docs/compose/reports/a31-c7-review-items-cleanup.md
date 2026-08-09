# 任务 A-31-C7：复核项 64 处置 + 新发现死代码清理报告

> 完成日期：2026-08-09｜依据：S3 报告 `method-audit-desktop-2026-08-08.md` §7.2（复核项 64）+ C-6 报告「新发现死代码」表
> 基线：C-6 完成后 `54bedadf3`｜分支 `master`｜分 5 组独立 commit + push

## 执行摘要

| 组 | 内容 | Commit | 验证 |
|----|------|--------|------|
| A | 基类 protected API 残留 7（D12-D18，先文档后代码） | `b2c28d27e` | build 0/0 + 架构 88/88 |
| B/C | DI 扩展 3（D19-D21）+ 接口成员 14（D22-D35） | `6e1f032fe` | build 0/0 + 架构 88/88 |
| D/E | 死 Model 核实 + D38 + 接口契约 39（D43-D71，D72 保留） | `4492d06b7` | build 0/0 + 架构 88/88 + Desktop 受影响单测 227 通过 |
| S | 新发现死代码（PasswordHelper/RetryPolicyExtensions/PerformanceReport） | `15656271d` | build 0/0 + 架构 88/88 + Server PasswordHelper 10/10 |
| Docs | 文档同步（DESKTOP_ARCHITECTURE_STANDARD/README/AGENTS/总账） | 本 commit | 无代码改动 |

**删除总量**：~3,750 行（A 160 + B/C 1,108 + D/E 1,936 + S 545）。**每项删除前均 serena/grep 全仓复核**（含 XAML/DI/测试），C 批次改动后引用已逐一验证为 0。

---

## 第一部分：复核项 64（S3 §7.2）

### A. 基类 protected API 残留（D12-D18，7 项）→ 全部删除

**复核**：grep 全仓（src + tests + XAML）0 子类调用；`_disposables` 字段仍被 `Editable.cs` Dispose 读取，删除 `AddDisposable` 无 CS0414 风险；`UiDispatcher` 为 protected 属性保留。

| # | 项 | 处置 |
|---|----|------|
| D12 | `ExecuteWithErrorHandlingAsync`（Async.cs:22） | ✅ 删（文件删除） |
| D13 | `ExecuteWithErrorHandlingAsync<T>`（Async.cs:62） | ✅ 删（文件删除） |
| D14 | `RunOnUIThread`（Async.cs:102） | ✅ 删（文件删除） |
| D15 | `RunOnUIThreadAsync`（Async.cs:110） | ✅ 删（文件删除） |
| D16 | `AddDisposable`（NavigableViewModelBase.cs:284） | ✅ 删 |
| D17 | `CloseDialogWithResult<T>`（DialogViewModelBase.cs:125） | ✅ 删 |
| D18 | `TryGetDialogParameter<T>`（DialogViewModelBase.cs:174） | ✅ 删 |

**文档先行**：README.md 5 行 API 表、ADR-0007:10 `ExecuteWithErrorHandlingAsync` 名称、02-desktop.md:274 命令模式代码示例（改为 try/catch + SetError 现行模式）先同步再删代码。

### B. DI 注册扩展 0 调用（D19-D21，3 项）→ 全部删除

**复核**：Shell 已改走 `UnifiedApiClientExtensions.AddUnifiedApiClient`（SwitchingApiClient 路径，`UnifiedApiClientExtensions.cs:106-114`）；`AddHttpClientApiClient`×2 / `AddRefitApiClient` 全仓 0 调用；`HttpClientName` 常量与内部 `LocalWebApiHttpClientFactory` 仅被死方法引用（Shell 侧有独立同构实现）。

| # | 项 | 处置 |
|---|----|------|
| D19 | `AddHttpClientApiClient(IContainerRegistry, int)` | ✅ 删（整文件 `HttpClientApiClientExtensions.cs` 删除） |
| D20 | `AddHttpClientApiClient(IContainerRegistry)` | ✅ 删（同上） |
| D21 | `AddRefitApiClient` | ✅ 删（整文件 `RefitApiClientExtensions.cs` 删除） |

### C. 接口成员 0 消费（D22-D35，14 项）→ 全部删除（成对）

**复核**：接口+实现成对 grep 0 消费；`GetDiagnostics` 3 处确认无诊断 UI 消费者（`LoginFlowDiagnostics`/`StartupPipelineDiagnostics`/`SessionDiagnostics` record 仅被死方法引用，连带删除）。

| # | 项 | 处置 |
|---|----|------|
| D22/D52 | `InvalidateAll`（IDesktopCacheManager + DesktopCacheManager:94） | ✅ 删（接口+实现） |
| D23 | `IsLoggedInAsync`（IAuthenticationService + AuthenticationService:47） | ✅ 删（接口+实现 + 测试 Act 3 联动：AuthenticationIntegrationTests `TokenClear_ExpiredToken_RequireRelogin` 移除依赖段） |
| D24 | `CheckConnectionAsync`（IAuthenticationService:59 + AuthenticationService:221） | ✅ 删（接口+实现） |
| D25 | `StartupStepResult.SkippedResult`（IStartupPipeline:128） | ✅ 删——复核确认 startup 管道无引用（`Skipped` 属性仍被活代码使用，保留） |
| D26 | `EnterReadOnlyMode`（WorkspaceState:88） | ✅ 删（+ WorkspaceStateTests 对应测试；`EnterEditMode` 非本任务范围保留） |
| D27 | `GetByIdSimpleAsync`（MedicalCaseService:240） | ✅ 删 |
| D28 | `CancelMedicalCaseViaApiAsync`（MedicalCaseService:330） | ✅ 删 |
| D29 | `CanFire`/`GetPermittedEvents`（IEditModeStateMachine + EditModeStateMachine:93/:170） | ✅ 删（接口+实现 + EditModeStateMachineTests E.10/E.12 共 6 测试；`Fire` 内联守卫不经过它们） |
| D30/D55 | `BatchDeletePatientsAsync`（IPatientService:36 + PatientService:98） | ✅ 删（接口+实现 + Patients README 方法列表同步） |
| D31 | `MatchPatientAsync`（IPatientCardReaderIntegration:148） | ✅ 删（接口+实现 + 整文件 `PatientMatchFallbackChainTests.cs` 8 测试；`PatientMatchType`/`PatientMatchResult` 连带删除；需求文档 11e-cardreader/13-traceability-matrix 标注「实现已移除，需求待产品决策」） |
| D32 | `CanRestore`（IUserStatusHandler:37 + UserStatusHandler:67） | ✅ 删（接口+实现 + Users README 2 处） |
| D33/D49 | `StopMonitoringAsync`/`ResetCircuitBreaker`（IApiHealthMonitor + ApiHealthMonitor:75/:103） | ✅ 删（接口+实现） |
| D34/D53 | `HandleLoginSuccessAsync`/`GetDiagnostics`（ILoginCoordinator + LoginCoordinator:183/:271） | ✅ 删（接口+实现 + LoginCoordinator:228 注释同步 + `LoginFlowDiagnostics` record 连带） |
| D35/D57 | `GetDiagnostics`×2（StartupPipeline:219 + SessionLifecycleManager:237） | ✅ 删（接口+实现 + `StartupPipelineDiagnostics`/`StartupStepDiagnostics`/`SessionDiagnostics` record 连带 + StartupPipelineTests 诊断区 2 测试 + Contracts README:124） |

### D. 死代码 Model/映射（D36-D39，4 项）→ 核实 + 删除

| # | 项 | 处置 |
|---|----|------|
| D36 | `PatientItem` 整类 | ✅ C-6 已删，**本批次核实 0 残留**（仅剩 DESKTOP_ARCHITECTURE_STANDARD 文档引用，Docs 组已清理） |
| D37 | `UserItem` 整类 | ✅ C-6 已删，**本批次核实 0 残留**（同上） |
| D38 | `HerbListControlViewModel.MoveItem`/`GetNextEmptySlotIndex`（:249/:296） | ✅ 删（grep 0 引用；`RequestNewSlot` 仍被 code-behind 调用证明类活）+ 02-desktop.md:420 同步 |
| D39 | `FormulaMasterDetailViewModel.OnSelfPropertyChanged` 空体 | ✅ C-6 已删，**本批次核实 0 残留** |

### E. 接口契约成员 0 业务调用（D43-D71，39 项）→ 全部删除

**复核**：逐项 grep（src + tests）；「仅测试引用」5 组删生产 + 删/改测试；D60 集群全链（接口+类+DI 注册+组合属性）0 消费确认。

| # | 项 | 处置 |
|---|----|------|
| D43 | `IAuthenticationStateMachine.FireAsync`（:46 + 实现:180） | ✅ 删（消费方全用同步 `Fire`） |
| D44 | `IApiClientHerbs.GetCategoriesAsync`（:114 + HerbsHttpApiClient:68 + HerbApiClient:87） | ✅ 删（接口+双实现；远程 throw NotSupportedException 预留链） |
| D45 | `IApiClientFormulas.GetCategoriesAsync`（:136 + FormulasHttpApiClient:78 + FormulaApiClient:100） | ✅ 删（同上） |
| D46 | `IApiClientRegistrations` 3 方法（GetRegistrationsAsync/QuickVisitAsync/DeleteRegistrationAsync，:82/:88/:94 + 双实现） | ✅ 删 |
| D47 | `IPerformanceMonitor` 5 方法（RecordMemoryBaseline/GetMemorySnapshots/GetMetric/GetAllMetrics/GenerateReport，:30/:35/:42/:47/:53 + PerformanceMonitor:108-154） | ✅ 删（仅 StartTiming/StopTiming 活） |
| D48 | `IRoleRegistry.GetAllDefinitions`/`IsRegistered`（:27/:33 + RoleRegistry:57/:63） | ✅ 删（+ Contracts/Infrastructure README 同步） |
| D49 | 见 D33 | ✅（B/C 组处理） |
| D50 | `ICommonDialogService` 4 方法（ShowInfoAsync/ShowInputAsync/ShowOpenFileDialogAsync/ShowSaveFileDialogAsync，:30/:70/:78/:87 + CommonDialogService:23-102） | ✅ 删（+ Contracts README:181-185 同步；`using Microsoft.Win32` 连带移除） |
| D51 | `IConnectionModeService.TestLocalConnectionAsync`（:78 + ConnectionModeService:123） | ✅ 删 |
| D52 | 见 D22 | ✅（B/C 组处理） |
| D53 | 见 D34 | ✅（B/C 组处理） |
| D54 | `INavigationCoordinator.SubscribeToRegionCollection`/`UnsubscribeFromRegionCollection`（:121/:126 + NavigationCoordinator:294/:297） | ✅ 删 |
| D55 | 见 D30 | ✅（B/C 组处理） |
| D56 | `ISessionManager` 4 方法（SetSession/HasRole/IsAdmin/GetCurrentUserRoleDisplay，:48/:70/:75/:80 + SessionManager:43/:73-75） | ✅ 删（grep `.SetSession(`/`.HasRole(`/`.IsAdmin()`/`.GetCurrentUserRoleDisplay(` 0 调用）+ README 同步 |
| D57 | 见 D35 | ✅（B/C 组处理） |
| D58 | `IUserNotificationService.ShowInfoAsync`（:34 + UserNotificationService:68） | ✅ 删 |
| D59 | `IDesktopExceptionHandler` 5 方法（LogException/CanRetry/UnregisterGlobalExceptionHandlers/SafeExecuteAsync×2，:24/:34/:47/:66/:71 + DesktopExceptionHandler:36/:57/:95/:216/:229） | ✅ 删（+ DesktopExceptionHandlerTests 6 个 CanRetry 测试；`using System.Net.Sockets` 连带移除；RegisterGlobalExceptionHandlers 保留） |
| D60 | `IAsyncExecutor` 7 成员（ExecuteSafelyAsync×2/ExecuteWithRetryAsync×2/ExecuteOnUIThread/ExecuteOnUIThreadAsync/ExecuteWithTimeoutAsync） | ✅ **集群全链删除**：接口 `IAsyncExecutor.cs` + 类 `AsyncExecutor.cs` + DI 注册（ViewModelServicesExtensions:32）+ 组合属性（IMasterDetailServices/IListViewServices/MasterDetailServices/ListViewServices）+ 4 个 VM 测试 16 处引用 + Infrastructure README（否则类字段 CS0414 警告） |
| D61 | `ISelectionService.SelectMultiple`/`ToggleSelection`（:44/:50 + SelectionService:60/:75） | ✅ 删（+ Infrastructure README:174/:181 同步） |
| D62 | `IDialogManager` 3 方法（ShowInfoAsync/ShowInputAsync/ShowDialogAsync，:36/:53/:62 + DialogManager:43/:93/:120） | ✅ 删 |
| D63 | `INotificationService` 3 方法（ShowInfoAsync/ShowLoading/HideLoading，:37/:57/:62 + NotificationService:75/:122/:143） | ✅ 删（+ 连带 `LoadingStateChanged` 事件与 `LoadingStateChangedEventArgs` 类型——否则 CS0067 事件未使用警告） |
| D64 | `ICardReaderFactory.GetSupportedReaders`（:14 + CardReaderFactory:55） | ✅ 删（`_supportedReaders`/`CheckDllAvailability` 仍被 AutoDetectReaderAsync 使用，保留） |
| D65 | `ICredentialVault` 3 方法（VerifyIntegrityAsync/MigrateOldFormatAsync/HasValidTokenAsync，:78/:84/:91） | ✅ 删（**仅测试引用**：CredentialVaultTests 3 区 9 测试删除；业务用 Save/Get/ClearPassword） |
| D66 | `IPhotoStorageService` 3 方法（LoadPhotoAsync/DeletePhotoAsync/PhotoExists，:22/:29/:34） | ✅ 删（**仅测试引用**：DpapiPhotoStorageServiceTests 7 测试删除 + SavePhotoAsync_SameIdentifier_OverwritesFile 改走路径断言；业务用 SavePhotoAsync） |
| D67 | `ILogoutService.ProcessPendingServerLogoutsAsync`（:37 + LogoutService:141） | ✅ 删（**仅测试引用**：LogoutServiceTests 3 测试删除 + 连带私有 `PublishPendingLogoutsClearedEvent`；`_pendingLogouts`/`_processingLock` 仍被活代码使用） |
| D68 | `ITokenValidator.ValidateAndGetUserInfoAsync`（:28 + LocalTokenValidator:177） | ✅ 删（**仅测试引用**：LocalTokenValidatorTests 2 测试删除；业务用 ValidateTokenAsync） |
| D69 | `ITokenManager` 4 方法（SetTokens/ClearTokens/IsTokenValid/IsTokenExpiringSoon，:39/:44/:50/:57） | ✅ 删（AccessToken/RefreshToken/AccessTokenExpiry 属性保留——SignalRClient 用 AccessToken；字段补 `= null` 初始化避免 CS0649） |
| D70 | `IModuleLoadingService.GetLoadedModules`/`LoadModulesAsync`（:22/:39 + ModuleLoadingService:83/:99） | ✅ 删（ModuleLazyLoader 用 IsModuleLoaded/LoadModuleAsync） |
| D71 | `IPrintService.BatchPrintAsync`（:40 + PrescriptionPrintService:173） | ✅ 删（+ Printing README:39/AGENTS.md:53、modules/printing.md:36、09-printing.md:55 需求文档标注移除） |
| D72 | `HuaDaNativeMethods` 13 extern | ⏸️ **保留**（硬件 SDK P/Invoke 预留，删除前须确认无外部 DLL 依赖——待用户确认） |

---

## 第二部分：新发现死代码（C-6 报告）→ 全部删除

| 项 | 位置 | 处置 |
|----|------|------|
| `PasswordHelper.RandomByteLength` 私有常量 | PasswordHelper.cs:21 | ✅ 删 |
| `PasswordHelper.PasswordValidationResult` 类 | PasswordHelper.cs | ✅ 删（+ Server PasswordHelperTests `PasswordValidationResult_DefaultConstructor` 测试；生产 0 引用） |
| `PasswordHelper.CheckPasswordStrength`/`IsCommonPassword` | PasswordHelper.cs | ✅ 删（生产 0 调用仅测试引用——测死功能删测试：CheckPasswordStrength×2 + IsCommonPassword Theory；强度校验走活方法 `PasswordPolicyValidator`；`WeakPasswords` 集合连带删除避免 CS0169；`using Regex/Enums` 连带移除） |
| `RetryPolicyExtensions.CreateTimeoutPolicy`/`CreateHttpRetryPolicy`/`CreateCircuitBreakerPolicy` | RetryPolicyExtensions.cs:16/:43/:53 | ✅ 删（整文件删除：3 工厂 + 私有 `ShouldRetry` + `RetryPolicyOptions` 全链 0 消费；`RetryPolicyIntegrationTests.cs` 整文件 9 测试删除） |
| `PerformanceReport.GetLevelIndicator`/`FormatBytes` | PerformanceReport.cs | ✅ **C-6 已连带删除**（GetFormattedReport 删除时一并移除）——本批次 grep 0 残留核实，无动作 |

---

## 第三部分：文档同步（SSOT，先文档后代码）

| 文档 | 变更 |
|------|------|
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/README.md` | 基类 API 表 5 行（D12-D18）+ RoleRegistry/SessionManager/SelectionService/AsyncExecutor 条目 |
| `docs/03-architecture/decisions/0007-viewmodel-composition-pattern.md` | :10 移除 `ExecuteWithErrorHandlingAsync` 名称 |
| `docs/03-architecture/02-desktop.md` | :274 命令示例改现行 try/catch 模式；:420 空槽管理；:541-562 读卡器架构图/接口表移除 MatchPatientAsync |
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/README.md` | IStartupPipeline/IRoleRegistry/ISessionManager/ICommonDialogService 成员表 |
| `src/Client/Desktop/Core/LYBT.Desktop.Printing/README.md` + `AGENTS.md` | BatchPrintAsync 移除 |
| `docs/03-architecture/modules/printing.md` | IPrintService 接口契约移除 BatchPrintAsync |
| `docs/02-requirements/09-printing.md` | 业务规则 5 标注批量打印移除（需求待产品决策） |
| `docs/02-requirements/11e-cardreader.md` + `13-traceability-matrix.md` | US-CARD-002/MatchPatientAsync 标注实现移除（需求待产品决策） |
| `src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md` | §4.3 命令示例 UserItem→UserListDto；§4.5 Mapperly 示例改活实现 FormulaDetailModelMapper；§6.3.1 目录树移除 PatientItem.cs；§9.2 命名示例改 HerbItem/FormulaItem；**§10.1.3-10.1.5（UI Model/Mapperly Mapper/ViewModel 三个 UserItem 遗留章节）整体移除** |
| `src/Client/Desktop/Modules/LYBT.Desktop.Patients/README.md` + `AGENTS.md`、`LYBT.Desktop.Users/AGENTS.md` | PatientItem/UserItem 描述移除 |
| `docs/03-architecture/14-structure-design-blueprint.md` | 复核 §3.2/§3.3：为模块结构表，无接口成员清单，无需同步 |
| `docs/03-architecture/13-project-master-plan.md` | A-31 行 C-7 状态 ⬜→✅ + 5 组 Commit SHA |

---

## 保留项与待确认

1. **D72 `HuaDaNativeMethods` 13 个 extern**（HD_ReadCard/GetCertNo/.../IsDllAvailable）——**保留**，硬件 SDK P/Invoke 预留（adapter 只用 HD_InitComm 等 5 个）；删除前须确认无外部 DLL 依赖，**待用户确认**。
2. `AuthenticationIntegrationTests` 4 个失败（NSubstitute 无法代理 Refit 接口 `IAuthApi`）——**基线既有问题**（组 B/C 提交前用 git stash 验证：clean 状态同样 4 失败），非本次改动引入。
3. `US-CARD-002`（患者匹配降级链）与批量打印需求——实现已移除，**需求状态待产品决策**（已在 11e-cardreader/09-printing/13-traceability-matrix 标注）。
4. `AuthenticationStateMachine.CanFire/GetPermittedEvents`（Foundation）——S3 候选但**不在 C-7 清单**（D29 仅指 EditModeStateMachine），保留未动，可留待后续批次。

## 架构约束合规

- **P07/P08/P10**：未引入任何新引用关系；删除均为成对（接口+实现）或整链，无模块边界变更
- **双控制器树**：本批次不涉及权限/端点变更（无 Controller 改动），无需双端同步
- **0 错误 0 警告**：每组 `dotnet build LYBTZYZS.sln --no-incremental` 均 0 错误 0 警告（存量警告一并修复：CS0649 TokenManager 字段、CS0067 NotificationService 事件）
- **测试联动**：测死功能测试全删（18 个测试类/区域），测活行为测试保留或改走活方法
