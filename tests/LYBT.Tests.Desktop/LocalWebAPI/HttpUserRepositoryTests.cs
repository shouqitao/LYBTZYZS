using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpUserRepository unit tests
/// </summary>
public class HttpUserRepositoryTests : IDisposable
{
    private readonly HttpClient _mockHttpClient;
    private readonly ILogger<HttpUserRepository> _logger;
    private readonly HttpUserRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpUserRepositoryTests()
    {
        var handler = new MockHttpMessageHandler();
        _mockHttpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpUserRepository>>();
        _repo = new HttpUserRepository(_mockHttpClient, _logger);
    }

    public void Dispose()
    {
        _mockHttpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        string? capturedMethod = null;
        string? capturedPath = null;
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            capturedPath = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.DeleteAsync(id);
        result.Should().BeTrue();
        capturedMethod.Should().Be("DELETE");
        capturedPath.Should().Contain($"/api/users/{id}");
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        var paged = new PagedResult<UserListDto> { Items = new List<UserListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.SearchAsync("nonexistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.ToggleStatusAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.RestoreAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_Returns_Success_On_200()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Returns_Failure_On_400()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var result = await repo.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest { OldPassword = "wrong", NewPassword = "new" });
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_To_Users()
    {
        var detail = new UserDetailDto { Id = Guid.NewGuid(), UserName = "testuser", RealName = "Test User" };
        var json = JsonSerializer.Serialize(detail, Json);
        string? capturedMethod = null;
        string? capturedPath = null;
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            capturedPath = req.RequestUri?.PathAndQuery;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpUserRepository(client, _logger);

        var input = new UserInputDto { UserName = "testuser", RealName = "Test User" };
        var result = await repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.UserName.Should().Be("testuser");
        capturedMethod.Should().Be("POST");
        capturedPath.Should().Be("/api/users");
        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("testuser");
    }
}
