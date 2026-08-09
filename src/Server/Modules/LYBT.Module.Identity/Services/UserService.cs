using LYBT.Entities.Users;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Identity.Application.Mappers;
using LYBT.Module.Identity.Infrastructure;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Identity.Services;

/// <summary>
/// 用户服务实现（A-31-C3a 合并 UserService + UserCrossModuleService）。
/// 读操作直查（Controller 读走 Service，写走 MediatR Handler，见蓝图 §2.2）；登录凭证方法供 LoginCommandHandler/跨模块消费。
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IdentityDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IdentityDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ── 读服务（Controller 直查）──

    public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var result = await _userRepository.GetPagedAsync(page, pageSize, keyword, null, null, ct);
        var dtos = result.Items.Select(IdentityMapper.ToListDto).ToList();
        var pagedResult = new PagedResult<UserListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
        return Result<PagedResult<UserListDto>>.Success(pagedResult);
    }

    public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.UserNotFound));
        return Result<UserDetailDto>.Success(IdentityMapper.ToDetailDto(user));
    }

    public async Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "无法获取当前用户信息");

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, ErrorMessages.Get(ErrorCode.UserNotFound));

        return Result<UserDetailDto>.Success(IdentityMapper.ToDetailDto(user));
    }

    // ── 跨模块/登录凭证（原 UserCrossModuleService）──

    public async Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var u = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (u == null) return null;

        return IdentityMapper.ToBasicDto(u);
    }

    public async Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var u = await _context.Users
            .AsNoTracking()
            .Where(x => x.UserName == username && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (u == null) return null;

        return IdentityMapper.ToCredentialDto(u);
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
            user.LastLoginTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return false;

        return await _userManager.CheckPasswordAsync(user, password);
    }
}
