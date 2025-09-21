using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Mapping
{
    /// <summary>
    /// MedicalCase模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确�?
    /// </summary>
    public class MedicalCaseMappingProfileTests
    {
        private readonly IMapper _mapper;

        public MedicalCaseMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MedicalCaseMappingProfile());
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MedicalCaseMappingProfile());
            });

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_MedicalCase_To_MedicalCaseDto_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                // CaseNumber属性不存在于实体中
                CreatedAt = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                Remark = "测试病历"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(medicalCase.Id);
            dto.PatientId.Should().Be(medicalCase.PatientId);
            dto.DoctorId.Should().Be(medicalCase.DoctorId);
            // 实体中使用ConsultationDate和CreatedAt
            dto.ConsultationDate.Should().Be(medicalCase.ConsultationDate);
            dto.CaseStatus.Should().Be(medicalCase.Status);
            dto.Remark.Should().Be(medicalCase.Remark);
        }

        [Fact]
        public void Map_MedicalCase_To_MedicalCaseDetailDto_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                // CaseNumber属性不存在于实体中
                CreatedAt = DateTime.Now,
                Status = MedicalCaseStatus.Closed,
                Remark = "详细病历测试"
            };

            // Act
            var detailDto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(medicalCase.Id);
            detailDto.PatientId.Should().Be(medicalCase.PatientId);
            detailDto.DoctorId.Should().Be(medicalCase.DoctorId);
            // 实体中使用ConsultationDate和CreatedAt
            detailDto.ConsultationDate.Should().Be(medicalCase.ConsultationDate);
            detailDto.CaseStatus.Should().Be(medicalCase.Status);
            detailDto.Remark.Should().Be(medicalCase.Remark);
        }

        [Fact]
        public void Map_MedicalCaseCreateDto_To_MedicalCase_Should_Success()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                // CaseNumber属性不存在于CreateDto�?
                // CreatedTime将由系统自动设置
                // Status将使用默认�?
                Remark = "新建病历"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            // CaseNumber和CreatedTime由系统自动设�?
            medicalCase.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
            medicalCase.Status.Should().Be(MedicalCaseStatus.Active);
            medicalCase.Remark.Should().Be(createDto.Remark);

            // 验证忽略字段
            medicalCase.Id.Should().Be(Guid.Empty);
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        [Fact]
        public void Map_MedicalCaseUpdateDto_To_MedicalCase_Should_Success()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = Guid.NewGuid(),
                // Status使用MedicalCaseStatus枚举
                Remark = "更新病历",
                // 这些字段应该被忽略，因为它们不属于MedicalCase实体
                DiagnosisSummary = "诊断摘要",
                ChiefComplaint = "主诉",
                PresentIllness = "现病史",
                DiagnosisResult = "诊断结果",
                TreatmentPlan = "治疗方案"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert
            medicalCase.Should().NotBeNull();
            // Status属性需要特定的更新逻辑
            medicalCase.Remark.Should().Be(updateDto.Remark);

            // 验证忽略字段
            medicalCase.Id.Should().Be(Guid.Empty);
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        [Fact]
        public void Map_MedicalCaseDto_To_MedicalCase_Should_Success()
        {
            // Arrange
            var dto = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                // CaseNumber和CreatedTime属性不存在于DTO中
                ConsultationDate = DateTime.Now,
                CaseStatus = MedicalCaseStatus.Active, // InProgress状态已合并到Active
                Remark = "DTO转实体测试"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(dto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(dto.PatientId);
            medicalCase.DoctorId.Should().Be(dto.DoctorId);
            // CaseNumber和CreatedTime属性不存在于实体中
            medicalCase.ConsultationDate.Should().Be(dto.ConsultationDate);
            medicalCase.Status.Should().Be(dto.CaseStatus);
            medicalCase.Remark.Should().Be(dto.Remark);

            // 验证忽略字段
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        [Fact]
        public void Map_MedicalCase_With_DraftStatus_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                // CaseNumber属性不存在于实体中
                Status = MedicalCaseStatus.Active, // Draft状态已合并到Active
                Remark = "草稿状态病历"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(MedicalCaseStatus.Active);
            dto.Remark.Should().Be("草稿状态病历");
        }

        [Fact]
        public void Map_MedicalCase_With_CompletedStatus_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                Status = MedicalCaseStatus.Closed // Completed状态已合并到Closed
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(MedicalCaseStatus.Closed);
        }

        [Fact]
        public void Map_MedicalCase_With_CancelledStatus_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                Status = MedicalCaseStatus.Closed // Cancelled状态已合并到Closed
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(MedicalCaseStatus.Closed);
        }

        [Fact]
        public void Map_MedicalCase_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                // CaseNumber属性不存在于实体中
                Status = MedicalCaseStatus.Active, // InProgress状态已合并到Active
                Remark = null
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(medicalCase.Id);
            dto.PatientId.Should().Be(medicalCase.PatientId);
            dto.DoctorId.Should().Be(medicalCase.DoctorId);
            // CaseNumber属性不存在于实体中
            dto.CaseStatus.Should().Be(medicalCase.Status);
            dto.Remark.Should().BeNull();
        }

        [Fact]
        public void Map_MedicalCaseCreateDto_With_MinimalData_Should_Success()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
                // 使用默认状态Active，Draft已合并
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            medicalCase.Status.Should().Be(MedicalCaseStatus.Active);
        }
    }
}
