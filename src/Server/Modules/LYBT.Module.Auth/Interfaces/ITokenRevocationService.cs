namespace LYBT.Module.Auth.Interfaces;

/// <summary>
/// Token撤销服务接口
/// </summary>
public interface ITokenRevocationService
{
    /// <summary>
    /// 撤销单个RefreshToken
    /// </summary>
    /// <param name="token">Token字符串</param>
    /// <param name="reason">撤销原因</param>
    /// <returns>是否撤销成功</returns>
    Task<bool> RevokeTokenAsync(string token, string reason);

    /// <summary>
    /// 查询Token是否已撤销
    /// </summary>
    /// <param name="token">Token字符串</param>
    /// <returns>是否已撤销</returns>
    Task<bool> IsTokenRevokedAsync(string token);
}
