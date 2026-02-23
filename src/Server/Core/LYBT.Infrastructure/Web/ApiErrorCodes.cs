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
    ///
    /// Phase 3 S3 将引入 MCCEE 错误码体系，届时按需重新定义
    /// </summary>
    public static class ApiErrorCodes
    {
        #region 数据操作相关错误

        /// <summary>
        /// 数据保存失败 (HerbsController使用)
        /// </summary>
        public const string DATA_SAVE_FAILED = "DATA_SAVE_FAILED";

        #endregion

        #region 向后兼容别名 (旧代码引用)

        /// <summary>
        /// [兼容] 数据保存失败 - 使用 DATA_SAVE_FAILED
        /// </summary>
        public const string DATASAVEFAILED = DATA_SAVE_FAILED;

        #endregion
    }
}
