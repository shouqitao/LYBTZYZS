using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// PrescriptionService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class PrescriptionServiceTests
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly Mock<IPrescriptionQueryService> _mockQueryService;
        private readonly Mock<IPrescriptionBusinessService> _mockBusinessService;

        public PrescriptionServiceTests()
        {
            _mockQueryService = new Mock<IPrescriptionQueryService>();
            _mockBusinessService = new Mock<IPrescriptionBusinessService>();
            _prescriptionService = new PrescriptionService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new PrescriptionService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new PrescriptionService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new PrescriptionQueryDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<PrescriptionDto>>.Success(new PagedResult<PrescriptionDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(It.IsAny<PrescriptionQueryDto>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(It.Is<PrescriptionQueryDto>(q => q.PageIndex == 1 && q.PageSize == 10)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescriptionDto = new PrescriptionDto { Id = prescriptionId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(prescriptionDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(prescriptionId), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto { ConsultationId = Guid.NewGuid() };
            var createdPrescription = new PrescriptionDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(createdPrescription);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var updateDto = new PrescriptionEditDto { Id = prescriptionId };
            var updatedPrescription = new PrescriptionDto { Id = prescriptionId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(updatedPrescription);

            _mockBusinessService.Setup(x => x.UpdateAsync(prescriptionId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.UpdateAsync(prescriptionId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(prescriptionId, updateDto), Times.Once);
        }

        #endregion

        // Note: 处方项操作和兼容性检查方法在当前接口中不存在，已移除相关测试

        #region 边界值测试

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.DeleteAsync(prescriptionId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.DeleteAsync(prescriptionId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(prescriptionId), Times.Once);
        }

        #endregion

        #region 查询方法测试

        [Fact]
        public async Task GetByPatientIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionDto> { new PrescriptionDto { PatientId = patientId } };
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescription = new PrescriptionDto { MedicalCaseId = medicalCaseId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(prescription);

            _mockQueryService.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByMedicalCaseIdAsync(medicalCaseId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "患者名";
            var prescriptions = new List<PrescriptionDto> { new PrescriptionDto { PatientName = "患者名" } };
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.SearchAsync(keyword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_All_Prescriptions()
        {
            // Arrange
            var prescriptions = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = Guid.NewGuid() },
                new PrescriptionDto { Id = Guid.NewGuid() }
            };
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDoctorTodayPrescriptionsAsync_Should_Return_Today_Prescriptions()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionDto>
            {
                new PrescriptionDto { UserId = doctorId, CreatedAt = DateTime.Today }
            };
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService.Setup(x => x.GetDoctorTodayPrescriptionsAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetDoctorTodayPrescriptionsAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            _mockQueryService.Verify(x => x.GetDoctorTodayPrescriptionsAsync(doctorId), Times.Once);
        }

        #endregion

        #region 验证功能测试

        [Fact]
        public async Task ValidateAsync_Should_Check_Price_Precision()
        {
            // Arrange
            var prescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                TotalAmount = 123.456m, // 3位小数，应该失败
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto { UnitPrice = 10.00m, Quantity = 2 }
                }
            };

            // Act
            var result = await _prescriptionService.ValidateAsync(prescription);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("价格精度");
        }

        [Fact]
        public async Task ValidateAsync_Should_Check_Compatibility()
        {
            // Arrange
            var prescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                TotalAmount = 100.00m,
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto { HerbId = Guid.NewGuid(), HerbName = "甘草" },
                    new PrescriptionItemDto { HerbId = Guid.NewGuid(), HerbName = "甘遂" } // 十八反
                }
            };

            // Act
            var result = await _prescriptionService.ValidateAsync(prescription);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("配伍禁忌");
        }

        [Fact]
        public async Task ValidateAsync_Should_Check_Total_Amount()
        {
            // Arrange
            var prescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                TotalAmount = 100.00m,
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto { UnitPrice = 10.00m, Quantity = 5, Amount = 50.00m },
                    new PrescriptionItemDto { UnitPrice = 20.00m, Quantity = 3, Amount = 60.00m }
                }
            };

            // Act
            var result = await _prescriptionService.ValidateAsync(prescription);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("总金额不匹配"); // 110 != 100
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_Valid_Prescription()
        {
            // Arrange
            var prescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                TotalAmount = 70.00m,
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto { UnitPrice = 10.00m, Quantity = 3, Amount = 30.00m },
                    new PrescriptionItemDto { UnitPrice = 20.00m, Quantity = 2, Amount = 40.00m }
                }
            };

            // Act
            var result = await _prescriptionService.ValidateAsync(prescription);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region 复制功能测试

        [Fact]
        public async Task CopyAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var sourceId = Guid.NewGuid();
            var targetPatientId = Guid.NewGuid();
            var copiedPrescription = new PrescriptionDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(copiedPrescription);

            _mockBusinessService.Setup(x => x.CopyAsync(sourceId, targetPatientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CopyAsync(sourceId, targetPatientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CopyAsync(sourceId, targetPatientId), Times.Once);
        }

        [Fact]
        public async Task CopyLastPrescriptionAsync_Should_Copy_Most_Recent()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var copiedPrescription = new PrescriptionDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(copiedPrescription);

            _mockBusinessService.Setup(x => x.CopyLastPrescriptionAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CopyLastPrescriptionAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockBusinessService.Verify(x => x.CopyLastPrescriptionAsync(patientId), Times.Once);
        }

        #endregion

        #region 快速保存和取消测试

        [Fact]
        public async Task QuickSaveAsync_Should_Save_Prescription_Quickly()
        {
            // Arrange
            var dto = new PrescriptionQuickSaveDto
            {
                PatientId = Guid.NewGuid(),
                Items = new List<PrescriptionItemDto>()
            };
            var savedPrescription = new PrescriptionDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(savedPrescription);

            _mockBusinessService.Setup(x => x.QuickSaveAsync(dto))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.QuickSaveAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockBusinessService.Verify(x => x.QuickSaveAsync(dto), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_Should_Cancel_Prescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var reason = "患者要求取消";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.CancelAsync(prescriptionId, reason))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CancelAsync(prescriptionId, reason);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockBusinessService.Verify(x => x.CancelAsync(prescriptionId, reason), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_Should_Handle_Empty_Reason()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Failure("取消原因不能为空");

            _mockBusinessService.Setup(x => x.CancelAsync(prescriptionId, string.Empty))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CancelAsync(prescriptionId, string.Empty);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("取消原因不能为空");
        }

        #endregion

        #region 边界值测试

        [Fact]
        public void PrescriptionService_Should_Implement_IPrescriptionService()
        {
            _prescriptionService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IPrescriptionService>();
        }

        #endregion
    }
}