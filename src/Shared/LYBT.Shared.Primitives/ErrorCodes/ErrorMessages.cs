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
        [ErrorCode.TokenExpired] = ("Access Token 已过期，请刷新令牌", "Access token expired"),
        [ErrorCode.DeviceMismatch] = ("登录设备不匹配", "Device mismatch"),
        [ErrorCode.PasswordChangeRequired] = ("首次登录需要修改密码", "Password change required"),
        [ErrorCode.SessionExpired] = ("会话已到期，请重新登录", "Session expired"),
        // Auth MCCEE 码
        [ErrorCode.AuthInvalidCredentials] = ("用户名或密码错误", "Invalid credentials"),
        [ErrorCode.AuthTokenInvalid] = ("登录凭据无效", "Token is invalid"),
        [ErrorCode.AuthTokenRevoked] = ("登录已失效，请重新登录", "Token has been revoked"),
        [ErrorCode.AuthRefreshTokenExpired] = ("会话已过期，请重新登录", "Refresh token expired"),
        [ErrorCode.AuthRefreshTokenInvalid] = ("刷新凭据无效", "Refresh token is invalid"),
        [ErrorCode.AuthConcurrentSessionLimit] = ("登录设备数超过限制", "Concurrent session limit exceeded"),

        // 患者模块 (2xxxx)
        [ErrorCode.PatientNotFound] = ("患者信息不存在", "Patient not found"),
        [ErrorCode.PatientIdCardExists] = ("身份证号已被使用", "Patient ID card exists"),
        [ErrorCode.PatientPhoneExists] = ("手机号已被使用", "Patient phone exists"),
        [ErrorCode.PatientHasActiveCases] = ("患者有关联的医案，无法删除", "Patient has referenced cases"),
        [ErrorCode.PatientDisabled] = ("患者档案已停用", "Patient is disabled"),
        [ErrorCode.InvalidPatientStatus] = ("无效的患者状态", "Invalid patient status"),
        // 207xx: 业务规则错误
        [ErrorCode.PatientPhoneDuplicate] = ("手机号已存在", "Phone number already exists"),
        [ErrorCode.PatientNotDeleted] = ("该患者未被删除，无需恢复", "Patient is not deleted, no need to restore"),
        [ErrorCode.PatientBatchOperationEmpty] = ("请至少选择一个患者", "Please select at least one patient"),
        [ErrorCode.PatientBatchCheckExceeded] = ("批量检查最多支持100条记录", "Batch check limit exceeded (max 100)"),
        [ErrorCode.PatientInvalidPagination] = ("页码和页大小参数无效", "Invalid pagination parameters"),
        // 208xx: 导入错误
        [ErrorCode.PatientImportFileEmpty] = ("文件不能为空", "Import file is empty"),
        [ErrorCode.PatientImportFileFormat] = ("仅支持.xlsx格式的Excel文件", "Only .xlsx format is supported"),
        [ErrorCode.PatientImportFileSize] = ("文件大小不能超过10MB", "File size exceeds 10MB limit"),
        [ErrorCode.PatientImportNoWorksheet] = ("Excel文件中没有工作表", "No worksheet in Excel file"),
        [ErrorCode.PatientImportRowExceeded] = ("导入数据超过限制（最大1000行）", "Import row limit exceeded (max 1000)"),

        // 医案模块 (3xxxx)
        [ErrorCode.MedicalCaseNotFound] = ("医案不存在", "Medical case not found"),
        [ErrorCode.InvalidMedicalCaseState] = ("医案状态不允许此操作", "Invalid medical case state"),
        [ErrorCode.MedicalCaseArchived] = ("医案已归档，无法修改", "Medical case is archived"),
        [ErrorCode.MedicalCaseLocked] = ("医案正在被其他用户编辑", "Medical case is locked"),
        [ErrorCode.MedicalCaseVersionConflict] = ("医案数据已被其他用户修改，请刷新页面后重试", "Medical case version conflict"),
        [ErrorCode.DuplicateMedicalCase] = ("无法创建重复医案", "Duplicate medical case"),
        [ErrorCode.MedicalCaseMissingDiagnosis] = ("医案缺少必要的诊断信息", "Medical case missing diagnosis"),
        [ErrorCode.MedicalCaseHasPrescriptions] = ("无法删除有处方的医案", "Medical case has prescriptions"),
        // 301xx: 创建医案错误
        [ErrorCode.McPatientNotFound] = ("患者不存在", "Patient not found"),
        [ErrorCode.McDoctorNotFound] = ("医生不存在", "Doctor not found"),
        [ErrorCode.McActiveCaseExists] = ("该患者已有进行中的医案，请先完成现有医案", "Patient already has an active medical case"),
        [ErrorCode.McSuspendedCaseExists] = ("该患者已有挂起的医案，请先处理现有医案", "Patient already has a suspended medical case"),
        [ErrorCode.McPatientDisabled] = ("该患者已被禁用，无法创建医案", "Patient is disabled, cannot create medical case"),
        // 302xx: 权限错误
        [ErrorCode.McCannotEditCase] = ("无权限编辑此医案", "No permission to edit this medical case"),
        [ErrorCode.McCannotDeleteCase] = ("无权限删除此医案", "No permission to delete this medical case"),
        [ErrorCode.McCannotCancelCase] = ("无权限取消此医案", "No permission to cancel this medical case"),
        [ErrorCode.McCannotDeletePrescription] = ("无权限删除此医案的处方", "No permission to delete prescription"),
        [ErrorCode.McCannotSuspendCase] = ("无权限挂起此医案", "No permission to suspend this medical case"),
        // 303xx: 状态转换错误
        [ErrorCode.McInvalidStatusTransition] = ("不允许的状态转换", "Invalid status transition"),
        [ErrorCode.McPrescriptionFlagRequired] = ("请先标记是否需要开处方", "Please mark prescription requirement first"),
        [ErrorCode.McPrescriptionRequired] = ("已标记需要开处方，但处方不存在，无法完成医案", "Prescription required but not found"),
        [ErrorCode.McCompletedCannotSuspend] = ("已完成的医案不可挂起", "Completed case cannot be suspended"),
        [ErrorCode.McDeletedCannotSuspend] = ("已删除的医案不可挂起", "Deleted case cannot be suspended"),
        [ErrorCode.McCompletedCannotCancel] = ("已完成的医案不可取消", "Completed case cannot be cancelled"),
        [ErrorCode.McAlreadyDeleted] = ("医案已经是删除状态", "Medical case is already deleted"),
        // 304xx: 处方错误
        [ErrorCode.McPrescriptionFlagNotSet] = ("未标记需要开处方，请先设置处方需求标记", "Prescription flag not set"),
        [ErrorCode.McPrescriptionAlreadyExists] = ("医案已存在处方，请使用更新接口", "Prescription already exists, use update endpoint"),
        [ErrorCode.McPrintedRequiresReason] = ("医案已打印，修改需要提供修改原因", "Printed case requires edit reason"),
        [ErrorCode.McPrintedCannotDelete] = ("医案已打印，不允许删除处方", "Cannot delete prescription of printed case"),
        [ErrorCode.McConsultationNotFound] = ("诊断记录不存在", "Consultation not found"),
        // 305xx: 并发和系统错误
        [ErrorCode.McPrescriptionCreateRetryFailed] = ("创建处方失败，请稍后重试", "Prescription creation retry failed"),
        [ErrorCode.McSaveRetryFailed] = ("保存失败，请稍后重试", "Save retry failed"),
        // 306xx: 参数验证错误
        [ErrorCode.McRequestIdMismatch] = ("请求ID与路由ID不匹配", "Request ID mismatch"),
        [ErrorCode.McInvalidPagination] = ("页码和页大小参数无效", "Invalid pagination parameters"),
        [ErrorCode.McBatchQueryExceeded] = ("单次最多查询50个医案", "Batch query limit exceeded (max 50)"),
        [ErrorCode.McBatchOperationEmpty] = ("请至少选择一个医案", "Please select at least one medical case"),
        [ErrorCode.McInvalidPatientId] = ("患者ID无效", "Invalid patient ID"),
        [ErrorCode.McInvalidCountParam] = ("返回数量参数无效（1-50）", "Invalid count parameter (1-50)"),
        [ErrorCode.McCaseNotFound] = ("医案不存在", "Medical case not found"),

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
        [ErrorCode.HerbValidationFailed] = ("药材验证失败", "Herb validation failed"),
        [ErrorCode.HerbNoPermission] = ("您没有权限操作此药材，只能操作自己创建的数据", "No permission to operate this herb"),
        [ErrorCode.HerbNotDeleted] = ("该药材未被删除，无需恢复", "Herb is not deleted, no need to restore"),
        [ErrorCode.HerbInvalidPagination] = ("页码和页大小参数无效", "Invalid pagination parameters"),
        [ErrorCode.HerbBatchEmpty] = ("请至少选择一个药材", "Please select at least one herb"),
        [ErrorCode.HerbBatchImportExceeded] = ("批量导入最多支持10000条记录", "Batch import limit exceeded (max 10000)"),
        [ErrorCode.HerbBatchCheckExceeded] = ("批量检查最多支持100条记录", "Batch check limit exceeded (max 100)"),
        [ErrorCode.HerbBatchItemNotFound] = ("药材不存在", "Herb not found in batch operation"),
        [ErrorCode.HerbBatchItemDeletedOrMissing] = ("药材不存在或已删除", "Herb not found or deleted"),
        [ErrorCode.HerbBatchItemError] = ("操作失败", "Batch item operation failed"),
        [ErrorCode.HerbImportFileEmpty] = ("文件不能为空", "Import file is empty"),
        [ErrorCode.HerbImportFileFormat] = ("仅支持.xlsx格式的Excel文件", "Only .xlsx format is supported"),
        [ErrorCode.HerbImportFileSize] = ("文件大小不能超过10MB", "File size exceeds 10MB limit"),
        [ErrorCode.HerbImportExcelError] = ("Excel文件格式错误", "Excel file format error"),
        [ErrorCode.HerbImportNoData] = ("Excel文件中没有数据行", "No data rows in Excel file"),

        // 方剂模块 (6xxxx)
        [ErrorCode.FormulaNotFound] = ("方剂不存在", "Formula not found"),
        [ErrorCode.FormulaNameExists] = ("方剂名称已存在", "Formula name exists"),
        [ErrorCode.FormulaNoHerbs] = ("方剂草药为空", "Formula has no herbs"),
        [ErrorCode.FormulaValidationFailed] = ("方剂验证失败", "Formula validation failed"),
        [ErrorCode.FormulaInUse] = ("无法删除已使用的方剂", "Formula is in use"),
        [ErrorCode.FormulaDisabled] = ("方剂已停用", "Formula is disabled"),
        // 601xx: 核心错误
        [ErrorCode.FormulaIdInvalid] = ("验方ID不能为空", "Formula ID cannot be empty"),
        [ErrorCode.FormulaNoPermission] = ("您没有权限操作此验方，只能操作自己创建的数据", "No permission to operate this formula"),
        [ErrorCode.FormulaCreateFailed] = ("新增验方失败", "Failed to create formula"),
        [ErrorCode.FormulaUpdateFailed] = ("更新验方失败", "Failed to update formula"),
        [ErrorCode.FormulaDeleteFailed] = ("验方不存在", "Formula not found for deletion"),
        [ErrorCode.FormulaNotDeleted] = ("该验方未被删除，无需恢复", "Formula is not deleted, no need to restore"),
        [ErrorCode.FormulaInvalidPagination] = ("页码和页大小参数无效", "Invalid pagination parameters"),
        // 602xx: 药材验证错误
        [ErrorCode.FormulaHerbItemIdInvalid] = ("参数不能为空", "Parameter cannot be empty"),
        [ErrorCode.FormulaHerbItemNotFound] = ("药材项不存在", "Formula herb item not found"),
        [ErrorCode.FormulaHerbItemAlreadyValidated] = ("该药材已校验，无需重复操作", "Herb item already validated"),
        [ErrorCode.FormulaSystemHerbNotFound] = ("所选药材不存在", "Selected system herb not found"),
        [ErrorCode.FormulaPendingValidationListFailed] = ("获取待校验验方列表失败", "Failed to get pending validation list"),
        // 603xx: 批量操作错误
        [ErrorCode.FormulaBatchEmpty] = ("请至少选择一个方剂", "Please select at least one formula"),
        [ErrorCode.FormulaBatchImportEmpty] = ("导入数据不能为空", "Import data cannot be empty"),
        [ErrorCode.FormulaBatchItemNotFound] = ("方剂不存在", "Formula not found in batch operation"),
        [ErrorCode.FormulaBatchItemError] = ("操作失败", "Batch item operation failed"),

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
