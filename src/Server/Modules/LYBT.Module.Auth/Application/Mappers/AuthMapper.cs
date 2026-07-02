using LYBT.Module.Auth.Domain;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Application.Mappers;

/// <summary>
/// 认证数据映射器。静态类，用于 Domain 实体与 DTO 之间的转换。
/// </summary>
public static class AuthMapper
{
    /// <summary>
    /// AuthSession 实体转换为会话信息DTO。
    /// </summary>
    public static SessionInfoDto ToSessionInfo(AuthSession session) => new()
    {
        SessionId = session.Id,
        UserId = session.UserId,
        LoginTime = session.LoginTime,
        ExpiryTime = session.ExpiryTime,
        IpAddress = session.IpAddress,
        UserAgent = session.UserAgent,
        IsValid = session.IsValid()
    };
}


