// ---------------------------------------------------------------------------
// E2ECollectionFixture — E2E 测试集合定义（禁用并行）
// ---------------------------------------------------------------------------
// 每个 E2E 测试类自带独立 LocalDB（IAsyncLifetime 每类建/删库），
// 集合级禁用并行避免 LocalDB 争用；xunit.runner.json 亦已全局关闭并行（双保险）。
// ---------------------------------------------------------------------------

namespace LYBT.Tests.Desktop.E2E;

[CollectionDefinition("E2ELocal", DisableParallelization = true)]
public class E2ELocalCollection
{
}
