# LYBTZYZS 代码覆盖率基准

**生成日期**: 2025-12-20
**覆盖率工具**: coverlet.collector 6.0.4 + ReportGenerator

## 整体覆盖率

| 指标 | 数值 |
|------|------|
| 行覆盖率 | 15.4% |
| 分支覆盖率 | 12.9% |
| 方法覆盖率 | 11.5% |
| 覆盖行数 | 2,992 / 19,321 |
| 程序集数量 | 37 |
| 类数量 | 529 |

## 模块覆盖率明细

| 模块 | 行覆盖率 | 说明 |
|------|---------|------|
| LYBT.Shared.ExceptionHandling | 高 | 异常处理基础设施 |
| LYBT.Desktop.Contracts | 82.6% | 契约和接口定义 |
| LYBT.Desktop.Foundation | 24.2% | 基础设施层 |
| LYBT.Desktop.Consultation | 26.6% | 诊断模块 |
| LYBT.Module.Users | ~7% | 用户模块 |
| UI模块 (Formula/Herbs/etc.) | 0% | 需要集成测试 |

## 零覆盖代码分析

### 类型分布
- **UI控件/ViewModel**: 未测试但活跃使用
- **服务扩展方法**: DI注册代码，运行时使用
- **模型类**: 数据传输对象，被动使用

### 死代码检查结果
- 未使用的私有字段: 0
- 未使用的私有方法: 0
- [Obsolete]标记代码: 0
- TODO remove注释: 0

**结论**: 无需清理的死代码

## 覆盖率提升建议

1. **优先级1 - 核心业务逻辑**
   - LYBT.Module.* 服务层
   - LYBT.Desktop.*.Services

2. **优先级2 - ViewModel单元测试**
   - 使用Mock隔离UI依赖
   - 测试命令和状态转换

3. **优先级3 - 集成测试**
   - API端点测试
   - 数据库操作测试

## 运行覆盖率收集

```powershell
# 运行测试并收集覆盖率
dotnet test LYBT.All.sln --settings tests/.runsettings --collect:"XPlat Code Coverage" --results-directory BIN/TestResults/Coverage

# 生成HTML报告
reportgenerator "-reports:BIN/TestResults/Coverage/**/coverage.cobertura.xml" "-targetdir:BIN/TestResults/CoverageReport" "-reporttypes:Html;JsonSummary"
```

## 版本历史

| 日期 | 行覆盖率 | 变更说明 |
|------|---------|---------|
| 2025-12-20 | 15.4% | 初始基准 |
