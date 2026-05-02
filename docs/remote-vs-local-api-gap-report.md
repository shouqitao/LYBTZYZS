# 远程API vs 本地API 差异分析报告

> 生成日期: 2026-05-02
> 范围: Desktop客户端 Refit接口 + Server/LocalWebAPI Controllers

---

## 1. 总体统计

| 维度 | 远程 (Server) | 本地 (LocalWebAPI) | 差异 |
|------|--------------|-------------------|------|
| Controller数量 | 14 | 10 | -4 (Sync/Configuration/Diagnostics/Health合并或省略) |
| Controller端点数 | 97 | ~60 | -37 |
| Refit接口数量 | 8 (含ISyncApi) | 7 (无ILocalSyncApi) | -1 |
| Refit方法总数 | 90 | 79 | -11 |

---

## 2. 返回类型差异 (关键)

### 远程接口: 统一使用 `ApiResponse<T>` 包装

```csharp
// 远程 - 所有返回值都包装在ApiResponse中
Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(...);
Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id);
Task<ApiResponse> DeleteUserAsync(Guid id);
```

### 本地接口: 直接返回DTO，无包装

```csharp
// 本地 - 直接返回DTO
Task<List<UserListDto>> GetUsersAsync();
Task<UserDetailDto> GetUserByIdAsync(Guid id);
Task DeleteUserAsync(Guid id);
```

**影响**: Repository层在处理返回值时需要不同的解包逻辑。远程需要 `.Data` 访问实际数据，本地直接使用。

---

## 3. 分页查询差异

### 远程: 返回 `PagedResult<T>` (包含分页元数据)

```csharp
Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
    [Refit.Query] int page = 1,
    [Refit.Query] int pageSize = 20,
    [Refit.Query] string? keyword = null);
```

### 本地: 返回 `List<T>` (无分页，返回全量数据)

```csharp
Task<List<UserListDto>> GetUsersAsync();
```

**影响**: 本地模式下所有数据一次性加载，小数据量可接受，大数据量可能有性能问题。

---

## 4. 批量操作参数差异

### 远程: 使用 `BatchDeleteInputDto` 包装

```csharp
Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(
    [Refit.Body] BatchDeleteInputDto request);  // 包含 Ids 属性
```

### 本地: 直接使用 `List<Guid>`

```csharp
Task<BatchOperationResultDto> BatchDeleteAsync(
    [Refit.Body] List<Guid> ids);  // 直接传ID列表
```

**影响**: Repository层需要适配不同的参数类型。

---

## 5. 逐模块详细对比

### 5.1 Auth模块

| 方法 | 远程 IAuthApi | 本地 ILocalAuthApi | 差异 |
|------|-------------|-------------------|------|
| Login | `LoginAsync(LoginRequest)` → `ApiResponse<LoginResponse>` | `LoginAsync(LoginRequest)` → `object` | 返回类型不同 |
| AutoLogin | `LoginWithAutoTokenAsync(AutoLoginRequest)` → `ApiResponse<LoginResponse>` | `AutoLoginAsync(object)` → `object` | 方法名+参数类型+返回类型均不同 |
| Logout | `LogoutAsync(LogoutRequest)` → `ApiResponse` | `LogoutAsync()` → `void` | 参数+返回类型不同 |
| Refresh | `RefreshTokenAsync(RefreshTokenRequest)` → `ApiResponse<LoginResponse>` | `RefreshAsync()` → `object` | 方法名+参数+返回类型不同 |
| ValidateToken | `ValidateTokenFromHeaderAsync()` + `ValidateTokenAsync(ValidateTokenRequest)` | `ValidateTokenAsync()` → `object` | 远程有2个验证方法，本地1个 |
| HealthCheck | `HealthCheckAsync()` → `ApiResponse<HealthCheckResponse>` | 无 | **本地缺失** |

**总结**: Auth模块差异最大，方法名、参数、返回类型全面不一致。

### 5.2 Users模块

| 方法 | 远程 IUserApi | 本地 ILocalUserApi | 差异 |
|------|-------------|-------------------|------|
| GetUsers | `GetUsersAsync(page, pageSize, keyword)` → `ApiResponse<PagedResult<UserListDto>>` | `GetUsersAsync()` → `List<UserListDto>` | 无分页参数，无ApiResponse包装 |
| GetById | `GetUserByIdAsync(Guid)` → `ApiResponse<UserDetailDto>` | `GetUserByIdAsync(Guid)` → `UserDetailDto` | 仅返回类型 |
| Create | `CreateUserAsync(UserInputDto)` → `ApiResponse<UserDetailDto>` | `CreateUserAsync(UserInputDto)` → `UserDetailDto` | 仅返回类型 |
| Update | `UpdateUserAsync(Guid, UserInputDto)` → `ApiResponse<UserDetailDto>` | `UpdateUserAsync(Guid, UserInputDto)` → `UserDetailDto` | 仅返回类型 |
| Delete | `DeleteUserAsync(Guid)` → `ApiResponse` | `DeleteUserAsync(Guid)` → `void` | 仅返回类型 |
| ChangePassword | `ChangePasswordAsync(Guid, ChangePasswordRequest)` → `ApiResponse` | `ChangePasswordAsync(Guid, ChangePasswordRequest)` → `void` | 仅返回类型 |
| ResetPassword | `ResetPasswordAsync(Guid, ResetPasswordRequestDto)` → `ApiResponse<ResetPasswordResponseDto>` | `ResetPasswordAsync(Guid)` → `ResetPasswordResponseDto` | **本地缺少request参数** |
| ChangeProfile | `ChangeProfileAsync(Guid, ChangeProfileDto)` → `ApiResponse<UserDetailDto>` | `ChangeProfileAsync(Guid, ChangeProfileDto)` → `UserDetailDto` | 仅返回类型 |
| ToggleStatus | `ToggleStatusAsync(Guid)` → `ApiResponse<UserDetailDto>` | `ToggleStatusAsync(Guid)` → `UserDetailDto` | 仅返回类型 |
| Restore | `RestoreAsync(Guid)` → `ApiResponse<UserDetailDto>` | `RestoreAsync(Guid)` → `UserDetailDto` | 仅返回类型 |
| BatchDelete | `BatchDeleteAsync(BatchDeleteInputDto)` → `ApiResponse<BatchOperationResultDto>` | `BatchDeleteAsync(List<Guid>)` → `BatchOperationResultDto` | **参数类型不同** |
| BatchEnable | `BatchEnableAsync(BatchDeleteInputDto)` → `ApiResponse<BatchOperationResultDto>` | `BatchEnableAsync(List<Guid>)` → `BatchOperationResultDto` | **参数类型不同** |
| BatchDisable | `BatchDisableAsync(BatchDeleteInputDto)` → `ApiResponse<BatchOperationResultDto>` | `BatchDisableAsync(List<Guid>)` → `BatchOperationResultDto` | **参数类型不同** |
| BatchImport | `BatchImportAsync(UserBatchImportInputDto)` → `ApiResponse<UserBatchImportResultDto>` | 无 | **本地缺失** |
| GetCurrentUser | 无 | `GetCurrentUserAsync()` → `UserDetailDto` | **远程缺失** (远程通过Auth实现) |

### 5.3 Patients模块

| 方法 | 远程 IPatientApi | 本地 ILocalPatientApi | 差异 |
|------|----------------|---------------------|------|
| GetPatients | `GetPatientsAsync(page, pageSize, keyword)` → `ApiResponse<PagedResult<PatientListDto>>` | `GetPatientsAsync(keyword, page, pageSize)` → `List<PatientListDto>` | 返回类型+参数顺序 |
| GetById | ✓ | ✓ | 仅返回类型 |
| Create | ✓ | ✓ | 仅返回类型 |
| Update | ✓ | ✓ | 仅返回类型 |
| Delete | ✓ | ✓ | 仅返回类型 |
| ToggleStatus | 无 | `ToggleStatusAsync(Guid)` → `PatientDetailDto` | **远程缺失** |
| Restore | ✓ | ✓ | 仅返回类型 |
| BatchDelete | `BatchDeleteAsync(BatchDeleteInputDto)` | `BatchDeleteAsync(List<Guid>)` | **参数类型不同** |
| BatchImport | `BatchImportAsync(PatientBatchImportInputDto)` | 无 | **本地缺失** |
| ExportTemplate | `ExportTemplateAsync()` → `HttpResponseMessage` | `ExportTemplateAsync()` → `object` | 返回类型不同 |
| Export | `ExportPatientsAsync(keyword)` → `HttpResponseMessage` | `ExportPatientsAsync(keyword)` → `List<PatientDetailDto>` | **返回类型完全不同** |

### 5.4 Herbs模块

| 方法 | 远程 IHerbApi | 本地 ILocalHerbApi | 差异 |
|------|-------------|-------------------|------|
| GetHerbs | `GetHerbsAsync(page, pageSize, keyword, category)` → `ApiResponse<PagedResult<HerbListDto>>` | `GetHerbsAsync(keyword)` → `List<HerbListDto>` | **无分页+无category过滤** |
| GetById | ✓ | ✓ | 仅返回类型 |
| Create | ✓ | ✓ | 仅返回类型 |
| Update | ✓ | ✓ | 仅返回类型 |
| Delete | ✓ | ✓ | 仅返回类型 |
| ToggleStatus | ✓ | ✓ | 仅返回类型 |
| Restore | ✓ | ✓ | 仅返回类型 |
| BatchDelete | `BatchDeleteAsync(BatchDeleteInputDto)` | `BatchDeleteAsync(List<Guid>)` | **参数类型不同** |
| BatchEnable | `BatchEnableAsync(BatchDeleteInputDto)` | `BatchEnableAsync(List<Guid>)` | **参数类型不同** |
| BatchDisable | `BatchDisableAsync(BatchDeleteInputDto)` | `BatchDisableAsync(List<Guid>)` | **参数类型不同** |
| BatchImport | `BatchImportAsync(StreamPart)` [Multipart] | 无 | **本地缺失 (远程用Multipart上传文件)** |
| Export | `ExportHerbsAsync(keyword)` → `HttpResponseMessage` | `ExportHerbsAsync(keyword)` → `List<HerbDetailDto>` | **返回类型不同** |
| ExportTemplate | `ExportTemplateAsync()` → `HttpResponseMessage` | `ExportTemplateAsync()` → `object` | 返回类型不同 |

### 5.5 Formulas模块

| 方法 | 远程 IFormulaApi | 本地 ILocalFormulaApi | 差异 |
|------|----------------|---------------------|------|
| GetFormulas | `GetFormulasAsync(page, pageSize, keyword, category)` → `ApiResponse<PagedResult<FormulaListDto>>` | `GetFormulasAsync(keyword)` → `List<FormulaListDto>` | **无分页+无category过滤** |
| GetById | ✓ | ✓ | 仅返回类型 |
| Create | ✓ | ✓ | 仅返回类型 |
| Update | ✓ | ✓ | 仅返回类型 |
| Delete | ✓ | ✓ | 仅返回类型 |
| Clone | ✓ | ✓ | 仅返回类型 |
| ToggleStatus | ✓ | ✓ | 仅返回类型 |
| Restore | ✓ | ✓ | 仅返回类型 |
| BatchDelete | `BatchDeleteAsync(BatchDeleteInputDto)` | `BatchDeleteAsync(List<Guid>)` | **参数类型不同** |
| BatchEnable | `BatchEnableAsync(BatchDeleteInputDto)` | `BatchEnableAsync(List<Guid>)` | **参数类型不同** |
| BatchDisable | `BatchDisableAsync(BatchDeleteInputDto)` | `BatchDisableAsync(List<Guid>)` | **参数类型不同** |
| BatchImport | `BatchImportAsync(FormulaBatchImportInputDto)` | 无 | **本地缺失** |
| Export | `ExportFormulasAsync(category)` → `HttpResponseMessage` | `ExportFormulasAsync(category)` → `List<FormulaDetailDto>` | **返回类型不同** |
| ExportTemplate | `ExportTemplateAsync()` → `HttpResponseMessage` | `ExportTemplateAsync()` → `object` | 返回类型不同 |

### 5.6 MedicalCases模块

| 方法 | 远程 IMedicalCaseApi | 本地 ILocalMedicalCaseApi | 差异 |
|------|---------------------|-------------------------|------|
| GetMedicalCases | `GetMedicalCasesAsync(page, pageSize, keyword, includeAllDoctors)` → `ApiResponse<PagedResult<MedicalCaseListDto>>` | `GetMedicalCasesAsync(patientId)` → `List<MedicalCaseListDto>` | **参数完全不同，无分页** |
| GetById | ✓ | ✓ | 仅返回类型 |
| Create | ✓ | ✓ | 仅返回类型 |
| Delete | ✓ | ✓ | 仅返回类型 |
| Save | ✓ | ✓ | 仅返回类型 |
| Query | `QueryMedicalCasesAsync(8个参数)` → `ApiResponse<PagedResult<MedicalCaseListDto>>` | `QueryAsync(8个参数)` → `PagedResult<MedicalCaseListDto>` | **方法名不同，本地保留了PagedResult** |
| Search | `SearchMedicalCasesAsync(6个参数)` → `ApiResponse<PagedResult<MedicalCaseDetailDto>>` | `SearchAsync(6个参数)` → `PagedResult<MedicalCaseDetailDto>` | **方法名不同，本地保留了PagedResult** |
| BatchDetails | `GetBatchDetailsAsync(BatchDetailQueryDto)` → `ApiResponse<List<MedicalCaseDetailDto>>` | `GetBatchDetailsAsync(List<Guid>)` → `List<MedicalCaseDetailDto>` | **参数类型不同** |
| BatchDelete | `BatchDeleteAsync(BatchDeleteInputDto)` | `BatchDeleteAsync(List<Guid>)` | **参数类型不同** |
| Permissions | ✓ | ✓ | 仅返回类型 |
| CloseCase | ✓ | ✓ | 仅返回类型 |
| Suspend | `SuspendAsync(Guid, ConsultationInputDto?)` | `SuspendCaseAsync(Guid)` | **方法名不同，本地无request参数** |
| Cancel | `CancelMedicalCaseAsync(Guid, CancelMedicalCaseRequestDto?)` → `IApiResponse` | `CancelCaseAsync(Guid)` → `void` | **方法名不同，本地无request参数** |
| UpdateStatus | ✓ | ✓ | 仅返回类型 |
| SetPrescriptionFlag | ✓ | ✓ | 仅返回类型 |
| RecordPrintCompleted | ✓ | ✓ | 仅返回类型 |
| GetPendingCases | `GetPendingCasesAsync(patientId)` → `ApiResponse<List<PendingMedicalCaseDto>>` | 无 | **本地缺失** |
| GetAuditLogs | `GetAuditLogsAsync(id, page, pageSize)` → `ApiResponse<MedicalCaseAuditLogPagedResultDto>` | 无 | **本地缺失** |
| AddPrintLog | `AddPrintLogAsync(Guid, PrintLogInputDto)` → `ApiResponse<object>` | 无 | **本地缺失** |

### 5.7 Registrations模块

| 方法 | 远程 IRegistrationApi | 本地 ILocalRegistrationApi | 差异 |
|------|---------------------|--------------------------|------|
| GetRegistrations | `GetListAsync(page, pageSize, keyword, startDate, endDate, patientId, doctorId)` → `ApiResponse<PagedResult<RegistrationListDto>>` | `GetRegistrationsAsync(date)` → `List<RegistrationListDto>` | **参数完全不同，无分页** |
| GetById | ✓ | ✓ | 仅返回类型 |
| Create | ✓ | ✓ | 仅返回类型 |
| GetQueue | ✓ | ✓ | 仅返回类型 |
| StartVisit | ✓ | ✓ | 仅返回类型 |
| Cancel | ✓ | ✓ | 仅返回类型 |
| Delete | 无 | `DeleteRegistrationAsync(Guid)` → `void` | **远程缺失** (远程无删除) |

### 5.8 Sync模块

| 方法 | 远程 ISyncApi | 本地 | 差异 |
|------|-------------|------|------|
| GetEntityTypes | ✓ | 无 | **本地无需同步** |
| GetMetadata | ✓ | 无 | **本地无需同步** |
| Compare | ✓ | 无 | **本地无需同步** |
| Upload | ✓ | 无 | **本地无需同步** |
| Download | ✓ | 无 | **本地无需同步** |
| Delete | ✓ | 无 | **本地无需同步** |

---

## 6. Controller端点覆盖差异

### 6.1 远程有、本地无的端点

| 模块 | 端点 | 说明 |
|------|------|------|
| Sync | 全部6个端点 | 同步是服务器端操作，本地不需要 |
| Diagnostics | logging/status, logging/debug/enable, logging/debug/disable, logging/level | 远程有日志管理，本地只有db-info/version/logs |
| Configuration | GET /, POST validate | 远程有完整配置管理，本地只有简单的key-value |
| Patients | check-reference, batch-check-reference | 引用检查（删除前确认） |
| Herbs | export-all, check-reference, batch-check-reference, categories | 导出全量、引用检查、分类列表 |
| Formulas | pending-validation, validate, categories | 待验证列表、验方验证、分类列表 |
| MedicalCases | consultations, prescriptions (子资源) | 诊断和处方独立查询 |
| Registrations | quick-visit | 快速就诊 |

### 6.2 本地有、远程无的端点

| 模块 | 端点 | 说明 |
|------|------|------|
| Patients | toggle-status | 本地有切换状态，远程也有但不在Refit接口 |
| Registrations | DELETE /{id} | 本地支持删除挂号，远程不支持 |
| MedicalCases | by-status/{status} | 本地有按状态查询 |

### 6.3 MedicalCases控制器架构差异

**远程**: 拆分为4个Controller
- `MedicalCasesController` - CRUD + 查询
- `MedicalCaseProcessingController` - 状态变更 (close/suspend/cancel/status)
- `MedicalCasePrintController` - 打印相关
- `MedicalCaseAuditController` - 权限 + 审计日志

**本地**: 合并为1个Controller
- `MedicalCasesController` - 所有功能合并

---

## 7. 关键差异汇总

### 7.1 系统性差异 (影响所有模块)

| 差异类型 | 远程 | 本地 | 严重程度 |
|---------|------|------|---------|
| 返回类型包装 | `ApiResponse<T>` | 直接 `T` | 高 - 影响所有Repository |
| 分页查询 | `PagedResult<T>` + 分页参数 | `List<T>` 无分页 | 中 - 小数据量可接受 |
| 批量操作参数 | `BatchDeleteInputDto` | `List<Guid>` | 中 - 需要适配 |
| 导出返回 | `HttpResponseMessage` (文件流) | `List<T>` (JSON数据) | 中 - 本地返回JSON而非文件 |
| 导入方式 | Multipart文件上传 | JSON body | 中 - 本地无文件上传 |

### 7.2 方法级差异 (需要特别关注)

| 模块 | 方法 | 差异详情 |
|------|------|---------|
| Auth | AutoLogin | 方法名不同 + 参数类型不同 |
| Auth | Logout | 本地无参数，远程需要LogoutRequest |
| Auth | Refresh | 方法名不同 + 本地无参数 |
| Users | ResetPassword | **本地缺少request参数** |
| Herbs | GetHerbs | 本地缺少category过滤参数 |
| MedicalCases | Suspend | 方法名不同 + 本地无request参数 |
| MedicalCases | Cancel | 方法名不同 + 本地无request参数 |
| MedicalCases | GetMedicalCases | 参数完全不同 |
| Registrations | GetRegistrations | 参数完全不同 |

### 7.3 缺失方法统计

| 类型 | 数量 | 列表 |
|------|------|------|
| 远程有、本地无 (Refit) | 11 | HealthCheck, BatchImport(×4), GetPendingCases, GetAuditLogs, AddPrintLog, Sync(×6) |
| 本地有、远程无 (Refit) | 2 | GetCurrentUser, DeleteRegistration |
| 远程有、本地无 (Controller) | ~15 | check-reference, categories, consultations, prescriptions等 |
| 本地有、远程无 (Controller) | 3 | toggle-status(Patients), DELETE(Registrations), by-status(MedicalCases) |

---

## 8. 建议

### 8.1 高优先级 (影响功能正确性)

1. **Users.ResetPasswordAsync**: 本地接口缺少 `ResetPasswordRequestDto` 参数，需要补充
2. **Auth模块方法对齐**: AutoLogin/Logout/Refresh 的方法名和参数差异较大，Repository层需要仔细适配

### 8.2 中优先级 (影响代码质量)

1. **批量操作参数统一**: 考虑让本地接口也使用 `BatchDeleteInputDto`，减少Repository适配代码
2. **导出功能对齐**: 本地返回JSON列表 vs 远程返回文件流，需要在Repository层处理差异

### 8.3 低优先级 (功能增强)

1. **分页支持**: 本地目前返回全量数据，数据量大时可能需要添加分页
2. **引用检查**: check-reference/batch-check-reference 端点可防止误删
3. **分类过滤**: Herbs/Formulas 的 categories 端点和 category 过滤参数
