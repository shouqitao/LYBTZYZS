# OpenSpec Proposal: remove-titlebar-add-close-button

## Status: Proposed

## Summary

移除程序的Windows标题栏，实现无边框全屏界面。程序退出入口**仅限登录界面**：登录框右上角的关闭按钮(X)和Alt+F4快捷键。已登录用户必须先退出登录返回登录界面才能关闭程序。

## Motivation

- 提升用户体验：全屏无边框界面更加沉浸和专业
- 现有登录界面已经是全屏设计（WindowState="Maximized"），标题栏显得多余
- 标准软件设计模式：退出入口仅在登录界面，确保用户数据安全
- 防止误操作：已登录状态下禁用Alt+F4，避免意外关闭导致数据丢失

## Scope

### In Scope
- MainWindow.xaml: 设置WindowStyle="None"移除标题栏
- MainWindow.xaml.cs: 添加Alt+F4拦截逻辑（仅登录界面允许）
- LoginView.xaml: 添加关闭按钮(X)，优化复选框布局（水平对齐）
- LoginViewModel.cs: 添加CloseApplicationCommand
- **NEW**: LoginView.xaml: 使用中医风格背景图替代渐变背景
- **NEW**: LoginView.xaml: FHD(1920x1080)为主尺寸优化字体和登录框大小/位置

### Out of Scope
- 窗口拖动功能（保持最大化，不需要拖动）
- 工作台界面的关闭按钮（必须通过退出登录返回登录界面）
- 最小化/最大化按钮（程序保持最大化状态）

## Technical Approach

### 1. 移除标题栏
修改`MainWindow.xaml`：
```xml
<Window ...
        WindowStyle="None"
        WindowState="Maximized"
        ResizeMode="NoResize">
```

### 2. Alt+F4拦截逻辑
在`MainWindow.xaml.cs`中添加：
```csharp
// 仅在登录界面允许Alt+F4关闭
protected override void OnPreviewKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.System && e.SystemKey == Key.F4)
    {
        // 检查当前是否在登录界面
        if (!IsOnLoginScreen())
        {
            e.Handled = true; // 阻止Alt+F4
        }
    }
    base.OnPreviewKeyDown(e);
}
```

### 3. 关闭按钮（登录界面已存在）
登录框右上角的关闭按钮直接调用`Application.Current.Shutdown()`退出程序。
由于仅在登录界面显示，无需logout逻辑。

## Impact Analysis

| 组件 | 影响程度 | 说明 |
|------|---------|------|
| MainWindow.xaml | 中 | 添加WindowStyle="None", ResizeMode="NoResize" |
| MainWindow.xaml.cs | 中 | 添加Alt+F4拦截逻辑 |
| LoginView.xaml | 低 | 确认关闭按钮存在 |
| LoginViewModel.cs | 低 | 确认关闭命令存在 |

## Risk Assessment

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| Alt+F4拦截失效 | 低 | 中 | 同时在Window.Closing事件中拦截 |
| 用户不知如何退出 | 低 | 低 | 登录界面关闭按钮明显可见 |

## Related Issues
- 无

## Related Specs
- authentication: 退出登录流程
- ui-style-conventions: 按钮样式规范
