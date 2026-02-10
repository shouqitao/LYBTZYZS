using FluentAssertions;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Patients.Tests.Services;

/// <summary>
/// PatientService 单元测试
/// 测试命名规范: {Method}_{Scenario}_{ExpectedBehavior}
/// </summary>
public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _repositoryMock;
    private readonly Mock<ILogger<PatientService>> _loggerMock;
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _loggerMock = new Mock<ILogger<PatientService>>();
        _service = new PatientService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreatePatientAsync Tests

    [Fact]
    public async Task CreatePatientAsync_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var inputDto = new PatientInputDto
        {
            Name = "新患者",
            Gender = Gender.Male
        };
        var createdPatient = CreateTestPatientDetailDto(Guid.NewGuid(), "新患者", Gender.Male);

        _repositoryMock
            .Setup(r => r.CreateAsync(inputDto))
            .ReturnsAsync(createdPatient);

        // Act
        var result = await _service.CreatePatientAsync(inputDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("新患者");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task CreatePatientAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        var inputDto = new PatientInputDto
        {
            Name = "错误患者"
        };
        _repositoryMock
            .Setup(r => r.CreateAsync(inputDto))
            .ThrowsAsync(new Exception("数据库错误"));

        // Act
        var result = await _service.CreatePatientAsync(inputDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UpdatePatientAsync Tests

    [Fact]
    public async Task UpdatePatientAsync_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var inputDto = new PatientInputDto
        {
            Id = patientId,
            Name = "更新患者"
        };
        var updatedPatient = CreateTestPatientDetailDto(patientId, "更新患者", Gender.Male);

        _repositoryMock
            .Setup(r => r.UpdateAsync(inputDto))
            .ReturnsAsync(updatedPatient);

        // Act
        var result = await _service.UpdatePatientAsync(inputDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("更新患者");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePatientAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        var inputDto = new PatientInputDto
        {
            Id = Guid.NewGuid(),
            Name = "错误患者"
        };
        _repositoryMock
            .Setup(r => r.UpdateAsync(inputDto))
            .ThrowsAsync(new Exception("更新失败"));

        // Act
        var result = await _service.UpdatePatientAsync(inputDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region DeletePatientAsync Tests

    [Fact]
    public async Task DeletePatientAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(patientId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeletePatientAsync(patientId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task DeletePatientAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(patientId))
            .ThrowsAsync(new Exception("删除失败"));

        // Act
        var result = await _service.DeletePatientAsync(patientId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region BatchDeletePatientsAsync Tests

    [Fact]
    public async Task BatchDeletePatientsAsync_WithValidIds_ReturnsSuccess()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 2, FailureCount = 0 };

        _repositoryMock
            .Setup(r => r.BatchDeleteAsync(ids))
            .ReturnsAsync(batchResult);

        // Act
        var result = await _service.BatchDeletePatientsAsync(ids);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.SuccessCount.Should().Be(2);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task BatchDeletePatientsAsync_WithEmptyList_ReturnsFailed()
    {
        // Arrange
        var ids = new List<Guid>();

        // Act
        var result = await _service.BatchDeletePatientsAsync(ids);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("没有选择");
    }

    [Fact]
    public async Task BatchDeletePatientsAsync_WhenRepositoryReturnsNull_ReturnsFailed()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        _repositoryMock
            .Setup(r => r.BatchDeleteAsync(ids))
            .ReturnsAsync((BatchOperationResultDto?)null);

        // Act
        var result = await _service.BatchDeletePatientsAsync(ids);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BatchDeletePatientsAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        _repositoryMock
            .Setup(r => r.BatchDeleteAsync(ids))
            .ThrowsAsync(new Exception("批量删除失败"));

        // Act
        var result = await _service.BatchDeletePatientsAsync(ids);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region SearchPatientsAsync Tests

    [Fact]
    public async Task SearchPatientsAsync_WithKeyword_ReturnsMatchingPatients()
    {
        // Arrange
        var patients = new List<PatientListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "张三" },
            new() { Id = Guid.NewGuid(), Name = "张四" }
        };

        _repositoryMock
            .Setup(r => r.SearchAsync("张"))
            .ReturnsAsync(patients);

        // Act
        var result = await _service.SearchPatientsAsync("张");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task SearchPatientsAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.SearchAsync("error"))
            .ThrowsAsync(new Exception("搜索失败"));

        // Act
        var result = await _service.SearchPatientsAsync("error");

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetPatientsPagedAsync Tests

    [Fact]
    public async Task GetPatientsPagedAsync_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var pagedResult = new PagedResult<PatientListDto>
        {
            Items = new List<PatientListDto>
            {
                new() { Id = Guid.NewGuid(), Name = "患者1" },
                new() { Id = Guid.NewGuid(), Name = "患者2" }
            },
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetPatientsPagedAsync(1, 20);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetPatientsPagedAsync_WithKeyword_FiltersResults()
    {
        // Arrange
        var pagedResult = new PagedResult<PatientListDto>
        {
            Items = new List<PatientListDto>
            {
                new() { Id = Guid.NewGuid(), Name = "王五" }
            },
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, "王"))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetPatientsPagedAsync(1, 20, "王");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPatientsPagedAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, null))
            .ThrowsAsync(new Exception("查询失败"));

        // Act
        var result = await _service.GetPatientsPagedAsync(1, 20);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = CreateTestPatientDetailDto(patientId, "测试患者", Gender.Male);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);

        // Act
        var result = await _service.GetByIdAsync(patientId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(patientId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(nonExistingId))
            .ReturnsAsync((PatientDetailDto?)null);

        // Act
        var result = await _service.GetByIdAsync(nonExistingId);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Contain("不存在");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryThrows_ReturnsFailed()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(patientId))
            .ThrowsAsync(new Exception("查询失败"));

        // Act
        var result = await _service.GetByIdAsync(patientId);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private static PatientDetailDto CreateTestPatientDetailDto(
        Guid id,
        string name,
        Gender gender,
        CommonStatus status = CommonStatus.Enabled)
    {
        return new PatientDetailDto
        {
            Id = id,
            Name = name,
            Gender = gender,
            Status = status,
            PhoneNumber = "13800138000",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
