using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Dtos;

namespace LYBT.UI.WPF.Services {
    public interface IUserManagementService {
        Task<bool> AddAsync(UserCreateDto dto);
        Task<bool> DisableAsync(Guid id);
        Task<bool> EnableAsync(Guid id);
        Task<List<UserRole>> GetRolesAsync();
        Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query);
        Task<bool> UpdateAsync(UserEditDto dto);
    }
}