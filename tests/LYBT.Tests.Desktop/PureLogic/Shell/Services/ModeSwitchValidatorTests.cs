using FluentAssertions;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Shell.Services;

/// <summary>
/// ModeSwitchValidator 单元测试 (US-SYNC-008)
/// 测试模式切换前置检查逻辑
/// </summary>
public class ModeSwitchValidatorTests
{
    private readonly IMedicalCaseDataSource _medicalCaseDataSource;
    private readonly ILogger<ModeSwitchValidator> _logger;

    public ModeSwitchValidatorTests()
    {
        _medicalCaseDataSource = Substitute.For<IMedicalCaseDataSource>();
        _logger = Substitute.For<ILogger<ModeSwitchValidator>>();
    }

    #region SYNC-D01: 本地 -> 远程切换检查

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_NoUnfinishedCases_ReturnsValid()
    {
        // Arrange
        SetupMedicalCaseQuery(MedicalCaseStatus.Active, 0);
        SetupMedicalCaseQuery(MedicalCaseStatus.Suspended, 0);
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.UnfinishedCaseCount.Should().BeNull();
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_HasActiveCases_ReturnsInvalid()
    {
        // Arrange
        SetupMedicalCaseQuery(MedicalCaseStatus.Active, 3);
        SetupMedicalCaseQuery(MedicalCaseStatus.Suspended, 0);
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.UnfinishedCaseCount.Should().Be(3);
        result.ErrorMessage.Should().Contain("3");
        result.ErrorMessage.Should().Contain("未完成");
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_HasSuspendedCases_ReturnsInvalid()
    {
        // Arrange
        SetupMedicalCaseQuery(MedicalCaseStatus.Active, 0);
        SetupMedicalCaseQuery(MedicalCaseStatus.Suspended, 2);
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.UnfinishedCaseCount.Should().Be(2);
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_HasBothActiveAndSuspended_ReturnsTotalCount()
    {
        // Arrange
        SetupMedicalCaseQuery(MedicalCaseStatus.Active, 2);
        SetupMedicalCaseQuery(MedicalCaseStatus.Suspended, 3);
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.UnfinishedCaseCount.Should().Be(5);
        result.ErrorMessage.Should().Contain("5");
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_OnlyCompletedCases_ReturnsValid()
    {
        // Arrange: Completed cases don't block switching
        SetupMedicalCaseQuery(MedicalCaseStatus.Active, 0);
        SetupMedicalCaseQuery(MedicalCaseStatus.Suspended, 0);
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_DataSourceThrows_ReturnsInvalid()
    {
        // Arrange
        _medicalCaseDataSource
            .QueryAsync(
                patientId: Arg.Any<Guid?>(),
                userId: Arg.Any<Guid?>(),
                status: Arg.Any<MedicalCaseStatus?>(),
                startDate: Arg.Any<DateTime?>(),
                endDate: Arg.Any<DateTime?>(),
                page: Arg.Any<int>(),
                pageSize: Arg.Any<int>(),
                ct: Arg.Any<CancellationToken>())
            .Returns<(List<MedicalCaseDetailDto>, int)>(_ => throw new InvalidOperationException("DB connection failed"));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("检查失败");
    }

    #endregion

    #region 远程 -> 本地切换检查

    [Fact]
    public async Task ValidateRemoteToLocalSwitch_InvalidConnectionString_ReturnsInvalid()
    {
        // Arrange: use a clearly invalid connection string
        var sut = CreateValidator(localConnectionString: "Server=INVALID_SERVER_12345;Database=INVALID;Connect Timeout=1;");

        // Act
        var result = await sut.ValidateRemoteToLocalSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("本地数据库");
    }

    #endregion

    #region Helper Methods

    private ModeSwitchValidator CreateValidator(string? localConnectionString = null)
    {
        return new ModeSwitchValidator(
            _medicalCaseDataSource,
            localConnectionString ?? "Server=(localdb)\\MSSQLLocalDB;Database=LYBTZYZS_Test_Switch;Trusted_Connection=True;Connect Timeout=3;",
            _logger);
    }

    private void SetupMedicalCaseQuery(MedicalCaseStatus status, int count)
    {
        var items = Enumerable.Range(0, Math.Min(count, 1))
            .Select(_ => new MedicalCaseDetailDto())
            .ToList();

        _medicalCaseDataSource
            .QueryAsync(
                patientId: Arg.Any<Guid?>(),
                userId: Arg.Any<Guid?>(),
                status: status,
                startDate: Arg.Any<DateTime?>(),
                endDate: Arg.Any<DateTime?>(),
                page: 1,
                pageSize: 1,
                ct: Arg.Any<CancellationToken>())
            .Returns((items, count));
    }

    #endregion
}
