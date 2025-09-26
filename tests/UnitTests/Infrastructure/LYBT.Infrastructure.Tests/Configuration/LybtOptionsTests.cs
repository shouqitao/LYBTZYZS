using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LYBT.Infrastructure.Configuration.Extensions;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace LYBT.Infrastructure.Tests.Configuration;

/// <summary>
/// LybtOptions 配置测试
/// 验证统一配置选项的绑定、验证和兼容性
/// </summary>
[TestFixture]
public class LybtOptionsTests
{
    private IConfiguration _configuration;
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void SetUp()
    {
        // 创建测试配置
        var configDict = new Dictionary<string, string>
        {
            // JWT 配置
            ["Lybt:Authentication:Jwt:SecretKey"] = "LybtJwtSecretKey2025!@#$%^&*()ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client",
            ["Lybt:Authentication:Jwt:AccessTokenExpirationMinutes"] = "480",
            ["Lybt:Authentication:Jwt:RefreshTokenExpirationDays"] = "30",
            
            // 密码策略配置
            ["Lybt:Authentication:PasswordPolicy:MinLength"] = "8",
            ["Lybt:Authentication:PasswordPolicy:MaxLength"] = "100",
            ["Lybt:Authentication:PasswordPolicy:RequireDigit"] = "true",
            ["Lybt:Authentication:PasswordPolicy:RequireUppercase"] = "true",
            ["Lybt:Authentication:PasswordPolicy:RequireLowercase"] = "true",
            ["Lybt:Authentication:PasswordPolicy:RequireSpecialChar"] = "true",
            
            // 会话配置
            ["Lybt:Authentication:Session:TimeoutMinutes"] = "120",
            ["Lybt:Authentication:Session:AllowConcurrentSessions"] = "false",
            
            // 默认密码配置
            ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "LybtAdmin2025@SecurePass!",
            ["Lybt:Authentication:DefaultPasswords:NewUserPassword"] = "Lybt2025@TempPass!",
            
            // 数据库配置
            ["Lybt:Infrastructure:Database:ConnectionString"] = "Server=localhost;Database=Test;Trusted_Connection=true;",
            ["Lybt:Infrastructure:Database:ConnectionPool:MaxConnections"] = "100",
            ["Lybt:Infrastructure:Database:ConnectionPool:MinConnections"] = "5",
            
            // 缓存配置
            ["Lybt:Infrastructure:Cache:MemoryCache:SizeLimit"] = "104857600",
            ["Lybt:Infrastructure:Cache:DistributedCache:Type"] = "Memory",
            
            // 安全配置
            ["Lybt:Security:RateLimiting:Enabled"] = "true",
            ["Lybt:Security:RateLimiting:GlobalLimit:PermitLimit"] = "1000",
            
            // 业务配置
            ["Lybt:Business:SystemAdmin:Username"] = "sysadmin",
            ["Lybt:Business:SystemAdmin:Email"] = "admin@lybt.com",
            ["Lybt:Business:UserManagement:DefaultRole"] = "Staff"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // 设置服务容器
        var services = new ServiceCollection();
        services.AddLybtConfiguration(_configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public void GetLybtOptions_ShouldBindCorrectly()
    {
        // Act
        var options = _configuration.GetLybtOptions();

        // Assert
        options.Should().NotBeNull();
        
        // 验证 JWT 配置
        options.Authentication.Jwt.SecretKey.Should().Be("LybtJwtSecretKey2025!@#$%^&*()ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        options.Authentication.Jwt.Issuer.Should().Be("LYBT.WebAPI");
        options.Authentication.Jwt.Audience.Should().Be("LYBT.Client");
        options.Authentication.Jwt.AccessTokenExpirationMinutes.Should().Be(480);
        options.Authentication.Jwt.RefreshTokenExpirationDays.Should().Be(30);

        // 验证密码策略配置
        options.Authentication.PasswordPolicy.MinLength.Should().Be(8);
        options.Authentication.PasswordPolicy.MaxLength.Should().Be(100);
        options.Authentication.PasswordPolicy.RequireDigit.Should().BeTrue();
        options.Authentication.PasswordPolicy.RequireUppercase.Should().BeTrue();

        // 验证会话配置
        options.Authentication.Session.TimeoutMinutes.Should().Be(120);
        options.Authentication.Session.AllowConcurrentSessions.Should().BeFalse();

        // 验证数据库配置
        options.Infrastructure.Database.ConnectionString.Should().Be("Server=localhost;Database=Test;Trusted_Connection=true;");
        options.Infrastructure.Database.ConnectionPool.MaxConnections.Should().Be(100);
        options.Infrastructure.Database.ConnectionPool.MinConnections.Should().Be(5);

        // 验证业务配置
        options.Business.SystemAdmin.Username.Should().Be("sysadmin");
        options.Business.SystemAdmin.Email.Should().Be("admin@lybt.com");
        options.Business.UserManagement.DefaultRole.Should().Be("Staff");
    }

    [Test]
    public void LybtOptions_InjectedOptions_ShouldBindCorrectly()
    {
        // Act
        var optionsInstance = _serviceProvider.GetRequiredService<IOptions<LybtOptions>>();
        var options = optionsInstance.Value;

        // Assert
        options.Should().NotBeNull();
        options.Authentication.Jwt.Issuer.Should().Be("LYBT.WebAPI");
        options.Infrastructure.Database.ConnectionString.Should().Contain("Server=localhost");
    }

    [Test]
    public void ValidateLybtConfiguration_WithValidConfig_ShouldSucceed()
    {
        // Act
        var validationResult = _configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Test]
    public void ValidateLybtConfiguration_WithMissingJwtSecretKey_ShouldFail()
    {
        // Arrange
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client"
            // 缺少 SecretKey
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("JWT SecretKey is required");
    }

    [Test]
    public void ValidateLybtConfiguration_WithShortJwtSecretKey_ShouldFail()
    {
        // Arrange
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:SecretKey"] = "short",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client",
            ["Lybt:Infrastructure:Database:ConnectionString"] = "test",
            ["Lybt:Business:SystemAdmin:Username"] = "admin",
            ["Lybt:Business:SystemAdmin:Email"] = "admin@test.com",
            ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "password"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("JWT SecretKey must be at least 32 characters");
    }

    [Test]
    public void ValidateLybtConfiguration_WithMissingDatabaseConnection_ShouldFail()
    {
        // Arrange
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:SecretKey"] = "LybtJwtSecretKey2025!@#$%^&*()ABCD",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client"
            // 缺少数据库连接字符串
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("Database ConnectionString is required");
    }

    [Test]
    public void ValidateLybtConfiguration_WithInvalidPasswordPolicy_ShouldFail()
    {
        // Arrange - MinLength > MaxLength
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:SecretKey"] = "LybtJwtSecretKey2025!@#$%^&*()ABCD",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client",
            ["Lybt:Authentication:PasswordPolicy:MinLength"] = "20",
            ["Lybt:Authentication:PasswordPolicy:MaxLength"] = "10", // 小于 MinLength
            ["Lybt:Infrastructure:Database:ConnectionString"] = "test",
            ["Lybt:Business:SystemAdmin:Username"] = "admin",
            ["Lybt:Business:SystemAdmin:Email"] = "admin@test.com",
            ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "password"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("Password MinLength cannot be greater than MaxLength");
    }

    [Test]
    public void ValidateLybtConfiguration_WithInvalidConnectionPool_ShouldFail()
    {
        // Arrange - MinConnections > MaxConnections
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:SecretKey"] = "LybtJwtSecretKey2025!@#$%^&*()ABCD",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client",
            ["Lybt:Infrastructure:Database:ConnectionString"] = "test",
            ["Lybt:Infrastructure:Database:ConnectionPool:MinConnections"] = "100",
            ["Lybt:Infrastructure:Database:ConnectionPool:MaxConnections"] = "50", // 小于 MinConnections
            ["Lybt:Business:SystemAdmin:Username"] = "admin",
            ["Lybt:Business:SystemAdmin:Email"] = "admin@test.com",
            ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "password"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("Database MinConnections cannot be greater than MaxConnections");
    }

    [Test]
    public void PasswordPolicyConfiguration_DataAnnotationValidation_ShouldWork()
    {
        // Arrange
        var passwordPolicy = new PasswordPolicyConfiguration
        {
            MinLength = 200, // 超出范围
            RequireDigit = true,
            RequireUppercase = true
        };

        var context = new ValidationContext(passwordPolicy);
        var validationResults = new List<ValidationResult>();

        // Act
        bool isValid = Validator.TryValidateObject(passwordPolicy, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("MinLength"));
    }

    [Test]
    public void JwtConfiguration_DataAnnotationValidation_ShouldWork()
    {
        // Arrange
        var jwtConfig = new JwtConfiguration
        {
            SecretKey = "short", // 太短
            Issuer = "", // 必填项为空
            Audience = "LYBT.Client",
            AccessTokenExpirationMinutes = 0 // 超出范围
        };

        var context = new ValidationContext(jwtConfig);
        var validationResults = new List<ValidationResult>();

        // Act
        bool isValid = Validator.TryValidateObject(jwtConfig, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().NotBeEmpty();
    }

    [Test]
    public void LegacyCompatibilityOptions_ShouldBeRegistered()
    {
        // Act & Assert - 验证所有传统配置选项都已注册
        var authOptions = _serviceProvider.GetService<IOptions<AuthOptions>>();
        var jwtOptions = _serviceProvider.GetService<IOptions<JwtOptions>>();
        var databaseOptions = _serviceProvider.GetService<IOptions<DatabaseOptions>>();
        var userOptions = _serviceProvider.GetService<IOptions<UserOptions>>();

        authOptions.Should().NotBeNull();
        jwtOptions.Should().NotBeNull();
        databaseOptions.Should().NotBeNull();
        userOptions.Should().NotBeNull();

        // 验证映射是否正确
        jwtOptions.Value.SecretKey.Should().Be("LybtJwtSecretKey2025!@#$%^&*()ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        jwtOptions.Value.Issuer.Should().Be("LYBT.WebAPI");
        userOptions.Value.DefaultRole.Should().Be("Staff");
    }

    [Test]
    public void DistributedCacheType_Enum_ShouldHaveCorrectValues()
    {
        // Act & Assert
        var memoryType = DistributedCacheType.Memory;
        var redisType = DistributedCacheType.Redis;
        var sqlServerType = DistributedCacheType.SqlServer;

        memoryType.Should().Be(DistributedCacheType.Memory);
        redisType.Should().Be(DistributedCacheType.Redis);
        sqlServerType.Should().Be(DistributedCacheType.SqlServer);
    }

    [Test]
    public void QueueProcessingOrder_Enum_ShouldHaveCorrectValues()
    {
        // Act & Assert
        var oldestFirst = QueueProcessingOrder.OldestFirst;
        var newestFirst = QueueProcessingOrder.NewestFirst;

        oldestFirst.Should().Be(QueueProcessingOrder.OldestFirst);
        newestFirst.Should().Be(QueueProcessingOrder.NewestFirst);
    }

    [Test]
    public void ConfigurationValidationResult_ShouldWorkCorrectly()
    {
        // Arrange
        var validResult = new ConfigurationValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        var invalidResult = new ConfigurationValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Error 1", "Error 2" }
        };

        // Assert
        validResult.IsValid.Should().BeTrue();
        validResult.Errors.Should().BeEmpty();

        invalidResult.IsValid.Should().BeFalse();
        invalidResult.Errors.Should().HaveCount(2);
        invalidResult.Errors.Should().Contain("Error 1");
        invalidResult.Errors.Should().Contain("Error 2");
    }

    [Test]
    public void LybtOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new LybtOptions();

        // Assert - 验证默认值
        options.Authentication.Should().NotBeNull();
        options.Security.Should().NotBeNull();
        options.Infrastructure.Should().NotBeNull();
        options.Business.Should().NotBeNull();
        options.Application.Should().NotBeNull();

        // 验证一些关键默认值
        options.Authentication.Jwt.AccessTokenExpirationMinutes.Should().Be(480);
        options.Authentication.PasswordPolicy.MinLength.Should().Be(8);
        options.Security.RateLimiting.Enabled.Should().BeTrue();
        options.Infrastructure.Database.ConnectionPool.MaxConnections.Should().Be(100);
        options.Business.UserManagement.DefaultRole.Should().Be("Staff");
        options.Application.WebApi.Performance.MinWorkerThreads.Should().Be(50);
    }

    [Test]
    public void RedisDistributedCache_ValidationTest()
    {
        // Arrange
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:SecretKey"] = "LybtJwtSecretKey2025!@#$%^&*()ABCD",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client",
            ["Lybt:Infrastructure:Database:ConnectionString"] = "test",
            ["Lybt:Infrastructure:Cache:DistributedCache:Type"] = "Redis",
            // 缺少 RedisConnectionString
            ["Lybt:Business:SystemAdmin:Username"] = "admin",
            ["Lybt:Business:SystemAdmin:Email"] = "admin@test.com",
            ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "password"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("Redis connection string is required when using Redis distributed cache");
    }

    [Test]
    public void SqlServerDistributedCache_ValidationTest()
    {
        // Arrange
        var configDict = new Dictionary<string, string>
        {
            ["Lybt:Authentication:Jwt:SecretKey"] = "LybtJwtSecretKey2025!@#$%^&*()ABCD",
            ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI",
            ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client",
            ["Lybt:Infrastructure:Database:ConnectionString"] = "test",
            ["Lybt:Infrastructure:Cache:DistributedCache:Type"] = "SqlServer",
            // 缺少 SqlServerConnectionString
            ["Lybt:Business:SystemAdmin:Username"] = "admin",
            ["Lybt:Business:SystemAdmin:Email"] = "admin@test.com",
            ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "password"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var validationResult = configuration.ValidateLybtConfiguration();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain("SQL Server connection string is required when using SQL Server distributed cache");
    }
}