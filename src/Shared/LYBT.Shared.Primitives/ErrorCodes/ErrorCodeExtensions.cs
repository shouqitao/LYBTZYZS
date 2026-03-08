namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// ErrorCode扩展方法
/// consolidate-exception-handling: 错误码辅助方法
/// </summary>
public static class ErrorCodeExtensions
{
    /// <summary>
    /// 获取错误码对应的HTTP状态码
    /// </summary>
    public static int ToHttpStatusCode(this ErrorCode errorCode)
    {
        return errorCode switch
        {
            // 400 Bad Request - 验证错误
            ErrorCode.InvalidRequest => 400,
            ErrorCode.ValidationFailed => 400,
            ErrorCode.PasswordPolicyViolation => 400,
            ErrorCode.PrescriptionNoHerbs => 400,
            ErrorCode.FormulaNoHerbs => 400,
            ErrorCode.FormulaIdInvalid => 400,
            ErrorCode.FormulaInvalidPagination => 400,
            ErrorCode.FormulaHerbItemIdInvalid => 400,
            ErrorCode.FormulaBatchEmpty => 400,
            ErrorCode.FormulaBatchImportEmpty => 400,
            ErrorCode.McRequestIdMismatch => 400,
            ErrorCode.McInvalidPagination => 400,
            ErrorCode.McBatchQueryExceeded => 400,
            ErrorCode.McBatchOperationEmpty => 400,
            ErrorCode.McInvalidPatientId => 400,
            ErrorCode.McInvalidCountParam => 400,
            ErrorCode.PatientPhoneDuplicate => 400,
            ErrorCode.PatientBatchOperationEmpty => 400,
            ErrorCode.PatientBatchCheckExceeded => 400,
            ErrorCode.PatientInvalidPagination => 400,
            ErrorCode.PatientImportFileEmpty => 400,
            ErrorCode.PatientImportFileFormat => 400,
            ErrorCode.PatientImportFileSize => 400,
            ErrorCode.PatientImportNoWorksheet => 400,
            ErrorCode.PatientImportRowExceeded => 400,
            ErrorCode.HerbInvalidPrice => 400,
            ErrorCode.HerbValidationFailed => 400,
            ErrorCode.HerbInvalidPagination => 400,
            ErrorCode.HerbBatchEmpty => 400,
            ErrorCode.HerbBatchImportExceeded => 400,
            ErrorCode.HerbBatchCheckExceeded => 400,
            ErrorCode.HerbImportFileEmpty => 400,
            ErrorCode.HerbImportFileFormat => 400,
            ErrorCode.HerbImportFileSize => 400,
            ErrorCode.UnsupportedEntityType => 400,
            ErrorCode.JsonDeserializeFailed => 400,

            // 401 Unauthorized - 认证错误
            ErrorCode.Unauthorized => 401,
            ErrorCode.InvalidPassword => 401,
            ErrorCode.CredentialsExpired => 401,
            ErrorCode.DeviceMismatch => 401,
            ErrorCode.SessionExpired => 401,
            ErrorCode.InvalidRefreshToken => 401,
            ErrorCode.AuthInvalidCredentials => 401,
            ErrorCode.AuthTokenInvalid => 401,
            ErrorCode.AuthTokenRevoked => 401,
            ErrorCode.AuthRefreshTokenExpired => 401,
            ErrorCode.AuthRefreshTokenInvalid => 401,
            ErrorCode.AuthConcurrentSessionLimit => 401,

            // 403 Forbidden - 授权错误
            ErrorCode.Forbidden => 403,
            ErrorCode.UserDisabled => 403,
            ErrorCode.UserLocked => 403,
            ErrorCode.McCannotEditCase => 403, // 302xx: 权限错误
            ErrorCode.McCannotDeleteCase => 403,
            ErrorCode.McCannotCancelCase => 403,
            ErrorCode.McCannotDeletePrescription => 403,
            ErrorCode.McCannotSuspendCase => 403,
            ErrorCode.PatientDisabled => 403,
            ErrorCode.HerbDisabled => 403,
            ErrorCode.HerbNoPermission => 403,
            ErrorCode.FormulaDisabled => 403,
            ErrorCode.FormulaNoPermission => 403,
            ErrorCode.CannotDeleteSysAdmin => 403,
            ErrorCode.RegistrationUnauthorizedCancel => 403,

            // 404 Not Found - 资源未找到
            ErrorCode.NotFound => 404,
            ErrorCode.UserNotFound => 404,
            ErrorCode.RoleNotFound => 404,
            ErrorCode.PatientNotFound => 404,
            ErrorCode.MedicalCaseNotFound => 404,
            ErrorCode.McPatientNotFound => 404,
            ErrorCode.McDoctorNotFound => 404,
            ErrorCode.McCaseNotFound => 404,
            ErrorCode.PrescriptionNotFound => 404,
            ErrorCode.HerbNotFound => 404,
            ErrorCode.FormulaNotFound => 404,
            ErrorCode.FormulaDeleteFailed => 404,
            ErrorCode.SyncEntityNotFound => 404,
            ErrorCode.RegistrationNotFound => 404,

            // 409 Conflict - 冲突错误
            ErrorCode.ConcurrencyConflict => 409,
            ErrorCode.UserNameExists => 409,
            ErrorCode.EmailExists => 409,
            ErrorCode.PatientIdCardExists => 409,
            ErrorCode.PatientPhoneExists => 409,
            ErrorCode.DuplicateMedicalCase => 409,
            ErrorCode.MedicalCaseVersionConflict => 409,
            ErrorCode.HerbNameExists => 409,
            ErrorCode.FormulaNameExists => 409,
            ErrorCode.MedicalCaseLocked => 409,
            ErrorCode.SyncDataConflict => 409,

            // 422 Unprocessable Entity - 业务规则违反
            ErrorCode.InvalidPatientStatus => 422,
            ErrorCode.InvalidMedicalCaseState => 422,
            ErrorCode.InvalidPrescriptionState => 422,
            ErrorCode.PatientHasActiveCases => 422,
            ErrorCode.McActiveCaseExists => 422,
            ErrorCode.McSuspendedCaseExists => 422,
            ErrorCode.McPatientDisabled => 422,
            ErrorCode.McInvalidStatusTransition => 422,
            ErrorCode.McPrescriptionFlagRequired => 422,
            ErrorCode.McPrescriptionRequired => 422,
            ErrorCode.McCompletedCannotSuspend => 422,
            ErrorCode.McDeletedCannotSuspend => 422,
            ErrorCode.McCompletedCannotCancel => 422,
            ErrorCode.McAlreadyDeleted => 422,
            ErrorCode.McCancelReasonRequired => 422,
            ErrorCode.McPrescriptionItemsRequired => 422,
            ErrorCode.McPrescriptionFlagNotSet => 422,
            ErrorCode.McPrescriptionAlreadyExists => 422,
            ErrorCode.McPrintedRequiresReason => 422,
            ErrorCode.McPrintedCannotDelete => 422,
            ErrorCode.MedicalCaseArchived => 422,
            ErrorCode.MedicalCaseMissingDiagnosis => 422,
            ErrorCode.MedicalCaseHasPrescriptions => 422,
            ErrorCode.PrescriptionAlreadyDispensed => 422,
            ErrorCode.PrescriptionDosageExceeded => 422,
            ErrorCode.PrescriptionContraindication => 422,
            ErrorCode.PrescriptionCompleted => 422,
            ErrorCode.HerbInsufficientStock => 422,
            ErrorCode.HerbInUse => 422,
            ErrorCode.FormulaValidationFailed => 422,
            ErrorCode.FormulaInUse => 422,
            ErrorCode.PasswordChangeRequired => 422,
            ErrorCode.SyncPatientNotFound => 422,
            ErrorCode.SyncHerbNotFound => 422,
            ErrorCode.SyncCaseLocked => 422,
            ErrorCode.SyncHerbHasReference => 422,
            ErrorCode.SyncPatientHasReference => 422,
            ErrorCode.RegistrationInvalidStatusTransition => 422,
            ErrorCode.RegistrationCancelNotAllowed => 422,
            ErrorCode.RegistrationPatientDisabled => 422,
            ErrorCode.RegistrationDoctorNotAvailable => 422,
            ErrorCode.RegistrationDuplicateWaiting => 409,

            // 429 Too Many Requests
            ErrorCode.RateLimitExceeded => 429,

            // 503 Service Unavailable
            ErrorCode.ServiceUnavailable => 503,
            ErrorCode.Timeout => 503,

            // 500 Internal Server Error - 默认/同步上传失败
            ErrorCode.HerbUploadFailed => 500,
            ErrorCode.PatientUploadFailed => 500,
            ErrorCode.FormulaUploadFailed => 500,
            ErrorCode.MedicalCaseUploadFailed => 500,
            ErrorCode.McPrescriptionCreateRetryFailed => 500,
            ErrorCode.McSaveRetryFailed => 500,
            ErrorCode.McConsultationNotFound => 500,
            ErrorCode.SyncReferenceCheckFailed => 500,

            _ => 500
        };
    }

    /// <summary>
    /// 获取错误码对应的错误类别
    /// </summary>
    public static ErrorCategory ToCategory(this ErrorCode errorCode)
    {
        return errorCode switch
        {
            // 验证错误
            ErrorCode.InvalidRequest => ErrorCategory.Validation,
            ErrorCode.ValidationFailed => ErrorCategory.Validation,
            ErrorCode.PasswordPolicyViolation => ErrorCategory.Validation,
            ErrorCode.PrescriptionNoHerbs => ErrorCategory.Validation,
            ErrorCode.FormulaNoHerbs => ErrorCategory.Validation,
            ErrorCode.FormulaIdInvalid => ErrorCategory.Validation,
            ErrorCode.FormulaInvalidPagination => ErrorCategory.Validation,
            ErrorCode.FormulaHerbItemIdInvalid => ErrorCategory.Validation,
            ErrorCode.FormulaBatchEmpty => ErrorCategory.Validation,
            ErrorCode.FormulaBatchImportEmpty => ErrorCategory.Validation,
            ErrorCode.McRequestIdMismatch => ErrorCategory.Validation,
            ErrorCode.McInvalidPagination => ErrorCategory.Validation,
            ErrorCode.McBatchQueryExceeded => ErrorCategory.Validation,
            ErrorCode.McBatchOperationEmpty => ErrorCategory.Validation,
            ErrorCode.McInvalidPatientId => ErrorCategory.Validation,
            ErrorCode.McInvalidCountParam => ErrorCategory.Validation,
            ErrorCode.PatientPhoneDuplicate => ErrorCategory.Validation,
            ErrorCode.PatientBatchOperationEmpty => ErrorCategory.Validation,
            ErrorCode.PatientBatchCheckExceeded => ErrorCategory.Validation,
            ErrorCode.PatientInvalidPagination => ErrorCategory.Validation,
            ErrorCode.PatientImportFileEmpty => ErrorCategory.Validation,
            ErrorCode.PatientImportFileFormat => ErrorCategory.Validation,
            ErrorCode.PatientImportFileSize => ErrorCategory.Validation,
            ErrorCode.PatientImportNoWorksheet => ErrorCategory.Validation,
            ErrorCode.PatientImportRowExceeded => ErrorCategory.Validation,
            ErrorCode.HerbInvalidPrice => ErrorCategory.Validation,
            ErrorCode.HerbValidationFailed => ErrorCategory.Validation,
            ErrorCode.HerbInvalidPagination => ErrorCategory.Validation,
            ErrorCode.HerbBatchEmpty => ErrorCategory.Validation,
            ErrorCode.HerbBatchImportExceeded => ErrorCategory.Validation,
            ErrorCode.HerbBatchCheckExceeded => ErrorCategory.Validation,
            ErrorCode.HerbImportFileEmpty => ErrorCategory.Validation,
            ErrorCode.HerbImportFileFormat => ErrorCategory.Validation,
            ErrorCode.HerbImportFileSize => ErrorCategory.Validation,
            ErrorCode.UnsupportedEntityType => ErrorCategory.Validation,
            ErrorCode.JsonDeserializeFailed => ErrorCategory.Validation,

            // 认证错误
            ErrorCode.Unauthorized => ErrorCategory.Authentication,
            ErrorCode.InvalidPassword => ErrorCategory.Authentication,
            ErrorCode.CredentialsExpired => ErrorCategory.Authentication,
            ErrorCode.DeviceMismatch => ErrorCategory.Authentication,
            ErrorCode.SessionExpired => ErrorCategory.Authentication,
            ErrorCode.InvalidRefreshToken => ErrorCategory.Authentication,
            ErrorCode.AuthInvalidCredentials => ErrorCategory.Authentication,
            ErrorCode.AuthTokenInvalid => ErrorCategory.Authentication,
            ErrorCode.AuthTokenRevoked => ErrorCategory.Authentication,
            ErrorCode.AuthRefreshTokenExpired => ErrorCategory.Authentication,
            ErrorCode.AuthRefreshTokenInvalid => ErrorCategory.Authentication,
            ErrorCode.AuthConcurrentSessionLimit => ErrorCategory.Authentication,

            // 授权错误
            ErrorCode.Forbidden => ErrorCategory.Authorization,
            ErrorCode.UserDisabled => ErrorCategory.Authorization,
            ErrorCode.UserLocked => ErrorCategory.Authorization,
            ErrorCode.McCannotEditCase => ErrorCategory.Authorization,
            ErrorCode.McCannotDeleteCase => ErrorCategory.Authorization,
            ErrorCode.McCannotCancelCase => ErrorCategory.Authorization,
            ErrorCode.McCannotDeletePrescription => ErrorCategory.Authorization,
            ErrorCode.McCannotSuspendCase => ErrorCategory.Authorization,
            ErrorCode.PatientDisabled => ErrorCategory.Authorization,
            ErrorCode.HerbDisabled => ErrorCategory.Authorization,
            ErrorCode.HerbNoPermission => ErrorCategory.Authorization,
            ErrorCode.FormulaDisabled => ErrorCategory.Authorization,
            ErrorCode.FormulaNoPermission => ErrorCategory.Authorization,
            ErrorCode.CannotDeleteSysAdmin => ErrorCategory.Authorization,
            ErrorCode.RegistrationUnauthorizedCancel => ErrorCategory.Authorization,

            // 资源错误
            ErrorCode.NotFound => ErrorCategory.Resource,
            ErrorCode.UserNotFound => ErrorCategory.Resource,
            ErrorCode.RoleNotFound => ErrorCategory.Resource,
            ErrorCode.PatientNotFound => ErrorCategory.Resource,
            ErrorCode.MedicalCaseNotFound => ErrorCategory.Resource,
            ErrorCode.McPatientNotFound => ErrorCategory.Resource,
            ErrorCode.McDoctorNotFound => ErrorCategory.Resource,
            ErrorCode.McCaseNotFound => ErrorCategory.Resource,
            ErrorCode.PrescriptionNotFound => ErrorCategory.Resource,
            ErrorCode.HerbNotFound => ErrorCategory.Resource,
            ErrorCode.FormulaNotFound => ErrorCategory.Resource,
            ErrorCode.FormulaDeleteFailed => ErrorCategory.Resource,
            ErrorCode.SyncEntityNotFound => ErrorCategory.Resource,
            ErrorCode.RegistrationNotFound => ErrorCategory.Resource,

            // 并发错误
            ErrorCode.ConcurrencyConflict => ErrorCategory.Concurrency,
            ErrorCode.MedicalCaseVersionConflict => ErrorCategory.Concurrency,
            ErrorCode.MedicalCaseLocked => ErrorCategory.Concurrency,
            ErrorCode.SyncDataConflict => ErrorCategory.Concurrency,

            // 业务逻辑错误
            ErrorCode.UserNameExists => ErrorCategory.Business,
            ErrorCode.EmailExists => ErrorCategory.Business,
            ErrorCode.PatientIdCardExists => ErrorCategory.Business,
            ErrorCode.PatientPhoneExists => ErrorCategory.Business,
            ErrorCode.DuplicateMedicalCase => ErrorCategory.Business,
            ErrorCode.HerbNameExists => ErrorCategory.Business,
            ErrorCode.FormulaNameExists => ErrorCategory.Business,
            ErrorCode.InvalidPatientStatus => ErrorCategory.Business,
            ErrorCode.McActiveCaseExists => ErrorCategory.Business,
            ErrorCode.McSuspendedCaseExists => ErrorCategory.Business,
            ErrorCode.McPatientDisabled => ErrorCategory.Business,
            ErrorCode.McInvalidStatusTransition => ErrorCategory.Business,
            ErrorCode.McPrescriptionFlagRequired => ErrorCategory.Business,
            ErrorCode.McPrescriptionRequired => ErrorCategory.Business,
            ErrorCode.McCompletedCannotSuspend => ErrorCategory.Business,
            ErrorCode.McDeletedCannotSuspend => ErrorCategory.Business,
            ErrorCode.McCompletedCannotCancel => ErrorCategory.Business,
            ErrorCode.McAlreadyDeleted => ErrorCategory.Business,
            ErrorCode.McCancelReasonRequired => ErrorCategory.Business,
            ErrorCode.McPrescriptionItemsRequired => ErrorCategory.Business,
            ErrorCode.McPrescriptionFlagNotSet => ErrorCategory.Business,
            ErrorCode.McPrescriptionAlreadyExists => ErrorCategory.Business,
            ErrorCode.McPrintedRequiresReason => ErrorCategory.Business,
            ErrorCode.McPrintedCannotDelete => ErrorCategory.Business,
            ErrorCode.InvalidMedicalCaseState => ErrorCategory.Business,
            ErrorCode.InvalidPrescriptionState => ErrorCategory.Business,
            ErrorCode.PatientHasActiveCases => ErrorCategory.Business,
            ErrorCode.MedicalCaseArchived => ErrorCategory.Business,
            ErrorCode.MedicalCaseMissingDiagnosis => ErrorCategory.Business,
            ErrorCode.MedicalCaseHasPrescriptions => ErrorCategory.Business,
            ErrorCode.PrescriptionAlreadyDispensed => ErrorCategory.Business,
            ErrorCode.PrescriptionDosageExceeded => ErrorCategory.Business,
            ErrorCode.PrescriptionContraindication => ErrorCategory.Business,
            ErrorCode.PrescriptionCompleted => ErrorCategory.Business,
            ErrorCode.HerbInsufficientStock => ErrorCategory.Business,
            ErrorCode.PatientNotDeleted => ErrorCategory.Business,
            ErrorCode.HerbInUse => ErrorCategory.Business,
            ErrorCode.HerbNotDeleted => ErrorCategory.Business,
            ErrorCode.HerbBatchItemNotFound => ErrorCategory.Business,
            ErrorCode.HerbBatchItemDeletedOrMissing => ErrorCategory.Business,
            ErrorCode.HerbBatchItemError => ErrorCategory.Business,
            ErrorCode.HerbImportExcelError => ErrorCategory.Business,
            ErrorCode.HerbImportNoData => ErrorCategory.Business,
            ErrorCode.FormulaValidationFailed => ErrorCategory.Business,
            ErrorCode.FormulaInUse => ErrorCategory.Business,
            ErrorCode.FormulaNotDeleted => ErrorCategory.Business,
            ErrorCode.FormulaCreateFailed => ErrorCategory.Business,
            ErrorCode.FormulaUpdateFailed => ErrorCategory.Business,
            ErrorCode.FormulaHerbItemNotFound => ErrorCategory.Business,
            ErrorCode.FormulaHerbItemAlreadyValidated => ErrorCategory.Business,
            ErrorCode.FormulaSystemHerbNotFound => ErrorCategory.Business,
            ErrorCode.FormulaPendingValidationListFailed => ErrorCategory.Business,
            ErrorCode.FormulaBatchItemNotFound => ErrorCategory.Business,
            ErrorCode.FormulaBatchItemError => ErrorCategory.Business,
            ErrorCode.PasswordChangeRequired => ErrorCategory.Business,
            ErrorCode.RateLimitExceeded => ErrorCategory.Business,
            ErrorCode.SyncPatientNotFound => ErrorCategory.Business,
            ErrorCode.SyncHerbNotFound => ErrorCategory.Business,
            ErrorCode.SyncCaseLocked => ErrorCategory.Business,
            ErrorCode.SyncHerbHasReference => ErrorCategory.Business,
            ErrorCode.SyncPatientHasReference => ErrorCategory.Business,
            ErrorCode.SyncNoEntityTypeSelected => ErrorCategory.Business,
            ErrorCode.SyncFailed => ErrorCategory.Business,
            ErrorCode.SyncChecksumTypeError => ErrorCategory.Business,
            ErrorCode.SyncDependencyNotSynced => ErrorCategory.Business,
            ErrorCode.SyncPatientRemapFailed => ErrorCategory.Business,
            ErrorCode.SyncLocalActiveCasesExist => ErrorCategory.Business,
            ErrorCode.RegistrationInvalidStatusTransition => ErrorCategory.Business,
            ErrorCode.RegistrationCancelNotAllowed => ErrorCategory.Business,
            ErrorCode.RegistrationPatientDisabled => ErrorCategory.Business,
            ErrorCode.RegistrationDoctorNotAvailable => ErrorCategory.Business,
            ErrorCode.RegistrationDuplicateWaiting => ErrorCategory.Business,

            // 系统错误
            ErrorCode.InternalError => ErrorCategory.System,
            ErrorCode.DatabaseError => ErrorCategory.System,
            ErrorCode.ServiceUnavailable => ErrorCategory.System,
            ErrorCode.Timeout => ErrorCategory.System,
            ErrorCode.McPrescriptionCreateRetryFailed => ErrorCategory.System,
            ErrorCode.McSaveRetryFailed => ErrorCategory.System,
            ErrorCode.McConsultationNotFound => ErrorCategory.System,
            ErrorCode.HerbUploadFailed => ErrorCategory.System,
            ErrorCode.PatientUploadFailed => ErrorCategory.System,
            ErrorCode.FormulaUploadFailed => ErrorCategory.System,
            ErrorCode.MedicalCaseUploadFailed => ErrorCategory.System,
            ErrorCode.SyncReferenceCheckFailed => ErrorCategory.System,

            // 配置错误
            ErrorCode.ConfigurationError => ErrorCategory.Configuration,

            // 默认
            _ => ErrorCategory.General
        };
    }

    /// <summary>
    /// 获取错误码所属的模块名称
    /// </summary>
    public static string GetModuleName(this ErrorCode errorCode)
    {
        var code = (int)errorCode;

        return code switch
        {
            < 10000 => "General",
            < 20000 => "Users",
            < 30000 => "Patients",
            < 40000 => "MedicalCase",
            < 50000 => "Prescriptions",
            < 60000 => "Herbs",
            < 70000 => "Formula",
            < 80000 => "Sync",
            < 90000 => "Registration",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 格式化错误码为字符串（如 "ERR-30001"）
    /// </summary>
    public static string ToFormattedString(this ErrorCode errorCode)
    {
        return $"ERR-{(int)errorCode:D5}";
    }
}
