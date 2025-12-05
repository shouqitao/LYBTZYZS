## ADDED Requirements

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
