# LYBT.Desktop.Users

> 用户管理模块 — Master-Detail CRUD / Handler 组件化 / 密码重置 / 状态切换 / Mapperly 映射

## 项目定位

- **层级**: Client / Desktop / Modules
- **职责**: 系统用户管理界面，支持 CRUD、角色筛选、密码重置、状态切换（启用/禁用/恢复）。采用 MasterDetail 组合模式 + Handler 组件拆分职责
- **状态**: Active
- **依赖**: `AuthenticationModule`

## 目录结构

```
LYBT.Desktop.Users/
├── UsersModule.cs                           # Prism 模块注册 (IModule, 依赖 AuthenticationModule)
├── Interfaces/
│   └── IUserService.cs                      # 用户 Service 接口 (14 方法, CommandResult<T> 模式)
├── Services/
│   └── RemoteUserService.cs                 # IUserService 实现，通过 IUserRepository 调用远程 API
├── ViewModels/
│   ├── UserMasterDetailViewModel.cs         # Master-Detail 主 VM (组合模式)
│   ├── UserEditorViewModel.cs               # 编辑子 VM (ObservableObject, 对象 DP 模式)
│   └── Handlers/
│       ├── IUserPasswordHandler.cs          # 密码处理接口
│       ├── UserPasswordHandler.cs           # 重置密码 (确认 + API + 显示新密码)
│       ├── IUserStatusHandler.cs            # 状态处理接口
│       └── UserStatusHandler.cs             # 切换状态/恢复 (继承 BaseStatusHandler)
├── Controls/
│   ├── UserMasterDetailControl.xaml/.cs     # Master-Detail 可复用控件
│   ├── UserEditControl.xaml/.cs             # 用户编辑控件
│   └── UserViewControl.xaml/.cs             # 用户只读预览控件
├── Models/
│   ├── UserDetailModel.cs                   # Detail 编辑模型 (ValidatableModelBase)
│   └── Items/
│       └── UserEditContext.cs               # 编辑上下文 (ValidateAll)
├── Mappers/
│   └── UserMapper.cs                        # Mapperly 编译时映射器
├── Repositories/
│   └── UserRepository.cs                    # 仓储实现 (委托 IUserRepository)
└── README.md
```

## 核心组件

### UsersModule — 模块入口

**设计依据**: `[ModuleDependency("AuthenticationModule")]`；注册 Handler 组件 + MasterDetail 服务 + ViewModel 映射

| 注册项 | 方式 | 说明 |
|--------|------|------|
| `IUserService` → `RemoteUserService` | `Register<TFrom, TTo>()` | 用户 Service 实现 |
| `IUserPasswordHandler` → `UserPasswordHandler` | `Register<TFrom, TTo>()` | 密码重置 Handler |
| `IUserStatusHandler` → `UserStatusHandler` | `Register<TFrom, TTo>()` | 状态切换 Handler |
| `IMasterDetailServices<UserListDto, UserDetailModel>` | `AddMasterDetailServices<TList, TDetail>()` | MasterDetail 基础设施 |
| `UserEditorViewModel` | `Register<T>()` | 编辑子 VM |
| `UserMasterDetailViewModel` | `Register<T>()` | 主 VM + ViewModelLocationProvider 映射 |

### UserMasterDetailViewModel — Master-Detail 主逻辑

**设计依据**: 继承 `MasterDetailViewModelBase<UserListDto, UserDetailModel>`；组合 `UserEditorViewModel` 处理编辑状态；Handler 组件拆分密码/状态操作实现 SRP

| 命令 | CanExecute | 说明 |
|------|------------|------|
| `ClearFiltersCommand` | `HasActiveFilters` | 清除角色/状态/关键词筛选 |
| `ResetPasswordCommand` | `_passwordHandler.CanResetPassword` | 委托 `IUserPasswordHandler` |
| `ToggleUserStatusCommand` | `_statusHandler.CanToggleUserStatus` | 委托 `IUserStatusHandler`，成功后刷新列表 |
| `RestoreCommand` | `_statusHandler.CanRestore` | 恢复已删除用户（需管理员权限） |

| 筛选属性 | 类型 | 说明 |
|----------|------|------|
| `SelectedRoleFilter` | `UserRole?` | 角色筛选，变更时回到第一页 |
| `SelectedStatusFilter` | `CommonStatus?` | 状态筛选 |
| `ShowInactiveUsers` | `bool` | 显示已禁用用户 |

**抽象方法实现**: `LoadListAsync` (分页 + 客户端过滤) / `LoadDetailAsync` (ID 查询 + 初始化 Editor) / `CreateNewDetail` / `SaveDetailAsync` (Editor 校验 + Create/Update 分支) / `DeleteItemAsync` (禁止删除当前登录用户)

### UserEditorViewModel — 编辑子 ViewModel

**设计依据**: 继承 `ObservableObject`；对象 DP 模式，封装编辑状态供 `UserMasterDetailViewModel` 组合使用

| 方法 | 说明 |
|------|------|
| `InitializeFromDto(UserDetailDto)` | 编辑模式初始化 |
| `InitializeForNewCase()` | 新建模式初始化 |
| `Validate()` | 委托 `UserEditContext.ValidateAll()` |
| `GetUserInput()` | 提取 `UserInputDto` 用于 API 调用 |

| 属性 | 类型 | 说明 |
|------|------|------|
| `User` | `UserEditContext` | `[ObservableProperty]`，编辑上下文 |
| `IsDirty` | `bool` | `[ObservableProperty]`，是否已修改 |

### UserPasswordHandler — 密码重置

**设计依据**: SRP 组件，确认对话框 → API 调用 → 显示新密码；`CanResetPassword` 要求用户状态为 Enabled

| 方法 | 说明 |
|------|------|
| `ResetPasswordAsync(UserListDto)` | 确认 → `IUserService.ResetPasswordAsync` → 显示临时密码 |
| `CanResetPassword(UserListDto?, bool)` | `user != null && !isBusy && Status == Enabled` |

### UserStatusHandler — 状态切换

**设计依据**: 继承 `BaseStatusHandler<UserListDto>`；`ToggleUserStatusAsync` 独立实现走 UserService，`RestoreAsync` 委托基类

| 方法 | 说明 |
|------|------|
| `ToggleUserStatusAsync(UserListDto)` | 切换启用/禁用，成功后返回 `true` 触发刷新 |
| `RestoreAsync(UserListDto)` | 恢复已删除用户 |
| `CanToggleUserStatus(UserListDto?, bool)` | `user != null && !isBusy` |
| `CanRestore(UserListDto?, bool, bool)` | `user != null && !isBusy && isAdmin` |

### RemoteUserService — IUserService 实现

**设计依据**: 14 个方法，统一 `CommandResult<T>` 返回模式；通过 `IUserRepository` 调用远程 API；`ClientErrorMessageMapper` 转换异常为用户友好消息

| 方法分组 | 方法 | 返回类型 |
|----------|------|----------|
| CRUD | `CreateUserAsync` / `UpdateUserAsync` / `DeleteUserAsync` / `BatchDeleteAsync` | `CommandResult<UserDetailDto>` / `CommandResult<bool>` / `CommandResult<BatchOperationResultDto>` |
| 查询 | `GetByIdAsync` / `GetPagedAsync` / `GetAllAsync` / `GetByUsernameAsync` / `SearchAsync` / `GetDoctorsAsync` | `CommandResult<T>` |
| 个人资料 | `ChangeProfileAsync` | `CommandResult<UserDetailDto>` |
| 密码 | `ChangePasswordAsync` / `ResetPasswordAsync` | `CommandResult<bool>` / `CommandResult<ResetPasswordResponseDto>` |
| 状态 | `ToggleStatusAsync` | `CommandResult<UserDetailDto>` |

### UserMapper — Mapperly 编译时映射

**设计依据**: `[Mapper(RequiredMappingStrategy = Target)]`；零运行时开销，替代 AutoMapper

| 方法 | 映射方向 | 说明 |
|------|----------|------|
| `ToItem` | `UserDetailDto` → `UserItem` | API → UI 模型（忽略 15+ UI 计算属性） |
| `ToDto` | `UserItem` → `UserDetailDto` | UI → API（忽略 LastLoginTime/FailedLoginCount/Remark） |
| `ToInputDto` | `UserItem` → `UserInputDto` | 保存调用（手动设置 Id，忽略 Password/ConfirmPassword） |

## 依赖关系

```
LYBT.Desktop.Users
├── LYBT.Desktop.Foundation      (IDesktopCacheManager)
├── LYBT.Desktop.Infrastructure   (MasterDetailViewModelBase, BaseStatusHandler, IMasterDetailServices, IViewModelServices)
├── LYBT.Desktop.Contracts        (IUserRepository)
├── LYBT.Shared.Models            (UserListDto, UserDetailDto, UserInputDto, UserRole, CommonStatus)
└── LYBT.Shared.Primitives        (PinYinHelper)
```

NuGet: `Prism.Core`, `Prism.DryIoc`, `Prism.Wpf`, `Riok.Mapperly`

**被依赖**: `LYBT.Desktop.Admin`（UserManagementView 嵌入 UserMasterDetailControl）

## 设计决策

| 决策 | 原因 |
|------|------|
| Handler 组件拆分 (Password/Status) | ViewModel 职责过重，SRP 拆分；Handler 可独立测试和复用 |
| `UserEditorViewModel` 组合模式 | 编辑状态与列表/导航逻辑解耦，Editor 可独立验证 |
| `CommandResult<T>` 统一返回 | 所有 Service 方法返回 `CommandResult<T>`，统一错误处理路径 |
| `BaseStatusHandler` 继承 | `UserStatusHandler` 复用基类的确认对话框和错误处理模板 |
| Mapperly 编译时映射 | 零运行时开销，编译期生成映射代码，替代 AutoMapper |
| `ViewModelLocationProvider.Register` 手动映射 | Prism 默认约定查找 `UserMasterDetailControlViewModel`（不存在），需显式映射到 `UserMasterDetailViewModel` |

## 已知陷阱

- `RemoteUserService` 通过 `IUserRepository` 调用远程 API，若 Repository 实现抛异常，`ClientErrorMessageMapper` 会转换为用户友好消息，但某些网络异常可能绕过
- `UserEditorViewModel.Validate()` 委托 `UserEditContext.ValidateAll()`，验证失败时 VM 层不设置 ErrorMessage，由调用方（MasterDetailVM）通过 Dialog 显示
- `UserStatusHandler.ToggleUserStatusAsync` 捕获 `HttpRequestException` 但不重新抛出，返回 `false` 静默处理
- `DeleteItemAsync` 禁止删除当前登录用户，通过 `SessionManager?.CurrentUser.Id` 比对实现
- `UserMapper` 使用大量 `[MapperIgnoreTarget]`/`[MapperIgnoreSource]` 属性，新增 `UserItem` 属性时需同步更新忽略列表
