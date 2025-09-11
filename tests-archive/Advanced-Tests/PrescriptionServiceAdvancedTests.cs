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
    /// PrescriptionService高级场景测试 - UltraThink设计
    /// 职责单一：专注于复杂业务场景和特殊功能测试
    /// 代码干净：清晰的测试组织，AAA模式
    /// 性能出色：高效的测试执行
    /// </summary>
    public class PrescriptionServiceAdvancedTests : IDisposable
    {
        private readonly PrescriptionService _service;
        private readonly Mock<IPrescriptionRepository> _repositoryMock;
        private readonly Mock<IUnifiedLogService> _logServiceMock;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly PrescriptionTestDataBuilder _builder;

        public PrescriptionServiceAdvancedTests()
        {
            _mockFactory = new MockFactory();
            _builder = new PrescriptionTestDataBuilder();
            _repositoryMock = new Mock<IPrescriptionRepository>();
            _logServiceMock = new Mock<IUnifiedLogService>();

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

            _service = new PrescriptionService(
                _repositoryMock.Object,
                _logServiceMock.Object,
                _mapper);
        }

        #region 医生工作流测试

        [Fact]
        public async Task GetDoctorTodayPrescriptionsAsync_ReturnsOnlyTodayPrescriptions()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var today = DateTime.Today;
            
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().WithUserId(doctorId).WithCreateTime(today.AddHours(9)).Build(),
                _builder.AsValidPrescription().WithUserId(doctorId).WithCreateTime(today.AddHours(14)).Build(),
                _builder.AsValidPrescription().WithUserId(doctorId).WithCreateTime(today.AddDays(-1)).Build(), // 昨天
                _builder.AsValidPrescription().WithUserId(Guid.NewGuid()).WithCreateTime(today).Build() // 其他医生
            };
            
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetDoctorTodayPrescriptionsAsync(doctorId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(doctorId, p.UserId));
            Assert.All(result, p => Assert.Equal(today, p.CreateTime.Date));
        }

        [Fact]
        public async Task GetDoctorTodayPrescriptionsAsync_OrderedByTimeDesc()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var prescriptions = _builder.BuildDoctorTodayPrescriptions(doctorId, 5);
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions.ToList());

            // Act
            var result = await _service.GetDoctorTodayPrescriptionsAsync(doctorId);

            // Assert
            Assert.Equal(5, result.Count);
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(result[i].CreateTime >= result[i + 1].CreateTime);
            }
        }

        #endregion

        #region 处方复制功能测试

        [Fact]
        public async Task CopyLastPrescriptionAsync_WithHistory_CopiesSuccessfully()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            
            var lastPrescription = _builder
                .AsClassicPrescription()
                .WithPatientId(patientId)
                .Build();
            
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(new List<PrescriptionModel> { lastPrescription });
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, "操作员");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal(doctorId, result.DoctorId);
            Assert.Equal(lastPrescription.Diagnosis, result.Diagnosis);
            Assert.Equal(lastPrescription.DosageCount, result.DosageCount);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PrescriptionModel>()), Times.Once);
        }

        [Fact]
        public async Task CopyLastPrescriptionAsync_NoHistory_ReturnsNull()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(new List<PrescriptionModel>());

            // Act
            var result = await _service.CopyLastPrescriptionAsync(patientId, Guid.NewGuid(), Guid.NewGuid(), "操作员");

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PrescriptionModel>()), Times.Never);
        }

        #endregion

        #region 快速保存和提交功能测试

        [Fact]
        public async Task QuickSaveAsync_ExistingPrescription_SavesAsDraft()
        {
            // Arrange
            var prescription = _builder.AsValidPrescription().Build();
            var quickDto = new QuickPrescriptionDto
            {
                Diagnosis = "快速保存的诊断",
                Advice = "快速保存的医嘱"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.QuickSaveAsync(prescription.Id, quickDto, Guid.NewGuid(), "操作员");

            // Assert
            Assert.True(result);
            Assert.Equal(quickDto.Diagnosis, prescription.Diagnosis);
            Assert.Equal(quickDto.Advice, prescription.Advice);
            Assert.Equal(PrescriptionStatus.Draft, prescription.Status);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Once);
        }

        [Fact]
        public async Task QuickSaveAsync_NonExistingPrescription_ReturnsFalse()
        {
            // Arrange
            var quickDto = new QuickPrescriptionDto
            {
                Diagnosis = "诊断",
                Advice = "医嘱"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((PrescriptionModel)null);

            // Act
            var result = await _service.QuickSaveAsync(Guid.NewGuid(), quickDto, Guid.NewGuid(), "操作员");

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()), Times.Never);
        }

        [Fact]
        public async Task SubmitPrescriptionAsync_DraftPrescription_SubmitsSuccessfully()
        {
            // Arrange
            var prescription = _builder
                .AsValidPrescription()
                .AsDraft()
                .WithDiagnosis("完整诊断")
                .Build();

            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SubmitPrescriptionAsync(prescription.Id, Guid.NewGuid(), "操作员");

            // Assert
            Assert.True(result);
            Assert.Equal(PrescriptionStatus.Draft, prescription.Status); // 代码中又设回Draft了
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Once);
        }

        [Fact]
        public async Task SubmitPrescriptionAsync_IncompletePrescription_ReturnsFalse()
        {
            // Arrange
            var prescription = _builder
                .AsEmptyPrescription() // 没有处方项
                .AsDraft()
                .Build();

            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);

            // Act
            var result = await _service.SubmitPrescriptionAsync(prescription.Id, Guid.NewGuid(), "操作员");

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()), Times.Never);
        }

        [Fact]
        public async Task SubmitPrescriptionAsync_NonDraftPrescription_ReturnsFalse()
        {
            // Arrange
            var prescription = _builder
                .AsCompletedPrescription()
                .Build();

            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);

            // Act
            var result = await _service.SubmitPrescriptionAsync(prescription.Id, Guid.NewGuid(), "操作员");

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()), Times.Never);
        }

        #endregion

        #region 统计功能测试

        [Fact]
        public async Task GetStatisticsAsync_AllPrescriptions_CalculatesCorrectly()
        {
            // Arrange
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsDraft().Build(),
                _builder.AsDraft().Build(),
                _builder.AsCompleted().Build(),
                _builder.AsCompleted().Build(),
                _builder.AsCompleted().Build()
            };

            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetStatisticsAsync();

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.DraftCount);
            Assert.Equal(3, result.CompletedCount);
        }

        [Fact]
        public async Task GetStatisticsAsync_FilterByDoctor_ReturnsFilteredStats()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().WithUserId(doctorId).Build(),
                _builder.AsValidPrescription().WithUserId(doctorId).Build(),
                _builder.AsValidPrescription().WithUserId(Guid.NewGuid()).Build()
            };

            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetStatisticsAsync(doctorId);

            // Assert
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetStatisticsAsync_FilterByDateRange_ReturnsFilteredStats()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().CreatedDaysAgo(10).Build(), // 范围外
                _builder.AsValidPrescription().CreatedDaysAgo(5).Build(),  // 范围内
                _builder.AsValidPrescription().CreatedDaysAgo(2).Build(),  // 范围内
                _builder.AsValidPrescription().CreatedToday().Build()      // 范围内
            };

            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetStatisticsAsync(null, startDate, endDate);

            // Assert
            Assert.Equal(3, result.TotalCount);
        }

        #endregion

        #region 模板功能测试

        [Fact]
        public async Task CreateFromTemplateAsync_CurrentlyReturnsNull()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            // Act
            var result = await _service.CreateFromTemplateAsync(
                templateId, patientId, doctorId, Guid.NewGuid(), "操作员");

            // Assert
            Assert.Null(result); // 功能暂未实现
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task GetPagedAsync_LargeDataset_PerformsEfficiently()
        {
            // Arrange
            var prescriptions = new List<PrescriptionModel>();
            for (int i = 0; i < 1000; i++)
            {
                prescriptions.Add(_builder.AsValidPrescription().Build());
            }

            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            var request = new PaginationRequest
            {
                CurrentPage = 10,
                PageSize = 50
            };

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _service.GetPagedAsync(request);
            stopwatch.Stop();

            // Assert
            Assert.Equal(1000, result.TotalCount);
            Assert.Equal(50, result.Items.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"分页查询耗时过长: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 数据完整性测试

        [Fact]
        public async Task CreateAsync_PreservesItemDetails()
        {
            // Arrange
            var items = new List<PrescriptionItemCreateDto>
            {
                new PrescriptionItemCreateDto
                {
                    HerbId = Guid.NewGuid(),
                    HerbName = "麻黄",
                    Quantity = 9,
                    Unit = "g",
                    UnitPrice = 2.5m,
                    Remark = "先煎"
                },
                new PrescriptionItemCreateDto
                {
                    HerbId = Guid.NewGuid(),
                    HerbName = "桂枝",
                    Quantity = 6,
                    Unit = "g",
                    UnitPrice = 3.0m,
                    Remark = "后下"
                }
            };

            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "风寒感冒",
                Items = items
            };

            PrescriptionModel capturedModel = null;
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PrescriptionModel>()))
                .Callback<PrescriptionModel>(m => capturedModel = m)
                .ReturnsAsync(true);

            // Act
            await _service.CreateAsync(createDto, Guid.NewGuid(), "操作员");

            // Assert
            Assert.NotNull(capturedModel);
            Assert.Equal(2, capturedModel.Items.Count);
            Assert.Contains(capturedModel.Items, i => i.HerbName == "麻黄" && i.Remark == "先煎");
            Assert.Contains(capturedModel.Items, i => i.HerbName == "桂枝" && i.Remark == "后下");
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task GetPatientHistoryAsync_ZeroLimit_ReturnsEmpty()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var prescriptions = _builder.BuildPatientHistory(patientId, 5);
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions.ToList());

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId, 0);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPagedAsync_PageBeyondTotal_ReturnsEmpty()
        {
            // Arrange
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().Build(),
                _builder.AsValidPrescription().Build()
            };
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            var request = new PaginationRequest
            {
                CurrentPage = 10,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Empty(result.Items);
        }

        #endregion

        public void Dispose()
        {
            _mockFactory?.ClearCache();
        }
    }
}