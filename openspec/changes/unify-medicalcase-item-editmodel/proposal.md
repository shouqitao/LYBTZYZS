# unify-medicalcase-item-editmodel

## Why

### 发现的问题

医案模块存在Item类与EditModel类的功能重复定义，增加了维护成本和潜在的一致性风险。

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| ConsultationItem (248行) | 字段重复 | 4个诊断字段+验证逻辑 | 统一为单一类 |
| ConsultationEditModel (28行) | 功能重复 | 相同4个字段+IsValid+Reset | 合并到Item |
| PrescriptionItem (421行) | 字段重复 | 5+核心字段+4计算属性 | 统一为单一类 |
| PrescriptionEditModel (60行) | 功能重复 | 相同字段+NotifyItemsChanged | 合并到Item |

### 重复内容详情

**Consultation类重复:**
- 字段: `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TcmDiagnosis`
- 验证: 两者都检查`TcmDiagnosis`非空
- EditModel的`Reset()`方法Item类缺失

**Prescription类重复:**
- 字段: `DosageCount`, `Usage`, `Advice`, `Remark`, `SingleDosePrice`, `Items`
- 计算属性: `ItemCount`, `HasItems`, `IsValid`, `TotalPrice`
- 方法: `NotifyItemsChanged()`在两处实现
- 默认值: "水煎服，一日一剂，分早晚两次温服" 重复定义

### 影响分析

- **代码重复**: ~180行可消除
- **维护风险**: 字段同步修改时需改两处
- **基类不一致**: Item用BindableBase，EditModel用ObservableObject

## What Changes

### 设计决策: 统一为单一Item类

**理由:**
1. EditModel是Item的严格子集，无独有功能
2. Item已有完整的属性定义和IValidatable实现
3. 只需为Item添加`Reset()`方法即可替代EditModel
4. 保持BindableBase基类（Prism标准）

### Phase 1: 增强ConsultationItem

1. 为`ConsultationItem`添加`Reset()`方法
2. 更新`MedicalCaseMasterDetailViewModel`，用ConsultationItem替代ConsultationEditModel
3. 删除`ConsultationEditModel.cs`
4. 更新相关XAML绑定路径

### Phase 2: 增强PrescriptionItem

1. 为`PrescriptionItem`添加`Reset()`方法（已有`Clear()`，统一命名）
2. 确保默认值常量化（提取DefaultUsage常量）
3. 更新`MedicalCaseMasterDetailViewModel`，用PrescriptionItem替代PrescriptionEditModel
4. 删除`PrescriptionEditModel.cs`
5. 更新相关XAML绑定路径

### Phase 3: 清理与验证

1. 删除`Models/Edit/`目录（如果为空）
2. 更新Mapper引用（如有）
3. 编译验证
4. 运行时验证绑定无错误

## Architecture

### 变更前

```
Models/
├── Items/
│   ├── ConsultationItem.cs      # 248行，BindableBase
│   └── PrescriptionItem.cs      # 421行，BindableBase
├── Edit/
│   ├── ConsultationEditModel.cs # 28行，ObservableObject (重复)
│   └── PrescriptionEditModel.cs # 60行，ObservableObject (重复)
└── MedicalCaseDetailModel.cs
```

### 变更后

```
Models/
├── Items/
│   ├── ConsultationItem.cs      # ~260行，添加Reset()
│   └── PrescriptionItem.cs      # ~430行，添加Reset()，常量提取
└── MedicalCaseDetailModel.cs
```

### ViewModel变更

```csharp
// 变更前
public class MedicalCaseMasterDetailViewModel
{
    public ConsultationEditModel ConsultationEdit { get; }  // 删除
    public PrescriptionEditModel PrescriptionEdit { get; }  // 删除
}

// 变更后
public class MedicalCaseMasterDetailViewModel
{
    public ConsultationItem Consultation { get; }  // 直接使用Item
    public PrescriptionItem Prescription { get; }  // 直接使用Item
}
```

## Impact

- **文件变更**: 6-8个文件
- **代码减少**: ~88行（删除EditModel）+ XAML简化
- **风险等级**: Medium - 涉及XAML绑定路径变更
- **测试要求**: 验证编辑界面数据绑定正常

## Risks

| 风险 | 缓解措施 |
|------|----------|
| XAML绑定路径变更导致运行时错误 | 编译后启动应用检查绑定错误日志 |
| Item类职责过重 | Item本身已承担展示+验证职责，增加Reset()合理 |
| Mapper依赖EditModel | 检查并更新Mapper引用 |

## Success Criteria

1. `ConsultationEditModel.cs`和`PrescriptionEditModel.cs`已删除
2. Desktop解决方案编译通过
3. 医案编辑界面功能正常（诊断录入、处方编辑）
4. 无System.Windows.Data绑定错误

## References

- 代码分析报告: 2026-01-17会话
- 相关记忆: `slim-workspace-viewmodel-decision-2026-01-12`
- 历史提案: `consolidate-panel-viewmodels` (~95%完成)
