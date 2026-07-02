# LYBTZYZS Full Solution Inventory — Every Project, Class, Method, Field

> Generated 2026-06-29 via Serena full-solution scan  
> 40 projects | ~600+ types | Method/field-level precision

---

## Solution Statistics

| Layer | Projects | Types | Key Pattern |
|-------|----------|-------|-------------|
| Server Core | 2 | ~25 | Entities + Infrastructure |
| Server Modules | 8 | ~80 | Service/Repository per domain |
| Server Services | 1 | ~15 | WebAPI controllers |
| Desktop Core | 9 | ~330 | Contracts/Foundation/Infrastructure |
| Desktop Modules | 8 | ~100 | MVVM per domain |
| Desktop Roles | 4 | ~25 | Role workspaces |
| Desktop Shell | 1 | ~40 | Prism bootstrapping |
| Desktop LocalWebAPI | 1 | ~25 | Embedded ASP.NET Core |
| Shared | 8 | ~120 | Primitives/Models/Config |
| Tools | 4 | ~4 | Utility programs |
| Tests | 3 | ~50 | Integration/Unit/Architecture |
| **Total** | **40** | **~814** | |

---

## 1. Server Core (2 projects)

### 1.1 LYBT.Entities

**Path**: `src/Server/Core/LYBT.Entities/`

| Namespace | File | Type | Members |
|-----------|------|------|---------|
| Common | BaseEntity.cs | `abstract class BaseEntity : IAuditableEntity, ISoftDeletable` | `Id` Guid, `CreatedAt` DateTime, `UpdatedAt` DateTime?, `CreatedBy` Guid?, `UpdatedBy` Guid?, `RowVersion` byte[]?, `IsDeleted` bool |
| Common | IAuditableEntity.cs | `interface IAuditableEntity` | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| Common | ISoftDeletable.cs | `interface ISoftDeletable` | `IsDeleted` bool |
| Common | SystemLog.cs | `class SystemLog` | `Id` int, `Timestamp`, `Level` string, `Message`, `Exception`, `LoggerName`, `UserId`, `RequestId`, `CorrelationId`, `MachineName`, `ThreadId`, `Properties` |
| Patients | PatientModel.cs | `class Patient : BaseEntity` | `Name` string, `PinYinCode` string?, `Gender` Gender, `BirthDate` DateTime?, `IdNumber` string? `[SensitiveData]`, `PhoneNumber` string? `[SensitiveData]`, `Status` CommonStatus, `Age` int? `[NotMapped]` |
| MedicalCases | MedicalCaseModel.cs | `class MedicalCase : BaseEntity` | `PatientId` Guid, `PatientName` string, `UserId` Guid, `DoctorName` string, `CaseNumber` string?, `CaseStatus` MedicalCaseStatus, `NeedsPrescription` bool?, `CompletedAt` DateTime?, `Consultation` Consultation?, `Prescription` Prescription?, `IsLocked` bool, `IsActive` bool, `IsCompleted` bool + methods: `Complete()`, `Suspend()`, `SoftDelete()`, `UpdateConsultation(...)` |
| Consultations | ConsultationModel.cs | `class Consultation : BaseEntity` | `PresentIllness` string?, `TongueDiagnosis` string?, `PulseDiagnosis` string?, `TcmDiagnosis` string? |
| Prescriptions | PrescriptionModel.cs | `class Prescription : BaseEntity` | `MedicalCaseId` Guid, `PrescriptionNumber` string?, `DosageCount` int, `Discount` decimal, `Usage` string?, `Advice` string?, `ReferencedFormulas` string?, `Remark` string?, `Items` ICollection\<PrescriptionItem\> |
| Prescriptions | PrescriptionItem.cs | `class PrescriptionItem` | `Id` Guid, `PrescriptionId` Guid, `HerbId` Guid, `HerbName` string, `Dosage` int, `Unit` string, `DecocteMethod` DecocteMethod, `UnitPrice` decimal, `Amount` decimal, `Usage` string?, `Remark` string? |
| Herbs | HerbModel.cs | `class Herb : BaseEntity` | `Name` string, `PinYinCode` string?, `Category` string?, `Properties` string?, `Origin` string?, `Spec` string?, `Unit` string, `Price` decimal, `CostPrice` decimal?, `Effect` string?, `Usage` string?, `Remark` string?, `Status` CommonStatus |
| Formulas | FormulaModel.cs | `class Formula : BaseEntity` | `Name` string, `Effect` string?, `Indication` string?, `Usage` string?, `Remark` string?, `Property` string?, `Status` CommonStatus, `IsShared` bool, `ValidationStatus` FormulaValidationStatus, `Category` string?, `FormulaType` FormulaType, `UserId` Guid?, `Herbs` ICollection\<FormulaHerbItem\> |
| Formulas | FormulaHerbItem.cs | `class FormulaHerbItem` | `Id` Guid, `FormulaId` Guid, `Formula` Formula?, `HerbId` Guid?, `OriginalHerbName` string?, `IsValidated` bool, `HerbName` string, `Dosage` int, `Unit` string, `Usage` string?, `Remark` string?, `ProcessingMethod` string?, `DecocteMethod` DecocteMethod |
| Registrations | RegistrationModel.cs | `class Registration : BaseEntity` | `PatientId` Guid, `PatientName` string, `DoctorId` Guid, `DoctorName` string, `MedicalCaseId` Guid?, `Source` RegistrationSource, `Status` RegistrationStatus, `QueueNumber` int, `RegistrationFee` decimal, `Remark` string? |
| Users | ApplicationUser.cs | `class ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDeletable` | `RealName` string, `PinYinCode` string?, `Role` UserRole, `IsSysAdmin` bool, `Status` CommonStatus, `MustChangeOnNextLogin` bool, `LastLoginAt` DateTime?, `Remark` string? + audit fields |
| Users | ApplicationRole.cs | `class ApplicationRole : IdentityRole<Guid>` | `Description` string? |
| Users | UserRole.cs | `enum UserRole` | `Receptionist=0`, `Doctor=1`, `Admin=10`, `SuperAdmin=100` |
| Auth | AuthSessionModel.cs | `class AuthSession` | `Id` Guid, `UserId` Guid, `TokenHash` string, `LoginTime` DateTime, `LogoutTime` DateTime?, `ExpiryTime` DateTime, `IpAddress` string?, `UserAgent` string?, `IsRevoked` bool, `Status` CommonStatus |
| Attributes | SensitiveDataAttribute.cs | `[SensitiveData]` | `Type` SensitiveDataType, `MaskingMode` MaskingMode |

### 1.2 LYBT.Infrastructure

**Path**: `src/Server/Core/LYBT.Infrastructure/`

#### Data

| File | Type | Members |
|------|------|---------|
| AppDbContext.cs | `class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` | DbSets: `AuthSessions`, `Patients`, `MedicalCases`, `Consultations`, `Prescriptions`, `PrescriptionItems`, `Herbs`, `Formulas`, `Registrations`, `SystemLogs` + methods: `OnModelCreating()`, `SaveChangesAsync()`, `SaveChanges()`, `SetAuditFields()`, `GetCurrentUserId()` |
| DatabaseInitializationService.cs | `class DatabaseInitializationService` | `InitializeDatabaseAsync()` |
| EntityOptimizationExtensions.cs | `static class EntityOptimizationExtensions` | `ApplyOptimizations()` — global query filters + indexes |

#### Interfaces

| File | Type | Members |
|------|------|---------|
| IRepository.cs | `interface IRepository<T> where T : class` | 15 methods: `GetByIdAsync`, `GetAllAsync`, `GetPagedAsync`, `FindAsync`, `GetSingleAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `AddRangeAsync`, `CountAsync`, `SaveChangesAsync`, `AddWithoutSaveAsync`, `UpdateWithoutSaveAsync`, `DeleteWithoutSaveAsync`, `SaveAllChangesAsync` |
| IDbContextAccessor.cs | `interface IDbContextAccessor` | `Context` AppDbContext |

#### Repositories

| File | Type | Extra Methods |
|------|------|--------------|
| BaseRepository.cs | `abstract class BaseRepository<TEntity> : IRepository<TEntity>` | `FindAsync` (6-param), `SelectAsync<T>`, `GetPagedAsync` (expression), `ExistsAsync` (predicate+id), `CountAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`, `RestoreAsync`, `HardDeleteAsync`, `GetQueryable`, `GetNoTrackingQueryable`, `FromSqlRawAsync` + virtual: `ApplyKeywordFilter`, `ApplyDefaultOrdering` |

#### Cross-Module

| File | Type | Members |
|------|------|---------|
| CrossModuleService.cs | `class CrossModuleService : IUserCrossModuleService, ICrossModuleAuthService` | User queries + Auth operations |
| IUserCrossModuleService.cs | `interface IUserCrossModuleService` | User lookup methods |
| ICrossModuleAuthService.cs | `interface ICrossModuleAuthService` | Auth verification methods |

#### Web

| File | Type | Members |
|------|------|---------|
| BaseApiController.cs | `abstract class BaseApiController : ControllerBase` | `GetOperator()`, `LogOperation()`, `GetModelErrors()`, `IsValidGuid()`, `GetRequestId()`, `IsModelValid`, `GetValidationErrorMessage()`, `Success()`, `Success<T>()`, `SuccessPaged<T>()`, `Error()`, `NotFound()`, `BusinessFail()`, `ValidationFail()`, `HandleResult<T>()`, `HandleResult()`, `HandleServiceResult<T>()`, `HandlePagedResult<T>()`, `HandleBoolResult()`, `IsAdminOrOwner()`, `ValidateOwnership()`, `ValidateModel()`, `ValidatePagination()`, `GetEntityWithOwnershipCheckAsync<T>()` |

#### DI Extensions

| File | Methods |
|------|---------|
| ServiceCollectionExtensions.cs | `RegisterAllApplicationServices()` — chains: `RegisterInfrastructureServices()`, `RegisterAuthenticationServices()`, `RegisterBusinessModules()`, `RegisterApiServices()`, `RegisterControllerServices()`, `ConfigureRateLimiting()`, `ConfigurePerformanceOptimizations()`, `AddSecurityServices()` |
| RepositoryServiceCollectionExtensions.cs | `AddRepository<TInterface, TImpl>()` |

---

## 2. Server Modules (8 projects)

### 2.1 LYBT.Module.Auth

| File | Type | Members |
|------|------|---------|
| AuthModule.cs | `static class` | `AddAuthModule()` |
| IJwtService.cs | `interface IJwtService` | `GenerateToken()` x2, `ValidateToken()`, `RefreshToken()`, `ValidateAutoLoginToken()` |
| JwtService.cs | `class JwtService : IJwtService` | Impl of above |
| IAuthService.cs | `interface IAuthService` | `LoginAsync()`, `LogoutAsync()`, `VerifyCredentialsAsync()`, `ValidateTokenAsync()`, `GetSessionInfoAsync()`, `LoginWithAutoTokenAsync()`, `ChangePasswordAsync()` |
| AuthService.cs | `class AuthService : IAuthService` | Impl of above |

### 2.2 LYBT.Module.Users

| File | Type | Members |
|------|------|---------|
| UsersModule.cs | `static class` | `AddUsersModule()` |
| IUserService.cs | `interface IUserService` | `GetPagedAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, `ToggleStatusAsync()`, `ResetPasswordAsync()`, `ChangePasswordAsync()`, `ChangeProfileAsync()`, `BatchDeleteAsync()`, `SearchAsync()` |
| UserService.cs | `class UserService : IUserService` | Impl |
| IUserManagerService.cs | `interface IUserManagerService` | User management operations |
| UserCrossModuleService.cs | `class UserCrossModuleService : IUserCrossModuleService` | Cross-module user queries |
| IdentitySeedData.cs | `class IdentitySeedData` | `SeedAsync()` — 4 roles + 2 default users |

### 2.3 LYBT.Module.Patients

| File | Type | Members |
|------|------|---------|
| PatientsModule.cs | `static class` | `AddPatientsModule()` |
| IPatientService.cs | `interface IPatientService` | `GetPagedAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, `SearchAsync()` + region "Entity直接返回方法" |
| PatientService.cs | `class PatientService : IPatientService` | Impl |
| IPatientRepository.cs | `interface IPatientRepository : IRepository<Patient>` | `GetByNameAsync()`, `ExistsAsync()`, `GetByPhoneNumberAsync()`, `GetByIdNumberAsync()`, `GetPagedWithStatusFilterAsync()`, `GetByIdIncludingDeletedAsync()` |
| PatientRepository.cs | `class PatientRepository` | Impl + overrides: `ApplyKeywordFilter` (Name/PinYinCode), `ApplyDefaultOrdering` (Name asc) |

### 2.4 LYBT.Module.Herbs

| File | Type | Members |
|------|------|---------|
| HerbsModule.cs | `static class` | `AddHerbsModule()` |
| IHerbService.cs | `interface IHerbService` | `GetPagedAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, `ToggleStatusAsync()`, `BatchDeleteAsync()`, `BatchImportAsync()`, `CheckReferenceAsync()`, `BatchCheckReferenceAsync()` |
| HerbService.cs | `class HerbService : IHerbService` | Impl |
| IHerbRepository.cs | `interface IHerbRepository : IRepository<Herb>` | `GetByNameAsync()`, `GetByNameOrPinyinAsync()`, `ExistsByNameAsync()`, `GetPagedAsync()` (4-param), `GetByIdIncludingDeletedAsync()` |
| HerbRepository.cs | `class HerbRepository` | Impl + overrides |
| IHerbReferenceRepository.cs | `interface IHerbReferenceRepository` | `GetPrescriptionReferenceCountAsync()`, `GetFormulaReferenceCountAsync()`, `GetRecentPrescriptionReferencesAsync()` |
| HerbReferenceRepository.cs | `class HerbReferenceRepository` | Impl |

### 2.5 LYBT.Module.Formula

| File | Type | Members |
|------|------|---------|
| FormulaModule.cs | `static class` | `AddFormulaModule()` |
| IFormulaService.cs | `interface IFormulaService` | `GetPagedAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, `ToggleStatusAsync()`, `BatchDeleteAsync()`, `GetPendingValidationAsync()`, `ValidateFormulaHerbAsync()` |
| FormulaService.cs | `class FormulaService : IFormulaService` | Impl |
| IFormulaImportExportService.cs | `interface IFormulaImportExportService` | `BatchImportAsync()`, `ExportFormulasAsync()` |
| FormulaImportExportService.cs | `class FormulaImportExportService` | Impl |
| IFormulaRepository.cs | `interface IFormulaRepository : IRepository<Formula>` | `GetTemplatesAsync()`, `GetByIdWithHerbsAsync()`, `FindWithHerbsAsync()`, `GetPagedWithDetailsAsync()` x2, `GetByUserIdAsync()`, `GetAllWithHerbsAsync()`, `GetByIdIncludingDeletedAsync()` |
| FormulaRepository.cs | `class FormulaRepository` | Impl |

### 2.6 LYBT.Module.MedicalCase

| File | Type | Members |
|------|------|---------|
| MedicalCaseModule.cs | `static class` | `AddMedicalCaseModule()` |
| IMedicalCaseFacade.cs | `interface IMedicalCaseFacade` | `SaveAsync()`, `SetPrescriptionFlagAsync()`, `DeleteAsync()`, `BatchDeleteAsync()`, `UpdateStatusAsync()`, `CompleteAsync()`, `SuspendAsync()`, `CancelAsync()` + read operations |
| MedicalCaseFacade.cs | `class MedicalCaseFacade : IMedicalCaseFacade` | Impl (coordinates Command/Query/State services) |
| IMedicalCaseCommandService.cs | `interface IMedicalCaseCommandService` | `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()` |
| MedicalCaseCommandService.cs | `class MedicalCaseCommandService` | Impl |
| IMedicalCaseQueryService.cs | `interface IMedicalCaseQueryService` | `GetByIdAsync()`, `GetListAsync()`, `QueryAsync()`, `SearchAsync()`, `GetPendingCasesAsync()` |
| MedicalCaseQueryService.cs | `class MedicalCaseQueryService` | Impl |
| IMedicalCaseStateService.cs | `interface IMedicalCaseStateService` | `CompleteAsync()`, `SuspendAsync()`, `CancelAsync()`, `UpdateStatusAsync()` |
| MedicalCaseStateService.cs | `class MedicalCaseStateService` | Impl |
| MedicalCaseServiceHelper.cs | `static class MedicalCaseServiceHelper` | Helper methods for MedicalCase operations |
| IMedicalCaseReferenceService.cs | `interface IMedicalCaseReferenceService` | Reference check operations |
| IMedicalCaseRepository.cs | `interface IMedicalCaseRepository : IRepository<MedicalCase>` | `GetByPatientIdAsync()`, `GetByIdWithDetailsAsync()`, `GetByIdWithDetailsFreshAsync()`, `GetPagedWithDetailsAsync()`, `GetPendingCasesAsync()`, `GetAllPendingCasesAsync()`, `QueryAsync()`, `GetUnfinishedCaseByPatientIdAsync()`, `GetBatchWithDetailsAsync()`, `CountByPrefixAsync()`, `CountPrescriptionsByPrefixAsync()` |
| MedicalCaseRepository.cs | `class MedicalCaseRepository` | Impl (601 lines, includes overridden UpdateAsync for Prescription state management) |
| IMedicalCaseReferenceRepository.cs | `interface IMedicalCaseReferenceRepository` | `CountUnfinishedAsync()`, `CountAllAsync()`, `GetRecentAsync()` |
| MedicalCaseReferenceRepository.cs | `class MedicalCaseReferenceRepository` | Impl |

### 2.7 LYBT.Module.Registration

| File | Type | Members |
|------|------|---------|
| RegistrationModule.cs | `static class` | `AddRegistrationModule()` |
| IRegistrationService.cs | `interface IRegistrationService` | `CreateAsync()`, `QuickVisitAsync()`, `GetByIdAsync()`, `GetPagedAsync()`, `GetWaitingQueueAsync()`, `StartVisitAsync()`, `CancelAsync()` |
| RegistrationService.cs | `class RegistrationService : IRegistrationService` | Impl |
| IRegistrationRepository.cs | `interface IRegistrationRepository : IRepository<Registration>` | `GetPagedAsync()` (7-param), `GetWaitingQueueAsync()`, `GetByStatusAsync()`, `HasWaitingRegistrationAsync()`, `GetByMedicalCaseIdAsync()`, `GetWaitingCountByDoctorAsync()`, `GetTodayMaxQueueNumberAsync()` |
| RegistrationRepository.cs | `class RegistrationRepository` | Impl |

### 2.8 LYBT.Module.Reports

| File | Type | Members |
|------|------|---------|
| ReportsModule.cs | `static class` | `AddReportsModule()` |
| IReportService.cs | `interface IReportService` | `GetDailyIncomeAsync()`, `GetDailyConsultationsAsync()`, `GetDailyHerbUsageAsync()` |
| ReportService.cs | `class ReportService : IReportService` | Impl |
| IReportRepository.cs | `interface IReportRepository` | `GetTodayRegistrationFeeTotalAsync()`, `GetTodayMedicineFeeTotalAsync()`, `GetTodayConsultationCountAsync()`, `GetTodayConsultationsByDoctorAsync()`, `GetTodayHerbUsageAsync()` |
| ReportRepository.cs | `class ReportRepository` | Impl |

---

## 3. Server Services (1 project)

### 3.1 LYBT.WebAPI

**Path**: `src/Server/Services/LYBT.WebAPI/`

#### Controllers (12)

| Controller | Route | Policy | Actions |
|------------|-------|--------|---------|
| AuthController | `api/v1/auth` | AllowAnonymous | `Login`, `Logout`, `Refresh`, `AutoLogin`, `Validate` |
| UsersController | `api/v1/users` | AdminOrSuperAdmin | `GetList`, `GetById`, `Create`, `Update`, `Delete`, `ResetPassword`, `ChangeProfile`, `ChangePassword`, `ToggleStatus`, `BatchDelete`, `GetCurrentUser` |
| PatientsController | `api/v1/patients` | DoctorOrAdmin | `GetList` [OutputCache], `GetById`, `Create` (201), `Update`, `Delete`, `ToggleStatus`, `BatchDelete` |
| HerbsController | `api/v1/herbs` | DoctorOrAdmin | `GetList` [OutputCache], `GetById`, `Create` (201), `Update`, `Delete`, `ToggleStatus`, `BatchDelete`, `BatchImport` |
| FormulasController | `api/v1/formulas` | DoctorOrAdmin | `GetList`, `GetById`, `Create` (201), `Update`, `Delete`, `ToggleStatus`, `BatchDelete`, `BatchImport`, `GetPendingValidation`, `ValidateHerb` |
| MedicalCasesController | `api/v1/medicalcases` | DoctorOrAdmin | `Create`, `Save`, `Delete`, `GetById`, `GetList`, `Query`, `Search`, `GetConsultations`, `GetPrescriptions`, `SetPrescriptionFlag`, `BatchDelete` |
| MedicalCaseProcessingController | `api/v1/medicalcases` | DoctorOrAdmin | `UpdateStatus`, `Close`, `Suspend`, `Cancel` |
| RegistrationsController | `api/v1/registrations` | DoctorOrAdmin | `Create`, `QuickVisit`, `GetById`, `GetList`, `GetQueue`, `StartVisit`, `Cancel` |
| ReportsController | `api/v1/reports` | DoctorOrAdmin | `GetDailyIncome`, `GetDailyConsultations`, `GetDailyHerbUsage` |
| ConfigurationController | `api/v1/configuration` | AdminOrSuperAdmin | `GetAll`, `GetByKey`, `Validate` |
| DiagnosticsController | `api/v1/diagnostics` | AdminOrSuperAdmin | `GetLoggingStatus`, `EnableDebugMode`, `DisableDebugMode`, `SetLoggingLevel` |
| HealthController | `api/v1/health` | AllowAnonymous | `Get`, `Ping`, `GetDetails` |

#### Filters

| File | Type |
|------|------|
| ApiLoggingFilter.cs | `class ApiLoggingFilter : IAsyncActionFilter` — logs request start, parameters (with sensitive data masking), completion time, exceptions |

---

## 4. Desktop Core (9 projects)

### 4.1 LYBT.Types (14 types)

| Type | Members |
|------|---------|
| `enum UnfinishedCaseChoice` | `Continue`, `CloseAndCreate`, `CloseOnly`, `Cancel` |
| `enum AuthState` | `Idle=0` through `RefreshingToken=50` (11 values) |
| `enum AuthEvent` | `StartLogin` through `Reset` (16 values) |
| `class AuthStateChangedEventArgs` | `PreviousState`, `CurrentState`, `Trigger`, `StatusMessage`, `Timestamp` |
| `record PerformanceMetric` | `OperationName`, `DurationMs`, `MemoryBeforeBytes`, `MemoryAfterBytes`, `MemoryDeltaBytes`, `Timestamp`, `Level`, `FormattedDuration`, `FormattedMemoryDelta` |
| `enum PerformanceLevel` | `Excellent`, `Good`, `Acceptable`, `Poor` |
| `static class PerformanceThresholds` | `ExcellentThreshold=500`, `GoodThreshold=1500`, `AcceptableThreshold=3000`, `GetLevelDescription()` |
| `record PerformanceReport` | `GeneratedAt`, `Metrics`, `TotalDurationMs`, `TotalMemoryDeltaBytes`, `AverageDurationMs`, `SlowestOperation`, `LevelDistribution`, `GetFormattedReport()`, `GetJsonReport()` |
| `record CommandResult<T>` | `Success`, `Data`, `Error` + `Succeeded()`, `Failed()`, `NotFound()`, implicit bool |
| `record CommandResult` | `Success`, `Error` + `Succeeded()`, `Failed()`, implicit bool |
| `record BreadcrumbItem` | `Title`, `ViewName`, `IsCurrent` |
| `static class CacheEvents` | nested `InvalidatedEvent` |
| `record CacheInvalidatedPayload` | `Domain`, `Reason`, `Timestamp` |
| `enum CacheDomain` | `Patients`, `MedicalCases`, `Herbs`, `Formulas`, `Users`, `All` |

### 4.2 LYBT.Desktop.Contracts (78 types)

#### Refit API Interfaces (16)

| Interface | Methods |
|-----------|---------|
| `IAuthApi` | `Login`, `Logout`, `Refresh`, `AutoLogin`, `Validate` |
| `IUserApi` | CRUD + `ResetPassword`, `ChangeProfile`, `ChangePassword`, `ToggleStatus`, `BatchDelete` |
| `IPatientApi` | CRUD + `BatchDelete`, `ToggleStatus`, `BatchImport`, `Export` |
| `IHerbApi` | CRUD + `BatchDelete`, `ToggleStatus`, `BatchImport`, `Export` |
| `IFormulaApi` | CRUD + `BatchDelete`, `ToggleStatus`, `BatchImport`, `PendingValidation`, `ValidateHerb` |
| `IMedicalCaseApi` | CRUD + `Query`, `Search`, `Consultations`, `Prescriptions`, `SetPrescriptionFlag`, `BatchDelete` |
| `IRegistrationApi` | CRUD + `QuickVisit`, `Queue`, `StartVisit`, `Cancel` |
| `IReportsApi` | `DailyIncome`, `DailyConsultations`, `DailyHerbUsage` |
| `IDiagnosticsApi` | `LoggingStatus`, `EnableDebug`, `DisableDebug`, `SetLevel` |
| `ILocalUserApi` | Local user operations |
| `ILocalPatientApi` | Local patient operations |
| `ILocalMedicalCaseApi` | Local medical case operations |
| `ILocalHerbApi` | Local herb operations |
| `ILocalFormulaApi` | Local formula operations |
| `ILocalRegistrationApi` | Local registration operations |
| `ILocalAuthApi` | Local auth operations |

#### Unified ApiClient Interfaces (9)

| Interface | Aggregates |
|-----------|------------|
| `IApiClient` | Auth, Users, Patients, Herbs, Formulas, MedicalCases, Registrations, Reports |
| `IApiClientAuth` | Login, Logout, Refresh, AutoLogin, Validate, HealthCheck |
| `IApiClientUsers` | GetUsers, GetUserById, CreateUser, UpdateUser, DeleteUser, ChangeProfile, ChangePassword, ResetPassword, ToggleStatus, BatchDelete, GetCurrentUser |
| `IApiClientPatients` | GetPatients, GetPatientById, CreatePatient, UpdatePatient, DeletePatient, BatchImport, ExportTemplate, ExportPatients, BatchDelete, ToggleStatus |
| `IApiClientHerbs` | GetHerbs, GetHerbById, CreateHerb, UpdateHerb, DeleteHerb, BatchImport, ExportTemplate, ExportHerbs, BatchDelete, ToggleStatus |
| `IApiClientFormulas` | GetFormulas, GetFormulaById, CreateFormula, UpdateFormula, DeleteFormula, BatchImport, BatchDelete, ToggleStatus, GetPendingValidation, ValidateFormulaHerb |
| `IApiClientMedicalCases` | CreateMedicalCase, UpdateMedicalCase, DeleteMedicalCase, GetById, GetList, Query, Search, GetConsultations, GetPrescriptions, SetPrescriptionFlag, BatchDelete, UpdateStatus |
| `IApiClientRegistrations` | Create, QuickVisit, GetById, GetList, GetQueue, StartVisit, Cancel |
| `IApiClientReports` | GetDailyIncome, GetDailyConsultations, GetDailyHerbUsage |

#### Service Interfaces (30+)

| Interface | Key Members |
|-----------|-------------|
| `IViewModelServices` | `LoggerFactory`, `EventAggregator`, `RegionManager`, `SessionManager`, `UserNotificationService`, `CommonDialogService`, `ToastService`, `RoleRegistry`, `UiThreadDispatcher` |
| `IWorkspaceHost` | `SetBusy`, `ShowErrorAsync`, `ShowSuccessAsync`, `ShowConfirmAsync`, `CommonDialogService`, `NotifyStateChanged`, `RequestEnterEditMode` |
| `ISessionManager` | `CurrentUser`, `CurrentUserId`, `CurrentUserName`, `IsAuthenticated`, `IsLoggedIn`, `SetSession`, `ClearSession`, `HasPermission`, `HasRole`, `IsAdmin`, `GetCurrentUserRoleDisplay`, `SessionExpired`/`SessionChanged` events |
| `INavigationCoordinator` | `NavigateTo`, `NavigateToHome`, `NavigateBack`, `NavigateForward`, `NavigateToBreadcrumb`, `ClearHistory`, `ShowLoginDialog`, `ClearLoginRegion`, `ClearContentRegion`, `CurrentView`, `CanNavigateBack`, `CanNavigateForward`, `Breadcrumbs`, `NavigationHistory`, `NavigationChanged` |
| `ILoginCoordinator` | `CurrentState`, `IsLoggedIn`, `CurrentUser`, `StateChanged`/`LoginSucceeded`/`LogoutCompleted` events, `LoginAsync`, `HandleLoginSuccessAsync`, `LogoutAsync`, `GetDiagnostics` |
| `IConnectionSettingsService` | `CurrentUrl`, `IsLocal`, `LocalUrl`, `RemoteUrl`, `PreferredMode`, `SetUrlAsync`, `SaveRemoteUrlAsync`, `SavePreferredModeAsync`, `UrlChanged`, `IsValidUrl` |
| `IConnectionModeService` | `CurrentMode`, `CurrentModeDisplay`, `IsRemote`, `IsLocal`, `IsRemoteAvailable`, `ApiStatusDisplay`, `DetectBestModeAsync`, `CheckRemoteAvailableAsync`, `TestRemoteConnectionAsync`, `TestLocalConnectionAsync`, `SetMode`, `ModeChanged` |
| `IEmbeddedLocalWebApiService` | `IsRunning`, `BaseUrl`, `StartAsync`, `StopAsync` |
| `IApiHealthMonitor` | `Status`, `ConnectionState`, `LastCheckTime`, `NextCheckTime`, `ConsecutiveFailures`, `LastError`, `IsChecking`, `CheckInterval`, `CheckTimeout`, `CircuitBreakerThreshold`, `CircuitBreakerRecoveryTime`, `StatusChanged`/`CheckCompleted` events, `StartMonitoringAsync`, `StopMonitoringAsync`, `ForceCheckAsync`, `ResetCircuitBreaker` |
| `IMedicalCaseQueryService` | `GetPagedAsync`, `QueryAsync`, `GetUnfinishedCaseByPatientIdAsync`, `CloseCaseAsync` |
| `IMedicalCaseLifecycleService` | `MedicalCaseId`, `CurrentConsultation`, `CurrentPrescription`, `InitializeAsync`, `ReloadAsync`, `SuspendAsync`, `CancelMedicalCaseAsync`, `CompleteMedicalCaseAsync`, `ResumeSuspendedAsync` |
| `IMedicalCaseCommandService` | `Current`, `HasChanges`, `SaveAsync`, `DeleteAsync`, `CreateMedicalCaseAsync` |
| `IRegistrationService` (Desktop) | `CreateAsync`, `GetByIdAsync`, `GetPagedAsync`, `GetQueueAsync`, `StartVisitAsync`, `CancelAsync` |
| `IUserNotificationService` | `HandleExceptionAsync`, `ShowError`/`ShowSuccess`/`ShowWarning`/`ShowInfo`/`ShowConfirmAsync` |
| `IUserActivityTracker` | `LastActivityTime`, `IsUserActive`, `TimeUntilInactive`, `IsTracking`, `SessionExpired` event, `StartTracking`, `StopTracking`, `ResetActivity` |
| `IToastService` | `ShowInfo`/`ShowSuccess`/`ShowWarning`/`ShowError`, `Show` |
| `IStartupPipeline` | `State`, `Steps`, `StateChanged`/`StepCompleted` events, `RegisterStep`, `ExecuteAsync`, `GetDiagnostics`, `Reset` |
| `IStartupStep` | `Name`, `Order`, `IsRequired`, `ParallelGroup`, `ExecuteAsync` |
| `ICredentialVault` | `SavePasswordAsync`, `GetPasswordAsync`, `HasSavedPasswordAsync`, `ClearPasswordAsync`, `SaveAutoLoginTokenAsync`, `GetAutoLoginTokenAsync`, `ClearCredentialsAsync`, `VerifyIntegrityAsync`, `MigrateOldFormatAsync`, `HasValidTokenAsync` |
| `ITokenManager` | `AccessToken`, `RefreshToken`, `AccessTokenExpiry`, `SetTokens`, `ClearTokens`, `IsTokenValid`, `IsTokenExpiringSoon` |
| `ITokenStorageService` | `SaveAuthenticationAsync`, `GetTokenAsync`, `GetRefreshTokenAsync`, `GetLoginResponseAsync`, `ClearAuthenticationAsync`, `IsTokenExpiredAsync`, `GetToken`, `GetLoginResponse`, `ClearAuthentication` |
| `ITokenLifecycleService` | `CurrentState`, `RemainingTime`, `StartMonitoring`, `StartMonitoringFromStorageAsync`, `StopMonitoring`, `UpdateExpiration`, `TryRefreshTokenAsync`, `Reset`, `WarningThreshold` |
| `IAuthenticationService` (Desktop) | `IsLoggedInAsync`, `LoginAsync`, `LogoutAsync`, `GetCurrentUserAsync`, `GetCurrentUser`, `GetToken`, `ValidateTokenAsync`, `ClearAuthInfo`, `CheckConnectionAsync`, `LoginWithAutoTokenAsync` |
| `ILocalAuthService` | `ValidateAsync`, `ChangePasswordAsync` |
| `IPhotoStorageService` | `SavePhotoAsync`, `LoadPhotoAsync`, `DeletePhotoAsync`, `PhotoExists` |
| `ILogoutService` | `LogoutAsync`, `ExecuteLocalLogoutAsync`, `ProcessPendingServerLogoutsAsync`, `PendingServerLogoutCount` |
| `IUsernameStorageService` | `SaveUsernameAsync`, `GetSavedUsernameAsync`, `IsRememberMeEnabledAsync`, `ClearUsernameAsync` |
| `IPrescriptionSettingsService` | `DuplicateHerbMergeStrategy`, `CalculateMergedDosage()` |
| `IClinicSettingsService` | Clinic configuration |
| `ICommonDialogService` | `ShowInfo`/`ShowWarning`/`ShowError`/`ShowConfirm`/`ShowTripleChoice`/`ShowInputAsync`, `ShowOpenFileDialog`/`ShowSaveFileDialogAsync`, `ShowUnfinishedCaseDialogAsync` |
| `IEditable` | `HasUnsavedChanges`, `IsEditing`, `MarkAsChanged`, `MarkAsSaved`, `BeginEdit`, `CancelEdit`, `EndEdit` |
| `IDesktopCacheManager` | `InvalidatePatientCaches`, `InvalidateMedicalCaseCaches`, `InvalidateHerbCaches`, `InvalidateFormulaCaches`, `InvalidateUserCaches`, `InvalidateAll` |
| `IApiRouter` | `CurrentUrl`, `IsLocal` |
| `IApplicationTickService` | `Tick` event, `TickCount`, `IsRunning`, `Start`, `Stop` |
| `IActiveConsultationService` | `HasActiveConsultation`, `ActiveMedicalCaseId`, `Register`, `Unregister`, `RequestLeaveAsync` |
| `ICurrentUserProvider` | `CurrentUserId` |
| `IAsyncInitializable` | `InitializeAsync` |
| `IHerbSearchProvider` | `SearchHerbsAsync`, `GetAllHerbsAsync` |
| `IFormulaSearchProvider` | `GetFormulasPagedAsync`, `GetFormulaByIdAsync` |
| `IAuthenticationStateMachine` | `CurrentState`, `IsAuthenticated`, `IsTransitioning`, `StatusMessage`, `Fire`, `FireAsync`, `CanFire`, `Reset`, `GetPermittedEvents`, `StateChanged` |

#### Repository Interfaces (6)

| Interface | Methods |
|-----------|---------|
| `IUserRepository` | CRUD + domain queries |
| `IRegistrationRepository` (Desktop) | CRUD + queue operations |
| `IPatientRepository` (Desktop) | CRUD + search |
| `IMedicalCaseRepository` (Desktop) | CRUD + lifecycle operations |
| `IHerbRepository` (Desktop) | CRUD + search |
| `IFormulaRepository` (Desktop) | CRUD + search |

#### Role Interfaces (2)

| Interface | Members |
|-----------|---------|
| `IRoleRegistry` | `Register`, `GetDefinition`, `GetAllDefinitions`, `IsRegistered`, `GetHomeViewName`, `GetModulesForRole` |
| `IRoleDefinition` | `Role`, `DisplayName`, `Description`, `HomeViewName`, `RequiredModules`, `BaseModules`, `GetAllModules` |

#### Performance (2)

| Interface | Members |
|-----------|---------|
| `IPerformanceMonitor` | `StartTiming`, `StopTiming`, `RecordMemoryBaseline`, `GetMemorySnapshots`, `GetMetric`, `GetAllMetrics`, `GenerateReport`, `Clear`, `MetricRecorded` event |
| `IDatabaseInitializer` | `EnsureInitializedAsync` |

### 4.3 LYBT.Desktop.Foundation (~65 types)

#### Security (29 types)

| Type | Key Members |
|------|-------------|
| `ITokenManager` | `AccessToken`, `RefreshToken`, `AccessTokenExpiry`, `SetTokens`, `ClearTokens`, `IsTokenValid`, `IsTokenExpiringSoon` |
| `TokenManager` | Impl |
| `ITokenStorageService` | `SaveAuthenticationAsync`, `GetTokenAsync`, `GetRefreshTokenAsync`, `GetLoginResponseAsync`, `ClearAuthenticationAsync`, `IsTokenExpiredAsync` |
| `TokenStorageService` | Impl |
| `ITokenValidator` | `ValidateTokenAsync`, `ValidateAndGetUserInfoAsync` |
| `LocalTokenValidator` | Impl |
| `ITokenRefreshHandler` | `RefreshTokenAsync` |
| `TokenRefreshHandler` : DelegatingHandler | Impl |
| `ITokenLifecycleService` | `CurrentState`, `RemainingTime`, `StartMonitoring`, `StopMonitoring`, `TryRefreshTokenAsync`, `WarningThreshold` |
| `TokenLifecycleService` | Impl (enum `TokenLifecycleState`: NotAuthenticated, Active, Warning, Expired) |
| `IAuthenticationService` (Foundation) | `IsLoggedInAsync`, `LoginAsync`, `LogoutAsync`, `GetCurrentUserAsync`, `GetToken`, `ValidateTokenAsync`, `ClearAuthInfo`, `CheckConnectionAsync`, `LoginWithAutoTokenAsync` |
| `AuthenticationService` | Impl |
| `ICredentialVault` | `SavePasswordAsync`, `GetPasswordAsync`, `HasSavedPasswordAsync`, `ClearPasswordAsync`, `SaveAutoLoginTokenAsync`, `GetAutoLoginTokenAsync`, `ClearCredentialsAsync`, `VerifyIntegrityAsync`, `MigrateOldFormatAsync`, `HasValidTokenAsync` |
| `CredentialVault` | Impl (uses DPAPI encryption) |
| `ILogoutService` | `LogoutAsync`, `ExecuteLocalLogoutAsync`, `ProcessPendingServerLogoutsAsync`, `PendingServerLogoutCount` |
| `LogoutService` | Impl |
| `IUsernameStorageService` | `SaveUsernameAsync`, `GetSavedUsernameAsync`, `IsRememberMeEnabledAsync`, `ClearUsernameAsync` |
| `UsernameStorageService` | Impl |
| `IPhotoStorageService` | `SavePhotoAsync`, `LoadPhotoAsync`, `DeletePhotoAsync`, `PhotoExists` |
| `DpapiPhotoStorageService` | Impl (DPAPI) |
| `static AuthEvents` | 14 event types (LoginStarted, LoginSucceeded, LoginFailed, LogoutStarted, LogoutCompleted, etc.) |
| Records | `LoginStartedPayload`, `LogoutStartedPayload`, `LoginSucceededPayload`, `LoginFailedPayload`, `LogoutCompletedPayload`, `ServerLogoutFailedPayload`, `PendingLogoutsClearedPayload`, `SessionExtendedPayload`, `TokenRefreshSucceededPayload`, `TokenRefreshFailedPayload`, `SessionExpiredPayload`, `PasswordChangedPayload`, `ProfileUpdatedPayload` |
| Enums | `LoginFailureReason`, `SessionExpiredReason`, `ServerLogoutFailureReason`, `TokenRefreshFailureReason` |

#### HTTP (12 types)

| Type | Key Members |
|------|-------------|
| `SwitchingApiClient` : IApiClient | Routes to `RefitApiClient` or `HttpClientApiClient` based on URL |
| `RefitApiClient` | Refit-generated HTTP clients |
| `HttpClientApiClient` | Local WebAPI HTTP client |
| `TokenRefreshHandler` : DelegatingHandler | Auto token refresh |
| `AuthorizationMessageHandler` : DelegatingHandler | Bearer token injection |
| `ApiErrorHandler` | Error handling |
| `ApiService` + `IApiService` | API abstraction |
| `RequestDeduplicator` | Request deduplication |
| `RetryPolicyExtensions` | Retry policies |
| `RefitApiClientExtensions` | Extension methods |
| `HttpClientApiClientExtensions` | Extension methods |
| 8 domain wrappers | `AuthApiClient`, `UserApiClient`, `PatientApiClient`, `HerbApiClient`, `FormulaApiClient`, `MedicalCaseApiClient`, `RegistrationApiClient`, `ReportsApiClient` |

#### Settings, Services, HealthCheck, Modules, Performance, Caching, Utilities

| Type | Key Members |
|------|-------------|
| `ISettingsService` / `SettingsService` | `GetSetting<T>`, `SaveSettingAsync<T>`, `ResetToDefaultsAsync`, `HasSetting` |
| `ConnectionModeService` : IConnectionModeService | Mode detection and switching |
| `IApiHealthCheckService` / `ApiHealthCheckService` | Health checking |
| `IModuleLoadingService` / `ModuleLoadingService` | Prism module loading |
| `IStartupOptimizationService` / `StartupOptimizationService` | Startup optimization |
| `DesktopCacheManager` : IDesktopCacheManager | Cache invalidation |
| `static ExcelHelper` | `ExportToExcel`, `ExportAsync<T>`, `ImportFromExcel`, `ParseAsync<T>`, `CreateTemplate`, `GenerateTemplateAsync<T>` |
| `IApplicationStateService` / `ApplicationStateService` | Application state |
| `IApiRouter` / `ApiRouter` | URL routing |

### 4.4 LYBT.Desktop.Controls (~60 types)

#### Enums (6)

`SuggestionType`, `DuplicateDosageStrategy`, `BadgeType`, `PatientCardDisplayMode`, `HerbItemChangeType`, `HerbListChangeType`

#### Models (5)

`NavigationItem`, `DuplicateDosageStrategyExtensions`, `PatientDisplayModel`, `HerbItemChangedEventArgs`, `HerbListChangedEventArgs`

#### UserControls (20)

`UnifiedPaginationBar`, `LoadingOverlay`, `NavigationSuggestionsPanel`, `NavigationHistoryPanel`, `BreadcrumbControl`, `StatusBadge`, `SearchBox`, `PatientInfoCardControl`, `MasterDetailLayout`, `MasterDetailControlBase`, `InfoCard`, `EmptyState`, `DetailToolbar`, `DataGridToolbar`, `CardReaderStatusControl`, `BreadcrumbBar`, `BaseDetailContainer`, `ToastControl`, `HerbListControl`, `HerbItemControl`, `FormulaViewControl`

#### ViewModels (2)

`HerbListControlViewModel`, `HerbItemControlViewModel` (implements `IHerbItemEditable`)

#### Helpers (2)

`ResponsiveLayoutHelper`, `BindingProxy` (Freezable)

#### Converters (20)

`BoolToBrushConverter`, `BoolToColorConverter`, `BoolToDoubleConverter`, `BoolToIntConverter`, `BoolToMarginConverter`, `BooleanToVisibilityConverter`, `InverseBooleanConverter`, `InverseBooleanToVisibilityConverter`, `InverseNullToVisibilityConverter`, `NullToVisibilityConverter`, `StringToVisibilityConverter`, `ZeroToVisibilityConverter`, `ApiHealthStatusToTextConverter`, `ApiHealthStatusToColorConverter`, `DecocteMethodToVisibilityConverter`, `EnumDescriptionConverter`, `FirstCharacterConverter`, `NavigationPathConverter`, `NavigationHistoryConverter`, `NavigationBreadcrumbConverter`, `NavigationForwardStackConverter`, `ConverterInstances` (static)

### 4.5 LYBT.Desktop.Infrastructure (~95 types)

#### ViewModel Bases (7)

`CoreViewModelBase` : ObservableObject + IDisposable, `DialogViewModelBase` : CoreViewModelBase + IDialogAware, `NavigableViewModelBase`, `ValidatableModelBase` : BindableBase + INotifyDataErrorInfo, `HerbItemViewModelBase` : ObservableObject + IHerbItemEditable, `ChildViewModelBase` : ObservableObject + IDisposable, `MasterDetailViewModelBase<TListItem,TDetail>`, `UnfinishedCaseDialogViewModel` : DialogViewModelBase, `BaseStatusHandler<TListDto>`

#### Models/State (4)

`LoadingState`, `PaginationState`, `SearchState` (all ObservableObject), `PaginationOptions` (record), `DisplayOptions` (record)

#### Services (30+ implementations)

`WpfUiThreadDispatcher`, `ViewModelServices`, `UserNotificationService`, `UserActivityTracker`, `SessionManager`, `SelectionService<T>`, `SearchService`, `PrescriptionSettingsService`, `PaginationService`, `MasterDetailServices<T,T>`, `LoadingStateManager`, `ListViewServices<T>`, `ErrorHandler`, `DialogManager`, `DetailEditorService<T>`, `ConnectionSettingsService`, `CommonDialogService`, `ClinicSettingsService`, `AsyncExecutor`, `ApplicationTickService`, `ApiRouter`, `ActiveConsultationService`, `ToastService`, `NotificationService`

#### Service Interfaces (local)

`ISelectionService<T>`, `ISearchService`, `IPaginationService`, `IMasterDetailServices<T,T>`, `ILoadingStateManager`, `IListViewServices<T>`, `IErrorHandler`, `IDialogManager`, `IDetailEditorService<T>`, `IAsyncExecutor`, `INotificationService` + event args, `IClinicSettingsService`

#### Roles (5)

`RoleRegistry` : IRoleRegistry, `RoleDefinitionBase` : IRoleDefinition, `AdminRoleDefinition`, `SuperAdminRoleDefinition`, `DoctorRoleDefinition`, `ReceptionistRoleDefinition`

#### Repository (1)

`RepositoryBase<TDetailDto,TListDto,TCreateDto,TUpdateDto,TApi>`

#### Events (8)

`CaseConsultationCompletedEvent`, `CasePrescriptionCompletedEvent`, `WorkspaceChangedEvent`, `PatientCreatedEvent`, `PatientUpdatedEvent`, `PatientSelectedEvent` + payload records + `EventSubscriptionManager`

#### Commands (1)

`IApplicationCommands` : `NavigateToHome`, `NavigateToCommand`, `ShowError`, `ShowSuccess`, `ShowConfirm` + `static ApplicationCommands`

#### HTTP (4)

`BaseUrlDelegatingHandler`, `LoggingHttpHandler`, `ApiResponseHelper`, `ProblemDetailsParser` + `record ProblemDetailsResponse`

#### Constants (3)

`SystemConstants`, `RegionNames`, `ViewNames`, `CommonOptions`

#### Behaviors (3)

`ResponsiveLayoutBehavior`, `PasswordBoxHelper`, `DataGridSelectionBehavior`

#### Other

`ConfigurationExtensions`, `TaskExtensions`, `DesktopSerilogConfiguration`, `ViewModelServicesExtensions`, `Windows/BaseDialogWindow`, `Views/UnfinishedCaseDialog`, `Security/SensitiveInfoFilter`

### 4.6 LYBT.Desktop.Navigation (1 type)

`NavigationCoordinator` : INavigationCoordinator — `NavigateTo`, `NavigateToHome` x2, `NavigateBack`, `NavigateForward`, `NavigateToBreadcrumb`, `ClearHistory`, `ShowLoginDialog`, `ClearLoginRegion`, `ClearContentRegion`, `SubscribeToRegionCollection`, `UnsubscribeToRegionCollection`, `CurrentView`, `CanNavigateBack`, `CanNavigateForward`, `Breadcrumbs`, `NavigationHistory`, `NavigationChanged` event + internal: `_forwardStack`, `_breadcrumbs`, `_navigationHistory`, `ViewToModuleMap`, `EnsureModuleLoaded`, `UpdateBreadcrumbs`, `ConvertToNavigationParameters`

### 4.7 LYBT.Desktop.Printing (14 types)

| Type | Members |
|------|---------|
| `PrintingModule` | IModule |
| `IPrintService<TModel>` | `PrintAsync`, `PreviewAsync`, `ExportAsync`, `BatchPrintAsync`, `GetAvailablePrinters`, `SetDefaultPrinter`, `GetDefaultPrinter` |
| `PrescriptionPrintService` : IPrintService\<PrescriptionPrintModel\> | Impl |
| `PrescriptionPdfExporter` | PDF export |
| `record PrescriptionPrintModel` | 35 properties (ClinicName, PatientName, Items, DosageCount, TotalPrice, etc.) |
| `PrescriptionItemPrintModel` | `SequenceNumber`, `HerbName`, `Dosage`, `Unit`, `DecocteMethod`, `DisplayText` |
| `PrintOptions` | `PrinterName`, `Copies`, `PaperSize`, `Orientation`, `DuplexPrinting`, `ShowDialog` |
| `enum PaperSize` | `A4`, `A5`, `Letter`, `Legal` |
| `enum PrintOrientation` | `Portrait`, `Landscape` |
| `enum ExportFormat` | `Xps`, `Pdf` |
| 4 XAML UserControls | `PrescriptionPrintTemplate`, `PrescriptionPrintA4Template`, `PrescriptionContinuationTemplate`, `PrescriptionContinuationA4Template` |

### 4.8 LYBT.Desktop.LocalData (10 types)

| Type | Members |
|------|---------|
| `LocalDbContext` : DbContext | DbSets: `Patients`, `Users`, `Herbs`, `Formulas`, `FormulaHerbItems`, `MedicalCases`, `Consultations`, `Prescriptions`, `PrescriptionItems`, `Registrations` + `OnModelCreating`, `SaveChanges`/`SaveChangesAsync` |
| `DatabaseInitializer` : IDatabaseInitializer | `EnsureInitializedAsync()` |
| `LocalAuthService` : ILocalAuthService | `ValidateAsync`, `ChangePasswordAsync` |
| `ChecksumHelper` | Static checksum utilities |
| 6 Mapperly mappers | `LocalUserMapper`, `LocalPatientMapper`, `LocalMedicalCaseMapper`, `LocalHerbMapper`, `LocalFormulaMapper`, `LocalRegistrationMapper` |

### 4.9 LYBT.Desktop.CardReader (18 types)

| Type | Members |
|------|---------|
| `CardReaderModule` | IModule |
| `ICardReader` : IDisposable | `Name`, `Vendor`, `Model`, `IsConnected`, `ConnectAsync`, `DisconnectAsync`, `ReadCardAsync`, `DetectCardAsync`, `ConnectionStateChanged`, `CardDetected` |
| `CardReaderOptions` | `ConnectTimeout`, `ReadTimeout`, `ReconnectInterval`, `AutoReconnect`, `PhotoSaveDirectory`, `UsbPort`, `SerialPort` |
| `ICardReaderFactory` | `CreateReader()`, `GetAvailableVendors()` |
| `enum CardReaderType` | `Auto`, `HuaDaHD100`, `Mock` |
| `ICardReaderService` : IDisposable | `CurrentReader`, `IsConnected`, `IsAutoReadEnabled`, `InitializeAsync`, `DisconnectAsync`, `ReadCardAsync`, `StartAutoRead`, `StopAutoRead`, events |
| `CardReaderService` | Impl |
| `CardReaderFactory` | Impl |
| `HuaDaHD100CardReader` | Impl (P/Invoke) |
| `MockCardReader` | Impl |
| `HuaDaNativeMethods` | Static P/Invoke declarations |
| `CardReadResult` | `Name`, `IdNumber`, `Gender`, `Nation`, `BirthDate`, `Address`, `IssuingAuthority`, `ValidFrom`, `ValidTo`, `CardType`, `PhotoData`, `PhotoFilePath`, `ReadTime`, `IsSuccess`, `ErrorMessage`, `ErrorCode`, `Age` + `Success()`, `Failure()`, `ParseGender()`, `ParseDate()`, `ParseExpireDate()`, `GetErrorMessage()` |
| `enum CardType` | `IdCard=0`, `ForeignerResidencePermit=1`, `HongKongMacaoTaiwanResidencePermit=2` |
| `IPatientCardReaderIntegration` | `FindPatientByIdNumberAsync`, `QuickCreatePatientAsync`, `FindOrCreatePatientAsync`, `GetPatientDetailByIdAsync`, `MatchPatientAsync` |
| `PatientFromCardResult` | `PatientId`, `Name`, `IdNumber`, `IsNewlyCreated`, `LastVisitTime`, `VisitCount` |
| `enum PatientMatchType` | `ExactMatch`, `FuzzyMatch`, `MultipleCandidates`, `NoMatch` |
| `PatientMatchResult` | `MatchType`, `Patient`, `Candidates` |
| `enum CardReaderIntegrationEventType` | `PatientFound`, `PatientNotFound`, `PatientCreated`, `ReadFailed` |
| `CardReaderIntegrationEventArgs` | `EventType`, `CardResult`, `Patient`, `ErrorMessage` |

---

## 5. Desktop Modules (8 projects)

### 5.1 LYBT.Desktop.Auth

| Type | Members |
|------|---------|
| `AuthenticationModule` | IModule, deps: none (root) |
| `LoginViewModel` | Username, Password, LoginCommand, AutoLogin, ServerConfig |
| `ServerConfigDialogViewModel` | URL config, connection testing |
| `FirstRunSetupDialogViewModel` | First-run wizard |

### 5.2 LYBT.Desktop.Users

| Type | Members |
|------|---------|
| `UsersModule` | IModule, deps: AuthenticationModule |
| `UserMasterDetailViewModel` | MasterDetailViewModelBase\<UserListDto, UserDetailDto\> |
| `UserEditorViewModel` | User CRUD editing |
| `UserPasswordHandler` | Password management |
| `UserStatusHandler` | Status toggle |
| `RemoteUserService` : IUserService | API calls |
| `UserRepository` : IUserRepository | Local API calls |

### 5.3 LYBT.Desktop.Patients

| Type | Members |
|------|---------|
| `PatientsModule` | IModule, deps: Authentication, Users |
| `PatientMasterDetailViewModel` | MasterDetailViewModelBase\<PatientListDto, PatientDetailDto\> |
| `PatientEditorViewModel` | Patient CRUD editing |
| `PatientCardReaderViewModel` | Card reader integration |
| `IPatientSearchCache` | Search caching |
| `PatientSearchManager` | Search management |
| `MedicalCaseStartCoordinator` | Medical case creation workflow |
| `RemotePatientService` : IPatientService | API calls |
| `PatientRepository` : IPatientRepository | Local API calls |

### 5.4 LYBT.Desktop.Herbs

| Type | Members |
|------|---------|
| `HerbsModule` | IModule, deps: AuthenticationModule |
| `HerbMasterDetailViewModel` | MasterDetailViewModelBase\<HerbListDto, HerbDetailDto\> |
| `HerbEditorViewModel` | Herb CRUD editing |
| `HerbStatusHandler` | Status toggle |
| `RemoteHerbService` : IHerbService | API calls |
| `HerbRepository` : IHerbRepository | Local API calls |
| `HerbMapper` (Mapperly) | Entity↔DTO mapping |

### 5.5 LYBT.Desktop.Formula

| Type | Members |
|------|---------|
| `FormulaModule` | IModule, deps: HerbsModule |
| `FormulaMasterDetailViewModel` | MasterDetailViewModelBase\<FormulaListDto, FormulaDetailDto\> |
| `FormulaEditorViewModel` | Formula CRUD editing |
| `FormulaStatusHandler` | Status toggle |
| `FormulaItem` (UI Model) | 20+ properties, computed: TypeText, StatusText, HerbCompositionText, SearchText |
| `FormulaHerbItem` (Item) | HerbId, HerbName, Dosage, Unit, Usage, SortOrder, DecocteMethod |
| `FormulaEditContext` | Edit model with Herbs collection |
| `FormulaDetailModel` | Extended detail model |
| `FormulaMapper` (Mapperly) | ToItem, ToDto, ToInputDto |
| `FormulaHerbItemMapper` (Mapperly) | ToItem, ToDto, ToInputDto |
| `FormulaDetailModelMapper` (Mapperly) | ToItem, ToDto, ToInputDto |
| `RemoteFormulaService` : IFormulaService | API calls |
| `FormulaRepository` : IFormulaRepository | Local API calls |

### 5.6 LYBT.Desktop.MedicalCase (26 types)

| Type | Members |
|------|---------|
| `MedicalCaseModule` | IModule, deps: Patients, Herbs, Formula |
| `MedicalCaseMasterDetailViewModel` | MasterDetailViewModelBase\<MedicalCaseListDto, MedicalCaseDetailModel\> + `ConsultationEditor`, `PrescriptionEditor` child VMs |
| `ConsultationEditorViewModel` | `Consultation` ConsultationItem, `InitializeFromDto()`, `InitializeForNewCase()`, `GetConsultationData()`, `Validate()`, `Reset()` |
| `PrescriptionEditorViewModel` | `Prescription` PrescriptionItem, `InitializeFromDto()`, `InitializeForNewCase()`, `GetPrescriptionData()`, `Validate()`, `Reset()` |
| `MedicalCaseCommandsViewModel` | Delegate commands: Save, Suspend, Complete, Print, ExportPdf, EnterEditMode, ImportFormula, CopyHistory, ClearHerbs |
| `MedicalCaseService` : IMedicalCaseService | Aggregate service: CRUD, lifecycle, query, cache management |
| `MedicalCaseChangeTracker` | `SetBaseline()`, `HasChanges()`, `ClearBaseline()` |
| `MedicalCaseRepository` : IMedicalCaseRepository | Local API calls for all medical case operations |
| `EditModeStateMachine` : IEditModeStateMachine | 14-entry transition table, `Initialize()`, `CanFire()`, `Fire()`, `GetPermittedEvents()` |
| `PrescriptionPrintHandler` | `PrintPreviewAsync()`, `ExportPdfAsync()`, `BuildPrescriptionDetailDto()` |
| `PrintResult` | `IsSuccess`, `ErrorMessage` + factory methods |
| `WorkspaceState` (record) | 15+ properties: EditState, Mode, CanEdit, IsPrescriptionEnabled, etc. + computed: IsEditing, IsReadOnly, ShowEditBanner, HeaderTitle |
| `CompletenessCheck` (record) | DiagnosisComplete, PrescriptionDecisionComplete, PrescriptionContentComplete, DosageCountComplete, CanCompleteCase |
| `MedicalCaseDetailModel` | Extended model with prescription items, computed: DiagnosisSummary, PrescriptionSummary, StatusText |
| `PrescriptionItem` (UI Model) | 20+ properties, computed: ItemCount, HasItems, IsValid, TotalPrice |
| `ConsultationItem` (UI Model) | 15+ properties, computed: IsDiagnosisComplete, IsPresentIllnessValid |
| Enums | `WorkspaceMode` (Clinical/Management/Reception), `WorkspaceEditState` (6 values), `WorkspaceEditEvent` (10 values), `EditType` (Create/EditSuspended/EditCompleted/ViewOnly), `EditState` (Editing/ReadOnly) |
| `MedicalCaseNavigationParameters` | Constants + factory: ForClinical, ForManagementView, ForManagementEdit |
| Dialogs | `UnsavedChangesDialogViewModel`, `HistoryCopyDialogViewModel`, `FormulaImportDialogViewModel` |
| Interfaces | `IValidatable`, `IMedicalCaseWorkspaceContext`, `IMedicalCaseService`, `IMedicalCaseEditContext`, `IEditModeStateMachine`, `IDataProvider` |

### 5.7 LYBT.Desktop.Registration (7 types)

| Type | Members |
|------|---------|
| `RegistrationModule` | IModule, deps: Authentication, Patients, Users |
| `RegistrationListViewModel` | WaitingQueue, SelectedRegistration, Commands: Refresh, Create, StartVisit, Cancel + auto-refresh |
| `RemoteRegistrationService` : IRegistrationService | API calls |
| `RegistrationRepository` : IRegistrationRepository | Local API calls |
| `RegistrationCreateDialogViewModel` | Dialog for creating registrations |
| `RegistrationMapper` (Mapperly) | Entity↔DTO mapping |
| `RegistrationQueueItem` | Queue display model |

### 5.8 LYBT.Desktop.Reports

| Type | Members |
|------|---------|
| `ReportsModule` | IModule |
| `ReportsHomeViewModel` | Dashboard statistics |
| `ReportsHomeView` | Dashboard UI |

---

## 6. Desktop Roles (4 projects)

| Project | Types | Home View | Required Modules |
|---------|-------|-----------|-----------------|
| `LYBT.Desktop.Admin` | `AdminModule`, `AdminHomeViewModel`, `SystemSettingsViewModel`, `SystemSettingsService` | `AdminHomeView` | Users, Patients, Herbs, Formula, MedicalCase |
| `LYBT.Desktop.Clinical` | `ClinicalModule`, `PatientSelectionViewModel`, `MedicalCaseWorkspaceViewModel` (766 lines, FSM), `ClinicalWorkspaceViewModel`, `PendingQueueViewModel`, `CardReaderViewModel` | `ClinicalWorkspaceView` | Users, Patients, Herbs, Formula, MedicalCase, Registration |
| `LYBT.Desktop.Receptionist` | `ReceptionistModule`, `ReceptionistHomeViewModel`, `RegistrationQueueItem` | `ReceptionistHomeView` | Users, Patients, Registration |
| `LYBT.Desktop.Sysadmin` | `SysadminModule`, `SysadminHomeViewModel`, `LogLevelControlViewModel`, `StatusCard`, `DashboardStatus` | `SysadminHomeView` | Users, Sysadmin |

---

## 7. Desktop Shell (1 project)

**Path**: `src/Client/Desktop/Shell/`

| Type | Members |
|------|---------|
| `App` : PrismApplication | `OnStartup`, `CreateShell`, `RegisterTypes`, `ConfigureViewModelLocator`, `ConfigureModuleCatalog`, `OnInitialized`, `LoadRoleBasedModulesAsync` |
| `MainWindowViewModel` | 642 lines — Login state, sidebar, navigation, keyboard shortcuts, theme, health check |
| `AccountSettingsViewModel` | Account settings management |
| `LoginCoordinator` | Full login flow orchestration |
| `StartupPipeline` | Step-based startup (ErrorHandling → ModuleCoordinator → CoreServices → LocalWebApiStartup → ApiHealthCheck → Warmup) |
| `ApiHealthMonitor` | Circuit breaker health monitoring |
| `SessionLifecycleManager` | Session lifecycle management |
| `StatusBarManager` | Status bar management |
| `NavigationManager` | Navigation management |
| `MenuManager` | Menu management |
| 3 Dialog VMs | Confirmation, Message, Input dialogs |
| 6 extension classes | DI registration extensions |

---

## 8. Desktop LocalWebAPI (1 project)

**Path**: `src/Client/Desktop/LocalWebAPI/`

| Type | Members |
|------|---------|
| `LocalWebApiProgram` | Embedded ASP.NET Core host (port 5300) |
| `LocalJwtConfig` | JWT configuration for local mode |
| `LocalApiMapper` (Mapperly) | DTO mapping for local API |
| `LocalWebApiSeedData` | Seed data for local database |
| `MedicalCasesController` | 219 lines, 16 endpoints |
| `FormulasController` | Formula CRUD endpoints |
| 6 `Http*Repository` classes | HTTP-based repository implementations for local mode |

---

## 9. Shared (8 projects)

### 9.1 LYBT.Shared.Primitives

| File | Type | Members |
|------|------|---------|
| ErrorCodes.cs | `enum ErrorCode` | ~120 codes: 0xxxx (General), 1xxxx (Users/Auth), 2xxxx (Patients), 3xxxx (MedicalCase), 4xxxx (Prescriptions), 5xxxx (Herbs), 6xxxx (Formula), 7xxxx (Sync), 8xxxx (Registration) |
| ValidationConstants.cs | `static class ValidationConstants` | `NameMaxLength=100`, `RemarkMaxLength=1000`, `LongRemarkMaxLength=2000`, `AddressMaxLength=200`, `PhoneMaxLength=20`, `IdCardMaxLength=18`, `DiagnosisMaxLength=500`, `FourDiagnosisMaxLength=2000`, `DosageCountMinValue=1..100`, `HerbDosageMinValue=0.1..1000`, `PriceMinValue=0.01..100000`, `AgeMinValue=0..200`, `IdCardRegex`, `PhoneRegex`, `EmailRegex` |

### 9.2 LYBT.Shared.Models

**Enums (13 files)**: `UserRole`, `MedicalCaseStatus`, `MedicalCaseQueryType`, `RegistrationSource`, `RegistrationStatus`, `Gender`, `DecocteMethod`, `HerbRole`, `FormulaType`, `FormulaValidationStatus`, `CommonStatus`, `PasswordStrength`, `ErrorCategory`, `ErrorSeverity`, `DuplicateStrategy`

**Contracts (80+ DTOs)**: See Section [S7] of the architecture analysis for complete property lists.

**Extensions**: `EnumExtensions` (GetDescription, GetAllDescriptions, GetEnumByDescription, ToKeyValueList), `DtoConversionExtensions` (MedicalCaseDetailDto.ToInputDto, etc.)

### 9.3 LYBT.Shared.Configuration

| File | Type | Members |
|------|------|---------|
| DatabaseOptions.cs | `class DatabaseOptions` | `ConnectionString`, `AutoMigrate`, `MigrationTimeoutSeconds`, nested `ConnectionPoolOptions`, `MonitoringOptions`, `RetryPolicyOptions` |
| SecurityOptions.cs | `class SecurityOptions` | `RateLimitingOptions` (Global, Login, API, WhitelistedIPs), `AccountLockoutOptions` (MaxFailedCount, LockoutMinutes), `AuditRetentionDays` |
| JwtOptions.cs | `class JwtOptions` | `SecretKey`, `Issuer`, `Audience`, `AccessTokenExpirationMinutes`, `RefreshTokenExpirationDays`, `ClockSkewSeconds` |
| ApiClientOptions.cs | `class ApiClientOptions` | `BaseUrl`, `TimeoutSeconds`, `IgnoreSslErrors` |
| Validators | `DatabaseOptionsValidator`, `JwtOptionsValidator`, `SecurityOptionsValidator` |
| Extensions | `AddLybtServerConfiguration()`, `AddLybtClientConfiguration()` |

### 9.4 LYBT.Shared.Utilities

| File | Type | Members |
|------|------|---------|
| PasswordHelper.cs | `static class PasswordHelper` | `HashPassword()`, `VerifyPassword()`, `ValidatePasswordStrength()`, `GenerateRandomPassword()` (BCrypt cost 12) |
| BcryptPasswordService.cs | `class BcryptPasswordService : IPasswordService` | DI wrapper |
| PasswordPolicyValidator.cs | `class PasswordPolicyValidator` | Configurable rules: min length, uppercase, lowercase, digits, special chars |
| PinYinHelper.cs | `static class PinYinHelper` | `GetInitials()` — Chinese to Pinyin initials |
| CacheExtensions.cs | `static class CacheExtensions` | `RemoveByPrefix()`, `Clear()` for IMemoryCache |

### 9.5 LYBT.Shared.Validators (11 validators)

| Validator | Target |
|-----------|--------|
| `LoginRequestValidator` | LoginRequest |
| `SuperAdminLoginRequestValidator` | SuperAdminLoginRequest |
| `ChangePasswordRequestValidator` | ChangePasswordRequest |
| `PatientInputDtoValidator` | PatientInputDto |
| `MedicalCaseInputDtoValidator` | MedicalCaseInputDto |
| `ConsultationInputDtoValidator` | ConsultationInputDto |
| `PrescriptionInputDtoValidator` | PrescriptionInputDto |
| `HerbInputDtoValidator` | HerbInputDto |
| `FormulaInputDtoValidator` | FormulaInputDto |
| `UserInputDtoValidator` | UserInputDto |
| `MedicalCaseBusinessRules` | MedicalCase business rules |

### 9.6 LYBT.Shared.ExceptionHandling

| File | Type | Members |
|------|------|---------|
| AppException.cs | `class AppException : Exception` | `ErrorCode`, `TypedErrorCode`, `UserMessage`, `Category`, `GetHttpStatusCode()` |
| NotFoundException.cs | Subclasses | `User.NotFound(id)`, `Patient.NotFound(id)`, `Herb.NotFound(id)`, `MedicalCase.NotFound(id)`, `Formula.NotFound(id)`, `Registration.NotFound(id)` |
| BusinessException.cs | Subclass | Business logic errors |
| ValidationException.cs | Subclass | `Errors` dict (field → string[]) |
| ConflictException.cs | Subclass | Conflict errors |
| UnauthorizedException.cs | Subclass | Auth errors |
| ApiException.cs | Subclass | External API errors |
| ExceptionFactory.cs | Static nested classes | `User`, `Patient`, `Herb`, `Prescription`, `MedicalCase`, `Formula` — typed factory methods |
| BusinessExceptionHandler.cs | IExceptionHandler | Catches AppException → HTTP status mapping |
| SystemExceptionHandler.cs | IExceptionHandler | Fallback for non-AppException types |
| DesktopExceptionHandler.cs | IDesktopExceptionHandler | Desktop error handling |

### 9.7 LYBT.Shared.Logging

| File | Type | Members |
|------|------|---------|
| TraceContext.cs | `class TraceContext` | Correlation ID management |
| CorrelationIdEnricher.cs | Serilog enricher | Adds correlation ID to logs |
| SensitiveDataMasker.cs | `static class SensitiveDataMasker` | `IsSensitiveFieldName()`, `MaskValue()` |
| SensitiveDataDestructuringPolicy.cs | Serilog policy | Redacts sensitive fields |
| LoggingLevelManager.cs | `class LoggingLevelManager` | Runtime log level control |
| DebugModeInfo.cs | `class DebugModeInfo` | Debug mode state |

### 9.8 LYBT.Shared.Components

| File | Type | Members |
|------|------|---------|
| HerbValidatorBase.cs | `abstract class HerbValidatorBase<TItem>` | Generic herb import validation: duplicate detection, dosage range warnings, required-field checks, `Merge()` |

---

## 10. Tools (4 projects)

| Project | File | Purpose |
|---------|------|---------|
| ApiTester | Program.cs | Login + password reset testing |
| LoginTester | Program.cs | Verify password reset |
| PasswordHashGenerator | Program.cs | BCrypt hash generation |
| UserInfoVerifier | Program.cs | User info verification |

---

## 11. Tests (3 projects)

| Project | Scope | Framework |
|---------|-------|-----------|
| LYBT.Tests.Server | Integration (real SQL Server + Respawn) | xUnit + WebApplicationFactory |
| LYBT.Tests.Desktop | Desktop with LocalDB | xUnit |
| LYBT.Tests.Architecture | Module isolation, dependency guards | NetArchTest |

---

## Cross-Verification Summary

| Layer | Serena Verified | codegraph Verified | Total Types |
|-------|----------------|-------------------|-------------|
| Entities | ✓ All 13 | ✓ All 13 | 13 |
| Infrastructure | ✓ All repos/services | ✓ All configs | ~25 |
| Server Modules | ✓ All interfaces | ✓ All implementations | ~80 |
| WebAPI Controllers | ✓ All 12 | ✓ All actions | ~15 |
| Desktop Contracts | ✓ All 78 | ✓ All 78 | 78 |
| Desktop Foundation | ✓ All 65 | ✓ All 65 | 65 |
| Desktop Controls | ✓ All 60 | ✓ All 60 | 60 |
| Desktop Infrastructure | ✓ All 95 | ✓ All 95 | 95 |
| Desktop Modules | ✓ All 100 | ✓ All 100 | 100 |
| Desktop Roles | ✓ All 25 | ✓ All 25 | 25 |
| Desktop Shell | ✓ All 40 | ✓ All 40 | 40 |
| LocalWebAPI | ✓ All 25 | ✓ All 25 | 25 |
| Shared | ✓ All 120 | ✓ All 120 | 120 |
| Tools | ✓ All 4 | ✓ All 4 | 4 |
| **Total** | **~814** | **~814** | **814** |
