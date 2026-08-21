using FluentAssertions;
using LYBT.Module.Catalog.Interfaces;
using System.Reflection;

namespace LYBT.Tests.Server.Unit.Catalog;

/// <summary>
/// P1-11: 批量名称查询契约（CheckHerbReferenceQuery 批量路径——单次 IN 避免 N+1）
/// </summary>
public class HerbBatchNameLookupTests
{
    [Fact]
    public void IHerbRepository_Exposes_GetNamesByIdsAsync()
    {
        var m = typeof(IHerbRepository).GetMethod("GetNamesByIdsAsync");
        m.Should().NotBeNull("批量引用检查需 GetNamesByIdsAsync（单次 IN）");
        m!.ReturnType.Should().Be(typeof(Task<Dictionary<Guid, string>>));
    }

    [Fact]
    public void ConcreteHerbRepository_Implements_GetNamesByIdsAsync()
    {
        // P1-11：具体仓储已实现批量名称查询（单次 IN），批量引用检查据此避免 N+1
        var repoType = typeof(LYBT.Module.Catalog.Infrastructure.HerbRepository);
        var m = repoType.GetMethod("GetNamesByIdsAsync");
        m.Should().NotBeNull("HerbRepository 应实现 GetNamesByIdsAsync（P1-11）");
        m!.ReturnType.Should().Be(typeof(Task<Dictionary<Guid, string>>));
    }

    [Fact]
    public void BatchHandler_Still_Exposes_Both_Handle_Overloads()
    {
        var h = typeof(LYBT.Module.Catalog.Application.Queries.CheckHerbReferenceQueryHandler);
        var single = h.GetMethod("Handle", [typeof(LYBT.Module.Catalog.Application.Queries.CheckHerbReferenceQuery), typeof(CancellationToken)]);
        var batch = h.GetMethod("Handle", [typeof(LYBT.Module.Catalog.Application.Queries.BatchCheckHerbReferenceQuery), typeof(CancellationToken)]);
        single.Should().NotBeNull();
        batch.Should().NotBeNull();
    }
}
