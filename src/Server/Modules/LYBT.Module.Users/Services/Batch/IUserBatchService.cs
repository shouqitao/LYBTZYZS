using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Users.Services.Batch
{
    /// <summary>
    /// 鐢ㄦ埛鎵归噺鎿嶄綔鏈嶅姟鎺ュ彛
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬鐢ㄦ埛鐨勬壒閲忔搷浣滃姛鑳?
    /// </summary>
    public interface IUserBatchService
    {
        /// <summary>
        /// 鎵归噺鍚敤鐢ㄦ埛
        /// </summary>
        /// <param name="ids">鐢ㄦ埛ID鍒楄〃</param>        /// <returns>褰卞搷鐨勮褰曟暟</returns>
        Task<ServiceResult<int>> BatchEnableUsersAsync(List<Guid> ids);

        /// <summary>
        /// 鎵归噺绂佺敤鐢ㄦ埛
        /// </summary>        /// <param name="ids">鐢ㄦ埛ID鍒楄〃</param>        /// <returns>褰卞搷鐨勮褰曟暟</returns>
        Task<ServiceResult<int>> BatchDisableUsersAsync(List<Guid> ids);

        /// <summary>
        /// 鎵归噺鍒犻櫎鐢ㄦ埛
        /// </summary>        /// <param name="ids">鐢ㄦ埛ID鍒楄〃</param>
        /// <returns>褰卞搷鐨勮褰曟暟</returns>
        Task<ServiceResult<int>> BatchDeleteUsersAsync(List<Guid> ids);
    }
}

