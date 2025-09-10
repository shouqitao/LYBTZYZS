---
name: 01-core-optimization
status: backlog
created: 2025-09-08T10:24:53Z
progress: 0%
prd: .claude/prds/01-core-optimization.md
github: [Will be updated when synced to GitHub]
---

# Epic: 01-core-optimization

## Overview

实现核心架构优化，通过补偿事务模式和Saga事务协调器解决系统数据一致性缺口。重点保护诊疗流程(MedicalCase+Consultation)和处方创建(Prescription+Herbs)的关键业务场景，确保跨模块操作的数据完整性和业务流程可靠性。

## Architecture Decisions

- **事务模式选择**: 补偿事务模式 + 单一聚合根事务，避免分布式事务的复杂性
- **协调器设计**: 通用事务协调器(ITransactionCoordinator)支持多步骤事务和自动补偿
- **技术栈**: 基于现有EF Core和.NET 8架构，无需引入新的分布式事务组件
- **设计模式**: Saga模式处理长时间运行的业务流程，Command模式封装事务步骤

## Technical Approach

### Frontend Components
- 无需修改现有UI组件，保持ViewModel接口不变
- 通过Service层透明集成事务保护机制
- 添加事务执行状态的用户反馈机制

### Backend Services
- **事务基础设施**: 
  - `ITransactionCoordinator` - 核心事务协调器接口
  - `TransactionDefinition<T>` - 事务定义和步骤配置
  - `ITransactionStep<T>` - 事务步骤抽象接口
- **业务事务实现**:
  - `StartConsultationTransaction` - 诊疗流程事务定义
  - `CreatePrescriptionTransaction` - 处方创建事务定义
- **补偿机制**: 每个事务步骤配对实现补偿逻辑

### Infrastructure
- 扩展现有`AppDbContext`支持事务协调
- 集成到现有日志和监控体系
- 保持单体应用架构，无需微服务拆分

## Implementation Strategy

- **Phase 1** (10天): 事务基础设施开发和单元测试
- **Phase 2** (10天): 诊疗流程事务保护集成
- **Phase 3** (10天): 处方创建事务保护集成
- **风险缓解**: 功能开关支持新旧实现切换，A/B测试验证
- **测试策略**: 单元测试、集成测试、并发测试全覆盖

## Task Breakdown Preview

High-level task categories that will be created:
- [ ] **事务基础设施**: 协调器、步骤接口、上下文管理 (10天)
- [ ] **诊疗流程保护**: 医疗案例+诊断记录事务化 (10天)  
- [ ] **处方创建保护**: 处方+药材关联事务化 (10天)
- [ ] **监控和测试**: 事务监控、日志记录、测试套件 (集成到上述任务)

## Dependencies

- **现有架构**: 依赖当前EF Core和AppDbContext架构
- **业务服务**: 需要MedicalCase、Consultation、Prescription等业务服务配合
- **无外部依赖**: 不引入新的分布式事务组件，基于现有技术栈
- **测试数据**: 需要完整的测试数据集验证事务场景

## Success Criteria (Technical)

- **事务成功率**: > 99.9%
- **补偿成功率**: > 99.5%  
- **响应时间**: 事务执行 < 2秒 (95%情况)
- **并发支持**: 10个并发事务无冲突
- **代码覆盖率**: > 85%
- **零数据不一致**: 通过完整的数据一致性验证

## Tasks Created
- [ ] 001.md - 设计和实现事务协调器核心接口 (parallel: true)
- [ ] 002.md - 实现事务协调器执行引擎 (parallel: false)
- [ ] 003.md - 创建通用事务步骤基类和实用工具 (parallel: true)
- [ ] 004.md - 设计诊疗流程事务定义和步骤 (parallel: false)
- [ ] 005.md - 设计处方创建事务定义和步骤 (parallel: false)
- [ ] 006.md - 集成事务协调器到现有业务服务 (parallel: false)
- [ ] 007.md - 实现事务监控和性能优化 (parallel: true)
- [ ] 008.md - 完善测试套件和文档体系 (parallel: true)

Total tasks: 8
Parallel tasks: 4
Sequential tasks: 4
Estimated total effort: 178 hours (约22.25工作日)

## Estimated Effort

- **总体工期**: 30工作日
- **开发资源**: 1名高级开发工程师全职
- **测试工期**: 额外10工作日 (包含在实施计划中)
- **关键路径**: 事务基础设施 → 诊疗流程集成 → 处方创建集成
- **风险缓冲**: 每个阶段预留2天风险缓冲时间