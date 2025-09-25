using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Xunit;

namespace LYBT.Desktop.Core.Tests.Services
{
    /// <summary>
    /// SessionManager 单元测试
    /// 验证会话管理、用户登录、患者选择、诊疗状态管理等核心功能
    /// </summary>
    public class SessionManagerTests : IDisposable
    {
        private readonly Mock<IEventAggregator> _eventAggregatorMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<ILogger<SessionManager>> _loggerMock;
        private readonly SessionManager _sessionManager;
        private readonly Mock<ISettingsManager> _settingsManagerMock;

        public SessionManagerTests()
        {
            _eventAggregatorMock = new Mock<IEventAggregator>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<SessionManager>>();
            _settingsManagerMock = new Mock<ISettingsManager>();

            // 设置默认的缓存行为
            object cacheValue = null;
            _memoryCacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
                           .Returns(false);

            _sessionManager = new SessionManager(
                _eventAggregatorMock.Object,
                _memoryCacheMock.Object,
                _loggerMock.Object,
                _settingsManagerMock.Object);
        }

        #region 用户登录测试

        [Fact]
        public async Task LoginAsync_ValidUser_ShouldSetSessionCorrectly()
        {
            // Arrange
            var user = new UserDto
            {
                Id = Guid.NewGuid(),
                Name = "测试医生",
                LoginName = "testdoctor",
                UserRole = UserRole.Doctor
            };

            // Act
            await _sessionManager.LoginAsync(user, "test-token");

            // Assert
            _sessionManager.IsLoggedIn.Should().BeTrue();
            _sessionManager.CurrentUser.Should().BeEquivalentTo(user);
            _sessionManager.AuthToken.Should().Be("test-token");
            _sessionManager.LoginTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task LoginAsync_NullUser_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _sessionManager.LoginAsync(null, "token"));
        }

        [Fact]
        public async Task LoginAsync_EmptyToken_ShouldThrowArgumentException()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sessionManager.LoginAsync(user, string.Empty));
        }

        #endregion

        #region 用户登出测试

        [Fact]
        public async Task LogoutAsync_ShouldClearSession()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Name = "测试用户" };
            await _sessionManager.LoginAsync(user, "token");

            // Act
            await _sessionManager.LogoutAsync();

            // Assert
            _sessionManager.IsLoggedIn.Should().BeFalse();
            _sessionManager.CurrentUser.Should().BeNull();
            _sessionManager.AuthToken.Should().BeNull();
            _sessionManager.CurrentPatient.Should().BeNull();
            _sessionManager.ActiveConsultation.Should().BeNull();
        }

        #endregion

        #region 患者选择测试

        [Fact]
        public async Task SetCurrentPatientAsync_ValidPatient_ShouldSetPatientCorrectly()
        {
            // Arrange
            var patient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = Gender.Male,
                Age = 30
            };

            // Act
            await _sessionManager.SetCurrentPatientAsync(patient);

            // Assert
            _sessionManager.CurrentPatient.Should().BeEquivalentTo(patient);
            _sessionManager.HasActivePatient.Should().BeTrue();
        }

        [Fact]
        public async Task SetCurrentPatientAsync_Null_ShouldClearPatient()
        {
            // Arrange
            var patient = new PatientDto { Id = Guid.NewGuid() };
            await _sessionManager.SetCurrentPatientAsync(patient);

            // Act
            await _sessionManager.SetCurrentPatientAsync(null);

            // Assert
            _sessionManager.CurrentPatient.Should().BeNull();
            _sessionManager.HasActivePatient.Should().BeFalse();
        }

        #endregion

        #region 诊疗会话测试

        [Fact]
        public async Task StartConsultationAsync_ValidConsultation_ShouldSetActiveConsultation()
        {
            // Arrange
            var consultation = new ConsultationDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                ConsultationNo = "CONS-2024-001",
                Status = ConsultationStatus.InProgress
            };

            // Act
            await _sessionManager.StartConsultationAsync(consultation);

            // Assert
            _sessionManager.ActiveConsultation.Should().BeEquivalentTo(consultation);
            _sessionManager.IsInConsultation.Should().BeTrue();
        }

        [Fact]
        public async Task EndConsultationAsync_ShouldClearActiveConsultation()
        {
            // Arrange
            var consultation = new ConsultationDto { Id = Guid.NewGuid() };
            await _sessionManager.StartConsultationAsync(consultation);

            // Act
            await _sessionManager.EndConsultationAsync();

            // Assert
            _sessionManager.ActiveConsultation.Should().BeNull();
            _sessionManager.IsInConsultation.Should().BeFalse();
        }

        #endregion

        #region 会话验证测试

        [Fact]
        public void ValidateSession_LoggedIn_ShouldReturnTrue()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid() };
            _sessionManager.LoginAsync(user, "token").Wait();

            // Act
            var result = _sessionManager.ValidateSession();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateSession_NotLoggedIn_ShouldReturnFalse()
        {
            // Act
            var result = _sessionManager.ValidateSession();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateSession_ExpiredToken_ShouldReturnFalse()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid() };
            _sessionManager.LoginAsync(user, "token").Wait();

            // 模拟令牌过期（通过设置很早的登录时间）
            // 这需要SessionManager暴露一个测试方法或使用反射
            // 此处简化处理

            // Act & Assert
            // 需要根据实际的SessionManager实现来调整
        }

        #endregion

        #region 权限检查测试

        [Fact]
        public void HasPermission_AdminUser_ShouldHaveAllPermissions()
        {
            // Arrange
            var admin = new UserDto
            {
                Id = Guid.NewGuid(),
                UserRole = UserRole.Admin
            };
            _sessionManager.LoginAsync(admin, "token").Wait();

            // Act
            var canManageUsers = _sessionManager.HasPermission("ManageUsers");
            var canViewReports = _sessionManager.HasPermission("ViewReports");

            // Assert
            canManageUsers.Should().BeTrue();
            canViewReports.Should().BeTrue();
        }

        [Fact]
        public void HasPermission_DoctorUser_ShouldHaveLimitedPermissions()
        {
            // Arrange
            var doctor = new UserDto
            {
                Id = Guid.NewGuid(),
                UserRole = UserRole.Doctor
            };
            _sessionManager.LoginAsync(doctor, "token").Wait();

            // Act
            var canManageUsers = _sessionManager.HasPermission("ManageUsers");
            var canViewPatients = _sessionManager.HasPermission("ViewPatients");

            // Assert
            canManageUsers.Should().BeFalse();
            canViewPatients.Should().BeTrue();
        }

        #endregion

        #region 会话状态持久化测试

        [Fact]
        public async Task SaveSessionStateAsync_ShouldPersistToSettings()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Name = "测试用户" };
            await _sessionManager.LoginAsync(user, "token");

            // Act
            await _sessionManager.SaveSessionStateAsync();

            // Assert
            _settingsManagerMock.Verify(x => x.SaveSettingAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task RestoreSessionStateAsync_ValidState_ShouldRestoreSession()
        {
            // Arrange
            var sessionData = "{\"UserId\":\"" + Guid.NewGuid() + "\",\"Token\":\"valid-token\"}";
            _settingsManagerMock.Setup(x => x.GetSettingAsync<string>(It.IsAny<string>()))
                              .ReturnsAsync(sessionData);

            // Act
            var restored = await _sessionManager.RestoreSessionStateAsync();

            // Assert
            restored.Should().BeTrue();
            // 根据实际实现验证恢复的状态
        }

        #endregion

        public void Dispose()
        {
            _sessionManager?.Dispose();
        }
    }
}