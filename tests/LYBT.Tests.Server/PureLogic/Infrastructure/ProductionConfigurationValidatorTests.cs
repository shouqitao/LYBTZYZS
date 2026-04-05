using FluentAssertions;
using LYBT.Infrastructure.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Infrastructure;

public class ProductionConfigurationValidatorTests
{
    private static Dictionary<string, string?> BuildFullConfig()
    {
        return new Dictionary<string, string?>
        {
            // Critical
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=LYBTDB;TrustServerCertificate=True",
            ["Jwt:SecretKey"] = "ThisIsASecretKeyThatIsAtLeast32CharsLong!!",

            // Important
            ["DefaultPasswords:SysAdminPassword"] = "Admin@123456",
            ["DefaultPasswords:NewUserPassword"] = "User@123456",
            ["SystemAdmin:UserName"] = "admin",
            ["SystemAdmin:Email"] = "admin@example.com",
            ["SystemAdmin:DisplayName"] = "系统管理员",
            ["Jwt:Issuer"] = "LYBT-WebAPI",
            ["Jwt:Audience"] = "LYBT-Client",

            // Optional
            ["AllowedHosts"] = "*",
            ["SystemAdmin:AllowAutoCreateInProduction"] = "false",
            ["SystemAdmin:InitialSetupToken"] = "ASecureTokenThatIsAtLeast32CharactersLong!"
        };
    }

    private static ProductionConfigurationValidator CreateValidator(Dictionary<string, string?> configData)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        return new ProductionConfigurationValidator(config);
    }

    #region ValidateCriticalItems

    [Fact]
    public void ValidateCriticalItems_WhenAllCriticalPresent_ReturnsEmpty()
    {
        // Arrange
        var validator = CreateValidator(BuildFullConfig());

        // Act
        var result = validator.ValidateCriticalItems();

        // Assert
        result.Should().BeEmpty("所有 Critical 配置项均已正确设置");
    }

    [Fact]
    public void ValidateCriticalItems_WhenCriticalAbsent_ReturnsMissingItems()
    {
        // Arrange
        var data = BuildFullConfig();
        data.Remove("ConnectionStrings:DefaultConnection");
        data.Remove("Jwt:SecretKey");
        var validator = CreateValidator(data);

        // Act
        var result = validator.ValidateCriticalItems();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Contains("ConnectionStrings:DefaultConnection"));
        result.Should().Contain(s => s.Contains("Jwt:SecretKey"));
    }

    [Fact]
    public void ValidateCriticalItems_WhenJwtSecretKeyTooShort_ReportsMinLengthViolation()
    {
        // Arrange
        var data = BuildFullConfig();
        data["Jwt:SecretKey"] = "short";  // 远少于 32 字符
        var validator = CreateValidator(data);

        // Act
        var result = validator.ValidateCriticalItems();

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("Jwt:SecretKey")
            .And.Contain("长度不足");
    }

    #endregion

    #region ValidateImportantItems

    [Fact]
    public void ValidateImportantItems_WhenAllImportantPresent_ReturnsEmpty()
    {
        // Arrange
        var validator = CreateValidator(BuildFullConfig());

        // Act
        var result = validator.ValidateImportantItems();

        // Assert
        result.Should().BeEmpty("所有 Important 配置项均已正确设置");
    }

    [Fact]
    public void ValidateImportantItems_WhenImportantAbsent_ReturnsMissingItems()
    {
        // Arrange
        var data = BuildFullConfig();
        data.Remove("DefaultPasswords:SysAdminPassword");
        data.Remove("SystemAdmin:UserName");
        var validator = CreateValidator(data);

        // Act
        var result = validator.ValidateImportantItems();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Contains("DefaultPasswords:SysAdminPassword"));
        result.Should().Contain(s => s.Contains("SystemAdmin:UserName"));
    }

    [Fact]
    public void ValidateImportantItems_WhenEmailFormatInvalid_ReportsFormatViolation()
    {
        // Arrange
        var data = BuildFullConfig();
        data["SystemAdmin:Email"] = "not-an-email";
        var validator = CreateValidator(data);

        // Act
        var result = validator.ValidateImportantItems();

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Contain("SystemAdmin:Email")
            .And.Contain("格式验证失败");
    }

    #endregion

    #region ValidateOrThrow

    [Fact]
    public void ValidateOrThrow_WhenAllConfigValid_DoesNotThrow()
    {
        // Arrange
        var validator = CreateValidator(BuildFullConfig());

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrThrow_WhenCriticalMissing_ThrowsProductionConfigurationException()
    {
        // Arrange
        var data = BuildFullConfig();
        data.Remove("ConnectionStrings:DefaultConnection");
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().Throw<ProductionConfigurationException>()
            .WithMessage("*ConnectionStrings:DefaultConnection*");
    }

    #endregion

    #region CrossFieldRules

    [Fact]
    public void ValidateOrThrow_WhenAutoCreateFalse_NoErrors()
    {
        // Arrange — AutoCreate=false, token irrelevant
        var data = BuildFullConfig();
        data["SystemAdmin:AllowAutoCreateInProduction"] = "false";
        data.Remove("SystemAdmin:InitialSetupToken");
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrThrow_WhenAutoCreateTrueAndValidToken_NoErrors()
    {
        // Arrange
        var data = BuildFullConfig();
        data["SystemAdmin:AllowAutoCreateInProduction"] = "true";
        data["SystemAdmin:InitialSetupToken"] = "ASecureTokenThatIsAtLeast32CharactersLong!";
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrThrow_WhenAutoCreateTrueAndMissingToken_ThrowsWithCrossFieldError()
    {
        // Arrange
        var data = BuildFullConfig();
        data["SystemAdmin:AllowAutoCreateInProduction"] = "true";
        data.Remove("SystemAdmin:InitialSetupToken");
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().Throw<ProductionConfigurationException>()
            .WithMessage("*InitialSetupToken*");
    }

    [Fact]
    public void ValidateOrThrow_WhenAutoCreateTrueAndPlaceholderToken_ThrowsWithCrossFieldError()
    {
        // Arrange — 未展开的环境变量占位符
        var data = BuildFullConfig();
        data["SystemAdmin:AllowAutoCreateInProduction"] = "true";
        data["SystemAdmin:InitialSetupToken"] = "${INITIAL_SETUP_TOKEN}";
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().Throw<ProductionConfigurationException>()
            .WithMessage("*占位符*");
    }

    [Fact]
    public void ValidateOrThrow_WhenAutoCreateTrueAndShortToken_ThrowsWithCrossFieldError()
    {
        // Arrange — Token 少于 32 字符
        var data = BuildFullConfig();
        data["SystemAdmin:AllowAutoCreateInProduction"] = "true";
        data["SystemAdmin:InitialSetupToken"] = "too-short-token";
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().Throw<ProductionConfigurationException>()
            .WithMessage("*InitialSetupToken*长度不足*");
    }

    [Fact]
    public void ValidateOrThrow_WhenAutoCreateNotSet_NoErrors()
    {
        // Arrange — AllowAutoCreateInProduction 未设置 (Optional 项, 不触发 CrossField)
        var data = BuildFullConfig();
        data.Remove("SystemAdmin:AllowAutoCreateInProduction");
        data.Remove("SystemAdmin:InitialSetupToken");
        var validator = CreateValidator(data);

        // Act & Assert
        var act = () => validator.ValidateOrThrow();
        act.Should().NotThrow();
    }

    #endregion
}
