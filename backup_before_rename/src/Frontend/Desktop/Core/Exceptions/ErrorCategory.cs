namespace LYBT.Desktop.Core.Exceptions
{
    /// <summary>
    /// 错误类别枚举
    /// </summary>
    public enum ErrorCategory
    {
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
    
    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// 信息级别 - 不影响操作
        /// </summary>
        Info = 0,
        
        /// <summary>
        /// 警告级别 - 可能影响操作
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// 错误级别 - 操作失败但系统可恢复
        /// </summary>
        Error = 2,
        
        /// <summary>
        /// 严重级别 - 需要立即处理
        /// </summary>
        Critical = 3,
        
        /// <summary>
        /// 致命级别 - 系统无法继续运行
        /// </summary>
        Fatal = 4
    }
}