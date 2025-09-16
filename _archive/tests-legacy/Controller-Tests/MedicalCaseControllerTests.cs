using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using LYBT.WebAPI.Controllers;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using FluentAssertions;
using System.Collections.Generic;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// MedicalCaseController 单元测试
    /// </summary>
    public class MedicalCaseControllerTests
    {
        private readonly Mock<IMedicalCaseService> _medicalCaseServiceMock;
        private readonly Mock<ILogger<MedicalCaseController>> _loggerMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly MedicalCaseController _controller;

        public MedicalCaseControllerTests()
        {
            _medicalCaseServiceMock = new Mock<IMedicalCaseService>();
            _loggerMock = new Mock<ILogger<MedicalCaseController>>();
            _cacheMock = new Mock<IMemoryCache>();

            _controller = new MedicalCaseController(
                _medicalCaseServiceMock.Object,
                _loggerMock.Object,
                _cacheMock.Object
            );
        }

        #region GetPaged Tests

        [Fact]
        public async Task GetPaged_WithDefaultParameters_ShouldReturnPagedData()
        {
            // Arrange
            var expectedResult = new PaginatedResult<MedicalCaseDetailDto>
            {
                Items = new List<MedicalCaseDetailDto>
                {
                    new() { Id = Guid.NewGuid(), PatientName = "张三", CaseNumber = "MC202501001" },
                    new() { Id = Guid.NewGuid(), PatientName = "李四", CaseNumber = "MC202501002" }
                },
                TotalCount = 10,
                PageIndex = 1,
                PageSize = 20
            };

            _medicalCaseServiceMock.Setup(x => x.GetPagedAsync(1, 20))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetPaged();

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var pagedResult = okResult.Value as PaginatedResult<MedicalCaseDetailDto>;
            pagedResult.Should().NotBeNull();
            pagedResult!.Items.Should().HaveCount(2);
            pagedResult.TotalCount.Should().Be(10);
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(2, 20)]
        [InlineData(5, 50)]
        public async Task GetPaged_WithCustomParameters_ShouldReturnCorrectPage(int pageIndex, int pageSize)
        {
            // Arrange
            var expectedResult = new PaginatedResult<MedicalCaseDetailDto>
            {
                Items = new List<MedicalCaseDetailDto>(),
                TotalCount = 100,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            _medicalCaseServiceMock.Setup(x => x.GetPagedAsync(pageIndex, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetPaged(pageIndex, pageSize);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();

            var pagedResult = okResult!.Value as PaginatedResult<MedicalCaseDetailDto>;
            pagedResult!.PageIndex.Should().Be(pageIndex);
            pagedResult.PageSize.Should().Be(pageSize);
        }

        [Fact]
        public async Task GetPaged_WhenServiceThrowsException_ShouldReturnBadRequest()
        {
            // Arrange
            _medicalCaseServiceMock.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("数据库查询错误"));

            // Act
            var result = await _controller.GetPaged();

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidId_ShouldReturnMedicalCase()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedMedicalCase = new MedicalCaseDetailDto
            {
                Id = id,
                PatientId = Guid.NewGuid(),
                PatientName = "王五",
                CaseNumber = "MC202501003",
                Status = MedicalCaseStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _medicalCaseServiceMock.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(expectedMedicalCase);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var medicalCase = okResult.Value as MedicalCaseDetailDto;
            medicalCase.Should().NotBeNull();
            medicalCase!.Id.Should().Be(id);
            medicalCase.PatientName.Should().Be("王五");
        }

        [Fact]
        public async Task GetById_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _medicalCaseServiceMock.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseDetailDto?)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().NotBeNull();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        #endregion

        #region Create Tests

        [Fact]
        public async Task Create_WithValidData_ShouldReturnCreatedMedicalCase()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                RegistrationId = Guid.NewGuid(),
                ChiefComplaint = "头痛、头晕",
                PresentIllness = "患者近3天出现头痛、头晕症状",
                PatientName = "赵六"
            };

            var createdMedicalCase = new MedicalCaseDetailDto
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                PatientName = createDto.PatientName!,
                CaseNumber = "MC202501004",
                ChiefComplaint = createDto.ChiefComplaint,
                PresentIllness = createDto.PresentIllness,
                Status = MedicalCaseStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _medicalCaseServiceMock.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(createdMedicalCase);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var medicalCase = okResult.Value as MedicalCaseDetailDto;
            medicalCase.Should().NotBeNull();
            medicalCase!.PatientName.Should().Be("赵六");
            medicalCase.ChiefComplaint.Should().Be(createDto.ChiefComplaint);
        }

        [Fact]
        public async Task Create_WithNullData_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.Create(null!);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Create_WhenServiceThrowsException_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                ChiefComplaint = "测试"
            };

            _medicalCaseServiceMock.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("患者不存在"));

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new MedicalCaseEditDto
            {
                ChiefComplaint = "更新后的主诉",
                PresentIllness = "更新后的现病史",
                PastHistory = "既往史：无特殊"
            };

            _medicalCaseServiceMock.Setup(x => x.UpdateAsync(id, updateDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(id, updateDto);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Update_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new MedicalCaseEditDto();

            _medicalCaseServiceMock.Setup(x => x.UpdateAsync(id, updateDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(id, updateDto);

            // Assert
            result.Should().NotBeNull();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Update_WithNullData_ShouldReturnBadRequest()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.Update(id, null!);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        #endregion

        #region GetByPatientId Tests

        [Fact]
        public async Task GetByPatientId_WithValidId_ShouldReturnMedicalCases()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedCases = new List<MedicalCaseDetailDto>
            {
                new() { Id = Guid.NewGuid(), PatientId = patientId, PatientName = "孙七", CaseNumber = "MC202501005" },
                new() { Id = Guid.NewGuid(), PatientId = patientId, PatientName = "孙七", CaseNumber = "MC202501006" }
            };

            _medicalCaseServiceMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedCases);

            // Act
            var result = await _controller.GetByPatientId(patientId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var cases = okResult.Value as List<MedicalCaseDetailDto>;
            cases.Should().NotBeNull();
            cases!.Should().HaveCount(2);
            cases.All(c => c.PatientId == patientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetByPatientId_WithNoData_ShouldReturnEmptyList()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _medicalCaseServiceMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseDetailDto>());

            // Act
            var result = await _controller.GetByPatientId(patientId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();

            var cases = okResult!.Value as List<MedicalCaseDetailDto>;
            cases.Should().NotBeNull();
            cases!.Should().BeEmpty();
        }

        #endregion

        #region GetTodayByUserId Tests

        [Fact]
        public async Task GetTodayByUserId_WithValidId_ShouldReturnTodaysCases()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedCases = new List<MedicalCaseDetailDto>
            {
                new() 
                { 
                    Id = Guid.NewGuid(), 
                    PatientName = "周八", 
                    CaseNumber = "MC202501007",
                    CreatedAt = DateTime.Today.AddHours(9),
                    CreatedBy = userId
                }
            };

            _medicalCaseServiceMock.Setup(x => x.GetTodayByUserIdAsync(userId))
                .ReturnsAsync(expectedCases);

            // Act
            var result = await _controller.GetTodayByUserId(userId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var cases = okResult.Value as List<MedicalCaseDetailDto>;
            cases.Should().NotBeNull();
            cases!.Should().HaveCount(1);
            cases[0].CreatedBy.Should().Be(userId);
        }

        #endregion

        #region UpdateStatus Tests

        [Fact]
        public async Task UpdateStatus_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var id = Guid.NewGuid();
            var newStatus = MedicalCaseStatus.Completed;

            _medicalCaseServiceMock.Setup(x => x.UpdateStatusAsync(id, newStatus))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateStatus(id, newStatus);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task UpdateStatus_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var newStatus = MedicalCaseStatus.Processing;

            _medicalCaseServiceMock.Setup(x => x.UpdateStatusAsync(id, newStatus))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateStatus(id, newStatus);

            // Assert
            result.Should().NotBeNull();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Theory]
        [InlineData(MedicalCaseStatus.Pending)]
        [InlineData(MedicalCaseStatus.Processing)]
        [InlineData(MedicalCaseStatus.Completed)]
        [InlineData(MedicalCaseStatus.Cancelled)]
        public async Task UpdateStatus_WithDifferentStatuses_ShouldCallServiceCorrectly(MedicalCaseStatus status)
        {
            // Arrange
            var id = Guid.NewGuid();
            _medicalCaseServiceMock.Setup(x => x.UpdateStatusAsync(id, status))
                .ReturnsAsync(true);

            // Act
            await _controller.UpdateStatus(id, status);

            // Assert
            _medicalCaseServiceMock.Verify(x => x.UpdateStatusAsync(id, status), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var id = Guid.NewGuid();
            _medicalCaseServiceMock.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Delete_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _medicalCaseServiceMock.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().NotBeNull();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Delete_WhenServiceThrowsException_ShouldReturnBadRequest()
        {
            // Arrange
            var id = Guid.NewGuid();
            _medicalCaseServiceMock.Setup(x => x.DeleteAsync(id))
                .ThrowsAsync(new InvalidOperationException("无法删除已完成的案例"));

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task AllEndpoints_WhenUnexpectedExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var exception = new Exception("未预期的错误");
            _medicalCaseServiceMock.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(exception);

            // Act
            await _controller.GetPaged();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}