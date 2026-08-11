using LYBT.Shared.Models.Primitives.ErrorCodes;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 异常工厂（B2 US-ERR-007: 统一抛异常入口——服务层用一行代码抛出语义化异常）
/// </summary>
public static class ExceptionFactory
{
    /// <summary>抛业务规则异常（400/409 按错误码映射）</summary>
    public static BusinessException Business(string message, string? businessRule = null)
        => new(message, businessRule!);

    /// <summary>抛资源未找到异常（404）</summary>
    public static NotFoundException NotFound(string message)
        => new(message);

    /// <summary>抛资源未找到异常（404，资源维度）</summary>
    public static NotFoundException NotFound(string resourceType, string resourceId)
        => new(resourceType, resourceId);

    /// <summary>抛资源冲突异常（409）</summary>
    public static ConflictException Conflict(string message, EC errorCode = EC.ConcurrencyConflict)
        => new(errorCode, message);

    /// <summary>抛未授权异常（401）</summary>
    public static UnauthorizedException Unauthorized(string message)
        => new(message);

    /// <summary>抛验证异常（422 + 字段级 errors）</summary>
    public static ValidationException Validation(string field, string message)
        => new(field, message);

    /// <summary>抛验证异常（422 + 多字段 errors 字典）</summary>
    public static ValidationException Validation(IDictionary<string, string[]> errors, string message = "请求参数验证失败")
        => new(errors, message);

    /// <summary>抛通用 API 异常（显式状态码）</summary>
    public static ApiException Api(string message, int statusCode, ErrorCategory category = ErrorCategory.General)
        => new(message, statusCode, category);
}
