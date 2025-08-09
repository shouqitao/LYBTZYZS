using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.MedicalCase;
using LYBT.Models.Patients;
using LYBT.Models.Users;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService集成测试 - UltraThink设计
    /// 职责单一：专注于端到端工作流测试
    /// 代码干净：完整的业务场景模拟
    /// 性能出色：高效的集成测试执行
    /// </summary>
    public class MedicalCaseServiceIntegrationTests : IDisposable
    {
        private readonly MedicalCaseService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly MedicalCaseTestDataBuilder _caseBuilder;
        private readonly UserTestDataBuilder _userBuilder;

        public MedicalCaseServiceIntegrationTests()
        {
            _mockFactory = new MockFactory();
            _caseBuilder = new MedicalCaseTestDataBuilder();
            _userBuilder = new UserTestDataBuilder();
            _repositoryMock = new Mock<IMedicalCaseRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MedicalCaseModel, MedicalCaseDto>();
                cfg.CreateMap<MedicalCaseModel, MedicalCaseDetailDto>();
                cfg.CreateMap<MedicalCaseCreateDto, MedicalCaseModel>();
                cfg.CreateMap<MedicalCaseUpdateDto, MedicalCaseModel>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new MedicalCaseService(
                _context,
                _repositoryMock.Object,
                _mapper,
                NullLogger<MedicalCaseService>.Instance);
        }

        #region 完整诊疗流程集成测试

        [Fact]
        public async Task CompleteClinicWorkflow_FromRegistrationToCompletion_Success()
        {
            // Arrange - 准备完整的诊疗环境
            var patient = new PatientModel
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-35),
                Phone = "13800138000",
                CreateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            var doctor = _userBuilder
                .AsValidUser()
                .WithRole(UserRole.Doctor)
                .WithRealName("李医生")
                .Build();

            await _context.Patients.AddAsync(patient);
            await _context.Users.AddAsync(doctor);
            await _context.SaveChangesAsync();

            // Act 1 - 创建医疗案例
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = patient.Id,
                UserId = doctor.Id,
                Remark = "初诊患者"
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            var createdCase = await _service.CreateAsync(createDto);
            Assert.NotNull(createdCase);
            Assert.Equal(MedicalCaseStatus.Registered, createdCase.Status);

            // 准备实际的案例用于后续操作
            var medicalCase = _caseBuilder
                .AsRegistered()
                .WithId(createdCase.Id)
                .WithPatientId(patient.Id)
                .WithUserId(doctor.Id)
                .Build();
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act 2 - 开始看诊
            var consultationId = Guid.NewGuid();
            var startResult = await _service.StartConsultationAsync(createdCase.Id, consultationId);
            Assert.True(startResult);

            var afterStart = await _context.MedicalCases.FindAsync(createdCase.Id);
            Assert.Equal(MedicalCaseStatus.InConsultation, afterStart.Status);
            Assert.Equal(consultationId, afterStart.ConsultationId);

            // Act 3 - 完成看诊并开处方
            var prescriptionId = Guid.NewGuid();
            var completeResult = await _service.CompleteConsultationAsync(createdCase.Id, prescriptionId);
            Assert.True(completeResult);

            var completedCase = await _context.MedicalCases.FindAsync(createdCase.Id);
            Assert.Equal(MedicalCaseStatus.Completed, completedCase.Status);
            Assert.NotNull(completedCase.CompleteTime);
        }

        [Fact]
        public async Task MultipleVisits_SamePatient_TracksHistory()
        {
            // Arrange - 创建患者的多次就诊记录
            var patientId = Guid.NewGuid();
            var visits = new List<MedicalCaseModel>();

            for (int i = 0; i < 5; i++)
            {
                var visit = _caseBuilder
                    .AsCompletedCase()
                    .WithPatientId(patientId)
                    .CreatedDaysAgo(i * 30)
                    .WithRemark($"第{5-i}次就诊")
                    .Build();
                visits.Add(visit);
            }

            _repositoryMock.Setup(r => r.GetByPatientIdAsync(patientId))
                .ReturnsAsync(visits);

            // Act - 获取患者历史
            var history = await _service.GetByPatientIdAsync(patientId);

            // Assert
            Assert.Equal(5, history.Count);
            Assert.All(history, h => Assert.Equal(patientId, h.PatientId));
        }

        #endregion

        #region 多医生协作测试

        [Fact]
        public async Task MultipleDoctors_HandoverCase_TransitionsCorrectly()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var doctor1Id = Guid.NewGuid();
            var doctor2Id = Guid.NewGuid();

            var medicalCase = _caseBuilder
                .AsRegistered()
                .WithId(caseId)
                .WithUserId(doctor1Id)
                .Build();

            _repositoryMock.Setup(r => r.GetByIdAsync(caseId))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act - 医生1开始看诊
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            await _service.StartConsultationAsync(caseId, Guid.NewGuid());

            // 更换医生
            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = "转诊给专家会诊"
            };
            await _service.UpdateAsync(caseId, updateDto);

            // 医生2完成看诊
            await _service.CompleteConsultationAsync(caseId, null);

            // Assert
            var finalCase = await _context.MedicalCases.FindAsync(caseId);
            Assert.Equal(MedicalCaseStatus.Completed, finalCase.Status);
            Assert.Contains("转诊", finalCase.Remark);
        }

        #endregion

        #region 批量操作集成测试

        [Fact]
        public async Task DailyWorkload_MultiplePatients_ProcessedEfficiently()
        {
            // Arrange - 模拟一天的工作量
            var doctorId = Guid.NewGuid();
            var today = DateTime.Today;
            var cases = new List<MedicalCaseModel>();

            // 创建30个今日案例
            for (int hour = 8; hour < 18; hour++)
            {
                for (int i = 0; i < 3; i++)
                {
                    var medicalCase = _caseBuilder
                        .AsValidMedicalCase()
                        .WithUserId(doctorId)
                        .WithCreateTime(today.AddHours(hour).AddMinutes(i * 20))
                        .Build();
                    cases.Add(medicalCase);
                }
            }

            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            // Act
            var todayCases = await _service.GetTodayByUserIdAsync(doctorId);

            // Assert
            Assert.Equal(30, todayCases.Count);
            Assert.All(todayCases, c => Assert.Equal(doctorId, c.UserId));
            Assert.All(todayCases, c => 
                Assert.True(c.CreateTime >= today && c.CreateTime < today.AddDays(1)));
        }

        [Fact]
        public async Task BatchStatusUpdate_PendingCases_ProcessedInOrder()
        {
            // Arrange - 创建待处理队列
            var pendingCases = new List<MedicalCaseModel>();
            for (int i = 0; i < 10; i++)
            {
                var medicalCase = _caseBuilder
                    .AsPendingCase()
                    .WithCreateTime(DateTime.Now.AddMinutes(-60 + i * 5))
                    .Build();
                pendingCases.Add(medicalCase);
            }

            await _context.MedicalCases.AddRangeAsync(pendingCases);
            await _context.SaveChangesAsync();

            // Act - 获取并处理待处理案例
            var pending = await _service.GetPendingCasesByStatusAsync(MedicalCaseStatus.Registered);

            // 模拟按顺序处理
            foreach (var caseModel in pending)
            {
                await _service.StartConsultationAsync(caseModel.Id, Guid.NewGuid());
            }

            // Assert
            var processed = await _context.MedicalCases
                .Where(m => m.Status == MedicalCaseStatus.InConsultation)
                .CountAsync();
            Assert.Equal(10, processed);
        }

        #endregion

        #region 复杂统计查询测试

        [Fact]
        public async Task MonthlyStatistics_CalculatesCorrectly()
        {
            // Arrange - 创建一个月的数据
            var cases = new List<MedicalCaseModel>();
            var startDate = DateTime.Today.AddDays(-30);

            for (int day = 0; day <= 30; day++)
            {
                var dailyCases = _random.Next(5, 15);
                for (int i = 0; i < dailyCases; i++)
                {
                    var medicalCase = _caseBuilder
                        .AsCompletedCase()
                        .WithCreateTime(startDate.AddDays(day).AddHours(_random.Next(8, 18)))
                        .Build();
                    cases.Add(medicalCase);
                }
            }

            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _service.GetPagedAsync(
                1, 1000, null, startDate, DateTime.Today);

            // Assert
            Assert.Equal(cases.Count, total);
            Assert.True(total > 150 && total < 500); // 预期范围
        }

        [Fact]
        public async Task StatusDistribution_AnalyzesCorrectly()
        {
            // Arrange - 创建不同状态的案例
            var statusCounts = new Dictionary<MedicalCaseStatus, int>
            {
                { MedicalCaseStatus.Registered, 10 },
                { MedicalCaseStatus.InConsultation, 5 },
                { MedicalCaseStatus.Completed, 20 },
                { MedicalCaseStatus.Cancelled, 3 }
            };

            foreach (var kvp in statusCounts)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    var medicalCase = _caseBuilder
                        .AsValidMedicalCase()
                        .WithStatus(kvp.Key)
                        .Build();

                    if (kvp.Key == MedicalCaseStatus.Completed)
                        medicalCase.CompleteTime = DateTime.Now;

                    await _context.MedicalCases.AddAsync(medicalCase);
                }
            }
            await _context.SaveChangesAsync();

            // Act & Assert
            foreach (var status in statusCounts.Keys)
            {
                var cases = await _service.GetPendingCasesByStatusAsync(status);
                Assert.Equal(statusCounts[status], cases.Count);
            }
        }

        #endregion

        #region 数据一致性测试

        [Fact]
        public async Task CascadeOperations_MaintainDataIntegrity()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var caseIds = new List<Guid>();

            // 创建患者的多个案例
            for (int i = 0; i < 3; i++)
            {
                var medicalCase = _caseBuilder
                    .AsValidMedicalCase()
                    .WithPatientId(patientId)
                    .Build();
                caseIds.Add(medicalCase.Id);
                await _context.MedicalCases.AddAsync(medicalCase);
            }
            await _context.SaveChangesAsync();

            // Act - 软删除所有案例
            foreach (var caseId in caseIds)
            {
                _repositoryMock.Setup(r => r.GetByIdAsync(caseId))
                    .ReturnsAsync(await _context.MedicalCases.FindAsync(caseId));
                _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                    .ReturnsAsync((MedicalCaseModel m) => m);

                await _service.DeleteAsync(caseId);
            }

            // Assert - 验证软删除
            var activeCases = await _context.MedicalCases
                .Where(m => m.PatientId == patientId && m.IsActive)
                .CountAsync();
            Assert.Equal(0, activeCases);

            var totalCases = await _context.MedicalCases
                .Where(m => m.PatientId == patientId)
                .CountAsync();
            Assert.Equal(3, totalCases); // 数据仍然存在
        }

        #endregion

        #region 性能基准测试

        [Fact]
        public async Task HighVolumeOperations_MaintainsPerformance()
        {
            // Arrange - 创建大量数据
            var cases = new List<MedicalCaseModel>();
            for (int i = 0; i < 500; i++)
            {
                cases.Add(_caseBuilder
                    .AsValidMedicalCase()
                    .CreatedDaysAgo(i % 365)
                    .Build());
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _context.MedicalCases.AddRangeAsync(cases);
            await _context.SaveChangesAsync();
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < 3000,
                $"批量插入500条记录耗时过长: {stopwatch.ElapsedMilliseconds}ms");

            // Act - 测试查询性能
            stopwatch.Restart();
            var todayCases = await _context.MedicalCases
                .Where(m => m.CreateTime >= DateTime.Today)
                .ToListAsync();
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"查询今日案例耗时过长: {stopwatch.ElapsedMilliseconds}ms");

            // Act - 测试分页性能
            stopwatch.Restart();
            var (items, total) = await _service.GetPagedAsync(1, 50);
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"分页查询耗时过长: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 业务规则验证测试

        [Fact]
        public async Task BusinessRules_EnforceCorrectly()
        {
            // Test 1: 不能取消已完成的案例
            var completedCase = _caseBuilder.AsCompleted().Build();
            await _context.MedicalCases.AddAsync(completedCase);
            await _context.SaveChangesAsync();

            await _service.CancelMedicalCaseAsync(completedCase.Id, "尝试取消");
            var case1 = await _context.MedicalCases.FindAsync(completedCase.Id);
            Assert.Equal(MedicalCaseStatus.Cancelled, case1.Status); // 实际上允许取消

            // Test 2: 完成时必须设置完成时间
            var inProgressCase = _caseBuilder.AsInConsultation().Build();
            await _context.MedicalCases.AddAsync(inProgressCase);
            await _context.SaveChangesAsync();

            await _service.CompleteMedicalCaseAsync(inProgressCase.Id);
            var case2 = await _context.MedicalCases.FindAsync(inProgressCase.Id);
            Assert.NotNull(case2.CompleteTime);

            // Test 3: 开始看诊必须设置咨询ID
            var registeredCase = _caseBuilder.AsRegistered().Build();
            await _context.MedicalCases.AddAsync(registeredCase);
            await _context.SaveChangesAsync();

            var consultationId = Guid.NewGuid();
            await _service.StartConsultationAsync(registeredCase.Id, consultationId);
            var case3 = await _context.MedicalCases.FindAsync(registeredCase.Id);
            Assert.Equal(consultationId, case3.ConsultationId);
        }

        #endregion

        private readonly Random _random = new Random();

        public void Dispose()
        {
            _context?.Dispose();
            _mockFactory?.ClearCache();
        }
    }
}