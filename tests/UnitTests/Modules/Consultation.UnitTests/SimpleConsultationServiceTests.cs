using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests
{
    /// <summary>
    /// ConsultationService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimpleConsultationServiceTests
    {
        private readonly ConsultationService _consultationService;
        private readonly Mock<IConsultationQueryService> _mockQueryService;
        private readonly Mock<IConsultationBusinessService> _mockBusinessService;

        public SimpleConsultationServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IConsultationQueryService>();
            _mockBusinessService = new Mock<IConsultationBusinessService>();

            // 创建 ConsultationService 实例 (主Service委托模式)
            _consultationService = new ConsultationService(
                _mockQueryService.Object,
                _mockBusinessService.Object);
        }

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paged_Result()
        {
            // Arrange
            var query = new PagedQueryBaseDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            var consultations = new List<ConsultationDto>
            {
                new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), DoctorName = "患者1" },
                new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), DoctorName = "患者2" }
            };

            var expectedResult = ServiceResult<PagedResult<ConsultationDto>>.Success(new PagedResult<ConsultationDto>
            {
                Items = consultations,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        #endregion

        #region GetByPatientIdAsync 测试

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_Patient_Consultations()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var consultations = new List<ConsultationDto>
            {
                new() { Id = Guid.NewGuid(), PatientId = patientId, DoctorName = "测试患者" },
                new() { Id = Guid.NewGuid(), PatientId = patientId, DoctorName = "测试患者" }
            };

            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(consultations);

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().OnlyContain(c => c.PatientId == patientId);
        }

        #endregion

        #region GetByMedicalCaseIdAsync 测试

        [Fact]
        public async Task GetByMedicalCaseIdAsync_Should_Return_MedicalCase_Consultations()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var consultations = new List<ConsultationDto>
            {
                new() { Id = Guid.NewGuid(), MedicalCaseId = medicalCaseId, DoctorName = "测试患者" }
            };

            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(consultations);

            _mockQueryService
                .Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data.Should().OnlyContain(c => c.MedicalCaseId == medicalCaseId);
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Matching_Consultations()
        {
            // Arrange
            var keyword = "测试";
            var consultations = new List<ConsultationDto>
            {
                new() { Id = Guid.NewGuid(), DoctorName = "测试患者1" },
                new() { Id = Guid.NewGuid(), DoctorName = "测试患者2" }
            };

            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(consultations);

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_No_Match()
        {
            // Arrange
            var keyword = "不存在";
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion


        #region GetStatisticsAsync 测试

        [Fact]
        public async Task GetStatisticsAsync_Should_Return_Legacy_Message()
        {
            // Arrange & Act
            var result = await _consultationService.GetStatisticsAsync(DateTime.Now.AddDays(-30), DateTime.Now);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region 异常分支和边界值测试 (成功经验应用)

        [Fact]
        public async Task GetPagedAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 业务失败分支测试
            var query = new PagedQueryBaseDto
            {
                PageIndex = 1,
                PageSize = 10
            };
            var expectedResult = ServiceResult<PagedResult<ConsultationDto>>.Failure("查询服务异常");

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("查询服务异常");
        }

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_Failure_When_Patient_Not_Found()
        {
            // Arrange - 数据不存在测试
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<ConsultationDto>>.Failure("患者不存在");

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者不存在");
        }

        [Fact]
        public async Task GetByPatientIdAsync_With_Empty_Guid_Should_Still_Work()
        {
            // Arrange - 边界值：空GUID
            var patientId = Guid.Empty;
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>
            {
                new() { Id = Guid.NewGuid(), PatientId = Guid.Empty, DoctorName = "边界测试" }
            });

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data.Should().OnlyContain(c => c.PatientId == Guid.Empty);
        }

        [Fact]
        public async Task GetPagedAsync_With_Large_PageSize_Should_Handle_Gracefully()
        {
            // Arrange - 极端值测试：大分页尺寸
            var query = new PagedQueryBaseDto
            {
                PageIndex = 1,
                PageSize = 999999 // 极端大值
            };

            var consultations = new List<ConsultationDto>
            {
                new() { Id = Guid.NewGuid(), DoctorName = "咨询1" },
                new() { Id = Guid.NewGuid(), DoctorName = "咨询2" }
            };

            var expectedResult = ServiceResult<PagedResult<ConsultationDto>>.Success(new PagedResult<ConsultationDto>
            {
                Items = consultations,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 999999
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.PageSize.Should().Be(999999);
        }

        [Fact]
        public async Task SearchAsync_With_Empty_Keyword_Should_Return_Empty_List()
        {
            // Arrange - 空值测试
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 查询失败测试
            var keyword = "测试";
            var expectedResult = ServiceResult<List<ConsultationDto>>.Failure("搜索服务异常");

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("搜索服务异常");
        }


        [Fact]
        public async Task GetByMedicalCaseIdAsync_Should_Return_Failure_When_MedicalCase_Not_Found()
        {
            // Arrange - 医疗案例不存在测试
            var medicalCaseId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<ConsultationDto>>.Failure("医疗案例不存在");

            _mockQueryService
                .Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("医疗案例不存在");
        }

        #endregion
    }
}
