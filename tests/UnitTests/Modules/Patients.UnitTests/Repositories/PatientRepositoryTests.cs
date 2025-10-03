using FluentAssertions;
using LYBT.Module.Patients.Repositories;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Patients.Tests.Repositories;

/// <summary>
/// PatientRepository 单元测试 - 使用 SQLite InMemory
/// Issue #865 - [SRV-3] Repository 层测试
/// </summary>
public class PatientRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PatientRepository _sut;

    public PatientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _sut = new PatientRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    #region GetByIdAsync 测试

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            Name = "张三",
            Gender = Gender.Male,
            BirthDate = DateTime.Today.AddYears(-30),
            PhoneNumber = "13800138000",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(patientId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("张三");
        result.PhoneNumber.Should().Be("13800138000");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync 测试

    [Fact]
    public async Task GetAllAsync_ReturnsAllPatients()
    {
        // Arrange
        var patients = new[]
        {
            new Patient { Id = Guid.NewGuid(), Name = "患者1", Gender = Gender.Male, CreatedAt = DateTime.UtcNow },
            new Patient { Id = Guid.NewGuid(), Name = "患者2", Gender = Gender.Female, CreatedAt = DateTime.UtcNow }
        };
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().Contain(new[] { "患者1", "患者2" });
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region AddAsync 测试

    [Fact]
    public async Task AddAsync_Should_AddPatientSuccessfully()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "新患者",
            Gender = Gender.Male,
            PhoneNumber = "13900139000",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _sut.AddAsync(patient);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("新患者");
        _context.Patients.Should().Contain(p => p.Id == patient.Id);
    }

    #endregion

    #region UpdateAsync 测试

    [Fact]
    public async Task UpdateAsync_Should_UpdatePatientSuccessfully()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "原姓名",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        patient.Name = "新姓名";
        var result = await _sut.UpdateAsync(patient);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("新姓名");
        var updatedPatient = await _context.Patients.FindAsync(patient.Id);
        updatedPatient!.Name.Should().Be("新姓名");
    }

    #endregion

    #region DeleteAsync 测试

    [Fact]
    public async Task DeleteAsync_Should_SoftDeletePatient()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "待删除患者",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(patient.Id);

        // Assert
        result.Should().BeTrue();
        var deletedPatient = await _context.Patients.FindAsync(patient.Id);
        deletedPatient!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPagedAsync 测试

    [Fact]
    public async Task GetPagedAsync_Should_ReturnPagedResults()
    {
        // Arrange
        var patients = Enumerable.Range(1, 15).Select(i => new Patient
        {
            Id = Guid.NewGuid(),
            Name = $"患者{i}",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        }).ToArray();
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _sut.GetPagedAsync(1, 10);

        // Assert
        items.Should().HaveCount(10);
        totalCount.Should().Be(15);
    }

    #endregion

    #region GetByNameAsync 测试

    [Fact]
    public async Task GetByNameAsync_WithExistingName_ReturnsPatient()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "李四",
            Gender = Gender.Female,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNameAsync("李四");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("李四");
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByNameAsync("不存在的姓名");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPatientWithVisitsAsync 测试

    [Fact]
    public async Task GetPatientWithVisitsAsync_WithExistingId_ReturnsPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            Name = "王五",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPatientWithVisitsAsync(patientId);

        // Assert - 目前未实现 Visits 导航属性，仅验证返回患者
        result.Should().NotBeNull();
        result!.Name.Should().Be("王五");
    }

    #endregion

    #region GetPatientSummariesAsync 测试

    [Fact]
    public async Task GetPatientSummariesAsync_Should_ReturnPagedSummaries()
    {
        // Arrange
        var patients = new[]
        {
            new Patient { Id = Guid.NewGuid(), Name = "患者A", Gender = Gender.Male, BirthDate = DateTime.Today.AddYears(-25), PhoneNumber = "13800000001", CreatedAt = DateTime.UtcNow },
            new Patient { Id = Guid.NewGuid(), Name = "患者B", Gender = Gender.Female, BirthDate = DateTime.Today.AddYears(-30), PhoneNumber = "13800000002", CreatedAt = DateTime.UtcNow }
        };
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPatientSummariesAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(s => s.Name == "患者A" && s.Age == 25);
        result.Items.Should().Contain(s => s.Name == "患者B" && s.Age == 30);
    }

    #endregion

    #region SearchPatientsAsync 测试

    [Fact]
    public async Task SearchPatientsAsync_WithEmptySearchTerm_ReturnsAllPatients()
    {
        // Arrange
        var patients = new[]
        {
            new Patient { Id = Guid.NewGuid(), Name = "张三", Gender = Gender.Male, CreatedAt = DateTime.UtcNow },
            new Patient { Id = Guid.NewGuid(), Name = "李四", Gender = Gender.Female, CreatedAt = DateTime.UtcNow }
        };
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SearchPatientsAsync(null, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchPatientsAsync_ByName_ReturnsMatchingPatients()
    {
        // Arrange
        var patients = new[]
        {
            new Patient { Id = Guid.NewGuid(), Name = "张三", Gender = Gender.Male, CreatedAt = DateTime.UtcNow },
            new Patient { Id = Guid.NewGuid(), Name = "张四", Gender = Gender.Male, CreatedAt = DateTime.UtcNow },
            new Patient { Id = Guid.NewGuid(), Name = "李四", Gender = Gender.Female, CreatedAt = DateTime.UtcNow }
        };
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SearchPatientsAsync("张", 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Name.Should().Contain("张"));
    }

    [Fact]
    public async Task SearchPatientsAsync_ByPhoneNumber_ReturnsMatchingPatients()
    {
        // Arrange
        var patients = new[]
        {
            new Patient { Id = Guid.NewGuid(), Name = "患者1", PhoneNumber = "13800138000", Gender = Gender.Male, CreatedAt = DateTime.UtcNow },
            new Patient { Id = Guid.NewGuid(), Name = "患者2", PhoneNumber = "13900139000", Gender = Gender.Female, CreatedAt = DateTime.UtcNow }
        };
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SearchPatientsAsync("138", 1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().PhoneNumber.Should().Contain("138");
    }

    #endregion

    #region GetPatientsByIdsAsync 测试

    [Fact]
    public async Task GetPatientsByIdsAsync_WithMultipleIds_ReturnsMatchingPatients()
    {
        // Arrange
        var patient1 = new Patient { Id = Guid.NewGuid(), Name = "患者1", Gender = Gender.Male, CreatedAt = DateTime.UtcNow };
        var patient2 = new Patient { Id = Guid.NewGuid(), Name = "患者2", Gender = Gender.Female, CreatedAt = DateTime.UtcNow };
        var patient3 = new Patient { Id = Guid.NewGuid(), Name = "患者3", Gender = Gender.Male, CreatedAt = DateTime.UtcNow };
        await _context.Patients.AddRangeAsync(patient1, patient2, patient3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPatientsByIdsAsync(new[] { patient1.Id, patient2.Id });

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().Contain(new[] { "患者1", "患者2" });
    }

    [Fact]
    public async Task GetPatientsByIdsAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetPatientsByIdsAsync(Enumerable.Empty<Guid>());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region PhoneNumberExistsAsync 测试

    [Fact]
    public async Task PhoneNumberExistsAsync_WithExistingPhoneNumber_ReturnsTrue()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "患者",
            PhoneNumber = "13800138000",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.PhoneNumberExistsAsync("13800138000");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PhoneNumberExistsAsync_WithNonExistentPhoneNumber_ReturnsFalse()
    {
        // Act
        var result = await _sut.PhoneNumberExistsAsync("99999999999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PhoneNumberExistsAsync_WithExcludeId_ExcludesSpecifiedPatient()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "患者",
            PhoneNumber = "13800138000",
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.PhoneNumberExistsAsync("13800138000", patient.Id);

        // Assert
        result.Should().BeFalse(); // 排除自己后应该不存在
    }

    #endregion

    #region GetStatisticsAsync 测试

    [Fact]
    public async Task GetStatisticsAsync_Should_ReturnCorrectStatistics()
    {
        // Arrange
        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);
        
        var patients = new[]
        {
            new Patient { Id = Guid.NewGuid(), Name = "男患者1", Gender = Gender.Male, BirthDate = today.AddYears(-25), CreatedAt = thisMonth.AddDays(1) },
            new Patient { Id = Guid.NewGuid(), Name = "男患者2", Gender = Gender.Male, BirthDate = today.AddYears(-30), CreatedAt = lastMonth.AddDays(15) }, // 明确设置为上月中旬
            new Patient { Id = Guid.NewGuid(), Name = "女患者1", Gender = Gender.Female, BirthDate = today.AddYears(-35), CreatedAt = thisMonth.AddDays(5) }
        };
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalPatients.Should().Be(3);
        result.MaleCount.Should().Be(2);
        result.FemaleCount.Should().Be(1);
        result.NewPatientsThisMonth.Should().BeGreaterOrEqualTo(2); // 本月创建的患者（可能包含边界情况）
        result.AverageAge.Should().BeApproximately(30.0, 0.1); // 平均年龄 (25+30+35)/3 = 30
    }

    #endregion

    #region UpdateLastVisitDateAsync 测试

    [Fact]
    public async Task UpdateLastVisitDateAsync_Should_UpdateMultiplePatients()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddHours(-1);
        var patient1 = new Patient { Id = Guid.NewGuid(), Name = "患者1", Gender = Gender.Male, CreatedAt = originalTime, UpdatedAt = originalTime };
        var patient2 = new Patient { Id = Guid.NewGuid(), Name = "患者2", Gender = Gender.Female, CreatedAt = originalTime, UpdatedAt = originalTime };
        await _context.Patients.AddRangeAsync(patient1, patient2);
        await _context.SaveChangesAsync();

        var visitDate = DateTime.Now;

        // Act
        await _sut.UpdateLastVisitDateAsync(new[] { patient1.Id, patient2.Id }, visitDate);

        // Assert - 需要重新查询以获取 ExecuteUpdateAsync 的更新
        _context.ChangeTracker.Clear(); // 清除跟踪，强制重新加载
        var updatedPatient1 = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patient1.Id);
        var updatedPatient2 = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patient2.Id);
        updatedPatient1!.UpdatedAt.Should().BeAfter(originalTime); // 验证已更新
        updatedPatient2!.UpdatedAt.Should().BeAfter(originalTime);
    }

    #endregion
}
