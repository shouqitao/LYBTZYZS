---
created: 2025-09-03T13:20:35Z
last_updated: 2025-09-03T13:20:35Z
version: 1.0
author: Claude Code PM System
---

# Project Structure

## Root Directory Organization

```
LYBTZYZS/
├── .claude/                    # Claude Code configuration and context
│   ├── context/               # Project context documentation
│   ├── documents/             # Project management documents
│   └── scripts/               # PM automation scripts
├── .github/                   # GitHub configuration
│   └── workflows/            # CI/CD workflows
├── .serena/                   # Serena MCP server configuration
├── .vs/                       # Visual Studio configuration
├── docs/                      # Project documentation
│   ├── api/                  # API documentation
│   ├── architecture/         # Architecture documentation
│   ├── deployment/           # Deployment guides
│   ├── development/          # Development guides
│   ├── process/              # Process documentation
│   ├── requirements/         # Requirements documentation
│   ├── testing/              # Testing documentation
│   └── ultrathink/           # UltraThink methodology docs
├── scripts/                   # Build and utility scripts
├── src/                       # Source code
│   ├── Backend/              # Backend services
│   ├── Client/               # Client applications
│   ├── Frontend/             # Frontend applications
│   ├── Server/               # Server components
│   └── Shared/               # Shared libraries
├── tests/                     # Test projects
│   ├── Backend/              # Backend tests
│   ├── Frontend/             # Frontend tests
│   └── Integration/          # Integration tests
└── tools/                     # Development tools
```

## Solution Files

### Main Solutions
- **LYBT.All.sln** - Complete solution with all 48 projects
- **LYBT.Desktop.sln** - Desktop client solution
- **LYBT.Server.sln** - Server-side solution

## Source Code Structure

### Backend Structure (`src/Backend/`)
```
Backend/
├── Core/                      # Core backend libraries
│   ├── LYBT.Infrastructure/  # Infrastructure layer
│   └── LYBT.Models/          # Domain models
├── Modules/                   # Business modules
│   ├── LYBT.Module.Auth/     # Authentication module
│   ├── LYBT.Module.Consultation/ # Consultation module
│   ├── LYBT.Module.Formula/  # Formula management
│   ├── LYBT.Module.Herbs/    # Herbs management
│   ├── LYBT.Module.MedicalCase/ # Medical case management
│   ├── LYBT.Module.Patients/ # Patient management
│   ├── LYBT.Module.Prescriptions/ # Prescription management
│   └── LYBT.Module.Users/    # User management
└── Services/
    └── LYBT.WebAPI/          # Web API service
```

### Frontend Structure (`src/Frontend/Desktop/`)
```
Desktop/
├── Core/                      # Core frontend libraries
│   ├── Configuration/        # Configuration management
│   ├── Constants/            # Application constants
│   ├── Controls/             # Reusable controls
│   ├── Converters/           # XAML converters
│   ├── Helpers/              # Utility helpers
│   ├── Interfaces/           # Interface definitions
│   ├── Mapping/              # Object mapping
│   ├── Models/               # Frontend models
│   └── ViewModels/           # Base view models
├── Infrastructure/            # Frontend infrastructure
├── Modules/                   # Feature modules
│   ├── Authentication/       # Login/Auth module
│   ├── Consultation/         # Consultation UI
│   ├── SystemManagement/     # System management UI
│   └── Registration/         # Patient registration
├── Services/                  # Frontend services
├── Shell/                     # Application shell
└── Themes/                    # UI themes and styles
```

### Shared Structure (`src/Shared/`)
```
Shared/
├── LYBT.Shared.Models/        # Shared data models
│   ├── Auth/                 # Authentication models
│   ├── Common/               # Common models
│   ├── Contracts/            # DTO contracts
│   ├── Core/                 # Core base models
│   └── Enums/                # Shared enumerations
└── LYBT.Shared.Utilities/    # Shared utilities
    └── Helpers/              # Helper classes
```

## Module Organization Pattern

Each business module follows a consistent pattern:

### Backend Module Structure
```
LYBT.Module.{ModuleName}/
├── Interfaces/               # Module interfaces
│   ├── I{Module}Repository.cs
│   └── I{Module}Service.cs
├── Mapping/                  # AutoMapper profiles
│   └── {Module}MappingProfile.cs
├── Repositories/             # Data access layer
│   └── {Module}Repository.cs
├── Services/                 # Business logic layer
│   ├── {Module}Service.cs
│   ├── {Module}QueryService.cs
│   └── {Module}BusinessService.cs
└── {Module}Module.cs        # Module registration
```

### Frontend Module Structure (UltraThink Dual-Layer)
```
Modules/{ModuleName}/
├── ViewModels/              # View models
│   └── {Feature}ViewModel.cs
├── Views/                   # XAML views
│   └── {Feature}View.xaml
├── Services/                # Module services
│   ├── {Module}QueryService.cs    # Query operations
│   └── {Module}BusinessService.cs # Business operations
└── {Module}Module.cs       # Prism module registration
```

## File Naming Conventions

### C# Files
- **Models**: `{Entity}Model.cs` (e.g., `UserModel.cs`)
- **DTOs**: `{Entity}{Action}Dto.cs` (e.g., `UserCreateDto.cs`)
- **Services**: `{Entity}Service.cs` (e.g., `UserService.cs`)
- **Repositories**: `{Entity}Repository.cs`
- **ViewModels**: `{View}ViewModel.cs`
- **Interfaces**: `I{Contract}.cs`

### XAML Files
- **Views**: `{Feature}View.xaml`
- **Windows**: `{Feature}Window.xaml`
- **Controls**: `{Control}Control.xaml`
- **Styles**: `{Component}Styles.xaml`

## Key Directories

### Configuration
- `.claude/` - Claude Code specific configuration
- `.vs/` - Visual Studio settings
- `.github/` - GitHub Actions and templates

### Documentation
- `docs/api/` - API documentation
- `docs/architecture/` - System architecture
- `docs/requirements/` - Business requirements
- `docs/ultrathink/` - UltraThink methodology

### Build Output
- `src/BIN/Debug/` - Debug build output
- `src/BIN/Release/` - Release build output

### Testing
- `tests/Backend/` - Backend unit tests
- `tests/Frontend/` - Frontend unit tests
- `tests/Integration/` - Integration tests

## Architecture Patterns

### Frontend: UltraThink Dual-Layer
- **QueryService**: Complex queries, search, statistics
- **BusinessService**: Business logic and CRUD operations
- **Module**: Pure delegation pattern

### Backend: Traditional 3-Layer
- **Controller**: HTTP request handling
- **Service**: Business logic
- **Repository**: Data access

## Project Dependencies

### Core Technologies
- **.NET 8.0** - Primary framework
- **WPF** - Desktop client framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 8.0.17** - ORM
- **SQL Server** - Database

### Key Libraries
- **Prism.DryIoc 9.0.537** - MVVM framework
- **AutoMapper 13.0.1** - Object mapping
- **Refit 8.0.0** - REST client
- **Swashbuckle 9.0.1** - API documentation