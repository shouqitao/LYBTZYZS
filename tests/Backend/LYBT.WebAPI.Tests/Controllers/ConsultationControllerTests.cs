using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.WebAPI.Controllers;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// 看诊控制器单元测试
    /// </summary>
    public class ConsultationControllerTests
    {
        private readonly Mock<IConsultationService> _mockConsultationService;
        private readonly Mock<ILogger<ConsultationController>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly ConsultationController _controller;

        public ConsultationControllerTests()
        {
            _mockConsultationService = new Mock<IConsultationService>();
            _mockLogger = new Mock<ILogger<ConsultationController>>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new ConsultationController(
                _mockConsultationService.Object,
                _mockLogger.Object,
                _mockCache.Object);
        }

        #region GetConsultations Tests (改进后的分页查询)

        [Fact]
        public async Task GetConsultations_ReturnsOkResult_WithPagedData()
        {
            // Arrange
            var pagedResult = new PagedResult<ConsultationDto>
            {
                Data = new[] { new ConsultationDto() },
                TotalCount = 1,
                PageIndex = 1,
                PageSize = 10
            };
            _mockConsultationService.Setup(x => x.GetPagedAsync(It.IsAny<ConsultationPagedQueryDto>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetConsultations();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().Be(pagedResult);
        }

        [Fact]
        public async Task GetConsultations_WithParameters_PassesCorrectQuery()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            var status = 1;

            ConsultationPagedQueryDto capturedQuery = null;
            _mockConsultationService.Setup(x => x.GetPagedAsync(It.IsAny<ConsultationPagedQueryDto>()))
                .Callback<ConsultationPagedQueryDto>(q => capturedQuery = q)
                .ReturnsAsync(new PagedResult<ConsultationDto>());

            // Act
            await _controller.GetConsultations(
                page: 2,
                pageSize: 20,
                keyword: "test",
                doctorId: doctorId,
                patientId: patientId,
                startDate: startDate,
                endDate: endDate,
                status: status);

            // Assert
            capturedQuery.Should().NotBeNull();
            capturedQuery.CurrentPage.Should().Be(2);
            capturedQuery.PageSize.Should().Be(20);
            capturedQuery.SearchKeyword.Should().Be("test");
            capturedQuery.DoctorId.Should().Be(doctorId);
            capturedQuery.PatientId.Should().Be(patientId);
            capturedQuery.StartDate.Should().Be(startDate);
            capturedQuery.EndDate.Should().Be(endDate);
            capturedQuery.Status.Should().Be(status);
        }

        [Fact]
        public async Task GetConsultations_WhenExceptionThrown_ReturnsProblemDetails()
        {
            // Arrange
            _mockConsultationService.Setup(x => x.GetPagedAsync(It.IsAny<ConsultationPagedQueryDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetConsultations();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            objectResult?.Value.Should().BeOfType<ProblemDetails>();
        }

        #endregion

        #region UpdateStatus Tests (新增的状态更新接口)

        [Fact]
        public async Task UpdateStatus_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateStatusDto { Status = 2, Reason = "已完成诊断" };
            var updatedConsultation = new ConsultationDetailDto { Id = id };
            
            _mockConsultationService.Setup(x => x.UpdateStatusAsync(id, dto.Status, dto.Reason))
                .ReturnsAsync(updatedConsultation);

            // Act
            var result = await _controller.UpdateStatus(id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().Be(updatedConsultation);
        }

        [Fact]
        public async Task UpdateStatus_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateStatusDto();
            _controller.ModelState.AddModelError("Status", "Status is required");

            // Act
            var result = await _controller.UpdateStatus(id, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateStatus_InvalidOperation_ReturnsProblemDetails()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateStatusDto { Status = 2 };
            _mockConsultationService.Setup(x => x.UpdateStatusAsync(id, dto.Status, dto.Reason))
                .ThrowsAsync(new InvalidOperationException("无效的状态转换"));

            // Act
            var result = await _controller.UpdateStatus(id, dto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            var problemDetails = objectResult?.Value as ProblemDetails;
            problemDetails?.Title.Should().Be("状态更新失败");
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_ExistingId_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var consultation = new ConsultationDetailDto { Id = id };
            _mockConsultationService.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(consultation);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().Be(consultation);
        }

        [Fact]
        public async Task GetById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockConsultationService.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((ConsultationDetailDto)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region StartConsultation Tests

        [Fact]
        public async Task StartConsultation_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var dto = new ConsultationStartDto { PatientId = Guid.NewGuid() };
            var consultation = new ConsultationDetailDto();
            _mockConsultationService.Setup(x => x.StartConsultationAsync(dto))
                .ReturnsAsync(consultation);

            // Act
            var result = await _controller.StartConsultation(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region CompleteConsultation Tests

        [Fact]
        public async Task CompleteConsultation_Success_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationCompleteDto();
            _mockConsultationService.Setup(x => x.CompleteConsultationAsync(id, dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CompleteConsultation(id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().BeEquivalentTo(new { message = "看诊完成" });
        }

        [Fact]
        public async Task CompleteConsultation_Failure_ReturnsProblemDetails()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationCompleteDto();
            _mockConsultationService.Setup(x => x.CompleteConsultationAsync(id, dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CompleteConsultation(id, dto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Success_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockConsultationService.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().BeEquivalentTo(new { message = "删除成功" });
        }

        [Fact]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockConsultationService.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task AllEndpoints_ReturnProblemDetails_OnException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var errorMessage = "Unexpected error";
            
            _mockConsultationService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception(errorMessage));
            _mockConsultationService.Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception(errorMessage));
            _mockConsultationService.Setup(x => x.GetTodayConsultationsByDoctorAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception(errorMessage));

            // Act & Assert - GetById
            var getByIdResult = await _controller.GetById(id);
            getByIdResult.Should().BeOfType<ObjectResult>();
            (getByIdResult as ObjectResult)?.Value.Should().BeOfType<ProblemDetails>();

            // Act & Assert - Delete
            var deleteResult = await _controller.Delete(id);
            deleteResult.Should().BeOfType<ObjectResult>();
            (deleteResult as ObjectResult)?.Value.Should().BeOfType<ProblemDetails>();

            // Act & Assert - GetTodayConsultationsByDoctor
            var todayResult = await _controller.GetTodayConsultationsByDoctor(id);
            todayResult.Should().BeOfType<ObjectResult>();
            (todayResult as ObjectResult)?.Value.Should().BeOfType<ProblemDetails>();
        }

        #endregion
    }
}