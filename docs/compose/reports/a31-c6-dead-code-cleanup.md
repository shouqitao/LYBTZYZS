# A-31-C6 死代码清理报告（C 批次收官）

> 执行者：Mimo Code ｜ 日期：2026-08-09
> 任务书：`docs/compose/specs/task-a31-c6-dead-code-cleanup-2026-08-08.md`
> 基线：C-5 完成 `9c19791e8` ｜ 分支 `master`
> 依据：S1 §6（method-audit-shared）+ S2 §7（method-audit-server）+ S3 §7.1/7.4（method-audit-desktop）
> 方针：只删符号级确认安全的死代码（机械删除项）；复核项不删留待人工；禁止顺手清理

## 执行摘要

| 组 | 内容 | Commit | 验证 |
|----|------|--------|------|
| C6-1 Shared | 12 符号 + 测死功能测试联动（8 生产文件 + 4 测试文件） | `1c9c2545b` | build 0/0，Shared 单测 110/110 |
| C6-2 Server | 29 符号（真死 14 + 死链 9 + 常量 2 + 接口 5 组）+ 连带死端点 6 + ReferenceCheckResult record | `a9293616d` | build 0/0，单测 22/22 + 架构 88/88 |
| C6-3 Desktop | 14 项 + 2 死类 + OnSelfPropertyChanged 空体 + 测死功能测试联动 | `b15fc8597` | build 0/0，Desktop 13/13 + 架构 88/88 |

三个 commit 均已 `git push origin master`。全 Solution `dotnet build LYBTZYZS.sln --no-incremental` 三组均 **0 错误 0 警告**；架构测试 `LYBT.Tests.Architecture` 三组均 **88/88 全绿**。

---

## C6-1 Shared（commit `1c9c2545b`）

### 删除清单（每项删除前已重新 grep 核实调用点）

| # | 符号 | 位置 | 核实结果 | 处理 |
|---|------|------|---------|------|
| 1 | `SetCorrelationId` | `ActivityCorrelationIdProvider.cs:24` + `ICorrelationIdProvider.cs:19` | 全仓 0 调用 | 接口+实现成对删 |
| 2 | `GetCorrelationIdOrNew` | `ActivityCorrelationIdProvider.cs:33` | 全仓 0 调用 | 删 |
| 3 | `WriteToConsoleWithTemplate` | `LoggerConfigurationExtensions.cs:68` | 全仓 0 调用 | 删 + 连带 `using Serilog.Events` |
| 4 | `WriteToFileWithTemplate` | `LoggerConfigurationExtensions.cs:91` | 全仓 0 调用 | 同上 |
| 5 | `MaskObject` | `SensitiveDataMasker.cs:142` | 生产 0 调用；仅测试引用 | 删 + 删测死功能测试 |
| 6 | `SanitizeException` | `SensitiveDataMasker.cs:275` | C-1 后复查仍死（生产 0 调用，仅测试引用） | 删 + 删测死功能测试 |
| 7 | `NotFoundException.User/Patient/Herb/MedicalCase/Formula`（5 静态工厂） | `NotFoundException.cs:54-68` | 生产 0 调用（实际构造为直接 `new`）；仅 1 处测试引用 | 删 + 删测死功能测试 |
| 8 | `Result<T>.ValidationFailure` | `Result.cs:75` | 全仓 0 调用 | 删 |
| 9 | `ErrorCodeExtensions.GetModuleName` | `ErrorCodeExtensions.cs:287` | 生产 0 调用；仅测试引用 | 删 + 测试联动（见下） |
| 10 | `PasswordHelper.GenerateTemporaryPassword` | `PasswordHelper.cs:45` | 生产 0 调用；仅测试引用 | 删 + 删测死功能测试 |
| 11 | `PasswordHelper.GenerateSalt` | `PasswordHelper.cs:76` | 同上 | 同上 |
| 12 | `PasswordHelper.ValidatePassword` | `PasswordHelper.cs:98` | 同上 | 同上 |
| 13 | `PasswordHelper.SecureEquals` | `PasswordHelper.cs:318` | 同上 | 同上 |

### 测试联动（测死功能 → 删测试；有效断言 → 保留）

- `SensitiveDataMaskerTests.cs`：删 `SanitizeException 测试` + `MaskObject 测试` 两个 region（测死功能）；Mask/IsSensitiveFieldName/SanitizeText/MaskUri 测试保留（测存活方法）
- `PasswordHelperTests.cs`：删 SecureEquals ×5 + ValidatePassword ×8 + GenerateTemporaryPassword ×2 + GenerateSalt ×2（测死功能）；CheckPasswordStrength/IsCommonPassword/GenerateSecurePassword/PasswordValidationResult/PasswordStrength 测试保留；类注释同步更新
- `ErrorCodeTests.cs`：删 `模块分区测试` region（GetModuleName_* ×6，测死功能）；`HerbMcceeCodes/PatientMcceeCodes/FormulaMcceeCodes/MedicalCaseMcceeCodes` 4 个测试中的 `GetModuleName` 断言行删除（保留 ErrorMessages 有效断言）；`AuthMcceeCodes_AllReturn401` 中的 `GetModuleName` 断言行删除
- `BusinessExceptionTests.cs`：删 `NotFoundException_StaticFactory_User_CreatesCorrectException`（测死工厂）；`NotFoundException_WithTypedErrorCode_SetsCorrectStatus` / `new NotFoundException(resourceType, resourceId)` 保留（测存活构造器）

### 验证

- `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
- `dotnet test tests/LYBT.Tests.Server/ --filter "SensitiveDataMaskerTests|PasswordHelperTests|ErrorCodeTests|BusinessExceptionTests"`：110/110 通过

---

## C6-2 Server（commit `a9293616d`）

### 删除清单（S2 §7.1 逐项核实执行）

| # | 符号 | 位置 | 处理 |
|---|------|------|------|
| D1 | `GetOverallStatusAsync` | `IHealthCheckService.cs:21` + `HealthCheckService.cs:91` | 接口+实现成对删 |
| D2 | `GetHerbByNameOrPinyinAsync` | `IHerbCrossModuleService.cs:15` + `HerbCrossModuleService.cs:40` | 接口+实现成对删 |
| D3 | `CheckHerbReferenceAsync` | `IHerbCrossModuleService.cs:18` + `HerbCrossModuleService.cs:59` | 接口+实现成对删 + **连带删 `ReferenceCheckResult.cs` record 文件**（S2 报告注明的死 record） |
| D4 | `UpdateUserPasswordHashAsync` | `IUserCrossModuleService.cs:18` + `UserCrossModuleService.cs:53` | 接口+实现成对删 |
| D5 | `UserExistsAsync` | `IUserCrossModuleService.cs:21` + `UserCrossModuleService.cs:64` | 接口+实现成对删 |
| D7 | `GetPagedAsync` 3 参重载 | `PatientRepository.cs:23-28` + `IPatientRepository.cs:16` | 实现+接口成员删；4 参重载保留（`PatientService.cs:25` 消费） |
| D8 | `ToListDtos(List<Registration>)` | `RegistrationMapper.cs:21`（Mapperly partial） | 删；`MedicalCaseMapper.ToListDtos`（活）不受影响 |
| D9 | `CountConnections` | `RegistrationConnectionManager.cs:28` | 生产 0 调用，仅测试 → 删 + 删测死功能测试 |
| D10 | `TryGetDoctorId(string, out Guid)` 公共版 | `RegistrationConnectionManager.cs:24` | 0 调用；`RegistrationHub.cs:63` 私有同名重载保留；测试联动（见下） |
| D11 | `ToPrescriptionEntity` / `UpdatePrescriptionEntity` | `MedicalCaseMapper.cs:105,120`（Mapperly partial） | 生产 0 调用，仅测试 → 删 + 删测死功能测试 + 连带删测试私有 helper `CreateTestPrescriptionInputDto` |
| D12 | `GetConsultationListAsync`/`GetPrescriptionListAsync`/`GetBatchDetailDtosAsync`/`GetPatientConsultationsAsync`/`GetPatientPrescriptionsAsync` | `MedicalCaseQueryService.cs:94,112,382,392,420` + `IMedicalCaseQueryService.cs:66,74,148,153,158` | 死链 5 方法：唯一调用方为死端点 → 接口+实现成对删 |
| D13 | `AddPrintLogAsync` | `MedicalCaseCommandService.cs:293` + `IMedicalCaseCommandService.cs:86` | 死链：唯一调用方为死端点 → 删；repository 层 `AddPrintLogAsync`（`MedicalCaseRepository.AuditLogs.cs:63`，被 `RecordPrintAsync` 使用）**保留** |
| D14 | `GetPatientConsultationsPagedAsync`/`GetPatientPrescriptionsPagedAsync`/`GetBatchWithDetailsAsync` | `MedicalCaseRepository.cs:73,87,273` + `IMedicalCaseRepository.cs:27,32,101` | 死链 3 方法：唯一调用方为 D12 → 接口+实现成对删 |
| D15 | `SuperAdminUserType` / `DefaultUserType` | `RoleConstants.cs:16,21` | 常量 0 引用，删 |

### 连带删除（删除清单项的必要条件，S2 §7.2「死链整体清理」）

- **`BaseMedicalCasesController` 6 个死端点**（`GetPatientConsultations`/`GetPatientPrescriptions`/`GetConsultations`/`GetPrescriptions`/`GetBatchDetails`/`AddPrintLog`，:88-155/:233-249）：D12/D13 的**唯一调用方**。S2 §7.2 已核实「双端子类均未 override、Desktop Refit 契约无对应方法 → 0 客户端消费」。删除 D12/D13 物理上必须移除这些端点，否则编译失败。Remote WebAPI + Desktop LocalWebAPI 双端均继承该基类，删除后双端路由一致移除（无双轨分叉）。
- 测试联动：
  - `RegistrationConnectionManagerTests.cs`：删 `Add_Then_TryGetDoctorId_ReturnsMappedDoctor`/`CountConnections_CountsOnlyTargetDoctorConnections`/`TryGetDoctorId_UnknownConnection_ReturnsFalse`（测死功能）；`TryRemove_RemovesMappingAndReturnsDoctorId` 中 `TryGetDoctorId` 断言改写为 `TryRemove` 二次调用（保留有效断言改走活方法）
  - `MedicalCaseMapperTests.cs`：删 `ToPrescriptionEntity 测试`/`UpdatePrescriptionEntity 测试` 两个 region（测死功能）

### 验证

- `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
- `dotnet test tests/LYBT.Tests.Server/ --filter "RegistrationConnectionManagerTests|MedicalCaseMapperTests"`：22/22 通过
- `dotnet test tests/LYBT.Tests.Architecture/`：88/88 通过

---

## C6-3 Desktop（commit `b15fc8597`）

### 删除清单（S3 §7.1「可安全删 14」+ 2 死类 + 空体）

| # | 符号 | 位置 | 核实结果 |
|---|------|------|---------|
| D1 | `BuildQueryString` (protected static) | `HttpApiClientBase.cs:117` | 12 子类无任何调用，grep 双 0 |
| D2 | `GetSafeMessageWithTrackingCode` | `ClientErrorMessageMapper.cs:365` | 全仓 0 调用（连测试也无） |
| D3 | `GetMessageWithTrackingCode` | `ClientErrorMessageMapper.cs:381` | 同上 |
| D4 | `GetFullTrackingCode` | `ClientErrorMessageMapper.cs:404` | 仅测试引用 → 删 + 删测死功能测试（`ErrorTraceCodeTests.cs` 删 `US_ERR_007_GetFullTrackingCode_WithProvider_ReturnsFullId`） |
| D5 | `GetOptimalColumnCount` (public static) | `ResponsiveLayoutHelper.cs:43` | 0 调用；同类 GetScreenCategory/GetRecommendedMasterWidth 被 `MasterDetailLayout.xaml.cs` 使用 |
| D6 | `GetDisplayName` (public static ext) | `DuplicateDosageStrategy.cs:62` | 0 调用（含 tests） |
| D7 | `ForManagementView` | `MedicalCaseNavigationParameters.cs:61` | 0 调用（ForClinical 在用） |
| D8 | `ForManagementEdit` | `MedicalCaseNavigationParameters.cs:78` | 0 调用 |
| D9 | `GetFormattedReport` | `PerformanceReport.cs:53` | 0 调用 |
| D10 | `GetJsonReport` | `PerformanceReport.cs:101` | 0 调用 |
| D11 | `ForceState` (internal) | `AuthenticationStateMachine.cs:206` | 生产+tests 双 0 |
| D40 | `DeserializeAsync<T>` (protected static) | `HttpApiClientBase.cs:53` | 0 引用；同文件 DeserializeEnvelopeAsync 等被子类调用 |
| D41 | `WpfUiThreadDispatcher(Dispatcher)` (internal ctor) | `WpfUiThreadDispatcher.cs:17` | 0 引用；public ctor（DI 注册 `ViewModelServicesExtensions.cs:28`）保留 |
| D42 | `CreateCompositePolicy` | `RetryPolicyExtensions.cs:86` | 仅测试引用 → 删 + 删测死功能测试（`RetryPolicyIntegrationTests.cs` 删 `CompositePolicy_WhenTransientFailure_ShouldRetryAndSucceed`/`CompositePolicy_WhenSuccessOnFirstAttempt_ShouldNotRetry`） |
| D36 | `PatientItem` 整类 | `Patients/Models/Items/PatientItem.cs` | 全仓（含 XAML/nameof）0 实例化 → 文件删除 |
| D37 | `UserItem` 整类 | `Users/Models/Items/UserItem.cs` | 同上 → 文件删除 |
| D39 | `OnSelfPropertyChanged` 空体 | `FormulaMasterDetailViewModel.cs:354` | 空体逻辑死 → 删方法 + **连带删 ctor:78 订阅 `PropertyChanged +=` 与 Dispose:363 退订 `-=`**（否则编译失败） |

### 验证

- `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
- `dotnet test tests/LYBT.Tests.Desktop/ --filter "ErrorTraceCodeTests|RetryPolicyIntegrationTests"`：13/13 通过
- `dotnet test tests/LYBT.Tests.Architecture/`：88/88 通过

---

## 保留复核项（不删，按任务书）

1. **S3 §7.2 复核项 64**（接口契约 39+14 / 文档化基类 API 7 / DI 扩展 3 / 死 Model 4 / extern SDK 1 组）——留待人工评审
2. **BaseCrudController 防呆桩**（GetList/BatchDelete 等基类 `throw new NotSupportedException` 桩）——子类 override 的契约签名，删除破坏编译
3. **S2 §7.2 剩余 0 消费端点**：WebAPI `HealthController.GetDetailedHealth`、`ReportsController` 5 个 trend 端点——判 C 不判 D，需产品确认外部 API 消费者后另行处理
4. **S2 §7.2 死链相关端点**已在本批次连带删除（见 C6-2），不重复保留

## 新发现死代码（记录不处理，任务书「禁止顺手清理」）

| 项 | 位置 | 说明 |
|----|------|------|
| `PasswordHelper.RandomByteLength` 私有常量 | `PasswordHelper.cs:21` | `GenerateSalt` 删除后无引用 |
| `PasswordHelper.PasswordValidationResult` 类 | `PasswordHelper.cs` | `ValidatePassword` 删除后生产 0 引用（测试 `PasswordValidationResult_DefaultConstructor` 保留） |
| `PasswordHelper.CheckPasswordStrength` / `IsCommonPassword` | `PasswordHelper.cs` | 生产 0 调用，仅测试引用（S1 清单外） |
| `RetryPolicyExtensions.CreateTimeoutPolicy` / `CreateHttpRetryPolicy` / `CreateCircuitBreakerPolicy` | `RetryPolicyExtensions.cs:16,43,53` | `CreateCompositePolicy` 删除后 0 引用（public API） |
| `PerformanceReport.GetLevelIndicator` / `FormatBytes`（private） | `PerformanceReport.cs` | `GetFormattedReport` 删除后无引用 |
| `MedicalCaseMapper.ToDetailDtos` 间接引用面 | `MedicalCaseMapper.cs:63` | 经核实仍被 `SearchMedicalCasesAsync`/`GetPatientRecentMedicalCasesAsync` 调用（活，排除） |
| 文档漂移：`DESKTOP_ARCHITECTURE_STANDARD.md:1179-1186`、模块 README/AGENTS.md 中 PatientItem/UserItem 描述 | docs/ | S3 报告 W6 已记录，文档同步另行处理 |

## 架构约束合规

- 双控制器树：本批次仅删 `BaseMedicalCasesController`（Remote 与 LocalWebAPI 共同基类）死端点，双端路由一致移除，无权限/端点分叉
- 接口成员删除：全部「接口+实现成对且 0 消费」→ 全删；无「删除破坏编译」的单删场景（死链端点已连带清理）
- P07/P08/P10：未引入任何新引用关系；删除了 CrossModule 接口死成员，不违反模块边界
