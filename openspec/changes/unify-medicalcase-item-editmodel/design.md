# unify-medicalcase-item-editmodel 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计。统一医案模块的 Item 与 EditModel 模型，消除 ConsultationItem/ConsultationEditModel 和 PrescriptionItem/PrescriptionEditModel 之间的重复定义。

## 架构决策

### ADR-1: 统一到 Item 类而非 EditModel

**状态**: 已采纳

**背景**: 目前存在两套数据模型：
- `*Item` 类 (如 ConsultationItem, PrescriptionItem): 功能完整，包含验证、数据提供等
- `*EditModel` 类: 仅包含基础属性和 Reset() 方法，功能重复

**决策**: 将 EditModel 的功能合并到 Item 类，删除 EditModel

**后果**:
- 正面: 消除代码重复，统一数据模型，减少维护成本
- 负面: 需要调整 ViewModel 属性类型

### ADR-2: Reset() 方法仅重置可编辑字段

**状态**: 已采纳

**背景**: 分析发现：
- `Reset()` 不用于取消操作（取消使用克隆恢复 `RestoreFromClone()`）
- `Reset()` 主要用于工具栏"清空处方"命令
- `Clear()` 方法重置包括ID在内的所有字段

**决策**:
- `Reset()` 仅重置用户可编辑字段（不重置ID）
- 保留 `Clear()` 用于完全清空（包括ID）

**后果**:
- 正面: 语义清晰，与现有行为一致
- 负面: 无

### ADR-3: 保持 XAML 绑定路径不变

**状态**: 已采纳

**背景**:
- XAML 绑定使用 `Consultation.PropertyName` 和 `Prescription.PropertyName` 格式
- DependencyProperty 类型为 `typeof(object)`，支持 duck typing
- Item 和 EditModel 的属性名相同

**决策**: ViewModel 属性名保持不变，仅更改类型

**后果**:
- 正面: XAML 无需修改，降低风险
- 负面: 无

## 实现策略

### 策略选择

采用**渐进式替换**策略：按 Phase 逐步替换，每个 Phase 完成后验证编译通过。

### 关键实现点

1. **ConsultationItem 增强**
   - 添加 `Reset()` 方法重置4个诊断字段
   - 方法签名与 ConsultationEditModel.Reset() 保持一致

2. **PrescriptionItem 增强**
   - 添加 `DefaultUsage` 常量
   - 添加 `Reset()` 方法（区别于现有 `Clear()`）
   - `Reset()` 不重置 MedicalCaseId，仅重置用户可编辑字段

3. **ViewModel 属性类型替换**
   - `Consultation` 属性: `ConsultationEditModel` → `ConsultationItem`
   - `Prescription` 属性: `PrescriptionEditModel` → `PrescriptionItem`
   - 初始化逻辑调整为使用 Item 类

## 变更清单

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `src/.../Models/Items/ConsultationItem.cs` | 添加 Reset() 方法 |
| `src/.../Models/Items/PrescriptionItem.cs` | 添加 DefaultUsage 常量和 Reset() 方法 |
| `src/.../ViewModels/MedicalCaseMasterDetailViewModel.cs` | 属性类型从 EditModel 改为 Item |

### 删除文件

| 文件路径 | 原因 |
|----------|------|
| `src/.../Models/Edit/ConsultationEditModel.cs` | 功能已合并到 ConsultationItem |
| `src/.../Models/Edit/PrescriptionEditModel.cs` | 功能已合并到 PrescriptionItem |
| `src/.../Models/Edit/` 目录 | 目录清空后删除 |

### 保持不变

| 文件类型 | 原因 |
|----------|------|
| `*.xaml` 绑定文件 | 属性名一致，duck typing 支持类型变更 |
| Mapper 文件 | 无直接依赖 EditModel |

## 依赖关系

### 变更顺序

```
Phase 1 (Consultation) ──────────┐
                                 │
Phase 2 (Prescription) ──────────┼──> Phase 3 (Cleanup)
                                 │
                                 │
```

Phase 1 和 Phase 2 可并行，但建议顺序执行以便验证。

## 测试策略

### 验证方式

1. **编译验证**: `dotnet build LYBT.Desktop.sln -c Release --no-restore`
2. **运行时验证**: 启动应用检查 System.Windows.Data 绑定错误
3. **功能验证**:
   - 医案编辑界面诊断录入
   - 医案编辑界面处方编辑
   - 清空处方功能

## 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 绑定路径不匹配 | 低 | 高 | 属性名一致，duck typing 支持 |
| Reset() 行为差异 | 低 | 中 | 参照 EditModel 实现 |
| 遗漏引用导致编译失败 | 中 | 低 | 每 Phase 编译验证 |

## 回滚计划

如果变更失败:
1. 恢复已删除的 EditModel 文件 (git checkout)
2. 回退 ViewModel 属性类型
3. 移除 Item 类新增方法

---

**设计者**: Claude Code
**设计日期**: 2026-01-17
**执行日期**: 2026-01-17
**状态**: 已执行 (代码变更完成，待运行时验证)
