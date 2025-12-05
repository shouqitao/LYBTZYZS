# Tasks: 重构工作台布局

## 0. Shell层紧凑优化（优先级最高）

- [ ] 0.1 MainWindow.xaml: 顶部工具栏高度60px→48px
- [ ] 0.2 MainWindow.xaml: 内容区外边距Border Margin="20"→"12"
- [ ] 0.3 MainWindow.xaml: 内容区内边距Grid Margin="20"→"12"
- [ ] 0.4 调整Header内元素间距以适应48px高度
- [ ] 0.5 验证Shell层总占用从140px降至96px

## 1. Header用户菜单集成（复用现有空间）

- [ ] 1.1 创建UserMenuControl用户控件（紧凑设计，适应48px高度）
- [ ] 1.2 修改MainWindow.xaml，在Header右侧添加UserMenuControl（不增加高度）
- [ ] 1.3 实现下拉菜单：修改个人信息、修改密码、退出登录
- [ ] 1.4 更新MainWindowViewModel，添加个人信息/密码修改命令
- [ ] 1.5 调整现有退出登录按钮位置到菜单内

## 2. 管理员工作台(AdminHomeView)重构

- [ ] 2.1 移除用户信息区域（UserInfoSection）
- [ ] 2.2 调整标题区域高度从~120px到~80px
- [ ] 2.3 将功能卡片emoji图标替换为MaterialDesign图标
- [ ] 2.4 调整卡片网格布局，增加间距和圆角
- [ ] 2.5 优化卡片hover效果和交互动画
- [ ] 2.6 移除底部提示文字区域

## 3. 医生工作台(ClinicalHomeView)重构

- [ ] 3.1 移除用户信息区域
- [ ] 3.2 调整标题区域高度
- [ ] 3.3 统一卡片样式与Admin工作台一致
- [ ] 3.4 优化主功能区（开始接诊）视觉效果
- [ ] 3.5 调整统计数据区域布局
- [ ] 3.6 优化辅助功能卡片布局

## 4. 样式和资源

- [ ] 4.1 创建WorkbenchStyles.xaml资源字典
- [ ] 4.2 定义统一的卡片样式（FunctionCardStyle）
- [ ] 4.3 定义标题区域样式（WorkbenchTitleStyle）
- [ ] 4.4 引入MaterialDesignThemes图标资源
- [ ] 4.5 更新App.xaml合并资源字典

## 5. 测试验证

- [ ] 5.1 验证Admin工作台所有功能入口可用
- [ ] 5.2 验证Clinical工作台所有功能入口可用
- [ ] 5.3 验证用户菜单中个人信息/密码修改功能
- [ ] 5.4 验证不同分辨率下的布局响应
- [ ] 5.5 编译验证无警告无错误
