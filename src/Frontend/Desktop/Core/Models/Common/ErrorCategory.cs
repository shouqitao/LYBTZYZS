namespace LYBT.WPF.Client.Core.Models.Common
{
    /// <summary>
    /// 错误分类
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// 网络相关错误
        /// </summary>
        Network = 1,

        /// <summary>
        /// 认证和授权错误
        /// </summary>
        Authentication = 2,

        /// <summary>
        /// 数据验证错误
        /// </summary>
        Validation = 3,

        /// <summary>
        /// 业务逻辑错误
        /// </summary>
        Business = 4,

        /// <summary>
        /// 系统内部错误
        /// </summary>
        System = 5,

        /// <summary>
        /// 用户操作错误
        /// </summary>
        UserOperation = 6,

        /// <summary>
        /// 配置错误
        /// </summary>
        Configuration = 7,

        /// <summary>
        /// 外部服务错误
        /// </summary>
        ExternalService = 8,

        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown = 99
    }
}