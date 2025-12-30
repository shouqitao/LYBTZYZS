# Spec Delta: client-api-conventions

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Refit Interface Naming Convention

Client-side Refit API interfaces SHALL follow consistent naming conventions with explicit return type requirements.

#### Scenario: Delete method signature
- **WHEN** defining delete methods in Refit interfaces
- **THEN** methods SHALL:
  - Return `Task<ApiResponse>` (not `Task<ApiResponse<T>>`)
  - Use pattern `Delete{Entity}Async(Guid id)`
  - Include `[Delete]` attribute with proper route

**Change Note**: Added explicit return type requirement for delete methods. Previously no return type was specified.
