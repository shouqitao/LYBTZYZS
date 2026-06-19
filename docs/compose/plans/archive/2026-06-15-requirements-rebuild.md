# 需求文档体系重建 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete `docs/01-product/` (10 files) and `docs/02-requirements/` (22 files), rebuild both directories from scratch based on current code state (138 US implemented). Consolidate 15 modules → 10 modules.

**Architecture:** Documentation-only rebuild. Zero code changes. Source of truth = (1) approved design spec `docs/compose/specs/2026-06-15-requirements-rebuild-design.md`, (2) current codebase (controllers/services/entities), (3) explore-2 module inventory report. Old docs are deleted entirely — no content migration, rebuild from code reality.

**Tech Stack:** Markdown only. No build step. Verification = `grep` for broken links + US count reconciliation + manual spot-check.

---

## Source Documents

| Document | Path | Purpose |
|----------|------|---------|
| Design spec (approved) | `docs/compose/specs/2026-06-15-requirements-rebuild-design.md` | Authoritative scope, file list, US format, 6-stage plan |
| Module inventory (explore-2) | Inline in this plan §"Module Specs" | Per-module controllers, services, operations, business rules, US counts |
| Docs inventory (explore-1) | Inline in this plan Task 20 | Cross-reference map of links that will break |

---

## File Structure

### New `docs/01-product/` (4 files, WHO + WHY)

```
docs/01-product/
├── README.md           # Index + navigation
├── 01-vision.md        # Vision + problem + value (absorbs old jtbd/value-prop/customer-journey)
├── 02-personas.md      # 4 role personas (Receptionist/Doctor/Admin/SuperAdmin)
└── 03-glossary.md      # TCM + technical glossary (铁律: Consultation≠问诊, MedicalCase≠病历, Formula≠公式)
```

### New `docs/02-requirements/` (12 files, WHAT)

```
docs/02-requirements/
├── README.md           # Index + US overview table (all 138 US listed)
├── 01-prd.md           # Top-level PRD
├── 02-auth.md          # 认证与会话 (13 US)
├── 03-users.md         # 用户管理 (12 US)
├── 04-patients.md      # 患者管理 (13 US)
├── 05-herbs.md         # 药材管理 (13 US)
├── 06-formulas.md      # 验方管理 (13 US)
├── 07-medical-cases.md # 医案管理 (18 US — core aggregate root)
├── 08-registration.md  # 挂号管理 (7 US)
├── 09-printing.md      # 处方打印 (4 US)
├── 10-sync.md          # 数据同步 (8 US)
├── 11-platform.md      # 平台基础设施 (35 US — Shell+Config+Error+Logging+Health+CardReader merged)
└── 12-nfr.md           # 非功能需求
```

**Total: 16 files** (4 product + 12 requirements), down from 32 (10 + 22).

---

## Shared Conventions

### US Format Template (per design [S3])

Every User Story in `02-requirements/02-*.md` through `11-*.md` MUST follow this exact structure:

```markdown
### US-XXX-NNN: 标题

**角色**: 角色
**优先级**: Must / Should / Could
**状态**: ✅ 已实现

**作为** [角色]，**我想要** [功能]，**以便** [价值]

**验收标准**:
- [ ] 条件 1
- [ ] 条件 2

**业务规则**:
1. 规则描述

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | ... |
| 本地 | ... |
```

### US Numbering (preserved from old scheme)

| Prefix | Module | Count |
|--------|--------|-------|
| US-AUTH | 认证 | 13 |
| US-USER | 用户 | 12 |
| US-PAT | 患者 | 13 |
| US-HERB | 药材 | 13 |
| US-FORM | 验方 | 13 |
| US-MC | 医案 | 18 |
| US-REG | 挂号 | 7 |
| US-PRINT | 打印 | 4 |
| US-SYNC | 同步 | 8 |
| US-SHELL | 平台¹ | 35 |
| **Total** | | **136**² |

¹ Platform merges 6 old prefixes: US-SHELL (7) + US-CFG (4) + US-ERR (8) + US-LOG (7) + US-SYS (9) + US-CARD (2) = 37 → consolidated to 35 (2 redundant removed).
² Target ~136-140. Old total was 138. Reconcile final count in Task 22.

### File Header Convention

Every new `.md` file starts with:

```markdown
# [Title]

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

```

### Dual-Mode Table Convention

When a US behaves identically in both modes, use:

```markdown
**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |
```

When it differs, document the actual difference.

---

## Module Specs (from explore-2 inventory)

_This section is the authoritative source for US content. Each module task references these specs._

### AUTH — 认证与会话 (target 13 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:21` (route `api/v1/Auth`)
- Local: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs:19`
- Services: `IAuthService` (`src/Server/Modules/LYBT.Module.Auth/Interfaces/IAuthService.cs:11`), `IAutoLoginService`, `ITokenManagementService`, `ITokenRevocationService`, `ISecurityAuditService`, `IJwtService`

**Business rules:**
- Token rotation with family-based revocation (replay-attack guard)
- Configurable account lockout: `SecurityOptions.AccountLockout.MaxFailedCount` / `LockoutMinutes`
- Rate limiting: `[EnableRateLimiting("Login")]` on login + auto-login
- AutoLoginToken server-revocable, rotates on success
- Logout works even with expired token (`[AllowAnonymous]`)
- Reserved usernames: admin/administrator/root/system/superadmin/sysadmin

**Dual-mode:**
- Remote: Full JWT (2h access / 7d refresh) + RefreshToken family + AutoLoginToken rotation
- Local: Simplified JWT via `LocalJwtConfig` — 1-year token, no refresh, rate limit 5/min

### USERS — 用户管理 (target 12 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:22` (route `api/v1/users`, 13 endpoints)
- Services: `IUserService` + `IUserQueryService`, `IUserPasswordService`, `IUserStatusService`, `IUserBatchOperationService`

**Business rules:**
- Role hierarchy: Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100
- 4 authorization policies: AdminOnly, DoctorOrAdmin, PatientAccess (incl. Receptionist), SuperAdminOnly
- UserName immutable on update
- IDOR protection: profile/password change requires `id == currentUserId`
- Cannot delete self; role change triggers token revocation
- `BatchDelete` uses single SaveChanges; `BatchUpdateStatus` uses per-item UpdateAsync

**Dual-mode:** Identical (both use `IUserService`)

### PATIENTS — 患者管理 (target 13 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:24` (route `api/v1/Patients`, 12 endpoints, `[Authorize(Policy = PatientAccess)]`)
- Services: `IPatientService`, `IPatientImportExportService`

**Business rules:**
- Sensitive data masking via `[SensitiveData]` attribute: IdCardNumber/PhoneNumber=Partial, Address=Default, AllergyHistory/MedicalHistory=Hash
- Phone uniqueness enforced
- Age computed from BirthDate (manual copy in controller, Mapperly ignores)
- Pinyin auto-generation on Create/Update
- Non-admin users see only enabled patients
- Delete blocked if referenced by MedicalCases → 422

**Dual-mode:** Identical (same `IPatientService`)

### HERBS — 药材管理 (target 13 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:22` (route `api/v1/Herbs`, 15 endpoints, `[Authorize(Policy = DoctorOrAdmin)]`)
- Services: `IHerbService`, `IHerbImportExportService`

**Business rules:**
- Record-Only mode (no inventory management)
- Pinyin search: `PinyinAbbreviation` ("dg" → "当归")
- Two import paths: `ImportFromExcelAsync` (server EPPlus) vs `BatchImportAsync` (client DTOs)
- Reference check before delete (prescriptions) → 422
- `DuplicateStrategy` enum (Skip/Update/Error), max 10000 records
- Category filter
- OutputCache policy `HerbsCache`

**Dual-mode:** Identical

### FORMULAS — 验方管理 (target 13 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs:23` (route `api/v1/Formulas`, 13 endpoints, `[Authorize(Policy = DoctorOrAdmin)]`)
- Services: `IFormulaService` (`src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs:11`), `IFormulaImportExportService`

**Business rules — validation workflow:**
- State machine: `Draft ↔ Validated`
- New formula starts as `Draft`
- FLAW-F1 fix: herb changes re-evaluate; if any herb unvalidated, demote Validated→Draft
- `ValidateFormulaHerbAsync`: validates single herb; when ALL herbs validated, auto-promote to Validated
- `GetPendingValidationFormulasAsync`: returns Draft formulas (Doctor's to-do list)
- Herb binding: `OriginalHerbName` (free-text) → `SelectedHerbId` (system Herb); `IsValidated` iff `HerbId.HasValue`
- Ownership: Doctor sees own + shared; Admin sees all
- Import: defaults to Draft; auto-promotes to Validated if all herbs matched

**Dual-mode:** Identical

### MEDICALCASES — 医案管理 (target 18 US — CORE)

**Source code:**
- 4 controllers, all `api/v1/medicalcases`:
  - `MedicalCasesController` (`src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs:26`) — CRUD
  - `MedicalCaseProcessingController` (`:25`) — state transitions
  - `MedicalCaseAuditController` (`:23`) — permissions + audit
  - `MedicalCasePrintController` (`:23`) — print write-back
- Facade: `IMedicalCaseFacade` (`src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseFacade.cs:15`) — aggregates 5 CQRS services
- Business rules: `MedicalCaseBusinessRules` (`src/Shared/LYBT.Shared.Validators/BusinessRules/MedicalCaseBusinessRules.cs:9`)

**Business rules:**
- State machine: `Suspended ↔ Active`; `Completed` terminal (only via CompleteAsync); `Cancelled` removed (= soft-delete)
- **BR-001**: Single active case per patient (cannot create if Active or Suspended exists)
- Permission matrix:
  - Admin/SuperAdmin: edit ALL cases regardless of state
  - Doctor: own cases only; Active=always editable; Completed=editable same-day only (locked next day)
  - Admin CANNOT create medical cases (Doctors only)
  - Deletion = edit permission
- Edit-reason required when: IsLocked OR IsCompleted OR not-owner
- Complete workflow validates: NeedsPrescription set; if true, prescription must exist with ≥1 item; TCM diagnosis not empty
- Cancel restrictions: Completed cannot cancel; printed cases cannot cancel; non-same-day/non-owner requires reason
- Print protection: editing printed case resets IsPrinted=false
- HasPrescription computed: `entity.Prescription != null && !entity.Prescription.IsDeleted`
- Concurrency retry: up to 3 on DbUpdateConcurrencyException
- Audit: 20-field diff tracking
- Registration linkage: complete → Registration Completed; cancel → Source-aware rollback

**Dual-mode:** Identical (22 endpoints via Facade)

### REGISTRATION — 挂号管理 (target 7 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24` (route `api/v1/Registrations`, 7 endpoints, `[Authorize(Policy = PatientAccess)]`)
- Services: `IRegistrationService`, `IRegistrationRepository`; injects `IMedicalCaseCommandService` for QuickVisit

**Business rules:**
- Two-source model: Receptionist creates `Waiting` (queued); Doctor QuickVisit creates `InProgress` (no queue)
- Status flow: `Waiting → InProgress → Completed`; `Waiting → Cancelled` (receptionist only)
- REG-BR-001: cancellation only from Waiting
- QuickVisit atomicity: `TransactionScope(ReadCommitted)` wraps Registration + MedicalCase
- US-REG-005: MedicalCase completion → Registration Completed
- US-REG-006: MedicalCase cancellation → Source-aware rollback

**Dual-mode:** Identical

### PRINTING — 处方打印 (target 4 US)

**Source code:**
- Desktop-only: `src/Client/Desktop/Core/LYBT.Desktop.Printing/`
- Interface: `IPrintService<TModel>` (`Interfaces/IPrintService.cs:9`)
- Impl: `PrescriptionPrintService` (`Services/PrescriptionPrintService.cs:23`)
- PDF: `PrescriptionPdfExporter` (QuestPDF, Community license)
- Templates (4): `PrescriptionPrintTemplate.xaml` (A5), `PrescriptionPrintA4Template.xaml` (A4), `PrescriptionContinuationTemplate.xaml` (A5 multi-page), `PrescriptionContinuationA4Template.xaml` (A4 multi-page)
- Server write-back: `MedicalCasePrintController.cs:44,72`

**Business rules:**
- Empty-prescription guard (CODE-24): throws if `model.Items` null/empty
- A5 default paper size (TCM standard); A4 supported
- Multi-page continuation templates for overflow
- PrintLogRequested event fires on success AND failure
- Print-protection coupling with MedicalCase (editing resets IsPrinted)
- PDF export uses QuestPDF; preview uses WPF FixedDocument + XPS

**Dual-mode:** Desktop-only; works identically in both modes (local OS operation)

### SYNC — 数据同步 (target 8 US)

**Source code:**
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/SyncController.cs:20` (route `api/v1/Sync`, 6 endpoints, `[Authorize(Policy = DoctorOrAdmin)]`)
- Services: `ISyncService`, `ISyncRepository`, `ChecksumHelper`
- Desktop: `SyncViewModel` (`src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs:21`); `SyncPhase.cs:7`

**Business rules — 5-phase workflow:**
1. Idle → 2. CheckingDifferences (`/compare`) → 3. ReviewingDifferences (user resolves) → 4. ExecutingSync (`/upload`+`/download`+`/delete`) → 5. Completed/Failed
- Conflict detection: checksum-based via `ChecksumHelper`
- Conflict resolution: per-item UseLocal/UseServer/Skip; ALL must resolve before ExecuteSync
- Error classification: `TransientNetwork, AuthExpired, BusinessReject, ConflictChanged, Unknown` — only first 3 enable Retry
- Pre-conditions: `SessionManager.IsAuthenticated` AND `IApiHealthCheckService.CheckHealthAsync`
- Delete with reference check (server-side rejects FK violations)
- Supported entities (4): Herb, Patient, Formula, MedicalCase
- Selection model: `IsSelected` on items; computed counts drive CanExecute

**Dual-mode:** Inherently dual-mode (local Desktop ↔ remote Server)

### PLATFORM — 平台基础设施 (target 35 US)

_Merges 6 old modules: Shell + Config + Error + Logging + Health + CardReader_

**Source code:**
- **Shell**: `src/Client/Desktop/Shell/App.xaml.cs:41` (PrismApplication, single-instance mutex, splash, two-phase Serilog, role-based module loading)
- **Bootstrapper**: `ApplicationBootstrapper.LoadModulesForRoleAsync` (`src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs:35`)
- **Config**: `ConfigurationController` (`src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs:15`, `[Authorize(Policy = SuperAdminOnly)]`); `ISystemConfigurationService`
- **Health**: `HealthController` (`:19`) — anonymous `/health`+`/ping`, authorized `/details`
- **Diagnostics**: `DiagnosticsController` (`:20`, `[Authorize(Policy = SuperAdminOnly)]`, `LoggingLevelManager`)
- **Card reader**: `ICardReaderService` (`src/Client/Desktop/Core/LYBT.Desktop.CardReader/Services/ICardReaderService.cs:10`); `HuaDaHD100CardReader` (P/Invoke) + `MockCardReader`
- **Card reader integration**: `IPatientCardReaderIntegration` (find-or-create with PRD-15 dedup)

**Business rules:**
- Single-instance: `Mutex` named `Global\LYBTZYZS_Shell_Instance`
- Role-based module loading: `LoadModulesForRoleAsync` filters by role
- Two-phase Serilog bootstrap (catches startup errors before final logger)
- Debug-mode cap: max 120 minutes
- Health `/details` returns 503 when DB Degraded/Unhealthy
- Card reader strategy: `AutoDetectReaderAsync` selects HuaDa vs Mock
- Card reader dedup (PRD-15): `PatientMatchType` enum: ExactMatch, FuzzyMatch, MultipleCandidates, NoMatch
- Global exception handlers: app-domain + dispatcher level

**US distribution within platform.md (use H2 sections):**
- `## Shell` (7 US — app launch, splash, role loading, account settings, navigation)
- `## Configuration` (4 US — get/validate config)
- `## Error Handling` (8 US — global handler, friendly messages, trace ID)
- `## Logging & Audit` (7 US — structured logs, correlation, masking, retention)
- `## Health & Diagnostics` (9 US — liveness, detailed check, log level control)
- `## Card Reader` (2 US — init/read, find-or-create patient)

**Dual-mode:**
- `SwitchingApiClient` routes localhost → embedded LocalWebAPI; otherwise → Refit remote
- LocalWebAPI reuses all 8 server modules' Service layer
- LocalDbBackupService (local mode only)

---

## Tasks

### Task 1: Delete old documentation directories

**Covers:** [S1], [S5] Stage 1

**Files:**
- Delete: `docs/01-product/` (10 files: README.md, 01-vision.md, 02-personas.md, 03-jtbd.md, 04-user-roles.md, 05-feature-list.md, 06-clinical-workflow.md, 07-glossary.md, 08-value-proposition.md, 09-customer-journey.md)
- Delete: `docs/02-requirements/` (22 files: README.md, 01-prd.md, 02-auth.md through 21-role-permission-matrix.md)

- [ ] **Step 1: Verify current file counts match expectation**

Run from `D:\source\repos\LYBTZYZS`:
```powershell
(Get-ChildItem docs/01-product -File).Count
(Get-ChildItem docs/02-requirements -File).Count
```
Expected: `10` and `22`. If counts differ, STOP and reconcile with the inventory in design spec [S2] before proceeding.

- [ ] **Step 2: Delete both directories**

```powershell
Remove-Item -Recurse -Force docs/01-product
Remove-Item -Recurse -Force docs/02-requirements
```

- [ ] **Step 3: Verify deletion**

```powershell
Test-Path docs/01-product
Test-Path docs/02-requirements
```
Expected: both `False`.

- [ ] **Step 4: Commit**

```powershell
git add -A docs/01-product docs/02-requirements
git commit -m "docs: 删除旧的 01-product/ 和 02-requirements/ 目录（准备重建）"
```

---

### Task 2: Create new directory structure with placeholder READMEs

**Covers:** [S2], [S5] Stage 1

**Files:**
- Create: `docs/01-product/README.md`
- Create: `docs/02-requirements/README.md`

- [ ] **Step 1: Create directories**

```powershell
New-Item -ItemType Directory -Path docs/01-product -Force
New-Item -ItemType Directory -Path docs/02-requirements -Force
```

- [ ] **Step 2: Write placeholder `docs/01-product/README.md`**

```markdown
# 产品文档 (01-product)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建中

## 文件索引

| 文件 | 内容 | 状态 |
|------|------|------|
| [01-vision.md](01-vision.md) | 产品愿景 + 问题定义 + 核心价值 | 待写 |
| [02-personas.md](02-personas.md) | 4 角色画像 | 待写 |
| [03-glossary.md](03-glossary.md) | 业务术语表 | 待写 |
```

- [ ] **Step 3: Write placeholder `docs/02-requirements/README.md`**

```markdown
# 需求文档 (02-requirements)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建中

## 文件索引

| 文件 | 模块 | US 数 | 状态 |
|------|------|-------|------|
| [01-prd.md](01-prd.md) | 顶层 PRD | — | 待写 |
| [02-auth.md](02-auth.md) | 认证与会话 | 13 | 待写 |
| [03-users.md](03-users.md) | 用户管理 | 12 | 待写 |
| [04-patients.md](04-patients.md) | 患者管理 | 13 | 待写 |
| [05-herbs.md](05-herbs.md) | 药材管理 | 13 | 待写 |
| [06-formulas.md](06-formulas.md) | 验方管理 | 13 | 待写 |
| [07-medical-cases.md](07-medical-cases.md) | 医案管理 | 18 | 待写 |
| [08-registration.md](08-registration.md) | 挂号管理 | 7 | 待写 |
| [09-printing.md](09-printing.md) | 处方打印 | 4 | 待写 |
| [10-sync.md](10-sync.md) | 数据同步 | 8 | 待写 |
| [11-platform.md](11-platform.md) | 平台基础设施 | 35 | 待写 |
| [12-nfr.md](12-nfr.md) | 非功能需求 | — | 待写 |
| **合计** | | **136** | |
```

- [ ] **Step 4: Commit**

```powershell
git add docs/01-product/README.md docs/02-requirements/README.md
git commit -m "docs: 创建新的 01-product/ 和 02-requirements/ 目录结构"
```

---

### Task 3: Write `docs/01-product/01-vision.md`

**Covers:** [S2] 01-product/, [S4] improvement #3 (merge jtbd/value-prop/customer-journey into vision)

**Files:**
- Create: `docs/01-product/01-vision.md`

**Content sections (H2):**
1. `## 产品愿景` — one-paragraph vision statement for 凌隐宝堂中医诊所管理系统
2. `## 问题定义` — pain points the system solves (paper-based records, pinyin lookup efficiency, formula digitalization, prescription printing, multi-doctor coordination)
3. `## 核心价值` — value by role (Doctor/Admin/Receptionist) — absorb old `08-value-proposition.md` content
4. `## 业务目标` — 5 business goals with success metrics (absorb old `01-vision.md` goals)
5. `## 核心场景` — 3 scenarios: 首诊/复诊/义诊 (absorb old `05-feature-list.md` scenarios)
6. `## 系统边界` — what's in scope (v1) vs deferred (v2.0)
7. `## 客户旅程` — 6-phase clinic journey (absorb old `09-customer-journey.md` essence — keep it concise, not 518 lines)

**Content sources:** Read deleted content from git history if needed:
```powershell
git show HEAD~1:docs/01-product/01-vision.md     # old vision
git show HEAD~1:docs/01-product/05-feature-list.md # scenarios
git show HEAD~1:docs/01-product/08-value-proposition.md
git show HEAD~1:docs/01-product/09-customer-journey.md
git show HEAD~1:docs/01-product/03-jtbd.md
```

**Length target:** 150-250 lines (concise; old 01-vision was ~similar, but merged content from 4 files means trim aggressively).

**Rules:**
- Remove ALL v2.0 deferred features from main narrative — list them briefly in `## 系统边界` only
- Remove SQLite references (deprecated — MEMORY.md confirms LocalDB is current)
- No `FR-` prefix references (deprecated, replaced by `US-`)
- Cross-link to `02-requirements/01-prd.md` for detailed requirements

- [ ] **Step 1: Write the file** with all 7 sections above. Pull essence from git history but rewrite in the new consolidated structure.

- [ ] **Step 2: Verify no stale references**

```powershell
Select-String -Path docs/01-product/01-vision.md -Pattern "SQLite","FR-","jtbd\.md","value-proposition\.md","customer-journey\.md"
```
Expected: no matches.

- [ ] **Step 3: Commit**

```powershell
git add docs/01-product/01-vision.md
git commit -m "docs(product): 重写 01-vision.md（合并 jtbd/value-prop/customer-journey）"
```

---

### Task 4: Write `docs/01-product/02-personas.md`

**Covers:** [S2] 01-product/

**Files:**
- Create: `docs/01-product/02-personas.md`

**Content sections:**
1. Intro paragraph
2. Four personas, each with H3:
   - `### 前台接待员 (Receptionist)` — PermissionLevel=0
   - `### 医生 (Doctor)` — PermissionLevel=1
   - `### 管理员 (Admin)` — PermissionLevel=10
   - `### 超级管理员 (SuperAdmin)` — PermissionLevel=100

Each persona includes:
- 背景 (background)
- 日常工作流 (daily workflow)
- 目标 (goals)
- 痛点 (pain points)
- 成功标准 (success criteria)
- 权限范围 (permission scope — brief; full matrix lives in `02-requirements/01-prd.md`)

**Content source:** `git show HEAD~1:docs/01-product/02-personas.md` (3 personas) — add SuperAdmin as 4th.

**Length target:** 100-180 lines.

- [ ] **Step 1: Write the file** with 4 personas. Port the 3 existing personas from git history, add SuperAdmin.

- [ ] **Step 2: Commit**

```powershell
git add docs/01-product/02-personas.md
git commit -m "docs(product): 重写 02-personas.md（4 角色画像）"
```

---

### Task 5: Write `docs/01-product/03-glossary.md`

**Covers:** [S2] 01-product/

**Files:**
- Create: `docs/01-product/03-glossary.md`

**Content sections:**
1. `## 铁律 (Iron Rules)` — the critical terminology distinctions:
   - **Consultation = 中医诊断** (NOT "问诊" or "就诊")
   - **MedicalCase = 医案** (NOT "病历")
   - **Formula = 验方/经验方** (NOT "公式")
2. `## 业务术语` — table of TCM + clinic terms (中药材, 处方, 挂号, etc.)
3. `## 技术术语` — table of technical terms (Dual-Mode, AggregateRoot, CQRS, SoftDelete, etc.)

**Content source:** `git show HEAD~1:docs/01-product/07-glossary.md`

**Length target:** 60-120 lines.

- [ ] **Step 1: Write the file**

- [ ] **Step 2: Commit**

```powershell
git add docs/01-product/03-glossary.md
git commit -m "docs(product): 重写 03-glossary.md（业务+技术术语表）"
```

---

### Task 6: Update `docs/01-product/README.md` (finalize index)

**Covers:** [S2] 01-product/README

**Files:**
- Modify: `docs/01-product/README.md`

- [ ] **Step 1: Rewrite README.md** — replace placeholder "待写" statuses with "✅ 已完成" and add a one-paragraph product overview at the top.

- [ ] **Step 2: Commit**

```powershell
git add docs/01-product/README.md
git commit -m "docs(product): 完善 README.md 索引"
```

---

### Task 7: Write `docs/02-requirements/01-prd.md`

**Covers:** [S2] 02-requirements/, [S5] Stage 3

**Files:**
- Create: `docs/02-requirements/01-prd.md`

**Content sections:**
1. `## 执行摘要` — 2-3 paragraphs
2. `## 问题陈述` — clinic pain points
3. `## 量化痛点` — measurable pains
4. `## 目标用户` — brief; link to `01-product/02-personas.md`
5. `## 成功指标` — KPIs
6. `## 范围` — v1 scope (10 modules, 136 US); v2.0 deferred
7. `## 权限矩阵` — consolidated role-permission matrix (absorb old `21-role-permission-matrix.md` essence)
8. `## 依赖与风险`
9. `## 模块总览` — table linking to each `02-*.md` through `11-*.md`

**Rules:**
- NO `FR-` prefix (use `US-`)
- NO references to deleted files (jtbd.md, value-proposition.md, etc.)
- Link to `01-product/` files for WHO/WHY context
- Keep role-permission matrix here as the SINGLE source of truth (old matrix was a separate 448-line file — condense to essential)

**Content source:** `git show HEAD~2:docs/02-requirements/01-prd.md` (adjust ~N based on commits) + `git show HEAD~2:docs/02-requirements/21-role-permission-matrix.md`

**Length target:** 200-350 lines.

- [ ] **Step 1: Write the file**

- [ ] **Step 2: Verify no broken links**

```powershell
Select-String -Path docs/02-requirements/01-prd.md -Pattern "FR-","jtbd","value-proposition","customer-journey","user-story-map","roadmap\.md","role-permission-matrix"
```
Expected: no matches.

- [ ] **Step 3: Commit**

```powershell
git add docs/02-requirements/01-prd.md
git commit -m "docs(req): 重写 01-prd.md（顶层 PRD + 权限矩阵合并）"
```

---

### Task 8: Write `docs/02-requirements/02-auth.md` (13 US)

**Covers:** [S5] Stage 4 — AUTH module; spec §"AUTH"

**Files:**
- Create: `docs/02-requirements/02-auth.md`

**Document structure:**
```markdown
# 认证与会话 (Authentication & Session)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 模块概述
[1-2 paragraphs: medical data security, dual-mode auth]

## 业务规则
1. Token rotation with family-based revocation
2. Account lockout (configurable: MaxFailedCount/LockoutMinutes)
3. Rate limiting on login + auto-login
4. Reserved usernames

## 用户故事

### US-AUTH-001: 用户名密码登录
[full US per template]

### US-AUTH-002 through US-AUTH-013
[each US per template]
```

**US list (13):**
| ID | Title | Priority |
|----|-------|----------|
| US-AUTH-001 | 用户名密码登录 | Must |
| US-AUTH-002 | 登录失败锁定 | Must |
| US-AUTH-003 | 登录限流 | Must |
| US-AUTH-004 | 令牌刷新 | Must |
| US-AUTH-005 | 令牌验证 | Must |
| US-AUTH-006 | 重放攻击检测（令牌族撤销） | Must |
| US-AUTH-007 | 安全审计日志 | Should |
| US-AUTH-008 | 登出（含过期令牌） | Must |
| US-AUTH-009 | 本地自动登录（AutoLoginToken） | Must |
| US-AUTH-010 | AutoLoginToken 轮换 | Should |
| US-AUTH-011 | 保留用户名拦截 | Must |
| US-AUTH-012 | 本地简化认证（1年令牌） | Must |
| US-AUTH-013 | 本地登录限流（5次/分） | Must |

**Example US (write this one fully, others follow same template):**

```markdown
### US-AUTH-001: 用户名密码登录

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 任何系统用户，**我想要** 使用用户名和密码登录系统，**以便** 安全地访问我的工作环境。

**验收标准**:
- [ ] 接受用户名 + 密码组合
- [ ] 验证成功后返回 JWT 访问令牌（远程：2h；本地：1年）
- [ ] 验证失败返回通用错误信息（不泄露用户名是否存在）
- [ ] 连续失败达到阈值后触发账户锁定
- [ ] 登录端点应用限流策略

**业务规则**:
1. 远程模式返回 access_token (2h) + refresh_token (7d, 可旋转)
2. 本地模式返回单一 JWT (1年, 无 refresh)
3. 保留用户名（admin/administrator/root/system/superadmin/sysadmin）拒绝普通注册
4. SuperAdmin 凭证存储于 AdminSecrets 表（已统一到 Users 表，Role=100）— 注：AdminSecrets 已移除（Issue #1909），统一到 Users

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | JWT 2h + Refresh 7d（族旋转）+ 安全审计 |
| 本地 | JWT 1年 + 限流 5次/分 + 无 refresh |

**实现参考**: `AuthController.cs:44` (login), `AuthService.cs`, `LocalWebAPI/Controllers/AuthController.cs:41`
```

- [ ] **Step 1: Write module header + business rules + all 13 US** following the template. Use the example US-AUTH-001 as the pattern. Source: AUTH spec above.

- [ ] **Step 2: Verify US count**

```powershell
(Select-String -Path docs/02-requirements/02-auth.md -Pattern "^### US-AUTH-").Count
```
Expected: `13`.

- [ ] **Step 3: Commit**

```powershell
git add docs/02-requirements/02-auth.md
git commit -m "docs(req): 重写 02-auth.md（13 US，认证与会话）"
```

---

### Task 9: Write `docs/02-requirements/03-users.md` (12 US)

**Covers:** [S5] Stage 4 — USERS module; spec §"USERS"

**Files:**
- Create: `docs/02-requirements/03-users.md`

**US list (12):**
| ID | Title | Priority |
|----|-------|----------|
| US-USER-001 | 分页查询用户列表 | Must |
| US-USER-002 | 查看用户详情 | Must |
| US-USER-003 | 查看当前用户资料 | Must |
| US-USER-004 | 创建用户 | Must |
| US-USER-005 | 更新用户（用户名不可变） | Must |
| US-USER-006 | 删除用户（软删除，不可删自己） | Must |
| US-USER-007 | 重置用户密码（SuperAdmin） | Must |
| US-USER-008 | 修改个人资料（IDOR 防护） | Must |
| US-USER-009 | 修改密码（需旧密码） | Must |
| US-USER-010 | 启用/禁用用户 | Must |
| US-USER-011 | 恢复软删除用户（SuperAdmin） | Should |
| US-USER-012 | 批量操作（删除/启用/禁用） | Should |

**Source:** USERS spec above + `git show HEAD~3:docs/02-requirements/03-users.md` for old content reference.

- [ ] **Step 1: Write header + 4-level permission model explanation + 12 US**

- [ ] **Step 2: Verify count** — `(Select-String -Path docs/02-requirements/03-users.md -Pattern "^### US-USER-").Count` = 12

- [ ] **Step 3: Commit** — `git commit -m "docs(req): 重写 03-users.md（12 US，用户管理）"`

---

### Task 10: Write `docs/02-requirements/04-patients.md` (13 US)

**Covers:** [S5] Stage 4 — PATIENTS module; spec §"PATIENTS"

**Files:**
- Create: `docs/02-requirements/04-patients.md`

**US list (13):**
| ID | Title | Priority |
|----|-------|----------|
| US-PAT-001 | 分页查询患者列表 | Must |
| US-PAT-002 | 查看患者详情 | Must |
| US-PAT-003 | 创建患者 | Must |
| US-PAT-004 | 更新患者 | Must |
| US-PAT-005 | 删除患者（软删除，引用检查） | Must |
| US-PAT-006 | 启用/禁用患者 | Must |
| US-PAT-007 | 恢复软删除患者 | Should |
| US-PAT-008 | 批量删除患者 | Should |
| US-PAT-009 | 单个引用检查 | Must |
| US-PAT-010 | 批量引用检查 | Should |
| US-PAT-011 | 下载导入模板 | Should |
| US-PAT-012 | 导出患者 Excel | Should |
| US-PAT-013 | 敏感数据脱敏（存储与传输） | Must |

**Source:** PATIENTS spec above.

- [ ] **Step 1: Write header + sensitive data masking rules + 13 US**

- [ ] **Step 2: Verify count** = 13

- [ ] **Step 3: Commit** — `docs(req): 重写 04-patients.md（13 US，患者管理）`

---

### Task 11: Write `docs/02-requirements/05-herbs.md` (13 US)

**Covers:** [S5] Stage 4 — HERBS module; spec §"HERBS"

**Files:**
- Create: `docs/02-requirements/05-herbs.md`

**US list (13):**
| ID | Title | Priority |
|----|-------|----------|
| US-HERB-001 | 分页查询药材列表 | Must |
| US-HERB-002 | 查看药材详情 | Must |
| US-HERB-003 | 创建药材 | Must |
| US-HERB-004 | 更新药材 | Must |
| US-HERB-005 | 删除药材（软删除，引用检查） | Must |
| US-HERB-006 | 批量导入药材（Skip/Update/Error 策略） | Must |
| US-HERB-007 | 导出全部药材 | Should |
| US-HERB-008 | 单个引用检查 | Must |
| US-HERB-009 | 批量引用检查 | Should |
| US-HERB-010 | 启用/禁用药材 | Must |
| US-HERB-011 | 恢复软删除药材 | Should |
| US-HERB-012 | 批量操作（启用/禁用/删除） | Should |
| US-HERB-013 | 导出 Excel + 下载模板 | Should |

**Source:** HERBS spec above.

- [ ] **Step 1: Write header + record-only note + pinyin search rules + 13 US**

- [ ] **Step 2: Verify count** = 13

- [ ] **Step 3: Commit** — `docs(req): 重写 05-herbs.md（13 US，药材管理）`

---

### Task 12: Write `docs/02-requirements/06-formulas.md` (13 US)

**Covers:** [S5] Stage 4 — FORMULAS module; spec §"FORMULAS"

**Files:**
- Create: `docs/02-requirements/06-formulas.md`

**US list (13):**
| ID | Title | Priority |
|----|-------|----------|
| US-FORM-001 | 分页查询验方列表（按所有权） | Must |
| US-FORM-002 | 查看验方详情 | Must |
| US-FORM-003 | 创建验方（Draft 初始状态） | Must |
| US-FORM-004 | 更新验方（触发状态重新评估） | Must |
| US-FORM-005 | 删除验方（软删除） | Must |
| US-FORM-006 | 批量导入验方 | Must |
| US-FORM-007 | 查询待验证验方（Doctor to-do） | Must |
| US-FORM-008 | 验证单个药材（绑定系统药材） | Must |
| US-FORM-009 | 全部药材验证后自动晋升 Validated | Must |
| US-FORM-010 | 药材变更后降级 Draft（FLAW-F1） | Must |
| US-FORM-011 | 启用/禁用验方 | Must |
| US-FORM-012 | 恢复软删除验方 | Should |
| US-FORM-013 | 批量操作 + 导出 + 模板 | Should |

**Source:** FORMULAS spec above.

- [ ] **Step 1: Write header + validation state machine (Draft↔Validated) + herb binding rules + 13 US**

- [ ] **Step 2: Verify count** = 13

- [ ] **Step 3: Commit** — `docs(req): 重写 06-formulas.md（13 US，验方管理）`

---

### Task 13: Write `docs/02-requirements/07-medical-cases.md` (18 US — CORE)

**Covers:** [S5] Stage 4 — MEDICALCASES module; spec §"MEDICALCASES"

**Files:**
- Create: `docs/02-requirements/07-medical-cases.md`

**US list (18):**
| ID | Title | Priority |
|----|-------|----------|
| US-MC-001 | 创建医案（含诊断+处方聚合） | Must |
| US-MC-002 | 保存医案（统一聚合保存） | Must |
| US-MC-003 | 设置处方需求标志（3步工作流第2步） | Must |
| US-MC-004 | 查询医案详情（含诊断+处方） | Must |
| US-MC-005 | 分页查询医案列表（按角色过滤） | Must |
| US-MC-006 | 统一查询（ByPatient/Pending/Recent 等） | Must |
| US-MC-007 | 跨模块搜索（患者+诊断+日期） | Must |
| US-MC-008 | 查询诊断历史 | Should |
| US-MC-009 | 查询处方历史 | Should |
| US-MC-010 | 更新医案状态（Active/Suspended） | Must |
| US-MC-011 | 完成医案（工作流验证） | Must |
| US-MC-012 | 强制关闭医案 | Should |
| US-MC-013 | 暂停医案 | Must |
| US-MC-014 | 取消医案（软删除+打印保护） | Must |
| US-MC-015 | 删除/批量删除医案 | Must |
| US-MC-016 | 查询医案权限 | Must |
| US-MC-017 | 查询审计日志（20字段差异） | Should |
| US-MC-018 | 批量详情查询（≤50，解决 N+1） | Should |

**Business rules to document prominently:**
- BR-001: Single active case per patient
- Lock rule: Completed = editable same-day only
- Edit-reason requirements
- Print protection coupling
- Admin CANNOT create cases

**Source:** MEDICALCASES spec above — the most detailed.

- [ ] **Step 1: Write header + state machine diagram (mermaid) + permission matrix table + BR-001 explanation + all 18 US**

- [ ] **Step 2: Verify count** = 18

- [ ] **Step 3: Commit** — `docs(req): 重写 07-medical-cases.md（18 US，医案管理核心）`

---

### Task 14: Write `docs/02-requirements/08-registration.md` (7 US)

**Covers:** [S5] Stage 4 — REGISTRATION module; spec §"REGISTRATION"

**Files:**
- Create: `docs/02-requirements/08-registration.md`

**US list (7):**
| ID | Title | Priority |
|----|-------|----------|
| US-REG-001 | 前台创建挂号（Waiting 排队） | Must |
| US-REG-002 | 医生快速就诊（QuickVisit 原子事务） | Must |
| US-REG-003 | 查看挂号详情 | Must |
| US-REG-004 | 分页查询挂号 + 查看排队 | Must |
| US-REG-005 | 开始就诊（Waiting→InProgress） | Must |
| US-REG-006 | 取消挂号（仅 Waiting） | Must |
| US-REG-007 | 医案联动（完成/取消自动回写） | Must |

**Source:** REGISTRATION spec above.

- [ ] **Step 1: Write header + two-source model + status flow + QuickVisit atomicity + 7 US**

- [ ] **Step 2: Verify count** = 7

- [ ] **Step 3: Commit** — `docs(req): 重写 08-registration.md（7 US，挂号管理）`

---

### Task 15: Write `docs/02-requirements/09-printing.md` (4 US)

**Covers:** [S5] Stage 4 — PRINTING module; spec §"PRINTING"

**Files:**
- Create: `docs/02-requirements/09-printing.md`

**US list (4):**
| ID | Title | Priority |
|----|-------|----------|
| US-PRINT-001 | 打印处方（A5/A4，对话框/直打印） | Must |
| US-PRINT-002 | 处方预览 | Must |
| US-PRINT-003 | 导出处方（XPS/PDF） | Should |
| US-PRINT-004 | 打印记录回写服务器（成功/失败） | Must |

**Source:** PRINTING spec above.

- [ ] **Step 1: Write header + A5 default note + empty-prescription guard + print protection coupling + 4 US**

- [ ] **Step 2: Verify count** = 4

- [ ] **Step 3: Commit** — `docs(req): 重写 09-printing.md（4 US，处方打印）`

---

### Task 16: Write `docs/02-requirements/10-sync.md` (8 US)

**Covers:** [S5] Stage 4 — SYNC module; spec §"SYNC"

**Files:**
- Create: `docs/02-requirements/10-sync.md`

**US list (8):**
| ID | Title | Priority |
|----|-------|----------|
| US-SYNC-001 | 查询支持的同步实体类型 | Must |
| US-SYNC-002 | 获取实体元数据 | Must |
| US-SYNC-003 | 对比本地与服务端差异（Checksum） | Must |
| US-SYNC-004 | 上传本地独有实体 | Must |
| US-SYNC-005 | 下载服务端独有实体 | Must |
| US-SYNC-006 | 同步删除（引用检查） | Must |
| US-SYNC-007 | 冲突解决（逐项 UseLocal/Server/Skip） | Must |
| US-SYNC-008 | 错误分类与重试（Transient/Conflict/Auth） | Should |

**Source:** SYNC spec above.

- [ ] **Step 1: Write header + 5-phase workflow diagram (mermaid) + conflict resolution rules + 8 US**

- [ ] **Step 2: Verify count** = 8

- [ ] **Step 3: Commit** — `docs(req): 重写 10-sync.md（8 US，数据同步）`

---

### Task 17: Write `docs/02-requirements/11-platform.md` (35 US — merged)

**Covers:** [S5] Stage 4 — PLATFORM module; spec §"PLATFORM"

**Files:**
- Create: `docs/02-requirements/11-platform.md`

**Structure (6 H2 sections, 35 US total):**

```markdown
# 平台基础设施 (Platform)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建
> 合并原 Shell + Configuration + Error Handling + Logging + Health & Diagnostics + Card Reader 6 个模块

## Shell (7 US)
### US-SHELL-001 ~ US-SHELL-007

## Configuration (4 US)
### US-CFG-001 ~ US-CFG-004

## Error Handling (8 US)
### US-ERR-001 ~ US-ERR-008

## Logging & Audit (7 US)
### US-LOG-001 ~ US-LOG-007

## Health & Diagnostics (9 US)
### US-SYS-001 ~ US-SYS-009

## Card Reader (2 US)
### US-CARD-001 ~ US-CARD-002
```

**US lists per section (use old US IDs — pull titles from git history):**

Shell (7):
| ID | Title |
|----|-------|
| US-SHELL-001 | 应用启动（单实例） |
| US-SHELL-002 | 启动闪屏 |
| US-SHELL-003 | 角色基础模块加载 |
| US-SHELL-004 | 账户设置（个人资料+密码） |
| US-SHELL-005 | 菜单导航 |
| US-SHELL-006 | 全局异常处理 |
| US-SHELL-007 | 双模式连接切换 |

Configuration (4):
| ID | Title |
|----|-------|
| US-CFG-001 | 查询所有配置 |
| US-CFG-002 | 查询单个配置 |
| US-CFG-003 | 验证生产配置 |
| US-CFG-004 | 功能开关 |

Error Handling (8):
| ID | Title |
|----|-------|
| US-ERR-001 | 全局异常处理（Dispatcher+AppDomain） |
| US-ERR-002 | 中文友好错误消息 |
| US-ERR-003 | 追踪 ID（TraceId） |
| US-ERR-004 | CorrelationId 端到端追踪 |
| US-ERR-005 | 生产环境堆栈屏蔽 |
| US-ERR-006 | 验证错误统一格式（422） |
| US-ERR-007 | 业务异常分类 |
| US-ERR-008 | 异常层级（Validation/NotFound/Conflict → Business） |

Logging & Audit (7):
| ID | Title |
|----|-------|
| US-LOG-001 | 结构化日志（Serilog） |
| US-LOG-002 | 两阶段 Serilog 引导 |
| US-LOG-003 | 敏感数据脱敏 |
| US-LOG-004 | 审计日志（可配置保留期） |
| US-LOG-005 | 日志级别动态调整 |
| US-LOG-006 | CorrelationId 注入 |
| US-LOG-007 | 日志自动清理（默认 365 天） |

Health & Diagnostics (9):
| ID | Title |
|----|-------|
| US-SYS-001 | 匿名存活探针（/health） |
| US-SYS-002 | Ping 端点（/ping） |
| US-SYS-003 | 详细健康检查（/details，含 DB） |
| US-SYS-004 | 健康状态 503 返回 |
| US-SYS-005 | 日志级别状态查询 |
| US-SYS-006 | 启用调试模式（定时，≤120 分钟） |
| US-SYS-007 | 禁用调试模式 |
| US-SYS-008 | 设置显式日志级别 |
| US-SYS-009 | 调试模式自动过期 |

Card Reader (2):
| ID | Title |
|----|-------|
| US-CARD-001 | 身份证读卡（初始化+读取+自动读） |
| US-CARD-002 | 患者去重查找或创建（PRD-15） |

**Source:** PLATFORM spec above. Pull old US titles from `git show HEAD~6:docs/02-requirements/{12-desktop-shell,11-configuration,13-error-handling,14-logging,15-health-diagnostics,16-card-reader}.md` (adjust N based on commit count).

- [ ] **Step 1: Write header + 6 H2 sections + all 35 US** (7+4+8+7+9+2 = 37, but consolidate 2 redundant → 35. Document which 2 were removed at top of file.)

- [ ] **Step 2: Verify count**

```powershell
(Select-String -Path docs/02-requirements/11-platform.md -Pattern "^### US-(SHELL|CFG|ERR|LOG|SYS|CARD)-").Count
```
Expected: `35` (or 37 before consolidation — document the consolidation).

- [ ] **Step 3: Commit** — `docs(req): 重写 11-platform.md（35 US，合并 6 个平台模块）`

---

### Task 18: Write `docs/02-requirements/12-nfr.md`

**Covers:** [S5] Stage 5 — NFR

**Files:**
- Create: `docs/02-requirements/12-nfr.md`

**Content sections:**
1. `## 性能 (Performance)` — API response SLAs, list query times, sync throughput
2. `## 数据 (Data)` — capacity estimates, retention, backup
3. `## 可用性 (Availability)` — uptime, single-user local mode
4. `## 安全 (Security)` — JWT, sensitive data, DPAPI, audit
5. `## 可维护性 (Maintainability)` — architecture tests, code style
6. `## 兼容性 (Compatibility)` — Windows 10+, .NET 8, SQL Server

**NFR numbering:** `NFR-PERF-NNN`, `NFR-DATA-NNN`, `NFR-AVAIL-NNN`, `NFR-SEC-NNN`.

**Source:** `git show HEAD~7:docs/02-requirements/17-nfr.md` (adjust N). Remove any `FR-` references. Remove SQLite references.

**Length target:** 150-250 lines.

- [ ] **Step 1: Write the file** with all 6 sections. Keep NFR IDs stable (NFR-PERF-001 etc.) for cross-reference compatibility with `03-architecture/` and `05-development/`.

- [ ] **Step 2: Verify no stale references**

```powershell
Select-String -Path docs/02-requirements/12-nfr.md -Pattern "FR-","SQLite"
```
Expected: no matches (SQLite only allowed if explicitly noting deprecation).

- [ ] **Step 3: Commit**

```powershell
git add docs/02-requirements/12-nfr.md
git commit -m "docs(req): 重写 12-nfr.md（非功能需求）"
```

---

### Task 19: Update `docs/02-requirements/README.md` (finalize US overview)

**Covers:** [S2] 02-requirements/README, [S5] Stage 6

**Files:**
- Modify: `docs/02-requirements/README.md`

- [ ] **Step 1: Rewrite README.md** — replace placeholder statuses with "✅ 已完成". Add the full US overview table (all 136 US IDs + titles in one master table for cross-reference). Add US numbering convention section.

- [ ] **Step 2: Verify total US count**

```powershell
$total = 0
Get-ChildItem docs/02-requirements/02-*.md, docs/02-requirements/03-*.md, docs/02-requirements/04-*.md, docs/02-requirements/05-*.md, docs/02-requirements/06-*.md, docs/02-requirements/07-*.md, docs/02-requirements/08-*.md, docs/02-requirements/09-*.md, docs/02-requirements/10-*.md, docs/02-requirements/11-*.md | ForEach-Object {
  $c = (Select-String -Path $_.FullName -Pattern "^### US-").Count
  Write-Host "$($_.Name): $c"
  $total += $c
}
Write-Host "TOTAL: $total"
```
Expected: total = 136 (±2 acceptable; document any delta). Per-file: 13, 12, 13, 13, 13, 18, 7, 4, 8, 35.

- [ ] **Step 3: Commit**

```powershell
git add docs/02-requirements/README.md
git commit -m "docs(req): 完善 README.md（US 总览表 + 编号规则）"
```

---

### Task 20: Fix cross-references in other doc directories

**Covers:** [S5] Stage 6 — verify cross-references

**Files (modify — broken links identified by explore-1):**

| File | Old reference pattern | New target |
|------|----------------------|------------|
| `docs/README.md` | `01-product/` (claims 10 files), `02-requirements/` (claims "15 modules") | Update to 4 files + 10 modules/136 US; fix deep links |
| `docs/AGENTS.md` | `01-product/` "8 files", `02-requirements/` "15 modules" | Update to 4 files + 10 modules |
| `docs/03-architecture/02-desktop.md` | ~14 links to `07-medical-cases.md`, `16-card-reader.md`, `18-ui-patterns.md`, `02-auth.md`, `13-error-handling.md`, `12-desktop-shell.md`, `10-sync.md`, `17-nfr.md` + stale `FR-` prefixes | Remap to new paths (`07-medical-cases.md` unchanged, `16-card-reader.md` → `11-platform.md#card-reader`, `12-desktop-shell.md` → `11-platform.md#shell`, `13-error-handling.md` → `11-platform.md#error-handling`, `14-logging.md` → `11-platform.md#logging-audit`, `15-health-diagnostics.md` → `11-platform.md#health-diagnostics`, `11-configuration.md` → `11-platform.md#configuration`, `18-ui-patterns.md` → DELETED, `17-nfr.md` → `12-nfr.md`). Replace `FR-XXX-NNN` with `US-XXX-NNN` |
| `docs/03-architecture/03-server.md` | ~11 links to `13-error-handling.md`, `17-nfr.md`, `14-logging.md`, `11-configuration.md`, `15-health-diagnostics.md`, `02-auth.md` + `FR-` prefixes | Same remapping as above |
| `docs/03-architecture/07-configuration.md` | Link to `11-configuration.md` | Remap to `11-platform.md#configuration` |
| `docs/04-api-reference/02-users.md` | Link to `03-users.md` | Path unchanged (03-users.md still exists) |
| `docs/04-api-reference/03-patients.md` | `04-patients.md` FR-PAT-011/012 | Path unchanged; replace `FR-` with `US-` |
| `docs/04-api-reference/04-herbs.md` | Link to `05-herbs.md` | Path unchanged |
| `docs/04-api-reference/05-formulas.md` | Link to `06-formulas.md` | Path unchanged |
| `docs/04-api-reference/06-medical-cases.md` | Link to `07-medical-cases.md` | Path unchanged |
| `docs/04-api-reference/09-sync.md` | Link to `10-sync.md` | Path unchanged |
| `docs/04-api-reference/README.md` | Link to `02-auth.md` | Path unchanged |
| `docs/05-development/standards/STD-01-CQRS-Boundary.md` | Link to `07-medical-cases.md` | Path unchanged |
| `docs/05-development/standards/STD-02-CorrelationId.md` | Links to `14-logging.md`, `17-nfr.md` | Remap: `14-logging.md` → `11-platform.md#logging-audit`, `17-nfr.md` → `12-nfr.md` |
| `docs/05-development/standards/STD-04-SensitiveData.md` | Links to `14-logging.md`, `04-patients.md` | `14-logging.md` → `11-platform.md#logging-audit` |
| `docs/05-development/standards/STD-06-JWT-Security.md` | Links to `02-auth.md`, `03-users.md` | Paths unchanged |
| `docs/05-development/09-performance-baseline.md` | `17-nfr.md` NFR-PERF-001~004 | `17-nfr.md` → `12-nfr.md` |

**Key remapping rules:**
- `02-auth.md` → `02-auth.md` (UNCHANGED)
- `03-users.md` → `03-users.md` (UNCHANGED)
- `04-patients.md` → `04-patients.md` (UNCHANGED)
- `05-herbs.md` → `05-herbs.md` (UNCHANGED)
- `06-formulas.md` → `06-formulas.md` (UNCHANGED)
- `07-medical-cases.md` → `07-medical-cases.md` (UNCHANGED)
- `08-registration.md` → `08-registration.md` (UNCHANGED)
- `09-printing.md` → `09-printing.md` (UNCHANGED)
- `10-sync.md` → `10-sync.md` (UNCHANGED)
- `11-configuration.md` → `11-platform.md#configuration`
- `12-desktop-shell.md` → `11-platform.md#shell`
- `13-error-handling.md` → `11-platform.md#error-handling`
- `14-logging.md` → `11-platform.md#logging-audit`
- `15-health-diagnostics.md` → `11-platform.md#health-diagnostics`
- `16-card-reader.md` → `11-platform.md#card-reader`
- `17-nfr.md` → `12-nfr.md`
- `18-ui-patterns.md` → DELETED (content merged elsewhere or dropped — verify in Task 21)
- `19-user-story-map.md` → DELETED
- `20-roadmap.md` → DELETED
- `21-role-permission-matrix.md` → DELETED (merged into `01-prd.md`)
- `01-product/01-vision.md` → UNCHANGED path
- `01-product/02-personas.md` → UNCHANGED path
- `01-product/03-glossary.md` → UNCHANGED path (was `07-glossary.md`)
- `01-product/04-user-roles.md` → DELETED (merged into `01-product/02-personas.md` + `02-requirements/01-prd.md`)
- `01-product/05-feature-list.md` → DELETED (merged into `01-vision.md`)
- `01-product/06-clinical-workflow.md` → DELETED (merged into `01-vision.md`)
- `01-product/03-jtbd.md` → DELETED (merged into `01-vision.md`)
- `01-product/08-value-proposition.md` → DELETED (merged into `01-vision.md`)
- `01-product/09-customer-journey.md` → DELETED (merged into `01-vision.md`)
- All `FR-XXX-NNN` → `US-XXX-NNN` (verify prefix mapping: FR-AUTH→US-AUTH, FR-MC→US-MC, etc.)

- [ ] **Step 1: Find ALL broken links**

```powershell
Select-String -Path docs/03-architecture/*.md, docs/04-api-reference/*.md, docs/05-development/**/*.md, docs/README.md, docs/AGENTS.md -Pattern "01-product/0[4-9]","01-product/0[1-3]-","02-requirements/(1[1-9]|2[01])-","FR-"
```

This lists every line with a broken reference. Review the output.

- [ ] **Step 2: Fix `docs/README.md`** — update the two directory entries (file counts, module count, US count) and the 3 deep links.

- [ ] **Step 3: Fix `docs/AGENTS.md`** — update `01-product/` and `02-requirements/` rows in the subdirectory table.

- [ ] **Step 4: Fix `docs/03-architecture/02-desktop.md`** — apply remapping rules. Replace `FR-` with `US-`.

- [ ] **Step 5: Fix `docs/03-architecture/03-server.md`** — apply remapping rules.

- [ ] **Step 6: Fix `docs/03-architecture/07-configuration.md`** — remap single link.

- [ ] **Step 7: Fix `docs/05-development/standards/STD-02-CorrelationId.md`** — remap `14-logging.md` → `11-platform.md#logging-audit`, `17-nfr.md` → `12-nfr.md`.

- [ ] **Step 8: Fix `docs/05-development/standards/STD-04-SensitiveData.md`** — remap `14-logging.md`.

- [ ] **Step 9: Fix `docs/05-development/09-performance-baseline.md`** — remap `17-nfr.md` → `12-nfr.md`.

- [ ] **Step 10: Verify no broken links remain**

```powershell
Select-String -Path docs/**/*.md -Pattern "01-product/(0[4-9]|06)","02-requirements/(1[1-9]|2[01])-","FR-[A-Z]"
```
Expected: no matches (except possibly inside this plan doc or historical plan docs in `docs/plans/` — those are OK as historical record).

- [ ] **Step 11: Commit**

```powershell
git add docs/03-architecture/ docs/04-api-reference/ docs/05-development/ docs/README.md docs/AGENTS.md
git commit -m "docs: 修复跨目录引用（重映射到新需求文档结构）"
```

---

### Task 21: Handle UI patterns document decision

**Covers:** [S4] improvement #2

**Files:**
- Decision: `18-ui-patterns.md` was deleted. Its content (UI-D01~D06 patterns) needs a home.

- [ ] **Step 1: Check if any surviving doc references `18-ui-patterns.md`**

```powershell
Select-String -Path docs/**/*.md -Pattern "18-ui-patterns"
```

- [ ] **Step 2: Decision**

If references exist in `03-architecture/02-desktop.md` or elsewhere:
- Option A (recommended): Remove the references — UI patterns are implementation detail, not requirements.
- Option B: Migrate essential UI-D0n references into `01-product/01-vision.md` §"核心场景" as brief notes.

If the `UI-D0n` IDs are referenced by code or tests, preserve them in `11-platform.md` under a new `## UI Patterns` subsection.

- [ ] **Step 3: Apply the decision** and update any referencing docs.

- [ ] **Step 4: Commit** — `docs: 处理 UI 模式文档（删除/迁移）`

---

### Task 22: Final verification + summary commit

**Covers:** [S5] Stage 6

- [ ] **Step 1: Verify file structure**

```powershell
Write-Host "=== 01-product/ ==="
Get-ChildItem docs/01-product -Name
Write-Host "=== 02-requirements/ ==="
Get-ChildItem docs/02-requirements -Name
```
Expected:
- 01-product: README.md, 01-vision.md, 02-personas.md, 03-glossary.md (4 files)
- 02-requirements: README.md, 01-prd.md, 02-auth.md through 12-nfr.md (12 files)

- [ ] **Step 2: Verify US count per module**

```powershell
Get-ChildItem docs/02-requirements/0[2-9]-*.md, docs/02-requirements/1[0-1]-*.md | ForEach-Object {
  $c = (Select-String -Path $_.FullName -Pattern "^### US-").Count
  Write-Host "$($_.Name): $c US"
}
```
Expected: 13, 12, 13, 13, 13, 18, 7, 4, 8, 35 (total 136).

- [ ] **Step 3: Verify no stale `FR-` prefixes in new docs**

```powershell
Select-String -Path docs/01-product/*.md, docs/02-requirements/*.md -Pattern "FR-[A-Z]"
```
Expected: no matches.

- [ ] **Step 4: Verify no SQLite references in new docs (except explicit deprecation notes)**

```powershell
Select-String -Path docs/01-product/*.md, docs/02-requirements/*.md -Pattern "SQLite"
```
Expected: no matches, or only matches inside `## 系统边界` noting deprecation.

- [ ] **Step 5: Verify no broken internal links**

```powershell
Select-String -Path docs/01-product/*.md, docs/02-requirements/*.md -Pattern "\]\(0[1-9]-" | ForEach-Object {
  $link = $_.Line -replace '.*\]\(([^)]+)\).*','$1'
  $fullPath = Join-Path (Split-Path $_.Path) $link
  if (-not (Test-Path $fullPath)) { Write-Host "BROKEN: $($_.Path) -> $link" }
}
```
Expected: no BROKEN lines.

- [ ] **Step 6: Update MEMORY.md** — mark "需求文档体系重建" as COMPLETE in `## Next goal` section. Record final US count and any deltas from the 138 baseline.

- [ ] **Step 7: Final summary commit (if any straggler changes)**

```powershell
git add -A
git status  # review
git commit -m "docs: 需求文档体系重建完成（15模块→10模块，138→136 US）"
```

---

## Self-Review Checklist

**Spec coverage:**
- [S1] 目标 → Task 1 (delete) + Tasks 3-19 (rebuild) ✓
- [S2] 文档体系 → Tasks 2-19 (all files) ✓
- [S3] US 格式 → Shared Conventions + Task 8 example US ✓
- [S4] 关键改进 → Task 1 (delete redundant), Task 3 (merge jtbd/value-prop/journey), Task 17 (merge 6→1 platform), Task 7 (merge role matrix) ✓
- [S5] 执行计划 → Stage 1: Tasks 1-2; Stage 2: Tasks 3-6; Stage 3: Tasks 7-8; Stage 4: Tasks 8-17; Stage 5: Task 18; Stage 6: Tasks 19-22 ✓

**Placeholder scan:** No "TBD" or "implement later". Every US task has a full US ID list. Module specs provide business rules. ✓

**Type consistency:** US prefixes consistent throughout (US-AUTH, US-USER, US-PAT, US-HERB, US-FORM, US-MC, US-REG, US-PRINT, US-SYNC, US-SHELL/CFG/ERR/LOG/SYS/CARD). File paths consistent between file-structure section and individual tasks. ✓

---

## Execution Notes

- **Commit per task** — each task ends with a commit. Do not batch multiple tasks into one commit (except Task 20 which has multiple steps but one logical change).
- **Use git history** — old content is accessible via `git show HEAD~N:path`. Use it for content reference, not copy-paste (rewrite per new structure).
- **Chinese prose, English identifiers** — per AGENTS.md code style.
- **No v2.0 scope creep** — per MEMORY.md: "先不着眼扩展". Document v2.0 only in `## 系统边界` / `## 范围` sections.
- **No Emoji in code** — but US status `✅ 已实现` is acceptable in documentation (it's content, not code).
