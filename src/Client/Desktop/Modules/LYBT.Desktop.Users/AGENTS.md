# LYBT.Desktop.Users - Desktop Users Module

**Purpose**: Desktop UI module for user management with Handler component pattern for SRP.

## Structure

```
LYBT.Desktop.Users/
├── CommandHandlers/     # IUserCommandHandler (dead code, not registered)
├── Controls/            # UserMasterDetailControl, UserEditControl, UserViewControl
├── Interfaces/          # IUserRepository, IUserService
├── Mappers/             # UserMapper (Mapperly)
├── Models/              # UserDetailModel, UserItem
├── Repositories/        # UserRepository (DataSource + optional IUserApi)
├── ViewModels/
│   ├── Components/      # UserService (CRUD operations)
│   ├── Handlers/        # IUserPasswordHandler, IUserStatusHandler, IUserImportExportHandler
│   └── UserMasterDetailViewModel.cs
└── UsersModule.cs       # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `UsersModule.cs` | Depends on AuthenticationModule |
| ViewModel logic | `ViewModels/UserMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative |
| Password reset | `ViewModels/Handlers/UserPasswordHandler.cs` | Confirm dialog + UserService |
| Status toggle | `ViewModels/Handlers/UserStatusHandler.cs` | Enable/disable/restore |
| Import/Export | `ViewModels/Handlers/UserImportExportHandler.cs` | Excel operations |

## CONVENTIONS

- **Handler pattern** — Password/Status/ImportExport split into separate handler components
- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>` (V2 composition pattern)
- **DataSource abstraction** — Repository delegates to IUserDataSource (Local/Remote)
- **UserDetailModel.Clone()** — Bypasses RealName setter to avoid PinYin auto-generation

## ANTI-PATTERNS

- **IUserService dead code** — Desktop IUserService not registered, not referenced
- **IUserCommandHandler dead code** — Not registered in DI, only documented
- **UserService.ChangePasswordAsync** — Placeholder implementation (TODO)
- **Cross-module references** — MUST NOT reference other Desktop modules directly
