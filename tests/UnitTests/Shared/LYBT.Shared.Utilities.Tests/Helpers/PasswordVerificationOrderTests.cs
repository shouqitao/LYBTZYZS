using FluentAssertions;
using LYBT.Shared.Utilities.Helpers;
using Xunit;

namespace LYBT.Shared.Utilities.Tests.Helpers;

/// <summary>
/// 密码验证参数顺序测试
/// 确保所有代码使用正确的参数顺序：Verify(hash, password)
/// </summary>
public class PasswordVerificationOrderTests
{
    [Fact]
    public void Verify_Should_Accept_Hash_First_Password_Second()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = PasswordHelper.Hash(password);

        // Act - 正确的参数顺序
        var result = PasswordHelper.Verify(hash, password);

        // Assert
        result.Should().BeTrue("当使用正确的参数顺序(hash, password)时应该返回true");
    }

    [Fact]
    public void Verify_Should_Fail_With_Wrong_Order()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = PasswordHelper.Hash(password);

        // Act - 错误的参数顺序（如果参数顺序错误会导致验证失败）
        // 这里故意使用错误的顺序来验证修复
        var wrongOrderResult = false;
        try
        {
            // 如果参数顺序颠倒，会因为Base64解码失败而返回false
            wrongOrderResult = PasswordHelper.Verify(password, hash);
        }
        catch
        {
            // 可能抛出异常
        }

        // Assert
        wrongOrderResult.Should().BeFalse("当参数顺序错误时应该返回false或抛出异常");
    }

    [Fact]
    public void Verify_Should_Work_With_Different_Passwords()
    {
        // Arrange
        var passwords = new[]
        {
            "SimplePass123",
            "Complex!@#$%^&*()_+Password",
            "中文密码123",
            "VeryLongPasswordWithManyCharacters1234567890!@#$%^&*()"
        };

        foreach (var password in passwords)
        {
            // Act
            var hash = PasswordHelper.Hash(password);
            var isValid = PasswordHelper.Verify(hash, password);
            var isInvalidWithWrongPassword = PasswordHelper.Verify(hash, password + "wrong");

            // Assert
            isValid.Should().BeTrue($"密码'{password}'应该验证成功");
            isInvalidWithWrongPassword.Should().BeFalse($"错误的密码应该验证失败");
        }
    }

    [Fact]
    public void Verify_Should_Validate_Hash_Format()
    {
        // Arrange
        var password = "TestPassword";
        var validHash = PasswordHelper.Hash(password);
        var invalidHashes = new[]
        {
            "not_a_base64_string",
            "SGVsbG8=",  // 太短的Base64
        };

        // Act & Assert - 有效哈希应该工作
        PasswordHelper.Verify(validHash, password).Should().BeTrue();

        // 无效哈希应该返回false
        foreach (var invalidHash in invalidHashes)
        {
            var result = PasswordHelper.Verify(invalidHash, "anypassword");
            result.Should().BeFalse($"无效的哈希格式'{invalidHash}'应该返回false");
        }

        // 空字符串应该抛出异常
        Action act = () => PasswordHelper.Verify("", "anypassword");
        act.Should().Throw<ArgumentException>("空哈希应该抛出异常");
    }

    [Theory]
    [InlineData("password123", "Password123")]  // 大小写不同
    [InlineData("password", "password ")]       // 有空格
    [InlineData("pass", "passs")]              // 长度不同
    public void Verify_Should_Be_Case_And_Space_Sensitive(string originalPassword, string testPassword)
    {
        // Arrange
        var hash = PasswordHelper.Hash(originalPassword);

        // Act
        var result = PasswordHelper.Verify(hash, testPassword);

        // Assert
        result.Should().BeFalse("密码验证应该对大小写和空格敏感");
    }

    [Fact]
    public void Multiple_Hashes_Of_Same_Password_Should_Be_Different()
    {
        // Arrange
        var password = "SamePassword123";

        // Act
        var hash1 = PasswordHelper.Hash(password);
        var hash2 = PasswordHelper.Hash(password);

        // Assert
        hash1.Should().NotBe(hash2, "相同密码的多次哈希应该产生不同结果（因为使用了随机盐）");

        // 但都应该能验证成功
        PasswordHelper.Verify(hash1, password).Should().BeTrue();
        PasswordHelper.Verify(hash2, password).Should().BeTrue();
    }
}