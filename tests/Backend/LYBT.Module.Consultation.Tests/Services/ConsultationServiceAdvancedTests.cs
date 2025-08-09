using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.Consultation;
using LYBT.Models.MedicalCase;
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
    /// ConsultationService高级场景测试 - UltraThink设计
    /// 职责单一：专注于复杂业务场景和并发测试
    /// 代码干净：清晰的测试组织，AAA模式
    /// 性能出色：并发测试验证系统稳定性
    /// </summary>
    public class ConsultationServiceAdvancedTests : IDisposable
    {
        private readonly ConsultationService _consultationService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly ConsultationTestDataBuilder _consultationBuilder;

        public ConsultationServiceAdvancedTests()
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

        #region 并发场景测试

        [Fact]
        public async Task ConcurrentUpdates_DifferentConsultations_AllSucceed()
        {
            // Arrange
            var consultations = new List<ConsultationModel>();
            for (int i = 0; i < 10; i++)
            {
                var consultation = _consultationBuilder.AsNewConsultation().Build();
                consultations.Add(consultation);
            }
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            // Act
            var tasks = consultations.Select(c => Task.Run(async () =>
            {
                var updateDto = new ConsultationUpdateDto
                {
                    Diagnosis = $"并发更新诊断 - {Guid.NewGuid()}"
                };
                await _consultationService.UpdateConsultationAsync(c.Id, updateDto);
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            foreach (var consultation in consultations)
            {
                var updated = await _context.Consultations.FindAsync(consultation.Id);
                Assert.Contains("并发更新诊断", updated!.Diagnosis);
            }
        }

        [Fact]
        public async Task ConcurrentReads_SameConsultation_NoDataCorruption()
        {
            // Arrange
            var consultation = _consultationBuilder.AsCompleteTCMConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var tasks = new List<Task<ConsultationDetailDto?>>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(async () => 
                    await _consultationService.GetByIdAsync(consultation.Id)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r =>
            {
                Assert.NotNull(r);
                Assert.Equal(consultation.Id, r!.Id);
                Assert.Equal(consultation.Diagnosis, r.Diagnosis);
            });
        }

        [Fact]
        public async Task ConcurrentStartConsultations_DifferentMedicalCases_AllSucceed()
        {
            // Arrange
            var medicalCases = new List<MedicalCaseModel>();
            for (int i = 0; i < 5; i++)
            {
                var medicalCase = new MedicalCaseModel
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    Status = MedicalCaseStatus.Created,
                    CreateTime = DateTime.Now,
                    IsActive = true
                };
                medicalCases.Add(medicalCase);
            }
            await _context.MedicalCases.AddRangeAsync(medicalCases);
            await _context.SaveChangesAsync();

            // Act
            var tasks = medicalCases.Select(mc => Task.Run(async () =>
            {
                var startDto = new ConsultationStartDto
                {
                    MedicalCaseId = mc.Id,
                    PatientId = mc.PatientId,
                    UserId = Guid.NewGuid()
                };
                return await _consultationService.StartConsultationAsync(startDto);
            })).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(5, results.Length);
            Assert.All(results, r => Assert.NotNull(r));
            var consultationCount = await _context.Consultations.CountAsync();
            Assert.Equal(5, consultationCount);
        }

        #endregion

        #region 复杂查询测试

        [Fact]
        public async Task GetPagedAsync_WithAllFilters_ReturnsCorrectResults()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var today = DateTime.Today;

            var consultations = new[]
            {
                _consultationBuilder.AsValidConsultation()
                    .WithPatientId(patientId)
                    .WithUserId(userId)
                    .WithConsultationTime(today)
                    .WithDiagnosis("头痛发热")
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithPatientId(patientId)
                    .WithUserId(Guid.NewGuid())
                    .WithConsultationTime(today.AddDays(-1))
                    .WithDiagnosis("腹痛")
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithPatientId(Guid.NewGuid())
                    .WithUserId(userId)
                    .WithConsultationTime(today.AddDays(-2))
                    .WithDiagnosis("咳嗽")
                    .Build()
            };
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PatientId = patientId,
                UserId = userId,
                StartDate = today.AddDays(-1),
                EndDate = today.AddDays(1),
                DiagnosisKeyword = "头痛",
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("头痛发热", result.Data[0].Diagnosis);
        }

        [Fact]
        public async Task GetPagedAsync_LargeDateSet_PerformsEfficiently()
        {
            // Arrange - 创建1000条记录
            var consultations = new List<ConsultationModel>();
            for (int i = 0; i < 1000; i++)
            {
                consultations.Add(_consultationBuilder
                    .AsValidConsultation()
                    .WithConsultationTime(DateTime.Now.AddDays(-i))
                    .Build());
            }
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 20
            };

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _consultationService.GetPagedAsync(query);
            stopwatch.Stop();

            // Assert
            Assert.Equal(1000, result.TotalCount);
            Assert.Equal(20, result.Data.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"查询耗时过长: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 状态转换测试

        [Fact]
        public async Task ConsultationLifecycle_CompleteFlow_StateTransitionsCorrect()
        {
            // Arrange - 创建医疗案例
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseModel
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                Status = MedicalCaseStatus.Created,
                CreateTime = DateTime.Now,
                IsActive = true
            };
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act & Assert - 开始看诊
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCaseId,
                PatientId = medicalCase.PatientId,
                UserId = Guid.NewGuid()
            };
            var consultation = await _consultationService.StartConsultationAsync(startDto);
            Assert.NotNull(consultation);

            var updatedCase1 = await _context.MedicalCases.FindAsync(medicalCaseId);
            Assert.Equal(MedicalCaseStatus.InConsultation, updatedCase1!.Status);

            // Act & Assert - 更新诊断
            var updateDto = new ConsultationUpdateDto
            {
                TCMDiagnosis = "风寒感冒",
                TreatmentPrinciple = "疏风散寒"
            };
            var updated = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);
            Assert.Equal("风寒感冒", updated.TCMDiagnosis);

            // Act & Assert - 完成看诊
            var completeDto = new ConsultationCompleteDto
            {
                Summary = "治疗完成",
                Diagnosis = "确诊风寒感冒",
                TCMDiagnosis = "风寒感冒",
                TreatmentPrinciple = "疏风散寒，解表发汗",
                MedicalAdvice = "多饮温水，注意保暖"
            };
            var completed = await _consultationService.CompleteConsultationAsync(consultation.Id, completeDto);
            Assert.True(completed);

            var updatedCase2 = await _context.MedicalCases.FindAsync(medicalCaseId);
            Assert.Equal(MedicalCaseStatus.Completed, updatedCase2!.Status);
        }

        [Fact]
        public async Task UpdateStatus_MultipleStateChanges_MaintainsHistory()
        {
            // Arrange
            var consultation = _consultationBuilder.AsValidConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act - 多次状态更新
            await _consultationService.UpdateStatusAsync(consultation.Id, (int)CommonStatus.Disabled, "临时禁用");
            await _consultationService.UpdateStatusAsync(consultation.Id, (int)CommonStatus.Enabled, "重新启用");
            await _consultationService.UpdateStatusAsync(consultation.Id, (int)CommonStatus.Disabled, "永久禁用");

            // Assert
            var final = await _context.Consultations.FindAsync(consultation.Id);
            Assert.Equal(CommonStatus.Disabled, final!.Status);
            Assert.Contains("临时禁用", final.Remark);
            Assert.Contains("重新启用", final.Remark);
            Assert.Contains("永久禁用", final.Remark);
        }

        #endregion

        #region 数据完整性测试

        [Fact]
        public async Task UpdateConsultation_PreservesUnmodifiedFields()
        {
            // Arrange
            var consultation = _consultationBuilder
                .AsCompleteTCMConsultation()
                .WithInspection("原始望诊")
                .WithAuscultationOlfaction("原始闻诊")
                .WithInquiry("原始问诊")
                .WithPalpation("原始切诊")
                .Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act - 只更新部分字段
            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "更新后的望诊"
                // 其他字段不更新
            };
            await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            var updated = await _context.Consultations.FindAsync(consultation.Id);
            Assert.Equal("更新后的望诊", updated!.Inspection);
            Assert.Equal("原始闻诊", updated.AuscultationOlfaction);
            Assert.Equal("原始问诊", updated.Inquiry);
            Assert.Equal("原始切诊", updated.Palpation);
        }

        [Fact]
        public async Task DeleteAsync_SoftDelete_DataPreserved()
        {
            // Arrange
            var consultation = _consultationBuilder.AsCompleteTCMConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var originalData = new
            {
                consultation.Diagnosis,
                consultation.TCMDiagnosis,
                consultation.TreatmentPrinciple
            };

            // Act
            var deleted = await _consultationService.DeleteAsync(consultation.Id);

            // Assert
            Assert.True(deleted);
            var deletedConsultation = await _context.Consultations.FindAsync(consultation.Id);
            Assert.NotNull(deletedConsultation);
            Assert.Equal(CommonStatus.Disabled, deletedConsultation.Status);
            Assert.Equal(originalData.Diagnosis, deletedConsultation.Diagnosis);
            Assert.Equal(originalData.TCMDiagnosis, deletedConsultation.TCMDiagnosis);
            Assert.Equal(originalData.TreatmentPrinciple, deletedConsultation.TreatmentPrinciple);
        }

        #endregion

        #region 复杂业务场景测试

        [Fact]
        public async Task GetTodayConsultationsByDoctor_MultipleConsultations_OrderedCorrectly()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var today = DateTime.Today;

            var consultations = new[]
            {
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(today.AddHours(9))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(today.AddHours(14))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(today.AddHours(11))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(Guid.NewGuid())
                    .WithConsultationTime(today.AddHours(10))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(today.AddDays(-1))
                    .Build()
            };
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _consultationService.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.True(result[0].ConsultationTime < result[1].ConsultationTime);
            Assert.True(result[1].ConsultationTime < result[2].ConsultationTime);
        }

        [Fact]
        public async Task GetPatientHistory_MultipleVisits_ReturnsInDescOrder()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var consultations = new List<ConsultationModel>();
            
            for (int i = 0; i < 10; i++)
            {
                consultations.Add(_consultationBuilder
                    .AsValidConsultation()
                    .WithPatientId(patientId)
                    .WithConsultationTime(DateTime.Now.AddDays(-i))
                    .Build());
            }
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            // Act
            var history = await _consultationService.GetPatientHistoryAsync(patientId);

            // Assert
            Assert.Equal(10, history.Count);
            for (int i = 0; i < history.Count - 1; i++)
            {
                Assert.True(history[i].ConsultationTime > history[i + 1].ConsultationTime);
            }
        }

        [Fact]
        public async Task GetDoctorConsultationCount_WithDateRange_CalculatesCorrectly()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            var consultations = new[]
            {
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(startDate.AddDays(5))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(startDate.AddDays(15))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(startDate.AddDays(25))
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(startDate.AddDays(-5)) // 范围外
                    .Build(),
                _consultationBuilder.AsValidConsultation()
                    .WithUserId(doctorId)
                    .WithConsultationTime(endDate.AddDays(5)) // 范围外
                    .Build()
            };
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            // Act
            var count = await _consultationService.GetDoctorConsultationCountAsync(
                doctorId, startDate, endDate);

            // Assert
            Assert.Equal(3, count);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task GetPagedAsync_PageSizeExceedsTotal_ReturnsAllAvailable()
        {
            // Arrange
            await CreateConsultationsInContext(5);
            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 100
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(5, result.Data.Count);
        }

        [Fact]
        public async Task GetPagedAsync_PageBeyondLastPage_ReturnsEmpty()
        {
            // Arrange
            await CreateConsultationsInContext(10);
            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 5,
                PageSize = 5
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(10, result.TotalCount);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task UpdateConsultation_EmptyStringValues_UpdatesCorrectly()
        {
            // Arrange
            var consultation = _consultationBuilder
                .AsCompleteTCMConsultation()
                .WithDiagnosis("原始诊断")
                .Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "",
                TCMDiagnosis = "",
                MedicalAdvice = ""
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal("", result.Diagnosis);
            Assert.Equal("", result.TCMDiagnosis);
            Assert.Equal("", result.MedicalAdvice);
        }

        #endregion

        #region Unicode和特殊字符测试

        [Fact]
        public async Task UpdateConsultation_WithUnicodeAndEmoji_HandlesCorrectly()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "中医诊断：風寒感冒 😷",
                TCMDiagnosis = "經絡不通 🩺",
                MedicalAdvice = "多喝水💧 注意休息🛌"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal("中医诊断：風寒感冒 😷", result.Diagnosis);
            Assert.Equal("經絡不通 🩺", result.TCMDiagnosis);
            Assert.Equal("多喝水💧 注意休息🛌", result.MedicalAdvice);
        }

        #endregion

        #region Helper Methods

        private async Task<List<ConsultationModel>> CreateConsultationsInContext(int count)
        {
            var consultations = new List<ConsultationModel>();
            for (int i = 0; i < count; i++)
            {
                consultations.Add(_consultationBuilder.AsValidConsultation().Build());
            }
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();
            return consultations;
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _mockFactory?.ClearCache();
        }
    }
}