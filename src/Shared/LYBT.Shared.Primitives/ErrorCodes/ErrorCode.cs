namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// 错误码枚举
/// consolidate-exception-handling: 统一错误码定义
///
/// 错误码分区规则:
/// - 0xxxx: 通用错误
/// - 1xxxx: 用户模块 (Users)
/// - 2xxxx: 患者模块 (Patients)
/// - 3xxxx: 病例模块 (MedicalCase)
/// - 4xxxx: 处方模块 (Prescriptions)
/// - 5xxxx: 草药模块 (Herbs)
/// - 6xxxx: 配方模块 (Formula)
/// - 7xxxx: 问诊模块 (Consultation)
/// </summary>
public enum ErrorCode
{
    #region 0xxxx - 通用错误

    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 请求参数无效
    /// </summary>
    InvalidRequest = 1,

    /// <summary>
    /// 资源未找到
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// 验证失败
    /// </summary>
    ValidationFailed = 3,

    /// <summary>
    /// 未授权访问
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// 禁止访问
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// 并发冲突
    /// </summary>
    ConcurrencyConflict = 6,

    /// <summary>
    /// 操作超时
    /// </summary>
    Timeout = 7,

    /// <summary>
    /// 服务不可用
    /// </summary>
    ServiceUnavailable = 8,

    /// <summary>
    /// 内部服务器错误
    /// </summary>
    InternalError = 9,

    /// <summary>
    /// 数据库操作失败
    /// </summary>
    DatabaseError = 10,

    /// <summary>
    /// 配置错误
    /// </summary>
    ConfigurationError = 11,

    /// <summary>
    /// 请求频率过高
    /// </summary>
    RateLimitExceeded = 12,

    #endregion

    #region 1xxxx - 用户模块 (Users)

    /// <summary>
    /// 用户未找到
    /// </summary>
    UserNotFound = 10001,

    /// <summary>
    /// 用户名已存在
    /// </summary>
    UserNameExists = 10002,

    /// <summary>
    /// 邮箱已被使用
    /// </summary>
    EmailExists = 10003,

    /// <summary>
    /// 密码不正确
    /// </summary>
    InvalidPassword = 10004,

    /// <summary>
    /// 密码不符合策略要求
    /// </summary>
    PasswordPolicyViolation = 10005,

    /// <summary>
    /// 用户已被禁用
    /// </summary>
    UserDisabled = 10006,

    /// <summary>
    /// 用户已被锁定
    /// </summary>
    UserLocked = 10007,

    /// <summary>
    /// 登录凭证过期
    /// </summary>
    CredentialsExpired = 10008,

    /// <summary>
    /// 刷新令牌无效或过期
    /// </summary>
    InvalidRefreshToken = 10009,

    /// <summary>
    /// 角色不存在
    /// </summary>
    RoleNotFound = 10010,

    /// <summary>
    /// 无法删除系统管理员
    /// </summary>
    CannotDeleteSysAdmin = 10011,

    /// <summary>
    /// 需要首次登录修改密码
    /// </summary>
    PasswordChangeRequired = 10012,

    /// <summary>
    /// Access Token已过期
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    TokenExpired = 10013,

    /// <summary>
    /// 设备指纹不匹配
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    DeviceMismatch = 10014,

    /// <summary>
    /// 会话已过期
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    SessionExpired = 10015,

    #endregion

    #region 2xxxx - 患者模块 (Patients)

    /// <summary>
    /// 患者未找到
    /// </summary>
    PatientNotFound = 20001,

    /// <summary>
    /// 患者身份证已存在
    /// </summary>
    PatientIdCardExists = 20002,

    /// <summary>
    /// 患者电话已存在
    /// </summary>
    PatientPhoneExists = 20003,

    /// <summary>
    /// 患者有未完成的病例
    /// </summary>
    PatientHasActiveCases = 20004,

    /// <summary>
    /// 患者已被禁用
    /// </summary>
    PatientDisabled = 20005,

    /// <summary>
    /// 无效的患者状态
    /// </summary>
    InvalidPatientStatus = 20006,

    #endregion

    #region 3xxxx - 病例模块 (MedicalCase)

    /// <summary>
    /// 病例未找到
    /// </summary>
    MedicalCaseNotFound = 30001,

    /// <summary>
    /// 病例状态不允许此操作
    /// </summary>
    InvalidMedicalCaseState = 30002,

    /// <summary>
    /// 病例已归档
    /// </summary>
    MedicalCaseArchived = 30003,

    /// <summary>
    /// 病例正在被其他用户编辑
    /// </summary>
    MedicalCaseLocked = 30004,

    /// <summary>
    /// 病例数据版本冲突
    /// </summary>
    MedicalCaseVersionConflict = 30005,

    /// <summary>
    /// 无法创建重复病例
    /// </summary>
    DuplicateMedicalCase = 30006,

    /// <summary>
    /// 病例缺少必要的诊断信息
    /// </summary>
    MedicalCaseMissingDiagnosis = 30007,

    /// <summary>
    /// 无法删除有处方的病例
    /// </summary>
    MedicalCaseHasPrescriptions = 30008,

    #endregion

    #region 4xxxx - 处方模块 (Prescriptions)

    /// <summary>
    /// 处方未找到
    /// </summary>
    PrescriptionNotFound = 40001,

    /// <summary>
    /// 处方状态不允许此操作
    /// </summary>
    InvalidPrescriptionState = 40002,

    /// <summary>
    /// 处方已发药
    /// </summary>
    PrescriptionAlreadyDispensed = 40003,

    /// <summary>
    /// 处方草药为空
    /// </summary>
    PrescriptionNoHerbs = 40004,

    /// <summary>
    /// 处方剂量超出限制
    /// </summary>
    PrescriptionDosageExceeded = 40005,

    /// <summary>
    /// 处方包含禁忌配伍
    /// </summary>
    PrescriptionContraindication = 40006,

    /// <summary>
    /// 无法修改已完成的处方
    /// </summary>
    PrescriptionCompleted = 40007,

    #endregion

    #region 5xxxx - 草药模块 (Herbs)

    /// <summary>
    /// 草药未找到
    /// </summary>
    HerbNotFound = 50001,

    /// <summary>
    /// 草药名称已存在
    /// </summary>
    HerbNameExists = 50002,

    /// <summary>
    /// 草药库存不足
    /// </summary>
    HerbInsufficientStock = 50003,

    /// <summary>
    /// 草药已被禁用
    /// </summary>
    HerbDisabled = 50004,

    /// <summary>
    /// 无法删除已使用的草药
    /// </summary>
    HerbInUse = 50005,

    /// <summary>
    /// 草药价格无效
    /// </summary>
    HerbInvalidPrice = 50006,

    #endregion

    #region 6xxxx - 配方模块 (Formula)

    /// <summary>
    /// 配方未找到
    /// </summary>
    FormulaNotFound = 60001,

    /// <summary>
    /// 配方名称已存在
    /// </summary>
    FormulaNameExists = 60002,

    /// <summary>
    /// 配方草药为空
    /// </summary>
    FormulaNoHerbs = 60003,

    /// <summary>
    /// 配方验证失败
    /// </summary>
    FormulaValidationFailed = 60004,

    /// <summary>
    /// 无法删除已使用的配方
    /// </summary>
    FormulaInUse = 60005,

    /// <summary>
    /// 配方已被禁用
    /// </summary>
    FormulaDisabled = 60006,

    #endregion

    #region 7xxxx - 问诊模块 (Consultation)

    /// <summary>
    /// 问诊记录未找到
    /// </summary>
    ConsultationNotFound = 70001,

    /// <summary>
    /// 问诊状态不允许此操作
    /// </summary>
    InvalidConsultationState = 70002,

    /// <summary>
    /// 问诊已完成
    /// </summary>
    ConsultationCompleted = 70003,

    /// <summary>
    /// 问诊数据不完整
    /// </summary>
    ConsultationIncomplete = 70004,

    /// <summary>
    /// 症状描述为空
    /// </summary>
    ConsultationNoSymptoms = 70005,

    #endregion
}
