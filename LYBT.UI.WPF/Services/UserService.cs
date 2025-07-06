using LYBT.Module.Users.Dtos;
using LYBT.Common.Enums.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LYBT.UI.WPF.Apis;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 UserService 的说明
    /// </summary>
    public class UserService : Interfaces.IUserService {
        private readonly IUserApi _userApi;
        public UserService(IUserApi userApi) => _userApi = userApi;

        /// <summary>
        /// 方法 SearchAsync 的说明
        /// </summary>
        public async Task<IList<UserDto>> SearchAsync(string keyword = "") {
            var resp = await _userApi.SearchAsync(new UserQueryDto { Keyword = keyword, Page = 1, PageSize = 100 });
            return resp.Users;
        }

        /// <summary>
        /// 方法 AddUserAsync 的说明
        /// </summary>
        public async Task<bool> AddUserAsync(UserCreateDto user) {
            var resp = await _userApi.AddAsync(user);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateUserAsync 的说明
        /// </summary>
        public async Task<bool> UpdateUserAsync(UserDetailDto user) {
            var resp = await _userApi.UpdateAsync(user);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DisableUserAsync 的说明
        /// </summary>
        public async Task<bool> DisableUserAsync(Guid id) {
            var resp = await _userApi.DisableAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 EnableUserAsync 的说明
        /// </summary>
        public async Task<bool> EnableUserAsync(Guid id) {
            var resp = await _userApi.EnableAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 BatchDisableAsync 的说明
        /// </summary>
        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var resp = await _userApi.BatchDisableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 BatchEnableAsync 的说明
        /// </summary>
        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var resp = await _userApi.BatchEnableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 ResetPasswordAsync 的说明
        /// </summary>
        public async Task<bool> ResetPasswordAsync(Guid id) {
            var resp = await _userApi.ResetPasswordAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 ChangePasswordAsync 的说明
        /// </summary>
        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var resp = await _userApi.ChangePasswordAsync(new ChangePasswordDto {
                UserId = id,
                OldPassword = oldPassword,
                NewPassword = newPassword
            });
            return resp.Success;
        }

        /// <summary>
        /// 修改个人信息
        /// </summary>
        public async Task<bool> ChangeProfileAsync(Guid id, string realName, string? email, string? phoneNumber) {
            var resp = await _userApi.ChangeProfileAsync(new ChangeProfileDto {
                UserId = id,
                RealName = realName,
                Email = email,
                PhoneNumber = phoneNumber
            });
            return resp.Success;
        }

        /// <summary>
        /// 方法 GetRolesAsync 的说明
        /// </summary>
        public async Task<IList<UserRole>> GetRolesAsync() {
            return await _userApi.GetRolesAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<UserDto?> GetByIdAsync(Guid id) {
            return await _userApi.GetByIdAsync(id);
        }
    }
}
