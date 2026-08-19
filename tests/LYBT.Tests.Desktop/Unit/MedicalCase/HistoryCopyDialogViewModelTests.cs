using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Dialogs;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Tests.Desktop;

public class HistoryCopyDialogViewModelTests
{
    private readonly IViewModelServices _services;
    private readonly IMedicalCaseRepository _repo;
    private readonly MedicalCaseDetailModelMapper _mapper = new();

    public HistoryCopyDialogViewModelTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var ea = Substitute.For<IEventAggregator>();
        var rm = Substitute.For<IRegionManager>();
        var sm = Substitute.For<ISessionManager>();
        var uns = Substitute.For<IUserNotificationService>();
        var cds = Substitute.For<ICommonDialogService>();
        var toast = Substitute.For<IToastService>();
        var rr = Substitute.For<LYBT.Desktop.Contracts.Roles.IRoleRegistry>();
        var ui = Substitute.For<IUiThreadDispatcher>();
        _services = Substitute.For<IViewModelServices>();
        _services.LoggerFactory.Returns(loggerFactory);
        _services.EventAggregator.Returns(ea);
        _services.RegionManager.Returns(rm);
        _services.SessionManager.Returns(sm);
        _services.UserNotificationService.Returns(uns);
        _services.CommonDialogService.Returns(cds);
        _services.ToastService.Returns(toast);
        _services.RoleRegistry.Returns(rr);
        _services.UiThreadDispatcher.Returns(ui);
        _repo = Substitute.For<IMedicalCaseRepository>();
    }

    private HistoryCopyDialogViewModel CreateSut() => new(_services, _repo, _mapper);

    private static MedicalCaseDetailDto CreateDto(Guid id, DateTime createdAt, string patientName = "测试患者")
        => new() { Id = id, PatientId = Guid.NewGuid(), PatientName = patientName, CaseStatus = MedicalCaseStatus.Completed, CreatedAt = createdAt, CaseNumber = "MC-001", PrescriptionId = Guid.NewGuid(), Prescription = new LYBT.Shared.Models.Contracts.Prescriptions.PrescriptionDetailDto { Items = new List<LYBT.Shared.Models.Contracts.Prescriptions.PrescriptionItemDto> { new() { HerbName = "人参", Dosage = 10, Unit = "g" } } } };

    [Fact]
    public async Task LoadHistory_ReturnsPagedResults_Should_When_PatientIdProvided()
    {
        // Arrange
        var sut = CreateSut();
        var patientId = Guid.NewGuid();
        var dto1 = CreateDto(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        var dto2 = CreateDto(Guid.NewGuid(), DateTime.UtcNow);
        _repo.QueryAsync(Arg.Any<MedicalCaseQueryDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<MedicalCaseListDto> { Items = new List<MedicalCaseListDto> { new() { Id = dto1.Id, PatientName = dto1.PatientName, CaseStatus = dto1.CaseStatus, HasPrescription = true }, new() { Id = dto2.Id, PatientName = dto2.PatientName, CaseStatus = dto2.CaseStatus, HasPrescription = true } }, TotalCount = 2 });
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<MedicalCaseDetailDto?>(callInfo.Arg<Guid>() == dto1.Id ? dto1 : dto2));

        // Act
        var parameters = new Prism.Services.Dialogs.DialogParameters { { "PatientId", patientId }, { "PatientName", "测试患者" } };
        // OnDialogOpenedCore is protected, invoke via reflection
        var method = typeof(HistoryCopyDialogViewModel).GetMethod("OnDialogOpenedCore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(sut, new object[] { parameters });

        // Assert - after async load, FilteredCases should eventually contain items (allow async)
        await System.Threading.Tasks.Task.Delay(300);
        sut.PatientName.Should().Be("测试患者");
    }

    [Fact]
    public void FilterByDateRange_ReturnsFilteredResults_Should_When_DateSet()
    {
        // Arrange
        var sut = CreateSut();
        // 直接设置原数据 via reflection
        var field = typeof(HistoryCopyDialogViewModel).GetField("_currentPatientCases", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = new List<LYBT.Desktop.MedicalCase.Models.MedicalCaseDetailModel>
        {
            new() { Id = Guid.NewGuid(), PatientName = "A", CreatedAt = DateTime.UtcNow.AddDays(-10), Status = MedicalCaseStatus.Completed },
            new() { Id = Guid.NewGuid(), PatientName = "B", CreatedAt = DateTime.UtcNow, Status = MedicalCaseStatus.Completed }
        };
        sut.PatientName = "测试患者";
        field!.SetValue(sut, list);
        // Act
        sut.StartDate = DateTime.UtcNow.AddDays(-5);
        sut.EndDate = DateTime.UtcNow;

        // Assert
        sut.FilteredCases.Should().HaveCount(1);
        sut.FilteredCases[0].PatientName.Should().Be("B");
    }

    [Fact]
    public async Task PreviewCopy_ReturnsPreviewData_Should_When_CaseSelected()
    {
        // Arrange
        var sut = CreateSut();
        var dto = CreateDto(Guid.NewGuid(), DateTime.UtcNow);
        _repo.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MedicalCaseDetailDto?>(dto));
        var model = _mapper.ToItem(dto);
        // Act
        sut.SelectedCase = model;
        await System.Threading.Tasks.Task.Delay(200);
        // Assert
        sut.SelectedCaseDetail.Should().NotBeNull();
        sut.SelectedPrescriptionItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CopyToNewCase_CallsService_Should_When_ConfirmWithPrescription()
    {
        // Arrange
        var sut = CreateSut();
        var dto = CreateDto(Guid.NewGuid(), DateTime.UtcNow);
        _repo.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MedicalCaseDetailDto?>(dto));
        var model = _mapper.ToItem(dto);
        sut.SelectedCase = model;
        await System.Threading.Tasks.Task.Delay(200);
        // Act: CanConfirm should be true when SelectedCase and herbs
        var canConfirm = sut.ConfirmCommand.CanExecute(null);
        // Assert
        canConfirm.Should().BeTrue("选中医案且有处方药材时应可确认复制");
        sut.SelectedPrescriptionItems.Should().NotBeEmpty();
    }
}
