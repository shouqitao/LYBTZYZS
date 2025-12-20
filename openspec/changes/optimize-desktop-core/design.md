# Design: optimize-desktop-core

## 当前架构分析

### 项目结构现状

```
src/Client/Desktop/Core/
├── LYBT.Desktop.Contracts/     # 接口定义
│   ├── Api/                   # 6个API接口
│   ├── Services/              # 17个服务接口
│   ├── Components/            # 组件接口
│   └── Models/                # 模型定义
│
├── LYBT.Desktop.Foundation/   # 基础设施（内容过多）
│   ├── Application/           # 2文件
│   ├── Caching/              # 1文件
│   ├── Configuration/        # 配置
│   ├── Extensions/           # 扩展方法
│   ├── Handlers/             # 处理器
│   ├── HealthCheck/          # 2文件
│   ├── Helpers/              # 辅助类
│   ├── Http/                 # 4文件
│   ├── Modules/              # 2文件
│   ├── Performance/          # 2文件
│   ├── Security/             # 16文件 ← 过度设计
│   └── Settings/             # 设置
│
├── LYBT.Desktop.Infrastructure/ # UI基础设施（杂货铺）
│   ├── Behaviors/            # 行为
│   ├── Commands/             # 命令
│   ├── Configuration/        # 配置
│   ├── Constants/            # 常量
│   ├── Controls/             # 18+控件 ← 应独立
│   ├── Converters/           # 17转换器 ← 应独立
│   ├── DependencyInjection/  # DI
│   ├── Events/               # 15事件
│   ├── Extensions/           # 扩展
│   ├── Helpers/              # 辅助
│   ├── Http/                 # 2文件 ← 重复
│   ├── Interfaces/           # 接口
│   ├── Localization/         # 本地化
│   ├── Logging/              # 日志
│   ├── Repositories/         # 仓储
│   ├── Security/             # 安全
│   ├── Services/             # 14+服务
│   ├── Templates/            # 模板
│   ├── Themes/               # 主题
│   └── Views/                # 视图
│
└── LYBT.Desktop.Models/       # ViewModel
    ├── Http/                 # HTTP模型
    ├── Items/                # 项目模型
    ├── Mappers/              # 映射器
    ├── Prescriptions/        # 处方模型
    └── ViewModels/Base/      # 6个基类
```

### 依赖关系现状

```
当前依赖链：

Contracts ────────────────────────────────────────┐
    ↑                                              │
Foundation ───────────────────────────────────────┼──→ Shared.Models
    ↑                                              │     Shared.Utilities
Infrastructure ───────────────────────────────────┼──→ Shared.ExceptionHandling
    ↑                                              │     Shared.Logging
Models ────────────────────────────────────────────┘     Shared.Components

问题：Models依赖Infrastructure（反模式）
```

### Token管理详细分析

**Security目录16个文件**:

| 文件 | 职责 | 问题 |
|-----|------|-----|
| IAuthenticationService | 认证接口 | 保留 |
| AuthenticationService | 认证实现 | 保留 |
| ITokenStorage | Token存储接口 | 与ITokenStorageService重复 |
| ITokenStorageService | Token存储服务接口 | 功能重叠 |
| SecureTokenStorage | 加密存储实现 | 底层实现 |
| TokenStorageService | 存储服务实现 | 与上面重复 |
| ITokenValidator | Token验证接口 | 保留 |
| LocalTokenValidator | 本地验证实现 | 保留 |
| ITokenLifecycleService | 生命周期接口 | 过度设计 |
| TokenLifecycleService | 生命周期实现 | 状态机过复杂 |
| TokenLifecycleState | 状态枚举 | 可内联 |
| TokenLifecycleStateChangedEvent | 状态变更事件 | 可简化 |
| ISecureCredentialStorage | 凭证存储接口 | 可合并 |
| SecureCredentialStorage | 凭证存储实现 | 保留 |
| IUsernameStorageService | 用户名存储接口 | 可合并到凭证存储 |
| UsernameStorageService | 用户名存储实现 | 可删除 |

**接口重复分析**:

```csharp
// ITokenStorage - 低级存储接口
public interface ITokenStorage
{
    Task SaveTokenAsync(LoginResponse loginResponse);
    Task<LoginResponse?> LoadTokenAsync();
    Task ClearTokenAsync();
}

// ITokenStorageService - 高级服务接口（功能重叠）
public interface ITokenStorageService
{
    Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe);
    Task<string?> GetTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<LoginResponse?> GetLoginResponseAsync();
    Task ClearAuthenticationAsync();
    Task<bool> IsTokenExpiredAsync();
}
```

两个接口本质上做同样的事，应合并为一个。

## 目标架构设计

### 项目结构

```
src/Client/Desktop/Core/
├── LYBT.Desktop.Contracts/     # 接口定义（保持）
│   ├── Api/
│   ├── Services/
│   └── Models/
│
├── LYBT.Desktop.Foundation/   # 基础设施（精简）
│   ├── Configuration/        # 配置管理
│   ├── Http/                 # HTTP处理（合并）
│   │   ├── ApiService.cs
│   │   ├── AuthorizationMessageHandler.cs
│   │   ├── TokenRefreshHandler.cs
│   │   ├── RetryPolicyExtensions.cs
│   │   ├── ProblemDetailsParser.cs    # 从Infrastructure移入
│   │   └── ProblemDetailsResponse.cs  # 从Infrastructure移入
│   ├── Security/             # Token管理（简化为8文件）
│   │   ├── IAuthenticationService.cs
│   │   ├── AuthenticationService.cs
│   │   ├── ITokenService.cs          # 合并接口
│   │   ├── TokenService.cs           # 合并实现
│   │   ├── ITokenValidator.cs
│   │   ├── LocalTokenValidator.cs
│   │   ├── ICredentialStorage.cs     # 合并凭证
│   │   └── SecureCredentialStorage.cs
│   ├── Caching/
│   ├── HealthCheck/
│   └── Performance/
│
├── LYBT.Desktop.Infrastructure/ # 服务实现（精简）
│   ├── Services/             # 业务服务
│   ├── Events/               # Prism事件
│   ├── DependencyInjection/  # DI配置
│   ├── Localization/         # 本地化
│   ├── Helpers/              # 辅助类
│   └── Extensions/           # 扩展方法
│
├── LYBT.Desktop.Controls/    # 新项目：UI组件库
│   ├── Controls/             # XAML控件
│   ├── Converters/           # 值转换器
│   ├── Templates/            # 控件模板
│   ├── Themes/               # 主题资源
│   └── Behaviors/            # 行为
│
└── LYBT.Desktop.Models/       # ViewModel（解耦）
    └── ViewModels/           # 仅依赖Contracts
```

### 依赖关系目标

```
目标依赖链：

                    ┌─────────────────────────────────────┐
                    │           Contracts                  │
                    │        (接口定义层)                   │
                    └─────────────────────────────────────┘
                                    ↑
                    ┌─────────────────────────────────────┐
                    │          Foundation                  │
                    │        (基础设施层)                   │
                    └─────────────────────────────────────┘
                                    ↑
                    ┌─────────────────────────────────────┐
                    │        Infrastructure                │
                    │        (服务实现层)                   │
                    └─────────────────────────────────────┘


┌─────────────────────────────────────┐
│            Controls                  │  ← 独立UI组件库
│          (UI组件层)                   │
└─────────────────────────────────────┘


┌─────────────────────────────────────┐
│             Models                   │  ← 仅依赖Contracts
│          (ViewModel层)               │──────→ Contracts
└─────────────────────────────────────┘
```

## 详细设计

### Phase 1: 简化Token管理

#### 新接口设计

```csharp
// ITokenService.cs - 统一Token服务接口
public interface ITokenService
{
    // 存储操作
    Task SaveAuthenticationAsync(LoginResponse loginResponse, bool persist = false);
    Task<LoginResponse?> GetLoginResponseAsync();
    Task ClearAuthenticationAsync();
    
    // Token访问
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    
    // 生命周期
    Task<bool> IsTokenExpiredAsync();
    Task<TimeSpan?> GetRemainingTimeAsync();
    TokenState CurrentState { get; }
    
    // 事件
    event EventHandler<TokenStateChangedEventArgs>? StateChanged;
}

public enum TokenState
{
    None,
    Valid,
    Expiring,
    Expired
}

public class TokenStateChangedEventArgs : EventArgs
{
    public TokenState OldState { get; }
    public TokenState NewState { get; }
}
```

#### 新实现设计

```csharp
// TokenService.cs - 统一实现
public class TokenService : ITokenService, IDisposable
{
    private readonly ISecureCredentialStorage _storage;
    private readonly ILogger<TokenService> _logger;
    private readonly Timer _monitorTimer;
    
    private LoginResponse? _cachedResponse;
    private TokenState _currentState = TokenState.None;
    
    // 实现所有接口方法...
}
```

#### 凭证存储接口设计

```csharp
// ICredentialStorage.cs - 统一凭证存储接口
public interface ICredentialStorage
{
    // Token存储
    Task SaveLoginResponseAsync(LoginResponse response);
    Task<LoginResponse?> LoadLoginResponseAsync();
    Task ClearLoginResponseAsync();
    
    // 用户名记忆
    Task SaveUsernameAsync(string username);
    Task<string?> LoadUsernameAsync();
    Task ClearUsernameAsync();
}
```

### Phase 2: Controls项目设计

#### 项目配置

```xml
<!-- LYBT.Desktop.Controls.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>LYBT.Desktop.Controls</RootNamespace>
  </PropertyGroup>
  
  <!-- 无业务依赖，仅WPF基础 -->
</Project>
```

#### 命名空间规划

```
LYBT.Desktop.Controls
├── Controls/          # LYBT.Desktop.Controls.Controls
├── Converters/        # LYBT.Desktop.Controls.Converters
├── Templates/         # LYBT.Desktop.Controls.Templates
├── Themes/            # LYBT.Desktop.Controls.Themes
└── Behaviors/         # LYBT.Desktop.Controls.Behaviors
```

### Phase 3: Models解耦设计

#### ViewModelBase重构

```csharp
// Before: 直接依赖Infrastructure
public abstract class ViewModelBase : BindableBase
{
    // 通过构造函数直接注入Infrastructure中的服务
    protected readonly IUserNotificationService _notificationService;
}

// After: 仅依赖Contracts中的接口
public abstract class ViewModelBase : BindableBase
{
    // 接口定义在Contracts中
    protected readonly IUserNotificationService _notificationService;
    // IUserNotificationService接口已在Contracts/Services/中
}
```

#### csproj变更

```xml
<!-- Before -->
<ItemGroup>
  <ProjectReference Include="..\LYBT.Desktop.Infrastructure\..." />
</ItemGroup>

<!-- After -->
<ItemGroup>
  <ProjectReference Include="..\LYBT.Desktop.Contracts\..." />
</ItemGroup>
```

### Phase 4: HTTP合并设计

#### 目录结构

```
Foundation/Http/
├── ApiService.cs                    # 保留
├── AuthorizationMessageHandler.cs   # 保留
├── TokenRefreshHandler.cs           # 保留
├── RetryPolicyExtensions.cs         # 保留
├── ProblemDetailsParser.cs          # 从Infrastructure移入
└── ProblemDetailsResponse.cs        # 从Infrastructure移入
```

#### 命名空间统一

```csharp
// 统一使用
namespace LYBT.Desktop.Foundation.Http;
```

## 迁移策略

### 编译顺序

1. Contracts（无变更）
2. Foundation（Phase 1 + Phase 4变更）
3. Controls（Phase 2新建）
4. Infrastructure（Phase 2 + Phase 4清理）
5. Models（Phase 3变更）

### 回归测试策略

每个Phase完成后执行:
1. 全量编译 `dotnet build LYBT.All.sln`
2. 单元测试 `dotnet test tests/UnitTests/`
3. 集成测试 `dotnet test tests/IntegrationTests/`
4. 冒烟测试 - 应用启动、登录、核心功能

## 文件变更清单

### 删除文件 (Phase 1)
- Foundation/Security/ITokenStorage.cs
- Foundation/Security/ITokenStorageService.cs
- Foundation/Security/SecureTokenStorage.cs
- Foundation/Security/TokenStorageService.cs
- Foundation/Security/ITokenLifecycleService.cs
- Foundation/Security/TokenLifecycleService.cs
- Foundation/Security/TokenLifecycleState.cs
- Foundation/Security/TokenLifecycleStateChangedEvent.cs
- Foundation/Security/IUsernameStorageService.cs
- Foundation/Security/UsernameStorageService.cs

### 新建文件 (Phase 1)
- Foundation/Security/ITokenService.cs
- Foundation/Security/TokenService.cs
- Foundation/Security/ICredentialStorage.cs

### 移动文件 (Phase 2)
- Infrastructure/Controls/* → Controls/Controls/
- Infrastructure/Converters/* → Controls/Converters/
- Infrastructure/Templates/* → Controls/Templates/
- Infrastructure/Themes/* → Controls/Themes/
- Infrastructure/Behaviors/* → Controls/Behaviors/

### 移动文件 (Phase 4)
- Infrastructure/Http/ProblemDetailsParser.cs → Foundation/Http/
- Infrastructure/Http/ProblemDetailsResponse.cs → Foundation/Http/

### 删除目录 (Phase 2 + 4)
- Infrastructure/Controls/
- Infrastructure/Converters/
- Infrastructure/Templates/
- Infrastructure/Themes/
- Infrastructure/Http/
