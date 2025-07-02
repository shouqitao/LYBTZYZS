using LYBT.Module.Users.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface IUserManagementService {
        Task<IList<UserDto>> SearchAsync(string keyword = "");
        Task<bool> AddUserAsync(UserCreateDto user);
        Task<bool> UpdateUserAsync(UserEditDto user);
        Task<bool> DisableUserAsync(Guid id);
        Task<bool> ResetPasswordAsync(Guid id, string newPassword);
    }
}
