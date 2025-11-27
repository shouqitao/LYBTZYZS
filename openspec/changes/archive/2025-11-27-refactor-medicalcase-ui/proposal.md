# Proposal: refactor-medicalcase-ui

## Summary

重构医案看诊界面(MedicalCaseWorkspaceView)的UI布局，解决当前界面布局凌乱、按钮放置不合理的问题，使界面更加简洁大气，符合现代医疗软件UI设计规范。

## Problem Statement

根据用户反馈和截图分析，当前医案界面存在以下问题：

### 布局问题
1. **左侧诊断区**：
   - 按钮位置不统一（保存草稿、确认诊断在右下角）
   - 表单字段间距不一致
   - 分组边框样式过重，视觉噪音大

2. **右侧处方区**：
   - 操作按钮（添加行、保存草稿、删除处方）放在药材卡片上方，与工作流不符
   - 快速导入按钮与主操作按钮混杂
   - 治法方案/治疗原则字段重复（诊断区已有）
   - 价格信息区域布局拥挤

3. **底部操作栏**：
   - 暂停看诊按钮位置不够醒目
   - 状态信息区域复杂，难以快速理解

4. **整体问题**：
   - 颜色使用不统一（多种蓝色、绿色、橙色）
   - 按钮样式不一致
   - 缺乏视觉层次感

### 历史技术债务
- `MedicalCaseFlowView`、`MedicalCaseEditorView` 等多个遗留视图文件需要清理
- 部分ViewModel存在冗余代码

## Proposed Solution

### 设计原则（参考医疗软件UI最佳实践）

1. **简洁清晰** - 每个区域服务单一目的，减少视觉噪音
2. **直观导航** - 使用熟悉的布局模式，操作按钮放在用户期望的位置
3. **一致性** - 统一颜色、字体、间距、按钮样式
4. **平静配色** - 使用蓝色、灰色为主色调，绿色仅用于确认操作
5. **无障碍** - 足够的对比度、可读的字体大小

### UI重构方案

#### Phase 1: 统一设计系统
- 定义统一的颜色常量（Primary、Secondary、Success、Warning、Danger）
- 创建共享按钮样式（Primary、Secondary、Outline、Danger）
- 统一间距规范（4px网格系统）
- 统一字体大小（标题16px、正文14px、辅助12px）

#### Phase 2: 重构诊断面板(ConsultationPanel)
- 移除多余边框，使用留白分隔
- 操作按钮移至面板底部固定区域
- 精简表单字段标签样式
- 统一输入框高度和间距

#### Phase 3: 重构处方面板(PrescriptionEditorPanel)
- 移除重复的"治法方案/治疗原则"字段（使用诊断区的数据）
- 操作按钮重新布局：快速导入放顶部，CRUD操作放底部
- 药材卡片区域增加视觉分隔
- 价格信息区域使用卡片样式，与药材区分离

#### Phase 4: 重构底部操作栏
- 简化状态显示（使用图标+颜色代替长文本）
- 按钮按重要性排列：完成看诊(主) > 打印 > 暂停(次)
- 增加按钮间距，提升可点击性

#### Phase 5: 清理技术债务
- 删除废弃的 MedicalCaseFlowView 相关文件
- 删除废弃的 MedicalCaseEditorView 相关文件
- 合并/清理冗余ViewModel代码

## Impact

### 影响范围
- **Views**: MedicalCaseWorkspaceView, ConsultationPanel, PrescriptionEditorPanel
- **ViewModels**: MedicalCaseWorkspaceViewModel, ConsultationPanelViewModel, PrescriptionPanelViewModel
- **Styles**: 新增共享样式资源字典

### 风险评估
- **低风险**: 纯UI重构，不涉及业务逻辑变更
- **测试**: 现有ViewModel单元测试仍然有效
- **兼容性**: 不影响API或数据结构

## Alternatives Considered

1. **引入第三方UI库（如MaterialDesignInXAML）**
   - 优点：现成的现代化控件
   - 缺点：增加依赖、学习成本、可能与现有样式冲突
   - **决定：不采用**（用户明确要求不引入第三方控件）

2. **完全重写界面**
   - 优点：可以从零开始设计
   - 缺点：工作量大、风险高
   - **决定：不采用**（渐进式重构更安全）

## Success Criteria

1. 通过视觉审查：界面布局整齐、颜色统一、无多余边框
2. 操作按钮位置符合用户习惯
3. 删除所有废弃的View/ViewModel文件
4. 现有单元测试全部通过
5. 无新增编译警告
