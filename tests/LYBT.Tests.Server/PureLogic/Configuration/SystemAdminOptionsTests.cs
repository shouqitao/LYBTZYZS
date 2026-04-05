using FluentAssertions;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;

namespace LYBT.Tests.Server.PureLogic.Configuration;

public class SystemAdminOptionsTests
{
    [Fact]
    public void AllowAutoCreateInProduction_DefaultValue_ShouldBeFalse()
    {
        var options = new SystemAdminOptions();
        options.AllowAutoCreateInProduction.Should().BeFalse();
    }

    [Fact]
    public void InitialSetupToken_DefaultValue_ShouldBeNull()
    {
        var options = new SystemAdminOptions();
        options.InitialSetupToken.Should().BeNull();
    }

    [Theory]
    [InlineData(true, "setup-token-123")]
    [InlineData(false, null)]
    public void BindFromConfiguration_ShouldMapNewProperties(
        bool allowAutoCreate,
        string? setupToken)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemAdmin:AllowAutoCreateInProduction"] = allowAutoCreate.ToString(),
                ["SystemAdmin:InitialSetupToken"] = setupToken
            })
            .Build();

        var options = new SystemAdminOptions();
        configuration.GetSection(SystemAdminOptions.SectionName).Bind(options);

        options.AllowAutoCreateInProduction.Should().Be(allowAutoCreate);
        options.InitialSetupToken.Should().Be(setupToken);
    }

    [Fact]
    public void SectionName_ShouldBeSystemAdmin()
    {
        SystemAdminOptions.SectionName.Should().Be("SystemAdmin");
    }
}
