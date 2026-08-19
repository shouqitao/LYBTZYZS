# LYBT.Desktop.Users - Desktop Users Module

**Purpose**: Desktop UI module for user management with Handler component pattern for SRP.

## Structure

```
LYBT.Desktop.Users/
├── Controls/            # UserMasterDetailControl, UserEditControl, UserViewControl
├── Interfaces/          # IUserRepository, IUserService
├── Models/              # UserDetailModel
├── Repositories/        # UserRepository
├── Services/            # UserService (CRUD + 密码/资料管理)
├── ViewModels/
│   ├── Handlers/        # IUserPasswordHandler, IUserStatusHandler
│   └── UserMasterDetailViewModel.cs
├── Views/               # UserMasterDetailView XAML views
└── UsersModule.cs       # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `UsersModule.cs` | Depends on AuthenticationModule |
| ViewModel logic | `ViewModels/UserMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative |
| Password reset | `ViewModels/Handlers/UserPasswordHandler.cs` | Confirm dialog + UserService |
| Status toggle | `ViewModels/Handlers/UserStatusHandler.cs` | Enable/disable/restore |

## CONVENTIONS

- **Handler pattern** — Password/Status split into separate handler components
- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>` (V2 composition pattern)
- **Service layer** — `UserService` extends `CrudServiceBase` (CommandResult 模式，ADR-0020 契约)
- **UserDetailModel.Clone()** — Bypasses RealName setter to avoid PinYin auto-generation

## ANTI-PATTERNS

- **Cross-module references** — MUST NOT reference other Desktop modules directly
