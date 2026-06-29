using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services;

/// <summary>
/// 用户跨模块服务实现
/// 替代 CrossModuleService 中的用户查询逻辑
/// </summary>
public class UserCrossModuleService : IUserCrossModuleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserCrossModuleService> _logger;

    public UserCrossModuleService(IDbContextAccessor dbAccessor, ILogger<UserCrossModuleService> logger)
    {
        _context = dbAccessor.Context;
        _logger = logger;
    }

    public async Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var u = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (u == null) return null;

        return new UserBasicDto
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            RealName = u.RealName,
            Role = u.Role,
            Status = u.Status,
            PhoneNumber = u.PhoneNumber,
            Email = u.Email,
            PinYinCode = u.PinYinCode,
            LastLoginTime = u.LastLoginAt,
            FailedLoginCount = u.AccessFailedCount,
            LockoutEnd = u.LockoutEnd?.UtcDateTime,
            MustChangeOnNextLogin = u.MustChangeOnNextLogin,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            Remark = u.Remark
        };
    }

    public async Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var u = await _context.Users
            .AsNoTracking()
            .Where(x => x.UserName == username && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (u == null) return null;

        return new UserCredentialDto
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            RealName = u.RealName,
            Role = u.Role,
            Status = u.Status,
            PhoneNumber = u.PhoneNumber,
            Email = u.Email,
            PinYinCode = u.PinYinCode,
            LastLoginTime = u.LastLoginAt,
            FailedLoginCount = u.AccessFailedCount,
            LockoutEnd = u.LockoutEnd?.UtcDateTime,
            MustChangeOnNextLogin = u.MustChangeOnNextLogin,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            Remark = u.Remark,
            PasswordHash = u.PasswordHash ?? string.Empty
        };
    }

    public async Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
    }

    public async Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user != null)
        {
            user.AccessFailedCount = failedLoginCount;
            user.LockoutEnd = lockoutEnd.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(lockoutEnd.Value, DateTimeKind.Utc))
                : (DateTimeOffset?)null;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user != null)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
