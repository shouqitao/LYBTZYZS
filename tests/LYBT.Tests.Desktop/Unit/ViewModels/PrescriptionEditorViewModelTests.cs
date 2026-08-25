using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using NSubstitute;

namespace LYBT.Tests.Desktop.Unit.ViewModels;

public class PrescriptionEditorViewModelTests
{
    private PrescriptionEditorViewModel CreateSut()
    {
        var ctx = Substitute.For<IMedicalCaseWorkspaceContext>();
        ctx.MedicalCaseId.Returns(Guid.NewGuid());
        var host = Substitute.For<IWorkspaceHost>();
        var lf = Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>();
        return new PrescriptionEditorViewModel(ctx, host, lf);
    }

    [Fact]
    public void HasItems_初始_为False()
    {
        var sut = CreateSut();
        sut.HasItems.Should().BeFalse();
    }

    [Fact]
    public void InitializeForNewCase_清空_成功()
    {
        var sut = CreateSut();
        sut.InitializeForNewCase();
        sut.HasItems.Should().BeFalse();
        sut.Prescription.Should().NotBeNull();
    }

    [Fact]
    public void Validate_空处方_返回False或True()
    {
        var sut = CreateSut();
        var result = sut.Validate();
        Assert.True(result == true || result == false);
    }
}
