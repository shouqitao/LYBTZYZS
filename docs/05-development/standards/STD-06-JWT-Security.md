# STD-06: JWT 安全规范

## 适用范围

远程模式下的用户认证、Token 签发与验证。

## 规范内容

### Token 生命周期

| Token 类型 | 有效期 | 用途 |
|------------|--------|------|
| Access Token | 15 分钟 | API 请求认证，短生命周期降低泄露风险 |
| Refresh Token | 7 天 | 静默续签 Access Token，用户活跃时自动刷新 |
| AutoLogin Token | 7 天 | 自动登录凭据，HMAC 签名验证 |

### 签发规则

| 规则 | 说明 |
|------|------|
| SecretKey >= 32 字符 | 生产环境密钥长度不低于 256 位 |
| Issuer/Audience 必须配置 | 防止跨应用 Token 滥用 |
| Claims 包含 UserId/UserName/Role | 最小信息原则，不在 Token 中存储敏感数据 |
| Token 签名算法 | HmacSha256 (对称签名，单服务架构足够) |

### Token 撤销策略

通过 `RevokedTokens` 表实现 Token 黑名单:

| 场景 | 撤销方式 |
|------|---------|
| 用户主动登出 | 撤销当前 Token Family |
| 管理员重置密码 | 撤销该用户所有 Token Family |
| 用户修改密码 | 撤销该用户所有 Token Family |
| 角色变更 | 撤销该用户所有 Token Family，强制重登录 |
| 用户被禁用/删除 | 撤销该用户所有 Token Family |

撤销接口统一通过 `ICrossModuleAuthService.RevokeAllUserTokensAsync()` 调用 (AUTH-D07)。

### AutoLogin Token

| 规则 | 说明 |
|------|------|
| HMAC 签名 | 使用独立密钥签名，防篡改 |
| DPAPI 加密存储 | Windows 用户级加密，本机绑定 |
| 单设备绑定 | Token 包含设备标识，其他设备无法使用 |

### 安全约束

1. Token 仅通过 HTTPS 传输 (生产环境强制)
2. Refresh Token 单次使用: 每次刷新后旧 Token 失效 (Token Rotation)
3. 失败登录锁定: 连续 5 次失败后锁定 15 分钟
4. 登录失败隐藏: 用户不存在和密码错误统一返回"用户名或密码错误"，防枚举攻击

## 参考

- 认证 PRD: `docs/02-requirements/auth.md`
- 用户管理 PRD: `docs/02-requirements/users.md`

---

创建日期: 2026-02-26
