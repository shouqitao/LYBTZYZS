# Desktop Core Layer Architecture

本规范定义Desktop Core层的项目结构和职责划分。

## MODIFIED Requirements

### Requirement: REQ-CORE-001 Desktop Core项目结构

Desktop Core层 **SHALL** 包含4个项目，职责明确，依赖方向正确。

**当前状态**: 5个项目，存在职责重叠和依赖方向问题

**目标状态**: 4个项目，职责清晰

#### Scenario: 项目结构验证

**Given** Desktop Core层源代码目录
**When** 检查项目数量和结构
**Then** 应存在以下4个项目:
  - LYBT.Desktop.Contracts (纯接口定义)
  - LYBT.Desktop.Foundation (技术基础设施)
  - LYBT.Desktop.Infrastructure (WPF基础设施)
  - LYBT.Desktop.Models (ViewModel和业务模型)
**And** 不应存在LYBT.Desktop.Presentation项目

---

### Requirement: REQ-CORE-002 Contracts层职责

Contracts项目 **MUST** 包含所有可被Module引用的接口定义。

#### Scenario: Contracts接口完整性

**Given** LYBT.Desktop.Contracts项目
**When** 检查接口定义
**Then** 应包含以下目录:
  - Api/ (Refit API接口)
  - Services/ (服务接口，原Infrastructure.Interfaces)
  - Components/ (组件接口，原Infrastructure.Interfaces.Components)
**And** 不应依赖WPF相关包

---

### Requirement: REQ-CORE-003 Foundation层职责

Foundation项目 **SHALL** 提供技术基础设施服务。

#### Scenario: Foundation职责范围

**Given** LYBT.Desktop.Foundation项目
**When** 检查功能模块
**Then** 应包含以下模块:
  - Security/ (认证、Token管理)
  - Http/ (HTTP客户端、DelegatingHandlers)
  - Caching/ (缓存服务)
  - Logging/ (日志相关)
  - Configuration/ (配置管理)
**And** 应依赖Contracts
**And** 不应依赖Infrastructure或Models

---

### Requirement: REQ-CORE-004 Infrastructure层职责

Infrastructure项目 **MUST** 提供WPF基础设施，包含合并后的Presentation内容。

#### Scenario: Infrastructure职责范围

**Given** LYBT.Desktop.Infrastructure项目
**When** 检查功能模块
**Then** 应包含以下模块:
  - Controls/ (通用WPF控件，包括原Presentation.Components)
  - Converters/ (值转换器)
  - Services/ (WPF服务实现，包括通知服务)
  - Themes/ (主题和样式)
  - Behaviors/ (附加行为)
  - Events/ (事件定义)
**And** 应依赖Foundation
**And** 不应包含Interfaces/目录(已迁移到Contracts)

---

### Requirement: REQ-CORE-005 Models层职责

Models项目 **SHALL** 提供ViewModel基类和业务模型，且 **MUST** 不依赖Infrastructure。

#### Scenario: Models依赖正确性

**Given** LYBT.Desktop.Models项目
**When** 检查项目引用
**Then** 应依赖Contracts
**And** 应依赖Foundation
**And** 不应依赖Infrastructure

---

### Requirement: REQ-CORE-006 依赖方向

Core层项目间的依赖 **MUST** 遵循单向依赖原则。

#### Scenario: 依赖链验证

**Given** Desktop Core层所有项目
**When** 分析项目引用
**Then** 依赖方向应为:
  - Contracts → Shared.Models (仅)
  - Foundation → Contracts
  - Infrastructure → Foundation
  - Models → Contracts, Foundation
**And** 不应存在循环依赖
**And** Models不应依赖Infrastructure
