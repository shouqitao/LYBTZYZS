# dto-cleanup Specification

## Purpose
TBD - created by archiving change cleanup-obsolete-code. Update Purpose after archive.
## Requirements
### Requirement: Unused DTO Removal

The system SHALL remove all DTO classes that have no code references outside their definition file.

#### Scenario: FormulaAnalysisDtos file deletion
- **WHEN** the FormulaAnalysisDtos.cs file is evaluated
- **AND** none of its 6 DTO classes are referenced by any code
- **THEN** the entire file SHALL be deleted
- **AND** the deletion SHALL be documented in CHANGELOG

#### Scenario: MedicalCaseDtos partial cleanup
- **WHEN** MedicalCaseDtos.cs is evaluated
- **AND** CompleteMedicalCaseDto, SuspendMedicalCaseDto, ArchiveMedicalCaseDto, DoctorMedicalCaseStatisticsDto have no references
- **AND** these DTOs are superseded by UpdateMedicalCaseStatusDto or never implemented
- **THEN** these 4 DTO classes SHALL be removed from the file
- **AND** remaining DTOs with references SHALL be preserved

#### Scenario: PatientOperationDtos partial cleanup
- **WHEN** PatientOperationDtos.cs is evaluated
- **AND** PatientVisitHistoryDto, PatientProfileManagementDto have no references
- **THEN** these 2 DTO classes SHALL be removed from the file
- **AND** remaining DTOs with references SHALL be preserved

#### Scenario: HerbOperationDtos partial cleanup
- **WHEN** HerbOperationDtos.cs is evaluated
- **AND** CompatibilitySuggestionDto, HerbSpecialPriceDto have no references
- **THEN** these 2 DTO classes SHALL be removed from the file
- **AND** remaining DTOs with references SHALL be preserved

### Requirement: DTO Cleanup Validation

The system SHALL verify no compilation errors after DTO removal.

#### Scenario: Build verification
- **WHEN** unused DTOs are removed
- **THEN** `dotnet build LYBT.All.sln` SHALL succeed without errors
- **AND** no new warnings SHALL be introduced

#### Scenario: Test verification
- **WHEN** unused DTOs are removed
- **THEN** all existing unit tests SHALL continue to pass
- **AND** all integration tests SHALL continue to pass

