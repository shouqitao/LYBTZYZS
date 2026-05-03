using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpFormulaRepository unit tests
/// </summary>
public class HttpFormulaRepositoryTests : IDisposable
{
    private readonly HttpClient _mockHttpClient;
    private readonly ILogger<HttpFormulaRepository> _logger;
    private readonly HttpFormulaRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpFormulaRepositoryTests()
    {
        var handler = new MockHttpMessageHandler();
        _mockHttpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpFormulaRepository>>();
        _repo = new HttpFormulaRepository(_mockHttpClient, _logger);
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
        var repo = new HttpFormulaRepository(client, _logger);

        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpFormulaRepository(client, _logger);

        var result = await repo.DeleteAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        var paged = new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpFormulaRepository(client, _logger);

        var result = await repo.SearchAsync("nonexistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CloneFormulaAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpFormulaRepository(client, _logger);

        var result = await repo.CloneFormulaAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpFormulaRepository(client, _logger);

        var result = await repo.ToggleStatusAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpFormulaRepository(client, _logger);

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
        var repo = new HttpFormulaRepository(client, _logger);

        var result = await repo.BatchDeleteAsync([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_To_Formulas()
    {
        var detail = new FormulaDetailDto { Id = Guid.NewGuid(), Name = "TestFormula" };
        var json = JsonSerializer.Serialize(detail, Json);
        string? capturedMethod = null;
        string? capturedPath = null;
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            capturedPath = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpFormulaRepository(client, _logger);

        var input = new FormulaInputDto { Name = "TestFormula" };
        var result = await repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.Name.Should().Be("TestFormula");
        capturedMethod.Should().Be("POST");
        capturedPath.Should().Be("/api/formulas");
    }
}
