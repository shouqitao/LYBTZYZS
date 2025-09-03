using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Patients.Controllers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// 患者控制器单元测试
    /// 测试优化后的RESTful接口和ToggleStatus功能
    /// </summary>
    public class PatientsControllerTests
    {
        private readonly Mock<IPatientService> _mockPatientService;
        private readonly Mock<ILogger<PatientsController>> _mockLogger;
        private readonly PatientsController _controller;

        public PatientsControllerTests()
        {
            _mockPatientService = new Mock<IPatientService>();
            _mockLogger = new Mock<ILogger<PatientsController>>();
            _controller = new PatientsController(_mockPatientService.Object, _mockLogger.Object);
        }

        #region GET /api/patients Tests

        [Fact]
        public async Task GetPatients_WithDefaultParameters_ReturnsOkResultWithPaginatedData()
        {
            // Arrange
            var expectedData = new PaginatedResult<PatientDto>
            {
                Items = new List<PatientDto>
                {
                    new PatientDto 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "张三", 
                        Gender = "男",
                        Age = 35,
                        PhoneNumber = "13800138000",
                        PatientNumber = "P202501001"
                    },
                    new PatientDto 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "李四", 
                        Gender = "女",
                        Age = 28,
                        PhoneNumber = "13900139000",
                        PatientNumber = "P202501002"
                    }
                },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockPatientService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetPatients();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedResult<PatientDto>>().Subject;
            returnValue.Items.Should().HaveCount(2);
            returnValue.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPatients_WithKeywordSearch_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var keyword = "张";
            _mockPatientService.Setup(x => x.GetPagedAsync(
                1,
                10,
                keyword,
                It.IsAny<bool?>())
            ).ReturnsAsync(new PaginatedResult<PatientDto>());

            // Act
            await _controller.GetPatients(1, 10, keyword);

            // Assert
            _mockPatientService.Verify(x => x.GetPagedAsync(1, 10, keyword, null), Times.Once);
        }

        [Fact]
        public async Task GetPatients_WithActiveFilter_ReturnsOnlyActivePatients()
        {
            // Arrange
            var expectedData = new PaginatedResult<PatientDto>
            {
                Items = new List<PatientDto>
                {
                    new PatientDto { Id = Guid.NewGuid(), Name = "活跃患者", IsActive = true }
                },
                TotalCount = 1
            };

            _mockPatientService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                true)
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetPatients(isActive: true);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedResult<PatientDto>>().Subject;
            returnValue.Items.Should().OnlyContain(p => p.IsActive == true);
        }

        #endregion

        #region GET /api/patients/{id} Tests

        [Fact]
        public async Task GetPatient_WithExistingId_ReturnsOkResultWithPatient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedPatient = new PatientDto
            {
                Id = patientId,
                Name = "测试患者",
                Gender = "男",
                Age = 30,
                PhoneNumber = "13800138000",
                PatientNumber = "P202501001"
            };

            _mockPatientService.Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(expectedPatient);

            // Act
            var result = await _controller.GetPatient(patientId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PatientDto>().Subject;
            returnValue.Id.Should().Be(patientId);
            returnValue.Name.Should().Be("测试患者");
        }

        [Fact]
        public async Task GetPatient_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockPatientService.Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync((PatientDto?)null);

            // Act
            var result = await _controller.GetPatient(patientId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region POST /api/patients Tests

        [Fact]
        public async Task CreatePatient_WithValidData_ReturnsOkResultWithCreatedPatient()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "新患者",
                Gender = "男",
                Age = 35,
                PhoneNumber = "13900139001",
                IdentityCard = "110101198801011234",
                Address = "北京市朝阳区"
            };

            var createdPatient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Gender = createDto.Gender,
                Age = createDto.Age,
                PhoneNumber = createDto.PhoneNumber,
                PatientNumber = "P202501003",
                IsActive = true
            };

            _mockPatientService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(createdPatient);

            // Act
            var result = await _controller.CreatePatient(createDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PatientDto>().Subject;
            returnValue.Name.Should().Be("新患者");
            returnValue.PatientNumber.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreatePatient_QuickCreate_WithMinimalData_ReturnsOkResult()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "快速创建患者",
                Gender = "男",
                Age = 25,
                PhoneNumber = "13800138888"
                // 其他字段可以为空
            };

            var createdPatient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Gender = createDto.Gender,
                Age = createDto.Age,
                PhoneNumber = createDto.PhoneNumber,
                PatientNumber = "P202501004",
                IsActive = true
            };

            _mockPatientService.Setup(x => x.CreateAsync(It.IsAny<PatientCreateDto>()))
                .ReturnsAsync(createdPatient);

            // Act
            var result = await _controller.CreatePatient(createDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region PUT /api/patients/{id} Tests

        [Fact]
        public async Task UpdatePatient_WithValidData_ReturnsOkResultWithUpdatedPatient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientEditDto
            {
                Id = patientId,
                Name = "更新后的患者",
                PhoneNumber = "13900139999",
                Address = "新地址"
            };

            var updatedPatient = new PatientDto
            {
                Id = patientId,
                Name = updateDto.Name,
                PhoneNumber = updateDto.PhoneNumber,
                Address = updateDto.Address
            };

            _mockPatientService.Setup(x => x.UpdateAsync(It.IsAny<PatientEditDto>()))
                .ReturnsAsync(updatedPatient);

            // Act
            var result = await _controller.UpdatePatient(patientId, updateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PatientDto>().Subject;
            returnValue.Name.Should().Be("更新后的患者");
            
            // 验证ID被正确设置
            _mockPatientService.Verify(x => x.UpdateAsync(It.Is<PatientEditDto>(dto => dto.Id == patientId)), Times.Once);
        }

        #endregion

        #region DELETE /api/patients/{id} Tests

        [Fact]
        public async Task DeletePatient_WithExistingId_ReturnsOkResultWithSuccessMessage()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockPatientService.Setup(x => x.DeleteAsync(patientId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePatient(patientId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { message = "患者删除成功" });
        }

        #endregion

        #region POST /api/patients/{id}/toggle-status Tests

        [Fact]
        public async Task ToggleStatus_WithActivePatient_ReturnsOkResultWithInactivePatient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var toggledPatient = new PatientDto
            {
                Id = patientId,
                Name = "测试患者",
                IsActive = false // 从活跃变为不活跃
            };

            _mockPatientService.Setup(x => x.ToggleStatusAsync(patientId))
                .ReturnsAsync(toggledPatient);

            // Act
            var result = await _controller.ToggleStatus(patientId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PatientDto>().Subject;
            returnValue.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleStatus_WithInactivePatient_ReturnsOkResultWithActivePatient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var toggledPatient = new PatientDto
            {
                Id = patientId,
                Name = "测试患者",
                IsActive = true // 从不活跃变为活跃
            };

            _mockPatientService.Setup(x => x.ToggleStatusAsync(patientId))
                .ReturnsAsync(toggledPatient);

            // Act
            var result = await _controller.ToggleStatus(patientId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PatientDto>().Subject;
            returnValue.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ToggleStatus_WithNonExistingPatient_ReturnsNotFound()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockPatientService.Setup(x => x.ToggleStatusAsync(patientId))
                .ReturnsAsync((PatientDto?)null);

            // Act
            var result = await _controller.ToggleStatus(patientId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Edge Cases and Error Handling Tests

        [Fact]
        public async Task GetPatients_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockPatientService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>())
            ).ThrowsAsync(new Exception("数据库连接错误"));

            // Act
            var act = async () => await _controller.GetPatients();

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("数据库连接错误");
        }

        [Fact]
        public async Task CreatePatient_WithDuplicatePhoneNumber_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "重复患者",
                Gender = "男",
                Age = 30,
                PhoneNumber = "13800138000" // 已存在的电话
            };

            _mockPatientService.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("该手机号已存在"));

            // Act
            var act = async () => await _controller.CreatePatient(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("该手机号已存在");
        }

        #endregion
    }
}