using System;
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
    /// ConsultationService异常处理测试 - UltraThink设计
    /// 职责单一：专注于异常场景和错误恢复
    /// 代码干净：清晰的异常测试模式
    /// 性能出色：快速异常检测
    /// </summary>
    public class ConsultationServiceExceptionTests : IDisposable
    {
        private readonly ConsultationService _consultationService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly ConsultationTestDataBuilder _consultationBuilder;

        public ConsultationServiceExceptionTests()
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

        #region Null参数异常测试

        [Fact]
        public async Task GetPagedAsync_NullQuery_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _consultationService.GetPagedAsync(null!));
        }

        [Fact]
        public async Task StartConsultationAsync_NullDto_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _consultationService.StartConsultationAsync(null!));
        }

        [Fact]
        public async Task UpdateConsultationAsync_NullDto_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _consultationService.UpdateConsultationAsync(Guid.NewGuid(), null!));
        }

        [Fact]
        public async Task CompleteConsultationAsync_NullDto_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _consultationService.CompleteConsultationAsync(Guid.NewGuid(), null!));
        }

        #endregion

        #region 无效ID异常测试

        [Fact]
        public async Task GetByIdAsync_EmptyGuid_ReturnsNull()
        {
            // Act
            var result = await _consultationService.GetByIdAsync(Guid.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_NonExistentId_ReturnsNull()
        {
            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateConsultationAsync_NonExistentId_ThrowsException()
        {
            // Arrange
            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "测试诊断"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _consultationService.UpdateConsultationAsync(Guid.NewGuid(), updateDto));
            Assert.Contains("看诊记录不存在", ex.Message);
        }

        [Fact]
        public async Task CompleteConsultationAsync_NonExistentId_ThrowsException()
        {
            // Arrange
            var completeDto = new ConsultationCompleteDto
            {
                Summary = "完成总结"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _consultationService.CompleteConsultationAsync(Guid.NewGuid(), completeDto));
            Assert.Contains("看诊记录不存在", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _consultationService.DeleteAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion

        #region 业务规则违反异常测试

        [Fact]
        public async Task StartConsultationAsync_DuplicateForMedicalCase_ThrowsException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var existingConsultation = _consultationBuilder
                .AsValidConsultation()
                .WithMedicalCaseId(medicalCaseId)
                .Build();
            await _context.Consultations.AddAsync(existingConsultation);
            await _context.SaveChangesAsync();

            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCaseId,
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _consultationService.StartConsultationAsync(startDto));
            Assert.Contains("该医疗案例已存在看诊记录", ex.Message);
        }

        [Fact]
        public async Task StartConsultationAsync_MedicalCaseNotFound_ThrowsException()
        {
            // Arrange
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // Act & Assert - 应该抛出异常，因为找不到医疗案例
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _consultationService.StartConsultationAsync(startDto));
        }

        [Fact]
        public async Task UpdateConsultationAsync_DisabledConsultation_ThrowsException()
        {
            // Arrange
            var consultation = _consultationBuilder
                .AsValidConsultation()
                .AsInactive()
                .Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "更新诊断"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto));
            Assert.Contains("看诊记录不存在", ex.Message);
        }

        #endregion

        #region 数据库异常测试

        [Fact]
        public async Task StartConsultationAsync_TransactionRollback_NoDataCreated()
        {
            // Arrange - 创建一个会导致异常的场景
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(), // 不存在的医疗案例
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            var countBefore = await _context.Consultations.CountAsync();

            // Act
            try
            {
                await _consultationService.StartConsultationAsync(startDto);
            }
            catch
            {
                // 忽略异常
            }

            // Assert - 确保没有创建新记录
            var countAfter = await _context.Consultations.CountAsync();
            Assert.Equal(countBefore, countAfter);
        }

        [Fact]
        public async Task CompleteConsultationAsync_TransactionRollback_StateUnchanged()
        {
            // Arrange
            var consultation = _consultationBuilder.AsValidConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var originalDiagnosis = consultation.Diagnosis;

            var completeDto = new ConsultationCompleteDto
            {
                Diagnosis = "新诊断",
                Summary = "完成总结"
            };

            // Act - 因为没有对应的医疗案例，应该失败
            try
            {
                await _consultationService.CompleteConsultationAsync(consultation.Id, completeDto);
            }
            catch
            {
                // 忽略异常
            }

            // Assert - 确保状态未改变
            var unchanged = await _context.Consultations.FindAsync(consultation.Id);
            Assert.Equal(originalDiagnosis, unchanged!.Diagnosis);
        }

        #endregion

        #region 边界值异常测试

        [Fact]
        public async Task GetPagedAsync_NegativePageNumber_HandlesGracefully()
        {
            // Arrange
            await CreateConsultationsInContext(5);
            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = -1,
                PageSize = 10
            };

            // Act & Assert - 应该处理负页码
            var result = await _consultationService.GetPagedAsync(query);
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_ZeroPageSize_HandlesGracefully()
        {
            // Arrange
            await CreateConsultationsInContext(5);
            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 0
            };

            // Act & Assert - 应该处理零页大小
            var result = await _consultationService.GetPagedAsync(query);
            Assert.NotNull(result);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetDoctorConsultationCountAsync_InvalidDateRange_ReturnsZero()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            await CreateConsultationsInContext(5);

            // Act - 结束日期早于开始日期
            var count = await _consultationService.GetDoctorConsultationCountAsync(
                doctorId, 
                DateTime.Today, 
                DateTime.Today.AddDays(-10));

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region 并发异常测试

        [Fact]
        public async Task ConcurrentStartConsultation_SameMedicalCase_OnlyOneSucceeds()
        {
            // Arrange
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

            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = medicalCaseId,
                PatientId = medicalCase.PatientId,
                UserId = Guid.NewGuid()
            };

            // Act - 尝试并发创建两个看诊
            var task1 = Task.Run(async () => await _consultationService.StartConsultationAsync(startDto));
            var task2 = Task.Run(async () => await _consultationService.StartConsultationAsync(startDto));

            var results = await Task.WhenAll(
                task1.ContinueWith(t => t.IsFaulted ? null : t.Result),
                task2.ContinueWith(t => t.IsFaulted ? null : t.Result));

            // Assert - 只有一个应该成功
            var successCount = results.Count(r => r != null);
            Assert.Equal(1, successCount);

            var consultationCount = await _context.Consultations
                .CountAsync(c => c.MedicalCaseId == medicalCaseId);
            Assert.Equal(1, consultationCount);
        }

        #endregion

        #region 状态异常测试

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_ThrowsException()
        {
            // Arrange
            var consultation = _consultationBuilder.AsValidConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act & Assert - 传入无效的状态值
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _consultationService.UpdateStatusAsync(consultation.Id, 999, "无效状态"));
        }

        [Fact]
        public async Task GetByIdAsync_DeletedConsultation_ReturnsNull()
        {
            // Arrange
            var consultation = _consultationBuilder
                .AsValidConsultation()
                .AsInactive()
                .Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _consultationService.GetByIdAsync(consultation.Id);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region 数据验证异常测试

        [Fact]
        public async Task StartConsultationAsync_EmptyPatientId_ThrowsException()
        {
            // Arrange
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.Empty,
                UserId = Guid.NewGuid()
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _consultationService.StartConsultationAsync(startDto));
        }

        [Fact]
        public async Task UpdateConsultationAsync_InvalidUpdateTime_HandlesCorrectly()
        {
            // Arrange
            var consultation = _consultationBuilder.AsValidConsultation().Build();
            consultation.UpdateTime = DateTime.MinValue; // 设置无效时间
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "更新诊断"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.NotEqual(DateTime.MinValue, result.UpdateTime);
            Assert.True(result.UpdateTime > DateTime.Now.AddMinutes(-1));
        }

        #endregion

        #region Helper Methods

        private async Task<ConsultationModel[]> CreateConsultationsInContext(int count)
        {
            var consultations = new ConsultationModel[count];
            for (int i = 0; i < count; i++)
            {
                consultations[i] = _consultationBuilder.AsValidConsultation().Build();
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