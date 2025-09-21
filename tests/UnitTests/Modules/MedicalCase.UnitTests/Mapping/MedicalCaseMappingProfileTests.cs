using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Mapping
{
    /// <summary>
    /// MedicalCase模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// </summary>
    public class MedicalCaseMappingProfileTests
    {
        private readonly IMapper _mapper;

        public MedicalCaseMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MedicalCaseMappingProfile());
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MedicalCaseMappingProfile());
            }, NullLoggerFactory.Instance);

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
                CaseNumber = "MC20241001001",
                CreatedTime = DateTime.Now,
                Status = CaseStatus.InProgress,
                Remark = "测试病历"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(medicalCase.Id);
            dto.PatientId.Should().Be(medicalCase.PatientId);
            dto.DoctorId.Should().Be(medicalCase.DoctorId);
            dto.CaseNumber.Should().Be(medicalCase.CaseNumber);
            dto.CreatedTime.Should().Be(medicalCase.CreatedTime);
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
                CaseNumber = "MC20241001002",
                CreatedTime = DateTime.Now,
                Status = CaseStatus.Completed,
                Remark = "详细病历测试"
            };

            // Act
            var detailDto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(medicalCase.Id);
            detailDto.PatientId.Should().Be(medicalCase.PatientId);
            detailDto.DoctorId.Should().Be(medicalCase.DoctorId);
            detailDto.CaseNumber.Should().Be(medicalCase.CaseNumber);
            detailDto.CreatedTime.Should().Be(medicalCase.CreatedTime);
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
                CaseNumber = "MC20241001003",
                CreatedTime = DateTime.Now,
                Status = CaseStatus.InProgress,
                Remark = "新建病历"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            medicalCase.CaseNumber.Should().Be(createDto.CaseNumber);
            medicalCase.CreatedTime.Should().Be(createDto.CreatedTime);
            medicalCase.Status.Should().Be(createDto.Status);
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
                Status = CaseStatus.Completed,
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
            medicalCase.Status.Should().Be(updateDto.Status);
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
                CaseNumber = "MC20241001004",
                CreatedTime = DateTime.Now,
                CaseStatus = CaseStatus.InProgress,
                Remark = "DTO转实体测试"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(dto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(dto.PatientId);
            medicalCase.DoctorId.Should().Be(dto.DoctorId);
            medicalCase.CaseNumber.Should().Be(dto.CaseNumber);
            medicalCase.CreatedTime.Should().Be(dto.CreatedTime);
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
                CaseNumber = "MC20241001005",
                Status = CaseStatus.Draft,
                Remark = "草稿状态病历"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(CaseStatus.Draft);
            dto.Remark.Should().Be("草稿状态病历");
        }

        [Fact]
        public void Map_MedicalCase_With_CompletedStatus_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                Status = CaseStatus.Completed
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(CaseStatus.Completed);
        }

        [Fact]
        public void Map_MedicalCase_With_CancelledStatus_Should_Success()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                Status = CaseStatus.Cancelled
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.CaseStatus.Should().Be(CaseStatus.Cancelled);
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
                CaseNumber = "MC20241001006",
                Status = CaseStatus.InProgress,
                Remark = null
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(medicalCase.Id);
            dto.PatientId.Should().Be(medicalCase.PatientId);
            dto.DoctorId.Should().Be(medicalCase.DoctorId);
            dto.CaseNumber.Should().Be(medicalCase.CaseNumber);
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
                DoctorId = Guid.NewGuid(),
                Status = CaseStatus.Draft
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            medicalCase.Status.Should().Be(createDto.Status);
        }
    }
}