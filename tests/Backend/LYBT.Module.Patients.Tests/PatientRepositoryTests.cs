using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Tests.Base;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Patients.Tests
{
    /// <summary>
    /// PatientRepository 单元测试
    /// </summary>
    public class PatientRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly PatientRepository _repository;
        private readonly string _databaseName;

        public PatientRepositoryTests()
        {
            _databaseName = $"TestDb_{Guid.NewGuid()}";
            
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .EnableSensitiveDataLogging()
                .Options;

            _context = new AppDbContext(options);
            _repository = new PatientRepository(_context);
            
            // 确保数据库已创建
            _context.Database.EnsureCreated();
        }

        #region 基础CRUD测试

        [Fact]
        public async Task AddAsync_WithValidPatient_ShouldCreatePatient()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateTestPatient("张三", "110101199001011234", "13800000001");

            // Act
            var result = await _repository.AddAsync(patient);

            // Assert
            result.Should().BeTrue();
            var patientInDb = await _context.Patients.FindAsync(patient.Id);
            patientInDb.Should().NotBeNull();
            patientInDb!.Name.Should().Be("张三");
            patientInDb.IdNumber.Should().Be("110101199001011234");
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnPatient()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateEnabledPatient();
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.GetByIdAsync(patient.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(patient.Id);
            result.Name.Should().Be(patient.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledPatientAndIncludeDisabledFalse_ShouldReturnNull()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateDisabledPatient();
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.GetByIdAsync(patient.Id, includeDisabled: false);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledPatientAndIncludeDisabledTrue_ShouldReturnPatient()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateDisabledPatient();
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.GetByIdAsync(patient.Id, includeDisabled: true);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task UpdateAsync_WithValidPatient_ShouldUpdatePatient()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateTestPatient();
            await _repository.AddAsync(patient);

            patient.Name = "更新后的姓名";
            patient.PhoneNumber = "13900000000";

            // Act
            var result = await _repository.UpdateAsync(patient);

            // Assert
            result.Should().BeTrue();
            var updatedPatient = await _context.Patients.FindAsync(patient.Id);
            updatedPatient!.Name.Should().Be("更新后的姓名");
            updatedPatient.PhoneNumber.Should().Be("13900000000");
            updatedPatient.UpdateTime.Should().NotBeNull();
        }

        #endregion

        #region 启用/禁用测试

        [Fact]
        public async Task EnableAsync_WithExistingPatient_ShouldEnablePatient()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateDisabledPatient();
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.EnableAsync(patient.Id);

            // Assert
            result.Should().BeTrue();
            var enabledPatient = await _context.Patients.FindAsync(patient.Id);
            enabledPatient!.Status.Should().Be(CommonStatus.Enabled);
            enabledPatient.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task EnableAsync_WithNonExistingPatient_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.EnableAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DisableAsync_WithExistingPatient_ShouldDisablePatient()
        {
            // Arrange
            var patient = PatientTestDataGenerator.CreateEnabledPatient();
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.DisableAsync(patient.Id);

            // Assert
            result.Should().BeTrue();
            var disabledPatient = await _context.Patients.FindAsync(patient.Id);
            disabledPatient!.Status.Should().Be(CommonStatus.Disabled);
            disabledPatient.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task DisableAsync_WithNonExistingPatient_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.DisableAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task BatchDisableAsync_WithValidIds_ShouldDisableAllPatients()
        {
            // Arrange
            var patients = PatientTestDataGenerator.CreateTestPatients(3, CommonStatus.Enabled);
            foreach (var patient in patients)
            {
                await _repository.AddAsync(patient);
            }

            var ids = patients.Select(p => p.Id).ToList();

            // Act
            var result = await _repository.BatchDisableAsync(ids);

            // Assert
            result.Should().Be(3);
            foreach (var id in ids)
            {
                var patient = await _context.Patients.FindAsync(id);
                patient!.Status.Should().Be(CommonStatus.Disabled);
                patient.UpdateTime.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task BatchEnableAsync_WithValidIds_ShouldEnableAllPatients()
        {
            // Arrange
            var patients = PatientTestDataGenerator.CreateTestPatients(3, CommonStatus.Disabled);
            foreach (var patient in patients)
            {
                await _repository.AddAsync(patient);
            }

            var ids = patients.Select(p => p.Id).ToList();

            // Act
            var result = await _repository.BatchEnableAsync(ids);

            // Assert
            result.Should().Be(3);
            foreach (var id in ids)
            {
                var patient = await _context.Patients.FindAsync(id);
                patient!.Status.Should().Be(CommonStatus.Enabled);
                patient.UpdateTime.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task BatchDisableAsync_WithEmptyIds_ShouldReturnZero()
        {
            // Act
            var result = await _repository.BatchDisableAsync(new List<Guid>());

            // Assert
            result.Should().Be(0);
        }

        #endregion

        #region 查询测试

        [Fact]
        public async Task GetListAsync_WithoutKeyword_ShouldReturnAllEnabledPatients()
        {
            // Arrange
            var enabledPatients = PatientTestDataGenerator.CreateTestPatients(3, CommonStatus.Enabled);
            var disabledPatients = PatientTestDataGenerator.CreateTestPatients(2, CommonStatus.Disabled);
            
            foreach (var patient in enabledPatients.Concat(disabledPatients))
            {
                await _repository.AddAsync(patient);
            }

            // Act
            var result = await _repository.GetListAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(p => p.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetListAsync_WithKeyword_ShouldReturnFilteredPatients()
        {
            // Arrange
            var patient1 = PatientTestDataGenerator.CreateTestPatient("张三");
            var patient2 = PatientTestDataGenerator.CreateTestPatient("李四");
            var patient3 = PatientTestDataGenerator.CreateTestPatient("张伟");
            
            await _repository.AddAsync(patient1);
            await _repository.AddAsync(patient2);
            await _repository.AddAsync(patient3);

            // Act
            var result = await _repository.GetListAsync("张");

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Name == "张三");
            result.Should().Contain(p => p.Name == "张伟");
        }

        [Fact]
        public async Task GetListAsync_WithPagination_ShouldReturnPagedResult()
        {
            // Arrange
            var patients = PatientTestDataGenerator.CreateTestPatients(10, CommonStatus.Enabled);
            foreach (var patient in patients)
            {
                await _repository.AddAsync(patient);
            }

            // Act
            var result = await _repository.GetListAsync(page: 1, pageSize: 3);

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var enabledPatients = PatientTestDataGenerator.CreateTestPatients(5, CommonStatus.Enabled);
            var disabledPatients = PatientTestDataGenerator.CreateTestPatients(3, CommonStatus.Disabled);
            
            foreach (var patient in enabledPatients.Concat(disabledPatients))
            {
                await _repository.AddAsync(patient);
            }

            // Act
            var enabledCount = await _repository.GetCountAsync(includeDisabled: false);
            var totalCount = await _repository.GetCountAsync(includeDisabled: true);

            // Assert
            enabledCount.Should().Be(5);
            totalCount.Should().Be(8);
        }

        #endregion

        #region 专门查询测试

        [Fact]
        public async Task GetByIdNumberAsync_WithExistingIdNumber_ShouldReturnPatient()
        {
            // Arrange
            var idNumber = "110101199001011234";
            var patient = PatientTestDataGenerator.CreatePatientWithIdNumber(idNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.GetByIdNumberAsync(idNumber);

            // Assert
            result.Should().NotBeNull();
            result!.IdNumber.Should().Be(idNumber);
        }

        [Fact]
        public async Task GetByIdNumberAsync_WithNonExistingIdNumber_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdNumberAsync("999999999999999999");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByPhoneNumberAsync_WithExistingPhoneNumber_ShouldReturnPatient()
        {
            // Arrange
            var phoneNumber = "13800000001";
            var patient = PatientTestDataGenerator.CreatePatientWithPhoneNumber(phoneNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.GetByPhoneNumberAsync(phoneNumber);

            // Assert
            result.Should().NotBeNull();
            result!.PhoneNumber.Should().Be(phoneNumber);
        }

        [Fact]
        public async Task GetByPhoneNumberAsync_WithNonExistingPhoneNumber_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByPhoneNumberAsync("99999999999");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region 存在性检查测试

        [Fact]
        public async Task IsIdNumberExistsAsync_WithExistingIdNumber_ShouldReturnTrue()
        {
            // Arrange
            var idNumber = "110101199001011234";
            var patient = PatientTestDataGenerator.CreatePatientWithIdNumber(idNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.IsIdNumberExistsAsync(idNumber);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsIdNumberExistsAsync_WithNonExistingIdNumber_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.IsIdNumberExistsAsync("999999999999999999");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsIdNumberExistsAsync_WithExcludeId_ShouldExcludeSpecifiedPatient()
        {
            // Arrange
            var idNumber = "110101199001011234";
            var patient = PatientTestDataGenerator.CreatePatientWithIdNumber(idNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.IsIdNumberExistsAsync(idNumber, patient.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsPhoneNumberExistsAsync_WithExistingPhoneNumber_ShouldReturnTrue()
        {
            // Arrange
            var phoneNumber = "13800000001";
            var patient = PatientTestDataGenerator.CreatePatientWithPhoneNumber(phoneNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.IsPhoneNumberExistsAsync(phoneNumber);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsPhoneNumberExistsAsync_WithNonExistingPhoneNumber_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.IsPhoneNumberExistsAsync("99999999999");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsPhoneNumberExistsAsync_WithExcludeId_ShouldExcludeSpecifiedPatient()
        {
            // Arrange
            var phoneNumber = "13800000001";
            var patient = PatientTestDataGenerator.CreatePatientWithPhoneNumber(phoneNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.IsPhoneNumberExistsAsync(phoneNumber, patient.Id);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 搜索测试

        [Fact]
        public async Task SearchAsync_WithKeyword_ShouldReturnMatchingPatients()
        {
            // Arrange
            var patient1 = PatientTestDataGenerator.CreateTestPatient("张三", phoneNumber: "13800000001");
            var patient2 = PatientTestDataGenerator.CreateTestPatient("李四", phoneNumber: "13900000002");
            
            await _repository.AddAsync(patient1);
            await _repository.AddAsync(patient2);

            // Act
            var result = await _repository.SearchAsync("张");

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("张三");
        }

        [Fact]
        public async Task ExactSearchAsync_WithPhoneNumber_ShouldReturnExactMatch()
        {
            // Arrange
            var phoneNumber = "13800000001";
            var patient = PatientTestDataGenerator.CreatePatientWithPhoneNumber(phoneNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.ExactSearchAsync(phoneNumber);

            // Assert
            result.Should().HaveCount(1);
            result.First().PhoneNumber.Should().Be(phoneNumber);
        }

        [Fact]
        public async Task ExactSearchAsync_WithIdNumber_ShouldReturnExactMatch()
        {
            // Arrange
            var idNumber = "110101199001011234";
            var patient = PatientTestDataGenerator.CreatePatientWithIdNumber(idNumber);
            await _repository.AddAsync(patient);

            // Act
            var result = await _repository.ExactSearchAsync(idNumber);

            // Assert
            result.Should().HaveCount(1);
            result.First().IdNumber.Should().Be(idNumber);
        }

        #endregion

        #region 获取患者列表测试

        [Fact]
        public async Task GetActivePatientsAsync_ShouldReturnOnlyEnabledPatients()
        {
            // Arrange
            var enabledPatients = PatientTestDataGenerator.CreateTestPatients(3, CommonStatus.Enabled);
            var disabledPatients = PatientTestDataGenerator.CreateTestPatients(2, CommonStatus.Disabled);
            
            foreach (var patient in enabledPatients.Concat(disabledPatients))
            {
                await _repository.AddAsync(patient);
            }

            // Act
            var result = await _repository.GetActivePatientsAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(p => p.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPatientsByIdNumberAsync_ShouldReturnPatientsWithSameIdNumber()
        {
            // Arrange
            var idNumber = "110101199001011234";
            var patient1 = PatientTestDataGenerator.CreatePatientWithIdNumber(idNumber);
            var patient2 = PatientTestDataGenerator.CreatePatientWithIdNumber(idNumber);
            var patient3 = PatientTestDataGenerator.CreatePatientWithIdNumber("999999199001011234");
            
            await _repository.AddAsync(patient1);
            await _repository.AddAsync(patient2);
            await _repository.AddAsync(patient3);

            // Act
            var result = await _repository.GetPatientsByIdNumberAsync(idNumber);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(p => p.IdNumber == idNumber);
        }

        [Fact]
        public async Task GetPatientsByNameAndPhoneAsync_ShouldReturnMatchingPatients()
        {
            // Arrange
            var name = "张三";
            var phoneNumber = "13800000001";
            var patient1 = PatientTestDataGenerator.CreateTestPatient(name, phoneNumber: phoneNumber);
            var patient2 = PatientTestDataGenerator.CreateTestPatient(name, phoneNumber: phoneNumber);
            var patient3 = PatientTestDataGenerator.CreateTestPatient("李四", phoneNumber: phoneNumber);
            
            await _repository.AddAsync(patient1);
            await _repository.AddAsync(patient2);
            await _repository.AddAsync(patient3);

            // Act
            var result = await _repository.GetPatientsByNameAndPhoneAsync(name, phoneNumber);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(p => p.Name == name && p.PhoneNumber == phoneNumber);
        }

        [Fact]
        public async Task GetByNameAsync_ShouldReturnPatientsWithSameName()
        {
            // Arrange
            var name = "张三";
            var patient1 = PatientTestDataGenerator.CreateTestPatient(name);
            var patient2 = PatientTestDataGenerator.CreateTestPatient(name);
            var patient3 = PatientTestDataGenerator.CreateTestPatient("李四");
            
            await _repository.AddAsync(patient1);
            await _repository.AddAsync(patient2);
            await _repository.AddAsync(patient3);

            // Act
            var result = await _repository.GetByNameAsync(name);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(p => p.Name == name);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task GetPatientsByIdNumberAsync_WithEmptyIdNumber_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetPatientsByIdNumberAsync("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPatientsByNameAndPhoneAsync_WithEmptyParameters_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetPatientsByNameAndPhoneAsync("", "");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByNameAsync_WithEmptyName_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetByNameAsync("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPatientsBySimilarNameAsync_WithEmptyName_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetPatientsBySimilarNameAsync("");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}