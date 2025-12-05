# webapi-cleanup Specification

## Purpose
TBD - created by archiving change refactor-webapi-layer. Update Purpose after archive.
## Requirements
### Requirement: Dead Endpoint Identification

The system SHALL maintain a documented inventory of API endpoints that have no corresponding Client-side calls (Dead Endpoints).

#### Scenario: Endpoint usage analysis
- **WHEN** analyzing Server-side API endpoints
- **THEN** each endpoint SHALL be categorized as:
  - Active (has Client caller)
  - Dead (no Client caller)
  - Operational (for monitoring/maintenance only)

#### Scenario: Dead endpoint documentation
- **WHEN** a Dead Endpoint is identified
- **THEN** documentation SHALL include:
  - Endpoint path and HTTP method
  - Reason for being unused
  - Recommendation (keep for future/remove/deprecate)

### Requirement: Batch Operation Pattern Consistency

The system SHALL use a consistent pattern for batch operations across all Controllers.

#### Scenario: Batch delete pattern selection
- **WHEN** implementing batch delete functionality
- **THEN** the system SHALL use ONE of the following patterns consistently:
  - Server-side batch endpoint (single API call)
  - Client-side iteration (multiple single-delete calls)

#### Scenario: Pattern documentation
- **WHEN** a batch operation pattern is chosen
- **THEN** the pattern SHALL be documented in `webapi-conventions` spec

### Requirement: Controller Size Guidelines

The system SHALL follow guidelines for Controller class size to maintain readability and single responsibility.

#### Scenario: Large controller warning
- **WHEN** a Controller exceeds 500 lines of code
- **THEN** the system SHOULD evaluate splitting into sub-controllers or extracting to services

#### Scenario: MedicalCaseController evaluation
- **WHEN** evaluating MedicalCaseController (currently 1192 lines)
- **THEN** the system SHALL document:
  - Current responsibility breakdown
  - Potential split strategy
  - Decision rationale (split vs keep)

### Requirement: Dead Endpoint Removal

The system SHALL remove all API endpoints that have been marked as `[Obsolete]` and have no active Client callers.

#### Scenario: Obsolete endpoint deletion
- **WHEN** an API endpoint is marked with `[Obsolete]` attribute
- **AND** no Client-side code references this endpoint
- **THEN** the endpoint SHALL be removed from the codebase
- **AND** related tests (if any) SHALL be removed
- **AND** the removal SHALL be documented in CHANGELOG

#### Scenario: CacheHealthController removal
- **WHEN** the CacheHealthController is evaluated
- **AND** it has `[Obsolete]` attribute on class level
- **AND** no Client calls any of its endpoints
- **THEN** the entire Controller file SHALL be deleted
- **AND** no Service/Repository layer changes are required

### Requirement: Batch Operation Pattern Enforcement

The system SHALL enforce Client-side iteration pattern for all batch delete operations.

#### Scenario: Server-side batch delete endpoint removal
- **WHEN** a Controller has a batch delete endpoint (e.g., `POST /batch-delete`)
- **AND** the endpoint is marked `[Obsolete]`
- **THEN** the endpoint SHALL be removed
- **AND** Client SHALL use iteration over single-delete endpoint instead

#### Scenario: Affected batch delete endpoints
- **WHEN** cleaning up batch delete endpoints
- **THEN** the following endpoints SHALL be removed:
  - `HerbsController.BatchDeleteHerbs`
  - `FormulasController.BatchDeleteFormulas`
  - `UsersController.BatchDeleteUsers`

### Requirement: Deprecated State Transition Endpoint Cleanup

The system SHALL remove deprecated state transition endpoints that have been superseded by unified status update endpoints.

#### Scenario: CompleteMedicalCase endpoint removal
- **WHEN** `MedicalCaseController.CompleteMedicalCase` is evaluated
- **AND** it is marked `[Obsolete]` with message referencing `PUT /{id}/status`
- **THEN** the endpoint SHALL be removed
- **AND** Clients SHALL use `PUT /{id}/status` with `Completed` status instead

#### Scenario: ToggleStatus endpoint removal
- **WHEN** `UsersController.ToggleStatus` is evaluated
- **AND** it is marked `[Obsolete]`
- **AND** no Client uses this endpoint
- **THEN** the endpoint SHALL be removed

### Requirement: Dead Code File Removal

The system SHALL remove code files that have no external references and serve no functional purpose.

#### Scenario: Unused configuration options removal
- **WHEN** a configuration options class is defined
- **AND** no code binds, reads, or references the configuration class
- **THEN** the configuration file SHALL be deleted
- **AND** no runtime behavior SHALL be affected

#### Scenario: Duplicate internal class removal
- **WHEN** multiple classes provide the same functionality
- **AND** one implementation is actively used while another is never called
- **THEN** the unused implementation SHALL be removed
- **AND** the active implementation SHALL remain unchanged

### Requirement: Configuration Options Consolidation

The system SHALL use a single, unified configuration approach for WebAPI settings.

#### Scenario: LybtOptions as single source of truth
- **WHEN** WebAPI needs configuration values
- **THEN** all settings SHALL be read from `LybtOptions` class hierarchy
- **AND** no separate `WebApiConfigurationOptions` class SHALL exist
- **AND** Swagger, JSON, and performance settings SHALL be part of `LybtOptions`

