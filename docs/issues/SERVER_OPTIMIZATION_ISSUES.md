# Server端性能优化Issues汇总

## 📋 优化任务清单

根据`docs/optimization/SERVER_OPTIMIZATION_PLAN.md`生成的详细优化Issues，每个Issue都包含具体到方法级别的实施指导。

### P0 - 紧急优化（影响系统稳定性）

#### [Issue #803: EF Core查询性能优化](ISSUE_803_EF_CORE_OPTIMIZATION.md)
- **问题**: N+1查询、缺少AsNoTracking、未使用投影
- **涉及模块**: Patients、Consultation、Prescriptions、Herbs、Users
- **预计工时**: 2天
- **关键优化点**:
  - PatientRepository.GetAllAsync() - 添加AsNoTracking
  - ConsultationRepository.GetByPatientIdAsync() - 修复N+1问题
  - 使用Select投影减少数据传输

#### [Issue #804: 修复同步阻塞问题](ISSUE_804_ASYNC_PROGRAMMING.md)
- **问题**: .Result/.Wait()导致线程阻塞、缺少CancellationToken
- **涉及模块**: Auth、Users、Consultation、Formula、Herbs
- **预计工时**: 1天
- **关键优化点**:
  - AuthService构造函数移除.Result
  - ConsultationService.GetDetailsAsync()并行化
  - 所有public异步方法添加CancellationToken

### P1 - 高优先级优化（提升性能）

#### [Issue #805: 响应缓存优化](ISSUE_805_CACHING_OPTIMIZATION.md)
- **问题**: 缺乏缓存策略、重复查询静态数据
- **涉及模块**: Herbs、Formulas、Users权限
- **预计工时**: 1天
- **关键优化点**:
  - 配置ResponseCaching和OutputCache
  - HerbsController添加缓存标记
  - 实现缓存预热和失效策略

#### [Issue #806: 中间件管道优化](ISSUE_806_MIDDLEWARE_OPTIMIZATION.md)
- **问题**: 中间件顺序不当、缺少响应压缩
- **涉及模块**: WebAPI启动配置
- **预计工时**: 1天
- **关键优化点**:
  - 优化中间件执行顺序
  - 启用Brotli/Gzip压缩
  - 实现全局异常处理

#### [Issue #807: 日志和DI优化](ISSUE_807_LOGGING_DI_OPTIMIZATION.md)
- **问题**: 日志级别过高、DI生命周期混乱
- **涉及模块**: 所有Service和Repository
- **预计工时**: 1天
- **关键优化点**:
  - 生产环境日志级别调整为Warning
  - 修正服务生命周期
  - 消除Service Locator反模式

## 📊 实施统计

| 优先级 | Issue数量 | 预计总工时 | 影响范围 |
|--------|-----------|------------|----------|
| P0 | 2个 | 3天 | 系统稳定性 |
| P1 | 3个 | 3天 | 性能提升 |
| **总计** | **5个** | **6天** | **全系统** |

## 🎯 预期收益

### 性能指标改善
- **API响应时间**: 150ms → 75ms (-50%)
- **数据库查询次数**: 100次/请求 → 20次/请求 (-80%)
- **内存占用**: 180MB → 120MB (-33%)
- **启动时间**: 5秒 → 3秒 (-40%)

### 质量指标改善
- 消除所有同步阻塞代码
- 100%异常处理覆盖
- 静态数据缓存命中率90%
- 日志I/O降低50%

## 🔧 实施建议

### 执行顺序
1. **第1-2天**: 完成P0级别优化（#803、#804）
2. **第3天**: 实施缓存优化（#805）
3. **第4天**: 优化中间件（#806）
4. **第5天**: 日志和DI优化（#807）
5. **第6天**: 集成测试和性能验证

### 验证方法
```bash
# 负载测试
ab -n 1000 -c 10 http://localhost:5001/api/patients

# 性能分析
dotnet-trace collect --process-id <pid> --providers Microsoft-Windows-DotNETRuntime

# 内存分析
dotnet-dump collect -p <pid>
```

### 注意事项
1. 每个优化点实施后必须回归测试
2. 保持向后兼容，不改变API契约
3. 优化过程记录性能基准对比
4. 生产环境部署需要蓝绿策略

## 📎 相关文档
- [Server端性能优化方案](../optimization/SERVER_OPTIMIZATION_PLAN.md)
- [优化实施示例代码](../optimization/IMPLEMENTATION_EXAMPLES.md)
- [ASP.NET Core性能最佳实践](https://docs.microsoft.com/aspnet/core/performance)

---
**创建日期**: 2025-01-10
**维护者**: Claude Code
**状态**: 待实施