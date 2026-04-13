using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// RefreshToken 仓储实现
    /// 替代 AuthService/AutoLoginService 中直接使用 AppDbContext
    /// </summary>
    internal class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(AppDbContext dbContext, ILogger<RefreshTokenRepository> logger)
            : base(dbContext, logger)
        {
            _logger = logger;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
        }

        public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<RefreshToken>> GetActiveTokensByFamilyIdAsync(string familyId, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(t => t.FamilyId == familyId && !t.IsRevoked)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(token, cancellationToken);
            _logger.LogDebug("[REPO] RefreshToken.Add({Token}) Family={FamilyId}", token.Token[..8] + "...", token.FamilyId);
        }

        public async Task UpdateRangeAsync(List<RefreshToken> tokens, CancellationToken cancellationToken = default)
        {
            _context.RefreshTokens.UpdateRange(tokens);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[REPO] RefreshToken.UpdateRange Count={Count}", tokens.Count);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
