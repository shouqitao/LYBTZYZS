namespace LYBT.Shared.Models.Errors;

/// <summary>
/// 错误类别枚举
/// refactor-logging-system: 定义错误的分类，用于日志分析和错误处理策略
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// 通用/未分类错误
    /// </summary>
    General = 0,

    /// <summary>
    /// 验证错误 - 输入数据不符合业务规则
    /// </summary>
    Validation = 1,

    /// <summary>
    /// 认证错误 - 身份验证失败
    /// </summary>
    Authentication = 2,

    /// <summary>
    /// 授权错误 - 权限不足
    /// </summary>
    Authorization = 3,

    /// <summary>
    /// 资源错误 - 资源不存在或状态不正确
    /// </summary>
    Resource = 4,

    /// <summary>
    /// 业务逻辑错误 - 违反业务规则
    /// </summary>
    Business = 5,

    /// <summary>
    /// 并发错误 - 数据冲突
    /// </summary>
    Concurrency = 6,

    /// <summary>
    /// 系统错误 - 内部服务器错误
    /// </summary>
    System = 7,

    /// <summary>
    /// 外部依赖错误 - 第三方服务调用失败
    /// </summary>
    External = 8,

    /// <summary>
    /// 配置错误 - 系统配置问题
    /// </summary>
    Configuration = 9
}
