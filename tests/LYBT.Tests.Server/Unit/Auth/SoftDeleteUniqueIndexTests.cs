using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Identity.Application.Commands;
using LYBT.Module.Identity.Infrastructure;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LYBT.Tests.Server.Unit.Auth;

/// <summary>
/// 软删实体占用唯一索引（softdelete-uniqueindex-fix 2026-08-13——真机 500 根因）:
/// 创建用户时 FindByNameAsync 走 QueryFilter（不含软删）→ 查不到软删 → INSERT 撞 UserNameIndex → 500。
/// 修复：含软删查重（IgnoreQueryFilters）→ 422 友好提示（方案 B——软删不释放索引）。
/// 真实 UserStore + SQLite（真实 SQL 引擎 + 唯一约束校验）。
/// </summary>
public class SoftDeleteUniqueIndexTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CreateUserCommandHandler _handler;

    public SoftDeleteUniqueIndexTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new IdentityDbContext(options);
        _context.Database.EnsureCreated();

        _userManager = new UserManager<ApplicationUser>(
            new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, IdentityRole<Guid>, IdentityDbContext, Guid>(_context),
            Options.Create(new IdentityOptions
            {
                User = { RequireUniqueEmail = false },
            }),
            new PasswordHasher<ApplicationUser>(),
            new[] { new UserValidator<ApplicationUser>() },
            new[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var passwordOptions = Options.Create(new DefaultPasswordOptions { NewUserPassword = "TestPass1!" });
        _handler = new CreateUserCommandHandler(_userManager, passwordOptions);
    }

    [Fact]
    public async Task Create_WithSoftDeletedUserName_Returns422WithRestoreHint()
    {
        // 先建用户（活体）
        var first = await CreateUserAsync("softdel", "softdel@test.local", UserRole.Admin);
        if (!first.IsSuccess)
            throw new InvalidOperationException($"创建失败: {first.Error} (code={first.ErrorCode})");

        // 软删
        var loaded = await _userManager.Users.SingleAsync(u => u.UserName == "softdel");
        loaded.SoftDelete(Guid.NewGuid());
        await _userManager.UpdateAsync(loaded);

        // 再次创建同名 → 必须 422 友好提示（非 500）
        var second = await _handler.Handle(
            Command("softdel", "other@test.local", UserRole.Admin), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.ErrorCode.Should().Be(ErrorCode.UserNameExists);
        second.Error.Should().Contain("已被删除").And.Contain("恢复");
    }

    [Fact]
    public async Task Create_WithSoftDeletedEmail_Returns422()
    {
        var first = await CreateUserAsync("userA", "dup@test.local", UserRole.Admin);
        if (!first.IsSuccess)
            throw new InvalidOperationException($"创建失败: {first.Error} (code={first.ErrorCode})");

        var loaded = await _userManager.Users.SingleAsync(u => u.UserName == "userA");
        loaded.SoftDelete(Guid.NewGuid());
        await _userManager.UpdateAsync(loaded);

        var second = await _handler.Handle(
            Command("userB", "dup@test.local", UserRole.Admin), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.ErrorCode.Should().Be(ErrorCode.InvalidRequest);
        second.Error.Should().Contain("邮箱").And.Contain("已删除");
    }

    [Fact]
    public async Task SoftDeletedUserName_CanBeRestored_ThenCreateRejected()
    {
        // 建 + 软删
        var first = await CreateUserAsync("restoreme", "restore@test.local", UserRole.Admin);
        if (!first.IsSuccess)
            throw new InvalidOperationException($"创建失败: {first.Error} (code={first.ErrorCode})");
        var loaded = await _userManager.Users.SingleAsync(u => u.UserName == "restoreme");
        loaded.SoftDelete(Guid.NewGuid());
        await _userManager.UpdateAsync(loaded);

        // 恢复（UserName 不变——索引行是自己的——无冲突）
        loaded.IsDeleted = false;
        var restoreResult = await _userManager.UpdateAsync(loaded);
        restoreResult.Succeeded.Should().BeTrue("软删用户恢复不应有唯一索引冲突");

        // 恢复后同名创建 → 原「用户名已存在」422
        var dup = await _handler.Handle(
            Command("restoreme", "other2@test.local", UserRole.Admin), CancellationToken.None);
        dup.IsSuccess.Should().BeFalse();
        dup.ErrorCode.Should().Be(ErrorCode.UserNameExists);
    }

    private async Task<LYBT.Shared.Models.Contracts.Common.Result<LYBT.Shared.Models.Contracts.Users.UserDetailDto>> CreateUserAsync(
        string userName, string email, UserRole role)
        => await _handler.Handle(Command(userName, email, role), CancellationToken.None);

    private static CreateUserCommand Command(string userName, string email, UserRole role)
        => new(
            new UserInputDto { UserName = userName, RealName = "测试", Email = email, Role = role },
            Guid.NewGuid(),
            UserRole.SuperAdmin);

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
