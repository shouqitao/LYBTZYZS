---
type: module
title: 认证与会话管理模块
tags: [module, auth, security, jwt]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/02-auth.md
---

# 认证与会话管理模块

## 概述

认证模块采用 JWT Bearer Token 机制，为中医诊所管理系统提供完整的身份验证生命周期管理。该模块通过自动登录、滑动刷新、重放攻击检测和不活跃超时等机制，在保障患者数据安全的同时，最小化医生登录摩擦，确保诊疗连续性。

模块支持远程模式 (JWT Token + Server WebAPI) 和本地模式 (LocalAuthService 密码验证) 双模式运行，适应诊所不同网络环境下的部署需求。

## 核心能力

| 能力 | 说明 | 技术实现 |
|------|------|----------|
| 密码登录 | 用户名 + 密码获取 JWT AccessToken + RefreshToken | POST `/api/v1/auth/login`，限流 5次/60秒 |
| 自动登录 | AutoLoginToken (DPAPI 加密 + HMAC-SHA256 校验) 实现免密启动 | `%LOCALAPPDATA%/LYBT/credentials.dat` |
| 滑动刷新 | AccessToken 剩余 < 5分钟时自动刷新，用户无感知 | POST `/api/v1/auth/refresh`，SemaphoreSlim 互斥锁 |
| 重放攻击检测 | RefreshToken 重复使用时触发整个 Token Family 撤销 | FamilyId 追踪，ErrorCode=TokenRevoked |
| 不活跃超时 | 15分钟无键盘/鼠标操作后静默登出 | InactivityTimer，跳转登录页 |
| 单会话登录 | 同一账号仅允许一台设备登录，新设备登录踢出旧设备 | 登录时撤销该用户所有现有 Token Family |
| Token 验证 | 操作前确认用户会话有效性 | GET `/api/v1/auth/validate` |
| 凭证安全存储 | DPAPI LocalMachine 加密 + HMAC 完整性校验 | 不绑定 Windows 用户账号，适合诊所共用电脑 |

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 登录/登出/Token 刷新/会话超时延长 |
| Admin | 登录/登出/Token 刷新/会话超时延长 |
| Doctor | 登录/登出/Token 刷新/会话超时延长 |
| Receptionist | 登录/登出/Token 刷新/会话超时延长 |

> 认证操作本身不区分角色，所有已注册用户均可使用。角色权限在具体业务功能中通过 `[Authorize(Roles = "...")]` 控制。

## 关键业务规则

### Token 生命周期

| 参数 | 默认值 | 说明 |
|------|--------|------|
| AccessToken 有效期 | 30 分钟 | `Jwt:AccessTokenExpirationMinutes` |
| RefreshToken 有效期 | 7 天 | `Jwt:RefreshTokenExpirationDays` |
| 绝对会话期限 | 30 天 | 超过此期限必须重新登录 |
| 不活跃超时 | 15 分钟 | `Session:InactivityTimeoutMinutes` |
| 刷新阈值 | 5 分钟 | AccessToken 剩余 < 5分钟时触发刷新 |
| 登录限流 | 5次/60秒 | 防止暴力破解 |

### 认证流程

```
应用启动 → 检查 AutoLoginToken → [有] 自动登录 → 成功 → 进入工作台
                                                → 失败 → 手动登录
                               → [无] 手动登录 → 输入用户名密码 → 验证 → 进入工作台
工作中 → AccessToken 即将过期 → 滑动刷新 (用户无感) → 继续工作
      → 不活跃 15 分钟 → 静默登出 → 跳转登录页
      → 其他设备登录 → Token Family 撤销 → 强制登出
```

### Token 刷新失败分级处理

| 失败类型 | 处理策略 |
|----------|----------|
| 网络错误/5xx | 指数退避重试 (1s→2s→4s，最多 3 次) |
| 401 RefreshTokenExpired (10204) | 尝试 AutoLogin → 失败则跳转登录页 |
| 401 TokenRevoked (10203) | 立即清除所有本地 Token → 显示 "您的账号已在其他设备登录" → 跳转登录页 |
| 200 OK | 替换 Token → 继续请求 |

### 凭证存储安全

- **写入**: AutoLoginToken → DPAPI Protect (LocalMachine + entropy) → HMAC-SHA256 签名 → 写入 `credentials.dat`
- **读取**: 读取文件 → 验证 HMAC → DPAPI Unprotect → POST `/auth/auto-login`
- **旧格式迁移**: 检测到无 HMAC 的旧格式凭据 → 登录成功后自动迁移到新格式
- **HMAC 不匹配**: 删除文件 + 记录安全警告日志 → 回退手动登录

### 状态机

| 状态转换 | 触发条件 |
|----------|----------|
| Idle → Validating | 开始登录 (手动或自动) |
| Validating → Active | 登录成功 |
| Validating → Idle | 登录失败 |
| Active → Refreshing | AccessToken 即将过期 |
| Refreshing → Active | Token 刷新成功 |
| 任意状态 → Idle | 用户登出或不活跃超时 |

## 相关链接

- user - 用户管理模块
- [authentication](../07-authentication.md) - 认证架构总览
- ADR-005-superadmin-auth - SuperAdmin 认证决策
- ADR-008-token-security - Token 安全决策
