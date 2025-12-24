# OpenSpec Proposal: 完善数据流转日志跟踪

**Change ID**: enhance-dataflow-logging
**Created**: 2025-12-24
**Updated**: 2025-12-24
**Status**: Draft
**Priority**: P1
**Phase**: Pre-Release Stabilization
**Spec Deltas**: logging-infrastructure (8 requirements added: LOG-012 ~ LOG-019)
**Depends On**: unify-desktop-command-handler (已完成)

---

## 问题背景

### 当前问题

1. **操作失败无法追踪**: Issue #2261中，用户创建失败但无法从日志中定位原因：
   - API请求/响应未记录
   - Repository操作未记录
   - 数据流转链路不完整

2. **现有日志分散且不规范**:
   - Desktop端有1672处日志调用，但无统一前缀
   - 各模块日志格式不一致
   - 无法快速过滤特定层级日志

3. **缺少端到端追踪能力**:
   - CorrelationId基础设施已建立但未充分利用
   - Desktop与WebAPI之间缺少追踪连接
   - 无法追踪一个用户操作的完整链路

### 前置工作已完成 (OpenSpec: unify-desktop-command-handler)

Desktop层数据处理架构已统一，形成三种模式：
- **CommandHandler**: 无状态CRUD操作，统一返回`(success, data, error)`元组
- **AggregateService**: 有状态聚合根管理
- **StateManager**: 有状态简单实体管理

### 现有基础设施 (logging-infrastructure spec LOG-001 ~ LOG-011)

LYBT.Shared.Logging已提供：
- `ICorrelationIdProvider` - 关联ID提供者接口
- `ActivityCorrelationIdProvider` - W3C TraceContext实现
- `CorrelationIdEnricher` - Serilog日志丰富器
- `SensitiveDataMasker` - 敏感数据脱敏

---

## 目标

### 核心目标

1. **完整数据流转追踪**: 从UI点击到数据库存储的全链路日志
2. **统一日志格式**: 所有层级使用规范化前缀
3. **全模块覆盖**: 8个业务模块全部覆盖
4. **快速问题定位**: 任何操作失败都能在5分钟内定位原因

### 成功标准

- [ ] ViewModel操作有日志 (LOG-017)
- [ ] HTTP请求/响应有日志 (LOG-012)
- [ ] CorrelationId端到端传递 (LOG-013)
- [ ] Controller Action有日志 (LOG-014)
- [ ] Repository操作有日志 (LOG-015)
- [ ] 敏感数据自动脱敏 (LOG-016)
- [ ] 日志格式统一规范 (LOG-018)
- [ ] 现有日志规范化 (LOG-019)

---

## 模块覆盖范围

### Desktop端 (8个模块)

#### CommandHandler (无状态CRUD - [CMD]前缀)

| 模块 | CommandHandler | 当前日志状态 |
|------|----------------|--------------|
| Consultation | ConsultationCommandHandler | 部分有[CMD] |
| Formula | FormulaCommandHandler | 部分有[CMD] |
| Herbs | HerbCommandHandler | **新增，已有[CMD]** |
| MedicalCase | MedicalCaseCommandHandler | 部分有[CMD] |
| Patients | PatientCommandHandler | 部分有[CMD] |
| Users | UserCommandHandler | 部分有[CMD] |

#### AggregateService (有状态聚合根 - [AGG]前缀)

| 模块 | AggregateService | 当前日志状态 |
|------|------------------|--------------|
| MedicalCase | MedicalCaseAggregateService | 无统一前缀 |

#### StateManager (有状态实体 - [STATE]前缀)

| 模块 | StateManager | 当前日志状态 |
|------|--------------|--------------|
| Patients | PatientStateManager | 无统一前缀 |

#### 其他Handler (专用处理器 - [HDL]前缀)

| 模块 | Handler | 职责 |
|------|---------|------|
| MedicalCase | MedicalCaseNavigationHandler | 导航处理 |
| MedicalCase | MedicalCaseLifecycleHandler | 生命周期管理 |
| MedicalCase | PrescriptionSaveHandler | 处方保存 |
| MedicalCase | PrescriptionItemHandler | 处方项操作 |
| MedicalCase | PrescriptionImportHandler | 处方导入 |
| Patients | UnfinishedCaseHandler | 未完成病例处理 |

#### 无数据处理层的模块

| 模块 | 说明 |
|------|------|
| Auth | 使用AuthCoordinator协调登录流程 |
| Prescriptions | 仅PrintService，无CRUD操作 |

### Server端 (8个模块)

| 模块 | Services | 日志前缀 |
|------|----------|----------|
| Auth | AuthService, JwtService, TokenRevocationService, SecurityAuditService | [SVC] |
| Consultation | ConsultationService | [SVC] |
| Formula | FormulaService | [SVC] |
| Herbs | HerbService | [SVC] |
| MedicalCase | MedicalCaseCommandService, MedicalCaseQueryService, MedicalCaseStateService, MedicalCaseAuditService, MedicalCasePermissionService | [SVC] |
| Patients | PatientService | [SVC] |
| Prescriptions | PrescriptionService, PrescriptionNumberService | [SVC] |
| Users | UserService | [SVC] |

---

## Spec Deltas 概览

本提案向`logging-infrastructure`规范添加8个新需求：

| Requirement | 标题 | 层级 | 描述 |
|-------------|------|------|------|
| LOG-012 | HTTP客户端请求日志 | Desktop | 记录所有API请求/响应 |
| LOG-013 | 分布式追踪Header传递 | Desktop + Server | traceparent header传递 |
| LOG-014 | Server端API Action日志 | Server | Controller Action日志 |
| LOG-015 | Repository操作日志 | Server | CRUD操作日志 |
| LOG-016 | URI敏感数据脱敏 | Shared | URI参数脱敏 |
| LOG-017 | ViewModel操作日志 | Desktop | 用户操作生命周期日志 |
| LOG-018 | 日志格式标准化 | 全局 | 统一前缀规范 |
| LOG-019 | 现有日志规范化 | 全局 | 更新现有日志格式 |

详见: `specs/logging-infrastructure/spec.md`

---

## 端到端日志链路

完成后，一个医案保存操作的日志链路：

```
[VM] Save started - MedicalCase                     ← ViewModel层
  [AGG] SaveAsync: MedicalCase-001                  ← AggregateService层
    [CMD] UpdateMedicalCase: {Id}                   ← CommandHandler层
      [HTTP] >>> PUT /api/medicalcases/{id}         ← HTTP Client层
        [API] >>> MedicalCasesController.Update     ← Controller层
          [SVC] UpdateAsync: {Id}                   ← Service层
            [REPO] MedicalCase.Update               ← Repository层
            [REPO] MedicalCase.Update completed
          [SVC] UpdateAsync completed: success
        [API] <<< completed in 120ms
      [HTTP] <<< 200 Duration=150ms
    [CMD] UpdateMedicalCase completed: success
  [AGG] SaveAsync completed
[VM] Save completed - Duration=200ms
```

通过CorrelationId可过滤完整链路，快速定位问题。

---

## 日志前缀规范 (LOG-018)

| 层级 | 前缀 | 适用范围 | 示例 |
|------|------|----------|------|
| ViewModel | [VM] | ViewModelBase派生类 | [VM] Save started |
| AggregateService | [AGG] | 有状态聚合根管理器 | [AGG] SaveAsync |
| StateManager | [STATE] | 有状态实体管理器 | [STATE] InitializeAsync |
| CommandHandler | [CMD] | 无状态CRUD操作 | [CMD] CreateUser |
| Handler | [HDL] | 专用处理器 | [HDL] NavigateTo |
| HTTP Client | [HTTP] | API请求/响应 | [HTTP] >>> POST /api/users |
| Controller | [API] | Controller Action | [API] >>> UsersController.Create |
| Service | [SVC] | Server端业务服务 | [SVC] CreateAsync |
| Repository | [REPO] | 数据访问操作 | [REPO] User.Add |

---

## 影响范围

### 新增文件

| 文件 | 位置 | 描述 |
|------|------|------|
| LoggingHttpHandler.cs | Desktop.Infrastructure | HTTP请求/响应日志 |
| CorrelationIdMiddleware.cs | WebAPI | Server端CorrelationId中间件 |
| ApiLoggingFilter.cs | WebAPI | Controller Action日志Filter |

### 修改文件

| 类别 | 文件数 | 修改内容 |
|------|--------|----------|
| ViewModel基类 | 2 | 添加[VM]操作日志 |
| Repository基类 | 1 | 添加[REPO]CRUD日志 |
| CommandHandler | 6 | 规范化[CMD]前缀 |
| AggregateService | 1 | 添加[AGG]前缀 |
| StateManager | 1 | 添加[STATE]前缀 |
| 其他Handler | 6 | 添加[HDL]前缀 |
| Server Service | 15 | 规范化[SVC]前缀 |
| SensitiveDataMasker | 1 | 添加MaskUri方法 |

### 不影响的部分

- 业务逻辑不变
- API契约不变
- 数据库Schema不变

---

## 实施计划

| Phase | 内容 | Tasks | 描述 |
|-------|------|-------|------|
| 1 | Desktop ViewModel日志 | 1.1-1.2 | ViewModelBase添加[VM]日志 |
| 2 | Desktop HTTP日志 | 2.1-2.4 | LoggingHttpHandler + CorrelationId传递 |
| 3 | Server CorrelationId | 3.1-3.2 | CorrelationIdMiddleware |
| 4 | Server API日志 | 4.1-4.2 | ApiLoggingFilter |
| 5 | Repository日志 | 5.1-5.2 | RepositoryBase添加[REPO]日志 |
| 6 | Desktop数据处理层规范化 | 6.1-6.8 | CommandHandler/AggregateService/StateManager/Handler |
| 7 | Server Service规范化 | 7.1-7.8 | 8个模块Service添加[SVC]前缀 |
| 8 | 集成测试与文档 | 8.1-8.5 | 端到端测试 + 文档更新 |

详见: `tasks.md`

---

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 性能开销 | 低 | 使用异步日志、结构化日志 |
| 日志量增大 | 中 | Repository/ViewModel使用Debug级别 |
| 敏感数据泄露 | 中 | 使用SensitiveDataMasker |
| 大量文件修改 | 中 | 分Phase执行，每Phase验证 |

---

## 参考文档

- [.NET Logging Best Practices](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [ASP.NET Core HTTP Logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [Serilog Structured Logging](https://github.com/serilog/serilog/wiki/Structured-Data)
- OpenSpec: unify-desktop-command-handler (架构统一基础)

---

## 审批

- [ ] 技术方案评审
- [ ] 用户确认
- [ ] 开始实施
