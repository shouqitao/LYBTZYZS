using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Catalog.Interfaces;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// HERB-005 单删引用检查单测（AC-TEST P2-5: B1 新增 ValidateBeforeDeleteAsync——
/// 有处方/验方引用的药材拒绝删除）
/// </summary>
public class HerbReferenceCheckTests
{
    private readonly FakeHerbRepository _herbRepository;
    private readonly FakeHerbReferenceRepository _referenceRepository;
    private readonly HerbCommandHandler _handler;

    public HerbReferenceCheckTests()
    {
        _herbRepository = new FakeHerbRepository();
        _referenceRepository = new FakeHerbReferenceRepository();
        _handler = new HerbCommandHandler(_herbRepository, _referenceRepository);
    }

    private static Herb CreateHerb(string name = "黄芪")
        => new() { Id = Guid.NewGuid(), Name = name };

    [Fact]
    public async Task Delete_WithPrescriptionReferences_IsRejected()
    {
        var herb = CreateHerb();
        _herbRepository.ExistingHerb = herb;
        _referenceRepository.PrescriptionCount = 3;
        _referenceRepository.FormulaCount = 0;

        var result = await _handler.Handle(
            new DeleteEntityCommand<Herb>(herb.Id, Guid.NewGuid(), UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("3 条处方");
        _herbRepository.Updated.Should().BeNull("有引用时不得软删除");
    }

    [Fact]
    public async Task Delete_WithFormulaReferences_IsRejected()
    {
        var herb = CreateHerb("当归");
        _herbRepository.ExistingHerb = herb;
        _referenceRepository.PrescriptionCount = 0;
        _referenceRepository.FormulaCount = 2;

        var result = await _handler.Handle(
            new DeleteEntityCommand<Herb>(herb.Id, Guid.NewGuid(), UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2 个验方");
    }

    [Fact]
    public async Task Delete_WithBothReferences_MessageListsBoth()
    {
        var herb = CreateHerb();
        _herbRepository.ExistingHerb = herb;
        _referenceRepository.PrescriptionCount = 1;
        _referenceRepository.FormulaCount = 1;

        var result = await _handler.Handle(
            new DeleteEntityCommand<Herb>(herb.Id, Guid.NewGuid(), UserRole.Admin), CancellationToken.None);

        result.Error.Should().Contain("1 条处方").And.Contain("1 个验方");
    }

    [Fact]
    public async Task Delete_WithoutReferences_SoftDeletes()
    {
        var herb = CreateHerb();
        _herbRepository.ExistingHerb = herb;
        _referenceRepository.PrescriptionCount = 0;
        _referenceRepository.FormulaCount = 0;

        var result = await _handler.Handle(
            new DeleteEntityCommand<Herb>(herb.Id, Guid.NewGuid(), UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _herbRepository.Updated.Should().Be(herb);
        herb.IsDeleted.Should().BeTrue(); // 软删除
    }

    [Fact]
    public async Task Delete_NonexistentHerb_ReturnsNotFound()
    {
        _herbRepository.ExistingHerb = null;

        var result = await _handler.Handle(
            new DeleteEntityCommand<Herb>(Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

/// <summary>手写假仓储（Server 测试零 mock 框架约定）</summary>
internal sealed class FakeHerbRepository : IHerbRepository
{
    public Herb? ExistingHerb { get; set; }
    public Herb? Updated { get; private set; }
    public bool Exists { get; set; }

    public Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(ExistingHerb);
    public Task<Herb?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) => Task.FromResult(ExistingHerb);
    public Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => Task.FromResult(ids.ToDictionary(id => id, id => ExistingHerb?.Name ?? string.Empty));
    public Task<Herb> UpdateAsync(Herb entity, CancellationToken ct = default) { Updated = entity; return Task.FromResult(entity); }
    public Task<Herb> AddAsync(Herb entity, CancellationToken ct = default) => Task.FromResult(entity);
#pragma warning disable CS0618
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
#pragma warning restore CS0618
    public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(id, ct);
    public Task<bool> RestoreAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> HardDeleteAsync(Herb entity, CancellationToken ct = default) => Task.FromResult(true);
    public Task<PagedResult<Herb>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<Herb>());
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(Exists);
    public Task<Herb?> GetByNameAsync(string name, CancellationToken ct = default) => Task.FromResult(ExistingHerb);
}

internal sealed class FakeHerbReferenceRepository : IHerbReferenceRepository
{
    public int PrescriptionCount { get; set; }
    public int FormulaCount { get; set; }
    public Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct = default) => Task.FromResult(PrescriptionCount);
    public Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct = default) => Task.FromResult(FormulaCount);
    public Task<Dictionary<Guid, int>> GetBatchPrescriptionReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default)
        => Task.FromResult(herbIds.ToDictionary(id => id, _ => PrescriptionCount));
    public Task<Dictionary<Guid, int>> GetBatchFormulaReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default)
        => Task.FromResult(herbIds.ToDictionary(id => id, _ => FormulaCount));
    public Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct = default)
        => Task.FromResult(new List<PrescriptionReferenceDto>());
}
