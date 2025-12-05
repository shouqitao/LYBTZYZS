# Change: 重构工作台布局与视觉优化

## Why

当前系统布局存在以下问题：

### Shell层问题（影响所有二级界面）
1. **顶部工具栏60px + 内容区边距80px = 固定占用140px**
2. 1080p分辨率下，真正可用工作区仅794px（73.5%）
3. 看诊界面、管理界面等二级工作区空间不足

### 工作台页面问题
4. 标题栏与工作区比例不协调，标题区域占用过多垂直空间
5. "修改个人信息"和"修改密码"按钮放置在工作区内，与主功能混杂
6. 功能图标使用emoji，与MaterialDesign风格不一致
7. 整体视觉效果与成熟的企业管理系统存在差距

## What Changes

### 0. Shell层紧凑优化（优先级最高）
- **MODIFIED** 顶部工具栏高度：60px → **48px**
- **MODIFIED** 内容区外边距：20px → **12px**
- **MODIFIED** 内容区内边距：20px → **12px**
- **结果**: Shell层固定占用从140px降至**96px**，节省44px

### 1. Header用户菜单（复用现有空间）
- **MODIFIED** 将"修改个人信息"和"修改密码"移至顶部Header右侧用户菜单
- **MODIFIED** Header右侧布局：时间 -> API状态 -> 用户菜单(含个人信息/密码修改) -> 退出登录
- **注意**: 用户菜单复用现有48px高度，不增加Header高度

### 2. 工作台布局优化
- **MODIFIED** 移除工作台内的用户信息区域和按钮
- **MODIFIED** 调整标题区域高度，从当前的~120px减少到~60px
- **MODIFIED** 功能卡片网格占据更多垂直空间

### 3. 视觉效果改进
- **MODIFIED** 功能卡片图标从emoji调整为MaterialDesign图标
- **MODIFIED** 卡片布局从3x2调整为响应式网格
- **MODIFIED** 增加卡片间距和圆角，提升现代感
- **MODIFIED** 统一Admin和Clinical工作台的设计语言

## Impact

- Affected specs:
  - `shell-layout` - Header布局修改
  - `workbench-layout` (NEW) - 工作台布局规范

- Affected code:
  - `MainWindow.xaml` - Header区域添加用户菜单
  - `AdminHomeView.xaml` - 管理员工作台重构
  - `ClinicalHomeView.xaml` - 医生工作台重构
  - 相关ViewModel可能需要调整命令绑定
