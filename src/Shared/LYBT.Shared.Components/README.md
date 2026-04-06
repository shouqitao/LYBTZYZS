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

## 开发笔记

# LYBT.Shared.Components 代码知识

处方和配方模块共享的药材业务组件库，提供药材验证和编辑的泛型基类与接口。

## 代码文件结构

```
LYBT.Shared.Components/
├── IHerbItem.cs            # 药材项只读接口 (泛型约束)
├── IHerbItemEditable.cs    # 可编辑药材项接口 (UI 编辑支持)
└── HerbValidatorBase.cs    # 药材验证器基类 + ValidationResult 类
```

### IHerbItem.cs
**IHerbItem** (interface) | 药材项目只读接口，作为泛型约束用于 HerbValidatorBase

| 属性 | 类型 | 说明 |
|------|------|------|
| HerbId | Guid | 药材 ID |
| HerbName | string | 药材名称 |
| Unit | string | 单位 |
| Dosage | int | 剂量 (整数克) |
| UnitPrice | decimal | 单价 |

### IHerbItemEditable.cs
**IHerbItemEditable** : IHerbItem | 可编辑药材项接口，扩展药材选择和过滤功能，用于 Desktop UI 控件

| 属性 | 类型 | 说明 |
|------|------|------|
| AllHerbs | ObservableCollection\<HerbListDto\>? | 所有药材列表引用 (父 ViewModel 注入) |
| FilteredHerbs | ObservableCollection\<HerbListDto\> | 过滤后的药材列表 (拼音码/名称) |
| SelectedHerb | HerbListDto? | 选中的药材 (设置后自动填充属性) |

### HerbValidatorBase.cs
**HerbValidatorBase\<TItem\>** (abstract class, where TItem : IHerbItem) | 药材验证器基类

| 方法 | 说明 |
|------|------|
| GetDuplicateHerbs(IEnumerable\<TItem\>) | 检测重复药材，返回重复名称列表 |
| HasDuplicateHerbs(IEnumerable\<TItem\>) | 判断是否存在重复药材 |
| IsValidDosage(decimal, decimal, decimal) | 验证剂量是否在合理范围内 |
| GetDosageWarning(TItem, decimal, decimal) | 获取剂量异常警告文本 |
| ValidateRequiredFields(TItem) | 验证药材项必填字段 (HerbId/HerbName/Dosage/Unit) |
| ValidateHerbListNotEmpty(IEnumerable\<TItem\>, string) | 验证药材列表不为空 |
| ValidateHerbList(IEnumerable\<TItem\>, string) | 组合验证: 非空 + 去重 + 必填 + 剂量警告 |

**ValidationResult** (class) | 验证结果容器

| 方法/属性 | 说明 |
|-----------|------|
| Errors / Warnings | 错误和警告消息列表 |
| IsValid / HasWarnings | 状态判断属性 |
| AddError(string) | 添加错误 |
| AddWarning(string) | 添加警告 |
| Merge(ValidationResult) | 合并另一个验证结果 |
| GetErrorSummary() / GetWarningSummary() | 获取汇总文本 |

## 死代码清理记录

| 类型/方法 | 状态 | 说明 |
|-----------|------|------|
| HerbCalculatorBase | [已清理] 2026-03-01 | 文件已删除，无继承实现类，无外部引用 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| IHerbItem.Dosage | 类型为 int，但 HerbValidatorBase 中以 decimal 处理 | 接口定义为 int Dosage，但验证基类期望 decimal，导致隐式类型转换 | 统一为 decimal 或 int，消除类型不一致 |
| ValidationResult | 与 Shared.Models.Contracts.Common.ValidationResult 同名 | 两个不同的 ValidationResult 类，使用时需注意命名空间区分 | 考虑重命名为 HerbValidationResult 以避免混淆 |
| IHerbItemEditable | 依赖 LYBT.Shared.Models 的 HerbListDto | Components 层反向依赖 Models 层的具体 DTO | 可考虑抽象为接口解耦 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| IHerbItem.Dosage 为 int 但计算逻辑用 decimal | 接口设计与实际使用不一致 | 调用方注意类型转换，修改需同步所有实现类 |
| ValidationResult 命名冲突 | Shared.Components 和 Shared.Models 各有同名类 | 使用时通过完整命名空间引用，避免 using 冲突 |
