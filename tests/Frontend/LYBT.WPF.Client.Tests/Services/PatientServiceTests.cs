using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 患者服务前端单元测试
    /// 测试核心患者管理功能的基本行为
    /// </summary>
    public class PatientServiceTests
    {
        private readonly Mock<IApiService> _mockApiService;
        private readonly Mock<IPatientsApiService> _mockPatientsApiService;
        private readonly PatientService _service;

        public PatientServiceTests()
        {
            _mockApiService = new Mock<IApiService>();
            _mockPatientsApiService = new Mock<IPatientsApiService>();
            _service = new PatientService(_mockApiService.Object, _mockPatientsApiService.Object);
        }

        #region Test Data Factory Methods

        private PatientDetailDto CreateTestPatientDto(Guid? id = null)
        {
            return new PatientDetailDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "张三",
                Gender = Gender.Male,
                Age = 30,
                PhoneNumber = "13800138000",
                IDNumber = "110101199001011234",
                Address = "北京市朝阳区",
                AllergyHistory = "青霉素过敏",
                BirthDate = new DateTime(1990, 1, 1),
                CreateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };
        }

        private PatientPagedQueryDto CreateTestQueryDto()
        {
            return new PatientPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 20,
                SearchKeyword = "张三",
                Name = "张三",
                PhoneNumber = "13800138000",
                IDNumber = "110101199001011234",
                Address = "北京市",
                Gender = Gender.Male,
                MinAge = 20,
                MaxAge = 40,
                Status = CommonStatus.Enabled
            };
        }

        private PaginatedResult<PatientDetailDto> CreateTestPaginatedResult()
        {
            return new PaginatedResult<PatientDetailDto>
            {
                Items = new List<PatientDetailDto> { CreateTestPatientDto() },
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

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidDto_ReturnsSuccess()
        {
            // Arrange
            var dto = CreateTestPatientDto();
            var apiResponse = CreateSuccessApiResponse(dto);
            
            _mockPatientsApiService
                .Setup(x => x.CreatePatientAsync(It.IsAny<PatientDetailDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockPatientsApiService.Verify(x => x.CreatePatientAsync(dto), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var dto = CreateTestPatientDto();
            _mockPatientsApiService
                .Setup(x => x.CreatePatientAsync(It.IsAny<PatientDetailDto>()))
                .ThrowsAsync(new Exception("API错误"));

            // Act
            var result = await _service.AddAsync(dto);

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
            var dto = CreateTestPatientDto();
            var apiResponse = CreateSuccessApiResponse(dto);
            
            _mockPatientsApiService
                .Setup(x => x.UpdatePatientAsync(It.IsAny<Guid>(), It.IsAny<PatientDetailDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockPatientsApiService.Verify(x => x.UpdatePatientAsync(dto.Id, dto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var dto = CreateTestPatientDto();
            _mockPatientsApiService
                .Setup(x => x.UpdatePatientAsync(It.IsAny<Guid>(), It.IsAny<PatientDetailDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region EnableAsync Tests

        [Fact]
        public async Task EnableAsync_WithValidId_CallsToggleStatusApi()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());
            
            _mockPatientsApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.EnableAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockPatientsApiService.Verify(x => x.ToggleStatusAsync(patientId), Times.Once);
        }

        #endregion

        #region DisableAsync Tests

        [Fact]
        public async Task DisableAsync_WithValidId_CallsToggleStatusApi()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());
            
            _mockPatientsApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DisableAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockPatientsApiService.Verify(x => x.ToggleStatusAsync(patientId), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsPatientData()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedDto = CreateTestPatientDto(patientId);
            var apiResponse = CreateSuccessApiResponse(expectedDto);
            
            _mockPatientsApiService
                .Setup(x => x.GetPatientAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(patientId);
            result.Data.Name.Should().Be(expectedDto.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockPatientsApiService
                .Setup(x => x.GetPatientAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("患者不存在"));

            // Act
            var result = await _service.GetByIdAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WhenApiCallSucceeds_ReturnsPatientList()
        {
            // Arrange
            var expectedPatients = new List<PatientDetailDto> { CreateTestPatientDto(), CreateTestPatientDto() };
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _mockPatientsApiService
                .Setup(x => x.GetAllAsync())
                .ThrowsAsync(new Exception("获取失败"));

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = CreateTestQueryDto();
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);
            
            _mockPatientsApiService
                .Setup(x => x.GetPatientsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Gender?>(),
                    It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<PatientStatus?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
        }

        [Fact]
        public async Task GetPagedAsync_WhenApiCallFails_ReturnsEmptyResult()
        {
            // Arrange
            var query = CreateTestQueryDto();
            var apiResponse = CreateFailureApiResponse<PaginatedResult<PatientDetailDto>>();
            
            _mockPatientsApiService
                .Setup(x => x.GetPatientsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Gender?>(),
                    It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<PatientStatus?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.ErrorMessage.Should().Be("获取患者列表失败");
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_WithKeyword_ReturnsSearchResults()
        {
            // Arrange
            const string keyword = "张三";
            var expectedPatients = new List<PatientDetailDto> { CreateTestPatientDto() };
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.SearchAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            _mockPatientsApiService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_ReturnsResults()
        {
            // Arrange
            const string keyword = "";
            var expectedPatients = new List<PatientDetailDto>();
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.SearchAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region BatchDisableAsync Tests

        [Fact]
        public async Task BatchDisableAsync_Always_ReturnsFailure()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var result = await _service.BatchDisableAsync(ids);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("批量操作功能已禁用");
        }

        #endregion

        #region BatchEnableAsync Tests

        [Fact]
        public async Task BatchEnableAsync_Always_ReturnsFailure()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var result = await _service.BatchEnableAsync(ids);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("批量操作功能已禁用");
        }

        #endregion

        #region ExportAsync Tests

        [Fact]
        public async Task ExportAsync_WhenApiCallSucceeds_ReturnsPatientData()
        {
            // Arrange
            var expectedPatients = new List<PatientDetailDto> { CreateTestPatientDto(), CreateTestPatientDto() };
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.ExportAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ExportAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
        }

        #endregion

        #region ImportAsync Tests

        [Fact]
        public async Task ImportAsync_Always_ReturnsFailure()
        {
            // Arrange
            var patients = new List<PatientDetailDto> { CreateTestPatientDto() };

            // Act
            var result = await _service.ImportAsync(patients);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("导入功能已禁用");
        }

        #endregion

        #region GetActivePatientsAsync Tests

        [Fact]
        public async Task GetActivePatientsAsync_WhenApiCallSucceeds_ReturnsActivePatients()
        {
            // Arrange
            var expectedPatients = new List<PatientDetailDto> { CreateTestPatientDto() };
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.GetActivePatientsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetActivePatientsAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        #endregion

        #region FindOrCreateAsync Tests

        [Fact]
        public async Task FindOrCreateAsync_WithValidDto_ReturnsPatient()
        {
            // Arrange
            var dto = CreateTestPatientDto();
            var apiResponse = CreateSuccessApiResponse(dto);
            
            _mockPatientsApiService
                .Setup(x => x.FindOrCreateAsync(It.IsAny<PatientDetailDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.FindOrCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(dto.Id);
            _mockPatientsApiService.Verify(x => x.FindOrCreateAsync(dto), Times.Once);
        }

        #endregion

        #region QuickSearchAsync Tests

        [Fact]
        public async Task QuickSearchAsync_WithKeyword_CallsSearchAsync()
        {
            // Arrange
            const string keyword = "张三";
            var expectedPatients = new List<PatientDetailDto> { CreateTestPatientDto() };
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.SearchAsync(It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.QuickSearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            _mockPatientsApiService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        #endregion

        #region GetListAsync Tests

        [Fact]
        public async Task GetListAsync_WhenApiCallSucceeds_ReturnsPatientInfoList()
        {
            // Arrange
            var expectedPatients = new List<PatientDetailDto> { CreateTestPatientDto() };
            var apiResponse = CreateSuccessApiResponse(expectedPatients);
            
            _mockPatientsApiService
                .Setup(x => x.GetActivePatientsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Name.Should().Be(expectedPatients.First().Name);
        }

        [Fact]
        public async Task GetListAsync_WhenApiCallFails_ReturnsEmptyList()
        {
            // Arrange
            var apiResponse = CreateFailureApiResponse<List<PatientDetailDto>>();
            
            _mockPatientsApiService
                .Setup(x => x.GetActivePatientsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetListAsync_WhenApiCallThrowsException_ThrowsException()
        {
            // Arrange
            _mockPatientsApiService
                .Setup(x => x.GetActivePatientsAsync())
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            Func<Task> act = async () => await _service.GetListAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("获取患者列表失败: 网络错误");
        }

        #endregion
    }
}