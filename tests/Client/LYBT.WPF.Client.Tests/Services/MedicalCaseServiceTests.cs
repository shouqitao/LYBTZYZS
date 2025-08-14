using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 医疗案例服务前端单元测试
    /// 测试医疗案例管理的核心功能
    /// </summary>
    public class MedicalCaseServiceTests
    {
        private readonly Mock<IMedicalCaseApiService> _mockApiService;
        private readonly MedicalCaseService _service;

        public MedicalCaseServiceTests()
        {
            _mockApiService = new Mock<IMedicalCaseApiService>();
            _service = new MedicalCaseService(_mockApiService.Object);
        }

        #region Test Data Factory Methods

        private MedicalCaseDto CreateTestMedicalCaseDto(Guid? id = null)
        {
            return new MedicalCaseDto
            {
                Id = id ?? Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生",
                DiagnosisSummary = "风寒感冒",
                Status = "Registered",
                CreateTime = DateTime.Now,
                CompleteTime = null
            };
        }

        private MedicalCaseDetailDto CreateTestMedicalCaseDetailDto(Guid? id = null)
        {
            return new MedicalCaseDetailDto
            {
                Id = id ?? Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                Status = MedicalCaseStatus.Registered,
                CreateTime = DateTime.Now,
                UpdateTime = null,
                CompleteTime = null,
                Remark = "测试备注",
                Consultation = null
            };
        }

        private MedicalCaseCreateDto CreateTestMedicalCaseCreateDto()
        {
            return new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                RegistrationId = Guid.NewGuid(),
                Remark = "新建案例"
            };
        }

        private MedicalCaseEditDto CreateTestMedicalCaseEditDto(Guid? id = null)
        {
            return new MedicalCaseEditDto
            {
                Id = id ?? Guid.NewGuid(),
                Status = MedicalCaseStatus.InConsultation,
                Remark = "更新备注",
                CompleteTime = DateTime.Now
            };
        }

        private PaginatedResult<MedicalCaseDto> CreateTestPaginatedResult()
        {
            return new PaginatedResult<MedicalCaseDto>
            {
                Items = new List<MedicalCaseDto> { CreateTestMedicalCaseDto() },
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };
        }

        /// <summary>
        /// 创建成功的 ApiResponse
        /// </summary>
        private ApiResponse<T> CreateSuccessApiResponse<T>(T content)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return new ApiResponse<T>(response, content, new RefitSettings());
        }

        /// <summary>
        /// 创建失败的 ApiResponse
        /// </summary>
        private ApiResponse<T> CreateFailureApiResponse<T>()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            return new ApiResponse<T>(response, default(T), new RefitSettings());
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResult()
        {
            // Arrange
            const int pageIndex = 1;
            const int pageSize = 20;
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(pageIndex, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task GetPagedAsync_WhenApiCallFails_ReturnsEmptyResultWithError()
        {
            // Arrange
            const int pageIndex = 1;
            const int pageSize = 20;
            var apiResponse = CreateFailureApiResponse<PaginatedResult<MedicalCaseDto>>();

            _mockApiService
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(pageIndex, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.CurrentPage.Should().Be(pageIndex);
            result.PageSize.Should().Be(pageSize);
            result.ErrorMessage.Should().Be("获取医疗案例失败");
        }

        [Fact]
        public async Task GetPagedAsync_WhenExceptionThrown_ReturnsEmptyResultWithError()
        {
            // Arrange
            const int pageIndex = 1;
            const int pageSize = 20;

            _mockApiService
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.GetPagedAsync(pageIndex, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.ErrorMessage.Should().Contain("分页查询医疗案例时发生错误");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsDetailDto()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detailDto = CreateTestMedicalCaseDetailDto(medicalCaseId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(medicalCaseId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(medicalCaseId);
            result.Data.PatientName.Should().Be(detailDto.PatientName);
        }

        [Fact]
        public async Task GetByIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("获取失败"));

            // Act
            var result = await _service.GetByIdAsync(medicalCaseId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDto_ReturnsMedicalCaseInfo()
        {
            // Arrange
            var createDto = CreateTestMedicalCaseCreateDto();
            var createdDto = CreateTestMedicalCaseDto();
            var apiResponse = CreateSuccessApiResponse(createdDto);

            _mockApiService
                .Setup(x => x.CreateAsync(It.IsAny<MedicalCaseCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(createdDto.Id);
            result.Data.PatientName.Should().Be(createdDto.PatientName);
            result.Data.DoctorName.Should().Be(createdDto.DoctorName);
            _mockApiService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestMedicalCaseCreateDto();

            _mockApiService
                .Setup(x => x.CreateAsync(It.IsAny<MedicalCaseCreateDto>()))
                .ThrowsAsync(new Exception("创建失败"));

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidDto_ReturnsSuccess()
        {
            // Arrange
            var editDto = CreateTestMedicalCaseEditDto();
            var apiResponse = CreateSuccessApiResponse(true);

            _mockApiService
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<MedicalCaseEditDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateAsync(editDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.UpdateAsync(editDto.Id, editDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var editDto = CreateTestMedicalCaseEditDto();

            _mockApiService
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<MedicalCaseEditDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateAsync(editDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_WithValidPatientId_ReturnsMedicalCaseList()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var medicalCaseList = new List<MedicalCaseDto> { CreateTestMedicalCaseDto(), CreateTestMedicalCaseDto() };
            var apiResponse = CreateSuccessApiResponse(medicalCaseList);

            _mockApiService
                .Setup(x => x.GetByPatientIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            _mockApiService.Verify(x => x.GetByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetByPatientIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.GetByPatientIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("获取患者案例失败"));

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetTodayByUserIdAsync Tests

        [Fact]
        public async Task GetTodayByUserIdAsync_WithValidUserId_ReturnsTodayMedicalCaseList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var medicalCaseList = new List<MedicalCaseDto> { CreateTestMedicalCaseDto() };
            var apiResponse = CreateSuccessApiResponse(medicalCaseList);

            _mockApiService
                .Setup(x => x.GetTodayByUserIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetTodayByUserIdAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            _mockApiService.Verify(x => x.GetTodayByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetTodayByUserIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.GetTodayByUserIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("获取今日案例失败"));

            // Act
            var result = await _service.GetTodayByUserIdAsync(userId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            const MedicalCaseStatus newStatus = MedicalCaseStatus.InConsultation;
            var apiResponse = CreateSuccessApiResponse(true);

            _mockApiService
                .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<MedicalCaseStatus>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateStatusAsync(medicalCaseId, newStatus);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.UpdateStatusAsync(medicalCaseId, newStatus), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            const MedicalCaseStatus newStatus = MedicalCaseStatus.Completed;

            _mockApiService
                .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<MedicalCaseStatus>()))
                .ThrowsAsync(new Exception("状态更新失败"));

            // Act
            var result = await _service.UpdateStatusAsync(medicalCaseId, newStatus);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(true);

            _mockApiService
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteAsync(medicalCaseId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.DeleteAsync(medicalCaseId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("删除失败"));

            // Act
            var result = await _service.DeleteAsync(medicalCaseId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region ConvertToMedicalCaseInfo Tests

        [Fact]
        public async Task ConvertToMedicalCaseInfo_ConvertsPropertiesCorrectly()
        {
            // Arrange
            var dto = CreateTestMedicalCaseDto();
            var apiResponse = CreateSuccessApiResponse(dto);

            _mockApiService
                .Setup(x => x.CreateAsync(It.IsAny<MedicalCaseCreateDto>()))
                .ReturnsAsync(apiResponse);

            var createDto = CreateTestMedicalCaseCreateDto();

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert - 验证转换逻辑
            result.IsSuccess.Should().BeTrue();
            var medicalCaseInfo = result.Data;
            medicalCaseInfo.Should().NotBeNull();
            medicalCaseInfo.Id.Should().Be(dto.Id);
            medicalCaseInfo.PatientName.Should().Be(dto.PatientName);
            medicalCaseInfo.DoctorName.Should().Be(dto.DoctorName);
            medicalCaseInfo.UserId.Should().Be(dto.DoctorId); // DoctorId映射到UserId
            medicalCaseInfo.CreateTime.Should().Be(dto.CreateTime);
            medicalCaseInfo.IsSelected.Should().BeFalse(); // 默认值
            medicalCaseInfo.IsActive.Should().BeTrue(); // 默认值
        }

        [Fact]
        public async Task ConvertToMedicalCaseInfo_ParsesStatusCorrectly()
        {
            // Arrange - 测试不同状态字符串的解析
            var dto = CreateTestMedicalCaseDto();
            dto.Status = "InConsultation";
            var apiResponse = CreateSuccessApiResponse(dto);

            _mockApiService
                .Setup(x => x.CreateAsync(It.IsAny<MedicalCaseCreateDto>()))
                .ReturnsAsync(apiResponse);

            var createDto = CreateTestMedicalCaseCreateDto();

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be(MedicalCaseStatus.InConsultation);
        }

        [Fact]
        public async Task ConvertToMedicalCaseInfo_HandlesEmptyStatusCorrectly()
        {
            // Arrange - 测试空状态字符串的处理
            var dto = CreateTestMedicalCaseDto();
            dto.Status = "";
            var apiResponse = CreateSuccessApiResponse(dto);

            _mockApiService
                .Setup(x => x.CreateAsync(It.IsAny<MedicalCaseCreateDto>()))
                .ReturnsAsync(apiResponse);

            var createDto = CreateTestMedicalCaseCreateDto();

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Status.Should().Be(MedicalCaseStatus.Registered); // 默认状态
        }

        #endregion
    }
}