using LYBT.SharedKernel.DTOs;

namespace LYBT.SharedKernel.Contracts;

/// <summary>
/// 跨模块用户查询服务接口。供Auth、MedicalCase等模块查询用户信息。
/// 实现位于LYBT.Module.Users。
/// </summary>
public interface IUserCrossModuleService
{
    /// <summary>
    /// 根据用户ID获取用户基本信息。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户基本信息，不存在返回null</returns>
    Task<UserBasicDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户名获取用户基本信息。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户基本信息，不存在返回null</returns>
    Task<UserBasicDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户凭证（用户名+密码）。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证成功返回用户ID，失败返回null</returns>
    Task<Guid?> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default);
}


