# OpenSpec Proposal: refactor-exception-handling-system

**Created**: 2025-12-20
**Status**: Draft
**Author**: Claude Code
**Spec References**: error-handling

---

## Why

### 当前问题分析

经过深度代码审计，发现当前异常处理体系存在以下问题：

#### P1 级别（阻塞性问题）

| 问题 | 位置 | 影响 |
|------|------|------|
| **异常吞没** | Service层 | catch块返回`Result.Failure()`而非re-throw，导致中间件链断裂、CorrelationId追踪失效 |

#### P2 级别（架构缺陷）

| 问题 | 位置 | 影响 |
|------|------|------|
| **异常类型混用** | Service层 | 使用`InvalidOperationException`表示业务异常，而非`BusinessException` |
| **ViewModel无异常基类** | Desktop Infrastructure | 每个ViewModel自行实现try-catch，处理逻辑不一致 |
| **API异常处理不完整** | ViewModels | 未对401(需登录)、409(数据冲突)等状态码做特殊处理 |
| **异常消息泄露** | ViewModels | 直接显示`ex.Message`，未经过安全过滤 |

#### P3 级别（优化项）

| 问题 | 位置 | 影响 |
|------|------|------|
| **无HTTP重试机制** | HttpClient | 瞬态故障（网络抖动、超时）直接失败，无Polly集成 |
| **无集中式监控** | 全局 | 异常统计、告警机制缺失 |

### 现有资产

当前已有部分异常处理基础设施：

```
后端:
├── AppException层级 (AppException → BusinessException, ValidationException, NotFoundException, ConflictException, UnauthorizedException)
├── IExceptionHandler链 (BusinessExceptionHandler + SystemExceptionHandler)
├── RFC 7807 ProblemDetails格式
├── ExceptionFactory便捷创建
└── ErrorCode枚举 (模块编码体系: 5位 = 2位模块 + 3位错误)

前端:
├── ErrorHandlingService (全局异常捕获)
├── ProblemDetailsResponse (API错误解析)
└── ClientErrorMessageMapper (错误消息本地化)
```

---

## What Changes

### 架构目标

建立**端到端一致**的异常处理体系：

```
[异常发生] → [统一抛出] → [链式处理] → [用户友好展示] → [日志追踪]
```

### Phase 1: Service层异常规范化

**目标**: 消除异常吞没，统一异常抛出标准

**变更内容**:

1. **禁止catch-and-return模式**
   ```csharp
   // ❌ 禁止
   try { ... } catch (Exception ex) { return Result.Failure(ex.Message); }

   // ✅ 正确
   throw ExceptionFactory.Business(ErrorCode.XXX, "消息");
   ```

2. **异常类型标准化**
   - 业务逻辑错误 → `BusinessException`
   - 数据未找到 → `NotFoundException`
   - 数据冲突 → `ConflictException`
   - 参数验证 → `ValidationException`
   - 权限不足 → `UnauthorizedException`
   - 系统级错误 → 原生异常（由SystemExceptionHandler处理）

3. **创建ExceptionThrowingGuidelines.md文档**

### Phase 2: ViewModel层异常处理基类

**目标**: 提供统一的异常处理模板方法

**新增组件**:

```csharp
// ViewModelBase新增方法
protected async Task<T?> SafeExecuteAsync<T>(
    Func<Task<T>> action,
    string operationName,
    T? fallbackValue = default,
    Action<Exception>? onError = null)
{
    try
    {
        IsBusy = true;
        return await action();
    }
    catch (ApiException ex) when (ex.StatusCode == 401)
    {
        await HandleUnauthorizedAsync();
        return fallbackValue;
    }
    catch (ApiException ex) when (ex.StatusCode == 409)
    {
        await HandleConflictAsync(operationName);
        return fallbackValue;
    }
    catch (Exception ex)
    {
        await HandleExceptionAsync(ex, operationName);
        onError?.Invoke(ex);
        return fallbackValue;
    }
    finally
    {
        IsBusy = false;
    }
}
```

**状态码特殊处理**:

| 状态码 | 处理策略 |
|--------|----------|
| 401 Unauthorized | 导航到登录页，清除会话 |
| 409 Conflict | 提示数据已被修改，建议刷新 |
| 504 Gateway Timeout | 提示服务暂时不可用，建议稍后重试 |

### Phase 3: HTTP韧性层（Polly集成）

**目标**: 提升网络通信的健壮性

**Polly策略配置**:

```csharp
services.AddHttpClient<IApiClient>()
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutRejectedException>()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

**策略矩阵**:

| 策略 | 触发条件 | 参数 |
|------|----------|------|
| Retry | HttpRequestException, Timeout | 3次, 指数退避(2^n秒) |
| Circuit Breaker | 连续5次失败 | 熔断30秒 |
| Timeout | 请求超时 | 30秒/请求 |

### Phase 4: 异常消息安全化

**目标**: 防止敏感信息泄露

**实现方式**:

1. **扩展ClientErrorMessageMapper**
   - 所有异常消息必须通过Mapper转换
   - 系统异常显示通用消息："操作失败，请稍后重试"
   - 业务异常显示ErrorCode对应的本地化消息

2. **消息过滤规则**
   ```csharp
   // 敏感信息过滤
   - 数据库连接字符串
   - 堆栈跟踪
   - 内部服务地址
   - SQL语句
   ```

---

## Impact

### 影响的Spec

| Spec | 变更类型 | 说明 |
|------|----------|------|
| error-handling | 扩展 | 新增ERR-008~ERR-012约束 |

### 影响的代码

| 模块 | 影响范围 | 变更类型 |
|------|----------|----------|
| LYBT.Desktop.Infrastructure | ViewModelBase | 新增SafeExecuteAsync方法 |
| LYBT.Desktop.* | 所有ViewModel | 重构使用SafeExecuteAsync |
| LYBT.WebAPI | Program.cs | 添加Polly配置 |
| LYBT.Application | 所有Service | 移除catch-return模式 |
| LYBT.Shared.Models | ErrorCode | 扩展错误码 |

### 新增依赖

```xml
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.*" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.*" />
```

---

## Risk Assessment

### 风险矩阵

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| Service层改造导致异常类型不匹配 | 中 | 高 | 编译时类型检查 + 单元测试 |
| ViewModel迁移遗漏 | 低 | 中 | 代码审查清单 |
| Polly配置不当导致延迟 | 低 | 中 | 性能测试验证 |
| 错误消息本地化缺失 | 中 | 低 | 默认回退消息 |

### 回滚策略

每个Phase独立完成，可单独回滚：
- Phase 1: Service层异常规范化 → 恢复原catch-return代码
- Phase 2: ViewModel基类 → 删除SafeExecuteAsync，保留原有try-catch
- Phase 3: Polly集成 → 移除Polly包和配置
- Phase 4: 消息安全化 → 恢复直接显示ex.Message

---

## Implementation Phases

### Phase 1: Service层异常规范化（优先级最高）

1. 创建异常抛出规范文档
2. 审计所有Service类的catch块
3. 逐模块改造，移除catch-return模式
4. 更新单元测试验证异常抛出

### Phase 2: ViewModel层异常处理基类

1. 在ViewModelBase中添加SafeExecuteAsync
2. 添加HandleUnauthorizedAsync/HandleConflictAsync方法
3. 逐模块迁移ViewModel使用新方法
4. 集成测试验证

### Phase 3: HTTP韧性层

1. 添加Polly NuGet包
2. 配置HttpClient策略
3. 添加Polly日志记录
4. 压力测试验证

### Phase 4: 异常消息安全化

1. 扩展ClientErrorMessageMapper
2. 添加敏感信息过滤器
3. 审计所有显示ex.Message的位置
4. 替换为安全消息

---

## Validation Criteria

- [ ] 所有Service层不存在catch-return模式
- [ ] 所有ViewModel使用SafeExecuteAsync包装异步操作
- [ ] 401响应自动导航到登录页
- [ ] 409响应提示用户刷新数据
- [ ] HTTP瞬态故障自动重试（最多3次）
- [ ] 连续失败触发熔断（5次→30秒熔断）
- [ ] 用户界面不显示堆栈跟踪或敏感信息
- [ ] 所有异常记录CorrelationId
