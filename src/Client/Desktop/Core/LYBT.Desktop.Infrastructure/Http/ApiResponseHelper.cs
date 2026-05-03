using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// 统一处理 Remote (ApiResponse&lt;T&gt;) 和 Local (直接 DTO) 的返回值解包。
/// Remote 模式下 Refit 返回 ApiResponse&lt;T&gt;，需要 .Data 访问实际数据。
/// Local 模式下 Refit 直接返回 DTO，无需解包。
/// </summary>
public static class ApiResponseHelper
{
    /// <summary>
    /// 从 ApiResponse&lt;T&gt; 解包，返回 .Data。
    /// </summary>
    public static T? Unwrap<T>(ApiResponse<T> apiResponse) where T : class
    {
        return apiResponse.Data;
    }

    /// <summary>
    /// 从 ApiResponse&lt;T&gt; 解包，返回 .Data；如果响应失败则抛出异常。
    /// </summary>
    public static T UnwrapOrThrow<T>(ApiResponse<T> apiResponse) where T : class
    {
        if (!apiResponse.Success)
        {
            throw new InvalidOperationException(
                $"API request failed: {apiResponse.Message}");
        }

        return apiResponse.Data
            ?? throw new InvalidOperationException(
                "API response indicated success but Data is null.");
    }

    /// <summary>
    /// 对于直接返回 DTO 的 Local 模式，原样返回（无需解包）。
    /// </summary>
    public static T Unwrap<T>(T response) where T : class
    {
        return response;
    }

    /// <summary>
    /// 检查 ApiResponse 是否成功（对于直接返回 DTO 的 Local 模式，始终返回 true）
    /// </summary>
    public static bool IsSuccess<T>(ApiResponse<T>? apiResponse) where T : class
    {
        return apiResponse?.Success ?? false;
    }
}
