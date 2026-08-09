using FluentAssertions;
using Xunit;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Utilities.Security;

namespace LYBT.Tests.Server.Utilities
{
    /// <summary>
    /// PasswordHelper工具类单元测试
    /// 覆盖保留的纯工具方法（GenerateSecurePassword）
    /// 注：密码哈希/验证已统一走 Identity UserManager（A-27 删除 BCrypt 相关测试）；A-31-C7 删除 CheckPasswordStrength/IsCommonPassword/PasswordValidationResult（生产 0 消费，强度校验走 PasswordPolicyValidator）
    /// </summary>
    public class PasswordHelperTests
    {
        #region Basic API Tests

        [Fact]
        public void GenerateSecurePassword_WithDefaultParameters_ShouldGenerateValidPassword()
        {
            // Act
            var password = PasswordHelper.GenerateSecurePassword();

            // Assert
            password.Should().NotBeNullOrEmpty();
            password.Length.Should().Be(12);
            password.Should().MatchRegex(@"[a-z]"); // 包含小写字母
            password.Should().MatchRegex(@"[A-Z]"); // 包含大写字母
            password.Should().MatchRegex(@"\d"); // 包含数字
            password.Should().MatchRegex(@"[!@#$%^&*]"); // 包含特殊字符
        }

        [Theory]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(24)]
        public void GenerateSecurePassword_WithDifferentLengths_ShouldGenerateCorrectLength(int length)
        {
            // Act
            var password = PasswordHelper.GenerateSecurePassword(length);

            // Assert
            password.Length.Should().Be(length);
        }

        [Fact]
        public void GenerateSecurePassword_WithOnlyLowercase_ShouldOnlyContainLowercase()
        {
            // Act
            var password = PasswordHelper.GenerateSecurePassword(
                length: 12,
                includeUppercase: false,
                includeLowercase: true,
                includeDigits: false,
                includeSpecialChars: false);

            // Assert
            password.Should().MatchRegex(@"^[a-z]+$");
        }

        [Fact]
        public void GenerateSecurePassword_WithTooShortLength_ShouldThrowException()
        {
            // Act & Assert
            var act = () => PasswordHelper.GenerateSecurePassword(3);
            act.Should().Throw<ArgumentException>().WithMessage("密码长度至少为4位");
        }

        [Fact]
        public void GenerateSecurePassword_WithNoCharacterTypes_ShouldThrowException()
        {
            // Act & Assert
            var act = () => PasswordHelper.GenerateSecurePassword(
                includeUppercase: false,
                includeLowercase: false,
                includeDigits: false,
                includeSpecialChars: false);
            act.Should().Throw<ArgumentException>().WithMessage("至少要包含一种字符类型");
        }

        [Fact]
        public void GenerateSecurePassword_MultipleCalls_ShouldGenerateDifferentPasswords()
        {
            // Act
            var password1 = PasswordHelper.GenerateSecurePassword();
            var password2 = PasswordHelper.GenerateSecurePassword();

            // Assert
            password1.Should().NotBe(password2);
        }

        [Fact]
        public void GenerateSecurePassword_WithSpecificTypes_ShouldContainRequiredTypes()
        {
            // Act
            var password = PasswordHelper.GenerateSecurePassword(
                length: 16,
                includeUppercase: true,
                includeLowercase: true,
                includeDigits: true,
                includeSpecialChars: false);

            // Assert
            password.Should().MatchRegex(@"[A-Z]"); // 包含大写字母
            password.Should().MatchRegex(@"[a-z]"); // 包含小写字母
            password.Should().MatchRegex(@"\d"); // 包含数字
            password.Should().NotMatchRegex(@"[!@#$%^&*]"); // 不包含特殊字符
        }

        [Fact]
        public void PasswordStrength_Values_ShouldHaveCorrectOrder()
        {
            // Assert
            ((int)PasswordStrength.Weak).Should().Be(1);
            ((int)PasswordStrength.Fair).Should().Be(2);
            ((int)PasswordStrength.Good).Should().Be(3);
            ((int)PasswordStrength.Strong).Should().Be(4);
            ((int)PasswordStrength.VeryStrong).Should().Be(5);
        }

        #endregion
    }
}
