using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Users.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using Xunit;

namespace LYBT.Desktop.Users.Tests.ViewModels
{
    /// <summary>
    /// ChangePasswordDialogViewModel 单元测试
    /// 测试修改密码对话框的核心功能
    /// </summary>
    public class ChangePasswordDialogViewModelTests : IDisposable
    {
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<ChangePasswordDialogViewModel>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserNotificationService> _mockNotificationService;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly ChangePasswordDialogViewModel _viewModel;

        public ChangePasswordDialogViewModelTests()
        {
            // Arrange - Setup Mocks
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<ChangePasswordDialogViewModel>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockNotificationService = new Mock<IUserNotificationService>();
            _mockAuthService = new Mock<IAuthenticationService>();

            // Setup LoggerFactory to return mock logger
            _mockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            // Create ViewModel instance
            _viewModel = new ChangePasswordDialogViewModel(
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object,
                _mockRegionManager.Object,
                _mockAuthService.Object,
                _mockSessionManager.Object,
                _mockNotificationService.Object
            );
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Assert
            _viewModel.Should().NotBeNull();
            _viewModel.Title.Should().Be("修改密码");
            _viewModel.ConfirmCommand.Should().NotBeNull();
            _viewModel.CancelCommand.Should().NotBeNull();
        }

        #endregion

        #region 密码强度计算测试

        [Theory]
        [InlineData("abc", 0)] // 太短，无强度
        [InlineData("Abcd1234", 1)] // 8字符，大小写+数字 -> 弱 (strength=3, 3/2=1)
        [InlineData("Abcd1234!@#$", 2)] // 12字符，大小写+数字+特殊 -> 中 (strength=5, 5/2=2)
        [InlineData("Abcdefgh12345!@#", 2)] // 16字符，完整复杂度 -> 中 (strength=5, 5/2=2, 最高只能2)
        public void CalculatePasswordStrength_WithDifferentPasswords_ShouldReturnCorrectStrength(string password, int expectedStrength)
        {
            // Act
            _viewModel.NewPassword = password;

            // Assert
            _viewModel.PasswordStrength.Should().Be(expectedStrength);
        }

        [Theory]
        [InlineData(1, "密码强度：弱")]
        [InlineData(2, "密码强度：中")]
        public void PasswordStrengthText_ShouldMatchStrengthValue(int strength, string expectedText)
        {
            // Arrange - 设置能产生对应强度的密码
            // 注意：当前算法 strength = Math.Min(rawStrength / 2, 3)，最多只能达到2
            var passwords = new Dictionary<int, string>
            {
                { 1, "Abcd1234" },           // strength=3 → 3/2=1
                { 2, "Abcd1234!@#$" }        // strength=5 → 5/2=2
            };

            // Act
            _viewModel.NewPassword = passwords[strength];

            // Assert
            _viewModel.PasswordStrengthText.Should().Be(expectedText);
        }

        #endregion

        #region 密码验证测试

        [Fact]
        public void ValidatePasswords_WithEmptyCurrentPassword_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = string.Empty;
            _viewModel.NewPassword = "NewPass123!";
            _viewModel.ConfirmPassword = "NewPass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("当前密码");
        }

        [Fact]
        public void ValidatePasswords_WithShortNewPassword_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "Short1!"; // 7字符，少于8
            _viewModel.ConfirmPassword = "Short1!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("至少8个字符");
        }

        [Fact]
        public void ValidatePasswords_WithoutUpperAndLowerCase_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "newpass123!"; // 只有小写
            _viewModel.ConfirmPassword = "newpass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("大小写字母");
        }

        [Fact]
        public void ValidatePasswords_WithoutDigit_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "NewPassword!"; // 无数字
            _viewModel.ConfirmPassword = "NewPassword!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("数字");
        }

        [Fact]
        public void ValidatePasswords_WithoutSpecialChar_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "NewPass1234"; // 无特殊字符
            _viewModel.ConfirmPassword = "NewPass1234";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("特殊字符");
        }

        [Fact]
        public void ValidatePasswords_WithMismatchedPasswords_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "NewPass123!";
            _viewModel.ConfirmPassword = "DifferentPass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("不一致");
        }

        [Fact]
        public void ValidatePasswords_WithSameOldAndNewPassword_ShouldFail()
        {
            // Arrange
            _viewModel.CurrentPassword = "SamePass123!";
            _viewModel.NewPassword = "SamePass123!";
            _viewModel.ConfirmPassword = "SamePass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("不能与当前密码相同");
        }

        [Fact]
        public void ValidatePasswords_WithValidInput_ShouldPass()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "NewPass123!";
            _viewModel.ConfirmPassword = "NewPass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeTrue();
            _viewModel.HasError.Should().BeFalse();
        }

        #endregion

        #region 命令测试

        [Fact]
        public void CanConfirm_WithAllPasswordsFilled_ShouldReturnTrue()
        {
            // Arrange
            _viewModel.CurrentPassword = "OldPass123!";
            _viewModel.NewPassword = "NewPass123!";
            _viewModel.ConfirmPassword = "NewPass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("CanConfirm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canConfirm = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canConfirm.Should().BeTrue();
        }

        [Fact]
        public void CanConfirm_WithEmptyPassword_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.CurrentPassword = string.Empty;
            _viewModel.NewPassword = "NewPass123!";
            _viewModel.ConfirmPassword = "NewPass123!";

            // Act
            var method = typeof(ChangePasswordDialogViewModel)
                .GetMethod("CanConfirm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canConfirm = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canConfirm.Should().BeFalse();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _viewModel?.Dispose();
        }

        #endregion
    }
}
