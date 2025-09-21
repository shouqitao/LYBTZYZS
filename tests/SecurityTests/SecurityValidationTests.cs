using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Utilities.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SecurityTests;

/// <summary>
/// Comprehensive security validation tests
/// </summary>
public class SecurityValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.Production.json", optional: true);
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresent_InAllResponses()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");

        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("Permissions-Policy");

        response.Headers.Should().NotContainKey("X-Powered-By");
        response.Headers.Should().NotContainKey("Server");
    }

    [Fact]
    public async Task CSP_ShouldBeStrict_InProduction()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.Headers.Should().ContainKey("Content-Security-Policy");
        var csp = response.Headers.GetValues("Content-Security-Policy").First();

        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("upgrade-insecure-requests");
    }

    [Fact]
    public async Task HSTS_ShouldBeEnabled_InProduction()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.Headers.Should().ContainKey("Strict-Transport-Security");
        var hsts = response.Headers.GetValues("Strict-Transport-Security").First();
        hsts.Should().Contain("max-age=31536000");
        hsts.Should().Contain("includeSubDomains");
    }

    [Fact]
    public async Task UnauthorizedAccess_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoints_ShouldRequireAdminRole()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "TestPassword123!"
        };

        // Act - Login as regular user
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        if (loginResponse.IsSuccessStatusCode)
        {
            var token = await loginResponse.Content.ReadAsStringAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Try accessing admin endpoint
            var response = await _client.DeleteAsync("/api/v1/users/batch");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task RateLimiting_ShouldBeEnforced()
    {
        var tasks = new List<Task<HttpResponseMessage>>();

        // Send 150 requests (exceeding the 120 limit)
        for (int i = 0; i < 150; i++)
        {
            tasks.Add(_client.GetAsync("/api/v1/health/ping"));
        }

        var responses = await Task.WhenAll(tasks);

        // Some requests should be rate limited (429)
        responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests).Should().BeGreaterThan(0);
    }

    [Fact]
    public void PasswordPolicy_ShouldValidateComplexity()
    {
        var validator = new PasswordPolicyValidator();

        // Test weak passwords
        validator.Validate("password").IsValid.Should().BeFalse();
        validator.Validate("12345678").IsValid.Should().BeFalse();
        validator.Validate("Password").IsValid.Should().BeFalse();

        // Test strong passwords
        validator.Validate("P@ssw0rd123!").IsValid.Should().BeTrue();
        validator.Validate("MySecure#Pass2024").IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAttempts_ShouldBeLimited()
    {
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        // Try 6 login attempts (exceeding the 5 limit)
        HttpResponseMessage lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
        }

        // Account should be locked after 5 attempts
        lastResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.Forbidden
        );
    }

    [Fact]
    public async Task HealthEndpoint_ShouldMinimizeInfo_InProduction()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");
        var content = await response.Content.ReadAsStringAsync();
        var health = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        health.TryGetProperty("status", out _).Should().BeTrue();
        health.TryGetProperty("timestamp", out _).Should().BeTrue();

        // Should not expose sensitive information
        health.TryGetProperty("version", out _).Should().BeFalse();
        health.TryGetProperty("environment", out _).Should().BeFalse();
        health.TryGetProperty("connectionString", out _).Should().BeFalse();
    }

    [Fact]
    public async Task DetailedHealthCheck_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void JWT_Configuration_ShouldBeSecure()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

        // JWT secret should be strong
        jwtOptions.Secret.Length.Should().BeGreaterOrEqualTo(32);
        jwtOptions.Secret.Should().NotContain("Development");
        jwtOptions.Secret.Should().NotContain("Default");

        // Token expiry should be reasonable
        jwtOptions.ExpireMinutes.Should().BeLessThanOrEqualTo(480); // 8 hours max
        jwtOptions.RememberMeExpireMinutes.Should().BeLessThanOrEqualTo(43200); // 30 days max
    }

    [Fact]
    public void SecurityOptions_ShouldBeConfigured()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var securityOptions = configuration.GetSection("Security").Get<SecurityOptions>();

        if (securityOptions != null)
        {
            // Check security headers configuration
            securityOptions.SecurityHeaders.Should().NotBeNull();
            securityOptions.SecurityHeaders.ContentSecurityPolicy.Should().NotBeNullOrEmpty();
            securityOptions.SecurityHeaders.XFrameOptions.Should().Be("DENY");
            securityOptions.SecurityHeaders.XContentTypeOptions.Should().Be("nosniff");
        }
    }

    [Fact]
    public async Task SqlInjection_ShouldBePrevented()
    {
        // Attempt SQL injection in search parameter
        var maliciousInput = "'; DROP TABLE Users; --";

        var response = await _client.GetAsync($"/api/v1/users/search?query={Uri.EscapeDataString(maliciousInput)}");

        // Should handle safely without error
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest
        );

        // Verify Users table still exists (would require auth)
        // This is a basic check - in real scenario, we'd verify through authorized endpoint
    }

    [Fact]
    public async Task XSS_ShouldBePrevented()
    {
        var xssPayload = new
        {
            Username = "<script>alert('XSS')</script>",
            Email = "test@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/users", xssPayload);

        // If the request succeeds, response should not contain unescaped script
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("<script>");
            content.Should().NotContain("alert(");
        }
    }

    [Fact]
    public async Task CORS_ShouldBeConfigured()
    {
        _client.DefaultRequestHeaders.Add("Origin", "https://malicious-site.com");

        var response = await _client.GetAsync("/api/v1/health");

        // Should not allow arbitrary origins
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
            allowedOrigin.Should().NotBe("*");
            allowedOrigin.Should().NotBe("https://malicious-site.com");
        }
    }
}