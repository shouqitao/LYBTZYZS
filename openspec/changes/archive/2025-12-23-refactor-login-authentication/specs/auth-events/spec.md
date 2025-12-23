# auth-events Specification

## Purpose

定义认证事件体系规范，使用Prism EventAggregator实现组件间解耦通信，支持认证生命周期监控。

## ADDED Requirements

### Requirement: AEV-001 认证事件定义

系统 **SHALL** 定义标准化的认证事件，覆盖登录、登出、Token管理等场景。

#### Scenario: 登录相关事件

- **GIVEN** 系统需要通知登录状态变化
- **WHEN** 定义登录事件
- **THEN** 包含以下事件类型：
  - LoginStartedEvent: 登录开始
  - LoginSucceededEvent: 登录成功
  - LoginFailedEvent: 登录失败
  - AutoLoginAttemptedEvent: 自动登录尝试

#### Scenario: 会话相关事件

- **GIVEN** 系统需要通知会话状态变化
- **WHEN** 定义会话事件
- **THEN** 包含以下事件类型：
  - SessionExpiringEvent: 会话即将过期
  - SessionExpiredEvent: 会话已过期
  - SessionExtendedEvent: 会话已延长

#### Scenario: 登出相关事件

- **GIVEN** 系统需要通知登出状态变化
- **WHEN** 定义登出事件
- **THEN** 包含以下事件类型：
  - LogoutStartedEvent: 登出开始
  - LogoutCompletedEvent: 登出完成
  - ForcedLogoutEvent: 强制登出（Token被撤销）

#### Scenario: Token相关事件

- **GIVEN** 系统需要通知Token状态变化
- **WHEN** 定义Token事件
- **THEN** 包含以下事件类型：
  - TokenRefreshedEvent: Token刷新成功
  - TokenRefreshFailedEvent: Token刷新失败

---

### Requirement: AEV-002 事件载荷规范

系统 **SHALL** 为每个事件定义标准化的载荷结构，包含必要的上下文信息。

#### Scenario: LoginSucceededEvent载荷

- **GIVEN** 登录成功事件触发
- **WHEN** 构建事件载荷
- **THEN** 包含：
  - UserId: 用户ID
  - Username: 用户名
  - RealName: 真实姓名
  - Role: 用户角色
  - Timestamp: 登录时间
  - IsAutoLogin: 是否自动登录

#### Scenario: LoginFailedEvent载荷

- **GIVEN** 登录失败事件触发
- **WHEN** 构建事件载荷
- **THEN** 包含：
  - Username: 尝试的用户名
  - ErrorCode: 错误码
  - ErrorMessage: 错误消息
  - Timestamp: 失败时间
  - AttemptCount: 尝试次数

#### Scenario: SessionExpiringEvent载荷

- **GIVEN** 会话即将过期事件触发
- **WHEN** 构建事件载荷
- **THEN** 包含：
  - ExpiresAt: 过期时间
  - RemainingSeconds: 剩余秒数
  - Timestamp: 事件时间

#### Scenario: TokenRefreshFailedEvent载荷

- **GIVEN** Token刷新失败事件触发
- **WHEN** 构建事件载荷
- **THEN** 包含：
  - FailureReason: 失败原因枚举
  - ErrorMessage: 错误消息
  - WillRetry: 是否将重试
  - Timestamp: 失败时间

---

### Requirement: AEV-003 事件发布规范

系统 **SHALL** 在认证关键节点发布对应事件，确保事件时序正确。

#### Scenario: 登录流程事件序列

- **GIVEN** 用户执行登录操作
- **WHEN** 登录流程执行
- **THEN** 按以下顺序发布事件：
  1. LoginStartedEvent（开始验证）
  2. LoginSucceededEvent 或 LoginFailedEvent（验证完成）

#### Scenario: Token刷新事件序列

- **GIVEN** 系统执行Token刷新
- **WHEN** 刷新流程执行
- **THEN** 发布以下事件之一：
  - TokenRefreshedEvent（刷新成功）
  - TokenRefreshFailedEvent（刷新失败）

#### Scenario: 登出流程事件序列

- **GIVEN** 用户执行登出操作
- **WHEN** 登出流程执行
- **THEN** 按以下顺序发布事件：
  1. LogoutStartedEvent（开始登出）
  2. LogoutCompletedEvent（登出完成）

---

### Requirement: AEV-004 事件订阅指南

系统 **SHALL** 提供清晰的事件订阅模式，支持组件响应认证状态变化。

#### Scenario: 保存工作响应会话过期

- **GIVEN** 编辑界面订阅SessionExpiringEvent
- **WHEN** 收到会话即将过期事件
- **THEN** 自动保存未完成的工作
- **AND** 显示会话过期警告

#### Scenario: 清理资源响应登出

- **GIVEN** 模块订阅LogoutCompletedEvent
- **WHEN** 收到登出完成事件
- **THEN** 清理模块内的用户相关资源
- **AND** 重置模块状态

#### Scenario: 更新UI响应登录成功

- **GIVEN** Shell订阅LoginSucceededEvent
- **WHEN** 收到登录成功事件
- **THEN** 更新用户信息显示
- **AND** 导航到工作台

---

## Related Specs

- authentication (AUTH-000 统一定时任务调度)
- authentication (AUTH-003 登出前警告)
- module-communication (模块间通信规范)
