using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Tests.Desktop.Infrastructure;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.LocalData.DataSources;

/// <summary>
/// LocalPatientDataSource 单元测试
/// OpenSpec: implement-local-mode Phase 5
/// </summary>
public class LocalPatientDataSourceTests : IClassFixture<LocalDbContextFixture>
{
    private readonly LocalDbContextFixture _fixture;
    private readonly ILogger<LocalPatientDataSource> _logger;

    public LocalPatientDataSourceTests(LocalDbContextFixture fixture)
    {
        _fixture = fixture;
        _logger = LocalDbContextFixture.CreateLogger<LocalPatientDataSource>();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingPatient_ReturnsPatient()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("张三", "13800138001");
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(patient.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("张三");
        result.PhoneNumber.Should().Be("13800138001");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingPatient_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedPatient_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("已删除患者");
        patient.IsDeleted = true;
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(patient.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        for (int i = 1; i <= 15; i++)
        {
            context.Patients.Add(CreateTestPatient($"患者{i:D2}"));
        }
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(page: 1, pageSize: 10);

        // Assert
        total.Should().Be(15);
        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersResults()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Patients.Add(CreateTestPatient("张三", phoneNumber: "13800138001"));
        context.Patients.Add(CreateTestPatient("张四", phoneNumber: "13800138002"));
        context.Patients.Add(CreateTestPatient("李五", phoneNumber: "13900139001"));
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(page: 1, pageSize: 10, keyword: "张");

        // Assert
        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(p => p.Name.Contains("张"));
    }

    [Fact]
    public async Task GetPagedAsync_WithPhoneKeyword_FiltersResults()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Patients.Add(CreateTestPatient("患者A", phoneNumber: "13800138001"));
        context.Patients.Add(CreateTestPatient("患者B", phoneNumber: "13900139001"));
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(page: 1, pageSize: 10, keyword: "138");

        // Assert
        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Name.Should().Be("患者A");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidPatient_ReturnsCreatedPatient()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalPatientDataSource(context, _logger);
        var input = new PatientInputDto
        {
            Name = "新患者",
            Gender = Gender.Male,
            PhoneNumber = "13800138888"
        };

        // Act
        var result = await dataSource.CreateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("新患者");

        // 验证数据库中已保存
        var saved = await context.Patients.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingPatient_UpdatesSuccessfully()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("原名称");
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var input = new PatientInputDto
        {
            Id = patient.Id,
            Name = "新名称",
            Gender = patient.Gender,
            PhoneNumber = "13900139999"
        };
        var result = await dataSource.UpdateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("新名称");
        result.PhoneNumber.Should().Be("13900139999");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingPatient_ThrowsException()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalPatientDataSource(context, _logger);
        var input = new PatientInputDto
        {
            Id = Guid.NewGuid(),
            Name = "不存在"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dataSource.UpdateAsync(input));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingPatient_SoftDeletes()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("待删除");
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.DeleteAsync(patient.Id);

        // Assert
        result.Should().BeTrue();

        // 验证软删除
        var deleted = await context.Patients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == patient.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingPatient_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_EmptyKeyword_ReturnsEmptyList()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.SearchAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ValidKeyword_ReturnsMatchingPatients()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Patients.Add(CreateTestPatient("王大明", pinYinCode: "WDM"));
        context.Patients.Add(CreateTestPatient("王小明", pinYinCode: "WXM"));
        context.Patients.Add(CreateTestPatient("李小红", pinYinCode: "LXH"));
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.SearchAsync("王");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Name.Contains("王"));
    }

    #endregion

    #region GetByIdNumberAsync Tests

    [Fact]
    public async Task GetByIdNumberAsync_ExistingIdNumber_ReturnsPatient()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("身份证测试");
        patient.IdNumber = "110101199001011234";
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdNumberAsync("110101199001011234");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("身份证测试");
    }

    [Fact]
    public async Task GetByIdNumberAsync_EmptyIdNumber_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdNumberAsync("");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_DeletedPatient_RestoresSuccessfully()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("已删除待恢复");
        patient.IsDeleted = true;
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);

        // Act
        var result = await dataSource.RestoreAsync(patient.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("已删除待恢复");

        // Verify entity is no longer soft-deleted
        var restored = await context.Patients.FindAsync(patient.Id);
        restored!.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region BatchDeleteAsync Tests

    [Fact]
    public async Task BatchDeleteAsync_MultiplePatients_DeletesAll()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patients = new List<Patient>
        {
            CreateTestPatient("批量1"),
            CreateTestPatient("批量2"),
            CreateTestPatient("批量3")
        };
        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);
        var ids = patients.Select(p => p.Id).ToList();

        // Act
        var result = await dataSource.BatchDeleteAsync(ids);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalCount.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchDeleteAsync_SomeNonExisting_ReportsFailures()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var patient = CreateTestPatient("存在的患者");
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var dataSource = new LocalPatientDataSource(context, _logger);
        var ids = new List<Guid> { patient.Id, Guid.NewGuid() };

        // Act
        var result = await dataSource.BatchDeleteAsync(ids);

        // Assert
        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.SuccessfulIds.Should().Contain(patient.Id);
    }

    #endregion

    #region Helper Methods

    private static Patient CreateTestPatient(
        string name,
        string? phoneNumber = null,
        string? pinYinCode = null)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            Name = name,
            Gender = Gender.Male,
            PhoneNumber = phoneNumber ?? "13800000000",
            PinYinCode = pinYinCode ?? name.ToUpper(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    #endregion
}
