using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Patients.Tests.Repositories;

/// <summary>
/// PatientRepository 单元测试
/// 测试命名规范: {Method}_{Scenario}_{ExpectedBehavior}
/// </summary>
public class PatientRepositoryTests
{
    private readonly Mock<IPatientDataSource> _dataSourceMock;
    private readonly Mock<ILogger<PatientRepository>> _loggerMock;
    private readonly PatientRepository _repository;

    public PatientRepositoryTests()
    {
        _dataSourceMock = new Mock<IPatientDataSource>();
        _loggerMock = new Mock<ILogger<PatientRepository>>();
        _repository = new PatientRepository(_dataSourceMock.Object, _loggerMock.Object);
    }

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var patients = new List<PatientDetailDto>
        {
            CreateTestPatient("张三", Gender.Male),
            CreateTestPatient("李四", Gender.Female)
        };
        _dataSourceMock
            .Setup(ds => ds.GetPagedAsync(1, 20, null, default))
            .ReturnsAsync((patients, 2));

        // Act
        var result = await _repository.GetPagedAsync(1, 20, null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersResults()
    {
        // Arrange
        var patients = new List<PatientDetailDto>
        {
            CreateTestPatient("张三", Gender.Male)
        };
        _dataSourceMock
            .Setup(ds => ds.GetPagedAsync(1, 20, "张", default))
            .ReturnsAsync((patients, 1));

        // Act
        var result = await _repository.GetPagedAsync(1, 20, "张");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Contain("张");
    }

    [Fact]
    public async Task GetPagedAsync_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        _dataSourceMock
            .Setup(ds => ds.GetPagedAsync(1, 20, "不存在", default))
            .ReturnsAsync((new List<PatientDetailDto>(), 0));

        // Act
        var result = await _repository.GetPagedAsync(1, 20, "不存在");

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsPatientDetailDto()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = CreateTestPatient("测试患者", Gender.Male);
        patient.Id = patientId;
        _dataSourceMock
            .Setup(ds => ds.GetByIdAsync(patientId, default))
            .ReturnsAsync(patient);

        // Act
        var result = await _repository.GetByIdAsync(patientId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(patientId);
        result.Name.Should().Be("测试患者");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _dataSourceMock
            .Setup(ds => ds.GetByIdAsync(nonExistingId, default))
            .ReturnsAsync((PatientDetailDto?)null);

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedPatient()
    {
        // Arrange
        var inputDto = new PatientInputDto
        {
            Name = "新患者",
            Gender = Gender.Male,
            PhoneNumber = "13800138000"
        };
        var createdPatient = CreateTestPatient("新患者", Gender.Male);
        createdPatient.Id = Guid.NewGuid();
        createdPatient.PhoneNumber = "13800138000";

        _dataSourceMock
            .Setup(ds => ds.CreateAsync(It.IsAny<PatientInputDto>(), default))
            .ReturnsAsync(createdPatient);

        // Act
        var result = await _repository.CreateAsync(inputDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("新患者");
        result.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public async Task CreateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ReturnsUpdatedPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var inputDto = new PatientInputDto
        {
            Id = patientId,
            Name = "更新后患者",
            Gender = Gender.Female
        };
        var updatedPatient = CreateTestPatient("更新后患者", Gender.Female);
        updatedPatient.Id = patientId;

        _dataSourceMock
            .Setup(ds => ds.UpdateAsync(It.IsAny<PatientInputDto>(), default))
            .ReturnsAsync(updatedPatient);

        // Act
        var result = await _repository.UpdateAsync(inputDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(patientId);
        result.Name.Should().Be("更新后患者");
    }

    [Fact]
    public async Task UpdateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        _dataSourceMock
            .Setup(ds => ds.DeleteAsync(patientId, default))
            .ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(patientId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _dataSourceMock
            .Setup(ds => ds.DeleteAsync(nonExistingId, default))
            .ReturnsAsync(false);

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithKeyword_ReturnsMatchingPatients()
    {
        // Arrange
        var patients = new List<PatientDetailDto>
        {
            CreateTestPatient("张三", Gender.Male),
            CreateTestPatient("张四", Gender.Female)
        };
        _dataSourceMock
            .Setup(ds => ds.SearchAsync("张", default))
            .ReturnsAsync(patients);

        // Act
        var result = await _repository.SearchAsync("张");

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Name.Contains("张")).Should().BeTrue();
    }

    #endregion

    #region GetByIdNumberAsync Tests

    [Fact]
    public async Task GetByIdNumberAsync_WithExistingIdNumber_ReturnsPatient()
    {
        // Arrange
        var idNumber = "110101199001011234";
        var patientDto = CreateTestPatient("身份证患者", Gender.Male);
        patientDto.IdNumber = idNumber;
        _dataSourceMock
            .Setup(ds => ds.GetByIdNumberAsync(idNumber, default))
            .ReturnsAsync(patientDto);

        // Act
        var result = await _repository.GetByIdNumberAsync(idNumber);

        // Assert
        result.Should().NotBeNull();
        result!.IdNumber.Should().Be(idNumber);
    }

    [Fact]
    public async Task GetByIdNumberAsync_WithNonExistingIdNumber_ReturnsNull()
    {
        // Arrange
        _dataSourceMock
            .Setup(ds => ds.GetByIdNumberAsync("000000000000000000", default))
            .ReturnsAsync((PatientDetailDto?)null);

        // Act
        var result = await _repository.GetByIdNumberAsync("000000000000000000");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region BatchDeleteAsync Tests

    [Fact]
    public async Task BatchDeleteAsync_WithValidIds_ReturnsResult()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 3, FailureCount = 0 };
        _dataSourceMock
            .Setup(ds => ds.BatchDeleteAsync(ids, default))
            .ReturnsAsync(batchResult);

        // Act
        var result = await _repository.BatchDeleteAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchDeleteAsync_WithPartialFailure_ReportsCorrectly()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 1, FailureCount = 1 };
        _dataSourceMock
            .Setup(ds => ds.BatchDeleteAsync(ids, default))
            .ReturnsAsync(batchResult);

        // Act
        var result = await _repository.BatchDeleteAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task BatchDeleteAsync_WhenDataSourceThrows_ReturnsNull()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        _dataSourceMock
            .Setup(ds => ds.BatchDeleteAsync(ids, default))
            .ThrowsAsync(new Exception("批量删除失败"));

        // Act
        var result = await _repository.BatchDeleteAsync(ids);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static PatientDetailDto CreateTestPatient(
        string name,
        Gender gender,
        CommonStatus status = CommonStatus.Enabled)
    {
        return new PatientDetailDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Gender = gender,
            Status = status,
            PhoneNumber = "13800138000",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
