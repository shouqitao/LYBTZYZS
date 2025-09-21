using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    public class SysAdminHandlerTests
    {
        private readonly SysAdminHandler _sysAdminHandler;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IOptions<SysAdminOptions>> _mockSysAdminOptions;
        private readonly SysAdminOptions _sysAdminOptions;

        public SysAdminHandlerTests()
        {
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockSysAdminOptions = new Mock<IOptions<SysAdminOptions>>();

            _sysAdminOptions = new SysAdminOptions
            {
                Username = "sysadmin",
                DefaultPassword = "LybtAdmin2025@SecurePass!"
            };

            _mockSysAdminOptions.Setup(x => x.Value).Returns(_sysAdminOptions);

            _sysAdminHandler = new SysAdminHandler(
                _mockAuthRepository.Object,
                _mockSysAdminOptions.Object);
        }

        [Fact]
        public void SysAdminHandler_Should_Implement_ISysAdminHandler()
        {
            _sysAdminHandler.Should().BeAssignableTo<ISysAdminHandler>();
        }

        [Theory]
        [InlineData("sysadmin", true)]
        [InlineData("SYSADMIN", true)]
        [InlineData("SysAdmin", true)]
        [InlineData("admin", false)]
        [InlineData("user", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSysAdmin_Should_Return_Correct_Result(string username, bool expected)
        {
            // Act
            var result = _sysAdminHandler.IsSysAdmin(username);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task GetSysAdminPasswordHashAsync_Should_Return_PasswordHash_When_Valid()
        {
            // Arrange
            var expectedHash = "hashed_password_value";
            _mockAuthRepository.Setup(x => x.GetAdminPasswordHashAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedHash);

            // Act
            var result = await _sysAdminHandler.GetSysAdminPasswordHashAsync();

            // Assert
            result.Should().Be(expectedHash);
            _mockAuthRepository.Verify(x => x.GetAdminPasswordHashAsync(_sysAdminOptions.Username), Times.Once);
        }

        [Fact]
        public async Task GetSysAdminPasswordHashAsync_Should_Return_Null_When_NoHash()
        {
            // Arrange
            _mockAuthRepository.Setup(x => x.GetAdminPasswordHashAsync(It.IsAny<string>()))
                .ReturnsAsync((string)null);

            // Act
            var result = await _sysAdminHandler.GetSysAdminPasswordHashAsync();

            // Assert
            result.Should().BeNull();
            _mockAuthRepository.Verify(x => x.GetAdminPasswordHashAsync(_sysAdminOptions.Username), Times.Once);
        }

        [Fact]
        public void Constructor_Should_Throw_When_AuthRepository_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SysAdminHandler(null, _mockSysAdminOptions.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_When_SysAdminOptions_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SysAdminHandler(_mockAuthRepository.Object, null));
        }
    }
}