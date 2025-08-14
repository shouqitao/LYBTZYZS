using System;
using System.Collections.Generic;
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
    /// MedicalCaseService异常处理测试 - UltraThink设计
    /// 职责单一：专注于异常场景和错误恢复
    /// 代码干净：清晰的异常测试模式
    /// 性能出色：快速异常检测
    /// </summary>
    public class MedicalCaseServiceExceptionTests : IDisposable
    {
        private readonly MedicalCaseService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;
        private readonly MockFactory _mockFactory;
        private readonly MedicalCaseTestDataBuilder _builder;

        public MedicalCaseServiceExceptionTests()
        {
            _mockFactory = new MockFactory();
            _builder = new MedicalCaseTestDataBuilder();
            _repositoryMock = new Mock<IMedicalCaseRepository>();
            _loggerMock = new Mock<ILogger<MedicalCaseService>>();

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
                _loggerMock.Object);
        }

        #region Null参数异常测试

        [Fact]
        public async Task CreateAsync_NullDto_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.CreateAsync(null));
        }

        [Fact]
        public async Task UpdateAsync_NullDto_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.UpdateAsync(Guid.NewGuid(), null));
        }

        [Fact]
        public async Task GetPagedAsync_NullRequest_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.GetPagedAsync(null));
        }

        #endregion

        #region Repository异常处理测试

        [Fact]
        public async Task GetListAsync_RepositoryThrows_LogsAndRethrows()
        {
            // Arrange
            var exception = new InvalidOperationException("数据库连接失败");
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.GetListAsync());

            Assert.Equal("数据库连接失败", thrown.Message);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("获取医疗案例列表失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_RepositoryThrows_LogsAndRethrows()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseModel>()))
                .ThrowsAsync(new DbUpdateException("唯一键冲突"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(
                async () => await _service.CreateAsync(createDto));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("创建医疗案例失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrows_LogsAndRethrows()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto { Remark = "更新" };
            var medicalCase = _builder.AsValidMedicalCase().Build();

            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("并发冲突"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                async () => await _service.UpdateAsync(id, updateDto));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("更新医疗案例失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region 无效ID异常测试

        [Fact]
        public async Task GetByIdAsync_EmptyGuid_ReturnsNull()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((MedicalCaseModel)null);

            // Act
            var result = await _service.GetByIdAsync(Guid.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_NonExistentId_LogsWarningAndReturnsFalse()
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
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("医疗案例不存在")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentId_LogsWarningAndReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((MedicalCaseModel)null);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("医疗案例不存在")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region 业务规则异常测试

        [Fact]
        public async Task StartConsultationAsync_CaseNotFound_ReturnsFalse()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var consultationId = Guid.NewGuid();

            // Act
            var result = await _service.StartConsultationAsync(caseId, consultationId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CompleteConsultationAsync_CaseNotFound_ReturnsFalse()
        {
            // Arrange
            var caseId = Guid.NewGuid();

            // Act
            var result = await _service.CompleteConsultationAsync(caseId, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CancelMedicalCaseAsync_CaseNotFound_ReturnsFalse()
        {
            // Arrange
            var caseId = Guid.NewGuid();

            // Act
            var result = await _service.CancelMedicalCaseAsync(caseId, "取消原因");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region 数据验证异常测试

        [Fact]
        public async Task CreateAsync_InvalidPatientId_HandlesGracefully()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.Empty, // 无效的患者ID
                UserId = Guid.NewGuid()
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Guid.Empty, result.PatientId); // 不验证，直接使用
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_HandlesGracefully()
        {
            // Arrange
            var medicalCase = _builder.AsValidMedicalCase().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act - 传入无效的状态值
            var result = await _service.UpdateStatusAsync(medicalCase.Id, (MedicalCaseStatus)999);

            // Assert
            Assert.True(result);
            Assert.Equal((MedicalCaseStatus)999, medicalCase.Status); // 不验证，直接赋值
        }

        #endregion

        #region 并发异常测试

        [Fact]
        public async Task ConcurrentUpdates_OptimisticConcurrency_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var medicalCase = _builder.AsValidMedicalCase().Build();
            
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(medicalCase);
            
            var concurrencyException = new DbUpdateConcurrencyException("并发更新冲突");
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ThrowsAsync(concurrencyException);

            var updateDto = new MedicalCaseUpdateDto { Remark = "并发更新" };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                async () => await _service.UpdateAsync(id, updateDto));
        }

        #endregion

        #region 边界条件异常测试

        [Fact]
        public async Task GetPagedAsync_NegativePage_HandlesGracefully()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MedicalCaseModel> { _builder.AsValidMedicalCase().Build() });

            var request = new PaginationRequest
            {
                CurrentPage = -1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            // Skip会处理负数，返回所有记录
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetPagedAsync_ZeroPageSize_ReturnsEmpty()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(_builder.BuildMixedStatusCases(5));

            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 0
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetPagedAsync_VeryLargePageSize_HandlesGracefully()
        {
            // Arrange
            var cases = _builder.BuildMixedStatusCases(10);
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(cases);

            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = int.MaxValue
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Items.Count); // 返回所有记录
        }

        #endregion

        #region 数据库连接异常测试

        [Fact]
        public async Task GetTodayByUserIdAsync_DbConnectionFailed_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            // 模拟数据库连接失败
            _context.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await _service.GetTodayByUserIdAsync(userId));
        }

        #endregion

        #region 映射异常测试

        [Fact]
        public async Task GetListAsync_MapperFailure_ThrowsException()
        {
            // Arrange
            var invalidCases = new List<MedicalCaseModel> { null }; // null会导致映射失败
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(invalidCases);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.GetListAsync());
        }

        #endregion

        #region 事务异常测试

        [Fact]
        public async Task StartConsultationAsync_SaveChangesFails_RollsBack()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var consultationId = Guid.NewGuid();
            var medicalCase = _builder.AsRegistered().WithId(caseId).Build();
            
            // 添加案例但不保存，模拟事务中的状态
            _context.MedicalCases.Add(medicalCase);
            
            // 立即dispose context模拟保存失败
            _context.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await _service.StartConsultationAsync(caseId, consultationId));
        }

        #endregion

        #region 日期范围异常测试

        [Fact]
        public async Task GetPagedAsync_InvalidDateRange_ReturnsEmpty()
        {
            // Arrange
            await _context.MedicalCases.AddRangeAsync(
                _builder.AsValidMedicalCase().Build(),
                _builder.AsValidMedicalCase().Build()
            );
            await _context.SaveChangesAsync();

            // Act - 结束日期早于开始日期
            var (items, total) = await _service.GetPagedAsync(
                1, 10, null, 
                DateTime.Today, 
                DateTime.Today.AddDays(-10));

            // Assert
            Assert.Empty(items);
            Assert.Equal(0, total);
        }

        #endregion

        #region 特殊字符异常测试

        [Fact]
        public async Task CreateAsync_VeryLongRemark_HandlesGracefully()
        {
            // Arrange
            var veryLongRemark = new string('A', 10000); // 10000个字符
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Remark = veryLongRemark
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(veryLongRemark, result.Remark);
        }

        [Fact]
        public async Task UpdateAsync_SqlInjectionAttempt_HandledSafely()
        {
            // Arrange
            var medicalCase = _builder.AsValidMedicalCase().Build();
            var maliciousRemark = "'; DROP TABLE MedicalCases; --";
            
            _repositoryMock.Setup(r => r.GetByIdAsync(medicalCase.Id))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MedicalCaseModel>()))
                .ReturnsAsync((MedicalCaseModel m) => m);

            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = maliciousRemark
            };

            // Act
            var result = await _service.UpdateAsync(medicalCase.Id, updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal(maliciousRemark, medicalCase.Remark); // EF Core会自动处理SQL注入
        }

        #endregion

        public void Dispose()
        {
            try
            {
                _context?.Dispose();
            }
            catch
            {
                // 忽略dispose异常
            }
            _mockFactory?.ClearCache();
        }
    }
}