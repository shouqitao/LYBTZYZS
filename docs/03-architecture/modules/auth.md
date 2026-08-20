# Auth 模块设计
> 版本: v1.0 | 日期: 2026-08-20

> 复杂度: 4/10 | 状态: 草稿

## 概述

Auth 管理认证授权，支持远程/本地双模式。远程使用完整 JWT + Refresh Token，本地使用简化 JWT（1年有效期）。

**职责**: 用户登录/登出、JWT Token 生成验证、Refresh Token 旋转撤销、Token 生命周期管理。

**依赖**: 上游 Users（用户信息），下游所有需认证模块。

## API 端点

| HTTP | 路由 | 权限 |
|------|------|------|
| POST | `/api/v1/auth/login` | 匿名 |
| POST | `/api/v1/auth/refresh` | 匿名 |
| POST | `/api/v1/auth/logout` | 匿名 |
| POST | `/api/v1/auth/validate` | 已认证 |

## 双模式认证流程

**远程**: Login → UserManager.FindByNameAsync → CheckPasswordSignIn（失败计数/锁定）→ JWT AccessToken(30min) + RefreshToken(7天) → TokenRefreshHandler 自动刷新

**本地**: 简化验证（无锁定/2FA）→ AccessToken 1年有效期 → 无 RefreshToken

> ⚠️ 本地模式安全风险：Token 1年有效，无撤销机制；/refresh 无签名校验。

## Token 生命周期

```
Active → (过期前5min) → Warning → (过期) → Expired
  ↑ 刷新成功则重置为 Active
```

Desktop: `TokenLifecycleService`(定时监控) + `TokenRefreshHandler`(自动重试) + `TokenManager`(线程安全内存存储)

## 业务规则

| 规则 | 描述 |
|------|------|
| 密码策略 | BCrypt work factor 11，自动重哈希 |
| 账户锁定 | 5次失败锁定15分钟 |
| Token 有效期 | AccessToken 30min，RefreshToken 7天 |
| 本地 Token | 1年有效期，无刷新 |
| Token 族旋转 | Refresh Token 使用后废弃旧 Token |

## 已知问题

1. AuthService（227行）是死代码 — Controller 直接用 UserManager/SignInManager
2. 本地模式 /refresh 无签名校验
3. Token 族旋转待实现
4. 审计日志缺失（登录/登出无记录）
