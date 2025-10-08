using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// 处方服务单元测试
    /// 测试处方的创建、查询、更新、删除以及价格计算、打印格式生成等核心业务逻辑
    /// </summary>
    public class PrescriptionServiceTests : TestBase
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly Mock<IPrescriptionRepository> _repositoryMock;
        private readonly Mock<ILogger<PrescriptionService>> _loggerMock;

        public PrescriptionServiceTests()
        {
            _repositoryMock = CreateMock<IPrescriptionRepository>();
            _loggerMock = CreateLoggerMock<PrescriptionService>();

            _prescriptionService = new PrescriptionService(
                _repositoryMock.Object,
                Mapper,
                _loggerMock.Object);
        }


        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidParams_ShouldReturnPagedResult()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescriptions = new List<Prescription>
            {
                new Prescription
                {
                    Id = prescriptionId,
                    MedicalCaseId = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    DosageCount = 7,
                    Discount = 1.0m,
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<PrescriptionItem>
                    {
                        new PrescriptionItem
                        {
                            Id = Guid.NewGuid(),
                            HerbName = "柴胡",
                            Quantity = 12,
                            Unit = "g",
                            UnitPrice = 0.5m
                        }
                    }
                }
            };

            var pagedResult = new PagedResult<Prescription>
            {
                Items = prescriptions,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(1, 20, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(1, 20);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(1);

            _repositoryMock.Verify(x => x.GetPagedWithDetailsAsync(1, 20, null), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WithKeyword_ShouldReturnFilteredResult()
        {
            // Arrange
            var keyword = "柴胡";
            var pagedResult = new PagedResult<Prescription>
            {
                Items = new List<Prescription>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(1, 20, keyword))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(1, 20, keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(x => x.GetPagedWithDetailsAsync(1, 20, keyword), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                CreatedAt = DateTime.UtcNow,
                Items = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Quantity = 12,
                        Unit = "g",
                        UnitPrice = 0.5m
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
                .ReturnsAsync(prescription);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(prescriptionId);
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(nonExistentId))
                .ReturnsAsync((Prescription)null);

            // Act
            var result = await _prescriptionService.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("不存在");
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Quantity = 7,
                Usage = "水煎服，每日一剂",
                TotalAmount = 168.50m,
                Items = new List<PrescriptionItemCreateDto>
                {
                    new PrescriptionItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Quantity = 12,
                        Unit = "g",
                        UnitPrice = 0.5m,
                        Subtotal = 6m
                    }
                }
            };

            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                UserId = createDto.DoctorId,
                DosageCount = createDto.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Prescription>()))
                .ReturnsAsync(prescription);

            // Act
            var result = await _prescriptionService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Prescription>()), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var updateDto = new PrescriptionUpdateDto
            {
                Advice = "避风寒，多休息",
                Discount = 0.9m,
                Remark = "测试备注",
                DosageCount = 7
            };

            var existingPrescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(existingPrescription);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Prescription>()))
                .ReturnsAsync(existingPrescription);

            // Act
            var result = await _prescriptionService.UpdateAsync(prescriptionId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Prescription>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new PrescriptionUpdateDto
            {
                Advice = "测试医嘱",
                Discount = 0.9m
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Prescription)null!);

            // Act
            var result = await _prescriptionService.UpdateAsync(nonExistentId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("不存在");

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Prescription>()), Times.Never);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(prescriptionId))
                .ReturnsAsync(true);

            // Act
            var result = await _prescriptionService.DeleteAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _repositoryMock.Verify(x => x.DeleteAsync(prescriptionId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(prescriptionId))
                .ReturnsAsync(false);

            // Act
            var result = await _prescriptionService.DeleteAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("删除失败");
        }

        #endregion

        #region GetByMedicalCaseIdAsync Tests

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithValidId_ShouldReturnPrescriptions()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptions = new List<Prescription>
            {
                new Prescription
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCaseId,
                    PatientId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    DosageCount = 7,
                    Discount = 1.0m,
                    Items = new List<PrescriptionItem>()
                }
            };

            _repositoryMock.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithNoPrescriptions_ShouldReturnEmptyList()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(new List<Prescription>());

            // Act
            var result = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region RecalculatePriceAsync Tests

        [Fact]
        public async Task RecalculatePriceAsync_WithValidId_ShouldReturnRecalculatedPrice()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 0.9m,
                Items = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Quantity = 12,
                        Unit = "g",
                        UnitPrice = 0.5m
                    },
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "黄芩",
                        Quantity = 9,
                        Unit = "g",
                        UnitPrice = 0.8m
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
                .ReturnsAsync(prescription);

            // Act
            var result = await _prescriptionService.RecalculatePriceAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(prescriptionId);
            // 价格计算：(12 * 0.5 + 9 * 0.8) * 7 * 0.9 = (6 + 7.2) * 7 * 0.9 = 13.2 * 7 * 0.9 = 83.16
            result.Data.TotalPrice.Should().BeApproximately(83.16m, 0.01m);
        }

        [Fact]
        public async Task RecalculatePriceAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(nonExistentId))
                .ReturnsAsync((Prescription)null);

            // Act
            var result = await _prescriptionService.RecalculatePriceAsync(nonExistentId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("不存在");
        }

        #endregion

        #region GeneratePrintFormatAsync Tests

        [Fact]
        public async Task GeneratePrintFormatAsync_WithValidId_ShouldReturnPrintFormat()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Indication = "风寒感冒",
                DosageCount = 7,
                Discount = 1.0m,
                Advice = "忌生冷",
                CreatedAt = DateTime.UtcNow,
                Items = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Quantity = 12,
                        Unit = "g",
                        UnitPrice = 0.5m
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
                .ReturnsAsync(prescription);

            // Act
            var result = await _prescriptionService.GeneratePrintFormatAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data.Should().Contain("处方编号");
            result.Data.Should().Contain("药材清单");
            result.Data.Should().Contain("柴胡");
            result.Data.Should().Contain("帖数: 7 帖");
        }

        [Fact]
        public async Task GeneratePrintFormatAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(nonExistentId))
                .ReturnsAsync((Prescription)null);

            // Act
            var result = await _prescriptionService.GeneratePrintFormatAsync(nonExistentId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("不存在");
        }

        [Fact]
        public async Task GeneratePrintFormatAsync_WithDiscount_ShouldIncludeDiscountInfo()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 0.85m, // 85% 折扣
                CreatedAt = DateTime.UtcNow,
                Items = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Quantity = 12,
                        Unit = "g",
                        UnitPrice = 0.5m
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
                .ReturnsAsync(prescription);

            // Act
            var result = await _prescriptionService.GeneratePrintFormatAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("折扣");
            result.Data.Should().Contain("85%");
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
