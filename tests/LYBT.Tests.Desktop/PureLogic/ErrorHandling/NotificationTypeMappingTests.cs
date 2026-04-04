using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Handlers;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.ErrorHandling;

public class NotificationTypeMappingTests
{
    #region US-ERR-008: Exception severity enum values (ExceptionSeverityMapper does not exist)

    [Fact(Skip = "ExceptionSeverityMapper class does not exist on disk — pending implementation (SUSPECT dead code per CLAUDE.md)")]
    public void US_ERR_008_ExceptionSeverityMapper_MapsToToastForInfo()
    {
    }

    [Fact(Skip = "ExceptionSeverityMapper class does not exist on disk — pending implementation (SUSPECT dead code per CLAUDE.md)")]
    public void US_ERR_008_ExceptionSeverityMapper_MapsToDialogForError()
    {
    }

    [Fact]
    public void US_ERR_008_ExceptionSeverity_EnumValues_AreCorrect()
    {
        ExceptionSeverity.Information.Should().Be((ExceptionSeverity)0);
        ExceptionSeverity.Warning.Should().Be((ExceptionSeverity)1);
        ExceptionSeverity.Error.Should().Be((ExceptionSeverity)2);
        ExceptionSeverity.Critical.Should().Be((ExceptionSeverity)3);
    }

    [Fact]
    public void US_ERR_008_ExceptionSeverity_HasExpectedMemberCount()
    {
        var values = Enum.GetValues<ExceptionSeverity>();
        values.Length.Should().Be(4,
            "ExceptionSeverity should have exactly 4 levels: Information, Warning, Error, Critical");
    }

    #endregion
}
