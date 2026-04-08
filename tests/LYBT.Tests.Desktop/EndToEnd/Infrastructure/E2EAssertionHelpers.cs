using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

public static class E2EAssertionHelpers
{
    public static T AssertSuccess<T>(ApiResponse<T> response)
    {
        response.Should().NotBeNull();
        response.Success.Should().BeTrue(
            $"Expected success but got: {response.Message}");
        response.Data.Should().NotBeNull();
        return response.Data!;
    }

    public static void AssertError<T>(ApiResponse<T> response, string? expectedMessagePart = null)
    {
        response.Should().NotBeNull();
        response.Success.Should().BeFalse("Expected failure response");

        if (expectedMessagePart is not null)
        {
            response.Message.Should().Contain(expectedMessagePart);
        }
    }

    public static PagedResult<TItem> AssertPaged<TItem>(
        ApiResponse<PagedResult<TItem>> response,
        int? expectedMinCount = null)
    {
        var paged = AssertSuccess(response);
        paged.Items.Should().NotBeNull();

        if (expectedMinCount.HasValue)
        {
            paged.Items.Should().HaveCountGreaterThanOrEqualTo(expectedMinCount.Value);
            paged.TotalCount.Should().BeGreaterThanOrEqualTo(expectedMinCount.Value);
        }

        paged.CurrentPage.Should().BeGreaterThan(0);
        paged.PageSize.Should().BeGreaterThan(0);

        return paged;
    }

    public static async Task AssertUnauthorized(Func<Task> action)
    {
        var ex = await Assert.ThrowsAsync<Refit.ApiException>(action);
        ex.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    public static async Task AssertForbidden(Func<Task> action)
    {
        var ex = await Assert.ThrowsAsync<Refit.ApiException>(action);
        ex.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    public static async Task<Refit.ApiException> AssertApiException(
        Func<Task> action,
        System.Net.HttpStatusCode expectedStatus)
    {
        var ex = await Assert.ThrowsAsync<Refit.ApiException>(action);
        ex.StatusCode.Should().Be(expectedStatus);
        return ex;
    }
}
