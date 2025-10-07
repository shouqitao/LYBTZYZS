using Microsoft.AspNetCore.Authorization;
using LYBT.Infrastructure.Authorization;
using LYBT.Shared.Utilities.Security;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Authorization
{
    public class AuthorizeRolesTests
    {
        [Fact]
        public void Admin_Should_ReturnAuthorizeAttribute_When_Accessed()
        {
            // Act
            var adminAttribute = AuthorizeRoles.Admin;

            // Assert
            adminAttribute.Should().NotBeNull();
            adminAttribute.Should().BeOfType<AuthorizeAttribute>();
            adminAttribute.Policy.Should().Be(RoleHelper.Policies.AdminOnly);
        }

        [Fact]
        public void Doctor_Should_ReturnAuthorizeAttribute_When_Accessed()
        {
            // Act
            var doctorAttribute = AuthorizeRoles.Doctor;

            // Assert
            doctorAttribute.Should().NotBeNull();
            doctorAttribute.Should().BeOfType<AuthorizeAttribute>();
            doctorAttribute.Policy.Should().Be(RoleHelper.Policies.DoctorOnly);
        }

        [Fact]
        public void DoctorOrAdmin_Should_ReturnAuthorizeAttribute_When_Accessed()
        {
            // Act
            var doctorOrAdminAttribute = AuthorizeRoles.DoctorOrAdmin;

            // Assert
            doctorOrAdminAttribute.Should().NotBeNull();
            doctorOrAdminAttribute.Should().BeOfType<AuthorizeAttribute>();
            doctorOrAdminAttribute.Policy.Should().Be(RoleHelper.Policies.DoctorOrAdmin);
        }

        [Theory]
        [InlineData(new string[] { "Admin" }, "Admin")]
        [InlineData(new string[] { "Doctor" }, "Doctor")]
        [InlineData(new string[] { "Admin", "Doctor" }, "Admin,Doctor")]
        public void RequireRoles_Should_ReturnAuthorizeAttributeWithRoles_When_ValidRolesProvided(string[] roles, string expectedRoles)
        {
            // Act
            var result = AuthorizeRoles.RequireRoles(roles);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<AuthorizeAttribute>();
            result.Roles.Should().Be(expectedRoles);
        }

        [Fact]
        public void RequireRoles_Should_NormalizeRoles_When_RolesProvided()
        {
            // Arrange
            var roles = new[] { "admin", "DOCTOR" };

            // Act
            var result = AuthorizeRoles.RequireRoles(roles);

            // Assert
            result.Should().NotBeNull();
            result.Roles.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void RequireRoles_Should_HandleEmptyRoleArray_When_EmptyArrayProvided()
        {
            // Arrange
            var roles = new string[] { };

            // Act
            var result = AuthorizeRoles.RequireRoles(roles);

            // Assert
            result.Should().NotBeNull();
            result.Roles.Should().Be("");
        }

        [Fact]
        public void RequireRoles_Should_ThrowArgumentNullException_When_NullRolesProvided()
        {
            // Arrange
            string[] nullRoles = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => AuthorizeRoles.RequireRoles(nullRoles));
        }

        [Theory]
        [InlineData("AdminPolicy")]
        [InlineData("DoctorPolicy")]
        [InlineData("CustomPolicy")]
        public void RequirePolicy_Should_ReturnAuthorizeAttributeWithPolicy_When_ValidPolicyProvided(string policy)
        {
            // Act
            var result = AuthorizeRoles.RequirePolicy(policy);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<AuthorizeAttribute>();
            result.Policy.Should().Be(policy);
        }

        [Fact]
        public void RequirePolicy_Should_HandleEmptyPolicy_When_EmptyStringProvided()
        {
            // Arrange
            var policy = "";

            // Act
            var result = AuthorizeRoles.RequirePolicy(policy);

            // Assert
            result.Should().NotBeNull();
            result.Policy.Should().Be("");
        }

        [Fact]
        public void RequirePolicy_Should_HandleNullPolicy_When_NullProvided()
        {
            // Arrange
            string nullPolicy = null;

            // Act
            var result = AuthorizeRoles.RequirePolicy(nullPolicy);

            // Assert
            result.Should().NotBeNull();
            result.Policy.Should().BeNull();
        }

        [Fact]
        public void Admin_Should_ReturnSameInstance_When_AccessedMultipleTimes()
        {
            // Act
            var admin1 = AuthorizeRoles.Admin;
            var admin2 = AuthorizeRoles.Admin;

            // Assert
            admin1.Should().BeSameAs(admin2);
        }

        [Fact]
        public void Doctor_Should_ReturnSameInstance_When_AccessedMultipleTimes()
        {
            // Act
            var doctor1 = AuthorizeRoles.Doctor;
            var doctor2 = AuthorizeRoles.Doctor;

            // Assert
            doctor1.Should().BeSameAs(doctor2);
        }

        [Fact]
        public void DoctorOrAdmin_Should_ReturnSameInstance_When_AccessedMultipleTimes()
        {
            // Act
            var doctorOrAdmin1 = AuthorizeRoles.DoctorOrAdmin;
            var doctorOrAdmin2 = AuthorizeRoles.DoctorOrAdmin;

            // Assert
            doctorOrAdmin1.Should().BeSameAs(doctorOrAdmin2);
        }

        [Fact]
        public void RequireRoles_Should_HandleSpecialCharactersInRoles_When_SpecialCharactersProvided()
        {
            // Arrange
            var roles = new[] { "Role-With-Dashes", "Role_With_Underscores", "Role.With.Dots" };

            // Act
            var result = AuthorizeRoles.RequireRoles(roles);

            // Assert
            result.Should().NotBeNull();
            result.Roles.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void RequirePolicy_Should_HandleSpecialCharactersInPolicy_When_SpecialCharactersProvided()
        {
            // Arrange
            var policy = "Policy-With-Special_Characters.123";

            // Act
            var result = AuthorizeRoles.RequirePolicy(policy);

            // Assert
            result.Should().NotBeNull();
            result.Policy.Should().Be(policy);
        }
    }
}