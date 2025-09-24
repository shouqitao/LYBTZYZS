# LYBT.Desktop.Workbench.Admin - 系统管理工作台

## 🎯 项目概述

**系统管理工作台**是专为系统管理员设计的综合性管理环境，它聚合了用户管理、系统配置、数据维护、监控报表等多个核心业务模块的管理视图，提供一站式的后台管理功能。本项目基于Prism MVVM架构，是管理员进行系统维护的主要交互界面。

## 📦 项目结构

```
SystemWorkbench/
├── Services/                   # 服务层
│   └── ISystemWorkbenchNavigator.cs # 系统工作台导航接口
├── ViewModels/                 # 视图模型
│   └── SystemWorkbenchMainViewModel.cs # 主工作台视图模型
├── Views/                      # 用户界面
│   └── SystemWorkbenchMainView.xaml  # 主工作台视图
└── SystemWorkbenchModule.cs    # Prism模块定义
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc**: 用于模块化、依赖注入和区域导航。
- **LYBT.Desktop.Core**: 提供ViewModel基类和通用服务。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Workbenches\SystemWorkbench\LYBT.Desktop.Workbench.Admin.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `Services` 层实现的业务服务接口（如 `IUserService`）来完成业务操作，并通过导航将各个业务模块的管理视图加载到内容区域。

```csharp
// SystemWorkbenchMainViewModel.cs
public class SystemWorkbenchMainViewModel : CoreViewModel
{
    private readonly IRegionManager _regionManager;

    public SystemWorkbenchMainViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
        // 导航到用户管理视图
        NavigateToUserManagementCommand = new DelegateCommand(NavigateToUserManagement);
    }

    private void NavigateToUserManagement()
    {
        // "UserListView" 在 LYBT.Desktop.Users 模块中定义和注册
        _regionManager.RequestNavigate("AdminContentRegion", "UserListView");
    }
}
```

---

*（详细的内部导航、集成模块等信息请参考本文档后续章节。）*