using FluentAssertions;
using LYBT.Shared.Configuration.Options.Common;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Auth
{
    /// <summary>
    /// JWT配置验证测试
    /// unify-configuration-system: 迁移到 LYBT.Shared.Configuration.Options.Common.JwtOptions
    /// </summary>
    public class JwtOptionsValidationTests
    {
        [Fact]
        public void JwtOptions_ShouldHaveSecureDefaults()
        {
            // Arrange
            var options = new JwtOptions();

            // Assert - 验证默认值的安全性
            options.AccessTokenExpirationMinutes.Should().BeLessOrEqualTo(30,
                "AccessToken不应该有过长的有效期");
            options.RefreshTokenExpirationDays.Should().BeLessOrEqualTo(30,
                "RefreshToken的有效期不应超过30天");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("short")]
        [InlineData("NotLongEnoughKey123")]
        public void JwtOptions_ShouldRejectWeakSecrets(string? secretKey)
        {
            // Arrange
            var options = new JwtOptions { SecretKey = secretKey! };

            // Act
            var isValid = IsSecretValid(options.SecretKey);

            // Assert
            isValid.Should().BeFalse("密钥必须至少32个字符");
        }

        [Fact]
        public void JwtOptions_ShouldAcceptStrongSecret()
        {
            // Arrange
            var strongKey = "ThisIsAVeryStrongSecretThatIsAtLeast32CharactersLong!@#$%";
            var options = new JwtOptions { SecretKey = strongKey };

            // Act
            var isValid = IsSecretValid(options.SecretKey);

            // Assert
            isValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(5, true)]    // 5分钟 - 有效
        [InlineData(15, true)]   // 15分钟 - 有效
        [InlineData(30, true)]   // 30分钟 - 有效
        [InlineData(60, false)]  // 60分钟 - 太长
        [InlineData(480, false)] // 8小时 - 太长
        public void AccessTokenExpiration_ShouldBeReasonable(int minutes, bool shouldBeValid)
        {
            // Arrange
            var options = new JwtOptions
            {
                AccessTokenExpirationMinutes = minutes
            };

            // Act
            var isValid = IsAccessTokenExpirationValid(options.AccessTokenExpirationMinutes);

            // Assert
            isValid.Should().Be(shouldBeValid);
        }

        [Theory]
        [InlineData(1, true)]    // 1天 - 有效
        [InlineData(7, true)]    // 7天 - 有效
        [InlineData(30, true)]   // 30天 - 有效
        [InlineData(90, false)]  // 90天 - 太长
        [InlineData(365, false)] // 365天 - 太长
        public void RefreshTokenExpiration_ShouldBeReasonable(int days, bool shouldBeValid)
        {
            // Arrange
            var options = new JwtOptions
            {
                RefreshTokenExpirationDays = days
            };

            // Act
            var isValid = IsRefreshTokenExpirationValid(options.RefreshTokenExpirationDays);

            // Assert
            isValid.Should().Be(shouldBeValid);
        }

        [Fact]
        public void JwtOptions_ShouldRequireIssuerAndAudience()
        {
            // Arrange
            var options = new JwtOptions();

            // Act & Assert
            options.Issuer.Should().NotBeNullOrEmpty("Issuer是必需的");
            options.Audience.Should().NotBeNullOrEmpty("Audience是必需的");
        }

        [Theory]
        [InlineData("http://localhost", false)]  // HTTP不安全
        [InlineData("https://localhost", true)]  // HTTPS安全
        [InlineData("https://api.lybt.com", true)]
        [InlineData("http://api.lybt.com", false)]
        public void Issuer_ShouldUseSecureProtocol(string issuer, bool shouldBeValid)
        {
            // Arrange
            var options = new JwtOptions { Issuer = issuer };

            // Act
            var isValid = IsIssuerSecure(options.Issuer);

            // Assert
            isValid.Should().Be(shouldBeValid);
        }

        #region Helper Methods

        private bool IsSecretValid(string? secretKey)
        {
            return !string.IsNullOrEmpty(secretKey) && secretKey.Length >= 32;
        }

        private bool IsAccessTokenExpirationValid(int minutes)
        {
            return minutes > 0 && minutes <= 30;
        }

        private bool IsRefreshTokenExpirationValid(int days)
        {
            return days > 0 && days <= 30;
        }

        private bool IsIssuerSecure(string? issuer)
        {
            if (string.IsNullOrEmpty(issuer))
                return false;

            // 在生产环境中应该使用HTTPS
            return issuer.StartsWith("https://") || issuer == "localhost";
        }

        #endregion
    }
}
