using FluentAssertions;
using LYBT.Entities.Patients;
using Xunit;

namespace LYBT.Tests.Server.Unit.Entities.Patients;

/// <summary>
/// Patient entity tests - Age computed property only.
/// Trivial property getter/setter tests removed (test restructuring 2026-03-05).
/// </summary>
public class PatientModelTests
{
    [Fact]
    public void Age_WhenBirthDateIsNull_ShouldReturnNull()
    {
        var patient = new Patient { BirthDate = null };
        patient.Age.Should().BeNull();
    }

    [Fact]
    public void Age_WhenBirthDateIsSet_ShouldCalculateCorrectAge()
    {
        var patient = new Patient { BirthDate = DateTime.Today.AddYears(-30) };
        patient.Age.Should().Be(30);
    }

    [Fact]
    public void Age_WhenBirthDateIsThisYear_ShouldReturn0()
    {
        var patient = new Patient { BirthDate = DateTime.Today.AddMonths(-6) };
        patient.Age.Should().Be(0);
    }

    [Fact]
    public void Age_WhenBirthdayNotYetReached_ShouldSubtractOne()
    {
        var birthDate = new DateTime(DateTime.Today.Year - 25, DateTime.Today.Month + 1, DateTime.Today.Day);
        var patient = new Patient { BirthDate = birthDate };
        patient.Age.Should().Be(24);
    }
}
