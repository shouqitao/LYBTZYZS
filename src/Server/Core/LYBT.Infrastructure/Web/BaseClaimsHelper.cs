using System.Security.Claims;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// Claims 提取和角色映射辅助类
/// 供 WebAPI 和 LocalWebAPI 共享
/// </summary>
public static class BaseClaimsHelper
{
    /// <summary>
    /// 从 Claims 提取当前用户 ID
    /// </summary>
    public static Guid GetCurrentUserId(ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
}


