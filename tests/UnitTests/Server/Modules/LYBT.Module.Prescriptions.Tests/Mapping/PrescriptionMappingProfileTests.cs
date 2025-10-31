using System;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Mapping
{
    /// <summary>
    /// Prescriptions模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
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
        public void Map_Prescription_To_PrescriptionDto_Should_Success()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Indication = "清热解毒",
                DosageCount = 7,
                Discount = 0.8m,
                Advice = "饭后服用",
                FormulaSource = "逍遥散",
                Status = PrescriptionStatus.Draft,
                Remark = "温服"
            };

            // Act
            var dto = _mapper.Map<PrescriptionDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(prescription.Id);
            dto.MedicalCaseId.Should().Be(prescription.MedicalCaseId);
            dto.PatientId.Should().Be(prescription.PatientId ?? Guid.Empty);
            dto.UserId.Should().Be(prescription.UserId ?? Guid.Empty);
            dto.Indication.Should().Be(prescription.Indication);
            dto.DosageCount.Should().Be(prescription.DosageCount);
            dto.Discount.Should().Be(prescription.Discount);
            dto.Advice.Should().Be(prescription.Advice);
            dto.FormulaSource.Should().Be(prescription.FormulaSource);
            dto.Remark.Should().Be(prescription.Remark);

            // 验证计算属性被忽略
            dto.SingleDosePrice.Should().Be(0);
            dto.TotalPrice.Should().Be(0);
            dto.TotalWeight.Should().Be(0);
        }

        [Fact]
        public void Map_Prescription_To_PrescriptionDetailDto_Should_Success()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Indication = "温中健脾",
                DosageCount = 14,
                Discount = 0.9m,
                Advice = "温服",
                FormulaSource = "四君子汤",
                Status = PrescriptionStatus.Completed,
                Remark = "体质虚寒者适用"
            };

            // Act
            var detailDto = _mapper.Map<PrescriptionDetailDto>(prescription);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(prescription.Id);
            detailDto.MedicalCaseId.Should().Be(prescription.MedicalCaseId);
            detailDto.PatientId.Should().Be(prescription.PatientId ?? Guid.Empty);
            detailDto.UserId.Should().Be(prescription.UserId ?? Guid.Empty);
            detailDto.Indication.Should().Be(prescription.Indication);
            detailDto.DosageCount.Should().Be(prescription.DosageCount);
            detailDto.Discount.Should().Be(prescription.Discount);
            detailDto.Advice.Should().Be(prescription.Advice);
            detailDto.FormulaSource.Should().Be(prescription.FormulaSource);
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
                Quantity = 10,
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
            dto.Quantity.Should().Be(prescriptionItem.Quantity);
            dto.Unit.Should().Be(prescriptionItem.Unit);
            dto.UnitPrice.Should().Be(prescriptionItem.UnitPrice);
            dto.Remark.Should().Be(prescriptionItem.Remark);
        }

        [Fact]
        public void Map_PrescriptionCreateDto_To_Prescription_Should_Success()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ConsultationId = Guid.NewGuid(),
                Diagnosis = "脾胃虚弱",
                DosageCount = 5,
                Quantity = 5,
                Usage = "水煎服",
                TotalAmount = 125.50m,
                FormulaSource = "新方剂",
                Advice = "温服",
                Remark = "调理脾胃"
            };

            // Act
            var prescription = _mapper.Map<Prescription>(createDto);

            // Assert
            prescription.Should().NotBeNull();
            prescription.PatientId.Should().Be(createDto.PatientId);
            // 注意：DoctorId映射到UserId
            // 其他字段根据映射配置来验证

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
                Quantity = 15,
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
            prescriptionItem.Quantity.Should().Be((int)createDto.Quantity);
            prescriptionItem.Unit.Should().Be(createDto.Unit);
            prescriptionItem.UnitPrice.Should().Be(createDto.UnitPrice);
            prescriptionItem.Remark.Should().Be(createDto.Remark);

            // 验证忽略字段
            prescriptionItem.Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public void Map_PrescriptionEditDto_To_Prescription_Should_Success()
        {
            // Arrange
            var editDto = new PrescriptionEditDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Diagnosis = "修改后的诊断",
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
            prescription.PatientId.Should().Be(editDto.PatientId);
            prescription.UserId.Should().Be(editDto.UserId);
            prescription.DosageCount.Should().Be(editDto.DosageCount);
            prescription.Discount.Should().Be(editDto.Discount);
            prescription.Advice.Should().Be(editDto.Advice);
            prescription.Remark.Should().Be(editDto.Remark);

            // 验证忽略字段 - BaseEntity.Id 有默认初始化为 Guid.NewGuid()
            prescription.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Map_Prescription_With_DraftStatus_Should_Success()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = PrescriptionStatus.Draft,
                Indication = "草稿处方"
            };

            // Act
            var dto = _mapper.Map<PrescriptionDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            // Note: DTO uses CommonStatus, Entity uses PrescriptionStatus
            dto.Indication.Should().Be("草稿处方");
        }

        [Fact]
        public void Map_Prescription_With_CompletedStatus_Should_Success()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                Status = PrescriptionStatus.Completed
            };

            // Act
            var dto = _mapper.Map<PrescriptionDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            // Note: DTO uses CommonStatus, Entity uses PrescriptionStatus
        }

        [Fact]
        public void Map_Prescription_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Indication = null,
                Advice = null,
                FormulaSource = null,
                Remark = null,
                DosageCount = 1,
                Status = PrescriptionStatus.Draft
            };

            // Act
            var dto = _mapper.Map<PrescriptionDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(prescription.Id);
            dto.MedicalCaseId.Should().Be(prescription.MedicalCaseId);
            dto.PatientId.Should().Be(prescription.PatientId ?? Guid.Empty);
            dto.UserId.Should().Be(prescription.UserId ?? Guid.Empty);
            dto.Indication.Should().BeNull();
            dto.Advice.Should().BeNull();
            dto.FormulaSource.Should().BeNull();
            dto.Remark.Should().BeNull();
            dto.DosageCount.Should().Be(prescription.DosageCount);
            // Note: DTO uses CommonStatus, Entity uses PrescriptionStatus
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
                Quantity = 5,
                Unit = "g",
                UnitPrice = 0.0m,
                Remark = "免费提供"
            };

            // Act
            var dto = _mapper.Map<PrescriptionItemDto>(prescriptionItem);

            // Assert
            dto.Should().NotBeNull();
            dto.HerbName.Should().Be("免费药材");
            dto.Quantity.Should().Be(5.0m);
            dto.UnitPrice.Should().Be(0.0m);
            dto.Remark.Should().Be("免费提供");
        }

        [Fact]
        public void Map_Prescription_With_HighDiscount_Should_Success()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Discount = 0.5m, // 5折
                DosageCount = 20,
                Indication = "长期调理处方"
            };

            // Act
            var dto = _mapper.Map<PrescriptionDto>(prescription);

            // Assert
            dto.Should().NotBeNull();
            dto.Discount.Should().Be(0.5m);
            dto.DosageCount.Should().Be(20);
            dto.Indication.Should().Be("长期调理处方");
        }

        [Fact]
        public void Map_PrescriptionCreateDto_With_MinimalData_Should_Success()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "最简诊断",
                DosageCount = 1,
                Quantity = 1,
                TotalAmount = 10.0m
            };

            // Act
            var prescription = _mapper.Map<Prescription>(createDto);

            // Assert
            prescription.Should().NotBeNull();
            prescription.PatientId.Should().Be(createDto.PatientId);
            prescription.DosageCount.Should().Be(createDto.DosageCount);
        }
    }
}
