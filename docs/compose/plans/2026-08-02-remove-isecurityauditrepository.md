# Remove ISecurityAuditRepository Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the redundant `ISecurityAuditRepository` interface and its implementation; have `SecurityAuditService` inject `AppDbContext` directly.

**Architecture:** The `SecurityAuditRepository` is a thin pass-through over `AppDbContext.SecurityAuditLogs`. Removing it eliminates an unnecessary abstraction layer. `SecurityAuditService` will use `AppDbContext` directly (the same DbContext the repository used).

**Tech Stack:** .NET 8, EF Core, ASP.NET Core

## Global Constraints

- Do NOT touch `CrossModuleService` or any `CrossModule` sub-interfaces
- No public API behavior changes
- No route/DTO changes
- `dotnet build` must pass
- `dotnet test tests/LYBT.Tests.Architecture/` must pass

---

### Task 1: Remove interface + repository, update service, update DI

**Files:**
- Delete: `src/Server/Modules/LYBT.Module.Auth/Interfaces/ISecurityAuditRepository.cs`
- Delete: `src/Server/Modules/LYBT.Module.Auth/Infrastructure/SecurityAuditRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/SecurityAuditService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/AuthModule.cs`

- [ ] **Step 1: Delete the interface file**

```bash
rm src/Server/Modules/LYBT.Module.Auth/Interfaces/ISecurityAuditRepository.cs
```

- [ ] **Step 2: Delete the repository file**

```bash
rm src/Server/Modules/LYBT.Module.Auth/Infrastructure/SecurityAuditRepository.cs
```

- [ ] **Step 3: Update SecurityAuditService to inject AppDbContext directly**

Replace the constructor and field to use `AppDbContext` instead of `ISecurityAuditRepository`. Update `RecordEventAsync` to call `_context.SecurityAuditLogs.AddAsync()` and `_context.SaveChangesAsync()` directly.

- [ ] **Step 4: Remove ISecurityAuditRepository DI registration from AuthModule.cs**

Delete the line: `services.AddScoped<Interfaces.ISecurityAuditRepository, Infrastructure.SecurityAuditRepository>();`

- [ ] **Step 5: Build and verify**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 6: Run architecture tests**

```bash
dotnet test tests/LYBT.Tests.Architecture/
```

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "refactor(auth): remove ISecurityAuditRepository, inject DbContext directly"
```
