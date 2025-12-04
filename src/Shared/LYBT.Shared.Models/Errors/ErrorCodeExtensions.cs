namespace LYBT.Shared.Models.Errors;

/// <summary>
/// ErrorCode扩展方法
/// refactor-logging-system: 提供错误码到HTTP状态码、错误类别的映射
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
            ErrorCode.ConsultationNoSymptoms => 400,
            ErrorCode.ConsultationIncomplete => 400,
            ErrorCode.HerbInvalidPrice => 400,

            // 401 Unauthorized - 认证错误
            ErrorCode.Unauthorized => 401,
            ErrorCode.InvalidPassword => 401,
            ErrorCode.CredentialsExpired => 401,
            ErrorCode.InvalidRefreshToken => 401,

            // 403 Forbidden - 授权错误
            ErrorCode.Forbidden => 403,
            ErrorCode.UserDisabled => 403,
            ErrorCode.UserLocked => 403,
            ErrorCode.PatientDisabled => 403,
            ErrorCode.HerbDisabled => 403,
            ErrorCode.FormulaDisabled => 403,
            ErrorCode.CannotDeleteSysAdmin => 403,

            // 404 Not Found - 资源未找到
            ErrorCode.NotFound => 404,
            ErrorCode.UserNotFound => 404,
            ErrorCode.RoleNotFound => 404,
            ErrorCode.PatientNotFound => 404,
            ErrorCode.MedicalCaseNotFound => 404,
            ErrorCode.PrescriptionNotFound => 404,
            ErrorCode.HerbNotFound => 404,
            ErrorCode.FormulaNotFound => 404,
            ErrorCode.ConsultationNotFound => 404,

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

            // 422 Unprocessable Entity - 业务规则违反
            ErrorCode.InvalidPatientStatus => 422,
            ErrorCode.InvalidMedicalCaseState => 422,
            ErrorCode.InvalidPrescriptionState => 422,
            ErrorCode.InvalidConsultationState => 422,
            ErrorCode.PatientHasActiveCases => 422,
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
            ErrorCode.ConsultationCompleted => 422,
            ErrorCode.PasswordChangeRequired => 422,

            // 429 Too Many Requests
            ErrorCode.RateLimitExceeded => 429,

            // 503 Service Unavailable
            ErrorCode.ServiceUnavailable => 503,
            ErrorCode.Timeout => 503,

            // 500 Internal Server Error - 默认
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
            ErrorCode.ConsultationNoSymptoms => ErrorCategory.Validation,
            ErrorCode.ConsultationIncomplete => ErrorCategory.Validation,
            ErrorCode.HerbInvalidPrice => ErrorCategory.Validation,

            // 认证错误
            ErrorCode.Unauthorized => ErrorCategory.Authentication,
            ErrorCode.InvalidPassword => ErrorCategory.Authentication,
            ErrorCode.CredentialsExpired => ErrorCategory.Authentication,
            ErrorCode.InvalidRefreshToken => ErrorCategory.Authentication,

            // 授权错误
            ErrorCode.Forbidden => ErrorCategory.Authorization,
            ErrorCode.UserDisabled => ErrorCategory.Authorization,
            ErrorCode.UserLocked => ErrorCategory.Authorization,
            ErrorCode.PatientDisabled => ErrorCategory.Authorization,
            ErrorCode.HerbDisabled => ErrorCategory.Authorization,
            ErrorCode.FormulaDisabled => ErrorCategory.Authorization,
            ErrorCode.CannotDeleteSysAdmin => ErrorCategory.Authorization,

            // 资源错误
            ErrorCode.NotFound => ErrorCategory.Resource,
            ErrorCode.UserNotFound => ErrorCategory.Resource,
            ErrorCode.RoleNotFound => ErrorCategory.Resource,
            ErrorCode.PatientNotFound => ErrorCategory.Resource,
            ErrorCode.MedicalCaseNotFound => ErrorCategory.Resource,
            ErrorCode.PrescriptionNotFound => ErrorCategory.Resource,
            ErrorCode.HerbNotFound => ErrorCategory.Resource,
            ErrorCode.FormulaNotFound => ErrorCategory.Resource,
            ErrorCode.ConsultationNotFound => ErrorCategory.Resource,

            // 并发错误
            ErrorCode.ConcurrencyConflict => ErrorCategory.Concurrency,
            ErrorCode.MedicalCaseVersionConflict => ErrorCategory.Concurrency,
            ErrorCode.MedicalCaseLocked => ErrorCategory.Concurrency,

            // 业务逻辑错误
            ErrorCode.UserNameExists => ErrorCategory.Business,
            ErrorCode.EmailExists => ErrorCategory.Business,
            ErrorCode.PatientIdCardExists => ErrorCategory.Business,
            ErrorCode.PatientPhoneExists => ErrorCategory.Business,
            ErrorCode.DuplicateMedicalCase => ErrorCategory.Business,
            ErrorCode.HerbNameExists => ErrorCategory.Business,
            ErrorCode.FormulaNameExists => ErrorCategory.Business,
            ErrorCode.InvalidPatientStatus => ErrorCategory.Business,
            ErrorCode.InvalidMedicalCaseState => ErrorCategory.Business,
            ErrorCode.InvalidPrescriptionState => ErrorCategory.Business,
            ErrorCode.InvalidConsultationState => ErrorCategory.Business,
            ErrorCode.PatientHasActiveCases => ErrorCategory.Business,
            ErrorCode.MedicalCaseArchived => ErrorCategory.Business,
            ErrorCode.MedicalCaseMissingDiagnosis => ErrorCategory.Business,
            ErrorCode.MedicalCaseHasPrescriptions => ErrorCategory.Business,
            ErrorCode.PrescriptionAlreadyDispensed => ErrorCategory.Business,
            ErrorCode.PrescriptionDosageExceeded => ErrorCategory.Business,
            ErrorCode.PrescriptionContraindication => ErrorCategory.Business,
            ErrorCode.PrescriptionCompleted => ErrorCategory.Business,
            ErrorCode.HerbInsufficientStock => ErrorCategory.Business,
            ErrorCode.HerbInUse => ErrorCategory.Business,
            ErrorCode.FormulaValidationFailed => ErrorCategory.Business,
            ErrorCode.FormulaInUse => ErrorCategory.Business,
            ErrorCode.ConsultationCompleted => ErrorCategory.Business,
            ErrorCode.PasswordChangeRequired => ErrorCategory.Business,
            ErrorCode.RateLimitExceeded => ErrorCategory.Business,

            // 系统错误
            ErrorCode.InternalError => ErrorCategory.System,
            ErrorCode.DatabaseError => ErrorCategory.System,
            ErrorCode.ServiceUnavailable => ErrorCategory.System,
            ErrorCode.Timeout => ErrorCategory.System,

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
            < 80000 => "Consultation",
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
