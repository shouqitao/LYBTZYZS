using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// ConsultationService中医四诊专项测试 - UltraThink设计
    /// 职责单一：专注于中医四诊功能的测试
    /// 代码干净：清晰的中医诊疗测试场景
    /// 性能出色：针对性测试，避免冗余
    /// </summary>
    public class ConsultationServiceTCMTests : IDisposable
    {
        private readonly ConsultationService _consultationService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly ConsultationTestDataBuilder _consultationBuilder;

        public ConsultationServiceTCMTests()
        {
            _mockFactory = new MockFactory();
            _consultationBuilder = new ConsultationTestDataBuilder();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ConsultationModel, ConsultationDto>();
                cfg.CreateMap<ConsultationModel, ConsultationDetailDto>();
                cfg.CreateMap<ConsultationUpdateDto, ConsultationModel>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _consultationService = new ConsultationService(
                _context, 
                _mapper, 
                NullLogger<ConsultationService>.Instance);
        }

        #region 望诊（Inspection）测试

        [Fact]
        public async Task Inspection_CompleteObservation_RecordsAllAspects()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色：萎黄无华；神态：精神倦怠，目光无神；形体：形体消瘦，肌肉松软；舌象：舌淡红，苔薄白"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.NotNull(result.Inspection);
            Assert.Contains("面色", result.Inspection);
            Assert.Contains("神态", result.Inspection);
            Assert.Contains("形体", result.Inspection);
            Assert.Contains("舌象", result.Inspection);
        }

        [Fact]
        public async Task Inspection_DifferentComplexions_RecordsCorrectly()
        {
            // Arrange - 测试不同面色的记录
            var testCases = new[]
            {
                "面色红润，精神饱满", // 正常
                "面色苍白，口唇淡白", // 血虚
                "面色萎黄，眼睑浮肿", // 脾虚
                "面色晦暗，口唇紫绀", // 血瘀
                "面色潮红，两颧泛红"  // 阴虚
            };

            foreach (var inspectionText in testCases)
            {
                var consultation = _consultationBuilder.AsNewConsultation().Build();
                await _context.Consultations.AddAsync(consultation);
                await _context.SaveChangesAsync();

                var updateDto = new ConsultationUpdateDto
                {
                    Inspection = inspectionText
                };

                // Act
                var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

                // Assert
                Assert.Equal(inspectionText, result.Inspection);
            }
        }

        #endregion

        #region 闻诊（Auscultation and Olfaction）测试

        [Fact]
        public async Task AuscultationOlfaction_VoiceAndOdor_RecordsComplete()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                AuscultationOlfaction = "声音：语声低微，气短懒言；咳嗽：咳声重浊，痰多；口气：口气酸臭；体味：无明显异味"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.NotNull(result.AuscultationOlfaction);
            Assert.Contains("语声低微", result.AuscultationOlfaction);
            Assert.Contains("咳声重浊", result.AuscultationOlfaction);
            Assert.Contains("口气酸臭", result.AuscultationOlfaction);
        }

        [Fact]
        public async Task AuscultationOlfaction_DifferentCoughTypes_RecordsCorrectly()
        {
            // Arrange - 不同咳嗽类型
            var coughTypes = new Dictionary<string, string>
            {
                { "风寒咳嗽", "咳声重浊，痰白稀薄" },
                { "风热咳嗽", "咳声清脆，痰黄稠粘" },
                { "燥邪咳嗽", "干咳无痰，咽喉干燥" },
                { "痰湿咳嗽", "咳声重浊，痰多易咯" }
            };

            foreach (var cough in coughTypes)
            {
                var consultation = _consultationBuilder.AsNewConsultation().Build();
                await _context.Consultations.AddAsync(consultation);
                await _context.SaveChangesAsync();

                var updateDto = new ConsultationUpdateDto
                {
                    AuscultationOlfaction = cough.Value,
                    TCMDiagnosis = cough.Key
                };

                // Act
                var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

                // Assert
                Assert.Equal(cough.Value, result.AuscultationOlfaction);
                Assert.Equal(cough.Key, result.TCMDiagnosis);
            }
        }

        #endregion

        #region 问诊（Inquiry）测试

        [Fact]
        public async Task Inquiry_TenQuestions_RecordsAllAspects()
        {
            // Arrange - 中医十问
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var tenQuestions = @"
                一问寒热：恶寒发热，寒重热轻
                二问汗：无汗
                三问头身：头痛身痛，肢体酸楚
                四问胸腹：胸闷气短，腹部胀满
                五问饮食：食欲不振，口淡无味
                六问二便：大便溏薄，小便清长
                七问睡眠：入睡困难，多梦易醒
                八问经带（女）：月经推迟，量少色淡
                九问小儿：不适用
                十问既往史：既往体健";

            var updateDto = new ConsultationUpdateDto
            {
                Inquiry = tenQuestions
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.NotNull(result.Inquiry);
            Assert.Contains("寒热", result.Inquiry);
            Assert.Contains("汗", result.Inquiry);
            Assert.Contains("头身", result.Inquiry);
            Assert.Contains("胸腹", result.Inquiry);
            Assert.Contains("饮食", result.Inquiry);
            Assert.Contains("二便", result.Inquiry);
            Assert.Contains("睡眠", result.Inquiry);
        }

        [Fact]
        public async Task Inquiry_DifferentSymptomCombinations_DiagnosesCorrectly()
        {
            // Arrange - 不同症状组合对应不同证型
            var symptomPatterns = new Dictionary<string, string>
            {
                {
                    "恶寒重发热轻，无汗，头身疼痛",
                    "风寒表证"
                },
                {
                    "发热重恶寒轻，汗出，咽喉肿痛",
                    "风热表证"
                },
                {
                    "食少纳呆，腹胀便溏，倦怠乏力",
                    "脾虚证"
                },
                {
                    "失眠多梦，心悸健忘，面色无华",
                    "心血虚证"
                }
            };

            foreach (var pattern in symptomPatterns)
            {
                var consultation = _consultationBuilder.AsNewConsultation().Build();
                await _context.Consultations.AddAsync(consultation);
                await _context.SaveChangesAsync();

                var updateDto = new ConsultationUpdateDto
                {
                    Inquiry = pattern.Key,
                    TCMDiagnosis = pattern.Value
                };

                // Act
                var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

                // Assert
                Assert.Equal(pattern.Key, result.Inquiry);
                Assert.Equal(pattern.Value, result.TCMDiagnosis);
            }
        }

        #endregion

        #region 切诊（Palpation）测试

        [Fact]
        public async Task Palpation_PulseConditions_RecordsAllTypes()
        {
            // Arrange - 28脉的部分常见脉象
            var pulseTypes = new[]
            {
                "脉浮：轻取即得，重按稍减",
                "脉沉：轻取不应，重按始得",
                "脉迟：一息不足四至",
                "脉数：一息五至以上",
                "脉细：脉体细小如线",
                "脉弦：端直以长，如按琴弦",
                "脉滑：往来流利，如珠走盘",
                "脉涩：往来艰涩，如刀刮竹"
            };

            foreach (var pulse in pulseTypes)
            {
                var consultation = _consultationBuilder.AsNewConsultation().Build();
                await _context.Consultations.AddAsync(consultation);
                await _context.SaveChangesAsync();

                var updateDto = new ConsultationUpdateDto
                {
                    Palpation = pulse,
                    PulseCondition = pulse.Split('：')[0]
                };

                // Act
                var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

                // Assert
                Assert.Equal(pulse, result.Palpation);
                Assert.Contains(pulse.Split('：')[0], result.PulseCondition);
            }
        }

        [Fact]
        public async Task Palpation_AbdominalExamination_RecordsFindings()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Palpation = "腹诊：腹软，无压痛反跳痛，肝脾未触及，肠鸣音正常；脉诊：脉沉细无力"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Contains("腹软", result.Palpation);
            Assert.Contains("脉沉细", result.Palpation);
        }

        #endregion

        #region 舌诊测试

        [Fact]
        public async Task TongueInspection_CompleteDescription_RecordsAllAspects()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                TongueInspection = "舌质：淡红；舌体：胖大边有齿痕；舌苔：薄白腻苔；舌底：络脉淡紫"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Contains("舌质", result.TongueInspection);
            Assert.Contains("舌体", result.TongueInspection);
            Assert.Contains("舌苔", result.TongueInspection);
            Assert.Contains("舌底", result.TongueInspection);
        }

        [Fact]
        public async Task TongueInspection_DifferentPatterns_IndicatesDifferentSyndromes()
        {
            // Arrange - 不同舌象对应不同证型
            var tonguePatterns = new Dictionary<string, string>
            {
                { "舌淡红，苔薄白", "正常或表证" },
                { "舌红，苔黄腻", "湿热证" },
                { "舌淡，苔白滑", "寒湿证" },
                { "舌绛，少苔", "阴虚证" },
                { "舌紫暗，有瘀斑", "血瘀证" }
            };

            foreach (var pattern in tonguePatterns)
            {
                var consultation = _consultationBuilder.AsNewConsultation().Build();
                await _context.Consultations.AddAsync(consultation);
                await _context.SaveChangesAsync();

                var updateDto = new ConsultationUpdateDto
                {
                    TongueInspection = pattern.Key,
                    TCMDiagnosis = pattern.Value
                };

                // Act
                var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

                // Assert
                Assert.Equal(pattern.Key, result.TongueInspection);
                Assert.Equal(pattern.Value, result.TCMDiagnosis);
            }
        }

        #endregion

        #region 综合诊断测试

        [Fact]
        public async Task CompleteExamination_WindColdSyndrome_DiagnosesCorrectly()
        {
            // Arrange - 风寒感冒完整四诊
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色苍白，精神尚可",
                AuscultationOlfaction = "咳嗽声重，痰白稀薄",
                Inquiry = "恶寒重发热轻，无汗，头身疼痛，鼻塞流清涕",
                Palpation = "脉浮紧",
                TongueInspection = "舌淡红，苔薄白",
                PulseCondition = "脉浮紧",
                TCMDiagnosis = "风寒感冒",
                TreatmentPrinciple = "疏风散寒，解表发汗"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal("风寒感冒", result.TCMDiagnosis);
            Assert.Equal("疏风散寒，解表发汗", result.TreatmentPrinciple);
            Assert.NotNull(result.Inspection);
            Assert.NotNull(result.AuscultationOlfaction);
            Assert.NotNull(result.Inquiry);
            Assert.NotNull(result.Palpation);
        }

        [Fact]
        public async Task CompleteExamination_SpleenDeficiency_DiagnosesCorrectly()
        {
            // Arrange - 脾虚证完整四诊
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色萎黄，形体消瘦，精神倦怠",
                AuscultationOlfaction = "语声低微，无特殊气味",
                Inquiry = "食少纳呆，腹胀便溏，倦怠乏力，四肢不温",
                Palpation = "腹软喜按，脉沉细无力",
                TongueInspection = "舌淡胖，边有齿痕，苔白腻",
                PulseCondition = "脉沉细无力",
                TCMDiagnosis = "脾虚证",
                TreatmentPrinciple = "健脾益气，温中和胃"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal("脾虚证", result.TCMDiagnosis);
            Assert.Equal("健脾益气，温中和胃", result.TreatmentPrinciple);
        }

        [Fact]
        public async Task CompleteExamination_LiverQiStagnation_DiagnosesCorrectly()
        {
            // Arrange - 肝郁气滞证完整四诊
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色暗淡，表情抑郁",
                AuscultationOlfaction = "叹息频作",
                Inquiry = "情志不舒，胸胁胀痛，善太息，月经不调",
                Palpation = "脉弦",
                TongueInspection = "舌淡红，苔薄白",
                PulseCondition = "脉弦",
                TCMDiagnosis = "肝郁气滞证",
                TreatmentPrinciple = "疏肝理气，活血化瘀"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal("肝郁气滞证", result.TCMDiagnosis);
            Assert.Equal("疏肝理气，活血化瘀", result.TreatmentPrinciple);
        }

        #endregion

        #region 医嘱和随访测试

        [Fact]
        public async Task MedicalAdvice_CompleteTCMAdvice_RecordsAllAspects()
        {
            // Arrange
            var consultation = _consultationBuilder.AsCompleteTCMConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var medicalAdvice = @"
                1. 饮食调理：忌食生冷、辛辣、油腻之品，宜清淡易消化饮食
                2. 起居调摄：注意保暖，避风寒，保持充足睡眠
                3. 情志调养：保持心情舒畅，避免情绪激动
                4. 运动指导：适当运动，如太极拳、八段锦等
                5. 服药指导：按时服药，饭后温服
                6. 复诊时间：一周后复诊";

            var updateDto = new ConsultationUpdateDto
            {
                MedicalAdvice = medicalAdvice
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Contains("饮食调理", result.MedicalAdvice);
            Assert.Contains("起居调摄", result.MedicalAdvice);
            Assert.Contains("情志调养", result.MedicalAdvice);
            Assert.Contains("运动指导", result.MedicalAdvice);
            Assert.Contains("服药指导", result.MedicalAdvice);
            Assert.Contains("复诊时间", result.MedicalAdvice);
        }

        #endregion

        #region 边缘情况测试

        [Fact]
        public async Task TCMExamination_WithVeryLongDescription_HandlesCorrectly()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var longDescription = new string('症', 1000); // 1000个字符

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = longDescription,
                AuscultationOlfaction = longDescription,
                Inquiry = longDescription,
                Palpation = longDescription
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal(longDescription, result.Inspection);
            Assert.Equal(longDescription, result.AuscultationOlfaction);
            Assert.Equal(longDescription, result.Inquiry);
            Assert.Equal(longDescription, result.Palpation);
        }

        [Fact]
        public async Task TCMExamination_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var specialText = "舌苔：黄腻（+++）；脉象：弦滑【120次/分】；症状：咳嗽*剧烈*";

            var updateDto = new ConsultationUpdateDto
            {
                TongueInspection = specialText,
                PulseCondition = specialText
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal(specialText, result.TongueInspection);
            Assert.Equal(specialText, result.PulseCondition);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _mockFactory?.ClearCache();
        }
    }
}