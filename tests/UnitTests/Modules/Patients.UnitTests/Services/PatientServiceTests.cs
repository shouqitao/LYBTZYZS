using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests.Services
{
    /// <summary>
    /// PatientService 完整单元测试 - 统一服务架构
    /// 测试服务层的所有业务逻辑和查询操作
    /// </summary>
    public class PatientServiceTests
    {
        private readonly PatientService _patientService;
        private readonly Mock<IPatientRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<PatientService>> _mockLogger;

        public PatientServiceTests()
        {
            _mockRepository = new Mock<IPatientRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<PatientService>>();
            _patientService = new PatientService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_Repository_Is_Null()
        {
            // Act & Assert
            var action = () => new PatientService(null!, _mockMapper.Object, _mockLogger.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("patientRepository");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Mapper_Is_Null()
        {
            // Act & Assert
            var action = () => new PatientService(_mockRepository.Object, null!, _mockLogger.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("mapper");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            var action = () => new PatientService(_mockRepository.Object, _mockMapper.Object, null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Patient_When_Found()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "张三" };
            var patientDto = new PatientDto { Id = patientId, Name = "张三" };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync(patient);
            _mockMapper.Setup(x => x.Map<PatientDto>(It.IsAny<Patient>())).Returns(patientDto);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(patientDto);
            _mockRepository.Verify(x => x.GetByIdAsync(patientId, false), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_NotFound_When_Patient_Not_Exists()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync((Patient?)null);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("患者不存在");
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Patients_Matching_Keyword()
        {
            // Arrange
            var keyword = "张";
            var patients = new List<Patient>
            {
                new() { Name = "张三" },
                new() { Name = "张四" }
            };
            var patientDtos = new List<PatientDto>
            {
                new() { Name = "张三" },
                new() { Name = "张四" }
            };

            _mockRepository.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Patient, bool>>>()))
                .ReturnsAsync(patients);
            _mockMapper.Setup(x => x.Map<List<PatientDto>>(patients)).Returns(patientDtos);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Create_Patient_Successfully()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "李四",
                Gender = Gender.Male,
                BirthDate = DateTime.Today.AddYears(-30),
                PhoneNumber = "13900139000"
            };
            var patient = new Patient { Id = Guid.NewGuid(), Name = "李四" };
            var patientDto = new PatientDto { Id = patient.Id, Name = "李四" };

            _mockMapper.Setup(x => x.Map<Patient>(It.IsAny<PatientCreateDto>())).Returns(patient);
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<Patient>())).ReturnsAsync(patient);
            _mockMapper.Setup(x => x.Map<PatientDto>(It.IsAny<Patient>())).Returns(patientDto);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(patientDto);
            _mockRepository.Verify(x => x.AddAsync(It.IsAny<Patient>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_Patient_When_Exists()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientUpdateDto
            {
                Name = "王五",
                PhoneNumber = "13800138000"
            };
            var existingPatient = new Patient { Id = patientId, Name = "张三" };
            var updatedPatient = new Patient { Id = patientId, Name = "王五" };
            var patientDto = new PatientDto { Id = patientId, Name = "王五" };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, true)).ReturnsAsync(existingPatient);
            _mockMapper.Setup(x => x.Map(It.IsAny<PatientUpdateDto>(), It.IsAny<Patient>())).Returns(updatedPatient);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Patient>())).ReturnsAsync(updatedPatient);
            _mockMapper.Setup(x => x.Map<PatientDto>(updatedPatient)).Returns(patientDto);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(patientDto);
        }

        [Fact]
        public async Task DeleteAsync_Should_Soft_Delete_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "张三", IsDeleted = false };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync(patient);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Patient>())).ReturnsAsync((Patient p) => p);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<Patient>(p => p.IsDeleted == true)), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_Should_Enable_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "张三", Status = CommonStatus.Disabled };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync(patient);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Patient>())).ReturnsAsync((Patient p) => p);

            // Act
            var result = await _patientService.EnableAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<Patient>(p => p.Status == CommonStatus.Enabled)), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_Should_Disable_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "张三", Status = CommonStatus.Enabled };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync(patient);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Patient>())).ReturnsAsync((Patient p) => p);

            // Act
            var result = await _patientService.DisableAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<Patient>(p => p.Status == CommonStatus.Disabled)), Times.Once);
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task ImportPatientsAsync_Should_Import_Multiple_Patients()
        {
            // Arrange
            var patients = new List<PatientCreateDto>
            {
                new() { Name = "患者1", PhoneNumber = "13800000001" },
                new() { Name = "患者2", PhoneNumber = "13800000002" }
            };

            _mockMapper.Setup(x => x.Map<Patient>(It.IsAny<PatientCreateDto>()))
                .Returns<PatientCreateDto>(dto => new Patient { Name = dto.Name });
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<Patient>()))
                .ReturnsAsync((Patient p) => p);

            // Act
            var result = await _patientService.ImportPatientsAsync(patients);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(x => x.AddAsync(It.IsAny<Patient>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ExportPatientsAsync_Should_Export_Patient_Data()
        {
            // Arrange
            var query = new PagedQueryBaseDto { PageIndex = 1, PageSize = 10 };
            var patients = new List<Patient>
            {
                new() { Name = "张三" },
                new() { Name = "李四" }
            };

            _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(patients);

            // Act
            var result = await _patientService.ExportPatientsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region 异常处理测试

        [Fact]
        public async Task GetByIdAsync_Should_Handle_Repository_Exception()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("获取患者信息失败");
        }

        [Fact]
        public async Task CreateAsync_Should_Handle_Validation_Errors()
        {
            // Arrange
            var createDto = new PatientCreateDto(); // 空数据

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        #endregion
    }
}