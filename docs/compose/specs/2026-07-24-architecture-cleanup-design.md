---
feature: architecture-cleanup
status: delivered
updated: 2026-07-24
branch: refactor/arch-cleanup-remaining
commits: ac614f184..30ae7f4c9
---

# Architecture Cleanup — Remaining Issues

## Report

**What was built** — 5 surgical fixes: removed dead `ValidateTokenFromHeaderAsync` Refit method (latent duplicate-route bug), registered `IApiClientDeploy`/`IApiClientDiagnostics` on unified `IApiClient` with full vertical slice (adapters + Local/Remote/Switching implementations), fixed garbled Chinese encoding in `ValidationAccessors.cs`, removed duplicate using in `HerbItemViewModelBase.cs`, and archived two completed plan files.

**Verification** — `dotnet build LYBTZYZS.sln`: 0 errors, 5 pre-existing warnings.

**Journey log** — Reviewer caught P1: `HttpClientApiClient` Diagnostics debug URLs missing `/logging/` segment; fixed before finalize.

## [S1] Problem

After the inheritance redundancy optimization and completion of both HIGH/remaining issue plans (9/9 tasks done), several residual issues remain:

1. **H1**: `IAuthApi.cs` has two Refit methods mapping to the identical route `GET /api/v1/auth/validate`. `ValidateTokenFromHeaderAsync` has zero callers — dead code.
2. **H2**: New `IApiClientDeploy` and `IApiClientDiagnostics` interfaces exist but are not registered on the unified `IApiClient` interface.
3. **M1**: `ValidationAccessors.cs` contains garbled Chinese comments (mojibake).
4. **M3**: `HerbItemViewModelBase.cs` has a duplicate `using` directive.

## [S2] Design

### H1: Delete dead `ValidateTokenFromHeaderAsync`

- Removed from `IAuthApi.cs`, `IApiClientAuth.cs`, `AuthApiClient.cs`, `HttpClientApiClient.cs`
- Updated tests to use `ValidateTokenAsync` instead
- Updated README documentation

### H2: Register Deploy/Diagnostics on IApiClient

- Added `IApiClientDeploy Deploy` and `IApiClientDiagnostics Diagnostics` to `IApiClient.cs`
- Created `DeployApiClient` and `DiagnosticsApiClient` adapter classes
- Implemented in `HttpClientApiClient` (Local: business fail for Deploy, real endpoints for Diagnostics)
- Implemented in `RefitApiClient` (Remote: Refit-backed)
- Implemented in `SwitchingApiClient` (delegate to active)

### M1: Fix garbled encoding

- Rewrote all comments in `ValidationAccessors.cs` with correct UTF-8 Chinese text

### M3: Remove duplicate using

- Removed duplicate `using LYBT.Shared.Models.Contracts.Herbs;` from `HerbItemViewModelBase.cs`

## [S3] Out of Scope

- M2 (BaseUsersController indentation) — cosmetic
- M4 (DiagnosticsController DbContext injection) — separate concern
- L1 (BaseRepository null-conditional) — trivial style
- L2 (Three-tier repository consolidation) — major refactor

## Tasks

- [x] T1: Delete `ValidateTokenFromHeaderAsync` from IAuthApi + all implementations — acceptance: duplicate route removed, `dotnet build` passes (covers: S2)
- [x] T2: Register Deploy/Diagnostics on IApiClient + implement in all three implementations — acceptance: `IApiClient.Deploy` and `IApiClient.Diagnostics` accessible, `dotnet build` passes (covers: S2; depends: T1)
- [x] T3: Fix ValidationAccessors.cs garbled encoding — acceptance: all Chinese comments render correctly, `dotnet build` passes (covers: S2)
- [x] T4: Remove duplicate using in HerbItemViewModelBase.cs — acceptance: no duplicate using, `dotnet build` passes (covers: S2)
- [x] T5: Archive completed plan files — acceptance: both plan files moved to archive directory (covers: S3)
