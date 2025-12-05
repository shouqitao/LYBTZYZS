# Proposal: remove-statusbar-relocate-status

## Summary

移除底部状态栏(30px高)，将API状态指示器和时间显示重新定位到更符合现代设计规范的位置，分别处理登录前和登录后两种状态。

## Background

当前MainWindow.xaml在底部固定30px状态栏，包含:
- "就绪"文本 (左侧)
- API健康状态指示器 (中右，含圆点+文字+重试按钮)
- 当前时间 (右侧)

问题:
1. 底部状态栏占用固定30px垂直空间，与全屏无边框设计理念冲突
2. 登录界面已是精心设计的左右分屏布局，底部状态栏破坏视觉平衡
3. 状态信息对用户的重要性不同：API状态是关键系统信息，时间是辅助信息
4. 现代UI设计趋势倾向于减少固定区域，将状态信息融入界面

## Design

### 登录前状态 (Pre-login)

**API状态指示器**: 移至登录框右上角关闭按钮(X)的左侧
- 使用小圆点指示器 (8px)
- 颜色语义: 绿色=连接正常，红色=连接失败，黄色=检查中
- 鼠标悬停显示Tooltip: "API状态: 正常/失败/检查中"
- 连接失败时点击可重试

**时间显示**: 移除
- 登录界面不需要显示时间，保持简洁

### 登录后状态 (Post-login)

**API状态指示器**: 移至顶部工具栏右侧，退出登录按钮左侧
- 使用小圆点指示器 (8px) + 文字标签
- 与用户名、退出登录按钮同一水平线
- 连接失败时显示重试链接

**时间显示**: 移至顶部工具栏右侧，API状态左侧
- 格式: HH:mm (不需要秒)
- 与其他状态信息统一在顶部栏

### 视觉参考 (Carbon Design System)

参考IBM Carbon Design System的Status Indicator模式:
- 使用Shape indicator (小圆点 + 颜色 + 标签)
- 保持至少3:1对比度
- 配合文字说明确保无障碍访问

## Impact

### 文件变更

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `Shell/Views/MainWindow.xaml` | 修改 | 移除底部状态栏Row，重构顶部工具栏 |
| `Shell/ViewModels/MainWindowViewModel.cs` | 修改 | 可能调整时间格式 |
| `Auth/Views/LoginView.xaml` | 修改 | 添加API状态指示器到关闭按钮左侧 |
| `Auth/ViewModels/LoginViewModel.cs` | 修改 | 添加API状态绑定 |

### 测试影响

- UI回归测试: 验证登录/工作台界面布局正确
- 功能测试: API状态显示、重试功能、时间显示

## Risks

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| API状态指示器过小被忽视 | 中 | 中 | 使用明显颜色，添加动画(失败时) |
| LoginView与MainWindow的API状态绑定复杂 | 低 | 中 | 使用共享ViewModel或服务注入 |

## Alternatives Considered

1. **保留底部状态栏但减小高度**: 仍占用空间，不够彻底
2. **浮动状态指示器**: 实现复杂，可能遮挡内容
3. **仅移除时间，保留API状态在底部**: 不符合全面清理目标

## Decision

推荐实施方案: 完全移除底部状态栏，按上述设计重新定位状态信息。

---
Created: 2025-12-05
Author: Claude Code
Status: DRAFT
