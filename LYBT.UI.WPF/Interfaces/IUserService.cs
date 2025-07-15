using LYBT.Module.Users.Dtos;
using LYBT.Common.Enums.Users;
using LYBT.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IUserService {
        Task<IList<UserDto>> SearchAsync(string keyword = "");
        Task<PagedResultDto<UserDto>> SearchAsync(UserQueryDto query);
        Task<bool> AddUserAsync(UserCreateDto user);
        Task<bool> UpdateUserAsync(UserDetailDto user);
        Task<bool> DisableUserAsync(Guid id);
        Task<bool> EnableUserAsync(Guid id);
        Task<int> BatchDisableAsync(List<Guid> ids);
        Task<int> BatchEnableAsync(List<Guid> ids);
        Task<bool> ResetPasswordAsync(Guid id);
        Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
        Task<bool> ChangeProfileAsync(Guid id, string realName, string? email, string? phoneNumber);
        Task<IList<UserRole>> GetRolesAsync();
        Task<UserDto?> GetByIdAsync(Guid id);
    }
}
