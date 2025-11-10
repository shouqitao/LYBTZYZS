using LYBT.Infrastructure.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LYBT.UnitTests.Infrastructure.Configuration;

public class ProductionConfigurationValidatorTests
{
    [Fact]
    public void ValidateOrThrow_AllConfigsValid_ShouldNotThrow()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=LYBTDB;User Id=sa;Password=Test123;",
                ["Lybt:Jwt:SecretKey"] = "ThisIsAVeryLongSecretKeyForJWT123456",
                ["Lybt:DefaultPasswords:SysAdminPassword"] = "Admin@123",
                ["Lybt:DefaultPasswords:NewUserPassword"] = "User@123",
                ["Lybt:SystemAdmin:Username"] = "admin",
                ["Lybt:SystemAdmin:Email"] = "admin@example.com",
                ["AllowedHosts"] = "example.com"
            })
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        validator.ValidateOrThrow(); // 不应抛出异常
    }

    [Fact]
    public void ValidateOrThrow_MissingCriticalConfig_ShouldThrowWithDetails()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<ProductionConfigurationException>(() => validator.ValidateOrThrow());
        Assert.Contains("数据库连接字符串", ex.Message);
        Assert.Contains("ConnectionStrings__DefaultConnection", ex.Message);
        Assert.Contains("setx", ex.Message); // 应包含修复命令
    }

    [Fact]
    public void ValidateOrThrow_PlaceholderInConfig_ShouldThrowWithPlaceholderError()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "#{DATABASE_CONNECTION_STRING}#",
                ["Lybt:Jwt:SecretKey"] = "ValidSecretKeyWithAtLeast32Chars",
                ["Lybt:DefaultPasswords:SysAdminPassword"] = "Admin@123",
                ["Lybt:DefaultPasswords:NewUserPassword"] = "User@123",
                ["Lybt:SystemAdmin:Username"] = "admin",
                ["Lybt:SystemAdmin:Email"] = "admin@example.com"
            })
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<ProductionConfigurationException>(() => validator.ValidateOrThrow());
        Assert.Contains("占位符", ex.Message);
        Assert.Contains("#{DATABASE_CONNECTION_STRING}#", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_JwtKeyTooShort_ShouldThrowWithLengthError()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=LYBTDB;",
                ["Lybt:Jwt:SecretKey"] = "Short", // < 32 字符
                ["Lybt:DefaultPasswords:SysAdminPassword"] = "Admin@123",
                ["Lybt:DefaultPasswords:NewUserPassword"] = "User@123",
                ["Lybt:SystemAdmin:Username"] = "admin",
                ["Lybt:SystemAdmin:Email"] = "admin@example.com"
            })
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<ProductionConfigurationException>(() => validator.ValidateOrThrow());
        Assert.Contains("长度不足", ex.Message);
        Assert.Contains("32", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_InvalidEmailFormat_ShouldThrowWithFormatError()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=LYBTDB;",
                ["Lybt:Jwt:SecretKey"] = "ValidSecretKeyWithAtLeast32Chars",
                ["Lybt:DefaultPasswords:SysAdminPassword"] = "Admin@123",
                ["Lybt:DefaultPasswords:NewUserPassword"] = "User@123",
                ["Lybt:SystemAdmin:Username"] = "admin",
                ["Lybt:SystemAdmin:Email"] = "invalid-email" // 无效邮箱格式
            })
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<ProductionConfigurationException>(() => validator.ValidateOrThrow());
        Assert.Contains("格式验证失败", ex.Message);
    }

    [Fact]
    public void ValidateOrThrow_OptionalConfigMissing_ShouldNotThrow()
    {
        // Arrange - AllowedHosts 是可选配置
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=LYBTDB;",
                ["Lybt:Jwt:SecretKey"] = "ValidSecretKeyWithAtLeast32Chars",
                ["Lybt:DefaultPasswords:SysAdminPassword"] = "Admin@123",
                ["Lybt:DefaultPasswords:NewUserPassword"] = "User@123",
                ["Lybt:SystemAdmin:Username"] = "admin",
                ["Lybt:SystemAdmin:Email"] = "admin@example.com"
                // AllowedHosts 未设置
            })
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        validator.ValidateOrThrow(); // 不应抛出异常
    }

    [Fact]
    public void ValidateOrThrow_PartialConfig_ShouldReportOnlyMissingItems()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=LYBTDB;",
                ["Lybt:Jwt:SecretKey"] = "ValidSecretKeyWithAtLeast32Chars"
                // 缺少其他 Important 配置
            })
            .Build();

        var validator = new ProductionConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<ProductionConfigurationException>(() => validator.ValidateOrThrow());
        Assert.Contains("SysAdminPassword", ex.Message);
        Assert.Contains("NewUserPassword", ex.Message);
        Assert.Contains("Username", ex.Message);
        Assert.Contains("Email", ex.Message);
    }

    [Fact]
    public void Constructor_NullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProductionConfigurationValidator(null!));
    }
}