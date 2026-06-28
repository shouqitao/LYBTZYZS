# Users 模块设计

> v1.0 | 2026-06-28

## 模块概述

Users 管理系统用户：CRUD、四级角色(Receptionist/Doctor/Admin/SuperAdmin)、密码策略、批量操作。

**职责边界**：用户账户全生命周期（创建→管理→禁用/恢复）

## 接口契约

| 端点 | Service | 权限 | 关键功能 |
|------|---------|------|----------|
| GET /users | GetPagedAsync | AdminOrSuperAdmin | 分页查询 |
| GET /users/current | GetCurrentAsync | 已认证 | 当前用户资料 |
| POST /users | CreateAsync | AdminOrSuperAdmin | 用户创建 |
| PUT /users/{id} | UpdateAsync | AdminOrSuperAdmin | 用户更新 |
| DELETE /users/{id} | SoftDeleteAsync | AdminOrSuperAdmin | 用户删除 |
| POST /users/{id}/reset-password | ResetPasswordAsync | AdminOrSuperAdmin | 密码重置 |
| PUT /users/{id}/profile | UpdateProfileAsync | 已认证 | 个人资料(IDOR防护) |
| PUT /users/{id}/change-password | ChangePasswordAsync | 已认证 | 修改密码 |
| POST /users/{id}/toggle-status | ToggleStatusAsync | AdminOrSuperAdmin | 启用/禁用 |
| POST /users/{id}/restore | RestoreAsync | AdminOrSuperAdmin | 恢复软删除 |

## 关键业务规则

| 规则 | 约束 | US |
|------|------|-----|
| 四级权限 | Receptionist(0) < Doctor(1) < Admin(10) < SuperAdmin(100) | US-USER-001 |
| Sysadmin保护 | 禁止删除/禁用/修改sysadmin | ADR-0005 |
| 密码策略 | BCrypt WorkFactor=12，首登强制改密 | US-USER-009 |
| 批量操作 | 批量删除/启用/禁用(最多100条) | US-USER-012 |
| IDOR防护 | 仅修改自己资料(PUT /profile) | US-USER-008 |
