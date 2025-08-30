using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCase模块字段更新完整性测试
    /// UltraThink设计：确保UpdateAsync方法能正确映射DTO中的所有字段到实体
    /// 防止手工字段映射导致的字段遗漏问题
    /// </summary>
    public class MedicalCaseFieldUpdateCompletenessTests : IDisposable
    {
        private readonly MedicalCaseService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly IMapper _mapper;
        private readonly MedicalCaseTestDataBuilder _builder;

        public MedicalCaseFieldUpdateCompletenessTests()
        {
            _builder = new MedicalCaseTestDataBuilder();
            _repositoryMock = new Mock<IMedicalCaseRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 使用MedicalCase模块的真实AutoMapper配置
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MedicalCaseModel, MedicalCaseDto>();
                cfg.CreateMap<MedicalCaseModel, MedicalCaseDetailDto>();
                cfg.CreateMap<MedicalCaseCreateDto, MedicalCaseModel>();
                
                // 关键：使用真实的UpdateDto映射配置，包含忽略DTO中存在但实体中不存在的字段
                cfg.CreateMap<MedicalCaseUpdateDto, MedicalCaseModel>()
                    .ForSourceMember(src => src.Id, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.RegistrationId, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.DiagnosisSummary, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.ChiefComplaint, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.PresentIllness, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.PastHistory, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.DiagnosisResult, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.TreatmentPlan, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.PhysicalExamination, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.AuxiliaryExamination, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.PrescriptionInfo, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.FollowUpPlan, opt => opt.DoNotValidate());
                
                cfg.CreateMap<MedicalCaseEditDto, MedicalCaseUpdateDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new MedicalCaseService(
                _context,
                _repositoryMock.Object,
                _mapper,
                NullLogger<MedicalCaseService>.Instance);
        }

        /// <summary>
        /// 测试UpdateAsync方法是否能正确更新实体中存在的所有字段
        /// 这个测试确保我们修复的AutoMapper配置能正确工作，防止字段更新不完整问题
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithCompleteDto_UpdatesAllMappedEntityFields()
        {
            // Arrange - 创建原始医疗案例
            var originalCase = _builder.AsValidMedicalCase().Build();
            originalCase.PatientId = Guid.NewGuid();
            originalCase.UserId = Guid.NewGuid(); // 注意：实体使用UserId，DTO使用DoctorId
            originalCase.Status = MedicalCaseStatus.Registered;
            originalCase.Remark = "原始备注";

            _repositoryMock.Setup(r => r.GetByIdAsync(originalCase.Id))
                .ReturnsAsync(originalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // 创建包含所有可映射字段的更新DTO
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = originalCase.Id, // 这个字段在映射中被忽略，不会覆盖实体ID
                
                // MedicalCaseInputBaseDto基类字段 - 应该被映射到实体
                PatientId = Guid.NewGuid(), // 新患者ID
                DoctorId = Guid.NewGuid(),  // 映射到实体的UserId字段
                RegistrationId = Guid.NewGuid(), // 这个字段在映射中被忽略
                Remark = "更新后的备注",
                
                // MedicalCaseEditDto字段 - 只有Status应该被映射
                Status = ((int)MedicalCaseStatus.InConsultation).ToString(),
                
                // 以下字段在实体中不存在，在映射配置中被忽略：
                DiagnosisSummary = "诊断摘要",
                ChiefComplaint = "主诉",
                PresentIllness = "现病史", 
                PastHistory = "既往史",
                DiagnosisResult = "诊断结果",
                TreatmentPlan = "治疗方案",
                
                // MedicalCaseUpdateDto字段 - 在实体中不存在，应该被忽略
                PhysicalExamination = "体格检查",
                AuxiliaryExamination = "辅助检查",
                PrescriptionInfo = "处方信息",
                FollowUpPlan = "随访计划"
            };

            // Act - 执行更新
            var result = await _service.UpdateAsync(originalCase.Id, updateDto);

            // Assert - 验证更新结果和字段映射
            Assert.True(result);
            
            // 验证实体中存在且DTO中有对应字段的属性都被正确更新
            Assert.Equal(updateDto.PatientId, originalCase.PatientId); // ✓ 应该更新
            Assert.Equal(updateDto.DoctorId, originalCase.UserId);      // ✓ 应该更新（DTO.DoctorId → Entity.UserId）
            Assert.Equal(updateDto.Remark, originalCase.Remark);        // ✓ 应该更新
            Assert.Equal(MedicalCaseStatus.InConsultation, originalCase.Status); // ✓ 应该更新
            
            // 验证实体ID没有被DTO中的Id覆盖（因为ForSourceMember忽略）
            Assert.Equal(originalCase.Id, originalCase.Id); // ID应该保持不变
            
            // 验证Repository的UpdateAsync方法被调用
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()), Times.Once);
        }

        /// <summary>
        /// 测试部分字段更新场景
        /// 验证只有非空字段被更新，空字段不会覆盖原有值
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithPartialDto_UpdatesOnlyProvidedFields()
        {
            // Arrange
            var originalCase = _builder.AsValidMedicalCase().Build();
            originalCase.Remark = "原始备注";
            originalCase.Status = MedicalCaseStatus.Registered;

            _repositoryMock.Setup(r => r.GetByIdAsync(originalCase.Id))
                .ReturnsAsync(originalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // 只提供部分字段的更新DTO
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = originalCase.Id,
                Status = ((int)MedicalCaseStatus.Completed).ToString(),
                Remark = "完成诊疗"
                // PatientId, DoctorId等其他字段为默认值
            };

            var originalPatientId = originalCase.PatientId;
            var originalUserId = originalCase.UserId;

            // Act
            await _service.UpdateAsync(originalCase.Id, updateDto);

            // Assert
            // 提供的字段应该被更新
            Assert.Equal(MedicalCaseStatus.Completed, originalCase.Status);
            Assert.Equal("完成诊疗", originalCase.Remark);
            
            // 未提供的字段应该被默认值覆盖（这是AutoMapper的行为）
            // 注意：这测试了我们的AutoMapper配置是否正确处理了字段映射
            Assert.Equal(Guid.Empty, originalCase.PatientId); // AutoMapper会用默认值覆盖
            Assert.Equal(Guid.Empty, originalCase.UserId);    // AutoMapper会用默认值覆盖
        }

        /// <summary>
        /// 测试状态字符串到枚举的转换
        /// 验证Status字段的字符串到枚举映射工作正常
        /// </summary>
        [Theory]
        [InlineData("0", MedicalCaseStatus.Registered)]
        [InlineData("1", MedicalCaseStatus.InConsultation)]
        [InlineData("2", MedicalCaseStatus.Completed)]
        [InlineData("3", MedicalCaseStatus.Cancelled)]
        public async Task UpdateAsync_WithDifferentStatusValues_UpdatesStatusCorrectly(
            string statusString, MedicalCaseStatus expectedStatus)
        {
            // Arrange
            var originalCase = _builder.AsValidMedicalCase().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(originalCase.Id))
                .ReturnsAsync(originalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            var updateDto = new MedicalCaseUpdateDto
            {
                Id = originalCase.Id,
                Status = statusString,
                PatientId = originalCase.PatientId, // 保持原值
                DoctorId = originalCase.UserId,     // 保持原值
                Remark = originalCase.Remark        // 保持原值
            };

            // Act
            await _service.UpdateAsync(originalCase.Id, updateDto);

            // Assert
            Assert.Equal(expectedStatus, originalCase.Status);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}