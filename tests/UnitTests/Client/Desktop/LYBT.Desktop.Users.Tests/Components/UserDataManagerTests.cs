using FluentAssertions;
using LYBT.Desktop.Users.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Users.Tests.Components
{
    /// <summary>
    /// UserDataManager 单元测试
    /// Issue #1779: Users模块组件化测试
    /// </summary>
    public class UserDataManagerTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly Mock<ILogger<UserDataManager>> _mockLogger;
        private readonly UserDataManager _dataManager;

        public UserDataManagerTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserDataManager>>();
            _dataManager = new UserDataManager(
                _mockRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task InitializeAsync_WithValidId_ShouldLoadUser()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockUser = new UserDto
            {
                Id = testId,
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            _mockRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(mockUser);

            // Act
            await _dataManager.InitializeAsync(testId);

            // Assert
            _dataManager.Current.Should().NotBeNull();
            _dataManager.Current!.UserName.Should().Be("testuser");
        }

        [Fact]
        public async Task SaveAsync_WithChanges_ShouldCallRepository()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockUser = new UserDto
            {
                Id = testId,
                UserName = "testuser",
                RealName = "原始姓名",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            _mockRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(mockUser);
            await _dataManager.InitializeAsync(testId);

            // Modify user
            var updatedUser = new UserDto
            {
                Id = testId,
                UserName = "testuser",
                RealName = "新姓名",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            _dataManager.UpdateUser(updatedUser);

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserInputDto>()))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _dataManager.SaveAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserInputDto>()), Times.Once);
        }

        [Fact]
        public async Task ToggleStatusAsync_ShouldChangeStatus()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockUser = new UserDto
            {
                Id = testId,
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            _mockRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(mockUser);
            await _dataManager.InitializeAsync(testId);

            var toggledUser = new UserDto
            {
                Id = testId,
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Disabled
            };
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserInputDto>()))
                .ReturnsAsync(toggledUser);

            // Act
            var result = await _dataManager.ToggleStatusAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserInputDto>()), Times.Once);
        }
    }
}
