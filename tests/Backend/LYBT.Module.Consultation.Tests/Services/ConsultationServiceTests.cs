using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// ConsultationService核心功能测试 - UltraThink设计
    /// 职责单一：专注于ConsultationService的核心功能测试
    /// 代码干净：清晰的测试结构，AAA模式
    /// 性能出色：使用内存数据库，快速执行
    /// </summary>
    public class ConsultationServiceTests : IDisposable
    {
        private readonly ConsultationService _consultationService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;
        private readonly MockFactory _mockFactory;
        private readonly ConsultationTestDataBuilder _consultationBuilder;
        private readonly UserTestDataBuilder _userBuilder;

        public ConsultationServiceTests()
        {
            _mockFactory = new MockFactory();
            _consultationBuilder = new ConsultationTestDataBuilder();
            _userBuilder = new UserTestDataBuilder();

            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 配置AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ConsultationModel, ConsultationDto>();
                cfg.CreateMap<ConsultationModel, ConsultationDetailDto>();
                cfg.CreateMap<ConsultationStartDto, ConsultationModel>();
                cfg.CreateMap<ConsultationUpdateDto, ConsultationModel>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _logger = NullLogger<ConsultationService>.Instance;

            _consultationService = new ConsultationService(_context, _mapper, _logger);
        }

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidQuery_ReturnsPaginatedResult()
        {
            // Arrange
            await CreateConsultationsInContext(10);
            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 5
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(5, result.Data.Count);
            Assert.Equal(1, result.PageIndex);
        }

        [Fact]
        public async Task GetPagedAsync_WithPatientIdFilter_ReturnsFilteredResults()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var consultations = new List<ConsultationModel>
            {
                _consultationBuilder.AsValidConsultation().WithPatientId(patientId).Build(),
                _consultationBuilder.AsValidConsultation().WithPatientId(patientId).Build(),
                _consultationBuilder.AsValidConsultation().WithPatientId(Guid.NewGuid()).Build()
            };
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PatientId = patientId,
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Data, c => Assert.Equal(patientId, c.PatientId));
        }

        [Fact]
        public async Task GetPagedAsync_WithDateRangeFilter_ReturnsFilteredResults()
        {
            // Arrange
            var today = DateTime.Today;
            await _context.Consultations.AddRangeAsync(new[]
            {
                _consultationBuilder.AsValidConsultation().WithConsultationTime(today.AddDays(-5)).Build(),
                _consultationBuilder.AsValidConsultation().WithConsultationTime(today.AddDays(-2)).Build(),
                _consultationBuilder.AsValidConsultation().WithConsultationTime(today).Build(),
                _consultationBuilder.AsValidConsultation().WithConsultationTime(today.AddDays(2)).Build()
            });
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                StartDate = today.AddDays(-3),
                EndDate = today.AddDays(1),
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_WithDiagnosisKeyword_ReturnsMatchingResults()
        {
            // Arrange
            await _context.Consultations.AddRangeAsync(new[]
            {
                _consultationBuilder.AsValidConsultation().WithDiagnosis("头痛发热").Build(),
                _consultationBuilder.AsValidConsultation().WithDiagnosis("腹痛腹泻").Build(),
                _consultationBuilder.AsValidConsultation().WithTCMDiagnosis("风寒头痛").Build()
            });
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                DiagnosisKeyword = "头痛",
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_OnlyReturnsEnabledConsultations()
        {
            // Arrange
            await _context.Consultations.AddRangeAsync(new[]
            {
                _consultationBuilder.AsValidConsultation().AsActive().Build(),
                _consultationBuilder.AsValidConsultation().AsActive().Build(),
                _consultationBuilder.AsValidConsultation().AsInactive().Build()
            });
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_ReturnsOrderedByConsultationTimeDesc()
        {
            // Arrange
            var consultations = new[]
            {
                _consultationBuilder.AsValidConsultation().WithConsultationTime(DateTime.Now.AddDays(-3)).Build(),
                _consultationBuilder.AsValidConsultation().WithConsultationTime(DateTime.Now.AddDays(-1)).Build(),
                _consultationBuilder.AsValidConsultation().WithConsultationTime(DateTime.Now.AddDays(-2)).Build()
            };
            await _context.Consultations.AddRangeAsync(consultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            Assert.True(result.Data[0].ConsultationTime > result.Data[1].ConsultationTime);
            Assert.True(result.Data[1].ConsultationTime > result.Data[2].ConsultationTime);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenConsultationExists_ReturnsDetailDto()
        {
            // Arrange
            var consultation = _consultationBuilder.AsCompleteTCMConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _consultationService.GetByIdAsync(consultation.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(consultation.Id, result.Id);
            Assert.Equal(consultation.Diagnosis, result.Diagnosis);
        }

        [Fact]
        public async Task GetByIdAsync_WhenConsultationNotExists_ReturnsNull()
        {
            // Act
            var result = await _consultationService.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_WhenConsultationDisabled_ReturnsNull()
        {
            // Arrange
            var consultation = _consultationBuilder.AsValidConsultation().AsInactive().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _consultationService.GetByIdAsync(consultation.Id);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByMedicalCaseIdAsync Tests

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WhenExists_ReturnsDetailDto()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var consultation = _consultationBuilder
                .AsValidConsultation()
                .WithMedicalCaseId(medicalCaseId)
                .Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(medicalCaseId, result.MedicalCaseId);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WhenNotExists_ReturnsNull()
        {
            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region StartConsultationAsync Tests

        [Fact]
        public async Task StartConsultationAsync_WithValidData_CreatesNewConsultation()
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

            // Act
            var result = await _consultationService.StartConsultationAsync(startDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(medicalCaseId, result.MedicalCaseId);
            Assert.Equal(startDto.PatientId, result.PatientId);
            Assert.Equal(startDto.UserId, result.UserId);

            // 验证医疗案例状态已更新
            var updatedCase = await _context.MedicalCases.FindAsync(medicalCaseId);
            Assert.Equal(MedicalCaseStatus.InConsultation, updatedCase!.Status);
        }

        [Fact]
        public async Task StartConsultationAsync_WhenConsultationExists_ThrowsException()
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
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _consultationService.StartConsultationAsync(startDto));
        }

        [Fact]
        public async Task StartConsultationAsync_WithTransaction_RollbacksOnError()
        {
            // Arrange
            var startDto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(), // 不存在的医疗案例
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            var consultationCountBefore = await _context.Consultations.CountAsync();

            // Act & Assert
            try
            {
                await _consultationService.StartConsultationAsync(startDto);
            }
            catch
            {
                // 忽略异常
            }

            var consultationCountAfter = await _context.Consultations.CountAsync();
            Assert.Equal(consultationCountBefore, consultationCountAfter);
        }

        #endregion

        #region UpdateConsultationAsync Tests

        [Fact]
        public async Task UpdateConsultationAsync_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "更新后的诊断",
                TCMDiagnosis = "风寒感冒",
                TreatmentPrinciple = "疏风散寒",
                MedicalAdvice = "多喝水，注意休息"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Diagnosis, result.Diagnosis);
            Assert.Equal(updateDto.TCMDiagnosis, result.TCMDiagnosis);
            Assert.Equal(updateDto.TreatmentPrinciple, result.TreatmentPrinciple);
        }

        [Fact]
        public async Task UpdateConsultationAsync_WhenNotExists_ThrowsException()
        {
            // Arrange
            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "更新诊断"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _consultationService.UpdateConsultationAsync(Guid.NewGuid(), updateDto));
        }

        [Fact]
        public async Task UpdateConsultationAsync_OnlyUpdatesProvidedFields()
        {
            // Arrange
            var consultation = _consultationBuilder
                .AsCompleteTCMConsultation()
                .WithDiagnosis("原始诊断")
                .WithTCMDiagnosis("原始中医诊断")
                .Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "新诊断"
                // 不更新TCMDiagnosis
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal("新诊断", result.Diagnosis);
            Assert.Equal("原始中医诊断", result.TCMDiagnosis); // 保持不变
        }

        #endregion

        #region CompleteConsultationAsync Tests

        [Fact]
        public async Task CompleteConsultationAsync_WithValidData_CompletesSuccessfully()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var consultation = _consultationBuilder
                .AsCompleteTCMConsultation()
                .WithMedicalCaseId(medicalCaseId)
                .Build();
            
            var medicalCase = new MedicalCaseModel
            {
                Id = medicalCaseId,
                PatientId = consultation.PatientId,
                ConsultationId = consultation.Id,
                Status = MedicalCaseStatus.InConsultation,
                CreateTime = DateTime.Now,
                IsActive = true
            };

            await _context.Consultations.AddAsync(consultation);
            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.SaveChangesAsync();

            var completeDto = new ConsultationCompleteDto
            {
                Summary = "诊疗完成，患者症状明显改善",
                NextVisitDate = DateTime.Now.AddDays(7)
            };

            // Act
            var result = await _consultationService.CompleteConsultationAsync(consultation.Id, completeDto);

            // Assert
            Assert.True(result);
            
            // 验证医疗案例状态
            var updatedCase = await _context.MedicalCases.FindAsync(medicalCaseId);
            Assert.Equal(MedicalCaseStatus.Completed, updatedCase!.Status);
        }

        #endregion

        #region 中医四诊相关测试

        [Fact]
        public async Task UpdateConsultationAsync_WithTCMExamination_UpdatesAllFields()
        {
            // Arrange
            var consultation = _consultationBuilder.AsNewConsultation().Build();
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色萎黄，精神倦怠",
                AuscultationOlfaction = "语声低微，口气清淡",
                Inquiry = "食欲不振，大便溏薄",
                Palpation = "腹软无压痛",
                TongueInspection = "舌淡红，苔薄白",
                PulseCondition = "脉沉细"
            };

            // Act
            var result = await _consultationService.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            Assert.Equal(updateDto.Inspection, result.Inspection);
            Assert.Equal(updateDto.AuscultationOlfaction, result.AuscultationOlfaction);
            Assert.Equal(updateDto.Inquiry, result.Inquiry);
            Assert.Equal(updateDto.Palpation, result.Palpation);
            Assert.Equal(updateDto.TongueInspection, result.TongueInspection);
            Assert.Equal(updateDto.PulseCondition, result.PulseCondition);
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