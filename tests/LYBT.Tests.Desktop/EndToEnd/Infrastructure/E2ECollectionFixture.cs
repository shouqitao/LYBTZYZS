using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

public class E2ECollectionFixture : IAsyncLifetime, IDisposable
{
    private static readonly SemaphoreSlim _globalLoginLock = new(1, 1);
    private static readonly Dictionary<string, RoleSession> _globalRoleSessions = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized = false;

    private IConfiguration _configuration = null!;
    private HttpClient _httpClient = null!;
    private ILogger<E2ECollectionFixture> _logger = null!;

    public static class Roles
    {
        public const string SysAdmin = "SysAdmin";
        public const string Admin = "Admin";
        public const string Doctor = "Doctor";
        public const string Receptionist = "Receptionist";
    }

    public async Task InitializeAsync()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var baseUrl = _configuration["WebAPI:BaseUrl"]!;
        var timeoutSeconds = _configuration.GetValue<int>("WebAPI:TimeoutSeconds", 30);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<E2ECollectionFixture>();

        await _globalLoginLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                _logger.LogInformation("Initializing E2E Collection Fixture...");

                var (sysAdminToken, sysAdminLoginResponse) = await LoginAsync(
                    _configuration["TestCredentials:Username"]!,
                    _configuration["TestCredentials:Password"]!);

                if (string.IsNullOrEmpty(sysAdminToken) || sysAdminLoginResponse == null)
                    throw new InvalidOperationException("Failed to login as SysAdmin");

                _globalRoleSessions[Roles.SysAdmin] = new RoleSession(sysAdminToken, sysAdminLoginResponse);
                _logger.LogInformation("SysAdmin logged in successfully");

                await EnsureRoleAsync(Roles.Admin,
                    _configuration["TestCredentials:Admin:Username"] ?? "e2e_admin",
                    _configuration["TestCredentials:Admin:Password"] ?? "AdminPass123!",
                    UserRole.Admin);

                await EnsureRoleAsync(Roles.Doctor,
                    _configuration["TestCredentials:Doctor:Username"] ?? "doctor",
                    _configuration["TestCredentials:Doctor:Password"] ?? "DoctorPass123!",
                    UserRole.Doctor);

                await EnsureRoleAsync(Roles.Receptionist,
                    _configuration["TestCredentials:Receptionist:Username"] ?? "e2e_receptionist",
                    _configuration["TestCredentials:Receptionist:Password"] ?? "ReceptionistPass123!",
                    UserRole.Receptionist);

                _initialized = true;
                _logger.LogInformation("E2E Collection Fixture initialized successfully");
            }
        }
        finally
        {
            _globalLoginLock.Release();
        }
    }

    public Task DisposeAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    public string GetTokenForRole(string role)
    {
        lock (_globalRoleSessions)
        {
            if (!_globalRoleSessions.TryGetValue(role, out var session))
                throw new InvalidOperationException($"Role '{role}' was not pre-logged in.");

            return session.AccessToken;
        }
    }

    public LoginResponse GetLoginResponseForRole(string role)
    {
        lock (_globalRoleSessions)
        {
            if (!_globalRoleSessions.TryGetValue(role, out var session))
                throw new InvalidOperationException($"Role '{role}' was not pre-logged in.");

            return session.LoginResponse;
        }
    }

    public IConfiguration Configuration => _configuration;

    private async Task EnsureRoleAsync(string role, string username, string password, UserRole userRole)
    {
        var (token, loginResponse) = await LoginAsync(username, password);

        if (!string.IsNullOrEmpty(token) && loginResponse != null)
        {
            lock (_globalRoleSessions)
            {
                _globalRoleSessions[role] = new RoleSession(token, loginResponse);
            }
            _logger.LogInformation("Role '{Role}' logged in as '{Username}'", role, username);
            return;
        }

        _logger.LogWarning("Login failed for '{Username}', creating user...", username);

        var sysAdminToken = GetTokenForRole(Roles.SysAdmin);
        var createSuccess = await CreateUserAsync(username, password, userRole, sysAdminToken);

        if (createSuccess)
        {
            var (newToken, newLoginResponse) = await LoginAsync(username, password);
            if (!string.IsNullOrEmpty(newToken) && newLoginResponse != null)
            {
                lock (_globalRoleSessions)
                {
                    _globalRoleSessions[role] = new RoleSession(newToken, newLoginResponse);
                }
                _logger.LogInformation("Role '{Role}' logged in after creation", role);
                return;
            }

            throw new InvalidOperationException($"Failed to ensure role '{role}' for '{username}'");
        }
    }


    private async Task<(string? Token, LoginResponse? LoginResponse)> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new { UserName = username, Password = password };
            var json = JsonSerializer.Serialize(loginRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/auth/login", content);

            if (!response.IsSuccessStatusCode)
                return (null, null);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (result.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("token", out var tokenElement))
            {
                var token = tokenElement.GetString();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody);
                return (token, loginResponse);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Login failed for {Username}", username);
            return (null, null);
        }
    }
    private async Task<bool> CreateUserAsync(string username, string password, UserRole role, string adminToken)
    {
        try
        {
            var createRequest = new UserInputDto
            {
                UserName = username,
                Password = password,
                ConfirmPassword = password,
                RealName = $"E2E测试{role}",
                Role = role,
                Remark = "E2E测试自动创建"
            };

            var json = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                Converters = { new JsonStringEnumConverter() }
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return true;

            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Create user failed: {response.StatusCode}, {errorBody}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger?.LogError(ex, "Create user failed for {Username}", username);
            throw;
        }
    }

    private sealed record RoleSession(string AccessToken, LoginResponse LoginResponse);

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

[CollectionDefinition("E2E")]
public class E2ETestCollection : ICollectionFixture<E2ECollectionFixture>
{
}