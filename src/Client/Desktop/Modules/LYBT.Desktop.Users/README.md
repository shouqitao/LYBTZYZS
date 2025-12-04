# LYBT.Desktop.Users

> 用户管理模块 | 用户CRUD/角色分配(仅管理员)

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供系统用户管理界面，支持用户创建、编辑、删除、角色分配。仅管理员可访问

## 目录结构

```
LYBT.Desktop.Users/
├── Interfaces/
│   └── IUserRepository.cs           # 用户仓储接口
├── Repositories/
│   └── UserRepository.cs            # 用户仓储实现
├── ViewModels/
│   ├── UserManagementViewModel.cs   # 用户管理ViewModel
│   ├── UserDetailViewModel.cs       # 用户详情ViewModel
│   └── UserItemViewModel.cs         # 用户条目ViewModel
├── Views/
│   ├── UserManagementView.xaml      # 管理视图
│   ├── UserDetailView.xaml          # 详情视图
│   └── UserDetailDialog.xaml        # 编辑对话框
└── UsersModule.cs                    # Prism模块注册
```

## UserManagementViewModel

### 属性(19个)

| 属性 | 类型 | 说明 |
|------|------|------|
| Users | ObservableCollection | 用户列表 |
| SelectedUser | UserDto | 选中的用户 |
| SearchText | string | 搜索关键词 |
| FilterRole | string | 角色筛选 |
| FilterStatus | bool? | 状态筛选(启用/禁用) |
| IsLoading | bool | 加载状态 |
| TotalCount | int | 总数量 |
| PageIndex | int | 当前页码 |
| PageSize | int | 每页数量 |
| CanCreate | bool | 可创建用户 |
| CanEdit | bool | 可编辑用户 |
| CanDelete | bool | 可删除用户 |

### 命令(20个)

| 命令 | 说明 |
|------|------|
| LoadCommand | 加载用户列表 |
| SearchCommand | 搜索用户 |
| CreateCommand | 新建用户 |
| EditCommand | 编辑用户 |
| DeleteCommand | 删除用户 |
| ToggleStatusCommand | 切换启用状态 |
| ResetPasswordCommand | 重置密码 |
| AssignRoleCommand | 分配角色 |
| RefreshCommand | 刷新列表 |
| FilterCommand | 筛选用户 |
| ExportCommand | 导出用户 |

## UserDetailViewModel

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| UserId | Guid | 用户ID |
| Username | string | 用户名 |
| DisplayName | string | 显示名称 |
| Email | string | 邮箱 |
| Phone | string | 手机号 |
| Role | string | 角色(Admin/Doctor/Pharmacist) |
| IsActive | bool | 启用状态 |
| CreatedAt | DateTime | 创建时间 |
| LastLoginAt | DateTime? | 最后登录时间 |
| HasChanges | bool | 变更标记 |

### 命令

| 命令 | 说明 |
|------|------|
| SaveCommand | 保存用户 |
| CancelCommand | 取消编辑 |
| ValidateCommand | 验证数据 |
| ResetPasswordCommand | 重置密码 |

## IUserRepository

| 方法 | 说明 |
|------|------|
| GetAllAsync | 获取所有用户 |
| GetByIdAsync | 按ID获取 |
| GetPagedAsync | 分页查询 |
| SearchAsync | 搜索用户 |
| CreateAsync | 创建用户 |
| UpdateAsync | 更新用户 |
| DeleteAsync | 删除用户 |
| ResetPasswordAsync | 重置密码 |
| UpdateStatusAsync | 更新状态 |

## 角色权限

| 角色 | 权限 |
|------|------|
| Admin | 用户管理全部权限 |
| Doctor | 无访问权限 |
| Pharmacist | 无访问权限 |

## 访问控制

| 控制点 | 说明 |
|--------|------|
| 模块加载 | 仅Admin角色加载此模块 |
| 菜单显示 | 非Admin不显示用户管理菜单 |
| 路由守卫 | 导航时检查角色权限 |

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Infrastructure (ISessionManager/权限检查)
- LYBT.Desktop.Contracts (IUserApi)
- LYBT.Shared.Models (UserDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载/权限控制)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
