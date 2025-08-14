using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// PrescriptionService集成测试 - UltraThink设计
    /// 职责单一：专注于端到端工作流测试
    /// 代码干净：完整的业务场景模拟
    /// 性能出色：高效的集成测试执行
    /// </summary>
    public class PrescriptionServiceIntegrationTests : IDisposable
    {
        private readonly PrescriptionService _service;
        private readonly Mock<IPrescriptionRepository> _repositoryMock;
        private readonly Mock<IUnifiedLogService> _logServiceMock;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly PrescriptionTestDataBuilder _builder;
        private readonly List<PrescriptionModel> _inMemoryDatabase;

        public PrescriptionServiceIntegrationTests()
        {
            _mockFactory = new MockFactory();
            _builder = new PrescriptionTestDataBuilder();
            _repositoryMock = new Mock<IPrescriptionRepository>();
            _logServiceMock = new Mock<IUnifiedLogService>();
            _inMemoryDatabase = new List<PrescriptionModel>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PrescriptionModel, PrescriptionDto>();
                cfg.CreateMap<PrescriptionModel, PrescriptionDetailDto>();
                cfg.CreateMap<PrescriptionCreateDto, PrescriptionModel>();
                cfg.CreateMap<PrescriptionEditDto, PrescriptionModel>();
                cfg.CreateMap<PrescriptionItemModel, PrescriptionItemDto>();
                cfg.CreateMap<PrescriptionItemCreateDto, PrescriptionItemModel>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            // 设置内存数据库模拟
            SetupInMemoryRepository();

            _service = new PrescriptionService(
                _repositoryMock.Object,
                _logServiceMock.Object,
                _mapper);
        }

        private void SetupInMemoryRepository()
        {
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(() => _inMemoryDatabase.ToList());

            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => _inMemoryDatabase.FirstOrDefault(p => p.Id == id));

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync((PrescriptionModel model) =>
                {
                    _inMemoryDatabase.Add(model);
                    return true;
                });

            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync((PrescriptionModel model) =>
                {
                    var existing = _inMemoryDatabase.FirstOrDefault(p => p.Id == model.Id);
                    if (existing != null)
                    {
                        _inMemoryDatabase.Remove(existing);
                        _inMemoryDatabase.Add(model);
                        return true;
                    }
                    return false;
                });

            _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var existing = _inMemoryDatabase.FirstOrDefault(p => p.Id == id);
                    if (existing != null)
                    {
                        _inMemoryDatabase.Remove(existing);
                        return true;
                    }
                    return false;
                });

            _repositoryMock.Setup(r => r.CancelAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var existing = _inMemoryDatabase.FirstOrDefault(p => p.Id == id);
                    if (existing != null)
                    {
                        existing.Status = PrescriptionStatus.Draft; // 模拟取消
                        return true;
                    }
                    return false;
                });
        }

        #region 完整处方流程测试

        [Fact]
        public async Task CompletePrescriptionWorkflow_FromCreateToDispense_Success()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            // Step 1: 创建处方
            var createDto = new PrescriptionCreateDto
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Diagnosis = "风寒感冒，恶寒发热",
                DosageCount = 3,
                Advice = "温服，服后覆被取微汗",
                Items = new List<PrescriptionItemCreateDto>
                {
                    new PrescriptionItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "麻黄",
                        Quantity = 9,
                        Unit = "g",
                        UnitPrice = 2.5m
                    },
                    new PrescriptionItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "桂枝",
                        Quantity = 6,
                        Unit = "g",
                        UnitPrice = 3.0m
                    }
                }
            };

            var created = await _service.CreateAsync(createDto, operatorId, "张医生");
            Assert.NotNull(created);
            Assert.Equal(PrescriptionStatus.Draft, created.Status);

            // Step 2: 快速保存（修改部分内容）
            var quickDto = new QuickPrescriptionDto
            {
                Diagnosis = "风寒感冒，恶寒发热，无汗",
                Advice = "温服，服后覆被取微汗，避风寒"
            };

            var quickSaved = await _service.QuickSaveAsync(created.Id, quickDto, operatorId, "张医生");
            Assert.True(quickSaved);

            // Step 3: 提交处方
            var submitted = await _service.SubmitPrescriptionAsync(created.Id, operatorId, "张医生");
            Assert.True(submitted);

            // Step 4: 验证最终状态
            var final = await _service.GetByIdAsync(created.Id.ToString());
            Assert.NotNull(final);
            Assert.Equal("风寒感冒，恶寒发热，无汗", final.Diagnosis);
            Assert.Equal("温服，服后覆被取微汗，避风寒", final.Advice);
            Assert.Equal(2, final.Items.Count);
        }

        [Fact]
        public async Task PatientPrescriptionHistory_MultipleVisits_TracksCorrectly()
        {
            // Arrange - 创建患者的多次处方记录
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            for (int i = 0; i < 5; i++)
            {
                var prescription = _builder
                    .AsValidPrescription()
                    .WithPatientId(patientId)
                    .WithUserId(doctorId)
                    .CreatedDaysAgo(i * 7)
                    .WithDiagnosis($"第{5-i}次就诊")
                    .Build();
                _inMemoryDatabase.Add(prescription);
            }

            // Act - 获取患者历史
            var history = await _service.GetPatientHistoryAsync(patientId, 10);

            // Assert
            Assert.Equal(5, history.Count);
            Assert.All(history, h => Assert.Equal(patientId, h.PatientId));
            // 验证按时间降序排列
            Assert.Equal("第5次就诊", history[0].Diagnosis);
            Assert.Equal("第1次就诊", history[4].Diagnosis);
        }

        #endregion

        #region 处方复制和模板测试

        [Fact]
        public async Task CopyLastPrescription_CompleteFlow_Success()
        {
            // Arrange - 创建历史处方
            var patientId = Guid.NewGuid();
            var originalDoctorId = Guid.NewGuid();
            var newDoctorId = Guid.NewGuid();

            var originalPrescription = _builder
                .AsClassicPrescription()
                .WithPatientId(patientId)
                .WithUserId(originalDoctorId)
                .CreatedDaysAgo(7)
                .Build();
            _inMemoryDatabase.Add(originalPrescription);

            // Act - 复制处方
            var copied = await _service.CopyLastPrescriptionAsync(
                patientId, newDoctorId, Guid.NewGuid(), "李医生");

            // Assert
            Assert.NotNull(copied);
            Assert.Equal(patientId, copied.PatientId);
            Assert.Equal(newDoctorId, copied.DoctorId);
            Assert.Equal(originalPrescription.Diagnosis, copied.Diagnosis);
            Assert.Equal(originalPrescription.DosageCount, copied.DosageCount);
            Assert.Equal(originalPrescription.Items.Count, copied.Items.Count);

            // 验证是新创建的处方
            Assert.NotEqual(originalPrescription.Id, copied.Id);
            Assert.Equal(2, _inMemoryDatabase.Count);
        }

        #endregion

        #region 医生工作流集成测试

        [Fact]
        public async Task DoctorDailyWorkflow_MultiplePrescriptions_HandlesCorrectly()
        {
            // Arrange - 模拟医生一天的工作
            var doctorId = Guid.NewGuid();
            var today = DateTime.Today;
            var operatorId = Guid.NewGuid();

            // 上午的处方
            for (int i = 0; i < 5; i++)
            {
                var prescription = _builder
                    .AsValidPrescription()
                    .WithUserId(doctorId)
                    .WithCreateTime(today.AddHours(9).AddMinutes(i * 30))
                    .Build();
                _inMemoryDatabase.Add(prescription);
            }

            // 下午的处方
            for (int i = 0; i < 3; i++)
            {
                var prescription = _builder
                    .AsValidPrescription()
                    .WithUserId(doctorId)
                    .WithCreateTime(today.AddHours(14).AddMinutes(i * 30))
                    .Build();
                _inMemoryDatabase.Add(prescription);
            }

            // 其他医生的处方
            _inMemoryDatabase.Add(_builder
                .AsValidPrescription()
                .WithUserId(Guid.NewGuid())
                .CreatedToday()
                .Build());

            // 昨天的处方
            _inMemoryDatabase.Add(_builder
                .AsValidPrescription()
                .WithUserId(doctorId)
                .CreatedDaysAgo(1)
                .Build());

            // Act
            var todayPrescriptions = await _service.GetDoctorTodayPrescriptionsAsync(doctorId);

            // Assert
            Assert.Equal(8, todayPrescriptions.Count);
            Assert.All(todayPrescriptions, p => Assert.Equal(doctorId, p.UserId));
            Assert.All(todayPrescriptions, p => Assert.Equal(today, p.CreateTime.Date));
        }

        #endregion

        #region 统计和报表测试

        [Fact]
        public async Task GetStatistics_CompleteDataset_CalculatesCorrectly()
        {
            // Arrange - 创建多样化的数据集
            var doctorId = Guid.NewGuid();
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            // 草稿状态
            for (int i = 0; i < 3; i++)
            {
                _inMemoryDatabase.Add(_builder
                    .AsValidPrescription()
                    .WithUserId(doctorId)
                    .AsDraft()
                    .CreatedDaysAgo(i)
                    .Build());
            }

            // 完成状态
            for (int i = 0; i < 5; i++)
            {
                _inMemoryDatabase.Add(_builder
                    .AsCompletedPrescription()
                    .WithUserId(doctorId)
                    .CreatedDaysAgo(i + 5)
                    .Build());
            }

            // 配药状态
            for (int i = 0; i < 2; i++)
            {
                _inMemoryDatabase.Add(_builder
                    .AsDispensedPrescription()
                    .WithUserId(doctorId)
                    .CreatedDaysAgo(i + 10)
                    .Build());
            }

            // 其他医生的处方
            _inMemoryDatabase.Add(_builder
                .AsValidPrescription()
                .WithUserId(Guid.NewGuid())
                .Build());

            // 范围外的处方
            _inMemoryDatabase.Add(_builder
                .AsValidPrescription()
                .WithUserId(doctorId)
                .CreatedDaysAgo(35)
                .Build());

            // Act
            var stats = await _service.GetStatisticsAsync(doctorId, startDate, endDate);

            // Assert
            Assert.Equal(10, stats.TotalCount); // 3草稿 + 5完成 + 2配药
            Assert.Equal(3, stats.DraftCount);
            Assert.Equal(5, stats.CompletedCount);
        }

        #endregion

        #region 性能和批量操作测试

        [Fact]
        public async Task LargeScaleOperation_500Prescriptions_PerformsEfficiently()
        {
            // Arrange - 创建大量处方
            for (int i = 0; i < 500; i++)
            {
                var prescription = _builder
                    .AsValidPrescription()
                    .CreatedDaysAgo(i % 30)
                    .Build();
                _inMemoryDatabase.Add(prescription);
            }

            // Act - 分页查询
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var pagedResult = await _service.GetPagedAsync(new PaginationRequest
            {
                CurrentPage = 5,
                PageSize = 50
            });
            stopwatch.Stop();

            // Assert
            Assert.Equal(500, pagedResult.TotalCount);
            Assert.Equal(50, pagedResult.Items.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"查询耗时过长: {stopwatch.ElapsedMilliseconds}ms");

            // Act - 统计查询
            stopwatch.Restart();
            var stats = await _service.GetStatisticsAsync();
            stopwatch.Stop();

            Assert.Equal(500, stats.TotalCount);
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"统计耗时过长: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 数据一致性测试

        [Fact]
        public async Task ConcurrentOperations_DataConsistency_Maintained()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

            // Act - 并发创建处方
            var tasks = doctorIds.Select(doctorId => Task.Run(async () =>
            {
                var createDto = new PrescriptionCreateDto
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Diagnosis = $"医生{doctorId}的诊断",
                    DosageCount = 7,
                    Items = new List<PrescriptionItemCreateDto>
                    {
                        new PrescriptionItemCreateDto
                        {
                            HerbId = Guid.NewGuid(),
                            HerbName = "测试药材",
                            Quantity = 10,
                            Unit = "g",
                            UnitPrice = 5.0m
                        }
                    }
                };
                return await _service.CreateAsync(createDto, doctorId, "医生");
            })).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(5, results.Length);
            Assert.All(results, r => Assert.NotNull(r));
            Assert.Equal(5, _inMemoryDatabase.Count);
            
            // 验证每个处方的数据完整性
            foreach (var prescription in _inMemoryDatabase)
            {
                Assert.NotNull(prescription.Diagnosis);
                Assert.NotEmpty(prescription.Items);
                Assert.Equal(7, prescription.DosageCount);
            }
        }

        #endregion

        #region 复杂查询测试

        [Fact]
        public async Task ComplexSearch_MultipleFilters_ReturnsCorrectResults()
        {
            // Arrange - 创建多样化数据
            var targetId = Guid.NewGuid();
            var targetPatientId = Guid.NewGuid();
            var targetDoctorId = Guid.NewGuid();

            _inMemoryDatabase.AddRange(new[]
            {
                _builder.AsValidPrescription().WithId(targetId).WithPatientId(targetPatientId).WithUserId(targetDoctorId).Build(),
                _builder.AsValidPrescription().WithPatientId(targetPatientId).WithUserId(Guid.NewGuid()).Build(),
                _builder.AsValidPrescription().WithPatientId(Guid.NewGuid()).WithUserId(targetDoctorId).Build(),
                _builder.AsValidPrescription().Build()
            });

            // Act - 搜索特定ID
            var searchRequest = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = targetId.ToString().Substring(0, 8)
            };
            var searchResult = await _service.GetPagedAsync(searchRequest);

            // Assert
            Assert.Single(searchResult.Items);
            Assert.Equal(targetId, searchResult.Items[0].Id);

            // Act - 搜索患者ID
            searchRequest.SearchKeyword = targetPatientId.ToString().Substring(0, 8);
            searchResult = await _service.GetPagedAsync(searchRequest);

            // Assert
            Assert.Equal(2, searchResult.Items.Count);
            Assert.All(searchResult.Items, p => Assert.Equal(targetPatientId, p.PatientId));
        }

        #endregion

        public void Dispose()
        {
            _inMemoryDatabase?.Clear();
            _mockFactory?.ClearCache();
        }
    }
}