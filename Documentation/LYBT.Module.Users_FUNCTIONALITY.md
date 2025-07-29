# LYBT.Module.Users 功能说明文档

## 模块概述

用户管理模块负责系统用户的完整生命周期管理，包括用户注册、信息维护、权限控制、密码管理等核心功能。本模块采用软删除策略，支持基于角色的权限控制。

## 数据模型

### UserModel (用户实体)

**文件位置**: `Models/UserModel.cs`

| 字段名              | 类型                   | 说明         | 验证规则             |
| ---------------- | -------------------- | ---------- | ---------------- |
| Id               | Guid                 | 用户唯一标识（主键） | 必填               |
| UserName         | string               | 用户名（唯一）    | 长度2-32字符，必填      |
| RealName         | string               | 真实姓名       | 最长20字符，必填        |
| PinyinCode       | string               | 真实姓名拼音码    | 最长32字符，用于快速检索    |
| Role             | UserRole             | 用户角色（单一角色） | 必填，一个用户只能有一个角色  |
| IsActive         | bool                 | 启用状态       | true=启用，false=禁用 |
| CreatedTime      | DateTime             | 创建时间       | 系统自动设置           |
| LastLoginTime    | DateTime?            | 最近登录时间     | 可为空              |
| PasswordHash     | string               | 密码哈希值      | 必填，敏感信息          |
| FailedLoginCount | int                  | 连续登录失败次数   | 用于账户锁定策略         |
| LockoutEnd       | DateTime?            | 账号锁定截止时间   | null表示未锁定        |
| Email            | string?              | 邮箱地址       | 可选，需符合邮箱格式       |
| PhoneNumber      | string?              | 手机号码       | 可选，需符合手机号格式      |

## DTO 数据传输对象

### UserDto (用户信息展示)

**使用场景**: 用户列表展示、用户基本信息返回
**特点**: 不包含敏感信息如密码哈希

```csharp
- Id: 用户ID
- UserName: 用户名
- RealName: 真实姓名
- Role: 角色（单一）
- IsActive: 启用状态
- CreatedTime: 创建时间
- LastLoginTime: 最近登录时间
- Email: 邮箱
- PhoneNumber: 手机号
```

### UserCreateDto (用户创建)

**使用场景**: 管理员创建新用户
**特点**: 不包含密码字段，系统使用默认密码

```csharp
- UserName: 用户名（必填，唯一性检查）
- RealName: 真实姓名（必填）
- Role: 角色（必填，单一角色）
- IsActive: 启用状态（默认true）
- Email: 邮箱（可选）
- PhoneNumber: 手机号（可选）
```

### UserDetailDto (用户详情编辑)

**使用场景**: 管理员编辑用户信息
**特点**: 包含ID字段用于更新操作

```csharp
- Id: 用户ID（必填，标识更新目标）
- RealName: 真实姓名
- Role: 角色
- IsActive: 启用状态
- Email: 邮箱
- PhoneNumber: 手机号
```

### UserQueryDto (用户查询条件)

**使用场景**: 用户列表的分页查询和条件筛选

```csharp
- Keyword: 关键词（模糊匹配用户名、真实姓名、拼音码）
- Role: 角色筛选（单选）
- IsActive: 启用状态筛选
- Page: 页码（继承自PaginationRequest）
- PageSize: 每页大小（继承自PaginationRequest）
```

### ChangePasswordDto (密码修改)

**使用场景**: 用户自行修改密码

```csharp
- OldPassword: 原密码（验证身份）
- NewPassword: 新密码
```

### ChangeProfileDto (个人信息修改)

**使用场景**: 用户修改个人资料

```csharp
- RealName: 真实姓名
- Email: 邮箱
- PhoneNumber: 手机号
```

### BatchIdsDto (批量操作)

**使用场景**: 批量启用/禁用用户

```csharp
- Ids: 用户ID列表
```

## 服务层 (IUserService & UserService)

### 查询类方法

#### SearchAsync

```csharp
Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query, UserRole currentUserRole)
```

**功能**: 分页条件查询用户列表
**权限控制**: 管理员可查看所有用户（包括禁用），普通用户只能查看启用用户
**使用场景**: 用户管理页面的列表展示

#### GetByIdAsync

```csharp
Task<UserDto?> GetByIdAsync(Guid id, UserRole currentUserRole)
```

**功能**: 根据ID获取单个用户详情
**权限控制**: 同SearchAsync
**使用场景**: 用户详情页面、编辑前数据加载

#### GetActiveUsersAsync

```csharp
Task<List<UserDto>> GetActiveUsersAsync()
```

**功能**: 获取所有启用用户的简单列表
**使用场景**: 下拉选择框、关联选择等场景

### 管理类方法

#### AddAsync

```csharp
Task<bool> AddAsync(UserCreateDto dto, Guid operatorId, string operatorName)
```

**功能**: 创建新用户
**业务逻辑**: 

- 用户名唯一性检查
- 单一角色验证（通过Required特性）
- 自动生成拼音码
- 使用系统默认密码
- 记录操作日志
  **使用场景**: 管理员添加新用户

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(UserDetailDto dto, Guid operatorId, string operatorName)
```

**功能**: 更新用户信息
**业务逻辑**: 

- 记录修改前后数据对比
- 自动更新拼音码
- 记录详细操作日志
  **使用场景**: 管理员编辑用户信息

#### DisableAsync / EnableAsync

```csharp
Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName)
Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 单个用户启用/禁用
**业务逻辑**: 软删除策略，仅修改IsActive状态
**使用场景**: 用户状态管理

#### BatchDisableAsync / BatchEnableAsync

```csharp
Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName)
Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName)
```

**功能**: 批量启用/禁用用户
**业务逻辑**: 

- 批量大小限制（由配置控制）
- 返回实际影响的记录数
- 记录批量操作日志
  **使用场景**: 批量用户状态管理

### 密码管理方法

#### ResetPasswordAsync

```csharp
Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 管理员重置用户密码为默认值
**业务逻辑**: 

- 使用系统配置的默认密码
- 可选发送密码重置通知（待实现）
- 记录密码重置日志
  **使用场景**: 用户忘记密码时的管理员操作

#### ChangePasswordAsync

```csharp
Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
```

**功能**: 用户自行修改密码
**业务逻辑**: 

- 验证原密码正确性
- 密码哈希加密存储
- 记录密码修改日志
  **使用场景**: 用户个人中心密码修改

#### ChangeProfileAsync

```csharp
Task<bool> ChangeProfileAsync(Guid id, string realName, string? email, string? phoneNumber)
```

**功能**: 用户修改个人信息
**业务逻辑**: 

- 自动更新拼音码
- 记录修改前后对比
- 记录个人信息修改日志
  **使用场景**: 用户个人中心信息修改

### 工具方法

#### GetRoles

```csharp
List<UserRole> GetRoles()
```

**功能**: 获取系统所有可用角色
**使用场景**: 角色选择下拉框

## 仓储层 (IUserRepository & UserRepository)

### 基础CRUD方法

#### AddAsync / UpdateAsync

```csharp
Task<bool> AddAsync(UserModel user)
Task<bool> UpdateAsync(UserModel user)
```

**功能**: 基础的增加和更新操作
**使用场景**: 服务层调用的底层数据操作

#### DisableAsync / EnableAsync

```csharp
Task<bool> DisableAsync(Guid id)
Task<bool> EnableAsync(Guid id)
```

**功能**: 软删除策略的启用/禁用操作
**实现**: 直接修改IsActive字段

### 查询方法

#### GetPagedAsync

```csharp
Task<(IList<UserModel> users, int total)> GetPagedAsync(UserQueryDto query, bool includeDisabled = false)
```

**功能**: 分页条件查询
**查询条件**: 

- 关键词模糊匹配（用户名、真实姓名、拼音码）
- 角色筛选（单选）
- 启用状态筛选
- 自动隐藏sysadmin内置用户
  **排序**: 按创建时间倒序

#### GetByUsernameAsync

```csharp
Task<UserModel?> GetByUsernameAsync(string userName)
```

**功能**: 根据用户名查找用户
**特点**: 包括禁用用户，主要用于登录验证
**使用场景**: 认证模块的用户查找

#### GetByIdAsync

```csharp
Task<UserModel?> GetByIdAsync(Guid id, bool includeDisabled = false)
```

**功能**: 根据ID查找用户
**权限控制**: 可选择是否包含禁用用户

#### GetUsersByIdsAsync

```csharp
Task<List<UserModel>> GetUsersByIdsAsync(List<Guid> ids, bool includeDisabled = false)
```

**功能**: 批量根据ID查找用户
**使用场景**: 批量操作前的数据验证

### 特殊查询方法

#### ExistsByUsernameAsync

```csharp
Task<bool> ExistsByUsernameAsync(string userName)
```

**功能**: 检查用户名是否已存在
**特点**: 包括禁用用户，确保用户名全局唯一
**使用场景**: 用户创建时的唯一性验证

#### GetActiveUsersAsync

```csharp
Task<List<UserModel>> GetActiveUsersAsync()
```

**功能**: 获取所有启用用户
**排序**: 按真实姓名排序
**使用场景**: 需要用户选择的下拉框场景

### 批量操作方法

#### UpdateActiveStatusAsync

```csharp
Task<int> UpdateActiveStatusAsync(List<Guid> ids, bool isActive)
```

**功能**: 批量更新用户启用状态
**特点**: 使用EF Core的批量更新，性能优化
**返回**: 实际影响的记录数

#### UpdatePasswordAsync

```csharp
Task<bool> UpdatePasswordAsync(Guid id, string passwordHash)
```

**功能**: 更新用户密码哈希
**使用场景**: 密码重置和修改操作

## 权限控制策略

### 角色级别权限

- **管理员(Admin)**: 可查看和操作所有用户（包括禁用用户）
- **普通用户**: 只能查看启用的用户，不能进行管理操作

### 数据隐藏策略

- 内置sysadmin用户在所有查询中被自动隐藏
- 密码哈希值不会在任何DTO中暴露
- 禁用用户对普通用户不可见

### 操作权限

- 只有管理员可以创建、编辑、启用/禁用其他用户
- 用户可以修改自己的密码和个人信息
- 所有管理操作都需要记录操作者信息

## 日志审计

### 操作日志记录

所有用户管理操作都会记录详细的审计日志，包括：

- 用户创建、编辑、启用/禁用
- 密码重置和修改
- 批量操作
- 个人信息修改

### 日志内容

- 操作者信息（ID和姓名）
- 操作类型和描述
- 操作对象信息
- 修改前后数据对比（敏感信息除外）
- 操作时间

## 配置选项 (UserOptions)

### 安全配置

- `DefaultUserPassword`: 新用户默认密码和重置密码
- `MaxBatchOperationSize`: 批量操作的最大数量限制
- `EnableDetailedAuditLogging`: 是否启用详细审计日志

### 通知配置

- `SendPasswordResetNotification`: 是否发送密码重置通知（功能待实现）

## 使用示例

### 管理员创建用户

```csharp
var createDto = new UserCreateDto {
    UserName = "doctor001",
    RealName = "张医生",
    Role = UserRole.DiagnosingDoctor,
    Email = "doctor@clinic.com"
};
await userService.AddAsync(createDto, adminId, "管理员");
```

### 用户修改个人信息

```csharp
await userService.ChangeProfileAsync(
    userId, "张三丰", "zhangsan@email.com", "13800138000"
);
```

### 分页查询用户

```csharp
var query = new UserQueryDto {
    Keyword = "张",
    Role = UserRole.DiagnosingDoctor,
    Page = 1,
    PageSize = 20
};
var (users, total) = await userService.SearchAsync(query, currentUserRole);
```

### 批量禁用用户

```csharp
var ids = new List<Guid> { userId1, userId2, userId3 };
var count = await userService.BatchDisableAsync(ids, adminId, "管理员");
```