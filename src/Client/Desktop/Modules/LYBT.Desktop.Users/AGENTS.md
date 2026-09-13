# LYBT.Desktop.Users - Desktop Users Module

**Purpose**: Desktop UI module for user management with Handler component pattern for SRP. Exposes embeddable `Controls/` only — it registers no navigation views and no dialogs.

## Structure

```
LYBT.Desktop.Users/
├── Controls/            # UserMasterDetailControl, UserEditControl, UserViewControl
├── Mappers/             # UserMapper (Mapperly: DTO→DetailModel/EditContext, EditContext→InputDto, ApplyToDetailModel)
├── Models/              # UserDetailModel, Items/UserEditContext
├── Repositories/        # UserRepository (EntityApiClientRepositoryBase + IApiClientIdentity)
├── Services/            # UserService (CrudServiceBase + IUserService)
├── ViewModels/
│   ├── Handlers/        # IUserPasswordHandler/UserPasswordHandler, IUserStatusHandler/UserStatusHandler
│   ├── UserMasterDetailViewModel.cs
│   └── UserEditorViewModel.cs   # EditorViewModelBase<UserEditContext>
└── UsersModule.cs       # Prism IModule registration
```

> `IUserService` lives in `LYBT.Desktop.Contracts.Services`; `IUserRepository` in `LYBT.Desktop.Contracts.Repositories`. There is no module-local `Interfaces/` folder.

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `UsersModule.cs` | `[ModuleDependency("AuthenticationModule")]`; registers services, handlers, mapper, MasterDetail, 2 VMs |
| ViewModel logic | `ViewModels/UserMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative; filters + `DefaultRoleFilter` support |
| Password reset | `ViewModels/Handlers/UserPasswordHandler.cs` | Confirm dialog + `IUserService.ResetPasswordAsync` |
| Status toggle | `ViewModels/Handlers/UserStatusHandler.cs` | Hooks: `ExecuteSetStatusAsync` (UserService) / `ExecuteRestoreAsync` (UserRepository) |
| Mapping | `Mappers/UserMapper.cs` | Replaces 3 hand-written mapping sites (D2) |

## CONVENTIONS

- **Handler pattern** — Password/Status split into separate handler components; status handler derives from `BaseStatusHandler<UserListDto>` and only implements the abstract hooks
- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>` + `EditorViewModelBase<UserEditContext>` for the edit sub-VM
- **Service layer** — `UserService : CrudServiceBase<UserListDto, UserDetailDto, UserInputDto>`, `IUserService`（CommandResult 模式，ADR-0020 契约）
- **Cache invalidation** — `UserEditorViewModel.Reset()` calls `IDesktopCacheManager.InvalidateUserCaches()`

## ANTI-PATTERNS

- **Cross-module references** — MUST NOT reference other Desktop modules directly; consumers use `LYBT.Desktop.Contracts.Services.IUserService`
- **Deleting the current user** — single and batch delete both filter out `SessionManager.CurrentUser.Id`

<!-- MANUAL: -->
