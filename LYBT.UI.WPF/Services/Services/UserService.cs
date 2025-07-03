using LYBT.Module.Users.Dtos;
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class UserService : IUserService {
        private readonly IUserApi _userApi;
        public UserService(IUserApi userApi) => _userApi = userApi;

        public async Task<IList<UserDto>> SearchAsync(string keyword = "") {
            var resp = await _userApi.SearchAsync(new UserQueryDto { Keyword = keyword, Page = 1, PageSize = 100 });
            return resp.Users;
        }

        public async Task<bool> AddUserAsync(UserCreateDto user) {
            var resp = await _userApi.AddAsync(user);
            return resp.Success;
        }

        public async Task<bool> UpdateUserAsync(UserEditDto user) {
            var resp = await _userApi.UpdateAsync(user);
            return resp.Success;
        }

        public async Task<bool> DisableUserAsync(Guid id) {
            var resp = await _userApi.DisableAsync(id);
            return resp.Success;
        }

        public async Task<bool> EnableUserAsync(Guid id) {
            var resp = await _userApi.EnableAsync(id);
            return resp.Success;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var resp = await _userApi.BatchDisableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var resp = await _userApi.BatchEnableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        public async Task<bool> ResetPasswordAsync(Guid id, string newPassword) {
            var resp = await _userApi.ResetPasswordAsync(id, new ResetPasswordDto { NewPassword = newPassword });
            return resp.Success;
        }

        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var resp = await _userApi.ChangePasswordAsync(new ChangePasswordDto {
                UserId = id,
                OldPassword = oldPassword,
                NewPassword = newPassword
            });
            return resp.Success;
        }

        public async Task<IList<UserRole>> GetRolesAsync() {
            return await _userApi.GetRolesAsync();
        }

        public async Task<UserDto?> GetByIdAsync(Guid id) {
            return await _userApi.GetByIdAsync(id);
        }
    }
}
