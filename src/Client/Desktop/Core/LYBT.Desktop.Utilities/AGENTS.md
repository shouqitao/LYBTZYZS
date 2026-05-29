<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Utilities

## Purpose
Desktop utility class library providing Excel import/export functionality for the TCM clinic application. Uses NPOI (Apache POI .NET port) for reading and writing Excel files (.xls/.xlsx) without requiring Microsoft Office installation. Contains `ExcelHelper` with generic methods for converting between DataTable/collections and Excel workbooks. This is a standalone utility library with no Prism or MVVM dependencies -- it can be consumed by any layer of the desktop application.

## Key Files
| File | Description |
|------|-------------|
| `Excel/ExcelHelper.cs` | Static helper class for Excel operations (export collections to .xlsx, import .xlsx to DataTable, cell formatting, column mapping) |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Excel/` | Excel import/export utilities (ExcelHelper) |

## For AI Agents

### Working In This Directory
- This is a pure utility library -- no Prism modules, no DI registration, no ViewModels
- `ExcelHelper` uses static methods -- no instance state
- NPOI handles both .xls (HSSF) and .xlsx (XSSF) formats automatically
- The csproj sets `<AcceptNPOIOSMFLicense>true</AcceptNPOIOSMFLicense>` for NPOI license acceptance
- This project has zero internal project references -- it only depends on NPOI and System.ComponentModel.Annotations
- When adding new utility categories, create a new subdirectory (like `Excel/`) with the helper class

### Testing Requirements
- Test Excel export: verify file is valid xlsx, correct sheet name, correct column headers, correct data rows
- Test Excel import: verify DataTable populated correctly, handle empty files, handle malformed data
- Test edge cases: empty collections, null values, special characters in cell content, large datasets
- NPOI creates files in memory -- tests should verify byte array output, not file system

### Common Patterns
- Static helper class pattern (no DI, no instance state)
- Generic methods accepting `IEnumerable<T>` with column mapping via attributes or fluent configuration
- DataTable as intermediate representation for import operations
- Stream-based I/O (accept Stream, return Stream) for testability

## Dependencies

### Internal
- None (standalone utility library)

### External
- `NPOI` -- Excel file format library (Apache POI .NET port)
- `System.ComponentModel.Annotations` -- Data annotation attributes for column mapping

<!-- MANUAL: -->
