using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Catalog.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

public class BatchImportBoundaryTests
{
    [Fact]
    public async Task BatchImport_EmptyList_ReturnsValidationFail_Should_When_PatientRequestEmpty()
    {
        var api = Substitute.For<IApiClientPatients>();
        api.BatchImportAsync(Arg.Any<PatientBatchImportInputDto>())
            .Returns(new ApiResponse<PatientBatchImportResultDto> { Success = false, Message = "导入列表不能为空" });
        var repo = new PatientRepository(api, Substitute.For<ILogger<PatientRepository>>());
        var request = new PatientBatchImportInputDto { Patients = new List<PatientInputDto>(), Strategy = DuplicateStrategy.Skip };

        var result = await repo.BatchImportAsync(request);

        result.Should().BeNull("空列表应视为业务拒绝返回 null");
        await api.Received(1).BatchImportAsync(Arg.Is<PatientBatchImportInputDto>(r => r.Patients.Count == 0));
    }

    [Fact]
    public async Task BatchImport_Count0_ReturnsValidationFail_Should_When_HerbRequestCountZero()
    {
        var api = Substitute.For<IApiClientHerbs>();
        api.BatchImportAsync(Arg.Any<HerbBatchImportInputDto>())
            .Returns(new ApiResponse<HerbBatchImportResultDto> { Success = false, Message = "导入列表不能为空" });
        var repo = new HerbRepository(api, Substitute.For<ILogger<HerbRepository>>());
        var request = new HerbBatchImportInputDto { Herbs = new List<HerbInputDto>(), Strategy = DuplicateStrategy.Skip };

        var result = await repo.BatchImportAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BatchImport_DuplicateSkip_ReturnsSkippedCount_Should_When_SkipStrategy()
    {
        var api = Substitute.For<IApiClientHerbs>();
        var dto = new HerbBatchImportResultDto { SuccessCount = 1, FailureCount = 1, TotalCount = 2, Message = "部分成功" };
        dto.Failures.Add(new HerbImportFailureDto { RowNumber = 2, HerbName = "人参", Reason = "已跳过重复" });
        api.BatchImportAsync(Arg.Any<HerbBatchImportInputDto>())
            .Returns(new ApiResponse<HerbBatchImportResultDto> { Success = true, Data = dto });
        var repo = new HerbRepository(api, Substitute.For<ILogger<HerbRepository>>());
        var request = new HerbBatchImportInputDto
        {
            Herbs = new List<HerbInputDto> { new() { Name = "人参" }, new() { Name = "人参" } },
            Strategy = DuplicateStrategy.Skip
        };

        var result = await repo.BatchImportAsync(request);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Message.Should().NotBeNullOrEmpty();
        result.Failures.Should().HaveCount(1);
    }

    [Fact]
    public async Task BatchImport_DuplicateUpdate_ReturnsUpdatedCount_Should_When_UpdateStrategy()
    {
        var api = Substitute.For<IApiClientPatients>();
        var dto = new PatientBatchImportResultDto { SuccessCount = 2, FailureCount = 0, TotalCount = 2, Message = "更新成功" };
        api.BatchImportAsync(Arg.Any<PatientBatchImportInputDto>())
            .Returns(new ApiResponse<PatientBatchImportResultDto> { Success = true, Data = dto });
        var repo = new PatientRepository(api, Substitute.For<ILogger<PatientRepository>>());
        var request = new PatientBatchImportInputDto
        {
            Patients = new List<PatientInputDto> { new() { Name = "张三", PhoneNumber = "13800138000", Gender = Gender.Male } },
            Strategy = DuplicateStrategy.Update
        };

        var result = await repo.BatchImportAsync(request);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BatchImport_ConcurrencyConflict_ReturnsRetried_Should_When_HttpThrows()
    {
        var api = Substitute.For<IApiClientHerbs>();
        api.BatchImportAsync(Arg.Any<HerbBatchImportInputDto>())
            .Returns(Task.FromException<ApiResponse<HerbBatchImportResultDto>>(new System.Net.Http.HttpRequestException("并发冲突 409")));
        var repo = new HerbRepository(api, Substitute.For<ILogger<HerbRepository>>());
        var request = new HerbBatchImportInputDto { Herbs = new List<HerbInputDto> { new() { Name = "人参" } }, Strategy = DuplicateStrategy.Error };

        var result = await repo.BatchImportAsync(request);

        result.Should().BeNull();
        await api.Received(1).BatchImportAsync(Arg.Any<HerbBatchImportInputDto>());
    }
}
