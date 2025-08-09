using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.Consultation;
using LYBT.Models.MedicalCase;
using LYBT.Models.Patients;
using LYBT.Models.Users;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// ConsultationService集成测试 - UltraThink设计
    /// 职责单一：专注于端到端工作流测试
    /// 代码干净：完整的业务场景模拟
    /// 性能出色：高效的集成测试执行
    /// </summary>
    public class ConsultationServiceIntegrationTests : IDisposable
    {
        private readonly ConsultationService _consultationService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly ConsultationTestDataBuilder _consultationBuilder;
        private readonly UserTestDataBuilder _userBuilder;

        public ConsultationServiceIntegrationTests()
        {
            _mockFactory = new MockFactory();
            _consultationBuilder = new ConsultationTestDataBuilder();
            _userBuilder = new UserTestDataBuilder();

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

        #region 完整诊疗流程集成测试

        [Fact]
        public async Task CompleteClinicWorkflow_FromRegistrationToCompletion_Success()
        {
            // Arrange - 准备患者、医生、医疗案例
            var patient = new PatientModel
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30),
                Phone = "13800138000",
                CreateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            var doctor = _userBuilder
                .AsValidUser()
                .WithRole(UserRole.Doctor)
                .WithRealName("李医生")
                .Build();

            var medicalCase = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Status = MedicalCaseStatus.Created,
                CreateTime = DateTime.Now,
                IsActive = true
            };

            await _context.Patients.AddAsync(patient);
            await _context.Users.AddAsync(doctor);
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act 1 - 开始看诊
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCase.Id,
                PatientId = patient.Id,
                UserId = doctor.Id
            };
            var consultation = await _consultationService.StartConsultationAsync(startDto);

            // Assert 1
            Assert.NotNull(consultation);
            Assert.Equal(patient.Id, consultation.PatientId);
            Assert.Equal(doctor.Id, consultation.UserId);

            // Act 2 - 进行中医四诊
            var fourExamDto = new ConsultationUpdateDto
            {
                Inspection = "面色萎黄，精神倦怠，形体消瘦",
                AuscultationOlfaction = "语声低微，口气清淡",
                Inquiry = "食欲不振，腹胀便溏，倦怠乏力",
                Palpation = "腹软喜按，脉沉细无力",
                TongueInspection = "舌淡胖，边有齿痕，苔白腻",
                PulseCondition = "脉沉细无力"
            };
            var afterExam = await _consultationService.UpdateConsultationAsync(consultation.Id, fourExamDto);

            // Assert 2
            Assert.NotNull(afterExam.Inspection);
            Assert.NotNull(afterExam.AuscultationOlfaction);
            Assert.NotNull(afterExam.Inquiry);
            Assert.NotNull(afterExam.Palpation);

            // Act 3 - 诊断和治疗方案
            var diagnosisDto = new ConsultationUpdateDto
            {
                TCMDiagnosis = "脾虚证",
                Diagnosis = "慢性胃炎，脾胃虚弱型",
                TreatmentPrinciple = "健脾益气，温中和胃",
                MedicalAdvice = "1. 饮食调理：忌生冷油腻\n2. 作息规律\n3. 适当运动"
            };
            var afterDiagnosis = await _consultationService.UpdateConsultationAsync(consultation.Id, diagnosisDto);

            // Assert 3
            Assert.Equal("脾虚证", afterDiagnosis.TCMDiagnosis);
            Assert.Equal("健脾益气，温中和胃", afterDiagnosis.TreatmentPrinciple);

            // Act 4 - 完成看诊
            var completeDto = new ConsultationCompleteDto
            {
                Diagnosis = afterDiagnosis.Diagnosis,
                TCMDiagnosis = afterDiagnosis.TCMDiagnosis,
                TreatmentPrinciple = afterDiagnosis.TreatmentPrinciple,
                MedicalAdvice = afterDiagnosis.MedicalAdvice,
                Summary = "患者脾虚证明显，予以中药调理"
            };
            var completed = await _consultationService.CompleteConsultationAsync(consultation.Id, completeDto);

            // Assert 4
            Assert.True(completed);
            var finalCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
            Assert.Equal(MedicalCaseStatus.Completed, finalCase!.Status);
        }

        [Fact]
        public async Task MultipleConsultations_SamePatient_TrackHistory()
        {
            // Arrange - 创建患者和多次就诊记录
            var patientId = Guid.NewGuid();
            var patient = new PatientModel
            {
                Id = patientId,
                Name = "王五",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-45),
                Phone = "13900139000",
                CreateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };
            await _context.Patients.AddAsync(patient);

            // 创建3次不同时间的就诊
            var diagnoses = new[]
            {
                ("风寒感冒", DateTime.Now.AddDays(-30)),
                ("脾胃虚寒", DateTime.Now.AddDays(-15)),
                ("肝郁气滞", DateTime.Now.AddDays(-5))
            };

            foreach (var (diagnosis, time) in diagnoses)
            {
                var consultation = _consultationBuilder
                    .AsCompleteTCMConsultation()
                    .WithPatientId(patientId)
                    .WithConsultationTime(time)
                    .WithTCMDiagnosis(diagnosis)
                    .Build();
                await _context.Consultations.AddAsync(consultation);
            }
            await _context.SaveChangesAsync();

            // Act - 获取患者历史
            var history = await _consultationService.GetPatientHistoryAsync(patientId);

            // Assert
            Assert.Equal(3, history.Count);
            Assert.Equal("肝郁气滞", history[0].Diagnosis); // 最近的在前
            Assert.Equal("脾胃虚寒", history[1].Diagnosis);
            Assert.Equal("风寒感冒", history[2].Diagnosis);
        }

        #endregion

        #region 批量操作集成测试

        [Fact]
        public async Task BatchConsultations_MultiplePatients_ProcessedCorrectly()
        {
            // Arrange - 创建多个患者和医疗案例
            var patients = new List<PatientModel>();
            var medicalCases = new List<MedicalCaseModel>();
            var doctorId = Guid.NewGuid();

            for (int i = 0; i < 5; i++)
            {
                var patient = new PatientModel
                {
                    Id = Guid.NewGuid(),
                    Name = $"患者{i + 1}",
                    Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                    BirthDate = DateTime.Now.AddYears(-20 - i * 5),
                    Phone = $"1380013800{i}",
                    CreateTime = DateTime.Now,
                    Status = CommonStatus.Enabled
                };
                patients.Add(patient);

                var medicalCase = new MedicalCaseModel
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    Status = MedicalCaseStatus.Created,
                    CreateTime = DateTime.Now,
                    IsActive = true
                };
                medicalCases.Add(medicalCase);
            }

            await _context.Patients.AddRangeAsync(patients);
            await _context.MedicalCases.AddRangeAsync(medicalCases);
            await _context.SaveChangesAsync();

            // Act - 批量开始看诊
            var consultations = new List<ConsultationDetailDto>();
            foreach (var (patient, medicalCase) in patients.Zip(medicalCases))
            {
                var startDto = new ConsultationStartDto
                {
                    MedicalCaseId = medicalCase.Id,
                    PatientId = patient.Id,
                    UserId = doctorId
                };
                var consultation = await _consultationService.StartConsultationAsync(startDto);
                consultations.Add(consultation);
            }

            // Assert
            Assert.Equal(5, consultations.Count);
            Assert.All(consultations, c => Assert.Equal(doctorId, c.UserId));

            // Act - 获取医生今日看诊列表
            var todayList = await _consultationService.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert
            Assert.Equal(5, todayList.Count);
        }

        #endregion

        #region 复杂查询集成测试

        [Fact]
        public async Task ComplexSearch_MultipleFilters_ReturnsCorrectResults()
        {
            // Arrange - 创建多样化的测试数据
            var doctorId = Guid.NewGuid();
            var patientIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
            
            var consultations = new List<ConsultationModel>
            {
                // 患者1的3次就诊
                _consultationBuilder.AsCompleteTCMConsultation()
                    .WithPatientId(patientIds[0])
                    .WithUserId(doctorId)
                    .WithConsultationTime(DateTime.Today.AddDays(-10))
                    .WithTCMDiagnosis("风寒感冒")
                    .Build(),
                _consultationBuilder.AsCompleteTCMConsultation()
                    .WithPatientId(patientIds[0])
                    .WithUserId(doctorId)
                    .WithConsultationTime(DateTime.Today.AddDays(-5))
                    .WithTCMDiagnosis("脾虚证")
                    .Build(),
                _consultationBuilder.AsCompleteTCMConsultation()
                    .WithPatientId(patientIds[0])
                    .WithUserId(doctorId)
                    .WithConsultationTime(DateTime.Today)
                    .WithTCMDiagnosis("肝郁气滞")
                    .Build(),
                
                // 患者2的2次就诊
                _consultationBuilder.AsCompleteTCMConsultation()
                    .WithPatientId(patientIds[1])
                    .WithUserId(doctorId)
                    .WithConsultationTime(DateTime.Today.AddDays(-7))
                    .WithTCMDiagnosis("风热感冒")
                    .Build(),
                _consultationBuilder.AsCompleteTCMConsultation()
                    .WithPatientId(patientIds[1])
                    .WithUserId(Guid.NewGuid()) // 不同医生
                    .WithConsultationTime(DateTime.Today.AddDays(-3))
                    .WithTCMDiagnosis("阴虚火旺")
                    .Build(),
                
                // 患者3的1次就诊
                _consultationBuilder.AsCompleteTCMConsultation()
                    .WithPatientId(patientIds[2])
                    .WithUserId(doctorId)
                    .WithConsultationTime(DateTime.Today.AddDays(-15))
                    .WithTCMDiagnosis("血瘀证")
                    .AsInactive() // 已删除
                    .Build()
            };

            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            // Act 1 - 查询特定医生在特定时间段的看诊
            var query1 = new ConsultationPagedQueryDto
            {
                UserId = doctorId,
                StartDate = DateTime.Today.AddDays(-8),
                EndDate = DateTime.Today,
                CurrentPage = 1,
                PageSize = 10
            };
            var result1 = await _consultationService.GetPagedAsync(query1);

            // Assert 1
            Assert.Equal(3, result1.TotalCount); // 排除了已删除和时间范围外的

            // Act 2 - 查询包含"感冒"的诊断
            var query2 = new ConsultationPagedQueryDto
            {
                DiagnosisKeyword = "感冒",
                CurrentPage = 1,
                PageSize = 10
            };
            var result2 = await _consultationService.GetPagedAsync(query2);

            // Assert 2
            Assert.Equal(2, result2.TotalCount); // 风寒感冒和风热感冒

            // Act 3 - 统计医生看诊数量
            var count = await _consultationService.GetDoctorConsultationCountAsync(
                doctorId, 
                DateTime.Today.AddDays(-30), 
                DateTime.Today);

            // Assert 3
            Assert.Equal(4, count); // 不包括已删除的
        }

        #endregion

        #region 数据关联集成测试

        [Fact]
        public async Task ConsultationWithRelations_LoadsAllAssociations()
        {
            // Arrange - 创建完整的关联数据
            var patient = new PatientModel
            {
                Id = Guid.NewGuid(),
                Name = "赵六",
                Gender = Gender.Female,
                BirthDate = DateTime.Now.AddYears(-35),
                Phone = "13700137000",
                CreateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            var doctor = _userBuilder
                .AsValidUser()
                .WithRole(UserRole.Doctor)
                .WithRealName("陈医生")
                .Build();

            var medicalCase = new MedicalCaseModel
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Status = MedicalCaseStatus.Created,
                CreateTime = DateTime.Now,
                IsActive = true
            };

            await _context.Patients.AddAsync(patient);
            await _context.Users.AddAsync(doctor);
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // 创建看诊
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCase.Id,
                PatientId = patient.Id,
                UserId = doctor.Id
            };
            var consultation = await _consultationService.StartConsultationAsync(startDto);

            // Act - 获取详情，验证关联加载
            var detail = await _consultationService.GetByIdAsync(consultation.Id);

            // Assert
            Assert.NotNull(detail);
            Assert.Equal("赵六", detail.PatientName);
            Assert.Equal("陈医生", detail.DoctorName);
            Assert.Equal(medicalCase.Id, detail.MedicalCaseId);
        }

        #endregion

        #region 性能测试

        [Fact]
        public async Task LargeScaleOperation_1000Consultations_PerformsEfficiently()
        {
            // Arrange - 创建1000条看诊记录
            var consultations = new List<ConsultationModel>();
            var patientIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
            var doctorIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

            for (int i = 0; i < 1000; i++)
            {
                var consultation = _consultationBuilder
                    .AsValidConsultation()
                    .WithPatientId(patientIds[i % 100])
                    .WithUserId(doctorIds[i % 10])
                    .WithConsultationTime(DateTime.Now.AddDays(-i % 365))
                    .WithRandomTCMDiagnosis()
                    .Build();
                consultations.Add(consultation);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();
            stopwatch.Stop();

            // Assert - 批量插入性能
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"批量插入1000条记录耗时过长: {stopwatch.ElapsedMilliseconds}ms");

            // Act - 分页查询性能
            stopwatch.Restart();
            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 50
            };
            var result = await _consultationService.GetPagedAsync(query);
            stopwatch.Stop();

            // Assert - 查询性能
            Assert.Equal(1000, result.TotalCount);
            Assert.Equal(50, result.Data.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"分页查询耗时过长: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 中医特色集成测试

        [Fact]
        public async Task TCMDiagnosticPattern_CompleteWorkflow_Success()
        {
            // Arrange - 准备中医诊疗场景
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var medicalCaseId = Guid.NewGuid();

            var patient = new PatientModel
            {
                Id = patientId,
                Name = "孙七",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-50),
                Phone = "13600136000",
                CreateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            var medicalCase = new MedicalCaseModel
            {
                Id = medicalCaseId,
                PatientId = patientId,
                Status = MedicalCaseStatus.Created,
                CreateTime = DateTime.Now,
                IsActive = true
            };

            await _context.Patients.AddAsync(patient);
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act - 创建风寒感冒的完整诊疗
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCaseId,
                PatientId = patientId,
                UserId = doctorId
            };
            var consultation = await _consultationService.StartConsultationAsync(startDto);

            // 四诊合参
            var tcmExamDto = new ConsultationUpdateDto
            {
                Inspection = "面色苍白，精神尚可，鼻塞流清涕",
                AuscultationOlfaction = "咳嗽声重浊，痰白稀薄，无特殊气味",
                Inquiry = "恶寒重发热轻，无汗，头身疼痛，口不渴，二便调",
                Palpation = "脉浮紧",
                TongueInspection = "舌淡红，苔薄白",
                PulseCondition = "脉浮紧"
            };
            await _consultationService.UpdateConsultationAsync(consultation.Id, tcmExamDto);

            // 辨证论治
            var diagnosisDto = new ConsultationUpdateDto
            {
                TCMDiagnosis = "风寒感冒，表实证",
                Diagnosis = "上呼吸道感染（风寒型）",
                TreatmentPrinciple = "疏风散寒，解表发汗",
                Medical