using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.PureLogic.Registration;

public class RegistrationSourceTests
{
    [Fact]
    public void RegistrationSource_has_two_values()
    {
        Enum.GetValues<RegistrationSource>().Should().HaveCount(2);
    }

    [Theory]
    [InlineData(RegistrationSource.Receptionist, 0)]
    [InlineData(RegistrationSource.Doctor, 1)]
    public void RegistrationSource_has_correct_ordinal(RegistrationSource source, int expected)
    {
        ((int)source).Should().Be(expected);
    }

    [Fact]
    public void Receptionist_is_default_source()
    {
        default(RegistrationSource).Should().Be(RegistrationSource.Receptionist);
    }

    [Theory]
    [InlineData(RegistrationSource.Receptionist, "前台挂号")]
    [InlineData(RegistrationSource.Doctor, "医生看诊")]
    public void Source_description_matches(RegistrationSource source, string expected)
    {
        var description = source switch
        {
            RegistrationSource.Receptionist => "前台挂号",
            RegistrationSource.Doctor => "医生看诊",
            _ => string.Empty
        };

        description.Should().Be(expected);
    }

    [Theory]
    [InlineData(RegistrationSource.Receptionist, true)]
    [InlineData(RegistrationSource.Doctor, false)]
    public void Receptionist_goes_through_Waiting_state(RegistrationSource source, bool expected)
    {
        var goesThroughWaiting = source == RegistrationSource.Receptionist;

        goesThroughWaiting.Should().Be(expected);
    }

    [Theory]
    [InlineData(RegistrationSource.Receptionist, RegistrationStatus.Waiting, true)]
    [InlineData(RegistrationSource.Doctor, RegistrationStatus.Waiting, false)]
    [InlineData(RegistrationSource.Doctor, RegistrationStatus.InProgress, true)]
    public void Initial_status_matches_source_type(RegistrationSource source, RegistrationStatus status, bool expected)
    {
        var isInitial = source switch
        {
            RegistrationSource.Receptionist => status == RegistrationStatus.Waiting,
            RegistrationSource.Doctor => status == RegistrationStatus.InProgress,
            _ => false
        };

        isInitial.Should().Be(expected);
    }
}
