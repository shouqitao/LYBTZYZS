using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Shell.Services;

/// <summary>
/// ModeSwitchValidator 单元测试 (US-SYNC-008)
/// SYNC-D02: ModeSwitchValidator 不再依赖 DataSource/Repository，直接使用 SQL 查询
/// 本地->远程检查需要真实数据库连接，使用集成测试覆盖
/// 这里仅测试远程->本地检查 (连接字符串验证)
/// </summary>
public class ModeSwitchValidatorTests
{
    private readonly ILogger<ModeSwitchValidator> _logger;

    public ModeSwitchValidatorTests()
    {
        _logger = Substitute.For<ILogger<ModeSwitchValidator>>();
    }

    #region 远程 -> 本地切换检查

    [Fact]
    public async Task ValidateRemoteToLocalSwitch_InvalidConnectionString_ReturnsInvalid()
    {
        // Arrange: use a clearly invalid connection string
        var sut = CreateValidator(localConnectionString: "Server=INVALID_SERVER_12345;Database=INVALID;Connect Timeout=1;");

        // Act
        var result = await sut.ValidateRemoteToLocalSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("本地数据库");
    }

    [Fact]
    public async Task ValidateLocalToRemoteSwitch_InvalidConnectionString_ReturnsInvalid()
    {
        // Arrange: invalid connection = exception = returns failed
        var sut = CreateValidator(localConnectionString: "Server=INVALID_SERVER_12345;Database=INVALID;Connect Timeout=1;");

        // Act
        var result = await sut.ValidateLocalToRemoteSwitchAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("检查失败");
    }

    #endregion

    #region Helper Methods

    private ModeSwitchValidator CreateValidator(string? localConnectionString = null)
    {
        return new ModeSwitchValidator(
            localConnectionString ?? "Server=(localdb)\\MSSQLLocalDB;Database=LYBTZYZS_Test_Switch;Trusted_Connection=True;Connect Timeout=3;",
            _logger);
    }

    #endregion
}
