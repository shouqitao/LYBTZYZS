# LYBT.Desktop.Users - 用户管理模块

## 🎯 项目概述

**用户管理模块 (Users Module)** 是WPF桌面客户端的业务模块之一，采用MVVM架构。它为管理员提供管理系统用户（如医生、其他管理员）的用户界面，支持用户的创建、编辑、角色分配和状态管理。

## 📦 项目结构

```
LYBT.Desktop.Users/
├── ViewModels/              # MVVM视图模型
│   ├── UserListViewModel.cs     # 列表视图模型
│   └── UserEditViewModel.cs     # 编辑视图模型
├── Views/                   # WPF视图
│   ├── UserListView.xaml        # 用户列表界面
│   └── UserEditView.xaml        # 用户编辑界面
└── UsersModule.cs             # Prism模块定义与注册
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc**: 用于模块化、依赖注入和区域导航。
- **LYBT.Desktop.Core**: 提供ViewModel基类和通用服务。
- **LYBT.Desktop.Services**: 提供与后端交互的业务服务实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Users\LYBT.Desktop.Users.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IUserService` 接口，并调用该服务来完成所有用户相关的业务操作。

```csharp
// UserListViewModel.cs
public class UserListViewModel : CoreViewModel
{
    private readonly IUserService _userService;

    public UserListViewModel(IUserService userService)
    {
        _userService = userService;
    }

    private async Task LoadUsers()
    {
        var result = await _userService.GetPagedAsync(new PagedQueryBaseDto());
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*