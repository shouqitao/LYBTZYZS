using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Services;

/// <summary>
/// 本地认证服务实现 - BCrypt 密码验证
/// </summary>
public class LocalAuthService : ILocalAuthService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalAuthService> _logger;
    private readonly LocalUserMapper _mapper = new();

    /// <summary>
    /// 最大失败登录次数
    /// </summary>
    private const int MaxFailedLoginCount = 5;

    /// <summary>
    /// 锁定时间（分钟）
    /// </summary>
    private const int LockoutMinutes = 15;

    public LocalAuthService(LocalDbContext context, ILogger<LocalAuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserDetailDto?> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("[LocalAuth] 登录失败 - 用户名或密码为空");
            return null;
        }

        // 查询用户（包含已禁用的用户，以便返回正确的错误信息）
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

        if (user == null)
        {
            _logger.LogWarning("[LocalAuth] 登录失败 - 用户不存在: {Username}", username);
            return null;
        }

        // 检查账户状态
        if (user.Status == CommonStatus.Disabled)
        {
            _logger.LogWarning("[LocalAuth] 登录失败 - 账户已禁用: {Username}", username);
            return null;
        }

        // 检查账户锁定
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("[LocalAuth] 登录失败 - 账户已锁定至 {LockoutEnd}: {Username}",
                user.LockoutEnd.Value, username);
            return null;
        }

        // 验证密码
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // 增加失败次数
            user.FailedLoginCount++;

            // 检查是否需要锁定
            if (user.FailedLoginCount >= MaxFailedLoginCount)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("[LocalAuth] 账户已锁定 {Minutes} 分钟: {Username}",
                    LockoutMinutes, username);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogWarning("[LocalAuth] 登录失败 - 密码错误 (失败次数: {Count}): {Username}",
                user.FailedLoginCount, username);
            return null;
        }

        // 登录成功，重置失败次数
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginTime = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalAuth] 登录成功: {Username} ({RealName})",
            username, user.RealName);

        return _mapper.ToDetailDto(user);
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        string oldPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            _logger.LogWarning("[LocalAuth] 修改密码失败 - 密码为空");
            return false;
        }

        var user = await _context.Users.FindAsync([userId], ct);
        if (user == null)
        {
            _logger.LogWarning("[LocalAuth] 修改密码失败 - 用户不存在: {UserId}", userId);
            return false;
        }

        // 验证旧密码
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        {
            _logger.LogWarning("[LocalAuth] 修改密码失败 - 旧密码错误: {Username}", user.UserName);
            return false;
        }

        // 更新密码
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalAuth] 密码修改成功: {Username}", user.UserName);
        return true;
    }
}
