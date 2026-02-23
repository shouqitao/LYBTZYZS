using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Infrastructure.Tests.Services;

/// <summary>
/// CrossModuleService 单元测试
/// OpenSpec: decouple-server-modules - Phase 1 Task 1.9-1.13
/// 测试跨模块只读查询服务
/// </summary>
public class CrossModuleServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly CrossModuleService _service;

    public CrossModuleServiceTests()
    {
        // 使用InMemory数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _service = new CrossModuleService(_dbContext, NullLogger<CrossModuleService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region 患者查询测试

    [Fact]
    public async Task GetPatientBasicInfoAsync_WithExistingPatient_ShouldReturnDto()
    {
        // Arrange
        var patient = CreateTestPatient();
        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientBasicInfoAsync(patient.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(patient.Id);
        result.Name.Should().Be(patient.Name);
        result.Gender.Should().Be(patient.Gender);
        result.Phone.Should().Be(patient.PhoneNumber);
    }

    [Fact]
    public async Task GetPatientBasicInfoAsync_WithNonExistingPatient_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _service.GetPatientBasicInfoAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPatientBasicInfoAsync_WithDeletedPatient_ShouldReturnNull()
    {
        // Arrange
        var patient = CreateTestPatient();
        patient.IsDeleted = true;
        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientBasicInfoAsync(patient.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPatientsBasicInfoAsync_WithMultiplePatients_ShouldReturnDictionary()
    {
        // Arrange
        var patients = new[]
        {
            CreateTestPatient("张三", Gender.Male),
            CreateTestPatient("李四", Gender.Female),
            CreateTestPatient("王五", Gender.Unknown)
        };
        _dbContext.Patients.AddRange(patients);
        await _dbContext.SaveChangesAsync();

        var ids = patients.Select(p => p.Id).ToList();

        // Act
        var result = await _service.GetPatientsBasicInfoAsync(ids);

        // Assert
        result.Should().HaveCount(3);
        result.Keys.Should().BeEquivalentTo(ids);
        result[patients[0].Id].Name.Should().Be("张三");
        result[patients[1].Id].Name.Should().Be("李四");
    }

    [Fact]
    public async Task GetPatientsBasicInfoAsync_WithEmptyList_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var emptyIds = new List<Guid>();

        // Act
        var result = await _service.GetPatientsBasicInfoAsync(emptyIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPatientsBasicInfoAsync_ShouldExcludeDeletedPatients()
    {
        // Arrange
        var activePatient = CreateTestPatient("活跃患者", Gender.Male);
        var deletedPatient = CreateTestPatient("已删除患者", Gender.Female);
        deletedPatient.IsDeleted = true;

        _dbContext.Patients.AddRange(activePatient, deletedPatient);
        await _dbContext.SaveChangesAsync();

        var ids = new[] { activePatient.Id, deletedPatient.Id };

        // Act
        var result = await _service.GetPatientsBasicInfoAsync(ids);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey(activePatient.Id);
        result.Should().NotContainKey(deletedPatient.Id);
    }

    #endregion

    #region 药材查询测试

    [Fact]
    public async Task GetHerbBasicInfoAsync_WithExistingHerb_ShouldReturnDto()
    {
        // Arrange
        var herb = CreateTestHerb("黄芪", "HQ");
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetHerbBasicInfoAsync(herb.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(herb.Id);
        result.Name.Should().Be("黄芪");
        result.Pinyin.Should().Be("HQ");
        result.Category.Should().Be(herb.Category);
    }

    [Fact]
    public async Task GetHerbBasicInfoAsync_WithNonExistingHerb_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _service.GetHerbBasicInfoAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHerbByNameOrPinyinAsync_WithMatchingName_ShouldReturnDto()
    {
        // Arrange
        var herb = CreateTestHerb("当归", "DG");
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetHerbByNameOrPinyinAsync("当归");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("当归");
    }

    [Fact]
    public async Task GetHerbByNameOrPinyinAsync_WithMatchingPinyin_ShouldReturnDto()
    {
        // Arrange
        var herb = CreateTestHerb("人参", "RS");
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetHerbByNameOrPinyinAsync("RS");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("人参");
        result.Pinyin.Should().Be("RS");
    }

    [Fact]
    public async Task GetHerbByNameOrPinyinAsync_WithNoMatch_ShouldReturnNull()
    {
        // Arrange
        var herb = CreateTestHerb("甘草", "GC");
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetHerbByNameOrPinyinAsync("不存在的药材");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHerbByNameOrPinyinAsync_WithNullOrEmpty_ShouldReturnNull()
    {
        // Act & Assert
        var resultNull = await _service.GetHerbByNameOrPinyinAsync(null!);
        resultNull.Should().BeNull();

        var resultEmpty = await _service.GetHerbByNameOrPinyinAsync("");
        resultEmpty.Should().BeNull();

        var resultWhitespace = await _service.GetHerbByNameOrPinyinAsync("   ");
        resultWhitespace.Should().BeNull();
    }

    [Fact]
    public async Task GetHerbByNameOrPinyinAsync_WithDeletedHerb_ShouldReturnNull()
    {
        // Arrange
        var herb = CreateTestHerb("茯苓", "FL");
        herb.IsDeleted = true;
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetHerbByNameOrPinyinAsync("茯苓");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 辅助方法

    private static Patient CreateTestPatient(string name = "测试患者", Gender gender = Gender.Unknown)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            Name = name,
            Gender = gender,
            PhoneNumber = "13800000000",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };
    }

    private static Herb CreateTestHerb(string name, string pinyin)
    {
        return new Herb
        {
            Id = Guid.NewGuid(),
            Name = name,
            PinYinCode = pinyin,
            Category = "补益药",
            Unit = "克",
            Price = 10.0m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };
    }

    #endregion
}
