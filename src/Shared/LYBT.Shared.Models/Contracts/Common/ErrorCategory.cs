namespace LYBT.Shared.Models.Contracts.Common {

    /// <summary>
    /// 错误类别枚举
    /// </summary>
    public enum ErrorCategory {

        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 网络连接错误
        /// </summary>
        Network = 1,

        /// <summary>
        /// 身份验证错误
        /// </summary>
        Authentication = 2,

        /// <summary>
        /// 授权/权限错误
        /// </summary>
        Authorization = 3,

        /// <summary>
        /// 数据验证错误
        /// </summary>
        Validation = 4,

        /// <summary>
        /// 业务逻辑错误
        /// </summary>
        Business = 5,

        /// <summary>
        /// 数据访问错误
        /// </summary>
        DataAccess = 6,

        /// <summary>
        /// 配置错误
        /// </summary>
        Configuration = 7,

        /// <summary>
        /// 文件系统错误
        /// </summary>
        FileSystem = 8,

        /// <summary>
        /// 并发冲突错误
        /// </summary>
        Concurrency = 9,

        /// <summary>
        /// 超时错误
        /// </summary>
        Timeout = 10,

        /// <summary>
        /// 服务不可用
        /// </summary>
        ServiceUnavailable = 11,

        /// <summary>
        /// 资源不存在
        /// </summary>
        ResourceNotFound = 12,

        /// <summary>
        /// 系统内部错误
        /// </summary>
        Internal = 13
    }
}
