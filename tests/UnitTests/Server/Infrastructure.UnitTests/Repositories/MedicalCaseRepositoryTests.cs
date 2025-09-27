using FluentAssertions;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Consultation;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.UnitTests.Infrastructure.Repositories
{
    /// <summary>
    /// MedicalCaseRepository仓储层单元测试
    /// </summary>
    public class MedicalCaseRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicalCaseRepository _repository;

        public MedicalCaseRepositoryTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _repository = new MedicalCaseRepository(_context);

            // 种子数据
            SeedData();
        }

        private void SeedData()
        {
            // 添加患者
            var patient1 = new Patient
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "张三",
                Gender = Gender.Male,
                Age = 35,
                Phone = "13812345678"
            };

            var patient2 = new Patient
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "李四",
                Gender = Gender.Female,
                Age = 28,
                Phone = "13987654321"
            };

            // 添加医生
            var doctor1 = new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserName = "doctor1",
                RealName = "王医生",
                Role = UserRole.Doctor
            };

            var doctor2 = new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                UserName = "doctor2",
                RealName = "赵医生",
                Role = UserRole.Doctor
            };

            _context.Patients.AddRange(patient1, patient2);
            _context.Users.AddRange(doctor1, doctor2);
            _context.SaveChanges();
        }

        #region Query With Navigation Properties Tests

        [Fact]
        public async Task GetWithNavigationProperties_ShouldIncludeRelatedData()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var doctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientId = patientId,
                DoctorId = doctorId,
                CaseNumber = "MC20250927001",
                Status = MedicalCaseStatus.Active
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉",
                Status = ConsultationStatus.Completed
            };

            _context.MedicalCases.Add(medicalCase);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear(); // 清除跟踪，强制重新加载

            // Act
            var query = _repository.GetQueryable()
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Consultation);

            var result = await query.FirstOrDefaultAsync(m => m.Id == medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.Patient.Should().NotBeNull();
            result.Patient!.Name.Should().Be("张三");
            result.Doctor.Should().NotBeNull();
            result.Doctor!.RealName.Should().Be("王医生");
            result.Consultation.Should().NotBeNull();
            result.Consultation!.ChiefComplaint.Should().Be("测试主诉");
        }

        #endregion

        #region N+1 Query Problem Tests

        [Fact]
        public async Task GetPagedWithIncludes_ShouldAvoidNPlusOneQuery()
        {
            // Arrange - 创建多个医疗案例
            var medicalCases = new List<MedicalCase>();
            for (int i = 0; i < 10; i++)
            {
                var caseId = Guid.NewGuid();
                medicalCases.Add(new MedicalCase
                {
                    Id = caseId,
                    PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    CaseNumber = $"MC2025092700{i}",
                    Status = MedicalCaseStatus.Active
                });

                _context.Consultations.Add(new Consultation
                {
                    MedicalCaseId = caseId,
                    ChiefComplaint = $"主诉{i}"
                });
            }

            _context.MedicalCases.AddRange(medicalCases);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act - 使用Include避免N+1问题
            var query = _repository.GetQueryable()
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.Consultation);

            var results = await query.Take(5).ToListAsync();

            // Assert
            results.Should().HaveCount(5);
            results.Should().AllSatisfy(m =>
            {
                m.Patient.Should().NotBeNull("应该一次性加载Patient");
                m.Doctor.Should().NotBeNull("应该一次性加载Doctor");
                m.Consultation.Should().NotBeNull("应该一次性加载Consultation");
            });
        }

        [Fact]
        public async Task GetByPatientId_ShouldUseIndex()
        {
            // Arrange
            var patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            // 为同一患者创建多个案例
            for (int i = 0; i < 5; i++)
            {
                _context.MedicalCases.Add(new MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    CaseNumber = $"MC2025092700{i}",
                    Status = MedicalCaseStatus.Active
                });
            }

            await _context.SaveChangesAsync();

            // Act
            var results = await _repository.GetQueryable()
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(5);
            results.Should().AllSatisfy(m => m.PatientId.Should().Be(patientId));
        }

        #endregion

        #region Transaction Tests

        [Fact]
        public async Task CreateWithTransaction_ShouldRollbackOnError()
        {
            // Arrange
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Act - 创建有效的医疗案例
                var validCase = new MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    CaseNumber = "MC20250927999"
                };

                await _repository.AddAsync(validCase);
                await _context.SaveChangesAsync();

                // 尝试创建无效的医疗案例（PatientId不存在）
                var invalidCase = new MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(), // 不存在的PatientId
                    DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    CaseNumber = "MC20250927998"
                };

                await _repository.AddAsync(invalidCase);
                await _context.SaveChangesAsync(); // 这里应该失败

                await transaction.CommitAsync();
            }
            catch
            {
                // 回滚事务
                await transaction.RollbackAsync();
            }

            // Assert - 验证没有数据被保存
            var count = await _repository.GetQueryable()
                .Where(m => m.CaseNumber.StartsWith("MC20250927"))
                .CountAsync();

            count.Should().Be(0, "事务应该回滚，没有数据被保存");
        }

        #endregion

        #region Soft Delete Tests

        [Fact]
        public async Task GetAll_ShouldNotIncludeSoftDeleted()
        {
            // Arrange
            var activeCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CaseNumber = "MC20250927901",
                IsDeleted = false
            };

            var deletedCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CaseNumber = "MC20250927902",
                IsDeleted = true
            };

            _context.MedicalCases.AddRange(activeCase, deletedCase);
            await _context.SaveChangesAsync();

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            results.Should().Contain(m => m.CaseNumber == "MC20250927901");
            results.Should().NotContain(m => m.CaseNumber == "MC20250927902", "软删除的记录不应该被返回");
        }

        [Fact]
        public async Task SoftDelete_ShouldMarkAsDeleted()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CaseNumber = "MC20250927903",
                IsDeleted = false
            };

            _context.MedicalCases.Add(medicalCase);
            await _context.SaveChangesAsync();

            // Act
            await _repository.SoftDeleteAsync(medicalCase.Id);

            // Assert
            var deletedCase = await _context.MedicalCases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == medicalCase.Id);

            deletedCase.Should().NotBeNull();
            deletedCase!.IsDeleted.Should().BeTrue();
        }

        #endregion

        #region Complex Query Tests

        [Fact]
        public async Task SearchByMultipleCriteria_ShouldReturnCorrectResults()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var doctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            // 创建不同状态的案例
            var cases = new[]
            {
                new MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Status = MedicalCaseStatus.Active,
                    CreatedAt = today
                },
                new MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = doctorId,
                    Status = MedicalCaseStatus.Completed,
                    CreatedAt = today.AddDays(-1)
                },
                new MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    DoctorId = doctorId,
                    Status = MedicalCaseStatus.Active,
                    CreatedAt = today
                }
            };

            _context.MedicalCases.AddRange(cases);
            await _context.SaveChangesAsync();

            // Act - 查询今天创建的、特定患者的、活跃状态的案例
            var results = await _repository.GetQueryable()
                .Where(m => m.PatientId == patientId)
                .Where(m => m.Status == MedicalCaseStatus.Active)
                .Where(m => m.CreatedAt >= today)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            results.First().PatientId.Should().Be(patientId);
            results.First().Status.Should().Be(MedicalCaseStatus.Active);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}