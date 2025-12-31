# Spec Delta: desktop-controls

## Overview

本变更定义高内聚的 `HerbEditorControl` 控件规范，用于统一药材编辑功能。

## ADDED Requirements

### Requirement: CTRL-001 HerbEditorControl 控件定义

`HerbEditorControl` SHALL 作为药材编辑的统一控件，封装所有药材编辑逻辑。

**控件职责**:
- 药材项的CRUD操作
- 价格自动计算
- 重复药材检测
- 空槽位管理
- 输出药材列表

#### Scenario: 控件基本使用
- **GIVEN** 页面需要药材编辑功能
- **WHEN** 使用 `HerbEditorControl`
- **THEN** SHALL 绑定 `AllHerbs` 提供可选药材
- **AND** SHALL 通过 `HerbList` 获取编辑结果
- **AND** SHALL 通过 `ItemCount`, `SingleDosagePrice`, `TotalPrice` 获取统计信息

---

### Requirement: CTRL-002 HerbEditorControl 输入属性

控件 SHALL 定义以下输入 DependencyProperty。

| 属性 | 类型 | 说明 |
|-----|------|------|
| AllHerbs | ObservableCollection<HerbListDto> | 可选药材列表 |
| IsReadOnly | bool | 是否只读模式 |
| DosageCount | int | 剂数(用于总价计算) |

#### Scenario: 只读模式
- **GIVEN** `IsReadOnly = true`
- **WHEN** 用户尝试编辑药材
- **THEN** SHALL 禁用所有编辑功能
- **AND** SHALL 隐藏删除按钮

---

### Requirement: CTRL-003 HerbEditorControl 输出属性

控件 SHALL 定义以下只读输出属性。

| 属性 | 类型 | 说明 |
|-----|------|------|
| HerbList | IReadOnlyList<HerbItemDto> | 药材列表输出 |
| ItemCount | int | 有效药材数量 |
| SingleDosagePrice | decimal | 单剂价格 |
| TotalPrice | decimal | 总价(单剂*剂数) |
| HasDuplicates | bool | 是否有重复药材 |
| DuplicateWarning | string | 重复药材警告文本 |
| IsValid | bool | 列表是否有效 |

#### Scenario: 价格自动计算
- **GIVEN** 用户添加或修改药材
- **WHEN** 药材的剂量或选择变化
- **THEN** SHALL 自动重新计算 `SingleDosagePrice`
- **AND** SHALL 自动重新计算 `TotalPrice`
- **AND** SHALL 更新 `ItemCount`

#### Scenario: 重复检测自动执行
- **GIVEN** 药材列表变化
- **WHEN** 存在相同HerbId的多个药材
- **THEN** `HasDuplicates` SHALL 为 `true`
- **AND** `DuplicateWarning` SHALL 包含重复药材名称

---

### Requirement: CTRL-004 HerbEditorControl 公共方法

控件 SHALL 提供以下公共方法供外部调用。

| 方法 | 说明 |
|-----|------|
| LoadFromDto(items) | 从DTO加载药材数据 |
| AddHerbs(items) | 添加药材(用于导入) |
| Clear() | 清空所有药材 |
| Validate() | 手动触发校验 |

#### Scenario: 加载已有数据
- **GIVEN** 需要编辑已有处方
- **WHEN** 调用 `LoadFromDto(existingItems)`
- **THEN** SHALL 清空当前药材
- **AND** SHALL 加载传入的药材数据
- **AND** SHALL 触发价格重新计算

#### Scenario: 导入药材
- **GIVEN** 用户从方剂导入药材
- **WHEN** 调用 `AddHerbs(formulaItems)`
- **THEN** SHALL 将新药材添加到列表末尾
- **AND** SHALL 触发重复检测
- **AND** SHALL 触发价格重新计算

---

### Requirement: CTRL-005 HerbEditorControl 事件

控件 SHALL 定义以下事件供外部订阅。

| 事件 | 说明 |
|-----|------|
| HerbListChanged | 药材列表变化时触发 |
| FormulaImportRequested | 用户点击导入方剂按钮 |
| HistoryCopyRequested | 用户点击复制历史按钮 |

#### Scenario: 导入请求处理
- **GIVEN** 用户点击"导入方剂"按钮
- **WHEN** 控件触发 `FormulaImportRequested` 事件
- **THEN** 外部ViewModel SHALL 处理对话框显示
- **AND** 导入结果 SHALL 通过 `AddHerbs()` 方法注入

---

### Requirement: DTO-001 HerbItemDto 数据结构

`HerbItemDto` SHALL 作为控件的输出数据结构。

```csharp
public class HerbItemDto
{
    public Guid HerbId { get; init; }
    public string HerbName { get; init; }
    public decimal Dosage { get; init; }
    public string Unit { get; init; }
    public string? DecocteMethod { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal ItemTotal { get; init; }
    public bool IsValid { get; init; }
    public string? ValidationMessage { get; init; }
}
```

#### Scenario: DTO转换
- **GIVEN** 需要保存处方
- **WHEN** 获取 `HerbEditorControl.HerbList`
- **THEN** SHALL 返回 `IReadOnlyList<HerbItemDto>`
- **AND** 每个DTO SHALL 包含计算好的 `ItemTotal`
- **AND** 每个DTO SHALL 包含校验状态

---

### Requirement: VAL-001 药材项校验规则

单个药材项 SHALL 在控件内部进行校验。

| 校验项 | 规则 | 级别 |
|-------|------|------|
| HerbId | 不能为空Guid | Error |
| Dosage | 必须 > 0 | Error |
| Dosage | 必须 <= 1000 | Warning |
| HerbName | 不能为空字符串 | Error |

#### Scenario: 剂量校验
- **GIVEN** 用户输入药材剂量
- **WHEN** 剂量 <= 0
- **THEN** `HerbItemDto.IsValid` SHALL 为 `false`
- **AND** `ValidationMessage` SHALL 包含错误描述

---

### Requirement: VAL-002 药材列表校验规则

药材列表 SHALL 在控件级别进行校验。

| 校验项 | 规则 | 级别 |
|-------|------|------|
| 重复药材 | 相同HerbId不能出现多次 | Warning |
| 最小数量 | 保存时至少1个有效药材 | Error |

#### Scenario: 重复药材警告
- **GIVEN** 列表中存在相同药材
- **WHEN** 检测到重复
- **THEN** `HasDuplicates` SHALL 为 `true`
- **AND** 不阻止用户继续编辑
- **AND** SHALL 显示警告提示

---

## MODIFIED Requirements

### Requirement: 处方面板简化

`PrescriptionPanelViewModel` SHALL 简化药材编辑相关逻辑。

**移除的职责**:
- 药材项CRUD操作 → 迁移到 `HerbEditorControl`
- 价格计算 → 迁移到 `HerbEditorControl`
- 重复检测 → 迁移到 `HerbEditorControl`

**保留的职责**:
- 诊断关联校验
- 调用保存API
- 处理导入对话框请求
- 管理剂数、用法等非药材属性

#### Scenario: 保存处方
- **GIVEN** 用户点击保存
- **WHEN** 执行保存逻辑
- **THEN** SHALL 直接从 `HerbEditorControl.HerbList` 获取药材
- **AND** SHALL 转换为 `PrescriptionItemInputDto` 列表
- **AND** SHALL 调用保存API

---

## REMOVED Requirements

### Requirement: 移除独立Handler

以下独立Handler SHALL 被移除，功能迁移到 `HerbEditorControl`。

#### Scenario: PrescriptionItemHandler移除
- **GIVEN** `PrescriptionItemHandler` 功能已迁移
- **WHEN** 重构完成
- **THEN** SHALL 删除 `PrescriptionItemHandler.cs`

#### Scenario: PrescriptionCalculator移除
- **GIVEN** `PrescriptionCalculator` 功能已迁移
- **WHEN** 重构完成
- **THEN** SHALL 删除 `PrescriptionCalculator.cs`

#### Scenario: PrescriptionImportHandler移除
- **GIVEN** 导入功能已迁移
- **WHEN** 重构完成
- **THEN** SHALL 删除 `PrescriptionImportHandler.cs`

#### Scenario: HerbListEditor移除
- **GIVEN** `HerbEditorControl` 已替代
- **WHEN** 重构完成
- **THEN** SHALL 删除 `HerbListEditor.xaml` 和 `.xaml.cs`

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| desktop-medicalcase | 处方面板使用HerbEditorControl |
| desktop-formula | 方剂编辑复用HerbEditorControl |
| wpf-control-conventions | 控件设计遵循WPF规范 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-31 | 1.0 | 初始版本，定义HerbEditorControl规范 |
