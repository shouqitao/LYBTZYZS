using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpMedicalCaseRepository unit tests — verifies delegation to IApiClient.
/// </summary>
public class HttpMedicalCaseRepositoryTests
{
    private readonly IApiClient _mockApiClient;
    private readonly IApiClientMedicalCases _mockMedicalCases;
    private readonly ILogger<HttpMedicalCaseRepository> _logger;
    private readonly HttpMedicalCaseRepository _repo;

    public HttpMedicalCaseRepositoryTests()
    {
        _mockApiClient = Substitute.For<IApiClient>();
        _mockMedicalCases = Substitute.For<IApiClientMedicalCases>();
        _mockApiClient.MedicalCases.Returns(_mockMedicalCases);
        _logger = Substitute.For<ILogger<HttpMedicalCaseRepository>>();
        _repo = new HttpMedicalCaseRepository(_mockApiClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Data_When_Success()
    {
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        _mockMedicalCases.GetMedicalCaseByIdAsync(id)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_No_Data()
    {
        var id = Guid.NewGuid();
        _mockMedicalCases.GetMedicalCaseByIdAsync(id)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = false, Data = null });

        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        _mockMedicalCases.DeleteMedicalCaseAsync(id)
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.DeleteAsync(id);

        result.Should().BeTrue();
        await _mockMedicalCases.Received(1).DeleteMedicalCaseAsync(id);
    }

    [Fact]
    public async Task CloseCaseAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        _mockMedicalCases.CloseCaseAsync(id)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail });

        var result = await _repo.CloseCaseAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetPermissionsAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var permissions = new MedicalCasePermissionDto { CanEdit = true };
        _mockMedicalCases.GetPermissionsAsync(id)
            .Returns(new ApiResponse<MedicalCasePermissionDto> { Success = true, Data = permissions });

        var result = await _repo.GetPermissionsAsync(id);

        result.Should().NotBeNull();
        result!.CanEdit.Should().BeTrue();
    }

    [Fact]
    public async Task SetPrescriptionFlagAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        var request = new SetPrescriptionFlagRequest { NeedsPrescription = true };
        _mockMedicalCases.SetPrescriptionFlagAsync(id, request)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail });

        var result = await _repo.SetPrescriptionFlagAsync(id, request);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        var request = new MedicalCaseStatusInputDto { Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed };
        _mockMedicalCases.UpdateStatusAsync(id, request)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail });

        var result = await _repo.UpdateStatusAsync(id, request);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SuspendAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        _mockMedicalCases.SuspendAsync(id, null)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail });

        var result = await _repo.SuspendAsync(id, null);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordPrintCompletedAsync_Returns_Data_On_Success()
    {
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        var request = new PrintCompletedRequest();
        _mockMedicalCases.RecordPrintCompletedAsync(id, request)
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail });

        var result = await _repo.RecordPrintCompletedAsync(id, request);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBatchDetailsAsync_Returns_List_On_Success()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var details = new List<MedicalCaseDetailDto>
        {
            new() { Id = ids[0] },
            new() { Id = ids[1] }
        };
        _mockMedicalCases.GetBatchDetailsAsync(Arg.Any<BatchDetailQueryDto>())
            .Returns(new ApiResponse<List<MedicalCaseDetailDto>> { Success = true, Data = details });

        var result = await _repo.GetBatchDetailsAsync(ids);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task BatchDeleteAsync_Returns_Data_On_Success()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 2, FailureCount = 0 };
        _mockMedicalCases.BatchDeleteAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchDeleteAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_Throws_On_Failure()
    {
        _mockMedicalCases.CreateMedicalCaseAsync(Arg.Any<MedicalCaseInputDto>())
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "Create failed" });

        var input = new MedicalCaseInputDto { PatientId = Guid.NewGuid() };
        await _repo.Invoking(r => r.CreateAsync(input))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveAsync_Throws_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockMedicalCases.SaveAsync(id, Arg.Any<MedicalCaseInputDto>())
            .Returns(new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "Save failed" });

        var input = new MedicalCaseInputDto { PatientId = Guid.NewGuid() };
        await _repo.Invoking(r => r.SaveAsync(id, input))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
