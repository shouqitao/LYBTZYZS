using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Registration.Dialogs;
using LYBT.Desktop.Registration.ViewModels;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Regions;
using Prism.Services.Dialogs;
using Xunit;
using LYBT.Tests.Desktop.Infrastructure;

namespace LYBT.Tests.Desktop.PureLogic.Registration;

[Collection("UserJourney")]
public class RegistrationMasterDetailViewModelTests : UserJourneyTestBase
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IRegistrationService _registrationService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IPatientApi _patientApi;
    private readonly IDialogService _dialogService;
    private readonly ICommonDialogService _commonDialogService;
    private readonly ISessionManager _sessionManager;
    private readonly IPatientService _patientService;
    private readonly IUserService _userService;
    private readonly Guid _currentUserId = Guid.NewGuid();

    private sealed class TestableRegistrationListViewModel(
        IViewModelServices services,
        IRegistrationService registrationService,
        INavigationCoordinator navigationCoordinator,
        IPatientApi patientApi,
        IDialogService? dialogService = null)
        : RegistrationListViewModel(services, registrationService, navigationCoordinator, patientApi, dialogService)
    {
        public Task InitializePublicAsync() => base.InitializeAsync(CreateTestNavigationContext());
    }

    public RegistrationMasterDetailViewModelTests(UserJourneyFixture fixture) : base(fixture)
    {
        _viewModelServices = CreateViewModelServicesMock();

        // 使用真实的 LoggerFactory 确保 Logger 不为 null
        // NSubstitute mock 的 CreateLogger ReturnsForAnyArgs 在某些情况下无法拦截静态扩展方法调用
        var realLoggerFactory = LoggerFactory.Create(builder => { });
        _viewModelServices.LoggerFactory.Returns(realLoggerFactory);
        _registrationService = Substitute.For<IRegistrationService>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _patientApi = Substitute.For<IPatientApi>();
        _dialogService = Substitute.For<IDialogService>();
        _commonDialogService = _viewModelServices.CommonDialogService;
        _sessionManager = _viewModelServices.SessionManager;
        _patientService = Substitute.For<IPatientService>();
        _userService = Substitute.For<IUserService>();

        _sessionManager.CurrentUser.Returns(new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "receptionist",
            RealName = "前台",
            Role = UserRole.Receptionist,
            Status = CommonStatus.Enabled
        });
        _sessionManager.CurrentUserId.Returns(_currentUserId);
        _commonDialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
    }

    private TestableRegistrationListViewModel CreateListSut() => new(
        _viewModelServices,
        _registrationService,
        _navigationCoordinator,
        _patientApi,
        _dialogService);

    private RegistrationCreateDialogViewModel CreateDialogSut() => new(
        _viewModelServices,
        _patientService,
        _userService,
        _registrationService);

    private static RegistrationListDto CreateQueueItem(
        Guid? id = null,
        RegistrationStatus status = RegistrationStatus.Waiting,
        RegistrationSource source = RegistrationSource.Receptionist,
        string patientName = "张三",
        Guid? patientId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            PatientId = patientId ?? Guid.NewGuid(),
            PatientName = patientName,
            DoctorId = Guid.NewGuid(),
            DoctorName = "李医生",
            Source = source,
            Status = status,
            CreatedAt = DateTime.Now
        };

    private static RegistrationDetailDto CreateRegistrationDetail(Guid id, Guid patientId, string patientName)
        => new()
        {
            Id = id,
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = Guid.NewGuid(),
            DoctorName = "李医生",
            Source = RegistrationSource.Receptionist,
            Status = RegistrationStatus.Waiting,
            CreatedAt = DateTime.Now
        };

    private static NavigationContext CreateTestNavigationContext()
        => new(Substitute.For<IRegionNavigationService>(), new Uri("http://test"));

    [Fact]
    public async Task InitializeAsync_loads_registration_queue()
    {
        var sut = CreateListSut();
        var items = new List<RegistrationListDto>
        {
            CreateQueueItem(patientName: "张三"),
            CreateQueueItem(patientName: "李四")
        };

        _registrationService.GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<List<RegistrationListDto>>(true, items, null)));

        await sut.InitializePublicAsync();

        await _registrationService.Received(1).GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        sut.WaitingQueue.Should().HaveCount(2);
        sut.QueueCount.Should().Be(2);
        sut.HasQueueData.Should().BeTrue();
        sut.IsQueueEmpty.Should().BeFalse();
        sut.OnNavigatedFrom(CreateTestNavigationContext());
    }

    [Fact]
    public async Task InitializeAsync_uses_current_doctor_id_when_role_is_doctor()
    {
        _sessionManager.CurrentUser.Returns(new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "doctor",
            RealName = "医生",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled
        });

        var sut = CreateListSut();
        var doctorId = _sessionManager.CurrentUserId;

        _registrationService.GetQueueAsync(doctorId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<List<RegistrationListDto>>(true, [], null)));

        await sut.InitializePublicAsync();

        await _registrationService.Received(1).GetQueueAsync(doctorId, Arg.Any<CancellationToken>());
        sut.OnNavigatedFrom(CreateTestNavigationContext());
    }

    [Fact]
    public async Task SearchPatientsCommand_filters_results_and_updates_selection_ui()
    {
        var sut = CreateDialogSut();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        _patientService.SearchPatientsAsync("张", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<IEnumerable<PatientListDto>>(true,
                [new PatientListDto { Id = patientId, Name = "张三", Gender = Gender.Male }], null)));

        sut.PatientSearchText = "张";
        await sut.SearchPatientsCommand.ExecuteAsync(null);

        await _patientService.Received(1).SearchPatientsAsync("张", Arg.Any<CancellationToken>());
        sut.PatientSearchResults.Should().HaveCount(1);
        sut.ShowPatientResults.Should().BeTrue();
        sut.StatusMessage.Should().Contain("找到 1 位患者");

        sut.SelectPatientCommand.Execute(sut.PatientSearchResults[0]);
        sut.SelectedPatient.Should().NotBeNull();
        sut.PatientSearchText.Should().Be("张三");
        sut.ShowPatientResults.Should().BeFalse();
    }

    [Fact]
    public async Task CreateRegistrationCommand_opens_dialog_and_refreshes_queue()
    {
        var sut = CreateListSut();
        var queue = new List<RegistrationListDto> { CreateQueueItem(patientName: "张三") };
        _registrationService.GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<List<RegistrationListDto>>(true, queue, null)));

        IDialogParameters? capturedParameters = null;
        Action<IDialogResult>? capturedCallback = null;
        _dialogService.When(x => x.ShowDialog("RegistrationCreateDialog", Arg.Any<IDialogParameters>(), Arg.Any<Action<IDialogResult>>()))
            .Do(callInfo =>
            {
                capturedParameters = callInfo.ArgAt<IDialogParameters>(1);
                capturedCallback = callInfo.ArgAt<Action<IDialogResult>>(2);
            });

        await sut.InitializePublicAsync();
        sut.CreateRegistrationCommand.Execute(null);

        _dialogService.Received(1).ShowDialog("RegistrationCreateDialog", Arg.Any<IDialogParameters>(), Arg.Any<Action<IDialogResult>>());

        capturedCallback!.Invoke(new DialogResult(ButtonResult.OK));
        await Task.Delay(50);

        await _registrationService.Received(2).GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        sut.OnNavigatedFrom(CreateTestNavigationContext());
    }

    [Fact]
    public async Task StartVisitCommand_starts_visit_and_navigates_to_workspace()
    {
        var sut = CreateListSut();
        var registrationId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patientDetail = new PatientDetailDto { Id = patientId, Name = "张三", Gender = Gender.Male };

        _registrationService.GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<List<RegistrationListDto>>(true, [], null)));
        _registrationService.StartVisitAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<Guid>(true, Guid.NewGuid(), null)));
        _patientApi.GetPatientByIdAsync(patientId)
            .Returns(Task.FromResult(new ApiResponse<PatientDetailDto> { Success = true, Data = patientDetail }));

        await sut.InitializePublicAsync();
        sut.SelectedRegistration = CreateQueueItem(id: registrationId, patientId: patientId);

        await sut.StartVisitCommand.ExecuteAsync(null);

        await _registrationService.Received(1).StartVisitAsync(registrationId, Arg.Any<CancellationToken>());
        await _patientApi.Received(1).GetPatientByIdAsync(patientId);
        _navigationCoordinator.Received(1).NavigateTo(
            ViewNames.MedicalCaseWorkspace,
            Arg.Is<IDictionary<string, object>>(p =>
                p.ContainsKey(MedicalCaseNavigationParameters.MedicalCaseIdKey) &&
                p.ContainsKey("CurrentPatient") &&
                p.ContainsKey(MedicalCaseNavigationParameters.WorkspaceModeKey) &&
                p.ContainsKey(MedicalCaseNavigationParameters.InitialEditStateKey)));
        sut.OnNavigatedFrom(CreateTestNavigationContext());
    }

    [Fact]
    public async Task CancelRegistrationCommand_cancels_registration_and_refreshes_queue()
    {
        var sut = CreateListSut();
        var registrationId = Guid.NewGuid();

        _registrationService.GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<List<RegistrationListDto>>(true, [], null)));
        _registrationService.CancelAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult(true, null)));

        await sut.InitializePublicAsync();
        sut.SelectedRegistration = CreateQueueItem(id: registrationId);

        await sut.CancelRegistrationCommand.ExecuteAsync(null);

        await _commonDialogService.Received(1).ShowConfirmAsync(
            Arg.Any<string>(),
            Arg.Any<string>());
        await _registrationService.Received(1).CancelAsync(registrationId, Arg.Any<CancellationToken>());
        await _registrationService.Received(2).GetQueueAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        sut.OnNavigatedFrom(CreateTestNavigationContext());
    }
}
