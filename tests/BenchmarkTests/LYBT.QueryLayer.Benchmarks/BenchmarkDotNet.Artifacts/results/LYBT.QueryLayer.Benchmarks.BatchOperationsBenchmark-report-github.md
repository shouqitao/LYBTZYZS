```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.19044.6691/21H2/November2021Update)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.414
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-LCABIO : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

IterationCount=5  WarmupCount=2  

```
| Method                   | BatchSize | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------- |---------- |-----------:|------------:|----------:|------:|--------:|---------:|--------:|----------:|------------:|
| Delete_Batch_Pattern     | 5         |   182.6 μs |    36.92 μs |   5.71 μs |  0.11 |    0.02 |   7.8125 |       - |  32.15 KB |        0.07 |
| Disable_Batch_Pattern    | 5         |   209.4 μs |    23.63 μs |   6.14 μs |  0.13 |    0.02 |   7.8125 |       - |  32.93 KB |        0.07 |
| Delete_N_Plus_1_Pattern  | 5         | 1,631.1 μs |   851.62 μs | 221.16 μs |  1.02 |    0.18 | 113.2813 |       - | 472.21 KB |        1.00 |
| Disable_N_Plus_1_Pattern | 5         | 1,639.5 μs |   905.38 μs | 235.13 μs |  1.02 |    0.19 | 113.2813 |       - | 471.72 KB |        1.00 |
|                          |           |            |             |           |       |         |          |         |           |             |
| Delete_Batch_Pattern     | 10        |   188.0 μs |    40.11 μs |  10.42 μs |  0.12 |    0.02 |   8.3008 |       - |  35.51 KB |        0.07 |
| Disable_Batch_Pattern    | 10        |   222.9 μs |    44.21 μs |   6.84 μs |  0.14 |    0.02 |   8.7891 |       - |  36.43 KB |        0.07 |
| Disable_N_Plus_1_Pattern | 10        | 1,438.9 μs |   329.66 μs |  85.61 μs |  0.89 |    0.12 | 125.0000 |       - | 516.29 KB |        1.00 |
| Delete_N_Plus_1_Pattern  | 10        | 1,636.8 μs |   894.06 μs | 232.18 μs |  1.02 |    0.18 | 125.0000 |       - | 517.62 KB |        1.00 |
|                          |           |            |             |           |       |         |          |         |           |             |
| Delete_Batch_Pattern     | 20        |   239.9 μs |    40.22 μs |  10.45 μs |  0.09 |    0.01 |  10.7422 |       - |  44.12 KB |        0.07 |
| Disable_Batch_Pattern    | 20        |   329.3 μs |   219.64 μs |  33.99 μs |  0.13 |    0.02 |  10.7422 |       - |  45.07 KB |        0.07 |
| Disable_N_Plus_1_Pattern | 20        | 2,633.9 μs | 2,569.46 μs | 667.28 μs |  1.00 |    0.27 | 140.6250 |  7.8125 | 605.55 KB |        0.99 |
| Delete_N_Plus_1_Pattern  | 20        | 2,681.6 μs | 3,199.35 μs | 495.10 μs |  1.02 |    0.22 | 140.6250 |       - | 608.94 KB |        1.00 |
|                          |           |            |             |           |       |         |          |         |           |             |
| Delete_Batch_Pattern     | 50        |   317.6 μs |    89.76 μs |  13.89 μs |  0.08 |    0.01 |  15.6250 |       - |  67.48 KB |        0.08 |
| Disable_Batch_Pattern    | 50        |   456.5 μs |    93.60 μs |  24.31 μs |  0.11 |    0.01 |  16.6016 |       - |  68.21 KB |        0.08 |
| Disable_N_Plus_1_Pattern | 50        | 3,673.5 μs |   523.95 μs | 136.07 μs |  0.90 |    0.08 | 210.9375 | 31.2500 | 869.87 KB |        0.99 |
| Delete_N_Plus_1_Pattern  | 50        | 4,120.0 μs | 2,315.92 μs | 358.39 μs |  1.01 |    0.11 | 203.1250 | 15.6250 | 880.24 KB |        1.00 |
