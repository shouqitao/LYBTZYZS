# LYBT.Module.Users 代码知识

服务端用户管理模块，提供用户CRUD、密码管理、角色权限控制、批量操作和状态切换功能。

## 代码文件结构

```
LYBT.Module.Users/
├── UsersModule.cs                         # 模块DI注册入口
├── Interfaces/
│   ├── IUserService.cs                    # 用户服务接口
│   └── IUserRepository.cs                # 用户仓储接口
├── Mapping/
│   └── UserMapper.cs                      # Mapperly编译时映射器
├── Repositories/
│   └── UserRepository.cs                  # 用户仓储实现 (internal)
└── Services/
    └── UserService.cs                     # 用户服务实现（807行）
```

### UsersModule.cs
**UsersModule** (static) | 模块DI注册

| 方法 | 说明 |
|------|------|
| AddUsersModule(IServiceCollection, IConfiguration) | 注册IUserRepository(通过AddRepository扩展方法)、IUserService(Scoped)、FluentValidation验证器 |
| UseUsersModule(IApplicationBuilder) | 中间件配置(当前为空占位) |

### Interfaces/IUserService.cs
**IUserService** | 用户服务统一接口，标准CRUD模式

| 方法 | 说明 |
|------|------|
| GetPagedAsync(int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status) | 分页查询用户，支持关键字/角色/状态筛选 |
| GetByIdAsync(Guid id) | 根据ID获取用户详情 |
| SearchAsync(string keyword) | 搜索用户(用户名/真实姓名/邮箱) |
| CreateAsync(UserInputDto, CancellationToken) | 创建用户，含权限检查和用户名唯一性验证 |
| UpdateAsync(Guid id, UserInputDto, CancellationToken) | 更新用户，含角色变更Token撤销 |
| DeleteAsync(Guid id) | 软删除用户，含自删除保护和最后管理员保护 |
| ResetPasswordAsync(Guid id, ResetPasswordRequestDto) | 管理员重置密码，使用配置默认密码 |
| ValidatePasswordAsync(string userName, string password) | 验证用户密码 |
| ChangePasswordAsync(Guid id, string oldPassword, string newPassword) | 更改密码，含密码策略验证 |
| ChangeProfileAsync(Guid userId, ChangeProfileDto) | 修改个人信息(RealName/PhoneNumber) |
| ToggleStatusAsync(Guid id) | 切换启用/禁用状态 |
| RestoreAsync(Guid id) | 恢复软删除的用户 |
| BatchDeleteAsync(List\<Guid\> ids, Guid? currentUserId) | 批量软删除用户 |
| BatchUpdateStatusAsync(List\<Guid\> ids, CommonStatus status, Guid? currentUserId) | 批量更新用户状态 |

### Interfaces/IUserRepository.cs
**IUserRepository** : IRepository\<User\> | 用户仓储接口，继承11个标准CRUD方法

自定义方法:
| 方法 | 说明 |
|------|------|
| GetByUsernameAsync(string username) | 根据用户名或邮箱查询用户 |
| UsernameExistsAsync(string username) | 检查用户名唯一性 |
| GetPagedAsync(int pageNumber, int pageSize, string? keyword, UserRole? role, CommonStatus? status) | 带筛选条件的分页查询(DB层执行) |
| GetByIdIncludingDeletedAsync(Guid id) | 获取包含已软删除的实体(用于Restore) |

### Mapping/UserMapper.cs
**UserMapper** (partial, Mapperly) | 编译时映射器，替代AutoMapper

| 方法 | 说明 |
|------|------|
| ToListDto(User) | User实体转UserListDto |
| ToListDtos(List\<User\>) | 批量转换 |
| ToDetailDto(User) | User实体转UserDetailDto |
| ToDetailDtos(List\<User\>) | 批量转换 |
| ToEntity(UserInputDto) | UserInputDto转User实体(创建)，忽略Id/Status/PasswordHash/审计字段 |
| UpdateEntity(UserInputDto, User) | 更新现有实体，忽略Id/UserName/Status/PasswordHash/审计字段 |

### Repositories/UserRepository.cs
**UserRepository** (internal) : BaseRepository\<User\>, IUserRepository | 用户仓储实现

模板方法覆盖:
| 方法 | 说明 |
|------|------|
| ApplyKeywordFilter(IQueryable, string) | 按用户名、真实姓名、拼音码过滤 |
| ApplyDefaultOrdering(IQueryable) | 按用户名升序排序 |

自定义方法:
| 方法 | 说明 |
|------|------|
| GetPagedAsync(...) | DB层分页查询，复用模板方法进行关键字过滤和排序 |
| GetByUsernameAsync(string) | 按用户名或邮箱查询，AsNoTracking优化 |
| UsernameExistsAsync(string) | AnyAsync检查用户名是否存在 |
| GetByIdIncludingDeletedAsync(Guid) | 使用IgnoreQueryFilters绕过全局软删除过滤器 |

### Services/UserService.cs
**UserService** : BaseService\<User\>, IUserService | 用户服务实现 (807行)

依赖: IUserRepository, IConfiguration, IHttpContextAccessor, IValidator\<UserInputDto\>, ICrossModuleAuthService, UserMapper

权限控制辅助方法:
| 方法 | 说明 |
|------|------|
| GetCurrentUserRole() | 从HttpContext Claims提取当前用户角色 |
| GetCurrentUserId() | 从HttpContext Claims提取当前用户ID |
| CanManageUser(UserRole?, UserRole?) | 权限判断：SuperAdmin管理所有，Admin管理Doctor+Receptionist |
| CanDeleteUserAsync(Guid, UserRole) | 删除权限检查+最后SuperAdmin/Admin保护 |

业务方法(实现IUserService全部方法):

创建用户: FluentValidation -> 权限检查 -> 保留用户名检查 -> 用户名唯一性 -> Mapperly映射 -> 拼音码生成 -> 密码哈希(PasswordHelper) -> 保存

更新用户: 获取实体 -> FluentValidation -> 权限检查 -> 角色变更权限 -> Mapperly更新 -> 拼音码更新 -> 保存 -> 角色变更时撤销Token

删除用户: 自删除保护 -> 获取实体 -> 权限检查+最后管理员保护 -> 撤销Token -> 软删除

重置密码: 使用配置默认密码 -> PasswordHelper哈希 -> 标记MustChangeOnNextLogin -> 撤销Token

更改密码: 密码策略验证(PasswordPolicyValidator) -> 旧密码验证 -> 新密码哈希 -> 清除须改密标记 -> 撤销Token

批量操作: 逐个权限检查 -> 自删除/自修改保护 -> 最后管理员保护 -> 统一SaveChanges(BatchDelete) / 逐个UpdateAsync(BatchUpdateStatus)

## 死代码与废弃标记

(无)

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| Services/UserService.cs | 文件过大(807行) | 包含CRUD、密码管理、权限控制、批量操作、个人资料修改等多个职责 | 考虑拆分：UserService(CRUD) + UserPasswordService(密码相关) + UserBatchService(批量操作) |
| Services/UserService.cs | BatchDeleteAsync与BatchUpdateStatusAsync事务不一致 | BatchDelete使用标记+统一SaveChanges(单事务)，BatchUpdateStatus逐个UpdateAsync(多事务) | BatchUpdateStatus应改为统一SaveChanges模式以保证事务一致性 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| UserName创建后不可更改 | UserMapper.UpdateEntity忽略UserName字段，UserInputDto也不包含UserName用于更新 | 设计如此，如需改名需新增专用接口 |
| EF Core 8 FindAsync与软删除 | FindAsync在实体不在ChangeTracker中时会应用全局查询过滤器(IsDeleted)，无法查到已删除记录 | Restore操作使用GetByIdIncludingDeletedAsync(IgnoreQueryFilters) |
| 密码重置使用配置默认密码 | ResetPasswordAsync始终使用Lybt:DefaultPasswords:NewUserPassword配置值 | 如未配置则回退到PasswordHelper.GenerateTemporaryPassword()随机生成 |
| 保留用户名列表硬编码 | admin/administrator/root/system/superadmin/sysadmin在CreateAsync中硬编码 | 如需调整保留用户名列表，需修改代码 |
