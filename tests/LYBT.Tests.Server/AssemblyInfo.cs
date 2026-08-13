using Xunit;

// 2026-08-13（observability-completion）: Serilog 全局 ReloadableLogger 与多宿主测试并行冲突
// （"The logger is already frozen"——flaky ~1/3）——程序集级禁并行根治。
// 代价：Server 测试串行（~55s→~2min）——门禁稳定性优先。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
