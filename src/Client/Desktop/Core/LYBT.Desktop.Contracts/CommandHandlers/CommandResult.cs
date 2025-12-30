namespace LYBT.Desktop.Contracts.CommandHandlers;

/// <summary>
/// CommandHandler统一返回类型
/// OpenSpec: unify-desktop-architecture (Phase 1.4)
/// 所有CommandHandler方法使用此类型返回结果，确保错误处理一致性
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
/// <param name="Success">操作是否成功</param>
/// <param name="Data">返回数据（成功时有值）</param>
/// <param name="Error">错误消息（失败时有值）</param>
public record CommandResult<T>(bool Success, T? Data, string? Error)
{
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static CommandResult<T> Succeeded(T data) => new(true, data, null);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static CommandResult<T> Failed(string error) => new(false, default, error);

    /// <summary>
    /// 创建未找到结果
    /// </summary>
    public static CommandResult<T> NotFound(string? message = null)
        => new(false, default, message ?? "未找到请求的资源");

    /// <summary>
    /// 隐式转换为bool（方便条件判断）
    /// </summary>
    public static implicit operator bool(CommandResult<T> result) => result.Success;
}

/// <summary>
/// 无数据的CommandHandler返回类型
/// 用于删除等不需要返回数据的操作
/// </summary>
public record CommandResult(bool Success, string? Error)
{
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static CommandResult Succeeded() => new(true, null);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static CommandResult Failed(string error) => new(false, error);

    /// <summary>
    /// 隐式转换为bool
    /// </summary>
    public static implicit operator bool(CommandResult result) => result.Success;
}
