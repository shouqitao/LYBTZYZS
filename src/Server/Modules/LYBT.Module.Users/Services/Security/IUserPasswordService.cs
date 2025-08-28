using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Users.Services.Security
{
    /// <summary>
    /// 鐢ㄦ埛瀵嗙爜绠＄悊鏈嶅姟鎺ュ彛
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬鐢ㄦ埛瀵嗙爜鐩稿叧鐨勬墍鏈夋搷浣?
    /// </summary>
    public interface IUserPasswordService
    {
        /// <summary>
        /// 鐢ㄦ埛淇敼瀵嗙爜
        /// </summary>
        /// <param name="id">鐢ㄦ埛ID</param>        /// <param name="oldPassword">鏃у瘑鐮?/param>        /// <param name="newPassword">鏂板瘑鐮?/param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 绠＄悊鍛橀噸缃敤鎴峰瘑鐮?
        /// </summary>        /// <param name="id">鐢ㄦ埛ID</param>        /// <param name="newPassword">鏂板瘑鐮?/param>
        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);
    }
}

