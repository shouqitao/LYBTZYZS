using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 用户域服务 (ISP: D5-1，A-31-C3a 由 IUserCrossModuleService 改名 + IUserService 合并)
/// 供 MedicalCase + Registration 模块跨模块使用；Identity 模块内登录/Controller 亦经其消费。
/// </summary>
public interface IUserService
{
    // ── 读服务（原 Users 模块 IUserService，Controller 直查）──

    /// <summary>分页查询用户</summary>
    Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);

    /// <summary>按 ID 获取用户详情</summary>
    Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>获取当前用户详情</summary>
    Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct);

    // ── 跨模块/登录凭证（原 IUserCrossModuleService）──

    /// <summary>获取用户基本信息</summary>
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>按用户名获取用户凭证信息 (含密码哈希)</summary>
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// T5-P2-01: 更新登录失败状态 (FailedLoginCount + LockoutEnd)
    /// </summary>
    Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// T5-P2-01: 重置登录状态 (成功登录后清除锁定，更新 LastLoginTime)
    /// </summary>
    Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户密码 (使用 Identity PBKDF2 而非 BCrypt)
    /// </summary>
    Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default);
}
