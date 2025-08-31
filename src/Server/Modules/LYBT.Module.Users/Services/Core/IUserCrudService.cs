using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Services.Core
{
    /// <summary>
    /// 鐢ㄦ埛鍩虹CRUD鎿嶄綔鏈嶅姟鎺ュ彛
    /// UltraThink閲嶆瀯锛氬崟涓€鑱岃矗鍘熷垯锛屽彧璐熻矗鐢ㄦ埛鐨勫熀纭€澧炲垹鏀规煡鎿嶄綔
    /// </summary>
    public interface IUserCrudService
    {
        /// <summary>
        /// 鍒涘缓鐢ㄦ埛
        /// </summary>
        /// <param name="dto">鍒涘缓鐢ㄦ埛DTO</param>        /// <returns>鍒涘缓鐨勭敤鎴稤TO</returns>
        Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto);

        /// <summary>
        /// 鏇存柊鐢ㄦ埛淇℃伅
        /// </summary>        /// <param name="id">鐢ㄦ埛ID</param>        /// <param name="dto">鏇存柊鐢ㄦ埛DTO</param>        /// <returns>鏇存柊鐨勭敤鎴稤TO</returns>
        Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto dto);

        /// <summary>
        /// 鍒犻櫎鐢ㄦ埛
        /// </summary>        /// <param name="id">鐢ㄦ埛ID</param>
        /// <returns>鍒犻櫎缁撴灉</returns>
        Task<ServiceResult<bool>> DeleteUserAsync(Guid id);

    }
}

