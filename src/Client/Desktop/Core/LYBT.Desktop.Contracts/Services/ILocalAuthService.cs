using LYBT.Entities.Users;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 本地认证服务接口
/// OpenSpec: implement-local-mode
/// </summary>
public interface ILocalAuthService
{
    /// <summary>
    /// 验证用户凭据
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码（明文）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>验证成功返回用户实体，否则返回 null</returns>
    Task<User?> ValidateAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="oldPassword">旧密码（明文）</param>
    /// <param name="newPassword">新密码（明文）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否修改成功</returns>
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken ct = default);
}
