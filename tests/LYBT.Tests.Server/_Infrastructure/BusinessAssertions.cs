using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Business-specific assertion extensions.
/// Eliminates false positives by enforcing business data validation,
/// not just HTTP status code checks.
/// </summary>
public static class BusinessAssertions
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Assert HTTP 200 + Success=true + Data is not null.
    /// Returns the deserialized Data for further assertions.
    /// </summary>
    public static async Task<T> ShouldBeSuccessWithDataAsync<T>(
        this HttpResponseMessage response, string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because ?? "API call should succeed");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
        body.Should().NotBeNull("response body should be deserializable");
        body!.Success.Should().BeTrue(because ?? "API should indicate success");
        body.Data.Should().NotBeNull(because ?? "response should contain data");
        return body.Data!;
    }

    /// <summary>
    /// Assert HTTP 201 Created + Success=true + Data is not null.
    /// </summary>
    public static async Task<T> ShouldBeCreatedWithDataAsync<T>(
        this HttpResponseMessage response, string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because ?? "resource should be created");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue(because ?? "creation should succeed");
        body.Data.Should().NotBeNull(because ?? "created resource should be returned");
        return body.Data!;
    }

    /// <summary>
    /// Assert paginated response with items.
    /// Returns the paged result for further assertions.
    /// </summary>
    public static async Task<PagedResult<T>> ShouldBePagedResultAsync<T>(
        this HttpResponseMessage response,
        int? expectedMinCount = null,
        string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<T>>>(JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Items.Should().NotBeNull();

        if (expectedMinCount.HasValue)
        {
            body.Data.Items.Should().HaveCountGreaterOrEqualTo(
                expectedMinCount.Value, because);
        }

        return body.Data;
    }

    /// <summary>
    /// Assert business error with expected status code and message contains.
    /// </summary>
    public static async Task ShouldBeBusinessErrorAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? messageContains = null)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var content = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse("business error should indicate failure");

        if (messageContains != null)
        {
            body.Message.Should().Contain(messageContains,
                $"error message should contain '{messageContains}'");
        }
    }

    /// <summary>
    /// Assert HTTP 401 Unauthorized.
    /// </summary>
    public static void ShouldBeUnauthorized(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Assert HTTP 403 Forbidden.
    /// </summary>
    public static void ShouldBeForbidden(this HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Assert HTTP 404 Not Found with business error.
    /// </summary>
    public static async Task ShouldBeNotFoundAsync(
        this HttpResponseMessage response, string? messageContains = null)
    {
        await response.ShouldBeBusinessErrorAsync(
            HttpStatusCode.NotFound, messageContains);
    }

    /// <summary>
    /// Assert successful response (HTTP 200 + Success=true) without data check.
    /// Useful for delete/update operations that return success without data.
    /// </summary>
    public static async Task ShouldBeSuccessAsync(
        this HttpResponseMessage response, string? because = null)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because ?? "API call should succeed");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<object>>(JsonOpts);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue(because ?? "API should indicate success");
    }
}
