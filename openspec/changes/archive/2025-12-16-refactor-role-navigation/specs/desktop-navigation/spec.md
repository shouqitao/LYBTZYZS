# Desktop Navigation Specification

## ADDED Requirements

### Requirement: Role-Based Home Navigation

系统 SHALL 提供基于用户角色的主页导航服务，根据当前登录用户的角色自动导航到对应的主页视图。

#### Scenario: Admin user navigates to home
- **WHEN** 当前用户角色为 Admin
- **AND** 用户点击侧边栏"返回主页"按钮
- **THEN** 系统导航到 AdminHomeView

#### Scenario: Doctor user navigates to home
- **WHEN** 当前用户角色为 Doctor
- **AND** 用户点击侧边栏"返回主页"按钮
- **THEN** 系统导航到 ClinicalHomeView

#### Scenario: Unknown role fallback
- **WHEN** 当前用户角色未知或为其他角色
- **AND** 用户点击侧边栏"返回主页"按钮
- **THEN** 系统导航到默认主页 ClinicalHomeView

### Requirement: SidebarControl Component

系统 SHALL 提供独立的 SidebarControl 侧边栏控件，包含用户信息、导航菜单和状态显示功能。

#### Scenario: Sidebar displays user information
- **WHEN** 用户已登录
- **THEN** 侧边栏显示用户头像（姓名首字）
- **AND** 展开状态显示用户姓名和角色

#### Scenario: Sidebar home button in menu
- **WHEN** 侧边栏展开
- **THEN** 菜单区域顶部显示"返回主页"按钮（带主页图标和文字）

#### Scenario: Sidebar home button collapsed
- **WHEN** 侧边栏收缩
- **THEN** 仅显示主页图标

#### Scenario: Sidebar toggle expand/collapse
- **WHEN** 用户点击展开/收缩按钮或按Ctrl+M
- **THEN** 侧边栏在展开和收缩状态之间切换

### Requirement: IRoleNavigationService Interface

系统 SHALL 提供 IRoleNavigationService 接口，封装角色感知的导航逻辑。

#### Scenario: Service provides role-aware navigation
- **GIVEN** IRoleNavigationService 实例已注入
- **WHEN** 调用 NavigateToHome() 方法
- **THEN** 系统根据当前用户角色导航到对应主页

#### Scenario: Service provides current role
- **GIVEN** 用户已登录
- **WHEN** 访问 CurrentUserRole 属性
- **THEN** 返回当前用户的 UserRole 枚举值
