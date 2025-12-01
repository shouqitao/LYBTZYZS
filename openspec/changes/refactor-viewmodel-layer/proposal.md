# OpenSpec Proposal: refactor-viewmodel-layer

## 元数据

- **提案ID**: refactor-viewmodel-layer
- **创建日期**: 2025-11-30
- **状态**: Draft
- **关联**: service-conventions, client-api-conventions

## Why

Client端ViewModel层存在以下技术债务需要解决：
1. **大型ViewModel问题** - 3个ViewModel超过1400行，违反单一职责原则
2. **代码模式不一致** - 命令初始化、错误处理、异步模式存在重复和不统一
3. **MedicalCase模块缺少Components分离** - 其他模块已采用Components模式，但核心模块未采用

## What Changes

### Phase 1: ViewModel设计规范创建
- 创建viewmodel-conventions spec，定义ViewModel设计标准

### Phase 2: Components分层提取
- 为MedicalCase模块添加Components分层
- 拆分大型ViewModel

### Phase 3: 代码模式统一
- 统一命令初始化模式
- 统一错误处理模式
- 统一异步执行模式

## 问题陈述

### 背景

当前Client端ViewModel层架构分析结果：
- **总ViewModel数量**: 45个
- **基类体系**: ViewModelBase → UnifiedViewModelBase → UnifiedListViewModelBase（四层继承）
- **Components模式采用率**: 4/8模块（50%）

### 当前状态

#### 大型ViewModel统计

| ViewModel | 行数 | 问题 |
|-----------|------|------|
| MedicalCaseWorkspaceViewModel | 1544行 | 职责过多，缺少Components分离 |
| PrescriptionPanelViewModel | 1484行 | 混合编辑、计算、验证逻辑 |
| PatientSelectionViewModel | 1429行 | 搜索、分页、队列功能混杂 |

#### Components采用情况

| 模块 | Components | 状态 |
|------|-----------|------|
| Formula | CommandHandler, DataManager, Calculator, Validator | 已分离 |
| Patients | CommandHandler, DataManager, Validator | 已分离 |
| Prescriptions | CommandHandler, DataManager, Calculator, Validator, EventCoordinator | 已分离 |
| Users | CommandHandler | 部分分离 |
| **MedicalCase** | **无** | **需要添加** |

#### 代码重复问题

1. **命令初始化模式** - 每个ViewModel重复3-8行初始化代码
2. **异步加载模式** - 12+处相似的加载数据代码
3. **错误处理风格** - 3种不同的错误处理方式混用

### 需要做的

1. 创建viewmodel-conventions spec作为设计规范
2. 为MedicalCase模块添加Components分层
3. 拆分3个大型ViewModel
4. 提取统一的代码模式到基类或辅助类

## 受影响的组件

### 新增规范
- `openspec/specs/viewmodel-conventions/spec.md`

### 重构代码

#### MedicalCase模块
- `MedicalCaseWorkspaceViewModel.cs` - 拆分
- 新增: `MedicalCaseCommandHandler.cs`
- 新增: `MedicalCaseDataManager.cs`
- 新增: `MedicalCaseValidator.cs`

#### 基础设施层
- `ViewModelBase.cs` - 增强错误处理模板
- 新增: `CommandFactory.cs` - 命令初始化工厂

#### 其他大型ViewModel（按需）
- `PrescriptionPanelViewModel.cs`
- `PatientSelectionViewModel.cs`

## 成功标准

1. viewmodel-conventions spec通过validation
2. MedicalCaseWorkspaceViewModel行数 < 400行
3. 所有ViewModel遵循统一的错误处理模式
4. 编译通过，现有功能不受影响
5. 单元测试通过

## 风险评估

- **风险**: 中等
- **原因**: 涉及核心MedicalCase模块重构
- **缓解**: 分Phase执行，每Phase独立验证

## 工作量估算

- **Phase 1（规范文档）**: 小
- **Phase 2（Components分层）**: 中
- **Phase 3（代码模式统一）**: 中

---

**提案状态**: Draft
