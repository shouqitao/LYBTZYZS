<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.CardReader

## Purpose
Identity card reader hardware integration module for the TCM clinic desktop application. Uses the adapter/strategy pattern to support multiple card reader vendors (currently HuaDa HD100 via P/Invoke native DLLs, plus a MockCardReader for testing). Provides singleton `ICardReaderService` for reading national ID cards, extracting patient demographics (name, ID number, gender, ethnicity, address, photo), and integrating with the Patient module via `IPatientCardReaderIntegration` for find-or-create patient workflows including PRD-15 deduplication matching (IdNumber exact match, Name+BirthDate fuzzy match, multiple candidates, no match).

## Key Files
| File | Description |
|------|-------------|
| `CardReaderModule.cs` | Prism IModule entry point; registers `ICardReaderFactory` and `ICardReaderService` as singletons |
| `Abstractions/ICardReader.cs` | Strategy interface for card readers (Connect, ReadCard, DetectCard, events) |
| `Abstractions/ICardReaderFactory.cs` | Factory interface for creating vendor-specific readers |
| `Adapters/HuaDaHD100CardReader.cs` | HD100 implementation using P/Invoke to native HDstdapi.dll |
| `Adapters/MockCardReader.cs` | Test/mock reader returning sample data |
| `Services/CardReaderFactory.cs` | Factory implementation that selects reader by vendor name |
| `Services/CardReaderService.cs` | High-level service (singleton) wrapping factory, managing lifecycle |
| `Services/ICardReaderService.cs` | Service interface consumed by ViewModels |
| `Models/CardReadResult.cs` | Result DTO from card read (Name, IdNumber, Gender, Ethnicity, Address, PhotoPath) |
| `Integration/IPatientCardReaderIntegration.cs` | Cross-module contract for Patient module integration (FindByIdNumber, QuickCreate, FindOrCreate, MatchPatient) |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Abstractions/` | Interface definitions (ICardReader, ICardReaderFactory) |
| `Adapters/` | Vendor-specific reader implementations (HuaDa HD100, Mock) |
| `Models/` | Data transfer objects (CardReadResult) |
| `Native/` | Native DLL files (HDstdapi.dll etc.) copied to output at build |
| `Services/` | Service layer (CardReaderFactory, CardReaderService) |
| `Integration/` | Cross-module integration interfaces for Patient module |

## For AI Agents

### Working In This Directory
- This module uses P/Invoke with `AllowUnsafeBlocks=true` -- native DLLs are in `Native/` and copied to output via csproj `<None Update>` rules
- The adapter pattern means new vendors are added by implementing `ICardReader` in `Adapters/` and registering in `CardReaderFactory`
- `CardReaderService` is a singleton -- changes affect the entire application lifecycle
- Cross-module integration with Patient module is via `IPatientCardReaderIntegration` (implemented in the Patient module, not here)
- This module references `LYBT.Desktop.Infrastructure` and `LYBT.Shared.Models` only; it does NOT reference any business modules

### Testing Requirements
- Use `MockCardReader` for unit/integration tests -- never depend on physical hardware
- Test card read scenarios: success, connection failure, card not present, cancellation
- Test patient integration: FindPatientByIdNumber, QuickCreatePatient, MatchPatient deduplication chain

### Common Patterns
- Strategy pattern: `ICardReader` interface with vendor-specific adapters
- Factory pattern: `ICardReaderFactory` creates the appropriate reader
- Event-driven: `ConnectionStateChanged` and `CardDetected` events for reactive UI
- PRD-15 deduplication: `PatientMatchResult` with `PatientMatchType` enum (ExactMatch, FuzzyMatch, MultipleCandidates, NoMatch)

## Dependencies

### Internal
- `LYBT.Desktop.Infrastructure` -- WPF infrastructure services
- `LYBT.Shared.Models` -- Shared DTOs and contracts (PatientFromCardResult, PatientDetailDto)

### External
- `Prism.Core` / `Prism.DryIoc` / `Prism.Wpf` -- WPF MVVM framework
- Native DLLs: `HDstdapi.dll` (HuaDa card reader SDK)

<!-- MANUAL: -->
