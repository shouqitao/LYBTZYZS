using FluentAssertions;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests
{
    public class ConsultationControllerTests
    {
        private readonly ConsultationController _controller;
        private readonly Mock<IConsultationService> _mockService;
        private readonly Mock<ILogger<ConsultationController>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;

        public ConsultationControllerTests()
        {
            _mockService = new Mock<IConsultationService>();
            _mockLogger = new Mock<ILogger<ConsultationController>>();
            _mockCache = new Mock<IMemoryCache>();

            _controller = new ConsultationController(
                _mockService.Object,
                _mockLogger.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task GetConsultations_ShouldReturnOkWithPagedResult()
        {
            // Arrange
            var expectedResult = new PagedResult<ConsultationDto>
            {
                Data = new List<ConsultationDto>
                {
                    new ConsultationDto { Id = Guid.NewGuid(), Diagnosis = "测试诊断" }
                },
                TotalCount = 1,
                PageIndex = 1,
                PageSize = 10
            };

            _mockService.Setup(x => x.GetPagedAsync(It.IsAny<ConsultationPagedQueryDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetConsultations(page: 1, pageSize: 10);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetById_WithExistingId_ShouldReturnOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedResult = new ConsultationDetailDto
            {
                Id = id,
                Diagnosis = "测试诊断",
                PatientName = "测试患者",
                DoctorName = "测试医生"
            };

            _mockService.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetById_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((ConsultationDetailDto?)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "看诊记录不存在" });
        }

        [Fact]
        public async Task StartConsultation_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var dto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            var expectedResult = new ConsultationDetailDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                UserId = dto.UserId
            };

            _mockService.Setup(x => x.StartConsultationAsync(It.IsAny<ConsultationStartDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.StartConsultation(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task StartConsultation_WithInvalidOperation_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _mockService.Setup(x => x.StartConsultationAsync(It.IsAny<ConsultationStartDto>()))
                .ThrowsAsync(new InvalidOperationException("该医疗案例已存在看诊记录"));

            // Act
            var result = await _controller.StartConsultation(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "该医疗案例已存在看诊记录" });
        }

        [Fact]
        public async Task UpdateConsultation_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationUpdateDto
            {
                Diagnosis = "更新的诊断",
                TCMDiagnosis = "中医诊断"
            };

            var expectedResult = new ConsultationDetailDto
            {
                Id = id,
                Diagnosis = dto.Diagnosis,
                TCMDiagnosis = dto.TCMDiagnosis
            };

            _mockService.Setup(x => x.UpdateConsultationAsync(id, It.IsAny<ConsultationUpdateDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UpdateConsultation(id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task CompleteConsultation_WithSuccess_ShouldReturnOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationCompleteDto
            {
                Diagnosis = "最终诊断",
                TCMDiagnosis = "中医诊断",
                TreatmentPrinciple = "治疗原则",
                MedicalAdvice = "医嘱"
            };

            _mockService.Setup(x => x.CompleteConsultationAsync(id, It.IsAny<ConsultationCompleteDto>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CompleteConsultation(id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { message = "看诊完成" });
        }

        [Fact]
        public async Task CompleteConsultation_WithFailure_ShouldReturnBadRequest()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationCompleteDto
            {
                Diagnosis = "最终诊断"
            };

            _mockService.Setup(x => x.CompleteConsultationAsync(id, It.IsAny<ConsultationCompleteDto>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CompleteConsultation(id, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "完成看诊失败" });
        }

        [Fact]
        public async Task GetTodayConsultationsByDoctor_ShouldReturnOk()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = new List<ConsultationDto>
            {
                new ConsultationDto { Id = Guid.NewGuid(), DoctorName = "测试医生" }
            };

            _mockService.Setup(x => x.GetTodayConsultationsByDoctorAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetTodayConsultationsByDoctor(doctorId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetPatientHistory_ShouldReturnOk()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = new List<ConsultationDto>
            {
                new ConsultationDto { Id = Guid.NewGuid(), PatientName = "测试患者" }
            };

            _mockService.Setup(x => x.GetPatientHistoryAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetPatientHistory(patientId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetDoctorConsultationCount_ShouldReturnOk()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedCount = 10;

            _mockService.Setup(x => x.GetDoctorConsultationCountAsync(doctorId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _controller.GetDoctorConsultationCount(doctorId, null, null);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { count = expectedCount });
        }

        [Fact]
        public async Task Delete_WithSuccess_ShouldReturnOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { message = "删除成功" });
        }

        [Fact]
        public async Task Delete_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "看诊记录不存在" });
        }
    }
}