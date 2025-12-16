# Tasks: refactor-role-navigation

## 1. 创建角色导航服务

- [ ] 1.1 创建 `IRoleNavigationService` 接口
  - 定义 `NavigateToHome()` 方法
  - 定义 `GetHomeViewForCurrentRole()` 方法
  - 定义 `CurrentUserRole` 属性

- [ ] 1.2 实现 `RoleNavigationService`
  - 注入 IRegionManager 和 ISessionManager
  - 实现角色到主页视图的映射：
    - Admin → AdminHomeView
    - Doctor → ClinicalHomeView
    - 默认 → ClinicalHomeView

- [ ] 1.3 在DI容器注册服务
  - 修改 ServiceCollectionExtensions.cs
  - 注册为 Singleton

## 2. 创建SidebarControl控件

- [ ] 2.1 创建 SidebarControl.xaml
  - 从MainWindow.xaml提取侧边栏代码
  - 结构：展开按钮 → 用户信息 → 菜单区 → 状态信息 → 退出按钮

- [ ] 2.2 创建 SidebarControl.xaml.cs
  - 定义依赖属性：
    - IsExpanded (bool)
    - CurrentUser (UserDto)
    - ApiStatus (enum)
    - CurrentTime (DateTime)
  - 定义命令依赖属性：
    - ToggleCommand
    - NavigateToHomeCommand
    - EditProfileCommand
    - ChangePasswordCommand
    - LogoutCommand

- [ ] 2.3 添加返回主页按钮
  - 位置：菜单区域顶部（用户信息下方第一个）
  - 图标：Home图标
  - 样式：SidebarMenuItemStyle
  - 收缩时仅图标，展开时图标+文字

## 3. 更新MainWindow

- [ ] 3.1 替换内联侧边栏为SidebarControl
  - 删除MainWindow.xaml中Grid.Row="0"到Row="6"的侧边栏代码（约130行）
  - 使用单行SidebarControl替代

- [ ] 3.2 更新MainWindowViewModel
  - 注入 IRoleNavigationService
  - 添加 NavigateToHomeCommand（调用IRoleNavigationService.NavigateToHome()）
  - 绑定到SidebarControl

## 4. 验证和测试

- [ ] 4.1 编译验证
  - 确保0错误0警告

- [ ] 4.2 功能测试
  - SidebarControl展开/收缩正常
  - 返回主页按钮显示正确
  - Admin角色返回AdminHomeView
  - Doctor角色返回ClinicalHomeView
  - 所有原有功能正常（修改信息、修改密码、退出登录）
