namespace LYBT.Infrastructure.Web
{

    /// <summary>
    /// API错误代码常量 - 前后端契约标准化
    /// 统一定义所有API可能返回的错误代码，便于前端统一处理
    /// </summary>
    public static class ApiErrorCodes
    {

        #region 通用错误代码

        /// <summary>
        /// 参数验证失败
        /// </summary>
        public const string VALIDATIONERROR = "VALIDATION_ERROR";

        /// <summary>
        /// 未授权访问
        /// </summary>
        public const string UNAUTHORIZED = "UNAUTHORIZED";

        /// <summary>
        /// 禁止访问
        /// </summary>
        public const string FORBIDDEN = "FORBIDDEN";

        /// <summary>
        /// 资源未找到
        /// </summary>
        public const string NOTFOUND = "NOT_FOUND";

        /// <summary>
        /// 资源冲突
        /// </summary>
        public const string CONFLICT = "CONFLICT";

        /// <summary>
        /// 服务器内部错误
        /// </summary>
        public const string INTERNALERROR = "INTERNAL_ERROR";

        /// <summary>
        /// 操作超时
        /// </summary>
        public const string TIMEOUT = "TIMEOUT";

        /// <summary>
        /// 请求频率过高
        /// </summary>
        public const string RATELIMITEXCEEDED = "RATE_LIMIT_EXCEEDED";

        #endregion 通用错误代码

        #region 认证授权相关错误

        /// <summary>
        /// 用户名或密码错误
        /// </summary>
        public const string INVALIDCREDENTIALS = "INVALID_CREDENTIALS";

        /// <summary>
        /// 账户已被锁定
        /// </summary>
        public const string ACCOUNTLOCKED = "ACCOUNT_LOCKED";

        /// <summary>
        /// 账户已禁用
        /// </summary>
        public const string ACCOUNTDISABLED = "ACCOUNT_DISABLED";

        /// <summary>
        /// Token已过期
        /// </summary>
        public const string TOKENEXPIRED = "TOKEN_EXPIRED";

        /// <summary>
        /// Token无效
        /// </summary>
        public const string INVALIDTOKEN = "INVALID_TOKEN";

        /// <summary>
        /// 权限不足
        /// </summary>
        public const string INSUFFICIENTPERMISSIONS = "INSUFFICIENT_PERMISSIONS";

        /// <summary>
        /// 认证失败
        /// </summary>
        public const string AUTHENTICATIONFAILED = "AUTHENTICATION_FAILED";

        /// <summary>
        /// 密码修改失败
        /// </summary>
        public const string PASSWORDCHANGEFAILED = "PASSWORD_CHANGE_FAILED";

        #endregion 认证授权相关错误

        #region 业务相关错误

        /// <summary>
        /// 用户名已存在
        /// </summary>
        public const string USERNAMEEXISTS = "USERNAME_EXISTS";

        /// <summary>
        /// 用户不存在
        /// </summary>
        public const string USERNOTFOUND = "USER_NOT_FOUND";

        /// <summary>
        /// 患者不存在
        /// </summary>
        public const string PATIENTNOTFOUND = "PATIENT_NOT_FOUND";

        /// <summary>
        /// 身份证号已存在
        /// </summary>
        public const string IDNUMBEREXISTS = "ID_NUMBER_EXISTS";

        /// <summary>
        /// 手机号已存在
        /// </summary>
        public const string PHONEEXISTS = "PHONE_EXISTS";

        /// <summary>
        /// 药材不存在
        /// </summary>
        public const string HERBNOTFOUND = "HERB_NOT_FOUND";

        /// <summary>
        /// 药材名称已存在
        /// </summary>
        public const string HERBNAMEEXISTS = "HERB_NAME_EXISTS";

        /// <summary>
        /// 库存不足
        /// </summary>
        public const string INSUFFICIENTSTOCK = "INSUFFICIENT_STOCK";

        /// <summary>
        /// 处方不存在
        /// </summary>
        public const string PRESCRIPTIONNOTFOUND = "PRESCRIPTION_NOT_FOUND";

        /// <summary>
        /// 验方不存在
        /// </summary>
        public const string FORMULANOTFOUND = "FORMULA_NOT_FOUND";

        /// <summary>
        /// 验方名称已存在
        /// </summary>
        public const string FORMULANAMEEXISTS = "FORMULA_NAME_EXISTS";

        /// <summary>
        /// 诊疗记录不存在
        /// </summary>
        public const string CONSULTATIONNOTFOUND = "CONSULTATION_NOT_FOUND";

        /// <summary>
        /// 病历不存在
        /// </summary>
        public const string MEDICALCASENOTFOUND = "MEDICAL_CASE_NOT_FOUND";

        #endregion 业务相关错误

        #region 数据操作相关错误

        /// <summary>
        /// 数据保存失败
        /// </summary>
        public const string DATASAVEFAILED = "DATA_SAVE_FAILED";

        /// <summary>
        /// 数据更新失败
        /// </summary>
        public const string DATAUPDATEFAILED = "DATA_UPDATE_FAILED";

        /// <summary>
        /// 数据删除失败
        /// </summary>
        public const string DATADELETEFAILED = "DATA_DELETE_FAILED";

        /// <summary>
        /// 数据库连接失败
        /// </summary>
        public const string DATABASECONNECTIONFAILED = "DATABASE_CONNECTION_FAILED";

        /// <summary>
        /// 数据格式错误
        /// </summary>
        public const string INVALIDDATAFORMAT = "INVALID_DATA_FORMAT";

        /// <summary>
        /// 数据完整性约束违反
        /// </summary>
        public const string DATAINTEGRITYVIOLATION = "DATA_INTEGRITY_VIOLATION";

        /// <summary>
        /// 数据验证失败
        /// </summary>
        public const string VALIDATIONFAILED = "VALIDATION_FAILED";

        /// <summary>
        /// 数据导出失败
        /// </summary>
        public const string DATAEXPORTFAILED = "DATA_EXPORT_FAILED";

        /// <summary>
        /// 数据查询失败
        /// </summary>
        public const string DATAQUERYFAILED = "DATA_QUERY_FAILED";

        #endregion 数据操作相关错误

        #region 文件操作相关错误

        /// <summary>
        /// 文件不存在
        /// </summary>
        public const string FILENOTFOUND = "FILE_NOT_FOUND";

        /// <summary>
        /// 文件格式不支持
        /// </summary>
        public const string UNSUPPORTEDFILEFORMAT = "UNSUPPORTED_FILE_FORMAT";

        /// <summary>
        /// 文件大小超限
        /// </summary>
        public const string FILESIZEEXCEEDED = "FILE_SIZE_EXCEEDED";

        /// <summary>
        /// 文件上传失败
        /// </summary>
        public const string FILEUPLOADFAILED = "FILE_UPLOAD_FAILED";

        /// <summary>
        /// 文件解析失败
        /// </summary>
        public const string FILEPARSEFAILED = "FILE_PARSE_FAILED";

        #endregion 文件操作相关错误

        #region 缓存相关错误

        /// <summary>
        /// 缓存操作失败
        /// </summary>
        public const string CACHEOPERATIONFAILED = "CACHE_OPERATION_FAILED";

        /// <summary>
        /// 缓存数据过期
        /// </summary>
        public const string CACHEDATAEXPIRED = "CACHE_DATA_EXPIRED";

        #endregion 缓存相关错误

        #region 第三方服务相关错误

        /// <summary>
        /// 第三方服务不可用
        /// </summary>
        public const string EXTERNALSERVICEUNAVAILABLE = "EXTERNAL_SERVICE_UNAVAILABLE";

        /// <summary>
        /// 第三方API调用失败
        /// </summary>
        public const string EXTERNALAPICALLFAILED = "EXTERNAL_API_CALL_FAILED";

        #endregion 第三方服务相关错误

        #region 系统相关错误

        /// <summary>
        /// 系统配置错误
        /// </summary>
        public const string SYSTEMCONFIGERROR = "SYSTEM_CONFIG_ERROR";

        /// <summary>
        /// 功能未实现
        /// </summary>
        public const string FEATURENOTIMPLEMENTED = "FEATURE_NOT_IMPLEMENTED";

        #endregion 系统相关错误
    }
}
