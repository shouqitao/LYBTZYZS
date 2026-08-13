using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;

namespace LYBT.Tests.Server;

/// <summary>
/// MedicalCaseMapper 单元测试
/// 测试 Mapperly 编译时生成的映射逻辑 + MapToMedicalCaseDetailDto 手动丰富
/// AntiMock: 纯映射测试，无依赖
/// </summary>
public class MedicalCaseMapperTests
{
    private readonly MedicalCaseMapper _mapper = new();

    #region ToListDto 测试

    [Fact]
    public void ToListDto_WithValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateTestMedicalCase();

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.PatientId.Should().Be(entity.PatientId);
        dto.PatientName.Should().Be(entity.PatientName);
        dto.UserId.Should().Be(entity.UserId);
        dto.DoctorName.Should().Be(entity.DoctorName);
        dto.CompletedAt.Should().Be(entity.CompletedAt);
        dto.CaseStatus.Should().Be(entity.CaseStatus);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
    }

    [Fact]
    public void ToListDto_ShouldIgnoreComputedFields()
    {
        // Arrange
        var entity = CreateTestMedicalCase();
        entity.CaseNumber = "MC-2025-001";

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert - Mapperly忽略的字段应保持默认值
        dto.CaseNumber.Should().BeNull(); // MapperIgnoreTarget
        dto.PatientGender.Should().Be(default(Gender)); // MapperIgnoreTarget
        dto.PatientAge.Should().BeNull(); // MapperIgnoreTarget
        dto.Diagnosis.Should().BeNull(); // MapperIgnoreTarget
        dto.HasConsultation.Should().BeFalse(); // MapperIgnoreTarget
        dto.HasPrescription.Should().BeFalse(); // MapperIgnoreTarget
    }

    #endregion

    #region ToListDtos 测试

    [Fact]
    public void ToListDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<MedicalCaseEntity>
        {
            CreateTestMedicalCase(),
            CreateTestMedicalCase(),
            CreateTestMedicalCase()
        };

        // Act
        var dtos = _mapper.ToListDtos(entities);

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Id.Should().Be(entities[0].Id);
        dtos[1].Id.Should().Be(entities[1].Id);
        dtos[2].Id.Should().Be(entities[2].Id);
    }

    [Fact]
    public void ToListDtos_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var entities = new List<MedicalCaseEntity>();

        // Act
        var dtos = _mapper.ToListDtos(entities);

        // Assert
        dtos.Should().BeEmpty();
    }

    #endregion

    #region ToDetailDto 测试

    [Fact]
    public void ToDetailDto_WithValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateTestMedicalCase();

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.PatientId.Should().Be(entity.PatientId);
        dto.PatientName.Should().Be(entity.PatientName);
        dto.UserId.Should().Be(entity.UserId);
        dto.DoctorName.Should().Be(entity.DoctorName);
        dto.CompletedAt.Should().Be(entity.CompletedAt);
        dto.CaseStatus.Should().Be(entity.CaseStatus);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
    }

    [Fact]
    public void ToDetailDto_ShouldIgnoreNestedAndComputedFields()
    {
        // Arrange
        var entity = CreateTestMedicalCase();

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert - Mapperly忽略的字段应保持默认值
        dto.CaseNumber.Should().BeNull();
        dto.PatientGender.Should().Be(default(Gender));
        dto.PatientAge.Should().BeNull();
        dto.Diagnosis.Should().BeNull();
        dto.PresentIllness.Should().BeNull();
        dto.ConsultationId.Should().BeNull();
        dto.PrescriptionId.Should().BeNull();
        dto.Consultation.Should().BeNull();
        dto.Prescription.Should().BeNull();
    }

    #endregion

    #region ToDetailDtos 测试

    [Fact]
    public void ToDetailDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<MedicalCaseEntity>
        {
            CreateTestMedicalCase(),
            CreateTestMedicalCase()
        };

        // Act
        var dtos = _mapper.ToDetailDtos(entities);

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(entities[0].Id);
        dtos[1].Id.Should().Be(entities[1].Id);
    }

    #endregion

    #region ToConsultationDetailDto 测试

    [Fact]
    public void ToConsultationDetailDto_WithValidEntity_ShouldMapProperties()
    {
        // Arrange
        var entity = CreateTestConsultation();

        // Act
        var dto = _mapper.ToConsultationDetailDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.PresentIllness.Should().Be(entity.PresentIllness);
        dto.TongueDiagnosis.Should().Be(entity.TongueDiagnosis);
        dto.PulseDiagnosis.Should().Be(entity.PulseDiagnosis);
        dto.TcmDiagnosis.Should().Be(entity.TcmDiagnosis);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
        dto.CreatedBy.Should().Be(entity.CreatedBy);
    }

    [Fact]
    public void ToConsultationDetailDto_ShouldMapIdToMedicalCaseId()
    {
        // Arrange - MapProperty: Id → MedicalCaseId
        var entity = CreateTestConsultation();

        // Act
        var dto = _mapper.ToConsultationDetailDto(entity);

        // Assert
        dto.MedicalCaseId.Should().Be(entity.Id);
    }

    [Fact]
    public void ToConsultationDetailDto_ShouldIgnoreContextFields()
    {
        // Arrange
        var entity = CreateTestConsultation();

        // Act
        var dto = _mapper.ToConsultationDetailDto(entity);

        // Assert - 这些字段由EnrichConsultationDetailDto手动填充
        dto.PatientId.Should().Be(Guid.Empty);
        dto.UserId.Should().Be(Guid.Empty);
        dto.PatientName.Should().BeNull();
        dto.DoctorName.Should().BeNull();
    }

    #endregion

    #region ToPrescriptionDetailDto 测试

    [Fact]
    public void ToPrescriptionDetailDto_WithValidEntity_ShouldMapProperties()
    {
        // Arrange
        var entity = CreateTestPrescription();

        // Act
        var dto = _mapper.ToPrescriptionDetailDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.PrescriptionNumber.Should().Be(entity.PrescriptionNumber);
        dto.MedicalCaseId.Should().Be(entity.MedicalCaseId);
        dto.DosageCount.Should().Be(entity.DosageCount);
        dto.Usage.Should().Be(entity.Usage);
        dto.Advice.Should().Be(entity.Advice);
        dto.ReferencedFormulas.Should().Be(entity.ReferencedFormulas);
        dto.Discount.Should().Be(entity.Discount);
        dto.Remark.Should().Be(entity.Remark);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToPrescriptionDetailDto_ShouldIgnoreComputedFields()
    {
        // Arrange
        var entity = CreateTestPrescription();

        // Act
        var dto = _mapper.ToPrescriptionDetailDto(entity);

        // Assert - Mapperly忽略的计算字段应保持默认值
        dto.SingleDosePrice.Should().Be(0);
        dto.TotalPrice.Should().Be(0);
        dto.TotalWeight.Should().Be(0);
        dto.DuplicateWarning.Should().BeNull();
        dto.MissingDrugWarning.Should().BeNull();
        dto.Status.Should().Be(default(CommonStatus));
        dto.Items.Should().BeEmpty();
    }

    #endregion

    #region ToPrescriptionItemDto 测试

    [Fact]
    public void ToPrescriptionItemDto_WithValidEntity_ShouldMapProperties()
    {
        // Arrange
        var entity = CreateTestPrescriptionItem();

        // Act
        var dto = _mapper.ToPrescriptionItemDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.HerbId.Should().Be(entity.HerbId);
        dto.HerbName.Should().Be(entity.HerbName);
        dto.Unit.Should().Be(entity.Unit);
        dto.UnitPrice.Should().Be(entity.UnitPrice);
        dto.Dosage.Should().Be(entity.Dosage);
        dto.Usage.Should().Be(entity.Usage);
        dto.DecocteMethod.Should().Be(entity.DecocteMethod);
        dto.Remark.Should().Be(entity.Remark);
    }

    [Fact]
    public void ToPrescriptionItemDto_ShouldIgnoreComputedFields()
    {
        // Arrange
        var entity = CreateTestPrescriptionItem();

        // Act
        var dto = _mapper.ToPrescriptionItemDto(entity);

        // Assert - Mapperly忽略的计算字段应保持默认值
        dto.TotalPrice.Should().Be(0);
        dto.TotalWeight.Should().Be(0);
        dto.Subtotal.Should().Be(0);
        dto.Notes.Should().Be(entity.Remark); // Notes maps from Remark
    }

    #endregion

    #region MapToMedicalCaseDetailDto 测试（完整版）

    [Fact]
    public void MapToMedicalCaseDetailDto_WithFullNavigationProperties_ShouldMapAll()
    {
        // Arrange
        var entity = CreateTestMedicalCaseWithNavigations();

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - 基础字段
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.PatientId.Should().Be(entity.PatientId);
        dto.PatientName.Should().Be(entity.PatientName);
        dto.UserId.Should().Be(entity.UserId);
        dto.DoctorName.Should().Be(entity.DoctorName);

        // Assert - 手动丰富字段
        dto.CaseNumber.Should().Be(entity.CaseNumber);
        dto.Diagnosis.Should().Be(entity.Consultation!.TcmDiagnosis);
        dto.PresentIllness.Should().Be(entity.Consultation!.PresentIllness);
        dto.ConsultationId.Should().Be(entity.Id); // 共享主键
        dto.PrescriptionId.Should().Be(entity.Prescription!.Id);
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithConsultation_ShouldEnrichConsultationDto()
    {
        // Arrange
        var entity = CreateTestMedicalCaseWithNavigations();

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - Consultation嵌套DTO
        dto.Consultation.Should().NotBeNull();
        dto.Consultation!.MedicalCaseId.Should().Be(entity.Id);
        dto.Consultation.PatientId.Should().Be(entity.PatientId);
        dto.Consultation.UserId.Should().Be(entity.UserId);
        dto.Consultation.PatientName.Should().Be(entity.PatientName);
        dto.Consultation.DoctorName.Should().Be(entity.DoctorName);
        dto.Consultation.PresentIllness.Should().Be(entity.Consultation!.PresentIllness);
        dto.Consultation.TcmDiagnosis.Should().Be(entity.Consultation!.TcmDiagnosis);
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithPrescription_ShouldEnrichPrescriptionDto()
    {
        // Arrange
        var entity = CreateTestMedicalCaseWithNavigations();

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - Prescription嵌套DTO
        dto.Prescription.Should().NotBeNull();
        dto.Prescription!.MedicalCaseId.Should().Be(entity.Id);
        dto.Prescription.Items.Should().NotBeEmpty();
        dto.Prescription.TotalWeight.Should().BeGreaterThan(0);
        dto.Prescription.Status.Should().Be(CommonStatus.Enabled);
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithPrescription_ShouldComputeItemPriceSnapshot()
    {
        // Arrange —— 真机复现路径：保存处方 → 详情查询（价格快照 A2 决策 + US-MC-002）
        // 柴胡 10g × 5.0 = 50.0；白芍 10g × 5.0 = 50.0；DosageCount=7、Discount=1.0
        var entity = CreateTestMedicalCaseWithNavigations();

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - 明细金额快照（2026-08-13 修复：原 Mapperly 忽略 Subtotal/TotalPrice 恒 0）
        var item1 = dto.Prescription!.Items![0];
        item1.Subtotal.Should().Be(50.0m, "明细小计 = 单价 × 剂量（单帖，文档 §721 Amount）——柴胡 10g×5.0");
        item1.TotalPrice.Should().Be(350.0m, "明细总价 = 单帖小计 × 剂数——50.0 × 7");
        var item2 = dto.Prescription!.Items![1];
        item2.Subtotal.Should().Be(50.0m, "白芍 10g×5.0");
        item2.TotalPrice.Should().Be(350.0m);

        // 处方级汇总（既有语义不变）：SingleDosePrice=100、TotalPrice=100×7×1.0=700
        dto.Prescription.SingleDosePrice.Should().Be(100.0m);
        dto.Prescription.TotalPrice.Should().Be(700.0m);
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithPrescriptionDiscount_ItemPriceExcludesDiscount()
    {
        // Arrange —— 折扣为处方级 MC-D14 整体概念，不摊入明细
        var entity = CreateTestMedicalCaseWithNavigations();
        entity.Prescription!.Discount = 0.9m;

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - 明细不受折扣影响；处方级总价含折扣
        var item = dto.Prescription!.Items![0];
        item.Subtotal.Should().Be(50.0m);
        item.TotalPrice.Should().Be(350.0m);
        dto.Prescription.SingleDosePrice.Should().Be(100.0m);
        dto.Prescription.TotalPrice.Should().Be(630.0m, "100 × 7 × 0.9（MC-D14）");
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithoutConsultation_ShouldBeNull()
    {
        // Arrange
        var entity = CreateTestMedicalCase();
        entity.Consultation = null;

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert
        dto.Consultation.Should().BeNull();
        dto.ConsultationId.Should().BeNull();
        dto.Diagnosis.Should().BeNull();
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithoutPrescription_ShouldBeNull()
    {
        // Arrange
        var entity = CreateTestMedicalCase();
        entity.Prescription = null;

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert
        dto.Prescription.Should().BeNull();
        dto.PrescriptionId.Should().BeNull();
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_WithDeletedPrescription_ShouldBeNull()
    {
        // Arrange
        var entity = CreateTestMedicalCaseWithNavigations();
        entity.Prescription!.IsDeleted = true;

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - 已删除的处方不应映射
        dto.Prescription.Should().BeNull();
        dto.PrescriptionId.Should().BeNull();
    }

    [Fact]
    public void MapToMedicalCaseDetailDto_PrescriptionCalculation_ShouldBeCorrect()
    {
        // Arrange
        var entity = CreateTestMedicalCaseWithNavigations();
        // Prescription.DosageCount = 7, Discount = 1.0
        // Items: 2 items, each Dosage=10, UnitPrice=5.0
        // SingleDosePrice = 5*10 + 5*10 = 100
        // TotalPrice = 100 * 7 * 1.0 = 700

        // Act
        var dto = _mapper.MapToMedicalCaseDetailDto(entity);

        // Assert - 计算字段
        dto.Prescription!.SingleDosePrice.Should().Be(100m);
        dto.Prescription.TotalPrice.Should().Be(700m);
        dto.Prescription.TotalWeight.Should().Be(20m);
    }

    #endregion

    #region Helper Methods

    private static MedicalCaseEntity CreateTestMedicalCase()
    {
        return new MedicalCaseEntity
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "张三",
            UserId = Guid.NewGuid(),
            DoctorName = "李医生",
            CaseStatus = MedicalCaseStatus.Active,
            CompletedAt = null,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static MedicalCaseEntity CreateTestMedicalCaseWithNavigations()
    {
        var medicalCaseId = Guid.NewGuid();

        return new MedicalCaseEntity
        {
            Id = medicalCaseId,
            PatientId = Guid.NewGuid(),
            PatientName = "王五",
            UserId = Guid.NewGuid(),
            DoctorName = "赵医生",
            CaseNumber = "MC-2025-001",
            CaseStatus = MedicalCaseStatus.Active,
            CompletedAt = null,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            Consultation = new Consultation
            {
                Id = medicalCaseId, // 共享主键
                PresentIllness = "患者主诉头痛三天",
                TongueDiagnosis = "舌红苔薄",
                PulseDiagnosis = "脉弦细",
                TcmDiagnosis = "肝阳上亢",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            Prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                PrescriptionNumber = "RX-2025-001",
                DosageCount = 7,
                Discount = 1.0m,
                Usage = "每日一剂，水煎服",
                Advice = "忌辛辣",
                ReferencedFormulas = "逍遥散",
                Remark = "处方备注",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                Items = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Dosage = 10,
                        Unit = "g",
                        UnitPrice = 5.0m,
                        DecocteMethod = DecocteMethod.Default,
                        Usage = "疏肝解郁"
                    },
                    new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        HerbName = "白芍",
                        Dosage = 10,
                        Unit = "g",
                        UnitPrice = 5.0m,
                        DecocteMethod = DecocteMethod.Default,
                        Usage = "养血柔肝"
                    }
                }
            }
        };
    }

    private static Consultation CreateTestConsultation()
    {
        return new Consultation
        {
            Id = Guid.NewGuid(),
            PresentIllness = "患者主诉失眠一周",
            TongueDiagnosis = "舌淡苔白",
            PulseDiagnosis = "脉细弱",
            TcmDiagnosis = "心脾两虚",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
    }

    private static Prescription CreateTestPrescription()
    {
        return new Prescription
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            PrescriptionNumber = "RX-2025-002",
            DosageCount = 7,
            Discount = 1.0m,
            Usage = "每日一剂",
            Advice = "饭后服用",
            ReferencedFormulas = "归脾汤",
            Remark = "测试处方",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static PrescriptionItem CreateTestPrescriptionItem()
    {
        return new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = Guid.NewGuid(),
            HerbId = Guid.NewGuid(),
            HerbName = "黄芪",
            Dosage = 15,
            Unit = "g",
            UnitPrice = 8.0m,
            DecocteMethod = DecocteMethod.Default,
            Usage = "补气固表",
            Remark = "药材备注"
        };
    }

    #endregion
}
