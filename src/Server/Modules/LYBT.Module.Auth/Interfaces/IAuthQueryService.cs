using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Auth.Interfaces {

    /// <summary>
    /// 认证查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// </summary>
    public interface IAuthQueryService {

        /// <summary>
        /// 根据用户名获取用户信息（用于认证）
        /// </summary>
        Task<ServiceResult<User>> GetUserForAuthenticationAsync(string username);

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        Task<ServiceResult<bool>> ValidateTokenAsync(string token);

        /// <summary>
        /// 获取会话信息
        /// </summary>
        Task<ServiceResult<object>> GetSessionInfoAsync(string token);

        /// <summary>
        /// 根据用户ID获取当前用户信息
        /// </summary>
        Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId);

        /// <summary>
        /// 从Token中提取用户ID
        /// </summary>
        string ExtractUserIdFromToken(string token);
    }
}
