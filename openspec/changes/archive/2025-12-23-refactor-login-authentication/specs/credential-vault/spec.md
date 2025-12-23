# credential-vault Specification

## Purpose

定义凭据保险库规范，安全存储用户名和AutoLoginToken（非密码），使用DPAPI加密和HMAC完整性校验。

## ADDED Requirements

### Requirement: CVT-001 AutoLoginToken存储

系统 **SHALL** 使用AutoLoginToken替代密码存储，AutoLoginToken由服务端生成，可随时撤销。

#### Scenario: 保存AutoLoginToken

- **GIVEN** 用户勾选"记住密码"并成功登录
- **AND** 服务端返回AutoLoginToken
- **WHEN** 系统保存凭据
- **THEN** 使用DPAPI加密AutoLoginToken
- **AND** 计算HMAC完整性校验值
- **AND** 存储到安全位置

#### Scenario: 不存储明文密码

- **GIVEN** 用户输入密码登录
- **WHEN** 系统处理登录成功
- **THEN** 不将用户密码存储到任何持久化位置
- **AND** 仅保存服务端返回的AutoLoginToken

#### Scenario: AutoLoginToken用于自动登录

- **GIVEN** 用户已保存AutoLoginToken
- **WHEN** 应用启动时尝试自动登录
- **THEN** 读取并解密AutoLoginToken
- **AND** 调用服务端auto-login API
- **AND** 获取新的AccessToken和RefreshToken

---

### Requirement: CVT-002 凭据完整性校验

系统 **SHALL** 使用HMAC对存储的凭据进行完整性校验，防止篡改。

#### Scenario: 写入时计算HMAC

- **GIVEN** 系统需要保存凭据
- **WHEN** 凭据被加密后
- **THEN** 计算凭据内容的HMAC-SHA256
- **AND** 将HMAC与加密数据一起存储

#### Scenario: 读取时验证HMAC

- **GIVEN** 系统需要读取凭据
- **WHEN** 读取存储的凭据数据
- **THEN** 首先验证HMAC是否匹配
- **AND** 匹配则解密数据
- **AND** 不匹配则视为凭据损坏

#### Scenario: HMAC验证失败处理

- **GIVEN** 读取凭据时HMAC验证失败
- **WHEN** 系统检测到凭据可能被篡改
- **THEN** 删除存储的凭据
- **AND** 记录安全警告日志
- **AND** 用户需要重新登录

---

### Requirement: CVT-003 凭据迁移兼容

系统 **SHALL** 支持读取旧格式凭据并自动迁移到新格式。

#### Scenario: 检测旧格式凭据

- **GIVEN** 系统启动时检查凭据存储
- **WHEN** 发现存在旧格式凭据（无HMAC）
- **THEN** 标记需要迁移
- **AND** 下次成功登录后迁移到新格式

#### Scenario: 自动迁移凭据

- **GIVEN** 存在旧格式凭据
- **AND** 用户成功登录
- **WHEN** 服务端返回AutoLoginToken
- **THEN** 删除旧格式凭据
- **AND** 保存新格式凭据（含HMAC）
- **AND** 记录迁移日志

---

### Requirement: CVT-004 凭据清除

系统 **SHALL** 提供完整的凭据清除能力，确保敏感信息被彻底删除。

#### Scenario: 用户登出清除凭据

- **GIVEN** 用户执行登出操作
- **AND** 用户未勾选"记住密码"
- **WHEN** 系统清除凭据
- **THEN** 删除存储的用户名
- **AND** 删除存储的AutoLoginToken
- **AND** 删除HMAC校验值

#### Scenario: 保留用户名选项

- **GIVEN** 用户执行登出操作
- **AND** 用户勾选了"记住用户名"但未勾选"记住密码"
- **WHEN** 系统清除凭据
- **THEN** 保留用户名
- **AND** 删除AutoLoginToken

---

## Related Specs

- login-credential-handling (用户名变更时清空密码)
- authentication (AUTH-009 Logout后强制重新登录)
