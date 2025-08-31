using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Users.Services.Account
{
    /// <summary>
    /// 用户账户状态管理服务接口
    /// UltraThink重构：专注于用户账户状态和个人资料管理
    /// </summary>
    public interface IUserAccountService
    {
        /// <summary>
        /// 启用用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> EnableUserAsync(Guid id);

        /// <summary>
        /// 禁用用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> DisableUserAsync(Guid id);

        /// <summary>
        /// 用户修改个人资料
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="realName">真实姓名</param>
        /// <param name="phoneNumber">电话号码</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> ChangeProfileAsync(Guid id, string realName, string phoneNumber);
    }
}