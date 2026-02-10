namespace LYBT.Tests.Server.Integration.Fixtures;

/// <summary>
/// Server端集成测试共享Collection。
/// 所有标记[Collection("ServerIntegration")]的测试类共享同一个WebApiFixture实例。
/// 避免多个WebApplicationFactory实例引起的Serilog冻结和DB冲突。
/// </summary>
[CollectionDefinition("ServerIntegration")]
public class ServerIntegrationCollection : ICollectionFixture<WebApiFixture>
{
    // 此类不包含代码，仅用于CollectionDefinition标记
}
