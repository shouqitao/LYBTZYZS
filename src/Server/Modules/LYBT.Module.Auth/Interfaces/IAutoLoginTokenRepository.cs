using LYBT.Entities.Auth;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// AutoLoginToken 仓储接口
    /// 负责 AutoLoginToken 的持久化操作，替代 AutoLoginService 中直接使用 AppDbContext
    /// </summary>
    public interface IAutoLoginTokenRepository
    {
        /// <summary>
        /// 根据 Token 和用户名获取 AutoLoginToken
        /// </summary>
        Task<AutoLoginToken?> GetByTokenAndUsernameAsync(string token, string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有未撤销 AutoLoginToken
        /// </summary>
        Task<List<AutoLoginToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定 Family 的所有未撤销 Token
        /// </summary>
        Task<List<AutoLoginToken>> GetActiveTokensByFamilyIdAsync(string familyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加新的 AutoLoginToken
        /// </summary>
        Task AddAsync(AutoLoginToken token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量更新 Token（用于批量撤销）
        /// </summary>
        Task UpdateRangeAsync(List<AutoLoginToken> tokens, CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存更改
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
