using LYBT.Shared.Primitives.ErrorCodes;

namespace LYBT.Shared.ExceptionHandling.Mappers;

/// <summary>
/// 错误消息映射接口
/// consolidate-exception-handling: 从LYBT.Infrastructure迁移
/// </summary>
public interface IErrorMessageMapper
{
    /// <summary>
    /// 获取错误码对应的用户友好消息
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <returns>用户友好消息</returns>
    string GetUserMessage(ErrorCode errorCode);

    /// <summary>
    /// 获取错误码对应的技术消息（供日志使用）
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <returns>技术消息</returns>
    string GetTechnicalMessage(ErrorCode errorCode);

    /// <summary>
    /// 获取错误码对应的用户友好消息，支持参数格式化
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的用户友好消息</returns>
    string GetUserMessage(ErrorCode errorCode, params object[] args);
}
