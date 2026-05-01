using FluentAssertions;
using LYBT.Desktop.LocalData.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Shell.Services;

public class ModeSwitchValidatorTests
{
    private readonly ILogger<ModeSwitchValidator> _logger;

    public ModeSwitchValidatorTests()
    {
        _logger = Substitute.For<ILogger<ModeSwitchValidator>>();
    }

    [Fact]
    public async Task ValidateRemoteToLocalSwitch_AlwaysReturnsValid()
    {
        var sut = new ModeSwitchValidator(_logger);

        var result = await sut.ValidateRemoteToLocalSwitchAsync();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_AlwaysReturnsValid()
    {
        var sut = new ModeSwitchValidator(_logger);

        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        result.IsValid.Should().BeTrue();
    }
}
