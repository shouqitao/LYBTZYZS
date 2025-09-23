# 模块对接分析报告：用户管理 (Users)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 12 |
| Desktop 已对接点数 | 11 |
| **对接完成情况** | **92%** |

---

### WebApi 总点数列表 (12 个)

- `GetUsers` (GET /api/v1/users)
- `GetUserById` (GET /api/v1/users/{id})
- `CreateUser` (POST /api/v1/users)
- `UpdateUser` (PUT /api/v1/users/{id})
- `ToggleStatus` (PATCH /api/v1/users/{id}/toggle-status)
- `BatchDisable` (PATCH /api/v1/users/batch-disable)
- `BatchEnable` (PATCH /api/v1/users/batch-enable)
- `ResetPassword` (POST /api/v1/users/reset-password/{id})
- `ChangePassword` (PATCH /api/v1/users/password)
- `ChangeProfile` (PUT /api/v1/users/profile)
- `GetRoles` (GET /api/v1/users/roles)
- `GetActiveUsers` (GET /api/v1/users/active)

---

### Desktop 已对接点数列表 (11 个)

- `GetUsersAsync`
- `GetUserByIdAsync`
- `CreateUserAsync`
- `UpdateUserAsync`
- `ToggleStatusAsync`
- `BatchDisableAsync`
- `BatchEnableAsync`
- `ResetPasswordAsync`
- `ChangePasswordAsync`
- `ChangeProfileAsync`
- `GetActiveUsersAsync`

---

### 未对接点分析

- **`GetRoles`**: 获取所有角色列表的API。后端已提供，但前端尚未有功能（如下拉菜单或权限配置页面）调用它。

---

### 状态总结

**对接已基本完成。**

功能非常完整，仅一个用于辅助和配置的“获取角色列表”API尚未被调用，对核心业务流程没有影响。
