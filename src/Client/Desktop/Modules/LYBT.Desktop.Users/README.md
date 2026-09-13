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
├── Services/
│   └── UserService.cs                       # IUserService 实现（CrudServiceBase 派生，经 IUserRepository 访问远程 API）
├── ViewModels/
│   ├── UserMasterDetailViewModel.cs         # Master-Detail 主 VM (组合模式)
│   ├── UserEditorViewModel.cs               # 编辑子 VM (EditorViewModelBase<UserEditContext>)
│   └── Handlers/
│       ├── IUserPasswordHandler.cs          # 密码处理接口
│       ├── UserPasswordHandler.cs           # 重置密码 (确认 + API + 显示新密码)
│       ├── IUserStatusHandler.cs            # 状态处理接口
│       └── UserStatusHandler.cs             # 切换状态/恢复 (继承 BaseStatusHandler)
├── Controls/
│   ├── UserMasterDetailControl.xaml/.cs     # Master-Detail 可复用控件
│   ├── UserEditControl.xaml/.cs             # 用户编辑控件
│   └── UserViewControl.xaml/.cs             # 用户只读预览控件
├── Mappers/
│   └── UserMapper.cs                        # Mapperly 编译时映射 (DTO↔Model↔EditContext↔InputDto)
├── Models/
│   ├── UserDetailModel.cs                   # Detail 编辑模型 (ValidatableModelBase)
│   └── Items/
│       └── UserEditContext.cs               # 编辑上下文 (ValidateAll)
├── Repositories/
│   └── UserRepository.cs                    # 仓储实现（EntityApiClientRepositoryBase + IUserRepository）
└── README.md
```

> 本模块**无 `Interfaces/` 与 `Views/`**：`IUserService` 定义在 `LYBT.Desktop.Contracts.Services`，`IUserRepository` 在 `LYBT.Desktop.Contracts.Repositories`；模块不注册导航视图，仅向角色台提供可嵌入的 `Controls/`。

## 视图 / ViewModel 清单

**计数口径**：View = 页面/导航级 XAML（`*/Views/*.xaml`）；Control = 内嵌组件（`*/Controls/*.xaml`）；Dialog = `*/Dialogs/**/*.xaml`；ViewModel 按「每文件 1 个 VM 类型」计。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 0 | 本模块不注册导航视图 |
| Control | 3 | `UserMasterDetailControl`、`UserEditControl`、`UserViewControl` |
| Dialog | 0 | — |
| ViewModel | 2 | `UserMasterDetailViewModel`、`UserEditorViewModel`（`ViewModels/Handlers/` 下 4 个文件为 Handler，非 VM） |

> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

### UsersModule — 模块入口

**设计依据**: `[ModuleDependency("AuthenticationModule")]`；注册 Handler 组件 + MasterDetail 服务 + ViewModel 映射

| 注册项 | 方式 | 说明 |
|--------|------|------|
| `IUserService` → `UserService` | `Register<TFrom, TTo>()` | 用户 Service 实现（`IUserRepository` 由 Shell DI 注册） |
| `IUserPasswordHandler` → `UserPasswordHandler` | `Register<TFrom, TTo>()` | 密码重置 Handler |
| `IUserStatusHandler` → `UserStatusHandler` | `Register<TFrom, TTo>()` | 状态切换 Handler |
| `IMasterDetailServices<UserListDto, UserDetailModel>` | `AddMasterDetailServices<TList, TDetail>()` | MasterDetail 基础设施 |
| `UserMapper` | `RegisterSingleton<T>()` | Mapperly 映射器（无状态，单例） |
| `UserEditorViewModel` | `Register<T>()` | 编辑子 VM |
| `UserMasterDetailViewModel` | `Register<T>()` | 主 VM + `ViewModelLocationProvider.Register(UserMasterDetailControl → UserMasterDetailViewModel)` |

### UserMasterDetailViewModel — Master-Detail 主逻辑

**设计依据**: 继承 `MasterDetailViewModelBase<UserListDto, UserDetailModel>`；组合 `UserEditorViewModel` 处理编辑状态；Handler 组件拆分密码/状态操作实现 SRP

| 命令 | CanExecute | 说明 |
|------|------------|------|
| `ClearFiltersCommand` | `HasActiveFilters` | 清除角色/状态/关键词筛选 |
| `ResetPasswordCommand` | `_passwordHandler.CanResetPassword` | 委托 `IUserPasswordHandler` |
| `ToggleUserStatusCommand` | `_statusHandler.CanToggleUserStatus` | 委托 `IUserStatusHandler`，成功后刷新列表 |
| `RestoreCommand` | `CanRestore()`（基类） | 恢复已删除用户（需管理员权限） |

| 筛选属性 | 类型 | 说明 |
|----------|------|------|
| `SelectedRoleFilter` | `UserRole?` | 角色筛选，变更时回到第一页 |
| `SelectedStatusFilter` | `CommonStatus?` | 状态筛选 |
| `ShowInactiveUsers` | `bool` | 显示已禁用用户 |

**抽象方法实现**: `LoadListAsync` (分页 + 客户端过滤) / `LoadDetailAsync` (ID 查询 + 初始化 Editor) / `CreateNewDetail` / `SaveDetailAsync` (Editor 校验 + Create/Update 分支) / `DeleteItemAsync` (禁止删除当前登录用户)

### UserEditorViewModel — 编辑子 ViewModel

**设计依据**: 继承 `EditorViewModelBase<UserEditContext>`（D3 对齐 Patients/Catalog：原先的 `ObservableObject` 手写 DP 已收敛到基类）；映射经 Mapperly `UserMapper`（D2）；`Reset()` 覆写后失效用户缓存

| 方法 | 说明 |
|------|------|
| `InitializeFromDto(UserDetailDto)` | 编辑模式初始化（`UserMapper.ToEditContext`，保留 PinYinCode 回退） |
| `InitializeForNewCase()` | 新建模式初始化（基类实现，`CreateNewContext()` 返回 `UserEditContext.CreateNew()`） |
| `Validate()`（基类） | 委托 `UserEditContext.ValidateAll()` |
| `GetUserInput()` | `UserMapper.ToInputDto`（保留 Trim 行为） |
| `Reset()`（覆写） | 基类重置 + `IDesktopCacheManager.InvalidateUserCaches()` |

| 属性 | 类型 | 说明 |
|------|------|------|
| `User` | `UserEditContext` | 编辑上下文（XAML 绑定目标，等价于基类 `Context`） |
| `IsDirty` | `bool` | 基类提供，是否已修改 |

### UserPasswordHandler — 密码重置

**设计依据**: SRP 组件，确认对话框 → API 调用 → 显示新密码；`CanResetPassword` 要求用户状态为 Enabled

| 方法 | 说明 |
|------|------|
| `ResetPasswordAsync(UserListDto)` | 确认 → `IUserService.ResetPasswordAsync` → 显示临时密码 |
| `CanResetPassword(UserListDto?, bool)` | `user != null && !isBusy && Status == Enabled` |

### UserStatusHandler — 状态切换

**设计依据**: 继承 `BaseStatusHandler<UserListDto>`（`ToggleStatusAsync`/`RestoreAsync` 模板方法）；钩子实现 `ExecuteSetStatusAsync`（走 `IUserService.SetStatusAsync`）与 `ExecuteRestoreAsync`（走 `IUserRepository.RestoreAsync`）

| 方法 | 说明 |
|------|------|
| `ToggleUserStatusAsync(UserListDto)` | 转发基类 `ToggleStatusAsync`，成功后返回 `true` 触发刷新 |
| `RestoreAsync(UserListDto)` | 基类模板：恢复已删除用户 |
| `CanToggleUserStatus(UserListDto?, bool)` | `user != null && !isBusy` |

### UserService — IUserService 实现

**设计依据**: 继承 `CrudServiceBase<UserListDto, UserDetailDto, UserInputDto>`（`Core/LYBT.Desktop.Contracts/Services/ICrudService.cs` 契约）；统一 `CommandResult<T>` 返回模式；通过 `IUserRepository` 访问远程 API；`ClientErrorMessageMapper` 转换异常为用户友好消息

| 方法分组 | 方法 | 返回类型 |
|----------|------|----------|
| CRUD（基类） | `CreateAsync` / `UpdateAsync` / `DeleteAsync` / `GetByIdAsync` / `GetPagedAsync` / `SearchAsync` | `CommandResult<T>` |
| 查询（模块） | `GetAllAsync`（`new`，内部分页拉全量） / `GetByUsernameAsync` / `GetDoctorsAsync` | `CommandResult<T>` |
| 批量 | `BatchDeleteAsync` / `BatchSetStatusAsync`（另含 `[Obsolete]` 的 `BatchEnableAsync`/`BatchDisableAsync`） | `CommandResult<BatchOperationResultDto>` |
| 个人资料 | `ChangeProfileAsync` | `CommandResult<UserDetailDto>` |
| 密码 | `ChangePasswordAsync` / `ResetPasswordAsync` | `CommandResult<bool>` / `CommandResult<ResetPasswordResponseDto>` |
| 状态 | `SetStatusAsync`（另含 `[Obsolete]` 的 `ToggleStatusAsync`） | `CommandResult<UserDetailDto>` |

## 依赖关系

### 依赖（编译时 ProjectReference）

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Foundation | `IDesktopCacheManager`（编辑重置后失效用户缓存） |
| LYBT.Desktop.Infrastructure | `MasterDetailViewModelBase`、`EditorViewModelBase`、`BaseStatusHandler`、`IMasterDetailServices`、`IViewModelServices`、`DependencyInjection` |
| LYBT.Desktop.Contracts | `Services.IUserService`、`Repositories.IUserRepository`、`ApiClient.IApiClientIdentity`、`Results` |
| LYBT.Shared.Models | `UserListDto` / `UserDetailDto` / `UserInputDto`、`UserRole`、`CommonStatus`、`Utilities.Text.PinYinHelper` |

传递依赖：`LYBT.Shared.ExceptionHandling`（`ClientErrorMessageMapper`，经 Foundation/Infrastructure）。
NuGet 直接引用：`Prism.Core` / `Prism.DryIoc` / `Prism.Wpf`、`Riok.Mapperly`。

### 被依赖

| 消费方 | 接口/控件 | 说明 |
|--------|-----------|------|
| `Roles/LYBT.Desktop.Admin` `UserManagementView` | `UserMasterDetailControl` | 薄包装（支持 `DefaultRoleFilter` 导航参数） |
| `Roles/LYBT.Desktop.Admin` `Sysadmin` `SysadminHomeView` | 导航 `ViewNames.UserManagement` | 运维台入口复用同一视图 |
| `Registrations` `RegistrationCreateDialogViewModel` | `Contracts.Services.IUserService` | 医生下拉列表（经接口，无模块引用） |
| `Roles/LYBT.Desktop.Clinical` `ReceptionistHomeViewModel` | `Contracts.Services.IUserService` | 前台医生列表 |

> 跨模块仅经 `LYBT.Desktop.Contracts` 接口或共享 Control 交互；`PatientsModule` / `RegistrationModule` 另以 `[ModuleDependency("UsersModule")]` 声明运行时加载顺序。

## 设计决策

| 决策 | 原因 |
|------|------|
| Handler 组件拆分 (Password/Status) | ViewModel 职责过重，SRP 拆分；Handler 可独立测试和复用 |
| `UserEditorViewModel` 组合模式 | 编辑状态与列表/导航逻辑解耦，Editor 可独立验证 |
| `CommandResult<T>` 统一返回 | 所有 Service 方法返回 `CommandResult<T>`，统一错误处理路径 |
| `BaseStatusHandler` 继承 | `UserStatusHandler` 只实现 `EntityTypeName`/`GetEntityId`/`GetEntityDisplayName`/`GetEntityStatus` + 两个执行钩子，确认对话框与错误处理模板由基类复用 |
| Mapperly 编译时映射 | 零运行时开销，编译期生成映射代码，替代 AutoMapper；`UserMapper` 同时服务 DetailModel / EditContext / InputDto 三向映射，消除三处手写字段映射 |
| `ViewModelLocationProvider.Register` 手动映射 | Prism 默认约定查找 `UserMasterDetailControlViewModel`（不存在），需显式映射到 `UserMasterDetailViewModel` |

## 已知陷阱

- `UserService` 通过 `IUserRepository` 调用远程 API，Repository 内部经 `IApiClientIdentity` 访问服务端；异常在 Service/Repository 层由 `ClientErrorMessageMapper` 转为用户友好消息，但某些网络异常可能绕过
- `UserEditorViewModel.Validate()` 委托 `UserEditContext.ValidateAll()`，验证失败时 VM 层不设置 ErrorMessage，由调用方（MasterDetailVM）通过 Dialog 显示
- `UserStatusHandler` 不吞异常：状态切换/恢复的异常由 `BaseStatusHandler` 统一 `catch` → 记日志 + `ShowErrorAsync`，返回 `false`；失败不会静默
- `DeleteItemAsync` 禁止删除当前登录用户（`SessionManager?.CurrentUser.Id` 比对）；批量删除同样过滤当前登录用户并提示被跳过的条数
- `UserMapper` 与 CommunityToolkit 源生成器互斥：Item 类使用 `[ObservableProperty]` 时 Mapperly 看不到生成成员，需要 `[MapperIgnoreSource/Target]` + 手动补映射（`UserDetailModel.PinYinCode` 即此模式）

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）
