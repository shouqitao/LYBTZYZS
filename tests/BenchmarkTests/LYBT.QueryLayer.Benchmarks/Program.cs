using BenchmarkDotNet.Running;
using LYBT.QueryLayer.Benchmarks;

// 运行基准测试
// 使用命令: dotnet run -c Release
BenchmarkRunner.Run<BatchOperationsBenchmark>();
