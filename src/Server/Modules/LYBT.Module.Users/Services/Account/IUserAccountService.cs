using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Users.Services.Account
{
    /// <summary>
    /// 鐢ㄦ埛璐︽埛鐘舵€佺鐞嗘湇鍔℃帴鍙?
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬鐢ㄦ埛璐︽埛鐘舵€佸拰涓汉璧勬枡绠＄悊
    /// </summary>
    public interface IUserAccountService
    {
        /// <summary>
        /// 鍚敤鐢ㄦ埛
        /// </summary>
        /// <param name="id">鐢ㄦ埛ID</param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> EnableUserAsync(Guid id);

        /// <summary>
        /// 绂佺敤鐢ㄦ埛
        /// </summary>        /// <param name="id">鐢ㄦ埛ID</param>        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> DisableUserAsync(Guid id);

        /// <summary>
        /// 鐢ㄦ埛淇敼涓汉璧勬枡
        /// </summary>        /// <param name="id">鐢ㄦ埛ID</param>        /// <param name="realName">鐪熷疄濮撳悕</param>        /// <param name="phoneNumber">鐢佃瘽鍙风爜</param>
        /// <returns>鎿嶄綔缁撴灉</returns>
        Task<ServiceResult<bool>> ChangeProfileAsync(Guid id, string realName, string phoneNumber);
    }
}

