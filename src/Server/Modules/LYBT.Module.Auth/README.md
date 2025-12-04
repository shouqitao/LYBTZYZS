# LYBT.Module.Auth

> 身份认证与授权 | 传统三层 | JWT + RefreshToken

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: IUserService(查询用户信息)

## 目录结构

```
LYBT.Module.Auth/
├── AuthModule.cs
├── Interfaces/
│   └── IJwtService.cs
├── Services/
│   ├── AuthService.cs
│   └── JwtService.cs
└── Models/
    └── (配置模型)
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IAuthService | 9 | 登录/登出/刷新令牌/会话管理 |
| IJwtService | 3 | JWT生成/验证/密钥强度检查 |

## 安全特性

| 特性 | 说明 |
|------|------|
| 双轨认证 | 超级管理员(AdminSecrets表) + 普通用户(Users表) |
| JWT | AccessToken 2小时有效期 |
| RefreshToken | 7天有效期，支持撤销 |
| 密码加密 | BCrypt(工作因子12) |

## 依赖关系

### 依赖
- LYBT.Infrastructure (AppDbContext)
- LYBT.Entities (User, AdminSecret, AuthSession)
- LYBT.Shared.Models (LoginRequest, LoginResponse等)
- LYBT.Module.Users (用户数据访问)

### 被依赖
- LYBT.WebAPI (AuthController)
- 所有需要认证的模块

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/auth/login | POST | 用户登录(双轨认证) |
| /api/auth/logout | POST | 用户登出 |
| /api/auth/refresh-token | POST | 刷新访问令牌 |
| /api/auth/revoke-token | POST | 撤销RefreshToken |
| /api/auth/validate-token | POST | 验证JWT有效性 |
| /api/auth/session-info | GET | 获取会话信息 |
| /api/auth/change-password | POST | 修改密码 |
| /api/auth/verify | GET | 心跳检查 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
