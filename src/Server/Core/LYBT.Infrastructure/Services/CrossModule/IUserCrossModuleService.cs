using System.Threading;
using System.Threading;
using LYBT.Shared.Models.DTOs.Users;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 用户域跨模块服务 (ISP: D5-1)
/// 供 MedicalCase + Auth 模块使用
/// </summary>
public interface IUserCrossModuleService
{
    /// <summary>获取用户基本信息</summary>
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>按用户名获取用户凭证信息 (含密码哈希)</summary>
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>更新用户密码哈希</summary>
    Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default);

    /// <summary>检查用户是否存在 (未删除)</summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// T5-P2-01: 更新登录失败状态 (FailedLoginCount + LockoutEnd)
    /// </summary>
    Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// T5-P2-01: 重置登录状态 (成功登录后清除锁定，更新 LastLoginTime)
    /// </summary>
    Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default);
}
