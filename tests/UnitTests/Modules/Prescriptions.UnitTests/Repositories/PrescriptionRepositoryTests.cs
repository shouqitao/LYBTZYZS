using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Tests.Base;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Repositories;

/// <summary>
/// PrescriptionRepository 单元测试
/// 测试处方仓储的完整功能，包括缓存优化和业务方法
/// </summary>
public class PrescriptionRepositoryTests : RepositoryTestBase
{
    private readonly PrescriptionRepository _repository;
    private readonly IMemoryCache _cache;

    public PrescriptionRepositoryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _repository = new PrescriptionRepository(Context, NullLogger<PrescriptionRepository>.Instance, _cache);
    }

    #region 基础CRUD操作测试

    [Fact]
    public async Task AddAsync_ShouldCreatePrescription_WhenValidEntity()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescription();

        // Act
        var result = await _repository.AddAsync(prescription);

        // Assert
        result.Should().BeTrue();
        
        var saved = await _repository.GetByIdAsync(prescription.Id);
        saved.Should().NotBeNull();
        saved!.PatientName.Should().Be(prescription.PatientName);
        saved.DoctorName.Should().Be(prescription.DoctorName);
    }

    [Fact]
    public async Task AddAsync_ShouldCreatePrescriptionWithItems_WhenHasItems()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescriptionWithItems(3);

        // Act
        var result = await _repository.AddAsync(prescription);

        // Assert
        result.Should().BeTrue();
        
        var saved = await _repository.GetByIdAsync(prescription.Id);
        saved.Should().NotBeNull();
        saved!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPrescriptionWithItems_WhenExists()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescriptionWithItems(2);
        await _repository.AddAsync(prescription);

        // Act
        var result = await _repository.GetByIdAsync(prescription.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(prescription.Id);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item => 
            item.PrescriptionId.Should().Be(prescription.Id));
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
    public async Task UpdateAsync_ShouldModifyPrescription_WhenValidChanges()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescription();
        await _repository.AddAsync(prescription);

        var newStatus = PrescriptionStatus.Completed;
        var newNotes = "更新后的处方备注";
        prescription.Status = newStatus;
        prescription.Notes = newNotes;

        // Act
        var result = await _repository.UpdateAsync(prescription);

        // Assert
        result.Should().BeTrue();

        var updated = await _repository.GetByIdAsync(prescription.Id);
        updated!.Status.Should().Be(newStatus);
        updated.Notes.Should().Be(newNotes);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePrescription_WhenExists()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescription();
        await _repository.AddAsync(prescription);

        // Act
        var result = await _repository.DeleteAsync(prescription.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _repository.GetByIdAsync(prescription.Id);
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

    #region 列表查询测试

    [Fact]
    public async Task GetListAsync_ShouldReturnAllPrescriptionsWithItems_WhenDataExists()
    {
        // Arrange
        var prescriptions = PrescriptionTestDataGenerator.CreateTestPrescriptions(3);
        foreach (var prescription in prescriptions)
        {
            await _repository.AddAsync(prescription);
        }

        // Act
        var result = await _repository.GetListAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.Items.Should().NotBeNull());
    }

    [Fact]
    public async Task GetListAsync_ShouldReturnEmpty_WhenNoData()
    {
        // Act
        var result = await _repository.GetListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListAsync_ShouldIncludePrescriptionItems_WhenExists()
    {
        // Arrange
        var prescriptionWithItems = PrescriptionTestDataGenerator.CreateTestPrescriptionWithItems(4);
        await _repository.AddAsync(prescriptionWithItems);

        // Act
        var result = await _repository.GetListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Items.Should().HaveCount(4);
    }

    #endregion

    #region 业务操作测试

    [Fact]
    public async Task CancelAsync_ShouldSetStatusToDraft_WhenValidId()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescription(
            status: PrescriptionStatus.Completed);
        await _repository.AddAsync(prescription);

        // Act
        var result = await _repository.CancelAsync(prescription.Id);

        // Assert
        result.Should().BeTrue();

        var cancelled = await _repository.GetByIdAsync(prescription.Id);
        cancelled!.Status.Should().Be(PrescriptionStatus.Draft);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnFalse_WhenPrescriptionNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.CancelAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelAsync_ShouldWorkWithAnyStatus_WhenValidId()
    {
        // Arrange
        var statuses = new[] { 
            PrescriptionStatus.Draft, 
            PrescriptionStatus.InProgress, 
            PrescriptionStatus.Completed 
        };

        foreach (var status in statuses)
        {
            var prescription = PrescriptionTestDataGenerator.CreateTestPrescription(status: status);
            await _repository.AddAsync(prescription);

            // Act
            var result = await _repository.CancelAsync(prescription.Id);

            // Assert
            result.Should().BeTrue();
            var cancelled = await _repository.GetByIdAsync(prescription.Id);
            cancelled!.Status.Should().Be(PrescriptionStatus.Draft);
        }
    }

    #endregion

    #region 缓存行为测试

    [Fact]
    public async Task GetByIdAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescription();
        await _repository.AddAsync(prescription);

        // Act - 第一次调用，数据库查询
        var firstResult = await _repository.GetByIdAsync(prescription.Id);
        
        // 修改数据库中的数据
        var directEntity = Context.Prescriptions.First(p => p.Id == prescription.Id);
        directEntity.Notes = "数据库直接修改";
        await Context.SaveChangesAsync();
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetByIdAsync(prescription.Id);

        // Assert
        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        // 缓存的数据应该与第一次查询相同
        secondResult!.Notes.Should().Be(firstResult!.Notes);
        secondResult.Notes.Should().NotBe("数据库直接修改");
    }

    [Fact]
    public async Task GetListAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var prescriptions = PrescriptionTestDataGenerator.CreateTestPrescriptions(2);
        foreach (var prescription in prescriptions)
        {
            await _repository.AddAsync(prescription);
        }

        // Act - 第一次调用
        var firstResult = await _repository.GetListAsync();
        
        // 添加新的处方
        var newPrescription = PrescriptionTestDataGenerator.CreateTestPrescription();
        await _repository.AddAsync(newPrescription);
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetListAsync();

        // Assert
        firstResult.Should().HaveCount(2);
        secondResult.Should().HaveCount(2); // 应该与缓存中的数量相同
    }

    [Fact]
    public async Task Cache_ShouldStoreWithCorrectKey_ForGetById()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescription();
        await _repository.AddAsync(prescription);

        // Act
        await _repository.GetByIdAsync(prescription.Id);
        
        // Assert - 验证缓存键是否正确
        var expectedCacheKey = $"Prescription_withItems:{prescription.Id}";
        var cached = _cache.Get<Prescription>(expectedCacheKey);
        cached.Should().NotBeNull();
        cached!.Id.Should().Be(prescription.Id);
    }

    #endregion

    #region 复杂数据场景测试

    [Fact]
    public async Task AddAsync_ShouldHandlePrescriptionWithManyItems_Successfully()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescriptionWithItems(10);

        // Act
        var result = await _repository.AddAsync(prescription);

        // Assert
        result.Should().BeTrue();

        var saved = await _repository.GetByIdAsync(prescription.Id);
        saved!.Items.Should().HaveCount(10);
        saved.Items.Should().AllSatisfy(item =>
        {
            item.HerbName.Should().NotBeNullOrEmpty();
            item.Dosage.Should().BeGreaterThan(0);
            item.Unit.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldHandleItemChanges_WhenItemsModified()
    {
        // Arrange
        var prescription = PrescriptionTestDataGenerator.CreateTestPrescriptionWithItems(3);
        await _repository.AddAsync(prescription);

        // 修改处方项
        prescription.Items.First().Dosage = 999;
        prescription.Items.First().Notes = "修改后的备注";

        // Act
        var result = await _repository.UpdateAsync(prescription);

        // Assert
        result.Should().BeTrue();

        var updated = await _repository.GetByIdAsync(prescription.Id);
        var modifiedItem = updated!.Items.First();
        modifiedItem.Dosage.Should().Be(999);
        modifiedItem.Notes.Should().Be("修改后的备注");
    }

    [Fact]
    public async Task GetListAsync_ShouldHandleDifferentPrescriptionTypes()
    {
        // Arrange
        var draftPrescription = PrescriptionTestDataGenerator.CreateTestPrescription(
            status: PrescriptionStatus.Draft);
        var completedPrescription = PrescriptionTestDataGenerator.CreateTestPrescription(
            status: PrescriptionStatus.Completed);
        var inProgressPrescription = PrescriptionTestDataGenerator.CreateTestPrescription(
            status: PrescriptionStatus.InProgress);

        await _repository.AddAsync(draftPrescription);
        await _repository.AddAsync(completedPrescription);
        await _repository.AddAsync(inProgressPrescription);

        // Act
        var result = await _repository.GetListAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.Status == PrescriptionStatus.Draft);
        result.Should().Contain(p => p.Status == PrescriptionStatus.Completed);
        result.Should().Contain(p => p.Status == PrescriptionStatus.InProgress);
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
    public async Task GetByIdAsync_ShouldHandleInvalidGuid()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_ShouldHandleInvalidGuid()
    {
        // Act
        var result = await _repository.CancelAsync(Guid.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Repository_ShouldHandleMultipleConcurrentOperations()
    {
        // Arrange
        var prescriptions = PrescriptionTestDataGenerator.CreateTestPrescriptions(5);

        // Act - 并发添加
        var addTasks = prescriptions.Select(p => _repository.AddAsync(p));
        var results = await Task.WhenAll(addTasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());

        var allPrescriptions = await _repository.GetListAsync();
        allPrescriptions.Should().HaveCount(5);
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