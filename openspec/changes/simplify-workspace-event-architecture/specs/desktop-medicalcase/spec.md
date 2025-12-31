# Spec Delta: desktop-medicalcase

## Overview

本变更对desktop-medicalcase模块的通信模式、状态管理和组件结构进行简化：
1. 移除模块内部的PubSubEvent使用，改用直接方法调用和Action回调
2. 简化状态追踪，使用枚举和派生属性替代冗余UI属性
3. 内联过小的独立组件

## ADDED Requirements

### Requirement: STATE-001 面板状态枚举

子面板 SHALL 使用统一的 `PanelStatus` 枚举表示状态。

**规范**:
- 枚举值: `NotStarted`, `InProgress`, `Completed`
- 每个子ViewModel SHALL 维护自己的 `Status` 属性
- 状态变更 SHALL 触发 `PropertyChanged` 通知

#### Scenario: 诊断面板状态管理
- **GIVEN** ConsultationPanelViewModel初始化
- **WHEN** 首次加载
- **THEN** Status SHALL 为 `NotStarted`
- **AND** 开始编辑时 SHALL 变为 `InProgress`
- **AND** 诊断完成时 SHALL 变为 `Completed`

#### Scenario: 处方面板状态管理
- **GIVEN** PrescriptionPanelViewModel初始化
- **WHEN** 首次加载
- **THEN** Status SHALL 为 `NotStarted`
- **AND** 开始编辑时 SHALL 变为 `InProgress`
- **AND** 处方保存成功时 SHALL 变为 `Completed`

---

### Requirement: STATE-002 父ViewModel派生属性模式

父ViewModel SHALL 使用派生属性从子ViewModel读取状态，而非维护冗余副本。

**规范**:
- 父VM SHALL NOT 复制子VM的状态属性
- 父VM的状态属性 SHALL 直接委托给子VM
- 派生属性 SHALL 在子VM属性变更时自动更新

#### Scenario: NeedsPrescription派生
- **GIVEN** WorkspaceViewModel需要访问是否需要处方
- **WHEN** 实现属性访问
- **THEN** SHALL 返回 `ConsultationPanelViewModel.NeedsPrescription`
- **AND** SHALL NOT 维护本地副本

#### Scenario: CanComplete自动计算
- **GIVEN** WorkspaceViewModel需要确定是否可完成医案
- **WHEN** 访问 `CanComplete` 属性
- **THEN** SHALL 自动计算: 诊断完成 && (不需要处方 || 处方完成)
- **AND** SHALL NOT 需要手动触发更新

---

### Requirement: STATE-003 状态UI转换器模式

状态到UI属性的转换 SHALL 使用ValueConverter，而非维护多个UI属性。

**规范**:
- `PanelStatusToTextConverter` SHALL 转换状态到显示文本
- `PanelStatusToColorConverter` SHALL 转换状态到显示颜色
- UI绑定 SHALL 使用Converter而非直接绑定多个属性

#### Scenario: 诊断状态显示
- **GIVEN** UI需要显示诊断状态
- **WHEN** 绑定状态属性
- **THEN** SHALL 使用 `{Binding ConsultationStatus, Converter={StaticResource PanelStatusToTextConverter}}`
- **AND** SHALL 使用 `{Binding ConsultationStatus, Converter={StaticResource PanelStatusToColorConverter}}`

---

### Requirement: COMM-001 父子ViewModel直接调用模式

父ViewModel SHALL 通过直接方法调用与子ViewModel通信，而非使用PubSubEvent。

**规范**:
- 父ViewModel持有子ViewModel引用 SHALL 直接调用其公共方法
- 公共方法 SHALL 返回操作结果供父ViewModel处理
- 此模式 SHALL 仅适用于同一ViewModel树内通信

#### Scenario: 父请求子保存数据
- **GIVEN** WorkspaceViewModel需要保存处方数据
- **WHEN** 触发保存操作
- **THEN** SHALL 直接调用 `await _prescriptionPanelViewModel.SaveAsync()`
- **AND** SHALL 处理返回的SaveResult

---

### Requirement: COMM-002 子ViewModel回调通知模式

子ViewModel SHALL 使用Action回调通知父ViewModel操作完成，而非使用PubSubEvent。

**规范**:
- 子ViewModel构造函数 MAY 接受可选的Action回调参数
- 操作完成后 SHALL 调用回调（如已提供）
- 回调参数 SHALL 使用简单DTO传递必要信息

#### Scenario: 子通知父保存完成
- **GIVEN** PrescriptionPanelViewModel保存成功
- **WHEN** API返回成功
- **THEN** SHALL 调用 `_onSaved?.Invoke(result)`
- **AND** 结果 SHALL 包含PrescriptionId和RowVersion

---

### Requirement: COMM-003 跨模块事件保留策略

跨模块通信 SHALL 继续使用PubSubEvent机制。

**规范**:
- 被其他模块订阅的事件 SHALL 保留
- `CaseEvents.ConsultationCompletedEvent` SHALL 保留（待诊队列订阅）
- `CaseEvents.PrescriptionCompletedEvent` SHALL 保留（其他模块订阅）

#### Scenario: 跨模块事件使用
- **GIVEN** 医案完成需要通知其他模块
- **WHEN** 医案状态变为Completed
- **THEN** SHALL 使用 `CaseEvents.ConsultationCompletedEvent`
- **AND** SHALL 使用 `CaseEvents.PrescriptionCompletedEvent`

---

### Requirement: COMP-001 组件整合原则

小型无复用组件 SHALL 内联到使用者中，以减少不必要的间接层。

**整合标准**:
- 行数<150 且 单一使用者 且 无测试覆盖 → SHALL 内联
- 有单元测试覆盖 → SHALL 保留独立
- 被多个组件使用 → SHALL 保留独立
- 行数>300 → SHALL 保留独立

#### Scenario: WorkspaceStatusDisplay内联
- **GIVEN** WorkspaceStatusDisplay仅129行且仅被WorkspaceVM使用
- **WHEN** 评估组件整合
- **THEN** SHALL 将代码内联到WorkspaceViewModel
- **AND** SHALL 使用 `#region Status Display` 组织代码
- **AND** SHALL 删除独立文件

---

## MODIFIED Requirements

### Requirement: VM-002 工作区通信模式补充

ViewModel树内部通信 SHALL 优先使用直接方法调用和Action回调，而非PubSubEvent，补充viewmodel-conventions规范VM-002关于工作区通信模式的说明。

**通信方式选择标准**:
| 通信场景 | 推荐方式 |
|---------|---------|
| 父→子 | 直接方法调用 |
| 子→父 | Action回调 |
| 跨模块 | PubSubEvent |

#### Scenario: 通信方式选择
- **GIVEN** ViewModel需要与另一ViewModel通信
- **WHEN** 选择通信方式
- **THEN** 同一ViewModel树内 SHALL 使用直接调用或回调
- **AND** 跨模块 SHALL 使用PubSubEvent
- **AND** SHALL NOT 在同一树内使用PubSubEvent

---

## REMOVED Requirements

### Requirement: 移除模块内部事件定义

以下模块内部事件 SHALL 被移除。

#### Scenario: SaveAllRequestedEvent移除
- **GIVEN** SaveAllRequestedEvent用于父请求子保存
- **WHEN** 重构为直接方法调用
- **THEN** SHALL 删除SaveAllRequestedEvent类
- **AND** SHALL 改用 `_prescriptionPanelViewModel.SaveAsync()` 直接调用

#### Scenario: PrescriptionSavedEvent移除
- **GIVEN** PrescriptionSavedEvent用于子通知父保存完成
- **WHEN** 重构为Action回调
- **THEN** SHALL 删除PrescriptionSavedEvent类及其Payload
- **AND** SHALL 改用构造函数注入的 `Action<PrescriptionSaveResult>` 回调

#### Scenario: PrescriptionDataChangedEvent移除
- **GIVEN** PrescriptionDataChangedEvent定义但无订阅者
- **WHEN** 代码审查确认无使用
- **THEN** SHALL 直接删除该事件类
- **AND** 无需替代方案

---

### Requirement: 移除冗余状态属性

以下冗余状态属性 SHALL 被移除或简化。

#### Scenario: NeedsPrescription重复移除
- **GIVEN** NeedsPrescription在WorkspaceVM和ConsultationVM重复定义
- **WHEN** 简化状态模型
- **THEN** SHALL 保留ConsultationVM中的定义（数据源）
- **AND** WorkspaceVM SHALL 改为派生属性

#### Scenario: 8个状态UI属性移除
- **GIVEN** WorkspaceVM有8个状态UI属性需要手动维护
- **WHEN** 简化状态模型
- **THEN** SHALL 移除这8个属性
- **AND** SHALL 改用派生属性+Converter

---

### Requirement: 移除过小独立组件

以下过小组件 SHALL 被内联移除。

#### Scenario: WorkspaceStatusDisplay移除
- **GIVEN** WorkspaceStatusDisplay仅129行
- **WHEN** 评估组件必要性
- **THEN** SHALL 删除独立文件
- **AND** 代码 SHALL 内联到WorkspaceViewModel
- **AND** SHALL 使用region组织内联代码

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| viewmodel-conventions | VM-002 通信模式的工作区场景补充 |
| module-communication | 明确Desktop层事件使用边界 |
| wpf-binding-conventions | 状态Converter使用规范 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-31 | 1.0 | 初始版本，定义工作区事件架构规范 |
| 2025-12-31 | 2.0 | 简化版本，采用KISS原则 |
| 2025-12-31 | 2.1 | 增加组件整合规范 |
| 2025-12-31 | 3.0 | 增加状态简化规范(STATE-001~003) |
