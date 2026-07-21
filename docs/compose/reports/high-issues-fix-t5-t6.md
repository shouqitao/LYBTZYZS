---
feature: high-issues-fix-t5-t6
status: delivered
specs: []
plans:
  - docs/compose/plans/2026-07-21-high-issues-fix.md
branch: fix/high-issues-t5-t6
---

# HIGH Issues Fix T5-T6 — Final Report

## What Was Built

Two surgical fixes addressing HIGH severity issues from the architecture audit:

1. **H8 — Sync-over-async deadlock risk**: Converted three batch operation methods in `MasterDetailViewModelBase<T,T>` from `.GetAwaiter().GetResult()` (synchronous blocking) to proper `async/await`. This eliminates potential deadlocks when the calling thread has a synchronization context (WPF UI thread).

2. **H9 — Dead AutoMapper references**: Removed `AutoMapper` and `AutoMapper.Extensions.Microsoft.DependencyInjection` PackageVersion entries from `Directory.Packages.props`. The project migrated to Riok.Mapperly (compile-time source generator) long ago; no code referenced AutoMapper.

## Architecture

### H8 Fix

File: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

Three virtual methods were blocking on async calls synchronously:
- `DeleteBatchAsync` — called `DeleteItemAsync().GetAwaiter().GetResult()` in a foreach loop
- `EnableBatchAsync` — called `SetItemEnabledAsync().GetAwaiter().GetResult()` in a foreach loop
- `DisableBatchAsync` — called `SetItemEnabledAsync().GetAwaiter().GetResult()` in a foreach loop

Converted to `async` methods using `await` in the loop body. Removed the now-unnecessary `return Task.CompletedTask` patterns.

### H9 Fix

File: `Directory.Packages.props`

Removed two dead `PackageVersion` entries:
- `AutoMapper` (12.0.1)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (12.0.1)

No `.csproj` file referenced these packages (existing comments confirmed "AutoMapper已移除"). No `using AutoMapper` existed in any `.cs` file.

## Verification

- `dotnet build LYBTZYZS.sln --no-restore`: **0 errors**, 8 warnings (all pre-existing, same count as baseline)
- No new warnings introduced by either fix

## Journey Log

- [lesson] Sync-over-async (`GetAwaiter().GetResult()`) is a silent deadlock bomb on WPF's UI thread — always convert to `async/await` even in virtual methods that return `Task`
- [lesson] Central package management (`Directory.Packages.props`) can accumulate dead entries after migrations — periodic audit prevents confusion

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/plans/2026-07-21-high-issues-fix.md` | Implementation plan | T5+T6 executed |
