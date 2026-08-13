using LYBT.Entities.Users;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Identity.Application.Mappers;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Identity.Services;

/// <summary>
/// 用户服务实现（A-31-C3a 合并 UserService + UserCrossModuleService）。
/// 读操作直查（Controller 读走 Service，写走 MediatR Handler，见蓝图 §2.2）；登录凭证方法供 LoginCommandHandler/跨模块消费。
/// P10-1（2026-08-14）: 移除 IdentityDbContext 直注——凭证/登录状态方法移入 IUserRepository（Service 不直连 DbContext）
/// </summary>
public class UserService : IUserCrossModuleService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _logger = logger;
    }

    // ── 读服务（Controller 直查）──

    public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, UserRole? role = null, CommonStatus? status = null, CancellationToken ct = default)
    {
        var result = await _userRepository.GetPagedAsync(page, pageSize, keyword, role, status, ct);
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
        // P10-1: 走 Repository（GetByIdAsync 含 !IsDeleted 过滤）
        var u = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return u == null ? null : IdentityMapper.ToBasicDto(u);
    }

    public async Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // P10-1: 走 Repository
        var u = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        return u == null ? null : IdentityMapper.ToCredentialDto(u);
    }

    public async Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        // P10-1: 走 Repository（DateTime→DateTimeOffset 转换在 Repository 内处理）
        await _userRepository.UpdateLoginFailureAsync(
            userId,
            failedLoginCount,
            lockoutEnd.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(lockoutEnd.Value, DateTimeKind.Utc))
                : (DateTimeOffset?)null,
            cancellationToken);
    }

    public async Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // P10-1: 走 Repository
        await _userRepository.ResetLoginStateAsync(userId, cancellationToken);
    }

    public async Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return false;

        return await _userManager.CheckPasswordAsync(user, password);
    }
}
