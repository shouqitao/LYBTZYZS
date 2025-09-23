using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Session;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Tests.Services.Session;

/// <summary>
/// UnifiedSessionManager单元测试 - Phase 2重构验证
/// 专注于核心会话管理功能的测试
/// </summary>
public class UnifiedSessionManagerTests : IDisposable
{
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly Mock<ILogger<UnifiedSessionManager>> _mockLogger;
    private readonly UnifiedSessionManager _sessionManager;
    private readonly UserDto _testUser;

    public UnifiedSessionManagerTests()
    {
        _mockPermissionService = new Mock<IPermissionService>();
        _mockLogger = new Mock<ILogger<UnifiedSessionManager>>();
        _sessionManager = new UnifiedSessionManager(_mockPermissionService.Object, _mockLogger.Object);

        // 测试数据初始化 - 使用实际的DTO结构
        _testUser = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "test-doctor",
            RealName = "测试医生",
            Role = UserRole.Doctor,
            PhoneNumber = "13800138000",
            Email = "test@lybt.com"
        };
    }

    [Fact]
    public void SetUserSession_ShouldSetUserAndToken()
    {
        // Arrange
        const string token = "test-jwt-token";
        var eventRaised = false;
        _sessionManager.UserSessionChanged += (_, _) => eventRaised = true;

        // Act
        _sessionManager.SetUserSession(_testUser, token);

        // Assert
        _sessionManager.CurrentUser.Should().Be(_testUser);
        _sessionManager.IsLoggedIn.Should().BeTrue();
        _sessionManager.LoginTime.Should().NotBeNull();
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void ClearUserSession_ShouldClearUserData()
    {
        // Arrange
        _sessionManager.SetUserSession(_testUser, "token");
        var eventRaised = false;
        _sessionManager.UserSessionChanged += (_, _) => eventRaised = true;

        // Act
        _sessionManager.ClearUserSession();

        // Assert
        _sessionManager.CurrentUser.Should().BeNull();
        _sessionManager.IsLoggedIn.Should().BeFalse();
        _sessionManager.LoginTime.Should().BeNull();
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void GetUserRole_ShouldReturnCorrectRole()
    {
        // Arrange
        _sessionManager.SetUserSession(_testUser, "token");

        // Act
        var role = _sessionManager.GetUserRole();

        // Assert
        role.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public void HasRole_ShouldValidateUserRole()
    {
        // Arrange
        _sessionManager.SetUserSession(_testUser, "token");

        // Act & Assert
        _sessionManager.HasRole(UserRole.Doctor).Should().BeTrue();
        _sessionManager.HasRole(UserRole.Admin).Should().BeFalse();
    }

    [Fact]
    public void GetUserRole_WithoutLogin_ShouldReturnNull()
    {
        // Act
        var role = _sessionManager.GetUserRole();

        // Assert
        role.Should().BeNull();
    }

    [Fact]
    public void HasRole_WithoutLogin_ShouldReturnFalse()
    {
        // Act & Assert
        _sessionManager.HasRole(UserRole.Doctor).Should().BeFalse();
        _sessionManager.HasRole(UserRole.Admin).Should().BeFalse();
    }

    [Fact]
    public void HasPermission_ShouldValidatePermissions()
    {
        // Arrange
        const string permission = "CREATE_PRESCRIPTION";
        _mockPermissionService.Setup(p => p.HasPermission(_testUser, permission))
            .Returns(true);
        _sessionManager.SetUserSession(_testUser, "token");

        // Act
        var result = _sessionManager.HasPermission(permission);

        // Assert
        result.Should().BeTrue();
        _mockPermissionService.Verify(p => p.HasPermission(_testUser, permission), Times.Once);
    }

    [Fact]
    public void HasPermission_WithoutLogin_ShouldReturnFalse()
    {
        // Arrange
        const string permission = "CREATE_PRESCRIPTION";

        // Act
        var result = _sessionManager.HasPermission(permission);

        // Assert
        result.Should().BeFalse();
        _mockPermissionService.Verify(p => p.HasPermission(It.IsAny<UserRole>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SelectPatient_ShouldUpdateCurrentPatient()
    {
        // Arrange
        var testPatient = new PatientDto
        {
            Id = Guid.NewGuid(),
            Name = "测试患者"
        };
        _sessionManager.SetUserSession(_testUser, "token");
        var eventRaised = false;
        _sessionManager.PatientSelectionChanged += (_, _) => eventRaised = true;

        // Act
        _sessionManager.SelectPatient(testPatient);

        // Assert
        _sessionManager.CurrentPatient.Should().Be(testPatient);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void ClearPatientSelection_ShouldClearCurrentPatient()
    {
        // Arrange
        var testPatient = new PatientDto
        {
            Id = Guid.NewGuid(),
            Name = "测试患者"
        };
        _sessionManager.SetUserSession(_testUser, "token");
        _sessionManager.SelectPatient(testPatient);
        var eventRaised = false;
        _sessionManager.PatientSelectionChanged += (_, _) => eventRaised = true;

        // Act
        _sessionManager.ClearPatientSelection();

        // Assert
        _sessionManager.CurrentPatient.Should().BeNull();
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new List<bool>();
        var lockObject = new object();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                var user = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Username = $"user-{index}",
                    RealName = $"用户{index}",
                    Role = UserRole.Doctor
                };

                try
                {
                    _sessionManager.SetUserSession(user, $"token-{index}");
                    var isLoggedIn = _sessionManager.IsLoggedIn;
                    
                    lock (lockObject)
                    {
                        results.Add(isLoggedIn);
                    }
                }
                catch
                {
                    lock (lockObject)
                    {
                        results.Add(false);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.All(r => r).Should().BeTrue();
        results.Count.Should().Be(10);
    }

    public void Dispose()
    {
        _sessionManager?.Dispose();
    }
}