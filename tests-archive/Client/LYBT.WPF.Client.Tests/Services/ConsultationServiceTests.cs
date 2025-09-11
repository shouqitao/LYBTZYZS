using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 看诊服务前端单元测试
    /// 测试核心看诊功能的基本行为
    /// </summary>
    public class ConsultationServiceTests
    {
        private readonly Mock<IConsultationApiService> _mockApiService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<ConsultationService>> _mockLogger;
        private readonly ConsultationService _service;

        public ConsultationServiceTests()
        {
            _mockApiService = new Mock<IConsultationApiService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ConsultationService>>();
            _service = new ConsultationService(_mockApiService.Object, _mockMapper.Object, _mockLogger.Object);
        }

        #region Test Data Factory Methods

        private PaginationRequest CreateTestPaginationRequest()
        {
            return new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "测试关键词"
            };
        }

        private ConsultationDto CreateTestConsultationDto(Guid? id = null)
        {
            return new ConsultationDto
            {
                Id = id ?? Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                UserId = Guid.NewGuid(),
                DoctorName = "王医生",
                Diagnosis = "感冒",
                ConsultationTime = DateTime.Now,
                Status = "enabled"
            };
        }

        private ConsultationDetailDto CreateTestConsultationDetailDto(Guid? id = null)
        {
            return new ConsultationDetailDto
            {
                Id = id ?? Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                UserId = Guid.NewGuid(),
                DoctorName = "王医生",
                Inspection = "面色苍白",
                AuscultationOlfaction = "声音低沉",
                Inquiry = "头痛乏力",
                Palpation = "脉象细弱",
                TongueInspection = "舌质淡白",
                PulseCondition = "脉细数",
                TCMDiagnosis = "气血不足",
                Diagnosis = "感冒",
                TreatmentPrinciple = "益气补血",
                MedicalAdvice = "注意休息",
                ConsultationTime = DateTime.Now,
                CreateTime = DateTime.Now,
                Remark = "注意保暖"
            };
        }

        private ConsultationStartDto CreateTestConsultationStartDto()
        {
            return new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                RegistrationId = Guid.NewGuid(),
                Remark = "开始看诊"
            };
        }

        private ConsultationUpdateDto CreateTestConsultationUpdateDto()
        {
            return new ConsultationUpdateDto
            {
                Inspection = "面色红润",
                AuscultationOlfaction = "声音洪亮",
                Inquiry = "精神饱满",
                Palpation = "脉象有力",
                TongueInspection = "舌质红润",
                PulseCondition = "脉搏正常",
                TCMDiagnosis = "肝气郁结",
                Diagnosis = "焦虑症",
                TreatmentPrinciple = "疏肝解郁",
                MedicalAdvice = "放松心情",
                Remark = "定期复查"
            };
        }

        private ConsultationCompleteDto CreateTestConsultationCompleteDto()
        {
            return new ConsultationCompleteDto
            {
                Diagnosis = "风寒感冒",
                TCMDiagnosis = "风寒束表",
                TreatmentPrinciple = "解表散寒",
                MedicalAdvice = "多喝热水",
                TreatmentPlanId = Guid.NewGuid(),
                NeedFollowUp = true,
                FollowUpDate = DateTime.Now.AddDays(7),
                FollowUpRemark = "一周后复诊"
            };
        }

        private ConsultationInfo CreateTestConsultationInfo(Guid? id = null)
        {
            return new ConsultationInfo
            {
                Id = id ?? Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                UserId = Guid.NewGuid(),
                DoctorName = "王医生",
                Diagnosis = "感冒",
                ConsultationTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };
        }

        private PagedResult<ConsultationDto> CreateTestPagedResult()
        {
            return new PagedResult<ConsultationDto>
            {
                Data = new List<ConsultationDto> { CreateTestConsultationDto() },
                TotalCount = 1,
                PageIndex = 1,
                PageSize = 10
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

        #region SearchConsultationsAsync Tests

        [Fact]
        public async Task SearchConsultationsAsync_WithValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = CreateTestPaginationRequest();
            var pagedResult = CreateTestPagedResult();
            var apiResponse = CreateSuccessApiResponse(pagedResult);

            _mockApiService
                .Setup(x => x.GetConsultationsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchConsultationsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task SearchConsultationsAsync_WhenApiCallFails_ReturnsEmptyResult()
        {
            // Arrange
            var query = CreateTestPaginationRequest();
            var apiResponse = CreateFailureApiResponse<PagedResult<ConsultationDto>>();

            _mockApiService
                .Setup(x => x.GetConsultationsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchConsultationsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.CurrentPage.Should().Be(query.CurrentPage);
            result.PageSize.Should().Be(query.PageSize);
        }

        [Fact]
        public async Task SearchConsultationsAsync_WhenExceptionThrown_ReturnsEmptyResult()
        {
            // Arrange
            var query = CreateTestPaginationRequest();

            _mockApiService
                .Setup(x => x.GetConsultationsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.SearchConsultationsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsConsultationInfo()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var detailDto = CreateTestConsultationDetailDto(consultationId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(consultationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(consultationId);
            result.Data.PatientName.Should().Be(detailDto.PatientName);
            result.Data.DoctorName.Should().Be(detailDto.DoctorName);
        }

        [Fact]
        public async Task GetByIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            
            _mockApiService
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("获取失败"));

            // Act
            var result = await _service.GetByIdAsync(consultationId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetByMedicalCaseIdAsync Tests

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithValidId_ReturnsConsultationInfo()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var detailDto = CreateTestConsultationDetailDto();
            detailDto.MedicalCaseId = medicalCaseId;
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.GetByMedicalCaseIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.MedicalCaseId.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WhenExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.GetByMedicalCaseIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region StartConsultationAsync Tests

        [Fact]
        public async Task StartConsultationAsync_WithValidDto_ReturnsConsultationInfo()
        {
            // Arrange
            var startDto = CreateTestConsultationStartDto();
            var detailDto = CreateTestConsultationDetailDto();
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.StartConsultationAsync(It.IsAny<ConsultationStartDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.StartConsultationAsync(startDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PatientName.Should().Be(detailDto.PatientName);
            _mockApiService.Verify(x => x.StartConsultationAsync(startDto), Times.Once);
        }

        [Fact]
        public async Task StartConsultationAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var startDto = CreateTestConsultationStartDto();

            _mockApiService
                .Setup(x => x.StartConsultationAsync(It.IsAny<ConsultationStartDto>()))
                .ThrowsAsync(new Exception("开始看诊失败"));

            // Act
            var result = await _service.StartConsultationAsync(startDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region UpdateConsultationAsync Tests

        [Fact]
        public async Task UpdateConsultationAsync_WithValidData_ReturnsUpdatedInfo()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var updateDto = CreateTestConsultationUpdateDto();
            var detailDto = CreateTestConsultationDetailDto(consultationId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.UpdateConsultationAsync(It.IsAny<Guid>(), It.IsAny<ConsultationUpdateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateConsultationAsync(consultationId, updateDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(consultationId);
            _mockApiService.Verify(x => x.UpdateConsultationAsync(consultationId, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateConsultationAsync_WhenExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var updateDto = CreateTestConsultationUpdateDto();

            _mockApiService
                .Setup(x => x.UpdateConsultationAsync(It.IsAny<Guid>(), It.IsAny<ConsultationUpdateDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateConsultationAsync(consultationId, updateDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region CompleteConsultationAsync Tests

        [Fact]
        public async Task CompleteConsultationAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var completeDto = CreateTestConsultationCompleteDto();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockApiService
                .Setup(x => x.CompleteConsultationAsync(It.IsAny<Guid>(), It.IsAny<ConsultationCompleteDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CompleteConsultationAsync(consultationId, completeDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.CompleteConsultationAsync(consultationId, completeDto), Times.Once);
        }

        [Fact]
        public async Task CompleteConsultationAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var completeDto = CreateTestConsultationCompleteDto();

            _mockApiService
                .Setup(x => x.CompleteConsultationAsync(It.IsAny<Guid>(), It.IsAny<ConsultationCompleteDto>()))
                .ThrowsAsync(new Exception("完成看诊失败"));

            // Act
            var result = await _service.CompleteConsultationAsync(consultationId, completeDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetTodayConsultationsByDoctorAsync Tests

        [Fact]
        public async Task GetTodayConsultationsByDoctorAsync_WithValidDoctorId_ReturnsConsultationList()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var consultationList = new List<ConsultationDto> { CreateTestConsultationDto(), CreateTestConsultationDto() };
            var apiResponse = CreateSuccessApiResponse(consultationList);

            _mockApiService
                .Setup(x => x.GetTodayConsultationsByDoctorAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTodayConsultationsByDoctorAsync_WhenApiCallFails_ReturnsEmptyList()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var apiResponse = CreateFailureApiResponse<List<ConsultationDto>>();

            _mockApiService
                .Setup(x => x.GetTodayConsultationsByDoctorAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert - 根据实现逻辑，API调用失败时返回成功的空列表
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region GetPatientHistoryAsync Tests

        [Fact]
        public async Task GetPatientHistoryAsync_WithValidPatientId_ReturnsHistoryList()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var consultationList = new List<ConsultationDto> { CreateTestConsultationDto() };
            var apiResponse = CreateSuccessApiResponse(consultationList);

            _mockApiService
                .Setup(x => x.GetPatientHistoryAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WhenApiCallFails_ReturnsEmptyList()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var apiResponse = CreateFailureApiResponse<List<ConsultationDto>>();

            _mockApiService
                .Setup(x => x.GetPatientHistoryAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region GetDoctorConsultationCountAsync Tests

        [Fact]
        public async Task GetDoctorConsultationCountAsync_WithValidDoctorId_ReturnsCount()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            const int expectedCount = 5;
            var apiResponse = CreateSuccessApiResponse(expectedCount);

            _mockApiService
                .Setup(x => x.GetDoctorConsultationCountAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetDoctorConsultationCountAsync(doctorId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedCount);
        }

        [Fact]
        public async Task GetDoctorConsultationCountAsync_WithDateRange_ReturnsCount()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now;
            const int expectedCount = 3;
            var apiResponse = CreateSuccessApiResponse(expectedCount);

            _mockApiService
                .Setup(x => x.GetDoctorConsultationCountAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetDoctorConsultationCountAsync(doctorId, startDate, endDate);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedCount);
            _mockApiService.Verify(x => x.GetDoctorConsultationCountAsync(doctorId, startDate, endDate), Times.Once);
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidData_ReturnsUpdatedInfo()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            const int newStatus = 2;
            const string reason = "状态更新测试";
            var detailDto = CreateTestConsultationDetailDto(consultationId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateStatusDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateStatusAsync(consultationId, newStatus, reason);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(consultationId);
            _mockApiService.Verify(x => x.UpdateStatusAsync(consultationId, It.Is<UpdateStatusDto>(dto => 
                dto.Status == newStatus && dto.Reason == reason)), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateStatusDto>()))
                .ThrowsAsync(new Exception("状态更新失败"));

            // Act
            var result = await _service.UpdateStatusAsync(consultationId, 1);

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
            var consultationId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockApiService
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteAsync(consultationId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.DeleteAsync(consultationId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("删除失败"));

            // Act
            var result = await _service.DeleteAsync(consultationId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion
    }
}