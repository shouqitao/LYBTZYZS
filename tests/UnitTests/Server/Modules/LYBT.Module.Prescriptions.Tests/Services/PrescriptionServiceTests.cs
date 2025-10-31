// Issue #1601 Phase 1: 测试文件暂时禁用，等待Phase 2重构
#if FALSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
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
    /// Issue #1601 Phase 1: 测试暂时禁用，等待Phase 2重构为通过MedicalCase聚合根测试
    /// </summary>
    [Trait("Category", "Disabled")]
    public class PrescriptionServiceTests : TestBase
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly Mock<IPrescriptionRepository> _repositoryMock;
        private readonly Mock<IFormulaRepository> _formulaRepositoryMock;
        private readonly Mock<ILogger<PrescriptionService>> _loggerMock;

        public PrescriptionServiceTests()
        {
            _repositoryMock = CreateMock<IPrescriptionRepository>();
            _formulaRepositoryMock = CreateMock<IFormulaRepository>();
            var medicalCaseRepositoryMock = CreateMock<LYBT.Module.MedicalCase.Interfaces.IMedicalCaseRepository>();
            var patientRepositoryMock = CreateMock<LYBT.Module.Patients.Interfaces.IPatientRepository>();
            var consultationRepositoryMock = CreateMock<LYBT.Module.Consultation.Interfaces.IConsultationRepository>();
            var numberServiceMock = CreateMock<IPrescriptionNumberService>();
            _loggerMock = CreateLoggerMock<PrescriptionService>();

            // Issue #1551: Mock编号生成服务
            numberServiceMock.Setup(x => x.GenerateNumberAsync(It.IsAny<DateTime>()))
                .ReturnsAsync((DateTime date) => $"RX-{date:yyyyMMdd}-0001");

            _prescriptionService = new PrescriptionService(
                _repositoryMock.Object,
                _formulaRepositoryMock.Object,
                medicalCaseRepositoryMock.Object,
                patientRepositoryMock.Object,
                consultationRepositoryMock.Object,
                numberServiceMock.Object,
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
            result.Data!.Items.Should().HaveCount(1);
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
            result.Data!.Id.Should().Be(prescriptionId);
            result.Data.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(nonExistentId))
                .ReturnsAsync((Prescription?)null);

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
                Items = new List<PrescriptionItemInputDto>
                {
                    new PrescriptionItemInputDto
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
            result.Data!.Id.Should().Be(prescriptionId);
            // 价格计算：(12 * 0.5 + 9 * 0.8) * 7 * 0.9 = (6 + 7.2) * 7 * 0.9 = 13.2 * 7 * 0.9 = 83.16
            result.Data.TotalPrice.Should().BeApproximately(83.16m, 0.01m);
        }

        [Fact]
        public async Task RecalculatePriceAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(nonExistentId))
                .ReturnsAsync((Prescription?)null);

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
                .ReturnsAsync((Prescription?)null);

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

        #region SearchPrescriptionsAsync Tests

        [Fact]
        public async Task SearchPrescriptionsAsync_WithNoParameters_ShouldReturnEmptyList()
        {
            // Arrange - 无需参数

            // Act
            var result = await _prescriptionService.SearchPrescriptionsAsync(null, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchPrescriptionsAsync_WithEmptyStrings_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyPatientName = "   ";
            var emptySymptomKeyword = "";

            // Act
            var result = await _prescriptionService.SearchPrescriptionsAsync(emptyPatientName, emptySymptomKeyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        // Note: 完整的集成测试将在IntegrationTests项目中进行
        // 这里的单元测试仅验证基本逻辑和边界条件

        #endregion

        #region ImportFormulaIntoPrescriptionAsync Tests (Issue #1472 FORMULA-7)

        [Fact]
        public async Task ImportFormulaIntoPrescriptionAsync_WithNonExistentFormula_ShouldReturnFailure()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var nonExistentFormulaId = Guid.NewGuid();

            _formulaRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentFormulaId))
                .ReturnsAsync((LYBT.Entities.Formula.Formula?)null);

            // Act
            var result = await _prescriptionService.ImportFormulaIntoPrescriptionAsync(prescriptionId, nonExistentFormulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("验方不存在");

            // 验证未调用处方查询（验方检查在前）
            _repositoryMock.Verify(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ImportFormulaIntoPrescriptionAsync_WithDraftFormula_ShouldReturnFailure()
        {
            // Arrange - Issue #1472 (FORMULA-7): 验证Draft状态验方不能导入
            var prescriptionId = Guid.NewGuid();
            var formulaId = Guid.NewGuid();

            var draftFormula = new LYBT.Entities.Formula.Formula
            {
                Id = formulaId,
                Name = "未校验验方",
                ValidationStatus = FormulaValidationStatus.Draft, // ⭐ 核心测试点
                Herbs = new List<LYBT.Entities.Formula.FormulaHerbItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbId = null, // 未验证，HerbId为null
                        OriginalHerbName = "未知药材A",
                        IsValidated = false, // ⭐ 未验证
                        Quantity = 10
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbId = null,
                        OriginalHerbName = "未知药材B",
                        IsValidated = false, // ⭐ 未验证
                        Quantity = 15
                    }
                }
            };

            _formulaRepositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(draftFormula);

            // Act
            var result = await _prescriptionService.ImportFormulaIntoPrescriptionAsync(prescriptionId, formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("包含未校验的药材");
            result.ErrorMessage.Should().Contain("未知药材A");
            result.ErrorMessage.Should().Contain("未知药材B");

            // 验证未调用处方查询和更新（验方状态检查失败）
            _repositoryMock.Verify(x => x.GetByIdWithItemsAsync(It.IsAny<Guid>()), Times.Never);
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Prescription>()), Times.Never);
        }

        [Fact]
        public async Task ImportFormulaIntoPrescriptionAsync_WithNonExistentPrescription_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentPrescriptionId = Guid.NewGuid();
            var formulaId = Guid.NewGuid();

            var validatedFormula = new LYBT.Entities.Formula.Formula
            {
                Id = formulaId,
                Name = "六味地黄丸",
                ValidationStatus = FormulaValidationStatus.Validated, // 已验证
                Herbs = new List<LYBT.Entities.Formula.FormulaHerbItem>()
            };

            _formulaRepositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(validatedFormula);

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(nonExistentPrescriptionId))
                .ReturnsAsync((Prescription?)null);

            // Act
            var result = await _prescriptionService.ImportFormulaIntoPrescriptionAsync(nonExistentPrescriptionId, formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("处方不存在");

            // 验证未调用更新
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Prescription>()), Times.Never);
        }

        [Fact]
        public async Task ImportFormulaIntoPrescriptionAsync_WithValidatedFormula_ShouldImportSuccessfully()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var formulaId = Guid.NewGuid();
            var herbId1 = Guid.NewGuid();
            var herbId2 = Guid.NewGuid();

            var validatedFormula = new LYBT.Entities.Formula.Formula
            {
                Id = formulaId,
                Name = "六味地黄丸",
                ValidationStatus = FormulaValidationStatus.Validated, // ⭐ 已验证
                Herbs = new List<LYBT.Entities.Formula.FormulaHerbItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbId = herbId1,
                        OriginalHerbName = "熟地黄",
                        IsValidated = true, // ⭐ 已验证
                        Quantity = 24,
                        Unit = "g",
                        Usage = "煎服"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbId = herbId2,
                        OriginalHerbName = "山药",
                        IsValidated = true, // ⭐ 已验证
                        Quantity = 12,
                        Unit = "g",
                        Usage = "煎服"
                    }
                }
            };

            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                ReferencedFormulas = string.Empty, // 初始为空
                Items = new List<PrescriptionItem>()
            };

            _formulaRepositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(validatedFormula);

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
                .ReturnsAsync(prescription);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Prescription>()))
                .ReturnsAsync((Prescription p) => p);

            // Act
            var result = await _prescriptionService.ImportFormulaIntoPrescriptionAsync(prescriptionId, formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("六味地黄丸");
            result.Message.Should().Contain("2味药材");

            // 验证处方项被添加
            prescription.Items.Should().HaveCount(2);
            prescription.Items.Should().Contain(item => item.HerbId == herbId1 && item.HerbName == "熟地黄" && item.Quantity == 24);
            prescription.Items.Should().Contain(item => item.HerbId == herbId2 && item.HerbName == "山药" && item.Quantity == 12);

            // 验证ReferencedFormulas字段更新
            prescription.ReferencedFormulas.Should().Be("六味地黄丸");

            // 验证调用了更新
            _repositoryMock.Verify(x => x.UpdateAsync(It.Is<Prescription>(p => p.Id == prescriptionId)), Times.Once);
        }

        [Fact]
        public async Task ImportFormulaIntoPrescriptionAsync_WithDuplicateFormula_ShouldNotDuplicateReference()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var formulaId = Guid.NewGuid();

            var validatedFormula = new LYBT.Entities.Formula.Formula
            {
                Id = formulaId,
                Name = "逍遥散",
                ValidationStatus = FormulaValidationStatus.Validated,
                Herbs = new List<LYBT.Entities.Formula.FormulaHerbItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        OriginalHerbName = "柴胡",
                        IsValidated = true,
                        Quantity = 10,
                        Unit = "g"
                    }
                }
            };

            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                ReferencedFormulas = "逍遥散,归脾汤", // 已包含"逍遥散"
                Items = new List<PrescriptionItem>()
            };

            _formulaRepositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(validatedFormula);

            _repositoryMock.Setup(x => x.GetByIdWithItemsAsync(prescriptionId))
                .ReturnsAsync(prescription);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Prescription>()))
                .ReturnsAsync((Prescription p) => p);

            // Act
            var result = await _prescriptionService.ImportFormulaIntoPrescriptionAsync(prescriptionId, formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            // 验证ReferencedFormulas字段未重复添加
            prescription.ReferencedFormulas.Should().Be("逍遥散,归脾汤"); // 未追加
            prescription.ReferencedFormulas.Split(',').Should().HaveCount(2); // 仍然是2个验方
        }

        #endregion

        #region Business Rules Tests (Issue #1423 - RULE-3)

        /// <summary>
        /// RULE-3: 当天可改隔日锁定 - 创建当天可以修改
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenCreatedToday_ShouldReturnSuccess()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var existingPrescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                Advice = "原始医嘱",
                CreatedAt = DateTime.Today.AddHours(10) // 今天创建
            };

            var updateDto = new PrescriptionUpdateDto
            {
                Advice = "更新后的医嘱",
                Discount = 0.9m,
                Remark = "更新备注",
                DosageCount = 10
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

        /// <summary>
        /// RULE-3: 当天可改隔日锁定 - 隔日后不可修改
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenCreatedYesterday_ShouldReturnFailure()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var existingPrescription = new Prescription
            {
                Id = prescriptionId,
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 1.0m,
                Advice = "原始医嘱",
                CreatedAt = DateTime.Today.AddDays(-1).AddHours(10) // 昨天创建
            };

            var updateDto = new PrescriptionUpdateDto
            {
                Advice = "尝试更新的医嘱",
                Discount = 0.9m
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(existingPrescription);

            // Act
            var result = await _prescriptionService.UpdateAsync(prescriptionId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("已超过可修改期限");
            result.Message.Should().Contain("仅限创建当天可修改");

            // 验证UpdateAsync不应被调用
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Prescription>()), Times.Never);
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
#endif
