using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;

namespace LYBT.Tests.Server.PureLogic.MedicalCase;

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
        dto.Remark.Should().Be(entity.Remark);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.PrintVersion.Should().Be(entity.PrintVersion);
        dto.PrintCount.Should().Be(entity.PrintCount);
        dto.IsPrinted.Should().Be(entity.IsPrinted);
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

    #region ToPrescriptionEntity 测试

    [Fact]
    public void ToPrescriptionEntity_WithValidDto_ShouldMapProperties()
    {
        // Arrange
        var dto = CreateTestPrescriptionInputDto();

        // Act
        var entity = _mapper.ToPrescriptionEntity(dto);

        // Assert
        entity.Should().NotBeNull();
        entity.DosageCount.Should().Be(dto.DosageCount);
        entity.Usage.Should().Be(dto.Usage);
        entity.Advice.Should().Be(dto.Advice);
        entity.ReferencedFormulas.Should().Be(dto.ReferencedFormulas);
        entity.Discount.Should().Be(dto.Discount);
        entity.Remark.Should().Be(dto.Remark);
    }

    [Fact]
    public void ToPrescriptionEntity_ShouldIgnoreAuditAndSystemFields()
    {
        // Arrange
        var dto = CreateTestPrescriptionInputDto();

        // Act
        var entity = _mapper.ToPrescriptionEntity(dto);

        // Assert - Mapperly忽略的系统字段应保持默认值
        entity.Id.Should().NotBe(Guid.Empty); // Constructor default = Guid.NewGuid()
        entity.MedicalCaseId.Should().Be(Guid.Empty); // Ignored (default)
        entity.PrescriptionNumber.Should().BeNull(); // Ignored
        entity.Items.Should().BeEmpty(); // Ignored (ICollection default)
        entity.CreatedAt.Should().BeAfter(DateTime.MinValue); // Constructor default = UtcNow
        entity.CreatedBy.Should().BeNull();
        entity.UpdatedAt.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
        entity.RowVersion.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region UpdatePrescriptionEntity 测试

    [Fact]
    public void UpdatePrescriptionEntity_ShouldUpdateMappedFields()
    {
        // Arrange
        var existing = CreateTestPrescription();
        var dto = new PrescriptionInputDto
        {
            DosageCount = 14,
            Usage = "每日两次",
            Advice = "饭后服用",
            Discount = 0.8m,
            Remark = "新备注"
        };

        // Act
        _mapper.UpdatePrescriptionEntity(dto, existing);

        // Assert
        existing.DosageCount.Should().Be(14);
        existing.Usage.Should().Be("每日两次");
        existing.Advice.Should().Be("饭后服用");
        existing.Discount.Should().Be(0.8m);
        existing.Remark.Should().Be("新备注");
    }

    [Fact]
    public void UpdatePrescriptionEntity_ShouldNotModifyIgnoredFields()
    {
        // Arrange
        var existing = CreateTestPrescription();
        var originalId = existing.Id;
        var originalMedicalCaseId = existing.MedicalCaseId;
        var originalCreatedAt = existing.CreatedAt;
        var dto = CreateTestPrescriptionInputDto();

        // Act
        _mapper.UpdatePrescriptionEntity(dto, existing);

        // Assert - 忽略的字段不应被修改
        existing.Id.Should().Be(originalId);
        existing.MedicalCaseId.Should().Be(originalMedicalCaseId);
        existing.CreatedAt.Should().Be(originalCreatedAt);
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

    #region ToPrescriptionItemDtos 测试

    [Fact]
    public void ToPrescriptionItemDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<PrescriptionItem>
        {
            CreateTestPrescriptionItem(),
            CreateTestPrescriptionItem(),
            CreateTestPrescriptionItem()
        };

        // Act
        var dtos = _mapper.ToPrescriptionItemDtos(entities);

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Id.Should().Be(entities[0].Id);
        dtos[1].Id.Should().Be(entities[1].Id);
        dtos[2].Id.Should().Be(entities[2].Id);
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
            Remark = "测试备注",
            PrintVersion = 1,
            PrintCount = 0,
            IsPrinted = false,
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
            Remark = "完整测试",
            PrintVersion = 1,
            PrintCount = 0,
            IsPrinted = false,
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

    private static PrescriptionInputDto CreateTestPrescriptionInputDto()
    {
        return new PrescriptionInputDto
        {
            DosageCount = 7,
            Usage = "每日一剂",
            Advice = "饭后服用",
            Discount = 1.0m,
            Remark = "测试输入"
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
