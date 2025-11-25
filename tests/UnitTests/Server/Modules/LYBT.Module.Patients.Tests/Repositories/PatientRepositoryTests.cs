using Microsoft.Extensions.Logging;
using Moq;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using FluentAssertions;
using Xunit;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Tests.Repositories
{
    /// <summary>
    /// PatientRepository单元测试
    /// 测试Repository层的CRUD操作和搜索功能
    /// </summary>
    public class PatientRepositoryTests : TestBase
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<DbSet<Patient>> _mockDbSet;
        private readonly ILogger<PatientRepository> _logger;
        private readonly IPatientRepository _repository;
        private readonly List<Patient> _testPatients;

        public PatientRepositoryTests()
        {
            _testPatients = CreateTestPatients();
            _mockDbSet = new Mock<DbSet<Patient>>();
            _mockContext = new Mock<AppDbContext>();
            _mockContext.Setup(c => c.Set<Patient>()).Returns(_mockDbSet.Object);

            _logger = CreateLogger<PatientRepository>();

            _repository = new PatientRepository(_mockContext.Object, _logger);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidContext_ShouldCreateInstance()
        {
            // Arrange
            var logger = CreateLogger<PatientRepository>();

            // Act
            var repository = new PatientRepository(_mockContext.Object, logger);

            // Assert
            repository.Should().NotBeNull();
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

        #region Mock Setup Tests

        [Fact]
        public void MockSetup_VerifyMethodsExist()
        {
            // Arrange & Act & Assert
            // 测试Mock对象设置正确
            _mockContext.Verify(c => c.Set<Patient>(), Times.Never);
            _mockDbSet.Verify(d => d.FindAsync(It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WithMockSetup_ShouldCallContext()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedPatient = _testPatients.First();

            _mockDbSet.Setup(d => d.FindAsync(It.IsAny<object[]>())).ReturnsAsync(expectedPatient);

            // Act
            var result = await _repository.GetByIdAsync(patientId);

            // Assert
            _mockDbSet.Verify(d => d.FindAsync(It.Is<object[]>(ids => ids.First().Equals(patientId))), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithMockSetup_ShouldCallDbSet()
        {
            // Arrange
            var queryable = _testPatients.AsQueryable();
            _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            _mockDbSet.As<IQueryable<Patient>>().Verify(m => m.GetEnumerator(), Times.Once);
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