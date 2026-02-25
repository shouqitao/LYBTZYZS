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
        [ErrorCode.PatientHasActiveCases] = ("患者有关联的医案，无法删除", "Patient has referenced cases"),
        [ErrorCode.PatientDisabled] = ("患者档案已停用", "Patient is disabled"),
        [ErrorCode.InvalidPatientStatus] = ("无效的患者状态", "Invalid patient status"),

        // 医案模块 (3xxxx)
        [ErrorCode.MedicalCaseNotFound] = ("医案不存在", "Medical case not found"),
        [ErrorCode.InvalidMedicalCaseState] = ("医案状态不允许此操作", "Invalid medical case state"),
        [ErrorCode.MedicalCaseArchived] = ("医案已归档，无法修改", "Medical case is archived"),
        [ErrorCode.MedicalCaseLocked] = ("医案正在被其他用户编辑", "Medical case is locked"),
        [ErrorCode.MedicalCaseVersionConflict] = ("医案数据已被其他用户修改，请刷新页面后重试", "Medical case version conflict"),
        [ErrorCode.DuplicateMedicalCase] = ("无法创建重复医案", "Duplicate medical case"),
        [ErrorCode.MedicalCaseMissingDiagnosis] = ("医案缺少必要的诊断信息", "Medical case missing diagnosis"),
        [ErrorCode.MedicalCaseHasPrescriptions] = ("无法删除有处方的医案", "Medical case has prescriptions"),

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
        [ErrorCode.HerbNotDeleted] = ("该药材未被删除，无需恢复", "Herb is not deleted, no need to restore"),
        [ErrorCode.HerbInvalidPagination] = ("页码和页大小参数无效", "Invalid pagination parameters"),
        [ErrorCode.HerbBatchImportExceeded] = ("批量导入最多支持10000条记录", "Batch import limit exceeded (max 10000)"),

        // 方剂模块 (6xxxx)
        [ErrorCode.FormulaNotFound] = ("方剂不存在", "Formula not found"),
        [ErrorCode.FormulaNameExists] = ("方剂名称已存在", "Formula name exists"),
        [ErrorCode.FormulaNoHerbs] = ("方剂草药为空", "Formula has no herbs"),
        [ErrorCode.FormulaValidationFailed] = ("方剂验证失败", "Formula validation failed"),
        [ErrorCode.FormulaInUse] = ("无法删除已使用的方剂", "Formula is in use"),
        [ErrorCode.FormulaDisabled] = ("方剂已停用", "Formula is disabled"),

        // 同步模块 (7xxxx)
        // 701xx: 服务端通用错误
        [ErrorCode.UnsupportedEntityType] = ("不支持的实体类型", "Unsupported entity type"),
        [ErrorCode.JsonDeserializeFailed] = ("JSON 反序列化失败", "JSON deserialization failed"),
        [ErrorCode.SyncDataConflict] = ("服务器已存在该数据", "Server data conflict"),
        // 702xx: 服务端上传错误
        [ErrorCode.HerbUploadFailed] = ("药材上传失败", "Herb upload failed"),
        [ErrorCode.PatientUploadFailed] = ("患者上传失败", "Patient upload failed"),
        [ErrorCode.FormulaUploadFailed] = ("验方上传失败", "Formula upload failed"),
        [ErrorCode.MedicalCaseUploadFailed] = ("医案上传失败", "Medical case upload failed"),
        // 703xx: MedicalCase 同步错误
        [ErrorCode.SyncPatientNotFound] = ("患者不存在，请先同步患者", "Sync patient not found"),
        [ErrorCode.SyncHerbNotFound] = ("药材不存在，请先同步药材", "Sync herb not found"),
        [ErrorCode.SyncCaseLocked] = ("医案已完成且已锁定，无法通过同步覆盖", "Medical case is locked, cannot overwrite via sync"),
        // 704xx: 同步删除错误
        [ErrorCode.SyncReferenceCheckFailed] = ("无法检查引用关系", "Sync reference check failed"),
        [ErrorCode.SyncHerbHasReference] = ("药材被处方引用，请先禁用", "Herb has prescription references"),
        [ErrorCode.SyncPatientHasReference] = ("患者有医案记录，请先禁用", "Patient has medical case references"),
        [ErrorCode.SyncEntityNotFound] = ("实体不存在或已删除", "Sync entity not found or deleted"),
        // 705xx: 客户端错误
        [ErrorCode.SyncNoEntityTypeSelected] = ("请选择要同步的数据类型", "No entity type selected for sync"),
        [ErrorCode.SyncFailed] = ("同步失败", "Sync failed"),
        [ErrorCode.SyncChecksumTypeError] = ("不支持的 Checksum 实体类型", "Unsupported checksum entity type"),
        [ErrorCode.SyncDependencyNotSynced] = ("请先同步药材和患者数据", "Sync dependencies not satisfied"),
        [ErrorCode.SyncPatientRemapFailed] = ("无法匹配患者，请手动处理", "Patient remap failed"),
        [ErrorCode.SyncLocalActiveCasesExist] = ("本地有未完成的医案，请先完成或取消后再切换模式", "Local active cases exist, complete or cancel before switching mode")
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
