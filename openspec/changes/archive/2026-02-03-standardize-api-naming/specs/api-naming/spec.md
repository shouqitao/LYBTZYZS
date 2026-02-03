# API Naming Standards

## MODIFIED Requirements

### Requirement: REQ-API-001 URL Path Naming Convention

All API URL paths MUST use kebab-case naming convention.

#### Scenario: Formula batch import URL follows kebab-case

**Given** the IFormulaApi interface defines BatchImportAsync endpoint
**When** the endpoint URL is defined
**Then** the URL path must be `/api/v1/formulas/batch-import`
**And** not use shortened names like `/import`

### Requirement: REQ-API-002 Batch Operation URL Pattern

Batch operation endpoints MUST follow the `/batch-{action}` naming pattern where applicable.

#### Scenario: Formula batch import URL follows batch pattern

**Given** the IFormulaApi interface defines BatchImportAsync endpoint
**When** the endpoint URL is defined
**Then** the URL path must be `/api/v1/formulas/batch-import`
**And** not use shortened names like `/import`

**Note**: IHerbApi.BatchImportAsync uses `/import` for Multipart file upload, which is acceptable as it semantically represents file import rather than batch data operation.

### Requirement: REQ-API-003 Consistent Return Types

All API methods MUST use the project's standard `ApiResponse` or `ApiResponse<T>` return types.

#### Scenario: Delete operation returns ApiResponse

**Given** the IMedicalCaseApi interface defines DeleteMedicalCaseAsync method
**When** the method return type is defined
**Then** it must return `Task<ApiResponse>` or `Task<ApiResponse<T>>`
**And** not use the low-level `Refit.IApiResponse` type

## REMOVED Requirements

### Requirement: REQ-API-001-AUTH (Removed)

~~Auth API URL follows kebab-case~~

**Reason**: The `ChangeSysAdminPasswordAsync` endpoint was removed from server in Issue #1909. The corresponding client-side method definition is a ghost API and should be deleted rather than renamed.

### Requirement: REQ-API-002-HERB (Removed)

~~Herb batch import URL follows batch pattern~~

**Reason**: HerbsController has two import endpoints:
- `/import` - Multipart file upload for Excel import
- `/batch-import` - JSON Body for batch data import

Renaming the Multipart endpoint would cause URL conflict. The current naming is semantically appropriate for file import operations.
