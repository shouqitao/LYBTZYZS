## AGENTS.md — 用户模块（LYBT.Module.Users）

### 1. Agent 概述

用户模块负责系统中用户账号的创建、管理与维护，包括注册、禁用、启用、修改信息、重置密码以及修改个人资料等。用户模块与认证模块协作，实现系统登录功能，是权限控制的基础模块之一。

### 2. 核心能力

- 用户信息的增删改查（CRUD）
- 支持分页与条件查询用户列表
- 用户账号启用/禁用
- 重置用户密码、修改个人密码
- 修改用户个人资料
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

- `Task<(IList<UserDto>, int)> SearchAsync(UserQueryDto dto)`
- `Task<UserDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(UserCreateDto dto, Guid operatorId, string operatorName)`
- `Task<bool> UpdateAsync(UserDetailDto dto, Guid operatorId, string operatorName)`
- `Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName)`
- `Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName)`
- `Task<bool> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName)`
- `Task<bool> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName)`
- `Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName)`
- `Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)`
- `Task<bool> ChangeProfileAsync(Guid id, string realName, string? phone, string? email)`
- `Task<IList<string>> GetRoles()`

