using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces
{
    public interface IUserQueryService
    {
        Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Result<List<UserListDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    }
}
