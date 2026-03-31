using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// AutoLoginToken 仓储实现
    /// 替代 AutoLoginService 中直接使用 AppDbContext
    /// </summary>
    internal class AutoLoginTokenRepository : IAutoLoginTokenRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AutoLoginTokenRepository> _logger;

        public AutoLoginTokenRepository(AppDbContext dbContext, ILogger<AutoLoginTokenRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<AutoLoginToken?> GetByTokenAndUsernameAsync(string token, string userName, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<AutoLoginToken>()
                .FirstOrDefaultAsync(t =>
                    t.Token == token &&
                    t.UserName.ToLower() == userName.ToLower(),
                    cancellationToken);
        }

        public async Task<List<AutoLoginToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<AutoLoginToken>()
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AutoLoginToken>> GetActiveTokensByFamilyIdAsync(string familyId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<AutoLoginToken>()
                .Where(t => t.FamilyId == familyId && !t.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(AutoLoginToken token, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<AutoLoginToken>().AddAsync(token, cancellationToken);
            _logger.LogDebug("[REPO] AutoLoginToken.Add UserId={UserId} Family={FamilyId}", token.UserId, token.FamilyId);
        }

        public async Task UpdateRangeAsync(List<AutoLoginToken> tokens, CancellationToken cancellationToken = default)
        {
            _dbContext.Set<AutoLoginToken>().UpdateRange(tokens);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[REPO] AutoLoginToken.UpdateRange Count={Count}", tokens.Count);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
