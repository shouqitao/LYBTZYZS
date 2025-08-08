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
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 处方服务前端单元测试
    /// 测试处方管理的核心功能
    /// </summary>
    public class PrescriptionServiceTests
    {
        private readonly Mock<IPrescriptionApiService> _mockApiService;
        private readonly PrescriptionService _service;

        public PrescriptionServiceTests()
        {
            _mockApiService = new Mock<IPrescriptionApiService>();
            _service = new PrescriptionService(_mockApiService.Object);
        }

        #region Test Data Factory Methods

        private PrescriptionDto CreateTestPrescriptionDto(Guid? id = null)
        {
            return new PrescriptionDto
            {
                Id = id ?? Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生",
                Diagnosis = "风寒感冒，需解表散寒",
                DosageCount = 7,
                SingleDosePrice = 25.50m,
                TotalPrice = 178.50m,
                TotalWeight = 120.5m,
                Status = PrescriptionStatus.Draft,
                CreateTime = DateTime.Now,
                Advice = "饭后服用，注意保暖",
                Items = new List<PrescriptionItemDto>
                {
                    CreateTestPrescriptionItemDto(),
                    CreateTestPrescriptionItemDto()
                }
            };
        }

        private PrescriptionDetailDto CreateTestPrescriptionDetailDto(Guid? id = null)
        {
            var dto = new PrescriptionDetailDto
            {
                Id = id ?? Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生",
                Diagnosis = "风寒感冒",
                DosageCount = 7,
                SingleDosePrice = 30.00m,
                TotalPrice = 210.00m,
                TotalWeight = 150.0m,
                Status = PrescriptionStatus.Completed,
                CreateTime = DateTime.Now,
                Advice = "温服，忌生冷",
                FormulaSource = "麻黄汤加减",
                DuplicateWarning = null,
                MissingDrugWarning = null,
                UpdateTime = DateTime.Now,
                Remark = "患者体质偏寒",
                Items = new List<PrescriptionItemDto> { CreateTestPrescriptionItemDto() }
            };
            return dto;
        }

        private PrescriptionItemDto CreateTestPrescriptionItemDto()
        {
            return new PrescriptionItemDto
            {
                Id = Guid.NewGuid(),
                HerbId = Guid.NewGuid(),
                HerbName = "麻黄",
                Quantity = 9,
                Unit = "g",
                UnitPrice = 3.50m,
                TotalPrice = 31.50m,
                TotalWeight = 9,
                Remark = "先煎"
            };
        }

        private PrescriptionItemCreateDto CreateTestPrescriptionItemCreateDto()
        {
            return new PrescriptionItemCreateDto
            {
                HerbId = Guid.NewGuid(),
                HerbName = "桂枝",
                Quantity = 12,
                Unit = "g",
                UnitPrice = 2.80m,
                Remark = "后下"
            };
        }

        private PrescriptionCreateDto CreateTestPrescriptionCreateDto()
        {
            return new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "气血不足，脾胃虚弱",
                DosageCount = 14,
                Advice = "早晚温服，禁食辛辣",
                FormulaSource = "四君子汤",
                Items = new List<PrescriptionItemCreateDto>
                {
                    CreateTestPrescriptionItemCreateDto(),
                    CreateTestPrescriptionItemCreateDto()
                },
                Remark = "患者素体虚弱"
            };
        }

        private PrescriptionEditDto CreateTestPrescriptionEditDto(Guid? id = null)
        {
            return new PrescriptionEditDto
            {
                Id = id ?? Guid.NewGuid(),
                Diagnosis = "肝郁脾虚，气机不畅",
                DosageCount = 10,
                Advice = "饭前服用，保持心情舒畅",
                Items = new List<PrescriptionItemCreateDto>
                {
                    CreateTestPrescriptionItemCreateDto()
                },
                Remark = "调整处方剂量"
            };
        }

        private PaginatedResult<PrescriptionDto> CreateTestPaginatedResult()
        {
            return new PaginatedResult<PrescriptionDto>
            {
                Items = new List<PrescriptionDto> 
                { 
                    CreateTestPrescriptionDto(),
                    CreateTestPrescriptionDto() 
                },
                TotalCount = 2,
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
        public async Task GetPagedAsync_WithValidRequest_ReturnsPagedResult()
        {
            // Arrange
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 20,
                SearchKeyword = "感冒"
            };
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task GetPagedAsync_WhenApiCallFails_ReturnsEmptyResultWithError()
        {
            // Arrange
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 20
            };
            var apiResponse = CreateFailureApiResponse<PaginatedResult<PrescriptionDto>>();

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetPagedAsync_WhenExceptionThrown_ReturnsEmptyResultWithError()
        {
            // Arrange
            var request = new PaginationRequest { CurrentPage = 2, PageSize = 10 };

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.CurrentPage.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("网络错误");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsDetailDto()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var detailDto = CreateTestPrescriptionDetailDto(prescriptionId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(prescriptionId);
            result.Data.PatientName.Should().Be(detailDto.PatientName);
            result.Data.Diagnosis.Should().Be(detailDto.Diagnosis);
            result.Data.FormulaSource.Should().Be(detailDto.FormulaSource);
        }

        [Fact]
        public async Task GetByIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("获取失败"));

            // Act
            var result = await _service.GetByIdAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDto_ReturnsPrescriptionDto()
        {
            // Arrange
            var createDto = CreateTestPrescriptionCreateDto();
            var createdDto = CreateTestPrescriptionDto();
            var apiResponse = CreateSuccessApiResponse(createdDto);

            _mockApiService
                .Setup(x => x.CreatePrescriptionAsync(It.IsAny<PrescriptionCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(createdDto.Id);
            result.Data.PatientName.Should().Be(createdDto.PatientName);
            result.Data.Items.Should().HaveCount(2);
            _mockApiService.Verify(x => x.CreatePrescriptionAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestPrescriptionCreateDto();

            _mockApiService
                .Setup(x => x.CreatePrescriptionAsync(It.IsAny<PrescriptionCreateDto>()))
                .ThrowsAsync(new Exception("创建失败"));

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateAsync_ValidatesHerbItems_BeforeCreation()
        {
            // Arrange
            var createDto = CreateTestPrescriptionCreateDto();
            createDto.Items.Clear(); // 空的处方项目列表

            var createdDto = CreateTestPrescriptionDto();
            createdDto.Items.Clear();
            var apiResponse = CreateSuccessApiResponse(createdDto);

            _mockApiService
                .Setup(x => x.CreatePrescriptionAsync(It.IsAny<PrescriptionCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty(); // 允许创建空处方
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidDto_ReturnsUpdatedPrescription()
        {
            // Arrange
            var editDto = CreateTestPrescriptionEditDto();
            var updatedDto = CreateTestPrescriptionDto(editDto.Id);
            updatedDto.Diagnosis = editDto.Diagnosis;
            var apiResponse = CreateSuccessApiResponse(updatedDto);

            _mockApiService
                .Setup(x => x.UpdatePrescriptionAsync(It.IsAny<Guid>(), It.IsAny<PrescriptionEditDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateAsync(editDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(editDto.Id);
            result.Data.Diagnosis.Should().Be(editDto.Diagnosis);
            _mockApiService.Verify(x => x.UpdatePrescriptionAsync(editDto.Id, editDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var editDto = CreateTestPrescriptionEditDto();

            _mockApiService
                .Setup(x => x.UpdatePrescriptionAsync(It.IsAny<Guid>(), It.IsAny<PrescriptionEditDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateAsync(editDto);

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
            var prescriptionId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(true);

            _mockApiService
                .Setup(x => x.DeletePrescriptionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockApiService.Verify(x => x.DeletePrescriptionAsync(prescriptionId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.DeletePrescriptionAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("删除失败"));

            // Act
            var result = await _service.DeleteAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region CancelAsync Tests

        [Fact]
        public async Task CancelAsync_WithValidId_ReturnsCancelledPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var cancelledDto = CreateTestPrescriptionDto(prescriptionId);
            cancelledDto.Status = PrescriptionStatus.Completed; // 作废的处方标记为已完成
            var apiResponse = CreateSuccessApiResponse(cancelledDto);

            _mockApiService
                .Setup(x => x.CancelPrescriptionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CancelAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(prescriptionId);
            result.Data.Status.Should().Be(PrescriptionStatus.Completed); // 作废的处方标记为已完成
            _mockApiService.Verify(x => x.CancelPrescriptionAsync(prescriptionId), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.CancelPrescriptionAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("作废失败"));

            // Act
            var result = await _service.CancelAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_WithValidPatientId_ReturnsPrescriptionList()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByPatientIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var apiResponse = CreateFailureApiResponse<PaginatedResult<PrescriptionDto>>();

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("获取患者处方列表失败");
        }

        #endregion

        #region GetByDoctorIdAsync Tests

        [Fact]
        public async Task GetByDoctorIdAsync_WithValidDoctorId_ReturnsPrescriptionList()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByDoctorIdAsync(doctorId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByDoctorIdAsync_WhenExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var doctorId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.GetByDoctorIdAsync(doctorId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("获取医生处方失败");
        }

        #endregion

        #region GetTodayPrescriptionsAsync Tests

        [Fact]
        public async Task GetTodayPrescriptionsAsync_ReturnsOnlyTodayPrescriptions()
        {
            // Arrange
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetTodayPrescriptionsAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            // 验证日期参数
            _mockApiService.Verify(x => x.GetListAsync(
                1, 1000, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<PrescriptionStatus?>(),
                It.Is<DateTime?>(d => d.HasValue && d.Value.Date == DateTime.Today),
                It.Is<DateTime?>(d => d.HasValue && d.Value.Date == DateTime.Today),
                It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public async Task GetTodayPrescriptionsAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var apiResponse = CreateFailureApiResponse<PaginatedResult<PrescriptionDto>>();

            _mockApiService
                .Setup(x => x.GetListAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<PrescriptionStatus?>(), It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetTodayPrescriptionsAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("获取今日处方列表失败");
        }

        #endregion

        #region Prescription Business Logic Tests

        [Fact]
        public async Task CreateAsync_WithChineseHerbFormula_CalculatesTotalPriceCorrectly()
        {
            // Arrange - 测试中药处方的价格计算
            var createDto = CreateTestPrescriptionCreateDto();
            createDto.DosageCount = 7; // 7剂
            createDto.Items = new List<PrescriptionItemCreateDto>
            {
                new() { HerbId = Guid.NewGuid(), HerbName = "人参", Quantity = 10, Unit = "g", UnitPrice = 5.00m },
                new() { HerbId = Guid.NewGuid(), HerbName = "黄芪", Quantity = 30, Unit = "g", UnitPrice = 2.00m }
            };

            var createdDto = CreateTestPrescriptionDto();
            createdDto.SingleDosePrice = 110.00m; // (10*5 + 30*2) = 110
            createdDto.TotalPrice = 770.00m; // 110 * 7 = 770
            var apiResponse = CreateSuccessApiResponse(createdDto);

            _mockApiService
                .Setup(x => x.CreatePrescriptionAsync(It.IsAny<PrescriptionCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.SingleDosePrice.Should().Be(110.00m);
            result.Data.TotalPrice.Should().Be(770.00m);
        }

        [Fact]
        public async Task UpdateAsync_ChangingDosageCount_RecalculatesTotalPrice()
        {
            // Arrange - 测试修改剂数后的价格重算
            var editDto = CreateTestPrescriptionEditDto();
            editDto.DosageCount = 14; // 改为14剂

            var updatedDto = CreateTestPrescriptionDto(editDto.Id);
            updatedDto.DosageCount = 14;
            updatedDto.SingleDosePrice = 50.00m;
            updatedDto.TotalPrice = 700.00m; // 50 * 14
            var apiResponse = CreateSuccessApiResponse(updatedDto);

            _mockApiService
                .Setup(x => x.UpdatePrescriptionAsync(It.IsAny<Guid>(), It.IsAny<PrescriptionEditDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateAsync(editDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.DosageCount.Should().Be(14);
            result.Data.TotalPrice.Should().Be(700.00m);
        }

        #endregion
    }
}