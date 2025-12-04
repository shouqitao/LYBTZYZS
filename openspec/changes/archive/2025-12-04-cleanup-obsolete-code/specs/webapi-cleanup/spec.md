# webapi-cleanup Specification Delta

## ADDED Requirements

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
