# Users 模块设计

> 复杂度: 5/10 | 状态: 草稿

## 概述

Users 管理系统用户，支持四级角色权限、批量操作、sysadmin 保护。

**职责**: 用户 CRUD/搜索/批量操作、角色管理、密码重置/修改、Sysadmin 账户保护。

**依赖**: 上游无（基础模块），下游 Auth+所有业务模块。使用 ASP.NET Core Identity。

## API 端点

| HTTP | 路由 | 权限 |
|------|------|------|
| GET | `/api/v1/users` | AdminOrSuperAdmin |
| GET | `/api/v1/users/current` | 任何已认证 |
| POST | `/api/v1/users` | AdminOrSuperAdmin |
| POST | `/api/v1/users/{id}/reset-password` | AdminOrSuperAdmin |
| PUT | `/api/v1/users/{id}/change-password` | 任何已认证(仅自己) |
| POST | `/api/v1/users/batch-delete` | AdminOrSuperAdmin |

## 角色层级

```
SuperAdmin (100) ── 可管理所有人
    └── Admin (10) ── 可管理 Doctor + Receptionist
        └── Doctor (1) / Receptionist (0) ── 不可管理他人
```

**Sysadmin 特殊规则**: `IsSysAdmin=true` 标记，非角色。不可删除/禁用/修改（Controller 多处守卫）。

## 业务规则

| 规则 | 描述 |
|------|------|
| Sysadmin 保护 | 不可删/禁/改 |
| 角色层级 | SuperAdmin→全部, Admin→Doctor+Receptionist |
| 保留用户名 | admin/sysadmin/root 等不可注册 |
| 强制改密 | MustChangeOnNextLogin 标记 |

## 已知问题

- 无独立 Service 层，Controller 直接使用 Identity（违反三层架构）
- GetList 服务端 LINQ-to-Objects 过滤（性能隐患）
- 610 vs 599 行 Controller 重复（WebAPI/LocalWebAPI）
