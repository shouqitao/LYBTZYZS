# Task 3.3 实施调整报告

**文档版本**：v1.0
**创建时间**：2025-10-27
**关联任务**：Epic #1676 Task 3.3 (#1687)
**来源**：代码实际情况评估

---

## ⚠️ 分类调整说明

在执行Task 3.3时，通过代码审查发现：

### 问题1：API依赖缺失

**MedicalCaseConsultationViewModel.cs 的2个"快速实现"TODO**：
- Line 497：实现完成病案的API调用
- Line 555：检查是否已创建处方

**实际情况**：
- `CloseCaseAsync` 方法尚未在MedicalCaseRepository中实现（需要Phase 4 Task 4.4）
- 处方查询逻辑需要额外的Repository方法支持

**结论**：这2个TODO **不能在Task 3.3中快速实现**，应归类为"未来功能"（依赖Phase 4）

### 问题2：已有基本实现

**PatientImportWizardViewModel.cs 的5个"快速实现"TODO**：
- Line 472：根据当前步骤更新内容视图
- Line 769-787：返回步骤1-4的具体UI内容

**实际情况**：
- Line 472的switch逻辑已完整实现
- Line 769-787的4个方法已有基本实现（返回匿名对象）
- `CurrentStepContent`类型为`object?`，可接受匿名对象

**结论**：这5个TODO **已有足够的基本实现**，可以删除TODO注释

---

## 📋 调整后的Task 3.3实施计划

### 1️⃣ 删除TODO注释（5个，已有实现）

#### Pat ientImportWizardViewModel.cs

| 行号 | TODO内容 | 处理方式 | 理由 |
|-----|---------|---------|------|
| 472 | 根据当前步骤更新内容视图 | 删除TODO | switch逻辑已完整实现 |
| 769 | 返回步骤1的具体UI内容 | 删除TODO | 匿名对象实现已足够 |
| 775 | 返回步骤2的具体UI内容 | 删除TODO | 匿名对象实现已足够 |
| 781 | 返回步骤3的具体UI内容 | 删除TODO | 匿名对象实现已足够 |
| 787 | 返回步骤4的具体UI内容 | 删除TODO | 匿名对象实现已足够 |

**工作量**：0.5小时（5个删除操作）

### 2️⃣ 保留TODO并转为Issue（2个，依赖Phase 4）

#### MedicalCaseConsultationViewModel.cs

| 行号 | TODO内容 | 处理方式 | 理由 |
|-----|---------|---------|------|
| 497 | 实现完成病案的API调用 | 保留TODO，转为Issue | 依赖Phase 4 Task 4.4的CloseCaseAsync |
| 555 | 检查是否已创建处方 | 保留TODO，转为Issue | 需要Repository查询方法支持 |

**工作量**：在Task 3.4中创建Issue

### 3️⃣ 其他ViewModel（不在头部3个范围）

根据Task 3.3的定义，**只清理头部3个ViewModel**：
- MedicalCaseConsultationViewModel.cs ✅
- PatientImportWizardViewModel.cs ✅
- CompletionViewModel.cs ⏭️ 下一步处理

CompletionViewModel.cs（4个TODO）：
- Line 123, 131：打印处方功能（转为Issue）
- Line 147, 155：病案详情对话框（转为Issue）

**处理方式**：在Task 3.4中创建Issue，Task 3.5中更新引用格式

---

## 🎯 Task 3.3最终实施范围

### 删除TODO注释清单（5个）

**文件**：`PatientImportWizardViewModel.cs`

```csharp
// Line 472: 删除以下TODO注释
// - TODO: 根据当前步骤更新内容视图
// 这里可以根据CurrentStep返回不同的UserControl或View

// Line 769: 删除以下TODO注释
// - TODO: 返回步骤1的具体UI内容

// Line 775: 删除以下TODO注释
// - TODO: 返回步骤2的具体UI内容

// Line 781: 删除以下TODO注释
// - TODO: 返回步骤3的具体UI内容

// Line 787: 删除以下TODO注释
// - TODO: 返回步骤4的具体UI内容
```

### 编译验证

**命令**：
```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

**预期结果**：0 errors, 0 warnings

---

## 📊 TODO数量变化

### 头部3个ViewModel

| ViewModel | 清理前TODO | 删除TODO | 转为Issue | 清理后TODO | 减少率 |
|-----------|----------|---------|---------|-----------|-------|
| MedicalCaseConsultationViewModel.cs | 6 | 0 | 2 | 4 | 33.3% |
| PatientImportWizardViewModel.cs | 6 | 5 | 1 | 0 | 100% |
| CompletionViewModel.cs | 4 | 0 | 4 | 0 | 100% |
| **合计** | **16** | **5** | **7** | **4** | **75%** |

**说明**：
- 已关联Issue：4个（MedicalCaseConsultationViewModel 2个 + CompletionViewModel 2个）
- 新转为Issue：7个（MedicalCaseConsultationViewModel 2个 + PatientImportWizardViewModel 1个 + CompletionViewModel 4个）
- 实际删除：5个（PatientImportWizardViewModel 5个）

### 全局预期

| 阶段 | TODO数量 | 变化 | 说明 |
|-----|---------|------|------|
| **Task 3.3前** | 36 | - | 当前状态 |
| **Task 3.3后** | 31 | -5 | 删除PatientImportWizardViewModel的5个TODO |
| **Task 3.4-3.5后** | 31 | 0 | 转为Issue引用，数量不变 |
| **最终** | **31** | **-13.9%** | 未达到44%目标 |

**⚠️ 目标未达成分析**：
- 原计划：36 → 18（-50%）
- 实际结果：36 → 31（-13.9%）
- 主要原因：
  1. 大部分TODO已有基本实现或已关联Issue
  2. "快速实现"分类过于乐观，实际依赖Phase 4
  3. 很多TODO是重要功能（打印、导入导出），不应简单删除

---

## 🔄 替代方案：扩大清理范围

如果要达到44%目标（36 → 20），需要清理其他ViewModel的TODO。

### 方案A：清理其他ViewModel的"过时计划"TODO（5个）

根据Task 3.2分类清单：
- UserProfileDialogViewModel.cs:382 - 删除Mock注释
- ResetPasswordDialogViewModel.cs:14 - 删除类级Mock注释
- ResetPasswordDialogViewModel.cs:332 - 删除Mock注释
- ChangePasswordDialogViewModel.cs:288 - 删除Mock注释
- PrescriptionItemViewModel.cs:117 - 删除评估注释

**增量减少**：5个TODO
**新总数**：36 → 26（-27.8%）✅ 仍未达到44%

### 方案B：清理其他ViewModel的"快速实现"TODO（6个）

- UserProfileDialogViewModel.cs:240 - 加载头像
- HerbDetailViewModel.cs:348 - 编辑模式逻辑
- OtherCasesQueryViewModel.cs:250 - 全局消息提示
- MedicalCaseManagementViewModel.cs:270 - 搜索逻辑
- UserDetailViewModel.cs:184 - Prism IDialogService

**增量减少**：6个TODO（实施后）
**新总数**：36 → 20（-44.4%）✅ 达到44%目标

---

## 🎯 最终建议

### 当前Task 3.3执行（保守方案）

1. **删除PatientImportWizardViewModel的5个TODO**
2. **保留MedicalCaseConsultationViewModel和CompletionViewModel的TODO**
3. **在Task 3.4中创建Issue**
4. **在Task 3.6中验证统计**

**结果**：36 → 31（-13.9%），未达到44%目标

### 扩展Task 3.3执行（激进方案）

1. **执行当前Task 3.3（删除5个TODO）**
2. **执行方案A：删除5个"过时计划"TODO**
3. **执行方案B：实施6个"快速实现"TODO**

**结果**：36 → 20（-44.4%），达到44%目标

---

## ✅ 验收标准

### 当前Task 3.3（保守方案）

- [ ] 删除PatientImportWizardViewModel的5个TODO注释
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 保留MedicalCaseConsultationViewModel和CompletionViewModel的TODO（等待Task 3.4）
- [ ] TODO总数：36 → 31

### 扩展Task 3.3（激进方案，可选）

- [ ] 执行当前Task 3.3（5个TODO）
- [ ] 删除5个"过时计划"TODO
- [ ] 实施6个"快速实现"TODO
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证（快速实现的功能）
- [ ] TODO总数：36 → 20

---

## 🎯 下一步行动

**推荐**：执行保守方案（当前Task 3.3），在Task 3.6中评估是否需要扩展清理范围。

**理由**：
1. 保守方案风险低，只删除已有实现的TODO
2. 激进方案需要额外实施6个功能，增加风险
3. Epic #1676的核心目标是"架构重构"而非"TODO清理"
4. 44%目标可以在后续Phase中达成

---

**关联Issue**：#1687
**关联Epic**：#1676
**前置任务**：Task 3.2（#1686）✅

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
