# LYBT.Module.Users - Server Users Module

**Purpose**: Server-side user management with Admin/Doctor dual-role system.

## Structure

```
LYBT.Module.Users/
├── Interfaces/          # IUserService, IUserRepository
├── Services/            # UserService (807 lines)
├── Repositories/        # UserRepository
├── Mapping/             # UserMapper (Mapperly)
├── Validators/          # FluentValidation
└── UsersModule.cs       # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| CRUD + password mgmt | `Services/UserService.cs` | 807 lines |
| Permission control | `Services/UserService.cs` | GetCurrentUserRole, CanManageUser |
| Username uniqueness | `Repositories/UserRepository.cs` | UsernameExistsAsync |
| Token revocation | `Services/UserService.cs` | Role change triggers token revoke |

## CONVENTIONS

- **Dual-role** — Admin manages Doctor+Receptionist; SuperAdmin manages all
- **Password security** — ASP.NET Core Identity PasswordHasher, 8+ chars required
- **UserName immutable** — UpdateEntity ignores UserName field
- **Reserved usernames** — admin/administrator/root/system/superadmin/sysadmin hardcoded

## ANTI-PATTERNS

- **BatchDelete vs BatchUpdateStatus inconsistency** — Delete uses single SaveChanges, Update uses per-item UpdateAsync
- **FindAsync with soft-delete** — Use IgnoreQueryFilters() for Restore operations
- **Hardcoded reserved list** — Modify code to adjust reserved usernames
