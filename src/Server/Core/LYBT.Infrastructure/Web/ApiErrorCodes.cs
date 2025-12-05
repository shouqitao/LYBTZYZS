namespace LYBT.Infrastructure.Web
{
    /// <summary>
    /// API错误代码常量 - 前后端契约标准化
    /// 统一定义所有API可能返回的错误代码，便于前端统一处理
    ///
    /// 设计原则：
    /// - 仅定义实际使用或明确需要的错误码
    /// - 新错误码按需添加，避免过度设计
    /// - 错误码命名遵循 SCREAMING_SNAKE_CASE 规范
    /// </summary>
    public static class ApiErrorCodes
    {
        #region 通用错误代码 (HTTP状态码对应)

        /// <summary>
        /// 参数验证失败 (400)
        /// </summary>
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";

        /// <summary>
        /// 未授权访问 (401)
        /// </summary>
        public const string UNAUTHORIZED = "UNAUTHORIZED";

        /// <summary>
        /// 禁止访问 (403)
        /// </summary>
        public const string FORBIDDEN = "FORBIDDEN";

        /// <summary>
        /// 资源未找到 (404)
        /// </summary>
        public const string NOT_FOUND = "NOT_FOUND";

        /// <summary>
        /// 资源冲突 (409)
        /// </summary>
        public const string CONFLICT = "CONFLICT";

        /// <summary>
        /// 服务器内部错误 (500)
        /// </summary>
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";

        #endregion

        #region 认证授权相关错误

        /// <summary>
        /// 用户名或密码错误
        /// </summary>
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

        /// <summary>
        /// Token已过期
        /// </summary>
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

        /// <summary>
        /// Token无效
        /// </summary>
        public const string INVALID_TOKEN = "INVALID_TOKEN";

        #endregion

        #region 数据操作相关错误

        /// <summary>
        /// 数据保存失败 (HerbsController使用)
        /// </summary>
        public const string DATA_SAVE_FAILED = "DATA_SAVE_FAILED";

        /// <summary>
        /// 数据验证失败
        /// </summary>
        public const string VALIDATION_FAILED = "VALIDATION_FAILED";

        #endregion

        #region 向后兼容别名 (旧代码引用)

        // 保留旧命名风格的别名，避免破坏现有代码
        // TODO: 统一后可删除

        /// <summary>
        /// [兼容] 数据保存失败 - 使用 DATA_SAVE_FAILED
        /// </summary>
        public const string DATASAVEFAILED = DATA_SAVE_FAILED;

        #endregion
    }
}
