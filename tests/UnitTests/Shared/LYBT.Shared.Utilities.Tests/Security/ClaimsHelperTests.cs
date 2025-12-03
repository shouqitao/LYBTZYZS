using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using LYBT.Shared.Utilities.Security;
using Xunit;

namespace LYBT.Shared.Utilities.Tests.Security
{
    /// <summary>
    /// ClaimsHelper工具类单元测试
    /// </summary>
    public class ClaimsHelperTests
    {
        #region CreateClaims方法测试

        [Fact]
        public void CreateClaims_WithBasicParameters_ShouldCreateCorrectClaims()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "Doctor";

            // Act
            var claims = ClaimsHelper.CreateClaims(userId, username, role);

            // Assert
            claims.Should().NotBeEmpty();
            claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
            claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == username);
            claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Doctor");
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
        }

        [Fact]
        public void CreateClaims_WithAdditionalClaims_ShouldIncludeAllClaims()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "Admin";
            var additionalClaims = new Dictionary<string, string>
            {
                ["custom_claim"] = "custom_value",
                ["department"] = "IT"
            };

            // Act
            var claims = ClaimsHelper.CreateClaims(userId, username, role, additionalClaims);

            // Assert
            claims.Should().Contain(c => c.Type == "custom_claim" && c.Value == "custom_value");
            claims.Should().Contain(c => c.Type == "department" && c.Value == "IT");
        }

        [Fact]
        public void CreateClaims_WithNullAdditionalClaims_ShouldWorkCorrectly()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "Doctor";

            // Act
            var claims = ClaimsHelper.CreateClaims(userId, username, role, null);

            // Assert
            claims.Should().NotBeEmpty();
            claims.Should().NotContain(c => c.Type == "custom_claim");
        }

        [Fact]
        public void CreateClaims_ShouldNormalizeRole()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "医生"; // 中文角色

            // Act
            var claims = ClaimsHelper.CreateClaims(userId, username, role);

            // Assert
            var roleClaim = claims.First(c => c.Type == ClaimTypes.Role);
            roleClaim.Value.Should().Be("Doctor"); // 应该被标准化
        }

        [Fact]
        public void CreateClaims_JtiClaim_ShouldBeUniqueGuid()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "Doctor";

            // Act
            var claims1 = ClaimsHelper.CreateClaims(userId, username, role);
            var claims2 = ClaimsHelper.CreateClaims(userId, username, role);

            var jti1 = claims1.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var jti2 = claims2.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            // Assert
            jti1.Should().NotBe(jti2);
            Guid.TryParse(jti1, out _).Should().BeTrue();
            Guid.TryParse(jti2, out _).Should().BeTrue();
        }

        [Fact]
        public void CreateClaims_IatClaim_ShouldBeValidUnixTimestamp()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "Doctor";

            // Act
            var claims = ClaimsHelper.CreateClaims(userId, username, role);

            // Assert
            var iatClaim = claims.First(c => c.Type == JwtRegisteredClaimNames.Iat);
            iatClaim.ValueType.Should().Be(ClaimValueTypes.Integer64);
            long.TryParse(iatClaim.Value, out var timestamp).Should().BeTrue();
            timestamp.Should().BeGreaterThan(0);
        }

        #endregion

        #region GetUserId方法测试

        [Fact]
        public void GetUserId_WithValidPrincipal_ShouldReturnUserId()
        {
            // Arrange
            var userId = "123";
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetUserId(principal);

            // Assert
            result.Should().Be(userId);
        }

        [Fact]
        public void GetUserId_WithSubClaim_ShouldReturnUserId()
        {
            // Arrange
            var userId = "123";
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetUserId(principal);

            // Assert
            result.Should().Be(userId);
        }

        [Fact]
        public void GetUserId_WithUnauthenticatedPrincipal_ShouldReturnNull()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            };
            var identity = new ClaimsIdentity(claims); // 未认证
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetUserId(principal);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        public void GetUserId_WithNullPrincipal_ShouldReturnNull(ClaimsPrincipal? principal)
        {
            // Act
            var result = ClaimsHelper.GetUserId(principal);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetUsername方法测试

        [Fact]
        public void GetUsername_WithValidPrincipal_ShouldReturnUsername()
        {
            // Arrange
            var username = "testuser";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetUsername(principal);

            // Assert
            result.Should().Be(username);
        }

        [Fact]
        public void GetUsername_WithUniqueNameClaim_ShouldReturnUsername()
        {
            // Arrange
            var username = "testuser";
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, username)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetUsername(principal);

            // Assert
            result.Should().Be(username);
        }

        [Fact]
        public void GetUsername_WithUnauthenticatedPrincipal_ShouldReturnNull()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims); // 未认证
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetUsername(principal);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetRole方法测试

        // 注意：RoleHelper.NormalizeRole是大小写敏感的
        // "Admin" → "Admin"，"admin" → "Doctor"（默认值）
        [Fact]
        public void GetRole_WithValidPrincipal_ShouldReturnNormalizedRole()
        {
            // Arrange
            var role = "Admin"; // 使用精确匹配的角色
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetRole(principal);

            // Assert
            result.Should().Be("Admin"); // 精确匹配时被标准化为Admin
        }

        [Fact]
        public void GetRole_WithUnauthenticatedPrincipal_ShouldReturnNull()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims); // 未认证
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetRole(principal);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region HasRole方法测试

        // 注意：RoleHelper.NormalizeRole是大小写敏感的
        // HasRole会对两个角色都进行标准化后再比较
        // "admin" 标准化为 "Doctor"，所以 HasRole("Admin", "admin") = HasRole("Admin", "Doctor") = false
        [Theory]
        [InlineData("Admin", "Admin", true)]
        [InlineData("Admin", "admin", false)]  // admin标准化为Doctor，所以不匹配Admin
        [InlineData("Doctor", "Doctor", true)]
        [InlineData("Doctor", "doctor", true)] // doctor标准化为Doctor，匹配
        [InlineData("Admin", "Doctor", false)]
        [InlineData("Doctor", "Admin", false)]
        public void HasRole_WithDifferentRoles_ShouldReturnCorrectResult(string userRole, string checkRole, bool expected)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, userRole)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.HasRole(principal, checkRole);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void HasRole_WithUnauthenticatedPrincipal_ShouldReturnFalse()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims); // 未认证
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.HasRole(principal, "Admin");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region HasAnyRole方法测试

        [Fact]
        public void HasAnyRole_WithMatchingRole_ShouldReturnTrue()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Doctor")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.HasAnyRole(principal, "Admin", "Doctor", "User");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasAnyRole_WithoutMatchingRole_ShouldReturnFalse()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Doctor")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            // 注意：只有"Admin"和"管理员"标准化为Admin，其他所有角色都标准化为Doctor
            // 所以这里只检查Admin角色，确保Doctor用户没有Admin权限
            var result = ClaimsHelper.HasAnyRole(principal, "Admin", "管理员");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasAnyRole_WithEmptyRoles_ShouldReturnFalse()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Doctor")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.HasAnyRole(principal);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsAdmin方法测试

        // 注意：RoleHelper.NormalizeRole是大小写敏感的
        // "admin" 标准化为 "Doctor"，所以 IsAdmin("admin") = false
        [Theory]
        [InlineData("Admin", true)]
        [InlineData("admin", false)]  // admin标准化为Doctor，不是Admin
        [InlineData("Doctor", false)]
        [InlineData("doctor", false)] // doctor标准化为Doctor，不是Admin
        public void IsAdmin_WithDifferentRoles_ShouldReturnCorrectResult(string role, bool expected)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.IsAdmin(principal);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region IsDoctor方法测试

        // 注意：RoleHelper.NormalizeRole是大小写敏感的
        // "admin" 标准化为 "Doctor"，所以 IsDoctor("admin") = true
        [Theory]
        [InlineData("Doctor", true)]
        [InlineData("doctor", true)]  // doctor标准化为Doctor
        [InlineData("Admin", false)]
        [InlineData("admin", true)]   // admin标准化为Doctor，所以IsDoctor为true
        public void IsDoctor_WithDifferentRoles_ShouldReturnCorrectResult(string role, bool expected)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.IsDoctor(principal);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region GetClaimValue方法测试

        [Fact]
        public void GetClaimValue_WithExistingClaim_ShouldReturnValue()
        {
            // Arrange
            var claimType = "custom_claim";
            var claimValue = "custom_value";
            var claims = new[]
            {
                new Claim(claimType, claimValue)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetClaimValue(principal, claimType);

            // Assert
            result.Should().Be(claimValue);
        }

        [Fact]
        public void GetClaimValue_WithNonExistingClaim_ShouldReturnNull()
        {
            // Arrange
            var claims = new[]
            {
                new Claim("other_claim", "value")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetClaimValue(principal, "non_existing_claim");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetClaimValue_WithUnauthenticatedPrincipal_ShouldReturnNull()
        {
            // Arrange
            var claims = new[]
            {
                new Claim("claim", "value")
            };
            var identity = new ClaimsIdentity(claims); // 未认证
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetClaimValue(principal, "claim");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetClaimsAsDictionary方法测试

        [Fact]
        public void GetClaimsAsDictionary_WithValidPrincipal_ShouldReturnAllClaims()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("custom_claim", "custom_value")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetClaimsAsDictionary(principal);

            // Assert
            result.Should().HaveCount(4);
            result.Should().ContainKey(ClaimTypes.NameIdentifier).WhoseValue.Should().Be("123");
            result.Should().ContainKey(ClaimTypes.Name).WhoseValue.Should().Be("testuser");
            result.Should().ContainKey(ClaimTypes.Role).WhoseValue.Should().Be("Admin");
            result.Should().ContainKey("custom_claim").WhoseValue.Should().Be("custom_value");
        }

        [Fact]
        public void GetClaimsAsDictionary_WithDuplicateClaims_ShouldKeepFirstOne()
        {
            // Arrange
            var claims = new[]
            {
                new Claim("duplicate_claim", "first_value"),
                new Claim("duplicate_claim", "second_value"),
                new Claim("other_claim", "other_value")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetClaimsAsDictionary(principal);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("duplicate_claim").WhoseValue.Should().Be("first_value");
            result.Should().ContainKey("other_claim").WhoseValue.Should().Be("other_value");
        }

        [Fact]
        public void GetClaimsAsDictionary_WithUnauthenticatedPrincipal_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var claims = new[]
            {
                new Claim("claim", "value")
            };
            var identity = new ClaimsIdentity(claims); // 未认证
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = ClaimsHelper.GetClaimsAsDictionary(principal);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetClaimsAsDictionary_WithNullPrincipal_ShouldReturnEmptyDictionary()
        {
            // Act
            var result = ClaimsHelper.GetClaimsAsDictionary(null);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region 综合集成测试

        [Fact]
        public void Integration_CreateAndExtractClaims_ShouldWorkCorrectly()
        {
            // Arrange
            var userId = "123";
            var username = "testuser";
            var role = "Admin";
            var additionalClaims = new Dictionary<string, string>
            {
                ["department"] = "IT"
            };

            // Act - 创建Claims
            var claims = ClaimsHelper.CreateClaims(userId, username, role, additionalClaims);
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            // Act - 提取信息
            var extractedUserId = ClaimsHelper.GetUserId(principal);
            var extractedUsername = ClaimsHelper.GetUsername(principal);
            var extractedRole = ClaimsHelper.GetRole(principal);
            var isAdmin = ClaimsHelper.IsAdmin(principal);
            var isDoctor = ClaimsHelper.IsDoctor(principal);
            var customClaim = ClaimsHelper.GetClaimValue(principal, "department");

            // Assert
            extractedUserId.Should().Be(userId);
            extractedUsername.Should().Be(username);
            extractedRole.Should().Be("Admin");
            isAdmin.Should().BeTrue();
            isDoctor.Should().BeFalse();
            customClaim.Should().Be("IT");
        }

        #endregion
    }
}
