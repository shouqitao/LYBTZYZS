using System.Security.Claims;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Server.Unit.Controllers;

/// <summary>
/// MedicalCasesController 单元测试
/// 测试 CRUD 操作（12个方法）
/// </summary>
public class MedicalCasesControllerTests
{
    private readonly IMedicalCaseFacade _facade;
    private readonly MedicalCaseMapper _mapper;
    private readonly ILogger<MedicalCasesController> _logger;
    private readonly MedicalCasesController _controller;

    public MedicalCasesControllerTests()
    {
        _facade = Substitute.For<IMedicalCaseFacade>();
        _mapper = new MedicalCaseMapper();
        _logger = Substitute.For<ILogger<MedicalCasesController>>();
        _controller = new MedicalCasesController(_facade, _mapper, _logger);
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

    #region CreateMedicalCase - 创建医案

    [Fact]
    public async Task CreateMedicalCase_WithValidInput_ReturnsOkWithCreatedData()
    {
        // Arrange
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var input = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = doctorId,
            Remark = "Test remark"
        };

        var createdEntity = new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = input.PatientId,
            UserId = doctorId,
            PatientName = "TestPatient",
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _facade.SaveAsync(Arg.Is<MedicalCaseInputDto>(x => x.PatientId == input.PatientId && x.Id == null), doctorId, false)
            .Returns(createdEntity);

        // Act
        var result = await _controller.CreateMedicalCase(input);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("医案创建成功");
        response.Data.Should().NotBeNull();
        response.Data!.PatientId.Should().Be(input.PatientId);
    }

    [Fact]
    public async Task CreateMedicalCase_WhenPatientNotFound_ReturnsNotFound()
    {
        // Arrange
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var input = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = doctorId
        };

        _facade.SaveAsync(Arg.Any<MedicalCaseInputDto>(), doctorId, false)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.CreateMedicalCase(input);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("患者不存在");
    }

    #endregion

    #region SetPrescriptionFlag - 设置处方标记

    [Fact]
    public async Task SetPrescriptionFlag_WithValidRequest_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new SetPrescriptionFlagRequest { NeedsPrescription = true };

        var updatedEntity = new MedicalCase
        {
            Id = id,
            NeedsPrescription = true,
            PatientId = Guid.NewGuid(),
            PatientName = "TestPatient",
            UserId = Guid.NewGuid(),
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active
        };

        _facade.SetPrescriptionFlagAsync(id, request.NeedsPrescription, Arg.Any<Guid>(), Arg.Any<bool>())
            .Returns(updatedEntity);

        // Act
        var result = await _controller.SetPrescriptionFlag(id, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("处方标记更新成功");
    }

    [Fact]
    public async Task SetPrescriptionFlag_WhenMedicalCaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new SetPrescriptionFlagRequest { NeedsPrescription = false };

        _facade.SetPrescriptionFlagAsync(id, request.NeedsPrescription, Arg.Any<Guid>(), Arg.Any<bool>())
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.SetPrescriptionFlag(id, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region Save - 保存医案聚合根

    [Fact]
    public async Task Save_WithMatchingIds_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var input = new MedicalCaseInputDto
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            UserId = doctorId,
            Remark = "Updated remark"
        };

        var updatedEntity = new MedicalCase
        {
            Id = id,
            PatientId = input.PatientId,
            UserId = doctorId,
            PatientName = "TestPatient",
            DoctorName = "TestDoctor",
            CaseStatus = MedicalCaseStatus.Active,
            Remark = input.Remark
        };

        _facade.SaveAsync(Arg.Is<MedicalCaseInputDto>(x => x.Id == id), doctorId, false)
            .Returns(updatedEntity);

        // Act
        var result = await _controller.Save(id, input);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("保存成功");
        response.Data!.Remark.Should().Be("Updated remark");
    }

    [Fact]
    public async Task Save_WhenIdsDoNotMatch_ReturnsBadRequest()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();
        var input = new MedicalCaseInputDto
        {
            Id = bodyId,
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        // Act
        var result = await _controller.Save(routeId, input);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("请求ID与路由ID不一致");
    }

    [Fact]
    public async Task Save_WhenMedicalCaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var input = new MedicalCaseInputDto
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            UserId = doctorId
        };

        _facade.SaveAsync(Arg.Any<MedicalCaseInputDto>(), doctorId, false)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.Save(id, input);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region DeleteMedicalCase - 删除医案

    [Fact]
    public async Task DeleteMedicalCase_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _facade.DeleteAsync(id, doctorId, false)
            .Returns(true);

        // Act
        var result = await _controller.DeleteMedicalCase(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteMedicalCase_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _facade.DeleteAsync(id, doctorId, false)
            .Returns(false);

        // Act
        var result = await _controller.DeleteMedicalCase(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region BatchDelete - 批量删除

    [Fact]
    public async Task BatchDelete_WithValidIds_ReturnsOkWithResult()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var dto = new BatchDeleteInputDto { Ids = ids };
        var doctorId = Guid.Parse(_controller.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var operationResult = Result<BatchOperationResultDto>.Success(new BatchOperationResultDto
        {
            TotalCount = 2,
            SuccessCount = 2,
            FailureCount = 0,
            Message = "成功删除2个医案"
        });

        _facade.BatchDeleteAsync(ids, doctorId, false)
            .Returns(operationResult);

        // Act
        var result = await _controller.BatchDelete(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<BatchOperationResultDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task BatchDelete_WithEmptyIds_ReturnsBadRequest()
    {
        // Arrange
        var dto = new BatchDeleteInputDto { Ids = new List<Guid>() };

        // Act
        var result = await _controller.BatchDelete(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("请至少选择一个医案");
    }

    #endregion

    #region GetBatchDetails - 批量获取详情

    [Fact]
    public async Task GetBatchDetails_WithValidIds_ReturnsOkWithDetails()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var dto = new BatchDetailQueryDto { Ids = ids };

        var entities = new List<MedicalCase>
        {
            new()
            {
                Id = ids[0],
                PatientId = Guid.NewGuid(),
                PatientName = "Patient1",
                UserId = Guid.NewGuid(),
                DoctorName = "Doctor1",
                CaseStatus = MedicalCaseStatus.Active
            },
            new()
            {
                Id = ids[1],
                PatientId = Guid.NewGuid(),
                PatientName = "Patient2",
                UserId = Guid.NewGuid(),
                DoctorName = "Doctor2",
                CaseStatus = MedicalCaseStatus.Completed
            }
        };

        _facade.GetBatchAsync(ids)
            .Returns(entities);

        // Act
        var result = await _controller.GetBatchDetails(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<MedicalCaseDetailDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBatchDetails_WithEmptyIds_ReturnsBadRequest()
    {
        // Arrange
        var dto = new BatchDetailQueryDto { Ids = new List<Guid>() };

        // Act
        var result = await _controller.GetBatchDetails(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    #endregion

    #region GetById - 获取单个医案详情

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithDetail()
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
            CaseStatus = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _facade.GetByIdAsync(id)
            .Returns(entity);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("查询成功");
        response.Data!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _facade.GetByIdAsync(id)
            .Returns((MedicalCase?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<MedicalCaseDetailDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("医案不存在");
    }

    #endregion

    #region GetList - 查询列表（分页）

    [Fact]
    public async Task GetList_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var pagedResult = new PagedResult<MedicalCaseListDto>
        {
            Items = new List<MedicalCaseListDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientName = "Patient1",
                    DoctorName = "Doctor1",
                    CaseStatus = MedicalCaseStatus.Active
                }
            },
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _facade.GetListDtoAsync(
            Arg.Any<MedicalCaseStatus?>(),
            Arg.Any<Guid?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<Guid?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>())
            .Returns(pagedResult);

        // Act
        var result = await _controller.GetList();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PagedResult<MedicalCaseListDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetList_WithInvalidParameters_ReturnsBadRequest(int page, int pageSize)
    {
        // Act
        var result = await _controller.GetList(page: page, pageSize: pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse<PagedResult<MedicalCaseListDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    #endregion

    #region GetMedicalCases - 统一查询端点

    [Fact]
    public async Task GetMedicalCases_WithValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var query = new MedicalCaseQueryDto
        {
            QueryType = MedicalCaseQueryType.All,
            PageIndex = 1,
            PageSize = 20
        };

        var pagedResult = new PagedResult<MedicalCaseListDto>
        {
            Items = new List<MedicalCaseListDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientName = "Patient1",
                    DoctorName = "Doctor1",
                    CaseStatus = MedicalCaseStatus.Active
                }
            },
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _facade.QueryAsync(Arg.Any<MedicalCaseQueryDto>())
            .Returns(pagedResult);

        // Act
        var result = await _controller.GetMedicalCases(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PagedResult<MedicalCaseListDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.TotalCount.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetMedicalCases_WithInvalidPagination_ReturnsBadRequest(int pageIndex, int pageSize)
    {
        // Arrange
        var query = new MedicalCaseQueryDto
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        // Act
        var result = await _controller.GetMedicalCases(query);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse<PagedResult<MedicalCaseListDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    #endregion

    #region SearchMedicalCases - 跨医案搜索

    [Fact]
    public async Task SearchMedicalCases_WithValidParameters_ReturnsOkWithResults()
    {
        // Arrange
        var pagedResult = new PagedResult<MedicalCaseDetailDto>
        {
            Items = new List<MedicalCaseDetailDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientName = "TestPatient",
                    DoctorName = "TestDoctor"
                }
            },
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _facade.SearchMedicalCasesAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int>(),
            Arg.Any<int>())
            .Returns(pagedResult);

        // Act
        var result = await _controller.SearchMedicalCases(patientName: "Test");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PagedResult<MedicalCaseDetailDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Be("搜索成功");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task SearchMedicalCases_WithInvalidParameters_ReturnsBadRequest(int page, int pageSize)
    {
        // Act
        var result = await _controller.SearchMedicalCases(page: page, pageSize: pageSize);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value as ApiResponse<PagedResult<MedicalCaseDetailDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    #endregion

    #region GetConsultationList - 查询辨证记录列表

    [Fact]
    public async Task GetConsultationList_WithValidId_ReturnsOkWithList()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var consultations = new List<ConsultationDetailDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                TcmDiagnosis = "Test Diagnosis"
            }
        };

        _facade.GetConsultationListAsync(medicalCaseId)
            .Returns(consultations);

        // Act
        var result = await _controller.GetConsultationList(medicalCaseId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<ConsultationDetailDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(1);
    }

    #endregion

    #region GetPrescriptionList - 查询处方列表

    [Fact]
    public async Task GetPrescriptionList_WithValidId_ReturnsOkWithList()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var prescriptions = new List<PrescriptionDetailDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                DosageCount = 3
            }
        };

        _facade.GetPrescriptionListAsync(medicalCaseId)
            .Returns(prescriptions);

        // Act
        var result = await _controller.GetPrescriptionList(medicalCaseId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<PrescriptionDetailDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(1);
    }

    #endregion
}
