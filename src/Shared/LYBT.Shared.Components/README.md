# LYBT.Shared.Components

> 跨端共享组件库 | 中药计算/验证逻辑 | 泛型设计

## 项目定位

- **层级**: Shared层
- **职责**: 提供Server/Client共享的业务组件，实现跨端代码复用

## 目录结构

```
LYBT.Shared.Components/
├── IHerbItem.cs              # 中药项接口
├── HerbCalculatorBase.cs     # 中药计算器基类
└── HerbValidatorBase.cs      # 中药验证器基类
```

## 核心组件

| 组件 | 方法数 | 说明 |
|------|--------|------|
| IHerbItem | 6属性 | 中药项接口(HerbId/HerbName/Dosage/Unit/Quantity/UnitPrice) |
| HerbCalculatorBase<T> | 8 | 计算总剂量/总重量/总价/药材比例/标准差/单位转换 |
| HerbValidatorBase<T> | 7 | 重复检查/剂量验证/必填验证/药材列表验证 |

## 设计特点

| 特点 | 说明 |
|------|------|
| 泛型约束 | `where T : IHerbItem` 支持任何实现接口的类型 |
| 跨端复用 | Server端用DTO计算，Client端用ViewModel验证 |
| 零外部依赖 | 纯.NET 8标准库 |

## 跨端复用场景

| 端 | 使用类型 | 场景 |
|----|----------|------|
| Server | HerbCalculatorBase<PrescriptionItemDto> | 处方总价计算 |
| Client | HerbValidatorBase<PrescriptionItemViewModel> | 实时输入验证 |

## 依赖关系

### 依赖
- LYBT.Shared.Models (ValidationResult类型)

### 被依赖
- LYBT.Module.Prescriptions (Server端处方计算)
- LYBT.Desktop.Prescriptions (Desktop端处方验证)

### NuGet包
- 无外部包依赖

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
