using FluentAssertions;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.UnitTests.Core.Services
{
    /// <summary>
    /// ConsultationService服务层单元测试
    /// </summary>
    public class ConsultationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<ConsultationBusinessService>> _loggerMock;
        private readonly ConsultationBusinessService _service;

        public ConsultationServiceTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _loggerMock = new Mock<ILogger<ConsultationBusinessService>>();

            // 创建服务实例
            var repository = new ConsultationRepository(_context);
            _service = new ConsultationBusinessService(repository, _loggerMock.Object);
        }

        #region Get Through MedicalCase Tests

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public async Task GetByMedicalCaseId_ShouldReturnConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            // 先创建MedicalCase
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Status = MedicalCaseStatus.Active
            };

            // 创建关联的Consultation
            var consultation = new Consultation
            {
<<<<<<< HEAD
                MedicalCaseId = medicalCaseId,
=======
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                UserId = createDto.UserId,
                MedicalCaseId = createDto.MedicalCaseId,
                ChiefComplaint = createDto.ChiefComplaint,
                TCMDiagnosis = createDto.TCMDiagnosis ?? "测试诊断",
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<ConsultationEntity>()))
                .ReturnsAsync(consultation);

            // Act
            var result = await _consultationService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.PatientId.Should().Be(createDto.PatientId);
            result.Data.ChiefComplaint.Should().Be(createDto.ChiefComplaint);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ConsultationEntity>()), Times.Once);
        }
        */

        [Fact]
        public async Task CreateAsync_WithNullData_ShouldReturnFailure()
        {
            // Arrange
            ConsultationCreateDto createDto = null;

            // Act
            var result = await _consultationService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("数据不能为空");
        }

        #endregion

        #region 查询诊疗记录测试

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnRecord()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var consultation = new ConsultationEntity
            {
                Id = consultationId,
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
>>>>>>> feature/medical-case-aggregate-root
                ChiefComplaint = "测试主诉",
                PresentIllness = "测试现病史",
                Diagnosis = "测试诊断",
                Status = ConsultationStatus.Completed
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCaseId.Should().Be(medicalCaseId);
            result.ChiefComplaint.Should().Be("测试主诉");
            result.Diagnosis.Should().Be("测试诊断");
        }
        */

        [Fact]
        public async Task GetByMedicalCaseId_ShouldReturnNull_WhenNoConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            // 只创建MedicalCase，不创建Consultation
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().BeNull("没有关联的Consultation");
        }

        #endregion

        #region Cascade Update Tests

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public async Task UpdateConsultation_ShouldNotAffectMedicalCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Remark = "原始备注",
                Status = MedicalCaseStatus.Active
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "原始主诉",
                Status = ConsultationStatus.InProgress
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var updateDto = new ConsultationUpdateDto
            {
                ChiefComplaint = "更新后的主诉",
                Diagnosis = "新的诊断"
            };

            var result = await _service.UpdateAsync(medicalCaseId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.ChiefComplaint.Should().Be("更新后的主诉");

            // 验证MedicalCase没有被影响
            var unchangedCase = await _context.MedicalCases.FindAsync(medicalCaseId);
            unchangedCase!.Remark.Should().Be("原始备注");
        }
        */

        #endregion

        #region Soft Delete Tests

        /* // 暂时注释掉，等待Consultation聚合根重构完成
        [Fact]
        public async Task SoftDelete_ShouldMarkAsDeleted()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉",
                IsDeleted = false
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            await _service.DeleteAsync(medicalCaseId);

            // Assert
            var deletedConsultation = await _context.Consultations
                .IgnoreQueryFilters() // 忽略软删除过滤器
                .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);

            deletedConsultation.Should().NotBeNull();
            deletedConsultation!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task GetByMedicalCaseId_ShouldNotReturnSoftDeleted()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "已删除的诊疗记录",
                IsDeleted = true
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().BeNull("软删除的记录不应该被返回");
        }
        */

        #endregion

        #region Status Transition Tests

        [Fact]
        public async Task CompleteConsultation_ShouldUpdateStatusAndTimestamp()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                Status = ConsultationStatus.InProgress,
                ChiefComplaint = "测试主诉"
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CompleteConsultationAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(ConsultationStatus.Completed);
            result.CompletedAt.Should().NotBeNull();
            result.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CompleteConsultation_ShouldFailForAlreadyCompleted()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var completedTime = DateTime.UtcNow.AddHours(-1);
            
            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                Status = ConsultationStatus.Completed,
                CompletedAt = completedTime
            };

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _service.CompleteConsultationAsync(medicalCaseId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*已完成*");
        }

        #endregion

        #region TCM Diagnosis Tests

        [Fact]
        public async Task CreateWithTCMDiagnosis_ShouldSaveAllFields()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "头痛发热",
                // 四诊信息
                Inspection = "面色红润，舌淡红，苔薄白",
                Auscultation = "声音洪亮",
                Inquiry = "睡眠欠佳，纳食可",
                Palpation = "脉浮数",
                // 中医诊断
                TcmDiagnosis = "外感风寒",
                Syndrome = "风寒束表证",
                TreatmentPrinciple = "疏风散寒，宣肺解表"
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result!.Inspection.Should().Contain("舌淡红");
            result.Auscultation.Should().Contain("洪亮");
            result.Inquiry.Should().Contain("睡眠");
            result.Palpation.Should().Contain("脉浮数");
            result.TcmDiagnosis.Should().Be("外感风寒");
            result.Syndrome.Should().Contain("风寒");
            result.TreatmentPrinciple.Should().Contain("疏风散寒");
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}