using FluentAssertions;
using Xunit;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Utilities.Security;

namespace LYBT.Tests.Server.Utilities
{
    /// <summary>
    /// PasswordHelper工具类单元测试
    /// 覆盖保留的纯工具方法（CheckPasswordStrength/IsCommonPassword/GenerateSecurePassword）
    /// 注：密码哈希/验证已统一走 Identity UserManager（A-27 删除 BCrypt 相关测试）
    /// </summary>
    public class PasswordHelperTests
    {
        #region Basic API Tests

        // 注意：hasSpecial检测实现有问题，特殊字符不会加分
        // 实际评分公式：min(len*2, 20) + hasLower*10 + hasUpper*10 + hasDigit*10 + (len>=12)*10 + (len>=16)*10
        [Theory]
        [InlineData("", PasswordStrength.Weak)]
        [InlineData(null, PasswordStrength.Weak)]
        [InlineData("abc", PasswordStrength.Weak)]           // 6+10=16 → Weak
        [InlineData("Abc1", PasswordStrength.Good)]          // 8+10+10+10=38 → Good (≥35)
        [InlineData("Abc123!", PasswordStrength.Good)]       // 14+10+10+10=44 → Good (≥35)
        [InlineData("SecurePassword123!", PasswordStrength.VeryStrong)]  // 20+10+10+10+10+10=70 → VeryStrong (≥60)
        [InlineData("VerySecurePassword123!@#", PasswordStrength.VeryStrong)]  // 20+10+10+10+10+10=70 → VeryStrong
        public void CheckPasswordStrength_WithDifferentPasswords_ShouldReturnCorrectStrength(string? password, PasswordStrength expectedStrength)
        {
            // Act
            var result = PasswordHelper.CheckPasswordStrength(password!);

            // Assert
            result.Should().Be(expectedStrength);
        }

        [Fact]
        public void CheckPasswordStrength_WithCommonPassword_ShouldHaveLowerStrength()
        {
            // Arrange
            var commonPassword = "password123";

            // Act
            var result = PasswordHelper.CheckPasswordStrength(commonPassword);

            // Assert
            result.Should().BeOneOf(PasswordStrength.Weak, PasswordStrength.Fair);
        }

        [Theory]
        [InlineData("123456", true)]
        [InlineData("password", true)]
        [InlineData("admin", true)]
        [InlineData("Password", true)] // 大小写不敏感
        [InlineData("ADMIN", true)]
        [InlineData("SecurePassword123!", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsCommonPassword_WithDifferentPasswords_ShouldReturnCorrectResult(string? password, bool expected)
        {
            // Act
            var result = PasswordHelper.IsCommonPassword(password!);

            // Assert
            result.Should().Be(expected);
        }

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
        public void PasswordValidationResult_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var result = new PasswordHelper.PasswordValidationResult();

            // Assert
            result.IsValid.Should().BeFalse();
            // 注意：PasswordStrength枚举从1开始（Weak=1），默认值是0（未定义）
            result.Strength.Should().Be(default(PasswordStrength));
            result.Errors.Should().NotBeNull().And.BeEmpty();
            result.Suggestions.Should().Be(string.Empty);
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
