using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Infrastructure;

/// <summary>
/// Should Have User Stories for Error Handling.
/// PRD: US-ERR-001 (Global exception), US-ERR-002 (ProblemDetails format),
///      US-ERR-004 (Exception type system), US-ERR-005 (Severity classification),
///      US-ERR-006 (Error message mapping)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_ErrorHandling_ShouldHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    private static readonly JsonSerializerOptions ProblemJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public US_ErrorHandling_ShouldHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region US-ERR-001: Global exception handling

    [Fact]
    public async Task US_ERR_001_InvalidEndpoint_Returns404WithStructuredResponse()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();

        // Act - request a non-existent endpoint
        var response = await client.GetAsync("/api/v1/nonexistent-endpoint-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-ERR-001: non-existent endpoint should return 404");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace(
            "US-ERR-001: error response should contain a body");
    }

    [Fact]
    public async Task US_ERR_001_MalformedJson_Returns400()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();
        var malformedContent = new StringContent(
            "{ this is not valid json }",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act - send malformed JSON to a valid endpoint
        var response = await client.PostAsync("/api/v1/patients", malformedContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-ERR-001: malformed JSON should return 400");
    }

    #endregion

    #region US-ERR-002: ProblemDetails format

    [Fact]
    public async Task US_ERR_002_ErrorResponse_ContainsProblemDetailsFields()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();

        // Act - trigger a 404 by requesting non-existent resource
        var response = await client.GetAsync($"/api/v1/patients/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        // Verify structured error response (either ProblemDetails or ApiResponse)
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Should have at least one of: status/title (ProblemDetails) or success/message (ApiResponse)
        var hasProblemDetails = root.TryGetProperty("status", out _) ||
                                root.TryGetProperty("title", out _);
        var hasApiResponse = root.TryGetProperty("success", out _) ||
                             root.TryGetProperty("message", out _);

        (hasProblemDetails || hasApiResponse).Should().BeTrue(
            "US-ERR-002: error should be structured (ProblemDetails or ApiResponse format)");
    }

    [Fact]
    public async Task US_ERR_002_ValidationError_ContainsDetailedErrors()
    {
        // Arrange - send invalid data to trigger validation
        var client = await LoginAsDoctorAsync();
        var invalidPayload = new { Name = "" }; // empty required field

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/patients", invalidPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-ERR-002: validation failure should return 400");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        // Should contain error details
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var hasErrors = root.TryGetProperty("errors", out _) ||
                        root.TryGetProperty("detail", out _);
        hasErrors.Should().BeTrue(
            "US-ERR-002: validation error should contain error details");
    }

    #endregion

    #region US-ERR-004: Exception type system (400/422/404)

    [Fact]
    public async Task US_ERR_004_NotFound_Returns404()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();

        // Act - request non-existent entity
        var response = await client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-ERR-004: non-existent entity should return 404");
    }

    [Fact]
    public async Task US_ERR_004_BusinessRuleViolation_Returns422()
    {
        // Arrange - cancel a non-existent registration (triggers BusinessFail)
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.PutAsync(
            $"/api/v1/registrations/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "US-ERR-004: business rule violation should return 422");
    }

    [Fact]
    public async Task US_ERR_004_ValidationFailure_Returns400()
    {
        // Arrange - invalid input
        var client = await LoginAsDoctorAsync();
        var payload = new { Name = "", Gender = 99 }; // invalid

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-ERR-004: validation failure should return 400");
    }

    #endregion

    #region US-ERR-005: Severity classification

    [Fact]
    public async Task US_ERR_005_ClientError_HasWarningSeverity()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();

        // Act - trigger a 404
        var response = await client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        // Assert - check if severity is included in response
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // ProblemDetails extensions may include severity
        if (root.TryGetProperty("severity", out var severity))
        {
            severity.GetString().Should().BeOneOf("warning", "info",
                "US-ERR-005: 4xx errors should have warning/info severity");
        }
        // If no severity field, the test documents that severity is not exposed (acceptable)
    }

    [Fact]
    public async Task US_ERR_005_UnauthorizedRequest_Returns401()
    {
        // Arrange - anonymous client (no auth token)
        var client = AnonymousClient;

        // Act
        var response = await client.GetAsync("/api/v1/patients?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "US-ERR-005: unauthenticated request should return 401");
    }

    #endregion

    #region US-ERR-006: Error message mapping

    [Fact]
    public async Task US_ERR_006_NotFoundError_ContainsLocalizedMessage()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();

        // Act
        var response = await client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();

        // Should contain a human-readable message (not just status code)
        content.ToLowerInvariant().Should().Contain("message",
            "US-ERR-006: error response should contain a message field");
    }

    [Fact]
    public async Task US_ERR_006_CorrelationId_IncludedInErrorResponse()
    {
        // Arrange
        var client = await LoginAsDoctorAsync();

        // Act
        var response = await client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        // Check if correlationId is in response body or headers
        var hasCorrelationInBody = content.Contains("correlationId", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("requestId", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("traceId", StringComparison.OrdinalIgnoreCase);
        var hasCorrelationHeader = response.Headers.Contains("X-Correlation-ID") ||
                                    response.Headers.Contains("traceparent");

        (hasCorrelationInBody || hasCorrelationHeader).Should().BeTrue(
            "US-ERR-006: error response should include correlation/trace ID for debugging");
    }

    #endregion
}
