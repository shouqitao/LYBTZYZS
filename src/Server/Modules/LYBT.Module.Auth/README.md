# LYBT.Module.Auth - 身份认证与授权模块

## 🎯 项目概述

**身份认证与授权模块 (Auth Module)** 是系统的核心安全模块，提供JWT无状态认证、RBAC角色权限控制和完整的安全审计功能。专为小型中医诊所场景优化，支持Admin/Doctor双角色管理。

## 📦 项目结构

```
LYBT.Module.Auth/
├── Services/                  # 业务逻辑实现
│   ├── AuthService.cs         # 主服务 (实现IAuthService)
│   └── JwtAuthenticationService.cs # JWT生成与验证服务
├── AuthModule.cs              # 模块依赖注入注册
└── README.md                  # 模块文档
```

## 🛠 技术栈

- **.NET 8 & ASP.NET Core**: 基础框架。
- **JWT (JSON Web Tokens)**: 用于生成和验证无状态的认证令牌。
- **BCrypt.Net**: 用于密码的哈希处理和验证。
- **Entity Framework Core**: 通过仓储模式间接使用，用于访问用户信息和会话。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.Auth\LYBT.Module.Auth.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `AuthController` 对外暴露。

- **API路由前缀**: `/api/v1/auth`

### 关键端点
- `POST /login`: 用户登录，成功后返回JWT。
- `POST /register`: (管理员权限) 注册新用户。
- `POST /logout`: 用户登出，可用于使令牌失效。

---

*（详细的内部架构、安全设计、DTO等信息请参考本文档后续章节。）*