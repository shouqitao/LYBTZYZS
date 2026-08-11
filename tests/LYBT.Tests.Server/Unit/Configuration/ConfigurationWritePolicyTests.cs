using FluentAssertions;
using LYBT.Infrastructure.Configuration.Security;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 配置写白名单/脱敏策略单测（AC-TEST P3-7: SHELL-018 配置 API 纯逻辑——
/// IsAllowed 白名单 + IsSensitive 脱敏判定——Phase 1 新增方法无独立测试）
/// </summary>
public class ConfigurationWritePolicyTests
{
    // ── IsAllowed（白名单） ──

    [Theory]
    [InlineData("Session:TimeoutMinutes")]
    [InlineData("Security:AccountLockout:MaxFailedCount")]
    [InlineData("App:Environment")]
    [InlineData("SystemAdmin:SessionTimeoutMinutes")]
    public void IsAllowed_BusinessKeysInWhitelist_ReturnsTrue(string key)
        => ConfigurationWritePolicy.IsAllowed(key).Should().BeTrue();

    [Theory]
    [InlineData("Jwt:SecretKey")]                          // 精确禁止键
    [InlineData("ConnectionStrings:DefaultConnection")]    // 精确禁止键
    [InlineData("Database:ConnectionString")]              // 精确禁止键
    [InlineData("DefaultPasswords:NewUserPassword")]       // 整节禁止
    [InlineData("UnknownSection:AnyKey")]                  // 未知节
    [InlineData("")]
    public void IsAllowed_SensitiveOrUnknownKeys_ReturnsFalse(string key)
        => ConfigurationWritePolicy.IsAllowed(key).Should().BeFalse();

    [Fact]
    public void IsAllowed_NullKey_ReturnsFalse()
        => ConfigurationWritePolicy.IsAllowed(null!).Should().BeFalse();

    // ── IsSensitive（脱敏掩码——SHELL-018 Phase 1） ──

    [Theory]
    [InlineData("Jwt:SecretKey")]
    [InlineData("Jwt:SigningKey")]
    [InlineData("ConnectionStrings:DefaultConnection")]
    [InlineData("DefaultPasswords:SysAdminPassword")]
    [InlineData("Database:ConnectionString")]
    [InlineData("Auth:ClientSecret")]
    [InlineData("Api:AccessToken")]
    public void IsSensitive_SecretLikeKeys_ReturnsTrue(string key)
        => ConfigurationWritePolicy.IsSensitive(key).Should().BeTrue();

    [Theory]
    [InlineData("Session:TimeoutMinutes")]
    [InlineData("ClinicSettings:Name")]
    [InlineData("FeatureToggles:OverwriteConflicts")]
    [InlineData("App:Environment")]
    public void IsSensitive_BusinessKeys_ReturnsFalse(string key)
        => ConfigurationWritePolicy.IsSensitive(key).Should().BeFalse();
}
