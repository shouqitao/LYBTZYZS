using FluentAssertions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Xunit;

namespace LYBT.Shared.Utilities.Tests.Security
{
    /// <summary>
    /// PasswordPolicyValidator 单元测试
    /// </summary>
    public class PasswordPolicyValidatorTests
    {
        #region Validate 测试

        // 注意：测试密码不能包含顺序字母(abc/bcd等)或顺序数字(123/456等)
        [Theory]
        [InlineData("Ax7!mqwp", true)]  // 符合所有要求，无顺序模式
        [InlineData("Bz9@nkrthjwx", true)]  // 长密码，无顺序模式
        public void Validate_WithValidPassword_ShouldPass(string password, bool expectedValid)
        {
            // Act
            var result = PasswordPolicyValidator.Validate(password, out var errors);

            // Assert
            result.Should().Be(expectedValid);
            if (expectedValid)
            {
                errors.Should().BeEmpty();
            }
        }

        [Fact]
        public void Validate_WithNullPassword_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate(null!, out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码不能为空");
        }

        [Fact]
        public void Validate_WithEmptyPassword_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate(string.Empty, out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码不能为空");
        }

        [Fact]
        public void Validate_WithShortPassword_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate("Aa1!", out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain($"密码长度不能少于 {PasswordPolicyValidator.Policy.MinLength} 位");
        }

        [Fact]
        public void Validate_WithoutUppercase_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate("aa1!abcd", out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码必须包含至少一个大写字母");
        }

        [Fact]
        public void Validate_WithoutLowercase_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate("AA1!ABCD", out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码必须包含至少一个小写字母");
        }

        [Fact]
        public void Validate_WithoutDigit_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate("Aa!abcde", out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码必须包含至少一个数字");
        }

        [Fact]
        public void Validate_WithoutSpecialChar_ShouldReturnError()
        {
            // Act
            var result = PasswordPolicyValidator.Validate("Aa1abcde", out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain($"密码必须包含至少一个特殊字符 ({PasswordPolicyValidator.Policy.SpecialCharacters})");
        }

        [Theory]
        [InlineData("AAA1!abc")]  // 连续3个A
        [InlineData("Aa111!bc")]  // 连续3个1
        public void Validate_WithRepeatingCharacters_ShouldReturnError(string password)
        {
            // Act
            var result = PasswordPolicyValidator.Validate(password, out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码不能包含连续重复3次或以上的字符");
        }

        [Theory]
        [InlineData("Aa!abc123")]  // 包含123
        [InlineData("Aa!abc456")]  // 包含456
        [InlineData("Aa!abc987")]  // 包含987
        public void Validate_WithSequentialNumbers_ShouldReturnError(string password)
        {
            // Act
            var result = PasswordPolicyValidator.Validate(password, out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码不能包含连续的数字序列（如123、456）");
        }

        [Theory]
        [InlineData("Abc1!test")]  // 包含abc
        [InlineData("Xyz1!test")]  // 包含xyz
        public void Validate_WithSequentialLetters_ShouldReturnError(string password)
        {
            // Act
            var result = PasswordPolicyValidator.Validate(password, out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码不能包含连续的字母序列（如abc、xyz）");
        }

        // 注意：实现检查的是完整密码匹配（不区分大小写），不是子串匹配
        [Theory]
        [InlineData("password")]   // 在常见密码列表中
        [InlineData("Password")]   // 大小写不敏感匹配
        [InlineData("qwerty")]     // 在常见密码列表中
        public void Validate_WithCommonPassword_ShouldReturnError(string password)
        {
            // Act
            var result = PasswordPolicyValidator.Validate(password, out var errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().Contain("密码过于简单，请使用更复杂的密码");
        }

        #endregion

        #region CalculateStrength 测试

        [Fact]
        public void CalculateStrength_WithNullPassword_ShouldReturn0()
        {
            // Act
            var strength = PasswordPolicyValidator.CalculateStrength(null!);

            // Assert
            strength.Should().Be(0);
        }

        [Fact]
        public void CalculateStrength_WithEmptyPassword_ShouldReturn0()
        {
            // Act
            var strength = PasswordPolicyValidator.CalculateStrength(string.Empty);

            // Assert
            strength.Should().Be(0);
        }

        // 注意：测试密码不能包含顺序模式，否则会扣分
        [Theory]
        [InlineData("Ax7!mqwp", 50)]  // 基本符合要求，无扣分项
        [InlineData("Bz9@nkrthjwx", 70)]  // 长密码，无扣分项
        public void CalculateStrength_WithValidPassword_ShouldReturnScore(string password, int minScore)
        {
            // Act
            var strength = PasswordPolicyValidator.CalculateStrength(password);

            // Assert
            strength.Should().BeGreaterOrEqualTo(minScore);
            strength.Should().BeLessOrEqualTo(100);
        }

        [Theory]
        [InlineData("password")]  // 弱密码
        [InlineData("123456")]  // 弱密码
        public void CalculateStrength_WithCommonPassword_ShouldReturnLowScore(string password)
        {
            // Act
            var strength = PasswordPolicyValidator.CalculateStrength(password);

            // Assert
            strength.Should().BeLessThan(40);
        }

        #endregion

        #region GetStrengthLevel 测试

        [Fact]
        public void GetStrengthLevel_WithNullPassword_ShouldReturnWeak()
        {
            // Act
            var level = PasswordPolicyValidator.GetStrengthLevel(null!);

            // Assert
            level.Should().Be(PasswordStrength.Weak);
        }

        [Fact]
        public void GetStrengthLevel_WithWeakPassword_ShouldReturnWeakOrFair()
        {
            // Act
            // "password" 得分约36分（长度32 + 小写10 + 唯一字符14 - 常见密码20）
            // 属于Fair范围（20-39分）
            var level = PasswordPolicyValidator.GetStrengthLevel("password");

            // Assert
            level.Should().BeOneOf(PasswordStrength.Weak, PasswordStrength.Fair);
        }

        [Fact]
        public void GetStrengthLevel_WithStrongPassword_ShouldReturnVeryStrong()
        {
            // 使用不含顺序模式的长密码
            // Act
            var level = PasswordPolicyValidator.GetStrengthLevel("Xw9!qmtpjrnkhzBv");

            // Assert
            level.Should().Be(PasswordStrength.VeryStrong);
        }

        #endregion

        #region GenerateSecurePassword 测试

        [Fact]
        public void GenerateSecurePassword_WithDefaultLength_ShouldGenerateValidPassword()
        {
            // Act
            var password = PasswordPolicyValidator.GenerateSecurePassword();

            // Assert
            password.Should().NotBeNullOrEmpty();
            password.Length.Should().Be(12);
            PasswordPolicyValidator.Validate(password, out var errors).Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(24)]
        public void GenerateSecurePassword_WithCustomLength_ShouldGeneratePasswordOfCorrectLength(int length)
        {
            // Act
            var password = PasswordPolicyValidator.GenerateSecurePassword(length);

            // Assert
            password.Should().NotBeNullOrEmpty();
            password.Length.Should().Be(length);
        }

        [Fact]
        public void GenerateSecurePassword_WithTooShortLength_ShouldUseMinLength()
        {
            // Act
            var password = PasswordPolicyValidator.GenerateSecurePassword(4);

            // Assert
            password.Length.Should().BeGreaterOrEqualTo(PasswordPolicyValidator.Policy.MinLength);
        }

        [Fact]
        public void GenerateSecurePassword_WithTooLongLength_ShouldUseMaxLength()
        {
            // Act
            var password = PasswordPolicyValidator.GenerateSecurePassword(200);

            // Assert
            password.Length.Should().BeLessOrEqualTo(PasswordPolicyValidator.Policy.MaxLength);
        }

        [Fact]
        public void GenerateSecurePassword_MultipleTimes_ShouldGenerateDifferentPasswords()
        {
            // Act
            var password1 = PasswordPolicyValidator.GenerateSecurePassword();
            var password2 = PasswordPolicyValidator.GenerateSecurePassword();
            var password3 = PasswordPolicyValidator.GenerateSecurePassword();

            // Assert
            password1.Should().NotBe(password2);
            password2.Should().NotBe(password3);
            password1.Should().NotBe(password3);
        }

        [Fact]
        public void GenerateSecurePassword_ShouldIncludeAllCharacterTypes()
        {
            // Act
            var password = PasswordPolicyValidator.GenerateSecurePassword(20);

            // Assert
            password.Should().MatchRegex("[A-Z]", "应包含大写字母");
            password.Should().MatchRegex("[a-z]", "应包含小写字母");
            password.Should().MatchRegex(@"\d", "应包含数字");
            password.Should().MatchRegex(@"[!@#$%^&*()\-_+=\[\]{}|;':"",./<>?]", "应包含特殊字符");
        }

        #endregion

        #region Policy 常量测试

        [Fact]
        public void Policy_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            PasswordPolicyValidator.Policy.MinLength.Should().Be(8);
            PasswordPolicyValidator.Policy.MaxLength.Should().Be(128);
            PasswordPolicyValidator.Policy.RequireUppercase.Should().BeTrue();
            PasswordPolicyValidator.Policy.RequireLowercase.Should().BeTrue();
            PasswordPolicyValidator.Policy.RequireDigit.Should().BeTrue();
            PasswordPolicyValidator.Policy.RequireSpecialChar.Should().BeTrue();
            PasswordPolicyValidator.Policy.PasswordHistoryCount.Should().Be(5);
            PasswordPolicyValidator.Policy.PasswordExpirationDays.Should().Be(90);
        }

        #endregion
    }
}
