# 日志级别深度设计（业界案例校准，2026-08-13）

> 依据：Serilog 官方（Configuration Basics Wiki）+ Dash0 Serilog Log Levels Guide + postsharp.net 高赞 + Safran API Tracing 分层案例
> 原则：**每一层记录「该层职责对应的观测事实」，级别语义严格对齐业界**

## 一、业界级别语义（权威定义）

| 级别 | 业界定义 | 何时用 |
|------|---------|--------|
| **Verbose/Trace** | 最细内部细节，生产几乎不用 | 逐行追踪（极少） |
| **Debug** | 内部系统事件——「**如何发生的**」，不对外可观测但诊断有用 | Repository 正常 CRUD、中间件细节 |
| **Information** | 系统职责对应的**可观测动作**——「**系统做了什么**」 | Controller 操作、Service 业务决策、请求开始/完成 |
| **Warning** | 服务降级/异常但**可继续** | 并发重试、缓存回退、旧数据兼容、降级 Draft |
| **Error** | **单个操作失败**（如 HTTP 请求处理失败） | 未处理异常、SQL 失败、外部调用失败 |
| **Fatal** | **整个应用崩溃** | 启动失败、不可恢复错误 |

**核心校准**（与我们现状的差异）：
1. **Repository 正常 CRUD 用 Debug**（业界「如何发生」）——我们 BaseRepository 已用 Debug ✅
2. **Controller 操作用 Information**（可观测动作）——LogOperation 已用 ✅
3. **Warning 用于「降级/重试/兼容」**——我们并发重试已用 ✅
4. **Error 只用于「操作失败」**——不用于预期业务拒绝（422 是业务，不该 Error）

## 二、分层日志策略（Safran 分层案例校准）

| 层 | 正常路径级别 | 异常路径级别 | 记录内容 | 参数打通 |
|----|-------------|-------------|---------|---------|
| **Controller** | Information | Error（未处理） | LogOperation：操作者 + 业务参数(脱敏) + 目标ID + 结果 | ✅ 已有 |
| **Service** | Information | Error | 业务决策、状态变更、校验结果 | ⚠️ 部分 |
| **Handler** | Information | Error | 命令执行、权限拒绝(403)、业务拒绝(422) | ⚠️ 补 |
| **Repository** | **Debug** | **Error**（SQL/并发）| 实体Id、操作类型、异常链 | ⚠️ 异常补 |
| **Middleware/Filter** | Information | Error | 请求开始/完成、耗时、CorrelationId | ✅ 已有 |

## 三、关键规则（落地）

### 规则 1：级别 vs 业务结果（重要校准）
```
业务成功 → Information（LogOperation）
业务拒绝（422/400 校验失败）→ Information（不是 Error！预期行为）
权限拒绝（403）→ Warning（可疑但非系统错误）
未处理异常（500）→ Error（系统错误）
启动失败 → Fatal
```

### 规则 2：Repository 异常必打（当前缺口）
```
所有 Repository 的 SaveChanges/查询异常 → LogError(ex, "[REPO] {Entity} 操作失败 {Operation}") 
  含实体 Id + CorrelationId（Enricher 自动）+ 异常链（@x 自动含 InnerException）
```

### 规则 3：参数打通（「和参数中的日志接连打通」）
```
Controller LogOperation: 参数脱敏（SensitiveDataMasker）✅ 已有
Repository: 实体 Id + 操作类型（结构化属性）
Service: 业务参数（脱敏）
异常: 异常对象作为第一参数（@x 渲染完整链）✅ 已有
```

### 规则 4：级别可配（US-LOG-005 动态调整已实现）
```
默认 Information；Debug 模式（US-SYS-006）切 Debug——Repository Debug 日志才输出
诊断时：开 Debug 模式 → 看 Repository 细节 → 关
```

## 四、缺口清单（vs 业界标准）

| # | 缺口 | 层级 | 优先级 |
|---|------|------|:---:|
| 1 | Repository 异常日志缺失（27 个模块 Repository 仅 1 个有） | Repository | P0 |
| 2 | MedicalCases 写操作日志缺失（11 端点仅 1） | Controller | P0 |
| 3 | Service 业务决策日志不完整 | Service | P1 |
| 4 | Users/Auth 操作日志不完整（创建有，更新/删除/禁用缺） | Controller | P1 |
| 5 | 业务拒绝(422)错误日志级别校准（应为 Information 非 Error） | 全局 | P1 |

## 五、验收

- [ ] Repository 异常日志：所有模块 Repository SaveChanges/查询异常 LogError 含实体 Id
- [ ] Controller 操作日志：MedicalCases/Users/Auth 写操作补齐 LogOperation
- [ ] 422 业务拒绝不产生 Error 日志（Information）
- [ ] 403 权限拒绝产生 Warning
- [ ] build 0/0 + Server 全量 + 架构 87/87
