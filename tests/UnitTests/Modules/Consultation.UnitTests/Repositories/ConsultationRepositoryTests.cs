using FluentAssertions;
using LYBT.Infrastructure.Data;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using LYBT.Module.Consultation.Repositories;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Tests.Repositories
{
    /// <summary>
    /// ConsultationRepository 单元测试
    /// </summary>
    public class ConsultationRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ConsultationRepository _repository;

        public ConsultationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"DataSource=:memory:")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();
            _repository = new ConsultationRepository(_context);
        }

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_WithValidPatientId_ShouldReturnConsultations()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            var consultation = new ConsultationEntity
            {
                Id = medicalCase.Id, // 共享主键
                ChiefComplaint = "头痛",
                TCMDiagnosis = "风寒感冒",
                MedicalCase = medicalCase,
                IsDeleted = false
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].ChiefComplaint.Should().Be("头痛");
            result[0].MedicalCase.Should().NotBeNull();
            result[0].MedicalCase.PatientName.Should().Be("测试患者");
        }

        [Fact]
        public async Task GetByPatientIdAsync_WithNoConsultations_ShouldReturnEmptyList()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByPatientIdAsync_ShouldExcludeDeletedConsultations()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            var deletedConsultation = new ConsultationEntity
            {
                Id = medicalCase.Id,
                ChiefComplaint = "已删除",
                MedicalCase = medicalCase,
                IsDeleted = true
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(deletedConsultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByPatientIdAsync_ShouldOrderByCreatedAtDescending()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            var medicalCase1 = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "患者",
                DoctorName = "医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = MedicalCaseStatus.Closed  // 避免 UNIQUE 约束冲突
            };
            var consultation1 = new ConsultationEntity
            {
                Id = medicalCase1.Id,
                ChiefComplaint = "第一次",
                MedicalCase = medicalCase1
            };

            var medicalCase2 = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "患者",
                DoctorName = "医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation2 = new ConsultationEntity
            {
                Id = medicalCase2.Id,
                ChiefComplaint = "第二次",
                MedicalCase = medicalCase2
            };

            await _context.MedicalCases.AddRangeAsync(medicalCase1, medicalCase2);
            await _context.Consultations.AddRangeAsync(consultation1, consultation2);
            await _context.SaveChangesAsync();

            // AppDbContext 会自动设置 CreatedAt，需要手动更新以测试排序
            var olderDate = DateTime.Now.AddDays(-2);
            var newerDate = DateTime.Now;
            await _context.Consultations
                .Where(c => c.Id == consultation1.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.CreatedAt, olderDate));
            await _context.Consultations
                .Where(c => c.Id == consultation2.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.CreatedAt, newerDate));

            // Act
            var result = await _repository.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().HaveCount(2);
            result[0].ChiefComplaint.Should().Be("第二次");
            result[1].ChiefComplaint.Should().Be("第一次");
        }

        #endregion

        #region GetPagedWithDetailsAsync Tests

        [Fact]
        public async Task GetPagedWithDetailsAsync_WithDefaultParameters_ShouldReturnPaged()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            var consultation = new ConsultationEntity
            {
                Id = medicalCase.Id,
                ChiefComplaint = "咳嗽",
                TCMDiagnosis = "肺热",
                MedicalCase = medicalCase
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedWithDetailsAsync(1, 20);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.Items[0].MedicalCase.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPagedWithDetailsAsync_WithKeyword_ShouldFilterByChiefComplaint()
        {
            // Arrange
            var medicalCase1 = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者1",
                DoctorName = "医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation1 = new ConsultationEntity
            {
                Id = medicalCase1.Id,
                ChiefComplaint = "头痛发热",
                MedicalCase = medicalCase1
            };

            var medicalCase2 = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者2",
                DoctorName = "医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation2 = new ConsultationEntity
            {
                Id = medicalCase2.Id,
                ChiefComplaint = "咳嗽",
                MedicalCase = medicalCase2
            };

            await _context.MedicalCases.AddRangeAsync(medicalCase1, medicalCase2);
            await _context.Consultations.AddRangeAsync(consultation1, consultation2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedWithDetailsAsync(1, 20, "头痛");

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].ChiefComplaint.Should().Contain("头痛");
        }

        [Fact]
        public async Task GetPagedWithDetailsAsync_WithKeyword_ShouldFilterByPatientName()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张小明",
                DoctorName = "李医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation = new ConsultationEntity
            {
                Id = medicalCase.Id,
                ChiefComplaint = "咳嗽",
                MedicalCase = medicalCase
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedWithDetailsAsync(1, 20, "张小明");

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].MedicalCase.PatientName.Should().Contain("张小明");
        }

        [Fact]
        public async Task GetPagedWithDetailsAsync_ShouldExcludeDeleted()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var deletedConsultation = new ConsultationEntity
            {
                Id = medicalCase.Id,
                ChiefComplaint = "已删除",
                MedicalCase = medicalCase,
                IsDeleted = true
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(deletedConsultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedWithDetailsAsync(1, 20);

            // Assert
            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPagedWithDetailsAsync_ShouldRespectPagination()
        {
            // Arrange
            for (int i = 0; i < 25; i++)
            {
                var medicalCase = new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = $"患者{i}",
                    DoctorName = "医生",
                    CreatedBy = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
                var consultation = new ConsultationEntity
                {
                    Id = medicalCase.Id,
                    ChiefComplaint = $"主诉{i}",
                    MedicalCase = medicalCase
                };

                await _context.MedicalCases.AddAsync(medicalCase);
                await _context.Consultations.AddAsync(consultation);
            }
            await _context.SaveChangesAsync();

            // Act - 第1页，每页10条
            var page1 = await _repository.GetPagedWithDetailsAsync(1, 10);
            var page2 = await _repository.GetPagedWithDetailsAsync(2, 10);

            // Assert
            page1.TotalCount.Should().Be(25);
            page1.Items.Should().HaveCount(10);
            page1.CurrentPage.Should().Be(1);

            page2.TotalCount.Should().Be(25);
            page2.Items.Should().HaveCount(10);
            page2.CurrentPage.Should().Be(2);
        }

        #endregion

        #region GetByIdWithDetailsAsync Tests

        [Fact]
        public async Task GetByIdWithDetailsAsync_WithValidId_ShouldReturnConsultation()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation = new ConsultationEntity
            {
                Id = medicalCase.Id,
                ChiefComplaint = "主诉",
                TCMDiagnosis = "诊断",
                MedicalCase = medicalCase
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdWithDetailsAsync(consultation.Id);

            // Assert
            result.Should().NotBeNull();
            result.ChiefComplaint.Should().Be("主诉");
            result.MedicalCase.Should().NotBeNull();
            result.MedicalCase.PatientName.Should().Be("测试患者");
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdWithDetailsAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_WithDeletedConsultation_ShouldReturnNull()
        {
            // Arrange
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation = new ConsultationEntity
            {
                Id = medicalCase.Id,
                ChiefComplaint = "已删除",
                MedicalCase = medicalCase,
                IsDeleted = true
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdWithDetailsAsync(consultation.Id);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetByMedicalCaseIdAsync Tests

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithValidId_ShouldReturnConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            var consultation = new ConsultationEntity
            {
                Id = medicalCaseId,
                ChiefComplaint = "主诉",
                MedicalCase = medicalCase
            };

            await _context.MedicalCases.AddAsync(medicalCase);
            await _context.Consultations.AddAsync(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.ChiefComplaint.Should().Be("主诉");
            result.MedicalCase.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByMedicalCaseIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        public void Dispose()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }
    }
}
