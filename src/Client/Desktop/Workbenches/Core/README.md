# LYBT.Desktop.Workbench.Core - 工作台核心框架

## 🎯 项目概述

**工作台核心框架 (Workbench Core)** 是桌面客户端UI架构的关键部分。它不提供具体的用户界面，而是为不同的用户角色（如管理员、医生）提供一个可切换的、独立的**工作台（Workbench）**环境。其核心职责是根据用户角色，动态地加载和导航到对应的工作台模块。

## 📦 项目结构

```
LYBT.Desktop.Workbench.Core/
├── Routing/                   # 路由核心
│   ├── IWorkbenchRouter.cs      # 角色到工作台的路由接口
│   └── WorkbenchRouter.cs       # 路由实现
├── Navigation/                # 导航模型
│   └── NavigationItem.cs      # 导航菜单项的模型
└── WorkbenchCoreModule.cs       # Prism模块定义与注册
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc**: 用于模块化和依赖注入。
- **LYBT.Desktop.Core**: 依赖于桌面核心库。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Workbenches\Core\LYBT.Desktop.Workbench.Core.csproj
```

## 🔌 API 接口

此项目为UI基础设施，不直接调用API接口。

### 核心机制

`WorkbenchRouter` 服务在用户登录后被调用，根据用户的角色（`UserRole` 枚举）决定导航到哪个主视图区域。例如：

- **Admin** 角色 → 导航到 `SystemWorkbench`
- **Doctor** 角色 → 导航到 `MedicalWorkbench`

```csharp
// WorkbenchRouter.cs
public class WorkbenchRouter : IWorkbenchRouter
{
    private readonly IRegionManager _regionManager;

    public void Route(UserRole role)
    {
        string targetWorkbench = role switch
        {
            UserRole.Admin => "SystemWorkbenchMainView",
            UserRole.Doctor => "MedicalWorkbenchMainView",
            _ => "DefaultView"
        };

        _regionManager.RequestNavigate("ContentRegion", targetWorkbench);
    }
}
```