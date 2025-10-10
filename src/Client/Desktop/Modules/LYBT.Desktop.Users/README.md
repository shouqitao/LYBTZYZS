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
- **LYBT.Desktop.Foundation**: 提供Repository基类和ApiClient。
- **模块内 Repositories/**: 提供与后端交互的数据访问层实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Users\LYBT.Desktop.Users.csproj
```

## 🔌 数据访问层架构

此项目为UI模块，采用 **ViewModel → Repository → ApiClient** 三层架构：

- **ViewModel 层**：UI业务逻辑，通过依赖注入获取 Repository
- **Repository 层**（模块内 `Repositories/`）：数据访问与转换，调用 Foundation 层的 ApiClient
- **ApiClient 层**（Foundation）：统一的HTTP通信封装

### 代码示例

```csharp
// UserManagementViewModel.cs
using LYBT.Desktop.Users.Repositories;

public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserRepository _userRepository;

    public UserManagementViewModel(IUserRepository userRepository, ...)
    {
        _userRepository = userRepository;
    }

    private async Task LoadUsersAsync()
    {
        var result = await _userRepository.GetPagedAsync(1, 100);
        if (result != null && result.Items != null)
        {
            foreach (var user in result.Items)
            {
                Users.Add(user);
            }
        }
    }
}
```

**关键差异**：
- ❌ 禁止直接依赖 `LYBT.Desktop.Services` 的 Server Service（会导致运行时崩溃）
- ⚠️ **注意**：Users 模块的 ViewModel 尚未完成迁移（Issue #1128），部分 ViewModel 仍使用 `IUserService`

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*