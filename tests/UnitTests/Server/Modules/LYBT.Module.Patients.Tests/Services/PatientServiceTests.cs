using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests.Services
{
    /// <summary>
    /// 患者服务单元测试
    /// 测试患者CRUD操作及搜索功能的所有场景
    /// </summary>
    public class PatientServiceTests : TestBase
    {
        private readonly PatientService _patientService;
        private readonly Mock<IPatientRepository> _repositoryMock;
        private readonly Mock<ILogger<PatientService>> _loggerMock;
        private readonly Mock<IValidator<PatientInputDto>> _validatorMock;

        public PatientServiceTests()
        {
            _repositoryMock = CreateMock<IPatientRepository>();
            _loggerMock = CreateLoggerMock<PatientService>();
            _validatorMock = CreateMock<IValidator<PatientInputDto>>();

            // 默认validator返回验证成功
            _validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<PatientInputDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            // 创建PatientService实例，使用基类提供的Mapper
            _patientService = new PatientService(
                _repositoryMock.Object,
                Mapper,
                _loggerMock.Object,
                _validatorMock.Object);
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
                .Setup(x => x.GetPagedAsync(1, 20, It.IsAny<string?>()))
                .ReturnsAsync(pagedResult);

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

            _repositoryMock.Verify(x => x.GetPagedAsync(1, 20, It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理，测试更新为期望异常上抛
            var exception = new Exception("数据库错误");
            _repositoryMock
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
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
                .Setup(x => x.GetPagedAsync(1, 20, It.IsAny<string?>()))
                .ReturnsAsync(pagedResult);

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
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(patientId);
            result.Data!.Name.Should().Be(patient.Name);
            result.Data!.PhoneNumber.Should().Be(patient.PhoneNumber);

            _repositoryMock.Verify(x => x.GetByIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingPatient_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync((Patient?)null);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者不存在");
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
                .Setup(x => x.GetByIdAsync(patientId))
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
                .Setup(x => x.AddAsync(It.IsAny<Patient>()))
                .ReturnsAsync(createdPatient);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(createDto.Name);
            result.Data!.PhoneNumber.Should().Be(createDto.PhoneNumber);
            result.Data!.IdNumber.Should().Be(createDto.IdNumber);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Patient>()), Times.Once);
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
                .Setup(x => x.AddAsync(It.IsAny<Patient>()))
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
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(existingPatient);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Patient>()))
                .ReturnsAsync(updatedPatient);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(patientId);
            result.Data!.Name.Should().Be(updateDto.Name);
            result.Data!.PhoneNumber.Should().Be(updateDto.PhoneNumber);

            _repositoryMock.Verify(x => x.GetByIdAsync(patientId), Times.Once);
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Patient>()), Times.Once);
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
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync((Patient?)null);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者不存在");

            _repositoryMock.Verify(x => x.GetByIdAsync(patientId), Times.Once);
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Patient>()), Times.Never);
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
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(existingPatient);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Patient>()))
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
                .Setup(x => x.GetPagedAsync(1, 100, keyword))
                .ReturnsAsync(new PagedResult<Patient>
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

            _repositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
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

            _repositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task SearchAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "不存在的名字";

            // Service使用GetPagedAsync进行搜索，返回空结果
            _repositoryMock
                .Setup(x => x.GetPagedAsync(1, 100, keyword))
                .ReturnsAsync(new PagedResult<Patient>
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
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
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
                .Setup(x => x.DeleteAsync(patientId))
                .ReturnsAsync(true);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _repositoryMock.Verify(x => x.DeleteAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.DeleteAsync(patientId))
                .ReturnsAsync(false);

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
                .Setup(x => x.DeleteAsync(patientId))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _patientService.DeleteAsync(patientId));

            thrownException.Message.Should().Be("数据库错误");
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
