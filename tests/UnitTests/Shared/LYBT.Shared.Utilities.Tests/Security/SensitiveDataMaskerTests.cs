using FluentAssertions;
using LYBT.Shared.Utilities.Security;
using Xunit;

namespace LYBT.Shared.Utilities.Tests.Security;

public class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskSensitiveData_Should_Mask_Password_Fields()
    {
        // Arrange
        var data = new
        {
            Username = "admin",
            Password = "MySecretPassword123",
            Email = "admin@test.com"
        };

        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(data);

        // Assert
        result.Should().Contain("\"username\":\"admin\"");
        result.Should().Contain("\"password\":\"[MASKED]\"");
        result.Should().Contain("\"email\":\"admin@test.com\"");
        result.Should().NotContain("MySecretPassword123");
    }

    [Fact]
    public void MaskSensitiveData_Should_Mask_Token_Fields()
    {
        // Arrange
        var data = new
        {
            UserId = "12345",
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = "refresh_token_value_123456",
            Data = "normal data"
        };

        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(data);

        // Assert
        result.Should().Contain("\"userId\":\"12345\"");
        result.Should().Contain("\"accessToken\":\"[MASKED]\"");
        result.Should().Contain("\"refreshToken\":\"[MASKED]\"");
        result.Should().Contain("\"data\":\"normal data\"");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
        result.Should().NotContain("refresh_token_value_123456");
    }

    [Fact]
    public void MaskSensitiveData_Should_Mask_ApiKey_And_Secret_Fields()
    {
        // Arrange
        var data = new
        {
            ServiceName = "MyService",
            ApiKey = "test_api_key_123456789",
            SecretKey = "secret_key_abcdef123456",
            ConnectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=MyPass123"
        };

        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(data);

        // Assert
        result.Should().Contain("\"serviceName\":\"MyService\"");
        result.Should().Contain("\"apiKey\":\"[MASKED]\"");
        result.Should().Contain("\"secretKey\":\"[MASKED]\"");
        result.Should().Contain("\"connectionString\":\"[MASKED]\"");
        result.Should().NotContain("test_api_key_123456789");
        result.Should().NotContain("secret_key_abcdef123456");
        result.Should().NotContain("MyPass123");
    }

    [Fact]
    public void MaskSensitiveString_Should_Mask_JWT_Tokens()
    {
        // Arrange
        var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var result = SensitiveDataMasker.MaskSensitiveString(input);

        // Assert
        result.Should().Be("Authorization: Bearer [MASKED_TOKEN]");
    }

    [Fact]
    public void MaskSensitiveString_Should_Mask_Base64_Keys()
    {
        // Arrange
        var input = "The encryption key is: dGhpcyBpcyBhIHZlcnkgbG9uZyBzZWNyZXQga2V5IGZvciBkZW1vbnN0cmF0aW9u and some text after";

        // Act
        var result = SensitiveDataMasker.MaskSensitiveString(input);

        // Assert
        result.Should().Contain("[MASKED_KEY]");
        result.Should().Contain("and some text after");
        result.Should().NotContain("dGhpcyBpcyBhIHZlcnkgbG9uZyBzZWNyZXQga2V5IGZvciBkZW1vbnN0cmF0aW9u");
    }

    [Fact]
    public void MaskSensitiveData_Should_Handle_Nested_Objects()
    {
        // Arrange
        var data = new
        {
            User = new
            {
                Id = 1,
                Username = "testuser",
                Credentials = new
                {
                    Password = "SecretPass123",
                    Token = "auth_token_xyz"
                }
            },
            Timestamp = "2025-01-01"
        };

        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(data);

        // Assert
        result.Should().Contain("\"username\":\"testuser\"");
        result.Should().Contain("\"password\":\"[MASKED]\"");
        result.Should().Contain("\"token\":\"[MASKED]\"");
        result.Should().Contain("\"timestamp\":\"2025-01-01\"");
        result.Should().NotContain("SecretPass123");
        result.Should().NotContain("auth_token_xyz");
    }

    [Fact]
    public void MaskSensitiveData_Should_Handle_Arrays()
    {
        // Arrange
        var data = new
        {
            Users = new[]
            {
                new { Name = "User1", Password = "Pass1" },
                new { Name = "User2", Password = "Pass2" }
            }
        };

        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(data);

        // Assert
        result.Should().Contain("\"name\":\"User1\"");
        result.Should().Contain("\"name\":\"User2\"");
        result.Should().Contain("\"password\":\"[MASKED]\"");
        result.Should().NotContain("Pass1");
        result.Should().NotContain("Pass2");
    }

    [Fact]
    public void MaskSensitiveData_Should_Handle_Null()
    {
        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(null);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void CreateSafeExceptionMessage_Should_Mask_Sensitive_Info()
    {
        // Arrange
        var innerEx = new Exception("Database connection failed: Password=MySecretPass123");
        var ex = new Exception("Authentication failed with token: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.signature", innerEx);

        // Act
        var result = SensitiveDataMasker.CreateSafeExceptionMessage(ex);

        // Assert
        result.Should().Contain("Bearer [MASKED_TOKEN]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
        result.Should().NotContain("MySecretPass123");
    }

    [Fact]
    public void MaskSensitiveData_Should_Be_Case_Insensitive()
    {
        // Arrange
        var data = new
        {
            PASSWORD = "Upper123",
            PassWord = "Mixed123",
            password = "Lower123",
            ApiKey = "Key123",
            API_KEY = "KEY123"
        };

        // Act
        var result = SensitiveDataMasker.MaskSensitiveData(data);

        // Assert
        result.Should().Contain("\"[MASKED]\"");
        result.Should().NotContain("Upper123");
        result.Should().NotContain("Mixed123");
        result.Should().NotContain("Lower123");
        result.Should().NotContain("Key123");
        result.Should().NotContain("KEY123");
    }
}