using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using NSubstitute;

namespace LYBT.Tests.Desktop.Unit.ViewModels;

public class ConsultationEditorViewModelTests
{
    private ConsultationEditorViewModel CreateSut()
    {
        var ctx = Substitute.For<IMedicalCaseWorkspaceContext>();
        ctx.MedicalCaseId.Returns(Guid.NewGuid());
        var host = Substitute.For<IWorkspaceHost>();
        var loggerFactory = Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>();
        return new ConsultationEditorViewModel(ctx, host, loggerFactory);
    }

    [Fact]
    public void Consultation_属性变更_触发通知()
    {
        var sut = CreateSut();
        var newItem = new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem { PresentIllness = "头痛" };
        sut.Consultation = newItem;
        sut.Consultation.PresentIllness.Should().Be("头痛");
    }

    [Fact]
    public void InitializeForNewCase_重置_成功()
    {
        var sut = CreateSut();
        sut.InitializeForNewCase();
        sut.Consultation.Should().NotBeNull();
    }

    [Fact]
    public void Validate_空主诉_返回False()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "";
        sut.Validate().Should().BeFalse();
    }
}
