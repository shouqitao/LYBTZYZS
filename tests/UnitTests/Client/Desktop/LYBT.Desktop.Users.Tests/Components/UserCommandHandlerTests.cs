using FluentAssertions;
using LYBT.Desktop.Users.Components;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Desktop.Users.Tests.Components
{
    /// <summary>
    /// UserCommandHandler 单元测试
    /// Issue #1779: Users模块组件化测试
    /// </summary>
    public class UserCommandHandlerTests
    {
        private readonly Mock<UserDataManager> _mockDataManager;
        private readonly Mock<UserValidator> _mockValidator;
        private readonly Mock<ILogger<UserCommandHandler>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly UserCommandHandler _commandHandler;

        public UserCommandHandlerTests()
        {
            _mockDataManager = new Mock<UserDataManager>(
                Mock.Of<IUserRepository>(),
                Mock.Of<ILogger<UserDataManager>>());

            _mockValidator = new Mock<UserValidator>(
                Mock.Of<LYBT.Desktop.Infrastructure.Interfaces.Components.IValidationService>(),
                _mockDataManager.Object,
                Mock.Of<ILogger<UserValidator>>());

            _mockLogger = new Mock<ILogger<UserCommandHandler>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockEventAggregator = new Mock<IEventAggregator>();

            _commandHandler = new UserCommandHandler(
                _mockDataManager.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _mockRegionManager.Object,
                _mockEventAggregator.Object);
        }

        [Fact]
        public async Task ToggleStatusAsync_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var mockUser = new UserDto
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Status = CommonStatus.Enabled
            };
            _mockDataManager.Setup(m => m.Current).Returns(mockUser);
            _mockValidator.Setup(v => v.CanToggleStatus(out It.Ref<string>.IsAny)).Returns(true);
            _mockDataManager.Setup(m => m.ToggleStatusAsync()).ReturnsAsync(true);

            // Act
            var result = await _commandHandler.ToggleStatusAsync();

            // Assert
            result.Should().BeTrue();
            _mockValidator.Verify(v => v.CanToggleStatus(out It.Ref<string>.IsAny), Times.Once);
            _mockDataManager.Verify(m => m.ToggleStatusAsync(), Times.Once);
        }

        [Fact]
        public async Task ReloadAsync_ShouldCallDataManager()
        {
            // Arrange
            _mockDataManager.Setup(m => m.ReloadAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _commandHandler.ReloadAsync();

            // Assert
            result.Should().BeTrue();
            _mockDataManager.Verify(m => m.ReloadAsync(), Times.Once);
        }

        [Fact]
        public async Task NavigateToUserEditAsync_ShouldNavigate()
        {
            // Arrange
            var testId = Guid.NewGuid();
            _mockRegionManager.Setup(r => r.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NavigationParameters>()));

            // Act
            var result = await _commandHandler.NavigateToUserEditAsync(testId);

            // Assert
            result.Should().BeTrue();
            _mockRegionManager.Verify(r => r.RequestNavigate(
                "AdminContentRegion",
                "UserEditView",
                It.IsAny<NavigationParameters>()), Times.Once);
        }
    }
}
