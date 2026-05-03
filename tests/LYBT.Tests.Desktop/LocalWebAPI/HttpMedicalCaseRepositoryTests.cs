using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpMedicalCaseRepository unit tests
/// </summary>
public class HttpMedicalCaseRepositoryTests : IDisposable
{
    private readonly HttpClient _mockHttpClient;
    private readonly ILogger<HttpMedicalCaseRepository> _logger;
    private readonly HttpMedicalCaseRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpMedicalCaseRepositoryTests()
    {
        var handler = new MockHttpMessageHandler();
        _mockHttpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpMedicalCaseRepository>>();
        _repo = new HttpMedicalCaseRepository(_mockHttpClient, _logger);
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
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.DeleteAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CloseCaseAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.CloseCaseAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPermissionsAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.GetPermissionsAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetPrescriptionFlagAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.SetPrescriptionFlagAsync(Guid.NewGuid(), new SetPrescriptionFlagRequest { NeedsPrescription = true });
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.UpdateStatusAsync(Guid.NewGuid(), new MedicalCaseStatusInputDto { Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed });
        result.Should().BeNull();
    }

    [Fact]
    public async Task SuspendAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.SuspendAsync(Guid.NewGuid(), null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RecordPrintCompletedAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.RecordPrintCompletedAsync(Guid.NewGuid(), new PrintCompletedRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBatchDetailsAsync_Deserializes_List()
    {
        var details = new List<MedicalCaseDetailDto>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };
        var json = JsonSerializer.Serialize(details, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.GetBatchDetailsAsync([Guid.NewGuid(), Guid.NewGuid()]);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task BatchDeleteAsync_Deserializes_Result()
    {
        var batchResult = new BatchOperationResultDto { SuccessCount = 5, FailureCount = 0 };
        var json = JsonSerializer.Serialize(batchResult, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpMedicalCaseRepository(client, _logger);

        var result = await repo.BatchDeleteAsync([Guid.NewGuid(), Guid.NewGuid()]);
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(5);
        result.FailureCount.Should().Be(0);
    }
}
