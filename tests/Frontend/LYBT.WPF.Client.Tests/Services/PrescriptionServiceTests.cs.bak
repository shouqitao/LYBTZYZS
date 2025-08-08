using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Shared.Models.ApiResponses;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Dtos;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Common;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 处方服务前端单元测试
    /// 测试新创建的服务层功能
    /// </summary>
    public class PrescriptionServiceTests
    {
        private readonly Mock<IPrescriptionApiService> _mockApiService;
        private readonly Mock<ILogger<PrescriptionService>> _mockLogger;
        private readonly PrescriptionService _service;

        public PrescriptionServiceTests()
        {
            _mockApiService = new Mock<IPrescriptionApiService>();
            _mockLogger = new Mock<ILogger<PrescriptionService>>();
            _service = new PrescriptionService(_mockApiService.Object, _mockLogger.Object);
        }

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidRequest_ReturnsPagedResult()
        {
            // Arrange
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "感冒"
            };

            var apiResponse = new PaginatedResult<PrescriptionDto>
            {
                Items = new List<PrescriptionDto>
                {
                    new PrescriptionDto 
                    { 
                        Id = Guid.NewGuid(), 
                        PatientId = Guid.NewGuid(),
                        DoctorId = Guid.NewGuid(),
                        Diagnosis = "感冒",
                        Status = PrescriptionStatus.Completed,
                        TotalPrice = 150.00m,
                        CreateTime = DateTime.Now.AddHours(-1)
                    },
                    new PrescriptionDto 
                    { 
                        Id = Guid.NewGuid(), 
                        PatientId = Guid.NewGuid(),
                        DoctorId = Guid.NewGuid(),
                        Diagnosis = "风寒感冒",
                        Status = PrescriptionStatus.Draft,
                        TotalPrice = 120.00m,
                        CreateTime = DateTime.Now.AddHours(-2)
                    }
                },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockApiService.Setup(x => x.GetPrescriptionsAsync(
                request.CurrentPage,
                request.PageSize,
                request.SearchKeyword,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.CurrentPage.Should().Be(1);
            result.Total.Should().Be(2);
            result.Items.Should().Contain(p => p.Diagnosis.Contains("感冒"));
        }

        [Fact]
        public async Task GetPagedAsync_WithDateRangeFilter_CallsApiWithCorrectDates()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                StartDate = startDate,
                EndDate = endDate
            };

            _mockApiService.Setup(x => x.GetPrescriptionsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                startDate,
                endDate,
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ReturnsAsync(new PaginatedResult<PrescriptionDto>());

            // Act
            await _service.GetPagedAsync(request);

            // Assert
            _mockApiService.Verify(x => x.GetPrescriptionsAsync(
                1, 10, null, startDate, endDate, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_ApiThrowsException_ReturnsEmptyResult()
        {
            // Arrange
            var request = new PaginationRequest { CurrentPage = 1, PageSize = 10 };

            _mockApiService.Setup(x => x.GetPrescriptionsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.Total.Should().Be(0);
            
            // 验证错误日志
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取处方列表失败")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingPrescription_ReturnsDetailedPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescriptionDetail = new PrescriptionDetailDto
            {
                Id = prescriptionId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "胃炎",
                Status = PrescriptionStatus.Completed,
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "白术",
                        Quantity = 10,
                        Unit = "g",
                        Price = 5.00m
                    },
                    new PrescriptionItemDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "茅苍术",
                        Quantity = 10,
                        Unit = "g",
                        Price = 3.00m
                    }
                },
                TotalPrice = 80.00m,
                Remark = "饭后服用",
                CreateTime = DateTime.Now.AddHours(-1)
            };

            _mockApiService.Setup(x => x.GetPrescriptionAsync(prescriptionId))
                .ReturnsAsync(prescriptionDetail);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.Id.Should().Be(prescriptionId);
            result.Content.Items.Should().HaveCount(2);
            result.Content.TotalPrice.Should().Be(80.00m);
        }

        [Fact]
        public async Task GetByIdAsync_PrescriptionNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                System.Net.Http.HttpMethod.Get,
                new HttpResponseMessage(HttpStatusCode.NotFound),
                new RefitSettings());

            _mockApiService.Setup(x => x.GetPrescriptionAsync(prescriptionId))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Content.Should().BeNull();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ReturnsCreatedPrescription()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "风寒感冒",
                Items = new List<PrescriptionItemCreateDto>
                {
                    new PrescriptionItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 10,
                        Price = 5.00m
                    }
                },
                Remark = "餐后温水送服"
            };

            var createdPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                Diagnosis = createDto.Diagnosis,
                Status = PrescriptionStatus.Draft,
                TotalPrice = 50.00m,
                CreateTime = DateTime.Now
            };

            _mockApiService.Setup(x => x.CreatePrescriptionAsync(createDto))
                .ReturnsAsync(createdPrescription);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.Diagnosis.Should().Be("风寒感冒");
            result.Content.Status.Should().Be(PrescriptionStatus.Draft);
        }

        [Fact]
        public async Task CreateAsync_WithEmptyItems_ReturnsValidationError()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "测试",
                Items = new List<PrescriptionItemCreateDto>() // 空列表
            };

            var errorContent = new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "处方必须包含至少一种药材",
                Status = 400
            };

            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                System.Net.Http.HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new System.Net.Http.StringContent(
                        System.Text.Json.JsonSerializer.Serialize(errorContent))
                },
                new RefitSettings());

            _mockApiService.Setup(x => x.CreatePrescriptionAsync(createDto))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Error?.Detail.Should().Contain("处方必须包含至少一种药材");
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ReturnsUpdatedPrescription()
        {
            // Arrange
            var updateDto = new PrescriptionEditDto
            {
                Id = Guid.NewGuid(),
                Diagnosis = "更新后的诊断",
                Items = new List<PrescriptionItemEditDto>
                {
                    new PrescriptionItemEditDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 15,
                        Price = 6.00m
                    }
                },
                Remark = "更新后的备注"
            };

            var updatedPrescription = new PrescriptionDto
            {
                Id = updateDto.Id,
                Diagnosis = updateDto.Diagnosis,
                TotalPrice = 90.00m,
                UpdateTime = DateTime.Now
            };

            _mockApiService.Setup(x => x.UpdatePrescriptionAsync(updateDto.Id, updateDto))
                .ReturnsAsync(updatedPrescription);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.Diagnosis.Should().Be("更新后的诊断");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_DraftPrescription_ReturnsSuccessResult()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var apiResponse = new ApiResponse<object> { Message = "处方删除成功" };

            _mockApiService.Setup(x => x.DeletePrescriptionAsync(prescriptionId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion

        #region CancelAsync Tests

        [Fact]
        public async Task CancelAsync_ValidPrescription_ReturnsCancelledPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var cancelledPrescription = new PrescriptionDto
            {
                Id = prescriptionId,
                Status = PrescriptionStatus.Cancelled,
                CancelTime = DateTime.Now,
                CancelReason = "患者取消"
            };

            _mockApiService.Setup(x => x.CancelPrescriptionAsync(prescriptionId))
                .ReturnsAsync(cancelledPrescription);

            // Act
            var result = await _service.CancelAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.Status.Should().Be(PrescriptionStatus.Cancelled);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task GetPagedAsync_WithPatientFilter_ReturnsOnlyPatientPrescriptions()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                CustomFilters = new Dictionary<string, object>
                {
                    { "PatientId", patientId }
                }
            };

            var apiResponse = new PaginatedResult<PrescriptionDto>
            {
                Items = new List<PrescriptionDto>
                {
                    new PrescriptionDto { Id = Guid.NewGuid(), PatientId = patientId }
                },
                TotalCount = 1
            };

            _mockApiService.Setup(x => x.GetPrescriptionsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                patientId,
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Items.Should().OnlyContain(p => p.PatientId == patientId);
        }

        [Fact]
        public async Task CreateAsync_NetworkTimeout_ReturnsTimeoutError()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "测试"
            };

            var timeoutException = new TaskCanceledException("请求超时");

            _mockApiService.Setup(x => x.CreatePrescriptionAsync(createDto))
                .ThrowsAsync(timeoutException);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.Error?.Detail.Should().Contain("请求超时");
        }

        [Fact]
        public async Task UpdateAsync_CompletedPrescription_ReturnsBusinessError()
        {
            // Arrange
            var updateDto = new PrescriptionEditDto
            {
                Id = Guid.NewGuid(),
                Diagnosis = "尝试更新"
            };

            var errorContent = new ProblemDetails
            {
                Title = "Business Rule Violation",
                Detail = "已完成的处方不能修改",
                Status = 400
            };

            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                System.Net.Http.HttpMethod.Put,
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new System.Net.Http.StringContent(
                        System.Text.Json.JsonSerializer.Serialize(errorContent))
                },
                new RefitSettings());

            _mockApiService.Setup(x => x.UpdatePrescriptionAsync(updateDto.Id, updateDto))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.Error?.Detail.Should().Contain("已完成的处方不能修改");
        }

        #endregion
    }
}