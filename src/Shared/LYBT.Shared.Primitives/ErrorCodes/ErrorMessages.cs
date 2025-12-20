namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// 错误消息映射 - 提供中英文错误消息
/// consolidate-exception-handling: 统一错误消息管理
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<ErrorCode, (string Zh, string En)> Messages = new()
    {
        // 通用错误 (0xxxx)
        [ErrorCode.Unknown] = ("未知错误", "Unknown error"),
        [ErrorCode.InvalidRequest] = ("请求参数无效", "Invalid request parameters"),
        [ErrorCode.NotFound] = ("请求的资源不存在", "Resource not found"),
        [ErrorCode.ValidationFailed] = ("输入数据验证失败，请检查后重试", "Validation failed"),
        [ErrorCode.Unauthorized] = ("请先登录后再访问此资源", "Unauthorized access"),
        [ErrorCode.Forbidden] = ("您没有权限执行此操作", "Access forbidden"),
        [ErrorCode.ConcurrencyConflict] = ("数据已被其他用户修改，请刷新后重试", "Concurrency conflict detected"),
        [ErrorCode.Timeout] = ("操作超时，请稍后重试", "Operation timed out"),
        [ErrorCode.ServiceUnavailable] = ("服务暂时不可用，请稍后重试", "Service unavailable"),
        [ErrorCode.InternalError] = ("系统处理异常，请稍后重试", "Internal server error"),
        [ErrorCode.DatabaseError] = ("数据库操作失败，请稍后重试", "Database error"),
        [ErrorCode.ConfigurationError] = ("系统配置错误，请联系管理员", "Configuration error"),
        [ErrorCode.RateLimitExceeded] = ("请求过于频繁，请稍后重试", "Rate limit exceeded"),

        // 用户模块 (1xxxx)
        [ErrorCode.UserNotFound] = ("用户不存在", "User not found"),
        [ErrorCode.UserNameExists] = ("用户名已被使用", "Username already exists"),
        [ErrorCode.EmailExists] = ("邮箱已被使用", "Email already exists"),
        [ErrorCode.InvalidPassword] = ("用户名或密码错误", "Invalid password"),
        [ErrorCode.PasswordPolicyViolation] = ("密码不符合策略要求", "Password policy violation"),
        [ErrorCode.UserDisabled] = ("用户账号已被禁用，请联系管理员", "User account is disabled"),
        [ErrorCode.UserLocked] = ("账号已被锁定，请稍后重试", "User account is locked"),
        [ErrorCode.CredentialsExpired] = ("您的登录已过期，请重新登录", "Credentials expired"),
        [ErrorCode.InvalidRefreshToken] = ("登录状态异常，请重新登录", "Invalid refresh token"),
        [ErrorCode.RoleNotFound] = ("角色不存在", "Role not found"),
        [ErrorCode.CannotDeleteSysAdmin] = ("无法删除系统管理员", "Cannot delete system admin"),
        [ErrorCode.PasswordChangeRequired] = ("首次登录需要修改密码", "Password change required"),

        // 患者模块 (2xxxx)
        [ErrorCode.PatientNotFound] = ("患者信息不存在", "Patient not found"),
        [ErrorCode.PatientIdCardExists] = ("身份证号已被使用", "Patient ID card exists"),
        [ErrorCode.PatientPhoneExists] = ("手机号已被使用", "Patient phone exists"),
        [ErrorCode.PatientHasActiveCases] = ("患者有未完成的病例", "Patient has active cases"),
        [ErrorCode.PatientDisabled] = ("患者档案已停用", "Patient is disabled"),
        [ErrorCode.InvalidPatientStatus] = ("无效的患者状态", "Invalid patient status"),

        // 病历模块 (3xxxx)
        [ErrorCode.MedicalCaseNotFound] = ("病历不存在", "Medical case not found"),
        [ErrorCode.InvalidMedicalCaseState] = ("病历状态不允许此操作", "Invalid medical case state"),
        [ErrorCode.MedicalCaseArchived] = ("病历已归档，无法修改", "Medical case is archived"),
        [ErrorCode.MedicalCaseLocked] = ("病历正在被其他用户编辑", "Medical case is locked"),
        [ErrorCode.MedicalCaseVersionConflict] = ("病历数据已被其他用户修改，请刷新页面后重试", "Medical case version conflict"),
        [ErrorCode.DuplicateMedicalCase] = ("无法创建重复病例", "Duplicate medical case"),
        [ErrorCode.MedicalCaseMissingDiagnosis] = ("病例缺少必要的诊断信息", "Medical case missing diagnosis"),
        [ErrorCode.MedicalCaseHasPrescriptions] = ("无法删除有处方的病例", "Medical case has prescriptions"),

        // 处方模块 (4xxxx)
        [ErrorCode.PrescriptionNotFound] = ("处方不存在", "Prescription not found"),
        [ErrorCode.InvalidPrescriptionState] = ("处方状态不允许此操作", "Invalid prescription state"),
        [ErrorCode.PrescriptionAlreadyDispensed] = ("处方已发药，无法修改", "Prescription already dispensed"),
        [ErrorCode.PrescriptionNoHerbs] = ("处方草药为空", "Prescription has no herbs"),
        [ErrorCode.PrescriptionDosageExceeded] = ("处方剂量超出限制", "Prescription dosage exceeded"),
        [ErrorCode.PrescriptionContraindication] = ("处方包含禁忌配伍", "Prescription contraindication"),
        [ErrorCode.PrescriptionCompleted] = ("无法修改已完成的处方", "Prescription is completed"),

        // 药材模块 (5xxxx)
        [ErrorCode.HerbNotFound] = ("药材不存在", "Herb not found"),
        [ErrorCode.HerbNameExists] = ("药材名称已存在", "Herb name exists"),
        [ErrorCode.HerbInsufficientStock] = ("药材库存不足", "Insufficient herb stock"),
        [ErrorCode.HerbDisabled] = ("药材已停用", "Herb is disabled"),
        [ErrorCode.HerbInUse] = ("无法删除已使用的药材", "Herb is in use"),
        [ErrorCode.HerbInvalidPrice] = ("药材价格无效", "Invalid herb price"),

        // 方剂模块 (6xxxx)
        [ErrorCode.FormulaNotFound] = ("方剂不存在", "Formula not found"),
        [ErrorCode.FormulaNameExists] = ("方剂名称已存在", "Formula name exists"),
        [ErrorCode.FormulaNoHerbs] = ("方剂草药为空", "Formula has no herbs"),
        [ErrorCode.FormulaValidationFailed] = ("方剂验证失败", "Formula validation failed"),
        [ErrorCode.FormulaInUse] = ("无法删除已使用的方剂", "Formula is in use"),
        [ErrorCode.FormulaDisabled] = ("方剂已停用", "Formula is disabled"),

        // 诊断模块 (7xxxx)
        [ErrorCode.ConsultationNotFound] = ("诊断记录不存在", "Consultation not found"),
        [ErrorCode.InvalidConsultationState] = ("问诊状态不允许此操作", "Invalid consultation state"),
        [ErrorCode.ConsultationCompleted] = ("问诊已完成，无法修改", "Consultation is completed"),
        [ErrorCode.ConsultationIncomplete] = ("问诊数据不完整", "Consultation is incomplete"),
        [ErrorCode.ConsultationNoSymptoms] = ("症状描述为空", "Consultation has no symptoms")
    };

    /// <summary>
    /// 获取错误消息
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="useEnglish">是否使用英文</param>
    /// <returns>错误消息</returns>
    public static string Get(ErrorCode code, bool useEnglish = false)
    {
        if (Messages.TryGetValue(code, out var msg))
            return useEnglish ? msg.En : msg.Zh;
        return code.ToString();
    }

    /// <summary>
    /// 获取格式化的错误消息
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="useEnglish">是否使用英文</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的错误消息</returns>
    public static string GetFormatted(ErrorCode code, bool useEnglish = false, params object[] args)
    {
        var template = Get(code, useEnglish);
        try
        {
            return args.Length > 0 ? string.Format(template, args) : template;
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>
    /// 获取用户友好消息（中文）
    /// </summary>
    public static string GetUserMessage(ErrorCode code) => Get(code, useEnglish: false);

    /// <summary>
    /// 获取技术消息（英文）
    /// </summary>
    public static string GetTechnicalMessage(ErrorCode code) => Get(code, useEnglish: true);

    /// <summary>
    /// 获取英文消息（别名）
    /// </summary>
    public static string GetEnglish(ErrorCode code) => Get(code, useEnglish: true);
}
