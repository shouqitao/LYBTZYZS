using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// PendingQueueViewModel 单元测试
/// 验证待诊队列子VM在患者选择上下文（无活跃医案）下的行为
/// TDD RED 阶段 - 定义移到 PatientSelectionViewModel 后的预期行为
/// </summary>
public class PendingQueueViewModelTests
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IWorkspaceHost _host;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ObservableCollection<PendingMedicalCaseDto> _emptyQueue;

    public PendingQueueViewModelTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _context = Substitute.For<IMedicalCaseWorkspaceContext>();
        _host = Substitute.For<IWorkspaceHost>();
        var dialogService = Substitute.For<ICommonDialogService>();
        dialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        _host.CommonDialogService.Returns(dialogService);
        _medicalCaseService = Substitute.For<IMedicalCaseService>();
        _pendingQueueManager = Substitute.For<IPendingQueueManager>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();

        _emptyQueue = new ObservableCollection<PendingMedicalCaseDto>();
        _pendingQueueManager.PendingQueue.Returns(_emptyQueue);
        _context.MedicalCaseId.Returns(Guid.Empty);
        _context.State.Returns(new WorkspaceState(EditState: EditState.ReadOnly, CanEdit: false));
    }

    private PendingQueueViewModel CreateSut() => new(
        _context,
        _host,
        _loggerFactory,
        _medicalCaseService,
        _pendingQueueManager,
        _navigationCoordinator);

    [Fact]
    public void Queue_IsObservableCollection_BackedByPendingQueueManager()
    {
        var sut = CreateSut();

        sut.Queue.Should().BeSameAs(_emptyQueue);
    }

    [Fact]
    public void HasNoPendingCases_ReturnsTrue_WhenQueueIsEmpty()
    {
        var sut = CreateSut();

        sut.HasNoPendingCases.Should().BeTrue();
    }

    [Fact]
    public void HasNoPendingCases_ReturnsFalse_WhenQueueHasItems()
    {
        _emptyQueue.Add(new PendingMedicalCaseDto { PatientId = Guid.NewGuid() });
        var sut = CreateSut();

        sut.HasNoPendingCases.Should().BeFalse();
    }

    [Fact]
    public async Task SelectPendingCaseAsync_WithNoActiveMedicalCaseId_SkipsSuspend_NavigatesDirectly()
    {
        _context.MedicalCaseId.Returns(Guid.Empty);
        var targetCase = new PendingMedicalCaseDto
        {
            PatientId = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            CaseStatus = MedicalCaseStatus.Suspended
        };
        _context.CurrentPatient.Returns(new PatientDetailDto
        {
            Id = targetCase.PatientId,
            Name = "Test Patient"
        });
        var sut = CreateSut();
        var suspendWasCalled = false;
        sut.SuspendCurrentCase = () =>
        {
            suspendWasCalled = true;
            return Task.CompletedTask;
        };

        await sut.SelectPendingCaseAsync(targetCase);

        suspendWasCalled.Should().BeFalse("no active medical case — suspend should be skipped");
        _navigationCoordinator.Received().NavigateTo(
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task RefreshQueueAsync_WithEmptyQueue_DoesNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.RefreshQueueAsync();
        await act.Should().NotThrowAsync();
    }
}
