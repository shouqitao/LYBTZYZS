using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Repositories;
using LYBT.Entities.Consultation;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Consultation.Tests
{
    public class ConsultationRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ConsultationRepository _repository;

        public ConsultationRepositoryTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new ConsultationRepository(_context);

            // 初始化测试数据
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            var consultations = new List<ConsultationModel>
            {
                new ConsultationModel
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    PatientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    MedicalCaseId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Diagnosis = "测试诊断1",
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now.AddDays(-5),
                    ConsultationTime = DateTime.Now.AddDays(-5)
                },
                new ConsultationModel
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    PatientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    MedicalCaseId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    Diagnosis = "测试诊断2",
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now.AddDays(-2),
                    ConsultationTime = DateTime.Now.AddDays(-2)
                },
                new ConsultationModel
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    PatientId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    UserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    MedicalCaseId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Diagnosis = "测试诊断3",
                    Status = CommonStatus.Disabled,
                    CreateTime = DateTime.Now.AddDays(-1),
                    ConsultationTime = DateTime.Now.AddDays(-1)
                }
            };

            _context.Consultations.AddRange(consultations);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnConsultation()
        {
            // Arrange
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            // Act
            var result = await _repository.GetByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.Diagnosis.Should().Be("测试诊断1");
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(id);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllConsultations()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldReturnPagedResult()
        {
            // Arrange
            int page = 1;
            int pageSize = 2;

            // Act
            var result = await _repository.GetPagedAsync(page, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(3);
            result.CurrentPage.Should().Be(page);
            result.PageSize.Should().Be(pageSize);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddNewConsultation()
        {
            // Arrange
            var newConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                Diagnosis = "新诊断",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now,
                ConsultationTime = DateTime.Now
            };

            // Act
            var result = await _repository.CreateAsync(newConsultation);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(newConsultation.Id);

            // 验证数据库中存在该记录
            var consultationInDb = await _context.Consultations.FindAsync(newConsultation.Id);
            consultationInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExistingConsultation()
        {
            // Arrange
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var consultation = await _repository.GetByIdAsync(id);
            consultation!.Diagnosis = "更新后的诊断";
            consultation.TCMDiagnosis = "中医诊断";

            // Act
            var result = await _repository.UpdateAsync(consultation);

            // Assert
            result.Should().BeTrue();

            // 验证数据库中的更新
            var updatedConsultation = await _context.Consultations.FindAsync(id);
            updatedConsultation!.Diagnosis.Should().Be("更新后的诊断");
            updatedConsultation.TCMDiagnosis.Should().Be("中医诊断");
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_ShouldDeleteConsultation()
        {
            // Arrange
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            // Act
            var result = await _repository.DeleteAsync(id);

            // Assert
            result.Should().BeTrue();

            // 验证数据库中不存在该记录
            var consultationInDb = await _context.Consultations.FindAsync(id);
            consultationInDb.Should().BeNull();
        }

        [Fact]
        public async Task GetByPatientIdAsync_ShouldReturnPatientConsultations()
        {
            // Arrange
            var patientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            // Act
            var result = await _repository.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeInDescendingOrder(c => c.CreateTime);
        }

        [Fact]
        public async Task GetByDoctorIdAsync_ShouldReturnDoctorConsultations()
        {
            // Arrange
            var doctorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            // Act
            var result = await _repository.GetByDoctorIdAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeInDescendingOrder(c => c.CreateTime);
        }

        [Fact]
        public async Task GetByDateRangeAsync_ShouldReturnConsultationsInRange()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(-4);
            var endDate = DateTime.Now.AddDays(-1);

            // Act
            var result = await _repository.GetByDateRangeAsync(startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // 只有两条记录在日期范围内
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_ShouldReturnConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            // Act
            var result = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCaseId.Should().Be(medicalCaseId);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}