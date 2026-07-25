# HIGH Issues Fix Plan

> **For agentic workers:** Use compose:execute to implement this plan task-by-task.

**Goal:** Fix all 9 HIGH severity issues from the architecture audit.

**Architecture:** Surgical fixes across LocalWebAPI controllers, Refit interfaces, repositories, config files, and ViewModels.

---

### Task 1: H1+H2 — LocalWebAPI 裸返回修复

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/DiagnosticsController.cs`

- [ ] AuthController: `Ok(result)` → `Success(result)`, `Unauthorized(result)` → `BusinessFail(result.Error)`
- [ ] DiagnosticsController: `Ok(result)` → `Success(result)`
- [ ] Build: `dotnet build LYBTZYZS.sln --no-restore`

---

### Task 2: H4 — IAuthApi ValidateToken 方法不匹配

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuthApi.cs`

- [ ] Change `POST /api/v1/auth/validate` to `GET /api/v1/auth/validate` (matching server)
- [ ] Build

---

### Task 3: H5 — N+1 查询修复

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

- [ ] Replace foreach loop with single query using `ids.Contains(m.Id)` + `CompatibilityLevel` check
- [ ] Build

---

### Task 4: H6+H7 — 移除硬编码密码

**Files:**
- Modify: `src/Client/Desktop/Shell/appsettings.json`
- Modify: `src/Tools/ApiTester/Program.cs`

- [ ] Shell appsettings: replace passwords with placeholders, remove JWT secret
- [ ] ApiTester: replace hardcoded password with config/env
- [ ] Build

---

### Task 5: H8 — 同步阻塞异步修复

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

- [ ] Convert `DeleteBatchAsync`/`EnableBatchAsync`/`DisableBatchAsync` to use `await` instead of `.GetAwaiter().GetResult()`
- [ ] Build

---

### Task 6: H9 — 移除 AutoMapper 包引用

**Files:**
- Modify: `Directory.Packages.props`

- [ ] Remove AutoMapper PackageVersion entries
- [ ] Build
