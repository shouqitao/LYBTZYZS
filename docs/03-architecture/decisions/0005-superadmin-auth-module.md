# ADR-0005: SuperAdmin 归属 Auth 模块

**状态**: 已采纳
**日期**: 2025-12-01
**来源**: ADR-010, ADR-011

## 背景

SuperAdmin 是系统初始化专用账户，与普通用户 (Admin/Doctor) 有本质区别。需要确定其归属模块和认证流程。

## 决策

- SuperAdmin 归属 Auth 模块，存储在 AdminSecrets 表
- 不在 Users 表中，不参与用户管理 CRUD
- 使用 UserType 字段区分认证路由
- RefreshToken 支持 SuperAdmin 和普通用户两种类型

### 安全措施
- Token 存储使用 DPAPI 加密 (Desktop)
- RefreshToken 支持撤销和重放攻击检测
- 安全审计日志 (SecurityAuditLog)

## 理由

- 职责分离: SuperAdmin 是安全关注点，不是业务关注点
- 模块边界清晰: Auth 模块管理认证，Users 模块管理业务用户
- 安全隔离: SuperAdmin 凭据与普通用户凭据分开存储

## 变更记录

| 日期 | 变更 |
|------|------|
| 2025-12-01 | 初始决策 |
| 2025-12-05 | Token 安全重构 |
