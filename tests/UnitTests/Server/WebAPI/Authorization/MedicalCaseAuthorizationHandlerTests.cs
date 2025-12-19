using System.Security.Claims;
using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Authorization;

/// <summary>
/// 医案授权处理器单元测试
/// refactor-authorization-system: Task 1.4
/// </summary>
public class MedicalCaseAuthorizationHandlerTests
{
    private readonly Mock<IMedicalCasePermissionService> _mockPermissionService;
    private readonly ILogger<MedicalCaseAuthorizationHandler> _logger;
    private readonly MedicalCaseAuthorizationHandler _handler;

    public MedicalCaseAuthorizationHandlerTests()
    {
        _mockPermissionService = new Mock<IMedicalCasePermissionService>();
        _logger = NullLogger<MedicalCaseAuthorizationHandler>.Instance;
        _handler = new MedicalCaseAuthorizationHandler(_mockPermissionService.Object, _logger);
    }

    #region Admin可编辑所有医案

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Admin_CanEdit_AllMedicalCases(UserRole role)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid(); // 不同的医生创建的医案
        var medicalCase = CreateMedicalCase(doctorId, MedicalCaseStatus.Active);

        _mockPermissionService
            .Setup(s => s.CanEdit(userId, role, medicalCase))
            .Returns(true);

        var context = CreateAuthorizationContext(userId, role, medicalCase, MedicalCaseOperations.Edit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("Admin 应该可以编辑任意医案");
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Admin_CanDelete_AllMedicalCases(UserRole role)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(doctorId, MedicalCaseStatus.Completed);

        _mockPermissionService
            .Setup(s => s.CanDelete(userId, role, medicalCase))
            .Returns(true);

        var context = CreateAuthorizationContext(userId, role, medicalCase, MedicalCaseOperations.Delete);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("Admin 应该可以删除任意医案");
    }

    #endregion

    #region Doctor可编辑自己的Draft/Active医案

    [Theory]
    [InlineData(MedicalCaseStatus.Draft)]
    [InlineData(MedicalCaseStatus.Active)]
    public async Task Doctor_CanEdit_OwnDraftOrActiveMedicalCase(MedicalCaseStatus status)
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(doctorId, status);

        _mockPermissionService
            .Setup(s => s.CanEdit(doctorId, UserRole.Doctor, medicalCase))
            .Returns(true);

        var context = CreateAuthorizationContext(doctorId, UserRole.Doctor, medicalCase, MedicalCaseOperations.Edit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue($"Doctor 应该可以编辑自己的 {status} 医案");
    }

    #endregion

    #region Doctor不能编辑他人医案

    [Fact]
    public async Task Doctor_CannotEdit_OtherDoctorMedicalCase()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var otherDoctorId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(otherDoctorId, MedicalCaseStatus.Draft);

        _mockPermissionService
            .Setup(s => s.CanEdit(doctorId, UserRole.Doctor, medicalCase))
            .Returns(false);

        var context = CreateAuthorizationContext(doctorId, UserRole.Doctor, medicalCase, MedicalCaseOperations.Edit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("Doctor 不应该能编辑他人的医案");
    }

    #endregion

    #region Doctor不能编辑Completed医案

    [Fact]
    public async Task Doctor_CannotEdit_CompletedMedicalCase()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(doctorId, MedicalCaseStatus.Completed);

        _mockPermissionService
            .Setup(s => s.CanEdit(doctorId, UserRole.Doctor, medicalCase))
            .Returns(false);

        var context = CreateAuthorizationContext(doctorId, UserRole.Doctor, medicalCase, MedicalCaseOperations.Edit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("Doctor 不应该能编辑已完成的医案");
    }

    #endregion

    #region 未认证用户无权限

    [Fact]
    public async Task UnauthenticatedUser_CannotEdit()
    {
        // Arrange
        var medicalCase = CreateMedicalCase(Guid.NewGuid(), MedicalCaseStatus.Draft);
        var context = CreateAuthorizationContextWithoutUser(medicalCase, MedicalCaseOperations.Edit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("未认证用户不应该有任何权限");
    }

    #endregion

    #region Read操作验证

    [Fact]
    public async Task AuthenticatedUser_CanRead_AnyMedicalCase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(doctorId, MedicalCaseStatus.Completed);
        var context = CreateAuthorizationContext(userId, UserRole.Doctor, medicalCase, MedicalCaseOperations.Read);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("已认证用户应该可以读取任意医案");
    }

    #endregion

    #region Claims提取测试

    [Fact]
    public async Task Handler_ExtractsUserIdFromNameIdentifierClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(userId, MedicalCaseStatus.Draft);

        _mockPermissionService
            .Setup(s => s.CanEdit(userId, UserRole.Doctor, medicalCase))
            .Returns(true);

        // 使用 NameIdentifier claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "Doctor")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            new[] { MedicalCaseOperations.Edit },
            principal,
            medicalCase);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
        _mockPermissionService.Verify(s => s.CanEdit(userId, UserRole.Doctor, medicalCase), Times.Once);
    }

    [Fact]
    public async Task Handler_ExtractsUserIdFromSubClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(userId, MedicalCaseStatus.Draft);

        _mockPermissionService
            .Setup(s => s.CanEdit(userId, UserRole.Doctor, medicalCase))
            .Returns(true);

        // 使用 sub claim (JWT 标准)
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("role", "Doctor")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            new[] { MedicalCaseOperations.Edit },
            principal,
            medicalCase);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_HandlesSysAdminLegacyRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medicalCase = CreateMedicalCase(Guid.NewGuid(), MedicalCaseStatus.Draft);

        _mockPermissionService
            .Setup(s => s.CanEdit(userId, UserRole.SuperAdmin, medicalCase))
            .Returns(true);

        // 使用遗留的 SysAdmin 角色名
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "SysAdmin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            new[] { MedicalCaseOperations.Edit },
            principal,
            medicalCase);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
        _mockPermissionService.Verify(s => s.CanEdit(userId, UserRole.SuperAdmin, medicalCase), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static MedicalCase CreateMedicalCase(Guid doctorId, MedicalCaseStatus status)
    {
        return new MedicalCase
        {
            Id = Guid.NewGuid(),
            UserId = doctorId,
            CaseStatus = status,
            PatientId = Guid.NewGuid()
        };
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        Guid userId,
        UserRole role,
        MedicalCase resource,
        OperationAuthorizationRequirement requirement)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource);
    }

    private static AuthorizationHandlerContext CreateAuthorizationContextWithoutUser(
        MedicalCase resource,
        OperationAuthorizationRequirement requirement)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // 无认证身份

        return new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource);
    }

    #endregion
}
