using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Users.Services.Security
{
    /// <summary>
    /// 用户密码管理服务接口
    /// UltraThink重构：专注于用户密码相关的所有操作
    /// </summary>
    public interface IUserPasswordService
    {
        /// <summary>
        /// 用户修改密码
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="oldPassword">旧密码</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 管理员重置用户密码
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);
    }
}