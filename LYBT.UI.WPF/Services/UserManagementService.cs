using LYBT.Module.Users.Models;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class UserManagementService : IUserManagementService {
        private readonly IUserApi _userApi;
        public UserManagementService(IUserApi userApi) => _userApi = userApi;

        public Task<List<UserModel>> GetUsersAsync(string keyword = "") => _userApi.GetUsersAsync(keyword);
        public Task<bool> AddUserAsync(UserModel user) => _userApi.AddUserAsync(user);
        public Task<bool> UpdateUserAsync(Guid id, UserModel user) => _userApi.UpdateUserAsync(id, user);
        public Task<bool> DisableUserAsync(Guid id) => _userApi.DisableUserAsync(id);
        public Task<bool> ResetPasswordAsync(Guid id) => _userApi.ResetPasswordAsync(id);
    }
}
