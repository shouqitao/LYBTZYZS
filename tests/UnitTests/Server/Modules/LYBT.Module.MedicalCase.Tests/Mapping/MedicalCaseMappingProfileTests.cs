using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Mapping
{
    /// <summary>
    /// MedicalCaseMappingProfile 映射配置测试
    /// 验证病历实体与DTO之间的映射正确性
    /// </summary>
    public class MedicalCaseMappingProfileTests
    {
        private readonly IMapper _mapper;

        public MedicalCaseMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MedicalCaseMappingProfile>();
            });

            _mapper = config.CreateMapper();
        }

        #region 配置有效性测试

        [Fact]
        public void MappingConfiguration_ShouldBeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MedicalCaseMappingProfile>();
            });

            // Assert
            config.AssertConfigurationIsValid();
        }

        #endregion

        #region MedicalCase -> MedicalCaseDto 映射测试

        [Fact]
        public void MedicalCase_To_MedicalCaseDto_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                ConsultationId = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Status = MedicalCaseStatus.Completed,
                VisitDate = DateTime.Now.AddDays(-1),
                Diagnosis = "感冒",
                Treatment = "清热解毒",
                Notes = "注意休息",
                FollowUpDate = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now.AddDays(-2)
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(medicalCase.Id);
            dto.PatientId.Should().Be(medicalCase.PatientId);
            dto.ConsultationId.Should().Be(medicalCase.ConsultationId);
            dto.PrescriptionId.Should().Be(medicalCase.PrescriptionId);
            dto.DoctorId.Should().Be(medicalCase.DoctorId);
            dto.CaseStatus.Should().Be(medicalCase.Status); // Status映射到CaseStatus
            dto.VisitDate.Should().Be(medicalCase.VisitDate);
            dto.Diagnosis.Should().Be(medicalCase.Diagnosis);
            dto.Treatment.Should().Be(medicalCase.Treatment);
            dto.Notes.Should().Be(medicalCase.Notes);
            dto.FollowUpDate.Should().Be(medicalCase.FollowUpDate);
        }

        [Fact]
        public void MedicalCase_To_MedicalCaseDto_WithNullOptionalFields_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                Status = MedicalCaseStatus.InProgress,
                VisitDate = DateTime.Now,
                Diagnosis = "初步诊断",
                Treatment = null,
                Notes = null,
                FollowUpDate = null
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(MedicalCaseStatus.InProgress);
            dto.Treatment.Should().BeNull();
            dto.Notes.Should().BeNull();
            dto.FollowUpDate.Should().BeNull();
        }

        #endregion

        #region MedicalCase -> MedicalCaseDetailDto 映射测试

        [Fact]
        public void MedicalCase_To_MedicalCaseDetailDto_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                ConsultationId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Status = MedicalCaseStatus.Draft,
                VisitDate = DateTime.Now,
                Diagnosis = "详细诊断",
                Treatment = "详细治疗方案",
                Notes = "详细备注",
                FollowUpDate = DateTime.Now.AddMonths(1)
            };

            // Act
            var detailDto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(medicalCase.Id);
            detailDto.PatientId.Should().Be(medicalCase.PatientId);
            detailDto.CaseStatus.Should().Be(medicalCase.Status);
            detailDto.Diagnosis.Should().Be(medicalCase.Diagnosis);
            detailDto.Treatment.Should().Be(medicalCase.Treatment);
        }

        #endregion

        #region MedicalCaseCreateDto -> MedicalCase 映射测试

        [Fact]
        public void MedicalCaseCreateDto_To_MedicalCase_ShouldMapCorrectly()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                ConsultationId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                VisitDate = DateTime.Now,
                Diagnosis = "新病历诊断",
                Treatment = "新治疗方案",
                Notes = "新备注",
                FollowUpDate = DateTime.Now.AddWeeks(2)
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.ConsultationId.Should().Be(createDto.ConsultationId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            medicalCase.VisitDate.Should().Be(createDto.VisitDate);
            medicalCase.Diagnosis.Should().Be(createDto.Diagnosis);
            medicalCase.Treatment.Should().Be(createDto.Treatment);
            medicalCase.Notes.Should().Be(createDto.Notes);
            medicalCase.FollowUpDate.Should().Be(createDto.FollowUpDate);
        }

        [Fact]
        public void MedicalCaseCreateDto_To_MedicalCase_ShouldIgnoreIdAndNavigationProperties()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                Diagnosis = "测试诊断"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert - Id和导航属性应该被忽略
            medicalCase.Id.Should().Be(Guid.Empty);
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        #endregion

        #region MedicalCaseUpdateDto -> MedicalCase 映射测试

        [Fact]
        public void MedicalCaseUpdateDto_To_MedicalCase_ShouldMapAllowedFields()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Diagnosis = "更新的诊断",
                Treatment = "更新的治疗",
                Notes = "更新的备注",
                FollowUpDate = DateTime.Now.AddDays(14)
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.Diagnosis.Should().Be(updateDto.Diagnosis);
            medicalCase.Treatment.Should().Be(updateDto.Treatment);
            medicalCase.Notes.Should().Be(updateDto.Notes);
            medicalCase.FollowUpDate.Should().Be(updateDto.FollowUpDate);
        }

        [Fact]
        public void MedicalCaseUpdateDto_To_MedicalCase_ShouldIgnoreNavigationProperties()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Diagnosis = "忽略测试"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert - 导航属性应该被忽略
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        [Fact]
        public void MedicalCaseUpdateDto_To_MedicalCase_ShouldOnlyMapNonNullValues()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Diagnosis = "部分更新",
                Treatment = null, // null值应该被跳过
                Notes = null
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert - ForAllMembers条件会跳过null值
            medicalCase.Diagnosis.Should().Be("部分更新");
            medicalCase.Treatment.Should().BeNull();
            medicalCase.Notes.Should().BeNull();
        }

        #endregion

        #region MedicalCaseDto -> MedicalCase 映射测试

        [Fact]
        public void MedicalCaseDto_To_MedicalCase_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCaseDto = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                ConsultationId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseStatus = MedicalCaseStatus.Completed,
                VisitDate = DateTime.Now.AddDays(-3),
                Diagnosis = "DTO诊断",
                Treatment = "DTO治疗"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(medicalCaseDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.Id.Should().Be(medicalCaseDto.Id);
            medicalCase.PatientId.Should().Be(medicalCaseDto.PatientId);
            medicalCase.Status.Should().Be(medicalCaseDto.CaseStatus); // CaseStatus映射到Status
            medicalCase.Diagnosis.Should().Be(medicalCaseDto.Diagnosis);
            medicalCase.Treatment.Should().Be(medicalCaseDto.Treatment);
        }

        [Fact]
        public void MedicalCaseDto_To_MedicalCase_ShouldIgnoreNavigationProperties()
        {
            // Arrange
            var medicalCaseDto = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                CaseStatus = MedicalCaseStatus.Draft,
                Diagnosis = "导航属性测试"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(medicalCaseDto);

            // Assert
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        #endregion

        #region 状态映射测试

        [Theory]
        [InlineData(MedicalCaseStatus.Active)]
        [InlineData(MedicalCaseStatus.Closed)]
        public void MedicalCase_To_MedicalCaseDto_ShouldMapAllStatuses(MedicalCaseStatus status)
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                Status = status,
                VisitDate = DateTime.Now,
                Diagnosis = "状态测试"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.CaseStatus.Should().Be(status);
        }

        [Theory]
        [InlineData(MedicalCaseStatus.Active)]
        [InlineData(MedicalCaseStatus.Closed)]
        public void MedicalCaseDto_To_MedicalCase_ShouldMapAllStatuses(MedicalCaseStatus status)
        {
            // Arrange
            var dto = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                CaseStatus = status,
                VisitDate = DateTime.Now,
                Diagnosis = "反向状态测试"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(dto);

            // Assert
            medicalCase.Status.Should().Be(status);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void Mapping_WithNullSource_ShouldReturnNull()
        {
            // Arrange
            LYBT.Entities.MedicalCase.MedicalCase? nullMedicalCase = null;

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(nullMedicalCase);

            // Assert
            dto.Should().BeNull();
        }

        [Fact]
        public void MedicalCaseCreateDto_WithMinimalData_ShouldMapSuccessfully()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                VisitDate = DateTime.Now,
                Diagnosis = "最小数据"
                // 其他字段为null
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.Diagnosis.Should().Be("最小数据");
            medicalCase.Treatment.Should().BeNull();
            medicalCase.Notes.Should().BeNull();
        }

        #endregion

        #region 特殊字符测试

        [Fact]
        public void MedicalCase_WithSpecialCharacters_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                Status = MedicalCaseStatus.Completed,
                VisitDate = DateTime.Now,
                Diagnosis = "风寒感冒（轻度）；咳嗽",
                Treatment = "清热解毒、宣肺止咳\n服用：小柴胡汤加减",
                Notes = "患者体质偏寒，建议温服；忌食生冷/辛辣"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Diagnosis.Should().Be("风寒感冒（轻度）；咳嗽");
            dto.Treatment.Should().Be("清热解毒、宣肺止咳\n服用：小柴胡汤加减");
            dto.Notes.Should().Be("患者体质偏寒，建议温服；忌食生冷/辛辣");
        }

        #endregion
    }
}
