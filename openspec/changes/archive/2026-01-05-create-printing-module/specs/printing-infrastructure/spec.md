## ADDED Requirements

### Requirement: Print Service Module

The system SHALL provide a dedicated Printing module (`LYBT.Desktop.Printing`) as a Core-level infrastructure component for all printing operations.

#### Scenario: Module initialization
- **WHEN** the application starts
- **THEN** the PrintingModule SHALL be loaded by the Shell
- **AND** print services SHALL be available for dependency injection

#### Scenario: Module location
- **WHEN** referencing the Printing module
- **THEN** it SHALL be located at `src/Client/Desktop/Core/LYBT.Desktop.Printing/`
- **AND** it SHALL target net8.0-windows framework

---

### Requirement: Generic Print Service Interface

The system SHALL provide a generic print service interface `IPrintService<TModel>` for type-safe printing operations.

#### Scenario: Print document
- **WHEN** calling `PrintAsync(model, options)`
- **THEN** the system SHALL render the model using the associated template
- **AND** display a print dialog for printer selection
- **AND** return true if printing completes successfully

#### Scenario: Preview document
- **WHEN** calling `PreviewAsync(model, options)`
- **THEN** the system SHALL display a preview window
- **AND** the preview SHALL include print settings panel (printer, copies, paper size)
- **AND** the user MAY proceed to print from the preview

#### Scenario: Export document
- **WHEN** calling `ExportAsync(model, filePath, format)`
- **THEN** the system SHALL export the document to the specified file path
- **AND** return true if export completes successfully
- **AND** support XPS format in MVP phase

---

### Requirement: Print Options

The system SHALL provide a `PrintOptions` class for configuring print operations.

#### Scenario: Configure printer
- **WHEN** setting `PrinterName` in options
- **THEN** the system SHALL use the specified printer
- **OR** fall back to system default if not specified

#### Scenario: Configure copies
- **WHEN** setting `Copies` in options
- **THEN** the system SHALL print the specified number of copies
- **AND** default to 1 copy if not specified

#### Scenario: Configure paper size
- **WHEN** setting `PaperSize` in options
- **THEN** the system SHALL use the specified paper size
- **AND** support A4 and A5 sizes
- **AND** default to A5 for prescription printing

---

### Requirement: Prescription Print Template

The system SHALL provide a XAML-based template for prescription printing.

#### Scenario: Template content
- **WHEN** printing a prescription
- **THEN** the template SHALL display clinic information (name, address, phone)
- **AND** patient information (name, gender, age)
- **AND** diagnosis information (present illness, tongue, pulse, TCM diagnosis)
- **AND** prescription details (herb items with dosage, dose count, usage)
- **AND** fee information (single dose price, total price)
- **AND** doctor information and prescription date

#### Scenario: Template sizing
- **WHEN** using A5 paper size
- **THEN** the template SHALL fit within 148mm x 210mm dimensions
- **AND** use appropriate font sizes for readability

#### Scenario: Template location
- **WHEN** referencing the prescription template
- **THEN** it SHALL be located at `Printing/Templates/PrescriptionPrintTemplate.xaml`

---

### Requirement: Print Model Convention

The system SHALL use naming conventions to associate print models with templates.

#### Scenario: Model-template association
- **WHEN** a print model class is named `{Name}PrintModel`
- **THEN** the system SHALL locate the template at `Templates/{Name}PrintTemplate.xaml`
- **AND** use this template for rendering

#### Scenario: Prescription model
- **WHEN** using `PrescriptionPrintModel`
- **THEN** it SHALL include properties for clinic, patient, diagnosis, prescription, and fee information
- **AND** SHALL be located at `Printing/Models/PrescriptionPrintModel.cs`

---

### Requirement: DI Registration

The system SHALL register print services following the project's DI conventions.

#### Scenario: Shell registration
- **WHEN** the Shell application initializes
- **THEN** the PrintingModule SHALL be registered
- **AND** `IPrintService<PrescriptionPrintModel>` SHALL be resolvable

#### Scenario: Business module consumption
- **WHEN** a business module (e.g., Clinical) needs printing
- **THEN** it SHALL inject `IPrintService<TModel>` via constructor
- **AND** NOT directly reference the PrintService implementation
