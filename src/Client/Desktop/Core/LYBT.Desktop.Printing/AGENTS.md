<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Printing

## Purpose
Printing service module providing prescription printing, preview, and PDF export functionality for the TCM clinic desktop application. Uses QuestPDF for high-quality PDF generation and WPF FixedDocument/XPS for print preview and direct printing. Implements the generic `IPrintService<TModel>` interface for type-safe print operations. Currently supports `PrescriptionPrintService` for TCM prescription printing with A5 and A4 paper sizes, including continuation templates for multi-page prescriptions. Registers as a Prism module with singleton print service.

## Key Files
| File | Description |
|------|-------------|
| `PrintingModule.cs` | Prism IModule entry point; registers `IPrintService<PrescriptionPrintModel>` as singleton |
| `Interfaces/IPrintService.cs` | Generic print service interface (Print, Preview, Export, BatchPrint, GetAvailablePrinters) |
| `Services/PrescriptionPrintService.cs` | Prescription-specific print implementation (WPF FixedDocument + XPS) |
| `Services/PrescriptionPdfExporter.cs` | QuestPDF-based PDF export for prescriptions |
| `Models/PrescriptionPrintModel.cs` | Print data model containing prescription content for rendering |
| `Models/PrintLogEntry.cs` | Audit log entry for print operations |
| `Templates/PrescriptionPrintTemplate.xaml` | WPF XAML template for A5 prescription printing |
| `Templates/PrescriptionPrintA4Template.xaml` | WPF XAML template for A4 prescription printing |
| `Templates/PrescriptionContinuationTemplate.xaml` | A5 continuation template for multi-page prescriptions |
| `Templates/PrescriptionContinuationA4Template.xaml` | A4 continuation template for multi-page prescriptions |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Interfaces/` | Print service interface definitions (IPrintService<TModel>, PrintOptions, enums) |
| `Models/` | Print data models (PrescriptionPrintModel, PrintLogEntry) |
| `Services/` | Print service implementations (PrescriptionPrintService, PrescriptionPdfExporter) |
| `Templates/` | WPF XAML print templates (A5/A4, normal/continuation) |

## For AI Agents

### Working In This Directory
- This is a Prism module -- register new print services in `PrintingModule.RegisterTypes()`
- `IPrintService<TModel>` is generic -- add new print types by creating a new TModel and implementing the service
- Paper sizes: A5 is the default for prescriptions (TCM standard), A4 is available for detailed prescriptions
- Continuation templates handle prescriptions that span multiple pages
- PDF export uses QuestPDF; print preview uses WPF FixedDocument + XPS
- The module only references `LYBT.Desktop.Infrastructure` (no direct business module dependencies)
- `SixLabors.Fonts` and `SixLabors.ImageSharp` are explicitly referenced to override QuestPDF's transitive dependencies for security

### Testing Requirements
- Test `PrescriptionPrintService` with various prescription sizes (single page, multi-page continuation)
- Verify PDF export produces valid PDF files
- Test `GetAvailablePrinters()` returns system printer list
- Test `PrintOptions` defaults (A5, Portrait, 1 copy, ShowDialog=true)

### Common Patterns
- Generic `IPrintService<TModel>` for type-safe print operations
- Template-based rendering: XAML templates for WPF print, QuestPDF fluent API for PDF
- `PrintOptions` DTO for printer selection, copies, paper size, orientation, duplex
- `ExportFormat` enum (Xps, Pdf) for output format selection
- `BatchPrintAsync` for bulk printing operations

## Dependencies

### Internal
- `LYBT.Desktop.Infrastructure` -- WPF infrastructure services

### External
- `QuestPDF` -- PDF generation library (fluent API, license: community)
- `SixLabors.Fonts` -- Font handling (explicit reference for security)
- `SixLabors.ImageSharp` -- Image processing (explicit reference for security)
- `Prism.Core` / `Prism.DryIoc` -- WPF MVVM framework
- `Microsoft.Extensions.Logging.Abstractions` -- Logging

<!-- MANUAL: -->
