# 🔐 认证模块 (LYBT.Desktop.Auth)

## 📋 模块概述

认证模块是LYBTZYZS系统的安全基础，提供完整的用户认证和授权功能，采用UltraThink双层架构设计。

**架构状态**: ✅ **UltraThink双层架构完成** (2025-09-02)
**编译状态**: ✅ **零编译警告零错误** 
**重构成果**: 140个编译错误 → 0错误 (100%成功)

## 🏗️ 架构设计

### UltraThink双层架构
```
AuthModule (纯委托层)
    ├── AuthQueryService (查询专业层)
    └── AuthBusinessService (业务逻辑层)
```

### 核心组件

#### 1. AuthModule (主服务入口)
- **文件**: `Services/AuthModule.cs`
- **职责**: 纯委托模式统一入口
- **特点**: 无业务逻辑，请求路由分发

#### 2. AuthQueryService (查询服务)
- **文件**: `Services/AuthQueryService.cs`
- **职责**: 用户查询、会话查询、权限查询
- **特点**: 专注查询性能，不涉及数据修改

#### 3. AuthBusinessService (业务服务)
- **文件**: `Services/AuthBusinessService.cs`  
- **职责**: 登录验证、令牌管理、会话管理
- **特点**: 完整业务场景处理

## 🎯 核心功能

### 用户认证
- **登录验证**: 用户名密码验证
- **JWT令牌**: 安全令牌生成和验证
- **记住登录**: Remember Me功能支持
- **会话管理**: 登录会话跟踪

### 权限控制
- **角色权限**: Admin/Doctor角色区分
- **RBAC模式**: 基于角色的访问控制
- **权限检查**: 功能级别权限验证

### 安全特性
- **密码安全**: BCrypt哈希加密
- **令牌过期**: 8小时标准过期策略
- **安全审计**: 登录日志和操作记录

## 📁 文件结构

```
Auth/
├── Services/
│   ├── AuthModule.cs                 # 主服务入口 (纯委托)
│   ├── AuthQueryService.cs           # 查询服务
│   └── AuthBusinessService.cs        # 业务服务  
├── Interfaces/
│   ├── IAuthModule.cs               # 主服务接口
│   ├── IAuthQueryService.cs         # 查询服务接口
│   └── IAuthBusinessService.cs      # 业务服务接口
├── ViewModels/
│   └── LoginViewModel.cs            # 登录视图模型
├── Views/
│   ├── LoginView.xaml               # 登录界面
│   └── LoginWindow.xaml             # 登录窗口
└── AuthModule.cs                    # Prism模块注册
```

## 🔧 接口定义

### IAuthBusinessService (业务接口)
```csharp
public interface IAuthBusinessService
{
    // 核心认证功能
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync();
}
```

### IAuthQueryService (查询接口)  
```csharp
public interface IAuthQueryService
{
    // 用户状态查询
    Task<ServiceResult<bool>> IsUserLoggedInAsync();
    Task<ServiceResult<UserDto>> GetCurrentUserAsync();
    Task<ServiceResult<List<PermissionDto>>> GetUserPermissionsAsync();
}
```

## 🎨 视图组件

### LoginView (登录界面)
- **功能**: 用户名密码输入、记住登录选项
- **绑定**: LoginViewModel数据绑定
- **样式**: 现代化Material Design风格

### LoginWindow (登录窗口)
- **功能**: 独立登录窗口容器
- **特性**: 模态对话框、自适应布局
- **集成**: 与主应用Shell集成

## 🔄 业务流程

### 登录流程
1. 用户输入凭据
2. AuthBusinessService验证
3. 生成JWT令牌
4. 创建用户会话
5. 更新UI状态

### 权限检查流程
1. 获取当前用户令牌
2. 解析用户角色信息
3. 检查功能权限映射
4. 返回权限验证结果

## 📊 重构成果

### 重构前 (传统模式)
- **编译错误**: 140个
- **代码行数**: 600+ 行
- **架构复杂度**: 多层Helper类混乱

### 重构后 (UltraThink双层)
- **编译错误**: 0个 ✅
- **代码行数**: 200+ 行
- **架构简化**: 67%代码精简
- **维护性**: 大幅提升

## 🚀 使用示例

### 服务注册 (AuthModule.cs)
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // UltraThink双层架构服务注册
    containerRegistry.RegisterSingleton<IAuthQueryService, AuthQueryService>();
    containerRegistry.RegisterSingleton<IAuthBusinessService, AuthBusinessService>();
    
    // 主服务注册 (纯委托模式)
    containerRegistry.RegisterSingleton<AuthModule>();
    containerRegistry.RegisterSingleton<IAuthModule>(container => 
        container.Resolve<AuthModule>());
}
```

### 业务调用示例
```csharp
public class LoginViewModel
{
    private readonly IAuthModule _authModule;
    
    public async Task LoginAsync()
    {
        var request = new LoginRequest 
        { 
            Username = Username, 
            Password = Password 
        };
        
        var result = await _authModule.LoginAsync(request);
        if (result.IsSuccess)
        {
            // 登录成功，跳转主界面
            NavigateToMain();
        }
    }
}
```

## 🔧 依赖关系

### NuGet依赖
- `Prism.DryIoc` 9.0.537 - MVVM框架
- `System.IdentityModel.Tokens.Jwt` - JWT令牌处理
- `BCrypt.Net-Next` - 密码哈希加密

### 项目依赖
- `LYBT.Shared.Models` - 数据传输对象
- `LYBT.Shared.Interfaces` - 服务接口定义
- `LYBT.Desktop.Core` - 核心基础设施

## 🎯 开发指南

### 添加新认证功能
1. 在 `IAuthBusinessService` 中定义接口
2. 在 `AuthBusinessService` 中实现逻辑
3. 在 `AuthModule` 中添加委托调用
4. 更新相关ViewModels和Views

### 最佳实践
- 遵循UltraThink双层架构模式
- 保持主Module纯委托特性
- 使用ServiceResult统一返回格式
- 记录安全相关操作日志

## 📝 变更日志

### v2.0.0 (2025-09-02)
- 🏆 **UltraThink双层架构重构完成**
- ✅ **140个编译错误全部修复**
- 🧹 **移除所有CoreService层冗余代码**
- ⚡ **实现67%代码精简**

### v1.0.0 (历史版本)
- 基础认证功能实现
- JWT令牌集成
- 用户会话管理

---

**Auth模块** - LYBTZYZS系统安全基石 🔐