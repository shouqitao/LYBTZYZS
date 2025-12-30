# client-api-conventions Specification

## Purpose

定义WPF Desktop客户端Refit API接口的设计规范，确保前后端API契约一致性，与webapi-cleanup规范形成互补。

## Cross-Reference

- **webapi-cleanup**: Server端API清理规范，本spec与其保持一致
- **dto-architecture**: DTO设计模式规范
## Requirements
### Requirement: Refit Interface Naming Convention

Client-side Refit API interfaces SHALL follow consistent naming conventions with explicit return type requirements.

#### Scenario: Delete method signature
- **WHEN** defining delete methods in Refit interfaces
- **THEN** methods SHALL:
  - Return `Task<ApiResponse>` (not `Task<ApiResponse<T>>`)
  - Use pattern `Delete{Entity}Async(Guid id)`
  - Include `[Delete]` attribute with proper route

**Change Note**: Added explicit return type requirement for delete methods. Previously no return type was specified.

### Requirement: Batch Operation Pattern

Client-side batch operations SHALL use client-side iteration pattern.

#### Scenario: Batch delete implementation
- **WHEN** implementing batch delete functionality
- **THEN** the Client SHALL:
  - Loop through selected items
  - Call single-item delete API for each item
  - NOT call any server-side batch-delete endpoint

#### Scenario: Batch delete in ViewModel
- **WHEN** implementing `OnExecuteBatchDeleteAsync` in ViewModel
- **THEN** the implementation SHALL:
  - Iterate through the item list
  - Call `DeleteAsync` for each item via Repository/Api
  - Aggregate results for user feedback

**Rationale**: Server-side batch endpoints are marked obsolete. Client-side iteration provides:
- Better error isolation (one failure doesn't affect others)
- Simpler transaction management
- Consistent behavior across all modules

### Requirement: Aggregate Root API Access

APIs for DDD aggregate child entities SHALL be accessed through the aggregate root path.

#### Scenario: MedicalCase child resources
- **GIVEN** MedicalCase is the aggregate root
- **WHEN** accessing Consultation or Prescription data
- **THEN** the API path SHALL be:
  - `PUT /api/v1/medicalcases/{id}/consultation` - for Consultation
  - `POST /api/v1/medicalcases/{id}/prescription` - for Prescription
  - NOT `/api/v1/consultations/{id}` or `/api/v1/prescriptions/{id}`

#### Scenario: IMedicalCaseApi design
- **WHEN** designing IMedicalCaseApi interface
- **THEN** it SHALL include methods for child resources:
  - `UpdateConsultationAsync(Guid medicalCaseId, ...)`
  - `CreatePrescriptionAsync(Guid medicalCaseId, ...)`
  - Child resource methods SHALL take aggregate root ID as first parameter

### Requirement: Status Update Pattern

Status changes SHALL use standard update APIs, not specialized toggle endpoints.

#### Scenario: Entity status change
- **WHEN** changing an entity's status (e.g., Enabled/Disabled)
- **THEN** the Client SHALL:
  - Use the standard `Update{Entity}Async` method
  - Pass the complete entity with new status value
  - NOT call any `/toggle-status` endpoint

#### Scenario: MedicalCase status update
- **WHEN** updating MedicalCase status
- **THEN** use one of:
  - `UpdateStatusAsync` - for general status changes
  - `CloseCaseAsync` - for completing a case (convenience method)
  - NOT `CompleteMedicalCase` (deprecated)

### Requirement: Error Handling Convention

Refit API calls SHALL handle errors consistently.

#### Scenario: API response handling
- **WHEN** processing API responses
- **THEN** the Client SHALL:
  - Check `ApiResponse.IsSuccess` before accessing data
  - Log errors with appropriate context
  - Display user-friendly error messages

#### Scenario: Network error handling
- **WHEN** a network error occurs during API call
- **THEN** the Client SHALL:
  - Catch `Refit.ApiException` or `HttpRequestException`
  - Provide meaningful error message to user
  - NOT expose raw exception details in UI

### Requirement: Standard API Function Matrix

Each business entity API SHALL implement a consistent set of standard methods.

#### Scenario: Basic CRUD methods
- **GIVEN** a new entity API interface is created
- **WHEN** defining the interface methods
- **THEN** it SHALL include at minimum:
  - `Get{Entities}Async` - list query
  - `Get{Entity}ByIdAsync` - single entity query
  - `Create{Entity}Async` - create entity
  - `Update{Entity}Async` - update entity
  - `Delete{Entity}Async` - delete entity (returns `ApiResponse`, NOT `ApiResponse<T>`)

#### Scenario: Batch operation methods
- **GIVEN** an entity supports list selection operations
- **WHEN** defining batch methods
- **THEN** the interface SHALL include:
  - `BatchDeleteAsync` - batch soft delete
  - `BatchEnableAsync` - batch enable (if entity has Status field)
  - `BatchDisableAsync` - batch disable (if entity has Status field)

#### Scenario: Status management methods
- **GIVEN** an entity has Status field and supports soft delete
- **WHEN** defining status methods
- **THEN** the interface SHALL include:
  - `ToggleStatusAsync` - toggle enabled/disabled state
  - `RestoreAsync` - restore soft-deleted entity

#### Scenario: Import/Export methods
- **GIVEN** an entity supports batch data entry
- **WHEN** defining import/export methods
- **THEN** the interface SHALL include:
  - `BatchImportAsync` - batch import data
  - `ExportTemplateAsync` - export empty template
  - `Export{Entities}Async` - export entity data

### Requirement: Delete Method Return Type

Delete operations SHALL return non-generic ApiResponse.

#### Scenario: Single delete return type
- **WHEN** defining `Delete{Entity}Async` method
- **THEN** the return type SHALL be `Task<ApiResponse>`
- **AND** the return type SHALL NOT be `Task<ApiResponse<ApiResponse>>` or `Task<ApiResponse<T>>`

**Rationale**: Delete operations do not return entity data. Only success/failure status is needed.

#### Scenario: Batch delete return type
- **WHEN** defining `BatchDeleteAsync` method
- **THEN** the return type SHALL be `Task<ApiResponse>`

### Requirement: No Duplicate Methods

API interfaces SHALL NOT contain methods with overlapping functionality.

#### Scenario: Search vs Query methods
- **WHEN** an interface needs search capability
- **THEN** it SHALL have only ONE search method (prefer `Search{Entities}Async`)
- **AND** it SHALL NOT have both `Query{Entities}Async` and `Search{Entities}Async`

**Rationale**: Duplicate methods cause confusion and maintenance burden.

