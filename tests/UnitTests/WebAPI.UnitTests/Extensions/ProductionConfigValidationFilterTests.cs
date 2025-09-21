using FluentAssertions;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// ProductionConfigValidationFilter 测试
/// </summary>
public class ProductionConfigValidationFilterTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IApplicationBuilder> _mockAppBuilder;

    public ProductionConfigValidationFilterTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockAppBuilder = new Mock<IApplicationBuilder>();

        // 设置基本配置
        SetupBasicConfiguration();
    }

    private void SetupBasicConfiguration()
    {
        // 设置有效的数据库连接字符串
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Server=localhost;Database=TestDB;Trusted_Connection=true;");

        // 设置有效的JWT密钥
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("ValidJwtSecretKeyThatIsAtLeast32CharactersLong123456789");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => new ProductionConfigValidationFilter(_mockConfiguration.Object);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrow()
    {
        // Act & Assert
        var action = () => new ProductionConfigValidationFilter(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configure_WithValidConfiguration_ShouldReturnConfiguredAction()
    {
        // Arrange
        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        var next = new Mock<Action<IApplicationBuilder>>();

        // Act
        var result = filter.Configure(next.Object);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Configure_WhenExecuted_ShouldCallNext()
    {
        // Arrange
        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        var nextCalled = false;
        Action<IApplicationBuilder> next = app => { nextCalled = true; };

        // Act
        var configuredAction = filter.Configure(next);
        configuredAction(_mockAppBuilder.Object);

        // Assert
        nextCalled.Should().BeTrue("应该调用下一个配置方法");
    }

    [Fact]
    public void Configure_WithValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().NotThrow("有效配置应该通过验证");
    }

    [Fact]
    public void Configure_WithEmptyConnectionString_ShouldThrow()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns(string.Empty);

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*数据库连接字符串*不能为空*");
    }

    [Fact]
    public void Configure_WithNullConnectionString_ShouldThrow()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns((string?)null);

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*数据库连接字符串*不能为空*");
    }

    [Fact]
    public void Configure_WithWhitespaceConnectionString_ShouldThrow()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("   ");

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*数据库连接字符串*不能为空*");
    }

    [Fact]
    public void Configure_WithEmptyJwtSecret_ShouldThrow()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns(string.Empty);

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT密钥*不能为空*");
    }

    [Fact]
    public void Configure_WithNullJwtSecret_ShouldThrow()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns((string?)null);

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT密钥*不能为空*");
    }

    [Fact]
    public void Configure_WithShortJwtSecret_ShouldThrow()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("TooShort"); // 少于32位

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT密钥*长度至少32位*");
    }

    [Fact]
    public void Configure_WithExactly32CharJwtSecret_ShouldPass()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("12345678901234567890123456789012"); // 正好32位

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().NotThrow("32位JWT密钥应该通过验证");
    }

    [Fact]
    public void Configure_WithMultipleErrors_ShouldIncludeAllInMessage()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns(string.Empty);
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("short");

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        var exception = action.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("数据库连接字符串");
        exception.Message.Should().Contain("JWT密钥");
    }

    [Fact]
    public void Configure_ShouldValidateInCorrectOrder()
    {
        // Arrange
        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        var validationCalled = false;
        Action<IApplicationBuilder> next = app => { validationCalled = true; };

        // Act
        var configuredAction = filter.Configure(next);
        configuredAction(_mockAppBuilder.Object);

        // Assert
        validationCalled.Should().BeTrue("验证应该在调用next之前完成");
    }

    [Fact]
    public void Configure_WithValidationFailure_ShouldNotCallNext()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns(string.Empty);

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        var nextCalled = false;
        Action<IApplicationBuilder> next = app => { nextCalled = true; };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().Throw<InvalidOperationException>();
        nextCalled.Should().BeFalse("验证失败时不应该调用next");
    }

    [Fact]
    public void Configure_ShouldCreateDetailedErrorReport()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("  "); // 空白字符串
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("x"); // 太短的密钥

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        var exception = action.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("生产环境配置验证失败");
        exception.Message.Should().ContainAll(
            "数据库连接字符串",
            "JWT密钥",
            "不能为空",
            "长度至少32位"
        );
    }

    [Fact]
    public void Configure_WithLongJwtSecret_ShouldPass()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("ThisIsAVeryLongJwtSecretKeyThatIsDefinitelyMoreThan32CharactersLongAndShouldPassValidation");

        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction = filter.Configure(next);
        var action = () => configuredAction(_mockAppBuilder.Object);

        // Assert
        action.Should().NotThrow("长JWT密钥应该通过验证");
    }

    [Fact]
    public void Configure_MultipleCallsWithSameFilter_ShouldBehaveConsistently()
    {
        // Arrange
        var filter = new ProductionConfigValidationFilter(_mockConfiguration.Object);
        Action<IApplicationBuilder> next = app => { };

        // Act
        var configuredAction1 = filter.Configure(next);
        var configuredAction2 = filter.Configure(next);

        var action1 = () => configuredAction1(_mockAppBuilder.Object);
        var action2 = () => configuredAction2(_mockAppBuilder.Object);

        // Assert
        action1.Should().NotThrow("第一次调用应该成功");
        action2.Should().NotThrow("第二次调用应该成功");
    }
}