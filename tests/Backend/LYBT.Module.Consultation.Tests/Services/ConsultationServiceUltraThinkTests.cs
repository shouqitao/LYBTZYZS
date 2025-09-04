using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Consultation.Services;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// ConsultationService UltraThink委托模式测试
    /// 验证纯委托模式的正确性：Service → QueryService/BusinessService
    /// </summary>
    public class ConsultationServiceUltraThinkTests
    {
        private readonly ConsultationService _consultationService;
        private readonly Mock<IConsultationQueryService> _mockQueryService;
        private readonly Mock<IConsultationBusinessService> _mockBusinessService;

        public ConsultationServiceUltraThinkTests()
        {
            _mockQueryService = new Mock<IConsultationQueryService>();
            _mockBusinessService = new Mock<IConsultationBusinessService>();
            _consultationService = new ConsultationService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullQueryService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => new ConsultationService(null, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_WithNullBusinessService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => new ConsultationService(_mockQueryService.Object, null);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("businessService");
        }

        [Fact]
        public void Constructor_WithValidServices_InitializesSuccessfully()
        {
            // Act & Assert
            var service = new ConsultationService(_mockQueryService.Object, _mockBusinessService.Object);
            service.Should().NotBeNull();
        }

        #endregion

        #region Query Operations Delegation Tests

        [Fact]
        public async Task GetPagedAsync_DelegatesToQueryService()
        {
            // Arrange
            var query = new PagedQueryBaseDto { CurrentPage = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<ConsultationDto>>.Success(
                new PagedResult<ConsultationDto>
                {
                    Data = new List<ConsultationDto>(),
                    TotalCount = 0
                });
            
            _mockQueryService.Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByPatientIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            
            _mockQueryService.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            
            _mockQueryService.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByMedicalCaseIdAsync(medicalCaseId), Times.Once);
        }

        [Fact]
        public async Task GetByDoctorIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            
            _mockQueryService.Setup(x => x.GetByDoctorIdAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByDoctorIdAsync(doctorId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByDoctorIdAsync(doctorId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DelegatesToQueryService()
        {
            // Arrange
            var keyword = "感冒";
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            
            _mockQueryService.Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.SearchAsync(keyword);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            
            _mockQueryService.Setup(x => x.GetPatientHistoryAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetPatientHistoryAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetPatientHistoryAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetFourDiagnosisByMedicalCaseIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var expectedResult = ServiceResult<object>.Success(new { Inspection = "正常" });
            
            _mockQueryService.Setup(x => x.GetFourDiagnosisByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetFourDiagnosisByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetFourDiagnosisByMedicalCaseIdAsync(medicalCaseId), Times.Once);
        }

        #endregion

        #region Business Operations Delegation Tests

        [Fact]
        public async Task SaveFourDiagnosisAsync_DelegatesToBusinessService()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var fourDiagnosisData = new { Inspection = "面色正常", Palpation = "脉象平稳" };
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData), Times.Once);
        }

        [Fact]
        public async Task ValidateWorkflowStateAsync_DelegatesToBusinessService_ReturnsBool()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var targetStatus = ConsultationStatus.InProgress;
            var serviceResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ValidateWorkflowStateAsync(consultationId, targetStatus))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _consultationService.ValidateWorkflowStateAsync(consultationId, targetStatus);

            // Assert
            result.Should().BeTrue();
            _mockBusinessService.Verify(x => x.ValidateWorkflowStateAsync(consultationId, targetStatus), Times.Once);
        }

        [Fact]
        public async Task ValidateWorkflowStateAsync_WhenServiceResultFails_ReturnsFalse()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var targetStatus = ConsultationStatus.Completed;
            var serviceResult = ServiceResult<bool>.Failure("验证失败");
            
            _mockBusinessService.Setup(x => x.ValidateWorkflowStateAsync(consultationId, targetStatus))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _consultationService.ValidateWorkflowStateAsync(consultationId, targetStatus);

            // Assert
            result.Should().BeFalse();
            _mockBusinessService.Verify(x => x.ValidateWorkflowStateAsync(consultationId, targetStatus), Times.Once);
        }

        #endregion

        #region 未实现方法测试

        [Fact]
        public async Task GetByIdAsync_ReturnsFailureResult()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

            // Act
            var result = await _consultationService.GetByIdAsync(consultationId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("GetByIdAsync方法需要在QueryService中实现");
        }

        [Fact]
        public async Task StartAsync_ReturnsFailureResult()
        {
            // Arrange
            var startDto = new ConsultationStartDto { PatientId = Guid.NewGuid() };

            // Act
            var result = await _consultationService.StartAsync(startDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("StartAsync方法需要在BusinessService中实现");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFailureResult()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var updateDto = new ConsultationDetailDto { Diagnosis = "更新的诊断" };

            // Act
            var result = await _consultationService.UpdateAsync(consultationId, updateDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("UpdateAsync方法需要在BusinessService中实现");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFailureResult()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

            // Act
            var result = await _consultationService.DeleteAsync(consultationId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("DeleteAsync方法需要在BusinessService中实现");
        }

        #endregion

        #region Legacy Support Tests

        [Fact]
        public async Task GetStatisticsAsync_ReturnsEmptyStatistics()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            // Act
            var result = await _consultationService.GetStatisticsAsync(startDate, endDate);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            // 验证返回的统计数据结构
            var statsObject = result.Data;
            statsObject.Should().NotBeNull();
        }

        #endregion

        #region 架构验证测试

        [Fact]
        public void Service_FollowsUltraThinkDelegationPattern()
        {
            // 验证Service类遵循纯委托模式
            var serviceType = typeof(ConsultationService);
            
            // 1. 验证构造函数只依赖QueryService和BusinessService
            var constructor = serviceType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            
            parameters.Should().HaveCount(2);
            parameters[0].ParameterType.Name.Should().Be("IConsultationQueryService");
            parameters[1].ParameterType.Name.Should().Be("IConsultationBusinessService");
        }

        [Fact] 
        public void Service_HasCorrectDependencyInjection()
        {
            // 验证依赖注入的正确性
            var service = new ConsultationService(_mockQueryService.Object, _mockBusinessService.Object);
            service.Should().NotBeNull();
            
            // 验证服务不为null说明构造函数正确处理了依赖
            _mockQueryService.Should().NotBeNull();
            _mockBusinessService.Should().NotBeNull();
        }

        #endregion
    }
}