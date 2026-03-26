using System.Security.Claims;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Server.Unit.Controllers;

/// <summary>
/// MedicalCaseWorkflowController 单元测试
/// 测试工作流操作（4个方法）
/// </summary>
public class MedicalCaseWorkflowControllerTests
{
    private readonly IMedicalCaseFacade _facade;
    private readonly MedicalCaseMapper _mapper;
    private readonly ILogger<MedicalCaseWorkflowController> _logger;
    private readonly MedicalCaseWorkflowController _controller;

    public MedicalCaseWorkflowControllerTests()
    {
        _facade = Substitute.For<IMedicalCaseFacade>();
        _mapper = new MedicalCaseMapper();
        _logger = Substitute.For<ILogger<MedicalCaseWorkflowController>>();
        _controller = new MedicalCaseWorkflowController(_facade, _mapper, _logger);
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

    #region UpdateStatus - 更新医案状态

    [Fact]
    public async Task UpdateStatus_WithActiveStatus_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Active };

        var updatedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(),
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        _facade.UpdateStatusAsync(id, request.Status)
            .Returns(updatedEntity);

        // Act
        var result = await _controller.UpdateStatus(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("状态更新成功");
        response.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Active);
    }

    [Fact]
    public async Task UpdateStatus_WithCompletedStatus_CallsCompleteAsyncAndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed };
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var completedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = doctorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _facade.CompleteAsync(id, doctorId, false, false)
            .Returns(completedEntity);

        // Act
        var result = await _controller.UpdateStatus(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("医案已完成");
        response.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task UpdateStatus_WhenMedicalCaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Suspended };

        _facade.UpdateStatusAsync(id, request.Status)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.UpdateStatus(id, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region CloseMedicalCase - 关闭医案

    [Fact]
    public async Task CloseMedicalCase_WhenExists_ReturnsOkWithClosedStatus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var closedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = doctorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _facade.CompleteAsync(id, doctorId, false, true)
            .Returns(closedEntity);

        // Act
        var result = await _controller.CloseMedicalCase(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("医案已关闭");
        response.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task CloseMedicalCase_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _facade.CompleteAsync(id, doctorId, false, true)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.CloseMedicalCase(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region Suspend - 挂起医案

    [Fact]
    public async Task Suspend_WhenExists_ReturnsOkWithSuspendedStatus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new ConsultationInputDto
        {
            PresentIllness = "Test illness"
        };

        var suspendedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = doctorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Suspended
        };

        _facade.SuspendAsync(id, request, doctorId, false)
            .Returns(suspendedEntity);

        // Act
        var result = await _controller.Suspend(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("医案已暂存");
        response.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Suspended);
    }

    [Fact]
    public async Task Suspend_WithNullRequest_ReturnsOkWithSuspendedStatus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var suspendedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = doctorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Suspended
        };

        _facade.SuspendAsync(id, null, doctorId, false)
            .Returns(suspendedEntity);

        // Act
        var result = await _controller.Suspend(id, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("医案已暂存");
    }

    [Fact]
    public async Task Suspend_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _facade.SuspendAsync(id, Arg.Any<ConsultationInputDto?>(), doctorId, false)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.Suspend(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region CancelMedicalCase - 取消医案

    [Fact]
    public async Task CancelMedicalCase_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new CancelMedicalCaseRequestDto { Reason = "Test cancellation" };

        var cancelledEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = doctorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            IsDeleted = true
        };

        _facade.CancelAsync(id, doctorId, false, request.Reason)
            .Returns(cancelledEntity);

        // Act
        var result = await _controller.CancelMedicalCase(id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelMedicalCase_WithNullRequest_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var cancelledEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = doctorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            IsDeleted = true
        };

        _facade.CancelAsync(id, doctorId, false, null)
            .Returns(cancelledEntity);

        // Act
        var result = await _controller.CancelMedicalCase(id, null);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelMedicalCase_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _facade.CancelAsync(id, doctorId, false, Arg.Any<string?>())
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.CancelMedicalCase(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion
}