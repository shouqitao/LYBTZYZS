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

