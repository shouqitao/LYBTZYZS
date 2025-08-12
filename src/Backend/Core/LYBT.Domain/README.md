# LYBT领域层 - Domain Driven Design

## 架构说明

本目录实现了领域驱动设计（DDD）的核心概念：

### 目录结构

```
LYBT.Domain/
├── Aggregates/        # 聚合根
├── Entities/          # 实体
├── ValueObjects/      # 值对象
├── DomainEvents/      # 领域事件
├── Services/          # 领域服务
├── Specifications/    # 业务规约
├── Exceptions/        # 领域异常
└── SeedWork/          # 基础设施

```

### 核心概念

1. **聚合根（Aggregate Root）**
   - 保证业务一致性的边界
   - 控制对内部实体的访问
   - 发布领域事件

2. **实体（Entity）**
   - 具有唯一标识
   - 生命周期管理
   - 业务行为封装

3. **值对象（Value Object）**
   - 不可变性
   - 相等性比较
   - 业务概念表达

4. **领域事件（Domain Event）**
   - 业务状态变化通知
   - 解耦业务逻辑
   - 事件溯源支持

5. **领域服务（Domain Service）**
   - 跨聚合业务逻辑
   - 复杂业务计算
   - 业务规则验证

## 设计原则

- **充血模型**：业务逻辑在领域对象内
- **聚合设计**：小而内聚的聚合
- **最终一致性**：通过领域事件实现
- **业务语言**：使用业务术语命名