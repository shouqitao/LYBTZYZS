# Explanation（概念解释）总览

> **文档类型**：Explanation（理解导向 + 理论导向）
> **适用场景**：深入理解架构设计、设计决策、业务规则
> **目标读者**：架构师、需要深入理解系统的开发者

**版本**：v6.0 Diátaxis框架版
**更新时间**：2025-10-29
**维护团队**：架构组

---

## 🎯 什么是 Explanation？

Explanation 是**理解导向的概念解释**，解释系统的架构设计、设计决策背后的原因、业务规则的由来。Explanation的核心特点是：
- ✅ **深入解释**：不仅说"是什么"，更重要的是说"为什么"
- ✅ **提供背景**：说明设计的历史演进和决策依据
- ✅ **讨论权衡**：分析不同方案的优缺点和选择理由
- ✅ **建立理解**：帮助读者建立系统性的认知框架

### 📚 与其他文档类型的区别

| 对比项 | Explanation | Tutorial | How-to Guides | Reference |
|-------|-------------|----------|---------------|-----------|
| **目标** | 理解概念 | 学习 | 解决问题 | 查阅信息 |
| **受众** | 架构师 | 新手 | 实践者 | 所有人 |
| **场景** | 深入理解设计 | 第一次接触 | 完成特定任务 | 查找API/配置 |
| **特点** | 深入解释 | 手把手引导 | 步骤清晰 | 精确简洁 |

**何时使用 Explanation？**
- ✅ 你想理解为什么选择三层架构而不是其他架构
- ✅ 你想知道某个设计决策背后的考量和权衡
- ✅ 你想理解业务规则的来源和演进过程
- ✅ 你想学习系统中使用的设计模式和最佳实践

---

## 📂 Explanation 分类

### 🏗️ 架构说明（Architecture Documentation）

**入口**：[架构总览](architecture/README.md)

**适用场景**：理解系统的整体架构和技术选型

**核心文档**：

#### 分层架构
- **[Server端架构](architecture/server/README.md)** - 三层架构设计说明
  - 为什么采用三层架构
  - 每层的职责和边界
  - DDD聚合根设计
  - Repository模式的应用

- **[Client端架构](architecture/client/README.md)** - MVVM架构设计说明
  - 为什么采用MVVM模式
  - 五层架构设计（Shell/Contracts/Modules/Core/Infrastructure）
  - Prism框架的选择
  - 数据绑定和命令模式

- **[共享架构](architecture/shared/README.md)** - 跨端共享设计说明
  - 哪些组件可以共享
  - 跨端认证方案
  - 共享模型设计

#### 架构决策记录（ADR）
- **[ADR总览](architecture/decisions/README.md)** - 所有架构决策记录
  - [ADR-001: FluentValidation验证框架](architecture/decisions/ADR-001-fluentvalidation-as-validation-framework.md)
  - [ADR-002: AutoMapper映射框架](architecture/decisions/ADR-002-automapper-as-mapping-framework.md)
  - [ADR-003: Repository简化设计](architecture/decisions/ADR-003-repository-simplification.md)
  - [ADR-004: 组件设计指南](architecture/decisions/ADR-004-component-design-guidelines.md)
  - [ADR-005: 聚合根长期架构](architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md) ⭐ 重要
  - [ADR-006: 病案/诊断/处方重构](architecture/decisions/ADR-006-medicalcase-consultation-prescription-refactoring.md)

#### 设计模式
- **[设计模式总览](architecture/patterns/)** - 系统中使用的设计模式
  - [聚合根模式](architecture/patterns/aggregate-root-pattern.md) - DDD聚合根设计
  - [Repository模式](architecture/patterns/repository-pattern.md) - 数据访问模式
  - [MVVM模式](architecture/patterns/mvvm-pattern.md) - 客户端架构模式
  - [组件模式](architecture/patterns/component-pattern.md) - UI组件设计

#### 架构演进
- **[架构演进](architecture/evolution.md)** - 架构的历史演进过程
  - v1.0 → v2.0 → v3.0 → v4.0 → v5.0 → v6.0
  - 每个版本的重大变化和原因

- **[架构例外清单](architecture/exceptions.md)** - 不符合架构规范的例外
  - 为什么存在例外
  - 例外的影响和补救措施
  - 何时消除例外

### 📋 业务规则（Business Rules）

**入口**：[业务规则](business-rules.md)

**适用场景**：理解系统的核心业务约束

**核心内容**：
- **14条核心业务规则** - 贯穿整个系统的业务约束
  - 规则来源和演进历史
  - 规则之间的关系和优先级
  - 规则在代码中的实现位置
  - 违反规则的后果

**示例规则**：
- 规则1：一个病案只能有一个有效诊断记录
- 规则2：处方必须关联到诊断记录
- 规则3：药品库存不能为负数
- ...

### 📝 设计文档（Design Documentation）

**入口**：[设计文档目录](design/)

**适用场景**：理解特定功能的技术设计

**文档结构**：
- 功能背景和业务目标
- 技术方案设计
- API端点设计
- 数据库Schema设计
- Phase拆分和实施计划
- 风险评估和缓解措施

**常见设计文档**：
- [患者管理系统设计](design/patient-management-design.md)
- [病案工作流设计](design/medical-case-workflow-design.md)
- [诊断流程设计](design/consultation-process-design.md)
- [处方管理设计](design/prescription-management-design.md)

### 📄 需求文档（Requirements Documentation）

**入口**：[需求文档目录](requirements/)

**适用场景**：理解功能需求的业务价值和验收标准

**文档结构**：
- 功能概述（业务价值、用户故事）
- 业务目标（解决什么问题）
- 验收标准（功能完成的判定条件）
- 关联Issues（整合的现有Issues）
- 优先级和时间估算

**常见需求文档**：
- [患者管理需求](requirements/patient-management-requirements.md)
- [病案工作流需求](requirements/medical-case-workflow-requirements.md)
- [诊断流程需求](requirements/consultation-process-requirements.md)
- [处方管理需求](requirements/prescription-management-requirements.md)

---

## 🚀 我想理解...（常见理解需求）

### 架构相关
- **为什么采用三层架构** → [Server端架构](architecture/server/README.md)
- **为什么采用MVVM模式** → [Client端架构](architecture/client/README.md)
- **为什么选择Prism框架** → [ADR-004: 组件设计指南](architecture/decisions/ADR-004-component-design-guidelines.md)
- **为什么使用聚合根模式** → [聚合根模式](architecture/patterns/aggregate-root-pattern.md)
- **架构如何演进的** → [架构演进](architecture/evolution.md)

### 设计决策
- **为什么选择FluentValidation** → [ADR-001](architecture/decisions/ADR-001-fluentvalidation-as-validation-framework.md)
- **为什么选择AutoMapper** → [ADR-002](architecture/decisions/ADR-002-automapper-as-mapping-framework.md)
- **为什么简化Repository** → [ADR-003](architecture/decisions/ADR-003-repository-simplification.md)
- **聚合根长期规划** → [ADR-005](architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md) ⭐

### 业务规则
- **为什么一个病案只能有一个诊断** → [业务规则](business-rules.md) 规则1
- **为什么处方必须关联诊断** → [业务规则](business-rules.md) 规则2
- **业务规则如何演进** → [业务规则](business-rules.md) 演进历史章节

### 功能设计
- **病案工作流如何设计** → [病案工作流设计](design/medical-case-workflow-design.md)
- **诊断流程为什么这样设计** → [诊断流程设计](design/consultation-process-design.md)
- **处方管理的技术方案** → [处方管理设计](design/prescription-management-design.md)

---

## 📚 相关文档

### 学习系统（新手）
如果你是第一次接触系统，推荐先学习：
- [Tutorial总览](../tutorials/README.md) - 学习导向的引导式教程
- [5分钟快速开始](../tutorials/quick-start.md) - 快速启动系统
- [开发第一个功能](../tutorials/first-feature.md) - 完整开发流程演示

### 解决具体问题
需要完成特定开发任务，请查阅：
- [How-to Guides总览](../how-to-guides/README.md) - 任务导向的操作指南
- [Server端操作](../how-to-guides/server/README.md) - 后端开发指南
- [Client端操作](../how-to-guides/client/README.md) - 前端开发指南

### 查阅技术细节
需要查找API、配置、命令等技术细节，请查阅：
- [Reference总览](../reference/README.md) - 信息导向的参考手册
- [API参考](../reference/quick-reference/api-reference.md) - 所有API端点
- [代码模式](../reference/quick-reference/code-patterns.md) - 常用代码模式

---

## 🎯 使用建议

### 如何使用 Explanation

1. **带着问题阅读**
   Explanation文档通常较长，带着具体问题阅读效果更好（例如："为什么要用聚合根？"）

2. **关注"为什么"**
   Explanation的价值在于解释决策原因和权衡，不要只看结论

3. **对比不同方案**
   ADR文档通常会列出多个方案的对比，有助于理解选择的合理性

4. **结合代码阅读**
   理解概念后，结合实际代码加深理解

5. **参与讨论**
   如果对某个设计有疑问或建议，欢迎在GitHub Issues中讨论

### Explanation 的局限性

Explanation **不适合**以下场景：
- ❌ 学习如何使用系统 → 请查阅[Tutorial](../tutorials/README.md)
- ❌ 解决具体开发问题 → 请查阅[How-to Guides](../how-to-guides/README.md)
- ❌ 查找API定义 → 请查阅[Reference](../reference/README.md)

Explanation **侧重于**：
- ✅ 为什么这样设计
- ✅ 设计的演进过程
- ✅ 不同方案的权衡
- ✅ 设计的长期影响

---

## 🔄 文档维护

### 贡献指南

欢迎贡献新的Explanation内容！优秀的概念解释应该：
- ✅ **解释"为什么"**：不仅说是什么，更重要的是为什么
- ✅ **提供背景**：说明设计的历史演进和决策依据
- ✅ **讨论权衡**：分析不同方案的优缺点
- ✅ **引用证据**：引用代码、ADR、业务需求等证据
- ✅ **结构清晰**：使用标题、列表、图表提高可读性

### ADR编写规范

新增架构决策时，必须创建ADR文档：
- 使用[ADR模板](architecture/decisions/template.md)
- 编号递增（ADR-XXX）
- 状态标记：Proposed → Accepted → Implemented
- 记录决策背景、方案对比、后果分析

### 文档更新记录

- **v6.0 (2025-10-29)**: Diátaxis框架重构，新建Explanation分类
- **v5.0 (2025-10-15)**: 三层对齐架构重组
- **v4.0 (2025-09-20)**: 完善ADR文档
- **v3.0 (2025-08-10)**: 新增业务规则文档

---

**最后更新**：2025-10-29
**文档版本**：v6.0（Diátaxis框架重构版）
