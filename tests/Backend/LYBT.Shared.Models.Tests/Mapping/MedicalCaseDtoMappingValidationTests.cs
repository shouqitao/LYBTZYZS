using System;
using System.Collections.Generic;
using AutoMapper;
using Xunit;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Tests.Mapping
{
    /// <summary>
    /// MedicalCase模块DTO映射验证测试
    /// UltraThink质量保证：确保MedicalCase相关的所有DTO映射正确无误
    /// 这是之前发现严重字段更新问题的模块，需要特别关注映射完整性
    /// </summary>
    public class MedicalCaseDtoMappingValidationTests : BaseDtoMappingValidationTests
    {
        protected override IEnumerable<Profile> GetMappingProfiles()
        {
            yield return new MedicalCaseMappingProfile();
        }

        protected override IEnumerable<(Type Source, Type Destination)> GetMappingPairs()
        {
            // MedicalCaseModel ↔ MedicalCaseDto 双向映射
            yield return (typeof(MedicalCaseModel), typeof(MedicalCaseDto));
            yield return (typeof(MedicalCaseDto), typeof(MedicalCaseModel));

            // MedicalCaseCreateDto → MedicalCaseModel 单向映射
            yield return (typeof(MedicalCaseCreateDto), typeof(MedicalCaseModel));

            // MedicalCaseUpdateDto → MedicalCaseModel 单向映射（重点测试）
            yield return (typeof(MedicalCaseUpdateDto), typeof(MedicalCaseModel));

            // MedicalCaseModel → MedicalCaseDetailDto 单向映射
            yield return (typeof(MedicalCaseModel), typeof(MedicalCaseDetailDto));
        }

        /// <summary>
        /// 测试MedicalCaseModel到MedicalCaseDto的映射
        /// </summary>
        [Fact]
        public void MapMedicalCaseModelToDto_ShouldMapAllFields()
        {
            // Arrange
            var medicalCase = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                ChiefComplaint = "头痛头晕三天",
                PresentIllness = "患者3天前开始出现头痛头晕",
                PastHistory = "高血压病史5年",
                PersonalHistory = "无吸烟饮酒史",
                FamilyHistory = "父亲有高血压",
                PhysicalExam = "血压150/90mmHg",
                Diagnosis = "原发性高血压",
                TreatmentPlan = "降压药物治疗",
                FollowUpPlan = "2周后复查",
                Status = MedicalCaseStatus.InProgress,
                Severity = CaseSeverity.Moderate,
                Priority = CasePriority.Normal,
                Remark = "需要密切观察血压变化",
                CreateTime = DateTime.Now.AddDays(-5),
                UpdateTime = DateTime.Now.AddHours(-2)
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert - 验证所有15个字段都正确映射（这是之前只映射2个字段的问题）
            Assert.Equal(medicalCase.Id, dto.Id);
            Assert.Equal(medicalCase.PatientId, dto.PatientId);
            Assert.Equal(medicalCase.DoctorId, dto.DoctorId);
            Assert.Equal(medicalCase.CaseNumber, dto.CaseNumber);
            Assert.Equal(medicalCase.ChiefComplaint, dto.ChiefComplaint);
            Assert.Equal(medicalCase.PresentIllness, dto.PresentIllness);
            Assert.Equal(medicalCase.PastHistory, dto.PastHistory);
            Assert.Equal(medicalCase.PersonalHistory, dto.PersonalHistory);
            Assert.Equal(medicalCase.FamilyHistory, dto.FamilyHistory);
            Assert.Equal(medicalCase.PhysicalExam, dto.PhysicalExam);
            Assert.Equal(medicalCase.Diagnosis, dto.Diagnosis);
            Assert.Equal(medicalCase.TreatmentPlan, dto.TreatmentPlan);
            Assert.Equal(medicalCase.FollowUpPlan, dto.FollowUpPlan);
            Assert.Equal(medicalCase.Status.ToString(), dto.Status);
            Assert.Equal(medicalCase.Remark, dto.Remark);
            Assert.Equal(medicalCase.CreateTime, dto.CreateTime);
            Assert.Equal(medicalCase.UpdateTime, dto.UpdateTime);
        }

        /// <summary>
        /// 测试MedicalCaseUpdateDto到MedicalCaseModel的映射（重点测试）
        /// 这是之前发现严重问题的映射，需要确保所有字段都能正确更新
        /// </summary>
        [Fact]
        public void MapMedicalCaseUpdateDtoToModel_ShouldMapAllFields()
        {
            // Arrange
            var existingModel = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501001",
                Status = MedicalCaseStatus.Registered
            };

            var updateDto = new MedicalCaseUpdateDto
            {
                Id = existingModel.Id,
                ChiefComplaint = "更新的主诉",
                PresentIllness = "更新的现病史",
                PastHistory = "更新的既往史", 
                PersonalHistory = "更新的个人史",
                FamilyHistory = "更新的家族史",
                PhysicalExam = "更新的体格检查",
                Diagnosis = "更新的诊断",
                TreatmentPlan = "更新的治疗方案",
                FollowUpPlan = "更新的随访计划",
                Status = "InProgress",
                Remark = "更新的备注"
            };

            // Act - 这里使用AutoMapper进行更新映射（修复后的方式）
            _mapper.Map(updateDto, existingModel);

            // Assert - 验证所有字段都被正确更新（而不是只有2个字段）
            Assert.Equal(updateDto.Id, existingModel.Id);
            Assert.Equal(updateDto.ChiefComplaint, existingModel.ChiefComplaint);
            Assert.Equal(updateDto.PresentIllness, existingModel.PresentIllness);
            Assert.Equal(updateDto.PastHistory, existingModel.PastHistory);
            Assert.Equal(updateDto.PersonalHistory, existingModel.PersonalHistory);
            Assert.Equal(updateDto.FamilyHistory, existingModel.FamilyHistory);
            Assert.Equal(updateDto.PhysicalExam, existingModel.PhysicalExam);
            Assert.Equal(updateDto.Diagnosis, existingModel.Diagnosis);
            Assert.Equal(updateDto.TreatmentPlan, existingModel.TreatmentPlan);
            Assert.Equal(updateDto.FollowUpPlan, existingModel.FollowUpPlan);
            Assert.Equal(MedicalCaseStatus.InProgress, existingModel.Status);
            Assert.Equal(updateDto.Remark, existingModel.Remark);
        }

        /// <summary>
        /// 测试MedicalCaseCreateDto到MedicalCaseModel的映射
        /// </summary>
        [Fact]
        public void MapMedicalCaseCreateDtoToModel_ShouldMapAllFields()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "新建主诉",
                PresentIllness = "新建现病史",
                PastHistory = "新建既往史",
                Remark = "新建备注"
            };

            // Act
            var model = _mapper.Map<MedicalCaseModel>(createDto);

            // Assert
            Assert.Equal(createDto.PatientId, model.PatientId);
            Assert.Equal(createDto.DoctorId, model.DoctorId);
            Assert.Equal(createDto.ChiefComplaint, model.ChiefComplaint);
            Assert.Equal(createDto.PresentIllness, model.PresentIllness);
            Assert.Equal(createDto.PastHistory, model.PastHistory);
            Assert.Equal(createDto.Remark, model.Remark);
        }

        /// <summary>
        /// 测试复杂字段更新场景
        /// 模拟实际业务中的复杂更新操作，确保不会遗漏字段
        /// </summary>
        [Fact]
        public void MedicalCaseComplexUpdate_ShouldUpdateAllRelevantFields()
        {
            // Arrange - 模拟数据库中的现有记录
            var existingCase = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC202501002",
                ChiefComplaint = "原始主诉",
                PresentIllness = "原始现病史",
                PastHistory = "原始既往史",
                PersonalHistory = "原始个人史",
                FamilyHistory = "原始家族史",
                PhysicalExam = "原始体格检查",
                Diagnosis = "原始诊断",
                TreatmentPlan = "原始治疗方案",
                FollowUpPlan = "原始随访计划",
                Status = MedicalCaseStatus.Registered,
                Severity = CaseSeverity.Mild,
                Priority = CasePriority.Low,
                Remark = "原始备注",
                CreateTime = DateTime.Now.AddDays(-7),
                UpdateTime = DateTime.Now.AddDays(-1)
            };

            // 保存原始值用于验证未更新字段不变
            var originalPatientId = existingCase.PatientId;
            var originalDoctorId = existingCase.DoctorId;
            var originalCaseNumber = existingCase.CaseNumber;
            var originalCreateTime = existingCase.CreateTime;

            var complexUpdateDto = new MedicalCaseUpdateDto
            {
                Id = existingCase.Id,
                ChiefComplaint = "完全更新的主诉内容",
                PresentIllness = "完全更新的现病史，包含详细症状描述",
                PastHistory = "完全更新的既往史，包含多种疾病",
                PersonalHistory = "完全更新的个人史",
                FamilyHistory = "完全更新的家族史",
                PhysicalExam = "完全更新的体格检查结果",
                Diagnosis = "完全更新的诊断结果",
                TreatmentPlan = "完全更新的综合治疗方案",
                FollowUpPlan = "完全更新的详细随访计划",
                Status = "Completed",
                Remark = "完全更新的详细备注信息"
            };

            // Act
            _mapper.Map(complexUpdateDto, existingCase);

            // Assert - 验证所有更新字段
            Assert.Equal("完全更新的主诉内容", existingCase.ChiefComplaint);
            Assert.Equal("完全更新的现病史，包含详细症状描述", existingCase.PresentIllness);
            Assert.Equal("完全更新的既往史，包含多种疾病", existingCase.PastHistory);
            Assert.Equal("完全更新的个人史", existingCase.PersonalHistory);
            Assert.Equal("完全更新的家族史", existingCase.FamilyHistory);
            Assert.Equal("完全更新的体格检查结果", existingCase.PhysicalExam);
            Assert.Equal("完全更新的诊断结果", existingCase.Diagnosis);
            Assert.Equal("完全更新的综合治疗方案", existingCase.TreatmentPlan);
            Assert.Equal("完全更新的详细随访计划", existingCase.FollowUpPlan);
            Assert.Equal(MedicalCaseStatus.Completed, existingCase.Status);
            Assert.Equal("完全更新的详细备注信息", existingCase.Remark);

            // Assert - 验证不应更新的字段保持不变
            Assert.Equal(originalPatientId, existingCase.PatientId);
            Assert.Equal(originalDoctorId, existingCase.DoctorId);
            Assert.Equal(originalCaseNumber, existingCase.CaseNumber);
            Assert.Equal(originalCreateTime, existingCase.CreateTime);
        }

        /// <summary>
        /// 测试字段更新完整性的边界条件
        /// 确保空值和null值的正确处理
        /// </summary>
        [Theory]
        [InlineData("", "空字符串测试")]
        [InlineData(null, "null值测试")]
        [InlineData("   ", "空白字符测试")]
        public void MedicalCaseUpdate_ShouldHandleEdgeCases(string testValue, string testDescription)
        {
            // Arrange
            var existingCase = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                ChiefComplaint = "原始值",
                Diagnosis = "原始诊断"
            };

            var updateDto = new MedicalCaseUpdateDto
            {
                Id = existingCase.Id,
                ChiefComplaint = testValue,
                Diagnosis = testValue
            };

            // Act & Assert - 映射不应抛出异常
            var exception = Record.Exception(() =>
            {
                _mapper.Map(updateDto, existingCase);
            });

            Assert.Null(exception);
            // 验证值确实被更新了（即使是空值）
            Assert.Equal(testValue, existingCase.ChiefComplaint);
            Assert.Equal(testValue, existingCase.Diagnosis);
        }
    }
}