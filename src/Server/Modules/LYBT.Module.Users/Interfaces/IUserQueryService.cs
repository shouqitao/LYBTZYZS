using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces
{
    public interface IUserQueryService
    {
        Task<PagedResult<UserDto>> GetPagedUsersAsync(UserSearchDto searchDto);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
    }
}
