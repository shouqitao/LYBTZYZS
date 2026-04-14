using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// AutoLoginToken 仓储实现
    /// 继承 BaseRepository，遵循 P-02 架构规则
    /// </summary>
    internal class AutoLoginTokenRepository : BaseRepository<AutoLoginToken>, IAutoLoginTokenRepository
    {
        public AutoLoginTokenRepository(AppDbContext dbContext, ILogger<AutoLoginTokenRepository> logger)
            : base(dbContext, logger)
        {
        }

        public async Task<AutoLoginToken?> GetByTokenAndUsernameAsync(string token, string userName, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t =>
                    t.Token == token &&
                    t.UserName.ToLower() == userName.ToLower(),
                    cancellationToken);
        }

        public async Task<List<AutoLoginToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AutoLoginToken>> GetActiveTokensByFamilyIdAsync(string familyId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.FamilyId == familyId && !t.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public override async Task AddAsync(AutoLoginToken token, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(token, cancellationToken);
            _logger.LogDebug("[REPO] AutoLoginToken.Add UserId={UserId} Family={FamilyId}", token.UserId, token.FamilyId);
        }

        public async Task UpdateRangeAsync(List<AutoLoginToken> tokens, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(tokens);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[REPO] AutoLoginToken.UpdateRange Count={Count}", tokens.Count);
        }

        public override async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
