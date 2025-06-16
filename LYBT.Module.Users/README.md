当然！下面为你输出**LYBT.Module.Users 模块的 README.md**（标准格式、可直接用于源码根目录），内容包含模块简介、功能列表、典型DTO与接口用法、依赖关系、常见注意事项、快速上手等内容。
 如需英文版或中英文双语，可进一步说明。

------

# LYBT.Module.Users

## 模块简介

`LYBT.Module.Users` 是凌隐宝堂系统的【用户管理】核心模块，负责用户账号、角色、权限等相关的所有数据存取与业务逻辑，包括注册、登录、信息维护、批量/分页管理等，支持后台和API一体化开发。

------

## 主要功能

- 用户信息的新增、编辑、删除、批量删除
- 用户登录校验（含密码加密、禁用校验）
- 用户详情查询与用户列表分页查询
- 用户唯一性校验
- 批量操作与软删除
- 密码重置、状态修改等扩展操作
- DTO 自动注解校验，API 自动文档
- 支持 EF Core/AutoMapper 组件集成

------

## 目录结构

```plaintext
LYBT.Module.Users
│
├─ Models
│   ├─ UserModel.cs
│   └─ Dtos
│       ├─ UserCreateDto.cs
│       ├─ UserEditDto.cs
│       ├─ UserDto.cs
│       ├─ UserLoginDto.cs
│       ├─ UserLoginResultDto.cs
│       ├─ UserPagedQueryDto.cs
│       └─ UserBatchDeleteDto.cs
├─ Interfaces
│   ├─ IUserRepository.cs
│   └─ IUserService.cs
├─ Repositories
│   └─ UserRepository.cs
├─ Services
│   └─ UserService.cs
├─ Mapping
│   └─ UserMappingProfile.cs
└─ README.md
```

## 接口说明

- `IUserService`：用户业务服务接口
- `IUserRepository`：用户数据库仓储接口，使用 `AppDbContext` 持久化

------

## 依赖说明

- `LYBT.Common.Enums`：枚举类型，如用户角色等
- `LYBT.Common.Models`：通用分页等DTO
- `LYBT.Common.Helpers`：密码加密等工具类
- `AutoMapper`：对象映射
- `Microsoft.EntityFrameworkCore`：ORM数据库操作

------

## DTO 注解与默认值说明

- 所有 DTO 字段均采用 `[Required]`、`[StringLength]`、`[Range]` 等注解进行自动参数校验
- 字符串、布尔、枚举等属性均有合理默认值，支持前后端分离与自动文档生成

------

## 典型接口用法示例

### 新增用户

```json
POST /api/users/add
{
  "username": "doctor001",
  "realName": "张三",
  "password": "abc12345",
  "role": 2
}
```

> 返回：`{ "success": true }`

------

### 用户分页查询

```json
POST /api/users/paged
{
  "username": "do",
  "pageIndex": 1,
  "pageSize": 20
}
```

> 返回：

```json
{
  "totalCount": 35,
  "items": [
    { "id": "...", "username": "doctor001", "realName": "张三", "role": 2, "isActive": true }
  ]
}
```

------

### 登录校验

```json
POST /api/users/login
{
  "username": "doctor001",
  "password": "abc12345"
}
```

> 返回：

- 成功：`{"success":true,"id":"...","username":"doctor001","realName":"张三","role":2,"isActive":true,"message":"登录成功"}`
- 失败：`{"success":false,"message":"用户名或密码错误"}`

------

## 代码风格约定

- **接口与实现分离**，接口全部用`Task<T>`异步风格
- **自动化注解校验**（DTO注解+Service兜底），保证参数安全
- **全链路默认值**（DTO/Model均赋初值，防空防错）
- **所有接口全中文注释，方便团队协作与维护**

------

## 快速集成

1. 在 `Startup.cs` 注册 UserService/UserRepository/AutoMapper 配置
2. 引入 DTO、接口和实现
3. 通过依赖注入（DI）自动调用业务逻辑
4. 可直接用于 ASP.NET Core WebAPI 或桌面/WPF等多端项目

------

## 常见问题

- **参数缺失/格式错误**：会自动在接口返回400错误，前端可直接展示
- **唯一性校验**：后端Service层始终二次防御，防止接口外调用绕过
- **批量操作/分页**：建议用带默认值和注解的DTO，API会自动补全缺省参数
- **密码加密/校验**：采用 SHA256，具体可在`LYBT.Common.Helpers.PasswordHelper`自定义实现

------

## 联系方式与反馈

如需问题反馈、定制开发或代码咨询，请联系开发负责人或在项目 issue 区留言。

------

如需英文版、详细示例或Swagger文档导出，请继续提出！
