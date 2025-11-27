# Spec: login-credential-handling

## Overview

登录界面凭据处理规范，定义用户名、密码字段与保存凭据的交互行为。

---

## ADDED Requirements

### Requirement: 用户名变更时清空密码

当用户在登录界面修改用户名字段，且该用户名与之前自动填充的保存用户名不同时，系统 **SHALL** 自动清空密码字段。

**Rationale**: 防止显示前一个用户的保存密码，避免安全隐患和用户困惑。

#### Scenario: 用户修改用户名后密码自动清空

**Given** 登录界面已加载保存的凭据（用户名："doctor1"，密码："****"）
**When** 用户将用户名修改为 "doctor2"
**Then** 密码字段自动清空
**And** "记住密码"复选框状态保持不变

#### Scenario: 初始加载不触发密码清空

**Given** 存储中有保存的凭据（用户名："doctor1"，密码已加密）
**When** 登录界面初始化并自动填充凭据
**Then** 用户名显示 "doctor1"
**And** 密码字段显示掩码密码（不被清空）

#### Scenario: 无保存凭据时正常输入

**Given** 存储中没有保存的凭据
**When** 用户手动输入用户名 "newuser"
**Then** 密码字段保持为空（用户未输入）
**And** 不触发任何额外的密码清空逻辑

#### Scenario: 用户名清空不触发密码清空

**Given** 登录界面已加载保存的凭据
**When** 用户清空用户名字段（变为空字符串）
**Then** 密码字段不受影响
**And** 允许用户继续编辑

---

## Related Capabilities

- `authentication`: 登录认证流程
- `credential-storage`: 凭据安全存储（DPAPI加密）
