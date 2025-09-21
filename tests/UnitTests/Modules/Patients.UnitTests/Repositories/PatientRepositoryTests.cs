using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Module.Patients.Tests.Repositories
{
    public class PatientRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<PatientRepository>> _mockLogger;
        private readonly IMemoryCache _realCache;
        private readonly PatientRepository _repository;

        public PatientRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _mockLogger = new Mock<ILogger<PatientRepository>>();
            _realCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
            _repository = new PatientRepository(_context, _mockLogger.Object, _realCache);
        }

        private Patient CreateTestPatient(string name = "测试患者", string phoneNumber = "13800000000", Gender gender = Gender.Male)
        {
            return new Patient
            {
                Id = Guid.NewGuid(),
                Name = name,
                PhoneNumber = phoneNumber,
                Gender = gender,
                Age = 30,
                Address = "测试地址",
                IdCard = "310101199001011234",
                EmergencyContact = "紧急联系人",
                EmergencyPhone = "13900000000",
                Allergies = "无过敏史",
                MedicalHistory = "无重大疾病史"
            };
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _repository.Should().NotBeNull();
            _repository.Should().BeAssignableTo<IPatientRepository>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_ContextIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PatientRepository(null!, _mockLogger.Object, _realCache));
            exception.ParamName.Should().Be("context");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PatientRepository(_context, null!, _realCache));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_CacheIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PatientRepository(_context, _mockLogger.Object, null!));
            exception.ParamName.Should().Be("cache");
        }

        #endregion

        #region 基础CRUD测试

        [Fact]
        public async Task AddAsync_Should_AddPatient_When_ValidPatientProvided()
        {
            // Arrange
            var patient = CreateTestPatient("新患者", "13800000001");

            // Act
            var result = await _repository.AddAsync(patient);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("新患者");

            var savedPatient = await _context.Patients.FindAsync(patient.Id);
            savedPatient.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnPatient_When_PatientExists()
        {
            // Arrange
            var patient = CreateTestPatient("查询患者", "13800000002");
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(patient.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(patient.Id);
            result.Name.Should().Be("查询患者");
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdatePatient_When_ValidPatientProvided()
        {
            // Arrange
            var patient = CreateTestPatient("原始患者", "13800000003");
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();
            _context.Entry(patient).State = EntityState.Detached;

            patient.Name = "更新患者";

            // Act
            var result = await _repository.UpdateAsync(patient);
            await _repository.SaveChangesAsync();

            // Assert
            result.Name.Should().Be("更新患者");

            var updatedPatient = await _context.Patients.FindAsync(patient.Id);
            updatedPatient!.Name.Should().Be("更新患者");
        }

        [Fact]
        public async Task DeleteAsync_Should_DeletePatient_When_ValidPatientProvided()
        {
            // Arrange
            var patient = CreateTestPatient("删除患者", "13800000004");
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteAsync(patient);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().BeTrue();

            var deletedPatient = await _context.Patients.FindAsync(patient.Id);
            deletedPatient.Should().BeNull();
        }

        #endregion

        #region 分页查询测试

        [Fact]
        public async Task GetPagedAsync_Should_ReturnPagedResult_When_PatientsExist()
        {
            // Arrange
            var patients = Enumerable.Range(1, 5)
                .Select(i => CreateTestPatient($"患者{i}", $"1380000000{i}"))
                .ToArray();
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(null, 1, 3);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(3);
        }

        [Fact]
        public async Task GetPagedAsync_Should_ApplyPredicate_When_PredicateProvided()
        {
            // Arrange
            var patients = new[]
            {
                CreateTestPatient("张三", "13800000001", Gender.Male),
                CreateTestPatient("李四", "13800000002", Gender.Female),
                CreateTestPatient("王五", "13800000003", Gender.Male)
            };
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(p => p.Gender == Gender.Male, 1, 10);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Items.Should().OnlyContain(p => p.Gender == Gender.Male);
        }

        #endregion

        #region 查询方法测试

        [Fact]
        public async Task FindAsync_Should_ReturnMatchingPatients_When_PredicateMatches()
        {
            // Arrange
            var patients = new[]
            {
                CreateTestPatient("张三", "13800000001"),
                CreateTestPatient("李四", "13800000002"),
                CreateTestPatient("张五", "13800000003")
            };
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FindAsync(p => p.Name.Contains("张"));

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(p => p.Name.Contains("张"));
        }

        [Fact]
        public async Task GetSingleAsync_Should_ReturnPatient_When_PatientMatches()
        {
            // Arrange
            var patient = CreateTestPatient("唯一患者", "13800000001");
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetSingleAsync(p => p.PhoneNumber == "13800000001");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("唯一患者");
        }

        [Fact]
        public async Task ExistsAsync_Should_ReturnTrue_When_PatientExists()
        {
            // Arrange
            var patient = CreateTestPatient("存在患者", "13800000001");
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(patient.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CountAsync_Should_ReturnCorrectCount_When_PatientsExist()
        {
            // Arrange
            var patients = new[]
            {
                CreateTestPatient("患者1", "13800000001", Gender.Male),
                CreateTestPatient("患者2", "13800000002", Gender.Female),
                CreateTestPatient("患者3", "13800000003", Gender.Male)
            };
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var totalCount = await _repository.CountAsync();
            var maleCount = await _repository.CountAsync(p => p.Gender == Gender.Male);

            // Assert
            totalCount.Should().Be(3);
            maleCount.Should().Be(2);
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task AddRangeAsync_Should_AddAllPatients_When_ValidPatientsProvided()
        {
            // Arrange
            var patients = new[]
            {
                CreateTestPatient("患者1", "13800000001"),
                CreateTestPatient("患者2", "13800000002"),
                CreateTestPatient("患者3", "13800000003")
            };

            // Act
            var result = await _repository.AddRangeAsync(patients);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().HaveCount(3);

            var savedPatients = await _context.Patients.ToListAsync();
            savedPatients.Should().HaveCount(3);
        }

        [Fact]
        public async Task DeleteRangeAsync_Should_DeleteAllPatients_When_ValidPatientsProvided()
        {
            // Arrange
            var patients = new[]
            {
                CreateTestPatient("删除1", "13800000001"),
                CreateTestPatient("删除2", "13800000002")
            };
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteRangeAsync(patients);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().Be(2);

            var remainingPatients = await _context.Patients.ToListAsync();
            remainingPatients.Should().BeEmpty();
        }

        #endregion

        #region 错误处理测试

        [Fact]
        public async Task AddAsync_Should_ThrowArgumentNullException_When_PatientIsNull()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.AddAsync(null!));
            exception.ParamName.Should().Be("entity");
        }

        [Fact]
        public async Task UpdateAsync_Should_ThrowArgumentNullException_When_PatientIsNull()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.UpdateAsync(null!));
            exception.ParamName.Should().Be("entity");
        }

        [Fact]
        public async Task DeleteAsync_Should_ReturnFalse_When_PatientIsNull()
        {
            // Act
            var result = await _repository.DeleteAsync((Patient)null!);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 性能和缓存测试

        [Fact]
        public async Task GetByIdAsync_Should_UseCache_When_PatientCached()
        {
            // Arrange
            var patient = CreateTestPatient("缓存患者", "13800000001");
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetByIdAsync(patient.Id);
            var result2 = await _repository.GetByIdAsync(patient.Id);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!.Name.Should().Be(result2!.Name);
        }

        [Fact]
        public async Task GetAllAsync_Should_ReturnAllPatients_When_PatientsExist()
        {
            // Arrange
            var patients = new[]
            {
                CreateTestPatient("患者1", "13800000001"),
                CreateTestPatient("患者2", "13800000002")
            };
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        #endregion

        #region 流式处理测试

        [Fact]
        public async Task GetAllStreamAsync_Should_StreamAllPatients_When_PatientsExist()
        {
            // Arrange
            var patients = Enumerable.Range(1, 5)
                .Select(i => CreateTestPatient($"流式患者{i}", $"1380000000{i}"))
                .ToArray();
            await _context.Patients.AddRangeAsync(patients);
            await _context.SaveChangesAsync();

            // Act
            var results = new List<Patient>();
            await foreach (var patient in _repository.GetAllStreamAsync())
            {
                results.Add(patient);
            }

            // Assert
            results.Should().HaveCount(5);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _realCache?.Dispose();
        }
    }

    // 集成测试类
    public class PatientRepositoryIntegrationTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly PatientRepository _repository;
        private readonly IMemoryCache _cache;

        public PatientRepositoryIntegrationTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            var logger = new Mock<ILogger<PatientRepository>>();
            _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
            _repository = new PatientRepository(_context, logger.Object, _cache);
        }

        [Fact]
        public async Task PatientRepository_Should_WorkWithCompleteLifecycle_When_RealScenario()
        {
            // Arrange - 创建患者
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "集成测试患者",
                PhoneNumber = "13800000000",
                Gender = Gender.Male,
                Age = 35,
                Address = "上海市浦东新区",
                IdCard = "310115198801011234",
                EmergencyContact = "家属姓名",
                EmergencyPhone = "13900000000",
                Allergies = "青霉素过敏",
                MedicalHistory = "高血压病史5年"
            };

            // Act & Assert - 模拟完整患者管理流程
            // 1. 添加患者
            await _repository.AddAsync(patient);
            await _repository.SaveChangesAsync();

            // 2. 查询患者
            var foundPatient = await _repository.GetByIdAsync(patient.Id);
            foundPatient.Should().NotBeNull();
            foundPatient!.Name.Should().Be("集成测试患者");

            // 3. 更新患者信息
            foundPatient.Address = "上海市黄浦区";
            foundPatient.Age = 36;
            await _repository.UpdateAsync(foundPatient);
            await _repository.SaveChangesAsync();

            // 4. 验证更新
            var updatedPatient = await _repository.GetByIdAsync(patient.Id);
            updatedPatient!.Address.Should().Be("上海市黄浦区");
            updatedPatient.Age.Should().Be(36);

            // 5. 搜索患者
            var searchResults = await _repository.FindAsync(p => p.Name.Contains("集成"));
            searchResults.Should().HaveCount(1);

            // 6. 分页查询
            var pagedResult = await _repository.GetPagedAsync(null, 1, 10);
            pagedResult.Items.Should().HaveCount(1);
            pagedResult.TotalCount.Should().Be(1);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _cache?.Dispose();
        }
    }
}