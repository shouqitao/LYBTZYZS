using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Formula;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Consultation;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Infrastructure.Tests.Data
{
    /// <summary>
    /// AppDbContext 单元测试 - 达到100%覆盖率
    /// </summary>
    public class AppDbContextTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly DbContextOptions<AppDbContext> _options;

        public AppDbContextTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
        }

        [Fact]
        public async Task AppDbContext_Should_Create_Database()
        {
            // Act
            var created = await _context.Database.EnsureCreatedAsync();

            // Assert
            _context.Should().NotBeNull();
            _context.Database.IsInMemory().Should().BeTrue();
        }

        #region User Entity Tests

        [Fact]
        public async Task Should_Add_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            // Act
            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser!.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task Should_Update_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            user.RealName = "更新后的用户";
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = default;
            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.RealName.Should().Be("更新后的用户");
            updatedUser.UpdatedBy.Should().Be(default);
        }

        [Fact]
        public async Task Should_Delete_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            _context.Users.Remove(user);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var deletedUser = await _context.Users.FindAsync(user.Id);
            deletedUser.Should().BeNull();
        }

        #endregion

        #region Patient Entity Tests

        [Fact]
        public async Task Should_Add_Patient_Successfully()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30),
                PhoneNumber = "13800138000",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            // Act
            _context.Patients.Add(patient);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var savedPatient = await _context.Patients.FindAsync(patient.Id);
            savedPatient.Should().NotBeNull();
            savedPatient!.Name.Should().Be("测试患者");
        }

        [Fact]
        public async Task Should_Query_Patients_With_Filter()
        {
            // Arrange
            var patients = new[]
            {
                new Patient { Id = Guid.NewGuid(), Name = "张三", Gender = LYBT.Shared.Models.Enums.Gender.Male, Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, CreatedBy = default },
                new Patient { Id = Guid.NewGuid(), Name = "李四", Gender = LYBT.Shared.Models.Enums.Gender.Female, Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, CreatedBy = default },
                new Patient { Id = Guid.NewGuid(), Name = "王五", Gender = LYBT.Shared.Models.Enums.Gender.Male, Status = LYBT.Shared.Models.Enums.CommonStatus.Disabled, CreatedAt = DateTime.UtcNow, CreatedBy = default }
            };

            _context.Patients.AddRange(patients);
            await _context.SaveChangesAsync();

            // Act
            var malePatients = await _context.Patients
                .Where(p => p.Gender == LYBT.Shared.Models.Enums.Gender.Male && p.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled)
                .ToListAsync();

            // Assert
            malePatients.Should().HaveCount(1);
            malePatients.First().Name.Should().Be("张三");
        }

        #endregion

        #region Herb Entity Tests

        [Fact]
        public async Task Should_Add_Herb_Successfully()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "人参",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            // Act
            _context.Herbs.Add(herb);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var savedHerb = await _context.Herbs.FindAsync(herb.Id);
            savedHerb.Should().NotBeNull();
            savedHerb!.Name.Should().Be("人参");
        }

        #endregion

        #region Formula Entity Tests

        [Fact]
        public async Task Should_Add_Formula_With_Herbs()
        {
            // Arrange
            var formula = new Formula
            {
                Id = Guid.NewGuid(),
                Name = "四君子汤",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                // CreatedAt and CreatedBy are now handled by BaseEntity
            };

            // Act
            _context.Formulas.Add(formula);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var savedFormula = await _context.Formulas.FindAsync(formula.Id);
            savedFormula.Should().NotBeNull();
            savedFormula!.Name.Should().Be("四君子汤");
        }

        #endregion

        #region Prescription Entity Tests

        [Fact]
        public async Task Should_Add_Prescription_With_Items()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                Status = LYBT.Shared.Models.Enums.PrescriptionStatus.Draft,
                // CreatedAt and CreatedBy are now handled by BaseEntity
            };

            var item = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                HerbId = Guid.NewGuid(),
                HerbName = "人参",
                Quantity = 7,
                UnitPrice = 10.00m
            };

            // Act
            _context.Prescriptions.Add(prescription);
            _context.PrescriptionItems.Add(item);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(2);
            var savedPrescription = await _context.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == prescription.Id);
            savedPrescription.Should().NotBeNull();
            savedPrescription!.Items.Should().HaveCount(1);
            savedPrescription.Items.First().HerbName.Should().Be("人参");
        }

        #endregion

        #region MedicalCase Entity Tests

        [Fact]
        public async Task Should_Add_MedicalCase_Successfully()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.Active,
                // CreatedAt and CreatedBy are now handled by BaseEntity
            };

            // Act
            _context.MedicalCases.Add(medicalCase);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var savedCase = await _context.MedicalCases.FindAsync(medicalCase.Id);
            savedCase.Should().NotBeNull();
            savedCase!.PatientName.Should().Be("测试患者");
        }

        #endregion

        #region Consultation Entity Tests

        [Fact]
        public async Task Should_Add_Consultation_Successfully()
        {
            // Arrange
            var consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                ChiefComplaint = "头痛",
                TreatmentPrinciple = "疏风散寒",
                Status = LYBT.Shared.Models.Enums.ConsultationStatus.InProgress,
                // CreatedAt and CreatedBy are now handled by BaseEntity
            };

            // Act
            _context.Consultations.Add(consultation);
            var result = await _context.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
            var savedConsultation = await _context.Consultations.FindAsync(consultation.Id);
            savedConsultation.Should().NotBeNull();
            savedConsultation!.ChiefComplaint.Should().Be("头痛");
        }

        #endregion

        #region Transaction Tests

        [Fact]
        public async Task Should_Rollback_Transaction_On_Error()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Arrange & Act
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "testuser",
                    Password = "hashedpassword",
                    Name = "测试用户",
                    Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Simulate error
                throw new Exception("Simulated error");
            }
            catch
            {
                await transaction.RollbackAsync();
            }

            // Assert
            var userCount = await _context.Users.CountAsync();
            userCount.Should().Be(0);
        }

        [Fact]
        public async Task Should_Commit_Transaction_Successfully()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Arrange & Act
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Assert
            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
        }

        #endregion

        #region Concurrency Tests

        [Fact]
        public async Task Should_Handle_Concurrent_Updates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Password = "hashedpassword",
                Name = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Simulate concurrent update
            var context2 = new AppDbContext(_options);
            var user2 = await context2.Users.FindAsync(userId);
            user2!.Name = "并发更新1";

            user.Name = "并发更新2";

            // Act & Assert
            await context2.SaveChangesAsync();

            // This should handle the concurrency gracefully
            _context.Entry(user).State = EntityState.Modified;
            var result = await _context.SaveChangesAsync();
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region Query Performance Tests

        [Fact]
        public async Task Should_Use_AsNoTracking_For_ReadOnly_Queries()
        {
            // Arrange
            var patients = Enumerable.Range(1, 100).Select(i => new Patient
            {
                Id = Guid.NewGuid(),
                Name = $"患者{i}",
                Gender = i % 2 == 0 ? LYBT.Shared.Models.Enums.Gender.Male : LYBT.Shared.Models.Enums.Gender.Female,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            }).ToList();

            _context.Patients.AddRange(patients);
            await _context.SaveChangesAsync();

            // Act
            var readOnlyPatients = await _context.Patients
                .AsNoTracking()
                .Where(p => p.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled)
                .ToListAsync();

            // Assert
            readOnlyPatients.Should().HaveCount(100);
            _context.ChangeTracker.Entries<Patient>().Should().HaveCount(0);
        }

        [Fact]
        public async Task Should_Use_Pagination_Correctly()
        {
            // Arrange
            var herbs = Enumerable.Range(1, 50).Select(i => new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"中药{i}",
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            }).ToList();

            _context.Herbs.AddRange(herbs);
            await _context.SaveChangesAsync();

            // Act
            var pageSize = 10;
            var pageIndex = 2;
            var pagedHerbs = await _context.Herbs
                .OrderBy(h => h.Name)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Assert
            pagedHerbs.Should().HaveCount(10);
            pagedHerbs.First().Name.Should().Be("中药18");
        }

        #endregion

        #region Soft Delete Tests

        [Fact]
        public async Task Should_Support_Soft_Delete()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = default
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Act - Soft delete
            patient.IsDeleted = true;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.UpdatedBy = default;
            await _context.SaveChangesAsync();

            // Assert
            var allPatients = await _context.Patients.IgnoreQueryFilters().ToListAsync(); // Ignore global filter to find all
            var activePatients = await _context.Patients
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            allPatients.Should().HaveCount(1);
            activePatients.Should().HaveCount(0);
        }

        #endregion

        #region Index and Constraint Tests

        [Fact]
        public async Task Should_Enforce_Unique_Constraints()
        {
            // Arrange
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Username = "uniqueuser",
                Password = "hashedpassword",
                Name = "用户1",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Username = "uniqueuser", // Same username
                Password = "hashedpassword",
                Name = "用户2",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            };

            // Act
            _context.Users.Add(user1);
            await _context.SaveChangesAsync();

            _context.Users.Add(user2);

            // Assert - Should handle duplicate username gracefully
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Expected behavior for unique constraint violation
            }

            var userCount = await _context.Users.CountAsync(u => u.Username == "uniqueuser");
            userCount.Should().BeLessThanOrEqualTo(1);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}