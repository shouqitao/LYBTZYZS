using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.MedicalCase.Reports.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net.Http;

namespace LYBT.Tests.Desktop.Unit.MedicalCase;

/// <summary>
/// ReportService 单测（P0-2：Service 经 IReportRepository 访问，成功 → Succeeded / 失败 → Failed / 异常 → Failed）
/// </summary>
public class ReportServiceTests
{
    private static ReportService CreateService(out IReportRepository repository)
    {
        repository = Substitute.For<IReportRepository>();
        return new ReportService(repository, Substitute.For<ILogger<ReportService>>());
    }

    [Fact]
    public async Task GetDailyIncome_Success_ReturnsSucceeded()
    {
        var service = CreateService(out var repository);
        var dto = new DailyIncomeDto { TotalIncome = 100m };
        repository.GetDailyIncomeAsync(null, null)
            .Returns(Task.FromResult(ApiResponse<DailyIncomeDto>.CreateSuccess(dto)));

        var result = await service.GetDailyIncomeAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetDailyIncome_Failure_ReturnsFailed()
    {
        var service = CreateService(out var repository);
        repository.GetDailyIncomeAsync(null, null)
            .Returns(Task.FromResult(ApiResponse<DailyIncomeDto>.CreateFail("报表服务不可用")));

        var result = await service.GetDailyIncomeAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Be("报表服务不可用");
    }

    [Fact]
    public async Task GetDailyIncome_RepositoryThrows_ReturnsFailed()
    {
        var service = CreateService(out var repository);
        repository.GetDailyIncomeAsync(null, null)
            .Returns(Task.FromException<ApiResponse<DailyIncomeDto>>(new HttpRequestException("network down")));

        var result = await service.GetDailyIncomeAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetDailyConsultations_Success_ReturnsSucceeded()
    {
        var service = CreateService(out var repository);
        var dto = new DailyConsultationDto { TotalCount = 5 };
        repository.GetDailyConsultationsAsync(null, null)
            .Returns(Task.FromResult(ApiResponse<DailyConsultationDto>.CreateSuccess(dto)));

        var result = await service.GetDailyConsultationsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetDailyHerbUsage_Success_ReturnsSucceeded()
    {
        var service = CreateService(out var repository);
        var dto = new DailyHerbUsageDto();
        repository.GetDailyHerbUsageAsync(null, null)
            .Returns(Task.FromResult(ApiResponse<DailyHerbUsageDto>.CreateSuccess(dto)));

        var result = await service.GetDailyHerbUsageAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetDailyHerbUsage_Failure_ReturnsFailed()
    {
        var service = CreateService(out var repository);
        repository.GetDailyHerbUsageAsync(null, null)
            .Returns(Task.FromResult(ApiResponse<DailyHerbUsageDto>.CreateFail("无数据")));

        var result = await service.GetDailyHerbUsageAsync();

        result.Success.Should().BeFalse();
    }
}
