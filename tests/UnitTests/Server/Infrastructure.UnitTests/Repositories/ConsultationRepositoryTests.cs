using FluentAssertions;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Repositories;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.UnitTests.Infrastructure.Repositories
{
    /// <summary>
    /// ConsultationRepository仓储层单元测试
    /// </summary>
    public class ConsultationRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ConsultationRepository _repository;

        public ConsultationRepositoryTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _repository = new ConsultationRepository(_context);
        }

        #region Shared Primary Key Tests

        [Fact]
        public async Task GetById_ShouldUseMedicalCaseId()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Status = MedicalCaseStatus.Active
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId, // 使用MedicalCaseId作为主键
                ChiefComplaint = "测试主诉",
                Diagnosis = "测试诊断"
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(medicalCaseId); // 使用MedicalCaseId查询

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCaseId.Should().Be(medicalCaseId);
            result.ChiefComplaint.Should().Be("测试主诉");
        }

        [Fact]
        public async Task Create_ShouldSharePrimaryKeyWithMedicalCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            // 先创建MedicalCase
            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // Act - 创建共享主键的Consultation
            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "新的诊疗记录"
            };

            await _repository.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Assert
            var saved = await _context.Consultations.FindAsync(medicalCaseId);
            saved.Should().NotBeNull();
            saved!.MedicalCaseId.Should().Be(medicalCaseId);
        }

        #endregion

        #region Association Data Loading Tests

        [Fact]
        public async Task GetWithPrescription_ShouldLoadRelatedData()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            var prescription = new Prescription
            {
                Id = prescriptionId,
                PrescriptionNo = "RX20250927001",
                Type = "中药饮片"
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉",
                PrescriptionId = prescriptionId
            };

            _context.Prescriptions.Add(prescription);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _repository.GetQueryable()
                .Include(c => c.Prescription)
                .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.Prescription.Should().NotBeNull();
            result.Prescription!.PrescriptionNo.Should().Be("RX20250927001");
        }

        [Fact]
        public async Task GetWithMedicalCase_ShouldLoadParentData()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseNumber = "MC20250927001"
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉"
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _repository.GetQueryable()
                .Include(c => c.MedicalCase)
                .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCase.Should().NotBeNull();
            result.MedicalCase!.CaseNumber.Should().Be("MC20250927001");
        }

        #endregion

        #region Query Performance Tests

        [Fact]
        public async Task GetByStatus_ShouldUseIndex()
        {
            // Arrange - 创建不同状态的诊疗记录
            var consultations = new[]
            {
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    Status = ConsultationStatus.Pending,
                    ChiefComplaint = "待处理1"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    Status = ConsultationStatus.InProgress,
                    ChiefComplaint = "进行中1"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    Status = ConsultationStatus.Completed,
                    ChiefComplaint = "已完成1"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    Status = ConsultationStatus.Completed,
                    ChiefComplaint = "已完成2"
                }
            };

            _context.Consultations.AddRange(consultations);
            await _context.SaveChangesAsync();

            // Act - 查询已完成的诊疗
            var results = await _repository.GetQueryable()
                .Where(c => c.Status == ConsultationStatus.Completed)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().AllSatisfy(c => c.Status.Should().Be(ConsultationStatus.Completed));
        }

        [Fact]
        public async Task GetByDateRange_ShouldReturnCorrectResults()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;

            var consultations = new[]
            {
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    CreatedAt = today,
                    ChiefComplaint = "今天"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    CreatedAt = today.AddDays(-1),
                    ChiefComplaint = "昨天"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    CreatedAt = today.AddDays(-7),
                    ChiefComplaint = "上周"
                }
            };

            _context.Consultations.AddRange(consultations);
            await _context.SaveChangesAsync();

            // Act - 查询最近3天的诊疗
            var startDate = today.AddDays(-3);
            var results = await _repository.GetQueryable()
                .Where(c => c.CreatedAt >= startDate)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().Contain(c => c.ChiefComplaint == "今天");
            results.Should().Contain(c => c.ChiefComplaint == "昨天");
            results.Should().NotContain(c => c.ChiefComplaint == "上周");
        }

        #endregion

        #region Cascade Operations Tests

        [Fact]
        public async Task Delete_WhenMedicalCaseDeleted_ShouldCascade()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "将被级联删除"
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act - 删除MedicalCase
            _context.MedicalCases.Remove(medicalCase);
            await _context.SaveChangesAsync();

            // Assert - Consultation应该被级联删除
            var deletedConsultation = await _context.Consultations.FindAsync(medicalCaseId);
            deletedConsultation.Should().BeNull("Consultation应该被级联删除");
        }

        #endregion

        #region Unique Constraint Tests

        [Fact]
        public async Task Create_DuplicateMedicalCaseId_ShouldFail()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var consultation1 = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "第一个诊疗"
            };

            _context.Consultations.Add(consultation1);
            await _context.SaveChangesAsync();

            // Act - 尝试创建相同MedicalCaseId的第二个Consultation
            var consultation2 = new Consultation
            {
                MedicalCaseId = medicalCaseId, // 相同的主键
                ChiefComplaint = "第二个诊疗"
            };

            _context.Consultations.Add(consultation2);

            // Assert
            Func<Task> act = async () => await _context.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already tracked*");
        }

        #endregion

        #region TCM Diagnosis Query Tests

        [Fact]
        public async Task SearchByTCMDiagnosis_ShouldReturnCorrectResults()
        {
            // Arrange
            var consultations = new[]
            {
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    TcmDiagnosis = "风寒感冒",
                    Syndrome = "风寒束表证",
                    TreatmentPrinciple = "疏风散寒"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    TcmDiagnosis = "风热感冒",
                    Syndrome = "风热犯表证",
                    TreatmentPrinciple = "疏风清热"
                },
                new Consultation
                {
                    MedicalCaseId = Guid.NewGuid(),
                    TcmDiagnosis = "脾胃虚寒",
                    Syndrome = "中焦虚寒证",
                    TreatmentPrinciple = "温中健脾"
                }
            };

            _context.Consultations.AddRange(consultations);
            await _context.SaveChangesAsync();

            // Act - 搜索含"风"的诊断
            var results = await _repository.GetQueryable()
                .Where(c => c.TcmDiagnosis != null && c.TcmDiagnosis.Contains("风"))
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().Contain(c => c.TcmDiagnosis == "风寒感冒");
            results.Should().Contain(c => c.TcmDiagnosis == "风热感冒");
            results.Should().NotContain(c => c.TcmDiagnosis == "脾胃虚寒");
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}