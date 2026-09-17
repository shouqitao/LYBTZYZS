using Xunit;

namespace LYBT.Tests.Server._Infrastructure;

/// <summary>
/// SQL Server 集成测试串行集合 — 共享 LYBT_Test 库 + Respawn 清库，
/// 并行会互相清表导致偶发失败，故禁用并行。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class SqlServerIntegrationCollection
{
    public const string Name = "SqlServerIntegration";
}
