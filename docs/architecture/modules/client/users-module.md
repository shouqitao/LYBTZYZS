# 用户管理模块 (Users Module) - 客户端

## 模块概述

用户管理模块是凌隐宝堂中医诊所管理系统的核心模块之一，负责用户的创建、编辑、权限管理和状态控制等功能。该模块提供了完整的用户生命周期管理能力。

## 模块位置

```
src/Client/Desktop/Modules/Users/
├── LYBT.Desktop.Users.csproj
├── UsersModule.cs
├── Views/
│   ├── UserManagementView.xaml
│   ├── UserEditView.xaml
│   ├── UserCreateView.xaml
│   └── UserDetailView.xaml
├── ViewModels/
│   ├── UserManagementViewModel.cs
│   ├── UserEditViewModel.cs
│   ├── UserCreateViewModel.cs
│   └── UserDetailViewModel.cs
└── Services/
    └── UserService.cs
```

## 核心功能

### 1. 用户管理
- **用户列表**：分页显示所有用户，支持搜索和筛选
- **创建用户**：新建用户账号，设置角色和权限
- **编辑用户**：修改用户基本信息和角色
- **用户详情**：查看用户完整信息和操作历史
- **状态管理**：启用/禁用用户账号

### 2. 权限控制
- **角色分配**：Admin（管理员）、Doctor（医生）
- **权限验证**：基于角色的访问控制
- **超级管理员保护**：防止创建与超级管理员相同的用户名

### 3. 密码管理
- **密码重置**：管理员可重置用户密码
- **默认密码**：新用户使用系统配置的默认密码
- **首次登录**：提示用户修改默认密码

## 服务层实现

### UserService
```csharp
public class UserService : IUserService
{
    private readonly IHttpClientService _httpClient;

    // 获取用户列表（分页）
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserQueryDto query);

    // 创建新用户
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);

    // 更新用户信息
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto);

    // 删除用户
    Task<ServiceResult<bool>> DeleteAsync(Guid id);

    // 重置密码
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid id);

    // 切换用户状态
    Task<ServiceResult<bool>> ToggleStatusAsync(Guid id);
}
```

## 视图模型架构

### UserManagementViewModel
主列表视图模型，负责：
- 用户列表的加载和刷新
- 搜索和筛选逻辑
- 导航到创建/编辑/详情页面
- 快速操作（状态切换、密码重置）

### UserEditViewModel
编辑视图模型，负责：
- 加载现有用户数据
- 验证输入数据
- 保存更改
- 角色权限修改

### UserCreateViewModel
创建视图模型，负责：
- 初始化新用户表单
- 用户名唯一性验证
- 保留用户名检查
- 创建用户并设置默认密码

## 数据流

```
View (XAML)
    ↓ 绑定
ViewModel (MVVM)
    ↓ 调用
UserService
    ↓ HTTP请求
HttpClientService
    ↓ API调用
WebAPI (/api/v1/users)
```

## 安全特性

### 1. 超级管理员隔离
- 超级管理员不在Users表中显示
- 用户名从配置文件读取，不存储在数据库
- 创建用户时自动检查保留用户名

### 2. 保留用户名列表
```csharp
private static readonly HashSet<string> ReservedUsernames = new()
{
    "admin", "administrator", "root",
    "system", "superadmin", "sysadmin"
};
```

### 3. 权限验证
- 只有Admin角色可以访问用户管理模块
- 操作前验证当前用户权限
- 敏感操作需要二次确认

## 事件通信

### 发布的事件
- `UserCreatedEvent` - 用户创建成功
- `UserUpdatedEvent` - 用户信息更新
- `UserDeletedEvent` - 用户删除
- `UserStatusChangedEvent` - 用户状态变更

### 订阅的事件
- `RefreshUsersEvent` - 刷新用户列表
- `NavigateToUserDetailEvent` - 导航到用户详情

## UI 特性

### 1. 响应式设计
- 自适应列表布局
- 可调整列宽
- 支持键盘快捷键

### 2. 数据验证
- 实时输入验证
- 错误提示显示
- 必填字段标识

### 3. 用户体验
- 加载状态指示器
- 操作成功/失败提示
- 批量操作支持

## 配置项

```json
{
  "Lybt": {
    "Users": {
      "DefaultPassword": "123456",
      "PasswordExpiryDays": 90,
      "MaxLoginAttempts": 5,
      "EnableUserCache": true,
      "CacheDurationMinutes": 30
    }
  }
}
```

## 依赖关系

### 内部依赖
- `LYBT.Desktop.Core` - 基础框架
- `LYBT.Desktop.Infrastructure` - 基础设施
- `LYBT.Desktop.Services` - 共享服务

### 外部依赖
- `LYBT.Shared.Models` - 数据契约
- `LYBT.Shared.Interfaces` - 服务接口

## 性能优化

### 1. 缓存策略
- 用户列表缓存30分钟
- 角色权限缓存
- 增量更新机制

### 2. 延迟加载
- 按需加载用户详情
- 虚拟化列表滚动
- 分页数据获取

## 错误处理

### 1. 网络错误
- 自动重试机制
- 离线状态提示
- 本地缓存回退

### 2. 业务错误
- 用户名重复检查
- 保留用户名验证
- 权限不足提示

## 测试覆盖

### 单元测试
- UserService 方法测试
- ViewModel 逻辑测试
- 数据验证测试

### 集成测试
- API 调用测试
- 权限验证测试
- 事件通信测试

## 未来增强

1. **批量导入**：支持从Excel批量导入用户
2. **审计日志**：记录所有用户操作
3. **双因素认证**：增强账户安全性
4. **密码策略**：复杂度要求和历史记录
5. **在线状态**：显示用户在线/离线状态

## 相关文档

- [服务端用户模块](../server/users-module.md)
- [认证模块](./auth-module.md)
- [权限系统设计](../../security/rbac.md)
- [API 文档](../../../api/users-api.md)