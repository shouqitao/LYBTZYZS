using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.MedicalCase.Reports.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

public class ReportServiceTests
{
    private readonly IReportRepository _repo;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _repo = Substitute.For<IReportRepository>();
        var logger = Substitute.For<ILogger<ReportService>>();
        _sut = new ReportService(_repo, logger);
    }

    [Fact]
    public async Task GetDailyStatsAsync_ReturnsData_Should_When_RepositorySucceeds()
    {
        // Arrange
        var dto = new DailyIncomeDto { TotalIncome = 1234.56m, RegistrationFeeTotal = 200m, MedicineFeeTotal = 1034.56m };
        _repo.GetDailyIncomeAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(new ApiResponse<DailyIncomeDto> { Success = true, Data = dto, Message = "ok" });

        // Act
        var result = await _sut.GetDailyIncomeAsync(DateTime.Today, DateTime.Today);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalIncome.Should().Be(1234.56m);
        result.Data!.RegistrationFeeTotal.Should().Be(200m);
    }

    [Fact]
    public async Task GetDailyStatsAsync_EmptyData_ReturnsZero_Should_When_RepositoryReturnsEmpty()
    {
        // Arrange
        _repo.GetDailyIncomeAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(new ApiResponse<DailyIncomeDto> { Success = false, Data = null, Message = "no data" });
        _repo.GetDailyConsultationsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(new ApiResponse<DailyConsultationDto> { Success = true, Data = new DailyConsultationDto { TotalCount = 0 } });

        // Act
        var incomeResult = await _sut.GetDailyIncomeAsync(DateTime.Today, DateTime.Today);
        var consultResult = await _sut.GetDailyConsultationsAsync(DateTime.Today, DateTime.Today);

        // Assert: 空数据应转为 Failed（Service 契约），上层 VM 可据此显示零值
        incomeResult.Success.Should().BeFalse();
        incomeResult.Data.Should().BeNull();
        incomeResult.Error.Should().NotBeNullOrEmpty();

        consultResult.Success.Should().BeTrue();
        consultResult.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDailyStatsAsync_Exception_Throws_Should_When_RepositoryThrows()
    {
        // Arrange
        _repo.GetDailyIncomeAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(Task.FromException<ApiResponse<DailyIncomeDto>>(new System.Net.Http.HttpRequestException("network down")));
        _repo.GetDailyHerbUsageAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns(Task.FromException<ApiResponse<DailyHerbUsageDto>>(new InvalidOperationException("infra failed")));

        // Act
        var incomeResult = await _sut.GetDailyIncomeAsync(DateTime.Today, DateTime.Today);
        var herbResult = await _sut.GetDailyHerbUsageAsync(DateTime.Today, DateTime.Today);

        // Assert: 异常应被捕获并转为 Failed，而非向上传播（ADR-0020 Service 契约）
        incomeResult.Success.Should().BeFalse();
        incomeResult.Error.Should().NotBeNullOrEmpty();
        herbResult.Success.Should().BeFalse();
        herbResult.Error.Should().NotBeNullOrEmpty();

        // 确保不抛异常
        var actIncome = async () => await _sut.GetDailyIncomeAsync();
        await actIncome.Should().NotThrowAsync();
    }
}
