using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpHerbRepository unit tests
/// </summary>
public class HttpHerbRepositoryTests : IDisposable
{
    private readonly HttpClient _mockHttpClient;
    private readonly ILogger<HttpHerbRepository> _logger;
    private readonly HttpHerbRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpHerbRepositoryTests()
    {
        var handler = new MockHttpMessageHandler();
        _mockHttpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpHerbRepository>>();
        _repo = new HttpHerbRepository(_mockHttpClient, _logger);
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
        var repo = new HttpHerbRepository(client, _logger);

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
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.DeleteAsync(id);
        result.Should().BeTrue();
        capturedMethod.Should().Be("DELETE");
        capturedPath.Should().Contain($"/api/herbs/{id}");
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_List_On_No_Results()
    {
        var paged = new PagedResult<HerbListDto> { Items = new List<HerbListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.SearchAsync("nonexistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.ToggleStatusAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.RestoreAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task BatchDeleteAsync_Deserializes_Result()
    {
        var batchResult = new BatchOperationResultDto { SuccessCount = 3, FailureCount = 1 };
        var json = JsonSerializer.Serialize(batchResult, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpHerbRepository(client, _logger);

        var result = await repo.BatchDeleteAsync([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_And_Returns_Detail()
    {
        var detail = new HerbDetailDto { Id = Guid.NewGuid(), Name = "TestHerb" };
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
        var repo = new HttpHerbRepository(client, _logger);

        var input = new HerbInputDto { Name = "TestHerb" };
        var result = await repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.Name.Should().Be("TestHerb");
        capturedMethod.Should().Be("POST");
        capturedPath.Should().Be("/api/herbs");
        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("TestHerb");
    }
}
