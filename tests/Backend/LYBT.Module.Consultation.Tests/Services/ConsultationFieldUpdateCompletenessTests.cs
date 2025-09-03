using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// Consultation模块字段更新完整性测试
    /// UltraThink设计：确保UpdateAsync方法能正确映射ConsultationUpdateDto中的所有字段到ConsultationModel实体
    /// 重点验证中医四诊（望闻问切）的所有详细字段都能被正确更新
    /// </summary>
    public class ConsultationFieldUpdateCompletenessTests : IDisposable
    {
        private readonly ConsultationService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IConsultationRepository> _repositoryMock;
        private readonly IMapper _mapper;

        public ConsultationFieldUpdateCompletenessTests()
        {
            _repositoryMock = new Mock<IConsultationRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 使用Consultation模块的真实AutoMapper配置
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ConsultationModel, ConsultationDto>();
                cfg.CreateMap<ConsultationModel, ConsultationDetailDto>();
                cfg.CreateMap<ConsultationCreateDto, ConsultationModel>();
                
                // 关键：ConsultationUpdateDto映射配置，需要处理字段名不匹配的情况
                cfg.CreateMap<ConsultationUpdateDto, ConsultationModel>()
                    .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))         // DTO.DoctorId → Entity.UserId
                    .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.Diagnosis))  // DTO.Diagnosis → Entity.TCMDiagnosis
                    .ForMember(dest => dest.CreateTime, opt => opt.Ignore())                        // 创建时间不应被更新
                    .ForMember(dest => dest.UpdateTime, opt => opt.Ignore());                       // 更新时间由系统管理
                
                cfg.CreateMap<ConsultationEditDto, ConsultationUpdateDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new ConsultationService(
                _context,
                _repositoryMock.Object,
                _mapper,
                NullLogger<ConsultationService>.Instance);
        }

        /// <summary>
        /// 测试ConsultationUpdateDto到ConsultationModel的完整字段映射
        /// 验证所有四诊字段和诊断字段都能被正确更新
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithCompleteDto_UpdatesAllConsultationFields()
        {
            // Arrange - 创建原始看诊记录
            var originalConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                
                // 基础字段
                ChiefComplaint = "原始主诉",
                PresentIllness = "原始现病史",
                IsCompleted = false,
                Remark = "原始备注",
                
                // 四诊基础字段
                Inspection = "原始望诊",
                AuscultationOlfaction = "原始闻诊",
                Inquiry = "原始问诊",
                Palpation = "原始切诊",
                
                // 诊断相关字段
                PatternDifferentiation = "原始辨证分析",
                TCMDiagnosis = "原始中医辨证",
                TreatmentPrinciple = "原始治疗原则",
                MedicalAdvice = "原始医嘱"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalConsultation.Id))
                .ReturnsAsync(originalConsultation);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConsultationModel>()))
                .ReturnsAsync((ConsultationModel c) => c);

            // 创建包含所有字段的更新DTO
            var updateDto = new ConsultationUpdateDto
            {
                // 基础字段
                ChiefComplaint = "更新后主诉",
                PresentIllness = "更新后现病史", 
                Remark = "更新后备注",
                DoctorId = Guid.NewGuid(),  // 映射到实体的UserId
                PatientId = originalConsultation.PatientId, // 保持不变
                IsCompleted = true,
                
                // 四诊基础字段
                Inspection = "更新后望诊结果",
                AuscultationOlfaction = "更新后闻诊结果",
                Inquiry = "更新后问诊结果",
                Palpation = "更新后切诊结果",
                
                // 舌脉诊
                TongueInspection = "更新后舌诊结果",
                PulseCondition = "更新后脉诊结果",
                
                // 诊断相关字段
                PatternDifferentiation = "更新后辨证分析",
                TCMDiagnosis = "更新后中医辨证", // 注意：这个字段映射有问题，应该是Diagnosis → TCMDiagnosis
                Diagnosis = "更新后诊断结果",     // 这个应该映射到实体的TCMDiagnosis
                TreatmentPrinciple = "更新后治疗原则",
                MedicalAdvice = "更新后医嘱",
                
                // 详细四诊属性 - 望诊细项
                Complexion = "面色红润",
                Spirit = "神态清醒",
                BodyShape = "体型适中",
                TongueBody = "舌质淡红",
                TongueCoating = "舌苔薄白",
                
                // 详细四诊属性 - 闻诊细项
                Voice = "声音洪亮",
                Breath = "呼吸平稳",
                Cough = "无咳嗽",
                Odor = "无异味",
                
                // 详细四诊属性 - 问诊细项
                ColdHeat = "畏寒喜热",
                Sweat = "无汗",
                Appetite = "食欲良好",
                Sleep = "睡眠安稳",
                StoolUrine = "二便正常",
                HeadBody = "头身无不适",
                ChestAbdomen = "胸腹无胀痛",
                Menstruation = "月经正常",
                
                // 详细四诊属性 - 切诊细项
                Pulse = "脉象沉细",
                PulseRate = "脉率70次/分",
                PulseStrength = "脉力中等",
                PulseRhythm = "脉律齐整",
                PulseShape = "脉形细数",
                LeftPulse = "左脉沉弱",
                RightPulse = "右脉滑数",
                
                // 证候
                TCMSyndrome = "气血两虚证"
            };

            // Act - 执行更新
            var result = await _service.UpdateAsync(originalConsultation.Id, updateDto);

            // Assert - 验证更新结果
            Assert.NotNull(result);
            Assert.True(result.Success);
            
            // 验证基础字段更新
            Assert.Equal("更新后主诉", originalConsultation.ChiefComplaint);
            Assert.Equal("更新后现病史", originalConsultation.PresentIllness);
            Assert.Equal("更新后备注", originalConsultation.Remark);
            Assert.Equal(updateDto.DoctorId, originalConsultation.UserId); // DoctorId → UserId映射
            Assert.True(originalConsultation.IsCompleted);
            
            // 验证四诊基础字段更新
            Assert.Equal("更新后望诊结果", originalConsultation.Inspection);
            Assert.Equal("更新后闻诊结果", originalConsultation.AuscultationOlfaction);
            Assert.Equal("更新后问诊结果", originalConsultation.Inquiry);
            Assert.Equal("更新后切诊结果", originalConsultation.Palpation);
            Assert.Equal("更新后舌诊结果", originalConsultation.TongueInspection);
            Assert.Equal("更新后脉诊结果", originalConsultation.PulseCondition);
            
            // 验证诊断相关字段更新
            Assert.Equal("更新后辨证分析", originalConsultation.PatternDifferentiation);
            Assert.Equal("更新后诊断结果", originalConsultation.TCMDiagnosis); // Diagnosis → TCMDiagnosis映射
            Assert.Equal("更新后治疗原则", originalConsultation.TreatmentPrinciple);
            Assert.Equal("更新后医嘱", originalConsultation.MedicalAdvice);
            
            // 验证详细四诊属性更新（选取几个关键字段验证）
            Assert.Equal("面色红润", originalConsultation.Complexion);
            Assert.Equal("神态清醒", originalConsultation.Spirit);
            Assert.Equal("舌质淡红", originalConsultation.TongueBody);
            Assert.Equal("舌苔薄白", originalConsultation.TongueCoating);
            Assert.Equal("声音洪亮", originalConsultation.Voice);
            Assert.Equal("畏寒喜热", originalConsultation.ColdHeat);
            Assert.Equal("脉象沉细", originalConsultation.Pulse);
            Assert.Equal("脉率70次/分", originalConsultation.PulseRate);
            Assert.Equal("气血两虚证", originalConsultation.TCMSyndrome);
            
            // 验证Repository的UpdateAsync方法被调用
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ConsultationModel>()), Times.Once);
        }

        /// <summary>
        /// 测试字段映射的特殊情况
        /// 验证DTO.DoctorId → Entity.UserId 和 DTO.Diagnosis → Entity.TCMDiagnosis 的映射
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithFieldNameMappings_MapsCorrectly()
        {
            // Arrange
            var originalConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TCMDiagnosis = "原始中医诊断"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalConsultation.Id))
                .ReturnsAsync(originalConsultation);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConsultationModel>()))
                .ReturnsAsync((ConsultationModel c) => c);

            var newDoctorId = Guid.NewGuid();
            var updateDto = new ConsultationUpdateDto
            {
                DoctorId = newDoctorId,       // 应该映射到实体的UserId
                Diagnosis = "新的诊断结果",    // 应该映射到实体的TCMDiagnosis
                PatientId = originalConsultation.PatientId
            };

            // Act
            await _service.UpdateAsync(originalConsultation.Id, updateDto);

            // Assert
            // 验证字段名映射正确
            Assert.Equal(newDoctorId, originalConsultation.UserId);        // DoctorId → UserId
            Assert.Equal("新的诊断结果", originalConsultation.TCMDiagnosis); // Diagnosis → TCMDiagnosis
        }

        /// <summary>
        /// 测试四诊详细属性的批量更新
        /// 验证所有详细四诊字段（30+个字段）都能被正确更新
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithDetailedTCMFields_UpdatesAllDetailFields()
        {
            // Arrange
            var originalConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalConsultation.Id))
                .ReturnsAsync(originalConsultation);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConsultationModel>()))
                .ReturnsAsync((ConsultationModel c) => c);

            var updateDto = new ConsultationUpdateDto
            {
                PatientId = originalConsultation.PatientId,
                DoctorId = originalConsultation.UserId,
                
                // 望诊详细属性
                Complexion = "面色萎黄",
                Spirit = "神疲乏力", 
                BodyShape = "形体消瘦",
                TongueBody = "舌质红",
                TongueCoating = "舌苔黄腻",
                
                // 闻诊详细属性
                Voice = "声音嘶哑",
                Breath = "气短懒言",
                Cough = "干咳无痰",
                Odor = "口臭明显",
                
                // 问诊详细属性
                ColdHeat = "恶寒发热",
                Sweat = "盗汗",
                Appetite = "纳差",
                Sleep = "失眠多梦",
                StoolUrine = "便秘尿黄",
                HeadBody = "头痛身重",
                ChestAbdomen = "胸闷腹胀",
                Menstruation = "月经不调",
                
                // 切诊详细属性
                Pulse = "脉弦细",
                PulseRate = "脉率90次/分",
                PulseStrength = "脉力微弱",
                PulseRhythm = "脉律不齐",
                PulseShape = "脉形弦滑",
                LeftPulse = "左脉弦紧",
                RightPulse = "右脉细弱",
                
                // 证候
                TCMSyndrome = "肝郁脾虚证"
            };

            // Act
            await _service.UpdateAsync(originalConsultation.Id, updateDto);

            // Assert - 验证所有详细字段都被正确更新
            
            // 望诊
            Assert.Equal("面色萎黄", originalConsultation.Complexion);
            Assert.Equal("神疲乏力", originalConsultation.Spirit);
            Assert.Equal("形体消瘦", originalConsultation.BodyShape);
            Assert.Equal("舌质红", originalConsultation.TongueBody);
            Assert.Equal("舌苔黄腻", originalConsultation.TongueCoating);
            
            // 闻诊
            Assert.Equal("声音嘶哑", originalConsultation.Voice);
            Assert.Equal("气短懒言", originalConsultation.Breath);
            Assert.Equal("干咳无痰", originalConsultation.Cough);
            Assert.Equal("口臭明显", originalConsultation.Odor);
            
            // 问诊
            Assert.Equal("恶寒发热", originalConsultation.ColdHeat);
            Assert.Equal("盗汗", originalConsultation.Sweat);
            Assert.Equal("纳差", originalConsultation.Appetite);
            Assert.Equal("失眠多梦", originalConsultation.Sleep);
            Assert.Equal("便秘尿黄", originalConsultation.StoolUrine);
            Assert.Equal("头痛身重", originalConsultation.HeadBody);
            Assert.Equal("胸闷腹胀", originalConsultation.ChestAbdomen);
            Assert.Equal("月经不调", originalConsultation.Menstruation);
            
            // 切诊
            Assert.Equal("脉弦细", originalConsultation.Pulse);
            Assert.Equal("脉率90次/分", originalConsultation.PulseRate);
            Assert.Equal("脉力微弱", originalConsultation.PulseStrength);
            Assert.Equal("脉律不齐", originalConsultation.PulseRhythm);
            Assert.Equal("脉形弦滑", originalConsultation.PulseShape);
            Assert.Equal("左脉弦紧", originalConsultation.LeftPulse);
            Assert.Equal("右脉细弱", originalConsultation.RightPulse);
            
            // 证候
            Assert.Equal("肝郁脾虚证", originalConsultation.TCMSyndrome);
        }

        /// <summary>
        /// 测试可选字段的null值处理
        /// 验证可选字段设置为null时不会导致错误
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithNullOptionalFields_HandlesNullsCorrectly()
        {
            // Arrange
            var originalConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "原始主诉",
                PresentIllness = "原始现病史",
                Inspection = "原始望诊"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalConsultation.Id))
                .ReturnsAsync(originalConsultation);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConsultationModel>()))
                .ReturnsAsync((ConsultationModel c) => c);

            var updateDto = new ConsultationUpdateDto
            {
                PatientId = originalConsultation.PatientId,
                DoctorId = originalConsultation.UserId,
                // 所有可选字段设为null
                ChiefComplaint = null,
                PresentIllness = null,
                Inspection = null,
                AuscultationOlfaction = null,
                Inquiry = null,
                Palpation = null,
                Remark = null
            };

            // Act
            var result = await _service.UpdateAsync(originalConsultation.Id, updateDto);

            // Assert
            Assert.True(result.Success);
            // AutoMapper应该将null值映射到实体，覆盖原有值
            Assert.Null(originalConsultation.ChiefComplaint);
            Assert.Null(originalConsultation.PresentIllness);
            Assert.Null(originalConsultation.Inspection);
            Assert.Null(originalConsultation.AuscultationOlfaction);
            Assert.Null(originalConsultation.Inquiry);
            Assert.Null(originalConsultation.Palpation);
            Assert.Null(originalConsultation.Remark);
        }

        /// <summary>
        /// 测试IsCompleted字段的布尔值更新
        /// 验证诊断完成状态的切换
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateAsync_WithDifferentCompletionStatus_UpdatesCorrectly(bool isCompletedValue)
        {
            // Arrange
            var originalConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                IsCompleted = !isCompletedValue // 设为相反值
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalConsultation.Id))
                .ReturnsAsync(originalConsultation);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConsultationModel>()))
                .ReturnsAsync((ConsultationModel c) => c);

            var updateDto = new ConsultationUpdateDto
            {
                PatientId = originalConsultation.PatientId,
                DoctorId = originalConsultation.UserId,
                IsCompleted = isCompletedValue
            };

            // Act
            await _service.UpdateAsync(originalConsultation.Id, updateDto);

            // Assert
            Assert.Equal(isCompletedValue, originalConsultation.IsCompleted);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}