---
type: concept
title: 密码管理与安全策略
tags: [security, authentication, cryptography]
related: [authentication, configuration-management, testing-strategy]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/05-development/06-security-password-management.md"]
---
# 密码管理与安全策略

## 概述

密码管理与安全策略定义了凌隐宝堂系统中用户凭证的存储、验证、保护及管理员账户的生命周期管理机制。该策略核心在于通过[认证授权](authentication.md)模块底层的密码哈希算法、可配置的账户锁定机制以及环境感知的管理员自动化运维流程，确保系统在面对暴力破解、时序攻击及内部误操作时的安全性。具体哈希实现详见[用户管理模块](modules/users-module.md)的密码策略章节。

## 核心机制

### 1. 密码哈希

系统通过 `IPasswordService` 接口抽象密码哈希操作，确保业务层与具体哈希算法解耦。
*   **实现**：`PasswordService` 封装了具体的哈希和验证逻辑。
*   **自适应性**：系统支持在验证密码时检测哈希版本，可在用户登录时自动重新哈希并更新存储，实现平滑迁移。

### 2. 账户锁定策略 (Account Lockout)

为防止暴力破解，系统实施了基于连续登录失败次数的自动锁定机制。
*   **配置驱动**：通过 `AccountLockoutOptions` (`Security:AccountLockout`) 配置最大失败次数 (`MaxFailedCount`) 和锁定持续时间 (`LockoutMinutes`)。
*   **环境差异化**：
    *   **生产环境**：严格启用，默认失败 5 次锁定 15 分钟。
    *   **开发/测试环境**：可禁用或放宽限制（如失败 10 次锁定 1 分钟），以优化开发者体验并避免测试过程中的频繁锁定。

### 3. 定时安全比较 (Timing-safe Comparison)

在处理敏感令牌（如 `InitialSetupToken`）时，系统使用 `SecureEquals` 方法进行固定时间字符串比较。
*   **目的**：防止攻击者通过测量响应时间的微小差异（时序攻击）来推断令牌的正确字符，从而逐步破解令牌。

### 4. 密码复杂度验证

通过 `ValidatePassword` 方法强制执行密码策略：
*   长度 ≥ 8 位。
*   必须包含大写字母、小写字母、数字和特殊字符。

## 系统管理员 (sysadmin) 生命周期自动化

`sysadmin` 账户的管理通过 `SystemAdminOptions` 配置驱动，实现了从创建到重置的自动化流程，减少了手动数据库操作带来的安全风险。

### 启动流程逻辑

1.  **自动创建**：若 `AutoCreateOnStartup` 为真且账户不存在，则创建账户。
2.  **生产环境保护**：在生产环境中，除非提供有效的 `InitialSetupToken` 且 `AllowAutoCreateInProduction` 为真，否则禁止自动创建或重置。
3.  **开发环境便利化**：在开发/测试环境中，若 `ForceResetOnStartup` 为真，每次启动时强制重置 `sysadmin` 的密码、失败计数和锁定状态，确保开发者始终可使用默认密码登录。

### 安全约束

*   **生产环境禁令**：`ForceResetOnStartup` 在生产环境中被强制忽略，防止因配置错误导致根账户被意外重置。
*   **Token 保护**：`InitialSetupToken` 的比较使用定时安全算法，防止侧信道泄露。

## 可测试性设计 (Testability via DI)

系统将密码逻辑抽象为 `IPasswordService` 接口，而非直接依赖静态方法。
*   **依赖注入**：`PasswordService` 通过 DI 容器注入。
*   **Mock 支持**：在单元测试中，可以替换为 Mock 实现，模拟各种密码验证场景（如成功、失败、锁定），而无需实际执行耗时的哈希计算。这符合[测试策略](testing-strategy.md)中关于隔离外部依赖的原则。

## 配置管理集成

密码与安全策略深度集成于配置管理架构中：
*   **Options 模式**：`AccountLockoutOptions` 和 `SystemAdminOptions` 均采用强类型绑定。
*   **环境分层**：通过 `appsettings.Development.json`、`appsettings.Test.json` 等文件实现不同环境的安全策略差异化，确保生产环境的高安全性与开发环境的高便利性并存。

## 相关链接

- [认证授权](authentication.md)

- configuration-management — 配置管理架构
- [测试策略](testing-strategy.md)