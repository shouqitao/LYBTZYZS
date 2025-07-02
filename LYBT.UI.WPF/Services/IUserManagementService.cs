using LYBT.Module.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface IUserManagementService {
        Task<List<UserModel>> GetUsersAsync(string keyword = "");
        Task<bool> AddUserAsync(UserModel user);
        Task<bool> UpdateUserAsync(Guid id, UserModel user);
        Task<bool> DisableUserAsync(Guid id);
        Task<bool> ResetPasswordAsync(Guid id);
    }
}
