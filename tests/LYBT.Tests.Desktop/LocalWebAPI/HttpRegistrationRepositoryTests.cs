using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpRegistrationRepository unit tests — verifies delegation to IApiClient.
/// </summary>
public class HttpRegistrationRepositoryTests
{
    private readonly IApiClient _mockApiClient;
    private readonly IApiClientRegistrations _mockRegistrations;
    private readonly ILogger<HttpRegistrationRepository> _logger;
    private readonly HttpRegistrationRepository _repo;

    public HttpRegistrationRepositoryTests()
    {
        _mockApiClient = Substitute.For<IApiClient>();
        _mockRegistrations = Substitute.For<IApiClientRegistrations>();
        _mockApiClient.Registrations.Returns(_mockRegistrations);
        _logger = Substitute.For<ILogger<HttpRegistrationRepository>>();
        _repo = new HttpRegistrationRepository(_mockApiClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Data_When_Success()
    {
        var id = Guid.NewGuid();
        var detail = new RegistrationDetailDto { Id = id, PatientName = "Patient A" };
        _mockRegistrations.GetByIdAsync(id)
            .Returns(new ApiResponse<RegistrationDetailDto> { Success = true, Data = detail });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.PatientName.Should().Be("Patient A");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_No_Data()
    {
        var id = Guid.NewGuid();
        _mockRegistrations.GetByIdAsync(id)
            .Returns(new ApiResponse<RegistrationDetailDto> { Success = false, Data = null });

        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWaitingQueueAsync_Returns_List_On_Success()
    {
        var items = new List<RegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), PatientName = "Patient A" },
            new() { Id = Guid.NewGuid(), PatientName = "Patient B" }
        };
        _mockRegistrations.GetQueueAsync(null)
            .Returns(new ApiResponse<List<RegistrationListDto>> { Success = true, Data = items });

        var result = await _repo.GetWaitingQueueAsync();

        result.Should().HaveCount(2);
        result[0].PatientName.Should().Be("Patient A");
    }

    [Fact]
    public async Task GetWaitingQueueAsync_Returns_Empty_On_Failure()
    {
        _mockRegistrations.GetQueueAsync(null)
            .Returns(new ApiResponse<List<RegistrationListDto>> { Success = false, Data = null });

        var result = await _repo.GetWaitingQueueAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task StartVisitAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var medicalCaseId = Guid.NewGuid();
        _mockRegistrations.StartVisitAsync(id)
            .Returns(new ApiResponse<Guid> { Success = true, Data = medicalCaseId });

        var result = await _repo.StartVisitAsync(id);

        result.Should().Be(medicalCaseId);
    }

    [Fact]
    public async Task StartVisitAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockRegistrations.StartVisitAsync(id)
            .Returns(new ApiResponse<Guid> { Success = false });

        var result = await _repo.StartVisitAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        _mockRegistrations.CancelAsync(id)
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.CancelAsync(id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAsync_Returns_False_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockRegistrations.CancelAsync(id)
            .Returns(new ApiResponse { Success = false });

        var result = await _repo.CancelAsync(id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Throws_On_Failure()
    {
        _mockRegistrations.CreateAsync(Arg.Any<RegistrationInputDto>())
            .Returns(new ApiResponse<RegistrationDetailDto> { Success = false, Message = "Create failed" });

        var input = new RegistrationInputDto { PatientId = Guid.NewGuid(), PatientName = "P", DoctorId = Guid.NewGuid(), DoctorName = "D" };
        await _repo.Invoking(r => r.CreateAsync(input))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Returns_Data_On_Success()
    {
        var detail = new RegistrationDetailDto { Id = Guid.NewGuid(), PatientName = "Patient A" };
        _mockRegistrations.CreateAsync(Arg.Any<RegistrationInputDto>())
            .Returns(new ApiResponse<RegistrationDetailDto> { Success = true, Data = detail });

        var input = new RegistrationInputDto { PatientId = Guid.NewGuid(), PatientName = "Patient A", DoctorId = Guid.NewGuid(), DoctorName = "Doctor B" };
        var result = await _repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.PatientName.Should().Be("Patient A");
    }
}
