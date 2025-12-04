# LYBT.Module.Users

> 用户管理 | 传统三层 | Admin/Doctor双角色体系

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 被Auth模块依赖进行身份验证

## 目录结构

```
LYBT.Module.Users/
├── UsersModule.cs
├── Interfaces/
│   └── IUserRepository.cs
├── Services/
│   └── UserService.cs
├── Repositories/
│   └── UserRepository.cs
├── Validators/
│   ├── UserCreateDtoValidator.cs
│   └── UserUpdateDtoValidator.cs
└── Mapping/
    └── UserMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IUserService | 19 | CRUD/搜索/密码管理/状态管理/批量操作 |
| IUserRepository | 25 | CRUD/分页/用户名邮箱唯一性检查 |

## 角色与状态

| 枚举 | 值 | 说明 |
|------|------|------|
| UserRole.Admin | - | 管理员 |
| UserRole.Doctor | - | 医生 |
| UserStatus.Active | - | 正常 |
| UserStatus.Inactive | - | 停用 |
| UserStatus.Locked | - | 锁定 |

## 密码安全

| 特性 | 说明 |
|------|------|
| 哈希算法 | ASP.NET Core Identity PasswordHasher |
| 强度要求 | 8位以上、大小写+数字+特殊字符 |
| 重置机制 | 生成临时密码 |

## DTO规范(2025-09-20优化)

| DTO | 用途 |
|------|------|
| UserCreateDto | 创建用户(用户名、密码、角色) |
| UserUpdateDto | 更新用户(显示名、角色、状态) |
| UserSearchDto | 搜索用户(关键词、角色、状态筛选) |
| UserDto | 响应DTO(Role为UserRole枚举) |

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (User实体, UserRole/UserStatus枚举)
- LYBT.Shared.Models (UserDto等)

### 被依赖
- LYBT.Module.Auth (身份验证)
- LYBT.Module.MedicalCase (DoctorId关联)
- LYBT.WebAPI (UsersController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/users | GET | 分页查询用户 |
| /api/users/{id} | GET | 按ID查询用户详情 |
| /api/users/username/{username} | GET | 按用户名查询 |
| /api/users/statistics | GET | 获取用户统计信息 |
| /api/users | POST | 创建用户 |
| /api/users/{id} | PUT | 更新用户 |
| /api/users/{id} | DELETE | 删除用户 |
| /api/users/batch-delete | POST | 批量删除 |
| /api/users/{id}/enable | POST | 启用用户 |
| /api/users/{id}/disable | POST | 禁用用户 |
| /api/users/{id}/change-password | POST | 修改密码 |
| /api/users/{id}/reset-password | POST | 重置密码 |
| /api/users/{id}/profile | PUT | 修改用户资料 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-09-20 | DTO优化(Create/Update分离、Role枚举化) |
| 2025-10-29 | 初始版本 |
