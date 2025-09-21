using AutoMapper;
using FluentAssertions;
using LYBT.Module.Consultation.Mapping;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Mapping
{
    /// <summary>
    /// Consultation模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// </summary>
    public class ConsultationMappingProfileTests
    {
        private readonly IMapper _mapper;

        public ConsultationMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new ConsultationMappingProfile());
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new ConsultationMappingProfile());
            }, NullLoggerFactory.Instance);

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_ConsultationDetailDto_To_Consultation_Should_Success()
        {
            // Arrange
            var detailDto = new ConsultationDetailDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "头痛",
                PresentIllness = "头痛3天，伴有恶心",
                PastHistory = "高血压病史",
                PersonalHistory = "无特殊",
                FamilyHistory = "父亲有高血压",
                TCMDiagnosis = "肝阳上亢",
                DiagnosisResult = "头痛（肝阳上亢证）",
                TreatmentPlan = "平肝潜阳",
                ConsultationNotes = "患者精神状态良好",
                // 显示字段，应该被忽略
                PatientName = "张三",
                DoctorName = "李医生",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                ConsultationStatus = ConsultationStatus.InProgress,
                IsCompleted = false
            };

            // Act
            var consultation = _mapper.Map<LYBT.Entities.Consultation.Consultation>(detailDto);

            // Assert
            consultation.Should().NotBeNull();
            consultation.PatientId.Should().Be(detailDto.PatientId);
            consultation.DoctorId.Should().Be(detailDto.DoctorId);
            consultation.MedicalCaseId.Should().Be(detailDto.MedicalCaseId);
            consultation.ChiefComplaint.Should().Be(detailDto.ChiefComplaint);
            consultation.PresentIllness.Should().Be(detailDto.PresentIllness);
            consultation.PastHistory.Should().Be(detailDto.PastHistory);
            consultation.PersonalHistory.Should().Be(detailDto.PersonalHistory);
            consultation.FamilyHistory.Should().Be(detailDto.FamilyHistory);
            consultation.TCMDiagnosis.Should().Be(detailDto.TCMDiagnosis);
            consultation.DiagnosisResult.Should().Be(detailDto.DiagnosisResult);
            consultation.TreatmentPlan.Should().Be(detailDto.TreatmentPlan);
            consultation.ConsultationNotes.Should().Be(detailDto.ConsultationNotes);

            // 验证忽略字段
            consultation.Id.Should().Be(Guid.Empty);
            consultation.Patient.Should().BeNull();
            consultation.User.Should().BeNull();
            consultation.MedicalCase.Should().BeNull();
        }

        [Fact]
        public void Map_Consultation_To_ConsultationDto_Should_Success()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "咳嗽",
                PresentIllness = "咳嗽2周",
                TCMDiagnosis = "肺燥咳嗽",
                DiagnosisResult = "咳嗽（肺燥证）",
                TreatmentPlan = "润肺止咳",
                ConsultationTime = DateTime.Now,
                Status = CommonStatus.Active
            };

            // Act
            var dto = _mapper.Map<ConsultationDto>(consultation);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(consultation.Id);
            dto.PatientId.Should().Be(consultation.PatientId);
            dto.DoctorId.Should().Be(consultation.DoctorId);
            dto.MedicalCaseId.Should().Be(consultation.MedicalCaseId);
            dto.ChiefComplaint.Should().Be(consultation.ChiefComplaint);
            dto.PresentIllness.Should().Be(consultation.PresentIllness);
            dto.TCMDiagnosis.Should().Be(consultation.TCMDiagnosis);
            dto.DiagnosisResult.Should().Be(consultation.DiagnosisResult);
            dto.TreatmentPlan.Should().Be(consultation.TreatmentPlan);
            dto.ConsultationTime.Should().Be(consultation.ConsultationTime);
            dto.Status.Should().Be(consultation.Status);

            // DoctorName需要从关联数据获取，所以会被忽略
            dto.DoctorName.Should().BeNull();
        }

        [Fact]
        public void Map_Consultation_To_ConsultationDetailDto_Should_Success()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "胃痛",
                PresentIllness = "胃痛1周",
                PastHistory = "胃炎",
                PersonalHistory = "吸烟史",
                FamilyHistory = "无特殊",
                TCMDiagnosis = "胃气郁滞",
                DiagnosisResult = "胃痛（气滞证）",
                TreatmentPlan = "理气和胃",
                ConsultationNotes = "建议戒烟",
                ConsultationTime = DateTime.Now,
                Status = CommonStatus.Active
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(consultation.Id);
            detailDto.PatientId.Should().Be(consultation.PatientId);
            detailDto.DoctorId.Should().Be(consultation.DoctorId);
            detailDto.MedicalCaseId.Should().Be(consultation.MedicalCaseId);
            detailDto.ChiefComplaint.Should().Be(consultation.ChiefComplaint);
            detailDto.PresentIllness.Should().Be(consultation.PresentIllness);
            detailDto.PastHistory.Should().Be(consultation.PastHistory);
            detailDto.PersonalHistory.Should().Be(consultation.PersonalHistory);
            detailDto.FamilyHistory.Should().Be(consultation.FamilyHistory);
            detailDto.TCMDiagnosis.Should().Be(consultation.TCMDiagnosis);
            detailDto.DiagnosisResult.Should().Be(consultation.DiagnosisResult);
            detailDto.TreatmentPlan.Should().Be(consultation.TreatmentPlan);
            detailDto.ConsultationNotes.Should().Be(consultation.ConsultationNotes);
            detailDto.ConsultationTime.Should().Be(consultation.ConsultationTime);

            // 验证状态映射
            detailDto.ConsultationStatus.Should().Be(ConsultationStatus.InProgress);
            detailDto.StartTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

            // 关联字段需要从其他数据获取，所以会被忽略
            detailDto.PatientName.Should().BeNull();
            detailDto.DoctorName.Should().BeNull();
            detailDto.EndTime.Should().BeNull();
        }

        [Fact]
        public void Map_Consultation_With_DisabledStatus_Should_MapToCompleted()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Status = CommonStatus.Disabled
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.ConsultationStatus.Should().Be(ConsultationStatus.Completed);
        }

        [Fact]
        public void Map_Consultation_With_ActiveStatus_Should_MapToInProgress()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Status = CommonStatus.Active
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.ConsultationStatus.Should().Be(ConsultationStatus.InProgress);
        }

        [Fact]
        public void Map_ConsultationDetailDto_With_NullFields_Should_Success()
        {
            // Arrange
            var detailDto = new ConsultationDetailDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "主诉",
                PresentIllness = null,
                PastHistory = null,
                PersonalHistory = null,
                FamilyHistory = null,
                TCMDiagnosis = null,
                DiagnosisResult = null,
                TreatmentPlan = null,
                ConsultationNotes = null
            };

            // Act
            var consultation = _mapper.Map<LYBT.Entities.Consultation.Consultation>(detailDto);

            // Assert
            consultation.Should().NotBeNull();
            consultation.PatientId.Should().Be(detailDto.PatientId);
            consultation.DoctorId.Should().Be(detailDto.DoctorId);
            consultation.ChiefComplaint.Should().Be(detailDto.ChiefComplaint);
            consultation.PresentIllness.Should().BeNull();
            consultation.PastHistory.Should().BeNull();
            consultation.PersonalHistory.Should().BeNull();
            consultation.FamilyHistory.Should().BeNull();
            consultation.TCMDiagnosis.Should().BeNull();
            consultation.DiagnosisResult.Should().BeNull();
            consultation.TreatmentPlan.Should().BeNull();
            consultation.ConsultationNotes.Should().BeNull();
        }

        [Fact]
        public void Map_Consultation_With_ComplexTCMDiagnosis_Should_Success()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                TCMDiagnosis = "肝郁脾虚，痰湿内阻",
                DiagnosisResult = "失眠（肝郁脾虚证）",
                TreatmentPlan = "疏肝健脾，化痰安神"
            };

            // Act
            var dto = _mapper.Map<ConsultationDto>(consultation);

            // Assert
            dto.Should().NotBeNull();
            dto.TCMDiagnosis.Should().Be("肝郁脾虚，痰湿内阻");
            dto.DiagnosisResult.Should().Be("失眠（肝郁脾虚证）");
            dto.TreatmentPlan.Should().Be("疏肝健脾，化痰安神");
        }

        [Fact]
        public void Map_Consultation_With_LongConsultationNotes_Should_Success()
        {
            // Arrange
            var longNotes = "患者精神状态良好，舌淡红苔薄白，脉弦细。建议：1. 规律作息；2. 饮食清淡；3. 适当运动；4. 定期复诊。";
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ConsultationNotes = longNotes
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.ConsultationNotes.Should().Be(longNotes);
        }
    }
}