using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Identity.Application.Commands;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Tests.Server.Unit.Auth;

/// <summary>
/// USER-D04 层级校验（真机缺口 2026-08-13——sysadmin 创建 Doctor 曾 200 成功）:
/// Sysadmin → 仅可创建 Admin；Admin → 仅可创建 Doctor/Receptionist；不可创建 SuperAdmin。
/// </summary>
public class CreateUserHierarchyTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly LYBT.Module.Identity.Infrastructure.IdentityDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserHierarchyTests()
    {
        // 真实 UserStore + SQLite（软删查重走 EF Users 查询——Fake 无法模拟 IgnoreQueryFilters）
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<LYBT.Module.Identity.Infrastructure.IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new LYBT.Module.Identity.Infrastructure.IdentityDbContext(options);
        _context.Database.EnsureCreated();

        _userManager = new UserManager<ApplicationUser>(
            new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, IdentityRole<Guid>, LYBT.Module.Identity.Infrastructure.IdentityDbContext, Guid>(_context),
            Options.Create(new IdentityOptions { User = { RequireUniqueEmail = false } }),
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
    public async Task SysAdmin_Creates_Admin_Succeeds()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _userManager.Users.AnyAsync(u => u.Role == UserRole.Admin)).Should().BeTrue();
    }

    [Fact]
    public async Task SysAdmin_Creates_Doctor_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.Doctor), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden); // 403
        result.Error.Should().Contain("仅可创建 Admin");

    }

    [Fact]
    public async Task SysAdmin_Creates_Receptionist_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.Receptionist), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        result.Error.Should().Contain("仅可创建 Admin");
    }

    [Fact]
    public async Task Admin_Creates_Doctor_Succeeds()
    {
        var result = await _handler.Handle(
            Command(UserRole.Admin, UserRole.Doctor), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _userManager.Users.AnyAsync(u => u.Role == UserRole.Doctor)).Should().BeTrue();
    }

    [Fact]
    public async Task Admin_Creates_Admin_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.Admin, UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        result.Error.Should().Contain("仅可创建 Doctor/Receptionist");

    }

    [Fact]
    public async Task AnyOperator_Creates_SuperAdmin_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.SuperAdmin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        result.Error.Should().Contain("系统管理员账号");

    }

    [Fact]
    public async Task NonAdmin_CreatesUser_IsUnauthorized()
    {
        var result = await _handler.Handle(
            Command(UserRole.Doctor, UserRole.Doctor), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);

    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static CreateUserCommand Command(UserRole operatorRole, UserRole targetRole)
        => new(
            new UserInputDto
            {
                UserName = $"u{Guid.NewGuid():N}"[..10],
                RealName = "测试用户",
                Role = targetRole,
            },
            Guid.NewGuid(),
            operatorRole);
}
