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
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests
{
    /// <summary>
    /// 简化的患者服务测试
    /// </summary>
    public class SimplePatientServiceTests
    {
        private readonly Mock<IPatientRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<PatientService>> _mockLogger;
        private readonly PatientService _service;

        public SimplePatientServiceTests()
        {
            _mockRepository = new Mock<IPatientRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<PatientService>>();
            _service = new PatientService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Patient_When_Found()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patient = new Patient { Id = patientId, Name = "测试患者" };
            var patientDto = new PatientDto { Id = patientId, Name = "测试患者" };
            
            _mockRepository.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);
            _mockMapper.Setup(m => m.Map<PatientDto>(patient)).Returns(patientDto);

            // Act
            var result = await _service.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("测试患者");
        }

        [Fact]
        public async Task CreateAsync_Should_Create_New_Patient()
        {
            // Arrange
            var createDto = new PatientCreateDto 
            { 
                Name = "新患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                PhoneNumber = "13800138000"
            };
            
            var patient = new Patient 
            { 
                Id = Guid.NewGuid(),
                Name = "新患者"
            };
            
            var patientDto = new PatientDto 
            { 
                Id = patient.Id,
                Name = "新患者"
            };

            _mockMapper.Setup(m => m.Map<Patient>(createDto)).Returns(patient);
            _mockRepository.Setup(r => r.AddAsync(patient)).ReturnsAsync(patient);
            _mockMapper.Setup(m => m.Map<PatientDto>(patient)).Returns(patientDto);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("新患者");
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_Existing_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientUpdateDto { Name = "更新后的名字" };
            var existingPatient = new Patient { Id = patientId, Name = "原名字" };
            var updatedPatient = new Patient { Id = patientId, Name = "更新后的名字" };
            var patientDto = new PatientDto { Id = patientId, Name = "更新后的名字" };

            _mockRepository.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(existingPatient);
            _mockMapper.Setup(m => m.Map(updateDto, existingPatient)).Returns(updatedPatient);
            _mockRepository.Setup(r => r.UpdateAsync(existingPatient)).ReturnsAsync(updatedPatient);
            _mockMapper.Setup(m => m.Map<PatientDto>(updatedPatient)).Returns(patientDto);

            // Act
            var result = await _service.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("更新后的名字");
        }

        [Fact]
        public async Task DeleteAsync_Should_Delete_Patient()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            _mockRepository.Setup(r => r.DeleteAsync(patientId)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paged_Results()
        {
            // Arrange
            var patients = new List<Patient>
            {
                new Patient { Id = Guid.NewGuid(), Name = "患者1" },
                new Patient { Id = Guid.NewGuid(), Name = "患者2" }
            };
            
            var pagedResult = new PagedResult<Patient>
            {
                Items = patients,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };

            var patientDtos = new List<PatientDto>
            {
                new PatientDto { Id = patients[0].Id, Name = "患者1" },
                new PatientDto { Id = patients[1].Id, Name = "患者2" }
            };

            _mockRepository.Setup(r => r.GetPagedAsync(1, 20)).ReturnsAsync(pagedResult);
            _mockMapper.Setup(m => m.Map<List<PatientDto>>(patients)).Returns(patientDtos);

            // Act
            var result = await _service.GetPagedAsync(1, 20);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }
    }
}