using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpRegistrationRepository unit tests
/// </summary>
public class HttpRegistrationRepositoryTests : IDisposable
{
    private readonly HttpClient _mockHttpClient;
    private readonly ILogger<HttpRegistrationRepository> _logger;
    private readonly HttpRegistrationRepository _repo;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpRegistrationRepositoryTests()
    {
        var handler = new MockHttpMessageHandler();
        _mockHttpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        _logger = Substitute.For<ILogger<HttpRegistrationRepository>>();
        _repo = new HttpRegistrationRepository(_mockHttpClient, _logger);
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
        var repo = new HttpRegistrationRepository(client, _logger);

        var result = await repo.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWaitingQueueAsync_Deserializes_List()
    {
        var items = new List<RegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), PatientName = "Patient A" },
            new() { Id = Guid.NewGuid(), PatientName = "Patient B" }
        };
        var json = JsonSerializer.Serialize(items, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpRegistrationRepository(client, _logger);

        var result = await repo.GetWaitingQueueAsync();
        result.Should().HaveCount(2);
        result[0].PatientName.Should().Be("Patient A");
    }

    [Fact]
    public async Task GetWaitingQueueAsync_With_DoctorId_Passes_Query_Param()
    {
        var doctorId = Guid.NewGuid();
        string? capturedPath = null;
        var items = new List<RegistrationListDto>();
        var json = JsonSerializer.Serialize(items, Json);
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpRegistrationRepository(client, _logger);

        await repo.GetWaitingQueueAsync(doctorId);
        capturedPath.Should().Contain($"doctorId={doctorId}");
    }

    [Fact]
    public async Task StartVisitAsync_Returns_Null_On_404()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpRegistrationRepository(client, _logger);

        var result = await repo.StartVisitAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_Returns_True_On_Success()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpRegistrationRepository(client, _logger);

        var result = await repo.CancelAsync(Guid.NewGuid());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAsync_Returns_False_On_Error()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:0") };
        var repo = new HttpRegistrationRepository(client, _logger);

        var result = await repo.CancelAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Sends_Post_To_Registrations()
    {
        var detail = new RegistrationDetailDto { Id = Guid.NewGuid(), PatientName = "Patient A" };
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
        var repo = new HttpRegistrationRepository(client, _logger);

        var input = new RegistrationInputDto
        {
            PatientId = Guid.NewGuid(),
            PatientName = "Patient A",
            DoctorId = Guid.NewGuid(),
            DoctorName = "Doctor B"
        };
        var result = await repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.PatientName.Should().Be("Patient A");
        capturedMethod.Should().Be("POST");
        capturedPath.Should().Be("/api/registrations");
    }
}
