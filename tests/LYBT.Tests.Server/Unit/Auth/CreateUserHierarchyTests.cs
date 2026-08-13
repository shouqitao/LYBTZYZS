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

namespace LYBT.Tests.Server.Unit.Auth;

/// <summary>
/// USER-D04 层级校验（真机缺口 2026-08-13——sysadmin 创建 Doctor 曾 200 成功）:
/// Sysadmin → 仅可创建 Admin；Admin → 仅可创建 Doctor/Receptionist；不可创建 SuperAdmin。
/// </summary>
public class CreateUserHierarchyTests : IDisposable
{
    private readonly FakeUserManager _userManager = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserHierarchyTests()
    {
        var passwordOptions = Options.Create(new DefaultPasswordOptions { NewUserPassword = "TestPass1!" });
        _handler = new CreateUserCommandHandler(_userManager, passwordOptions);
    }

    [Fact]
    public async Task SysAdmin_Creates_Admin_Succeeds()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userManager.Created.Should().NotBeNull();
        _userManager.Created!.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task SysAdmin_Creates_Doctor_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.Doctor), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden); // 403
        result.Error.Should().Contain("仅可创建 Admin");
        _userManager.Created.Should().BeNull("sysadmin 创建 Doctor 必须被拦截");
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
        _userManager.Created!.Role.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public async Task Admin_Creates_Admin_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.Admin, UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        result.Error.Should().Contain("仅可创建 Doctor/Receptionist");
        _userManager.Created.Should().BeNull();
    }

    [Fact]
    public async Task AnyOperator_Creates_SuperAdmin_IsForbidden()
    {
        var result = await _handler.Handle(
            Command(UserRole.SuperAdmin, UserRole.SuperAdmin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        result.Error.Should().Contain("系统管理员账号");
        _userManager.Created.Should().BeNull();
    }

    [Fact]
    public async Task NonAdmin_CreatesUser_IsUnauthorized()
    {
        var result = await _handler.Handle(
            Command(UserRole.Doctor, UserRole.Doctor), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
        _userManager.Created.Should().BeNull();
    }

    public void Dispose() => _userManager.Dispose();

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

/// <summary>UserManager 手写替身（Server 零 mock 约定——override 所需方法）</summary>
internal sealed class FakeUserManager : UserManager<ApplicationUser>
{
    public ApplicationUser? Existing { get; set; }
    public ApplicationUser? Created { get; private set; }
    public bool CreateSucceeds { get; set; } = true;

    public FakeUserManager() : base(
        new NullUserStore(),
        null!,
        new PasswordHasher<ApplicationUser>(),
        Array.Empty<IUserValidator<ApplicationUser>>(),
        Array.Empty<IPasswordValidator<ApplicationUser>>(),
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        null!,
        NullLogger<UserManager<ApplicationUser>>.Instance)
    {
    }

    public override Task<ApplicationUser?> FindByNameAsync(string userName) => Task.FromResult(Existing);

    public override Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        Created = user;
        return Task.FromResult(
            CreateSucceeds ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError { Description = "模拟创建失败" }));
    }
}

/// <summary>最小 IUserStore 空实现（UserManager 构造仅需类型——方法不被调用）</summary>
internal sealed class NullUserStore : IUserStore<ApplicationUser>
{
    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) => throw new NotSupportedException();
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) => throw new NotSupportedException();
    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct) => throw new NotSupportedException();
    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct) => throw new NotSupportedException();
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Id.ToString());
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName);
    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult<string?>(user.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) => throw new NotSupportedException();
    public void Dispose() { }
}
