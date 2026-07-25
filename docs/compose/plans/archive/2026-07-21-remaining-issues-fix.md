# Remaining Issues Fix Plan

> **For agentic workers:** Use compose:execute to implement this plan task-by-task.

**Goal:** Fix remaining issues from full codebase scan.

**Architecture:** Surgical fixes across controllers and infrastructure.

---

### Task 1: AuthController Logout 裸返回修复

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`

- [ ] Line 43: `Ok(new ApiResponse { ... })` → `Success("已登出")`
- [ ] Build

---

### Task 2: BaseUsersController BatchDelete 缺少 IsSuccess 检查

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`

- [ ] Add `if (!result.IsSuccess || result.Value == null) return BusinessFail(...)` before returning success
- [ ] Build

---

### Task 3: FormulasController StatusCode(403) 统一为 Forbid()

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`

- [ ] Line 76: `StatusCode(403, ApiResponse<object>.CreateFail(...))` → `Forbid("无权限查看此验方")`
- [ ] Build

---

### Task 4: ModuleLazyLoader 同步阻塞异步修复

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/ModuleLazyLoader.cs`

- [ ] Convert `EnsureModuleLoaded` to async `EnsureModuleLoadedAsync` using `await` instead of `.GetAwaiter().GetResult()`
- [ ] Build
