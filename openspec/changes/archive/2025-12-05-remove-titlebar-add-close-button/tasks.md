# Tasks: remove-titlebar-add-close-button

## Phase 1: 移除标题栏

- [x] 1.1 修改 `MainWindow.xaml`
  - [x] 1.1.1 设置 `WindowStyle="None"` 移除标题栏
  - [x] 1.1.2 设置 `ResizeMode="NoResize"` 禁用调整大小
  - [x] 1.1.3 确认 `WindowState="Maximized"` 保持最大化

## Phase 2: 实现Alt+F4拦截逻辑

- [x] 2.1 修改 `MainWindow.xaml.cs`
  - [x] 2.1.1 重写 `OnPreviewKeyDown` 拦截Alt+F4
  - [x] 2.1.2 添加 `IsOnLoginScreen()` 判断方法
  - [x] 2.1.3 仅登录界面允许Alt+F4关闭，其他界面阻止
  - [x] 2.1.4 登录界面Alt+F4调用ViewModel显示确认框

- [x] 2.2 修改 `MainWindowViewModel.cs`
  - [x] 2.2.1 添加 `RequestCloseApplicationAsync()` 方法
  - [x] 2.2.2 使用 `ShowConfirmationAsync` 显示确认框

## Phase 3: 添加关闭按钮

- [x] 3.1 修改 `LoginView.xaml`
  - [x] 3.1.1 在登录框Border内右上角添加关闭按钮(X)
  - [x] 3.1.2 按钮样式：显示"X"，悬停变红色背景白色文字
  - [x] 3.1.3 绑定到 `CloseApplicationCommand`

- [x] 3.2 修改 `LoginViewModel.cs`
  - [x] 3.2.1 添加 `CloseApplicationCommand` 属性
  - [x] 3.2.2 实现退出确认框 + `Application.Current.Shutdown()` 退出

## Phase 4: 优化复选框布局

- [x] 4.1 修改 `LoginView.xaml`
  - [x] 4.1.1 "记住用户名"和"记住密码"水平对齐在同一行
  - [x] 4.1.2 "记住密码"后显示警告文字"仅在可信设备使用"
  - [x] 4.1.3 确认勾选"记住密码"时自动勾选"记住用户名"（ViewModel已实现）

## Phase 5: 登录界面背景图与FHD优化

- [x] 5.1 添加背景图资源
  - [x] 5.1.1 创建 `Shell/Assets/Images/Backgrounds/` 目录
  - [x] 5.1.2 添加 `img-login-background.jpg` 背景图
  - [x] 5.1.3 更新 `LYBT.Desktop.Shell.csproj` 包含图片资源

- [x] 5.2 修改 `LoginView.xaml` 背景
  - [x] 5.2.1 将渐变背景替换为ImageBrush背景图
  - [x] 5.2.2 设置Stretch="UniformToFill"确保填满屏幕
  - [x] 5.2.3 调整半透明遮罩层透明度适配背景图（#20000000）

- [x] 5.3 FHD(1920x1080)优化
  - [x] 5.3.1 登录框宽度优化：480px（FHD下占25%屏幕宽度）
  - [x] 5.3.2 登录框位置：右侧居中，右边距80px
  - [x] 5.3.3 字体大小优化：左侧标题52px，登录框标题24px，输入框14px，按钮15px
  - [x] 5.3.4 输入框高度：48px
  - [x] 5.3.5 按钮高度：52px
  - [x] 5.3.6 内边距优化：登录框Padding 40,36

- [x] 5.4 多分辨率适配
  - [x] 5.4.1 使用Grid相对布局确保缩放（左侧*弹性，右侧480固定）
  - [ ] 5.4.2 测试1366x768分辨率显示（需手动测试）
  - [ ] 5.4.3 测试2560x1440分辨率显示（需手动测试）

## Phase 6: 验证

- [x] 6.1 编译验证
  - [x] 6.1.1 运行 `dotnet build LYBT.All.sln`
  - [x] 6.1.2 确认0错误0警告

- [ ] 6.2 功能验证（需手动测试）
  - [ ] 6.2.1 启动程序确认无标题栏
  - [ ] 6.2.2 确认登录框右上角显示关闭按钮
  - [ ] 6.2.3 登录界面：点击X按钮弹出确认框，确认后退出程序
  - [ ] 6.2.4 登录界面：Alt+F4可退出程序
  - [ ] 6.2.5 工作台界面：Alt+F4被阻止，无法退出
  - [ ] 6.2.6 工作台界面：必须先退出登录才能关闭程序
  - [ ] 6.2.7 确认"记住用户名"和"记住密码"水平对齐
  - [ ] 6.2.8 确认勾选"记住密码"自动勾选"记住用户名"
  - [ ] 6.2.9 确认背景图正确显示
  - [ ] 6.2.10 确认FHD下登录框大小和位置合适

## Dependencies

```
Phase 1 (移除标题栏) ✓
    ↓
Phase 2 (Alt+F4拦截逻辑) ✓
    ↓
Phase 3 (添加关闭按钮) ✓
    ↓
Phase 4 (优化复选框布局) ✓
    ↓
Phase 5 (背景图与FHD优化) ✓
    ↓
Phase 6 (验证) 编译通过 ✓，待手动功能验证
```

## Estimated Files Changed

| 文件 | 操作 | 预估行数 | 状态 |
|------|------|----------|------|
| `src/Client/Desktop/Shell/Views/MainWindow.xaml` | MODIFY | +2 | ✓ |
| `src/Client/Desktop/Shell/Views/MainWindow.xaml.cs` | MODIFY | +25 | ✓ |
| `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | MODIFY | +15 | ✓ |
| `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml` | MODIFY | +80 | ✓ |
| `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs` | MODIFY | +15 | ✓ |
| `src/Client/Desktop/Shell/Assets/Images/Backgrounds/img-login-background.jpg` | ADD | - | ✓ |
| `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj` | MODIFY | +5 | ✓ |
