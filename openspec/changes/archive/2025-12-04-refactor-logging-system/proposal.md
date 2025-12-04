# OpenSpec Change Proposal: refactor-logging-system

**Status**: approved
**Created**: 2025-12-04
**Author**: Claude Code

## Problem Statement

当前LYBTZYZS项目的日志系统和错误处理机制存在以下问题:

### 日志系统问题

#### Server端问题
1. **日志配置分散**: Serilog配置分布在Program.cs和appsettings.json中,缺乏统一的配置管理
2. **缺少请求上下文关联**: 没有实现请求级别的CorrelationId追踪,难以关联单个请求的所有日志
3. **日志富集不足**: 仅使用基础的时间戳和日志级别,缺少用户、操作类型等业务上下文
4. **两阶段初始化未实现**: 没有使用Serilog推荐的bootstrap logger模式,启动阶段的异常可能丢失
5. **敏感数据处理分散**: LogSanitizer和SensitiveDataMasker功能重叠,需要统一整合
6. **重要日志无持久化**: Warning/Error级别日志仅存文件,无法长期保存和高效查询

#### Client端问题
1. **缺少结构化日志**: 当前仅使用Debug provider,无文件日志输出
2. **日志追踪困难**: 无法将客户端日志与服务端日志关联
3. **错误处理日志不完整**: StandardExceptionHandler记录的信息不够详细

#### 跨端问题
1. **无统一的CorrelationId**: 客户端请求无法与服务端日志关联
2. **日志格式不统一**: 两端日志格式差异大,不利于问题排查

### 错误处理问题

#### Server端问题
1. **ErrorCode体系不完善**: 当前AppException有ErrorCode属性,但缺乏标准化的错误码体系
2. **错误响应格式不统一**: 不同异常类型返回的JSON结构有差异
3. **业务异常分类不清晰**: BusinessException、ValidationException边界模糊
4. **缺少错误上下文**: 异常信息缺少CorrelationId等追踪信息

#### Client端问题
1. **错误处理模式与Server端不一致**: StandardExceptionHandler和GlobalExceptionHandler处理逻辑差异大
2. **用户消息映射分散**: ExceptionMessageMapper硬编码,扩展性差
3. **错误恢复策略缺失**: 缺少重试、降级等错误恢复机制

#### 跨端问题
1. **错误码不统一**: Server端和Client端使用不同的错误分类
2. **错误信息传递不完整**: API错误响应到Client端后信息丢失

## Goals

### 日志系统目标
1. **统一日志架构**: Server端和Client端使用一致的Serilog配置模式
2. **请求追踪**: 实现端到端的CorrelationId追踪
3. **结构化日志**: 所有日志采用结构化格式,便于查询分析
4. **敏感数据保护**: 统一的敏感数据脱敏机制
5. **可观测性增强**: 丰富的日志上下文信息
6. **分级存储**: Warning/Error级别日志持久化到数据库,支持长期保存和查询
7. **动态调试**: 生产环境可通过配置动态开启/关闭Debug日志

### 错误处理目标
1. **统一错误码体系**: 建立分层的ErrorCode枚举,涵盖所有业务场景
2. **标准化错误响应**: 定义统一的API错误响应格式(RFC 7807 Problem Details)
3. **错误处理一致性**: Server端和Client端使用相同的错误分类和处理模式
4. **错误可追溯**: 每个错误响应包含CorrelationId,可关联完整日志链路
5. **用户友好**: 提供清晰的用户提示消息,隐藏技术细节

### v1.0.0阶段范围
- 项目已从MVP升级到v1.0.0阶段,可适当引入更完善的方案
- 采用分级存储策略: 文件日志(全量) + 数据库日志(Warning+级别)
- 保持方案的可扩展性,为未来扩展预留接口
- 错误处理采用渐进式改进,不破坏现有API契约
- 审计日志(字段级变更追踪)规划到v1.1阶段

## Scope

### In Scope

#### 日志系统重构
1. **Server端重构**:
   - 实现Serilog两阶段初始化
   - 添加CorrelationId中间件
   - 统一敏感数据脱敏处理
   - 增强请求日志(IDiagnosticContext)
   - 优化Enrichers配置

2. **日志分级存储**:
   - 添加Serilog.Sinks.MSSqlServer依赖
   - 创建SystemLogs数据库表
   - 配置Warning+级别写入数据库
   - 实现LoggingLevelSwitch动态调试开关
   - 日志保留策略: 文件30天, 数据库90天(Warning)/永久(Error)

3. **Client端重构**:
   - 集成Serilog替换当前Debug provider
   - 实现文件日志输出
   - 添加CorrelationId请求头传递
   - 统一异常日志记录

4. **共享组件**:
   - 统一的日志配置模型
   - 共享的CorrelationId基础设施
   - 敏感数据脱敏扩展整合

#### 错误处理重构
1. **错误码体系**:
   - 定义分层ErrorCode枚举(通用/模块/业务)
   - 错误码文档化

2. **Server端错误处理**:
   - 实现RFC 7807 Problem Details响应格式
   - 增强GlobalExceptionHandler,添加CorrelationId
   - 统一异常到HTTP状态码映射

3. **Client端错误处理**:
   - 重构StandardExceptionHandler对齐Server端
   - 改进ExceptionMessageMapper为可配置模式
   - 实现错误响应解析(Problem Details)

4. **共享组件**:
   - 统一的ErrorCode定义(LYBT.Shared.Models)
   - 标准错误响应DTO

### Out of Scope
1. 日志聚合系统(ELK/Seq)集成
2. 实时日志监控告警
3. 日志分析Dashboard
4. 分布式追踪(OpenTelemetry)
5. 自动重试/熔断机制(Polly高级配置)
6. 错误统计和分析Dashboard
7. 审计日志(字段级数据变更追踪) - 规划到v1.1

## Risk Assessment

### 技术风险
- **低风险**: Serilog是成熟的日志框架,API稳定
- **中风险**: 客户端引入Serilog需要调整DI配置
- **中风险**: Problem Details格式变更需要前端适配

### 兼容性风险
- **低风险**: 日志格式变化不影响业务逻辑
- **中风险**: API错误响应格式变更,需要Client端同步更新
- **需关注**: 现有日志分析脚本可能需要适配新格式

### 性能风险
- **低风险**: Serilog异步sink可以避免日志写入阻塞
- **需关注**: 客户端文件日志需要合理配置rolling策略
- **需关注**: 数据库日志写入需要配置批量写入和异步模式

## Success Criteria

### 日志系统
1. Server端日志包含CorrelationId,可追踪完整请求链路
2. Client端产生结构化文件日志,日志保留策略30天
3. 所有敏感数据(手机号、身份证等)在日志中自动脱敏
4. 两端日志格式统一,包含时间戳、级别、来源、CorrelationId
5. 启动阶段异常能够被正确记录
6. Warning/Error级别日志自动写入SystemLogs数据库表
7. 可通过配置动态开启Debug日志,无需重启应用

### 错误处理
1. 所有API错误响应符合RFC 7807 Problem Details格式
2. 错误响应包含CorrelationId,可关联日志
3. ErrorCode枚举覆盖所有业务场景,有完整文档
4. Client端能正确解析Problem Details并显示用户友好消息
5. 单元测试覆盖新增的错误处理组件

## References

- Serilog Best Practices: https://github.com/serilog/serilog/wiki/Configuration-Basics
- Serilog ASP.NET Core: https://github.com/serilog/serilog-aspnetcore
- .NET Logging Best Practices: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging
- RFC 7807 Problem Details: https://datatracker.ietf.org/doc/html/rfc7807
- ASP.NET Core Error Handling: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling
- Issue #2254: 敏感数据脱敏功能(已完成)
