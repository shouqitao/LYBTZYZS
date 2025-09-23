# 模块对接分析报告：认证授权 (Auth)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 5 |
| Desktop 已对接点数 | 3 |
| **对接完成情况** | **60%** |

---

### WebApi 总点数列表 (5 个)

- `Login` (POST /api/v1/auth/login)
- `Logout` (POST /api/v1/auth/logout)
- `GetCurrentUser` (GET /api/v1/auth/current-user)
- `RefreshToken` (POST /api/v1/auth/refresh-token)
- `ChangePassword` (POST /api/v1/auth/change-password)

---

### Desktop 已对接点数列表 (3 个)

- `LoginAsync`
- `LogoutAsync`
- `GetCurrentUserAsync`

---

### 未对接点分析

- **`RefreshToken`**: 刷新令牌。用于在用户Token过期时自动续期，实现“保持登录”或“七天免登录”等功能。
- **`ChangePassword`**: 修改密码。

---

### 状态总结

**核心功能已对接。**

用户的登录、登出、获取当前用户信息这三个最核心的流程已经打通。但缺少了提升用户体验的“自动刷新登录状态”和用户中心的“修改密码”功能。
