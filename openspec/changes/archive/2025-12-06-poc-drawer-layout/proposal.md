# Change: POC - 隐藏式Drawer导航布局

## Why

这是一个**概念验证（POC）**提案，探索使用隐藏式左侧Drawer导航替代现有顶部Header布局。

### 目标
- 验证隐藏式Drawer布局在WPF中的可行性
- 评估空间效率提升效果（理论上可达100%）
- 对比方案E（紧凑顶部Header）的实际体验差异
- 为未来架构决策提供数据支撑

### 背景
当前系统采用顶部Header布局，Shell层固定占用140px（优化后96px）。方案D通过隐藏式Drawer导航，在默认状态下实现0px固定占用，工作区空间最大化。

## What Changes

### Shell布局架构重构
- **REMOVED** 顶部60px Header（移至Drawer内）
- **ADDED** 左上角汉堡按钮（触发Drawer）
- **ADDED** NavigationDrawer控件（覆盖式，点击显示/隐藏）
- **MODIFIED** ContentRegion占据整个窗口（除汉堡按钮外）

### Drawer内容
- 系统Logo和名称
- 当前时间显示
- API状态指示器
- 用户信息和菜单（个人信息、密码修改）
- 退出登录按钮
- （可选）快捷导航链接

### 交互模式
- 默认：Drawer隐藏，仅显示汉堡按钮
- 点击汉堡按钮：Drawer从左侧滑入（覆盖内容，内容区半透明遮罩）
- 点击遮罩或Drawer外部：Drawer滑出隐藏
- 快捷键支持：Ctrl+M 切换Drawer

## Impact

- Affected specs:
  - `shell-layout` - 完全重构Shell布局架构

- Affected code:
  - `MainWindow.xaml` - 移除顶部Header，添加Drawer
  - `MainWindowViewModel` - 添加Drawer开关状态
  - 新增 `NavigationDrawerControl.xaml` 用户控件
  - 可能需要引入MaterialDesignThemes NavigationDrawer

## POC验收标准

1. Drawer可正常显示/隐藏，动画流畅
2. 工作区在Drawer隐藏时占据100%窗口空间
3. 所有原Header功能（时间、API状态、用户菜单、退出）在Drawer中可用
4. 交互体验符合预期（点击外部关闭、快捷键支持）
5. 不影响现有业务功能和导航
