# LYBT.Desktop.Auth - 认证授权模块

## 🎯 项目概述

**认证授权模块 (Auth Module)** 是WPF桌面客户端的安全核心，采用MVVM架构。它为用户提供登录界面，负责用户身份认证、JWT令牌的获取与管理，并为整个应用程序提供会话管理和权限验证的基础。

## 📦 项目结构

```
LYBT.Desktop.Auth/
├── ViewModels/              # MVVM视图模型
│   └── LoginViewModel.cs      # 登录视图模型
├── Views/                   # WPF视图
│   └── LoginView.xaml         # 登录界面
└── AuthModule.cs            # Prism模块定义与注册
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc**: 用于模块化、依赖注入和区域导航。
- **LYBT.Desktop.Core**: 提供ViewModel基类和通用服务。
- **LYBT.Desktop.Services**: 提供 `IAuthService` 等业务服务的实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Auth\LYBT.Desktop.Auth.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IAuthService` 接口，并调用该服务来完成所有认证授权相关的业务操作。

```csharp
// LoginViewModel.cs
public class LoginViewModel : CoreViewModel
{
    private readonly IAuthService _authService;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new DelegateCommand(async () => await ExecuteLogin());
    }

    private async Task ExecuteLogin()
    {
        var request = new LoginRequest { Username = Username, Password = Password };
        var result = await _authService.LoginAsync(request);
        if(result.IsSuccess)
        {
            // 登录成功，发布用户登录事件
            EventAggregator.GetEvent<UserLoggedInEvent>().Publish();
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*