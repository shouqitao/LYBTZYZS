using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地用户数据源实现 - SQLite EF Core
/// OpenSpec: implement-local-mode
/// </summary>
public class LocalUserDataSource : IUserDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalUserDataSource> _logger;
    private readonly LocalUserMapper _mapper = new();

    public LocalUserDataSource(LocalDbContext context, ILogger<LocalUserDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetById - Id={Id}", id);
        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<UserDetailDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetByUsername - Username={Username}", username);

        if (string.IsNullOrWhiteSpace(username))
            return null;

        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<UserDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.GetPaged - Page={Page}, Keyword={Keyword}", page, keyword);

        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.UserName.Contains(keyword) ||
                u.RealName.Contains(keyword) ||
                (u.PinYinCode != null && u.PinYinCode.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Create - Username={Username}", input.UserName);

        // 检查用户名是否已存在
        var exists = await _context.Users
            .AnyAsync(u => u.UserName == input.UserName, ct);

        if (exists)
            throw new InvalidOperationException($"用户名已存在: {input.UserName}");

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        entity.Status = CommonStatus.Enabled;

        // 密码哈希处理
        if (!string.IsNullOrEmpty(input.Password))
        {
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);
        }

        _context.Users.Add(entity);
        await _context.SaveChangesAsync(ct);

        return _mapper.ToDetailDto(entity);
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto input, CancellationToken ct = default)
    {
        var id = input.Id ?? throw new InvalidOperationException("更新用户时必须提供ID");
        _logger.LogInformation("[LocalDataSource] User.Update - Id={Id}", id);

        var existing = await _context.Users.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"用户不存在: {id}");

        // 检查用户名是否被其他用户使用
        if (input.UserName != null)
        {
            var nameConflict = await _context.Users
                .AnyAsync(u => u.UserName == input.UserName && u.Id != id, ct);

            if (nameConflict)
                throw new InvalidOperationException($"用户名已被使用: {input.UserName}");
        }

        // 保留密码哈希（不通过此方法更新密码）
        var passwordHash = existing.PasswordHash;

        // 更新可变字段
        if (input.UserName != null) existing.UserName = input.UserName;
        if (input.RealName != null) existing.RealName = input.RealName;
        existing.PinYinCode = input.PinYinCode;
        existing.PhoneNumber = input.PhoneNumber;
        existing.Email = input.Email;
        if (input.Role.HasValue) existing.Role = input.Role.Value;
        existing.Remark = input.Remark;
        existing.UpdatedAt = DateTime.Now;

        // 确保密码不被覆盖
        existing.PasswordHash = passwordHash;

        await _context.SaveChangesAsync(ct);
        return _mapper.ToDetailDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.Delete - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // 不允许删除管理员
        if (entity.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("[LocalDataSource] User.Delete - Cannot delete SuperAdmin: {Id}", id);
            return false;
        }

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(
        Guid id,
        string oldPasswordHash,
        string newPasswordHash,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.ChangePassword - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // 验证旧密码
        if (!BCrypt.Net.BCrypt.Verify(oldPasswordHash, entity.PasswordHash))
        {
            _logger.LogWarning("[LocalDataSource] User.ChangePassword - Invalid old password: {Id}", id);
            return false;
        }

        // 更新密码
        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPasswordHash);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] User.ChangePassword succeeded - Id={Id}", id);
        return true;
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] User.ToggleStatus - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        // 不允许禁用管理员
        if (entity.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("[LocalDataSource] User.ToggleStatus - Cannot toggle SuperAdmin status: {Id}", id);
            return false;
        }

        entity.Status = entity.Status == CommonStatus.Enabled
            ? CommonStatus.Disabled
            : CommonStatus.Enabled;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateLastLoginTimeAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.UpdateLastLoginTime - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.LastLoginTime = DateTime.Now;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResetFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.ResetFailedLoginCount - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.FailedLoginCount = 0;
        entity.LockoutEnd = null;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> IncrementFailedLoginCountAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] User.IncrementFailedLoginCount - Id={Id}", id);

        var entity = await _context.Users.FindAsync([id], ct);
        if (entity == null)
            return 0;

        entity.FailedLoginCount++;
        await _context.SaveChangesAsync(ct);
        return entity.FailedLoginCount;
    }
}
