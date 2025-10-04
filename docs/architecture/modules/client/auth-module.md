# 客户端Auth模块设计文档

> ⚠️ **当前状态警告**：本文档描述的是设计目标，实际代码仅完成基础框架。大部分功能标记为"设计完成📋"或"待实现⚠️"，不应误解为已完成功能。

## 文档信息
- **创建时间**: 2025-09-27
- **模块名称**: LYBT.Desktop.Auth
- **模块版本**: 1.0.0-基础框架
- **架构标准**: 模块化双层架构
- **技术栈**: WPF + Prism.DryIoc + MVVM
- **实际状态**: 基础框架已搭建，核心业务逻辑待实现

## 1. 模块概述

### 1.1 模块定位
客户端Auth模块是凌隐宝堂中医诊所管理系统的安全核心组件，负责用户身份认证、JWT令牌管理和会话状态维护。该模块采用WPF+Prism架构，实现了完整的MVVM模式框架。

### 1.2 核心功能状态

#### 1.2.1 设计完成📋
- **模块化双层架构设计**: 服务层分离设计完成
- **Prism模块化架构**: 模块注册和依赖注入框架
- **MVVM基础框架**: ViewModel基类和View结构
- **API接口设计**: IAuthApi接口定义完成

#### 1.2.2 基础实现✅
- **AuthenticationModule**: Prism模块注册完成
- **服务层框架**: AuthService、AuthQueryService、AuthBusinessService基础类
- **LoginViewModel**: 继承ModernViewModelBase的空壳类
- **LoginView/LoginWindow**: XAML界面框架

#### 1.2.3 待实现⚠️
- **用户登录认证**: 登录界面业务逻辑待实现
- **JWT令牌管理**: 令牌获取、存储和更新逻辑待实现
- **会话状态管理**: 用户登录状态和会话信息管理待实现
- **密码管理**: 密码修改功能待实现
- **API连接检查**: 后端服务连接状态监控待实现
- **安全登出**: 登出流程和状态清理待实现

### 1.3 技术特性（已实现）
- **模块化设计**: 基于Prism模块化框架，支持按需加载
- **依赖注入**: 使用DryIoc容器实现控制反转
- **接口驱动**: 通过接口抽象实现松耦合架构

### 1.4 架构约束
- 严格遵循模块化双层架构：QueryService（查询）+ BusinessService（业务）
- 禁止使用ServiceLocator反模式，强制使用构造函数注入
- 所有ViewModel必须继承ModernViewModelBase基类
- API调用统一通过IAuthApi接口实现

## 2. 当前架构实现

### 2.1 实际文件结构

```
LYBT.Desktop.Auth/
├── AuthenticationModule.cs          ✅ 已实现
├── Interfaces/                      📋 设计完成
├── Mappings/                        📋 设计完成
├── Models/                          📋 设计完成
├── Services/                        ⚠️ 框架已搭建，业务逻辑待实现
│   ├── AuthService.cs              ✅ 委托层框架完成
│   ├── AuthQueryService.cs         ⚠️ 查询逻辑待实现
│   ├── AuthBusinessService.cs      ⚠️ 业务逻辑待实现
│   └── AuthServiceAdapter.cs       📋 适配器设计完成
├── ViewModels/                      ⚠️ 基础框架，业务逻辑待实现
│   └── LoginViewModel.cs           ⚠️ 空壳类，继承基类但无业务逻辑
└── Views/                           ✅ 界面框架已搭建
    ├── LoginView.xaml              📋 界面设计完成
    ├── LoginView.xaml.cs           📋 代码后台框架
    ├── LoginWindow.xaml            📋 独立窗口设计完成
    └── LoginWindow.xaml.cs         📋 代码后台框架
```

### 2.2 当前模块注册（实际代码）

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 模块化架构：注册模块业务服务
    containerRegistry.RegisterSingleton<AuthService>();

    // 注册视图模型
    containerRegistry.Register<LoginViewModel>();

    // 注册视图用于导航
    containerRegistry.RegisterForNavigation<LoginView>();
}
```

### 2.3 当前LoginViewModel（实际代码）

```csharp
/// <summary>
/// 登录视图模型 - 架构重构后简化版本
/// TODO: 重构完成后重新实现业务逻辑
/// </summary>
public class LoginViewModel : ModernViewModelBase
{
    public LoginViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IErrorHandlingService errorHandlingService)
        : base(eventAggregator, loggerFactory, errorHandlingService)
    {
    }
}
```

## 3. 待实现功能规划

### 3.1 高优先级⚠️

1. **LoginViewModel业务逻辑实现**
   - 用户名/密码绑定属性
   - 登录命令实现
   - 输入验证逻辑
   - 状态管理集成

2. **AuthBusinessService业务逻辑**
   - 登录API调用
   - JWT令牌处理
   - 会话状态更新
   - 错误处理机制

3. **基础UI交互**
   - 登录按钮事件处理
   - 加载状态指示
   - 错误消息显示

### 3.2 中优先级📋

1. **会话状态管理**
   - ISessionManager集成
   - 全局状态事件
   - 跨模块通信

2. **安全功能**
   - 密码安全处理
   - 记住我功能
   - 自动登录

3. **网络连接管理**
   - API连接检查
   - 重试机制
   - 离线模式支持

### 3.3 低优先级📋

1. **高级UI功能**
   - 动画效果
   - 主题系统集成
   - 多语言支持

2. **性能优化**
   - 异步操作优化
   - 内存管理
   - 启动速度优化

## 4. 技术债务

### 4.1 当前问题
- **LoginViewModel空实现**: 仅有构造函数，无任何业务逻辑
- **服务层未实现**: AuthQueryService和AuthBusinessService仅有接口和基础结构
- **TODO标记过多**: 代码中存在大量TODO注释需要处理
- **缺少单元测试**: 整个模块没有测试覆盖

### 4.2 实现计划

#### 4.2.1 第一阶段（核心功能）
1. 实现LoginViewModel基础登录逻辑
2. 实现AuthBusinessService登录API调用
3. 添加基础错误处理和状态管理

#### 4.2.2 第二阶段（完善功能）
1. 实现会话状态管理
2. 添加安全功能和用户体验优化
3. 完善错误处理和网络管理

#### 4.2.3 第三阶段（测试和优化）
1. 添加单元测试覆盖
2. 性能优化和稳定性提升
3. 文档更新和代码清理

## 5. 接口设计（已完成📋）

### 5.1 模块化双层架构接口

```csharp
// 委托层接口 - 统一服务入口
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request);
    // ...其他业务方法
}

// 查询专业层接口 - 只读查询操作
public interface IAuthQueryService
{
    bool IsLoggedIn { get; }
    Task<ServiceResult<UserDto?>> GetCurrentUser();
    Task<ServiceResult<bool>> CheckConnectionAsync();
}

// 业务逻辑层接口 - 状态修改操作
public interface IAuthBusinessService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request);
}
```

### 5.2 适配器接口设计

```csharp
// UI层认证接口 - 通过适配器实现
public interface IAuthenticationService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync();
    bool IsLoggedIn { get; }
    Task<UserDto?> GetCurrentUserAsync();
}
```

## 6. 实际项目配置

### 6.1 项目依赖（.csproj）
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Prism.DryIoc" Version="8.1.97" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\..." />
    <ProjectReference Include="..\..\Core\..." />
  </ItemGroup>
</Project>
```

## 7. 状态总结

### 7.1 完成度评估
- **架构设计**: 90% 完成 ✅
- **基础框架**: 70% 完成 ✅
- **业务逻辑**: 10% 完成 ⚠️
- **UI实现**: 30% 完成 ⚠️
- **测试覆盖**: 0% 完成 ⚠️

### 7.2 后续工作重点
1. **立即需要**: 实现LoginViewModel和AuthBusinessService的核心登录逻辑
2. **短期目标**: 完成基础的用户认证流程，确保系统可以正常登录
3. **中期目标**: 添加会话管理、安全功能和错误处理
4. **长期目标**: 性能优化、测试覆盖和高级功能实现

---

*文档版本: 2.0 - 真实状态反映版*  
*最后更新: 2025-09-28*  
*状态: 基础框架已搭建，核心业务逻辑待实现*