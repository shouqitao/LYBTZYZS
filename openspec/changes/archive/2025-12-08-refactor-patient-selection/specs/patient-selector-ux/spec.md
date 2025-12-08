# Spec: patient-selector-ux

## Purpose

定义患者选择器的UI/UX改进规范，包括键盘导航、状态指示和搜索结果高亮。

## Context

当前患者选择器缺乏专业应用的交互体验：
- 无键盘导航支持，必须使用鼠标操作
- 搜索状态无视觉反馈
- 搜索结果无关键字高亮

---

## ADDED Requirements

### Requirement: UX-PS-001 键盘导航

患者选择器 SHALL 支持完整的键盘操作流程，无需鼠标即可完成患者选择。快捷键：Down从搜索框到列表、Up/Down在列表移动、Enter确认、Escape取消、Ctrl+N新建。

#### Scenario: 键盘选择患者
- **GIVEN** 搜索框有焦点
- **AND** 用户输入关键字后显示结果列表
- **WHEN** 用户按Down键
- **THEN** 焦点移动到列表第一项
- **AND** 第一项显示选中高亮

#### Scenario: 键盘确认选择
- **GIVEN** 列表中某项被高亮
- **WHEN** 用户按Enter键
- **THEN** 选中该患者
- **AND** 发布PatientSelectedEvent

#### Scenario: 取消操作
- **GIVEN** 搜索框有内容或列表有选中项
- **WHEN** 用户按Escape键
- **THEN** 清空搜索框
- **AND** 清除列表选中状态
- **AND** 焦点返回搜索框

---

### Requirement: UX-PS-002 搜索状态指示

患者选择器 SHALL 显示明确的搜索状态视觉反馈，包括Idle、Debouncing、Searching、ResultsReady、Error五种状态。

#### Scenario: 搜索中状态
- **GIVEN** 用户输入搜索关键字
- **WHEN** API请求进行中
- **THEN** 显示加载指示器ProgressRing
- **AND** 搜索按钮显示加载状态

#### Scenario: 空结果状态
- **GIVEN** 搜索完成
- **WHEN** 结果为空
- **THEN** 显示未找到匹配的患者提示
- **AND** 提供新建患者快捷入口

#### Scenario: 错误状态
- **GIVEN** 搜索请求失败
- **WHEN** 显示错误状态
- **THEN** 显示错误消息
- **AND** 提供重试按钮

---

### Requirement: UX-PS-003 搜索结果高亮

患者选择器 SHALL 在搜索结果中高亮显示匹配的关键字。

#### Scenario: 名称高亮
- **GIVEN** 搜索关键字为某字
- **WHEN** 显示包含该字的患者姓名
- **THEN** 匹配字符使用高亮样式显示
- **AND** 其余文字正常显示

#### Scenario: 拼音码高亮
- **GIVEN** 搜索关键字为拼音字母
- **WHEN** 显示患者拼音码
- **THEN** 匹配部分使用高亮样式显示

---

### Requirement: UX-PS-004 结果计数显示

患者选择器 SHALL 显示搜索结果统计信息。

#### Scenario: 显示结果计数
- **GIVEN** 搜索完成
- **WHEN** 找到多个匹配患者
- **THEN** 显示找到N位患者的文本

#### Scenario: 分页信息
- **GIVEN** 结果超过一页
- **WHEN** 显示结果
- **THEN** 显示第X页共Y页的信息
- **AND** 支持翻页操作

---

## Accessibility

- 支持Tab键在搜索框和列表间切换
- 列表项有明确的焦点样式
- 屏幕阅读器支持结果计数播报
- 高对比度模式下保持可见性

## Dependencies

- `LYBT.Desktop.Presentation` - 样式资源
- `ModernWPF` - UI控件库

## Migration Notes

1. 键盘导航独立实现，不影响现有功能
2. 状态指示通过绑定实现，改动较小
3. 高亮功能需要创建辅助工具类
4. 所有UI变更应在测试环境验证
