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

namespace LYBT.Module.Patients.Tests.Services
{
    /// <summary>
    /// PatientService 单元测试 - 简化版CRUD
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

        [Fact]
        public async Task GetPagedAsync_Should_Return_Success_With_Data()
        {
            // Arrange
            var entities = new List<Patient> { new Patient { Id = Guid.NewGuid(), Name = "张三" } };
            var pagedResult = new PagedResult<Patient>
            {
                Items = entities,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };
            var dtos = new List<PatientDto> { new PatientDto { Id = entities[0].Id, Name = "张三" } };

            _mockRepository.Setup(x => x.GetPagedAsync(1, 20)).ReturnsAsync(pagedResult);
            _mockMapper.Setup(x => x.Map<List<PatientDto>>(entities)).Returns(dtos);

            // Act
            var result = await _patientService.GetPagedAsync(1, 20);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Success_When_Patient_Exists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new Patient { Id = id, Name = "张三" };
            var dto = new PatientDto { Id = id, Name = "张三" };

            _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _mockMapper.Setup(x => x.Map<PatientDto>(entity)).Returns(dto);

            // Act
            var result = await _patientService.GetByIdAsync(id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(id);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Patient_Not_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Patient?)null);

            // Act
            var result = await _patientService.GetByIdAsync(id);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("患者不存在");
        }

        [Fact]
        public async Task CreateAsync_Should_Return_Success_With_Created_Patient()
        {
            // Arrange
            var createDto = new PatientCreateDto { Name = "张三", Gender = LYBT.Shared.Models.Enums.Gender.Male, PhoneNumber = "13800138000" };
            var entity = new Patient { Id = Guid.NewGuid(), Name = "张三" };
            var dto = new PatientDto { Id = entity.Id, Name = "张三" };

            _mockMapper.Setup(x => x.Map<Patient>(createDto)).Returns(entity);
            _mockRepository.Setup(x => x.AddAsync(entity)).ReturnsAsync(entity);
            _mockMapper.Setup(x => x.Map<PatientDto>(entity)).Returns(dto);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_Should_Return_Success_When_Patient_Exists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new PatientUpdateDto { Name = "李四" };
            var entity = new Patient { Id = id, Name = "张三" };
            var updatedEntity = new Patient { Id = id, Name = "李四" };
            var dto = new PatientDto { Id = id, Name = "李四" };

            _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _mockMapper.Setup(x => x.Map(updateDto, entity)).Returns(updatedEntity);
            _mockRepository.Setup(x => x.UpdateAsync(entity)).ReturnsAsync(updatedEntity);
            _mockMapper.Setup(x => x.Map<PatientDto>(updatedEntity)).Returns(dto);

            // Act
            var result = await _patientService.UpdateAsync(id, updateDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_Should_Return_Failure_When_Patient_Not_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new PatientUpdateDto { Name = "李四" };
            _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Patient?)null);

            // Act
            var result = await _patientService.UpdateAsync(id, updateDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("患者不存在");
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_Success_When_Delete_Succeeds()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(x => x.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _patientService.DeleteAsync(id);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_Failure_When_Delete_Fails()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(x => x.DeleteAsync(id)).ReturnsAsync(false);

            // Act
            var result = await _patientService.DeleteAsync(id);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("删除失败");
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_Keyword_Is_Empty()
        {
            // Arrange
            var keyword = "";

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_Keyword_Is_Whitespace()
        {
            // Arrange
            var keyword = "   ";

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Matches_When_Keyword_Exists()
        {
            // Arrange
            var keyword = "张";
            var entities = new List<Patient>
            {
                new Patient { Id = Guid.NewGuid(), Name = "张三" },
                new Patient { Id = Guid.NewGuid(), Name = "张四" },
                new Patient { Id = Guid.NewGuid(), Name = "李五" }
            };
            var matchedEntities = entities.Where(p => p.Name.Contains(keyword)).ToList();
            var dtos = matchedEntities.Select(e => new PatientDto { Id = e.Id, Name = e.Name }).ToList();

            _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(entities);
            _mockMapper.Setup(x => x.Map<List<PatientDto>>(It.IsAny<List<Patient>>())).Returns(dtos);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data!.All(d => d.Name.Contains(keyword)).Should().BeTrue();
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_No_Matches()
        {
            // Arrange
            var keyword = "不存在";
            var entities = new List<Patient>
            {
                new Patient { Id = Guid.NewGuid(), Name = "张三" },
                new Patient { Id = Guid.NewGuid(), Name = "李四" }
            };

            _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(entities);
            _mockMapper.Setup(x => x.Map<List<PatientDto>>(It.IsAny<List<Patient>>())).Returns(new List<PatientDto>());

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }
    }
}
