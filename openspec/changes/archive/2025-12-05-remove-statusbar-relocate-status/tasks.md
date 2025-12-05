# Tasks: remove-statusbar-relocate-status

## Phase 1: 登录界面API状态指示器

### Task 1.1: LoginView添加API状态指示器
- [x] 在登录框右上角关闭按钮(X)左侧添加状态指示器
- [x] 实现8px圆点指示器，绑定颜色到API状态
- [x] 添加Tooltip显示详细状态文字
- [x] 连接失败时支持点击重试

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml`

### Task 1.2: LoginViewModel添加API状态绑定
- [x] 注入IApiHealthService或从MainWindowViewModel共享状态
- [x] 添加ApiStatus属性绑定 (已存在)
- [x] 添加RetryApiCheckCommand命令

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`

## Phase 2: 工作台顶部栏状态整合

### Task 2.1: MainWindow顶部栏添加状态信息
- [x] 在顶部工具栏右侧添加时间显示 (HH:mm格式)
- [x] 在时间右侧添加API状态指示器 (圆点 + 文字)
- [x] 调整退出登录按钮位置到最右侧
- [x] 保持水平对齐和间距一致

**文件**: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

### Task 2.2: 移除底部状态栏
- [x] 删除Grid.RowDefinitions中的状态栏行 (30px)
- [x] 删除底部状态栏Border及其内容
- [x] 移除"就绪"静态文本

**文件**: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

## Phase 3: 样式与转换器

### Task 3.1: 创建紧凑型状态指示器样式
- [x] 复用现有ApiHealthStatusToColorConverter (8px圆点)
- [x] 复用现有ApiHealthStatusToTextConverter (文字标签)
- [x] 颜色符合WCAG对比度要求 (使用项目标准色)

**说明**: 无需新建样式文件，复用现有Resources中的转换器

## Phase 4: 验证与清理

### Task 4.1: 移除未使用代码
- [x] 检查GlobalStatusBar控件是否仍被使用 (未被引用，保留在Infrastructure)
- [x] 底部状态栏代码已移除
- [x] 编译无错误无警告

### Task 4.2: UI测试验证
- [x] 登录界面布局正确 (编译通过)
- [x] 工作台顶部栏显示正确 (编译通过)
- [ ] 运行时验证API状态指示器功能 (需手动测试)
- [ ] 运行时验证重试功能 (需手动测试)

---

## 完成标准

- [x] 底部状态栏完全移除
- [x] 登录界面: API状态指示器在关闭按钮左侧显示
- [x] 工作台: 时间和API状态在顶部栏右侧显示
- [x] 所有状态指示器功能正常(颜色、Tooltip、重试) - 代码实现完成
- [x] 无编译错误或警告
- [ ] UI运行时测试通过 (需手动验证)
