namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// 错误码枚举
/// consolidate-exception-handling: 统一错误码定义
///
/// 错误码分区规则 (MCCEE: M=模块, CC=子类别, EE=序号):
/// - 0xxxx: 通用错误
/// - 1xxxx: 用户模块 (Users)
/// - 2xxxx: 患者模块 (Patients)
/// - 3xxxx: 医案模块 (MedicalCase)
/// - 4xxxx: 处方模块 (Prescriptions)
/// - 5xxxx: 草药模块 (Herbs)
/// - 6xxxx: 配方模块 (Formula)
/// - 7xxxx: 同步模块 (Sync)
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
    /// 设备指纹不匹配
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    DeviceMismatch = 10014,

    /// <summary>
    /// 会话已过期
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    SessionExpired = 10015,

    // --- Auth MCCEE 编码 (101xx~103xx) ---
    // OpenSpec: T3-X1-01 - Auth 模块 MCCEE 统一

    // 101xx: 认证错误

    /// <summary>
    /// 凭据无效 (用户名或密码错误)
    /// </summary>
    AuthInvalidCredentials = 10101,

    // 102xx: Token 错误

    /// <summary>
    /// Token 无效 (格式错误或签名验证失败)
    /// </summary>
    AuthTokenInvalid = 10202,

    /// <summary>
    /// Token 已被撤销
    /// </summary>
    AuthTokenRevoked = 10203,

    /// <summary>
    /// RefreshToken 已过期
    /// </summary>
    AuthRefreshTokenExpired = 10204,

    /// <summary>
    /// RefreshToken 无效
    /// </summary>
    AuthRefreshTokenInvalid = 10205,

    /// <summary>
    /// AccessToken 已过期
    /// US-ERR-007 (CODE-25)
    /// </summary>
    AuthAccessTokenExpired = 10206,

    // 103xx: 会话错误

    /// <summary>
    /// 并发会话数超限
    /// </summary>
    AuthConcurrentSessionLimit = 10303,

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
    /// 患者有关联的医案
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

    // --- MCCEE 编码 (207xx~208xx) ---

    // 207xx: 业务规则错误

    /// <summary>
    /// 手机号已存在 (创建/更新时)
    /// </summary>
    PatientPhoneDuplicate = 20701,

    /// <summary>
    /// 该患者未被删除，无需恢复
    /// </summary>
    PatientNotDeleted = 20702,

    /// <summary>
    /// 批量操作时 ID 列表为空
    /// </summary>
    PatientBatchOperationEmpty = 20703,

    /// <summary>
    /// 批量检查超出限制 (最多 100 条)
    /// </summary>
    PatientBatchCheckExceeded = 20704,

    /// <summary>
    /// 分页参数无效
    /// </summary>
    PatientInvalidPagination = 20705,

    // 208xx: 导入错误

    /// <summary>
    /// 导入文件为空
    /// </summary>
    PatientImportFileEmpty = 20801,

    /// <summary>
    /// 导入文件格式不正确 (仅支持 .xlsx)
    /// </summary>
    PatientImportFileFormat = 20802,

    /// <summary>
    /// 导入文件大小超限 (最大 10MB)
    /// </summary>
    PatientImportFileSize = 20803,

    /// <summary>
    /// Excel 文件中没有工作表
    /// </summary>
    PatientImportNoWorksheet = 20804,

    /// <summary>
    /// 导入数据超过限制 (最大 1000 行)
    /// </summary>
    PatientImportRowExceeded = 20805,

    #endregion

    #region 3xxxx - 医案模块 (MedicalCase)

    /// <summary>
    /// 医案未找到
    /// </summary>
    MedicalCaseNotFound = 30001,

    /// <summary>
    /// 医案状态不允许此操作
    /// </summary>
    InvalidMedicalCaseState = 30002,

    /// <summary>
    /// 医案已归档
    /// </summary>
    MedicalCaseArchived = 30003,

    /// <summary>
    /// 医案正在被其他用户编辑
    /// </summary>
    MedicalCaseLocked = 30004,

    /// <summary>
    /// 医案数据版本冲突
    /// </summary>
    MedicalCaseVersionConflict = 30005,

    /// <summary>
    /// 无法创建重复医案
    /// </summary>
    DuplicateMedicalCase = 30006,

    /// <summary>
    /// 医案缺少必要的诊断信息
    /// </summary>
    MedicalCaseMissingDiagnosis = 30007,

    /// <summary>
    /// 无法删除有处方的医案
    /// </summary>
    MedicalCaseHasPrescriptions = 30008,

    // --- MCCEE 编码 (301xx~306xx) ---
    // OpenSpec: T3-X1-12 - 现有 30001~30008 保留兼容，新 MCCEE 码并行

    // 301xx: 创建医案错误

    /// <summary>
    /// 创建医案时患者不存在
    /// </summary>
    McPatientNotFound = 30101,

    /// <summary>
    /// 创建医案时医生不存在
    /// </summary>
    McDoctorNotFound = 30102,

    /// <summary>
    /// 该患者已有进行中的医案
    /// </summary>
    McActiveCaseExists = 30103,

    /// <summary>
    /// 该患者已有挂起的医案
    /// </summary>
    McSuspendedCaseExists = 30104,

    /// <summary>
    /// 患者已被禁用，无法创建医案
    /// </summary>
    McPatientDisabled = 30105,

    // 302xx: 权限错误

    /// <summary>
    /// 无权限编辑此医案
    /// </summary>
    McCannotEditCase = 30201,

    /// <summary>
    /// 无权限删除此医案
    /// </summary>
    McCannotDeleteCase = 30202,

    /// <summary>
    /// 无权限取消此医案
    /// </summary>
    McCannotCancelCase = 30203,

    /// <summary>
    /// 无权限删除此医案的处方
    /// </summary>
    McCannotDeletePrescription = 30204,

    /// <summary>
    /// 无权限挂起此医案
    /// </summary>
    McCannotSuspendCase = 30205,

    // 303xx: 状态转换错误

    /// <summary>
    /// 不允许的状态转换
    /// </summary>
    McInvalidStatusTransition = 30301,

    /// <summary>
    /// 完成前需标记处方需求
    /// </summary>
    McPrescriptionFlagRequired = 30302,

    /// <summary>
    /// 已标记需要开处方但处方不存在
    /// </summary>
    McPrescriptionRequired = 30303,

    /// <summary>
    /// 已完成的医案不可挂起
    /// </summary>
    McCompletedCannotSuspend = 30304,

    /// <summary>
    /// 已删除的医案不可挂起
    /// </summary>
    McDeletedCannotSuspend = 30305,

    /// <summary>
    /// 已完成的医案不可取消
    /// </summary>
    McCompletedCannotCancel = 30306,

    /// <summary>
    /// 医案已经是删除状态
    /// </summary>
    McAlreadyDeleted = 30307,

    /// <summary>
    /// 非当天本人取消需提供取消原因
    /// T5-P2-16
    /// </summary>
    McCancelReasonRequired = 30308,

    /// <summary>
    /// 完成时处方明细为空
    /// T5-P2-15
    /// </summary>
    McPrescriptionItemsRequired = 30309,

    // 304xx: 处方错误

    /// <summary>
    /// 未标记需要开处方
    /// </summary>
    McPrescriptionFlagNotSet = 30401,

    /// <summary>
    /// 医案已存在处方，请使用更新接口
    /// </summary>
    McPrescriptionAlreadyExists = 30402,

    /// <summary>
    /// 医案已打印，修改需要提供修改原因
    /// </summary>
    McPrintedRequiresReason = 30403,

    /// <summary>
    /// 医案已打印，不允许删除处方
    /// </summary>
    McPrintedCannotDelete = 30404,

    /// <summary>
    /// 诊断记录不存在 (内部错误)
    /// </summary>
    McConsultationNotFound = 30405,

    // 305xx: 并发和系统错误

    /// <summary>
    /// 创建处方并发重试失败
    /// </summary>
    McPrescriptionCreateRetryFailed = 30501,

    /// <summary>
    /// 保存并发重试失败
    /// </summary>
    McSaveRetryFailed = 30502,

    // 306xx: 参数验证错误

    /// <summary>
    /// 请求 ID 与路由 ID 不匹配
    /// </summary>
    McRequestIdMismatch = 30601,

    /// <summary>
    /// 分页参数无效
    /// </summary>
    McInvalidPagination = 30602,

    /// <summary>
    /// 批量查询超出限制 (最多 50 个)
    /// </summary>
    McBatchQueryExceeded = 30603,

    /// <summary>
    /// 批量操作时 ID 列表为空
    /// </summary>
    McBatchOperationEmpty = 30604,

    /// <summary>
    /// 患者 ID 无效
    /// </summary>
    McInvalidPatientId = 30605,

    /// <summary>
    /// 返回数量参数无效 (1-50)
    /// </summary>
    McInvalidCountParam = 30606,

    /// <summary>
    /// 医案不存在
    /// </summary>
    McCaseNotFound = 30607,

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

    // --- MCCEE 编码 (501xx~503xx) ---

    // 501xx: 核心错误

    /// <summary>
    /// 药材验证失败 (FluentValidation)
    /// </summary>
    HerbValidationFailed = 50102,

    /// <summary>
    /// 无权限操作此药材 (Doctor 操作他人创建的药材)
    /// </summary>
    HerbNoPermission = 50103,

    /// <summary>
    /// 该药材未被删除，无需恢复
    /// </summary>
    HerbNotDeleted = 50104,

    /// <summary>
    /// 分页参数无效
    /// </summary>
    HerbInvalidPagination = 50106,

    // 502xx: 批量操作错误

    /// <summary>
    /// 批量操作时 ID 列表为空
    /// </summary>
    HerbBatchEmpty = 50201,

    /// <summary>
    /// 批量导入超出限制 (最多 10000 条)
    /// </summary>
    HerbBatchImportExceeded = 50202,

    /// <summary>
    /// 批量检查超出限制 (最多 100 条)
    /// </summary>
    HerbBatchCheckExceeded = 50203,

    /// <summary>
    /// 批量操作时单项药材不存在
    /// </summary>
    HerbBatchItemNotFound = 50204,

    /// <summary>
    /// 批量状态更新时药材不存在或已删除
    /// </summary>
    HerbBatchItemDeletedOrMissing = 50205,

    /// <summary>
    /// 批量操作时单项数据库异常
    /// </summary>
    HerbBatchItemError = 50206,

    // 503xx: Excel 导入错误

    /// <summary>
    /// 导入文件为空
    /// </summary>
    HerbImportFileEmpty = 50301,

    /// <summary>
    /// 导入文件格式不正确 (仅支持 .xlsx)
    /// </summary>
    HerbImportFileFormat = 50302,

    /// <summary>
    /// 导入文件大小超限 (最大 10MB)
    /// </summary>
    HerbImportFileSize = 50303,

    /// <summary>
    /// Excel 文件格式错误 (无工作表)
    /// </summary>
    HerbImportExcelError = 50304,

    /// <summary>
    /// Excel 文件中没有数据行
    /// </summary>
    HerbImportNoData = 50305,

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

    // --- MCCEE 编码 (601xx~603xx) ---

    // 601xx: 核心错误

    /// <summary>
    /// 验方ID不能为空
    /// </summary>
    FormulaIdInvalid = 60102,

    /// <summary>
    /// 无权限操作此验方 (Doctor 操作他人创建的验方)
    /// </summary>
    FormulaNoPermission = 60103,

    /// <summary>
    /// 新增验方失败
    /// </summary>
    FormulaCreateFailed = 60104,

    /// <summary>
    /// 更新验方失败
    /// </summary>
    FormulaUpdateFailed = 60105,

    /// <summary>
    /// 删除验方失败 (验方不存在)
    /// </summary>
    FormulaDeleteFailed = 60106,

    /// <summary>
    /// 该验方未被删除，无需恢复
    /// </summary>
    FormulaNotDeleted = 60107,

    /// <summary>
    /// 分页参数无效
    /// </summary>
    FormulaInvalidPagination = 60108,

    // 602xx: 药材验证错误

    /// <summary>
    /// 药材项参数不能为空 (formulaId/herbItemId/selectedHerbId)
    /// </summary>
    FormulaHerbItemIdInvalid = 60201,

    /// <summary>
    /// 药材项不存在
    /// </summary>
    FormulaHerbItemNotFound = 60202,

    /// <summary>
    /// 该药材已校验，无需重复操作
    /// </summary>
    FormulaHerbItemAlreadyValidated = 60203,

    /// <summary>
    /// 所选系统药材不存在
    /// </summary>
    FormulaSystemHerbNotFound = 60204,

    /// <summary>
    /// 获取待校验验方列表失败
    /// </summary>
    FormulaPendingValidationListFailed = 60205,

    // 603xx: 批量操作错误

    /// <summary>
    /// 批量操作时 ID 列表为空
    /// </summary>
    FormulaBatchEmpty = 60301,

    /// <summary>
    /// 批量导入数据不能为空
    /// </summary>
    FormulaBatchImportEmpty = 60302,

    /// <summary>
    /// 批量操作时单项方剂不存在
    /// </summary>
    FormulaBatchItemNotFound = 60303,

    /// <summary>
    /// 批量操作时单项数据库异常
    /// </summary>
    FormulaBatchItemError = 60304,

    #endregion

    #region 7xxxx - 同步模块 (Sync)

    // --- 701xx: 服务端通用错误 ---

    /// <summary>
    /// 不支持的实体类型
    /// </summary>
    UnsupportedEntityType = 70101,

    /// <summary>
    /// JSON 反序列化失败
    /// </summary>
    JsonDeserializeFailed = 70102,

    /// <summary>
    /// 同步数据冲突 (服务端已存在)
    /// </summary>
    SyncDataConflict = 70103,

    // --- 702xx: 服务端上传错误 ---

    /// <summary>
    /// 药材上传失败
    /// </summary>
    HerbUploadFailed = 70201,

    /// <summary>
    /// 患者上传失败
    /// </summary>
    PatientUploadFailed = 70202,

    /// <summary>
    /// 验方上传失败
    /// </summary>
    FormulaUploadFailed = 70203,

    /// <summary>
    /// 医案上传失败
    /// </summary>
    MedicalCaseUploadFailed = 70204,

    // --- 703xx: 服务端 MedicalCase 特有错误 ---

    /// <summary>
    /// 同步时患者不存在
    /// </summary>
    SyncPatientNotFound = 70301,

    /// <summary>
    /// 同步时药材不存在
    /// </summary>
    SyncHerbNotFound = 70302,

    /// <summary>
    /// 医案已锁定，无法通过同步覆盖
    /// </summary>
    SyncCaseLocked = 70304,

    // --- 704xx: 服务端删除错误 ---

    /// <summary>
    /// 同步删除引用检查失败
    /// </summary>
    SyncReferenceCheckFailed = 70401,

    /// <summary>
    /// 同步删除时药材被处方引用
    /// </summary>
    SyncHerbHasReference = 70402,

    /// <summary>
    /// 同步删除时患者有医案记录
    /// </summary>
    SyncPatientHasReference = 70403,

    /// <summary>
    /// 同步删除时实体不存在
    /// </summary>
    SyncEntityNotFound = 70404,

    // --- 705xx: 客户端错误 ---

    /// <summary>
    /// 未选择同步数据类型
    /// </summary>
    SyncNoEntityTypeSelected = 70501,

    /// <summary>
    /// 同步失败
    /// </summary>
    SyncFailed = 70502,

    /// <summary>
    /// 不支持的 Checksum 实体类型
    /// </summary>
    SyncChecksumTypeError = 70503,

    /// <summary>
    /// 同步前依赖未满足
    /// </summary>
    SyncDependencyNotSynced = 70504,

    /// <summary>
    /// 患者重映射失败
    /// </summary>
    SyncPatientRemapFailed = 70505,

    /// <summary>
    /// 本地有未完成的医案，无法切换模式
    /// </summary>
    SyncLocalActiveCasesExist = 70506,

    #endregion

    #region 8xxxx - 挂号模块 (Registration)

    /// <summary>
    /// 挂号记录不存在
    /// </summary>
    RegistrationNotFound = 80001,

    /// <summary>
    /// 非法状态转换
    /// </summary>
    RegistrationInvalidStatusTransition = 80002,

    /// <summary>
    /// 有活跃/已完成医案，不允许取消
    /// </summary>
    RegistrationCancelNotAllowed = 80003,

    /// <summary>
    /// 无权取消此挂号 (非 Receptionist 或非本人创建)
    /// </summary>
    RegistrationUnauthorizedCancel = 80004,

    /// <summary>
    /// 患者已禁用，不允许创建挂号
    /// </summary>
    RegistrationPatientDisabled = 80005,

    /// <summary>
    /// 指派医生不可用 (禁用/不存在)
    /// </summary>
    RegistrationDoctorNotAvailable = 80006,

    /// <summary>
    /// 该患者已有等待中的挂号记录
    /// </summary>
    RegistrationDuplicateWaiting = 80007,

    #endregion
}
