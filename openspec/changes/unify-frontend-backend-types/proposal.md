# Proposal: 统一前后端实体类型与命名

## 概述

统一Desktop UI Model与Shared DTO之间的类型定义和命名规范，消除枚举类型转字符串、bool替代枚举等不一致问题，提高代码可维护性和类型安全性。

## 背景

当前存在两类不一致问题：

### 1. DTO层类型不一致

| DTO | 属性 | 当前类型 | 应该类型 | 问题 |
|-----|------|----------|----------|------|
| MedicalCaseDetailDto | PatientGender | `string?` | `Gender` enum | 同一概念不同类型 |
| MedicalCaseListDto | PatientGender | `string?` | `Gender` enum | 同一概念不同类型 |

对比：`PatientDetailDto.Gender`使用`Gender`枚举，但`MedicalCaseDetailDto.PatientGender`使用`string`。

### 2. UI Model与DTO类型不一致

| UI Model | 属性 | 当前类型 | DTO类型 | 问题 |
|----------|------|----------|---------|------|
| PatientItem | Gender | `string` | `Gender` enum | 枚举转字符串 |
| MedicalCaseItem | PatientGender | `string` | `string`(应为enum) | 应统一为枚举 |
| HerbItem | IsActive | `bool` | `CommonStatus` enum | bool替代枚举 |
| FormulaItem | IsActive | `bool` | `CommonStatus` enum | bool替代枚举 |

### 一致的好例子（应遵循的模式）

| UI Model | 属性 | 类型 | 备注 |
|----------|------|------|------|
| UserItem | Role | `UserRole` enum | 保持与DTO一致 |
| UserItem | Status | `CommonStatus` enum | 保持与DTO一致 |
| MedicalCaseItem | Status | `MedicalCaseStatus` enum | 保持与DTO一致 |

### 命名规范

对于聚合DTO中引用其他实体的属性，使用前缀区分是合理的设计：
- `MedicalCaseDetailDto.PatientName` - 在医案上下文中引用患者姓名
- `MedicalCaseDetailDto.DoctorName` - 在医案上下文中引用医生姓名
- `PatientDetailDto.Name` - 在患者上下文中直接使用Name

**关键点**：命名可以有前缀，但**类型必须一致**。`PatientGender`应该是`Gender`枚举，不是string。

## 完整字段比对清单

### PatientItem vs PatientDetailDto

| PatientItem | PatientDetailDto | 类型问题 | 命名问题 |
|-------------|------------------|----------|----------|
| Gender: `string` | Gender: `Gender` | **string vs enum** | - |
| IdCard: `string?` | IdNumber: `string?` | - | **IdCard vs IdNumber** |
| LastVisitDate: `DateTime?` | LastVisitTime: `DateTime?` | - | **LastVisitDate vs LastVisitTime** |
| 其他字段 | 一致 | ✓ | ✓ |

### HerbItem vs HerbDetailDto

| HerbItem | HerbDetailDto | 类型问题 | 命名问题 |
|----------|---------------|----------|----------|
| IsActive: `bool` | Status: `CommonStatus` | **bool vs enum** | **IsActive vs Status** |
| Pinyin: `string?` | PinYinCode: `string?` | - | **Pinyin vs PinYinCode** |
| DosageUnit: `string?` | Unit: `string?` | - | **DosageUnit vs Unit** |
| UnitPrice: `decimal` | Price: `decimal` | - | **UnitPrice vs Price** |
| Specification: `string?` | Spec: `string?` | - | **Specification vs Spec** |
| 其他字段 | 一致 | ✓ | ✓ |

### FormulaItem vs FormulaDetailDto

| FormulaItem | FormulaDetailDto | 类型问题 | 命名问题 |
|-------------|------------------|----------|----------|
| IsActive: `bool` | Status: `CommonStatus` | **bool vs enum** | **IsActive vs Status** |
| Indication: `string?` | Indications: `string?` | - | **单复数不一致** |
| Contraindication: `string?` | Contraindications: `string?` | - | **单复数不一致** |
| Note: `string?` | Remark: `string?` | - | **Note vs Remark** |
| CreatedBy: `string?` | CreatedBy: `Guid?` | **string? vs Guid?** | - |
| Pinyin: `string?` | -(缺失) | - | DTO缺失 |
| Source: `string?` | Source: `string?` | ✓ | ✓ |
| Composition: `string?` | -(缺失) | - | DTO缺失 |
| Modification: `string?` | -(缺失) | - | DTO缺失 |
| IsClassic: `bool` | -(缺失) | - | DTO缺失 |
| IsPersonal: `bool` | IsShared: `bool` | ✓ | **语义相反** |
| UsageCount: `int` | -(缺失) | - | DTO缺失 |

### MedicalCaseItem vs MedicalCaseDetailDto

| MedicalCaseItem | MedicalCaseDetailDto | 类型问题 | 命名问题 |
|-----------------|----------------------|----------|----------|
| PatientGender: `string` | PatientGender: `string?` | 应为Gender enum | DTO也需修复 |
| Status: `MedicalCaseStatus` | CaseStatus: `MedicalCaseStatus` | ✓ | **Status vs CaseStatus** |
| CompletionReason: `string?` | -(缺失) | - | DTO缺失 |
| 其他字段 | 一致 | ✓ | ✓ |

### UserItem vs UserDetailDto (参考标准)

| UserItem | UserDetailDto | 类型问题 | 命名问题 |
|----------|---------------|----------|----------|
| Role: `UserRole` | Role: `UserRole` | ✓ | ✓ |
| Status: `CommonStatus` | Status: `CommonStatus` | ✓ | ✓ |
| CreateTime: `DateTime` | CreatedAt: `DateTime` | - | **CreateTime vs CreatedAt** |
| UpdateTime: `DateTime?` | UpdatedAt: `DateTime?` | - | **UpdateTime vs UpdatedAt** |

## 问题分析

### 1. 类型安全性降低
- `string`类型的Gender可能包含无效值
- `Enum.Parse<Gender>(Gender)`可能抛出异常

### 2. 代码冗余
- `FromDto()`中需要`.ToString()`转换
- `ToDto()`中需要`Enum.Parse<>()`转换
- 每个Item类都有重复的转换逻辑

### 3. 维护成本增加
- 枚举值变更时需要同步修改多处
- 字符串比较不如枚举比较可靠

## 解决方案

### 原则

1. **类型一致性**: UI Model的枚举属性应直接使用枚举类型，与DTO保持一致
2. **命名一致性**: 属性名应与DTO保持一致（如`Gender`而非`GenderText`）
3. **显示转换分离**: UI显示文本通过专门的Display属性或Converter处理
4. **特殊设计例外**: 确有必要的类型转换需添加注释说明原因

### 目标状态

```csharp
// 统一前
public class PatientItem
{
    public string Gender { get; set; }  // 字符串
}

// 统一后
public class PatientItem
{
    public Gender Gender { get; set; }  // 枚举
    public string GenderDisplay => Gender switch { ... };  // 显示用
}
```

## 影响范围

### 需修改的文件

**DTO层（优先）：**
1. **MedicalCaseDetailDto.cs** - PatientGender: string? → Gender enum
2. **MedicalCaseListDto.cs** - PatientGender: string? → Gender enum

**UI Model层：**
1. **PatientItem.cs** - Gender: string → Gender enum
2. **MedicalCaseItem.cs** - PatientGender: string → Gender enum
3. **HerbItem.cs** - IsActive: bool → Status: CommonStatus enum
4. **FormulaItem.cs** - IsActive: bool → Status: CommonStatus enum

**命名统一（可选，影响较大）：**
- UserItem: CreateTime→CreatedAt, UpdateTime→UpdatedAt
- PatientItem: IdCard→IdNumber, LastVisitDate→LastVisitTime
- HerbItem: Pinyin→PinYinCode, DosageUnit→Unit, UnitPrice→Price, Specification→Spec
- FormulaItem: Indication→Indications, Contraindication→Contraindications, Note→Remark
- MedicalCaseItem: Status→CaseStatus

### XAML绑定更新

- 原绑定`{Binding Gender}`可能需要添加Converter
- 或使用新的Display属性`{Binding GenderDisplay}`

## 验收标准

- [ ] 所有UI Model枚举属性使用枚举类型
- [ ] FromDto/ToDto不再需要ToString()/Parse转换
- [ ] 编译0错误0警告
- [ ] 所有测试通过
- [ ] XAML绑定正常工作

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| XAML绑定失效 | 中 | 添加GenderConverter或Display属性 |
| 序列化问题 | 低 | JsonStringEnumConverter已配置 |
| 测试失败 | 低 | 更新测试中的mock数据 |
| 命名变更范围大 | 高 | 分阶段实施，优先类型统一 |

## 优先级

P2 - 技术债务清理，不影响功能

## 实施策略

1. **Phase 0**: DTO层PatientGender类型统一（优先，影响最小）
2. **Phase 1-2**: PatientItem/MedicalCaseItem的Gender类型统一
3. **Phase 3-4**: HerbItem/FormulaItem的Status类型统一
4. **Phase 5**: 验证与测试
5. **(待定)**: 命名统一（影响范围大，需单独评估）

## 相关Issue

- 无直接关联Issue，属于代码质量改进
