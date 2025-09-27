using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Module.Users.Repositories;

namespace LYBT.Module.Users.Services
{
    public class UserQueryService : IUserQueryService
    {
        private readonly IUserRepository _repository;

        public UserQueryService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<UserDto>> GetPagedUsersAsync(UserSearchDto searchDto)
        {
            return await Task.FromResult(new PagedResult<UserDto>());
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            return await Task.FromResult<UserDto?>(null);
        }
    }
}
