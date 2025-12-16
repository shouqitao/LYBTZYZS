# Design: refactor-role-navigation

## Context

当前项目采用WPF + Prism框架。MainWindow.xaml中侧边栏代码约130行，与窗口逻辑耦合。角色导航逻辑分散在多个ViewModel中。

本次重构包含两个目标：
1. 将侧边栏提取为独立的SidebarControl控件
2. 创建统一的角色导航服务并添加返回主页功能

## Goals / Non-Goals

**Goals:**
- 提供统一的角色导航服务接口
- 实现角色感知的主页导航
- 将侧边栏提取为可复用的SidebarControl控件
- 在侧边栏添加返回主页按钮
- 减少MainWindow.xaml代码量

**Non-Goals:**
- 不重构AdminHomeView/ClinicalHomeView内部逻辑
- 不改变Prism Region导航机制
- 不实现细粒度权限控制

## Decisions

### Decision 1: SidebarControl使用依赖属性而非ViewModel

**选择**: 控件使用依赖属性绑定，不创建独立ViewModel

**理由**:
- 侧边栏数据来自MainWindowViewModel，无需独立数据源
- 依赖属性支持双向绑定，满足IsExpanded等状态同步需求
- 避免多层ViewModel嵌套，保持简单

### Decision 2: 角色映射硬编码

**选择**: 在RoleNavigationService中硬编码角色→视图映射

```csharp
public string GetHomeViewForCurrentRole()
{
    return _sessionManager.CurrentUser?.Role switch
    {
        UserRole.Admin => "AdminHomeView",
        UserRole.Doctor => "ClinicalHomeView",
        _ => "ClinicalHomeView"
    };
}
```

**理由**:
- 当前仅2种角色，配置化过度设计
- 编译时检查，避免字符串拼写错误

### Decision 3: 返回主页按钮位置

**选择**: 菜单区顶部（用户信息下方第一个）

**理由**:
- 高频操作，位置显眼
- 与"修改个人信息"、"修改密码"形成完整的用户操作区
- 符合用户习惯（主页通常在顶部）

## Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      MainWindow.xaml                         │
│  ┌───────────────────┐  ┌─────────────────────────────────┐ │
│  │   SidebarControl  │  │         ContentRegion           │ │
│  │  ┌─────────────┐  │  │                                 │ │
│  │  │ 展开/收缩   │  │  │   AdminHomeView                 │ │
│  │  │ 用户信息    │  │  │   or                            │ │
│  │  │ ──────────  │  │  │   ClinicalHomeView              │ │
│  │  │ 🏠返回主页  │──┼──│   or                            │ │
│  │  │ 修改信息    │  │  │   其他业务视图                   │ │
│  │  │ 修改密码    │  │  │                                 │ │
│  │  │ ──────────  │  │  │                                 │ │
│  │  │ 状态信息    │  │  │                                 │ │
│  │  │ ──────────  │  │  │                                 │ │
│  │  │ 退出登录    │  │  │                                 │ │
│  │  └─────────────┘  │  │                                 │ │
│  └───────────────────┘  └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                    │
                    ▼ NavigateToHomeCommand
┌─────────────────────────────────────────────────────────────┐
│               RoleNavigationService                          │
│  - IRegionManager                                           │
│  - ISessionManager                                          │
│  + NavigateToHome() → 根据角色导航到对应主页                  │
│  + GetHomeViewForCurrentRole()                              │
└─────────────────────────────────────────────────────────────┘
```

## SidebarControl 依赖属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| IsExpanded | bool | 展开/收缩状态 |
| CurrentUser | UserDto | 当前用户信息 |
| ApiStatus | ApiHealthStatus | API状态 |
| CurrentTime | DateTime | 当前时间 |
| ToggleCommand | ICommand | 展开/收缩命令 |
| NavigateToHomeCommand | ICommand | 返回主页命令 |
| EditProfileCommand | ICommand | 修改个人信息命令 |
| ChangePasswordCommand | ICommand | 修改密码命令 |
| LogoutCommand | ICommand | 退出登录命令 |

## Interface Definition

```csharp
public interface IRoleNavigationService
{
    /// <summary>
    /// 导航到当前用户角色对应的主页
    /// </summary>
    void NavigateToHome();

    /// <summary>
    /// 获取当前用户角色对应的主页视图名称
    /// </summary>
    string GetHomeViewForCurrentRole();

    /// <summary>
    /// 当前用户角色
    /// </summary>
    UserRole? CurrentUserRole { get; }
}
```

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 控件化增加文件数量 | 换取MainWindow简洁性和控件复用性 |
| 依赖属性较多 | 保持与原逻辑一致，无额外学习成本 |

## Open Questions

- 无
