using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpPatientRepository unit tests — verifies delegation to IApiClient.
/// </summary>
public class HttpPatientRepositoryTests
{
    private readonly IApiClient _mockApiClient;
    private readonly IApiClientPatients _mockPatients;
    private readonly ILogger<HttpPatientRepository> _logger;
    private readonly HttpPatientRepository _repo;

    public HttpPatientRepositoryTests()
    {
        _mockApiClient = Substitute.For<IApiClient>();
        _mockPatients = Substitute.For<IApiClientPatients>();
        _mockApiClient.Patients.Returns(_mockPatients);
        _logger = Substitute.For<ILogger<HttpPatientRepository>>();
        _repo = new HttpPatientRepository(_mockApiClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Data_When_Success()
    {
        var id = Guid.NewGuid();
        var detail = new PatientDetailDto { Id = id, Name = "TestPatient" };
        _mockPatients.GetPatientByIdAsync(id)
            .Returns(new ApiResponse<PatientDetailDto> { Success = true, Data = detail });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("TestPatient");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_No_Data()
    {
        var id = Guid.NewGuid();
        _mockPatients.GetPatientByIdAsync(id)
            .Returns(new ApiResponse<PatientDetailDto> { Success = false, Data = null });

        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        _mockPatients.DeletePatientAsync(id)
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.DeleteAsync(id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        _mockPatients.GetPatientsAsync(1, 100, "nonexistent")
            .Returns(new ApiResponse<PagedResult<PatientListDto>> { Success = true, Data = new PagedResult<PatientListDto> { Items = [] } });

        var result = await _repo.SearchAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Throws_On_Failure()
    {
        _mockPatients.CreatePatientAsync(Arg.Any<PatientInputDto>())
            .Returns(new ApiResponse<PatientDetailDto> { Success = false, Message = "Create failed" });

        var input = new PatientInputDto { Name = "TestPatient" };
        await _repo.Invoking(r => r.CreateAsync(input))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Returns_Data_On_Success()
    {
        var detail = new PatientDetailDto { Id = Guid.NewGuid(), Name = "TestPatient" };
        _mockPatients.CreatePatientAsync(Arg.Any<PatientInputDto>())
            .Returns(new ApiResponse<PatientDetailDto> { Success = true, Data = detail });

        var input = new PatientInputDto { Name = "TestPatient" };
        var result = await _repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.Name.Should().Be("TestPatient");
    }

    [Fact]
    public async Task BatchDeleteAsync_Returns_Data_On_Success()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 1 };
        _mockPatients.BatchDeleteAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchDeleteAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task RestoreAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var detail = new PatientDetailDto { Id = id };
        _mockPatients.RestoreAsync(id)
            .Returns(new ApiResponse<PatientDetailDto> { Success = true, Data = detail });

        var result = await _repo.RestoreAsync(id);

        result.Should().NotBeNull();
    }
}
