using System.Security.Claims;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Server.Unit.Controllers;

/// <summary>
/// MedicalCaseAuditController 单元测试
/// 测试审计操作（2个方法）
/// </summary>
public class MedicalCaseAuditControllerTests
{
    private readonly IMedicalCaseFacade _facade;
    private readonly MedicalCaseMapper _mapper;
    private readonly ILogger<MedicalCaseAuditController> _logger;
    private readonly MedicalCaseAuditController _controller;

    public MedicalCaseAuditControllerTests()
    {
        _facade = Substitute.For<IMedicalCaseFacade>();
        _mapper = new MedicalCaseMapper();
        _logger = Substitute.For<ILogger<MedicalCaseAuditController>>();
        _controller = new MedicalCaseAuditController(_facade, _mapper, _logger);
        SetupControllerContext(_controller);
    }

    /// <summary>
    /// 设置控制器的 HttpContext 和 User Claims
    /// </summary>
    private void SetupControllerContext(ControllerBase controller)
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "TestDoctor"),
            new(ClaimTypes.Role, "Doctor")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetPermissions - 获取权限

    [Fact]
    public async Task GetPermissions_WhenExists_ReturnsOkWithPermissions()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = userId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var permissions = new MedicalCasePermissionDto
        {
            CanEdit = true,
            CanDelete = true,
            RequiresEditReason = false,
            DenialReason = null
        };

        _facade.GetByIdAsync(id).Returns(entity);
        _facade.GetPermissions(userId, UserRole.Doctor, entity).Returns(permissions);

        // Act
        var result = await _controller.GetPermissions(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCasePermissionDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("查询成功");
        response.Data.Should().NotBeNull();
        response.Data!.CanEdit.Should().BeTrue();
        response.Data.CanDelete.Should().BeTrue();
        response.Data.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public async Task GetPermissions_WhenReadOnly_ReturnsOkWithReadOnlyPermissions()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(), // Different user
            DoctorName = "OtherDoctor",
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddDays(-2), // Completed 2 days ago
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var permissions = new MedicalCasePermissionDto
        {
            CanEdit = false,
            CanDelete = false,
            RequiresEditReason = true,
            DenialReason = "医案已锁定，只能查看"
        };

        _facade.GetByIdAsync(id).Returns(entity);
        _facade.GetPermissions(userId, UserRole.Doctor, entity).Returns(permissions);

        // Act
        var result = await _controller.GetPermissions(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCasePermissionDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.CanEdit.Should().BeFalse();
        response.Data.CanDelete.Should().BeFalse();
        response.Data.IsReadOnly.Should().BeTrue();
        response.Data.DenialReason.Should().Be("医案已锁定，只能查看");
    }

    [Fact]
    public async Task GetPermissions_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _facade.GetByIdAsync(id).Returns((MedicalCase?)null);

        // Act
        var result = await _controller.GetPermissions(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCasePermissionDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    [Fact]
    public async Task GetPermissions_AsAdmin_ReturnsFullPermissions()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Setup admin context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "TestAdmin"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(), // Different user
            DoctorName = "OtherDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        var permissions = new MedicalCasePermissionDto
        {
            CanEdit = true,
            CanDelete = true,
            RequiresEditReason = false,
            DenialReason = null
        };

        _facade.GetByIdAsync(id).Returns(entity);
        _facade.GetPermissions(userId, UserRole.Admin, entity).Returns(permissions);

        // Act
        var result = await _controller.GetPermissions(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCasePermissionDto>;
        response.Should().NotBeNull();
        response!.Data!.CanEdit.Should().BeTrue();
        response.Data.CanDelete.Should().BeTrue();
    }

    #endregion

    #region GetAuditLogs - 获取审计日志

    [Fact]
    public async Task GetAuditLogs_WhenExists_ReturnsOkWithLogs()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(),
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        var logs = new List<MedicalCaseAuditLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = id,
                OperatorId = Guid.NewGuid(),
                OperatorName = "TestDoctor",
                OperatorRole = UserRole.Doctor,
                OperationType = AuditOperationType.Create,
                ChangedFields = "[]",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = id,
                OperatorId = Guid.NewGuid(),
                OperatorName = "TestDoctor",
                OperatorRole = UserRole.Doctor,
                OperationType = AuditOperationType.Update,
                ChangedFields = "[\"Remark\"]",
                OldValues = "{\"Remark\":\"old\"}",
                NewValues = "{\"Remark\":\"new\"}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _facade.GetByIdAsync(id).Returns(entity);
        _facade.GetAuditLogsPagedAsync(id, 1, 20).Returns((logs, logs.Count));

        // Act
        var result = await _controller.GetAuditLogs(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseAuditLogPagedResultDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("查询成功");
        response.Data.Should().NotBeNull();
        response.Data!.Logs.Should().HaveCount(2);
        response.Data.TotalCount.Should().Be(2);
        response.Data.CurrentPage.Should().Be(1);
        response.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetAuditLogs_WhenEmpty_ReturnsOkWithEmptyList()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(),
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        var logs = new List<MedicalCaseAuditLog>();

        _facade.GetByIdAsync(id).Returns(entity);
        _facade.GetAuditLogsPagedAsync(id, 1, 20).Returns((logs, 0));

        // Act
        var result = await _controller.GetAuditLogs(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseAuditLogPagedResultDto>;
        response.Should().NotBeNull();
        response!.Data!.Logs.Should().BeEmpty();
        response.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAuditLogs_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _facade.GetByIdAsync(id).Returns((MedicalCase?)null);

        // Act
        var result = await _controller.GetAuditLogs(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseAuditLogPagedResultDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetAuditLogs_WithInvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(),
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        _facade.GetByIdAsync(id).Returns(entity);

        // Act
        var result = await _controller.GetAuditLogs(id, page, pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse<MedicalCaseAuditLogPagedResultDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuditLogs_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(),
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        var logs = new List<MedicalCaseAuditLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = id,
                OperatorId = Guid.NewGuid(),
                OperatorName = "TestDoctor",
                OperatorRole = UserRole.Doctor,
                OperationType = AuditOperationType.Update,
                CreatedAt = DateTime.UtcNow
            }
        };

        _facade.GetByIdAsync(id).Returns(entity);
        _facade.GetAuditLogsPagedAsync(id, 2, 10).Returns((logs, 11));

        // Act
        var result = await _controller.GetAuditLogs(id, page: 2, pageSize: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseAuditLogPagedResultDto>;
        response.Should().NotBeNull();
        response!.Data!.CurrentPage.Should().Be(2);
        response.Data.PageSize.Should().Be(10);
        response.Data.TotalCount.Should().Be(11);
        response.Data.TotalPages.Should().Be(2);
    }

    #endregion
}
