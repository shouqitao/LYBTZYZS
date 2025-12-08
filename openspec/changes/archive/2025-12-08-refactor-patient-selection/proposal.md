# Proposal: refactor-patient-selection

## Summary
针对患者选择模块进行系统性优化，聚焦于性能提升和UI/UX改进。基于代码重新评估后，移除原有的"架构简化"阶段（因现有架构已经良好分层）。

## Problem Statement

### 当前问题

**1. 代码现状澄清**

经过代码分析，发现现有架构实际上已经良好分层：
- `PatientSelectionView` + `PatientSelectionViewModel` - 主力生产页面（完整实现）
- `PatientSearchManager` - 已从ViewModel提取的搜索服务（Issue #1790已完成）
- `MedicalCaseStartCoordinator`、`PendingQueueManager` - 职责清晰的协调器

但存在一个遗留问题：
- `PatientSelectorControl` + `PatientSelectorViewModel` (Presentation层) 是**未完成的半成品控件**，使用模拟数据，未连接API

**2. 性能问题**
- 当前防抖时间300ms可能过短，导致频繁API请求
- 缺乏客户端缓存机制，重复搜索相同关键字会重新请求
- Server端返回完整PatientDto，可考虑轻量级DTO

**3. UI/UX不足**
- 患者选择器缺乏键盘导航支持（如上下方向键选择、回车确认）
- 搜索框没有明确的搜索中状态指示
- 搜索结果无高亮匹配文字

## Proposed Solution

### Phase 1: 代码清理 (patient-selection-cleanup) - 已评估为可选

**选项A（推荐）**：保留但标记 `PatientSelectorControl` 为实验性/未完成
**选项B**：删除未完成的 `PatientSelectorControl` 控件

> 注：原计划的"合并PatientSearchManager"已确认为**不需要**，因为该服务已正确分离（Issue #1790）

### Phase 2: 性能优化 (patient-search-performance)
1. 增加防抖时间从300ms至500ms
2. 实现客户端搜索结果缓存（LRU缓存，最大10条，5分钟过期）
3. 优化Server端分页查询，支持投影查询减少数据传输
4. 支持搜索请求取消（避免旧请求覆盖新结果）

### Phase 3: UI/UX改进 (patient-selector-ux)
1. 添加键盘导航支持（Down从搜索框到列表、Up/Down移动、Enter确认、Escape取消、Ctrl+N新建）
2. 搜索状态指示器（Idle/Debouncing/Searching/ResultsReady/Error状态机）
3. 搜索结果关键字高亮
4. 结果计数显示

## Impact Analysis

### 影响范围

**Server端：**
- `PatientsController.cs` - 可选添加轻量级搜索端点
- `PatientService.cs` - 可选优化查询投影

**Client端：**
- `PatientSelectionViewModel.cs` - 集成缓存服务、调整防抖
- `PatientSelectionView.xaml` - 添加键盘导航、状态指示UI
- 新增 `PatientSearchCache.cs` - LRU缓存服务
- 新增 `HighlightHelper.cs` - 关键字高亮工具

**Shared：**
- 可选添加 `PatientSearchResultDto` 轻量级DTO

### 风险评估
- **低风险**：主要是增量改进，不涉及大规模架构变更
- **兼容性**：现有API保持向后兼容
- **测试覆盖**：缓存服务需要单元测试

## Spec Deltas

1. **[patient-search-performance](specs/patient-search-performance/spec.md)** - 新规范
   - 定义缓存机制规范
   - 定义防抖参数
   - 定义性能指标

2. **[patient-selector-ux](specs/patient-selector-ux/spec.md)** - 新规范
   - 键盘导航规范
   - 搜索状态机规范
   - 视觉反馈规范

3. ~~**[patient-selection-architecture](specs/patient-selection-architecture/spec.md)**~~ - **建议删除或大幅修改**
   - 原假设已被证伪，现有架构无需简化

## Success Criteria

1. 搜索响应时间 < 50ms（缓存命中时）
2. 搜索响应数据大小减少30%（如实现轻量级DTO）
3. 键盘可完成完整的患者选择流程
4. 所有现有功能正常工作（回归测试通过）

## Timeline Estimate

- Phase 1 (代码清理): 可选，1个任务
- Phase 2 (性能优化): 3个任务
- Phase 3 (UI/UX): 3个任务

## 重新评估说明

| 原假设 | 实际情况 | 结论 |
|--------|----------|------|
| PatientSearchManager需要合并 | 已是独立服务，被正确使用 | **无需修改** |
| 两套组件功能重叠 | PatientSelectorControl是未完成的控件 | **清理或保留** |
| 职责边界模糊 | 实际上分层良好 | **无需修改** |

## Stakeholder Sign-off

- [ ] 代码现状确认
- [ ] Phase 1方向确认（清理/保留PatientSelectorControl）
- [ ] 产品确认UI/UX改动
- [ ] 测试计划审核
