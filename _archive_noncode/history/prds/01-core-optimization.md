# 核心架构优化需求 (PRD-001)

## 📋 需求概述

| 字段   | 内容                                             |
| ---- | ---------------------------------------------- |
| 需求编号 | PRD-001                                        |
| 需求名称 | 核心架构优化 - 数据一致性与事务保护                            |
| 优先级  | P1 (紧急)                                        |
| 预估工期 | 30工作日                                          |
| 风险等级 | 🔴 高风险缓解                                       |
| 负责模块 | 跨模块 (MedicalCase, Consultation, Prescriptions) |

## 🎯 需求背景

根据架构分析报告，系统存在**数据一致性缺口**这一高风险项：

- 跨模块数据操作可能出现不一致状态
- 缺乏分布式事务管理机制
- 关键业务流程缺乏事务保护

**问题影响**:

- 数据完整性风险
- 业务流程中断风险  
- 难以排查数据不一致问题

## 🎯 需求目标

### 主要目标

1. **实现关键业务流程事务保护**
2. **建立数据一致性保证机制**
3. **提供事务失败补偿机制**
4. **增强系统可靠性和数据完整性**

### 成功指标

- ✅ 诊疗流程事务成功率 > 99.9%
- ✅ 数据不一致事件 = 0
- ✅ 事务回滚机制有效性 = 100%
- ✅ 业务流程完整性验证通过

## 📊 现状分析

### 问题场景识别

#### 场景1: 诊疗流程事务风险

**当前实现**:

```csharp
// ConsultationViewModel.cs - 问题代码
public async Task StartConsultationAsync(Guid patientId)
{
    // 步骤1：创建医疗案例
    var caseResult = await _medicalCaseService.CreateCaseAsync(patientId);
    if (!caseResult.Success) return;

    // 步骤2：创建诊断记录 - 如果失败，案例已创建但无诊断
    var consultationResult = await _consultationService.CreateConsultationAsync(caseResult.Data.Id);

    // 问题：缺乏事务回滚机制
}
```

**风险点**:

- MedicalCase创建成功，但Consultation创建失败
- 患者会显示有未完成的案例，但无法进行诊断
- 需要手动清理孤立的MedicalCase记录

#### 场景2: 处方创建事务风险

**当前实现**:

```csharp
// PrescriptionBusinessService.cs - 问题代码
public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
{
    // 步骤1：创建处方主记录
    var prescription = new Prescription { ... };
    await _repository.CreateAsync(prescription);

    // 步骤2：创建处方药材关联 - 如果失败，处方记录已存在
    foreach(var herb in dto.Herbs)
    {
        await _prescriptionHerbRepository.CreateAsync(new PrescriptionHerb { ... });
        // 问题：某个药材失败，前面的已经创建
    }
}
```

**风险点**:

- 处方主记录创建成功，但部分药材关联失败
- 导致处方不完整，药材配伍信息缺失
- 影响处方准确性和安全性

## 🔧 解决方案设计

### 技术方案选择

#### 方案对比

| 方案       | 复杂度 | 性能影响 | 维护成本 | 推荐度 |
| -------- | --- | ---- | ---- | --- |
| 数据库分布式事务 | 高   | 高    | 高    | ❌   |
| Saga事务模式 | 中   | 中    | 中    | ✅   |
| 补偿事务模式   | 低   | 低    | 低    | ⭐   |
| 单一聚合根事务  | 低   | 低    | 低    | ✅   |

**推荐方案**: **补偿事务模式** + **单一聚合根事务**

### 详细实现方案

#### 1. 事务协调器设计

```csharp
public interface ITransactionCoordinator
{
    Task<TransactionResult<T>> ExecuteAsync<T>(TransactionDefinition<T> definition);
}

public class TransactionDefinition<T>
{
    public string TransactionName { get; set; }
    public List<ITransactionStep<T>> Steps { get; set; }
    public List<ICompensationStep<T>> CompensationSteps { get; set; }
}

public interface ITransactionStep<T>
{
    Task<StepResult<T>> ExecuteAsync(TransactionContext<T> context);
    Task<bool> CanCompensateAsync(TransactionContext<T> context);
}
```

#### 2. 具体业务实现

##### 诊疗流程事务保护

```csharp
public class StartConsultationTransaction : TransactionDefinition<ConsultationResult>
{
    public StartConsultationTransaction(Guid patientId)
    {
        TransactionName = "StartConsultation";

        Steps = new List<ITransactionStep<ConsultationResult>>
        {
            new CreateMedicalCaseStep(patientId),
            new CreateConsultationStep(),
            new UpdatePatientStatusStep(patientId)
        };

        CompensationSteps = new List<ICompensationStep<ConsultationResult>>
        {
            new DeleteConsultationCompensation(),
            new DeleteMedicalCaseCompensation(),
            new RevertPatientStatusCompensation()
        };
    }
}
```

##### 处方创建事务保护

```csharp
public class CreatePrescriptionTransaction : TransactionDefinition<PrescriptionResult>
{
    public CreatePrescriptionTransaction(PrescriptionCreateDto dto)
    {
        TransactionName = "CreatePrescription";

        Steps = new List<ITransactionStep<PrescriptionResult>>
        {
            new ValidatePrescriptionStep(dto),
            new CreatePrescriptionMainStep(dto),
            new CreatePrescriptionHerbsStep(dto.Herbs),
            new CalculatePrescriptionTotalStep(),
            new UpdateConsultationStatusStep()
        };

        CompensationSteps = new List<ICompensationStep<PrescriptionResult>>
        {
            new DeletePrescriptionHerbsCompensation(),
            new DeletePrescriptionMainCompensation(),
            new RevertConsultationStatusCompensation()
        };
    }
}
```

#### 3. 数据库事务优化

```csharp
public class TransactionDbContext : AppDbContext
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        using var transaction = await Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

## 📝 详细需求规格

### 功能需求

#### FR-001: 事务协调器实现

- **描述**: 实现通用的事务协调器，支持多步骤事务和补偿机制
- **输入**: 事务定义和执行上下文
- **输出**: 事务执行结果和补偿信息
- **异常处理**: 任意步骤失败时触发补偿流程

#### FR-002: 诊疗流程事务保护

- **描述**: 为创建医疗案例+诊断记录流程添加事务保护
- **业务规则**: 
  - 医疗案例和诊断记录必须同时成功创建
  - 任意失败时回滚全部操作
  - 保证患者状态一致性
- **验证规则**: 创建后验证数据完整性和关联关系

#### FR-003: 处方创建事务保护

- **描述**: 为处方创建+药材关联流程添加事务保护
- **业务规则**:
  - 处方主记录和所有药材关联必须同时成功
  - 药材配伍验证失败时回滚整个处方
  - 计算总价必须在所有药材确认后进行
- **验证规则**: 处方完整性和药材配伍安全性验证

#### FR-004: 数据一致性验证

- **描述**: 定期验证关键业务数据的一致性
- **检查项目**:
  - 医疗案例和诊断记录的关联完整性
  - 处方和药材关联的数据完整性
  - 患者状态和案例状态的一致性
- **修复机制**: 发现不一致时自动修复或标记待处理

### 非功能需求

#### NFR-001: 性能要求

- **事务响应时间**: < 2秒 (95%的情况)
- **并发事务支持**: 支持10个并发事务无冲突
- **数据库锁定时间**: < 500ms (避免长时间锁定)

#### NFR-002: 可靠性要求

- **事务成功率**: > 99.9%
- **补偿成功率**: > 99.5%
- **数据一致性**: 100% (零容忍不一致)

#### NFR-003: 可维护性要求

- **事务日志**: 完整记录每个事务步骤和结果
- **监控接口**: 提供事务执行统计和监控接口
- **调试支持**: 支持事务执行过程跟踪和调试

## 🔧 技术实现

### 开发任务分解

#### 任务1: 事务基础设施 (10天)

- [ ] 设计事务协调器接口和基类
- [ ] 实现补偿事务模式
- [ ] 创建事务执行上下文管理
- [ ] 实现事务日志和监控

**交付物**:

- `Core/LYBT.Infrastructure/Transaction/` 目录
- `ITransactionCoordinator.cs`
- `TransactionCoordinator.cs` 
- `TransactionDefinition.cs`
- `TransactionStep.cs` 基类

#### 任务2: 诊疗流程事务保护 (10天)

- [ ] 实现诊疗流程事务定义
- [ ] 创建医疗案例创建步骤
- [ ] 创建诊断记录创建步骤  
- [ ] 实现相应的补偿步骤
- [ ] 集成到现有业务服务中

**交付物**:

- `StartConsultationTransaction.cs`
- `CreateMedicalCaseStep.cs`
- `CreateConsultationStep.cs`
- 相应的补偿步骤类
- 更新的 `ConsultationBusinessService.cs`

#### 任务3: 处方创建事务保护 (10天)

- [ ] 实现处方创建事务定义  
- [ ] 创建处方验证步骤
- [ ] 创建药材关联步骤
- [ ] 实现价格计算步骤
- [ ] 实现相应的补偿步骤

**交付物**:

- `CreatePrescriptionTransaction.cs` 
- `ValidatePrescriptionStep.cs`
- `CreatePrescriptionHerbsStep.cs`
- 相应的补偿步骤类
- 更新的 `PrescriptionBusinessService.cs`

### 代码实现示例

#### 事务协调器核心实现

```csharp
public class TransactionCoordinator : ITransactionCoordinator
{
    private readonly ILogger<TransactionCoordinator> _logger;
    private readonly ITransactionLogger _transactionLogger;

    public async Task<TransactionResult<T>> ExecuteAsync<T>(TransactionDefinition<T> definition)
    {
        var transactionId = Guid.NewGuid();
        var context = new TransactionContext<T> 
        { 
            TransactionId = transactionId,
            TransactionName = definition.TransactionName
        };

        try
        {
            _logger.LogInformation($"开始执行事务: {definition.TransactionName} [{transactionId}]");

            // 执行所有步骤
            foreach (var step in definition.Steps)
            {
                var stepResult = await step.ExecuteAsync(context);
                if (!stepResult.Success)
                {
                    await ExecuteCompensationAsync(definition, context, step);
                    return TransactionResult<T>.Failure(stepResult.ErrorMessage);
                }
                context.ExecutedSteps.Add(step);
            }

            _logger.LogInformation($"事务执行成功: {definition.TransactionName} [{transactionId}]");
            return TransactionResult<T>.Success(context.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"事务执行异常: {definition.TransactionName} [{transactionId}]");
            await ExecuteCompensationAsync(definition, context, null);
            return TransactionResult<T>.Failure(ex.Message);
        }
    }

    private async Task ExecuteCompensationAsync<T>(
        TransactionDefinition<T> definition, 
        TransactionContext<T> context, 
        ITransactionStep<T> failedStep)
    {
        _logger.LogWarning($"开始执行补偿流程: {definition.TransactionName} [{context.TransactionId}]");

        // 逆序执行补偿步骤
        foreach (var compensationStep in definition.CompensationSteps.AsEnumerable().Reverse())
        {
            try
            {
                await compensationStep.CompensateAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"补偿步骤执行失败: {compensationStep.GetType().Name}");
                // 补偿失败需要人工介入
            }
        }
    }
}
```

## 🧪 测试策略

### 单元测试

#### 测试范围

- [ ] 事务协调器核心逻辑
- [ ] 各个事务步骤的执行和补偿
- [ ] 异常情况的处理机制
- [ ] 事务上下文的状态管理

#### 关键测试用例

```csharp
[Test]
public async Task StartConsultation_WhenConsultationCreationFails_ShouldCompensateMedicalCase()
{
    // Arrange
    var transaction = new StartConsultationTransaction(patientId);
    _consultationService.Setup(x => x.CreateAsync(It.IsAny<ConsultationCreateDto>()))
                       .ThrowsAsync(new Exception("诊断创建失败"));

    // Act
    var result = await _coordinator.ExecuteAsync(transaction);

    // Assert
    result.Success.Should().BeFalse();
    _medicalCaseService.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Once);
}
```

### 集成测试

#### 测试场景

- [ ] 完整诊疗流程事务成功场景
- [ ] 诊疗流程各步骤失败场景的补偿
- [ ] 处方创建事务成功场景  
- [ ] 处方创建各步骤失败场景的补偿
- [ ] 并发事务执行无冲突验证

### 性能测试

#### 测试指标

- [ ] 事务执行时间
- [ ] 数据库锁定时间
- [ ] 并发事务处理能力
- [ ] 内存和CPU使用情况

## 📊 验收标准

### 功能验收

- [ ] **事务成功率**: > 99.9%
- [ ] **补偿成功率**: > 99.5% 
- [ ] **数据一致性**: 零不一致事件
- [ ] **业务流程完整性**: 100%通过验证

### 性能验收

- [ ] **响应时间**: 事务执行 < 2秒
- [ ] **并发支持**: 10个并发事务无冲突
- [ ] **资源使用**: 内存增长 < 10%

### 质量验收

- [ ] **代码覆盖率**: > 85%
- [ ] **编译质量**: 零警告零错误
- [ ] **文档完整性**: API文档和使用指南

## 🚀 部署计划

### 环境准备

- [ ] 开发环境事务功能测试
- [ ] 测试环境集成测试验证
- [ ] 生产环境灰度发布

### 发布策略

1. **Phase 1**: 事务基础设施发布 (不影响现有功能)
2. **Phase 2**: 诊疗流程事务保护 (A/B测试)
3. **Phase 3**: 处方创建事务保护 (逐步推广)

### 回滚计划

- 保留原有业务服务实现
- 提供功能开关控制新旧实现切换
- 监控关键指标，异常时快速回滚

## 📈 监控和运维

### 关键监控指标

- **事务执行统计**: 成功率、失败率、平均执行时间
- **补偿执行统计**: 触发次数、成功率、补偿耗时
- **数据一致性检查**: 定期验证结果和修复统计

### 告警机制

- 事务成功率低于99%时告警
- 补偿成功率低于95%时告警  
- 发现数据不一致时立即告警

### 运维工具

- 事务执行日志查询界面
- 数据一致性检查工具
- 手动补偿执行工具

---

## 📞 项目信息

**需求负责人**: Senior .NET Architecture Analyst  
**开发预估**: 30工作日  
**测试预估**: 10工作日  
**发布时间**: Phase 1 实施期  
**风险等级**: 🔴 → 🟢 (高风险缓解)