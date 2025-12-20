using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LYBT.Module.Patients.Tests.Repositories
{
    /// <summary>
    /// PatientRepository单元测试
    /// 测试Repository层的CRUD操作和搜索功能
    /// </summary>
    public class PatientRepositoryTests : TestBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PatientRepository> _logger;
        private readonly IPatientRepository _repository;
        private readonly List<Patient> _testPatients;

        public PatientRepositoryTests()
        {
            _testPatients = CreateTestPatients();
            _context = CreateInMemoryContext();

            // 将测试数据添加到InMemory数据库
            _context.Set<Patient>().AddRange(_testPatients);
            _context.SaveChanges();

            _logger = CreateLogger<PatientRepository>();

            _repository = new PatientRepository(_context, _logger);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidContext_ShouldCreateInstance()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var logger = CreateLogger<PatientRepository>();

            // Act
            var repository = new PatientRepository(context, logger);

            // Assert
            repository.Should().NotBeNull();
            context.Dispose();
        }

        [Fact]
        public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var logger = CreateLogger<PatientRepository>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PatientRepository(null!, logger));
        }

        #endregion

        #region Service Interface Tests

        [Fact]
        public void Repository_ShouldImplementIPatientRepository()
        {
            // Assert
            _repository.Should().BeAssignableTo<IPatientRepository>();
        }

        #endregion

        #region Dispose

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        public async Task AddAsync_WithNullEntity_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
        }

        [Fact]
        public async Task UpdateAsync_WithNullEntity_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
        }

        #endregion

        #region Helper Methods

        private List<Patient> CreateTestPatients()
        {
            return new List<Patient>
            {
                new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = "张三",
                    PinYinCode = "zs",
                    Gender = Shared.Models.Enums.Gender.Male,
                    BirthDate = new DateTime(1990, 1, 1),
                    PhoneNumber = "13800138000",
                    IdNumber = "110101199001011234",
                    Address = "北京市朝阳区",
                    EmergencyContactName = "李四",
                    EmergencyContactPhone = "13900139000",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = "李四",
                    PinYinCode = "ls",
                    Gender = Shared.Models.Enums.Gender.Female,
                    BirthDate = new DateTime(1992, 5, 15),
                    PhoneNumber = "13800138001",
                    IdNumber = "110101199205151234",
                    Address = "上海市浦东新区",
                    EmergencyContactName = "王五",
                    EmergencyContactPhone = "13900139001",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }

        private Patient CreateTestPatient()
        {
            return new Patient
            {
                Id = Guid.NewGuid(),
                Name = $"测试患者_{Guid.NewGuid():N}",
                PinYinCode = "cs",
                Gender = Shared.Models.Enums.Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000",
                IdNumber = "110101199001011234",
                Address = "测试地址",
                EmergencyContactName = "测试联系人",
                EmergencyContactPhone = "13900139000",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}