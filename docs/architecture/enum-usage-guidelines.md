# 枚举使用规范

> 创建日期: 2025-11-29
> 状态: 规范文档
> 关联文档: [Status vs IsDeleted 概念区分](./status-vs-isdeleted.md)

## 概述

本文档定义项目中枚举的分类、使用场景和命名规范，确保枚举的一致性使用。

## 枚举分类

### 1. 状态类枚举（State Enums）

用于表示实体的业务状态或可用性状态。

| 枚举 | 位置 | 用途 | 适用实体 |
|------|------|------|----------|
| `CommonStatus` | SystemEnums.cs | 启用/禁用控制 | User, Patient, Herb, Formula, Consultation, AuthSession |
| `MedicalCaseStatus` | MedicalCaseEnums.cs | 医案生命周期 | MedicalCase |
| `CaseStatus` | CaseStatus.cs | MedicalCaseStatus别名 | （不直接使用） |
| `PrescriptionStatus` | PrescriptionStatus.cs | 处方状态 | Prescription |
| `DataStatus` | SystemEnums.cs | 数据状态（草稿/正常/锁定/归档） | 通用 |
| `AuditStatus` | SystemEnums.cs | 审核状态 | 需要审核的实体 |
| `PaymentStatus` | SystemEnums.cs | 支付状态 | Payment相关 |

### 2. 分类类枚举（Category Enums）

用于实体的固有属性分类。

| 枚举 | 位置 | 用途 |
|------|------|------|
| `Gender` | Gender.cs | 性别 |
| `UserRole` | AuthEnums.cs | 用户角色 |
| `FormulaValidationStatus` | FormulaValidationStatus.cs | 验方验证状态 |
| `CompatibilityType` | SystemEnums.cs | 药材配伍类型 |
| `CompatibilitySeverity` | SystemEnums.cs | 配伍严重程度 |

### 3. 操作类枚举（Operation Enums）

用于表示操作类型或结果。

| 枚举 | 位置 | 用途 |
|------|------|------|
| `OperationResult` | SystemEnums.cs | API操作结果 |
| `AuditOperationType` | MedicalCaseEnums.cs | 审计日志操作类型 |
| `DuplicateStrategy` | DuplicateStrategy.cs | 重复处理策略 |

### 4. 时间类枚举（Time Enums）

用于时间相关的选项。

| 枚举 | 位置 | 用途 |
|------|------|------|
| `WorkDay` | SystemEnums.cs | 工作日 |
| `TimeSlot` | SystemEnums.cs | 时间段（上午/下午/晚上） |

## 关键设计决策

### CommonStatus vs MedicalCaseStatus

```
CommonStatus（启用/禁用）        MedicalCaseStatus（生命周期）
├── Disabled (0)                ├── Draft (0)      暂存
└── Enabled (1)                 ├── Active (1)     进行中
                                ├── Completed (2)  已完成
                                └── Cancelled (3)  已取消
```

**区别**：
- `CommonStatus`：简单的二元状态，表示"可用"或"不可用"
- `MedicalCaseStatus`：业务流程状态机，表示医案所处的生命周期阶段

**MedicalCase 不使用 CommonStatus 的原因**：
1. 医案有明确的生命周期流程（Draft → Active → Completed）
2. "禁用"概念对医案不适用（医案不能被"禁用"，只能"取消"或"软删除"）
3. 取消使用 `Cancelled` 状态，删除使用 `IsDeleted` 软删除

### CaseStatus 的定位

`CaseStatus` 是 `MedicalCaseStatus` 的别名枚举，**不建议直接使用**。
- 历史原因保留，新代码应直接使用 `MedicalCaseStatus`
- 缺少 `Cancelled` 状态

## 枚举命名规范

### 文件命名
- 单一枚举：`{EnumName}.cs`（如 `Gender.cs`）
- 相关枚举组：`{Domain}Enums.cs`（如 `SystemEnums.cs`, `MedicalCaseEnums.cs`）

### 枚举命名
- 使用 PascalCase
- 名称应清晰表达用途
- 状态枚举以 `Status` 结尾
- 类型枚举以 `Type` 结尾

### 枚举值命名
- 使用 PascalCase
- 应能清晰表达含义
- 避免使用缩写

## 枚举配置规范

### 必须配置
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]  // API序列化为字符串
public enum MyStatus
{
    [Description("描述文本")]  // 用于UI显示
    Value = 0
}
```

### 数值规范
- 从0或1开始，保持连续
- 禁用/默认状态使用0
- 启用/正常状态使用1
- 数值一旦定义，不可更改（影响数据库存储）

## 数据库存储

枚举在数据库中存储为 `int` 类型：

```csharp
// Entity配置
builder.Property(e => e.Status)
    .HasConversion<int>()
    .HasDefaultValue(CommonStatus.Enabled);
```

## 使用示例

### 正确用法

```csharp
// 用户禁用 - 使用 CommonStatus
user.Status = CommonStatus.Disabled;

// 医案状态变更 - 使用 MedicalCaseStatus
medicalCase.CaseStatus = MedicalCaseStatus.Completed;

// 医案删除 - 使用 IsDeleted（不是状态枚举）
medicalCase.IsDeleted = true;
```

### 错误用法

```csharp
// 错误：医案不应使用 CommonStatus
// medicalCase.Status = CommonStatus.Disabled;  // MedicalCase已移除此字段

// 错误：不应使用 CaseStatus（使用 MedicalCaseStatus）
// medicalCase.CaseStatus = CaseStatus.Draft;
```

## 新增枚举检查清单

添加新枚举时确认：

- [ ] 是否有现有枚举可复用？
- [ ] 文件位置是否正确（单独文件 vs 组合文件）？
- [ ] 是否添加了 `[JsonConverter]` 和 `[Description]` 特性？
- [ ] 数值是否从0开始且连续？
- [ ] 是否需要在 Entity 配置中设置默认值？
- [ ] 是否更新了本文档？

## 参考资料

- [Status vs IsDeleted 概念区分](./status-vs-isdeleted.md)
- [.NET Enum Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/enum)
