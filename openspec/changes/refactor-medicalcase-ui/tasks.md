# Tasks: refactor-medicalcase-ui

## Task Overview

| Task | 描述 | 工作量 | 状态 |
|------|------|--------|------|
| 1.1 | 创建共享样式资源字典 | 30min | Done |
| 1.2 | 定义颜色、按钮、输入框样式 | 20min | Done |
| 2.1 | 重构 ConsultationPanel 布局 | 45min | Done |
| 2.2 | 统一诊断面板字段样式 | 20min | Done |
| 3.1 | 重构 PrescriptionEditorPanel 布局 | 45min | Done |
| 3.2 | 移除重复字段，优化药材卡片区 | 30min | Done |
| 4.1 | 重构 MedicalCaseWorkspaceView 底部操作栏 | 30min | Done |
| 4.2 | 简化状态指示器 | 15min | Done |
| 5.1 | 清理废弃的 View 文件 | 15min | Done |
| 5.2 | 清理废弃的 ViewModel 文件 | 15min | Done |
| 6.1 | 1920x1080 分辨率测试 | 20min | Skipped |
| 6.2 | 1366x768 分辨率兼容测试 | 20min | Skipped |

**总工作量**: ~5h
**实际完成**: ~4h (测试需用户手动验证)

---

## Phase 1: 设计系统基础

### Task 1.1: 创建共享样式资源字典
**Priority**: P0
**Effort**: 30min
**Dependencies**: None
**Status**: Done

- [x] 创建 `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Theming/MedicalCaseStyles.xaml`
- [x] 在 `MedicalCaseWorkspaceView` 中引用样式资源
- [x] 确保样式资源可被 Controls 目录下的 UserControl 引用

**验收标准**:
- 样式资源文件创建成功
- 编译无错误

### Task 1.2: 定义颜色、按钮、输入框样式
**Priority**: P0
**Effort**: 20min
**Dependencies**: Task 1.1
**Status**: Done

定义以下样式:
- [x] 颜色常量 (Primary, Success, Warning, Danger, 中性色)
- [x] `PrimaryButtonStyle` - 主要操作按钮(绿色)
- [x] `SecondaryButtonStyle` - 次要操作按钮(蓝色)
- [x] `OutlineButtonStyle` - 轮廓按钮(导入等)
- [x] `WarningButtonStyle` - 警告操作按钮(橙色)
- [x] `DangerButtonStyle` - 危险操作按钮(红色)
- [x] `FormTextBoxStyle` - 统一输入框样式
- [x] `SectionHeaderStyle` - 区块标题样式
- [x] `FieldLabelStyle` - 字段标签样式

**验收标准**:
- 所有样式可正常应用
- 样式视觉效果符合设计规范

---

## Phase 2: 诊断面板重构

### Task 2.1: 重构 ConsultationPanel 布局
**Priority**: P0
**Effort**: 45min
**Dependencies**: Phase 1
**Status**: Done

- [x] 移除多余的 Border 边框，使用留白分隔
- [x] 调整 Grid.RowDefinitions 布局
- [x] 操作按钮区固定在面板底部
- [x] 应用新的样式资源

**验收标准**:
- 布局整洁，无多余边框
- 按钮位置合理（面板底部）
- 现有数据绑定正常工作

### Task 2.2: 统一诊断面板字段样式
**Priority**: P1
**Effort**: 20min
**Dependencies**: Task 2.1
**Status**: Done

- [x] 统一所有 TextBox 高度和间距
- [x] 统一标签样式
- [x] 必填字段标记样式统一
- [x] Expander 折叠区域样式优化

**验收标准**:
- 所有字段视觉一致
- 必填标记清晰可见

---

## Phase 3: 处方面板重构

### Task 3.1: 重构 PrescriptionEditorPanel 布局
**Priority**: P0
**Effort**: 45min
**Dependencies**: Phase 1
**Status**: Done

- [x] 保留顶部快速导入按钮区
- [x] 移除中间的"添加行/保存草稿/删除处方"按钮行
- [x] 药材卡片区域增加视觉间距
- [x] 底部价格信息区使用卡片样式

**验收标准**:
- 布局符合设计图
- 药材卡片正常显示
- 价格计算正常

### Task 3.2: 移除重复字段，优化药材卡片区
**Priority**: P1
**Effort**: 30min
**Dependencies**: Task 3.1
**Status**: Done

- [x] 移除"治法方案/治疗原则"字段（诊断区已有）
- [x] 调整药材卡片统计信息位置
- [x] 优化空状态提示

**验收标准**:
- 无重复字段
- 空状态有友好提示

---

## Phase 4: 底部操作栏重构

### Task 4.1: 重构 MedicalCaseWorkspaceView 底部操作栏
**Priority**: P0
**Effort**: 30min
**Dependencies**: Phase 1
**Status**: Done

- [x] 调整按钮排列顺序：暂停(左) | 状态(中) | 打印+完成(右)
- [x] 应用新的按钮样式
- [x] 调整高度为 64px

**验收标准**:
- 按钮布局合理
- 样式统一

### Task 4.2: 简化状态指示器
**Priority**: P1
**Effort**: 15min
**Dependencies**: Task 4.1
**Status**: Done

- [x] 将冗长状态文本改为 "●已诊断 ●待开方" 格式
- [x] 使用颜色区分状态：绿色(完成)、灰色(待处理)、黄色(进行中)

**验收标准**:
- 状态一目了然
- 颜色语义正确

---

## Phase 5: 技术债务清理

### Task 5.1: 清理废弃的 View 文件
**Priority**: P2
**Effort**: 15min
**Dependencies**: Phase 4 完成后
**Status**: Done

已删除文件:
- [x] `Views/MedicalCaseFlowView.xaml` + `.xaml.cs`
- [x] `Views/MedicalCaseEditorView.xaml` + `.xaml.cs`
- [x] `Views/CompletionView.xaml` + `.xaml.cs`

**验收标准**:
- 文件已删除
- 编译无错误
- Module 注册已更新

### Task 5.2: 清理废弃的 ViewModel 文件
**Priority**: P2
**Effort**: 15min
**Dependencies**: Task 5.1
**Status**: Done

已删除文件:
- [x] `ViewModels/MedicalCaseFlowViewModel.cs`
- [x] `ViewModels/MedicalCaseFormViewModel.cs`
- [x] `ViewModels/CompletionViewModel.cs`

**验收标准**:
- 未使用的 ViewModel 已删除
- 编译无错误

---

## Phase 6: 测试验证

### Task 6.1: 1920x1080 分辨率测试
**Priority**: P1
**Effort**: 20min
**Dependencies**: Phase 4
**Status**: Skipped (需用户手动验证)

测试项:
- [ ] 整体布局比例正确 (40:60)
- [ ] 文字清晰可读
- [ ] 按钮可点击区域足够
- [ ] 滚动行为正常
- [ ] 无截断或溢出

**验收标准**:
- 所有测试项通过

### Task 6.2: 1366x768 分辨率兼容测试
**Priority**: P1
**Effort**: 20min
**Dependencies**: Task 6.1
**Status**: Skipped (需用户手动验证)

测试项:
- [ ] 布局自适应正常
- [ ] 关键内容不被截断
- [ ] 滚动功能正常
- [ ] 操作按钮可访问

**验收标准**:
- 最小分辨率下可正常使用

---

## Summary

| Phase | Tasks | Total Effort | Status |
|-------|-------|--------------|--------|
| Phase 1 | 2 | 50min | Done |
| Phase 2 | 2 | 65min | Done |
| Phase 3 | 2 | 75min | Done |
| Phase 4 | 2 | 45min | Done |
| Phase 5 | 2 | 30min | Done |
| Phase 6 | 2 | 40min | Skipped |
| **Total** | **12** | **~5h** | **10/12 Done** |

## Dependency Graph

```
Phase 1 (设计系统基础) ✓
    ├── Task 1.1 (样式资源字典) ✓
    └── Task 1.2 (颜色/按钮/输入框样式) ✓ → 依赖 1.1

Phase 1 完成 →
    ├── Phase 2 (诊断面板重构) ✓
    │   ├── Task 2.1 (布局重构) ✓
    │   └── Task 2.2 (样式统一) ✓ → 依赖 2.1
    │
    ├── Phase 3 (处方面板重构) ✓
    │   ├── Task 3.1 (布局重构) ✓
    │   └── Task 3.2 (移除重复) ✓ → 依赖 3.1
    │
    └── Phase 4 (底部操作栏) ✓
        ├── Task 4.1 (按钮布局) ✓
        └── Task 4.2 (状态指示器) ✓ → 依赖 4.1

Phase 2-4 完成 →
    Phase 5 (技术债务清理) ✓
    └── Phase 6 (测试验证) ⏸️ 需用户手动验证
```

## Implementation Notes

### 创建的文件
- `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Theming/MedicalCaseStyles.xaml` - 共享样式资源字典

### 修改的文件
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWorkspaceView.xaml` - 主工作区视图
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/ConsultationPanel.xaml` - 诊断面板
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml` - 处方面板
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs` - 模块注册

### 删除的文件
- `Views/MedicalCaseFlowView.xaml` + `.xaml.cs`
- `Views/MedicalCaseEditorView.xaml` + `.xaml.cs`
- `Views/CompletionView.xaml` + `.xaml.cs`
- `ViewModels/MedicalCaseFlowViewModel.cs`
- `ViewModels/MedicalCaseFormViewModel.cs`
- `ViewModels/CompletionViewModel.cs`

## Notes

- **不引入第三方控件**: 所有样式使用 WPF 原生控件实现
- **保持 MVVM**: 仅修改 XAML，不改变 ViewModel 绑定
- **渐进式重构**: 按 Phase 顺序执行，每个 Phase 完成后可独立验证
- **主目标分辨率**: 1920x1080，兼容 1366x768
