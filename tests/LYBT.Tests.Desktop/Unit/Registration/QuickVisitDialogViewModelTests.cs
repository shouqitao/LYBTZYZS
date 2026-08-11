using FluentAssertions;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Registrations.Dialogs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using NSubstitute;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.Unit.Registration;

/// <summary>
/// QuickVisit 弹窗 VM 单测（AC-TEST P2-6: B2 新增 QuickVisit UI 链无测试——
/// 患者选择 → 确认 → service 调用 → OK 参数返回）
/// </summary>
public class QuickVisitDialogViewModelTests : IDisposable
{
    private readonly IPatientService _patientService;
    private readonly IRegistrationService _registrationService;
    private readonly QuickVisitDialogViewModel _vm;
    private IDialogResult? _closeResult;

    public QuickVisitDialogViewModelTests()
    {
        _patientService = Substitute.For<IPatientService>();
        _registrationService = Substitute.For<IRegistrationService>();
        _vm = new QuickVisitDialogViewModel(
            Substitute.For<IViewModelServices>(),
            _patientService,
            _registrationService);
        _vm.RequestClose += r => _closeResult = r;
    }

    private static PatientListDto CreatePatient() => new()
    {
        Id = Guid.NewGuid(),
        Name = "张三",
        PhoneNumber = "13800000000"
    };

    [Fact]
    public void CanConfirm_WithoutPatient_IsFalse()
    {
        _vm.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectPatient_SetsSelection_AndEnablesConfirm()
    {
        var patient = CreatePatient();
        _vm.PatientSearchText = "张三";

        _vm.SelectPatientCommand.Execute(patient);

        _vm.SelectedPatient.Should().Be(patient);
        _vm.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    private void ExecuteAndWait()
    {
        _vm.ConfirmCommand.Execute(null);
        // Confirm() 内部 SafeFireAndForget——轮询等待异步完成（上限 5 秒）
        for (var i = 0; i < 50 && _closeResult is null; i++)
            Task.Delay(100).GetAwaiter().GetResult();
    }

    public void Dispose() => _vm.Dispose();

    [Fact]
    public async Task Confirm_WithPatient_CallsQuickVisit_AndClosesWithResult()
    {
        var patient = CreatePatient();
        _vm.SelectPatientCommand.Execute(patient);
        var result = new QuickVisitResultDto
        {
            RegistrationId = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            PatientId = patient.Id
        };
        _registrationService.QuickVisitAsync(Arg.Any<QuickVisitInputDto>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult<QuickVisitResultDto>.Succeeded(result));

        ExecuteAndWait();

        await _registrationService.Received(1).QuickVisitAsync(
            Arg.Is<QuickVisitInputDto>(r => r.PatientId == patient.Id && r.PatientName == patient.Name),
            Arg.Any<CancellationToken>());
        _closeResult.Should().NotBeNull();
        _closeResult!.Result.Should().Be(ButtonResult.OK);
        _closeResult.Parameters.GetValue<QuickVisitResultDto>("QuickVisitResult").MedicalCaseId
            .Should().Be(result.MedicalCaseId);
    }

    [Fact]
    public void Confirm_ServiceFailure_SetsStatus_AndDoesNotClose()
    {
        var patient = CreatePatient();
        _vm.SelectPatientCommand.Execute(patient);
        _registrationService.QuickVisitAsync(Arg.Any<QuickVisitInputDto>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult<QuickVisitResultDto>.Failed("患者已有进行中医案"));

        ExecuteAndWait();

        _vm.StatusMessage.Should().Contain("患者已有进行中医案");
        _closeResult.Should().BeNull();
    }
}
