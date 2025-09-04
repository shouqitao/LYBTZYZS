using FluentAssertions;
using LYBT.Entities.MedicalCase;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Tests.Base;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Repositories;

/// <summary>
/// MedicalCaseRepository 单元测试
/// 测试医疗案例仓储的完整功能，包括缓存优化和业务方法
/// </summary>
public class MedicalCaseRepositoryTests : RepositoryTestBase
{
    private readonly MedicalCaseRepository _repository;
    private readonly IMemoryCache _cache;

    public MedicalCaseRepositoryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _repository = new MedicalCaseRepository(Context, NullLogger<MedicalCaseRepository>.Instance, _cache);
    }

    #region 基础CRUD操作测试

    [Fact]
    public async Task AddAsync_ShouldCreateMedicalCase_WhenValidEntity()
    {
        // Arrange
        var medicalCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase();

        // Act
        var result = await _repository.AddAsync(medicalCase);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(medicalCase.Id);
        
        var saved = await _repository.GetByIdAsync(medicalCase.Id);
        saved.Should().NotBeNull();
        saved!.PatientId.Should().Be(medicalCase.PatientId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMedicalCaseWithConsultation_WhenExists()
    {
        // Arrange
        var medicalCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase();
        await _repository.AddAsync(medicalCase);

        // Act
        var result = await _repository.GetByIdAsync(medicalCase.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(medicalCase.Id);
        result.PatientId.Should().Be(medicalCase.PatientId);
        // 验证Include关系
        result.Consultation.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyMedicalCase_WhenValidChanges()
    {
        // Arrange
        var medicalCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase();
        await _repository.AddAsync(medicalCase);

        var newStatus = MedicalCaseStatus.Completed;
        medicalCase.Status = newStatus;

        // Act
        var result = await _repository.UpdateAsync(medicalCase);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(newStatus);

        var updated = await _repository.GetByIdAsync(medicalCase.Id);
        updated!.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveMedicalCase_WhenExists()
    {
        // Arrange
        var medicalCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase();
        await _repository.AddAsync(medicalCase);

        // Act
        var result = await _repository.DeleteAsync(medicalCase.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _repository.GetByIdAsync(medicalCase.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 分页查询测试

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults_WhenValidParameters()
    {
        // Arrange
        var medicalCases = MedicalCaseTestDataGenerator.CreateTestMedicalCases(15);
        foreach (var mc in medicalCases)
        {
            await _repository.AddAsync(mc);
        }

        // Act
        var result = await _repository.GetPagedAsync(null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        // 验证默认按ConsultationDate降序排序
        result.Items.First().ConsultationDate.Should().BeAfter(result.Items.Last().ConsultationDate);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByPredicate_WhenProvided()
    {
        // Arrange
        var completedCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(status: MedicalCaseStatus.Completed);
        var inProgressCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(status: MedicalCaseStatus.InProgress);
        
        await _repository.AddAsync(completedCase);
        await _repository.AddAsync(inProgressCase);

        // Act
        var result = await _repository.GetPagedAsync(
            mc => mc.Status == MedicalCaseStatus.Completed, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(MedicalCaseStatus.Completed);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnEmptyResult_WhenNoData()
    {
        // Act
        var result = await _repository.GetPagedAsync(null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region 业务查询方法测试

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnPatientMedicalCases_WhenExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var medicalCases = MedicalCaseTestDataGenerator.CreateTestMedicalCasesForPatient(patientId, 3);
        var otherCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(); // 不同患者的案例

        foreach (var mc in medicalCases.Concat(new[] { otherCase }))
        {
            await _repository.AddAsync(mc);
        }

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(mc => mc.PatientId.Should().Be(patientId));
        // 验证按ConsultationDate降序排序
        result.Should().BeInDescendingOrder(mc => mc.ConsultationDate);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnEmpty_WhenNoMedicalCases()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByPatientIdAsync(nonExistentPatientId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnDoctorMedicalCases_WhenExists()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var medicalCases = MedicalCaseTestDataGenerator.CreateTestMedicalCasesForDoctor(doctorId, 3);
        var otherCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(); // 不同医生的案例

        foreach (var mc in medicalCases.Concat(new[] { otherCase }))
        {
            await _repository.AddAsync(mc);
        }

        // Act
        var result = await _repository.GetByUserIdAsync(doctorId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(mc => mc.DoctorId.Should().Be(doctorId));
        result.Should().BeInDescendingOrder(mc => mc.ConsultationDate);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnMedicalCasesByStatus_WhenExists()
    {
        // Arrange
        var targetStatus = MedicalCaseStatus.InProgress;
        var matchingCases = MedicalCaseTestDataGenerator.CreateTestMedicalCases(2, status: targetStatus);
        var nonMatchingCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(status: MedicalCaseStatus.Completed);

        foreach (var mc in matchingCases.Concat(new[] { nonMatchingCase }))
        {
            await _repository.AddAsync(mc);
        }

        // Act
        var result = await _repository.GetByStatusAsync(targetStatus);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(mc => mc.Status.Should().Be(targetStatus));
        result.Should().BeInDescendingOrder(mc => mc.ConsultationDate);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnMedicalCasesInRange_WhenExists()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today.AddDays(-1);
        var inRangeDate = startDate.AddDays(5);
        var outOfRangeDate = DateTime.Today.AddDays(-15);

        var inRangeCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(consultationDate: inRangeDate);
        var outOfRangeCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(consultationDate: outOfRangeDate);

        await _repository.AddAsync(inRangeCase);
        await _repository.AddAsync(outOfRangeCase);

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().ConsultationDate.Should().BeOnOrAfter(startDate);
        result.First().ConsultationDate.Should().BeOnOrBefore(endDate);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ShouldReturnMostRecentCase_WhenExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var olderDate = DateTime.Today.AddDays(-10);
        var newerDate = DateTime.Today.AddDays(-5);

        var olderCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(
            patientId: patientId, consultationDate: olderDate);
        var newerCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(
            patientId: patientId, consultationDate: newerDate);

        await _repository.AddAsync(olderCase);
        await _repository.AddAsync(newerCase);

        // Act
        var result = await _repository.GetLatestByPatientIdAsync(patientId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(newerCase.Id);
        result.ConsultationDate.Should().Be(newerDate);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ShouldReturnNull_WhenNoMedicalCases()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid();

        // Act
        var result = await _repository.GetLatestByPatientIdAsync(nonExistentPatientId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 缓存行为测试

    [Fact]
    public async Task GetByIdAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var medicalCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase();
        await _repository.AddAsync(medicalCase);

        // Act - 第一次调用，数据库查询
        var firstResult = await _repository.GetByIdAsync(medicalCase.Id);
        
        // 修改数据库中的数据
        var directEntity = Context.MedicalCases.First(mc => mc.Id == medicalCase.Id);
        directEntity.Status = MedicalCaseStatus.Completed;
        await Context.SaveChangesAsync();
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetByIdAsync(medicalCase.Id);

        // Assert
        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        // 缓存的数据应该与第一次查询相同，而不是修改后的数据
        secondResult!.Status.Should().Be(firstResult!.Status);
        secondResult.Status.Should().NotBe(MedicalCaseStatus.Completed);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var medicalCases = MedicalCaseTestDataGenerator.CreateTestMedicalCasesForPatient(patientId, 2);
        foreach (var mc in medicalCases)
        {
            await _repository.AddAsync(mc);
        }

        // Act - 第一次调用
        var firstResult = await _repository.GetByPatientIdAsync(patientId);
        
        // 添加新的医案
        var newCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase(patientId: patientId);
        await _repository.AddAsync(newCase);
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetByPatientIdAsync(patientId);

        // Assert
        firstResult.Should().HaveCount(2);
        secondResult.Should().HaveCount(2); // 应该与缓存中的数量相同
    }

    #endregion

    #region 边界条件和异常测试

    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task GetPagedAsync_ShouldHandleInvalidPageParameters()
    {
        // Arrange
        var medicalCases = MedicalCaseTestDataGenerator.CreateTestMedicalCases(5);
        foreach (var mc in medicalCases)
        {
            await _repository.AddAsync(mc);
        }

        // Act - 负数页面
        var negativePageResult = await _repository.GetPagedAsync(null, -1, 10);
        
        // Act - 零页面大小
        var zeroPageSizeResult = await _repository.GetPagedAsync(null, 1, 0);

        // Assert
        negativePageResult.Should().NotBeNull();
        negativePageResult.Items.Should().BeEmpty();
        
        zeroPageSizeResult.Should().NotBeNull();
        zeroPageSizeResult.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnEmpty_WhenInvalidDateRange()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(-1); // 结束日期早于开始日期
        
        var medicalCase = MedicalCaseTestDataGenerator.CreateTestMedicalCase();
        await _repository.AddAsync(medicalCase);

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache?.Dispose();
        }
        base.Dispose(disposing);
    }
}