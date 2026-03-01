# LYBT.Desktop.Users 代码知识

## 模块概述

用户管理模块 -- Desktop 端用户 CRUD、角色分配、密码重置、状态切换、批量导入导出。采用 MasterDetail 组合模式，通过 Handler 组件拆分密码/状态/导入导出等职责，供 Admin 角色台复用。

### 架构分层

```
UserMasterDetailControl (可复用 UI 控件，由 Admin 角色台嵌入)
  |
UserMasterDetailViewModel (组合模式 ViewModel)
  |
  +-- UserService (CRUD/查询/状态/密码操作)
  +-- IUserPasswordHandler -> UserPasswordHandler (密码重置)
  +-- IUserStatusHandler -> UserStatusHandler (状态切换/恢复)
  +-- IUserImportExportHandler -> UserImportExportHandler (Excel 导入导出)
  |
IUserRepository -> UserRepository (数据仓储，委托 IUserDataSource / IUserApi)
```

### DI 注册 (UsersModule.cs)

```csharp
containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();
containerRegistry.Register<UserService>();
containerRegistry.Register<IUserPasswordHandler, UserPasswordHandler>();
containerRegistry.Register<IUserStatusHandler, UserStatusHandler>();
containerRegistry.Register<IUserImportExportHandler, UserImportExportHandler>();
containerRegistry.AddMasterDetailServices<UserListDto, UserDetailModel>();
containerRegistry.Register<UserMasterDetailViewModel>();
```

### 模块依赖: `[ModuleDependency("AuthenticationModule")]`

## 架构决策

| 决策 | 原因 | 关联 OpenSpec |
|------|------|--------------|
| Handler 组件拆分 (Password/Status/ImportExport) | ViewModel 职责过重，SRP 拆分 | refactor-frontend-srp-patterns |
| MasterDetailControl 复用模式 | 供 Admin 角色台 UserManagementView 嵌入 | refactor-admin-workspace |
| UserItem (BindableBase) 替代直接使用 DTO | Desktop 层与 Shared 层解耦，支持 UI 计算属性 | resolve-mapperly-source-generator-conflict |
| Mapperly 编译时映射 | 零运行时开销，替代 AutoMapper | adopt-mapperly-unified-mapping |
| UserDetailModel (ValidatableModelBase) | Detail 区域使用可验证模型，支持 DataAnnotations | refactor-master-detail-layout |
| UserRepository 双模式 (DataSource + API) | 通过 IUserDataSource 支持 Local/Remote，IUserApi 可选注入 (仅 Remote 高级功能) | - |

## 代码文件结构

### 模块注册

| 文件 | 类名 | 基类/接口 | 职责 |
|------|------|-----------|------|
| UsersModule.cs | UsersModule | IModule | Prism 模块注册，注册 Repository/Service/Handler/ViewModel |

### 接口层 (Interfaces/)

| 文件 | 类名 | 职责 |
|------|------|------|
| IUserRepository.cs | IUserRepository | 用户数据仓储接口 (CRUD + 搜索 + 密码 + 状态 + 批量操作) |
| IUserService.cs | IUserService | 用户 Service 接口 (CRUD + 查询 + 个人资料 + 密码管理) |

#### IUserRepository 核心方法

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| GetPagedAsync | `Task<PagedResult<UserListDto>>` | 分页查询 (ListDto) |
| GetByIdAsync | `Task<UserDetailDto?>` | 按 ID 获取详情 |
| CreateAsync | `Task<UserDetailDto>` | 创建用户 |
| UpdateAsync | `Task<UserDetailDto>` | 更新用户 |
| DeleteAsync | `Task<bool>` | 软删除 |
| GetByUsernameAsync | `Task<UserDetailDto>` | 按用户名查询 |
| SearchAsync | `Task<List<UserListDto>>` | 关键词搜索 |
| GetDoctorsAsync | `Task<List<UserListDto>>` | 获取启用状态的医生 |
| ChangeProfileAsync | `Task<UserDetailDto>` | 修改个人资料 (仅 Remote) |
| ChangePasswordAsync | `Task<ServiceResult>` | 修改密码 |
| ResetPasswordAsync | `Task<ServiceResult<ResetPasswordResponseDto>>` | 管理员重置密码 (仅 Remote) |
| BatchImportAsync | `Task<UserBatchImportResultDto?>` | 批量导入 (仅 Remote) |
| ToggleStatusAsync | `Task<UserDetailDto?>` | 切换启用/禁用 |
| RestoreAsync | `Task<UserDetailDto?>` | 恢复已删除用户 (仅 Remote) |
| BatchDeleteAsync | `Task<BatchOperationResultDto?>` | 批量删除 |
| BatchEnableAsync | `Task<BatchOperationResultDto?>` | 批量启用 (仅 Remote) |
| BatchDisableAsync | `Task<BatchOperationResultDto?>` | 批量禁用 (仅 Remote) |

### 数据仓储层 (Repositories/)

| 文件 | 类名 | 基类/接口 | 职责 |
|------|------|-----------|------|
| UserRepository.cs | UserRepository | IUserRepository | 数据仓储实现，委托 IUserDataSource (Local/Remote) + 可选 IUserApi (Remote 高级功能) |

关键设计: `IUserApi?` 为可选注入，Local 模式下为 null，部分功能 (ChangeProfile/ResetPassword/BatchImport/Restore/BatchEnable/BatchDisable) 仅 Remote 模式可用。

### 模型层 (Models/)

| 文件 | 类名 | 基类 | 职责 |
|------|------|------|------|
| Models/UserDetailModel.cs | UserDetailModel | ValidatableModelBase | Detail 区域可编辑模型，含 DataAnnotations 验证 |
| Models/Items/UserItem.cs | UserItem | BindableBase | 列表项 UI 模型 (替代直接使用 DTO) |

#### UserDetailModel 验证规则

| 属性 | 验证 |
|------|------|
| UserName | Required, StringLength(3-50) |
| RealName | Required, StringLength(max 100)，设置时自动生成 PinYinCode |
| PhoneNumber | Phone, StringLength(max 20) |
| Email | EmailAddress |
| Remark | StringLength(max 1000) |

#### UserItem 计算属性

| 属性 | 计算逻辑 |
|------|----------|
| RoleDisplayText | Admin -> "管理员", Doctor -> "医师" |
| RoleColor | Admin -> "#9C27B0", Doctor -> "#2196F3" |
| StatusText | Enabled -> "正常", Disabled -> "禁用" |
| IsActive | Status == Enabled |
| CanEdit | IsActive |
| CanDelete | UserName != "sysadmin" |
| CanResetPassword | IsActive |
| DisplayText | "{RealName}({UserName}) - {RoleDisplayText}" |

### 映射层 (Mappers/)

| 文件 | 类名 | 基类 | 职责 |
|------|------|------|------|
| UserMapper.cs | UserMapper | Mapperly [Mapper] | 编译时映射: UserDetailDto <-> UserItem <-> UserInputDto |

#### 映射方法

| 方法 | 映射方向 | 说明 |
|------|----------|------|
| ToItem | UserDetailDto -> UserItem | 从 API 加载到 UI 模型 |
| ToDto | UserItem -> UserDetailDto | 保存到 API |
| ToInputDto | UserItem -> UserInputDto | 创建/更新 API 调用 (手动设置 Id) |

### Service 层 (ViewModels/Components/)

| 文件 | 类名 | 职责 |
|------|------|------|
| UserService.cs | UserService | 用户命令操作 (CRUD + 查询 + 状态 + 密码) |

#### UserService 核心方法

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| CreateAsync | `(bool, UserDetailDto?, string?)` | 创建用户 |
| UpdateAsync | `(bool, UserDetailDto?, string?)` | 更新用户 |
| DeleteAsync | `(bool, string?)` | 删除用户 |
| BatchDeleteAsync | `(bool, BatchOperationResultDto?, string?)` | 批量删除 |
| GetByIdAsync | `(bool, UserDetailDto?, string?)` | 按 ID 查询 |
| GetPagedAsync | `(bool, PagedResult<UserListDto>?, string?)` | 分页查询 (返回 ListDto) |
| GetByUsernameAsync | `(bool, UserDetailDto?, string?)` | 按用户名查询 |
| SearchAsync | `(bool, List<UserListDto>?, string?)` | 搜索 |
| GetDoctorsAsync | `(bool, List<UserListDto>?, string?)` | 获取医生列表 |
| ChangeProfileAsync | `(bool, UserDetailDto?, string?)` | 修改个人资料 |
| ToggleStatusAsync | `(bool, UserDetailDto?, string?)` | 切换状态 |
| ChangePasswordAsync | `(bool, string?)` | 修改密码 (占位实现) |
| ResetPasswordAsync | `(bool, string?, ResetPasswordResponseDto?)` | 重置密码 |

### Handler 层 (ViewModels/Handlers/)

| 文件 | 接口 | 实现类 | 职责 |
|------|------|--------|------|
| IUserPasswordHandler.cs | IUserPasswordHandler | - | 密码处理接口 |
| UserPasswordHandler.cs | - | UserPasswordHandler | 重置密码 (确认对话框 + 调用 UserService) |
| IUserStatusHandler.cs | IUserStatusHandler | - | 状态处理接口 |
| UserStatusHandler.cs | - | UserStatusHandler | 切换状态/恢复用户 |
| IUserImportExportHandler.cs | IUserImportExportHandler | - | 导入导出接口 |
| UserImportExportHandler.cs | - | UserImportExportHandler | Excel 导入/导出/模板下载 |

#### IUserPasswordHandler 方法

| 方法 | 说明 |
|------|------|
| ResetPasswordAsync(UserListDto) | 重置用户密码 |
| CanResetPassword(UserListDto?, bool) | 判断是否可重置 (user != null && !isBusy && Enabled) |

#### IUserStatusHandler 方法

| 方法 | 说明 |
|------|------|
| ToggleUserStatusAsync(UserListDto) | 切换启用/禁用 |
| RestoreAsync(UserListDto) | 恢复已删除用户 |
| CanToggleUserStatus(UserListDto?, bool) | 判断是否可切换 |
| CanRestore(UserListDto?, bool, bool) | 判断是否可恢复 (需管理员) |

#### IUserImportExportHandler 方法

| 方法 | 说明 |
|------|------|
| ImportAsync() | Excel 导入用户 (返回是否需刷新) |
| ExportAsync(string?) | 导出用户到 Excel |
| DownloadTemplateAsync() | 下载导入模板 |

### CommandHandler 层 (CommandHandlers/)

| 文件 | 类名 | 基类/接口 | 职责 |
|------|------|-----------|------|
| IUserCommandHandler.cs | IUserCommandHandler | ICommandHandlerBase<UserListDto, UserDetailDto, UserInputDto> | CommandHandler 接口 |
| UserCommandHandler.cs | UserCommandHandler | IUserCommandHandler | CRUD + 搜索 + 密码重置 + 状态切换 |

### ViewModel 层

| 文件 | 类名 | 基类 | 职责 |
|------|------|------|------|
| UserMasterDetailViewModel.cs | UserMasterDetailViewModel | MasterDetailViewModelBase<UserListDto, UserDetailModel> | 组合模式 ViewModel |

#### UserMasterDetailViewModel Commands

| 命令 | CanExecute | 说明 |
|------|------------|------|
| ClearFiltersCommand | HasActiveFilters | 清除角色/状态/关键词筛选 |
| ResetPasswordCommand | CanResetPassword | 重置选中用户密码 |
| ToggleUserStatusCommand | CanToggleUserStatus | 切换选中用户状态 |
| RestoreCommand | CanRestore | 恢复选中的已删除用户 |
| ImportCommand | - | 导入用户 (Excel) |
| ExportCommand | - | 导出用户 (Excel) |
| DownloadTemplateCommand | - | 下载导入模板 |

#### UserMasterDetailViewModel 扩展属性

| 属性 | 类型 | 说明 |
|------|------|------|
| SelectedRoleFilter | UserRole? | 角色筛选，变更时回到第一页 |
| SelectedStatusFilter | CommonStatus? | 状态筛选 |
| ShowInactiveUsers | bool | 显示已禁用用户 |
| IsAdmin | bool | 当前用户是否管理员 |
| IsUserNameReadOnly | bool | 编辑模式下用户名只读 |
| RoleOptions | ObservableCollection<UserRole> | 角色选项列表 |
| StatusOptions | ObservableCollection<CommonStatus> | 状态选项列表 |
| DetailTitle | string | 动态标题 ("新增用户" / "编辑用户 - XXX" / "用户详情 - XXX") |

### View/Control 层

| 文件 | 类名 | 基类 | 职责 |
|------|------|------|------|
| Controls/UserMasterDetailControl.xaml.cs | UserMasterDetailControl | MasterDetailControlBase | Master-Detail 可复用控件，构造时初始化 UserMasterDetailViewModel |
| Controls/UserViewControl.xaml.cs | UserViewControl | UserControl | 用户预览控件 (DependencyProperty 绑定) |
| Controls/UserEditControl.xaml.cs | UserEditControl | UserControl | 用户编辑控件 (双向绑定 + 验证错误源) |

#### UserViewControl DependencyProperties

UserName, RealName, PinYinCode, Role, PhoneNumber, Email, Status, LastLoginTime, CreatedAt, UpdatedAt, ShowStatus

#### UserEditControl DependencyProperties

UserName, IsUserNameReadOnly, RealName, PinYinCode, PhoneNumber, Email, Role, RoleOptions, Status, StatusOptions, ShowStatus, ErrorsSource

## 死代码与废弃标记

| 类型 | 文件 | 状态 | 说明 |
|------|------|------|------|
| IUserService | Interfaces/IUserService.cs | **疑似死代码** | Desktop.Users 命名空间内的 IUserService 未被任何文件引用，未在 DI 注册；与 Server 端同名接口无关 |
| IUserCommandHandler / UserCommandHandler | CommandHandlers/ | **疑似死代码** | 仅自身文件内互相引用，未在 UsersModule.cs 注册，未被 ViewModel 使用 |
| UserItem | Models/Items/UserItem.cs | **低活跃** | 仅被 UserMapper.cs 引用，UserMasterDetailViewModel 未使用 UserItem，直接构造 UserDetailModel |

## 已知陷阱

- `UserService.ChangePasswordAsync` 是占位实现 (TODO)，始终返回 (true, "修改密码功能开发中")
- `UserRepository` 构造函数的 `IUserApi? api = null` 在 Local 模式下为 null，调用 Remote-only 方法时会返回 null/NotSupportedException
- `UserDetailModel.Clone()` 直接赋值私有字段 `_realName` / `_pinYinCode`，避免触发 RealName setter 中的自动拼音生成
- `UserMasterDetailControl` 在构造函数中调用 `InitializeViewModel<UserMasterDetailViewModel>()`，ViewModel 不通过 Prism Navigation 注入

## 依赖关系

### 项目依赖
- LYBT.Desktop.Foundation (BaseApiRepository/Security)
- LYBT.Desktop.Infrastructure (MasterDetailControlBase/ViewModelBase/Services)
- LYBT.Desktop.Models (ViewModelBase/ValidatableModelBase)
- LYBT.Desktop.Contracts (IUserApi/IUserDataSource/ICommandHandlerBase)
- LYBT.Desktop.Utilities (ExcelHelper)
- LYBT.Shared.Models (UserListDto/UserDetailDto/UserInputDto/枚举)
- LYBT.Shared.Primitives (ValidationConstants)
- Riok.Mapperly (编译时映射)

### 被依赖
- LYBT.Desktop.Admin (UserManagementView 嵌入 UserMasterDetailControl)
- LYBT.Desktop.Shell (AccountSettingsViewModel 使用 IUserRepository)

---

最后更新: 2026-03-01
