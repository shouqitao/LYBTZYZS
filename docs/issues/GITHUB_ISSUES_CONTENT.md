# GitHub Issues 创建内容

复制以下内容到GitHub创建对应的Issue。

---

## Issue #803: [Server] 优化EF Core查询性能 - 解决N+1问题

### 问题描述
当前系统存在严重的EF Core查询性能问题：
- 大量查询未使用`AsNoTracking()`，导致不必要的变更跟踪开销
- 存在N+1查询问题，未正确使用`Include()`预加载关联数据
- 未使用投影查询，加载了不必要的字段

### 验收标准
- [ ] 所有只读查询必须添加`AsNoTracking()`
- [ ] 消除所有N+1查询问题
- [ ] 实施投影查询，只查询需要的字段
- [ ] 单元测试验证查询性能提升
- [ ] 数据库查询次数减少50%以上

### 实施清单
- [ ] PatientRepository.GetAllAsync() - 添加AsNoTracking
- [ ] PatientRepository.SearchAsync() - 使用投影查询
- [ ] ConsultationRepository.GetByPatientIdAsync() - 修复N+1问题
- [ ] PrescriptionRepository.GetByConsultationIdAsync() - 预加载关联数据
- [ ] HerbRepository.GetAllAsync() - 使用编译查询优化
- [ ] UserRepository.GetUserWithRolesAsync() - Include关联数据

### 技术参考
详细实施指南：`docs/issues/ISSUE_803_EF_CORE_OPTIMIZATION.md`

### 标签
`bug` `performance` `ef-core` `database` `optimization` `mvp` `P0-urgent`

### 预估工时
2天

---

## Issue #804: [Server] 修复异步编程中的同步阻塞问题

### 问题描述
系统中存在多处同步阻塞调用，违反了异步编程最佳实践：
- 使用`.Result`和`.Wait()`导致线程阻塞
- 未正确传递`CancellationToken`
- 独立的异步操作未并行执行
- 缺少`ConfigureAwait(false)`配置

### 验收标准
- [ ] 消除所有`.Result`和`.Wait()`调用
- [ ] 所有public异步方法接受`CancellationToken`参数
- [ ] Service层方法使用`ConfigureAwait(false)`
- [ ] 独立的异步操作使用`Task.WhenAll()`并行执行
- [ ] 通过代码审查，无死锁风险

### 实施清单
- [ ] AuthService构造函数 - 移除.Result调用
- [ ] UserService.GetUserById() - 改为异步GetUserByIdAsync()
- [ ] ConsultationService.GetDetailsAsync() - 并行执行独立查询
- [ ] PrescriptionService.CreateBatchAsync() - 实现并发控制
- [ ] 所有Controller添加CancellationToken支持
- [ ] HerbService.LoadHerbsData() - 修复同步I/O
- [ ] FormulaService.CalculateFormula() - 移除Wait()调用

### 技术参考
详细实施指南：`docs/issues/ISSUE_804_ASYNC_PROGRAMMING.md`

### 标签
`bug` `performance` `async` `threading` `optimization` `mvp` `P0-urgent`

### 预估工时
1天

---

## Issue #805: [Server] 实现响应缓存和输出缓存策略

### 问题描述
系统缺乏有效的缓存策略，导致：
- 重复查询静态数据（草药列表、配方模板）
- 未启用响应缓存，增加服务器负载
- 缺少输出缓存配置
- 内存缓存使用单一，未分层

### 验收标准
- [ ] 响应缓存和输出缓存配置完成
- [ ] 控制器方法添加适当的缓存标记
- [ ] 实现分层缓存服务
- [ ] 缓存预热机制工作正常
- [ ] 缓存失效策略正确
- [ ] 性能测试显示响应时间降低40%

### 实施清单
- [ ] Program.cs - 配置ResponseCaching和OutputCache中间件
- [ ] HerbsController - 添加缓存属性（1小时）
- [ ] FormulasController - 配置模板缓存（2小时）
- [ ] UsersController - 权限缓存（10分钟）
- [ ] 实现ICacheService接口
- [ ] 创建CacheWarmupService后台服务
- [ ] 添加缓存管理端点

### 技术参考
详细实施指南：`docs/issues/ISSUE_805_CACHING_OPTIMIZATION.md`

### 标签
`enhancement` `performance` `caching` `optimization` `mvp` `P1-high`

### 预估工时
1天

---

## Issue #806: [Server] 优化中间件管道顺序和配置

### 问题描述
当前中间件配置存在问题：
- 中间件顺序不当影响性能
- 缺少响应压缩
- 异常处理位置不合理
- 缺少请求/响应日志
- CORS配置过于宽松

### 验收标准
- [ ] 中间件按照正确顺序配置
- [ ] 响应压缩正常工作
- [ ] 请求日志记录完整
- [ ] 全局异常处理正确
- [ ] 安全头配置到位
- [ ] CORS和速率限制生效

### 实施清单
- [ ] Program.cs - 重构中间件顺序
- [ ] 配置Brotli/Gzip响应压缩
- [ ] 实现RequestLoggingMiddleware
- [ ] 实现GlobalExceptionMiddleware
- [ ] 添加SecurityHeadersMiddleware
- [ ] 优化CORS策略配置
- [ ] 配置速率限制
- [ ] 添加健康检查端点

### 技术参考
详细实施指南：`docs/issues/ISSUE_806_MIDDLEWARE_OPTIMIZATION.md`

### 标签
`enhancement` `performance` `middleware` `security` `optimization` `mvp` `P1-high`

### 预估工时
1天

---

## Issue #807: [Server] 优化日志配置和依赖注入生命周期

### 问题描述
系统存在以下问题：
- 日志级别设置不当，产生大量无用日志
- 依赖注入生命周期混乱，导致内存泄漏
- 缺少结构化日志
- Service定位器反模式使用
- 启动时间过长

### 验收标准
- [ ] 日志输出减少50%以上
- [ ] 所有服务生命周期正确配置
- [ ] 消除Service Locator反模式
- [ ] 启动时间缩短30%
- [ ] 内存泄漏问题解决
- [ ] 结构化日志实施

### 实施清单
- [ ] appsettings.Production.json - 调整日志级别为Warning
- [ ] Program.cs - 配置Serilog结构化日志
- [ ] 审查Repository生命周期（Scoped）
- [ ] 审查Service生命周期（Scoped/Transient）
- [ ] UserService - 消除Service Locator
- [ ] 实现延迟初始化服务
- [ ] 修复资源释放问题（IDisposable）
- [ ] 使用.NET 8键控服务

### 技术参考
详细实施指南：`docs/issues/ISSUE_807_LOGGING_DI_OPTIMIZATION.md`

### 标签
`enhancement` `performance` `logging` `dependency-injection` `optimization` `mvp` `P1-high`

### 预估工时
1天

---

## 创建顺序建议

1. 先创建P0级别（#803, #804）- 标记为`P0-urgent`
2. 再创建P1级别（#805, #806, #807）- 标记为`P1-high`
3. 在每个Issue中引用技术实施文档路径
4. 设置Milestone为"MVP性能优化"
5. 分配给对应的开发人员

## 项目看板配置

建议在GitHub Projects中创建看板：
- **待办 (Todo)**: 新创建的Issues
- **进行中 (In Progress)**: 正在实施的优化
- **审查 (Review)**: 完成待验证
- **完成 (Done)**: 验证通过

每个Issue完成后通过PR关闭，PR描述中使用`Fixes #803`等关键字自动关联。