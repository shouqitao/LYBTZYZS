using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService核心功能测试 - UltraThink设计
    /// 职责单一：专注于MedicalCaseService的核心功能测试
    /// 代码干净：清晰的测试结构，AAA模式
    /// 性能出色：使用内存数据库，快速执行
    /// </summary>
    public class MedicalCaseServiceTests : IDisposable
    {
        private readonly MedicalCaseService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly MedicalCaseTestDataBuilder _builder;

        public MedicalCaseServiceTests()
        {
            _mockFactory = new MockFactory();
            _builder = new MedicalCaseTestDataBuilder();
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
                cfg.CreateMap<MedicalCaseEditDto, MedicalCaseUpdateDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new MedicalCaseService(
                _context,
                _repositoryMock.Object,
                _mapper,
                NullLogger<MedicalCaseService>.Instance);
        }

        #region GetListAsync Tests

        [Fact]
        public async Task GetListAsync_ReturnsAllMedicalCases()
        {
            // Arrange
            var cases = _builder.BuildMixedStatusCases(5);
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(cases);

            // Act
            var result = await _service.GetListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MedicalCaseModel>());

            // Act
            var result = await _service.GetListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidRequest_ReturnsPaginatedResult()
        {
            // Arrange
            var cases = new List<MedicalCaseModel>();
            for (int i = 0; i < 20; i++)
            {
                cases.Add(_builder.AsValidMedicalCase().Build());
            }
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(cases);

            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(20, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(1, result.CurrentPage);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearchKeyword_FiltersResults()
        {
            // Arrange
            var cases = new List<MedicalCaseModel>
            {
                _builder.AsValidMedicalCase().Build(),
                _builder.AsValidMedicalCase().Build(),
                _builder.AsValidMedicalCase().Build()
            };

            // 设置返回的DTO包含可搜索的内容
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(cases);

            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "特定关键词"
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            // 由于映射后的DTO中PatientName等为null，搜索结果为空
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetPagedAsync_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            var cases = new List<MedicalCaseModel>();
            for (int i = 0; i < 25; i++)
            {
                cases.Add(_builder.AsValidMedicalCase()
                    .WithCreateTime(DateTime.Now.AddDays(-i))
                    .Build());
            }
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(cases);

            var request = new PaginationRequest
            {
                CurrentPage = 2,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(2, result.CurrentPage);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDetailDto()
        {
            // Arrange
            var medicalCase = _builder.AsValidMedicalCase().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.GetByIdAsync(medicalCase.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(medicalCase.Id, result.Id);
            _repositoryMock.Verify(r => r.GetByIdAsync(medicalCase.Id), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel)null);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidDto_CreatesNewCase()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Remark = "新患者首诊"
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.PatientId, result.PatientId);
            Assert.Equal(MedicalCaseStatus.Registered, result.Status);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<MedicalCaseModel>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_SetsCorrectDefaults()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid()
            };

            MedicalCaseModel capturedModel = null;
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseModel>()))
                .Callback<MedicalCaseModel>(m => capturedModel = m)
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(capturedModel);
            Assert.NotEqual(Guid.Empty, capturedModel.Id);
            Assert.Equal(MedicalCaseStatus.Registered, capturedModel.Status);
            Assert.True(capturedModel.IsActive);
            Assert.True(capturedModel.CreateTime > DateTime.Now.AddMinutes(-1));
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingCase_UpdatesSuccessfully()
        {
            // Arrange
            var medicalCase = _builder.AsValidMedicalCase().Build();
            var updateDto = new MedicalCaseUpdateDto
            {
                Status = (int)MedicalCaseStatus.InConsultation,
                Remark = "开始看诊"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            var result = await _service.UpdateAsync(medicalCase.Id, updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal(MedicalCaseStatus.InConsultation, medicalCase.Status);
            Assert.Equal("开始看诊", medicalCase.Remark);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingCase_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto { Remark = "更新" };

            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel)null);

            // Act
            var result = await _service.UpdateAsync(id, updateDto);

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithCompleteTime_SetsCompleteTime()
        {
            // Arrange
            var medicalCase = _builder.AsValidMedicalCase().Build();
            var completeTime = DateTime.Now;
            var updateDto = new MedicalCaseUpdateDto
            {
                CompleteTime = completeTime
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            await _service.UpdateAsync(medicalCase.Id, updateDto);

            // Assert
            Assert.Equal(completeTime, medicalCase.CompleteTime);
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_ToCompleted_SetsCompleteTime()
        {
            // Arrange
            var medicalCase = _builder.AsConsultingCase().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            var result = await _service.UpdateStatusAsync(medicalCase.Id, MedicalCaseStatus.Completed);

            // Assert
            Assert.True(result);
            Assert.Equal(MedicalCaseStatus.Completed, medicalCase.Status);
            Assert.NotNull(medicalCase.CompleteTime);
            Assert.True(medicalCase.CompleteTime > DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public async Task UpdateStatusAsync_NonExistingCase_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel)null);

            // Act
            var result = await _service.UpdateStatusAsync(id, MedicalCaseStatus.Completed);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingCase_SoftDeletes()
        {
            // Arrange
            var medicalCase = _builder.AsValidMedicalCase().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            var result = await _service.DeleteAsync(medicalCase.Id);

            // Assert
            Assert.True(result);
            Assert.False(medicalCase.IsActive);
            Assert.True(medicalCase.UpdateTime > DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public async Task DeleteAsync_NonExistingCase_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel)null);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_ReturnsPatientCases()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var cases = _builder.BuildPatientHistory(patientId, 3);
            _repositoryMock.Setup(r => r.GetByPatientIdAsync(patientId))
                .ReturnsAsync(cases);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, c => Assert.Equal(patientId, c.PatientId));
        }

        [Fact]
        public async Task GetByPatientIdAsync_NoHistory_ReturnsEmptyList()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseModel>());

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetByUserIdAsync Tests

        [Fact]
        public async Task GetByUserIdAsync_ReturnsDoctorCases()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cases = new List<MedicalCaseModel>
            {
                _builder.AsValidMedicalCase().WithUserId(userId).Build(),
                _builder.AsValidMedicalCase().WithUserId(userId).Build()
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cases);

            // Act
            var result = await _service.GetByUserIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(userId, c.UserId));
        }

        #endregion

        #region GetTodayCasesAsync Tests

        [Fact]
        public async Task GetTodayCasesAsync_ReturnsTodayCases()
        {
            // Arrange
            var today = DateTime.Today;
            var cases = new List<MedicalCaseModel>
            {
                _builder.AsTodayCase().Build(),
                _builder.AsTodayCase().Build()
            };
            _repositoryMock.Setup(r => r.GetByDateRangeAsync(today, today.AddDays(1)))
                .ReturnsAsync(cases);

            // Act
            var result = await _service.GetTodayCasesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Workflow Tests

        [Fact]
        public async Task StartConsultationAsync_ValidCase_UpdatesStatus()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var consultationId = Guid.NewGuid();
            var medicalCase = _builder.AsRegistered().WithId(caseId).Build();
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.StartConsultationAsync(caseId, consultationId);

            // Assert
            Assert.True(result);
            var updatedCase = await _context.MedicalCases.FindAsync(caseId);
            Assert.Equal(MedicalCaseStatus.InConsultation, updatedCase.Status);
            Assert.Equal(consultationId, updatedCase.ConsultationId);
        }

        [Fact]
        public async Task CompleteConsultationAsync_ValidCase_CompletesCase()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var medicalCase = _builder.AsInConsultation().WithId(caseId).Build();
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CompleteConsultationAsync(caseId, prescriptionId);

            // Assert
            Assert.True(result);
            var updatedCase = await _context.MedicalCases.FindAsync(caseId);
            Assert.Equal(MedicalCaseStatus.Completed, updatedCase.Status);
            Assert.NotNull(updatedCase.CompleteTime);
        }

        [Fact]
        public async Task CancelMedicalCaseAsync_ValidCase_CancelsWithReason()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var reason = "患者临时有事";
            var medicalCase = _builder.AsRegistered().WithId(caseId).Build();
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CancelMedicalCaseAsync(caseId, reason);

            // Assert
            Assert.True(result);
            var updatedCase = await _context.MedicalCases.FindAsync(caseId);
            Assert.Equal(MedicalCaseStatus.Cancelled, updatedCase.Status);
            Assert.Equal(reason, updatedCase.Remark);
        }

        #endregion

        #region GetPendingCasesByStatusAsync Tests

        [Fact]
        public async Task GetPendingCasesByStatusAsync_ReturnsFilteredCases()
        {
            // Arrange
            await _context.MedicalCases.AddRangeAsync(
                _builder.AsRegistered().Build(),
                _builder.AsRegistered().Build(),
                _builder.AsInConsultation().Build(),
                _builder.AsCompleted().Build()
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetPendingCasesByStatusAsync(MedicalCaseStatus.Registered);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(MedicalCaseStatus.Registered, c.Status));
        }

        #endregion

        #region Advanced Paging Tests

        [Fact]
        public async Task GetPagedAsync_WithStatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            await _context.MedicalCases.AddRangeAsync(
                _builder.AsRegistered().Build(),
                _builder.AsInConsultation().Build(),
                _builder.AsCompleted().Build()
            );
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _service.GetPagedAsync(1, 10, MedicalCaseStatus.Registered);

            // Assert
            Assert.Equal(1, total);
            Assert.Single(items);
            Assert.Equal(MedicalCaseStatus.Registered, items[0].Status);
        }

        [Fact]
        public async Task GetPagedAsync_WithDateRange_ReturnsFilteredResults()
        {
            // Arrange
            var today = DateTime.Today;
            await _context.MedicalCases.AddRangeAsync(
                _builder.AsValidMedicalCase().WithCreateTime(today.AddDays(-5)).Build(),
                _builder.AsValidMedicalCase().WithCreateTime(today.AddDays(-2)).Build(),
                _builder.AsValidMedicalCase().WithCreateTime(today).Build(),
                _builder.AsValidMedicalCase().WithCreateTime(today.AddDays(1)).Build()
            );
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _service.GetPagedAsync(
                1, 10, null, today.AddDays(-3), today.AddHours(23));

            // Assert
            Assert.Equal(2, total);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _mockFactory?.ClearCache();
        }
    }
}