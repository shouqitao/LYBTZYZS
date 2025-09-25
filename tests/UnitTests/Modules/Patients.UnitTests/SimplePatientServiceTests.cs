using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests
{
    /// <summary>
    /// 患者服务简化测试 - 快速验证核心功能
    /// </summary>
    public class SimplePatientServiceTests
    {
        private readonly PatientService _patientService;
        private readonly Mock<IPatientRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<PatientService>> _mockLogger;

        public SimplePatientServiceTests()
        {
            _mockRepository = new Mock<IPatientRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<PatientService>>();
            _patientService = new PatientService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "测试患者" };
            var patientDto = new PatientDto { Id = patientId, Name = "测试患者" };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync(patient);
            _mockMapper.Setup(x => x.Map<PatientDto>(It.IsAny<Patient>())).Returns(patientDto);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("测试患者");
        }

        [Fact]
        public async Task CreateAsync_Should_Create_New_Patient()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "新患者",
                PhoneNumber = "13800138000"
            };
            var patient = new Patient { Id = Guid.NewGuid(), Name = "新患者" };
            var patientDto = new PatientDto { Id = patient.Id, Name = "新患者" };

            _mockMapper.Setup(x => x.Map<Patient>(It.IsAny<PatientCreateDto>())).Returns(patient);
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<Patient>())).ReturnsAsync(patient);
            _mockMapper.Setup(x => x.Map<PatientDto>(It.IsAny<Patient>())).Returns(patientDto);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("新患者");
        }

        [Fact]
        public async Task DeleteAsync_Should_Soft_Delete_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "待删除患者", IsDeleted = false };

            _mockRepository.Setup(x => x.GetByIdAsync(patientId, false)).ReturnsAsync(patient);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Patient>())).ReturnsAsync((Patient p) => p);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<Patient>(p => p.IsDeleted == true)), Times.Once);
        }
    }
}