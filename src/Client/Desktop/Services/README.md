# LYBT.Desktop.Services

## 🎯 项目概述

LYBT.Desktop.Services是凌隐宝堂桌面客户端的业务服务层，提供用户认证、权限管理、对话框服务、错误处理等核心业务支持服务。该模块作为桌面端与后端API的中间层，负责实现 `Shared.Interfaces` 中定义的业务接口，协调业务逻辑，并为上层ViewModel提供服务。

**核心价值**:
- **业务逻辑封装**: 将认证、会话管理等通用业务逻辑集中处理。
- **服务接口实现**: 提供 `IUserService`, `IAuthService` 等核心业务接口的具体实现。
- **UI服务抽象**: 提供统一的对话框和通知服务，解耦ViewModel与具体UI框架。

## 📦 项目结构

```
src/Client/Desktop/Services/
├── ApiService.cs              # (已废弃) 统一API调用服务
├── UserSessionManager.cs      # 用户会话管理
├── PermissionService.cs       # 权限验证服务
├── CommonDialogService.cs     # 通用对话框
├── PrismDialogService.cs      # Prism对话框集成
├── CredentialService.cs       # 凭证管理
├── ErrorHandlingService.cs    # 错误处理服务
└── Handlers/                  # HTTP消息处理器
    └── AuthHeaderHandler.cs   # 自动附加认证头
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.Wpf**: 用于对话框服务 (`IDialogService`) 的集成。
- **AutoMapper**: 用于DTO和本地模型之间的转换。
- **Refit**: 间接使用，通过 `Infrastructure` 层提供的API客户端与后端通信。

## 🚀 快速开始

此项目是一个类库，不包含可执行文件。可以通过解决方案或以下命令进行构建：

```bash
# 还原解决方案依赖
dotnet restore LYBT.All.sln

# 构建此项目
dotnet build src\Client\Desktop\Services\LYBT.Desktop.Services.csproj
```

## 🔌 API 接口

此项目为桌面端业务服务层，不直接对外提供API接口。它的核心职责是**实现** `Shared.Interfaces` 中定义的业务服务接口。

### 接口实现示例 (`IAuthService`)

```csharp
public class AuthService : IAuthService
{
    private readonly IAuthApi _authApi; // 由Infrastructure层提供
    private readonly IUserSessionManager _sessionManager;

    public AuthService(IAuthApi authApi, IUserSessionManager sessionManager)
    {
        _authApi = authApi;
        _sessionManager = sessionManager;
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var apiResponse = await _authApi.LoginAsync(request);
        if (apiResponse.Success && apiResponse.Data != null)
        {            
            // 登录成功后，使用UserSessionManager管理会话状态
            await _sessionManager.StartSessionAsync(apiResponse.Data);
            return ServiceResult<LoginResponse>.Success(apiResponse.Data);
        }
        return ServiceResult<LoginResponse>.Failure(apiResponse.Message);
    }

    public async Task LogoutAsync()
    {
        await _sessionManager.EndSessionAsync();
    }
}
```