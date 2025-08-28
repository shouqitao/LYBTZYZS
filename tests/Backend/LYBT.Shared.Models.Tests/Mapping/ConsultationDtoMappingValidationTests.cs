using System;
using System.Collections.Generic;
using AutoMapper;
using Xunit;
using LYBT.Module.Consultation.Mapping;
using LYBT.Entities.Consultation;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Tests.Mapping
{
    /// <summary>
    /// Consultation模块DTO映射验证测试
    /// UltraThink质量保证：确保Consultation相关的所有DTO映射正确无误
    /// </summary>
    public class ConsultationDtoMappingValidationTests : BaseDtoMappingValidationTests
    {
        protected override IEnumerable<Profile> GetMappingProfiles()
        {
            yield return new ConsultationMappingProfile();
        }

        protected override IEnumerable<(Type Source, Type Destination)> GetMappingPairs()
        {
            // ConsultationModel ↔ ConsultationDto 双向映射
            yield return (typeof(ConsultationModel), typeof(ConsultationDto));
            yield return (typeof(ConsultationDto), typeof(ConsultationModel));

            // ConsultationCreateDto → ConsultationModel 单向映射
            yield return (typeof(ConsultationCreateDto), typeof(ConsultationModel));

            // ConsultationUpdateDto → ConsultationModel 单向映射
            yield return (typeof(ConsultationUpdateDto), typeof(ConsultationModel));

            // ConsultationModel → ConsultationDetailDto 单向映射
            yield return (typeof(ConsultationModel), typeof(ConsultationDetailDto));
        }

        /// <summary>
        /// 测试ConsultationModel到ConsultationDto的映射
        /// </summary>
        [Fact]
        public void MapConsultationModelToDto_ShouldMapAllFields()
        {
            // Arrange
            var consultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                
                // 中医四诊
                Observation = "面色苍白，精神疲倦",
                Auscultation = "语声低微，呼吸浅短",
                Inquiry = "头晕乏力，食欲不振",
                Palpation = "脉细弱，舌淡苔白",
                
                // 诊断信息
                Diagnosis = "气血两虚",
                Syndrome = "气血不足证",
                Treatment = "补气养血",
                Prescription = "八珍汤加减",
                
                Status = ConsultationStatus.Completed,
                Remark = "患者配合度良好",
                CreateTime = DateTime.Now.AddHours(-2),
                UpdateTime = DateTime.Now.AddMinutes(-30)
            };

            // Act
            var dto = _mapper.Map<ConsultationDto>(consultation);

            // Assert - 验证所有重要字段都正确映射
            Assert.Equal(consultation.Id, dto.Id);
            Assert.Equal(consultation.PatientId, dto.PatientId);
            Assert.Equal(consultation.DoctorId, dto.DoctorId);
            Assert.Equal(consultation.MedicalCaseId, dto.MedicalCaseId);
            
            // 中医四诊验证
            Assert.Equal(consultation.Observation, dto.Observation);
            Assert.Equal(consultation.Auscultation, dto.Auscultation);
            Assert.Equal(consultation.Inquiry, dto.Inquiry);
            Assert.Equal(consultation.Palpation, dto.Palpation);
            
            // 诊断信息验证
            Assert.Equal(consultation.Diagnosis, dto.Diagnosis);
            Assert.Equal(consultation.Syndrome, dto.Syndrome);
            Assert.Equal(consultation.Treatment, dto.Treatment);
            Assert.Equal(consultation.Prescription, dto.Prescription);
            
            Assert.Equal(consultation.Status.ToString(), dto.Status);
            Assert.Equal(consultation.Remark, dto.Remark);
            Assert.Equal(consultation.CreateTime, dto.CreateTime);
            Assert.Equal(consultation.UpdateTime, dto.UpdateTime);
        }

        /// <summary>
        /// 测试ConsultationCreateDto到ConsultationModel的映射
        /// </summary>
        [Fact]
        public void MapConsultationCreateDtoToModel_ShouldMapAllFields()
        {
            // Arrange
            var createDto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                
                // 中医四诊
                Observation = "面红目赤，烦躁不安",
                Auscultation = "声音洪亮，呼吸急促",
                Inquiry = "口渴喜冷饮，大便秘结",
                Palpation = "脉洪数，舌红苔黄",
                
                // 诊断
                Diagnosis = "热证",
                Treatment = "清热泻火",
                
                Remark = "新建诊断记录"
            };

            // Act
            var model = _mapper.Map<ConsultationModel>(createDto);

            // Assert
            Assert.Equal(createDto.PatientId, model.PatientId);
            Assert.Equal(createDto.DoctorId, model.DoctorId);
            Assert.Equal(createDto.MedicalCaseId, model.MedicalCaseId);
            Assert.Equal(createDto.Observation, model.Observation);
            Assert.Equal(createDto.Auscultation, model.Auscultation);
            Assert.Equal(createDto.Inquiry, model.Inquiry);
            Assert.Equal(createDto.Palpation, model.Palpation);
            Assert.Equal(createDto.Diagnosis, model.Diagnosis);
            Assert.Equal(createDto.Treatment, model.Treatment);
            Assert.Equal(createDto.Remark, model.Remark);
        }

        /// <summary>
        /// 测试ConsultationUpdateDto到ConsultationModel的映射
        /// </summary>
        [Fact]
        public void MapConsultationUpdateDtoToModel_ShouldMapAllFields()
        {
            // Arrange
            var existing = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                Status = ConsultationStatus.InProgress
            };

            var updateDto = new ConsultationUpdateDto
            {
                Id = existing.Id,
                Observation = "更新望诊",
                Auscultation = "更新闻诊", 
                Inquiry = "更新问诊",
                Palpation = "更新切诊",
                Diagnosis = "更新诊断",
                Syndrome = "更新证候",
                Treatment = "更新治法",
                Prescription = "更新方药",
                Status = "Completed",
                Remark = "更新备注"
            };

            // Act
            _mapper.Map(updateDto, existing);

            // Assert
            Assert.Equal(updateDto.Observation, existing.Observation);
            Assert.Equal(updateDto.Auscultation, existing.Auscultation);
            Assert.Equal(updateDto.Inquiry, existing.Inquiry);
            Assert.Equal(updateDto.Palpation, existing.Palpation);
            Assert.Equal(updateDto.Diagnosis, existing.Diagnosis);
            Assert.Equal(updateDto.Syndrome, existing.Syndrome);
            Assert.Equal(updateDto.Treatment, existing.Treatment);
            Assert.Equal(updateDto.Prescription, existing.Prescription);
            Assert.Equal(ConsultationStatus.Completed, existing.Status);
            Assert.Equal(updateDto.Remark, existing.Remark);
        }

        /// <summary>
        /// 测试中医四诊数据的完整性
        /// 确保中医专业术语和长文本正确处理
        /// </summary>
        [Fact]
        public void ConsultationTCMData_ShouldMaintainIntegrity()
        {
            // Arrange - 复杂的中医诊断数据
            var complex = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                
                // 望诊详细记录
                Observation = @"患者面色萎黄，形体消瘦，精神萎靡，目光无神。
                               舌质淡红，舌体胖大，边有齿痕，苔薄白而润。",
                
                // 闻诊记录  
                Auscultation = @"语声低微，少气懒言，咳声无力。
                                呼吸浅短，不愿多语。",
                
                // 问诊详细
                Inquiry = @"主诉：乏力倦怠2月余
                           现病史：患者2月前无明显诱因出现全身乏力，倦怠嗜睡，
                           食欲减退，腹胀便溏，畏寒肢冷。",
                
                // 切诊
                Palpation = @"脉象：脉细弱无力，尺脉尤甚
                             按诊：腹软，无压痛反跳痛，肝脾未及",
                
                // 中医诊断
                Diagnosis = "脾肾阳虚证",
                Syndrome = "脾肾阳虚，运化失职，寒湿内生",
                Treatment = "温补脾肾，化湿止泻",
                Prescription = "附子理中汤合四神丸加减"
            };

            // Act - 往返映射
            var dto = _mapper.Map<ConsultationDto>(complex);
            var roundTrip = _mapper.Map<ConsultationModel>(dto);

            // Assert - 验证复杂中文内容完整性
            Assert.Contains("面色萎黄", dto.Observation);
            Assert.Contains("语声低微", dto.Auscultation);
            Assert.Contains("乏力倦怠", dto.Inquiry);
            Assert.Contains("脉细弱", dto.Palpation);
            Assert.Equal("脾肾阳虚证", dto.Diagnosis);
            Assert.Contains("附子理中汤", dto.Prescription);
            
            // 往返验证
            Assert.Equal(complex.Diagnosis, roundTrip.Diagnosis);
            Assert.Equal(complex.Syndrome, roundTrip.Syndrome);
        }
    }
}