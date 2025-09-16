using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Helpers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Tests.Helpers
{
    /// <summary>
    /// PatientValidationHelper单元测试
    /// 测试重构后的患者验证助手类，确保使用BaseValidationHelper基类方法的正确性
    /// </summary>
    public class PatientValidationHelperTests : IDisposable
    {
        private readonly Mock<IPatientRepository> _mockRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientValidationHelper> _logger;
        private readonly Mock<PatientValidationService> _mockValidationService;
        private readonly PatientValidationHelper _validationHelper;

        public PatientValidationHelperTests()
        {
            _mockRepository = new Mock<IPatientRepository>();
            
            // 配置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                // 添加基本的映射配置
                cfg.CreateMap<PatientCreateDto, PatientDto>();
                cfg.CreateMap<PatientUpdateDto, PatientDto>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _logger = NullLogger<PatientValidationHelper>.Instance;
            
            // 创建PatientValidationService的Mock
            _mockValidationService = new Mock<PatientValidationService>();

            _validationHelper = new PatientValidationHelper(
                _mockRepository.Object,
                _mapper,
                _logger,
                _mockValidationService.Object);
        }

        public void Dispose()
        {
            // 清理资源
        }

        #region ValidateForCreateAsync Tests

        [Fact]
        public async Task ValidateForCreateAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "13800138000",
                IdNumber = "11010519491231002X",
                Gender = Gender.Male,
                Age = 30,
                Address = "北京市朝阳区",
                MedicalHistory = "无重大疾病史"
            };

            _mockValidationService
                .Setup(x => x.ValidateForCreateAsync(It.IsAny<PatientDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _validationHelper.ValidateForCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockValidationService.Verify(x => x.ValidateForCreateAsync(It.IsAny<PatientDto>()), Times.Once);
        }

        [Fact]
        public async Task ValidateForCreateAsync_WithValidationServiceException_ReturnsFailure()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "13800138000"
            };

            var exception = new ArgumentException("验证失败");
            _mockValidationService
                .Setup(x => x.ValidateForCreateAsync(It.IsAny<PatientDto>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _validationHelper.ValidateForCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("验证失败");
        }

        #endregion

        #region ValidateForUpdateAsync Tests

        [Fact]
        public async Task ValidateForUpdateAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var dto = new PatientUpdateDto
            {
                Name = "李四",
                PhoneNumber = "13900139000",
                IdNumber = "11010519491231003X",
                Gender = Gender.Female,
                Age = 25
            };

            _mockValidationService
                .Setup(x => x.ValidateForUpdateAsync(It.IsAny<Guid>(), It.IsAny<PatientDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _validationHelper.ValidateForUpdateAsync(patientId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockValidationService.Verify(x => x.ValidateForUpdateAsync(patientId, It.IsAny<PatientDto>()), Times.Once);
        }

        [Fact]
        public async Task ValidateForUpdateAsync_WithValidationServiceException_ReturnsFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var dto = new PatientUpdateDto
            {
                Name = "李四",
                PhoneNumber = "13900139000"
            };

            var exception = new InvalidOperationException("更新验证失败");
            _mockValidationService
                .Setup(x => x.ValidateForUpdateAsync(It.IsAny<Guid>(), It.IsAny<PatientDto>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _validationHelper.ValidateForUpdateAsync(patientId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("更新验证失败");
        }

        #endregion

        #region ValidatePatientAsync Tests

        [Fact]
        public async Task ValidatePatientAsync_WithValidData_ReturnsSuccessResult()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "王五",
                PhoneNumber = "13700137000"
            };

            _mockValidationService
                .Setup(x => x.ValidateForCreateAsync(It.IsAny<PatientDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _validationHelper.ValidatePatientAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            
            // 验证返回的对象结构
            var data = result.Data as dynamic;
            ((bool)data.IsValid).Should().BeTrue();
            ((string)data.Message).Should().Be("验证通过");
        }

        [Fact]
        public async Task ValidatePatientAsync_WithValidationException_ReturnsFailureResult()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "无效患者"
            };

            var exception = new ArgumentException("患者信息无效");
            _mockValidationService
                .Setup(x => x.ValidateForCreateAsync(It.IsAny<PatientDto>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _validationHelper.ValidatePatientAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue(); // 方法总是返回成功，但数据中包含验证结果
            result.Data.Should().NotBeNull();
            
            var data = result.Data as dynamic;
            ((bool)data.IsValid).Should().BeFalse();
            ((string)data.Message).Should().Contain("患者信息无效");
        }

        #endregion

        #region Data Integrity Validation Tests

        [Fact]
        public void ValidatePatientId_WithValidGuid_ReturnsSuccess()
        {
            // Arrange
            var validId = Guid.NewGuid();

            // Act
            var result = _validationHelper.ValidatePatientId(validId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidatePatientId_WithEmptyGuid_ReturnsFailure()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            var result = _validationHelper.ValidatePatientId(emptyId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者ID");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Theory]
        [InlineData("张三")]
        [InlineData("李小红")]
        [InlineData("王大明")]
        public void ValidatePatientName_WithValidName_ReturnsSuccess(string name)
        {
            // Act
            var result = _validationHelper.ValidatePatientName(name);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePatientName_WithInvalidName_ReturnsFailure(string invalidName)
        {
            // Act
            var result = _validationHelper.ValidatePatientName(invalidName);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者姓名");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Fact]
        public void ValidatePatientName_WithTooLongName_ReturnsFailure()
        {
            // Arrange
            var longName = new string('名', 51); // 超过50个字符的限制

            // Act
            var result = _validationHelper.ValidatePatientName(longName);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("不能超过50个字符");
        }

        [Theory]
        [InlineData("13800138000")]
        [InlineData("15901234567")]
        [InlineData("18612345678")]
        public void ValidatePhoneNumber_WithValidNumber_ReturnsSuccess(string phoneNumber)
        {
            // Act
            var result = _validationHelper.ValidatePhoneNumber(phoneNumber);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("1234567890")]    // 不是1开头
        [InlineData("120012345678")]  // 超过11位
        [InlineData("1380013800")]    // 少于11位
        public void ValidatePhoneNumber_WithInvalidNumber_ReturnsFailure(string phoneNumber)
        {
            // Act
            var result = _validationHelper.ValidatePhoneNumber(phoneNumber);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("格式不正确");
        }

        [Theory]
        [InlineData("11010519491231002X")]
        [InlineData("110105194912310021")]
        [InlineData("123456789012345")]  // 15位身份证
        public void ValidateIdNumber_WithValidIdCard_ReturnsSuccess(string idNumber)
        {
            // Act
            var result = _validationHelper.ValidateIdNumber(idNumber);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("12345")]           // 太短
        [InlineData("1234567890123456789")] // 太长
        [InlineData("11010519491231002A")]  // 末位不是X或数字
        public void ValidateIdNumber_WithInvalidIdCard_ReturnsFailure(string idNumber)
        {
            // Act
            var result = _validationHelper.ValidateIdNumber(idNumber);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("格式不正确");
        }

        [Theory]
        [InlineData(25)]
        [InlineData(0)]
        [InlineData(150)]
        public void ValidateAge_WithValidAge_ReturnsSuccess(int age)
        {
            // Act
            var result = _validationHelper.ValidateAge(age);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(151)]
        public void ValidateAge_WithInvalidAge_ReturnsFailure(int age)
        {
            // Act
            var result = _validationHelper.ValidateAge(age);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("年龄");
        }

        [Fact]
        public void ValidateAge_WithNullAge_ReturnsSuccess()
        {
            // Act
            var result = _validationHelper.ValidateAge(null);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(Gender.Male)]
        [InlineData(Gender.Female)]
        public void ValidateGender_WithValidGender_ReturnsSuccess(Gender gender)
        {
            // Act
            var result = _validationHelper.ValidateGender(gender);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidateGender_WithNullGender_ReturnsSuccess()
        {
            // Act
            var result = _validationHelper.ValidateGender(null);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidateGender_WithInvalidGender_ReturnsFailure()
        {
            // Arrange
            var invalidGender = (Gender)999; // 无效的枚举值

            // Act
            var result = _validationHelper.ValidateGender(invalidGender);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("性别值无效");
        }

        #endregion

        #region Business Rules Validation Tests

        [Fact]
        public async Task ValidateCanDeleteAsync_WithExistingPatient_ReturnsSuccess()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient
            {
                Id = patientId,
                Name = "测试患者",
                Status = CommonStatus.Enabled
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(patientId, true))
                .ReturnsAsync(patient);

            // Act
            var result = await _validationHelper.ValidateCanDeleteAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateCanDeleteAsync_WithNonExistentPatient_ReturnsFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            
            _mockRepository
                .Setup(x => x.GetByIdAsync(patientId, true))
                .ReturnsAsync((Patient)null);

            // Act
            var result = await _validationHelper.ValidateCanDeleteAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者不存在");
        }

        [Fact]
        public async Task ValidateCanUpdateAsync_WithExistingPatient_ReturnsSuccess()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient
            {
                Id = patientId,
                Name = "测试患者",
                Status = CommonStatus.Enabled
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(patientId, true))
                .ReturnsAsync(patient);

            // Act
            var result = await _validationHelper.ValidateCanUpdateAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateCanUpdateAsync_WithNonExistentPatient_ReturnsFailure()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            
            _mockRepository
                .Setup(x => x.GetByIdAsync(patientId, true))
                .ReturnsAsync((Patient)null);

            // Act
            var result = await _validationHelper.ValidateCanUpdateAsync(patientId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者不存在");
        }

        [Theory]
        [InlineData(CommonStatus.Enabled, CommonStatus.Disabled)]
        [InlineData(CommonStatus.Disabled, CommonStatus.Enabled)]
        public void ValidateStatusChange_WithDifferentStatuses_ReturnsSuccess(CommonStatus current, CommonStatus newStatus)
        {
            // Act
            var result = _validationHelper.ValidateStatusChange(current, newStatus);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidateStatusChange_WithSameStatus_ReturnsFailure()
        {
            // Act
            var result = _validationHelper.ValidateStatusChange(CommonStatus.Enabled, CommonStatus.Enabled);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("新状态与当前状态相同");
        }

        #endregion

        #region ProcessIdNumberInfo Tests

        [Fact]
        public void ProcessIdNumberInfo_WithValidPatient_ReturnsSuccess()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                IdNumber = "11010519491231002X"
            };

            _mockValidationService
                .Setup(x => x.ProcessIdNumberInfo(It.IsAny<Patient>()));

            // Act
            var result = _validationHelper.ProcessIdNumberInfo(patient);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(patient);
            _mockValidationService.Verify(x => x.ProcessIdNumberInfo(patient), Times.Once);
        }

        #endregion

        #region ValidatePatientBasicInfoAsync Tests

        [Fact]
        public async Task ValidatePatientBasicInfoAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "13800138000",
                IdNumber = "11010519491231002X",
                Gender = Gender.Male,
                Age = 30,
                Address = "北京市朝阳区测试街道123号",
                MedicalHistory = "无重大疾病史，偶有感冒发烧"
            };

            // Act
            var result = await _validationHelper.ValidatePatientBasicInfoAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePatientBasicInfoAsync_WithInvalidName_ReturnsFailure()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "", // 无效姓名
                PhoneNumber = "13800138000",
                IdNumber = "11010519491231002X",
                Gender = Gender.Male,
                Age = 30
            };

            // Act
            var result = await _validationHelper.ValidatePatientBasicInfoAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者姓名");
        }

        [Fact]
        public async Task ValidatePatientBasicInfoAsync_WithInvalidPhoneNumber_ReturnsFailure()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "12345", // 无效手机号
                IdNumber = "11010519491231002X",
                Gender = Gender.Male,
                Age = 30
            };

            // Act
            var result = await _validationHelper.ValidatePatientBasicInfoAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("格式不正确");
        }

        [Fact]
        public async Task ValidatePatientBasicInfoAsync_WithTooLongAddress_ReturnsFailure()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "13800138000",
                IdNumber = "11010519491231002X",
                Gender = Gender.Male,
                Age = 30,
                Address = new string('地', 201) // 超过200字符限制
            };

            // Act
            var result = await _validationHelper.ValidatePatientBasicInfoAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("地址");
            result.ErrorMessage.Should().Contain("不能超过200个字符");
        }

        [Fact]
        public async Task ValidatePatientBasicInfoAsync_WithTooLongMedicalHistory_ReturnsFailure()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "张三",
                PhoneNumber = "13800138000",
                IdNumber = "11010519491231002X",
                Gender = Gender.Male,
                Age = 30,
                Address = "北京市朝阳区",
                MedicalHistory = new string('史', 1001) // 超过1000字符限制
            };

            // Act
            var result = await _validationHelper.ValidatePatientBasicInfoAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("既往病史");
            result.ErrorMessage.Should().Contain("不能超过1000个字符");
        }

        #endregion
    }
}