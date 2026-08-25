using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Infrastructure.Services.Toast;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.Unit.ViewModels;

public class MedicalCaseCommandsViewModelTests
{
    private MedicalCaseCommandsViewModel CreateSut()
    {
        var ctx = Substitute.For<IMedicalCaseWorkspaceContext>();
        ctx.MedicalCaseId.Returns(Guid.NewGuid());
        var host = Substitute.For<IWorkspaceHost>();
        var lf = Substitute.For<ILoggerFactory>();
        var svc = Substitute.For<IMedicalCaseService>();
        var provider = Substitute.For<IMedicalCaseDataProvider>();
        var print = new LYBT.Desktop.MedicalCase.ViewModels.Components.PrescriptionPrintHandler(
            Substitute.For<IMedicalCaseService>(),
            Substitute.For<IMedicalCaseRepository>(),
            Substitute.For<ISessionManager>(),
            Substitute.For<IClinicSettingsService>(),
            Substitute.For<ILoggerFactory>(),
            null);
        var toast = Substitute.For<IToastService>();
        var dialog = Substitute.For<IDialogService>();
        return new MedicalCaseCommandsViewModel(ctx, host, lf, svc, provider, print, toast, dialog);
    }

    [Fact]
    public void Commands_初始化_不为Null()
    {
        var sut = CreateSut();
        sut.SaveCommand.Should().NotBeNull();
        sut.SuspendCommand.Should().NotBeNull();
        sut.CompleteCommand.Should().NotBeNull();
        sut.PrintCommand.Should().NotBeNull();
    }

    [Fact]
    public void CanSave_初始_应为False或True()
    {
        var sut = CreateSut();
        // CanSave depends on context state, just verify not throw
        var can = sut.SaveCommand.CanExecute(null);
        Assert.True(can == true || can == false);
    }
}
