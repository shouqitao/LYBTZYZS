using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;

namespace LYBT.Module.Identity.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ApplicationUser?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);
    Task<PagedResult<ApplicationUser>> GetPagedAsync(int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken ct);
    Task UpdateAsync(ApplicationUser user, CancellationToken ct);

    // P10-1（2026-08-14）: 以下方法从 UserService 直注 IdentityDbContext 移入 Repository（Service 不直连 DbContext）
    /// <summary>按用户名查询未删除用户（登录凭证）</summary>
    Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct);

    /// <summary>更新登录失败状态（AccessFailedCount/LockoutEnd）</summary>
    Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTimeOffset? lockoutEnd, CancellationToken ct);

    /// <summary>重置登录状态（AccessFailedCount=0/LockoutEnd=null/LastLoginTime=now）</summary>
    Task ResetLoginStateAsync(Guid userId, CancellationToken ct);
}
