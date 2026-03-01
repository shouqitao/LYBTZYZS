# LYBT.Desktop.Contracts 代码知识

Desktop 端接口契约层，定义跨模块共享的抽象接口、API 客户端契约和事件类型。所有模块通过此层实现依赖倒置，消除编译期直接依赖。

## 架构决策

| 决策 | 原因 | 日期 | 关联 OpenSpec |
|------|------|------|--------------|
| AuthState 统一状态机替代双状态机 | 原有 LoginState + LoginFlowState 架构冗余，合并为单一 AuthState 枚举 (11个状态 + AuthEvent 转换) | Phase 1.1 | refactor-auth-role-system |
| MedicalCase 服务 SRP 三分离 | 将原单体 MedicalCaseService 拆分为 Query/Command/Lifecycle 三个接口，各司其职 | ADR-1 | refactor-frontend-srp-patterns |
| CommandHandler 统一返回类型 | 所有 CommandHandler 方法使用 CommandResult<T> 返回，确保错误处理一致性 | Phase 1.4 | unify-desktop-architecture |
| INavigationCoordinator 整合三个导航服务 | 合并 NavigationManager + ViewNavigationService + RoleNavigationService 为单一协调器 | ADR-3 + ADR-7 | unify-navigation-architecture |
| IViewModelServices 聚合接口 | 将 ViewModel 基类所需的 7 个通用服务聚合为 1 个接口，简化构造函数 | - | enhance-viewmodel-architecture |
| IApplicationTickService 统一定时调度 | 单一 DispatcherTimer 每秒 Tick，替代各组件独立 Timer，减少资源浪费 | AUTH-000 | refactor-token-sliding-expiration |
| IUserActivityState 分离查询接口 | 从 IUserActivityTracker 提取只读查询接口，供 Foundation 层使用，避免循环依赖 | AUTH-002 | refactor-token-sliding-expiration |
| DataSource 抽象层 | IDataSourceBase<TDetail, TInput> 统一 CRUD 操作，支持远程/本地双模式切换 | - | SYNC-D02 |
| CrossModule 搜索接口 | IHerbSearchProvider / IFormulaSearchProvider 解耦模块间编译期依赖 (D5-3) | - | - |
| IPendingQueueManager 解耦 | 待诊队列管理独立接口，消除 MedicalCase 和 Patients 模块的直接耦合 | - | refactor-medicalcase-workspace |
| LoginCoordinator 简化 | 移除 rememberCredentials 参数（凭证保存由 ViewModel 处理）、移除 IsAutoLogin、移除 AutoLoginAttemptCount | - | simplify-login-options |
| AutoLoginToken 机制 | 替代密码存储，支持服务端撤销和 Token 轮换 | CVT-001 | refactor-login-authentication |
| IMedicalCaseApi 聚合保存 | SaveAsync (PUT /medicalcases/{id}) 一次保存诊断+处方，减少 API 调用次数 | Phase 3.5 | refactor-medicalcase-aggregate-crud |
| QueryMedicalCasesAsync 统一查询 | 整合多种查询方式为单一端点 GET /medicalcases/query | - | optimize-medicalcase-api |
| IDesktopCacheManager 统一失效 | 按域 (Patients/MedicalCases/All) 统一管理缓存失效 | - | - |
| ICurrentUserProvider 审计字段 | 为 LocalDbContext 提供当前用户 ID，填充审计字段 | - | implement-local-mode |
| ImportValidationResult 提升到 Contracts | 原在 Infrastructure 层，提升到 Contracts 避免循环依赖 | Issue #1781 | - |
| IRoleDefinition 策略模式 | 每个角色实现此接口定义模块加载和导航行为，替代 switch-case | Phase 2.1.1 | refactor-auth-role-system |

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| ISessionManager.SetCurrentUser | [COMPAT] 兼容保留 | SetSession (支持 RefreshToken) | 待全面迁移后移除 |
| ISessionManager.SetUserSession | [COMPAT] 兼容保留 | SetSession 的别名 | 待全面迁移后移除 |
| ISessionManager.ClearUserSession | [COMPAT] 兼容保留 | ClearSession 的别名 | 待全面迁移后移除 |
| IUserDataSource 过渡态方法 (T4-X2) | [COMPAT] 兼容保留 | 待 SYNC-D02 完成后统一重构 | SYNC-D02 完成后 |
| IPatientDataSource 过渡态方法 | [COMPAT] 兼容保留 | 待 SYNC-D02 完成后统一重构 | SYNC-D02 完成后 |
| IHerbDataSource 过渡态方法 | [COMPAT] 兼容保留 | 待 SYNC-D02 完成后统一重构 | SYNC-D02 完成后 |
| IFormulaDataSource 过渡态方法 | [COMPAT] 兼容保留 | 待 SYNC-D02 完成后统一重构 | SYNC-D02 完成后 |
| SessionExpiring 事件 | [DEAD] 已移除 | simplify-auth-architecture: 不再显示即将过期警告 | 已清理 |
| SessionExpiringEventArgs | [DEAD] 已移除 | simplify-auth-architecture | 已清理 |

状态值: [DEAD] 已废弃 | [COMPAT] 兼容保留 | [PENDING] 待重构

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| GetPendingCasesAsync 与 QueryMedicalCasesAsync 不能互相替代 | 返回 DTO 类型不同: Pending 返回 PendingMedicalCaseDto (含 Type 字段), Query 返回 MedicalCaseListDto (含 CaseStatus 字段) | 保留两个独立端点 (standardize-api-naming) |
| IMedicalCaseApi 中大量已删除方法的注释 | API 演进过程中删除了重复/Ghost API，注释保留用于追溯 | 不要尝试恢复这些方法，它们的功能已合并到现有方法中 |
| IDataSourceBase.GetPagedAsync 与具体模块的同名方法签名不同 | 基础接口只有 keyword 参数，Herb/Formula 的 DataSource 重载增加了 category 参数 | 使用具体接口类型而非基础接口调用带 category 的方法 |
| ICommonDialogService 与 IUserNotificationService 功能重叠 | 历史原因: IUserNotificationService 原名 IErrorHandlingService (Issue #840 重命名), ICommonDialogService 后加入提供更丰富的对话框 | IUserNotificationService 用于简单消息提示, ICommonDialogService 用于需要用户选择的对话框 |
| ISessionManager.IsLoggedIn 是 IsAuthenticated 的别名 | 兼容性保留，两个属性行为完全一致 | 新代码统一使用 IsAuthenticated |
| CommandResult<T> 隐式转换为 bool | 使用 `if (result)` 判断成功/失败，但可能被误用为空值检查 | 明确使用 `result.Success` 提高可读性 |

## OpenSpec 追踪

| OpenSpec ID | 内容 | 状态 |
|-------------|------|------|
| refactor-auth-role-system | 统一认证状态机 (AuthState/AuthEvent/IAuthenticationStateMachine)、角色定义 (IRoleDefinition/IRoleRegistry)、登录协调器 (ILoginCoordinator) | 已完成 |
| refactor-token-sliding-expiration | 应用级 Tick 服务 (AUTH-000)、用户活动追踪 (AUTH-001/002/003)、IUserActivityState 分离 | 已完成 |
| unify-desktop-architecture | CommandHandler 统一接口/返回类型/查询参数 (Phase 1.4) | 已完成 |
| unify-navigation-architecture | INavigationCoordinator 整合导航 (ADR-3 + ADR-7) | 已完成 |
| refactor-frontend-srp-patterns | MedicalCase 服务三分离 (ADR-1): IMedicalCaseQueryService / IMedicalCaseCommandService / IMedicalCaseLifecycleService; IAsyncInitializable 接口 | 已完成 |
| clarify-cancel-consultation-logic | IActiveConsultationService、LeaveConsultationChoice 枚举 | 已完成 |
| optimize-medicalcase-navigation | UnfinishedCaseChoice 四选项弹窗、ICommonDialogService.ShowUnfinishedCaseDialogAsync | 已完成 |
| simplify-login-options | 移除 rememberCredentials/IsAutoLogin/AutoLoginAttemptCount | 已完成 |
| simplify-auth-architecture | 移除 SessionExpiring 事件和 SessionExpiringEventArgs | 已完成 |
| refactor-medicalcase-workspace | IPendingQueueManager 解耦 MedicalCase 和 Patients 模块 | 已完成 |
| enhance-viewmodel-architecture | IViewModelServices 聚合接口 (7个服务 -> 1个注入) | 已完成 |
| standardize-desktop-api-layer | API 返回类型修正 (IUserApi)、导入导出功能 (IFormulaApi) | 已完成 |
| standardize-api-naming | 统一 ApiResponse 返回类型、REQ-API-002 批量操作 URL 模式、保留 PendingCases 独立端点 | 已完成 |
| optimize-medicalcase-api | QueryMedicalCasesAsync 统一查询端点 | 已完成 |
| consolidate-medicalcase-queries | SearchMedicalCasesAsync 跨医案搜索 (LIFECYCLE-015)、删除 CreateMedicalCaseWithDetailsAsync/SoftDeleteMedicalCaseAsync (Phase 7) | 已完成 |
| consolidate-medicalcase-detail-queries | 删除 GetMedicalCasesByPatientIdAsync/GetMedicalCaseByIdWithDetailsAsync/GetUnfinishedCaseByPatientIdAsync、新增 GetBatchDetailsAsync | 已完成 |
| simplify-medicalcase-api | 删除 Ghost APIs (ClearPrescription/ImportFormulaIntoPrescription)、删除独立 Prescription CRUD、删除 UpdateConsultationAsync | 已完成 |
| post-release-cleanup | 合并 GetMedicalCasesListAsync 到 GetMedicalCasesAsync，统一返回 MedicalCaseListDto | 已完成 |
| fix-history-copy-all-patients | GetMedicalCasesAsync 添加 includeAllDoctors 参数 | 已完成 |
| unify-pending-query-api | GetPendingCasesAsync 添加 patientId 参数支持按患者筛选 | 已完成 |
| refactor-medicalcase-api | SuspendAsync (LIFECYCLE-010) 挂起医案 | 已完成 |
| refactor-medicalcase-management | GetPermissionsAsync (LIFECYCLE-007)、GetAuditLogsAsync (LIFECYCLE-008) | 已完成 |
| refactor-medicalcase-aggregate-crud | SaveAsync 聚合保存 (Phase 3.5) | 已完成 |
| optimize-module-list-ui | IPatientApi/IHerbApi/IFormulaApi 恢复功能 (Restore) 和状态切换 (ToggleStatus) | 已完成 |
| optimize-batch-operations | Phase 2: 批量删除/启用/禁用 (Herbs/Formulas/Users/Patients/MedicalCases) | 已完成 |
| refactor-login-authentication | AutoLoginToken 自动登录 (CVT-001) | 已完成 |
| cleanup-patient-dead-code | 删除重复的 PagedResult<T> 和未使用的 IPagedCommandHandler | 已完成 |
| cleanup-formula-dead-code | 删除 GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync | 已完成 |
| implement-local-mode | ICurrentUserProvider 接口 | 已完成 |
| SYNC-D02 | DataSource 过渡态方法 (Herb/Patient/Formula/User)，待双模式完成后统一重构 | 进行中 |
| enhance-duplicate-herb-dialog | IPrescriptionSettingsService 处方设置 | 已完成 |
| rationalize-module-architecture | IMedicalCaseQueryService 遵循依赖倒置原则 | 已完成 |

## 代码文件结构

### Api/ -- Refit API 客户端接口 (7个)

Refit 在编译期自动生成实现类，无需手写实现。

#### IAuthApi.cs

认证 API 客户端，JWT 认证、会话管理、健康检查。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| POST | `LoginAsync(LoginRequest) -> ApiResponse<LoginResponse>` | /api/v1/auth/login |
| POST | `LoginWithAutoTokenAsync(AutoLoginRequest) -> ApiResponse<LoginResponse>` | /api/v1/auth/auto-login |
| POST | `LogoutAsync(LogoutRequest) -> ApiResponse` | /api/v1/auth/logout |
| POST | `RefreshTokenAsync(RefreshTokenRequest) -> ApiResponse<LoginResponse>` | /api/v1/auth/refresh |
| GET | `ValidateTokenFromHeaderAsync() -> ApiResponse<object>` | /api/v1/auth/validate |
| POST | `ValidateTokenAsync(ValidateTokenRequest) -> ApiResponse<ValidateTokenResponse>` | /api/v1/auth/validate |
| GET | `HealthCheckAsync() -> HealthCheckResponse` | /api/v1/health |

#### IFormulaApi.cs

验方 CRUD + 批量操作 + 导入导出。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| GET | `GetFormulasAsync(page, pageSize, keyword?, category?) -> ApiResponse<PagedResult<FormulaListDto>>` | /api/v1/formulas |
| GET | `GetFormulaByIdAsync(id) -> ApiResponse<FormulaDetailDto>` | /api/v1/formulas/{id} |
| POST | `CreateFormulaAsync(FormulaInputDto) -> ApiResponse<FormulaDetailDto>` | /api/v1/formulas |
| PUT | `UpdateFormulaAsync(id, FormulaInputDto) -> ApiResponse<FormulaDetailDto>` | /api/v1/formulas/{id} |
| DELETE | `DeleteFormulaAsync(id) -> ApiResponse` | /api/v1/formulas/{id} |
| POST | `CloneFormulaAsync(id) -> ApiResponse<FormulaDetailDto>` | /api/v1/formulas/{id}/clone |
| POST | `ToggleStatusAsync(id) -> ApiResponse<FormulaDetailDto>` | /api/v1/formulas/{id}/toggle-status |
| POST | `RestoreAsync(id) -> ApiResponse<FormulaDetailDto>` | /api/v1/formulas/{id}/restore |
| POST | `BatchDeleteAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/formulas/batch-delete |
| POST | `BatchEnableAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/formulas/batch-enable |
| POST | `BatchDisableAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/formulas/batch-disable |
| POST | `BatchImportAsync(FormulaBatchImportInputDto) -> ApiResponse<FormulaBatchImportResultDto>` | /api/v1/formulas/batch-import |
| GET | `ExportFormulasAsync(category?) -> HttpResponseMessage` | /api/v1/formulas/export |
| GET | `ExportTemplateAsync() -> HttpResponseMessage` | /api/v1/formulas/import-template |

#### IHerbApi.cs

药材 CRUD + 批量操作 + 导入导出。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| GET | `GetHerbsAsync(page, pageSize, keyword?, category?) -> ApiResponse<PagedResult<HerbListDto>>` | /api/v1/herbs |
| GET | `GetHerbByIdAsync(id) -> ApiResponse<HerbDetailDto>` | /api/v1/herbs/{id} |
| POST | `CreateHerbAsync(HerbInputDto) -> ApiResponse<HerbDetailDto>` | /api/v1/herbs |
| PUT | `UpdateHerbAsync(id, HerbInputDto) -> ApiResponse<HerbDetailDto>` | /api/v1/herbs/{id} |
| DELETE | `DeleteHerbAsync(id) -> ApiResponse` | /api/v1/herbs/{id} |
| POST (Multipart) | `BatchImportAsync(StreamPart file) -> ApiResponse<HerbBatchImportResultDto>` | /api/v1/herbs/import |
| GET | `ExportTemplateAsync() -> HttpResponseMessage` | /api/v1/herbs/import-template |
| GET | `ExportHerbsAsync(keyword?) -> HttpResponseMessage` | /api/v1/herbs/export |
| POST | `ToggleStatusAsync(id) -> ApiResponse<HerbDetailDto>` | /api/v1/herbs/{id}/toggle-status |
| POST | `RestoreAsync(id) -> ApiResponse<HerbDetailDto>` | /api/v1/herbs/{id}/restore |
| POST | `BatchDeleteAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/herbs/batch-delete |
| POST | `BatchEnableAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/herbs/batch-enable |
| POST | `BatchDisableAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/herbs/batch-disable |

#### IMedicalCaseApi.cs

医案 CRUD + 生命周期 + 打印 + 批量操作。方法最多的 API 接口。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| GET | `GetMedicalCasesAsync(page, pageSize, keyword?, includeAllDoctors) -> ApiResponse<PagedResult<MedicalCaseListDto>>` | /api/v1/medicalcases |
| GET | `QueryMedicalCasesAsync(queryType, patientId?, doctorId?, keyword?, pageIndex, pageSize, includeAllDoctors, limit?) -> ApiResponse<PagedResult<MedicalCaseListDto>>` | /api/v1/medicalcases/query |
| GET | `GetMedicalCaseByIdAsync(id) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id} |
| GET | `GetPendingCasesAsync(patientId?) -> ApiResponse<List<PendingMedicalCaseDto>>` | /api/v1/medicalcases/pending |
| GET | `SearchMedicalCasesAsync(patientName?, diagnosisKeyword?, startDate?, endDate?, page, pageSize) -> ApiResponse<PagedResult<MedicalCaseDetailDto>>` | /api/v1/medicalcases/search |
| POST | `CreateMedicalCaseAsync(MedicalCaseInputDto) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases |
| PUT | `SaveAsync(id, MedicalCaseInputDto) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id} |
| DELETE | `DeleteMedicalCaseAsync(id) -> ApiResponse` | /api/v1/medicalcases/{id} |
| PUT | `SetPrescriptionFlagAsync(medicalCaseId, SetPrescriptionFlagRequest) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id}/prescription-flag |
| PUT | `CloseCaseAsync(id) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id}/close |
| PUT | `SuspendAsync(id, ConsultationInputDto?) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id}/suspend |
| PUT | `CancelMedicalCaseAsync(id, CancelMedicalCaseRequestDto?) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id}/cancel |
| PUT | `UpdateStatusAsync(id, MedicalCaseStatusInputDto) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id}/status |
| GET | `GetPermissionsAsync(id) -> ApiResponse<MedicalCasePermissionDto>` | /api/v1/medicalcases/{id}/permissions |
| GET | `GetAuditLogsAsync(id, page, pageSize) -> ApiResponse<MedicalCaseAuditLogPagedResultDto>` | /api/v1/medicalcases/{id}/audit-logs |
| PUT | `RecordPrintCompletedAsync(medicalCaseId, PrintCompletedRequest) -> ApiResponse<MedicalCaseDetailDto>` | /api/v1/medicalcases/{id}/print-completed |
| POST | `AddPrintLogAsync(medicalCaseId, PrintLogInputDto) -> ApiResponse<object>` | /api/v1/medicalcases/{id}/print-logs |
| POST | `BatchDeleteAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/medicalcases/batch-delete |
| POST | `GetBatchDetailsAsync(BatchDetailQueryDto) -> ApiResponse<List<MedicalCaseDetailDto>>` | /api/v1/medicalcases/batch-details |

#### IPatientApi.cs

患者 CRUD + 批量操作 + 导入导出。注: 患者无 Status 字段，无 ToggleStatus 方法。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| GET | `GetPatientsAsync(page, pageSize, keyword?) -> ApiResponse<PagedResult<PatientListDto>>` | /api/v1/patients |
| GET | `GetPatientByIdAsync(id) -> ApiResponse<PatientDetailDto>` | /api/v1/patients/{id} |
| POST | `CreatePatientAsync(PatientInputDto) -> ApiResponse<PatientDetailDto>` | /api/v1/patients |
| PUT | `UpdatePatientAsync(id, PatientInputDto) -> ApiResponse<PatientDetailDto>` | /api/v1/patients/{id} |
| DELETE | `DeletePatientAsync(id) -> ApiResponse` | /api/v1/patients/{id} |
| POST | `BatchImportAsync(PatientBatchImportInputDto) -> ApiResponse<PatientBatchImportResultDto>` | /api/v1/patients/batch-import |
| GET | `ExportTemplateAsync() -> HttpResponseMessage` | /api/v1/patients/import-template |
| GET | `ExportPatientsAsync(keyword?) -> HttpResponseMessage` | /api/v1/patients/export |
| POST | `RestoreAsync(id) -> ApiResponse<PatientDetailDto>` | /api/v1/patients/{id}/restore |
| POST | `BatchDeleteAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/patients/batch-delete |

#### ISyncApi.cs

数据同步 API，对应服务器端 SyncController。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| GET | `GetEntityTypesAsync() -> ApiResponse<IReadOnlyList<string>>` | /api/v1/sync/entity-types |
| GET | `GetMetadataAsync(entityType) -> ApiResponse<List<SyncMetadataDto>>` | /api/v1/sync/metadata |
| POST | `CompareAsync(SyncCompareInputDto) -> ApiResponse<SyncCompareResultDto>` | /api/v1/sync/compare |
| POST | `UploadAsync(SyncUploadInputDto) -> ApiResponse<SyncUploadResultDto>` | /api/v1/sync/upload |
| POST | `DownloadAsync(SyncDownloadInputDto) -> ApiResponse<SyncDownloadResultDto>` | /api/v1/sync/download |
| POST | `DeleteAsync(SyncDeleteInputDto) -> ApiResponse<SyncDeleteResultDto>` | /api/v1/sync/delete |

#### IUserApi.cs

用户 CRUD + 密码管理 + 批量操作。

| HTTP | 方法签名 | 路由 |
|------|----------|------|
| GET | `GetUsersAsync(page, pageSize, keyword?) -> ApiResponse<PagedResult<UserListDto>>` | /api/v1/users |
| GET | `GetUserByIdAsync(id) -> ApiResponse<UserDetailDto>` | /api/v1/users/{id} |
| POST | `CreateUserAsync(UserInputDto) -> ApiResponse<UserDetailDto>` | /api/v1/users |
| PUT | `UpdateUserAsync(id, UserInputDto) -> ApiResponse<UserDetailDto>` | /api/v1/users/{id} |
| DELETE | `DeleteUserAsync(id) -> ApiResponse` | /api/v1/users/{id} |
| PUT | `ChangeProfileAsync(id, ChangeProfileDto) -> ApiResponse<UserDetailDto>` | /api/v1/users/{id}/profile |
| PUT | `ChangePasswordAsync(id, ChangePasswordRequest) -> ApiResponse` | /api/v1/users/{id}/change-password |
| POST | `ResetPasswordAsync(id, ResetPasswordRequestDto) -> ApiResponse<ResetPasswordResponseDto>` | /api/v1/users/{id}/reset-password |
| POST | `BatchImportAsync(UserBatchImportInputDto) -> ApiResponse<UserBatchImportResultDto>` | /api/v1/users/batch-import |
| POST | `ToggleStatusAsync(id) -> ApiResponse<UserDetailDto>` | /api/v1/users/{id}/toggle-status |
| POST | `RestoreAsync(id) -> ApiResponse<UserDetailDto>` | /api/v1/users/{id}/restore |
| POST | `BatchDeleteAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/users/batch-delete |
| POST | `BatchEnableAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/users/batch-enable |
| POST | `BatchDisableAsync(BatchDeleteInputDto) -> ApiResponse<BatchOperationResultDto>` | /api/v1/users/batch-disable |

---

### CommandHandlers/ -- 统一命令模式 (Phase 1.4)

#### CommandResult.cs

统一返回类型，支持泛型和无数据两种形式。

- `CommandResult<T>` -- record(Success, Data?, Error?)
  - `static Succeeded(T data) -> CommandResult<T>`
  - `static Failed(string error) -> CommandResult<T>`
  - `static NotFound(string? message) -> CommandResult<T>`
  - `implicit operator bool` -- 隐式转换为 bool

- `CommandResult` -- record(Success, Error?)
  - `static Succeeded() -> CommandResult`
  - `static Failed(string error) -> CommandResult`
  - `implicit operator bool` -- 隐式转换为 bool

#### ICommandHandlerBase.cs

泛型 CRUD 接口和只读接口。

- `ICommandHandlerBase<TListDto, TDetailDto, TInputDto>` -- 约束: where T : class
  - `GetListAsync(QueryParams?) -> Task<CommandResult<List<TListDto>>>`
  - `GetDetailAsync(Guid id) -> Task<CommandResult<TDetailDto>>`
  - `SaveAsync(TInputDto input) -> Task<CommandResult<TDetailDto>>`
  - `DeleteAsync(Guid id) -> Task<CommandResult<bool>>`

实现者: IFormulaCommandHandler, IUserCommandHandler, IPatientCommandHandler

#### QueryParams.cs

统一查询参数 record。

- 属性: SearchText, Page(=1), PageSize(=20), SortBy, SortDescending, Filters
- `static Default -> QueryParams`
- `static Search(string) -> QueryParams`
- `static Paged(int, int) -> QueryParams`
- `WithFilter(string key, object value) -> QueryParams` -- 不可变追加过滤条件

---

### DataSources/ -- 数据源抽象层 (支持双模式)

每个接口均有 Remote + Local 两个实现 (RemoteXxxDataSource + LocalXxxDataSource)。

#### IDataSourceBase.cs

通用 CRUD 基础接口 `IDataSourceBase<TDetail, TInput>` (约束: where T : class)。

- `GetByIdAsync(Guid id, CancellationToken) -> Task<TDetail?>`
- `GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken) -> Task<(List<TDetail>, int Total)>`
- `CreateAsync(TInput, CancellationToken) -> Task<TDetail>`
- `UpdateAsync(TInput, CancellationToken) -> Task<TDetail>`
- `DeleteAsync(Guid id, CancellationToken) -> Task<bool>`

#### IFormulaDataSource.cs

继承 `IDataSourceBase<FormulaDetailDto, FormulaInputDto>`，扩展验方特有操作。

- `CloneAsync(Guid id, ct) -> Task<FormulaDetailDto?>`
- `ToggleStatusAsync(Guid id, ct) -> Task<bool>`
- `RestoreAsync(Guid id, ct) -> Task<FormulaDetailDto?>`
- `GetWithHerbsAsync(Guid id, ct) -> Task<FormulaDetailDto?>`
- `GetPagedAsync(page, pageSize, keyword?, category?, ct) -> Task<(List<FormulaDetailDto>, int)>` -- 带分类的重载
- `BatchImportAsync(List<FormulaImportItemDto>, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `GetPendingValidationAsync(ct) -> Task<List<FormulaDetailDto>>` -- [COMPAT] SYNC-D02
- `GetAllForExportAsync(keyword?, ct) -> Task<List<FormulaDetailDto>>` -- [COMPAT] SYNC-D02
- `ValidateHerbBindingsAsync(Guid formulaId, ct) -> Task<bool>` -- [COMPAT] SYNC-D02
- `BatchToggleStatusAsync(List<Guid>, bool enable, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `BatchDeleteAsync(List<Guid>, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `GetImportTemplateColumns() -> string[]` -- [COMPAT] SYNC-D02
- `GetImportTemplateHerbColumns() -> string[]` -- [COMPAT] SYNC-D02

#### IHerbDataSource.cs

继承 `IDataSourceBase<HerbDetailDto, HerbInputDto>`，扩展药材特有操作。

- `GetPagedAsync(page, pageSize, keyword?, category?, ct) -> Task<(List<HerbDetailDto>, int)>` -- 带分类的重载
- `ToggleStatusAsync(Guid id, ct) -> Task<bool>`
- `RestoreAsync(Guid id, ct) -> Task<HerbDetailDto?>`
- `GetCategoriesAsync(ct) -> Task<List<string>>`
- `BatchDeleteAsync(List<Guid>, ct) -> Task<BatchOperationResultDto>`
- `BatchToggleStatusAsync(List<Guid>, bool enable, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `BatchImportAsync(List<HerbInputDto>, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `GetAllForExportAsync(keyword?, ct) -> Task<List<HerbDetailDto>>` -- [COMPAT] SYNC-D02
- `HasReferencesAsync(Guid herbId, ct) -> Task<bool>` -- [COMPAT] SYNC-D02
- `GetImportTemplateColumns() -> string[]` -- [COMPAT] SYNC-D02

#### IMedicalCaseDataSource.cs

继承 `IDataSourceBase<MedicalCaseDetailDto, MedicalCaseInputDto>`，扩展聚合根操作。

- `AddPrintLogAsync(medicalCaseId, isSuccess, printType, printerName?, errorMessage?, ct) -> Task<bool>`
- `SaveAsync(MedicalCaseInputDto, ct) -> Task<MedicalCaseDetailDto>` -- 聚合保存
- `CompleteAsync(Guid id, ct) -> Task<bool>`
- `CancelAsync(Guid id, reason?, ct) -> Task<bool>`
- `GetWithDetailsAsync(Guid id, ct) -> Task<MedicalCaseDetailDto?>`
- `QueryAsync(patientId?, userId?, status?, startDate?, endDate?, page, pageSize, ct) -> Task<(List<MedicalCaseDetailDto>, int)>`
- `GetByPatientIdAsync(Guid patientId, ct) -> Task<List<MedicalCaseDetailDto>>`
- `BatchDeleteAsync(List<Guid>, ct) -> Task<BatchOperationResultDto>`

#### IPatientDataSource.cs

继承 `IDataSourceBase<PatientDetailDto, PatientInputDto>`，扩展患者特有操作。

- `SearchAsync(string keyword, ct) -> Task<List<PatientDetailDto>>`
- `GetByIdNumberAsync(string idNumber, ct) -> Task<PatientDetailDto?>`
- `RestoreAsync(Guid id, ct) -> Task<PatientDetailDto?>`
- `BatchDeleteAsync(List<Guid>, ct) -> Task<BatchOperationResultDto>`
- `BatchImportAsync(List<PatientInputDto>, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `GetAllForExportAsync(keyword?, ct) -> Task<List<PatientDetailDto>>` -- [COMPAT] SYNC-D02
- `HasMedicalCasesAsync(Guid patientId, ct) -> Task<bool>` -- [COMPAT] SYNC-D02
- `BatchCheckReferencesAsync(List<Guid>, ct) -> Task<Dictionary<Guid, bool>>` -- [COMPAT] SYNC-D02

#### IUserDataSource.cs

继承 `IDataSourceBase<UserDetailDto, UserInputDto>`，扩展用户特有操作。

- `GetByUsernameAsync(string username, ct) -> Task<UserDetailDto?>`
- `ChangePasswordAsync(Guid id, oldHash, newHash, ct) -> Task<bool>`
- `ToggleStatusAsync(Guid id, ct) -> Task<bool>`
- `UpdateLastLoginTimeAsync(Guid id, ct) -> Task<bool>`
- `ResetFailedLoginCountAsync(Guid id, ct) -> Task<bool>`
- `IncrementFailedLoginCountAsync(Guid id, ct) -> Task<int>`
- `RestoreAsync(Guid id, ct) -> Task<UserDetailDto?>` -- [COMPAT] SYNC-D02
- `BatchDeleteAsync(List<Guid>, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `ResetPasswordAsync(Guid id, ct) -> Task<ResetPasswordResponseDto>` -- [COMPAT] SYNC-D02
- `BatchToggleStatusAsync(List<Guid>, bool enable, ct) -> Task<BatchOperationResultDto>` -- [COMPAT] SYNC-D02
- `GetCurrentUserAsync(ct) -> Task<UserDetailDto?>` -- [COMPAT] SYNC-D02

---

### Events/ -- 事件定义

#### CacheEvents.cs

- `CacheEvents.InvalidatedEvent` -- 继承 `PubSubEvent<CacheInvalidatedPayload>`，缓存失效通知
- `CacheInvalidatedPayload` -- record: Domain(CacheDomain), Reason(string), Timestamp(DateTime)
- `CacheDomain` -- 枚举: Patients, MedicalCases, All

---

### Models/ -- 契约模型

#### ImportValidationResult.cs

导入验证结果类 (Issue #1781 从 Infrastructure 提升到 Contracts 避免循环依赖)。

- 属性: IsValid, Errors(List<string>), Warnings(List<string>), ValidRowCount, InvalidRowCount

---

### Roles/ -- 角色体系

#### IRoleDefinition.cs

角色定义接口，策略模式替代 switch-case (Phase 2.1.1)。

- 属性: Role(UserRole), DisplayName, Description, HomeViewName, RequiredModules(IReadOnlyList<string>), BaseModules(IReadOnlyList<string>)
- `GetAllModules() -> IEnumerable<string>` -- 返回基础模块 + 角色特定模块

#### IRoleRegistry.cs

角色注册表接口 (Phase 2.1.2)。

- `Register(IRoleDefinition) -> void`
- `GetDefinition(UserRole) -> IRoleDefinition?`
- `GetAllDefinitions() -> IReadOnlyCollection<IRoleDefinition>`
- `IsRegistered(UserRole) -> bool`
- `GetHomeViewName(UserRole) -> string`
- `GetModulesForRole(UserRole) -> IEnumerable<string>`

---

### Security/ -- 安全认证

#### AuthState.cs

三个类型定义于同一文件。

- `AuthState` -- 枚举(11值): Idle(0), Authenticating(1), ValidatingToken(2), LoadingProfile(3), LoadingModules(4), Navigating(5), Authenticated(10), Failed(20), LoggingOut(30), SessionExpired(40), RefreshingToken(50)

- `AuthEvent` -- 枚举(15值): StartLogin, StartAutoLogin, CredentialsValidated, TokenValidated, ProfileLoaded, ModulesLoaded, NavigationCompleted, LoginFailure, StartLogout, LogoutSuccess, LogoutFailure, SessionExpire, StartTokenRefresh, TokenRefreshSuccess, TokenRefreshFailure, Reset

- `AuthStateChangedEventArgs` -- EventArgs 子类
  - 属性: PreviousState(AuthState), CurrentState(AuthState), Trigger(AuthEvent), StatusMessage(string?), Timestamp(DateTime)

#### IAuthenticationStateMachine.cs

统一认证状态机接口，替代原双状态机架构。

- 属性: CurrentState(AuthState), IsAuthenticated, IsTransitioning, StatusMessage(string?)
- `Fire(AuthEvent, statusMessage?) -> bool`
- `FireAsync(AuthEvent, statusMessage?) -> Task<bool>`
- `CanFire(AuthEvent) -> bool`
- `Reset() -> void`
- `GetPermittedEvents() -> IEnumerable<AuthEvent>`
- 事件: `StateChanged -> EventHandler<AuthStateChangedEventArgs>`

---

### Services/ -- 服务接口

#### CrossModule/IFormulaSearchProvider.cs

验方搜索提供者，解耦 MedicalCase 对 Formula 模块的编译期依赖 (D5-3)。

- `GetFormulasPagedAsync(int page, int pageSize) -> Task<PagedResult<FormulaListDto>>`
- `GetFormulaByIdAsync(Guid id) -> Task<FormulaDetailDto?>`

#### CrossModule/IHerbSearchProvider.cs

药材搜索提供者，解耦 MedicalCase/Formula 对 Herbs 模块的编译期依赖 (D5-3)。

- `SearchHerbsAsync(string keyword) -> Task<IReadOnlyList<HerbListDto>>`
- `GetAllHerbsAsync() -> Task<IReadOnlyList<HerbListDto>>`

#### IActiveConsultationService.cs

活跃医案追踪，离开确认逻辑。

- 属性: HasActiveConsultation(bool), ActiveMedicalCaseId(Guid?)
- `Register(Guid medicalCaseId, Func<Task<LeaveConsultationResult>> leaveHandler) -> void`
- `Unregister() -> void`
- `RequestLeaveAsync() -> Task<LeaveConsultationResult>`
- 附属类型: `LeaveConsultationResult` -- CanLeave(bool), Choice(LeaveConsultationChoice), 静态工厂 AllowLeave/CancelLeave
- 附属枚举: `LeaveConsultationChoice` -- None, Suspend, CancelCase, Stay

#### IApplicationTickService.cs

统一 1 秒定时调度 (AUTH-000)，单一 DispatcherTimer 替代各组件独立 Timer。

- 属性: TickCount(long), IsRunning(bool)
- `Start() -> void`
- `Stop() -> void`
- 事件: `Tick -> EventHandler<ApplicationTickEventArgs>`
- 附属类型: `ApplicationTickEventArgs` -- TickCount(long), Timestamp(DateTime)

#### IAsyncInitializable.cs

异步初始化接口，View 加载时自动执行。

- `InitializeAsync() -> Task`

注: 被 MasterDetailViewModelBase/MasterDetailControlBase 类型检查引用，无直接实现类

#### ICommonDialogService.cs

通用对话框服务，提供丰富的用户交互对话框。

- `ShowInfoAsync(message, title?) -> Task`
- `ShowWarningAsync(message, title?) -> Task`
- `ShowErrorAsync(message, title?) -> Task`
- `ShowConfirmAsync(message, title?) -> Task<bool>`
- `ShowTripleChoiceAsync(message, title?) -> Task<TripleChoiceResult>` -- 是/否/取消
- `ShowInputAsync(message, title?, defaultValue?) -> Task<string?>`
- `ShowOpenFileDialogAsync(filter?, title?) -> Task<string?>`
- `ShowSaveFileDialogAsync(filter?, title?, defaultFileName?) -> Task<string?>`
- `ShowUnfinishedCaseDialogAsync(string patientName) -> Task<UnfinishedCaseChoice>`
- 附属枚举: `TripleChoiceResult` -- Yes, No, Cancel

#### ICurrentUserProvider.cs

当前用户 ID 提供者，供 LocalDbContext 审计字段填充。

- 属性: CurrentUserId(Guid?)

实现: SessionBasedCurrentUserProvider (Shell)

#### IDesktopCacheManager.cs

缓存失效管理器，按域统一管理。

- `InvalidatePatientCaches() -> void`
- `InvalidateMedicalCaseCaches() -> void`
- `InvalidateAll() -> void`

实现: DesktopCacheManager (Foundation)

#### ILocalAuthService.cs

本地认证服务 (本地模式使用)。

- `ValidateAsync(username, password, ct) -> Task<UserDetailDto?>`
- `ChangePasswordAsync(userId, oldPassword, newPassword, ct) -> Task<bool>`

实现: LocalAuthService (LocalData)

#### ILoginCoordinator.cs

登录流程协调器，编排认证-会话-模块加载-导航完整流程。

- 属性: CurrentState(AuthState), IsLoggedIn(bool), CurrentUser(UserDetailDto?)
- `LoginAsync(username, password) -> Task<LoginResult>`
- `HandleLoginSuccessAsync(UserDetailDto user, DateTime tokenExpiresAt) -> Task`
- `LogoutAsync() -> Task`
- `GetDiagnostics() -> LoginFlowDiagnostics`
- 事件: StateChanged, LoginSucceeded, LogoutCompleted
- 附属类型:
  - `LoginSuccessEventArgs` -- User(UserDetailDto), TokenExpiresAt(DateTime)
  - `LoginResult` -- record: Success, ErrorMessage?, ErrorCode?, User?, 静态工厂 Succeeded/Failed
  - `LoginFlowDiagnostics` -- record: CurrentState, IsLoggedIn, UserName?, UserRole?, LoginTime?, LastStateChangeTime?, LoginAttemptCount

#### IMedicalCaseCommandService.cs

医案写操作服务 (ADR-1 SRP 分离)。

- 属性: Current(MedicalCaseDetailDto?), HasChanges(bool)
- `SaveAsync() -> Task<bool>`
- `DeleteAsync() -> Task<bool>`
- `CreateMedicalCaseAsync(Guid patientId) -> Task<(bool success, Guid medicalCaseId, string? errorMessage)>`

#### IMedicalCaseLifecycleService.cs

医案生命周期服务 (ADR-1 SRP 分离)。

- 属性: MedicalCaseId(Guid), CurrentConsultation(ConsultationDetailDto?), CurrentPrescription(PrescriptionDetailDto?)
- `InitializeAsync(Guid entityId) -> Task`
- `ReloadAsync() -> Task`
- `SuspendAsync(Guid medicalCaseId) -> Task<(bool success, string? errorMessage)>`
- `CancelMedicalCaseAsync(Guid medicalCaseId, string? reason) -> Task<(bool, string?)>`
- `CompleteMedicalCaseAsync(Guid medicalCaseId) -> Task<(bool, string?)>`
- `ResumeSuspendedAsync(Guid medicalCaseId) -> Task<(bool, string?)>`

#### IMedicalCaseQueryService.cs

医案查询服务 (ADR-1 SRP 分离)，供 Patients 模块跨模块查询。

- `GetPagedAsync(page, pageSize, searchText?) -> Task<PagedResult<MedicalCaseListDto>?>`
- `QueryAsync(MedicalCaseQueryDto) -> Task<PagedResult<MedicalCaseListDto>?>`
- `GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors) -> Task<MedicalCaseDetailDto?>`
- `CloseCaseAsync(Guid medicalCaseId) -> Task<ApiResponse<MedicalCaseDetailDto>>`

#### INavigationCoordinator.cs

统一导航协调器 (ADR-3 + ADR-7)，整合三个独立导航服务。

- 基础导航:
  - `NavigateTo(string viewName, IDictionary<string, object>? parameters) -> void`
  - `NavigateToHome() -> void` / `NavigateToHome(UserRole) -> void`
  - `NavigateBack() -> void`
  - 属性: CanNavigateBack(bool), CurrentView(string?)
- 历史导航:
  - 属性: NavigationHistory(IReadOnlyList<string>)
  - `ClearHistory() -> void`
  - 事件: `NavigationChanged -> EventHandler<NavigationChangedEventArgs>`
- Region 管理:
  - `ShowLoginDialog() -> void`
  - `ClearLoginRegion() -> void`
  - `ClearContentRegion() -> void`
  - `SubscribeToRegionCollection() -> void`
  - `UnsubscribeFromRegionCollection() -> void`
- 附属类型: `NavigationChangedEventArgs` -- FromView(string?), ToView(string), Parameters

#### IPendingQueueManager.cs

待诊队列管理器，解耦 MedicalCase 和 Patients 模块。

- 属性: PendingQueue(ObservableCollection<PendingMedicalCaseDto>)
- `LoadPendingCasesAsync() -> Task`
- `LoadPatientForPendingCaseAsync(Guid patientId) -> Task<PatientDetailDto?>`
- `RemoveFromQueue(Guid patientId) -> void`
- `ClearQueue() -> void`

#### IPrescriptionSettingsService.cs

处方设置服务，重复药材合并策略。

- 属性: DuplicateHerbMergeStrategy(string) -- 值: Max/Min/Sum/Import/Keep
- `CalculateMergedDosage(int currentDosage, int importedDosage) -> int`

实现: PrescriptionSettingsService (Infrastructure)

#### ISessionManager.cs

会话管理器，管理登录状态、用户信息、权限检查。

- 用户属性: CurrentUser(UserDetailDto?), CurrentUserId(Guid?), CurrentUserName(string?)
- 认证属性: IsAuthenticated(bool), IsLoggedIn(bool) -- [COMPAT] IsLoggedIn 是 IsAuthenticated 的别名
- 会话方法:
  - `SetCurrentUser(UserDetailDto, string token) -> void` -- [COMPAT]
  - `SetSession(UserDetailDto, accessToken, refreshToken?) -> void`
  - `SetUserSession(UserDetailDto, string token) -> void` -- [COMPAT] 别名
  - `ClearSession() -> void`
  - `ClearUserSession() -> void` -- [COMPAT] 别名
- 权限方法:
  - `HasPermission(UserRole) -> bool`
  - `HasPermission(string) -> bool`
  - `HasRole(string) -> bool`
  - `IsAdmin() -> bool`
  - `GetCurrentUserRoleDisplay() -> string`
- 事件: SessionExpired, SessionChanged(SessionChangedEventArgs)
- 附属类型: `SessionChangedEventArgs` -- IsLoggedIn(bool), User(UserDetailDto?)

#### IStartupPipeline.cs

启动管道和步骤定义，含大量诊断类型。

- `IStartupStep` 接口:
  - 属性: Name(string), Order(int), IsRequired(bool)
  - `ExecuteAsync(IProgress<string>?, CancellationToken) -> Task<StartupStepResult>`
  - 实现: ApiHealthCheckStartupStep, WarmupStartupStep, ModuleCoordinatorStartupStep, ErrorHandlingStartupStep, CoreServicesStartupStep (均在 Shell)

- `IStartupPipeline` 接口:
  - 属性: State(StartupPipelineState), Steps(IReadOnlyList<IStartupStep>)
  - `RegisterStep(IStartupStep) -> void`
  - `ExecuteAsync(IProgress<string>?, CancellationToken) -> Task<StartupPipelineResult>`
  - `GetDiagnostics() -> StartupPipelineDiagnostics`
  - `Reset() -> void`
  - 事件: StateChanged, StepCompleted

- 附属类型:
  - `StartupPipelineState` -- 枚举: NotStarted, Running, Completed, Failed, Cancelled
  - `StartupStepResult` -- record: Success, ErrorMessage?, Exception?, Duration, Skipped, 静态工厂 Succeeded/Failed/SkippedResult
  - `StartupPipelineResult` -- record: Success, TotalDuration, StepResults, FailedStepName?, ErrorMessage?, 静态工厂 Succeeded/Failed
  - `StartupPipelineStateChangedEventArgs` -- PreviousState, CurrentState, CurrentStepName?
  - `StartupStepCompletedEventArgs` -- StepName, StepOrder, Result, CompletedCount, TotalCount
  - `StartupPipelineDiagnostics` -- record: CurrentState, TotalSteps, CompletedSteps, FailedSteps, TotalDuration?, StepDiagnostics
  - `StartupStepDiagnostics` -- record: Name, Order, IsRequired, Executed, Success, Duration?, ErrorMessage?

#### ISyncService.cs

数据同步协调服务，管理本地与服务器数据同步。

- `GetSupportedEntityTypesAsync(ct) -> Task<IReadOnlyList<string>>`
- `CheckDifferencesAsync(string entityType, ct) -> Task<SyncCheckResult>`
- `UploadAsync(entityType, List<Guid>, ct) -> Task<SyncUploadResultDto>`
- `DownloadAsync(entityType, List<Guid>, ct) -> Task<SyncDownloadResultDto>`
- `DeleteAsync(entityType, List<Guid>, ct) -> Task<SyncDeleteResultDto>`
- `ExecuteSyncAsync(entityType, SyncResolution, ct) -> Task<SyncExecutionResult>`
- 附属类型:
  - `SyncCheckResult` -- EntityType, LocalOnly, ServerOnly, Conflicts, HasDifferences, TotalDifferences
  - `SyncResolution` -- ToUpload, ToDownload, ConflictResolutions(Dict<Guid,bool>), Skipped
  - `SyncExecutionResult` -- EntityType, UploadedCount, DownloadedCount, SkippedCount, FailedCount, Errors, IsSuccess

实现: SyncService (LocalData)

#### IUserActivityState.cs

用户活动状态只读查询接口 (AUTH-002)，供 Foundation 层使用避免循环依赖。

- 属性: IsUserActive(bool)
- `ResetActivity() -> void`

#### IUserActivityTracker.cs

用户活动追踪完整接口 (AUTH-001/002/003)，读写操作。

- 属性: LastActivityTime(DateTime), IsUserActive(bool), TimeUntilInactive(TimeSpan), IsTracking(bool)
- `StartTracking() -> void`
- `StopTracking() -> void`
- `ResetActivity() -> void`
- 事件: SessionExpired

#### IUserNotificationService.cs

用户通知服务 (原 IErrorHandlingService 重命名)。

- `HandleExceptionAsync(Exception, context?) -> Task`
- `ShowErrorAsync(message, title?) -> Task`
- `ShowSuccessAsync(message, title?) -> Task`
- `ShowWarningAsync(message, title?) -> Task`
- `ShowInfoAsync(message, title?) -> Task`
- `ShowConfirmAsync(message, title?) -> Task<bool>`

#### IViewModelServices.cs

ViewModel 服务聚合接口，简化构造函数参数 (7 -> 1)。

- 属性: LoggerFactory(ILoggerFactory), EventAggregator(IEventAggregator), RegionManager(IRegionManager), SessionManager(ISessionManager), UserNotificationService(IUserNotificationService), CommonDialogService(ICommonDialogService), RoleRegistry(IRoleRegistry)

#### UnfinishedCaseChoice.cs

未完成医案对话框选择枚举。

- Continue -- 继续看诊
- CloseAndCreate -- 关闭并新建
- CloseOnly -- 仅关闭
- Cancel -- 取消操作

---

### 死代码分析补充

| 接口 | 实现数 | 状态 |
|------|--------|------|
| ICommandHandler (Components/) | 0 | [已清理] 2026-03-01，文件及 Components 目录已删除 |
| IReadOnlyCommandHandler | 0 | [已清理] 2026-03-01，从 ICommandHandlerBase.cs 中移除 |
| ICustomDialogAware | 0 | [已清理] 2026-03-01，文件已删除 |
| IAsyncInitializable | 0 (类型检查引用) | MasterDetailViewModelBase/ControlBase 做类型检查，无 class 实现此接口 |

## 模块演进记录

### 目录结构

```
LYBT.Desktop.Contracts/
+-- Api/                          # Refit API 客户端接口 (7个)
|   +-- IAuthApi.cs               # 认证: 登录/登出/Token刷新/自动登录/健康检查
|   +-- IFormulaApi.cs            # 验方 CRUD + 批量操作 + 导入导出
|   +-- IHerbApi.cs               # 药材 CRUD + 批量操作 + 导入导出
|   +-- IMedicalCaseApi.cs        # 医案 CRUD + 生命周期 + 批量操作 + 打印
|   +-- IPatientApi.cs            # 患者 CRUD + 批量操作 + 导入导出
|   +-- ISyncApi.cs               # 数据同步: 元数据/比对/上传/下载/删除
|   +-- IUserApi.cs               # 用户 CRUD + 批量操作 + 密码管理
+-- CommandHandlers/              # CommandHandler 统一模式 (Phase 1.4)
|   +-- CommandResult.cs          # 统一返回类型 (含隐式 bool 转换)
|   +-- ICommandHandlerBase.cs    # 泛型 CRUD + 只读接口
|   +-- QueryParams.cs            # 统一查询参数 (分页/搜索/排序/过滤)
+-- DataSources/                  # 数据源抽象层 (支持双模式)
|   +-- IDataSourceBase.cs        # 通用 CRUD 基础接口
|   +-- IFormulaDataSource.cs     # 验方数据源 (含克隆/验证/分类)
|   +-- IHerbDataSource.cs        # 药材数据源 (含分类/引用检查)
|   +-- IMedicalCaseDataSource.cs # 医案数据源 (含聚合保存/生命周期)
|   +-- IPatientDataSource.cs     # 患者数据源 (含搜索/引用检查)
|   +-- IUserDataSource.cs        # 用户数据源 (含密码/状态管理)
+-- Events/                       # 事件定义
|   +-- CacheEvents.cs            # 缓存失效事件 (PubSubEvent + CacheDomain)
+-- Models/                       # 契约模型
|   +-- ImportValidationResult.cs # 导入验证结果
+-- Roles/                        # 角色体系
|   +-- IRoleDefinition.cs        # 角色定义接口 (模块/导航)
|   +-- IRoleRegistry.cs          # 角色注册表接口
+-- Security/                     # 安全认证
|   +-- AuthState.cs              # 认证状态枚举 + 认证事件 + 状态变更参数
|   +-- IAuthenticationStateMachine.cs  # 状态机接口 (Fire/CanFire/Reset)
+-- Services/                     # 服务接口
    +-- CrossModule/              # 跨模块搜索接口
    |   +-- IFormulaSearchProvider.cs  # 验方搜索 (解耦 MedicalCase -> Formula)
    |   +-- IHerbSearchProvider.cs     # 药材搜索 (解耦 MedicalCase/Formula -> Herbs)
    +-- IActiveConsultationService.cs  # 活跃医案追踪 (离开确认)
    +-- IApplicationTickService.cs     # 统一 1 秒定时调度
    +-- IAsyncInitializable.cs         # 异步初始化接口
    +-- ICommonDialogService.cs        # 通用对话框 (Info/Warn/Error/Confirm/Input/File)
    +-- ICurrentUserProvider.cs        # 当前用户 ID (本地模式审计)
    +-- IDesktopCacheManager.cs        # 缓存失效管理器
    +-- ILocalAuthService.cs           # 本地认证 (Validate/ChangePassword)
    +-- ILoginCoordinator.cs           # 登录流程协调 + LoginResult + LoginFlowDiagnostics
    +-- IMedicalCaseCommandService.cs  # 医案写操作 (Save/Delete/Create)
    +-- IMedicalCaseLifecycleService.cs # 医案生命周期 (Suspend/Cancel/Complete/Resume)
    +-- IMedicalCaseQueryService.cs    # 医案查询 (分页/统一查询/未完成医案)
    +-- INavigationCoordinator.cs      # 统一导航 + NavigationChangedEventArgs
    +-- IPendingQueueManager.cs        # 待诊队列管理
    +-- IPrescriptionSettingsService.cs # 处方设置 (重复药材合并策略)
    +-- ISessionManager.cs            # 会话管理 + SessionChangedEventArgs
    +-- IStartupPipeline.cs           # 启动管道 + 步骤 + 诊断
    +-- ISyncService.cs               # 数据同步协调 + SyncCheckResult + SyncResolution
    +-- IUserActivityState.cs          # 用户活动状态查询 (只读)
    +-- IUserActivityTracker.cs        # 用户活动追踪 (读写)
    +-- IUserNotificationService.cs    # 用户通知 (HandleException + 消息提示)
    +-- IViewModelServices.cs          # ViewModel 服务聚合 (7 -> 1)
    +-- UnfinishedCaseChoice.cs        # 未完成医案四选项枚举
```

### 重大演进

1. **认证架构重构** (refactor-auth-role-system): 双状态机 -> 统一 AuthState; LoginFlowState -> AuthState; IRoleDefinition 策略模式替代 switch-case
2. **MedicalCase API 精简** (simplify-medicalcase-api + consolidate-medicalcase-queries): 从 20+ 个端点精简到 ~15 个核心端点，删除 Ghost API、重复查询、独立 Prescription CRUD
3. **MedicalCase 服务 SRP** (refactor-frontend-srp-patterns): 单体服务 -> Query + Command + Lifecycle 三接口
4. **导航统一** (unify-navigation-architecture): 三个独立导航服务合并为 INavigationCoordinator
5. **DataSource 抽象层引入**: 为 SYNC-D02 双模式 (远程/本地) 准备的数据访问抽象
6. **批量操作标准化** (optimize-batch-operations Phase 2): 所有模块统一支持 BatchDelete/BatchEnable/BatchDisable
7. **Token 过期机制重构** (refactor-token-sliding-expiration): 引入 IApplicationTickService + IUserActivityTracker + IUserActivityState 三层架构
8. **登录流程简化** (simplify-login-options + simplify-auth-architecture): 移除多余参数和事件，AutoLoginToken 替代密码存储
