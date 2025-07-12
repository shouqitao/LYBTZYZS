## AGENTS.md — 用户模块（LYBT.Module.Users）

### 1. Agent 概述

用户模块负责系统中用户账号的创建、管理与维护，包括注册、禁用、启用、修改信息、重置密码以及修改个人资料等。用户模块与认证模块协作，实现系统登录功能，是权限控制的基础模块之一。

### 2. 核心能力

- 新增与修改用户信息
- 分页查询用户列表
- 用户账号启用/禁用及批量操作
- 重置密码、修改个人密码
- 修改个人资料
- 获取系统内置角色列表

### 3. 输入输出规范

#### 输入

- `UserCreateDto`：用于新增用户（包含用户名、姓名、角色等）
- `UserDetailDto`：用于更新用户
- `UserQueryDto`：用于分页/条件查询
- `ChangePasswordDto`：修改密码
- `ChangeProfileDto`：修改个人信息

#### 输出

- `UserDto`：用户信息展示对象
- `(IList<UserDto> Users, int TotalCount)`：分页查询结果
- `bool`：表示操作成功或失败

### 4. 协作与依赖模块

- **认证模块**：调用用户模块获取用户凭证进行登录校验
- **日志模块**：每次用户操作（新增、修改、禁用等）都记录日志
- **通用模块**：使用性别、状态等枚举定义
- **基础设施模块**：通过仓储访问数据库中的用户实体表

### 5. 示例场景

#### 添加用户

管理员添加新用户：

```csharp
var dto = new UserCreateDto {
  UserName = "doctor001",
  RealName = "李医生",
  RoleNames = new List<string>{"Doctor"}
};
await _userService.AddAsync(dto, adminId, adminName);
```

#### 禁用用户

```csharp
await _userService.DisableAsync(userId, adminId, adminName);
```

#### 用户修改密码

```csharp
await _userService.ChangePasswordAsync(userId, oldPwd, newPwd);
```

### 6. 接口列表

- `Task<(IList<UserDto>, int)> SearchAsync(UserQueryDto dto)` - 搜索用户
- `Task<UserDetailDto?> GetByIdAsync(Guid id)` - 根据ID获取用户
- `Task<bool> AddAsync(UserCreateDto dto, Guid operatorId, string operatorName)` - 新增用户
- `Task<bool> UpdateAsync(UserDetailDto dto, Guid operatorId, string operatorName)` - 更新用户
- `Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName)` - 禁用用户
- `Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName)` - 启用用户
- `Task<bool> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName)` - 批量禁用
- `Task<bool> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName)` - 批量启用
- `Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName)` - 重置密码
- `Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)` - 修改密码
- `Task<bool> ChangeProfileAsync(Guid id, string realName, string? phone, string? email)` - 修改个人资料
- `Task<IList<string>> GetRoles()` - 获取角色列表

### Web API 接口对照

| 设计接口 | 接口说明 | 状态 | 备注 |
| --- | --- | --- | --- |
| `POST /api/users` | 新增用户 | 未实现 | 实际路径 `POST /api/Users/add` |
| `PUT /api/users/{id}` | 更新用户 | 未实现 | 实际路径 `PUT /api/Users/update` |
| `GET /api/users` | 查询用户 | 未实现 | 实际路径 `GET /api/Users/search` |
| `DELETE /api/users/{id}` | 删除用户 | 未实现 | 仅提供 `disable/{id}` 与 `enable/{id}` |



接口数量：12
已实现 Web API 数量：0
