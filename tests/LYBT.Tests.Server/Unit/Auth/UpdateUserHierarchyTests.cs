using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Identity.Application.Commands;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Xunit;

namespace LYBT.Tests.Server.Unit.Auth;

/// <summary>
/// UPDATEUSER-HIERARCHY-FIX 层级校验测试（真机缺口：testadmin 更新另一个 Admin → 200 应拒绝——
/// CreateUser/Restore 有层级校验但 Update/Delete/ToggleStatus/BatchDelete 漏了，统一走 UserHierarchyGuard）：
/// - Admin 更新 Admin → Forbidden（仅超级管理员可管理管理员账号）
/// - Admin 更新 Doctor → 成功
/// - Admin 升级 Doctor→Admin → Unauthorized（越级）
/// - 不可自管；非管理员 → Unauthorized
/// - Delete/ToggleStatus 同样走 guard
/// </summary>
public class UpdateUserHierarchyTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();
    private static readonly Guid OtherAdminId = Guid.NewGuid();
    private static readonly Guid OperatorId = Guid.NewGuid();

    private static ApplicationUser MakeUser(Guid id, UserRole role, bool isSysAdmin = false) =>
        new()
        {
            Id = id,
            UserName = $"user{id:N}".Substring(0, 15),
            RealName = $"用户{id:N}".Substring(0, 10),
            Role = role,
            IsSysAdmin = isSysAdmin,
            Status = CommonStatus.Enabled,
            IsDeleted = false,
        };

    [Fact]
    public async Task Admin_Updates_Admin_ReturnsForbidden()
    {
        var repo = new FakeUserRepository { Target = MakeUser(OtherAdminId, UserRole.Admin) };
        var handler = new UpdateUserCommandHandler(repo);

        var result = await handler.Handle(
            new UpdateUserCommand(
                OtherAdminId,
                new UserInputDto { RealName = "新名字", Role = UserRole.Admin },
                OperatorId,
                OperatorId,
                UserRole.Admin
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse("Admin 不能管理 Admin（一级管一级 USER-D05）");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
        result.Error.Should().Contain("仅超级管理员可管理管理员账号");
    }

    [Fact]
    public async Task Admin_Updates_Doctor_Succeeds()
    {
        var repo = new FakeUserRepository { Target = MakeUser(DoctorId, UserRole.Doctor) };
        var handler = new UpdateUserCommandHandler(repo);

        var result = await handler.Handle(
            new UpdateUserCommand(
                DoctorId,
                new UserInputDto { RealName = "新名字", Role = UserRole.Doctor },
                OperatorId,
                OperatorId,
                UserRole.Admin
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue("Admin 可管理 Doctor（一级管一级允许）");
    }

    [Fact]
    public async Task Admin_Promotes_DoctorToAdmin_ReturnsUnauthorized()
    {
        var repo = new FakeUserRepository { Target = MakeUser(DoctorId, UserRole.Doctor) };
        var handler = new UpdateUserCommandHandler(repo);

        var result = await handler.Handle(
            new UpdateUserCommand(
                DoctorId,
                new UserInputDto { RealName = "新名字", Role = UserRole.Admin },
                OperatorId,
                OperatorId,
                UserRole.Admin
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized, "Admin 无权将用户提升为管理员角色");
    }

    [Fact]
    public async Task SysAdmin_Updates_Admin_Succeeds()
    {
        var repo = new FakeUserRepository { Target = MakeUser(OtherAdminId, UserRole.Admin) };
        var handler = new UpdateUserCommandHandler(repo);

        var result = await handler.Handle(
            new UpdateUserCommand(
                OtherAdminId,
                new UserInputDto { RealName = "新名字", Role = UserRole.Admin },
                OperatorId,
                OperatorId,
                UserRole.SuperAdmin
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue("sysadmin 可管理 Admin");
    }

    [Fact]
    public async Task Update_Self_ReturnsForbidden()
    {
        var repo = new FakeUserRepository { Target = MakeUser(OperatorId, UserRole.Admin) };
        var handler = new UpdateUserCommandHandler(repo);

        var result = await handler.Handle(
            new UpdateUserCommand(
                OperatorId,
                new UserInputDto { RealName = "新名字", Role = UserRole.Admin },
                OperatorId,
                OperatorId,
                UserRole.Admin
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse("不可自管（不能改自己角色/资料）");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Doctor_Updates_AnyUser_ReturnsUnauthorized()
    {
        var repo = new FakeUserRepository { Target = MakeUser(DoctorId, UserRole.Doctor) };
        var handler = new UpdateUserCommandHandler(repo);

        var result = await handler.Handle(
            new UpdateUserCommand(
                DoctorId,
                new UserInputDto { RealName = "新名字", Role = UserRole.Doctor },
                OperatorId,
                OperatorId,
                UserRole.Doctor
            ),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse("非管理员无权管理用户");
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_AdminByAdmin_ReturnsForbidden()
    {
        var repo = new FakeUserRepository { Target = MakeUser(OtherAdminId, UserRole.Admin) };
        var handler = new DeleteUserCommandHandler(repo, null!, null!);

        var result = await handler.Handle(
            new DeleteUserCommand(OtherAdminId, OperatorId, UserRole.Admin),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse("Admin 不能删除 Admin（同类排查）");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
    }

    /// <summary>手写 fake（AntiMock 惯例——不引 mock 库）</summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        public ApplicationUser? Target { get; set; }

        public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Target);

        public Task<ApplicationUser?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Target);

        public Task<PagedResult<ApplicationUser>> GetPagedAsync(
            int page,
            int pageSize,
            string? keyword,
            UserRole? role,
            CommonStatus? status,
            CancellationToken ct
        ) => Task.FromResult(new PagedResult<ApplicationUser>());

        public Task UpdateAsync(ApplicationUser user, CancellationToken ct) => Task.CompletedTask;

        // P10-1（2026-08-14）: 新增 Repository 方法（本测试路径不触发——满足接口签名）
        public Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct) =>
            Task.FromResult<ApplicationUser?>(null);
        public Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTimeOffset? lockoutEnd, CancellationToken ct) =>
            Task.CompletedTask;
        public Task ResetLoginStateAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }
}
