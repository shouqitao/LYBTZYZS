using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// JwtAuthenticationService 完整单元测试
    /// 职责：JWT令牌生成、验证、刷新、用户信息提取
    /// </summary>
    public class JwtAuthenticationServiceTests
    {
        private readonly JwtAuthenticationService _jwtAuthenticationService;
        private readonly Mock<IOptions<JwtOptions>> _mockJwtOptions;
        private readonly Mock<ILogger<JwtAuthenticationService>> _mockLogger;
        private readonly JwtOptions _jwtOptions;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public JwtAuthenticationServiceTests()
        {
            _mockJwtOptions = new Mock<IOptions<JwtOptions>>();
            _mockLogger = new Mock<ILogger<JwtAuthenticationService>>();
            _tokenHandler = new JwtSecurityTokenHandler();

            _jwtOptions = new JwtOptions
            {
                Secret = "LYBT_JWT_SECRET_KEY_FOR_TESTING_PURPOSES_32_CHARS_MINIMUM",
                Issuer = "LYBT.WebAPI.Test",
                Audience = "LYBT.Client.Test",
                ExpireMinutes = 480,
                RememberMeExpireMinutes = 43200,
                ClockSkewSeconds = 300
            };

            _mockJwtOptions.Setup(x => x.Value).Returns(_jwtOptions);

            _jwtAuthenticationService = new JwtAuthenticationService(_mockJwtOptions.Object, _mockLogger.Object, null);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_JwtOptions_Is_Null()
        {
            // Act & Assert
            var action = () => new JwtAuthenticationService(null!, _mockLogger.Object, null);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("jwtOptions");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            var action = () => new JwtAuthenticationService(_mockJwtOptions.Object, null!, null);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_Should_Create_Instance_When_Dependencies_Are_Valid()
        {
            // Act
            var service = new JwtAuthenticationService(_mockJwtOptions.Object, _mockLogger.Object, null);

            // Assert
            service.Should().NotBeNull();
            service.Should().BeAssignableTo<IJwtAuthenticationService>();
        }

        #endregion

        #region GenerateToken 测试

        [Fact]
        public void GenerateToken_Should_Generate_Valid_Token_For_Doctor()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testdoctor";
            var role = UserRole.Doctor;

            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Assert
            token.Should().NotBeNullOrEmpty();
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == userName);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.Doctor.ToString());
        }

        [Fact]
        public void GenerateToken_Should_Generate_Valid_Token_For_Admin()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000001";
            var userName = "sysadmin";
            var role = UserRole.Admin;

            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Assert
            token.Should().NotBeNullOrEmpty();
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == userName);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.Admin.ToString());
        }

        [Fact]
        public void GenerateToken_Should_Set_Standard_Expiry_When_RememberMe_False()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;

            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Assert
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
            jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_Should_Set_Extended_Expiry_When_RememberMe_True()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;

            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, true);

            // Assert
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtOptions.RememberMeExpireMinutes);
            jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_Should_Include_Required_Claims()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;

            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Assert
            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // 验证必须的声明
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role);

            // 验证JTI不为空
            var jtiClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti);
            Guid.TryParse(jtiClaim.Value, out _).Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void GenerateToken_Should_Handle_Empty_UserId(string? userId)
        {
            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId!, "testuser", UserRole.Doctor, false);

            // Assert
            token.Should().NotBeNullOrEmpty();
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == (userId ?? ""));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void GenerateToken_Should_Handle_Empty_Username(string? username)
        {
            // Act
            var token = _jwtAuthenticationService.GenerateToken("user-123", username!, UserRole.Doctor, false);

            // Assert
            token.Should().NotBeNullOrEmpty();
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == (username ?? ""));
        }

        #endregion

        #region ValidateToken 测试

        [Fact]
        public void ValidateToken_Should_Return_ClaimsPrincipal_When_Token_Is_Valid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Act
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);

            // Assert
            claimsPrincipal.Should().NotBeNull();
            claimsPrincipal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(userId);
            claimsPrincipal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value.Should().Be(userName);
            claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(UserRole.Doctor.ToString());
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Is_Invalid()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(invalidToken);

            // Assert
            claimsPrincipal.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateToken_Should_Return_Null_When_Token_Is_Empty(string? token)
        {
            // Act
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token!);

            // Assert
            claimsPrincipal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Is_Expired()
        {
            // Arrange - 创建一个过期的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser"),
                new(ClaimTypes.Role, UserRole.Doctor.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiredToken = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(-1), // 过期1小时，远超过ClockSkew容差
                signingCredentials: creds);

            var tokenString = _tokenHandler.WriteToken(expiredToken);

            // Act
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(tokenString);

            // Assert
            claimsPrincipal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Has_Wrong_Issuer()
        {
            // Arrange - 创建一个签发者错误的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var wrongIssuerToken = new JwtSecurityToken(
                issuer: "WrongIssuer",
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = _tokenHandler.WriteToken(wrongIssuerToken);

            // Act
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(tokenString);

            // Assert
            claimsPrincipal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Has_Wrong_Secret()
        {
            // Arrange - 创建一个用错误密钥签名的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser")
            };

            var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WRONG_SECRET_KEY_FOR_TESTING_32_CHARS"));
            var wrongCreds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

            var wrongSecretToken = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: wrongCreds);

            var tokenString = _tokenHandler.WriteToken(wrongSecretToken);

            // Act
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(tokenString);

            // Assert
            claimsPrincipal.Should().BeNull();
        }

        #endregion

        #region RefreshToken 测试

        [Fact]
        public void RefreshToken_Should_Return_New_Token_When_Current_Token_Is_Valid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;
            var originalToken = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Act
            var newToken = _jwtAuthenticationService.RefreshToken(originalToken);

            // Assert
            newToken.Should().NotBeNullOrEmpty();
            newToken.Should().NotBe(originalToken);

            var newJwtToken = _tokenHandler.ReadJwtToken(newToken);
            newJwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            newJwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == userName);
            newJwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.Doctor.ToString());
        }

        [Fact]
        public void RefreshToken_Should_Throw_When_Token_Is_Invalid()
        {
            // Arrange
            var invalidToken = "invalid.jwt.token";

            // Act & Assert
            var action = () => _jwtAuthenticationService.RefreshToken(invalidToken);
            action.Should().Throw<SecurityTokenException>()
                .WithMessage("Invalid token");
        }

        [Fact]
        public void RefreshToken_Should_Default_To_Doctor_Role_When_Role_Parse_Fails()
        {
            // Arrange - 创建一个包含无效角色的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser"),
                new(ClaimTypes.Role, "InvalidRole")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenWithInvalidRole = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = _tokenHandler.WriteToken(tokenWithInvalidRole);

            // Act
            var newToken = _jwtAuthenticationService.RefreshToken(tokenString);

            // Assert
            var newJwtToken = _tokenHandler.ReadJwtToken(newToken);
            newJwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.Doctor.ToString());
        }

        [Fact]
        public void RefreshToken_Should_Handle_Missing_Claims()
        {
            // Arrange - 创建一个缺少某些声明的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123")
                // 缺少 UniqueName 和 Role
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var incompleteToken = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = _tokenHandler.WriteToken(incompleteToken);

            // Act
            var newToken = _jwtAuthenticationService.RefreshToken(tokenString);

            // Assert
            var newJwtToken = _tokenHandler.ReadJwtToken(newToken);
            newJwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "user-123");
            newJwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "");
            newJwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.Doctor.ToString());
        }

        #endregion

        #region ExtractUserInfo 测试

        [Fact]
        public void ExtractUserInfo_Should_Return_UserInfo_When_Token_Is_Valid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Admin;
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Act
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(token);

            // Assert
            userInfo.Should().NotBeNull();
            userInfo!.UserId.Should().Be(userId);
            userInfo.Username.Should().Be(userName);
            userInfo.Role.Should().Be(UserRole.Admin);
            userInfo.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void ExtractUserInfo_Should_Return_Null_When_Token_Is_Malformed()
        {
            // Arrange
            var malformedToken = "malformed.token";

            // Act
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(malformedToken);

            // Assert
            userInfo.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ExtractUserInfo_Should_Return_Null_When_Token_Is_Empty(string? token)
        {
            // Act
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(token!);

            // Assert
            userInfo.Should().BeNull();
        }

        [Fact]
        public void ExtractUserInfo_Should_Default_To_Doctor_Role_When_Role_Parse_Fails()
        {
            // Arrange - 创建一个包含无效角色的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123"),
                new(JwtRegisteredClaimNames.UniqueName, "testuser"),
                new(ClaimTypes.Role, "InvalidRole")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenWithInvalidRole = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = _tokenHandler.WriteToken(tokenWithInvalidRole);

            // Act
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(tokenString);

            // Assert
            userInfo.Should().NotBeNull();
            userInfo!.Role.Should().Be(UserRole.Doctor);
        }

        [Fact]
        public void ExtractUserInfo_Should_Handle_Missing_Claims()
        {
            // Arrange - 创建一个缺少某些声明的令牌
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "user-123")
                // 缺少其他声明
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var incompleteToken = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = _tokenHandler.WriteToken(incompleteToken);

            // Act
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(tokenString);

            // Assert
            userInfo.Should().NotBeNull();
            userInfo!.UserId.Should().Be("user-123");
            userInfo.Username.Should().Be("");
            userInfo.Role.Should().Be(UserRole.Doctor);
        }

        [Fact]
        public void ExtractUserInfo_Should_Handle_Exception_Gracefully()
        {
            // Arrange - 一个看起来像JWT但实际上不是的字符串
            var fakeToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.invalid.signature";

            // Act
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(fakeToken);

            // Assert
            userInfo.Should().BeNull();
        }

        #endregion

        #region 边界值和集成测试

        [Fact]
        public void Full_Token_Lifecycle_Should_Work_Correctly()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "integrationtest";
            var role = UserRole.Admin;

            // Act & Assert - 完整的令牌生命周期测试

            // 1. 生成令牌
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, true);
            token.Should().NotBeNullOrEmpty();

            // 2. 验证令牌
            var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
            claimsPrincipal.Should().NotBeNull();

            // 3. 提取用户信息
            var userInfo = _jwtAuthenticationService.ExtractUserInfo(token);
            userInfo.Should().NotBeNull();
            userInfo!.UserId.Should().Be(userId);
            userInfo.Username.Should().Be(userName);
            userInfo.Role.Should().Be(role);

            // 4. 刷新令牌
            var newToken = _jwtAuthenticationService.RefreshToken(token);
            newToken.Should().NotBeNullOrEmpty();
            newToken.Should().NotBe(token);

            // 5. 验证新令牌
            var newClaimsPrincipal = _jwtAuthenticationService.ValidateToken(newToken);
            newClaimsPrincipal.Should().NotBeNull();
            newClaimsPrincipal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(userId);
        }

        [Fact]
        public void JwtAuthenticationService_Should_Implement_IJwtAuthenticationService()
        {
            // Assert
            _jwtAuthenticationService.Should().BeAssignableTo<IJwtAuthenticationService>();
        }

        [Fact]
        public void Multiple_Tokens_Should_Have_Unique_JTI()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;

            // Act
            var token1 = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);
            var token2 = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Assert
            var jwtToken1 = _tokenHandler.ReadJwtToken(token1);
            var jwtToken2 = _tokenHandler.ReadJwtToken(token2);

            var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            jti1.Should().NotBe(jti2);
        }

        [Theory]
        [InlineData(UserRole.Doctor)]
        [InlineData(UserRole.Admin)]
        public void GenerateToken_Should_Work_For_All_User_Roles(UserRole role)
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";

            // Act
            var token = _jwtAuthenticationService.GenerateToken(userId, userName, role, false);

            // Assert
            token.Should().NotBeNullOrEmpty();
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role.ToString());
        }

        #endregion
    }
}