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
/// MedicalCasePrintController 单元测试
/// 测试打印操作（2个方法）
/// </summary>
public class MedicalCasePrintControllerTests
{
    private readonly IMedicalCaseFacade _facade;
    private readonly MedicalCaseMapper _mapper;
    private readonly ILogger<MedicalCasePrintController> _logger;
    private readonly MedicalCasePrintController _controller;

    public MedicalCasePrintControllerTests()
    {
        _facade = Substitute.For<IMedicalCaseFacade>();
        _mapper = new MedicalCaseMapper();
        _logger = Substitute.For<ILogger<MedicalCasePrintController>>();
        _controller = new MedicalCasePrintController(_facade, _mapper, _logger);
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

    #region RecordPrintCompleted - 记录打印完成

    [Fact]
    public async Task RecordPrintCompleted_WhenExists_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintCompletedRequest
        {
            PrintType = PrintType.Prescription,
            PrinterName = "TestPrinter"
        };

        var updatedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = operatorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            IsPrinted = true,
            PrintCount = 1,
            PrintVersion = 2,
            LastPrintedAt = DateTime.UtcNow
        };

        _facade.RecordPrintCompletedAsync(
            id, 
            request.PrintType, 
            operatorId, 
            "TestDoctor", 
            request.PrinterName)
            .Returns(updatedEntity);

        // Act
        var result = await _controller.RecordPrintCompleted(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("打印记录更新成功");
        response.Data!.IsPrinted.Should().BeTrue();
        response.Data.PrintCount.Should().Be(1);
        response.Data.PrintVersion.Should().Be(2);
    }

    [Fact]
    public async Task RecordPrintCompleted_WithFormulaPrintType_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintCompletedRequest
        {
            PrintType = PrintType.Formula,
            PrinterName = "TestPrinter"
        };

        var updatedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = operatorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            IsPrinted = true,
            PrintCount = 3,
            PrintVersion = 4
        };

        _facade.RecordPrintCompletedAsync(
            id, 
            request.PrintType, 
            operatorId, 
            "TestDoctor", 
            request.PrinterName)
            .Returns(updatedEntity);

        // Act
        var result = await _controller.RecordPrintCompleted(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("打印记录更新成功");
    }

    [Fact]
    public async Task RecordPrintCompleted_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintCompletedRequest
        {
            PrintType = PrintType.Prescription
        };

        _facade.RecordPrintCompletedAsync(
            id, 
            request.PrintType, 
            operatorId, 
            "TestDoctor", 
            null)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.RecordPrintCompleted(id, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    [Fact]
    public async Task RecordPrintCompleted_WithoutPrinterName_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintCompletedRequest
        {
            PrintType = PrintType.Prescription,
            PrinterName = null
        };

        var updatedEntity = new MedicalCase
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = operatorId,
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            IsPrinted = true,
            PrintCount = 1,
            PrintVersion = 2,
            LastPrintedAt = DateTime.UtcNow
        };

        _facade.RecordPrintCompletedAsync(
            id, 
            request.PrintType, 
            operatorId, 
            "TestDoctor", 
            null)
            .Returns(updatedEntity);

        // Act
        var result = await _controller.RecordPrintCompleted(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    #endregion

    #region AddPrintLog - 添加打印日志

    [Fact]
    public async Task AddPrintLog_Successful_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintLogInputDto
        {
            PrintType = PrintType.Prescription,
            IsSuccess = true,
            PrinterName = "TestPrinter"
        };

        _facade.AddPrintLogAsync(
            id,
            request.PrintType,
            request.IsSuccess,
            operatorId,
            "TestDoctor",
            request.PrinterName,
            null)
            .Returns(true);

        // Act
        var result = await _controller.AddPrintLog(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("打印日志记录成功");
    }

    [Fact]
    public async Task AddPrintLog_Failed_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintLogInputDto
        {
            PrintType = PrintType.Prescription,
            IsSuccess = false,
            PrinterName = "TestPrinter",
            ErrorMessage = "Out of paper"
        };

        _facade.AddPrintLogAsync(
            id,
            request.PrintType,
            request.IsSuccess,
            operatorId,
            "TestDoctor",
            request.PrinterName,
            request.ErrorMessage)
            .Returns(true);

        // Act
        var result = await _controller.AddPrintLog(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("打印日志记录成功");
    }

    [Fact]
    public async Task AddPrintLog_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintLogInputDto
        {
            PrintType = PrintType.Prescription,
            IsSuccess = true
        };

        _facade.AddPrintLogAsync(
            id,
            request.PrintType,
            request.IsSuccess,
            operatorId,
            "TestDoctor",
            null,
            null)
            .Returns(false);

        // Act
        var result = await _controller.AddPrintLog(id, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    [Fact]
    public async Task AddPrintLog_WithFormulaPrintType_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var operatorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new PrintLogInputDto
        {
            PrintType = PrintType.Formula,
            IsSuccess = true,
            PrinterName = "FormulaPrinter"
        };

        _facade.AddPrintLogAsync(
            id,
            request.PrintType,
            request.IsSuccess,
            operatorId,
            "TestDoctor",
            request.PrinterName,
            null)
            .Returns(true);

        // Act
        var result = await _controller.AddPrintLog(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    #endregion
}
