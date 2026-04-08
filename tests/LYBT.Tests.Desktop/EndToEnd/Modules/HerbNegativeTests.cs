using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class HerbNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public HerbNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateHerb_EmptyName_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new HerbInputDto { Name = "", Unit = "克", Price = 10m };

        try
        {
            var response = await HerbApi.CreateHerbAsync(input);
            E2EAssertionHelpers.AssertError(response, "Name");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateHerb_NegativePrice_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new HerbInputDto
        {
            Name = $"负价测试_{Guid.NewGuid():N}"[..12],
            Unit = "克",
            Price = -5m
        };

        try
        {
            var response = await HerbApi.CreateHerbAsync(input);
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
    public async Task CreateHerb_PriceExceedsMax_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new HerbInputDto
        {
            Name = $"高价测试_{Guid.NewGuid():N}"[..12],
            Unit = "克",
            Price = 9_999_999m
        };

        try
        {
            var response = await HerbApi.CreateHerbAsync(input);
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
    public async Task CreateHerb_EmptyUnit_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new HerbInputDto
        {
            Name = $"无单位_{Guid.NewGuid():N}"[..12],
            Unit = "",
            Price = 10m
        };

        try
        {
            var response = await HerbApi.CreateHerbAsync(input);
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
    public async Task GetHerb_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await HerbApi.GetHerbByIdAsync(fakeId);
            response.Success.Should().BeFalse("不存在的药材ID应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
