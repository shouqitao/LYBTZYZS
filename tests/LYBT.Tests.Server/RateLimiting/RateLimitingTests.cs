using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Server.RateLimiting;

/// <summary>
/// Rate limiting integration tests.
/// Verifies Login endpoint fixed-window limiter behavior: 5 requests / 60 second window.
/// Uses a dedicated RateLimitingFixture (with rate limiting enabled), isolated from main test suite.
///
/// Design decisions:
/// - Single test method verifies the full rate-limit lifecycle (pass -> reject -> response format -> isolation)
/// - All assertions execute sequentially in one method to avoid xUnit execution order nondeterminism
///   causing quota competition across tests
/// - Fixed-window limiter allows 5 requests per 60-second window; test must complete within one window
/// </summary>
[Collection("RateLimiting")]
public class RateLimitingTests
{
    private readonly RateLimitingFixture _fixture;
    private const string LoginEndpoint = "/api/v1/auth/login";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RateLimitingTests(RateLimitingFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies the full rate limiter lifecycle:
    ///
    /// Phase 1 - Within window: First PermitLimit (5) requests pass normally (not 429)
    /// Phase 2 - Exceeded: Request #6 returns 429 TooManyRequests
    /// Phase 3 - Response format: 429 response contains structured ApiResponse (Success=false, errorCode, retryAfter)
    /// Phase 4 - Isolation: Non-rate-limited endpoint (/api/v1/health/ping) is unaffected by Login quota exhaustion
    ///
    /// Merged into a single test method for deterministic quota consumption,
    /// avoiding xUnit execution order nondeterminism causing cross-test quota competition.
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_RateLimitLifecycle_AllowsThenRejectsAndReturnsProperFormat()
    {
        // Arrange
        var permitLimit = RateLimitingFixture.PermitLimit;
        var loginRequest = new LoginRequest
        {
            UserName = "admin",
            Password = RateLimitingFixture.SeedPassword
        };

        // ===== Phase 1: Send PermitLimit requests within window, verify all pass =====
        var passedResponses = new List<HttpResponseMessage>();
        for (var i = 0; i < permitLimit; i++)
        {
            var response = await _fixture.AnonymousClient
                .PostAsJsonAsync(LoginEndpoint, loginRequest);
            passedResponses.Add(response);
        }

        // Assert Phase 1: No request should be rate-limited
        for (var i = 0; i < passedResponses.Count; i++)
        {
            passedResponses[i].StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                $"Request #{i + 1} should not be rate-limited (within PermitLimit={permitLimit})");

            var statusCode = (int)passedResponses[i].StatusCode;
            statusCode.Should().BeOneOf(new[] { 200, 401 },
                $"Request #{i + 1} should return a normal business status code (200 or 401)");
        }

        // ===== Phase 2: Send request exceeding quota, verify rejection =====
        var exceedRequest = new LoginRequest
        {
            UserName = "admin",
            Password = "any_password_for_exceed_test"
        };

        var rejectedResponse = await _fixture.AnonymousClient
            .PostAsJsonAsync(LoginEndpoint, exceedRequest);

        // Assert Phase 2: Exceeding quota should return 429
        rejectedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            $"Request #{permitLimit + 1} should be rate-limited with 429 (exceeded PermitLimit={permitLimit})");

        // ===== Phase 3: Verify 429 response contains structured ApiResponse format =====
        var content = await rejectedResponse.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace("429 response should contain a body");

        // 3a. Verify ApiResponse base fields
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(content, JsonOptions);
        apiResponse.Should().NotBeNull("429 response should be structured ApiResponse format");
        apiResponse!.Success.Should().BeFalse("Rate-limited response Success should be false");
        apiResponse.Message.Should().NotBeNullOrWhiteSpace("Rate-limited response should contain error message");

        // 3b. Verify errors field contains errorCode and retryAfter
        // ApiResponse uses [JsonPropertyName] attributes, field names are lowercase:
        //   success, message, data, errors, timestamp, requestId
        // CreateFail places error details in errors field (not data)
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("errors", out var errorsElement).Should().BeTrue(
            "429 response errors field should contain error details (ApiResponse.CreateFail places details in Errors)");

        if (errorsElement.ValueKind == JsonValueKind.Object)
        {
            errorsElement.TryGetProperty("errorCode", out var errorCodeElement).Should().BeTrue(
                "errors should contain errorCode field");
            errorCodeElement.GetString().Should().NotBeNullOrWhiteSpace(
                "errorCode should be a non-empty string");

            errorsElement.TryGetProperty("retryAfter", out var retryAfterElement).Should().BeTrue(
                "errors should contain retryAfter field");
            retryAfterElement.GetDouble().Should().BeGreaterThan(0,
                "retryAfter should be a positive number (seconds)");
        }

        // ===== Phase 4: Verify non-rate-limited endpoint is unaffected =====
        // Login quota is exhausted, but health check endpoint has no [EnableRateLimiting] attribute
        var healthResponse = await _fixture.AnonymousClient
            .GetAsync("/api/v1/health/ping");

        healthResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "Non-rate-limited endpoint should not return 429 (Login rate limit policy only affects endpoints with [EnableRateLimiting(\"Login\")])");
    }
}
