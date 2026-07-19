namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// 错误消息映射 - 提供中英文错误消息
/// consolidate-exception-handling: 统一错误消息管理
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<ErrorCode, (string Zh, string En)> Messages = new()
    {
        // 0xxxx - 通用错误
        [ErrorCode.Unknown] = ("未知错误", "Unknown error"),
        [ErrorCode.InvalidRequest] = ("请求参数无效", "Invalid request"),
        [ErrorCode.NotFound] = ("资源未找到", "Resource not found"),
        [ErrorCode.ValidationFailed] = ("验证失败", "Validation failed"),
        [ErrorCode.Unauthorized] = ("未授权访问", "Unauthorized"),
        [ErrorCode.Forbidden] = ("禁止访问", "Forbidden"),
        [ErrorCode.ConcurrencyConflict] = ("并发冲突", "Concurrency conflict"),
        [ErrorCode.Timeout] = ("操作超时", "Operation timeout"),
        [ErrorCode.ServiceUnavailable] = ("服务不可用", "Service unavailable"),
        [ErrorCode.InternalError] = ("内部服务器错误", "Internal server error"),
        [ErrorCode.DatabaseError] = ("数据库操作失败", "Database operation failed"),
        [ErrorCode.RateLimitExceeded] = ("请求频率过高", "Rate limit exceeded"),

        // 1xxxx - 用户模块
        [ErrorCode.UserNotFound] = ("用户未找到", "User not found"),
        [ErrorCode.UserNameExists] = ("用户名已存在", "Username already exists"),
        [ErrorCode.InvalidPassword] = ("密码不正确", "Invalid password"),
        [ErrorCode.UserDisabled] = ("用户已被禁用", "User is disabled"),
        [ErrorCode.UserLocked] = ("用户已被锁定", "User is locked"),
        [ErrorCode.InvalidRefreshToken] = ("刷新令牌无效或过期", "Invalid or expired refresh token"),
        [ErrorCode.CannotDeleteSysAdmin] = ("无法删除系统管理员", "Cannot delete system administrator"),
        [ErrorCode.PasswordChangeRequired] = ("需要首次登录修改密码", "Password change required on first login"),

        // 1xxxx - Auth MCCEE
        [ErrorCode.AuthInvalidCredentials] = ("凭据无效（用户名或密码错误）", "Invalid credentials"),
        [ErrorCode.AuthTokenInvalid] = ("令牌无效（格式错误或签名验证失败）", "Invalid token"),
        [ErrorCode.AuthTokenRevoked] = ("令牌已被撤销", "Token has been revoked"),
        [ErrorCode.AuthRefreshTokenExpired] = ("刷新令牌已过期", "Refresh token has expired"),
        [ErrorCode.AuthRefreshTokenInvalid] = ("刷新令牌无效", "Invalid refresh token"),
        [ErrorCode.AuthAccessTokenExpired] = ("访问令牌已过期", "Access token has expired"),
        [ErrorCode.AuthConcurrentSessionLimit] = ("并发会话数超限", "Concurrent session limit exceeded"),

        // 2xxxx - 患者模块
        [ErrorCode.PatientNotFound] = ("患者未找到", "Patient not found"),
        [ErrorCode.PatientIdCardExists] = ("患者身份证已存在", "Patient ID card already exists"),
        [ErrorCode.PatientHasActiveCases] = ("患者有关联的医案", "Patient has active medical cases"),
        [ErrorCode.PatientPhoneDuplicate] = ("手机号已存在", "Phone number already exists"),
        [ErrorCode.PatientNotDeleted] = ("该患者未被删除，无需恢复", "Patient is not deleted"),
        [ErrorCode.PatientBatchOperationEmpty] = ("批量操作时ID列表为空", "Batch operation ID list is empty"),
        [ErrorCode.PatientBatchCheckExceeded] = ("批量检查超出限制（最多100条）", "Batch check limit exceeded"),
        [ErrorCode.PatientInvalidPagination] = ("分页参数无效", "Invalid pagination parameters"),
        [ErrorCode.PatientImportFileEmpty] = ("导入文件为空", "Import file is empty"),
        [ErrorCode.PatientImportFileFormat] = ("导入文件格式不正确（仅支持.xlsx）", "Invalid file format"),
        [ErrorCode.PatientImportFileSize] = ("导入文件大小超限（最大10MB）", "File size limit exceeded"),
        [ErrorCode.PatientImportNoWorksheet] = ("Excel文件中没有工作表", "No worksheet found"),
        [ErrorCode.PatientImportRowExceeded] = ("导入数据超过限制（最大1000行）", "Import row limit exceeded"),

        // 3xxxx - 医案模块
        [ErrorCode.MedicalCaseNotFound] = ("医案未找到", "Medical case not found"),
        [ErrorCode.InvalidMedicalCaseState] = ("医案状态不允许此操作", "Invalid medical case state"),
        [ErrorCode.MedicalCaseLocked] = ("医案正在被其他用户编辑", "Medical case is locked"),
        [ErrorCode.MedicalCaseVersionConflict] = ("医案数据版本冲突", "Medical case version conflict"),
        [ErrorCode.McPatientNotFound] = ("创建医案时患者不存在", "Patient not found"),
        [ErrorCode.McDoctorNotFound] = ("创建医案时医生不存在", "Doctor not found"),
        [ErrorCode.McActiveCaseExists] = ("该患者已有进行中的医案", "Active case already exists"),
        [ErrorCode.McSuspendedCaseExists] = ("该患者已有挂起的医案", "Suspended case already exists"),
        [ErrorCode.McPatientDisabled] = ("患者已被禁用，无法创建医案", "Patient is disabled"),
        [ErrorCode.McCannotEditCase] = ("无权限编辑此医案", "No permission to edit"),
        [ErrorCode.McCannotDeleteCase] = ("无权限删除此医案", "No permission to delete"),
        [ErrorCode.McCannotCancelCase] = ("无权限取消此医案", "No permission to cancel"),
        [ErrorCode.McCannotDeletePrescription] = ("无权限删除此医案的处方", "No permission to delete prescription"),
        [ErrorCode.McCannotSuspendCase] = ("无权限挂起此医案", "No permission to suspend"),
        [ErrorCode.McInvalidStatusTransition] = ("不允许的状态转换", "Invalid status transition"),
        [ErrorCode.McPrescriptionFlagRequired] = ("完成前需标记处方需求", "Prescription flag required"),
        [ErrorCode.McPrescriptionRequired] = ("已标记需要开处方但处方不存在", "Prescription required but not found"),
        [ErrorCode.McCompletedCannotSuspend] = ("已完成的医案不可挂起", "Completed case cannot be suspended"),
        [ErrorCode.McDeletedCannotSuspend] = ("已删除的医案不可挂起", "Deleted case cannot be suspended"),
        [ErrorCode.McCompletedCannotCancel] = ("已完成的医案不可取消", "Completed case cannot be cancelled"),
        [ErrorCode.McAlreadyDeleted] = ("医案已经是删除状态", "Case is already deleted"),
        [ErrorCode.McCancelReasonRequired] = ("非当天本人取消需提供取消原因", "Cancel reason required"),
        [ErrorCode.McPrescriptionItemsRequired] = ("完成时处方明细为空", "Prescription items required"),
        [ErrorCode.MedicalCaseMissingDiagnosis] = ("医案缺少必要的诊断信息", "Missing diagnosis information"),
        [ErrorCode.McPrescriptionFlagNotSet] = ("未标记需要开处方", "Prescription flag not set"),
        [ErrorCode.McPrescriptionAlreadyExists] = ("医案已存在处方，请使用更新接口", "Prescription already exists"),
        [ErrorCode.McPrintedRequiresReason] = ("医案已打印，修改需要提供修改原因", "Printed case requires reason"),
        [ErrorCode.McPrintedCannotDelete] = ("医案已打印，不允许删除处方", "Printed case cannot delete prescription"),
        [ErrorCode.McConsultationNotFound] = ("诊断记录不存在", "Consultation not found"),
        [ErrorCode.McPrescriptionCreateRetryFailed] = ("创建处方并发重试失败", "Prescription create retry failed"),
        [ErrorCode.McSaveRetryFailed] = ("保存并发重试失败", "Save retry failed"),
        [ErrorCode.McRequestIdMismatch] = ("请求ID与路由ID不匹配", "Request ID mismatch"),
        [ErrorCode.McInvalidPagination] = ("分页参数无效", "Invalid pagination"),
        [ErrorCode.McBatchQueryExceeded] = ("批量查询超出限制（最多50个）", "Batch query limit exceeded"),
        [ErrorCode.McBatchOperationEmpty] = ("批量操作时ID列表为空", "Batch ID list is empty"),
        [ErrorCode.McInvalidPatientId] = ("患者ID无效", "Invalid patient ID"),
        [ErrorCode.McInvalidCountParam] = ("返回数量参数无效（1-50）", "Invalid count parameter"),
        [ErrorCode.McCaseNotFound] = ("医案不存在", "Medical case not found"),

        // 5xxxx - 草药模块
        [ErrorCode.HerbNotFound] = ("草药未找到", "Herb not found"),
        [ErrorCode.HerbNameExists] = ("草药名称已存在", "Herb name already exists"),
        [ErrorCode.HerbValidationFailed] = ("药材验证失败", "Herb validation failed"),
        [ErrorCode.HerbNoPermission] = ("无权限操作此药材", "No permission"),
        [ErrorCode.HerbNotDeleted] = ("该药材未被删除，无需恢复", "Herb is not deleted"),
        [ErrorCode.HerbInvalidPagination] = ("分页参数无效", "Invalid pagination"),
        [ErrorCode.HerbBatchEmpty] = ("批量操作时ID列表为空", "Batch ID list is empty"),
        [ErrorCode.HerbBatchImportExceeded] = ("批量导入超出限制（最多10000条）", "Batch import limit exceeded"),
        [ErrorCode.HerbBatchCheckExceeded] = ("批量检查超出限制（最多100条）", "Batch check limit exceeded"),
        [ErrorCode.HerbBatchItemNotFound] = ("批量操作时单项药材不存在", "Herb not found"),
        [ErrorCode.HerbBatchItemDeletedOrMissing] = ("批量状态更新时药材不存在或已删除", "Herb not found or deleted"),
        [ErrorCode.HerbBatchItemError] = ("批量操作时单项数据库异常", "Database error"),
        [ErrorCode.HerbImportFileEmpty] = ("导入文件为空", "Import file is empty"),
        [ErrorCode.HerbImportFileFormat] = ("导入文件格式不正确（仅支持.xlsx）", "Invalid file format"),
        [ErrorCode.HerbImportFileSize] = ("导入文件大小超限（最大10MB）", "File size limit exceeded"),
        [ErrorCode.HerbImportExcelError] = ("Excel文件格式错误（无工作表）", "Excel format error"),

        // 6xxxx - 配方模块
        [ErrorCode.FormulaNotFound] = ("配方未找到", "Formula not found"),
        [ErrorCode.FormulaNameExists] = ("配方名称已存在", "Formula name already exists"),
        [ErrorCode.FormulaValidationFailed] = ("配方验证失败", "Formula validation failed"),
        [ErrorCode.FormulaIdInvalid] = ("验方ID不能为空", "Invalid formula ID"),
        [ErrorCode.FormulaNoPermission] = ("无权限操作此验方", "No permission"),
        [ErrorCode.FormulaCreateFailed] = ("新增验方失败", "Failed to create formula"),
        [ErrorCode.FormulaUpdateFailed] = ("更新验方失败", "Failed to update formula"),
        [ErrorCode.FormulaDeleteFailed] = ("删除验方失败", "Failed to delete formula"),
        [ErrorCode.FormulaNotDeleted] = ("该验方未被删除，无需恢复", "Formula is not deleted"),
        [ErrorCode.FormulaInvalidPagination] = ("分页参数无效", "Invalid pagination"),
        [ErrorCode.FormulaHerbItemIdInvalid] = ("药材项参数不能为空", "Invalid herb item parameters"),
        [ErrorCode.FormulaHerbItemNotFound] = ("药材项不存在", "Herb item not found"),
        [ErrorCode.FormulaHerbItemAlreadyValidated] = ("该药材已校验，无需重复操作", "Herb already validated"),
        [ErrorCode.FormulaSystemHerbNotFound] = ("所选系统药材不存在", "System herb not found"),
        [ErrorCode.FormulaPendingValidationListFailed] = ("获取待校验验方列表失败", "Failed to get validation list"),
        [ErrorCode.FormulaBatchEmpty] = ("批量操作时ID列表为空", "Batch ID list is empty"),
        [ErrorCode.FormulaBatchImportEmpty] = ("批量导入数据不能为空", "Batch import data is empty"),
        [ErrorCode.FormulaBatchItemNotFound] = ("批量操作时单项方剂不存在", "Formula not found"),
        [ErrorCode.FormulaBatchItemError] = ("批量操作时单项数据库异常", "Database error"),

        // 8xxxx - 挂号模块
        [ErrorCode.RegistrationNotFound] = ("挂号记录不存在", "Registration not found"),
        [ErrorCode.RegistrationInvalidStatusTransition] = ("非法状态转换", "Invalid status transition"),
        [ErrorCode.RegistrationCancelNotAllowed] = ("有活跃/已完成医案，不允许取消", "Cannot cancel with active cases"),
    };

    public static string Get(ErrorCode code, bool useEnglish = false)
    {
        if (Messages.TryGetValue(code, out var msg))
            return useEnglish ? msg.En : msg.Zh;
        return code.ToString();
    }

    public static string GetUserMessage(ErrorCode code) => Get(code, useEnglish: false);
}
