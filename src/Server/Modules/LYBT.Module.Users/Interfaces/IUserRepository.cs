using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;

namespace LYBT.Module.Users.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<ApplicationUser>> GetPagedAsync(int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken ct);
    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct);
    Task AddAsync(ApplicationUser user, CancellationToken ct);
    Task UpdateAsync(ApplicationUser user, CancellationToken ct);
}


