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
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";

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
        public const string NOT_FOUND = "NOT_FOUND";

        /// <summary>
        /// 资源冲突
        /// </summary>
        public const string CONFLICT = "CONFLICT";

        /// <summary>
        /// 服务器内部错误
        /// </summary>
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";

        /// <summary>
        /// 操作超时
        /// </summary>
        public const string TIMEOUT = "TIMEOUT";

        /// <summary>
        /// 请求频率过高
        /// </summary>
        public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

        #endregion

        #region 认证授权相关错误

        /// <summary>
        /// 用户名或密码错误
        /// </summary>
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

        /// <summary>
        /// 账户已被锁定
        /// </summary>
        public const string ACCOUNT_LOCKED = "ACCOUNT_LOCKED";

        /// <summary>
        /// 账户已禁用
        /// </summary>
        public const string ACCOUNT_DISABLED = "ACCOUNT_DISABLED";

        /// <summary>
        /// Token已过期
        /// </summary>
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

        /// <summary>
        /// Token无效
        /// </summary>
        public const string INVALID_TOKEN = "INVALID_TOKEN";

        /// <summary>
        /// 权限不足
        /// </summary>
        public const string INSUFFICIENT_PERMISSIONS = "INSUFFICIENT_PERMISSIONS";

        /// <summary>
        /// 认证失败
        /// </summary>
        public const string AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED";

        /// <summary>
        /// 密码修改失败
        /// </summary>
        public const string PASSWORD_CHANGE_FAILED = "PASSWORD_CHANGE_FAILED";

        #endregion

        #region 业务相关错误

        /// <summary>
        /// 用户名已存在
        /// </summary>
        public const string USERNAME_EXISTS = "USERNAME_EXISTS";

        /// <summary>
        /// 用户不存在
        /// </summary>
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";

        /// <summary>
        /// 患者不存在
        /// </summary>
        public const string PATIENT_NOT_FOUND = "PATIENT_NOT_FOUND";

        /// <summary>
        /// 身份证号已存在
        /// </summary>
        public const string ID_NUMBER_EXISTS = "ID_NUMBER_EXISTS";

        /// <summary>
        /// 手机号已存在
        /// </summary>
        public const string PHONE_EXISTS = "PHONE_EXISTS";

        /// <summary>
        /// 药材不存在
        /// </summary>
        public const string HERB_NOT_FOUND = "HERB_NOT_FOUND";

        /// <summary>
        /// 药材名称已存在
        /// </summary>
        public const string HERB_NAME_EXISTS = "HERB_NAME_EXISTS";

        /// <summary>
        /// 库存不足
        /// </summary>
        public const string INSUFFICIENT_STOCK = "INSUFFICIENT_STOCK";

        /// <summary>
        /// 处方不存在
        /// </summary>
        public const string PRESCRIPTION_NOT_FOUND = "PRESCRIPTION_NOT_FOUND";

        /// <summary>
        /// 验方不存在
        /// </summary>
        public const string FORMULA_NOT_FOUND = "FORMULA_NOT_FOUND";

        /// <summary>
        /// 验方名称已存在
        /// </summary>
        public const string FORMULA_NAME_EXISTS = "FORMULA_NAME_EXISTS";

        /// <summary>
        /// 看诊记录不存在
        /// </summary>
        public const string CONSULTATION_NOT_FOUND = "CONSULTATION_NOT_FOUND";

        /// <summary>
        /// 病历不存在
        /// </summary>
        public const string MEDICAL_CASE_NOT_FOUND = "MEDICAL_CASE_NOT_FOUND";

        #endregion

        #region 数据操作相关错误

        /// <summary>
        /// 数据保存失败
        /// </summary>
        public const string DATA_SAVE_FAILED = "DATA_SAVE_FAILED";

        /// <summary>
        /// 数据更新失败
        /// </summary>
        public const string DATA_UPDATE_FAILED = "DATA_UPDATE_FAILED";

        /// <summary>
        /// 数据删除失败
        /// </summary>
        public const string DATA_DELETE_FAILED = "DATA_DELETE_FAILED";

        /// <summary>
        /// 数据库连接失败
        /// </summary>
        public const string DATABASE_CONNECTION_FAILED = "DATABASE_CONNECTION_FAILED";

        /// <summary>
        /// 数据格式错误
        /// </summary>
        public const string INVALID_DATA_FORMAT = "INVALID_DATA_FORMAT";

        /// <summary>
        /// 数据完整性约束违反
        /// </summary>
        public const string DATA_INTEGRITY_VIOLATION = "DATA_INTEGRITY_VIOLATION";

        /// <summary>
        /// 数据验证失败
        /// </summary>
        public const string VALIDATION_FAILED = "VALIDATION_FAILED";

        /// <summary>
        /// 数据导出失败
        /// </summary>
        public const string DATA_EXPORT_FAILED = "DATA_EXPORT_FAILED";

        /// <summary>
        /// 数据查询失败
        /// </summary>
        public const string DATA_QUERY_FAILED = "DATA_QUERY_FAILED";

        #endregion

        #region 文件操作相关错误

        /// <summary>
        /// 文件不存在
        /// </summary>
        public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";

        /// <summary>
        /// 文件格式不支持
        /// </summary>
        public const string UNSUPPORTED_FILE_FORMAT = "UNSUPPORTED_FILE_FORMAT";

        /// <summary>
        /// 文件大小超限
        /// </summary>
        public const string FILE_SIZE_EXCEEDED = "FILE_SIZE_EXCEEDED";

        /// <summary>
        /// 文件上传失败
        /// </summary>
        public const string FILE_UPLOAD_FAILED = "FILE_UPLOAD_FAILED";

        /// <summary>
        /// 文件解析失败
        /// </summary>
        public const string FILE_PARSE_FAILED = "FILE_PARSE_FAILED";

        #endregion

        #region 缓存相关错误

        /// <summary>
        /// 缓存操作失败
        /// </summary>
        public const string CACHE_OPERATION_FAILED = "CACHE_OPERATION_FAILED";

        /// <summary>
        /// 缓存数据过期
        /// </summary>
        public const string CACHE_DATA_EXPIRED = "CACHE_DATA_EXPIRED";

        #endregion

        #region 第三方服务相关错误

        /// <summary>
        /// 第三方服务不可用
        /// </summary>
        public const string EXTERNAL_SERVICE_UNAVAILABLE = "EXTERNAL_SERVICE_UNAVAILABLE";

        /// <summary>
        /// 第三方API调用失败
        /// </summary>
        public const string EXTERNAL_API_CALL_FAILED = "EXTERNAL_API_CALL_FAILED";

        #endregion

        #region 系统相关错误

        /// <summary>
        /// 系统配置错误
        /// </summary>
        public const string SYSTEM_CONFIG_ERROR = "SYSTEM_CONFIG_ERROR";

        /// <summary>
        /// 功能未实现
        /// </summary>
        public const string FEATURE_NOT_IMPLEMENTED = "FEATURE_NOT_IMPLEMENTED";

        #endregion
    }

    /// <summary>
    /// 错误消息常量
    /// </summary>
    public static class ApiErrorMessages
    {
        public const string VALIDATION_FAILED = "参数验证失败";
        public const string UNAUTHORIZED_ACCESS = "未授权访问";
        public const string FORBIDDEN_ACCESS = "禁止访问";
        public const string RESOURCE_NOT_FOUND = "资源未找到";
        public const string INTERNAL_SERVER_ERROR = "服务器内部错误";
        public const string OPERATION_TIMEOUT = "操作超时";
        public const string RATE_LIMIT_EXCEEDED = "请求频率过高，请稍后再试";
        
        public const string INVALID_CREDENTIALS = "用户名或密码错误";
        public const string ACCOUNT_LOCKED = "账户已被锁定";
        public const string ACCOUNT_DISABLED = "账户已禁用";
        public const string TOKEN_EXPIRED = "登录已过期，请重新登录";
        public const string INVALID_TOKEN = "无效的访问令牌";
        
        public const string DATA_SAVE_FAILED = "数据保存失败";
        public const string DATA_UPDATE_FAILED = "数据更新失败";
        public const string DATA_DELETE_FAILED = "数据删除失败";
        
        public const string USERNAME_EXISTS = "用户名已存在";
        public const string ID_NUMBER_EXISTS = "身份证号已存在";
        public const string PHONE_EXISTS = "手机号已存在";
        public const string HERB_NAME_EXISTS = "药材名称已存在";
        public const string FORMULA_NAME_EXISTS = "验方名称已存在";
    }
}