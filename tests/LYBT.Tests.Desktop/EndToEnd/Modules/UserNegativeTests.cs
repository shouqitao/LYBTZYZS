using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class UserNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;
    private static int _phoneSequence;

    public UserNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static UserInputDto CreateValidUserInput(string suffix = "") => new()
    {
        UserName = $"negtest{suffix}_{Guid.NewGuid():N}"[..20],
        Password = "TestPass123!",
        ConfirmPassword = "TestPass123!",
        RealName = $"测试用户{suffix}",
        Role = UserRole.Doctor,
        PhoneNumber = $"138{Interlocked.Increment(ref _phoneSequence):D8}",
        PinYinCode = "CSYH"
    };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateUser_DuplicateUsername_ShouldFail()
    {
        await LoginAsSysadminAsync();

        var input1 = CreateValidUserInput("dup");
        var first = await UserApi.CreateUserAsync(input1);
        first.Success.Should().BeTrue("首次创建应成功");

        var input2 = CreateValidUserInput();
        input2.UserName = input1.UserName;
        
        try
        {
            var second = await UserApi.CreateUserAsync(input2);
            E2EAssertionHelpers.AssertError(second, "已存在");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateUser_ShortPassword_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = CreateValidUserInput("sp");
        input.Password = "12";
        input.ConfirmPassword = "12";

        try
        {
            var response = await UserApi.CreateUserAsync(input);
            E2EAssertionHelpers.AssertError(response);
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateUser_PasswordMismatch_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = CreateValidUserInput("pm");
        input.ConfirmPassword = "DifferentPass456!";

        try
        {
            var response = await UserApi.CreateUserAsync(input);
            E2EAssertionHelpers.AssertError(response);
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateUser_InvalidEmail_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = CreateValidUserInput("em");
        input.Email = "not-an-email";

        try
        {
            var response = await UserApi.CreateUserAsync(input);
            E2EAssertionHelpers.AssertError(response);
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task ChangePassword_WrongOldPassword_ShouldFail()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var userId = loginResponse.User.Id;
        var request = new ChangePasswordRequest
        {
            OldPassword = "CompletelyWrongOldPass!",
            NewPassword = "NewSecurePass123!"
        };

        try
        {
            var response = await UserApi.ChangePasswordAsync(userId, request);
            response.Success.Should().BeFalse("旧密码错误时不应成功修改");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task GetUser_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await UserApi.GetUserByIdAsync(fakeId);
            response.Success.Should().BeFalse("不存在的用户ID应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
