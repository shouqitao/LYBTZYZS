using FluentAssertions;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.Users.Components;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Desktop.Users.Tests.Components
{
    /// <summary>
    /// UserValidator 单元测试
    /// Issue #1779: Users模块组件化测试
    /// </summary>
    public class UserValidatorTests
    {
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<UserDataManager> _mockDataManager;
        private readonly Mock<ILogger<UserValidator>> _mockLogger;
        private readonly UserValidator _validator;

        public UserValidatorTests()
        {
            _mockValidationService = new Mock<IValidationService>();
            _mockDataManager = new Mock<UserDataManager>(
                Mock.Of<LYBT.Desktop.Users.Interfaces.IUserRepository>(),
                Mock.Of<ILogger<UserDataManager>>());
            _mockLogger = new Mock<ILogger<UserValidator>>();

            _validator = new UserValidator(
                _mockValidationService.Object,
                _mockDataManager.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void IsValid_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var validUser = new UserDto
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            _mockDataManager.Setup(m => m.Current).Returns(validUser);
            _mockValidationService.Setup(v => v.IsValid(It.IsAny<UserDto>(), out It.Ref<string>.IsAny))
                .Returns(true);

            // Act
            var result = _validator.IsValid(out var errorMessage);

            // Assert
            result.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_WithNullData_ShouldReturnFalse()
        {
            // Arrange
            _mockDataManager.Setup(m => m.Current).Returns((UserDto?)null);

            // Act
            var result = _validator.IsValid(out var errorMessage);

            // Assert
            result.Should().BeFalse();
            errorMessage.Should().NotBeEmpty();
        }

        [Fact]
        public void CanEditUser_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var validUser = new UserDto
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            _mockDataManager.Setup(m => m.Current).Returns(validUser);

            // Act
            var result = _validator.CanEditUser(out var errorMessage);

            // Assert
            result.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }
    }
}
