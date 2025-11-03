using AutoMapper;
using LYBT.Module.Consultation.Mapping;
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
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new ConsultationMappingProfile());
            });

            // Assert
            config.AssertConfigurationIsValid();
        }

        // Issue #1562 Phase 5: 已删除测试 - ConsultationDetailDto类型已删除

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public void Map_Consultation_To_ConsultationDto_Should_Success()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "咳嗽",
                PresentIllness = "咳嗽2周",
                TCMDiagnosis = "肺燥咳嗽",
                TreatmentPrinciple = "润肺止咳",
                Status = CommonStatus.Enabled
            };

            // Act
            var dto = _mapper.Map<ConsultationDto>(consultation);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(consultation.Id);
            dto.PatientId.Should().Be(consultation.PatientId);
            dto.UserId.Should().Be(consultation.UserId);
            dto.MedicalCaseId.Should().Be(consultation.MedicalCaseId);
            dto.ChiefComplaint.Should().Be(consultation.ChiefComplaint);
            dto.PresentIllness.Should().Be(consultation.PresentIllness);
            dto.TCMDiagnosis.Should().Be(consultation.TCMDiagnosis);
            dto.TCMDiagnosis.Should().Be(consultation.TCMDiagnosis);
            dto.TreatmentPrinciple.Should().Be(consultation.TreatmentPrinciple);
            dto.Status.Should().Be(consultation.Status);

            // DoctorName需要从关联数据获取，所以会被忽略
            dto.DoctorName.Should().BeNull();
        }
        */

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public void Map_Consultation_To_ConsultationDetailDto_Should_Success()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "胃痛",
                PresentIllness = "胃痛1周",
                Inspection = "面色萎黄",
                AuscultationOlfaction = "口中有异味",
                Inquiry = "饮食不规律",
                Palpation = "脉弦细",
                TCMDiagnosis = "胃气郁滞",
                TreatmentPrinciple = "理气和胃",
                MedicalAdvice = "建议戒烟",
                Status = CommonStatus.Enabled
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(consultation.Id);
            detailDto.PatientId.Should().Be(consultation.PatientId);
            detailDto.UserId.Should().Be(consultation.UserId);
            detailDto.MedicalCaseId.Should().Be(consultation.MedicalCaseId);
            detailDto.ChiefComplaint.Should().Be(consultation.ChiefComplaint);
            detailDto.PresentIllness.Should().Be(consultation.PresentIllness);
            detailDto.Inspection.Should().Be(consultation.Inspection);
            detailDto.AuscultationOlfaction.Should().Be(consultation.AuscultationOlfaction);
            detailDto.Inquiry.Should().Be(consultation.Inquiry);
            detailDto.Palpation.Should().Be(consultation.Palpation);
            detailDto.TCMDiagnosis.Should().Be(consultation.TCMDiagnosis);
            detailDto.TCMDiagnosis.Should().Be(consultation.TCMDiagnosis);
            detailDto.TreatmentPrinciple.Should().Be(consultation.TreatmentPrinciple);
            detailDto.MedicalAdvice.Should().Be(consultation.MedicalAdvice);

            // 验证状态映射
            detailDto.ConsultationStatus.Should().Be(ConsultationStatus.InProgress);
            detailDto.StartTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

            // 关联字段需要从其他数据获取，所以会被忽略
            detailDto.PatientName.Should().BeNull();
            detailDto.DoctorName.Should().BeNull();
            detailDto.EndTime.Should().BeNull();
        }
        */

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public void Map_Consultation_With_DisabledStatus_Should_MapToCompleted()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = CommonStatus.Disabled
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.ConsultationStatus.Should().Be(ConsultationStatus.Completed);
        }
        */

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public void Map_Consultation_With_ActiveStatus_Should_MapToInProgress()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = CommonStatus.Enabled
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.ConsultationStatus.Should().Be(ConsultationStatus.InProgress);
        }
        */

        // Issue #1562 Phase 5: 已删除测试 - ConsultationDetailDto类型已删除

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public void Map_Consultation_With_ComplexTCMDiagnosis_Should_Success()
        {
            // Arrange
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TCMDiagnosis = "肝郁脾虚，痰湿内阻",
                TreatmentPrinciple = "疏肝健脾，化痰安神"
            };

            // Act
            var dto = _mapper.Map<ConsultationDto>(consultation);

            // Assert
            dto.Should().NotBeNull();
            dto.TCMDiagnosis.Should().Be("肝郁脾虚，痰湿内阻");
            // Note: Entity doesn't have DiagnosisResult, check TCMDiagnosis instead
            dto.TreatmentPrinciple.Should().Be("疏肝健脾，化痰安神");
        }
        */

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public void Map_Consultation_With_LongConsultationNotes_Should_Success()
        {
            // Arrange
            var longNotes = "患者精神状态良好，舌淡红苔薄白，脉弦细。建议：1. 规律作息；2. 饮食清淡；3. 适当运动；4. 定期复诊。";
            var consultation = new LYBT.Entities.Consultation.Consultation
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                MedicalAdvice = longNotes
            };

            // Act
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultation);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.MedicalAdvice.Should().Be(longNotes);
        }
        */
    }
}
