---
type: concept
title: 认证与 Token 安全
tags: [concept, security, auth, jwt]
created: 2026-06-10
updated: 2026-06-10
source: docs/03-architecture/decisions/0005-superadmin-auth-module.md
---

## 概述

系统采用 **JWT Bearer Token** 作为认证机制，SuperAdmin（超级管理员）与普通用户分离存储和路由。Token 安全机制包含 **Token Family**、重放攻击检测、**DPAPI 加密**（Desktop 端凭据）和滑动过期策略。尽管当前部署规模仅 3-5 人，这些"防御性设计"为未来扩展和多诊所云部署预留能力。

## 核心内容

### JWT 认证架构

```
请求 → Auth Middleware → Controller
           │
           ├─ 验证 JWT 签名 (HS256)
           ├─ 检查过期时间 (exp)
           ├─ 提取 UserId / Role / Jti
           └─ 设置 HttpContext.User
```

| 组件 | 职责 |
|------|------|
| AuthService | 登录验证、JWT 生成、RefreshToken 轮换 |
| ITokenRevocationService | Token 撤销接口 |
| AdminSecrets 表 | SuperAdmin 凭据独立存储（不在 Users 表） |
| RefreshTokens 表 | 刷新令牌、FamilyId、使用次数追踪 |

### SuperAdmin 分离

SuperAdmin 是系统初始化专用账户，与普通用户有本质区别：

| 维度 | SuperAdmin | 普通用户 (Admin/Doctor/Receptionist) |
|------|-----------|-------------------------------------|
| 存储位置 | AdminSecrets 表 | Users 表 |
| UserType 字段 | "SuperAdmin" | "User" |
| 认证路由 | 专属验证逻辑 | 标准 JWT 验证 |
| 管理方式 | 不参与 CRUD | 通过 Users 模块管理 |

### Token Family 与重放检测

**Token Family 机制** — 每次刷新 Token 时生成新的 `FamilyId` 并关联到同一族：

```
初始登录 → FamilyId = guid_1 → Token_A
Token_A 刷新 → FamilyId = guid_1 → Token_B (标记 Token_A.IsUsed=true)
Token_B 刷新 → FamilyId = guid_1 → Token_C (标记 Token_B.IsUsed=true)
```

**重放攻击检测** — 若检测到已使用的 Token (`IsUsed=true`) 再次提交刷新请求，系统判定为重放攻击：

1. 撤销该 Family 下所有未过期 Token
2. 记录安全审计日志 (SecurityAuditLog)
3. 强制用户重新登录

### Desktop 端 DPAPI 加密

Desktop 客户端使用 Windows **DPAPI** (Data Protection API) 加密本地存储的凭据：

```csharp
// 加密
var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

// 解密
var decrypted = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
```

确保即使磁盘文件被窃取，Token 也无法在其他机器上解密。

### Token 生命周期

| 阶段 | 操作 | DB 写入 |
|------|------|---------|
| 登录 | 生成 JWT + RefreshToken，插入 RefreshTokens 表 | 1 次 INSERT |
| 刷新 | 标记旧 Token IsUsed=true，创建新 Token | 1 次 UPDATE + 1 次 INSERT |
| 登出 | 撤销当前会话所有 Token | N 次 UPDATE |
| 清理 | 定时任务删除过期 Token | 批量 DELETE |

**滑动刷新** — RefreshToken 有效期内每次使用自动续期；超过 `InactiveTimeout` 未活动则强制重新登录。

### 安全措施汇总

| 措施 | 目的 | 实现 |
|------|------|------|
| BCrypt 密码哈希 | 防止明文泄露 | `BCrypt.Net-Next` 库 |
| TokenHash 存储 | AuthSession 不存明文 Token | SHA256 哈希 |
| FamilyId 追踪 | 精确撤销整族 Token | GUID 族标识 |
| IsUsed 标记 | 防止 Token 重复使用 | 布尔标志 |
| UsageCount 计数 | 监控异常使用频率 | 整数计数器 |
| DPAPI 加密 | Desktop 本地凭据保护 | Windows 原生 API |
| 单会话登录 | 防止多设备并发 | LastLoginTime + ExpiryTime 校验 |

## 相关链接

- [[auth-module]] — Auth 模块整体设计
- [[user]] — 用户实体与角色体系
- [[ADR-005-superadmin-auth]] — SuperAdmin 归属 Auth 模块的决策
- [[ADR-008-token-security]] — Token 安全防御性设计的详细论证
- [[ADR-004-user-context-propagation]] — 用户上下文在调用链中的传递
