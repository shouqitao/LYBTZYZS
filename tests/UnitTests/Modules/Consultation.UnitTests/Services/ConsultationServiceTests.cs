using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Consultation;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Common;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// 诊疗服务单元测试
    /// 测试诊疗记录的创建、查询、更新、删除等核心业务逻辑
    /// </summary>
    public class ConsultationServiceTests : TestBase
    {
        private readonly ConsultationService _consultationService;
        private readonly Mock<IConsultationRepository> _repositoryMock;
        private readonly Mock<ILogger<ConsultationService>> _loggerMock;
        private readonly AppDbContext _context;

        public ConsultationServiceTests()
        {
            // 创建内存数据库上下文
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _repositoryMock = CreateMock<IConsultationRepository>();
            _loggerMock = CreateLoggerMock<ConsultationService>();

            _consultationService = new ConsultationService(
                _context,
                _repositoryMock.Object,
                _loggerMock.Object,
                Mapper);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 注册诊疗服务相关的依赖
            services.AddSingleton(_consultationService);
            services.AddSingleton(_repositoryMock.Object);
        }

        #region 创建诊疗记录测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var createDto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "头痛发热",
                PresentIllnessHistory = "昨日开始出现头痛，伴有发热38.5度",
                PastMedicalHistory = "既往体健",
                PersonalHistory = "无特殊",
                FamilyHistory = "父母健康",
                PhysicalExamination = "T 38.5℃，P 90次/分，R 20次/分，BP 120/80mmHg",
                ChineseMedicineDiagnosis = "外感风热",
                WesternMedicineDiagnosis = "上呼吸道感染",
                TreatmentPrinciple = "疏风清热",
                ConsultationDate = DateTime.Now
            };

            var consultation = new ConsultationRecord
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                ChiefComplaint = createDto.ChiefComplaint,
                ConsultationDate = createDto.ConsultationDate,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<ConsultationRecord>()))
                .ReturnsAsync(consultation);

            // Act
            var result = await _consultationService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PatientId.Should().Be(createDto.PatientId);
            result.Data.ChiefComplaint.Should().Be(createDto.ChiefComplaint);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ConsultationRecord>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNullData_ShouldReturnFailure()
        {
            // Arrange
            ConsultationCreateDto createDto = null;

            // Act
            var result = await _consultationService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("数据不能为空");
        }

        [Fact]
        public async Task CreateAsync_WithRepositoryException_ShouldHandleError()
        {
            // Arrange
            var createDto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "测试主诉",
                ConsultationDate = DateTime.Now
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<ConsultationRecord>()))
                .ThrowsAsync(new Exception("数据库连接失败"));

            // Act
            var result = await _consultationService.CreateAsync(createDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("创建诊疗记录失败");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region 查询诊疗记录测试

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnRecord()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var consultation = new ConsultationRecord
            {
                Id = consultationId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "测试主诉",
                ConsultationDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(consultationId))
                .ReturnsAsync(consultation);

            // Act
            var result = await _consultationService.GetByIdAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(consultationId);
            result.Data.ChiefComplaint.Should().Be(consultation.ChiefComplaint);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdAsync(invalidId))
                .ReturnsAsync((ConsultationRecord)null);

            // Act
            var result = await _consultationService.GetByIdAsync(invalidId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("诊疗记录不存在");
        }

        [Fact]
        public async Task GetByPatientIdAsync_WithValidPatientId_ShouldReturnRecords()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var consultations = new List<ConsultationRecord>
            {
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = Guid.NewGuid(),
                    ChiefComplaint = "头痛",
                    ConsultationDate = DateTime.Now.AddDays(-7),
                    CreatedAt = DateTime.Now.AddDays(-7)
                },
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = Guid.NewGuid(),
                    ChiefComplaint = "腹痛",
                    ConsultationDate = DateTime.Now.AddDays(-3),
                    CreatedAt = DateTime.Now.AddDays(-3)
                }
            };

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(consultations);

            // Act
            var result = await _consultationService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.All(c => c.PatientId == patientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithValidCaseId_ShouldReturnRecords()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var consultations = new List<ConsultationRecord>
            {
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCaseId,
                    PatientId = Guid.NewGuid(),
                    DoctorId = Guid.NewGuid(),
                    ChiefComplaint = "咳嗽",
                    ConsultationDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                }
            };

            _repositoryMock.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(consultations);

            // Act
            var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].MedicalCaseId.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResults()
        {
            // Arrange
            var pageRequest = new PagedRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "头痛"
            };

            var consultations = new List<ConsultationRecord>
            {
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    ChiefComplaint = "头痛发热",
                    ConsultationDate = DateTime.Now
                },
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    ChiefComplaint = "头痛眩晕",
                    ConsultationDate = DateTime.Now.AddDays(-1)
                }
            };

            _repositoryMock.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((consultations, 2));

            // Act
            var result = await _consultationService.GetPagedAsync(pageRequest);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        #endregion

        #region 更新诊疗记录测试

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var updateDto = new ConsultationUpdateDto
            {
                Id = consultationId,
                ChiefComplaint = "更新后的主诉",
                PresentIllnessHistory = "更新后的现病史",
                ChineseMedicineDiagnosis = "更新后的中医诊断",
                TreatmentPrinciple = "更新后的治则治法"
            };

            var existingConsultation = new ConsultationRecord
            {
                Id = consultationId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "原始主诉",
                ConsultationDate = DateTime.Now.AddDays(-1),
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(consultationId))
                .ReturnsAsync(existingConsultation);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ConsultationRecord>()))
                .ReturnsAsync(existingConsultation);

            // Act
            var result = await _consultationService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(consultationId);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ConsultationRecord>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var updateDto = new ConsultationUpdateDto
            {
                Id = Guid.NewGuid(),
                ChiefComplaint = "更新的主诉"
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(updateDto.Id))
                .ReturnsAsync((ConsultationRecord)null);

            // Act
            var result = await _consultationService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("诊疗记录不存在");

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ConsultationRecord>()), Times.Never);
        }

        #endregion

        #region 删除诊疗记录测试

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var consultation = new ConsultationRecord
            {
                Id = consultationId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "测试主诉",
                IsDeleted = false
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(consultationId))
                .ReturnsAsync(consultation);

            _repositoryMock.Setup(x => x.DeleteAsync(consultationId))
                .ReturnsAsync(true);

            // Act
            var result = await _consultationService.DeleteAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("成功");

            _repositoryMock.Verify(x => x.DeleteAsync(consultationId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdAsync(invalidId))
                .ReturnsAsync((ConsultationRecord)null);

            // Act
            var result = await _consultationService.DeleteAsync(invalidId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("诊疗记录不存在");

            _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryFails_ShouldReturnFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var consultation = new ConsultationRecord
            {
                Id = consultationId,
                ChiefComplaint = "测试主诉"
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(consultationId))
                .ReturnsAsync(consultation);

            _repositoryMock.Setup(x => x.DeleteAsync(consultationId))
                .ReturnsAsync(false);

            // Act
            var result = await _consultationService.DeleteAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("删除诊疗记录失败");
        }

        #endregion

        #region 统计分析测试

        [Fact]
        public async Task GetStatisticsAsync_WithDateRange_ShouldReturnCorrectStats()
        {
            // Arrange
            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;
            var doctorId = Guid.NewGuid();

            var consultations = new List<ConsultationRecord>
            {
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    ConsultationDate = DateTime.Now.AddDays(-10),
                    ChineseMedicineDiagnosis = "外感风热",
                    TreatmentPrinciple = "疏风清热"
                },
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    ConsultationDate = DateTime.Now.AddDays(-5),
                    ChineseMedicineDiagnosis = "肝肾阴虚",
                    TreatmentPrinciple = "滋补肝肾"
                },
                new ConsultationRecord
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    ConsultationDate = DateTime.Now.AddDays(-2),
                    ChineseMedicineDiagnosis = "外感风热",
                    TreatmentPrinciple = "疏风清热"
                }
            };

            _repositoryMock.Setup(x => x.GetByDateRangeAsync(startDate, endDate, doctorId))
                .ReturnsAsync(consultations);

            // Act
            var result = await _consultationService.GetStatisticsAsync(startDate, endDate, doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCount.Should().Be(3);
            result.Data.DiagnosisDistribution.Should().HaveCount(2);
            result.Data.DiagnosisDistribution["外感风热"].Should().Be(2);
            result.Data.DiagnosisDistribution["肝肾阴虚"].Should().Be(1);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task CreateAsync_WithEmptyRequiredFields_ShouldReturnValidationError()
        {
            // Arrange
            var createDto = new ConsultationCreateDto
            {
                PatientId = Guid.Empty, // 无效的患者ID
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "", // 空的主诉
                ConsultationDate = DateTime.Now
            };

            // Act
            var result = await _consultationService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("验证失败");
        }

        [Fact]
        public async Task GetPagedAsync_WithNegativePageNumber_ShouldUseDefaultPage()
        {
            // Arrange
            var pageRequest = new PagedRequest
            {
                PageNumber = -1, // 无效的页码
                PageSize = 10
            };

            _repositoryMock.Setup(x => x.GetPagedAsync(
                    1, // 应该使用默认值1
                    10,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((new List<ConsultationRecord>(), 0));

            // Act
            var result = await _consultationService.GetPagedAsync(pageRequest);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.PageNumber.Should().Be(1);
        }

        [Fact]
        public async Task UpdateAsync_WithPartialData_ShouldOnlyUpdateProvidedFields()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var updateDto = new ConsultationUpdateDto
            {
                Id = consultationId,
                ChiefComplaint = "仅更新主诉"
                // 其他字段为null，不应被更新
            };

            var existingConsultation = new ConsultationRecord
            {
                Id = consultationId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                ChiefComplaint = "原始主诉",
                PresentIllnessHistory = "原始现病史",
                ChineseMedicineDiagnosis = "原始中医诊断",
                ConsultationDate = DateTime.Now.AddDays(-1)
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(consultationId))
                .ReturnsAsync(existingConsultation);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ConsultationRecord>()))
                .ReturnsAsync((ConsultationRecord c) => c);

            // Act
            var result = await _consultationService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // 验证只有ChiefComplaint被更新
            _repositoryMock.Verify(x => x.UpdateAsync(
                It.Is<ConsultationRecord>(c =>
                    c.ChiefComplaint == "仅更新主诉" &&
                    c.PresentIllnessHistory == "原始现病史" &&
                    c.ChineseMedicineDiagnosis == "原始中医诊断"
                )), Times.Once);
        }

        #endregion

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }
    }
}