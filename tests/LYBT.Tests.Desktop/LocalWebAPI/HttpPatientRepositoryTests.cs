using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpPatientRepository unit tests
/// </summary>
public class HttpPatientRepositoryTests
{
    private readonly HttpClient _mockHttpClient;
    private readonly ILogger<HttpPatientRepository> _logger;
    private readonly HttpPatientRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpPatientRepositoryTests()
    {
        var handler = new MockHttpMessageHandler();
        _mockHttpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpPatientRepository>>();
        _repo = new HttpPatientRepository(_mockHttpClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpPatientRepository(client, _logger);

        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpPatientRepository(client, _logger);

        var result = await repo.DeleteAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_List_On_No_Results()
    {
        var paged = new PagedResult<PatientListDto> { Items = new List<PatientListDto>() };
        var json = JsonSerializer.Serialize(paged, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpPatientRepository(client, _logger);

        var result = await repo.SearchAsync("nonexistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public void Unsupported_Methods_Return_Null()
    {
        _repo.BatchImportAsync(null!).Result.Should().BeNull();
        _repo.ExportTemplateAsync().Result.Should().BeNull();
        _repo.ExportPatientsAsync().Result.Should().BeNull();
        _repo.RestoreAsync(Guid.NewGuid()).Result.Should().BeNull();
        _repo.BatchDeleteAsync([]).Result.Should().BeNull();
    }
}

/// <summary>
/// Simple mock HTTP message handler for testing
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handler = null)
    {
        _handler = handler ?? ((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}
