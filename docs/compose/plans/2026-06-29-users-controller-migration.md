# Users Controller Migration Plan

> Migrate BaseUsersController from direct IUserService calls to MediatR ISender.

**Goal:** Replace all `_userService.XxxAsync()` calls with `_sender.Send(new XxxCommand/Query(...))`.

**Approach:** Modify BaseUsersController to inject `ISender` (MediatR) instead of `IUserService`, then replace each action method's service call with the corresponding command/query handler.

---

### Task 1: Update BaseUsersController Constructor

Replace `IUserService` with `ISender` in constructor and field.

### Task 2: Migrate Each Action Method

| Action | Current Call | New Call |
|--------|-------------|----------|
| GetList | `_userService.GetPagedUsersAsync(...)` | `_sender.Send(new GetUsersQuery(...))` |
| GetById | `_userService.GetUserByIdAsync(...)` | `_sender.Send(new GetUserQuery(...))` |
| Create | `_userService.CreateUserAsync(...)` | `_sender.Send(new CreateUserCommand(...))` |
| Update | `_userService.UpdateUserAsync(...)` | `_sender.Send(new UpdateUserCommand(...))` |
| Delete | `_userService.DeleteUserAsync(...)` | `_sender.Send(new DeleteUserCommand(...))` |
| ToggleStatus | `_userService.ToggleUserStatusAsync(...)` | (needs new command) |
| BatchDelete | `_userService.BatchDeleteUsersAsync(...)` | (needs new command) |
| ResetPassword | `_userService.ResetPasswordAsync(...)` | (needs new command) |
| ChangeProfile | `_userService.ChangeProfileAsync(...)` | (needs new command) |
| ChangePassword | `_userService.ChangePasswordAsync(...)` | (needs new command) |
| GetCurrentUser | `_userService.GetCurrentUserAsync(...)` | (needs new query) |

### Task 3: Create Missing Commands/Queries

- ToggleUserStatusCommand + Handler
- BatchDeleteUsersCommand + Handler
- ResetPasswordCommand + Handler
- ChangeProfileCommand + Handler
- ChangePasswordCommand + Handler
- GetCurrentUserQuery + Handler
