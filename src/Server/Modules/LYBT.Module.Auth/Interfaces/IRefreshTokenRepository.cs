using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Entities.Auth;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 刷新令牌仓储接口
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// 添加刷新令牌
        /// </summary>
        Task<RefreshToken> AddAsync(RefreshToken refreshToken);

        /// <summary>
        /// 根据令牌值获取
        /// </summary>
        Task<RefreshToken?> GetByTokenAsync(string token);

        /// <summary>
        /// 根据JTI获取
        /// </summary>
        Task<RefreshToken?> GetByJtiAsync(string jti);

        /// <summary>
        /// 获取用户的所有活跃令牌
        /// </summary>
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);

        /// <summary>
        /// 获取用户的所有令牌
        /// </summary>
        Task<IEnumerable<RefreshToken>> GetAllTokensByUserIdAsync(Guid userId);

        /// <summary>
        /// 更新刷新令牌
        /// </summary>
        Task UpdateAsync(RefreshToken refreshToken);

        /// <summary>
        /// 撤销令牌
        /// </summary>
        Task RevokeAsync(string token, string reason, Guid? revokedByUserId = null);

        /// <summary>
        /// 撤销用户的所有令牌
        /// </summary>
        Task RevokeAllByUserIdAsync(Guid userId, string reason, Guid? revokedByUserId = null);

        /// <summary>
        /// 删除过期的令牌
        /// </summary>
        Task<int> DeleteExpiredTokensAsync();

        /// <summary>
        /// 获取用户的令牌数量
        /// </summary>
        Task<int> GetActiveTokenCountByUserIdAsync(Guid userId);

        /// <summary>
        /// 检查令牌是否存在且有效
        /// </summary>
        Task<bool> IsValidTokenAsync(string token);

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}