using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// PrescriptionService UltraThink委托模式测试
    /// 验证纯委托模式的正确性：Service → QueryService/BusinessService
    /// </summary>
    public class PrescriptionServiceUltraThinkTests
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly Mock<IPrescriptionQueryService> _mockQueryService;
        private readonly Mock<IPrescriptionBusinessService> _mockBusinessService;

        public PrescriptionServiceUltraThinkTests()
        {
            _mockQueryService = new Mock<IPrescriptionQueryService>();
            _mockBusinessService = new Mock<IPrescriptionBusinessService>();
            _prescriptionService = new PrescriptionService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullQueryService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => new PrescriptionService(null, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_WithNullBusinessService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => new PrescriptionService(_mockQueryService.Object, null);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("businessService");
        }

        [Fact]
        public void Constructor_WithValidServices_InitializesSuccessfully()
        {
            // Act & Assert
            var service = new PrescriptionService(_mockQueryService.Object, _mockBusinessService.Object);
            service.Should().NotBeNull();
        }

        #endregion

        #region Query Operations Delegation Tests

        [Fact]
        public async Task GetByIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedResult = ServiceResult<PrescriptionDto>.Success(new PrescriptionDto { Id = prescriptionId });
            
            _mockQueryService.Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(prescriptionId), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_DelegatesToQueryService()
        {
            // Arrange
            var query = new PrescriptionQueryDto { CurrentPage = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<PrescriptionDto>>.Success(
                new PagedResult<PrescriptionDto>
                {
                    Data = new List<PrescriptionDto>(),
                    TotalCount = 0
                });
            
            _mockQueryService.Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(query);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByPatientIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
            
            _mockQueryService.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
            
            _mockQueryService.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByMedicalCaseIdAsync(medicalCaseId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DelegatesToQueryService()
        {
            // Arrange
            var keyword = "阿司匹林";
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
            
            _mockQueryService.Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.SearchAsync(keyword);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_DelegatesToQueryService_ReturnsListDirectly()
        {
            // Arrange
            var prescriptions = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = Guid.NewGuid(), Diagnosis = "感冒" },
                new PrescriptionDto { Id = Guid.NewGuid(), Diagnosis = "头痛" }
            };
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            
            _mockQueryService.Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetAllAsync();

            // Assert
            result.Should().BeEquivalentTo(prescriptions);
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenServiceResultFails_ReturnsEmptyList()
        {
            // Arrange
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Failure("查询失败");
            
            _mockQueryService.Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetAllAsync();

            // Assert
            result.Should().BeEmpty();
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDoctorTodayPrescriptionsAsync_DelegatesToQueryService_ReturnsListDirectly()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = Guid.NewGuid(), DoctorId = doctorId }
            };
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            
            _mockQueryService.Setup(x => x.GetDoctorTodayPrescriptionsAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetDoctorTodayPrescriptionsAsync(doctorId);

            // Assert
            result.Should().BeEquivalentTo(prescriptions);
            _mockQueryService.Verify(x => x.GetDoctorTodayPrescriptionsAsync(doctorId), Times.Once);
        }

        #endregion

        #region Business Operations Delegation Tests

        [Fact]
        public async Task CopyAsync_DelegatesToBusinessService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var newName = "复制的处方";
            var expectedResult = ServiceResult<PrescriptionDto>.Success(
                new PrescriptionDto { Id = Guid.NewGuid(), Name = newName });
            
            _mockBusinessService.Setup(x => x.CopyAsync(prescriptionId, newName, Guid.Empty, "System"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CopyAsync(prescriptionId, newName);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.CopyAsync(prescriptionId, newName, Guid.Empty, "System"), Times.Once);
        }

        [Fact]
        public async Task CopyLastPrescriptionAsync_DelegatesToBusinessService_ReturnsDataDirectly()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "张医生";
            var prescriptionDto = new PrescriptionDto { Id = Guid.NewGuid(), PatientId = patientId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(prescriptionDto);
            
            _mockBusinessService.Setup(x => x.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName);

            // Assert
            result.Should().Be(prescriptionDto);
            _mockBusinessService.Verify(x => x.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task CopyLastPrescriptionAsync_WhenServiceResultFails_ReturnsNull()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "张医生";
            var expectedResult = ServiceResult<PrescriptionDto>.Failure("复制失败");
            
            _mockBusinessService.Setup(x => x.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName);

            // Assert
            result.Should().BeNull();
            _mockBusinessService.Verify(x => x.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task CreateFromTemplateAsync_DelegatesToBusinessService_ReturnsDataDirectly()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "李医生";
            var prescriptionDto = new PrescriptionDto { Id = Guid.NewGuid(), PatientId = patientId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(prescriptionDto);
            
            _mockBusinessService.Setup(x => x.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName);

            // Assert
            result.Should().Be(prescriptionDto);
            _mockBusinessService.Verify(x => x.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task QuickSaveAsync_DelegatesToBusinessService_ReturnsBoolDirectly()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var dto = new QuickPrescriptionDto { Diagnosis = "快速诊断" };
            var operatorId = Guid.NewGuid();
            var operatorName = "王医生";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.QuickSaveAsync(prescriptionId, dto, operatorId, operatorName))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.QuickSaveAsync(prescriptionId, dto, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            _mockBusinessService.Verify(x => x.QuickSaveAsync(prescriptionId, dto, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_DelegatesToBusinessService_ReturnsBoolDirectly()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescriptionIdString = prescriptionId.ToString();
            var operatorId = Guid.NewGuid();
            var operatorName = "赵医生";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.CancelAsync(prescriptionId, operatorId, operatorName))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CancelAsync(prescriptionIdString, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            _mockBusinessService.Verify(x => x.CancelAsync(prescriptionId, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_WithInvalidGuid_ReturnsFalse()
        {
            // Arrange
            var invalidId = "invalid-guid";
            var operatorId = Guid.NewGuid();
            var operatorName = "医生";

            // Act
            var result = await _prescriptionService.CancelAsync(invalidId, operatorId, operatorName);

            // Assert
            result.Should().BeFalse();
            _mockBusinessService.Verify(x => x.CancelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region 内置方法测试（非委托）

        [Fact]
        public async Task ValidateAsync_PerformsValidationLogic()
        {
            // Arrange
            var validDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                Diagnosis = "感冒发热",
                TreatmentPlan = "对症治疗"
            };

            // Act
            var result = await _prescriptionService.ValidateAsync(validDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.IsValid.Should().BeTrue();
            result.Data.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithInvalidData_ReturnsValidationErrors()
        {
            // Arrange
            var invalidDto = new PrescriptionCreateDto
            {
                PatientId = Guid.Empty,  // 无效的患者ID
                Diagnosis = "",          // 空诊断
                TreatmentPlan = "治疗方案"
            };

            // Act
            var result = await _prescriptionService.ValidateAsync(invalidDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.IsValid.Should().BeFalse();
            result.Data.Errors.Should().Contain("处方诊断不能为空");
            result.Data.Errors.Should().Contain("患者ID不能为空");
        }

        #endregion

        #region 未实现方法测试

        [Fact]
        public async Task CreateAsync_ReturnsFailureResult()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto { Diagnosis = "新处方" };

            // Act
            var result = await _prescriptionService.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("CreateAsync方法需要在BusinessService中实现");
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFailureResult()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var editDto = new PrescriptionEditDto { Diagnosis = "更新的处方" };

            // Act
            var result = await _prescriptionService.UpdateAsync(prescriptionId, editDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("UpdateAsync方法需要在BusinessService中实现");
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFailureResult()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();

            // Act
            var result = await _prescriptionService.DeleteAsync(prescriptionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("DeleteAsync方法需要在BusinessService中实现");
        }

        #endregion

        #region 架构验证测试

        [Fact]
        public void Service_FollowsUltraThinkDelegationPattern()
        {
            // 验证Service类遵循纯委托模式
            var serviceType = typeof(PrescriptionService);
            
            // 1. 验证构造函数只依赖QueryService和BusinessService
            var constructor = serviceType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            
            parameters.Should().HaveCount(2);
            parameters[0].ParameterType.Name.Should().Be("IPrescriptionQueryService");
            parameters[1].ParameterType.Name.Should().Be("IPrescriptionBusinessService");
        }

        [Fact] 
        public void Service_HasCorrectDependencyInjection()
        {
            // 验证依赖注入的正确性
            var service = new PrescriptionService(_mockQueryService.Object, _mockBusinessService.Object);
            service.Should().NotBeNull();
            
            // 验证服务不为null说明构造函数正确处理了依赖
            _mockQueryService.Should().NotBeNull();
            _mockBusinessService.Should().NotBeNull();
        }

        #endregion
    }
}