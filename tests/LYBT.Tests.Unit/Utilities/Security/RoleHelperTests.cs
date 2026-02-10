using LYBT.Shared.Utilities.Security;

namespace LYBT.Tests.Unit.Utilities.Security
{
    /// <summary>
    /// RoleHelper工具类单元测试
    /// </summary>
    public class RoleHelperTests
    {
        #region Roles常量测试

        [Fact]
        public void Roles_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            RoleHelper.Roles.Admin.Should().Be("Admin");
            RoleHelper.Roles.Doctor.Should().Be("Doctor");
            RoleHelper.Roles.All.Should().Contain("Admin", "Doctor");
            RoleHelper.Roles.All.Length.Should().Be(2);
        }

        #endregion

        #region Policies常量测试

        [Fact]
        public void Policies_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            RoleHelper.Policies.AdminOnly.Should().Be("AdminPolicy");
            RoleHelper.Policies.DoctorOnly.Should().Be("DoctorPolicy");
            RoleHelper.Policies.DoctorOrAdmin.Should().Be("DoctorOrAdminPolicy");
        }

        #endregion

        #region NormalizeRole方法测试

        // 注意：实现使用大小写敏感的匹配，只有精确匹配"Admin"或"管理员"才返回Admin
        [Theory]
        [InlineData("Admin", "Admin")]
        [InlineData("admin", "Doctor")]  // 大小写敏感，不匹配Admin
        [InlineData("管理员", "Admin")]
        [InlineData("Doctor", "Doctor")]
        [InlineData("doctor", "Doctor")] // 大小写敏感，不匹配Doctor但默认也是Doctor
        [InlineData("医生", "Doctor")]
        [InlineData("用户", "Doctor")]
        [InlineData("普通用户", "Doctor")]
        [InlineData("User", "Doctor")]
        [InlineData("  Admin  ", "Admin")]
        [InlineData("unknown", "Doctor")]
        [InlineData("", "Doctor")]
        [InlineData(null, "Doctor")]
        [InlineData("   ", "Doctor")]
        public void NormalizeRole_WithDifferentInputs_ShouldReturnCorrectRole(string? input, string expected)
        {
            // Act
            var result = RoleHelper.NormalizeRole(input);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region GetDisplayName方法测试

        // 注意：实现使用大小写敏感的匹配
        [Theory]
        [InlineData("Admin", "管理员")]
        [InlineData("admin", "医生")]  // 大小写敏感，不匹配Admin，默认Doctor显示"医生"
        [InlineData("管理员", "管理员")]
        [InlineData("Doctor", "医生")]
        [InlineData("doctor", "医生")] // 大小写敏感，默认Doctor显示"医生"
        [InlineData("医生", "医生")]
        [InlineData("User", "医生")]
        [InlineData("unknown", "医生")] // 默认Doctor显示"医生"
        [InlineData("", "医生")]
        [InlineData(null, "医生")]
        [InlineData("   ", "医生")]
        public void GetDisplayName_WithDifferentInputs_ShouldReturnCorrectDisplayName(string? input, string expected)
        {
            // Act
            var result = RoleHelper.GetDisplayName(input);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region IsValidRole方法测试

        // 注意：实现使用大小写敏感的匹配，且默认映射到Doctor（所以大多数输入都是valid）
        [Theory]
        [InlineData("Admin", true)]
        [InlineData("admin", true)]  // 默认映射到Doctor，仍然valid
        [InlineData("ADMIN", true)]  // 默认映射到Doctor，仍然valid
        [InlineData("Doctor", true)]
        [InlineData("doctor", true)] // 默认映射到Doctor，仍然valid
        [InlineData("DOCTOR", true)] // 默认映射到Doctor，仍然valid
        [InlineData("管理员", true)] // 会被标准化为Admin
        [InlineData("医生", true)] // 会被标准化为Doctor
        [InlineData("User", true)] // 会被标准化为Doctor
        [InlineData("invalid", true)] // 默认映射到Doctor，仍然valid
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("   ", false)]
        public void IsValidRole_WithDifferentInputs_ShouldReturnCorrectResult(string? input, bool expected)
        {
            // Act
            var result = RoleHelper.IsValidRole(input);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region IsAdmin方法测试

        // 注意：实现使用大小写敏感的匹配
        [Theory]
        [InlineData("Admin", true)]
        [InlineData("admin", false)]  // 大小写敏感，不匹配Admin
        [InlineData("ADMIN", false)]  // 大小写敏感，不匹配Admin
        [InlineData("管理员", true)]
        [InlineData("Doctor", false)]
        [InlineData("医生", false)]
        [InlineData("User", false)]
        [InlineData("invalid", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAdmin_WithDifferentInputs_ShouldReturnCorrectResult(string? input, bool expected)
        {
            // Act
            var result = RoleHelper.IsAdmin(input);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region IsDoctor方法测试

        [Theory]
        [InlineData("Doctor", true)]
        [InlineData("doctor", true)]
        [InlineData("DOCTOR", true)]
        [InlineData("医生", true)]
        [InlineData("用户", true)]
        [InlineData("User", true)]
        [InlineData("Admin", false)]
        [InlineData("管理员", false)]
        [InlineData("invalid", true)] // 默认映射到Doctor
        [InlineData("", true)] // 默认映射到Doctor
        [InlineData(null, true)] // 默认映射到Doctor
        public void IsDoctor_WithDifferentInputs_ShouldReturnCorrectResult(string? input, bool expected)
        {
            // Act
            var result = RoleHelper.IsDoctor(input);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region GetPolicyRoles方法测试

        [Fact]
        public void GetPolicyRoles_WithAdminOnlyPolicy_ShouldReturnAdminRole()
        {
            // Act
            var result = RoleHelper.GetPolicyRoles(RoleHelper.Policies.AdminOnly);

            // Assert
            result.Should().ContainSingle().Which.Should().Be(RoleHelper.Roles.Admin);
        }

        [Fact]
        public void GetPolicyRoles_WithDoctorOnlyPolicy_ShouldReturnDoctorRole()
        {
            // Act
            var result = RoleHelper.GetPolicyRoles(RoleHelper.Policies.DoctorOnly);

            // Assert
            result.Should().ContainSingle().Which.Should().Be(RoleHelper.Roles.Doctor);
        }

        [Fact]
        public void GetPolicyRoles_WithDoctorOrAdminPolicy_ShouldReturnBothRoles()
        {
            // Act
            var result = RoleHelper.GetPolicyRoles(RoleHelper.Policies.DoctorOrAdmin);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(RoleHelper.Roles.Doctor);
            result.Should().Contain(RoleHelper.Roles.Admin);
        }

        [Fact]
        public void GetPolicyRoles_WithInvalidPolicy_ShouldReturnEmptyArray()
        {
            // Act
            var result = RoleHelper.GetPolicyRoles("InvalidPolicy");

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void GetPolicyRoles_WithNullOrEmptyPolicy_ShouldReturnEmptyArray(string? policyName)
        {
            // Act
            var result = RoleHelper.GetPolicyRoles(policyName!);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region 角色映射逻辑测试

        [Fact]
        public void RoleMapping_ChineseToEnglish_ShouldWorkCorrectly()
        {
            // Arrange & Act & Assert
            RoleHelper.NormalizeRole("管理员").Should().Be("Admin");
            RoleHelper.NormalizeRole("医生").Should().Be("Doctor");
            RoleHelper.NormalizeRole("用户").Should().Be("Doctor");
            RoleHelper.NormalizeRole("普通用户").Should().Be("Doctor");
        }

        [Fact]
        public void RoleMapping_EnglishVariations_ShouldWorkCorrectly()
        {
            // Arrange & Act & Assert
            RoleHelper.NormalizeRole("Admin").Should().Be("Admin");
            RoleHelper.NormalizeRole("Doctor").Should().Be("Doctor");
            RoleHelper.NormalizeRole("User").Should().Be("Doctor");
        }

        [Fact]
        public void RoleMapping_CaseSensitive_ShouldWorkCorrectly()
        {
            // 注意：实现使用大小写敏感的匹配
            // Arrange & Act & Assert
            RoleHelper.IsAdmin("Admin").Should().BeTrue();   // 精确匹配
            RoleHelper.IsAdmin("admin").Should().BeFalse();  // 大小写敏感，不匹配
            RoleHelper.IsAdmin("ADMIN").Should().BeFalse();  // 大小写敏感，不匹配
            RoleHelper.IsDoctor("Doctor").Should().BeTrue(); // 精确匹配
            RoleHelper.IsDoctor("doctor").Should().BeTrue(); // 默认映射到Doctor
            RoleHelper.IsDoctor("DOCTOR").Should().BeTrue(); // 默认映射到Doctor
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void AllMethods_WithWhitespaceOnlyInput_ShouldHandleGracefully()
        {
            // Arrange
            var whitespaceRole = "   ";

            // Act & Assert
            RoleHelper.NormalizeRole(whitespaceRole).Should().Be("Doctor");
            RoleHelper.GetDisplayName(whitespaceRole).Should().Be("医生");
            RoleHelper.IsValidRole(whitespaceRole).Should().BeFalse();
            RoleHelper.IsAdmin(whitespaceRole).Should().BeFalse();
            RoleHelper.IsDoctor(whitespaceRole).Should().BeTrue(); // 默认映射
        }

        [Fact]
        public void AllMethods_WithSpecialCharacters_ShouldDefaultToDoctor()
        {
            // Arrange
            var specialRole = "!@#$%";

            // Act & Assert
            // 特殊字符会被NormalizeRole转为默认值"Doctor"
            RoleHelper.NormalizeRole(specialRole).Should().Be("Doctor");
            // IsValidRole检查标准化后的角色是否在有效角色列表中
            // 由于"Doctor"是有效角色，所以返回true
            RoleHelper.IsValidRole(specialRole).Should().BeTrue();
            RoleHelper.IsAdmin(specialRole).Should().BeFalse();
            RoleHelper.IsDoctor(specialRole).Should().BeTrue();
        }

        #endregion
    }
}
