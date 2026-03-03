using FluentAssertions;
using FluentValidation;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace LYBT.Tests.Unit.Modules.Patients.Services
{
    /// <summary>
    /// 患者服务单元测试
    /// 测试患者CRUD操作及搜索功能的所有场景
    /// </summary>
    public class PatientServiceTests : TestBase
    {
        private readonly PatientService _patientService;
        private readonly IPatientRepository _repositoryMock;
        private readonly ILogger<PatientService> _loggerMock;
        private readonly IValidator<PatientInputDto> _validatorMock;
        private readonly AppDbContext _dbContext;
        private readonly ICacheInvalidationService _cacheInvalidationMock;
        private readonly IPatientImportExportService _importExportMock;

        public PatientServiceTests()
        {
            _repositoryMock = CreateMock<IPatientRepository>();
            _loggerMock = CreateLoggerMock<PatientService>();
            _validatorMock = CreateMock<IValidator<PatientInputDto>>();
            _cacheInvalidationMock = CreateMock<ICacheInvalidationService>();
            _importExportMock = CreateMock<IPatientImportExportService>();

            // 默认validator返回验证成功
            _validatorMock
                .ValidateAsync(Arg.Any<PatientInputDto>(), Arg.Any<CancellationToken>())
                .Returns(new FluentValidation.Results.ValidationResult());

            // 使用 InMemory SQLite 创建真实 DbContext
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"DataSource=:memory:")
                .Options;
            _dbContext = new AppDbContext(options);
            _dbContext.Database.OpenConnection();
            _dbContext.Database.EnsureCreated();

            // 创建PatientService实例（Mapperly迁移后，Service内部使用私有Mapper）
            _patientService = new PatientService(
                _repositoryMock,
                _loggerMock,
                _validatorMock,
                _dbContext,
                _cacheInvalidationMock,
                _importExportMock);
        }

        public override void Dispose()
        {
            _dbContext.Database.CloseConnection();
            _dbContext.Dispose();
            base.Dispose();
        }



        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var patients = CreateTestPatients(5);
            var pagedResult = new PagedResult<Patient>
            {
                Items = patients,
                TotalCount = 5,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .GetPagedAsync(1, 20, Arg.Any<string?>())
                .Returns(pagedResult);

            // Act
            var result = await _patientService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(5);
            result.Data!.TotalCount.Should().Be(5);
            result.Data!.CurrentPage.Should().Be(1);
            result.Data!.PageSize.Should().Be(20);

            await _repositoryMock.Received(1).GetPagedAsync(1, 20, Arg.Any<string?>());
        }

        [Fact]
        public async Task GetPagedAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var exception = new Exception("数据库错误");
            _repositoryMock
                .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.GetPagedAsync(1, 20, null));

            thrownException.Message.Should().Be("数据库错误");
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<Patient>
            {
                Items = new List<Patient>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .GetPagedAsync(1, 20, Arg.Any<string?>())
                .Returns(pagedResult);

            // Act
            var result = await _patientService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().BeEmpty();
            result.Data!.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithExistingPatient_ShouldReturnPatient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = CreateTestPatient(patientId);

            _repositoryMock
                .GetByIdAsync(patientId)
                .Returns(patient);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(patientId);
            result.Data!.Name.Should().Be(patient.Name);
            result.Data!.PhoneNumber.Should().Be(patient.PhoneNumber);

            await _repositoryMock.Received(1).GetByIdAsync(patientId);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingPatient_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock
                .GetByIdAsync(patientId)
                .Returns((Patient?)null);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者信息不存在");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var patientId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock
                .GetByIdAsync(patientId)
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.GetByIdAsync(patientId));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreatePatient()
        {
            // Arrange
            var createDto = new PatientInputDto
            {
                Name = "张三",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000",
                IdNumber = "110101199001011234",
                Address = "北京市朝阳区",
                EmergencyContactName = "李四",
                EmergencyContactPhone = "13900139000"
            };

            var createdPatient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Gender = createDto.Gender,
                BirthDate = createDto.BirthDate,
                PhoneNumber = createDto.PhoneNumber,
                IdNumber = createDto.IdNumber,
                Address = createDto.Address,
                EmergencyContactName = createDto.EmergencyContactName,
                EmergencyContactPhone = createDto.EmergencyContactPhone,
                CreatedAt = DateTime.UtcNow
            };

            _repositoryMock
                .AddAsync(Arg.Any<Patient>())
                .Returns(createdPatient);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(createDto.Name);
            result.Data!.PhoneNumber.Should().Be(createDto.PhoneNumber);
            result.Data!.IdNumber.Should().Be(createDto.IdNumber);

            await _repositoryMock.Received(1).AddAsync(Arg.Any<Patient>());
        }

        [Fact]
        public async Task CreateAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var createDto = new PatientInputDto
            {
                Name = "张三",
                Gender = Gender.Male,
                PhoneNumber = "13800138000"
            };

            var exception = new Exception("数据库错误");

            _repositoryMock
                .AddAsync(Arg.Any<Patient>())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.CreateAsync(createDto));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_WithExistingPatient_ShouldUpdatePatient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var existingPatient = CreateTestPatient(patientId);

            var updateDto = new PatientInputDto
            {
                Name = "更新的姓名",
                PhoneNumber = "13900139000",
                Address = "更新的地址",
                EmergencyContactName = "更新的紧急联系人",
                EmergencyContactPhone = "13700137000"
            };

            var updatedPatient = new Patient
            {
                Id = patientId,
                Name = updateDto.Name,
                Gender = existingPatient.Gender,
                BirthDate = existingPatient.BirthDate,
                PhoneNumber = updateDto.PhoneNumber,
                IdNumber = existingPatient.IdNumber,
                Address = updateDto.Address,
                EmergencyContactName = updateDto.EmergencyContactName,
                EmergencyContactPhone = updateDto.EmergencyContactPhone,
                UpdatedAt = DateTime.UtcNow
            };

            _repositoryMock
                .GetByIdAsync(patientId)
                .Returns(existingPatient);

            _repositoryMock
                .UpdateAsync(Arg.Any<Patient>())
                .Returns(updatedPatient);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(patientId);
            result.Data!.Name.Should().Be(updateDto.Name);
            result.Data!.PhoneNumber.Should().Be(updateDto.PhoneNumber);

            await _repositoryMock.Received(1).GetByIdAsync(patientId);
            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<Patient>());
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingPatient_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientInputDto
            {
                Name = "更新的姓名"
            };

            _repositoryMock
                .GetByIdAsync(patientId)
                .Returns((Patient?)null);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者信息不存在");

            await _repositoryMock.Received(1).GetByIdAsync(patientId);
            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var patientId = Guid.NewGuid();
            var existingPatient = CreateTestPatient(patientId);
            var updateDto = new PatientInputDto
            {
                Name = "更新的姓名"
            };

            var exception = new Exception("数据库错误");

            _repositoryMock
                .GetByIdAsync(patientId)
                .Returns(existingPatient);

            _repositoryMock
                .UpdateAsync(Arg.Any<Patient>())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.UpdateAsync(patientId, updateDto));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_WithMatchingKeyword_ShouldReturnMatchingPatients()
        {
            // Arrange
            var keyword = "张";
            var matchingPatients = new List<Patient>
            {
                CreateTestPatient(),
                CreateTestPatient()
            };
            matchingPatients[0].Name = "张三";
            matchingPatients[1].Name = "张四";

            // Service使用GetPagedAsync进行搜索
            _repositoryMock
                .GetPagedAsync(1, 100, keyword)
                .Returns(new PagedResult<Patient>
                {
                    Items = matchingPatients,
                    TotalCount = 2,
                    CurrentPage = 1,
                    PageSize = 100
                });

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "";

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();

            await _repositoryMock.DidNotReceive().GetAllAsync();
        }

        [Fact]
        public async Task SearchAsync_WithWhitespaceKeyword_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "   ";

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();

            await _repositoryMock.DidNotReceive().GetAllAsync();
        }

        [Fact]
        public async Task SearchAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "不存在的名字";

            // Service使用GetPagedAsync进行搜索，返回空结果
            _repositoryMock
                .GetPagedAsync(1, 100, keyword)
                .Returns(new PagedResult<Patient>
                {
                    Items = new List<Patient>(),
                    TotalCount = 0,
                    CurrentPage = 1,
                    PageSize = 100
                });

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var keyword = "张";
            var exception = new Exception("数据库错误");

            _repositoryMock
                .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.SearchAsync(keyword));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_WithExistingPatient_ShouldDeleteSuccessfully()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock
                .DeleteAsync(patientId)
                .Returns(true);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            await _repositoryMock.Received(1).DeleteAsync(patientId);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock
                .DeleteAsync(patientId)
                .Returns(false);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("删除失败");
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var patientId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock
                .DeleteAsync(patientId)
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.DeleteAsync(patientId));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region RestoreAsync 测试

        [Fact]
        public async Task RestoreAsync_WithDeletedPatient_ShouldRestore()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = CreateTestPatient(patientId);
            patient.IsDeleted = true;

            _repositoryMock.GetByIdIncludingDeletedAsync(patientId).Returns(patient);
            _repositoryMock.UpdateAsync(Arg.Any<Patient>()).Returns(patient);

            // Act
            var result = await _patientService.RestoreAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            patient.IsDeleted.Should().BeFalse();

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<Patient>());
        }

        [Fact]
        public async Task RestoreAsync_WithNonDeletedPatient_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = CreateTestPatient(patientId);
            patient.IsDeleted = false;

            _repositoryMock.GetByIdIncludingDeletedAsync(patientId).Returns(patient);

            // Act
            var result = await _patientService.RestoreAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("未被删除");

            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
        }

        [Fact]
        public async Task RestoreAsync_WithNonExistingPatient_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock.GetByIdIncludingDeletedAsync(patientId).Returns((Patient?)null);

            // Act
            var result = await _patientService.RestoreAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者信息不存在");
        }

        #endregion

        #region BatchDeleteAsync 测试

        [Fact]
        public async Task BatchDeleteAsync_WithValidIds_ShouldSoftDeleteAll()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var patient1 = CreateTestPatient(id1);
            var patient2 = CreateTestPatient(id2);

            _repositoryMock.GetByIdAsync(id1).Returns(patient1);
            _repositoryMock.GetByIdAsync(id2).Returns(patient2);
            _repositoryMock.UpdateAsync(Arg.Any<Patient>())
                .Returns(callInfo => callInfo.Arg<Patient>());

            // Act
            var result = await _patientService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(2);
            result.Data!.FailureCount.Should().Be(0);

            // 验证使用软删除
            await _repositoryMock.Received(2).UpdateAsync(Arg.Any<Patient>());
        }

        [Fact]
        public async Task BatchDeleteAsync_WithEmptyList_ShouldReturnEmptyResult()
        {
            // Arrange
            var ids = new List<Guid>();

            // Act
            var result = await _patientService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(0);
            result.Data!.SuccessCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithSomeNonExistent_ShouldReportPartial()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var patient1 = CreateTestPatient(id1);

            _repositoryMock.GetByIdAsync(id1).Returns(patient1);
            _repositoryMock.GetByIdAsync(id2).Returns((Patient?)null);
            _repositoryMock.UpdateAsync(Arg.Any<Patient>())
                .Returns(callInfo => callInfo.Arg<Patient>());

            // Act
            var result = await _patientService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data!.FailureCount.Should().Be(1);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithException_ShouldIsolateErrors()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var patient1 = CreateTestPatient(id1);
            var patient2 = CreateTestPatient(id2);

            _repositoryMock.GetByIdAsync(id1).Returns(patient1);
            _repositoryMock.GetByIdAsync(id2).Returns(patient2);

            // 第一个成功，第二个抛异常
            _repositoryMock.UpdateAsync(Arg.Any<Patient>())
                .Returns(
                    callInfo => patient1,
                    callInfo => throw new Exception("Database error")
                );

            // Act
            var result = await _patientService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data!.FailureCount.Should().Be(1);
        }

        #endregion

        #region CheckReferenceAsync 测试

        [Fact]
        public async Task CheckReferenceAsync_WithNoReferences_ShouldReturnFalse()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = CreateTestPatient(patientId);

            _repositoryMock.GetByIdAsync(patientId).Returns(patient);

            // 模拟没有医案引用 - 通过DbContext查询
            // 由于使用InMemory数据库，默认为空

            // Act
            var result = await _patientService.CheckReferenceAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.HasReferences.Should().BeFalse();
        }

        [Fact]
        public async Task CheckReferenceAsync_WithNonExistingPatient_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock.GetByIdAsync(patientId).Returns((Patient?)null);

            // Act
            var result = await _patientService.CheckReferenceAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者信息不存在");
        }

        #endregion

        #region BatchCheckReferenceAsync 测试

        [Fact]
        public async Task BatchCheckReferenceAsync_WithValidIds_ShouldReturnAll()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var patient1 = CreateTestPatient(id1);
            var patient2 = CreateTestPatient(id2);

            _repositoryMock.GetByIdAsync(id1).Returns(patient1);
            _repositoryMock.GetByIdAsync(id2).Returns(patient2);

            // Act
            var result = await _patientService.BatchCheckReferenceAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task BatchCheckReferenceAsync_WithEmptyList_ShouldReturnEmpty()
        {
            // Arrange
            var ids = new List<Guid>();

            // Act
            var result = await _patientService.BatchCheckReferenceAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion

        #region 辅助方法

        private Patient CreateTestPatient(Guid? id = null)
        {
            var patientId = id ?? Guid.NewGuid();
            return new Patient
            {
                Id = patientId,
                Name = $"患者_{patientId.ToString().Substring(0, 8)}",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000",
                IdNumber = $"110101199001{patientId.ToString().Substring(0, 6)}",
                Address = "测试地址",
                EmergencyContactName = "紧急联系人",
                EmergencyContactPhone = "13900139000",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private List<Patient> CreateTestPatients(int count)
        {
            var patients = new List<Patient>();
            for (int i = 0; i < count; i++)
            {
                patients.Add(CreateTestPatient());
            }
            return patients;
        }

        #endregion
    }
}
