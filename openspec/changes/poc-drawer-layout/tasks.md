# Tasks: POC - 隐藏式Drawer导航布局

## 0. 准备工作

- [ ] 0.1 创建POC分支: `poc/drawer-layout`
- [ ] 0.2 备份当前MainWindow.xaml

## 1. Drawer基础结构

- [ ] 1.1 修改MainWindow.xaml，移除顶部Header（Row 0）
- [ ] 1.2 添加Drawer面板Border（Width=240px, HorizontalAlignment=Left）
- [ ] 1.3 添加遮罩层Border（Background=#80000000）
- [ ] 1.4 添加汉堡按钮（左上角，48x48px）
- [ ] 1.5 设置Drawer初始状态（TranslateTransform X=-240）

## 2. Drawer内容

- [ ] 2.1 添加Logo和系统名称区域
- [ ] 2.2 添加时间显示（绑定CurrentTime）
- [ ] 2.3 添加API状态指示器（绑定ApiStatus）
- [ ] 2.4 添加用户信息区域（头像/名称/角色）
- [ ] 2.5 添加"修改个人信息"按钮
- [ ] 2.6 添加"修改密码"按钮
- [ ] 2.7 添加"退出登录"按钮（底部固定）

## 3. 动画实现

- [ ] 3.1 创建OpenDrawerStoryboard（滑入动画，300ms）
- [ ] 3.2 创建CloseDrawerStoryboard（滑出动画，300ms）
- [ ] 3.3 实现遮罩层淡入/淡出效果
- [ ] 3.4 测试动画流畅度

## 4. 交互逻辑

- [ ] 4.1 MainWindowViewModel添加IsDrawerOpen属性
- [ ] 4.2 实现ToggleDrawerCommand
- [ ] 4.3 实现CloseDrawerCommand
- [ ] 4.4 遮罩层点击关闭Drawer
- [ ] 4.5 添加快捷键支持（Ctrl+M切换，Escape关闭）

## 5. 样式优化

- [ ] 5.1 汉堡按钮hover效果
- [ ] 5.2 Drawer菜单项hover效果
- [ ] 5.3 Drawer阴影效果（DropShadow）
- [ ] 5.4 整体视觉一致性检查

## 6. 验证测试

- [ ] 6.1 验证Drawer开关功能正常
- [ ] 6.2 验证所有功能（时间、API状态、用户菜单、退出）可用
- [ ] 6.3 验证工作区在Drawer隐藏时占据100%空间
- [ ] 6.4 验证动画流畅度（无卡顿）
- [ ] 6.5 验证快捷键功能
- [ ] 6.6 验证不影响现有业务功能
- [ ] 6.7 不同分辨率下的显示效果

## 7. 文档和评估

- [ ] 7.1 记录POC实现过程中的问题
- [ ] 7.2 对比方案E的实际体验差异
- [ ] 7.3 收集用户反馈（如适用）
- [ ] 7.4 撰写POC评估报告
