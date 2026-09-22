using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LYBT.Tests.Desktop;

/// <summary>
/// B-07 初始管理员服务单测：用户名存在性（精确匹配）/ 探测失败 / 创建成功与失败 / 请求载荷（Admin 角色 + 确认口令）。
/// 唯一替身为 <see cref="IApiClientIdentity"/>（WPF 边界接口）。
/// </summary>
public class InitialAdminServiceTests
{
    private readonly IApiClientIdentity _identity = Substitute.For<IApiClientIdentity>();
    private readonly InitialAdminService _sut;

    public InitialAdminServiceTests()
    {
        _sut = new InitialAdminService(_identity, NullLogger<InitialAdminService>.Instance);
    }

    /// <summary>让 GetUsersAsync 返回给定用户名集合的首页，并经回调暴露调用方传入的检索关键词</summary>
    private void StubUsersPage(Action<string?> onKeyword, params string[] userNames)
    {
        _identity
            .GetUsersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                onKeyword(ci.ArgAt<string?>(2));
                return Task.FromResult(new ApiResponse<PagedResult<UserListDto>>
                {
                    Success = true,
                    Message = "操作成功",
                    Data = new PagedResult<UserListDto>(
                        userNames.Select(n => new UserListDto { UserName = n }).ToList(),
                        userNames.Length,
                        1,
                        20)
                });
            });
    }

    [Fact]
    public async Task ExistsAsync_WithExactMatch_ReturnsTrueAndProbesByUserName()
    {
        string? keyword = null;
        StubUsersPage(k => keyword = k, "admin01", "admin02");

        var result = await _sut.ExistsAsync("admin01");

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        keyword.Should().Be("admin01");
    }

    [Fact]
    public async Task ExistsAsync_WithDifferentCasing_MatchesIgnoreCase()
    {
        StubUsersPage(_ => { }, "Admin01");

        var result = await _sut.ExistsAsync("admin01");

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithFuzzyHitsOnly_ReturnsFalse()
    {
        // 服务端关键词检索是模糊匹配——只有用户名完全一致才算已存在
        StubUsersPage(_ => { }, "admin012", "superadmin01");

        var result = await _sut.ExistsAsync("admin01");

        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenApiReportsFailure_ReturnsFailureWithMessage()
    {
        _identity
            .GetUsersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ApiResponse<PagedResult<UserListDto>>.CreateFail("本地服务未启动")));

        var result = await _sut.ExistsAsync("admin01");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("本地服务未启动");
    }

    [Fact]
    public async Task ExistsAsync_WhenApiThrows_ReturnsFailureWithExceptionMessage()
    {
        _identity
            .GetUsersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ApiResponse<PagedResult<UserListDto>>>(new HttpRequestException("连接被拒绝")));

        var result = await _sut.ExistsAsync("admin01");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("连接被拒绝");
    }

    [Fact]
    public async Task CreateAdminAsync_SendsAdminRoleWithConfirmedPassword()
    {
        UserInputDto? captured = null;
        _identity
            .CreateUserAsync(Arg.Any<UserInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<UserInputDto>();
                return Task.FromResult(ApiResponse<UserDetailDto>.CreateSuccess());
            });

        var result = await _sut.CreateAdminAsync("admin01", "张三", "P@ssw0rd!");

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.UserName.Should().Be("admin01");
        captured.RealName.Should().Be("张三");
        captured.Password.Should().Be("P@ssw0rd!");
        captured.ConfirmPassword.Should().Be("P@ssw0rd!");
        captured.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task CreateAdminAsync_WhenApiReportsFailure_ReturnsFailureWithMessage()
    {
        _identity
            .CreateUserAsync(Arg.Any<UserInputDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ApiResponse<UserDetailDto>.CreateFail("用户名已存在")));

        var result = await _sut.CreateAdminAsync("admin01", "张三", "P@ssw0rd!");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("用户名已存在");
    }

    [Fact]
    public async Task CreateAdminAsync_WhenApiThrows_ReturnsFailureWithExceptionMessage()
    {
        _identity
            .CreateUserAsync(Arg.Any<UserInputDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ApiResponse<UserDetailDto>>(new HttpRequestException("本地服务未启动")));

        var result = await _sut.CreateAdminAsync("admin01", "张三", "P@ssw0rd!");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("本地服务未启动");
    }
}
