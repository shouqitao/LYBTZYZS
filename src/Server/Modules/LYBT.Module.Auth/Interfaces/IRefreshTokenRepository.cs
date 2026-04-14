using LYBT.Entities.Auth;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// RefreshToken 仓储接口
    /// 负责 RefreshToken 的持久化操作，替代 AuthService 中直接使用 AppDbContext
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// 根据 Token 值获取 RefreshToken
        /// </summary>
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有活跃 Token（未撤销且未过期）
        /// </summary>
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定 Family 的所有未撤销 Token
        /// </summary>
        Task<List<RefreshToken>> GetActiveTokensByFamilyIdAsync(string familyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加新的 RefreshToken
        /// </summary>
        Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量更新 Token（用于批量撤销）
        /// </summary>
        Task UpdateRangeAsync(List<RefreshToken> tokens, CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
