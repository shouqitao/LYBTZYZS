# LYBT.Shared.Components

> 跨端共享组件库 | 中药验证逻辑 | 泛型设计

## 项目定位

- **层级**: Shared层
- **职责**: 提供Server/Client共享的业务组件，实现跨端代码复用

## 目录结构

```
LYBT.Shared.Components/
├── IHerbItem.cs              # 中药项只读接口
├── IHerbItemEditable.cs      # 可编辑中药项接口 (UI编辑支持)
└── HerbValidatorBase.cs      # 中药验证器基类
```

## 核心组件

| 组件 | 方法数 | 说明 |
|------|--------|------|
| IHerbItem | 5属性 | 中药项只读接口 (HerbId/HerbName/Dosage/Unit/UnitPrice) |
| IHerbItemEditable | 3属性 | 可编辑中药项接口，扩展药材选择和过滤 (AllHerbs/FilteredHerbs/SelectedHerb) |
| HerbValidatorBase\<T\> | 7 | 重复检查/剂量验证/必填验证/药材列表验证 |

## 设计特点

| 特点 | 说明 |
|------|------|
| 泛型约束 | `where T : IHerbItem` 支持任何实现接口的类型 |
| 跨端复用 | Server端用DTO验证，Client端用ViewModel验证 |
| 编辑支持 | IHerbItemEditable扩展拼音码过滤和药材选择能力 |

## 跨端复用场景

| 端 | 使用类型 | 场景 |
|----|----------|------|
| Client | HerbValidatorBase<PrescriptionItemViewModel> | 实时输入验证 |
| Client | IHerbItemEditable | 药材编辑控件数据绑定 |

## 设计依据

- 中药验证逻辑从 Prescription 和 Formula 模块提取(Issue #1153)，消除两端重复实现
- 采用泛型约束 `where T : IHerbItem`，Server 端用 DTO 类型、Desktop 端用 ViewModel 类型，共享同一套算法
- 独立为单独项目而非放入 Shared.Utilities，因为药材验证是领域逻辑而非通用工具
- IHerbItemEditable 依赖 LYBT.Shared.Models 的 HerbListDto，为 Desktop UI 控件提供药材选择能力

## 依赖关系

### 依赖
- LYBT.Shared.Models (ValidationResult类型, HerbListDto)

### 被依赖
- LYBT.Desktop.MedicalCase (处方编辑验证)
- LYBT.Desktop.Formula (验方编辑验证)

### NuGet包
- 无外部包依赖

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 死代码清理: 移除 HerbCalculatorBase, 补充 IHerbItemEditable |
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
