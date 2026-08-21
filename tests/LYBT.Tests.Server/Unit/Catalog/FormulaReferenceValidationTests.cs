using FluentAssertions;
using FluentValidation;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Module.Catalog.Application.Validators;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Server.Unit.Catalog;

/// <summary>
/// 引用校验缺口修复（任务书 formula-realsql-fix-2026-08-13）:
/// 创建验方——空 herbs → 400（Validator）; herbId 不存在/已删除 → 422（Handler 引用校验）
/// </summary>
public class FormulaReferenceValidationTests
{
    private readonly FakeFormulaRepository _formulaRepository = new();
    private readonly FakeHerbRepository _herbRepository = new();
    private readonly FormulaCommandHandler _handler;

    public FormulaReferenceValidationTests()
    {
        _handler = new FormulaCommandHandler(_formulaRepository, _herbRepository, NullLogger<FormulaCommandHandler>.Instance);
    }

    [Fact]
    public async Task Create_WithEmptyHerbs_IsRejectedByValidator()
    {
        var validator = new CreateFormulaValidator();
        var command = new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(
            new FormulaInputDto { Name = "空组成方", Herbs = new List<FormulaHerbItemInputDto>() },
            Guid.NewGuid());

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Herbs") && e.ErrorMessage.Contains("至少一味"));
    }

    [Fact]
    public async Task Create_WithUnknownHerbId_Returns422FailureWithMessage()
    {
        var input = new FormulaInputDto
        {
            Name = "引用不存在方",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "幽灵草", HerbId = Guid.NewGuid(), Dosage = 5, Unit = "g" },
            },
        };

        var result = await _handler.Handle(
            new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(input, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.FormulaValidationFailed); // 422（映射表 FormulaValidationFailed → 422）
        result.Error.Should().Contain("不存在的药材");
    }

    [Fact]
    public async Task Create_WithDeletedHerbId_Returns422FailureWithMessage()
    {
        var deletedHerb = Herb.Create(name: "已删药", unit: "g", price: 1m, pinYinCode: "YS");
        deletedHerb.SoftDelete(Guid.NewGuid());
        _herbRepository.ExistingHerb = deletedHerb;

        var input = new FormulaInputDto
        {
            Name = "引用已删方",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "已删药", HerbId = deletedHerb.Id, Dosage = 5, Unit = "g" },
            },
        };

        var result = await _handler.Handle(
            new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(input, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.FormulaValidationFailed); // 422
        result.Error.Should().Contain("已删除的药材");
    }
}

internal sealed class FakeHerbRepository : IHerbRepository
{
    public Herb? ExistingHerb { get; set; }

    public Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(ExistingHerb);
    public Task<Herb?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) => Task.FromResult(ExistingHerb);
    public Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => Task.FromResult(ids.ToDictionary(id => id, id => ExistingHerb?.Name ?? string.Empty));
    public Task<Herb> UpdateAsync(Herb entity, CancellationToken ct = default) => Task.FromResult(entity);
    public Task<Herb> AddAsync(Herb entity, CancellationToken ct = default) => Task.FromResult(entity);
#pragma warning disable CS0618
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
#pragma warning restore CS0618
    public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(id, ct);
    public Task<bool> RestoreAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> HardDeleteAsync(Herb entity, CancellationToken ct = default) => Task.FromResult(true);
    public Task<PagedResult<Herb>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<Herb>());
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
    public Task<Herb?> GetByNameAsync(string name, CancellationToken ct = default) => Task.FromResult(ExistingHerb);
}

internal sealed class FakeFormulaRepository : IFormulaRepository
{
    public Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Formula?>(null);
    public Task<Formula?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Formula?>(null);
    public Task<Formula> UpdateAsync(Formula entity, CancellationToken ct = default) => Task.FromResult(entity);
    public Task<Formula> AddAsync(Formula entity, CancellationToken ct = default) => Task.FromResult(entity);
#pragma warning disable CS0618
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
#pragma warning restore CS0618
    public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(id, ct);
    public Task<bool> RestoreAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> HardDeleteAsync(Formula entity, CancellationToken ct = default) => Task.FromResult(true);
    public Task<PagedResult<Formula>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<Formula>());
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
    public Task<List<Formula>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<Formula, bool>> predicate, CancellationToken ct = default)
        => Task.FromResult(new List<Formula>());
}
