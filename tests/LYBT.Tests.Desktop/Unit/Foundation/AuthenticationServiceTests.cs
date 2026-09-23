using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// TDD Batch 7 L1 — AuthenticationService（今天修改）单元测试。
/// 覆盖 LoginAsync：调用 API、成功返回数据（供调用方保存 Token）、失败返回错误。
/// 注：AuthenticationService.LoginAsync 不负责持久化 Token（调用方 VM/Coordinator 保存），
/// 故「SavesToken」用例适配为「成功返回可保存的 LoginResponse 数据」。
/// </summary>
public class AuthenticationServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();
    private readonly IApiClientIdentity _identity = Substitute.For<IApiClientIdentity>();
    private readonly ITokenStorageService _tokenStorage = Substitute.For<ITokenStorageService>();
    private readonly ITokenValidator _tokenValidator = Substitute.For<ITokenValidator>();
    private readonly ICredentialVault _credentialVault = Substitute.For<ICredentialVault>();
    private readonly ILogger<AuthenticationService> _logger = Substitute.For<ILogger<AuthenticationService>>();

    private AuthenticationService CreateSut()
    {
        _apiClient.Identity.Returns(_identity);
        return new AuthenticationService(_apiClient, _tokenStorage, _tokenValidator, _credentialVault, _logger);
    }

    private static LoginRequest MakeRequest() => new() { UserName = "user1", Password = "pw123" };

    private static LoginResponse MakeResponse(string token = "jwt-token")
        => new()
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDetailDto { UserName = "user1" }
        };

    [Fact]
    public async Task LoginAsync_CallsApi()
    {
        var svc = CreateSut();
        var request = MakeRequest();
        _identity.LoginAsync(request).Returns(Task.FromResult(new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = MakeResponse()
        }));

        var result = await svc.LoginAsync(request);

        result.Success.Should().BeTrue();
        await _identity.Received(1).LoginAsync(request);
    }

    [Fact]
    public async Task LoginAsync_ReturnsLoginData()
    {
        // AuthenticationService.LoginAsync 返回 LoginResponse 数据（由调用方持久化 Token）
        var svc = CreateSut();
        var request = MakeRequest();
        var response = MakeResponse("saved-token");
        _identity.LoginAsync(request).Returns(Task.FromResult(new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = response
        }));

        var result = await svc.LoginAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(response);
        result.Data!.Token.Should().Be("saved-token");
    }

    [Fact]
    public async Task LoginAsync_Failure_ReturnsError()
    {
        var svc = CreateSut();
        var request = MakeRequest();
        _identity.LoginAsync(request).Returns(Task.FromResult(new ApiResponse<LoginResponse>
        {
            Success = false,
            Message = "用户名或密码错误"
        }));

        var result = await svc.LoginAsync(request);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("用户名或密码错误");
    }
}
