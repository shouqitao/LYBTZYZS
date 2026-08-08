using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces;

/// <summary>
/// 用户服务接口 — 封装简单 CRUD 操作，供 Controller 直接注入。
/// </summary>
public interface IUserService
{
    Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);
    Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct);
}
