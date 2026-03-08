using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase.Components;

/// <summary>
/// PrescriptionPrintHandler 单元测试
/// CODE-24: 验证空处方打印应被阻止
/// </summary>
public class PrescriptionPrintHandlerTests
{
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IMedicalCaseRepository _repository;
    private readonly ISessionManager _sessionManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPrintService<PrescriptionPrintModel> _printService;

    public PrescriptionPrintHandlerTests()
    {
        _medicalCaseService = Substitute.For<IMedicalCaseService>();
        _repository = Substitute.For<IMedicalCaseRepository>();
        _sessionManager = Substitute.For<ISessionManager>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _printService = Substitute.For<IPrintService<PrescriptionPrintModel>>();
    }

    private PrescriptionPrintHandler CreateSut() => new(
        _medicalCaseService, _repository, _sessionManager, _loggerFactory, _printService);

    [Fact]
    public async Task PrintPreviewAsync_NoPrescription_ShouldReturnFailed()
    {
        // Arrange
        _medicalCaseService.CachedPrescription.Returns((PrescriptionDetailDto?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.PrintPreviewAsync(Guid.NewGuid(), null, null, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("没有可打印的处方数据");
    }

    [Fact]
    public async Task PrintPreviewAsync_EmptyItems_ShouldReturnFailed()
    {
        // Arrange - prescription exists but Items is empty
        var emptyPrescription = new PrescriptionDetailDto
        {
            Id = Guid.NewGuid(),
            DosageCount = 7,
            Items = new List<PrescriptionItemDto>() // Empty!
        };
        _medicalCaseService.CachedPrescription.Returns(emptyPrescription);
        var sut = CreateSut();

        // Act
        var result = await sut.PrintPreviewAsync(Guid.NewGuid(), null, null, null);

        // Assert - CODE-24: empty prescription should be blocked
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("无药材信息");
    }

    [Fact]
    public async Task PrintPreviewAsync_NullItems_ShouldReturnFailed()
    {
        // Arrange - prescription exists but Items is null
        var nullItemsPrescription = new PrescriptionDetailDto
        {
            Id = Guid.NewGuid(),
            DosageCount = 7,
            Items = null! // Null!
        };
        _medicalCaseService.CachedPrescription.Returns(nullItemsPrescription);
        var sut = CreateSut();

        // Act
        var result = await sut.PrintPreviewAsync(Guid.NewGuid(), null, null, null);

        // Assert - CODE-24: null items should be blocked
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("无药材信息");
    }
}
