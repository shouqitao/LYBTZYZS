# 事务系统架构设计文档

## 概述

本文档描述了凌隐宝堂中医诊所系统中的分布式事务协调器架构设计。该系统采用Saga模式实现复杂业务流程的事务管理，确保数据一致性和系统可靠性。

## 设计目标

### 核心目标
- **数据一致性**: 保证跨多个实体的业务操作要么全部成功，要么全部回滚
- **可靠性**: 系统故障时能够自动恢复和补偿
- **可观测性**: 提供完整的事务执行日志和性能监控
- **扩展性**: 支持新业务流程的快速集成

### 业务目标
- **中医特化**: 支持中医诊疗流程的事务化管理
- **用户体验**: 确保诊疗操作的原子性，避免数据不一致导致的用户困惑
- **合规性**: 满足医疗行业对数据完整性和审计的要求

## 系统架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                   Transaction System                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Transaction     │  │ Transaction     │  │ Transaction     │ │
│  │ Definitions     │  │ Coordinator     │  │ Monitoring      │ │
│  │                 │  │                 │  │                 │ │
│  │ • Consultation  │  │ • Execute Steps │  │ • Logger        │ │
│  │ • Prescription  │  │ • Compensation  │  │ • Metrics       │ │
│  │ • Custom        │  │ • Retry Logic   │  │ • Health Check  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Transaction     │  │ Transaction     │  │ Transaction     │ │
│  │ Steps           │  │ Context         │  │ Base Classes    │ │
│  │                 │  │                 │  │                 │ │
│  │ • Database Ops  │  │ • Data Passing  │  │ • Step Base     │ │
│  │ • Validation    │  │ • State Mgmt    │  │ • Conditional   │ │
│  │ • External API  │  │ • Error Handling│  │ • Compensatable │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Database        │  │ Caching         │  │ Logging         │ │
│  │ • EF Core       │  │ • Memory Cache  │  │ • Structured    │ │
│  │ • Transaction   │  │ • Step History  │  │ • Performance   │ │
│  │ • Connection    │  │ • Metrics       │  │ • Audit Trail   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. 事务协调器 (Transaction Coordinator)

**职责**: 事务执行的核心引擎，负责步骤编排、异常处理和补偿机制。

**核心接口**:
```csharp
public interface ITransactionCoordinator<TContext> 
    where TContext : TransactionContext
{
    Task<TransactionResult<TContext>> ExecuteAsync(
        TransactionDefinition<TContext> definition, 
        TContext context, 
        CancellationToken cancellationToken = default);
}
```

**关键特性**:
- **步骤编排**: 支持顺序和并行执行模式
- **重试机制**: 可配置的重试策略和次数
- **补偿处理**: 自动识别并执行补偿操作
- **超时控制**: 防止长时间运行的事务阻塞系统

### 2. 事务步骤 (Transaction Steps)

**设计模式**: Command Pattern + Strategy Pattern

**步骤层次结构**:
```
ITransactionStep<TContext>
├── TransactionStepBase<TContext>          # 抽象基类
│   ├── DatabaseTransactionStep<TContext>  # 数据库操作步骤
│   ├── ConditionalTransactionStep<TContext> # 条件执行步骤
│   └── ExternalApiStep<TContext>          # 外部API调用步骤
```

**生命周期**:
1. **CanExecuteAsync**: 前置条件检查
2. **ExecuteAsync**: 核心业务逻辑执行
3. **CompensateAsync**: 失败时的补偿操作

### 3. 事务上下文 (Transaction Context)

**作用**: 在事务步骤间传递数据和状态

**设计原则**:
- **类型安全**: 强类型上下文，编译时检查
- **状态隔离**: 每个事务拥有独立的上下文实例
- **可序列化**: 支持持久化和恢复

**示例**:
```csharp
public class PrescriptionTransactionContext : TransactionContext
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid MedicalCaseId { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    // ... 其他业务数据
}
```

### 4. 监控系统 (Monitoring System)

#### 4.1 事务日志 (Transaction Logger)

**功能**: 完整记录事务执行过程

**数据模型**:
```
TransactionLog
├── 基本信息: ID, Name, Status, Duration
├── 时间信息: StartTime, EndTime, CreatedAt
├── 关联信息: UserId, EntityIds
├── 错误信息: Exception, ContextSnapshot
└── 步骤历史: StepHistory (缓存)
```

**查询能力**:
- 按时间范围查询事务历史
- 按用户、事务类型筛选
- 慢事务和异常事务统计
- 事务步骤详细轨迹

#### 4.2 性能指标 (Metrics)

**实时指标**:
- 活跃事务数量
- 事务处理量 (TPS)
- 平均执行时间
- 成功率统计

**历史分析**:
- 按小时/天的执行趋势
- 事务类型性能对比
- 慢事务告警和分析
- 错误模式识别

#### 4.3 健康检查 (Health Check)

**检查维度**:
- 数据库连接状态
- 事务日志系统可用性
- 指标收集系统状态
- 系统资源使用情况

## 业务集成

### 1. 诊疗流程事务

**场景**: 患者开始诊疗的完整流程

**事务定义**:
```csharp
StartConsultationTransaction
├── CreateMedicalCaseStep      # 创建医疗案例
├── InitializeConsultationStep # 初始化诊断记录
└── UpdatePatientStatusStep    # 更新患者状态
```

**业务规则**:
- 患者必须已注册且状态正常
- 医生必须有执业权限
- 不能有进行中的诊疗案例

### 2. 处方创建事务

**场景**: 医生为患者开具中药处方

**事务定义**:
```csharp
CreatePrescriptionTransaction
├── ValidatePrerequisitesStep  # 验证先决条件
├── CreatePrescriptionStep     # 创建处方记录
├── AddPrescriptionItemsStep   # 添加药材项目
├── ValidateCompatibilityStep  # 验证配伍安全性
└── UpdateMedicalCaseStep      # 更新医疗案例关联
```

**中医特色验证**:
- **十八反**: 甘草与甘遂、大戟、海藻、芫花相反
- **十九畏**: 硫磺畏朴硝、水银畏砒霜等
- **妊娠禁忌**: 孕妇禁用/慎用药材检查
- **剂量安全**: 单味药材和总剂量限制

## 技术实现细节

### 1. 错误处理策略

**异常分类**:
- **业务异常**: 验证失败、业务规则违反
- **系统异常**: 数据库连接失败、网络超时
- **用户异常**: 权限不足、操作取消

**处理策略**:
```csharp
switch (exception)
{
    case BusinessValidationException:
        // 记录日志，返回友好错误信息
        break;
    case SystemException:
        // 记录详细错误，尝试重试或降级
        break;
    case SecurityException:
        // 记录安全日志，终止事务
        break;
}
```

### 2. 性能优化

**数据库优化**:
- 使用EF Core批量操作减少数据库往返
- 合理的连接池配置（Max=20, Min=2）
- 索引优化：事务ID、时间范围、状态字段

**内存优化**:
- 步骤历史使用内存缓存，定期清理
- 活跃事务指标内存存储，提高查询性能
- 合理的缓存过期策略

**并发优化**:
- 使用ConcurrentDictionary保证线程安全
- 异步编程模式，避免线程阻塞
- 合理的信号量控制并发数量

### 3. 配置管理

**事务配置选项**:
```csharp
public class TransactionOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public int MaxRetryCount { get; set; } = 3;
    public bool EnableAutoCompensation { get; set; } = true;
    public bool EnableParallelExecution { get; set; } = false;
    public TransactionLogLevel LogLevel { get; set; } = TransactionLogLevel.Information;
}
```

## 部署和运维

### 1. 依赖注入配置

```csharp
// Program.cs 或 Startup.cs
services.AddCompleteTransactionSystem();

// 包含以下服务：
// - ITransactionCoordinator<T>
// - ITransactionLogger
// - ITransactionMetrics
// - 所有事务步骤和定义
// - 健康检查和配置选项
```

### 2. 健康检查端点

```
GET /health/transaction
```

**响应示例**:
```json
{
  "status": "Healthy",
  "description": "事务系统运行正常",
  "data": {
    "database_connected": true,
    "active_transaction_count": 3,
    "success_rate": 98.5,
    "average_execution_time_ms": 1250.0
  }
}
```

### 3. 监控指标

**关键指标**:
- 事务成功率 > 95%
- 平均响应时间 < 5秒
- 活跃事务数量 < 100
- 错误率 < 5%

**告警规则**:
- 连续5分钟成功率 < 90%
- 单个事务执行时间 > 30秒
- 活跃事务数量 > 200
- 数据库连接失败

## 最佳实践

### 1. 事务设计原则

**单一职责**: 每个事务步骤只处理一个业务操作
**幂等性**: 步骤可以安全地重复执行
**可补偿性**: 每个步骤都应该有对应的回滚操作
**超时控制**: 设置合理的步骤和事务超时时间

### 2. 错误处理准则

**快速失败**: 尽早发现和报告错误
**详细日志**: 记录足够的上下文信息用于调试
**用户友好**: 向用户展示可理解的错误信息
**自动恢复**: 对于临时性错误，实现自动重试

### 3. 性能考虑

**批处理**: 合并相似的数据库操作
**异步处理**: 对于耗时操作使用异步模式
**缓存策略**: 合理使用缓存减少数据库压力
**资源释放**: 及时释放数据库连接和内存资源

## 扩展指南

### 添加新的事务定义

1. **创建事务上下文**:
```csharp
public class NewTransactionContext : TransactionContext
{
    // 定义业务数据属性
}
```

2. **实现事务步骤**:
```csharp
public class NewTransactionStep : TransactionStepBase<NewTransactionContext>
{
    // 实现步骤逻辑
}
```

3. **定义事务流程**:
```csharp
public class NewTransactionDefinition
{
    public TransactionDefinition<NewTransactionContext> Create()
    {
        // 组装事务步骤
    }
}
```

4. **注册依赖注入**:
```csharp
services.AddScoped<NewTransactionStep>();
services.AddScoped<NewTransactionDefinition>();
```

### 自定义监控指标

1. **扩展ITransactionMetrics接口**
2. **在TransactionMetrics中添加新的指标收集逻辑**
3. **更新健康检查以包含新指标**
4. **在前端展示新的指标数据**

## 故障排查

### 常见问题

**事务超时**:
- 检查数据库连接状态
- 确认步骤执行时间是否过长
- 调整超时配置参数

**补偿失败**:
- 检查补偿步骤的实现逻辑
- 确认数据状态是否支持回滚
- 查看详细的错误日志

**性能问题**:
- 监控数据库查询性能
- 检查内存使用情况
- 分析慢事务告警

### 调试技巧

1. **启用详细日志**: 设置LogLevel为Debug
2. **查看事务轨迹**: 使用GetTransactionByIdAsync获取完整执行历史
3. **监控实时指标**: 使用GetCurrentMetricsAsync观察系统状态
4. **分析慢事务**: 使用GetSlowTransactionAlertsAsync识别性能瓶颈

## 版本历史

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| 1.0 | 2025-01-31 | 初始版本，包含核心事务协调器功能 |
| 1.1 | 2025-01-31 | 添加监控系统和健康检查 |
| 1.2 | 2025-01-31 | 完善中医特色业务流程支持 |

---

**维护人员**: Claude AI Assistant  
**最后更新**: 2025-01-31  
**审核状态**: 待审核