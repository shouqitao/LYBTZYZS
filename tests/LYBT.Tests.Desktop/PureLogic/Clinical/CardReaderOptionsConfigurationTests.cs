using LYBT.Desktop.CardReader.Abstractions;
using Microsoft.Extensions.Configuration;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// PRD-13: CardReaderOptions 从 appsettings.json 读取配置
/// TDD RED: 验证配置绑定和默认值行为
/// </summary>
public class CardReaderOptionsConfigurationTests
{
    [Fact]
    public void SectionName_is_CardReader()
    {
        CardReaderOptions.SectionName.Should().Be("CardReader");
    }

    [Fact]
    public void Binds_from_configuration_section()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CardReader:UsbPort"] = "2001",
                ["CardReader:ConnectTimeout"] = "3000",
                ["CardReader:ReadTimeout"] = "5000",
                ["CardReader:AutoReconnect"] = "false",
                ["CardReader:ReconnectInterval"] = "1500",
                ["CardReader:SerialPort"] = "3",
            })
            .Build();

        var options = config.GetSection(CardReaderOptions.SectionName)
            .Get<CardReaderOptions>();

        options.Should().NotBeNull();
        options!.UsbPort.Should().Be(2001);
        options.ConnectTimeout.Should().Be(3000);
        options.ReadTimeout.Should().Be(5000);
        options.AutoReconnect.Should().BeFalse();
        options.ReconnectInterval.Should().Be(1500);
        options.SerialPort.Should().Be(3);
    }

    [Fact]
    public void Uses_defaults_when_section_missing()
    {
        var config = new ConfigurationBuilder().Build();

        var options = config.GetSection(CardReaderOptions.SectionName)
            .Get<CardReaderOptions>() ?? new CardReaderOptions();

        options.UsbPort.Should().Be(1001);
        options.ConnectTimeout.Should().Be(5000);
        options.ReadTimeout.Should().Be(10000);
        options.AutoReconnect.Should().BeTrue();
        options.ReconnectInterval.Should().Be(3000);
        options.SerialPort.Should().Be(0);
    }

    [Fact]
    public void Partial_config_uses_defaults_for_missing_properties()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CardReader:UsbPort"] = "3001",
            })
            .Build();

        var options = config.GetSection(CardReaderOptions.SectionName)
            .Get<CardReaderOptions>();

        options.Should().NotBeNull();
        options!.UsbPort.Should().Be(3001);
        // Other properties should retain defaults
        options.ConnectTimeout.Should().Be(5000);
        options.ReadTimeout.Should().Be(10000);
        options.AutoReconnect.Should().BeTrue();
    }
}
