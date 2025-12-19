using System;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Mapping
{
    /// <summary>
    /// Prescriptions模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除，通过MedicalCaseId关联获取
    /// </summary>
    public class PrescriptionMappingProfileTests
    {
        private readonly IMapper _mapper;

        public PrescriptionMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PrescriptionMappingProfile());
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PrescriptionMappingProfile());
            });

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_Prescription_To_PrescriptionDetailDto_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            // OpenSpec: simplify-medicalcase-dataflow - Indication/FormulaSource已移除
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                DosageCount = 7,
                Discount = 0.8m,
                Advice = "饭后服用",
                Remark = "温服"
            };

            // Act
            var dto = _mapper.Map<PrescriptionDetailDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(prescription.Id);
            dto.MedicalCaseId.Should().Be(prescription.MedicalCaseId);
            dto.DosageCount.Should().Be(prescription.DosageCount);
            dto.Discount.Should().Be(prescription.Discount);
            dto.Advice.Should().Be(prescription.Advice);
            dto.Remark.Should().Be(prescription.Remark);

            // 验证计算属性被忽略
            dto.SingleDosePrice.Should().Be(0);
            dto.TotalPrice.Should().Be(0);
            dto.TotalWeight.Should().Be(0);
        }

        [Fact]
        public void Map_Prescription_To_PrescriptionDetailDto_WithDifferentData_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            // OpenSpec: simplify-medicalcase-dataflow - Indication/FormulaSource已移除
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                DosageCount = 14,
                Discount = 0.9m,
                Advice = "温服",
                Remark = "体质虚寒者适用"
            };

            // Act
            var detailDto = _mapper.Map<PrescriptionDetailDto>(prescription);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(prescription.Id);
            detailDto.MedicalCaseId.Should().Be(prescription.MedicalCaseId);
            detailDto.DosageCount.Should().Be(prescription.DosageCount);
            detailDto.Discount.Should().Be(prescription.Discount);
            detailDto.Advice.Should().Be(prescription.Advice);
            detailDto.Remark.Should().Be(prescription.Remark);

            // 验证计算属性被忽略
            detailDto.SingleDosePrice.Should().Be(0);
            detailDto.TotalPrice.Should().Be(0);
            detailDto.TotalWeight.Should().Be(0);
        }

        [Fact]
        public void Map_PrescriptionItem_To_PrescriptionItemDto_Should_Success()
        {
            // Arrange
            var prescriptionItem = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                HerbId = Guid.NewGuid(),
                HerbName = "当归",
                Dosage = 10,
                Unit = "g",
                UnitPrice = 0.5m,
                Remark = "酒制"
            };

            // Act
            var dto = _mapper.Map<PrescriptionItemDto>(prescriptionItem);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(prescriptionItem.Id);
            dto.HerbId.Should().Be(prescriptionItem.HerbId);
            dto.HerbName.Should().Be(prescriptionItem.HerbName);
            dto.Dosage.Should().Be(prescriptionItem.Dosage);
            dto.Unit.Should().Be(prescriptionItem.Unit);
            dto.UnitPrice.Should().Be(prescriptionItem.UnitPrice);
            dto.Remark.Should().Be(prescriptionItem.Remark);
        }

        [Fact]
        public void Map_PrescriptionInputDto_Create_To_Prescription_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/DoctorId/ConsultationId已移除，通过MedicalCaseId关联获取
            // OpenSpec: simplify-medicalcase-dataflow - Diagnosis/FormulaSource已移除
            var createDto = new PrescriptionInputDto
            {
                DosageCount = 5,
                Usage = "水煎服",
                TotalPrice = 125.50m,
                Advice = "温服",
                Remark = "调理脾胃"
            };

            // Act
            var prescription = _mapper.Map<Prescription>(createDto);

            // Assert
            prescription.Should().NotBeNull();
            prescription.DosageCount.Should().Be(createDto.DosageCount);

            // 验证忽略字段 - BaseEntity.Id 有默认初始化为 Guid.NewGuid()
            prescription.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Map_PrescriptionItemInputDto_To_PrescriptionItem_Should_Success()
        {
            // Arrange
            var createDto = new PrescriptionItemInputDto
            {
                HerbId = Guid.NewGuid(),
                HerbName = "黄芪",
                Dosage = 15,
                Unit = "g",
                UnitPrice = 0.8m,
                Usage = "炙制",
                Remark = "补气"
            };

            // Act
            var prescriptionItem = _mapper.Map<PrescriptionItem>(createDto);

            // Assert
            prescriptionItem.Should().NotBeNull();
            prescriptionItem.HerbId.Should().Be(createDto.HerbId);
            prescriptionItem.HerbName.Should().Be(createDto.HerbName);
            prescriptionItem.Dosage.Should().Be((int)createDto.Dosage);
            prescriptionItem.Unit.Should().Be(createDto.Unit);
            prescriptionItem.UnitPrice.Should().Be(createDto.UnitPrice);
            prescriptionItem.Remark.Should().Be(createDto.Remark);

            // 验证忽略字段
            prescriptionItem.Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public void Map_PrescriptionInputDto_Update_To_Prescription_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            // OpenSpec: simplify-medicalcase-dataflow - Diagnosis已移除
            var editDto = new PrescriptionInputDto
            {
                Id = Guid.NewGuid(),
                DosageCount = 10,
                TotalPrice = 200.0m,
                Discount = 0.85m,
                Advice = "饭前服用",
                Remark = "长期调理"
            };

            // Act
            var prescription = _mapper.Map<Prescription>(editDto);

            // Assert
            prescription.Should().NotBeNull();
            prescription.DosageCount.Should().Be(editDto.DosageCount);
            prescription.Discount.Should().Be(editDto.Discount);
            prescription.Advice.Should().Be(editDto.Advice);
            prescription.Remark.Should().Be(editDto.Remark);

            // 验证忽略字段 - BaseEntity.Id 有默认初始化为 Guid.NewGuid()
            prescription.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Map_Prescription_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            // OpenSpec: simplify-medicalcase-dataflow - Indication/FormulaSource已移除
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                Advice = null,
                Remark = null,
                DosageCount = 1
            };

            // Act
            var dto = _mapper.Map<PrescriptionDetailDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(prescription.Id);
            dto.MedicalCaseId.Should().Be(prescription.MedicalCaseId);
            dto.Advice.Should().BeNull();
            dto.Remark.Should().BeNull();
            dto.DosageCount.Should().Be(prescription.DosageCount);
        }

        [Fact]
        public void Map_PrescriptionItem_With_ZeroPrice_Should_Success()
        {
            // Arrange
            var prescriptionItem = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                HerbId = Guid.NewGuid(),
                HerbName = "免费药材",
                Dosage = 5,
                Unit = "g",
                UnitPrice = 0.0m,
                Remark = "免费提供"
            };

            // Act
            var dto = _mapper.Map<PrescriptionItemDto>(prescriptionItem);

            // Assert
            dto.Should().NotBeNull();
            dto.HerbName.Should().Be("免费药材");
            dto.Dosage.Should().Be(5);
            dto.UnitPrice.Should().Be(0.0m);
            dto.Remark.Should().Be("免费提供");
        }

        [Fact]
        public void Map_Prescription_With_HighDiscount_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            // OpenSpec: simplify-medicalcase-dataflow - Indication已移除
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                Discount = 0.5m, // 5折
                DosageCount = 20,
                Remark = "长期调理处方"
            };

            // Act
            var dto = _mapper.Map<PrescriptionDetailDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            dto.Discount.Should().Be(0.5m);
            dto.DosageCount.Should().Be(20);
            dto.Remark.Should().Be("长期调理处方");
        }

        [Fact]
        public void Map_PrescriptionInputDto_With_MinimalData_Should_Success()
        {
            // Arrange
            // OpenSpec: optimize-entity-data-flow - PatientId/DoctorId已移除，Quantity替代DosageCount
            // OpenSpec: simplify-medicalcase-dataflow - Diagnosis已移除
            var createDto = new PrescriptionInputDto
            {
                DosageCount = 1,
                TotalPrice = 10.0m
            };

            // Act
            var prescription = _mapper.Map<Prescription>(createDto);

            // Assert
            prescription.Should().NotBeNull();
            prescription.DosageCount.Should().Be(createDto.DosageCount);
        }
    }
}
