# 用户模块重构 — 设计规格

> 日期: 2026-06-18

## [S1] 问题

双轨用户模型：`User`(业务实体) 与 `ApplicationUser`(Identity) 字段重复。Server `UserService`(Module.Users) 是死代码。Desktop `UserService` 的密码功能是占位实现。

## [S2] 方案

统一到标准 ASP.NET Core Identity：
- **保留** ApplicationUser + Identity 体系（两端共享）
- **删除** User 实体 + Server UserService/UserRepository/UserMapper
- **删除** Desktop UserService（占位实现）
- **保留** DTO 契约 + UI 控件
- **双模式差异**仅在 IdentityOptions 配置层

## [S3] 不在范围

- Desktop UI 改动（UI 不变）
- 新增功能
- 测试重写（删除死代码相关的测试）
