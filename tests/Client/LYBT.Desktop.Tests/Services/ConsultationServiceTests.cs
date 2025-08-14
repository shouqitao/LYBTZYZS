using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Refit;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Tests.Services
{
    /// <summary>
    /// 看诊服务单元测试
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
            _service = new ConsultationService(
                _mockApiService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        #region SearchConsultationsAsync Tests (使用新的GET接口)

        [Fact]
        public async Task SearchConsultationsAsync_Success_ReturnsPagedResult()
        {
            // Arrange
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "test"
            };

            var apiResponse = new PagedResult<ConsultationDto>
            {
                Data = new List<ConsultationDto>
                {
                    new ConsultationDto { Id = Guid.NewGuid(), PatientName = "患者1" }
                },
                TotalCount = 1,
                PageIndex = 1,
                PageSize = 10
            };

            var refitResponse = new Refit.ApiResponse<PagedResult<ConsultationDto>>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                apiResponse,
                new RefitSettings());

            _mockApiService.Setup(x => x.GetConsultationsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync(refitResponse);

            // Act
            var result = await _service.SearchConsultationsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchConsultationsAsync_WithExtensionData_PassesCorrectParameters()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var request = new PaginationRequest
            {
                CurrentPage = 2,
                PageSize = 20,
                SearchKeyword = "keyword",
                ExtensionData = new Dictionary<string, object>
                {
                    { "DoctorId", doctorId },
                    { "PatientId", patientId },
                    { "StartDate", DateTime.Today.AddDays(-7) },
                    { "EndDate", DateTime.Today },
                    { "Status", 1 }
                }
            };

            var refitResponse = new Refit.ApiResponse<PagedResult<ConsultationDto>>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                new PagedResult<ConsultationDto>(),
                new RefitSettings());

            _mockApiService.Setup(x => x.GetConsultationsAsync(
                    2, 20, "keyword",
                    doctorId, patientId,
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1))
                .ReturnsAsync(refitResponse);

            // Act
            await _service.SearchConsultationsAsync(request);

            // Assert
            _mockApiService.Verify(x => x.GetConsultationsAsync(
                2, 20, "keyword",
                doctorId, patientId,
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1), Times.Once);
        }

        [Fact]
        public async Task SearchConsultationsAsync_ApiFailure_ReturnsEmptyResult()
        {
            // Arrange
            var request = new PaginationRequest();
            _mockApiService.Setup(x => x.GetConsultationsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("API Error"));

            // Act
            var result = await _service.SearchConsultationsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        #endregion

        #region UpdateStatusAsync Tests (新增方法)

        [Fact]
        public async Task UpdateStatusAsync_Success_ReturnsUpdatedConsultation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var status = 2;
            var reason = "已完成诊断";
            var updatedDto = new ConsultationDetailDto { Id = id };

            var refitResponse = new Refit.ApiResponse<ConsultationDetailDto>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                updatedDto,
                new RefitSettings());

            _mockApiService.Setup(x => x.UpdateStatusAsync(id, It.IsAny<UpdateStatusDto>()))
                .ReturnsAsync(refitResponse);

            // Act
            var result = await _service.UpdateStatusAsync(id, status, reason);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateStatusAsync_CreatesCorrectDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var status = 3;
            var reason = "取消原因";
            UpdateStatusDto capturedDto = null;

            var refitResponse = new Refit.ApiResponse<ConsultationDetailDto>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                new ConsultationDetailDto(),
                new RefitSettings());

            _mockApiService.Setup(x => x.UpdateStatusAsync(id, It.IsAny<UpdateStatusDto>()))
                .Callback<Guid, UpdateStatusDto>((_, dto) => capturedDto = dto)
                .ReturnsAsync(refitResponse);

            // Act
            await _service.UpdateStatusAsync(id, status, reason);

            // Assert
            capturedDto.Should().NotBeNull();
            capturedDto.Status.Should().Be(status);
            capturedDto.Reason.Should().Be(reason);
        }

        [Fact]
        public async Task UpdateStatusAsync_ApiFailure_ReturnsFailure()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockApiService.Setup(x => x.UpdateStatusAsync(id, It.IsAny<UpdateStatusDto>()))
                .ThrowsAsync(new Exception("API Error"));

            // Act
            var result = await _service.UpdateStatusAsync(id, 2);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("更新看诊状态失败");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Success_ReturnsConsultationInfo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationDetailDto
            {
                Id = id,
                PatientName = "测试患者"
            };

            var refitResponse = new Refit.ApiResponse<ConsultationDetailDto>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                dto,
                new RefitSettings());

            _mockApiService.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(refitResponse);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(id);
        }

        #endregion

        #region StartConsultationAsync Tests

        [Fact]
        public async Task StartConsultationAsync_Success_ReturnsConsultationInfo()
        {
            // Arrange
            var dto = new ConsultationStartDto { PatientId = Guid.NewGuid() };
            var resultDto = new ConsultationDetailDto { Id = Guid.NewGuid() };

            var refitResponse = new Refit.ApiResponse<ConsultationDetailDto>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                resultDto,
                new RefitSettings());

            _mockApiService.Setup(x => x.StartConsultationAsync(dto))
                .ReturnsAsync(refitResponse);

            // Act
            var result = await _service.StartConsultationAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region CompleteConsultationAsync Tests

        [Fact]
        public async Task CompleteConsultationAsync_Success_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ConsultationCompleteDto();

            var refitResponse = new Refit.ApiResponse<object>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                new { message = "看诊完成" },
                new RefitSettings());

            _mockApiService.Setup(x => x.CompleteConsultationAsync(id, dto))
                .ReturnsAsync(refitResponse);

            // Act
            var result = await _service.CompleteConsultationAsync(id, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_Success_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();

            var refitResponse = new Refit.ApiResponse<object>(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),
                new { message = "删除成功" },
                new RefitSettings());

            _mockApiService.Setup(x => x.DeleteAsync(id))
                .ReturnsAsync(refitResponse);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion
    }
}