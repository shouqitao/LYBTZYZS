using System.ComponentModel;

namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// 错误类别枚举
/// consolidate-exception-handling: 错误分类
/// </summary>
public enum ErrorCategory
{
    /// <summary>通用/未分类错误</summary>
    [Description("通用错误")]
    General = 0,

    /// <summary>验证错误 - 输入数据不符合业务规则</summary>
    [Description("验证错误")]
    Validation = 1,

    /// <summary>认证错误 - 身份验证失败</summary>
    [Description("认证错误")]
    Authentication = 2,

    /// <summary>授权错误 - 权限不足</summary>
    [Description("授权错误")]
    Authorization = 3,

    /// <summary>资源错误 - 资源不存在或状态不正确</summary>
    [Description("资源错误")]
    Resource = 4,

    /// <summary>业务逻辑错误 - 违反业务规则</summary>
    [Description("业务错误")]
    Business = 5,

    /// <summary>并发错误 - 数据冲突</summary>
    [Description("并发错误")]
    Concurrency = 6,

    /// <summary>系统错误 - 内部服务器错误</summary>
    [Description("系统错误")]
    System = 7,

    /// <summary>外部依赖错误 - 第三方服务调用失败</summary>
    [Description("外部错误")]
    External = 8,

    /// <summary>配置错误 - 系统配置问题</summary>
    [Description("配置错误")]
    Configuration = 9,

    /// <summary>网络连接错误</summary>
    [Description("网络错误")]
    Network = 10,

    /// <summary>未知错误</summary>
    [Description("未知错误")]
    Unknown = 99
}

/// <summary>
/// 错误严重程度枚举
/// consolidate-exception-handling: 错误严重级别
/// </summary>
public enum ErrorSeverity
{
    /// <summary>信息级别 - 仅为通知，不影响正常使用</summary>
    [Description("信息")]
    Info = 0,

    /// <summary>警告级别 - 可能影响功能，但不会阻止使用</summary>
    [Description("警告")]
    Warning = 1,

    /// <summary>错误级别 - 影响功能正常使用，需要用户注意</summary>
    [Description("错误")]
    Error = 2,

    /// <summary>严重级别 - 严重影响系统功能，需要立即处理</summary>
    [Description("严重")]
    Critical = 3,

    /// <summary>致命级别 - 系统无法继续运行，需要重启或修复</summary>
    [Description("致命")]
    Fatal = 4
}
