using Xunit;

namespace LYBT.Tests.Desktop.Integration.Fixtures;

/// <summary>
/// xUnit Collection 定义，用于 WebApi 集成测试
/// 
/// 使用此 Collection 确保测试类之间共享同一个 WebApiFixture 实例
/// 避免重复启动 WebApplicationFactory，提高测试性能
/// </summary>
[CollectionDefinition("WebApiIntegration")]
public class WebApiCollection : ICollectionFixture<WebApiFixture>
{
    // ICollectionFixture 接口的实现由 xUnit 自动处理
    // 无需额外代码，只需标记 Collection 名称
}
